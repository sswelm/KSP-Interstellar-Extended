using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class CarbonDioxideElectroliser : RefineryActivity, IRefineryActivity
    {
        public CarbonDioxideElectroliser()
        {
            ActivityName = "CarbonDioxide Electrolysis: CO<size=7>2</size> => CO + O<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
        }

        const double carbonMonoxideMassByFraction = 28.010 / (28.010 + 15.999);
        const double oxygenMassByFraction = 1 - carbonMonoxideMassByFraction;

        double _fixedMaxConsumptionDioxideRate;
        double _consumptionStorageRatio;

        double _dioxide_consumption_rate;
        double _monoxide_production_rate;
        double _oxygen_production_rate;

        string _dioxideResourceName;
        string _oxygenResourceName;
        string _monoxideResourceName;

        double _dioxide_density;
        double _oxygen_density;
        double _monoxide_density;

        double _availableDioxideMass;
        double _spareRoomOxygenMass;
        double _spareRoomMonoxideMass;

        double _maxCapacityDioxideMass;
        double _maxCapacityMonoxideMass;
        double _maxCapacityOxygenMass;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() { return _part.GetConnectedResources(_dioxideResourceName).Any(rs => rs.amount > 0);  }

        public string Status => string.Copy(_status);

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _dioxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _oxygenResourceName = InterstellarResourcesConfiguration.Instance.LqdOxygen;
            _monoxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            
            _dioxide_density = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName);
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName);

            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;

            _availableDioxideMass = partsThatContainDioxide.Sum(p => p.amount) * _dioxide_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;

            // determine how much carbondioxide we can consume
            _fixedMaxConsumptionDioxideRate = Math.Min(_current_rate * fixedDeltaTime, _availableDioxideMass);

            if (_fixedMaxConsumptionDioxideRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomMonoxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxMonoxideRate = _fixedMaxConsumptionDioxideRate * carbonMonoxideMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionDioxideRate * oxygenMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleMonoxideRatio = fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real elextrolysis
                _dioxide_consumption_rate = _part.RequestResource(_dioxideResourceName, _consumptionStorageRatio * _fixedMaxConsumptionDioxideRate / _dioxide_density) / fixedDeltaTime * _dioxide_density;

                var monoxide_rate_temp = _dioxide_consumption_rate * carbonMonoxideMassByFraction;
                var oxygen_rate_temp = _dioxide_consumption_rate * oxygenMassByFraction;

                _monoxide_production_rate = -_part.RequestResource(_monoxideResourceName, -monoxide_rate_temp * fixedDeltaTime / _monoxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_density;
                _oxygen_production_rate = -_part.RequestResource(_oxygenResourceName, -oxygen_rate_temp * fixedDeltaTime / _oxygen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _dioxide_consumption_rate = 0;
                _monoxide_production_rate = 0;
                _oxygen_production_rate = 0;
            }

            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_CarbonDioxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Available"
            GUILayout.Label(_availableDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_CarbonDioxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Consumption Rate"
            GUILayout.Label((_dioxide_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_CarbonMonoxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Storage"
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.00000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_CarbonMonoxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Production Rate"
            GUILayout.Label((_monoxide_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygen_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_monoxide_production_rate > 0 && _oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg1");//"Electrolysing CarbonDioxide"
            else if (_fixedMaxConsumptionDioxideRate <= 0.0000000001)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg2");//"Out of CarbonDioxide"
            else if (_monoxide_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg3", _oxygenResourceName);//"Insufficient " +  + " Storage"
            else if (_oxygen_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg3", _monoxideResourceName);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.CarbonDioxide, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
