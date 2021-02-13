using KSP.Localization;
using System;

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

        // isPersistent
        [KSPField(isPersistant = true)]
        public double lastActiveTime = 1;

        // Settings
        [KSPField(isPersistant = false)]
        public double halfLifeInYears = 0;
        [KSPField(isPersistant = false)]
        public double halfLifeInDays = 0;
        [KSPField(isPersistant = false)]
        public double decayConstant;
        [KSPField(isPersistant = false)]
        public string resourceName = "";
        [KSPField(isPersistant = false)]
        public string decayProduct = "";
        [KSPField(isPersistant = false)]
        public bool canConvertVolume = true;

        private double _densityRat = 1;
        private PartResourceDefinition _decaySourceDefinition;
        private PartResourceDefinition _decayProductDefinition;

        public override void OnStart(StartState state)
        {
            _decaySourceDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            _decayProductDefinition = PartResourceLibrary.Instance.GetDefinition(decayProduct);

            if (_decaySourceDefinition == null)
                return;

            if (CheatOptions.UnbreakableJoints)
                return;

            var decayResource = part.Resources[resourceName];
            if (decayResource == null)
                return;

            if (halfLifeInYears > 0)
                decayConstant = Math.Log(2) / (halfLifeInYears * LengthYear * HourDay * MinutesInHour * SecondsInMinute);

            if (halfLifeInDays > 0)
                decayConstant = Math.Log(2) / (halfLifeInDays * HourDay * MinutesInHour * SecondsInMinute);

            if (_decayProductDefinition != null)
            {
                var decayDensity = _decayProductDefinition.density;
                if (decayDensity > 0 && decayResource.info.density > 0)
                    _densityRat = (double)(decimal)decayResource.info.density / (double)(decimal)_decayProductDefinition.density;
            }

            if (state == StartState.Editor || CheatOptions.UnbreakableJoints)
                return;

            double timeDiffInSeconds = lastActiveTime - Planetarium.GetUniversalTime();

            if (!(timeDiffInSeconds > 0)) return;

            double resourceAmount = decayResource.amount;
            decayResource.amount = resourceAmount * Math.Exp(-decayConstant * timeDiffInSeconds);

            if (_decayProductDefinition == null)
                return;

            double resourceChange = resourceAmount - decayResource.amount;
            if (resourceChange <= 0)
                return;

            var decayProductAmount = resourceChange * _densityRat;

            var decayProductResource = part.Resources[decayProduct];

            if (canConvertVolume && decayProductResource != null)
            {
                decayProductResource.amount += decayProductAmount;

                var productOverflow = Math.Max(0, decayProductResource.amount - decayProductResource.maxAmount);
                if (productOverflow > 0)
                    decayProductResource.maxAmount = decayProductResource.amount;

                var appliedDecayAmount = productOverflow / _densityRat;
                decayResource.maxAmount -= appliedDecayAmount;

                decayResource.amount = Math.Min(decayResource.amount, decayResource.maxAmount);

                return;
            }

            part.RequestResource(decayProduct, -decayProductAmount);
        }

        public void FixedUpdate()
        {
            if (CheatOptions.UnbreakableJoints) return;

            if (HighLogic.LoadedSceneIsEditor) return;

            if (_decaySourceDefinition == null) return;

            var decayResource = part.Resources[resourceName];
            if (decayResource == null) return;

            var currentActiveTime = Planetarium.GetUniversalTime();

            lastActiveTime = currentActiveTime;

            double decayAmount = decayConstant * decayResource.amount * TimeWarp.fixedDeltaTime;
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

            part.RequestResource(decayProduct, -decayProductAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_RadioactiveDecay_info");//"Radioactive Decay"
        }
    }
}
