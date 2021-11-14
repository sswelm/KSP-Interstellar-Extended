using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    [KSPModule("Thermal Aerospike")]
    class ThermalAerospikeController : ThermalEngineController { }

    [KSPModule("Thermal Nozzle")]
    class ThermalNozzleController : ThermalEngineController { }

    [KSPModule("Plasma Nozzle")]
    class PlasmaNozzleController : ThermalEngineController { }

    [KSPModule("Thermal Engine")]
    class ThermalEngineController : ResourceSuppliableModule, IFNEngineNoozle, IUpgradeableModule, IRescalable<ThermalEngineController>
    {
        public const string Group = "ThermalEngineController";
        public const string GroupTitle = "#LOC_KSPIE_ThermalNozzleController_groupName";

        // Persistent True
        [KSPField(isPersistant = true)] public double storedAbsoluteFactor = 1;
        [KSPField(isPersistant = true)] public double storedFractionThermalReciever = 1;
        [KSPField(isPersistant = true)] public bool IsEnabled;
        [KSPField(isPersistant = true)] public bool isupgraded;
        [KSPField(isPersistant = true)] public int fuel_mode;
        [KSPField(isPersistant = true)] public bool isDeployed;
        [KSPField(isPersistant = true)] public double animationStarted;
        [KSPField(isPersistant = true)] public bool exhaustAllowed = true;
        [KSPField(isPersistant = true)] public bool canActivatePowerSource;
        [KSPField(isPersistant = true)] public float windowPositionX = 1000;
        [KSPField(isPersistant = true)] public float windowPositionY = 200;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_IspThrottle")
         , UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = 0, affectSymCounterparts = UI_Scene.All)]//Isp Throttle
        public float ispThrottle = 0;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_FuelFlowThrottle")
         , UI_FloatRange(stepIncrement = 10, maxValue = 1000, minValue = 100, affectSymCounterparts = UI_Scene.All)]//Fuel Flow Throttle
        public float fuelFlowThrottle = 100;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "MHD Power %")
         , UI_FloatRange(stepIncrement = 1f, maxValue = 200, minValue = 0, affectSymCounterparts = UI_Scene.All)]
        public float mhdPowerGenerationPercentage = 101;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_Radius", guiUnits = " m", guiFormat = "F2")]//Radius
        public double radius = 2.5;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiFormat = "F5", guiName = "#LOC_KSPIE_ThermalNozzleController_MaxFuelFlow")] protected double maxFuelFlowRate;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiFormat = "F5", guiName = "#LOC_KSPIE_ThermalNozzleController_MaxFuelFlowonengine")] public float maxFuelFlowOnEngine;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiFormat = "F5", guiName = "#LOC_KSPIE_ThermalNozzleController_FuelFlowMultplier")] public double fuelflowMultplier;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiFormat = "F5", guiName = "#LOC_KSPIE_ThermalNozzleController_FuelflowThrotlemodifier")] public double fuelFlowThrottleModifier = 1;

        [KSPField] public bool controlWasteHeatBuffer = true;
        [KSPField] public double fuelflowThrottleMaxValue = 100;
        [KSPField] public double effectiveFuelflowThrottle;
        [KSPField] public double ispFlowMultiplier;
        [KSPField] public double requiredElectricalPowerFromMhd;
        [KSPField] public double requiredMhdEnergyRatio;
        [KSPField] public double exhaustModifier;
        [KSPField] public double maxEngineFuelFlow;
        [KSPField] public double fuelEffectRatio;
        [KSPField] public float powerEffectRatio;
        [KSPField] public float runningEffectRatio;
        [KSPField] public double startupHeatReductionRatio;
        [KSPField] public double missingPrecoolerProportionExponent = 0.5;
        [KSPField] public double minimumBaseIsp = 0;
        [KSPField] public bool canUsePureChargedPower = false;
        [KSPField] public float takeoffIntakeBonus = 0.002f;
        [KSPField] public float jetengineAccelerationBaseSpeed = 0.2f;
        [KSPField] public float jetengineDecelerationBaseSpeed = 0.4f;
        [KSPField] public double engineAccelerationBaseSpeed = 2;
        [KSPField] public double engineDecelerationBaseSpeed = 2;
        [KSPField] public double wasteheatRatioDecelerationMult = 10;
        [KSPField] public float finalEngineDecelerationSpeed;
        [KSPField] public float finalEngineAccelerationSpeed;
        [KSPField] public bool useEngineResponseTime;
        [KSPField] public bool initialized;
        [KSPField] public float wasteHeatMultiplier = 1;
        [KSPField] public int jetPerformanceProfile = 0;
        [KSPField] public bool isJet = false;
        [KSPField] public float powerTrustMultiplier = 1;
        [KSPField] public float powerTrustMultiplierJet = 1;
        [KSPField] public double IspTempMultOffset = -1.371670613;
        [KSPField] public float sootHeatDivider = 150;
        [KSPField] public float sootThrustDivider = 150;
        [KSPField] public double maxTemp = 2750;
        [KSPField] public double heatConductivity = 0.12;
        [KSPField] public double heatConvectiveConstant = 1;
        [KSPField] public double emissiveConstant = 0.85;
        [KSPField] public float thermalMassModifier = 1f;
        [KSPField] public double skinMaxTemp = 2750;
        [KSPField] public float maxThermalNozzleIsp;
        [KSPField] public float maxJetModeBaseIsp = 0;
        [KSPField] public float maxLfoModeBaseIsp = 0;
        [KSPField] public float effectiveIsp;
        [KSPField] public double skinInternalConductionMult = 1;
        [KSPField] public double skinThermalMassModifier = 1;
        [KSPField] public double skinSkinConductionMult = 1;
        [KSPField] public string deployAnimationName = "";
        [KSPField] public string pulseAnimationName = "";
        [KSPField] public string emiAnimationName = "";
        [KSPField] public float pulseDuration = 0;
        [KSPField] public float recoveryAnimationDivider = 1;
        [KSPField] public double wasteheatEfficiencyLowTemperature = 0.99;
        [KSPField] public double wasteheatEfficiencyHighTemperature = 0.99;
        [KSPField] public float upgradeCost = 1;
        [KSPField] public string upgradeTechReq = "";
        [KSPField] public string EffectNameJet;
        [KSPField] public string EffectNameLFO;
        [KSPField] public string EffectNameNonLFO;
        [KSPField] public string EffectNameLithium;
        [KSPField] public string EffectNameSpool;
        [KSPField] public string runningEffectNameLFO;
        [KSPField] public string runningEffectNameNonLFO;
        [KSPField] public string powerEffectNameLFO;
        [KSPField] public string powerEffectNameNonLFO;
        [KSPField] public float windowWidth = 200;
        [KSPField] public double ispCoreTempMult = 0;
        [KSPField] public bool showPartTemperature = true;
        [KSPField] public double baseMaxIsp;
        [KSPField] public double wasteHeatBufferMassMult = 2.0e+5;
        [KSPField] public double wasteHeatBufferMult = 1;
        [KSPField] public bool allowUseOfChargedPower = true;
        [KSPField] public bool overrideAtmCurve = true;
        [KSPField] public bool overrideVelocityCurve = true;
        [KSPField] public bool overrideAtmosphereCurve = true;
        [KSPField] public bool overrideAccelerationSpeed = true;
        [KSPField] public bool overrideDecelerationSpeed = true;
        [KSPField] public bool usePropellantBaseIsp = false;
        [KSPField] public bool isPlasmaNozzle = false;
        [KSPField] public bool canUsePlasmaPower = false;
        [KSPField] public double requiredMegajouleRatio = 0;
        [KSPField] public double exitArea = 1;
        [KSPField] public double exitAreaScaleExponent = 2;
        [KSPField] public bool showThrustPercentage = true;
        [KSPField] public string throttleAnimName;
        [KSPField] public float throttleAnimExp = 1;

        //GUI
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ExitArea", guiUnits = " m\xB2", guiFormat = "F3")]//Exit Area
        public double scaledExitArea = 1;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_AfterburnerTechReq")]//Afterburner upgrade tech
        public string afterburnerTechReq = string.Empty;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_Propellant")]//Propellant
        public string _fuelmode;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_IspPropellantMultiplier", guiFormat = "F3")]//Propellant Isp Multiplier
        public double _ispPropellantMultiplier = 1;
        [KSPField(groupName = Group, isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_ThermalNozzleController_SootAccumulation", guiUnits = " %", guiFormat = "F3")]//Soot Accumulation
        public double sootAccumulationPercentage;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_MaxSoot")]//Max Soot
        public float _propellantSootFactorFullThrotle;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_MinSoot")]//Min Soot
        public float _propellantSootFactorMinThrotle;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_EquilibriumSoot")]//Equilibrium Soot
        public float _propellantSootFactorEquilibrium;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_Temperature")]//Temperature
        public string temperatureStr = "";
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ISPThrustMult")]//ISP / Thrust Mult
        public string thrustIspMultiplier = "";
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_FuelThrustMultiplier", guiFormat = "F3")]//Fuel Thrust Multiplier
        public double _thrustPropellantMultiplier = 1;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_UpgradeCost")]//Upgrade Cost
        public string upgradeCostStr;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ControlHeatProduction")]//Control Heat Production
        public bool controlHeatProduction = true;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_HeatExponent")]//Heat Exponent
        public float heatProductionExponent = 7.1f;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_RadiusHeatExponent")]//Radius Heat Exponent
        public double radiusHeatProductionExponent = 0.25;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_RadiusHeatMultiplier")]//Radius Heat Multiplier
        public double radiusHeatProductionMult = 10;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_HeatProductionMultiplier")]//Heat Production Multiplier
        public double heatProductionMultiplier = 1;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_Ispmodifier")]//Isp modifier
        public double ispHeatModifier;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_RadiusHeatModifier")]//Radius modifier
        public double radiusHeatModifier;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_EngineHeatProductionMult")]//Engine Heat Production Mult
        public double engineHeatProductionMult;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_PowerToMass")]//Power To Mass
        public double powerToMass;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_SpaceHeatProduction")]//Space Heat Production
        public double spaceHeatProduction = 100;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_EngineHeatProduction", guiFormat = "F2")]//Engine Heat Production
        public double engineHeatProduction;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_MaxThrustOnEngine", guiFormat = "F1", guiUnits = " kN")]//Max Thrust On Engine
        public float maxThrustOnEngine;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_EffectiveIspOnEngine")]//Effective Isp On Engine
        public float realIspEngine;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_Threshold", guiUnits = " kN", guiFormat = "F5")]//Threshold
        public double pressureThreshold;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_RequestedThermalHeat", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F2")]//Requested ThermalHeat
        public double requested_thermal_power;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_RequestedCharge", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Requested Charge
        public double requested_charge_particles;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_RecievedPower", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F2")]//Recieved Power
        public double reactor_power_received;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_RadiusModifier")]//Radius Modifier
        public string radiusModifier;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_Vacuum")]//Vacuum
        public string vacuumPerformance;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_Sea")]//Sea
        public string surfacePerformance;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_BaseIsp")]//Base Isp
        protected float _baseIspMultiplier;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_DecompositionEnergy")]//Decomposition Energy
        protected float _decompositionEnergy;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_EngineMaxThrust", guiFormat = "F1", guiUnits = " kN")]//Engine Max Thrust
        protected double engineMaxThrust;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ThrustPerMJ", guiFormat = "F3", guiUnits = " kN")]//Thrust Per MJ
        protected double thrustPerMegaJoule;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_MaxHydrogenThrustInSpace")]//Max Hydrogen Thrust In Space
        protected double max_thrust_in_space;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_FinalMaxThrustInSpace", guiFormat = "F1", guiUnits = " kN")]//Final Max Thrust In Space
        protected double final_max_thrust_in_space;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ThrustInCurrentAtmosphere")]//Thrust In Current Atmosphere
        protected double max_thrust_in_current_atmosphere;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_CurrentMaxEngineThrust", guiFormat = "F1", guiUnits = " kN")]//Current Max Engine Thrust
        protected double final_max_engine_thrust;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_MaximumISP", guiFormat = "F1", guiUnits = "s")]//Maximum ISP
        protected double _maxISP;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_MinimumISP", guiFormat = "F1", guiUnits = "s")]//Minimum ISP
        protected double _minISP;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_MaxCalculatedThrust", guiFormat = "F1", guiUnits = " kN")]//Max Calculated Thrust
        protected double calculatedMaxThrust;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_CurrentMassFlow", guiFormat = "F5")]//Current Mass Flow
        protected double currentMassFlow;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_IsOpenCycleCooler", guiFormat = "F5")]//Is Open CycleCooler
        protected bool isOpenCycleCooler;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_FuelFlowForCooling", guiFormat = "F5")]//Fuel Flow ForCooling
        protected double fuelFlowForCooling;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_AirCooling", guiFormat = "F5")]//Air Cooling
        protected double airFlowForCooling;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_CurrentIsp", guiFormat = "F1")]//Current Isp
        protected double current_isp;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_MaxPressureThresholdAtKerbinSurface", guiFormat = "F1", guiUnits = " kPa")]//Max Pressure Thresshold @ 1 atm
        protected double maxPressureThresholdAtKerbinSurface;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ThermalRatio")]//Thermal Ratio
        protected double thermalResourceRatio;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ChargedPowerRatio")]//Charged Power Ratio
        protected double chargedResourceRatio;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ExpectedMaxThrust")]//Expected Max Thrust
        protected double expectedMaxThrust;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_IsLFO")]//Is LFO
        protected bool _propellantIsLFO;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_VelocityModifier", guiFormat = "F3")]//Velocity Modifier
        protected float vcurveAtCurrentVelocity;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_AtmosphereModifier", guiFormat = "F3")]//Atmosphere Modifier
        protected float atmosphereModifier;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_AtomType")]//Atom Type
        protected int _atomType = 1;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_PropellantType")]//Propellant Type
        protected int _propType = 1;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_IsNeutronAbsorber")]//Is Neutron Absorber
        protected bool _isNeutronAbsorber = false;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiFormat = "F2", guiName = "#LOC_KSPIE_ThermalNozzleController_MaxThermalPower", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Max Thermal Power
        protected double currentMaxThermalPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiFormat = "F2", guiName = "#LOC_KSPIE_ThermalNozzleController_MaxChargedPower", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Max Charged Power
        protected double currentMaxChargedPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiFormat = "F2", guiName = "#LOC_KSPIE_ThermalNozzleController_AvailableTPower", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Available T Power
        protected double availableThermalPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiFormat = "F2", guiName = "#LOC_KSPIE_ThermalNozzleController_AvailableCPower", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Available C Power
        protected double availableChargedPower;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_AirFlowHeatModifier", guiFormat = "F3")]//Air Flow Heat Modifier
        protected double airflowHeatModifier;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ThermalPowerSupply", guiFormat = "F2")]//Thermal Power Supply
        protected double effectiveThermalSupply;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ChargedPowerSupply", guiFormat = "F2")]//Charged Power Supply
        protected double effectiveChargedSupply;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumPowerUsageForPropulsionRatio;
        [KSPField(guiActive = true,  groupName = Group, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumThermalPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumChargedPower;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiFormat = "F2", guiName = "#LOC_KSPIE_ThermalNozzleController_MaximumReactorPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Maximum Reactor Power
        public double maximumReactorPower;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiFormat = "F3", guiName = "#LOC_KSPIE_ThermalNozzleController_HeatThrustModifier")]//Heat Thrust Modifier
        public double heatThrustModifier;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiFormat = "F3", guiName = "#LOC_KSPIE_ThermalNozzleController_PowerThrustModifier")]//Heat Thrust Modifier
        public double powerThrustModifier;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_EffectiveThrustFraction")]//Effective Thrust Fraction
        public double effectiveThrustFraction = 1;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_ThermalNozzleController_ElectricalyPowered", guiUnits = "%", guiFormat = "F1")]//Electricaly Powered
        public double received_megajoules_percentage;
        [KSPField(groupName = Group, isPersistant = true, guiActive = false, guiName = "Jet Spool Ratio", guiFormat = "F2")]
        public float jetSpoolRatio;
        [KSPField(groupName = Group, isPersistant = false, guiActive = false, guiName = "Spool Effect Ratio", guiFormat = "F2")]
        public float spoolEffectRatio;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_PropelantWindow"), UI_Toggle(disabledText = "#LOC_KSPIE_ThermalNozzleController_WindowHidden", enabledText = "#LOC_KSPIE_ThermalNozzleController_WindowShown", affectSymCounterparts = UI_Scene.None)]//Propelant Window--Hidden--Shown
        public bool render_window;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ModuleSabreHeating_MissingPrecoolerRatio")]
        public double missingPrecoolerRatio;

        [KSPField] public bool hasJetUpgradeTech1;
        [KSPField] public bool hasJetUpgradeTech2;
        [KSPField] public bool hasJetUpgradeTech3;
        [KSPField] public bool hasJetUpgradeTech4;
        [KSPField] public bool hasJetUpgradeTech5;

        [KSPField] public int supportedPropellantAtoms = 511;
        [KSPField] public int supportedPropellantTypes = 511;

        [KSPField] public float requestedThrottle;
        [KSPField] public float effectiveJetengineAccelerationSpeed;
        [KSPField] public float effectiveJetengineDecelerationSpeed;

        [KSPField] public double baseJetHeatproduction = 0;
        [KSPField] public double coreTemperature = 3000;
        [KSPField] public double minimumThrust = 0.000001;
        [KSPField] public double plasmaAfterburnerHeatModifier = 0.5;
        [KSPField] public double thermalHeatModifier = 5;
        [KSPField] public double currentThrottle;
        [KSPField] public double previousThrottle;
        [KSPField] public double delayedThrottle;
        [KSPField] public double previousDelayedThrottle;
        [KSPField] public double adjustedThrottle;
        [KSPField] public double adjustedFuelFlowMult;
        [KSPField] public double adjustedFuelFlowExponent = 2;

        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)]
        public double receivedMegajoulesRatio;

        [KSPField] public double minThrottle = 0;

        // Constants
        private const double HydroloxDecompositionEnergy = 16.2137;

        //Internal
        private string _flameoutText;
        private string _powerEffectNameParticleFx;
        private string _runningEffectNameParticleFx;
        private string _fuelTechRequirement;

        private double _mhdTrustIspModifier = 1;
        private double _heatDecompositionFraction;

        private float _fuelCoolingFactor = 1;
        private float _fuelToxicity;
        private float _fuelMinimumCoreTemp;
        private float _currentAnimationRatio;
        private float _minDecompositionTemp;
        private float _maxDecompositionTemp;
        private float _originalEngineAccelerationSpeed;
        private float _originalEngineDecelerationSpeed;
        private float _jetTechBonus;
        private float _jetTechBonusPercentage;
        private float _jetTechBonusCurveChange;

        private int _windowId;
        private int _switches;

        private bool _checkedConnectivity;
        private bool _showIspThrottle;
        private bool _fuelRequiresUpgrade;
        private bool _engineWasInactivePreviousFrame;
        private bool _hasRequiredUpgrade;
        private bool _hasSetupPropellant;
        private bool _currentPropellantIsJet;

        private BaseField _fuelFlowThrottleField;
        private BaseField _sootAccumulationPercentageField;
        private BaseField _upgradeCostStrField;
        private BaseEvent _retrofitEngineEvent;

        private FloatCurve _atmCurve;
        private FloatCurve _atmosphereCurve;
        private FloatCurve _velCurve;

        private FloatCurve _originalAtmCurve;
        private FloatCurve _originalAtmosphereCurve;
        private FloatCurve _originalVelocityCurve;

        private Animation deployAnim;
        private Animation throttleAnimation;
        private AnimationState[] pulseAnimationState;
        private AnimationState[] emiAnimationState;
        private ResourceBuffers resourceBuffers;
        private ModuleEnginesThermalNozzle timewarpEngine;
        private ConfigNode[] fuelConfigNodes;

        private readonly Guid id = Guid.NewGuid();
        private readonly List<Propellant> _listOfPropellants = new List<Propellant>();

        private List<FNModulePrecooler> _vesselPreCoolers;
        private List<AtmosphericIntake> _vesselResourceIntakes;
        private List<ThermalEngineFuel> _allThermalEngineFuels;
        private List<ThermalEngineFuel> _compatibleThermalEngineFuels;

        private Rect _windowPosition;

        private IFNPowerSource _myAttachedReactor;
        public IFNPowerSource AttachedReactor
        {
            get => _myAttachedReactor;
            private set
            {
                _myAttachedReactor = value;
                _myAttachedReactor?.AttachThermalReciever(id, radius);
            }
        }

        public ModuleEngines AttachedEngine { get; private set; }

        public string UpgradeTechnology => upgradeTechReq;

        public double EffectiveCoreTempIspMult => (ispCoreTempMult == 0 ? PluginSettings.Config.IspCoreTempMult : ispCoreTempMult) + IspTempMultOffset;

        public bool UsePlasmaPower => isPlasmaNozzle || canUsePlasmaPower && (AttachedReactor != null && AttachedReactor.PlasmaPropulsionEfficiency > 0);

        public bool UseThermalPowerOnly => AttachedReactor != null &&
                                           (!isPlasmaNozzle || !SourceCanProducePlasmaOrChargedPower || AttachedReactor.SupportMHD || AttachedReactor.ChargedPowerRatio == 0 || _listOfPropellants.Count > 1 );

        public bool UseThermalAndChargedPower => !UseThermalPowerOnly && ispThrottle == 0;

        public bool SourceCanProducePlasmaOrChargedPower => AttachedReactor.ChargedParticlePropulsionEfficiency > 0 || AttachedReactor.PlasmaPropulsionEfficiency > 0;

        public bool UsePlasmaAfterBurner => SourceCanProducePlasmaOrChargedPower && isPlasmaNozzle && (ispThrottle != 100 || !canUsePureChargedPower);

        public bool UseChargedPowerOnly => canUsePureChargedPower && ispThrottle == 100 && _listOfPropellants.Count == 1;

        public void NextPropellantInternal()
        {
            fuel_mode++;
            if (fuel_mode >= fuelConfigNodes.Length)
                fuel_mode = 0;

            SetupPropellants(fuel_mode, true, false);
        }

        public void PreviousPropellantInternal()
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuelConfigNodes.Length - 1;

            SetupPropellants(fuel_mode, false, false);
        }

        // Note: we assume OnRescale is called at load and after any time tweakscale changes the size of an part
        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            Debug.Log("[KSPI]: ThermalNozzleController OnRescale was called with factor " + factor.absolute.linear);

            storedAbsoluteFactor = (double)(decimal)factor.absolute.linear;

            ScaleParameters();

            // update simulation
            EstimateEditorPerformance();
            UpdateRadiusModifier();
            UpdateIspEngineParams();
        }

        private void ScaleParameters()
        {
            scaledExitArea = exitArea * Math.Pow(storedAbsoluteFactor, exitAreaScaleExponent);
        }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_NextPropellant", active = true)]//Next Propellant
        public void NextPropellant()
        {
            fuel_mode++;
            if (fuel_mode >= fuelConfigNodes.Length)
                fuel_mode = 0;

            SetupPropellants(true, false);
        }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ThermalNozzleController_PreviousPropellant", active = true)]//Previous Propellant
        public void PreviousPropellant()
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuelConfigNodes.Length - 1;

            SetupPropellants(false, false);
        }

        [KSPAction("Next Propellant")]
        public void TogglePropellantAction(KSPActionParam param)
        {
            NextPropellantInternal();
        }

        [KSPAction("Previous Propellant")]
        public void PreviousPropellant(KSPActionParam param)
        {
            PreviousPropellantInternal();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ThermalNozzleController_Retrofit", active = true)]//Retrofit
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public float CurrentThrottle
        {
            get
            {
                if (AttachedEngine != null && AttachedEngine.isOperational && exhaustAllowed)
                    return (float)(adjustedThrottle * receivedMegajoulesRatio * effectiveThrustFraction * fuelFlowThrottleModifier);
                else
                    return 0;
            }
        }

        public double RequestedThrottle
        {
            get
            {
                if (AttachedEngine != null && AttachedEngine.isOperational && exhaustAllowed)
                    return requestedThrottle;
                else
                    return 0;
            }
        }

        public double ReactorWasteheatModifier
        {
            get
            {
                var baseWasteheatEfficiency = isPlasmaNozzle ? wasteheatEfficiencyHighTemperature : wasteheatEfficiencyLowTemperature;
                var reactorWasteheatModifier = AttachedReactor == null ? 1 : isPlasmaNozzle ? AttachedReactor.PlasmaWasteheatProductionMult : AttachedReactor.EngineWasteheatProductionMult;
                var wasteheatEfficiencyModifier = (1 - baseWasteheatEfficiency) * reactorWasteheatModifier;
                if (_fuelCoolingFactor > 0)
                    wasteheatEfficiencyModifier /= _fuelCoolingFactor;
                return wasteheatEfficiencyModifier;
            }
        }

        public bool PropellantAbsorbsNeutrons => _isNeutronAbsorber;

        public bool IsPlasmaNozzle => isPlasmaNozzle;
        public bool RequiresPlasmaHeat => UsePlasmaPower;
        public bool RequiresThermalHeat => !UsePlasmaPower;
        public bool RequiresChargedPower => false;

        public void upgradePartModule()
        {
            isupgraded = true;

            fuelConfigNodes = isJet ? GetPropellantsHybrid() : GetPropellants(isJet);
        }

        public void OnEditorAttach()
        {
            ConnectToThermalSource();

            if (AttachedReactor == null) return;

            LoadFuelModes();

            EstimateEditorPerformance();

            SetupPropellants(fuel_mode);
        }

        public void OnEditorDetach()
        {
            foreach (var symPart in part.symmetryCounterparts)
            {
                var symThermalNozzle = symPart.FindModuleImplementing<ThermalEngineController>();

                if (symThermalNozzle == null) continue;

                Debug.Log("[KSPI]: called DetachWithReactor on symmetryCounterpart");
                symThermalNozzle.DetachWithReactor();
            }

            DetachWithReactor();
        }

        public void DetachWithReactor()
        {
            if (AttachedReactor == null)
                return;

            AttachedReactor.DetachThermalReciever(id);

            AttachedReactor.DisconnectWithEngine(this);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            timewarpEngine = part.FindModuleImplementing<ModuleEnginesThermalNozzle>();
            if (timewarpEngine != null)
            {
                timewarpEngine.Fields[nameof(ModuleEnginesThermalNozzle.thrust_d)].Attribute.groupName = Group;
                timewarpEngine.Fields[nameof(ModuleEnginesThermalNozzle.thrust_d)].Attribute.groupDisplayName = GroupTitle;
            }
            else
                Debug.LogError("[KSPI]: ThermalEngineController - failed to find ModuleEnginesThermalNozzle during load for " + part.name);

            AttachedEngine = part.FindModuleImplementing<ModuleEngines>();
            if (AttachedEngine != null)
            {
                AttachedEngine.Fields[nameof(ModuleEngines.status)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.status)].Attribute.groupDisplayName = GroupTitle;

                AttachedEngine.Fields[nameof(ModuleEngines.realIsp)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.realIsp)].Attribute.groupDisplayName = GroupTitle;

                AttachedEngine.Fields[nameof(ModuleEngines.finalThrust)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.finalThrust)].Attribute.groupDisplayName = GroupTitle;

                AttachedEngine.Fields[nameof(ModuleEngines.propellantReqMet)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.propellantReqMet)].Attribute.groupDisplayName = GroupTitle;

                AttachedEngine.Fields[nameof(ModuleEngines.fuelFlowGui)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.fuelFlowGui)].Attribute.groupDisplayName = GroupTitle;

                AttachedEngine.Fields[nameof(ModuleEngines.thrustPercentage)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.thrustPercentage)].Attribute.groupDisplayName = GroupTitle;

                AttachedEngine.Fields[nameof(ModuleEngines.independentThrottle)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.independentThrottle)].Attribute.groupDisplayName = GroupTitle;

                AttachedEngine.Fields[nameof(ModuleEngines.independentThrottlePercentage)].Attribute.groupName = Group;
                AttachedEngine.Fields[nameof(ModuleEngines.independentThrottlePercentage)].Attribute.groupDisplayName = GroupTitle;
            }
            else
                Debug.LogWarning("[KSPI]: ThermalNozzleController - failed to find engine during load for " + part.name);
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("[KSPI]: ThermalNozzleController - start");

            if (AttachedEngine == null)
                AttachedEngine = part.FindModuleImplementing<ModuleEngines>();

            if (AttachedEngine != null)
            {
                AttachedEngine.Fields[nameof(ModuleEngines.thrustPercentage)].guiActive = showThrustPercentage;

                _originalAtmCurve = AttachedEngine.atmCurve;
                _originalAtmosphereCurve = AttachedEngine.atmosphereCurve;
                _originalVelocityCurve = AttachedEngine.velCurve;

                _originalEngineAccelerationSpeed = AttachedEngine.engineAccelerationSpeed;
                _originalEngineDecelerationSpeed = AttachedEngine.engineDecelerationSpeed;
            }
            else
                Debug.LogError("[KSPI]: ThermalNozzleController - failed to find engine during start for " + part.name);

            resourcesToSupply = new[] { ResourceSettings.Config.ElectricPowerInMegawatt };

            base.OnStart(state);

            _windowId = new System.Random(part.GetInstanceID()).Next(int.MaxValue);
            _windowPosition = new Rect(windowPositionX, windowPositionY, windowWidth, 10);

            _flameoutText = Localizer.Format("#autoLOC_219016");

            // use default when not configured
            if (maxThermalNozzleIsp == 0)
                maxThermalNozzleIsp = (float)PluginSettings.Config.MaxThermalNozzleIsp;
            if (maxJetModeBaseIsp == 0)
                maxJetModeBaseIsp = maxThermalNozzleIsp;
            if (maxLfoModeBaseIsp == 0)
                maxLfoModeBaseIsp = maxThermalNozzleIsp;

            ScaleParameters();

            // make sure thermal values are fixed and not screwed up by Deadly Reentry
            part.maxTemp = maxTemp;
            part.emissiveConstant = emissiveConstant;
            part.heatConductivity = heatConductivity;
            part.thermalMassModifier = thermalMassModifier;
            part.heatConvectiveConstant = heatConvectiveConstant;

            part.skinMaxTemp = skinMaxTemp;
            part.skinSkinConductionMult = skinSkinConductionMult;
            part.skinThermalMassModifier = skinThermalMassModifier;
            part.skinInternalConductionMult = skinInternalConductionMult;

            if (!string.IsNullOrEmpty(deployAnimationName))
                deployAnim = part.FindModelAnimators(deployAnimationName).FirstOrDefault();
            if (!string.IsNullOrEmpty(pulseAnimationName))
                pulseAnimationState = PluginHelper.SetUpAnimation(pulseAnimationName, part);
            if (!string.IsNullOrEmpty(emiAnimationName))
                emiAnimationState = PluginHelper.SetUpAnimation(emiAnimationName, part);

            resourceBuffers = new ResourceBuffers();
            if (controlWasteHeatBuffer)
            {
                resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, wasteHeatBufferMassMult * wasteHeatBufferMult, true));
                resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            }

            resourceBuffers.Init(part);

            if (!string.IsNullOrEmpty(throttleAnimName))
                throttleAnimation = part.FindModelAnimators(throttleAnimName).FirstOrDefault();

            // find attached thermal source
            ConnectToThermalSource();

            maxPressureThresholdAtKerbinSurface = scaledExitArea * GameConstants.EarthAtmospherePressureAtSeaLevel;

            _fuelFlowThrottleField = Fields[nameof(fuelFlowThrottle)];

            var mhdPowerGenerationPercentageField = Fields[nameof(mhdPowerGenerationPercentage)];
            mhdPowerGenerationPercentageField.guiActive = requiredMegajouleRatio > 0;
            mhdPowerGenerationPercentageField.guiActiveEditor = requiredMegajouleRatio > 0;

            var ispThrottleField = Fields[nameof(ispThrottle)];
            ispThrottleField.guiActiveEditor = _showIspThrottle;
            ispThrottleField.guiActive = _showIspThrottle;

            if (state == StartState.Editor)
            {
                canActivatePowerSource = true;

                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;

                fuelConfigNodes = GetPropellants(isJet);
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }

                LoadFuelModes();

                SetupPropellants(fuel_mode);

                EstimateEditorPerformance();

                UpdateRadiusModifier();

                return;
            }

            if (requiredMegajouleRatio == 0)
                receivedMegajoulesRatio = 1;

            Fields[nameof(received_megajoules_percentage)].guiActive = requiredMegajouleRatio > 0;

            _sootAccumulationPercentageField = Fields[nameof(sootAccumulationPercentage)];
            _upgradeCostStrField = Fields[nameof(upgradeCostStr)];
            _retrofitEngineEvent = Events[nameof(RetrofitEngine)];

            UpdateRadiusModifier();

            UpdateIspEngineParams();

            // research all available pre-coolers, intakes and nozzles on the vessel
            _vesselPreCoolers = vessel.FindPartModulesImplementing<FNModulePrecooler>();
            _vesselResourceIntakes = vessel.FindPartModulesImplementing<AtmosphericIntake>();

            // if we can upgrade, let's do so
            if (isupgraded)
                upgradePartModule();
            else
            {
                if (this.HasTechsRequiredToUpgrade())
                    _hasRequiredUpgrade = true;

                // if not, use basic propellants
                fuelConfigNodes = GetPropellants(isJet);
            }

            hasJetUpgradeTech1 = PluginHelper.HasTechRequirementOrEmpty(PluginSettings.Config.JetUpgradeTech1);
            hasJetUpgradeTech2 = PluginHelper.HasTechRequirementOrEmpty(PluginSettings.Config.JetUpgradeTech2);
            hasJetUpgradeTech3 = PluginHelper.HasTechRequirementOrEmpty(PluginSettings.Config.JetUpgradeTech3);
            hasJetUpgradeTech4 = PluginHelper.HasTechRequirementOrEmpty(PluginSettings.Config.JetUpgradeTech4);
            hasJetUpgradeTech5 = PluginHelper.HasTechRequirementOrEmpty(PluginSettings.Config.JetUpgradeTech5);

            _jetTechBonus = 1 + Convert.ToInt32(hasJetUpgradeTech1) * 1.2f + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f * Convert.ToInt32(hasJetUpgradeTech3) + 2.0736f * Convert.ToInt32(hasJetUpgradeTech4) + 2.48832f * Convert.ToInt32(hasJetUpgradeTech5);
            _jetTechBonusCurveChange = _jetTechBonus / 9.92992f;
            _jetTechBonusPercentage = _jetTechBonus / 49.6496f;

            var reactorSpeed = AttachedReactor.ReactorSpeedMult > 0 ? AttachedReactor.ReactorSpeedMult : 1;

            effectiveJetengineAccelerationSpeed = overrideAccelerationSpeed ? jetengineAccelerationBaseSpeed * (float)reactorSpeed * _jetTechBonusCurveChange * 5 : _originalEngineAccelerationSpeed;
            effectiveJetengineDecelerationSpeed = overrideDecelerationSpeed ? jetengineDecelerationBaseSpeed * (float)reactorSpeed * _jetTechBonusCurveChange * 5 : _originalEngineDecelerationSpeed;

            Fields[nameof(temperatureStr)].guiActive = showPartTemperature;

            LoadFuelModes();

            SetupPropellants(fuel_mode);
        }

        private void UpdateConfigEffects()
        {
            if (!(AttachedEngine is ModuleEnginesFX)) return;

            if (!string.IsNullOrEmpty(EffectNameJet))
                part.Effect(EffectNameJet, 0, -1);
            if (!string.IsNullOrEmpty(EffectNameLFO))
                part.Effect(EffectNameLFO, 0, -1);
            if (!string.IsNullOrEmpty(EffectNameNonLFO))
                part.Effect(EffectNameNonLFO, 0, -1);
            if (!string.IsNullOrEmpty(EffectNameLithium))
                part.Effect(EffectNameLithium, 0, -1);

            if (!string.IsNullOrEmpty(runningEffectNameNonLFO))
                part.Effect(runningEffectNameNonLFO, 0, -1);
            if (!string.IsNullOrEmpty(runningEffectNameLFO))
                part.Effect(runningEffectNameLFO, 0, -1);

            if (!string.IsNullOrEmpty(powerEffectNameNonLFO))
                part.Effect(powerEffectNameNonLFO, 0, -1);
            if (!string.IsNullOrEmpty(powerEffectNameLFO))
                part.Effect(powerEffectNameLFO, 0, -1);


            if (_propellantIsLFO)
            {
                if (!string.IsNullOrEmpty(powerEffectNameLFO))
                    _powerEffectNameParticleFx = powerEffectNameLFO;
                else  if (!string.IsNullOrEmpty(EffectNameLFO))
                    _powerEffectNameParticleFx = EffectNameLFO;

                if (!string.IsNullOrEmpty(runningEffectNameLFO))
                    _runningEffectNameParticleFx = runningEffectNameLFO;
            }
            else if (_currentPropellantIsJet && !string.IsNullOrEmpty(EffectNameJet))
            {
                _powerEffectNameParticleFx = EffectNameJet;
            }
            else if (_isNeutronAbsorber)
            {
                if (!string.IsNullOrEmpty(EffectNameLithium))
                    _powerEffectNameParticleFx = EffectNameLithium;
                else if (!string.IsNullOrEmpty(EffectNameLFO))
                    _powerEffectNameParticleFx = EffectNameLFO;
            }
            else
            {
                if (!string.IsNullOrEmpty(powerEffectNameNonLFO))
                    _powerEffectNameParticleFx = powerEffectNameNonLFO;
                else if (!string.IsNullOrEmpty(EffectNameNonLFO))
                    _powerEffectNameParticleFx = EffectNameNonLFO;

                if (!string.IsNullOrEmpty(runningEffectNameNonLFO))
                    _runningEffectNameParticleFx = runningEffectNameNonLFO;
            }
        }

        private void ConnectToThermalSource()
        {
            var source = PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part, (p) => p.IsThermalSource && maxThermalNozzleIsp >= p.MinThermalNozzleTempRequired, 10, 10, 10);

            if (source?.Source == null)
            {
                Debug.LogWarning("[KSPI]: ThermalNozzleController - Failed to find thermal source");
                return;
            }

            AttachedReactor = source.Source;
            AttachedReactor.ConnectWithEngine(this);

            var partDistance = (int)Math.Max(Math.Ceiling(source.Cost) - 1, 0);

            if (AttachedReactor != null)
            {
                _showIspThrottle = isPlasmaNozzle
                    && SourceCanProducePlasmaOrChargedPower
                    && AttachedReactor.ChargedPowerRatio > 0;

                var ispThrottleField = Fields[nameof(ispThrottle)];
                if (ispThrottleField != null)
                {
                    ispThrottleField.guiActiveEditor = _showIspThrottle;
                    ispThrottleField.guiActive = _showIspThrottle;
                }
            }

            Debug.Log("[KSPI]: ThermalNozzleController - Found thermal source with distance " + partDistance);
        }

        // Is called in the VAB
        public virtual void Update()
        {
            if (!HighLogic.LoadedSceneIsEditor) return;

            EstimateEditorPerformance();

            UpdateRadiusModifier();

            UpdateIspEngineParams();
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate()
        {
            resourceBuffers.Init(part);

            if (_myAttachedReactor == null && _checkedConnectivity == false)
            {
                _checkedConnectivity = true;
                var message = "Warning: " + part.partInfo.title + " is not connected to a power source!";
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            // setup propellant after startup to allow InterstellarFuelSwitch to configure the propellant
            if (!_hasSetupPropellant)
            {
                _hasSetupPropellant = true;
                SetupPropellants(fuel_mode, true, true);
            }

            temperatureStr = part.temperature.ToString("F0") + "K / " + part.maxTemp.ToString("F0") + "K";
            UpdateAtmosphericPressureThreshold();

            _sootAccumulationPercentageField.guiActive = sootAccumulationPercentage > 0;

            thrustIspMultiplier = _ispPropellantMultiplier.ToString("0.00") + " / " + _thrustPropellantMultiplier.ToString("0.00");

            if (ResearchAndDevelopment.Instance != null && isJet)
            {
                _retrofitEngineEvent.active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasRequiredUpgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
            }
            else
                _retrofitEngineEvent.active = false;

            _upgradeCostStrField.guiActive = !isupgraded && _hasRequiredUpgrade && isJet;

            if (AttachedEngine == null)
                return;

            exhaustAllowed = AllowedExhaust();

            fuelflowMultplier = AttachedEngine.flowMultiplier;

            // only allow shutdown when engine throttle is down
            AttachedEngine.Events[nameof(ModuleEngines.Shutdown)].active = AttachedEngine.currentThrottle == 0 && AttachedEngine.getIgnitionState;

            if (AttachedEngine.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                part.force_activate();
            }

            if (!IsEnabled || deployAnim == null || initialized) return;

            if (isDeployed)
            {
                deployAnim[deployAnimationName].normalizedTime = 1;
                deployAnim[deployAnimationName].layer = 1;
                deployAnim.Blend(deployAnimationName);
                initialized = true;
            }
            else if (animationStarted == 0)
            {
                deployAnim[deployAnimationName].normalizedTime = 0;
                deployAnim[deployAnimationName].speed = 1;
                deployAnim[deployAnimationName].layer = 1;
                deployAnim.Blend(deployAnimationName);
                AttachedEngine.Shutdown();
                animationStarted = Planetarium.GetUniversalTime();
            }
            else if ((Planetarium.GetUniversalTime() > animationStarted + deployAnim[deployAnimationName].length))
            {
                initialized = true;
                isDeployed = true;
                AttachedEngine.Activate();
            }
        }

        private bool AllowedExhaust()
        {
            if (CheatOptions.IgnoreAgencyMindsetOnContracts)
                return true;

            var homeworld = FlightGlobals.GetHomeBody();
            var toHomeworld = vessel.CoMD - homeworld.position;
            var distanceToSurfaceHomeworld = toHomeworld.magnitude - homeworld.Radius;
            var cosineAngle = Vector3d.Dot(part.transform.up.normalized, toHomeworld.normalized);
            var currentExhaustAngle = Math.Acos(cosineAngle) * (180 / Math.PI);

            if (double.IsNaN(currentExhaustAngle) || double.IsInfinity(currentExhaustAngle))
                currentExhaustAngle = cosineAngle > 0 ? 180 : 0;

            if (AttachedReactor == null)
                return false;

            if (AttachedReactor.MayExhaustInAtmosphereHomeworld)
                return true;

            var minAltitude = AttachedReactor.MayExhaustInLowSpaceHomeworld ? homeworld.atmosphereDepth : homeworld.scienceValues.spaceAltitudeThreshold;

            if (distanceToSurfaceHomeworld < minAltitude)
                return false;

            if (AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 10 * homeworld.Radius)
                return true;

            if (!AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 20 * homeworld.Radius)
                return true;

            var radiusDividedByAltitude = (homeworld.Radius + minAltitude) / toHomeworld.magnitude;

            var coneAngle = 45 * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude;

            var allowedExhaustAngle = coneAngle + Math.Tanh(radiusDividedByAltitude) * (180 / Math.PI);

            if (allowedExhaustAngle < 3)
                return true;

            return currentExhaustAngle > allowedExhaustAngle;
        }

        public override void OnActive()
        {
            base.OnActive();

            LoadFuelModes();

            SetupPropellants(true, true);
        }

        public void LoadFuelModes()
        {
            _allThermalEngineFuels = fuelConfigNodes.Select(node => new ThermalEngineFuel(node, fuelConfigNodes.IndexOf(node), part)).ToList();

            // quit if we do not have access to reactor
            if (AttachedReactor == null)
            {
                Debug.LogWarning("[KSPI]: ThermalNozzleController - Skipped filtering on compatible fuel modes, no reactor available");
                return;
            }

            _compatibleThermalEngineFuels = _allThermalEngineFuels.Where(fuel =>

                    PluginHelper.HasTechRequirementOrEmpty(fuel.TechRequirement) &&

                    (fuel.RequiresUpgrade == false || _fuelRequiresUpgrade && isupgraded) &&
                    (fuel.IsLFO == false || (fuel.IsLFO && PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))) &&
                    (fuel.CoolingFactor >= AttachedReactor.MinCoolingFactor) &&
                    (fuel.MinimumCoreTemp <= AttachedReactor.MaxCoreTemperature) &&
                    ((fuel.AtomType & AttachedReactor.SupportedPropellantAtoms) == fuel.AtomType) &&
                    ((fuel.AtomType & supportedPropellantAtoms) == fuel.AtomType) &&
                    ((fuel.PropType & AttachedReactor.SupportedPropellantTypes) == fuel.PropType) &&
                    ((fuel.PropType & supportedPropellantTypes) == fuel.PropType)

                ).ToList();

            Debug.Log("[KSPI]: ThermalNozzleController - Found " + _compatibleThermalEngineFuels.Count +
                " compatible fuel modes out of " + _allThermalEngineFuels.Count + " available");

            var nextPropellantEvent = Events[nameof(NextPropellant)];
            if (nextPropellantEvent != null)
            {
                nextPropellantEvent.guiActive = _compatibleThermalEngineFuels.Count > 1;
                nextPropellantEvent.guiActiveEditor = _compatibleThermalEngineFuels.Count > 1;
            }

            var prevPropellantEvent = Events[nameof(PreviousPropellant)];
            if (prevPropellantEvent != null)
            {
                prevPropellantEvent.guiActive = _compatibleThermalEngineFuels.Count > 1;
                prevPropellantEvent.guiActiveEditor = _compatibleThermalEngineFuels.Count > 1;
            }
        }

        public void SetupPropellants(bool forward = true, bool notifySwitching = false)
        {
            SetupPropellants(fuel_mode, forward, notifySwitching);

            foreach (var symPart in part.symmetryCounterparts)
            {
                var symThermalNozzle = symPart.FindModuleImplementing<ThermalEngineController>();

                if (symThermalNozzle != null)
                    symThermalNozzle.SetupPropellants(fuel_mode, forward, notifySwitching);
            }
        }

        public void SetupPropellants(int newFuelMode, bool forward = true, bool notifySwitching = false)
        {
            if (_myAttachedReactor == null)
                return;

            fuel_mode = newFuelMode;

            var chosenPropellant = fuelConfigNodes[fuel_mode];

            UpdatePropellantModeBehavior(chosenPropellant);
            var propellantNodes = chosenPropellant.GetNodes("PROPELLANT");
            _listOfPropellants.Clear();

            foreach (var propNode in propellantNodes)
            {
                var extendedPropellant = new ExtendedPropellant();
                extendedPropellant.Load(propNode);

                if (_listOfPropellants == null)
                    Debug.LogWarning("[KSPI]: ThermalNozzleController - SetupPropellants list_of_propellants is null");

                _listOfPropellants.Add(extendedPropellant);
            }

            var missingResources = string.Empty;
            var canLoadPropellant =
                !(_listOfPropellants.Any(m => PartResourceLibrary.Instance.GetDefinition(m.name) == null)
                  || (!PluginHelper.HasTechRequirementOrEmpty(_fuelTechRequirement))
                  || (_fuelRequiresUpgrade && !isupgraded)
                  || (_fuelMinimumCoreTemp > AttachedReactor.MaxCoreTemperature)
                  || (_fuelCoolingFactor < AttachedReactor.MinCoolingFactor)
                  || (_propellantIsLFO && !PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))
                  || ((_atomType & AttachedReactor.SupportedPropellantAtoms) != _atomType)
                  || ((_atomType & supportedPropellantAtoms) != _atomType)
                  || ((_propType & AttachedReactor.SupportedPropellantTypes) != _propType)
                  || ((_propType & supportedPropellantTypes) != _propType));

            if (canLoadPropellant && HighLogic.LoadedSceneIsFlight)
            {
                foreach (Propellant curEnginePropellant in _listOfPropellants)
                {
                    var extendedPropellant = curEnginePropellant as ExtendedPropellant;

                    if (extendedPropellant == null)
                        continue;

                    var resourceDefinition =
                        PartResourceLibrary.Instance.GetDefinition(extendedPropellant.StoragePropellantName);
                    double maxAmount = 0;
                    if (resourceDefinition != null)
                        part.GetConnectedResourceTotals(resourceDefinition.id, extendedPropellant.GetFlowMode(), out _,
                            out maxAmount);

                    if (maxAmount != 0) continue;

                    if (notifySwitching)
                        missingResources += curEnginePropellant.name + " ";
                    canLoadPropellant = false;
                    break;
                }
            }

            if (AttachedEngine == null)
                return;

            //Get the Ignition state, i.e. is the engine shutdown or activated
            var engineState = AttachedEngine.getIgnitionState;

            // update the engine with the new propellants
            if (canLoadPropellant)
            {
                Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant chosen propellant " + fuel_mode + " / " +
                          fuelConfigNodes.Count());

                AttachedEngine.Shutdown();

                var newPropNode = new ConfigNode();

                foreach (var prop in _listOfPropellants)
                {
                    ResourceFlowMode flowMode = prop.GetFlowMode();
                    Debug.Log("[KSPI]: ThermalNozzleController set propellant name: " + prop.name + " ratio: " +
                              prop.ratio + " resourceFlowMode: " + flowMode);

                    var propellantConfigNode = newPropNode.AddNode("PROPELLANT");
                    propellantConfigNode.AddValue("name", prop.name);
                    propellantConfigNode.AddValue("ratio", prop.ratio);
                    propellantConfigNode.AddValue("DrawGauge", "true");

                    if (flowMode != ResourceFlowMode.NULL)
                        propellantConfigNode.AddValue("resourceFlowMode", flowMode.ToString());
                }

                AttachedEngine.Load(newPropNode);

                // update timewarp propellant
                if (timewarpEngine != null)
                {
                    if (_listOfPropellants.Count > 0)
                    {
                        timewarpEngine.propellant1 = _listOfPropellants[0].name;
                        timewarpEngine.ratio1 = _listOfPropellants[0].ratio;
                    }

                    if (_listOfPropellants.Count > 1)
                    {
                        timewarpEngine.propellant2 = _listOfPropellants[1].name;
                        timewarpEngine.ratio2 = _listOfPropellants[1].ratio;
                    }

                    if (_listOfPropellants.Count > 2)
                    {
                        timewarpEngine.propellant3 = _listOfPropellants[2].name;
                        timewarpEngine.ratio3 = _listOfPropellants[2].ratio;
                    }

                    if (_listOfPropellants.Count > 3)
                    {
                        timewarpEngine.propellant4 = _listOfPropellants[3].name;
                        timewarpEngine.ratio4 = _listOfPropellants[3].ratio;
                    }
                }
            }

            if (canLoadPropellant && engineState)
                AttachedEngine.Activate();

            if (HighLogic.LoadedSceneIsFlight)
            {
                // you can have any fuel you want in the editor but not in flight
                // should we switch to another propellant because we have none of this one?
                bool nextPropellant = !canLoadPropellant;

                // do the switch if needed
                if (nextPropellant && (_switches <= fuelConfigNodes.Length || fuel_mode != 0))
                {
                    // always shows the first fuel mode when all fuel mods are tested at least once
                    ++_switches;
                    if (notifySwitching)
                        ScreenMessages.PostScreenMessage(
                            Localizer.Format("#LOC_KSPIE_ThermalNozzleController_PostMsg1", missingResources), 5.0f,
                            ScreenMessageStyle.LOWER_CENTER); //"Switching Propellant, missing resource <<1>>

                    if (forward)
                        NextPropellantInternal();
                    else
                        PreviousPropellantInternal();
                }
            }
            else
            {
                bool nextPropellant = false;

                Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant " + _listOfPropellants[0].name);

                // Still ignore propellants that don't exist or we cannot use due to the limitations of the engine
                if (!canLoadPropellant && (_switches <= fuelConfigNodes.Length || fuel_mode != 0))
                {
                    //if (((_atomType & this.supportedPropellantAtoms) != _atomType))
                    //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant nozzle atom " + this.supportedPropellantAtoms + " != " + _atomType);
                    //if (((_propType & this.supportedPropellantTypes) != _propType))
                    //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant nozzle type " + this.supportedPropellantTypes + " != " + _propType);

                    //if (((_atomType & _myAttachedReactor.SupportedPropellantAtoms) != _atomType))
                    //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant reactor atom " + _myAttachedReactor.SupportedPropellantAtoms + " != " + _atomType);
                    //if (((_propType & _myAttachedReactor.SupportedPropellantTypes) != _propType))
                    //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant reactor type " + _myAttachedReactor.SupportedPropellantTypes + " != " + _propType);

                    nextPropellant = true;
                }

                if (nextPropellant)
                {
                    ++_switches;
                    if (forward)
                        NextPropellantInternal();
                    else
                        PreviousPropellantInternal();
                }

                EstimateEditorPerformance(); // update editor estimates
            }

            _switches = 0;
        }

        private void UpdatePropellantModeBehavior(ConfigNode chosenPropellant)
        {
            _fuelmode = chosenPropellant.GetValue("guiName");
            _propellantIsLFO = chosenPropellant.HasValue("isLFO") && bool.Parse(chosenPropellant.GetValue("isLFO"));
            _currentPropellantIsJet = chosenPropellant.HasValue("isJet") && bool.Parse(chosenPropellant.GetValue("isJet"));
            _propellantSootFactorFullThrotle = chosenPropellant.HasValue("maxSootFactor") ? float.Parse(chosenPropellant.GetValue("maxSootFactor")) : 0;
            _propellantSootFactorMinThrotle = chosenPropellant.HasValue("minSootFactor") ? float.Parse(chosenPropellant.GetValue("minSootFactor")) : 0;
            _propellantSootFactorEquilibrium = chosenPropellant.HasValue("levelSootFraction") ? float.Parse(chosenPropellant.GetValue("levelSootFraction")) : 0;
            _minDecompositionTemp = chosenPropellant.HasValue("MinDecompositionTemp") ? float.Parse(chosenPropellant.GetValue("MinDecompositionTemp")) : 0;
            _maxDecompositionTemp = chosenPropellant.HasValue("MaxDecompositionTemp") ? float.Parse(chosenPropellant.GetValue("MaxDecompositionTemp")) : 0;
            _decompositionEnergy = chosenPropellant.HasValue("DecompositionEnergy") ? float.Parse(chosenPropellant.GetValue("DecompositionEnergy")) : 0;
            _baseIspMultiplier = chosenPropellant.HasValue("BaseIspMultiplier") ? float.Parse(chosenPropellant.GetValue("BaseIspMultiplier")) : 0;
            _fuelTechRequirement = chosenPropellant.HasValue("TechRequirement") ? chosenPropellant.GetValue("TechRequirement") : string.Empty;
            _fuelCoolingFactor = chosenPropellant.HasValue("coolingFactor") ? float.Parse(chosenPropellant.GetValue("coolingFactor")) : 1;
            _fuelToxicity = chosenPropellant.HasValue("Toxicity") ? float.Parse(chosenPropellant.GetValue("Toxicity")) : 0;
            _fuelMinimumCoreTemp = chosenPropellant.HasValue("minimumCoreTemp") ? float.Parse(chosenPropellant.GetValue("minimumCoreTemp")) : 0;
            _fuelRequiresUpgrade = chosenPropellant.HasValue("RequiresUpgrade") && bool.Parse(chosenPropellant.GetValue("RequiresUpgrade"));
            _isNeutronAbsorber = chosenPropellant.HasValue("isNeutronAbsorber") && bool.Parse(chosenPropellant.GetValue("isNeutronAbsorber"));
            _atomType = chosenPropellant.HasValue("atomType") ? int.Parse(chosenPropellant.GetValue("atomType")) : 1;
            _propType = chosenPropellant.HasValue("propType") ? int.Parse(chosenPropellant.GetValue("propType")) : 1;

            if (!UsePlasmaPower && !usePropellantBaseIsp && !_currentPropellantIsJet && _decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                UpdateThrustPropellantMultiplier();
            else
            {
                _heatDecompositionFraction = 1;

                if ((usePropellantBaseIsp || AttachedReactor.UsePropellantBaseIsp || UsePlasmaPower) && _baseIspMultiplier > 0)
                    _ispPropellantMultiplier = _baseIspMultiplier;
                else
                    _ispPropellantMultiplier = chosenPropellant.HasValue("ispMultiplier") ? float.Parse(chosenPropellant.GetValue("ispMultiplier")) : 1;

                var rawTrustPropellantMultiplier = chosenPropellant.HasValue("thrustMultiplier") ? float.Parse(chosenPropellant.GetValue("thrustMultiplier")) : 1;
                _thrustPropellantMultiplier = _propellantIsLFO || _currentPropellantIsJet || rawTrustPropellantMultiplier <= 1 ? rawTrustPropellantMultiplier : ((rawTrustPropellantMultiplier + 1) / 2);
            }
        }

        private void UpdateThrustPropellantMultiplier()
        {
            if (AttachedEngine == null)
                return;

            coreTemperature = AttachedEngine.currentThrottle > 0 ? AttachedReactor.CoreTemperature : AttachedReactor.MaxCoreTemperature;
            var linearFraction = Math.Max(0, Math.Min(1, (coreTemperature - _minDecompositionTemp) / (_maxDecompositionTemp - _minDecompositionTemp)));
            _heatDecompositionFraction = Math.Pow(0.36, Math.Pow(3 - linearFraction * 3, 2) / 2);
            var rawTrustPropellantMultiplier = Math.Sqrt(_heatDecompositionFraction * _decompositionEnergy / HydroloxDecompositionEnergy) * 1.04 + 1;

            _ispPropellantMultiplier = _baseIspMultiplier * rawTrustPropellantMultiplier;
            _thrustPropellantMultiplier = _propellantIsLFO ? rawTrustPropellantMultiplier : (rawTrustPropellantMultiplier + 1) / 2;

            // lower efficiency of plasma nozzle when used with heavier propellants except when used with a neutron absorber like lithium
            if (UsePlasmaPower && !_isNeutronAbsorber)
            {
                var plasmaEfficiency = Math.Pow(_baseIspMultiplier, 1d/3d);
                _ispPropellantMultiplier *= plasmaEfficiency;
                _thrustPropellantMultiplier *= plasmaEfficiency;
            }
        }

        public void UpdateIspEngineParams(double atmosphereIspEfficiency = 1, double performanceBonus = 0)
        {
            // recalculate ISP based on power and core temp available
            _atmCurve = new FloatCurve();
            _atmosphereCurve = new FloatCurve();
            _velCurve = new FloatCurve();

            UpdateMaxIsp();

            if (!_currentPropellantIsJet)
            {
                effectiveIsp = (float)(_maxISP * atmosphereIspEfficiency);

                _atmosphereCurve.Add(0, effectiveIsp, 0, 0);

                var wasteheatModifier = wasteheatRatioDecelerationMult > 0
                    ? Math.Max((1 - GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt)) * wasteheatRatioDecelerationMult, 1)
                    : 1;

                if (AttachedReactor != null)
                {
                    finalEngineAccelerationSpeed = (float)Math.Min(engineAccelerationBaseSpeed * AttachedReactor.ReactorSpeedMult, 33);
                    finalEngineDecelerationSpeed = (float)Math.Min(engineDecelerationBaseSpeed * AttachedReactor.ReactorSpeedMult * Math.Max(0.25, wasteheatModifier), 33);
                    useEngineResponseTime = AttachedReactor.ReactorSpeedMult > 0;
                }

                AttachedEngine.useAtmCurve = false;
                AttachedEngine.useVelCurve = false;
                AttachedEngine.useEngineResponseTime = useEngineResponseTime;
                AttachedEngine.engineAccelerationSpeed = finalEngineAccelerationSpeed;
                AttachedEngine.engineDecelerationSpeed = finalEngineDecelerationSpeed;
                AttachedEngine.exhaustDamage = true;
                AttachedEngine.exhaustDamageMaxRange = (float)(_maxISP / 100);

                if (minThrottle > 0)
                {
                    var multiplier = 0.5f + AttachedEngine.currentThrottle;
                    AttachedEngine.engineAccelerationSpeed *= multiplier;
                    AttachedEngine.engineDecelerationSpeed *= multiplier;
                }
            }
            else
            {
                if (overrideVelocityCurve && jetPerformanceProfile == 0)    // Ramjet
                {
                    _velCurve.Add(0, _jetTechBonusPercentage * 0.01f + takeoffIntakeBonus);
                    _velCurve.Add(3 - _jetTechBonusCurveChange, 1);
                    _velCurve.Add(5 + _jetTechBonusCurveChange * 2, 1);
                    _velCurve.Add(14, 0 + _jetTechBonusPercentage);
                    _velCurve.Add(20, 0);
                }
                else if (overrideVelocityCurve && jetPerformanceProfile == 1)   // Turbojet
                {
                    _velCurve.Add(0.0f, 0.20f + _jetTechBonusPercentage * 2 + takeoffIntakeBonus);
                    _velCurve.Add(0.2f, 0.50f + _jetTechBonusPercentage);
                    _velCurve.Add(0.5f, 0.80f + _jetTechBonusPercentage);
                    _velCurve.Add(1.0f, 1.00f);
                    _velCurve.Add(2.0f, 0.80f + _jetTechBonusPercentage);
                    _velCurve.Add(3.0f, 0.60f + _jetTechBonusPercentage);
                    _velCurve.Add(4.0f, 0.40f + _jetTechBonusPercentage);
                    _velCurve.Add(5.0f, 0.20f + _jetTechBonusPercentage);
                    _velCurve.Add(7.0f, 0.00f);
                }
                else if (overrideVelocityCurve && jetPerformanceProfile == 2)   // Turbo ramjet
                {
                    _velCurve.Add(0.0f, 0.10f + _jetTechBonusPercentage + takeoffIntakeBonus);
                    _velCurve.Add(0.2f, 0.25f + _jetTechBonusPercentage);
                    _velCurve.Add(0.5f, 0.50f + _jetTechBonusPercentage);
                    _velCurve.Add(1.0f, 1.00f);
                    _velCurve.Add(10, 0 + _jetTechBonusPercentage);
                    _velCurve.Add(20, 0);
                }
                else
                    _velCurve = _originalVelocityCurve;

                if (overrideAtmosphereCurve )
                {
                    _atmosphereCurve.Add(0, Mathf.Min((float)_maxISP * 5f / 4f, maxThermalNozzleIsp));
                    _atmosphereCurve.Add(0.15f, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                    _atmosphereCurve.Add(0.3f, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                    _atmosphereCurve.Add(1, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                }
                else if (_originalAtmosphereCurve != null)
                    _atmosphereCurve = _originalAtmosphereCurve;
                else
                    _atmosphereCurve.Add(0, effectiveIsp);

                if (vessel != null)
                    effectiveIsp = _atmosphereCurve.Evaluate((float)vessel.atmDensity);

                if (overrideAtmCurve && jetPerformanceProfile == 0)
                {
                    _atmCurve.Add(0, 0);
                    _atmCurve.Add(0.01f, (float)Math.Min(1, 0.20 + 0.20 * performanceBonus));
                    _atmCurve.Add(0.04f, (float)Math.Min(1, 0.50 + 0.15 * performanceBonus));
                    _atmCurve.Add(0.16f, (float)Math.Min(1, 0.75 + 0.10 * performanceBonus));
                    _atmCurve.Add(0.64f, (float)Math.Min(1, 0.90 + 0.05 * performanceBonus));
                    _atmCurve.Add(1, 1);
                }
                else if (overrideAtmCurve)
                {
                    _atmCurve.Add(0, 0);
                    _atmCurve.Add(0.01f, (float)Math.Min(1, 0.10 + 0.10 * performanceBonus));
                    _atmCurve.Add(0.04f, (float)Math.Min(1, 0.25 + 0.10 * performanceBonus));
                    _atmCurve.Add(0.16f, (float)Math.Min(1, 0.50 + 0.10 * performanceBonus));
                    _atmCurve.Add(0.64f, (float)Math.Min(1, 0.80 + 0.10 * performanceBonus));
                    _atmCurve.Add(1, 1);
                }
                else
                    _atmCurve = _originalAtmCurve;

                AttachedEngine.atmCurve = _atmCurve;
                AttachedEngine.velCurve = _velCurve;
                AttachedEngine.engineAccelerationSpeed = effectiveJetengineAccelerationSpeed;
                AttachedEngine.engineDecelerationSpeed = effectiveJetengineDecelerationSpeed;

                AttachedEngine.useAtmCurve = true;
                AttachedEngine.useVelCurve = true;

                if (AttachedReactor != null)
                    useEngineResponseTime = AttachedReactor.ReactorSpeedMult > 0;

                AttachedEngine.useEngineResponseTime = useEngineResponseTime;
            }

            AttachedEngine.atmosphereCurve = _atmosphereCurve;
        }

        public double GetNozzleFlowRate()
        {
            return AttachedEngine.isOperational ? maxFuelFlowRate : 0;
        }

        public void EstimateEditorPerformance()
        {
            if (AttachedEngine == null)
                return;

            var atmosphereFloatCurve = new FloatCurve();
            if (AttachedEngine == null)
                return;

            if (AttachedReactor == null)
            {
                atmosphereFloatCurve.Add(0, (float)minimumThrust, 0, 0);
                AttachedEngine.atmosphereCurve = atmosphereFloatCurve;
            }

            UpdateMaxIsp();

            if (_maxISP <= 0)
                return;

            var baseMaxThrust = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumPower / _maxISP / PhysicsGlobals.GravitationalAcceleration * GetHeatExchangerThrustMultiplier();
            var maxThrustInSpace = baseMaxThrust;
            baseMaxThrust *= _thrustPropellantMultiplier;

            final_max_thrust_in_space = baseMaxThrust;

            maxFuelFlowOnEngine = (float)Math.Max(baseMaxThrust / (PhysicsGlobals.GravitationalAcceleration * _maxISP), 1e-10);
            AttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;

            maxThrustOnEngine = (float)Math.Max(baseMaxThrust, minimumThrust);
            AttachedEngine.maxThrust = maxThrustOnEngine;
            UpdateAtmosphericPressureThreshold();

            // update engine thrust/ISP for thermal nozzle
            if (!_currentPropellantIsJet)
            {
                var maxThrustInCurrentAtmosphere = Math.Max(maxThrustInSpace - pressureThreshold, minimumThrust);

                var thrustAtmosphereRatio = maxThrustInSpace > 0 ? Math.Max(maxThrustInCurrentAtmosphere / maxThrustInSpace, 0.01) : 0.01;
                _minISP = _maxISP * thrustAtmosphereRatio;
            }
            else
                _minISP = _maxISP;

            atmosphereFloatCurve.Add(0, (float)_maxISP, 0, 0);
            atmosphereFloatCurve.Add(1, (float)_minISP, 0, 0);

            AttachedEngine.atmosphereCurve = atmosphereFloatCurve;
        }

        public override void OnFixedUpdate()
        {
            if (canActivatePowerSource && AttachedReactor != null)
            {
                AttachedReactor.EnableIfPossible();
                canActivatePowerSource = false;
            }

            base.OnFixedUpdate();
        }

        public void FixedUpdate() // FixedUpdate is also called while not staged
        {
            if (!HighLogic.LoadedSceneIsFlight || AttachedEngine == null) return;

            double timeWarpFixedDeltaTime = TimeWarp.fixedDeltaTime;

            UpdateConfigEffects();

            if (AttachedEngine.currentThrottle > 0 && !exhaustAllowed)
            {
                string message = AttachedReactor.MayExhaustInLowSpaceHomeworld
                    ? Localizer.Format("#LOC_KSPIE_ThermalNozzleController_PostMsg2") //"Engine halted - Radioactive exhaust not allowed towards or inside homeworld atmosphere"
                    : Localizer.Format("#LOC_KSPIE_ThermalNozzleController_PostMsg3");//"Engine halted - Radioactive exhaust not allowed towards or near homeworld atmosphere"

                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                vessel.ctrlState.mainThrottle = 0;

                // Return to realtime
                if (vessel.packed)
                    TimeWarp.SetRate(0, true);
            }

            requestedThrottle = AttachedEngine.requestedThrottle;

            previousThrottle = currentThrottle;
            currentThrottle = AttachedEngine.currentThrottle;

            if (minThrottle > 0 && requestedThrottle > 0 && AttachedReactor.ReactorSpeedMult > 0)
            {
                previousDelayedThrottle = delayedThrottle;
                delayedThrottle = Math.Min(delayedThrottle + timeWarpFixedDeltaTime * AttachedEngine.engineAccelerationSpeed, minThrottle);
            }
            else if (minThrottle > 0 && requestedThrottle == 0 && AttachedReactor.ReactorSpeedMult > 0)
            {
                delayedThrottle = Math.Max(delayedThrottle - timeWarpFixedDeltaTime * AttachedEngine.engineAccelerationSpeed, 0);
                previousDelayedThrottle = adjustedThrottle;
            }
            else
            {
                previousDelayedThrottle = previousThrottle;
                delayedThrottle = minThrottle;
            }

            adjustedThrottle = currentThrottle >= 0.01
                ? delayedThrottle + (1 - delayedThrottle) * currentThrottle
                : Math.Max(currentThrottle, currentThrottle * 100 * delayedThrottle);

            if (minThrottle > 0)
                adjustedFuelFlowMult = previousThrottle > 0 ? Math.Min(100, (1 / Math.Max(currentThrottle, previousThrottle)) * Math.Pow(previousDelayedThrottle, adjustedFuelFlowExponent)) : 0;
            else
                adjustedFuelFlowMult = 1;

            if (AttachedReactor == null)
            {
                if (AttachedEngine.isOperational && currentThrottle > 0)
                {
                    AttachedEngine.Shutdown();
                    Debug.Log("[KSPI]: Engine Shutdown: No reactor attached!");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ThermalNozzleController_PostMsg4"), 5.0f, ScreenMessageStyle.UPPER_CENTER);//"Engine Shutdown: No reactor attached!"
                }
                AttachedEngine.CLAMP = 0;
                AttachedEngine.flameoutBar = float.MaxValue;
                // Other engines on the vessel should be able to run normally if set to independent throttle
                if (!AttachedEngine.independentThrottle)
                    vessel.ctrlState.mainThrottle = 0;
                maxFuelFlowOnEngine = 1e-10f;
                AttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;
                return;
            }

            if (_myAttachedReactor.Part != part)
            {
                resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
                resourceBuffers.UpdateBuffers();
            }

            // attach/detach with radius
            if (AttachedEngine.isOperational)
                AttachedReactor.AttachThermalReciever(id, radius);
            else
                AttachedReactor.DetachThermalReciever(id);

            bool canUseChargedPower = allowUseOfChargedPower && AttachedReactor.ChargedPowerRatio > 0;

            effectiveThrustFraction = GetHeatExchangerThrustMultiplier();

            effectiveThermalSupply = !UseChargedPowerOnly ? effectiveThrustFraction * GetAvailableStableSupply(ResourceSettings.Config.ThermalPowerInMegawatt) : 0;
            effectiveChargedSupply = canUseChargedPower ? effectiveThrustFraction * GetAvailableStableSupply(ResourceSettings.Config.ChargedPowerInMegawatt) : 0;

            maximumPowerUsageForPropulsionRatio = UsePlasmaPower
                ? AttachedReactor.PlasmaPropulsionEfficiency
                : AttachedReactor.ThermalPropulsionEfficiency;

            maximumThermalPower = AttachedReactor.MaximumThermalPower;
            maximumChargedPower = AttachedReactor.MaximumChargedPower;

            currentMaxThermalPower = Math.Min(effectiveThermalSupply, effectiveThrustFraction * maximumThermalPower * maximumPowerUsageForPropulsionRatio * adjustedThrottle);
            currentMaxChargedPower = Math.Min(effectiveChargedSupply, effectiveThrustFraction * maximumChargedPower * maximumPowerUsageForPropulsionRatio * adjustedThrottle);

            thermalResourceRatio = GetResourceBarFraction(ResourceSettings.Config.ThermalPowerInMegawatt);
            chargedResourceRatio = GetResourceBarFraction(ResourceSettings.Config.ChargedPowerInMegawatt);

            availableThermalPower = exhaustAllowed ? currentMaxThermalPower * (thermalResourceRatio > 0.5 ? 1 : thermalResourceRatio * 2) : 0;
            availableChargedPower = exhaustAllowed ? currentMaxChargedPower * (chargedResourceRatio > 0.5 ? 1 : chargedResourceRatio * 2) : 0;

            UpdateAnimation();

            isOpenCycleCooler = !isPlasmaNozzle && UseThermalAndChargedPower && !CheatOptions.IgnoreMaxTemperature;

            // when in jet mode apply extra cooling from intake air
            if (isOpenCycleCooler && isJet && part.atmDensity > 0)
            {
                var wasteheatRatio = GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);
                airFlowForCooling = maxFuelFlowRate * part.GetResourceRatio(ResourceSettings.Config.IntakeOxygenAir);
                ConsumeFnResourcePerSecond(40 * wasteheatRatio * wasteheatRatio * airFlowForCooling, ResourceSettings.Config.WasteHeatInMegawatt);
            }

            // flame-out when reactor cannot produce power
            AttachedEngine.flameoutBar = AttachedReactor.CanProducePower ? 0 : float.MaxValue;

            if (AttachedEngine.getIgnitionState && AttachedEngine.currentThrottle > 0)
                GenerateThrustFromReactorHeat();
            else
            {
                _engineWasInactivePreviousFrame = true;

                UpdateIspEngineParams();

                expectedMaxThrust = _maxISP <= 0.0 ? 0.0 : (AttachedReactor.MaximumPower * maximumPowerUsageForPropulsionRatio *
                    GetPowerThrustModifier() * GetHeatThrustModifier() * GetHeatExchangerThrustMultiplier() / (PhysicsGlobals.GravitationalAcceleration * _maxISP));
                calculatedMaxThrust = expectedMaxThrust;

                expectedMaxThrust *= _thrustPropellantMultiplier * (CheatOptions.UnbreakableJoints ? 1 : 1 - sootAccumulationPercentage / 200);

                maxFuelFlowRate = _maxISP <= 0.0 ? 0.0 : expectedMaxThrust / (_maxISP * PhysicsGlobals.GravitationalAcceleration);

                UpdateAtmosphericPressureThreshold();

                var thrustAtmosphereRatio = expectedMaxThrust <= 0 ? 0 : Math.Max(0, expectedMaxThrust - pressureThreshold) / expectedMaxThrust;

                current_isp = _maxISP * thrustAtmosphereRatio;

                calculatedMaxThrust = Math.Max((calculatedMaxThrust - pressureThreshold), minimumThrust);

                var sootModifier = CheatOptions.UnbreakableJoints ? 1 : sootHeatDivider > 0 ? 1 - (sootAccumulationPercentage / sootThrustDivider) : 1;

                calculatedMaxThrust *= _thrustPropellantMultiplier * sootModifier;

                effectiveIsp = isJet ? (float)Math.Min(current_isp, maxThermalNozzleIsp) : (float)current_isp;

                var newIsp = new FloatCurve();
                newIsp.Add(0, effectiveIsp, 0, 0);
                AttachedEngine.atmosphereCurve = newIsp;

                if (AttachedEngine.useVelCurve && AttachedEngine.velCurve != null)
                {
                    vcurveAtCurrentVelocity = AttachedEngine.velCurve.Evaluate((float)(vessel.speed / vessel.speedOfSound));

                    if (IsInvalidNumber(vcurveAtCurrentVelocity))
                        vcurveAtCurrentVelocity = 0;

                    calculatedMaxThrust *= vcurveAtCurrentVelocity;
                }
                else
                    vcurveAtCurrentVelocity = 1;

                if (AttachedEngine.useAtmCurve && AttachedEngine.atmCurve != null)
                {
                    atmosphereModifier = AttachedEngine.atmCurve.Evaluate((float)vessel.atmDensity);

                    if (IsInvalidNumber(atmosphereModifier))
                        atmosphereModifier = 0;

                    calculatedMaxThrust *= atmosphereModifier;
                }
                else
                    atmosphereModifier = 1;

                UpdateJetSpoolSpeed();

                if (_currentPropellantIsJet)
                {
                    if (IsInvalidNumber(jetSpoolRatio))
                        jetSpoolRatio = 0;

                    calculatedMaxThrust *= jetSpoolRatio;
                    maxFuelFlowRate *= jetSpoolRatio;
                }

                // prevent too low number of max thrust
                if (calculatedMaxThrust <= minimumThrust)
                {
                    calculatedMaxThrust = minimumThrust;
                    maxFuelFlowRate = 0;
                }

                // set engines maximum fuel flow
                if (IsPositiveValidNumber(maxFuelFlowRate) && IsPositiveValidNumber(AttachedReactor.FuelRatio))
                    maxFuelFlowOnEngine = (float)Math.Max(maxFuelFlowRate * AttachedReactor.FuelRatio * AttachedReactor.FuelRatio, 1e-10);
                else
                    maxFuelFlowOnEngine = 1e-10f;

                AttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;

                // set heat production to 0 to prevent heat spike at activation
                AttachedEngine.heatProduction = 0;

                if (pulseDuration == 0 && AttachedEngine is ModuleEnginesFX)
                {
                    powerEffectRatio = 0;
                    runningEffectRatio = 0;

                    if (!string.IsNullOrEmpty(_powerEffectNameParticleFx))
                        part.Effect(_powerEffectNameParticleFx, powerEffectRatio);
                    if (!string.IsNullOrEmpty(_runningEffectNameParticleFx))
                        part.Effect(_runningEffectNameParticleFx, runningEffectRatio);
                }

                UpdateThrottleAnimation(0);
            }

            if (!string.IsNullOrEmpty(EffectNameSpool))
            {
                spoolEffectRatio = jetSpoolRatio * vcurveAtCurrentVelocity * atmosphereModifier;
                part.Effect(EffectNameSpool, spoolEffectRatio);
            }

            if (AttachedEngine.getIgnitionState && AttachedEngine.status == _flameoutText)
            {
                AttachedEngine.maxFuelFlow = 1e-10f;
            }
        }

        private void UpdateJetSpoolSpeed()
        {
            if (AttachedEngine != null &&  AttachedEngine.getIgnitionState && AttachedEngine.useVelCurve && AttachedEngine.velCurve != null)
                jetSpoolRatio += Math.Min(TimeWarp.fixedDeltaTime * 0.1f, 1 - jetSpoolRatio);
            else
                jetSpoolRatio -= Math.Min(TimeWarp.fixedDeltaTime * 0.1f, jetSpoolRatio );
        }

        private void UpdateAtmosphericPressureThreshold()
        {
            if (!_currentPropellantIsJet)
            {
                var staticPressure = HighLogic.LoadedSceneIsFlight
                    ? FlightGlobals.getStaticPressure(vessel.transform.position)
                    : GameConstants.EarthAtmospherePressureAtSeaLevel;

                pressureThreshold = scaledExitArea * staticPressure;
            }
            else
                pressureThreshold = 0;
        }

        private void UpdateAnimation()
        {
            if (AttachedEngine == null)
                return;

            float increase;

            if (AttachedEngine.currentThrottle > 0 && expectedMaxThrust > 0)
                increase = 0.02f;
            else if (_currentAnimationRatio > 1 / recoveryAnimationDivider)
                increase = 0.02f;
            else if (_currentAnimationRatio > 0)
                increase = 0.02f / -recoveryAnimationDivider;
            else
                increase = 0;

            _currentAnimationRatio += increase;


            if (pulseDuration > 0 && AttachedEngine is ModuleEnginesFX)
            {
                if (!string.IsNullOrEmpty(_powerEffectNameParticleFx))
                {
                    powerEffectRatio = increase > 0 && expectedMaxThrust > 0 && AttachedEngine.currentThrottle > 0 &&
                                       _currentAnimationRatio < pulseDuration
                        ? 1 - _currentAnimationRatio / pulseDuration
                        : 0;

                    part.Effect(_powerEffectNameParticleFx, powerEffectRatio);
                }

                if (!string.IsNullOrEmpty(_runningEffectNameParticleFx))
                {
                    runningEffectRatio = increase > 0 && expectedMaxThrust > 0 &&
                                         AttachedEngine.currentThrottle > 0 && _currentAnimationRatio < pulseDuration
                        ? 1 - _currentAnimationRatio / pulseDuration
                        : 0;

                    part.Effect(_runningEffectNameParticleFx, runningEffectRatio);
                }

            }

            if (pulseDuration > 0 && expectedMaxThrust > 0 && increase > 0 && AttachedEngine.currentThrottle > 0 &&
                _currentAnimationRatio < pulseDuration)
                PluginHelper.SetAnimationRatio(1, emiAnimationState);
            else
                PluginHelper.SetAnimationRatio(0, emiAnimationState);

            if (_currentAnimationRatio > 1 + (2 - (AttachedEngine.currentThrottle * 2)))
                _currentAnimationRatio = 0;

            PluginHelper.SetAnimationRatio(Math.Max(Math.Min(_currentAnimationRatio, 1), 0), pulseAnimationState);
        }

        private double CalculateElectricalPowerCurrentlyNeeded(double maximumElectricPower)
        {
            var currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));
            var spareResourceCapacity = GetSpareResourceCapacity(ResourceSettings.Config.ElectricPowerInMegawatt);
            var powerRequestRatio = mhdPowerGenerationPercentage * 0.01;
            return Math.Min(maximumElectricPower, currentUnfilledResourceDemand * Math.Min(1, powerRequestRatio) + spareResourceCapacity * Math.Max(0, powerRequestRatio - 1));
        }

        private void GenerateThrustFromReactorHeat()
        {
            // shutdown engine when connected heatSource cannot produce power
            if (!AttachedReactor.CanProducePower)
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ThermalNozzleController_PostMsg5"), 0.02f, ScreenMessageStyle.UPPER_CENTER);//"no power produced by thermal source!"

            // consume power when plasma nozzle
            if (requiredMegajouleRatio > 0)
            {
                var availablePower = availableThermalPower + availableChargedPower;
                var requestedElectricPowerMegajoules = availablePower * requiredMegajouleRatio * AttachedReactor.MagneticNozzlePowerMult;
                var receivedElectricMegajoules = ConsumeFnResourcePerSecond(requestedElectricPowerMegajoules, ResourceSettings.Config.ElectricPowerInMegawatt);
                SupplyFnResourcePerSecond(receivedElectricMegajoules, ResourceSettings.Config.WasteHeatInMegawatt);
                requiredElectricalPowerFromMhd = CalculateElectricalPowerCurrentlyNeeded(Math.Min(availablePower, availablePower * requiredMegajouleRatio * AttachedReactor.MagneticNozzleMhdMult));
                var electricalPowerCurrentlyNeedRatio = requestedElectricPowerMegajoules > 0 ? Math.Min(1, requiredElectricalPowerFromMhd / requestedElectricPowerMegajoules) : 0;
                receivedMegajoulesRatio = Math.Min(1, Math.Max(electricalPowerCurrentlyNeedRatio,  requestedElectricPowerMegajoules > 0 ? receivedElectricMegajoules / requestedElectricPowerMegajoules : 0));
                received_megajoules_percentage = receivedMegajoulesRatio * 100;
            }
            else
                receivedMegajoulesRatio = 1;

            requested_thermal_power = receivedMegajoulesRatio * availableThermalPower;

            reactor_power_received = ConsumeFnResourcePerSecond(requested_thermal_power, ResourceSettings.Config.ThermalPowerInMegawatt);

            if (currentMaxChargedPower > 0)
            {
                requested_charge_particles = receivedMegajoulesRatio * availableChargedPower;
                reactor_power_received += ConsumeFnResourcePerSecond(requested_charge_particles, ResourceSettings.Config.ChargedPowerInMegawatt);
            }

            // convert part of the exhaust energy directly into electric power
            if (requiredMegajouleRatio > 0 && requiredElectricalPowerFromMhd > 0 && reactor_power_received > 0)
            {
                SupplyFnResourcePerSecond(requiredElectricalPowerFromMhd, ResourceSettings.Config.ElectricPowerInMegawatt);
                requiredMhdEnergyRatio = requiredElectricalPowerFromMhd / reactor_power_received;
            }
            else
                requiredMhdEnergyRatio = 0;

            _mhdTrustIspModifier = 1 - requiredMhdEnergyRatio;

            GetMaximumIspAndThrustMultiplier();
            UpdateSootAccumulation();

            // consume wasteheat
            if (!CheatOptions.IgnoreMaxTemperature)
            {
                var sootModifier = CheatOptions.UnbreakableJoints || sootHeatDivider <= 0 ? 1 : 1 - sootAccumulationPercentage / sootHeatDivider;
                ConsumeFnResourcePerSecond(sootModifier * (1 - ReactorWasteheatModifier) * reactor_power_received, ResourceSettings.Config.WasteHeatInMegawatt);
            }

            if (reactor_power_received > 0 && _maxISP > 0)
            {
                if (_engineWasInactivePreviousFrame)
                {
                    current_isp = _maxISP * 0.01;
                    _engineWasInactivePreviousFrame = false;
                }

                var ispRatio = _currentPropellantIsJet ? current_isp / _maxISP : 1;

                var powerHeatModifier = receivedMegajoulesRatio * GetPowerThrustModifier() * GetHeatThrustModifier();

                engineMaxThrust = powerHeatModifier * _mhdTrustIspModifier * reactor_power_received / _maxISP / PhysicsGlobals.GravitationalAcceleration;

                thrustPerMegaJoule = powerHeatModifier * maximumPowerUsageForPropulsionRatio / _maxISP / PhysicsGlobals.GravitationalAcceleration * ispRatio;

                expectedMaxThrust = thrustPerMegaJoule * AttachedReactor.MaximumPower * effectiveThrustFraction;

                final_max_thrust_in_space = Math.Max(thrustPerMegaJoule * AttachedReactor.RawMaximumPower * effectiveThrustFraction, minimumThrust);

                if (AttachedEngine != null)
                    AttachedEngine.maxThrust = (float)final_max_thrust_in_space;

                calculatedMaxThrust = expectedMaxThrust;
            }
            else
            {
                calculatedMaxThrust = 0;
                expectedMaxThrust = 0;
            }

            max_thrust_in_space = engineMaxThrust;

            max_thrust_in_current_atmosphere = max_thrust_in_space;

            UpdateAtmosphericPressureThreshold();

            // update engine thrust/ISP for thermal nozzle
            if (!_currentPropellantIsJet)
            {
                max_thrust_in_current_atmosphere = Math.Max(max_thrust_in_space - pressureThreshold, 1e-10);

                var atmosphereThrustEfficiency = max_thrust_in_space > 0 ? Math.Min(1, max_thrust_in_current_atmosphere / max_thrust_in_space) : 0;

                var thrustAtmosphereRatio = max_thrust_in_space > 0 ? Math.Max(atmosphereThrustEfficiency, 0.01) : 0.01;

                UpdateIspEngineParams(thrustAtmosphereRatio, 1 - missingPrecoolerRatio);
                current_isp = _maxISP * thrustAtmosphereRatio;
                calculatedMaxThrust *= atmosphereThrustEfficiency;
            }
            else
                current_isp = _maxISP;

            if (!double.IsInfinity(max_thrust_in_current_atmosphere) && !double.IsNaN(max_thrust_in_current_atmosphere))
            {
                var sootModifier = _thrustPropellantMultiplier * (CheatOptions.UnbreakableJoints ? 1 : 1 - sootAccumulationPercentage / sootThrustDivider);
                final_max_engine_thrust = max_thrust_in_current_atmosphere * sootModifier;
                calculatedMaxThrust *= sootModifier;
            }
            else
            {
                final_max_engine_thrust = 1e-10;
                calculatedMaxThrust = final_max_engine_thrust;
            }

            // amount of fuel being used at max throttle with no atmospheric limits
            if (_maxISP <= 0) return;

            var maxThrustForFuelFlow = final_max_engine_thrust > 0.0001 ? calculatedMaxThrust : final_max_engine_thrust;

            // calculate maximum fuel flow rate
            maxFuelFlowRate = maxThrustForFuelFlow / current_isp / PhysicsGlobals.GravitationalAcceleration;

            fuelFlowThrottleModifier = 1;

            if (AttachedEngine != null && AttachedEngine.useVelCurve && AttachedEngine.velCurve != null)
            {
                vcurveAtCurrentVelocity = AttachedEngine.velCurve.Evaluate((float)(vessel.speed / vessel.speedOfSound));

                if (IsInvalidNumber(vcurveAtCurrentVelocity))
                    vcurveAtCurrentVelocity = 0;

                calculatedMaxThrust *= vcurveAtCurrentVelocity;
                fuelFlowThrottleModifier *= vcurveAtCurrentVelocity;
            }
            else
                vcurveAtCurrentVelocity = 1;

            if (AttachedEngine != null && AttachedEngine.useAtmCurve && AttachedEngine.atmCurve != null)
            {
                atmosphereModifier = AttachedEngine.atmCurve.Evaluate((float)vessel.atmDensity);

                if (IsInvalidNumber(atmosphereModifier))
                    atmosphereModifier = 0;

                calculatedMaxThrust *= atmosphereModifier;
                fuelFlowThrottleModifier *= atmosphereModifier;
            }
            else
                atmosphereModifier = 1;

            UpdateJetSpoolSpeed();

            if (_currentPropellantIsJet)
            {
                if (IsInvalidNumber(jetSpoolRatio))
                    jetSpoolRatio = 0;

                calculatedMaxThrust *= jetSpoolRatio;
                maxFuelFlowRate *= jetSpoolRatio;
            }

            if (calculatedMaxThrust <= minimumThrust || double.IsNaN(calculatedMaxThrust) || double.IsInfinity(calculatedMaxThrust))
            {
                calculatedMaxThrust = minimumThrust;
                maxFuelFlowRate = 1e-10;
            }

            // set engines maximum fuel flow
            if (IsPositiveValidNumber(maxFuelFlowRate) && IsPositiveValidNumber(adjustedFuelFlowMult) && IsPositiveValidNumber(AttachedReactor.FuelRatio))
                maxFuelFlowOnEngine = (float)Math.Max(maxFuelFlowRate * adjustedFuelFlowMult * AttachedReactor.FuelRatio * AttachedReactor.FuelRatio, 1e-10);
            else
                maxFuelFlowOnEngine = 1e-10f;

            CalculateMissingPreCoolerRatio();

            airflowHeatModifier = missingPrecoolerRatio > 0 ? Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20 / GameConstants.atmospheric_non_precooled_limit * missingPrecoolerRatio), 0): 0;
            airflowHeatModifier *= vessel.atmDensity * (vessel.speed / vessel.speedOfSound);

            if (airflowHeatModifier.IsInfinityOrNaN())
                airflowHeatModifier = 0;

            if (AttachedEngine != null)
            {
                AttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;
                maxThrustOnEngine = AttachedEngine.maxThrust;
                realIspEngine = AttachedEngine.realIsp;
                currentMassFlow = AttachedEngine.fuelFlowGui * AttachedEngine.mixtureDensity;
            }

            // act as open cycle cooler
            if (isOpenCycleCooler)
            {
                var wasteheatRatio = GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);
                fuelFlowForCooling = currentMassFlow;
                ConsumeFnResourcePerSecond(_fuelCoolingFactor * wasteheatRatio * fuelFlowForCooling, ResourceSettings.Config.WasteHeatInMegawatt);
            }

            // give back propellant
            if (UseChargedPowerOnly && _listOfPropellants.Count == 1)
            {
                var resource = PartResourceLibrary.Instance.GetDefinition(_listOfPropellants.First().name);
                AttachedReactor.UseProductForPropulsion(1, currentMassFlow, new []{resource});
            }

            if (controlHeatProduction)
            {
                ispHeatModifier =  Math.Sqrt(realIspEngine) * (UsePlasmaPower ? plasmaAfterburnerHeatModifier : thermalHeatModifier);
                powerToMass = part.mass > 0 ? Math.Sqrt(maxThrustOnEngine / part.mass) : 0;
                radiusHeatModifier = Math.Pow(radius * radiusHeatProductionMult, radiusHeatProductionExponent);
                engineHeatProductionMult = AttachedReactor.EngineHeatProductionMult;
                var reactorHeatModifier = isPlasmaNozzle ? AttachedReactor.PlasmaHeatProductionMult : AttachedReactor.EngineHeatProductionMult;
                var jetHeatProduction = baseJetHeatproduction > 0 ? baseJetHeatproduction : spaceHeatProduction;

                spaceHeatProduction = heatProductionMultiplier * reactorHeatModifier * AttachedReactor.EngineHeatProductionMult * _ispPropellantMultiplier * ispHeatModifier * radiusHeatModifier * powerToMass / _fuelCoolingFactor;
                engineHeatProduction = _currentPropellantIsJet
                    ? jetHeatProduction * (1 + airflowHeatModifier * PluginSettings.Config.AirflowHeatMult)
                    : spaceHeatProduction;

                if (AttachedEngine != null)
                    AttachedEngine.heatProduction = (float)(engineHeatProduction * Math.Max(0, startupHeatReductionRatio));

                startupHeatReductionRatio = Math.Min(1, startupHeatReductionRatio + currentThrottle);
            }

            if (pulseDuration != 0 || AttachedEngine == null || !(AttachedEngine is ModuleEnginesFX)) return;

            maxEngineFuelFlow = AttachedEngine.maxThrust > minimumThrust ? maxThrustOnEngine / realIspEngine / PhysicsGlobals.GravitationalAcceleration : 0;
            fuelEffectRatio = currentMassFlow / maxEngineFuelFlow;

            if (!string.IsNullOrEmpty(_powerEffectNameParticleFx))
            {
                powerEffectRatio = maxEngineFuelFlow > 0 ? (float)(exhaustModifier * Math.Min(AttachedEngine.currentThrottle, fuelEffectRatio)) : 0;

                if (_showIspThrottle)
                    powerEffectRatio *= 0.5f + (1 - ispThrottle) * 0.005f;

                part.Effect(_powerEffectNameParticleFx, powerEffectRatio);
            }

            if (!string.IsNullOrEmpty(_runningEffectNameParticleFx))
            {
                runningEffectRatio = maxEngineFuelFlow > 0 ? (float)(exhaustModifier * Math.Min(AttachedEngine.requestedThrottle, fuelEffectRatio)) : 0;

                if (_showIspThrottle)
                    powerEffectRatio *= 0.5f + (1 - ispThrottle) * 0.005f;

                part.Effect(_runningEffectNameParticleFx, powerEffectRatio);
            }

            UpdateThrottleAnimation(Math.Max(powerEffectRatio, runningEffectRatio));
        }

        private void UpdateThrottleAnimation(float ratio)
        {
            if (string.IsNullOrEmpty(throttleAnimName))
                return;

            throttleAnimation[throttleAnimName].speed = 0;
            throttleAnimation[throttleAnimName].normalizedTime = Mathf.Pow(ratio, throttleAnimExp);
            throttleAnimation.Blend(throttleAnimName);
        }

        private void CalculateMissingPreCoolerRatio()
        {
            var preCoolerArea = _vesselPreCoolers.Where(prc => prc.functional).Sum(prc => prc.area);
            var intakesOpenArea = _vesselResourceIntakes.Where(mre => mre.intakeOpen).Sum(mre => mre.area);

            missingPrecoolerRatio = _currentPropellantIsJet && intakesOpenArea > 0
                ? Math.Min(1,
                    Math.Max(0, Math.Pow((intakesOpenArea - preCoolerArea)/intakesOpenArea, missingPrecoolerProportionExponent)))
                : 0;

            if (missingPrecoolerRatio.IsInfinityOrNaN())
                missingPrecoolerRatio = 0;
        }

        private static bool IsInvalidNumber(double variable)
        {
            return double.IsNaN(variable) || double.IsInfinity(variable);
        }

        private static bool IsPositiveValidNumber(double variable)
        {
            return !double.IsNaN(variable) && !double.IsInfinity(variable) && variable > 0;
        }

        private void UpdateSootAccumulation()
        {
            if (!CheatOptions.UnbreakableJoints)
                return;

            if (AttachedEngine == null)
                return;

            if (AttachedEngine.currentThrottle > 0 && _propellantSootFactorFullThrotle != 0 || _propellantSootFactorMinThrotle != 0)
            {
                double sootEffect;

                if (_propellantSootFactorEquilibrium != 0)
                {
                    var ratio = AttachedEngine.currentThrottle > _propellantSootFactorEquilibrium
                        ? (AttachedEngine.currentThrottle - _propellantSootFactorEquilibrium) / (1 - _propellantSootFactorEquilibrium)
                        : 1 - (AttachedEngine.currentThrottle / _propellantSootFactorEquilibrium);

                    var sootMultiplier = AttachedEngine.currentThrottle < _propellantSootFactorEquilibrium ? 1
                        : _propellantSootFactorFullThrotle > 0 ? _heatDecompositionFraction : 1;

                    sootEffect = AttachedEngine.currentThrottle > _propellantSootFactorEquilibrium
                        ? _propellantSootFactorFullThrotle * ratio * sootMultiplier
                        : _propellantSootFactorMinThrotle * ratio * sootMultiplier;
                }
                else
                {
                    var sootMultiplier = _heatDecompositionFraction > 0 ? _heatDecompositionFraction : 1;
                    sootEffect = _propellantSootFactorFullThrotle * sootMultiplier;
                }

                sootAccumulationPercentage = Math.Min(100, Math.Max(0, sootAccumulationPercentage + (TimeWarp.fixedDeltaTime * sootEffect)));
            }
            else
            {
                sootAccumulationPercentage -= TimeWarp.fixedDeltaTime * AttachedEngine.currentThrottle * 0.1;
                sootAccumulationPercentage = Math.Max(0, sootAccumulationPercentage);
            }
        }

        private void GetMaximumIspAndThrustMultiplier()
        {
            // get the flameout safety limit
            if (_currentPropellantIsJet)
            {
                UpdateIspEngineParams();
                current_isp = AttachedEngine.atmosphereCurve.Evaluate((float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position), 1.0));
            }
            else
            {
                if (_decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                    UpdateThrustPropellantMultiplier();
                else
                    _heatDecompositionFraction = 1;

                UpdateMaxIsp();
            }
        }

        private void UpdateMaxIsp()
        {
            if (AttachedReactor == null)
                return;

            coreTemperature = AttachedEngine == null || AttachedEngine.currentThrottle > 0 ? AttachedReactor.CoreTemperature : AttachedReactor.MaxCoreTemperature;

            baseMaxIsp = Math.Sqrt(coreTemperature) * EffectiveCoreTempIspMult;

            if (IsPositiveValidNumber(AttachedReactor.FuelRatio))
                baseMaxIsp *= AttachedReactor.FuelRatio;

            if (baseMaxIsp > maxJetModeBaseIsp && _currentPropellantIsJet)
                baseMaxIsp = maxJetModeBaseIsp;
            else if (baseMaxIsp > maxLfoModeBaseIsp && _propellantIsLFO)
                baseMaxIsp = maxLfoModeBaseIsp;
            else if (baseMaxIsp > maxThermalNozzleIsp && !isPlasmaNozzle)
                baseMaxIsp = maxThermalNozzleIsp;

            fuelflowThrottleMaxValue = minimumBaseIsp > 0 ? 100 * Math.Max(1, baseMaxIsp / Math.Min(baseMaxIsp, minimumBaseIsp)) : 100;

            if (_fuelFlowThrottleField != null)
            {
                _fuelFlowThrottleField.guiActiveEditor = minimumBaseIsp > 0;
                _fuelFlowThrottleField.guiActive = minimumBaseIsp > 0;
            }

            if (UseThermalPowerOnly)
            {
                _maxISP = isPlasmaNozzle
                    ? baseMaxIsp + AttachedReactor.ChargedPowerRatio * baseMaxIsp
                    : baseMaxIsp;
            }
            else if (UseChargedPowerOnly)
            {
                var joulesPerAmu = AttachedReactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / GameConstants.dilution_factor;
                _maxISP = 100 * Math.Sqrt(joulesPerAmu * 2 / GameConstants.ATOMIC_MASS_UNIT) / PhysicsGlobals.GravitationalAcceleration;
            }
            else
            {
                var scaledChargedRatio =  Math.Pow(AttachedReactor.ChargedPowerRatio * AttachedReactor.NormalizedPowerMultiplier, 0.5);

                if (UsePlasmaAfterBurner)
                {
                    _maxISP = scaledChargedRatio * baseMaxIsp;
                    _maxISP += Math.Pow(ispThrottle / 100d, 2) * scaledChargedRatio * AttachedReactor.PlasmaAfterburnerRange * baseMaxIsp;
                }
                else
                    _maxISP = scaledChargedRatio * baseMaxIsp + (1 - scaledChargedRatio) * maxThermalNozzleIsp;
            }

            effectiveFuelflowThrottle = Math.Min(fuelflowThrottleMaxValue, fuelFlowThrottle);

            fuelflowMultplier = Math.Min(Math.Max(100, fuelflowThrottleMaxValue * _ispPropellantMultiplier), effectiveFuelflowThrottle) / 100;

            var fuelFlowDivider = fuelflowMultplier > 0 ? 1 / fuelflowMultplier : 0;

            ispFlowMultiplier = _ispPropellantMultiplier * fuelFlowDivider;

            _maxISP *= ispFlowMultiplier * _mhdTrustIspModifier;

            exhaustModifier = Math.Pow(Math.Min(1, (effectiveFuelflowThrottle / _ispPropellantMultiplier) / fuelflowThrottleMaxValue), 0.5);
        }

        public override string GetInfo()
        {
            var upgraded = this.HasTechsRequiredToUpgrade();

            var propNodes = upgraded && isJet ? GetPropellantsHybrid() : GetPropellants(isJet);

            var returnStr = StringBuilderCache.Acquire();
            returnStr.AppendLine(Localizer.Format("#LOC_KSPIE_ThermalNozzleController_Info1"));//Thrust: Variable
            foreach (var propellantNode in propNodes)
            {
                var ispMultiplier = float.Parse(propellantNode.GetValue("ispMultiplier"));
                var guiName = propellantNode.GetValue("guiName");
                returnStr.Append("<color=#ffdd00ff>");
                returnStr.Append(guiName);
                returnStr.Append("</color>\n<size=10>ISP: ");
                returnStr.Append((ispMultiplier * EffectiveCoreTempIspMult).ToString("0.000"));
                returnStr.AppendLine(" x Sqrt(Core Temperature)</size>");
            }
            return returnStr.ToStringAndRelease();
        }

        public override int getPowerPriority()
        {
            return 5;
        }

        public static ConfigNode[] GetPropellants(bool isJet)
        {
            ConfigNode[] propellantList = isJet
                ? GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT")
                : GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");

            if (propellantList == null)
                PluginHelper.ShowInstallationErrorMessage();

            return propellantList;
        }

        private double GetHeatThrustModifier()
        {
            var thrustCoreTempThreshold = PluginSettings.Config.ThrustCoreTempThreshold;
            var lowCoreTempBaseThrust = PluginSettings.Config.LowCoreTempBaseThrust;

            return thrustCoreTempThreshold <= 0
                ? 1.0
                : AttachedReactor.MaxCoreTemperature < thrustCoreTempThreshold
                    ? (AttachedReactor.CoreTemperature + lowCoreTempBaseThrust) / (thrustCoreTempThreshold + lowCoreTempBaseThrust)
                    : 1.0 + PluginSettings.Config.HighCoreTempThrustMult * Math.Max(Math.Log10(AttachedReactor.CoreTemperature / thrustCoreTempThreshold), 0);
        }

        private float CurrentPowerThrustMultiplier => _currentPropellantIsJet ? powerTrustMultiplierJet : powerTrustMultiplier;

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginSettings.Config.GlobalThermalNozzlePowerMaxThrustMult * CurrentPowerThrustMultiplier;
        }

        private void UpdateRadiusModifier()
        {
            if (_myAttachedReactor != null)
            {
                // re-attach with updated radius
                _myAttachedReactor.DetachThermalReciever(id);
                _myAttachedReactor.AttachThermalReciever(id, radius);

                Fields[nameof(vacuumPerformance)].guiActiveEditor = true;
                Fields[nameof(radiusModifier)].guiActiveEditor = true;
                Fields[nameof(surfacePerformance)].guiActiveEditor = true;

                effectiveThrustFraction = GetHeatExchangerThrustMultiplier();

                radiusModifier = effectiveThrustFraction.ToString("P1");

                UpdateMaxIsp();

                maximumReactorPower = AttachedReactor.MaximumPower;

                heatThrustModifier = GetHeatThrustModifier();
                powerThrustModifier = GetPowerThrustModifier();

                max_thrust_in_space = powerThrustModifier * heatThrustModifier * maximumReactorPower / _maxISP / PhysicsGlobals.GravitationalAcceleration * effectiveThrustFraction;
                final_max_thrust_in_space = Math.Max(max_thrust_in_space * _thrustPropellantMultiplier, minimumThrust);

                // Set max thrust
                if (AttachedEngine != null)
                    AttachedEngine.maxThrust = (float)final_max_thrust_in_space;

                var ispInSpace = _maxISP;

                vacuumPerformance = final_max_thrust_in_space.ToString("F1") + " kN @ " + ispInSpace.ToString("F0") + " s";

                maxPressureThresholdAtKerbinSurface = scaledExitArea * GameConstants.EarthAtmospherePressureAtSeaLevel;

                var maxSurfaceThrust = Math.Max(max_thrust_in_space - (maxPressureThresholdAtKerbinSurface), minimumThrust);
                var maxSurfaceIsp = _maxISP * (maxSurfaceThrust / max_thrust_in_space);
                var finalMaxSurfaceThrust = maxSurfaceThrust * _thrustPropellantMultiplier;

                surfacePerformance = finalMaxSurfaceThrust.ToString("F1") + " kN @ " + maxSurfaceIsp.ToString("F0") + " s";
            }
            else
            {
                Fields[nameof(vacuumPerformance)].guiActiveEditor = false;
                Fields[nameof(radiusModifier)].guiActiveEditor = false;
                Fields[nameof(surfacePerformance)].guiActiveEditor = false;
            }
        }

        private double GetHeatExchangerThrustMultiplier()
        {
            if (AttachedReactor == null || AttachedReactor.Radius == 0 || radius == 0) return 0;

            var currentFraction = _myAttachedReactor.GetFractionThermalReciever(id);

            if (currentFraction == 0) return storedFractionThermalReciever;

            // scale down thrust if it's attached to a larger sized reactor
            var  heatExchangeRatio = radius >= AttachedReactor.Radius ? 1
                : radius * radius / AttachedReactor.Radius / AttachedReactor.Radius;

            storedFractionThermalReciever = Math.Min(currentFraction, heatExchangeRatio);

            return storedFractionThermalReciever;
        }

        private static ConfigNode[] GetPropellantsHybrid()
        {
            ConfigNode[] atmosphericNtrPropellants = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT");
            ConfigNode[] basicNtrPropellants = GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");
            atmosphericNtrPropellants = atmosphericNtrPropellants.Concat(basicNtrPropellants).ToArray();

            return atmosphericNtrPropellants;
        }

        public override string getResourceManagerDisplayName()
        {
            var displayName = part.partInfo.title + " " + Localizer.Format("#LOC_KSPIE_ThermalNozzleController_nozzle");//" (nozzle)"

            if (similarParts == null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(part);
            }

            if (similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }

        public void OnGUI()
        {
            if (vessel == FlightGlobals.ActiveVessel && render_window)
                _windowPosition = GUILayout.Window(_windowId, _windowPosition, Window, part.partInfo.title);
        }

        private void Window(int windowId)
        {
            windowPositionX = _windowPosition.x;
            windowPositionY = _windowPosition.y;

            if (GUI.Button(new Rect(_windowPosition.width - 20, 2, 18, 18), "x"))
            {
                render_window = false;
            }

            GUILayout.BeginVertical();

            if (_compatibleThermalEngineFuels != null)
            {
                foreach (var fuel in _compatibleThermalEngineFuels)
                {
                    if (!HighLogic.LoadedSceneIsEditor && !fuel.HasAnyStorage()) continue;

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(fuel.GuiName, GUILayout.ExpandWidth(true)))
                    {
                        fuel_mode = fuel.Index;
                        SetupPropellants(true);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
