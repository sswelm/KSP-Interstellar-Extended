using KSP.Localization;
using System;

namespace FNPlugin.Refinery
{
    class AntimatterFactory : ResourceSuppliableModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool isActive = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterFactory_powerPecentage"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 1)]
        public float powerPercentage = 100;

        [KSPField]
        public string activateTitle = "#LOC_KSPIE_AntimatterFactory_producePositron";

        [KSPField(isPersistant = true)]
        public double last_active_time = 0;
        [KSPField(isPersistant = true)]
        public double electrical_power_ratio;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_AntimatterFactory_productionRate")]
        public string productionRateTxt;

        [KSPField]
        public double productionRate;
        [KSPField]
        public double efficiencyMultiplier = 10;
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Maximum Power capacity", guiUnits = " MW")]
        public double powerCapacity = 1000;
        [KSPField]
        public string resourceName = "Positrons";

        AntimatterGenerator _generator;
        PartResourceDefinition _antimatterDefinition;
        string disabledText;

        public override void OnStart(StartState state)
        {
            _antimatterDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            _generator = new AntimatterGenerator(this.part, efficiencyMultiplier, _antimatterDefinition);

            if (state == StartState.Editor)
                return;

            disabledText = Localizer.Format("#LOC_KSPIE_AntimatterFactory_disabled");

            Fields["isActive"].guiName = Localizer.Format(activateTitle);

            if (!isActive)
                return;

            var deltaTime = Planetarium.GetUniversalTime() - last_active_time;

            var energy_provided_in_megajoules = electrical_power_ratio * powerCapacity * deltaTime;

            _generator.Produce(energy_provided_in_megajoules);
        }

        public override void OnUpdate()
        {
            if (!isActive)
            {
                productionRateTxt = disabledText;
                return;
            }

            last_active_time = Planetarium.GetUniversalTime();

            double antimatter_rate_per_day = productionRate * PluginHelper.SecondsInDay;

            if (antimatter_rate_per_day > 0.1)
            {
                if (antimatter_rate_per_day > 1000)
                    productionRateTxt = (antimatter_rate_per_day / 1e3).ToString("0.0000") + " g/day";
                else
                    productionRateTxt = (antimatter_rate_per_day).ToString("0.0000") + " mg/day";
            }
            else
            {
                if (antimatter_rate_per_day > 0.1e-3)
                    productionRateTxt = (antimatter_rate_per_day * 1e3).ToString("0.0000") + " ug/day";
                else
                    productionRateTxt = (antimatter_rate_per_day * 1e6).ToString("0.0000") + " ng/day";
            }
        }

        public void FixedUpdate()
        {
            if (!isActive)
                return;

            var availablePower = getAvailableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES);
            var resourceBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);
            var effectiveResourceThrotling = resourceBarRatio > ResourceManager.ONE_THIRD ? 1 : resourceBarRatio * 3;

            var energy_requested_in_megajoules_per_second = Math.Min(powerCapacity, effectiveResourceThrotling * availablePower * (double)(decimal)powerPercentage * 0.01);

            var energy_provided_in_megajoules_per_second = CheatOptions.InfiniteElectricity
                ? energy_requested_in_megajoules_per_second
                : consumeFNResourcePerSecond(energy_requested_in_megajoules_per_second, ResourceManager.FNRESOURCE_MEGAJOULES);

            electrical_power_ratio = energy_requested_in_megajoules_per_second > 0 ? energy_provided_in_megajoules_per_second / energy_requested_in_megajoules_per_second : 0;

            _generator.Produce(energy_provided_in_megajoules_per_second * (double)(decimal)TimeWarp.fixedDeltaTime);

            productionRate = _generator.ProductionRate;
        }
    }
}
