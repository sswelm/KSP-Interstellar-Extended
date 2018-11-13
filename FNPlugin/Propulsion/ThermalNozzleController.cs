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

namespace FNPlugin
{
    class ThermalAerospikeController : ThermalNozzleController { }

    class ThermalNozzleController : ResourceSuppliableModule, IFNEngineNoozle, IUpgradeableModule, IRescalable<ThermalNozzleController>
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public double storedAbsoluteFactor = 1;

        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Soot Accumulation", guiUnits = " %", guiFormat = "F4")]
        public double sootAccumulationPercentage;
        [KSPField(isPersistant = true)]
        public bool isDeployed = false;
        [KSPField(isPersistant = true)]
        public double animationStarted = 0;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Use Thermal Power"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool useThermalPower = true;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Isp Throtle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 5f)]
        public float ispThrottle = 100;

        [KSPField]
        public float jetengineAccelerationBaseSpeed = 0.2f;
        [KSPField]
        public float jetengineDecelerationBaseSpeed = 0.4f;
        [KSPField]
        public float engineAccelerationBaseSpeed = 2f;
        [KSPField]
        public float engineDecelerationBaseSpeed = 10f;
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
        public double wasteheatEfficiencyHighTemperature = 0.9;
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
        public bool showPartTemperature = true;
        [KSPField]
        public double baseMaxIsp;
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
        public double radius = 2.5;
        [KSPField]
        public double exitArea = 1;
        [KSPField]
        public double exitAreaScaleExponent = 2;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Radius", guiUnits = " m", guiFormat = "F3")]
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
        public double radiusHeatProductionExponent = 0.3;
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
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Engine Heat Production", guiFormat = "F5")]
        public double engineHeatProduction;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max FuelFlow On Engine")]
        public float maxFuelFlowOnEngine;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Thrust On Engine")]
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
        public double power_received;
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
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Exchange Divider")]
        protected double heatExchangerThrustDivisor;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Engine Max Thrust", guiFormat = "F3", guiUnits = " kN")]
        protected double engineMaxThrust;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Thrust Per MJ", guiFormat = "F3", guiUnits = " kN")]
        protected double thrustPerMegaJoule;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Thrust In Space")]
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
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Max Fuel Flow", guiFormat = "F5")]
        protected double max_fuel_flow_rate = 0;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Engine Fuel Flow", guiFormat = "F5")]
        protected double currentEngineFuelFlow;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Current Isp", guiFormat = "F3")]
        protected double current_isp = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "MaxPressureThresshold")]
        protected double maxPressureThresholdAtKerbinSurface;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Thermal Ratio")]
        protected double thermalRatio;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Charged Power Ratio")]
        protected double chargedParticleRatio;
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
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Thermal Modifier")]
        protected double thrust_modifiers;
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
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Wasteheat Efficiency Modifier")]
        public double wasteheatEfficiencyModifier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumPowerUsageForPropulsionRatio;
        [KSPField(guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumThermalPower;
        [KSPField(guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double maximumChargedPower;

        [KSPField(guiActive = false)]
        public double powerHeatModifier;
        [KSPField(guiActive = false)]
        public double currentThrottle;
        [KSPField(guiActive = false)]
        public double requestedThrottle;
        [KSPField(guiActive = false)]
        public float effectRatio;

        [KSPField]
        int pre_coolers_active;
        [KSPField]
        int intakes_open;
        [KSPField]
        int total_intakes;
        [KSPField]
        double proportion;
        [KSPField]
        float effectiveJetengineAccelerationSpeed;
        [KSPField]
        float effectiveJetengineDecelerationSpeed;
        [KSPField]
        public int supportedPropellantAtoms = 511;
        [KSPField]
        public int supportedPropellantTypes = 511;


        // Constants
        protected const double _hydroloxDecompositionEnergy = 16.2137;

        //Internal
        protected string _particleFXName;
        protected string _fuelTechRequirement;
        
        protected double _heatDecompositionFraction;

        protected float _fuelCoolingFactor = 1;
        protected float _fuelToxicity;
        protected float _currentAnimatioRatio;
        protected float _minDecompositionTemp;
        protected float _maxDecompositionTemp;
        protected float _originalEngineAccelerationSpeed;
        protected float _originalEngineDecelerationSpeed;
        protected float _jetTechBonus;
        protected float _jetTechBonusPercentage;
        protected float _jetTechBonusCurveChange;

        protected int partDistance = 0;

        protected bool _fuelRequiresUpgrade = false;
        protected bool _engineWasInactivePreviousFrame = false;
        protected bool _hasrequiredupgrade = false;
        protected bool _hasSetupPropellant = false;
        protected bool _currentpropellant_is_jet = false;

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
        protected ConfigNode[] propellantsConfignodes;

        protected List<Propellant> list_of_propellants = new List<Propellant>();
        protected List<FNModulePreecooler> _vesselPrecoolers;
        protected List<ModuleResourceIntake> _vesselResourceIntakes;
        protected List<IFNEngineNoozle> _vesselThermalNozzles;

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

        public double EffectiveCorTempIspMult
        {
            get { return PluginHelper.IspCoreTempMult + IspTempMultOffset; }
        }

        public bool UseThermalPower
        {
            get { return useThermalPower || ispThrottle <= 5; }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next Propellant", active = true)]
        public void NextPropellant()
        {
            fuel_mode++;
            if (fuel_mode >= propellantsConfignodes.Length)
                fuel_mode = 0;

            SetupPropellants(true);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous Propellant", active = true)]
        public void PreviousPropellant()
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = propellantsConfignodes.Length - 1;

            SetupPropellants(false);
        }

        // Note: we assume OnRescale is called at load and after any time tweakscale changes the size of an part
        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            Debug.Log("[KSPI] - ThermalNozzleController OnRescale was called with factor " + factor.absolute.linear);

            storedAbsoluteFactor = (double)(decimal)factor.absolute.linear;

            ScaleParameters();

            // update simulation
            UpdateRadiusModifier();
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
                if (myAttachedEngine != null && myAttachedEngine.isOperational)
                    return myAttachedEngine.currentThrottle;
                else
                    return 0;
            }
        }

        public bool PropellantAbsorbsNeutrons { get { return _isNeutronAbsorber; } }

        public bool RequiresPlasmaHeat { get { return isPlasmaNozzle; } }

        public bool RequiresThermalHeat { get { return !isPlasmaNozzle; } }

        public bool RequiresChargedPower { get { return false; } }

        public void upgradePartModule()
        {
            isupgraded = true;

            if (isJet)
                propellantsConfignodes = getPropellantsHybrid();
            else
                propellantsConfignodes = getPropellants(isJet);
        }

        public ConfigNode[] getPropellants()
        {
            return propellantsConfignodes;
        }

        public void OnEditorAttach()
        {
            ConnectToThermalSource();

            if (AttachedReactor == null) return;

            EstimateEditorPerformance();
        }

        public void OnEditorDetach()
        {
            if (AttachedReactor == null) return;

            AttachedReactor.DisconnectWithEngine(this);

            AttachedReactor.DetachThermalReciever(id);
        }

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("[KSPI] - ThermalNozzleController - start");

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

                Debug.Log("[KSPI] - ThermalNozzleController - setup animation");

                if (!String.IsNullOrEmpty(deployAnimationName))
                    deployAnim = part.FindModelAnimators(deployAnimationName).FirstOrDefault();
                if (!String.IsNullOrEmpty(pulseAnimationName))
                    pulseAnimationState = PluginHelper.SetUpAnimation(pulseAnimationName, this.part);
                if (!String.IsNullOrEmpty(emiAnimationName))
                    emiAnimationState = PluginHelper.SetUpAnimation(emiAnimationName, this.part);

                Debug.Log("[KSPI] - ThermalNozzleController - calculate WasteHeat Capacity");

                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 2.0e+5 * wasteHeatBufferMult, true));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                resourceBuffers.Init(this.part);

                Debug.Log("[KSPI] - ThermalNozzleController - find module implementing <ModuleEngines>");

                myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
                timewarpEngine = this.part.FindModuleImplementing<ModuleEnginesWarp>();

                if (myAttachedEngine != null)
                {
                    originalAtmCurve = myAttachedEngine.atmCurve;
                    originalAtmosphereCurve = myAttachedEngine.atmosphereCurve;
                    originalVelocityCurve = myAttachedEngine.velCurve;

                    _originalEngineAccelerationSpeed = myAttachedEngine.engineAccelerationSpeed;
                    _originalEngineDecelerationSpeed = myAttachedEngine.engineDecelerationSpeed;
                }
                else
                    Debug.LogError("[KSPI] - ThermalNozzleController - failed to find engine!");

                // find attached thermal source
                ConnectToThermalSource();

                maxPressureThresholdAtKerbinSurface = scaledExitArea * GameConstants.EarthAtmospherePressureAtSeaLevel;

                if (state == StartState.Editor)
                {
                    part.OnEditorAttach += OnEditorAttach;
                    part.OnEditorDetach += OnEditorDetach;

                    propellantsConfignodes = getPropellants(isJet);
                    if (this.HasTechsRequiredToUpgrade())
                    {
                        isupgraded = true;
                        upgradePartModule();
                    }
                    SetupPropellants();
                    EstimateEditorPerformance();

                    return;
                }

                UpdateRadiusModifier();

                UpdateIspEngineParams();

                // presearch all avaialble precoolers, intakes and nozzles on the vessel
                _vesselPrecoolers = vessel.FindPartModulesImplementing<FNModulePreecooler>();
                _vesselResourceIntakes = vessel.FindPartModulesImplementing<ModuleResourceIntake>().Where(mre => mre.resourceName == InterstellarResourcesConfiguration.Instance.IntakeAir).ToList();
                _vesselThermalNozzles = vessel.FindPartModulesImplementing<IFNEngineNoozle>();

                // if we can upgrade, let's do so
                if (isupgraded)
                    upgradePartModule();
                else
                {
                    if (this.HasTechsRequiredToUpgrade())
                        _hasrequiredupgrade = true;

                    // if not, use basic propellants
                    propellantsConfignodes = getPropellants(isJet);
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

                var showIspThrotle = isPlasmaNozzle && !AttachedReactor.SupportMHD && !(AttachedReactor.ChargedPowerRatio > 0);
                Fields["temperatureStr"].guiActive = showPartTemperature;
                Fields["ispThrottle"].guiActiveEditor = showIspThrotle;
                Fields["ispThrottle"].guiActive = showIspThrotle;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - OnStart Exception in ThermalNozzleController.OnStart: " + e.Message);
            }

            try
            {
                SetupPropellants();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - OnStart Exception in SetupPropellants" + e.Message);
            }
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

                if (_currentpropellant_is_jet && !String.IsNullOrEmpty(EffectNameJet))
                    _particleFXName = EffectNameJet;
                else if (_propellantIsLFO && !String.IsNullOrEmpty(EffectNameLFO))
                    _particleFXName = EffectNameLFO;
                else if (_isNeutronAbsorber && !String.IsNullOrEmpty(EffectNameLithium))
                    _particleFXName = EffectNameLithium;
                else if (!String.IsNullOrEmpty(EffectNameNonLFO))
                    _particleFXName = EffectNameNonLFO;
            }
        }

        private void ConnectToThermalSource()
        {
            Debug.Log("[KSPI] - ThermalNozzleController - start BreadthFirstSearchForThermalSource");

            var source = PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part, (p) => p.IsThermalSource, 10, 10, 10);

            if (source == null || source.Source == null)
            {
                Debug.LogWarning("[KSPI] - ThermalNozzleController - BreadthFirstSearchForThermalSource-Failed to find thermal source");
                return;
            }

            AttachedReactor = source.Source;
            AttachedReactor.ConnectWithEngine(this);

            partDistance = (int)Math.Max(Math.Ceiling(source.Cost) - 1, 0);
            Debug.Log("[KSPI] - ThermalNozzleController - BreadthFirstSearchForThermalSource- Found thermal searchResult with distance " + partDistance);
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
                    SetupPropellants(true, true);
                }

                temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
                UpdateAtmosphericPresureTreshold();

                Fields["sootAccumulationPercentage"].guiActive = sootAccumulationPercentage > 0;

                thrustIspMultiplier = _ispPropellantMultiplier.ToString("0.0000") + " / " + _thrustPropellantMultiplier.ToString("0.0000");

                if (ResearchAndDevelopment.Instance != null && isJet)
                {
                    Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasrequiredupgrade;
                    upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
                }
                else
                    Events["RetrofitEngine"].active = false;

                Fields["upgradeCostStr"].guiActive = !isupgraded && _hasrequiredupgrade && isJet;

                // only show switch when relevant
                var showCanUseThermalPowerSwitch = this.allowUseOfThermalPower && !AttachedReactor.SupportMHD && !(AttachedReactor.ChargedPowerRatio > 0);
                Fields["useThermalPower"].guiActive = showCanUseThermalPowerSwitch;
                Fields["ispThrottle"].guiActive = useThermalPower && isPlasmaNozzle;

                if (myAttachedEngine == null)
                    return;

                // only allow shutdown when engine throttle is down
                myAttachedEngine.Events["Shutdown"].active = myAttachedEngine.currentThrottle == 0;

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
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - ThermalNozzle OnUpdates " + e.Message);
            }

        }

        public override void OnActive()
        {
            base.OnActive();
            SetupPropellants(true, true);
        }

        public void SetupPropellants(bool forward = true, bool notifySwitching = false)
        {
            if (_myAttachedReactor == null)
                return;

            try
            {
                var chosenpropellant = propellantsConfignodes[fuel_mode];

                UpdatePropellantModeBehavior(chosenpropellant);
                ConfigNode[] propellantNodes = chosenpropellant.GetNodes("PROPELLANT");
                list_of_propellants.Clear();

                foreach (ConfigNode propNode in propellantNodes)
                {
                    var curprop = new ExtendedPropellant();
                    curprop.Load(propNode);

                    if (list_of_propellants == null)
                        Debug.LogWarning("[KSPI] - ThermalNozzleController - SetupPropellants list_of_propellants is null");

                    list_of_propellants.Add(curprop);
                }

                string missingResources = String.Empty;
                bool canLoadPropellant = true;

                if (
                         list_of_propellants.Any(m => PartResourceLibrary.Instance.GetDefinition(m.name) == null) 
                    || (!PluginHelper.HasTechRequirementOrEmpty(_fuelTechRequirement))
                    || (_fuelRequiresUpgrade && !isupgraded)
                    || (_fuelCoolingFactor < AttachedReactor.MinCoolingFactor)
                    || (_propellantIsLFO && !PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))
                    || ((_atomType & _myAttachedReactor.SupportedPropellantAtoms) != _atomType)
                    || ((_atomType & this.supportedPropellantAtoms) != _atomType)
                    || ((_propType & _myAttachedReactor.SupportedPropellantTypes) != _propType)
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
                            part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

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
                    Debug.Log("[KSPI] - ThermalNozzleController - Setup propellant chosenpropellant " + fuel_mode + " / " + propellantsConfignodes.Count());

                    myAttachedEngine.Shutdown();

                    var newPropNode = new ConfigNode();

                    foreach (var prop in list_of_propellants)
                    {
                        var propellantConfigNode = newPropNode.AddNode("PROPELLANT");
                        propellantConfigNode.AddValue("name", prop.name);
                        propellantConfigNode.AddValue("ratio", prop.ratio);
                        propellantConfigNode.AddValue("DrawGauge", "true");
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
                    if (next_propellant && (switches <= propellantsConfignodes.Length || fuel_mode != 0))
                    {// always shows the first fuel mode when all fuel mods are tested at least once
                        ++switches;
                        if (notifySwitching)
                            ScreenMessages.PostScreenMessage("Switching Propellant, missing resource " + missingResources, 5.0f, ScreenMessageStyle.LOWER_CENTER);

                        if (forward)
                            NextPropellant();
                        else
                            PreviousPropellant();
                    }
                }
                else
                {
                    bool next_propellant = false;

                    UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - Setup propellant " + list_of_propellants[0].name);

                    // Still ignore propellants that don't exist or we cannot use due to the limitations of the engine
                    if (!canLoadPropellant && (switches <= propellantsConfignodes.Length || fuel_mode != 0))
                    {
                        if (((_atomType & this.supportedPropellantAtoms) != _atomType))
                            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - Setup propellant nozzle atom " + this.supportedPropellantAtoms + " != " + _atomType);
                        if (((_propType & this.supportedPropellantTypes) != _propType))
                            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - Setup propellant nozzle type " + this.supportedPropellantTypes + " != " + _propType);

                        if (((_atomType & _myAttachedReactor.SupportedPropellantAtoms) != _atomType))
                            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - Setup propellant reactor atom " + _myAttachedReactor.SupportedPropellantAtoms + " != " + _atomType);
                        if (((_propType & _myAttachedReactor.SupportedPropellantTypes) != _propType))
                            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - Setup propellant reactor type " + _myAttachedReactor.SupportedPropellantTypes + " != " + _propType);

                        next_propellant = true;
                    }

                    if (next_propellant)
                    {
                        ++switches;
                        if (forward)
                            NextPropellant();
                        else
                            PreviousPropellant();
                    }

                    EstimateEditorPerformance(); // update editor estimates
                }

                switches = 0;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Error SetupPropellants " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void UpdatePropellantModeBehavior(ConfigNode chosenpropellant)
        {
            _fuelmode = chosenpropellant.GetValue("guiName");
            _propellantIsLFO = chosenpropellant.HasValue("isLFO") ? bool.Parse(chosenpropellant.GetValue("isLFO")) : false;
            _currentpropellant_is_jet = chosenpropellant.HasValue("isJet") ? bool.Parse(chosenpropellant.GetValue("isJet")) : false;

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
            
            _fuelRequiresUpgrade = chosenpropellant.HasValue("RequiresUpgrade") ? Boolean.Parse(chosenpropellant.GetValue("RequiresUpgrade")) : false;
            _atomType = chosenpropellant.HasValue("atomType") ? int.Parse(chosenpropellant.GetValue("atomType")) : 1;
            _propType = chosenpropellant.HasValue("propType") ? int.Parse(chosenpropellant.GetValue("propType")) : 1;
            _isNeutronAbsorber = chosenpropellant.HasValue("isNeutronAbsorber") ? bool.Parse(chosenpropellant.GetValue("isNeutronAbsorber")) : false;

            if (!isPlasmaNozzle && !usePropellantBaseIsp && !_currentpropellant_is_jet && _decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                UpdateThrustPropellantMultiplier();
            else
            {
                _heatDecompositionFraction = 1;

                if ((usePropellantBaseIsp || AttachedReactor .UsePropellantBaseIsp|| isPlasmaNozzle) && _baseIspMultiplier > 0)
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
            if (isPlasmaNozzle && !_isNeutronAbsorber)
            {
                var plasmaEfficiency = Math.Pow(_baseIspMultiplier, 1d/3d);
                _ispPropellantMultiplier *= plasmaEfficiency;
                _thrustPropellantMultiplier *= plasmaEfficiency;
            }
        }

        public void UpdateIspEngineParams(double atmosphere_isp_efficiency = 1)
        {
            // recaculate ISP based on power and core temp available
            FloatCurve atmCurve = new FloatCurve();
            FloatCurve atmosphereIspCurve = new FloatCurve();
            FloatCurve velCurve = new FloatCurve();

            UpdateMaxIsp();

            if (!_currentpropellant_is_jet)
            {
                atmosphereIspCurve.Add(0, (float)_maxISP * (float)atmosphere_isp_efficiency, 0, 0);

                myAttachedEngine.useAtmCurve = false;
                myAttachedEngine.useVelCurve = false;
                myAttachedEngine.useEngineResponseTime = AttachedReactor.ReactorSpeedMult > 0;
                myAttachedEngine.engineAccelerationSpeed = engineAccelerationBaseSpeed * (float)AttachedReactor.ReactorSpeedMult;
                myAttachedEngine.engineDecelerationSpeed = engineDecelerationBaseSpeed * (float)AttachedReactor.ReactorSpeedMult;
            }
            else
            {
                if (overrideVelocityCurve)
                {
                    if (jetPerformanceProfile == 0)
                    {
                        velCurve.Add(0, 0.05f + _jetTechBonusPercentage / 2);
                        velCurve.Add(2.5f - _jetTechBonusCurveChange, 1);
                        velCurve.Add(5f + _jetTechBonusCurveChange, 1);
                        velCurve.Add(50, 0 + _jetTechBonusPercentage);
                    }
                    else if (jetPerformanceProfile == 1)
                    {
                        velCurve.Add(0f, 0.50f + _jetTechBonusPercentage);
                        velCurve.Add(1f, 1.00f);
                        velCurve.Add(2f, 0.75f + _jetTechBonusPercentage);
                        velCurve.Add(3f, 0.50f + _jetTechBonusPercentage);
                        velCurve.Add(4f, 0.25f + _jetTechBonusPercentage);
                        velCurve.Add(5f, 0.00f + _jetTechBonusPercentage);
                        velCurve.Add(6f, 0.00f);
                    }
                }
                else
                    velCurve = originalVelocityCurve;

                if (overrideAtmosphereCurve)
                {
                    if (jetPerformanceProfile == 0)
                    {
                        atmosphereIspCurve.Add(0, Mathf.Min((float)_maxISP * 5f / 4f, (float)PluginHelper.MaxThermalNozzleIsp));
                        atmosphereIspCurve.Add(0.15f, Mathf.Min((float)_maxISP, (float)PluginHelper.MaxThermalNozzleIsp));
                        atmosphereIspCurve.Add(0.3f, Mathf.Min((float)_maxISP, (float)PluginHelper.MaxThermalNozzleIsp));
                        atmosphereIspCurve.Add(1, Mathf.Min((float)_maxISP * 4f / 5f, (float)PluginHelper.MaxThermalNozzleIsp));
                    }
                    else if (jetPerformanceProfile == 1)
                    {
                        atmosphereIspCurve.Add(0, Mathf.Min((float)_maxISP * 5f / 4f, (float)PluginHelper.MaxThermalNozzleIsp));
                        atmosphereIspCurve.Add(0.15f, Mathf.Min((float)_maxISP, (float)PluginHelper.MaxThermalNozzleIsp));
                        atmosphereIspCurve.Add(0.3f, Mathf.Min((float)_maxISP, (float)PluginHelper.MaxThermalNozzleIsp));
                        atmosphereIspCurve.Add(1, Mathf.Min((float)_maxISP, (float)PluginHelper.MaxThermalNozzleIsp));
                    }
                }
                else
                    atmosphereIspCurve = originalAtmosphereCurve;

                if (overrideAtmCurve)
                {
                    atmCurve.Add(0, 0);
                    atmCurve.Add(0.045f, 0.25f);
                    atmCurve.Add(0.16f, 0.55f);
                    atmCurve.Add(0.5f, 0.8f);
                    atmCurve.Add(1f, 1f);
                }
                else
                    atmCurve = originalAtmCurve;

                myAttachedEngine.atmCurve = atmCurve;
                myAttachedEngine.velCurve = velCurve;
                myAttachedEngine.engineAccelerationSpeed = effectiveJetengineAccelerationSpeed;
                myAttachedEngine.engineDecelerationSpeed = effectiveJetengineDecelerationSpeed;

                myAttachedEngine.useAtmCurve = true;
                myAttachedEngine.useVelCurve = true;
                myAttachedEngine.useEngineResponseTime = AttachedReactor.ReactorSpeedMult > 0;
            }


            myAttachedEngine.atmosphereCurve = atmosphereIspCurve;
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

                var base_max_thrust = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumPower / _maxISP / GameConstants.STANDARD_GRAVITY * GetHeatExchangerThrustDivisor();
                var max_thrust_in_space = base_max_thrust;
                base_max_thrust *= _thrustPropellantMultiplier;

                final_max_thrust_in_space = base_max_thrust;

                myAttachedEngine.maxFuelFlow = (float)Math.Max(base_max_thrust / (GameConstants.STANDARD_GRAVITY * _maxISP), 0.0000000001);
                myAttachedEngine.maxThrust = (float)Math.Max(base_max_thrust, 0.000001);

                var max_thrust_in_current_atmosphere = max_thrust_in_space;

                UpdateAtmosphericPresureTreshold();

                // update engine thrust/ISP for thermal noozle
                if (!_currentpropellant_is_jet)
                {
                    max_thrust_in_current_atmosphere = Math.Max(max_thrust_in_space - pressureThreshold, 0.000001);

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
                atmospherecurve.Add(0, 0.000001f, 0, 0);
                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
        }

        public void FixedUpdate() // FixedUpdate is also called while not staged
        {
            if (!HighLogic.LoadedSceneIsFlight || myAttachedEngine == null) return;

            try
            {
                ConfigEffects();

                currentThrottle = myAttachedEngine.currentThrottle;
                requestedThrottle = myAttachedEngine.requestedThrottle;

                if (AttachedReactor == null)
                {
                    if (myAttachedEngine.isOperational && currentThrottle > 0)
                    {
                        //myAttachedEngine.Events["Shutdown"].Invoke();
                        myAttachedEngine.Shutdown();
                        ScreenMessages.PostScreenMessage("Engine Shutdown: No reactor attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    myAttachedEngine.maxFuelFlow = 0.0000000001f;
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

                bool canUseThermalPower = UseThermalPower;
                bool canUseChargedPower = this.allowUseOfChargedPower && AttachedReactor.ChargedPowerRatio > 0;

                effectiveThermalSupply = canUseThermalPower ? getResourceSupply(ResourceManager.FNRESOURCE_THERMALPOWER) : 0;
                effectiveChargedSupply = canUseChargedPower ? getResourceSupply(ResourceManager.FNRESOURCE_CHARGED_PARTICLES) : 0;

                maximumPowerUsageForPropulsionRatio = isPlasmaNozzle
                    ? AttachedReactor.PlasmaPropulsionEfficiency
                    : AttachedReactor.ThermalPropulsionEfficiency;

                maximumThermalPower = AttachedReactor.MaximumThermalPower;
                maximumChargedPower = AttachedReactor.MaximumChargedPower;

                currentMaxThermalPower = Math.Min(effectiveThermalSupply, maximumThermalPower * maximumPowerUsageForPropulsionRatio * currentThrottle);
                currentMaxChargedPower = Math.Min(effectiveChargedSupply, maximumChargedPower * maximumPowerUsageForPropulsionRatio * currentThrottle);

                thermalRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_THERMALPOWER);
                chargedParticleRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                availableThermalPower = currentMaxThermalPower * (thermalRatio > 0.5 ? 1 : thermalRatio * 2);
                availableChargedPower = currentMaxChargedPower * (chargedParticleRatio > 0.5 ? 1 : chargedParticleRatio * 2);

                UpdateAnimation();

                if (myAttachedEngine.getIgnitionState && currentThrottle > 0)
                    GenerateThrustFromReactorHeat();
                else
                {
                    _engineWasInactivePreviousFrame = true;

                    UpdateMaxIsp();

                    UpdateIspEngineParams();

                    expectedMaxThrust = AttachedReactor.MaximumPower * maximumPowerUsageForPropulsionRatio * GetPowerThrustModifier() * GetHeatThrustModifier() / GameConstants.STANDARD_GRAVITY / _maxISP * GetHeatExchangerThrustDivisor();
                    calculatedMaxThrust = expectedMaxThrust;

                    var sootMult = CheatOptions.UnbreakableJoints ? 1 : 1f - sootAccumulationPercentage / 200;

                    expectedMaxThrust *= _thrustPropellantMultiplier * sootMult;

                    max_fuel_flow_rate = expectedMaxThrust / _maxISP / GameConstants.STANDARD_GRAVITY;

                    UpdateAtmosphericPresureTreshold();

                    var thrustAtmosphereRatio = expectedMaxThrust <= 0 ? 0 : Math.Max(0, expectedMaxThrust - pressureThreshold) / expectedMaxThrust;

                    current_isp = _maxISP * thrustAtmosphereRatio;

                    calculatedMaxThrust = Math.Max((calculatedMaxThrust - pressureThreshold), 0.000001);

                    var sootModifier = CheatOptions.UnbreakableJoints ? 1 : sootHeatDivider > 0 ? 1f - (sootAccumulationPercentage / sootThrustDivider) : 1;

                    calculatedMaxThrust *= _thrustPropellantMultiplier * sootModifier;

                    var effectiveIsp = isJet ? Math.Min(current_isp, PluginHelper.MaxThermalNozzleIsp) : current_isp;

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
                    if (calculatedMaxThrust <= 0.000001)
                    {
                        calculatedMaxThrust = 0.000001;
                        max_fuel_flow_rate = 0;
                    }

                    // set engines maximum fuel flow
                    myAttachedEngine.maxFuelFlow = (float)Math.Max(max_fuel_flow_rate, 0.0000000001);
                    myAttachedEngine.heatProduction = 1;

                    if (pulseDuration == 0 && myAttachedEngine is ModuleEnginesFX && !String.IsNullOrEmpty(_particleFXName))
                    {
                        effectRatio = 0;
                        part.Effect(_particleFXName, effectRatio, -1);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Error FixedUpdate " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
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
                if (_maxISP > GameConstants.MaxThermalNozzleIsp && isPlasmaNozzle)
                    pressureThreshold *= 2;
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

                if (pulseDuration > 0 && !String.IsNullOrEmpty(_particleFXName) && myAttachedEngine is ModuleEnginesFX)
                {
                    if (increase > 0 && calculatedMaxThrust > 0 && myAttachedEngine.currentThrottle > 0 && _currentAnimatioRatio < pulseDuration)
                        part.Effect(_particleFXName, 1 - _currentAnimatioRatio / pulseDuration, -1);
                    else
                        part.Effect(_particleFXName, 0, -1);
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
                Debug.LogError("[KSPI] - Error UpdateAnimation " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }



        private void GenerateThrustFromReactorHeat()
        {
            try
            {
                if (!AttachedReactor.IsActive)
                    AttachedReactor.EnableIfPossible();

                GetMaximumIspAndThrustMultiplier();

                thrust_modifiers = AttachedReactor.GetFractionThermalReciever(id);
                requested_thermal_power = availableThermalPower * thrust_modifiers;

                power_received = consumeFNResourcePerSecond(requested_thermal_power, ResourceManager.FNRESOURCE_THERMALPOWER);

                if (currentMaxChargedPower > 0)
                {
                    requested_charge_particles = availableChargedPower * thrust_modifiers;
                    power_received += consumeFNResourcePerSecond(requested_charge_particles, ResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                }

                // shutdown engine when connected heatsource cannot produce power
                if (!AttachedReactor.CanProducePower)
                {
                    ScreenMessages.PostScreenMessage("no power produced by thermal source!", 0.02f, ScreenMessageStyle.UPPER_CENTER);
                }

                UpdateSootAccumulation();

                // consume wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    var sootModifier = CheatOptions.UnbreakableJoints
                        ? 1
                        : sootHeatDivider > 0
                            ? 1 - (sootAccumulationPercentage / sootHeatDivider)
                            : 1;

                    var baseWasteheatEfficiency = _maxISP > GameConstants.MaxThermalNozzleIsp ? wasteheatEfficiencyHighTemperature : wasteheatEfficiencyLowTemperature;

                    var reactorWasteheatModifier = isPlasmaNozzle ? AttachedReactor.PlasmaWasteheatProductionMult : AttachedReactor.EngineWasteheatProductionMult;

                    wasteheatEfficiencyModifier = 1 - ((1 - baseWasteheatEfficiency) * reactorWasteheatModifier * AttachedReactor.ThermalPropulsionWasteheatModifier / _fuelCoolingFactor);

                    consumeFNResourcePerSecond(sootModifier * wasteheatEfficiencyModifier * power_received, ResourceManager.FNRESOURCE_WASTEHEAT);
                }

                // calculate max thrust
                heatExchangerThrustDivisor = GetHeatExchangerThrustDivisor();

                if (power_received > 0 && _maxISP > 0)
                {
                    if (_engineWasInactivePreviousFrame)
                    {
                        current_isp = _maxISP * 0.01;
                        _engineWasInactivePreviousFrame = false;
                    }

                    var ispRatio = _currentpropellant_is_jet ? current_isp / _maxISP : 1;

                    powerHeatModifier = GetPowerThrustModifier() * GetHeatThrustModifier();

                    engineMaxThrust = powerHeatModifier * power_received / _maxISP / GameConstants.STANDARD_GRAVITY * heatExchangerThrustDivisor;

                    thrustPerMegaJoule = powerHeatModifier * maximumPowerUsageForPropulsionRatio / _maxISP / GameConstants.STANDARD_GRAVITY * heatExchangerThrustDivisor * ispRatio;

                    expectedMaxThrust = thrustPerMegaJoule * AttachedReactor.MaximumPower;

                    myAttachedEngine.maxThrust = (float)Math.Max(thrustPerMegaJoule * AttachedReactor.RawMaximumPower, 0.000001);

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
                    max_thrust_in_current_atmosphere = Math.Max(max_thrust_in_space - pressureThreshold,  0.0000000001);

                    var atmosphereThrustEfficiency = max_thrust_in_space > 0 ? Math.Min(1, max_thrust_in_current_atmosphere / max_thrust_in_space) : 0;

                    var thrustAtmosphereRatio = max_thrust_in_space > 0 ? Math.Max(atmosphereThrustEfficiency, 0.01) : 0.01;
                    UpdateIspEngineParams(thrustAtmosphereRatio);
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
                    final_max_engine_thrust = 0.0000000001;
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
                        max_fuel_flow_rate = 0.0000000001;
                        calculatedMaxThrust = 0;
                    }
                }

                if (myAttachedEngine.useAtmCurve && myAttachedEngine.atmCurve != null)
                {
                    atmosphereModifier = myAttachedEngine.atmCurve.Evaluate((float)vessel.atmDensity);

                    if (atmosphereModifier > 0 && !float.IsNaN(atmosphereModifier) && !float.IsInfinity(atmosphereModifier))
                    {
                        max_fuel_flow_rate = Math.Max(max_fuel_flow_rate * atmosphereModifier, 0.0000000001);
                        calculatedMaxThrust *= atmosphereModifier;
                    }
                    else
                    {
                        max_fuel_flow_rate = 1e-10;
                        calculatedMaxThrust = 0;
                    }
                }

                if (calculatedMaxThrust <= 0.000001 || double.IsNaN(calculatedMaxThrust) || double.IsInfinity(calculatedMaxThrust))
                {
                    calculatedMaxThrust = 0.000001;
                    max_fuel_flow_rate = 1e-10;
                }

                // set engines maximum fuel flow
                myAttachedEngine.maxFuelFlow = (float)Math.Max( max_fuel_flow_rate, 1e-10);

                // act as open cycle cooler
                if (!isPlasmaNozzle && !CheatOptions.IgnoreMaxTemperature)
                {
                    consumeFNResourcePerSecond(20 * getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT) * currentEngineFuelFlow, ResourceManager.FNRESOURCE_WASTEHEAT);
                }

                // Calculate
                pre_coolers_active = _vesselPrecoolers.Sum(prc => prc.ValidAttachedIntakes);
                total_intakes = _vesselResourceIntakes.Count();
                intakes_open = _vesselResourceIntakes.Where(mre => mre.intakeEnabled).Count();

                proportion = _currentpropellant_is_jet && intakes_open > 0
                    ? Math.Pow((double)(intakes_open - pre_coolers_active) / (double)intakes_open, 0.1)
                    : 0;

                if (double.IsNaN(proportion) || double.IsInfinity(proportion))
                    proportion = 0;

                airflowHeatModifier = proportion > 0
                    ? Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20.0 / GameConstants.atmospheric_non_precooled_limit * proportion), 0)
                    : 0;

                airflowHeatModifier *= vessel.atmDensity * (vessel.speed / vessel.speedOfSound);
                if (double.IsNaN(airflowHeatModifier) || double.IsInfinity(airflowHeatModifier))
                    airflowHeatModifier = 0;

                currentEngineFuelFlow = myAttachedEngine.fuelFlowGui * myAttachedEngine.mixtureDensity;

                maxFuelFlowOnEngine = myAttachedEngine.maxFuelFlow;
                maxThrustOnEngine = myAttachedEngine.maxThrust;
                realIspEngine = myAttachedEngine.realIsp;

                if (controlHeatProduction)
                {
                    //engineHeatProduction = (currentEngineFuelFlow >= engineHeatFuelThreshold && _maxISP > 100 && part.mass > 0.0001 && myAttachedEngine.currentThrottle > 0)
                    //    ? 0.5 * myAttachedEngine.currentThrottle * Math.Pow(radius, heatProductionExponent) * heatProductionMult * PluginHelper.EngineHeatProduction / currentEngineFuelFlow / _maxISP / part.mass
                    //    : 1;

                    ispHeatModifier = isPlasmaNozzle ? 0.5 * Approximate.Sqrt(realIspEngine) : 5 * Approximate.Sqrt(realIspEngine);
                    powerToMass = Approximate.Sqrt(maxThrustOnEngine / part.mass);
                    radiusHeatModifier = Math.Pow(scaledRadius * radiusHeatProductionMult, radiusHeatProductionExponent);
                    engineHeatProductionMult = AttachedReactor.EngineHeatProductionMult;
                    var reactorHeatModifier = isPlasmaNozzle ? AttachedReactor.PlasmaHeatProductionMult : AttachedReactor.EngineHeatProductionMult;
                    
                    spaceHeatProduction = heatProductionMultiplier * AttachedReactor.EngineHeatProductionMult * _ispPropellantMultiplier * ispHeatModifier * radiusHeatModifier * powerToMass / _fuelCoolingFactor;
                    engineHeatProduction = Math.Min(spaceHeatProduction * (1 + airflowHeatModifier * PluginHelper.AirflowHeatMult), 99999);

                    myAttachedEngine.heatProduction = (float)engineHeatProduction;
                }

                if (pulseDuration == 0 && myAttachedEngine is ModuleEnginesFX && !String.IsNullOrEmpty(_particleFXName))
                {
                    var maxEngineFuelFlow = myAttachedEngine.maxThrust / myAttachedEngine.realIsp / GameConstants.STANDARD_GRAVITY;
                    effectRatio = (float)Math.Min(1, currentEngineFuelFlow / maxEngineFuelFlow);
                    part.Effect(_particleFXName, effectRatio, -1);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Error GenerateThrustFromReactorHeat " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
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
            baseMaxIsp = Math.Sqrt(AttachedReactor.CoreTemperature) * EffectiveCorTempIspMult;

            if (baseMaxIsp > GameConstants.MaxThermalNozzleIsp && !isPlasmaNozzle)
                baseMaxIsp = GameConstants.MaxThermalNozzleIsp;

            if (!isPlasmaNozzle || AttachedReactor.SupportMHD || AttachedReactor.ChargedPowerRatio == 0) 
                _maxISP = baseMaxIsp * _ispPropellantMultiplier;
            else if (UseThermalPower)
            {
                var scaledChargedRatio = 0.2 + Math.Pow((Math.Max(0, AttachedReactor.ChargedPowerRatio - 0.2) * 1.25), 2);

                _maxISP = (scaledChargedRatio * baseMaxIsp + (1 - scaledChargedRatio) * 3000) * _ispPropellantMultiplier;
            }
            else // when only consuming charged particles from reactor
            {
                _maxISP = Math.Pow((double)(decimal)ispThrottle / 100, 2) * 20 * baseMaxIsp;
            }
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
            Debug.Log("[KSPI] - ThermalNozzleController - getPropellants");

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

                var heatExchangerThrustDivisor = GetHeatExchangerThrustDivisor();

                radiusModifier = (heatExchangerThrustDivisor * 100.0).ToString("0.00") + "%";

                UpdateMaxIsp();

                var max_thrust_in_space = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumPower / _maxISP / GameConstants.STANDARD_GRAVITY * heatExchangerThrustDivisor;

                final_max_thrust_in_space = Math.Max(max_thrust_in_space * _thrustPropellantMultiplier, 0.000001);

                myAttachedEngine.maxThrust = (float)final_max_thrust_in_space;

                var isp_in_space = heatExchangerThrustDivisor * _maxISP;

                vacuumPerformance = final_max_thrust_in_space.ToString("0.0") + "kN @ " + isp_in_space.ToString("0.0") + "s";

                maxPressureThresholdAtKerbinSurface = scaledExitArea * GameConstants.EarthAtmospherePressureAtSeaLevel;

                var maxSurfaceThrust = Math.Max(max_thrust_in_space - (maxPressureThresholdAtKerbinSurface), 0.000001);

                var maxSurfaceISP = _maxISP * (maxSurfaceThrust / max_thrust_in_space) * heatExchangerThrustDivisor;

                var final_max_surface_thrust = maxSurfaceThrust * _thrustPropellantMultiplier;

                surfacePerformance = final_max_surface_thrust.ToString("0.0") + "kN @ " + maxSurfaceISP.ToString("0.0") + "s";
            }
            else
            {
                Fields["vacuumPerformance"].guiActiveEditor = false;
                Fields["radiusModifier"].guiActiveEditor = false;
                Fields["surfacePerformance"].guiActiveEditor = false;
            }
        }


        private double storedFractionThermalReciever;
        private double GetHeatExchangerThrustDivisor()
        {
            if (AttachedReactor == null || AttachedReactor.Radius == 0 || scaledRadius == 0) return 0;

            if (_myAttachedReactor.GetFractionThermalReciever(id) == 0) return storedFractionThermalReciever;

            storedFractionThermalReciever = _myAttachedReactor.GetFractionThermalReciever(id);

            var fractionalReactorRadius = Math.Sqrt(AttachedReactor.Radius * AttachedReactor.Radius * storedFractionThermalReciever);

            // scale down thrust if it's attached to the wrong sized reactor
            double heat_exchanger_thrust_divisor = scaledRadius > fractionalReactorRadius
                ? fractionalReactorRadius * fractionalReactorRadius / scaledRadius / scaledRadius
                : normalizeFraction(scaledRadius / fractionalReactorRadius, 1);

            if (!_currentpropellant_is_jet)
            {
                for (int i = 0; i < partDistance; i++)
                {
                    heat_exchanger_thrust_divisor *= AttachedReactor.ThermalTransportationEfficiency;
                }
            }

            return heat_exchanger_thrust_divisor;
        }

        private double normalizeFraction(double variable, double normalizer)
        {
            return (normalizer + variable) / (1 + normalizer);
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
    }
}
