using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class MethanePyrolyser : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_power;
        protected double _fixedConsumptionRate;
        protected double _consumptionStorageRatio;

        protected double _monoxide_density;
        protected double _methane_density;
        protected double _hydrogen_density;
        protected double _oxygen_density;
        
        protected double _methane_consumption_rate;
        protected double _oxygen_consumption_rate;

        protected double _hydrogen_production_rate;
        protected double _monoxide_production_rate;
        protected double _current_rate;
        

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Methane Pyrolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements {
            get
            {
                return _part.GetConnectedResources(_methane_resource_name).Any(rs => rs.amount > 0) &&
                    _part.GetConnectedResources(_oxygen_resource_name).Any(rs => rs.amount > 0);
            }
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        protected string _monoxide_resource_name;
        protected string _methane_resource_name;
        protected string _hydrogen_resource_name;
        protected string _oxygen_resource_name;

        public MethanePyrolyser(Part part) 
        {
            _part = part;
            _vessel = part.vessel;

            _monoxide_resource_name = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            _hydrogen_resource_name = InterstellarResourcesConfiguration.Instance.Hydrogen;
            _methane_resource_name = InterstellarResourcesConfiguration.Instance.Methane;
            _oxygen_resource_name = InterstellarResourcesConfiguration.Instance.Oxygen;

            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxide_resource_name).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_resource_name).density;
            _methane_density = PartResourceLibrary.Instance.GetDefinition(_methane_resource_name).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(_oxygen_resource_name).density;
        }

        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityMethaneMass;
        protected double _maxCapacityOxygenMass;

        protected double _availableMethaneMass;
        protected double _availableOxygenMass;
        protected double _spareRoomHydrogenMass;
        protected double _spareRoomMonoxideMass;

        protected double _monoxideMassByFraction = 1 - 18.01528 / (18.01528 + 28.010); // taken from reverse water gas shift
        protected double _hydrogenMassByFraction = (8 * 1.008) / (44.01 + (8 * 1.008));
        protected double _oxygenMassByFraction = 32.0 / 52.0;
        protected double _methaneMassByFraction = 20.0 / 52.0;

        private double combined_consumption_rate;

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon; //* _vessel.atmDensity;

            // determine how much resource we have
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxide_resource_name);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogen_resource_name);
            var partsThatContainMethane = _part.GetConnectedResources(_methane_resource_name);
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygen_resource_name);

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMethaneMass = partsThatContainMethane.Sum(p => p.maxAmount) * _methane_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;

            // determine the amount of resources needed for pyrolysis that the vessel actually holds
            _availableMethaneMass = partsThatContainMethane.Sum(r => r.amount) * _methane_density;
            _availableOxygenMass = partsThatContainOxygen.Sum(r => r.amount) * _oxygen_density;

            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen_density;

            // this should determine how much resources this process can consume
            var fixedMaxMethaneConsumptionRate = _current_rate * _methaneMassByFraction * TimeWarp.fixedDeltaTime;
            var methaneConsumptionRatio = fixedMaxMethaneConsumptionRate > 0
                ? Math.Min(fixedMaxMethaneConsumptionRate, _availableMethaneMass) / fixedMaxMethaneConsumptionRate
                : 0;

            var fixedMaxOxygenConsumptionRate = _current_rate * _oxygenMassByFraction * TimeWarp.fixedDeltaTime;
            var oxygenConsumptionRatio = fixedMaxOxygenConsumptionRate > 0 ? Math.Min(fixedMaxOxygenConsumptionRate, _availableOxygenMass) / fixedMaxOxygenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * TimeWarp.fixedDeltaTime * Math.Min(methaneConsumptionRatio, oxygenConsumptionRatio);

            // begin the pyrolysis process
            if (_fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomMonoxideMass > 0))
            {
                
                var fixedMaxMonoxideRate = _fixedConsumptionRate * _monoxideMassByFraction;
                var fixedMaxHydrogenRate = _fixedConsumptionRate * _hydrogenMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate, fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate);
                               
                // this consumes the resources
                _oxygen_consumption_rate = _part.RequestResource(_oxygen_resource_name, _oxygenMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _oxygen_density) / TimeWarp.fixedDeltaTime * _oxygen_density;
                _methane_consumption_rate = _part.RequestResource(_methane_resource_name, _methaneMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _methane_density) / TimeWarp.fixedDeltaTime * _methane_density;
                var combined_consumption_rate = _oxygen_consumption_rate + _methane_consumption_rate;

                // this produces the products
                var monoxide_rate_temp = combined_consumption_rate * _monoxideMassByFraction;
                var water_rate_temp = combined_consumption_rate * _hydrogenMassByFraction;

                _monoxide_production_rate = -_part.RequestResource(_monoxide_resource_name, -monoxide_rate_temp * TimeWarp.fixedDeltaTime / _monoxide_density) / TimeWarp.fixedDeltaTime * _monoxide_density;
                _hydrogen_production_rate = -_part.RequestResource(_hydrogen_resource_name, -water_rate_temp * TimeWarp.fixedDeltaTime / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;
            }
            else
            {
                _methane_consumption_rate = 0;
                _oxygen_consumption_rate = 0;
                _hydrogen_production_rate = 0;
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
            GUILayout.Label("Overal Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((combined_consumption_rate / TimeWarp.fixedDeltaTime * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_oxygen_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Methane Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableMethaneMass.ToString("0.0000") + " mT / " + _maxCapacityMethaneMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Methane Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_methane_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Monoxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Monoxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_monoxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_rate > 0)
                _status = "Methane Pyrolysis Ongoing";
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
