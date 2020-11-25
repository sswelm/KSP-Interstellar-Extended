using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class AmmoniaElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public AmmoniaElectrolyzer()
        {
            ActivityName = "Ammonia Electrolysis";
            Formula = "NH<size=7>3</size> => N<size=7>2</size> + H<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon / 14.45;
        }

        private double _currentMassRate;
        private double _ammoniaDensity;
        private double _nitrogenDensity;
        private double _hydrogenDensity;

        private double _ammoniaConsumptionMassRate;
        private double _hydrogenProductionMassRate;
        private double _nitrogenProductionMassRate;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() { return _part.GetConnectedResources(ResourceSettings.Config.AmmoniaLqd).Any(rs => rs.amount > 0);  }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;
            _ammoniaDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.AmmoniaLqd).density;
            _nitrogenDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.Nitrogen).density;
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.HydrogenLqd).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _currentMassRate = (CurrentPower / EnergyPerTon);

            var spareCapacityNitrogen = _part.GetResourceSpareCapacity(ResourceSettings.Config.Nitrogen);
            var spareCapacityHydrogen = _part.GetResourceSpareCapacity(ResourceSettings.Config.HydrogenLqd);

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
                _ammoniaConsumptionMassRate = _part.RequestResource(ResourceSettings.Config.AmmoniaLqd, _currentMassRate * fixedDeltaTime / _ammoniaDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _ammoniaDensity;
                var hydrogenMassRate = _ammoniaConsumptionMassRate * GameConstants.ammoniaHydrogenFractionByMass;
                var nitrogenMassRate = _ammoniaConsumptionMassRate * (1 - GameConstants.ammoniaHydrogenFractionByMass);

                _hydrogenProductionMassRate = -_part.RequestResource(ResourceSettings.Config.HydrogenLqd, -hydrogenMassRate * fixedDeltaTime / _hydrogenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogenDensity;
                _nitrogenProductionMassRate = -_part.RequestResource(ResourceSettings.Config.Nitrogen, -nitrogenMassRate * fixedDeltaTime / _nitrogenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogenDensity;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
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

            var spareCapacityNitrogen = _part.GetResourceSpareCapacity(ResourceSettings.Config.Nitrogen);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_SpareCapacityNitrogen"), _bold_label, GUILayout.Width(labelWidth));//"Spare Capacity Nitrogen"
            GUILayout.Label(spareCapacityNitrogen.ToString("0.000"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenProductionMassRate > 0 && _nitrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg1");//"Electrolysing"
            else if (_hydrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg2");//"Electrolysing: Insufficient Nitrogen Storage"
            else if (_nitrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg3");//"Electrolysing: Insufficient Hydrogen Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Postmsg") +" " + ResourceSettings.Config.AmmoniaLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
