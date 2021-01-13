using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Constants;

namespace FNPlugin.Extensions
{
    public static class Vector3dExtension
    {
        // Calculate DeltaV vector and update resource demand from mass (demandMass)
        public static Vector3d CalculateDeltaVV(this Vector3d thrustDirection, double totalMass, float deltaTime, double thrust, double isp, out double demandMass)
        {
            // Mass flow rate
            var massFlowRate = thrust / (isp * PhysicsGlobals.GravitationalAcceleration);
            // Change in mass over time interval dT
            var dm = massFlowRate * deltaTime;
            // Resource demand from propellants with mass
            demandMass = dm;
            // Mass at end of time interval dT
            var finalMass = totalMass - dm;
            // deltaV amount
            var deltaV = finalMass > 0 && totalMass > 0
                ? isp * PhysicsGlobals.GravitationalAcceleration * Math.Log(totalMass / finalMass)
                : 0;

            // Return deltaV vector
            return deltaV * thrustDirection;
        }

        public static bool LineOfSightToSun(this Vector3d vesselPosition, Vector3d starPosition)
        {
            Vector3d bminusa = starPosition - vesselPosition;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                if (referenceBody.flightGlobalsIndex == 0)
                { // the sun should not block line of sight to the sun
                    continue;
                }

                Vector3d refminusa = referenceBody.position - vesselPosition;

                if (Vector3d.Dot(refminusa, bminusa) <= 0)
                    continue;

                var normalizedBminusa = bminusa.normalized;

                var cosReferenceSunNormB = Vector3d.Dot(refminusa, normalizedBminusa);

                if (cosReferenceSunNormB >= bminusa.magnitude)
                    continue;

                Vector3d tang = refminusa - cosReferenceSunNormB * normalizedBminusa;
                if (tang.magnitude < referenceBody.Radius)
                    return false;
            }
            return true;
        }
    }
}
