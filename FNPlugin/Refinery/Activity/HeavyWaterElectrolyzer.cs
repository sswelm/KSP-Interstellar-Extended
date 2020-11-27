using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class HeavyWaterElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public HeavyWaterElectrolyzer()
        {
            ActivityName = "Heavy Water Electrolysis";
            Formula = "D<size=7>2</size>O => D<size=7>2</size> + O<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        private const double DeuteriumAtomicMass = 2.01410178;
        private const double OxygenAtomicMass = 15.999;
        private const double DeuteriumMassByFraction = (2 * DeuteriumAtomicMass) / (OxygenAtomicMass + (2 * DeuteriumAtomicMass)); // 0.201136
        private const double OxygenMassByFraction = 1 - DeuteriumMassByFraction;

        double _heavyWaterConsumptionRate;
        double _deuteriumProductionRate;
        double _oxygenProductionRate;
        double _fixedMaxConsumptionWaterRate;
        double _consumptionStorageRatio;

        double _heavyWaterDensity;
        double _oxygenDensity;
        double _deuteriumDensity;

        double _availableHeavyWaterMass;
        double _spareRoomOxygenMass;
        double _spareRoomDeuteriumMass;

        double _maxCapacityHeavyWaterMass;
        double _maxCapacityDeuteriumMass;
        double _maxCapacityOxygenMass;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() {  return _part.GetConnectedResources(ResourceSettings.Config.HeavyWater).Any(rs => rs.amount > 0);   }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;

            _vessel = localPart.vessel;
            _heavyWaterDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.HeavyWater).density;
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.OxygenLqd).density;
            _deuteriumDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.DeuteriumLqd).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(ResourceSettings.Config.HeavyWater).ToList();
            var partsThatContainOxygen = _part.GetConnectedResources(ResourceSettings.Config.OxygenLqd).ToList();
            var partsThatContainDeuterium = _part.GetConnectedResources(ResourceSettings.Config.DeuteriumLqd).ToList();

            _maxCapacityHeavyWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _heavyWaterDensity;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygenDensity;
            _maxCapacityDeuteriumMass = partsThatContainDeuterium.Sum(p => p.maxAmount) * _deuteriumDensity;

            _availableHeavyWaterMass = partsThatContainWater.Sum(p => p.amount) * _heavyWaterDensity;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygenDensity;
            _spareRoomDeuteriumMass = partsThatContainDeuterium.Sum(r => r.maxAmount - r.amount) * _deuteriumDensity;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate * fixedDeltaTime, _availableHeavyWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomDeuteriumMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * DeuteriumMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * OxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real electrolysis
                _heavyWaterConsumptionRate = _part.RequestResource(ResourceSettings.Config.HeavyWater, _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _heavyWaterDensity) / fixedDeltaTime * _heavyWaterDensity;

                var deuteriumRateTemp = _heavyWaterConsumptionRate * DeuteriumMassByFraction;
                var oxygenRateTemp = _heavyWaterConsumptionRate * OxygenMassByFraction;

                _deuteriumProductionRate = -_part.RequestResource(ResourceSettings.Config.DeuteriumLqd, -deuteriumRateTemp * fixedDeltaTime / _deuteriumDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuteriumDensity;
                _oxygenProductionRate = -_part.RequestResource(ResourceSettings.Config.OxygenLqd, -oxygenRateTemp * fixedDeltaTime / _oxygenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygenDensity;
            }
            else
            {
                _heavyWaterConsumptionRate = 0;
                _deuteriumProductionRate = 0;
                _oxygenProductionRate = 0;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Available"), _bold_label, GUILayout.Width(labelWidth));//"Heavy Water Available"
            GUILayout.Label(_availableHeavyWaterMass.ToString("0.0000") + " mT / " + _maxCapacityHeavyWaterMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_HeavyWaterConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Heavy Water Consumption Rate"
            GUILayout.Label((_heavyWaterConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_DeuteriumStorage"), _bold_label, GUILayout.Width(labelWidth));//"Deuterium Storage"
            GUILayout.Label(_spareRoomDeuteriumMass.ToString("0.00000") + " mT / " + _maxCapacityDeuteriumMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_DeuteriumProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Deuterium Production Rate"
            GUILayout.Label((_deuteriumProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygenProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_deuteriumProductionRate > 0 && _oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg1");//"Electrolyzing Water"
            else if (_fixedMaxConsumptionWaterRate <= 0.0000000001)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg2");//"Out of water"
            else if (_deuteriumProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg3", ResourceSettings.Config.OxygenLqd);//"Insufficient " +  + " Storage"
            else if (_oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg3", ResourceSettings.Config.HydrogenLqd);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Postmsg") + " " + ResourceSettings.Config.HeavyWater, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
