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

            if (ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_MEGAJOULES).hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager mega_manager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_MEGAJOULES).getManagerForVessel(vessel);
                if (mega_manager != null && mega_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        mega_manager.showWindow();

                    // show window
                    mega_manager.OnGUI();
                }
            }

            if (ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_THERMALPOWER).hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager thermal_manager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_THERMALPOWER).getManagerForVessel(vessel);
                if (thermal_manager != null && thermal_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        thermal_manager.showWindow();

                    // show window
                    thermal_manager.OnGUI();
                }
            }

            if (ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_CHARGED_PARTICLES).hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager charged_manager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_CHARGED_PARTICLES).getManagerForVessel(vessel);
                if (charged_manager != null && charged_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        charged_manager.showWindow();

                    // show window
                    charged_manager.OnGUI();
                }
            }

            if (ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_WASTEHEAT).hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager waste_manager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_WASTEHEAT).getManagerForVessel(vessel);
                if (waste_manager != null && waste_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        waste_manager.showWindow();

                    // show window
                    waste_manager.OnGUI();
                }
            }

            show_window = false;
        }
    }
}
