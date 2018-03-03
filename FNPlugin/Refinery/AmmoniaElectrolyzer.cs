using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin.Refinery
{
    class AmmoniaElectrolyzer : RefineryActivityBase, IRefineryActivity
    {
        double _current_mass_rate;
        double _ammonia_density;
        double _nitrogen_density;
        double _hydrogen_density;

        double _ammonia_consumption_mass_rate;
        double _hydrogen_production_mass_rate;
        double _nitrogen_production_mass_rate;

        public RefineryType RefineryType { get { return RefineryType.electrolysis; ; } }

        public String ActivityName { get { return "Ammonia Electrolysis"; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public AmmoniaElectrolyzer(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _nitrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Nitrogen).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_mass_rate = (CurrentPower / PluginHelper.ElectrolysisEnergyPerTon) * 14.45;

            var spare_capacity_nitrogen = _part.GetResourceSpareCapacity(InterstellarResourcesConfiguration.Instance.Nitrogen);
            var spare_capacity_hydrogen = _part.GetResourceSpareCapacity(InterstellarResourcesConfiguration.Instance.Hydrogen);

            double max_nitrogen_mass_rate = (_current_mass_rate * (1 - GameConstants.ammoniaHydrogenFractionByMass)) * fixedDeltaTime / _nitrogen_density;
            double max_hydrogen_mass_rate = (_current_mass_rate * GameConstants.ammoniaHydrogenFractionByMass) * fixedDeltaTime / _hydrogen_density;

            // prevent overflow
            if (spare_capacity_nitrogen <= max_nitrogen_mass_rate || spare_capacity_hydrogen <= max_hydrogen_mass_rate)
            {
                _ammonia_consumption_mass_rate = 0;
                _hydrogen_production_mass_rate = 0;
                _nitrogen_production_mass_rate = 0;
            }
            else
            {
                _ammonia_consumption_mass_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, _current_mass_rate * fixedDeltaTime / _ammonia_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _ammonia_density;
                double hydrogen_mass_rate = _ammonia_consumption_mass_rate * GameConstants.ammoniaHydrogenFractionByMass;
                double nitrogen_mass_rate = _ammonia_consumption_mass_rate * (1 - GameConstants.ammoniaHydrogenFractionByMass);

                _hydrogen_production_mass_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, -hydrogen_mass_rate * fixedDeltaTime / _hydrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen_density;
                _nitrogen_production_mass_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.Nitrogen, -nitrogen_mass_rate * fixedDeltaTime / _nitrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen_density;
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
            GUILayout.Label("Ammonia Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_ammonia_consumption_mass_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_production_mass_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_nitrogen_production_mass_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            var spare_capacity_nitrogen = _part.GetResourceSpareCapacity(InterstellarResourcesConfiguration.Instance.Nitrogen);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Spare Capacity Nitrogen", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(spare_capacity_nitrogen.ToString("0.000"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_mass_rate > 0 && _nitrogen_production_mass_rate > 0)
                _status = "Electrolysing";
            else if (_hydrogen_production_mass_rate > 0)
                _status = "Electrolysing: Insufficient Nitrogen Storage";
            else if (_nitrogen_production_mass_rate > 0)
                _status = "Electrolysing: Insufficient Hydrogen Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }

        public void PrintMissingResources() {
            ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.Ammonia, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
