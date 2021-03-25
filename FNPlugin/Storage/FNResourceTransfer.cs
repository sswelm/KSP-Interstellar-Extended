using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Storage
{
    class FNResourceTransfer : ResourceSuppliableModule
    {
        public const string Group = "Transfer";
        public const string GroupTitle = "#LOC_KSPIE_ResourceTransfer_GroupTitle";

        [KSPField] public string resourceName = "";
        [KSPField] public double maxTransferCapacity = 1;
        [KSPField] public double transferCostPerUnit = 1;
        [KSPField] public bool showPriority = true;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Transfer Priority"), UI_FloatRange(minValue = -10, maxValue = 10, stepIncrement = 1, affectSymCounterparts = UI_Scene.All)]
        public float transferPriority = 0;

        private BaseField _transferPriorityField;
        private PartResourceDefinition _resourceDefinition;
        private PartResourceDefinition _powerDefinition;

        private readonly List<FNResourceTransfer> _managedTransferableResources = new List<FNResourceTransfer>();

        public double AvailableStorage => PartResource == null || !PartResource.flowState ? 0 : PartResource.maxAmount - PartResource.amount;
        public double AvailableAmount => PartResource == null || !PartResource.flowState ? 0 : PartResource.amount;

        public int TransferPriority { get; private set; }

        private PartResource _partResource;

        public PartResource PartResource
        {
            get
            {
                if (_partResource != null)
                    return _partResource;

                if (_resourceDefinition == null)
                    _resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);

                return _resourceDefinition == null ? null : part.Resources.Get(_resourceDefinition.id);
            }
        }


        public override void OnStart(StartState state)
        {
            _transferPriorityField = Fields[nameof(transferPriority)];

            if (_transferPriorityField == null) return;

            _transferPriorityField.guiName = resourceName;
            _transferPriorityField.Attribute.groupStartCollapsed = !showPriority;

            _powerDefinition = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            _resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            if (_resourceDefinition == null)
                return;

            TransferPriority = (int)Math.Round(transferPriority);

            if (HighLogic.LoadedSceneIsEditor)
                return;

            var compatibleTanks =
                part.vessel.FindPartModulesImplementing<FNResourceTransfer>()
                    .Where(m => m.resourceName == _resourceDefinition.name);

            _managedTransferableResources.AddRange(compatibleTanks);
        }

        public void Update()
        {
            if (PartResource == null || !PartResource.flowState)
            {
                if (HighLogic.LoadedSceneIsEditor)
                    _transferPriorityField.guiActiveEditor = false;
                else
                    _transferPriorityField.guiActive = false;

                return;
            }

            if (HighLogic.LoadedSceneIsEditor)
                _transferPriorityField.guiActiveEditor = true;
            else
                _transferPriorityField.guiActive = true;

            TransferPriority = (int)Math.Round(transferPriority);
        }

        public void FixedUpdate()
        {
            if (_resourceDefinition == null)
                return;

            // force refresh
            _partResource = null;

            if (PartResource == null || !PartResource.flowState)
                return;

            var availableStorage = PartResource.maxAmount - PartResource.amount;
            if (availableStorage <= 0)
                return;

            var tanksWithAvailableStorage = _managedTransferableResources
                .Where(m => m.PartResource != null && m.AvailableStorage > 0)
                .OrderByDescending(m => m.TransferPriority);

            var topTank = tanksWithAvailableStorage.FirstOrDefault();

            if (topTank == null || topTank.TransferPriority != TransferPriority)
                return;

            var tanksWithAvailableStoredResource = _managedTransferableResources
                .Where(m => m.PartResource != null &&  m.AvailableAmount > 0 && m.TransferPriority < TransferPriority)
                .OrderBy(m => m.TransferPriority).ToList();

            if (!tanksWithAvailableStoredResource.Any())
                return;

            var fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);
            var powerCostPerUnit = _resourceDefinition.resourceTransferMode == ResourceTransferMode.NONE ? transferCostPerUnit : 0;
            var maxTransfer = Math.Min(availableStorage, fixedDeltaTime * maxTransferCapacity);
            var fixedPowerRequest = maxTransfer * powerCostPerUnit;
            var fixedMaxAmount = maxTransfer * GetPowerRatio(fixedPowerRequest);
            var requestedResource = fixedMaxAmount;

            for (var priority = tanksWithAvailableStoredResource.First().TransferPriority; priority < TransferPriority; priority++)
            {
                var tanksWithSamePriority = tanksWithAvailableStoredResource
                        .Where(m => m.TransferPriority == priority).ToList();

                foreach (var currentTank in tanksWithSamePriority)
                {
                    var availableAmount = tanksWithSamePriority.Sum(m => m.AvailableAmount);
                    var partialRequest = Math.Min(availableAmount, requestedResource) / tanksWithSamePriority.Count;

                    if (currentTank.PartResource.amount <= partialRequest)
                        currentTank.PartResource.amount = 0;
                    else
                        currentTank.PartResource.amount -= partialRequest;

                    requestedResource -= partialRequest;

                    if (requestedResource <= 0)
                        break;
                }
            }

            var resourceReduction = fixedMaxAmount - requestedResource;
            var consumeRatio = fixedMaxAmount > 0 ? resourceReduction / fixedMaxAmount : 0;

            ConsumePower(consumeRatio, fixedPowerRequest);
            PartResource.amount = Math.Min(PartResource.maxAmount, PartResource.amount + resourceReduction);
        }

        private void ConsumePower(double consumeRatio, double fixedPowerRequest)
        {
            if (CheatOptions.InfiniteElectricity)
                return;

            var requestedPower = consumeRatio * fixedPowerRequest * 0.001;
            var receivedPower = ConsumeMegawatts(requestedPower);
            var powerShortage = requestedPower - receivedPower;
            part.RequestResource(_powerDefinition.id, powerShortage);
        }

        private double GetPowerRatio(double fixedPowerRequest)
        {
            if (CheatOptions.InfiniteElectricity)
                return 1;

            var availablePower = GetAvailableStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt);

            double powerRatio;
            if (availablePower * 1000 > fixedPowerRequest)
                powerRatio = 1;
            else
            {
                var receivedPower = part.RequestResource(_powerDefinition.id, fixedPowerRequest, true);
                powerRatio = fixedPowerRequest > 0 ? receivedPower / fixedPowerRequest : 0;
            }

            return powerRatio;
        }
    }
}
