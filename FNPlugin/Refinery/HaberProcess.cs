using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Resources;
using FNPlugin.Extensions;

namespace FNPlugin.Refinery
{
    class HaberProcess : RefineryActivityBase, IRefineryActivity
    {
        double _hydrogen_density;
        double _nitrogen_density;
        double _ammonia_density;

        double _hydrogen_consumption_rate;
        double _ammonia_production_rate;
        double _nitrogen_consumption_rate;

        public RefineryType RefineryType { get { return RefineryType.synthesize; } }

        public String ActivityName { get { return "Haber Process (Ammonia Production)"; } }

        public bool HasActivityRequirements 
        { 
            get { return HasAccessToHydrogen() && HasAccessToNitrogen(); } 
        }

        private bool HasAccessToHydrogen()
        {
            return  _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen).Any(rs => rs.amount > 0);
        }

        private bool HasAccessToNitrogen()
        {
			var atmosphericNitrogen = AtmosphericResourceHandler.getAtmosphericResourceContent(_vessel.mainBody.flightGlobalsIndex, InterstellarResourcesConfiguration.Instance.Nitrogen);

            return _vessel.atmDensity * atmosphericNitrogen >= 0.01 || _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Nitrogen).Any(rs => rs.amount > 0);
        }

        public double PowerRequirements { get { return PluginHelper.BaseHaberProcessPowerConsumption; } }

        private double _effectiveMaxPowerRequirements;

        public String Status { get { return String.Copy(_status); } }

        public HaberProcess(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _nitrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Nitrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double powerModifier, bool allowOverflow, double fixedDeltaTime)
        {
            _effectiveMaxPowerRequirements = PowerRequirements * powerModifier;
            _current_power = powerFraction * _effectiveMaxPowerRequirements;

            _current_rate = CurrentPower / PluginHelper.HaberProcessEnergyPerTon;

            double ammoniaNitrogenFractionByMass = (1 - GameConstants.ammoniaHydrogenFractionByMass);

            double hydrogen_rate = _current_rate * GameConstants.ammoniaHydrogenFractionByMass;
            double nitrogen_rate = _current_rate * ammoniaNitrogenFractionByMass;

            _hydrogen_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, hydrogen_rate * fixedDeltaTime / _hydrogen_density, ResourceFlowMode.ALL_VESSEL) * _hydrogen_density / fixedDeltaTime;
            _nitrogen_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Nitrogen, nitrogen_rate * fixedDeltaTime / _nitrogen_density, ResourceFlowMode.ALL_VESSEL) * _nitrogen_density / fixedDeltaTime;

            if (_hydrogen_consumption_rate > 0 && _nitrogen_consumption_rate > 0)
                _ammonia_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, -_nitrogen_consumption_rate / ammoniaNitrogenFractionByMass * fixedDeltaTime / _ammonia_density, ResourceFlowMode.ALL_VESSEL) * _ammonia_density / fixedDeltaTime;
            
            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Rate ", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_current_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_nitrogen_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_hydrogen_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ammonia Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_ammonia_production_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_ammonia_production_rate > 0) 
                _status = "Haber Process Ongoing";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }

        public void PrintMissingResources()
        {
            if (!HasAccessToHydrogen())
                ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.Hydrogen, 3.0f, ScreenMessageStyle.UPPER_CENTER);
            if (!HasAccessToNitrogen())
                ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.Nitrogen, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
