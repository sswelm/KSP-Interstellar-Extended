using System;
using FNPlugin.Powermanagement;
using FNPlugin.Refinery;
using KSP.Localization;

namespace FNPlugin.Resources
{
    class AntimatterFactory : ResourceSuppliableModule
    {
        public const string Group = "Antimatter Factory";
        public const string GroupTitle = "Antimatter Factory";

        [KSPField(isPersistant = true)] public double lastActiveTime;
        [KSPField(isPersistant = true)] public double electricalPowerRatio;

        [KSPField(advancedTweakable = true, guiActive = false)] public double _availablePower;
        [KSPField(advancedTweakable = true, guiActive = false)] public double _resourceBarRatio;
        [KSPField(advancedTweakable = true, guiActive = false)] public double _effectiveResourceThrottling;

        [KSPField] public double productionRate;
        [KSPField] public double efficiencyMultiplier = 10;
        [KSPField] public string activateTitle = "#LOC_KSPIE_AntimatterFactory_producePositron";
        [KSPField] public string resourceName = "Positrons";

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, isPersistant = true), UI_Toggle(disabledText = "#LOC_KSPIE_AntimatterFactory_Off", enabledText = "#LOC_KSPIE_AntimatterFactory_On")]//OffOn
        public bool isActive = false;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterFactory_powerPecentage"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 1)]
        public float powerPercentage = 100;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_AntimatterFactory_productionRate")]
        public string productionRateTxt;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterFactory_MaximumPowercapacity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Maximum Power capacity
        public double powerCapacity = 1000;

        private AntimatterGenerator _generator;
        private PartResourceDefinition _antimatterDefinition;
        private string _disabledText;

        public override void OnStart(StartState state)
        {
            _antimatterDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            _generator = new AntimatterGenerator(part, efficiencyMultiplier, _antimatterDefinition);

            productionRate = _generator.ProductionRate;

            if (state == StartState.Editor)
                return;

            _disabledText = Localizer.Format("#LOC_KSPIE_AntimatterFactory_disabled");

            Fields[nameof(isActive)].guiName = Localizer.Format(activateTitle);

            if (!isActive)
                return;

            var deltaTime = Planetarium.GetUniversalTime() - lastActiveTime;

            _generator.Produce(electricalPowerRatio * powerCapacity * deltaTime);
        }

        public override void OnUpdate()
        {
            if (!isActive)
            {
                productionRateTxt = _disabledText;
                return;
            }

            lastActiveTime = Planetarium.GetUniversalTime();

            double antimatterRatePerDay = productionRate * PluginSettings.Config.SecondsInDay;

            if (antimatterRatePerDay > 0.1)
            {
                if (antimatterRatePerDay > 1000)
                    productionRateTxt = (antimatterRatePerDay / 1e3).ToString("0.0000") + " g/" + Localizer.Format("#LOC_KSPIE_AntimatterFactory_perday");//day
                else
                    productionRateTxt = (antimatterRatePerDay).ToString("0.0000") + " mg/" + Localizer.Format("#LOC_KSPIE_AntimatterFactory_perday");//day
            }
            else
            {
                if (antimatterRatePerDay > 0.1e-3)
                    productionRateTxt = (antimatterRatePerDay * 1e3).ToString("0.0000") + " ug/" + Localizer.Format("#LOC_KSPIE_AntimatterFactory_perday");//day
                else
                    productionRateTxt = (antimatterRatePerDay * 1e6).ToString("0.0000") + " ng/" + Localizer.Format("#LOC_KSPIE_AntimatterFactory_perday");//day
            }
        }

        public void FixedUpdate()
        {
            if (!isActive)
                return;

            _availablePower = GetAvailableStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt);
            _resourceBarRatio = GetResourceBarRatio(ResourceSettings.Config.ElectricPowerInMegawatt);
            _effectiveResourceThrottling = _resourceBarRatio > 1d/3d ? 1 : _resourceBarRatio * 3;

            var energyRequestedInMegajoulesPerSecond = powerPercentage * 0.01 * Math.Min(powerCapacity, _effectiveResourceThrottling * _availablePower);

            var energyProvidedInMegajoulesPerSecond = CheatOptions.InfiniteElectricity
                ? energyRequestedInMegajoulesPerSecond
                : ConsumeFnResourcePerSecond(energyRequestedInMegajoulesPerSecond, ResourceSettings.Config.ElectricPowerInMegawatt);

            electricalPowerRatio = energyRequestedInMegajoulesPerSecond > 0 ? energyProvidedInMegajoulesPerSecond / energyRequestedInMegajoulesPerSecond : 0;

            _generator.Produce(energyProvidedInMegajoulesPerSecond * (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7));

            productionRate = _generator.ProductionRate;
        }
    }
}
