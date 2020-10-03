using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class SolarWindProcessor : RefineryActivity, IRefineryActivity
    {
        public SolarWindProcessor()
        {
            ActivityName = "Solar Wind Process";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
        }

        double _fixedConsumptionRate;

        double _solar_wind_density;

        double _hydrogen_liquid_density;
        double _hydrogen_gas_density;

        double _deuterium_liquid_density;
        double _deuterium_gas_density;

        double _liquid_helium3_liquid_density;
        double _liquid_helium3_gas_density;

        double _liquid_helium4_liquid_density;
        double _liquid_helium4_gas_density;

        double _monoxide_liquid_density;
        double _monoxide_gas_density;

        double _nitrogen_liquid_density;
        double _nitrogen_gas_density;

        double _neon_liquid_density;
        double _neon_gas_density;

        double _solar_wind_consumption_rate;
        double _hydrogen_production_rate;
        double _deuterium_production_rate;
        double _liquid_helium3_production_rate;
        double _liquid_helium4_production_rate;
        double _monoxide_production_rate;
        double _nitrogen_production_rate;
        double _neon_production_rate;

        string _solar_wind_resource_name;

        string _hydrogen_liquid_resource_name;
        string _hydrogen_gas_resource_name;

        string _deuterium_liquid_resource_name;
        string _deuterium_gas_resource_name;

        string _helium3_liquid_resource_name;
        string _helium3_gas_resource_name;

        string _helium4_liquid_resource_name;
        string _helium4_gas_resource_name;

        string _monoxide_liquid_resource_name;
        string _monoxide_gas_resource_name;

        string _nitrogen_liquid_resource_name;
        string _nitrogen_gas_resource_name;

        string _neon_liquid_resource_name;
        string _neon_gas_resource_name;

        public RefineryType RefineryType => RefineryType.Cryogenics;

        public bool HasActivityRequirements ()
        {
           return _part.GetConnectedResources(_solar_wind_resource_name).Any(rs => rs.maxAmount > 0);
        }


        public string Status => string.Copy(_status);


        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _solar_wind_resource_name = InterstellarResourcesConfiguration.Instance.SolarWind;

            _hydrogen_liquid_resource_name = InterstellarResourcesConfiguration._HYDROGEN_LIQUID;
            _hydrogen_gas_resource_name = InterstellarResourcesConfiguration._HYDROGEN_GAS;
            _deuterium_liquid_resource_name = InterstellarResourcesConfiguration._DEUTERIUM_LIQUID;
            _deuterium_gas_resource_name = InterstellarResourcesConfiguration._DEUTERIUM_GAS;
            _helium3_liquid_resource_name = InterstellarResourcesConfiguration._HELIUM3_LIQUID;
            _helium3_gas_resource_name = InterstellarResourcesConfiguration._HELIUM3_GAS;
            _helium4_liquid_resource_name = InterstellarResourcesConfiguration._HELIUM4_LIQUID;
            _helium4_gas_resource_name = InterstellarResourcesConfiguration._HELIUM4_GAS;
            _monoxide_liquid_resource_name = InterstellarResourcesConfiguration._CARBONMONOXIDE_LIQUID;
            _monoxide_gas_resource_name = InterstellarResourcesConfiguration._CARBONMONOXIDE_GAS;
            _nitrogen_liquid_resource_name = InterstellarResourcesConfiguration._NITROGEN_LIQUID;
            _nitrogen_gas_resource_name = InterstellarResourcesConfiguration._NITROGEN_GAS;
            _neon_liquid_resource_name = InterstellarResourcesConfiguration._NEON_LIQUID;
            _neon_gas_resource_name = InterstellarResourcesConfiguration._NEON_GAS;

            _solar_wind_density = PartResourceLibrary.Instance.GetDefinition(_solar_wind_resource_name).density;

            _hydrogen_liquid_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_liquid_resource_name).density;
            _hydrogen_gas_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_gas_resource_name).density;
            _deuterium_liquid_density = PartResourceLibrary.Instance.GetDefinition(_deuterium_liquid_resource_name).density;
            _deuterium_gas_density = PartResourceLibrary.Instance.GetDefinition(_deuterium_gas_resource_name).density;
            _liquid_helium3_liquid_density = PartResourceLibrary.Instance.GetDefinition(_helium3_liquid_resource_name).density;
            _liquid_helium3_gas_density = PartResourceLibrary.Instance.GetDefinition(_helium3_gas_resource_name).density;
            _liquid_helium4_liquid_density = PartResourceLibrary.Instance.GetDefinition(_helium4_liquid_resource_name).density;
            _liquid_helium4_gas_density = PartResourceLibrary.Instance.GetDefinition(_helium4_gas_resource_name).density;
            _monoxide_liquid_density = PartResourceLibrary.Instance.GetDefinition(_monoxide_liquid_resource_name).density;
            _monoxide_gas_density = PartResourceLibrary.Instance.GetDefinition(_monoxide_gas_resource_name).density;
            _nitrogen_liquid_density = PartResourceLibrary.Instance.GetDefinition(_nitrogen_liquid_resource_name).density;
            _nitrogen_gas_density = PartResourceLibrary.Instance.GetDefinition(_nitrogen_gas_resource_name).density;
            _neon_liquid_density = PartResourceLibrary.Instance.GetDefinition(_neon_liquid_resource_name).density;
            _neon_gas_density = PartResourceLibrary.Instance.GetDefinition(_neon_gas_resource_name).density;
        }

        protected double _maxCapacitySolarWindMass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityDeuteriumMass;
        protected double _maxCapacityHelium3Mass;
        protected double _maxCapacityHelium4Mass;
        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityNitrogenMass;
        protected double _maxCapacityNeonMass;

        protected double _storedSolarWindMass;
        protected double _storedHydrogenMass;
        protected double _storedDeuteriumMass;
        protected double _storedHelium3Mass;
        protected double _storedHelium4Mass;
        protected double _storedMonoxideMass;
        protected double _storedNitrogenMass;
        protected double _storedNeonMass;

        protected double _spareRoomHydrogenMass;
        protected double _spareRoomDeuteriumMass;
        protected double _spareRoomHelium3Mass;
        protected double _spareRoomHelium4Mass;
        protected double _spareRoomMonoxideMass;
        protected double _spareRoomNitrogenMass;
        protected double _spareRoomNeonMass;

        /* these are the constituents of solar wind with their appropriate mass ratios. According to http://solar-center.stanford.edu/FAQ/Qsolwindcomp.html and other sources,
         * about 90 % of atoms in solar wind are hydrogen. About 8 % is helium (he-3 is less stable than he-4 and apparently the He-3/He-4 ratio is very close to 1/2000), so that's about 7,996 % He-4 and 0.004 % He-3. There are supposedly only trace amounts of heavier elements such as C, O, N and Ne,
         * so I decided on 1 % of atoms for CO and 0.5 % for Nitrogen and Neon. The exact fractions were calculated as (percentage * atomic mass of the element) / Sum of (percentage * atomic mass of the element) for every element.
        */
        protected double _hydrogenMassByFraction  = 0.540564; // see above how I got this number (as an example 90 * 1.008 / 167.8245484 = 0.540564), ie. percentage times atomic mass of H / sum of the same stuff in numerator for every element
        protected double _deuteriumMassByFraction = 0.00001081128; // because D/H ratio in solarwind is 2 *10e-5 * Hydrogen mass
        protected double _helium3MassByFraction   = 0.0000071; // because examples are nice to have (0.004 * 3.016 / 167.8245484 = 0.0000071)
        protected double _helium4MassByFraction   = 0.1906752;
        protected double _monoxideMassByFraction  = 0.1669004;
        protected double _nitrogenMassByFraction  = 0.04173108;
        protected double _neonMassByFraction      = 0.06012;

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            // determine how much resource we have
            var partsThatContainSolarWind = _part.GetConnectedResources(_solar_wind_resource_name);

            var partsThatContainHydrogenLiquid = _part.GetConnectedResources(_hydrogen_liquid_resource_name);
            var partsThatContainHydrogenGas = _part.GetConnectedResources(_hydrogen_gas_resource_name);
            var partsThatContainDeuteriumLiquid = _part.GetConnectedResources(_deuterium_liquid_resource_name);
            var partsThatContainDeuteriumGas = _part.GetConnectedResources(_deuterium_gas_resource_name);
            var partsThatContainLqdHelium3Liquid = _part.GetConnectedResources(_helium3_liquid_resource_name);
            var partsThatContainLqdHelium3Gas = _part.GetConnectedResources(_helium3_gas_resource_name);
            var partsThatContainLqdHelium4Liquid = _part.GetConnectedResources(_helium4_liquid_resource_name);
            var partsThatContainLqdHelium4Gas = _part.GetConnectedResources(_helium4_gas_resource_name);
            var partsThatContainMonoxideLiquid = _part.GetConnectedResources(_monoxide_liquid_resource_name);
            var partsThatContainMonoxideGas = _part.GetConnectedResources(_monoxide_gas_resource_name);
            var partsThatContainNitrogenLiquid = _part.GetConnectedResources(_nitrogen_liquid_resource_name);
            var partsThatContainNitrogenGas = _part.GetConnectedResources(_nitrogen_gas_resource_name);
            var partsThatContainNeonLiquid = _part.GetConnectedResources(_neon_liquid_resource_name);
            var partsThatContainNeonGas = _part.GetConnectedResources(_neon_gas_resource_name);

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            _maxCapacitySolarWindMass = partsThatContainSolarWind.Sum(p => p.maxAmount) * _solar_wind_density;

            _maxCapacityHydrogenMass = partsThatContainHydrogenLiquid.Sum(p => p.maxAmount) * _hydrogen_liquid_density + partsThatContainHydrogenGas.Sum(p => p.maxAmount) * _hydrogen_gas_density;
            _maxCapacityDeuteriumMass = partsThatContainDeuteriumLiquid.Sum(p => p.maxAmount) * _deuterium_liquid_density + partsThatContainDeuteriumGas.Sum(p => p.maxAmount) * _deuterium_gas_density;
            _maxCapacityHelium3Mass = partsThatContainLqdHelium3Liquid.Sum(p => p.maxAmount) * _liquid_helium3_liquid_density + partsThatContainLqdHelium3Gas.Sum(p => p.maxAmount) * _liquid_helium3_gas_density;
            _maxCapacityHelium4Mass = partsThatContainLqdHelium4Liquid.Sum(p => p.maxAmount) * _liquid_helium4_liquid_density + partsThatContainLqdHelium4Gas.Sum(p => p.maxAmount) * _liquid_helium4_gas_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxideLiquid.Sum(p => p.maxAmount) * _monoxide_liquid_density + partsThatContainMonoxideGas.Sum(p => p.maxAmount) * _monoxide_gas_density;
            _maxCapacityNitrogenMass = partsThatContainNitrogenLiquid.Sum(p => p.maxAmount) * _nitrogen_liquid_density + partsThatContainNitrogenGas.Sum(p => p.maxAmount) * _nitrogen_gas_density;
            _maxCapacityNeonMass = partsThatContainNeonLiquid.Sum(p => p.maxAmount) * _neon_liquid_density + partsThatContainNeonGas.Sum(p => p.maxAmount) * _neon_gas_density;

            // determine the amount of resources needed for processing (i.e. solar wind) that the vessel actually holds
            _storedSolarWindMass = partsThatContainSolarWind.Sum(r => r.amount) * _solar_wind_density;

            _storedHydrogenMass = partsThatContainHydrogenLiquid.Sum(r => r.amount) * _hydrogen_liquid_density + partsThatContainHydrogenGas.Sum(p => p.amount) * _hydrogen_gas_density;
            _storedDeuteriumMass = partsThatContainDeuteriumLiquid.Sum(r => r.amount) * _deuterium_liquid_density + partsThatContainDeuteriumGas.Sum(r => r.amount) * _deuterium_gas_density;
            _storedHelium3Mass = partsThatContainLqdHelium3Liquid.Sum(r => r.amount) * _liquid_helium3_liquid_density + partsThatContainLqdHelium3Gas.Sum(r => r.amount) * _liquid_helium3_gas_density;
            _storedHelium4Mass = partsThatContainLqdHelium4Liquid.Sum(r => r.amount) * _liquid_helium4_liquid_density + partsThatContainLqdHelium4Gas.Sum(r => r.amount) * _liquid_helium4_gas_density;
            _storedMonoxideMass = partsThatContainMonoxideLiquid.Sum(r => r.amount) * _monoxide_liquid_density + partsThatContainMonoxideGas.Sum(r => r.amount) * _monoxide_gas_density;
            _storedNitrogenMass = partsThatContainNitrogenLiquid.Sum(r => r.amount) * _nitrogen_liquid_density + partsThatContainNitrogenGas.Sum(r => r.amount) * _nitrogen_gas_density;
            _storedNeonMass = partsThatContainNeonLiquid.Sum(r => r.amount) * _neon_liquid_density + partsThatContainNeonGas.Sum(r => r.amount) * _neon_gas_density;

            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            _spareRoomHydrogenMass = _maxCapacityHydrogenMass - _storedHydrogenMass;
            _spareRoomDeuteriumMass = _maxCapacityDeuteriumMass - _storedDeuteriumMass;
            _spareRoomHelium3Mass = _maxCapacityHelium3Mass - _storedHelium3Mass;
            _spareRoomHelium4Mass = _maxCapacityHelium4Mass - _storedHelium4Mass;
            _spareRoomMonoxideMass = _maxCapacityMonoxideMass - _storedMonoxideMass;
            _spareRoomNitrogenMass = _maxCapacityNitrogenMass - _storedNitrogenMass;
            _spareRoomNeonMass = _maxCapacityNeonMass - _storedNeonMass;

            // this should determine how much resource this process can consume
            double fixedMaxSolarWindConsumptionRate = _current_rate * fixedDeltaTime * _solar_wind_density;
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
                var _consumptionStorageRatios = new[] 
                { 
                    fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate, 
                    fixedMaxPossibleDeuteriumRate / fixedMaxDeuteriumRate,  
                    fixedMaxPossibleHelium3Rate / fixedMaxHelium3Rate, 
                    fixedMaxPossibleHelium4Rate / fixedMaxHelium4Rate, 
                    fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate, 
                    fixedMaxPossibleNitrogenRate / fixedMaxNitrogenRate, 
                    fixedMaxPossibleNeonRate / fixedMaxNeonRate 
                };

                double minConsumptionStorageRatio = _consumptionStorageRatios.Min();
                               
                // this consumes the resource
                _solar_wind_consumption_rate = _part.RequestResource(_solar_wind_resource_name, minConsumptionStorageRatio * _fixedConsumptionRate / _solar_wind_density) / fixedDeltaTime * _solar_wind_density;

                // this produces the products
                var hydrogen_rate_temp = _solar_wind_consumption_rate * _hydrogenMassByFraction;
                var deuterium_rate_temp = _solar_wind_consumption_rate * _deuteriumMassByFraction;
                var helium3_rate_temp = _solar_wind_consumption_rate * _helium3MassByFraction;
                var helium4_rate_temp = _solar_wind_consumption_rate * _helium4MassByFraction;
                var monoxide_rate_temp = _solar_wind_consumption_rate * _monoxideMassByFraction;
                var nitrogen_rate_temp = _solar_wind_consumption_rate * _nitrogenMassByFraction;
                var neon_rate_temp = _solar_wind_consumption_rate * _neonMassByFraction;
                
                _hydrogen_production_rate = -_part.RequestResource(_hydrogen_gas_resource_name, -hydrogen_rate_temp * fixedDeltaTime / _hydrogen_gas_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen_gas_density;
                _hydrogen_production_rate += -_part.RequestResource(_hydrogen_liquid_resource_name, -(hydrogen_rate_temp - _hydrogen_production_rate) * fixedDeltaTime / _hydrogen_liquid_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen_liquid_density;

                _deuterium_production_rate = -_part.RequestResource(_deuterium_gas_resource_name, -deuterium_rate_temp * fixedDeltaTime / _deuterium_gas_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium_gas_density;
                _deuterium_production_rate += -_part.RequestResource(_deuterium_liquid_resource_name, -(deuterium_rate_temp - _deuterium_production_rate) * fixedDeltaTime / _deuterium_liquid_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium_liquid_density;

                _liquid_helium3_production_rate = -_part.RequestResource(_helium3_gas_resource_name, -helium3_rate_temp * fixedDeltaTime / _liquid_helium3_gas_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquid_helium3_gas_density;
                _liquid_helium3_production_rate += -_part.RequestResource(_helium3_liquid_resource_name, -(helium3_rate_temp - _liquid_helium3_production_rate) * fixedDeltaTime / _liquid_helium3_liquid_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquid_helium3_liquid_density;

                _liquid_helium4_production_rate = -_part.RequestResource(_helium4_gas_resource_name, -helium4_rate_temp * fixedDeltaTime / _liquid_helium4_gas_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquid_helium4_gas_density;
                _liquid_helium4_production_rate += -_part.RequestResource(_helium4_liquid_resource_name, -(helium4_rate_temp - _liquid_helium4_production_rate) * fixedDeltaTime / _liquid_helium4_liquid_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquid_helium4_liquid_density;

                _monoxide_production_rate = -_part.RequestResource(_monoxide_gas_resource_name, -monoxide_rate_temp * fixedDeltaTime / _monoxide_gas_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_gas_density;
                _monoxide_production_rate += -_part.RequestResource(_monoxide_liquid_resource_name, -(monoxide_rate_temp - _monoxide_production_rate) * fixedDeltaTime / _monoxide_liquid_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_liquid_density;

                _nitrogen_production_rate = -_part.RequestResource(_nitrogen_gas_resource_name, -nitrogen_rate_temp * fixedDeltaTime / _nitrogen_gas_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen_gas_density;
                _nitrogen_production_rate += -_part.RequestResource(_nitrogen_liquid_resource_name, -(nitrogen_rate_temp - _nitrogen_production_rate) * fixedDeltaTime / _nitrogen_liquid_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen_liquid_density;

                _neon_production_rate = -_part.RequestResource(_neon_gas_resource_name, -neon_rate_temp * fixedDeltaTime / _neon_gas_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neon_gas_density;
                _neon_production_rate += -_part.RequestResource(_neon_liquid_resource_name, -(neon_rate_temp - _neon_production_rate) * fixedDeltaTime / _neon_liquid_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neon_liquid_density;
            }
            else
            {
                _solar_wind_consumption_rate = 0;
                _hydrogen_production_rate = 0;
                _deuterium_production_rate = 0;
                _liquid_helium3_production_rate = 0;
                _liquid_helium4_production_rate = 0;
                _monoxide_production_rate = 0;
                _nitrogen_production_rate = 0;
                _neon_production_rate = 0;
            }
            updateStatusMessage();
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
            GUILayout.Label((((float)_solar_wind_consumption_rate * GameConstants.SECONDS_IN_HOUR * 1e6).ToString("0.0000")) + " g/hour", _value_label, GUILayout.Width(valueWidth));
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
            GUILayout.Label(_hydrogen_gas_resource_name + " / " + _hydrogen_liquid_resource_name, _value_label, GUILayout.Width(150));
            GUILayout.Label((_hydrogenMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_hydrogen_production_rate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_helium4_gas_resource_name + " / " + _helium4_liquid_resource_name, _value_label, GUILayout.Width(150));
            GUILayout.Label((_helium4MassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_liquid_helium4_production_rate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_monoxide_gas_resource_name + " / " + _monoxide_liquid_resource_name, _value_label, GUILayout.Width(150));
            GUILayout.Label((_monoxideMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_monoxide_production_rate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_nitrogen_gas_resource_name + " / " + _nitrogen_liquid_resource_name, _value_label, GUILayout.Width(150));
            GUILayout.Label((_nitrogenMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_nitrogen_production_rate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_neon_gas_resource_name + " / " + _neon_liquid_resource_name, _value_label, GUILayout.Width(150));
            GUILayout.Label((_neonMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_neon_production_rate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_deuterium_gas_resource_name + " / " + _deuterium_liquid_resource_name, _value_label, GUILayout.Width(150));
            GUILayout.Label((_deuteriumMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityDeuteriumMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedDeuteriumMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomDeuteriumMass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_deuterium_production_rate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_helium3_gas_resource_name + " / " + _helium3_liquid_resource_name, _value_label, GUILayout.Width(150));
            GUILayout.Label((_helium3MassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_liquid_helium3_production_rate * GameConstants.SECONDS_IN_HOUR * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_solar_wind_consumption_rate > 0)
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
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Postmsg") +" " + InterstellarResourcesConfiguration.Instance.SolarWind, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
