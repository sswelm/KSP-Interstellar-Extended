using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class ReverseWaterGasShift : RefineryActivity, IRefineryActivity
    {
        public ReverseWaterGasShift()
        {
            ActivityName = "Reverse Water Gas Shift";
            PowerRequirements = PluginHelper.BaseHaberProcessPowerConsumption * 5;
        }

        const double waterMassByFraction = 18.01528 / (18.01528 + 28.010);
        const double monoxideMassByFraction = 1 - waterMassByFraction;

        const double hydrogenMassByFraction = (2 * 1.008) / (44.01 + (2 * 1.008));
        const double dioxideMassByFraction = 1 - hydrogenMassByFraction;
        
        double _fixedConsumptionRate;
        double _consumptionRate;
        double _consumptionStorageRatio;

        double _dioxide_consumption_rate;
        double _hydrogen_consumption_rate;
        double _monoxide_production_rate;
        double _water_production_rate;

        string _waterResourceName;
        string _monoxideResourceName;
        string _dioxideResourceName;
        string _hydrogenResourceName;

        double _water_density;
        double _dioxide_density;
        double _hydrogen_density;
        double _monoxide_density;

        double _availableDioxideMass;
        double _availableHydrogenMass;
        double _spareRoomWaterMass;
        double _spareRoomMonoxideMass;

        double _maxCapacityWaterMass;
        double _maxCapacityDioxideMass;
        double _maxCapacityMonoxideMass;
        double _maxCapacityHydrogenMass;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_dioxideResourceName).Any(rs => rs.amount > 0) &&
                   _part.GetConnectedResources(_hydrogenResourceName).Any(rs => rs.amount > 0);
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

            _water_density = PartResourceLibrary.Instance.GetDefinition(_waterResourceName).density;
            _dioxide_density = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName).density;
            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _allowOverflow = allowOverflow;
            
            // determine overall maximum production rate
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.HaberProcessEnergyPerTon;

            // determine how much resource we have
            var partsThatContainWater = _part.GetConnectedResources(_waterResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName);
            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName);

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;
            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;

            _availableDioxideMass = partsThatContainDioxide.Sum(r => r.amount) * _dioxide_density;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogen_density;

            _spareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _water_density;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;

            // determine how much we can consume
            var fixedMaxDioxideConsumptionRate = _current_rate * dioxideMassByFraction * fixedDeltaTime;
            var dioxideConsumptionRatio = fixedMaxDioxideConsumptionRate > 0 ? Math.Min(fixedMaxDioxideConsumptionRate, _availableDioxideMass) / fixedMaxDioxideConsumptionRate : 0;

            var fixedMaxHydrogenConsumptionRate =  _current_rate * hydrogenMassByFraction * fixedDeltaTime;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(dioxideConsumptionRatio, hydrogenConsumptionRatio);
            _consumptionRate = _fixedConsumptionRate / fixedDeltaTime;

            if (_fixedConsumptionRate > 0 && (_spareRoomMonoxideMass > 0 || _spareRoomWaterMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxMonoxideRate = _fixedConsumptionRate * monoxideMassByFraction;
                var fixedMaxWaterRate = _fixedConsumptionRate * waterMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate, fixedMaxPossibleWaterRate / fixedMaxWaterRate);

                // now we do the real consumption
                _dioxide_consumption_rate = _part.RequestResource(_dioxideResourceName, dioxideMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _dioxide_density) / fixedDeltaTime * _dioxide_density;
                _hydrogen_consumption_rate = _part.RequestResource(_hydrogenResourceName, hydrogenMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _hydrogen_density) / fixedDeltaTime * _hydrogen_density;
                var combined_consumption_rate = _dioxide_consumption_rate + _hydrogen_consumption_rate;

                var monoxide_rate_temp = combined_consumption_rate * monoxideMassByFraction;
                var water_rate_temp = combined_consumption_rate * waterMassByFraction;

                _monoxide_production_rate = -_part.RequestResource(_monoxideResourceName, -monoxide_rate_temp * fixedDeltaTime / _monoxide_density) / fixedDeltaTime * _monoxide_density;
                _water_production_rate = -_part.RequestResource(_waterResourceName, -water_rate_temp * fixedDeltaTime / _water_density) / fixedDeltaTime * _water_density;
            }
            else
            {
                _dioxide_consumption_rate = 0;
                _hydrogen_consumption_rate = 0;
                _monoxide_production_rate = 0;
                _water_production_rate = 0;
            }

            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_CurrentConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Current Consumption"
            GUILayout.Label(((_consumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_CarbonDioxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Available"
            GUILayout.Label(_availableDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_CarbonDioxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Consumption Rate"
            GUILayout.Label((_dioxide_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_HydrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Available"
            GUILayout.Label(_availableHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_HydrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Consumption Rate"
            GUILayout.Label((_hydrogen_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_WaterStorage"), _bold_label, GUILayout.Width(labelWidth));//"Water Storage"
            GUILayout.Label(_spareRoomWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_WaterProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Water Production Rate"
            GUILayout.Label((_water_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_MonoxideMonoxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"MonoxideMonoxide Storage"
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_MonoxideMonoxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"MonoxideMonoxide Production Rate"
            GUILayout.Label((_monoxide_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_monoxide_production_rate > 0 && _water_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg1");//"Water Gas Shifting"
            else if (_fixedConsumptionRate <= 0.0000000001)
            {
                if (_availableDioxideMass <= 0.0000000001)
                    _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg2");//"Out of CarbonDioxide"
                else
                    _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg3");//"Out of Hydrogen"
            }
            else if (_monoxide_production_rate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg5", _waterResourceName);//"Overflowing ""Insufficient " +  + " Storage"
            else if (_water_production_rate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg5", _monoxideResourceName);//"Overflowing ""Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg6");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg7");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.CarbonDioxide).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.CarbonDioxide, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.Hydrogen, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
