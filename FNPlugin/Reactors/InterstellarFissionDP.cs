using System;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Fission Reactor")]
    class InterstellarFissionDP : InterstellarFissionPB, IChargedParticleSource
    {
        public double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct : 0; } }

        public double MaximumChargedIspMult { get { return (float)maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return (float)minimumChargdIspMult; } }
    }
}