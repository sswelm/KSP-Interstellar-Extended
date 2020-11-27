using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class AnthraquinoneProcessor : RefineryActivity, IRefineryActivity
    {
        public AnthraquinoneProcessor()
        {
            ActivityName = "Anthraquinone Process";
            Formula = "H<size=7>2</size> + O<size=7>2</size> => H<size=7>2</size>O<size=7>2</size> (HTP)";
            PowerRequirements = PluginHelper.BaseAnthraquiononePowerConsumption;
            EnergyPerTon = PluginHelper.AnthraquinoneEnergyPerTon;
        }

        private const double HydrogenMassByFraction = (1.0079 * 2) / 34.01468;
        private const double OxygenMassByFraction = 1 - ((1.0079 * 2) / 34.01468);

        private double _fixedConsumptionRate;
        private double _consumptionRate;

        private double _hydrogenDensity;
        private double _oxygenDensity;
        private double _hydrogenPeroxideDensity;

        private string _oxygenResourceName;
        private string _hydrogenResourceName;
        private string _hydrogenPeroxideResourceName;

        private double _maxCapacityOxygenMass;
        private double _maxCapacityHydrogenMass;
        private double _maxCapacityPeroxideMass;

        private double _availableOxygenMass;
        private double _availableHydrogenMass;
        private double _spareRoomHydrogenPeroxideMass;

        private double _hydrogenConsumptionRate;
        private double _oxygenConsumptionRate;
        private double _hydrogenPeroxideProductionRate;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(ResourceSettings.Config.HydrogenLqd).Any(rs => rs.amount > 0) &
                _part.GetConnectedResources(ResourceSettings.Config.OxygenLqd).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _oxygenResourceName = ResourceSettings.Config.OxygenLqd;
            _hydrogenResourceName = ResourceSettings.Config.HydrogenLqd;
            _hydrogenPeroxideResourceName = ResourceSettings.Config.HydrogenPeroxide;

            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.HydrogenLqd).density;
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.OxygenLqd).density;
            _hydrogenPeroxideDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.HydrogenPeroxide).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPower = PowerRequirements * productionModifier;

            // determine how much resource we have
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / PluginHelper.AnthraquinoneEnergyPerTon;

            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName).ToList();
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName).ToList();
            var partsThatContainPeroxide = _part.GetConnectedResources(_hydrogenPeroxideResourceName).ToList();

            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygenDensity;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogenDensity;
            _maxCapacityPeroxideMass = partsThatContainPeroxide.Sum(p => p.maxAmount) * _hydrogenPeroxideDensity;

            _availableOxygenMass = partsThatContainOxygen.Sum(r => r.amount) * _oxygenDensity;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogenDensity;
            _spareRoomHydrogenPeroxideMass = partsThatContainPeroxide.Sum(r => r.maxAmount - r.amount) * _hydrogenPeroxideDensity;

            // determine how much we can consume
            var fixedMaxOxygenConsumptionRate = _current_rate * OxygenMassByFraction * fixedDeltaTime;
            var oxygenConsumptionRatio = fixedMaxOxygenConsumptionRate > 0 ? Math.Min(fixedMaxOxygenConsumptionRate, _availableOxygenMass) / fixedMaxOxygenConsumptionRate : 0;

            var fixedMaxHydrogenConsumptionRate = _current_rate * HydrogenMassByFraction * fixedDeltaTime;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(oxygenConsumptionRatio, hydrogenConsumptionRatio);
            _consumptionRate = _fixedConsumptionRate / fixedDeltaTime;

            if (_fixedConsumptionRate > 0 && _spareRoomHydrogenPeroxideMass > 0)
            {
                var fixedMaxPossibleHydrogenPeroxideRate = Math.Min(_spareRoomHydrogenPeroxideMass, _fixedConsumptionRate);

                var hydrogenConsumptionRate = fixedMaxPossibleHydrogenPeroxideRate * HydrogenMassByFraction;
                var oxygenConsumptionRate = fixedMaxPossibleHydrogenPeroxideRate * OxygenMassByFraction;

                // consume the resource
                _hydrogenConsumptionRate = _part.RequestResource(_hydrogenResourceName, hydrogenConsumptionRate / _hydrogenDensity) / fixedDeltaTime * _hydrogenDensity;
                _oxygenConsumptionRate = _part.RequestResource(_oxygenResourceName, oxygenConsumptionRate / _oxygenDensity) / fixedDeltaTime * _oxygenDensity;

                var combinedConsumptionRate = (_hydrogenConsumptionRate + _oxygenConsumptionRate) * fixedDeltaTime / _hydrogenPeroxideDensity;

                _hydrogenPeroxideProductionRate = -_part.RequestResource(_hydrogenPeroxideResourceName, -combinedConsumptionRate) / fixedDeltaTime * _hydrogenPeroxideDensity;
            }
            else
            {
                _hydrogenConsumptionRate = 0;
                _oxygenConsumptionRate = 0;
                _hydrogenPeroxideProductionRate = 0;
            }


            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_OveralConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Overal Consumption"
            GUILayout.Label(((_consumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000")) + " mT/"+Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_HydrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Available"
            GUILayout.Label(_availableHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_HydrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Consumption Rate"
            GUILayout.Label((_hydrogenConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/"+Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_OxygenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Available"
            GUILayout.Label(_availableOxygenMass.ToString("0.00000") + " mT / " + _maxCapacityOxygenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_OxygenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Consumption Rate"
            GUILayout.Label((_oxygenConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/"+Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_HydrogenPeroxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Peroxide Storage"
            GUILayout.Label(_spareRoomHydrogenPeroxideMass.ToString("0.00000") + " mT / " + _maxCapacityPeroxideMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_HydrogenPeroxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Peroxide Production Rate"
            GUILayout.Label((_hydrogenPeroxideProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/"+Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenPeroxideProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_statumsg1");//"Electrolysing"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_statumsg3");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(ResourceSettings.Config.HydrogenLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_Postmsg") + " " + ResourceSettings.Config.HydrogenLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(ResourceSettings.Config.OxygenLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AnthraquinoneProcessor_Postmsg") + " " + ResourceSettings.Config.OxygenLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
