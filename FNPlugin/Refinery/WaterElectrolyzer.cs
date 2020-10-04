using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class WaterElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public WaterElectrolyzer()
        {
            ActivityName = "Water Electrolysis: H<size=7>2</size>O => H<size=7>2</size> + O<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        private const double ProtiumAtomicMass = 1.00782503207;
        private const double OxygenAtomicMass = 15.999;
        private const double HydrogenMassByFraction = (2 * ProtiumAtomicMass) / (OxygenAtomicMass + (2 * ProtiumAtomicMass)); // 0.1119067
        private const double OxygenMassByFraction = 1 - HydrogenMassByFraction;
        
        private double _waterConsumptionRate;
        private double _hydrogenProductionRate;
        private double _oxygenProductionRate;
        private double _fixedMaxConsumptionWaterRate;
        private double _consumptionStorageRatio;

        private double _waterDensity;
        private double _oxygenDensity;
        private double _hydrogenDensity;

        private double _availableWaterMass;
        private double _spareRoomOxygenMass;
        private double _spareRoomHydrogenMass;

        private double _maxCapacityWaterMass;
        private double _maxCapacityHydrogenMass;
        private double _maxCapacityOxygenMass;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() {  return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).Any(rs => rs.amount > 0);  }

        public string Status => string.Copy(_status);

        public void Initialize(Part part)
        {
            _part = part;

            _vessel = part.vessel;
            _waterDensity = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdOxygen).density;
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;

            // determine how much mass we can produce at max
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / EnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).ToList();
            var partsThatContainOxygen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdOxygen).ToList();
            var partsThatContainHydrogen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen).ToList();

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _waterDensity;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygenDensity;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogenDensity;

            _availableWaterMass = partsThatContainWater.Sum(p => p.amount) * _waterDensity;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygenDensity;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogenDensity;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate * fixedDeltaTime, _availableWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomHydrogenMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * HydrogenMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * OxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real electrolysis
                _waterConsumptionRate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Water, _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _waterDensity) / fixedDeltaTime * _waterDensity;

                var hydrogenRateTemp = _waterConsumptionRate * HydrogenMassByFraction;
                var oxygenRateTemp = _waterConsumptionRate * OxygenMassByFraction;

                _hydrogenProductionRate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, -hydrogenRateTemp * fixedDeltaTime / _hydrogenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogenDensity;
                _oxygenProductionRate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdOxygen, -oxygenRateTemp * fixedDeltaTime / _oxygenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygenDensity;
            }
            else
            {
                _waterConsumptionRate = 0;
                _hydrogenProductionRate = 0;
                _oxygenProductionRate = 0;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_WaterAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Water Available"
            GUILayout.Label(_availableWaterMass.ToString("0.00000") + " mT / " + _maxCapacityWaterMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.00000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_WaterConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Water Consumption Rate"
            GUILayout.Label((_waterConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_HydrogenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Storage"
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_HydrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_hydrogenProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.00000") + " mT / " + _maxCapacityOxygenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygenProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenProductionRate > 0 && _oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg1");//"Electrolyzing Water"
            else if (_fixedMaxConsumptionWaterRate <= 0.0000000001)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg2");//"Out of water"
            else if (_hydrogenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg3", InterstellarResourcesConfiguration.Instance.LqdOxygen);//"Insufficient " +  + " Storage"
            else if (_oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg3", InterstellarResourcesConfiguration.Instance.Hydrogen);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources() 
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_WaterElectroliser_Postmsg") +" " + InterstellarResourcesConfiguration.Instance.Water, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
