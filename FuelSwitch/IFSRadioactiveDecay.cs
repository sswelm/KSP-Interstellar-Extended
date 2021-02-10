using KSP.Localization;
using System;

namespace InterstellarFuelSwitch
{
    [KSPModule("Radioactive Decay")]
    class IFSRadioactiveDecay : PartModule
    {
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
        private PartResourceDefinition _decayProductDefinition;

        public override void OnStart(StartState state)
        {
            var decayResource = part.Resources[resourceName];
            if (decayResource == null)
                return;

            if (halfLifeInYears > 0)
                decayConstant = Math.Log(2) / (halfLifeInYears * 365.25 * 24 * 60 * 60);

            if (halfLifeInDays > 0)
                decayConstant = Math.Log(2) / (halfLifeInDays * 24 * 60 * 60);

            _decayProductDefinition = PartResourceLibrary.Instance.GetDefinition(decayProduct);
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

            double nChange = resourceAmount - decayResource.amount;

            if (nChange <= 0)
                return;

            var decayProductAmount = nChange * _densityRat;

            var decayProductResource = part.Resources[decayProduct];

            if (canConvertVolume && decayProductResource != null)
            {
                decayProductResource.amount += decayProductAmount;

                var productOverflow = Math.Max(0, decayProductResource.amount - decayProductResource.maxAmount);
                if (productOverflow > 0)
                    decayProductResource.maxAmount = decayProductResource.amount;

                decayResource.maxAmount -= productOverflow / _densityRat;
                decayResource.amount = Math.Min(decayResource.amount, decayResource.maxAmount);

                return;
            }

            part.RequestResource(decayProduct, -decayProductAmount);
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            var decayResource = part.Resources[resourceName];
            if (decayResource == null) return;

            var currentActiveTime = Planetarium.GetUniversalTime();

            lastActiveTime = currentActiveTime;

            if (CheatOptions.UnbreakableJoints)
                return;

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

                decayResource.maxAmount -= productOverflow / _densityRat;
                decayResource.amount = Math.Min(decayResource.amount, decayResource.maxAmount);

                return;
            }

            part.RequestResource(decayProduct, -decayProductAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_IFS_RadioactiveDecay_Getinfo");//"Radioactive Decay"
        }
    }
}
