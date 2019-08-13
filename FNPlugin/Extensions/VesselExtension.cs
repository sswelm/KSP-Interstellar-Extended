using System;
using UnityEngine;

namespace FNPlugin.Extensions
{
    public static class VesselExtension
    {
        public static bool PersistHeading(this Vessel vessel, bool forceRotation = false)
        {
            var canPersistDirection = vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.ORBITING;
            var sasIsActive = vessel.ActionGroups[KSPActionGroup.SAS];

            if (!canPersistDirection || !sasIsActive) return true;

            var requestedDirection = Vector3d.zero;
            var universalTime = Planetarium.GetUniversalTime();
            var vesselPosition = vessel.orbit.getPositionAtUT(universalTime);

            switch (vessel.Autopilot.Mode)
            {
                case VesselAutopilot.AutopilotMode.Prograde:
                    requestedDirection = vessel.obt_velocity.normalized;
                    break;
                case VesselAutopilot.AutopilotMode.Retrograde:
                    requestedDirection = -vessel.obt_velocity.normalized;
                    break;
                case VesselAutopilot.AutopilotMode.Maneuver:
                    requestedDirection = vessel.patchedConicSolver.maneuverNodes.Count > 0 ? vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(vessel.orbit) : vessel.obt_velocity.normalized;
                    break;
                case VesselAutopilot.AutopilotMode.Target:
                    requestedDirection = (vessel.targetObject.GetOrbit().getPositionAtUT(universalTime) - vesselPosition).normalized;
                    break;
                case VesselAutopilot.AutopilotMode.AntiTarget:
                    requestedDirection = -(vessel.targetObject.GetOrbit().getPositionAtUT(universalTime) - vesselPosition).normalized;
                    break;
                case VesselAutopilot.AutopilotMode.Normal:
                    requestedDirection = Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position)).normalized;
                    break;
                case VesselAutopilot.AutopilotMode.Antinormal:
                    requestedDirection = -Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position)).normalized;
                    break;
                case VesselAutopilot.AutopilotMode.RadialIn:
                    requestedDirection = -Vector3.Cross(vessel.obt_velocity, Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position))).normalized;
                    break;
                case VesselAutopilot.AutopilotMode.RadialOut:
                    requestedDirection = Vector3.Cross(vessel.obt_velocity, Vector3.Cross(vessel.obt_velocity, (vesselPosition - vessel.mainBody.position))).normalized;
                    break;
            }

            if (requestedDirection == Vector3d.zero) return true;

            if (forceRotation || Vector3d.Dot(vessel.transform.up.normalized, requestedDirection) > 0.9)
            {
                vessel.transform.Rotate(Quaternion.FromToRotation(vessel.transform.up.normalized, requestedDirection).eulerAngles, Space.World);
                vessel.SetRotation(vessel.transform.rotation);
            }
            else
            {
                var directionName = Enum.GetName(typeof(VesselAutopilot.AutopilotMode), vessel.Autopilot.Mode);
                var message = "Persistant Thrust stopped - vessel is not facing " + directionName;
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
                return false;
            }
            return true;
        }
    }
}
