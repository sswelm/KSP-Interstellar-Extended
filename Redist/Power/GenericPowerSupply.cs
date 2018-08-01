namespace FNPlugin.Refinery
{
    [KSPModule("Power Supply")]
    class GenericPowerSupply : PartModule, IPowerSupply
    {
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Proces")]
        public string displayName = "";

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            displayName = part.partInfo.title;
        }

        public double ConsumeMegajoulesFixed(double powerRequest, double fixedDeltaTime)
        {
            return part.RequestResource("ElectricCharge", powerRequest * fixedDeltaTime);
        }

        public double ConsumeMegajoulesPerSecond(double powerRequest)
        {
            return part.RequestResource("ElectricCharge", powerRequest * TimeWarp.fixedDeltaTime);
        }

        public void SupplyMegajoulesPerSecondWithMax(double supply, double maxsupply)
        {
            part.RequestResource("ElectricCharge", -supply * TimeWarp.fixedDeltaTime);
        }

        public void SupplyMegajoulesFixedWithMax(double supply, double maxsupply)
        {
            part.RequestResource("ElectricCharge", -supply * TimeWarp.fixedDeltaTime);
        }

        public string getResourceManagerDisplayName()
        {
            return displayName;
        }

        public override string GetInfo()
        {
            return displayName;
        }

        public int getPowerPriority()
        {
            return 4;
        }
    }
}
