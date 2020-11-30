using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Collections.Generic;
using FNPlugin.Resources;

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
        public const string GROUP = "FNBatteryGenerator";
        public const string GROUP_TITLE = "#LOC_KSPIE_FNBatteryGenerator_groupName";

        // configuration
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_MaximumEnergy", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F2")]//Maximum Energy Stored
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
        [KSPField(groupName = GROUP, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricSupplyPriority = 5;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_SpareMWCapacity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Spare MW Capacity
        public double spareResourceCapacity;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_Remainingsupplylifetime", guiUnits = " s", guiFormat = "F0")]//Remaining supply lifetime
        public double batterySupplyRemaining;
        [KSPField(groupName = GROUP, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_MaximumPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Maximum Power
        public double currentMaxPower = 1;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_PowerSupply", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Power Supply
        public double powerSupply;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_Wasteheat", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]//Wasteheat
        public double wasteheat;

        [KSPField(groupName = GROUP, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double electrical_power_currently_needed;
        [KSPField(groupName = GROUP, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_powerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerPercentage = 100;

        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerSurplus;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = false, guiFormat = "F2")]
        public double effectiveFuelRatio;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = false, guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
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

            string[] resources_to_supply = { ResourceSettings.Config.ElectricPowerInMegawatt };
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
            powerSupply = 0;
            wasteheat = 0;
            requestedPower = 0;
            consumedPower = 0;

            wasteheatRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);
            inefficiency = 1 - efficiency;
            efficiencyModifier = inefficiency == 0 || wasteheatRatio < efficiency ? 1 : 1 - ((wasteheatRatio - efficiency) / inefficiency);
            currentMaxPower = Math.Round(powerPercentage * 0.01, 2) * maxPower;
            effectiveMaxPower = currentMaxPower * efficiency;

            powerSurplus = GetCurrentSurplus(ResourceSettings.Config.ElectricPowerInMegawatt);
            currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));

            if (currentUnfilledResourceDemand == 0 && powerSurplus > 0 && canRecharge)
            {
                for (var i = 0; i < inputResourceNames.Count; i++)
                {
                    var currentResourceName = inputResourceNames[i];
                    var currentBatteryResource = part.Resources[currentResourceName];

                    if (currentBatteryResource == null)
                        continue;

                    var spareRoom = Math.Max(0, currentBatteryResource.maxAmount - currentBatteryResource.amount);

                    if (Math.Round(spareRoom) == 0)
                        continue;

                    var currentInputConversionRate = inputResourceRate[i];
                    var maxPowerNeeded = currentInputConversionRate > 0 ? spareRoom / currentInputConversionRate / fixedDeltaTime : 0;

                    requestedPower = Math.Min(powerSurplus * 0.995, maxPowerNeeded);
                    consumedPower = consumeFNResourcePerSecond(requestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                    // TODO: waste heat when charging?

                    currentRequestedConsumptionRate = consumedPower * currentInputConversionRate;
                    currentFixedConsumption = currentRequestedConsumptionRate * fixedDeltaTime;

                    if (currentFixedConsumption.IsInfinityOrNaN().IsFalse())
                        currentBatteryResource.amount = currentBatteryResource.amount + currentFixedConsumption;
                }
            }
            else if (currentUnfilledResourceDemand > 0)
            {
                spareResourceCapacity = getSpareResourceCapacity(ResourceSettings.Config.ElectricPowerInMegawatt);

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

                        var currentBatterySupplyRemaining = currentFuelRatio / fixedDeltaTime / GameConstants.ecPerMJ;
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

                    supplyFNResourcePerSecondWithMax(powerSupply, effectiveMaxPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                    if (inefficiency > 0)
                        supplyFNResourcePerSecondWithMax(wasteheat, currentMaxPower * inefficiency * effectiveFuelRatio, ResourceManager.FNRESOURCE_WASTEHEAT);
                }
            }
            else
            {
                // there is either no demand, or we can't charge at the moment.
                supplyFNResourcePerSecondWithMax(0, effectiveMaxPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                if (inefficiency > 0)
                    supplyFNResourcePerSecondWithMax(0, currentMaxPower * inefficiency * effectiveFuelRatio, ResourceManager.FNRESOURCE_WASTEHEAT);
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
