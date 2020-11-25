using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class PeroxideProcess : RefineryActivity, IRefineryActivity
    {
        public PeroxideProcess()
        {
            ActivityName = "Peroxide Process";
            Formula = "H<size=7>2</size>O<size=7>2</size> + NH<size=7>3</size> => H<size=7>2</size>O + N<size=7>2</size>H<size=7>4</size> (Hydrazine)";
            PowerRequirements = PluginHelper.BasePechineyUgineKuhlmannPowerConsumption;
            EnergyPerTon = PluginHelper.PechineyUgineKuhlmannEnergyPerTon;
        }

        private const double AmmoniaMassConsumptionRatio = (2 * 35.04) / (2 * 35.04 + 34.0147);
        private const double HydrogenPeroxideMassConsumptionRatio = 34.0147 / (2 * 35.04 + 34.0147);
        private const double HydrazineMassProductionRatio = 32.04516 / (32.04516 + 2 * 18.01528);
        private const double WaterMassProductionRatio = (2 * 18.01528) / (32.04516 + 2 * 18.01528);

        private double _fixedConsumptionRate;
        private double _consumptionRate;
        private double _consumptionStorageRatio;

        private string _ammoniaResourceName;
        private string _hydrazineResourceName;
        private string _hydrogenPeroxideName;
        private string _waterResourceName;

        private double _ammoniaDensity;
        private double _waterDensity;
        private double _hydrogenPeroxideDensity;
        private double _hydrazineDensity;

        private double _ammoniaConsumptionRate;
        private double _hydrogenPeroxideConsumptionRate;
        private double _waterProductionRate;
        private double _hydrazineProductionRate;

        private double _maxCapacityAmmoniaMass;
        private double _maxCapacityHydrogenPeroxideMass;
        private double _maxCapacityHydrazineMass;
        private double _maxCapacityWaterMass;

        private double _availableAmmoniaMass;
        private double _availableHydrogenPeroxideMass;
        private double _spareRoomHydrazineMass;
        private double _spareRoomWaterMass;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_hydrogenPeroxideName).Any(rs => rs.amount > 0)
            & _part.GetConnectedResources(_ammoniaResourceName).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _ammoniaResourceName = InterstellarResourcesConfiguration.Instance.AmmoniaLqd;
            _hydrazineResourceName = InterstellarResourcesConfiguration.Instance.Hydrazine;
            _waterResourceName = InterstellarResourcesConfiguration.Instance.Water;
            _hydrogenPeroxideName = InterstellarResourcesConfiguration.Instance.HydrogenPeroxide;

            _ammoniaDensity = PartResourceLibrary.Instance.GetDefinition(_ammoniaResourceName).density;
            _waterDensity = PartResourceLibrary.Instance.GetDefinition(_waterResourceName).density;
            _hydrogenPeroxideDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenPeroxideName).density;
            _hydrazineDensity = PartResourceLibrary.Instance.GetDefinition(_hydrazineResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPower = PowerRequirements * productionModifier;
            _current_power = PowerRequirements * powerFraction;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine how much resource we have
            var partsThatContainAmmonia = _part.GetConnectedResources(_ammoniaResourceName).ToList();
            var partsThatContainHydrogenPeroxide = _part.GetConnectedResources(_hydrogenPeroxideName).ToList();
            var partsThatContainHydrazine = _part.GetConnectedResources(_hydrazineResourceName).ToList();
            var partsThatContainWater = _part.GetConnectedResources(_waterResourceName).ToList();

            _maxCapacityAmmoniaMass = partsThatContainAmmonia.Sum(p => p.maxAmount) * _ammoniaDensity;
            _maxCapacityHydrogenPeroxideMass = partsThatContainHydrogenPeroxide.Sum(p => p.maxAmount) * _hydrogenPeroxideDensity;
            _maxCapacityHydrazineMass = partsThatContainHydrogenPeroxide.Sum(p => p.maxAmount) * _hydrazineDensity;
            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _waterDensity;

            _availableAmmoniaMass = partsThatContainAmmonia.Sum(r => r.amount) * _ammoniaDensity;
            _availableHydrogenPeroxideMass = partsThatContainHydrogenPeroxide.Sum(r => r.amount) * _hydrogenPeroxideDensity;

            _spareRoomHydrazineMass = partsThatContainHydrazine.Sum(r => r.maxAmount - r.amount) * _hydrazineDensity;
            _spareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _waterDensity;

            // determine how much we can consume
            var fixedMaxAmmoniaConsumptionRate = _current_rate * AmmoniaMassConsumptionRatio * fixedDeltaTime;
            var ammoniaConsumptionRatio = fixedMaxAmmoniaConsumptionRate > 0 ? Math.Min(fixedMaxAmmoniaConsumptionRate, _availableAmmoniaMass) / fixedMaxAmmoniaConsumptionRate : 0;

            var fixedMaxHydrogenPeroxideConsumptionRate = _current_rate * HydrogenPeroxideMassConsumptionRatio * fixedDeltaTime;
            var hydrogenPeroxideConsumptionRatio = fixedMaxHydrogenPeroxideConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenPeroxideConsumptionRate, _availableHydrogenPeroxideMass) / fixedMaxHydrogenPeroxideConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(ammoniaConsumptionRatio, hydrogenPeroxideConsumptionRatio);
            _consumptionRate = _fixedConsumptionRate / fixedDeltaTime;

            if (_fixedConsumptionRate > 0 && (_spareRoomHydrazineMass > 0 || _spareRoomWaterMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrazineRate = _fixedConsumptionRate * HydrazineMassProductionRatio;
                var fixedMaxWaterRate = _fixedConsumptionRate * WaterMassProductionRatio;

                var fixedMaxPossibleHydrazineRate = allowOverflow ? fixedMaxHydrazineRate : Math.Min(_spareRoomHydrazineMass, fixedMaxHydrazineRate);
                var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrazineRate / fixedMaxHydrazineRate, fixedMaxPossibleWaterRate / fixedMaxWaterRate);

                // now we do the real consumption
                var ammoniaRequest = _fixedConsumptionRate * AmmoniaMassConsumptionRatio * _consumptionStorageRatio / _ammoniaDensity;
                _ammoniaConsumptionRate = _part.RequestResource(_ammoniaResourceName, ammoniaRequest) * _ammoniaDensity / fixedDeltaTime;

                var hydrogenPeroxideRequest = _fixedConsumptionRate * HydrogenPeroxideMassConsumptionRatio * _consumptionStorageRatio / _hydrogenPeroxideDensity;
                _hydrogenPeroxideConsumptionRate = _part.RequestResource(_hydrogenPeroxideName, hydrogenPeroxideRequest) * _hydrogenPeroxideDensity / fixedDeltaTime;

                var combinedConsumptionRate = _ammoniaConsumptionRate + _hydrogenPeroxideConsumptionRate;

                var fixedHydrazineProduction = combinedConsumptionRate * HydrazineMassProductionRatio * fixedDeltaTime / _hydrazineDensity;
                    _hydrazineProductionRate = -_part.RequestResource(_hydrazineResourceName, -fixedHydrazineProduction) * _hydrazineDensity / fixedDeltaTime;

                var fixedWaterProduction = combinedConsumptionRate * WaterMassProductionRatio * fixedDeltaTime / _waterDensity;
                    _waterProductionRate = -_part.RequestResource(_waterResourceName, -fixedWaterProduction) * _waterDensity / fixedDeltaTime;
            }
            else
            {
                _ammoniaConsumptionRate = 0;
                _hydrogenPeroxideConsumptionRate = 0;
                _hydrazineProductionRate = 0;
                _waterProductionRate = 0;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_Consumption"), _bold_label, GUILayout.Width(labelWidth));//"Current Consumption"
            GUILayout.Label(((_consumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.00000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_AmmoniaAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Available"
            GUILayout.Label(_availableAmmoniaMass.ToString("0.00000") + " mT / " + _maxCapacityAmmoniaMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_AmmonaConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Consumption Rate"
            GUILayout.Label((_ammoniaConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrogenPeroxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Peroxide Available"
            GUILayout.Label(_availableHydrogenPeroxideMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenPeroxideMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrogenPeroxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Peroxide Consumption Rate"
            GUILayout.Label((_hydrogenPeroxideConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_WaterStorage"), _bold_label, GUILayout.Width(labelWidth));//"Water Storage"
            GUILayout.Label(_spareRoomWaterMass.ToString("0.00000") + " mT / " + _maxCapacityWaterMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_WaterProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Water Production Rate"
            GUILayout.Label((_waterProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrazineStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrazine Storage"
            GUILayout.Label(_spareRoomHydrazineMass.ToString("0.00000") + " mT / " + _maxCapacityHydrazineMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrazineProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrazine Production Rate"
            GUILayout.Label((_hydrazineProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_waterProductionRate > 0 && _hydrazineProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg1");//"Peroxide Process Ongoing"
            else if (_hydrazineProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg2");//"Ongoing: Insufficient MonoPropellant Storage"
            else if (_waterProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg3");//"Ongoing: Insufficient Water Storage"
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg4");//"Insufficient Power"
            else
            {
                if (_ammoniaConsumptionRate > 0 && _hydrogenPeroxideConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg5");//"Insufficient Storage"
                else if (_ammoniaConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg6");//"Hydrogen Peroxide Deprived"
                else if (_hydrogenPeroxideConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg7");//"Ammonia Deprived"
                else
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg8");//"Hydrogen Peroxide and Ammonia Deprived"
            }
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_PeroxideProcess_Postmsg1", InterstellarResourcesConfiguration.Instance.HydrogenPeroxide), 3.0f, ScreenMessageStyle.UPPER_CENTER);//"Missing " +  + " (Hydrogen Peroxide)"
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.AmmoniaLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_PeroxideProcess_Postmsg2", InterstellarResourcesConfiguration.Instance.AmmoniaLqd), 3.0f, ScreenMessageStyle.UPPER_CENTER);//"Missing " +
        }
    }
}
