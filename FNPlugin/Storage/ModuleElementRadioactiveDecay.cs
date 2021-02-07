using System;
using KSP.Localization;

namespace FNPlugin
{
    [KSPModule("Radioactive Decay")]
    class ModuleElementRadioactiveDecay : PartModule
    {
        // Persistent False
        [KSPField(isPersistant = false)]
        public double halfLifeInYears = 0;
        [KSPField(isPersistant = false)]
        public double halfLifeInDays = 0;
        [KSPField(isPersistant = false)]
        public double decayConstant = 0;
        [KSPField(isPersistant = false)]
        public string resourceName = "";
        [KSPField(isPersistant = false)]
        public string decayProduct = "";
        [KSPField(isPersistant = true)]
        public double lastActiveTime = 1;


        private double _densityRat = 1;
        private PartResourceDefinition _decayDefinition;

        public override void OnStart(StartState state)
        {
            var decayResource = part.Resources[resourceName];
            if (decayResource == null)
                return;

            if (halfLifeInYears > 0)
                decayConstant = Math.Log(2) / (halfLifeInYears * 365.25 * 24 * 60 * 60);

            if (halfLifeInDays > 0)
                decayConstant = Math.Log(2) / (halfLifeInDays * 24 * 60 * 60);

            _decayDefinition = PartResourceLibrary.Instance.GetDefinition(decayProduct);
            if (_decayDefinition != null)
            {
                var decayDensity = _decayDefinition.density;
                if (decayDensity > 0 && decayResource.info.density > 0)
                    _densityRat = (double)(decimal)decayResource.info.density / (double)(decimal)_decayDefinition.density;
            }

            if (state == StartState.Editor || CheatOptions.UnbreakableJoints)
                return;

            double timeDiffInSeconds = lastActiveTime - Planetarium.GetUniversalTime();

            if (!(timeDiffInSeconds > 0)) return;

            double resourceAmount = decayResource.amount;
            decayResource.amount = resourceAmount * Math.Exp(-decayConstant * timeDiffInSeconds);
            double nChange = resourceAmount - decayResource.amount;

            if (_decayDefinition != null && nChange > 0)
                part.RequestResource(decayProduct, -nChange * _densityRat);
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

            if (_decayDefinition == null || !(decayAmount > 0)) return;

            var requestAmount = decayAmount * _densityRat;
            part.RequestResource(decayProduct, -requestAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_RadioactiveDecay_info");//"Radioactive Decay"
        }
    }
}
