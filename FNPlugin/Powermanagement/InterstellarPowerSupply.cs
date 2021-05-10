using System;
using FNPlugin.Power;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    [KSPModule("Power Supply")]
    class InterstellarPowerSupply : ResourceSuppliableModule, IPowerSupply
    {
        [KSPField(isPersistant = true)]
        public bool isActivated;

        public const string Group = "PowerSupply";
        public const string GroupTitle = "PowerSupply";

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_InterstellarPowerSupply_TotalPowerSupply", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Total Power Supply
        public double totalPowerSupply;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiName = "#LOC_KSPIE_InterstellarPowerSupply_Proces")]//Proces
        public string displayName = "";
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_InterstellarPowerSupply_PowerPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float powerPriority = 4;

        protected ResourceBuffers resourceBuffers;
        protected double currentPowerSupply;

        public int PowerPriority
        {
            get => (int)Math.Round(powerPriority, 0);
            set => powerPriority = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public override void OnStart(StartState state)
        {
            displayName = part.partInfo.title;
            resourcesToSupply = new [] { ResourceSettings.Config.ElectricPowerInMegawatt };

            if (state == StartState.Editor) return;

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricPowerInMegawatt));
            if (!Kerbalism.IsLoaded)
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricPowerInKilowatt, 1000));
            resourceBuffers.Init(part);
        }

        private void Activate()
        {
            if (isActivated)
                return;

            Debug.Log("[KSPI]: PowerSupply on " + part.name + " was Force Activated");
            this.part.force_activate();

            isActivated = true;
        }

        public double ConsumeMegajoulesFixed(double powerRequest, double fixedDeltaTime)
        {
            if (powerRequest > 0)
                Activate();

            return consumeFNResource(powerRequest, ResourceSettings.Config.ElectricPowerInMegawatt, fixedDeltaTime);
        }

        public double ConsumeMegajoulesPerSecond(double powerRequest)
        {
            if (powerRequest > 0)
                Activate();

            return ConsumeFnResourcePerSecond(powerRequest, ResourceSettings.Config.ElectricPowerInMegawatt);
        }

        public void SupplyMegajoulesPerSecondWithMax(double supply, double maxsupply)
        {
            if (supply > 0)
                Activate();

            currentPowerSupply += supply;

            SupplyFnResourcePerSecondWithMax(supply, maxsupply, ResourceSettings.Config.ElectricPowerInMegawatt);
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
            return PowerPriority;
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            totalPowerSupply = GetCurrentResourceSupply(ResourceSettings.Config.ElectricPowerInMegawatt);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, currentPowerSupply);
            if (!Kerbalism.IsLoaded)
                resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, currentPowerSupply);
            resourceBuffers.UpdateBuffers();

            currentPowerSupply = 0;
        }
    }
}
