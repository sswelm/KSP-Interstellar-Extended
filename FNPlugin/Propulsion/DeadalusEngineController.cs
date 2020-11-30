using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.External;
using KSP.Localization;
using System;
using System.Collections.Generic;
using FNPlugin.Resources;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    [KSPModule("Fission Engine")]
    class FissionEngineController : DaedalusEngineController { }

    [KSPModule("Confinement Fusion Engine")]
    class FusionEngineController : DaedalusEngineController { }

    [KSPModule("Confinement Fusion Engine")]
    class DaedalusEngineController : ResourceSuppliableModule, IUpgradeableModule , IRescalable<DaedalusEngineController>
    {
        public const string GROUP = "FusionEngine";
        public const string GROUP_TITLE = "#LOC_KSPIE_FusionEngine_groupName";

        // Persistent
        [KSPField(isPersistant = true)]
        public double thrustMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double ispMultiplier = 1;
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool rad_safety_features = true;

        [KSPField]
        public double massThrustExp = 0;
        [KSPField]
        public double massIspExp = 0;
        [KSPField]
        public double higherScaleThrustExponent = 3;
        [KSPField]
        public double lowerScaleThrustExponent = 4;
        [KSPField]
        public double higherScaleIspExponent = 0.25;
        [KSPField]
        public double lowerScaleIspExponent = 1;
        [KSPField]
        public double GThreshold = 9;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_speedLimit", guiUnits = "c"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 1, minValue = 0.005f)]
        public float speedLimit = 1;
        [KSPField(groupName = GROUP, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_fuelLimit", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        public float fuelLimit = 100;
        [KSPField(groupName = GROUP, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_maximizeThrust"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool maximizeThrust = true;

        [KSPField(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_powerUsage")]
        public string powerUsage;
        [KSPField(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_wasteHeat", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double wasteHeat;

        [KSPField]
        public double finalRequestedPower;

        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string fusionFuel1 = string.Empty;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string fusionFuel2 = string.Empty;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string fusionFuel3 = string.Empty;

        [KSPField(groupName = GROUP, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_FusionEngine_fusionFuel")]
        public string fuelName1 = "FusionPellets";
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string fuelName2 = string.Empty;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public string fuelName3 = string.Empty;

        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double fuelRatio1 = 1;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double fuelRatio2 = 0;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double fuelRatio3 = 0;
        [KSPField]
        public string effectName = string.Empty;

        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_temperatureStr")]
        public string temperatureStr = "";
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_speedOfLight", guiUnits = " m/s")]
        public double engineSpeedOfLight;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio = 0;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_relativity", guiFormat = "F10")]
        public double relativity;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")]
        public double timeDilation;

        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_radhazardstr")]
        public string radhazardstr = "";
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiFormat = "F3", guiUnits = " t")]
        public float partMass = 1;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fusionRatio", guiFormat = "F3")]
        public double fusionRatio = 0;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsCurrent")]
        public double fuelAmounts;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsMax")]
        public double fuelAmountsMax;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsRatio")]
        public string fuelAmountsRatio;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_thrustPowerInTeraWatt", guiFormat = "F2", guiUnits = " TW")]
        public double thrustPowerInTeraWatt = 0;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_calculatedFuelflow", guiFormat = "F6", guiUnits = " U")]
        public double calculatedFuelflow = 0;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateKgPerSecond", guiFormat = "F6", guiUnits = " kg/s")]
        public double massFlowRateKgPerSecond;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateTonPerHour", guiFormat = "F6", guiUnits = " t/h")]
        public double massFlowRateTonPerHour;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_storedThrotle")]
        public float storedThrotle = 0;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveMaxThrustInKiloNewton", guiFormat = "F2", guiUnits = " kN")]
        public double effectiveMaxThrustInKiloNewton = 0;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveIsp", guiFormat = "F1", guiUnits = "s")]
        public double effectiveIsp = 0;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_worldSpaceVelocity", guiFormat = "F2", guiUnits = " m/s")]
        public double worldSpaceVelocity;

        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
        public string translatedTechMk1;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
        public string translatedTechMk2;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
        public string translatedTechMk3;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
        public string translatedTechMk4;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
        public string translatedTechMk5;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
        public string translatedTechMk6;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
        public string translatedTechMk7;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")]//<size=10>Upgrade Tech</size>
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

        [KSPField]
        public float wasteheatMk1 = 0;
        [KSPField]
        public float wasteheatMk2 = 0;
        [KSPField]
        public float wasteheatMk3 = 0;
        [KSPField]
        public float wasteheatMk4 = 0;
        [KSPField]
        public float wasteheatMk5 = 0;
        [KSPField]
        public float wasteheatMk6 = 0;
        [KSPField]
        public float wasteheatMk7 = 0;
        [KSPField]
        public float wasteheatMk8 = 0;
        [KSPField]
        public float wasteheatMk9 = 0;

        [KSPField]
        public double powerRequirementMk1 = 0;
        [KSPField]
        public double powerRequirementMk2 = 0;
        [KSPField]
        public double powerRequirementMk3 = 0;
        [KSPField]
        public double powerRequirementMk4 = 0;
        [KSPField]
        public double powerRequirementMk5 = 0;
        [KSPField]
        public double powerRequirementMk6 = 0;
        [KSPField]
        public double powerRequirementMk7 = 0;
        [KSPField]
        public double powerRequirementMk8 = 0;
        [KSPField]
        public double powerRequirementMk9 = 0;

        [KSPField]
        public double powerProductionMk1 = 0;
        [KSPField]
        public double powerProductionMk2 = 0;
        [KSPField]
        public double powerProductionMk3 = 0;
        [KSPField]
        public double powerProductionMk4 = 0;
        [KSPField]
        public double powerProductionMk5 = 0;
        [KSPField]
        public double powerProductionMk6 = 0;
        [KSPField]
        public double powerProductionMk7 = 0;
        [KSPField]
        public double powerProductionMk8 = 0;
        [KSPField]
        public double powerProductionMk9 = 0;

        [KSPField]
        public double thrustIspMk1 = 83886;
        [KSPField]
        public double thrustIspMk2 = 104857;
        [KSPField]
        public double thrustIspMk3 = 131072;
        [KSPField]
        public double thrustIspMk4 = 163840;
        [KSPField]
        public double thrustIspMk5 = 204800;
        [KSPField]
        public double thrustIspMk6 = 256000;
        [KSPField]
        public double thrustIspMk7 = 320000;
        [KSPField]
        public double thrustIspMk8 = 400000;
        [KSPField]
        public double thrustIspMk9 = 500000;

        [KSPField]
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
        public double powerThrottleExponent = 0.5;
        [KSPField]
        public double ispThrottleExponent = 0.5;
        [KSPField]
        public double fuelNeutronsFraction = 0.005;
        [KSPField]
        public int powerPriority = 4;
        [KSPField]
        public float upgradeCost = 100;
        [KSPField]
        public string originalName = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_originalName");//"Prototype Deadalus IC Fusion Engine"
        [KSPField]
        public string upgradedName = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_upgradedName");//"Deadalus IC Fusion Engine"

        [KSPField]
        public string upgradeTechReq1 = null;
        [KSPField]
        public string upgradeTechReq2 = null;
        [KSPField]
        public string upgradeTechReq3 = null;
        [KSPField]
        public string upgradeTechReq4 = null;
        [KSPField]
        public string upgradeTechReq5 = null;
        [KSPField]
        public string upgradeTechReq6 = null;
        [KSPField]
        public string upgradeTechReq7 = null;
        [KSPField]
        public string upgradeTechReq8 = null;

        [KSPField]
        public double demandMass;
        [KSPField]
        public double fuelRatio;
        [KSPField]
        double averageDensity;
        [KSPField]
        float throttle;
        [KSPField]
        double ratioHeadingVersusRequest;

        [KSPField]
        public double fuelFactor1;
        [KSPField]
        public double fuelFactor2;
        [KSPField]
        public double fuelFactor3;

        [KSPField]
        public double fusionFuelRequestAmount1 = 0.0;
        [KSPField]
        public double fusionFuelRequestAmount2 = 0.0;
        [KSPField]
        public double fusionFuelRequestAmount3 = 0.0;

        [KSPField]
        public double timeDilationMaximumThrust;

        FNEmitterController emitterController;
        ModuleEngines curEngineT;
        BaseEvent deactivateRadSafetyEvent;
        BaseEvent activateRadSafetyEvent;
        BaseField radhazardstrField;

        PartResourceDefinition fuelResourceDefinition1;
        PartResourceDefinition fuelResourceDefinition2;
        PartResourceDefinition fuelResourceDefinition3;

        const string LIGHTBLUE = "#7fdfffff";

        bool radhazard;
        bool warpToReal;
        double engineIsp;
        double universalTime;
        double percentageFuelRemaining;
        int vesselChangedSIOCountdown;
        int totalNumberOfGenerations;

        private int _engineGenerationType;
        public GenerationType EngineGenerationType
        {
            get => (GenerationType) _engineGenerationType;
            private set => _engineGenerationType = (int) value;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_DeadalusEngineController_DeactivateRadSafety", active = true)]//Disable Radiation Safety
        public void DeactivateRadSafety()
        {
            rad_safety_features = false;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_DeadalusEngineController_ActivateRadSafety", active = false)]//Activate Radiation Safety
        public void ActivateRadSafety()
        {
            rad_safety_features = true;
        }

        public void VesselChangedSOI()
        {
            vesselChangedSIOCountdown = 10;
        }

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq1; } }

        private float RawMaximumThrust
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

        private double MaximumThrust
        {
            get
            {
                return RawMaximumThrust * thrustMultiplier * Math.Pow(part.mass / partMass, massThrustExp);
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

        public double PowerProduction
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1:
                        return powerProductionMk1;
                    case (int)GenerationType.Mk2:
                        return powerProductionMk2;
                    case (int)GenerationType.Mk3:
                        return powerProductionMk3;
                    case (int)GenerationType.Mk4:
                        return powerProductionMk4;
                    case (int)GenerationType.Mk5:
                        return powerProductionMk5;
                    case (int)GenerationType.Mk6:
                        return powerProductionMk6;
                    case (int)GenerationType.Mk7:
                        return powerProductionMk7;
                    case (int)GenerationType.Mk8:
                        return powerProductionMk8;
                    default:
                        return powerProductionMk9;
                }
            }
        }

        public double RawEngineIsp
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1:
                        return thrustIspMk1;
                    case (int)GenerationType.Mk2:
                        return thrustIspMk2;
                    case (int)GenerationType.Mk3:
                        return thrustIspMk3;
                    case (int)GenerationType.Mk4:
                        return thrustIspMk4;
                    case (int)GenerationType.Mk5:
                        return thrustIspMk5;
                    case (int)GenerationType.Mk6:
                        return thrustIspMk6;
                    case (int)GenerationType.Mk7:
                        return thrustIspMk7;
                    case (int)GenerationType.Mk8:
                        return thrustIspMk8;
                    default:
                        return thrustIspMk9;
                }
            }
        }

        public double EngineIsp { get { return RawEngineIsp * ispMultiplier * Math.Pow(part.mass / partMass, massIspExp); } }

        private double EffectiveMaxPowerRequirement { get { return PowerRequirement * powerRequirementMultiplier; } }

        private double EffectiveMaxPowerProduction { get { return PowerProduction * powerRequirementMultiplier; } }

        private double EffectiveMaxFusionWasteHeat { get { return FusionWasteHeat * wasteHeatMultiplier; } }


        public void upgradePartModule()
        {
            //isupgraded = true;
        }

        #endregion

        public override void OnStart(StartState state)
        {
            string[] resources_to_supply = { ResourceManager.FNRESOURCE_WASTEHEAT, ResourceSettings.Config.ElectricPowerInMegawatt };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            engineSpeedOfLight = GameConstants.speedOfLight * PluginHelper.SpeedOfLightMult;

            UpdateFuelFactors();

            part.maxTemp = maxTemp;
            part.thermalMass = 1;
            part.thermalMassModifier = 1;

            curEngineT = part.FindModuleImplementing<ModuleEngines>();

            if (curEngineT == null) return;

            DetermineTechLevel();

            engineIsp = EngineIsp;

            // bind with fields and events
            deactivateRadSafetyEvent = Events[nameof(DeactivateRadSafety)];
            activateRadSafetyEvent = Events[nameof(ActivateRadSafety)];
            radhazardstrField = Fields[nameof(radhazardstr)];

            translatedTechMk1 = PluginHelper.DisplayTech(upgradeTechReq1);
            translatedTechMk2 = PluginHelper.DisplayTech(upgradeTechReq2);
            translatedTechMk3 = PluginHelper.DisplayTech(upgradeTechReq3);
            translatedTechMk4 = PluginHelper.DisplayTech(upgradeTechReq4);
            translatedTechMk5 = PluginHelper.DisplayTech(upgradeTechReq5);
            translatedTechMk6 = PluginHelper.DisplayTech(upgradeTechReq6);
            translatedTechMk7 = PluginHelper.DisplayTech(upgradeTechReq7);
            translatedTechMk8 = PluginHelper.DisplayTech(upgradeTechReq8);

            InitializeKerbalismEmitter();
        }

        private void InitializeKerbalismEmitter()
        {
            if (!Kerbalism.IsLoaded)
                return;

            emitterController = part.FindModuleImplementing<FNEmitterController>();

            if (emitterController == null)
                UnityEngine.Debug.LogWarning("[KSPI]: No Emitter Found om " + part.partInfo.title);
        }

        private void UpdateKerbalismEmitter()
        {
            if (emitterController == null)
                return;

            emitterController.reactorActivityFraction = fusionRatio;
            emitterController.exhaustActivityFraction = fusionRatio;
            emitterController.fuelNeutronsFraction = fuelNeutronsFraction;
        }

        private void UpdateFuelFactors()
        {

            if (!string.IsNullOrEmpty(fuelName1))
                fuelResourceDefinition1 = PartResourceLibrary.Instance.GetDefinition(fuelName1);
            else if (!string.IsNullOrEmpty(fusionFuel1))
                fuelResourceDefinition1 = PartResourceLibrary.Instance.GetDefinition(fusionFuel1);

            if (!string.IsNullOrEmpty(fuelName2))
                fuelResourceDefinition2 = PartResourceLibrary.Instance.GetDefinition(fuelName2);
            else if (!string.IsNullOrEmpty(fusionFuel2))
                fuelResourceDefinition2 = PartResourceLibrary.Instance.GetDefinition(fusionFuel2);

            if (!string.IsNullOrEmpty(fuelName3))
                fuelResourceDefinition3 = PartResourceLibrary.Instance.GetDefinition(fuelName3);
            else if (!string.IsNullOrEmpty(fusionFuel3))
                fuelResourceDefinition3 = PartResourceLibrary.Instance.GetDefinition(fusionFuel3);

            var ratioSum = 0.0;
            var densitySum = 0.0;

            if (fuelResourceDefinition1 != null)
            {
                ratioSum += fuelRatio1;
                densitySum += fuelResourceDefinition1.density * fuelRatio1;
            }
            if (fuelResourceDefinition2 != null)
            {
                ratioSum += fuelRatio2;
                densitySum += fuelResourceDefinition2.density * fuelRatio2;
            }
            if (fuelResourceDefinition3 != null)
            {
                ratioSum += fuelRatio3;
                densitySum += fuelResourceDefinition3.density * fuelRatio3;
            }

            averageDensity = densitySum / ratioSum;

            fuelFactor1 = fuelResourceDefinition1 != null ? fuelRatio1/ratioSum : 0;
            fuelFactor2 = fuelResourceDefinition2 != null ? fuelRatio2/ratioSum : 0;
            fuelFactor3 = fuelResourceDefinition3 != null ? fuelRatio3/ratioSum : 0;
        }

        private void DetermineTechLevel()
        {
            totalNumberOfGenerations = 1;
            if (!string.IsNullOrEmpty(upgradeTechReq1))
                totalNumberOfGenerations++;
            if (!string.IsNullOrEmpty(upgradeTechReq2))
                totalNumberOfGenerations++;
            if (!string.IsNullOrEmpty(upgradeTechReq3))
                totalNumberOfGenerations++;
            if (!string.IsNullOrEmpty(upgradeTechReq4))
                totalNumberOfGenerations++;
            if (!string.IsNullOrEmpty(upgradeTechReq5))
                totalNumberOfGenerations++;
            if (!string.IsNullOrEmpty(upgradeTechReq6))
                totalNumberOfGenerations++;
            if (!string.IsNullOrEmpty(upgradeTechReq7))
                totalNumberOfGenerations++;
            if (!string.IsNullOrEmpty(upgradeTechReq8))
                totalNumberOfGenerations++;

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
            if (HighLogic.LoadedSceneIsEditor)
            {
                // configure engine for Kerbal Engeneer support
                UpdateAtmosphericCurve(EngineIsp);
                effectiveMaxThrustInKiloNewton = MaximumThrust;
                calculatedFuelflow = effectiveMaxThrustInKiloNewton / EngineIsp / GameConstants.STANDARD_GRAVITY;
                curEngineT.maxFuelFlow = (float)calculatedFuelflow;
                curEngineT.maxThrust = (float)effectiveMaxThrustInKiloNewton;
                powerUsage = EffectiveMaxPowerRequirement.ToString("0.00") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");
                wasteHeat = EffectiveMaxFusionWasteHeat;
            }
            else
            {
                part.GetConnectedResourceTotals(fuelResourceDefinition1.id, out fuelAmounts, out fuelAmountsMax);

                percentageFuelRemaining = fuelAmountsMax > 0 ? fuelAmounts / fuelAmountsMax * 100 : 0;
                fuelAmountsRatio = percentageFuelRemaining.ToString("0.000") + "% ";
            }
        }

        private string FormatThrustStatistics(double value, double isp, string color = null, string format = "F0")
        {
            var result = value.ToString(format) + " kN @ " + isp.ToString(format) + "s";

            if (string.IsNullOrEmpty(color))
                return result;

            return "<color=" + color + ">" + result + "</color>";
        }

        private string FormatPowerStatistics(double powerRequirement, double wasteheat, string color = null, string format = "F0")
        {
            var result = (powerRequirement * powerRequirementMultiplier).ToString(format) + " MWe / " + wasteheat.ToString(format) + " MJ";

            if (string.IsNullOrEmpty(color))
                return result;

            return "<color=" + color + ">" + result + "</color>";
        }

        // Note: we assume OnRescale is called at load and after any time tweakscale changes the size of an part
        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            UnityEngine.Debug.Log("[KSPI]: DaedalusEngineController OnRescale was called with factor " + factor.absolute.linear);

            var storedAbsoluteFactor = (double)(decimal)factor.absolute.linear;

            thrustMultiplier = storedAbsoluteFactor >= 1 ? Math.Pow(storedAbsoluteFactor, higherScaleThrustExponent) : Math.Pow(storedAbsoluteFactor, lowerScaleThrustExponent);
            ispMultiplier = storedAbsoluteFactor >= 1 ? Math.Pow(storedAbsoluteFactor, higherScaleIspExponent) : Math.Pow(storedAbsoluteFactor, lowerScaleIspExponent);
        }

        public override void OnUpdate()
        {
            // stop engines and drop out of timewarp when X pressed
            if (vessel.packed && storedThrotle > 0 && Input.GetKeyDown(KeyCode.X))
            {
                // Return to realtime
                TimeWarp.SetRate(0, true);

                storedThrotle = 0;
                vessel.ctrlState.mainThrottle = storedThrotle;
            }

            if (curEngineT == null) return;

            // When transitioning from timewarp to real update radiationRatio
            if (warpToReal)
            {
                vessel.ctrlState.mainThrottle = storedThrotle;
                warpToReal = false;
            }

            deactivateRadSafetyEvent.active = rad_safety_features;
            activateRadSafetyEvent.active = !rad_safety_features;

            if (curEngineT.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                UnityEngine.Debug.Log("[KSPI]: DeadalusEngineController on " + part.name + " was Force Activated");
                part.force_activate();
            }

            var kerbalHazardCount = 0;
            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);
                if (distance < leathalDistance && currentVessel != this.vessel)
                    kerbalHazardCount += currentVessel.GetCrewCount();
            }

            if (kerbalHazardCount > 0)
            {
                radhazard = true;
                radhazardstr = Localizer.Format(kerbalHazardCount > 1
                    ? "#LOC_KSPIE_DeadalusEngineController_radhazardstr2"
                    : "#LOC_KSPIE_DeadalusEngineController_radhazardstr1", kerbalHazardCount);

                radhazardstrField.guiActive = true;
            }
            else
            {
                radhazardstrField.guiActive = false;
                radhazard = false;
                radhazardstr = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_radhazardstr3");//"None."
            }

            Fields["powerUsage"].guiActive = EffectiveMaxPowerRequirement > 0;
            Fields["wasteHeat"].guiActive = EffectiveMaxFusionWasteHeat > 0;
        }

        private void ShutDown(string reason)
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

        private void CalculateTimeDialation()
        {
            worldSpaceVelocity = vessel.orbit.GetFrameVel().magnitude;

            lightSpeedRatio = Math.Min(worldSpaceVelocity / engineSpeedOfLight, 0.9999999999);

            timeDilation = Math.Sqrt(1 - (lightSpeedRatio * lightSpeedRatio));

            relativity = 1 / timeDilation;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            if (!IsEnabled)
            {
                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, 0, -1);
                UpdateTime();
            }

            temperatureStr = part.temperature.ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";
        }

        private void UpdateTime()
        {
            universalTime = Planetarium.GetUniversalTime();
            CalculateTimeDialation();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (curEngineT == null) return;

            if (vesselChangedSIOCountdown > 0)
                vesselChangedSIOCountdown--;

            UpdateTime();

            throttle = !curEngineT.getFlameoutState && curEngineT.currentThrottle > 0 ? Mathf.Max(curEngineT.currentThrottle, 0.01f) : 0;

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown(Localizer.Format("#LOC_KSPIE_DeadalusEngineController_Shutdownreason1"));//"Inertial Fusion cannot operate in atmosphere!"

                if (radhazard && rad_safety_features)
                    ShutDown(Localizer.Format("#LOC_KSPIE_DeadalusEngineController_Shutdownreason2"));//"Engines throttled down as they presently pose a radiation hazard"
            }

            KillKerbalsWithRadiation(throttle);

            if (!vessel.packed && !warpToReal)
                storedThrotle = vessel.ctrlState.mainThrottle;

            // Update ISP
            effectiveIsp = timeDilation * engineIsp;

            UpdateAtmosphericCurve(effectiveIsp);

            if (throttle > 0 && !vessel.packed)
            {
                TimeWarp.GThreshold = GThreshold;

                var thrustRatio = Math.Max(curEngineT.thrustPercentage * 0.01, 0.01);
                var scaledThrottle = Math.Pow(thrustRatio * throttle, ispThrottleExponent);
                effectiveIsp = timeDilation * engineIsp * scaledThrottle;

                UpdateAtmosphericCurve(effectiveIsp);

                fusionRatio = ProcessPowerAndWasteHeat(throttle);

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, (float)(throttle * fusionRatio), -1);

                // Update FuelFlow
                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                calculatedFuelflow = fusionRatio * effectiveMaxThrustInKiloNewton / effectiveIsp / GameConstants.STANDARD_GRAVITY;
                massFlowRateKgPerSecond = thrustRatio * curEngineT.currentThrottle * calculatedFuelflow * 0.001;

                if (!curEngineT.getFlameoutState && fusionRatio < 0.01)
                {
                    curEngineT.status = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_curEngineTstatus1");//"Insufficient Electricity"
                }

                ratioHeadingVersusRequest = 0;
            }
            else if (vessel.packed && curEngineT.currentThrottle > 0 && curEngineT.getIgnitionState && curEngineT.enabled && FlightGlobals.ActiveVessel == vessel && throttle > 0 && percentageFuelRemaining > (100 - fuelLimit) && lightSpeedRatio < speedLimit)
            {
                warpToReal = true; // Set to true for transition to realtime

                fusionRatio = CheatOptions.InfiniteElectricity
                    ? 1
                    : maximizeThrust
                        ? ProcessPowerAndWasteHeat(1)
                        : ProcessPowerAndWasteHeat(storedThrotle);

                if (fusionRatio <= 0.01)
                {
                    var message = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg1");//"Thrust warp stopped - insufficient power"
                    UnityEngine.Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    // Return to realtime
                    TimeWarp.SetRate(0, true);
                }

                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                calculatedFuelflow = effectiveIsp > 0 ? fusionRatio * effectiveMaxThrustInKiloNewton / effectiveIsp / PhysicsGlobals.GravitationalAcceleration : 0;
                massFlowRateKgPerSecond = calculatedFuelflow * 0.001;

                if (TimeWarp.fixedDeltaTime > 20)
                {
                    var deltaCalculations = (float)Math.Ceiling(TimeWarp.fixedDeltaTime * 0.05);
                    var deltaTimeStep = TimeWarp.fixedDeltaTime / deltaCalculations;

                    for (var step = 0; step < deltaCalculations; step++)
                    {
                        PersistentThrust(deltaTimeStep, universalTime + (step * deltaTimeStep), part.transform.up, vessel.totalMass);
                        CalculateTimeDialation();
                    }
                }
                else
                    PersistentThrust(TimeWarp.fixedDeltaTime, universalTime, part.transform.up, vessel.totalMass);

                if (fuelRatio < 0.999)
                {
                    var message = (fuelRatio <= 0) ? Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg2") : Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg3");//"Thrust warp stopped - propellant depleted" : "Thrust warp stopped - running out of propellant"
                    UnityEngine.Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    // Return to realtime
                    TimeWarp.SetRate(0, true);
                }

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, (float)(throttle * fusionRatio), -1);
            }
            else
            {
                ratioHeadingVersusRequest = vessel.PersistHeading(vesselChangedSIOCountdown > 0, ratioHeadingVersusRequest == 1);

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, 0, -1);

                powerUsage = "0.00" + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit") + " / " + EffectiveMaxPowerRequirement.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");

                if (!(percentageFuelRemaining > (100 - fuelLimit) || lightSpeedRatio > speedLimit))
                {
                    warpToReal = false;
                    vessel.ctrlState.mainThrottle = 0;
                }

                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / GameConstants.STANDARD_GRAVITY;
                massFlowRateKgPerSecond = 0;
                fusionRatio = 0;
            }

            curEngineT.maxFuelFlow = Mathf.Max((float)calculatedFuelflow,  1e-10f);
            curEngineT.maxThrust =  Mathf.Max((float)effectiveMaxThrustInKiloNewton, 0.0001f);

            massFlowRateTonPerHour = massFlowRateKgPerSecond * 3.6;
            thrustPowerInTeraWatt = effectiveMaxThrustInKiloNewton * 500 * effectiveIsp * GameConstants.STANDARD_GRAVITY * 1e-12;

            UpdateKerbalismEmitter();
        }

        private void UpdateAtmosphericCurve(double isp)
        {
            var newAtmosphereCurve = new FloatCurve();
            newAtmosphereCurve.Add(0, (float)isp);
            newAtmosphereCurve.Add(maxAtmosphereDensity, 0);
            curEngineT.atmosphereCurve = newAtmosphereCurve;
        }

        private void PersistentThrust(float modifiedFixedDeltaTime, double modifiedUniversalTime, Vector3d thrustVector, double vesselMass)
        {
            ratioHeadingVersusRequest = vessel.PersistHeading(vesselChangedSIOCountdown > 0, ratioHeadingVersusRequest == 1);
            if (ratioHeadingVersusRequest != 1)
            {
                UnityEngine.Debug.Log("[KSPI]: " + "quit persistent heading: " + ratioHeadingVersusRequest);
                return;
            }

            timeDilationMaximumThrust = timeDilation * timeDilation * MaximumThrust * (maximizeThrust ? 1 : storedThrotle);

            var deltaVv = thrustVector.CalculateDeltaVV(vesselMass, modifiedFixedDeltaTime, timeDilationMaximumThrust * fusionRatio, timeDilation * engineIsp, out demandMass);

            double persistentThrustDot = Vector3d.Dot(this.part.transform.up, vessel.obt_velocity);
            if (persistentThrustDot < 0 && (vessel.obt_velocity.magnitude <= deltaVv.magnitude * 2))
            {
                var message = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg4");//"Thrust warp stopped - orbital speed too low"
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                UnityEngine.Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
                return;
            }

            fuelRatio = CollectFuel(demandMass);

            effectiveMaxThrustInKiloNewton = timeDilationMaximumThrust * fuelRatio;

            if (fuelRatio <= 0)
                return;

            vessel.orbit.Perturb(deltaVv * fuelRatio, modifiedUniversalTime);
        }

        private double CollectFuel(double demandMass)
        {
            if (CheatOptions.InfinitePropellant || demandMass <= 0)
                return 1;

            fusionFuelRequestAmount1 = 0.0;
            fusionFuelRequestAmount2 = 0.0;
            fusionFuelRequestAmount3 = 0.0;

            var totalAmount = demandMass / averageDensity;

            double availableRatio = 1;
            if (fuelFactor1 > 0)
            {
                fusionFuelRequestAmount1 = fuelFactor1 * totalAmount;
                availableRatio = Math.Min(part.GetResourceAvailable(fuelResourceDefinition1, ResourceFlowMode.STACK_PRIORITY_SEARCH) / fusionFuelRequestAmount1, availableRatio);
            }
            if (fuelFactor2 > 0)
            {
                fusionFuelRequestAmount2 = fuelFactor2 * totalAmount;
                availableRatio = Math.Min(part.GetResourceAvailable(fuelResourceDefinition2, ResourceFlowMode.STACK_PRIORITY_SEARCH) / fusionFuelRequestAmount2, availableRatio);
            }
            if (fuelFactor3 > 0)
            {
                fusionFuelRequestAmount3 = fuelFactor3 * totalAmount;
                availableRatio = Math.Min(part.GetResourceAvailable(fuelResourceDefinition3, ResourceFlowMode.STACK_PRIORITY_SEARCH) / fusionFuelRequestAmount3, availableRatio);
            }

            if (availableRatio <= float.Epsilon)
                return 0;

            double receivedRatio = 1;
            if (fuelFactor1 > 0)
            {
                var receivedFusionFuel = part.RequestResource(fuelResourceDefinition1.id, fusionFuelRequestAmount1 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount1 > 0 ? receivedFusionFuel / fusionFuelRequestAmount1 : 0);
            }
            if (fuelFactor2 > 0)
            {
                var receivedFusionFuel = part.RequestResource(fuelResourceDefinition2.id, fusionFuelRequestAmount2 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount2 > 0 ? receivedFusionFuel / fusionFuelRequestAmount2 : 0);
            }
            if (fuelFactor3 > 0)
            {
                var receivedFusionFuel = part.RequestResource(fuelResourceDefinition3.id, fusionFuelRequestAmount3 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount3 > 0 ? receivedFusionFuel / fusionFuelRequestAmount3 : 0);
            }
            return receivedRatio;
        }

        private double ProcessPowerAndWasteHeat(float requestedThrottle)
        {
            // Calculate Fusion Ratio
            var effectiveMaxPowerRequirement = EffectiveMaxPowerRequirement;
            var effectiveMaxPowerProduction = EffectiveMaxPowerProduction;
            var effectiveMaxFusionWasteHeat = EffectiveMaxFusionWasteHeat;

            var wasteheatRatio = getResourceBarFraction(ResourceManager.FNRESOURCE_WASTEHEAT);

            var wasteheatModifier = CheatOptions.IgnoreMaxTemperature || wasteheatRatio < 0.9 ? 1 : (1  - wasteheatRatio) * 10;

            var requestedPower = requestedThrottle * effectiveMaxPowerRequirement * wasteheatModifier;

            finalRequestedPower = requestedPower * wasteheatModifier;

            var receivedPower = CheatOptions.InfiniteElectricity || requestedPower <= 0
                ? finalRequestedPower
                : consumeFNResourcePerSecond(finalRequestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);

            var plasmaRatio = !requestedPower.IsInfinityOrNaNorZero() && !receivedPower.IsInfinityOrNaNorZero() ? Math.Min(1, receivedPower / requestedPower) : 0;

            powerUsage = receivedPower.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit") + " / " + requestedPower.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");

            // The Absorbed wasteheat from Fusion production and reaction
            wasteHeat = requestedThrottle * plasmaRatio * effectiveMaxFusionWasteHeat;
            if (!CheatOptions.IgnoreMaxTemperature && effectiveMaxFusionWasteHeat > 0)
                supplyFNResourcePerSecondWithMax(wasteHeat, effectiveMaxFusionWasteHeat, ResourceManager.FNRESOURCE_WASTEHEAT);

            if (!CheatOptions.InfiniteElectricity && effectiveMaxPowerProduction > 0)
                supplyFNResourcePerSecondWithMax(requestedThrottle * plasmaRatio * effectiveMaxPowerProduction, effectiveMaxPowerProduction, ResourceSettings.Config.ElectricPowerInMegawatt);

            return plasmaRatio;
        }

        private void KillKerbalsWithRadiation(float radiationRatio)
        {
            if (!radhazard || radiationRatio <= 0 || rad_safety_features) return;

            var vesselsToRemove = new List<Vessel>();
            var crewToRemove = new List<ProtoCrewMember>();

            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);

                if (distance >= leathalDistance || currentVessel == vessel || currentVessel.GetCrewCount() <= 0) continue;

                var invSqDist = distance / killDivider;
                var invSqMult = 1 / invSqDist / invSqDist;

                foreach (var crewMember in currentVessel.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1 - TimeWarp.fixedDeltaTime * invSqMult)) continue;

                    if (!currentVessel.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg5", crewMember.name), 5f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Radiation!"
                        crewToRemove.Add(crewMember);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg5", crewMember.name), 5f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Radiation!"
                        vesselsToRemove.Add(currentVessel);
                    }
                }
            }

            foreach (var currentVessel in vesselsToRemove)
            {
                currentVessel.rootPart.Die();
            }

            foreach (var crewMember in crewToRemove)
            {
                var currentVessel = FlightGlobals.Vessels.Find(p => p.GetVesselCrew().Contains(crewMember));
                var partWithCrewMember = currentVessel.Parts.Find(p => p.protoModuleCrew.Contains(crewMember));
                partWithCrewMember.RemoveCrewmember(crewMember);
                crewMember.Die();
            }
        }

        public override int getPowerPriority()
        {
            // when providing surplus power, we want to be one of the first to consume and therefore provide power
            return PowerProduction > PowerRequirement ? 1 : powerPriority;
        }

        public override int getSupplyPriority()
        {
            return 1;
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();
            DetermineTechLevel();

            if (!string.IsNullOrEmpty(upgradeTechReq1))
            {
                sb.Append("<color=#7fdfffff>").Append(Localizer.Format("#LOC_KSPIE_Generic_upgradeTechnologies")).AppendLine(":</color><size=10>");
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq1)));
                if (!string.IsNullOrEmpty(upgradeTechReq2))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq2)));
                if (!string.IsNullOrEmpty(upgradeTechReq3))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq3)));
                if (!string.IsNullOrEmpty(upgradeTechReq4))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq4)));
                if (!string.IsNullOrEmpty(upgradeTechReq5))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq5)));
                if (!string.IsNullOrEmpty(upgradeTechReq6))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq6)));
                if (!string.IsNullOrEmpty(upgradeTechReq7))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq7)));
                if (!string.IsNullOrEmpty(upgradeTechReq8))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq8)));
                sb.AppendLine("</size>");
            }

            sb.Append("<color=#7fdfffff>").Append(Localizer.Format("#LOC_KSPIE_Generic_EnginePerformance")).AppendLine(":</color><size=10>");
            sb.AppendLine(FormatThrustStatistics(maxThrustMk1, thrustIspMk1));
            if (!string.IsNullOrEmpty(upgradeTechReq1))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk2, thrustIspMk2));
            if (!string.IsNullOrEmpty(upgradeTechReq2))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk3, thrustIspMk3));
            if (!string.IsNullOrEmpty(upgradeTechReq3))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk4, thrustIspMk4));
            if (!string.IsNullOrEmpty(upgradeTechReq4))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk5, thrustIspMk5));
            if (!string.IsNullOrEmpty(upgradeTechReq5))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk6, thrustIspMk6));
            if (!string.IsNullOrEmpty(upgradeTechReq6))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk7, thrustIspMk7));
            if (!string.IsNullOrEmpty(upgradeTechReq7))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk8, thrustIspMk8));
            if (!string.IsNullOrEmpty(upgradeTechReq8))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk9, thrustIspMk9));
            sb.AppendLine("</size>");

            sb.Append("<color=#7fdfffff>").Append(Localizer.Format("#LOC_KSPIE_Generic_PowerRequirementAndWasteheat")).AppendLine(":</color><size=10>");
            sb.AppendLine(FormatPowerStatistics(powerRequirementMk1, wasteheatMk1));
            if (!string.IsNullOrEmpty(upgradeTechReq1))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk2, wasteheatMk2));
            if (!string.IsNullOrEmpty(upgradeTechReq2))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk3, wasteheatMk3));
            if (!string.IsNullOrEmpty(upgradeTechReq3))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk4, wasteheatMk4));
            if (!string.IsNullOrEmpty(upgradeTechReq4))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk5, wasteheatMk5));
            if (!string.IsNullOrEmpty(upgradeTechReq5))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk6, wasteheatMk6));
            if (!string.IsNullOrEmpty(upgradeTechReq6))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk7, wasteheatMk7));
            if (!string.IsNullOrEmpty(upgradeTechReq7))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk8, wasteheatMk8));
            if (!string.IsNullOrEmpty(upgradeTechReq8))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk9, wasteheatMk9));
            sb.Append("</size>");

            return sb.ToStringAndRelease();
        }
    }
}
