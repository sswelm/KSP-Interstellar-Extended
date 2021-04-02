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

            var megajoulesOvermanager = ResourceOvermanager.GetResourceOvermanagerForResource(ResourceSettings.Config.ElectricPowerInMegawatt);
            if (megajoulesOvermanager.HasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager megaManager = megajoulesOvermanager.GetManagerForVessel(vessel);
                if (megaManager != null && megaManager.PartModule != null)
                {
                    // activate rendering
                    if (showWindow)
                        megaManager.ShowWindow();

                    // show window
                    megaManager.OnGUI();
                }
            }

            var thermalPowerOvermanager = ResourceOvermanager.GetResourceOvermanagerForResource(ResourceSettings.Config.ThermalPowerInMegawatt);
            if (thermalPowerOvermanager.HasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager thermalManager = thermalPowerOvermanager.GetManagerForVessel(vessel);
                if (thermalManager != null && thermalManager.PartModule != null)
                {
                    // activate rendering
                    if (showWindow)
                        thermalManager.ShowWindow();

                    // show window
                    thermalManager.OnGUI();
                }
            }

            var chargedOvermanager = ResourceOvermanager.GetResourceOvermanagerForResource(ResourceSettings.Config.ChargedPowerInMegawatt);
            if (chargedOvermanager.HasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager chargedManager = chargedOvermanager.GetManagerForVessel(vessel);
                if (chargedManager != null && chargedManager.PartModule != null)
                {
                    // activate rendering
                    if (showWindow)
                        chargedManager.ShowWindow();

                    // show window
                    chargedManager.OnGUI();
                }
            }

            var wasteheatOvermanager = ResourceOvermanager.GetResourceOvermanagerForResource(ResourceSettings.Config.WasteHeatInMegawatt);
            if (wasteheatOvermanager.HasManagerForVessel(vessel) && !hideButton)
            {
                ResourceManager wasteManager = wasteheatOvermanager.GetManagerForVessel(vessel);
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
