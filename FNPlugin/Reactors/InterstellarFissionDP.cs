using FNPlugin.Redist;

namespace FNPlugin.Reactors
{
    [KSPModule("Fission Fragment Reactor")]
    class InterstellarFissionDP : InterstellarFissionPB, IChargedParticleSource
    {
        public double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct : 0; } }

        public double MaximumChargedIspMult { get { return (float)maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return (float)minimumChargdIspMult; } }

        public double MagneticNozzlePowerMult { get { return 1; } }
    }
}