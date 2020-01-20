using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class WaterGasShift : RefineryActivityBase, IRefineryActivity 
    {      
        const double waterMassByFraction = 18.01528 / (18.01528 + 28.010);
        const double monoxideMassByFraction = 1 - waterMassByFraction;

        const double hydrogenMassByFraction = (2 * 1.008) / (44.01 + (2 * 1.008));
        const double dioxideMassByFraction = 1 - hydrogenMassByFraction;
       
        double _fixedConsumptionRate;
        double _consumptionStorageRatio;

        double _water_consumption_rate;
        double _monoxide_consumption_rate;
        double _hydrogen_production_rate;
        double _dioxide_production_rate;

        string _waterResourceName;
        string _monoxideResourceName;
        string _dioxideResourceName;
        string _hydrogenResourceName;

        double _water_density;
        double _dioxide_density;
        double _hydrogen_density;
        double _monoxide_density;

        double _availableWaterMass;
        double _availableMonoxideMass;
        double _spareRoomDioxideMass;
        double _spareRoomHydrogenMass;

        double _maxCapacityWaterMass;
        double _maxCapacityDioxideMass;
        double _maxCapacityMonoxideMass;
        double _maxCapacityHydrogenMass;

        public RefineryType RefineryType { get { return RefineryType.synthesize; } }

        public String ActivityName { get { return "Water Gas Shift: H<size=7>2</size>0 + CO => CO<size=7>2</size> + H<size=7>2</size>"; } }

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_waterResourceName).Any(rs => rs.amount > 0) && _part.GetConnectedResources(_monoxideResourceName).Any(rs => rs.amount > 0);
        }

        public double PowerRequirements { get { return PluginHelper.BaseHaberProcessPowerConsumption * 5; } }

        public String Status { get { return String.Copy(_status); } }

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

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _allowOverflow = allowOverflow;
            
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.HaberProcessEnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(_waterResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName);
            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName);

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;
            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;

            _availableWaterMass = partsThatContainWater.Sum(r => r.amount) * _water_density;
            _availableMonoxideMass = partsThatContainMonoxide.Sum(r => r.amount) * _monoxide_density;
            _spareRoomDioxideMass = partsThatContainDioxide.Sum(r => r.maxAmount - r.amount) * _dioxide_density;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen_density;

            // determine how much carbondioxide we can consume
            var fixedMaxWaterConsumptionRate = _current_rate * waterMassByFraction * fixedDeltaTime;
            var waterConsumptionRatio = fixedMaxWaterConsumptionRate > 0 ? Math.Min(fixedMaxWaterConsumptionRate, _availableWaterMass) / fixedMaxWaterConsumptionRate : 0;

            var fixedMaxMonoxideConsumptionRate =  _current_rate * monoxideMassByFraction * fixedDeltaTime;
            var monoxideConsumptionRatio = fixedMaxMonoxideConsumptionRate > 0 ? Math.Min(fixedMaxMonoxideConsumptionRate, _availableMonoxideMass) / fixedMaxMonoxideConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(waterConsumptionRatio, monoxideConsumptionRatio);

            if (_fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomDioxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedConsumptionRate * hydrogenMassByFraction;
                var fixedMaxDioxideRate = _fixedConsumptionRate * dioxideMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleDioxideRate = allowOverflow ? fixedMaxDioxideRate : Math.Min(_spareRoomDioxideMass, fixedMaxDioxideRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate, fixedMaxPossibleDioxideRate / fixedMaxDioxideRate);

                // now we do the real elextrolysis
                _water_consumption_rate = _part.RequestResource(_waterResourceName, waterMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _water_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _water_density;
                _monoxide_consumption_rate = _part.RequestResource(_monoxideResourceName, monoxideMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _monoxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_density;
                var combined_consumption_rate = _water_consumption_rate + _monoxide_consumption_rate;

                var hydrogen_rate_temp = combined_consumption_rate * hydrogenMassByFraction;
                var dioxide_rate_temp = combined_consumption_rate * dioxideMassByFraction;

                _hydrogen_production_rate = -_part.RequestResource(_hydrogenResourceName, -hydrogen_rate_temp * fixedDeltaTime / _hydrogen_density) / fixedDeltaTime * _hydrogen_density;
                _dioxide_production_rate = -_part.RequestResource(_dioxideResourceName, -dioxide_rate_temp * fixedDeltaTime / _dioxide_density) / fixedDeltaTime * _dioxide_density;
            }
            else
            {
                _water_consumption_rate = 0;
                _monoxide_consumption_rate = 0;
                _hydrogen_production_rate = 0;
                _dioxide_production_rate = 0;
            }

            updateStatusMessage();
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
            GUILayout.Label((_water_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonMonoxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Available"
            GUILayout.Label(_availableMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonMonoxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Consumption Rate"
            GUILayout.Label((_monoxide_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonDioxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Storage"
            GUILayout.Label(_spareRoomDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonDioxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Production Rate"
            GUILayout.Label((_dioxide_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_HydrogenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Storage"
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_HydrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_dioxide_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_rate > 0 && _dioxide_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg1");//"Water Gas Swifting"
            else if (_fixedConsumptionRate <= 0.0000000001)
            {
                if (_availableWaterMass <= 0.0000000001)
                    _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg2");//"Out of Water"
                else
                    _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg3");//"Out of CarbonMonoxide"
            }
            else if (_hydrogen_production_rate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg5", _dioxideResourceName);//"Overflowing ""Insufficient " +  + " Storage"
            else if (_dioxide_production_rate > 0)
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
