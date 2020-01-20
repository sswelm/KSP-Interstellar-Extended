using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class WaterElectroliser : RefineryActivityBase, IRefineryActivity
    {
        const double protiumAtomicMass = 1.00782503207;
        const double oxygenAtomicMass = 15.999;
        const double hydrogenMassByFraction = (2 * protiumAtomicMass) / (oxygenAtomicMass + (2 * protiumAtomicMass)); // 0.1119067
        const double oxygenMassByFraction = 1 - hydrogenMassByFraction;
        
        double _water_consumption_rate;
        double _hydrogen_production_rate;
        double _oxygen_production_rate;
        double _fixedMaxConsumptionWaterRate;
        double _consumptionStorageRatio;

        double _water_density;
        double _oxygen_density;
        double _hydrogen_density;

        double _availableWaterMass;
        double _spareRoomOxygenMass;
        double _spareRoomHydrogenMass;

        double _maxCapacityWaterMass;
        double _maxCapacityHydrogenMass;
        double _maxCapacityOxygenMass;

        public RefineryType RefineryType { get { return RefineryType.electrolysis; } }

        public String ActivityName { get { return "Water Electrolysis: H<size=7>2</size>O => H<size=7>2</size> + O<size=7>2</size>"; } }

        public bool HasActivityRequirements() {  return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).Any(rs => rs.amount > 0);  }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            _part = part;

            _vessel = part.vessel;
            _water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdOxygen).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPower = productionModidier * PowerRequirements;

            // determine how much mass we can produce at max
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water);
            var partsThatContainOxygen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdOxygen);
            var partsThatContainHydrogen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen);

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;

            _availableWaterMass = partsThatContainWater.Sum(p => p.amount) * _water_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen_density;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate * fixedDeltaTime, _availableWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomHydrogenMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * hydrogenMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * oxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real elextrolysis
                _water_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Water, _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _water_density) / fixedDeltaTime * _water_density;

                var hydrogen_rate_temp = _water_consumption_rate * hydrogenMassByFraction;
                var oxygen_rate_temp = _water_consumption_rate * oxygenMassByFraction;

                _hydrogen_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, -hydrogen_rate_temp * fixedDeltaTime / _hydrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen_density;
                _oxygen_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdOxygen, -oxygen_rate_temp * fixedDeltaTime / _oxygen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _water_consumption_rate = 0;
                _hydrogen_production_rate = 0;
                _oxygen_production_rate = 0;
            }

            updateStatusMessage();
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
            GUILayout.Label((_water_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_HydrogenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Storage"
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_HydrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_hydrogen_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.00000") + " mT / " + _maxCapacityOxygenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterElectroliser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygen_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_rate > 0 && _oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg1");//"Electrolysing Water"
            else if (_fixedMaxConsumptionWaterRate <= 0.0000000001)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg2");//"Out of water"
            else if (_hydrogen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg3", InterstellarResourcesConfiguration.Instance.LqdOxygen);//"Insufficient " +  + " Storage"
            else if (_oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg3", InterstellarResourcesConfiguration.Instance.Hydrogen);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_WaterElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources() {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_WaterElectroliser_Postmsg") +" " + InterstellarResourcesConfiguration.Instance.Water, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
