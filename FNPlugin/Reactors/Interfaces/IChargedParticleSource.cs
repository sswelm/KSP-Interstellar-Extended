
namespace FNPlugin
{
    public interface IChargedParticleSource : IPowerSource
    {
        double CurrentMeVPerChargedProduct { get; }

        double  UseProductForPropulsion(double ratio, double consumedAmount);

        double MaximumChargedIspMult { get; }

        double MinimumChargdIspMult { get; }
    }
}
