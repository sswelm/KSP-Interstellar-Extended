using FNPlugin.Redist;

namespace FNPlugin.Powermanagement.Interfaces
{
    interface IFNElectricPowerGeneratorSource: IElectricPowerGeneratorSource
    {
        double GetHotBathTemperature(double coldBathTemperature);

        double RawGeneratorSourcePower { get; }

        double MaxEfficiency { get; }
    }
}
