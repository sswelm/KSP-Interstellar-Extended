using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class SabatierReactor : RefineryActivityBase, IRefineryActivity
    {
        double _fixedConsumptionRate;

        double _carbondioxide_density;
        double _methane_density;
        double _hydrogen_density;
        double _oxygen_density;
        
        double _hydrogen_consumption_rate;
        double _carbondioxide_consumption_rate;

        double _methane_production_rate;
        double _oxygen_production_rate;

        string _carbondioxide_resource_name;
        string _methane_resource_name;
        string _hydrogen_resource_name;
        string _oxygen_resource_name;

        double _maxCapacityCarbondioxideMass;
        double _maxCapacityHydrogenMass;
        double _maxCapacityMethaneMass;
        double _maxCapacityOxygenMass;

        double _availableCarbondioxideMass;
        double _availableHydrogenMass;
        double _spareRoomMethaneMass;
        double _spareRoomOxygenMass;

        double _carbonDioxideMassByFraction = 44.01 / (44.01 + (8 * 1.008));
        double _hydrogenMassByFraction = (8 * 1.008) / (44.01 + (8 * 1.008));
        double _oxygenMassByFraction = 32.0 / 52.0;
        double _methaneMassByFraction = 20.0 / 52.0;

        private double fixed_combined_consumption_rate;
        private double combined_consumption_rate;
     
        public RefineryType RefineryType { get { return RefineryType.synthesize; } }

        public String ActivityName { get { return "Sabatier Process: CO<size=7>2</size> + H<size=7>2</size> => O<size=7>2</size> + CH<size=7>4</size> (Methane)"; } }

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_hydrogen_resource_name).Any(rs => rs.amount > 0) &
                _part.GetConnectedResources(_carbondioxide_resource_name).Any(rs => rs.amount > 0);
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _carbondioxide_resource_name = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _hydrogen_resource_name = InterstellarResourcesConfiguration.Instance.Hydrogen;
            _methane_resource_name = InterstellarResourcesConfiguration.Instance.Methane;
            _oxygen_resource_name = InterstellarResourcesConfiguration.Instance.LqdOxygen;

            _carbondioxide_density = PartResourceLibrary.Instance.GetDefinition(_carbondioxide_resource_name).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_resource_name).density;
            _methane_density = PartResourceLibrary.Instance.GetDefinition(_methane_resource_name).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(_oxygen_resource_name).density;
        }



        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon; //* _vessel.atmDensity;

            // determine how much resource we have
            var partsThatContainCarbonDioxide = _part.GetConnectedResources(_carbondioxide_resource_name);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogen_resource_name);
            var partsThatContainMethane = _part.GetConnectedResources(_methane_resource_name);
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygen_resource_name);

            _maxCapacityCarbondioxideMass = partsThatContainCarbonDioxide.Sum(p => p.maxAmount) * _carbondioxide_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMethaneMass = partsThatContainMethane.Sum(p => p.maxAmount) * _methane_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;

            _availableCarbondioxideMass = partsThatContainCarbonDioxide.Sum(r => r.amount) * _carbondioxide_density;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogen_density;
            _spareRoomMethaneMass = partsThatContainMethane.Sum(r => r.maxAmount - r.amount) * _methane_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;

            var fixedMaxCarbondioxideConsumptionRate = _current_rate * _carbonDioxideMassByFraction * fixedDeltaTime;
            var carbondioxideConsumptionRatio = fixedMaxCarbondioxideConsumptionRate > 0 
                ? Math.Min(fixedMaxCarbondioxideConsumptionRate, _availableCarbondioxideMass) / fixedMaxCarbondioxideConsumptionRate 
                : 0;

            var fixedMaxHydrogenConsumptionRate = _current_rate * _hydrogenMassByFraction * fixedDeltaTime;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(carbondioxideConsumptionRatio, hydrogenConsumptionRatio);

            if (_fixedConsumptionRate > 0 && _spareRoomMethaneMass > 0)
            {
                var fixedMaxPossibleProductionRate = Math.Min(_spareRoomMethaneMass, _fixedConsumptionRate);

                var carbonDioxide_consumption_rate = fixedMaxPossibleProductionRate * _carbonDioxideMassByFraction;
                var hydrogen_consumption_rate = fixedMaxPossibleProductionRate * _hydrogenMassByFraction;

                // consume the resource
                _hydrogen_consumption_rate = _part.RequestResource(_hydrogen_resource_name, hydrogen_consumption_rate / _hydrogen_density) / fixedDeltaTime * _hydrogen_density;
                _carbondioxide_consumption_rate = _part.RequestResource(_carbondioxide_resource_name, carbonDioxide_consumption_rate / _carbondioxide_density) / fixedDeltaTime * _carbondioxide_density;

                fixed_combined_consumption_rate = _hydrogen_consumption_rate + _carbondioxide_consumption_rate;
                combined_consumption_rate = fixed_combined_consumption_rate / fixedDeltaTime;

                var fixedMethaneProduction = fixed_combined_consumption_rate * _methaneMassByFraction * fixedDeltaTime / _methane_density;
                var fixedOxygenProduction = fixed_combined_consumption_rate * _oxygenMassByFraction * fixedDeltaTime / _oxygen_density;

                _methane_production_rate = -_part.RequestResource(_methane_resource_name, -fixedMethaneProduction) / fixedDeltaTime * _methane_density;
                _oxygen_production_rate = -_part.RequestResource(_oxygen_resource_name, -fixedOxygenProduction) / fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _hydrogen_consumption_rate = 0;
                _carbondioxide_consumption_rate = 0;
                _methane_production_rate = 0;
            }
            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_Consumption"), _bold_label, GUILayout.Width(labelWidth));//"Overal Consumption"
            GUILayout.Label(((combined_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_CarbonDioxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Carbon Dioxide Available"
            GUILayout.Label(_availableCarbondioxideMass.ToString("0.0000") + " mT / " + _maxCapacityCarbondioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_CarbonDioxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Carbon Dioxide Consumption Rate"
            GUILayout.Label((_carbondioxide_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_HydrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Available"
            GUILayout.Label(_availableHydrogenMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_HydrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Consumption Rate"
            GUILayout.Label((_hydrogen_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_MethaneStorage"), _bold_label, GUILayout.Width(labelWidth));//"Methane Storage"
            GUILayout.Label(_spareRoomMethaneMass.ToString("0.0000") + " mT / " + _maxCapacityMethaneMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_MethaneProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Methane Production Rate"
            GUILayout.Label((_methane_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SabatierReactor_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygen_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_methane_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_SabatierReactor_Statumsg1");//"Sabatier Process Ongoing"
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_SabatierReactor_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_SabatierReactor_Statumsg3");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.CarbonDioxide).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SabatierReactor_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.CarbonDioxide, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SabatierReactor_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.Hydrogen, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
