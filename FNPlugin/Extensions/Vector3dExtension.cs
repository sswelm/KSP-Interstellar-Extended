using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public static class Vector3dExtension
    {
        // Calculate DeltaV vector and update resource demand from mass (demandMass)
        public static Vector3d CalculateDeltaVV(this Vector3d thrustDirection, float totalMass, float deltaTime, double thrust, double isp, out double demandMass)
        {
            // Mass flow rate
            var massFlowRate = thrust / (isp * GameConstants.STANDARD_GRAVITY);
            // Change in mass over time interval dT
            var dm = massFlowRate * deltaTime;
            // Resource demand from propellants with mass
            demandMass = dm;
            // Mass at end of time interval dT
            var finalMass = totalMass - dm;
            // deltaV amount
            var deltaV = finalMass > 0 && totalMass > 0
                ? isp * GameConstants.STANDARD_GRAVITY * Math.Log(totalMass / finalMass)
                : 0;

            // Return deltaV vector
            return deltaV * thrustDirection;
        }
    }
}
