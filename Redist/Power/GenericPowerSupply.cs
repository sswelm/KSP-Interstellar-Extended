namespace FNPlugin.Power
{
    [KSPModule("Power Supply")]
    class GenericPowerSupply : PartModule, IPowerSupply
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_GenericPowerSupply_Proces")]//Proces
        public string displayName = "";

        protected ResourceBuffers resourceBuffers;
        protected double currentPowerSupply;

        public int PowerPriority { get; set;  }

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            displayName = part.partInfo.title;

            if (state == StartState.Editor) return;

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig("Megajoules"));
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig("ElectricCharge", 1000));
            resourceBuffers.Init(this.part);

            UnityEngine.Debug.Log("[KSPI]: GenericPowerSupply on " + part.name + " was Force Activated");
            this.part.force_activate();
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
            currentPowerSupply += supply;

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

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            resourceBuffers.UpdateVariable("Megajoules", currentPowerSupply);
            resourceBuffers.UpdateVariable("ElectricCharge", currentPowerSupply);
            resourceBuffers.UpdateBuffers();

            currentPowerSupply = 0;
        }
    }
}
