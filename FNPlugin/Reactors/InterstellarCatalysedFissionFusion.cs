
using FNPlugin.Reactors.Interfaces;

namespace FNPlugin.Reactors
{
    [KSPModule("Antimatter Initiated Reactor")]
    class InterstellarCatalysedFissionFusion : InterstellarReactor, IChargedParticleSource
    {
        public double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct : 0; } }

        public override bool IsFuelNeutronRich { get { return CurrentFuelMode != null ? !CurrentFuelMode.Aneutronic : false; } }

        public double MaximumChargedIspMult { get { return 1; } }

		public double MinimumChargdIspMult { get { return 100; } }

    }
}