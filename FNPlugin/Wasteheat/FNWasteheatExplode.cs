namespace FNPlugin.Wasteheat 
{
    class FNWasteheatExplode : PartModule
    {
        [KSPField] 
        public bool activeOnlyWhenActivated = true;
        [KSPField]
        public double explodeFrame = 25;    // half a second
        [KSPField]
        public double explodeRatio = 1;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "Explosion Potential")]
        public float explosionPotential;

        private int _explodeCounter;

        public override void OnStart(StartState state)
        {
            if (explosionPotential <= 0)
                explosionPotential = part.explosionPotential;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            if (!enabled && activeOnlyWhenActivated == false)
                base.OnFixedUpdate();
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            var wasteheatResource = part.Resources[ResourceManager.FNRESOURCE_WASTEHEAT];

            if (!CheatOptions.IgnoreMaxTemperature && wasteheatResource != null && wasteheatResource.amount >= wasteheatResource.maxAmount * explodeRatio)
            {
                _explodeCounter++;
                if (_explodeCounter < explodeFrame) return;

                part.explosionPotential = explosionPotential;
                part.explode();
            }
            else
                _explodeCounter = 0;
        }
    }
}