using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AluminiumElectrolyser : RefineryActivityBase, IRefineryActivity
    {
        double _alumina_density;
        double _aluminium_density;
        double _oxygen_density;

        double _alumina_consumption_rate;
        double _aluminium_production_rate;
        double _oxygen_production_rate;

        public RefineryType RefineryType { get { return RefineryType.electrolysis; } }

        public String ActivityName { get { return "Aliminium Electrolysis: Al<size=7>2</size>O<size=7>3</size> => O<size=7>2</size> + Al<size=7>2</size>"; } }

        public bool HasActivityRequirements() { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Alumina).Any(rs => rs.amount > 0);  }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        private double _effectivePowerRequirements;

        public String Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;
            _alumina_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Alumina).density;
            _aluminium_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Aluminium).density;
            _oxygen_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdOxygen).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double powerModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectivePowerRequirements = powerModifier * PowerRequirements;
            _current_power = powerFraction * _effectivePowerRequirements;

            _current_rate = CurrentPower / PluginHelper.AluminiumElectrolysisEnergyPerTon;
            _alumina_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Alumina, _current_rate * fixedDeltaTime / _alumina_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _alumina_density;
            _aluminium_production_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Aluminium, -_alumina_consumption_rate * fixedDeltaTime / _aluminium_density, ResourceFlowMode.ALL_VESSEL) * _aluminium_density / fixedDeltaTime;
            _oxygen_production_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdOxygen, -GameConstants.aluminiumElectrolysisMassRatio * _alumina_consumption_rate * fixedDeltaTime / _oxygen_density, ResourceFlowMode.ALL_VESSEL) * _oxygen_density / fixedDeltaTime;
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
            GUILayout.Label("Alumina Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_alumina_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Aluminium Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_aluminium_production_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_oxygen_production_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_aluminium_production_rate > 0 && _oxygen_production_rate > 0)
                _status = "Electrolysing";
            else if (_alumina_consumption_rate > 0)
                _status = "Electrolysing: Insufficient Oxygen Storage";
            else if (_oxygen_production_rate > 0)
                _status = "Electrolysing: Insufficient Aluminium Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }


        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.Alumina, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }

    }
}
