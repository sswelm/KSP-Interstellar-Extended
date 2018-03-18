using FNPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin
{
    [KSPModule("#LOC_KSPIE_ElectricEngine_partModuleName")]
    class ElectricEngineControllerFX : ResourceSuppliableModule, IUpgradeableModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool isupgraded;
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
        public float baseISP = 1000;
        [KSPField]
        public float ispGears = 3;
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

        // GUI
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_warpThrust", guiFormat = "F6", guiUnits = "kN")]
        public double throtle_max_thrust;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_warpIsp", guiFormat = "F1", guiUnits = "s")]
        public double engineIsp;
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_maxPowerInput", guiUnits = " MW")]
        public double maxPower = 1000;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineMass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineType")]
        public string engineTypeStr = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_activePropellantName")]
        public string propNameStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerShare")]
        public string electricalPowerShareStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerRequested", guiFormat = "F3", guiUnits = " MW")]
        public double power_request;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerConsumption")]
        public string electricalPowerConsumptionStr = "";
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
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_maxThrottlePower", guiFormat = "F3", guiUnits = " MW")]
        public double maxThrottlePower;


        // privates
        const double OneThird = 1.0 / 3.0;

        double _g0 = PluginHelper.GravityConstant;
        double _modifiedEngineBaseIsp;
        double _electrical_share_f;
        double _electrical_consumption_f;
        double _previousAvailablePower = 0;
        double _heat_production_f;
        double _thrust_d = 0;
        double _isp_d = 0;
        double _throttle_d = 0;
        double _modifiedCurrentPropellantIspMultiplier;
        double _maxIsp;
        double _maxFuelFlowRate;

        int _rep;
        int _initializationCountdown;

        bool _hasrequiredupgrade;
        bool _hasGearTechnology;
        bool _warpToReal;

        List<ElectricEnginePropellant> _propellants;
        ModuleEngines _attachedEngine;

        // Properties
        public string UpgradeTechnology { get { return upgradeTechReq; } }
        public double MaxPower { get { return maxPower * powerReqMult; } }
        public double MaxEffectivePower { get { return MaxPower * CurrentPropellantEfficiency * ThermalEfficiency; } }
        public bool IsOperational {get { return _attachedEngine != null ? _attachedEngine.isOperational : false; } }

        private ElectricEnginePropellant _current_propellant;
        public ElectricEnginePropellant Current_propellant
        {
            get { return _current_propellant; }
            set
            {
                _current_propellant = value;
                _modifiedCurrentPropellantIspMultiplier = (PluginHelper.IspElectroPropellantModifierBase + CurrentIspMultiplier) / (1 + PluginHelper.IspNtrPropellantModifierBase);
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
            get { 
                return HighLogic.LoadedSceneIsFlight 
                ? CheatOptions.IgnoreMaxTemperature 
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
                    return 0.87 * Current_propellant.Efficiency;
                else if (type == (int)ElectricEngineType.VASIMR)
                    return Math.Max(1 - atmDensity, 0.00001) * (baseEfficency + ((1 - _attachedEngine.currentThrottle) * variableEfficency));
                else
                    return Current_propellant.Efficiency;
            }
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
            _initializationCountdown = 10;
            Debug.Log("[KSPI] - Start Initializing ElectricEngineControllerFX");
            try
            {
                // initialise resources
                this.resources_to_supply = new string[] { ResourceManager.FNRESOURCE_WASTEHEAT };
                base.OnStart(state);
                AttachToEngine();

                _g0 = PluginHelper.GravityConstant;
                _hasGearTechnology = String.IsNullOrEmpty(gearsTechReq) || PluginHelper.upgradeAvailable(gearsTechReq);
                _modifiedEngineBaseIsp = baseISP * PluginHelper.ElectricEngineIspMult;
                _hasrequiredupgrade = this.HasTechsRequiredToUpgrade();

                if (_hasrequiredupgrade && (isupgraded || state == StartState.Editor))
                    upgradePartModule();

                UpdateEngineTypeString();

                // calculate WasteHeat Capacity
                var wasteheatPowerResource = part.Resources.FirstOrDefault(r => r.resourceName == ResourceManager.FNRESOURCE_WASTEHEAT);
                if (wasteheatPowerResource != null)
                {
                    var wasteheatRatio = Math.Min(wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount, 0.95);
                    wasteheatPowerResource.maxAmount = part.mass * 2.0e+4 * wasteHeatMultiplier;
                    wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * wasteheatRatio;
                }

                // initialize propellant
                _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
                SetupPropellants(true);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Error OnStart ElectricEngineControllerFX " + e.Message);
            }
            Debug.Log("[KSPI] - End Initializing ElectricEngineControllerFX");
        }

        private void AttachToEngine()
        {
            _attachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (_attachedEngine != null)
                _attachedEngine.Fields["finalThrust"].guiFormat = "F5";
        }

        private void SetupPropellants(bool moveNext)
        {
            try
            {
                Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.First();

                if ((Current_propellant.SupportedEngines & type) != type)
                {
                    _rep++;
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
                        propellantConfigNode.AddValue("DrawGauge", "true");
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
                Debug.LogError("[KSPI] - SetupPropellants ElectricEngineControllerFX " + e.Message);
            }
        }

        public override void OnUpdate()
        {
            // Base class update
            base.OnUpdate();

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

            if (this.IsOperational)
            {
                Fields["electricalPowerShareStr"].guiActive = true;
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                Fields["efficiencyStr"].guiActive = true;
                electricalPowerShareStr = (100.0 * _electrical_share_f).ToString("0.00") + "%";
                electricalPowerConsumptionStr = _electrical_consumption_f.ToString("0.000") + " MW";
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
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
                Fields["efficiencyStr"].guiActive = false;
            }
        }


        // ReSharper disable once UnusedMember.Global
        public void Update()
        {
            propNameStr = Current_propellant != null ? Current_propellant.PropellantGUIName : "";
        }

        private float IspGears
        {
            get { return _hasGearTechnology ? ispGears : 1; }
        }

        private float ModifiedThrotte
        {
            get
            {
                return Current_propellant.SupportedEngines == 8
                    ? _attachedEngine.currentThrottle
                    : Math.Min(_attachedEngine.currentThrottle * IspGears, 1);
            }
        }

        private float ThrottleModifiedIsp()
        {
            return Current_propellant.SupportedEngines == 8
                ? 1
                : _attachedEngine.currentThrottle < (1f / IspGears)
                    ? IspGears
                    : IspGears - ((_attachedEngine.currentThrottle - (1f / IspGears)) * IspGears);
        }


        // ReSharper disable once UnusedMember.Global
        public void FixedUpdate()
        {
            if (_initializationCountdown > 0)
                _initializationCountdown--;

            if (!HighLogic.LoadedSceneIsFlight) return;

            if (_attachedEngine == null) return;

            if (_attachedEngine is ModuleEnginesFX)
                GetAllPropellants().ForEach(prop => part.Effect(prop.ParticleFXName, 0, -1)); // set all FX to zero

            if (Current_propellant == null) return;

            if (!this.vessel.packed && !_warpToReal)
                storedThrotle = vessel.ctrlState.mainThrottle;

            // retrieve power
            maxEffectivePower = MaxEffectivePower;
            var sumOfAllEffectivePower = vessel.FindPartModulesImplementing<ElectricEngineControllerFX>().Where(ee => ee.IsOperational).Sum(ee => ee.MaxEffectivePower);
            _electrical_share_f = sumOfAllEffectivePower > 0 ? maxEffectivePower / sumOfAllEffectivePower : 1;

            maxThrottlePower = maxEffectivePower * ModifiedThrotte;
            var currentPropellantEfficiency = CurrentPropellantEfficiency;

            if (CheatOptions.InfiniteElectricity)
            {
                power_request = maxThrottlePower;
            }
            else
            {
                var availablePower = Math.Max(getStableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES) - getCurrentHighPriorityResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES), 0);
                var megaJoulesBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);

                var effectiveResourceThrotling = megaJoulesBarRatio > OneThird ? 1 : megaJoulesBarRatio * 3;

                var powerPerEngine = effectiveResourceThrotling * ModifiedThrotte * EvaluateMaxThrust(availablePower * _electrical_share_f) * CurrentIspMultiplier * _modifiedEngineBaseIsp / GetPowerThrustModifier() * PluginHelper.GravityConstant;
                power_request = currentPropellantEfficiency <= 0 ? 0 : Math.Min(powerPerEngine / currentPropellantEfficiency, maxThrottlePower);
            }

            var powerReceived = CheatOptions.InfiniteElectricity 
                ? power_request
                : consumeFNResourcePerSecond(power_request, ResourceManager.FNRESOURCE_MEGAJOULES);

            // produce waste heat
            var heatToProduce = powerReceived * (1 - currentPropellantEfficiency) * Current_propellant.WasteHeatMultiplier;

            var heatProduction = CheatOptions.IgnoreMaxTemperature 
                ? heatToProduce
                : supplyFNResourcePerSecond(heatToProduce, ResourceManager.FNRESOURCE_WASTEHEAT);

            // update GUI Values
            _electrical_consumption_f = powerReceived;
            _heat_production_f = heatProduction;

            var effectiveIsp = _modifiedCurrentPropellantIspMultiplier * _modifiedEngineBaseIsp * ThrottleModifiedIsp();

            var maxThrustInSpace = currentPropellantEfficiency * CurrentPropellantThrustMultiplier * ModifiedThrotte * GetPowerThrustModifier() * powerReceived / (effectiveIsp * PluginHelper.GravityConstant);

            _maxIsp = _modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier * ThrottleModifiedIsp();
            _maxFuelFlowRate = _maxIsp <= 0 ? 0 : maxThrustInSpace / _maxIsp / PluginHelper.GravityConstant;

            var maxThrustWithCurrentThrottle = maxThrustInSpace * ModifiedThrotte;
            throtle_max_thrust = Current_propellant.SupportedEngines == 8
                ? maxThrustWithCurrentThrottle
                : Math.Max(maxThrustWithCurrentThrottle - (exitArea * FlightGlobals.getStaticPressure(vessel.transform.position)), 0);

            var throttle = _attachedEngine.currentThrottle > 0 ? Mathf.Max(_attachedEngine.currentThrottle, 0.01f) : 0;

            //if (ModifiedThrotte > 0)
            if (throttle > 0 && !this.vessel.packed)
            {
                if (IsValidPositiveNumber(throtle_max_thrust) && IsValidPositiveNumber(maxThrustWithCurrentThrottle))
                {
                    UpdateIsp(throtle_max_thrust / maxThrustWithCurrentThrottle);
                    _attachedEngine.maxFuelFlow = (float)Math.Max(_maxFuelFlowRate * (ModifiedThrotte / _attachedEngine.currentThrottle), 0.0000000001);
                }
                else
                {
                    UpdateIsp(0.000001);
                    _attachedEngine.maxFuelFlow = 0.0000000001f;
                }

                if (_attachedEngine is ModuleEnginesFX)
                    this.part.Effect(Current_propellant.ParticleFXName, Mathf.Min((float)Math.Pow(_electrical_consumption_f / maxEffectivePower, 0.5), _attachedEngine.finalThrust / _attachedEngine.maxThrust), -1);
            }
            else if (this.vessel.packed && _attachedEngine.enabled && FlightGlobals.ActiveVessel == vessel && throttle > 0 && _initializationCountdown == 0)
            {
                _warpToReal = true; // Set to true for transition to realtime

                PersistantThrust(TimeWarp.fixedDeltaTime, Planetarium.GetUniversalTime(), this.part.transform.up, this.vessel.GetTotalMass());
            }
            else
            {
                throtle_max_thrust = 0;
                var projected_max_thrust = Math.Max(maxThrustInSpace - (exitArea * FlightGlobals.getStaticPressure(vessel.transform.position)), 0);

                if (IsValidPositiveNumber(projected_max_thrust) && IsValidPositiveNumber(maxThrustInSpace))
                {
                    UpdateIsp(projected_max_thrust / maxThrustInSpace);
                    _attachedEngine.maxFuelFlow = (float)Math.Max(_maxFuelFlowRate, 0.0000000001);
                }
                else
                {
                    UpdateIsp(1);
                    _attachedEngine.maxFuelFlow = 0.0000000001f;
                }

                if (_attachedEngine is ModuleEnginesFX)
                    this.part.Effect(Current_propellant.ParticleFXName, 0, -1);
            }

            var vacuumPlasmaResource = part.Resources[InterstellarResourcesConfiguration.Instance.VacuumPlasma];
            if (isupgraded && vacuumPlasmaResource != null)
            {
                part.RequestResource(InterstellarResourcesConfiguration.Instance.VacuumPlasma, - vacuumPlasmaResource.maxAmount);
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

        private void PersistantThrust(float fixedDeltaTime, double universalTime, Vector3d thrustDirection, float vesselMass)
        {
            var propellantAverageDensity = Current_propellant.ResourceDefinition.density;

            double fuelRatio = 0;
            double demandMass;

            // determine fuel availability
            if (!CheatOptions.InfinitePropellant && propellantAverageDensity > 0)
            {
                thrustDirection.CalculateDeltaVV(vesselMass, fixedDeltaTime, throtle_max_thrust, engineIsp, out demandMass);

                var requestedAmount = demandMass / propellantAverageDensity;
                if (IsValidPositiveNumber(requestedAmount))
                    fuelRatio = part.RequestResource(Current_propellant.Propellant.name, requestedAmount) / requestedAmount;
            }
            else 
                fuelRatio = 1;

            var effectiveThrust = throtle_max_thrust * fuelRatio;

            var deltaVv = thrustDirection.CalculateDeltaVV(vesselMass, fixedDeltaTime, effectiveThrust, engineIsp, out demandMass);

            if (fuelRatio > 0.01)
                vessel.orbit.Perturb(deltaVv, universalTime);
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
                node.AddValue("maxAmount", maxPower * 0.0000001);
                node.AddValue("amount", maxPower * 0.0000001);
                this.part.AddResource(node);
            }
        }

        public override string GetInfo()
        {
            var props = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            var returnStr = Localizer.Format("#LOC_KSPIE_ElectricEngine_maxPowerConsumption") + " : " + MaxPower.ToString("F3") + " MW\n";
            var thrustPerMw = (2e6 * powerThrustMultiplier) / _g0 / (baseISP * PluginHelper.ElectricEngineIspMult) / 1000.0;
            props.ForEach(prop =>
            {
                var ispPropellantModifier = (PluginHelper.IspElectroPropellantModifierBase + (this.type == (int)ElectricEngineType.VASIMR ? prop.DecomposedIspMult : prop.IspMultiplier)) / (1 + PluginHelper.IspNtrPropellantModifierBase);
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
            Debug.Log("[KSPI] - ElectricEngineControllerFX toggleNextPropellant");
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
                fuel_mode = 0;

            SetupPropellants(true);
        }

        private void TogglePreviousPropellant()
        {
            Debug.Log("[KSPI] - ElectricEngineControllerFX togglePreviousPropellant");
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = _propellants.Count - 1;

            SetupPropellants(false);
        }

        private double EvaluateMaxThrust(double powerSupply)
        {
            if (Current_propellant == null) return 0;

            if (_modifiedCurrentPropellantIspMultiplier <= 0) return 0;

            return CurrentPropellantEfficiency * GetPowerThrustModifier() * powerSupply / (_modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * _g0);
        }

        private void UpdateIsp(double ispEfficiency)
        {
            var newIsp = new FloatCurve();
            engineIsp = ispEfficiency * _modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier * ThrottleModifiedIsp();
            newIsp.Add(0, (float)engineIsp);
            _attachedEngine.atmosphereCurve = newIsp;
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

        public override int getPowerPriority()
        {
            return 3;
        }
    }
}
