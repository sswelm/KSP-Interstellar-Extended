using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Refinery
{
    class AntimatterFactory : FNResourceSuppliableModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Producing"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool isActive = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Percentage"), UI_FloatRange(stepIncrement = 1f/3f, maxValue = 100, minValue = 1)]
        public float powerPercentage = 100;

        [KSPField(isPersistant = true)]
        public double last_active_time = 0;
        [KSPField(isPersistant = true, guiName = "Power Percentage")]
        public double electrical_power_ratio;

        [KSPField(guiActive = true, guiName = "Production Rate")]
        public string productionRateTxt;

        [KSPField]
        public double productionRate;
        [KSPField]
        public double efficiencyMultiplier = 10;
        [KSPField]
        public double powerCapacity = 1000;
        [KSPField]
        public string resourceName = "Positrons";

        AntimatterGenerator _generator;
        PartResourceDefinition _antimatterDefinition;

        public override void OnStart(StartState state)
        {
            _antimatterDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            _generator = new AntimatterGenerator(this.part, efficiencyMultiplier, _antimatterDefinition);

            if (state == StartState.Editor)
                return;

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
                productionRateTxt = "disabled";
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

            var availablePower = getAvailableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
            var resourceBarRatio = getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES);
            var effectiveResourceThrotling = resourceBarRatio > ORSResourceManager.ONE_THIRD ? 1 : resourceBarRatio * 3;

            var energy_requested_in_megajoules = Math.Min(powerCapacity, TimeWarp.fixedDeltaTime * effectiveResourceThrotling * availablePower * powerPercentage / 100d);

            var energy_provided_in_megajoules = CheatOptions.InfiniteElectricity
                ? energy_requested_in_megajoules
                : consumeFNResource(energy_requested_in_megajoules, FNResourceManager.FNRESOURCE_MEGAJOULES);

            electrical_power_ratio = energy_requested_in_megajoules > 0 ? energy_provided_in_megajoules / energy_requested_in_megajoules : 0; 

            _generator.Produce(energy_provided_in_megajoules);

            productionRate = _generator.ProductionRate / TimeWarp.fixedDeltaTime;
        }
    }
}
