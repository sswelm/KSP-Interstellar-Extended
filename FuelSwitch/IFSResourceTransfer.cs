using System.Collections.Generic;
using System.Linq;
using System;

namespace InterstellarFuelSwitch
{
    public class IFSResourceTransfer : PartModule
    {
        public const string Group = "Transfer";
        public const string GroupTitle = "Transfer Priority";

        [KSPField] public string resourceName = "";
        [KSPField] public double maxAmount = 1;
        [KSPField] public double transferCost = 0.1;
        [KSPField] public bool showPriority = true;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = true, guiName = "Transfer Priority"), UI_FloatRange(minValue = -10, maxValue = 10, stepIncrement = 1, affectSymCounterparts = UI_Scene.All)]
        public float transferPriority = 0;

        private BaseField _transferPriorityField;
        private PartResourceDefinition _resourceDefinition;
        private PartResourceDefinition _powerDefinition;

        private readonly List<IFSResourceTransfer> _managedTransferableResources = new List<IFSResourceTransfer>();

        public double AvailableStorage => PartResource == null || !PartResource.flowState ? 0 : PartResource.maxAmount - PartResource.amount;
        public double AvailableAmount => PartResource == null || !PartResource.flowState ? 0 : PartResource.amount;

        public int TransferPriority { get; private set; }
        public PartResource PartResource { get; private set; }

        public override void OnStart(StartState state)
        {
            _transferPriorityField = Fields[nameof(transferPriority)];

            if (_transferPriorityField == null) return;

            _transferPriorityField.guiName = resourceName;

            _powerDefinition = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            _resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            if (_resourceDefinition == null)
                return;

            TransferPriority = (int)Math.Round(transferPriority);

            if (HighLogic.LoadedSceneIsEditor)
                return;

            var compatibleTanks =
                part.vessel.FindPartModulesImplementing<IFSResourceTransfer>()
                    .Where(m => m.resourceName == resourceName);

            _managedTransferableResources.AddRange(compatibleTanks);
        }

        public void FixedUpdate()
        {
            if (_resourceDefinition == null)
                return;

            PartResource = part.Resources[resourceName];
            if (PartResource == null || !PartResource.flowState)
            {
                _transferPriorityField.guiActive = false;
                return;
            }

            _transferPriorityField.guiActive = showPriority;

            TransferPriority = (int)Math.Round(transferPriority);

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

            var fixedDeltaTime = (double)(decimal) Math.Round(TimeWarp.fixedDeltaTime, 7);
            var fixedPowerRequest = fixedDeltaTime * transferCost;
            var fixedMaxAmount = maxAmount * fixedDeltaTime * GetPowerRatio(fixedPowerRequest);
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
                    {
                        partialRequest -= currentTank.PartResource.amount;
                        currentTank.PartResource.amount = 0;
                    }
                    else
                    {
                        currentTank.PartResource.amount -= partialRequest;
                    }
                    requestedResource -= partialRequest;

                    if (requestedResource <= 0)
                        break;
                }
            }

            var resourceReduction = fixedMaxAmount - requestedResource;
            var consumeRatio = fixedMaxAmount > 0 ? resourceReduction / fixedMaxAmount : 0;
            ConsumePower(consumeRatio, fixedPowerRequest);
            PartResource.amount += resourceReduction;
        }

        private void ConsumePower(double consumeRatio, double fixedPowerRequest)
        {
            if (CheatOptions.InfiniteElectricity)
                return;

            part.RequestResource(_powerDefinition.id, consumeRatio * fixedPowerRequest);
        }

        private double GetPowerRatio(double fixedPowerRequest)
        {
            var receivedPower = part.RequestResource(_powerDefinition.id, fixedPowerRequest, true);
            var powerRatio = fixedPowerRequest > 0 ? receivedPower / fixedPowerRequest : 0;
            return powerRatio;
        }
    }
}
