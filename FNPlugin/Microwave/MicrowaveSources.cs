using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class MicrowaveSources : MonoBehaviour
    {
        public Dictionary<Vessel, VesselMicrowavePersistence> globalTransmitters = new Dictionary<Vessel, VesselMicrowavePersistence>();
        public Dictionary<Vessel, VesselRelayPersistence> globalRelays = new Dictionary<Vessel, VesselRelayPersistence>();

        public static MicrowaveSources instance
        {
            get;
            private set;
        }

        void Start()
        {
            DontDestroyOnLoad(this.gameObject);
            instance = this;
            Debug.Log("[KSP Interstellar]: MicrowaveSources initialized");
        }

        int unloaded_counter = -1;
        bool initialized = false;

        public void calculateTransmitters()
        {
            unloaded_counter++;

            if (unloaded_counter > FlightGlobals.Vessels.Count)
                unloaded_counter = 0;

            //foreach (var vessel in FlightGlobals.Vessels)
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++ )
            {
                var vessel = FlightGlobals.Vessels[i];

                // if vessel is offloaded to rails, parse file system
                if (vessel.state == Vessel.State.INACTIVE)
                {
                    //if (unloaded_counter % 101 != 1)                // sometimes rebuild unloaded vessels as transmitters and relays
                    //    continue;
                    if (initialized && i != unloaded_counter)
                        continue;

                    //Debug.Log("[KSP Interstellar]: update tranmitter for offloaded vessel " + i);

                    // add if vessel can act as a transmitter or relay
                    var trans_pers = MicrowavePowerTransmitter.getVesselMicrowavePersistanceForProtoVessel(vessel);
                    if (trans_pers.IsActive && trans_pers.getAvailablePower() > 0.001)
                        globalTransmitters[vessel] = trans_pers;
                    else
                        globalTransmitters.Remove(vessel);

                    // obly add if vessel can act as a relay
                    var relayPower = MicrowavePowerTransmitter.getVesselRelayPersistanceForProtoVessel(vessel);
                    if (relayPower.IsActive)
                        globalRelays[vessel] = relayPower;
                    else
                        globalRelays.Remove(vessel);

                    continue;
                }

                // if vessel is dead
                if (vessel.state == Vessel.State.DEAD)
                {
                    globalTransmitters.Remove(vessel);
                    globalRelays.Remove(vessel);
                    continue;
                }

                // if vessel is loaded
                if (vessel.FindPartModulesImplementing<MicrowavePowerTransmitter>().Any())
                {
                    // add if vessel can act as a transmitter or relay
                    var transmitterPower = MicrowavePowerTransmitter.getVesselMicrowavePersistanceForVessel(vessel);
                    if (transmitterPower.IsActive && transmitterPower.getAvailablePower() > 0.001)
                        globalTransmitters[vessel] = transmitterPower;
                    else
                        globalTransmitters.Remove(vessel);

                    // obly add if vessel can act as a relay
                    var relayPower = MicrowavePowerTransmitter.getVesselRelayPersistenceForVessel(vessel);
                    if (relayPower.IsActive)
                        globalRelays[vessel] = relayPower;
                    else
                        globalRelays.Remove(vessel);
                }
            }
            initialized = true;

        }

        uint counter = 0;
        void Update()                  
        {
            // update every 41e frame
            if (counter++ % 41 == 0 && HighLogic.LoadedSceneIsFlight)
                calculateTransmitters();
        }
    }
}
