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

        private int _maxIterations = 10;
        private int _bestScenarioPercentage;
        private int _numberOfRadiators;
        private int _thermalWindowId = 825462;
        private bool _hasThermalGenerators;

        private const int LabelWidth = 300;
        private const int ValueWidth = 85;

        private Rect windowPosition = new Rect(500, 500, LabelWidth + ValueWidth, 100);

        private GUIStyle _boldLabel;
        private GUIStyle _blueLabel;
        private GUIStyle _greenLabel;
        private GUIStyle _redLabel;
        private GUIStyle _orangeLabel;
        private GUIStyle _radiatorLabel;

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

        private double _sourceTempAtCustom;
        private double _sourceTempAt100Pc;
        private double _sourceTempAt90Pc;
        private double _sourceTempAt80Pc;
        private double _sourceTempAt70Pc;
        private double _sourceTempAt60Pc;
        private double _sourceTempAt50Pc;
        private double _sourceTempAt45Pc;
        private double _sourceTempAt40Pc;
        private double _sourceTempAt35Pc;
        private double _sourceTempAt30Pc;
        private double _sourceTempAt25Pc;
        private double _sourceTempAt20Pc;
        private double _sourceTempAt15Pc;
        private double _sourceTempAt10Pc;
        private double _sourceTempAt8Pc;
        private double _sourceTempAt6Pc;
        private double _sourceTempAt4Pc;
        private double _sourceTempAt2Pc;

        private double _restingRadiatorTempAtCustom;
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
        private double _hotColdBathEfficiencyAt100Percent;
        private double _hotColdBathEfficiencyAt90Percent;
        private double _hotColdBathEfficiencyAt80Percent;
        private double _hotColdBathEfficiencyAt70Percent;
        private double _hotColdBathEfficiencyAt60Percent;
        private double _hotColdBathEfficiencyAt50Percent;
        private double _hotColdBathEfficiencyAt45Percent;
        private double _hotColdBathEfficiencyAt40Percent;
        private double _hotColdBathEfficiencyAt35Percent;
        private double _hotColdBathEfficiencyAt30Percent;
        private double _hotColdBathEfficiencyAt25Percent;
        private double _hotColdBathEfficiencyAt20Percent;
        private double _hotColdBathEfficiencyAt15Percent;
        private double _hotColdBathEfficiencyAt10Percent;
        private double _hotColdBathEfficiencyAt8Percent;
        private double _hotColdBathEfficiencyAt6Percent;
        private double _hotColdBathEfficiencyAt4Percent;
        private double _hotColdBathEfficiencyAt2Percent;

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
            IEnumerable<IFNPowerSource> thermalSources,
            IEnumerable<BeamedPowerReceiver> beamedReceivers,
            IEnumerable<BeamedPowerTransmitter> beamedTransmitter,
            IEnumerable<ThermalEngineController> thermalEngines,
            IEnumerable<FusionECU2> variableEngines,
            IEnumerable<DaedalusEngineController> fusionEngines,
            List<FNGenerator> generators,
            IEnumerable<FNRadiator> radiators)
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

            _sourceTempAtCustom = double.MaxValue;
            _sourceTempAt100Pc = double.MaxValue;
            _sourceTempAt90Pc = double.MaxValue;
            _sourceTempAt80Pc = double.MaxValue;
            _sourceTempAt70Pc = double.MaxValue;
            _sourceTempAt60Pc = double.MaxValue;
            _sourceTempAt50Pc = double.MaxValue;
            _sourceTempAt45Pc = double.MaxValue;
            _sourceTempAt40Pc = double.MaxValue;
            _sourceTempAt35Pc = double.MaxValue;
            _sourceTempAt30Pc = double.MaxValue;
            _sourceTempAt25Pc = double.MaxValue;
            _sourceTempAt20Pc = double.MaxValue;
            _sourceTempAt15Pc = double.MaxValue;
            _sourceTempAt10Pc = double.MaxValue;
            _sourceTempAt8Pc = double.MaxValue;
            _sourceTempAt6Pc = double.MaxValue;
            _sourceTempAt4Pc = double.MaxValue;
            _sourceTempAt2Pc = double.MaxValue;

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
                var coreTempAtRadiatorTempAtCustomPct = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAtCustom);

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

                double thermalGeneratorMaxEfficiency = connectedThermalPowerGenerator == null ? 0 : connectedThermalPowerGenerator.MaxEfficiency;

                var effectiveThermalPowerAtCustomPct = combinedRawSourcePowerAtCustomPct * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAtCustomPct);
                var effectiveThermalPowerAt100Percent = combinedRawSourcePowerAt100Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt100Percent);
                var effectiveThermalPowerAt90Percent = combinedRawSourcePowerAt90Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt90Percent);
                var effectiveThermalPowerAt80Percent = combinedRawSourcePowerAt80Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt80Percent);
                var effectiveThermalPowerAt70Percent = combinedRawSourcePowerAt70Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt70Percent);
                var effectiveThermalPowerAt60Percent = combinedRawSourcePowerAt60Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt60Percent);
                var effectiveThermalPowerAt50Percent = combinedRawSourcePowerAt50Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt50Percent);
                var effectiveThermalPowerAt45Percent = combinedRawSourcePowerAt45Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt45Percent);
                var effectiveThermalPowerAt40Percent = combinedRawSourcePowerAt40Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt40Percent);
                var effectiveThermalPowerAt35Percent = combinedRawSourcePowerAt35Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt35Percent);
                var effectiveThermalPowerAt30Percent = combinedRawSourcePowerAt30Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt30Percent);
                var effectiveThermalPowerAt25Percent = combinedRawSourcePowerAt25Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt25Percent);
                var effectiveThermalPowerAt20Percent = combinedRawSourcePowerAt20Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt20Percent);
                var effectiveThermalPowerAt15Percent = combinedRawSourcePowerAt15Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt15Percent);
                var effectiveThermalPowerAt10Percent = combinedRawSourcePowerAt10Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt10Percent);
                var effectiveThermalPowerAt8Percent = combinedRawSourcePowerAt8Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt8Percent);
                var effectiveThermalPowerAt6Percent = combinedRawSourcePowerAt6Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt6Percent);
                var effectiveThermalPowerAt4Percent = combinedRawSourcePowerAt4Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt4Percent);
                var effectiveThermalPowerAt2Percent = combinedRawSourcePowerAt2Percent * (1 - thermalGeneratorMaxEfficiency * _hotColdBathEfficiencyAt2Percent);

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
                var effectiveWasteheatAtCustom = effectiveChargedPowerAtCustom + effectiveThermalPowerAtCustomPct + effectiveWastedPowerAtCustomPct;

                totalTemperaturePowerAt100Percent += coreTempAtRadiatorTempAt100Percent * effectiveWasteheatAt100Percent;
                totalTemperaturePowerAt90Percent += coreTempAtRadiatorTempAt90Percent * effectiveWasteheatAt90Percent;
                totalTemperaturePowerAt80Percent += coreTempAtRadiatorTempAt80Percent * effectiveWasteheatAt80Percent;
                totalTemperaturePowerAt70Percent += coreTempAtRadiatorTempAt70Percent * effectiveWasteheatAt70Percent;
                totalTemperaturePowerAt60Percent += coreTempAtRadiatorTempAt60Percent * effectiveWasteheatAt60Percent;
                totalTemperaturePowerAt50Percent += coreTempAtRadiatorTempAt50Percent * effectiveWasteheatAt50Percent;
                totalTemperaturePowerAt45Percent += coreTempAtRadiatorTempAt45Percent * effectiveWasteheatAt45Percent;
                totalTemperaturePowerAt40Percent += coreTempAtRadiatorTempAt40Percent * effectiveWasteheatAt40Percent;
                totalTemperaturePowerAt35Percent += coreTempAtRadiatorTempAt35Percent * effectiveWasteheatAt35Percent;
                totalTemperaturePowerAt30Percent += coreTempAtRadiatorTempAt30Percent * effectiveWasteheatAt30Percent;
                totalTemperaturePowerAt25Percent += coreTempAtRadiatorTempAt25Percent * effectiveWasteheatAt25Percent;
                totalTemperaturePowerAt20Percent += coreTempAtRadiatorTempAt20Percent * effectiveWasteheatAt20Percent;
                totalTemperaturePowerAt15Percent += coreTempAtRadiatorTempAt15Percent * effectiveWasteheatAt15Percent;
                totalTemperaturePowerAt10Percent += coreTempAtRadiatorTempAt10Percent * effectiveWasteheatAt10Percent;
                totalTemperaturePowerAt8Percent += coreTempAtRadiatorTempAt8Percent * effectiveWasteheatAt8Percent;
                totalTemperaturePowerAt6Percent += coreTempAtRadiatorTempAt6Percent * effectiveWasteheatAt6Percent;
                totalTemperaturePowerAt4Percent += coreTempAtRadiatorTempAt4Percent * effectiveWasteheatAt4Percent;
                totalTemperaturePowerAt2Percent += coreTempAtRadiatorTempAt2Percent * effectiveWasteheatAt2Percent;
                totalTemperaturePowerAtCustom += coreTempAtRadiatorTempAtCustomPct * effectiveWasteheatAtCustom;

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
                _wasteheatSourcePowerCustom += effectiveWasteheatAtCustom;
            }

            // calculated weighted core temperatures
            if (_wasteheatSourcePowerCustom > 0) _sourceTempAtCustom = totalTemperaturePowerAtCustom / _wasteheatSourcePowerCustom;
            if (_wasteheatSourcePower100Pc > 0) _sourceTempAt100Pc = totalTemperaturePowerAt100Percent / _wasteheatSourcePower100Pc;
            if (_wasteheatSourcePower90Pc > 0) _sourceTempAt90Pc = totalTemperaturePowerAt90Percent / _wasteheatSourcePower90Pc;
            if (_wasteheatSourcePower80Pc > 0) _sourceTempAt80Pc = totalTemperaturePowerAt80Percent / _wasteheatSourcePower80Pc;
            if (_wasteheatSourcePower70Pc > 0) _sourceTempAt70Pc = totalTemperaturePowerAt70Percent / _wasteheatSourcePower70Pc;
            if (_wasteheatSourcePower60Pc > 0) _sourceTempAt60Pc = totalTemperaturePowerAt60Percent / _wasteheatSourcePower60Pc;
            if (_wasteheatSourcePower50Pc > 0) _sourceTempAt50Pc = totalTemperaturePowerAt50Percent / _wasteheatSourcePower50Pc;
            if (_wasteheatSourcePower45Pc > 0) _sourceTempAt45Pc = totalTemperaturePowerAt45Percent / _wasteheatSourcePower45Pc;
            if (_wasteheatSourcePower40Pc > 0) _sourceTempAt40Pc = totalTemperaturePowerAt40Percent / _wasteheatSourcePower40Pc;
            if (_wasteheatSourcePower35Pc > 0) _sourceTempAt35Pc = totalTemperaturePowerAt35Percent / _wasteheatSourcePower35Pc;
            if (_wasteheatSourcePower30Pc > 0) _sourceTempAt30Pc = totalTemperaturePowerAt30Percent / _wasteheatSourcePower30Pc;
            if (_wasteheatSourcePower25Pc > 0) _sourceTempAt25Pc = totalTemperaturePowerAt25Percent / _wasteheatSourcePower25Pc;
            if (_wasteheatSourcePower20Pc > 0) _sourceTempAt20Pc = totalTemperaturePowerAt20Percent / _wasteheatSourcePower20Pc;
            if (_wasteheatSourcePower15Pc > 0) _sourceTempAt15Pc = totalTemperaturePowerAt15Percent / _wasteheatSourcePower15Pc;
            if (_wasteheatSourcePower10Pc > 0) _sourceTempAt10Pc = totalTemperaturePowerAt10Percent / _wasteheatSourcePower10Pc;
            if (_wasteheatSourcePower8Pc > 0) _sourceTempAt8Pc = totalTemperaturePowerAt8Percent / _wasteheatSourcePower8Pc;
            if (_wasteheatSourcePower6Pc > 0) _sourceTempAt6Pc = totalTemperaturePowerAt6Percent / _wasteheatSourcePower6Pc;
            if (_wasteheatSourcePower4Pc > 0) _sourceTempAt4Pc = totalTemperaturePowerAt4Percent / _wasteheatSourcePower4Pc;
            if (_wasteheatSourcePower2Pc > 0) _sourceTempAt2Pc = totalTemperaturePowerAt2Percent / _wasteheatSourcePower2Pc;

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
            _radMaxDissipation = 0;
            _averageRadTemp = 0;
            _totalArea = 0;

            foreach (FNRadiator radiator in radiators)
            {
                _numberOfRadiators++;
                _totalArea += radiator.BaseRadiatorArea;
                var maxRadTemperature = Math.Min(radiator.MaxRadiatorTemperature, _sourceTempAt100Pc);
                var tempToPowerFour = maxRadTemperature * maxRadTemperature * maxRadTemperature * maxRadTemperature;
                _radMaxDissipation += GameConstants.stefan_const * radiator.EffectiveRadiatorArea * tempToPowerFour / 1e6;
                _averageRadTemp += maxRadTemperature;
            }

            _averageRadTemp = _numberOfRadiators != 0 ? _averageRadTemp / _numberOfRadiators : double.NaN;

            var radRatioCustom = _wasteheatSourcePowerCustom / _radMaxDissipation;
            var radRatio100Pc = _wasteheatSourcePower100Pc / _radMaxDissipation;
            var radRatio90Pc = _wasteheatSourcePower90Pc / _radMaxDissipation;
            var radRatio80Pc = _wasteheatSourcePower80Pc / _radMaxDissipation;
            var radRatio70Pc = _wasteheatSourcePower70Pc / _radMaxDissipation;
            var radRatio60Pc = _wasteheatSourcePower60Pc / _radMaxDissipation;
            var radRatio50Pc = _wasteheatSourcePower50Pc / _radMaxDissipation;
            var radRatio45Pc = _wasteheatSourcePower45Pc / _radMaxDissipation;
            var radRatio40Pc = _wasteheatSourcePower40Pc / _radMaxDissipation;
            var radRatio35Pc = _wasteheatSourcePower35Pc / _radMaxDissipation;
            var radRatio30Pc = _wasteheatSourcePower30Pc / _radMaxDissipation;
            var radRatio25Pc = _wasteheatSourcePower25Pc / _radMaxDissipation;
            var radRatio20Pc = _wasteheatSourcePower20Pc / _radMaxDissipation;
            var radRatio15Pc = _wasteheatSourcePower15Pc / _radMaxDissipation;
            var radRatio10Pc = _wasteheatSourcePower10Pc / _radMaxDissipation;
            var radRatio8Pc = _wasteheatSourcePower8Pc / _radMaxDissipation;
            var radRatio6Pc = _wasteheatSourcePower6Pc / _radMaxDissipation;
            var radRatio4Pc = _wasteheatSourcePower4Pc / _radMaxDissipation;
            var radRatio2Pc = _wasteheatSourcePower2Pc / _radMaxDissipation;

            _restingRadiatorTempAtCustom = (!radRatioCustom.IsInfinityOrNaN() ? Math.Pow(radRatioCustom, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt100Percent = (!radRatio100Pc.IsInfinityOrNaN() ? Math.Pow(radRatio100Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt90Percent = (!radRatio90Pc.IsInfinityOrNaN() ? Math.Pow(radRatio90Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt80Percent = (!radRatio80Pc.IsInfinityOrNaN() ? Math.Pow(radRatio80Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt70Percent = (!radRatio70Pc.IsInfinityOrNaN() ? Math.Pow(radRatio70Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt60Percent = (!radRatio60Pc.IsInfinityOrNaN() ? Math.Pow(radRatio60Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt50Percent = (!radRatio50Pc.IsInfinityOrNaN() ? Math.Pow(radRatio50Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt45Percent = (!radRatio45Pc.IsInfinityOrNaN() ? Math.Pow(radRatio45Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt40Percent = (!radRatio40Pc.IsInfinityOrNaN() ? Math.Pow(radRatio40Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt35Percent = (!radRatio30Pc.IsInfinityOrNaN() ? Math.Pow(radRatio35Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt30Percent = (!radRatio30Pc.IsInfinityOrNaN() ? Math.Pow(radRatio30Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt25Percent = (!radRatio25Pc.IsInfinityOrNaN() ? Math.Pow(radRatio25Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt20Percent = (!radRatio20Pc.IsInfinityOrNaN() ? Math.Pow(radRatio20Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt15Percent = (!radRatio15Pc.IsInfinityOrNaN() ? Math.Pow(radRatio15Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt10Percent = (!radRatio10Pc.IsInfinityOrNaN() ? Math.Pow(radRatio10Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt8Percent = (!radRatio8Pc.IsInfinityOrNaN() ? Math.Pow(radRatio8Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt6Percent = (!radRatio6Pc.IsInfinityOrNaN() ? Math.Pow(radRatio6Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt4Percent = (!radRatio4Pc.IsInfinityOrNaN() ? Math.Pow(radRatio4Pc, 0.25) : 0) * _averageRadTemp;
            _restingRadiatorTempAt2Percent = (!radRatio2Pc.IsInfinityOrNaN() ? Math.Pow(radRatio2Pc, 0.25) : 0) * _averageRadTemp;

            var thermalGenerators = generators.Where(m => !m.chargedParticleMode).ToList();

            if (thermalGenerators.Count > 0)
            {
                _hasThermalGenerators = true;

                _hotColdBathEfficiencyAtCustomPct = CalculateHotColdBathEfficiency(_sourceTempAtCustom, _restingRadiatorTempAtCustom);
                _hotColdBathEfficiencyAt100Percent = CalculateHotColdBathEfficiency(_sourceTempAt100Pc, _restingRadiatorTempAt100Percent);
                _hotColdBathEfficiencyAt90Percent = CalculateHotColdBathEfficiency(_sourceTempAt90Pc, _restingRadiatorTempAt90Percent);
                _hotColdBathEfficiencyAt80Percent = CalculateHotColdBathEfficiency(_sourceTempAt80Pc, _restingRadiatorTempAt80Percent);
                _hotColdBathEfficiencyAt70Percent = CalculateHotColdBathEfficiency(_sourceTempAt70Pc, _restingRadiatorTempAt70Percent);
                _hotColdBathEfficiencyAt60Percent = CalculateHotColdBathEfficiency(_sourceTempAt60Pc, _restingRadiatorTempAt60Percent);
                _hotColdBathEfficiencyAt50Percent = CalculateHotColdBathEfficiency(_sourceTempAt50Pc, _restingRadiatorTempAt50Percent);
                _hotColdBathEfficiencyAt45Percent = CalculateHotColdBathEfficiency(_sourceTempAt45Pc, _restingRadiatorTempAt45Percent);
                _hotColdBathEfficiencyAt40Percent = CalculateHotColdBathEfficiency(_sourceTempAt40Pc, _restingRadiatorTempAt40Percent);
                _hotColdBathEfficiencyAt35Percent = CalculateHotColdBathEfficiency(_sourceTempAt35Pc, _restingRadiatorTempAt35Percent);
                _hotColdBathEfficiencyAt30Percent = CalculateHotColdBathEfficiency(_sourceTempAt30Pc, _restingRadiatorTempAt30Percent);
                _hotColdBathEfficiencyAt25Percent = CalculateHotColdBathEfficiency(_sourceTempAt25Pc, _restingRadiatorTempAt25Percent);
                _hotColdBathEfficiencyAt20Percent = CalculateHotColdBathEfficiency(_sourceTempAt20Pc, _restingRadiatorTempAt20Percent);
                _hotColdBathEfficiencyAt15Percent = CalculateHotColdBathEfficiency(_sourceTempAt15Pc, _restingRadiatorTempAt15Percent);
                _hotColdBathEfficiencyAt10Percent = CalculateHotColdBathEfficiency(_sourceTempAt10Pc, _restingRadiatorTempAt10Percent);
                _hotColdBathEfficiencyAt8Percent = CalculateHotColdBathEfficiency(_sourceTempAt8Pc, _restingRadiatorTempAt8Percent);
                _hotColdBathEfficiencyAt6Percent = CalculateHotColdBathEfficiency(_sourceTempAt6Pc, _restingRadiatorTempAt6Percent);
                _hotColdBathEfficiencyAt4Percent = CalculateHotColdBathEfficiency(_sourceTempAt4Pc, _restingRadiatorTempAt4Percent);
                _hotColdBathEfficiencyAt2Percent = CalculateHotColdBathEfficiency(_sourceTempAt2Pc, _restingRadiatorTempAt2Percent);
            }
            else
                _hasThermalGenerators = false;

            if (_sourceTempAtCustom >= double.MaxValue) _sourceTempAtCustom = -1;
            if (_sourceTempAt100Pc >= double.MaxValue) _sourceTempAt100Pc = -1;
            if (_sourceTempAt90Pc >= double.MaxValue) _sourceTempAt90Pc = -1;
            if (_sourceTempAt80Pc >= double.MaxValue) _sourceTempAt80Pc = -1;
            if (_sourceTempAt70Pc >= double.MaxValue) _sourceTempAt70Pc = -1;
            if (_sourceTempAt60Pc >= double.MaxValue) _sourceTempAt60Pc = -1;
            if (_sourceTempAt50Pc >= double.MaxValue) _sourceTempAt50Pc = -1;
            if (_sourceTempAt45Pc >= double.MaxValue) _sourceTempAt45Pc = -1;
            if (_sourceTempAt40Pc >= double.MaxValue) _sourceTempAt40Pc = -1;
            if (_sourceTempAt35Pc >= double.MaxValue) _sourceTempAt35Pc = -1;
            if (_sourceTempAt30Pc >= double.MaxValue) _sourceTempAt30Pc = -1;
            if (_sourceTempAt25Pc >= double.MaxValue) _sourceTempAt25Pc = -1;
            if (_sourceTempAt20Pc >= double.MaxValue) _sourceTempAt20Pc = -1;
            if (_sourceTempAt15Pc >= double.MaxValue) _sourceTempAt15Pc = -1;
            if (_sourceTempAt10Pc >= double.MaxValue) _sourceTempAt10Pc = -1;
            if (_sourceTempAt8Pc >= double.MaxValue) _sourceTempAt8Pc = -1;
            if (_sourceTempAt6Pc >= double.MaxValue) _sourceTempAt6Pc = -1;
            if (_sourceTempAt4Pc >= double.MaxValue) _sourceTempAt4Pc = -1;
            if (_sourceTempAt2Pc >= double.MaxValue) _sourceTempAt2Pc = -1;
        }

        private double CalculateHotColdBathEfficiency(double sourceTemperature, double restingTemperature)
        {
            if (sourceTemperature >= double.MaxValue || restingTemperature.IsInfinityOrNaN())
                return 0;

            return Math.Max(1 - restingTemperature / _sourceTempAt100Pc, 0);
        }

        private void CalculateGeneratedElectricPower(List<FNGenerator> generators)
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

                    _electricPowerAt100 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt100Percent;

                    if (generator.isLimitedByMinThrottle)
                    {
                        _electricPowerAt90 += _electricPowerAt100;
                        _electricPowerAt80 += _electricPowerAt100;
                        _electricPowerAt70 += _electricPowerAt100;
                        _electricPowerAt60 += _electricPowerAt100;
                        _electricPowerAt50 += _electricPowerAt100;
                        _electricPowerAt45 += _electricPowerAt100;
                        _electricPowerAt40 += _electricPowerAt100;
                        _electricPowerAt35 += _electricPowerAt100;
                        _electricPowerAt30 += _electricPowerAt100;
                        _electricPowerAt25 += _electricPowerAt100;
                        _electricPowerAt20 += _electricPowerAt100;
                        _electricPowerAt15 += _electricPowerAt100;
                        _electricPowerAt10 += _electricPowerAt100;
                        _electricPowerAt8 += _electricPowerAt100;
                        _electricPowerAt6 += _electricPowerAt100;
                        _electricPowerAt4 += _electricPowerAt100;
                        _electricPowerAt2 += _electricPowerAt100;
                        _electricPowerAtCustom += _electricPowerAt100;
                        continue;
                    }

                    _electricPowerAt90 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt90Percent * 0.90;
                    _electricPowerAt80 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt80Percent * 0.80;
                    _electricPowerAt70 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt70Percent * 0.70;
                    _electricPowerAt60 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt60Percent * 0.60;
                    _electricPowerAt50 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt50Percent * 0.50;
                    _electricPowerAt45 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt45Percent * 0.45;
                    _electricPowerAt40 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt40Percent * 0.40;
                    _electricPowerAt35 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt35Percent * 0.35;
                    _electricPowerAt30 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt30Percent * 0.30;
                    _electricPowerAt25 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt25Percent * 0.25;
                    _electricPowerAt20 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt20Percent * 0.20;
                    _electricPowerAt15 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt15Percent * 0.15;
                    _electricPowerAt10 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt10Percent * 0.10;
                    _electricPowerAt8 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt8Percent * 0.08;
                    _electricPowerAt6 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt6Percent * 0.06;
                    _electricPowerAt4 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt4Percent * 0.04;
                    _electricPowerAt2 += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAt2Percent * 0.02;
                    _electricPowerAtCustom += generatorMaximumGeneratorPower * _hotColdBathEfficiencyAtCustomPct * customScenarioFraction;
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
            if (scenarioElectricPower <= _bestScenarioElectricPower) return;

            _bestScenarioPercentage = percentage;
            _bestScenarioElectricPower = scenarioElectricPower;
        }

        // ReSharper disable once UnusedMember.Global
        protected void OnGUI()
        {
            if (renderWindow)
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
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalHeatProduction"), _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Heat Production:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_totalSourcePower), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_VesselMass"), _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Vessel Mass:"
            GUILayout.Label((_dryMass + _wetMass).ToString("0.000") + " t", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalAreaRadiators") + " (" + _numberOfRadiators + ")", _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Area Radiators:"
            GUILayout.Label(_totalArea.ToString("0.0") + " m\xB2", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorMaximumDissipation"), _boldLabel, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Maximum Dissipation:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(_radMaxDissipation), _radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
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
            string sourceTempString100 = _sourceTempAtCustom < 0 ? "N/A" : _sourceTempAtCustom.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString100, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string restingRadiatorTempAtCustomPercentageStr = !_restingRadiatorTempAtCustom.IsInfinityOrNaN() ? _restingRadiatorTempAtCustom.ToString("0.0") + " K" : "N/A";
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
                renderWindow = false;

            _radiatorLabel = _blueLabel;
            if (_bestScenarioPercentage >= 100) return;
            _radiatorLabel = _greenLabel;
            if (!(_radMaxDissipation < _totalSourcePower)) return;
            _radiatorLabel = _orangeLabel;
            if (_radMaxDissipation < _totalSourcePower * 0.3)
                _radiatorLabel = _redLabel;
        }
    }
}
