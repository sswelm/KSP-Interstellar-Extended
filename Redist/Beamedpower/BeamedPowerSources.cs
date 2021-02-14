using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Beamedpower
{
    public delegate IVesselMicrowavePersistence GetVesselMicrowavePersistanceForProtoVessel(Vessel vessel);
    public delegate IVesselRelayPersistence GetVesselRelayPersistanceForProtoVessel(Vessel vessel);
    public delegate IVesselMicrowavePersistence GetVesselMicrowavePersistanceForVessel(Vessel vessel);
    public delegate IVesselRelayPersistence GetVesselRelayPersistenceForVessel(Vessel vessel);

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BeamedPowerSources : MonoBehaviour
    {
        public Dictionary<Vessel, IVesselMicrowavePersistence> globalTransmitters = new Dictionary<Vessel, IVesselMicrowavePersistence>();
        public Dictionary<Vessel, IVesselRelayPersistence> globalRelays = new Dictionary<Vessel, IVesselRelayPersistence>();

        public static GetVesselMicrowavePersistanceForProtoVessel getVesselMicrowavePersistanceForProtoVesselCallback;
        public static GetVesselRelayPersistanceForProtoVessel getVesselRelayPersistanceForProtoVesselCallback;
        public static GetVesselMicrowavePersistanceForVessel getVesselMicrowavePersistanceForVesselCallback;
        public static GetVesselRelayPersistenceForVessel getVesselRelayPersistenceForVesselCallback;

        public static BeamedPowerSources instance
        {
            get;
            private set;
        }

        void Start()
        {
            DontDestroyOnLoad(this.gameObject);
            instance = this;
            Debug.Log("[KSPI]: MicrowaveSources initialized");
        }

        int counter = -1;
        bool initialized = false;

        public void calculateTransmitters()
        {
            counter++;

            if (counter > FlightGlobals.Vessels.Count)
                counter = 0;

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
                        Debug.Log("[KSPI]: Unregisted Transmitter for vessel " + vessel.name + " " + vessel.id + " because is was destroyed!");
                    }
                    continue;
                }

                // if vessel is offloaded on rails, parse file system
                if (!vessel.loaded)
                {
                    if (initialized && i != counter)
                        continue;

                    if (getVesselMicrowavePersistanceForProtoVesselCallback != null)
                    {
                        // add if vessel can act as a transmitter or relay
                        var trans_pers = getVesselMicrowavePersistanceForProtoVesselCallback(vessel);

                        var hasAnyPower = trans_pers.getAvailablePowerInKW() > 0.001;
                        if (trans_pers.IsActive && hasAnyPower)
                        {
                            if (!globalTransmitters.ContainsKey(vessel))
                            {
                                Debug.Log("[KSPI]: Added unloaded Transmitter for vessel " + vessel.name);
                            }
                            globalTransmitters[vessel] = trans_pers;
                        }
                        else
                        {
                            if (globalTransmitters.Remove(vessel))
                            {
                                if (!trans_pers.IsActive && !hasAnyPower)
                                    Debug.Log("[KSPI]: Unregisted unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is not active and has no power!");
                                else if (!trans_pers.IsActive)
                                    Debug.Log("[KSPI]: Unregisted unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is not active!");
                                else if (!hasAnyPower)
                                    Debug.Log("[KSPI]: Unregisted unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is has no power");
                            }
                        }
                    }

                    if (getVesselRelayPersistanceForProtoVesselCallback != null)
                    {
                        // only add if vessel can act as a relay
                        var relayPower = getVesselRelayPersistanceForProtoVesselCallback(vessel);

                        if (relayPower.IsActive)
                            globalRelays[vessel] = relayPower;
                        else
                            globalRelays.Remove(vessel);
                    }

                    continue;
                }

                // if vessel is loaded
                if (vessel.FindPartModulesImplementing<IMicrowavePowerTransmitter>().Any())
                {
                    if (getVesselMicrowavePersistanceForVesselCallback != null)
                    {
                        // add if vessel can act as a transmitter or relay
                        var transmitterPower = getVesselMicrowavePersistanceForVesselCallback(vessel);

                        if (transmitterPower != null && transmitterPower.IsActive && transmitterPower.getAvailablePowerInKW() > 0.001)
                        {
                            if (!globalTransmitters.ContainsKey(vessel))
                            {
                                Debug.Log("[KSPI]: Added loaded Transmitter for vessel " + vessel.name + " " + vessel.id);
                            }
                            globalTransmitters[vessel] = transmitterPower;
                        }
                        else
                            globalTransmitters.Remove(vessel);

                        // only add if vessel can act as a relay otherwise remove
                        var relayPower = getVesselRelayPersistenceForVesselCallback(vessel);

                        if (relayPower != null && relayPower.IsActive)
                            globalRelays[vessel] = relayPower;
                        else
                            globalRelays.Remove(vessel);
                    }
                }
            }
            initialized = true;

        }

        void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
                calculateTransmitters();
        }
    }
}
