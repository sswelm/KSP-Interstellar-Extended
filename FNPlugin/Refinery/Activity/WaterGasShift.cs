using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class WaterGasShift : RefineryActivity, IRefineryActivity 
    {
        public WaterGasShift()
        {
            ActivityName = "Water Gas Shift";
            Formula = "H<size=7>2</size>0 + CO => CO<size=7>2</size> + H<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseHaberProcessPowerConsumption * 5;
            EnergyPerTon = PluginHelper.HaberProcessEnergyPerTon;
        }

        private const double WaterMassByFraction = 18.01528 / (18.01528 + 28.010);
        private const double MonoxideMassByFraction = 1 - WaterMassByFraction;
        private const double HydrogenMassByFraction = (2 * 1.008) / (44.01 + (2 * 1.008));
        private const double DioxideMassByFraction = 1 - HydrogenMassByFraction;

        private double _fixedConsumptionRate;
        private double _consumptionStorageRatio;

        private double _waterConsumptionRate;
        private double _monoxideConsumptionRate;
        private double _hydrogenProductionRate;
        private double _dioxideProductionRate;

        private string _waterResourceName;
        private string _monoxideResourceName;
        private string _dioxideResourceName;
        private string _hydrogenResourceName;

        private double _waterDensity;
        private double _dioxideDensity;
        private double _hydrogenDensity;
        private double _monoxideDensity;

        private double _availableWaterMass;
        private double _availableMonoxideMass;
        private double _spareRoomDioxideMass;
        private double _spareRoomHydrogenMass;

        private double _maxCapacityWaterMass;
        private double _maxCapacityDioxideMass;
        private double _maxCapacityMonoxideMass;
        private double _maxCapacityHydrogenMass;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_waterResourceName).Any(rs => rs.amount > 0) && _part.GetConnectedResources(_monoxideResourceName).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _waterResourceName = InterstellarResourcesConfiguration.Instance.Water;
            _monoxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            _dioxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _hydrogenResourceName = InterstellarResourcesConfiguration.Instance.Hydrogen;

            _waterDensity = PartResourceLibrary.Instance.GetDefinition(_waterResourceName).density;
            _dioxideDensity = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName).density;
            _monoxideDensity = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _allowOverflow = allowOverflow;
            
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(_waterResourceName).ToList();
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName).ToList();
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName).ToList();
            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName).ToList();

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _waterDensity;
            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxideDensity;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogenDensity;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxideDensity;

            _availableWaterMass = partsThatContainWater.Sum(r => r.amount) * _waterDensity;
            _availableMonoxideMass = partsThatContainMonoxide.Sum(r => r.amount) * _monoxideDensity;
            _spareRoomDioxideMass = partsThatContainDioxide.Sum(r => r.maxAmount - r.amount) * _dioxideDensity;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogenDensity;

            // determine how much carbon dioxide we can consume
            var fixedMaxWaterConsumptionRate = _current_rate * WaterMassByFraction * fixedDeltaTime;
            var waterConsumptionRatio = fixedMaxWaterConsumptionRate > 0 ? Math.Min(fixedMaxWaterConsumptionRate, _availableWaterMass) / fixedMaxWaterConsumptionRate : 0;

            var fixedMaxMonoxideConsumptionRate =  _current_rate * MonoxideMassByFraction * fixedDeltaTime;
            var monoxideConsumptionRatio = fixedMaxMonoxideConsumptionRate > 0 ? Math.Min(fixedMaxMonoxideConsumptionRate, _availableMonoxideMass) / fixedMaxMonoxideConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(waterConsumptionRatio, monoxideConsumptionRatio);

            if (_fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomDioxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedConsumptionRate * HydrogenMassByFraction;
                var fixedMaxDioxideRate = _fixedConsumptionRate * DioxideMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleDioxideRate = allowOverflow ? fixedMaxDioxideRate : Math.Min(_spareRoomDioxideMass, fixedMaxDioxideRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate, fixedMaxPossibleDioxideRate / fixedMaxDioxideRate);

                // now we do the real electrolysis
                _waterConsumptionRate = _part.RequestResource(_waterResourceName, WaterMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _waterDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _waterDensity;
                _monoxideConsumptionRate = _part.RequestResource(_monoxideResourceName, MonoxideMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _monoxideDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxideDensity;
                var combinedConsumptionRate = _waterConsumptionRate + _monoxideConsumptionRate;

                var hydrogenRateTemp = combinedConsumptionRate * HydrogenMassByFraction;
                var dioxideRateTemp = combinedConsumptionRate * DioxideMassByFraction;

                _hydrogenProductionRate = -_part.RequestResource(_hydrogenResourceName, -hydrogenRateTemp * fixedDeltaTime / _hydrogenDensity) / fixedDeltaTime * _hydrogenDensity;
                _dioxideProductionRate = -_part.RequestResource(_dioxideResourceName, -dioxideRateTemp * fixedDeltaTime / _dioxideDensity) / fixedDeltaTime * _dioxideDensity;
            }
            else
            {
                _waterConsumptionRate = 0;
                _monoxideConsumptionRate = 0;
                _hydrogenProductionRate = 0;
                _dioxideProductionRate = 0;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_Consumption"), _bold_label, GUILayout.Width(labelWidth));//"Current Consumption"
            GUILayout.Label(((_fixedConsumptionRate / TimeWarp.fixedDeltaTime * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_WaterAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Water Available"
            GUILayout.Label(_availableWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Water Consumption Rate"
            GUILayout.Label((_waterConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonMonoxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Available"
            GUILayout.Label(_availableMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonMonoxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Consumption Rate"
            GUILayout.Label((_monoxideConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonDioxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Storage"
            GUILayout.Label(_spareRoomDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonDioxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Production Rate"
            GUILayout.Label((_dioxideProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_HydrogenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Storage"
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_HydrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_dioxideProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenProductionRate > 0 && _dioxideProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg1");//"Water Gas Swifting"
            else if (_fixedConsumptionRate <= 0.0000000001)
            {
                if (_availableWaterMass <= 0.0000000001)
                    _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg2");//"Out of Water"
                else
                    _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg3");//"Out of CarbonMonoxide"
            }
            else if (_hydrogenProductionRate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg5", _dioxideResourceName);//"Overflowing ""Insufficient " +  + " Storage"
            else if (_dioxideProductionRate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg5", _hydrogenResourceName);//"Overflowing ""Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg6");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg7");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_WaterGasShift_Postmsg") +" " + InterstellarResourcesConfiguration.Instance.Water, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.CarbonMoxoxide).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_WaterGasShift_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.CarbonMoxoxide, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
