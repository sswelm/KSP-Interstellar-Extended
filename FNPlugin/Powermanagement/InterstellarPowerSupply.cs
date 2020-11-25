using FNPlugin.Resources;
using System;
using UnityEngine;

namespace FNPlugin.Power
{
    [KSPModule("Power Supply")]
    class InterstellarPowerSupply : ResourceSuppliableModule, IPowerSupply
    {
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_InterstellarPowerSupply_TotalPowerSupply", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Total Power Supply
        public double totalPowerSupply;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_InterstellarPowerSupply_Proces")]//Proces
        public string displayName = "";
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_InterstellarPowerSupply_PowerPriority")]//Power Priority
        public int powerPriority = 4;

        protected ResourceBuffers resourceBuffers;
        protected double currentPowerSupply;

        public int PowerPriority
        {
            get { return powerPriority; }
            set { powerPriority = value; }
        }

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            displayName = part.partInfo.title;
            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;

            if (state == StartState.Editor) return;

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES));
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricChargePower, 1000));
            resourceBuffers.Init(this.part);

            Debug.Log("[KSPI]: PowerSupply on " + part.name + " was Force Activated");
            this.part.force_activate();
        }

        public double ConsumeMegajoulesFixed(double powerRequest, double fixedDeltaTime)
        {
            return consumeFNResource(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES, fixedDeltaTime);
        }

        public double ConsumeMegajoulesPerSecond(double powerRequest)
        {
            return consumeFNResourcePerSecond(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        public void SupplyMegajoulesPerSecondWithMax(double supply, double maxsupply)
        {
            currentPowerSupply += supply;

            supplyFNResourcePerSecondWithMax(supply, maxsupply, ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        public override string getResourceManagerDisplayName()
        {
            return displayName;
        }

        public override string GetInfo()
        {
            return displayName;
        }

        public override int getPowerPriority()
        {
            return powerPriority;
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            totalPowerSupply = getCurrentResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, currentPowerSupply);
            resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricChargePower, currentPowerSupply);
            resourceBuffers.UpdateBuffers();

            currentPowerSupply = 0;
        }
    }
}
