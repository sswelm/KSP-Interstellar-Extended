using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    // startup once during flight
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightUIStarter : MonoBehaviour
    {
        public static bool hideButton;
        public static bool showWindow;

        public void Start()
        {
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                hideButton = !hideButton;
            }
        }

        protected void OnGUI()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;

            if (vessel == null) return;

            var megajoulesOvermanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ElectricPowerInMegawatt);
            if (megajoulesOvermanager.hasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager megaManager = megajoulesOvermanager.getManagerForVessel(vessel);
                if (megaManager != null && megaManager.PartModule != null)
                {
                    // activate rendering
                    if (showWindow)
                        megaManager.ShowWindow();

                    // show window
                    megaManager.OnGUI();
                }
            }

            var thermalPowerOvermanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ThermalPowerInMegawatt);
            if (thermalPowerOvermanager.hasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager thermalManager = thermalPowerOvermanager.getManagerForVessel(vessel);
                if (thermalManager != null && thermalManager.PartModule != null)
                {
                    // activate rendering
                    if (showWindow)
                        thermalManager.ShowWindow();

                    // show window
                    thermalManager.OnGUI();
                }
            }

            var chargedOvermanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ChargedParticleInMegawatt);
            if (chargedOvermanager.hasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager chargedManager = chargedOvermanager.getManagerForVessel(vessel);
                if (chargedManager != null && chargedManager.PartModule != null)
                {
                    // activate rendering
                    if (showWindow)
                        chargedManager.ShowWindow();

                    // show window
                    chargedManager.OnGUI();
                }
            }

            var wasteheatOvermanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.WasteHeatInMegawatt);
            if (wasteheatOvermanager.hasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager wasteManager = wasteheatOvermanager.getManagerForVessel(vessel);
                if (wasteManager != null && wasteManager.PartModule != null)
                {
                    // activate rendering
                    if (showWindow)
                        wasteManager.ShowWindow();

                    // show window
                    wasteManager.OnGUI();
                }
            }

            showWindow = false;
        }
    }
}
