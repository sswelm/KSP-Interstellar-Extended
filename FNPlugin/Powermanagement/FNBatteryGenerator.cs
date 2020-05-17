using FNPlugin.Extensions;
using System;
using System.Collections.Generic;

namespace FNPlugin.Powermanagement
{
    [KSPModule("Reactor Power Generator")]
    class FNPowerGenerator : FNBatteryGenerator
    {
        public FNPowerGenerator() { canRecharge = false; }
    }

    [KSPModule("Battery Power Generator")]
    class FNBatteryGenerator : ResourceSuppliableModule
    {
        // configuration
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_MaximumPower", guiUnits = " MW", guiFormat = "F3")]//Maximum Power
        public double maxPower = 1;
        [KSPField]
        public bool forceActivateAtStartup = true;
        [KSPField]
        public bool canRecharge = true;
        [KSPField]
        public double efficiency = 1;
        [KSPField]
        public string inputResources = "KilowattHour";
        [KSPField]
        public string inputConversionRates = "2.77777777777e-1";
        [KSPField]
        public string outputConversionRates = "";
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricSupplyPriority = 5;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_SpareMWCapacity",  guiUnits = " MW", guiFormat = "F3")]//Spare MW Capacity
        public double spareResourceCapacity;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_Remainingsupplylifetime", guiUnits = " s", guiFormat = "F0")]//Remaining supply lifetime
        public double batterySupplyRemaining;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_MaximumPower", guiUnits = " MW", guiFormat = "F3")]//Maximum Power
        public double currentMaxPower = 1;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_PowerSupply", guiUnits = " MW", guiFormat = "F3")]//Power Supply
        public double powerSupply;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_Wasteheat", guiUnits = " MJ", guiFormat = "F3")]//Wasteheat
        public double wasteheat;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = " MW", guiFormat = "F4")]
        public double electrical_power_currently_needed;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_powerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerPercentage = 100;

        [KSPField(isPersistant = false, guiActive = true, guiUnits = " MW", guiFormat = "F4")]
        public double powerSurplus;
        [KSPField(isPersistant = false, guiActive = false, guiFormat = "F4")]
        public double effectiveFuelRatio;
        [KSPField(isPersistant = false, guiActive = false, guiUnits = " MW", guiFormat = "F4")]
        public double effectiveMaxPower;

        // privates
        List<string> inputResourceNames;
        List<double> inputResourceRate;

        double inefficiency;
        double wasteheatRatio;
        double efficiencyModifier;
        double fuelRatio;
        double currentUnfilledResourceDemand;
        double currentRequestedConsumptionRate;
        double currentFixedConsumption;
        double requestedPower;
        double consumedPower;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) return;

            string[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            if (forceActivateAtStartup)
                part.force_activate();

            inputResourceNames = ParseTools.ParseNames(inputResources);
            inputResourceRate = ParseTools.ParseDoubles(inputConversionRates);
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            fuelRatio = double.MaxValue;

            wasteheatRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);
            inefficiency = 1 - efficiency;

            efficiencyModifier = inefficiency == 0 || wasteheatRatio < efficiency ? 1 : 1 - ((wasteheatRatio - efficiency) / inefficiency);

            currentMaxPower = Math.Round(powerPercentage * 0.01, 2) * maxPower;
            currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES));
            spareResourceCapacity = getSpareResourceCapacity(ResourceManager.FNRESOURCE_MEGAJOULES);

            electrical_power_currently_needed = efficiency == 0 ? 0 : efficiencyModifier * Math.Min(currentUnfilledResourceDemand + spareResourceCapacity, currentMaxPower) / efficiency;

            for (var i = 0; i < inputResourceNames.Count; i++)
            {
                var currentResourceName = inputResourceNames[i];
                var currentBatteryResource = part.Resources[currentResourceName];

                if (currentBatteryResource != null)
                {
                    var currentInputConversionRate = inputResourceRate[i];

                    currentRequestedConsumptionRate = electrical_power_currently_needed * currentInputConversionRate;
                    currentFixedConsumption = currentRequestedConsumptionRate * fixedDeltaTime;
                    var currentFuelRatio = currentFixedConsumption == 0 ? 1 : currentBatteryResource.amount / currentFixedConsumption;

                    if (currentFuelRatio < fuelRatio)
                        fuelRatio = currentFuelRatio;

                    var currentBatterySupplyRemaining = currentFuelRatio / fixedDeltaTime / 1000;
                    if (currentBatterySupplyRemaining < batterySupplyRemaining)
                        batterySupplyRemaining = currentBatterySupplyRemaining;

                    if (currentFixedConsumption.IsInfinityOrNaN().IsFalse())
                        currentBatteryResource.amount = Math.Max(0, currentBatteryResource.amount - currentFixedConsumption);
                }
            }

            effectiveFuelRatio = Math.Min(1, fuelRatio);
            var rawPowerSupply = effectiveFuelRatio * electrical_power_currently_needed;

            effectiveMaxPower = currentMaxPower * efficiency * effectiveFuelRatio;
            

            powerSupply = efficiency * rawPowerSupply;
            wasteheat = inefficiency * rawPowerSupply;

            if (powerSupply != 0)
            {
                powerSurplus = 0;
                requestedPower = 0;
                consumedPower = 0;

                supplyFNResourcePerSecondWithMax(powerSupply, effectiveMaxPower, ResourceManager.FNRESOURCE_MEGAJOULES);
                if (inefficiency > 0)
                    supplyFNResourcePerSecondWithMax(wasteheat, currentMaxPower * inefficiency * effectiveFuelRatio, ResourceManager.FNRESOURCE_WASTEHEAT);
            }
            else if (canRecharge) 
            {
                powerSurplus = GetCurrentSurplus(ResourceManager.FNRESOURCE_MEGAJOULES);

                if (powerSurplus > 0)
                {
                    for (var i = 0; i < inputResourceNames.Count; i++)
                    {
                        var currentResourceName = inputResourceNames[i];
                        var currentBatteryResource = part.Resources[currentResourceName];

                        if (currentBatteryResource == null)
                            continue;

                        var spareRoom = Math.Max(0, currentBatteryResource.maxAmount - currentBatteryResource.amount);
                        var currentInputConversionRate = inputResourceRate[i];
                        var maxPowerNeeded = currentInputConversionRate > 0 ? 0 : spareRoom / currentInputConversionRate / fixedDeltaTime;

                        requestedPower = Math.Min(powerSurplus * 0.995, maxPowerNeeded);
                        consumedPower = consumeFNResourcePerSecond(requestedPower, ResourceManager.FNRESOURCE_MEGAJOULES);

                        currentRequestedConsumptionRate = consumedPower * currentInputConversionRate;
                        currentFixedConsumption = currentRequestedConsumptionRate * fixedDeltaTime;

                        if (currentFixedConsumption.IsInfinityOrNaN().IsFalse())
                            currentBatteryResource.amount = currentBatteryResource.amount + currentFixedConsumption;
                    }
                }
            }
            else
            {
                supplyFNResourcePerSecondWithMax(0, effectiveMaxPower, ResourceManager.FNRESOURCE_MEGAJOULES);
                if (inefficiency > 0)
                    supplyFNResourcePerSecondWithMax(wasteheat, currentMaxPower * inefficiency * effectiveFuelRatio, ResourceManager.FNRESOURCE_WASTEHEAT);
            }
        }

        public override int getPowerPriority()
        {
            return 5;   // lowest priority reserved for Battery recharge
        }

        public override int getSupplyPriority()
        {
            return (int)electricSupplyPriority;
        }
    }
}
