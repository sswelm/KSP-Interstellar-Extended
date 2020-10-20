using FNPlugin.Resources;
using System.Text;

namespace FNPlugin.Science
{
    [KSPModule("Sun Experiment")]
    class FNSunModuleScienceExperiment : ModuleScienceExperiment
    {
        // Settings
        [KSPField(guiActive = true, guiName = "Minimum experiment altitude", guiFormat = "F0", guiUnits = " km")]
        public double maximumDistanceInKm = 1e6;

        // Gui
        [KSPField(guiActive = true, guiName = "Current vessel altitude", guiFormat = "F0", guiUnits = " km")]
        public double vesselAltitudeInKm;
        [KSPField(guiActive = true, guiName = "In Star Orbit")]
        public bool inStarOrbit;

        public override void OnUpdate()
        {
            base.OnUpdate();

            vesselAltitudeInKm = vessel.altitude * 0.001;
            inStarOrbit = KopernicusHelper.IsStar(vessel.mainBody);
        }

        [KSPEvent(guiName = "Start Sun Experiment", active = true, guiActive = true)] //Deploy
        public new void DeployExperiment()
        {
            if (inStarOrbit == false)
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage("Not in orbit of a star", 4.0f, ScreenMessageStyle.UPPER_CENTER));
                return;
            }
            else if (vesselAltitudeInKm > maximumDistanceInKm)
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage("Needs to be lower than " + maximumDistanceInKm + " km from the star to perform the experiment", 4.0f, ScreenMessageStyle.UPPER_CENTER));
                return;
            }

            base.DeployExperiment();
        }

        [KSPAction("Start Sun Experiment")]
        public new void DeployAction(KSPActionParam actParams)
        {
            DeployExperiment();
        }

        public override string GetInfo()
        {
            return "Minimum experiment altitude: " + maximumDistanceInKm.ToString("F2") + " km";
        }
    }
}
