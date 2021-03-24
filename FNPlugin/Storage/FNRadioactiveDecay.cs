using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Storage
{
    class ModuleElementRadioactiveDecay : FNRadioactiveDecay {}

    [KSPModule("Radioactive Decay")]
    class FNRadioactiveDecay : PartModule
    {
        private const double LengthYear = 365.25;
        private const double HourDay = 24;
        private const double MinutesInHour = 60;
        private const double SecondsInMinute = 60;
        private const double SecondsInDay = SecondsInMinute * MinutesInHour * HourDay;

        // isPersistent
        [KSPField(isPersistant = true)]
        public double lastActiveTime = -1;

        // Settings
        [KSPField(isPersistant = false)] public double halfLifeInYears = 0;
        [KSPField(isPersistant = false)] public double halfLifeInDays = 0;
        [KSPField(isPersistant = false)] public double decayConstant;
        [KSPField(isPersistant = false)] public string resourceName = "";
        [KSPField(isPersistant = false)] public string decayProduct = "";
        [KSPField(isPersistant = false)] public bool canConvertVolume = true;

        private double _densityRat = 1;
        private PartResourceDefinition _decaySourceDefinition;
        private PartResourceDefinition _decayProductDefinition;
        private readonly List<FNResourceTransfer> _managedTransferableResources = new List<FNResourceTransfer>();

        public override void OnStart(StartState state)
        {
            _decaySourceDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            _decayProductDefinition = PartResourceLibrary.Instance.GetDefinition(decayProduct);

            if (_decaySourceDefinition == null)
                return;

            var decayResource = part.Resources[resourceName];
            if (decayResource == null)
                return;

            if (part.vessel != null)
            {
                var compatibleTanks =
                    part.vessel.FindPartModulesImplementing<FNResourceTransfer>()
                        .Where(m => m.resourceName == _decayProductDefinition.displayName);

                _managedTransferableResources.AddRange(compatibleTanks);
            }

            if (halfLifeInYears > 0)
                decayConstant = Math.Log(2) / (halfLifeInYears * LengthYear * SecondsInDay);

            if (halfLifeInDays > 0)
                decayConstant = Math.Log(2) / (halfLifeInDays * SecondsInDay);

            if (_decayProductDefinition != null)
            {
                if (_decayProductDefinition.density > 0 && _decaySourceDefinition.density > 0)
                    _densityRat = (double)(decimal)_decaySourceDefinition.density / (double)(decimal)_decayProductDefinition.density;
            }

            if (state == StartState.Editor || CheatOptions.UnbreakableJoints)
                return;

            if (lastActiveTime < 0)
                lastActiveTime = Planetarium.GetUniversalTime();

            var timeDiffInSeconds = Planetarium.GetUniversalTime() - lastActiveTime;

            if (!(timeDiffInSeconds > 0)) return;

            var resourceAmount = decayResource.amount;
            decayResource.amount = resourceAmount * Math.Exp(-decayConstant * timeDiffInSeconds);

            if (_decayProductDefinition == null)
                return;

            var resourceChange = resourceAmount - decayResource.amount;
            if (resourceChange <= 0)
                return;

            var decayProductAmount = resourceChange * _densityRat;

            var decayProductResource = part.Resources.Get(_decayProductDefinition.id);

            if (canConvertVolume && decayProductResource != null)
            {
                decayProductResource.amount += decayProductAmount;

                var productOverflow = Math.Max(0, decayProductResource.amount - decayProductResource.maxAmount);
                if (productOverflow > 0)
                    decayProductResource.maxAmount = decayProductResource.amount;

                var previousAmount = decayResource.maxAmount;
                var appliedDecayAmount = productOverflow / _densityRat;
                decayResource.maxAmount -= appliedDecayAmount;
                var effectiveDecayAmount = previousAmount - decayResource.maxAmount;
                var decayDifference = appliedDecayAmount - effectiveDecayAmount;

                decayResource.amount = Math.Min(decayResource.amount, decayResource.maxAmount);

                return;
            }

            StoreDecayProduct(decayProductAmount, decayProductResource);
        }

        public void FixedUpdate()
        {
            if (CheatOptions.UnbreakableJoints) return;

            if (HighLogic.LoadedSceneIsEditor) return;

            if (_decaySourceDefinition == null) return;

            var decayResource = part.Resources[resourceName];
            if (decayResource == null) return;

            lastActiveTime = Planetarium.GetUniversalTime();

            var decayAmount = decayConstant * decayResource.amount * TimeWarp.fixedDeltaTime;
            decayResource.amount -= decayAmount;

            if (_decayProductDefinition == null || !(decayAmount > 0)) return;

            var decayProductAmount = decayAmount * _densityRat;

            var decayProductResource = part.Resources[decayProduct];

            if (canConvertVolume && decayProductResource != null)
            {
                decayProductResource.amount += decayProductAmount;

                var productOverflow = Math.Max(0, decayProductResource.amount - decayProductResource.maxAmount);
                if (productOverflow > 0)
                    decayProductResource.maxAmount = decayProductResource.amount;

                var previousAmount = decayResource.maxAmount;
                var appliedDecayAmount = productOverflow / _densityRat;
                decayResource.maxAmount -= appliedDecayAmount;
                var effectiveDecayAmount = previousAmount - decayResource.maxAmount;
                var decayDifference = appliedDecayAmount - effectiveDecayAmount;

                decayResource.amount = Math.Min(decayResource.amount, decayResource.maxAmount);

                return;
            }

            StoreDecayProduct(decayProductAmount, decayProductResource);
        }

        private void StoreDecayProduct(double decayProductAmount, PartResource decayProductResource)
        {
            var shortageDecayProductAmount = decayProductAmount;
            if (decayProductResource != null)
            {
                var newLocalDecayProductAmount = decayProductResource.amount + decayProductAmount;

                if (newLocalDecayProductAmount > decayProductResource.maxAmount)
                {
                    shortageDecayProductAmount = newLocalDecayProductAmount - decayProductResource.maxAmount;
                    decayProductResource.amount = decayProductResource.maxAmount;
                }
                else
                {
                    decayProductResource.amount = newLocalDecayProductAmount;
                    return;
                }
            }

            StoreProductToTransferableTank(shortageDecayProductAmount);
        }

        private void StoreProductToTransferableTank(double shortageDecayProductAmount)
        {
            if (shortageDecayProductAmount <= 0)
                return;

            var tanksWithAvailableStorage = _managedTransferableResources
                .Where(m => m.AvailableStorage > 0)
                .OrderByDescending(m => m.transferPriority).ToList();

            if (!tanksWithAvailableStorage.Any())
            {
                part.RequestResource(_decayProductDefinition.id, -shortageDecayProductAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                return;
            }

            foreach (var fnResourceTransfer in tanksWithAvailableStorage)
            {
                var unrestrictedNewAmount = fnResourceTransfer.PartResource.amount + shortageDecayProductAmount;
                shortageDecayProductAmount = unrestrictedNewAmount - fnResourceTransfer.PartResource.maxAmount;
                fnResourceTransfer.PartResource.amount = Math.Min(fnResourceTransfer.PartResource.maxAmount, unrestrictedNewAmount);

                if (shortageDecayProductAmount <= 0)
                    break;
            }
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_RadioactiveDecay_info");//"Radioactive Decay"
        }
    }
}
