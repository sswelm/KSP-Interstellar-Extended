using FNPlugin.Beamedpower;
using FNPlugin.Powermanagement;
using FNPlugin.Propulsion;
using FNPlugin.Science;
using FNPlugin.Wasteheat;
using UnityEngine;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GameEventSubscriber : MonoBehaviour
    {
        void Start()
        {
            BeamedPowerSources.getVesselMicrowavePersistanceForProtoVesselCallback = BeamedPowerTransmitter.GetVesselMicrowavePersistenceForProtoVessel;
            BeamedPowerSources.getVesselRelayPersistanceForProtoVesselCallback = BeamedPowerTransmitter.GetVesselRelayPersistenceForProtoVessel;
            BeamedPowerSources.getVesselMicrowavePersistanceForVesselCallback = BeamedPowerTransmitter.GetVesselMicrowavePersistenceForVessel;
            BeamedPowerSources.getVesselRelayPersistenceForVesselCallback = BeamedPowerTransmitter.GetVesselRelayPersistenceForVessel;

            GameEvents.onGameStateSaved.Add(OnGameStateSaved);
            GameEvents.onDockingComplete.Add(OnDockingComplete);
            GameEvents.onPartDeCoupleComplete.Add(OnPartDeCoupleComplete);
            GameEvents.onVesselSOIChanged.Add(OnVesselSOIChanged);
            GameEvents.onPartDestroyed.Add(OnPartDestroyed);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOnRails);

            var kerbalismVersionStr = $"{Kerbalism.versionMajor}.{Kerbalism.versionMajorRevision}.{Kerbalism.versionMinor}.{Kerbalism.versionMinorRevision}";

            if (Kerbalism.versionMajor > 0)
                Debug.Log("[KSPI]: Loaded Kerbalism " + kerbalismVersionStr);

            Debug.Log("[KSPI]: GameEventSubscriber Initialized");
        }
        void OnDestroy()
        {
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOnRails);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
            GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
            GameEvents.onGameStateSaved.Remove(OnGameStateSaved);
            GameEvents.onDockingComplete.Remove(OnDockingComplete);
            GameEvents.onPartDeCoupleComplete.Remove(OnPartDeCoupleComplete);
            GameEvents.onVesselSOIChanged.Remove(OnVesselSOIChanged);

            Debug.Log("[KSPI]: GameEventSubscriber Deinitialised");
        }

        void OnVesselGoOnRails(Vessel vessel)
        {
            foreach (var part in vessel.Parts)
            {
                var autoStrutEvent = part.Events["ToggleAutoStrut"];
                if (autoStrutEvent != null)
                {
                    autoStrutEvent.guiActive = true;
                    autoStrutEvent.guiActiveUncommand = true;
                    autoStrutEvent.guiActiveUnfocused = true;
                    autoStrutEvent.requireFullControl = false;
                }

                var rigidAttachmentEvent = part.Events["ToggleRigidAttachment"];
                if (rigidAttachmentEvent != null)
                {
                    rigidAttachmentEvent.guiActive = true;
                    rigidAttachmentEvent.guiActiveUncommand = true;
                    rigidAttachmentEvent.guiActiveUnfocused = true;
                    rigidAttachmentEvent.requireFullControl = false;
                }
            }
        }

        void OnGameStateSaved(Game game)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnGameStateSaved");
            PluginHelper.LoadSaveFile();
        }

        void OnDockingComplete(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnDockingComplete");

            ResourceOvermanager.Reset();
            SupplyPriorityManager.Reset();

            ResetReceivers();
        }

        void OnPartDestroyed(Part part)
        {
            //Debug.Log("[KSPI]: GameEventSubscriber - detected OnPartDestroyed");

            //var drive = part.FindModuleImplementing<AlcubierreDrive>();

            //if (drive == null) return;

            //if (drive.IsSlave)
            //{
            //    Debug.Log("[KSPI]: GameEventSubscriber - destroyed part is a slave warpdrive");
            //    drive = drive.vessel.FindPartModulesImplementing<AlcubierreDrive>().FirstOrDefault(m => !m.IsSlave);
            //}

            //if (drive != null)
            //{
            //    Debug.Log("[KSPI]: GameEventSubscriber - deactivate master warp drive");
            //    drive.DeactivateWarpDrive();
            //}
        }

        void OnVesselSOIChanged (GameEvents.HostedFromToAction<Vessel, CelestialBody> gameEvent)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnVesselSOIChanged");
            gameEvent.host.FindPartModulesImplementing<FNPersistentRotation>().ForEach(e => e.VesselChangedSoi());
            gameEvent.host.FindPartModulesImplementing<ElectricEngineControllerFX>().ForEach(e => e.VesselChangedSoi());
            gameEvent.host.FindPartModulesImplementing<ModuleEnginesWarp>().ForEach(e => e.VesselChangedSoi());
            gameEvent.host.FindPartModulesImplementing<InterstellarEngineController>().ForEach(e => e.VesselChangedSoi());
            gameEvent.host.FindPartModulesImplementing<AlcubierreDrive>().ForEach(e => e.VesselChangedSoi());
        }

        void OnPartDeCoupleComplete (Part part)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnPartDeCoupleComplete");

            ResourceOvermanager.Reset();
            SupplyPriorityManager.Reset();
            FNRadiator.Reset();

            ResetReceivers();
        }

        void OnVesselDestroy(Vessel vessel)
        {
            ResourceOvermanager.ResetForVessel(vessel);
        }

        private static void ResetReceivers()
        {
            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                if (!currentVessel.loaded) continue;

                var receivers = currentVessel.FindPartModulesImplementing<BeamedPowerReceiver>();

                foreach (var receiver in receivers)
                {
                    Debug.Log("[KSPI]: OnDockingComplete - Restart receivers " + receiver.Part.name);
                    receiver.Restart(50);
                }
            }
        }
    }
}
