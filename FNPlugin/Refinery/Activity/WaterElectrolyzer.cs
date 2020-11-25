using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class WaterElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public WaterElectrolyzer()
        {
            ActivityName = "Water Electrolysis";
            Formula = "H<size=7>2</size>O => H<size=7>2</size> + O<size=7>2</size>";
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

        private PartResourceDefinition _water;
        private PartResourceDefinition _lqdWater;
        private PartResourceDefinition _oxygen;
        private PartResourceDefinition _hydrogen;

        private double _availableWaterMass;
        private double _availableLqdWaterMass;
        private double _spareRoomOxygenMass;
        private double _spareRoomHydrogenMass;

        private double _maxCapacityWaterMass;
        private double _maxCapacityHydrogenMass;
        private double _maxCapacityOxygenMass;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_water.name).Any(rs => rs.amount > 0)
                   || _part.GetConnectedResources(_lqdWater.name).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;

            _vessel = localPart.vessel;
            _water = PartResourceLibrary.Instance.GetDefinition("Water");
            _lqdWater = PartResourceLibrary.Instance.GetDefinition("LqdWater");
            _oxygen = PartResourceLibrary.Instance.GetDefinition("Oxygen");
            _hydrogen = PartResourceLibrary.Instance.GetDefinition("Hydrogen");
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;

            // determine how much mass we can produce at max
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / EnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(_water.name).ToList();
            var partsThatContainLqdWater = _part.GetConnectedResources(_lqdWater.name).ToList();
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygen.name).ToList();
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogen.name).ToList();

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water.density
                                    + partsThatContainLqdWater.Sum(p => p.maxAmount) * _lqdWater.density;

            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen.density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen.density;

            _availableWaterMass = partsThatContainWater.Sum(p => p.amount) * _water.density;
            _availableLqdWaterMass = partsThatContainWater.Sum(p => p.amount) * _lqdWater.density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen.density;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen.density;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate * fixedDeltaTime, allowOverflow? _availableWaterMass + _availableLqdWaterMass : _availableLqdWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 && _spareRoomHydrogenMass > 0
                                                      || allowOverflow && (_spareRoomOxygenMass > 0 || _spareRoomHydrogenMass > 0)) )
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * HydrogenMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * OxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                //  consume lqdWater before we consume drinking water
                var waterRequested = _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _lqdWater.density;
                var fixedWaterConsumptionRate = _part.RequestResource(_lqdWater.name, waterRequested) * _lqdWater.density;
                if (fixedWaterConsumptionRate < _fixedMaxConsumptionWaterRate)
                {
                    var lqdWaterRequested = _consumptionStorageRatio * (_fixedMaxConsumptionWaterRate - fixedWaterConsumptionRate) / _water.density;
                    fixedWaterConsumptionRate += _part.RequestResource(_water.name, lqdWaterRequested) * _water.density;
                }

                // now we do the real electrolysis
                _waterConsumptionRate = fixedWaterConsumptionRate / fixedDeltaTime;
                var hydrogenRateTemp = _waterConsumptionRate * HydrogenMassByFraction;
                var oxygenRateTemp = _waterConsumptionRate * OxygenMassByFraction;

                var hydrogenProductionAmount = -_part.RequestResource(
                    resourceName: ResourcesConfiguration.Instance.HydrogenLqd,
                    demand: -hydrogenRateTemp * fixedDeltaTime / _hydrogen.density,
                    flowMode: ResourceFlowMode.ALL_VESSEL);

                var oxygenProductionAmount = -_part.RequestResource(
                    resourceName: ResourcesConfiguration.Instance.LqdOxygen,
                    demand: -oxygenRateTemp * fixedDeltaTime / _oxygen.density,
                    flowMode: ResourceFlowMode.ALL_VESSEL);

                _hydrogenProductionRate = hydrogenProductionAmount / fixedDeltaTime * _hydrogen.density;
                _oxygenProductionRate = oxygenProductionAmount / fixedDeltaTime * _oxygen.density;
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
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg3", _oxygen.name);//"Insufficient " +  + " Storage"
            else if (_oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg3", _hydrogen.name);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_WaterElectroliser_Postmsg") +" " + _water.name, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
