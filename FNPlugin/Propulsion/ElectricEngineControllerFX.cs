﻿using FNPlugin.Extensions;
using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class ElectricEngineControllerFX : FNResourceSuppliableModule, IUpgradeableModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = true)]
        public bool vacplasmaadded = false;

        //Persistent False
        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
        [KSPField(isPersistant = false)]
        public string gearsTechReq;

        [KSPField(isPersistant = false)]
        public float powerReqMult = 1; 

        [KSPField(isPersistant = false)]
        public int type;
        [KSPField(isPersistant = false)]
        public int upgradedtype;
        [KSPField(isPersistant = false)]
        public float baseISP;
        [KSPField(isPersistant = false)]
        public float ispGears = 3;
        [KSPField(isPersistant = false)]
        public float exitArea = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Max Power Input", guiUnits = " MW")]
        public float maxPower;
        [KSPField(isPersistant = false, guiName = "Power Thrust Multiplier")]
        public float powerThrustMultiplier = 1.0f;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float baseEfficency = 0.3f;
        [KSPField(isPersistant = false)]
        public float variableEfficency = 0.3f;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;

        // GUI
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Type")]
        public string engineTypeStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant")]
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Share")]
        public string electricalPowerShareStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Recieved")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
        public string efficiencyStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
        public string heatProductionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Effect Power", guiFormat = "F3", guiUnits = " MW")]
        public double maxEffectivePower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Throttle Power", guiFormat = "F3", guiUnits = " MW")]
        public double maxThrottlePower;

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        // internal
        protected double _g0 = PluginHelper.GravityConstant;
        protected double _modifiedEngineBaseISP;
        protected List<ElectricEnginePropellant> _propellants;
        protected ModuleEngines _attached_engine;
        protected double _electrical_share_f = 0;
        protected double _electrical_consumption_f = 0;
        protected double _previousAvailablePower = 0;
        protected double _heat_production_f = 0;
        protected int _rep = 0;
        protected bool _hasrequiredupgrade;
        protected bool _hasGearTechnology;
        protected double _modifiedCurrentPropellantIspMultiplier;
        protected double _propellantIspMultiplierPowerLimitModifier;
        protected double _maxISP;
        protected double _max_fuel_flow_rate;

        // Numeric display values
        protected double thrust_d = 0;
        protected double isp_d = 0;
        protected double throttle_d = 0;

        private ElectricEnginePropellant _current_propellant;
        public ElectricEnginePropellant Current_propellant
        {
            get { return _current_propellant; }
            set
            {
                _current_propellant = value;
                _modifiedCurrentPropellantIspMultiplier = (PluginHelper.IspElectroPropellantModifierBase + CurrentIspMultiplier) / (1 + PluginHelper.IspNtrPropellantModifierBase);
                _propellantIspMultiplierPowerLimitModifier = _modifiedCurrentPropellantIspMultiplier + ((1 - _modifiedCurrentPropellantIspMultiplier) * PluginHelper.ElectricEnginePowerPropellantIspMultLimiter);
            }
        }

        public double MaxPower
        {
            get { return maxPower * powerReqMult; }
        }

        public float CurrentIspMultiplier
        {
            get { return type == (int)ElectricEngineType.VASIMR ? Current_propellant.DecomposedIspMult : Current_propellant.IspMultiplier; }
        }

        public double MaxEffectivePower
        {
            get { return MaxPower * CurrentPropellantEfficiency * ThermalEfficiency; }
        }

        public double ThermalEfficiency
        {
            get { return HighLogic.LoadedSceneIsFlight ? (1 - getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT)) : 1;}
        }

        public bool IsOperational
        {
            get { return _attached_engine != null ? _attached_engine.isOperational : false; }
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Next Propellant", active = true)]
        public void ToggleNextPropellantEvent()
        {
            toggleNextPropellant();
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Previous Propellant", active = true)]
        public void TogglePreviousPropellantEvent()
        {
            togglePreviousPropellant();
        }

        [KSPAction("Next Propellant")]
        public void ToggleNextPropellantAction(KSPActionParam param)
        {
            ToggleNextPropellantEvent();
        }

        [KSPAction("Previous Propellant")]
        public void TogglePreviousPropellantAction(KSPActionParam param)
        {
            TogglePreviousPropellantEvent();
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null) return;
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

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
            UnityEngine.Debug.Log("[KSPI] - Start Initializing ElectricEngineControllerFX");
            try
            {
                // initialise resources
                this.resources_to_supply = new string[] { FNResourceManager.FNRESOURCE_WASTEHEAT };
                base.OnStart(state);
                AttachToEngine();

                _g0 = PluginHelper.GravityConstant;
                _hasGearTechnology = String.IsNullOrEmpty(gearsTechReq) || PluginHelper.upgradeAvailable(gearsTechReq);
                _modifiedEngineBaseISP = baseISP * PluginHelper.ElectricEngineIspMult;
                _hasrequiredupgrade = this.HasTechsRequiredToUpgrade();

                if (_hasrequiredupgrade && (isupgraded || state == StartState.Editor))
                    upgradePartModule();
                UpdateEngineTypeString();

                // calculate WasteHeat Capacity
                var wasteheatPowerResource = part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT];
                if (wasteheatPowerResource != null)
                {
                    var ratio = wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount;
                    wasteheatPowerResource.maxAmount = part.mass * 1.0e+4 * wasteHeatMultiplier;
                    wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * ratio;
                }

                // initialize propellant
                _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
                SetupPropellants(true);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnStart ElectricEngineControllerFX " + e.Message);
            }
            UnityEngine.Debug.Log("[KSPI] - End Initializing ElectricEngineControllerFX");
        }

        private void AttachToEngine()
        {
            _attached_engine = this.part.FindModuleImplementing<ModuleEngines>();
            if (_attached_engine != null)
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";
        }

        private void SetupPropellants(bool moveNext)
        {
            try
            {
                Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();

                if ((Current_propellant.SupportedEngines & type) != type)
                {
                    _rep++;
                    togglePropellant(moveNext);
                    return;
                }

                Propellant new_propellant = Current_propellant.Propellant;

                List<Propellant> list_of_propellants = new List<Propellant>();
                list_of_propellants.Add(new_propellant);

                // if all propellant exist
                if (!list_of_propellants.Exists(prop => PartResourceLibrary.Instance.GetDefinition(prop.name) == null))
                {
                    //Get the Ignition state, i.e. is the engine shutdown or activated
                    var engineState = _attached_engine.getIgnitionState;

                    _attached_engine.Shutdown();

                    ConfigNode newPropNode = new ConfigNode();
                    foreach (var prop in list_of_propellants)
                    {
                        ConfigNode propellantConfigNode = newPropNode.AddNode("PROPELLANT");
                        propellantConfigNode.AddValue("name", prop.name);
                        propellantConfigNode.AddValue("ratio", prop.ratio);
                        propellantConfigNode.AddValue("DrawGauge", "true");
                    }
                    _attached_engine.Load(newPropNode);

                    if (engineState == true)
                        _attached_engine.Activate();
                }
                else if (_rep < _propellants.Count)
                {
                    _rep++;
                    togglePropellant(moveNext);
                    return;
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    // you can have any fuel you want in the editor but not in flight
                    //List<PartResource> totalpartresources = list_of_propellants.SelectMany(prop => part.GetConnectedResources(prop.name.Replace("LqdWater", "Water"))).ToList();
                    //if (!list_of_propellants.All(prop => totalpartresources.Select(pr => pr.resourceName).Contains(prop.name.Replace("LqdWater", "Water"))) && _rep < _propellants.Count)
                    //{
                    //    _rep++;
                    //    togglePropellant(moveNext);
                    //    return;
                    //}

                    var allVesselResourcesNames = part.vessel.parts.SelectMany(m => m.Resources).Select(m => m.resourceName).Distinct();
                    if (!list_of_propellants.All(prop => allVesselResourcesNames.Contains(prop.name.Replace("LqdWater", "Water"))) && _rep < _propellants.Count)
                    {
                        _rep++;
                        togglePropellant(moveNext);
                        return;
                    }
                }

                _rep = 0;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - SetupPropellants ElectricEngineControllerFX " + e.Message);
            }
        }

        // Format thrust into mN, N, kN
        public static string FormatThrust(double thrust)
        {
            if (thrust < 0.001)
                return Math.Round(thrust * 1000000.0, 3).ToString() + " mN";
            else if (thrust < 1.0)
                return Math.Round(thrust * 1000.0, 3).ToString() + " N";
            else
                return Math.Round(thrust, 3).ToString() + " kN";
        }

        public override void OnUpdate()
        {
            // Base class update
            base.OnUpdate();

            if (ResearchAndDevelopment.Instance != null)
            {
                Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasrequiredupgrade;
                Fields["upgradeCostStr"].guiActive = !isupgraded && _hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
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
                    efficiencyStr = (CurrentPropellantEfficiency * 100.0).ToString("0.00") + "%";
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
                    ? _attached_engine.currentThrottle
                    : Math.Min(_attached_engine.currentThrottle * IspGears, 1);
            }
        }

        private float ThrottleModifiedIsp()
        {
            return Current_propellant.SupportedEngines == 8
                ? 1
                : _attached_engine.currentThrottle < (1f / IspGears)
                    ? IspGears
                    : IspGears - ((_attached_engine.currentThrottle - (1f / IspGears)) * IspGears);
        }

        // Low thrust acceleration
        public static Vector3d CalculateLowThrustForce(Part part, float thrust, Vector3d up)
        {
            if (part != null)
                return up * thrust;
            else
                return Vector3d.zero;
        }

        public static double CalculateDeltaV(float Isp, float m0, float thrust, double dT)
        {
            // Mass flow rate
            double mdot = thrust / (Isp * 9.81);
            // Final mass
            double m1 = m0 - mdot * dT;
            // DeltaV
            return Isp * 9.81 * Math.Log(m0 / m1);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (_attached_engine == null) return;

            if (_attached_engine is ModuleEnginesFX)
                ElectricEngineControllerFX.getAllPropellants().ForEach(prop => part.Effect(prop.ParticleFXName, 0, -1)); // set all FX to zero

            if (Current_propellant == null) return;

            // retrieve power
            maxEffectivePower = MaxEffectivePower;
            var sumOfAllEffectivePower = vessel.FindPartModulesImplementing<ElectricEngineControllerFX>().Where(ee => ee.IsOperational).Sum(ee => ee.MaxEffectivePower);
            _electrical_share_f = sumOfAllEffectivePower > 0 ? maxEffectivePower / sumOfAllEffectivePower : 1;
            var availablePower = Math.Max(getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) - getCurrentHighPriorityResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES), 0);
            var powerAvailableForThisEngine = availablePower * _electrical_share_f;

            var currentPropellantEfficiency = CurrentPropellantEfficiency;
            var power_per_engine = ModifiedThrotte * EvaluateMaxThrust(powerAvailableForThisEngine) * CurrentIspMultiplier * _modifiedEngineBaseISP / GetPowerThrustModifier() * _g0;

            maxThrottlePower = maxEffectivePower * ModifiedThrotte;
            var power_request = currentPropellantEfficiency <= 0 ? 0
                : Math.Min(power_per_engine * TimeWarp.fixedDeltaTime / currentPropellantEfficiency, maxThrottlePower * TimeWarp.fixedDeltaTime);

            var power_received = consumeFNResource(power_request, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;

            // produce waste heat
            var heat_to_produce = power_received * (1 - currentPropellantEfficiency) * Current_propellant.WasteHeatMultiplier;
            var heat_production = supplyFNResource(heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

            // update GUI Values
            _electrical_consumption_f = power_received;
            _heat_production_f = heat_production;

            var max_thrust_in_space = currentPropellantEfficiency * CurrentPropellantThrustMultiplier * GetPowerThrustModifier() * power_received / (_modifiedCurrentPropellantIspMultiplier * _modifiedEngineBaseISP * ThrottleModifiedIsp() * _g0 * ModifiedThrotte);

            _maxISP = _modifiedEngineBaseISP * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier * ThrottleModifiedIsp();
            _max_fuel_flow_rate = _maxISP <= 0 ? 0 : max_thrust_in_space / _maxISP / PluginHelper.GravityConstant;

            if (ModifiedThrotte > 0)
            {
                var max_thrust_with_current_throttle = max_thrust_in_space * ModifiedThrotte;
                var actual_max_thrust = Current_propellant.SupportedEngines == 8 ? max_thrust_with_current_throttle
                    : Math.Max(max_thrust_with_current_throttle - (exitArea * FlightGlobals.getStaticPressure(vessel.transform.position)), 0);

                if (actual_max_thrust > 0 && !double.IsNaN(actual_max_thrust) && max_thrust_with_current_throttle > 0 && !double.IsNaN(max_thrust_with_current_throttle))
                {
                    updateISP(actual_max_thrust / max_thrust_with_current_throttle);
                    _attached_engine.maxFuelFlow = (float)Math.Max(_max_fuel_flow_rate * (ModifiedThrotte / _attached_engine.currentThrottle), 0.0000000001);
                }
                else
                {
                    updateISP(0.000001);
                    _attached_engine.maxFuelFlow = 0.0000000001f;
                }

                if (_attached_engine is ModuleEnginesFX)
                    this.part.Effect(Current_propellant.ParticleFXName, Mathf.Min((float)Math.Pow(_electrical_consumption_f / maxEffectivePower, 0.5), _attached_engine.finalThrust / _attached_engine.maxThrust), -1);
            }
            else
            {
                var actual_max_thrust = Math.Max(max_thrust_in_space - (exitArea * FlightGlobals.getStaticPressure(vessel.transform.position)), 0);

                if (!double.IsNaN(actual_max_thrust) && !double.IsInfinity(actual_max_thrust) && actual_max_thrust > 0 && max_thrust_in_space > 0)
                {
                    updateISP(actual_max_thrust / max_thrust_in_space);
                    _attached_engine.maxFuelFlow = (float)Math.Max(_max_fuel_flow_rate, 0.0000000001);
                }
                else
                {
                    updateISP(1);
                    _attached_engine.maxFuelFlow = 0.0000000001f;
                }

                if (_attached_engine is ModuleEnginesFX)
                    this.part.Effect(Current_propellant.ParticleFXName, 0, -1);
            }

            if (isupgraded)
                this.part.RequestResource(InterstellarResourcesConfiguration.Instance.VacuumPlasma, -part.Resources[InterstellarResourcesConfiguration.Instance.VacuumPlasma].maxAmount);
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
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", InterstellarResourcesConfiguration.Instance.VacuumPlasma);
                node.AddValue("maxAmount", MaxPower / 100);
                node.AddValue("possibleAmount", MaxPower / 100);
                this.part.AddResource(node);
                //this.part.Resources.UpdateList();
                //part.vessel.UpdateVesselResourceSet();
            }
        }

        protected double CurrentPropellantThrustMultiplier
        {
            get { return type == (int)ElectricEngineType.ARCJET ? Current_propellant.ThrustMultiplier : 1; }
        }
        protected double CurrentPropellantEfficiency
        {
            get
            {
                var atmDensity = HighLogic.LoadedSceneIsFlight ? vessel.atmDensity : 0;

                if (type == (int)ElectricEngineType.ARCJET)
                    return 0.87 * Current_propellant.Efficiency;
                else if (type == (int)ElectricEngineType.VASIMR)
                    return Math.Max(1 - atmDensity, 0.00001) * (baseEfficency + ((1 - _attached_engine.currentThrottle) * variableEfficency));
                else
                    return Current_propellant.Efficiency;
            }
        }


        public override string GetInfo()
        {
            var powerThrustModifier = GetPowerThrustModifier();
            List<ElectricEnginePropellant> props = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            string return_str = "Max Power Consumption: " + MaxPower.ToString("") + " MW\n";
            var thrust_per_mw = (2e6 * powerThrustMultiplier) / _g0 / (baseISP * PluginHelper.ElectricEngineIspMult) / 1000.0;
            props.ForEach(prop =>
            {
                var ispPropellantModifier = (PluginHelper.IspElectroPropellantModifierBase + (this.type == (int)ElectricEngineType.VASIMR ? prop.DecomposedIspMult : prop.IspMultiplier)) / (1 + PluginHelper.IspNtrPropellantModifierBase);
                var ispProp = _modifiedEngineBaseISP * ispPropellantModifier;

                double efficiency;
                if (type == (int)ElectricEngineType.ARCJET)
                    efficiency = 0.87 * prop.Efficiency;
                else if (type == (int)ElectricEngineType.VASIMR)
                    efficiency = baseEfficency + 0.5 * variableEfficency;
                else
                    efficiency = prop.Efficiency;

                var thrustProp = thrust_per_mw / ispPropellantModifier * efficiency * (type == (int)ElectricEngineType.ARCJET ? prop.ThrustMultiplier : 1);
                return_str = return_str + "---" + prop.PropellantGUIName + "---\nThrust: " + thrustProp.ToString("0.000") + " kN per MW\nEfficiency: " + (efficiency * 100.0).ToString("0.00") + "%\nISP: " + ispProp.ToString("0.00") + "s\n";
            });
            return return_str;
        }

        public override string getResourceManagerDisplayName()
        {
            return engineTypeStr + " Thruster" + (Current_propellant != null ? "(" + Current_propellant.PropellantGUIName + ")" : "");
        }

        protected void togglePropellant(bool next)
        {
            if (next)
                toggleNextPropellant();
            else
                togglePreviousPropellant();
        }

        protected void toggleNextPropellant()
        {
            UnityEngine.Debug.Log("[KSPI] - ElectricEngineControllerFX toggleNextPropellant");
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
                fuel_mode = 0;

            SetupPropellants(true);
        }

        protected void togglePreviousPropellant()
        {
            UnityEngine.Debug.Log("[KSPI] - ElectricEngineControllerFX togglePreviousPropellant");
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = _propellants.Count - 1;

            SetupPropellants(false);
        }

        protected double EvaluateMaxThrust(double power_supply)
        {
            if (Current_propellant == null) return 0;

            if (_modifiedCurrentPropellantIspMultiplier <= 0) return 0;

            return CurrentPropellantEfficiency * GetPowerThrustModifier() * power_supply / (_modifiedEngineBaseISP * _modifiedCurrentPropellantIspMultiplier * _g0);
        }

        protected void updateISP(double isp_efficiency)
        {
            FloatCurve newISP = new FloatCurve();
            newISP.Add(0, (float)(isp_efficiency * _modifiedEngineBaseISP * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier * ThrottleModifiedIsp()));
            _attached_engine.atmosphereCurve = newISP;
        }

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginHelper.GlobalElectricEnginePowerMaxThrustMult * this.powerThrustMultiplier;
        }

        private double GetAtmosphericDensityModifier()
        {
            return Math.Max(1.0 - (part.vessel.atmDensity * PluginHelper.ElectricEngineAtmosphericDensityThrustLimiter), 0.0);
        }

        protected static List<ElectricEnginePropellant> getAllPropellants()
        { 
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            }
            else
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).ToList();

            return propellant_list;
        }
    }
}
