using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class HeavyWaterElectroliser : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        const double deuteriumAtomicMass = 2.01410178;
        const double oxygenAtomicMass = 15.999;
        const double deuteuriuumMassByFraction = (2 * deuteriumAtomicMass) / (oxygenAtomicMass + (2 * deuteriumAtomicMass)); // 0.201136
        const double oxygenMassByFraction = 1 - deuteuriuumMassByFraction;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        
        protected double _heavy_water_consumption_rate;
        protected double _deuterium_production_rate;
        protected double _oxygen_production_rate;
        protected double _current_power;
        protected double _fixedMaxConsumptionWaterRate;
        protected double _current_rate;
        protected double _consumptionStorageRatio;

        protected double _heavy_water_density;
        protected double _oxygen_density;
        protected double _deuterium_density;

        protected double _availableHeavyWaterMass;
        protected double _spareRoomOxygenMass;
        protected double _spareRoomDeuteriumMass;

        protected double _maxCapacityHeavyWaterMass;
        protected double _maxCapacityDeuteriumMass;
        protected double _maxCapacityOxygenMass;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Heavy Water Electrolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements {  get  {  return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.HeavyWater).Any(rs => rs.amount > 0);  } }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public HeavyWaterElectroliser(Part part) 
        {
            _part = part;

            _vessel = part.vessel;
            _heavy_water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HeavyWater).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
            _deuterium_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdDeuterium).density;
        }

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.HeavyWater);
            var partsThatContainOxygen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Oxygen);
            var partsThatContainDeuteurium = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdDeuterium);

            _maxCapacityHeavyWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _heavy_water_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityDeuteriumMass = partsThatContainDeuteurium.Sum(p => p.maxAmount) * _deuterium_density;

            _availableHeavyWaterMass = partsThatContainWater.Sum(p => p.amount) * _heavy_water_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomDeuteriumMass = partsThatContainDeuteurium.Sum(r => r.maxAmount - r.amount) * _deuterium_density;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate * TimeWarp.fixedDeltaTime, _availableHeavyWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomDeuteriumMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * deuteuriuumMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * oxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real elextrolysis
                _heavy_water_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.HeavyWater, _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _heavy_water_density) / TimeWarp.fixedDeltaTime * _heavy_water_density;

                var deuterium_rate_temp = _heavy_water_consumption_rate * deuteuriuumMassByFraction;
                var oxygen_rate_temp = _heavy_water_consumption_rate * oxygenMassByFraction;

                _deuterium_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, -deuterium_rate_temp * TimeWarp.fixedDeltaTime / _deuterium_density) / TimeWarp.fixedDeltaTime * _deuterium_density;
                _oxygen_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -oxygen_rate_temp * TimeWarp.fixedDeltaTime / _oxygen_density) / TimeWarp.fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _heavy_water_consumption_rate = 0;
                _deuterium_production_rate = 0;
                _oxygen_production_rate = 0;
            }

            updateStatusMessage();
        }

        public void UpdateGUI()
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Heavy Water Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableHeavyWaterMass.ToString("0.0000") + " mT / " + _maxCapacityHeavyWaterMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Consumption Storage Ratio", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Heavy Water Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_heavy_water_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Deuterium Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomDeuteriumMass.ToString("0.00000") + " mT / " + _maxCapacityDeuteriumMass.ToString("0.00000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Deuterium Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_deuterium_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_oxygen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_deuterium_production_rate > 0 && _oxygen_production_rate > 0)
                _status = "Electrolysing Water";
            else if (_fixedMaxConsumptionWaterRate <= 0.0000000001)
                _status = "Out of water";
            else if (_deuterium_production_rate > 0)
                _status = "Insufficient " + InterstellarResourcesConfiguration.Instance.Oxygen + " Storage";
            else if (_oxygen_production_rate > 0)
                _status = "Insufficient " + InterstellarResourcesConfiguration.Instance.Hydrogen + " Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
