using FNPlugin.Extensions;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using KSP.Localization;

namespace FNPlugin
{
    [KSPModule("Fusion Engine")]
    class FusionEngineController : DaedalusEngineController { }

    [KSPModule("Inertial Confinement Fusion Engine")]
    class DaedalusEngineController : ResourceSuppliableModule, IUpgradeableModule 
    {
        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        //[KSPField(isPersistant = true)]
        //public bool isupgraded;
        [KSPField(isPersistant = true)]
        public bool rad_safety_features = true;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_speedLimit", guiUnits = "c"), UI_FloatRange(stepIncrement = 1 / 3f, maxValue = 1, minValue = 1 / 3f)]
        public float speedLimit = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_fuelLimit", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        public float fuelLimit = 100;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_maximizeThrust"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool maximizeThrust = true;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_powerUsage")]
        public string powerUsage;
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_fusionFuel")]
        public string fusionFuel = "FusionPellets";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_temperatureStr")]
        public string temperatureStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_speedOfLight", guiUnits = " m/s")]
        public double speedOfLight;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_relativity", guiFormat = "F10")]
        public double relativity;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")]
        public double timeDilation;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_radhazardstr")]
        public string radhazardstr = "";
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiUnits = " t")]
        public float partMass = 1;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fusionRatio", guiFormat = "F6")]
        public double fusionRatio = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsRatio")]
        public string fuelAmountsRatio;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_thrustPowerInTeraWatt", guiFormat = "F2", guiUnits = " TW")]
        public double thrustPowerInTeraWatt = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_calculatedFuelflow", guiFormat = "F6", guiUnits = " U")]
        public double calculatedFuelflow = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateKgPerSecond", guiFormat = "F6", guiUnits = " kg/s")]
        public double massFlowRateKgPerSecond;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateTonPerHour", guiFormat = "F6", guiUnits = " t/h")]
        public double massFlowRateTonPerHour;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_storedThrotle")]
        public float storedThrotle = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveMaxThrustInKiloNewton", guiFormat = "F2", guiUnits = " kN")]
        public double effectiveMaxThrustInKiloNewton = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveIsp", guiFormat = "F2", guiUnits = "s")]
        public double effectiveIsp = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_worldSpaceVelocity", guiFormat = "F3", guiUnits = " m/s")]
        public double worldSpaceVelocity;

        [KSPField]
        public double universalTime;
        //[KSPField]
        //public float powerRequirement = 2500;

        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk1;
        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk2;
        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk3;
        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk4;
        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk5;
        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk6;
        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk7;
        [KSPField(guiActiveEditor = true, guiName = "<size=10>Upgrade Tech</size>")]
        public string translatedTechMk8;

        [KSPField]
        public float maxThrustMk1 = 300;
        [KSPField]
        public float maxThrustMk2 = 500;
        [KSPField]
        public float maxThrustMk3 = 800;
        [KSPField]
        public float maxThrustMk4 = 1200;
        [KSPField]
        public float maxThrustMk5 = 1500;
        [KSPField]
        public float maxThrustMk6 = 2000;
        [KSPField]
        public float maxThrustMk7 = 2500;
        [KSPField]
        public float maxThrustMk8 = 3000;
        [KSPField]
        public float maxThrustMk9 = 3500;

        [KSPField(guiActiveEditor = true, guiName = "Mk1")]
        public string guiMaxThrustMk1;
        [KSPField(guiActiveEditor = true, guiName = "Mk2")]
        public string guiMaxThrustMk2;
        [KSPField(guiActiveEditor = true, guiName = "Mk3")]
        public string guiMaxThrustMk3;
        [KSPField(guiActiveEditor = true, guiName = "Mk4")]
        public string guiMaxThrustMk4;
        [KSPField(guiActiveEditor = true, guiName = "Mk5")]
        public string guiMaxThrustMk5;
        [KSPField(guiActiveEditor = true, guiName = "Mk6")]
        public string guiMaxThrustMk6;
        [KSPField(guiActiveEditor = true, guiName = "Mk7")]
        public string guiMaxThrustMk7;
        [KSPField(guiActiveEditor = true, guiName = "Mk8")]
        public string guiMaxThrustMk8;
        [KSPField(guiActiveEditor = true, guiName = "Mk9")]
        public string guiMaxThrustMk9;

        [KSPField]
        public float wasteheatMk1 = 2500;
        [KSPField]
        public float wasteheatMk2 = 2500;
        [KSPField]
        public float wasteheatMk3 = 2500;
        [KSPField]
        public float wasteheatMk4 = 2500;
        [KSPField]
        public float wasteheatMk5 = 2500;
        [KSPField]
        public float wasteheatMk6 = 2500;
        [KSPField]
        public float wasteheatMk7 = 2500;
        [KSPField]
        public float wasteheatMk8 = 2500;
        [KSPField]
        public float wasteheatMk9 = 2500;

        [KSPField]
        public double efficiencyMk1 = 0.55;
        [KSPField]
        public double efficiencyMk2 = 0.60;
        [KSPField]
        public double efficiencyMk3 = 0.65;
        [KSPField]
        public double efficiencyMk4 = 0.70;
        [KSPField]
        public double efficiencyMk5 = 0.75;
        [KSPField]
        public double efficiencyMk6 = 0.80;
        [KSPField]
        public double efficiencyMk7 = 0.85;
        [KSPField]
        public double efficiencyMk8 = 0.90;
        [KSPField]
        public double efficiencyMk9 = 0.95;

        [KSPField]
        public double powerRequirementMk1 = 2000;
        [KSPField]
        public double powerRequirementMk2 = 3000;
        [KSPField]
        public double powerRequirementMk3 = 4000;
        [KSPField]
        public double powerRequirementMk4 = 5000;
        [KSPField]
        public double powerRequirementMk5 = 6000;
        [KSPField]
        public double powerRequirementMk6 = 7000;
        [KSPField]
        public double powerRequirementMk7 = 8000;
        [KSPField]
        public double powerRequirementMk8 = 9000;
        [KSPField]
        public double powerRequirementMk9 = 10000;


        [KSPField(guiActive = true, guiName = "Available Upgrade Techs")]
        public int numberOfAvailableUpgradeTechs;
        [KSPField]
        public float maxAtmosphereDensity = 0;
        [KSPField]
        public float leathalDistance = 2000;
        [KSPField]
        public float killDivider = 50;
        [KSPField]
        public float wasteHeatMultiplier = 1;
        [KSPField]
        public float powerRequirementMultiplier = 1;
        [KSPField]
        public float maxTemp = 3200;
        [KSPField]
        public int powerPriority = 4;
        [KSPField]
        public float upgradeCost = 100;
        [KSPField]
        public string originalName = "Prototype Deadalus IC Fusion Engine";
        [KSPField]
        public string upgradedName = "Deadalus IC Fusion Engine";

        [KSPField]
        public string upgradeTechReq1;
        [KSPField]
        public string upgradeTechReq2;
        [KSPField]
        public string upgradeTechReq3;
        [KSPField]
        public string upgradeTechReq4;
        [KSPField]
        public string upgradeTechReq5;
        [KSPField]
        public string upgradeTechReq6;
        [KSPField]
        public string upgradeTechReq7;
        [KSPField]
        public string upgradeTechReq8;

        //bool hasrequiredupgrade;
        bool radhazard;
        bool warpToReal;
        float engineIsp;
        double percentageFuelRemaining;

        Stopwatch stopWatch;
        ModuleEngines curEngineT;
        BaseEvent deactivateRadSafetyEvent;
        BaseEvent activateRadSafetyEvent;
        BaseEvent retrofitEngineEvent;
        BaseField radhazardstrField;
        PartResourceDefinition fusionFuelResourceDefinition;

        private int _engineGenerationType;
        public GenerationType EngineGenerationType
        {
            get { return (GenerationType) _engineGenerationType; }
            private set { _engineGenerationType = (int) value; }
        }

        [KSPEvent(guiActive = true, guiName = "Disable Radiation Safety", active = true)]
        public void DeactivateRadSafety() 
        {
            rad_safety_features = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Radiation Safety", active = false)]
        public void ActivateRadSafety() 
        {
            rad_safety_features = true;
        }

        //[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        //public void RetrofitEngine()
        //{
        //    if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

        //    upgradePartModule();
        //    ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        //}

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq1; } }

        private double Efficiency
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1:
                        return efficiencyMk1;
                    case (int)GenerationType.Mk2:
                        return efficiencyMk2;
                    case (int)GenerationType.Mk3:
                        return efficiencyMk3;
                    case (int)GenerationType.Mk4:
                        return efficiencyMk4;
                    case (int)GenerationType.Mk5:
                        return efficiencyMk5;
                    case (int)GenerationType.Mk6:
                        return efficiencyMk6;
                    case (int)GenerationType.Mk7:
                        return efficiencyMk7;
                    case (int)GenerationType.Mk8:
                        return efficiencyMk8;
                    default:
                        return efficiencyMk9;
                }
            }
        }

        private float MaximumThrust
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1:
                        return maxThrustMk1;
                    case (int)GenerationType.Mk2:
                        return maxThrustMk2;
                    case (int)GenerationType.Mk3:
                        return maxThrustMk3;
                    case (int)GenerationType.Mk4:
                        return maxThrustMk4;
                    case (int)GenerationType.Mk5:
                        return maxThrustMk5;
                    case (int)GenerationType.Mk6:
                        return maxThrustMk6;
                    case (int)GenerationType.Mk7:
                        return maxThrustMk7;
                    case (int)GenerationType.Mk8:
                        return maxThrustMk8;
                    default:
                        return maxThrustMk9;
                }
            }
        }
        private float FusionWasteHeat
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1:
                        return wasteheatMk1;
                    case (int)GenerationType.Mk2:
                        return wasteheatMk2;
                    case (int)GenerationType.Mk3:
                        return wasteheatMk3;
                    case (int)GenerationType.Mk4:
                        return wasteheatMk4;
                    case (int)GenerationType.Mk5:
                        return wasteheatMk5;
                    case (int)GenerationType.Mk6:
                        return wasteheatMk6;
                    case (int)GenerationType.Mk7:
                        return wasteheatMk7;
                    case (int)GenerationType.Mk8:
                        return wasteheatMk8;
                    default:
                        return maxThrustMk9;
                }
            }
        }

        public double PowerRequirement
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1:
                        return powerRequirementMk1;
                    case (int)GenerationType.Mk2:
                        return powerRequirementMk2;
                    case (int)GenerationType.Mk3:
                        return powerRequirementMk3;
                    case (int)GenerationType.Mk4:
                        return powerRequirementMk4;
                    case (int)GenerationType.Mk5:
                        return powerRequirementMk5;
                    case (int)GenerationType.Mk6:
                        return powerRequirementMk6;
                    case (int)GenerationType.Mk7:
                        return powerRequirementMk7;
                    case (int)GenerationType.Mk8:
                        return powerRequirementMk8;
                    default:
                        return powerRequirementMk9;
                }
            }
        }

        private double EffectivePowerRequirement
        {
            get
            {
                return PowerRequirement * powerRequirementMultiplier;
            }
        }

        public void upgradePartModule()
        {
            //isupgraded = true;
        }

        #endregion

        public override void OnStart(StartState state) 
        {
            try
            {
                stopWatch = new Stopwatch();
                speedOfLight = GameConstants.speedOfLight * PluginHelper.SpeedOfLightMult;
                fusionFuelResourceDefinition = PartResourceLibrary.Instance.GetDefinition(fusionFuel);

                part.maxTemp = maxTemp;
                part.thermalMass = 1;
                part.thermalMassModifier = 1;

                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();

                if (curEngineT == null) return;

                engineIsp = curEngineT.atmosphereCurve.Evaluate(0);

                //// if we can upgrade, let's do so
                //if (isupgraded)
                //    upgradePartModule();
                //else if (this.HasTechsRequiredToUpgrade())
                //    hasrequiredupgrade = true;

                //if (state == StartState.Editor && this.HasTechsRequiredToUpgrade())
                //{
                //    isupgraded = true;
                //    upgradePartModule();
                //}

                DetermineTechLevel();

                // bind with fields and events
                deactivateRadSafetyEvent = Events["DeactivateRadSafety"];
                activateRadSafetyEvent = Events["ActivateRadSafety"];
                retrofitEngineEvent = Events["RetrofitEngine"];
                radhazardstrField = Fields["radhazardstr"];

                translatedTechMk1 = DisplayTech(upgradeTechReq1);
                translatedTechMk2 = DisplayTech(upgradeTechReq2);
                translatedTechMk3 = DisplayTech(upgradeTechReq3);
                translatedTechMk4 = DisplayTech(upgradeTechReq4);
                translatedTechMk5 = DisplayTech(upgradeTechReq5);
                translatedTechMk6 = DisplayTech(upgradeTechReq6);
                translatedTechMk7 = DisplayTech(upgradeTechReq7);
                translatedTechMk8 = DisplayTech(upgradeTechReq8);

                Fields["translatedTechMk1"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk1);
                Fields["translatedTechMk2"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk2);
                Fields["translatedTechMk3"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk3);
                Fields["translatedTechMk4"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk4);
                Fields["translatedTechMk5"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk5);
                Fields["translatedTechMk6"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk6);
                Fields["translatedTechMk7"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk7);
                Fields["translatedTechMk8"].guiActiveEditor = !String.IsNullOrEmpty(translatedTechMk8);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnStart " + e.Message + " stack " + e.StackTrace);
            }
        }

        private string DisplayTech(string techid)
        {
            if (String.IsNullOrEmpty(techid))
                return string.Empty;

            var translatedTech = Localizer.Format(PluginHelper.GetTechTitleById(techid));

            if (PluginHelper.UpgradeAvailable(techid))
                return "<size=10><color=green>Ѵ</color> " + translatedTech + "</size>";
            else
                return "<size=10><color=red>X</color> " + translatedTech + "</size>";
        }

        private void DetermineTechLevel()
        {
            numberOfAvailableUpgradeTechs = 0;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq1))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq2))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq3))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq4))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq5))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq6))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq7))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq8))
                numberOfAvailableUpgradeTechs++;

            EngineGenerationType = (GenerationType) numberOfAvailableUpgradeTechs;
        }

        public void Update()
        {
            try
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    powerUsage = (EffectivePowerRequirement / 1000d).ToString("0.000") + " GW";

                    guiMaxThrustMk1 = FormatStatistics(powerRequirementMk1, maxThrustMk1, EngineGenerationType == GenerationType.Mk1 ? "#7fdfffff" : null);
                    guiMaxThrustMk2 = FormatStatistics(powerRequirementMk2, maxThrustMk2, EngineGenerationType == GenerationType.Mk2 ? "#7fdfffff" : null);
                    guiMaxThrustMk3 = FormatStatistics(powerRequirementMk3, maxThrustMk3, EngineGenerationType == GenerationType.Mk3 ? "#7fdfffff" : null);
                    guiMaxThrustMk4 = FormatStatistics(powerRequirementMk4, maxThrustMk4, EngineGenerationType == GenerationType.Mk4 ? "#7fdfffff" : null);
                    guiMaxThrustMk5 = FormatStatistics(powerRequirementMk5, maxThrustMk5, EngineGenerationType == GenerationType.Mk5 ? "#7fdfffff" : null);
                    guiMaxThrustMk6 = FormatStatistics(powerRequirementMk6, maxThrustMk6, EngineGenerationType == GenerationType.Mk6 ? "#7fdfffff" : null);
                    guiMaxThrustMk7 = FormatStatistics(powerRequirementMk7, maxThrustMk7, EngineGenerationType == GenerationType.Mk7 ? "#7fdfffff" : null);
                    guiMaxThrustMk8 = FormatStatistics(powerRequirementMk8, maxThrustMk8, EngineGenerationType == GenerationType.Mk8 ? "#7fdfffff" : null);
                    guiMaxThrustMk9 = FormatStatistics(powerRequirementMk9, maxThrustMk9, EngineGenerationType == GenerationType.Mk9 ? "#7fdfffff" : null);

                    return;
                }

                double fusionFuelCurrentAmount;
                double fusionFuelMaxAmount;
                part.GetConnectedResourceTotals(fusionFuelResourceDefinition.id, out fusionFuelCurrentAmount, out fusionFuelMaxAmount);

                percentageFuelRemaining = fusionFuelCurrentAmount / fusionFuelMaxAmount * 100;
                fuelAmountsRatio = percentageFuelRemaining.ToString("0.0000") + "% " + fusionFuelMaxAmount.ToString("0") + " L";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error Update " + e.Message + " stack " + e.StackTrace);
            }
        }

        private string FormatStatistics(double powerRequirement,  double value, string color = null, string format = "F0")
        {
            var result = (powerRequirement * powerRequirementMultiplier).ToString(format) + " MWe " + value.ToString(format) + "kN";

            if (String.IsNullOrEmpty(color))
                return result;

            return "<color=" + color + ">" + result + "</color>";
        }

        //
        public override void OnUpdate()
        {

            if (curEngineT == null) return;

            try
            {
                // When transitioning from timewarp to real update throttle
                if (warpToReal)
                {
                    vessel.ctrlState.mainThrottle = storedThrotle;
                    warpToReal = false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnUpdate warpToReal: " + e.Message);
            }

            try
            {
                deactivateRadSafetyEvent.active = rad_safety_features;
                activateRadSafetyEvent.active = !rad_safety_features;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnUpdate Events: " + e.Message);
            }

            try
            {

                if (curEngineT.isOperational && !IsEnabled)
                {
                    IsEnabled = true;
                    part.force_activate();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnUpdate force_activate: " + e.Message);
            }


            try
            {
                var kerbalHazardCount = 0;
                foreach (var vess in FlightGlobals.Vessels)
                {
                    var distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);
                    if (distance < leathalDistance && vess != this.vessel)
                        kerbalHazardCount += vess.GetCrewCount();
                }

                if (kerbalHazardCount > 0)
                {
                    radhazard = true;
                    if (kerbalHazardCount > 1)
                        radhazardstr = kerbalHazardCount + " Kerbals.";
                    else
                        radhazardstr = kerbalHazardCount + " Kerbal.";

                    radhazardstrField.guiActive = true;
                }
                else
                {
                    radhazardstrField.guiActive = false;
                    radhazard = false;
                    radhazardstr = "None.";
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnUpdate kerbalHazardCount " + e.Message);
            }
        }

        private void ShutDown(string reason)
        {
            try
            {
                curEngineT.Events["Shutdown"].Invoke();
                curEngineT.currentThrottle = 0;
                curEngineT.requestedThrottle = 0;

                ScreenMessages.PostScreenMessage(reason, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                foreach (var fxGroup in part.fxGroups)
                {
                    fxGroup.setActive(false);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error ShutDown " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void CalculateTimeDialation()
        {
            try
            {
                worldSpaceVelocity = vessel.orbit.GetFrameVel().magnitude;

                lightSpeedRatio = Math.Min(worldSpaceVelocity / speedOfLight, 0.9999999999);

                timeDilation = Math.Sqrt(1 - (lightSpeedRatio * lightSpeedRatio));

                relativity = 1 / timeDilation;                
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error CalculateTimeDialation " + e.Message + " stack " + e.StackTrace);
            }
        }

        public void FixedUpdate()
        {
            try
            {
                if (HighLogic.LoadedSceneIsEditor)
                    return;

                if (!IsEnabled)
                    UpdateTime();

                temperatureStr = part.temperature.ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error FixedUpdate " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void UpdateTime()
        {
            try
            {
                universalTime = Planetarium.GetUniversalTime();
                CalculateTimeDialation();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error UpdateTime " + e.Message + " stack " + e.StackTrace);
            }
        }

        public override void OnFixedUpdate()
        {
            try
            {
                if (curEngineT == null) return;

                stopWatch.Reset();
                stopWatch.Start();

                UpdateTime();

                var throttle = curEngineT.currentThrottle > 0 ? Mathf.Max(curEngineT.currentThrottle, 0.01f) : 0;

                if (throttle > 0)
                {
                    if (vessel.atmDensity > maxAtmosphereDensity)
                        ShutDown("Inertial Fusion cannot operate in atmosphere!");

                    if (radhazard && rad_safety_features)
                        ShutDown("Engines throttled down as they presently pose a radiation hazard");
                }

                KillKerbalsWithRadiation(throttle);

                if (!this.vessel.packed && !warpToReal)
                    storedThrotle = vessel.ctrlState.mainThrottle;

                // Update ISP
                effectiveIsp = timeDilation * engineIsp;

                UpdateAtmosphericCurve(effectiveIsp);

                if (throttle > 0 && !this.vessel.packed)
                {
                    if (part.vessel.geeForce <= 2)
                        part.vessel.IgnoreGForces(1);

                    fusionRatio = ProcessPowerAndWasteHeat(throttle);

                    // Update FuelFlow
                    effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust * fusionRatio;
                    calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / PluginHelper.GravityConstant;
                    massFlowRateKgPerSecond = curEngineT.currentThrottle * calculatedFuelflow * 1000;

                    if (!curEngineT.getFlameoutState && fusionRatio < 0.01)
                    {
                        curEngineT.status = "Insufficient Electricity";
                    }
                }
                else if (this.vessel.packed && curEngineT.enabled && FlightGlobals.ActiveVessel == vessel && throttle > 0 && percentageFuelRemaining > (100 - fuelLimit) && lightSpeedRatio < speedLimit)
                {
                    warpToReal = true; // Set to true for transition to realtime

                    fusionRatio = CheatOptions.InfiniteElectricity 
                        ? 1 
                        : maximizeThrust 
                            ? ProcessPowerAndWasteHeat(1) 
                            : ProcessPowerAndWasteHeat(storedThrotle);

                    effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust * fusionRatio;
                    calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / PluginHelper.GravityConstant;
                    massFlowRateKgPerSecond = calculatedFuelflow * 1000;

                    if (TimeWarp.fixedDeltaTime > 20)
                    {
                        var deltaCalculations = (float)Math.Ceiling(TimeWarp.fixedDeltaTime / 20);
                        var deltaTimeStep = TimeWarp.fixedDeltaTime / deltaCalculations;

                        for (var step = 0; step < deltaCalculations; step++)
                        {
                            PersistantThrust(deltaTimeStep, universalTime + (step * deltaTimeStep), this.part.transform.up, this.vessel.GetTotalMass());
                            CalculateTimeDialation();
                        }
                    }
                    else
                        PersistantThrust(TimeWarp.fixedDeltaTime, universalTime, this.part.transform.up, this.vessel.GetTotalMass());
                }
                else
                {
                    powerUsage = "0.000 GW / " + (EffectivePowerRequirement / 1000d).ToString("0.000") + " GW";

                    if (!(percentageFuelRemaining > (100 - fuelLimit) || lightSpeedRatio > speedLimit))
                    {
                        warpToReal = false;
                        vessel.ctrlState.mainThrottle = 0;
                    }

                    effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                    calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / PluginHelper.GravityConstant;
                    massFlowRateKgPerSecond = 0;
                }

                curEngineT.maxFuelFlow = (float)calculatedFuelflow;
                curEngineT.maxThrust = (float)effectiveMaxThrustInKiloNewton;
                
                massFlowRateTonPerHour = massFlowRateKgPerSecond * 3.6;
                thrustPowerInTeraWatt = effectiveMaxThrustInKiloNewton * 500 * effectiveIsp * PluginHelper.GravityConstant * 1e-12;

                stopWatch.Stop();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error UpdateTime " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void UpdateAtmosphericCurve(double isp)
        {
            var newAtmosphereCurve = new FloatCurve();
            newAtmosphereCurve.Add(0, (float)isp);
            newAtmosphereCurve.Add(maxAtmosphereDensity, 0);
            curEngineT.atmosphereCurve = newAtmosphereCurve;
        }

        private void PersistantThrust(float modifiedFixedDeltaTime, double modifiedUniversalTime, Vector3d thrustVector, float vesselMass)
        {
            var timeDilationMaximumThrust = timeDilation * timeDilation * MaximumThrust * (maximizeThrust ? 1 : storedThrotle);
            var timeDialationEngineIsp = timeDilation * engineIsp;

            double demandMass;
            thrustVector.CalculateDeltaVV(vesselMass, modifiedFixedDeltaTime, timeDilationMaximumThrust * fusionRatio, timeDialationEngineIsp, out demandMass);

            var fusionFuelRequestAmount = demandMass / fusionFuelResourceDefinition.density;

            double recievedRatio;
            if (CheatOptions.InfinitePropellant)
                recievedRatio = 1;
            else
            {
                var recievedFusionFuel = part.RequestResource(fusionFuelResourceDefinition.id, fusionFuelRequestAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                recievedRatio = fusionFuelRequestAmount > 0 ? recievedFusionFuel/ fusionFuelRequestAmount : 0;
            }

            effectiveMaxThrustInKiloNewton = timeDilationMaximumThrust * recievedRatio;

            if (!(recievedRatio > 0.01)) return;

            var deltaVv = thrustVector.CalculateDeltaVV(vesselMass, modifiedFixedDeltaTime, effectiveMaxThrustInKiloNewton, timeDialationEngineIsp, out demandMass);
            vessel.orbit.Perturb(deltaVv, modifiedUniversalTime);
        }

        private double ProcessPowerAndWasteHeat(float throtle)
        {
            // Calculate Fusion Ratio
            var effectivePowerRequirement = EffectivePowerRequirement;
            var thrustPercentage = (double)(decimal)curEngineT.thrustPercentage;
            var requestedPower = (thrustPercentage * 0.01) * throtle * effectivePowerRequirement;

            var recievedPower = CheatOptions.InfiniteElectricity 
                ? requestedPower
                : consumeFNResourcePerSecond(requestedPower, ResourceManager.FNRESOURCE_MEGAJOULES);

            var plasmaRatio = effectivePowerRequirement > 0 ? recievedPower / requestedPower : 0;
            var wasteheatFusionRatio = plasmaRatio >= 1 ? 1 : plasmaRatio > 0.01 ? plasmaRatio : 0;

            powerUsage = (recievedPower / 1000d).ToString("0.000") + " GW / " + (effectivePowerRequirement * 0.001).ToString("0.000") + " GW";

            if (CheatOptions.IgnoreMaxTemperature) 
                return wasteheatFusionRatio;

            // Lasers produce Wasteheat
            supplyFNResourcePerSecond(recievedPower * (1 - Efficiency), ResourceManager.FNRESOURCE_WASTEHEAT);

            // The Aborbed wasteheat from Fusion
            supplyFNResourcePerSecond(FusionWasteHeat * wasteHeatMultiplier * wasteheatFusionRatio, ResourceManager.FNRESOURCE_WASTEHEAT);

            return wasteheatFusionRatio;
        }

        private void KillKerbalsWithRadiation(float throttle)
        {
            if (!radhazard || throttle <= 0 || rad_safety_features) return;

            var vesselsToRemove = new List<Vessel>();
            var crewToRemove = new List<ProtoCrewMember>();

            foreach (var vess in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                var invSqDist = distance / killDivider;
                var invSqMult = 1d / invSqDist / invSqDist;

                foreach (var crewMember in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1d - TimeWarp.fixedDeltaTime * invSqMult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(crewMember.name + " was killed by Neutron Radiation!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        crewToRemove.Add(crewMember);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(crewMember.name + " was killed by Neutron Radiation!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        vesselsToRemove.Add(vess);
                    }
                }
            }

            foreach (var vess in vesselsToRemove)
            {
                vess.rootPart.Die();
            }

            foreach (var crewMember in crewToRemove)
            {
                var vess = FlightGlobals.Vessels.Find(p => p.GetVesselCrew().Contains(crewMember));
                var partWithCrewMember = vess.Parts.Find(p => p.protoModuleCrew.Contains(crewMember));
                partWithCrewMember.RemoveCrewmember(crewMember);
                crewMember.Die();
            }
        }

        public override int getPowerPriority() 
        {
            return powerPriority;
        }
    }
}

