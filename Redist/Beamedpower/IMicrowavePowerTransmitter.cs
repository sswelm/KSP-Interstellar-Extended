
namespace FNPlugin.Beamedpower
{
    public interface IMicrowavePowerTransmitter { };

    public interface IVesselMicrowavePersistence
    {
        double getAvailablePowerInKW();

        bool IsActive { get; }
    }
    public interface IVesselRelayPersistence
    {
        bool IsActive { get; }
    }
}
