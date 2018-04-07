namespace FNPlugin
{
    public class ThermalPowerTransport : PartModule
    {
        [KSPField(isPersistant = true,  guiActive = false, guiActiveEditor = true, guiName = "Thermal Transport Cost")]
        public double thermalCost = 0.5;
    }
}
