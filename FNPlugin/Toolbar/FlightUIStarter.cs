using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
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
            string resourcename = FNResourceManager.FNRESOURCE_MEGAJOULES;
            Vessel vessel = FlightGlobals.ActiveVessel;
            ORSResourceManager mega_manager = null;

            if (vessel == null) return;


            if (FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel) && !hide_button)
            {
                mega_manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
                if (mega_manager.getPartModule() != null)
                {
                    mega_manager.OnGUI();

                    if (show_window)
                        mega_manager.showWindow();
                }
            }

        }
    }
}
