using FNPlugin.Powermanagement.Interfaces;

namespace FNPlugin.Reactors
{
    [KSPModule("Antimatter Initiated Reactor")]
    class InterstellarCatalysedFissionFusion : InterstellarReactor, IFNChargedParticleSource
    {
        public override bool IsFuelNeutronRich => CurrentFuelMode != null ? !CurrentFuelMode.Aneutronic : false;

        public double MaximumChargedIspMult => 1;

        public double MinimumChargdIspMult => 100;
    }
}
