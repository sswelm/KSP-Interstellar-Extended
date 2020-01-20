using System;
using KSP.Localization;

namespace FNPlugin 
{
    [KSPModule("Radioactive Decay")]
    class ModuleElementRadioactiveDecay : PartModule 
    {
        // Persistent False
        [KSPField(isPersistant = false)]
        public double decayConstant = 0;
        [KSPField(isPersistant = false)]
        public string resourceName = "";
        [KSPField(isPersistant = false)]
        public string decayProduct = "";
        [KSPField(isPersistant = false)]
        public double convFactor = 1;
        [KSPField(isPersistant = true)]
        public double lastActiveTime = 1;

        protected double density_rat = 1;

        private bool resourceDefinitionsContainDecayProduct;

        public override void OnStart(PartModule.StartState state)
        {
            double time_diff = lastActiveTime - Planetarium.GetUniversalTime();

            if (state == StartState.Editor)
                return;

            var decay_resource = part.Resources[resourceName];
            if (decay_resource == null)
                return;

            resourceDefinitionsContainDecayProduct = PartResourceLibrary.Instance.resourceDefinitions.Contains(decayProduct);
            if (resourceDefinitionsContainDecayProduct)
            {
                var decay_density = PartResourceLibrary.Instance.GetDefinition(decayProduct).density;
                if (decay_density > 0 && decay_resource.info.density > 0)
                    density_rat = decay_resource.info.density / PartResourceLibrary.Instance.GetDefinition(decayProduct).density;
            }

            if (!CheatOptions.UnbreakableJoints && decay_resource != null && time_diff > 0)
            {
                double n_0 = decay_resource.amount;
                decay_resource.amount = n_0 * Math.Exp(-decayConstant * time_diff);
                double n_change = n_0 - decay_resource.amount;

                if (resourceDefinitionsContainDecayProduct && n_change > 0)
                    part.RequestResource(decayProduct, -n_change * density_rat);
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            var decay_resource = part.Resources[resourceName];
            if (decay_resource == null) return;

            lastActiveTime = Planetarium.GetUniversalTime();

            if (CheatOptions.UnbreakableJoints)
                return;

            double decay_amount = decayConstant * decay_resource.amount * TimeWarp.fixedDeltaTime;
            decay_resource.amount -= decay_amount;

            if (resourceDefinitionsContainDecayProduct && decay_amount > 0)
                part.RequestResource(decayProduct, -decay_amount * density_rat);
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_RadioactiveDecay_info");//"Radioactive Decay"
        }

    }
}
