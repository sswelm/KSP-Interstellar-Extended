using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Powermanagement
{
    class FNBatteryGenerator : ResourceSuppliableModule
    {
        // configuration
        [KSPField]
        public double maxPower = 1;
        [KSPField]
        public double conversionRate = 2.77777777777e-1;
        [KSPField]
        public string resourceName = "KilowattHour";

        // debugging
        [KSPField(guiActive = true, guiUnits = " MW", guiFormat = "F3")]
        public double currentUnfilledResourceDemand;
        [KSPField(guiActive = true, guiUnits = " MW", guiFormat = "F3")]
        public double spareResourceCapacity;
        [KSPField(guiActive = true, guiUnits = " U/s")]
        public double requestedConsumptionRate;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = " MW", guiFormat = "F3")]
        public double electrical_power_currently_needed;

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

            if (batteryResource == null) return;

            currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES));
            spareResourceCapacity = getSpareResourceCapacity(ResourceManager.FNRESOURCE_MEGAJOULES);
            electrical_power_currently_needed = Math.Min(currentUnfilledResourceDemand + spareResourceCapacity, maxPower);

            requestedConsumptionRate = electrical_power_currently_needed * conversionRate;

            var fixedConsumption = requestedConsumptionRate * fixedDeltaTime;
            var fuelRatio = Math.Min(1,  batteryResource.amount / fixedConsumption);
            batteryResource.amount = Math.Max(0, batteryResource.amount - fixedConsumption);

            supplyFNResourcePerSecondWithMaxAndEfficiency(fuelRatio * electrical_power_currently_needed, maxPower, 1, ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        public override int getPowerPriority()
        {
            return 4;
        }
    }
}
