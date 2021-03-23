using FNPlugin.Powermanagement.Interfaces;

namespace FNPlugin.Reactors
{
    [KSPModule("Fission Fragment Reactor")]
    class InterstellarFissionDP : InterstellarFissionPB, IFNChargedParticleSource
    {
        public double MaximumChargedIspMult => (float)maximumChargedIspMult;

        public double MinimumChargdIspMult => (float)minimumChargdIspMult;
    }
}
