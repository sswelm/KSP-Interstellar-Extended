using FNPlugin.Beamedpower;
using FNPlugin.Constants;
using FNPlugin.Wasteheat;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GameEventSubscriber : MonoBehaviour
    {
        void Start()
        {
            //GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
            //GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
            //GameEvents.onSetSpeedMode.Add(OnSetSpeedModeChange);
            //GameEvents.onVesselLoaded.Add(OnVesselLoaded);
            //GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);

            BeamedPowerSources.getVesselMicrowavePersistanceForProtoVesselCallback = BeamedPowerTransmitter.getVesselMicrowavePersistanceForProtoVessel;
            BeamedPowerSources.getVesselRelayPersistanceForProtoVesselCallback = BeamedPowerTransmitter.getVesselRelayPersistanceForProtoVessel;
            BeamedPowerSources.getVesselMicrowavePersistanceForVesselCallback = BeamedPowerTransmitter.getVesselMicrowavePersistanceForVessel;
            BeamedPowerSources.getVesselRelayPersistenceForVesselCallback = BeamedPowerTransmitter.getVesselRelayPersistenceForVessel;

            GameEvents.onGameStateSaved.Add(OnGameStateSaved);
            GameEvents.onDockingComplete.Add(OnDockingComplete);
            GameEvents.onPartDeCoupleComplete.Add(OnPartDeCoupleComplete);
            GameEvents.onVesselSOIChanged.Add(OmVesselSOIChanged);

            Debug.Log("[KSPI]: GameEventSubscriber Initialised");
        }
        void OnDestroy()
        {
            //GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
            //GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
            //GameEvents.onSetSpeedMode.Remove(OnSetSpeedModeChange);
            //GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
            //GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);

            GameEvents.onGameStateSaved.Remove(OnGameStateSaved);
            GameEvents.onDockingComplete.Remove(OnDockingComplete);
            GameEvents.onPartDeCoupleComplete.Remove(OnPartDeCoupleComplete);
            GameEvents.onVesselSOIChanged.Remove(OmVesselSOIChanged);

            var kerbalismversionstr = string.Format("{0}.{1}.{2}.{3}", Kerbalism.versionMajor, Kerbalism.versionMajorRevision, Kerbalism.versionMinor, Kerbalism.versionMinorRevision);

            if (Kerbalism.versionMajor > 0)
                Debug.Log("[KSPI]: Loaded Kerbalism " + kerbalismversionstr);

            Debug.Log("[KSPI]: GameEventSubscriber Deinitialised");
        }

        void OnGameStateSaved(Game game)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnGameStateSaved");
            PluginHelper.LoadSaveFile();
        }

        void OnDockingComplete(GameEvents.FromToAction<Part, Part> fromToAction)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnDockingComplete");

            ResourceOvermanager.Reset();
            SupplyPriorityManager.Reset();

            ResetReceivers();
        }

        void  OmVesselSOIChanged (GameEvents.HostedFromToAction<Vessel, CelestialBody> gameEvent)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OmVesselSOIChanged");
            gameEvent.host.FindPartModulesImplementing<ElectricEngineControllerFX>().ForEach(e => e.VesselChangedSOI());
            gameEvent.host.FindPartModulesImplementing<ModuleEnginesWarp>().ForEach(e => e.VesselChangedSOI());
            gameEvent.host.FindPartModulesImplementing<DaedalusEngineController>().ForEach(e => e.VesselChangedSOI());
            gameEvent.host.FindPartModulesImplementing<AlcubierreDrive>().ForEach(e => e.VesselChangedSOI());
        }

        void  OnPartDeCoupleComplete (Part part)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnPartDeCoupleComplete");

            ResourceOvermanager.Reset();
            SupplyPriorityManager.Reset();
            FNRadiator.Reset();

            ResetReceivers();
        }

        private static void ResetReceivers()
        {
            foreach (var currentvessel in FlightGlobals.Vessels)
            {
                if (currentvessel.loaded)
                {
                    var receivers = currentvessel.FindPartModulesImplementing<BeamedPowerReceiver>();

                    foreach (var receiver in receivers)
                    {
                        Debug.Log("[KSPI]: OnDockingComplete - Restart receivers " + receiver.Part.name);
                        receiver.Restart(50);
                    }
                }
            }
        }
    }



    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PluginHelper : MonoBehaviour
    {
        public const string WARP_PLUGIN_SETTINGS_FILEPATH = "WarpPlugin/WarpPluginSettings/WarpPluginSettings";

        public static bool using_toolbar = false;
        protected static bool resources_configured = false;

        private static Dictionary<string, RDTech> rdTechByName;

        public static Dictionary<string, RDTech> RDTechByName 
        {
            get
            {
                if (rdTechByName == null)
                {
                    rdTechByName = new Dictionary<string, RDTech>();

                    // catalog part upgrades
                    ConfigNode[] techtree = GameDatabase.Instance.GetConfigNodes("TechTree");
                    Debug.Log("[KSPI]: PluginHelper found: " + techtree.Count() + " TechTrees");

                    for (int i = 0; i < techtree.Length; i++)
                    {
                        var techtreeConfig = techtree[i];

                        var technodes = techtreeConfig.nodes;

                        Debug.Log("[KSPI]: PluginHelper found: " + technodes.Count + " Technodes");
                        for (int j = 0; j < technodes.Count; j++)
                        {
                            var technode = technodes[j];

                            var tech = new RDTech();
                            tech.techID = technode.GetValue("id");
                            tech.title = technode.GetValue("title");

                            Debug.Log("[KSPI]: PluginHelper technode id: " + tech.techID + " title: " + tech.title);
                            rdTechByName.Add(tech.techID, tech);
                        }
                    }
                }
                return rdTechByName;
            }
        }

        private static Dictionary<string, PartUpgradeHandler.Upgrade> partUpgradeByName;

        public static Dictionary<string, PartUpgradeHandler.Upgrade> PartUpgradeByName 
        { 
            get 
            {
                if (partUpgradeByName == null)
                {
                    partUpgradeByName = new Dictionary<string, PartUpgradeHandler.Upgrade>();

                    // catalog part upgrades
                    ConfigNode[] partupgradeNodes = GameDatabase.Instance.GetConfigNodes("PARTUPGRADE");
                    Debug.Log("[KSPI]: PluginHelper found: " + partupgradeNodes.Count() + " Part upgrades");

                    for (int i = 0; i < partupgradeNodes.Length; i++)
                    {
                        var partUpgradeConfig = partupgradeNodes[i];

                        var partUpgrade = new PartUpgradeHandler.Upgrade();
                        partUpgrade.name = partUpgradeConfig.GetValue("name");
                        partUpgrade.techRequired = partUpgradeConfig.GetValue("techRequired");

                        Debug.Log("[KSPI]: PluginHelper indexed PARTUPGRADE " + partUpgrade.name + " with techRequired " + partUpgrade.techRequired);

                        partUpgradeByName.Add(partUpgrade.name, partUpgrade);
                    }
                }

                return partUpgradeByName;
            } 
        }

        private static Dictionary<string, ProtoTechNode> techNodeByName = new Dictionary<string, ProtoTechNode>();

        private static Dictionary<string, string> translationsByName;

        static Dictionary<string, string> TranslationsByName
        {
            get
            {
                if (translationsByName == null)
                {
                    translationsByName = new Dictionary<string, string>();

                    ConfigNode[] localizationConfigNodes = GameDatabase.Instance.GetConfigNodes("Localization");
                    Debug.Log("[KSPI]: PluginHelper loaded " + localizationConfigNodes.Count() + " Localization files");

                    for (int i = 0; i < localizationConfigNodes.Length; i++)
                    {
                        var localizationConfig = localizationConfigNodes[i];
                        var localization = localizationConfig.nodes;

                        for (int j = 0; j < localization.Count; j++)
                        {
                            var node = localization[j];

                            if (node.name == "en-us")
                            {
                                var translations = node.values;

                                Debug.Log("[KSPI]: PluginHelper translations: " + translations.Count);

                                for (int k = 0; k < translations.Count; k++)
                                {
                                    var translation = translations[k];

                                    translationsByName.Add(translation.name, translation.value);
                                }
                            }
                        }
                    }
                }

                return translationsByName;
            }
        }
        
        static protected bool buttonAdded;
        static protected Texture2D appIcon = null;
        static protected ApplicationLauncherButton appLauncherButton = null;

        #region static Properties

        public static bool TechnologyIsInUse
        {
            get { return (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX); }
        }

        public static ConfigNode PluginSettingsConfig
        {
            get { return GameDatabase.Instance.GetConfigNode(WARP_PLUGIN_SETTINGS_FILEPATH); }
        }

        public static string PluginSaveFilePath
        {
            get { return KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/WarpPlugin.cfg"; }
        }

        public static string PluginSettingsFilePath
        {
            get { return KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/WarpPluginSettings.cfg"; }
        }

        public static Dictionary<string, string> PartTechUpgrades { get; private set; }

        public static Dictionary<string, string> OrsResourceMappings { get; private set; }

        private static KeyCode _thermalUiKey = KeyCode.I;
        public static KeyCode ThermalUiKey { get { return _thermalUiKey; } }

        private static int _secondsInDay = GameConstants.KEBRIN_DAY_SECONDS;
        public static int SecondsInDay { get { return _secondsInDay; } }
        public static int HoursInDay { get { return GameConstants.KEBRIN_HOURS_DAY; } }
        public static int SecondsInHour { get { return GameConstants.SECONDS_IN_HOUR; } }

        private static double _spotsizeMult = 1.22;
        public static double SpotsizeMult { get { return _spotsizeMult; } }

        private static double _microwaveApertureDiameterMult = 10;
        public static double MicrowaveApertureDiameterMult { get { return _microwaveApertureDiameterMult; } }

        private static double _speedOfLight = 29979245.8;
        public static double SpeedOfLight { get { return GameConstants.speedOfLight * _speedOfLightMult; } } 

        private static double _speedOfLightMult = 0.1 ;
        public static double SpeedOfLightMult { get { return _speedOfLightMult; } }
 
        private static double _maxAtmosphericAltitudeMult = 1;
        public static double MaxAtmosphericAltitudeMult { get { return _maxAtmosphericAltitudeMult; } }

        private static double _minAtmosphericAirDensity = 0;
        public static double MinAtmosphericAirDensity { get { return _minAtmosphericAirDensity; } }

        private static double _ispCoreTempMult = GameConstants.IspCoreTemperatureMultiplier;
        public static double IspCoreTempMult { get { return _ispCoreTempMult; } }

        private static double _lowCoreTempBaseThrust = 0;
        public static double LowCoreTempBaseThrust { get { return _lowCoreTempBaseThrust; } }

        private static double _highCoreTempThrustMult = GameConstants.HighCoreTempThrustMultiplier;
        public static double HighCoreTempThrustMult { get { return _highCoreTempThrustMult; } }

        private static double _thrustCoreTempThreshold = 0;
        public static double ThrustCoreTempThreshold { get { return _thrustCoreTempThreshold; } }

        private static double _globalThermalNozzlePowerMaxThrustMult = 1;
        public static double GlobalThermalNozzlePowerMaxThrustMult { get { return _globalThermalNozzlePowerMaxThrustMult; } }

        private static double _globalMagneticNozzlePowerMaxThrustMult = 1;
        public static double GlobalMagneticNozzlePowerMaxThrustMult { get { return _globalMagneticNozzlePowerMaxThrustMult; } }

        private static double _globalElectricEnginePowerMaxThrustMult = 1;
        public static double GlobalElectricEnginePowerMaxThrustMult { get { return _globalElectricEnginePowerMaxThrustMult; } }

        private static double _maxPowerDrawForExoticMatterMult = 1;
        public static double MaxPowerDrawForExoticMatterMult { get { return _maxPowerDrawForExoticMatterMult; } }

        private static double _electricEngineIspMult = 1;
        public static double ElectricEngineIspMult { get { return _electricEngineIspMult; } }

        private static double _electricEnginePowerPropellantIspMultLimiter = 1;
        public static double ElectricEnginePowerPropellantIspMultLimiter { get { return _electricEnginePowerPropellantIspMultLimiter; } }

        private static double _electricEngineAtmosphericDensityThrustLimiter = 0;
        public static double ElectricEngineAtmosphericDensityThrustLimiter { get { return _electricEngineAtmosphericDensityThrustLimiter; } }

        //------------------------------------------------------------------------------------------

        private static double _basePowerConsumption = GameConstants.basePowerConsumption;
        public static double BasePowerConsumption { get { return PowerConsumptionMultiplier * _basePowerConsumption; } }

        private static double _baseAMFPowerConsumption = GameConstants.baseAMFPowerConsumption;
        public static double BaseAMFPowerConsumption { get { return PowerConsumptionMultiplier * _baseAMFPowerConsumption; } }

        private static double _baseCentriPowerConsumption = GameConstants.baseCentriPowerConsumption;
        public static double BaseCentriPowerConsumption { get { return PowerConsumptionMultiplier * _baseCentriPowerConsumption; } }

        private static double _baseELCPowerConsumption = GameConstants.baseELCPowerConsumption;
        public static double BaseELCPowerConsumption { get { return PowerConsumptionMultiplier * _baseELCPowerConsumption; } }

        private static double _baseAnthraquiononePowerConsumption = GameConstants.baseAnthraquiononePowerConsumption;
        public static double BaseAnthraquiononePowerConsumption { get { return PowerConsumptionMultiplier * _baseAnthraquiononePowerConsumption; } }

        private static double _basePechineyUgineKuhlmannPowerConsumption = GameConstants.basePechineyUgineKuhlmannPowerConsumption;
        public static double BasePechineyUgineKuhlmannPowerConsumption { get { return PowerConsumptionMultiplier * _basePechineyUgineKuhlmannPowerConsumption; } }

        private static double _baseHaberProcessPowerConsumption = GameConstants.baseHaberProcessPowerConsumption;
        public static double BaseHaberProcessPowerConsumption { get { return PowerConsumptionMultiplier * _baseHaberProcessPowerConsumption; } }

        private static double _baseUraniumAmmonolysisPowerConsumption = GameConstants.baseUraniumAmmonolysisPowerConsumption;
        public static double BaseUraniumAmmonolysisPowerConsumption { get { return PowerConsumptionMultiplier * _baseUraniumAmmonolysisPowerConsumption; } }

        //------------------------------------------------------------------------------------------------

        private static double _anthraquinoneEnergyPerTon = GameConstants.anthraquinoneEnergyPerTon;
        public static double AnthraquinoneEnergyPerTon { get { return PowerConsumptionMultiplier * _anthraquinoneEnergyPerTon; } }

        private static double _haberProcessEnergyPerTon = GameConstants.haberProcessEnergyPerTon;
        public static double HaberProcessEnergyPerTon { get { return PowerConsumptionMultiplier * _haberProcessEnergyPerTon; } }

        private static double _electrolysisEnergyPerTon = GameConstants.waterElectrolysisEnergyPerTon;
        public static double ElectrolysisEnergyPerTon { get { return PowerConsumptionMultiplier * _electrolysisEnergyPerTon; } }

        private static double _aluminiumElectrolysisEnergyPerTon = GameConstants.aluminiumElectrolysisEnergyPerTon;
        public static double AluminiumElectrolysisEnergyPerTon { get { return PowerConsumptionMultiplier * _aluminiumElectrolysisEnergyPerTon; } }

        private static double _pechineyUgineKuhlmannEnergyPerTon = GameConstants.pechineyUgineKuhlmannEnergyPerTon;
        public static double PechineyUgineKuhlmannEnergyPerTon { get { return PowerConsumptionMultiplier * _pechineyUgineKuhlmannEnergyPerTon; } }


        private static double _powerConsumptionMultiplier = 1;
        public static double PowerConsumptionMultiplier { get { return _powerConsumptionMultiplier; } }

        //----------------------------------------------------------------------------------------------

        private static double _ispElectroPropellantModifierBase = 0;
        public static double IspElectroPropellantModifierBase { get { return _ispElectroPropellantModifierBase; } }

        private static float _maxThermalNozzleIsp = GameConstants.MaxThermalNozzleIsp;
        public static float MaxThermalNozzleIsp { get { return _maxThermalNozzleIsp; } }

        private static double _airflowHeatMult = GameConstants.AirflowHeatMultiplier;
        public static double AirflowHeatMult { get { return _airflowHeatMult; } }

        private static double _engineHeatProduction = GameConstants.EngineHeatProduction;
        public static double EngineHeatProduction { get { return _engineHeatProduction; } }

        private static bool _isPanelHeatingClamped = false;
        public static bool IsSolarPanelHeatingClamped { get { return _isPanelHeatingClamped; } }

        private static bool _isThermalDissipationDisabled = false;
        public static bool IsThermalDissipationDisabled { get { return _isThermalDissipationDisabled; } }

        private static bool _isRecieverTempTweaked = false;
        public static bool IsRecieverCoreTempTweaked { get { return _isRecieverTempTweaked; } }

        private static bool _limitedWarpTravel = false;
        public static bool LimitedWarpTravel { get { return _limitedWarpTravel; } }

        private static bool _radiationMechanicsDisabled = false;
        public static bool RadiationMechanicsDisabled { get { return _radiationMechanicsDisabled; } }

        private static bool _matchDemandWithSupply = false;
        public static bool MatchDemandWithSupply { get { return _matchDemandWithSupply; } }

        // Jet Upgrade Techs

        private static string _jetUpgradeTech1 = String.Empty;
        public static string JetUpgradeTech1 { get { return _jetUpgradeTech1; } private set { _jetUpgradeTech1 = value; } }

        private static string _jetUpgradeTech2 = String.Empty;
        public static string JetUpgradeTech2 { get { return _jetUpgradeTech2; } private set { _jetUpgradeTech2 = value; } }

        private static string _jetUpgradeTech3 = String.Empty;
        public static string JetUpgradeTech3 { get { return _jetUpgradeTech3; } private set { _jetUpgradeTech3 = value; } }

        private static string _jetUpgradeTech4 = String.Empty;
        public static string JetUpgradeTech4 { get { return _jetUpgradeTech4; } private set { _jetUpgradeTech4 = value; } }

        private static string _jetUpgradeTech5 = String.Empty;
        public static string JetUpgradeTech5 { get { return _jetUpgradeTech5; } private set { _jetUpgradeTech5 = value; } }

        // RadiatorAreaMultiplier

        private static double _radiatorAreaMultiplier = 2;
        public static double RadiatorAreaMultiplier { get { return _radiatorAreaMultiplier; } private set { _radiatorAreaMultiplier = value; } }

        //// Radiator Upgrade Techs

        //private static string _radiatorUpgradeTech1 = "heatManagementSystems";
        //public static string RadiatorUpgradeTech1 { get { return _radiatorUpgradeTech1; } private set { _radiatorUpgradeTech1 = value; } }

        //private static string _radiatorUpgradeTech2 = "advHeatManagement";
        //public static string RadiatorUpgradeTech2 { get { return _radiatorUpgradeTech2; } private set { _radiatorUpgradeTech2 = value; } }

        //private static string _radiatorUpgradeTech3 = "specializedRadiators";
        //public static string RadiatorUpgradeTech3 { get { return _radiatorUpgradeTech3; } private set { _radiatorUpgradeTech3 = value; } }

        //private static string _radiatorUpgradeTech4 = "exoticRadiators";
        //public static string RadiatorUpgradeTech4 { get { return _radiatorUpgradeTech4; } private set { _radiatorUpgradeTech4 = value; } }

        //private static string _radiatorUpgradeTech5 = "extremeRadiators";
        //public static string RadiatorUpgradeTech5 { get { return _radiatorUpgradeTech5; } private set { _radiatorUpgradeTech5 = value; } }


        //private static double _radiatorTemperatureMk1 = 1850;
        //public static double RadiatorTemperatureMk1 { get { return _radiatorTemperatureMk1; } private set { _radiatorTemperatureMk1 = value; } }

        //private static double _radiatorTemperatureMk2 = 2200;
        //public static double RadiatorTemperatureMk2 { get { return _radiatorTemperatureMk2; } private set { _radiatorTemperatureMk2 = value; } }

        //private static double _radiatorTemperatureMk3 = 2616;
        //public static double RadiatorTemperatureMk3 { get { return _radiatorTemperatureMk3; } private set { _radiatorTemperatureMk3 = value; } }

        //private static double _radiatorTemperatureMk4 = 3111;
        //public static double RadiatorTemperatureMk4 { get { return _radiatorTemperatureMk4; } private set { _radiatorTemperatureMk4 = value; } }

        //private static double _radiatorTemperatureMk5 = 3700;
        //public static double RadiatorTemperatureMk5 { get { return _radiatorTemperatureMk5; } private set { _radiatorTemperatureMk5 = value; } }

        //private static double _radiatorTemperatureMk6 = 4400;
        //public static double RadiatorTemperatureMk6 { get { return _radiatorTemperatureMk6; } private set { _radiatorTemperatureMk6 = value; } }

        #endregion

        public static string formatMassStr(double mass, string format = "0.000000")
        {
            if (mass >= 1)
                return FormatString(mass / 1e+0, format) + " t";
            else if (mass >= 1e-3)
                return FormatString(mass / 1e-3, format) + " kg";
            else if (mass >= 1e-6)
                return FormatString(mass / 1e-6, format) + " g";
            else if (mass >= 1e-9)
                return FormatString(mass / 1e-9, format)+ " mg";
            else if (mass >= 1e-12)
                return FormatString(mass / 1e-12, format) + " ug";
            else if (mass >= 1e-15)
                return FormatString(mass / 1e-15, format) + " ng";
            else
                return FormatString(mass / 1e-18, format) + " pg";
        }

        private static string FormatString(double value,  string format)
        {
            if (format == null)
                return value.ToString();
            else
                return value.ToString(format);
        }

        public static bool HasTechRequirementOrEmpty(string techName)
        {
            return techName == String.Empty || PluginHelper.UpgradeAvailable(techName);
        }

        public static bool HasTechRequirementAndNotEmpty(string techName)
        {
            return techName != String.Empty && PluginHelper.UpgradeAvailable(techName);
        }

        public static Dictionary<string, string> TechTitleById;

        public static string GetTechTitleById(string id)
        {
            string result = ResearchAndDevelopment.GetTechnologyTitle(id);
            if (!String.IsNullOrEmpty(result))
                return result;

            PartUpgradeHandler.Upgrade partUpgrade;
            UnityEngine.Debug.Log("[KSPI]: lookup partUpgradeId " + id);
            if (PartUpgradeByName.TryGetValue(id, out partUpgrade))
            {
                UnityEngine.Debug.Log("[KSPI]: found partUpgradeId " + id + " now looking for " + partUpgrade.techRequired);

                RDTech upgradeTechnode;
                if (RDTechByName.TryGetValue(partUpgrade.techRequired, out upgradeTechnode))
                {
                    UnityEngine.Debug.Log("[KSPI]: found partUpgrade techRequired title " + upgradeTechnode.title);
                    return upgradeTechnode.title;
                }
                else
                    UnityEngine.Debug.LogWarning("[KSPI]: failed to find partUpgrade techRequired title for " + partUpgrade.techRequired);
            }
            else
                UnityEngine.Debug.LogWarning("[KSPI]: failed to find partUpgradeId " + id);

            RDTech technode;
            if (RDTechByName.TryGetValue(id, out technode))
            {
                UnityEngine.Debug.Log("[KSPI]: found title for tech " + id );
                return technode.title;
            }

            return id;
        }

        private static bool hasTech(string id)
        {
            if (String.IsNullOrEmpty(id) || id == "none")
                return false;

            if (ResearchAndDevelopment.Instance == null)
                return HasTechFromSaveFile(id);

            var techstate = ResearchAndDevelopment.Instance.GetTechState(id);
            if (techstate != null)
            {
                var available = techstate.state == RDTech.State.Available;
                if (available)
                    UnityEngine.Debug.Log("[KSPI]: found techid " + id + " available");
                else
                    UnityEngine.Debug.Log("[KSPI]: found techid " + id + " unavailable");
                return available;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[KSPI]: did not find techid " + id + " in techtree");
                return false;
            }
        }

        private static HashSet<string> researchedTechs;

        public static void LoadSaveFile()
        {
            researchedTechs = new HashSet<string>();

            string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";

            ConfigNode config = ConfigNode.Load(persistentfile);
            ConfigNode gameconf = config.GetNode("GAME");
            ConfigNode[] scenarios = gameconf.GetNodes("SCENARIO");

            foreach (ConfigNode scenario in scenarios)
            {
                if (scenario.GetValue("name") == "ResearchAndDevelopment")
                {
                    ConfigNode[] techs = scenario.GetNodes("Tech");
                    foreach (ConfigNode technode in techs)
                    {
                        var technodename = technode.GetValue("id");
                        researchedTechs.Add(technodename);
                    }
                }
            }
        }

        private static bool HasTechFromSaveFile(string techid)
        {
            if (researchedTechs == null)
                LoadSaveFile();

            bool found = researchedTechs.Contains(techid);
            if (found)
                UnityEngine.Debug.Log("[KSPI]: found techid " + techid + " in saved hash");
            else
                UnityEngine.Debug.Log("[KSPI]: we did not find techid " + techid + " in saved hash");

            return found;
        }

        public static bool UpgradeAvailable(string id)
        {
            if (String.IsNullOrEmpty(id))
                return false;

            if (id == "true" || id == "always")
                return true;

            if (id == "false" || id == "none")
                return false;

            PartUpgradeHandler.Upgrade partUpgrade;
            if (PluginHelper.PartUpgradeByName.TryGetValue(id, out partUpgrade))
            {
                UnityEngine.Debug.Log("[KSPI]: found PARTUPGRADE " + id + ", checking techRequired " + partUpgrade.techRequired);
                id = partUpgrade.techRequired;
            }

            if (HighLogic.CurrentGame != null)
            {
                if (TechnologyIsInUse)
                    return hasTech(id);
                else
                    return true;
            }
            return false;
        }

        public static float getKerbalRadiationDose(int kerbalidx)
        {
            try
            {
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode crew_roster = gameconf.GetNode("ROSTER");
                ConfigNode[] crew = crew_roster.GetNodes("CREW");
                ConfigNode sought_kerbal = crew[kerbalidx];
                if (sought_kerbal.HasValue("totalDose"))
                {
                    float dose = float.Parse(sought_kerbal.GetValue("totalDose"));
                    return dose;
                }
                return 0.0f;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[KSPI]: exception in getKerbalRadiationDose " + ex.Message);
                return 0.0f;
            }
        }

        public static ConfigNode getKerbal(int kerbalidx)
        {
            try
            {
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode crew_roster = gameconf.GetNode("ROSTER");
                ConfigNode[] crew = crew_roster.GetNodes("CREW");
                ConfigNode sought_kerbal = crew[kerbalidx];
                return sought_kerbal;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[KSPI]: exception in getKerbalRadiationDose " + ex.Message);
                return null;
            }
        }

        public static void saveKerbalRadiationdose(int kerbalidx, float rad)
        {
            try
            {
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode crew_roster = gameconf.GetNode("ROSTER");
                ConfigNode[] crew = crew_roster.GetNodes("CREW");
                ConfigNode sought_kerbal = crew[kerbalidx];
                if (sought_kerbal.HasValue("totalDose"))
                {
                    sought_kerbal.SetValue("totalDose", rad.ToString("E"));
                }
                else
                {
                    sought_kerbal.AddValue("totalDose", rad.ToString("E"));
                }
                config.Save(persistentfile);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[KSPI]: exception in getKerbalRadiationDose " + ex.Message);
            }
        }

        public static ConfigNode getPluginSaveFile()
        {
            ConfigNode config = ConfigNode.Load(PluginHelper.PluginSaveFilePath);
            if (config == null)
            {
                config = new ConfigNode();
                config.AddValue("writtenat", DateTime.Now.ToString());
                config.Save(PluginHelper.PluginSaveFilePath);
            }
            return config;
        }

        public static ConfigNode getPluginSettingsFile()
        {
            ConfigNode config = ConfigNode.Load(PluginHelper.PluginSettingsFilePath);
            if (config == null)
            {
                config = new ConfigNode();
            }
            return config;
        }

        public static double getMaxAtmosphericAltitude(CelestialBody body)
        {
            if (!body.atmosphere) return 0;
            return body.atmosphereDepth;
        }

        public static float getScienceMultiplier(Vessel vessel)
        {
            var vesselInAtmosphere = vessel.altitude < vessel.mainBody.atmosphereDepth;

            if (vessel.Splashed)
                return vessel.mainBody.scienceValues.SplashedDataValue;
            if (vesselInAtmosphere && vessel.horizontalSrfSpeed == 0)
                return vessel.mainBody.scienceValues.LandedDataValue;
            if (vesselInAtmosphere && vessel.altitude < vessel.mainBody.scienceValues.flyingAltitudeThreshold)
                return vessel.mainBody.scienceValues.FlyingLowDataValue;
            if (vesselInAtmosphere && vessel.altitude > vessel.mainBody.scienceValues.flyingAltitudeThreshold)
                return vessel.mainBody.scienceValues.FlyingHighDataValue;
            if (!vesselInAtmosphere && vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold)
                return vessel.mainBody.scienceValues.InSpaceLowDataValue;
            else
                return vessel.mainBody.scienceValues.InSpaceHighDataValue;
        }

        public static string getFormattedPowerString(double power, string shortFormat = "0", string longFormat = "0.0")
        {
            var absPower = Math.Abs(power);

            if (absPower > 1e6)
            {
                if (absPower > 2e7)
                    return (absPower / 1e6).ToString(shortFormat) + " TW";
                else
                    return (absPower / 1e6).ToString(longFormat) + " TW";
            }
            else if (absPower > 1e3)
            {
                if (absPower > 2e4)
                    return (absPower / 1e3).ToString(shortFormat) + " GW";
                else
                    return (absPower / 1e3).ToString(longFormat) + " GW";
            }
            else
            {
                if (absPower > 20)
                    return power.ToString(shortFormat) + " MW";
                else
                {
                    if (absPower > 1)
                        return absPower.ToString(longFormat) + " MW";
                    else
                        return (absPower * 1000).ToString(longFormat) + " KW";
                }
            }
        }

        public static string getFormatedMassString(double massInKg, string format)
        {
            if (massInKg < 0.000001)
                return (massInKg * 1000000).ToString(format) + " mg";
            else if (massInKg < 0.001)
                return (massInKg * 1000).ToString(format) + " g";
            else
                return (massInKg).ToString(format) + " kg";
        }

        public ApplicationLauncherButton InitializeApplicationButton()
        {
            ApplicationLauncherButton appButton = null;
            VABThermalUI.render_window = false;
            PluginHelper.using_toolbar = true;

            appIcon = GameDatabase.Instance.GetTexture("WarpPlugin/Category/WarpPlugin", false);

            if (appIcon != null)
            {
                appButton = ApplicationLauncher.Instance.AddModApplication(
                    OnAppLauncheActivate,
                    OnAppLauncherDeactivate,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                    appIcon);

                buttonAdded = true;
            }

            return appButton;
        }


        void OnAppLauncheActivate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                FlightUIStarter.hide_button = false;
                FlightUIStarter.show_window = true;
                VABThermalUI.render_window = false;
            }
            else
            {
                FlightUIStarter.hide_button = false;
                FlightUIStarter.show_window = false;
                VABThermalUI.render_window = true;
            }
        }

        void OnAppLauncherDeactivate()
        {
            FlightUIStarter.hide_button = true;
            FlightUIStarter.show_window = false;
            VABThermalUI.render_window = false;
        }

        public void Update()
        {
            if (ApplicationLauncher.Ready && !buttonAdded)
            {
                appLauncherButton = InitializeApplicationButton();
                if (appLauncherButton != null)
                    appLauncherButton.VisibleInScenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT;

                buttonAdded = true;
            }

            this.enabled = true;

            if (!resources_configured)
            {
                // read WarpPluginSettings.cfg 
                ConfigNode plugin_settings = GameDatabase.Instance.GetConfigNode(WARP_PLUGIN_SETTINGS_FILEPATH);
                if (plugin_settings != null)
                {
                    if (plugin_settings.HasValue("PartTechUpgrades"))
                    {
                        PartTechUpgrades = new Dictionary<string, string>();

                        string rawstring = plugin_settings.GetValue("PartTechUpgrades");
                        string[] splitValues = rawstring.Split(',').Select(sValue => sValue.Trim()).ToArray();

                        int pairs = splitValues.Length / 2;
                        int totalValues = splitValues.Length / 2 * 2;
                        for (int i = 0; i < totalValues; i += 2)
                            PartTechUpgrades.Add(splitValues[i], splitValues[i + 1]);

                        Debug.Log("[KSPI]: Part Tech Upgrades set to: " + rawstring);
                    }
                    if (plugin_settings.HasValue("OrsResourceMappings"))
                    {
                        OrsResourceMappings = new Dictionary<string, string>();

                        string rawstring = plugin_settings.GetValue("OrsResourceMappings");
                        string[] splitValues = rawstring.Split(',').Select(sValue => sValue.Trim()).ToArray();

                        int pairs = splitValues.Length / 2;
                        int totalValues = pairs * 2;
                        for (int i = 0; i < totalValues; i += 2)
                            OrsResourceMappings.Add(splitValues[i], splitValues[i + 1]);
                    }

                    if (plugin_settings.HasValue("ThermalUiKey"))
                    {
                        var thermalUiKeyStr = plugin_settings.GetValue("ThermalUiKey");

                        int thermalUiKeyInt;
                        if (int.TryParse(thermalUiKeyStr, out thermalUiKeyInt))
                        {
                            _thermalUiKey = (KeyCode)thermalUiKeyInt;
                            Debug.Log("[KSPI]: ThermalUiKey set to: " + PluginHelper.ThermalUiKey.ToString());
                        }
                        else
                        {
                            try
                            {
                                _thermalUiKey = (KeyCode)Enum.Parse(typeof(KeyCode), thermalUiKeyStr, true);
                                Debug.Log("[KSPI]: ThermalUiKey set to: " + PluginHelper.ThermalUiKey.ToString());
                            }
                            catch
                            {
                                Debug.LogError("[KSPI]: failed to convert " + thermalUiKeyStr + " to a KeyCode for ThermalUiKey");
                            }
                        }
                    }

                    
                    if (plugin_settings.HasValue("SecondsInDay"))
                    {
                        _secondsInDay = int.Parse(plugin_settings.GetValue("SecondsInDay"));
                        Debug.Log("[KSPI]: SecondsInDay set to: " + PluginHelper.SecondsInDay.ToString());
                    }

                    if (plugin_settings.HasValue("MicrowaveApertureDiameterMult"))
                    {
                        _microwaveApertureDiameterMult = double.Parse(plugin_settings.GetValue("MicrowaveApertureDiameterMult"));
                        Debug.Log("[KSPI]: Microwave Aperture Diameter Multiplier set to: " + PluginHelper.MicrowaveApertureDiameterMult.ToString());
                    }
                    if (plugin_settings.HasValue("SpotsizeMult"))
                    {
                        _spotsizeMult = double.Parse(plugin_settings.GetValue("SpotsizeMult"));
                        Debug.Log("[KSPI]: Spotsize Multiplier set to: " + PluginHelper.SpotsizeMult.ToString());
                    }

                    if (plugin_settings.HasValue("SpeedOfLightMult"))
                    {
                        _speedOfLightMult = double.Parse(plugin_settings.GetValue("SpeedOfLightMult"));
                        _speedOfLight = GameConstants.speedOfLight * _speedOfLightMult;

                        Debug.Log("[KSPI]: Speed Of Light Multiplier set to: " + PluginHelper.SpeedOfLightMult.ToString());
                    }
                    if (plugin_settings.HasValue("RadiationMechanicsDisabled"))
                    {
                        PluginHelper._radiationMechanicsDisabled = bool.Parse(plugin_settings.GetValue("RadiationMechanicsDisabled"));
                        Debug.Log("[KSPI]: Radiation Mechanics Disabled set to: " + PluginHelper.RadiationMechanicsDisabled.ToString());
                    }
                    if (plugin_settings.HasValue("ThermalMechanicsDisabled"))
                    {
                        PluginHelper._isThermalDissipationDisabled = bool.Parse(plugin_settings.GetValue("ThermalMechanicsDisabled"));
                        Debug.Log("[KSPI]: ThermalMechanics set to : " + (!PluginHelper.IsThermalDissipationDisabled).ToString());
                    }
                    if (plugin_settings.HasValue("SolarPanelClampedHeating"))
                    {
                        PluginHelper._isPanelHeatingClamped = bool.Parse(plugin_settings.GetValue("SolarPanelClampedHeating"));
                        Debug.Log("[KSPI]: Solar panels clamped heating set to enabled: " + PluginHelper.IsSolarPanelHeatingClamped.ToString());
                    }
                    if (plugin_settings.HasValue("RecieverTempTweak"))
                    {
                        PluginHelper._isRecieverTempTweaked = bool.Parse(plugin_settings.GetValue("RecieverTempTweak"));
                        Debug.Log("[KSPI]: Microwave reciever CoreTemp tweak is set to enabled: " + PluginHelper.IsRecieverCoreTempTweaked.ToString());
                    }
                    if (plugin_settings.HasValue("LimitedWarpTravel"))
                    {
                        PluginHelper._limitedWarpTravel = bool.Parse(plugin_settings.GetValue("LimitedWarpTravel"));
                        Debug.Log("[KSPI]: Apply Limited Warp Travel: " + PluginHelper.LimitedWarpTravel.ToString());
                    }
                    if (plugin_settings.HasValue("MatchDemandWithSupply"))
                    {
                        PluginHelper._matchDemandWithSupply = bool.Parse(plugin_settings.GetValue("MatchDemandWithSupply"));
                        Debug.Log("[KSPI]: Match Demand With Supply: " + PluginHelper.MatchDemandWithSupply.ToString());
                    }
                    if (plugin_settings.HasValue("MaxPowerDrawForExoticMatterMult"))
                    {
                        PluginHelper._maxPowerDrawForExoticMatterMult = double.Parse(plugin_settings.GetValue("MaxPowerDrawForExoticMatterMult"));
                        Debug.Log("[KSPI]: Max Power Draw For Exotic Matter Multiplier set to: " + PluginHelper.MaxPowerDrawForExoticMatterMult.ToString("0.000000"));
                    }
                    if (plugin_settings.HasValue("IspCoreTempMult"))
                    {
                        PluginHelper._ispCoreTempMult = double.Parse(plugin_settings.GetValue("IspCoreTempMult"));
                        Debug.Log("[KSPI]: Isp core temperature multiplier set to: " + PluginHelper.IspCoreTempMult.ToString("0.000000"));
                    }
                    if (plugin_settings.HasValue("ElectricEngineIspMult"))
                    {
                        PluginHelper._electricEngineIspMult = double.Parse(plugin_settings.GetValue("ElectricEngineIspMult"));
                        Debug.Log("[KSPI]: Electric EngineIsp Multiplier set to: " + PluginHelper.ElectricEngineIspMult.ToString("0.000000"));
                    }



                    if (plugin_settings.HasValue("GlobalThermalNozzlePowerMaxTrustMult"))
                    {
                        PluginHelper._globalThermalNozzlePowerMaxThrustMult = double.Parse(plugin_settings.GetValue("GlobalThermalNozzlePowerMaxTrustMult"));
                        Debug.Log("[KSPI]: Maximum Global Thermal Power Maximum Thrust Multiplier set to: " + PluginHelper.GlobalThermalNozzlePowerMaxThrustMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("GlobalMagneticNozzlePowerMaxTrustMult"))
                    {
                        PluginHelper._globalMagneticNozzlePowerMaxThrustMult = double.Parse(plugin_settings.GetValue("GlobalMagneticNozzlePowerMaxTrustMult"));
                        Debug.Log("[KSPI]: Maximum Global Magnetic Nozzle Power Maximum Thrust Multiplier set to: " + PluginHelper.GlobalMagneticNozzlePowerMaxThrustMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("GlobalElectricEnginePowerMaxTrustMult"))
                    {
                        PluginHelper._globalElectricEnginePowerMaxThrustMult = double.Parse(plugin_settings.GetValue("GlobalElectricEnginePowerMaxTrustMult"));
                        Debug.Log("[KSPI]: Maximum Global Electric Engine Power Maximum Thrust Multiplier set to: " + PluginHelper.GlobalElectricEnginePowerMaxThrustMult.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("MaxThermalNozzleIsp"))
                    {
                        PluginHelper._maxThermalNozzleIsp = float.Parse(plugin_settings.GetValue("MaxThermalNozzleIsp"));
                        Debug.Log("[KSPI] Maximum Thermal Nozzle Isp set to: " + PluginHelper.MaxThermalNozzleIsp.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("EngineHeatProduction"))
                    {
                        PluginHelper._engineHeatProduction = double.Parse(plugin_settings.GetValue("EngineHeatProduction"));
                        Debug.Log("[KSPI]: EngineHeatProduction set to: " + PluginHelper.EngineHeatProduction.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("EngineHeatProduction"))
                    {
                        PluginHelper._airflowHeatMult = double.Parse(plugin_settings.GetValue("AirflowHeatMult"));
                        Debug.Log("[KSPI]: AirflowHeatMultipler Isp set to: " + PluginHelper.AirflowHeatMult.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("TrustCoreTempThreshold"))
                    {
                        PluginHelper._thrustCoreTempThreshold = double.Parse(plugin_settings.GetValue("TrustCoreTempThreshold"));
                        Debug.Log("[KSPI]: Thrust core temperature threshold set to: " + PluginHelper.ThrustCoreTempThreshold.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("LowCoreTempBaseTrust"))
                    {
                        PluginHelper._lowCoreTempBaseThrust = double.Parse(plugin_settings.GetValue("LowCoreTempBaseTrust"));
                        Debug.Log("[KSPI]: Low core temperature base thrust modifier set to: " + PluginHelper.LowCoreTempBaseThrust.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("HighCoreTempTrustMult"))
                    {
                        PluginHelper._highCoreTempThrustMult = double.Parse(plugin_settings.GetValue("HighCoreTempTrustMult"));
                        Debug.Log("[KSPI]: High core temperature thrust divider set to: " + PluginHelper.HighCoreTempThrustMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("BasePowerConsumption"))
                    {
                        PluginHelper._basePowerConsumption = double.Parse(plugin_settings.GetValue("BasePowerConsumption"));
                        Debug.Log("[KSPI]: Base Power Consumption set to: " + PluginHelper.BasePowerConsumption.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("PowerConsumptionMultiplier"))
                    {
                        PluginHelper._powerConsumptionMultiplier = double.Parse(plugin_settings.GetValue("PowerConsumptionMultiplier"));
                        Debug.Log("[KSPI]: Base Power Consumption set to: " + PluginHelper.PowerConsumptionMultiplier.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("IspElectroPropellantModifierBase"))
                    {
                        PluginHelper._ispElectroPropellantModifierBase = double.Parse(plugin_settings.GetValue("IspNtrPropellantModifierBase"));
                        Debug.Log("[KSPI]: Isp Ntr Propellant Modifier Base set to: " + PluginHelper.IspElectroPropellantModifierBase.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("ElectricEnginePowerPropellantIspMultLimiter"))
                    {
                        PluginHelper._electricEnginePowerPropellantIspMultLimiter = double.Parse(plugin_settings.GetValue("ElectricEnginePowerPropellantIspMultLimiter"));
                        Debug.Log("[KSPI]: Electric Engine Power Propellant IspMultiplier Limiter set to: " + PluginHelper.ElectricEnginePowerPropellantIspMultLimiter.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("ElectricEngineAtmosphericDensityTrustLimiter"))
                    {
                        PluginHelper._electricEngineAtmosphericDensityThrustLimiter = double.Parse(plugin_settings.GetValue("ElectricEngineAtmosphericDensityTrustLimiter"));
                        Debug.Log("[KSPI]: Electric Engine Power Propellant IspMultiplier Limiter set to: " + PluginHelper.ElectricEngineAtmosphericDensityThrustLimiter.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("MaxAtmosphericAltitudeMult"))
                    {
                        PluginHelper._maxAtmosphericAltitudeMult = double.Parse(plugin_settings.GetValue("MaxAtmosphericAltitudeMult"));
                        Debug.Log("[KSPI]: Maximum Atmospheric Altitude Multiplier set to: " + PluginHelper.MaxAtmosphericAltitudeMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("MinAtmosphericAirDensity"))
                    {
                        PluginHelper._minAtmosphericAirDensity = double.Parse(plugin_settings.GetValue("MinAtmosphericAirDensity"));
                        Debug.Log("[KSPI]: Minimum Atmospheric Air Density set to: " + PluginHelper.MinAtmosphericAirDensity.ToString("0.0"));
                    }

                    // Jet Upgrade techs
                    if (plugin_settings.HasValue("JetUpgradeTech1"))
                    {
                        PluginHelper.JetUpgradeTech1 = plugin_settings.GetValue("JetUpgradeTech1");
                        Debug.Log("[KSPI]: JetUpgradeTech1 " + PluginHelper.JetUpgradeTech1);
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech1"))
                    {
                        PluginHelper.JetUpgradeTech2 = plugin_settings.GetValue("JetUpgradeTech2");
                        Debug.Log("[KSPI]: JetUpgradeTech2 " + PluginHelper.JetUpgradeTech2);
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech3"))
                    {
                        PluginHelper.JetUpgradeTech3 = plugin_settings.GetValue("JetUpgradeTech3");
                        Debug.Log("[KSPI]: JetUpgradeTech3 " + PluginHelper.JetUpgradeTech3);
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech4"))
                    {
                        PluginHelper.JetUpgradeTech4 = plugin_settings.GetValue("JetUpgradeTech4");
                        Debug.Log("[KSPI]: JetUpgradeTech4 " + PluginHelper.JetUpgradeTech4);
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech5"))
                    {
                        PluginHelper.JetUpgradeTech5 = plugin_settings.GetValue("JetUpgradeTech5");
                        Debug.Log("[KSPI]: JetUpgradeTech5 " + PluginHelper.JetUpgradeTech5);
                    }

                    // Radiator
                    if (plugin_settings.HasValue("RadiatorAreaMultiplier"))
                    {
                        PluginHelper.RadiatorAreaMultiplier = double.Parse(plugin_settings.GetValue("RadiatorAreaMultiplier"));
                        Debug.Log("[KSPI]: RadiatorAreaMultiplier " + PluginHelper.RadiatorAreaMultiplier);
                    }

                    resources_configured = true;
                }
                else
                {
                    showInstallationErrorMessage();
                }

            }
        }

        protected static bool warning_displayed = false;

        public static void showInstallationErrorMessage()
        {
            if (!warning_displayed)
            {
                //PopupDialog.SpawnPopupDialog("KSP Interstellar Installation Error", "KSP Interstellar is unable to detect files required for proper functioning.  Please make sure that this mod has been installed to [Base KSP directory]/GameData/WarpPlugin.", "OK", false, HighLogic.Skin);
                var errorMessage = "KSP Interstellar is unable to detect files required for proper functioning.  Please make sure that this mod has been installed to [Base KSP directory]/GameData/WarpPlugin.";
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "KSPI Error", "KSP Interstellar Installation Error", errorMessage, "OK", false, HighLogic.UISkin);

                warning_displayed = true;
            }
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part)
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }

        public static void SetAnimationRatio(float ratio, AnimationState[] animationState)
        {
            if (animationState == null) return;

            foreach (AnimationState anim in animationState)
            {
                anim.normalizedTime = ratio;
            }
        }

        private static Font mainFont;
        public static Font MainFont
        {
            get
            {
                if (mainFont == null)
                    mainFont = Font.CreateDynamicFontFromOSFont("Arial", 11);

                return mainFont;
            }
        }

    }
}
