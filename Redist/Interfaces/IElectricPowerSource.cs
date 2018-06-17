namespace FNPlugin.Redist
{
    public interface IElectricPowerGeneratorSource
    {
        double MaxStableMegaWattPower { get; }
        void Refresh();
        void FindAndAttachToPowerSource();
    }
}
