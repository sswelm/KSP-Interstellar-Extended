using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class SolarWindProcessor : RefineryActivityBase, IRefineryActivity
    {
        protected double _fixedConsumptionRate;

        protected double _solar_wind_density;
        protected double _hydrogen_density;
        protected double _deuterium_density;
        protected double _liquid_helium3_density;
        protected double _liquid_helium4_density;
        protected double _monoxide_density;
        protected double _nitrogen_density;
        protected double _neon_density;

        protected double _solar_wind_consumption_rate;

        protected double _hydrogen_production_rate;
        protected double _deuterium_production_rate;
        protected double _liquid_helium3_production_rate;
        protected double _liquid_helium4_production_rate;
        protected double _monoxide_production_rate;
        protected double _nitrogen_production_rate;
        protected double _neon_production_rate;

        public RefineryType RefineryType { get { return RefineryType.cryogenics; } }

        public String ActivityName { get { return "Solar Wind Process"; } }

        public bool HasActivityRequirements {
            get
            {
                return _part.GetConnectedResources(_solar_wind_resource_name).Any(rs => rs.amount > 0);
            }
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        protected string _solar_wind_resource_name;
        protected string _hydrogen_resource_name;
        protected string _deuterium_resource_name;
        protected string _liquid_helium3_resource_name;
        protected string _liquid_helium4_resource_name;
        protected string _monoxide_resource_name;
        protected string _nitrogen_resource_name;
        protected string _neon_resource_name;

        public SolarWindProcessor(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _solar_wind_resource_name = InterstellarResourcesConfiguration.Instance.SolarWind;
            _hydrogen_resource_name = InterstellarResourcesConfiguration._LIQUID_HYDROGEN;
            _deuterium_resource_name = InterstellarResourcesConfiguration._LIQUID_DEUTERIUM;
            _liquid_helium3_resource_name = InterstellarResourcesConfiguration._LIQUID_HELIUM_3;
            _liquid_helium4_resource_name = InterstellarResourcesConfiguration._LIQUID_HELIUM_4;
            _monoxide_resource_name = InterstellarResourcesConfiguration._LIQUID_CO;
            _nitrogen_resource_name = InterstellarResourcesConfiguration._LIQUID_NITROGEN;
            _neon_resource_name = InterstellarResourcesConfiguration._LIQUID_NEON;

            _solar_wind_density = PartResourceLibrary.Instance.GetDefinition(_solar_wind_resource_name).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_resource_name).density;
            _deuterium_density = PartResourceLibrary.Instance.GetDefinition(_deuterium_resource_name).density;
            _liquid_helium3_density = PartResourceLibrary.Instance.GetDefinition(_liquid_helium3_resource_name).density;
            _liquid_helium4_density = PartResourceLibrary.Instance.GetDefinition(_liquid_helium4_resource_name).density;
            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxide_resource_name).density;
            _nitrogen_density = PartResourceLibrary.Instance.GetDefinition(_nitrogen_resource_name).density;
            _neon_density = PartResourceLibrary.Instance.GetDefinition(_neon_resource_name).density;
        }

        protected double _maxCapacitySolarWindMass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityDeuteriumMass;
        protected double _maxCapacityHelium3Mass;
        protected double _maxCapacityHelium4Mass;
        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityNitrogenMass;
        protected double _maxCapacityNeonMass;

        protected double _availableSolarWindMass;
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

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            // determine how much resource we have
            var partsThatContainSolarWind = _part.GetConnectedResources(_solar_wind_resource_name);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogen_resource_name);
            var partsThatContainDeuterium = _part.GetConnectedResources(_deuterium_resource_name);
            var partsThatContainLqdHelium3 = _part.GetConnectedResources(_liquid_helium3_resource_name);
            var partsThatContainLqdHelium4 = _part.GetConnectedResources(_liquid_helium4_resource_name);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxide_resource_name);
            var partsThatContainNitrogen = _part.GetConnectedResources(_nitrogen_resource_name);
            var partsThatContainNeon = _part.GetConnectedResources(_neon_resource_name);

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            _maxCapacitySolarWindMass = partsThatContainSolarWind.Sum(p => p.maxAmount) * _solar_wind_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityDeuteriumMass = partsThatContainDeuterium.Sum(p => p.maxAmount) * _deuterium_density;
            _maxCapacityHelium3Mass = partsThatContainLqdHelium3.Sum(p => p.maxAmount) * _liquid_helium3_density;
            _maxCapacityHelium4Mass = partsThatContainLqdHelium4.Sum(p => p.maxAmount) * _liquid_helium4_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;
            _maxCapacityNitrogenMass = partsThatContainNitrogen.Sum(p => p.maxAmount) * _nitrogen_density;
            _maxCapacityNeonMass = partsThatContainNeon.Sum(p => p.maxAmount) * _neon_density;

            // determine the amount of resources needed for processing (i.e. solar wind) that the vessel actually holds
            _availableSolarWindMass = partsThatContainSolarWind.Sum(r => r.amount) * _solar_wind_density;

            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen_density;
            _spareRoomDeuteriumMass = partsThatContainDeuterium.Sum(r => r.maxAmount - r.amount) * _deuterium_density;
            _spareRoomHelium3Mass = partsThatContainLqdHelium3.Sum(r => r.maxAmount - r.amount) * _liquid_helium3_density;
            _spareRoomHelium4Mass = partsThatContainLqdHelium4.Sum(r => r.maxAmount - r.amount) * _liquid_helium4_density;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;
            _spareRoomNitrogenMass = partsThatContainNitrogen.Sum(r => r.maxAmount - r.amount) * _nitrogen_density;
            _spareRoomNeonMass = partsThatContainNeon.Sum(r => r.maxAmount - r.amount) * _neon_density;

            // this should determine how much resource this process can consume
            double fixedMaxSolarWindConsumptionRate = _current_rate * fixedDeltaTime * _solar_wind_density;
            double solarWindConsumptionRatio = fixedMaxSolarWindConsumptionRate > 0
                ? Math.Min(fixedMaxSolarWindConsumptionRate, _availableSolarWindMass) / fixedMaxSolarWindConsumptionRate
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

                _hydrogen_production_rate = -_part.RequestResource(_hydrogen_resource_name, -hydrogen_rate_temp * fixedDeltaTime / _hydrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen_density;
                _deuterium_production_rate = -_part.RequestResource(_deuterium_resource_name, -deuterium_rate_temp * fixedDeltaTime / _deuterium_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium_density;
                _liquid_helium3_production_rate = -_part.RequestResource(_liquid_helium3_resource_name, -helium3_rate_temp * fixedDeltaTime / _liquid_helium3_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquid_helium3_density;
                _liquid_helium4_production_rate = -_part.RequestResource(_liquid_helium4_resource_name, -helium4_rate_temp * fixedDeltaTime / _liquid_helium4_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _liquid_helium4_density;
                _monoxide_production_rate = -_part.RequestResource(_monoxide_resource_name, -monoxide_rate_temp * fixedDeltaTime / _monoxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_density;
                _nitrogen_production_rate = -_part.RequestResource(_nitrogen_resource_name, -nitrogen_rate_temp * fixedDeltaTime / _nitrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen_density;
                _neon_production_rate = -_part.RequestResource(_neon_resource_name, -neon_rate_temp * fixedDeltaTime / _neon_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neon_density;
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
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Solar Wind Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableSolarWindMass.ToString("0.000000") + " mT / " + _maxCapacitySolarWindMass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Solar Wind Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((((float)_solar_wind_consumption_rate * GameConstants.HOUR_SECONDS).ToString()) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.000000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((float)_hydrogen_production_rate * GameConstants.HOUR_SECONDS).ToString() + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Deuterium Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomDeuteriumMass.ToString("0.000000") + " mT / " + _maxCapacityDeuteriumMass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Deuterium Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((float)_deuterium_production_rate * GameConstants.HOUR_SECONDS).ToString() + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-3 Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHelium3Mass.ToString("0.000000") + " mT / " + _maxCapacityHelium3Mass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-3 Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((float)_liquid_helium3_production_rate * GameConstants.HOUR_SECONDS).ToString() + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-4 Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHelium4Mass.ToString("0.000000") + " mT / " + _maxCapacityHelium4Mass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-4 Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((float)_liquid_helium4_production_rate * GameConstants.HOUR_SECONDS).ToString() + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Monoxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.000000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Monoxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((float)_monoxide_production_rate * GameConstants.HOUR_SECONDS).ToString() + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomNitrogenMass.ToString("0.000000") + " mT / " + _maxCapacityNitrogenMass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((float)_nitrogen_production_rate * GameConstants.HOUR_SECONDS).ToString() + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Neon Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomNeonMass.ToString("0.00000") + " mT / " + _maxCapacityNeonMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Neon Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((float)_neon_production_rate * GameConstants.HOUR_SECONDS).ToString() + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_solar_wind_consumption_rate > 0)
                _status = "Processing of Solar Wind Particles Ongoing";
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage, try allowing overflow";
        }

        public void PrintMissingResources() {
            ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.SolarWind, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
