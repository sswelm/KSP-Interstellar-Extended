using FNPlugin.Power;
using FNPlugin.Resources;

namespace FNPlugin.Wasteheat
{
    class FNWasteheatBuffer : PartModule
    {
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public double wasteHeatBufferMult = 1;
        [KSPField] public double baseResourceAmount = 2.0e+5;

        ResourceBuffers _resourceBuffers;

        public override void OnStart(StartState state)
        {
            _resourceBuffers = new ResourceBuffers();
            _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, baseResourceAmount * wasteHeatBufferMult, true));
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.Init(part);
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.UpdateBuffers();
        }
    }
}
