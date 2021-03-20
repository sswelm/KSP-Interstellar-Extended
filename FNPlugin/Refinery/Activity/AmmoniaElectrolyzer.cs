using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class AmmoniaElectrolyzer : RefineryActivity
    {
        public AmmoniaElectrolyzer()
        {
            ActivityName = "Ammonia Electrolysis";
            Formula = "NH<size=7>3</size> => N<size=7>2</size> + H<size=7>2</size>";
            PowerRequirements = PluginSettings.Config.BaseELCPowerConsumption;
            EnergyPerTon = PluginSettings.Config.ElectrolysisEnergyPerTon / 14.45;
        }

        private double _currentMassRate;
        private double _ammoniaDensity;
        private double _nitrogenDensity;
        private double _hydrogenDensity;

        private string _ammoniaResourceName;
        private string _nitrogenResourceName;
        private string _hydrogenResourceName;

        private double _ammoniaConsumptionMassRate;
        private double _hydrogenProductionMassRate;
        private double _nitrogenProductionMassRate;

        public override string Status => string.Copy(_status);
        public override RefineryType RefineryType => RefineryType.Electrolysis;

        public override bool HasActivityRequirements()
        {
            if (_ammoniaResourceName == null || _nitrogenResourceName == null || _hydrogenResourceName == null)
                return false;

            return _part.GetConnectedResources(_ammoniaResourceName).Any(rs => rs.amount > 0);
        }

        public override void Initialize(Part localPart, InterstellarRefineryController controller)
        {
            base.Initialize(localPart, controller);

            _ammoniaResourceName = ResourceSettings.Config.AmmoniaGas;
            _nitrogenResourceName = ResourceSettings.Config.NitrogenGas;
            _hydrogenResourceName = ResourceSettings.Config.HydrogenGas;

            _ammoniaDensity = PartResourceLibrary.Instance.GetDefinition(_ammoniaResourceName).density;
            _nitrogenDensity = PartResourceLibrary.Instance.GetDefinition(_nitrogenResourceName).density;
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName).density;
        }

        public override void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _currentMassRate = (CurrentPower / EnergyPerTon);

            var spareCapacityNitrogen = _part.GetResourceSpareCapacity(_nitrogenResourceName);
            var spareCapacityHydrogen = _part.GetResourceSpareCapacity(_hydrogenResourceName);

            var maxNitrogenMassRate = (_currentMassRate * (1 - GameConstants.ammoniaHydrogenFractionByMass)) * fixedDeltaTime / _nitrogenDensity;
            var maxHydrogenMassRate = (_currentMassRate * GameConstants.ammoniaHydrogenFractionByMass) * fixedDeltaTime / _hydrogenDensity;

            // prevent overflow
            if (spareCapacityNitrogen <= maxNitrogenMassRate || spareCapacityHydrogen <= maxHydrogenMassRate)
            {
                _ammoniaConsumptionMassRate = 0;
                _hydrogenProductionMassRate = 0;
                _nitrogenProductionMassRate = 0;
            }
            else
            {
                _ammoniaConsumptionMassRate = _part.RequestResource(_ammoniaResourceName, _currentMassRate * fixedDeltaTime / _ammoniaDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _ammoniaDensity;
                var hydrogenMassRate = _ammoniaConsumptionMassRate * GameConstants.ammoniaHydrogenFractionByMass;
                var nitrogenMassRate = _ammoniaConsumptionMassRate * (1 - GameConstants.ammoniaHydrogenFractionByMass);

                _hydrogenProductionMassRate = -_part.RequestResource(_hydrogenResourceName, -hydrogenMassRate * fixedDeltaTime / _hydrogenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogenDensity;
                _nitrogenProductionMassRate = -_part.RequestResource(_nitrogenResourceName, -nitrogenMassRate * fixedDeltaTime / _nitrogenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogenDensity;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Consumption Rate"
            GUILayout.Label((_ammoniaConsumptionMassRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/"+Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_HydrProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_hydrogenProductionMassRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/" + Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_NitrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Nitrogen Production Rate"
            GUILayout.Label((_nitrogenProductionMassRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/" + Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();

            var spareCapacityNitrogen = _part.GetResourceSpareCapacity(_nitrogenResourceName);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_SpareCapacityNitrogen"), _bold_label, GUILayout.Width(labelWidth));//"Spare Capacity Nitrogen"
            GUILayout.Label(spareCapacityNitrogen.ToString("0.000"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenProductionMassRate > 0 && _nitrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg1");//"Electrolyzing"
            else if (_hydrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg2");//"Electrolyzing: Insufficient Nitrogen Storage"
            else if (_nitrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg3");//"Electrolyzing: Insufficient Hydrogen Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg5");//"Insufficient Storage"
        }

        public override void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Postmsg") +" " + _ammoniaResourceName, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
