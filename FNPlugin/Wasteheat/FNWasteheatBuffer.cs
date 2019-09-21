using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Power;

namespace FNPlugin.Wasteheat
{
    class FNWasteheatBuffer : PartModule 
    {
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public double wasteHeatBufferMult = 1;
        [KSPField]
        public double baseResourceAmount = 2.0e+5;

        ResourceBuffers _resourceBuffers;

        public override void OnStart(PartModule.StartState state)
        {
            _resourceBuffers = new ResourceBuffers();
            _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, baseResourceAmount * wasteHeatBufferMult, true));
            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            _resourceBuffers.Init(this.part);
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            _resourceBuffers.UpdateBuffers();

            var wasteheatResource = part.Resources["WasteHeat"];
        }
    }
}
