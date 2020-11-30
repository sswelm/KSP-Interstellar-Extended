using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin
{
    // startup once durring flight
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightUIStarter : MonoBehaviour
    {
        public static bool hide_button = false;
        public static bool show_window = false;

        public void Start()
        {
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                hide_button = !hide_button;
            }
        }

        protected void OnGUI()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;

            if (vessel == null) return;

            var megajoules_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ElectricPowerInMegawatt);
            if (megajoules_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager mega_manager = megajoules_overmanager.getManagerForVessel(vessel);
                if (mega_manager != null && mega_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        mega_manager.ShowWindow();

                    // show window
                    mega_manager.OnGUI();
                }
            }

            var thermalpower_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ThermalPowerInMegawatt);
            if (thermalpower_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager thermal_manager = thermalpower_overmanager.getManagerForVessel(vessel);
                if (thermal_manager != null && thermal_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        thermal_manager.ShowWindow();

                    // show window
                    thermal_manager.OnGUI();
                }
            }

            var charged_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ChargedParticleInMegawatt);
            if (charged_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager charged_manager = charged_overmanager.getManagerForVessel(vessel);
                if (charged_manager != null && charged_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        charged_manager.ShowWindow();

                    // show window
                    charged_manager.OnGUI();
                }
            }

            var wasteheat_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.WasteHeatInMegawatt);
            if (wasteheat_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager waste_manager = wasteheat_overmanager.getManagerForVessel(vessel);
                if (waste_manager != null && waste_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        waste_manager.ShowWindow();

                    // show window
                    waste_manager.OnGUI();
                }
            }

            show_window = false;
        }
    }
}
