using System.Collections;
using System;
using UnityEngine;

namespace PersistentRotation
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class Main : MonoBehaviour
    {
        public static Main instance { get; private set; }
        private Data data;
        public const float threshold = 0.05f;
        public Vessel activeVessel;

        /* MONOBEHAVIOUR METHODS */
        private void Awake()
        {
            instance = this;

            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
            GameEvents.onVesselWillDestroy.Add(OnVesselWillDestroy);
            GameEvents.onGameStateSave.Add(OnGameStateSave);
        }
        private void Start()
        {
            MechJebWrapper.Initialize();
            RemoteTechWrapper.Initialize();

            activeVessel = FlightGlobals.ActiveVessel;
            data = Data.instance;
        }
        private void FixedUpdate()
        {
            if (activeVessel != FlightGlobals.ActiveVessel)
            {
                activeVessel = FlightGlobals.ActiveVessel;
                Interface.instance.desiredRPMstr = data.FindPRVessel(activeVessel).desiredRPM.ToString();
            }

            foreach (Data.PRVessel v in data.PRVessels)
            {
                v.processed = false;
            }

            #region ### Cycle through all vessels ###
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                Data.PRVessel v = data.FindPRVessel(vessel);

                v.processed = true;
                if (v.dynamicReference)
                {
                    if (v.reference == null || (v.reference.GetType() != typeof(CelestialBody) || v.reference.GetName() != vessel.mainBody.GetName())) //Main body mode; continuous update of reference to mainBody
                    {
                        Debug.Log("[PR] Updated the reference of " + v.vessel.vesselName + " from " + (v.reference != null ? v.reference.GetName() : "Null") + " to " + vessel.mainBody.name);
                        v.reference = vessel.mainBody;
                        v.direction = (v.reference.GetTransform().position - vessel.transform.position).normalized;
                        v.rotation = vessel.transform.rotation;
                        v.planetariumRight = Planetarium.right;
                        v.lastActive = false;
                    }
                }
                if (vessel.packed)
                {
                    #region ### PACKED ###
                    if (vessel.loaded) //is okay, rotation doesnt need to be persistent when rotating
                    {
                        if (v.momentum.magnitude >= threshold)
                        {
                            PackedSpin(v);
                        }
                        else if (GetStabilityMode(vessel) != StabilityMode.ABSOLUTE)
                        {
                            if (GetStabilityMode(vessel) == StabilityMode.RELATIVE && v.rotationModeActive && !v.momentumModeActive && v.momentum.magnitude < threshold)
                            {
                                if (v.rotationModeActive == true && v.reference != null)
                                {
                                    if (v.reference == v.lastReference)
                                    {
                                        PackedRotation(v);
                                    }
                                }
                            }
                            else
                            {
                                PackedSpin(v);
                            }
                        }
                    }

                    v.lastActive = false;

                    #endregion
                }
                else
                {
                    #region ### UNPACKED ###
                    //Update Momentum when unpacked
                    if (GetStabilityMode(vessel) != StabilityMode.OFF && !v.momentumModeActive && vessel.angularVelocity.magnitude < threshold) //C1
                    {
                        v.momentum = Vector3.zero;
                    }
                    else
                    {
                        v.momentum = vessel.angularVelocity;
                    }
                    //Update mjMode when unpacked
                    v.mjMode = MechJebWrapper.GetMode(vessel);
                    v.rtMode = RemoteTechWrapper.GetMode(vessel);

                    //Apply Momentum to activeVessel using Fly-By-Wire
                    if (GetStabilityMode(vessel) == StabilityMode.RELATIVE && v.momentumModeActive) //C1 \ IsControllable
                    {
                        float desiredRPM = (vessel.angularVelocity.magnitude * 60f * (1f / Time.fixedDeltaTime)) / 360f;
                        if (v.desiredRPM >= 0)
                        {
                            vessel.ctrlState.roll = Mathf.Clamp((v.desiredRPM - desiredRPM), -1f, +1f);
                        }
                        else
                        {
                            vessel.ctrlState.roll = -Mathf.Clamp((-v.desiredRPM - desiredRPM), -1f, +1f);
                        }
                    }

                    //Update rotation
                    v.rotation = vessel.transform.rotation;

                    v.planetariumRight = Planetarium.right;

                    //Adjust SAS for Relative Rotation
                    if (v.rotationModeActive && v.reference != null) //C2
                    {
                        //Update direction
                        v.direction = (v.reference.GetTransform().position - vessel.transform.position).normalized;
                        if (GetStabilityMode(vessel) == StabilityMode.RELATIVE && !v.momentumModeActive)
                        {
                            if (v.lastActive && v.reference == v.lastReference)
                            {
                                AdjustSAS(v);
                            }
                            v.lastActive = true;
                        }
                        else
                        {
                            v.lastActive = false;
                        }

                        v.lastPosition = (Vector3d)v.lastTransform.position - v.reference.GetTransform().position;
                    }
                    else
                    {
                        v.direction = Vector3.zero;
                        v.lastPosition = Vector3.zero;
                        v.lastActive = false;
                    }
                    #endregion
                }

                v.lastTransform = vessel.ReferenceTransform;
                v.lastReference = v.reference;
            }
            #endregion

            data.PRVessels.RemoveAll(v => v.processed == false);
        }
        private void OnDestroy()
        {
            instance = null;
            //Unbind functions from GameEvents
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
            GameEvents.onVesselWillDestroy.Remove(OnVesselWillDestroy);
            GameEvents.onGameStateSave.Remove(OnGameStateSave);
        }

        /* EVENT METHODS */
        private void OnGameStateSave(ConfigNode config)
        {
            if (data)
            {
                data.Save();
            }
        }
        private void OnVesselWillDestroy(Vessel vessel)
        {
            Debug.Log("[PR] Deleting " + vessel.vesselName + " as reference.");

            foreach (Vessel _vessel in FlightGlobals.Vessels)
            {
                Data.PRVessel v = data.FindPRVessel(_vessel);
                if (!object.ReferenceEquals(_vessel, vessel))
                {
                    if (object.ReferenceEquals(vessel, v.reference))
                    {
                        v.reference = null;
                    }
                }
            }
        }
        private void OnVesselGoOnRails(Vessel vessel)
        {
            //Nothing to do here
        }
        private void OnVesselGoOffRails(Vessel vessel)
        {
            Data.PRVessel v = data.FindPRVessel(vessel);
            if (vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.SPLASHED && vessel.situation != Vessel.Situations.PRELAUNCH)
            {
                if (v.rotationModeActive && v.momentum.magnitude > threshold)
                {
                    ApplyMomentum(v);
                }
                else if (GetStabilityMode(vessel) != StabilityMode.ABSOLUTE)
                {
                    if (GetStabilityMode(vessel) == StabilityMode.RELATIVE && !v.momentumModeActive)
                    {

                        Quaternion shift = Quaternion.Euler(0f, Vector3.Angle(Planetarium.right, v.planetariumRight), 0f);

                        //Set relative rotation if there is a reference
                        if (v.reference != null)
                        {
                            vessel.SetRotation(FromToRotation(shift * v.direction, (v.reference.GetTransform().position - vessel.transform.position).normalized) * (shift * v.rotation), true);
                        }

                        //Reset momentumModeActive heading
                        vessel.Autopilot.SAS.lockedRotation = vessel.ReferenceTransform.rotation;
                    }
                    else
                    {
                        ApplyMomentum(v);
                    }
                }
            }
        }

        /* PRIVATE METHODS */
        private void PackedSpin(Data.PRVessel v)
        {

            if (v.vessel.situation != Vessel.Situations.LANDED && v.vessel.situation != Vessel.Situations.SPLASHED && v.vessel.situation != Vessel.Situations.PRELAUNCH)
                v.vessel.SetRotation(Quaternion.AngleAxis(v.momentum.magnitude * TimeWarp.CurrentRate, v.vessel.ReferenceTransform.rotation * v.momentum) * v.vessel.transform.rotation, true);
        }
        private void PackedRotation(Data.PRVessel v)
        {
            Quaternion shift = Quaternion.Euler(0f, Vector3.Angle(Planetarium.right, v.planetariumRight), 0f);
            if (v.vessel.situation != Vessel.Situations.LANDED && v.vessel.situation != Vessel.Situations.SPLASHED && v.vessel.situation != Vessel.Situations.PRELAUNCH)
                v.vessel.SetRotation(FromToRotation(shift * v.direction, (v.reference.GetTransform().position - v.vessel.transform.position).normalized) * (shift * v.rotation), true);
        }
        private void AdjustSAS(Data.PRVessel v)
        {
            if (v.reference != null)
            {
                if (v.lastTransform != null && v.lastPosition != null)
                {
                    Vector3d newPosition = (Vector3d)v.lastTransform.position - v.reference.GetTransform().position;
                    QuaternionD delta = FromToRotation(v.lastPosition, newPosition);
                    QuaternionD adjusted = delta * (QuaternionD)v.vessel.Autopilot.SAS.lockedRotation;
                    v.vessel.Autopilot.SAS.lockedRotation = adjusted;
                }
            }
        }
        private void ApplyMomentum(Data.PRVessel v)
        {
            Vector3 av = v.momentum;
            Vector3 COM = v.vessel.CoM;
            Quaternion rotation = v.vessel.ReferenceTransform.rotation;

            //Applying force on every part
            foreach (Part p in v.vessel.parts)
            {
                if (!p.GetComponent<Rigidbody>()) continue;
                p.GetComponent<Rigidbody>().AddTorque( rotation * av, ForceMode.VelocityChange );
                p.GetComponent<Rigidbody>().AddForce( Vector3.Cross(rotation * av, (p.transform.position - COM)), ForceMode.VelocityChange);
            }
        }

        /* UTILITY METHODS */
        private Quaternion FromToRotation(Vector3d fromv, Vector3d tov) //Stock FromToRotation() doesn't work correctly
        {
            Vector3d cross = Vector3d.Cross(fromv, tov);
            double dot = Vector3d.Dot(fromv, tov);
            double wval = dot + Math.Sqrt(fromv.sqrMagnitude * tov.sqrMagnitude);
            double norm = 1.0 / Math.Sqrt(cross.sqrMagnitude + wval * wval);
            return new QuaternionD(cross.x * norm, cross.y * norm, cross.z * norm, wval * norm);
        }
        private enum StabilityMode
        {
            OFF,
            ABSOLUTE,
            RELATIVE
        }
        private StabilityMode GetStabilityMode(Vessel vessel)
        {
            if (vessel.IsControllable == false || RemoteTechWrapper.Controllable(vessel) == false)
                return StabilityMode.OFF; /* Vessel is uncontrollable */
            else if (RemoteTechWrapper.GetMode(vessel) != RemoteTechWrapper.ACFlightMode.Off)
                return StabilityMode.ABSOLUTE;
            else if (MechJebWrapper.Active(vessel))
                return StabilityMode.ABSOLUTE; /* MechJeb is commanding the vessel */
            else if (vessel.ActionGroups[KSPActionGroup.SAS] && data.FindPRVessel(vessel).mjMode == MechJebWrapper.SATarget.OFF && MechJebWrapper.GetMode(vessel) == MechJebWrapper.SATarget.OFF)
            {
                /* Only stock SAS is enabled */

                if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.StabilityAssist)
                    return StabilityMode.RELATIVE;
                else
                    return StabilityMode.ABSOLUTE;
            }
            else if (!vessel.ActionGroups[KSPActionGroup.SAS] && (data.FindPRVessel(vessel).mjMode != MechJebWrapper.SATarget.OFF || MechJebWrapper.GetMode(vessel) != MechJebWrapper.SATarget.OFF))
                return StabilityMode.ABSOLUTE; /* Only SmartA.S.S. is enabled */
            else if (!vessel.ActionGroups[KSPActionGroup.SAS] && data.FindPRVessel(vessel).mjMode == MechJebWrapper.SATarget.OFF && MechJebWrapper.GetMode(vessel) == MechJebWrapper.SATarget.OFF)
                return StabilityMode.OFF; /* Nothing is enabled */
            else
                return StabilityMode.OFF;
        }
    }
}