using FNPlugin.Beamedpower;
using FNPlugin.Constants;
using FNPlugin.Powermanagement;
using FNPlugin.Propulsion;
using FNPlugin.Wasteheat;
using KSP.Localization;
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
            BeamedPowerSources.getVesselMicrowavePersistanceForProtoVesselCallback = BeamedPowerTransmitter.getVesselMicrowavePersistanceForProtoVessel;
            BeamedPowerSources.getVesselRelayPersistanceForProtoVesselCallback = BeamedPowerTransmitter.getVesselRelayPersistanceForProtoVessel;
            BeamedPowerSources.getVesselMicrowavePersistanceForVesselCallback = BeamedPowerTransmitter.getVesselMicrowavePersistanceForVessel;
            BeamedPowerSources.getVesselRelayPersistenceForVesselCallback = BeamedPowerTransmitter.getVesselRelayPersistenceForVessel;

            GameEvents.onGameStateSaved.Add(OnGameStateSaved);
            GameEvents.onDockingComplete.Add(OnDockingComplete);
            GameEvents.onPartDeCoupleComplete.Add(OnPartDeCoupleComplete);
            GameEvents.onVesselSOIChanged.Add(OnVesselSOIChanged);
            GameEvents.onPartDestroyed.Add(OnPartDestroyed);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOnRails);

            Debug.Log("[KSPI]: GameEventSubscriber Initialized");
        }
        void OnDestroy()
        {
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOnRails);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
            GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
            GameEvents.onGameStateSaved.Remove(OnGameStateSaved);
            GameEvents.onDockingComplete.Remove(OnDockingComplete);
            GameEvents.onPartDeCoupleComplete.Remove(OnPartDeCoupleComplete);
            GameEvents.onVesselSOIChanged.Remove(OnVesselSOIChanged);

            var kerbalismVersionStr =
                $"{Kerbalism.versionMajor}.{Kerbalism.versionMajorRevision}.{Kerbalism.versionMinor}.{Kerbalism.versionMinorRevision}";

            if (Kerbalism.versionMajor > 0)
                Debug.Log("[KSPI]: Loaded Kerbalism " + kerbalismVersionStr);

            Debug.Log("[KSPI]: GameEventSubscriber Deinitialised");
        }

        void OnVesselGoOnRails(Vessel vessel)
        {
            foreach (var part in vessel.Parts)
            {
                var autoStrutEvent = part.Events["ToggleAutoStrut"];
                if (autoStrutEvent != null)
                {
                    autoStrutEvent.guiActive = true;
                    autoStrutEvent.guiActiveUncommand = true;
                    autoStrutEvent.guiActiveUnfocused = true;
                    autoStrutEvent.requireFullControl = false;
                }

                var rigidAttachmentEvent = part.Events["ToggleRigidAttachment"];
                if (rigidAttachmentEvent != null)
                {
                    rigidAttachmentEvent.guiActive = true;
                    rigidAttachmentEvent.guiActiveUncommand = true;
                    rigidAttachmentEvent.guiActiveUnfocused = true;
                    rigidAttachmentEvent.requireFullControl = false;
                }
            }
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

        void OnPartDestroyed(Part part)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnPartDestroyed");

            var drive = part.FindModuleImplementing<AlcubierreDrive>();

            if (drive == null) return;

            if (drive.IsSlave)
            {
                Debug.Log("[KSPI]: GameEventSubscriber - destroyed part is a slave warpdrive");
                drive = drive.vessel.FindPartModulesImplementing<AlcubierreDrive>().FirstOrDefault(m => !m.IsSlave);
            }

            if (drive != null)
            {
                Debug.Log("[KSPI]: GameEventSubscriber - deactivate master warp drive");
                drive.DeactivateWarpDrive();
            }
        }

        void OnVesselSOIChanged (GameEvents.HostedFromToAction<Vessel, CelestialBody> gameEvent)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnVesselSOIChanged");
            gameEvent.host.FindPartModulesImplementing<ElectricEngineControllerFX>().ForEach(e => e.VesselChangedSoi());
            gameEvent.host.FindPartModulesImplementing<ModuleEnginesWarp>().ForEach(e => e.VesselChangedSOI());
            gameEvent.host.FindPartModulesImplementing<DaedalusEngineController>().ForEach(e => e.VesselChangedSOI());
            gameEvent.host.FindPartModulesImplementing<AlcubierreDrive>().ForEach(e => e.VesselChangedSOI());
        }

        void OnPartDeCoupleComplete (Part part)
        {
            Debug.Log("[KSPI]: GameEventSubscriber - detected OnPartDeCoupleComplete");

            ResourceOvermanager.Reset();
            SupplyPriorityManager.Reset();
            FNRadiator.Reset();

            ResetReceivers();
        }

        void OnVesselDestroy(Vessel vessel)
        {
            ResourceOvermanager.ResetForVessel(vessel);
        }

        private static void ResetReceivers()
        {
            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                if (!currentVessel.loaded) continue;

                var receivers = currentVessel.FindPartModulesImplementing<BeamedPowerReceiver>();

                foreach (var receiver in receivers)
                {
                    Debug.Log("[KSPI]: OnDockingComplete - Restart receivers " + receiver.Part.name);
                    receiver.Restart(50);
                }
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PluginHelper : MonoBehaviour
    {
        public const string WARP_PLUGIN_SETTINGS_FILEPATH = "WarpPlugin/WarpPluginSettings/WarpPluginSettings";

        public static bool usingToolbar;
        protected static bool resourcesConfigured;

        private static Dictionary<string, RDTech> rdTechByName;

        public static Dictionary<string, RDTech> RDTechByName
        {
            get
            {
                if (rdTechByName != null) return rdTechByName;

                rdTechByName = new Dictionary<string, RDTech>();

                // catalog part upgrades
                ConfigNode[] techTreeConfigs = GameDatabase.Instance.GetConfigNodes("TechTree");
                Debug.Log("[KSPI]: PluginHelper found: " + techTreeConfigs.Count() + " TechTrees");

                foreach (var techTreeConfig in techTreeConfigs)
                {
                    var techNodes = techTreeConfig.nodes;

                    Debug.Log("[KSPI]: PluginHelper found: " + techNodes.Count + " Technodes");
                    for (var j = 0; j < techNodes.Count; j++)
                    {
                        var techNode = techNodes[j];

                        var tech = new RDTech { techID = techNode.GetValue("id"), title = techNode.GetValue("title") };

                        if (rdTechByName.ContainsKey(tech.techID))
                            Debug.LogError("[KSPI]: Duplicate error: skipped technode id: " + tech.techID + " title: " + tech.title);
                        else
                        {
                            Debug.Log("[KSPI]: PluginHelper technode id: " + tech.techID + " title: " + tech.title);
                            rdTechByName.Add(tech.techID, tech);
                        }
                    }
                }
                return rdTechByName;
            }
        }

        private static Dictionary<string, PartUpgradeHandler.Upgrade> _partUpgradeByName;

        public static Dictionary<string, PartUpgradeHandler.Upgrade> PartUpgradeByName
        {
            get
            {
                if (_partUpgradeByName != null) return _partUpgradeByName;

                _partUpgradeByName = new Dictionary<string, PartUpgradeHandler.Upgrade>();

                // catalog part upgrades
                ConfigNode[] partUpgradeConfigs = GameDatabase.Instance.GetConfigNodes("PARTUPGRADE");
                Debug.Log("[KSPI]: PluginHelper found: " + partUpgradeConfigs.Count() + " Part upgrades");

                foreach (var partUpgradeConfig in partUpgradeConfigs)
                {
                    var partUpgrade = new PartUpgradeHandler.Upgrade
                    {
                        name = partUpgradeConfig.GetValue("name"),
                        techRequired = partUpgradeConfig.GetValue("techRequired"),
                        manufacturer = partUpgradeConfig.GetValue("manufacturer")
                    };

                    if (_partUpgradeByName.ContainsKey(partUpgrade.name))
                    {
                        //Debug.LogError("[KSPI]: Duplicate error: failed to add PARTUPGRADE" + partUpgrade.name + " with techRequired " + partUpgrade.techRequired + " from manufacturer " + partUpgrade.manufacturer);
                    }
                    else
                    {
                        //Debug.Log("[KSPI]: PluginHelper indexed PARTUPGRADE " + partUpgrade.name + " with techRequired " + partUpgrade.techRequired + " from manufacturer " + partUpgrade.manufacturer);
                        _partUpgradeByName.Add(partUpgrade.name, partUpgrade);
                    }
                }

                return _partUpgradeByName;
            }
        }

        static protected bool buttonAdded;
        static protected Texture2D appIcon = null;
        static protected ApplicationLauncherButton appLauncherButton = null;

        #region static Properties

        public static bool TechnologyIsInUse => (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);

        public static ConfigNode PluginSettingsConfig => GameDatabase.Instance.GetConfigNode(WARP_PLUGIN_SETTINGS_FILEPATH);

        public static string PluginSaveFilePath => KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/WarpPlugin.cfg";

        public static string PluginSettingsFilePath => KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/WarpPluginSettings.cfg";

        public static int HoursInDay => GameConstants.KEBRIN_HOURS_DAY;
        public static int SecondsInHour => GameConstants.SECONDS_IN_HOUR;

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

        private static double _electricEngineIspMult = 1;
        public static double ElectricEngineIspMult { get { return _electricEngineIspMult; } }

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

        private static float _maxThermalNozzleIsp = GameConstants.MaxThermalNozzleIsp;
        public static float MaxThermalNozzleIsp { get { return _maxThermalNozzleIsp; } }

        private static double _airflowHeatMult = GameConstants.AirflowHeatMultiplier;
        public static double AirflowHeatMult { get { return _airflowHeatMult; } }


        // RadiatorAreaMultiplier

        private static double _radiatorAreaMultiplier = 2;
        public static double RadiatorAreaMultiplier { get { return _radiatorAreaMultiplier; } private set { _radiatorAreaMultiplier = value; } }


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
            return techName == string.Empty || UpgradeAvailable(techName);
        }

        public static bool HasTechRequirementAndNotEmpty(string techName)
        {
            return techName != string.Empty && UpgradeAvailable(techName);
        }

        public static string DisplayTech(string techid)
        {
            return string.IsNullOrEmpty(techid) ? string.Empty : RUIutils.GetYesNoUIString(
                UpgradeAvailable(techid)) + " " + Localizer.Format(GetTechTitleById(techid));
        }

        public static string GetTechTitleById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return id;

            var result = ResearchAndDevelopment.GetTechnologyTitle(id);
            if (!string.IsNullOrEmpty(result))
                return result;

            if (PartUpgradeByName.TryGetValue(id, out var partUpgrade))
            {
                if (partUpgrade != null && !string.IsNullOrEmpty(partUpgrade.techRequired))
                {
                    if (RDTechByName.TryGetValue(partUpgrade.techRequired, out var upgradeTechNode))
                        return upgradeTechNode?.title;
                }
                else if (partUpgrade == null)
                    Debug.LogError("[KSPI]: GetTechTitleById - partUpgrade is null");
                else
                    Debug.LogError("[KSPI]: GetTechTitleById - partUpgrade.techRequired is null");
            }

            if (RDTechByName.TryGetValue(id, out var techNode))
                return techNode.title;

            return id;
        }

        private static bool HasTech(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "none")
                return false;

            if (ResearchAndDevelopment.Instance == null)
                return HasTechFromSaveFile(id);

            var techState = ResearchAndDevelopment.Instance.GetTechState(id);
            if (techState != null)
                return techState.state == RDTech.State.Available;
            else
                return false;
        }

        private static HashSet<string> _researchedTechs;

        public static void LoadSaveFile()
        {
            _researchedTechs = new HashSet<string>();

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
                        _researchedTechs.Add(technodename);
                    }
                }
            }
        }

        private static bool HasTechFromSaveFile(string techid)
        {
            if (_researchedTechs == null)
                LoadSaveFile();

            bool found = _researchedTechs.Contains(techid);
            if (found)
                Debug.Log("[KSPI]: found techid " + techid + " in saved hash");
            else
                Debug.Log("[KSPI]: we did not find techid " + techid + " in saved hash");

            return found;
        }

        public static bool UpgradeAvailable(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            if (id == "true" || id == "always")
                return true;

            if (id == "false" || id == "none")
                return false;

            PartUpgradeHandler.Upgrade partUpgrade;
            if (PartUpgradeByName.TryGetValue(id, out partUpgrade))
                id = partUpgrade.techRequired;

            if (HighLogic.CurrentGame != null)
                return !TechnologyIsInUse || HasTech(id);
            else
                return true;
        }

        public static double GetBlackBodyDissipation(double effectiveSurfaceArea, double temperatureDelta)
        {
            return effectiveSurfaceArea * PhysicsGlobals.StefanBoltzmanConstant * temperatureDelta * temperatureDelta * temperatureDelta * temperatureDelta;
        }

        public static double GetTimeWarpModifer()
        {
            return TimeWarp.fixedDeltaTime > 20 ? 1 + (TimeWarp.fixedDeltaTime - 20) : 1;
        }

        public static ConfigNode GetPluginSaveFile()
        {
            ConfigNode config = ConfigNode.Load(PluginSaveFilePath);
            if (config != null) return config;
            config = new ConfigNode();
            config.AddValue("writtenat", DateTime.Now.ToString());
            config.Save(PluginSaveFilePath);
            return config;
        }

        public static ConfigNode getPluginSettingsFile()
        {
            ConfigNode config = ConfigNode.Load(PluginSettingsFilePath) ?? new ConfigNode();
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

        public static string getFormattedPowerString(double power)
        {
            var absPower = Math.Abs(power);
            string suffix;

            if (absPower >= 1e6)
            {
                suffix = " TW";
                absPower *= 1e-6;
                power *= 1e-6;
            }
            else if (absPower >= 1000)
            {
                suffix = " GW";
                absPower *= 1e-3;
                power *= 1e-3;
            }
            else if (absPower >= 1)
            {
                suffix = " MW";
            }
            else if (absPower >= 0.001)
            {
                suffix = " KW";
                absPower *= 1e3;
                power *= 1e3;
            }
            else
                return (power * 1e6).ToString("0") + " W";
            if (absPower > 100.0)
                return power.ToString("0") + suffix;
            else if (absPower > 10.0)
                return power.ToString("0.0") + suffix;
            else
                return power.ToString("0.00") + suffix;
        }

        public ApplicationLauncherButton InitializeApplicationButton()
        {
            appIcon = GameDatabase.Instance.GetTexture("WarpPlugin/Category/WarpPlugin", false);

            if (appIcon == null) return null;

            var appButton = ApplicationLauncher.Instance.AddModApplication(
                OnAppLauncherActivate,
                OnAppLauncherDeactivate,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                appIcon);

            buttonAdded = true;

            return appButton;
        }


        void OnAppLauncherActivate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                FlightUIStarter.hide_button = false;
                FlightUIStarter.show_window = true;
                VABThermalUI.renderWindow = false;
            }
            else
            {
                FlightUIStarter.hide_button = false;
                FlightUIStarter.show_window = false;
                VABThermalUI.renderWindow = true;
            }
        }

        void OnAppLauncherDeactivate()
        {
            FlightUIStarter.hide_button = true;
            FlightUIStarter.show_window = false;
            VABThermalUI.renderWindow = false;
        }

        static int _ignoredGForces;
        public static void IgnoreGForces(Part part, int frames)
        {
            _ignoredGForces = frames;
            part.vessel.IgnoreGForces(frames);
        }

        public static bool GForcesIgnored => _ignoredGForces > 0;

        public static void UpdateIgnoredGForces()
        {
            if (_ignoredGForces > 0)
                --_ignoredGForces;
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

            if (resourcesConfigured) return;

            // read WarpPluginSettings.cfg
            var pluginSettingConfigs = GameDatabase.Instance.GetConfigNode(WARP_PLUGIN_SETTINGS_FILEPATH);

            if (pluginSettingConfigs == null)
            {
                ShowInstallationErrorMessage();
                return;
            }

            if (pluginSettingConfigs.HasValue("IspCoreTempMult"))
            {
                _ispCoreTempMult = double.Parse(pluginSettingConfigs.GetValue("IspCoreTempMult"));
                Debug.Log("[KSPI]: Isp core temperature multiplier set to: " + IspCoreTempMult.ToString("0.000000"));
            }
            if (pluginSettingConfigs.HasValue("ElectricEngineIspMult"))
            {
                _electricEngineIspMult = double.Parse(pluginSettingConfigs.GetValue("ElectricEngineIspMult"));
                Debug.Log("[KSPI]: Electric EngineIsp Multiplier set to: " + ElectricEngineIspMult.ToString("0.000000"));
            }
            if (pluginSettingConfigs.HasValue("GlobalThermalNozzlePowerMaxTrustMult"))
            {
                _globalThermalNozzlePowerMaxThrustMult = double.Parse(pluginSettingConfigs.GetValue("GlobalThermalNozzlePowerMaxTrustMult"));
                Debug.Log("[KSPI]: Maximum Global Thermal Power Maximum Thrust Multiplier set to: " + GlobalThermalNozzlePowerMaxThrustMult.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("GlobalMagneticNozzlePowerMaxTrustMult"))
            {
                _globalMagneticNozzlePowerMaxThrustMult = double.Parse(pluginSettingConfigs.GetValue("GlobalMagneticNozzlePowerMaxTrustMult"));
                Debug.Log("[KSPI]: Maximum Global Magnetic Nozzle Power Maximum Thrust Multiplier set to: " + GlobalMagneticNozzlePowerMaxThrustMult.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("GlobalElectricEnginePowerMaxTrustMult"))
            {
                _globalElectricEnginePowerMaxThrustMult = double.Parse(pluginSettingConfigs.GetValue("GlobalElectricEnginePowerMaxTrustMult"));
                Debug.Log("[KSPI]: Maximum Global Electric Engine Power Maximum Thrust Multiplier set to: " + GlobalElectricEnginePowerMaxThrustMult.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("MaxThermalNozzleIsp"))
            {
                _maxThermalNozzleIsp = float.Parse(pluginSettingConfigs.GetValue("MaxThermalNozzleIsp"));
                Debug.Log("[KSPI] Maximum Thermal Nozzle Isp set to: " + MaxThermalNozzleIsp.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("EngineHeatProduction"))
            {
                _airflowHeatMult = double.Parse(pluginSettingConfigs.GetValue("AirflowHeatMult"));
                Debug.Log("[KSPI]: AirflowHeatMultipler Isp set to: " + AirflowHeatMult.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("TrustCoreTempThreshold"))
            {
                _thrustCoreTempThreshold = double.Parse(pluginSettingConfigs.GetValue("TrustCoreTempThreshold"));
                Debug.Log("[KSPI]: Thrust core temperature threshold set to: " + ThrustCoreTempThreshold.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("LowCoreTempBaseTrust"))
            {
                _lowCoreTempBaseThrust = double.Parse(pluginSettingConfigs.GetValue("LowCoreTempBaseTrust"));
                Debug.Log("[KSPI]: Low core temperature base thrust modifier set to: " + LowCoreTempBaseThrust.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("HighCoreTempTrustMult"))
            {
                _highCoreTempThrustMult = double.Parse(pluginSettingConfigs.GetValue("HighCoreTempTrustMult"));
                Debug.Log("[KSPI]: High core temperature thrust divider set to: " + HighCoreTempThrustMult.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("BasePowerConsumption"))
            {
                _basePowerConsumption = double.Parse(pluginSettingConfigs.GetValue("BasePowerConsumption"));
                Debug.Log("[KSPI]: Base Power Consumption set to: " + BasePowerConsumption.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("PowerConsumptionMultiplier"))
            {
                _powerConsumptionMultiplier = double.Parse(pluginSettingConfigs.GetValue("PowerConsumptionMultiplier"));
                Debug.Log("[KSPI]: Base Power Consumption set to: " + PowerConsumptionMultiplier.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("ElectricEngineAtmosphericDensityTrustLimiter"))
            {
                _electricEngineAtmosphericDensityThrustLimiter = double.Parse(pluginSettingConfigs.GetValue("ElectricEngineAtmosphericDensityTrustLimiter"));
                Debug.Log("[KSPI]: Electric Engine Power Propellant IspMultiplier Limiter set to: " + ElectricEngineAtmosphericDensityThrustLimiter.ToString("0.0"));
            }
            if (pluginSettingConfigs.HasValue("MinAtmosphericAirDensity"))
            {
                _minAtmosphericAirDensity = double.Parse(pluginSettingConfigs.GetValue("MinAtmosphericAirDensity"));
                Debug.Log("[KSPI]: Minimum Atmospheric Air Density set to: " + MinAtmosphericAirDensity.ToString("0.0"));
            }

            // Radiator
            if (pluginSettingConfigs.HasValue("RadiatorAreaMultiplier"))
            {
                RadiatorAreaMultiplier = double.Parse(pluginSettingConfigs.GetValue("RadiatorAreaMultiplier"));
                Debug.Log("[KSPI]: RadiatorAreaMultiplier " + RadiatorAreaMultiplier);
            }

            resourcesConfigured = true;
        }

        private static bool _warningDisplayed;

        public static void ShowInstallationErrorMessage()
        {
            if (_warningDisplayed) return;

            var errorMessage =Localizer.Format("#LOC_KSPIE_PluginHelper_Installerror");// "KSP Interstellar is unable to detect files required for proper functioning.  Please make sure that this mod has been installed to [Base KSP directory]/GameData/WarpPlugin."
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "KSPI Error", "KSP Interstellar Installation Error", errorMessage, "OK", false, HighLogic.UISkin);

            _warningDisplayed = true;
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
