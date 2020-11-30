namespace FNPlugin.Reactors
{
    public class ThermalPowerTransport : PartModule
    {
        [KSPField(isPersistant = true,  guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalPowerTransport_ThermalTransportCost")]//Thermal Transport Cost
        public double thermalCost = 0.5;
    }
}
