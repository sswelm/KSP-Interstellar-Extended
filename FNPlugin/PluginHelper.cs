using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenResourceSystem;
using System.Text;
using KSP.IO;
using KSP.UI.Screens;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GameEventSubscriber : MonoBehaviour
    {
        void Start()
        {
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
            GameEvents.onSetSpeedMode.Add(OnSetSpeedModeChange);
            GameEvents.onVesselSituationChange.Add(OnVesselSituationChange);
            GameEvents.onVesselLoaded.Add(OnVesselLoaded);
            GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
            GameEvents.onGameStateSaved.Add(onGameStateSaved);

            Debug.Log("[KSP Interstellar] GameEventSubscriber Initialised");
        }
        void OnDestroy()
        {
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
            GameEvents.onSetSpeedMode.Remove(OnSetSpeedModeChange);
            GameEvents.onVesselSituationChange.Remove(OnVesselSituationChange);
            GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
            GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
            GameEvents.onGameStateSaved.Remove(onGameStateSaved);

            Debug.Log("[KSP Interstellar] GameEventSubscriber Deinitialised");
        }

        void onGameStateSaved(Game game)
        {
            Debug.Log("[KSP Interstellar] GameEventSubscriber - detected onGameStateSaved");
            PluginHelper.LoadSaveFile();
        }

        void OnVesselLoaded(Vessel vessel)
        {
            Debug.Log("[KSP Interstellar] GameEventSubscriber - detected OnVesselLoaded");
        }

        void OnTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> change)
        {
            Debug.Log("[KSP Interstellar] GameEventSubscriber - detected OnTechnologyResearched");
        }

        void OnSetSpeedModeChange(FlightGlobals.SpeedDisplayModes evt)
        {
            Debug.Log("[KSP Interstellar] GameEventSubscriber - detected OnSetSpeedModeChange");
        }

        void OnVesselGoOnRails(Vessel vessel)
        {
            Debug.Log("[KSP Interstellar] GameEventSubscriber - detected OnVesselGoOnRails");
        }

        void OnVesselGoOffRails(Vessel vessel)
        {
            Debug.Log("[KSP Interstellar] GameEventSubscriber - detected OnVesselGoOffRails");
        }

        void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> change)
        {
            bool shouldReinitialise = false;

            if (change.from == Vessel.Situations.DOCKED)
                shouldReinitialise = true;

            if (change.to == Vessel.Situations.DOCKED)
                shouldReinitialise = true;

            if (shouldReinitialise)
            {
                ORSHelper.removeVesselFromCache(change.host);

                Debug.Log("[KSP Interstellar] GameEventSubscriber - OnVesselSituationChange reinitialising");

                var generators = change.host.FindPartModulesImplementing<FNGenerator>();

                generators.ForEach(g => g.OnStart(PartModule.StartState.Docked));

                var radiators = change.host.FindPartModulesImplementing<FNRadiator>();

                radiators.ForEach(g => g.OnStart(PartModule.StartState.Docked));
            }
        }

        //void OnVesselChange(Vessel v)
        //{
        //    Debug.Log("[KSP Interstellar] OnVesselChange is called");
        //}
    }




    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PluginHelper : MonoBehaviour
    {
        public const double FIXED_SAT_ALTITUDE = 13599840256;
        public const int REF_BODY_KERBOL = 0;
        public const int REF_BODY_KERBIN = 1;
        public const int REF_BODY_MUN = 2;
        public const int REF_BODY_MINMUS = 3;
        public const int REF_BODY_MOHO = 4;
        public const int REF_BODY_EVE = 5;
        public const int REF_BODY_DUNA = 6;
        public const int REF_BODY_IKE = 7;
        public const int REF_BODY_JOOL = 8;
        public const int REF_BODY_LAYTHE = 9;
        public const int REF_BODY_VALL = 10;
        public const int REF_BODY_BOP = 11;
        public const int REF_BODY_TYLO = 12;
        public const int REF_BODY_GILLY = 13;
        public const int REF_BODY_POL = 14;
        public const int REF_BODY_DRES = 15;
        public const int REF_BODY_EELOO = 16;

        public static bool using_toolbar = false;
        public const int interstellar_major_version = 13;
        public const int interstellar_minor_version = 5;

        protected static bool plugin_init = false;
        protected static GameDatabase gdb;
        protected static bool resources_configured = false;
        
        static protected bool buttonAdded;
        static protected Texture2D appIcon = null;
        static protected ApplicationLauncherButton appLauncherButton = null;

        #region Static Properties

        public static bool TechnologyIsInUse
        {
            get { return (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX); }
        }

        public static ConfigNode PluginSettingsConfig
        {
            get { return GameDatabase.Instance.GetConfigNode("WarpPlugin/WarpPluginSettings/WarpPluginSettings"); }
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

        private static double _microwaveApertureDiameterMult = 10;
        public static double MicrowaveApertureDiameterMult { get { return _microwaveApertureDiameterMult; } }

        private static double _nonMicrowaveApertureDiameterMult = 1;
        public static double NonMicrowaveApertureDiameterMult { get { return _nonMicrowaveApertureDiameterMult; } }

        private static double _speedOfLightMult = GameConstants.speedOfLight;
        public static double SpeedOfLightMult { get { return _speedOfLightMult; } }
 
        private static double _maxAtmosphericAltitudeMult = 1;
        public static double MaxAtmosphericAltitudeMult { get { return _maxAtmosphericAltitudeMult; } }

        private static double _minAtmosphericAirDensity = 0;
        public static double MinAtmosphericAirDensity { get { return _minAtmosphericAirDensity; } }

        private static float _gravityConstant = GameConstants.STANDARD_GRAVITY;
        public static float GravityConstant { get { return _gravityConstant; } }

        private static float _ispCoreTempMult = GameConstants.IspCoreTemperatureMultiplier;
        public static float IspCoreTempMult { get { return _ispCoreTempMult; } }

        private static float _lowCoreTempBaseThrust = 0;
        public static float LowCoreTempBaseThrust { get { return _lowCoreTempBaseThrust; } }

        private static float _highCoreTempThrustMult = GameConstants.HighCoreTempThrustMultiplier;
        public static float HighCoreTempThrustMult { get { return _highCoreTempThrustMult; } }

        private static float _thrustCoreTempThreshold = 0;
        public static float ThrustCoreTempThreshold { get { return _thrustCoreTempThreshold; } }

        private static float _globalThermalNozzlePowerMaxThrustMult = 1;
        public static float GlobalThermalNozzlePowerMaxThrustMult { get { return _globalThermalNozzlePowerMaxThrustMult; } }

        private static double _globalMagneticNozzlePowerMaxThrustMult = 1;
        public static double GlobalMagneticNozzlePowerMaxThrustMult { get { return _globalMagneticNozzlePowerMaxThrustMult; } }

        private static double _globalElectricEnginePowerMaxThrustMult = 1;
        public static double GlobalElectricEnginePowerMaxThrustMult { get { return _globalElectricEnginePowerMaxThrustMult; } }

        private static float _maxPowerDrawForExoticMatterMult = 1;
        public static float MaxPowerDrawForExoticMatterMult { get { return _maxPowerDrawForExoticMatterMult; } }

        //private static double _lfoFuelThrustModifier = GameConstants.LfoFuelThrustModifier;
        //public static double LfoFuelThrustModifier { get { return _lfoFuelThrustModifier; } }

        private static double _electricEngineIspMult = 1;
        public static double ElectricEngineIspMult { get { return _electricEngineIspMult; } }

        private static float _electricEnginePowerPropellantIspMultLimiter = 1;
        public static float ElectricEnginePowerPropellantIspMultLimiter { get { return _electricEnginePowerPropellantIspMultLimiter; } }

        private static float _electricEngineAtmosphericDensityThrustLimiter = 0;
        public static float ElectricEngineAtmosphericDensityThrustLimiter { get { return _electricEngineAtmosphericDensityThrustLimiter; } }

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
        private static float _ispNtrPropellantModifierBase = 0;
        public static float IspNtrPropellantModifierBase { get { return _ispNtrPropellantModifierBase; } }

        private static float _ispElectroPropellantModifierBase = 0;
        public static float IspElectroPropellantModifierBase { get { return _ispElectroPropellantModifierBase; } }

        private static float _maxThermalNozzleIsp = GameConstants.MaxThermalNozzleIsp;
        public static float MaxThermalNozzleIsp { get { return _maxThermalNozzleIsp; } }

        private static float _airflowHeatMult = GameConstants.AirflowHeatMultiplier;
        public static float AirflowHeatMult { get { return _airflowHeatMult; } }

        private static float _engineHeatProduction = GameConstants.EngineHeatProduction;
        public static float EngineHeatProduction { get { return _engineHeatProduction; } }

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

        private static string _jetUpgradeTech0 = String.Empty;
        public static string JetUpgradeTech0 { get { return _jetUpgradeTech0; } private set { _jetUpgradeTech0 = value; } }

        private static string _jetUpgradeTech1 = String.Empty;
        public static string JetUpgradeTech1 { get { return _jetUpgradeTech1; } private set { _jetUpgradeTech1 = value; } }

        private static string _jetUpgradeTech2 = String.Empty;
        public static string JetUpgradeTech2 { get { return _jetUpgradeTech2; } private set { _jetUpgradeTech2 = value; } }

        private static string _jetUpgradeTech3 = String.Empty;
        public static string JetUpgradeTech3 { get { return _jetUpgradeTech3; } private set { _jetUpgradeTech3 = value; } }

        #endregion

        public static bool HasTechRequirementOrEmpty(string techName)
        {
            return techName == String.Empty || PluginHelper.upgradeAvailable(techName);
        }

        public static bool HasTechRequirementAndNotEmpty(string techName)
        {
            return techName != String.Empty && PluginHelper.upgradeAvailable(techName);
        }

        public static bool hasTech(string techid)
        {
            if (String.IsNullOrEmpty(techid))
                return false;

            if (ResearchAndDevelopment.Instance == null)
                return HasTechFromSaveFile(techid);

            var techstate = ResearchAndDevelopment.Instance.GetTechState(techid);
            if (techstate != null)
            {
                var available = techstate.state == RDTech.State.Available;
                if (available)
                    UnityEngine.Debug.Log("[KSPI] - found techid " + techid + " available");
                else
                    UnityEngine.Debug.Log("[KSPI] - found techid " + techid + " unavailable");
                return available;
            }
            else
            {
                UnityEngine.Debug.Log("[KSPI] - did not find techid " + techid);
                return false;
            }
        }

        private static HashSet<string> researchedTechs;

        public static void LoadSaveFile()
        {
            researchedTechs = new HashSet<string>();

            string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
            //UnityEngine.Debug.Log("[KSPI] - Loading ConfigNode " + persistentfile);
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

        public static bool HasTechFromSaveFile(string techid)
        {
            if (researchedTechs == null)
                LoadSaveFile();

            bool found = researchedTechs.Contains(techid);
            if (found)
                UnityEngine.Debug.Log("[KSPI] - found techid " + techid + " in saved hash");
            else
                UnityEngine.Debug.Log("[KSPI] - we did not find techid " + techid + " in saved hash");

            return found;
        }

        public static bool upgradeAvailable(string techid)
        {
            if (String.IsNullOrEmpty(techid))
                return false;

            if (HighLogic.CurrentGame != null)
            {
                if (PluginHelper.TechnologyIsInUse)
                    return PluginHelper.hasTech(techid);
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
                UnityEngine.Debug.LogError("[KSPI] - exception in getKerbalRadiationDose " + ex.Message);
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
                UnityEngine.Debug.LogError("[KSPI] - exception in getKerbalRadiationDose " + ex.Message);
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
                UnityEngine.Debug.LogError("[KSPI] - exception in getKerbalRadiationDose " + ex.Message);
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


        public static bool lineOfSightToSun(Vessel vess)
        {
            Vector3d a = PluginHelper.getVesselPos(vess);
            Vector3d b = FlightGlobals.Bodies[0].transform.position;

            return lineOfSightToSun(a, b);
        }

        public static bool lineOfSightToSun(Vector3d vessel, Vector3d star)
        {
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                if (referenceBody.flightGlobalsIndex == 0)
                { // the sun should not block line of sight to the sun
                    continue;
                }

                Vector3d refminusa = referenceBody.position - vessel;
                Vector3d bminusa = star - vessel;
                if (Vector3d.Dot(refminusa, bminusa) > 0)
                {
                    if (Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude)
                    {
                        Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                        if (tang.magnitude < referenceBody.Radius)
                            return false;
                    }
                }
            }
            return true;
        }

        public static Vector3d getVesselPos(Vessel v)
        {
            Vector3d v1p = (v.state == Vessel.State.ACTIVE) 
                ? (Vector3d)v.transform.position 
                : v.GetWorldPos3D();
            return v1p;
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
            if (power > 1000)
            {
                if (power > 20000)
                    return (power / 1000).ToString(shortFormat) + " GW";
                else
                    return (power / 1000).ToString(longFormat) + " GW";
            }
            else
            {
                if (power > 20)
                    return power.ToString(shortFormat) + " MW";
                else
                {
                    if (power > 1)
                        return power.ToString(longFormat) + " MW";
                    else
                        return (power * 1000).ToString(longFormat) + " KW";
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
                    OnAppLauncherTrue,
                    OnAppLauncherFalse,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.NEVER,
                    appIcon);

                buttonAdded = true;
            }

            return appButton;
        }


        void OnAppLauncherTrue()
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

        void OnAppLauncherFalse()
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
                    appLauncherButton.VisibleInScenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.FLIGHT;

                buttonAdded = true;
            }

            this.enabled = true;
            AvailablePart intakePart = PartLoader.getPartInfoByName("CircularIntake");
            if (intakePart != null)
            {
                if (intakePart.partPrefab.FindModulesImplementing<AtmosphericIntake>().Count <= 0 && PartLoader.Instance.IsReady())
                {
                    plugin_init = false;
                }
            }

            if (!resources_configured)
            {
                // read WarpPluginSettings.cfg 
                ConfigNode plugin_settings = GameDatabase.Instance.GetConfigNode("WarpPlugin/WarpPluginSettings/WarpPluginSettings");
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

                        Debug.Log("[KSP Interstellar] Part Tech Upgrades set to: " + rawstring);
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
                            Debug.Log("[KSPI] ThermalUiKey set to: " + PluginHelper.ThermalUiKey.ToString());
                        }
                        else
                        {
                            try
                            {
                                _thermalUiKey = (KeyCode)Enum.Parse(typeof(KeyCode), thermalUiKeyStr, true);
                                Debug.Log("[KSPI] ThermalUiKey set to: " + PluginHelper.ThermalUiKey.ToString());
                            }
                            catch
                            {
                                Debug.LogError("[KSPI] failed to convert " + thermalUiKeyStr + " to a KeyCode for ThermalUiKey");
                            }
                        }
                    }

                    if (plugin_settings.HasValue("SecondsInDay"))
                    {
                        _secondsInDay = int.Parse(plugin_settings.GetValue("SecondsInDay"));
                        Debug.Log("[KSPI] SecondsInDay set to: " + PluginHelper.SecondsInDay.ToString());
                    }

                    if (plugin_settings.HasValue("MicrowaveApertureDiameterMult"))
                    {
                        _microwaveApertureDiameterMult = double.Parse(plugin_settings.GetValue("MicrowaveApertureDiameterMult"));
                        Debug.Log("[KSPI] Microwave Aperture Diameter Multiplier set to: " + PluginHelper.MicrowaveApertureDiameterMult.ToString());
                    }
                    if (plugin_settings.HasValue("NonMicrowaveApertureDiameterMult"))
                    {
                        _nonMicrowaveApertureDiameterMult = double.Parse(plugin_settings.GetValue("NonMicrowaveApertureDiameterMult"));
                        Debug.Log("[KSPI] Non Microwave Aperture Diameter Multiplier set to: " + PluginHelper.NonMicrowaveApertureDiameterMult.ToString());
                    }

                    if (plugin_settings.HasValue("SpeedOfLightMult"))
                    {
                        _speedOfLightMult = double.Parse(plugin_settings.GetValue("SpeedOfLightMult"));
                        Debug.Log("[KSPI] Speed Of Light Multiplier set to: " + PluginHelper.SpeedOfLightMult.ToString());
                    }
                    if (plugin_settings.HasValue("RadiationMechanicsDisabled"))
                    {
                        PluginHelper._radiationMechanicsDisabled = bool.Parse(plugin_settings.GetValue("RadiationMechanicsDisabled"));
                        Debug.Log("[KSPI] Radiation Mechanics Disabled set to: " + PluginHelper.RadiationMechanicsDisabled.ToString());
                    }
                    if (plugin_settings.HasValue("ThermalMechanicsDisabled"))
                    {
                        PluginHelper._isThermalDissipationDisabled = bool.Parse(plugin_settings.GetValue("ThermalMechanicsDisabled"));
                        Debug.Log("[KSPI] ThermalMechanics set to : " + (!PluginHelper.IsThermalDissipationDisabled).ToString());
                    }
                    if (plugin_settings.HasValue("SolarPanelClampedHeating"))
                    {
                        PluginHelper._isPanelHeatingClamped = bool.Parse(plugin_settings.GetValue("SolarPanelClampedHeating"));
                        Debug.Log("[KSPI] Solar panels clamped heating set to enabled: " + PluginHelper.IsSolarPanelHeatingClamped.ToString());
                    }
                    if (plugin_settings.HasValue("RecieverTempTweak"))
                    {
                        PluginHelper._isRecieverTempTweaked = bool.Parse(plugin_settings.GetValue("RecieverTempTweak"));
                        Debug.Log("[KSPI] Microwave reciever CoreTemp tweak is set to enabled: " + PluginHelper.IsRecieverCoreTempTweaked.ToString());
                    }
                    if (plugin_settings.HasValue("LimitedWarpTravel"))
                    {
                        PluginHelper._limitedWarpTravel = bool.Parse(plugin_settings.GetValue("LimitedWarpTravel"));
                        Debug.Log("[KSPI] Apply Limited Warp Travel: " + PluginHelper.LimitedWarpTravel.ToString());
                    }
                    if (plugin_settings.HasValue("MatchDemandWithSupply"))
                    {
                        PluginHelper._matchDemandWithSupply = bool.Parse(plugin_settings.GetValue("MatchDemandWithSupply"));
                        Debug.Log("[KSPI] Match Demand With Supply: " + PluginHelper.MatchDemandWithSupply.ToString());
                    }
                    if (plugin_settings.HasValue("MaxPowerDrawForExoticMatterMult"))
                    {
                        PluginHelper._maxPowerDrawForExoticMatterMult = float.Parse(plugin_settings.GetValue("MaxPowerDrawForExoticMatterMult"));
                        Debug.Log("[KSPI] Max Power Draw For Exotic Matter Multiplier set to: " + PluginHelper.MaxPowerDrawForExoticMatterMult.ToString("0.000000"));
                    }
                    if (plugin_settings.HasValue("GravityConstant"))
                    {
                        PluginHelper._gravityConstant = Single.Parse(plugin_settings.GetValue("GravityConstant"));
                        Debug.Log("[KSPI] Gravity constant set to: " + PluginHelper.GravityConstant.ToString("0.000000"));
                    }
                    if (plugin_settings.HasValue("IspCoreTempMult"))
                    {
                        PluginHelper._ispCoreTempMult = float.Parse(plugin_settings.GetValue("IspCoreTempMult"));
                        Debug.Log("[KSPI] Isp core temperature multiplier set to: " + PluginHelper.IspCoreTempMult.ToString("0.000000"));
                    }
                    if (plugin_settings.HasValue("ElectricEngineIspMult"))
                    {
                        PluginHelper._electricEngineIspMult = double.Parse(plugin_settings.GetValue("ElectricEngineIspMult"));
                        Debug.Log("[KSPI] Electric EngineIsp Multiplier set to: " + PluginHelper.ElectricEngineIspMult.ToString("0.000000"));
                    }



                    if (plugin_settings.HasValue("GlobalThermalNozzlePowerMaxTrustMult"))
                    {
                        PluginHelper._globalThermalNozzlePowerMaxThrustMult = float.Parse(plugin_settings.GetValue("GlobalThermalNozzlePowerMaxTrustMult"));
                        Debug.Log("[KSPI] Maximum Global Thermal Power Maximum Thrust Multiplier set to: " + PluginHelper.GlobalThermalNozzlePowerMaxThrustMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("GlobalMagneticNozzlePowerMaxTrustMult"))
                    {
                        PluginHelper._globalMagneticNozzlePowerMaxThrustMult = double.Parse(plugin_settings.GetValue("GlobalMagneticNozzlePowerMaxTrustMult"));
                        Debug.Log("[KSPI] Maximum Global Magnetic Nozzle Power Maximum Thrust Multiplier set to: " + PluginHelper.GlobalMagneticNozzlePowerMaxThrustMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("GlobalElectricEnginePowerMaxTrustMult"))
                    {
                        PluginHelper._globalElectricEnginePowerMaxThrustMult = double.Parse(plugin_settings.GetValue("GlobalElectricEnginePowerMaxTrustMult"));
                        Debug.Log("[KSPI] Maximum Global Electric Engine Power Maximum Thrust Multiplier set to: " + PluginHelper.GlobalElectricEnginePowerMaxThrustMult.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("MaxThermalNozzleIsp"))
                    {
                        PluginHelper._maxThermalNozzleIsp = float.Parse(plugin_settings.GetValue("MaxThermalNozzleIsp"));
                        Debug.Log("[KSPI] Maximum Thermal Nozzle Isp set to: " + PluginHelper.MaxThermalNozzleIsp.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("EngineHeatProduction"))
                    {
                        PluginHelper._engineHeatProduction = float.Parse(plugin_settings.GetValue("EngineHeatProduction"));
                        Debug.Log("[KSPI] EngineHeatProduction set to: " + PluginHelper.EngineHeatProduction.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("EngineHeatProduction"))
                    {
                        PluginHelper._airflowHeatMult = float.Parse(plugin_settings.GetValue("AirflowHeatMult"));
                        Debug.Log("[KSPI] AirflowHeatMultipler Isp set to: " + PluginHelper.AirflowHeatMult.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("TrustCoreTempThreshold"))
                    {
                        PluginHelper._thrustCoreTempThreshold = float.Parse(plugin_settings.GetValue("TrustCoreTempThreshold"));
                        Debug.Log("[KSPI] Thrust core temperature threshold set to: " + PluginHelper.ThrustCoreTempThreshold.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("LowCoreTempBaseTrust"))
                    {
                        PluginHelper._lowCoreTempBaseThrust = float.Parse(plugin_settings.GetValue("LowCoreTempBaseTrust"));
                        Debug.Log("[KSPI] Low core temperature base thrust modifier set to: " + PluginHelper.LowCoreTempBaseThrust.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("HighCoreTempTrustMult"))
                    {
                        PluginHelper._highCoreTempThrustMult = float.Parse(plugin_settings.GetValue("HighCoreTempTrustMult"));
                        Debug.Log("[KSPI] High core temperature thrust divider set to: " + PluginHelper.HighCoreTempThrustMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("BasePowerConsumption"))
                    {
                        PluginHelper._basePowerConsumption = double.Parse(plugin_settings.GetValue("BasePowerConsumption"));
                        Debug.Log("[KSPI] Base Power Consumption set to: " + PluginHelper.BasePowerConsumption.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("PowerConsumptionMultiplier"))
                    {
                        PluginHelper._powerConsumptionMultiplier = double.Parse(plugin_settings.GetValue("PowerConsumptionMultiplier"));
                        Debug.Log("[KSPI] Base Power Consumption set to: " + PluginHelper.PowerConsumptionMultiplier.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("IspNtrPropellantModifierBase"))
                    {
                        PluginHelper._ispNtrPropellantModifierBase = float.Parse(plugin_settings.GetValue("IspNtrPropellantModifierBase"));
                        Debug.Log("[KSPI] Isp Ntr Propellant Modifier Base set to: " + PluginHelper.IspNtrPropellantModifierBase.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("IspElectroPropellantModifierBase"))
                    {
                        PluginHelper._ispElectroPropellantModifierBase = float.Parse(plugin_settings.GetValue("IspNtrPropellantModifierBase"));
                        Debug.Log("[KSPI] Isp Ntr Propellant Modifier Base set to: " + PluginHelper.IspElectroPropellantModifierBase.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("ElectricEnginePowerPropellantIspMultLimiter"))
                    {
                        PluginHelper._electricEnginePowerPropellantIspMultLimiter = float.Parse(plugin_settings.GetValue("ElectricEnginePowerPropellantIspMultLimiter"));
                        Debug.Log("[KSPI] Electric Engine Power Propellant IspMultiplier Limiter set to: " + PluginHelper.ElectricEnginePowerPropellantIspMultLimiter.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("ElectricEngineAtmosphericDensityTrustLimiter"))
                    {
                        PluginHelper._electricEngineAtmosphericDensityThrustLimiter = float.Parse(plugin_settings.GetValue("ElectricEngineAtmosphericDensityTrustLimiter"));
                        Debug.Log("[KSPI] Electric Engine Power Propellant IspMultiplier Limiter set to: " + PluginHelper.ElectricEngineAtmosphericDensityThrustLimiter.ToString("0.0"));
                    }

                    if (plugin_settings.HasValue("MaxAtmosphericAltitudeMult"))
                    {
                        PluginHelper._maxAtmosphericAltitudeMult = double.Parse(plugin_settings.GetValue("MaxAtmosphericAltitudeMult"));
                        Debug.Log("[KSPI] Maximum Atmospheric Altitude Multiplier set to: " + PluginHelper.MaxAtmosphericAltitudeMult.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("MinAtmosphericAirDensity"))
                    {
                        PluginHelper._minAtmosphericAirDensity = double.Parse(plugin_settings.GetValue("MinAtmosphericAirDensity"));
                        Debug.Log("[KSPI] Minimum Atmospheric Air Density set to: " + PluginHelper.MinAtmosphericAirDensity.ToString("0.0"));
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech0"))
                    {
                        PluginHelper.JetUpgradeTech0 = plugin_settings.GetValue("JetUpgradeTech0");
                        Debug.Log("[KSPI] JetUpgradeTech0" + PluginHelper.JetUpgradeTech0);
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech1"))
                    {
                        PluginHelper.JetUpgradeTech1 = plugin_settings.GetValue("JetUpgradeTech1");
                        Debug.Log("[KSPI] JetUpgradeTech1" + PluginHelper.JetUpgradeTech1);
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech2"))
                    {
                        PluginHelper.JetUpgradeTech2 = plugin_settings.GetValue("JetUpgradeTech2");
                        Debug.Log("[KSPI] JetUpgradeTech2" + PluginHelper.JetUpgradeTech2);
                    }
                    if (plugin_settings.HasValue("JetUpgradeTech3"))
                    {
                        PluginHelper.JetUpgradeTech3 = plugin_settings.GetValue("JetUpgradeTech3");
                        Debug.Log("[KSPI] JetUpgradeTech3" + PluginHelper.JetUpgradeTech3);
                    }

                    resources_configured = true;
                }
                else
                {
                    showInstallationErrorMessage();
                }

            }

            if (plugin_init) return;

            gdb = GameDatabase.Instance;
            plugin_init = true;

            List<AvailablePart> available_parts = PartLoader.LoadedPartsList;
            foreach (AvailablePart available_part in available_parts)
            {
                Part prefab_available_part = available_part.partPrefab;
                try
                {
                    if (prefab_available_part.Modules == null) continue;

                    ModuleResourceIntake intake = prefab_available_part.FindModuleImplementing<ModuleResourceIntake>();

                    if (intake != null && intake.resourceName == InterstellarResourcesConfiguration.Instance.IntakeAir)
                    {
                        var pm = prefab_available_part.gameObject.AddComponent<AtmosphericIntake>();
                        prefab_available_part.Modules.Add(pm);
                        pm.area = intake.area;
                        //pm.aoaThreshold = intake.aoaThreshold;
                        pm.intakeTransformName = intake.intakeTransformName;
                        //pm.maxIntakeSpeed = intake.maxIntakeSpeed;
                        pm.unitScalar = intake.unitScalar;
                        //pm.useIntakeCompensation = intake.useIntakeCompensation;
                        //pm.storesResource = intake.storesResource;

                        PartResource intake_air_resource = prefab_available_part.Resources[InterstellarResourcesConfiguration.Instance.IntakeAir];

                        if (intake_air_resource != null && !prefab_available_part.Resources.Contains(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere))
                        {
                            ConfigNode node = new ConfigNode("RESOURCE");
                            node.AddValue("name", InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);
                            node.AddValue("maxAmount", intake_air_resource.maxAmount);
                            node.AddValue("possibleAmount", intake_air_resource.amount);
                            prefab_available_part.AddResource(node);
                        }

                    }

                    if (prefab_available_part.FindModulesImplementing<ModuleDeployableSolarPanel>().Any())
                    {
                        var existingSolarControlModule = prefab_available_part.FindModuleImplementing<FNSolarPanelWasteHeatModule>();
                        if (existingSolarControlModule == null)
                        {
                            ModuleDeployableSolarPanel panel = prefab_available_part.FindModuleImplementing<ModuleDeployableSolarPanel>();
                            if (panel.chargeRate > 0)
                            {
                                //Type type = AssemblyLoader.GetClassByName(typeof(PartModule), "FNSolarPanelWasteHeatModule");
                                Type type = typeof(FNSolarPanelWasteHeatModule);
                                //if (type != null)
                                //{
                                FNSolarPanelWasteHeatModule pm = prefab_available_part.gameObject.AddComponent(type) as FNSolarPanelWasteHeatModule;
                                prefab_available_part.Modules.Add(pm);
                                //}
                            }

                            //if (!prefab_available_part.Resources.Contains("WasteHeat") && panel.chargeRate > 0)
                            //{
                            //    ConfigNode node = new ConfigNode("RESOURCE");
                            //    node.AddValue("name", "WasteHeat");
                            //    node.AddValue("maxAmount", panel.chargeRate * 100);
                            //    node.AddValue("possibleAmount", 0);

                            //    PartResource pr = prefab_available_part.AddResource(node);

                            //    if (available_part.resourceInfo != null && pr != null)
                            //    {
                            //        if (available_part.resourceInfo.Length == 0)
                            //            available_part.resourceInfo = pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
                            //        else
                            //            available_part.resourceInfo = available_part.resourceInfo + "\n" + pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
                            //    }
                            //}
                        }

                    }

                    if (prefab_available_part.FindModulesImplementing<ElectricEngineControllerFX>().Count() > 0)
                    {
                        available_part.moduleInfo = prefab_available_part.FindModulesImplementing<ElectricEngineControllerFX>().First().GetInfo();
                        available_part.moduleInfos.RemoveAll(modi => modi.moduleName == "Engine");
                        AvailablePart.ModuleInfo mod_info = available_part.moduleInfos.FirstOrDefault(modi => modi.moduleName == "Electric Engine Controller");

                        if (mod_info != null)
                            mod_info.moduleName = "Electric Engine";
                    }

                }
                catch (Exception ex)
                {
                    if (prefab_available_part != null)
                        print("[KSPI] Exception caught adding to: " + prefab_available_part.name + " part: " + ex.ToString());
                    else
                        print("[KSPI] Exception caught adding to unknown module");
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
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "KSP Interstellar Installation Error", errorMessage, "OK", false, HighLogic.UISkin);

                warning_displayed = true;
            }
        }

        /// <summary>Tests whether two vessels have line of sight to each other</summary>
        /// <returns><c>true</c> if a straight line from a to b is not blocked by any celestial body; 
        /// otherwise, <c>false</c>.</returns>
        public static bool HasLineOfSightWith(Vessel vessA, Vessel vessB, double freeDistance = 2500, double min_height = 5)
        {
            Vector3d vesselA = vessA.transform.position;
            Vector3d vesselB = vessB.transform.position;

            if (freeDistance > 0 && Vector3d.Distance(vesselA, vesselB) < freeDistance)           // if both vessels are within active view
                return true;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                Vector3d bodyFromA = referenceBody.position - vesselA;
                Vector3d bFromA = vesselB - vesselA;

                // Is body at least roughly between satA and satB?
                if (Vector3d.Dot(bodyFromA, bFromA) <= 0) continue;

                Vector3d bFromANorm = bFromA.normalized;

                if (Vector3d.Dot(bodyFromA, bFromANorm) >= bFromA.magnitude) continue;

                // Above conditions guarantee that Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm 
                // lies between the origin and bFromA
                Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm;

                if (lateralOffset.magnitude < referenceBody.Radius - min_height) return false;
            }
            return true;
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



    }
}
