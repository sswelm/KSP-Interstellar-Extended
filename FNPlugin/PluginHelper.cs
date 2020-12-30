using FNPlugin.Powermanagement;
using FNPlugin.Wasteheat;
using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PluginHelper : MonoBehaviour
    {
        public const string WARP_PLUGIN_SETTINGS_FILEPATH = "WarpPlugin/WarpPluginSettings/WarpPluginSettings";

        public static bool usingToolbar;
        protected bool techNodesConfigured;
        protected static bool resourcesConfigured;

        private static Dictionary<string, RDTech> _rdTechByName;

        public static Dictionary<string, RDTech> RDTechByName
        {
            get
            {
                if (_rdTechByName != null) return _rdTechByName;

                _rdTechByName = new Dictionary<string, RDTech>();

                // catalog part upgrades
                var techTreeConfigs = GameDatabase.Instance.GetConfigNodes("TechTree");
                Debug.Log("[KSPI]: PluginHelper found: " + techTreeConfigs.Count() + " TechTrees");

                foreach (var techTreeConfig in techTreeConfigs)
                {
                    var techNodes = techTreeConfig.nodes;

                    Debug.Log("[KSPI]: PluginHelper found: " + techNodes.Count + " Technodes");
                    for (var j = 0; j < techNodes.Count; j++)
                    {
                        var techNode = techNodes[j];

                        var tech = new RDTech { techID = techNode.GetValue("id"), title = techNode.GetValue("title") };

                        if (_rdTechByName.ContainsKey(tech.techID))
                            Debug.LogError("[KSPI]: Duplicate error: skipped technode id: " + tech.techID + " title: " + tech.title);
                        else
                        {
                            Debug.Log("[KSPI]: PluginHelper technode id: " + tech.techID + " title: " + tech.title);
                            _rdTechByName.Add(tech.techID, tech);
                        }
                    }
                }
                return _rdTechByName;
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

        private static bool _buttonAdded;
        private static Texture2D _appIcon;
        private static ApplicationLauncherButton _appLauncherButton;

        #region static Properties

        public static bool TechnologyIsInUse => (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);

        public static ConfigNode PluginSettingsConfig => GameDatabase.Instance.GetConfigNode(WARP_PLUGIN_SETTINGS_FILEPATH);

        public static string PluginSaveFilePath => KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/WarpPlugin.cfg";


        #endregion

        public static string FormatMassStr(double mass, string format = "0.000000")
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
            return format == null ? value.ToString(CultureInfo.InvariantCulture) : value.ToString(format);
        }

        public static bool HasTechRequirementOrEmpty(string techName)
        {
            return techName == string.Empty || UpgradeAvailable(techName);
        }

        public static bool HasTechRequirementAndNotEmpty(string techName)
        {
            return techName != string.Empty && UpgradeAvailable(techName);
        }

        public static string DisplayTech(string techId)
        {
            return string.IsNullOrEmpty(techId) ? string.Empty : RUIutils.GetYesNoUIString(
                UpgradeAvailable(techId)) + " " + Localizer.Format(GetTechTitleById(techId));
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

            return ResearchAndDevelopment.GetTechnologyState(id) == RDTech.State.Available;
        }

        private static HashSet<string> _researchedTechs;

        public static void LoadSaveFile()
        {
            _researchedTechs = new HashSet<string>();

            string persistentFile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";

            var config = ConfigNode.Load(persistentFile);
            var gameConfig = config.GetNode("GAME");
            var scenarios = gameConfig.GetNodes("SCENARIO");

            foreach (var scenario in scenarios)
            {
                if (scenario.GetValue("name") != "ResearchAndDevelopment") continue;

                var techs = scenario.GetNodes("Tech");
                foreach (var techNode in techs)
                {
                    var techNodeName = techNode.GetValue("id");
                    _researchedTechs.Add(techNodeName);
                }
            }
        }

        private static bool HasTechFromSaveFile(string techId)
        {
            if (_researchedTechs == null)
                LoadSaveFile();

            if (_researchedTechs == null)
            {
                Debug.LogError("[KSPI]: failed to load save file");
                return false;
            }

            bool found = _researchedTechs.Contains(techId);
            if (found)
                Debug.Log("[KSPI]: found techId " + techId + " in saved hash");
            else
                Debug.Log("[KSPI]: we did not find techId " + techId + " in saved hash");

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

            if (PartUpgradeByName.TryGetValue(id, out var partUpgrade))
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
            var config = ConfigNode.Load(PluginSaveFilePath);
            if (config != null) return config;
            config = new ConfigNode();
            config.AddValue("writtenat", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            config.Save(PluginSaveFilePath);
            return config;
        }

        public static double GetMaxAtmosphericAltitude(CelestialBody body)
        {
            if (!body.atmosphere) return 0;
            return body.atmosphereDepth;
        }

        public static float GetScienceMultiplier(Vessel vessel)
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

        public static string GetFormattedPowerString(double power)
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
                return power.ToString("0.0") + suffix;
            else if (absPower > 10.0)
                return power.ToString("0.00") + suffix;
            else
                return power.ToString("0.000") + suffix;
        }

        public ApplicationLauncherButton InitializeApplicationButton()
        {
            _appIcon = GameDatabase.Instance.GetTexture("WarpPlugin/Category/WarpPlugin", false);

            if (_appIcon == null) return null;

            var appButton = ApplicationLauncher.Instance.AddModApplication(
                OnAppLauncherActivate,
                OnAppLauncherDeactivate,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                _appIcon);

            _buttonAdded = true;

            return appButton;
        }


        void OnAppLauncherActivate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                FlightUIStarter.hideButton = false;
                FlightUIStarter.showWindow = true;
                VABThermalUI.RenderWindow = false;
            }
            else
            {
                FlightUIStarter.hideButton = false;
                FlightUIStarter.showWindow = false;
                VABThermalUI.RenderWindow = true;
            }
        }

        void OnAppLauncherDeactivate()
        {
            FlightUIStarter.hideButton = true;
            FlightUIStarter.showWindow = false;
            VABThermalUI.RenderWindow = false;
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
            if (ApplicationLauncher.Ready && !_buttonAdded)
            {
                _appLauncherButton = InitializeApplicationButton();
                if (_appLauncherButton != null)
                    _appLauncherButton.VisibleInScenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT;

                _buttonAdded = true;
            }

            AddMissingTechNodes();

            if (resourcesConfigured) return;

            // read WarpPluginSettings.cfg
            var pluginSettingConfigs = GameDatabase.Instance.GetConfigNode(WARP_PLUGIN_SETTINGS_FILEPATH);

            if (pluginSettingConfigs == null)
            {
                ShowInstallationErrorMessage();
                return;
            }

            resourcesConfigured = true;
        }

        private void AddMissingTechNodes()
        {
            if (techNodesConfigured) return;

            if (ResearchAndDevelopment.Instance == null) return;

            techNodesConfigured = true;
            AssetBase.RnDTechTree.ReLoad();
            var rdNodes = AssetBase.RnDTechTree.GetTreeNodes().ToList();

            if (ResearchAndDevelopment.GetTechnologyState("ionPropulsion") == RDTech.State.Available &&
                ResearchAndDevelopment.GetTechnologyState("experimentalPropulsion") == RDTech.State.Unavailable)
            {
                //for some reason, FindNodeByID is not a static method and you need a reference
                if (rdNodes[0].FindNodeByID("experimentalPropulsion", rdNodes) is ProtoRDNode rdNode)
                {
                    rdNode.tech.state = RDTech.State.Available;
                    ResearchAndDevelopment.Instance.SetTechState(rdNode.tech.techID, rdNode.tech);
                    ResearchAndDevelopment.Instance.UnlockProtoTechNode(rdNode.tech);
                }
            }

            if (ResearchAndDevelopment.GetTechnologyState("extremeNuclearPropulsion") == RDTech.State.Available &&
                ResearchAndDevelopment.GetTechnologyState("highPowerExoticNuclearPropulsion") == RDTech.State.Unavailable)
            {
                //for some reason, FindNodeByID is not a static method and you need a reference
                if (rdNodes[0].FindNodeByID("highPowerExoticNuclearPropulsion", rdNodes) is ProtoRDNode rdNode)
                {
                    rdNode.tech.state = RDTech.State.Available;
                    ResearchAndDevelopment.Instance.SetTechState(rdNode.tech.techID, rdNode.tech);
                    ResearchAndDevelopment.Instance.UnlockProtoTechNode(rdNode.tech);
                }
            }

            if (ResearchAndDevelopment.GetTechnologyState("advHeatManagement") == RDTech.State.Available &&
                ResearchAndDevelopment.GetTechnologyState("intermediateHeatManagement") == RDTech.State.Unavailable)
            {
                //for some reason, FindNodeByID is not a static method and you need a reference
                if (rdNodes[0].FindNodeByID("intermediateHeatManagement", rdNodes) is ProtoRDNode rdNode)
                {
                    rdNode.tech.state = RDTech.State.Available;
                    ResearchAndDevelopment.Instance.SetTechState(rdNode.tech.techID, rdNode.tech);
                    ResearchAndDevelopment.Instance.UnlockProtoTechNode(rdNode.tech);
                }
            }

            if (ResearchAndDevelopment.GetTechnologyState("specializedRadiators") == RDTech.State.Available &&
                ResearchAndDevelopment.GetTechnologyState("experimentalHeatManagement") == RDTech.State.Unavailable)
            {
                //for some reason, FindNodeByID is not a static method and you need a reference
                if (rdNodes[0].FindNodeByID("experimentalHeatManagement", rdNodes) is ProtoRDNode rdNode)
                {
                    rdNode.tech.state = RDTech.State.Available;
                    ResearchAndDevelopment.Instance.SetTechState(rdNode.tech.techID, rdNode.tech);
                    ResearchAndDevelopment.Instance.UnlockProtoTechNode(rdNode.tech);
                }
            }
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

        private static Font _mainFont;
        public static Font MainFont
        {
            get
            {
                if (_mainFont == null)
                    _mainFont = Font.CreateDynamicFontFromOSFont("Arial", 11);

                return _mainFont;
            }
        }
    }
}
