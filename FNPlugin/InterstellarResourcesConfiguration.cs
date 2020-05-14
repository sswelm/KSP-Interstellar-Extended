using System;
using UnityEngine;

namespace FNPlugin
{
    public class InterstellarResourcesConfiguration
    {
        private static InterstellarResourcesConfiguration _instance = null;

        public const String _ALIMINIUM = "Aluminium";
        public const String _ANTIMATTER = "Antimatter";
        public const String _INTAKEATMOSPHERE = "IntakeAtm";
        public const String _THORIUM_TETRAFLOURIDE = "ThF4";
        public const String _URANIUM_233 = "Uranium-233";
        public const String _URANIUM_NITRIDE = "UraniumNitride";
        public const String _ENRICHED_URANIUM = "EnrichedUranium";
        public const String _ACTINIDES = "Actinides";
        public const String _DEPLETED_FUEL = "DepletedFuel";
        public const String _VACUUM_PLASMA = "VacuumPlasma";
        public const String _EXOTIC_MATTER = "ExoticMatter";
        public const String _INTAKE_AIR = "IntakeAir";
        public const String _LITHIUM7 = "Lithium";
        public const String _LITHIUM6 = "Lithium6";
        public const String _PLUTONIUM_238 = "Plutonium-238";
        public const String _ALUMINA = "Alumina";

        public const String _DEUTERIUM_LIQUID = "LqdDeuterium";
        public const String _DEUTERIUM_GAS = "Deuterium";
        public const String _INTAKE_LIQUID = "IntakeLqd";
        public const String _ELECTRIC_CHARGE = "ElectricCharge";
        public const String _LIQUID_AMMONIA = "LqdAmmonia";
        public const String _LIQUID_ARGON = "LqdArgon";
        public const String _LIQUID_CO2 = "LqdCO2";

        public const String _CARBONMONOXIDE_LIQUID = "LqdCO";
        public const String _CARBONMONOXIDE_GAS = "CarbonMonoxide";

        public const String _CHLORINE = "Chlorine";
        public const String _LIQUID_METHANE = "LqdMethane";

        public const String _HELIUM4_LIQUID = "LqdHelium";
        public const String _HELIUM4_GAS = "Helium";
        public const String _HELIUM3_LIQUID = "LqdHe3";
        public const String _HELIUM3_GAS = "Helium3";
        public const String _HYDROGEN_LIQUID = "LqdHydrogen";
        public const String _HYDROGEN_GAS = "Hydrogen";
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

        private String _uranium_TerraFloride = "UF4";
        private String _aluminium = _ALIMINIUM;
        private String _ammonia = _LIQUID_AMMONIA;
        private String _argon = _LIQUID_ARGON;
        private String _carbonDioxide = _LIQUID_CO2;
        private String _carbonMoxoxide = _CARBONMONOXIDE_LIQUID;
        private String _fluorineGas = "Fluorine";
        private String _helium4_gas = _HELIUM4_GAS;
        private String _liquid_helium4 = _HELIUM4_LIQUID;
        private String _helium3_gas = _HELIUM3_GAS;
        private String _liquid_helium3 = _HELIUM3_LIQUID;
        private String _sodium = "Sodium";
        private String _hydrogen = _HYDROGEN_LIQUID;
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
        private String _kryton = _LIQUID_KRYPTON;
        private String _krytongas = _KRYPTON_GAS;

        public String Actinides { get { return _ACTINIDES; } }
        public String Alumina { get { return _ALUMINA; } }
        public String Aluminium { get { return _aluminium; } }
        public String Ammonia { get { return _ammonia; } }
        public String Antimatter { get { return _ANTIMATTER; } }
        public String Argon { get { return _argon; } }
        public String CarbonDioxide { get { return _carbonDioxide; } }
        public String CarbonMoxoxide { get { return _carbonMoxoxide; } }
        public String DepletedFuel { get { return _DEPLETED_FUEL; } }
        public String LqdDeuterium { get { return _DEUTERIUM_LIQUID; } }
        public String DeuteriumGas { get { return _DEUTERIUM_GAS; } }
        public String ExoticMatter { get { return _EXOTIC_MATTER; } }
        public String ElectricCharge { get { return _ELECTRIC_CHARGE; } }
        public String FluorineGas { get { return _fluorineGas; } }
        public String LqdHelium4 { get { return _liquid_helium4; } }
        public String Helium4Gas { get { return _helium4_gas; } }
        public String LqdHelium3 { get { return _liquid_helium3; } }
        public String Sodium { get { return _sodium; } }
        public String Helium3Gas { get { return _helium3_gas; } }
        public String Hydrogen { get { return _hydrogen; } }
        public String HydrogenPeroxide { get { return _hydrogen_peroxide; } }
        public String Hydrazine { get { return _hydrazine; } }
        public String IntakeAtmosphere { get { return _INTAKEATMOSPHERE; } }
        public String IntakeLiquid { get { return _INTAKE_LIQUID; } }
        public String Lithium6 { get { return _LITHIUM6; } }
        public String IntakeAir { get { return _INTAKE_AIR; } }
        public String Lithium7 { get { return _LITHIUM7; } }
        public String Methane { get { return _methane; } }
        public String NeonGas { get { return _neon_gas; } }
        public String Nitrogen { get { return _nitrogen; } }
        public String Nitrogen15 { get { return _nitrogen15; } }
        public String LqdOxygen { get { return _lqdOxygen; } }
        public String OxygenGas { get { return _oxygen_gas; } }
        public String Plutonium238 { get { return _PLUTONIUM_238; } }
        public String Regolith { get { return _regolith; } }
        public String SolarWind { get { return _solarWind; } }
        public String ThoriumTetraflouride { get { return _THORIUM_TETRAFLOURIDE; } }
        public String LqdTritium { get { return _tritium; } }
        public String TritiumGas { get { return _tritium_gas; } }
        public String UraniumTetraflouride { get { return _uranium_TerraFloride; } }
        public String UraniumNitride { get { return _URANIUM_NITRIDE; } }
        public String Uranium233 { get { return _URANIUM_233; } }
        public String EnrichedUrarium { get { return _ENRICHED_URANIUM; } }
        public String VacuumPlasma { get { return _VACUUM_PLASMA; } }
        public String Water { get { return _water; } }
        public String HeavyWater { get { return _heavyWater; } }
        public String Xenon { get { return _xenon; } }
        public String XenonGas { get { return _xenongas; } }
        public String KryptonGas { get { return _krytongas; } }
        public String Krypton { get { return _kryton; } }

        public InterstellarResourcesConfiguration(ConfigNode plugin_settings)
        {
            if (plugin_settings != null)
            {
                if (plugin_settings.HasValue("AluminiumResourceName"))
                {
                    _aluminium = plugin_settings.GetValue("AluminiumResourceName");
                    Debug.Log("[KSPI]: Aluminium resource name set to " + Aluminium);
                }
                if (plugin_settings.HasValue("AmmoniaResourceName"))
                {
                    _ammonia = plugin_settings.GetValue("AmmoniaResourceName");
                    Debug.Log("[KSPI]: Ammonia resource name set to " + Ammonia);
                }
                if (plugin_settings.HasValue("ArgonResourceName"))
                {
                    _argon = plugin_settings.GetValue("ArgonResourceName");
                    Debug.Log("[KSPI]: Argon resource name set to " + Argon);
                }
                if (plugin_settings.HasValue("CarbonDioxideResourceName"))
                {
                    _carbonDioxide = plugin_settings.GetValue("CarbonDioxideResourceName");
                    Debug.Log("[KSPI]: CarbonDioxide resource name set to " + CarbonDioxide);
                }
                if (plugin_settings.HasValue("CarbonMonoxideResourceName"))
                {
                    _carbonMoxoxide = plugin_settings.GetValue("CarbonMonoxideResourceName");
                    Debug.Log("[KSPI]: CarbonMonoxide resource name set to " + CarbonMoxoxide);
                }
                if (plugin_settings.HasValue("Helium4GasResourceName"))
                {
                    _helium4_gas = plugin_settings.GetValue("Helium4GasResourceName");
                    Debug.Log("[KSPI]: Helium4 Gas resource name set to " + Helium4Gas);
                }
                if (plugin_settings.HasValue("Helium3GasResourceName"))
                {
                    _helium3_gas = plugin_settings.GetValue("Helium3GasResourceName");
                    Debug.Log("[KSPI]: Helium3 Gas resource name set to " + Helium3Gas);
                }
                if (plugin_settings.HasValue("HeliumResourceName"))
                {
                    _liquid_helium4 = plugin_settings.GetValue("HeliumResourceName");
                    Debug.Log("[KSPI]: Helium4 Liquid resource name set to " + LqdHelium4);
                }
                if (plugin_settings.HasValue("Helium3ResourceName"))
                {
                    _liquid_helium3 = plugin_settings.GetValue("Helium3ResourceName");
                    Debug.Log("[KSPI]: Helium3 resource name set to " + LqdHelium3);
                }
                if (plugin_settings.HasValue("HydrazineResourceName"))
                {
                    _hydrazine = plugin_settings.GetValue("HydrazineResourceName");
                    Debug.Log("[KSPI]: Hydrazine resource name set to " + Hydrazine);
                }
                if (plugin_settings.HasValue("HydrogenResourceName"))
                {
                    _hydrogen = plugin_settings.GetValue("HydrogenResourceName");
                    Debug.Log("[KSPI]: Hydrogen resource name set to " + Hydrogen);
                }
                if (plugin_settings.HasValue("HydrogenPeroxideResourceName"))
                {
                    _hydrogen_peroxide = plugin_settings.GetValue("HydrogenPeroxideResourceName");
                    Debug.Log("[KSPI]: Hydrogen Peroxide resource name set to " + HydrogenPeroxide);
                }

                if (plugin_settings.HasValue("MethaneResourceName"))
                {
                    _methane = plugin_settings.GetValue("MethaneResourceName");
                    Debug.Log("[KSPI]: Methane resource name set to " + Methane);
                }
                if (plugin_settings.HasValue("NeonResourceName"))
                {
                    _neon_gas = plugin_settings.GetValue("NeonResourceName");
                    Debug.Log("[KSPI]: Neon resource name set to " + NeonGas);
                }
                if (plugin_settings.HasValue("NitrogenResourceName"))
                {
                    _nitrogen = plugin_settings.GetValue("NitrogenResourceName");
                    Debug.Log("[KSPI]: Nitrogen resource name set to " + Nitrogen);
                }
                if (plugin_settings.HasValue("OxygenResourceName"))
                {
                    _lqdOxygen = plugin_settings.GetValue("OxygenResourceName");
                    Debug.Log("[KSPI]: Oxygen resource name set to " + LqdOxygen);
                }
                if (plugin_settings.HasValue("RegolithResourceName"))
                {
                    _regolith = plugin_settings.GetValue("RegolithResourceName");
                    Debug.Log("[KSPI]: Regolith resource name set to " + Regolith);
                }
                if (plugin_settings.HasValue("XenonGasResourceName"))
                {
                    _xenongas = plugin_settings.GetValue("XenonGasResourceName");
                    Debug.Log("[KSPI]: XenonGas resource name set to " + XenonGas);
                }
                if (plugin_settings.HasValue("SolarWindResourceName"))
                {
                    _solarWind = plugin_settings.GetValue("SolarWindResourceName");
                    Debug.Log("[KSPI]: SolarWind resource name set to " + SolarWind);
                }
                if (plugin_settings.HasValue("TritiumResourceName"))
                {
                    _tritium = plugin_settings.GetValue("TritiumResourceName");
                    Debug.Log("[KSPI]: Tritium resource name set to " + LqdTritium);
                }
                if (plugin_settings.HasValue("UraniumTetraflourideName"))
                {
                    _uranium_TerraFloride = plugin_settings.GetValue("UraniumTetraflourideName");
                    Debug.Log("[KSPI]: UraniumTetraflouride resource name set to " + _uranium_TerraFloride);
                }
                if (plugin_settings.HasValue("WaterResourceName"))
                {
                    _water = plugin_settings.GetValue("WaterResourceName");
                    Debug.Log("[KSPI]: Water resource name set to " + Water);
                }
                if (plugin_settings.HasValue("HeavyWaterResourceName"))
                {
                    _heavyWater = plugin_settings.GetValue("HeavyWaterResourceName");
                    Debug.Log("[KSPI]: Heavy Water resource name set to " + HeavyWater);
                }
            } 
            else
            {
                PluginHelper.showInstallationErrorMessage();
            }
        }

        public static InterstellarResourcesConfiguration Instance { get { return _instance ?? (_instance = new InterstellarResourcesConfiguration(PluginHelper.PluginSettingsConfig)); } }
    }
}
