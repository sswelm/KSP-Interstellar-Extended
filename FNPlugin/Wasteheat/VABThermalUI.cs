using FNPlugin.Beamedpower;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Powermanagement;
using FNPlugin.Propulsion;
using FNPlugin.Redist;
using KSP.Localization;
using System;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;

namespace FNPlugin.Wasteheat
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class VABThermalUI : MonoBehaviour
    {
        public static bool renderWindow;

        private int bestPercentage;
        private int numberOfRadiators;
        private int thermalWindowId = 825462;
        private bool has_thermal_generators;

        private const int labelWidth = 300;
        private const int valueWidth = 80;

        private Rect windowPosition = new Rect(500, 500, labelWidth + valueWidth, 100);
        private GUIStyle bold_label;
        private GUIStyle green_label;
        private GUIStyle red_label;
        private GUIStyle orange_label;

        private float au_scale = 1;
        private float engine_throttle_percentage = 100;

        private double wasteheat_source_power_100pc;
        private double wasteheat_source_power_70pc;
        private double wasteheat_source_power_60pc;
        private double wasteheat_source_power_50pc;
        private double wasteheat_source_power_40pc;
        private double wasteheat_source_power_30pc;
        private double wasteheat_source_power_20pc;
        private double wasteheat_source_power_10pc;

        private double source_temp_at_100pc;
        private double source_temp_at_70pc;
        private double source_temp_at_60pc;
        private double source_temp_at_50pc;
        private double source_temp_at_40pc;
        private double source_temp_at_30pc;
        private double source_temp_at_20pc;
        private double source_temp_at_10pc;

        private double resting_radiator_temp_at_100pcnt;
        private double resting_radiator_temp_at_70pcnt;
        private double resting_radiator_temp_at_60pcnt;
        private double resting_radiator_temp_at_50pcnt;
        private double resting_radiator_temp_at_40pcnt;
        private double resting_radiator_temp_at_30pcnt;
        private double resting_radiator_temp_at_20pcnt;
        private double resting_radiator_temp_at_10pcnt;

        private double generator_efficiency_at_100pcnt;
        private double generator_efficiency_at_70pcnt;
        private double generator_efficiency_at_60pcnt;
        private double generator_efficiency_at_50pcnt;
        private double generator_efficiency_at_40pcnt;
        private double generator_efficiency_at_30pcnt;
        private double generator_efficiency_at_20pcnt;
        private double generator_efficiency_at_10pcnt;

        private double rad_max_dissip;
        private double total_area;
        private double average_rad_temp;
        private double auto_best_total_electric_power;
        private double drymass;
        private double wetmass;

        public void Start()
        {
            if (PluginHelper.usingToolbar)
                renderWindow = false;
        }


        public void Update()
        {
            if (!renderWindow)
                return;

            // thermal logic
            var thermalSources = new List<IPowerSource>();
            var radiators = new List<FNRadiator>();
            var generators = new List<FNGenerator>();
            var solarPanels = new List<ModuleDeployableSolarPanel>();
            var thermalEngines = new List<ThermalEngineController>();
            var beamedReceivers = new List<BeamedPowerReceiver>();
            var variableEngines = new List<FusionECU2>();
            var fusionEngines = new List<DaedalusEngineController>();
            var beamedTransmitter = new List<BeamedPowerTransmitter>();

            drymass = 0;
            wetmass = 0;

            foreach (var part in EditorLogic.fetch.ship.parts)
            {
                drymass += part.mass;
                wetmass += part.Resources.Sum(m => m.amount * m.info.density);

                thermalSources.AddRange(part.FindModulesImplementing<IPowerSource>());
                radiators.AddRange(part.FindModulesImplementing<FNRadiator>());
                solarPanels.AddRange(part.FindModulesImplementing<ModuleDeployableSolarPanel>());
                generators.AddRange(part.FindModulesImplementing<FNGenerator>());
                thermalEngines.AddRange(part.FindModulesImplementing<ThermalEngineController>());
                beamedReceivers.AddRange(part.FindModulesImplementing<BeamedPowerReceiver>());
                variableEngines.AddRange(part.FindModulesImplementing<FusionECU2>());
                fusionEngines.AddRange(part.FindModulesImplementing<DaedalusEngineController>());
                beamedTransmitter.AddRange(part.FindModulesImplementing<BeamedPowerTransmitter>());
            }

            wasteheat_source_power_100pc = 0;
            wasteheat_source_power_70pc = 0;
            wasteheat_source_power_60pc = 0;
            wasteheat_source_power_50pc = 0;
            wasteheat_source_power_40pc = 0;
            wasteheat_source_power_30pc = 0;
            wasteheat_source_power_20pc = 0;
            wasteheat_source_power_10pc = 0;

            source_temp_at_100pc = double.MaxValue;
            source_temp_at_70pc = double.MaxValue;
            source_temp_at_60pc = double.MaxValue;
            source_temp_at_50pc = double.MaxValue;
            source_temp_at_30pc = double.MaxValue;
            source_temp_at_20pc = double.MaxValue;
            source_temp_at_10pc = double.MaxValue;

            double totalTemperaturePowerAt100Percent = 0;
            double totalTemperaturePowerAt70Percent = 0;
            double totalTemperaturePowerAt60Percent = 0;
            double totalTemperaturePowerAt50Percent = 0;
            double totalTemperaturePowerAt40Percent = 0;
            double totalTemperaturePowerAt30Percent = 0;
            double totalTemperaturePowerAt20Percent = 0;
            double totalTemperaturePowerAt10Percent = 0;

            // first calculate reactors
            foreach (IPowerSource powerSource in thermalSources)
            {
                double combinedMaxStableMegaWattPower = 0;

                var connectedThermalPowerGenerator = powerSource.ConnectedThermalElectricGenerator;
                var connectedChargedPowerGenerator = powerSource.ConnectedChargedParticleElectricGenerator;

                // when connected to a thermal source, assume most thermal energy thermal power can end up in the radiators
                if (connectedThermalPowerGenerator != null)
                    combinedMaxStableMegaWattPower += (1 - powerSource.ChargedPowerRatio) * connectedThermalPowerGenerator.MaxStableMegaWattPower;

                if (connectedChargedPowerGenerator != null)
                {
                    // when a thermal source is not connected to a thermal power generator, all thermal power ends up in the radiators
                    if (connectedThermalPowerGenerator == null)
                        combinedMaxStableMegaWattPower += (1 - powerSource.ChargedPowerRatio) * connectedChargedPowerGenerator.MaxStableMegaWattPower;

                    // only non directly converted power end up in the radiators
                    var chargedPowerGenerator = connectedChargedPowerGenerator as FNGenerator;
                    if (chargedPowerGenerator != null)
                        combinedMaxStableMegaWattPower += powerSource.ChargedPowerRatio * connectedChargedPowerGenerator.MaxStableMegaWattPower * (1 - chargedPowerGenerator.maxEfficiency);
                }

                // only take reactor power in account when its actually connected to a power generator
                if (connectedThermalPowerGenerator == null && connectedChargedPowerGenerator == null) continue;

                double coreTempAtRadiatorTempAt100Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_100pcnt);
                double coreTempAtRadiatorTempAt70Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_70pcnt);
                double coreTempAtRadiatorTempAt60Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_60pcnt);
                double coreTempAtRadiatorTempAt50Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_50pcnt);
                double coreTempAtRadiatorTempAt40Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_40pcnt);
                double coreTempAtRadiatorTempAt30Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_30pcnt);
                double coreTempAtRadiatorTempAt20Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_20pcnt);
                double coreTempAtRadiatorTempAt10Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_10pcnt);

                var effectivePowerAt100Percent = (1 - generator_efficiency_at_100pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt100Percent));
                var effectivePowerAt70Percent = (1 - generator_efficiency_at_70pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt70Percent) * 0.7);
                var effectivePowerAt60Percent = (1 - generator_efficiency_at_60pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt50Percent) * 0.6);
                var effectivePowerAt50Percent = (1 - generator_efficiency_at_50pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt50Percent) * 0.5);
                var effectivePowerAt40Percent = (1 - generator_efficiency_at_40pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt40Percent) * 0.4);
                var effectivePowerAt30Percent = (1 - generator_efficiency_at_30pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt30Percent) * 0.3);
                var effectivePowerAt20Percent = (1 - generator_efficiency_at_20pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt20Percent) * 0.2);
                var effectivePowerAt10Percent = (1 - generator_efficiency_at_10pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt10Percent) * 0.1);

                totalTemperaturePowerAt100Percent += coreTempAtRadiatorTempAt100Percent * effectivePowerAt100Percent;
                totalTemperaturePowerAt70Percent += coreTempAtRadiatorTempAt70Percent * effectivePowerAt70Percent;
                totalTemperaturePowerAt60Percent += coreTempAtRadiatorTempAt60Percent * effectivePowerAt60Percent;
                totalTemperaturePowerAt50Percent += coreTempAtRadiatorTempAt50Percent * effectivePowerAt50Percent;
                totalTemperaturePowerAt40Percent += coreTempAtRadiatorTempAt40Percent * effectivePowerAt40Percent;
                totalTemperaturePowerAt30Percent += coreTempAtRadiatorTempAt30Percent * effectivePowerAt30Percent;
                totalTemperaturePowerAt20Percent += coreTempAtRadiatorTempAt20Percent * effectivePowerAt20Percent;
                totalTemperaturePowerAt10Percent += coreTempAtRadiatorTempAt10Percent * effectivePowerAt10Percent;

                wasteheat_source_power_100pc += effectivePowerAt100Percent;
                wasteheat_source_power_70pc += effectivePowerAt70Percent;
                wasteheat_source_power_60pc += effectivePowerAt60Percent;
                wasteheat_source_power_50pc += effectivePowerAt50Percent;
                wasteheat_source_power_40pc += effectivePowerAt40Percent;
                wasteheat_source_power_30pc += effectivePowerAt30Percent;
                wasteheat_source_power_20pc += effectivePowerAt20Percent;
                wasteheat_source_power_10pc += effectivePowerAt10Percent;
            }

            // calculated weighted core temperatures
            if (wasteheat_source_power_100pc > 0) source_temp_at_100pc = totalTemperaturePowerAt100Percent / wasteheat_source_power_100pc;
            if (wasteheat_source_power_70pc > 0) source_temp_at_70pc = totalTemperaturePowerAt70Percent / wasteheat_source_power_70pc;
            if (wasteheat_source_power_60pc > 0) source_temp_at_60pc = totalTemperaturePowerAt60Percent / wasteheat_source_power_60pc;
            if (wasteheat_source_power_50pc > 0) source_temp_at_50pc = totalTemperaturePowerAt50Percent / wasteheat_source_power_50pc;
            if (wasteheat_source_power_40pc > 0) source_temp_at_40pc = totalTemperaturePowerAt40Percent / wasteheat_source_power_40pc;
            if (wasteheat_source_power_30pc > 0) source_temp_at_30pc = totalTemperaturePowerAt30Percent / wasteheat_source_power_30pc;
            if (wasteheat_source_power_20pc > 0) source_temp_at_20pc = totalTemperaturePowerAt20Percent / wasteheat_source_power_20pc;
            if (wasteheat_source_power_10pc > 0) source_temp_at_10pc = totalTemperaturePowerAt10Percent / wasteheat_source_power_10pc;

            // calculate effect of on demand beamed power
            foreach (BeamedPowerReceiver beamedReceiver in beamedReceivers)
            {
                // only count receiver that are activated
                if (!beamedReceiver.receiverIsEnabled)
                    continue;

                var maxWasteheatProduction = beamedReceiver.MaximumRecievePower * (1 - beamedReceiver.activeBandwidthConfiguration.MaxEfficiencyPercentage * 0.01);

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.7;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.6;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.5;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.4;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.3;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.2;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.1;
            }

            foreach (var beamedPowerTransmitter in beamedTransmitter)
            {
                if (!beamedPowerTransmitter.IsEnabled)
                    return;

                var maxWasteheatProduction = beamedPowerTransmitter.PowerCapacity * beamedPowerTransmitter.activeBeamGenerator.efficiencyPercentage * 0.01;

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.7;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.6;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.5;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.4;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.3;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.2;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.1;
            }

            var engineThrottleRatio = 0.01 * engine_throttle_percentage;

            foreach (ThermalEngineController thermalNozzle in thermalEngines)
            {
                var maxWasteheatProduction = engineThrottleRatio * thermalNozzle.ReactorWasteheatModifier * thermalNozzle.AttachedReactor.NormalisedMaximumPower;

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.7;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.6;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.5;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.4;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.3;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.2;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.1;
            }

            foreach (FusionECU2 variableEngine in variableEngines)
            {
                var maxWasteheatProduction = engineThrottleRatio * variableEngine.fusionWasteHeatMax;

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.7;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.6;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.5;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.4;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.3;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.2;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.1;
            }

            foreach (DaedalusEngineController fusionEngine in fusionEngines)
            {
                var maxWasteheatProduction = 0.01 * engine_throttle_percentage * fusionEngine.wasteHeat;

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.7;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.6;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.5;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.4;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.3;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.2;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.1;
            }

            foreach (ModuleDeployableSolarPanel solarPanel in solarPanels)
            {
                wasteheat_source_power_100pc += solarPanel.chargeRate * 0.0005/au_scale/au_scale;
            }

            CalculateGeneratedElectricPower(generators);

            numberOfRadiators = 0;
            rad_max_dissip = 0;
            average_rad_temp = 0;
            total_area = 0;

            foreach (FNRadiator radiator in radiators)
            {
                total_area += radiator.BaseRadiatorArea;
                var maxRadTemperature = radiator.MaxRadiatorTemperature;
                maxRadTemperature = Math.Min(maxRadTemperature, source_temp_at_100pc);
                numberOfRadiators++;
                var tempToPowerFour = maxRadTemperature * maxRadTemperature * maxRadTemperature * maxRadTemperature;
                rad_max_dissip += GameConstants.stefan_const * radiator.EffectiveRadiatorArea * tempToPowerFour / 1e6;
                average_rad_temp += maxRadTemperature;
            }
            average_rad_temp = numberOfRadiators != 0 ? average_rad_temp / numberOfRadiators : double.NaN;

            var radRatio100Pc = wasteheat_source_power_100pc / rad_max_dissip;
            var radRatio70Pc = wasteheat_source_power_70pc / rad_max_dissip;
            var radRatio60Pc = wasteheat_source_power_60pc / rad_max_dissip;
            var radRatio50Pc = wasteheat_source_power_50pc / rad_max_dissip;
            var radRatio40Pc = wasteheat_source_power_40pc / rad_max_dissip;
            var radRatio30Pc = wasteheat_source_power_30pc / rad_max_dissip;
            var radRatio20Pc = wasteheat_source_power_20pc / rad_max_dissip;
            var radRatio10Pc = wasteheat_source_power_10pc / rad_max_dissip;

            resting_radiator_temp_at_100pcnt = (!radRatio100Pc.IsInfinityOrNaN() ? Math.Pow(radRatio100Pc,0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_70pcnt = (!radRatio70Pc.IsInfinityOrNaN() ? Math.Pow(radRatio70Pc, 0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_60pcnt = (!radRatio60Pc.IsInfinityOrNaN() ? Math.Pow(radRatio60Pc, 0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_50pcnt = (!radRatio50Pc.IsInfinityOrNaN() ? Math.Pow(radRatio50Pc, 0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_40pcnt = (!radRatio40Pc.IsInfinityOrNaN() ? Math.Pow(radRatio40Pc, 0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_30pcnt = (!radRatio30Pc.IsInfinityOrNaN() ? Math.Pow(radRatio30Pc, 0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_20pcnt = (!radRatio20Pc.IsInfinityOrNaN() ? Math.Pow(radRatio20Pc, 0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_10pcnt = (!radRatio10Pc.IsInfinityOrNaN() ? Math.Pow(radRatio10Pc, 0.25) : 0) * average_rad_temp;

            var thermalGenerators = generators.Where(m => !m.chargedParticleMode).ToList();

            if (thermalGenerators.Count > 0)
            {
                var maximumGeneratedPower = thermalGenerators.Sum(m => m.maximumGeneratorPowerMJ);
                var averageEfficiency = thermalGenerators.Sum(m => m.maxEfficiency * m.maximumGeneratorPowerMJ) / maximumGeneratedPower;

                has_thermal_generators = true;
                generator_efficiency_at_100pcnt = source_temp_at_100pc >= double.MaxValue || resting_radiator_temp_at_100pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_100pcnt / source_temp_at_100pc;
                generator_efficiency_at_100pcnt = Math.Max(averageEfficiency * generator_efficiency_at_100pcnt,0);

                generator_efficiency_at_70pcnt = source_temp_at_70pc >= double.MaxValue || resting_radiator_temp_at_70pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_70pcnt / source_temp_at_100pc;
                generator_efficiency_at_70pcnt = Math.Max(averageEfficiency * generator_efficiency_at_70pcnt, 0);

                generator_efficiency_at_60pcnt = source_temp_at_60pc >= double.MaxValue || resting_radiator_temp_at_60pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_60pcnt / source_temp_at_100pc;
                generator_efficiency_at_60pcnt = Math.Max(averageEfficiency * generator_efficiency_at_60pcnt, 0);

                generator_efficiency_at_50pcnt = source_temp_at_50pc >= double.MaxValue || resting_radiator_temp_at_50pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_50pcnt / source_temp_at_100pc;
                generator_efficiency_at_50pcnt = Math.Max(averageEfficiency * generator_efficiency_at_50pcnt, 0);

                generator_efficiency_at_40pcnt = source_temp_at_40pc >= double.MaxValue || resting_radiator_temp_at_40pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_40pcnt / source_temp_at_100pc;
                generator_efficiency_at_40pcnt = Math.Max(averageEfficiency * generator_efficiency_at_40pcnt, 0);

                generator_efficiency_at_30pcnt = source_temp_at_30pc >= double.MaxValue || resting_radiator_temp_at_30pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_30pcnt / source_temp_at_100pc;
                generator_efficiency_at_30pcnt = Math.Max(averageEfficiency * generator_efficiency_at_30pcnt, 0);

                generator_efficiency_at_20pcnt = source_temp_at_20pc >= double.MaxValue || resting_radiator_temp_at_20pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_20pcnt / source_temp_at_100pc;
                generator_efficiency_at_20pcnt = Math.Max(averageEfficiency * generator_efficiency_at_20pcnt, 0);

                generator_efficiency_at_10pcnt = source_temp_at_10pc >= double.MaxValue || resting_radiator_temp_at_10pcnt.IsInfinityOrNaN() ? 0 : 1 - resting_radiator_temp_at_10pcnt / source_temp_at_100pc;
                generator_efficiency_at_10pcnt = Math.Max(averageEfficiency * generator_efficiency_at_10pcnt, 0);
            }
            else
                has_thermal_generators = false;

            if (source_temp_at_100pc >= double.MaxValue) source_temp_at_100pc = -1;
            if (source_temp_at_70pc >= double.MaxValue) source_temp_at_70pc = -1;
            if (source_temp_at_60pc >= double.MaxValue) source_temp_at_60pc = -1;
            if (source_temp_at_50pc >= double.MaxValue) source_temp_at_50pc = -1;
            if (source_temp_at_40pc >= double.MaxValue) source_temp_at_40pc = -1;
            if (source_temp_at_30pc >= double.MaxValue) source_temp_at_30pc = -1;
            if (source_temp_at_20pc >= double.MaxValue) source_temp_at_20pc = -1;
            if (source_temp_at_10pc >= double.MaxValue) source_temp_at_10pc = -1;
        }

        private void CalculateGeneratedElectricPower(List<FNGenerator> generators)
        {
            double electricPowerAt100 = 0;
            double electricPowerAt70 = 0;
            double electricPowerAt60 = 0;
            double electricPowerAt50 = 0;
            double electricPowerAt40 = 0;
            double electricPowerAt30 = 0;
            double electricPowerAt20 = 0;
            double electricPowerAt10 = 0;

            foreach (var generator in generators)
            {
                if (generator.chargedParticleMode)
                {
                    electricPowerAt100 += generator.maximumGeneratorPowerMJ;
                }
                else
                {
                    var generatorMaximumGeneratorPower = generator.maximumGeneratorPowerMJ;

                    electricPowerAt100 += generatorMaximumGeneratorPower * generator_efficiency_at_100pcnt;

                    if (generator.isLimitedByMinThrotle)
                    {
                        electricPowerAt70 += electricPowerAt100;
                        electricPowerAt60 += electricPowerAt100;
                        electricPowerAt50 += electricPowerAt100;
                        electricPowerAt40 += electricPowerAt100;
                        electricPowerAt30 += electricPowerAt100;
                        electricPowerAt20 += electricPowerAt100;
                        electricPowerAt10 += electricPowerAt100;
                        continue;
                    }

                    electricPowerAt70 += generatorMaximumGeneratorPower * generator_efficiency_at_70pcnt * 0.7;
                    electricPowerAt60 += generatorMaximumGeneratorPower * generator_efficiency_at_60pcnt * 0.6;
                    electricPowerAt50 += generatorMaximumGeneratorPower * generator_efficiency_at_50pcnt * 0.5;
                    electricPowerAt40 += generatorMaximumGeneratorPower * generator_efficiency_at_40pcnt * 0.4;
                    electricPowerAt30 += generatorMaximumGeneratorPower * generator_efficiency_at_30pcnt * 0.3;
                    electricPowerAt20 += generatorMaximumGeneratorPower * generator_efficiency_at_20pcnt * 0.2;
                    electricPowerAt10 += generatorMaximumGeneratorPower * generator_efficiency_at_10pcnt * 0.1;
                }
            }

            bestPercentage = 100;
            auto_best_total_electric_power = electricPowerAt100;

            if (electricPowerAt70 > auto_best_total_electric_power)
            {
                bestPercentage = 70;
                auto_best_total_electric_power = electricPowerAt70;
            }
            if (electricPowerAt60 > auto_best_total_electric_power)
            {
                bestPercentage = 60;
                auto_best_total_electric_power = electricPowerAt60;
            }
            if (electricPowerAt50 > auto_best_total_electric_power)
            {
                bestPercentage = 50;
                auto_best_total_electric_power = electricPowerAt50;
            }
            if (electricPowerAt40 > auto_best_total_electric_power)
            {
                bestPercentage = 40;
                auto_best_total_electric_power = electricPowerAt40;
            }
            if (electricPowerAt30 > auto_best_total_electric_power)
            {
                bestPercentage = 30;
                auto_best_total_electric_power = electricPowerAt30;
            }
            if (electricPowerAt20 > auto_best_total_electric_power)
            {
                bestPercentage = 20;
                auto_best_total_electric_power = electricPowerAt20;
            }
            if (electricPowerAt10 > auto_best_total_electric_power)
            {
                bestPercentage = 10;
                auto_best_total_electric_power = electricPowerAt10;
            }
        }

        protected void OnGUI()
        {
            if (renderWindow)
                windowPosition = GUILayout.Window(thermalWindowId, windowPosition, Window, Localizer.Format("#LOC_KSPIE_VABThermalUI_title"));//"Interstellar Thermal Mechanics Helper"
        }

        private void Window(int windowId)
        {
            if (green_label == null)
                green_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.green}};
            if (red_label == null)
                red_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.red}};
            if (orange_label == null)
                orange_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
            if (bold_label == null)
                bold_label = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold};

            var guiLabelWidth = GUILayout.MinWidth(labelWidth);
            var guiValueWidth = GUILayout.MinWidth(valueWidth);

            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                renderWindow = false;

            GUIStyle radiatorLabel = green_label;
            if (rad_max_dissip < wasteheat_source_power_100pc)
            {
                radiatorLabel = orange_label;
                if (rad_max_dissip < wasteheat_source_power_30pc)
                    radiatorLabel = red_label;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_DistanceFromKerbol"), GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            au_scale = GUILayout.HorizontalSlider(au_scale, 0.001f, 8f, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(au_scale.ToString("0.000")+ " AU", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_EngineThrottlePercentage"), GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            engine_throttle_percentage = GUILayout.HorizontalSlider(engine_throttle_percentage, 0, 100, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(engine_throttle_percentage.ToString("0.0") + " %", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalHeatProduction"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Heat Production:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(wasteheat_source_power_100pc), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ThermalSourceTemperature100"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Thermal Source Temperature at 100%:"
            string sourceTempString100 = (source_temp_at_100pc < 0) ? "N/A" : source_temp_at_100pc.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString100, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ThermalSourceTemperature30"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Thermal Source Temperature at 30%:"
            string sourceTempString30 = (source_temp_at_30pc < 0) ? "N/A" : source_temp_at_30pc.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString30, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("Vessel Mass:"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Number of Radiators:"
            GUILayout.Label((drymass + wetmass).ToString("0.0") + " t", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalNumberofRadiators"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Number of Radiators:"
            GUILayout.Label(numberOfRadiators.ToString(), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalAreaRadiators"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Area Radiators:"
            GUILayout.Label(total_area.ToString("0.0") + " m\xB2", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorMaximumDissipation"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Maximum Dissipation:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(rad_max_dissip), radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string restingRadiatorTempAt100PcntStr = (!double.IsInfinity(resting_radiator_temp_at_100pcnt) && !double.IsNaN(resting_radiator_temp_at_100pcnt)) ? resting_radiator_temp_at_100pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorRestingTemperatureat100"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Resting Temperature at 100% Power:"
            GUILayout.Label(restingRadiatorTempAt100PcntStr, radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string restingRadiatorTempAt30PcntStr = (!double.IsInfinity(resting_radiator_temp_at_30pcnt) && !double.IsNaN(resting_radiator_temp_at_30pcnt)) ? resting_radiator_temp_at_30pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorRestingTemperatureat30"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Resting Temperature at 30% Power:"
            GUILayout.Label(restingRadiatorTempAt30PcntStr, radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            if (has_thermal_generators)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingGeneratorEfficiencyat100"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Resting Generator Efficiency at 100% Power:"
                GUILayout.Label((generator_efficiency_at_100pcnt*100).ToString("0.00") + "%", radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingGeneratorEfficiencyat30"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Resting Generator Efficiency at 30% Power:"
                GUILayout.Label((generator_efficiency_at_30pcnt * 100).ToString("0.00") + "%", radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Resting Electric Power at " + bestPercentage + "% Power", bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(PluginHelper.getFormattedPowerString(auto_best_total_electric_power), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
