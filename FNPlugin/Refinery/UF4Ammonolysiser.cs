using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class UF4Ammonolysiser : RefineryActivity, IRefineryActivity
    {
        public UF4Ammonolysiser()
        {
            ActivityName = "Uranium Tetraflouride Ammonolysis";
            PowerRequirements = PluginHelper.BaseUraniumAmmonolysisPowerConsumption;
        }

        double _ammonia_density;
        double _uranium_tetraflouride_density;
        double _uranium_nitride_density;

        double _ammonia_consumption_rate;
        double _uranium_tetraflouride_consumption_rate;
        double _uranium_nitride_production_rate;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride)
                .Any(rs => rs.amount > 0) && _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _uranium_tetraflouride_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride).density;
            _uranium_nitride_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumNitride).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower * GameConstants.baseUraniumAmmonolysisRate;
            double uf4persec = _current_rate * 1.24597 / _uranium_tetraflouride_density;
            double ammoniapersec = _current_rate * 0.901 / _ammonia_density;
            _uranium_tetraflouride_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride, uf4persec * fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) * _uranium_tetraflouride_density / fixedDeltaTime;
            _ammonia_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, ammoniapersec * fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) * _ammonia_density / fixedDeltaTime;

            if (_ammonia_consumption_rate > 0 && _uranium_tetraflouride_consumption_rate > 0)
                _uranium_nitride_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.UraniumNitride, -_uranium_tetraflouride_consumption_rate / 1.24597 / _uranium_nitride_density * fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _uranium_nitride_density;

            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_AmmonaConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammona Consumption Rate"
            GUILayout.Label(_ammonia_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Uranium Tetraflouride Consumption Rate"
            GUILayout.Label(_uranium_tetraflouride_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Uranium Nitride Production Rate"
            GUILayout.Label(_uranium_nitride_production_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_uranium_nitride_production_rate > 0)
            {
                _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg1");//"Uranium Tetraflouride Ammonolysis Process Ongoing"
            } else if (CurrentPower <= 0.01*PowerRequirements)
            {
                _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg2");//"Insufficient Power"
            } 
            else
            {
                if (_ammonia_consumption_rate > 0 && _uranium_tetraflouride_consumption_rate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg3");//"Insufficient Storage"
                else if (_ammonia_consumption_rate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg4");//"Uranium Tetraflouride Deprived"
                else if (_uranium_tetraflouride_consumption_rate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg5");//"Ammonia Deprived"
                else
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg6");//"UF4 and Ammonia Deprived"

            }
        }
        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.Ammonia, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.UraniumTetraflouride, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
