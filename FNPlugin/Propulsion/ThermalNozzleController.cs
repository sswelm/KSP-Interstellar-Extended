using FNPlugin.Propulsion;
using FNPlugin.Extensions;
using OpenResourceSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;

namespace FNPlugin
{
    class ThermalNozzleController : FNResourceSuppliableModule, IEngineNoozle, IUpgradeableModule, IRescalable<ThermalNozzleController>
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool isHybrid = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;

        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Soot Accumulation", guiUnits = " %")]
        public float sootAccumulationPercentage;
        [KSPField(isPersistant = true)]
        public bool isDeployed = false;


        [KSPField(isPersistant = true)]
        public double animationStarted = 0;
        [KSPField(isPersistant = false)]
        public bool initialized = false;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public int jetPerformanceProfile = 0;
        [KSPField(isPersistant = false)]
        public bool canUseLFO = false;
        [KSPField(isPersistant = false)]
        public bool isJet = false;
        [KSPField(isPersistant = false)]
        public float powerTrustMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float powerTrustMultiplierJet = 1;
        [KSPField(isPersistant = false)]
        public float IspTempMultOffset = -1.371670613f;
        [KSPField(isPersistant = false)]
        public float sootHeatDivider = 150;
        [KSPField(isPersistant = false)]
        public float sootThrustDivider = 150;
        [KSPField(isPersistant = false)]
        public float delayedThrottleFactor = 0.5f;

        [KSPField(isPersistant = false)]
        public float maxTemp = 2750;
        [KSPField(isPersistant = false)]
        public float heatConductivity = 0.12f;
        [KSPField(isPersistant = false)]
        public float heatConvectiveConstant = 1f;
        [KSPField(isPersistant = false)]
        public float emissiveConstant = 0.85f;
        [KSPField(isPersistant = false)]
        public float thermalMassModifier = 1f;
        [KSPField(isPersistant = false)]
        public float engineHeatProductionConst = 3000; 
        [KSPField(isPersistant = false)]
        public float engineHeatProductionExponent = 0.8f;
        [KSPField(isPersistant = false)]
        public float engineHeatFuelThreshold = 0.001f;
        [KSPField(isPersistant = false)]
        public float skinMaxTemp = 2750;
        [KSPField(isPersistant = false)]
        public float skinInternalConductionMult = 1;
        [KSPField(isPersistant = false)]
        public float skinThermalMassModifier = 1;
        [KSPField(isPersistant = false)]
        public float skinSkinConductionMult = 1;

        [KSPField(isPersistant = false)]
        public string deployAnimationName = String.Empty;
        [KSPField(isPersistant = false)]
        public string pulseAnimationName = String.Empty;
        [KSPField(isPersistant = false)]
        public string emiAnimationName = String.Empty;
        [KSPField(isPersistant = false)]
        public float pulseDuration = 0;
        [KSPField(isPersistant = false)]
        public float recoveryAnimationDivider = 1;

        [KSPField(isPersistant = false)]
        public float wasteheatEfficiencyLowTemperature = 1;
        [KSPField(isPersistant = false)]
        public float wasteheatEfficiencyHighTemperature = 0.9f;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
        [KSPField(isPersistant = false)]
        public string EffectNameJet = String.Empty;
        [KSPField(isPersistant = false)]
        public string EffectNameLFO = String.Empty;
        [KSPField(isPersistant = false)]
        public string EffectNameNonLFO = String.Empty;
        [KSPField(isPersistant = false)]
        public string EffectNameLithium = String.Empty;
        [KSPField(isPersistant = false)]
        public bool showPartTemperature = true;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool limitedByMaxThermalNozzleIsp = true;
        [KSPField(isPersistant = false, guiActive = false)]
        public float baseMaxIsp;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radius", guiUnits = " m")]
        public float radius;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Exit Area", guiUnits = " m2")]
        public float exitArea = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Empty Mass", guiUnits = " t")]
        public float partMass = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Afterburner upgrade tech")]
        public string afterburnerTechReq = String.Empty;

        //External
        public bool static_updating = true;
        public bool static_updating2 = true;

        //GUI
        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
        public string engineType = ":";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Propellant")]
        public string _fuelmode;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Propellant Isp Multiplier")]
        public float _ispPropellantMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Soot")]
        public float _propellantSootFactorFullThrotle;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Min Soot")]
        public float _propellantSootFactorMinThrotle;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Equilibrium Soot")]
        public float _propellantSootFactorEquilibrium;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Temperature")]
        public string temperatureStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "ISP / Thrust Mult")]
        public string thrustIspMultiplier = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Fuel Thrust Multiplier", guiFormat="F3")]
        public float _thrustPropellantMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Upgrade Cost")]
        public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Base Heat Production")]
        public float baseHeatProduction = 100;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Heat Production")]
        public double engineHeatProduction;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Treshold", guiUnits = " kN")]
        public double pressureTreshold;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmospheric Limit")]
        public float atmospheric_limit;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Heat", guiUnits = " MJ", guiFormat = "F3")]
        public double requested_thermal_power;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Charge", guiUnits = " MJ")]
        public float requested_charge_particles;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Recieved Power", guiUnits = " MJ", guiFormat="F3")]
        public double thermal_power_received;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radius Modifier")]
        public string radiusModifier;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Vacuum")]
        public string vacuumPerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Sea")]
        public string surfacePerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Base Isp")]
        protected float _baseIspMultiplier;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Decomposition Energy")]
        protected float _decompositionEnergy;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Exchange Divider")]
        protected float heatExchangerThrustDivisor;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Engine Max Thrust", guiFormat = "F3", guiUnits = " kN")]
        protected double engineMaxThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thrust In Space")]
        protected double max_thrust_in_space;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thrust In Current")]
        protected double max_thrust_in_current_atmosphere;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Final Engine Thrust")]
        protected double final_max_engine_thrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "MaxISP")]
        protected float _maxISP;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "MinISP")]
        protected float _minISP;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Calculated Thrust", guiFormat = "F3")]
        protected double calculatedMaxThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Fuel Flow")]
        protected double max_fuel_flow_rate = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Isp", guiFormat = "F3")]
        protected double current_isp = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "MaxPressureThresshold")]
        protected float maxPressureThresholdAtKerbinSurface;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thermal Ratio")]
        protected double thermalRatio;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Consumed")]
        protected double consumedWasteHeat;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Expected Max Thrust")]
        protected double expectedMaxThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Is LFO")]
        protected bool _propellantIsLFO = false;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Velocity Modifier", guiFormat = "F3")]
        protected float vcurveAtCurrentVelocity;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmosphere Modifier", guiFormat = "F3")]
        protected float atmosphereModifier;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atom Type")]
        protected int _atomType = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Propellant Type")]
        protected int _propType = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Is Neutron Absorber")]
        protected bool _isNeutronAbsorber = false;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Maximum Power", guiUnits = " MJ")]
        protected double _currentMaximumPower;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thermal Modifier")]
        protected double thermal_modifiers;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Available T Power ", guiUnits = " MJ")]
        protected double _availableThermalPower;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Delayed Throttle")]
        protected float delayedThrottle = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Air Flow Heat Modifier", guiFormat = "F3")]
        double airflowHeatModifier;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        int pre_coolers_active;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        int intakes_open;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        int total_intakes;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        double proportion;
        
        [KSPField]
        public int supportedPropellantAtoms = 511;
        [KSPField]
        public int supportedPropellantTypes = 511;

        //Internal
        protected string _particleFXName;
        //protected string _currentAudioFX;
        protected bool _fuelRequiresUpgrade;
        protected string _fuelTechRequirement;
        protected float _fuelToxicity;
        //protected double _savedReputationCost;
        protected float _heatDecompositionFraction;

        protected float _minDecompositionTemp;
        protected float _maxDecompositionTemp;
        protected const float _hydroloxDecompositionEnergy = 16.2137f;
        protected Guid id = Guid.NewGuid();
        protected ConfigNode[] propellantsConfignodes;

        protected bool hasrequiredupgrade = false;
        protected bool hasstarted = false;
        protected bool hasSetupPropellant = false;
        protected ModuleEngines myAttachedEngine;
        protected bool _currentpropellant_is_jet = false;

        List<Propellant> list_of_propellants = new List<Propellant>();

        protected Animation deployAnim;
        protected AnimationState[] pulseAnimationState;
        protected AnimationState[] emiAnimationState;
        protected int thrustLimitRatio = 0;
        protected double old_intake = 0;
        protected int partDistance = 0;
        protected float old_atmospheric_limit;
        protected double currentintakeatm;

        protected List<FNModulePreecooler> _vesselPrecoolers;
        protected List<ModuleResourceIntake> _vesselResourceIntakes;
        protected List<IEngineNoozle> _vesselThermalNozzles;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        protected float jetTechBonus;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        protected float jetTechBonusPercentage;

        public bool Static_updating { get { return static_updating; } set { static_updating = value; } }
        public bool Static_updating2 { get { return static_updating2; } set { static_updating2 = value; } }
        public int Fuel_mode { get { return fuel_mode; } }

        private IThermalSource _myAttachedReactor;
        public IThermalSource AttachedReactor
        {
            get { return _myAttachedReactor; }
            private set
            {
                _myAttachedReactor = value;
                if (_myAttachedReactor == null)
                    return;
                _myAttachedReactor.AttachThermalReciever(id, radius);
            }
        }

        //Static
        static Dictionary<string, double> intake_amounts = new Dictionary<string, double>();
        static Dictionary<string, double> intake_maxamounts = new Dictionary<string, double>();
        static Dictionary<string, double> fuel_flow_amounts = new Dictionary<string, double>();

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        private int switches = 0;

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

        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            // update variables
            radius *= factor.relative.linear;
            exitArea *= factor.relative.quadratic;

            // update simulation
            UpdateRadiusModifier();
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

        public void upgradePartModule()
        {
            engineType = upgradedName;
            isupgraded = true;

            if (isJet)
            {
                propellantsConfignodes = getPropellantsHybrid();
                isHybrid = true;
            }
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

            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - setup animation");

            if (!String.IsNullOrEmpty(deployAnimationName))
                deployAnim = part.FindModelAnimators(deployAnimationName).FirstOrDefault();
            if (!String.IsNullOrEmpty(pulseAnimationName))
                pulseAnimationState = PluginHelper.SetUpAnimation(pulseAnimationName, this.part);
            if (!String.IsNullOrEmpty(emiAnimationName))
                emiAnimationState = PluginHelper.SetUpAnimation(emiAnimationName, this.part);

            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - calculate WasteHeat Capacity");

            // calculate WasteHeat Capacity
            PartResource wasteheatPowerResource = part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT];
            if (wasteheatPowerResource != null)
            {
                var ratio = wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount;
                wasteheatPowerResource.maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * ratio;
            }

            engineType = originalName;

            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - find module implementing <ModuleEngines>");

            myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();

            // find attached thermal source
            ConnectToThermalSource();

            maxPressureThresholdAtKerbinSurface = exitArea * (float)GameConstants.EarthAtmospherePressureAtSeaLevel;

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
            else
                UpdateRadiusModifier();

            // presearch all avaialble precoolers, intakes and nozzles on the vessel
            _vesselPrecoolers = vessel.FindPartModulesImplementing<FNModulePreecooler>();
            _vesselResourceIntakes = vessel.FindPartModulesImplementing<ModuleResourceIntake>().Where(mre => mre.resourceName == InterstellarResourcesConfiguration.Instance.IntakeAir).ToList();
            _vesselThermalNozzles = vessel.FindPartModulesImplementing<IEngineNoozle>();

            // if we can upgrade, let's do so
            if (isupgraded)
                upgradePartModule();
            else
            {
                if (this.HasTechsRequiredToUpgrade())
                    hasrequiredupgrade = true;

                // if not, use basic propellants
                propellantsConfignodes = getPropellants(isJet);
            }

            bool hasJetUpgradeTech0 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech0);
            bool hasJetUpgradeTech1 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech1);
            bool hasJetUpgradeTech2 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech2);
            bool hasJetUpgradeTech3 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech3);

            jetTechBonus = Convert.ToInt32(hasJetUpgradeTech0) + 1.2f * Convert.ToInt32(hasJetUpgradeTech1) + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f * Convert.ToInt32(hasJetUpgradeTech3);
            jetTechBonusPercentage = jetTechBonus / 26.84f;

            hasstarted = true;

            Fields["temperatureStr"].guiActive = showPartTemperature;
            //Fields["chargedParticlePropulsionIsp"].guiActive = showChargedParticlePropulsionIsp;

            try
            {
                SetupPropellants();
            }
            catch
            {
                Debug.LogError("OnStart Exception in SetupPropellants");
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
            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - start BreadthFirstSearchForThermalSource");

            var source = ThermalSourceSearchResult.BreadthFirstSearchForThermalSource(part, (p) => p.IsThermalSource, 10, 1);

            if (source == null || source.Source == null)
            {
                UnityEngine.Debug.LogWarning("[KSPI] - ThermalNozzleController - BreadthFirstSearchForThermalSource-Failed to find thermal source");
                return;
            }

            AttachedReactor = source.Source;
            AttachedReactor.ConnectWithEngine(this);

            partDistance = (int)Math.Max(Math.Ceiling(source.Cost) - 1, 0);
            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - BreadthFirstSearchForThermalSource- Found thermal searchResult with distance " + partDistance);
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate()
        {
            try
            {
                // setup propellant after startup to allow InterstellarFuelSwitch to configure the propellant
                if (!hasSetupPropellant)
                {
                    hasSetupPropellant = true;
                    SetupPropellants(true, true);
                }

                temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
                UpdateAtmosphericPresureTreshold();

                Fields["sootAccumulationPercentage"].guiActive = sootAccumulationPercentage > 0;

                thrustIspMultiplier = _ispPropellantMultiplier + " / " + _thrustPropellantMultiplier;

                Fields["engineType"].guiActive = isJet;
                if (ResearchAndDevelopment.Instance != null && isJet)
                {
                    Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                    upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
                }
                else
                    Events["RetrofitEngine"].active = false;

                Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade && isJet;

                if (myAttachedEngine == null)
                    return;

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
                ConfigNode chosenpropellant = propellantsConfignodes[fuel_mode];

                UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - Setup propellant chosenpropellant " + fuel_mode + " / " +  propellantsConfignodes.Count());

                UpdatePropellantModeBehavior(chosenpropellant);
                ConfigNode[] propellantNodes = chosenpropellant.GetNodes("PROPELLANT");
                list_of_propellants.Clear();

                // loop though propellants until we get to the selected one, then set it up
                foreach (ConfigNode prop_node in propellantNodes)
                {
                    ExtendedPropellant curprop = new ExtendedPropellant();

                    curprop.Load(prop_node);

                    if (curprop.drawStackGauge && HighLogic.LoadedSceneIsFlight)
                    {
                        curprop.drawStackGauge = false;
                    }

                    if (list_of_propellants == null)
                        UnityEngine.Debug.LogWarning("[KSPI] - ThermalNozzleController - SetupPropellants list_of_propellants is null");

                    list_of_propellants.Add(curprop);
                }

                //Get the Ignition state, i.e. is the engine shutdown or activated
                var engineState = myAttachedEngine.getIgnitionState;

                myAttachedEngine.Shutdown();

                // update the engine with the new propellants
                if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null)
                {
                    ConfigNode newPropNode = new ConfigNode();
                    foreach (var prop in list_of_propellants)
                    {
                        ConfigNode propellantConfigNode = newPropNode.AddNode("PROPELLANT");
                        propellantConfigNode.AddValue("name", prop.name);
                        propellantConfigNode.AddValue("ratio", prop.ratio);
                        propellantConfigNode.AddValue("DrawGauge", "true");
                    }
                    myAttachedEngine.Load(newPropNode);
                }

                if (engineState == true)
                    myAttachedEngine.Activate();

                if (HighLogic.LoadedSceneIsFlight)
                { // you can have any fuel you want in the editor but not in flight
                    // should we switch to another propellant because we have none of this one?
                    bool next_propellant = false;

                    string missingResources = String.Empty;

                    foreach (Propellant curEngine_propellant in list_of_propellants)
                    {
                        var extendedPropellant = curEngine_propellant as ExtendedPropellant;
                        //IEnumerable<PartResource> partresources = part.GetConnectedResources(extendedPropellant.StoragePropellantName);
                        var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(extendedPropellant.StoragePropellantName);
                        double amount = 0;
                        double maxAmount = 0;
                        if (resourceDefinition != null)
                            part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

                        //if (!partresources.Any() || !PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name))
                        if (maxAmount == 0)
                        {
                            if (notifySwitching)
                                missingResources += curEngine_propellant.name + " ";
                            next_propellant = true;
                        }
                        else if (
                               (!PluginHelper.HasTechRequirementOrEmpty(_fuelTechRequirement))
                            || (_fuelRequiresUpgrade && !isupgraded)
                            || (_propellantIsLFO && !PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))
                            || ((_atomType & _myAttachedReactor.SupportedPropellantAtoms) != _atomType)
                            || ((_atomType & this.supportedPropellantAtoms) != _atomType)
                            || ((_propType & _myAttachedReactor.SupportedPropellantTypes) != _propType)
                            || ((_propType & this.supportedPropellantTypes) != _propType)
                            )
                        {
                            next_propellant = true;
                        }
                    }

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

                    // Still ignore propellants that don't exist or we cannot use due to the limmitations of the engine
                    if (
						( (!PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name))
                        || (!PluginHelper.HasTechRequirementOrEmpty(_fuelTechRequirement))
                        || (_fuelRequiresUpgrade && !isupgraded)
                        || (_propellantIsLFO && !PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))
                        || ((_atomType & _myAttachedReactor.SupportedPropellantAtoms) != _atomType)
                        || ((_atomType & this.supportedPropellantAtoms) != _atomType)
                        || ((_propType & _myAttachedReactor.SupportedPropellantTypes) != _propType)
                        || ((_propType & this.supportedPropellantTypes) != _propType)
						)  && (switches <= propellantsConfignodes.Length || fuel_mode != 0) ) 
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
                UnityEngine.Debug.LogError("[KSPI] - Error SetupPropellants " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void UpdatePropellantModeBehavior(ConfigNode chosenpropellant)
        {
            _fuelmode = chosenpropellant.GetValue("guiName");
            _propellantSootFactorFullThrotle = chosenpropellant.HasValue("maxSootFactor") ? float.Parse(chosenpropellant.GetValue("maxSootFactor")) : 0;
            _propellantSootFactorMinThrotle = chosenpropellant.HasValue("minSootFactor") ? float.Parse(chosenpropellant.GetValue("minSootFactor")) : 0;
            _propellantSootFactorEquilibrium = chosenpropellant.HasValue("levelSootFraction") ? float.Parse(chosenpropellant.GetValue("levelSootFraction")) : 0;
            _minDecompositionTemp = chosenpropellant.HasValue("MinDecompositionTemp") ? float.Parse(chosenpropellant.GetValue("MinDecompositionTemp")) : 0;
            _maxDecompositionTemp = chosenpropellant.HasValue("MaxDecompositionTemp") ? float.Parse(chosenpropellant.GetValue("MaxDecompositionTemp")) : 0;
            _decompositionEnergy = chosenpropellant.HasValue("DecompositionEnergy") ? float.Parse(chosenpropellant.GetValue("DecompositionEnergy")) : 0;
            _baseIspMultiplier = chosenpropellant.HasValue("BaseIspMultiplier") ? float.Parse(chosenpropellant.GetValue("BaseIspMultiplier")) : 0;
            _fuelTechRequirement = chosenpropellant.HasValue("TechRequirement") ? chosenpropellant.GetValue("TechRequirement") : String.Empty;
            _fuelToxicity = chosenpropellant.HasValue("Toxicity") ? float.Parse(chosenpropellant.GetValue("Toxicity")) : 0;
            _fuelRequiresUpgrade = chosenpropellant.HasValue("RequiresUpgrade") ? Boolean.Parse(chosenpropellant.GetValue("RequiresUpgrade")) : false;

            _currentpropellant_is_jet = chosenpropellant.HasValue("isJet") ? bool.Parse(chosenpropellant.GetValue("isJet")) : false;
            _propellantIsLFO = chosenpropellant.HasValue("isLFO") ? bool.Parse(chosenpropellant.GetValue("isLFO")) : false;
            _atomType = chosenpropellant.HasValue("atomType") ? int.Parse(chosenpropellant.GetValue("atomType")) : 1;
            _propType = chosenpropellant.HasValue("propType") ? int.Parse(chosenpropellant.GetValue("propType")) : 1;
            _isNeutronAbsorber = chosenpropellant.HasValue("isNeutronAbsorber") ? bool.Parse(chosenpropellant.GetValue("isNeutronAbsorber")) : false;

            if (!_currentpropellant_is_jet && _decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                UpdateThrustPropellantMultiplier();
            else
            {
                _heatDecompositionFraction = 1;
                _ispPropellantMultiplier = chosenpropellant.HasValue("ispMultiplier") ? float.Parse(chosenpropellant.GetValue("ispMultiplier")) : 1;
                var rawthrustPropellantMultiplier = chosenpropellant.HasValue("thrustMultiplier") ? float.Parse(chosenpropellant.GetValue("thrustMultiplier")) : 1;
                _thrustPropellantMultiplier = _propellantIsLFO ? rawthrustPropellantMultiplier : ((rawthrustPropellantMultiplier + 1) / 2.0f);
            }
        }

        private void UpdateThrustPropellantMultiplier()
        {
            var linearFraction = Math.Max(0, Math.Min(1, (AttachedReactor.CoreTemperature - _minDecompositionTemp) / (_maxDecompositionTemp - _minDecompositionTemp)));
            _heatDecompositionFraction = (float)Math.Pow(0.36, Math.Pow(3 - linearFraction * 3, 2) / 2);
            var thrustPropellantMultiplier = (float)Math.Sqrt(_heatDecompositionFraction * _decompositionEnergy / _hydroloxDecompositionEnergy) * 1.04f + 1;
            _ispPropellantMultiplier = _baseIspMultiplier * thrustPropellantMultiplier;
            _thrustPropellantMultiplier = _propellantIsLFO ? thrustPropellantMultiplier : thrustPropellantMultiplier + 1 / 2;
        }

        public void UpdateIspEngineParams(double atmosphere_isp_efficiency = 1) // , double max_thrust_in_space = 0) 
        {
            // recaculate ISP based on power and core temp available
            FloatCurve atmCurve = new FloatCurve();
            FloatCurve atmosphereIspCurve = new FloatCurve();
            FloatCurve velCurve = new FloatCurve();

            UpdateMaxIsp();

            if (!_currentpropellant_is_jet)
            {
                atmosphereIspCurve.Add(0, _maxISP * (float)atmosphere_isp_efficiency, 0, 0);

                myAttachedEngine.useAtmCurve = false;
                myAttachedEngine.useVelCurve = false;
                myAttachedEngine.useEngineResponseTime = false;
            }
            else
            {
                if (jetPerformanceProfile == 0)
                {
                    atmosphereIspCurve.Add(0, Mathf.Min(_maxISP * 5.0f / 4.0f, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereIspCurve.Add(0.15f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereIspCurve.Add(0.3f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereIspCurve.Add(1, Mathf.Min(_maxISP * 4.0f / 5.0f, PluginHelper.MaxThermalNozzleIsp));

                    var curveChange = jetTechBonus / 5.368f;

                    velCurve.Add(0, 0.05f + jetTechBonusPercentage / 2);
                    velCurve.Add(2.5f - curveChange, 1f);
                    velCurve.Add(5f + curveChange, 1f);
                    velCurve.Add(25f, 0 + jetTechBonusPercentage);
                }
                else if (jetPerformanceProfile == 1)
                {
                    atmosphereIspCurve.Add(0, Mathf.Min(_maxISP * 5.0f / 4.0f, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereIspCurve.Add(0.15f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereIspCurve.Add(0.3f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereIspCurve.Add(1, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));

                    velCurve.Add(0.00f, 0.50f + jetTechBonusPercentage);
                    velCurve.Add(1.00f, 1.00f);
                    velCurve.Add(2.00f, 0.75f + jetTechBonusPercentage);
                    velCurve.Add(3.00f, 0.50f + jetTechBonusPercentage);
                    velCurve.Add(4.00f, 0.25f + jetTechBonusPercentage);
                    velCurve.Add(5.00f, 0.00f + jetTechBonusPercentage);
                    velCurve.Add(6.00f, 0.00f);
                }

                // configure atmCurve
                atmCurve.Add(0, 0);
                atmCurve.Add(0.045f, 0.25f);
                atmCurve.Add(0.16f, 0.55f);
                atmCurve.Add(0.5f, 0.8f);
                atmCurve.Add(1f, 1f);

                myAttachedEngine.atmCurve = atmCurve;
                myAttachedEngine.useAtmCurve = true;

                myAttachedEngine.ignitionThreshold = 0.01f;
                myAttachedEngine.useVelCurve = true;
                myAttachedEngine.velCurve = velCurve;
                myAttachedEngine.useEngineResponseTime = true;
            }


            myAttachedEngine.atmosphereCurve = atmosphereIspCurve;
        }

        public float GetAtmosphericLimit()
        {
            atmospheric_limit = 1.0f;
            if (_currentpropellant_is_jet)
            {
                string resourcename = list_of_propellants[0].name;
                currentintakeatm = getIntakeAvailable(vessel, resourcename);
                var fuelRateThermalJets = GetFuelRateThermalJets(resourcename);

                if (fuelRateThermalJets > 0)
                {
                    // divide current available intake resource by fuel useage across all engines
                    atmospheric_limit = (float)Math.Min(currentintakeatm / fuelRateThermalJets, 1.0); ;
                }
                old_intake = currentintakeatm;
            }
            atmospheric_limit = Mathf.MoveTowards(old_atmospheric_limit, atmospheric_limit, 0.2f);
            old_atmospheric_limit = atmospheric_limit;
            return atmospheric_limit;
        }

        public double GetNozzleFlowRate()
        {
            return myAttachedEngine.isOperational ? max_fuel_flow_rate : 0;
        }

        public void EstimateEditorPerformance()
        {
            FloatCurve atmospherecurve = new FloatCurve();

            if (AttachedReactor != null)
            {
                UpdateMaxIsp();

                if (_maxISP <= 0)
                    return;

                double thrust = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumPower / _maxISP / PluginHelper.GravityConstant * GetHeatExchangerThrustDivisor();
                double max_thrust_in_space = thrust;
                thrust *= _thrustPropellantMultiplier;

                myAttachedEngine.maxFuelFlow = (float)Math.Max(thrust / (PluginHelper.GravityConstant * _maxISP), 0.0000000001);
                myAttachedEngine.maxThrust = (float)Math.Max(thrust, 0.00001);

                double max_thrust_in_current_atmosphere = max_thrust_in_space;

                UpdateAtmosphericPresureTreshold();

                // update engine thrust/ISP for thermal noozle
                if (!_currentpropellant_is_jet)
                {
                    max_thrust_in_current_atmosphere = Math.Max(max_thrust_in_space - pressureTreshold, 0.00001);

                    var thrustAtmosphereRatio = max_thrust_in_space > 0 ? Math.Max(max_thrust_in_current_atmosphere / max_thrust_in_space, 0.01) : 0.01;
                    _minISP = (float)(_maxISP * thrustAtmosphereRatio);
                }
                else
                    _minISP = _maxISP;

                atmospherecurve.Add(0, _maxISP, 0, 0);
                atmospherecurve.Add(1, _minISP, 0, 0);

                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
            else
            {
                atmospherecurve.Add(0, 0.00001f, 0, 0);
                myAttachedEngine.maxThrust = 0;
                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
        }

        private float GetIspPropellantModifier()
        {
            float ispModifier = (PluginHelper.IspNtrPropellantModifierBase == 0
                ? _ispPropellantMultiplier
                : (PluginHelper.IspNtrPropellantModifierBase + _ispPropellantMultiplier) / (1.0f + PluginHelper.IspNtrPropellantModifierBase));
            return ispModifier;
        }

        public void FixedUpdate() // FixedUpdate is also called while not staged
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight) return;

                if (myAttachedEngine == null) return;

                if (AttachedReactor == null)
                {
                    if (myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0)
                    {
                        myAttachedEngine.Events["Shutdown"].Invoke();
                        ScreenMessages.PostScreenMessage("Engine Shutdown: No reactor attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    myAttachedEngine.maxFuelFlow = 0.0000000001f;
                    return;
                }

                // attach/detach with radius
                if (myAttachedEngine.isOperational)
                    _myAttachedReactor.AttachThermalReciever(id, radius);
                else
                    _myAttachedReactor.DetachThermalReciever(id);

                ConfigEffects();

                delayedThrottle = _currentpropellant_is_jet || myAttachedEngine.currentThrottle < delayedThrottle || delayedThrottleFactor <= 0
                    ? myAttachedEngine.currentThrottle
                    : Mathf.MoveTowards(delayedThrottle, myAttachedEngine.currentThrottle, delayedThrottleFactor * TimeWarp.fixedDeltaTime);

                thermalRatio = getResourceBarRatio(FNResourceManager.FNRESOURCE_THERMALPOWER);
                _currentMaximumPower = AttachedReactor.MaximumPower * delayedThrottle;
                _availableThermalPower = _currentMaximumPower * thermalRatio;

                UpdateAnimation();

                if (myAttachedEngine.getIgnitionState && myAttachedEngine.currentThrottle >= 0.01)
                    GenerateThrustFromReactorHeat();
                else
                {
                    consumedWasteHeat = 0;

                    atmospheric_limit = GetAtmosphericLimit();

                    UpdateMaxIsp();

                    expectedMaxThrust = AttachedReactor.MaximumPower * GetPowerThrustModifier() * GetHeatThrustModifier() / PluginHelper.GravityConstant / _maxISP * GetHeatExchangerThrustDivisor();
                    calculatedMaxThrust = expectedMaxThrust;
                    expectedMaxThrust *= _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / 200f);

                    max_fuel_flow_rate = expectedMaxThrust / _maxISP / PluginHelper.GravityConstant;

                    UpdateAtmosphericPresureTreshold();

                    var thrustAtmosphereRatio = expectedMaxThrust <= 0 ? 0 : Math.Max(0, expectedMaxThrust - pressureTreshold) / expectedMaxThrust;

                    current_isp = _maxISP * thrustAtmosphereRatio;

                    calculatedMaxThrust = Math.Max((calculatedMaxThrust - pressureTreshold), 0.00001);
                    calculatedMaxThrust *= _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / sootThrustDivider);

                    FloatCurve newISP = new FloatCurve();

                    var effectiveIsp = isJet ? Math.Min(current_isp, PluginHelper.MaxThermalNozzleIsp) : current_isp;

                    newISP.Add(0, (float)effectiveIsp, 0, 0);
                    myAttachedEngine.atmosphereCurve = newISP;

                    if (myAttachedEngine.useVelCurve)
                    {
                        vcurveAtCurrentVelocity = myAttachedEngine.velCurve.Evaluate((float)vessel.srf_velocity.magnitude);

                        if (vcurveAtCurrentVelocity > 0 && !float.IsInfinity(vcurveAtCurrentVelocity) && !float.IsNaN(vcurveAtCurrentVelocity))
                        {
                            calculatedMaxThrust *= vcurveAtCurrentVelocity;
                        }
                        else
                        {
                            max_fuel_flow_rate = 0;
                            calculatedMaxThrust = 0;
                        }
                    }

                    // prevent to low number of maxthrust 
                    if (calculatedMaxThrust <= 0.00001f)
                    {
                        calculatedMaxThrust = 0.00001f;
                        max_fuel_flow_rate = 0;
                    }

                    myAttachedEngine.maxThrust = (float)calculatedMaxThrust;

                    // set engines maximum fuel flow
                    myAttachedEngine.maxFuelFlow = (float)Math.Max(Math.Min(1000f, max_fuel_flow_rate), 0.0000000001);

                    if (pulseDuration == 0 && myAttachedEngine is ModuleEnginesFX && !String.IsNullOrEmpty(_particleFXName))
                    {
                        
                        part.Effect(_particleFXName, 0, -1);
                    }
                }

                //tell static helper methods we are currently updating things
                static_updating = true;
                static_updating2 = true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnFixedUpdate " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void UpdateAtmosphericPresureTreshold()
        {
            if (!_currentpropellant_is_jet)
            {
                var staticPresure = HighLogic.LoadedSceneIsFlight 
                    ? (float)FlightGlobals.getStaticPressure(vessel.transform.position) 
                    : (float)GameConstants.EarthAtmospherePressureAtSeaLevel;

                pressureTreshold = exitArea * staticPresure;
                if (_maxISP > GameConstants.MaxThermalNozzleIsp && !limitedByMaxThermalNozzleIsp)
                    pressureTreshold *= 2;
            }
            else
                pressureTreshold = 0;
        }

        private void UpdateAnimation()
        {
            try
            {
                float increase;

                if (myAttachedEngine.currentThrottle > 0 && calculatedMaxThrust > 0)
                    increase = TimeWarp.fixedDeltaTime;
                else if (currentAnimatioRatio > 1 / recoveryAnimationDivider)
                    increase = TimeWarp.fixedDeltaTime;
                else if (currentAnimatioRatio > 0)
                    increase = TimeWarp.fixedDeltaTime / -recoveryAnimationDivider;
                else
                    increase = 0;

                currentAnimatioRatio += increase;

                if (pulseDuration > 0 && !String.IsNullOrEmpty(_particleFXName) && myAttachedEngine is ModuleEnginesFX)
                {
                    if (increase > 0 && calculatedMaxThrust > 0 && myAttachedEngine.currentThrottle > 0 && currentAnimatioRatio < pulseDuration)
                        part.Effect(_particleFXName, 1 - currentAnimatioRatio / pulseDuration, -1);
                    else
                        part.Effect(_particleFXName, 0, -1);
                }

                if (pulseDuration > 0 && calculatedMaxThrust > 0 && increase > 0 && myAttachedEngine.currentThrottle > 0 && currentAnimatioRatio < pulseDuration)
                    PluginHelper.SetAnimationRatio(1, emiAnimationState);
                else
                    PluginHelper.SetAnimationRatio(0, emiAnimationState);

                if (currentAnimatioRatio > 1 + (2 - (myAttachedEngine.currentThrottle * 2)))
                    currentAnimatioRatio = 0;

                PluginHelper.SetAnimationRatio(Math.Max(Math.Min(currentAnimatioRatio, 1), 0), pulseAnimationState);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error UpdateAnimation " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private float currentAnimatioRatio;

        private void GenerateThrustFromReactorHeat()
        {
            try
            {

                if (!AttachedReactor.IsActive)
                    AttachedReactor.EnableIfPossible();

                GetMaximumIspAndThrustMultiplier();

                var chargedPowerModifier = _isNeutronAbsorber ? 1 : (AttachedReactor.FullPowerForNonNeutronAbsorbants ? 1 : AttachedReactor.ChargedPowerRatio);

                thermal_modifiers = myAttachedEngine.currentThrottle * GetAtmosphericLimit() * AttachedReactor.GetFractionThermalReciever(id) * chargedPowerModifier;

                var maximum_requested_thermal_power = _currentMaximumPower * thermal_modifiers;

                var neutronAbsorbingModifier = _isNeutronAbsorber ? 1 : (AttachedReactor.FullPowerForNonNeutronAbsorbants ? 1 : 0);
                requested_thermal_power = Math.Min(_availableThermalPower * thermal_modifiers, AttachedReactor.MaximumThermalPower * delayedThrottle * neutronAbsorbingModifier);

                thermal_power_received = consumeFNResource(requested_thermal_power * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER) * AttachedReactor.ThermalPropulsionEfficiency / TimeWarp.fixedDeltaTime;

                if (thermal_power_received < maximum_requested_thermal_power)
                {
                    var chargedParticleRatio = (float)Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_CHARGED_PARTICLES), 2);
                    requested_charge_particles = (float)Math.Min((maximum_requested_thermal_power - thermal_power_received), AttachedReactor.MaximumChargedPower) * chargedParticleRatio;

                    thermal_power_received += consumeFNResource(requested_charge_particles * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES) / TimeWarp.fixedDeltaTime;
                }

                UpdateSootAccumulation();

                var sootModifier = sootHeatDivider > 0 ? 1f - (sootAccumulationPercentage / sootHeatDivider) : 1;
                var wasteheatEfficiencyModifier = _maxISP > GameConstants.MaxThermalNozzleIsp ? wasteheatEfficiencyHighTemperature : wasteheatEfficiencyLowTemperature;

                consumedWasteHeat = sootModifier * wasteheatEfficiencyModifier * thermal_power_received;

                // consume wasteheat
                consumeFNResource(consumedWasteHeat * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);

                // calculate max thrust
                heatExchangerThrustDivisor = GetHeatExchangerThrustDivisor();

                if (_availableThermalPower > 0 && _maxISP > 0)
                {
                    var ispRatio = _currentpropellant_is_jet ? current_isp / _maxISP : 1;
                    var thrustLimit = myAttachedEngine.thrustPercentage / 100f;
                    engineMaxThrust = Math.Max(thrustLimit * GetPowerThrustModifier() * GetHeatThrustModifier() * thermal_power_received / _maxISP / PluginHelper.GravityConstant * heatExchangerThrustDivisor * ispRatio / myAttachedEngine.currentThrottle, 0.001f);
                    calculatedMaxThrust = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumPower / _maxISP / PluginHelper.GravityConstant * heatExchangerThrustDivisor * ispRatio;
                }
                else
                {
                    engineMaxThrust = 0.001f;
                    calculatedMaxThrust = 0;
                }

                max_thrust_in_space = myAttachedEngine.thrustPercentage > 0
                    ? engineMaxThrust / myAttachedEngine.thrustPercentage * 100
                    : 0;

                var nozzleStaticPresure = (float)FlightGlobals.getStaticPressure(vessel.transform.position);

                max_thrust_in_current_atmosphere = max_thrust_in_space;

                UpdateAtmosphericPresureTreshold();

                // update engine thrust/ISP for thermal noozle
                if (!_currentpropellant_is_jet)
                {
                    max_thrust_in_current_atmosphere = Math.Max(max_thrust_in_space - pressureTreshold, Math.Max(myAttachedEngine.currentThrottle * 0.01, 0.0000000001));

                    var thrustAtmosphereRatio = max_thrust_in_space > 0 ? Math.Max(max_thrust_in_current_atmosphere / max_thrust_in_space, 0.01) : 0.01f;
                    UpdateIspEngineParams(thrustAtmosphereRatio);
                    current_isp = _maxISP * thrustAtmosphereRatio;
                    calculatedMaxThrust = Math.Max((calculatedMaxThrust - pressureTreshold), 0.0000000001);
                }
                else
                    current_isp = _maxISP;

                if (!Double.IsInfinity(max_thrust_in_current_atmosphere) && !double.IsNaN(max_thrust_in_current_atmosphere))
                {
                    final_max_engine_thrust = max_thrust_in_current_atmosphere * _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / sootThrustDivider);
                    calculatedMaxThrust *= _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / sootThrustDivider);
                }
                else
                {
                    final_max_engine_thrust = 0.0000000001;
                    calculatedMaxThrust = final_max_engine_thrust;
                }

                // amount of fuel being used at max throttle with no atmospheric limits
                if (_maxISP <= 0) return;

                // calculate maximum fuel flow rate
                max_fuel_flow_rate = final_max_engine_thrust / current_isp / PluginHelper.GravityConstant / myAttachedEngine.currentThrottle;

                if (myAttachedEngine.useVelCurve && myAttachedEngine.velCurve != null)
                {
                    vcurveAtCurrentVelocity = myAttachedEngine.velCurve.Evaluate((float)(vessel.speed / vessel.speedOfSound));

                    if (vcurveAtCurrentVelocity > 0 && !float.IsInfinity(vcurveAtCurrentVelocity) && !float.IsNaN(vcurveAtCurrentVelocity))
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

                    if (atmosphereModifier > 0 && !float.IsInfinity(atmosphereModifier) && !float.IsNaN(atmosphereModifier))
                    {
                        max_fuel_flow_rate = Math.Max(max_fuel_flow_rate * atmosphereModifier, 0.0000000001);
                        calculatedMaxThrust *= atmosphereModifier;
                    }
                    else
                    {
                        max_fuel_flow_rate = 0.0000000001;
                        calculatedMaxThrust = 0;
                    }
                }

                if (calculatedMaxThrust <= 0.00001f)
                {
                    calculatedMaxThrust = 0.00001f;
                    max_fuel_flow_rate = 0.0000000001;
                }

                if (!_currentpropellant_is_jet)
                    myAttachedEngine.maxThrust = (float)Math.Max(calculatedMaxThrust, 0.0001);
                else
                    myAttachedEngine.maxThrust = (float)Math.Max(engineMaxThrust, 0.0001);

                if (atmospheric_limit > 0 && atmospheric_limit != 1 && !float.IsInfinity(atmospheric_limit) && !float.IsNaN(atmospheric_limit))
                    max_fuel_flow_rate = Math.Max(max_fuel_flow_rate * atmospheric_limit, 0.0000000001);

                // set engines maximum fuel flow
                myAttachedEngine.maxFuelFlow = (float)Math.Max(Math.Min(1000, max_fuel_flow_rate), 0.0000000001);

				// Calculate
                pre_coolers_active = _vesselPrecoolers.Sum(prc => prc.ValidAttachedIntakes); ;
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

                engineHeatProduction = (max_fuel_flow_rate >= engineHeatFuelThreshold && _maxISP > 100 && part.mass > 0.001)
                    ? baseHeatProduction * PluginHelper.EngineHeatProduction / max_fuel_flow_rate / _maxISP / part.mass
                    : baseHeatProduction;

                engineHeatProduction *= (1 + airflowHeatModifier * PluginHelper.AirflowHeatMult);

				myAttachedEngine.heatProduction = (float)engineHeatProduction;

                if (pulseDuration == 0 && myAttachedEngine is ModuleEnginesFX && !String.IsNullOrEmpty(_particleFXName))
                {
                    part.Effect(_particleFXName, (float)Math.Max(0.1f * myAttachedEngine.currentThrottle, Math.Min(Math.Pow(thermal_power_received / requested_thermal_power, 0.5), delayedThrottle)), -1);
                }

                //if (_fuelToxicity > 0 && max_fuel_flow_rate > 0 && nozzleStaticPresure > 1)
                //{
                //    _savedReputationCost += max_fuel_flow_rate * _fuelToxicity * TimeWarp.fixedDeltaTime * Mathf.Pow(nozzleStaticPresure / 100, 3);
                //    if (_savedReputationCost > 1)
                //    {
                //        float flooredReputationCost = (int)Math.Floor(_savedReputationCost);

                //        if (Reputation.Instance != null)
                //            Reputation.Instance.addReputation_discrete(-flooredReputationCost, TransactionReasons.None);
                //        else
                //            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - No Reputation found, was not able to reduce reputation by " + flooredReputationCost);

                //        ScreenMessages.PostScreenMessage("You are poisoning the environment with " + _fuelmode + " from your exhaust!", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                //        _savedReputationCost -= flooredReputationCost;
                //    }
                //}
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error GenerateThrustFromReactorHeat " + e.Message + " Source: " + e.Source + " Stack trace: " + e.StackTrace);
            }
        }

        private void UpdateSootAccumulation()
        {
            if (myAttachedEngine.currentThrottle > 0 && _propellantSootFactorFullThrotle != 0 || _propellantSootFactorMinThrotle != 0)
            {
                float sootEffect;

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
                sootAccumulationPercentage -= TimeWarp.fixedDeltaTime * myAttachedEngine.currentThrottle * 0.1f;
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
            baseMaxIsp = (float)Math.Sqrt(AttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset);

            if (baseMaxIsp > GameConstants.MaxThermalNozzleIsp && limitedByMaxThermalNozzleIsp)
                baseMaxIsp = GameConstants.MaxThermalNozzleIsp;

            _maxISP = baseMaxIsp * GetIspPropellantModifier();
        }

        public override string GetInfo()
        {
            bool upgraded = false;
            if (this.HasTechsRequiredToUpgrade())
                upgraded = true;

            ConfigNode[] prop_nodes = upgraded && isJet ? getPropellantsHybrid() : getPropellants(isJet);

            string return_str = "Thrust: Variable\n";
            foreach (ConfigNode propellant_node in prop_nodes)
            {
                float ispMultiplier = float.Parse(propellant_node.GetValue("ispMultiplier"));
                string guiname = propellant_node.GetValue("guiName");
                return_str = return_str + "--" + guiname + "--\n" + "ISP: " + ispMultiplier.ToString("0.000") + " x " + (PluginHelper.IspCoreTempMult + IspTempMultOffset).ToString("0.000") + " x Sqrt(Core Temperature)" + "\n";
            }
            return return_str;
        }

        public override int getPowerPriority()
        {
            return 1;
        }

        // Static Methods
        // Amount of intake air available to use of a particular resource type
        public static double getIntakeAvailable(Vessel vess, string resourcename)
        {
            List<IEngineNoozle> nozzles = vess.FindPartModulesImplementing<IEngineNoozle>();
            bool updating = true;
            foreach (IEngineNoozle nozzle in nozzles)
            {
                if (!nozzle.Static_updating)
                {
                    updating = false;
                    break;
                }
            }

            if (updating)
            {
                nozzles.ForEach(nozzle => nozzle.Static_updating = false);
                //List<PartResource> partresources = vess.rootPart.GetConnectedResources(resourcename).ToList();
                var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resourcename);

                double currentintakeatm = 0;
                double maxintakeatm = 0;

                //partresources.ForEach(partresource => currentintakeatm += partresource.amount);
                //partresources.ForEach(partresource => maxintakeatm += partresource.maxAmount);

                vess.rootPart.GetConnectedResourceTotals(resourceDefinition.id, out currentintakeatm, out maxintakeatm);

                //intake_amounts[resourcename] = currentintakeatm;
                //intake_maxamounts[resourcename] = maxintakeatm;

                // to smooth out occilations in resource production, take a 90/10 weighted average of the previous frame production
                if (intake_amounts.ContainsKey(resourcename))
                    intake_amounts[resourcename] = (intake_amounts[resourcename] * 0.99) + (currentintakeatm * 0.01);
                else
                    intake_amounts[resourcename] = currentintakeatm;
                if (intake_maxamounts.ContainsKey(resourcename))
                    intake_maxamounts[resourcename] = (intake_maxamounts[resourcename] * 0.99) + (maxintakeatm * 0.01);
                else
                    intake_maxamounts[resourcename] = maxintakeatm;
            }

            if (intake_amounts.ContainsKey(resourcename))
                return Math.Max(intake_amounts[resourcename], 0);

            return 0.00001;
        }

        // enumeration of the fuel useage rates of all jets on a vessel
        public double GetFuelRateThermalJets(string resourcename)
        {
            int engines = 0;
            bool updating = true;
            foreach (IEngineNoozle nozzle in _vesselThermalNozzles)
            {
                ConfigNode[] prop_node = nozzle.getPropellants();

                if (prop_node == null) continue;

                ConfigNode[] assprops = prop_node[nozzle.Fuel_mode].GetNodes("PROPELLANT");

                if (prop_node[nozzle.Fuel_mode] == null || !assprops[0].GetValue("name").Equals(resourcename)) continue;

                if (!nozzle.Static_updating2)
                    updating = false;

                if (nozzle.GetNozzleFlowRate() > 0)
                    engines++;
            }

            if (updating)
            {
                double enum_rate = 0;
                foreach (IEngineNoozle nozzle in _vesselThermalNozzles)
                {
                    ConfigNode[] prop_node = nozzle.getPropellants();

                    if (prop_node == null) continue;

                    ConfigNode[] assprops = prop_node[nozzle.Fuel_mode].GetNodes("PROPELLANT");

                    if (prop_node[nozzle.Fuel_mode] == null || !assprops[0].GetValue("name").Equals(resourcename)) continue;

                    enum_rate += nozzle.GetNozzleFlowRate();
                    nozzle.Static_updating2 = false;
                }

                if (fuel_flow_amounts.ContainsKey(resourcename))
                    fuel_flow_amounts[resourcename] = enum_rate;
                else
                    fuel_flow_amounts.Add(resourcename, enum_rate);
            }

            if (fuel_flow_amounts.ContainsKey(resourcename))
                return fuel_flow_amounts[resourcename];

            return 0.1;
        }


        public static ConfigNode[] getPropellants(bool isJet)
        {
            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - getPropellants");

            ConfigNode[] propellantlist = isJet
                ? GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT")
                : GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");

            if (propellantlist == null)
                PluginHelper.showInstallationErrorMessage();

            return propellantlist;
        }

        private double GetHeatThrustModifier()
        {
            float coretempthreshold = PluginHelper.ThrustCoreTempThreshold;
            float lowcoretempbase = PluginHelper.LowCoreTempBaseThrust;

            return coretempthreshold <= 0
                ? 1.0f
                : AttachedReactor.CoreTemperature < coretempthreshold
                    ? (AttachedReactor.CoreTemperature + lowcoretempbase) / (coretempthreshold + lowcoretempbase)
                    : 1.0f + PluginHelper.HighCoreTempThrustMult * Math.Max(Math.Log10(AttachedReactor.CoreTemperature / coretempthreshold), 0);
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

        private float GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginHelper.GlobalThermalNozzlePowerMaxThrustMult * CurrentPowerThrustMultiplier;
        }

        private void UpdateRadiusModifier()
        {
            if (_myAttachedReactor != null)
            {
                // re-attach with updated radius
                _myAttachedReactor.DetachThermalReciever(id);
                _myAttachedReactor.AttachThermalReciever(id, radius);

                Fields["vacuumPerformance"].guiActiveEditor = true;
                Fields["radiusModifier"].guiActiveEditor = true;
                Fields["surfacePerformance"].guiActiveEditor = true;

                var heatExchangerThrustDivisor = GetHeatExchangerThrustDivisor();

                radiusModifier = (heatExchangerThrustDivisor * 100.0).ToString("0.00") + "%";

                UpdateMaxIsp();

                var max_thrust_in_space = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumThermalPower / _maxISP / PluginHelper.GravityConstant * heatExchangerThrustDivisor;

                var final_max_thrust_in_space = max_thrust_in_space * _thrustPropellantMultiplier;

                var isp_in_space = heatExchangerThrustDivisor * _maxISP;

                vacuumPerformance = final_max_thrust_in_space.ToString("0.0") + "kN @ " + isp_in_space.ToString("0.0") + "s";

                maxPressureThresholdAtKerbinSurface = exitArea * (float)GameConstants.EarthAtmospherePressureAtSeaLevel;

                var maxSurfaceThrust = Math.Max(max_thrust_in_space - (maxPressureThresholdAtKerbinSurface), 0.00001);

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


        private float storedFractionThermalReciever;
        private float GetHeatExchangerThrustDivisor()
        {
            if (AttachedReactor == null || AttachedReactor.GetRadius() == 0 || radius == 0) return 0;

            if (_myAttachedReactor.GetFractionThermalReciever(id) == 0) return storedFractionThermalReciever;

            storedFractionThermalReciever = _myAttachedReactor.GetFractionThermalReciever(id);

            var fractionalReactorRadius = Mathf.Sqrt(Mathf.Pow(AttachedReactor.GetRadius(), 2) * storedFractionThermalReciever);

            // scale down thrust if it's attached to the wrong sized reactor
            float heat_exchanger_thrust_divisor = radius > fractionalReactorRadius
                ? fractionalReactorRadius * fractionalReactorRadius / radius / radius
                : normalizeFraction(radius / (float)fractionalReactorRadius, 1f);

            if (!_currentpropellant_is_jet)
            {
                for (int i = 0; i < partDistance; i++)
                {
                    heat_exchanger_thrust_divisor *= AttachedReactor.ThermalTransportationEfficiency;
                }
            }

            return heat_exchanger_thrust_divisor;
        }

        private float normalizeFraction(float variable, float normalizer)
        {
            return (normalizer + variable) / (1f + normalizer);
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


    }
}
