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

        private int _bestScenarioPercentage;
        private int _numberOfRadiators;
        private int _thermalWindowId = 825462;
        private bool _hasThermalGenerators;

        private const int LabelWidth = 300;
        private const int ValueWidth = 85;

        private Rect windowPosition = new Rect(500, 500, LabelWidth + ValueWidth, 100);

        private GUIStyle bold_label;
        private GUIStyle blue_label;
        private GUIStyle green_label;
        private GUIStyle red_label;
        private GUIStyle orange_label;

        private float engineThrottlePercentage = 100;
        private float customScenarioPercentage = 100;
        private float customScenarioFraction = 1;

        private double wasteheat_source_power_custom;
        private double wasteheat_source_power_100pc;
        private double wasteheat_source_power_90pc;
        private double wasteheat_source_power_80pc;
        private double wasteheat_source_power_70pc;
        private double wasteheat_source_power_60pc;
        private double wasteheat_source_power_50pc;
        private double wasteheat_source_power_45pc;
        private double wasteheat_source_power_40pc;
        private double wasteheat_source_power_35pc;
        private double wasteheat_source_power_30pc;
        private double wasteheat_source_power_25pc;
        private double wasteheat_source_power_20pc;
        private double wasteheat_source_power_15pc;
        private double wasteheat_source_power_10pc;
        private double wasteheat_source_power_8pc;
        private double wasteheat_source_power_6pc;
        private double wasteheat_source_power_4pc;
        private double wasteheat_source_power_2pc;

        private double source_temp_at_custom;
        private double source_temp_at_100pc;
        private double source_temp_at_90pc;
        private double source_temp_at_80pc;
        private double source_temp_at_70pc;
        private double source_temp_at_60pc;
        private double source_temp_at_50pc;
        private double source_temp_at_45pc;
        private double source_temp_at_40pc;
        private double source_temp_at_35pc;
        private double source_temp_at_30pc;
        private double source_temp_at_25pc;
        private double source_temp_at_20pc;
        private double source_temp_at_15pc;
        private double source_temp_at_10pc;
        private double source_temp_at_8pc;
        private double source_temp_at_6pc;
        private double source_temp_at_4pc;
        private double source_temp_at_2pc;

        private double resting_radiator_temp_at_custom;
        private double resting_radiator_temp_at_100pcnt;
        private double resting_radiator_temp_at_90pcnt;
        private double resting_radiator_temp_at_80pcnt;
        private double resting_radiator_temp_at_70pcnt;
        private double resting_radiator_temp_at_60pcnt;
        private double resting_radiator_temp_at_50pcnt;
        private double resting_radiator_temp_at_45pcnt;
        private double resting_radiator_temp_at_40pcnt;
        private double resting_radiator_temp_at_35pcnt;
        private double resting_radiator_temp_at_30pcnt;
        private double resting_radiator_temp_at_25pcnt;
        private double resting_radiator_temp_at_20pcnt;
        private double resting_radiator_temp_at_15pcnt;
        private double resting_radiator_temp_at_10pcnt;
        private double resting_radiator_temp_at_8pcnt;
        private double resting_radiator_temp_at_6pcnt;
        private double resting_radiator_temp_at_4pcnt;
        private double resting_radiator_temp_at_2pcnt;

        private double generator_efficiency_at_custom;
        private double generator_efficiency_at_100pcnt;
        private double generator_efficiency_at_90pcnt;
        private double generator_efficiency_at_80pcnt;
        private double generator_efficiency_at_70pcnt;
        private double generator_efficiency_at_60pcnt;
        private double generator_efficiency_at_50pcnt;
        private double generator_efficiency_at_45pcnt;
        private double generator_efficiency_at_40pcnt;
        private double generator_efficiency_at_35pcnt;
        private double generator_efficiency_at_30pcnt;
        private double generator_efficiency_at_25pcnt;
        private double generator_efficiency_at_20pcnt;
        private double generator_efficiency_at_15pcnt;
        private double generator_efficiency_at_10pcnt;
        private double generator_efficiency_at_8pcnt;
        private double generator_efficiency_at_6pcnt;
        private double generator_efficiency_at_4pcnt;
        private double generator_efficiency_at_2pcnt;

        private double electricPowerAt100;
        private double electricPowerAt90;
        private double electricPowerAt80;
        private double electricPowerAt70;
        private double electricPowerAt60;
        private double electricPowerAt50;
        private double electricPowerAt45;
        private double electricPowerAt40;
        private double electricPowerAt35;
        private double electricPowerAt30;
        private double electricPowerAt25;
        private double electricPowerAt20;
        private double electricPowerAt15;
        private double electricPowerAt10;
        private double electricPowerAt8;
        private double electricPowerAt6;
        private double electricPowerAt4;
        private double electricPowerAt2;
        private double electricPowerAtCustom;

        private double _totalSourcePower;
        private double _radMaxDissipation;
        private double _totalArea;
        private double _averageRadTemp;
        private double _bestScenarioElectricPower;
        private double _dryMass;
        private double _wetMass;

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
            var thermalEngines = new List<ThermalEngineController>();
            var beamedReceivers = new List<BeamedPowerReceiver>();
            var variableEngines = new List<FusionECU2>();
            var fusionEngines = new List<DaedalusEngineController>();
            var beamedTransmitter = new List<BeamedPowerTransmitter>();

            _dryMass = 0;
            _wetMass = 0;
            _totalSourcePower = 0;
            customScenarioFraction = customScenarioPercentage * 0.01f;

            foreach (var part in EditorLogic.fetch.ship.parts)
            {
                _dryMass += part.mass;
                _wetMass += part.Resources.Sum(m => m.amount * m.info.density);

                thermalSources.AddRange(part.FindModulesImplementing<IPowerSource>());
                radiators.AddRange(part.FindModulesImplementing<FNRadiator>());
                generators.AddRange(part.FindModulesImplementing<FNGenerator>());
                thermalEngines.AddRange(part.FindModulesImplementing<ThermalEngineController>());
                beamedReceivers.AddRange(part.FindModulesImplementing<BeamedPowerReceiver>());
                variableEngines.AddRange(part.FindModulesImplementing<FusionECU2>());
                fusionEngines.AddRange(part.FindModulesImplementing<DaedalusEngineController>());
                beamedTransmitter.AddRange(part.FindModulesImplementing<BeamedPowerTransmitter>());
            }

            wasteheat_source_power_custom = 0;
            wasteheat_source_power_100pc = 0;
            wasteheat_source_power_90pc = 0;
            wasteheat_source_power_80pc = 0;
            wasteheat_source_power_70pc = 0;
            wasteheat_source_power_60pc = 0;
            wasteheat_source_power_50pc = 0;
            wasteheat_source_power_45pc = 0;
            wasteheat_source_power_40pc = 0;
            wasteheat_source_power_35pc = 0;
            wasteheat_source_power_30pc = 0;
            wasteheat_source_power_25pc = 0;
            wasteheat_source_power_20pc = 0;
            wasteheat_source_power_15pc = 0;
            wasteheat_source_power_10pc = 0;
            wasteheat_source_power_8pc = 0;
            wasteheat_source_power_6pc = 0;
            wasteheat_source_power_4pc = 0;
            wasteheat_source_power_2pc = 0;

            source_temp_at_custom = double.MaxValue;
            source_temp_at_100pc = double.MaxValue;
            source_temp_at_90pc = double.MaxValue;
            source_temp_at_80pc = double.MaxValue;
            source_temp_at_70pc = double.MaxValue;
            source_temp_at_60pc = double.MaxValue;
            source_temp_at_50pc = double.MaxValue;
            source_temp_at_45pc = double.MaxValue;
            source_temp_at_40pc = double.MaxValue;
            source_temp_at_35pc = double.MaxValue;
            source_temp_at_30pc = double.MaxValue;
            source_temp_at_25pc = double.MaxValue;
            source_temp_at_20pc = double.MaxValue;
            source_temp_at_15pc = double.MaxValue;
            source_temp_at_10pc = double.MaxValue;
            source_temp_at_8pc = double.MaxValue;
            source_temp_at_6pc = double.MaxValue;
            source_temp_at_4pc = double.MaxValue;
            source_temp_at_2pc = double.MaxValue;

            double totalTemperaturePowerAt100Percent = 0;
            double totalTemperaturePowerAt90Percent = 0;
            double totalTemperaturePowerAt80Percent = 0;
            double totalTemperaturePowerAt70Percent = 0;
            double totalTemperaturePowerAt60Percent = 0;
            double totalTemperaturePowerAt50Percent = 0;
            double totalTemperaturePowerAt45Percent = 0;
            double totalTemperaturePowerAt40Percent = 0;
            double totalTemperaturePowerAt35Percent = 0;
            double totalTemperaturePowerAt30Percent = 0;
            double totalTemperaturePowerAt25Percent = 0;
            double totalTemperaturePowerAt20Percent = 0;
            double totalTemperaturePowerAt15Percent = 0;
            double totalTemperaturePowerAt10Percent = 0;
            double totalTemperaturePowerAt8Percent = 0;
            double totalTemperaturePowerAt6Percent = 0;
            double totalTemperaturePowerAt4Percent = 0;
            double totalTemperaturePowerAt2Percent = 0;
            double totalTemperaturePowerAtCustom = 0;

            // first calculate reactors
            foreach (IPowerSource powerSource in thermalSources)
            {
                _totalSourcePower += powerSource.MaximumPower;

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

                var coreTempAtRadiatorTempAt100Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_100pcnt);
                var coreTempAtRadiatorTempAt90Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_90pcnt);
                var coreTempAtRadiatorTempAt80Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_80pcnt);
                var coreTempAtRadiatorTempAt70Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_70pcnt);
                var coreTempAtRadiatorTempAt60Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_60pcnt);
                var coreTempAtRadiatorTempAt50Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_50pcnt);
                var coreTempAtRadiatorTempAt45Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_45pcnt);
                var coreTempAtRadiatorTempAt40Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_40pcnt);
                var coreTempAtRadiatorTempAt35Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_35pcnt);
                var coreTempAtRadiatorTempAt30Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_30pcnt);
                var coreTempAtRadiatorTempAt25Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_25pcnt);
                var coreTempAtRadiatorTempAt20Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_20pcnt);
                var coreTempAtRadiatorTempAt15Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_15pcnt);
                var coreTempAtRadiatorTempAt10Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_10pcnt);
                var coreTempAtRadiatorTempAt8Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_8pcnt);
                var coreTempAtRadiatorTempAt6Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_6pcnt);
                var coreTempAtRadiatorTempAt4Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_4pcnt);
                var coreTempAtRadiatorTempAt2Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_2pcnt);
                var coreTempAtRadiatorTempAtCustom = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_custom);

                var effectivePowerAtCustom = (1 - generator_efficiency_at_custom) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAtCustom) * customScenarioFraction);
                var effectivePowerAt100Percent = (1 - generator_efficiency_at_100pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt100Percent));
                var effectivePowerAt90Percent = (1 - generator_efficiency_at_90pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt90Percent) * 0.90);
                var effectivePowerAt80Percent = (1 - generator_efficiency_at_80pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt80Percent) * 0.80);
                var effectivePowerAt70Percent = (1 - generator_efficiency_at_70pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt70Percent) * 0.70);
                var effectivePowerAt60Percent = (1 - generator_efficiency_at_60pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt60Percent) * 0.60);
                var effectivePowerAt50Percent = (1 - generator_efficiency_at_50pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt50Percent) * 0.50);
                var effectivePowerAt45Percent = (1 - generator_efficiency_at_45pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt45Percent) * 0.45);
                var effectivePowerAt40Percent = (1 - generator_efficiency_at_40pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt40Percent) * 0.40);
                var effectivePowerAt35Percent = (1 - generator_efficiency_at_35pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt35Percent) * 0.35);
                var effectivePowerAt30Percent = (1 - generator_efficiency_at_30pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt30Percent) * 0.30);
                var effectivePowerAt25Percent = (1 - generator_efficiency_at_25pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt25Percent) * 0.25);
                var effectivePowerAt20Percent = (1 - generator_efficiency_at_20pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt20Percent) * 0.20);
                var effectivePowerAt15Percent = (1 - generator_efficiency_at_15pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt15Percent) * 0.15);
                var effectivePowerAt10Percent = (1 - generator_efficiency_at_10pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt10Percent) * 0.10);
                var effectivePowerAt8Percent = (1 - generator_efficiency_at_8pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt8Percent) * 0.08);
                var effectivePowerAt6Percent = (1 - generator_efficiency_at_6pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt6Percent) * 0.06);
                var effectivePowerAt4Percent = (1 - generator_efficiency_at_4pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt4Percent) * 0.04);
                var effectivePowerAt2Percent = (1 - generator_efficiency_at_2pcnt) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt2Percent) * 0.02);

                totalTemperaturePowerAt100Percent += coreTempAtRadiatorTempAt100Percent * effectivePowerAt100Percent;
                totalTemperaturePowerAt90Percent += coreTempAtRadiatorTempAt90Percent * effectivePowerAt90Percent;
                totalTemperaturePowerAt80Percent += coreTempAtRadiatorTempAt80Percent * effectivePowerAt80Percent;
                totalTemperaturePowerAt70Percent += coreTempAtRadiatorTempAt70Percent * effectivePowerAt70Percent;
                totalTemperaturePowerAt60Percent += coreTempAtRadiatorTempAt60Percent * effectivePowerAt60Percent;
                totalTemperaturePowerAt50Percent += coreTempAtRadiatorTempAt50Percent * effectivePowerAt50Percent;
                totalTemperaturePowerAt45Percent += coreTempAtRadiatorTempAt45Percent * effectivePowerAt45Percent;
                totalTemperaturePowerAt40Percent += coreTempAtRadiatorTempAt40Percent * effectivePowerAt40Percent;
                totalTemperaturePowerAt35Percent += coreTempAtRadiatorTempAt35Percent * effectivePowerAt35Percent;
                totalTemperaturePowerAt30Percent += coreTempAtRadiatorTempAt30Percent * effectivePowerAt30Percent;
                totalTemperaturePowerAt25Percent += coreTempAtRadiatorTempAt25Percent * effectivePowerAt25Percent;
                totalTemperaturePowerAt20Percent += coreTempAtRadiatorTempAt20Percent * effectivePowerAt20Percent;
                totalTemperaturePowerAt15Percent += coreTempAtRadiatorTempAt15Percent * effectivePowerAt15Percent;
                totalTemperaturePowerAt10Percent += coreTempAtRadiatorTempAt10Percent * effectivePowerAt10Percent;
                totalTemperaturePowerAt8Percent += coreTempAtRadiatorTempAt8Percent * effectivePowerAt8Percent;
                totalTemperaturePowerAt6Percent += coreTempAtRadiatorTempAt6Percent * effectivePowerAt6Percent;
                totalTemperaturePowerAt4Percent += coreTempAtRadiatorTempAt4Percent * effectivePowerAt4Percent;
                totalTemperaturePowerAt2Percent += coreTempAtRadiatorTempAt2Percent * effectivePowerAt2Percent;
                totalTemperaturePowerAtCustom += coreTempAtRadiatorTempAtCustom * effectivePowerAtCustom;

                wasteheat_source_power_100pc += effectivePowerAt100Percent;
                wasteheat_source_power_90pc += effectivePowerAt90Percent;
                wasteheat_source_power_80pc += effectivePowerAt80Percent;
                wasteheat_source_power_70pc += effectivePowerAt70Percent;
                wasteheat_source_power_60pc += effectivePowerAt60Percent;
                wasteheat_source_power_50pc += effectivePowerAt50Percent;
                wasteheat_source_power_45pc += effectivePowerAt45Percent;
                wasteheat_source_power_40pc += effectivePowerAt40Percent;
                wasteheat_source_power_35pc += effectivePowerAt35Percent;
                wasteheat_source_power_30pc += effectivePowerAt30Percent;
                wasteheat_source_power_25pc += effectivePowerAt25Percent;
                wasteheat_source_power_20pc += effectivePowerAt20Percent;
                wasteheat_source_power_15pc += effectivePowerAt15Percent;
                wasteheat_source_power_10pc += effectivePowerAt10Percent;
                wasteheat_source_power_8pc += effectivePowerAt8Percent;
                wasteheat_source_power_6pc += effectivePowerAt6Percent;
                wasteheat_source_power_4pc += effectivePowerAt4Percent;
                wasteheat_source_power_2pc += effectivePowerAt2Percent;
                wasteheat_source_power_custom += effectivePowerAtCustom;
            }

            // calculated weighted core temperatures
            if (wasteheat_source_power_custom > 0) source_temp_at_custom = totalTemperaturePowerAtCustom / wasteheat_source_power_custom;
            if (wasteheat_source_power_100pc > 0) source_temp_at_100pc = totalTemperaturePowerAt100Percent / wasteheat_source_power_100pc;
            if (wasteheat_source_power_90pc > 0) source_temp_at_90pc = totalTemperaturePowerAt90Percent / wasteheat_source_power_90pc;
            if (wasteheat_source_power_80pc > 0) source_temp_at_80pc = totalTemperaturePowerAt80Percent / wasteheat_source_power_80pc;
            if (wasteheat_source_power_70pc > 0) source_temp_at_70pc = totalTemperaturePowerAt70Percent / wasteheat_source_power_70pc;
            if (wasteheat_source_power_60pc > 0) source_temp_at_60pc = totalTemperaturePowerAt60Percent / wasteheat_source_power_60pc;
            if (wasteheat_source_power_50pc > 0) source_temp_at_50pc = totalTemperaturePowerAt50Percent / wasteheat_source_power_50pc;
            if (wasteheat_source_power_45pc > 0) source_temp_at_45pc = totalTemperaturePowerAt45Percent / wasteheat_source_power_45pc;
            if (wasteheat_source_power_40pc > 0) source_temp_at_40pc = totalTemperaturePowerAt40Percent / wasteheat_source_power_40pc;
            if (wasteheat_source_power_35pc > 0) source_temp_at_35pc = totalTemperaturePowerAt35Percent / wasteheat_source_power_35pc;
            if (wasteheat_source_power_30pc > 0) source_temp_at_30pc = totalTemperaturePowerAt30Percent / wasteheat_source_power_30pc;
            if (wasteheat_source_power_25pc > 0) source_temp_at_25pc = totalTemperaturePowerAt25Percent / wasteheat_source_power_25pc;
            if (wasteheat_source_power_20pc > 0) source_temp_at_20pc = totalTemperaturePowerAt20Percent / wasteheat_source_power_20pc;
            if (wasteheat_source_power_15pc > 0) source_temp_at_15pc = totalTemperaturePowerAt15Percent / wasteheat_source_power_15pc;
            if (wasteheat_source_power_10pc > 0) source_temp_at_10pc = totalTemperaturePowerAt10Percent / wasteheat_source_power_10pc;
            if (wasteheat_source_power_8pc > 0) source_temp_at_8pc = totalTemperaturePowerAt8Percent / wasteheat_source_power_8pc;
            if (wasteheat_source_power_6pc > 0) source_temp_at_6pc = totalTemperaturePowerAt6Percent / wasteheat_source_power_6pc;
            if (wasteheat_source_power_4pc > 0) source_temp_at_4pc = totalTemperaturePowerAt4Percent / wasteheat_source_power_4pc;
            if (wasteheat_source_power_2pc > 0) source_temp_at_2pc = totalTemperaturePowerAt2Percent / wasteheat_source_power_2pc;

            // calculate effect of on demand beamed power
            foreach (BeamedPowerReceiver beamedReceiver in beamedReceivers)
            {
                // only count receiver that are activated
                if (!beamedReceiver.receiverIsEnabled)
                    continue;

                var maxWasteheatProduction = beamedReceiver.MaximumRecievePower * (1 - beamedReceiver.activeBandwidthConfiguration.MaxEfficiencyPercentage * 0.01);

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_90pc += maxWasteheatProduction * 0.90;
                wasteheat_source_power_80pc += maxWasteheatProduction * 0.80;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.70;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.60;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.50;
                wasteheat_source_power_45pc += maxWasteheatProduction * 0.45;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.40;
                wasteheat_source_power_35pc += maxWasteheatProduction * 0.35;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.30;
                wasteheat_source_power_25pc += maxWasteheatProduction * 0.25;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.20;
                wasteheat_source_power_15pc += maxWasteheatProduction * 0.15;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.10;
                wasteheat_source_power_8pc += maxWasteheatProduction * 0.08;
                wasteheat_source_power_6pc += maxWasteheatProduction * 0.06;
                wasteheat_source_power_4pc += maxWasteheatProduction * 0.04;
                wasteheat_source_power_2pc += maxWasteheatProduction * 0.02;
                wasteheat_source_power_custom += maxWasteheatProduction * customScenarioFraction;
            }

            foreach (BeamedPowerTransmitter beamedPowerTransmitter in beamedTransmitter)
            {
                if (!beamedPowerTransmitter.IsEnabled)
                    continue;

                var wasteheatFraction = 1 - beamedPowerTransmitter.activeBeamGenerator.efficiencyPercentage * 0.01;
                var powerCapacity = beamedPowerTransmitter.PowerCapacity;

                wasteheat_source_power_custom += Math.Min(electricPowerAtCustom, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_100pc += Math.Min(electricPowerAt100, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_90pc += Math.Min(electricPowerAt90, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_80pc += Math.Min(electricPowerAt80, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_70pc += Math.Min(electricPowerAt70, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_60pc += Math.Min(electricPowerAt60, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_50pc += Math.Min(electricPowerAt50, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_45pc += Math.Min(electricPowerAt45, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_40pc += Math.Min(electricPowerAt40, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_35pc += Math.Min(electricPowerAt35, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_30pc += Math.Min(electricPowerAt30, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_25pc += Math.Min(electricPowerAt25, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_20pc += Math.Min(electricPowerAt20, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_15pc += Math.Min(electricPowerAt15, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_10pc += Math.Min(electricPowerAt10, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_8pc += Math.Min(electricPowerAt8, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_6pc += Math.Min(electricPowerAt6, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_4pc += Math.Min(electricPowerAt4, powerCapacity) * wasteheatFraction;
                wasteheat_source_power_2pc += Math.Min(electricPowerAt2, powerCapacity) * wasteheatFraction;
            }

            var engineThrottleRatio = 0.01 * engineThrottlePercentage;

            foreach (ThermalEngineController thermalNozzle in thermalEngines)
            {
                var maxWasteheatProduction = engineThrottleRatio * thermalNozzle.ReactorWasteheatModifier * thermalNozzle.AttachedReactor.NormalisedMaximumPower;

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_90pc += maxWasteheatProduction * 0.90;
                wasteheat_source_power_80pc += maxWasteheatProduction * 0.80;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.70;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.60;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.50;
                wasteheat_source_power_45pc += maxWasteheatProduction * 0.45;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.40;
                wasteheat_source_power_35pc += maxWasteheatProduction * 0.35;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.30;
                wasteheat_source_power_25pc += maxWasteheatProduction * 0.25;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.20;
                wasteheat_source_power_15pc += maxWasteheatProduction * 0.15;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.10;
                wasteheat_source_power_8pc += maxWasteheatProduction * 0.08;
                wasteheat_source_power_6pc += maxWasteheatProduction * 0.06;
                wasteheat_source_power_4pc += maxWasteheatProduction * 0.04;
                wasteheat_source_power_2pc += maxWasteheatProduction * 0.02;
                wasteheat_source_power_custom += maxWasteheatProduction * customScenarioFraction;
            }

            foreach (FusionECU2 variableEngine in variableEngines)
            {
                var maxWasteheatProduction = engineThrottleRatio * variableEngine.fusionWasteHeatMax;

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_90pc += maxWasteheatProduction * 0.90;
                wasteheat_source_power_80pc += maxWasteheatProduction * 0.80;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.70;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.60;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.50;
                wasteheat_source_power_45pc += maxWasteheatProduction * 0.45;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.40;
                wasteheat_source_power_35pc += maxWasteheatProduction * 0.35;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.30;
                wasteheat_source_power_25pc += maxWasteheatProduction * 0.25;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.20;
                wasteheat_source_power_15pc += maxWasteheatProduction * 0.15;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.10;
                wasteheat_source_power_8pc += maxWasteheatProduction * 0.08;
                wasteheat_source_power_6pc += maxWasteheatProduction * 0.06;
                wasteheat_source_power_4pc += maxWasteheatProduction * 0.04;
                wasteheat_source_power_2pc += maxWasteheatProduction * 0.02;
                wasteheat_source_power_custom += maxWasteheatProduction * customScenarioFraction;
            }

            foreach (DaedalusEngineController fusionEngine in fusionEngines)
            {
                var maxWasteheatProduction = 0.01 * engineThrottlePercentage * fusionEngine.wasteHeat;

                wasteheat_source_power_100pc += maxWasteheatProduction;
                wasteheat_source_power_90pc += maxWasteheatProduction * 0.90;
                wasteheat_source_power_80pc += maxWasteheatProduction * 0.80;
                wasteheat_source_power_70pc += maxWasteheatProduction * 0.70;
                wasteheat_source_power_60pc += maxWasteheatProduction * 0.60;
                wasteheat_source_power_50pc += maxWasteheatProduction * 0.50;
                wasteheat_source_power_45pc += maxWasteheatProduction * 0.45;
                wasteheat_source_power_40pc += maxWasteheatProduction * 0.40;
                wasteheat_source_power_35pc += maxWasteheatProduction * 0.35;
                wasteheat_source_power_30pc += maxWasteheatProduction * 0.30;
                wasteheat_source_power_25pc += maxWasteheatProduction * 0.25;
                wasteheat_source_power_20pc += maxWasteheatProduction * 0.20;
                wasteheat_source_power_15pc += maxWasteheatProduction * 0.15;
                wasteheat_source_power_10pc += maxWasteheatProduction * 0.10;
                wasteheat_source_power_8pc += maxWasteheatProduction * 0.08;
                wasteheat_source_power_6pc += maxWasteheatProduction * 0.06;
                wasteheat_source_power_4pc += maxWasteheatProduction * 0.04;
                wasteheat_source_power_2pc += maxWasteheatProduction * 0.02;
                wasteheat_source_power_custom += maxWasteheatProduction * customScenarioFraction;
            }

            CalculateGeneratedElectricPower(generators);

            _numberOfRadiators = 0;
            _radMaxDissipation = 0;
            _averageRadTemp = 0;
            _totalArea = 0;

            foreach (FNRadiator radiator in radiators)
            {
                _totalArea += radiator.BaseRadiatorArea;
                var maxRadTemperature = radiator.MaxRadiatorTemperature;
                maxRadTemperature = Math.Min(maxRadTemperature, source_temp_at_100pc);
                _numberOfRadiators++;
                var tempToPowerFour = maxRadTemperature * maxRadTemperature * maxRadTemperature * maxRadTemperature;
                _radMaxDissipation += GameConstants.stefan_const * radiator.EffectiveRadiatorArea * tempToPowerFour / 1e6;
                _averageRadTemp += maxRadTemperature;
            }
            _averageRadTemp = _numberOfRadiators != 0 ? _averageRadTemp / _numberOfRadiators : double.NaN;

            var radRatioCustom = wasteheat_source_power_custom / _radMaxDissipation;
            var radRatio100Pc = wasteheat_source_power_100pc / _radMaxDissipation;
            var radRatio90Pc = wasteheat_source_power_90pc / _radMaxDissipation;
            var radRatio80Pc = wasteheat_source_power_80pc / _radMaxDissipation;
            var radRatio70Pc = wasteheat_source_power_70pc / _radMaxDissipation;
            var radRatio60Pc = wasteheat_source_power_60pc / _radMaxDissipation;
            var radRatio50Pc = wasteheat_source_power_50pc / _radMaxDissipation;
            var radRatio45Pc = wasteheat_source_power_45pc / _radMaxDissipation;
            var radRatio40Pc = wasteheat_source_power_40pc / _radMaxDissipation;
            var radRatio35Pc = wasteheat_source_power_35pc / _radMaxDissipation;
            var radRatio30Pc = wasteheat_source_power_30pc / _radMaxDissipation;
            var radRatio25Pc = wasteheat_source_power_25pc / _radMaxDissipation;
            var radRatio20Pc = wasteheat_source_power_20pc / _radMaxDissipation;
            var radRatio15Pc = wasteheat_source_power_15pc / _radMaxDissipation;
            var radRatio10Pc = wasteheat_source_power_10pc / _radMaxDissipation;
            var radRatio8Pc = wasteheat_source_power_8pc / _radMaxDissipation;
            var radRatio6Pc = wasteheat_source_power_6pc / _radMaxDissipation;
            var radRatio4Pc = wasteheat_source_power_4pc / _radMaxDissipation;
            var radRatio2Pc = wasteheat_source_power_2pc / _radMaxDissipation;

            resting_radiator_temp_at_custom = (!radRatioCustom.IsInfinityOrNaN() ? Math.Pow(radRatioCustom, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_100pcnt = (!radRatio100Pc.IsInfinityOrNaN() ? Math.Pow(radRatio100Pc,0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_90pcnt = (!radRatio90Pc.IsInfinityOrNaN() ? Math.Pow(radRatio90Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_80pcnt = (!radRatio80Pc.IsInfinityOrNaN() ? Math.Pow(radRatio80Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_70pcnt = (!radRatio70Pc.IsInfinityOrNaN() ? Math.Pow(radRatio70Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_60pcnt = (!radRatio60Pc.IsInfinityOrNaN() ? Math.Pow(radRatio60Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_50pcnt = (!radRatio50Pc.IsInfinityOrNaN() ? Math.Pow(radRatio50Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_45pcnt = (!radRatio45Pc.IsInfinityOrNaN() ? Math.Pow(radRatio45Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_40pcnt = (!radRatio40Pc.IsInfinityOrNaN() ? Math.Pow(radRatio40Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_35pcnt = (!radRatio30Pc.IsInfinityOrNaN() ? Math.Pow(radRatio35Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_30pcnt = (!radRatio30Pc.IsInfinityOrNaN() ? Math.Pow(radRatio30Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_25pcnt = (!radRatio25Pc.IsInfinityOrNaN() ? Math.Pow(radRatio25Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_20pcnt = (!radRatio20Pc.IsInfinityOrNaN() ? Math.Pow(radRatio20Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_15pcnt = (!radRatio15Pc.IsInfinityOrNaN() ? Math.Pow(radRatio15Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_10pcnt = (!radRatio10Pc.IsInfinityOrNaN() ? Math.Pow(radRatio10Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_8pcnt = (!radRatio8Pc.IsInfinityOrNaN() ? Math.Pow(radRatio8Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_6pcnt = (!radRatio6Pc.IsInfinityOrNaN() ? Math.Pow(radRatio6Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_4pcnt = (!radRatio4Pc.IsInfinityOrNaN() ? Math.Pow(radRatio4Pc, 0.25) : 0) * _averageRadTemp;
            resting_radiator_temp_at_2pcnt = (!radRatio2Pc.IsInfinityOrNaN() ? Math.Pow(radRatio2Pc, 0.25) : 0) * _averageRadTemp;

            var thermalGenerators = generators.Where(m => !m.chargedParticleMode).ToList();

            if (thermalGenerators.Count > 0)
            {
                var maximumGeneratedPower = thermalGenerators.Sum(m => m.maximumGeneratorPowerMJ);
                var averageMaxEfficiency = thermalGenerators.Sum(m => m.maxEfficiency * m.maximumGeneratorPowerMJ) / maximumGeneratedPower;

                _hasThermalGenerators = true;

                generator_efficiency_at_custom = CalculateGeneratorAtPercentage(source_temp_at_custom, resting_radiator_temp_at_custom, averageMaxEfficiency);
                generator_efficiency_at_100pcnt = CalculateGeneratorAtPercentage(source_temp_at_100pc, resting_radiator_temp_at_100pcnt, averageMaxEfficiency);
                generator_efficiency_at_90pcnt = CalculateGeneratorAtPercentage(source_temp_at_90pc, resting_radiator_temp_at_90pcnt, averageMaxEfficiency);
                generator_efficiency_at_80pcnt = CalculateGeneratorAtPercentage(source_temp_at_80pc, resting_radiator_temp_at_80pcnt, averageMaxEfficiency);
                generator_efficiency_at_70pcnt = CalculateGeneratorAtPercentage(source_temp_at_70pc, resting_radiator_temp_at_70pcnt, averageMaxEfficiency);
                generator_efficiency_at_60pcnt = CalculateGeneratorAtPercentage(source_temp_at_60pc, resting_radiator_temp_at_60pcnt, averageMaxEfficiency);
                generator_efficiency_at_50pcnt = CalculateGeneratorAtPercentage(source_temp_at_50pc, resting_radiator_temp_at_50pcnt, averageMaxEfficiency);
                generator_efficiency_at_45pcnt = CalculateGeneratorAtPercentage(source_temp_at_45pc, resting_radiator_temp_at_45pcnt, averageMaxEfficiency);
                generator_efficiency_at_40pcnt = CalculateGeneratorAtPercentage(source_temp_at_40pc, resting_radiator_temp_at_40pcnt, averageMaxEfficiency);
                generator_efficiency_at_35pcnt = CalculateGeneratorAtPercentage(source_temp_at_35pc, resting_radiator_temp_at_35pcnt, averageMaxEfficiency);
                generator_efficiency_at_30pcnt = CalculateGeneratorAtPercentage(source_temp_at_30pc, resting_radiator_temp_at_30pcnt, averageMaxEfficiency);
                generator_efficiency_at_25pcnt = CalculateGeneratorAtPercentage(source_temp_at_25pc, resting_radiator_temp_at_25pcnt, averageMaxEfficiency);
                generator_efficiency_at_20pcnt = CalculateGeneratorAtPercentage(source_temp_at_20pc, resting_radiator_temp_at_20pcnt, averageMaxEfficiency);
                generator_efficiency_at_15pcnt = CalculateGeneratorAtPercentage(source_temp_at_15pc, resting_radiator_temp_at_15pcnt, averageMaxEfficiency);
                generator_efficiency_at_10pcnt = CalculateGeneratorAtPercentage(source_temp_at_10pc, resting_radiator_temp_at_10pcnt, averageMaxEfficiency);
                generator_efficiency_at_8pcnt = CalculateGeneratorAtPercentage(source_temp_at_8pc, resting_radiator_temp_at_8pcnt, averageMaxEfficiency);
                generator_efficiency_at_6pcnt = CalculateGeneratorAtPercentage(source_temp_at_6pc, resting_radiator_temp_at_6pcnt, averageMaxEfficiency);
                generator_efficiency_at_4pcnt = CalculateGeneratorAtPercentage(source_temp_at_4pc, resting_radiator_temp_at_4pcnt, averageMaxEfficiency);
                generator_efficiency_at_2pcnt = CalculateGeneratorAtPercentage(source_temp_at_2pc, resting_radiator_temp_at_2pcnt, averageMaxEfficiency);
            }
            else
                _hasThermalGenerators = false;

            if (source_temp_at_custom >= double.MaxValue) source_temp_at_custom = -1;
            if (source_temp_at_100pc >= double.MaxValue) source_temp_at_100pc = -1;
            if (source_temp_at_90pc >= double.MaxValue) source_temp_at_90pc = -1;
            if (source_temp_at_80pc >= double.MaxValue) source_temp_at_80pc = -1;
            if (source_temp_at_70pc >= double.MaxValue) source_temp_at_70pc = -1;
            if (source_temp_at_60pc >= double.MaxValue) source_temp_at_60pc = -1;
            if (source_temp_at_50pc >= double.MaxValue) source_temp_at_50pc = -1;
            if (source_temp_at_45pc >= double.MaxValue) source_temp_at_45pc = -1;
            if (source_temp_at_40pc >= double.MaxValue) source_temp_at_40pc = -1;
            if (source_temp_at_35pc >= double.MaxValue) source_temp_at_35pc = -1;
            if (source_temp_at_30pc >= double.MaxValue) source_temp_at_30pc = -1;
            if (source_temp_at_25pc >= double.MaxValue) source_temp_at_25pc = -1;
            if (source_temp_at_20pc >= double.MaxValue) source_temp_at_20pc = -1;
            if (source_temp_at_15pc >= double.MaxValue) source_temp_at_15pc = -1;
            if (source_temp_at_10pc >= double.MaxValue) source_temp_at_10pc = -1;
            if (source_temp_at_8pc >= double.MaxValue) source_temp_at_8pc = -1;
            if (source_temp_at_6pc >= double.MaxValue) source_temp_at_6pc = -1;
            if (source_temp_at_4pc >= double.MaxValue) source_temp_at_4pc = -1;
            if (source_temp_at_2pc >= double.MaxValue) source_temp_at_2pc = -1;
        }

        private double CalculateGeneratorAtPercentage(double sourceTemperature, double restingTemperature, double efficiency)
        {
            if (sourceTemperature >= double.MaxValue || restingTemperature.IsInfinityOrNaN())
                return 0;

            return Math.Max(efficiency * (1 - restingTemperature / source_temp_at_100pc), 0);
        }

        private void CalculateGeneratedElectricPower(List<FNGenerator> generators)
        {
            electricPowerAtCustom = 0;
            electricPowerAt100 = 0;
            electricPowerAt90 = 0;
            electricPowerAt80 = 0;
            electricPowerAt70 = 0;
            electricPowerAt60 = 0;
            electricPowerAt50 = 0;
            electricPowerAt45 = 0;
            electricPowerAt40 = 0;
            electricPowerAt35 = 0;
            electricPowerAt30 = 0;
            electricPowerAt25 = 0;
            electricPowerAt20 = 0;
            electricPowerAt15 = 0;
            electricPowerAt10 = 0;
            electricPowerAt8 = 0;
            electricPowerAt6 = 0;
            electricPowerAt4 = 0;
            electricPowerAt2 = 0;

            foreach (FNGenerator generator in generators)
            {
                if (generator.chargedParticleMode)
                {
                    electricPowerAt100 += generator.maximumGeneratorPowerMJ;
                }
                else
                {
                    var generatorMaximumGeneratorPower = generator.maximumGeneratorPowerMJ;

                    electricPowerAt100 += generatorMaximumGeneratorPower * generator_efficiency_at_100pcnt;

                    if (generator.isLimitedByMinThrottle)
                    {
                        electricPowerAt90 += electricPowerAt100;
                        electricPowerAt80 += electricPowerAt100;
                        electricPowerAt70 += electricPowerAt100;
                        electricPowerAt60 += electricPowerAt100;
                        electricPowerAt50 += electricPowerAt100;
                        electricPowerAt45 += electricPowerAt100;
                        electricPowerAt40 += electricPowerAt100;
                        electricPowerAt35 += electricPowerAt100;
                        electricPowerAt30 += electricPowerAt100;
                        electricPowerAt25 += electricPowerAt100;
                        electricPowerAt20 += electricPowerAt100;
                        electricPowerAt15 += electricPowerAt100;
                        electricPowerAt10 += electricPowerAt100;
                        electricPowerAt8 += electricPowerAt100;
                        electricPowerAt6 += electricPowerAt100;
                        electricPowerAt4 += electricPowerAt100;
                        electricPowerAt2 += electricPowerAt100;
                        electricPowerAtCustom += electricPowerAt100;
                        continue;
                    }

                    electricPowerAt90 += generatorMaximumGeneratorPower * generator_efficiency_at_90pcnt * 0.90;
                    electricPowerAt80 += generatorMaximumGeneratorPower * generator_efficiency_at_80pcnt * 0.80;
                    electricPowerAt70 += generatorMaximumGeneratorPower * generator_efficiency_at_70pcnt * 0.70;
                    electricPowerAt60 += generatorMaximumGeneratorPower * generator_efficiency_at_60pcnt * 0.60;
                    electricPowerAt50 += generatorMaximumGeneratorPower * generator_efficiency_at_50pcnt * 0.50;
                    electricPowerAt45 += generatorMaximumGeneratorPower * generator_efficiency_at_45pcnt * 0.45;
                    electricPowerAt40 += generatorMaximumGeneratorPower * generator_efficiency_at_40pcnt * 0.40;
                    electricPowerAt35 += generatorMaximumGeneratorPower * generator_efficiency_at_35pcnt * 0.35;
                    electricPowerAt30 += generatorMaximumGeneratorPower * generator_efficiency_at_30pcnt * 0.30;
                    electricPowerAt25 += generatorMaximumGeneratorPower * generator_efficiency_at_25pcnt * 0.25;
                    electricPowerAt20 += generatorMaximumGeneratorPower * generator_efficiency_at_20pcnt * 0.20;
                    electricPowerAt15 += generatorMaximumGeneratorPower * generator_efficiency_at_15pcnt * 0.15;
                    electricPowerAt10 += generatorMaximumGeneratorPower * generator_efficiency_at_10pcnt * 0.10;
                    electricPowerAt8 += generatorMaximumGeneratorPower * generator_efficiency_at_8pcnt * 0.08;
                    electricPowerAt6 += generatorMaximumGeneratorPower * generator_efficiency_at_6pcnt * 0.06;
                    electricPowerAt4 += generatorMaximumGeneratorPower * generator_efficiency_at_4pcnt * 0.04;
                    electricPowerAt2 += generatorMaximumGeneratorPower * generator_efficiency_at_2pcnt * 0.02;
                    electricPowerAtCustom += generatorMaximumGeneratorPower * generator_efficiency_at_custom * customScenarioFraction;
                }
            }

            _bestScenarioPercentage = 0;
            _bestScenarioElectricPower = 0;

            GetBestPowerAndPercentage(100, electricPowerAt100);
            GetBestPowerAndPercentage(90, electricPowerAt90);
            GetBestPowerAndPercentage(80, electricPowerAt80);
            GetBestPowerAndPercentage(70, electricPowerAt70);
            GetBestPowerAndPercentage(60, electricPowerAt60);
            GetBestPowerAndPercentage(50, electricPowerAt50);
            GetBestPowerAndPercentage(45, electricPowerAt45);
            GetBestPowerAndPercentage(40, electricPowerAt40);
            GetBestPowerAndPercentage(35, electricPowerAt35);
            GetBestPowerAndPercentage(30, electricPowerAt30);
            GetBestPowerAndPercentage(25, electricPowerAt25);
            GetBestPowerAndPercentage(20, electricPowerAt20);
            GetBestPowerAndPercentage(15, electricPowerAt15);
            GetBestPowerAndPercentage(10, electricPowerAt10);
            GetBestPowerAndPercentage(8, electricPowerAt8);
            GetBestPowerAndPercentage(6, electricPowerAt6);
            GetBestPowerAndPercentage(4, electricPowerAt4);
            GetBestPowerAndPercentage(2, electricPowerAt2);
        }

        private void GetBestPowerAndPercentage (int percentage, double scenarioElectricPower)
        {
            if (scenarioElectricPower < _bestScenarioElectricPower) return;

            _bestScenarioPercentage = percentage;
            _bestScenarioElectricPower = scenarioElectricPower;
        }

        protected void OnGUI()
        {
            if (renderWindow)
                windowPosition = GUILayout.Window(_thermalWindowId, windowPosition, Window, Localizer.Format("#LOC_KSPIE_VABThermalUI_title"));//"Interstellar Thermal Mechanics Helper"
        }

        private void Window(int windowId)
        {
            if (blue_label == null)
                blue_label = new GUIStyle(GUI.skin.label) {normal = {textColor = new Color(0, 191/255f, 255/255f, 1f) } };
            if (green_label == null)
                green_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.green}};
            if (red_label == null)
                red_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.red}};
            if (orange_label == null)
                orange_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
            if (bold_label == null)
                bold_label = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold};

            var guiLabelWidth = GUILayout.MinWidth(LabelWidth);
            var guiValueWidth = GUILayout.MinWidth(ValueWidth);

            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                renderWindow = false;

            GUIStyle radiatorLabel = blue_label;
            if (_bestScenarioPercentage < 100)
            {
                radiatorLabel = green_label;
                if (_radMaxDissipation < _totalSourcePower)
                {
                    radiatorLabel = orange_label;
                    if (_radMaxDissipation < _totalSourcePower * 0.3)
                        radiatorLabel = red_label;
                }
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_EngineThrottlePercentage"), GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            engineThrottlePercentage = GUILayout.HorizontalSlider(engineThrottlePercentage, 0, 100, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(engineThrottlePercentage.ToString("0.0") + " %", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalHeatProduction"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Heat Production:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_totalSourcePower), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("Vessel Mass:"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Number of Radiators:"
            GUILayout.Label((_dryMass + _wetMass).ToString("0.000") + " t", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalAreaRadiators") + " (" + _numberOfRadiators + ")", bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Area Radiators:"
            GUILayout.Label(_totalArea.ToString("0.0") + " m\xB2", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorMaximumDissipation"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Maximum Dissipation:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_radMaxDissipation), radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Resting Electric Power at " + _bestScenarioPercentage + "% Power", bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(PluginHelper.getFormattedPowerString(_bestScenarioElectricPower), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Custom Reactor Power (Percentage)", GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            customScenarioPercentage = GUILayout.HorizontalSlider(customScenarioPercentage, 0, 100, GUILayout.ExpandWidth(true), guiLabelWidth);
            string customPercentageText = customScenarioPercentage.ToString("0.0") + "%";
            GUILayout.Label(customPercentageText, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wasteheat Production at " + customPercentageText, bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Heat Production:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(wasteheat_source_power_custom), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Thermal Source Temperature at " + customPercentageText, bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);
            string sourceTempString100 = source_temp_at_custom < 0 ? "N/A" : source_temp_at_custom.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString100, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string restingRadiatorTempAtCustomPercentageStr = !resting_radiator_temp_at_custom.IsInfinityOrNaN() ? resting_radiator_temp_at_custom.ToString("0.0") + " K" : "N/A";
            GUILayout.Label("Radiator Resting Temperature at " + customPercentageText, bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Resting Temperature at 30% Power:"
            GUILayout.Label(restingRadiatorTempAtCustomPercentageStr, radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            if (_hasThermalGenerators)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Resting Generator Efficiency at " + customPercentageText, bold_label, GUILayout.ExpandWidth(true), guiLabelWidth); //"Resting Generator Efficiency at 30% Power:"
                GUILayout.Label((generator_efficiency_at_custom * 100).ToString("0.00") + "%", radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Electric Power Output at " + customPercentageText, bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(PluginHelper.getFormattedPowerString(electricPowerAtCustom), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
