using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FNPlugin.Extensions;
using FNPlugin.Propulsion;
using FNPlugin.Resources;

namespace FNPlugin.Wasteheat
{
	class RadiatorManager
	{
		private static Dictionary<Vessel, RadiatorManager> managers = new Dictionary<Vessel, RadiatorManager>();

		public static RadiatorManager Update(FNRadiator radiator)
		{
			RadiatorManager manager;

			managers.TryGetValue(radiator.vessel, out manager);

			if (manager == null || manager.UpdatingRadiator == null || (manager.UpdatingRadiator != radiator && manager.Counter < radiator.updateCounter))
				manager = CreateManager(radiator);

			if (manager != null && manager.UpdatingRadiator == radiator)
				manager.Update();

			return manager;
		}

		private static RadiatorManager CreateManager(FNRadiator radiator)
		{
			RadiatorManager manager = new RadiatorManager(radiator);

			managers[radiator.vessel] = manager;

			return manager;
		}

		private RadiatorManager(FNRadiator radiator)
		{
			UpdatingRadiator = radiator;

			// determine number of upgrade techs
			NrAvailableUpgradeTechs = 1;
			if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech5))
				NrAvailableUpgradeTechs++;
			if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech4))
				NrAvailableUpgradeTechs++;
			if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech3))
				NrAvailableUpgradeTechs++;
			if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech2))
				NrAvailableUpgradeTechs++;
			if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech1))
				NrAvailableUpgradeTechs++;

			// determine fusion tech levels
			if (NrAvailableUpgradeTechs == 6)
				CurrentGenerationType = GenerationType.Mk6;
			else if (NrAvailableUpgradeTechs == 5)
				CurrentGenerationType = GenerationType.Mk5;
			else if (NrAvailableUpgradeTechs == 4)
				CurrentGenerationType = GenerationType.Mk4;
			else if (NrAvailableUpgradeTechs == 3)
				CurrentGenerationType = GenerationType.Mk3;
			else if (NrAvailableUpgradeTechs == 2)
				CurrentGenerationType = GenerationType.Mk2;
			else
				CurrentGenerationType = GenerationType.Mk1;

			MaxVacuumTemperatureTitanium = RadiatorProperties.RadiatorTemperatureMk4;

			if (CurrentGenerationType == GenerationType.Mk6)
				MaxVacuumTemperatureGraphene = RadiatorProperties.RadiatorTemperatureMk6;
			else if (CurrentGenerationType == GenerationType.Mk5)
				MaxVacuumTemperatureGraphene = RadiatorProperties.RadiatorTemperatureMk5;
			else if (CurrentGenerationType == GenerationType.Mk4)
				MaxVacuumTemperatureGraphene = RadiatorProperties.RadiatorTemperatureMk4;
			else if (CurrentGenerationType == GenerationType.Mk3)
				MaxVacuumTemperatureTitanium = MaxVacuumTemperatureGraphene = RadiatorProperties.RadiatorTemperatureMk3;
			else if (CurrentGenerationType == GenerationType.Mk2)
				MaxVacuumTemperatureTitanium = MaxVacuumTemperatureGraphene = RadiatorProperties.RadiatorTemperatureMk2;
			else
				MaxVacuumTemperatureTitanium = MaxVacuumTemperatureGraphene = RadiatorProperties.RadiatorTemperatureMk1;
		}

		public FNRadiator UpdatingRadiator { get; private set; }
		public GenerationType CurrentGenerationType { get; private set; }
		public int NrAvailableUpgradeTechs { get; private set; }
		public long Counter { get; private set; }
		public double WasteHeatRatio { get; private set; }
		public double MaxVacuumTemperatureGraphene { get; private set; }
		public double MaxVacuumTemperatureTitanium { get; private set; }

		private double external_temperature;

		public void Update()
		{
			Counter = UpdatingRadiator.updateCounter;

			WasteHeatRatio = UpdatingRadiator.getResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);
			var sqrtWasteHeatRatio = Math.Sqrt(WasteHeatRatio);

			if (Double.IsNaN(WasteHeatRatio))
			{
				Debug.LogError("KSPI - FNRadiator: FixedUpdate Single.IsNaN detected in WasteHeatRatio");
				return;
			}
			external_temperature = FlightGlobals.getExternalTemperature(UpdatingRadiator.vessel.transform.position);
			var normalized_atmosphere = Math.Min(UpdatingRadiator.vessel.atmDensity, 1);

			// titanium radiator
			var radiator_temperature_temp_val_titanium = external_temperature + Math.Min((MaxVacuumTemperatureTitanium - external_temperature) * sqrtWasteHeatRatio, MaxVacuumTemperatureTitanium - external_temperature);

			// graphene radiator
			var atmosphereModifierVacuum = Math.Max(Math.Min(1 - UpdatingRadiator.vessel.atmDensity, 1), 0);
			var atmosphereModifierAtmosphere = Math.Max(normalized_atmosphere, 0);
			var maxCurrentTemperatureGraphene = 1200 * atmosphereModifierAtmosphere + MaxVacuumTemperatureGraphene * atmosphereModifierVacuum;
			var radiator_temperature_temp_val_graphene = external_temperature + Math.Min((MaxVacuumTemperatureGraphene - external_temperature) * sqrtWasteHeatRatio, maxCurrentTemperatureGraphene - external_temperature);
		}
	}
}
