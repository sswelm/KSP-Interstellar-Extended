using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class PeroxideProcess : RefineryActivityBase, IRefineryActivity
    {
        double _fixedConsumptionRate;
        double _consumptionRate;
        double _consumptionStorageRatio;

        string _ammonia_resource_name;
        string _hydrazine_resource_name;
        string _hydrogen_peroxide_name;
        string _water_resource_name;

        double _ammonia_density;
        double _water_density;
        double _hydrogen_peroxide_density;
        double _hydrazine_density;

        double _ammonia_consumption_rate;
        double _hydrogen_peroxide_consumption_rate;
        double _water_production_rate;
        double _hydrazine_production_rate;

        double _maxCapacityAmmoniaMass;
        double _maxCapacityHydrogenPeroxideMass;
        double _maxCapacityHydrazineMass;
        double _maxCapacityWaterMass;

        double _availableAmmoniaMass;
        double _availableHydrogenPeroxideMass;
        double _spareRoomHydrazineMass;
        double _spareRoomWaterMass;

        double ammona_mass_consumption_ratio = (2 * 35.04) / (2 * 35.04 + 34.0147);
        double hydrogen_peroxide_mass_consumption_ratio = 34.0147 / (2 * 35.04 + 34.0147);
        double hydrazine_mass_production_ratio = 32.04516 / (32.04516 + 2 * 18.01528);
        double water_mass_production_ratio = (2 * 18.01528) / (32.04516 + 2 * 18.01528);

        public RefineryType RefineryType { get { return RefineryType.synthesize; } }

		public String ActivityName { get { return "Peroxide Process: H<size=7>2</size>O<size=7>2</size> + NH<size=7>3</size> => H<size=7>2</size>O + N<size=7>2</size>H<size=7>4</size> (Hydrazine)"; } }

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_hydrogen_peroxide_name).Any(rs => rs.amount > 0)
            & _part.GetConnectedResources(_ammonia_resource_name).Any(rs => rs.amount > 0);
        }

        public double PowerRequirements { get { return PluginHelper.BasePechineyUgineKuhlmannPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _ammonia_resource_name = InterstellarResourcesConfiguration.Instance.Ammonia;
            _hydrazine_resource_name = InterstellarResourcesConfiguration.Instance.Hydrazine;
            _water_resource_name = InterstellarResourcesConfiguration.Instance.Water;
            _hydrogen_peroxide_name = InterstellarResourcesConfiguration.Instance.HydrogenPeroxide;

            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(_ammonia_resource_name).density;
            _water_density = PartResourceLibrary.Instance.GetDefinition(_water_resource_name).density;
            _hydrogen_peroxide_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_peroxide_name).density;
            _hydrazine_density = PartResourceLibrary.Instance.GetDefinition(_hydrazine_resource_name).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _effectiveMaxPower = PowerRequirements * productionModidier;
            _current_power = PowerRequirements * powerFraction;

            _current_rate = CurrentPower / PluginHelper.PechineyUgineKuhlmannEnergyPerTon;

            // determine how much resource we have
            var partsThatContainAmmonia = _part.GetConnectedResources(_ammonia_resource_name);
            var partsThatContainHydrogenPeroxide = _part.GetConnectedResources(_hydrogen_peroxide_name);
            var partsThatContainHydrazine = _part.GetConnectedResources(_hydrazine_resource_name);
            var partsThatContainWater = _part.GetConnectedResources(_water_resource_name);

            _maxCapacityAmmoniaMass = partsThatContainAmmonia.Sum(p => p.maxAmount) * _ammonia_density;
            _maxCapacityHydrogenPeroxideMass = partsThatContainHydrogenPeroxide.Sum(p => p.maxAmount) * _hydrogen_peroxide_density;
            _maxCapacityHydrazineMass = partsThatContainHydrogenPeroxide.Sum(p => p.maxAmount) * _hydrazine_density;
            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;

            _availableAmmoniaMass = partsThatContainAmmonia.Sum(r => r.amount) * _ammonia_density;
            _availableHydrogenPeroxideMass = partsThatContainHydrogenPeroxide.Sum(r => r.amount) * _hydrogen_peroxide_density;

            _spareRoomHydrazineMass = partsThatContainHydrazine.Sum(r => r.maxAmount - r.amount) * _hydrazine_density;
            _spareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _water_density;

            // determine how much we can consume
            var fixedMaxAmmoniaConsumptionRate = _current_rate * ammona_mass_consumption_ratio * fixedDeltaTime;
            var ammoniaConsumptionRatio = fixedMaxAmmoniaConsumptionRate > 0 ? Math.Min(fixedMaxAmmoniaConsumptionRate, _availableAmmoniaMass) / fixedMaxAmmoniaConsumptionRate : 0;

            var fixedMaxHydrogenPeroxideConsumptionRate = _current_rate * hydrogen_peroxide_mass_consumption_ratio * fixedDeltaTime;
            var hydrogenPeroxideConsumptionRatio = fixedMaxHydrogenPeroxideConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenPeroxideConsumptionRate, _availableHydrogenPeroxideMass) / fixedMaxHydrogenPeroxideConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * Math.Min(ammoniaConsumptionRatio, hydrogenPeroxideConsumptionRatio);
            _consumptionRate = _fixedConsumptionRate / fixedDeltaTime;

            if (_fixedConsumptionRate > 0 && (_spareRoomHydrazineMass > 0 || _spareRoomWaterMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrazineRate = _fixedConsumptionRate * hydrazine_mass_production_ratio;
                var fixedMaxWaterRate = _fixedConsumptionRate * water_mass_production_ratio;

                var fixedMaxPossibleydrazineRate = allowOverflow ? fixedMaxHydrazineRate : Math.Min(_spareRoomHydrazineMass, fixedMaxHydrazineRate);
                var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleydrazineRate / fixedMaxHydrazineRate, fixedMaxPossibleWaterRate / fixedMaxWaterRate);

                // now we do the real consumption
                var ammonia_request = _fixedConsumptionRate * ammona_mass_consumption_ratio * _consumptionStorageRatio / _ammonia_density;
                _ammonia_consumption_rate = _part.RequestResource(_ammonia_resource_name, ammonia_request) * _ammonia_density / fixedDeltaTime;

                var hydrogen_peroxide_request = _fixedConsumptionRate * hydrogen_peroxide_mass_consumption_ratio * _consumptionStorageRatio / _hydrogen_peroxide_density;
                _hydrogen_peroxide_consumption_rate = _part.RequestResource(_hydrogen_peroxide_name, hydrogen_peroxide_request) * _hydrogen_peroxide_density / fixedDeltaTime;

                var combined_consumption_rate = _ammonia_consumption_rate + _hydrogen_peroxide_consumption_rate;

                var fixed_hydrazine_production = combined_consumption_rate * hydrazine_mass_production_ratio * fixedDeltaTime / _hydrazine_density;
                    _hydrazine_production_rate = -_part.RequestResource(_hydrazine_resource_name, -fixed_hydrazine_production) * _hydrazine_density / fixedDeltaTime;

                var fixed_water_production = combined_consumption_rate * water_mass_production_ratio * fixedDeltaTime / _water_density;
                    _water_production_rate = -_part.RequestResource(_water_resource_name, -fixed_water_production) * _water_density / fixedDeltaTime;
            }
            else
            {
                _ammonia_consumption_rate = 0;
                _hydrogen_peroxide_consumption_rate = 0;
                _hydrazine_production_rate = 0;
                _water_production_rate = 0;
            }

            updateStatusMessage();
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
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_AmmonaConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammona Consumption Rate"
            GUILayout.Label((_ammonia_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrogenPeroxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Peroxide Available"
            GUILayout.Label(_availableHydrogenPeroxideMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenPeroxideMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrogenPeroxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Peroxide Consumption Rate"
            GUILayout.Label((_hydrogen_peroxide_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_WaterStorage"), _bold_label, GUILayout.Width(labelWidth));//"Water Storage"
            GUILayout.Label(_spareRoomWaterMass.ToString("0.00000") + " mT / " + _maxCapacityWaterMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_WaterProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Water Production Rate"
            GUILayout.Label((_water_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrazineStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrazine Storage"
            GUILayout.Label(_spareRoomHydrazineMass.ToString("0.00000") + " mT / " + _maxCapacityHydrazineMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_PeroxideProcess_HydrazineProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrazine Production Rate"
            GUILayout.Label((_hydrazine_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_water_production_rate > 0 && _hydrazine_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg1");//"Peroxide Process Ongoing"
            else if (_hydrazine_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg2");//"Ongoing: Insufficient Monopropellant Storage"
            else if (_water_production_rate > 0)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg3");//"Ongoing: Insufficient Water Storage"
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg4");//"Insufficient Power"
            else
            {
                if (_ammonia_consumption_rate > 0 && _hydrogen_peroxide_consumption_rate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg5");//"Insufficient Storage"
                else if (_ammonia_consumption_rate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg6");//"Hydrogen Peroxide Deprived"
                else if (_hydrogen_peroxide_consumption_rate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg7");//"Ammonia Deprived"
                else
                    _status = Localizer.Format("#LOC_KSPIE_PeroxideProcess_Statumsg8");//"Hydrogen Peroxide and Ammonia Deprived"
            }
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_PeroxideProcess_Postmsg1", InterstellarResourcesConfiguration.Instance.HydrogenPeroxide), 3.0f, ScreenMessageStyle.UPPER_CENTER);//"Missing " +  + " (Hydrogen Peroxide)"
            if (!_part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_PeroxideProcess_Postmsg2", InterstellarResourcesConfiguration.Instance.Ammonia), 3.0f, ScreenMessageStyle.UPPER_CENTER);//"Missing " + 
        }
    }
}
