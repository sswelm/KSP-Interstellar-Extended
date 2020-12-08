using FNPlugin.Constants;
using System;
using UnityEngine;

namespace FNPlugin
{
    public class PluginSettings
    {
        private static PluginSettings _config;

        public static PluginSettings Config =>
            _config ?? (_config = new PluginSettings(PluginHelper.PluginSettingsConfig));

        // integers
        public int SpeedOfLight { get; private set; } = 299792458;
        public int SecondsInDay { get; private set; } = GameConstants.KEBRIN_DAY_SECONDS;
        public int MicrowaveApertureDiameterMult { get; private set; } = 10;

        // doubles
        public double AirflowHeatMult { get; private set; } = GameConstants.AirflowHeatMultiplier;
        public double BasePowerConsumption { get; private set; } = GameConstants.basePowerConsumption;
        public double ElectricEngineIspMult { get; private set; } = 1;
        public double GlobalMagneticNozzlePowerMaxThrustMult { get; private set; } = 1;
        public double GlobalThermalNozzlePowerMaxThrustMult { get; private set; } = 1;
        public double GlobalElectricEnginePowerMaxThrustMult { get; private set; } = 1;
        public double HighCoreTempThrustMult { get; private set; } = GameConstants.HighCoreTempThrustMultiplier;
        public double IspCoreTempMult { get; private set; } = GameConstants.IspCoreTemperatureMultiplier;
        public double LowCoreTempBaseThrust { get; private set; } = 0;
        public double PowerConsumptionMultiplier { get; private set; } = 1;
        public double MaxThermalNozzleIsp { get; private set; } = GameConstants.MaxThermalNozzleIsp;
        public double SpotsizeMult { get; private set; } = 1.22;
        public double ThrustCoreTempThreshold { get; private set; } = 0;

        // Jet Upgrade Techs
        public string JetUpgradeTech1 { get; private set; } = "";
        public string JetUpgradeTech2 { get; private set; } = "";
        public string JetUpgradeTech3 { get; private set; } = "";
        public string JetUpgradeTech4 { get; private set; } = "";
        public string JetUpgradeTech5 { get; private set; } = "";



        public PluginSettings(ConfigNode pluginSettings)
        {
            UpdateIntWithConfigNode(pluginSettings, nameof(MicrowaveApertureDiameterMult), value => MicrowaveApertureDiameterMult = value);
            UpdateIntWithConfigNode(pluginSettings, nameof(SpeedOfLight), value => SpeedOfLight = value);
            UpdateIntWithConfigNode(pluginSettings, nameof(SecondsInDay), value => SecondsInDay = value);

            UpdateDoubleWithConfigNode(pluginSettings, nameof(AirflowHeatMult), value => AirflowHeatMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BasePowerConsumption), value => BasePowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(ElectricEngineIspMult), value => ElectricEngineIspMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(GlobalMagneticNozzlePowerMaxThrustMult), value => GlobalMagneticNozzlePowerMaxThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(GlobalThermalNozzlePowerMaxThrustMult), value => GlobalThermalNozzlePowerMaxThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(GlobalElectricEnginePowerMaxThrustMult), value => GlobalThermalNozzlePowerMaxThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(HighCoreTempThrustMult), value => HighCoreTempThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(IspCoreTempMult), value => IspCoreTempMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(LowCoreTempBaseThrust), value => LowCoreTempBaseThrust = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(PowerConsumptionMultiplier), value => PowerConsumptionMultiplier = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(MaxThermalNozzleIsp), value => MaxThermalNozzleIsp = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(SpotsizeMult), value => SpotsizeMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(ThrustCoreTempThreshold), value => ThrustCoreTempThreshold = value);

            UpdateStringWithConfigNode(pluginSettings, nameof(JetUpgradeTech1), value => JetUpgradeTech1 = value);
            UpdateStringWithConfigNode(pluginSettings, nameof(JetUpgradeTech2), value => JetUpgradeTech2 = value);
            UpdateStringWithConfigNode(pluginSettings, nameof(JetUpgradeTech3), value => JetUpgradeTech3 = value);
            UpdateStringWithConfigNode(pluginSettings, nameof(JetUpgradeTech4), value => JetUpgradeTech4 = value);
            UpdateStringWithConfigNode(pluginSettings, nameof(JetUpgradeTech5), value => JetUpgradeTech5 = value);
        }

        private void UpdateStringWithConfigNode(ConfigNode pluginSettings, string propertyName, Action<string> property)
        {
            if (!pluginSettings.HasValue(propertyName)) return;

            var value = pluginSettings.GetValue(propertyName);

            property(value);

            Debug.Log("[KSPI]: property " + propertyName + " set to " + value);
        }

        private void UpdateIntWithConfigNode(ConfigNode pluginSettings, string propertyName, Action<int> property)
        {
            if (!pluginSettings.HasValue(propertyName)) return;

            var value = pluginSettings.GetValue(propertyName);

            if (!int.TryParse(value, out int number))
            {
                if (!double.TryParse(value, out double doubleNumber))
                    return;
                number = (int)doubleNumber;
            }

            property(number);

            Debug.Log("[KSPI]: property " + propertyName + " set to " + number);
        }

        private void UpdateDoubleWithConfigNode(ConfigNode pluginSettings, string propertyName, Action<double> property)
        {
            if (!pluginSettings.HasValue(propertyName)) return;

            var value = pluginSettings.GetValue(propertyName);

            if (!double.TryParse(value, out double number)) return;

            property(number);

            Debug.Log("[KSPI]: property " + propertyName + " set to " + number);
        }
    }
}
