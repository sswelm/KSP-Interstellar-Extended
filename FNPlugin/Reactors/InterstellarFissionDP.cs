using FNPlugin.Powermanagement;
using FNPlugin.Powermanagement.Interfaces;

namespace FNPlugin.Reactors
{
    [KSPModule("Fission Fragment Reactor")]
    class InterstellarFissionDP : InterstellarFissionPB, IFNChargedParticleSource
    {
        public double MaximumChargedIspMult { get { return (float)maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return (float)minimumChargdIspMult; } }
    }
}
