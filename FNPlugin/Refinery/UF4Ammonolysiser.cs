using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin.Refinery
{
    class UF4Ammonolysiser : RefineryActivityBase, IRefineryActivity
    {
        double _ammonia_density;
        double _uranium_tetraflouride_density;
        double _uranium_nitride_density;

        double _ammonia_consumption_rate;
        double _uranium_tetraflouride_consumption_rate;
        double _uranium_nitride_production_rate;

        public RefineryType RefineryType { get { return RefineryType.synthesize; } }

        public String ActivityName { get { return "Uranium Tetraflouride Ammonolysis"; } }

        public bool HasActivityRequirements 
        { 
            get 
            {
                    return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride)
                        .Any(rs => rs.amount > 0) && _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0);
            } 
        }

        public double PowerRequirements { get { return PluginHelper.BaseUraniumAmmonolysisPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public UF4Ammonolysiser(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _uranium_tetraflouride_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride).density;
            _uranium_nitride_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumNitride).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime)
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
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ammona Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_ammonia_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Uranium Tetraflouride Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_uranium_tetraflouride_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Uranium Nitride Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_uranium_nitride_production_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_uranium_nitride_production_rate > 0)
            {
                _status = "Uranium Tetraflouride Ammonolysis Process Ongoing";
            } else if (CurrentPower <= 0.01*PowerRequirements)
            {
                _status = "Insufficient Power";
            } 
            else
            {
                if (_ammonia_consumption_rate > 0 && _uranium_tetraflouride_consumption_rate > 0)
                    _status = "Insufficient Storage";
                else if (_ammonia_consumption_rate > 0)
                    _status = "Uranium Tetraflouride Deprived";
                else if (_uranium_tetraflouride_consumption_rate > 0)
                    _status = "Ammonia Deprived";
                else
                    _status = "UF4 and Ammonia Deprived";

            }
        }
        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.Ammonia, 3.0f, ScreenMessageStyle.UPPER_CENTER);
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.UraniumTetraflouride, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }

    }
}
