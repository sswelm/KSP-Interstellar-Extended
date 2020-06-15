using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public static class OrbitExtensions
    {
        // Perturb an orbit by a deltaV vector
        public static void Perturb(this Orbit orbit, Vector3d deltaVV, double universalTime)
        {
            if (deltaVV.magnitude == 0)
                return;

            // Transpose deltaVV Y and Z to match orbit frame
            Vector3d deltaVV_orbit = deltaVV.xzy;

            // Position vector
            Vector3d position = orbit.getRelativePositionAtUT(universalTime);

            // Update with current position and new velocity
            orbit.UpdateFromStateVectors(position, orbit.getOrbitalVelocityAtUT(universalTime) + deltaVV_orbit, orbit.referenceBody, universalTime);
            orbit.Init();
            orbit.UpdateFromUT(universalTime);
        }
    }

}

