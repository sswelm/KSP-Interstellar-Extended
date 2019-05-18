using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("#LOC_KSPIE_ElectricEngine_partModuleName")]
    class ElectrostaticEngineControllerFX : ElectricEngineControllerFX { }


    [KSPModule("#LOC_KSPIE_ElectricEngine_partModuleName")]
    class ElectricEngineControllerFX : ResourceSuppliableModule, IUpgradeableModule, IRescalable<ElectricEngineControllerFX>, IPartMassModifier
    {
        [KSPField(isPersistant = true)]
        public double storedAbsoluteFactor = 1;

        // Persistent True
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public string propellantName;
        [KSPField(isPersistant = true)]
        public string propellantGUIName;
        [KSPField(isPersistant = true)]
        public bool propellantIsSaved;

        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = true)]
        public bool vacplasmaadded;

        //Persistent False
        [KSPField]
        public string upgradeTechReq = "";
        [KSPField]
        public string gearsTechReq = "";
        [KSPField]
        public double powerReqMult = 1; 
        [KSPField]
        public int type;
        [KSPField]
        public int upgradedtype = 0;
        [KSPField]
        public double baseISP = 1000;
        [KSPField]
        public double ispGears = 1;
        [KSPField]
        public double exitArea = 0;
        [KSPField]
        public double powerThrustMultiplier = 1.0;
        [KSPField]
        public float upgradeCost = 0;
        [KSPField]
        public string originalName = "";
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public double baseEfficency = 0.3;
        [KSPField]
        public double variableEfficency = 0.3;
        [KSPField]
        public float storedThrotle;
        [KSPField]
        public double particleEffectMult = 1;
        [KSPField]
        public bool ignoreWasteheat = false;
        [KSPField]
        public double GThreshold = 9;

        [KSPField]
        public double Mk1Power = 1;
        [KSPField]
        public double Mk2Power = 1;
        [KSPField]
        public double Mk3Power = 1;
        [KSPField]
        public double Mk4Power = 1;
        [KSPField]
        public double Mk5Power = 1;
        [KSPField]
        public double Mk6Power = 1;
        [KSPField]
        public double Mk7Power = 1;

        [KSPField]
        public string Mk2Tech = "";
        [KSPField]
        public string Mk3Tech = "";
        [KSPField]
        public string Mk4Tech = "";
        [KSPField]
        public string Mk5Tech = "";
        [KSPField]
        public string Mk6Tech = "";
        [KSPField]
        public string Mk7Tech = "";

        // GUI
        [KSPField(guiActive = true, guiName = "#autoLOC_6001377", guiUnits = "#autoLOC_7001408", guiFormat = "F6")]
        public double thrust_d;
        [KSPField(guiActive = false, guiName = "Calculated Thrust", guiFormat = "F6", guiUnits = "kN")]
        public double calculated_thrust;
        [KSPField(guiActive = false)]
        public double simulated_max_thrust;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_warpIsp", guiFormat = "F1", guiUnits = "s")]
        public double engineIsp;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_maxPowerInput", guiUnits = " MW")]
        public double scaledMaxPower = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineMass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "expected mass", guiUnits = " t")]
        public double expectedMass = 0;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "desired mass", guiUnits = " t")]
        public double desiredMass = 0;
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineType")]
        public string engineTypeStr = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_activePropellantName")]
        public string propNameStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerShare")]
        public string electricalPowerShareStr = "";
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_powerRequested", guiFormat = "F3", guiUnits = " MW")]
        public double power_request;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_propellantEfficiency")]
        public string efficiencyStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_overheatEfficiency")]
        public string thermalEfficiency = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_heatProduction")]
        public string heatProductionStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_upgradeCost")]
        public string upgradeCostStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_maxEffectivePower", guiFormat = "F3", guiUnits = " MW")]
        public double maxEffectivePower;
        [KSPField(guiActive = false)]
        public double currentPropellantEfficiency;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_maxThrottlePower", guiFormat = "F3", guiUnits = " MW")]
        public double modifiedMaxThrottlePower;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")]
        public double timeDilation;

        // privates
        const double OneThird = 1d / 3d;

        double _currentPropellantEfficiency;
        double _speedOfLight;
        double _modifiedEngineBaseIsp;
        double _electrical_share_f;

        double _electrical_consumption_f;
        double _heat_production_f;
        double _modifiedCurrentPropellantIspMultiplier;
        double _maxIsp;
        double _effectiveIsp;
        double _ispPersistent;
        int vesselChangedSIOCountdown;

        [KSPField(guiActive = false)]
        protected double currentPowerForEngine;
        [KSPField(guiActive = false)]
        protected double availablePowerForEngine;
        [KSPField(guiActive = false)]
        protected double effectiveRecievedPower;
        [KSPField(guiActive = false)]
        protected double effectiveSimulatedPower;
        
        [KSPField(guiActive = false)]
        protected double effectivePowerThrustModifier;
        [KSPField(guiActive = false)]
        public double actualPowerReceived;
        [KSPField(guiActive = false)]
        public double simulatedPowerReceived;

        [KSPField(guiActive = false, guiName = "Capacity Modifier")]
        protected double powerCapacityModifier = 1;
        [KSPField(guiActive = false, guiName = "Atm Trust Efficiency")]
        protected double _atmosphereThrustEfficiency;
        [KSPField(guiActive = true, guiName = "Atm Trust Efficiency", guiFormat = "F3", guiUnits = "%")]
        protected double _atmosphereThrustEfficiencyPercentage;
        [KSPField(guiActive = false, guiName = "Max Fuel Flow Rate")]
        protected float _maxFuelFlowRate;
        [KSPField(guiActive = false, guiName = "Current Space Fuel Flow Rate")]
        protected double _currentSpaceFuelFlowRate;
        [KSPField(guiActive = false, guiName = "Potential Space Fuel Flow Rate")]
        protected double _simulatedSpaceFuelFlowRate;
        [KSPField(guiActive = false, guiName = "Fuel Flow Modifier")]
        protected double _fuelFlowModifier;
        [KSPField(guiActive = true, guiName = "Current Thrust in Space", guiFormat = "F6", guiUnits = " kN")]
        protected double currentThrustInSpace;
        [KSPField(guiActive = true, guiName = "Max Thrust in Space", guiFormat = "F6", guiUnits = " kN")]
        protected double simulatedThrustInSpace;

        [KSPField]
        protected double maxThrustFromPower = 0.001;
        [KSPField]
        protected double effectPower = 0;
        [KSPField]
        public string EffectName = String.Empty;
        [KSPField]
        protected string _particleFXName;
        [KSPField]
        public double massTweakscaleExponent = 3;
        [KSPField]
        public double powerExponent = 3;
        [KSPField]
        public double massExponent = 3;
        [KSPField]
        public double maxPower = 1000;

        int _rep;
        int _initializationCountdown;
        int _numberOfAvailableUpgradeTechs;

        bool _hasrequiredupgrade;
        bool _hasGearTechnology;
        bool _warpToReal;
        bool _isFullyStarted;

        ResourceBuffers resourceBuffers;
        FloatCurve _ispFloatCurve;
        List<ElectricEnginePropellant> _propellants;
        ModuleEngines _attachedEngine;

        // Properties
        public string UpgradeTechnology { get { return upgradeTechReq; } }
        public double MaxPower { get { return scaledMaxPower * powerReqMult * powerCapacityModifier; } }
        public double MaxEffectivePower { get { return ignoreWasteheat ? MaxPower :  MaxPower * CurrentPropellantEfficiency * ThermalEfficiency; } }
        public bool IsOperational {get { return _attachedEngine != null ? _attachedEngine.isOperational : false; } }

        public double PowerCapacityModifier
        {
            get
            {
                switch (_numberOfAvailableUpgradeTechs)
                {
                    case 0:
                        return Mk1Power;
                    case 1:
                        return Mk2Power;
                    case 2:
                        return Mk3Power;
                    case 3:
                        return Mk4Power;
                    case 4:
                        return Mk5Power;
                    case 5:
                        return Mk6Power;
                    case 6:
                        return Mk7Power;
                    default:
                        return 1;
                }
            }
        }

        private void DetermineTechLevel()
        {
            _numberOfAvailableUpgradeTechs = 0;
            if (PluginHelper.UpgradeAvailable(Mk2Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk3Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk4Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk5Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk6Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk7Tech))
                _numberOfAvailableUpgradeTechs++;
        }

        private ElectricEnginePropellant _current_propellant = null;
        public ElectricEnginePropellant Current_propellant
        {
            get { return _current_propellant; }
            set
            {
                if (value == null)
                    return;

                _current_propellant = value;
                propellantIsSaved = true;
                fuel_mode = _propellants.IndexOf(_current_propellant);
                propellantName = _current_propellant.PropellantName;
                propellantGUIName = _current_propellant.PropellantGUIName;
                _modifiedCurrentPropellantIspMultiplier = CurrentIspMultiplier;
            }
        }

        public double CurrentIspMultiplier
        {
            get 
            { 
                return type == (int)ElectricEngineType.VASIMR || type == (int)ElectricEngineType.ARCJET 
                ? Current_propellant.DecomposedIspMult 
                : Current_propellant.IspMultiplier; 
            }
        }

        public double ThermalEfficiency
        {
            get 
            { 
            return HighLogic.LoadedSceneIsFlight 
                ? CheatOptions.IgnoreMaxTemperature || ignoreWasteheat 
                    ? 1 
                    : (1 - getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT)) 
                : 1; 
            }
        }

        public double CurrentPropellantThrustMultiplier
        {
            get { return type == (int)ElectricEngineType.ARCJET ? Current_propellant.ThrustMultiplier : 1; }
        }

        public double CurrentPropellantEfficiency
        {
            get
            {
                var atmDensity = HighLogic.LoadedSceneIsFlight ? vessel.atmDensity : 0;

                if (type == (int)ElectricEngineType.ARCJET)
                    _currentPropellantEfficiency = 0.87 * Current_propellant.Efficiency;
                else if (type == (int)ElectricEngineType.VASIMR)
                    _currentPropellantEfficiency =  Math.Max(1 - atmDensity, 0.00001) * (baseEfficency + ((1 - _attachedEngine.currentThrottle) * variableEfficency));
                else
                    _currentPropellantEfficiency = Current_propellant.Efficiency;

                if (Current_propellant.IsInfinite)
                    _currentPropellantEfficiency += lightSpeedRatio;

                return _currentPropellantEfficiency;
            }
        }

        public void VesselChangedSOI()
        {
            vesselChangedSIOCountdown = 10;
        }

        // Events
        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_nextPropellant", active = true)]
        public void ToggleNextPropellantEvent()
        {
            ToggleNextPropellant();
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_previous Propellant", active = true)]
        public void TogglePreviousPropellantEvent()
        {
            TogglePreviousPropellant();
        }

        public void OnRescale(ScalingFactor factor)
        {
            storedAbsoluteFactor = (double)(decimal)factor.absolute.linear;

            ScaleParameters();
        }

        private void ScaleParameters()
        {
            expectedMass = (double)(decimal)part.prefabMass * Math.Pow(storedAbsoluteFactor, massTweakscaleExponent);
            desiredMass = (double)(decimal)part.prefabMass * Math.Pow(storedAbsoluteFactor, massExponent);
            scaledMaxPower = maxPower * Math.Pow(storedAbsoluteFactor, powerExponent);
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return (float)(desiredMass - expectedMass);
        }
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_retrofit", active = true)]
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null) return;
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        // Actions
        [KSPAction("#LOC_KSPIE_ElectricEngine_nextPropellant")]
        public void ToggleNextPropellantAction(KSPActionParam param)
        {
            ToggleNextPropellantEvent();
        }

        [KSPAction("#LOC_KSPIE_ElectricEngine_previous Propellant")]
        public void TogglePreviousPropellantAction(KSPActionParam param)
        {
            TogglePreviousPropellantEvent();
        }

        // Methods
        private void UpdateEngineTypeString()
        {
            engineTypeStr = isupgraded ? upgradedName : originalName;
        }

        public override void OnLoad(ConfigNode node)
        {
            if (isupgraded)
                upgradePartModule();
            UpdateEngineTypeString();
        }

        public override void OnStart(PartModule.StartState state)
        {
            ScaleParameters();

            _initializationCountdown = 10;
            Debug.Log("[KSPI]: Start Initializing ElectricEngineControllerFX");
            try
            {
                // initialise resources
                this.resources_to_supply = new [] { ResourceManager.FNRESOURCE_WASTEHEAT };
                base.OnStart(state);

                AttachToEngine();
                DetermineTechLevel();
                powerCapacityModifier = PowerCapacityModifier;

                _ispFloatCurve = new FloatCurve();
                _ispFloatCurve.Add(0, (float)baseISP);
                _speedOfLight = GameConstants.speedOfLight * PluginHelper.SpeedOfLightMult;
                _hasGearTechnology = String.IsNullOrEmpty(gearsTechReq) || PluginHelper.UpgradeAvailable(gearsTechReq);
                _modifiedEngineBaseIsp = baseISP * PluginHelper.ElectricEngineIspMult;
                _hasrequiredupgrade = this.HasTechsRequiredToUpgrade();

                if (_hasrequiredupgrade && (isupgraded || state == StartState.Editor))
                    upgradePartModule();

                UpdateEngineTypeString();

                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 2.0e+4, true));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, (double)(decimal)this.part.mass);
                resourceBuffers.Init(this.part);

                InitializePropellantMode();

                SetupPropellants(true);

                _attachedEngine.maxThrust = (float)maxThrustFromPower;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Error OnStart ElectricEngineControllerFX " + e.Message);
            }
            Debug.Log("[KSPI]: End Initializing ElectricEngineControllerFX");
        }

        private void InitializePropellantMode()
        {
            // initialize propellant
            _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);

            if (propellantIsSaved || HighLogic.LoadedSceneIsEditor)
            {
                if (!string.IsNullOrEmpty(propellantName))
                {
                    Current_propellant = _propellants.FirstOrDefault(m => m.PropellantName == propellantName);

                    if (Current_propellant == null)
                        Current_propellant = _propellants.FirstOrDefault(m => m.PropellantGUIName == propellantName);
                }

                if (Current_propellant == null && !string.IsNullOrEmpty(propellantGUIName))
                {
                    Current_propellant = _propellants.FirstOrDefault(m => m.PropellantName == propellantGUIName);

                    if (Current_propellant == null)
                        Current_propellant = _propellants.FirstOrDefault(m => m.PropellantGUIName == propellantGUIName);
                }
            }
            if (_propellants == null)
                Debug.LogWarning("[KSPI]: SetupPropellants _propellants is still null");

            if (Current_propellant == null)
                Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.First();
        }

        private void AttachToEngine()
        {
            _attachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (_attachedEngine != null)
            {
                var finalTrustField = _attachedEngine.Fields["finalThrust"];
                finalTrustField.guiActive = false;

                var realIspField = _attachedEngine.Fields["realIsp"];
                realIspField.guiActive = false;

                //_attachedEngine.Fields["finalThrust"].guiFormat = "F5";
            }
        }

        private void SetupPropellants(bool moveNext)
        {
            try
            {
                Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.First();

                if ((Current_propellant.SupportedEngines & type) != type)
                {
                    _rep++;
                    Debug.LogWarning("[KSPI]: SetupPropellants TogglePropellant");
                    TogglePropellant(moveNext);
                    return;
                }

                var listOfPropellants = new List<Propellant>();
                listOfPropellants.Add(Current_propellant.Propellant);

                // if all propellant exist
                if (!listOfPropellants.Exists(prop => PartResourceLibrary.Instance.GetDefinition(prop.name) == null))
                {
                    //Get the Ignition state, i.e. is the engine shutdown or activated
                    var engineState = _attachedEngine.getIgnitionState;

                    _attachedEngine.Shutdown();

                    var newPropNode = new ConfigNode();
                    foreach (var prop in listOfPropellants)
                    {
                        ConfigNode propellantConfigNode = newPropNode.AddNode("PROPELLANT");
                        propellantConfigNode.AddValue("name", prop.name);
                        propellantConfigNode.AddValue("ratio", prop.ratio);
                        propellantConfigNode.AddValue("DrawGauge", prop.drawStackGauge);
                    }
                    _attachedEngine.Load(newPropNode);

                    if (engineState == true)
                        _attachedEngine.Activate();
                }
                else if (_rep < _propellants.Count)
                {
                    _rep++;
                    TogglePropellant(moveNext);
                    return;
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    // you can have any fuel you want in the editor but not in flight
                    var allVesselResourcesNames = part.vessel.parts.SelectMany(m => m.Resources).Select(m => m.resourceName).Distinct();
                    if (!listOfPropellants.All(prop => allVesselResourcesNames.Contains(prop.name)) && _rep < _propellants.Count)
                    {
                        _rep++;
                        TogglePropellant(moveNext);
                        return;
                    }
                }

                _rep = 0;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: SetupPropellants ElectricEngineControllerFX " + e.Message);
            }
        }

        public override void OnUpdate()
        {
            // Base class update
            base.OnUpdate();

            // stop engines and drop out of timewarp when X pressed
            if (vessel.packed && storedThrotle > 0 && Input.GetKeyDown(KeyCode.X))
            {
                // Return to realtime
                TimeWarp.SetRate(0, true);

                storedThrotle = 0;
                vessel.ctrlState.mainThrottle = storedThrotle;
            }

            // When transitioning from timewarp to real update throttle
            if (_warpToReal)
            {
                vessel.ctrlState.mainThrottle = storedThrotle;
                _warpToReal = false;
            }

            if (ResearchAndDevelopment.Instance != null)
            {
                Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasrequiredupgrade;
                Fields["upgradeCostStr"].guiActive = !isupgraded && _hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " " + Localizer.Format("#LOC_KSPIE_ElectricEngine_science");
            }
            else
            {
                Events["RetrofitEngine"].active = false;
                Fields["upgradeCostStr"].guiActive = false;
            }

            var isInfinite = _current_propellant.IsInfinite;

            Fields["engineIsp"].guiActive = !isInfinite;
            Fields["propNameStr"].guiActive = !isInfinite;
            Fields["efficiencyStr"].guiActive = !isInfinite;
            Fields["thermalEfficiency"].guiActive = !ignoreWasteheat;

            if (this.IsOperational)
            {
                Fields["electricalPowerShareStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                Fields["efficiencyStr"].guiActive = true;
                electricalPowerShareStr = (100.0 * _electrical_share_f).ToString("0.00") + "%";
                heatProductionStr = _heat_production_f.ToString("0.000") + " MW";

                if (Current_propellant == null)
                    efficiencyStr = "";
                else
                {
                    efficiencyStr = (CurrentPropellantEfficiency * 100.0).ToString("0.00") + "%";
                    thermalEfficiency = (ThermalEfficiency * 100).ToString("0.00") + "%";
                }
            }
            else
            {
                Fields["electricalPowerShareStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
                Fields["efficiencyStr"].guiActive = false;
            }
        }


        // ReSharper disable once UnusedMember.Global
        public void Update()
        {
            partMass = part.mass;
            propNameStr = Current_propellant != null ? Current_propellant.PropellantGUIName : "";
        }

        private double IspGears
        {
            get { return _hasGearTechnology ? ispGears : 1; }
        }

        private double ModifiedThrotte
        {
            get
            {
                return Current_propellant.SupportedEngines == 8
                    ? _attachedEngine.currentThrottle
                    : Math.Min((double)(decimal)_attachedEngine.currentThrottle * IspGears, 1);
            }
        }

        private double ThrottleModifiedIsp()
        {
            var currentThrottle = (double)(decimal)_attachedEngine.currentThrottle;

            return Current_propellant.SupportedEngines == 8
                ? 1
                : currentThrottle < (1d / IspGears)
                    ? IspGears
                    : IspGears - ((currentThrottle - (1d / IspGears)) * IspGears);
        }


        // ReSharper disable once UnusedMember.Global
        public void FixedUpdate()
        {
            if (_initializationCountdown > 0)
                _initializationCountdown--;

            if (vesselChangedSIOCountdown > 0)
                vesselChangedSIOCountdown--;

            if (!HighLogic.LoadedSceneIsFlight) return;

            if (_attachedEngine == null) return;

            CalculateTimeDialation();

            if (_attachedEngine is ModuleEnginesFX)
                GetAllPropellants().ForEach(prop => part.Effect(prop.ParticleFXName, 0, -1)); // set all FX to zero

            if (Current_propellant == null) return;

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, (double)(decimal)this.part.mass);
            resourceBuffers.UpdateBuffers();

            if (!this.vessel.packed && !_warpToReal)
                storedThrotle = vessel.ctrlState.mainThrottle;

            maxEffectivePower = MaxEffectivePower;
            currentPropellantEfficiency = CurrentPropellantEfficiency;

            var sumOfAllEffectivePower = vessel.FindPartModulesImplementing<ElectricEngineControllerFX>().Where(ee => ee.IsOperational).Sum(ee => ee.MaxEffectivePower);
            _electrical_share_f = sumOfAllEffectivePower > 0 ? maxEffectivePower / sumOfAllEffectivePower : 1;

            modifiedMaxThrottlePower = maxEffectivePower * ModifiedThrotte;

             var availablePower = getAvailableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES);

             maxThrustFromPower = EvaluateMaxThrust(availablePower * _electrical_share_f);

            var megaJoulesBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);
            var effectiveResourceThrotling = megaJoulesBarRatio > OneThird ? 1 : megaJoulesBarRatio * 3;

            var rawPowerForEngine = effectiveResourceThrotling * maxThrustFromPower * CurrentIspMultiplier * _modifiedEngineBaseIsp / GetPowerThrustModifier() * GameConstants.STANDARD_GRAVITY;

            currentPowerForEngine = rawPowerForEngine * ModifiedThrotte;
            availablePowerForEngine = rawPowerForEngine;
            
            power_request = CheatOptions.InfiniteElectricity 
                ? modifiedMaxThrottlePower 
                : currentPropellantEfficiency <= 0 
                    ? 0 
                    : Math.Min(currentPowerForEngine / currentPropellantEfficiency, modifiedMaxThrottlePower);            

            actualPowerReceived = CheatOptions.InfiniteElectricity 
                ? power_request
                : consumeFNResourcePerSecond(power_request, ResourceManager.FNRESOURCE_MEGAJOULES);

            simulatedPowerReceived = Math.Min(availablePowerForEngine / currentPropellantEfficiency, maxEffectivePower);

            // produce waste heat
            var heatToProduce = actualPowerReceived * (1 - currentPropellantEfficiency) * Current_propellant.WasteHeatMultiplier;

            _heat_production_f = CheatOptions.IgnoreMaxTemperature 
                ? heatToProduce
                : supplyFNResourcePerSecond(heatToProduce, ResourceManager.FNRESOURCE_WASTEHEAT);

            // update GUI Values
            _electrical_consumption_f = actualPowerReceived;

            _effectiveIsp = _modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * ThrottleModifiedIsp();
            _maxIsp = _effectiveIsp * CurrentPropellantThrustMultiplier;

            var throtteModifier = ispGears == 1 ? 1 : ModifiedThrotte;

            effectivePowerThrustModifier = timeDilation * currentPropellantEfficiency * CurrentPropellantThrustMultiplier * GetPowerThrustModifier();

            effectiveRecievedPower = effectivePowerThrustModifier * actualPowerReceived * throtteModifier;
            effectiveSimulatedPower = effectivePowerThrustModifier * simulatedPowerReceived;

            currentThrustInSpace = _effectiveIsp <= 0 ? 0 : effectiveRecievedPower / _effectiveIsp / GameConstants.STANDARD_GRAVITY;
            simulatedThrustInSpace = _effectiveIsp <= 0 ? 0 :effectiveSimulatedPower / _effectiveIsp / GameConstants.STANDARD_GRAVITY;

            _attachedEngine.maxThrust = (float)Math.Max(simulatedThrustInSpace, 0.001);

            _currentSpaceFuelFlowRate = _maxIsp <= 0 ? 0 : currentThrustInSpace / _maxIsp / GameConstants.STANDARD_GRAVITY;
            _simulatedSpaceFuelFlowRate = _maxIsp <= 0 ? 0 : simulatedThrustInSpace / _maxIsp / GameConstants.STANDARD_GRAVITY;

            var maxThrustWithCurrentThrottle = currentThrustInSpace * throtteModifier;

            calculated_thrust = Current_propellant.SupportedEngines == 8
                ? maxThrustWithCurrentThrottle
                : Math.Max(maxThrustWithCurrentThrottle - (exitArea * vessel.staticPressurekPa), 0);

            simulated_max_thrust = Current_propellant.SupportedEngines == 8
                ? simulatedThrustInSpace
                : Math.Max(simulatedThrustInSpace - (exitArea * vessel.staticPressurekPa), 0);

            var throttle = _attachedEngine.currentThrottle > 0 ? Math.Max((double)(decimal)_attachedEngine.currentThrottle, 0.01) : 0;

            if (throttle > 0)
            {
                if (IsValidPositiveNumber(calculated_thrust) && IsValidPositiveNumber(maxThrustWithCurrentThrottle))
                {
                    _atmosphereThrustEfficiency = Math.Min(1, calculated_thrust / maxThrustWithCurrentThrottle);

                    _atmosphereThrustEfficiencyPercentage = _atmosphereThrustEfficiency * 100;

                    UpdateIsp(_atmosphereThrustEfficiency);

                    _fuelFlowModifier = ispGears == 1
                        ? 1 / throttle
                        : ModifiedThrotte / throttle;

                    _maxFuelFlowRate = (float)Math.Max(_atmosphereThrustEfficiency * _currentSpaceFuelFlowRate * _fuelFlowModifier, 1e-11);
                    _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
                }
                else
                {
                    UpdateIsp(1);
                    _atmosphereThrustEfficiency = 0;
                    _maxFuelFlowRate = 1e-11f;
                    _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
                }

                if (!this.vessel.packed)
                {
                    // allow throtle to be used up to Geeforce treshold
                    TimeWarp.GThreshold = GThreshold;

                    _isFullyStarted = true;
                    _ispPersistent = (double)(decimal)_attachedEngine.realIsp;

                    thrust_d = (double)(decimal)_attachedEngine.requestedMassFlow * GameConstants.STANDARD_GRAVITY * _ispPersistent;
                }
                else if (this.vessel.packed && _attachedEngine.enabled && FlightGlobals.ActiveVessel == vessel && _initializationCountdown == 0)
                {
                    _warpToReal = true; // Set to true for transition to realtime

                    thrust_d = calculated_thrust;

                    if (PersistHeading())
                        PersistantThrust((double)(decimal)TimeWarp.fixedDeltaTime, Planetarium.GetUniversalTime(), this.part.transform.up, this.vessel.totalMass, thrust_d, _ispPersistent);
                }
                else
                    IdleEngine();
            }
            else
                IdleEngine();

            if (_attachedEngine is ModuleEnginesFX && particleEffectMult > 0)
            {
                var engineFuelFlow = (double)(decimal)_attachedEngine.maxFuelFlow * (double)(decimal)_attachedEngine.currentThrottle;
                var max_fuel_flow_rate = (double)(decimal)_attachedEngine.maxThrust / (double)(decimal)_attachedEngine.realIsp / GameConstants.STANDARD_GRAVITY;

                effectPower = Math.Min(1, particleEffectMult * (engineFuelFlow / max_fuel_flow_rate));

                if (String.IsNullOrEmpty(EffectName))
                    _particleFXName = Current_propellant.ParticleFXName;
                else
                    _particleFXName = EffectName;

                this.part.Effect(_particleFXName, (float)effectPower, -1);
            }

            var vacuumPlasmaResource = part.Resources[InterstellarResourcesConfiguration.Instance.VacuumPlasma];
            if (isupgraded && vacuumPlasmaResource != null)
            {
                var calculatedConsumptionInTon = this.vessel.packed ? 0 : currentThrustInSpace / engineIsp / GameConstants.STANDARD_GRAVITY;
                vacuumPlasmaResource.maxAmount = Math.Max(0.0000001, calculatedConsumptionInTon * 200 * (double)(decimal)TimeWarp.fixedDeltaTime);
                part.RequestResource(InterstellarResourcesConfiguration.Instance.VacuumPlasma, - vacuumPlasmaResource.maxAmount);
            }
        }

        private bool PersistHeading()
        {
            var canPersistDirection = vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.ORBITING;

            if (canPersistDirection && vessel.ActionGroups[KSPActionGroup.SAS] && (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Prograde || vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Retrograde))
            {
                var requestedDirection = vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Prograde ? vessel.obt_velocity.normalized : vessel.obt_velocity.normalized * -1;
                var vesselDirection = vessel.transform.up.normalized;

                if (vesselChangedSIOCountdown > 0 || Vector3d.Dot(vesselDirection, requestedDirection) > 0.9)
                {
                    var rotation = Quaternion.FromToRotation(vesselDirection, requestedDirection);
                    vessel.transform.Rotate(rotation.eulerAngles, Space.World);
                    vessel.SetRotation(vessel.transform.rotation);
                }
                else
                {
                    var directionName = Enum.GetName(typeof(VesselAutopilot.AutopilotMode), vessel.Autopilot.Mode);
                    var message = "Thrust warp stopped - vessel is not facing " + directionName;
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    Debug.Log("[KSPI]: " + message);
                    TimeWarp.SetRate(0, true);
                    return false;
                }
            }
            return true;
        }

        private void IdleEngine()
        {
            thrust_d = 0;

            if (IsValidPositiveNumber(simulated_max_thrust) && IsValidPositiveNumber(simulatedThrustInSpace))
            {
                UpdateIsp(Math.Max(0, simulated_max_thrust / simulatedThrustInSpace));
                _maxFuelFlowRate = (float)Math.Max(_simulatedSpaceFuelFlowRate, 1e-11);
                _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
            }
            else
            {
                UpdateIsp(1);
                _maxFuelFlowRate = 1e-11f;
                _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
            }

            if (_attachedEngine is ModuleEnginesFX && particleEffectMult > 0)
                this.part.Effect(Current_propellant.ParticleFXName, 0, -1);
        }

        private void CalculateTimeDialation()
        {
            try
            {
                var worldSpaceVelocity = vessel.orbit.GetFrameVel().magnitude;

                lightSpeedRatio = Math.Min(worldSpaceVelocity / _speedOfLight, 0.9999999999);

                timeDilation = Math.Sqrt(1 - (lightSpeedRatio * lightSpeedRatio));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI]: Error CalculateTimeDialation " + e.Message + " stack " + e.StackTrace);
            }
        }

        private static bool IsValidPositiveNumber(double value)
        {
            if (double.IsNaN(value))
                return false;

            if (double.IsInfinity(value))
                return false;

            return !(value <= 0);
        }

        private void PersistantThrust(double fixedDeltaTime, double universalTime, Vector3d thrustDirection, double vesselMass, double thrust, double isp)
        {
            double demandMass;

            var deltaVv = CalculateDeltaVV(thrustDirection, vesselMass, fixedDeltaTime, thrust, isp, out demandMass);

            double persistentThrustDot = Vector3d.Dot(thrustDirection, vessel.obt_velocity);
            if (persistentThrustDot < 0 && (vessel.obt_velocity.magnitude <= deltaVv.magnitude * 2))
            {
                var message = "Thrust warp stopped - orbital speed too low";
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
                return;
            }

            double fuelRatio = 0;

            // determine fuel availability
            if (!Current_propellant.IsInfinite && !CheatOptions.InfinitePropellant && Current_propellant.ResourceDefinition.density > 0)
            {
                var requestedAmount = demandMass / (double)(decimal)Current_propellant.ResourceDefinition.density;
                if (IsValidPositiveNumber(requestedAmount))
                    fuelRatio = part.RequestResource(Current_propellant.Propellant.name, requestedAmount) / requestedAmount;
            }
            else 
                fuelRatio = 1;

            if (!double.IsNaN(fuelRatio) && !double.IsInfinity(fuelRatio) && fuelRatio > 0)
            {
                vessel.orbit.Perturb(deltaVv * fuelRatio, universalTime);
            }

            if (thrust > 0.0000005 && fuelRatio < 0.999999 && _isFullyStarted)
            {
                var message = "Thrust warp stopped - " + fuelRatio + " propellant depleted thust: " + thrust;
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
            }
        }

        public void upgradePartModule()
        {
            isupgraded = true;
            type = upgradedtype;
            _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            engineTypeStr = upgradedName;

            if (!vacplasmaadded && type == (int)ElectricEngineType.VACUUMTHRUSTER)
            {
                vacplasmaadded = true;
                var node = new ConfigNode("RESOURCE");
                node.AddValue("name", InterstellarResourcesConfiguration.Instance.VacuumPlasma);
                node.AddValue("maxAmount", scaledMaxPower * 0.0000001);
                node.AddValue("amount", scaledMaxPower * 0.0000001);
                this.part.AddResource(node);
            }
        }

        public override string GetInfo()
        {
            var props = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            var returnStr = Localizer.Format("#LOC_KSPIE_ElectricEngine_maxPowerConsumption") + " : " + maxPower.ToString("F3") + " MW\n";
            var thrustPerMw = (2e6 * powerThrustMultiplier) / GameConstants.STANDARD_GRAVITY / (baseISP * PluginHelper.ElectricEngineIspMult) / 1000.0;
            props.ForEach(prop =>
            {
                var ispPropellantModifier = (this.type == (int)ElectricEngineType.VASIMR ? prop.DecomposedIspMult : prop.IspMultiplier);
                var ispProp = _modifiedEngineBaseIsp * ispPropellantModifier;

                double efficiency;

                switch (type)
                {
                    case (int)ElectricEngineType.ARCJET:
                        efficiency = 0.87 * prop.Efficiency;
                        break;
                    case (int)ElectricEngineType.VASIMR:
                        efficiency = baseEfficency + 0.5 * variableEfficency;
                        break;
                    default:
                        efficiency = prop.Efficiency;
                        break;
                }

                var thrustProp = thrustPerMw / ispPropellantModifier * efficiency * (type == (int)ElectricEngineType.ARCJET ? prop.ThrustMultiplier : 1);
                returnStr = returnStr + "---" + prop.PropellantGUIName + "---\n" + Localizer.Format("#LOC_KSPIE_ElectricEngine_thrust") 
                    + ": " + thrustProp.ToString("0.000") + " " + Localizer.Format("#LOC_KSPIE_ElectricEngine_kiloNewtonPerMegaWatt") + "\n" + Localizer.Format("#LOC_KSPIE_ElectricEngine_efficiency") 
                    + " : " + (efficiency * 100.0).ToString("0.00") + "%\n" + Localizer.Format("#LOC_KSPIE_ElectricEngine_specificImpulse") + ": " + ispProp.ToString("0.00") + "s\n";
            });
            return returnStr;
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title + (Current_propellant != null ? " (" + Current_propellant.PropellantGUIName + ")" : "");
        }

        private void TogglePropellant(bool next)
        {
            if (next)
                ToggleNextPropellant();
            else
                TogglePreviousPropellant();
        }

        private void ToggleNextPropellant()
        {
            Debug.Log("[KSPI]: ElectricEngineControllerFX toggleNextPropellant");
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
                fuel_mode = 0;

            SetupPropellants(true);
        }

        private void TogglePreviousPropellant()
        {
            Debug.Log("[KSPI]: ElectricEngineControllerFX togglePreviousPropellant");
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = _propellants.Count - 1;

            SetupPropellants(false);
        }

        private double EvaluateMaxThrust(double powerSupply)
        {
            if (Current_propellant == null) return 0;

            if (_modifiedCurrentPropellantIspMultiplier <= 0) return 0;

            return CurrentPropellantEfficiency * GetPowerThrustModifier() * powerSupply / (_modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * GameConstants.STANDARD_GRAVITY);
        }

        private void UpdateIsp(double ispEfficiency)
        {
            _ispFloatCurve.Curve.RemoveKey(0);
            engineIsp = timeDilation * ispEfficiency * _modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier * ThrottleModifiedIsp();
            _ispFloatCurve.Add(0, (float)engineIsp);
            _attachedEngine.atmosphereCurve = _ispFloatCurve;
        }

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginHelper.GlobalElectricEnginePowerMaxThrustMult * this.powerThrustMultiplier;
        }

        private double GetAtmosphericDensityModifier()
        {
            return Math.Max(1.0 - (part.vessel.atmDensity * PluginHelper.ElectricEngineAtmosphericDensityThrustLimiter), 0.0);
        }

        private static List<ElectricEnginePropellant> GetAllPropellants()
        { 
            var propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellantList;
            if (propellantlist.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellantList = new List<ElectricEnginePropellant>();
            }
            else
                propellantList = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).ToList();

            return propellantList;
        }

        public static Vector3d CalculateDeltaVV(Vector3d thrustDirection, double totalMass, double deltaTime, double thrust, double isp, out double demandMass)
        {
            // Mass flow rate
            var massFlowRate = thrust / (isp * GameConstants.STANDARD_GRAVITY);
            // Change in mass over time interval dT
            var dm = massFlowRate * deltaTime;
            // Resource demand from propellants with mass
            demandMass = dm;
            // Mass at end of time interval dT
            var finalMass = totalMass - dm;
            // deltaV amount
            var deltaV = finalMass > 0 && totalMass > 0
                ? isp * GameConstants.STANDARD_GRAVITY * Math.Log(totalMass / finalMass)
                : 0;

            // Return deltaV vector
            return deltaV * thrustDirection;
        }

        public override int getPowerPriority()
        {
            return 3;
        }
    }
}
