using UnityEngine;

namespace FNPlugin.Wasteheat
{
	static public class RadiatorProperties
	{
		private static bool _isInitialized = false;
		private static double _radiatorTemperatureMk1 = 1850;
		private static double _radiatorTemperatureMk2 = 2200;
		private static double _radiatorTemperatureMk3 = 2616;
		private static double _radiatorTemperatureMk4 = 3111;
		private static double _radiatorTemperatureMk5 = 3700;
		private static double _radiatorTemperatureMk6 = 4400;

		private static string _radiatorUpgradeTech1 = "heatManagementSystems";
		private static string _radiatorUpgradeTech2 = "advHeatManagement";
		private static string _radiatorUpgradeTech3 = "specializedRadiators";
		private static string _radiatorUpgradeTech4 = "exoticRadiators";
		private static string _radiatorUpgradeTech5 = "extremeRadiators";

		public static string RadiatorUpgradeTech1 { get { return _radiatorUpgradeTech1; } private set { _radiatorUpgradeTech1 = value; } }
		public static string RadiatorUpgradeTech2 { get { return _radiatorUpgradeTech2; } private set { _radiatorUpgradeTech2 = value; } }
		public static string RadiatorUpgradeTech3 { get { return _radiatorUpgradeTech3; } private set { _radiatorUpgradeTech3 = value; } }
		public static string RadiatorUpgradeTech4 { get { return _radiatorUpgradeTech4; } private set { _radiatorUpgradeTech4 = value; } }
		public static string RadiatorUpgradeTech5 { get { return _radiatorUpgradeTech5; } private set { _radiatorUpgradeTech5 = value; } }

		public static double RadiatorTemperatureMk1 { get { return _radiatorTemperatureMk1; } private set { _radiatorTemperatureMk1 = value; } }
		public static double RadiatorTemperatureMk2 { get { return _radiatorTemperatureMk2; } private set { _radiatorTemperatureMk2 = value; } }
		public static double RadiatorTemperatureMk3 { get { return _radiatorTemperatureMk3; } private set { _radiatorTemperatureMk3 = value; } }
		public static double RadiatorTemperatureMk4 { get { return _radiatorTemperatureMk4; } private set { _radiatorTemperatureMk4 = value; } }
		public static double RadiatorTemperatureMk5 { get { return _radiatorTemperatureMk5; } private set { _radiatorTemperatureMk5 = value; } }
		public static double RadiatorTemperatureMk6 { get { return _radiatorTemperatureMk6; } private set { _radiatorTemperatureMk6 = value; } }

		static public void Initialize()
		{
			if (_isInitialized)
				return;

			Debug.Log("[KSPI] - RadiatorProperties - Attempting to read " + PluginHelper.WARP_PLUGIN_SETTINGS_FILEPATH);
			ConfigNode plugin_settings = GameDatabase.Instance.GetConfigNode(PluginHelper.WARP_PLUGIN_SETTINGS_FILEPATH);

			if (plugin_settings != null)
			{
				// Radiator Upgrade Tech
				if (plugin_settings.HasValue("RadiatorUpgradeTech1"))
				{
					RadiatorUpgradeTech1 = plugin_settings.GetValue("RadiatorUpgradeTech1");
					Debug.Log("[KSPI] RadiatorUpgradeTech1 " + RadiatorUpgradeTech1);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech2"))
				{
					RadiatorUpgradeTech2 = plugin_settings.GetValue("RadiatorUpgradeTech2");
					Debug.Log("[KSPI] RadiatorUpgradeTech2 " + RadiatorUpgradeTech2);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech3"))
				{
					RadiatorUpgradeTech3 = plugin_settings.GetValue("RadiatorUpgradeTech3");
					Debug.Log("[KSPI] RadiatorUpgradeTech3" + RadiatorUpgradeTech3);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech4"))
				{
					RadiatorUpgradeTech4 = plugin_settings.GetValue("RadiatorUpgradeTech4");
					Debug.Log("[KSPI] RadiatorUpgradeTech4 " + RadiatorUpgradeTech4);
				}
				if (plugin_settings.HasValue("RadiatorUpgradeTech5"))
				{
					RadiatorUpgradeTech5 = plugin_settings.GetValue("RadiatorUpgradeTech5");
					Debug.Log("[KSPI] RadiatorUpgradeTech5 " + RadiatorUpgradeTech5);
				}

				// Radiator Maximum Temperatures
				if (plugin_settings.HasValue("RadiatorTemperatureMk1"))
				{
					RadiatorTemperatureMk1 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk1"));
					Debug.Log("[KSPI] RadiatorTemperatureMk1" + RadiatorTemperatureMk1);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk2"))
				{
					RadiatorTemperatureMk2 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk2"));
					Debug.Log("[KSPI] RadiatorTemperatureMk2" + RadiatorTemperatureMk2);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk3"))
				{
					RadiatorTemperatureMk3 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk3"));
					Debug.Log("[KSPI] RadiatorTemperatureMk3" + RadiatorTemperatureMk3);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk4"))
				{
					RadiatorTemperatureMk4 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk4"));
					Debug.Log("[KSPI] RadiatorTemperatureMk4" + RadiatorTemperatureMk4);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk5"))
				{
					RadiatorTemperatureMk5 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk5"));
					Debug.Log("[KSPI] RadiatorTemperatureMk5" + RadiatorTemperatureMk5);
				}
				if (plugin_settings.HasValue("RadiatorTemperatureMk6"))
				{
					RadiatorTemperatureMk6 = double.Parse(plugin_settings.GetValue("RadiatorTemperatureMk6"));
					Debug.Log("[KSPI] RadiatorTemperatureMk6" + RadiatorTemperatureMk6);
				}
			}
		}
	}
}
