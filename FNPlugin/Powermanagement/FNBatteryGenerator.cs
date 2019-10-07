using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Powermanagement
{
    class FNBatteryGenerator : ResourceSuppliableModule
    {
        // configuration
        [KSPField(guiActiveEditor = true, guiName = "Maximum Power", guiUnits = " MW", guiFormat = "F3")]
        public double maxPower = 1;
        [KSPField]
        public double conversionRate = 2.77777777777e-1;
        [KSPField]
        public string resourceName = "KilowattHour";

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

        PartResource batteryResource;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) return;

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            part.force_activate();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            batteryResource = part.Resources[resourceName];

            if (batteryResource == null || batteryResource.amount == 0)
            {
                currentMaxPower = 0;
                currentUnfilledResourceDemand = 0;
                spareResourceCapacity = 0;
                requestedConsumptionRate = 0;
                batterySupplyRemaining = 0;
                return;
            }

            currentMaxPower = Math.Round(powerPercentage * 0.01, 2) * maxPower;

            currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES));
            spareResourceCapacity = getSpareResourceCapacity(ResourceManager.FNRESOURCE_MEGAJOULES);
            electrical_power_currently_needed = Math.Min(currentUnfilledResourceDemand + spareResourceCapacity, currentMaxPower);

            requestedConsumptionRate = electrical_power_currently_needed * conversionRate;

            var fixedConsumption = requestedConsumptionRate * fixedDeltaTime;
            var fuelRatio = fixedConsumption > 0 ? batteryResource.amount / fixedConsumption : 0;

            batterySupplyRemaining = fuelRatio / fixedDeltaTime / 1000;

            batteryResource.amount = Math.Max(0, batteryResource.amount - fixedConsumption);
            var maxSupply = Math.Min(currentMaxPower, batteryResource.amount);

            supplyFNResourcePerSecondWithMaxAndEfficiency(Math.Min(1,fuelRatio) * electrical_power_currently_needed, maxSupply, 1, ResourceManager.FNRESOURCE_MEGAJOULES);
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
