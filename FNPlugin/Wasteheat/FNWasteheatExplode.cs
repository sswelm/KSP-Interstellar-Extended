using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Wasteheat 
{
    class FNWasteheatExplode : PartModule 
    {
        [KSPField]
        public double explodeFrame = 30;
        [KSPField]
        public double explodeRatio = 1;
        

        private int explode_counter;

        public override void OnStart(PartModule.StartState state)
        {
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            var wasteheatResource = part.Resources["WasteHeat"];

            if (!CheatOptions.IgnoreMaxTemperature && wasteheatResource != null && wasteheatResource.amount >= wasteheatResource.maxAmount * explodeRatio)
            {
                explode_counter++;
                if (explode_counter > explodeFrame)
                    part.explode();
            }
            else
                explode_counter = 0;
        }
    }
}
