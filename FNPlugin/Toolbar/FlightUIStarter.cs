using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).hasManagerForVessel(vessel) && !hide_button)
            {
                ORSResourceManager mega_manager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).getManagerForVessel(vessel);
                if (mega_manager.getPartModule() != null)
                {
                    // activate rendering
                    if (show_window)
                        mega_manager.showWindow();

                    // show window
                    mega_manager.OnGUI();
                }
            }

            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).hasManagerForVessel(vessel) && !hide_button)
            {
                ORSResourceManager thermal_manager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_THERMALPOWER).getManagerForVessel(vessel);
                if (thermal_manager.getPartModule() != null)
                {
                    // activate rendering
                    if (show_window)
                        thermal_manager.showWindow();

                    // show window
                    thermal_manager.OnGUI();
                }
            }

            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_CHARGED_PARTICLES).hasManagerForVessel(vessel) && !hide_button)
            {
                ORSResourceManager charged_manager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_CHARGED_PARTICLES).getManagerForVessel(vessel);
                if (charged_manager.getPartModule() != null)
                {
                    // activate rendering
                    if (show_window)
                        charged_manager.showWindow();

                    // show window
                    charged_manager.OnGUI();
                }
            }

            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_WASTEHEAT).hasManagerForVessel(vessel) && !hide_button)
            {
                ORSResourceManager waste_manager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_WASTEHEAT).getManagerForVessel(vessel);
                if (waste_manager.getPartModule() != null)
                {
                    // activate rendering
                    if (show_window)
                        waste_manager.showWindow();

                    // show window
                    waste_manager.OnGUI();
                }
            }

        }
    }
}
