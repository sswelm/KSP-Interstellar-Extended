namespace FNPlugin.Reactors.Interfaces
{
    public interface IElectricPowerGeneratorSource
    {
        double MaxStableMegaWattPower { get; }
        void Refresh();
        void FindAndAttachToPowerSource();
    }
}
