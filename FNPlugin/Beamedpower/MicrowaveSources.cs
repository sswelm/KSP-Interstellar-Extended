using System.Collections.Generic;
using System.Linq;
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
            Debug.Log("[KSPI] - MicrowaveSources initialized");
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

                // first check if vessel is dead
                if (vessel.state == Vessel.State.DEAD)
                {
                    if (globalTransmitters.ContainsKey(vessel))
                    {
                        globalTransmitters.Remove(vessel);
                        globalRelays.Remove(vessel);
                        Debug.Log("[KSPI] - Unregisted Transmitter for vessel " + vessel.name + " " + vessel.id + " because is was destroyed!");
                    }
                    continue;
                }

                // if vessel is offloaded on rails, parse file system
                if (!vessel.loaded)
                {
                    //if (unloaded_counter % 101 != 1)                // sometimes rebuild unloaded vessels as transmitters and relays
                    //    continue;
                    if (initialized && i != unloaded_counter)
                        continue;

                    // add if vessel can act as a transmitter or relay
                    var trans_pers = MicrowavePowerTransmitter.getVesselMicrowavePersistanceForProtoVessel(vessel);

                    var hasAnyPower = trans_pers.getAvailablePowerInKW() > 0.001;
                    if (trans_pers.IsActive && hasAnyPower)
                    {
                        if (!globalTransmitters.ContainsKey(vessel))
                        {
                            Debug.Log("[KSPI] - Added unloaded Transmitter for vessel " + vessel.name);
                        }
                        globalTransmitters[vessel] = trans_pers;
                    }
                    else
                    {
                        if (globalTransmitters.Remove(vessel))
                        {
                            if (!trans_pers.IsActive && !hasAnyPower)
                                Debug.Log("[KSPI] - Unregisted unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is not active and has no power!");
                            else if (!trans_pers.IsActive)
                                Debug.Log("[KSPI] - Unregisted unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is not active!");
                            else if (!hasAnyPower)
                                Debug.Log("[KSPI] - Unregisted unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is has no power");
                        }
                    }

                    // only add if vessel can act as a relay
                    var relayPower = MicrowavePowerTransmitter.getVesselRelayPersistanceForProtoVessel(vessel);
                    if (relayPower.IsActive)
                        globalRelays[vessel] = relayPower;
                    else
                        globalRelays.Remove(vessel);

                    continue;
                }

                // if vessel is loaded
                if (vessel.FindPartModulesImplementing<MicrowavePowerTransmitter>().Any())
                {
                    // add if vessel can act as a transmitter or relay
                    var transmitterPower = MicrowavePowerTransmitter.getVesselMicrowavePersistanceForVessel(vessel);
                    if (transmitterPower != null && transmitterPower.IsActive && transmitterPower.getAvailablePowerInKW() > 0.001)
                    {
                        if (!globalTransmitters.ContainsKey(vessel))
                        {
                            Debug.Log("[KSPI] - Added loaded Transmitter for vessel " + vessel.name + " " + vessel.id);
                        }
                        globalTransmitters[vessel] = transmitterPower;
                    }
                    else
                        globalTransmitters.Remove(vessel);

                    // only add if vessel can act as a relay otherwise remove
                    var relayPower = MicrowavePowerTransmitter.getVesselRelayPersistenceForVessel(vessel);
                    if (relayPower != null && relayPower.IsActive)
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
