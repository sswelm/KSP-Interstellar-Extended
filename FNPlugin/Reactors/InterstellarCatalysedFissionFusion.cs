using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    [KSPModule("Antimatter Initiated Reactor")]
    class InterstellarCatalysedFissionFusion : InterstellarReactor, IChargedParticleSource
    {
        public double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct : 0; } }

        public override bool IsFuelNeutronRich { get { return CurrentFuelMode != null ? !CurrentFuelMode.Aneutronic : false; } }

        public float MaximumChargedIspMult { get { return 1f; } }

        public float MinimumChargdIspMult { get { return 100; } }

    }
}