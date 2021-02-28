using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
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
        public const string Group = "FNBatteryGenerator";
        public const string GroupTitle = "#LOC_KSPIE_FNBatteryGenerator_groupName";

        // configuration
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_MaximumEnergy", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F2")]//Maximum Energy Stored
        public double maxPower = 1;

        [KSPField] public bool forceActivateAtStartup = true;
        [KSPField] public bool canRecharge = true;
        [KSPField] public bool canConsumeGlobal = false;
        [KSPField] public double efficiency = 1;
        [KSPField] public string inputResources = "KilowattHour";
        [KSPField] public string inputConversionRates = "2.77777777777e-1";

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricSupplyPriority = 5;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_SpareMWCapacity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Spare MW Capacity
        public double spareResourceCapacity;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_Remainingsupplylifetime", guiUnits = " s", guiFormat = "F0")]//Remaining supply lifetime
        public double batterySupplyRemaining;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNBatteryGenerator_MaximumPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Maximum Power
        public double currentMaxPower = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_PowerSupply", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Power Supply
        public double powerSupply;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiName = "#LOC_KSPIE_FNBatteryGenerator_Wasteheat", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]//Wasteheat
        public double wasteheat;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double electricalPowerCurrentlyNeeded;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_powerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerPercentage = 100;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = true, guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerSurplus;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiFormat = "F2")]
        public double effectiveFuelRatio;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
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

            resourcesToSupply = new[]{ ResourceSettings.Config.ElectricPowerInMegawatt };

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

            wasteheatRatio = GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);
            inefficiency = 1 - efficiency;
            efficiencyModifier = inefficiency == 0 || wasteheatRatio < efficiency ? 1 : 1 - ((wasteheatRatio - efficiency) / inefficiency);
            currentMaxPower = Math.Round(powerPercentage * 0.01, 2) * maxPower;
            effectiveMaxPower = currentMaxPower * efficiency;

            powerSurplus = GetCurrentSurplus(ResourceSettings.Config.ElectricPowerInMegawatt);
            currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));

            if (currentUnfilledResourceDemand == 0 && powerSurplus > 0 && canRecharge)
            {
                SupplyFnResourcePerSecondWithMax(0, effectiveMaxPower, ResourceSettings.Config.ElectricPowerInMegawatt);

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
                    consumedPower = ConsumeFnResourcePerSecond(requestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                    // TODO: waste heat when charging?

                    currentRequestedConsumptionRate = consumedPower * currentInputConversionRate;
                    currentFixedConsumption = currentRequestedConsumptionRate * fixedDeltaTime;

                    if (currentFixedConsumption.IsInfinityOrNaN().IsFalse())
                        currentBatteryResource.amount = currentBatteryResource.amount + currentFixedConsumption;
                }
            }
            else if (currentUnfilledResourceDemand > 0)
            {
                spareResourceCapacity = GetSpareResourceCapacity(ResourceSettings.Config.ElectricPowerInMegawatt);

                electricalPowerCurrentlyNeeded = efficiency == 0 ? 0 : efficiencyModifier * Math.Min(currentUnfilledResourceDemand + spareResourceCapacity, currentMaxPower) / efficiency;

                for (var i = 0; i < inputResourceNames.Count; i++)
                {
                    var currentInputConversionRate = inputResourceRate[i];
                    var currentResourceName = inputResourceNames[i];
                    currentFixedConsumption = electricalPowerCurrentlyNeeded * currentInputConversionRate * fixedDeltaTime;

                    double currentFuelRatio;
                    if (canConsumeGlobal)
                    {
                        var consumedFixedAmount = CheatOptions.InfinitePropellant
                            ? currentFixedConsumption
                            : part.RequestResource(currentResourceName, currentFixedConsumption);

                        currentFuelRatio = currentFixedConsumption == 0 ? 1 : consumedFixedAmount / currentFixedConsumption;
                    }
                    else
                    {
                        var currentBatteryResource = part.Resources[currentResourceName];

                        if (currentBatteryResource == null) continue;

                        var availableAmount = CheatOptions.InfinitePropellant ? currentFixedConsumption : currentBatteryResource.amount;

                        currentFuelRatio = currentFixedConsumption == 0 ? 1 : availableAmount / currentFixedConsumption;

                        if (!CheatOptions.InfinitePropellant && currentFixedConsumption.IsInfinityOrNaN().IsFalse())
                            currentBatteryResource.amount = Math.Max(0, currentBatteryResource.amount - currentFixedConsumption);
                    }

                    if (currentFuelRatio < fuelRatio)
                        fuelRatio = currentFuelRatio;

                    var currentBatterySupplyRemaining = currentFuelRatio / fixedDeltaTime / GameConstants.ecPerMJ;
                    if (currentBatterySupplyRemaining < batterySupplyRemaining)
                        batterySupplyRemaining = currentBatterySupplyRemaining;
                }

                effectiveFuelRatio = Math.Min(1, fuelRatio);
                var rawPowerSupply = effectiveFuelRatio * electricalPowerCurrentlyNeeded;

                powerSupply = efficiency * rawPowerSupply;
                wasteheat = inefficiency * rawPowerSupply;

                if (powerSupply != 0)
                {
                    powerSurplus = 0;

                    effectiveMaxPower = currentMaxPower * efficiency * effectiveFuelRatio;

                    SupplyFnResourcePerSecondWithMax(powerSupply, effectiveMaxPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                    if (inefficiency > 0)
                        SupplyFnResourcePerSecondWithMax(wasteheat, currentMaxPower * inefficiency * effectiveFuelRatio, ResourceSettings.Config.WasteHeatInMegawatt);
                }
                else
                {
                    SupplyFnResourcePerSecondWithMax(0, effectiveMaxPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                    if (inefficiency > 0)
                        SupplyFnResourcePerSecondWithMax(0, currentMaxPower * inefficiency * effectiveFuelRatio, ResourceSettings.Config.WasteHeatInMegawatt);
                }
            }
            else
            {
                // there is either no demand, or we can't charge at the moment.
                SupplyFnResourcePerSecondWithMax(0, effectiveMaxPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                if (inefficiency > 0)
                    SupplyFnResourcePerSecondWithMax(0, currentMaxPower * inefficiency * effectiveFuelRatio, ResourceSettings.Config.WasteHeatInMegawatt);
            }
        }

        public override int getPowerPriority()
        {
            return 5;   // lowest priority reserved for Battery recharge
        }

        public override int GetSupplyPriority()
        {
            return (int)electricSupplyPriority;
        }
    }
}
