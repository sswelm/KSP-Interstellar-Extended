using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Extensions;

namespace FNPlugin.Powermanagement
{
    class FNBatteryGenerator : ResourceSuppliableModule
    {
        // configuration
        [KSPField(guiActiveEditor = true, guiName = "Maximum Power", guiUnits = " MW", guiFormat = "F3")]
        public double maxPower = 1;
        [KSPField]
        public string inputResources = "KilowattHour";
        [KSPField]
        public string outputResources;
        [KSPField]
        public string inputConversionRates = "2.77777777777e-1";
        [KSPField]
        public string outputConversionRates = "";


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricPowerPriority = 4;

        // debugging
        [KSPField(guiActive = false, guiName = "Power Demand" , guiUnits = " MW", guiFormat = "F3")]
        public double currentUnfilledResourceDemand;
        [KSPField(guiActive = false, guiName = "Spare MW Capacity",  guiUnits = " MW", guiFormat = "F3")]
        public double spareResourceCapacity;
        [KSPField(guiActive = true, guiName = "Battery Time remaining ", guiUnits = " s", guiFormat = "F0")]
        public double batterySupplyRemaining;
        [KSPField(guiActive = true, guiName = "Battery consumption", guiUnits = " KWh/s", guiFormat = "F3")]
        public double requestedConsumptionRate;

        [KSPField(guiActiveEditor = true, guiName = "Maximum Power", guiUnits = " MW", guiFormat = "F3")]
        public double currentMaxPower = 1;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = " MW", guiFormat = "F3")]
        public double electrical_power_currently_needed;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_powerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerPercentage = 100;

        // privates
        List<string> inputResourceNames;
        List<string> outputResourceNames;

        List<double> inputResourceRate;
        List<double> outputResourceRate;

        double fuelRatio;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) return;

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            part.force_activate();

            inputResourceNames = ParseTools.ParseNames(inputResources);
            outputResourceNames = ParseTools.ParseNames(outputResources);
            inputResourceRate = ParseTools.ParseDoubles(inputConversionRates);
            outputResourceRate = ParseTools.ParseDoubles(outputConversionRates);
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {

            currentMaxPower = Math.Round(powerPercentage * 0.01, 2) * maxPower;
            currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES));
            spareResourceCapacity = getSpareResourceCapacity(ResourceManager.FNRESOURCE_MEGAJOULES);
            electrical_power_currently_needed = Math.Min(currentUnfilledResourceDemand + spareResourceCapacity, currentMaxPower);

            batterySupplyRemaining = double.MaxValue;
            fuelRatio = double.MaxValue;

            for (var i = 0; i < inputResourceNames.Count; i++)
            {
                var currentResourceName = inputResourceNames[i];
                var currentConversionRate = inputResourceRate[i];

                var currentBatteryResource = part.Resources[currentResourceName];
                var currentRequestedConsumptionRate = electrical_power_currently_needed * currentConversionRate;
                var currentFixedConsumption = currentRequestedConsumptionRate * fixedDeltaTime;
                var currentFuelRatio = currentFixedConsumption > 0 ? currentBatteryResource.amount / currentFixedConsumption : 0;

                if (currentFuelRatio < fuelRatio)
                    fuelRatio = currentFuelRatio;

                var currentBatterySupplyRemaining = currentFuelRatio / fixedDeltaTime / 1000;
                if (currentBatterySupplyRemaining < batterySupplyRemaining)
                    batterySupplyRemaining = currentBatterySupplyRemaining;

                currentBatteryResource.amount = Math.Max(0, currentBatteryResource.amount - currentFixedConsumption);
            }

            supplyFNResourcePerSecondWithMaxAndEfficiency(Math.Min(1, fuelRatio) * electrical_power_currently_needed, currentMaxPower * Math.Min(1, fuelRatio), 1, ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        public override int getPowerPriority()
        {
            return 4;
        }

        public override int getSupplyPriority()
        {
            return (int)electricPowerPriority;
        }
    }
}
