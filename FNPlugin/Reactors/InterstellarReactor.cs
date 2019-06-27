using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Propulsion;
using FNPlugin.Redist;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Reactors
{
    [KSPModule("#LOC_KSPIE_Reactor_moduleName")]
    class InterstellarReactor : ResourceSuppliableModule, IFNPowerSource, IRescalable<InterstellarReactor>, IPartCostModifier
    {
        //public enum ReactorTypes
        //{
        //    FISSION_MSR = 1,
        //    FISSION_GFR = 2,
        //    FUSION_DT = 4,
        //    FUSION_GEN3 = 8,
        //    AIM_FISSION_FUSION = 16,
        //    ANTIMATTER = 32
        //}

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricPowerPriority = 2;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_powerPercentage"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 10)]
        public float powerPercentage = 100;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Forced Minimum Throtle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]
        public float forcedMinimumThrottle = 0;

        // Persistent True
        [KSPField(isPersistant = true)]
        public int fuelmode_index = -1;
        [KSPField(isPersistant = true)]
        public string fuel_mode_name = string.Empty;
        [KSPField(isPersistant = true)]
        public string fuel_mode_variant = string.Empty;

        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool isDeployed = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool breedtritium;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true)]
        public double ongoing_consumption_rate;
        [KSPField(isPersistant = true)]
        public double ongoing_wasteheat_rate;
        [KSPField(isPersistant = true)]
        public bool reactorInit;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_startEnabled"), UI_Toggle(disabledText = "True", enabledText = "False")]
        public bool startDisabled;
        [KSPField(isPersistant = true)]
        public double neutronEmbrittlementDamage;
        [KSPField(isPersistant = true)]
        public double maxEmbrittlementFraction = 0.5;
        [KSPField(isPersistant = true)]
        public float windowPositionX = 20;
        [KSPField(isPersistant = true)]
        public float windowPositionY = 20;
        [KSPField(isPersistant = true)]
        public int currentGenerationType;
        [KSPField(isPersistant = true)]
        public double storedPowerMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double stored_fuel_ratio = 1;
        [KSPField(isPersistant = true)]
        public double fuel_ratio = 1;
        [KSPField(isPersistant = true)]
        public double requested_thermal_power_ratio = 1;
        [KSPField(isPersistant = true)]
        public double maximumThermalPower;
        [KSPField(isPersistant = true)]
        public double maximumChargedPower;

        [KSPField(isPersistant = true)]
        public double thermal_power_ratio = 1;
        [KSPField(isPersistant = true)]
        public double charged_power_ratio = 1;
        [KSPField(isPersistant = true)]
        public double reactor_power_ratio = 1;
        [KSPField(isPersistant = true)]
        public double power_request_ratio;

        [KSPField(isPersistant = true)]
        public double thermal_propulsion_ratio;
        [KSPField(isPersistant = true)]
        public double plasma_propulsion_ratio;
        [KSPField(isPersistant = true)]
        public double charged_propulsion_ratio;

        [KSPField(isPersistant = true)]
        public double maximum_thermal_request_ratio;
        [KSPField(isPersistant = true)]
        public double maximum_charged_request_ratio;
        [KSPField(isPersistant = true)]
        public double maximum_reactor_request_ratio;
        [KSPField(isPersistant = true)]
        public double thermalThrottleRatio;
        [KSPField(isPersistant = true)]
        public double plasmaThrottleRatio;
        [KSPField(isPersistant = true)]
        public double chargedThrottleRatio;

        [KSPField(isPersistant = true)]
        public double storedIsThermalEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)]
        public double storedIsPlasmaEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)]
        public double storedIsChargedEnergyGeneratorEfficiency;

        [KSPField(isPersistant = true)]
        public double storedGeneratorThermalEnergyRequestRatio;
        [KSPField(isPersistant = true)]
        public double storedGeneratorPlasmaEnergyRequestRatio;
        [KSPField(isPersistant = true)]
        public double storedGeneratorChargedEnergyRequestRatio;

        [KSPField(isPersistant = true)]
        public double ongoing_total_power_generated;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_Reactor_thermalPower", guiFormat = "F6")]
        protected double ongoing_thermal_power_generated;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_Reactor_chargedPower ", guiFormat = "F6")]
        protected double ongoing_charged_power_generated;

        [KSPField(guiActive = false, guiName = "Lithium Modifier", guiFormat = "F6")]
        public double lithium_modifier = 1;
       
        [KSPField]
        public float minimumPowerPercentage = 10;
        [KSPField]
        public string upgradeTechReqMk2 = null;
        [KSPField]
        public string upgradeTechReqMk3 = null;
        [KSPField]
        public string upgradeTechReqMk4 = null;
        [KSPField]
        public string upgradeTechReqMk5 = null;
        [KSPField]
        public string upgradeTechReqMk6 = null;
        [KSPField]
        public string upgradeTechReqMk7 = null;

        [KSPField]
        public double minimumThrottleMk1 = 0;
        [KSPField]
        public double minimumThrottleMk2 = 0;
        [KSPField]
        public double minimumThrottleMk3 = 0;
        [KSPField]
        public double minimumThrottleMk4 = 0;
        [KSPField]
        public double minimumThrottleMk5 = 0;
        [KSPField]
        public double minimumThrottleMk6 = 0;
        [KSPField]
        public double minimumThrottleMk7 = 0;

        [KSPField]
        public double fuelEfficencyMk1 = 0;
        [KSPField]
        public double fuelEfficencyMk2 = 0;
        [KSPField]
        public double fuelEfficencyMk3 = 0;
        [KSPField]
        public double fuelEfficencyMk4 = 0;
        [KSPField]
        public double fuelEfficencyMk5 = 0;
        [KSPField]
        public double fuelEfficencyMk6 = 0;
        [KSPField]
        public double fuelEfficencyMk7 = 0;

        [KSPField]
        public double coreTemperatureMk1 = 0;
        [KSPField]
        public double coreTemperatureMk2 = 0;
        [KSPField]
        public double coreTemperatureMk3 = 0;
        [KSPField]
        public double coreTemperatureMk4 = 0;
        [KSPField]
        public double coreTemperatureMk5 = 0;
        [KSPField]
        public double coreTemperatureMk6 = 0;
        [KSPField]
        public double coreTemperatureMk7 = 0;

        [KSPField]
        public double basePowerOutputMk1 = 0;
        [KSPField]
        public double basePowerOutputMk2 = 0;
        [KSPField]
        public double basePowerOutputMk3 = 0;
        [KSPField]
        public double basePowerOutputMk4 = 0;
        [KSPField]
        public double basePowerOutputMk5 = 0;
        [KSPField]
        public double basePowerOutputMk6 = 0;
        [KSPField]
        public double basePowerOutputMk7 = 0;

        [KSPField]
        public double fusionEnergyGainFactorMk1 = 10;
        [KSPField]
        public double fusionEnergyGainFactorMk2;
        [KSPField]
        public double fusionEnergyGainFactorMk3;
        [KSPField]
        public double fusionEnergyGainFactorMk4;
        [KSPField]
        public double fusionEnergyGainFactorMk5;
        [KSPField]
        public double fusionEnergyGainFactorMk6;
        [KSPField]
        public double fusionEnergyGainFactorMk7;

        [KSPField]
        public string fuelModeTechReqLevel2;
        [KSPField]
        public string fuelModeTechReqLevel3;
        [KSPField]
        public string fuelModeTechReqLevel4;
        [KSPField]
        public string fuelModeTechReqLevel5;
        [KSPField]
        public string fuelModeTechReqLevel6;
        [KSPField]
        public string fuelModeTechReqLevel7;

        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk1", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk2", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk2;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk3", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk3;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk4", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk4;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk5", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk5;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk6", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk6;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk7", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk7;

        // Settings
        [KSPField]
        public bool showEngineConnectionInfo = true;
        [KSPField]
        public bool showPowerGeneratorConnectionInfo = true;
        [KSPField]
        public bool mayExhaustInAtmosphereHomeworld = true;
        [KSPField]
        public bool mayExhaustInLowSpaceHomeworld = true;
        [KSPField]
        public double minThermalNozzleTempRequired = 0;
        [KSPField]
        public bool canUseAllPowerForPlasma = true;
        [KSPField]
        public bool updateModuleCost = true;
        [KSPField]
        public int minCoolingFactor = 1;
        [KSPField]
        public double engineHeatProductionMult = 1;
        [KSPField]
        public double plasmaHeatProductionMult = 1;
        [KSPField]
        public double engineWasteheatProductionMult = 1;
        [KSPField]
        public double plasmaWasteheatProductionMult = 1;
        [KSPField]
        public bool supportMHD = false;
        [KSPField]
        public int reactorModeTechBonus = 0;
        [KSPField]
        public bool canBeCombinedWithLab = false;
        [KSPField]
        public bool canBreedTritium = false;
        [KSPField]
        public bool canDisableTritiumBreeding = true;
        [KSPField]
        public bool disableAtZeroThrottle = false;
        [KSPField]
        public bool controlledByEngineThrottle = false;
        [KSPField]
        public bool showShutDownInFlight = false;
        [KSPField]
        public bool showForcedMinimumThrottle = false;
        [KSPField]
        public bool showPowerPercentage = true;
        [KSPField]
        public double powerScaleExponent = 3;
        [KSPField]
        public double costScaleExponent = 1.86325;
        [KSPField]
        public double breedDivider = 100000;
        [KSPField]
        public double effectivePowerMultiplier;
        [KSPField]
        public double bonusBufferFactor = 0.05;
        [KSPField]
        public double thermalPowerBufferMult = 4;
        [KSPField]
        public double chargedPowerBufferMult = 4;
        [KSPField]
        public double massCoreTempExp = 0;
        [KSPField]
        public double massPowerExp = 0;
        [KSPField]
        public double heatTransportationEfficiency = 0.9;
        [KSPField]
        public double ReactorTemp = 0;
        [KSPField]
        public double powerOutputMultiplier = 1;
        [KSPField]
        public double upgradedReactorTemp = 0;
        [KSPField]
        public string animName = "";
        [KSPField]
        public string loopingAnimationName = "";
        [KSPField]
        public string startupAnimationName = "";
        [KSPField]
        public string shutdownAnimationName = "";
        [KSPField]
        public double reactorSpeedMult = 1;
        [KSPField]
        public double powerRatio;
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public string originalName = "";
        [KSPField]
        public float upgradeCost = 0;
        [KSPField(guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_Reactor_connectionRadius")]
        public double radius = 2.5;
        [KSPField]
        public double minimumThrottle = 0;
        [KSPField]
        public bool canShutdown = true;
        [KSPField]
        public int reactorType = 0;
        [KSPField]
        public double fuelEfficiency = 1;
        [KSPField]
        public bool containsPowerGenerator = false;
        [KSPField]
        public double fuelUsePerMJMult = 1;
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public double wasteHeatBufferMassMult = 2.0e+5;
        [KSPField]
        public double wasteHeatBufferMult = 1;
        [KSPField]
        public double hotBathTemperature = 0;
        [KSPField]
        public bool usePropellantBaseIsp = false;
        [KSPField]
        public double emergencyPowerShutdownFraction = 0.99;
        [KSPField]
        public double thermalPropulsionEfficiency = 1;
        [KSPField]
        public double plasmaPropulsionEfficiency = 1;
        [KSPField]
        public double chargedParticlePropulsionEfficiency = 1;

        [KSPField]
        public double thermalEnergyEfficiency = 1;
        [KSPField]
        public double chargedParticleEnergyEfficiency = 1;
        [KSPField] 
        public double plasmaEnergyEfficiency = 1;
        [KSPField]
        public double maxGammaRayPower = 0;

        [KSPField]
        public bool hasBuoyancyEffects = true;
        [KSPField]
        public double geeForceMultiplier = 0.1;
        [KSPField]
        public double geeForceTreshHold = 9;
        [KSPField]
        public double geeForceExponent = 2;
        [KSPField]
        public double minGeeForceModifier = 0.01;

        [KSPField]
        public bool hasOverheatEffects = true;
        [KSPField]
        public double overheatMultiplier = 10;
        [KSPField]
        public double overheatTreshHold = 0.95;
        [KSPField]
        public double overheatExponent = 2;
        [KSPField]
        public double minOverheatModifier = 0.01;


        [KSPField]
        public double neutronEmbrittlementLifepointsMax = 100;
        [KSPField]
        public double neutronEmbrittlementDivider = 1e+9;
        [KSPField]
        public double hotBathModifier = 1;
        [KSPField]
        public double thermalProcessingModifier = 1;
        [KSPField]
        public int supportedPropellantAtoms = GameConstants.defaultSupportedPropellantAtoms;
        [KSPField]
        public int supportedPropellantTypes = GameConstants.defaultSupportedPropellantTypes;
        [KSPField]
        public bool fullPowerForNonNeutronAbsorbants = true;
        [KSPField]
        public bool showPowerPriority = true;
        [KSPField]
        public bool showSpecialisedUI = true;
        [KSPField]
        public bool canUseNeutronicFuels = true;
        [KSPField]
        public bool canUseGammaRayFuels = true;
        [KSPField]
        public double maxNeutronsRatio = 1.04;
        [KSPField]
        public double minNeutronsRatio = 0;

        [KSPField]
        public int fuelModeTechLevel;
        [KSPField]
        public string bimodelUpgradeTechReq = String.Empty;
        [KSPField]
        public string powerUpgradeTechReq = String.Empty;
        [KSPField]
        public double powerUpgradeCoreTempMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_rawPowerOutput", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F4")]
        public double currentRawPowerOutput;

        [KSPField]
        public double PowerOutput = 0;
        [KSPField]
        public double upgradedPowerOutput = 0;
        [KSPField]
        public string upgradeTechReq = String.Empty;
        [KSPField]
        public bool shouldApplyBalance;
        [KSPField]
        public double tritium_molar_mass_ratio = 3.0160 / 7.0183;
        [KSPField]
        public double helium_molar_mass_ratio = 4.0023 / 7.0183;

        // GUI strings
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_coreTemperature")]
        public string coretempStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_reactorStatus")]
        public string statusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorFuelMode")]
        public string fuelModeStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_connectedRecievers")]
        public string connectedRecieversStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorSurface", guiUnits = " m\xB3")]
        public double reactorSurface;

        [KSPField]
        protected double max_power_to_supply = 0;
        [KSPField]
        protected double requested_thermal_to_supply_per_second;
        [KSPField]
        protected double max_thermal_to_supply_per_second;
        [KSPField]
        protected double requested_charged_to_supply_per_second;
        [KSPField]
        protected double max_charged_to_supply_per_second;
        [KSPField]
        protected double min_throttle;
        //[KSPField]
        //protected double safetyThrotleModifier;
        [KSPField]
        public double massCostExponent = 2.5;

        [KSPField(guiActiveEditor = false, guiName = "Initial Cost")]
        public double initialCost;
        [KSPField(guiActiveEditor = false, guiName = "Calculated Cost")]
        public double calculatedCost;
        [KSPField(guiActiveEditor = false, guiName = "Max Resource Cost")]
        public double maxResourceCost;
        [KSPField(guiActiveEditor = false, guiName = "Module Cost")]
        public float moduleCost;

        // Gui
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public float massDifference = 0;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "calibrated mass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorMass", guiUnits = " t")]
        public float currentMass = 0;
        [KSPField]
        public double maximumThermalPowerEffective = 0;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Embrittlement Fraction", guiFormat = "F4")]
        public double embrittlementModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Buoyancy Fraction", guiFormat = "F4")]
        public double geeForceModifier = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Overheat Fraction", guiFormat = "F4")]
        public double overheatModifier = 1;     

        [KSPField]public bool isConnectedToThermalGenerator;
        [KSPField]public bool isConnectedToChargedGenerator;
        [KSPField]public double maxRadiation = 0.01;

        // shared variabels
        protected bool decay_ongoing = false;
        protected bool initialized = false;

        protected double currentGeeForce;
        protected double animationStarted = 0;
        protected double powerPcnt;
        protected double totalAmountLithium = 0;
        protected double totalMaxAmountLithium = 0;  

        protected GUIStyle bold_style;
        protected GUIStyle text_style;
        protected List<ReactorFuelType> fuel_modes;
        protected List<ReactorFuelMode> current_fuel_variants_sorted;
        protected ReactorFuelMode current_fuel_variant;
        protected AnimationState[] pulseAnimation;
        protected ModuleAnimateGeneric startupAnimation;
        protected ModuleAnimateGeneric shutdownAnimation;
        protected ModuleAnimateGeneric loopingAnimation;

        FNHabitat habitat;
        Rect windowPosition;
        ReactorFuelType current_fuel_mode;
        PartResourceDefinition lithium6_def;
        PartResourceDefinition tritium_def;
        PartResourceDefinition helium_def;
        PartResourceDefinition hydrogenDefinition;
        ResourceBuffers resourceBuffers;
        PartModule emitterModule;
        BaseField emitterRadiationField;

        List<ReactorProduction> reactorProduction = new List<ReactorProduction>();
        List<IFNEngineNoozle> connectedEngines = new List<IFNEngineNoozle>();
        Queue<double> averageGeeforce = new Queue<double>();
        Queue<double> averageOverheat = new Queue<double>();
        Dictionary<Guid, double> connectedRecievers = new Dictionary<Guid, double>();
        Dictionary<Guid, double> connectedRecieversFraction = new Dictionary<Guid, double>();

        double tritium_density;
        double helium4_density;
        double lithium6_density;

        double consumedFuelTotalFixed;
        double consumedFuelTotalPerSecond;
        double connectedRecieversSum;

        double tritiumBreedingMassAdjustment;
        double heliumBreedingMassAdjustment;
        double staticBreedRate;

        double currentIsThermalEnergyGeneratorEfficiency;
        double currentIsChargedEnergyGenratorEfficiency;
        double currentIsPlasmaEnergyGeneratorEfficiency;

        double currentGeneratorThermalEnergyRequestRatio;
        double currentGeneratorPlasmaEnergyRequestRatio;
        double currentGeneratorChargedEnergyRequestRatio;

        double lithium_consumed_per_second;
        double tritium_produced_per_second;
        double helium_produced_per_second;

        int windowID = 90175467;
        int deactivate_timer = 0;

        bool currentThermalEnergyGeneratorIsMHD;
        bool hasSpecificFuelModeTechs;
        bool? hasBimodelUpgradeTechReq;
        
        bool isFixedUpdatedCalled;
        bool render_window = false;

        public ReactorFuelType CurrentFuelMode
        {
            get { return current_fuel_mode; }
            set
            {
                current_fuel_mode = value;
                max_power_to_supply = Math.Max(MaximumPower * TimeWarpFixedDeltaTime, 0);
                current_fuel_variants_sorted = current_fuel_mode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, max_power_to_supply, fuelUsePerMJMult);
                current_fuel_variant = current_fuel_variants_sorted.First();

                // persist
                fuelmode_index = current_fuel_mode.Index;
                fuel_mode_name = current_fuel_mode.ModeGUIName;
                fuel_mode_variant = current_fuel_variant.Name;
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
            maxResourceCost = part.Resources.Sum(m => m.maxAmount * m.info.unitCost);

            var dryCost = calculatedCost - initialCost;

            moduleCost = updateModuleCost ? (float)(maxResourceCost + dryCost) : 0;

            return moduleCost;
        }
                

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        // properties
        public double FuelRato { get { return fuel_ratio; } }

        public virtual double MagneticNozzlePowerMult { get { return 1; } }

        public bool MayExhaustInAtmosphereHomeworld { get { return mayExhaustInAtmosphereHomeworld; } }

        public bool MayExhaustInLowSpaceHomeworld { get { return mayExhaustInLowSpaceHomeworld; } }

        public double MinThermalNozzleTempRequired { get { return minThermalNozzleTempRequired; } }

        public virtual double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public bool UsePropellantBaseIsp { get { return usePropellantBaseIsp; } }

        public bool CanUseAllPowerForPlasma { get { return canUseAllPowerForPlasma;} }

        public double MinCoolingFactor { get { return minCoolingFactor; } }

        public double EngineHeatProductionMult { get { return engineHeatProductionMult; } }

        public double PlasmaHeatProductionMult { get { return plasmaHeatProductionMult; } }

        public double EngineWasteheatProductionMult { get { return engineWasteheatProductionMult; } }

        public double PlasmaWasteheatProductionMult { get { return plasmaWasteheatProductionMult; } }

        public double ThermalPropulsionWasteheatModifier { get { return 1; } }

        public double ConsumedFuelFixed { get { return consumedFuelTotalFixed; } }

        public bool SupportMHD { get { return supportMHD; } }

        public double ProducedThermalHeat { get { return ongoing_thermal_power_generated; } }

        public double ProducedChargedPower { get { return ongoing_charged_power_generated; } }

        public int ProviderPowerPriority { get { return (int)electricPowerPriority; } }

        public double RawTotalPowerProduced  { get { return ongoing_total_power_generated; } }

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

            var resultFixed = part.RequestResource(resource.name, -propellantMassPerSecond * ((double)(decimal)TimeWarp.fixedDeltaTime) / ((double)(decimal)resource.density), ResourceFlowMode.ALL_VESSEL);
        }

        public void ConnectWithEngine(IEngineNoozle engine)
        {
            var fnEngine = engine as IFNEngineNoozle;
            if (fnEngine == null)
                return;

            if (!connectedEngines.Contains(fnEngine))
                connectedEngines.Add(fnEngine);
        }

        public void DisconnectWithEngine(IEngineNoozle engine)
        {
            var fnEngine = engine as IFNEngineNoozle;
            if (fnEngine == null)
                return;

            if (connectedEngines.Contains(fnEngine))
                connectedEngines.Remove(fnEngine);
        }

        public GenerationType CurrentGenerationType
        {
            get
            {
                return (GenerationType)currentGenerationType;
            }
            private set
            {
                currentGenerationType = (int)value;
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

        public double ForcedMinimumThrottleRatio { get {  return ((double)(decimal)forcedMinimumThrottle) / 100; } }

        public int SupportedPropellantAtoms { get { return supportedPropellantAtoms; } }

        public int SupportedPropellantTypes { get { return supportedPropellantTypes; } }

        public bool FullPowerForNonNeutronAbsorbants { get { return fullPowerForNonNeutronAbsorbants; } }

        public double EfficencyConnectedThermalEnergyGenerator { get { return storedIsThermalEnergyGeneratorEfficiency; } }

        public double EfficencyConnectedChargedEnergyGenerator { get { return storedIsChargedEnergyGeneratorEfficiency; } }


        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, bool isMHD)
        {
            currentThermalEnergyGeneratorIsMHD = isMHD;

            if (isMHD)
            {
                currentIsPlasmaEnergyGeneratorEfficiency = efficency;
                currentGeneratorPlasmaEnergyRequestRatio = power_ratio;
            }
            else
            {
                currentIsThermalEnergyGeneratorEfficiency = efficency;
                currentGeneratorThermalEnergyRequestRatio = power_ratio;
            }
            isConnectedToThermalGenerator = true;
        }

        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio)
        {
            currentIsThermalEnergyGeneratorEfficiency = efficency;
            currentGeneratorThermalEnergyRequestRatio = power_ratio;
            isConnectedToThermalGenerator = true;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio)
        {
            currentIsChargedEnergyGenratorEfficiency = efficency;
            currentGeneratorChargedEnergyRequestRatio = power_ratio;
            isConnectedToChargedGenerator = true;
        }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType)
        {
            shouldApplyBalance = isConnectedToThermalGenerator && isConnectedToChargedGenerator;
            return shouldApplyBalance;
        }

        public bool IsThermalSource { get { return true; } }

        public double ThermalProcessingModifier { get { return thermalProcessingModifier; } }

        public Part Part { get { return this.part; } }

        public double ProducedWasteHeat { get { return ongoing_total_power_generated; } }

        public void AttachThermalReciever(Guid key, double radius)
        {
            if (!connectedRecievers.ContainsKey(key))
                connectedRecievers.Add(key, radius);
            UpdateConnectedRecieversStr();
        }

        public void DetachThermalReciever(Guid key)
        {
            if (connectedRecievers.ContainsKey(key))
                connectedRecievers.Remove(key);
            UpdateConnectedRecieversStr();
        }

        public double GetFractionThermalReciever(Guid key)
        {
            double result;
            if (connectedRecieversFraction.TryGetValue(key, out result))
                return result;
            else
                return 0;
        }

        public virtual void OnRescale(ScalingFactor factor)
        {
            try
            {
                // calculate multipliers
                Debug.Log("[KSPI]: InterstellarReactor.OnRescale called with " + factor.absolute.linear);
                storedPowerMultiplier = Math.Pow((double)(decimal)factor.absolute.linear, powerScaleExponent);

                initialCost = part.partInfo.cost * Math.Pow((double)(decimal)factor.absolute.linear, massCostExponent);
                calculatedCost = part.partInfo.cost * Math.Pow((double)(decimal)factor.absolute.linear, costScaleExponent);

                // update power
                DeterminePowerOutput();

                // refresh generators mass
                if (ConnectedThermalElectricGenerator != null)
                    ConnectedThermalElectricGenerator.Refresh();
                if (ConnectedChargedParticleElectricGenerator != null)
                    ConnectedChargedParticleElectricGenerator.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: InterstellarReactor.OnRescale" + e.Message);
            }
        }

        private void UpdateConnectedRecieversStr()
        {
            if (connectedRecievers == null) return;

            connectedRecieversSum = connectedRecievers.Sum(r => r.Value * r.Value);
            connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value * a.Value / connectedRecieversSum);

            reactorSurface = Math.Pow(radius, 2);
            connectedRecieversStr = connectedRecievers.Count() + " (" + connectedRecieversSum.ToString("0.000") + " m2)";
        }

        public double ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public bool HasBimodelUpgradeTechReq
        {
            get
            {
                if (hasBimodelUpgradeTechReq == null)
                    hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
                return (bool)hasBimodelUpgradeTechReq;
            }
        }

        public double ChargedParticlePropulsionEfficiency { get { return chargedParticlePropulsionEfficiency; } }

        public double ThermalPropulsionEfficiency { get { return thermalPropulsionEfficiency; } }

        public double PlasmaPropulsionEfficiency { get { return plasmaPropulsionEfficiency; } }

        public double ThermalEnergyEfficiency { get { return thermalEnergyEfficiency; } }

        public double PlasmaEnergyEfficiency { get { return plasmaEnergyEfficiency; } }

        public double ChargedParticleEnergyEfficiency { get { return chargedParticleEnergyEfficiency; } }

        public bool IsSelfContained { get { return containsPowerGenerator; } }

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public double PowerBufferBonus { get { return this.bonusBufferFactor; } }

        public double RawMaximumPower { get { return RawPowerOutput; } }

        public virtual double FuelEfficiency
        {
            get
            {
                double baseEfficency;
                switch (CurrentGenerationType)
                {
                    case GenerationType.Mk7:
                        baseEfficency = fuelEfficencyMk7;
                        break;
                    case GenerationType.Mk6:
                        baseEfficency = fuelEfficencyMk6;
                        break;
                    case GenerationType.Mk5:
                        baseEfficency = fuelEfficencyMk5;
                        break;
                    case GenerationType.Mk4:
                        baseEfficency = fuelEfficencyMk4;
                        break;
                    case GenerationType.Mk3:
                        baseEfficency = fuelEfficencyMk3;
                        break;
                    case GenerationType.Mk2:
                        baseEfficency = fuelEfficencyMk2;
                        break;
                    default:
                        baseEfficency = fuelEfficencyMk1;
                        break;
                }

                return baseEfficency * CurrentFuelMode.FuelEfficencyMultiplier;
            }
        }

        public int ReactorType { get { return reactorType; } }

        public virtual string TypeName { get { return part.partInfo.title; } }

        public virtual double ChargedPowerRatio
        {
            get
            {
                return CurrentFuelMode != null
                    ? CurrentFuelMode.ChargedPowerRatio
                    : 0;
            }
        }

        public double ThermalPowerRatio
        {
            get { return 1 - ChargedPowerRatio; }
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

        public double HotBathTemperature
        {
            get
            {
                if (hotBathTemperature == 0)
                    return CoreTemperature * hotBathModifier;
                else
                    return hotBathTemperature;
            }
        }

        public double EffectiveEmbrittlementEffectRatio
        {
            get 
            {
                embrittlementModifier = CheatOptions.UnbreakableJoints ? 1 : Math.Sin(ReactorEmbrittlemenConditionRatio * Math.PI * 0.5);
                return embrittlementModifier;
            }
        }

        public virtual double ReactorEmbrittlemenConditionRatio
        {
            get { return Math.Min(Math.Max(1 - (neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), maxEmbrittlementFraction), 1); }      
        }

        public virtual double NormalisedMaximumPower
        {
            get { return RawPowerOutput * EffectiveEmbrittlementEffectRatio * (CurrentFuelMode == null ? 1 : CurrentFuelMode.NormalisedReactionRate); }        
        }

        public virtual double MinimumPower { get { return MaximumPower * MinimumThrottle; } }

        public virtual double MaximumThermalPower 
        { 
            get { return PowerRatio * NormalisedMaximumPower * ThermalPowerRatio * geeForceModifier * overheatModifier; } 
        }

        public virtual double MaximumChargedPower 
        {
            get { return PowerRatio * NormalisedMaximumPower * ChargedPowerRatio * geeForceModifier * overheatModifier; }
        }

        public double ReactorSpeedMult { get { return reactorSpeedMult; } }

        public virtual bool CanProducePower { get { return stored_fuel_ratio > 0; } }

        public virtual bool IsNuclear { get { return false; } }

        public virtual bool IsActive { get { return IsEnabled; } }

        public virtual bool IsVolatileSource { get { return false; } }

        public virtual bool IsFuelNeutronRich { get { return false; } }

        public virtual double MaximumPower { get { return MaximumThermalPower + MaximumChargedPower; } }

        public virtual double StableMaximumReactorPower { get { return IsEnabled ? NormalisedMaximumPower : 0; } }

        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

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

        public int ReactorFuelModeTechLevel
        {
            get
            {
                return fuelModeTechLevel + reactorModeTechBonus;
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
                if (IsNuclear) return;

                stored_fuel_ratio = 1;
                IsEnabled = true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Reactor_reactorControlWindow", active = true, guiActiveUnfocused = true, unfocusedRange = 5f, guiActiveUncommand = true)]
        public void ToggleReactorControlWindow()
        {
            render_window = !render_window;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_activateReactor", active = false)]
        public void ActivateReactor()
        {
            Debug.Log("[KSPI]: Reactor on " + part.name + " was Force Activated by user");
            this.part.force_activate();

            if (habitat != null && !habitat.isDeployed)
            {
                string message = "Activation was canceled because " + part.name + " is not deployed";
                ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
                Debug.LogWarning("[KSPI]: " + message);
                return;
            }

            StartReactor();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_deactivateReactor", active = true)]
        public void DeactivateReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                startDisabled = true;
            else
            {
                if (IsNuclear) return;

                IsEnabled = false;
            }
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Reactor_enableTritiumBreeding", active = false)]
        public void StartBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Reactor_disableTritiumBreeding", active = true)]
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
                print("[KSPI]: PartTechUpgrades is not initialized");
                return false;
            }

            string upgradetechName;
            if (!PluginHelper.PartTechUpgrades.TryGetValue(part.name, out upgradetechName))
            {
                print("[KSPI]: PartTechUpgrade entry is not found for part '" + part.name + "'");
                return false;
            }

            print("[KSPI]: Found matching Interstellar upgradetech for part '" + part.name + "' with technode " + upgradetechName);

            return PluginHelper.UpgradeAvailable(upgradetechName);
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

            // initialise power output when missing
            if (powerOutputMk2 == 0)
                powerOutputMk2 = powerOutputMk1 * 1.5;
            if (powerOutputMk3 == 0)
                powerOutputMk3 = powerOutputMk2 * 1.5;
            if (powerOutputMk4 == 0)
                powerOutputMk4 = powerOutputMk3 * 1.5;
            if (powerOutputMk5 == 0)
                powerOutputMk5 = powerOutputMk4 * 1.5;
            if (powerOutputMk6 == 0)
                powerOutputMk6 = powerOutputMk5 * 1.5;
            if (powerOutputMk7 == 0)
                powerOutputMk7 = powerOutputMk6 * 1.5;

            if (minimumThrottleMk1 == 0)
                minimumThrottleMk1 = minimumThrottle;
            if (minimumThrottleMk2 == 0)
                minimumThrottleMk2 = minimumThrottleMk1;
            if (minimumThrottleMk3 == 0)
                minimumThrottleMk3 = minimumThrottleMk2;
            if (minimumThrottleMk4 == 0)
                minimumThrottleMk4 = minimumThrottleMk3;
            if (minimumThrottleMk5 == 0)
                minimumThrottleMk5 = minimumThrottleMk4;
            if (minimumThrottleMk6 == 0)
                minimumThrottleMk6 = minimumThrottleMk5;
            if (minimumThrottleMk7 == 0)
                minimumThrottleMk7 = minimumThrottleMk6;
        }

        public override void OnStart(PartModule.StartState state)
        {
            UpdateReactorCharacteristics();

            InitializeKerbalismEmitter();

            hydrogenDefinition = PartResourceLibrary.Instance.GetDefinition("LqdHydrogen");

            windowPosition = new Rect(windowPositionX, windowPositionY, 300, 100);
            hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
            staticBreedRate = 1 / powerOutputMultiplier / breedDivider / GameConstants.tritiumBreedRate;

            var powerPercentageField = Fields["powerPercentage"];
            powerPercentageField.guiActive = showPowerPercentage;
            UI_FloatRange[] powerPercentageFloatRange = { powerPercentageField.uiControlFlight as UI_FloatRange, powerPercentageField.uiControlEditor as UI_FloatRange };
            powerPercentageFloatRange[0].minValue = minimumPowerPercentage;
            powerPercentageFloatRange[1].minValue = minimumPowerPercentage;

            if (!part.Resources.Contains(ResourceManager.FNRESOURCE_THERMALPOWER))
            {
                var node = new ConfigNode("RESOURCE");
                node.AddValue("name", ResourceManager.FNRESOURCE_THERMALPOWER);
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

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_THERMALPOWER, ResourceManager.FNRESOURCE_WASTEHEAT, ResourceManager.FNRESOURCE_CHARGED_PARTICLES, ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, wasteHeatBufferMassMult * wasteHeatBufferMult, true));
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_THERMALPOWER, thermalPowerBufferMult));
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_CHARGED_PARTICLES, chargedPowerBufferMult));
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.Init(this.part);

            windowID = new System.Random(part.GetInstanceID()).Next(int.MaxValue);
            base.OnStart(state);

            // configure reactor modes
            fuel_modes = GetReactorFuelModes();
            SetDefaultFuelMode();
            UpdateFuelMode();

            if (state == StartState.Editor)
            {
                maximumThermalPowerEffective = MaximumThermalPower;
                coretempStr = CoreTemperature.ToString("0") + " K";
                return;
            }

            if (!reactorInit)
            {
                if (startDisabled)
                {
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

            tritium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.TritiumGas);
            helium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Helium4Gas);
            lithium6_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Lithium6);

            tritium_density = (double)(decimal)tritium_def.density;
            helium4_density = (double)(decimal)helium_def.density;
            lithium6_density = (double)(decimal)lithium6_def.density;

            tritiumBreedingMassAdjustment = tritium_molar_mass_ratio * lithium6_density/ tritium_density;
            heliumBreedingMassAdjustment = helium_molar_mass_ratio * lithium6_density / helium4_density;

            if (IsEnabled && last_active_time > 0)
                DoPersistentResourceUpdate();

            if (!String.IsNullOrEmpty(animName))
                pulseAnimation = PluginHelper.SetUpAnimation(animName, this.part);
            if (!String.IsNullOrEmpty(loopingAnimationName))
                loopingAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == loopingAnimationName);
            if (!String.IsNullOrEmpty(startupAnimationName))
                startupAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == startupAnimationName);
            if (!String.IsNullOrEmpty(shutdownAnimationName))
                shutdownAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == shutdownAnimationName);


            habitat = part.FindModuleImplementing<FNHabitat>();

            // only force activate if Enabled and not with a engine model
            var myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (IsEnabled && myAttachedEngine == null)
            {
                Debug.Log("[KSPI]: Reactor on " + part.name + " was Force Activated by system");
                this.part.force_activate();
                Fields["currentMass"].guiActiveEditor = true;
                Fields["radius"].guiActiveEditor = true;
                Fields["connectedRecieversStr"].guiActiveEditor = true;
                Fields["heatTransportationEfficiency"].guiActiveEditor = true;
            }
            else
                Debug.Log("[KSPI]: skipped calling Force on " + part.name);

            Fields["electricPowerPriority"].guiActive = showPowerPriority;
            Fields["reactorSurface"].guiActiveEditor = showSpecialisedUI;
            Fields["forcedMinimumThrottle"].guiActive = showForcedMinimumThrottle;
            Fields["forcedMinimumThrottle"].guiActiveEditor = showForcedMinimumThrottle;
        }

        private void UpdateReactorCharacteristics()
        {
            DeterminePowerGenerationType();

            DetermineFuelModeTechLevel();

            DeterminePowerOutput();

            DetermineFuelEfficency();

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
            // if coretemperature is missing, first look at lagacy value
            if (coreTemperatureMk1 == 0)
                coreTemperatureMk1 = ReactorTemp;
            if (coreTemperatureMk2 == 0)
                coreTemperatureMk2 = upgradedReactorTemp;
            if (coreTemperatureMk3 == 0)
                coreTemperatureMk3 = upgradedReactorTemp * powerUpgradeCoreTempMult;

            // prevent initial values
            if (coreTemperatureMk1 == 0)
                coreTemperatureMk1 = 2500;
            if (coreTemperatureMk2 == 0)
                coreTemperatureMk2 = coreTemperatureMk1;
            if (coreTemperatureMk3 == 0)
                coreTemperatureMk3 = coreTemperatureMk2;
            if (coreTemperatureMk4 == 0)
                coreTemperatureMk4 = coreTemperatureMk3;
            if (coreTemperatureMk5 == 0)
                coreTemperatureMk5 = coreTemperatureMk4;
            if (coreTemperatureMk6 == 0)
                coreTemperatureMk6 = coreTemperatureMk5;
            if (coreTemperatureMk7 == 0)
                coreTemperatureMk7 = coreTemperatureMk6;
        }

        private void DetermineFuelEfficency()
        {
            // if fuel efficency is missing, try to use lagacy value
            if (fuelEfficencyMk1 == 0)
                fuelEfficencyMk1 = fuelEfficiency;

            // prevent any initial values
            if (fuelEfficencyMk1 == 0)
                fuelEfficencyMk1 = 1;
            if (fuelEfficencyMk2 == 0)
                fuelEfficencyMk2 = fuelEfficencyMk1;
            if (fuelEfficencyMk3 == 0)
                fuelEfficencyMk3 = fuelEfficencyMk2;
            if (fuelEfficencyMk4 == 0)
                fuelEfficencyMk4 = fuelEfficencyMk3;
            if (fuelEfficencyMk5 == 0)
                fuelEfficencyMk5 = fuelEfficencyMk4;
            if (fuelEfficencyMk6 == 0)
                fuelEfficencyMk6 = fuelEfficencyMk5;
            if (fuelEfficencyMk7 == 0)
                fuelEfficencyMk7 = fuelEfficencyMk6;
        }

        private void DeterminePowerGenerationType()
        {
            // initialse tech requirment if missing 
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

            // show poweroutput when appropriate
            if (currentGenerationType >= 6)
                Fields["powerOutputMk7"].guiActiveEditor = true;
            if (currentGenerationType >= 5)
                Fields["powerOutputMk6"].guiActiveEditor = true;
            if (currentGenerationType >= 4)
                Fields["powerOutputMk5"].guiActiveEditor = true;
            if (currentGenerationType >= 3)
                Fields["powerOutputMk4"].guiActiveEditor = true;
            if (currentGenerationType >= 2)
                Fields["powerOutputMk3"].guiActiveEditor = true;
            if (currentGenerationType >= 1)
                Fields["powerOutputMk2"].guiActiveEditor = true;
            if (currentGenerationType >= 0)
                Fields["powerOutputMk1"].guiActiveEditor = true;
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
                if (ConnectedChargedParticleElectricGenerator != null)
                    ConnectedChargedParticleElectricGenerator.FindAndAttachToPowerSource();

                if (ConnectedThermalElectricGenerator != null)
                    ConnectedThermalElectricGenerator.FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Reactor.OnEditorDetach " + e.Message);
            }
        }

        public virtual void Update()
        {
            DeterminePowerOutput();

            UpdateKerbalismEmitter();

            currentMass = part.mass;
            currentRawPowerOutput = RawPowerOutput;

            Events["DeactivateReactor"].guiActive = HighLogic.LoadedSceneIsFlight && showShutDownInFlight && IsEnabled;

            if (HighLogic.LoadedSceneIsEditor)
            {
                reactorSurface = radius * radius;
            }
        }

        protected void UpdateFuelMode()
        {
            fuelModeStr = CurrentFuelMode != null ? CurrentFuelMode.ModeGUIName : "null";
        }

        public override void OnUpdate()
        {
            Events["StartBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && !breedtritium && IsFuelNeutronRich && IsEnabled;
            Events["StopBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && breedtritium && IsFuelNeutronRich && IsEnabled;
            UpdateFuelMode();

            coretempStr = CoreTemperature.ToString("0") + " K";

            if (IsEnabled && CurrentFuelMode != null)
            {
                if (CheatOptions.InfinitePropellant || stored_fuel_ratio > 0.99)
                    statusStr = "Active (" + powerPcnt.ToString("0.000") + "%)";
                else if (current_fuel_variant != null)
                {
                    if (stored_fuel_ratio == 0)
                        statusStr = current_fuel_variant.ReactorFuels.OrderBy(fuel => GetFuelAvailability(fuel)).First().ResourceName + " Deprived";
                    else
                        statusStr = current_fuel_variant.ReactorFuels.OrderBy(fuel => GetFuelAvailability(fuel)).First().ResourceName + (stored_fuel_ratio * 100) + "%";
                }
            }
            else
            {
                if (powerPcnt > 0)
                    statusStr = "Decay Heating (" + powerPcnt.ToString("0.000") + "%)";
                else
                    statusStr = "Offline";
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
            base.OnFixedUpdate();

            StoreGeneratorRequests();

            decay_ongoing = false;

            var maximumPower = MaximumPower;

            if (IsEnabled && maximumPower > 0)
            {
                //if (ReactorIsOverheating())
                //{
                //	if (FlightGlobals.ActiveVessel == vessel)
                //		ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_Reactor_reactorIsOverheating"), 5.0f, ScreenMessageStyle.UPPER_CENTER);

                //	IsEnabled = false;
                //	return;
                //}

                max_power_to_supply = Math.Max(maximumPower * timeWarpFixedDeltaTime, 0);

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
                            var totalThrust = engines.Sum(m => ((double)(decimal)m.realIsp) * ((double)(decimal)m.requestedMassFlow) * GameConstants.STANDARD_GRAVITY * Vector3d.Dot(m.part.transform.up, vessel.transform.up));
                            currentGeeForce = Math.Max(currentGeeForce, totalThrust / vessel.totalMass / GameConstants.STANDARD_GRAVITY);
                        }
                    }

                    var geeforce = double.IsNaN(currentGeeForce) || double.IsInfinity(currentGeeForce) ? 0 : currentGeeForce;

                    var scaledGeeforce = Math.Pow(Math.Max(geeforce - geeForceTreshHold, 0) * geeForceMultiplier, geeForceExponent);

                    geeForceModifier = Math.Min(Math.Max(1 - scaledGeeforce, minGeeForceModifier), 1);
                }
                else
                    geeForceModifier = 1;

                if (hasOverheatEffects && !CheatOptions.IgnoreMaxTemperature)
                {
                    averageOverheat.Enqueue(getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT));
                    if (averageOverheat.Count > 10)
                        averageOverheat.Dequeue();

                    var scaledOverheating = Math.Pow(Math.Max(getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT) - overheatTreshHold, 0) * overheatMultiplier, overheatExponent);

                    overheatModifier = Math.Min(Math.Max(1 - scaledOverheating, minOverheatModifier), 1);
                }
                else
                    overheatModifier = 1;

                current_fuel_variants_sorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, max_power_to_supply * geeForceModifier * overheatModifier, fuelUsePerMJMult);
                current_fuel_variant = current_fuel_variants_sorted.FirstOrDefault();
                fuel_mode_variant = current_fuel_variant.Name;
                
                stored_fuel_ratio = CheatOptions.InfinitePropellant ? 1 : current_fuel_variant != null ? Math.Min(current_fuel_variant.FuelRatio, 1) : 0;

                var true_variant = CurrentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, max_power_to_supply, fuelUsePerMJMult, false).FirstOrDefault();
                fuel_ratio = CheatOptions.InfinitePropellant ? 1 : true_variant != null ? Math.Min(true_variant.FuelRatio, 1) : 0;

                LookForAlternativeFuelTypes();

                UpdateCapacities();

                if (fuel_ratio < 0.99999)
                {
                    var message = Localizer.Format("#LOC_KSPIE_Reactor_ranOutOfFuelFor") + " " + CurrentFuelMode.ModeGUIName;
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
                }
             
                thermalThrottleRatio = connectedEngines.Any(m => m.RequiresThermalHeat) ? Math.Min(1, connectedEngines.Where(m => m.RequiresThermalHeat).Sum(e => e.CurrentThrottle)) : 0;
                plasmaThrottleRatio = connectedEngines.Any(m => m.RequiresPlasmaHeat) ? Math.Min(1, connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.CurrentThrottle)) : 0;
                chargedThrottleRatio = connectedEngines.Any(m => m.RequiresChargedPower) ? Math.Min(1, connectedEngines.Where(m => m.RequiresChargedPower).Max(e => e.CurrentThrottle)) : 0;

                thermal_propulsion_ratio = thermalPropulsionEfficiency * thermalThrottleRatio;
                plasma_propulsion_ratio = plasmaPropulsionEfficiency * plasmaThrottleRatio;
                charged_propulsion_ratio = chargedParticlePropulsionEfficiency * chargedThrottleRatio;

                var thermal_generator_ratio = thermalEnergyEfficiency * storedGeneratorThermalEnergyRequestRatio;
                var plasma_generator_ratio = plasmaEnergyEfficiency * storedGeneratorPlasmaEnergyRequestRatio;
                var charged_generator_ratio = chargedParticleEnergyEfficiency * storedGeneratorChargedEnergyRequestRatio;

                maximum_thermal_request_ratio = Math.Min(thermal_propulsion_ratio + plasma_propulsion_ratio + thermal_generator_ratio + plasma_generator_ratio, 1);
                maximum_charged_request_ratio = Math.Min(charged_propulsion_ratio + charged_generator_ratio, 1);

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

                //safetyThrotleModifier = GetSafetyOverheatPreventionRatio();
                max_charged_to_supply_per_second = maximumChargedPower * stored_fuel_ratio * geeForceModifier * overheatModifier * powerAccessModifier;
                requested_charged_to_supply_per_second = max_charged_to_supply_per_second * power_request_ratio * maximum_charged_request_ratio;

                var chargedParticlesManager = getManagerForVessel(ResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                var thermalHeatManager = getManagerForVessel(ResourceManager.FNRESOURCE_THERMALPOWER);

                min_throttle = stored_fuel_ratio > 0 ? MinimumThrottle / stored_fuel_ratio : 1;
                var neededChargedPowerPerSecond = getNeededPowerSupplyPerSecondWithMinimumRatio(max_charged_to_supply_per_second, min_throttle, ResourceManager.FNRESOURCE_CHARGED_PARTICLES, chargedParticlesManager);
                charged_power_ratio = Math.Min(maximum_charged_request_ratio, maximumChargedPower > 0 ? neededChargedPowerPerSecond / maximumChargedPower : 0);

                max_thermal_to_supply_per_second = maximumThermalPower * stored_fuel_ratio * geeForceModifier * overheatModifier * powerAccessModifier;
                requested_thermal_to_supply_per_second = max_thermal_to_supply_per_second * power_request_ratio * maximum_thermal_request_ratio;

                var neededThermalPowerPerSecond = getNeededPowerSupplyPerSecondWithMinimumRatio(max_thermal_to_supply_per_second, min_throttle, ResourceManager.FNRESOURCE_THERMALPOWER, thermalHeatManager);
                requested_thermal_power_ratio =  maximumThermalPower > 0 ? neededThermalPowerPerSecond / maximumThermalPower : 0;
                thermal_power_ratio = Math.Min(maximum_thermal_request_ratio, requested_thermal_power_ratio);

                reactor_power_ratio = Math.Min(overheatModifier * maximum_reactor_request_ratio, PowerRatio);

                ongoing_charged_power_generated = managedProvidedPowerSupplyPerSecondMinimumRatio(requested_charged_to_supply_per_second, max_charged_to_supply_per_second, reactor_power_ratio, ResourceManager.FNRESOURCE_CHARGED_PARTICLES, chargedParticlesManager);
                ongoing_thermal_power_generated = managedProvidedPowerSupplyPerSecondMinimumRatio(requested_thermal_to_supply_per_second, max_thermal_to_supply_per_second, reactor_power_ratio, ResourceManager.FNRESOURCE_THERMALPOWER, thermalHeatManager);
                ongoing_total_power_generated = ongoing_thermal_power_generated + ongoing_charged_power_generated;

                var totalPowerReceivedFixed = ongoing_total_power_generated * timeWarpFixedDeltaTime;

                UpdateEmbrittlement(Math.Max(thermalThrottleRatio, plasmaThrottleRatio));

                ongoing_consumption_rate = maximumPower > 0 ? ongoing_total_power_generated / maximumPower : 0;
                PluginHelper.SetAnimationRatio((float)ongoing_consumption_rate, pulseAnimation);
                powerPcnt = 100 * ongoing_consumption_rate;

                // produce wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    // skip first frame of wasteheat production
                    var delayed_wasteheat_rate = ongoing_consumption_rate > ongoing_wasteheat_rate ? Math.Min(ongoing_wasteheat_rate, ongoing_consumption_rate) : ongoing_consumption_rate;

                    supplyFNResourcePerSecondWithMax(delayed_wasteheat_rate * maximumPower, StableMaximumReactorPower, ResourceManager.FNRESOURCE_WASTEHEAT);

                    ongoing_wasteheat_rate = ongoing_consumption_rate;
                }

                // consume fuel
                if (!CheatOptions.InfinitePropellant)
                {
                    consumedFuelTotalFixed = 0;

                    for (var i = 0; i < current_fuel_variant.ReactorFuels.Count; i++)
                    {
                        var consumedMass = ConsumeReactorFuel(current_fuel_variant.ReactorFuels[i], totalPowerReceivedFixed / geeForceModifier);

                        consumedFuelTotalFixed += consumedMass;
                    }

                    consumedFuelTotalPerSecond = consumedFuelTotalFixed / timeWarpFixedDeltaTime;

                    // refresh production list
                    reactorProduction.Clear();

                    // produce reactor products
                    for (var i = 0; i < current_fuel_variant.ReactorProducts.Count; i++)
                    {
                        var product = current_fuel_variant.ReactorProducts[i];
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
                ongoing_thermal_power_generated = supplyManagedFNResourcePerSecondWithMinimumRatio(powerToSupply, 1, ResourceManager.FNRESOURCE_THERMALPOWER);
                ongoing_total_power_generated = ongoing_thermal_power_generated;
                BreedTritium(ongoing_thermal_power_generated, timeWarpFixedDeltaTime);
                ongoing_consumption_rate = MaximumPower > 0 ? ongoing_thermal_power_generated / MaximumPower : 0;
                powerPcnt = 100 * ongoing_consumption_rate;
                decay_ongoing = true;
            }
            else
            {
                ongoing_total_power_generated = 0;
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                powerPcnt = 0;
            }

            if (IsEnabled) return;

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_THERMALPOWER, 0);
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_CHARGED_PARTICLES, 0);
            resourceBuffers.UpdateBuffers();
        }

        private void UpdateEmbrittlement(double thermal_plasma_ratio)
        {
            var hasActiveNeutronAborbtion = connectedEngines.All(m => m.PropellantAbsorbsNeutrons) && thermal_plasma_ratio > 0;
            var lithium_embrittlement_modifer = 1 - Math.Max(lithium_modifier * 0.9, hasActiveNeutronAborbtion ? 0.9 : 0);

            if (!CheatOptions.UnbreakableJoints && CurrentFuelMode.NeutronsRatio > 0 && CurrentFuelMode.NeutronsRatio > 0)
                neutronEmbrittlementDamage += 5 * lithium_embrittlement_modifer * ongoing_total_power_generated * timeWarpFixedDeltaTime * CurrentFuelMode.NeutronsRatio / neutronEmbrittlementDivider;
        }

        private void LookForAlternativeFuelTypes()
        {
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType1);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType2);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType3);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType4);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType5);
        }

        private void SwitchToAlternativeFuelWhenAvailable(string alternativeFuelTypeName)
        {
            if (stored_fuel_ratio >= 0.99)
                return;

            if (String.IsNullOrEmpty(alternativeFuelTypeName))
                return;

            // look for most advanced version
            var alternativeFuelType = fuel_modes.LastOrDefault(m => m.ModeGUIName.Contains(alternativeFuelTypeName));
            if (alternativeFuelType == null)
            {
                Debug.LogWarning("[KSPI]: failed to find fueltype " + alternativeFuelTypeName);
                return;
            }

            Debug.Log("[KSPI]: searching fuelmodes for alternative for fuel type " + alternativeFuelTypeName);
            var alternativeFuelVariantsSorted = alternativeFuelType.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, max_power_to_supply, fuelUsePerMJMult);

            if (alternativeFuelVariantsSorted == null)
                return;

            var alternativeFuelVariant = alternativeFuelVariantsSorted.FirstOrDefault();
            if (alternativeFuelVariant == null)
            {
                Debug.LogError("[KSPI]: failed to find any variant for fueltype " + alternativeFuelTypeName);
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
            stored_fuel_ratio = current_fuel_variant.FuelRatio;
        }

        private void StoreGeneratorRequests()
        {
            storedIsThermalEnergyGeneratorEfficiency = currentIsThermalEnergyGeneratorEfficiency;
            storedIsPlasmaEnergyGeneratorEfficiency = currentIsPlasmaEnergyGeneratorEfficiency;
            storedIsChargedEnergyGeneratorEfficiency = currentIsChargedEnergyGenratorEfficiency;

            currentIsThermalEnergyGeneratorEfficiency = 0;
            currentIsPlasmaEnergyGeneratorEfficiency = 0;
            currentIsChargedEnergyGenratorEfficiency = 0;

            var previousStoredRatio = Math.Max(Math.Max(storedGeneratorThermalEnergyRequestRatio, storedGeneratorPlasmaEnergyRequestRatio), storedGeneratorChargedEnergyRequestRatio);
            
            storedGeneratorThermalEnergyRequestRatio = Math.Max(storedGeneratorThermalEnergyRequestRatio, previousStoredRatio);
            storedGeneratorPlasmaEnergyRequestRatio = Math.Max(storedGeneratorPlasmaEnergyRequestRatio, previousStoredRatio);
            storedGeneratorChargedEnergyRequestRatio = Math.Max(storedGeneratorChargedEnergyRequestRatio, previousStoredRatio);

            var requiredMinimumThrottle = Math.Max(MinimumThrottle, ForcedMinimumThrottleRatio);

            currentGeneratorThermalEnergyRequestRatio = Math.Max(currentGeneratorThermalEnergyRequestRatio, requiredMinimumThrottle);
            currentGeneratorPlasmaEnergyRequestRatio = Math.Max(currentGeneratorPlasmaEnergyRequestRatio, requiredMinimumThrottle);
            currentGeneratorChargedEnergyRequestRatio = Math.Max(currentGeneratorChargedEnergyRequestRatio, requiredMinimumThrottle);

            var thermalDifference = Math.Abs(storedGeneratorThermalEnergyRequestRatio - currentGeneratorThermalEnergyRequestRatio);
            var plasmaDifference = Math.Abs(storedGeneratorPlasmaEnergyRequestRatio - currentGeneratorPlasmaEnergyRequestRatio);
            var chargedDifference = Math.Abs(storedGeneratorChargedEnergyRequestRatio - currentGeneratorChargedEnergyRequestRatio);

            var thermalThrotleIsGrowing = currentGeneratorThermalEnergyRequestRatio > storedGeneratorThermalEnergyRequestRatio;
            var plasmaThrotleIsGrowing = currentGeneratorPlasmaEnergyRequestRatio > storedGeneratorPlasmaEnergyRequestRatio;
            var chargedThrotleIsGrowing = currentGeneratorChargedEnergyRequestRatio > storedGeneratorChargedEnergyRequestRatio;

            var fixedReactorSpeedMult = ReactorSpeedMult * timeWarpFixedDeltaTime;
            var minimumAcceleration = timeWarpFixedDeltaTime * timeWarpFixedDeltaTime;

            var thermalAccelerationReductionRatio = thermalThrotleIsGrowing
                ? storedGeneratorThermalEnergyRequestRatio <= 0.5 ? 1 : minimumAcceleration + (1 - storedGeneratorThermalEnergyRequestRatio) / 0.5
                : storedGeneratorThermalEnergyRequestRatio <= 0.5 ? minimumAcceleration + storedGeneratorThermalEnergyRequestRatio / 0.5 : 1;

            var plasmaAccelerationReductionRatio = plasmaThrotleIsGrowing
                ? storedGeneratorPlasmaEnergyRequestRatio <= 0.5 ? 1 : minimumAcceleration + (1 - storedGeneratorPlasmaEnergyRequestRatio) / 0.5
                : storedGeneratorPlasmaEnergyRequestRatio <= 0.5 ? minimumAcceleration + storedGeneratorPlasmaEnergyRequestRatio / 0.5 : 1;

            var chargedAccelerationReductionRatio = chargedThrotleIsGrowing
                ? storedGeneratorChargedEnergyRequestRatio <= 0.5 ? 1 : minimumAcceleration + (1 - storedGeneratorChargedEnergyRequestRatio) / 0.5
                : storedGeneratorChargedEnergyRequestRatio <= 0.5 ? minimumAcceleration + storedGeneratorChargedEnergyRequestRatio / 0.5 : 1;

            var fixedThermalSpeed = fixedReactorSpeedMult > 0 ? Math.Min(thermalDifference, fixedReactorSpeedMult) * thermalAccelerationReductionRatio : thermalDifference;
            var fixedPlasmaSpeed = fixedReactorSpeedMult > 0 ? Math.Min(plasmaDifference, fixedReactorSpeedMult) * plasmaAccelerationReductionRatio : plasmaDifference;
            var fixedChargedSpeed = fixedReactorSpeedMult > 0 ? Math.Min(chargedDifference, fixedReactorSpeedMult) * chargedAccelerationReductionRatio : chargedDifference;

            var thermalChangeFraction = thermalThrotleIsGrowing ? fixedThermalSpeed : -fixedThermalSpeed;
            var plasmaChangeFraction = plasmaThrotleIsGrowing ? fixedPlasmaSpeed : -fixedPlasmaSpeed;
            var chargedChangeFraction = chargedThrotleIsGrowing ? fixedChargedSpeed : -fixedChargedSpeed;

            storedGeneratorThermalEnergyRequestRatio = Math.Max(0, Math.Min(1, storedGeneratorThermalEnergyRequestRatio + thermalChangeFraction));
            storedGeneratorPlasmaEnergyRequestRatio = Math.Max(0, Math.Min(1, storedGeneratorPlasmaEnergyRequestRatio + plasmaChangeFraction));
            storedGeneratorChargedEnergyRequestRatio = Math.Max(0, Math.Min(1, storedGeneratorChargedEnergyRequestRatio + chargedChangeFraction));

            currentGeneratorThermalEnergyRequestRatio = 0;
            currentGeneratorPlasmaEnergyRequestRatio = 0;
            currentGeneratorChargedEnergyRequestRatio = 0;
        }

        private void UpdateCapacities()
        {
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_THERMALPOWER, MaximumThermalPower);
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_CHARGED_PARTICLES, MaximumChargedPower);
            resourceBuffers.UpdateBuffers();
        }

        protected double GetFuelRatio(ReactorFuel reactorFuel, double fuelEfficency, double megajoules)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            var fuelUseForPower = reactorFuel.GetFuelUseForPower(fuelEfficency, megajoules, fuelUsePerMJMult);

            return fuelUseForPower > 0 ? GetFuelAvailability(reactorFuel) / fuelUseForPower : 0;
        }

        private void BreedTritium(double neutronPowerReceivedEachSecond, double fixedDeltaTime)
        {
            var partResourceLithium6 = part.Resources[InterstellarResourcesConfiguration.Instance.Lithium6];
            if (partResourceLithium6 != null)
            {
                totalAmountLithium = partResourceLithium6.amount;
                totalMaxAmountLithium = partResourceLithium6.maxAmount;
            }
            else
            {
                totalAmountLithium = 0;
                totalMaxAmountLithium = 0;
            }

            var ratioLithium6 = totalAmountLithium > 0 ? totalAmountLithium / totalMaxAmountLithium : 0;

            if (!breedtritium || neutronPowerReceivedEachSecond <= 0 || fixedDeltaTime <= 0)
            {
                tritium_produced_per_second = 0;
                helium_produced_per_second = 0;
                return;
            }

            // calculate current maximum litlium consumption
            var breedRate = CurrentFuelMode.TritiumBreedModifier * staticBreedRate * neutronPowerReceivedEachSecond * fixedDeltaTime * Math.Sqrt(ratioLithium6);
            var lithRate = breedRate / lithium6_density;

            // get spare room tritium
            var spareRoomTritiumAmount = part.GetResourceSpareCapacity(tritium_def);

            // limit lithium consumption to maximum tritium storage
            var maximumTritiumProduction = lithRate * tritiumBreedingMassAdjustment;
            var maximumLitiumConsumtionRatio = maximumTritiumProduction > 0 ? Math.Min(maximumTritiumProduction, spareRoomTritiumAmount) / maximumTritiumProduction : 0;
            var lithiumRequest = lithRate * maximumLitiumConsumtionRatio;

            // consume the lithium
            var lithUsed = CheatOptions.InfinitePropellant
                ? lithiumRequest
                : part.RequestResource(lithium6_def.id, lithiumRequest, ResourceFlowMode.STACK_PRIORITY_SEARCH);

            // calculate effective lithium used for tritium breeding
            lithium_consumed_per_second = lithUsed / fixedDeltaTime;

            // caculate products
            var tritiumProduction = lithUsed * tritiumBreedingMassAdjustment;
            var heliumProduction = lithUsed * heliumBreedingMassAdjustment;

            // produce tritium and helium
            tritium_produced_per_second = CheatOptions.InfinitePropellant
                ? tritiumProduction / fixedDeltaTime
                : -part.RequestResource(tritium_def.name, -tritiumProduction) / fixedDeltaTime;

            helium_produced_per_second = CheatOptions.InfinitePropellant
                ? heliumProduction / fixedDeltaTime
                : -part.RequestResource(helium_def.name, -heliumProduction) / fixedDeltaTime;
        }

        public virtual double GetCoreTempAtRadiatorTemp(double radTemp)
        {
            return CoreTemperature;
        }

        public virtual double GetThermalPowerAtTemp(double temp)
        {
            return MaximumPower;
        }

        public double Radius
        {
            get { return radius; }
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

        public bool isVolatileSource()
        {
            return false;
        }

        public override string GetInfo()
        {
            UpdateReactorCharacteristics();

            var sb = new StringBuilder();

            if (showEngineConnectionInfo)
            {
                sb.AppendLine("<size=11><color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_propulsion") + ":</color><size=10>");
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_thermalNozzle") + ": " + UtilisationInfo(thermalPropulsionEfficiency));
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_plasmaNozzle") + ": " + UtilisationInfo(plasmaPropulsionEfficiency));
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_magneticNozzle") + ": " + UtilisationInfo(chargedParticlePropulsionEfficiency));
                sb.Append("</size>");
                sb.AppendLine();
            }

            if (showPowerGeneratorConnectionInfo)
            {
                sb.AppendLine("<size=11><color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_powerGeneration") + ":</color><size=10>");
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_thermalGenerator") + ": " + UtilisationInfo(thermalEnergyEfficiency));
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_MHDGenerator") + ": " + UtilisationInfo(plasmaEnergyEfficiency));
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_chargedParticleGenerator") + ": " + UtilisationInfo(chargedParticleEnergyEfficiency));
                sb.Append("</size>");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(upgradeTechReqMk2))
            {
                sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_powerUpgradeTechnologies") + ":</color><size=10>");
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk2)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk3)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk4)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk5)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk6)));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReqMk7)));
                sb.Append("</size>");
                sb.AppendLine();
            }

            if (thermalEnergyEfficiency > 0 || plasmaEnergyEfficiency > 0 || chargedParticleEnergyEfficiency > 0)
            {
                
                sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_ReactorPower") + ":</color><size=10>");
                sb.AppendLine("Mk1: " + PluginHelper.getFormattedPowerString(powerOutputMk1));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.AppendLine("Mk2: " + PluginHelper.getFormattedPowerString(powerOutputMk2));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.AppendLine("Mk3: " + PluginHelper.getFormattedPowerString(powerOutputMk3));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.AppendLine("Mk4: " + PluginHelper.getFormattedPowerString(powerOutputMk4));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.AppendLine("Mk5: " + PluginHelper.getFormattedPowerString(powerOutputMk5));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.AppendLine("Mk6: " + PluginHelper.getFormattedPowerString(powerOutputMk6));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.AppendLine("Mk7: " + PluginHelper.getFormattedPowerString(powerOutputMk7));
                sb.Append("</size>");
                sb.AppendLine();
            }

            if (hasSpecificFuelModeTechs)
            {
                sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_fuelModeUpgradeTechnologies") + ":</color><size=10>");
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel2) && fuelModeTechReqLevel2 != "none") sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel2)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel3) && fuelModeTechReqLevel3 != "none") sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel3)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel4) && fuelModeTechReqLevel4 != "none") sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel4)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel5) && fuelModeTechReqLevel5 != "none") sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel5)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel6) && fuelModeTechReqLevel6 != "none") sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel6)));
                if (!string.IsNullOrEmpty(fuelModeTechReqLevel7) && fuelModeTechReqLevel7 != "none") sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(fuelModeTechReqLevel7)));
                sb.Append("</size>");
                sb.AppendLine();
            }

            var maximumFuelTechLevel = GetMaximumFuelTechLevel();
            var fuelGroups = GetFuelGroups(maximumFuelTechLevel);

            if (fuelGroups.Count > 1)
            {
                sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_getInfoFuelModes") + ":</color><size=10>");

                foreach (var group in fuelGroups)
                {
                     sb.AppendLine("Mk" + (1 + group.TechLevel - reactorModeTechBonus).ToString() +  " : " + Localizer.Format(group.ModeGUIName));
                }
                sb.Append("</size>");
                sb.AppendLine();
            }
            
            if (plasmaPropulsionEfficiency > 0)
            {
                
                sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_plasmaNozzlePerformance") + ":</color><size=10>");
                                                              sb.AppendLine("Mk1: " + PlasmaNozzlePerformance(coreTemperatureMk1, powerOutputMk1 * plasmaPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.AppendLine("Mk2: " + PlasmaNozzlePerformance(coreTemperatureMk2, powerOutputMk2 * plasmaPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.AppendLine("Mk3: " + PlasmaNozzlePerformance(coreTemperatureMk3, powerOutputMk3 * plasmaPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.AppendLine("Mk4: " + PlasmaNozzlePerformance(coreTemperatureMk4, powerOutputMk4 * plasmaPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.AppendLine("Mk5: " + PlasmaNozzlePerformance(coreTemperatureMk5, powerOutputMk5 * plasmaPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.AppendLine("Mk6: " + PlasmaNozzlePerformance(coreTemperatureMk6, powerOutputMk6 * plasmaPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.AppendLine("Mk7: " + PlasmaNozzlePerformance(coreTemperatureMk7, powerOutputMk7 * plasmaPropulsionEfficiency));
                sb.Append("</size>");
                sb.AppendLine();
            }

            if (thermalPropulsionEfficiency > 0)
            {
                sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Reactor_thermalNozzlePerformance") + ":</color><size=10>");
                sb.AppendLine("Mk1: " + ThermalNozzlePerformance(coreTemperatureMk1, powerOutputMk1 * thermalPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk2)) sb.AppendLine("Mk2: " + ThermalNozzlePerformance(coreTemperatureMk2, powerOutputMk2 * thermalPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk3)) sb.AppendLine("Mk3: " + ThermalNozzlePerformance(coreTemperatureMk3, powerOutputMk3 * thermalPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk4)) sb.AppendLine("Mk4: " + ThermalNozzlePerformance(coreTemperatureMk4, powerOutputMk4 * thermalPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk5)) sb.AppendLine("Mk5: " + ThermalNozzlePerformance(coreTemperatureMk5, powerOutputMk5 * thermalPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk6)) sb.AppendLine("Mk6: " + ThermalNozzlePerformance(coreTemperatureMk6, powerOutputMk6 * thermalPropulsionEfficiency));
                if (!string.IsNullOrEmpty(upgradeTechReqMk7)) sb.AppendLine("Mk7: " + ThermalNozzlePerformance(coreTemperatureMk7, powerOutputMk7 * thermalPropulsionEfficiency));
                sb.Append("</size>");
                sb.AppendLine();
            }

            sb.AppendLine("</size>");
            return sb.ToString();
        }

        private List<ReactorFuelType> GetFuelGroups(int maximumFuelTechLevel)
        {
            var groups = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE")
                .Select(node => new ReactorFuelMode(node))
                .Where(fm =>
                       fm.AllFuelResourcesDefinitionsAvailable && fm.AllProductResourcesDefinitionsAvailable
                    && (fm.SupportedReactorTypes & ReactorType) == ReactorType
                    && maximumFuelTechLevel >= fm.TechLevel
                    && (fm.Aneutronic || canUseNeutronicFuels)
                    && maxGammaRayPower >= fm.GammaRayEnergy)
                .GroupBy(mode => mode.ModeGUIName).Select(group => new ReactorFuelType(group)).OrderBy(m => m.TechLevel).ToList();
            return groups;
        }

        private int GetMaximumFuelTechLevel()
        {
            int techlevels = 0;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel2) && fuelModeTechReqLevel2 != "none") techlevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel3) && fuelModeTechReqLevel3 != "none") techlevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel4) && fuelModeTechReqLevel4 != "none") techlevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel5) && fuelModeTechReqLevel5 != "none") techlevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel6) && fuelModeTechReqLevel6 != "none") techlevels++;
            if (!string.IsNullOrEmpty(fuelModeTechReqLevel7) && fuelModeTechReqLevel7 != "none") techlevels++;
            var maximumFuelTechLevel = techlevels + reactorModeTechBonus;
            return maximumFuelTechLevel;
        }

        private string ThermalNozzlePerformance(double temperature, double powerInMJ)
        {
            var isp = Math.Min(Math.Sqrt(temperature) * 21, PluginHelper.MaxThermalNozzleIsp);

            var exhaustvelocity = isp * 9.81;

            var thrust = powerInMJ * 2000 / exhaustvelocity / powerOutputMultiplier;

            return thrust.ToString("F1") + "kN @ " + isp.ToString("F0") + "s";
        }

        private string PlasmaNozzlePerformance(double temperature, double powerInMJ)
        {
            var isp = Math.Sqrt(temperature) * 21;

            var exhaustvelocity = isp * 9.81;

            var thrust = powerInMJ * 2000 / exhaustvelocity / powerOutputMultiplier;

            return thrust.ToString("F1") + "kN @ " + isp.ToString("F0") + "s";
        }

        private string UtilisationInfo(double value)
        {
            if (value > 0)
            {
                string result = "<color=green>Ѵ</color>";
                if (value != 1)
                    result += " <color=orange>" + (value * 100).ToString("F0") + "%</color>";
                return result;
            }
            else
                return "<color=red>X</color>";
        }

        protected void DoPersistentResourceUpdate()
        {
            if (CheatOptions.InfinitePropellant)
                return;

            // calculate delta time since last processing
            double deltaTimeDiff = Math.Max(Planetarium.GetUniversalTime() - last_active_time, 0);

            // determine avialable variants
            var persistantFuelVariantsSorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, deltaTimeDiff * ongoing_total_power_generated, fuelUsePerMJMult);

            // consume fuel
            foreach (var fuel in persistantFuelVariantsSorted.First().ReactorFuels)
            {
                ConsumeReactorFuel(fuel, deltaTimeDiff * ongoing_total_power_generated);
            }

            // produce reactor products
            foreach (var product in persistantFuelVariantsSorted.First().ReactorProducts)
            {
                ProduceReactorProduct(product, deltaTimeDiff * ongoing_total_power_generated);
            }

            // breed tritium
            BreedTritium(ongoing_total_power_generated * ThermalPowerRatio, deltaTimeDiff);

        }

        protected bool ReactorIsOverheating()
        {
            if (!CheatOptions.IgnoreMaxTemperature && getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT) >= emergencyPowerShutdownFraction && canShutdown)
            {
                deactivate_timer++;
                if (deactivate_timer > 3)
                    return true;
            }
            else
                deactivate_timer = 0;

            return false;
        }

        protected List<ReactorFuelType> GetReactorFuelModes()
        {
            var fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");

            var filteredFuelModes = fuelmodes.Select(node => new ReactorFuelMode(node))
                .Where(fm =>
                       fm.AllFuelResourcesDefinitionsAvailable
                    && fm.AllProductResourcesDefinitionsAvailable 
                    && (fm.SupportedReactorTypes & ReactorType) == ReactorType
                    && PluginHelper.HasTechRequirementOrEmpty(fm.TechRequirement)
                    && ReactorFuelModeTechLevel >= fm.TechLevel
                    && (fm.Aneutronic || canUseNeutronicFuels)
                    && maxGammaRayPower >= fm.GammaRayEnergy
                    && fm.NeutronsRatio <= maxNeutronsRatio 
                    && fm.NeutronsRatio >= minNeutronsRatio
                    ).ToList();

            for (int i = 0; i < filteredFuelModes.Count; i++)
            {
                filteredFuelModes[i].Position = i;
            }

            Debug.Log("[KSPI]: found " + filteredFuelModes.Count + " valid fuel types");

            //return filteredFuelModes;
            var groups = filteredFuelModes.GroupBy(mode => mode.ModeGUIName).Select(group => new ReactorFuelType(group)).ToList();

            Debug.Log("[KSPI]: grouped them into " + groups.Count + " valid fuel modes");

            return groups;
        }

        protected bool FuelRequiresLab(bool requiresLab)
        {
            var isConnectedToLab = part.IsConnectedToModule("ScienceModule", 10);

            return !requiresLab || isConnectedToLab && canBeCombinedWithLab;
        }

        protected virtual void SetDefaultFuelMode()
        {
            max_power_to_supply = Math.Max(MaximumPower * TimeWarpFixedDeltaTime, 0);
            CurrentFuelMode = fuel_modes.FirstOrDefault();

            if (CurrentFuelMode == null)
                print("[KSPI]: Warning : CurrentFuelMode is null");
            else
                print("[KSPI]: CurrentFuelMode = " + CurrentFuelMode.ModeGUIName);
        }

        protected double ConsumeReactorFuel(ReactorFuel fuel, double mJpower)
        {
            if (mJpower < (0.000005 / powerOutputMultiplier))
                return 0;

            var consumeAmountInUnitOfStorage = FuelEfficiency > 0 ?  mJpower * fuel.AmountFuelUsePerMJ * fuelUsePerMJMult / FuelEfficiency : 0;

            if (fuel.ConsumeGlobal)
            {
                var result = fuel.Simulate ? 0 : part.RequestResource(fuel.Definition.id, consumeAmountInUnitOfStorage, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                //var result = part.RequestResource(fuel.Definition.id, consumeAmountInUnitOfStorage, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                var fuelconsumption = fuel.Simulate ? consumeAmountInUnitOfStorage : result;
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

        protected virtual double ProduceReactorProduct(ReactorProduct product, double MJpower)
        {
            if (product.Definition == null)
                return 0;

            var productSupply = MJpower * product.AmountProductUsePerMJ * fuelUsePerMJMult / FuelEfficiency;

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

            //part.RequestResource(product.Definition.id, -productSupply, ResourceFlowMode.STAGE_PRIORITY_FLOW, product.Simulate);
            if (!product.Simulate)
                part.RequestResource(product.Definition.id, -productSupply, ResourceFlowMode.STAGE_PRIORITY_FLOW);
            return productSupply * product.DensityInTon;
        }

        protected double GetFuelAvailability(ReactorFuel fuel)
        {
            if (fuel == null)
                UnityEngine.Debug.LogError("[KSPI]: GetFuelAvailability fuel null");

            if (!fuel.ConsumeGlobal)
                return GetLocalResourceAmount(fuel);

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(fuel.Definition);
            else
                return part.FindAmountOfAvailableFuel(fuel.ResourceName, 4);
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
                UnityEngine.Debug.LogError("[KSPI]: GetFuelAvailability definition null");

            if (definition.resourceTransferMode == ResourceTransferMode.NONE)
            {
                if (part.Resources.Contains(definition.name))
                    return part.Resources[definition.name].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(definition);
            else
                return part.FindAmountOfAvailableFuel(definition.name, 4);
        }

        protected double GetProductAvailability(ReactorProduct product)
        {
            if (product == null)
                UnityEngine.Debug.LogError("[KSPI]: GetFuelAvailability product null");

            if (product.Definition == null)
            {
                UnityEngine.Debug.LogError("[KSPI]: GetFuelAvailability product definition null");
                return 0;
            }

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                    return part.Resources[product.ResourceName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(product.Definition);
            else
                return part.FindAmountOfAvailableFuel(product.ResourceName, 4);
        }

        protected double GetMaxProductAvailability(ReactorProduct product)
        {
            if (product == null)
                UnityEngine.Debug.LogError("[KSPI]: GetFuelAvailability product null");

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
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            bool found = false;

            foreach (PartModule module in part.Modules)
            {
                if (module.moduleName == "Emitter")
                {
                    emitterModule = module;

                    emitterRadiationField = module.Fields["radiation"];
                    if (emitterRadiationField != null)
                        emitterRadiationField.SetValue(maxRadiation * ongoing_consumption_rate, emitterModule);

                    found = true;
                    break;
                }
            }

            if (found)
                UnityEngine.Debug.Log("[KSPI]: Found Emitter");
            else
                UnityEngine.Debug.Log("[KSPI]: No Emitter Found");
        }

        private void UpdateKerbalismEmitter()
        {
            if (emitterModule == null)
                return;

            if (emitterRadiationField != null)
                emitterRadiationField.SetValue(maxRadiation * ongoing_consumption_rate, emitterModule);
        }

        public void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, Localizer.Format("#LOC_KSPIE_Reactor_reactorControlWindow"));
        }

        protected void PrintToGUILayout(string label, string value, GUIStyle bold_style, GUIStyle text_style, int witdhLabel = 150, int witdhValue = 150)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, bold_style, GUILayout.Width(witdhLabel));
            GUILayout.Label(value, text_style, GUILayout.Width(witdhValue));
            GUILayout.EndHorizontal();
        }

        protected virtual void WindowReactorSpecificOverride() { }

        private void Window(int windowID)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                if (bold_style == null)
                    bold_style = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold, font = PluginHelper.MainFont};

                if (text_style == null)
                    text_style = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Normal,font = PluginHelper.MainFont};

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    render_window = false;

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label(TypeName, bold_style, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (IsFuelNeutronRich)
                    PrintToGUILayout("Reactor Embrittlement", (100 * (1 - ReactorEmbrittlemenConditionRatio)).ToString("0.000000") + "%", bold_style, text_style);

                PrintToGUILayout("Geeforce overload ", (100 * (1 - geeForceModifier)).ToString("0.000000") + "%", bold_style, text_style);
                PrintToGUILayout("Overheating ", (100 * (1 - overheatModifier)).ToString("0.000000") + "%", bold_style, text_style);

                PrintToGUILayout("Radius", radius + "m", bold_style, text_style);
                PrintToGUILayout("Core Temperature", coretempStr, bold_style, text_style);
                PrintToGUILayout("Status", statusStr, bold_style, text_style);
                PrintToGUILayout("Fuel Mode", fuelModeStr, bold_style, text_style);
                PrintToGUILayout("Fuel Efficiency", (FuelEfficiency * 100).ToString(), bold_style, text_style);

                WindowReactorSpecificOverride();

                PrintToGUILayout("Current/Max Power Output", PluginHelper.getFormattedPowerString(ongoing_total_power_generated, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(NormalisedMaximumPower, "0.0", "0.000"), bold_style, text_style);

                if (ChargedPowerRatio < 1.0)
                    PrintToGUILayout("Current/Max Thermal Power", PluginHelper.getFormattedPowerString(ongoing_thermal_power_generated, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(MaximumThermalPower, "0.0", "0.000"), bold_style, text_style);
                if (ChargedPowerRatio > 0)
                    PrintToGUILayout("Current/Max Charged Power", PluginHelper.getFormattedPowerString(ongoing_charged_power_generated, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(MaximumChargedPower, "0.0", "0.000"), bold_style, text_style);

                if (CurrentFuelMode != null && current_fuel_variant.ReactorFuels != null)
                {
                    PrintToGUILayout("Energy Production", current_fuel_variant.GigawattPerGram.ToString("0.0") + " GW / g", bold_style, text_style);
                    PrintToGUILayout("Fuel Usage", current_fuel_variant.FuelUseInGramPerTeraJoule.ToString("0.000") + " g / TW", bold_style, text_style);

                    if (IsFuelNeutronRich && breedtritium && canBreedTritium)
                    {
                        PrintToGUILayout("Fuel Neutron Breed Rate", 100 * CurrentFuelMode.NeutronsRatio + "% ", bold_style, text_style);

                        var tritiumKgDay = tritium_produced_per_second * tritium_density * 1000 * PluginHelper.SecondsInDay;
                        PrintToGUILayout("Tritium Breed Rate", tritiumKgDay.ToString("0.000000") + " kg/day ", bold_style, text_style);

                        var heliumKgDay = helium_produced_per_second * helium4_density * 1000 * PluginHelper.SecondsInDay;
                        PrintToGUILayout("Helium Breed Rate", heliumKgDay.ToString("0.000000") + " kg/day ", bold_style, text_style);

                        double totalLithium6Amount;
                        double totalLithium6MaxAmount;
                        part.GetConnectedResourceTotals(lithium6_def.id, out totalLithium6Amount, out totalLithium6MaxAmount);

                        PrintToGUILayout("Lithium Reserves", totalLithium6Amount.ToString("0.000") + " L / " + totalLithium6MaxAmount.ToString("0.000") + " L", bold_style, text_style);

                        var lithiumConsumptionDay = lithium_consumed_per_second * PluginHelper.SecondsInDay;
                        PrintToGUILayout("Lithium Consumption", lithiumConsumptionDay.ToString("0.00000") + " L/day", bold_style, text_style);
                        var lithiumLifetimeTotalDays = lithiumConsumptionDay > 0 ? totalLithium6Amount / lithiumConsumptionDay : 0;

                        var lithiumLifetimeYears = Math.Floor(lithiumLifetimeTotalDays / GameConstants.KERBIN_YEAR_IN_DAYS);
                        var lithiumLifetimeYearsRemainderInDays = lithiumLifetimeTotalDays % GameConstants.KERBIN_YEAR_IN_DAYS;

                        var lithiumLifetimeRemainingDays = Math.Floor(lithiumLifetimeYearsRemainderInDays);
                        var lithiumLifetimeRemainingDaysRemainer = lithiumLifetimeYearsRemainderInDays % 1;

                        var lithiumLifetimeRemainingHours = lithiumLifetimeRemainingDaysRemainer * PluginHelper.SecondsInDay / GameConstants.SECONDS_IN_HOUR;

                        if (lithiumLifetimeYears < 1e9)
                        {
                            if (lithiumLifetimeYears < 1)
                                PrintToGUILayout("Lithium Remaining", lithiumLifetimeRemainingDays + " days " + lithiumLifetimeRemainingHours.ToString("0.0") + " hours", bold_style, text_style);
                            else if (lithiumLifetimeYears < 1e3)
                                PrintToGUILayout("Lithium Remaining", lithiumLifetimeYears + " years " + lithiumLifetimeRemainingDays + " days", bold_style, text_style);
                            else if (lithiumLifetimeYears < 1e6)
                                PrintToGUILayout("Lithium Remaining", lithiumLifetimeYears + " years " + lithiumLifetimeRemainingDays + " days", bold_style, text_style);
                            else
                                PrintToGUILayout("Lithium Remaining", lithiumLifetimeYears + " years " , bold_style, text_style);
                        }

                        double totalTritiumAmount;
                        double totalTritiumMaxAmount;
                        part.GetConnectedResourceTotals(tritium_def.id, out totalTritiumAmount, out totalTritiumMaxAmount);

                        var massTritiumAmount = totalTritiumAmount * tritium_density * 1000;
                        var massTritiumMaxAmount = totalTritiumMaxAmount * tritium_density * 1000;

                        PrintToGUILayout("Tritium Storage", massTritiumAmount.ToString("0.000000") + " kg / " + massTritiumMaxAmount.ToString("0.000000") + " kg", bold_style, text_style);

                        double totalHeliumAmount;
                        double totalHeliumMaxAmount;
                        part.GetConnectedResourceTotals(helium_def.id, out totalHeliumAmount, out totalHeliumMaxAmount);

                        var massHeliumAmount = totalHeliumAmount * helium4_density * 1000;
                        var massHeliumMaxAmount = totalHeliumMaxAmount * helium4_density * 1000;

                        PrintToGUILayout("Helium Storage", massHeliumAmount.ToString("0.000000") + " kg / " + massHeliumMaxAmount.ToString("0.000000") + " kg", bold_style, text_style);
                    }
                    else
                        PrintToGUILayout("Is Neutron rich", IsFuelNeutronRich.ToString(), bold_style, text_style);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Fuels:", bold_style, GUILayout.Width(150));
                    GUILayout.EndHorizontal();

                    foreach (var fuel in current_fuel_variant.ReactorFuels)
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
                        PrintToGUILayout(fuel.FuelName + " Reserves", PluginHelper.formatMassStr(availabilityInTon) + variantText, bold_style, text_style);

                        var tonFuelUsePerHour = ongoing_total_power_generated * fuel.TonsFuelUsePerMJ * fuelUsePerMJMult / FuelEfficiency * PluginHelper.SecondsInHour;
                        var kgFuelUsePerHour = tonFuelUsePerHour * 1000;
                        var kgFuelUsePerDay = kgFuelUsePerHour * PluginHelper.HoursInDay;

                        if (tonFuelUsePerHour > 120)
                            PrintToGUILayout(fuel.FuelName + " Consumption ", PluginHelper.formatMassStr(tonFuelUsePerHour / 60) + " / min", bold_style, text_style);
                        else
                            PrintToGUILayout(fuel.FuelName + " Consumption ", PluginHelper.formatMassStr(tonFuelUsePerHour) + " / hour", bold_style, text_style);

                        if (kgFuelUsePerDay > 0)
                        {
                            var fuelLifetimeD = availabilityInTon * 1000 / kgFuelUsePerDay;
                            var lifetimeYears = Math.Floor(fuelLifetimeD / GameConstants.KERBIN_YEAR_IN_DAYS);
                            if (lifetimeYears < 1e9)
                            {
                                if (lifetimeYears > 0)
                                {
                                    var lifetimeYearsDayRemainder = lifetimeYears < 1e+6 ? fuelLifetimeD % GameConstants.KERBIN_YEAR_IN_DAYS : 0;
                                    PrintToGUILayout(fuel.FuelName + " Lifetime", (double.IsNaN(lifetimeYears) ? "-" : lifetimeYears + " years " + (lifetimeYearsDayRemainder).ToString("0.00")) + " days", bold_style, text_style);
                                }
                                else if (fuelLifetimeD < 1)
                                {
                                    var minutesD = fuelLifetimeD * PluginHelper.HoursInDay * 60;
                                    var minutes = (int)Math.Floor(minutesD);
                                    var seconds = (int)Math.Ceiling((minutesD - minutes) * 60);

                                    PrintToGUILayout(fuel.FuelName + " Lifetime", minutes.ToString("F0") + " minutes " + seconds.ToString("F0") + " seconds", bold_style, text_style);
                                }
                                else
                                    PrintToGUILayout(fuel.FuelName + " Lifetime", (double.IsNaN(fuelLifetimeD) ? "-" : (fuelLifetimeD).ToString("0.00")) + " days", bold_style, text_style);
                            }
                            else
                                PrintToGUILayout(fuel.FuelName + " Lifetime", "", bold_style, text_style);
                        }
                        else
                            PrintToGUILayout(fuel.FuelName + " Lifetime", "", bold_style, text_style);
                    }

                    if (current_fuel_variant.ReactorProducts.Count > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Products:", bold_style, GUILayout.Width(150));
                        GUILayout.EndHorizontal();

                        foreach (var product in current_fuel_variant.ReactorProducts)
                        {
                            if (product == null)
                                continue;

                            var availabilityInTon = GetProductAvailability(product) * product.DensityInTon;
                            var maxAvailabilityInTon = GetMaxProductAvailability(product) * product.DensityInTon;

                            GUILayout.BeginHorizontal();
                            GUILayout.Label(product.FuelName + " Storage", bold_style, GUILayout.Width(150));
                            GUILayout.Label(PluginHelper.formatMassStr(availabilityInTon, "0.00000") + " / " + PluginHelper.formatMassStr(maxAvailabilityInTon, "0.00000"), text_style, GUILayout.Width(150));
                            GUILayout.EndHorizontal();

                            var hourProductionInTon = ongoing_total_power_generated * product.TonsProductUsePerMJ * fuelUsePerMJMult / FuelEfficiency * PluginHelper.SecondsInHour;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(product.FuelName + " Production", bold_style, GUILayout.Width(150));
                            GUILayout.Label(PluginHelper.formatMassStr(hourProductionInTon) + " / hour", text_style, GUILayout.Width(150));
                            GUILayout.EndHorizontal();
                        }
                    }
                }

                if (!IsNuclear)
                {
                    GUILayout.BeginHorizontal();

                    if (IsEnabled && canShutdown && GUILayout.Button("Deactivate", GUILayout.ExpandWidth(true)))
                        DeactivateReactor();
                    if (!IsEnabled && GUILayout.Button("Activate", GUILayout.ExpandWidth(true)))
                        ActivateReactor();

                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (IsEnabled)
                    {
                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button("Shutdown", GUILayout.ExpandWidth(true)))
                            IsEnabled = false;

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
                GUI.DragWindow();
            }

            catch (Exception e)
            {
                Debug.LogError("[KSPI]: InterstellarReactor Window(" + windowID + "): " + e.Message);
                throw;
            }
        }

        public override string getResourceManagerDisplayName()
        {
            var displayName = part.partInfo.title;
            if (fuel_modes.Count > 1 )
                displayName += " (" + fuelModeStr + ")";
            if (similarParts != null && similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }
    }
}