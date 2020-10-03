using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AluminiumElectrolyser : RefineryActivity, IRefineryActivity
    {
        public AluminiumElectrolyser()
        {
            ActivityName = "Aluminium Electrolysis: Al<size=7>2</size>O<size=7>3</size> => O<size=7>2</size> + Al<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
        }

        double _alumina_density;
        double _aluminium_density;
        double _oxygen_density;

        double _alumina_consumption_rate;
        double _aluminium_production_rate;
        double _oxygen_production_rate;

        public RefineryType RefineryType { get { return RefineryType.Electrolysis; } }

        public bool HasActivityRequirements() { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Alumina).Any(rs => rs.amount > 0);  }

        private double _effectivePowerRequirements;

        public string Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;
            _alumina_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Alumina).density;
            _aluminium_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Aluminium).density;
            _oxygen_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdOxygen).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectivePowerRequirements = productionModifier * PowerRequirements;
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
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_AluminaConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Alumina Consumption Rate"
            GUILayout.Label(_alumina_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/"+Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_hour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_AluminiumProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Aluminium Production Rate"
            GUILayout.Label(_aluminium_production_rate * GameConstants.SECONDS_IN_HOUR + " mT/"+Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_hour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label(_oxygen_production_rate * GameConstants.SECONDS_IN_HOUR + " mT/"+Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_hour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_aluminium_production_rate > 0 && _oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg1");//"Electrolysing"
            else if (_alumina_consumption_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg2");//"Electrolysing: Insufficient Oxygen Storage"
            else if (_oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg3");//"Electrolysing: Insufficient Aluminium Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg5");//"Insufficient Storage"
        }


        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.Alumina, 3.0f, ScreenMessageStyle.UPPER_CENTER);//"Missing "
        }

    }
}
