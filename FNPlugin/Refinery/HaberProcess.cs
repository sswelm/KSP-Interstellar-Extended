using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class HaberProcess : RefineryActivityBase, IRefineryActivity
    {
        double _hydrogen_consumption_rate;
        double _ammonia_production_rate;
        double _nitrogen_consumption_rate;

        PartResourceDefinition definition_hydrogen;
        PartResourceDefinition definition_nitrogen;
        PartResourceDefinition definition_ammonia;

        double availalble_hydrogen;
        double availalble_nitrogen;
        double spare_capacity_ammonia;

        double ammonia_density;
        double hydrogen_density;
        double nitrogen_density;

        public RefineryType RefineryType { get { return RefineryType.synthesize; } }

        public string ActivityName { get { return "Haber Process: H<size=7>2</size> + N<size=7>2</size> => NH<size=7>3</size> (Ammonia) "; } }

        public bool HasActivityRequirements ()
        {
            return HasAccessToHydrogen() & HasAccessToNitrogen() & HasSpareCapacityAmmonia();  
        }

        private bool HasAccessToHydrogen()
        {
            availalble_hydrogen = _part.GetResourceAvailable(definition_hydrogen, ResourceFlowMode.ALL_VESSEL);

            return availalble_hydrogen > 0;
        }

        private bool HasAccessToNitrogen()
        {
            availalble_nitrogen = _part.GetResourceAvailable(definition_nitrogen, ResourceFlowMode.ALL_VESSEL);

            return availalble_nitrogen > 0;
        }

        private bool HasSpareCapacityAmmonia()
        {
            spare_capacity_ammonia = _part.GetResourceSpareCapacity(definition_ammonia, ResourceFlowMode.ALL_VESSEL);

            return spare_capacity_ammonia > 0;
        }

        public double PowerRequirements { get { return PluginHelper.BaseHaberProcessPowerConsumption; } }

        private double _effectiveMaxPowerRequirements;

        public String Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            definition_ammonia = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia);
            definition_hydrogen = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen);
            definition_nitrogen = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Nitrogen);

            ammonia_density = (double)(decimal)definition_ammonia.density;
            hydrogen_density = (double)(decimal)definition_hydrogen.density;
            nitrogen_density = (double)(decimal)definition_nitrogen.density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double powerModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPowerRequirements = PowerRequirements * powerModifier;
            _current_power = powerFraction * _effectiveMaxPowerRequirements;
            _current_rate = CurrentPower / PluginHelper.HaberProcessEnergyPerTon;

            var hydrogen_rate = _current_rate * GameConstants.ammoniaHydrogenFractionByMass;
            var nitrogen_rate = _current_rate * (1 - GameConstants.ammoniaHydrogenFractionByMass);

            var required_hydrogen = hydrogen_rate * fixedDeltaTime / hydrogen_density;
            var required_nitrogen = nitrogen_rate * fixedDeltaTime / nitrogen_density;
            var max_production_ammonia = required_hydrogen * hydrogen_density / GameConstants.ammoniaHydrogenFractionByMass / ammonia_density;

            var supply_ratio_hydrogen = required_hydrogen > 0 ? Math.Min(1, availalble_hydrogen / required_hydrogen) : 0;
            var supply_ratio_nitrogen = required_nitrogen > 0 ? Math.Min(1, availalble_nitrogen / required_nitrogen) : 0;
            var production_ratio_ammonia = max_production_ammonia > 0 ? Math.Min(1, spare_capacity_ammonia / max_production_ammonia) : 0;

            var adjustedRateRatio = Math.Min(production_ratio_ammonia, Math.Min(supply_ratio_hydrogen, supply_ratio_nitrogen));

            _hydrogen_consumption_rate = _part.RequestResource(definition_hydrogen.id, adjustedRateRatio * required_hydrogen, ResourceFlowMode.ALL_VESSEL) * hydrogen_density / fixedDeltaTime;
            _nitrogen_consumption_rate = _part.RequestResource(definition_nitrogen.id, adjustedRateRatio * required_nitrogen, ResourceFlowMode.ALL_VESSEL) * nitrogen_density / fixedDeltaTime;

            var consumed_ratio_hydrogen = hydrogen_rate > 0 ? _hydrogen_consumption_rate / hydrogen_rate : 0;
            var consumed_ratio_nitrogen = nitrogen_rate > 0 ? _nitrogen_consumption_rate / nitrogen_rate : 0;

            var consumedRatio = Math.Min(consumed_ratio_hydrogen, consumed_ratio_nitrogen);

            if (consumedRatio > 0)
            {
                var ammonia_production = -consumedRatio * max_production_ammonia;
                var ammonia_produced = -_part.RequestResource(definition_ammonia.id, ammonia_production, ResourceFlowMode.ALL_VESSEL);
                _ammonia_production_rate = ammonia_produced * ammonia_density / fixedDeltaTime;

                if (isStartup)
                {
                    string message = "produced: " + (ammonia_produced * ammonia_density * 1000).ToString("0.000") + " kg Ammonia";
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 20, ScreenMessageStyle.LOWER_CENTER);
                }
            }
            
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
            GUILayout.Label("Current Rate:", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_current_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Available:", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((availalble_nitrogen * nitrogen_density * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Consumption Rate:", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_nitrogen_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Available:", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((availalble_hydrogen * hydrogen_density * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Consumption Rate:", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_hydrogen_consumption_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ammonia Spare Capacity:", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((spare_capacity_ammonia * ammonia_density * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ammonia Production Rate:", _bold_label, GUILayout.Width(labelWidth));
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
            if (!HasSpareCapacityAmmonia())
                ScreenMessages.PostScreenMessage("No Spare Capacity " + InterstellarResourcesConfiguration.Instance.Ammonia, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
