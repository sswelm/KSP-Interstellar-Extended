using System;
using UnityEngine;

namespace FNPlugin.Resources
{
    public class ResourceSettings
    {
        private static ResourceSettings _config;

        public const string LqdHydrogen = nameof(LqdHydrogen);
        public const string ElectricCharge = "ElectricCharge";

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

        public string HydrogenLqd { get; private set; } = LqdHydrogen;
        public string HydrogenGas { get; private set; } = "Hydrogen";
        public string FluorineGas { get; private set; } = "Fluorine";
        public string Lithium6 { get; private set; } = "Lithium6";
        public string Lithium7 { get; private set; } = "Lithium";

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

        public const String _CHLORINE = "Chlorine";
        public const String _LIQUID_METHANE = "LqdMethane";
        public const String _HELIUM4_LIQUID = "LqdHelium";
        public const String _HELIUM3_LIQUID = "LqdHe3";
        public const String _HELIUM3_GAS = "Helium3";
        public const String _NEON_LIQUID = "LqdNeon";
        public const String _NEON_GAS = "NeonGas";
        public const String _NITROGEN_LIQUID = "LqdNitrogen";
        public const String _NITROGEN_GAS = "Nitrogen";
        public const String _LIQUID_NITROGEN_15 = "LqdNitrogen15";
        public const String _LIQUID_OXYGEN = "LqdOxygen";
        public const String _OXYGEN_GAS = "Oxygen";
        public const String _LIQUID_WATER = "Water";
        public const String _LIQUID_HEAVYWATER = "HeavyWater";
        public const String _LIQUID_XENON = "LqdXenon";
        public const String _SODIUM = "Sodium";
        public const String _XENON_GAS = "LqdXenon";
        public const String _LIQUID_KRYPTON = "LqdKrypton";
        public const String _KRYPTON_GAS = "KryptonGas";
        public const String _LIQUID_TRITIUM = "LqdTritium";
        public const String _TRITIUM_GAS = "Tritium";

        private String _helium3_gas = _HELIUM3_GAS;
        private String _liquid_helium3 = _HELIUM3_LIQUID;
        private String _sodium = "Sodium";
        private String _hydrogen_peroxide = "HTP";
        private String _hydrazine = "Hydrazine";
        private String _methane = _LIQUID_METHANE;
        private String _nitrogen = _NITROGEN_LIQUID;
        private String _nitrogen15 = _LIQUID_NITROGEN_15;
        private String _lqdOxygen = _LIQUID_OXYGEN;
        private String _oxygen_gas = _OXYGEN_GAS;
        private String _water = _LIQUID_WATER;
        private String _heavyWater = _LIQUID_HEAVYWATER;
        private String _tritium = _LIQUID_TRITIUM;
        private String _tritium_gas = _TRITIUM_GAS;
        private String _solarWind = "SolarWind";
        private String _neon_gas = _NEON_LIQUID;
        private String _regolith = "Regolith";
        private String _xenongas = _XENON_GAS;
        private String _xenon = _LIQUID_XENON;
        private string _kryton = _LIQUID_KRYPTON;
        private string _krytongas = _KRYPTON_GAS;

        // ToDo convert to auto property


        public String LqdHelium3 { get { return _liquid_helium3; } }
        public String Sodium { get { return _sodium; } }
        public String Helium3Gas { get { return _helium3_gas; } }
        public String HydrogenPeroxide { get { return _hydrogen_peroxide; } }
        public String Hydrazine { get { return _hydrazine; } }

        public String Methane { get { return _methane; } }
        public String NeonGas { get { return _neon_gas; } }
        public String Nitrogen { get { return _nitrogen; } }
        public String Nitrogen15 { get { return _nitrogen15; } }
        public String LqdOxygen { get { return _lqdOxygen; } }
        public String OxygenGas { get { return _oxygen_gas; } }

        public String Regolith { get { return _regolith; } }
        public String SolarWind { get { return _solarWind; } }
        public String LqdTritium { get { return _tritium; } }
        public String TritiumGas { get { return _tritium_gas; } }
        public String Water { get { return _water; } }
        public String HeavyWater { get { return _heavyWater; } }
        public String Xenon { get { return _xenon; } }
        public String XenonGas { get { return _xenongas; } }
        public String KryptonGas { get { return _krytongas; } }
        public String Krypton { get { return _kryton; } }

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

                UpdatePropertyWithConfigNode(pluginSettings, nameof(DeuteriumGas), value => DeuteriumGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(DeuteriumLqd), value => DeuteriumLqd = value);

                UpdatePropertyWithConfigNode(pluginSettings, nameof(FluorineGas), value => FluorineGas = value);

                UpdatePropertyWithConfigNode(pluginSettings, nameof(Helium4Gas), value => Helium4Gas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Helium4Lqd), value => Helium4Lqd = value);

                UpdatePropertyWithConfigNode(pluginSettings, nameof(HydrogenLqd), value => HydrogenLqd = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(HydrogenGas), value => HydrogenGas = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Lithium6), value => Lithium6 = value);
                UpdatePropertyWithConfigNode(pluginSettings, nameof(Lithium7), value => Lithium7 = value);

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



                if (pluginSettings.HasValue("Helium3GasResourceName"))
                {
                    _helium3_gas = pluginSettings.GetValue("Helium3GasResourceName");
                    Debug.Log("[KSPI]: Helium3 Gas resource name set to " + Helium3Gas);
                }
                //if (pluginSettings.HasValue("HeliumResourceName"))
                //{
                //    _liquid_helium4 = pluginSettings.GetValue("HeliumResourceName");
                //    Debug.Log("[KSPI]: Helium4 Liquid resource name set to " + LqdHelium4);
                //}
                if (pluginSettings.HasValue("Helium3ResourceName"))
                {
                    _liquid_helium3 = pluginSettings.GetValue("Helium3ResourceName");
                    Debug.Log("[KSPI]: Helium3 resource name set to " + LqdHelium3);
                }
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
                if (pluginSettings.HasValue("MethaneResourceName"))
                {
                    _methane = pluginSettings.GetValue("MethaneResourceName");
                    Debug.Log("[KSPI]: Methane resource name set to " + Methane);
                }
                if (pluginSettings.HasValue("NeonResourceName"))
                {
                    _neon_gas = pluginSettings.GetValue("NeonResourceName");
                    Debug.Log("[KSPI]: Neon resource name set to " + NeonGas);
                }
                if (pluginSettings.HasValue("NitrogenResourceName"))
                {
                    _nitrogen = pluginSettings.GetValue("NitrogenResourceName");
                    Debug.Log("[KSPI]: Nitrogen resource name set to " + Nitrogen);
                }
                if (pluginSettings.HasValue("OxygenResourceName"))
                {
                    _lqdOxygen = pluginSettings.GetValue("OxygenResourceName");
                    Debug.Log("[KSPI]: Oxygen resource name set to " + LqdOxygen);
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
                if (pluginSettings.HasValue("WaterResourceName"))
                {
                    _water = pluginSettings.GetValue("WaterResourceName");
                    Debug.Log("[KSPI]: Water resource name set to " + Water);
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
