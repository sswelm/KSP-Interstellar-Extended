using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class HaberProcess : RefineryActivity, IRefineryActivity
    {
        public HaberProcess()
        {
            ActivityName = "Haber Process";
            Formula = "H<size=7>2</size> + N<size=7>2</size> => NH<size=7>3</size> (Ammonia)";
            PowerRequirements = PluginHelper.BaseHaberProcessPowerConsumption;
            EnergyPerTon = PluginHelper.HaberProcessEnergyPerTon;
        }

        private double _hydrogenConsumptionRate;
        private double _ammoniaProductionRate;
        private double _nitrogenConsumptionRate;

        private PartResourceDefinition _definitionHydrogen;
        private PartResourceDefinition _definitionNitrogen;
        private PartResourceDefinition _definitionAmmonia;

        private double _availableHydrogen;
        private double _availableNitrogen;
        private double _spareCapacityAmmonia;

        private double _ammoniaDensity;
        private double _hydrogenDensity;
        private double _nitrogenDensity;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements ()
        {
            return HasAccessToHydrogen() & HasAccessToNitrogen() & HasSpareCapacityAmmonia();
        }

        private bool HasAccessToHydrogen()
        {
            _availableHydrogen = _part.GetResourceAvailable(_definitionHydrogen, ResourceFlowMode.ALL_VESSEL);

            return _availableHydrogen > 0;
        }

        private bool HasAccessToNitrogen()
        {
            _availableNitrogen = _part.GetResourceAvailable(_definitionNitrogen, ResourceFlowMode.ALL_VESSEL);

            return _availableNitrogen > 0;
        }

        private bool HasSpareCapacityAmmonia()
        {
            _spareCapacityAmmonia = _part.GetResourceSpareCapacity(_definitionAmmonia, ResourceFlowMode.ALL_VESSEL);

            return _spareCapacityAmmonia > 0;
        }

        private double _effectiveMaxPowerRequirements;

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _definitionAmmonia = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.AmmoniaLqd);
            _definitionHydrogen = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen);
            _definitionNitrogen = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Nitrogen);

            _ammoniaDensity = _definitionAmmonia.density;
            _hydrogenDensity = _definitionHydrogen.density;
            _nitrogenDensity = _definitionNitrogen.density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPowerRequirements = PowerRequirements * productionModifier;
            _current_power = powerFraction * _effectiveMaxPowerRequirements;
            _current_rate = CurrentPower / EnergyPerTon;

            var hydrogenRate = _current_rate * GameConstants.ammoniaHydrogenFractionByMass;
            var nitrogenRate = _current_rate * (1 - GameConstants.ammoniaHydrogenFractionByMass);

            var requiredHydrogen = hydrogenRate * fixedDeltaTime / _hydrogenDensity;
            var requiredNitrogen = nitrogenRate * fixedDeltaTime / _nitrogenDensity;
            var maxProductionAmmonia = requiredHydrogen * _hydrogenDensity / GameConstants.ammoniaHydrogenFractionByMass / _ammoniaDensity;

            var supplyRatioHydrogen = requiredHydrogen > 0 ? Math.Min(1, _availableHydrogen / requiredHydrogen) : 0;
            var supplyRatioNitrogen = requiredNitrogen > 0 ? Math.Min(1, _availableNitrogen / requiredNitrogen) : 0;
            var productionRatioAmmonia = maxProductionAmmonia > 0 ? Math.Min(1, _spareCapacityAmmonia / maxProductionAmmonia) : 0;

            var adjustedRateRatio = Math.Min(productionRatioAmmonia, Math.Min(supplyRatioHydrogen, supplyRatioNitrogen));

            _hydrogenConsumptionRate = _part.RequestResource(_definitionHydrogen.id, adjustedRateRatio * requiredHydrogen, ResourceFlowMode.ALL_VESSEL) * _hydrogenDensity / fixedDeltaTime;
            _nitrogenConsumptionRate = _part.RequestResource(_definitionNitrogen.id, adjustedRateRatio * requiredNitrogen, ResourceFlowMode.ALL_VESSEL) * _nitrogenDensity / fixedDeltaTime;

            var consumedRatioHydrogen = hydrogenRate > 0 ? _hydrogenConsumptionRate / hydrogenRate : 0;
            var consumedRatioNitrogen = nitrogenRate > 0 ? _nitrogenConsumptionRate / nitrogenRate : 0;

            var consumedRatio = Math.Min(consumedRatioHydrogen, consumedRatioNitrogen);

            if (consumedRatio > 0)
            {
                var ammoniaProduction = -consumedRatio * maxProductionAmmonia;
                var ammoniaProduced = -_part.RequestResource(_definitionAmmonia.id, ammoniaProduction, ResourceFlowMode.ALL_VESSEL);
                _ammoniaProductionRate = ammoniaProduced * _ammoniaDensity / fixedDeltaTime;

                if (isStartup)
                {
                    string message = "produced: " + (ammoniaProduced * _ammoniaDensity * 1000).ToString("0.000") + " kg Ammonia";//
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 20, ScreenMessageStyle.LOWER_CENTER);
                }
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_CurrentRate"), _bold_label, GUILayout.Width(labelWidth));//"Current Rate:"
            GUILayout.Label(_current_rate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_NitrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Nitrogen Available:"
            GUILayout.Label((_availableNitrogen * _nitrogenDensity * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_NitrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Nitrogen Consumption Rate:"
            GUILayout.Label(_nitrogenConsumptionRate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_HydrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Available:"
            GUILayout.Label((_availableHydrogen * _hydrogenDensity * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_HydrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Consumption Rate:"
            GUILayout.Label(_hydrogenConsumptionRate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_AmmoniaSpareCapacity"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Spare Capacity:"
            GUILayout.Label((_spareCapacityAmmonia * _ammoniaDensity * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_AmmoniaProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Production Rate:"
            GUILayout.Label(_ammoniaProductionRate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_ammoniaProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HaberProcess_Statumsg1");//"Haber Process Ongoing"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_HaberProcess_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_HaberProcess_Statumsg4");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!HasAccessToHydrogen())
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HaberProcess_Postmsg1") + " " + InterstellarResourcesConfiguration.Instance.Hydrogen, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!HasAccessToNitrogen())
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HaberProcess_Postmsg1") + " " + InterstellarResourcesConfiguration.Instance.Nitrogen, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!HasSpareCapacityAmmonia())
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HaberProcess_Postmsg2") + " " + InterstellarResourcesConfiguration.Instance.AmmoniaLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//No Spare Capacity
        }
    }
}
