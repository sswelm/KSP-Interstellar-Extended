using System;
using TweakScale;

namespace InterstellarFuelSwitch
{
    class ModuleInventoryPartController: PartModule, IRescalable<InterstellarFuelSwitch>
    {
        [KSPField] public double extInventorySlots = 9;
        [KSPField] public double extPackedVolumeLimit = 0;

        private StartState _state;
        private ModuleInventoryPart _moduleInventoryPart;

        private ModuleInventoryPart ModuleInventoryPart
        {
            get
            {
                if (_moduleInventoryPart != null)
                    return _moduleInventoryPart;

                _moduleInventoryPart = part.FindModuleImplementing<ModuleInventoryPart>();
                return _moduleInventoryPart;
            }
        }

        public override void OnStart(StartState state)
        {
            _state = state;
            base.OnStart(state);
        }

        public void OnRescale(ScalingFactor factor)
        {
            if (ModuleInventoryPart == null)
                return;

            _moduleInventoryPart.InventorySlots = Math.Max(1, 3 * (int)(extInventorySlots * factor.absolute.linear / 3));
            _moduleInventoryPart.packedVolumeLimit = (float)(extPackedVolumeLimit * factor.absolute.cubic);

            _moduleInventoryPart.OnStart(HighLogic.LoadedSceneIsEditor ? StartState.Editor : _state);
        }
    }
}
