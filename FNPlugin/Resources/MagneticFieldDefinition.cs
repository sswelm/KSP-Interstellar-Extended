using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Resources
{
    class MagneticFieldDefinition
    {
        public MagneticFieldDefinition (string celestialBodyName, double strengthMult )
        {
            CelestialBodyName = celestialBodyName;
            StrengthMult = strengthMult;
        }

        public string CelestialBodyName { get; set; }
        public double StrengthMult { get; set; }
    }
}
