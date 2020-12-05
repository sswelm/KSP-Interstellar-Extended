using FNPlugin.Constants;
using System;
using UnityEngine;

namespace FNPlugin
{
    public class PluginSettings
    {
        private static PluginSettings _config;
        public static PluginSettings Config => _config ?? (_config = new PluginSettings(PluginHelper.PluginSettingsConfig));

        // doubles
        public  double SpotsizeMult { get; private set; } = 1.22;

        // integers
        public int SpeedOfLight { get; private set; } = 299792458;
        public int SecondsInDay { get; private set; } = GameConstants.KEBRIN_DAY_SECONDS;
        public int MicrowaveApertureDiameterMult { get; private set; } = 10;

        // Jet Upgrade Techs
        public string JetUpgradeTech1 { get; private set; } = "";
        public string JetUpgradeTech2 { get; private set; } = "";
        public string JetUpgradeTech3 { get; private set; } = "";
        public string JetUpgradeTech4 { get; private set; } = "";
        public string JetUpgradeTech5 { get; private set; } = "";


        public PluginSettings(ConfigNode pluginSettings)
        {
            UpdateIntWithConfigNode(pluginSettings, nameof(SpeedOfLight), value => SpeedOfLight = value);
            UpdateIntWithConfigNode(pluginSettings, nameof(SecondsInDay), value => SecondsInDay = value);
            UpdateIntWithConfigNode(pluginSettings, nameof(MicrowaveApertureDiameterMult), value => MicrowaveApertureDiameterMult = value);

            UpdateDoubleWithConfigNode(pluginSettings, nameof(SpotsizeMult), value => SpotsizeMult = value);

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
