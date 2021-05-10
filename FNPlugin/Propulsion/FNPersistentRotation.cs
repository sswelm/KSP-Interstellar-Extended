using FNPlugin.Extensions;

namespace FNPlugin.Propulsion
{
    [KSPModule("Persistent Rotation")]
    class FNPersistentRotation : PartModule
    {
        // Saved fields
        [KSPField(isPersistant = true)] public VesselAutopilot.AutopilotMode persistentAutopilotMode;
        [KSPField(isPersistant = true)] public double ratioHeadingVersusRequest;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor =  true, guiName = "Persistent Rotation"), UI_Toggle(disabledText = "#autoLOC_247995", enabledText = "#autoLOC_900889")]
        public bool IsEnabled = true;

        // Session fields
        private int _countdown = 100;
        private int _vesselChangedSoiCountdown;

        public void VesselChangedSoi()
        {
            _vesselChangedSoiCountdown = 10;
        }

        public void FixedUpdate()
        {
            if (IsEnabled == false)
                return;

            if (vessel == null)
                return;

            if (_vesselChangedSoiCountdown > 0)
                _vesselChangedSoiCountdown--;

            if (_countdown > 0 && ratioHeadingVersusRequest > 0.999)
            {
                _countdown--;

                if (persistentAutopilotMode != vessel.Autopilot.Mode)
                {
                    vessel.Autopilot.SetMode(persistentAutopilotMode);
                    return;
                }
                else
                    ratioHeadingVersusRequest = vessel.PersistHeading(true);
            }

            // persist heading and drop out of warp when previously was maintaining heading
            ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSoiCountdown > 0, ratioHeadingVersusRequest == 1);
            persistentAutopilotMode = vessel.Autopilot.Mode;
        }
    }
}
