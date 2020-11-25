using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class SolarWindProcessor : RefineryActivity, IRefineryActivity
    {
        public SolarWindProcessor()
        {
            ActivityName = "Solar Wind Process";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        private double _fixedConsumptionRate;

        private double _solarWindDensity;

        private double _hydrogenLiquidDensity;
        private double _hydrogenGasDensity;

        private double _deuteriumLiquidDensity;
        private double _deuteriumGasDensity;

        private double _liquidHelium3LiquidDensity;
        private double _liquidHelium3GasDensity;

        private double _liquidHelium4LiquidDensity;
        private double _liquidHelium4GasDensity;

        private double _monoxideLiquidDensity;
        private double _monoxideGasDensity;

        private double _nitrogenLiquidDensity;
        private double _nitrogenGasDensity;

        private double _neonLiquidDensity;
        private double _neonGasDensity;

        private double _solarWindConsumptionRate;
        private double _hydrogenProductionRate;
        private double _deuteriumProductionRate;
        private double _liquidHelium3ProductionRate;
        private double _liquidHelium4ProductionRate;
        private double _monoxideProductionRate;
        private double _nitrogenProductionRate;
        private double _neonProductionRate;

        private string _solarWindResourceName;

        private string _hydrogenLiquidResourceName;
        private string _hydrogenGasResourceName;

        private string _deuteriumLiquidResourceName;
        private string _deuteriumGasResourceName;

        private string _helium3LiquidResourceName;
        private string _helium3GasResourceName;

        private string _helium4LiquidResourceName;
        private string _helium4GasResourceName;

        private string _monoxideLiquidResourceName;
        private string _monoxideGasResourceName;

        private string _nitrogenLiquidResourceName;
        private string _nitrogenGasResourceName;

        private string _neonLiquidResourceName;
        private string _neonGasResourceName;

        public RefineryType RefineryType => RefineryType.Cryogenics;

        public bool HasActivityRequirements ()
        {
           return _part.GetConnectedResources(_solarWindResourceName).Any(rs => rs.maxAmount > 0);
        }


        public string Status => string.Copy(_status);


        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _solarWindResourceName = ResourceSettings.Config.SolarWind;

            _hydrogenLiquidResourceName = ResourceSettings.Config.HydrogenLqd;
            _hydrogenGasResourceName = ResourceSettings.Config.HydrogenGas;
            _deuteriumLiquidResourceName = ResourceSettings.Config.DeuteriumLqd;
            _deuteriumGasResourceName = ResourceSettings.Config.DeuteriumGas;
            _helium3LiquidResourceName = ResourceSettings._HELIUM3_LIQUID;
            _helium3GasResourceName = ResourceSettings._HELIUM3_GAS;
            _helium4LiquidResourceName = ResourceSettings._HELIUM4_LIQUID;
            _helium4GasResourceName = ResourceSettings.Config.Helium4Gas;
            _monoxideLiquidResourceName = ResourceSettings._CARBONMONOXIDE_LIQUID;
            _monoxideGasResourceName = ResourceSettings.Config.CarbonMonoxideGas;
            _nitrogenLiquidResourceName = ResourceSettings._NITROGEN_LIQUID;
            _nitrogenGasResourceName = ResourceSettings._NITROGEN_GAS;
            _neonLiquidResourceName = ResourceSettings._NEON_LIQUID;
            _neonGasResourceName = ResourceSettings._NEON_GAS;

            _solarWindDensity = PartResourceLibrary.Instance.GetDefinition(_solarWindResourceName).density;

            _hydrogenLiquidDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenLiquidResourceName).density;
            _hydrogenGasDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenGasResourceName).density;
            _deuteriumLiquidDensity = PartResourceLibrary.Instance.GetDefinition(_deuteriumLiquidResourceName).density;
            _deuteriumGasDensity = PartResourceLibrary.Instance.GetDefinition(_deuteriumGasResourceName).density;
            _liquidHelium3LiquidDensity = PartResourceLibrary.Instance.GetDefinition(_helium3LiquidResourceName).density;
            _liquidHelium3GasDensity = PartResourceLibrary.Instance.GetDefinition(_helium3GasResourceName).density;
            _liquidHelium4LiquidDensity = PartResourceLibrary.Instance.GetDefinition(_helium4LiquidResourceName).density;
            _liquidHelium4GasDensity = PartResourceLibrary.Instance.GetDefinition(_helium4GasResourceName).density;
            _monoxideLiquidDensity = PartResourceLibrary.Instance.GetDefinition(_monoxideLiquidResourceName).density;
            _monoxideGasDensity = PartResourceLibrary.Instance.GetDefinition(_monoxideGasResourceName).density;
            _nitrogenLiquidDensity = PartResourceLibrary.Instance.GetDefinition(_nitrogenLiquidResourceName).density;
            _nitrogenGasDensity = PartResourceLibrary.Instance.GetDefinition(_nitrogenGasResourceName).density;
            _neonLiquidDensity = PartResourceLibrary.Instance.GetDefinition(_neonLiquidResourceName).density;
            _neonGasDensity = PartResourceLibrary.Instance.GetDefinition(_neonGasResourceName).density;
        }

        private double _maxCapacitySolarWindMass;
        private double _maxCapacityHydrogenMass;
        private double _maxCapacityDeuteriumMass;
        private double _maxCapacityHelium3Mass;
        private double _maxCapacityHelium4Mass;
        private double _maxCapacityMonoxideMass;
        private double _maxCapacityNitrogenMass;
        private double _maxCapacityNeonMass;

        private double _storedSolarWindMass;
        private double _storedHydrogenMass;
        private double _storedDeuteriumMass;
        private double _storedHelium3Mass;
        private double _storedHelium4Mass;
        private double _storedMonoxideMass;
        private double _storedNitrogenMass;
        private double _storedNeonMass;

        private double _spareRoomHydrogenMass;
        private double _spareRoomDeuteriumMass;
        private double _spareRoomHelium3Mass;
        private double _spareRoomHelium4Mass;
        private double _spareRoomMonoxideMass;
        private double _spareRoomNitrogenMass;
        private double _spareRoomNeonMass;

        /* these are the constituents of solar wind with their appropriate mass ratios. According to http://solar-center.stanford.edu/FAQ/Qsolwindcomp.html and other sources,
         * about 90 % of atoms in solar wind are hydrogen. About 8 % is helium (he-3 is less stable than he-4 and apparently the He-3/He-4 ratio is very close to 1/2000), so that's about 7,996 % He-4 and 0.004 % He-3. There are supposedly only trace amounts of heavier elements such as C, O, N and Ne,
         * so I decided on 1 % of atoms for CO and 0.5 % for Nitrogen and Neon. The exact fractions were calculated as (percentage * atomic mass of the element) / Sum of (percentage * atomic mass of the element) for every element.
        */
        private double _hydrogenMassByFraction  = 0.540564; // see above how I got this number (as an example 90 * 1.008 / 167.8245484 = 0.540564), ie. percentage times atomic mass of H / sum of the same stuff in numerator for every element
        private double _deuteriumMassByFraction = 0.00001081128; // because D/H ratio in solarwind is 2 *10e-5 * Hydrogen mass
        private double _helium3MassByFraction   = 0.0000071; // because examples are nice to have (0.004 * 3.016 / 167.8245484 = 0.0000071)
        private double _helium4MassByFraction   = 0.1906752;
        private double _monoxideMassByFraction  = 0.1669004;
        private double _nitrogenMassByFraction  = 0.04173108;
        private double _neonMassByFraction      = 0.06012;

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine how much resource we have
            var partsThatContainSolarWind = _part.GetConnectedResources(_solarWindResourceName).ToList();

            var partsThatContainHydrogenLiquid = _part.GetConnectedResources(_hydrogenLiquidResourceName).ToList();
            var partsThatContainHydrogenGas = _part.GetConnectedResources(_hydrogenGasResourceName).ToList();
            var partsThatContainDeuteriumLiquid = _part.GetConnectedResources(_deuteriumLiquidResourceName).ToList();
            var partsThatContainDeuteriumGas = _part.GetConnectedResources(_deuteriumGasResourceName).ToList();
            var partsThatContainLqdHelium3Liquid = _part.GetConnectedResources(_helium3LiquidResourceName).ToList();
            var partsThatContainLqdHelium3Gas = _part.GetConnectedResources(_helium3GasResourceName).ToList();
            var partsThatContainLqdHelium4Liquid = _part.GetConnectedResources(_helium4LiquidResourceName).ToList();
            var partsThatContainLqdHelium4Gas = _part.GetConnectedResources(_helium4GasResourceName).ToList();
            var partsThatContainMonoxideLiquid = _part.GetConnectedResources(_monoxideLiquidResourceName).ToList();
            var partsThatContainMonoxideGas = _part.GetConnectedResources(_monoxideGasResourceName).ToList();
            var partsThatContainNitrogenLiquid = _part.GetConnectedResources(_nitrogenLiquidResourceName).ToList();
            var partsThatContainNitrogenGas = _part.GetConnectedResources(_nitrogenGasResourceName).ToList();
            var partsThatContainNeonLiquid = _part.GetConnectedResources(_neonLiquidResourceName).ToList();
            var partsThatContainNeonGas = _part.GetConnectedResources(_neonGasResourceName).ToList();

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            _maxCapacitySolarWindMass = partsThatContainSolarWind.Sum(p => p.maxAmount) * _solarWindDensity;

            _maxCapacityHydrogenMass = partsThatContainHydrogenLiquid.Sum(p => p.maxAmount) * _hydrogenLiquidDensity + partsThatContainHydrogenGas.Sum(p => p.maxAmount) * _hydrogenGasDensity;
            _maxCapacityDeuteriumMass = partsThatContainDeuteriumLiquid.Sum(p => p.maxAmount) * _deuteriumLiquidDensity + partsThatContainDeuteriumGas.Sum(p => p.maxAmount) * _deuteriumGasDensity;
            _maxCapacityHelium3Mass = partsThatContainLqdHelium3Liquid.Sum(p => p.maxAmount) * _liquidHelium3LiquidDensity + partsThatContainLqdHelium3Gas.Sum(p => p.maxAmount) * _liquidHelium3GasDensity;
            _maxCapacityHelium4Mass = partsThatContainLqdHelium4Liquid.Sum(p => p.maxAmount) * _liquidHelium4LiquidDensity + partsThatContainLqdHelium4Gas.Sum(p => p.maxAmount) * _liquidHelium4GasDensity;
            _maxCapacityMonoxideMass = partsThatContainMonoxideLiquid.Sum(p => p.maxAmount) * _monoxideLiquidDensity + partsThatContainMonoxideGas.Sum(p => p.maxAmount) * _monoxideGasDensity;
            _maxCapacityNitrogenMass = partsThatContainNitrogenLiquid.Sum(p => p.maxAmount) * _nitrogenLiquidDensity + partsThatContainNitrogenGas.Sum(p => p.maxAmount) * _nitrogenGasDensity;
            _maxCapacityNeonMass = partsThatContainNeonLiquid.Sum(p => p.maxAmount) * _neonLiquidDensity + partsThatContainNeonGas.Sum(p => p.maxAmount) * _neonGasDensity;

            // determine the amount of resources needed for processing (i.e. solar wind) that the vessel actually holds
            _storedSolarWindMass = partsThatContainSolarWind.Sum(r => r.amount) * _solarWindDensity;

            _storedHydrogenMass = partsThatContainHydrogenLiquid.Sum(r => r.amount) * _hydrogenLiquidDensity + partsThatContainHydrogenGas.Sum(p => p.amount) * _hydrogenGasDensity;
            _storedDeuteriumMass = partsThatContainDeuteriumLiquid.Sum(r => r.amount) * _deuteriumLiquidDensity + partsThatContainDeuteriumGas.Sum(r => r.amount) * _deuteriumGasDensity;
            _storedHelium3Mass = partsThatContainLqdHelium3Liquid.Sum(r => r.amount) * _liquidHelium3LiquidDensity + partsThatContainLqdHelium3Gas.Sum(r => r.amount) * _liquidHelium3GasDensity;
            _storedHelium4Mass = partsThatContainLqdHelium4Liquid.Sum(r => r.amount) * _liquidHelium4LiquidDensity + partsThatContainLqdHelium4Gas.Sum(r => r.amount) * _liquidHelium4GasDensity;
            _storedMonoxideMass = partsThatContainMonoxideLiquid.Sum(r => r.amount) * _monoxideLiquidDensity + partsThatContainMonoxideGas.Sum(r => r.amount) * _monoxideGasDensity;
            _storedNitrogenMass = partsThatContainNitrogenLiquid.Sum(r => r.amount) * _nitrogenLiquidDensity + partsThatContainNitrogenGas.Sum(r => r.amount) * _nitrogenGasDensity;
            _storedNeonMass = partsThatContainNeonLiquid.Sum(r => r.amount) * _neonLiquidDensity + partsThatContainNeonGas.Sum(r => r.amount) * _neonGasDensity;

            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            _spareRoomHydrogenMass = _maxCapacityHydrogenMass - _storedHydrogenMass;
            _spareRoomDeuteriumMass = _maxCapacityDeuteriumMass - _storedDeuteriumMass;
            _spareRoomHelium3Mass = _maxCapacityHelium3Mass - _storedHelium3Mass;
            _spareRoomHelium4Mass = _maxCapacityHelium4Mass - _storedHelium4Mass;
            _spareRoomMonoxideMass = _maxCapacityMonoxideMass - _storedMonoxideMass;
            _spareRoomNitrogenMass = _maxCapacityNitrogenMass - _storedNitrogenMass;
            _spareRoomNeonMass = _maxCapacityNeonMass - _storedNeonMass;

            // this should determine how much resource this process can consume
            double fixedMaxSolarWindConsumptionRate = _current_rate * fixedDeltaTime * _solarWindDensity;
            double solarWindConsumptionRatio = fixedMaxSolarWindConsumptionRate > 0
                ? Math.Min(fixedMaxSolarWindConsumptionRate, _storedSolarWindMass) / fixedMaxSolarWindConsumptionRate
                : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * solarWindConsumptionRatio;

            // begin the solar wind processing
            if (_fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomDeuteriumMass > 0 || _spareRoomHelium3Mass > 0 || _spareRoomHelium4Mass > 0 || _spareRoomMonoxideMass > 0 || _spareRoomNitrogenMass > 0)) // check if there is anything to consume and spare room for at least one of the products
            {
                var fixedMaxHydrogenRate = _fixedConsumptionRate * _hydrogenMassByFraction;
                var fixedMaxDeuteriumRate = _fixedConsumptionRate * _deuteriumMassByFraction;
                var fixedMaxHelium3Rate = _fixedConsumptionRate * _helium3MassByFraction;
                var fixedMaxHelium4Rate = _fixedConsumptionRate * _helium4MassByFraction;
                var fixedMaxMonoxideRate = _fixedConsumptionRate * _monoxideMassByFraction;
                var fixedMaxNitrogenRate = _fixedConsumptionRate * _nitrogenMassByFraction;
                var fixedMaxNeonRate = _fixedConsumptionRate * _neonMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleDeuteriumRate = allowOverflow ? fixedMaxDeuteriumRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxDeuteriumRate);
                var fixedMaxPossibleHelium3Rate = allowOverflow ? fixedMaxHelium3Rate : Math.Min(_spareRoomHelium3Mass, fixedMaxHelium3Rate);
                var fixedMaxPossibleHelium4Rate = allowOverflow ? fixedMaxHelium4Rate : Math.Min(_spareRoomHelium4Mass, fixedMaxHelium4Rate);
                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleNitrogenRate = allowOverflow ? fixedMaxNitrogenRate : Math.Min(_spareRoomNitrogenMass, fixedMaxNitrogenRate);
                var fixedMaxPossibleNeonRate = allowOverflow ? fixedMaxNeonRate : Math.Min(_spareRoomNeonMass, fixedMaxNeonRate);

                // finds the minimum of these five numbers (fixedMaxPossibleZZRate / fixedMaxZZRate), adapted from water electrolyser. Could be more pretty with a custom Min5() function, but eh.
                var consumptionStorageRatios = new[]
                {
                    fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate,
                    fixedMaxPossibleDeuteriumRate / fixedMaxDeuteriumRate,
                    fixedMaxPossibleHelium3Rate / fixedMaxHelium3Rate,
                    fixedMaxPossibleHelium4Rate / fixedMaxHelium4Rate,
                    fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate,
                    fixedMaxPossibleNitrogenRate / fixedMaxNitrogenRate,
                    fixedMaxPossibleNeonRate / fixedMaxNeonRate
                };

                double minConsumptionStorageRatio = consumptionStorageRatios.Min();

                // this consumes the resource
                _solarWindConsumptionRate = _part.RequestResource(_solarWindResourceName, minConsumptionStorageRatio * _fixedConsumptionRate / _solarWindDensity) / fixedDeltaTime * _solarWindDensity;

                // this produces the products
                var hydrogenRateTemp = _solarWindConsumptionRate * _hydrogenMassByFraction;
                var deuteriumRateTemp = _solarWindConsumptionRate * _deuteriumMassByFraction;
                var helium3RateTemp = _solarWindConsumptionRate * _helium3MassByFraction;
                var helium4RateTemp = _solarWindConsumptionRate * _helium4MassByFraction;
                var monoxideRateTemp = _solarWindConsumptionRate * _monoxideMassByFraction;
                var nitrogenRateTemp = _solarWindConsumptionRate * _nitrogenMassByFraction;
                var neonRateTemp = _solarWindConsumptionRate * _neonMassByFraction;

                _hydrogenProductionRate = -_part.RequestResource(_hydrogenGasResourceName, -hydrogenRateTemp * fixedDeltaTime / _hydrogenGasDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogenGasDensity;
                _hydrogenProductionRate += -_part.RequestResource(_hydrogenLiquidResourceName, -(hydrogenRateTemp - _hydrogenProductionRate) * fixedDeltaTime / _hydrogenLiquidDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogenLiquidDensity;

                _deuteriumProductionRate = -_part.RequestResource(_deuteriumGasResourceName, -deuteriumRateTemp * fixedDeltaTime / _deuteriumGasDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuteriumGasDensity;
                _deuteriumProductionRate += -_part.RequestResource(_deuteriumLiquidResourceName, -(deuteriumRateTemp - _deuteriumProductionRate) * fixedDeltaTime / _deuteriumLiquidDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuteriumLiquidDensity;

                _liquidHelium3ProductionRate = -_part.RequestResource(_helium3GasResourceName, -helium3RateTemp * fixedDeltaTime / _liquidHelium3GasDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquidHelium3GasDensity;
                _liquidHelium3ProductionRate += -_part.RequestResource(_helium3LiquidResourceName, -(helium3RateTemp - _liquidHelium3ProductionRate) * fixedDeltaTime / _liquidHelium3LiquidDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquidHelium3LiquidDensity;

                _liquidHelium4ProductionRate = -_part.RequestResource(_helium4GasResourceName, -helium4RateTemp * fixedDeltaTime / _liquidHelium4GasDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquidHelium4GasDensity;
                _liquidHelium4ProductionRate += -_part.RequestResource(_helium4LiquidResourceName, -(helium4RateTemp - _liquidHelium4ProductionRate) * fixedDeltaTime / _liquidHelium4LiquidDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquidHelium4LiquidDensity;

                _monoxideProductionRate = -_part.RequestResource(_monoxideGasResourceName, -monoxideRateTemp * fixedDeltaTime / _monoxideGasDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxideGasDensity;
                _monoxideProductionRate += -_part.RequestResource(_monoxideLiquidResourceName, -(monoxideRateTemp - _monoxideProductionRate) * fixedDeltaTime / _monoxideLiquidDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxideLiquidDensity;

                _nitrogenProductionRate = -_part.RequestResource(_nitrogenGasResourceName, -nitrogenRateTemp * fixedDeltaTime / _nitrogenGasDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogenGasDensity;
                _nitrogenProductionRate += -_part.RequestResource(_nitrogenLiquidResourceName, -(nitrogenRateTemp - _nitrogenProductionRate) * fixedDeltaTime / _nitrogenLiquidDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogenLiquidDensity;

                _neonProductionRate = -_part.RequestResource(_neonGasResourceName, -neonRateTemp * fixedDeltaTime / _neonGasDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neonGasDensity;
                _neonProductionRate += -_part.RequestResource(_neonLiquidResourceName, -(neonRateTemp - _neonProductionRate) * fixedDeltaTime / _neonLiquidDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neonLiquidDensity;
            }
            else
            {
                _solarWindConsumptionRate = 0;
                _hydrogenProductionRate = 0;
                _deuteriumProductionRate = 0;
                _liquidHelium3ProductionRate = 0;
                _liquidHelium4ProductionRate = 0;
                _monoxideProductionRate = 0;
                _nitrogenProductionRate = 0;
                _neonProductionRate = 0;
            }
            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SolarWindAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Solar Wind Available"
            GUILayout.Label((_storedSolarWindMass * 1e6).ToString("0.000") + " "+Localizer.Format("#LOC_KSPIE_SolarWindProcessor_gram"), _value_label, GUILayout.Width(valueWidth));//gram
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SolarWindMaxCapacity"), _bold_label, GUILayout.Width(labelWidth));//"Solar Wind Max Capacity"
            GUILayout.Label((_maxCapacitySolarWindMass * 1e6).ToString("0.000") + " gram", _value_label, GUILayout.Width(valueWidth));//
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SolarWindConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Solar Wind Consumption"
            GUILayout.Label((((float)_solarWindConsumptionRate * GameConstants.SECONDS_IN_HOUR * 1e6).ToString("0.0000")) + " g/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Output"), _bold_label, GUILayout.Width(150));//"Output"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Percentage"), _bold_label, GUILayout.Width(100));//"Percentage"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Capacity"), _bold_label, GUILayout.Width(100));//"Capacity"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Stored"), _bold_label, GUILayout.Width(100));//"Stored"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SpareRoom"), _bold_label, GUILayout.Width(100));//"Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Production"), _bold_label, GUILayout.Width(100));//"Production"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_hydrogenGasResourceName + " / " + _hydrogenLiquidResourceName, _value_label, GUILayout.Width(150));
            GUILayout.Label((_hydrogenMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_hydrogenProductionRate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_helium4GasResourceName + " / " + _helium4LiquidResourceName, _value_label, GUILayout.Width(150));
            GUILayout.Label((_helium4MassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_liquidHelium4ProductionRate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_monoxideGasResourceName + " / " + _monoxideLiquidResourceName, _value_label, GUILayout.Width(150));
            GUILayout.Label((_monoxideMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_monoxideProductionRate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_nitrogenGasResourceName + " / " + _nitrogenLiquidResourceName, _value_label, GUILayout.Width(150));
            GUILayout.Label((_nitrogenMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_nitrogenProductionRate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_neonGasResourceName + " / " + _neonLiquidResourceName, _value_label, GUILayout.Width(150));
            GUILayout.Label((_neonMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_neonProductionRate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_deuteriumGasResourceName + " / " + _deuteriumLiquidResourceName, _value_label, GUILayout.Width(150));
            GUILayout.Label((_deuteriumMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityDeuteriumMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedDeuteriumMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomDeuteriumMass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_deuteriumProductionRate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_helium3GasResourceName + " / " + _helium3LiquidResourceName, _value_label, GUILayout.Width(150));
            GUILayout.Label((_helium3MassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_liquidHelium3ProductionRate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_solarWindConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg1");//"Processing of Solar Wind Particles Ongoing"
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg2");//"Insufficient Power"
            else if (_storedSolarWindMass <= float.Epsilon)
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg3");//"No Solar Wind Particles Available"
            else
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg4");//"Insufficient Storage, try allowing overflow"
        }

        private string MetricTon(double value, string format = "")
        {
            return value == 0 ? "-" : value < 1 ? value < 0.001 ? (value * 1e6).ToString(format) + " g" : (value * 1e3).ToString(format) + " kg" : value.ToString(format) + " mT";
        }

        private string GramPerHour(double value, string format = "")
        {
            return value == 0 ? "-" : value < 1 ? (value * 1000).ToString(format) + " mg/hour" : value.ToString(format) + " g/hour";
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Postmsg") +" " + ResourceSettings.Config.SolarWind, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
