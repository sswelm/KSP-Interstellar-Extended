using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : InterstellarReactor, IChargedParticleSource
    {
        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Antimatter Reactor"; } }

        public double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct : 0; } }

        public float MaximumChargedIspMult { get { return 100f; } }

        public float MinimumChargdIspMult { get { return 1; } }
    }
}