using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class CarbonDioxideElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public CarbonDioxideElectrolyzer()
        {
            ActivityName = "CarbonDioxide Electrolysis";
            Formula = "CO<size=7>2</size> => CO + O<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        private const double CarbonMonoxideMassByFraction = 28.010 / (28.010 + 15.999);
        private const double OxygenMassByFraction = 1 - CarbonMonoxideMassByFraction;

        private double _fixedMaxConsumptionDioxideRate;
        private double _consumptionStorageRatio;

        private double _dioxideConsumptionRate;
        private double _monoxideProductionRate;
        private double _oxygenProductionRate;

        private string _dioxideResourceName;
        private string _oxygenResourceName;
        private string _monoxideResourceName;

        private double _dioxideDensity;
        private double _oxygenDensity;
        private double _monoxideDensity;

        private double _availableDioxideMass;
        private double _spareRoomOxygenMass;
        private double _spareRoomMonoxideMass;

        private double _maxCapacityDioxideMass;
        private double _maxCapacityMonoxideMass;
        private double _maxCapacityOxygenMass;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() { return _part.GetConnectedResources(_dioxideResourceName).Any(rs => rs.amount > 0);  }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _dioxideResourceName = ResourceSettings.Config.CarbonDioxideLqd;
            _oxygenResourceName = ResourceSettings.Config.OxygenLqd;
            _monoxideResourceName = ResourceSettings.Config.CarbonMonoxideGas;

            _dioxideDensity = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
            _monoxideDensity = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName).ToList();
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName).ToList();
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName).ToList();

            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxideDensity;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygenDensity;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxideDensity;

            _availableDioxideMass = partsThatContainDioxide.Sum(p => p.amount) * _dioxideDensity;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygenDensity;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxideDensity;

            // determine how much carbon dioxide we can consume
            _fixedMaxConsumptionDioxideRate = Math.Min(_current_rate * fixedDeltaTime, _availableDioxideMass);

            if (_fixedMaxConsumptionDioxideRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomMonoxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxMonoxideRate = _fixedMaxConsumptionDioxideRate * CarbonMonoxideMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionDioxideRate * OxygenMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleMonoxideRatio = fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real electrolysis
                _dioxideConsumptionRate = _part.RequestResource(_dioxideResourceName, _consumptionStorageRatio * _fixedMaxConsumptionDioxideRate / _dioxideDensity) / fixedDeltaTime * _dioxideDensity;

                var monoxideRateTemp = _dioxideConsumptionRate * CarbonMonoxideMassByFraction;
                var oxygenRateTemp = _dioxideConsumptionRate * OxygenMassByFraction;

                _monoxideProductionRate = -_part.RequestResource(_monoxideResourceName, -monoxideRateTemp * fixedDeltaTime / _monoxideDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxideDensity;
                _oxygenProductionRate = -_part.RequestResource(_oxygenResourceName, -oxygenRateTemp * fixedDeltaTime / _oxygenDensity, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygenDensity;
            }
            else
            {
                _dioxideConsumptionRate = 0;
                _monoxideProductionRate = 0;
                _oxygenProductionRate = 0;
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
            GUILayout.Label((_dioxideConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_CarbonMonoxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Storage"
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.00000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_CarbonMonoxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Production Rate"
            GUILayout.Label((_monoxideProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygenProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_monoxideProductionRate > 0 && _oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg1");//"Electrolysing CarbonDioxide"
            else if (_fixedMaxConsumptionDioxideRate <= 0.0000000001)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg2");//"Out of CarbonDioxide"
            else if (_monoxideProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg3", _oxygenResourceName);//"Insufficient " +  + " Storage"
            else if (_oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg3", _monoxideResourceName);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_CarbonDioxideElectroliser_Postmsg") + " " + ResourceSettings.Config.CarbonDioxideLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
