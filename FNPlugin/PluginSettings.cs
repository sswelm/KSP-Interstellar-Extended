using FNPlugin.Constants;
using System;
using UnityEngine;

namespace FNPlugin
{
    public class PluginSettings
    {
        private static PluginSettings _config;

        public static PluginSettings Config => _config ?? (_config = new PluginSettings(PluginHelper.PluginSettingsConfig));

        // integers
        public int HoursInDay { get; private set; } = GameConstants.KEBRIN_HOURS_DAY;
        public int MicrowaveApertureDiameterMult { get; private set; } = 10;
        public int SpeedOfLight { get; private set; } = 299792458;
        public int SecondsInDay { get; private set; } = GameConstants.KEBRIN_DAY_SECONDS;
        public int SecondsInHour => GameConstants.SECONDS_IN_HOUR;


        // doubles
        public double AirflowHeatMult { get; private set; } = GameConstants.AirflowHeatMultiplier;
        public double AnthraquinoneEnergyPerTon { get; private set; } = GameConstants.anthraquinoneEnergyPerTon;
        public double AluminiumElectrolysisEnergyPerTon { get; private set; } = GameConstants.aluminiumElectrolysisEnergyPerTon;
        public double BaseAnthraquiononePowerConsumption { get; private set; } = GameConstants.baseAnthraquiononePowerConsumption;
        public double BasePowerConsumption { get; private set; } = GameConstants.basePowerConsumption;
        public double BaseAMFPowerConsumption { get; private set; } = GameConstants.baseAMFPowerConsumption;
        public double BaseCentriPowerConsumption { get; private set; } = GameConstants.baseCentriPowerConsumption;
        public double BaseELCPowerConsumption { get; private set; } = GameConstants.baseELCPowerConsumption;
        public double BaseHaberProcessPowerConsumption { get; private set; } = GameConstants.baseHaberProcessPowerConsumption;
        public double BasePechineyUgineKuhlmannPowerConsumption { get; private set; } = GameConstants.basePechineyUgineKuhlmannPowerConsumption;
        public double BaseUraniumAmmonolysisPowerConsumption { get; private set; } = GameConstants.baseUraniumAmmonolysisPowerConsumption;
        public double ElectrolysisEnergyPerTon { get; private set; } = GameConstants.waterElectrolysisEnergyPerTon;
        public double ElectricEngineIspMult { get; private set; } = 1;
        public double ElectricEngineAtmosphericDensityThrustLimiter { get; private set; }
        public double GlobalMagneticNozzlePowerMaxThrustMult { get; private set; } = 1;
        public double GlobalThermalNozzlePowerMaxThrustMult { get; private set; } = 1;
        public double GlobalElectricEnginePowerMaxThrustMult { get; private set; } = 1;
        public double HaberProcessEnergyPerTon { get; private set; } = GameConstants.haberProcessEnergyPerTon;
        public double HighCoreTempThrustMult { get; private set; } = GameConstants.HighCoreTempThrustMultiplier;
        public double IspCoreTempMult { get; private set; } = GameConstants.IspCoreTemperatureMultiplier;
        public double LowCoreTempBaseThrust { get; private set; }
        public double MinAtmosphericAirDensity { get; private set; }
        public double PechineyUgineKuhlmannEnergyPerTon { get; private set; } = GameConstants.pechineyUgineKuhlmannEnergyPerTon;
        public double PowerConsumptionMultiplier { get; private set; } = 1;
        public double RadiatorAreaMultiplier { get; private set; } = 2;
        public double MaxThermalNozzleIsp { get; private set; } = GameConstants.MaxThermalNozzleIsp;
        public double SpotsizeMult { get; private set; } = 1.22;
        public double ThrustCoreTempThreshold { get; private set; }
        public double MaxResourceProcessingTimewarp { get; private set; } = 20;
        public double ConvectionMultiplier { get; private set; } = 0.01;

        // https://www.engineersedge.com/heat_transfer/convective_heat_transfer_coefficients__13378.htm
        //static public double airHeatTransferCoefficient = 0.001; // 100W/m2/K, range: 10 - 100, "Air"
        //static public double lqdHeatTransferCoefficient = 0.01; // 1000/m2/K, range: 100-1200, "Water in Free Convection"
        public double AirHeatTransferCoefficient { get; private set; } = 0.001;
        public double LqdHeatTransferCoefficient { get; private set; } = 0.01;

        // Jet Upgrade Techs
        public string JetUpgradeTech1 { get; private set; } = "";
        public string JetUpgradeTech2 { get; private set; } = "";
        public string JetUpgradeTech3 { get; private set; } = "";
        public string JetUpgradeTech4 { get; private set; } = "";
        public string JetUpgradeTech5 { get; private set; } = "";


        public PluginSettings(ConfigNode pluginSettings)
        {
            UpdateIntWithConfigNode(pluginSettings, nameof(HoursInDay), value => HoursInDay = value);
            UpdateIntWithConfigNode(pluginSettings, nameof(MicrowaveApertureDiameterMult), value => MicrowaveApertureDiameterMult = value);
            UpdateIntWithConfigNode(pluginSettings, nameof(SpeedOfLight), value => SpeedOfLight = value);
            UpdateIntWithConfigNode(pluginSettings, nameof(SecondsInDay), value => SecondsInDay = value);

            UpdateDoubleWithConfigNode(pluginSettings, nameof(AirflowHeatMult), value => AirflowHeatMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(AnthraquinoneEnergyPerTon), value => AnthraquinoneEnergyPerTon = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(AluminiumElectrolysisEnergyPerTon), value => AluminiumElectrolysisEnergyPerTon = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BaseAnthraquiononePowerConsumption), value => BaseAnthraquiononePowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BasePowerConsumption), value => BasePowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BaseAMFPowerConsumption), value => BaseAMFPowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BaseCentriPowerConsumption), value => BaseCentriPowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BaseELCPowerConsumption), value => BaseELCPowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BaseHaberProcessPowerConsumption), value => BaseHaberProcessPowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BasePechineyUgineKuhlmannPowerConsumption), value => BasePechineyUgineKuhlmannPowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(BaseUraniumAmmonolysisPowerConsumption), value => BaseUraniumAmmonolysisPowerConsumption = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(ElectrolysisEnergyPerTon), value => ElectrolysisEnergyPerTon = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(ElectricEngineIspMult), value => ElectricEngineIspMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(ElectricEngineAtmosphericDensityThrustLimiter), value => ElectricEngineAtmosphericDensityThrustLimiter = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(GlobalMagneticNozzlePowerMaxThrustMult), value => GlobalMagneticNozzlePowerMaxThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(GlobalThermalNozzlePowerMaxThrustMult), value => GlobalThermalNozzlePowerMaxThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(GlobalElectricEnginePowerMaxThrustMult), value => GlobalThermalNozzlePowerMaxThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(HaberProcessEnergyPerTon), value => HaberProcessEnergyPerTon = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(HighCoreTempThrustMult), value => HighCoreTempThrustMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(IspCoreTempMult), value => IspCoreTempMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(LowCoreTempBaseThrust), value => LowCoreTempBaseThrust = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(MinAtmosphericAirDensity), value => MinAtmosphericAirDensity = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(PechineyUgineKuhlmannEnergyPerTon), value => PechineyUgineKuhlmannEnergyPerTon = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(PowerConsumptionMultiplier), value => PowerConsumptionMultiplier = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(RadiatorAreaMultiplier), value => RadiatorAreaMultiplier = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(MaxThermalNozzleIsp), value => MaxThermalNozzleIsp = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(SpotsizeMult), value => SpotsizeMult = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(ThrustCoreTempThreshold), value => ThrustCoreTempThreshold = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(MaxResourceProcessingTimewarp), value => MaxResourceProcessingTimewarp = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(ConvectionMultiplier), value => ConvectionMultiplier = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(AirHeatTransferCoefficient), value => AirHeatTransferCoefficient = value);
            UpdateDoubleWithConfigNode(pluginSettings, nameof(LqdHeatTransferCoefficient), value => LqdHeatTransferCoefficient = value);

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
