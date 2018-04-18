using FNPlugin.Reactors.Interfaces;

namespace FNPlugin.Reactors
{
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : InterstellarReactor, IChargedParticleSource
    {
        [KSPField]
        public double magneticNozzlePowerMult = 0.1;
        [KSPField]
        public double maximumChargedIspMult = 100;
        [KSPField]
        public double minimumChargdIspMult = 10;


        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Antimatter Reactor"; } }

        public double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct : 0; } }

        public double MaximumChargedIspMult { get { return maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return minimumChargdIspMult; } }

        public double MagneticNozzlePowerMult { get { return magneticNozzlePowerMult; } }
    }
}