
namespace FNPlugin.Reactors.Interfaces
{
    public interface IChargedParticleSource : IPowerSource
    {
        double CurrentMeVPerChargedProduct { get; }

        void UseProductForPropulsion(double ratio, double propellantMassPerSecond);

        double MaximumChargedIspMult { get; }

        double MinimumChargdIspMult { get; }

        double MagneticNozzlePowerMult { get; }
    }
}
