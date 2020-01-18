using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class HeavyWaterElectroliser : RefineryActivityBase, IRefineryActivity
    {
        const double deuteriumAtomicMass = 2.01410178;
        const double oxygenAtomicMass = 15.999;
        const double deuteuriuumMassByFraction = (2 * deuteriumAtomicMass) / (oxygenAtomicMass + (2 * deuteriumAtomicMass)); // 0.201136
        const double oxygenMassByFraction = 1 - deuteuriuumMassByFraction;
        
        double _heavy_water_consumption_rate;
        double _deuterium_production_rate;
        double _oxygen_production_rate;
        double _fixedMaxConsumptionWaterRate;
        double _consumptionStorageRatio;

        double _heavy_water_density;
        double _oxygen_density;
        double _deuterium_density;

        double _availableHeavyWaterMass;
        double _spareRoomOxygenMass;
        double _spareRoomDeuteriumMass;

        double _maxCapacityHeavyWaterMass;
        double _maxCapacityDeuteriumMass;
        double _maxCapacityOxygenMass;

        public RefineryType RefineryType { get { return RefineryType.electrolysis; } }

        public String ActivityName { get { return "Heavy Water Electrolysis"; } }

        public bool HasActivityRequirements() {  return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.HeavyWater).Any(rs => rs.amount > 0);   }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            _part = part;

            _vessel = part.vessel;
            _heavy_water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HeavyWater).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdOxygen).density;
            _deuterium_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdDeuterium).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.HeavyWater);
            var partsThatContainOxygen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdOxygen);
            var partsThatContainDeuteurium = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdDeuterium);

            _maxCapacityHeavyWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _heavy_water_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityDeuteriumMass = partsThatContainDeuteurium.Sum(p => p.maxAmount) * _deuterium_density;

            _availableHeavyWaterMass = partsThatContainWater.Sum(p => p.amount) * _heavy_water_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomDeuteriumMass = partsThatContainDeuteurium.Sum(r => r.maxAmount - r.amount) * _deuterium_density;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate * fixedDeltaTime, _availableHeavyWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomDeuteriumMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * deuteuriuumMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * oxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real elextrolysis
                _heavy_water_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.HeavyWater, _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _heavy_water_density) / fixedDeltaTime * _heavy_water_density;

                var deuterium_rate_temp = _heavy_water_consumption_rate * deuteuriuumMassByFraction;
                var oxygen_rate_temp = _heavy_water_consumption_rate * oxygenMassByFraction;

                _deuterium_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, -deuterium_rate_temp * fixedDeltaTime / _deuterium_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium_density;
                _oxygen_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdOxygen, -oxygen_rate_temp * fixedDeltaTime / _oxygen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _heavy_water_consumption_rate = 0;
                _deuterium_production_rate = 0;
                _oxygen_production_rate = 0;
            }

            updateStatusMessage();
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
            GUILayout.Label((_heavy_water_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_DeuteriumStorage"), _bold_label, GUILayout.Width(labelWidth));//"Deuterium Storage"
            GUILayout.Label(_spareRoomDeuteriumMass.ToString("0.00000") + " mT / " + _maxCapacityDeuteriumMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_DeuteriumProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Deuterium Production Rate"
            GUILayout.Label((_deuterium_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygen_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_deuterium_production_rate > 0 && _oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg1");//"Electrolysing Water"
            else if (_fixedMaxConsumptionWaterRate <= 0.0000000001)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg2");//"Out of water"
            else if (_deuterium_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg3", InterstellarResourcesConfiguration.Instance.LqdOxygen);//"Insufficient " +  + " Storage"
            else if (_oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg3", InterstellarResourcesConfiguration.Instance.Hydrogen);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.HeavyWater, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
