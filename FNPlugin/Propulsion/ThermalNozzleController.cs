using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Propulsion;
using FNPlugin.Redist;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin
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
        // Persistent True
        [KSPField(isPersistant = true)]
        public double storedAbsoluteFactor = 1;
        [KSPField(isPersistant = true)]
        public double storedFractionThermalReciever = 1;

        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Soot Accumulation", guiUnits = " %", guiFormat = "F4")]
        public double sootAccumulationPercentage;
        [KSPField(isPersistant = true)]
        public bool isDeployed = false;
        [KSPField(isPersistant = true)]
        public double animationStarted = 0;
        [KSPField(isPersistant = true)]
        public bool exhaustAllowed = true;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Propelant Window"), UI_Toggle(disabledText = "Hidden", enabledText = "Shown", affectSymCounterparts = UI_Scene.None)]
        public bool render_window = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Isp Throttle"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = 0)]
        public float ispThrottle = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Fuel Flow Throttle"), UI_FloatRange(stepIncrement = 10, maxValue = 1000, minValue = 100)]
        public float fuelflowThrottle = 100;

        [KSPField(guiActive = false, guiName = "Max Fuel Flow", guiFormat = "F5")]
        protected double max_fuel_flow_rate = 0;
        [KSPField(guiActive = false, guiName = "Max FuelFlow on engine", guiFormat = "F5")]
        public float maxFuelFlowOnEngine;
        [KSPField(guiActive = false, guiName = "Fuelflow Multplier on engine", guiFormat = "F5")]
        public double fuelflowMultplier;
        [KSPField(guiActive = false, guiName = "Fuelflow Throtle modifier", guiFormat = "F5")]
        public double fuelflow_throtle_modifier = 1;


        [KSPField]
        public double missingPrecoolerProportionExponent = 0.5;

        [KSPField]
        public double exhaustModifier;        
        [KSPField]
        public double minimumBaseIsp = 0;
        [KSPField]
        public bool canUsePureChargedPower = false;
        [KSPField]
        public float jetengineAccelerationBaseSpeed = 0.2f;
        [KSPField]
        public float jetengineDecelerationBaseSpeed = 0.4f;
        [KSPField]
        public double engineAccelerationBaseSpeed = 2;
        [KSPField]
        public double engineDecelerationBaseSpeed = 2;

        [KSPField]
        public double wasteheatRatioDecelerationMult = 10;

        [KSPField]
        public float finalEngineDecelerationSpeed;
        [KSPField]
        public float finalEngineAccelerationSpeed;
        [KSPField]
        public bool useEngineResponseTime;
        [KSPField]
        public bool initialized = false;
        [KSPField]
        public float wasteHeatMultiplier = 1;
        [KSPField]
        public int jetPerformanceProfile = 0;
        [KSPField]
        public bool canUseLFO = false;
        [KSPField]
        public bool isJet = false;
        [KSPField]
        public float powerTrustMultiplier = 1;
        [KSPField]
        public float powerTrustMultiplierJet = 1;
        [KSPField]
        public double IspTempMultOffset = -1.371670613;
        [KSPField]
        public float sootHeatDivider = 150;
        [KSPField]
        public float sootThrustDivider = 150;
        [KSPField]
        public double maxTemp = 2750;
        [KSPField]
        public double heatConductivity = 0.12;
        [KSPField]
        public double heatConvectiveConstant = 1;
        [KSPField]
        public double emissiveConstant = 0.85;
        [KSPField]
        public float thermalMassModifier = 1f;
        [KSPField]
        public float engineHeatProductionConst = 3000;
        [KSPField]
        public double engineHeatProductionExponent = 0.8;
        [KSPField]
        public double engineHeatFuelThreshold = 0.000001;
        [KSPField]
        public double skinMaxTemp = 2750;
        [KSPField]
        public float maxThermalNozzleIsp = 0;
        [KSPField]
        public double skinInternalConductionMult = 1;
        [KSPField]
        public double skinThermalMassModifier = 1;
        [KSPField]
        public double skinSkinConductionMult = 1;
        [KSPField]
        public string deployAnimationName = String.Empty;
        [KSPField]
        public string pulseAnimationName = String.Empty;
        [KSPField]
        public string emiAnimationName = String.Empty;
        [KSPField]
        public float pulseDuration = 0;
        [KSPField]
        public float recoveryAnimationDivider = 1;
        [KSPField]
        public double wasteheatEfficiencyLowTemperature = 0.99;
        [KSPField]
        public double wasteheatEfficiencyHighTemperature = 0.99;
        [KSPField]
        public float upgradeCost = 1;
        [KSPField]
        public string originalName = "";
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public string upgradeTechReq = "";
        [KSPField]
        public string EffectNameJet = String.Empty;
        [KSPField]
        public string EffectNameLFO = String.Empty;
        [KSPField]
        public string EffectNameNonLFO = String.Empty;
        [KSPField]
        public string EffectNameLithium = String.Empty;


        [KSPField]
        public string runningEffectNameLFO = String.Empty;
        [KSPField]
        public string runningEffectNameNonLFO = String.Empty;

        [KSPField]
        public string powerEffectNameLFO = String.Empty;
        [KSPField]
        public string powerEffectNameNonLFO = String.Empty;

        [KSPField(isPersistant = true)]
        public float windowPositionX = 1000;
        [KSPField(isPersistant = true)]
        public float windowPositionY = 200;
        [KSPField]
        public float windowWidth = 200;


        [KSPField]
        public bool showPartTemperature = true;
        [KSPField]
        public double baseMaxIsp;
        [KSPField]
        public double wasteHeatBufferMassMult = 2.0e+5;
        [KSPField]
        public double wasteHeatBufferMult = 1;
        [KSPField]
        public bool allowUseOfThermalPower = true;
        [KSPField]
        public bool allowUseOfChargedPower = true;
        [KSPField]
        public bool overrideAtmCurve = true;
        [KSPField]
        public bool overrideVelocityCurve = true;
        [KSPField]
        public bool overrideAtmosphereCurve = true;
        [KSPField]
        public bool overrideAccelerationSpeed = true;
        [KSPField]
        public bool overrideDecelerationSpeed = true;
        [KSPField]
        public bool usePropellantBaseIsp = false;
        [KSPField]
        public bool isPlasmaNozzle = false;
        [KSPField]
        public bool canUsePlasmaPower = false;
        [KSPField]
        public double requiredMegajouleRatio = 0;
        [KSPField]
        public double radius = 2.5;
        [KSPField]
        public double exitArea = 1;
        [KSPField]
        public double exitAreaScaleExponent = 2;
        [KSPField]
        public double plasmaAfterburnerRange = 20;
        [KSPField]
        public bool showThrustPercentage = true;

        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Radius", guiUnits = " m", guiFormat = "F3")]
        public double scaledRadius;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Exit Area", guiUnits = " m2", guiFormat = "F3")]
        public double scaledExitArea = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Afterburner upgrade tech")]
        public string afterburnerTechReq = String.Empty;

        //GUI
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Propellant")]
        public string _fuelmode;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Propellant Isp Multiplier", guiFormat = "F3")]
        public double _ispPropellantMultiplier = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Soot")]
        public float _propellantSootFactorFullThrotle;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Min Soot")]
        public float _propellantSootFactorMinThrotle;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Equilibrium Soot")]
        public float _propellantSootFactorEquilibrium;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Temperature")]
        public string temperatureStr = "";
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "ISP / Thrust Mult")]
        public string thrustIspMultiplier = "";
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Fuel Thrust Multiplier", guiFormat = "F3")]
        public double _thrustPropellantMultiplier = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Upgrade Cost")]
        public string upgradeCostStr;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Control Heat Production")]
        public bool controlHeatProduction = true;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Heat Exponent")]
        public float heatProductionExponent = 7.1f;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Radius Heat Exponent")]
        public double radiusHeatProductionExponent = 0.25;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Radius Heat Multiplier")]
        public double radiusHeatProductionMult = 10;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Heat Production Multiplier")]
        public double heatProductionMultiplier = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Isp modifier")]
        public double ispHeatModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Radius modifier")]
        public double radiusHeatModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Engine Heat Production Mult")]
        public double engineHeatProductionMult;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Power To Mass")]
        public double powerToMass;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Space Heat Production")]
        public double spaceHeatProduction = 100;
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Engine Heat Production", guiFormat = "F5")]
        public double engineHeatProduction;
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Max Thrust On Engine", guiUnits = " kN")]
        public float maxThrustOnEngine;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Effective Isp On Engine")]
        public float realIspEngine;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Threshold", guiUnits = " kN", guiFormat = "F5")]
        public double pressureThreshold;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Requested ThermalHeat", guiUnits = " MJ", guiFormat = "F3")]
        public double requested_thermal_power;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Requested Charge", guiUnits = " MJ")]
        public double requested_charge_particles;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Recieved Power", guiUnits = " MJ", guiFormat = "F3")]
        public double reactor_power_received;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Radius Modifier")]
        public string radiusModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Vacuum")]
        public string vacuumPerformance;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Sea")]
        public string surfacePerformance;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Base Isp")]
        protected float _baseIspMultiplier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Decomposition Energy")]
        protected float _decompositionEnergy;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Engine Max Thrust", guiFormat = "F3", guiUnits = " kN")]
        protected double engineMaxThrust;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Thrust Per MJ", guiFormat = "F3", guiUnits = " kN")]
        protected double thrustPerMegaJoule;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Hydrogen Thrust In Space")]
        protected double max_thrust_in_space;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Final Max Thrust In Space", guiFormat = "F3", guiUnits = " kN")]
        protected double final_max_thrust_in_space;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Thrust In Current Atmosphere")]
        protected double max_thrust_in_current_atmosphere;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Current Max Engine Thrust", guiFormat = "F3", guiUnits = " kN")]
        protected double final_max_engine_thrust;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Maximum ISP", guiFormat = "F1", guiUnits = "s")]
        protected double _maxISP;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Minimum ISP", guiFormat = "F1", guiUnits = "s")]
        protected double _minISP;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Calculated Thrust", guiFormat = "F3", guiUnits = " kN")]
        protected double calculatedMaxThrust;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Current Mass Flow", guiFormat = "F5")]
        protected double currentMassFlow;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Is Open CycleCooler", guiFormat = "F5")]
        protected bool isOpenCycleCooler;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Fuel Flow ForCooling", guiFormat = "F5")]
        protected double fuelFlowForCooling;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Air Cooling", guiFormat = "F5")]
        protected double airFlowForCooling;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Current Isp", guiFormat = "F3")]
        protected double current_isp = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Max Pressure Thresshold @ 1 atm", guiFormat = "F3", guiUnits = " kN")]
        protected double maxPressureThresholdAtKerbinSurface;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Thermal Ratio")]
        protected double thermalResourceRatio;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Charged Power Ratio")]
        protected double chargedResourceRatio;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Expected Max Thrust")]
        protected double expectedMaxThrust;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Is LFO")]
        protected bool _propellantIsLFO = false;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Velocity Modifier", guiFormat = "F3")]
        protected float vcurveAtCurrentVelocity;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Atmosphere Modifier", guiFormat = "F3")]
        protected float atmosphereModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Atom Type")]
        protected int _atomType = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Propellant Type")]
        protected int _propType = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Is Neutron Absorber")]
        protected bool _isNeutronAbsorber = false;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Thermal Power", guiUnits = " MJ")]
        protected double currentMaxThermalPower;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Charged Power", guiUnits = " MJ")]
        protected double currentMaxChargedPower;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Available T Power ", guiUnits = " MJ")]
        protected double availableThermalPower;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Available C Power ", guiUnits = " MJ")]
        protected double availableChargedPower;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Air Flow Heat Modifier", guiFormat = "F3")]
        protected double airflowHeatModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Thermal Power Supply", guiFormat = "F3")]
        protected double effectiveThermalSupply;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Charged Power Supply", guiFormat = "F3")]
        protected double effectiveChargedSupply;
        [KSPField(guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumPowerUsageForPropulsionRatio;
        [KSPField(guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumThermalPower;
        [KSPField(guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumChargedPower;
        [KSPField(guiActive = false, guiActiveEditor = true, guiFormat = "F3", guiName = "Maximum Reactor Power", guiUnits = " MW")]
        public double maximumReactorPower;
        [KSPField(guiActive = false, guiActiveEditor = true, guiFormat = "F3", guiName = "Heat Thrust Modifier")]
        public double heatThrustModifier;
        [KSPField(guiActive = false, guiActiveEditor = true, guiFormat = "F3", guiName = "Heat Thrust Modifier")]
        public double powerThrustModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Effective Thrust Fraction")]
        public double effectiveThrustFraction = 1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Electricaly Powered", guiUnits = "%", guiFormat = "F3")]
        public double received_megajoules_percentage;

        [KSPField]
        public double minimumThrust = 0.000001;
        [KSPField]
        public bool showIspThrotle = false;
        [KSPField]
        public double powerHeatModifier;
        [KSPField]
        public double plasmaDuelModeHeatModifier = 0.1;
        [KSPField]
        public double plasmaAfterburnerHeatModifier = 0.5;
        [KSPField]
        public double thermalHeatModifier = 5;
        [KSPField]
        public double currentThrottle;
        [KSPField]
        public double previousThrottle;
        [KSPField]
        public double delayedThrottle;
        [KSPField]
        public double previousDelayedThrottle;
        [KSPField]
        public double adjustedThrottle;
        [KSPField]
        public double adjustedFuelFlowMult;
        [KSPField]
        public double attachedReactorFuelRato;
        [KSPField]
        public double adjustedFuelFlowExponent = 2;
        [KSPField]
        public float requestedThrottle;
        [KSPField]
        public float powerEffectRatio;
        [KSPField]
        public float runningEffectRatio;
        [KSPField]
        double received_megajoules_ratio;
        [KSPField]
        double pre_cooler_area;
        [KSPField]
        double intakes_open_area;
        [KSPField(guiActive = true, guiName = "Missing Precooler Ratio")]
        double missingPrecoolerRatio;
        [KSPField]
        float effectiveJetengineAccelerationSpeed;
        [KSPField]
        float effectiveJetengineDecelerationSpeed;
        [KSPField]
        public int supportedPropellantAtoms = 511;
        [KSPField]
        public int supportedPropellantTypes = 511;
        [KSPField]
        public double minThrottle = 0;
        [KSPField]
        public double reactorHeatModifier;

        // Constants
        protected const double _hydroloxDecompositionEnergy = 16.2137;

        //Internal
        protected string _flameoutText;
        protected string _powerEffectNameParticleFX;
        protected string _runningEffectNameParticleFX;
        protected string _fuelTechRequirement;
        
        protected double _heatDecompositionFraction;
        protected double _fuelflowThrottleMaxValue = 100;

        protected float _fuelCoolingFactor = 1;
        protected float _fuelToxicity;
        protected float _fuelMinimumCoreTemp;
        protected float _currentAnimatioRatio;
        protected float _minDecompositionTemp;
        protected float _maxDecompositionTemp;
        protected float _originalEngineAccelerationSpeed;
        protected float _originalEngineDecelerationSpeed;
        protected float _jetTechBonus;
        protected float _jetTechBonusPercentage;
        protected float _jetTechBonusCurveChange;

        protected int partDistance = 0;
        protected int _windowID;

        protected bool _fuelRequiresUpgrade = false;
        protected bool _engineWasInactivePreviousFrame = false;
        protected bool _hasrequiredupgrade = false;
        protected bool _hasSetupPropellant = false;
        protected bool _currentpropellant_is_jet = false;

        protected BaseField fuelflowThrottleField;
        protected BaseField sootAccumulationPercentageField;
        protected BaseField upgradeCostStrField;

        protected BaseEvent retrofitEngineEvent;

        protected UI_FloatRange fuelflowThrottleFloatRangeEditor;
        protected UI_FloatRange fuelflowThrottleFloatRangeFlight;

        protected FloatCurve atmCurve;
        protected FloatCurve atmosphereCurve;
        protected FloatCurve velCurve;

        protected FloatCurve originalAtmCurve;
        protected FloatCurve originalAtmosphereCurve;
        protected FloatCurve originalVelocityCurve;
        protected Animation deployAnim;
        protected AnimationState[] pulseAnimationState;
        protected AnimationState[] emiAnimationState;
        protected ResourceBuffers resourceBuffers;
        protected ModuleEnginesWarp timewarpEngine;
        protected ModuleEngines myAttachedEngine;
        protected Guid id = Guid.NewGuid();
        protected ConfigNode[] fuelConfignodes;

        protected List<Propellant> list_of_propellants = new List<Propellant>();
        protected List<FNModulePreecooler> _vesselPrecoolers;
        protected List<AtmosphericIntake> _vesselResourceIntakes;
        protected List<IFNEngineNoozle> _vesselThermalNozzles;

        protected List<ThermalEngineFuel> _allThermalEngineFuels;
        protected List<ThermalEngineFuel> _compatibleThermalEngineFuels;

        //protected GUIStyle blackGuiStyle;
        //protected GUIStyle grayGuiStyle;

        protected Rect windowPosition;

        private IFNPowerSource _myAttachedReactor;
        public IFNPowerSource AttachedReactor
        {
            get { return _myAttachedReactor; }
            private set
            {
                _myAttachedReactor = value;
                if (_myAttachedReactor == null)
                    return;
                _myAttachedReactor.AttachThermalReciever(id, scaledRadius);
            }
        }

        //Static
        static Dictionary<string, double> intake_amounts = new Dictionary<string, double>();
        static Dictionary<string, double> intake_maxamounts = new Dictionary<string, double>();
        static Dictionary<string, double> fuel_flow_amounts = new Dictionary<string, double>();

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        private int switches = 0;

        public double EffectiveCoreTempIspMult
        {
            get { return PluginHelper.IspCoreTempMult + IspTempMultOffset; }
        }

        public bool UsePlasmaPower
        {
            get { return isPlasmaNozzle || canUsePlasmaPower && (AttachedReactor != null && AttachedReactor.PlasmaPropulsionEfficiency > 0); }
        }

        public bool UseThermalPowerOnly
        {
            get { return AttachedReactor != null && (!isPlasmaNozzle || AttachedReactor.ChargedParticlePropulsionEfficiency == 0 || AttachedReactor.SupportMHD || AttachedReactor.ChargedPowerRatio == 0 || list_of_propellants.Count > 1 ); }
        }

        public bool UseThermalAndChargdPower
        {
            get { return !UseThermalPowerOnly && ispThrottle == 0; }
        }

        public bool UsePlasmaAfterBurner
        {
            get { return AttachedReactor.ChargedParticlePropulsionEfficiency > 0 && isPlasmaNozzle && ispThrottle != 0 && (ispThrottle != 1 || !canUsePureChargedPower); }
        }

        public bool UseChargedPowerOnly
        {
            get { return canUsePureChargedPower && ispThrottle == 100 && list_of_propellants.Count == 1; }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next Propellant", active = true)]
        public void NextPropellant()
        {
            fuel_mode++;
            if (fuel_mode >= fuelConfignodes.Length)
                fuel_mode = 0;

            SetupPropellants(true, false);
        }

        public void NextPropellantInternal()
        {
            fuel_mode++;
            if (fuel_mode >= fuelConfignodes.Length)
                fuel_mode = 0;

            SetupPropellants(fuel_mode, true, false);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous Propellant", active = true)]
        public void PreviousPropellant()
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuelConfignodes.Length - 1;

            SetupPropellants(false, false);
        }

        public void PreviousPropellantInternal()
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuelConfignodes.Length - 1;

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
            scaledRadius = radius * storedAbsoluteFactor;
            scaledExitArea = exitArea * Math.Pow(storedAbsoluteFactor, exitAreaScaleExponent);
        }


        [KSPAction("Next Propellant")]
        public void TogglePropellantAction(KSPActionParam param)
        {
            NextPropellant();
        }

        [KSPAction("Previous Propellant")]
        public void PreviousPropellant(KSPActionParam param)
        {
            PreviousPropellant();
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
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
                if (myAttachedEngine != null && myAttachedEngine.isOperational && exhaustAllowed)
                    return (float)(adjustedThrottle * received_megajoules_ratio * effectiveThrustFraction * fuelflow_throtle_modifier);
                else
                    return 0;
            }
        }

        public bool PropellantAbsorbsNeutrons { get { return _isNeutronAbsorber; } }

        public bool RequiresPlasmaHeat { get { return UsePlasmaPower; } }

        public bool RequiresThermalHeat { get { return !UsePlasmaPower; } }

        public bool RequiresChargedPower { get { return false; } }

        public void upgradePartModule()
        {
            isupgraded = true;

            if (isJet)
                fuelConfignodes = getPropellantsHybrid();
            else
                fuelConfignodes = getPropellants(isJet);
        }

        public ConfigNode[] getPropellants()
        {
            return fuelConfignodes;
        }

        public void OnEditorAttach()
        {
            ConnectToThermalSource();

            if (AttachedReactor == null) return;

            try { LoadFuelModes(); }
            catch (Exception e) { Debug.LogError("[KSPI]: OnEditorAttach Exception in LoadFuelModes" + e.Message); }

            try { EstimateEditorPerformance(); }
            catch (Exception e) { Debug.LogError("[KSPI]: OnEditorAttach Exception in EstimateEditorPerformance" + e.Message); }

            try { SetupPropellants(fuel_mode); }
            catch (Exception e) { Debug.LogError("[KSPI]: OnEditorAttach Exception in SetupPropellants" + e.Message); }
        }

        public void OnEditorDetach()
        {
            foreach (var symPart in part.symmetryCounterparts)
            {
                var symThermalNozzle = symPart.FindModuleImplementing<ThermalEngineController>();

                if (symThermalNozzle != null)
                {
                    Debug.Log("[KSPI]: called DetachWithReactor on symmetryCounterpart");
                    symThermalNozzle.DetachWithReactor();
                }
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

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("[KSPI]: ThermalNozzleController - start");

            _windowID = new System.Random(part.GetInstanceID()).Next(int.MaxValue);
            windowPosition = new Rect(windowPositionX, windowPositionY, windowWidth, 10);

            _flameoutText = Localizer.Format("#autoLOC_219016");

            // use default when maxThermalNozzleIsp is not configured
            if (maxThermalNozzleIsp == 0)
                maxThermalNozzleIsp = PluginHelper.MaxThermalNozzleIsp;

            ScaleParameters();            

            try
            {
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

                if (!String.IsNullOrEmpty(deployAnimationName))
                    deployAnim = part.FindModelAnimators(deployAnimationName).FirstOrDefault();
                if (!String.IsNullOrEmpty(pulseAnimationName))
                    pulseAnimationState = PluginHelper.SetUpAnimation(pulseAnimationName, this.part);
                if (!String.IsNullOrEmpty(emiAnimationName))
                    emiAnimationState = PluginHelper.SetUpAnimation(emiAnimationName, this.part);

                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, wasteHeatBufferMassMult * wasteHeatBufferMult, true));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                resourceBuffers.Init(this.part);

                myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
                timewarpEngine = this.part.FindModuleImplementing<ModuleEnginesWarp>();

                if (myAttachedEngine != null)
                {
                    myAttachedEngine.Fields["thrustPercentage"].guiActive = showThrustPercentage;

                    originalAtmCurve = myAttachedEngine.atmCurve;
                    originalAtmosphereCurve = myAttachedEngine.atmosphereCurve;
                    originalVelocityCurve = myAttachedEngine.velCurve;

                    _originalEngineAccelerationSpeed = myAttachedEngine.engineAccelerationSpeed;
                    _originalEngineDecelerationSpeed = myAttachedEngine.engineDecelerationSpeed;
                }
                else
                    Debug.LogError("[KSPI]: ThermalNozzleController - failed to find engine!");

                // find attached thermal source
                ConnectToThermalSource();

                maxPressureThresholdAtKerbinSurface = scaledExitArea * GameConstants.EarthAtmospherePressureAtSeaLevel;

                fuelflowThrottleField = Fields["fuelflowThrottle"];
                if (fuelflowThrottleField != null)
                {
                    fuelflowThrottleFloatRangeEditor = fuelflowThrottleField.uiControlEditor as UI_FloatRange;
                    fuelflowThrottleFloatRangeFlight = fuelflowThrottleField.uiControlFlight as UI_FloatRange;
                }
                else
                    Debug.LogError("[KSPI]: ThermalNozzleController - failed to find fuelflowThrottle field");

                var ispThrottleField = Fields["ispThrottle"];
                if (ispThrottleField != null)
                {
                    ispThrottleField.guiActiveEditor = showIspThrotle;
                    ispThrottleField.guiActive = showIspThrotle;
                }

                Debug.Log("[KSPI]: ThermalNozzleController - set Editor");

                if (state == StartState.Editor)
                {
                    part.OnEditorAttach += OnEditorAttach;
                    part.OnEditorDetach += OnEditorDetach;

                    fuelConfignodes = getPropellants(isJet);
                    if (this.HasTechsRequiredToUpgrade())
                    {
                        isupgraded = true;
                        upgradePartModule();
                    }

                    try { LoadFuelModes(); }
                    catch (Exception e) { Debug.LogError("[KSPI]: OnStart Exception in LoadFuelModes" + e.Message); }

                    try { SetupPropellants(fuel_mode); }
                    catch (Exception e) { Debug.LogError("[KSPI]: OnStart Exception in SetupPropellants" + e.Message); }

                    try { EstimateEditorPerformance(); }
                    catch (Exception e) { Debug.LogError("[KSPI]: OnStart Exception in EstimateEditorPerformance" + e.Message); }

                    try { UpdateRadiusModifier(); }
                    catch (Exception e) { Debug.LogError("[KSPI]: OnStart Exception in UpdateRadiusModifier" + e.Message); }

                    return;
                }

                if (requiredMegajouleRatio == 0)
                    received_megajoules_ratio = 1;

                Fields["received_megajoules_percentage"].guiActive = requiredMegajouleRatio > 0;

                sootAccumulationPercentageField = Fields["sootAccumulationPercentage"];
                upgradeCostStrField = Fields["upgradeCostStr"];
                retrofitEngineEvent = Events["RetrofitEngine"];

                UpdateRadiusModifier();

                UpdateIspEngineParams();

                // presearch all avaialble precoolers, intakes and nozzles on the vessel
                _vesselPrecoolers = vessel.FindPartModulesImplementing<FNModulePreecooler>();
                _vesselResourceIntakes = vessel.FindPartModulesImplementing<AtmosphericIntake>();
                _vesselThermalNozzles = vessel.FindPartModulesImplementing<IFNEngineNoozle>();

                // if we can upgrade, let's do so
                if (isupgraded)
                    upgradePartModule();
                else
                {
                    if (this.HasTechsRequiredToUpgrade())
                        _hasrequiredupgrade = true;

                    // if not, use basic propellants
                    fuelConfignodes = getPropellants(isJet);
                }

                bool hasJetUpgradeTech1 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech1);
                bool hasJetUpgradeTech2 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech2);
                bool hasJetUpgradeTech3 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech3);
                bool hasJetUpgradeTech4 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech4);
                bool hasJetUpgradeTech5 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech5);

                _jetTechBonus = 1 + Convert.ToInt32(hasJetUpgradeTech1) * 1.2f + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f * Convert.ToInt32(hasJetUpgradeTech3) + 2.0736f * Convert.ToInt32(hasJetUpgradeTech4) + 2.48832f * Convert.ToInt32(hasJetUpgradeTech5);
                _jetTechBonusCurveChange = _jetTechBonus / 9.92992f;
                _jetTechBonusPercentage = _jetTechBonus / 49.6496f;

                var reactorSpeed = AttachedReactor.ReactorSpeedMult > 0 ? AttachedReactor.ReactorSpeedMult : 1;

                effectiveJetengineAccelerationSpeed = overrideAccelerationSpeed ? jetengineAccelerationBaseSpeed * (float)reactorSpeed * _jetTechBonusCurveChange * 5 : _originalEngineAccelerationSpeed;
                effectiveJetengineDecelerationSpeed = overrideDecelerationSpeed ? jetengineDecelerationBaseSpeed * (float)reactorSpeed * _jetTechBonusCurveChange * 5 : _originalEngineDecelerationSpeed;

                Fields["temperatureStr"].guiActive = showPartTemperature;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: OnStart Exception in ThermalNozzleController.OnStart: " + e.Message);
            }

            try { LoadFuelModes(); }
            catch (Exception e) { Debug.LogError("[KSPI]: OnStart Exception in LoadFuelModes" + e.Message); }

            try { SetupPropellants(fuel_mode); }
            catch (Exception e) { Debug.LogError("[KSPI]: OnStart Exception in SetupPropellant" + e.Message); }
        }

        private void ConfigEffects()
        {
            if (myAttachedEngine is ModuleEnginesFX)
            {
                if (!String.IsNullOrEmpty(EffectNameJet))
                    part.Effect(EffectNameJet, 0, -1);
                if (!String.IsNullOrEmpty(EffectNameLFO))
                    part.Effect(EffectNameLFO, 0, -1);
                if (!String.IsNullOrEmpty(EffectNameNonLFO))
                    part.Effect(EffectNameNonLFO, 0, -1);
                if (!String.IsNullOrEmpty(EffectNameLithium))
                    part.Effect(EffectNameLithium, 0, -1);

                if (!String.IsNullOrEmpty(runningEffectNameNonLFO))
                    part.Effect(runningEffectNameNonLFO, 0, -1);
                if (!String.IsNullOrEmpty(runningEffectNameLFO))
                    part.Effect(runningEffectNameLFO, 0, -1);

                if (!String.IsNullOrEmpty(powerEffectNameNonLFO))
                    part.Effect(powerEffectNameNonLFO, 0, -1);
                if (!String.IsNullOrEmpty(powerEffectNameLFO))
                    part.Effect(powerEffectNameLFO, 0, -1);

                if (_currentpropellant_is_jet && !String.IsNullOrEmpty(EffectNameJet))
                    _powerEffectNameParticleFX = EffectNameJet;
                else if (_propellantIsLFO)
                {
                    if (!String.IsNullOrEmpty(powerEffectNameLFO))
                        _powerEffectNameParticleFX = powerEffectNameLFO;
                    else  if (!String.IsNullOrEmpty(EffectNameLFO))
                        _powerEffectNameParticleFX = EffectNameLFO;

                    if (!String.IsNullOrEmpty(runningEffectNameLFO))
                        _runningEffectNameParticleFX = runningEffectNameLFO;
                }

                else if (_isNeutronAbsorber)
                {
                    if (!String.IsNullOrEmpty(EffectNameLithium))
                        _powerEffectNameParticleFX = EffectNameLithium;
                    else if (!String.IsNullOrEmpty(EffectNameLFO))
                        _powerEffectNameParticleFX = EffectNameLFO;
                }
                else
                {
                    if (!String.IsNullOrEmpty(powerEffectNameNonLFO))
                        _powerEffectNameParticleFX = powerEffectNameNonLFO;
                    else if (!String.IsNullOrEmpty(EffectNameNonLFO))
                        _powerEffectNameParticleFX = EffectNameNonLFO;

                    if (!String.IsNullOrEmpty(runningEffectNameNonLFO))
                        _runningEffectNameParticleFX = runningEffectNameNonLFO;
                }

            }
        }

        private void ConnectToThermalSource()
        {
            try
            {
                Debug.Log("[KSPI]: ThermalNozzleController - start ConnectToThermalSource");

                var source = PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part, (p) => p.IsThermalSource && maxThermalNozzleIsp >= p.MinThermalNozzleTempRequired, 10, 10, 10);

                if (source == null || source.Source == null)
                {
                    Debug.LogWarning("[KSPI]: ThermalNozzleController - ConnectToThermalSource Failed to find thermal source");
                    return;
                }

                AttachedReactor = source.Source;
                AttachedReactor.ConnectWithEngine(this);

                this.partDistance = (int)Math.Max(Math.Ceiling(source.Cost) - 1, 0);

                if (AttachedReactor != null)
                {
                    this.showIspThrotle = this.isPlasmaNozzle && AttachedReactor.ChargedParticlePropulsionEfficiency > 0 && AttachedReactor.ChargedPowerRatio > 0;

                    var ispThrottleField = Fields["ispThrottle"];
                    ispThrottleField.guiActiveEditor = showIspThrotle;
                    ispThrottleField.guiActive = showIspThrotle;
                }

                Debug.Log("[KSPI]: ThermalNozzleController - BreadthFirstSearchForThermalSource- Found thermal searchResult with distance " + partDistance);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI]: ThermalNozzleController ConnectToThermalSource  " + e.Message);
            }
        }

        // Is called in the VAB
        public virtual void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                EstimateEditorPerformance();

                UpdateRadiusModifier();

                UpdateIspEngineParams();
            }
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate()
        {
            try
            {
                // setup propellant after startup to allow InterstellarFuelSwitch to configure the propellant
                if (!_hasSetupPropellant)
                {
                    _hasSetupPropellant = true;
                    SetupPropellants(fuel_mode, true, true);
                }

                temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
                UpdateAtmosphericPresureTreshold();
                
                sootAccumulationPercentageField.guiActive = sootAccumulationPercentage > 0;

                thrustIspMultiplier = _ispPropellantMultiplier.ToString("0.0000") + " / " + _thrustPropellantMultiplier.ToString("0.0000");

                if (ResearchAndDevelopment.Instance != null && isJet)
                {
                    retrofitEngineEvent.active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasrequiredupgrade;
                    upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
                }
                else
                    retrofitEngineEvent.active = false;

                upgradeCostStrField.guiActive = !isupgraded && _hasrequiredupgrade && isJet;

                if (myAttachedEngine == null)
                    return;

                exhaustAllowed = AllowedExhaust();

                fuelflowMultplier = myAttachedEngine.flowMultiplier;

                // only allow shutdown when engine throttle is down
                myAttachedEngine.Events["Shutdown"].active = myAttachedEngine.currentThrottle == 0 && myAttachedEngine.getIgnitionState;

                if (myAttachedEngine.isOperational && !IsEnabled)
                {
                    IsEnabled = true;
                    part.force_activate();
                }

                if (IsEnabled && deployAnim != null && !initialized)
                {
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
                        myAttachedEngine.Shutdown();
                        animationStarted = Planetarium.GetUniversalTime();
                    }
                    else if ((Planetarium.GetUniversalTime() > animationStarted + deployAnim[deployAnimationName].length))
                    {
                        initialized = true;
                        isDeployed = true;
                        myAttachedEngine.Activate();
                    }
                }

                Fields["airflowHeatModifier"].guiActive = _currentpropellant_is_jet;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ThermalNozzle OnUpdates " + e.Message);
            }
        }

        private bool AllowedExhaust()
        {
            var homeworld = FlightGlobals.GetHomeBody();
            var toHomeworld = vessel.CoMD - homeworld.position;
            var distanceToSurfaceHomeworld = toHomeworld.magnitude - homeworld.Radius;
            var cosineAngle = Vector3d.Dot(part.transform.up.normalized, toHomeworld.normalized);
            var currentExhaustAngle = Math.Acos(cosineAngle) * (180 / Math.PI);

            if (double.IsNaN(currentExhaustAngle) || double.IsInfinity(currentExhaustAngle))
                currentExhaustAngle = cosineAngle > 0 ? 180 : 0;

            if (AttachedReactor == null)
                return false;

            double allowedExhaustAngle;
            if (AttachedReactor.MayExhaustInAtmosphereHomeworld)
            {
                allowedExhaustAngle = 180;
                return true;
            }

            var minAltitude = AttachedReactor.MayExhaustInLowSpaceHomeworld ? homeworld.atmosphereDepth : homeworld.scienceValues.spaceAltitudeThreshold;

            if (distanceToSurfaceHomeworld < minAltitude)
                return false;

            if (AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 10 * homeworld.Radius)
                return true;

            if (!AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 20 * homeworld.Radius)
                return true;

            var radiusDividedByAltitude = (homeworld.Radius + minAltitude) / toHomeworld.magnitude;

            var coneAngle = 45 * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude;

            allowedExhaustAngle = coneAngle + Math.Tanh(radiusDividedByAltitude) * (180 / Math.PI);

            if (allowedExhaustAngle < 3)
                return true;

            return currentExhaustAngle > allowedExhaustAngle;
        }

        public override void OnActive()
        {
            base.OnActive();

            try { LoadFuelModes(); }
            catch (Exception e) { Debug.LogError("[KSPI]: OnStart Exception in LoadFuelModes" + e.Message); }

            SetupPropellants(true, true);
        }

        public void LoadFuelModes()
        {
            _allThermalEngineFuels = fuelConfignodes.Select(node => new ThermalEngineFuel(node, fuelConfignodes.IndexOf(node), this.part)).ToList();

            Debug.Log("[KSPI]: ThermalNozzleController - Indexed " + _allThermalEngineFuels.Count + " fuelmodes");

            // quit if we do not have access to reactor
            if (AttachedReactor == null)
            {
                Debug.LogWarning("[KSPI]: ThermalNozzleController - Skiped filtering on compatible fuelmodes, no reactor avaialble");
                return;
            }

            _compatibleThermalEngineFuels = _allThermalEngineFuels.Where(fuel =>

                    PluginHelper.HasTechRequirementOrEmpty(fuel.TechRequirement) &&

                    (fuel.RequiresUpgrade == false || (_fuelRequiresUpgrade && isupgraded)) &&
                    (fuel.IsLFO == false || (fuel.IsLFO && PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))) &&
                    (fuel.CoolingFactor >= AttachedReactor.MinCoolingFactor) &&
                    (fuel.MinimumCoreTemp <= AttachedReactor.CoreTemperature) &&
                    ((fuel.AtomType & AttachedReactor.SupportedPropellantAtoms) == fuel.AtomType) &&
                    ((fuel.AtomType & this.supportedPropellantAtoms) == fuel.AtomType) &&
                    ((fuel.PropType & AttachedReactor.SupportedPropellantTypes) == fuel.PropType) &&
                    ((fuel.PropType & this.supportedPropellantTypes) == fuel.PropType)

                ).ToList();

            Debug.Log("[KSPI]: ThermalNozzleController - Found " + _compatibleThermalEngineFuels.Count + " compatible fuelmodes");

            var concatednated = string.Join("", _compatibleThermalEngineFuels.Select(m => m.GuiName).ToArray());            

            var nextPropellantEvent = Events["NextPropellant"];
            if (nextPropellantEvent != null)
            {
                nextPropellantEvent.guiActive = _compatibleThermalEngineFuels.Count > 1;
                nextPropellantEvent.guiActiveEditor = _compatibleThermalEngineFuels.Count > 1;
            }
            else
                Debug.LogError("[KSPI]: ThermalNozzleController - Failed to find NextPropellant event");

            var prevPropellantEvent = Events["PreviousPropellant"];
            if (prevPropellantEvent != null)
            {
                prevPropellantEvent.guiActive = _compatibleThermalEngineFuels.Count > 1;
                prevPropellantEvent.guiActiveEditor = _compatibleThermalEngineFuels.Count > 1;
            }
            else
                Debug.LogWarning("[KSPI]: ThermalNozzleController - Failed to find PreviousPropellant event");
        }

        public void SetupPropellants(bool forward = true, bool notifySwitching = false)
        {
            SetupPropellants(fuel_mode, forward, notifySwitching);

            foreach (var symPart in part.symmetryCounterparts)
            {
                var symThermalNozzle = symPart.FindModuleImplementing<ThermalEngineController>();

                if (symThermalNozzle != null)
                {
                    symThermalNozzle.SetupPropellants(fuel_mode, forward, notifySwitching);
                }
            }

        }

        public void SetupPropellants( int newFuelMode,  bool forward = true, bool notifySwitching = false)
        {
            if (_myAttachedReactor == null)
                return;

            fuel_mode = newFuelMode;

            try
            {
                var chosenpropellant = fuelConfignodes[fuel_mode];

                UpdatePropellantModeBehavior(chosenpropellant);
                ConfigNode[] propellantNodes = chosenpropellant.GetNodes("PROPELLANT");
                list_of_propellants.Clear();

                foreach (ConfigNode propNode in propellantNodes)
                {
                    var curprop = new ExtendedPropellant();
                    curprop.Load(propNode);

                    if (list_of_propellants == null)
                        Debug.LogWarning("[KSPI]: ThermalNozzleController - SetupPropellants list_of_propellants is null");

                    list_of_propellants.Add(curprop);
                }

                string missingResources = String.Empty;
                bool canLoadPropellant = true;

                if (
                         list_of_propellants.Any(m => PartResourceLibrary.Instance.GetDefinition(m.name) == null)
                    || (!PluginHelper.HasTechRequirementOrEmpty(_fuelTechRequirement))
                    || (_fuelRequiresUpgrade && !isupgraded)
                    || (_fuelMinimumCoreTemp > AttachedReactor.CoreTemperature)
                    || (_fuelCoolingFactor < AttachedReactor.MinCoolingFactor)
                    || (_propellantIsLFO && !PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))
                    || ((_atomType & AttachedReactor.SupportedPropellantAtoms) != _atomType)
                    || ((_atomType & this.supportedPropellantAtoms) != _atomType)
                    || ((_propType & AttachedReactor.SupportedPropellantTypes) != _propType)
                    || ((_propType & this.supportedPropellantTypes) != _propType)
                    )
                {
                    canLoadPropellant = false;
                }

                if (canLoadPropellant && HighLogic.LoadedSceneIsFlight)
                {
                    foreach (Propellant curEngine_propellant in list_of_propellants)
                    {
                        var extendedPropellant = curEngine_propellant as ExtendedPropellant;

                        var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(extendedPropellant.StoragePropellantName);
                        double amount = 0;
                        double maxAmount = 0;
                        if (resourceDefinition != null)
                            part.GetConnectedResourceTotals(resourceDefinition.id, extendedPropellant.GetFlowMode(), out amount, out maxAmount);

                        if (maxAmount == 0)
                        {
                            if (notifySwitching)
                                missingResources += curEngine_propellant.name + " ";
                            canLoadPropellant = false;
                            break;
                        }
                    }
                }

                //Get the Ignition state, i.e. is the engine shutdown or activated
                var engineState = myAttachedEngine.getIgnitionState;

                // update the engine with the new propellants
                if (canLoadPropellant)
                {
                    Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant chosenpropellant " + fuel_mode + " / " + fuelConfignodes.Count());

                    myAttachedEngine.Shutdown();

                    var newPropNode = new ConfigNode();

                    foreach (var prop in list_of_propellants)
                    {
                        var flowMode = prop.GetFlowMode();
                        Debug.Log("[KSPI]: ThermalNozzleController set propellant name: " + prop.name + " ratio: " + prop.ratio + " resourceFlowMode: " + flowMode.ToString());

                        var propellantConfigNode = newPropNode.AddNode("PROPELLANT");
                        propellantConfigNode.AddValue("name", prop.name);
                        propellantConfigNode.AddValue("ratio", prop.ratio);
                        propellantConfigNode.AddValue("DrawGauge", "true");

                        if (flowMode != ResourceFlowMode.NULL)
                            propellantConfigNode.AddValue("resourceFlowMode", flowMode.ToString());
                    }

                    myAttachedEngine.Load(newPropNode);

                    // update timewarp propellant
                    if (timewarpEngine != null)
                    {
                        if (list_of_propellants.Count > 0)
                        {
                            timewarpEngine.propellant1 = list_of_propellants[0].name;
                            timewarpEngine.ratio1 = list_of_propellants[0].ratio;
                        }
                        if (list_of_propellants.Count > 1)
                        {
                            timewarpEngine.propellant2 = list_of_propellants[1].name;
                            timewarpEngine.ratio2 = list_of_propellants[1].ratio;
                        }
                        if (list_of_propellants.Count > 2)
                        {
                            timewarpEngine.propellant3 = list_of_propellants[2].name;
                            timewarpEngine.ratio3 = list_of_propellants[2].ratio;
                        }
                        if (list_of_propellants.Count > 3)
                        {
                            timewarpEngine.propellant4 = list_of_propellants[3].name;
                            timewarpEngine.ratio4 = list_of_propellants[3].ratio;
                        }
                    }
                }

                if (canLoadPropellant && engineState == true)
                    myAttachedEngine.Activate();

                if (HighLogic.LoadedSceneIsFlight)
                { // you can have any fuel you want in the editor but not in flight
                    // should we switch to another propellant because we have none of this one?
                    bool next_propellant = false;

                    if (!canLoadPropellant)
                        next_propellant = true;

                    // do the switch if needed
                    if (next_propellant && (switches <= fuelConfignodes.Length || fuel_mode != 0))
                    {// always shows the first fuel mode when all fuel mods are tested at least once
                        ++switches;
                        if (notifySwitching)
                        {
                            ScreenMessages.PostScreenMessage("Switching Propellant, missing resource " + missingResources, 5.0f, ScreenMessageStyle.LOWER_CENTER);
                        }

                        if (forward)
                            NextPropellantInternal();
                        else
                            PreviousPropellantInternal();
                    }
                }
                else
                {
                    bool next_propellant = false;

                    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant " + list_of_propellants[0].name);

                    // Still ignore propellants that don't exist or we cannot use due to the limitations of the engine
                    if (!canLoadPropellant && (switches <= fuelConfignodes.Length || fuel_mode != 0))
                    {
                        //if (((_atomType & this.supportedPropellantAtoms) != _atomType))
                        //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant nozzle atom " + this.supportedPropellantAtoms + " != " + _atomType);
                        //if (((_propType & this.supportedPropellantTypes) != _propType))
                        //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant nozzle type " + this.supportedPropellantTypes + " != " + _propType);

                        //if (((_atomType & _myAttachedReactor.SupportedPropellantAtoms) != _atomType))
                        //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant reactor atom " + _myAttachedReactor.SupportedPropellantAtoms + " != " + _atomType);
                        //if (((_propType & _myAttachedReactor.SupportedPropellantTypes) != _propType))
                        //    UnityEngine.Debug.Log("[KSPI]: ThermalNozzleController - Setup propellant reactor type " + _myAttachedReactor.SupportedPropellantTypes + " != " + _propType);

                        next_propellant = true;
                    }

                    if (next_propellant)
                    {
                        ++switches;
                        if (forward)
                            NextPropellantInternal();
                        else
                            PreviousPropellantInternal();
                    }

                    EstimateEditorPerformance(); // update editor estimates
                }

                switches = 0;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Error SetupPropellants " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void UpdatePropellantModeBehavior(ConfigNode chosenpropellant)
        {
            _fuelmode = chosenpropellant.GetValue("guiName");
            _propellantIsLFO = chosenpropellant.HasValue("isLFO") && bool.Parse(chosenpropellant.GetValue("isLFO"));
            _currentpropellant_is_jet = chosenpropellant.HasValue("isJet") && bool.Parse(chosenpropellant.GetValue("isJet"));

            _propellantSootFactorFullThrotle = chosenpropellant.HasValue("maxSootFactor") ? float.Parse(chosenpropellant.GetValue("maxSootFactor")) : 0;
            _propellantSootFactorMinThrotle = chosenpropellant.HasValue("minSootFactor") ? float.Parse(chosenpropellant.GetValue("minSootFactor")) : 0;
            _propellantSootFactorEquilibrium = chosenpropellant.HasValue("levelSootFraction") ? float.Parse(chosenpropellant.GetValue("levelSootFraction")) : 0;
            _minDecompositionTemp = chosenpropellant.HasValue("MinDecompositionTemp") ? float.Parse(chosenpropellant.GetValue("MinDecompositionTemp")) : 0;
            _maxDecompositionTemp = chosenpropellant.HasValue("MaxDecompositionTemp") ? float.Parse(chosenpropellant.GetValue("MaxDecompositionTemp")) : 0;
            _decompositionEnergy = chosenpropellant.HasValue("DecompositionEnergy") ? float.Parse(chosenpropellant.GetValue("DecompositionEnergy")) : 0;
            _baseIspMultiplier = chosenpropellant.HasValue("BaseIspMultiplier") ? float.Parse(chosenpropellant.GetValue("BaseIspMultiplier")) : 0;
            _fuelTechRequirement = chosenpropellant.HasValue("TechRequirement") ? chosenpropellant.GetValue("TechRequirement") : String.Empty;
            _fuelCoolingFactor = chosenpropellant.HasValue("coolingFactor") ? float.Parse(chosenpropellant.GetValue("coolingFactor")) : 1;
            _fuelToxicity = chosenpropellant.HasValue("Toxicity") ? float.Parse(chosenpropellant.GetValue("Toxicity")) : 0;
            _fuelMinimumCoreTemp = chosenpropellant.HasValue("minimumCoreTemp") ? float.Parse(chosenpropellant.GetValue("minimumCoreTemp")) : 0;
            
            _fuelRequiresUpgrade = chosenpropellant.HasValue("RequiresUpgrade") && Boolean.Parse(chosenpropellant.GetValue("RequiresUpgrade"));
            _atomType = chosenpropellant.HasValue("atomType") ? int.Parse(chosenpropellant.GetValue("atomType")) : 1;
            _propType = chosenpropellant.HasValue("propType") ? int.Parse(chosenpropellant.GetValue("propType")) : 1;
            _isNeutronAbsorber = chosenpropellant.HasValue("isNeutronAbsorber") && bool.Parse(chosenpropellant.GetValue("isNeutronAbsorber"));

            if (!UsePlasmaPower && !usePropellantBaseIsp && !_currentpropellant_is_jet && _decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                UpdateThrustPropellantMultiplier();
            else
            {
                _heatDecompositionFraction = 1;

                if ((usePropellantBaseIsp || AttachedReactor.UsePropellantBaseIsp || UsePlasmaPower) && _baseIspMultiplier > 0)
                    _ispPropellantMultiplier = _baseIspMultiplier;
                else
                    _ispPropellantMultiplier = chosenpropellant.HasValue("ispMultiplier") ? float.Parse(chosenpropellant.GetValue("ispMultiplier")) : 1;

                var rawthrustPropellantMultiplier = chosenpropellant.HasValue("thrustMultiplier") ? float.Parse(chosenpropellant.GetValue("thrustMultiplier")) : 1;
                _thrustPropellantMultiplier = _propellantIsLFO || _currentpropellant_is_jet || rawthrustPropellantMultiplier <= 1 ? rawthrustPropellantMultiplier : ((rawthrustPropellantMultiplier + 1) / 2);
            }
        }

        private void UpdateThrustPropellantMultiplier()
        {
            var linearFraction = Math.Max(0, Math.Min(1, (AttachedReactor.CoreTemperature - _minDecompositionTemp) / (_maxDecompositionTemp - _minDecompositionTemp)));
            _heatDecompositionFraction = Math.Pow(0.36, Math.Pow(3 - linearFraction * 3, 2) / 2);
            var rawthrustPropellantMultiplier = Math.Sqrt(_heatDecompositionFraction * _decompositionEnergy / _hydroloxDecompositionEnergy) * 1.04 + 1;

            
            _ispPropellantMultiplier = _baseIspMultiplier * rawthrustPropellantMultiplier;
            _thrustPropellantMultiplier = _propellantIsLFO ? rawthrustPropellantMultiplier : (rawthrustPropellantMultiplier + 1) / 2;

            // lower efficiency of plasma nozzle when used with heavier propellants except when used with a neutron absorber like lithium
            if (UsePlasmaPower && !_isNeutronAbsorber)
            {
                var plasmaEfficiency = Math.Pow(_baseIspMultiplier, 1d/3d);
                _ispPropellantMultiplier *= plasmaEfficiency;
                _thrustPropellantMultiplier *= plasmaEfficiency;
            }
        }

        public void UpdateIspEngineParams(double atmosphere_isp_efficiency = 1, double performance_bonus = 0)
        {
            // recaculate ISP based on power and core temp available
            atmCurve = new FloatCurve();
            atmosphereCurve = new FloatCurve();
            velCurve = new FloatCurve();

            UpdateMaxIsp();

            if (!_currentpropellant_is_jet)
            {
                atmosphereCurve.Add(0, (float)(_maxISP * atmosphere_isp_efficiency), 0, 0);

                var wasteheatRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);
                var wasteheatModifier = wasteheatRatioDecelerationMult > 0 ? Math.Max((1 - wasteheatRatio) * wasteheatRatioDecelerationMult, 1) : 1;

                if (AttachedReactor != null)
                {
                    finalEngineAccelerationSpeed = (float)Math.Min(engineAccelerationBaseSpeed * AttachedReactor.ReactorSpeedMult, 33);
                    finalEngineDecelerationSpeed = (float)Math.Min(engineDecelerationBaseSpeed * AttachedReactor.ReactorSpeedMult * wasteheatModifier, 33);
                    useEngineResponseTime = AttachedReactor.ReactorSpeedMult > 0;
                }

                myAttachedEngine.useAtmCurve = false;
                myAttachedEngine.useVelCurve = false;
                myAttachedEngine.useEngineResponseTime = useEngineResponseTime;
                myAttachedEngine.engineAccelerationSpeed = finalEngineAccelerationSpeed;
                myAttachedEngine.engineDecelerationSpeed = finalEngineDecelerationSpeed;

                if (minThrottle > 0)
                {
                    var multiplier = 0.5f + myAttachedEngine.currentThrottle;
                    myAttachedEngine.engineAccelerationSpeed *= multiplier;
                    myAttachedEngine.engineDecelerationSpeed *= multiplier;
                }
            }
            else
            {
                if (overrideVelocityCurve && jetPerformanceProfile == 0)    // Ramjet
                {
                    velCurve.Add(0, 0.005f + _jetTechBonusPercentage / 40);
                    velCurve.Add(2.5f - _jetTechBonusCurveChange, 1);
                    velCurve.Add(5 + _jetTechBonusCurveChange * 2, 1);
                    velCurve.Add(14, 0 + _jetTechBonusPercentage);
                    velCurve.Add(20, 0);
                }
                else if (overrideVelocityCurve && jetPerformanceProfile == 1)   // Turbojet
                {
                    velCurve.Add(0, 0.50f + _jetTechBonusPercentage);
                    velCurve.Add(1, 1.00f);
                    velCurve.Add(2, 0.75f + _jetTechBonusPercentage);
                    velCurve.Add(3, 0.50f + _jetTechBonusPercentage);
                    velCurve.Add(4, 0.25f + _jetTechBonusPercentage);
                    velCurve.Add(5, 0.00f + _jetTechBonusPercentage);
                    velCurve.Add(7, 0.00f);
                }
                else
                    velCurve = originalVelocityCurve;

                if (overrideAtmosphereCurve && jetPerformanceProfile == 0)
                {
                    atmosphereCurve.Add(0, Mathf.Min((float)_maxISP * 5f / 4f, maxThermalNozzleIsp));
                    atmosphereCurve.Add(0.15f, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                    atmosphereCurve.Add(0.3f, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                    atmosphereCurve.Add(1, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                }
                else if (overrideAtmosphereCurve && jetPerformanceProfile == 1)
                {
                    atmosphereCurve.Add(0, Mathf.Min((float)_maxISP * 5f / 4f, maxThermalNozzleIsp));
                    atmosphereCurve.Add(0.15f, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                    atmosphereCurve.Add(0.3f, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                    atmosphereCurve.Add(1, Mathf.Min((float)_maxISP, maxThermalNozzleIsp));
                }
                else
                    atmosphereCurve = originalAtmosphereCurve;

                if (overrideAtmCurve && jetPerformanceProfile == 0)
                {
                    atmCurve.Add(0, 0);
                    atmCurve.Add(0.01f, (float)Math.Min(1, 0.20 + 0.20 * performance_bonus));
                    atmCurve.Add(0.04f, (float)Math.Min(1, 0.50 + 0.15 * performance_bonus));
                    atmCurve.Add(0.16f, (float)Math.Min(1, 0.75 + 0.10 * performance_bonus));
                    atmCurve.Add(0.50f, (float)Math.Min(1, 0.90 + 0.05 * performance_bonus));
                    atmCurve.Add(1, 1);
                }
                else if (overrideAtmCurve && jetPerformanceProfile == 1)
                {
                    atmCurve.Add(0, 0);
                    atmCurve.Add(0.01f, (float)Math.Min(1, 0.10 + 0.10 * performance_bonus));
                    atmCurve.Add(0.04f, (float)Math.Min(1, 0.25 + 0.10 * performance_bonus));
                    atmCurve.Add(0.16f, (float)Math.Min(1, 0.50 + 0.10 * performance_bonus));
                    atmCurve.Add(0.50f, (float)Math.Min(1, 0.80 + 0.10 * performance_bonus));
                    atmCurve.Add(1, 1);
                }
                else
                    atmCurve = originalAtmCurve;

                myAttachedEngine.atmCurve = atmCurve;
                myAttachedEngine.velCurve = velCurve;
                myAttachedEngine.engineAccelerationSpeed = effectiveJetengineAccelerationSpeed;
                myAttachedEngine.engineDecelerationSpeed = effectiveJetengineDecelerationSpeed;

                myAttachedEngine.useAtmCurve = true;
                myAttachedEngine.useVelCurve = true;

                if (AttachedReactor != null)
                    useEngineResponseTime = AttachedReactor.ReactorSpeedMult > 0;

                myAttachedEngine.useEngineResponseTime = useEngineResponseTime;
            }


            myAttachedEngine.atmosphereCurve = atmosphereCurve;
        }

        public double GetNozzleFlowRate()
        {
            return myAttachedEngine.isOperational ? max_fuel_flow_rate : 0;
        }

        public void EstimateEditorPerformance()
        {
            var atmospherecurve = new FloatCurve();

            if (AttachedReactor != null)
            {
                UpdateMaxIsp();

                if (_maxISP <= 0)
                    return;

                var base_max_thrust = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumPower / _maxISP / GameConstants.STANDARD_GRAVITY * GetHeatExchangerThrustMultiplier();
                var max_thrust_in_space = base_max_thrust;
                base_max_thrust *= _thrustPropellantMultiplier;

                final_max_thrust_in_space = base_max_thrust;

                maxFuelFlowOnEngine = (float)Math.Max(base_max_thrust / (GameConstants.STANDARD_GRAVITY * _maxISP), 1e-10);
                myAttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;

                maxThrustOnEngine = (float)Math.Max(base_max_thrust, minimumThrust);
                myAttachedEngine.maxThrust = maxThrustOnEngine;

                var max_thrust_in_current_atmosphere = max_thrust_in_space;

                UpdateAtmosphericPresureTreshold();

                // update engine thrust/ISP for thermal noozle
                if (!_currentpropellant_is_jet)
                {
                    max_thrust_in_current_atmosphere = Math.Max(max_thrust_in_space - pressureThreshold, minimumThrust);

                    var thrustAtmosphereRatio = max_thrust_in_space > 0 ? Math.Max(max_thrust_in_current_atmosphere / max_thrust_in_space, 0.01) : 0.01;
                    _minISP = _maxISP * thrustAtmosphereRatio;
                }
                else
                    _minISP = _maxISP;

                atmospherecurve.Add(0, (float)_maxISP, 0, 0);
                atmospherecurve.Add(1, (float)_minISP, 0, 0);

                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
            else
            {
                atmospherecurve.Add(0, (float)minimumThrust, 0, 0);
                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
        }

        public void FixedUpdate() // FixedUpdate is also called while not staged
        {
            if (!HighLogic.LoadedSceneIsFlight || myAttachedEngine == null) return;

            try
            {
                ConfigEffects();

                if (myAttachedEngine.currentThrottle > 0)
                {
                    if (!exhaustAllowed)
                    {
                        string message = AttachedReactor.MayExhaustInLowSpaceHomeworld 
                            ? "Engine halted - Radioactive exhaust not allowed towards or inside homeworld atmosphere" 
                            : "Engine halted - Radioactive exhaust not allowed towards or near homeworld atmosphere";

                        ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                        vessel.ctrlState.mainThrottle = 0;

                        // Return to realtime
                        if (vessel.packed)
                            TimeWarp.SetRate(0, true);
                    }
                }

                requestedThrottle = myAttachedEngine.requestedThrottle;

                previousThrottle = currentThrottle;
                currentThrottle = (double)(decimal)myAttachedEngine.currentThrottle;

                if (minThrottle > 0 && requestedThrottle > 0 && AttachedReactor.ReactorSpeedMult > 0)
                {
                    previousDelayedThrottle = delayedThrottle;
                    delayedThrottle = Math.Min(delayedThrottle + timeWarpFixedDeltaTime * myAttachedEngine.engineAccelerationSpeed, minThrottle);
                }
                else if (minThrottle > 0 &&  requestedThrottle == 0 && AttachedReactor.ReactorSpeedMult > 0)
                {
                    delayedThrottle = Math.Max(delayedThrottle - timeWarpFixedDeltaTime * myAttachedEngine.engineAccelerationSpeed, 0);
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
                    if (myAttachedEngine.isOperational && currentThrottle > 0)
                    {
                        //myAttachedEngine.Events["Shutdown"].Invoke();
                        myAttachedEngine.Shutdown();
                        ScreenMessages.PostScreenMessage("Engine Shutdown: No reactor attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    myAttachedEngine.CLAMP = 0;
                    myAttachedEngine.flameoutBar = float.MaxValue;
                    vessel.ctrlState.mainThrottle = 0;
                    maxFuelFlowOnEngine = 1e-10f;
                    myAttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;
                    return;
                }

                if (_myAttachedReactor.Part != this.part)
                {
                    resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                    resourceBuffers.UpdateBuffers();
                }

                // attach/detach with radius
                if (myAttachedEngine.isOperational)
                    AttachedReactor.AttachThermalReciever(id, scaledRadius);
                else
                    AttachedReactor.DetachThermalReciever(id);

                bool canUseChargedPower = this.allowUseOfChargedPower && AttachedReactor.ChargedPowerRatio > 0;

                effectiveThrustFraction = GetHeatExchangerThrustMultiplier();

                effectiveThermalSupply = UseChargedPowerOnly == false ? effectiveThrustFraction * getAvailableStableSupply(ResourceManager.FNRESOURCE_THERMALPOWER) : 0;
                effectiveChargedSupply = canUseChargedPower == true ? effectiveThrustFraction * getAvailableStableSupply(ResourceManager.FNRESOURCE_CHARGED_PARTICLES) : 0;

                maximumPowerUsageForPropulsionRatio = UsePlasmaPower
                    ? AttachedReactor.PlasmaPropulsionEfficiency
                    : AttachedReactor.ThermalPropulsionEfficiency;

                maximumThermalPower = AttachedReactor.MaximumThermalPower;
                maximumChargedPower = AttachedReactor.MaximumChargedPower;

                currentMaxThermalPower = Math.Min(effectiveThermalSupply, effectiveThrustFraction * maximumThermalPower * maximumPowerUsageForPropulsionRatio * adjustedThrottle);
                currentMaxChargedPower = Math.Min(effectiveChargedSupply, effectiveThrustFraction * maximumChargedPower * maximumPowerUsageForPropulsionRatio * adjustedThrottle);

                thermalResourceRatio = (double)getResourceBarFraction(ResourceManager.FNRESOURCE_THERMALPOWER);
                chargedResourceRatio = (double)getResourceBarFraction(ResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                availableThermalPower = exhaustAllowed ? currentMaxThermalPower * (thermalResourceRatio > 0.5 ? 1 : thermalResourceRatio * 2) : 0;
                availableChargedPower = exhaustAllowed ? currentMaxChargedPower * (chargedResourceRatio > 0.5 ? 1 : chargedResourceRatio * 2) : 0;

                UpdateAnimation();

                if (myAttachedEngine.getIgnitionState && myAttachedEngine.status == _flameoutText)
                {
                    myAttachedEngine.Shutdown();
                    ScreenMessages.PostScreenMessage("Engine Shutdown: fuel missing", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                isOpenCycleCooler = (!isPlasmaNozzle || UseThermalAndChargdPower) && !CheatOptions.IgnoreMaxTemperature;

                // when in jet mode apply extra cooling from intake air
                if (isOpenCycleCooler && isJet && part.atmDensity > 0)
                {
                    var wasteheatRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);
                    airFlowForCooling = max_fuel_flow_rate * part.GetResourceRatio("IntakeAir");
                    consumeFNResourcePerSecond(40 * wasteheatRatio * wasteheatRatio * airFlowForCooling, ResourceManager.FNRESOURCE_WASTEHEAT);
                }

                // flameout when reactor cannot produce power
                myAttachedEngine.flameoutBar = AttachedReactor.CanProducePower ? 0 : float.MaxValue;

                if (myAttachedEngine.getIgnitionState && currentThrottle > 0)
                    GenerateThrustFromReactorHeat();
                else
                {
                    _engineWasInactivePreviousFrame = true;

                    UpdateMaxIsp();

                    UpdateIspEngineParams();

                    expectedMaxThrust = AttachedReactor.MaximumPower * maximumPowerUsageForPropulsionRatio * GetPowerThrustModifier() * GetHeatThrustModifier() / GameConstants.STANDARD_GRAVITY / _maxISP * GetHeatExchangerThrustMultiplier();
                    calculatedMaxThrust = expectedMaxThrust;

                    var sootMult = CheatOptions.UnbreakableJoints ? 1 : 1 - sootAccumulationPercentage / 200;

                    expectedMaxThrust *= _thrustPropellantMultiplier * sootMult;

                    max_fuel_flow_rate = expectedMaxThrust / _maxISP / GameConstants.STANDARD_GRAVITY;

                    UpdateAtmosphericPresureTreshold();

                    var thrustAtmosphereRatio = expectedMaxThrust <= 0 ? 0 : Math.Max(0, expectedMaxThrust - pressureThreshold) / expectedMaxThrust;

                    current_isp = _maxISP * thrustAtmosphereRatio;

                    calculatedMaxThrust = Math.Max((calculatedMaxThrust - pressureThreshold), minimumThrust);

                    var sootModifier = CheatOptions.UnbreakableJoints ? 1 : sootHeatDivider > 0 ? 1 - (sootAccumulationPercentage / sootThrustDivider) : 1;

                    calculatedMaxThrust *= _thrustPropellantMultiplier * sootModifier;

                    var effectiveIsp = isJet ? Math.Min(current_isp, maxThermalNozzleIsp) : current_isp;

                    var newIsp = new FloatCurve();
                    newIsp.Add(0, (float)effectiveIsp, 0, 0);
                    myAttachedEngine.atmosphereCurve = newIsp;

                    if (myAttachedEngine.useVelCurve)
                    {
                        vcurveAtCurrentVelocity = myAttachedEngine.velCurve.Evaluate((float)vessel.srf_velocity.magnitude);

                        if (vcurveAtCurrentVelocity > 0 && !float.IsNaN(vcurveAtCurrentVelocity) && !float.IsInfinity(vcurveAtCurrentVelocity))
                            calculatedMaxThrust *= vcurveAtCurrentVelocity;
                        else
                        {
                            max_fuel_flow_rate = 0;
                            calculatedMaxThrust = 0;
                        }
                    }

                    // prevent too low number of maxthrust 
                    if (calculatedMaxThrust <= minimumThrust)
                    {
                        calculatedMaxThrust = minimumThrust;
                        max_fuel_flow_rate = 0;
                    }

                    attachedReactorFuelRato = AttachedReactor.FuelRato;

                    // set engines maximum fuel flow
                    if (IsPositiveValidNumber(max_fuel_flow_rate) && IsPositiveValidNumber(attachedReactorFuelRato))
                        maxFuelFlowOnEngine = (float)Math.Max(max_fuel_flow_rate * AttachedReactor.FuelRato * attachedReactorFuelRato, 1e-10);
                    else
                        maxFuelFlowOnEngine = 1e-10f;

                    myAttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;

                    // set heat production to 1 to prevent heat spike at activation
                    myAttachedEngine.heatProduction = 1;

                    if (pulseDuration == 0 && myAttachedEngine is ModuleEnginesFX)
                    {
                        powerEffectRatio = 0;
                        runningEffectRatio = 0;

                        if (!String.IsNullOrEmpty(_powerEffectNameParticleFX))
                            part.Effect(_powerEffectNameParticleFX, powerEffectRatio, -1);
                        if (!String.IsNullOrEmpty(_runningEffectNameParticleFX))
                            part.Effect(_runningEffectNameParticleFX, runningEffectRatio, -1);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Error FixedUpdate " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void UpdateAtmosphericPresureTreshold()
        {
            if (!_currentpropellant_is_jet)
            {
                var staticPresure = HighLogic.LoadedSceneIsFlight
                    ? FlightGlobals.getStaticPressure(vessel.transform.position)
                    : GameConstants.EarthAtmospherePressureAtSeaLevel;

                pressureThreshold = scaledExitArea * staticPresure;
            }
            else
                pressureThreshold = 0;
        }

        private void UpdateAnimation()
        {
            try
            {
                float increase;

                if (myAttachedEngine.currentThrottle > 0 && calculatedMaxThrust > 0)
                    increase = TimeWarp.fixedDeltaTime;
                else if (_currentAnimatioRatio > 1 / recoveryAnimationDivider)
                    increase = TimeWarp.fixedDeltaTime;
                else if (_currentAnimatioRatio > 0)
                    increase = TimeWarp.fixedDeltaTime / -recoveryAnimationDivider;
                else
                    increase = 0;

                _currentAnimatioRatio += increase;

                if (pulseDuration > 0 && myAttachedEngine is ModuleEnginesFX)
                {
                    if (!String.IsNullOrEmpty(_powerEffectNameParticleFX))
                    {
                        powerEffectRatio = increase > 0 && calculatedMaxThrust > 0 && myAttachedEngine.currentThrottle > 0 && _currentAnimatioRatio < pulseDuration 
                            ? 1 - _currentAnimatioRatio / pulseDuration 
                            : 0;

                        part.Effect(_powerEffectNameParticleFX, powerEffectRatio, -1);
                    }

                    if (!String.IsNullOrEmpty(_runningEffectNameParticleFX))
                    {
                        runningEffectRatio = increase > 0 && calculatedMaxThrust > 0 && myAttachedEngine.currentThrottle > 0 && _currentAnimatioRatio < pulseDuration 
                            ? 1 - _currentAnimatioRatio / pulseDuration 
                            : 0;

                        part.Effect(_runningEffectNameParticleFX, runningEffectRatio, -1);
                    }
                    
                }

                if (pulseDuration > 0 && calculatedMaxThrust > 0 && increase > 0 && myAttachedEngine.currentThrottle > 0 && _currentAnimatioRatio < pulseDuration)
                    PluginHelper.SetAnimationRatio(1, emiAnimationState);
                else
                    PluginHelper.SetAnimationRatio(0, emiAnimationState);

                if (_currentAnimatioRatio > 1 + (2 - (myAttachedEngine.currentThrottle * 2)))
                    _currentAnimatioRatio = 0;

                PluginHelper.SetAnimationRatio(Math.Max(Math.Min(_currentAnimatioRatio, 1), 0), pulseAnimationState);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Error UpdateAnimation " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void GenerateThrustFromReactorHeat()
        {
            try
            {
                if (!AttachedReactor.IsActive)
                    AttachedReactor.EnableIfPossible();

                GetMaximumIspAndThrustMultiplier();

                // consume power when plasma nozzle
                if (requiredMegajouleRatio > 0)
                {
                    var requested_megajoules = (availableThermalPower + availableChargedPower) * requiredMegajouleRatio * AttachedReactor.MagneticNozzlePowerMult;
                    var received_megajoules = consumeFNResourcePerSecond(requested_megajoules, ResourceManager.FNRESOURCE_MEGAJOULES);
                    received_megajoules_ratio = requested_megajoules > 0 ? received_megajoules / requested_megajoules : 0;

                    received_megajoules_percentage = received_megajoules_ratio * 100;
                }
                else
                    received_megajoules_ratio = 1;

                requested_thermal_power = received_megajoules_ratio * availableThermalPower;

                reactor_power_received = consumeFNResourcePerSecond((double)requested_thermal_power, ResourceManager.FNRESOURCE_THERMALPOWER);

                if (currentMaxChargedPower > 0)
                {
                    requested_charge_particles = received_megajoules_ratio * availableChargedPower;
                    reactor_power_received += consumeFNResourcePerSecond((double)requested_charge_particles, ResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                }

                // shutdown engine when connected heatsource cannot produce power
                if (!AttachedReactor.CanProducePower)
                    ScreenMessages.PostScreenMessage("no power produced by thermal source!", 0.02f, ScreenMessageStyle.UPPER_CENTER);

                UpdateSootAccumulation();

                // consume wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    var sootModifier = CheatOptions.UnbreakableJoints
                        ? 1
                        : sootHeatDivider > 0
                            ? 1 - (sootAccumulationPercentage / sootHeatDivider)
                            : 1;

                    var baseWasteheatEfficiency = isPlasmaNozzle ? wasteheatEfficiencyHighTemperature : wasteheatEfficiencyLowTemperature;

                    var reactorWasteheatModifier = isPlasmaNozzle ? AttachedReactor.PlasmaWasteheatProductionMult : AttachedReactor.EngineWasteheatProductionMult;

                    var wasteheatEfficiencyModifier = (1 - baseWasteheatEfficiency) * reactorWasteheatModifier;
                    if (_fuelCoolingFactor > 0)
                        wasteheatEfficiencyModifier /= _fuelCoolingFactor;

                    consumeFNResourcePerSecond(sootModifier * (1 - wasteheatEfficiencyModifier) * reactor_power_received, ResourceManager.FNRESOURCE_WASTEHEAT);
                }

                if (reactor_power_received > 0 && _maxISP > 0)
                {
                    if (_engineWasInactivePreviousFrame)
                    {
                        current_isp = _maxISP * 0.01;
                        _engineWasInactivePreviousFrame = false;
                    }

                    var ispRatio = _currentpropellant_is_jet ? current_isp / _maxISP : 1;

                    powerHeatModifier = received_megajoules_ratio * GetPowerThrustModifier() * GetHeatThrustModifier();

                    engineMaxThrust = powerHeatModifier * reactor_power_received / _maxISP / GameConstants.STANDARD_GRAVITY;

                    thrustPerMegaJoule = powerHeatModifier * maximumPowerUsageForPropulsionRatio / _maxISP / GameConstants.STANDARD_GRAVITY * ispRatio;

                    expectedMaxThrust = thrustPerMegaJoule * AttachedReactor.MaximumPower * effectiveThrustFraction;

                    final_max_thrust_in_space = Math.Max(thrustPerMegaJoule * AttachedReactor.RawMaximumPower * effectiveThrustFraction, minimumThrust);

                    myAttachedEngine.maxThrust = (float)final_max_thrust_in_space;

                    calculatedMaxThrust = expectedMaxThrust;
                }
                else
                {
                    calculatedMaxThrust = 0;
                    expectedMaxThrust = 0;
                }

                max_thrust_in_space = engineMaxThrust;

                max_thrust_in_current_atmosphere = max_thrust_in_space;

                UpdateAtmosphericPresureTreshold();

                // update engine thrust/ISP for thermal noozle
                if (!_currentpropellant_is_jet)
                {
                    max_thrust_in_current_atmosphere = Math.Max(max_thrust_in_space - pressureThreshold, 1e-10);

                    var atmosphereThrustEfficiency = max_thrust_in_space > 0 ? Math.Min(1, max_thrust_in_current_atmosphere / max_thrust_in_space) : 0;

                    var thrustAtmosphereRatio = max_thrust_in_space > 0 ? Math.Max(atmosphereThrustEfficiency, 0.01) : 0.01;
                    
                    UpdateIspEngineParams(thrustAtmosphereRatio, 1 - missingPrecoolerRatio);
                    current_isp = _maxISP * thrustAtmosphereRatio;
                    calculatedMaxThrust = calculatedMaxThrust * atmosphereThrustEfficiency;
                }
                else
                    current_isp = _maxISP;

                if (!Double.IsInfinity(max_thrust_in_current_atmosphere) && !double.IsNaN(max_thrust_in_current_atmosphere))
                {
                    var sootMult = CheatOptions.UnbreakableJoints ? 1 : 1f - sootAccumulationPercentage / sootThrustDivider;
                    final_max_engine_thrust = max_thrust_in_current_atmosphere * _thrustPropellantMultiplier * sootMult;
                    calculatedMaxThrust *= _thrustPropellantMultiplier * sootMult;
                }
                else
                {
                    final_max_engine_thrust = 1e-10;
                    calculatedMaxThrust = final_max_engine_thrust;
                }

                // amount of fuel being used at max throttle with no atmospheric limits
                if (_maxISP <= 0) return;

                var max_thrust_for_fuel_flow = final_max_engine_thrust > 0.0001 ? calculatedMaxThrust : final_max_engine_thrust;

                // calculate maximum fuel flow rate
                max_fuel_flow_rate = max_thrust_for_fuel_flow / current_isp / GameConstants.STANDARD_GRAVITY;

                if (myAttachedEngine.useVelCurve && myAttachedEngine.velCurve != null)
                {
                    vcurveAtCurrentVelocity = myAttachedEngine.velCurve.Evaluate((float)(vessel.speed / vessel.speedOfSound));

                    if (vcurveAtCurrentVelocity > 0 && !float.IsNaN(vcurveAtCurrentVelocity) && !float.IsInfinity(vcurveAtCurrentVelocity))
                        calculatedMaxThrust *= vcurveAtCurrentVelocity;
                    else
                    {
                        max_fuel_flow_rate = 1e-10;
                        calculatedMaxThrust = 0;
                    }
                }

                if (myAttachedEngine.useAtmCurve && myAttachedEngine.atmCurve != null)
                {
                    atmosphereModifier = myAttachedEngine.atmCurve.Evaluate((float)vessel.atmDensity);

                    if (atmosphereModifier > 0 && !float.IsNaN(atmosphereModifier) && !float.IsInfinity(atmosphereModifier))
                    {
                        max_fuel_flow_rate = Math.Max(max_fuel_flow_rate * atmosphereModifier, 1e-10);
                        calculatedMaxThrust *= atmosphereModifier;
                    }
                    else
                    {
                        max_fuel_flow_rate = 1e-10;
                        calculatedMaxThrust = 0;
                    }
                }

                if (calculatedMaxThrust <= minimumThrust || double.IsNaN(calculatedMaxThrust) || double.IsInfinity(calculatedMaxThrust))
                {
                    calculatedMaxThrust = minimumThrust;
                    max_fuel_flow_rate = 1e-10;
                }

                fuelflow_throtle_modifier = _currentpropellant_is_jet && calculatedMaxThrust > 0 && final_max_thrust_in_space > 0 ? calculatedMaxThrust / final_max_thrust_in_space : 1;

                // set engines maximum fuel flow
                if (IsPositiveValidNumber(max_fuel_flow_rate) && IsPositiveValidNumber(adjustedFuelFlowMult) && IsPositiveValidNumber(AttachedReactor.FuelRato))
                    maxFuelFlowOnEngine = (float)Math.Max(max_fuel_flow_rate * adjustedFuelFlowMult * AttachedReactor.FuelRato * AttachedReactor.FuelRato, 1e-10);
                else
                    maxFuelFlowOnEngine = 1e-10f;
                myAttachedEngine.maxFuelFlow = maxFuelFlowOnEngine;

                CalculateMissingPreCoolerRatio();

                airflowHeatModifier = missingPrecoolerRatio > 0 ? Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20 / GameConstants.atmospheric_non_precooled_limit * missingPrecoolerRatio), 0): 0;
                airflowHeatModifier *= vessel.atmDensity * (vessel.speed / vessel.speedOfSound);

                if (airflowHeatModifier.IsInfinityOrNaN())
                    airflowHeatModifier = 0;

                maxThrustOnEngine = myAttachedEngine.maxThrust;
                realIspEngine = myAttachedEngine.realIsp;
                currentMassFlow = myAttachedEngine.fuelFlowGui * myAttachedEngine.mixtureDensity;

                // act as open cycle cooler
                if (isOpenCycleCooler)
                {
                    var wasteheatRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);
                    fuelFlowForCooling = currentMassFlow;
                    consumeFNResourcePerSecond(40 * wasteheatRatio * wasteheatRatio * fuelFlowForCooling, ResourceManager.FNRESOURCE_WASTEHEAT);
                }

                // give back propellant
                if (UseChargedPowerOnly && list_of_propellants.Count == 1)
                {
                    var resource = PartResourceLibrary.Instance.GetDefinition(list_of_propellants.First().name);
                    AttachedReactor.UseProductForPropulsion(1, currentMassFlow, resource);
                }

                if (controlHeatProduction)
                {
                    ispHeatModifier =  Math.Sqrt(realIspEngine) * (UsePlasmaPower ? plasmaAfterburnerHeatModifier : thermalHeatModifier);
                    powerToMass = part.mass > 0 ? Math.Sqrt(maxThrustOnEngine / part.mass) : 0;
                    radiusHeatModifier = Math.Pow(scaledRadius * radiusHeatProductionMult, radiusHeatProductionExponent);
                    engineHeatProductionMult = AttachedReactor.EngineHeatProductionMult;
                    reactorHeatModifier = isPlasmaNozzle ? AttachedReactor.PlasmaHeatProductionMult : AttachedReactor.EngineHeatProductionMult;

                    spaceHeatProduction = heatProductionMultiplier * reactorHeatModifier * AttachedReactor.EngineHeatProductionMult * _ispPropellantMultiplier * ispHeatModifier * radiusHeatModifier * powerToMass / _fuelCoolingFactor;
                    engineHeatProduction = Math.Min(spaceHeatProduction * (1 + airflowHeatModifier * PluginHelper.AirflowHeatMult), 999999);

                    myAttachedEngine.heatProduction = (float)engineHeatProduction;
                }

                if (pulseDuration == 0 && myAttachedEngine is ModuleEnginesFX)
                {
                    var maxEngineFuelFlow = myAttachedEngine.maxThrust > minimumThrust ? myAttachedEngine.maxThrust / myAttachedEngine.realIsp / GameConstants.STANDARD_GRAVITY : 0;

                    if (!String.IsNullOrEmpty(_powerEffectNameParticleFX))
                    {
                        powerEffectRatio = maxEngineFuelFlow > 0 ? (float)(exhaustModifier * Math.Min(myAttachedEngine.currentThrottle, currentMassFlow / maxEngineFuelFlow)) : 0;
                        part.Effect(_powerEffectNameParticleFX, powerEffectRatio, -1);
                    }

                    if (!String.IsNullOrEmpty(_runningEffectNameParticleFX))
                    {
                        runningEffectRatio = maxEngineFuelFlow > 0 ? (float)(exhaustModifier * Math.Min(myAttachedEngine.requestedThrottle, currentMassFlow / maxEngineFuelFlow)) : 0;
                        part.Effect(_runningEffectNameParticleFX, powerEffectRatio, -1);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Error GenerateThrustFromReactorHeat " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void CalculateMissingPreCoolerRatio()
        {
            pre_cooler_area = _vesselPrecoolers.Where(prc => prc.functional).Sum(prc => prc.area);
            intakes_open_area = _vesselResourceIntakes.Where(mre => mre.intakeOpen).Sum(mre => mre.area);

            missingPrecoolerRatio = _currentpropellant_is_jet && intakes_open_area > 0
                ? Math.Min(1,
                    Math.Max(0, Math.Pow((intakes_open_area - pre_cooler_area)/intakes_open_area, missingPrecoolerProportionExponent)))
                : 0;

            if (missingPrecoolerRatio.IsInfinityOrNaN())
                missingPrecoolerRatio = 0;
        }

        private bool IsPositiveValidNumber(double vaiable)
        {
            return !double.IsNaN(vaiable) && !double.IsInfinity(vaiable) && vaiable > 0;
        }

        private void UpdateSootAccumulation()
        {
            if (!CheatOptions.UnbreakableJoints)
                return;

            if (myAttachedEngine.currentThrottle > 0 && _propellantSootFactorFullThrotle != 0 || _propellantSootFactorMinThrotle != 0)
            {
                double sootEffect;

                if (_propellantSootFactorEquilibrium != 0)
                {
                    var ratio = myAttachedEngine.currentThrottle > _propellantSootFactorEquilibrium
                        ? (myAttachedEngine.currentThrottle - _propellantSootFactorEquilibrium) / (1 - _propellantSootFactorEquilibrium)
                        : 1 - (myAttachedEngine.currentThrottle / _propellantSootFactorEquilibrium);

                    var sootMultiplier = myAttachedEngine.currentThrottle < _propellantSootFactorEquilibrium ? 1
                        : _propellantSootFactorFullThrotle > 0 ? _heatDecompositionFraction : 1;

                    sootEffect = myAttachedEngine.currentThrottle > _propellantSootFactorEquilibrium
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
                sootAccumulationPercentage -= TimeWarp.fixedDeltaTime * myAttachedEngine.currentThrottle * 0.1;
                sootAccumulationPercentage = Math.Max(0, sootAccumulationPercentage);
            }
        }

        private void GetMaximumIspAndThrustMultiplier()
        {
            // get the flameout safety limit
            if (_currentpropellant_is_jet)
            {
                UpdateIspEngineParams();
                this.current_isp = myAttachedEngine.atmosphereCurve.Evaluate((float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position), 1.0));
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

            baseMaxIsp = Math.Sqrt(AttachedReactor.CoreTemperature) * EffectiveCoreTempIspMult;

            if (IsPositiveValidNumber(AttachedReactor.FuelRato))
                baseMaxIsp *= AttachedReactor.FuelRato;

            if (baseMaxIsp > maxThermalNozzleIsp && !isPlasmaNozzle)
                baseMaxIsp = maxThermalNozzleIsp;

            _fuelflowThrottleMaxValue = minimumBaseIsp > 0 ? 100 * Math.Max(1, baseMaxIsp / Math.Min(baseMaxIsp, minimumBaseIsp)) : 100;

            if (fuelflowThrottleField != null)
            {
                fuelflowThrottleField.guiActiveEditor = minimumBaseIsp > 0;
                fuelflowThrottleField.guiActive = minimumBaseIsp > 0;
            }

            if (fuelflowThrottleFloatRangeEditor != null)
                fuelflowThrottleFloatRangeEditor.maxValue = (float)_fuelflowThrottleMaxValue;
            if (fuelflowThrottleFloatRangeFlight != null)
                fuelflowThrottleFloatRangeFlight.maxValue = (float)_fuelflowThrottleMaxValue;

            if (UseThermalPowerOnly)
            {
                _maxISP = isPlasmaNozzle 
                    ? baseMaxIsp + AttachedReactor.ChargedPowerRatio * baseMaxIsp 
                    : baseMaxIsp;
            }
            else if (UseChargedPowerOnly)
            {
                var joules_per_amu = AttachedReactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / GameConstants.dilution_factor;
                _maxISP = 100 * Math.Sqrt(joules_per_amu * 2 / GameConstants.ATOMIC_MASS_UNIT) / GameConstants.STANDARD_GRAVITY;
            }
            else
            {
                var scaledChargedRatio = 0.2 + Math.Pow((Math.Max(0, AttachedReactor.ChargedPowerRatio - 0.2) * 1.25), 2);
                _maxISP = scaledChargedRatio * baseMaxIsp + (1 - scaledChargedRatio) * maxThermalNozzleIsp;

                if (UsePlasmaAfterBurner)  // when  mixing charged particles from reactor with cold propellant
                    _maxISP = _maxISP + Math.Pow((double)(decimal)ispThrottle / 100, 2) * plasmaAfterburnerRange * baseMaxIsp;
            }

            var effectiveFuelflowThrottle = Math.Min(_fuelflowThrottleMaxValue, (double)(decimal)fuelflowThrottle);

            fuelflowMultplier = Math.Min(Math.Max(100, _fuelflowThrottleMaxValue * _ispPropellantMultiplier), effectiveFuelflowThrottle) / 100;

            var fueFlowDivider = fuelflowMultplier > 0 ? 1 / fuelflowMultplier : 0;

            var ispFlowMultiplier = _ispPropellantMultiplier * fueFlowDivider;

            _maxISP *= ispFlowMultiplier;

            exhaustModifier = Math.Min(_fuelflowThrottleMaxValue, fueFlowDivider * 100) / _fuelflowThrottleMaxValue;
        }

        public override string GetInfo()
        {
            var upgraded = this.HasTechsRequiredToUpgrade();

            var propNodes = upgraded && isJet ? getPropellantsHybrid() : getPropellants(isJet);

            var returnStr = "Thrust: Variable\n";
            foreach (var propellantNode in propNodes)
            {
                var ispMultiplier = float.Parse(propellantNode.GetValue("ispMultiplier"));
                var guiname = propellantNode.GetValue("guiName");
                returnStr = returnStr + "--" + guiname + "--\n" + "ISP: " + ispMultiplier.ToString("0.000") + " x " + (PluginHelper.IspCoreTempMult + IspTempMultOffset).ToString("0.000") + " x Sqrt(Core Temperature)" + "\n";
            }
            return returnStr;
        }

        public override int getPowerPriority()
        {
            return 5;
        }

        public static ConfigNode[] getPropellants(bool isJet)
        {
            //Debug.Log("[KSPI]: ThermalNozzleController - getPropellants");

            ConfigNode[] propellantlist = isJet
                ? GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT")
                : GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");

            if (propellantlist == null)
                PluginHelper.showInstallationErrorMessage();

            return propellantlist;
        }

        private double GetHeatThrustModifier()
        {
            var coretempthreshold = PluginHelper.ThrustCoreTempThreshold;
            var lowcoretempbase = PluginHelper.LowCoreTempBaseThrust;

            return coretempthreshold <= 0
                ? 1.0
                : AttachedReactor.CoreTemperature < coretempthreshold
                    ? (AttachedReactor.CoreTemperature + lowcoretempbase) / (coretempthreshold + lowcoretempbase)
                    : 1.0 + PluginHelper.HighCoreTempThrustMult * Math.Max(Math.Log10(AttachedReactor.CoreTemperature / coretempthreshold), 0);
        }

        private float CurrentPowerThrustMultiplier
        {
            get
            {
                return _currentpropellant_is_jet
                    ? powerTrustMultiplierJet
                    : powerTrustMultiplier;
            }
        }

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginHelper.GlobalThermalNozzlePowerMaxThrustMult * CurrentPowerThrustMultiplier;
        }

        private void UpdateRadiusModifier()
        {
            if (_myAttachedReactor != null)
            {
                // re-attach with updated radius
                _myAttachedReactor.DetachThermalReciever(id);
                _myAttachedReactor.AttachThermalReciever(id, scaledRadius);

                Fields["vacuumPerformance"].guiActiveEditor = true;
                Fields["radiusModifier"].guiActiveEditor = true;
                Fields["surfacePerformance"].guiActiveEditor = true;

                effectiveThrustFraction = GetHeatExchangerThrustMultiplier();

                radiusModifier = (effectiveThrustFraction * 100.0).ToString("0.00") + "%";

                UpdateMaxIsp();

                maximumReactorPower = AttachedReactor.MaximumPower;

                if (maximumReactorPower == 0)
                    Debug.LogWarning("[KSPI]: ThermalNozzleController.UpdateRadiusModifier maximumReactorPower == 0");

                heatThrustModifier = GetHeatThrustModifier();
                powerThrustModifier = GetPowerThrustModifier();

                max_thrust_in_space = powerThrustModifier * heatThrustModifier * maximumReactorPower / _maxISP / GameConstants.STANDARD_GRAVITY * effectiveThrustFraction;
                final_max_thrust_in_space = Math.Max(max_thrust_in_space * _thrustPropellantMultiplier, minimumThrust);

                // Set max thrust
                myAttachedEngine.maxThrust = (float)final_max_thrust_in_space;

                var isp_in_space = _maxISP;

                vacuumPerformance = final_max_thrust_in_space.ToString("0.0") + "kN @ " + isp_in_space.ToString("0.0") + "s";

                maxPressureThresholdAtKerbinSurface = scaledExitArea * GameConstants.EarthAtmospherePressureAtSeaLevel;

                var maxSurfaceThrust = Math.Max(max_thrust_in_space - (maxPressureThresholdAtKerbinSurface), minimumThrust);
                var maxSurfaceISP = _maxISP * (maxSurfaceThrust / max_thrust_in_space);
                var final_max_surface_thrust = maxSurfaceThrust * _thrustPropellantMultiplier;

                surfacePerformance = final_max_surface_thrust.ToString("0.0") + "kN @ " + maxSurfaceISP.ToString("0.0") + "s";
            }
            else
            {
                //Debug.LogWarning("[KSPI]: ThermalNozzleController.UpdateRadiusModifier _myAttachedReactor == null");
                Fields["vacuumPerformance"].guiActiveEditor = false;
                Fields["radiusModifier"].guiActiveEditor = false;
                Fields["surfacePerformance"].guiActiveEditor = false;
            }
        }

        private double GetHeatExchangerThrustMultiplier()
        {
            if (AttachedReactor == null || AttachedReactor.Radius == 0 || scaledRadius == 0) return 0;

            var currentFraction = _myAttachedReactor.GetFractionThermalReciever(id);

            if (currentFraction == 0) return storedFractionThermalReciever;

            // scale down thrust if it's attached to a larger sized reactor
            var  heatExchangeRatio = scaledRadius >= AttachedReactor.Radius ? 1
                : scaledRadius * scaledRadius / AttachedReactor.Radius / AttachedReactor.Radius;

            storedFractionThermalReciever = Math.Min(currentFraction, heatExchangeRatio);

            return storedFractionThermalReciever;
        }

        private static ConfigNode[] getPropellantsHybrid()
        {
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT");
            ConfigNode[] propellantlist2 = GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");
            propellantlist = propellantlist.Concat(propellantlist2).ToArray();
            if (propellantlist == null || propellantlist2 == null)
                PluginHelper.showInstallationErrorMessage();

            return propellantlist;
        }

        public override string getResourceManagerDisplayName()
        {
            string displayName = part.partInfo.title + " (nozzle)";

            if (similarParts == null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            if (similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }

        public void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(_windowID, windowPosition, Window, part.partInfo.title);
        }

        private void Window(int windowID)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                {
                    render_window = false;
                }

                GUILayout.BeginVertical();

                foreach (var fuel in _compatibleThermalEngineFuels)
                {
                    if (HighLogic.LoadedSceneIsEditor || fuel.hasAnyStorage())
                    {
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
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ThermalNozzleController Window(" + windowID + "): " + e.Message);
                throw;
            }
        }
    }
}
