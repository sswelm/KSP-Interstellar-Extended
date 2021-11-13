
namespace FNPlugin.Propulsion
{
    public interface IFNEngineNoozle : IEngineNoozle
    {
        Part part { get; }

        double RequestedThrottle { get; }

        bool IsPlasmaNozzle { get; }
        bool RequiresPlasmaHeat { get; }
        bool RequiresThermalHeat { get; }
		bool PropellantAbsorbsNeutrons { get; }
    }
}
