using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.External;
using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using KSP.Localization;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    [KSPModule("Chemical Engine")]
    class ChemicalEngineController : InterstellarEngineController
    {
        public override string GetModuleDisplayName() => "Chemical Engine";

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            EnableField(nameof(mixedRatioPercentage));
            EnableField(nameof(fuelMassRatioStr));
            EnableField(nameof(fuelVolumeRatioStr));

            DisableField(nameof(speedLimit));
            DisableField(nameof(fuelLimit));
            DisableField(nameof(maximizeThrust));
            DisableField(nameof(mhdPowerGenerationPercentage));
            DisableField(nameof(engineSpeedOfLight));
            DisableField(nameof(lightSpeedRatio));
            DisableField(nameof(relativity));
            DisableField(nameof(timeDilation));
            DisableField(nameof(worldSpaceVelocity));
        }
    }

    [KSPModule("Fission Engine")]
    class FissionEngineController : InterstellarEngineController
    {
        public override string GetModuleDisplayName() => "Fission Engine";
    }

    [KSPModule("Confinement Fusion Engine")]
    class FusionEngineController : InterstellarEngineController
    {
        public override string GetModuleDisplayName() => "Confinement Fusion Engine";
    }

    [KSPModule("Daedalus Fusion Engine")]
    class DaedalusEngineController : InterstellarEngineController
    {
        public override string GetModuleDisplayName() => "Daedalus Fusion Engine";
    }

    [KSPModule("Interstellar Engine")]
    class InterstellarEngineController : ResourceSuppliableModule, IUpgradeableModule, IRescalable<InterstellarEngineController>
    {
        const string LightBlue = "<color=#7fdfffff>";

        // Persistent
        [KSPField(isPersistant = true)] public double thrustMultiplier = 1;
        [KSPField(isPersistant = true)] public double ispMultiplier = 1;
        [KSPField(isPersistant = true)] public bool IsEnabled;
        [KSPField(isPersistant = true)] public bool rad_safety_features = true;
        [KSPField(isPersistant = true)] public bool isDeployed;

        // Controllable settings
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_speedLimit", guiUnits = "c"),
         UI_FloatRange(stepIncrement = 0.005f, maxValue = 1, minValue = 0.005f)]
        public float speedLimit = 1;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_fuelLimit", guiUnits = "%"),
         UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        public float fuelLimit = 100;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Mix Ratio", guiUnits = "%"),
         UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1)]
        public float mixedRatioPercentage = 50;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "MHD Power %")
         , UI_FloatRange(stepIncrement = 1f, maxValue = 200, minValue = 0, affectSymCounterparts = UI_Scene.All)]
        public float mhdPowerGenerationPercentage = 101;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_maximizeThrust"),
         UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool maximizeThrust = true;

        // Non Persistent fields
        [KSPField] public int powerPriority = 3;
        [KSPField] public int numberOfAvailableUpgradeTechs;

        [KSPField] public string deployAnimName = "";
        [KSPField] public float deployAnimSpeed = 1;
        [KSPField] public bool canDeployOnSurface = true;
        [KSPField] public bool canDeployInAtmosphere = true;

        [KSPField] public double massThrustExp = 0;
        [KSPField] public double massIspExp = 0;
        [KSPField] public double higherScaleThrustExponent = 3;
        [KSPField] public double lowerScaleThrustExponent = 4;
        [KSPField] public double higherScaleIspExponent = 0.25;
        [KSPField] public double lowerScaleIspExponent = 1;
        [KSPField] public double GThreshold = 15;

        [KSPField] public string mhdPowerProductionResourceName = "_FusionPelletsMhdEcPower";
        [KSPField] public string effectName = string.Empty;

        [KSPField] public string fuelName1 = "FusionPellets";
        [KSPField] public string fuelName2 = string.Empty;
        [KSPField] public string fuelName3 = string.Empty;

        [KSPField] public double fuelRatio1 = 1;
        [KSPField] public double fuelRatio2 = 0;
        [KSPField] public double fuelRatio3 = 0;

        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk1;
        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk2;
        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk3;
        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk4;
        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk5;
        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk6;
        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk7;
        [KSPField(guiName = "#LOC_KSPIE_DeadalusEngineController_UpgradeTech")] public string translatedTechMk8;

        [KSPField] public float maxThrustMk1 = 300;
        [KSPField] public float maxThrustMk2 = 500;
        [KSPField] public float maxThrustMk3 = 800;
        [KSPField] public float maxThrustMk4 = 1200;
        [KSPField] public float maxThrustMk5 = 1500;
        [KSPField] public float maxThrustMk6 = 2000;
        [KSPField] public float maxThrustMk7 = 2500;
        [KSPField] public float maxThrustMk8 = 3000;
        [KSPField] public float maxThrustMk9 = 3500;

        [KSPField] public float minMixedRatioPercentageMk1 = 0;
        [KSPField] public float minMixedRatioPercentageMk2 = 0;
        [KSPField] public float minMixedRatioPercentageMk3 = 0;
        [KSPField] public float minMixedRatioPercentageMk4 = 0;
        [KSPField] public float minMixedRatioPercentageMk5 = 0;
        [KSPField] public float minMixedRatioPercentageMk6 = 0;
        [KSPField] public float minMixedRatioPercentageMk7 = 0;
        [KSPField] public float minMixedRatioPercentageMk8 = 0;
        [KSPField] public float minMixedRatioPercentageMk9 = 0;

        [KSPField] public float wasteheatMk1 = 0;
        [KSPField] public float wasteheatMk2 = 0;
        [KSPField] public float wasteheatMk3 = 0;
        [KSPField] public float wasteheatMk4 = 0;
        [KSPField] public float wasteheatMk5 = 0;
        [KSPField] public float wasteheatMk6 = 0;
        [KSPField] public float wasteheatMk7 = 0;
        [KSPField] public float wasteheatMk8 = 0;
        [KSPField] public float wasteheatMk9 = 0;

        [KSPField] public double powerRequirementMk1 = 0;
        [KSPField] public double powerRequirementMk2 = 0;
        [KSPField] public double powerRequirementMk3 = 0;
        [KSPField] public double powerRequirementMk4 = 0;
        [KSPField] public double powerRequirementMk5 = 0;
        [KSPField] public double powerRequirementMk6 = 0;
        [KSPField] public double powerRequirementMk7 = 0;
        [KSPField] public double powerRequirementMk8 = 0;
        [KSPField] public double powerRequirementMk9 = 0;

        [KSPField] public double powerProductionMk1 = 0;
        [KSPField] public double powerProductionMk2 = 0;
        [KSPField] public double powerProductionMk3 = 0;
        [KSPField] public double powerProductionMk4 = 0;
        [KSPField] public double powerProductionMk5 = 0;
        [KSPField] public double powerProductionMk6 = 0;
        [KSPField] public double powerProductionMk7 = 0;
        [KSPField] public double powerProductionMk8 = 0;
        [KSPField] public double powerProductionMk9 = 0;

        [KSPField] public double thrustIspMk1 = 83886;
        [KSPField] public double thrustIspMk2 = 104857;
        [KSPField] public double thrustIspMk3 = 131072;
        [KSPField] public double thrustIspMk4 = 163840;
        [KSPField] public double thrustIspMk5 = 204800;
        [KSPField] public double thrustIspMk6 = 256000;
        [KSPField] public double thrustIspMk7 = 320000;
        [KSPField] public double thrustIspMk8 = 400000;
        [KSPField] public double thrustIspMk9 = 500000;

        [KSPField] public double propellant2Isp = 0;
        [KSPField] public double propellant3Isp = 0;

        [KSPField] public float throttle;
        [KSPField] public float maxAtmosphereDensity = 0;
        [KSPField] public float lethalDistance = 2000;
        [KSPField] public float killDivider = 50;
        [KSPField] public float wasteHeatMultiplier = 1;
        [KSPField] public float powerRequirementMultiplier = 1;
        [KSPField] public float maxTemp = 3200;

        [KSPField] public double demandMass;
        [KSPField] public double fuelRatio;
        [KSPField] public double averageDensity;
        [KSPField] public double ratioHeadingVersusRequest;
        [KSPField] public double ispThrottleExponent = 0.5;
        [KSPField] public double fuelNeutronsFraction = 0.005;

        [KSPField] public string upgradeTechReq1 = null;
        [KSPField] public string upgradeTechReq2 = null;
        [KSPField] public string upgradeTechReq3 = null;
        [KSPField] public string upgradeTechReq4 = null;
        [KSPField] public string upgradeTechReq5 = null;
        [KSPField] public string upgradeTechReq6 = null;
        [KSPField] public string upgradeTechReq7 = null;
        [KSPField] public string upgradeTechReq8 = null;

        [KSPField] public double fuelFactor1;
        [KSPField] public double fuelFactor2;
        [KSPField] public double fuelFactor3;

        [KSPField] public double fusionFuelRequestAmount1;
        [KSPField] public double fusionFuelRequestAmount2;
        [KSPField] public double fusionFuelRequestAmount3;

        // Visible fields
        [KSPField(guiActive = false, guiFormat = "F3", guiName = "Mass Ratio")] public string fuelMassRatioStr;
        [KSPField(guiActive = false, guiFormat = "F3", guiName = "Volume Ratio")] public string fuelVolumeRatioStr;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_radhazardstr")] public string radHazardStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsRatio")] public string fuelAmountsRatio1;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsRatio")] public string fuelAmountsRatio2;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsRatio")] public string fuelAmountsRatio3;

        [KSPField(guiActive = true, guiActiveEditor = false, guiFormat = "F3", guiName = "Max Effective Thrust", guiUnits = "#autoLOC_7001408")] public double maxEffectiveThrust;
        [KSPField(guiActive = true, guiActiveEditor = false, guiFormat = "F5", guiName = "Max Effective Flow", guiUnits = "#autoLOC_7001409")] public double maxEffectiveFlow;
        [KSPField(guiActive = true, guiActiveEditor = true, guiFormat = "F2", guiName = "Max Effective Isp", guiUnits = "#autoLOC_7001400")] public double maxEffectiveIsp;

        [KSPField(guiActive = true, guiActiveEditor = true, guiFormat = "F2", guiName = "#LOC_KSPIE_FusionEngine_powerUsage")] public string powerUsage;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_speedOfLight", guiFormat = "F0", guiUnits = " m/s")] public double engineSpeedOfLight;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")] public double lightSpeedRatio;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiFormat = "F3", guiUnits = " t")] public float partMass = 1;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fusionRatio", guiFormat = "F3")] public double fusionRatio;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_relativity", guiFormat = "F10")] public double relativity;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")] public double timeDilation = 1;

        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_thrustPowerInTeraWatt", guiFormat = "F2", guiUnits = " TW")] public double thrustPowerInTerraWatt;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateKgPerSecond", guiFormat = "F6", guiUnits = " kg/s")] public double massFlowRateKgPerSecond;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateTonPerHour", guiFormat = "F6", guiUnits = " t/h")] public double massFlowRateTonPerHour;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_worldSpaceVelocity", guiFormat = "F2", guiUnits = " m/s")] public double worldSpaceVelocity;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveMaxThrustInKiloNewton", guiFormat = "F2", guiUnits = " kN")]
        public double effectiveMaxThrustInKiloNewton;
        [KSPField(guiActive = true, guiActiveEditor = true, guiFormat = "F2", guiName = "#LOC_KSPIE_FusionEngine_wasteHeat", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double wasteHeat;

        private FNEmitterController _emitterController;
        private ModuleEngines _curEngineT;
        private BaseEvent _deactivateRadSafetyEvent;
        private BaseEvent _activateRadSafetyEvent;
        private BaseField _radHazardStrField;
        private FloatCurve _factoryFloatCurve;
        private Animation _deployAnimation;

        private BaseField _fuelAmountsRatioField1;
        private BaseField _fuelAmountsRatioField2;
        private BaseField _fuelAmountsRatioField3;

        private PartResourceDefinition _fuelResourceDefinition1;
        private PartResourceDefinition _fuelResourceDefinition2;
        private PartResourceDefinition _fuelResourceDefinition3;

        public float storedThrottle;
        private bool _radHazard;
        private bool _warpToReal;
        private double _compositeIsp;
        private double _compositeWasteheatMult;
        private double _compositeThrustMult;
        private double _engineIsp;
        private double _universalTime;
        private double _percentageFuelRemaining1;
        private double _percentageFuelRemaining2;
        private double _percentageFuelRemaining3;
        private double _calculatedFuelflow;
        private int _vesselChangedSioCountdown;
        private int _engineGenerationType;

        public GenerationType EngineGenerationType
        {
            get => (GenerationType) _engineGenerationType;
            private set => _engineGenerationType = (int) value;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_DeadalusEngineController_DeactivateRadSafety", active = true)]//Disable Radiation Safety
        public void DeactivateRadSafety()
        {
            rad_safety_features = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_DeadalusEngineController_ActivateRadSafety", active = false)]//Activate Radiation Safety
        public void ActivateRadSafety()
        {
            rad_safety_features = true;
        }

        public void VesselChangedSoi()
        {
            _vesselChangedSioCountdown = 10;
        }

        #region IUpgradeableModule

        public string UpgradeTechnology => upgradeTechReq1;

        private float RawMaximumThrust
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return maxThrustMk1;
                    case (int)GenerationType.Mk2: return maxThrustMk2;
                    case (int)GenerationType.Mk3: return maxThrustMk3;
                    case (int)GenerationType.Mk4: return maxThrustMk4;
                    case (int)GenerationType.Mk5: return maxThrustMk5;
                    case (int)GenerationType.Mk6: return maxThrustMk6;
                    case (int)GenerationType.Mk7: return maxThrustMk7;
                    case (int)GenerationType.Mk8: return maxThrustMk8;
                    default: return maxThrustMk9;
                }
            }
        }

        private float MinMixedRatioPercentage
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return minMixedRatioPercentageMk1;
                    case (int)GenerationType.Mk2: return minMixedRatioPercentageMk2;
                    case (int)GenerationType.Mk3: return minMixedRatioPercentageMk3;
                    case (int)GenerationType.Mk4: return minMixedRatioPercentageMk4;
                    case (int)GenerationType.Mk5: return minMixedRatioPercentageMk5;
                    case (int)GenerationType.Mk6: return minMixedRatioPercentageMk6;
                    case (int)GenerationType.Mk7: return minMixedRatioPercentageMk7;
                    case (int)GenerationType.Mk8: return minMixedRatioPercentageMk8;
                    default: return minMixedRatioPercentageMk9;
                }
            }
        }

        private double MaximumThrust => RawMaximumThrust * _compositeThrustMult * thrustMultiplier * Math.Pow(part.mass / partMass, massThrustExp);

        private float FusionWasteHeat
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return wasteheatMk1;
                    case (int)GenerationType.Mk2: return wasteheatMk2;
                    case (int)GenerationType.Mk3: return wasteheatMk3;
                    case (int)GenerationType.Mk4: return wasteheatMk4;
                    case (int)GenerationType.Mk5: return wasteheatMk5;
                    case (int)GenerationType.Mk6: return wasteheatMk6;
                    case (int)GenerationType.Mk7: return wasteheatMk7;
                    case (int)GenerationType.Mk8: return wasteheatMk8;
                    default: return maxThrustMk9;
                }
            }
        }

        public double PowerRequirement
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return powerRequirementMk1;
                    case (int)GenerationType.Mk2: return powerRequirementMk2;
                    case (int)GenerationType.Mk3: return powerRequirementMk3;
                    case (int)GenerationType.Mk4: return powerRequirementMk4;
                    case (int)GenerationType.Mk5: return powerRequirementMk5;
                    case (int)GenerationType.Mk6: return powerRequirementMk6;
                    case (int)GenerationType.Mk7: return powerRequirementMk7;
                    case (int)GenerationType.Mk8: return powerRequirementMk8;
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
                    case (int)GenerationType.Mk1: return powerProductionMk1;
                    case (int)GenerationType.Mk2: return powerProductionMk2;
                    case (int)GenerationType.Mk3: return powerProductionMk3;
                    case (int)GenerationType.Mk4: return powerProductionMk4;
                    case (int)GenerationType.Mk5: return powerProductionMk5;
                    case (int)GenerationType.Mk6: return powerProductionMk6;
                    case (int)GenerationType.Mk7: return powerProductionMk7;
                    case (int)GenerationType.Mk8: return powerProductionMk8;
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
                    case (int)GenerationType.Mk1: return thrustIspMk1;
                    case (int)GenerationType.Mk2: return thrustIspMk2;
                    case (int)GenerationType.Mk3: return thrustIspMk3;
                    case (int)GenerationType.Mk4: return thrustIspMk4;
                    case (int)GenerationType.Mk5: return thrustIspMk5;
                    case (int)GenerationType.Mk6: return thrustIspMk6;
                    case (int)GenerationType.Mk7: return thrustIspMk7;
                    case (int)GenerationType.Mk8: return thrustIspMk8;
                    default:
                        return thrustIspMk9;
                }
            }
        }

        public double EngineIsp => RawEngineIsp * ispMultiplier * Math.Pow(part.mass / partMass, massIspExp);

        private double EffectiveMaxPowerRequirement => PowerRequirement * powerRequirementMultiplier;

        private double EffectiveMaxPowerProduction => PowerProduction * powerRequirementMultiplier;

        private double EffectiveMaxFusionWasteHeat => FusionWasteHeat * _compositeWasteheatMult * wasteHeatMultiplier;


        public void upgradePartModule() {}

        #endregion

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var displayName = GetModuleDisplayName();
            var className = GetType().Name;

            foreach (var field in Fields)
            {
                field.Attribute.groupName = className;
                field.Attribute.groupDisplayName = displayName;
            }

            foreach (var field in Events)
            {
                field.group.name = className;
                field.group.displayName = displayName;
            }

            var moduleEngines = part.FindModuleImplementing<ModuleEngines>();
            if (moduleEngines != null)
            {
                foreach (var field in moduleEngines.Fields)
                {
                    field.Attribute.groupName = className;
                    field.Attribute.groupDisplayName = displayName;
                }

                foreach (var field in moduleEngines.Events)
                {
                    field.group.name = className;
                    field.group.displayName = displayName;
                }
            }
            else
                Debug.LogWarning("[KSPI]: ThermalNozzleController - failed to find engine during load for " + part.name);
        }

        public override void OnStart(StartState state)
        {
            resourcesToSupply = new[] { ResourceSettings.Config.WasteHeatInMegawatt, ResourceSettings.Config.ElectricPowerInMegawatt };

            base.OnStart(state);

            InitializeDeployAnimation();

            _curEngineT = part.FindModuleImplementing<ModuleEngines>();
            if (_curEngineT == null) return;

            _factoryFloatCurve = _curEngineT.atmosphereCurve;

            _curEngineT.Fields[nameof(ModuleEngines.finalThrust)].guiActive = true;
            _curEngineT.Fields[nameof(ModuleEngines.fuelFlowGui)].guiActive = true;
            _curEngineT.Fields[nameof(ModuleEngines.realIsp)].guiActive = true;

            _fuelAmountsRatioField1 = Fields[nameof(fuelAmountsRatio1)];
            _fuelAmountsRatioField2 = Fields[nameof(fuelAmountsRatio2)];
            _fuelAmountsRatioField3 = Fields[nameof(fuelAmountsRatio3)];

            engineSpeedOfLight = PluginSettings.Config.SpeedOfLight;

            UpdateFuelFactors();
            DetermineTechLevel();

            part.maxTemp = maxTemp;
            part.thermalMass = 1;
            part.thermalMassModifier = 1;

            // for initial vacuumIsp use max vacuumIsp
            _compositeWasteheatMult = 1;
            _compositeThrustMult = 1;
            _engineIsp = EngineIsp;

            // bind with fields and events
            _deactivateRadSafetyEvent = Events[nameof(DeactivateRadSafety)];
            _activateRadSafetyEvent = Events[nameof(ActivateRadSafety)];
            _radHazardStrField = Fields[nameof(radHazardStr)];

            translatedTechMk1 = PluginHelper.DisplayTech(upgradeTechReq1);
            translatedTechMk2 = PluginHelper.DisplayTech(upgradeTechReq2);
            translatedTechMk3 = PluginHelper.DisplayTech(upgradeTechReq3);
            translatedTechMk4 = PluginHelper.DisplayTech(upgradeTechReq4);
            translatedTechMk5 = PluginHelper.DisplayTech(upgradeTechReq5);
            translatedTechMk6 = PluginHelper.DisplayTech(upgradeTechReq6);
            translatedTechMk7 = PluginHelper.DisplayTech(upgradeTechReq7);
            translatedTechMk8 = PluginHelper.DisplayTech(upgradeTechReq8);

            ConfigureMixedRatioPercentage();

            InitializeKerbalismEmitter();
        }

        private void UpdateButtons()
        {
            if (string.IsNullOrEmpty(deployAnimName))
                return;

            var deployEvent = Events[nameof(Deploy)];
            deployEvent.guiActiveEditor = !isDeployed;
            deployEvent.guiActive = !isDeployed;

            var retractEvent = Events[nameof(Retract)];
            retractEvent.guiActiveEditor = isDeployed;
            retractEvent.guiActive = isDeployed;
        }

        private void InitializeDeployAnimation()
        {
            if (!string.IsNullOrEmpty(deployAnimName))
            {
                _deployAnimation = part.FindModelAnimators(deployAnimName).First();

                if (_deployAnimation == null)
                    return;

                _deployAnimation[deployAnimName].speed = isDeployed ? 1 : -1;
                _deployAnimation[deployAnimName].normalizedTime = isDeployed ? 1 : 0;
                _deployAnimation.Blend(deployAnimName);
            }
            else
                isDeployed = true;
        }

        private void ConfigureMixedRatioPercentage()
        {
            var mixedRatioPercentageField = Fields[nameof(mixedRatioPercentage)];
            if (mixedRatioPercentageField.uiControlEditor is UI_FloatRange mixedRatioPercentageEditor)
                mixedRatioPercentageEditor.minValue = MinMixedRatioPercentage;
            if (mixedRatioPercentageField.uiControlFlight is UI_FloatRange mixedRatioPercentageFlight)
                mixedRatioPercentageFlight.minValue = MinMixedRatioPercentage;
        }

        private void InitializeKerbalismEmitter()
        {
            if (!Kerbalism.IsLoaded)
                return;

            _emitterController = part.FindModuleImplementing<FNEmitterController>();

            if (_emitterController == null)
                Debug.LogWarning("[KSPI]: No Emitter Found om " + part.partInfo.title);
        }

        private void UpdateKerbalismEmitter()
        {
            if (_emitterController == null)
                return;

            _emitterController.reactorActivityFraction = fusionRatio;
            _emitterController.exhaustActivityFraction = fusionRatio;
            _emitterController.fuelNeutronsFraction = fuelNeutronsFraction;
        }

        private void UpdateFuelFactors()
        {
            if (!string.IsNullOrEmpty(fuelName1))
                _fuelResourceDefinition1 = PartResourceLibrary.Instance.GetDefinition(fuelName1);
            if (!string.IsNullOrEmpty(fuelName2))
                _fuelResourceDefinition2 = PartResourceLibrary.Instance.GetDefinition(fuelName2);
            if (!string.IsNullOrEmpty(fuelName3))
                _fuelResourceDefinition3 = PartResourceLibrary.Instance.GetDefinition(fuelName3);

            var ratioSum = 0.0;
            var densitySum = 0.0;

            if (_fuelResourceDefinition1 != null)
            {
                ratioSum += fuelRatio1;
                densitySum += _fuelResourceDefinition1.density * fuelRatio1;
            }
            if (_fuelResourceDefinition2 != null)
            {
                ratioSum += fuelRatio2;
                densitySum += _fuelResourceDefinition2.density * fuelRatio2;
            }
            if (_fuelResourceDefinition3 != null)
            {
                ratioSum += fuelRatio3;
                densitySum += _fuelResourceDefinition3.density * fuelRatio3;
            }

            averageDensity = densitySum / ratioSum;

            fuelFactor1 = _fuelResourceDefinition1 != null ? fuelRatio1/ratioSum : 0;
            fuelFactor2 = _fuelResourceDefinition2 != null ? fuelRatio2/ratioSum : 0;
            fuelFactor3 = _fuelResourceDefinition3 != null ? fuelRatio3/ratioSum : 0;
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
            UpdateButtons();

            if (_curEngineT != null && Fields[nameof(mixedRatioPercentage)].guiActive)
            {
                fuelMassRatioStr = "";
                fuelVolumeRatioStr = "";
                double totalMass = 0;
                double volumeRatioMult = 0;
                double massRatioMult = 0;
                double rawEngineIsp = RawEngineIsp;

                for (var i = 0; i < _curEngineT.propellants.Count; i++)
                {
                    var currentPropellant = _curEngineT.propellants[i];

                    switch (i)
                    {
                        case 0:
                            currentPropellant.ratio = (float) fuelRatio1;
                            volumeRatioMult = 1 / fuelRatio1;
                            fuelVolumeRatioStr += $"{volumeRatioMult * fuelRatio1:F2}";
                            break;
                        case 1:
                            currentPropellant.ratio = (float) (fuelRatio2 * Math.Max(1e-10, mixedRatioPercentage / 100));
                            fuelVolumeRatioStr += $" : {volumeRatioMult * currentPropellant.ratio:F2}";
                            break;
                        case 2:
                            currentPropellant.ratio = (float) (fuelRatio3 * Math.Max(1e-10, mixedRatioPercentage / 100));
                            fuelVolumeRatioStr += $" : {volumeRatioMult * currentPropellant.ratio:F2}";
                            break;
                    }

                    totalMass += currentPropellant.ratio * currentPropellant.resourceDef.density;
                }

                _compositeIsp = 0;
                for (var i = 0; i < _curEngineT.propellants.Count; i++)
                {
                    var currentPropellant = _curEngineT.propellants[i];

                    switch (i)
                    {
                        case 0:
                            var massRatio0 = currentPropellant.ratio * currentPropellant.resourceDef.density / totalMass;
                            _compositeIsp += massRatio0 * rawEngineIsp * rawEngineIsp;
                            massRatioMult = 1 / massRatio0;
                            fuelMassRatioStr += $"{massRatioMult * massRatio0:F2}";
                            break;
                        case 1:
                            var massRatio1 = currentPropellant.ratio * currentPropellant.resourceDef.density / totalMass;
                            _compositeIsp += massRatio1 * propellant2Isp * propellant2Isp;
                            fuelMassRatioStr += $" : {massRatioMult * massRatio1:F2}";
                            break;
                        case 2:

                            var massRatio2 = currentPropellant.ratio * currentPropellant.resourceDef.density / totalMass;
                            _compositeIsp += massRatio2 * propellant3Isp * propellant3Isp;
                            fuelMassRatioStr += $" : {massRatioMult * massRatio2:F2}";
                            break;
                    }
                }

                _compositeWasteheatMult = Math.Pow(_compositeIsp / (rawEngineIsp * rawEngineIsp), 2);
                _engineIsp = Math.Sqrt(_compositeIsp);
                _compositeThrustMult = rawEngineIsp / _engineIsp;
            }
            else
            {
                _compositeWasteheatMult = 1;
                _engineIsp = EngineIsp;
                _compositeThrustMult = 1;
            }

            // Update ISP
            maxEffectiveIsp = timeDilation * _engineIsp;

            // Update Max Thrust
            effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;

            var wasteheatPartResource = part.Resources[ResourceSettings.Config.WasteHeatInMegawatt];
            if (wasteheatPartResource != null)
            {
                var localWasteheatRatio = wasteheatPartResource.amount / wasteheatPartResource.maxAmount;
                wasteheatPartResource.maxAmount = 1000 * partMass * wasteHeatMultiplier;
                wasteheatPartResource.amount = wasteheatPartResource.maxAmount * localWasteheatRatio;
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                // configure engine for Kerbal Engineering support
                UpdateAtmosphericCurve(EngineIsp);
                effectiveMaxThrustInKiloNewton = MaximumThrust;
                _calculatedFuelflow = effectiveMaxThrustInKiloNewton / EngineIsp / PhysicsGlobals.GravitationalAcceleration;
                _curEngineT.maxFuelFlow = (float)_calculatedFuelflow;
                _curEngineT.maxThrust = (float)effectiveMaxThrustInKiloNewton;
                powerUsage = EffectiveMaxPowerRequirement.ToString("0.00") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");
                wasteHeat = EffectiveMaxFusionWasteHeat;
            }
            else
            {
                part.GetConnectedResourceTotals(_fuelResourceDefinition1.id, out double fuelAmounts1, out double fuelAmountsMax1);
                _percentageFuelRemaining1 = fuelAmountsMax1 > 0 ? fuelAmounts1 / fuelAmountsMax1 * 100 : 0;
                fuelAmountsRatio1 = _percentageFuelRemaining1.ToString("0.000") + "% ";

                if (_fuelResourceDefinition2 != null)
                {
                    _fuelAmountsRatioField2.guiActive = true;
                    part.GetConnectedResourceTotals(_fuelResourceDefinition2.id, out double fuelAmounts2, out double fuelAmountsMax2);
                    _percentageFuelRemaining2 = fuelAmountsMax2 > 0 ? fuelAmounts2 / fuelAmountsMax2 * 100 : 0;
                    fuelAmountsRatio2 = _percentageFuelRemaining2.ToString("0.000") + "% ";
                }
                else
                    _fuelAmountsRatioField2.guiActive = false;

                if (_fuelResourceDefinition3 != null)
                {
                    _fuelAmountsRatioField3.guiActive = true;
                    part.GetConnectedResourceTotals(_fuelResourceDefinition3.id, out double fuelAmounts3, out double fuelAmountsMax3);
                    _percentageFuelRemaining3 = fuelAmountsMax3 > 0 ? fuelAmounts3 / fuelAmountsMax3 * 100 : 0;
                    fuelAmountsRatio3 = _percentageFuelRemaining3.ToString("0.000") + "% ";
                }
                else
                    _fuelAmountsRatioField3.guiActive = true;
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
            var result = (powerRequirement > 0 ? (powerRequirement * powerRequirementMultiplier).ToString(format) + " MWe / " : "") + wasteheat.ToString(format) + " MJ";

            if (string.IsNullOrEmpty(color))
                return result;

            return "<color=" + color + ">" + result + "</color>";
        }

        // Note: we assume OnRescale is called at load and after any time tweakscale changes the size of an part
        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            Debug.Log("[KSPI]: InterstellarEngineController OnRescale was called with factor " + factor.absolute.linear);

            var storedAbsoluteFactor = (double)(decimal)factor.absolute.linear;

            thrustMultiplier = storedAbsoluteFactor >= 1 ? Math.Pow(storedAbsoluteFactor, higherScaleThrustExponent) : Math.Pow(storedAbsoluteFactor, lowerScaleThrustExponent);
            ispMultiplier = storedAbsoluteFactor >= 1 ? Math.Pow(storedAbsoluteFactor, higherScaleIspExponent) : Math.Pow(storedAbsoluteFactor, lowerScaleIspExponent);
        }

        public override void OnUpdate()
        {
            // stop engines and drop out of timewarp when X pressed
            if (vessel.packed && storedThrottle > 0 && Input.GetKeyDown(KeyCode.X))
            {
                // Return to realtime
                TimeWarp.SetRate(0, true);

                storedThrottle = 0;
                vessel.ctrlState.mainThrottle = storedThrottle;
            }

            if (_curEngineT == null) return;

            // When transitioning from timewarp to real update radiationRatio
            if (_warpToReal)
            {
                vessel.ctrlState.mainThrottle = storedThrottle;
                _warpToReal = false;
            }

            _deactivateRadSafetyEvent.active = rad_safety_features;
            _activateRadSafetyEvent.active = !rad_safety_features;

            if (_curEngineT.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                Debug.Log("[KSPI]: DeadalusEngineController on " + part.name + " was Force Activated");
                part.force_activate();
            }

            var kerbalHazardCount = 0;
            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);
                if (distance < lethalDistance && currentVessel != this.vessel)
                    kerbalHazardCount += currentVessel.GetCrewCount();
            }

            if (kerbalHazardCount > 0)
            {
                _radHazard = true;
                radHazardStr = Localizer.Format(kerbalHazardCount > 1
                    ? "#LOC_KSPIE_DeadalusEngineController_radhazardstr2"
                    : "#LOC_KSPIE_DeadalusEngineController_radhazardstr1", kerbalHazardCount);

                _radHazardStrField.guiActive = true;
            }
            else
            {
                _radHazardStrField.guiActive = false;
                _radHazard = false;
                radHazardStr = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_radhazardstr3");//"None."
            }

            Fields[nameof(powerUsage)].guiActive = EffectiveMaxPowerRequirement > 0;
            Fields[nameof(wasteHeat)].guiActive = EffectiveMaxFusionWasteHeat > 0;
        }

        private void ShutDown(string reason)
        {
            _curEngineT.Events[nameof(ModuleEnginesFX.Shutdown)].Invoke();
            _curEngineT.currentThrottle = 0;
            _curEngineT.requestedThrottle = 0;

            ScreenMessages.PostScreenMessage(reason, 5.0f, ScreenMessageStyle.UPPER_CENTER);
            foreach (var fxGroup in part.fxGroups)
            {
                fxGroup.setActive(false);
            }
        }

        private void CalculateTimeDilation()
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

            if (IsEnabled) return;

            if (!string.IsNullOrEmpty(effectName))
                part.Effect(effectName, 0, -1);
            UpdateTime();
        }

        [KSPEvent(guiName = "Deploy", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]//Deploy Scoop
        public void Deploy()
        {
            if (vessel != null && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH) && !canDeployOnSurface)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_Generic_CannotDeployOnSurface"), 5, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            if (vessel != null && vessel.atmDensity > 0 && !canDeployInAtmosphere)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_Generic_CannotDeployInAtmosphere"), 5, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            PluginHelper.RunAnimation(deployAnimName, _deployAnimation, deployAnimSpeed, 0);

            isDeployed = true;
        }

        [KSPEvent(guiName = "Retract", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]//Deploy Scoop
        public void Retract()
        {
            if (vessel != null && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH) && !canDeployOnSurface)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_Generic_CannotRetractOnSurface"), 5, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            if (vessel != null && vessel.atmDensity > 0 && !canDeployInAtmosphere)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_Generic_CannotRetractInAtmosphere"), 5, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            PluginHelper.RunAnimation(deployAnimName, _deployAnimation, -deployAnimSpeed, 1);

            isDeployed = false;
        }

        private void UpdateTime()
        {
            _universalTime = Planetarium.GetUniversalTime();
            CalculateTimeDilation();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (_curEngineT == null) return;

            if (_vesselChangedSioCountdown > 0)
                _vesselChangedSioCountdown--;

            UpdateTime();

            throttle = !_curEngineT.getFlameoutState && _curEngineT.currentThrottle > 0 ? Mathf.Max(_curEngineT.currentThrottle, 0.01f) : 0;

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown(Localizer.Format("#LOC_KSPIE_DeadalusEngineController_Shutdownreason1"));//"Inertial Fusion cannot operate in atmosphere!"

                if (_radHazard && rad_safety_features)
                    ShutDown(Localizer.Format("#LOC_KSPIE_DeadalusEngineController_Shutdownreason2"));//"Engines throttled down as they presently pose a radiation hazard"
            }

            KillKerbalsWithRadiation(throttle);

            if (!vessel.packed && !_warpToReal)
                storedThrottle = vessel.ctrlState.mainThrottle;

            // Update ISP
            maxEffectiveIsp = timeDilation * _engineIsp;

            UpdateAtmosphericCurve(maxEffectiveIsp);

            if (throttle > 0 && !vessel.packed)
            {
                TimeWarp.GThreshold = GThreshold;

                var thrustRatio = Math.Max(_curEngineT.thrustPercentage * 0.01, 0.01);
                var scaledThrottle = Math.Pow(thrustRatio * throttle, ispThrottleExponent);
                maxEffectiveIsp = timeDilation * _engineIsp * scaledThrottle;

                UpdateAtmosphericCurve(maxEffectiveIsp);

                fusionRatio = ProcessPowerAndWasteHeat(throttle);

                _curEngineT.enabled = fusionRatio > 0.01;

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, (float)(throttle * fusionRatio), -1);

                // Update Max Thrust
                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;

                var maxFusionThrust = fusionRatio * effectiveMaxThrustInKiloNewton;

                maxEffectiveThrust = maxFusionThrust * throttle;

                // Update FuelFlow
                _calculatedFuelflow = maxFusionThrust / maxEffectiveIsp / PhysicsGlobals.GravitationalAcceleration;

                maxEffectiveFlow = _calculatedFuelflow * throttle;

                massFlowRateKgPerSecond = thrustRatio * _curEngineT.currentThrottle * _calculatedFuelflow * 0.001;

                if (!_curEngineT.getFlameoutState && fusionRatio < 0.01)
                {
                    _curEngineT.status = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_curEngineTstatus1");//"Insufficient Electricity"
                }

                ratioHeadingVersusRequest = 0;
            }
            else if (vessel.packed && _curEngineT.currentThrottle > 0 && _curEngineT.getIgnitionState && _curEngineT.enabled && FlightGlobals.ActiveVessel == vessel && throttle > 0 && _percentageFuelRemaining1 > (100 - fuelLimit) && lightSpeedRatio < speedLimit)
            {
                if (!vessel.Autopilot.Enabled)
                {
                    var message = Localizer.Format("#LOC_KSPIE_Generic_ThrustWarpStoppedSasDisabled");
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    // Return to realtime
                    TimeWarp.SetRate(0, true);
                }

                _warpToReal = true; // Set to true for transition to realtime

                fusionRatio = CheatOptions.InfiniteElectricity
                    ? 1
                    : maximizeThrust
                        ? ProcessPowerAndWasteHeat(1)
                        : ProcessPowerAndWasteHeat(storedThrottle);

                _curEngineT.enabled = fusionRatio > 0.01;

                if (fusionRatio <= 0.01)
                {
                    var message = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg1");//"Thrust warp stopped - insufficient power"
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    // Return to realtime
                    TimeWarp.SetRate(0, true);
                }

                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                var maxFusionThrust = fusionRatio * effectiveMaxThrustInKiloNewton;

                maxEffectiveThrust = maxFusionThrust * throttle;

                _calculatedFuelflow = maxEffectiveIsp > 0 ? maxFusionThrust / maxEffectiveIsp / PhysicsGlobals.GravitationalAcceleration : 0;

                maxEffectiveFlow = _calculatedFuelflow * throttle;

                massFlowRateKgPerSecond = _calculatedFuelflow * 0.001;

                var realFixedDeltaTime = (double)(decimal) Math.Round(TimeWarp.fixedDeltaTime, 7);

                if (realFixedDeltaTime > 20)
                {
                    var deltaCalculations = Math.Ceiling(realFixedDeltaTime * 0.05);
                    var deltaTimeStep = realFixedDeltaTime / deltaCalculations;

                    for (var step = 0; step < deltaCalculations; step++)
                    {
                        PersistentThrust(deltaTimeStep, _universalTime + step * deltaTimeStep, part.transform.up, vessel.totalMass);
                        CalculateTimeDilation();
                    }
                }
                else
                    PersistentThrust(realFixedDeltaTime, _universalTime, part.transform.up, vessel.totalMass);

                if (fuelRatio < 0.999)
                {
                    var message = (fuelRatio <= 0) ? Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg2") : Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg3");//"Thrust warp stopped - propellant depleted" : "Thrust warp stopped - running out of propellant"
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    // Return to realtime
                    TimeWarp.SetRate(0, true);
                }

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, (float)(throttle * fusionRatio), -1);
            }
            else
            {
                ProcessPowerAndWasteHeat(0);

                ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSioCountdown > 0, ratioHeadingVersusRequest == 1);

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, 0, -1);

                powerUsage = "0.00" + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit") + " / " + EffectiveMaxPowerRequirement.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");

                if (!(_percentageFuelRemaining1 > (100 - fuelLimit) || lightSpeedRatio > speedLimit))
                {
                    _warpToReal = false;
                    vessel.ctrlState.mainThrottle = 0;
                }

                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;

                maxEffectiveThrust = effectiveMaxThrustInKiloNewton * throttle;

                _calculatedFuelflow = effectiveMaxThrustInKiloNewton / maxEffectiveIsp / PhysicsGlobals.GravitationalAcceleration;

                maxEffectiveFlow = _calculatedFuelflow * throttle;

                massFlowRateKgPerSecond = 0;
                fusionRatio = 0;
                _curEngineT.enabled = isDeployed && (_deployAnimation == null || !_deployAnimation.isPlaying);
            }

            _curEngineT.maxFuelFlow = Mathf.Max((float)_calculatedFuelflow,  1e-10f);
            _curEngineT.maxThrust =  Mathf.Max((float)effectiveMaxThrustInKiloNewton, 0.0001f);

            massFlowRateTonPerHour = massFlowRateKgPerSecond * 3.6;
            thrustPowerInTerraWatt = effectiveMaxThrustInKiloNewton * 500 * maxEffectiveIsp * PhysicsGlobals.GravitationalAcceleration * 1e-12;

            UpdateKerbalismEmitter();
        }

        private void UpdateAtmosphericCurve(double vacuumIsp)
        {
            var factoryVacuumIsp = _factoryFloatCurve.Evaluate(0);
            var conversionFactor = vacuumIsp / factoryVacuumIsp;

            var newAtmosphereCurve = new FloatCurve();
            foreach (var key in _factoryFloatCurve.Curve.keys)
            {
                newAtmosphereCurve.Add(key.time, (float)(key.value * conversionFactor), key.inTangent, key.outTangent);
            }

            _curEngineT.atmosphereCurve = newAtmosphereCurve;
        }

        private void PersistentThrust(double fixedDeltaTime, double modifiedUniversalTime, Vector3d thrustVector, double vesselMass)
        {
            ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSioCountdown > 0, ratioHeadingVersusRequest == 1);
            if (ratioHeadingVersusRequest != 1)
            {
                Debug.Log("[KSPI]: " + "quit persistent heading: " + ratioHeadingVersusRequest);
                return;
            }

            var timeDilationMaximumThrust = timeDilation * timeDilation * MaximumThrust * (maximizeThrust ? 1 : storedThrottle);

            var deltaVv = PluginHelper.CalculateDeltaVv(thrustVector, vesselMass, fixedDeltaTime, timeDilationMaximumThrust * fusionRatio, timeDilation * _engineIsp, out demandMass);

            double persistentThrustDot = Vector3d.Dot(this.part.transform.up, vessel.obt_velocity);
            if (persistentThrustDot < 0 && (vessel.obt_velocity.magnitude <= deltaVv.magnitude * 2))
            {
                var message = Localizer.Format("#LOC_KSPIE_DeadalusEngineController_PostMsg4");//"Thrust warp stopped - orbital speed too low"
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
                return;
            }

            fuelRatio = CollectFuel(demandMass);

            effectiveMaxThrustInKiloNewton = timeDilationMaximumThrust * fuelRatio;

            if (fuelRatio <= 0)
                return;

            vessel.orbit.Perturb(deltaVv * fuelRatio, modifiedUniversalTime);
        }

        private double CollectFuel(double mass)
        {
            if (CheatOptions.InfinitePropellant || mass <= 0)
                return 1;

            fusionFuelRequestAmount1 = 0.0;
            fusionFuelRequestAmount2 = 0.0;
            fusionFuelRequestAmount3 = 0.0;

            var totalAmount = mass / averageDensity;

            double availableRatio = 1;
            if (fuelFactor1 > 0)
            {
                fusionFuelRequestAmount1 = fuelFactor1 * totalAmount;
                availableRatio = Math.Min(part.GetResourceAvailable(_fuelResourceDefinition1, ResourceFlowMode.STACK_PRIORITY_SEARCH) / fusionFuelRequestAmount1, availableRatio);
            }
            if (fuelFactor2 > 0)
            {
                fusionFuelRequestAmount2 = fuelFactor2 * totalAmount;
                availableRatio = Math.Min(part.GetResourceAvailable(_fuelResourceDefinition2, ResourceFlowMode.STACK_PRIORITY_SEARCH) / fusionFuelRequestAmount2, availableRatio);
            }
            if (fuelFactor3 > 0)
            {
                fusionFuelRequestAmount3 = fuelFactor3 * totalAmount;
                availableRatio = Math.Min(part.GetResourceAvailable(_fuelResourceDefinition3, ResourceFlowMode.STACK_PRIORITY_SEARCH) / fusionFuelRequestAmount3, availableRatio);
            }

            if (availableRatio <= float.Epsilon)
                return 0;

            double receivedRatio = 1;
            if (fuelFactor1 > 0)
            {
                var receivedFusionFuel = part.RequestResource(_fuelResourceDefinition1.id, fusionFuelRequestAmount1 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount1 > 0 ? receivedFusionFuel / fusionFuelRequestAmount1 : 0);
            }
            if (fuelFactor2 > 0)
            {
                var receivedFusionFuel = part.RequestResource(_fuelResourceDefinition2.id, fusionFuelRequestAmount2 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount2 > 0 ? receivedFusionFuel / fusionFuelRequestAmount2 : 0);
            }
            if (fuelFactor3 > 0)
            {
                var receivedFusionFuel = part.RequestResource(_fuelResourceDefinition3.id, fusionFuelRequestAmount3 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount3 > 0 ? receivedFusionFuel / fusionFuelRequestAmount3 : 0);
            }
            return receivedRatio;
        }

        private double CalculateElectricalPowerCurrentlyNeeded(double maximumElectricPower)
        {
            var currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));
            var spareResourceCapacity = GetSpareResourceCapacity(ResourceSettings.Config.ElectricPowerInMegawatt);
            var powerRequestRatio = mhdPowerGenerationPercentage * 0.01;
            return Math.Min(maximumElectricPower, currentUnfilledResourceDemand * Math.Min(1, powerRequestRatio) + spareResourceCapacity * Math.Max(0, powerRequestRatio - 1));
        }

        private double ProcessPowerAndWasteHeat(float requestedThrottle)
        {
            if (!isDeployed)
                return 0;

            if (_deployAnimation != null && _deployAnimation.isPlaying)
                return 0;

            // Calculate Fusion Ratio
            var effectiveMaxPowerRequirement = EffectiveMaxPowerRequirement;
            var effectiveMaxPowerProduction = EffectiveMaxPowerProduction;
            var effectiveMaxFusionWasteHeat = EffectiveMaxFusionWasteHeat;

            var wasteheatRatio = GetResourceBarFraction(ResourceSettings.Config.WasteHeatInMegawatt);

            var wasteheatModifier = CheatOptions.IgnoreMaxTemperature || wasteheatRatio < 0.9 ? 1 : (1  - wasteheatRatio) * 10;

            var requestedPower = requestedThrottle * effectiveMaxPowerRequirement;

            var finalRequestedPower = requestedPower * wasteheatModifier;

            var receivedPower = CheatOptions.InfiniteElectricity || requestedPower <= 0
                ? finalRequestedPower
                : ConsumeFnResourcePerSecond(finalRequestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);

            var plasmaRatio = requestedPower <= 0 ? 1
                : !requestedPower.IsInfinityOrNaNorZero() && !receivedPower.IsInfinityOrNaNorZero() ? Math.Min(1, receivedPower / requestedPower) : 0;

            powerUsage = receivedPower.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit") + " / " + requestedPower.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");

            // The Absorbed wasteheat from Fusion production and reaction
            wasteHeat = requestedThrottle * plasmaRatio * effectiveMaxFusionWasteHeat;
            if (!CheatOptions.IgnoreMaxTemperature && requestedThrottle > 0)
            {
                SupplyFnResourcePerSecondWithMax(wasteHeat, effectiveMaxFusionWasteHeat, ResourceSettings.Config.WasteHeatInMegawatt);
            }

            var availablePower = requestedThrottle * plasmaRatio * effectiveMaxPowerProduction;

            var powerNeeded = CalculateElectricalPowerCurrentlyNeeded(effectiveMaxPowerProduction);
            var mhdPowerProductionResource = Kerbalism.IsLoaded ? part.Resources[mhdPowerProductionResourceName] : null;
            if (mhdPowerProductionResource != null)
            {
                var availableElectricCharge = GameConstants.ecPerMJ * Math.Max(0, availablePower - powerNeeded);
                mhdPowerProductionResource.maxAmount = availableElectricCharge;
                mhdPowerProductionResource.amount = availableElectricCharge;
            }

            if (!CheatOptions.InfiniteElectricity && effectiveMaxPowerProduction > 0 && requestedThrottle > 0)
            {
                SupplyFnResourcePerSecondWithMax(availablePower, effectiveMaxPowerProduction, ResourceSettings.Config.ElectricPowerInMegawatt);
            }

            return plasmaRatio;
        }

        private void KillKerbalsWithRadiation(float radiationRatio)
        {
            if (!_radHazard || radiationRatio <= 0 || rad_safety_features) return;

            var vesselsToRemove = new List<Vessel>();
            var crewToRemove = new List<ProtoCrewMember>();

            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);

                if (distance >= lethalDistance || currentVessel == vessel || currentVessel.GetCrewCount() <= 0) continue;

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

        public override int GetSupplyPriority()
        {
            return 1;
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();
            DetermineTechLevel();

            if (!string.IsNullOrEmpty(upgradeTechReq1))
            {
                sb.Append(LightBlue).Append(Localizer.Format("#LOC_KSPIE_Generic_upgradeTechnologies")).AppendLine(":</color><size=10>");
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

            sb.Append(LightBlue).Append(Localizer.Format("#LOC_KSPIE_Generic_EnginePerformance")).AppendLine(":</color><size=10>");
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

            if (powerRequirementMk1 > 0)
                sb.Append(LightBlue).Append(Localizer.Format("#LOC_KSPIE_Generic_PowerRequirementAndWasteheat")).AppendLine(":</color><size=10>");
            else
                sb.Append(LightBlue).Append(Localizer.Format("#LOC_KSPIE_Generic_Wasteheat")).AppendLine(":</color><size=10>");

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
