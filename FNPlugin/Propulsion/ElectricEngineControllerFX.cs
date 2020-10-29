﻿using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Wasteheat;
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
        public const string GROUP = "ElectricEngineControllerFX";
        public const string GROUP_TITLE = "#LOC_KSPIE_ElectricEngine_groupName";

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
        public double powerReqMultWithoutReactor = 0;
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
        public double powerThrustMultiplier = 1;

        [KSPField]
        public double powerThrustMultiplierWithoutReactors = 0;

        [KSPField]
        public float upgradeCost = 0;
        [KSPField]
        public string originalName = "";
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public double baseIspIonisationDivider = 3000;
        [KSPField]
        public double minimumIonisationRatio = 0.05;
        [KSPField]
        public double ionisationMultiplier = 0.5;
        [KSPField]
        public double baseEfficency = 1;
        [KSPField]
        public double variableEfficency = 0;
        [KSPField]
        public float storedThrotle;
        [KSPField]
        public double particleEffectMult = 1;
        [KSPField]
        public bool ignoreWasteheat = false;
        [KSPField]
        public double GThreshold = 9;
        [KSPField]
        public double maxEffectPowerRatio = 0.75;

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
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineType")]
        public string engineTypeStr = "";
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#autoLOC_6001377", guiUnits = "#autoLOC_7001408", guiFormat = "F3")]
        public double thrust_d;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CalculatedThrust", guiFormat = "F3", guiUnits = "kN")]//Calculated Thrust
        public double calculated_thrust;
        [KSPField(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_warpIsp", guiFormat = "F1", guiUnits = "s")]
        public double engineIsp;
        [KSPField(groupName = GROUP, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_maxPowerInput",  guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double scaledMaxPower = 0;
        [KSPField(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_activePropellantName")]
        public string propNameStr = "";
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerShare")]
        public string electricalPowerShareStr = "";
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_MaximumPowerRequest", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Maximum Power Request
        public double maximum_power_request;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerRequested", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")] // Current Power Request
        public double current_power_request;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_propellantEfficiency")]
        public string efficiencyStr = "";
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_overheatEfficiency")]
        public string thermalEfficiency = "";
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_heatProduction")]
        public string heatProductionStr = "";
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_upgradeCost")]
        public string upgradeCostStr = "";
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_maxEffectivePower", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double maxEffectivePower;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_maxThrottlePower", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double modifiedMaxThrottlePower;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")]
        public double timeDilation = 1;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CapacityModifier")]//Capacity Modifier
        protected double powerCapacityModifier = 1;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_AtmTrustEfficiency")]//Atm Trust Efficiency
        protected double _atmosphereThrustEfficiency;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_AtmTrustEfficiency", guiFormat = "F2", guiUnits = "%")]//Atm Trust Efficiency
        protected double _atmosphereThrustEfficiencyPercentage;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_MaxFuelFlowRate")]//Max Fuel Flow Rate
        protected float _maxFuelFlowRate;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CurrentSpaceFuelFlowRate")]//Current Space Fuel Flow Rate
        protected double _currentSpaceFuelFlowRate;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_PotentialSpaceFuelFlowRate")]//Potential Space Fuel Flow Rate
        protected double _simulatedSpaceFuelFlowRate;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_FuelFlowModifier")]//Fuel Flow Modifier
        protected double _fuelFlowModifier;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_CurrentThrustinSpace", guiFormat = "F3", guiUnits = " kN")]//Current Thrust in Space
        protected double currentThrustInSpace;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_MaxThrustinSpace", guiFormat = "F3", guiUnits = " kN")]//Max Thrust in Space
        protected double simulatedThrustInSpace;
        [KSPField(guiActive = false)]
        public double simulated_max_thrust;

        [KSPField(guiActive = false)]
        public double currentPropellantEfficiency;
        [KSPField(groupName = GROUP, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineMass", guiFormat = "F3", guiUnits = " t")]
        public float partMass = 0;

        [KSPField]
        public double prefabMass;
        [KSPField]
        public double expectedMass = 0;
        [KSPField]
        public double desiredMass = 0;

        [KSPField(guiActive = false)]
        protected double modifiedMaximumPowerForEngine;
        [KSPField(guiActive = false)]
        protected double modifiedCurrentPowerForEngine;

        [KSPField(guiActive = false)]
        protected double effectiveMaximumAvailablePowerForEngine;
        [KSPField(guiActive = false)]
        protected double effectiveCurrentAvailablePowerForEngine;

        [KSPField(guiActive = false)]
        protected double effectiveMaximumPower;
        [KSPField(guiActive = false)]
        protected double effectiveRecievedPower;
        [KSPField(guiActive = false)]
        protected double effectiveSimulatedPower;
        [KSPField(guiActive = false)]
        protected double modifiedThrotte;
        [KSPField(guiActive = false)]
        protected double effectivePowerThrustModifier;
        [KSPField(guiActive = false)]
        public double actualPowerReceived;
        [KSPField(guiActive = false)]
        public double simulatedPowerReceived;

        [KSPField]
        protected double maximumAvailablePowerForEngine;
        [KSPField]
        protected double currentAvailablePowerForEngine;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_TotalPowerSupplied")]//Total Power Supplied
        protected double totalPowerSupplied;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_MaximumAvailablePower")]//Maximum Available Power
        protected double availableMaximumPower;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CurrentAvailablePower")]//Current Available Power
        protected double availableCurrentPower;

        [KSPField]
        protected double maximumThrustFromPower = 0.001;
        [KSPField]
        protected double currentThrustFromPower = 0.001;

        [KSPField]
        protected double megaJoulesBarRatio;
        [KSPField]
        protected double effectPower = 0;
        [KSPField]
        public string EffectName = string.Empty;

        [KSPField]
        public double massTweakscaleExponent = 3;
        [KSPField]
        public double powerExponent = 3;
        [KSPField]
        public double massExponent = 3;
        [KSPField]
        public double maxPower = 1000;
        [KSPField]
        public double effectiveResourceThrotling;
        [KSPField]
        public double ratioHeadingVersusRequest;

        int _rep;
        int _initializationCountdown;
        int _vesselChangedSioCountdown;
        int _numberOfAvailableUpgradeTechs;

        bool _hasRequiredUpgrade;
        bool _hasGearTechnology;
        bool _warpToReal;
        bool _isFullyStarted;

        double _maximumThrustInSpace;
        double _effectiveSpeedOfLight;
        double _modifiedEngineBaseIsp;
        double _electricalShareF;
        double _electricalConsumptionF;
        double _heatProductionF;
        double _modifiedCurrentPropellantIspMultiplier;
        double _maxIsp;
        double _effectiveIsp;
        double _ispPersistent;

        ResourceBuffers _resourceBuffers;
        FloatCurve _ispFloatCurve;
        ModuleEngines _attachedEngine;

        List<ElectricEnginePropellant> _vesselPropellants;
        List<string> _allPropellantsFx;

        // Properties
        public string UpgradeTechnology => upgradeTechReq;
        public double MaxPower => scaledMaxPower * powerReqMult * powerCapacityModifier;
        public double MaxEffectivePower => ignoreWasteheat ? MaxPower : MaxPower * CurrentPropellantEfficiency * ThermalEfficiency;
        public bool IsOperational => _attachedEngine != null && _attachedEngine.isOperational;

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

        private ElectricEnginePropellant _currentPropellant = null;
        public ElectricEnginePropellant CurrentPropellant
        {
            get => _currentPropellant;
            set
            {
                if (value == null)
                    return;

                _currentPropellant = value;
                propellantIsSaved = true;
                fuel_mode = _vesselPropellants.IndexOf(_currentPropellant);
                propellantName = _currentPropellant.PropellantName;
                propellantGUIName = _currentPropellant.PropellantGUIName;
                _modifiedCurrentPropellantIspMultiplier = CurrentIspMultiplier;
            }
        }

        public double CurrentIspMultiplier =>
            type == (int)ElectricEngineType.VASIMR || type == (int)ElectricEngineType.ARCJET
                ? CurrentPropellant.DecomposedIspMult
                : CurrentPropellant.IspMultiplier;

        public double ThermalEfficiency
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight || CheatOptions.IgnoreMaxTemperature || ignoreWasteheat)
                    return 1;

                var wasteheatRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);

                return 1 - wasteheatRatio * wasteheatRatio * wasteheatRatio;
            }
        }

        public double CurrentPropellantThrustMultiplier => type == (int)ElectricEngineType.ARCJET ? CurrentPropellant.ThrustMultiplier : 1;

        public double CurrentPropellantEfficiency
        {
            get
            {
                double efficiency;

                if (type == (int)ElectricEngineType.ARCJET)
                {
                    // achieves higher efficiencies due to wasteheat preheating
                    efficiency = (ionisationMultiplier * CurrentPropellant.Efficiency) + ((1 - ionisationMultiplier) * baseEfficency);
                }
                else if (type == (int)ElectricEngineType.VASIMR)
                {
                    var ionizationEnergyRatio = _attachedEngine.currentThrottle > 0
                        ? minimumIonisationRatio + (_attachedEngine.currentThrottle * ionisationMultiplier)
                        : minimumIonisationRatio;

                    ionizationEnergyRatio = Math.Min(1, ionizationEnergyRatio);

                    efficiency = (ionizationEnergyRatio * CurrentPropellant.Efficiency) + ((1 - ionizationEnergyRatio) * (baseEfficency + ((1 - _attachedEngine.currentThrottle) * variableEfficency)));
                }
                else if (type == (int)ElectricEngineType.VACUUMTHRUSTER)
                {
                    // achieves higher efficiencies due to wasteheat preheating
                    efficiency = CurrentPropellant.Efficiency;
                }
                else
                {
                    var ionizationEnergyRatio = Math.Min(1,  1 / (baseISP / baseIspIonisationDivider));

                    // achieve higher efficiencies at higher base isp
                    efficiency = (ionizationEnergyRatio * CurrentPropellant.Efficiency) + ((1 - ionizationEnergyRatio) * baseEfficency);
                }

                return efficiency;
            }
        }

        public void VesselChangedSoi()
        {
            _vesselChangedSioCountdown = 10;
        }

        // Events
        [KSPEvent(groupName = GROUP, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_nextPropellant", active = true)]
        public void ToggleNextPropellantEvent()
        {
            ToggleNextPropellant();
        }

        [KSPEvent(groupName = GROUP, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_previous Propellant", active = true)]
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
            prefabMass = part.prefabMass;
            expectedMass = prefabMass * Math.Pow(storedAbsoluteFactor, massTweakscaleExponent);
            desiredMass = prefabMass * Math.Pow(storedAbsoluteFactor, massExponent);
            scaledMaxPower = maxPower * Math.Pow(storedAbsoluteFactor, powerExponent);
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return 0.0f;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_retrofit", active = true)]
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
            if (state != StartState.Editor)
            {
                if (vessel.FindPartModulesImplementing<FNGenerator>().Any(m => m.isHighPower) == false)
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (powerThrustMultiplier == 1 && powerThrustMultiplierWithoutReactors > 0)
                        powerThrustMultiplier = powerThrustMultiplierWithoutReactors;

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (powerReqMult == 1 && powerReqMultWithoutReactor > 0)
                        powerReqMult = powerReqMultWithoutReactor;
                }
            }

            ScaleParameters();

            // initialise resources
            resources_to_supply = new string[] { ResourceManager.FNRESOURCE_WASTEHEAT };
            base.OnStart(state);

            AttachToEngine();
            DetermineTechLevel();
            powerCapacityModifier = PowerCapacityModifier;

            _initializationCountdown = 10;
            _ispFloatCurve = new FloatCurve();
            _ispFloatCurve.Add(0, (float)baseISP);
            _effectiveSpeedOfLight = GameConstants.speedOfLight * PluginHelper.SpeedOfLightMult;
            _hasGearTechnology = string.IsNullOrEmpty(gearsTechReq) || PluginHelper.UpgradeAvailable(gearsTechReq);
            _modifiedEngineBaseIsp = baseISP * PluginHelper.ElectricEngineIspMult;
            _hasRequiredUpgrade = this.HasTechsRequiredToUpgrade();

            if (_hasRequiredUpgrade && (isupgraded || state == StartState.Editor))
                upgradePartModule();

            UpdateEngineTypeString();

            _resourceBuffers = new ResourceBuffers();
            _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, 2.0e+4, true));
            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, part.mass);
            _resourceBuffers.Init(part);

            InitializePropellantMode();

            SetupPropellants(true);

            UpdateIsp();

            _attachedEngine.maxThrust = (float)maximumThrustFromPower;
        }

        private void InitializePropellantMode()
        {
            // initialize propellant
            _allPropellantsFx = GetAllPropellants().Select(m => m.ParticleFXName ).Distinct().ToList();
            _vesselPropellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);

            if (propellantIsSaved || HighLogic.LoadedSceneIsEditor)
            {
                if (!string.IsNullOrEmpty(propellantName))
                {
                    CurrentPropellant = _vesselPropellants.FirstOrDefault(m => m.PropellantName == propellantName) ??
                                        _vesselPropellants.FirstOrDefault(m => m.PropellantGUIName == propellantName);
                }

                if (CurrentPropellant == null && !string.IsNullOrEmpty(propellantGUIName))
                {
                    CurrentPropellant = _vesselPropellants.FirstOrDefault(m => m.PropellantName == propellantGUIName);

                    if (CurrentPropellant == null)
                        CurrentPropellant = _vesselPropellants.FirstOrDefault(m => m.PropellantGUIName == propellantGUIName);
                }
            }

            if (_vesselPropellants == null)
            {
                Debug.LogWarning("[KSPI]: SetupPropellants _vesselPropellants is still null");
            }
            else if (CurrentPropellant == null)
            {
                CurrentPropellant = fuel_mode < _vesselPropellants.Count ? _vesselPropellants[fuel_mode] : _vesselPropellants.First();
            }
        }

        private void AttachToEngine()
        {
            _attachedEngine = part.FindModuleImplementing<ModuleEngines>();
            if (_attachedEngine == null) return;

            var finalTrustField = _attachedEngine.Fields[nameof(_attachedEngine.finalThrust)];
            finalTrustField.guiActive = false;

            var realIspField = _attachedEngine.Fields[nameof(_attachedEngine.realIsp)];
            realIspField.guiActive = false;
        }

        private void SetupPropellants(bool moveNext)
        {
            try
            {
                CurrentPropellant = fuel_mode < _vesselPropellants.Count ? _vesselPropellants[fuel_mode] : _vesselPropellants.First();

                if ((CurrentPropellant.SupportedEngines & type) != type)
                {
                    _rep++;
                    Debug.LogWarning("[KSPI]: SetupPropellants TogglePropellant");
                    TogglePropellant(moveNext);
                    return;
                }

                var listOfPropellants = new List<Propellant>
                {
                    CurrentPropellant.Propellant
                };

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
                else if (_rep < _vesselPropellants.Count)
                {
                    _rep++;
                    TogglePropellant(moveNext);
                    return;
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    // you can have any fuel you want in the editor but not in flight
                    var allVesselResourcesNames = part.vessel.parts.SelectMany(m => m.Resources).Select(m => m.resourceName).Distinct();
                    if (!listOfPropellants.All(prop => allVesselResourcesNames.Contains(prop.name)) && _rep < _vesselPropellants.Count)
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
                Events[nameof(RetrofitEngine)].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasRequiredUpgrade;
                Fields[nameof(upgradeCostStr)].guiActive = !isupgraded && _hasRequiredUpgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " " + Localizer.Format("#LOC_KSPIE_ElectricEngine_science");
            }
            else
            {
                Events[nameof(RetrofitEngine)].active = false;
                Fields[nameof(upgradeCostStr)].guiActive = false;
            }

            var isInfinite = _currentPropellant.IsInfinite;

            Fields[nameof(engineIsp)].guiActive = !isInfinite;
            Fields[nameof(propNameStr)].guiActive = !isInfinite;
            Fields[nameof(efficiencyStr)].guiActive = !isInfinite;
            Fields[nameof(thermalEfficiency)].guiActive = !ignoreWasteheat;

            if (IsOperational)
            {
                Fields[nameof(electricalPowerShareStr)].guiActive = true;
                Fields[nameof(heatProductionStr)].guiActive = true;
                Fields[nameof(efficiencyStr)].guiActive = true;
                electricalPowerShareStr = _electricalShareF.ToString("P2");
                heatProductionStr = PluginHelper.getFormattedPowerString(_heatProductionF);

                if (CurrentPropellant == null)
                    efficiencyStr = "";
                else
                {
                    efficiencyStr = CurrentPropellantEfficiency.ToString("P2");
                    thermalEfficiency = ThermalEfficiency.ToString("P2");
                }
            }
            else
            {
                Fields[nameof(electricalPowerShareStr)].guiActive = false;
                Fields[nameof(heatProductionStr)].guiActive = false;
                Fields[nameof(efficiencyStr)].guiActive = false;
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void Update()
        {
            partMass = part.mass;
            propNameStr = CurrentPropellant != null ? CurrentPropellant.PropellantGUIName : "";
        }

        private double IspGears => _hasGearTechnology ? ispGears : 1;

        private double ModifiedThrottle =>
            CurrentPropellant.SupportedEngines == 8
                ? _attachedEngine.currentThrottle
                : Math.Min((double)(decimal)_attachedEngine.currentThrottle * IspGears, 1);

        private double ThrottleModifiedIsp()
        {
            var currentThrottle = (double)(decimal)_attachedEngine.currentThrottle;

            return CurrentPropellant.SupportedEngines == 8
                ? 1
                : currentThrottle < (1d / IspGears)
                    ? IspGears
                    : IspGears - ((currentThrottle - (1d / IspGears)) * IspGears);
        }

        // ReSharper disable once UnusedMember.Global
        public void FixedUpdate()
        {
            // disable exhaust effects
            if (!string.IsNullOrEmpty(EffectName))
                part.Effect(EffectName, (float)effectPower, -1);

            if (_allPropellantsFx != null)
            {
                // set all FX to zero
                foreach (var propName in _allPropellantsFx)
                {
                    var currentEffectPower = CurrentPropellant.ParticleFXName == propName ? effectPower : 0;
                    part.Effect(propName, (float)currentEffectPower, -1);
                }
            }

            // if not force activated or staged, still call OnFixedUpdateResourceSuppliable
            if (!isEnabled)
                OnFixedUpdateResourceSuppliable((double) (decimal) TimeWarp.fixedDeltaTime);
        }

        // ReSharper disable once UnusedMember.Global
        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (_attachedEngine == null || !HighLogic.LoadedSceneIsFlight) return;

            if (_initializationCountdown > 0)
                _initializationCountdown--;

            if (_vesselChangedSioCountdown > 0)
                _vesselChangedSioCountdown--;

            CalculateTimeDialation();

            if (CurrentPropellant == null) return;

            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, (double)(decimal)part.mass);
            _resourceBuffers.UpdateBuffers();

            if (!vessel.packed && !_warpToReal)
                storedThrotle = vessel.ctrlState.mainThrottle;

            maxEffectivePower = MaxEffectivePower;
            currentPropellantEfficiency = CurrentPropellantEfficiency;

            var sumOfAllEffectivePower = vessel.FindPartModulesImplementing<ElectricEngineControllerFX>().Where(ee => ee.IsOperational).Sum(ee => ee.MaxEffectivePower);
            _electricalShareF = sumOfAllEffectivePower > 0 ? maxEffectivePower / sumOfAllEffectivePower : 1;

            modifiedThrotte = ModifiedThrottle;
            modifiedMaxThrottlePower = maxEffectivePower * modifiedThrotte;

            totalPowerSupplied = getTotalPowerSupplied(ResourceManager.FNRESOURCE_MEGAJOULES);
            megaJoulesBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);

            effectiveResourceThrotling = megaJoulesBarRatio > 0.1 ? 1 : megaJoulesBarRatio * 10;

            availableMaximumPower = getAvailablePrioritisedStableSupply(ResourceManager.FNRESOURCE_MEGAJOULES);
            availableCurrentPower = CheatOptions.InfiniteElectricity
                ? availableMaximumPower
                : getAvailablePrioritisedCurrentSupply(ResourceManager.FNRESOURCE_MEGAJOULES);

            maximumAvailablePowerForEngine = availableMaximumPower * _electricalShareF;
            currentAvailablePowerForEngine = availableCurrentPower * _electricalShareF;

            maximumThrustFromPower = EvaluateMaxThrust(maximumAvailablePowerForEngine);
            currentThrustFromPower = EvaluateMaxThrust(currentAvailablePowerForEngine);

            effectiveMaximumAvailablePowerForEngine = maximumAvailablePowerForEngine * effectiveResourceThrotling;
            effectiveCurrentAvailablePowerForEngine = currentAvailablePowerForEngine * effectiveResourceThrotling;

            modifiedMaximumPowerForEngine = effectiveMaximumAvailablePowerForEngine * modifiedThrotte;
            modifiedCurrentPowerForEngine = effectiveCurrentAvailablePowerForEngine * modifiedThrotte;

            maximum_power_request = CheatOptions.InfiniteElectricity
                ? modifiedMaximumPowerForEngine
                : currentPropellantEfficiency <= 0
                    ? 0
                    : Math.Min(modifiedMaximumPowerForEngine, modifiedMaxThrottlePower);

            current_power_request = CheatOptions.InfiniteElectricity
                ? modifiedCurrentPowerForEngine
                : currentPropellantEfficiency <= 0
                    ? 0
                    : Math.Min(modifiedCurrentPowerForEngine, modifiedMaxThrottlePower);

            // request electric power
            actualPowerReceived = CheatOptions.InfiniteElectricity
                ? current_power_request
                : consumeFNResourcePerSecond(current_power_request, maximum_power_request, ResourceManager.FNRESOURCE_MEGAJOULES);

            simulatedPowerReceived = Math.Min(effectiveMaximumAvailablePowerForEngine, maxEffectivePower);

            // produce waste heat
            var heatModifier = (1 - currentPropellantEfficiency) * CurrentPropellant.WasteHeatMultiplier;
            var heatToProduce = actualPowerReceived * heatModifier;
            var maxHeatToProduce = maximumAvailablePowerForEngine * heatModifier;

            _heatProductionF = CheatOptions.IgnoreMaxTemperature
                ? heatToProduce
                : supplyFNResourcePerSecondWithMax(heatToProduce, maxHeatToProduce, ResourceManager.FNRESOURCE_WASTEHEAT);

            // update GUI Values
            _electricalConsumptionF = actualPowerReceived;

            _effectiveIsp = _modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * ThrottleModifiedIsp();
            _maxIsp = _effectiveIsp * CurrentPropellantThrustMultiplier;

            var throttleModifier = ispGears == 1 ? 1 : ModifiedThrottle;

            effectivePowerThrustModifier = timeDilation * currentPropellantEfficiency * CurrentPropellantThrustMultiplier * GetPowerThrustModifier();

            effectiveMaximumPower = effectivePowerThrustModifier * modifiedMaxThrottlePower * throttleModifier;
            effectiveRecievedPower = effectivePowerThrustModifier * actualPowerReceived * throttleModifier;
            effectiveSimulatedPower = effectivePowerThrustModifier * simulatedPowerReceived;

            _maximumThrustInSpace = effectiveMaximumPower / _effectiveIsp / GameConstants.STANDARD_GRAVITY;
            currentThrustInSpace = _effectiveIsp <= 0 ? 0 : effectiveRecievedPower / _effectiveIsp / GameConstants.STANDARD_GRAVITY;
            simulatedThrustInSpace = _effectiveIsp <= 0 ? 0 : effectiveSimulatedPower / _effectiveIsp / GameConstants.STANDARD_GRAVITY;

            _attachedEngine.maxThrust = (float)Math.Max(simulatedThrustInSpace, 0.001);

            _currentSpaceFuelFlowRate = _maxIsp <= 0 ? 0 : currentThrustInSpace / _maxIsp / GameConstants.STANDARD_GRAVITY;
            _simulatedSpaceFuelFlowRate = _maxIsp <= 0 ? 0 : simulatedThrustInSpace / _maxIsp / GameConstants.STANDARD_GRAVITY;

            var maxThrustWithCurrentThrottle = currentThrustInSpace * throttleModifier;

            calculated_thrust = CurrentPropellant.SupportedEngines == 8
                ? maxThrustWithCurrentThrottle
                : Math.Max(maxThrustWithCurrentThrottle - (exitArea * vessel.staticPressurekPa), 0);

            simulated_max_thrust = CurrentPropellant.SupportedEngines == 8
                ? simulatedThrustInSpace
                : Math.Max(simulatedThrustInSpace - (exitArea * vessel.staticPressurekPa), 0);

            var throttle = _attachedEngine.getIgnitionState && _attachedEngine.currentThrottle > 0 ? Math.Max(_attachedEngine.currentThrottle, 0.01) : 0;

            if (throttle > 0)
            {
                if (IsValidPositiveNumber(calculated_thrust) && IsValidPositiveNumber(maxThrustWithCurrentThrottle))
                {
                    _atmosphereThrustEfficiency = Math.Min(1, calculated_thrust / maxThrustWithCurrentThrottle);

                    _atmosphereThrustEfficiencyPercentage = _atmosphereThrustEfficiency * 100;

                    UpdateIsp(_atmosphereThrustEfficiency);

                    _fuelFlowModifier = ispGears == 1
                        ? 1 / throttle
                        : ModifiedThrottle / throttle;

                    _maxFuelFlowRate = (float)Math.Max(_atmosphereThrustEfficiency * _currentSpaceFuelFlowRate * _fuelFlowModifier, 0);
                    _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
                }
                else
                {
                    UpdateIsp(1);
                    _atmosphereThrustEfficiency = 0;
                    _maxFuelFlowRate = 0;
                    _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
                }

                if (vessel.packed == false)
                {
                    // allow throttle to be used up to Geeforce threshold
                    TimeWarp.GThreshold = GThreshold;

                    _isFullyStarted = true;
                    _ispPersistent = _attachedEngine.realIsp;

                    thrust_d = _attachedEngine.requestedMassFlow * GameConstants.STANDARD_GRAVITY * _ispPersistent;

                    ratioHeadingVersusRequest = 0;
                }
                else if (vessel.packed && _attachedEngine.isEnabled && FlightGlobals.ActiveVessel == vessel && _initializationCountdown == 0)
                {
                    _warpToReal = true; // Set to true for transition to realtime

                    thrust_d = calculated_thrust;

                    ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSioCountdown > 0, ratioHeadingVersusRequest == 1);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (ratioHeadingVersusRequest == 1)
                        PersistentThrust((double)(decimal)TimeWarp.fixedDeltaTime, Planetarium.GetUniversalTime(), part.transform.up, vessel.totalMass, thrust_d, _ispPersistent);
                }
                else
                    IdleEngine();
            }
            else
                IdleEngine();

            if (_attachedEngine is ModuleEnginesFX && particleEffectMult > 0)
            {
                var engineFuelFlow = _currentSpaceFuelFlowRate * _attachedEngine.currentThrottle;
                var currentMaxFuelFlowRate = _attachedEngine.maxThrust / _attachedEngine.realIsp / GameConstants.STANDARD_GRAVITY;
                var engineMaxFuelFlowRat = _maximumThrustInSpace / _attachedEngine.realIsp / GameConstants.STANDARD_GRAVITY;

                var currentEffectPower = Math.Min(1, particleEffectMult * (engineFuelFlow / currentMaxFuelFlowRate));
                var maximumEffectPower = Math.Min(1, particleEffectMult * (engineFuelFlow / engineMaxFuelFlowRat));

                effectPower = currentEffectPower * (1 - maxEffectPowerRatio) + maximumEffectPower * maxEffectPowerRatio;
            }

            var vacuumPlasmaResource = part.Resources[InterstellarResourcesConfiguration.Instance.VacuumPlasma];
            if (isupgraded && vacuumPlasmaResource != null)
            {
                var calculatedConsumptionInTon = vessel.packed ? 0 : simulatedThrustInSpace / engineIsp / GameConstants.STANDARD_GRAVITY;
                var vacuumPlasmaResourceAmount = calculatedConsumptionInTon * 2000 * TimeWarp.fixedDeltaTime;
                vacuumPlasmaResource.maxAmount = vacuumPlasmaResourceAmount;
                part.RequestResource(InterstellarResourcesConfiguration.Instance.VacuumPlasma, -vacuumPlasmaResource.maxAmount);
            }
        }

        private void IdleEngine()
        {
            thrust_d = 0;

            if (IsValidPositiveNumber(simulated_max_thrust) && IsValidPositiveNumber(simulatedThrustInSpace))
            {
                UpdateIsp(Math.Max(0, simulated_max_thrust / simulatedThrustInSpace));
                _maxFuelFlowRate = (float)Math.Max(_simulatedSpaceFuelFlowRate, 0);
                _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
            }
            else
            {
                UpdateIsp(1);
                _maxFuelFlowRate = 0;
                _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
            }

            if (_attachedEngine is ModuleEnginesFX && particleEffectMult > 0)
                part.Effect(CurrentPropellant.ParticleFXName, 0, -1);
        }

        private void CalculateTimeDialation()
        {
            var worldSpaceVelocity = vessel.orbit.GetFrameVel().magnitude;

            lightSpeedRatio = Math.Min(_effectiveSpeedOfLight == 0.0 ? 1.0 : worldSpaceVelocity / _effectiveSpeedOfLight, 0.9999999999);

            timeDilation = Math.Sqrt(1 - (lightSpeedRatio * lightSpeedRatio));
        }

        private static bool IsValidPositiveNumber(double value)
        {
            if (double.IsNaN(value))
                return false;

            if (double.IsInfinity(value))
                return false;

            return !(value <= 0);
        }

        private void PersistentThrust(double fixedDeltaTime, double universalTime, Vector3d thrustDirection, double vesselMass, double thrust, double isp)
        {
            var deltaVv = CalculateDeltaVV(thrustDirection, vesselMass, fixedDeltaTime, thrust, isp, out var demandMass);
            string message;

            var persistentThrustDot = Vector3d.Dot(thrustDirection, vessel.obt_velocity);
            if (persistentThrustDot < 0 && (vessel.obt_velocity.magnitude <= deltaVv.magnitude * 2))
            {
                message = Localizer.Format("#LOC_KSPIE_ElectricEngineController_PostMsg1");
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);//"Thrust warp stopped - orbital speed too low"
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
                return;
            }

            double fuelRatio = 0;

            // determine fuel availability
            if (!CurrentPropellant.IsInfinite && !CheatOptions.InfinitePropellant && CurrentPropellant.ResourceDefinition.density > 0)
            {
                var requestedAmount = demandMass / (double)(decimal)CurrentPropellant.ResourceDefinition.density;
                if (IsValidPositiveNumber(requestedAmount))
                    fuelRatio = part.RequestResource(CurrentPropellant.Propellant.name, requestedAmount) / requestedAmount;
            }
            else
                fuelRatio = 1;

            if (!double.IsNaN(fuelRatio) && !double.IsInfinity(fuelRatio) && fuelRatio > 0)
            {
                vessel.orbit.Perturb(deltaVv * fuelRatio, universalTime);
            }

            if (thrust > 0.0000005 && fuelRatio < 0.999999 && _isFullyStarted)
            {
                message = Localizer.Format("#LOC_KSPIE_ElectricEngineController_PostMsg2", fuelRatio, thrust);// "Thrust warp stopped - " + + " propellant depleted thust: " +
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
            }
        }

        public void upgradePartModule()
        {
            isupgraded = true;
            type = upgradedtype;
            _vesselPropellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            engineTypeStr = upgradedName;

            if (!vacplasmaadded && type == (int)ElectricEngineType.VACUUMTHRUSTER)
            {
                vacplasmaadded = true;
                var node = new ConfigNode("RESOURCE");
                node.AddValue("name", InterstellarResourcesConfiguration.Instance.VacuumPlasma);
                node.AddValue("maxAmount", scaledMaxPower * 0.0000001);
                node.AddValue("amount", scaledMaxPower * 0.0000001);
                part.AddResource(node);
            }
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_ElectricEngine_maxPowerConsumption") + ": " + PluginHelper.getFormattedPowerString(maxPower * powerReqMult);
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title + (CurrentPropellant != null ? " (" + CurrentPropellant.PropellantGUIName + ")" : "");
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
            if (fuel_mode >= _vesselPropellants.Count)
                fuel_mode = 0;

            SetupPropellants(true);

            UpdateIsp();
        }

        private void TogglePreviousPropellant()
        {
            Debug.Log("[KSPI]: ElectricEngineControllerFX togglePreviousPropellant");
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = _vesselPropellants.Count - 1;

            SetupPropellants(false);

            UpdateIsp();
        }

        private double EvaluateMaxThrust(double powerSupply)
        {
            if (CurrentPropellant == null) return 0;

            if (_modifiedCurrentPropellantIspMultiplier <= 0) return 0;

            return CurrentPropellantEfficiency * GetPowerThrustModifier() * powerSupply / (_modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * GameConstants.STANDARD_GRAVITY);
        }

        private void UpdateIsp(double ispEfficiency = 1)
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
            var configNodes = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");

            List<ElectricEnginePropellant> propellantList;
            if (configNodes.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellantList = new List<ElectricEnginePropellant>();
            }
            else
                propellantList = configNodes.Select(prop => new ElectricEnginePropellant(prop)).ToList();

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
