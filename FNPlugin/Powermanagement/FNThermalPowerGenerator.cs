using FNPlugin.Wasteheat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Extensions;

namespace FNPlugin.Reactors
{
    [KSPModule("Thermal Power Generator")]
    class FNThermalPowerGenerator : ResourceSuppliableModule
    {
        //Configuration 
        [KSPField] public double maximumPowerCapacity = 0.02; // 20 Kw
        [KSPField] public double maxConversionEfficiency = 0.5; // 50%
        [KSPField] public double requiredTemperatureRatio = 0.1; // 50%
        [KSPField] public double hotColdBathRatioExponent = 0.5;

        //GUI
        [KSPField(guiActive = false, guiName = "Maximum Power Supply", guiFormat = "F3", guiUnits = " MW")]
        public double maximumPowerSupplyInMegaWatt;
        [KSPField(guiActive = true, guiName = "Maximum Power Supply", guiFormat = "F3")]
        public string maximumPowerSupply;
        [KSPField(guiActive = false, guiName = "Current Power Supply", guiFormat = "F3", guiUnits = " MW")]
        public double currentPowerSupplyInMegaWatt;
        [KSPField(guiActive = true, guiName = "Current Power Supply", guiFormat = "F3")]
        public string currentPowerSupply;
        [KSPField(guiActive = true, guiName = "HotBath Temperature", guiFormat = "F3", guiUnits = " K")]
        public double hotBathTemperature;
        [KSPField(guiActive = true, guiName = "ColdBath Temperature", guiFormat = "F3", guiUnits = " K")]
        public double radiatorTemperature;

        // reference types
        private List<Part> _stackAttachedParts;
        private double timeWarpModifer;
        private double spaceTemperature;

        public Part Part
        {
            get { return this.part; }
        }


        public override void OnStart(PartModule.StartState state)
        {
            String[] resources = {ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT};
            this.resources_to_supply = resources;
            base.OnStart(state);

            if (state == StartState.Editor)
            {
                return;
            }

            // look for attached parts
            _stackAttachedParts = part.attachNodes
                .Where(atn => atn.attachedPart != null && atn.nodeType == AttachNode.NodeType.Stack)
                .Select(m => m.attachedPart).ToList();
        }

        public override void OnUpdate()
        {
            maximumPowerSupply = ResourceManager.getPowerFormatString(maximumPowerSupplyInMegaWatt);
            currentPowerSupply = ResourceManager.getPowerFormatString(currentPowerSupplyInMegaWatt);
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            base.OnFixedUpdate();

            var hasRadiators = FNRadiator.hasRadiatorsForVessel(vessel);

            // get radiator temperature
            if (hasRadiators)
                radiatorTemperature = FNRadiator.getAverageRadiatorTemperatureForVessel(vessel);
            else if (!_stackAttachedParts.Any())
                return;
            else
                radiatorTemperature = _stackAttachedParts.Min(m => m.temperature);

            if (double.IsNaN(radiatorTemperature))
                return;

            timeWarpModifer = PluginHelper.GetTimeWarpModifer();
            spaceTemperature = FlightIntegrator.ActiveVesselFI == null ? 4 : FlightIntegrator.ActiveVesselFI.backgroundRadiationTemp;

            hotBathTemperature = Math.Max(4, Math.Max(part.temperature, part.skinTemperature));

            var hotColdBathRatio = 1 - Math.Min(1, radiatorTemperature / hotBathTemperature);

            var thermalConversionEfficiency = maxConversionEfficiency * hotColdBathRatio;

            maximumPowerSupplyInMegaWatt = hotColdBathRatio > requiredTemperatureRatio
                ? thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency)
                : thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency) *
                  Math.Pow(hotColdBathRatio * (1 / requiredTemperatureRatio), hotColdBathRatioExponent);

            var currentUnfilledResourceDemand =
                Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES));

            var requiredRatio = Math.Min(1, currentUnfilledResourceDemand / maximumPowerSupplyInMegaWatt);

            currentPowerSupplyInMegaWatt = requiredRatio * maximumPowerSupplyInMegaWatt;

            var wasteheatInMegaJoules = (1 - thermalConversionEfficiency) * currentPowerSupplyInMegaWatt;

            if (hasRadiators)
                supplyFNResourcePerSecondWithMax(maximumPowerSupplyInMegaWatt, wasteheatInMegaJoules,
                    ResourceManager.FNRESOURCE_WASTEHEAT);
            else // dump heat in attached part
                DumpWasteheatInAttachedParts(fixedDeltaTime, wasteheatInMegaJoules);

            ExtractSystemHeat(fixedDeltaTime);

            // generate thermal power
            supplyFNResourcePerSecondWithMax(currentPowerSupplyInMegaWatt, maximumPowerSupplyInMegaWatt,
                ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        private void ExtractSystemHeat(double fixedDeltaTime)
        {
            var thermalMassPerKilogram = timeWarpModifer * part.mass * part.thermalMassModifier * PhysicsGlobals.StandardSpecificHeatCapacity * 1e-3;

            var temperatureChange = 0.5 * fixedDeltaTime * (currentPowerSupplyInMegaWatt / thermalMassPerKilogram);

            // lower part temperature
            if (!temperatureChange.IsInfinityOrNaN())
            {
                part.temperature = Math.Max(spaceTemperature, part.temperature - temperatureChange);
                part.skinTemperature = Math.Max(spaceTemperature, part.skinTemperature - temperatureChange);
            }
        }

        private void DumpWasteheatInAttachedParts(double fixedDeltaTime, double wasteheatInMegaJoules)
        {
            var stackAttachedPart = _stackAttachedParts.First(m => m.temperature <= radiatorTemperature);

            var stackThermalMassPerKilogram = timeWarpModifer * stackAttachedPart.mass * stackAttachedPart.thermalMassModifier * PhysicsGlobals.StandardSpecificHeatCapacity * 1e-3;

            var stackWasteTemperatureChange = 
                0.5 * fixedDeltaTime * (wasteheatInMegaJoules / _stackAttachedParts.Count / stackThermalMassPerKilogram);

            // increase stack part with waste temperature
            if (!stackWasteTemperatureChange.IsInfinityOrNaN())
            {
                stackAttachedPart.temperature = Math.Max(spaceTemperature, stackAttachedPart.temperature + stackWasteTemperatureChange);
                stackAttachedPart.skinTemperature = Math.Max(spaceTemperature, stackAttachedPart.skinTemperature + stackWasteTemperatureChange);
            }
        }

        public override int getSupplyPriority()
        {
            return 1;
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.AppendLine("Maximum Power: " + ResourceManager.getPowerFormatString(maximumPowerCapacity));

            return info.ToString();
        }
    }
}