using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class AluminiumElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public AluminiumElectrolyzer()
        {
            ActivityName = "Aluminium Electrolysis";
            Formula = "Al<size=7>2</size>O<size=7>3</size> => O<size=7>2</size> + Al<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.AluminiumElectrolysisEnergyPerTon;
        }

        private string _aluminaResourceName;
        private string _aluminiumResourceName;
        private string _oxygenResourceName;

        private double _aluminaDensity;
        private double _aluminiumDensity;
        private double _oxygenDensity;

        private double _aluminaConsumptionRate;
        private double _aluminiumProductionRate;
        private double _oxygenProductionRate;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() { return _part.GetConnectedResources(_aluminaResourceName).Any(rs => rs.amount > 0);  }

        private double _effectivePowerRequirements;

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _aluminaResourceName = ResourceSettings.Config.Alumina;
            _aluminiumResourceName = ResourceSettings.Config.Aluminium;
            _oxygenResourceName = ResourceSettings.Config.OxygenGas;

            _aluminaDensity = PartResourceLibrary.Instance.GetDefinition(_aluminaResourceName).density;
            _aluminiumDensity = PartResourceLibrary.Instance.GetDefinition(_aluminiumResourceName).density;
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectivePowerRequirements = productionModifier * PowerRequirements;
            _current_power = powerFraction * _effectivePowerRequirements;

            _current_rate = CurrentPower / EnergyPerTon;
            _aluminaConsumptionRate = _part.RequestResource(_aluminaResourceName, _current_rate * fixedDeltaTime / _aluminaDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _aluminaDensity;
            _aluminiumProductionRate = _part.RequestResource(_aluminiumResourceName, -_aluminaConsumptionRate * fixedDeltaTime / _aluminiumDensity, ResourceFlowMode.ALL_VESSEL) * _aluminiumDensity / fixedDeltaTime;
            _oxygenProductionRate = _part.RequestResource(_oxygenResourceName, -GameConstants.aluminiumElectrolysisMassRatio * _aluminaConsumptionRate * fixedDeltaTime / _oxygenDensity, ResourceFlowMode.ALL_VESSEL) * _oxygenDensity / fixedDeltaTime;
            UpdateStatusMessage();
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
            GUILayout.Label(_aluminaConsumptionRate * GameConstants.SECONDS_IN_HOUR + " mT/"+Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_hour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_AluminiumProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Aluminium Production Rate"
            GUILayout.Label(_aluminiumProductionRate * GameConstants.SECONDS_IN_HOUR + " mT/"+Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_hour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label(_oxygenProductionRate * GameConstants.SECONDS_IN_HOUR + " mT/"+Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_hour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_aluminiumProductionRate > 0 && _oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg1");//"Electrolysing"
            else if (_aluminaConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg2");//"Electrolysing: Insufficient Oxygen Storage"
            else if (_oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg3");//"Electrolysing: Insufficient Aluminium Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_statumsg5");//"Insufficient Storage"
        }


        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AluminiumElectrolyser_Postmsg") + " " + _aluminaResourceName, 3.0f, ScreenMessageStyle.UPPER_CENTER);//"Missing "
        }
    }
}
