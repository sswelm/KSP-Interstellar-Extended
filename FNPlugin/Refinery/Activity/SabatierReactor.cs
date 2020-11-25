using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class SabatierReactor : RefineryActivity, IRefineryActivity
    {
        public SabatierReactor()
        {
            ActivityName = "Sabatier Process";
            Formula = "CO<size=7>2</size> + H<size=7>2</size> => O<size=7>2</size> + CH<size=7>4</size> (Methane)";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        private const double CarbonDioxideMassByFraction = 44.01 / (44.01 + (8 * 1.008));
        private const double HydrogenMassByFraction = (8 * 1.008) / (44.01 + (8 * 1.008));
        private const double OxygenMassByFraction = 32.0 / 52.0;
        private const double MethaneMassByFraction = 20.0 / 52.0;

        private double _fixedConsumptionRate;

        private double _carbonDioxideDensity;
        private double _methaneDensity;
        private double _hydrogenDensity;
        private double _oxygenDensity;

        private double _hydrogenConsumptionRate;
        private double _carbonDioxideConsumptionRate;

        private double _methaneProductionRate;
        private double _oxygenProductionRate;

        private string _carbonDioxideResourceName;
        private string _methaneResourceName;
        private string _hydrogenResourceName;
        private string _oxygenResourceName;

        private double _maxCapacityCarbonDioxideMass;
        private double _maxCapacityHydrogenMass;
        private double _maxCapacityMethaneMass;
        private double _maxCapacityOxygenMass;

        private double _availableCarbonDioxideMass;
        private double _availableHydrogenMass;
        private double _spareRoomMethaneMass;
        private double _spareRoomOxygenMass;

        private double _fixedCombinedConsumptionRate;
        private double _combinedConsumptionRate;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_hydrogenResourceName).Any(rs => rs.amount > 0) &
                _part.GetConnectedResources(_carbonDioxideResourceName).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _carbonDioxideResourceName = ResourcesConfiguration.Instance.CarbonDioxideLqd;
            _hydrogenResourceName = ResourcesConfiguration.Instance.HydrogenLqd;
            _methaneResourceName = ResourcesConfiguration.Instance.Methane;
            _oxygenResourceName = ResourcesConfiguration.Instance.LqdOxygen;

            _carbonDioxideDensity = PartResourceLibrary.Instance.GetDefinition(_carbonDioxideResourceName).density;
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName).density;
            _methaneDensity = PartResourceLibrary.Instance.GetDefinition(_methaneResourceName).density;
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine how much resource we have
            var partsThatContainCarbonDioxide = _part.GetConnectedResources(_carbonDioxideResourceName).ToList();
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName).ToList();
            var partsThatContainMethane = _part.GetConnectedResources(_methaneResourceName).ToList();
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName).ToList();

            _maxCapacityCarbonDioxideMass = partsThatContainCarbonDioxide.Sum(p => p.maxAmount) * _carbonDioxideDensity;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogenDensity;
            _maxCapacityMethaneMass = partsThatContainMethane.Sum(p => p.maxAmount) * _methaneDensity;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygenDensity;

            _availableCarbonDioxideMass = partsThatContainCarbonDioxide.Sum(r => r.amount) * _carbonDioxideDensity;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogenDensity;
            _spareRoomMethaneMass = partsThatContainMethane.Sum(r => r.maxAmount - r.amount) * _methaneDensity;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygenDensity;

            var fixedMaxCarbonDioxideConsumptionRate = _current_rate * CarbonDioxideMassByFraction * fixedDeltaTime;
            var carbonDioxideConsumptionRatio = fixedMaxCarbonDioxideConsumptionRate > 0
                ? Math.Min(fixedMaxCarbonDioxideConsumptionRate, _availableCarbonDioxideMass) / fixedMaxCarbonDioxideConsumptionRate
                : 0;

            var fixedMaxHydrogenConsumptionRate = _current_rate * HydrogenMassByFraction * fixedDeltaTime;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(carbonDioxideConsumptionRatio, hydrogenConsumptionRatio);

            if (_fixedConsumptionRate > 0 && _spareRoomMethaneMass > 0)
            {
                var fixedMaxPossibleProductionRate = Math.Min(_spareRoomMethaneMass, _fixedConsumptionRate);

                var carbonDioxideConsumptionRate = fixedMaxPossibleProductionRate * CarbonDioxideMassByFraction;
                var hydrogenConsumptionRate = fixedMaxPossibleProductionRate * HydrogenMassByFraction;

                // consume the resource
                _hydrogenConsumptionRate = _part.RequestResource(_hydrogenResourceName, hydrogenConsumptionRate / _hydrogenDensity) / fixedDeltaTime * _hydrogenDensity;
                _carbonDioxideConsumptionRate = _part.RequestResource(_carbonDioxideResourceName, carbonDioxideConsumptionRate / _carbonDioxideDensity) / fixedDeltaTime * _carbonDioxideDensity;

                _fixedCombinedConsumptionRate = _hydrogenConsumptionRate + _carbonDioxideConsumptionRate;
                _combinedConsumptionRate = _fixedCombinedConsumptionRate / fixedDeltaTime;

                var fixedMethaneProduction = _fixedCombinedConsumptionRate * MethaneMassByFraction * fixedDeltaTime / _methaneDensity;
                var fixedOxygenProduction = _fixedCombinedConsumptionRate * OxygenMassByFraction * fixedDeltaTime / _oxygenDensity;

                _methaneProductionRate = -_part.RequestResource(_methaneResourceName, -fixedMethaneProduction) / fixedDeltaTime * _methaneDensity;
                _oxygenProductionRate = -_part.RequestResource(_oxygenResourceName, -fixedOxygenProduction) / fixedDeltaTime * _oxygenDensity;
            }
            else
            {
                _hydrogenConsumptionRate = 0;
                _carbonDioxideConsumptionRate = 0;
                _methaneProductionRate = 0;
            }
            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_Consumption"), _bold_label, GUILayout.Width(labelWidth));//"Overall Consumption"
            GUILayout.Label(((_combinedConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_CarbonDioxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Carbon Dioxide Available"
            GUILayout.Label(_availableCarbonDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityCarbonDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_CarbonDioxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Carbon Dioxide Consumption Rate"
            GUILayout.Label((_carbonDioxideConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_HydrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Available"
            GUILayout.Label(_availableHydrogenMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_HydrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Consumption Rate"
            GUILayout.Label((_hydrogenConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_MethaneStorage"), _bold_label, GUILayout.Width(labelWidth));//"Methane Storage"
            GUILayout.Label(_spareRoomMethaneMass.ToString("0.0000") + " mT / " + _maxCapacityMethaneMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_MethaneProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Methane Production Rate"
            GUILayout.Label((_methaneProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygenProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_methaneProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_SabatierReactor_Statumsg1");//"Sabatier Process Ongoing"
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_SabatierReactor_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_SabatierReactor_Statumsg3");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(ResourcesConfiguration.Instance.CarbonDioxideLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SabatierReactor_Postmsg") + " " + ResourcesConfiguration.Instance.CarbonDioxideLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(ResourcesConfiguration.Instance.HydrogenLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SabatierReactor_Postmsg") + " " + ResourcesConfiguration.Instance.HydrogenLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
