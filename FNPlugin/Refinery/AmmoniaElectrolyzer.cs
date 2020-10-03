using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AmmoniaElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public AmmoniaElectrolyzer()
        {
            ActivityName = "Ammonia Electrolysis: NH<size=7>3</size> => N<size=7>2</size> + H<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
        }

        double _current_mass_rate;
        double _ammonia_density;
        double _nitrogen_density;
        double _hydrogen_density;

        double _ammonia_consumption_mass_rate;
        double _hydrogen_production_mass_rate;
        double _nitrogen_production_mass_rate;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0);  }

        public string Status => string.Copy(_status);

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;
            _ammonia_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _nitrogen_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Nitrogen).density;
            _hydrogen_density = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
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
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Consumption Rate"
            GUILayout.Label((_ammonia_consumption_mass_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/"+Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_HydrProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_hydrogen_production_mass_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/" + Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_NitrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Nitrogen Production Rate"
            GUILayout.Label((_nitrogen_production_mass_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/" + Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();

            var spare_capacity_nitrogen = _part.GetResourceSpareCapacity(InterstellarResourcesConfiguration.Instance.Nitrogen);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_SpareCapacityNitrogen"), _bold_label, GUILayout.Width(labelWidth));//"Spare Capacity Nitrogen"
            GUILayout.Label(spare_capacity_nitrogen.ToString("0.000"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_mass_rate > 0 && _nitrogen_production_mass_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg1");//"Electrolysing"
            else if (_hydrogen_production_mass_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg2");//"Electrolysing: Insufficient Nitrogen Storage"
            else if (_nitrogen_production_mass_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg3");//"Electrolysing: Insufficient Hydrogen Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources() {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Postmsg") +" " + InterstellarResourcesConfiguration.Instance.Ammonia, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
