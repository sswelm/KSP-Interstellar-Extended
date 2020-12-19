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

        private double _generatorEfficiencyAtCustom;
        private double _generatorEfficiencyAt100Percent;
        private double _generatorEfficiencyAt90Percent;
        private double _generatorEfficiencyAt80Percent;
        private double _generatorEfficiencyAt70Percent;
        private double _generatorEfficiencyAt60Percent;
        private double _generatorEfficiencyAt50Percent;
        private double _generatorEfficiencyAt45Percent;
        private double _generatorEfficiencyAt40Percent;
        private double _generatorEfficiencyAt35Percent;
        private double _generatorEfficiencyAt30Percent;
        private double _generatorEfficiencyAt25Percent;
        private double _generatorEfficiencyAt20Percent;
        private double _generatorEfficiencyAt15Percent;
        private double _generatorEfficiencyAt10Percent;
        private double _generatorEfficiencyAt8Percent;
        private double _generatorEfficiencyAt6Percent;
        private double _generatorEfficiencyAt4Percent;
        private double _generatorEfficiencyAt2Percent;

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

            for (var iteration = 0; iteration < _maxIterations; iteration++)
            {
                CalculatePowerBalance(thermalSources, beamedReceivers, beamedTransmitter, thermalEngines, variableEngines, fusionEngines, generators, radiators);
            }
        }

        private void CalculatePowerBalance(
            IEnumerable<IPowerSource> thermalSources,
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
            foreach (IPowerSource powerSource in thermalSources)
            {
                _totalSourcePower += powerSource.MaximumPower;

                double combinedMaxStableMegaWattPower = 0;

                var connectedThermalPowerGenerator = powerSource.ConnectedThermalElectricGenerator;
                var connectedChargedPowerGenerator = powerSource.ConnectedChargedParticleElectricGenerator;

                // when connected to a thermal source, assume most thermal energy thermal power can end up in the radiators
                if (connectedThermalPowerGenerator != null) combinedMaxStableMegaWattPower += (1 - powerSource.ChargedPowerRatio) * connectedThermalPowerGenerator.MaxStableMegaWattPower;

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
                var coreTempAtRadiatorTempAtCustom = powerSource.GetCoreTempAtRadiatorTemp(_restingRadiatorTempAtCustom);

                var effectivePowerAtCustom = (1 - _generatorEfficiencyAtCustom) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAtCustom) * customScenarioFraction);
                var effectivePowerAt100Percent = (1 - _generatorEfficiencyAt100Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt100Percent));
                var effectivePowerAt90Percent = (1 - _generatorEfficiencyAt90Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt90Percent) * 0.90);
                var effectivePowerAt80Percent = (1 - _generatorEfficiencyAt80Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt80Percent) * 0.80);
                var effectivePowerAt70Percent = (1 - _generatorEfficiencyAt70Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt70Percent) * 0.70);
                var effectivePowerAt60Percent = (1 - _generatorEfficiencyAt60Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt60Percent) * 0.60);
                var effectivePowerAt50Percent = (1 - _generatorEfficiencyAt50Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt50Percent) * 0.50);
                var effectivePowerAt45Percent = (1 - _generatorEfficiencyAt45Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt45Percent) * 0.45);
                var effectivePowerAt40Percent = (1 - _generatorEfficiencyAt40Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt40Percent) * 0.40);
                var effectivePowerAt35Percent = (1 - _generatorEfficiencyAt35Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt35Percent) * 0.35);
                var effectivePowerAt30Percent = (1 - _generatorEfficiencyAt30Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt30Percent) * 0.30);
                var effectivePowerAt25Percent = (1 - _generatorEfficiencyAt25Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt25Percent) * 0.25);
                var effectivePowerAt20Percent = (1 - _generatorEfficiencyAt20Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt20Percent) * 0.20);
                var effectivePowerAt15Percent = (1 - _generatorEfficiencyAt15Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt15Percent) * 0.15);
                var effectivePowerAt10Percent = (1 - _generatorEfficiencyAt10Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt10Percent) * 0.10);
                var effectivePowerAt8Percent = (1 - _generatorEfficiencyAt8Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt8Percent) * 0.08);
                var effectivePowerAt6Percent = (1 - _generatorEfficiencyAt6Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt6Percent) * 0.06);
                var effectivePowerAt4Percent = (1 - _generatorEfficiencyAt4Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt4Percent) * 0.04);
                var effectivePowerAt2Percent = (1 - _generatorEfficiencyAt2Percent) * Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt2Percent) * 0.02);

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

                _wasteheatSourcePower100Pc += effectivePowerAt100Percent;
                _wasteheatSourcePower90Pc += effectivePowerAt90Percent;
                _wasteheatSourcePower80Pc += effectivePowerAt80Percent;
                _wasteheatSourcePower70Pc += effectivePowerAt70Percent;
                _wasteheatSourcePower60Pc += effectivePowerAt60Percent;
                _wasteheatSourcePower50Pc += effectivePowerAt50Percent;
                _wasteheatSourcePower45Pc += effectivePowerAt45Percent;
                _wasteheatSourcePower40Pc += effectivePowerAt40Percent;
                _wasteheatSourcePower35Pc += effectivePowerAt35Percent;
                _wasteheatSourcePower30Pc += effectivePowerAt30Percent;
                _wasteheatSourcePower25Pc += effectivePowerAt25Percent;
                _wasteheatSourcePower20Pc += effectivePowerAt20Percent;
                _wasteheatSourcePower15Pc += effectivePowerAt15Percent;
                _wasteheatSourcePower10Pc += effectivePowerAt10Percent;
                _wasteheatSourcePower8Pc += effectivePowerAt8Percent;
                _wasteheatSourcePower6Pc += effectivePowerAt6Percent;
                _wasteheatSourcePower4Pc += effectivePowerAt4Percent;
                _wasteheatSourcePower2Pc += effectivePowerAt2Percent;
                _wasteheatSourcePowerCustom += effectivePowerAtCustom;
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
                var maximumGeneratedPower = thermalGenerators.Sum(m => m.maximumGeneratorPowerMJ);
                var averageMaxEfficiency = thermalGenerators.Sum(m => m.maxEfficiency * m.maximumGeneratorPowerMJ) /
                                           maximumGeneratedPower;

                _hasThermalGenerators = true;

                _generatorEfficiencyAtCustom = CalculateGeneratorAtPercentage(_sourceTempAtCustom, _restingRadiatorTempAtCustom, averageMaxEfficiency);
                _generatorEfficiencyAt100Percent = CalculateGeneratorAtPercentage(_sourceTempAt100Pc, _restingRadiatorTempAt100Percent, averageMaxEfficiency);
                _generatorEfficiencyAt90Percent = CalculateGeneratorAtPercentage(_sourceTempAt90Pc, _restingRadiatorTempAt90Percent, averageMaxEfficiency);
                _generatorEfficiencyAt80Percent = CalculateGeneratorAtPercentage(_sourceTempAt80Pc, _restingRadiatorTempAt80Percent, averageMaxEfficiency);
                _generatorEfficiencyAt70Percent = CalculateGeneratorAtPercentage(_sourceTempAt70Pc, _restingRadiatorTempAt70Percent, averageMaxEfficiency);
                _generatorEfficiencyAt60Percent = CalculateGeneratorAtPercentage(_sourceTempAt60Pc, _restingRadiatorTempAt60Percent, averageMaxEfficiency);
                _generatorEfficiencyAt50Percent = CalculateGeneratorAtPercentage(_sourceTempAt50Pc, _restingRadiatorTempAt50Percent, averageMaxEfficiency);
                _generatorEfficiencyAt45Percent = CalculateGeneratorAtPercentage(_sourceTempAt45Pc, _restingRadiatorTempAt45Percent, averageMaxEfficiency);
                _generatorEfficiencyAt40Percent = CalculateGeneratorAtPercentage(_sourceTempAt40Pc, _restingRadiatorTempAt40Percent, averageMaxEfficiency);
                _generatorEfficiencyAt35Percent = CalculateGeneratorAtPercentage(_sourceTempAt35Pc, _restingRadiatorTempAt35Percent, averageMaxEfficiency);
                _generatorEfficiencyAt30Percent = CalculateGeneratorAtPercentage(_sourceTempAt30Pc, _restingRadiatorTempAt30Percent, averageMaxEfficiency);
                _generatorEfficiencyAt25Percent = CalculateGeneratorAtPercentage(_sourceTempAt25Pc, _restingRadiatorTempAt25Percent, averageMaxEfficiency);
                _generatorEfficiencyAt20Percent = CalculateGeneratorAtPercentage(_sourceTempAt20Pc, _restingRadiatorTempAt20Percent, averageMaxEfficiency);
                _generatorEfficiencyAt15Percent = CalculateGeneratorAtPercentage(_sourceTempAt15Pc, _restingRadiatorTempAt15Percent, averageMaxEfficiency);
                _generatorEfficiencyAt10Percent = CalculateGeneratorAtPercentage(_sourceTempAt10Pc, _restingRadiatorTempAt10Percent, averageMaxEfficiency);
                _generatorEfficiencyAt8Percent = CalculateGeneratorAtPercentage(_sourceTempAt8Pc, _restingRadiatorTempAt8Percent, averageMaxEfficiency);
                _generatorEfficiencyAt6Percent = CalculateGeneratorAtPercentage(_sourceTempAt6Pc, _restingRadiatorTempAt6Percent, averageMaxEfficiency);
                _generatorEfficiencyAt4Percent = CalculateGeneratorAtPercentage(_sourceTempAt4Pc, _restingRadiatorTempAt4Percent, averageMaxEfficiency);
                _generatorEfficiencyAt2Percent = CalculateGeneratorAtPercentage(_sourceTempAt2Pc, _restingRadiatorTempAt2Percent, averageMaxEfficiency);
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

        private double CalculateGeneratorAtPercentage(double sourceTemperature, double restingTemperature, double efficiency)
        {
            if (sourceTemperature >= double.MaxValue || restingTemperature.IsInfinityOrNaN())
                return 0;

            return Math.Max(efficiency * (1 - restingTemperature / _sourceTempAt100Pc), 0);
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
                    _electricPowerAt100 += generator.maximumGeneratorPowerMJ;
                }
                else
                {
                    var generatorMaximumGeneratorPower = generator.maximumGeneratorPowerMJ;

                    _electricPowerAt100 += generatorMaximumGeneratorPower * _generatorEfficiencyAt100Percent;

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

                    _electricPowerAt90 += generatorMaximumGeneratorPower * _generatorEfficiencyAt90Percent * 0.90;
                    _electricPowerAt80 += generatorMaximumGeneratorPower * _generatorEfficiencyAt80Percent * 0.80;
                    _electricPowerAt70 += generatorMaximumGeneratorPower * _generatorEfficiencyAt70Percent * 0.70;
                    _electricPowerAt60 += generatorMaximumGeneratorPower * _generatorEfficiencyAt60Percent * 0.60;
                    _electricPowerAt50 += generatorMaximumGeneratorPower * _generatorEfficiencyAt50Percent * 0.50;
                    _electricPowerAt45 += generatorMaximumGeneratorPower * _generatorEfficiencyAt45Percent * 0.45;
                    _electricPowerAt40 += generatorMaximumGeneratorPower * _generatorEfficiencyAt40Percent * 0.40;
                    _electricPowerAt35 += generatorMaximumGeneratorPower * _generatorEfficiencyAt35Percent * 0.35;
                    _electricPowerAt30 += generatorMaximumGeneratorPower * _generatorEfficiencyAt30Percent * 0.30;
                    _electricPowerAt25 += generatorMaximumGeneratorPower * _generatorEfficiencyAt25Percent * 0.25;
                    _electricPowerAt20 += generatorMaximumGeneratorPower * _generatorEfficiencyAt20Percent * 0.20;
                    _electricPowerAt15 += generatorMaximumGeneratorPower * _generatorEfficiencyAt15Percent * 0.15;
                    _electricPowerAt10 += generatorMaximumGeneratorPower * _generatorEfficiencyAt10Percent * 0.10;
                    _electricPowerAt8 += generatorMaximumGeneratorPower * _generatorEfficiencyAt8Percent * 0.08;
                    _electricPowerAt6 += generatorMaximumGeneratorPower * _generatorEfficiencyAt6Percent * 0.06;
                    _electricPowerAt4 += generatorMaximumGeneratorPower * _generatorEfficiencyAt4Percent * 0.04;
                    _electricPowerAt2 += generatorMaximumGeneratorPower * _generatorEfficiencyAt2Percent * 0.02;
                    _electricPowerAtCustom += generatorMaximumGeneratorPower * _generatorEfficiencyAtCustom * customScenarioFraction;
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
            if (scenarioElectricPower < _bestScenarioElectricPower) return;

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
                GUILayout.Label((_generatorEfficiencyAtCustom * 100).ToString("0.00") + "%", _radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
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
