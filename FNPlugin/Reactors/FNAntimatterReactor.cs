using System;
using FNPlugin.Powermanagement;

namespace FNPlugin.Reactors
{
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : InterstellarReactor, IFNChargedParticleSource
    {
        [KSPField]
        public double magneticNozzlePowerMult = 0.1;
        [KSPField]
        public double maximumChargedIspMult = 100;
        [KSPField]
        public double minimumChargdIspMult = 10;
        [KSPField]
        public double chargedProductMult = 1;
        [KSPField]
        public double chargedProductExp = 0;

        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Antimatter Reactor"; } }

        public override double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct * chargedProductMult * Math.Pow(massDifference, chargedProductExp) : 0; } }

        public double MaximumChargedIspMult { get { return maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return minimumChargdIspMult; } }

        public override double MagneticNozzlePowerMult { get { return magneticNozzlePowerMult; } }
    }
}
