using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.External;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
using FNPlugin.Propulsion;
using FNPlugin.Redist;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Reactors
{
    [KSPModule("#LOC_KSPIE_Reactor_moduleName")]
    class InterstellarReactor : ResourceSuppliableModule, IFNPowerSource, IRescalable<InterstellarReactor>, IPartCostModifier
    {
        public const string Group = "InterstellarReactor";
        public const string GroupTitle = "#LOC_KSPIE_Reactor_groupName";

        public const string UpgradesGroup = "ReactorUpgrades";
        public const string UpgradesGroupDisplayName = "#LOC_KSPIE_Reactor_upgrades";

        public const double TritiumMolarMassRatio = 3.0160 / 7.0183;
        public const double HeliumMolarMassRatio = 4.0023 / 7.0183;

        //public enum ReactorTypes
        //{
        //    FISSION_MSR = 1,
        //    FISSION_GFR = 2,
        //    FUSION_DT = 4,
        //    FUSION_GEN3 = 8,
        //    AIM_FISSION_FUSION = 16,
        //    ANTIMATTER = 32
        //}

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricPowerPriority = 2;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_powerPercentage"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 10)]
        public float powerPercentage = 100;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_ForcedMinimumThrotle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]//Forced Minimum Throtle
        public float forcedMinimumThrottle = 0;

        // Persistent True
        [KSPField(isPersistant = true)] public int fuel_mode;
        [KSPField(isPersistant = true)] public int fuelmode_index = -1;
        [KSPField(isPersistant = true)] public string fuel_mode_name = string.Empty;
        [KSPField(isPersistant = true)] public string fuel_mode_variant = string.Empty;
        [KSPField(isPersistant = true)] public double startTime;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_ReactorIsEnabled")] public bool IsEnabled;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_ReactorIsStated")]  public bool IsStarted;

        [KSPField(isPersistant = true)] public bool isDeployed = false;
        [KSPField(isPersistant = true)] public bool isupgraded = false;
        [KSPField(isPersistant = true)] public bool breedtritium;
        [KSPField(isPersistant = true)] public double last_active_time;
        [KSPField(isPersistant = true)] public double ongoing_consumption_rate;
        [KSPField(isPersistant = true)] public bool reactorInit;
        [KSPField(isPersistant = true)] public double neutronEmbrittlementDamage;
        [KSPField(isPersistant = true)] public double maxEmbrittlementFraction = 0.5;
        [KSPField(isPersistant = true)] public float windowPositionX = 20;
        [KSPField(isPersistant = true)] public float windowPositionY = 20;
        [KSPField(isPersistant = true)] public int currentGenerationType;

        [KSPField(isPersistant = true)] public double factorAbsoluteLinear = 1;
        [KSPField(isPersistant = true)] public double storedPowerMultiplier = 1;
        [KSPField(isPersistant = true)] public double stored_fuel_ratio = 1;
        [KSPField(isPersistant = true)] public double fuel_ratio = 1;
        [KSPField(isPersistant = true)] public double maximumThermalPower;
        [KSPField(isPersistant = true)] public double maximumChargedPower;
        [KSPField(isPersistant = true)] public double reactor_power_ratio = 1;

        [KSPField(isPersistant = true)] public double storedIsThermalEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)] public double storedIsPlasmaEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)] public double storedIsChargedEnergyGeneratorEfficiency;

        [KSPField(isPersistant = true)] public double storedGeneratorThermalEnergyRequestRatio;
        [KSPField(isPersistant = true)] public double storedGeneratorPlasmaEnergyRequestRatio;
        [KSPField(isPersistant = true)] public double storedGeneratorChargedEnergyRequestRatio;

        [KSPField(isPersistant = true)]
        public double ongoing_total_power_generated;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_thermalPower", guiFormat = "F6")]
        protected double ongoing_thermal_power_generated;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_chargedPower ", guiFormat = "F6")]
        protected double ongoing_charged_power_generated;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_LithiumModifier", guiFormat = "F6")]
        public double lithium_modifier = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiFormat = "F2", guiName = "#LOC_KSPIE_Reactor_connectionRadius")]
        public double radius = 2.5;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiName = "#LOC_KSPIE_FissionPB_IsSwappingFuelMode")]//Is Swapping Fuel Mode
        public bool isSwappingFuelMode;

        [KSPField] public double maximumPower;
        [KSPField] public double lostThermalPowerRatio = 0;
        [KSPField] public double lostChargedPowerRatio = 0;
        [KSPField] public float minimumPowerPercentage = 10;
        [KSPField] public float defaultPowerGeneratorPercentage = 101;

        [KSPField] public string upgradeTechReqMk2;
        [KSPField] public string upgradeTechReqMk3;
        [KSPField] public string upgradeTechReqMk4 = null;
        [KSPField] public string upgradeTechReqMk5 = null;
        [KSPField] public string upgradeTechReqMk6 = null;
        [KSPField] public string upgradeTechReqMk7 = null;

        [KSPField] public double minimumThrottleMk1;
        [KSPField] public double minimumThrottleMk2;
        [KSPField] public double minimumThrottleMk3;
        [KSPField] public double minimumThrottleMk4;
        [KSPField] public double minimumThrottleMk5;
        [KSPField] public double minimumThrottleMk6;
        [KSPField] public double minimumThrottleMk7;

        [KSPField] public double fuelEfficencyMk1;
        [KSPField] public double fuelEfficencyMk2;
        [KSPField] public double fuelEfficencyMk3;
        [KSPField] public double fuelEfficencyMk4;
        [KSPField] public double fuelEfficencyMk5;
        [KSPField] public double fuelEfficencyMk6;
        [KSPField] public double fuelEfficencyMk7;

        [KSPField] public double hotBathTemperatureMk1 = 0;
        [KSPField] public double hotBathTemperatureMk2 = 0;
        [KSPField] public double hotBathTemperatureMk3 = 0;
        [KSPField] public double hotBathTemperatureMk4 = 0;
        [KSPField] public double hotBathTemperatureMk5 = 0;
        [KSPField] public double hotBathTemperatureMk6 = 0;
        [KSPField] public double hotBathTemperatureMk7 = 0;

        [KSPField] public double coreTemperatureMk1;
        [KSPField] public double coreTemperatureMk2;
        [KSPField] public double coreTemperatureMk3;
        [KSPField] public double coreTemperatureMk4;
        [KSPField] public double coreTemperatureMk5;
        [KSPField] public double coreTemperatureMk6;
        [KSPField] public double coreTemperatureMk7;

        [KSPField] public double basePowerOutputMk1 = 0;
        [KSPField] public double basePowerOutputMk2 = 0;
        [KSPField] public double basePowerOutputMk3 = 0;
        [KSPField] public double basePowerOutputMk4 = 0;
        [KSPField] public double basePowerOutputMk5 = 0;
        [KSPField] public double basePowerOutputMk6 = 0;
        [KSPField] public double basePowerOutputMk7 = 0;

        [KSPField] public double fusionEnergyGainFactorMk1 = 0;
        [KSPField] public double fusionEnergyGainFactorMk2;
        [KSPField] public double fusionEnergyGainFactorMk3;
        [KSPField] public double fusionEnergyGainFactorMk4;
        [KSPField] public double fusionEnergyGainFactorMk5;
        [KSPField] public double fusionEnergyGainFactorMk6;
        [KSPField] public double fusionEnergyGainFactorMk7;

        [KSPField] public string fuelModeTechReqLevel2;
        [KSPField] public string fuelModeTechReqLevel3;
        [KSPField] public string fuelModeTechReqLevel4;
        [KSPField] public string fuelModeTechReqLevel5;
        [KSPField] public string fuelModeTechReqLevel6;
        [KSPField] public string fuelModeTechReqLevel7;

        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiName = "#LOC_KSPIE_Reactor_powerOutputMk1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")] public double powerOutputMk1;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiName = "#LOC_KSPIE_Reactor_powerOutputMk2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")] public double powerOutputMk2;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiName = "#LOC_KSPIE_Reactor_powerOutputMk3", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")] public double powerOutputMk3;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiName = "#LOC_KSPIE_Reactor_powerOutputMk4", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")] public double powerOutputMk4;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiName = "#LOC_KSPIE_Reactor_powerOutputMk5", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")] public double powerOutputMk5;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiName = "#LOC_KSPIE_Reactor_powerOutputMk6", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")] public double powerOutputMk6;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiName = "#LOC_KSPIE_Reactor_powerOutputMk7", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")] public double powerOutputMk7;

        [KSPField] public bool chargedPowerProducesWasteheat = false;
        [KSPField] public bool allModesAvailableAtStart;
        [KSPField] public bool showEngineConnectionInfo = true;
        [KSPField] public bool showPowerGeneratorConnectionInfo = true;
        [KSPField] public bool mayExhaustInAtmosphereHomeworld = true;
        [KSPField] public bool mayExhaustInLowSpaceHomeworld = true;
        [KSPField] public bool canUseAllPowerForPlasma = true;
        [KSPField] public bool updateModuleCost = true;
        [KSPField] public bool supportMHD = false;
        [KSPField] public bool canShutdown = true;
        [KSPField] public bool canBeCombinedWithLab = false;
        [KSPField] public bool canBreedTritium = false;
        [KSPField] public bool canDisableTritiumBreeding = true;
        [KSPField] public bool showShutDownInFlight = false;
        [KSPField] public bool showForcedMinimumThrottle = false;
        [KSPField] public bool showPowerPercentage = true;
        [KSPField] public bool containsPowerGenerator = false;
        [KSPField] public bool usePropellantBaseIsp = false;
        [KSPField] public bool hasBuoyancyEffects = true;
        [KSPField] public bool hasOverheatEffects = true;
        [KSPField] public bool fullPowerForNonNeutronAbsorbants = true;
        [KSPField] public bool showPowerPriority = true;
        [KSPField] public bool showSpecialisedUI = true;
        [KSPField] public bool canUseNeutronicFuels = true;
        [KSPField] public bool simulateConsumption = false;

        [KSPField(guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true, advancedTweakable = true)] public bool _shouldApplyBalance;
        [KSPField(guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true, advancedTweakable = true)] public bool _isConnectedToPlasmaNozzle;

        [KSPField] public int fuelModeTechLevel;
        [KSPField] public int minCoolingFactor = 1;
        [KSPField] public int reactorModeTechBonus = 0;
        [KSPField] public int reactorType = 0;
        [KSPField] public int supportedPropellantAtoms = GameConstants.defaultSupportedPropellantAtoms;
        [KSPField] public int supportedPropellantTypes = GameConstants.defaultSupportedPropellantTypes;

        [KSPField] public string maxChargedParticleUtilisationTechMk2 = null;
        [KSPField] public string maxChargedParticleUtilisationTechMk3 = null;
        [KSPField] public string maxChargedParticleUtilisationTechMk4 = null;
        [KSPField] public string maxChargedParticleUtilisationTechMk5 = null;

        [KSPField] public string animName = "";
        [KSPField] public string loopingAnimationName = "";
        [KSPField] public string startupAnimationName = "";
        [KSPField] public string shutdownAnimationName = "";
        [KSPField] public string upgradedName = "";
        [KSPField] public string originalName = "";
        [KSPField] public string powerUpgradeTechReq = "";
        [KSPField] public string upgradeTechReq = "";

        [KSPField] public double engineHeatProductionMult = 1;
        [KSPField] public double plasmaHeatProductionMult = 1;
        [KSPField] public double engineWasteheatProductionMult = 1;
        [KSPField] public double plasmaWasteheatProductionMult = 1;
        [KSPField] public double minThermalNozzleTempRequired = 0;
        [KSPField] public double powerScaleExponent = 3;
        [KSPField] public double costScaleExponent = 1.86325;
        [KSPField] public double breedDivider = 100000;
        [KSPField] public double maxThermalNozzleIsp = 2997.13f;
        [KSPField] public double effectivePowerMultiplier;
        [KSPField] public double bonusBufferFactor = 0.05;
        [KSPField] public double thermalPowerBufferMult = 4;
        [KSPField] public double chargedPowerBufferMult = 4;
        [KSPField] public double massCoreTempExp = 0;
        [KSPField] public double massPowerExp = 0;
        [KSPField] public double heatTransportationEfficiency = 0.9;
        [KSPField] public double ReactorTemp = 0;
        [KSPField] public double powerOutputMultiplier = 1;
        [KSPField] public double upgradedReactorTemp = 0;
        [KSPField] public double animExponent = 1;
        [KSPField] public double reactorSpeedMult = 1;
        [KSPField] public double minimumThrottle = 0;
        [KSPField] public double fuelEfficiency = 1;
        [KSPField] public double fuelUsePerMJMult = 1;
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public double wasteHeatBufferMult = 1;
        [KSPField] public double wasteHeatBufferMassMult = 2.0e+6;
        [KSPField] public double hotBathTemperature;
        [KSPField] public double emergencyPowerShutdownFraction = 0.99;
        [KSPField] public double thermalPropulsionEfficiency = 1;
        [KSPField] public double plasmaPropulsionEfficiency = 1;
        [KSPField] public double chargedParticlePropulsionEfficiency = 1;
        [KSPField] public double thermalEnergyEfficiency = 1;
        [KSPField] public double chargedParticleEnergyEfficiency = 1;
        [KSPField] public double plasmaEnergyEfficiency = 1;
        [KSPField] public double maxGammaRayPower = 0;
        [KSPField] public double powerUpgradeCoreTempMult = 1;
        [KSPField] public double PowerOutput = 0;
        [KSPField] public double massCostExponent = 2.5;
        [KSPField] public double plasmaAfterburnerRange = 2;

        [KSPField] public double magneticNozzlePowerMult = 1;
        [KSPField] public double magneticNozzleMhdMult = 2;

        [KSPField] public double overheatMultiplier = 10;
        [KSPField] public double overheatThreshHold = 0.95;
        [KSPField] public double overheatExponent = 2;
        [KSPField] public double minOverheatModifier = 0.01;

        [KSPField] public double minGeeForceModifier = 0.01;
        [KSPField] public double geeForceMultiplier = 0.1;
        [KSPField] public double geeForceThreshHold = 9;
        [KSPField] public double geeForceExponent = 2;

        [KSPField] public double maxChargedParticleUtilisationRatio = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk1 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk2 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk3 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk4 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk5 = 1;

        [KSPField] public string soundTerminateFilePath = "";
        [KSPField] public string soundInitiateFilePath = "";
        [KSPField] public string soundRunningFilePath = "";
        [KSPField] public double soundRunningPitchMin = 0.4;
        [KSPField] public double soundRunningPitchExp = 0;
        [KSPField] public double soundRunningPitchMult = 1;
        [KSPField] public double soundRunningVolumeExp = 1;
        [KSPField] public double soundRunningVolumeMin = 0;
        [KSPField] public double soundRunningVolumeMult = 1;

        [KSPField] public double neutronEmbrittlementLifepointsMax = 100;
        [KSPField] public double neutronEmbrittlementDivider = 1e+9;
        [KSPField] public double hotBathModifier = 1;
        [KSPField] public double thermalProcessingModifier = 1;

        [KSPField] public double maxChargedParticleRatio = 1;
        [KSPField] public double minChargedParticleRatio = 0;

        [KSPField] public double maxNeutronsRatio = 1.04;
        [KSPField] public double minNeutronsRatio = 0;

        // GUI strings
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_rawPowerOutput", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double currentRawPowerOutput;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_reactorStatus")]
        public string statusStr = string.Empty;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_coreTemperature")]
        public string coretempStr = string.Empty;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorFuelMode")]
        public string fuelModeStr = string.Empty;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_connectedRecievers")]
        public string connectedRecieversStr = string.Empty;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_reactorSurface", guiUnits = " m\xB3")] public double reactorSurface;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_InitialCost")] public double initialCost;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_CalculatedCost")] public double calculatedCost;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_MaxResourceCost")] public double maxResourceCost;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_ModuleCost")] public float moduleCost;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_NeutronEmbrittlementCost")] public double neutronEmbrittlementCost;

        // Gui
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public float massDifference;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_CalibratedMass", guiUnits = " t")]//calibrated mass
        public float partMass = 0;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorMass", guiFormat = "F3", guiUnits = " t")]
        public float currentMass;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_EmbrittlementFraction", guiFormat = "F4")]//Embrittlement Fraction
        public double embrittlementModifier;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_BuoyancyFraction", guiFormat = "F4")]//Buoyancy Fraction
        public double geeForceModifier = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_Reactor_OverheatFraction", guiFormat = "F4")]//Overheat Fraction
        public double overheatModifier = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorControlWindow", guiActiveUnfocused = true), UI_Toggle(disabledText = "#LOC_KSPIE_Reactor_reactorControlWindow_Hidden", enabledText = "#LOC_KSPIE_Reactor_reactorControlWindow_Shown", affectSymCounterparts = UI_Scene.None)]//Hidden-Shown
        public bool renderWindow;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_startEnabled", guiActiveUnfocused = true), UI_Toggle(disabledText = "#LOC_KSPIE_Reactor_startEnabled_True", enabledText = "#LOC_KSPIE_Reactor_startEnabled_False")]//True-False
        public bool startDisabled;

        // shared variables
        protected bool ongoingDecay;
        protected bool initialized;
        protected bool hasStarted;
        protected bool messagedRanOutOfFuel;

        protected double maxPowerToSupply;
        protected double minThrottle;
        protected double currentGeeForce;
        protected double animationStarted = 0;
        protected double powerPercent;
        protected double totalAmountLithium;
        protected double totalMaxAmountLithium;
        protected double maximumThermalPowerEffective;
        protected double lithiumNeutronAbsorption = 1;

        protected Rect windowPosition;
        protected GUIStyle boldStyle;
        protected GUIStyle textStyle;
        protected List<ReactorFuelType> fuelModes;
        protected List<ReactorFuelMode> currentFuelVariantsSorted;
        protected ReactorFuelMode currentFuelVariant;
        protected ReactorFuelMode previousFuelVariant;
        protected AnimationState[] pulseAnimation;
        protected ModuleAnimateGeneric startupAnimation;
        protected ModuleAnimateGeneric shutdownAnimation;
        protected ModuleAnimateGeneric loopingAnimation;

        private FNHabitat _centrifugeHabitat;
        private ReactorFuelType _currentFuelMode;
        private ResourceBuffers _resourceBuffers;
        private FNEmitterController _emitterController;
        private ModuleGenerator _heliumModuleGenerator;

        private PartResourceDefinition _lithium6Def;
        private PartResourceDefinition _tritiumDef;
        private PartResourceDefinition _heliumDef;
        private PartResourceDefinition _hydrogenDefinition;

        private readonly List<ReactorProduction> _reactorProduction = new List<ReactorProduction>();
        private readonly List<IFNEngineNoozle> _connectedEngines = new List<IFNEngineNoozle>();

        private readonly Queue<double> _averageGeeforce = new Queue<double>();
        private readonly Queue<double> _averageOverheat = new Queue<double>();
        private readonly Queue<double> _wasteheatBuffer = new Queue<double>();

        private AudioSource _initiateSound;
        private AudioSource _terminateSound;
        private AudioSource _runningSound;

        private double _tritiumDensity;
        private double _helium4Density;
        private double _lithium6Density;

        [KSPField(guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true, advancedTweakable = true)]
        private double _currentPropulsionRequestRatioSum;

        private double _consumedFuelTotalFixed;
        private double _connectedReceiversSum;
        private double _previousReactorPowerRatio;

        private double _currentThermalEnergyGeneratorMass;
        private double _currentChargedEnergyGeneratorMass;

        private double _tritiumBreedingMassAdjustment;
        private double _heliumBreedingMassAdjustment;
        private double _staticBreedRate;

        private double _currentIsThermalEnergyGeneratorEfficiency;
        private double _currentIsChargedEnergyGeneratorEfficiency;
        private double _currentIsPlasmaEnergyGeneratorEfficiency;

        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _currentGeneratorThermalEnergyRequestRatio;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _currentGeneratorPlasmaEnergyRequestRatio;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _currentGeneratorChargedEnergyRequestRatio;

        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maximumThermalRequestRatio;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maximumChargedRequestRatio;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maximumReactorRequestRatio;

        private double _lithiumConsumedPerSecond;
        private double _tritiumProducedPerSecond;
        private double _heliumProducedPerSecond;
        private double _auxiliaryPowerAvailable;

        private int _windowId = 90175467;
        private int _deactivateTimer;
        private int _chargedParticleUtilisationLevel = 1;

        private bool _hasSpecificFuelModeTechs;
        private bool _isFixedUpdatedCalled;

        // properties
        public double PlasmaAfterburnerRange => plasmaAfterburnerRange;
        public double ForcedMinimumThrottleRatio => (double)(decimal)forcedMinimumThrottle / 100;
        public double EfficencyConnectedThermalEnergyGenerator => storedIsThermalEnergyGeneratorEfficiency;
        public double EfficencyConnectedChargedEnergyGenerator => storedIsChargedEnergyGeneratorEfficiency;
        public float DefaultPowerGeneratorPercentage => defaultPowerGeneratorPercentage;
        public double FuelRatio => fuel_ratio;
        public double MinThermalNozzleTempRequired => minThermalNozzleTempRequired;
        public double MinCoolingFactor => minCoolingFactor;
        public double EngineHeatProductionMult => engineHeatProductionMult;
        public double PlasmaHeatProductionMult => plasmaHeatProductionMult;
        public double EngineWasteheatProductionMult => engineWasteheatProductionMult;
        public double PlasmaWasteheatProductionMult => plasmaWasteheatProductionMult;
        public double ThermalPropulsionWasteheatModifier => 1;
        public double ConsumedFuelFixed => _consumedFuelTotalFixed;
        public double Radius => radius;
        public double ThermalProcessingModifier => thermalProcessingModifier;
        public double ProducedWasteHeat => ongoing_total_power_generated;
        public double ProducedThermalHeat => ongoing_thermal_power_generated;
        public double ProducedChargedPower => ongoing_charged_power_generated;
        public double ThermalTransportationEfficiency => heatTransportationEfficiency;
        public double RawTotalPowerProduced => ongoing_total_power_generated;
        public double ChargedParticlePropulsionEfficiency => chargedParticlePropulsionEfficiency * maxChargedParticleUtilisationRatio;
        public double PlasmaPropulsionEfficiency => plasmaPropulsionEfficiency * maxChargedParticleUtilisationRatio;
        public double ThermalPropulsionEfficiency => thermalPropulsionEfficiency;
        public double ThermalEnergyEfficiency => thermalEnergyEfficiency;
        public double PlasmaEnergyEfficiency => plasmaEnergyEfficiency;
        public double ChargedParticleEnergyEfficiency => chargedParticleEnergyEfficiency;
        public double PowerBufferBonus => bonusBufferFactor;
        public double RawMaximumPowerForPowerGeneration => RawPowerOutput;
        public double RawMaximumPower => RawPowerOutput;
        public double ReactorSpeedMult => reactorSpeedMult;
        public double ThermalPowerRatio => 1 - ChargedPowerRatio;
        public double PowerRatio => (double)(decimal)powerPercentage / 100;

        public bool MayExhaustInAtmosphereHomeworld => mayExhaustInAtmosphereHomeworld;
        public bool MayExhaustInLowSpaceHomeworld => mayExhaustInLowSpaceHomeworld;
        public bool UsePropellantBaseIsp => usePropellantBaseIsp;
        public bool CanUseAllPowerForPlasma => canUseAllPowerForPlasma;
        public bool IsThermalSource => true;
        public bool IsSelfContained => containsPowerGenerator;
        public bool SupportMHD => supportMHD;
        public bool FullPowerForNonNeutronAbsorbants => fullPowerForNonNeutronAbsorbants;

        public int ProviderPowerPriority => (int)electricPowerPriority;
        public int ReactorFuelModeTechLevel => fuelModeTechLevel + reactorModeTechBonus;
        public int ReactorType => reactorType;
        public int SupportedPropellantAtoms => supportedPropellantAtoms;
        public int SupportedPropellantTypes => supportedPropellantTypes;

        [KSPField(advancedTweakable = true, guiActive = false)] public double _requestedThermalThrottle;
        public double RequestedThermalThrottle
        {
            get => _requestedThermalThrottle;
            private set => _requestedThermalThrottle = value;
        }

        [KSPField(advancedTweakable = true, guiActive = false)] public double _requestedPlasmaThrottle;
        public double RequestedPlasmaThrottle
        {
            get => _requestedPlasmaThrottle;
            private set => _requestedPlasmaThrottle = value;
        }

        [KSPField(advancedTweakable = true, guiActive = false)] public double _requestedChargedThrottle;
        public double RequestedChargedThrottle
        {
            get => _requestedChargedThrottle;
            private set => _requestedChargedThrottle = value;
        }

        [KSPField(advancedTweakable = true, guiActive = false)] public double _currentPlasmaPropulsionRatio;
        public double CurrentPlasmaPropulsionRatio
        {
            get => _currentPlasmaPropulsionRatio;
            private set => _currentPlasmaPropulsionRatio = value;
        }

        [KSPField(advancedTweakable = true, guiActive = false)] public double _currentChargedPropulsionRatio;
        public double CurrentChargedPropulsionRatio
        {
            get => _currentChargedPropulsionRatio;
            private set => _currentChargedPropulsionRatio = value;
        }

        public GenerationType CurrentGenerationType => (GenerationType)currentGenerationType;
        public GenerationType FuelModeTechLevel => (GenerationType)fuelModeTechLevel;
        public Part Part => part;

        public string UpgradeTechnology => upgradeTechReq;

        public virtual double MagneticNozzlePowerMult => magneticNozzlePowerMult;
        public virtual double MagneticNozzleMhdMult => magneticNozzleMhdMult;
        public virtual double CurrentMeVPerChargedProduct => _currentFuelMode?.MeVPerChargedProduct ?? 0;
        public virtual string TypeName => part.partInfo.title;
        public virtual double ChargedPowerRatio => CurrentFuelMode?.ChargedPowerRatio ?? 0;
        public virtual double ReactorEmbrittlementConditionRatio => Math.Min(Math.Max(1 - (neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), maxEmbrittlementFraction), 1);
        public virtual double NormalisedMaximumPower => RawPowerOutput * EffectiveEmbrittlementEffectRatio * (CurrentFuelMode?.ReactionRatePowerMultiplier ?? 1);
        public virtual double MinimumPower => MaximumPower * MinimumThrottle;
        public virtual double MaximumThermalPower => PowerRatio * NormalisedMaximumPower * ThermalPowerRatio * geeForceModifier * overheatModifier;
        public virtual double MaximumChargedPower => PowerRatio * NormalisedMaximumPower * ChargedPowerRatio * geeForceModifier * overheatModifier;
        public virtual bool CanProducePower => stored_fuel_ratio > 0;
        public virtual bool IsNuclear => false;
        public virtual bool IsActive => IsEnabled;
        public virtual bool IsVolatileSource => false;
        public virtual bool IsFuelNeutronRich => false;
        public virtual double MaximumPower => MaximumThermalPower + MaximumChargedPower;
        public virtual double StableMaximumReactorPower => IsEnabled ? NormalisedMaximumPower : 0;
        public virtual double MaxCoreTemperature => CoreTemperature;

        public bool IsConnectedToThermalGenerator { get; private set; }
        public bool IsConnectedToChargedGenerator { get; private set; }


        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }
        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        public ReactorFuelMode CurrentFuelVariant
        {
            get => currentFuelVariant;
            set
            {
                currentFuelVariant = value;
                fuel_mode_variant = currentFuelVariant?.Name;

                if (currentFuelVariant != null && previousFuelVariant != null && currentFuelVariant != previousFuelVariant)
                {
                    PartResource previousReactorFuelProcess = part.Resources["_" + previousFuelVariant.Name];

                    if (previousReactorFuelProcess != null)
                    {
                        previousReactorFuelProcess.maxAmount = 0;
                        previousReactorFuelProcess.amount = 0;
                    }

                }

                previousFuelVariant = value;
            }
        }

        // Complex Getters
        public double FusionEnergyGainFactor
        {
            get
            {
                switch (FuelModeTechLevel)
                {
                    case GenerationType.Mk7: return fusionEnergyGainFactorMk7;
                    case GenerationType.Mk6: return fusionEnergyGainFactorMk6;
                    case GenerationType.Mk5: return fusionEnergyGainFactorMk5;
                    case GenerationType.Mk4: return fusionEnergyGainFactorMk4;
                    case GenerationType.Mk3: return fusionEnergyGainFactorMk3;
                    case GenerationType.Mk2: return fusionEnergyGainFactorMk2;
                    default: return fusionEnergyGainFactorMk1;
                }
            }
        }

        public virtual double MinimumThrottle
        {
            get
            {
                double leveledThrottle;

                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7: leveledThrottle = minimumThrottleMk7; break;
                    case GenerationType.Mk6: leveledThrottle = minimumThrottleMk6; break;
                    case GenerationType.Mk5: leveledThrottle = minimumThrottleMk5; break;
                    case GenerationType.Mk4: leveledThrottle = minimumThrottleMk4; break;
                    case GenerationType.Mk3: leveledThrottle = minimumThrottleMk3; break;
                    case GenerationType.Mk2: leveledThrottle = minimumThrottleMk2; break;
                    case GenerationType.Mk1: leveledThrottle = minimumThrottleMk1; break;
                    default: leveledThrottle = minimumThrottleMk7; break;
                }

                return leveledThrottle;
            }
        }

        public virtual double FuelEfficiency
        {
            get
            {
                double baseEfficiency;
                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7: baseEfficiency = fuelEfficencyMk7; break;
                    case GenerationType.Mk6: baseEfficiency = fuelEfficencyMk6; break;
                    case GenerationType.Mk5: baseEfficiency = fuelEfficencyMk5; break;
                    case GenerationType.Mk4: baseEfficiency = fuelEfficencyMk4; break;
                    case GenerationType.Mk3: baseEfficiency = fuelEfficencyMk3; break;
                    case GenerationType.Mk2: baseEfficiency = fuelEfficencyMk2; break;
                    default: baseEfficiency = fuelEfficencyMk1; break;
                }

                return baseEfficiency * CurrentFuelMode.FuelEfficiencyMultiplier;
            }
        }

        public virtual double CoreTemperature
        {
            get
            {
                double baseCoreTemperature;
                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7: baseCoreTemperature = coreTemperatureMk7; break;
                    case GenerationType.Mk6: baseCoreTemperature = coreTemperatureMk6; break;
                    case GenerationType.Mk5: baseCoreTemperature = coreTemperatureMk5; break;
                    case GenerationType.Mk4: baseCoreTemperature = coreTemperatureMk4; break;
                    case GenerationType.Mk3: baseCoreTemperature = coreTemperatureMk3; break;
                    case GenerationType.Mk2: baseCoreTemperature = coreTemperatureMk2; break;
                    default: baseCoreTemperature = coreTemperatureMk1; break;
                }

                return baseCoreTemperature * Math.Pow(overheatModifier, 1.5) * EffectiveEmbrittlementEffectRatio * Math.Pow(part.mass / partMass, massCoreTempExp);
            }
        }

        public double HotBathTemperature
        {
            get
            {
                if (hotBathTemperature <= 0)
                {
                    switch (CurrentGenerationType)
                    {
                        case GenerationType.Mk7: hotBathTemperature = hotBathTemperatureMk7; break;
                        case GenerationType.Mk6: hotBathTemperature = hotBathTemperatureMk6; break;
                        case GenerationType.Mk5: hotBathTemperature = hotBathTemperatureMk5; break;
                        case GenerationType.Mk4: hotBathTemperature = hotBathTemperatureMk4; break;
                        case GenerationType.Mk3: hotBathTemperature = hotBathTemperatureMk3; break;
                        case GenerationType.Mk2: hotBathTemperature = hotBathTemperatureMk2; break;
                        default: hotBathTemperature = hotBathTemperatureMk1; break;
                    }
                }

                if (hotBathTemperature <= 0)
                    return CoreTemperature * hotBathModifier;
                else
                    return hotBathTemperature;
            }
        }

        public double RawPowerOutput
        {
            get
            {
                double rawPowerOutput;

                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7: rawPowerOutput = powerOutputMk7; break;
                    case GenerationType.Mk6: rawPowerOutput = powerOutputMk6; break;
                    case GenerationType.Mk5: rawPowerOutput = powerOutputMk5; break;
                    case GenerationType.Mk4: rawPowerOutput = powerOutputMk4; break;
                    case GenerationType.Mk3: rawPowerOutput = powerOutputMk3; break;
                    case GenerationType.Mk2: rawPowerOutput = powerOutputMk2; break;
                    default: rawPowerOutput = powerOutputMk1; break;
                }

                return rawPowerOutput;
            }
        }

        public ReactorFuelType CurrentFuelMode
        {
            get
            {
                if (_currentFuelMode != null)
                    return _currentFuelMode;

                Debug.Log("[KSPI]: CurrentFuelMode setting default fuelmode");
                SetDefaultFuelMode();

                return _currentFuelMode;
            }
            set
            {
                _currentFuelMode = value;
                maxPowerToSupply = Math.Max(MaximumPower * (double)(decimal)TimeWarp.fixedDeltaTime, 0);
                currentFuelVariantsSorted = _currentFuelMode.GetVariantsOrderedByFuelRatio(part, FuelEfficiency, maxPowerToSupply, fuelUsePerMJMult);
                CurrentFuelVariant = currentFuelVariantsSorted.First();

                // persist
                fuelmode_index = fuelModes.IndexOf(_currentFuelMode);
                fuel_mode = fuelmode_index;
                fuel_mode_name = _currentFuelMode.ModeGUIName;
            }
        }

        public double EffectiveEmbrittlementEffectRatio
        {
            get
            {
                embrittlementModifier = CheatOptions.UnbreakableJoints ? 1 : Math.Sin(ReactorEmbrittlementConditionRatio * Math.PI * 0.5);
                return embrittlementModifier;
            }
        }

        protected bool FullFuelRequirements()
        {
            return !CurrentFuelMode.Hidden && HasAllFuels() && FuelRequiresLab(CurrentFuelMode.RequiresLab);
        }

        protected bool HasAllFuels()
        {
            if (CheatOptions.InfinitePropellant)
                return true;

            var hasAllFuels = true;
            foreach (var fuel in currentFuelVariantsSorted.First().ReactorFuels)
            {
                if (!(GetFuelRatio(fuel, FuelEfficiency, NormalisedMaximumPower) < 1)) continue;

                hasAllFuels = false;
                break;
            }
            return hasAllFuels;
        }

        private void DetermineChargedParticleUtilizationRatio()
        {
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk2)) _chargedParticleUtilisationLevel++;
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk3)) _chargedParticleUtilisationLevel++;
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk4)) _chargedParticleUtilisationLevel++;
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk5)) _chargedParticleUtilisationLevel++;

            if (_chargedParticleUtilisationLevel == 1) maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk1;
            else if (_chargedParticleUtilisationLevel == 2) maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk2;
            else if (_chargedParticleUtilisationLevel == 3) maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk3;
            else if (_chargedParticleUtilisationLevel == 4) maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk4;
            else maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk5;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            neutronEmbrittlementCost = calculatedCost * Math.Pow((neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), 0.5);

            maxResourceCost = part.Resources.Sum(m => m.maxAmount * m.info.unitCost);

            var dryCost = calculatedCost - initialCost;

            moduleCost = updateModuleCost ? (float)(maxResourceCost + dryCost - neutronEmbrittlementCost) : 0;

            return moduleCost;
        }

        public void UpdateAuxiliaryPowerSource(double available)
        {
            _auxiliaryPowerAvailable = available;
        }


        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public void UseProductForPropulsion(double ratio, double propellantMassPerSecond)
        {
            UseProductForPropulsion(ratio, propellantMassPerSecond, new [] {_hydrogenDefinition});
        }

        public void UseProductForPropulsion(double chargedRatio, double propellantMassPerSecond, PartResourceDefinition[] resource)
        {
            if (chargedRatio <= 0) return;

            if (resource.Length == 0) return;

            var reactorProductMass = 0d;
            foreach (var product in _reactorProduction)
            {
                if (product.mass <= 0) continue;

                var effectiveMass = chargedRatio * product.mass;
                reactorProductMass += effectiveMass;

                // remove product from store
                var fuelAmount = product.fuelMode.DensityInTon > 0 ? effectiveMass / product.fuelMode.DensityInTon : 0;
                if (fuelAmount == 0) continue;

                part.RequestResource(product.fuelMode.ResourceName, fuelAmount);
            }

            var resourceRatio = 1d / resource.Length;
            var reAddMass = Math.Min(reactorProductMass, propellantMassPerSecond) * TimeWarp.fixedDeltaTime;
            foreach (var partResourceDefinition in resource)
            {
                // re-add consumed resource
                var amount = resourceRatio * reAddMass / partResourceDefinition.density;
                part.RequestResource(partResourceDefinition.name, -amount, ResourceFlowMode.ALL_VESSEL);
            }
        }

        public void ConnectWithEngine(IEngineNoozle engine)
        {
            Debug.Log("[KSPI]: ConnectWithEngine ");

            var fnEngine = engine as IFNEngineNoozle;
            if (fnEngine == null)
            {
                Debug.LogError("[KSPI]: engine is not a IFNEngineNozzle");
                return;
            }

            _isConnectedToPlasmaNozzle = fnEngine.IsPlasmaNozzle;

            if (!_connectedEngines.Contains(fnEngine))
                _connectedEngines.Add(fnEngine);
        }

        public void DisconnectWithEngine(IEngineNoozle engine)
        {
            Debug.Log("[KSPI]: DisconnectWithEngine ");

            if (!(engine is IFNEngineNoozle fnEngine))
            {
                Debug.LogError("[KSPI]: engine is not a IFNEngineNozzle");
                return;
            }

            _isConnectedToPlasmaNozzle = false;

            if (_connectedEngines.Contains(fnEngine))
                _connectedEngines.Remove(fnEngine);
        }

        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, bool isMHD, double mass)
        {
            _currentThermalEnergyGeneratorMass = mass;

            if (isMHD)
            {
                _currentIsPlasmaEnergyGeneratorEfficiency = efficency;
                _currentGeneratorPlasmaEnergyRequestRatio = power_ratio;
            }
            else
            {
                _currentIsThermalEnergyGeneratorEfficiency = efficency;
                _currentGeneratorThermalEnergyRequestRatio = power_ratio;
            }
            IsConnectedToThermalGenerator = true;
        }

        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio)
        {
            _currentIsThermalEnergyGeneratorEfficiency = efficency;
            _currentGeneratorThermalEnergyRequestRatio = power_ratio;
            IsConnectedToThermalGenerator = true;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio)
        {
            _currentIsChargedEnergyGeneratorEfficiency = efficency;
            _currentGeneratorChargedEnergyRequestRatio = power_ratio;
            IsConnectedToChargedGenerator = true;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio, double mass)
        {
            _currentChargedEnergyGeneratorMass = mass;
            _currentIsChargedEnergyGeneratorEfficiency = efficency;
            _currentGeneratorChargedEnergyRequestRatio = power_ratio;
            IsConnectedToChargedGenerator = true;
        }

        public double NormalizedPowerMultiplier => _currentFuelMode.NormalizedPowerMultiplier;

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType)
        {
            _shouldApplyBalance = IsConnectedToThermalGenerator && (IsConnectedToChargedGenerator || _isConnectedToPlasmaNozzle);
            return _shouldApplyBalance;
        }

        public override void AttachThermalReciever(Guid key, double radius)
        {
            if (!connectedReceivers.ContainsKey(key))
                connectedReceivers.Add(key, radius);
            UpdateConnectedReceiversStr();
        }

        public override void DetachThermalReciever(Guid key)
        {
            if (connectedReceivers.ContainsKey(key))
                connectedReceivers.Remove(key);
            UpdateConnectedReceiversStr();
        }

        public virtual void OnRescale(ScalingFactor factor)
        {
            // calculate multipliers
            factorAbsoluteLinear = (double) (decimal) factor.absolute.linear;
            Debug.Log("[KSPI]: InterstellarReactor.OnRescale called with " + factorAbsoluteLinear);
            storedPowerMultiplier = Math.Pow(factorAbsoluteLinear, powerScaleExponent);

            initialCost = part.partInfo.cost * Math.Pow(factorAbsoluteLinear, massCostExponent);
            calculatedCost = part.partInfo.cost * Math.Pow(factorAbsoluteLinear, costScaleExponent);

            // update power
            DeterminePowerOutput();

            // refresh generators mass
            ConnectedThermalElectricGenerator?.Refresh();
            ConnectedChargedParticleElectricGenerator?.Refresh();
        }

        private void UpdateConnectedReceiversStr()
        {
            if (connectedReceivers == null) return;

            _connectedReceiversSum = connectedReceivers.Sum(r => r.Value * r.Value);
            connectedReceiversFraction.Clear();
            foreach (var pair in connectedReceivers)
                connectedReceiversFraction[pair.Key] = pair.Value * pair.Value / _connectedReceiversSum;

            reactorSurface = Math.Pow(radius, 2);
            connectedRecieversStr = connectedReceivers.Count() + " (" + _connectedReceiversSum.ToString("0.000") + " m2)";
        }

        public virtual void StartReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                startDisabled = false;
            else
            {
                if (IsStarted && IsNuclear) return;

                stored_fuel_ratio = 1;
                IsEnabled = true;
                if (_runningSound != null)
                    _runningSound.Play();
            }
        }

        [KSPEvent(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_activateReactor", active = false)]
        public void ActivateReactor()
        {
            Debug.Log("[KSPI]: InterstellarReactor on " + part.name + " was Force Activated");
            part.force_activate();

            Events[nameof(ActivateReactor)].guiActive = false;
            Events[nameof(ActivateReactor)].active = false;

            if (_centrifugeHabitat != null && !_centrifugeHabitat.isDeployed)
            {
                var message = Localizer.Format("#LOC_KSPIE_Reactor_PostMsg1", part.name);
                ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
                Debug.LogWarning("[KSPI]: " + message);
                return;
            }

            StartReactor();
            IsStarted = true;
        }

        [KSPEvent(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_deactivateReactor", active = true)]
        public void DeactivateReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                startDisabled = true;
            else
            {
                if (IsNuclear) return;

                IsEnabled = false;

                if (_runningSound != null)
                    _runningSound.Stop();
            }
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_Reactor_enableTritiumBreeding", active = false)]
        public void StartBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = true;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_Reactor_disableTritiumBreeding", active = true)]
        public void StopBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = false;
        }

        [KSPAction("#LOC_KSPIE_Reactor_activateReactor")]
        public void ActivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            StartReactor();
        }

        [KSPAction("#LOC_KSPIE_Reactor_deactivateReactor")]
        public void DeactivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            DeactivateReactor();
        }

        [KSPAction("#LOC_KSPIE_Reactor_toggleReactor")]
        public void ToggleReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            IsEnabled = !IsEnabled;
        }

        public void DeterminePowerOutput()
        {
            massDifference = part.mass / partMass;

            effectivePowerMultiplier = storedPowerMultiplier * powerOutputMultiplier * Math.Pow(massDifference, massPowerExp);

            powerOutputMk1 = basePowerOutputMk1 * effectivePowerMultiplier;
            powerOutputMk2 = basePowerOutputMk2 * effectivePowerMultiplier;
            powerOutputMk3 = basePowerOutputMk3 * effectivePowerMultiplier;
            powerOutputMk4 = basePowerOutputMk4 * effectivePowerMultiplier;
            powerOutputMk5 = basePowerOutputMk5 * effectivePowerMultiplier;
            powerOutputMk6 = basePowerOutputMk6 * effectivePowerMultiplier;
            powerOutputMk7 = basePowerOutputMk7 * effectivePowerMultiplier;

            Fields[nameof(powerOutputMk1)].guiActiveEditor = true;
            Fields[nameof(powerOutputMk2)].guiActiveEditor = !string.IsNullOrEmpty(fuelModeTechReqLevel2);
            Fields[nameof(powerOutputMk3)].guiActiveEditor = !string.IsNullOrEmpty(fuelModeTechReqLevel3);
            Fields[nameof(powerOutputMk4)].guiActiveEditor = !string.IsNullOrEmpty(fuelModeTechReqLevel4);
            Fields[nameof(powerOutputMk5)].guiActiveEditor = !string.IsNullOrEmpty(fuelModeTechReqLevel5);
            Fields[nameof(powerOutputMk6)].guiActiveEditor = !string.IsNullOrEmpty(fuelModeTechReqLevel6);
            Fields[nameof(powerOutputMk7)].guiActiveEditor = !string.IsNullOrEmpty(fuelModeTechReqLevel7);

            // initialise power output when missing
            if (powerOutputMk2 <= 0) powerOutputMk2 = powerOutputMk1 * 1.5;
            if (powerOutputMk3 <= 0) powerOutputMk3 = powerOutputMk2 * 1.5;
            if (powerOutputMk4 <= 0) powerOutputMk4 = powerOutputMk3 * 1.5;
            if (powerOutputMk5 <= 0) powerOutputMk5 = powerOutputMk4 * 1.5;
            if (powerOutputMk6 <= 0) powerOutputMk6 = powerOutputMk5 * 1.5;
            if (powerOutputMk7 <= 0) powerOutputMk7 = powerOutputMk6 * 1.5;

            if (minimumThrottleMk1 <= 0) minimumThrottleMk1 = minimumThrottle;
            if (minimumThrottleMk2 <= 0) minimumThrottleMk2 = minimumThrottleMk1;
            if (minimumThrottleMk3 <= 0) minimumThrottleMk3 = minimumThrottleMk2;
            if (minimumThrottleMk4 <= 0) minimumThrottleMk4 = minimumThrottleMk3;
            if (minimumThrottleMk5 <= 0) minimumThrottleMk5 = minimumThrottleMk4;
            if (minimumThrottleMk6 <= 0) minimumThrottleMk6 = minimumThrottleMk5;
            if (minimumThrottleMk7 <= 0) minimumThrottleMk7 = minimumThrottleMk6;
        }

        public virtual void UpdateEditorPowerOutput()
        {
            ongoing_thermal_power_generated = MaximumThermalPower;
            ongoing_charged_power_generated = MaximumChargedPower;
            ongoing_total_power_generated = ongoing_thermal_power_generated + ongoing_charged_power_generated;
        }

        public override void OnStart(StartState state)
        {
            hasStarted = true;

            if (startTime <= 0)
                startTime = Planetarium.GetUniversalTime();

            UpdateReactorCharacteristics();

            InitializeKerbalismEmitter();

            _heliumModuleGenerator = part.FindModulesImplementing<ModuleGenerator>()
                .SingleOrDefault(m => m.resHandler.outputResources.Count == 1 && m.resHandler.outputResources
                    .Count(r => r.name == ResourceSettings.Config.Helium4Gas) == 1);

            _hydrogenDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.HydrogenLqd);

            windowPosition = new Rect(windowPositionX, windowPositionY, 300, 100);
            _staticBreedRate = 1 / powerOutputMultiplier / breedDivider / GameConstants.tritiumBreedRate;

            var powerPercentageField = Fields[nameof(powerPercentage)];
            powerPercentageField.guiActive = showPowerPercentage;
            UI_FloatRange[] powerPercentageFloatRange = { powerPercentageField.uiControlFlight as UI_FloatRange, powerPercentageField.uiControlEditor as UI_FloatRange };
            powerPercentageFloatRange[0].minValue = minimumPowerPercentage;
            powerPercentageFloatRange[1].minValue = minimumPowerPercentage;

            if (!part.Resources.Contains(ResourceSettings.Config.ThermalPowerInMegawatt))
            {
                var node = new ConfigNode("RESOURCE");
                node.AddValue("name", ResourceSettings.Config.ThermalPowerInMegawatt);
                node.AddValue("maxAmount", PowerOutput);
                node.AddValue("amount", 0);
                part.AddResource(node);
            }

            // while in edit mode, listen to on attach/detach event
            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
            }

            resourcesToSupply = new[]
            {
                ResourceSettings.Config.ThermalPowerInMegawatt,
                ResourceSettings.Config.WasteHeatInMegawatt,
                ResourceSettings.Config.ChargedPowerInMegawatt,
                ResourceSettings.Config.ElectricPowerInMegawatt
            };

            _resourceBuffers = new ResourceBuffers();
            _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier * wasteHeatBufferMult, wasteHeatBufferMassMult, true));
            _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ThermalPowerInMegawatt, thermalPowerBufferMult));
            _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ChargedPowerInMegawatt, chargedPowerBufferMult));
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.Init(part);

            _windowId = new System.Random(part.GetInstanceID()).Next(int.MaxValue);
            base.OnStart(state);

            // configure reactor modes
            fuelModes = GetReactorFuelModes();
            SetDefaultFuelMode();
            UpdateFuelMode();

            var myAttachedEngine = part.FindModuleImplementing<ModuleEngines>();
            var myGenerator = part.FindModuleImplementing<FNGenerator>();

            if (state == StartState.Editor)
            {
                maximumThermalPowerEffective = MaximumThermalPower;
                coretempStr = CoreTemperature.ToString("0") + " K";

                var displayPartData = myGenerator == null;

                Fields[nameof(radius)].guiActiveEditor = displayPartData;
                Fields[nameof(connectedRecieversStr)].guiActiveEditor = displayPartData;
                Fields[nameof(currentMass)].guiActiveEditor = displayPartData;

                return;
            }

            InitializeSounds();

            if (!reactorInit)
            {
                if (startDisabled)
                {
                    Events[nameof(ActivateReactor)].guiActive = true;
                    Events[nameof(ActivateReactor)].active = true;
                    last_active_time = Planetarium.GetUniversalTime() - 4d * PluginSettings.Config.SecondsInDay;
                    IsEnabled = false;
                    startDisabled = false;
                    breedtritium = false;
                }
                else
                {
                    IsEnabled = true;
                    breedtritium = true;
                }
                reactorInit = true;
            }

            if (IsEnabled && _runningSound != null)
            {
                _previousReactorPowerRatio = reactor_power_ratio;

                if (vessel.isActiveVessel)
                    _runningSound.Play();
            }

            _tritiumDef = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.TritiumGas);
            _heliumDef = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.Helium4Gas);
            _lithium6Def = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.Lithium6);

            _tritiumDensity = (double)(decimal)_tritiumDef.density;
            _helium4Density = (double)(decimal)_heliumDef.density;
            _lithium6Density = (double)(decimal)_lithium6Def.density;

            _tritiumBreedingMassAdjustment = TritiumMolarMassRatio * _lithium6Density/ _tritiumDensity;
            _heliumBreedingMassAdjustment = HeliumMolarMassRatio * _lithium6Density / _helium4Density;

            if (IsEnabled && last_active_time > 0)
                DoPersistentResourceUpdate();

            if (!string.IsNullOrEmpty(animName))
                pulseAnimation = PluginHelper.SetUpAnimation(animName, part);
            if (!string.IsNullOrEmpty(loopingAnimationName))
                loopingAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == loopingAnimationName);
            if (!string.IsNullOrEmpty(startupAnimationName))
                startupAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == startupAnimationName);
            if (!string.IsNullOrEmpty(shutdownAnimationName))
                shutdownAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == shutdownAnimationName);

            _centrifugeHabitat = part.FindModuleImplementing<FNHabitat>();

            // only force activate if Enabled and not with a engine model
            if (IsEnabled && myAttachedEngine == null)
            {
                Debug.Log("[KSPI]: InterstellarReactor on " + part.name + " was Force Activated");
                part.force_activate();

                Fields[nameof(heatTransportationEfficiency)].guiActiveEditor = true;
            }
            else
                Debug.Log("[KSPI]: skipped calling Force on " + part.name);

            Fields[nameof(electricPowerPriority)].guiActive = showPowerPriority;
            Fields[nameof(reactorSurface)].guiActiveEditor = showSpecialisedUI;
            Fields[nameof(forcedMinimumThrottle)].guiActive = showForcedMinimumThrottle;
            Fields[nameof(forcedMinimumThrottle)].guiActiveEditor = showForcedMinimumThrottle;
        }

        private void InitializeSounds()
        {
            if (!string.IsNullOrWhiteSpace(soundRunningFilePath))
            {
                _runningSound = gameObject.AddComponent<AudioSource>();
                _runningSound.clip = GameDatabase.Instance.GetAudioClip(soundRunningFilePath);
                _runningSound.volume = 0;
                _runningSound.dopplerLevel = 0;
                _runningSound.spatialBlend = 1f;
                _runningSound.maxDistance = 100f;
                _runningSound.rolloffMode = AudioRolloffMode.Logarithmic;
                _runningSound.loop = true;
                _runningSound.Stop();
            }

            //soundTerminateFilePath
            if (!string.IsNullOrWhiteSpace(soundTerminateFilePath))
            {
                _terminateSound = gameObject.AddComponent<AudioSource>();
                _terminateSound.clip = GameDatabase.Instance.GetAudioClip(soundTerminateFilePath);
                _terminateSound.volume = 0;
                _terminateSound.dopplerLevel = 0;
                _terminateSound.spatialBlend = 1f;
                _terminateSound.maxDistance = 100f;
                _terminateSound.rolloffMode = AudioRolloffMode.Logarithmic;
                _terminateSound.loop = false;
                _terminateSound.Stop();
            }

            if (!string.IsNullOrWhiteSpace(soundTerminateFilePath))
            {
                _initiateSound = gameObject.AddComponent<AudioSource>();
                _initiateSound.clip = GameDatabase.Instance.GetAudioClip(soundInitiateFilePath);
                _initiateSound.volume = 0;
                _initiateSound.dopplerLevel = 0;
                _initiateSound.spatialBlend = 1f;
                _initiateSound.maxDistance = 100f;
                _initiateSound.rolloffMode = AudioRolloffMode.Logarithmic;
                _initiateSound.loop = false;
                _initiateSound.Stop();
            }
        }

        private void UpdateReactorCharacteristics()
        {
            DeterminePowerGenerationType();

            DetermineFuelModeTechLevel();

            DeterminePowerOutput();

            DetermineChargedParticleUtilizationRatio();

            DetermineFuelEfficiency();

            DetermineCoreTemperature();
        }

        private void DetermineFuelModeTechLevel()
        {
            _hasSpecificFuelModeTechs =
                !string.IsNullOrEmpty(fuelModeTechReqLevel2)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel3)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel4)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel5)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel6)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel7);

            fuelModeTechLevel = 0;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel2)) fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel3)) fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel4)) fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel5)) fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel6)) fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel7)) fuelModeTechLevel++;
        }

        private void DetermineCoreTemperature()
        {
            // if coreTemperature is missing, first look at legacy value
            if (coreTemperatureMk1 <= 0) coreTemperatureMk1 = ReactorTemp;
            if (coreTemperatureMk2 <= 0) coreTemperatureMk2 = upgradedReactorTemp;
            if (coreTemperatureMk3 <= 0) coreTemperatureMk3 = upgradedReactorTemp * powerUpgradeCoreTempMult;

            // prevent initial values
            if (coreTemperatureMk1 <= 0) coreTemperatureMk1 = 2500;
            if (coreTemperatureMk2 <= 0) coreTemperatureMk2 = coreTemperatureMk1;
            if (coreTemperatureMk3 <= 0) coreTemperatureMk3 = coreTemperatureMk2;
            if (coreTemperatureMk4 <= 0) coreTemperatureMk4 = coreTemperatureMk3;
            if (coreTemperatureMk5 <= 0) coreTemperatureMk5 = coreTemperatureMk4;
            if (coreTemperatureMk6 <= 0) coreTemperatureMk6 = coreTemperatureMk5;
            if (coreTemperatureMk7 <= 0) coreTemperatureMk7 = coreTemperatureMk6;
        }

        private void DetermineFuelEfficiency()
        {
            // if fuel efficiency is missing, try to use legacy value
            if (fuelEfficencyMk1 <= 0)
                fuelEfficencyMk1 = fuelEfficiency;

            // prevent any initial values
            if (fuelEfficencyMk1 <= 0) fuelEfficencyMk1 = 1;
            if (fuelEfficencyMk2 <= 0) fuelEfficencyMk2 = fuelEfficencyMk1;
            if (fuelEfficencyMk3 <= 0) fuelEfficencyMk3 = fuelEfficencyMk2;
            if (fuelEfficencyMk4 <= 0) fuelEfficencyMk4 = fuelEfficencyMk3;
            if (fuelEfficencyMk5 <= 0) fuelEfficencyMk5 = fuelEfficencyMk4;
            if (fuelEfficencyMk6 <= 0) fuelEfficencyMk6 = fuelEfficencyMk5;
            if (fuelEfficencyMk7 <= 0) fuelEfficencyMk7 = fuelEfficencyMk6;
        }

        private void DeterminePowerGenerationType()
        {
            // initialize tech requirement if missing
            if (string.IsNullOrEmpty(upgradeTechReqMk2))
                upgradeTechReqMk2 = upgradeTechReq;
            if (string.IsNullOrEmpty(upgradeTechReqMk3))
                upgradeTechReqMk3 = powerUpgradeTechReq;

            // determine number of upgrade techs
            currentGenerationType = 0;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk7)) currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk6)) currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk5)) currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk4)) currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk3)) currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk2)) currentGenerationType++;
        }

        /// <summary>
        /// Event handler called when part is attached to another part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI]: attach " + part.partInfo.title);
                foreach (var node in part.attachNodes)
                {
                    if (node.attachedPart == null) continue;

                    var generator = node.attachedPart.FindModuleImplementing<FNGenerator>();
                    if (generator != null)
                        generator.FindAndAttachToPowerSource();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Reactor.OnEditorAttach " + e.Message);
            }
        }

        private void OnEditorDetach()
        {
            try
            {
                Debug.Log("[KSPI]: detach " + part.partInfo.title);

                ConnectedChargedParticleElectricGenerator?.FindAndAttachToPowerSource();
                ConnectedThermalElectricGenerator?.FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Reactor.OnEditorDetach " + e.Message);
            }
        }

        /// <summary>
        /// is called in both VAB and in flight
        /// </summary>
        public virtual void Update()
        {
            DeterminePowerOutput();
            UpdateKerbalismEmitter();

            currentMass = part.mass;
            currentRawPowerOutput = RawPowerOutput;
            coretempStr = CoreTemperature.ToString("0") + " K";

            Events[nameof(DeactivateReactor)].guiActive = HighLogic.LoadedSceneIsFlight && showShutDownInFlight && IsEnabled;

            if (!HighLogic.LoadedSceneIsEditor) return;

            UpdateConnectedReceiversStr();
            reactorSurface = radius * radius;
        }

        protected void SwitchToNextFuelMode(int initialFuelMode)
        {
            if (fuelModes == null || fuelModes.Count == 0)
                return;

            fuel_mode++;
            if (fuel_mode >= fuelModes.Count)
                fuel_mode = 0;
            fuelmode_index = fuel_mode;

            stored_fuel_ratio = 1;
            CurrentFuelMode = fuelModes[fuel_mode];
            fuel_mode_name = CurrentFuelMode.ModeGUIName;

            UpdateFuelMode();

            if (!FullFuelRequirements() && fuel_mode != initialFuelMode)
                SwitchToNextFuelMode(initialFuelMode);

            isSwappingFuelMode = true;
        }

        protected void SwitchToPreviousFuelMode(int initialFuelMode)
        {
            if (fuelModes == null || fuelModes.Count == 0)
                return;

            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuelModes.Count - 1;
            fuelmode_index = fuel_mode;

            CurrentFuelMode = fuelModes[fuel_mode];
            fuel_mode_name = CurrentFuelMode.ModeGUIName;

            UpdateFuelMode();

            if (!FullFuelRequirements() && fuel_mode != initialFuelMode)
                SwitchToPreviousFuelMode(initialFuelMode);

            isSwappingFuelMode = true;
        }

        protected void UpdateFuelMode()
        {
            fuelModeStr = CurrentFuelMode != null ? CurrentFuelMode.DisplayName : "null";
        }

        public override void OnUpdate()
        {
            Events[nameof(StartBreedTritiumEvent)].active = canDisableTritiumBreeding && canBreedTritium && !breedtritium && IsFuelNeutronRich && IsEnabled;
            Events[nameof(StopBreedTritiumEvent)].active = canDisableTritiumBreeding && canBreedTritium && breedtritium && IsFuelNeutronRich && IsEnabled;
            UpdateFuelMode();

            if (IsEnabled && CurrentFuelMode != null)
            {
                if (CheatOptions.InfinitePropellant || stored_fuel_ratio > 0.99)
                    statusStr = Localizer.Format("#LOC_KSPIE_Reactor_status1", powerPercent.ToString("0.0000"));//"Active (" +  + "%)"
                else if (currentFuelVariant != null)
                    statusStr = currentFuelVariant.ReactorFuels.OrderBy(GetFuelAvailability).First().ResourceName + " " + Localizer.Format("#LOC_KSPIE_Reactor_status2");//"Deprived"
            }
            else
            {
                statusStr = powerPercent > 0
                    ? Localizer.Format("#LOC_KSPIE_Reactor_status3", powerPercent.ToString("0.0000"))
                    : Localizer.Format("#LOC_KSPIE_Reactor_status4");
            }
        }

        /// <summary>
        /// FixedUpdate is also called when not activated before OnFixedUpdate
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                DeterminePowerOutput();
                maximumThermalPowerEffective = MaximumThermalPower;
                UpdateEditorPowerOutput();
                return;
            }

            if (!enabled)
                base.OnFixedUpdate();

            if (_isFixedUpdatedCalled) return;

            _isFixedUpdatedCalled = true;
            UpdateCapacities();
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            double timeWarpFixedDeltaTime = (double)(decimal)TimeWarp.fixedDeltaTime;
            if (!IsEnabled && !IsStarted)
            {
                IsStarted = true;
                IsEnabled = true;
            }

            base.OnFixedUpdate();

            if (_heliumModuleGenerator != null)
            {
                _heliumModuleGenerator.resHandler.outputResources.Single().rate = 0;
                _heliumModuleGenerator.generatorIsActive = false;
            }

            StoreGeneratorRequests(timeWarpFixedDeltaTime);

            ongoingDecay = false;

            maximumPower = MaximumPower;

            if (IsEnabled && maximumPower > 0)
            {
                maxPowerToSupply = Math.Max(maximumPower * timeWarpFixedDeltaTime, 0);

                UpdateGeeforceModifier();

                if (hasOverheatEffects && !CheatOptions.IgnoreMaxTemperature)
                {
                    _averageOverheat.Enqueue(GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt));
                    if (_averageOverheat.Count > 10)
                        _averageOverheat.Dequeue();

                    var scaledOverheating = Math.Pow(Math.Max(GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt) - overheatThreshHold, 0) * overheatMultiplier, overheatExponent);

                    overheatModifier = Math.Min(Math.Max(1 - scaledOverheating, minOverheatModifier), 1);
                }
                else
                    overheatModifier = 1;

                currentFuelVariantsSorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(part, FuelEfficiency, maxPowerToSupply * geeForceModifier * overheatModifier, fuelUsePerMJMult);
                CurrentFuelVariant = currentFuelVariantsSorted.FirstOrDefault();

                stored_fuel_ratio = CheatOptions.InfinitePropellant ? 1 : currentFuelVariant != null ? Math.Min(currentFuelVariant.FuelRatio, 1) : 0;

                LookForAlternativeFuelTypes();

                UpdateCapacities();

                RequestedThermalThrottle = _connectedEngines.Any(m => m.RequiresThermalHeat) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.RequestedThrottle)) : 0;
                RequestedPlasmaThrottle = _connectedEngines.Any(m => m.RequiresPlasmaHeat) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.RequestedThrottle)) : 0;
                RequestedChargedThrottle = _connectedEngines.Any(m => m.RequiresChargedPower) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.RequestedThrottle)) : 0;

                var thermalThrottleRatio = _connectedEngines.Any(m => m.RequiresThermalHeat) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresThermalHeat).Sum(e => e.CurrentThrottle)) : 0;
                var plasmaThrottleRatio = _connectedEngines.Any(m => m.RequiresPlasmaHeat) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.CurrentThrottle)) : 0;
                var chargedThrottleRatio = _connectedEngines.Any(m => m.RequiresChargedPower) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresChargedPower).Max(e => e.CurrentThrottle)) : 0;

                var currentThermalPropulsionRatio = ThermalPropulsionEfficiency * thermalThrottleRatio;
                CurrentPlasmaPropulsionRatio = PlasmaPropulsionEfficiency * plasmaThrottleRatio;
                CurrentChargedPropulsionRatio = ChargedParticlePropulsionEfficiency * chargedThrottleRatio;

                var maximumThermalPropulsionRatio = ThermalPropulsionEfficiency * (thermalThrottleRatio > 0 ? 1 : 0);
                var maximumPlasmaPropulsionRatio = PlasmaPropulsionEfficiency * (plasmaThrottleRatio > 0 ? 1 : 0);
                var maximumChargedPropulsionRatio = ChargedParticlePropulsionEfficiency * (chargedThrottleRatio > 0 ? 1 : 0);

                var currentThermalGeneratorRatio = thermalEnergyEfficiency * storedGeneratorThermalEnergyRequestRatio;
                var currentPlasmaGeneratorRatio = plasmaEnergyEfficiency * storedGeneratorPlasmaEnergyRequestRatio;
                var currentChargedGeneratorRatio = chargedParticleEnergyEfficiency * storedGeneratorChargedEnergyRequestRatio;

                var maximumThermalGeneratorRatio = thermalEnergyEfficiency * (storedGeneratorThermalEnergyRequestRatio > 0 ? 1 : 0);
                var maximumPlasmaGeneratorRatio = plasmaEnergyEfficiency * (storedGeneratorPlasmaEnergyRequestRatio > 0 ? 1 : 0);
                var maximumChargedGeneratorRatio = chargedParticleEnergyEfficiency * (storedGeneratorChargedEnergyRequestRatio > 0 ? 1 : 0);

                _currentPropulsionRequestRatioSum = Math.Min(1, currentThermalPropulsionRatio + CurrentPlasmaPropulsionRatio + CurrentChargedPropulsionRatio);

                var currentThermalRequestRatio = Math.Min(1, currentThermalPropulsionRatio + CurrentPlasmaPropulsionRatio + currentThermalGeneratorRatio + currentPlasmaGeneratorRatio);
                var currentChargedRequestRatio = Math.Min(1, CurrentChargedPropulsionRatio + currentChargedGeneratorRatio);

                UpdateFuelRatio(Math.Max(currentThermalRequestRatio, currentChargedRequestRatio));

                _maximumThermalRequestRatio = Math.Min(1, maximumThermalPropulsionRatio + maximumPlasmaPropulsionRatio + maximumThermalGeneratorRatio + maximumPlasmaGeneratorRatio);
                _maximumChargedRequestRatio = Math.Min(1, maximumChargedPropulsionRatio + maximumChargedGeneratorRatio);

                var modifierAdjustForDeltaTime = Math.Min(1, timeWarpFixedDeltaTime * 0.05);

                var finalCurrentThermalRequestRatio = Math.Max(currentThermalRequestRatio,
                    (1 - GetResourceBarFraction(ResourceSettings.Config.ThermalPowerInMegawatt)) * modifierAdjustForDeltaTime * ThermalPowerRatio);
                var finalCurrentChargedRequestRatio = Math.Max(currentChargedRequestRatio,
                    (1 - GetResourceBarFraction(ResourceSettings.Config.ChargedPowerInMegawatt)) * modifierAdjustForDeltaTime * ChargedPowerRatio);

                var finalReactorRequestRatio =  Math.Max(vessel.ctrlState.mainThrottle * 0.001, Math.Max(finalCurrentThermalRequestRatio, finalCurrentChargedRequestRatio)) ;
                _maximumReactorRequestRatio = Math.Min(1, Math.Max(_maximumThermalRequestRatio, _maximumChargedRequestRatio));

                var powerAccessModifier = Math.Max(
                    Math.Max(
                        _connectedEngines.Any(m => !m.RequiresChargedPower) ? 1 : 0,
                        _connectedEngines.Any(m => m.RequiresChargedPower) ? 1 : 0),
                   Math.Max(
                        Math.Max(storedIsThermalEnergyGeneratorEfficiency > 0 ? 1 : 0, storedIsPlasmaEnergyGeneratorEfficiency > 0 ? 1 : 0),
                        Math.Max(storedIsChargedEnergyGeneratorEfficiency > 0 ? 1 : 0, minimumThrottle)
                   ));

                maximumChargedPower = MaximumChargedPower;
                maximumThermalPower = MaximumThermalPower;

                var maxStoredGeneratorEnergyRequestedRatio = Math.Max(Math.Max(storedGeneratorThermalEnergyRequestRatio, storedGeneratorPlasmaEnergyRequestRatio), storedGeneratorChargedEnergyRequestRatio);
                var maxThrottleRatio = Math.Max(Math.Max(thermalThrottleRatio, plasmaThrottleRatio), chargedThrottleRatio);

                var powerRequestRatio = Math.Max(maxThrottleRatio, maxStoredGeneratorEnergyRequestedRatio);

                var maxChargedToSupplyPerSecond = maximumChargedPower * stored_fuel_ratio * geeForceModifier * powerAccessModifier;
                var requestedChargedToSupplyPerSecond = maxChargedToSupplyPerSecond * powerRequestRatio * currentChargedRequestRatio;

                minThrottle = stored_fuel_ratio > 0 ? MinimumThrottle / stored_fuel_ratio : 1;

                var maxThermalToSupplyPerSecond = maximumThermalPower * stored_fuel_ratio * geeForceModifier * powerAccessModifier;
                var requestedThermalToSupplyPerSecond = maxThermalToSupplyPerSecond * powerRequestRatio * currentThermalRequestRatio;

                reactor_power_ratio = Math.Min(overheatModifier * finalReactorRequestRatio, PowerRatio);

                var lostChargeModifier = 1 - lostChargedPowerRatio;
                ongoing_charged_power_generated = ManagedProvidedPowerSupplyPerSecondMinimumRatio(
                    requestedChargedToSupplyPerSecond * lostChargeModifier,
                    maxChargedToSupplyPerSecond * lostChargeModifier,
                    reactor_power_ratio, ResourceSettings.Config.ChargedPowerInMegawatt);

                var lostThermalModifier = 1 - lostThermalPowerRatio;
                ongoing_thermal_power_generated = ManagedProvidedPowerSupplyPerSecondMinimumRatio(
                    requestedThermalToSupplyPerSecond * lostThermalModifier,
                    maxThermalToSupplyPerSecond * lostThermalModifier,
                    reactor_power_ratio, ResourceSettings.Config.ThermalPowerInMegawatt);

                UpdateEmbrittlement(Math.Max(thermalThrottleRatio, plasmaThrottleRatio), timeWarpFixedDeltaTime);

                ongoing_total_power_generated = ongoing_thermal_power_generated + ongoing_charged_power_generated;
                ongoing_consumption_rate = maximumPower > 0 ? ongoing_total_power_generated / maximumPower : 0;
                PluginHelper.SetAnimationRatio((float)Math.Pow(ongoing_consumption_rate, animExponent), pulseAnimation);
                powerPercent = 100 * ongoing_consumption_rate;

                // produce wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    //var maximumGeneratedWasteheat = _maximumReactorRequestRatio * maxThermalToSupplyPerSecond * lostThermalModifier;
                    var rawGeneratedWasteheat = ongoing_thermal_power_generated + (chargedPowerProducesWasteheat ? ongoing_charged_power_generated : 0);
                    var averagePrevious = _wasteheatBuffer.Count > 0 ?  Math.Min(_wasteheatBuffer.Min(), rawGeneratedWasteheat) : rawGeneratedWasteheat;
                    var delayedWasteheatRate = rawGeneratedWasteheat > averagePrevious ? Math.Min(averagePrevious, rawGeneratedWasteheat) : rawGeneratedWasteheat;
                    SupplyFnResourcePerSecondWithMax(delayedWasteheatRate, delayedWasteheatRate, ResourceSettings.Config.WasteHeatInMegawatt);

                    _wasteheatBuffer.Enqueue(rawGeneratedWasteheat);
                    if (_wasteheatBuffer.Count > 2)
                        _wasteheatBuffer.Dequeue();
                }

                ProcessReactorFuel(timeWarpFixedDeltaTime);

                BreedTritium(ongoing_thermal_power_generated, timeWarpFixedDeltaTime);

                if (Planetarium.GetUniversalTime() != 0)
                    last_active_time = Planetarium.GetUniversalTime();
            }
            else if (!IsEnabled && IsNuclear && MaximumPower > 0 && (Planetarium.GetUniversalTime() - last_active_time <= 3 * PluginSettings.Config.SecondsInDay))
            {
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                var powerFraction = 0.1 * Math.Exp(-(Planetarium.GetUniversalTime() - last_active_time) / PluginSettings.Config.SecondsInDay / 24.0 * 9.0);
                var powerToSupply = Math.Max(MaximumPower * powerFraction, 0);
                ongoing_thermal_power_generated = SupplyManagedFnResourcePerSecondWithMinimumRatio(powerToSupply, 1, ResourceSettings.Config.ThermalPowerInMegawatt);
                ongoing_total_power_generated = ongoing_thermal_power_generated;
                BreedTritium(ongoing_thermal_power_generated, timeWarpFixedDeltaTime);
                ongoing_consumption_rate = MaximumPower > 0 ? ongoing_thermal_power_generated / MaximumPower : 0;
                powerPercent = 100 * ongoing_consumption_rate;
                ongoingDecay = true;
            }
            else
            {
                currentFuelVariantsSorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(part, FuelEfficiency, NormalisedMaximumPower, fuelUsePerMJMult);
                CurrentFuelVariant = currentFuelVariantsSorted.FirstOrDefault();

                stored_fuel_ratio = CheatOptions.InfinitePropellant ? 1 : currentFuelVariant != null ? Math.Min(currentFuelVariant.FuelRatio, 1) : 0;

                ongoing_total_power_generated = 0;
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                powerPercent = 0;
            }

            UpdatePlayedSound();

            _previousReactorPowerRatio = reactor_power_ratio;

            if (IsEnabled) return;

            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ThermalPowerInMegawatt, 0);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ChargedPowerInMegawatt, 0);
            _resourceBuffers.UpdateBuffers();
        }

        private void UpdateFuelRatio(double requestRatio)
        {
            var trueVariant = CurrentFuelMode
                .GetVariantsOrderedByFuelRatio(part, FuelEfficiency, maxPowerToSupply, fuelUsePerMJMult, false)
                .FirstOrDefault();

            fuel_ratio = CheatOptions.InfinitePropellant ? 1 : trueVariant != null ? Math.Min(trueVariant.FuelRatio, 1) : 0;
            if (fuel_ratio < 0.99999 && requestRatio > 0)
            {
                if (messagedRanOutOfFuel) return;

                messagedRanOutOfFuel = true;
                var message = Localizer.Format("#LOC_KSPIE_Reactor_ranOutOfFuelFor") + " " + CurrentFuelMode.ModeGUIName;
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
                TimeWarp.SetRate(0, true);
            }
            else
                messagedRanOutOfFuel = false;
        }

        private void ProcessReactorFuel(double timeWarpFixedDeltaTime)
        {
            var reactorFuelProcess = Kerbalism.IsLoaded ? part.Resources["_" + currentFuelVariant.Name] : null;
            var electricPowerGenerator = Kerbalism.IsLoaded ? part.Resources["_" + currentFuelVariant.Name + "_EC"] : null;

            if (Kerbalism.IsLoaded)
            {
                // update Kerbalism EC power generator
                if (electricPowerGenerator != null)
                {
                    electricPowerGenerator.maxAmount = _auxiliaryPowerAvailable;
                    electricPowerGenerator.amount = _auxiliaryPowerAvailable;
                }

                if (reactorFuelProcess != null)
                {
                    // disable fuel consumption when InfinitePropellant active
                    if (CheatOptions.InfinitePropellant)
                    {
                        //reactorFuelProcess.ReliablityEvent(0);
                        reactorFuelProcess.maxAmount = 0;
                        reactorFuelProcess.amount = 0;
                        return;
                    }
                }
            }

            // consume fuel
            _consumedFuelTotalFixed = 0;
            foreach (var reactorFuel in currentFuelVariant.ReactorFuels)
            {
                _consumedFuelTotalFixed += ConsumeReactorFuel(reactorFuel, ongoing_total_power_generated / geeForceModifier, timeWarpFixedDeltaTime, reactorFuelProcess);
            }

            // refresh production list
            _reactorProduction.Clear();

            // produce reactor products
            foreach (var product in currentFuelVariant.ReactorProducts)
            {
                var massProduced = ProduceReactorProduct(product, ongoing_total_power_generated / geeForceModifier, timeWarpFixedDeltaTime, reactorFuelProcess != null);
                if (!simulateConsumption && product.IsPropellant)
                    _reactorProduction.Add(new ReactorProduction {fuelMode = product, mass = massProduced});
            }
        }

        private void UpdatePlayedSound()
        {
            var scaledPitchRatio = Math.Pow(reactor_power_ratio, soundRunningPitchExp * soundRunningPitchMult);
            var scaledVolumeRatio = Math.Pow(reactor_power_ratio, soundRunningVolumeExp) * soundRunningVolumeMult;

            var pitch = soundRunningPitchMin * (1 - scaledPitchRatio) + scaledPitchRatio;
            var volume = reactor_power_ratio <= 0 ? 0 : GameSettings.SHIP_VOLUME * ( soundRunningVolumeMin * (1 - scaledVolumeRatio) + scaledVolumeRatio);

            if (_runningSound != null)
            {
                _runningSound.pitch = (float)pitch;
                _runningSound.volume = (float)volume;
            }

            if (_initiateSound != null)
            {
                _initiateSound.pitch = (float)pitch;
                _initiateSound.volume = (float)volume;
            }

            if (_previousReactorPowerRatio > 0.01 && reactor_power_ratio <= 0.01)
            {
                if (_initiateSound != null && _initiateSound.isPlaying)
                    _initiateSound.Stop();
                if (_runningSound != null && _runningSound.isPlaying)
                    _runningSound.Stop();

                if (vessel.isActiveVessel && _terminateSound != null && !_terminateSound.isPlaying)
                {
                    _terminateSound.PlayOneShot(_terminateSound.clip);
                    _terminateSound.volume = GameSettings.SHIP_VOLUME;
                }
            }
            else if (_previousReactorPowerRatio <= 0.01 && reactor_power_ratio > 0.01)
            {
                if (_runningSound != null && _runningSound.isPlaying)
                    _runningSound.Stop();
                if (_terminateSound != null && _terminateSound.isPlaying)
                    _terminateSound.Stop();

                if (vessel.isActiveVessel)
                {
                    if (_initiateSound != null && !_initiateSound.isPlaying)
                    {
                        _initiateSound.PlayOneShot(_initiateSound.clip);
                        _initiateSound.volume = GameSettings.SHIP_VOLUME;
                    }
                    else if (_runningSound != null)
                        _runningSound.Play();
                }
            }
            else if (_previousReactorPowerRatio > 0.01 && reactor_power_ratio > 0.01 && _runningSound != null)
            {
                if (vessel.isActiveVessel && !_runningSound.isPlaying)
                {
                    if ((_initiateSound == null || (_initiateSound != null && !_initiateSound.isPlaying)) &&
                        (_terminateSound == null || (_terminateSound != null && !_terminateSound.isPlaying)))
                        _runningSound.Play();
                }
                else if (!vessel.isActiveVessel && _runningSound.isPlaying)
                {
                    _runningSound.Stop();
                }
            }
        }

        private void UpdateGeeforceModifier()
        {
            if (hasBuoyancyEffects && !CheatOptions.UnbreakableJoints)
            {
                _averageGeeforce.Enqueue(vessel.geeForce);
                if (_averageGeeforce.Count > 10)
                    _averageGeeforce.Dequeue();

                currentGeeForce = vessel.geeForce > 0 && _averageGeeforce.Any() ? _averageGeeforce.Average() : 0;

                if (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ESCAPING)
                {
                    var engines = vessel.FindPartModulesImplementing<ModuleEngines>();
                    if (engines.Any())
                    {
                        var totalThrust = engines.Sum(m => m.realIsp * m.requestedMassFlow * PhysicsGlobals.GravitationalAcceleration * Vector3d.Dot(m.part.transform.up, vessel.transform.up));
                        currentGeeForce = Math.Max(currentGeeForce, totalThrust / vessel.totalMass / PhysicsGlobals.GravitationalAcceleration);
                    }
                }

                var geeforce = double.IsNaN(currentGeeForce) || double.IsInfinity(currentGeeForce) ? 0 : currentGeeForce;

                var scaledGeeforce = Math.Pow(Math.Max(geeforce - geeForceThreshHold, 0) * geeForceMultiplier, geeForceExponent);

                geeForceModifier = Math.Min(Math.Max(1 - scaledGeeforce, minGeeForceModifier), 1);
            }
            else
                geeForceModifier = 1;
        }

        private void UpdateEmbrittlement(double thermalPlasmaRatio, double timeWarpFixedDeltaTime)
        {
            var hasActiveNeutronAbsorption = _connectedEngines.All(m => m.PropellantAbsorbsNeutrons) && thermalPlasmaRatio > 0;
            var lithiumEmbrittlementModifer = 1 - Math.Max(lithium_modifier * 0.9, hasActiveNeutronAbsorption ? 0.9 : 0);

            if (!CheatOptions.UnbreakableJoints && CurrentFuelMode.NeutronsRatio > 0 && CurrentFuelMode.NeutronsRatio > 0)
                neutronEmbrittlementDamage += 5 * lithiumEmbrittlementModifer * ongoing_total_power_generated * timeWarpFixedDeltaTime * CurrentFuelMode.NeutronsRatio / neutronEmbrittlementDivider;
        }

        private void LookForAlternativeFuelTypes()
        {
            var originalFuelMode = CurrentFuelMode;
            var originalFuelRatio = stored_fuel_ratio;

            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType1);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType2);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType3);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType4);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType5);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType6);

            if (stored_fuel_ratio < 0.99)
            {
                CurrentFuelMode = originalFuelMode;
                stored_fuel_ratio = originalFuelRatio;
            }
        }

        private void SwitchToAlternativeFuelWhenAvailable(string alternativeFuelTypeName)
        {
            if (stored_fuel_ratio >= 0.99)
                return;

            if (string.IsNullOrEmpty(alternativeFuelTypeName))
                return;

            // look for most advanced version
            var alternativeFuelType = fuelModes.LastOrDefault(m => m.ModeGUIName.Contains(alternativeFuelTypeName));
            if (alternativeFuelType == null)
            {
                Debug.LogWarning("[KSPI]: failed to find fuelType " + alternativeFuelTypeName);
                return;
            }

            Debug.Log("[KSPI]: searching fuelModes for alternative for fuel type " + alternativeFuelTypeName);
            var alternativeFuelVariantsSorted = alternativeFuelType.GetVariantsOrderedByFuelRatio(part, FuelEfficiency, maxPowerToSupply, fuelUsePerMJMult);

            if (alternativeFuelVariantsSorted == null)
                return;

            var alternativeFuelVariant = alternativeFuelVariantsSorted.FirstOrDefault();
            if (alternativeFuelVariant == null)
            {
                Debug.LogError("[KSPI]: failed to find any variant for fuelType " + alternativeFuelTypeName);
                return;
            }

            if (alternativeFuelVariant.FuelRatio < 0.99)
            {
                Debug.LogWarning("[KSPI]: failed to find sufficient resource for " + alternativeFuelVariant.Name);
                return;
            }

            var message = Localizer.Format("#LOC_KSPIE_Reactor_switchingToAlternativeFuelMode") + " " + alternativeFuelType.ModeGUIName;
            Debug.Log("[KSPI]: " + message);
            ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);

            CurrentFuelMode = alternativeFuelType;
            stored_fuel_ratio = CurrentFuelVariant.FuelRatio;
        }

        private void StoreGeneratorRequests(double timeWarpFixedDeltaTime)
        {
            storedIsThermalEnergyGeneratorEfficiency = _currentIsThermalEnergyGeneratorEfficiency;
            storedIsPlasmaEnergyGeneratorEfficiency = _currentIsPlasmaEnergyGeneratorEfficiency;
            storedIsChargedEnergyGeneratorEfficiency = _currentIsChargedEnergyGeneratorEfficiency;

            _currentIsThermalEnergyGeneratorEfficiency = 0;
            _currentIsPlasmaEnergyGeneratorEfficiency = 0;
            _currentIsChargedEnergyGeneratorEfficiency = 0;

            //var previousStoredRatio = Math.Max(Math.Max(storedGeneratorThermalEnergyRequestRatio, storedGeneratorPlasmaEnergyRequestRatio), storedGeneratorChargedEnergyRequestRatio);

            //storedGeneratorThermalEnergyRequestRatio = Math.Max(storedGeneratorThermalEnergyRequestRatio, previousStoredRatio);
            //storedGeneratorPlasmaEnergyRequestRatio = Math.Max(storedGeneratorPlasmaEnergyRequestRatio, previousStoredRatio);
            //storedGeneratorChargedEnergyRequestRatio = Math.Max(storedGeneratorChargedEnergyRequestRatio, previousStoredRatio);

            var requiredMinimumThrottle = Math.Max(MinimumThrottle, ForcedMinimumThrottleRatio);

            _currentGeneratorThermalEnergyRequestRatio = Math.Max(_currentGeneratorThermalEnergyRequestRatio, requiredMinimumThrottle);
            _currentGeneratorPlasmaEnergyRequestRatio = Math.Max(_currentGeneratorPlasmaEnergyRequestRatio, requiredMinimumThrottle);
            _currentGeneratorChargedEnergyRequestRatio = Math.Max(_currentGeneratorChargedEnergyRequestRatio, requiredMinimumThrottle);

            var thermalDifference = Math.Abs(storedGeneratorThermalEnergyRequestRatio - _currentGeneratorThermalEnergyRequestRatio);
            var plasmaDifference = Math.Abs(storedGeneratorPlasmaEnergyRequestRatio - _currentGeneratorPlasmaEnergyRequestRatio);
            var chargedDifference = Math.Abs(storedGeneratorChargedEnergyRequestRatio - _currentGeneratorChargedEnergyRequestRatio);

            var thermalThrottleIsGrowing = _currentGeneratorThermalEnergyRequestRatio > storedGeneratorThermalEnergyRequestRatio;
            var plasmaThrottleIsGrowing = _currentGeneratorPlasmaEnergyRequestRatio > storedGeneratorPlasmaEnergyRequestRatio;
            var chargedThrottleIsGrowing = _currentGeneratorChargedEnergyRequestRatio > storedGeneratorChargedEnergyRequestRatio;

            var fixedReactorSpeedMultiplier = ReactorSpeedMult * timeWarpFixedDeltaTime;
            var minimumAcceleration = timeWarpFixedDeltaTime * timeWarpFixedDeltaTime;

            var thermalAccelerationReductionRatio = thermalThrottleIsGrowing
                ? storedGeneratorThermalEnergyRequestRatio <= 0.5 ? 1 : minimumAcceleration + (1 - storedGeneratorThermalEnergyRequestRatio) / 0.5
                : storedGeneratorThermalEnergyRequestRatio <= 0.5 ? minimumAcceleration + storedGeneratorThermalEnergyRequestRatio / 0.5 : 1;

            var plasmaAccelerationReductionRatio = plasmaThrottleIsGrowing
                ? storedGeneratorPlasmaEnergyRequestRatio <= 0.5 ? 1 : minimumAcceleration + (1 - storedGeneratorPlasmaEnergyRequestRatio) / 0.5
                : storedGeneratorPlasmaEnergyRequestRatio <= 0.5 ? minimumAcceleration + storedGeneratorPlasmaEnergyRequestRatio / 0.5 : 1;

            var chargedAccelerationReductionRatio = chargedThrottleIsGrowing
                ? storedGeneratorChargedEnergyRequestRatio <= 0.5 ? 1 : minimumAcceleration + (1 - storedGeneratorChargedEnergyRequestRatio) / 0.5
                : storedGeneratorChargedEnergyRequestRatio <= 0.5 ? minimumAcceleration + storedGeneratorChargedEnergyRequestRatio / 0.5 : 1;

            var fixedThermalSpeed = fixedReactorSpeedMultiplier > 0 ? Math.Min(thermalDifference, fixedReactorSpeedMultiplier) * thermalAccelerationReductionRatio : thermalDifference;
            var fixedPlasmaSpeed = fixedReactorSpeedMultiplier > 0 ? Math.Min(plasmaDifference, fixedReactorSpeedMultiplier) * plasmaAccelerationReductionRatio : plasmaDifference;
            var fixedChargedSpeed = fixedReactorSpeedMultiplier > 0 ? Math.Min(chargedDifference, fixedReactorSpeedMultiplier) * chargedAccelerationReductionRatio : chargedDifference;

            var thermalChangeFraction = thermalThrottleIsGrowing ? fixedThermalSpeed : -fixedThermalSpeed;
            var plasmaChangeFraction = plasmaThrottleIsGrowing ? fixedPlasmaSpeed : -fixedPlasmaSpeed;
            var chargedChangeFraction = chargedThrottleIsGrowing ? fixedChargedSpeed : -fixedChargedSpeed;

            storedGeneratorThermalEnergyRequestRatio = Math.Max(0, Math.Min(1, storedGeneratorThermalEnergyRequestRatio + thermalChangeFraction));
            storedGeneratorPlasmaEnergyRequestRatio = Math.Max(0, Math.Min(1, storedGeneratorPlasmaEnergyRequestRatio + plasmaChangeFraction));
            storedGeneratorChargedEnergyRequestRatio = Math.Max(0, Math.Min(1, storedGeneratorChargedEnergyRequestRatio + chargedChangeFraction));

            //_currentGeneratorThermalEnergyRequestRatio = 0;
            //_currentGeneratorPlasmaEnergyRequestRatio = 0;
            //_currentGeneratorChargedEnergyRequestRatio = 0;
        }

        private void UpdateCapacities()
        {
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ThermalPowerInMegawatt, MaximumThermalPower);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ChargedPowerInMegawatt, MaximumChargedPower);
            _resourceBuffers.UpdateBuffers();
        }

        protected double GetFuelRatio(ReactorFuel reactorFuel, double efficiency, double megajoules)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            var fuelUseForPower = reactorFuel.GetFuelUseForPower(efficiency, megajoules, fuelUsePerMJMult);

            return fuelUseForPower > 0 ? GetFuelAvailability(reactorFuel) / fuelUseForPower : 0;
        }

        private void BreedTritium(double neutronPowerReceivedEachSecond, double fixedDeltaTime, bool immediate = false)
        {
            _lithiumConsumedPerSecond = 0;
            _tritiumProducedPerSecond = 0;
            _heliumProducedPerSecond = 0;

            // verify if there is any lithium6 storage present
            var partResourceLithium6 = part.Resources[ResourceSettings.Config.Lithium6];
            if (partResourceLithium6 == null)
            {
                totalAmountLithium = 0;
                totalMaxAmountLithium = 0;
                return;
            }

            totalAmountLithium = partResourceLithium6.amount;
            totalMaxAmountLithium = partResourceLithium6.maxAmount;

            if (breedtritium == false || fixedDeltaTime <= 0 || totalAmountLithium.IsInfinityOrNaNorZero() || totalMaxAmountLithium.IsInfinityOrNaNorZero())
                return;

            lithiumNeutronAbsorption = CheatOptions.UnbreakableJoints ? 1 : Math.Max(0.01, Math.Sqrt(totalAmountLithium / totalMaxAmountLithium) - 0.0001);

            // calculate current maximum lithium consumption
            var breedRate = CurrentFuelMode.TritiumBreedModifier * CurrentFuelMode.NeutronsRatio * _staticBreedRate * neutronPowerReceivedEachSecond * lithiumNeutronAbsorption;
            var lithiumRate = breedRate / _lithium6Density;

            // get spare room tritium
            var spareRoomTritiumAmount = part.GetResourceSpareCapacity(_tritiumDef);

            // limit lithium consumption to maximum tritium storage
            var maximumTritiumProduction = lithiumRate * _tritiumBreedingMassAdjustment;
            var maximumLithiumConsumptionRatio = maximumTritiumProduction > 0 ? Math.Min(maximumTritiumProduction, spareRoomTritiumAmount) / maximumTritiumProduction : 0;
            var lithiumRequest = lithiumRate * maximumLithiumConsumptionRatio;

            // verify the amount of lithium we can actually consume
            _lithiumConsumedPerSecond = CheatOptions.InfinitePropellant
                ? lithiumRequest
                : part.RequestResource(_lithium6Def.id, lithiumRequest * fixedDeltaTime, ResourceFlowMode.STACK_PRIORITY_SEARCH, true) / fixedDeltaTime;

            // calculate products
            _tritiumProducedPerSecond = _lithiumConsumedPerSecond * _tritiumBreedingMassAdjustment;
            _heliumProducedPerSecond = _lithiumConsumedPerSecond * _heliumBreedingMassAdjustment;

            PartResource lithiumBreedControlResource = Kerbalism.IsLoaded ? part.Resources["_Lithium6Breeder"] : null;

            if (Kerbalism.IsLoaded && lithiumBreedControlResource != null)
            {
                // configure Kerbalism to consume lithium and produce helium and tritium gas
                lithiumBreedControlResource.maxAmount = lithiumRequest;
                lithiumBreedControlResource.amount = lithiumRequest;
            }
            else if (!CheatOptions.InfinitePropellant && _lithiumConsumedPerSecond > 0)
            {
                // consume the lithium
                part.RequestResource(_lithium6Def.id, _lithiumConsumedPerSecond * fixedDeltaTime, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                part.RequestResource(_tritiumDef.id, -_tritiumProducedPerSecond * fixedDeltaTime, ResourceFlowMode.STACK_PRIORITY_SEARCH);

                if (!immediate && _heliumModuleGenerator != null)
                {
                    _heliumModuleGenerator.resHandler.outputResources.Single().rate += _heliumProducedPerSecond;
                    _heliumModuleGenerator.generatorIsActive = true;
                }
                else
                    part.RequestResource(_heliumDef.id, -_heliumProducedPerSecond * fixedDeltaTime, ResourceFlowMode.STACK_PRIORITY_SEARCH);
            }
        }

        public virtual double GetCoreTempAtRadiatorTemp(double radTemp)
        {
            return CoreTemperature;
        }

        public virtual double GetThermalPowerAtTemp(double temp)
        {
            return MaximumPower;
        }

        public virtual bool shouldScaleDownJetISP()
        {
            return false;
        }

        public void EnableIfPossible()
        {
            if (!IsNuclear && !IsEnabled)
                IsEnabled = true;
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();

            const string headerSize = "<size=11>";
            const string headerColor = "<color=#7fdfffff>";

            UpdateReactorCharacteristics();
            if (showEngineConnectionInfo)
            {
                sb.Append(headerSize + headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_propulsion")).AppendLine(":</color><size=10>");
                sb.Append(Localizer.Format("#LOC_KSPIE_Reactor_thermalNozzle")).Append(": ");
                UtilizationInfo(sb, thermalPropulsionEfficiency);
                sb.AppendLine().Append(Localizer.Format("#LOC_KSPIE_Reactor_plasmaNozzle")).Append(": ");
                UtilizationInfo(sb, plasmaPropulsionEfficiency);
                sb.AppendLine().Append(Localizer.Format("#LOC_KSPIE_Reactor_magneticNozzle")).Append(": ");
                UtilizationInfo(sb, chargedParticlePropulsionEfficiency);
                sb.AppendLine().AppendLine("</size>");
            }

            if (showPowerGeneratorConnectionInfo)
            {
                sb.Append(headerSize + headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_powerGeneration")).AppendLine(":</color><size=10>");
                sb.Append(Localizer.Format("#LOC_KSPIE_Reactor_thermalGenerator")).Append(": ");
                UtilizationInfo(sb, thermalEnergyEfficiency);
                sb.AppendLine().Append(Localizer.Format("#LOC_KSPIE_Reactor_MHDGenerator")).Append(": ");
                UtilizationInfo(sb, plasmaEnergyEfficiency);
                sb.AppendLine().Append(Localizer.Format("#LOC_KSPIE_Reactor_chargedParticleGenerator")).Append(": ");
                UtilizationInfo(sb, chargedParticleEnergyEfficiency);
                sb.AppendLine().AppendLine("</size>");
            }

            if (!string.IsNullOrEmpty(upgradeTechReqMk2))
            {
                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_powerUpgradeTechnologies")).AppendLine(":</color><size=10>");
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk2)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk3)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk4)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk5)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk6)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk7)));
                sb.AppendLine("</size>");
            }

            if (thermalEnergyEfficiency > 0 || plasmaEnergyEfficiency > 0 || chargedParticleEnergyEfficiency > 0)
            {

                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_ReactorPower")).AppendLine(":</color><size=10>");
                sb.Append("Mk1: ").AppendLine(PluginHelper.GetFormattedPowerString(powerOutputMk1));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.Append("Mk2: ").AppendLine(PluginHelper.GetFormattedPowerString(powerOutputMk2));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.Append("Mk3: ").AppendLine(PluginHelper.GetFormattedPowerString(powerOutputMk3));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.Append("Mk4: ").AppendLine(PluginHelper.GetFormattedPowerString(powerOutputMk4));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.Append("Mk5: ").AppendLine(PluginHelper.GetFormattedPowerString(powerOutputMk5));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.Append("Mk6: ").AppendLine(PluginHelper.GetFormattedPowerString(powerOutputMk6));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.Append("Mk7: ").AppendLine(PluginHelper.GetFormattedPowerString(powerOutputMk7));
                sb.AppendLine("</size>");
            }

            if (_hasSpecificFuelModeTechs)
            {
                sb.AppendLine(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_fuelModeUpgradeTechnologies")).AppendLine(":</color><size=10>");
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel2) && fuelModeTechReqLevel2 != "none") sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel2)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel3) && fuelModeTechReqLevel3 != "none") sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel3)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel4) && fuelModeTechReqLevel4 != "none") sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel4)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel5) && fuelModeTechReqLevel5 != "none") sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel5)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel6) && fuelModeTechReqLevel6 != "none") sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel6)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel7) && fuelModeTechReqLevel7 != "none") sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel7)));
                sb.AppendLine("</size>");
            }

            var maximumFuelTechLevel = GetMaximumFuelTechLevel();
            var fuelGroups = GetFuelGroups(maximumFuelTechLevel);

            if (fuelGroups.Count > 1)
            {
                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_getInfoFuelModes")).AppendLine(":</color><size=10>");
                foreach (var group in fuelGroups)
                {
                    if (!allModesAvailableAtStart)
                        sb.Append("Mk").Append(Math.Max(0, 1 + group.TechLevel - reactorModeTechBonus)).Append(": ");

                    sb.AppendLine(Localizer.Format(group.DisplayName));
                }
                sb.AppendLine("</size>");
            }

            if (plasmaPropulsionEfficiency > 0)
            {
                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_plasmaNozzlePerformance")).AppendLine(":</color><size=10>");
                sb.Append("Mk1: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk1, powerOutputMk1));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.Append("Mk2: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk2, powerOutputMk2));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.Append("Mk3: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk3, powerOutputMk3));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.Append("Mk4: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk4, powerOutputMk4));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.Append("Mk5: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk5, powerOutputMk5));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.Append("Mk6: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk6, powerOutputMk6));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.Append("Mk7: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk7, powerOutputMk7));
                sb.AppendLine("</size>");
            }

            if (thermalPropulsionEfficiency > 0)
            {
                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_thermalNozzlePerformance")).AppendLine(":</color><size=10>");
                sb.Append("Mk1: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk1, powerOutputMk1));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.Append("Mk2: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk2, powerOutputMk2));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.Append("Mk3: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk3, powerOutputMk3));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.Append("Mk4: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk4, powerOutputMk4));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.Append("Mk5: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk5, powerOutputMk5));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.Append("Mk6: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk6, powerOutputMk6));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.Append("Mk7: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk7, powerOutputMk7));
                sb.AppendLine("</size>");
            }

            return sb.ToStringAndRelease();
        }

        private List<ReactorFuelType> GetFuelGroups(int maximumFuelTechLevel)
        {
            var allFuelModes = GameDatabase.Instance
                .GetConfigNodes("REACTOR_FUEL_MODE")
                .Select(node => new ReactorFuelMode(node)).ToList();

            var compatibleFuelModes = allFuelModes.Where(fm =>
                fm.AllFuelResourcesDefinitionsAvailable
                && fm.AllProductResourcesDefinitionsAvailable
                && (fm.SupportedReactorTypes & ReactorType) == ReactorType
                && maximumFuelTechLevel >= fm.TechLevel
                && FusionEnergyGainFactor >= fm.MinimumQ
                && (fm.Aneutronic || canUseNeutronicFuels)
                && maxGammaRayPower >= fm.GammaRayEnergy).ToList();

            var groups = compatibleFuelModes
                .GroupBy(mode => mode.ModeGuiName)
                .Select(group => new ReactorFuelType(group))
                .OrderBy(m => m.TechLevel).ToList();

            return groups;
        }

        private int GetMaximumFuelTechLevel()
        {
            int techLevels = 0;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel2) && fuelModeTechReqLevel2 != "none") techLevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel3) && fuelModeTechReqLevel3 != "none") techLevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel4) && fuelModeTechReqLevel4 != "none") techLevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel5) && fuelModeTechReqLevel5 != "none") techLevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel6) && fuelModeTechReqLevel6 != "none") techLevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel7) && fuelModeTechReqLevel7 != "none") techLevels++;
            var maximumFuelTechLevel = techLevels + reactorModeTechBonus;
            return maximumFuelTechLevel;
        }

        private string ThermalNozzlePerformance(double temperature, double powerInMj)
        {
            var isp = Math.Min(Math.Sqrt(temperature) * 21, maxThermalNozzleIsp);

            var exhaustVelocity = isp * PhysicsGlobals.GravitationalAcceleration;

            var thrust = powerInMj * 2000.0 * thermalPropulsionEfficiency / (exhaustVelocity * powerOutputMultiplier);

            return thrust.ToString("F1") + "kN @ " + isp.ToString("F0") + "s";
        }

        private string PlasmaNozzlePerformance(double temperature, double powerInMj)
        {
            var isp = Math.Sqrt(temperature) * 21;

            var exhaustVelocity = isp * PhysicsGlobals.GravitationalAcceleration;

            var thrust = powerInMj * 2000.0 * plasmaPropulsionEfficiency / (exhaustVelocity * powerOutputMultiplier);

            return thrust.ToString("F1") + "kN @ " + isp.ToString("F0") + "s";
        }

        private void UtilizationInfo(StringBuilder sb, double value)
        {
            sb.Append(RUIutils.GetYesNoUIString(value > 0.0));
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value > 0.0 && value != 1.0)
            {
                sb.Append(" (<color=orange>").Append((value * 100.0).ToString("F0")).Append("%</color>)");
            }
        }

        protected void DoPersistentResourceUpdate()
        {
            if (CheatOptions.InfinitePropellant)
                return;

            // calculate delta time since last processing
            double deltaTimeDiff = Math.Max(Planetarium.GetUniversalTime() - last_active_time, 0);

            last_active_time = Planetarium.GetUniversalTime();

            // determine available variants
            var persistentFuelVariantsSorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(part, FuelEfficiency, deltaTimeDiff * ongoing_total_power_generated, fuelUsePerMJMult);

            CurrentFuelVariant = persistentFuelVariantsSorted.FirstOrDefault();
            if (currentFuelVariant == null)
                return;

            // skip consumption when Kerbalism is loaded and process control resource is found
            var reactorFuelProcess = Kerbalism.IsLoaded ? part.Resources["_" + currentFuelVariant.Name] : null;
            if (reactorFuelProcess == null)
            {
                // consume fuel
                foreach (var fuel in persistentFuelVariantsSorted.First().ReactorFuels)
                {
                    ConsumeReactorFuel(fuel, ongoing_total_power_generated, deltaTimeDiff);
                }

                // produce reactor products
                foreach (var product in persistentFuelVariantsSorted.First().ReactorProducts)
                {
                    ProduceReactorProduct(product, ongoing_total_power_generated, deltaTimeDiff);
                }
            }

            // breed tritium
            BreedTritium(ongoing_total_power_generated * ThermalPowerRatio, deltaTimeDiff, true);
        }

        protected bool ReactorIsOverheating()
        {
            if (!CheatOptions.IgnoreMaxTemperature && GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt) >= emergencyPowerShutdownFraction && canShutdown)
            {
                _deactivateTimer++;
                if (_deactivateTimer > 3)
                    return true;
            }
            else
                _deactivateTimer = 0;

            return false;
        }

        protected List<ReactorFuelType> GetReactorFuelModes()
        {
            var fuelModeConfigs = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");

            var allFuelModesForReactorType = fuelModeConfigs
                .Select(node => new ReactorFuelMode(node)).Where(fm =>
                (fm.SupportedReactorTypes & ReactorType) == ReactorType).ToList();

            var filteredFuelModes =
                allFuelModesForReactorType.Where(fm =>
                PluginHelper.HasTechRequirementOrEmpty(fm.TechRequirement)
                    && (fm.Aneutronic || canUseNeutronicFuels)
                    && fm.MinimumQ <= FusionEnergyGainFactor
                    && fm.AllFuelResourcesDefinitionsAvailable
                    && fm.AllProductResourcesDefinitionsAvailable
                    && fm.TechLevel <= ReactorFuelModeTechLevel
                    && fm.GammaRayEnergy <= maxGammaRayPower
                    && fm.ChargedPowerRatio >= minChargedParticleRatio
                    && fm.ChargedPowerRatio <= maxChargedParticleRatio
                    && fm.NeutronsRatio <= maxNeutronsRatio
                    && fm.NeutronsRatio >= minNeutronsRatio
                    ).ToList();

            for (var i = 0; i < filteredFuelModes.Count; i++)
            {
                filteredFuelModes[i].Position = i;
            }

            Debug.Log("[KSPI]: found " + filteredFuelModes.Count + " valid fuel types");

            var groups = filteredFuelModes.GroupBy(mode => mode.ModeGuiName)
                .Select(group => new ReactorFuelType(group)).ToList();

            Debug.Log("[KSPI]: grouped them into " + groups.Count + " valid fuel modes");

            return groups;
        }

        protected bool FuelRequiresLab(bool requiresLab)
        {
            var isConnectedToLab = part.IsConnectedToModule("NuclearRefineryController", 10);

            return !requiresLab || (isConnectedToLab && canBeCombinedWithLab);
        }

        public virtual void SetDefaultFuelMode()
        {
            if (fuelModes == null)
            {
                Debug.Log("[KSPI]: SetDefaultFuelMode - load fuel modes");
                fuelModes = GetReactorFuelModes();
            }

            CurrentFuelMode = fuelModes.FirstOrDefault();

            maxPowerToSupply = Math.Max(MaximumPower * (double)(decimal)TimeWarp.fixedDeltaTime, 0);

            if (CurrentFuelMode == null)
                Debug.LogWarning("[KSPI]: Warning : CurrentFuelMode is null");
            else
                Debug.Log("[KSPI]: CurrentFuelMode = " + CurrentFuelMode.DisplayName);
        }

        protected double ConsumeReactorFuel(ReactorFuel fuel, double powerInMj, double deltaTime, PartResource resourceControl = null )
        {
            if (fuel == null)
            {
                Debug.LogWarning("[KSPI]: Warning ConsumeReactorFuel fuel null");
                return 0;
            }

            if (powerInMj.IsInfinityOrNaNorZero())
                return 0;

            var consumeAmountInUnitOfStorage = FuelEfficiency > 0 ? powerInMj * fuel.AmountFuelUsePerMj * fuelUsePerMJMult / FuelEfficiency : 0;
            if (resourceControl != null)
            {
                resourceControl.maxAmount = consumeAmountInUnitOfStorage;
                resourceControl.amount = consumeAmountInUnitOfStorage;
            }

            if (fuel.ConsumeGlobal)
            {
                var result = simulateConsumption ||  fuel.Simulate ? 0
                    : part.RequestResource(fuel.Definition.id, consumeAmountInUnitOfStorage * deltaTime, ResourceFlowMode.STAGE_PRIORITY_FLOW, resourceControl != null);

                return (simulateConsumption || fuel.Simulate || CheatOptions.InfinitePropellant ? consumeAmountInUnitOfStorage : result) * fuel.DensityInTon;
            }

            if (part.Resources.Contains(fuel.ResourceName))
            {
                double reduction = Math.Min(deltaTime * consumeAmountInUnitOfStorage, part.Resources[fuel.ResourceName].amount);
                if (resourceControl == null)
                    part.Resources[fuel.ResourceName].amount -= reduction;
                return reduction * fuel.DensityInTon;
            }
            else
                return 0;
        }

        protected virtual double ProduceReactorProduct(ReactorProduct product, double powerInMj, double deltaTime, bool simulate = false)
        {
            if (product == null)
            {
                Debug.LogWarning("[KSPI]: ProduceReactorProduct product null");
                return 0;
            }

            if (powerInMj.IsInfinityOrNaNorZero())
                return 0;

            var productSupply = powerInMj * product.AmountProductUsePerMj * fuelUsePerMJMult / FuelEfficiency;
            var fixedProductSupply = productSupply * deltaTime;

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                {
                    var partResource = part.Resources[product.ResourceName];
                    if (partResource == null)
                        return 0;

                    var availableStorage = partResource.maxAmount - partResource.amount;
                    if(!simulateConsumption && !simulate)
                        partResource.amount += Math.Min(fixedProductSupply, availableStorage);

                    return fixedProductSupply * product.DensityInTon;
                }
                else
                    return 0;
            }

            if (!simulateConsumption && !simulate)
            {
                part.RequestResource(product.Definition.id, -fixedProductSupply, ResourceFlowMode.STAGE_PRIORITY_FLOW);
            }

            return fixedProductSupply * product.DensityInTon;
        }

        protected double GetFuelAvailability(ReactorFuel fuel)
        {
            if (fuel == null)
            {
                Debug.LogError("[KSPI]: GetFuelAvailability fuel null");
                return 0;
            }

            if (!fuel.ConsumeGlobal)
                return GetLocalResourceAmount(fuel);

            return HighLogic.LoadedSceneIsFlight ? part.GetResourceAvailable(fuel.Definition) : part.FindAmountOfAvailableFuel(fuel.ResourceName, 4);
        }

        protected double GetLocalResourceRatio(ReactorFuel fuel)
        {
            if (part.Resources.Contains(fuel.ResourceName))
                return part.Resources[fuel.ResourceName].amount / part.Resources[fuel.ResourceName].maxAmount;
            else
                return 0;
        }

        protected double GetLocalResourceAmount(ReactorFuel fuel)
        {
            if (part.Resources.Contains(fuel.ResourceName))
                return part.Resources[fuel.ResourceName].amount;
            else
                return 0;
        }

        protected double GetFuelAvailability(PartResourceDefinition definition, bool consumeGlobal)
        {
            if (definition == null)
            {
                Debug.LogError("[KSPI]: GetFuelAvailability definition null");
                return 0;
            }

            if (!consumeGlobal)
            {
                if (part.Resources.Contains(definition.name))
                    return part.Resources[definition.name].amount;
                else
                    return 0;
            }

            return HighLogic.LoadedSceneIsFlight ? part.GetResourceAvailable(definition) : part.FindAmountOfAvailableFuel(definition.name, 4);
        }

        protected double GetProductAvailability(ReactorProduct product)
        {
            if (product == null)
            {
                Debug.LogError("[KSPI]: GetFuelAvailability product null");
                return 0;
            }

            if (product.Definition == null)
            {
                Debug.LogError("[KSPI]: GetFuelAvailability product definition null");
                return 0;
            }

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                    return part.Resources[product.ResourceName].amount;
                else
                    return 0;
            }

            return HighLogic.LoadedSceneIsFlight ? part.GetResourceAvailable(product.Definition) : part.FindAmountOfAvailableFuel(product.ResourceName, 4);
        }

        protected double GetMaxProductAvailability(ReactorProduct product)
        {
            if (product == null)
            {
                Debug.LogError("[KSPI]: GetMaxProductAvailability product null");
                return 0;
            }

            if (product.Definition == null)
                return 0;

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                    return part.Resources[product.ResourceName].maxAmount;
                else
                    return 0;
            }

            return HighLogic.LoadedSceneIsFlight
                ? part.GetResourceMaxAvailable(product.Definition)
                : part.FindMaxAmountOfAvailableFuel(product.ResourceName, 4);
        }

        private void InitializeKerbalismEmitter()
        {
            if (!Kerbalism.IsLoaded)
                return;

            _emitterController = part.FindModuleImplementing<FNEmitterController>();

            if (_emitterController != null)
            {
                _emitterController.diameter = radius;
                _emitterController.exhaustProducesNeutronRadiation = !mayExhaustInLowSpaceHomeworld;
                _emitterController.exhaustProducesGammaRadiation = !mayExhaustInAtmosphereHomeworld;
            }
            else
                Debug.LogWarning("[KSPI]: No Emitter Found om " + part.partInfo.title);
        }

        private void UpdateKerbalismEmitter()
        {
            if (_emitterController == null)
                return;

            _emitterController.reactorActivityFraction = ongoing_consumption_rate;
            _emitterController.fuelNeutronsFraction = CurrentFuelMode.NeutronsRatio;
            _emitterController.lithiumNeutronAbsorbtionFraction = lithiumNeutronAbsorption;
            _emitterController.exhaustActivityFraction = _currentPropulsionRequestRatioSum;
            _emitterController.radioactiveFuelLeakFraction = Math.Max(0, 1 - geeForceModifier);

            _emitterController.reactorShadowShieldMassProtection = IsConnectedToThermalGenerator || IsConnectedToChargedGenerator
                ? Math.Max(_currentChargedEnergyGeneratorMass, _currentThermalEnergyGeneratorMass) / (radius * radius) / (RawMaximumPower * 0.001)
                : 0;
        }

        public void OnGUI()
        {
            if (vessel == FlightGlobals.ActiveVessel && renderWindow)
                windowPosition = GUILayout.Window(_windowId, windowPosition, Window, Localizer.Format("#LOC_KSPIE_Reactor_reactorControlWindow"));
        }

        protected void PrintToGuiLayout(string label, string value, GUIStyle guiLabelStyle, GUIStyle guiValueStyle, int widthLabel = 150, int widthValue = 150)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, guiLabelStyle, GUILayout.Width(widthLabel));
            GUILayout.Label(value, guiValueStyle, GUILayout.Width(widthValue));
            GUILayout.EndHorizontal();
        }

        protected virtual void WindowReactorStatusSpecificOverride() { }

        protected virtual void WindowReactorControlSpecificOverride() { }

        private void Window(int windowId)
        {
            windowPositionX = windowPosition.x;
            windowPositionY = windowPosition.y;

            if (boldStyle == null)
                boldStyle = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold, font = PluginHelper.MainFont};

            if (textStyle == null)
                textStyle = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Normal,font = PluginHelper.MainFont};

            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                renderWindow = false;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(TypeName, boldStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            if (IsFuelNeutronRich)
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_ReactorEmbrittlement"), (100 * (1 - ReactorEmbrittlementConditionRatio)).ToString("0.000000") + "%", boldStyle, textStyle);//"Reactor Embrittlement"

            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_Geeforceoverload") +" ", (100 * (1 - geeForceModifier)).ToString("0.000000") + "%", boldStyle, textStyle);//Geeforce overload
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_Overheating") +" ", (100 * (1 - overheatModifier)).ToString("0.000000") + "%", boldStyle, textStyle);//Overheating

            WindowReactorStatusSpecificOverride();

            PrintToGuiLayout("Lifetime", ConvertSecondsToDayHourMinute((int)Math.Max(Planetarium.GetUniversalTime() - startTime, vessel == null ? 0d : vessel.missionTime)), boldStyle, textStyle);

            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_Radius"), radius + "m", boldStyle, textStyle);//"Radius"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CoreTemperature"), coretempStr, boldStyle, textStyle);//"Core Temperature"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_StatusLabel"), statusStr, boldStyle, textStyle);//"Status"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelMode"), fuelModeStr, boldStyle, textStyle);//"Fuel Mode"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelEfficiencyLabel"), (FuelEfficiency * 100).ToString(CultureInfo.InvariantCulture), boldStyle, textStyle);//"Fuel efficiency"

            WindowReactorControlSpecificOverride();

            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CurrentMaxPowerOutputLabel"), PluginHelper.GetFormattedPowerString(ongoing_total_power_generated) + " / " + PluginHelper.GetFormattedPowerString(NormalisedMaximumPower), boldStyle, textStyle);//"Current/Max Power Output"

            if (ChargedPowerRatio < 1.0)
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CurrentMaxThermalPower"), PluginHelper.GetFormattedPowerString(ongoing_thermal_power_generated) + " / " + PluginHelper.GetFormattedPowerString(MaximumThermalPower), boldStyle, textStyle);//"Current/Max Thermal Power"
            if (ChargedPowerRatio > 0)
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CurrentMaxChargedPower"), PluginHelper.GetFormattedPowerString(ongoing_charged_power_generated) + " / " + PluginHelper.GetFormattedPowerString(MaximumChargedPower), boldStyle, textStyle);//"Current/Max Charged Power"

            if (CurrentFuelMode != null && currentFuelVariant.ReactorFuels != null)
            {
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_EnergyProduction"), currentFuelVariant.GigawattPerGram.ToString("0.0") + " GW / g", boldStyle, textStyle);//"Energy Production"
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelUsage"), currentFuelVariant.FuelUseInGramPerTeraJoule.ToString("0.000") + " g / TW", boldStyle, textStyle);//"Fuel Usage"

                if (IsFuelNeutronRich && breedtritium && canBreedTritium)
                {
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelNeutronBreedRate"), 100 * CurrentFuelMode.NeutronsRatio + "% ", boldStyle, textStyle);//"Fuel Neutron Breed Rate"

                    var tritiumTonPerHour = _tritiumProducedPerSecond * _tritiumDensity  * 3600;
                    if (tritiumTonPerHour > 120)
                        PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_TritiumBreedRate"), PluginHelper.FormatMassStr(tritiumTonPerHour / 60) + " / " + Localizer.Format("#LOC_KSPIE_Reactor_min"), boldStyle, textStyle);//Consumption-min
                    else
                        PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_TritiumBreedRate"), PluginHelper.FormatMassStr(tritiumTonPerHour) + " / " + Localizer.Format("#LOC_KSPIE_Reactor_hour"), boldStyle, textStyle);//Consumption-min

                    var heliumKgDay = _heliumProducedPerSecond * _helium4Density * 1000 * PluginSettings.Config.SecondsInDay;
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_HeliumBreedRate"), heliumKgDay.ToString("0.000000") + " " + Localizer.Format("#LOC_KSPIE_Reactor_kgDay") + " ", boldStyle, textStyle);//"Helium Breed Rate"kg/day

                    part.GetConnectedResourceTotals(_lithium6Def.id, out var totalLithium6Amount, out var totalLithium6MaxAmount);

                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_LithiumReserves"), totalLithium6Amount.ToString("0.000") + " L / " + totalLithium6MaxAmount.ToString("0.000") + " L", boldStyle, textStyle);//"Lithium Reserves"

                    var lithiumConsumptionDay = _lithiumConsumedPerSecond * PluginSettings.Config.SecondsInDay;
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_LithiumConsumption"), lithiumConsumptionDay.ToString("0.00000") + " "+Localizer.Format("#LOC_KSPIE_Reactor_lithiumConsumptionDay"), boldStyle, textStyle);//"Lithium Consumption"L/day
                    var lithiumLifetimeTotalDays = lithiumConsumptionDay > 0 ? totalLithium6Amount / lithiumConsumptionDay : 0;

                    var lithiumLifetimeYears = Math.Floor(lithiumLifetimeTotalDays / GameConstants.KERBIN_YEAR_IN_DAYS);
                    var lithiumLifetimeYearsRemainderInDays = lithiumLifetimeTotalDays % GameConstants.KERBIN_YEAR_IN_DAYS;

                    var lithiumLifetimeRemainingDays = Math.Floor(lithiumLifetimeYearsRemainderInDays);
                    var lithiumLifetimeRemainingDaysRemainer = lithiumLifetimeYearsRemainderInDays % 1;

                    var lithiumLifetimeRemainingHours = lithiumLifetimeRemainingDaysRemainer * PluginSettings.Config.SecondsInDay / GameConstants.SECONDS_IN_HOUR;

                    if (lithiumLifetimeYears < 1e9)
                    {
                        if (lithiumLifetimeYears < 1)
                            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_LithiumRemaining"), lithiumLifetimeRemainingDays + " "+Localizer.Format("#LOC_KSPIE_Reactor_days") +" " + lithiumLifetimeRemainingHours.ToString("0.0") + " "+Localizer.Format("#LOC_KSPIE_Reactor_hours"), boldStyle, textStyle);//"Lithium Remaining"days""hours
                        else if (lithiumLifetimeYears < 1e3)
                            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_LithiumRemaining"), lithiumLifetimeYears + " "+Localizer.Format("#LOC_KSPIE_Reactor_years") +" " + lithiumLifetimeRemainingDays + " "+Localizer.Format("#LOC_KSPIE_Reactor_days"), boldStyle, textStyle);//"Lithium Remaining"years""days
                        else
                            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_LithiumRemaining"), lithiumLifetimeYears + " "+Localizer.Format("#LOC_KSPIE_Reactor_years") +" " , boldStyle, textStyle);//"Lithium Remaining"years
                    }

                    part.GetConnectedResourceTotals(_tritiumDef.id, out var totalTritiumAmount, out var totalTritiumMaxAmount);

                    var massTritiumAmount = totalTritiumAmount * _tritiumDensity * 1000;
                    var massTritiumMaxAmount = totalTritiumMaxAmount * _tritiumDensity * 1000;

                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_TritiumStorage"), massTritiumAmount.ToString("0.000000") + " kg / " + massTritiumMaxAmount.ToString("0.000000") + " kg", boldStyle, textStyle);//"Tritium Storage"

                    part.GetConnectedResourceTotals(_heliumDef.id, out var totalHeliumAmount, out var totalHeliumMaxAmount);

                    var massHeliumAmount = totalHeliumAmount * _helium4Density * 1000;
                    var massHeliumMaxAmount = totalHeliumMaxAmount * _helium4Density * 1000;

                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_HeliumStorage"), massHeliumAmount.ToString("0.000000") + " kg / " + massHeliumMaxAmount.ToString("0.000000") + " kg", boldStyle, textStyle);//"Helium Storage"
                }
                else
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_IsNeutronrich"), IsFuelNeutronRich.ToString(), boldStyle, textStyle);//"Is Neutron rich"

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_Reactor_Fuels") +":", boldStyle, GUILayout.Width(150));//Fuels
                GUILayout.EndHorizontal();

                foreach (var fuel in currentFuelVariant.ReactorFuels)
                {
                    if (fuel == null)
                        continue;

                    var resourceVariantsDefinitions = CurrentFuelMode.ResourceGroups.First(m => m.name == fuel.FuelName).resourceVariantsMetaData;

                    var availableResources = resourceVariantsDefinitions
                        .Select(m => new { m.resourceDefinition, m.ratio }).Distinct()
                        .Select(d => new { definition = d.resourceDefinition, amount = GetFuelAvailability(d.resourceDefinition, fuel.ConsumeGlobal), effectiveDensity = d.resourceDefinition.density * d.ratio})
                        .Where(m => m.amount > 0).ToList();

                    var availabilityInTon = availableResources.Sum(m => m.amount * m.effectiveDensity);

                    var variantText = availableResources.Count > 1 ? " (" + availableResources.Count + " variants)" : "";
                    PrintToGuiLayout(fuel.FuelName + " "+ Localizer.Format("#LOC_KSPIE_Reactor_Reserves"), PluginHelper.FormatMassStr(availabilityInTon) + variantText, boldStyle, textStyle);//Reserves

                    var tonFuelUsePerHour = ongoing_total_power_generated * fuel.TonsFuelUsePerMj * fuelUsePerMJMult / FuelEfficiency * PluginSettings.Config.SecondsInHour;
                    var kgFuelUsePerHour = tonFuelUsePerHour * 1000;
                    var kgFuelUsePerDay = kgFuelUsePerHour * PluginSettings.Config.HoursInDay;

                    if (tonFuelUsePerHour > 120)
                        PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Consumption") +" ", PluginHelper.FormatMassStr(tonFuelUsePerHour / 60) + " / "+Localizer.Format("#LOC_KSPIE_Reactor_min"), boldStyle, textStyle);//Consumption-min
                    else
                        PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Consumption") +" ", PluginHelper.FormatMassStr(tonFuelUsePerHour) + " / "+Localizer.Format("#LOC_KSPIE_Reactor_hour"), boldStyle, textStyle);//Consumption--hour

                    if (kgFuelUsePerDay > 0)
                    {
                        var fuelLifetimeD = availabilityInTon * 1000 / kgFuelUsePerDay;
                        var lifetimeYears = Math.Floor(fuelLifetimeD / GameConstants.KERBIN_YEAR_IN_DAYS);
                        if (lifetimeYears < 1e9)
                        {
                            if (lifetimeYears >= 10)
                            {
                                var lifetimeYearsDayRemainder = lifetimeYears < 1e+6 ? fuelLifetimeD % GameConstants.KERBIN_YEAR_IN_DAYS : 0;
                                PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Lifetime"), (double.IsNaN(lifetimeYears) ? "-" : lifetimeYears + " " + Localizer.Format("#LOC_KSPIE_Reactor_years") + " "), boldStyle, textStyle);//Lifetime years
                            }
                            else if (lifetimeYears > 0)
                            {
                                var lifetimeYearsDayRemainder = lifetimeYears < 1e+6 ? fuelLifetimeD % GameConstants.KERBIN_YEAR_IN_DAYS : 0;
                                PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Lifetime"), (double.IsNaN(lifetimeYears) ? "-" : lifetimeYears + " " + Localizer.Format("#LOC_KSPIE_Reactor_years") + " " + (lifetimeYearsDayRemainder).ToString("0.00")) + " " + Localizer.Format("#LOC_KSPIE_Reactor_days"), boldStyle, textStyle);//Lifetime--years--days
                            }
                            else if (fuelLifetimeD < 1)
                            {
                                var minutesD = fuelLifetimeD * PluginSettings.Config.HoursInDay * 60;
                                var minutes = (int)Math.Floor(minutesD);
                                var seconds = (int)Math.Ceiling((minutesD - minutes) * 60);

                                PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Lifetime"), minutes.ToString("F0") + " " + Localizer.Format("#LOC_KSPIE_Reactor_minutes") + " " + seconds.ToString("F0") + " " + Localizer.Format("#LOC_KSPIE_Reactor_seconds"), boldStyle, textStyle);//Lifetime--minutes--seconds
                            }
                            else
                                PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Lifetime"), (double.IsNaN(fuelLifetimeD) ? "-" : (fuelLifetimeD).ToString("0.00")) + " " + Localizer.Format("#LOC_KSPIE_Reactor_days"), boldStyle, textStyle);//Lifetime--days
                        }
                        else
                            PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Lifetime"), "", boldStyle, textStyle);//Lifetime
                    }
                    else
                        PrintToGuiLayout(fuel.FuelName + " "+ Localizer.Format("#LOC_KSPIE_Reactor_Lifetime"), "", boldStyle, textStyle);//Lifetime
                }

                if (currentFuelVariant.ReactorProducts.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localizer.Format("#LOC_KSPIE_Reactor_Products"), boldStyle, GUILayout.Width(150));//"Products:"
                    GUILayout.EndHorizontal();

                    foreach (var product in currentFuelVariant.ReactorProducts)
                    {
                        if (product == null)
                            continue;

                        var availabilityInTon = GetProductAvailability(product) * product.DensityInTon;
                        var maxAvailabilityInTon = GetMaxProductAvailability(product) * product.DensityInTon;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(product.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Storage"), boldStyle, GUILayout.Width(150));//Storage
                        GUILayout.Label(PluginHelper.FormatMassStr(availabilityInTon, "0.00000") + " / " + PluginHelper.FormatMassStr(maxAvailabilityInTon, "0.00000"), textStyle, GUILayout.Width(150));
                        GUILayout.EndHorizontal();

                        var hourProductionInTon = ongoing_total_power_generated * product.TonsProductUsePerMj * fuelUsePerMJMult / FuelEfficiency * PluginSettings.Config.SecondsInHour;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(product.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Production"), boldStyle, GUILayout.Width(150));//Production
                        GUILayout.Label(PluginHelper.FormatMassStr(hourProductionInTon) + " / " + Localizer.Format("#LOC_KSPIE_Reactor_hour"), textStyle, GUILayout.Width(150));//hour
                        GUILayout.EndHorizontal();
                    }
                }
            }

            if (!IsStarted || !IsNuclear)
            {
                GUILayout.BeginHorizontal();

                if (IsEnabled && canShutdown && GUILayout.Button(Localizer.Format("#LOC_KSPIE_Reactor_Deactivate"), GUILayout.ExpandWidth(true)))//"Deactivate"
                    DeactivateReactor();
                if (!IsEnabled && GUILayout.Button(Localizer.Format("#LOC_KSPIE_Reactor_Activate"), GUILayout.ExpandWidth(true)))//"Activate"
                    ActivateReactor();

                GUILayout.EndHorizontal();
            }
            else
            {
                if (IsEnabled)
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Reactor_Shutdown"), GUILayout.ExpandWidth(true)))//"Shutdown"
                        IsEnabled = false;

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public string ConvertSecondsToDayHourMinute(int n)
        {
            var secondsInYear = GameConstants.KERBIN_YEAR_IN_DAYS * PluginSettings.Config.SecondsInDay;

            int year = (int)(n / secondsInYear);
            n = n % (int)secondsInYear;

            int day = n / PluginSettings.Config.SecondsInDay;
            n = n % PluginSettings.Config.SecondsInDay;
            int hour = n / 3600;

            n %= 3600;
            int minutes = n / 60;

            n %= 60;
            int seconds = n;

            return year + " years " +  day + " days " + hour + " hours";
        }

        public override string getResourceManagerDisplayName()
        {
            var displayName = part.partInfo.title;
            if (fuelModes.Count > 1 )
                displayName += " (" + fuelModeStr + ")";
            if (similarParts != null && similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }
    }
}
