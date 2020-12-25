using FNPlugin.Beamedpower;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Powermanagement;
using FNPlugin.Powermanagement.Interfaces;
using FNPlugin.Propulsion;
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
        private const int LabelWidth = 300;
        private const int ValueWidth = 85;

        public static bool RenderWindow { get; set; }

        private int _maxIterations = 10;
        private int _bestScenarioPercentage;
        private int _numberOfRadiators;
        private int _thermalWindowId = 825462;
        private bool _hasThermalGenerators;

        private Rect windowPosition = new Rect(500, 500, LabelWidth + ValueWidth, 100);

        private GUIStyle _boldLabel;
        private GUIStyle _blueLabel;
        private GUIStyle _greenLabel;
        private GUIStyle _redLabel;
        private GUIStyle _orangeLabel;
        private GUIStyle _radiatorLabel;

        private float atmosphereDensity;
        private float submergedPercentage;
        private float externalTemperatureInKelvin = 290;
        private float engineThrottlePercentage = 100;
        private float customScenarioPercentage = 100;
        private float customScenarioFraction = 1;

        private double _wasteheatSourcePowerCustom;
        private double _wasteheatSourcePower100Pc;
        private double _wasteheatSourcePower90Pc;
        private double _wasteheatSourcePower80Pc;
        private double _wasteheatSourcePower70Pc;
        private double _wasteheatSourcePower60Pc;
        private double _wasteheatSourcePower50Pc;
        private double _wasteheatSourcePower45Pc;
        private double _wasteheatSourcePower40Pc;
        private double _wasteheatSourcePower35Pc;
        private double _wasteheatSourcePower30Pc;
        private double _wasteheatSourcePower25Pc;
        private double _wasteheatSourcePower20Pc;
        private double _wasteheatSourcePower15Pc;
        private double _wasteheatSourcePower10Pc;
        private double _wasteheatSourcePower8Pc;
        private double _wasteheatSourcePower6Pc;
        private double _wasteheatSourcePower4Pc;
        private double _wasteheatSourcePower2Pc;

        private double _averageSourceCoreTempAtCustom;
        private double _averageSourceCoreTempAt100Pc;

        private double _restingRadiatorTempAtCustomPct;
        private double _restingRadiatorTempAt100Percent;
        private double _restingRadiatorTempAt90Percent;
        private double _restingRadiatorTempAt80Percent;
        private double _restingRadiatorTempAt70Percent;
        private double _restingRadiatorTempAt60Percent;
        private double _restingRadiatorTempAt50Percent;
        private double _restingRadiatorTempAt45Percent;
        private double _restingRadiatorTempAt40Percent;
        private double _restingRadiatorTempAt35Percent;
        private double _restingRadiatorTempAt30Percent;
        private double _restingRadiatorTempAt25Percent;
        private double _restingRadiatorTempAt20Percent;
        private double _restingRadiatorTempAt15Percent;
        private double _restingRadiatorTempAt10Percent;
        private double _restingRadiatorTempAt8Percent;
        private double _restingRadiatorTempAt6Percent;
        private double _restingRadiatorTempAt4Percent;
        private double _restingRadiatorTempAt2Percent;

        private double _hotColdBathEfficiencyAtCustomPct;

        private double _electricPowerAtCustom;
        private double _electricPowerAt100;
        private double _electricPowerAt90;
        private double _electricPowerAt80;
        private double _electricPowerAt70;
        private double _electricPowerAt60;
        private double _electricPowerAt50;
        private double _electricPowerAt45;
        private double _electricPowerAt40;
        private double _electricPowerAt35;
        private double _electricPowerAt30;
        private double _electricPowerAt25;
        private double _electricPowerAt20;
        private double _electricPowerAt15;
        private double _electricPowerAt10;
        private double _electricPowerAt8;
        private double _electricPowerAt6;
        private double _electricPowerAt4;
        private double _electricPowerAt2;

        private double _totalSourcePower;
        private double _vesselMaxRadConvection;
        private double _vesselMaxRadDissipation;
        private double _vesselMaxRadConvectionAndDissipation;
        private double _vesselBaseRadiatorArea;
        private double _averageMaxRadTemp;
        private double _averageConvectiveBonus;
        private double _bestScenarioElectricPower;
        private double _dryMass;
        private double _wetMass;

        public void Start()
        {
            if (PluginHelper.usingToolbar)
                RenderWindow = false;
        }

        public void Update()
        {
            if (!RenderWindow)
                return;

            // thermal logic
            var thermalSources = new List<IFNPowerSource>();
            var radiators = new List<FNRadiator>();
            var generators = new List<FNGenerator>();
            var thermalEngines = new List<ThermalEngineController>();
            var beamedReceivers = new List<BeamedPowerReceiver>();
            var variableEngines = new List<FusionECU2>();
            var fusionEngines = new List<DaedalusEngineController>();
            var beamedTransmitter = new List<BeamedPowerTransmitter>();

            _dryMass = 0;
            _wetMass = 0;

            customScenarioFraction = customScenarioPercentage * 0.01f;

            foreach (var part in EditorLogic.fetch.ship.parts)
            {
                _dryMass += part.mass;
                _wetMass += part.Resources.Sum(m => m.amount * m.info.density);

                thermalSources.AddRange(part.FindModulesImplementing<IFNPowerSource>());
                radiators.AddRange(part.FindModulesImplementing<FNRadiator>());
                generators.AddRange(part.FindModulesImplementing<FNGenerator>());
                thermalEngines.AddRange(part.FindModulesImplementing<ThermalEngineController>());
                beamedReceivers.AddRange(part.FindModulesImplementing<BeamedPowerReceiver>());
                variableEngines.AddRange(part.FindModulesImplementing<FusionECU2>());
                fusionEngines.AddRange(part.FindModulesImplementing<DaedalusEngineController>());
                beamedTransmitter.AddRange(part.FindModulesImplementing<BeamedPowerTransmitter>());
            }

            for (var iteration = 0; iteration < _maxIterations; iteration++)
            {
                CalculatePowerBalance(thermalSources, beamedReceivers, beamedTransmitter, thermalEngines, variableEngines, fusionEngines, generators, radiators);
            }
        }

        private void CalculatePowerBalance(
            IReadOnlyCollection<IFNPowerSource> thermalSources,
            IReadOnlyCollection<BeamedPowerReceiver> beamedReceivers,
            IReadOnlyCollection<BeamedPowerTransmitter> beamedTransmitter,
            IReadOnlyCollection<ThermalEngineController> thermalEngines,
            IReadOnlyCollection<FusionECU2> variableEngines,
            IReadOnlyCollection<DaedalusEngineController> fusionEngines,
            IReadOnlyCollection<FNGenerator> generators,
            IReadOnlyCollection<FNRadiator> radiators)
        {
            _totalSourcePower = 0;

            _wasteheatSourcePowerCustom = 0;
            _wasteheatSourcePower100Pc = 0;
            _wasteheatSourcePower90Pc = 0;
            _wasteheatSourcePower80Pc = 0;
            _wasteheatSourcePower70Pc = 0;
            _wasteheatSourcePower60Pc = 0;
            _wasteheatSourcePower50Pc = 0;
            _wasteheatSourcePower45Pc = 0;
            _wasteheatSourcePower40Pc = 0;
            _wasteheatSourcePower35Pc = 0;
            _wasteheatSourcePower30Pc = 0;
            _wasteheatSourcePower25Pc = 0;
            _wasteheatSourcePower20Pc = 0;
            _wasteheatSourcePower15Pc = 0;
            _wasteheatSourcePower10Pc = 0;
            _wasteheatSourcePower8Pc = 0;
            _wasteheatSourcePower6Pc = 0;
            _wasteheatSourcePower4Pc = 0;
            _wasteheatSourcePower2Pc = 0;

            _averageSourceCoreTempAtCustom = double.MaxValue;
            _averageSourceCoreTempAt100Pc = double.MaxValue;

            double totalCoreTemperaturePowerAtCustom = 0;
            double totalCoreTemperaturePowerAt100 = 0;

            // first calculate reactors
            foreach (IFNPowerSource powerSource in thermalSources)
            {
                double combinedRawSourcePower = 0;
                double maxWastedEnergyRatio = 0;

                var connectedThermalPowerGenerator = (IFNElectricPowerGeneratorSource)powerSource.ConnectedThermalElectricGenerator;
                var connectedChargedPowerGenerator = (IFNElectricPowerGeneratorSource)powerSource.ConnectedChargedParticleElectricGenerator;

                // when connected to a thermal source, assume most thermal energy thermal power can end up in the radiators
                if (connectedThermalPowerGenerator != null)
                    combinedRawSourcePower += (1 - powerSource.ChargedPowerRatio) * connectedThermalPowerGenerator.RawGeneratorSourcePower;
                else
                {
                    maxWastedEnergyRatio = 1 - powerSource.ChargedPowerRatio;
                    combinedRawSourcePower += maxWastedEnergyRatio * powerSource.MaximumPower;
                }

                if (connectedChargedPowerGenerator != null)
                    combinedRawSourcePower += powerSource.ChargedPowerRatio * connectedChargedPowerGenerator.RawGeneratorSourcePower;

                _totalSourcePower += combinedRawSourcePower;

                // only take reactor power in account when its actually connected to a power generator
                if (connectedThermalPowerGenerator == null && connectedChargedPowerGenerator == null) continue;

                var coreTempAtRadiatorTempAt100Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt100Percent);
                var coreTempAtRadiatorTempAt90Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt90Percent);
                var coreTempAtRadiatorTempAt80Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt80Percent);
                var coreTempAtRadiatorTempAt70Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt70Percent);
                var coreTempAtRadiatorTempAt60Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt60Percent);
                var coreTempAtRadiatorTempAt50Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt50Percent);
                var coreTempAtRadiatorTempAt45Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt45Percent);
                var coreTempAtRadiatorTempAt40Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt40Percent);
                var coreTempAtRadiatorTempAt35Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt35Percent);
                var coreTempAtRadiatorTempAt30Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt30Percent);
                var coreTempAtRadiatorTempAt25Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt25Percent);
                var coreTempAtRadiatorTempAt20Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt20Percent);
                var coreTempAtRadiatorTempAt15Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt15Percent);
                var coreTempAtRadiatorTempAt10Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt10Percent);
                var coreTempAtRadiatorTempAt8Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt8Percent);
                var coreTempAtRadiatorTempAt6Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt6Percent);
                var coreTempAtRadiatorTempAt4Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt4Percent);
                var coreTempAtRadiatorTempAt2Percent = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAt2Percent);
                var coreTempAtRadiatorTempAtCustomPct = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAtCustomPct);

                var combinedRawSourcePowerAtCustomPct = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAtCustomPct) * customScenarioFraction);
                var combinedRawSourcePowerAt100Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt100Percent));
                var combinedRawSourcePowerAt90Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt90Percent) * 0.90);
                var combinedRawSourcePowerAt80Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt80Percent) * 0.80);
                var combinedRawSourcePowerAt70Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt70Percent) * 0.70);
                var combinedRawSourcePowerAt60Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt60Percent) * 0.60);
                var combinedRawSourcePowerAt50Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt50Percent) * 0.50);
                var combinedRawSourcePowerAt45Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt45Percent) * 0.45);
                var combinedRawSourcePowerAt40Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt40Percent) * 0.40);
                var combinedRawSourcePowerAt35Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt35Percent) * 0.35);
                var combinedRawSourcePowerAt30Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt30Percent) * 0.30);
                var combinedRawSourcePowerAt25Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt25Percent) * 0.25);
                var combinedRawSourcePowerAt20Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt20Percent) * 0.20);
                var combinedRawSourcePowerAt15Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt15Percent) * 0.15);
                var combinedRawSourcePowerAt10Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt10Percent) * 0.10);
                var combinedRawSourcePowerAt8Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt8Percent) * 0.08);
                var combinedRawSourcePowerAt6Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt6Percent) * 0.06);
                var combinedRawSourcePowerAt4Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt4Percent) * 0.04);
                var combinedRawSourcePowerAt2Percent = Math.Min(combinedRawSourcePower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt2Percent) * 0.02);

                double chargedGeneratorMaxInefficiency = connectedChargedPowerGenerator == null ? 0 : powerSource.ChargedPowerRatio * (1 - connectedChargedPowerGenerator.MaxEfficiency);

                var effectiveChargedPowerAtCustom = combinedRawSourcePowerAtCustomPct * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt100Percent = combinedRawSourcePowerAt100Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt90Percent = combinedRawSourcePowerAt90Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt80Percent = combinedRawSourcePowerAt80Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt70Percent = combinedRawSourcePowerAt70Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt60Percent = combinedRawSourcePowerAt60Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt50Percent = combinedRawSourcePowerAt50Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt45Percent = combinedRawSourcePowerAt45Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt40Percent = combinedRawSourcePowerAt40Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt35Percent = combinedRawSourcePowerAt35Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt30Percent = combinedRawSourcePowerAt30Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt25Percent = combinedRawSourcePowerAt25Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt20Percent = combinedRawSourcePowerAt20Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt15Percent = combinedRawSourcePowerAt15Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt10Percent = combinedRawSourcePowerAt10Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt8Percent = combinedRawSourcePowerAt8Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt6Percent = combinedRawSourcePowerAt6Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt4Percent = combinedRawSourcePowerAt4Percent * chargedGeneratorMaxInefficiency;
                var effectiveChargedPowerAt2Percent = combinedRawSourcePowerAt2Percent * chargedGeneratorMaxInefficiency;

                double thermalGeneratorMaxEfficiency = connectedThermalPowerGenerator?.MaxEfficiency ?? 0;

                var hotColdBathEfficiencyAt100Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt100Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt100Percent), 0);
                var hotColdBathEfficiencyAt90Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt90Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt90Percent), 0);
                var hotColdBathEfficiencyAt80Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt80Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt80Percent), 0);
                var hotColdBathEfficiencyAt70Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt70Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt70Percent), 0);
                var hotColdBathEfficiencyAt60Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt60Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt60Percent), 0);
                var hotColdBathEfficiencyAt50Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt50Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt50Percent), 0);
                var hotColdBathEfficiencyAt45Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt45Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt45Percent), 0);
                var hotColdBathEfficiencyAt40Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt40Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt40Percent), 0);
                var hotColdBathEfficiencyAt35Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt35Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt35Percent), 0);
                var hotColdBathEfficiencyAt30Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt30Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt30Percent), 0);
                var hotColdBathEfficiencyAt25Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt25Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt25Percent), 0);
                var hotColdBathEfficiencyAt20Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt20Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt20Percent), 0);
                var hotColdBathEfficiencyAt15Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt15Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt15Percent), 0);
                var hotColdBathEfficiencyAt10Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt10Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt10Percent), 0);
                var hotColdBathEfficiencyAt8Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt8Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt8Percent), 0);
                var hotColdBathEfficiencyAt6Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt6Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt6Percent), 0);
                var hotColdBathEfficiencyAt4Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt4Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt4Percent), 0);
                var hotColdBathEfficiencyAt2Percent = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAt2Percent / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAt2Percent), 0);
                var hotColdBathEfficiencyAtCustomPct = connectedThermalPowerGenerator == null ? 0 : Math.Max(1 - 0.75 * _restingRadiatorTempAtCustomPct / connectedThermalPowerGenerator.GetHotBathTemperature(_restingRadiatorTempAtCustomPct), 0);

                var effectiveThermalPowerAtCustomPct = combinedRawSourcePowerAtCustomPct * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAtCustomPct);
                var effectiveThermalPowerAt100Percent = combinedRawSourcePowerAt100Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt100Percent);
                var effectiveThermalPowerAt90Percent = combinedRawSourcePowerAt90Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt90Percent);
                var effectiveThermalPowerAt80Percent = combinedRawSourcePowerAt80Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt80Percent);
                var effectiveThermalPowerAt70Percent = combinedRawSourcePowerAt70Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt70Percent);
                var effectiveThermalPowerAt60Percent = combinedRawSourcePowerAt60Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt60Percent);
                var effectiveThermalPowerAt50Percent = combinedRawSourcePowerAt50Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt50Percent);
                var effectiveThermalPowerAt45Percent = combinedRawSourcePowerAt45Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt45Percent);
                var effectiveThermalPowerAt40Percent = combinedRawSourcePowerAt40Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt40Percent);
                var effectiveThermalPowerAt35Percent = combinedRawSourcePowerAt35Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt35Percent);
                var effectiveThermalPowerAt30Percent = combinedRawSourcePowerAt30Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt30Percent);
                var effectiveThermalPowerAt25Percent = combinedRawSourcePowerAt25Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt25Percent);
                var effectiveThermalPowerAt20Percent = combinedRawSourcePowerAt20Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt20Percent);
                var effectiveThermalPowerAt15Percent = combinedRawSourcePowerAt15Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt15Percent);
                var effectiveThermalPowerAt10Percent = combinedRawSourcePowerAt10Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt10Percent);
                var effectiveThermalPowerAt8Percent = combinedRawSourcePowerAt8Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt8Percent);
                var effectiveThermalPowerAt6Percent = combinedRawSourcePowerAt6Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt6Percent);
                var effectiveThermalPowerAt4Percent = combinedRawSourcePowerAt4Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt4Percent);
                var effectiveThermalPowerAt2Percent = combinedRawSourcePowerAt2Percent * (1 - thermalGeneratorMaxEfficiency * hotColdBathEfficiencyAt2Percent);

                var effectiveWastedPowerAtCustomPct = combinedRawSourcePowerAtCustomPct * maxWastedEnergyRatio;
                var effectiveWastedPowerAt100Percent = combinedRawSourcePowerAt100Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt90Percent = combinedRawSourcePowerAt90Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt80Percent = combinedRawSourcePowerAt80Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt70Percent = combinedRawSourcePowerAt70Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt60Percent = combinedRawSourcePowerAt60Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt50Percent = combinedRawSourcePowerAt50Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt45Percent = combinedRawSourcePowerAt45Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt40Percent = combinedRawSourcePowerAt40Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt35Percent = combinedRawSourcePowerAt35Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt30Percent = combinedRawSourcePowerAt30Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt25Percent = combinedRawSourcePowerAt25Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt20Percent = combinedRawSourcePowerAt20Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt15Percent = combinedRawSourcePowerAt15Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt10Percent = combinedRawSourcePowerAt10Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt8Percent = combinedRawSourcePowerAt8Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt6Percent = combinedRawSourcePowerAt6Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt4Percent = combinedRawSourcePowerAt4Percent * maxWastedEnergyRatio;
                var effectiveWastedPowerAt2Percent = combinedRawSourcePowerAt2Percent * maxWastedEnergyRatio;

                var effectiveWasteheatAt100Percent = effectiveChargedPowerAt100Percent + effectiveThermalPowerAt100Percent + effectiveWastedPowerAt100Percent;
                var effectiveWasteheatAt90Percent = effectiveChargedPowerAt90Percent + effectiveThermalPowerAt90Percent + effectiveWastedPowerAt90Percent;
                var effectiveWasteheatAt80Percent = effectiveChargedPowerAt80Percent + effectiveThermalPowerAt80Percent + effectiveWastedPowerAt80Percent;
                var effectiveWasteheatAt70Percent = effectiveChargedPowerAt70Percent + effectiveThermalPowerAt70Percent + effectiveWastedPowerAt70Percent;
                var effectiveWasteheatAt60Percent = effectiveChargedPowerAt60Percent + effectiveThermalPowerAt60Percent + effectiveWastedPowerAt60Percent;
                var effectiveWasteheatAt50Percent = effectiveChargedPowerAt50Percent + effectiveThermalPowerAt50Percent + effectiveWastedPowerAt50Percent;
                var effectiveWasteheatAt45Percent = effectiveChargedPowerAt45Percent + effectiveThermalPowerAt45Percent + effectiveWastedPowerAt45Percent;
                var effectiveWasteheatAt40Percent = effectiveChargedPowerAt40Percent + effectiveThermalPowerAt40Percent + effectiveWastedPowerAt40Percent;
                var effectiveWasteheatAt35Percent = effectiveChargedPowerAt35Percent + effectiveThermalPowerAt35Percent + effectiveWastedPowerAt35Percent;
                var effectiveWasteheatAt30Percent = effectiveChargedPowerAt30Percent + effectiveThermalPowerAt30Percent + effectiveWastedPowerAt30Percent;
                var effectiveWasteheatAt25Percent = effectiveChargedPowerAt25Percent + effectiveThermalPowerAt25Percent + effectiveWastedPowerAt25Percent;
                var effectiveWasteheatAt20Percent = effectiveChargedPowerAt20Percent + effectiveThermalPowerAt20Percent + effectiveWastedPowerAt20Percent;
                var effectiveWasteheatAt15Percent = effectiveChargedPowerAt15Percent + effectiveThermalPowerAt15Percent + effectiveWastedPowerAt15Percent;
                var effectiveWasteheatAt10Percent = effectiveChargedPowerAt10Percent + effectiveThermalPowerAt10Percent + effectiveWastedPowerAt10Percent;
                var effectiveWasteheatAt8Percent = effectiveChargedPowerAt8Percent + effectiveThermalPowerAt8Percent + effectiveWastedPowerAt8Percent;
                var effectiveWasteheatAt6Percent = effectiveChargedPowerAt6Percent + effectiveThermalPowerAt6Percent + effectiveWastedPowerAt6Percent;
                var effectiveWasteheatAt4Percent = effectiveChargedPowerAt4Percent + effectiveThermalPowerAt4Percent + effectiveWastedPowerAt4Percent;
                var effectiveWasteheatAt2Percent = effectiveChargedPowerAt2Percent + effectiveThermalPowerAt2Percent + effectiveWastedPowerAt2Percent;
                var effectiveWasteheatAtCustomPct = effectiveChargedPowerAtCustom + effectiveThermalPowerAtCustomPct + effectiveWastedPowerAtCustomPct;

                totalCoreTemperaturePowerAtCustom += coreTempAtRadiatorTempAtCustomPct * effectiveWasteheatAtCustomPct;
                totalCoreTemperaturePowerAt100 += coreTempAtRadiatorTempAt100Percent * effectiveWasteheatAt100Percent;

                _wasteheatSourcePower100Pc += effectiveWasteheatAt100Percent;
                _wasteheatSourcePower90Pc += effectiveWasteheatAt90Percent;
                _wasteheatSourcePower80Pc += effectiveWasteheatAt80Percent;
                _wasteheatSourcePower70Pc += effectiveWasteheatAt70Percent;
                _wasteheatSourcePower60Pc += effectiveWasteheatAt60Percent;
                _wasteheatSourcePower50Pc += effectiveWasteheatAt50Percent;
                _wasteheatSourcePower45Pc += effectiveWasteheatAt45Percent;
                _wasteheatSourcePower40Pc += effectiveWasteheatAt40Percent;
                _wasteheatSourcePower35Pc += effectiveWasteheatAt35Percent;
                _wasteheatSourcePower30Pc += effectiveWasteheatAt30Percent;
                _wasteheatSourcePower25Pc += effectiveWasteheatAt25Percent;
                _wasteheatSourcePower20Pc += effectiveWasteheatAt20Percent;
                _wasteheatSourcePower15Pc += effectiveWasteheatAt15Percent;
                _wasteheatSourcePower10Pc += effectiveWasteheatAt10Percent;
                _wasteheatSourcePower8Pc += effectiveWasteheatAt8Percent;
                _wasteheatSourcePower6Pc += effectiveWasteheatAt6Percent;
                _wasteheatSourcePower4Pc += effectiveWasteheatAt4Percent;
                _wasteheatSourcePower2Pc += effectiveWasteheatAt2Percent;
                _wasteheatSourcePowerCustom += effectiveWasteheatAtCustomPct;
            }

            // calculate weighted average core temperatures
            if (_wasteheatSourcePowerCustom > 0) _averageSourceCoreTempAtCustom = totalCoreTemperaturePowerAtCustom / _wasteheatSourcePowerCustom;
            if (_wasteheatSourcePower100Pc > 0) _averageSourceCoreTempAt100Pc = totalCoreTemperaturePowerAt100 / _wasteheatSourcePower100Pc;

            // calculate effect of on demand beamed power
            foreach (BeamedPowerReceiver beamedReceiver in beamedReceivers)
            {
                // only count receiver that are activated
                if (!beamedReceiver.receiverIsEnabled)
                    continue;

                var maxWasteheatProduction = beamedReceiver.MaximumRecievePower * (1 - beamedReceiver.activeBandwidthConfiguration.MaxEfficiencyPercentage * 0.01);

                _wasteheatSourcePower100Pc += maxWasteheatProduction;
                _wasteheatSourcePower90Pc += maxWasteheatProduction * 0.90;
                _wasteheatSourcePower80Pc += maxWasteheatProduction * 0.80;
                _wasteheatSourcePower70Pc += maxWasteheatProduction * 0.70;
                _wasteheatSourcePower60Pc += maxWasteheatProduction * 0.60;
                _wasteheatSourcePower50Pc += maxWasteheatProduction * 0.50;
                _wasteheatSourcePower45Pc += maxWasteheatProduction * 0.45;
                _wasteheatSourcePower40Pc += maxWasteheatProduction * 0.40;
                _wasteheatSourcePower35Pc += maxWasteheatProduction * 0.35;
                _wasteheatSourcePower30Pc += maxWasteheatProduction * 0.30;
                _wasteheatSourcePower25Pc += maxWasteheatProduction * 0.25;
                _wasteheatSourcePower20Pc += maxWasteheatProduction * 0.20;
                _wasteheatSourcePower15Pc += maxWasteheatProduction * 0.15;
                _wasteheatSourcePower10Pc += maxWasteheatProduction * 0.10;
                _wasteheatSourcePower8Pc += maxWasteheatProduction * 0.08;
                _wasteheatSourcePower6Pc += maxWasteheatProduction * 0.06;
                _wasteheatSourcePower4Pc += maxWasteheatProduction * 0.04;
                _wasteheatSourcePower2Pc += maxWasteheatProduction * 0.02;
                _wasteheatSourcePowerCustom += maxWasteheatProduction * customScenarioFraction;
            }

            foreach (BeamedPowerTransmitter beamedPowerTransmitter in beamedTransmitter)
            {
                if (!beamedPowerTransmitter.IsEnabled)
                    continue;

                var wasteheatFraction = 1 - beamedPowerTransmitter.activeBeamGenerator.efficiencyPercentage * 0.01;
                var powerCapacity = beamedPowerTransmitter.PowerCapacity;

                _wasteheatSourcePowerCustom += Math.Min(_electricPowerAtCustom, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower100Pc += Math.Min(_electricPowerAt100, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower90Pc += Math.Min(_electricPowerAt90, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower80Pc += Math.Min(_electricPowerAt80, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower70Pc += Math.Min(_electricPowerAt70, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower60Pc += Math.Min(_electricPowerAt60, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower50Pc += Math.Min(_electricPowerAt50, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower45Pc += Math.Min(_electricPowerAt45, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower40Pc += Math.Min(_electricPowerAt40, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower35Pc += Math.Min(_electricPowerAt35, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower30Pc += Math.Min(_electricPowerAt30, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower25Pc += Math.Min(_electricPowerAt25, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower20Pc += Math.Min(_electricPowerAt20, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower15Pc += Math.Min(_electricPowerAt15, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower10Pc += Math.Min(_electricPowerAt10, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower8Pc += Math.Min(_electricPowerAt8, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower6Pc += Math.Min(_electricPowerAt6, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower4Pc += Math.Min(_electricPowerAt4, powerCapacity) * wasteheatFraction;
                _wasteheatSourcePower2Pc += Math.Min(_electricPowerAt2, powerCapacity) * wasteheatFraction;
            }

            var engineThrottleRatio = 0.01 * engineThrottlePercentage;

            foreach (ThermalEngineController thermalNozzle in thermalEngines)
            {
                var maxWasteheatProduction = engineThrottleRatio * thermalNozzle.ReactorWasteheatModifier * thermalNozzle.AttachedReactor.NormalisedMaximumPower;

                _wasteheatSourcePower100Pc += maxWasteheatProduction;
                _wasteheatSourcePower90Pc += maxWasteheatProduction * 0.90;
                _wasteheatSourcePower80Pc += maxWasteheatProduction * 0.80;
                _wasteheatSourcePower70Pc += maxWasteheatProduction * 0.70;
                _wasteheatSourcePower60Pc += maxWasteheatProduction * 0.60;
                _wasteheatSourcePower50Pc += maxWasteheatProduction * 0.50;
                _wasteheatSourcePower45Pc += maxWasteheatProduction * 0.45;
                _wasteheatSourcePower40Pc += maxWasteheatProduction * 0.40;
                _wasteheatSourcePower35Pc += maxWasteheatProduction * 0.35;
                _wasteheatSourcePower30Pc += maxWasteheatProduction * 0.30;
                _wasteheatSourcePower25Pc += maxWasteheatProduction * 0.25;
                _wasteheatSourcePower20Pc += maxWasteheatProduction * 0.20;
                _wasteheatSourcePower15Pc += maxWasteheatProduction * 0.15;
                _wasteheatSourcePower10Pc += maxWasteheatProduction * 0.10;
                _wasteheatSourcePower8Pc += maxWasteheatProduction * 0.08;
                _wasteheatSourcePower6Pc += maxWasteheatProduction * 0.06;
                _wasteheatSourcePower4Pc += maxWasteheatProduction * 0.04;
                _wasteheatSourcePower2Pc += maxWasteheatProduction * 0.02;
                _wasteheatSourcePowerCustom += maxWasteheatProduction * customScenarioFraction;
            }

            foreach (FusionECU2 variableEngine in variableEngines)
            {
                var maxWasteheatProduction = engineThrottleRatio * variableEngine.fusionWasteHeatMax;

                _wasteheatSourcePower100Pc += maxWasteheatProduction;
                _wasteheatSourcePower90Pc += maxWasteheatProduction * 0.90;
                _wasteheatSourcePower80Pc += maxWasteheatProduction * 0.80;
                _wasteheatSourcePower70Pc += maxWasteheatProduction * 0.70;
                _wasteheatSourcePower60Pc += maxWasteheatProduction * 0.60;
                _wasteheatSourcePower50Pc += maxWasteheatProduction * 0.50;
                _wasteheatSourcePower45Pc += maxWasteheatProduction * 0.45;
                _wasteheatSourcePower40Pc += maxWasteheatProduction * 0.40;
                _wasteheatSourcePower35Pc += maxWasteheatProduction * 0.35;
                _wasteheatSourcePower30Pc += maxWasteheatProduction * 0.30;
                _wasteheatSourcePower25Pc += maxWasteheatProduction * 0.25;
                _wasteheatSourcePower20Pc += maxWasteheatProduction * 0.20;
                _wasteheatSourcePower15Pc += maxWasteheatProduction * 0.15;
                _wasteheatSourcePower10Pc += maxWasteheatProduction * 0.10;
                _wasteheatSourcePower8Pc += maxWasteheatProduction * 0.08;
                _wasteheatSourcePower6Pc += maxWasteheatProduction * 0.06;
                _wasteheatSourcePower4Pc += maxWasteheatProduction * 0.04;
                _wasteheatSourcePower2Pc += maxWasteheatProduction * 0.02;
                _wasteheatSourcePowerCustom += maxWasteheatProduction * customScenarioFraction;
            }

            foreach (DaedalusEngineController fusionEngine in fusionEngines)
            {
                var maxWasteheatProduction = 0.01 * engineThrottlePercentage * fusionEngine.wasteHeat;

                _wasteheatSourcePower100Pc += maxWasteheatProduction;
                _wasteheatSourcePower90Pc += maxWasteheatProduction * 0.90;
                _wasteheatSourcePower80Pc += maxWasteheatProduction * 0.80;
                _wasteheatSourcePower70Pc += maxWasteheatProduction * 0.70;
                _wasteheatSourcePower60Pc += maxWasteheatProduction * 0.60;
                _wasteheatSourcePower50Pc += maxWasteheatProduction * 0.50;
                _wasteheatSourcePower45Pc += maxWasteheatProduction * 0.45;
                _wasteheatSourcePower40Pc += maxWasteheatProduction * 0.40;
                _wasteheatSourcePower35Pc += maxWasteheatProduction * 0.35;
                _wasteheatSourcePower30Pc += maxWasteheatProduction * 0.30;
                _wasteheatSourcePower25Pc += maxWasteheatProduction * 0.25;
                _wasteheatSourcePower20Pc += maxWasteheatProduction * 0.20;
                _wasteheatSourcePower15Pc += maxWasteheatProduction * 0.15;
                _wasteheatSourcePower10Pc += maxWasteheatProduction * 0.10;
                _wasteheatSourcePower8Pc += maxWasteheatProduction * 0.08;
                _wasteheatSourcePower6Pc += maxWasteheatProduction * 0.06;
                _wasteheatSourcePower4Pc += maxWasteheatProduction * 0.04;
                _wasteheatSourcePower2Pc += maxWasteheatProduction * 0.02;
                _wasteheatSourcePowerCustom += maxWasteheatProduction * customScenarioFraction;
            }

            CalculateGeneratedElectricPower(generators);

            _numberOfRadiators = 0;
            _vesselMaxRadDissipation = 0;
            _vesselMaxRadConvection = 0;
            _vesselBaseRadiatorArea = 0;

            double totalMaxRadTempArea = 0;
            double totalConvectiveBonusArea = 0;
            double vesselConvectiveRadiatorArea = 0;
            double submergedRatio = (double)(decimal)submergedPercentage * 0.01;

            foreach (FNRadiator radiator in radiators)
            {
                _numberOfRadiators++;
                var baseRadiatorArea = radiator.BaseRadiatorArea;
                _vesselBaseRadiatorArea += baseRadiatorArea;
                vesselConvectiveRadiatorArea += radiator.radiatorArea;

                var maxRadTemperature = Math.Min(radiator.MaxRadiatorTemperature, _averageSourceCoreTempAt100Pc);

                var maxRadiatorConvection = FNRadiator.CalculateConvPowerDissipation(
                    radiatorSurfaceArea: radiator.radiatorArea,
                    radiatorConvectiveBonus: radiator.convectiveBonus,
                    radiatorTemperature: maxRadTemperature,
                    externalTemperature: externalTemperatureInKelvin,
                    atmosphericDensity: atmosphereDensity,
                    grapheneRadiatorRatio: radiator.IsGraphene ? 1 : 0,
                    submergedPortion: submergedRatio);

                _vesselMaxRadConvection += maxRadiatorConvection;

                var tempToPowerFour = maxRadTemperature * maxRadTemperature * maxRadTemperature * maxRadTemperature;
                _vesselMaxRadDissipation += GameConstants.stefan_const * radiator.EffectiveRadiatorArea * tempToPowerFour / 1e6;
                totalMaxRadTempArea += maxRadTemperature * baseRadiatorArea;
                totalConvectiveBonusArea += radiator.radiatorArea * radiator.convectiveBonus;
            }

            _vesselMaxRadConvectionAndDissipation = _vesselMaxRadConvection + _vesselMaxRadDissipation;

            _averageConvectiveBonus = totalConvectiveBonusArea != 0 ? totalConvectiveBonusArea / vesselConvectiveRadiatorArea : 1;
            _averageMaxRadTemp = totalMaxRadTempArea != 0 ? totalMaxRadTempArea / _vesselBaseRadiatorArea : double.NaN;

            var radRatioConvectionCustom = _vesselMaxRadConvection > 0 && _wasteheatSourcePowerCustom < _vesselMaxRadConvection ?  _wasteheatSourcePowerCustom / _vesselMaxRadConvection: double.NaN;
            var radRatioConvection100Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower100Pc < _vesselMaxRadConvection ? _wasteheatSourcePower100Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection90Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower90Pc < _vesselMaxRadConvection ?  _wasteheatSourcePower90Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection80Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower80Pc < _vesselMaxRadConvection ? _wasteheatSourcePower80Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection70Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower70Pc < _vesselMaxRadConvection ? _wasteheatSourcePower70Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection60Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower60Pc < _vesselMaxRadConvection ? _wasteheatSourcePower60Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection50Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower50Pc < _vesselMaxRadConvection ? _wasteheatSourcePower50Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection45Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower45Pc < _vesselMaxRadConvection ? _wasteheatSourcePower45Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection40Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower40Pc < _vesselMaxRadConvection ? _wasteheatSourcePower40Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection35Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower35Pc < _vesselMaxRadConvection ? _wasteheatSourcePower35Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection30Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower30Pc < _vesselMaxRadConvection ? _wasteheatSourcePower30Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection25Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower25Pc < _vesselMaxRadConvection ? _wasteheatSourcePower25Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection20Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower20Pc < _vesselMaxRadConvection ? _wasteheatSourcePower20Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection15Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower15Pc < _vesselMaxRadConvection ? _wasteheatSourcePower15Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection10Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower10Pc < _vesselMaxRadConvection ? _wasteheatSourcePower10Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection8Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower8Pc < _vesselMaxRadConvection ? _wasteheatSourcePower8Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection6Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower6Pc < _vesselMaxRadConvection ? _wasteheatSourcePower6Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection4Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower4Pc < _vesselMaxRadConvection ? _wasteheatSourcePower4Pc / _vesselMaxRadConvection : double.NaN;
            var radRatioConvection2Pc = _vesselMaxRadConvection > 0 && _wasteheatSourcePower2Pc < _vesselMaxRadConvection ? _wasteheatSourcePower2Pc / _vesselMaxRadConvection : double.NaN;

            var maxRadTempAboveExternalTemp = _averageMaxRadTemp - externalTemperatureInKelvin;

            var restingConvectionTempAtCustomPct = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvectionCustom;
            var restingConvectionTempAt100Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection100Pc;
            var restingConvectionTempAt90Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection90Pc;
            var restingConvectionTempAt80Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection80Pc;
            var restingConvectionTempAt70Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection70Pc;
            var restingConvectionTempAt60Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection60Pc;
            var restingConvectionTempAt50Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection50Pc;
            var restingConvectionTempAt45Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection45Pc;
            var restingConvectionTempAt40Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection40Pc;
            var restingConvectionTempAt35Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection35Pc;
            var restingConvectionTempAt30Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection30Pc;
            var restingConvectionTempAt25Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection25Pc;
            var restingConvectionTempAt20Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection20Pc;
            var restingConvectionTempAt15Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection15Pc;
            var restingConvectionTempAt10Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection10Pc;
            var restingConvectionTempAt8Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection8Pc;
            var restingConvectionTempAt6Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection6Pc;
            var restingConvectionTempAt4Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection4Pc;
            var restingConvectionTempAt2Percent = externalTemperatureInKelvin + maxRadTempAboveExternalTemp * radRatioConvection2Pc;

            var convectedPowerAtCustomPct = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAtCustomPct, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt100Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt100Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt90Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt90Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt80Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt80Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt70Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt70Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt60Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt60Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt50Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt50Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt45Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt45Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt40Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt40Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt35Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt35Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt30Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt30Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt25Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt25Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt20Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt20Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt15Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt15Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt10Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt10Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt8Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt8Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt6Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt6Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt4Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt4Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);
            var convectedPowerAt2Percent = FNRadiator.CalculateConvPowerDissipation(totalConvectiveBonusArea, _averageConvectiveBonus, restingConvectionTempAt2Percent, externalTemperatureInKelvin, atmosphereDensity, submergedRatio);

            var convectionRestingTempAboveExternalAtCustomPct = Math.Max(0, restingConvectionTempAtCustomPct - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt100Percent = Math.Max(0, restingConvectionTempAt100Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt90Percent = Math.Max(0, restingConvectionTempAt90Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt80Percent = Math.Max(0, restingConvectionTempAt80Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt70Percent = Math.Max(0, restingConvectionTempAt70Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt60Percent = Math.Max(0, restingConvectionTempAt60Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt50Percent = Math.Max(0, restingConvectionTempAt50Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt45Percent = Math.Max(0, restingConvectionTempAt45Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt40Percent = Math.Max(0, restingConvectionTempAt40Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt35Percent = Math.Max(0, restingConvectionTempAt35Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt30Percent = Math.Max(0, restingConvectionTempAt30Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt25Percent = Math.Max(0, restingConvectionTempAt25Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt20Percent = Math.Max(0, restingConvectionTempAt20Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt15Percent = Math.Max(0, restingConvectionTempAt15Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt10Percent = Math.Max(0, restingConvectionTempAt10Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt8Percent = Math.Max(0, restingConvectionTempAt8Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt6Percent = Math.Max(0, restingConvectionTempAt6Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt4Percent = Math.Max(0, restingConvectionTempAt4Percent - externalTemperatureInKelvin);
            var convectionRestingTempAboveExternalAt2Percent = Math.Max(0, restingConvectionTempAt2Percent - externalTemperatureInKelvin);

            var dissipationEnergyAtCustomPct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAtCustomPct, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt100Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt100Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt90Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt90Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt80Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt80Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt70Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt70Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt60Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt60Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt50Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt50Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt45Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt45Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt40Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt40Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt35Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt35Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt30Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt30Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt25Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt25Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt20Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt20Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt15Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt15Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt10Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt10Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt8Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt8Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt6Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt6Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt4Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt4Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;
            var dissipationEnergyAt2Pct = Math.Pow(Math.Pow(convectionRestingTempAboveExternalAt2Percent, 4) * PhysicsGlobals.StefanBoltzmanConstant * _vesselBaseRadiatorArea, 0.25) * 1e-6;

            var dissipationTempModifierAtCustomPct = convectedPowerAtCustomPct > dissipationEnergyAtCustomPct ?  1 - dissipationEnergyAtCustomPct / convectedPowerAtCustomPct : 1;
            var dissipationTempModifierAt100Percent = convectedPowerAt100Percent > dissipationEnergyAt100Pct ? 1 - dissipationEnergyAt100Pct / convectedPowerAt100Percent : 1;
            var dissipationTempModifierAt90Percent = convectedPowerAt90Percent > dissipationEnergyAt90Pct ? 1 - dissipationEnergyAt90Pct / convectedPowerAt90Percent : 1;
            var dissipationTempModifierAt80Percent = convectedPowerAt80Percent > dissipationEnergyAt80Pct ? 1 - dissipationEnergyAt80Pct / convectedPowerAt80Percent : 1;
            var dissipationTempModifierAt70Percent = convectedPowerAt70Percent > dissipationEnergyAt70Pct ? 1 - dissipationEnergyAt70Pct / convectedPowerAt70Percent : 1;
            var dissipationTempModifierAt60Percent = convectedPowerAt60Percent > dissipationEnergyAt60Pct ? 1 - dissipationEnergyAt60Pct / convectedPowerAt60Percent : 1;
            var dissipationTempModifierAt50Percent = convectedPowerAt50Percent > dissipationEnergyAt50Pct ? 1 - dissipationEnergyAt50Pct / convectedPowerAt50Percent : 1;
            var dissipationTempModifierAt45Percent = convectedPowerAt45Percent > dissipationEnergyAt70Pct ? 1 - dissipationEnergyAt45Pct / convectedPowerAt45Percent : 1;
            var dissipationTempModifierAt40Percent = convectedPowerAt40Percent > dissipationEnergyAt40Pct ? 1 - dissipationEnergyAt40Pct / convectedPowerAt40Percent : 1;
            var dissipationTempModifierAt35Percent = convectedPowerAt35Percent > dissipationEnergyAt35Pct ? 1 - dissipationEnergyAt35Pct / convectedPowerAt35Percent : 1;
            var dissipationTempModifierAt30Percent = convectedPowerAt30Percent > dissipationEnergyAt30Pct ? 1 - dissipationEnergyAt30Pct / convectedPowerAt30Percent : 1;
            var dissipationTempModifierAt25Percent = convectedPowerAt25Percent > dissipationEnergyAt25Pct ? 1 - dissipationEnergyAt25Pct / convectedPowerAt20Percent : 1;
            var dissipationTempModifierAt20Percent = convectedPowerAt20Percent > dissipationEnergyAt20Pct ? 1 - dissipationEnergyAt20Pct / convectedPowerAt20Percent : 1;
            var dissipationTempModifierAt15Percent = convectedPowerAt15Percent > dissipationEnergyAt15Pct ? 1 - dissipationEnergyAt15Pct / convectedPowerAt15Percent : 1;
            var dissipationTempModifierAt10Percent = convectedPowerAt10Percent > dissipationEnergyAt10Pct ? 1 - dissipationEnergyAt10Pct / convectedPowerAt10Percent : 1;
            var dissipationTempModifierAt8Percent = convectedPowerAt8Percent > dissipationEnergyAt8Pct ? 1 - dissipationEnergyAt8Pct / convectedPowerAt8Percent : 1;
            var dissipationTempModifierAt6Percent = convectedPowerAt6Percent > dissipationEnergyAt6Pct ? 1 - dissipationEnergyAt6Pct / convectedPowerAt6Percent : 1;
            var dissipationTempModifierAt4Percent = convectedPowerAt4Percent > dissipationEnergyAt4Pct ? 1 - dissipationEnergyAt4Pct / convectedPowerAt4Percent : 1;
            var dissipationTempModifierAt2Percent = convectedPowerAt2Percent > dissipationEnergyAt2Pct ? 1 - dissipationEnergyAt2Pct / convectedPowerAt2Percent : 1;

            var radRatioDissipationCustom = _wasteheatSourcePowerCustom / _vesselMaxRadDissipation;
            var radRatioDissipation100Pc = _wasteheatSourcePower100Pc / _vesselMaxRadDissipation;
            var radRatioDissipation90Pc = _wasteheatSourcePower90Pc / _vesselMaxRadDissipation;
            var radRatioDissipation80Pc = _wasteheatSourcePower80Pc / _vesselMaxRadDissipation;
            var radRatioDissipation70Pc = _wasteheatSourcePower70Pc / _vesselMaxRadDissipation;
            var radRatioDissipation60Pc = _wasteheatSourcePower60Pc / _vesselMaxRadDissipation;
            var radRatioDissipation50Pc = _wasteheatSourcePower50Pc / _vesselMaxRadDissipation;
            var radRatioDissipation45Pc = _wasteheatSourcePower45Pc / _vesselMaxRadDissipation;
            var radRatioDissipation40Pc = _wasteheatSourcePower40Pc / _vesselMaxRadDissipation;
            var radRatioDissipation35Pc = _wasteheatSourcePower35Pc / _vesselMaxRadDissipation;
            var radRatioDissipation30Pc = _wasteheatSourcePower30Pc / _vesselMaxRadDissipation;
            var radRatioDissipation25Pc = _wasteheatSourcePower25Pc / _vesselMaxRadDissipation;
            var radRatioDissipation20Pc = _wasteheatSourcePower20Pc / _vesselMaxRadDissipation;
            var radRatioDissipation15Pc = _wasteheatSourcePower15Pc / _vesselMaxRadDissipation;
            var radRatioDissipation10Pc = _wasteheatSourcePower10Pc / _vesselMaxRadDissipation;
            var radRatioDissipation8Pc = _wasteheatSourcePower8Pc / _vesselMaxRadDissipation;
            var radRatioDissipation6Pc = _wasteheatSourcePower6Pc / _vesselMaxRadDissipation;
            var radRatioDissipation4Pc = _wasteheatSourcePower4Pc / _vesselMaxRadDissipation;
            var radRatioDissipation2Pc = _wasteheatSourcePower2Pc / _vesselMaxRadDissipation;

            var restingDissipationRadiatorTempAtCustomPct = !radRatioDissipationCustom.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipationCustom, 0.25) : 0;
            var restingDissipationRadiatorTempAt100Percent = !radRatioDissipation100Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation100Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt90Percent = !radRatioDissipation90Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation90Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt80Percent = !radRatioDissipation80Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation80Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt70Percent = !radRatioDissipation70Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation70Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt60Percent = !radRatioDissipation60Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation60Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt50Percent = !radRatioDissipation50Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation50Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt45Percent = !radRatioDissipation45Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation45Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt40Percent = !radRatioDissipation40Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation40Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt35Percent = !radRatioDissipation30Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation35Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt30Percent = !radRatioDissipation30Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation30Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt25Percent = !radRatioDissipation25Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation25Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt20Percent = !radRatioDissipation20Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation20Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt15Percent = !radRatioDissipation15Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation15Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt10Percent = !radRatioDissipation10Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation10Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt8Percent = !radRatioDissipation8Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation8Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt6Percent = !radRatioDissipation6Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation6Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt4Percent = !radRatioDissipation4Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation4Pc, 0.25) : 0;
            var restingDissipationRadiatorTempAt2Percent = !radRatioDissipation2Pc.IsInfinityOrNaN() ? _averageMaxRadTemp * Math.Pow(radRatioDissipation2Pc, 0.25) : 0;

            _restingRadiatorTempAtCustomPct = restingConvectionTempAtCustomPct.IsInfinityOrNaN() ? restingDissipationRadiatorTempAtCustomPct : Math.Min(restingDissipationRadiatorTempAtCustomPct, restingConvectionTempAtCustomPct * dissipationTempModifierAtCustomPct);
            _restingRadiatorTempAt100Percent = restingConvectionTempAt100Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt100Percent : Math.Min(restingDissipationRadiatorTempAt100Percent, restingConvectionTempAt100Percent * dissipationTempModifierAt100Percent);
            _restingRadiatorTempAt90Percent = restingConvectionTempAt90Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt90Percent: Math.Min(restingDissipationRadiatorTempAt90Percent, restingConvectionTempAt90Percent * dissipationTempModifierAt90Percent);
            _restingRadiatorTempAt80Percent = restingConvectionTempAt80Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt80Percent: Math.Min(restingDissipationRadiatorTempAt80Percent, restingConvectionTempAt80Percent * dissipationTempModifierAt80Percent);
            _restingRadiatorTempAt70Percent = restingConvectionTempAt70Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt70Percent: Math.Min(restingDissipationRadiatorTempAt70Percent, restingConvectionTempAt70Percent * dissipationTempModifierAt70Percent);
            _restingRadiatorTempAt60Percent = restingConvectionTempAt60Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt60Percent: Math.Min(restingDissipationRadiatorTempAt60Percent, restingConvectionTempAt60Percent * dissipationTempModifierAt60Percent);
            _restingRadiatorTempAt50Percent = restingConvectionTempAt50Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt50Percent: Math.Min(restingDissipationRadiatorTempAt50Percent, restingConvectionTempAt50Percent * dissipationTempModifierAt50Percent);
            _restingRadiatorTempAt45Percent = restingConvectionTempAt45Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt45Percent: Math.Min(restingDissipationRadiatorTempAt45Percent, restingConvectionTempAt45Percent * dissipationTempModifierAt45Percent);
            _restingRadiatorTempAt40Percent = restingConvectionTempAt40Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt40Percent: Math.Min(restingDissipationRadiatorTempAt40Percent, restingConvectionTempAt40Percent * dissipationTempModifierAt40Percent);
            _restingRadiatorTempAt35Percent = restingConvectionTempAt35Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt35Percent: Math.Min(restingDissipationRadiatorTempAt35Percent, restingConvectionTempAt35Percent * dissipationTempModifierAt35Percent);
            _restingRadiatorTempAt30Percent = restingConvectionTempAt30Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt30Percent: Math.Min(restingDissipationRadiatorTempAt30Percent, restingConvectionTempAt30Percent * dissipationTempModifierAt30Percent);
            _restingRadiatorTempAt25Percent = restingConvectionTempAt25Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt25Percent: Math.Min(restingDissipationRadiatorTempAt25Percent, restingConvectionTempAt25Percent * dissipationTempModifierAt25Percent);
            _restingRadiatorTempAt20Percent = restingConvectionTempAt20Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt20Percent: Math.Min(restingDissipationRadiatorTempAt20Percent, restingConvectionTempAt20Percent * dissipationTempModifierAt20Percent);
            _restingRadiatorTempAt15Percent = restingConvectionTempAt15Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt15Percent: Math.Min(restingDissipationRadiatorTempAt15Percent, restingConvectionTempAt15Percent * dissipationTempModifierAt15Percent);
            _restingRadiatorTempAt10Percent = restingConvectionTempAt10Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt10Percent: Math.Min(restingDissipationRadiatorTempAt10Percent, restingConvectionTempAt10Percent * dissipationTempModifierAt10Percent);
            _restingRadiatorTempAt8Percent = restingConvectionTempAt8Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt8Percent: Math.Min(restingDissipationRadiatorTempAt8Percent, restingConvectionTempAt8Percent * dissipationTempModifierAt8Percent);
            _restingRadiatorTempAt6Percent = restingConvectionTempAt6Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt6Percent: Math.Min(restingDissipationRadiatorTempAt6Percent, restingConvectionTempAt6Percent * dissipationTempModifierAt6Percent);
            _restingRadiatorTempAt4Percent = restingConvectionTempAt4Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt4Percent: Math.Min(restingDissipationRadiatorTempAt4Percent, restingConvectionTempAt4Percent * dissipationTempModifierAt4Percent);
            _restingRadiatorTempAt2Percent = restingConvectionTempAt2Percent.IsInfinityOrNaN() ? restingDissipationRadiatorTempAt2Percent: Math.Min(restingDissipationRadiatorTempAt2Percent, restingConvectionTempAt2Percent * dissipationTempModifierAt2Percent);

            var thermalGenerators = generators.Where(m => !m.chargedParticleMode).ToList();

            if (thermalGenerators.Count > 0)
            {
                _hasThermalGenerators = true;

                var averageEfficiency = thermalGenerators.Sum(m => m.MaxStableMegaWattPower) / thermalGenerators.Sum(m => m.RawGeneratorSourcePower);

                _hotColdBathEfficiencyAtCustomPct = averageEfficiency * CalculateHotColdBathEfficiency(_averageSourceCoreTempAtCustom, _restingRadiatorTempAtCustomPct);
            }
            else
                _hasThermalGenerators = false;

            if (_averageSourceCoreTempAtCustom >= double.MaxValue)
                _averageSourceCoreTempAtCustom = -1;
        }

        private double CalculateHotColdBathEfficiency(double sourceTemperature, double radiatorRestingTemperature)
        {
            if (sourceTemperature >= double.MaxValue || radiatorRestingTemperature.IsInfinityOrNaN())
                return 0;

            return Math.Max(1 - 0.75 * radiatorRestingTemperature / _averageSourceCoreTempAtCustom, 0);
        }

        private void CalculateGeneratedElectricPower(IEnumerable<FNGenerator> generators)
        {
            _electricPowerAtCustom = 0;
            _electricPowerAt100 = 0;
            _electricPowerAt90 = 0;
            _electricPowerAt80 = 0;
            _electricPowerAt70 = 0;
            _electricPowerAt60 = 0;
            _electricPowerAt50 = 0;
            _electricPowerAt45 = 0;
            _electricPowerAt40 = 0;
            _electricPowerAt35 = 0;
            _electricPowerAt30 = 0;
            _electricPowerAt25 = 0;
            _electricPowerAt20 = 0;
            _electricPowerAt15 = 0;
            _electricPowerAt10 = 0;
            _electricPowerAt8 = 0;
            _electricPowerAt6 = 0;
            _electricPowerAt4 = 0;
            _electricPowerAt2 = 0;

            foreach (FNGenerator generator in generators)
            {
                if (generator.chargedParticleMode)
                {
                    _electricPowerAt100 += generator.maximumGeneratorPowerMJ * generator.powerOutputMultiplier;
                }
                else
                {
                    var generatorMaximumGeneratorPower = generator.maximumGeneratorPowerMJ * generator.powerOutputMultiplier;

                    if (generator.isLimitedByMinThrottle)
                    {
                        var generatorElectricPowerAt100 = generatorMaximumGeneratorPower * Math.Max(1 - _restingRadiatorTempAt100Percent / generator.GetHotBathTemperature(0.75 * _restingRadiatorTempAt100Percent), 0);

                        _electricPowerAt100 += generatorElectricPowerAt100;
                        _electricPowerAt90 += generatorElectricPowerAt100;
                        _electricPowerAt80 += generatorElectricPowerAt100;
                        _electricPowerAt70 += generatorElectricPowerAt100;
                        _electricPowerAt60 += generatorElectricPowerAt100;
                        _electricPowerAt50 += generatorElectricPowerAt100;
                        _electricPowerAt45 += generatorElectricPowerAt100;
                        _electricPowerAt40 += generatorElectricPowerAt100;
                        _electricPowerAt35 += generatorElectricPowerAt100;
                        _electricPowerAt30 += generatorElectricPowerAt100;
                        _electricPowerAt25 += generatorElectricPowerAt100;
                        _electricPowerAt20 += generatorElectricPowerAt100;
                        _electricPowerAt15 += generatorElectricPowerAt100;
                        _electricPowerAt10 += generatorElectricPowerAt100;
                        _electricPowerAt8 += generatorElectricPowerAt100;
                        _electricPowerAt6 += generatorElectricPowerAt100;
                        _electricPowerAt4 += generatorElectricPowerAt100;
                        _electricPowerAt2 += generatorElectricPowerAt100;
                        _electricPowerAtCustom += generatorElectricPowerAt100;
                        continue;
                    }

                    var hotColdBathEfficiencyAt100Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt100Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt100Percent), 0);
                    var hotColdBathEfficiencyAt90Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt90Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt90Percent), 0);
                    var hotColdBathEfficiencyAt80Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt80Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt80Percent), 0);
                    var hotColdBathEfficiencyAt70Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt70Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt70Percent), 0);
                    var hotColdBathEfficiencyAt60Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt60Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt60Percent), 0);
                    var hotColdBathEfficiencyAt50Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt50Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt50Percent), 0);
                    var hotColdBathEfficiencyAt45Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt45Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt45Percent), 0);
                    var hotColdBathEfficiencyAt40Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt40Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt40Percent), 0);
                    var hotColdBathEfficiencyAt35Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt35Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt35Percent), 0);
                    var hotColdBathEfficiencyAt30Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt30Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt30Percent), 0);
                    var hotColdBathEfficiencyAt25Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt25Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt25Percent), 0);
                    var hotColdBathEfficiencyAt20Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt20Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt20Percent), 0);
                    var hotColdBathEfficiencyAt15Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt15Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt15Percent), 0);
                    var hotColdBathEfficiencyAt10Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt10Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt10Percent), 0);
                    var hotColdBathEfficiencyAt8Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt8Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt8Percent), 0);
                    var hotColdBathEfficiencyAt6Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt6Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt6Percent), 0);
                    var hotColdBathEfficiencyAt4Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt4Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt4Percent), 0);
                    var hotColdBathEfficiencyAt2Percent = Math.Max(1 - 0.75 * _restingRadiatorTempAt2Percent / generator.GetHotBathTemperature(_restingRadiatorTempAt2Percent), 0);
                    var hotColdBathEfficiencyAtCustomPct = Math.Max(1 - 0.75 * _restingRadiatorTempAtCustomPct / generator.GetHotBathTemperature(_restingRadiatorTempAtCustomPct), 0);

                    _electricPowerAt100 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt100Percent;
                    _electricPowerAt90 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt90Percent * 0.90;
                    _electricPowerAt80 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt80Percent * 0.80;
                    _electricPowerAt70 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt70Percent * 0.70;
                    _electricPowerAt60 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt60Percent * 0.60;
                    _electricPowerAt50 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt50Percent * 0.50;
                    _electricPowerAt45 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt45Percent * 0.45;
                    _electricPowerAt40 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt40Percent * 0.40;
                    _electricPowerAt35 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt35Percent * 0.35;
                    _electricPowerAt30 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt30Percent * 0.30;
                    _electricPowerAt25 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt25Percent * 0.25;
                    _electricPowerAt20 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt20Percent * 0.20;
                    _electricPowerAt15 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt15Percent * 0.15;
                    _electricPowerAt10 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt10Percent * 0.10;
                    _electricPowerAt8 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt8Percent * 0.08;
                    _electricPowerAt6 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt6Percent * 0.06;
                    _electricPowerAt4 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt4Percent * 0.04;
                    _electricPowerAt2 += generatorMaximumGeneratorPower * hotColdBathEfficiencyAt2Percent * 0.02;
                    _electricPowerAtCustom += generatorMaximumGeneratorPower * hotColdBathEfficiencyAtCustomPct * customScenarioFraction;
                }
            }

            _bestScenarioPercentage = 0;
            _bestScenarioElectricPower = 0;

            GetBestPowerAndPercentage(100, _electricPowerAt100);
            GetBestPowerAndPercentage(90, _electricPowerAt90);
            GetBestPowerAndPercentage(80, _electricPowerAt80);
            GetBestPowerAndPercentage(70, _electricPowerAt70);
            GetBestPowerAndPercentage(60, _electricPowerAt60);
            GetBestPowerAndPercentage(50, _electricPowerAt50);
            GetBestPowerAndPercentage(45, _electricPowerAt45);
            GetBestPowerAndPercentage(40, _electricPowerAt40);
            GetBestPowerAndPercentage(35, _electricPowerAt35);
            GetBestPowerAndPercentage(30, _electricPowerAt30);
            GetBestPowerAndPercentage(25, _electricPowerAt25);
            GetBestPowerAndPercentage(20, _electricPowerAt20);
            GetBestPowerAndPercentage(15, _electricPowerAt15);
            GetBestPowerAndPercentage(10, _electricPowerAt10);
            GetBestPowerAndPercentage(8, _electricPowerAt8);
            GetBestPowerAndPercentage(6, _electricPowerAt6);
            GetBestPowerAndPercentage(4, _electricPowerAt4);
            GetBestPowerAndPercentage(2, _electricPowerAt2);
        }

        private void GetBestPowerAndPercentage (int percentage, double scenarioElectricPower)
        {
            if (scenarioElectricPower.IsInfinityOrNaN() ||  scenarioElectricPower <= _bestScenarioElectricPower) return;

            _bestScenarioPercentage = percentage;
            _bestScenarioElectricPower = scenarioElectricPower;
        }

        // ReSharper disable once UnusedMember.Global
        protected void OnGUI()
        {
            if (RenderWindow)
                windowPosition = GUILayout.Window(_thermalWindowId, windowPosition, Window, Localizer.Format("#LOC_KSPIE_VABThermalUI_title"));//"Interstellar Thermal Mechanics Helper"
        }

        private void Window(int windowId)
        {
            SetLabelColor();

            var guiLabelWidth = GUILayout.MinWidth(LabelWidth);
            var guiValueWidth = GUILayout.MinWidth(ValueWidth);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_EngineThrottlePercentage"), GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            engineThrottlePercentage = GUILayout.HorizontalSlider(engineThrottlePercentage, 0, 100, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(engineThrottlePercentage.ToString("0.0") + " %", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_AtmosphereTitle"), GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            atmosphereDensity = GUILayout.HorizontalSlider(atmosphereDensity, 0, 4, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(atmosphereDensity.ToString("0.00") + " " + Localizer.Format("#LOC_KSPIE_VABThermalUI_AtmosphereUnit"), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorsSubmergedPercentage"), GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);//Radiators Submerged (Percentage):
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            submergedPercentage = GUILayout.HorizontalSlider(submergedPercentage, 0, 100, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(submergedPercentage.ToString("0.0") + " %", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_VesselMass"), _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Vessel Mass:"
            GUILayout.Label((_dryMass + _wetMass).ToString("0.000") + " t", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalHeatProduction"), _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Heat Production:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_totalSourcePower), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            // prevent non logical input
            if (submergedPercentage > 0 && atmosphereDensity < 0.0313f)
                atmosphereDensity = 0.0313f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalAreaRadiators") + " (" + _numberOfRadiators + ")", _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Area Radiators:"
            GUILayout.Label(_vesselBaseRadiatorArea.ToString("0.0") + " m\xB2", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("Radiator Maximum Convection:"), _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Maximum Dissipation:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_vesselMaxRadConvection), _radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorMaximumDissipation"), _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Maximum Dissipation:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_vesselMaxRadDissipation), _radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingElectricPowerAt") + " " + _bestScenarioPercentage + "% Power", _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Resting Electric Power at"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_bestScenarioElectricPower), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_CustomReactorPowerPercentage"), GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth); // "Custom Reactor Power (Percentage)"
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            customScenarioPercentage = GUILayout.HorizontalSlider(customScenarioPercentage, 0, 100, GUILayout.ExpandWidth(true), guiLabelWidth);
            string customPercentageText = " " + customScenarioPercentage.ToString("0.0") + "%";
            GUILayout.Label(customPercentageText, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_WasteheatProductionAt") + customPercentageText, _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Wasteheat Production at "
            GUILayout.Label(PluginHelper.getFormattedPowerString(_wasteheatSourcePowerCustom), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ThermalSourceTemperatureAt") + customPercentageText, _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth); // "Thermal Source Temperature at"
            string sourceTempString100 = _averageSourceCoreTempAtCustom < 0 ? "N/A" : _averageSourceCoreTempAtCustom.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString100, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string restingRadiatorTempAtCustomPercentageStr = !_restingRadiatorTempAtCustomPct.IsInfinityOrNaN() ? _restingRadiatorTempAtCustomPct.ToString("0.0") + " K" : "N/A";
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorRestingTemperatureAt") + customPercentageText, _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Resting Temperature at"
            GUILayout.Label(restingRadiatorTempAtCustomPercentageStr, _radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            if (_hasThermalGenerators)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingGeneratorEfficiencyAt") + customPercentageText, _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth); //"Resting Generator Efficiency at"
                GUILayout.Label((_hotColdBathEfficiencyAtCustomPct * 100).ToString("0.00") + "%", _radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ElectricPowerOutputAt") + customPercentageText, _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Electric Power Output at"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_electricPowerAtCustom), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void SetLabelColor()
        {
            if (_blueLabel == null)
                _blueLabel = new GUIStyle(GUI.skin.label) {normal = {textColor = new Color(0, 191 / 255f, 255 / 255f, 1f)}};
            if (_greenLabel == null)
                _greenLabel = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.green}};
            if (_redLabel == null)
                _redLabel = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.red}};
            if (_orangeLabel == null)
                _orangeLabel = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold};

            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                RenderWindow = false;

            _radiatorLabel = _blueLabel;
            if (_bestScenarioPercentage >= 100 && _vesselMaxRadDissipation > _totalSourcePower) return;
            _radiatorLabel = _greenLabel;
            if (_bestScenarioPercentage >= 100 || _vesselMaxRadConvection > _totalSourcePower || _vesselMaxRadDissipation > _totalSourcePower) return;
                _radiatorLabel = _orangeLabel;
            if (_vesselMaxRadConvectionAndDissipation < _totalSourcePower * 0.3)
                _radiatorLabel = _redLabel;
        }
    }
}
