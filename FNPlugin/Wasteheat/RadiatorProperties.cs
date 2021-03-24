using UnityEngine;

namespace FNPlugin.Wasteheat
{
    public static class RadiatorProperties
	{
		private static bool _isInitialized;

        public static string RadiatorUpgradeTech1 { get; private set; } = "heatManagementSystems";
        public static string RadiatorUpgradeTech2 { get; private set; } = "advHeatManagement";
        public static string RadiatorUpgradeTech3 { get; private set; } = "specializedRadiators";
        public static string RadiatorUpgradeTech4 { get; private set; } = "exoticRadiators";
        public static string RadiatorUpgradeTech5 { get; private set; } = "extremeRadiators";

        public static double RadiatorTemperatureMk1 { get; private set; } = 1918;
        public static double RadiatorTemperatureMk2 { get; private set; } = 2272;
        public static double RadiatorTemperatureMk3 { get; private set; } = 2698;
        public static double RadiatorTemperatureMk4 { get; private set; } = 3200;
        public static double RadiatorTemperatureMk5 { get; private set; } = 3795;
        public static double RadiatorTemperatureMk6 { get; private set; } = 4500;

        public static void Initialize()
		{
			if (_isInitialized)
				return;

			Debug.Log("[KSPI]: RadiatorProperties - Attempting to read " + PluginHelper.WarpPluginSettingsFilepath);
			ConfigNode plugin_settings = GameDatabase.Instance.GetConfigNode(PluginHelper.WarpPluginSettingsFilepath);

			if (plugin_settings != null)
			{
				// Radiator Upgrade Tech
				if (plugin_settings.HasValue("RadiatorUpgradeTech1"))
				{
					RadiatorUpgradeTech1 = plugin_settings.GetValue("RadiatorUpgradeTech1");
					Debug.Log("[KSPI]: RadiatorUpgradeTech1 " + RadiatorUpgradeTech1);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech2"))
				{
					RadiatorUpgradeTech2 = plugin_settings.GetValue("RadiatorUpgradeTech2");
					Debug.Log("[KSPI]: RadiatorUpgradeTech2 " + RadiatorUpgradeTech2);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech3"))
				{
					RadiatorUpgradeTech3 = plugin_settings.GetValue("RadiatorUpgradeTech3");
					Debug.Log("[KSPI]: RadiatorUpgradeTech3" + RadiatorUpgradeTech3);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech4"))
				{
					RadiatorUpgradeTech4 = plugin_settings.GetValue("RadiatorUpgradeTech4");
					Debug.Log("[KSPI]: RadiatorUpgradeTech4 " + RadiatorUpgradeTech4);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech5"))
				{
					RadiatorUpgradeTech5 = plugin_settings.GetValue("RadiatorUpgradeTech5");
					Debug.Log("[KSPI]: RadiatorUpgradeTech5 " + RadiatorUpgradeTech5);
				}

				// Radiator Maximum Temperatures
				if (plugin_settings.HasValue("RadiatorTemperatureMk1"))
				{
					RadiatorTemperatureMk1 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk1"));
					Debug.Log("[KSPI]: RadiatorTemperatureMk1 " + RadiatorTemperatureMk1);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk2"))
				{
					RadiatorTemperatureMk2 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk2"));
					Debug.Log("[KSPI]: RadiatorTemperatureMk2 " + RadiatorTemperatureMk2);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk3"))
				{
					RadiatorTemperatureMk3 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk3"));
					Debug.Log("[KSPI]: RadiatorTemperatureMk3 " + RadiatorTemperatureMk3);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk4"))
				{
					RadiatorTemperatureMk4 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk4"));
					Debug.Log("[KSPI]: RadiatorTemperatureMk4 " + RadiatorTemperatureMk4);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk5"))
				{
					RadiatorTemperatureMk5 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk5"));
					Debug.Log("[KSPI]: RadiatorTemperatureMk5 " + RadiatorTemperatureMk5);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk6"))
				{
					RadiatorTemperatureMk6 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk6"));
					Debug.Log("[KSPI]: RadiatorTemperatureMk6 " + RadiatorTemperatureMk6);
				}
			}

            _isInitialized = true;
		}
	}
}
