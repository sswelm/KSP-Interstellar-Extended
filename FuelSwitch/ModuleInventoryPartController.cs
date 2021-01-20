using System;
using TweakScale;

namespace InterstellarFuelSwitch
{
    class ModuleInventoryPartController: PartModule, IRescalable<InterstellarFuelSwitch>
    {
        private StartState _state;
        private ModuleInventoryPart _moduleInventoryPart;
        private BaseField _moduleInventoryPartField;

        private float _inventorySlots;
        private float _packedVolumeLimit;
        private bool _isInitialized;

        public override void OnStart(StartState state)
        {
            _state = state;
            Initialize();
            base.OnStart(state);
        }

        public void OnRescale(ScalingFactor factor)
        {
            if (HighLogic.CurrentGame.file_version_minor < 11)
                return;

            Initialize();

            if (_moduleInventoryPart == null)
                return;

            _moduleInventoryPart.InventorySlots = Math.Max(1, 3 * (int)(_inventorySlots * factor.absolute.linear / 3));
            _moduleInventoryPartField?.SetValue((_packedVolumeLimit * factor.absolute.cubic), _moduleInventoryPart);
            _moduleInventoryPart.OnStart(HighLogic.LoadedSceneIsEditor ? StartState.Editor : _state);
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _moduleInventoryPart = part.FindModuleImplementing<ModuleInventoryPart>();
            if (_moduleInventoryPart == null)
                return;

            _inventorySlots = _moduleInventoryPart.InventorySlots;
            _moduleInventoryPartField = _moduleInventoryPart.Fields["packedVolumeLimit"];
            if (_moduleInventoryPartField != null)
                _packedVolumeLimit = (float) _moduleInventoryPartField.GetValue(_moduleInventoryPart);
        }
    }
}
