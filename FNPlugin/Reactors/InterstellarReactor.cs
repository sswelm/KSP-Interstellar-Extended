using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.External;
using FNPlugin.Power;
using FNPlugin.Propulsion;
using FNPlugin.Redist;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Reactors
{
    [KSPModule("#LOC_KSPIE_Reactor_moduleName")]
    class InterstellarReactor : ResourceSuppliableModule, IFNPowerSource, IRescalable<InterstellarReactor>, IPartCostModifier
    {
        public const string GROUP = "InterstellarReactor";
        public const string GROUP_TITLE = "#LOC_KSPIE_Reactor_groupName";

        public const string UpgradesGroup = "ReactorUpgrades";
        public const string UpgradesGroupDisplayName = "#LOC_KSPIE_Reactor_upgrades";

        //public enum ReactorTypes
        //{
        //    FISSION_MSR = 1,
        //    FISSION_GFR = 2,
        //    FUSION_DT = 4,
        //    FUSION_GEN3 = 8,
        //    AIM_FISSION_FUSION = 16,
        //    ANTIMATTER = 32
        //}

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricPowerPriority = 2;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_powerPercentage"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 10)]
        public float powerPercentage = 100;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_ForcedMinimumThrotle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]//Forced Minimum Throtle
        public float forcedMinimumThrottle = 0;

        // Persistent True
        [KSPField(isPersistant = true)] public int fuelmode_index = -1;
        [KSPField(isPersistant = true)] public string fuel_mode_name = string.Empty;
        [KSPField(isPersistant = true)] public string fuel_mode_variant = string.Empty;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_ReactorIsEnabled")]
        public bool IsEnabled;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_ReactorIsStated")]
        public bool IsStarted;

        [KSPField(isPersistant = true)] public bool isDeployed = false;
        [KSPField(isPersistant = true)] public bool isupgraded = false;
        [KSPField(isPersistant = true)] public bool breedtritium;
        [KSPField(isPersistant = true)] public double last_active_time;
        [KSPField(isPersistant = true)] public double ongoing_consumption_rate;
        [KSPField(isPersistant = true)] public double ongoing_wasteheat_rate;
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
        [KSPField(isPersistant = true)] public double requested_thermal_power_ratio = 1;
        [KSPField(isPersistant = true)] public double maximumThermalPower;
        [KSPField(isPersistant = true)] public double maximumChargedPower;

        [KSPField(isPersistant = true)] public double thermal_power_ratio = 1;
        [KSPField(isPersistant = true)] public double charged_power_ratio = 1;
        [KSPField(isPersistant = true)] public double reactor_power_ratio = 1;
        [KSPField(isPersistant = true)] public double power_request_ratio = 1;

        [KSPField] public double maximum_thermal_request_ratio;
        [KSPField] public double maximum_charged_request_ratio;
        [KSPField] public double maximum_reactor_request_ratio;
        [KSPField] public double thermalThrottleRatio;
        [KSPField] public double plasmaThrottleRatio;
        [KSPField] public double chargedThrottleRatio;

        [KSPField(isPersistant = true)] public double storedIsThermalEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)] public double storedIsPlasmaEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)] public double storedIsChargedEnergyGeneratorEfficiency;

        [KSPField(isPersistant = true)] public double storedGeneratorThermalEnergyRequestRatio;
        [KSPField(isPersistant = true)] public double storedGeneratorPlasmaEnergyRequestRatio;
        [KSPField(isPersistant = true)] public double storedGeneratorChargedEnergyRequestRatio;

        [KSPField(isPersistant = true)]
        public double ongoing_total_power_generated;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_thermalPower", guiFormat = "F6")]
        protected double ongoing_thermal_power_generated;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_Reactor_chargedPower ", guiFormat = "F6")]
        protected double ongoing_charged_power_generated;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "#LOC_KSPIE_Reactor_LithiumModifier", guiFormat = "F6")]
        public double lithium_modifier = 1;

        [KSPField] public double maximumPower;
        [KSPField] public float minimumPowerPercentage = 10;

        [KSPField] public string upgradeTechReqMk2 = null;
        [KSPField] public string upgradeTechReqMk3 = null;
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

        [KSPField] public double fusionEnergyGainFactorMk1 = 10;
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

        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_powerOutputMk1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerOutputMk1;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerOutputMk2;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk3", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerOutputMk3;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk4", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerOutputMk4;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk5", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerOutputMk5;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk6", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerOutputMk6;
        [KSPField(groupName = UpgradesGroup, groupDisplayName = UpgradesGroupDisplayName, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk7", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double powerOutputMk7;

        // Settings
        [KSPField] public double neutronsExhaustRadiationMult = 16;
        [KSPField] public double gammaRayExhaustRadiationMult = 4;
        [KSPField] public double neutronScatteringRadiationMult = 20;

        [KSPField] public bool showEngineConnectionInfo = true;
        [KSPField] public bool showPowerGeneratorConnectionInfo = true;
        [KSPField] public bool mayExhaustInAtmosphereHomeworld = true;
        [KSPField] public bool mayExhaustInLowSpaceHomeworld = true;
        [KSPField] public double minThermalNozzleTempRequired = 0;
        [KSPField] public bool canUseAllPowerForPlasma = true;
        [KSPField] public bool updateModuleCost = true;
        [KSPField] public int minCoolingFactor = 1;
        [KSPField] public double engineHeatProductionMult = 1;
        [KSPField] public double plasmaHeatProductionMult = 1;
        [KSPField] public double engineWasteheatProductionMult = 1;
        [KSPField] public double plasmaWasteheatProductionMult = 1;
        [KSPField] public bool supportMHD = false;
        [KSPField] public int reactorModeTechBonus = 0;
        [KSPField] public bool canBeCombinedWithLab = false;
        [KSPField] public bool canBreedTritium = false;
        [KSPField] public bool canDisableTritiumBreeding = true;
        [KSPField] public bool showShutDownInFlight = false;
        [KSPField] public bool showForcedMinimumThrottle = false;
        [KSPField] public bool showPowerPercentage = true;
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
        [KSPField] public string animName = "";
        [KSPField] public double animExponent = 1;
        [KSPField] public string loopingAnimationName = "";
        [KSPField] public string startupAnimationName = "";
        [KSPField] public string shutdownAnimationName = "";
        [KSPField] public double reactorSpeedMult = 1;
        [KSPField] public double powerRatio;
        [KSPField] public string upgradedName = "";
        [KSPField] public string originalName = "";

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = false, guiFormat = "F2", guiName = "#LOC_KSPIE_Reactor_connectionRadius")]
        public double radius = 2.5;

        [KSPField] public double minimumThrottle = 0;
        [KSPField] public bool canShutdown = true;
        [KSPField] public int reactorType = 0;
        [KSPField] public double fuelEfficiency = 1;
        [KSPField] public bool containsPowerGenerator = false;
        [KSPField] public double fuelUsePerMJMult = 1;
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public double wasteHeatBufferMassMult = 2.0e+5;
        [KSPField] public double wasteHeatBufferMult = 1;
        [KSPField] public double hotBathTemperature = 0;
        [KSPField] public bool usePropellantBaseIsp = false;
        [KSPField] public double emergencyPowerShutdownFraction = 0.99;
        [KSPField] public double thermalPropulsionEfficiency = 1;
        [KSPField] public double plasmaPropulsionEfficiency = 1;
        [KSPField] public double chargedParticlePropulsionEfficiency = 1;

        [KSPField] public double thermalEnergyEfficiency = 1;
        [KSPField] public double chargedParticleEnergyEfficiency = 1;
        [KSPField] public double plasmaEnergyEfficiency = 1;
        [KSPField] public double maxGammaRayPower = 0;

        [KSPField] public double maxChargedParticleUtilisationRatio = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk1 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk2 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk3 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk4 = 1;
        [KSPField] public double maxChargedParticleUtilisationRatioMk5 = 1;

        [KSPField] public string maxChargedParticleUtilisationTechMk2 = null;
        [KSPField] public string maxChargedParticleUtilisationTechMk3 = null;
        [KSPField] public string maxChargedParticleUtilisationTechMk4 = null;
        [KSPField] public string maxChargedParticleUtilisationTechMk5 = null;

        [KSPField] public bool hasBuoyancyEffects = true;
        [KSPField] public double geeForceMultiplier = 0.1;
        [KSPField] public double geeForceTreshHold = 9;
        [KSPField] public double geeForceExponent = 2;
        [KSPField] public double minGeeForceModifier = 0.01;

        [KSPField] public bool hasOverheatEffects = true;
        [KSPField] public double overheatMultiplier = 10;
        [KSPField] public double overheatTreshHold = 0.95;
        [KSPField] public double overheatExponent = 2;
        [KSPField] public double minOverheatModifier = 0.01;

        [KSPField] public string soundRunningFilePath = "";
        [KSPField] public double soundRunningPitchMin = 0.4;
        [KSPField] public double soundRunningPitchExp = 0;
        [KSPField] public double soundRunningVolumeExp = 0;
        [KSPField] public double soundRunningVolumeMin = 0;

        [KSPField] public string soundTerminateFilePath = "";
        [KSPField] public string soundInitiateFilePath = "";
        [KSPField] public double neutronEmbrittlementLifepointsMax = 100;
        [KSPField] public double neutronEmbrittlementDivider = 1e+9;
        [KSPField] public double hotBathModifier = 1;
        [KSPField] public double thermalProcessingModifier = 1;
        [KSPField] public int supportedPropellantAtoms = GameConstants.defaultSupportedPropellantAtoms;
        [KSPField] public int supportedPropellantTypes = GameConstants.defaultSupportedPropellantTypes;
        [KSPField] public bool fullPowerForNonNeutronAbsorbants = true;
        [KSPField] public bool showPowerPriority = true;
        [KSPField] public bool showSpecialisedUI = true;
        [KSPField] public bool canUseNeutronicFuels = true;
        [KSPField] public bool canUseGammaRayFuels = true;
        [KSPField] public double maxNeutronsRatio = 1.04;
        [KSPField] public double minNeutronsRatio = 0;

        [KSPField] public int fuelModeTechLevel;
        [KSPField] public string bimodelUpgradeTechReq = string.Empty;
        [KSPField] public string powerUpgradeTechReq = string.Empty;
        [KSPField] public double powerUpgradeCoreTempMult = 1;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_rawPowerOutput", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double currentRawPowerOutput;

        [KSPField] public double PowerOutput = 0;
        [KSPField] public double upgradedPowerOutput = 0;
        [KSPField] public string upgradeTechReq = string.Empty;
        [KSPField] public bool shouldApplyBalance;
        [KSPField] public double tritium_molar_mass_ratio = 3.0160 / 7.0183;
        [KSPField] public double helium_molar_mass_ratio = 4.0023 / 7.0183;

        // GUI strings
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_reactorStatus")]
        public string statusStr = string.Empty;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_coreTemperature")]
        public string coretempStr = string.Empty;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorFuelMode")]
        public string fuelModeStr = string.Empty;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_connectedRecievers")]
        public string connectedRecieversStr = string.Empty;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_reactorSurface", guiUnits = " m\xB3")]
        public double reactorSurface;

        [KSPField] protected double maxPowerToSupply;
        [KSPField] protected double requestedThermalToSupplyPerSecond;
        [KSPField] protected double maxThermalToSupplyPerSecond;
        [KSPField] protected double requestedChargedToSupplyPerSecond;
        [KSPField] protected double maxChargedToSupplyPerSecond;
        [KSPField] protected double minThrottle;
        [KSPField] public double massCostExponent = 2.5;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_InitialCost")]//Initial Cost
        public double initialCost;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_CalculatedCost")]//Calculated Cost
        public double calculatedCost;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_MaxResourceCost")]//Max Resource Cost
        public double maxResourceCost;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_ModuleCost")]//Module Cost
        public float moduleCost;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_NeutronEmbrittlementCost")]//Neutron Embrittlement Cost
        public double neutronEmbrittlementCost;

        // Gui
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public float massDifference;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_CalibratedMass", guiUnits = " t")]//calibrated mass
        public float partMass = 0;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorMass", guiFormat = "F3", guiUnits = " t")]
        public float currentMass;
        [KSPField]
        public double maximumThermalPowerEffective;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_EmbrittlementFraction", guiFormat = "F4")]//Embrittlement Fraction
        public double embrittlementModifier;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_BuoyancyFraction", guiFormat = "F4")]//Buoyancy Fraction
        public double geeForceModifier = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_OverheatFraction", guiFormat = "F4")]//Overheat Fraction
        public double overheatModifier = 1;

        [KSPField]public double lithiumNeutronAbsorbtion = 1;
        [KSPField]public bool isConnectedToThermalGenerator;
        [KSPField]public bool isConnectedToChargedGenerator;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorControlWindow"), UI_Toggle(disabledText = "#LOC_KSPIE_Reactor_reactorControlWindow_Hidden", enabledText = "#LOC_KSPIE_Reactor_reactorControlWindow_Shown", affectSymCounterparts = UI_Scene.None)]//Hidden-Shown
        public bool render_window;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_startEnabled"), UI_Toggle(disabledText = "#LOC_KSPIE_Reactor_startEnabled_True", enabledText = "#LOC_KSPIE_Reactor_startEnabled_False")]//True-False
        public bool startDisabled;

        // shared variabels
        protected bool decay_ongoing;
        protected bool initialized;
        protected bool messagedRanOutOfFuel;

        protected double currentGeeForce;
        protected double animationStarted = 0;
        protected double powerPcnt;
        protected double totalAmountLithium = 0;
        protected double totalMaxAmountLithium = 0;

        protected GUIStyle boldStyle;
        protected GUIStyle textStyle;
        protected List<ReactorFuelType> fuelModes;
        protected List<ReactorFuelMode> currentFuelVariantsSorted;
        protected ReactorFuelMode currentFuelVariant;
        protected AnimationState[] pulseAnimation;
        protected ModuleAnimateGeneric startupAnimation;
        protected ModuleAnimateGeneric shutdownAnimation;
        protected ModuleAnimateGeneric loopingAnimation;

        private FNHabitat centrifugeHabitat;
        private Rect windowPosition;
        private ReactorFuelType _currentFuelMode;
        private PartResourceDefinition _lithium6Def;
        private PartResourceDefinition _tritiumDef;
        private PartResourceDefinition _heliumDef;
        private PartResourceDefinition hydrogenDefinition;
        private ResourceBuffers _resourceBuffers;
        private FNEmitterController emitterController;

        private readonly List<ReactorProduction> reactorProduction = new List<ReactorProduction>();
        private readonly List<IFNEngineNoozle> connectedEngines = new List<IFNEngineNoozle>();
        private readonly Queue<double> averageGeeforce = new Queue<double>();
        private readonly Queue<double> averageOverheat = new Queue<double>();

        private AudioSource _initiateSound;
        private AudioSource _terminateSound;
        private AudioSource _runningSound;

        private double _tritiumDensity;
        private double _helium4Density;
        private double _lithium6Density;

        private double _propulsionRequestRatioSum;
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

        private double _currentGeneratorThermalEnergyRequestRatio;
        private double _currentGeneratorPlasmaEnergyRequestRatio;
        private double _currentGeneratorChargedEnergyRequestRatio;

        private double _lithiumConsumedPerSecond;
        private double _tritiumProducedPerSecond;
        private double _heliumProducedPerSecond;

        private int _windowId = 90175467;
        private int _deactivateTimer;
        private int _chargedParticleUtilisationLevel = 1;

        bool hasSpecificFuelModeTechs;
        bool? hasBimodelUpgradeTechReq;
        bool isFixedUpdatedCalled;

        // properties
        public double ForcedMinimumThrottleRatio => ((double)(decimal)forcedMinimumThrottle) / 100;

        public int SupportedPropellantAtoms => supportedPropellantAtoms;

        public int SupportedPropellantTypes => supportedPropellantTypes;

        public bool FullPowerForNonNeutronAbsorbants => fullPowerForNonNeutronAbsorbants;

        public double EfficencyConnectedThermalEnergyGenerator => storedIsThermalEnergyGeneratorEfficiency;

        public double EfficencyConnectedChargedEnergyGenerator => storedIsChargedEnergyGeneratorEfficiency;

        public double FuelRato => fuel_ratio;

        public virtual double MagneticNozzlePowerMult => 1;

        public bool MayExhaustInAtmosphereHomeworld => mayExhaustInAtmosphereHomeworld;

        public bool MayExhaustInLowSpaceHomeworld => mayExhaustInLowSpaceHomeworld;

        public double MinThermalNozzleTempRequired => minThermalNozzleTempRequired;

        public virtual double CurrentMeVPerChargedProduct => _currentFuelMode?.MeVPerChargedProduct ?? 0;

        public bool UsePropellantBaseIsp => usePropellantBaseIsp;

        public bool CanUseAllPowerForPlasma => canUseAllPowerForPlasma;

        public double MinCoolingFactor => minCoolingFactor;

        public double EngineHeatProductionMult => engineHeatProductionMult;

        public double PlasmaHeatProductionMult => plasmaHeatProductionMult;

        public double EngineWasteheatProductionMult => engineWasteheatProductionMult;

        public double PlasmaWasteheatProductionMult => plasmaWasteheatProductionMult;

        public double ThermalPropulsionWasteheatModifier => 1;

        public double ConsumedFuelFixed => _consumedFuelTotalFixed;

        public bool SupportMHD => supportMHD;

        public double Radius => radius;

        public bool IsThermalSource => true;

        public double ThermalProcessingModifier => thermalProcessingModifier;

        public Part Part => part;

        public double ProducedWasteHeat => ongoing_total_power_generated;

        public double ProducedThermalHeat => ongoing_thermal_power_generated;

        public double ProducedChargedPower => ongoing_charged_power_generated;

        public int ProviderPowerPriority => (int)electricPowerPriority;

        public double ThermalTransportationEfficiency => heatTransportationEfficiency;

        public double RawTotalPowerProduced => ongoing_total_power_generated;

        public GenerationType CurrentGenerationType => (GenerationType)currentGenerationType;

        public GenerationType FuelModeTechLevel => (GenerationType)fuelModeTechLevel;

        public double ChargedParticlePropulsionEfficiency => chargedParticlePropulsionEfficiency * maxChargedParticleUtilisationRatio;

        public double PlasmaPropulsionEfficiency => plasmaPropulsionEfficiency * maxChargedParticleUtilisationRatio;

        public double ThermalPropulsionEfficiency => thermalPropulsionEfficiency;

        public double ThermalEnergyEfficiency => thermalEnergyEfficiency;

        public double PlasmaEnergyEfficiency => plasmaEnergyEfficiency;

        public double ChargedParticleEnergyEfficiency => chargedParticleEnergyEfficiency;

        public bool IsSelfContained => containsPowerGenerator;

        public String UpgradeTechnology => upgradeTechReq;

        public double PowerBufferBonus => this.bonusBufferFactor;

        public double RawMaximumPowerForPowerGeneration => RawPowerOutput;

        public double RawMaximumPower => RawPowerOutput;

        public virtual double ReactorEmbrittlementConditionRatio => Math.Min(Math.Max(1 - (neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), maxEmbrittlementFraction), 1);

        public virtual double NormalisedMaximumPower => RawPowerOutput * EffectiveEmbrittlementEffectRatio * (CurrentFuelMode?.NormalisedReactionRate ?? 1);

        public virtual double MinimumPower => MaximumPower * MinimumThrottle;

        public virtual double MaximumThermalPower => PowerRatio * NormalisedMaximumPower * ThermalPowerRatio * geeForceModifier * overheatModifier;

        public virtual double MaximumChargedPower => PowerRatio * NormalisedMaximumPower * ChargedPowerRatio * geeForceModifier * overheatModifier;

        public double ReactorSpeedMult => reactorSpeedMult;

        public virtual bool CanProducePower => stored_fuel_ratio > 0;

        public virtual bool IsNuclear => false;

        public virtual bool IsActive => IsEnabled;

        public virtual bool IsVolatileSource => false;

        public virtual bool IsFuelNeutronRich => false;

        public virtual double MaximumPower => MaximumThermalPower + MaximumChargedPower;

        public virtual double StableMaximumReactorPower => IsEnabled ? NormalisedMaximumPower : 0;

        public int ReactorFuelModeTechLevel => fuelModeTechLevel + reactorModeTechBonus;

        public int ReactorType => reactorType;

        public virtual string TypeName => part.partInfo.title;

        public virtual double ChargedPowerRatio => CurrentFuelMode?.ChargedPowerRatio ?? 0;

        public double ThermalPowerRatio => 1 - ChargedPowerRatio;

        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        private void DetermineChargedParticleUtilizationRatio()
        {
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk2))
                _chargedParticleUtilisationLevel++;
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk3))
                _chargedParticleUtilisationLevel++;
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk4))
                _chargedParticleUtilisationLevel++;
            if (PluginHelper.UpgradeAvailable(maxChargedParticleUtilisationTechMk5))
                _chargedParticleUtilisationLevel++;

            if (_chargedParticleUtilisationLevel == 1)
                maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk1;
            else if (_chargedParticleUtilisationLevel == 2)
                maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk2;
            else if (_chargedParticleUtilisationLevel == 3)
                maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk3;
            else if (_chargedParticleUtilisationLevel == 4)
                maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk4;
            else
                maxChargedParticleUtilisationRatio = maxChargedParticleUtilisationRatioMk5;
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
                maxPowerToSupply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime, 0);
                currentFuelVariantsSorted = _currentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, maxPowerToSupply, fuelUsePerMJMult);
                currentFuelVariant = currentFuelVariantsSorted.First();

                // persist
                fuelmode_index = _currentFuelMode.Index;
                fuel_mode_name = _currentFuelMode.ModeGUIName;
                fuel_mode_variant = currentFuelVariant.Name;
            }
        }

        public double PowerRatio
        {
            get
            {
                powerRatio = ((double)(decimal)powerPercentage) / 100;

                return powerRatio;
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            neutronEmbrittlementCost = calculatedCost * Math.Pow((neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), 0.5);

            maxResourceCost = part.Resources.Sum(m => m.maxAmount * m.info.unitCost);

            var dryCost = calculatedCost - initialCost;

            moduleCost = updateModuleCost ? (float)(maxResourceCost + dryCost - neutronEmbrittlementCost) : 0;

            return moduleCost;
        }


        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public void UseProductForPropulsion(double ratio, double propellantMassPerSecond)
        {
            UseProductForPropulsion(ratio, propellantMassPerSecond, hydrogenDefinition);
        }

        public void UseProductForPropulsion(double ratio, double propellantMassPerSecond, PartResourceDefinition resource)
        {
            if (ratio <= 0) return;

            foreach (var product in reactorProduction)
            {
                if (product.mass <= 0) continue;

                var effectiveMass = ratio * product.mass;

                // remove product from store
                var fuelAmount = product.fuelmode.DensityInTon > 0 ? (effectiveMass / product.fuelmode.DensityInTon) : 0;
                if (fuelAmount == 0) continue;

                part.RequestResource(product.fuelmode.ResourceName, fuelAmount);
            }

            part.RequestResource(resource.name, -propellantMassPerSecond * TimeWarp.fixedDeltaTime / resource.density, ResourceFlowMode.ALL_VESSEL);
        }

        public void ConnectWithEngine(IEngineNoozle engine)
        {
            Debug.Log("[KSPI]: ConnectWithEngine ");

            var fnEngine = engine as IFNEngineNoozle;
            if (fnEngine == null)
            {
                Debug.LogError("[KSPI]: engine is not a IFNEngineNoozle");
                return;
            }

            if (!connectedEngines.Contains(fnEngine))
                connectedEngines.Add(fnEngine);
        }

        public void DisconnectWithEngine(IEngineNoozle engine)
        {
            Debug.Log("[KSPI]: DisconnectWithEngine ");

            var fnEngine = engine as IFNEngineNoozle;
            if (fnEngine == null)
            {
                Debug.LogError("[KSPI]: engine is not a IFNEngineNoozle");
                return;
            }

            if (connectedEngines.Contains(fnEngine))
                connectedEngines.Remove(fnEngine);
        }


        public double FusionEnergyGainFactor
        {
            get
            {
                switch (FuelModeTechLevel)
                {
                    case GenerationType.Mk7:
                        return fusionEnergyGainFactorMk7;
                    case GenerationType.Mk6:
                        return fusionEnergyGainFactorMk6;
                    case GenerationType.Mk5:
                        return fusionEnergyGainFactorMk5;
                    case GenerationType.Mk4:
                        return fusionEnergyGainFactorMk4;
                    case GenerationType.Mk3:
                        return fusionEnergyGainFactorMk3;
                    case GenerationType.Mk2:
                        return fusionEnergyGainFactorMk2;
                    default:
                        return fusionEnergyGainFactorMk1;
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
                    case GenerationType.Mk7:
                        leveledThrottle = minimumThrottleMk7;
                        break;
                    case GenerationType.Mk6:
                        leveledThrottle = minimumThrottleMk6;
                        break;
                    case GenerationType.Mk5:
                        leveledThrottle = minimumThrottleMk5;
                        break;
                    case GenerationType.Mk4:
                        leveledThrottle = minimumThrottleMk4;
                        break;
                    case GenerationType.Mk3:
                        leveledThrottle = minimumThrottleMk3;
                        break;
                    case GenerationType.Mk2:
                        leveledThrottle = minimumThrottleMk2;
                        break;
                    case GenerationType.Mk1:
                        leveledThrottle = minimumThrottleMk1;
                        break;
                    default:
                        leveledThrottle = minimumThrottleMk7;
                        break;
                }

                return leveledThrottle;
            }
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
            isConnectedToThermalGenerator = true;
        }

        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio)
        {
            _currentIsThermalEnergyGeneratorEfficiency = efficency;
            _currentGeneratorThermalEnergyRequestRatio = power_ratio;
            isConnectedToThermalGenerator = true;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio)
        {
            _currentIsChargedEnergyGeneratorEfficiency = efficency;
            _currentGeneratorChargedEnergyRequestRatio = power_ratio;
            isConnectedToChargedGenerator = true;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio, double mass)
        {
            _currentChargedEnergyGeneratorMass = mass;
            _currentIsChargedEnergyGeneratorEfficiency = efficency;
            _currentGeneratorChargedEnergyRequestRatio = power_ratio;
            isConnectedToChargedGenerator = true;
        }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType)
        {
            shouldApplyBalance = isConnectedToThermalGenerator && isConnectedToChargedGenerator;
            return shouldApplyBalance;
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
            try
            {
                // calculate multipliers
                factorAbsoluteLinear = (double)(decimal)factor.absolute.linear;
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
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: InterstellarReactor.OnRescale" + e.Message);
            }
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

        public bool HasBimodelUpgradeTechReq
        {
            get
            {
                if (hasBimodelUpgradeTechReq == null)
                    hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
                return (bool)hasBimodelUpgradeTechReq;
            }
        }

        public virtual double FuelEfficiency
        {
            get
            {
                double baseEfficiency;
                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7:
                        baseEfficiency = fuelEfficencyMk7;
                        break;
                    case GenerationType.Mk6:
                        baseEfficiency = fuelEfficencyMk6;
                        break;
                    case GenerationType.Mk5:
                        baseEfficiency = fuelEfficencyMk5;
                        break;
                    case GenerationType.Mk4:
                        baseEfficiency = fuelEfficencyMk4;
                        break;
                    case GenerationType.Mk3:
                        baseEfficiency = fuelEfficencyMk3;
                        break;
                    case GenerationType.Mk2:
                        baseEfficiency = fuelEfficencyMk2;
                        break;
                    default:
                        baseEfficiency = fuelEfficencyMk1;
                        break;
                }

                return baseEfficiency * CurrentFuelMode.FuelEfficencyMultiplier;
            }
        }

        public virtual double CoreTemperature
        {
            get
            {
                double baseCoreTemperature;
                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7:
                        baseCoreTemperature = coreTemperatureMk7;
                        break;
                    case GenerationType.Mk6:
                        baseCoreTemperature = coreTemperatureMk6;
                        break;
                    case GenerationType.Mk5:
                        baseCoreTemperature = coreTemperatureMk5;
                        break;
                    case GenerationType.Mk4:
                        baseCoreTemperature = coreTemperatureMk4;
                        break;
                    case GenerationType.Mk3:
                        baseCoreTemperature = coreTemperatureMk3;
                        break;
                    case GenerationType.Mk2:
                        baseCoreTemperature = coreTemperatureMk2;
                        break;
                    default:
                        baseCoreTemperature = coreTemperatureMk1;
                        break;
                }

                return baseCoreTemperature * Math.Pow(overheatModifier, 1.5) * EffectiveEmbrittlementEffectRatio * Math.Pow(part.mass / partMass, massCoreTempExp);
            }
        }

        public virtual double MaxCoreTemperature => CoreTemperature;

        public double HotBathTemperature
        {
            get
            {
                if (hotBathTemperature <= 0)
                {
                    switch (CurrentGenerationType)
                    {
                        case GenerationType.Mk7:
                            hotBathTemperature = hotBathTemperatureMk7;
                            break;
                        case GenerationType.Mk6:
                            hotBathTemperature = hotBathTemperatureMk6;
                            break;
                        case GenerationType.Mk5:
                            hotBathTemperature = hotBathTemperatureMk5;
                            break;
                        case GenerationType.Mk4:
                            hotBathTemperature = hotBathTemperatureMk4;
                            break;
                        case GenerationType.Mk3:
                            hotBathTemperature = hotBathTemperatureMk3;
                            break;
                        case GenerationType.Mk2:
                            hotBathTemperature = hotBathTemperatureMk2;
                            break;
                        default:
                            hotBathTemperature = hotBathTemperatureMk1;
                            break;
                    }
                }

                if (hotBathTemperature <= 0)
                    return CoreTemperature * hotBathModifier;
                else
                    return hotBathTemperature;
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

        public double RawPowerOutput
        {
            get
            {
                double rawPowerOutput;

                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7:
                        rawPowerOutput = powerOutputMk7;
                        break;
                    case GenerationType.Mk6:
                        rawPowerOutput = powerOutputMk6;
                        break;
                    case GenerationType.Mk5:
                        rawPowerOutput = powerOutputMk5;
                        break;
                    case GenerationType.Mk4:
                        rawPowerOutput = powerOutputMk4;
                        break;
                    case GenerationType.Mk3:
                        rawPowerOutput = powerOutputMk3;
                        break;
                    case GenerationType.Mk2:
                        rawPowerOutput = powerOutputMk2;
                        break;
                    default:
                        rawPowerOutput = powerOutputMk1;
                        break;
                }

                return rawPowerOutput;
            }
        }


        public virtual void StartReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                startDisabled = false;
            }
            else
            {
                if (IsStarted && IsNuclear) return;

                stored_fuel_ratio = 1;
                IsEnabled = true;
                if (_runningSound != null)
                    _runningSound.Play();
            }
        }

        [KSPEvent(groupName = GROUP, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_activateReactor", active = false)]
        public void ActivateReactor()
        {
            Debug.Log("[KSPI]: InterstellarReactor on " + part.name + " was Force Activated");
            part.force_activate();

            Events[nameof(ActivateReactor)].guiActive = false;
            Events[nameof(ActivateReactor)].active = false;

            if (centrifugeHabitat != null && !centrifugeHabitat.isDeployed)
            {
                var message = Localizer.Format("#LOC_KSPIE_Reactor_PostMsg1", part.name);
                ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
                Debug.LogWarning("[KSPI]: " + message);
                return;
            }

            StartReactor();
            IsStarted = true;
        }

        [KSPEvent(groupName = GROUP, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_deactivateReactor", active = true)]
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

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Reactor_enableTritiumBreeding", active = false)]
        public void StartBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = true;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Reactor_disableTritiumBreeding", active = true)]
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

        private bool CanPartUpgradeAlternative()
        {
            if (PluginHelper.PartTechUpgrades == null)
            {
                Debug.Log("[KSPI]: PartTechUpgrades is not initialized");
                return false;
            }

            if (!PluginHelper.PartTechUpgrades.TryGetValue(part.name, out var upgradeTechName))
            {
                Debug.Log("[KSPI]: PartTechUpgrade entry is not found for part '" + part.name + "'");
                return false;
            }

            Debug.Log("[KSPI]: Found matching Interstellar upgradeTech for part '" + part.name + "' with techNode " + upgradeTechName);

            return PluginHelper.UpgradeAvailable(upgradeTechName);
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
            if (powerOutputMk2 <= 0)
                powerOutputMk2 = powerOutputMk1 * 1.5;
            if (powerOutputMk3 <= 0)
                powerOutputMk3 = powerOutputMk2 * 1.5;
            if (powerOutputMk4 <= 0)
                powerOutputMk4 = powerOutputMk3 * 1.5;
            if (powerOutputMk5 <= 0)
                powerOutputMk5 = powerOutputMk4 * 1.5;
            if (powerOutputMk6 <= 0)
                powerOutputMk6 = powerOutputMk5 * 1.5;
            if (powerOutputMk7 <= 0)
                powerOutputMk7 = powerOutputMk6 * 1.5;

            if (minimumThrottleMk1 <= 0)
                minimumThrottleMk1 = minimumThrottle;
            if (minimumThrottleMk2 <= 0)
                minimumThrottleMk2 = minimumThrottleMk1;
            if (minimumThrottleMk3 <= 0)
                minimumThrottleMk3 = minimumThrottleMk2;
            if (minimumThrottleMk4 <= 0)
                minimumThrottleMk4 = minimumThrottleMk3;
            if (minimumThrottleMk5 <= 0)
                minimumThrottleMk5 = minimumThrottleMk4;
            if (minimumThrottleMk6 <= 0)
                minimumThrottleMk6 = minimumThrottleMk5;
            if (minimumThrottleMk7 <= 0)
                minimumThrottleMk7 = minimumThrottleMk6;
        }

        public override void OnStart(StartState state)
        {
            UpdateReactorCharacteristics();

            InitializeKerbalismEmitter();

            hydrogenDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.HydrogenLqd);

            windowPosition = new Rect(windowPositionX, windowPositionY, 300, 100);
            hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
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

            string[] resourcesToSupply = { ResourceSettings.Config.ThermalPowerInMegawatt, ResourceSettings.Config.WasteHeatInMegawatt, ResourceSettings.Config.ChargedParticleInMegawatt, ResourceSettings.Config.ElectricPowerInMegawatt };
            this.resources_to_supply = resourcesToSupply;

            _resourceBuffers = new ResourceBuffers();
            _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, wasteHeatBufferMassMult * wasteHeatBufferMult, true));
            _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ThermalPowerInMegawatt, thermalPowerBufferMult));
            _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ChargedParticleInMegawatt, chargedPowerBufferMult));
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);
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
                    last_active_time = Planetarium.GetUniversalTime() - 4d * PluginHelper.SecondsInDay;
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

            _tritiumDensity = _tritiumDef.density;
            _helium4Density = _heliumDef.density;
            _lithium6Density = _lithium6Def.density;

            _tritiumBreedingMassAdjustment = tritium_molar_mass_ratio * _lithium6Density/ _tritiumDensity;
            _heliumBreedingMassAdjustment = helium_molar_mass_ratio * _lithium6Density / _helium4Density;

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


            centrifugeHabitat = part.FindModuleImplementing<FNHabitat>();

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
                _runningSound.panStereo = 0;
                _runningSound.rolloffMode = AudioRolloffMode.Linear;
                _runningSound.loop = true;
                _runningSound.Stop();
            }

            //soundTerminateFilePath
            if (!string.IsNullOrWhiteSpace(soundTerminateFilePath))
            {
                _terminateSound = gameObject.AddComponent<AudioSource>();
                _terminateSound.clip = GameDatabase.Instance.GetAudioClip(soundTerminateFilePath);
                _terminateSound.volume = 0;
                _terminateSound.panStereo = 0;
                _terminateSound.rolloffMode = AudioRolloffMode.Linear;
                _terminateSound.loop = false;
                _terminateSound.Stop();
            }

            if (!string.IsNullOrWhiteSpace(soundTerminateFilePath))
            {
                _initiateSound = gameObject.AddComponent<AudioSource>();
                _initiateSound.clip = GameDatabase.Instance.GetAudioClip(soundInitiateFilePath);
                _initiateSound.volume = 0;
                _initiateSound.panStereo = 0;
                _initiateSound.rolloffMode = AudioRolloffMode.Linear;
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
            hasSpecificFuelModeTechs =
                !string.IsNullOrEmpty(fuelModeTechReqLevel2)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel3)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel4)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel5)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel6)
                || !string.IsNullOrEmpty(fuelModeTechReqLevel7);

            if (string.IsNullOrEmpty(fuelModeTechReqLevel2))
                fuelModeTechReqLevel2 = upgradeTechReqMk2;
            if (string.IsNullOrEmpty(fuelModeTechReqLevel3))
                fuelModeTechReqLevel3 = upgradeTechReqMk3;
            if (string.IsNullOrEmpty(fuelModeTechReqLevel4))
                fuelModeTechReqLevel4 = upgradeTechReqMk4;
            if (string.IsNullOrEmpty(fuelModeTechReqLevel5))
                fuelModeTechReqLevel5 = upgradeTechReqMk5;
            if (string.IsNullOrEmpty(fuelModeTechReqLevel6))
                fuelModeTechReqLevel6 = upgradeTechReqMk6;
            if (string.IsNullOrEmpty(fuelModeTechReqLevel7))
                fuelModeTechReqLevel7 = upgradeTechReqMk7;

            fuelModeTechLevel = 0;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel2))
                fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel3))
                fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel4))
                fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel5))
                fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel6))
                fuelModeTechLevel++;
            if (PluginHelper.UpgradeAvailable(fuelModeTechReqLevel7))
                fuelModeTechLevel++;
        }

        private void DetermineCoreTemperature()
        {
            // if coreTemperature is missing, first look at legacy value
            if (coreTemperatureMk1 <= 0)
                coreTemperatureMk1 = ReactorTemp;
            if (coreTemperatureMk2 <= 0)
                coreTemperatureMk2 = upgradedReactorTemp;
            if (coreTemperatureMk3 <= 0)
                coreTemperatureMk3 = upgradedReactorTemp * powerUpgradeCoreTempMult;

            // prevent initial values
            if (coreTemperatureMk1 <= 0)
                coreTemperatureMk1 = 2500;
            if (coreTemperatureMk2 <= 0)
                coreTemperatureMk2 = coreTemperatureMk1;
            if (coreTemperatureMk3 <= 0)
                coreTemperatureMk3 = coreTemperatureMk2;
            if (coreTemperatureMk4 <= 0)
                coreTemperatureMk4 = coreTemperatureMk3;
            if (coreTemperatureMk5 <= 0)
                coreTemperatureMk5 = coreTemperatureMk4;
            if (coreTemperatureMk6 <= 0)
                coreTemperatureMk6 = coreTemperatureMk5;
            if (coreTemperatureMk7 <= 0)
                coreTemperatureMk7 = coreTemperatureMk6;
        }

        private void DetermineFuelEfficiency()
        {
            // if fuel efficiency is missing, try to use legacy value
            if (fuelEfficencyMk1 <= 0)
                fuelEfficencyMk1 = fuelEfficiency;

            // prevent any initial values
            if (fuelEfficencyMk1 <= 0)
                fuelEfficencyMk1 = 1;
            if (fuelEfficencyMk2 <= 0)
                fuelEfficencyMk2 = fuelEfficencyMk1;
            if (fuelEfficencyMk3 <= 0)
                fuelEfficencyMk3 = fuelEfficencyMk2;
            if (fuelEfficencyMk4 <= 0)
                fuelEfficencyMk4 = fuelEfficencyMk3;
            if (fuelEfficencyMk5 <= 0)
                fuelEfficencyMk5 = fuelEfficencyMk4;
            if (fuelEfficencyMk6 <= 0)
                fuelEfficencyMk6 = fuelEfficencyMk5;
            if (fuelEfficencyMk7 <= 0)
                fuelEfficencyMk7 = fuelEfficencyMk6;
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
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk7))
                currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk6))
                currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk5))
                currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk4))
                currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk3))
                currentGenerationType++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk2))
                currentGenerationType++;
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

            if (HighLogic.LoadedSceneIsEditor)
            {
                UpdateConnectedReceiversStr();
                reactorSurface = radius * radius;
            }
        }

        protected void UpdateFuelMode()
        {
            fuelModeStr = CurrentFuelMode != null ? CurrentFuelMode.ModeGUIName : "null";
        }

        public override void OnUpdate()
        {
            Events[nameof(StartBreedTritiumEvent)].active = canDisableTritiumBreeding && canBreedTritium && !breedtritium && IsFuelNeutronRich && IsEnabled;
            Events[nameof(StopBreedTritiumEvent)].active = canDisableTritiumBreeding && canBreedTritium && breedtritium && IsFuelNeutronRich && IsEnabled;
            UpdateFuelMode();

            if (IsEnabled && CurrentFuelMode != null)
            {
                if (CheatOptions.InfinitePropellant || stored_fuel_ratio > 0.99)
                    statusStr = Localizer.Format("#LOC_KSPIE_Reactor_status1", powerPcnt.ToString("0.0000"));//"Active (" +  + "%)"
                else if (currentFuelVariant != null)
                    statusStr = currentFuelVariant.ReactorFuels.OrderBy(GetFuelAvailability).First().ResourceName + " " + Localizer.Format("#LOC_KSPIE_Reactor_status2");//"Deprived"
            }
            else
            {
                statusStr = powerPcnt > 0
                    ? Localizer.Format("#LOC_KSPIE_Reactor_status3", powerPcnt.ToString("0.0000"))
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
                return;
            }

            if (!enabled)
                base.OnFixedUpdate();

            if (isFixedUpdatedCalled) return;

            isFixedUpdatedCalled = true;
            UpdateCapacities();
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            double timeWarpFixedDeltaTime = TimeWarp.fixedDeltaTime;
            if (!IsEnabled && !IsStarted)
            {
                IsStarted = true;
                IsEnabled = true;
            }

            base.OnFixedUpdate();

            StoreGeneratorRequests(timeWarpFixedDeltaTime);

            decay_ongoing = false;

            maximumPower = MaximumPower;

            if (IsEnabled && maximumPower > 0)
            {
                maxPowerToSupply = Math.Max(maximumPower * timeWarpFixedDeltaTime, 0);

                UpdateGeeforceModifier();

                if (hasOverheatEffects && !CheatOptions.IgnoreMaxTemperature)
                {
                    averageOverheat.Enqueue(getResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt));
                    if (averageOverheat.Count > 10)
                        averageOverheat.Dequeue();

                    var scaledOverheating = Math.Pow(Math.Max(getResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt) - overheatTreshHold, 0) * overheatMultiplier, overheatExponent);

                    overheatModifier = Math.Min(Math.Max(1 - scaledOverheating, minOverheatModifier), 1);
                }
                else
                    overheatModifier = 1;

                currentFuelVariantsSorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, maxPowerToSupply * geeForceModifier * overheatModifier, fuelUsePerMJMult);
                currentFuelVariant = currentFuelVariantsSorted.FirstOrDefault();

                fuel_mode_variant = currentFuelVariant?.Name;

                stored_fuel_ratio = CheatOptions.InfinitePropellant ? 1 : currentFuelVariant != null ? Math.Min(currentFuelVariant.FuelRatio, 1) : 0;

                LookForAlternativeFuelTypes();

                UpdateCapacities();

                var trueVariant = CurrentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, maxPowerToSupply, fuelUsePerMJMult, false).FirstOrDefault();
                fuel_ratio = CheatOptions.InfinitePropellant ? 1 : trueVariant != null ? Math.Min(trueVariant.FuelRatio, 1) : 0;

                if (fuel_ratio < 0.99999)
                {
                    if (!messagedRanOutOfFuel)
                    {
                        messagedRanOutOfFuel = true;
                        var message = Localizer.Format("#LOC_KSPIE_Reactor_ranOutOfFuelFor") + " " + CurrentFuelMode.ModeGUIName;
                        Debug.Log("[KSPI]: " + message);
                        ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
                else
                    messagedRanOutOfFuel = false;

                thermalThrottleRatio = connectedEngines.Any(m => m.RequiresThermalHeat) ? Math.Min(1, connectedEngines.Where(m => m.RequiresThermalHeat).Sum(e => e.CurrentThrottle)) : 0;
                plasmaThrottleRatio = connectedEngines.Any(m => m.RequiresPlasmaHeat) ? Math.Min(1, connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.CurrentThrottle)) : 0;
                chargedThrottleRatio = connectedEngines.Any(m => m.RequiresChargedPower) ? Math.Min(1, connectedEngines.Where(m => m.RequiresChargedPower).Max(e => e.CurrentThrottle)) : 0;

                var thermalPropulsionRatio = ThermalPropulsionEfficiency * thermalThrottleRatio;
                var plasmaPropulsionRatio = PlasmaPropulsionEfficiency * plasmaThrottleRatio;
                var chargedPropulsionRatio = ChargedParticlePropulsionEfficiency * chargedThrottleRatio;

                var thermalGeneratorRatio = thermalEnergyEfficiency * storedGeneratorThermalEnergyRequestRatio;
                var plasmaGeneratorRatio = plasmaEnergyEfficiency * storedGeneratorPlasmaEnergyRequestRatio;
                var chargedGeneratorRatio = chargedParticleEnergyEfficiency * storedGeneratorChargedEnergyRequestRatio;

                _propulsionRequestRatioSum = Math.Min(1, thermalPropulsionRatio + plasmaPropulsionRatio + chargedPropulsionRatio);

                maximum_thermal_request_ratio = Math.Min(thermalPropulsionRatio + plasmaPropulsionRatio + thermalGeneratorRatio + plasmaGeneratorRatio, 1);
                maximum_charged_request_ratio = Math.Min(chargedPropulsionRatio + chargedGeneratorRatio, 1);

                maximum_reactor_request_ratio = Math.Max(maximum_thermal_request_ratio, maximum_charged_request_ratio);

                var powerAccessModifier = Math.Max(
                    Math.Max(
                        connectedEngines.Any(m => !m.RequiresChargedPower) ? 1 : 0,
                        connectedEngines.Any(m => m.RequiresChargedPower) ? 1 : 0),
                   Math.Max(
                        Math.Max(storedIsThermalEnergyGeneratorEfficiency > 0 ? 1 : 0, storedIsPlasmaEnergyGeneratorEfficiency > 0 ? 1 : 0),
                        storedIsChargedEnergyGeneratorEfficiency > 0 ? 1 : 0
                   ));

                maximumChargedPower = MaximumChargedPower;
                maximumThermalPower = MaximumThermalPower;

                var maxStoredGeneratorEnergyRequestedRatio = Math.Max(Math.Max(storedGeneratorThermalEnergyRequestRatio, storedGeneratorPlasmaEnergyRequestRatio), storedGeneratorChargedEnergyRequestRatio);
                var maxThrottleRatio = Math.Max(Math.Max(thermalThrottleRatio, plasmaThrottleRatio), chargedThrottleRatio);

                power_request_ratio = Math.Max(maxThrottleRatio, maxStoredGeneratorEnergyRequestedRatio);

                maxChargedToSupplyPerSecond = maximumChargedPower * stored_fuel_ratio * geeForceModifier * overheatModifier * powerAccessModifier;
                requestedChargedToSupplyPerSecond = maxChargedToSupplyPerSecond * power_request_ratio * maximum_charged_request_ratio;

                var chargedParticlesManager = getManagerForVessel(ResourceSettings.Config.ChargedParticleInMegawatt);
                var thermalHeatManager = getManagerForVessel(ResourceSettings.Config.ThermalPowerInMegawatt);

                minThrottle = stored_fuel_ratio > 0 ? MinimumThrottle / stored_fuel_ratio : 1;
                var neededChargedPowerPerSecond = getNeededPowerSupplyPerSecondWithMinimumRatio(maxChargedToSupplyPerSecond, minThrottle, ResourceSettings.Config.ChargedParticleInMegawatt, chargedParticlesManager);
                charged_power_ratio = Math.Min(maximum_charged_request_ratio, maximumChargedPower > 0 ? neededChargedPowerPerSecond / maximumChargedPower : 0);

                maxThermalToSupplyPerSecond = maximumThermalPower * stored_fuel_ratio * geeForceModifier * overheatModifier * powerAccessModifier;
                requestedThermalToSupplyPerSecond = maxThermalToSupplyPerSecond * power_request_ratio * maximum_thermal_request_ratio;

                var neededThermalPowerPerSecond = getNeededPowerSupplyPerSecondWithMinimumRatio(maxThermalToSupplyPerSecond, minThrottle, ResourceSettings.Config.ThermalPowerInMegawatt, thermalHeatManager);
                requested_thermal_power_ratio =  maximumThermalPower > 0 ? neededThermalPowerPerSecond / maximumThermalPower : 0;
                thermal_power_ratio = Math.Min(maximum_thermal_request_ratio, requested_thermal_power_ratio);

                reactor_power_ratio = Math.Min(overheatModifier * maximum_reactor_request_ratio, PowerRatio);

                ongoing_charged_power_generated = managedProvidedPowerSupplyPerSecondMinimumRatio(requestedChargedToSupplyPerSecond, maxChargedToSupplyPerSecond, reactor_power_ratio, ResourceSettings.Config.ChargedParticleInMegawatt, chargedParticlesManager);
                ongoing_thermal_power_generated = managedProvidedPowerSupplyPerSecondMinimumRatio(requestedThermalToSupplyPerSecond, maxThermalToSupplyPerSecond, reactor_power_ratio, ResourceSettings.Config.ThermalPowerInMegawatt, thermalHeatManager);
                ongoing_total_power_generated = ongoing_thermal_power_generated + ongoing_charged_power_generated;

                var totalPowerReceivedFixed = ongoing_total_power_generated * timeWarpFixedDeltaTime;

                UpdateEmbrittlement(Math.Max(thermalThrottleRatio, plasmaThrottleRatio), timeWarpFixedDeltaTime);

                ongoing_consumption_rate = maximumPower > 0 ? ongoing_total_power_generated / maximumPower : 0;
                PluginHelper.SetAnimationRatio((float)Math.Pow(ongoing_consumption_rate, animExponent), pulseAnimation);
                powerPcnt = 100 * ongoing_consumption_rate;

                // produce wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    // skip first frame of wasteheat production
                    var delayedWasteheatRate = ongoing_consumption_rate > ongoing_wasteheat_rate ? Math.Min(ongoing_wasteheat_rate, ongoing_consumption_rate) : ongoing_consumption_rate;

                    supplyFNResourcePerSecondWithMax(delayedWasteheatRate * maximumPower, StableMaximumReactorPower, ResourceSettings.Config.WasteHeatInMegawatt);

                    ongoing_wasteheat_rate = ongoing_consumption_rate;
                }

                // consume fuel
                if (!CheatOptions.InfinitePropellant)
                {
                    _consumedFuelTotalFixed = 0;

                    foreach (var reactorFuel in currentFuelVariant.ReactorFuels)
                    {
                        _consumedFuelTotalFixed += ConsumeReactorFuel(reactorFuel, totalPowerReceivedFixed / geeForceModifier);
                    }

                    // refresh production list
                    reactorProduction.Clear();

                    // produce reactor products
                    foreach (var product in currentFuelVariant.ReactorProducts)
                    {
                        var massProduced = ProduceReactorProduct(product, totalPowerReceivedFixed / geeForceModifier);
                        if (product.IsPropellant)
                            reactorProduction.Add(new ReactorProduction() { fuelmode = product, mass = massProduced });
                    }
                }

                BreedTritium(ongoing_thermal_power_generated, timeWarpFixedDeltaTime);

                if (Planetarium.GetUniversalTime() != 0)
                    last_active_time = Planetarium.GetUniversalTime();
            }
            else if (!IsEnabled && IsNuclear && MaximumPower > 0 && (Planetarium.GetUniversalTime() - last_active_time <= 3 * PluginHelper.SecondsInDay))
            {
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                var powerFraction = 0.1 * Math.Exp(-(Planetarium.GetUniversalTime() - last_active_time) / PluginHelper.SecondsInDay / 24.0 * 9.0);
                var powerToSupply = Math.Max(MaximumPower * powerFraction, 0);
                ongoing_thermal_power_generated = supplyManagedFNResourcePerSecondWithMinimumRatio(powerToSupply, 1, ResourceSettings.Config.ThermalPowerInMegawatt);
                ongoing_total_power_generated = ongoing_thermal_power_generated;
                BreedTritium(ongoing_thermal_power_generated, timeWarpFixedDeltaTime);
                ongoing_consumption_rate = MaximumPower > 0 ? ongoing_thermal_power_generated / MaximumPower : 0;
                powerPcnt = 100 * ongoing_consumption_rate;
                decay_ongoing = true;
            }
            else
            {
                currentFuelVariantsSorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(part, FuelEfficiency, NormalisedMaximumPower, fuelUsePerMJMult);
                currentFuelVariant = currentFuelVariantsSorted.FirstOrDefault();
                fuel_mode_variant = currentFuelVariant?.Name;
                stored_fuel_ratio = CheatOptions.InfinitePropellant ? 1 : currentFuelVariant != null ? Math.Min(currentFuelVariant.FuelRatio, 1) : 0;

                ongoing_total_power_generated = 0;
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                powerPcnt = 0;
            }

            UpdatePlayedSound();

            _previousReactorPowerRatio = reactor_power_ratio;

            if (IsEnabled) return;

            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ThermalPowerInMegawatt, 0);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ChargedParticleInMegawatt, 0);
            _resourceBuffers.UpdateBuffers();
        }

        private void UpdatePlayedSound()
        {
            var scaledPitchRatio = Math.Pow(reactor_power_ratio, soundRunningPitchExp);
            var scaledVolumeRatio = Math.Pow(reactor_power_ratio, soundRunningVolumeExp);

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

            if (_previousReactorPowerRatio > 0 && reactor_power_ratio <= 0)
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
            else if (_previousReactorPowerRatio <= 0 && reactor_power_ratio > 0)
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
            else if (_previousReactorPowerRatio > 0 && reactor_power_ratio > 0 && _runningSound != null)
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
                averageGeeforce.Enqueue(vessel.geeForce);
                if (averageGeeforce.Count > 10)
                    averageGeeforce.Dequeue();

                currentGeeForce = vessel.geeForce > 0 && averageGeeforce.Any() ? averageGeeforce.Average() : 0;

                if (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ESCAPING)
                {
                    var engines = vessel.FindPartModulesImplementing<ModuleEngines>();
                    if (engines.Any())
                    {
                        var totalThrust = engines.Sum(m => m.realIsp * m.requestedMassFlow * GameConstants.STANDARD_GRAVITY * Vector3d.Dot(m.part.transform.up, vessel.transform.up));
                        currentGeeForce = Math.Max(currentGeeForce, totalThrust / vessel.totalMass / GameConstants.STANDARD_GRAVITY);
                    }
                }

                var geeforce = double.IsNaN(currentGeeForce) || double.IsInfinity(currentGeeForce) ? 0 : currentGeeForce;

                var scaledGeeforce = Math.Pow(Math.Max(geeforce - geeForceTreshHold, 0) * geeForceMultiplier, geeForceExponent);

                geeForceModifier = Math.Min(Math.Max(1 - scaledGeeforce, minGeeForceModifier), 1);
            }
            else
                geeForceModifier = 1;
        }

        private void UpdateEmbrittlement(double thermalPlasmaRatio, double timeWarpFixedDeltaTime)
        {
            var hasActiveNeutronAbsorption = connectedEngines.All(m => m.PropellantAbsorbsNeutrons) && thermalPlasmaRatio > 0;
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
            stored_fuel_ratio = currentFuelVariant.FuelRatio;
        }

        private void StoreGeneratorRequests(double timeWarpFixedDeltaTime)
        {
            storedIsThermalEnergyGeneratorEfficiency = _currentIsThermalEnergyGeneratorEfficiency;
            storedIsPlasmaEnergyGeneratorEfficiency = _currentIsPlasmaEnergyGeneratorEfficiency;
            storedIsChargedEnergyGeneratorEfficiency = _currentIsChargedEnergyGeneratorEfficiency;

            _currentIsThermalEnergyGeneratorEfficiency = 0;
            _currentIsPlasmaEnergyGeneratorEfficiency = 0;
            _currentIsChargedEnergyGeneratorEfficiency = 0;

            var previousStoredRatio = Math.Max(Math.Max(storedGeneratorThermalEnergyRequestRatio, storedGeneratorPlasmaEnergyRequestRatio), storedGeneratorChargedEnergyRequestRatio);

            storedGeneratorThermalEnergyRequestRatio = Math.Max(storedGeneratorThermalEnergyRequestRatio, previousStoredRatio);
            storedGeneratorPlasmaEnergyRequestRatio = Math.Max(storedGeneratorPlasmaEnergyRequestRatio, previousStoredRatio);
            storedGeneratorChargedEnergyRequestRatio = Math.Max(storedGeneratorChargedEnergyRequestRatio, previousStoredRatio);

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

            _currentGeneratorThermalEnergyRequestRatio = 0;
            _currentGeneratorPlasmaEnergyRequestRatio = 0;
            _currentGeneratorChargedEnergyRequestRatio = 0;
        }

        private void UpdateCapacities()
        {
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ThermalPowerInMegawatt, MaximumThermalPower);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ChargedParticleInMegawatt, MaximumChargedPower);
            _resourceBuffers.UpdateBuffers();
        }

        protected double GetFuelRatio(ReactorFuel reactorFuel, double efficiency, double megajoules)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            var fuelUseForPower = reactorFuel.GetFuelUseForPower(efficiency, megajoules, fuelUsePerMJMult);

            return fuelUseForPower > 0 ? GetFuelAvailability(reactorFuel) / fuelUseForPower : 0;
        }

        private void BreedTritium(double neutronPowerReceivedEachSecond, double fixedDeltaTime)
        {
            _lithiumConsumedPerSecond = 0;
            _tritiumProducedPerSecond = 0;
            _heliumProducedPerSecond = 0;
            totalAmountLithium = 0;
            totalMaxAmountLithium = 0;

            if (breedtritium == false || neutronPowerReceivedEachSecond <= 0 || fixedDeltaTime <= 0)
                return;

            // verify if there is any lithium6 present
            var partResourceLithium6 = part.Resources[ResourceSettings.Config.Lithium6];
            if (partResourceLithium6 == null)
                return;

            totalAmountLithium = partResourceLithium6.amount;
            totalMaxAmountLithium = partResourceLithium6.maxAmount;

            if (totalAmountLithium.IsInfinityOrNaNorZero() || totalMaxAmountLithium.IsInfinityOrNaNorZero())
                return;

            lithiumNeutronAbsorbtion = CheatOptions.UnbreakableJoints ? 1 : Math.Max(0.01, Math.Sqrt(totalAmountLithium / totalMaxAmountLithium) - 0.0001);

            if (lithiumNeutronAbsorbtion <= 0.01)
                return;

            // calculate current maximum lithium consumption
            var breedRate = CurrentFuelMode.TritiumBreedModifier * CurrentFuelMode.NeutronsRatio * _staticBreedRate * neutronPowerReceivedEachSecond * fixedDeltaTime * lithiumNeutronAbsorbtion;
            var lithiumRate = breedRate / _lithium6Density;

            // get spare room tritium
            var spareRoomTritiumAmount = part.GetResourceSpareCapacity(_tritiumDef);

            // limit lithium consumption to maximum tritium storage
            var maximumTritiumProduction = lithiumRate * _tritiumBreedingMassAdjustment;
            var maximumLithiumConsumptionRatio = maximumTritiumProduction > 0 ? Math.Min(maximumTritiumProduction, spareRoomTritiumAmount) / maximumTritiumProduction : 0;
            var lithiumRequest = lithiumRate * maximumLithiumConsumptionRatio;

            // consume the lithium
            var lithiumUsed = CheatOptions.InfinitePropellant
                ? lithiumRequest
                : part.RequestResource(_lithium6Def.id, lithiumRequest, ResourceFlowMode.STACK_PRIORITY_SEARCH);

            // calculate effective lithium used for tritium breeding
            _lithiumConsumedPerSecond = lithiumUsed / fixedDeltaTime;

            // calculate products
            var tritiumProduction = lithiumUsed * _tritiumBreedingMassAdjustment;
            var heliumProduction = lithiumUsed * _heliumBreedingMassAdjustment;

            // produce tritium and helium
            _tritiumProducedPerSecond = CheatOptions.InfinitePropellant
                ? tritiumProduction / fixedDeltaTime
                : -part.RequestResource(_tritiumDef.name, -tritiumProduction) / fixedDeltaTime;

            _heliumProducedPerSecond = CheatOptions.InfinitePropellant
                ? heliumProduction / fixedDeltaTime
                : -part.RequestResource(_heliumDef.name, -heliumProduction) / fixedDeltaTime;
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
                if (!string.IsNullOrEmpty(upgradeTechReqMk2))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk2)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk3)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk4)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk5)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk6)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk7)));
                sb.AppendLine("</size>");
            }

            if (thermalEnergyEfficiency > 0 || plasmaEnergyEfficiency > 0 || chargedParticleEnergyEfficiency > 0)
            {

                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_ReactorPower")).AppendLine(":</color><size=10>");
                sb.Append("Mk1: ").AppendLine(PluginHelper.getFormattedPowerString(powerOutputMk1));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2))
                    sb.Append("Mk2: ").AppendLine(PluginHelper.getFormattedPowerString(powerOutputMk2));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3))
                    sb.Append("Mk3: ").AppendLine(PluginHelper.getFormattedPowerString(powerOutputMk3));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4))
                    sb.Append("Mk4: ").AppendLine(PluginHelper.getFormattedPowerString(powerOutputMk4));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5))
                    sb.Append("Mk5: ").AppendLine(PluginHelper.getFormattedPowerString(powerOutputMk5));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6))
                    sb.Append("Mk6: ").AppendLine(PluginHelper.getFormattedPowerString(powerOutputMk6));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7))
                    sb.Append("Mk7: ").AppendLine(PluginHelper.getFormattedPowerString(powerOutputMk7));
                sb.AppendLine("</size>");
            }

            if (hasSpecificFuelModeTechs)
            {
                sb.AppendLine(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_fuelModeUpgradeTechnologies")).AppendLine(":</color><size=10>");
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel2) && fuelModeTechReqLevel2 != "none")
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel2)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel3) && fuelModeTechReqLevel3 != "none")
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel3)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel4) && fuelModeTechReqLevel4 != "none")
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel4)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel5) && fuelModeTechReqLevel5 != "none")
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel5)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel6) && fuelModeTechReqLevel6 != "none")
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel6)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel7) && fuelModeTechReqLevel7 != "none")
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel7)));
                sb.AppendLine("</size>");
            }

            var maximumFuelTechLevel = GetMaximumFuelTechLevel();
            var fuelGroups = GetFuelGroups(maximumFuelTechLevel);

            if (fuelGroups.Count > 1)
            {
                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_getInfoFuelModes")).AppendLine(":</color><size=10>");
                foreach (var group in fuelGroups)
                {
                     sb.Append("Mk").Append(Math.Max(0, 1 + group.TechLevel - reactorModeTechBonus)).Append(": ").AppendLine(Localizer.Format(group.ModeGUIName));
                }
                sb.AppendLine("</size>");
            }

            if (plasmaPropulsionEfficiency > 0)
            {
                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_plasmaNozzlePerformance")).AppendLine(":</color><size=10>");
                sb.Append("Mk1: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk1, powerOutputMk1));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2))
                    sb.Append("Mk2: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk2, powerOutputMk2));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3))
                    sb.Append("Mk3: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk3, powerOutputMk3));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4))
                    sb.Append("Mk4: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk4, powerOutputMk4));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5))
                    sb.Append("Mk5: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk5, powerOutputMk5));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6))
                    sb.Append("Mk6: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk6, powerOutputMk6));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7))
                    sb.Append("Mk7: ").AppendLine(PlasmaNozzlePerformance(coreTemperatureMk7, powerOutputMk7));
                sb.AppendLine("</size>");
            }

            if (thermalPropulsionEfficiency > 0)
            {
                sb.Append(headerColor).Append(Localizer.Format("#LOC_KSPIE_Reactor_thermalNozzlePerformance")).AppendLine(":</color><size=10>");
                sb.Append("Mk1: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk1, powerOutputMk1));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2))
                    sb.Append("Mk2: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk2, powerOutputMk2));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3))
                    sb.Append("Mk3: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk3, powerOutputMk3));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4))
                    sb.Append("Mk4: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk4, powerOutputMk4));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5))
                    sb.Append("Mk5: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk5, powerOutputMk5));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6))
                    sb.Append("Mk6: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk6, powerOutputMk6));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7))
                    sb.Append("Mk7: ").AppendLine(ThermalNozzlePerformance(coreTemperatureMk7, powerOutputMk7));
                sb.AppendLine("</size>");
            }

            return sb.ToStringAndRelease();
        }

        private List<ReactorFuelType> GetFuelGroups(int maximumFuelTechLevel)
        {
            var groups = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE")
                .Select(node => new ReactorFuelMode(node))
                .Where(fm =>
                       fm.AllFuelResourcesDefinitionsAvailable && fm.AllProductResourcesDefinitionsAvailable
                    && (fm.SupportedReactorTypes & ReactorType) == ReactorType
                    && maximumFuelTechLevel >= fm.TechLevel
                    && FusionEnergyGainFactor >= fm.MinimumFusionGainFactor
                    && (fm.Aneutronic || canUseNeutronicFuels)
                    && maxGammaRayPower >= fm.GammaRayEnergy)
                .GroupBy(mode => mode.ModeGUIName).Select(group => new ReactorFuelType(group)).OrderBy(m => m.TechLevel).ToList();
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

            var exhaustVelocity = isp * GameConstants.STANDARD_GRAVITY;

            var thrust = powerInMj * 2000.0 * thermalPropulsionEfficiency / (exhaustVelocity * powerOutputMultiplier);

            return thrust.ToString("F1") + "kN @ " + isp.ToString("F0") + "s";
        }

        private string PlasmaNozzlePerformance(double temperature, double powerInMj)
        {
            var isp = Math.Sqrt(temperature) * 21;

            var exhaustVelocity = isp * GameConstants.STANDARD_GRAVITY;

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

            // determine available variants
            var persistentFuelVariantsSorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(part, FuelEfficiency, deltaTimeDiff * ongoing_total_power_generated, fuelUsePerMJMult);

            // consume fuel
            foreach (var fuel in persistentFuelVariantsSorted.First().ReactorFuels)
            {
                ConsumeReactorFuel(fuel, deltaTimeDiff * ongoing_total_power_generated);
            }

            // produce reactor products
            foreach (var product in persistentFuelVariantsSorted.First().ReactorProducts)
            {
                ProduceReactorProduct(product, deltaTimeDiff * ongoing_total_power_generated);
            }

            // breed tritium
            BreedTritium(ongoing_total_power_generated * ThermalPowerRatio, deltaTimeDiff);
        }

        protected bool ReactorIsOverheating()
        {
            if (!CheatOptions.IgnoreMaxTemperature && getResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt) >= emergencyPowerShutdownFraction && canShutdown)
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

            var filteredFuelModes = fuelModeConfigs.Select(node => new ReactorFuelMode(node))
                .Where(fm =>
                       fm.AllFuelResourcesDefinitionsAvailable
                    && fm.AllProductResourcesDefinitionsAvailable
                    && (fm.SupportedReactorTypes & ReactorType) == ReactorType
                    && PluginHelper.HasTechRequirementOrEmpty(fm.TechRequirement)
                    && ReactorFuelModeTechLevel >= fm.TechLevel
                    && FusionEnergyGainFactor >= fm.MinimumFusionGainFactor
                    && (fm.Aneutronic || canUseNeutronicFuels)
                    && maxGammaRayPower >= fm.GammaRayEnergy
                    && fm.NeutronsRatio <= maxNeutronsRatio
                    && fm.NeutronsRatio >= minNeutronsRatio
                    ).ToList();

            for (var i = 0; i < filteredFuelModes.Count; i++)
            {
                filteredFuelModes[i].Position = i;
            }

            Debug.Log("[KSPI]: found " + filteredFuelModes.Count + " valid fuel types");

            var groups = filteredFuelModes.GroupBy(mode => mode.ModeGUIName).Select(group => new ReactorFuelType(group)).ToList();

            Debug.Log("[KSPI]: grouped them into " + groups.Count + " valid fuel modes");

            return groups;
        }

        protected bool FuelRequiresLab(bool requiresLab)
        {
            var isConnectedToLab = part.IsConnectedToModule("ScienceModule", 10);

            return !requiresLab || isConnectedToLab && canBeCombinedWithLab;
        }

        public virtual void SetDefaultFuelMode()
        {
            if (fuelModes == null)
            {
                Debug.Log("[KSPI]: SetDefaultFuelMode - load fuel modes");
                fuelModes = GetReactorFuelModes();
            }

            CurrentFuelMode = fuelModes.FirstOrDefault();

            maxPowerToSupply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime, 0);

            if (CurrentFuelMode == null)
                print("[KSPI]: Warning : CurrentFuelMode is null");
            else
                print("[KSPI]: CurrentFuelMode = " + CurrentFuelMode.ModeGUIName);
        }

        protected double ConsumeReactorFuel(ReactorFuel fuel, double powerInMj)
        {
            if (fuel == null)
            {
                Debug.LogError("[KSPI]: ConsumeReactorFuel fuel null");
                return 0;
            }

            if (powerInMj.IsInfinityOrNaNorZero())
                return 0;

            var consumeAmountInUnitOfStorage = FuelEfficiency > 0 ?  powerInMj * fuel.AmountFuelUsePerMJ * fuelUsePerMJMult / FuelEfficiency : 0;

            if (fuel.ConsumeGlobal)
            {
                var result = fuel.Simulate ? 0 : part.RequestResource(fuel.Definition.id, consumeAmountInUnitOfStorage, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                return (fuel.Simulate ? consumeAmountInUnitOfStorage : result) * fuel.DensityInTon;
            }

            if (part.Resources.Contains(fuel.ResourceName))
            {
                double reduction = Math.Min(consumeAmountInUnitOfStorage, part.Resources[fuel.ResourceName].amount);
                part.Resources[fuel.ResourceName].amount -= reduction;
                return reduction * fuel.DensityInTon;
            }
            else
                return 0;
        }

        protected virtual double ProduceReactorProduct(ReactorProduct product, double powerInMj)
        {
            if (product == null)
            {
                Debug.LogError("[KSPI]: ProduceReactorProduct product null");
                return 0;
            }

            if (powerInMj.IsInfinityOrNaNorZero())
                return 0;

            var productSupply = powerInMj * product.AmountProductUsePerMJ * fuelUsePerMJMult / FuelEfficiency;

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                {
                    var partResource = part.Resources[product.ResourceName];
                    var availableStorage = partResource.maxAmount - partResource.amount;
                    var possibleAmount = Math.Min(productSupply, availableStorage);
                    part.Resources[product.ResourceName].amount += possibleAmount;
                    return productSupply * product.DensityInTon;
                }
                else
                    return 0;
            }

            if (!product.Simulate)
                part.RequestResource(product.Definition.id, -productSupply, ResourceFlowMode.STAGE_PRIORITY_FLOW);
            return productSupply * product.DensityInTon;
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

        protected double GetFuelAvailability(PartResourceDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[KSPI]: GetFuelAvailability definition null");
                return 0;
            }

            if (definition.resourceTransferMode == ResourceTransferMode.NONE)
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

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceMaxAvailable(product.Definition);
            else
                return part.FindMaxAmountOfAvailableFuel(product.ResourceName, 4);
        }

        private void InitializeKerbalismEmitter()
        {
            if (!Kerbalism.IsLoaded)
                return;

            emitterController = part.FindModuleImplementing<FNEmitterController>();

            if (emitterController != null)
            {
                emitterController.diameter = radius;
                emitterController.exhaustProducesNeutronRadiation = !mayExhaustInLowSpaceHomeworld;
                emitterController.exhaustProducesGammaRadiation = !mayExhaustInAtmosphereHomeworld;
            }
            else
                Debug.LogWarning("[KSPI]: No Emitter Found om " + part.partInfo.title);
        }

        private void UpdateKerbalismEmitter()
        {
            if (emitterController == null)
                return;

            emitterController.reactorActivityFraction = ongoing_consumption_rate;
            emitterController.fuelNeutronsFraction = CurrentFuelMode.NeutronsRatio;
            emitterController.lithiumNeutronAbsorbtionFraction = lithiumNeutronAbsorbtion;
            emitterController.exhaustActivityFraction = _propulsionRequestRatioSum;
            emitterController.radioactiveFuelLeakFraction = Math.Max(0, 1 - geeForceModifier);

            emitterController.reactorShadowShieldMassProtection = isConnectedToThermalGenerator || isConnectedToChargedGenerator
                ? Math.Max(_currentChargedEnergyGeneratorMass, _currentThermalEnergyGeneratorMass) / (radius * radius) / (RawMaximumPower * 0.001)
                : 0;
        }

        public void OnGUI()
        {
            if (vessel == FlightGlobals.ActiveVessel && render_window)
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
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                if (boldStyle == null)
                    boldStyle = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold, font = PluginHelper.MainFont};

                if (textStyle == null)
                    textStyle = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Normal,font = PluginHelper.MainFont};

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    render_window = false;

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label(TypeName, boldStyle, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (IsFuelNeutronRich)
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_ReactorEmbrittlement"), (100 * (1 - ReactorEmbrittlementConditionRatio)).ToString("0.000000") + "%", boldStyle, textStyle);//"Reactor Embrittlement"

                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_Geeforceoverload") +" ", (100 * (1 - geeForceModifier)).ToString("0.000000") + "%", boldStyle, textStyle);//Geeforce overload
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_Overheating") +" ", (100 * (1 - overheatModifier)).ToString("0.000000") + "%", boldStyle, textStyle);//Overheating

                WindowReactorStatusSpecificOverride();

                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_Radius"), radius + "m", boldStyle, textStyle);//"Radius"
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CoreTemperature"), coretempStr, boldStyle, textStyle);//"Core Temperature"
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_StatusLabel"), statusStr, boldStyle, textStyle);//"Status"
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelMode"), fuelModeStr, boldStyle, textStyle);//"Fuel Mode"
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelEfficiencyLabel"), (FuelEfficiency * 100).ToString(CultureInfo.InvariantCulture), boldStyle, textStyle);//"Fuel efficiency"

                WindowReactorControlSpecificOverride();

                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CurrentMaxPowerOutputLabel"), PluginHelper.getFormattedPowerString(ongoing_total_power_generated) + " / " + PluginHelper.getFormattedPowerString(NormalisedMaximumPower), boldStyle, textStyle);//"Current/Max Power Output"

                if (ChargedPowerRatio < 1.0)
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CurrentMaxThermalPower"), PluginHelper.getFormattedPowerString(ongoing_thermal_power_generated) + " / " + PluginHelper.getFormattedPowerString(MaximumThermalPower), boldStyle, textStyle);//"Current/Max Thermal Power"
                if (ChargedPowerRatio > 0)
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_CurrentMaxChargedPower"), PluginHelper.getFormattedPowerString(ongoing_charged_power_generated) + " / " + PluginHelper.getFormattedPowerString(MaximumChargedPower), boldStyle, textStyle);//"Current/Max Charged Power"

                if (CurrentFuelMode != null && currentFuelVariant.ReactorFuels != null)
                {
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_EnergyProduction"), currentFuelVariant.GigawattPerGram.ToString("0.0") + " GW / g", boldStyle, textStyle);//"Energy Production"
                    PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelUsage"), currentFuelVariant.FuelUseInGramPerTeraJoule.ToString("0.000") + " g / TW", boldStyle, textStyle);//"Fuel Usage"

                    if (IsFuelNeutronRich && breedtritium && canBreedTritium)
                    {
                        PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_FuelNeutronBreedRate"), 100 * CurrentFuelMode.NeutronsRatio + "% ", boldStyle, textStyle);//"Fuel Neutron Breed Rate"

                        var tritiumKgDay = _tritiumProducedPerSecond * _tritiumDensity * 1000 * PluginHelper.SecondsInDay;
                        PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_TritiumBreedRate"), tritiumKgDay.ToString("0.000000") + " " + Localizer.Format("#LOC_KSPIE_Reactor_kgDay") + " ", boldStyle, textStyle);//"Tritium Breed Rate"kg/day

                        var heliumKgDay = _heliumProducedPerSecond * _helium4Density * 1000 * PluginHelper.SecondsInDay;
                        PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_HeliumBreedRate"), heliumKgDay.ToString("0.000000") + " " + Localizer.Format("#LOC_KSPIE_Reactor_kgDay") + " ", boldStyle, textStyle);//"Helium Breed Rate"kg/day

                        part.GetConnectedResourceTotals(_lithium6Def.id, out var totalLithium6Amount, out var totalLithium6MaxAmount);

                        PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_LithiumReserves"), totalLithium6Amount.ToString("0.000") + " L / " + totalLithium6MaxAmount.ToString("0.000") + " L", boldStyle, textStyle);//"Lithium Reserves"

                        var lithiumConsumptionDay = _lithiumConsumedPerSecond * PluginHelper.SecondsInDay;
                        PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_Reactor_LithiumConsumption"), lithiumConsumptionDay.ToString("0.00000") + " "+Localizer.Format("#LOC_KSPIE_Reactor_lithiumConsumptionDay"), boldStyle, textStyle);//"Lithium Consumption"L/day
                        var lithiumLifetimeTotalDays = lithiumConsumptionDay > 0 ? totalLithium6Amount / lithiumConsumptionDay : 0;

                        var lithiumLifetimeYears = Math.Floor(lithiumLifetimeTotalDays / GameConstants.KERBIN_YEAR_IN_DAYS);
                        var lithiumLifetimeYearsRemainderInDays = lithiumLifetimeTotalDays % GameConstants.KERBIN_YEAR_IN_DAYS;

                        var lithiumLifetimeRemainingDays = Math.Floor(lithiumLifetimeYearsRemainderInDays);
                        var lithiumLifetimeRemainingDaysRemainer = lithiumLifetimeYearsRemainderInDays % 1;

                        var lithiumLifetimeRemainingHours = lithiumLifetimeRemainingDaysRemainer * PluginHelper.SecondsInDay / GameConstants.SECONDS_IN_HOUR;

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
                            .Select(d => new { definition = d.resourceDefinition, amount = GetFuelAvailability(d.resourceDefinition), effectiveDensity = d.resourceDefinition.density * d.ratio})
                            .Where(m => m.amount > 0).ToList();

                        var availabilityInTon = availableResources.Sum(m => m.amount * m.effectiveDensity);

                        var variantText = availableResources.Count > 1 ? " (" + availableResources.Count + " variants)" : "";
                        PrintToGuiLayout(fuel.FuelName + " "+ Localizer.Format("#LOC_KSPIE_Reactor_Reserves"), PluginHelper.formatMassStr(availabilityInTon) + variantText, boldStyle, textStyle);//Reserves

                        var tonFuelUsePerHour = ongoing_total_power_generated * fuel.TonsFuelUsePerMJ * fuelUsePerMJMult / FuelEfficiency * PluginHelper.SecondsInHour;
                        var kgFuelUsePerHour = tonFuelUsePerHour * 1000;
                        var kgFuelUsePerDay = kgFuelUsePerHour * PluginHelper.HoursInDay;

                        if (tonFuelUsePerHour > 120)
                            PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Consumption") +" ", PluginHelper.formatMassStr(tonFuelUsePerHour / 60) + " / "+Localizer.Format("#LOC_KSPIE_Reactor_min"), boldStyle, textStyle);//Consumption-min
                        else
                            PrintToGuiLayout(fuel.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Consumption") +" ", PluginHelper.formatMassStr(tonFuelUsePerHour) + " / "+Localizer.Format("#LOC_KSPIE_Reactor_hour"), boldStyle, textStyle);//Consumption--hour

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
                                    var minutesD = fuelLifetimeD * PluginHelper.HoursInDay * 60;
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
                            GUILayout.Label(PluginHelper.formatMassStr(availabilityInTon, "0.00000") + " / " + PluginHelper.formatMassStr(maxAvailabilityInTon, "0.00000"), textStyle, GUILayout.Width(150));
                            GUILayout.EndHorizontal();

                            var hourProductionInTon = ongoing_total_power_generated * product.TonsProductUsePerMJ * fuelUsePerMJMult / FuelEfficiency * PluginHelper.SecondsInHour;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(product.FuelName + " " + Localizer.Format("#LOC_KSPIE_Reactor_Production"), boldStyle, GUILayout.Width(150));//Production
                            GUILayout.Label(PluginHelper.formatMassStr(hourProductionInTon) + " / " + Localizer.Format("#LOC_KSPIE_Reactor_hour"), textStyle, GUILayout.Width(150));//hour
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

            catch (Exception e)
            {
                Debug.LogError("[KSPI]: InterstellarReactor Window(" + windowId + "): " + e.Message);
                throw;
            }
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
