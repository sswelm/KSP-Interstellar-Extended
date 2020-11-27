using System;
using UnityEngine;

namespace FNPlugin.Resources
{
    public class ResourceSettings
    {
        private static ResourceSettings _config;

        public const string LqdHydrogen = nameof(LqdHydrogen);
        public const string ElectricCharge = nameof(ElectricCharge);

        #region autoproperties

        // Chemical resources
        public string Actinides { get; private set; } = "Actinides";
        public string Alumina { get; private set; } = "Alumina";
        public string Aluminium { get; private set; } = "Aluminium";
        public string AmmoniaLqd { get; private set; } = "LqdAmmonia";
        public string ArgonLqd { get; private set; } = "LqdArgon";
        public string CarbonDioxideGas { get; private set; } = "CarbonDioxide";
        public string CarbonDioxideLqd { get; private set; } = "LqdCO2";
        public string CarbonMonoxideGas { get; private set; } = "CarbonMonoxide";
        public string CarbonMonoxideLqd { get; private set; } = "LqdCO";
        public string DeuteriumLqd { get; private set; } = "LqdDeuterium";
        public string DeuteriumGas { get; private set; } = "Deuterium";
        public string Helium4Gas { get; private set; } = "Helium";
        public string Helium4Lqd { get; private set; } = "LqdHelium";
        public string Helium3Gas { get; private set; } = "Helium3";
        public string Helium3Lqd { get; private set; } = "LqdHe3";
        public string HydrogenGas { get; private set; } = "Hydrogen";
        public string HydrogenLqd { get; private set; } = LqdHydrogen;
        public string FluorineGas { get; private set; } = "Fluorine";
        public string KryptonGas { get; private set; } = "KryptonGas";
        public string KryptonLqd { get; private set; } = "LqdKrypton";
        public string Lithium6 { get; private set; } = "Lithium6";
        public string Lithium7 { get; private set; } = "Lithium";
        public string ChlorineGas { get; private set; } = "Chlorine";
        public string MethaneLqd { get; private set; } = "LqdMethane";
        public string NeonGas { get; private set; } = "LqdGas";
        public string NeonLqd { get; private set; } = "LqdNeon";
        public string NitrogenLqd { get; private set; } = "LqdNitrogen";
        public string NitrogenGas { get; private set; } = "Nitrogen";
        public string Nitrogen15Lqd { get; private set; } = "LqdNitrogen15";
        public string Sodium { get; private set; } = "Sodium";
        public string OxygenGas { get; private set; } = "Oxygen";
        public string OxygenLqd { get; private set; } = "LqdOxygen";
        public string WaterPure { get; private set; } = "Water";
        public string WaterRaw { get; private set; } = "LqdWater";

        // Nuclear resources
        public string DepletedFuel { get; private set; } = "DepletedFuel";
        public string EnrichedUranium { get; private set; } = "EnrichedUranium";
        public string Plutonium238 { get; private set; } = "Plutonium-238";
        public string ThoriumTetraflouride { get; private set; } = "ThF4";
        public string UraniumTetraflouride { get; private set; } = "UF4";
        public string Uranium233 { get; private set; } = "Uranium-233";
        public string UraniumNitride { get; private set; } = "UraniumNitride";

        // Abstract resources
        public string IntakeOxygenAir { get; private set; } = "IntakeAir";
        public string IntakeLiquid { get; private set; } = "IntakeLqd";
        public string IntakeAtmosphere { get; private set; } = "IntakeAtm";

        // Pseudo resources
        public string ElectricChargePower { get; private set; } = ElectricCharge;
        public string AntiProtium { get; private set; } = "Antimatter";
        public string VacuumPlasma { get; private set; } = "VacuumPlasma";
        public string ExoticMatter { get; private set; } = "ExoticMatter";


        #endregion

        public const String _LIQUID_HEAVYWATER = "HeavyWater";
        public const String _LIQUID_XENON = "LqdXenon";
        public const String _XENON_GAS = "LqdXenon";
        public const String _LIQUID_TRITIUM = "LqdTritium";
        public const String _TRITIUM_GAS = "Tritium";

        private String _hydrogen_peroxide = "HTP";
        private String _hydrazine = "Hydrazine";
        private String _heavyWater = _LIQUID_HEAVYWATER;
        private String _tritium = _LIQUID_TRITIUM;
        private String _tritium_gas = _TRITIUM_GAS;
        private String _solarWind = "SolarWind";
        private String _regolith = "Regolith";
        private String _xenongas = _XENON_GAS;
        private String _xenon = _LIQUID_XENON;

        // ToDo convert to auto property
        public String HydrogenPeroxide { get { return _hydrogen_peroxide; } }
        public String Hydrazine { get { return _hydrazine; } }
        public String Regolith { get { return _regolith; } }
        public String SolarWind { get { return _solarWind; } }
        public String LqdTritium { get { return _tritium; } }
        public String TritiumGas { get { return _tritium_gas; } }

        public String HeavyWater { get { return _heavyWater; } }
        public String Xenon { get { return _xenon; } }
        public String XenonGas { get { return _xenongas; } }

        private void UpdatePropertyWithConfigNode(ConfigNode pluginSettings, string resourceName, Action<string> property)
        {
            if (!pluginSettings.HasValue(resourceName + "ResourceName")) return;

            var value = pluginSettings.GetValue(resourceName + "ResourceName");
            property(value);
            Debug.Log("[KSPI]: " + resourceName + " resource name set to " + property);
        }

        public ResourceSettings(ConfigNode pluginSettings)
        {
            if (pluginSettings != null)
            {
                // chemical resources
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Actinides), value => Actinides = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(ArgonLqd), value => ArgonLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Aluminium), value => Aluminium = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Alumina), value => Alumina = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(AmmoniaLqd), value => AmmoniaLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(CarbonDioxideGas), value => CarbonDioxideGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(CarbonDioxideLqd), value => CarbonDioxideLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(CarbonMonoxideGas), value => CarbonMonoxideGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(CarbonMonoxideLqd), value => CarbonMonoxideLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(ChlorineGas), value => ChlorineGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(DeuteriumGas), value => DeuteriumGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(DeuteriumLqd), value => DeuteriumLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(FluorineGas), value => FluorineGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Helium4Gas), value => Helium4Gas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Helium4Lqd), value => Helium4Lqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Helium3Gas), value => Helium3Gas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Helium3Lqd), value => Helium3Lqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(HydrogenLqd), value => HydrogenLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(HydrogenGas), value => HydrogenGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(KryptonGas), value => KryptonGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(KryptonLqd), value => KryptonLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Lithium6), value => Lithium6 = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Lithium7), value => Lithium7 = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(MethaneLqd), value => MethaneLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(NeonGas), value => NeonGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(NeonLqd), value => NeonLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(NitrogenGas), value => NitrogenGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(NitrogenLqd), value => NitrogenLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Nitrogen15Lqd), value => Nitrogen15Lqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(OxygenGas), value => OxygenGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(OxygenLqd), value => OxygenLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Sodium), value => Sodium = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(WaterPure), value => WaterPure = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(WaterRaw), value => WaterRaw = value);

                // abstract resources
                UpdatePropertyWithConfigNode(pluginSettings, nameof(IntakeAtmosphere), value => IntakeAtmosphere = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(IntakeOxygenAir), value => IntakeOxygenAir = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(IntakeLiquid), value => IntakeOxygenAir = value);

                // nuclear resources
                UpdatePropertyWithConfigNode(pluginSettings, nameof(DepletedFuel), value => DepletedFuel = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(EnrichedUranium), value => EnrichedUranium = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Plutonium238), value => Plutonium238 = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(ThoriumTetraflouride), value => ThoriumTetraflouride = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Uranium233), value => Uranium233 = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(UraniumNitride), value => UraniumNitride = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(UraniumTetraflouride), value => UraniumTetraflouride = value);

                // pseudo resources
                UpdatePropertyWithConfigNode(pluginSettings, nameof(ElectricChargePower), value => ElectricChargePower = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(AntiProtium), value => AntiProtium = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(ExoticMatter), value => ExoticMatter = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(VacuumPlasma), value => VacuumPlasma = value);

                if (pluginSettings.HasValue("HydrazineResourceName"))
                {
                    _hydrazine = pluginSettings.GetValue("HydrazineResourceName");
                    Debug.Log("[KSPI]: Hydrazine resource name set to " + Hydrazine);
                }

                if (pluginSettings.HasValue("HydrogenPeroxideResourceName"))
                {
                    _hydrogen_peroxide = pluginSettings.GetValue("HydrogenPeroxideResourceName");
                    Debug.Log("[KSPI]: Hydrogen Peroxide resource name set to " + HydrogenPeroxide);
                }
                if (pluginSettings.HasValue("RegolithResourceName"))
                {
                    _regolith = pluginSettings.GetValue("RegolithResourceName");
                    Debug.Log("[KSPI]: Regolith resource name set to " + Regolith);
                }
                if (pluginSettings.HasValue("XenonGasResourceName"))
                {
                    _xenongas = pluginSettings.GetValue("XenonGasResourceName");
                    Debug.Log("[KSPI]: XenonGas resource name set to " + XenonGas);
                }
                if (pluginSettings.HasValue("SolarWindResourceName"))
                {
                    _solarWind = pluginSettings.GetValue("SolarWindResourceName");
                    Debug.Log("[KSPI]: SolarWind resource name set to " + SolarWind);
                }
                if (pluginSettings.HasValue("TritiumResourceName"))
                {
                    _tritium = pluginSettings.GetValue("TritiumResourceName");
                    Debug.Log("[KSPI]: Tritium resource name set to " + LqdTritium);
                }
                if (pluginSettings.HasValue("HeavyWaterResourceName"))
                {
                    _heavyWater = pluginSettings.GetValue("HeavyWaterResourceName");
                    Debug.Log("[KSPI]: Heavy Water resource name set to " + HeavyWater);
                }
            }
            else
            {
                PluginHelper.showInstallationErrorMessage();
            }
        }

        public static ResourceSettings Config => _config ?? (_config = new ResourceSettings(PluginHelper.PluginSettingsConfig));
    }
}
