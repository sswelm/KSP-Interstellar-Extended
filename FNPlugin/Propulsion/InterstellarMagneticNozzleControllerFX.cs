using FNPlugin.Constants;
using FNPlugin.Power;
using FNPlugin.Redist;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Linq;
using FNPlugin.Powermanagement;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    class InterstellarMagneticNozzleControllerFX : ResourceSuppliableModule, IFNEngineNoozle
    {
        public const string GROUP = "MagneticNozzleController";
        public const string GROUP_TITLE = "#LOC_KSPIE_MagneticNozzleControllerFX_groupName";

        //Persistent
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_SimulatedThrottle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]//Simulated Throttle
        public float simulatedThrottle = 0.5f;
        [KSPField(isPersistant = true)]
        double powerBufferStore;
        [KSPField(isPersistant = true)]
        public bool exhaustAllowed = true;

        // Non Persistant fields
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_ThermalNozzleController_Radius", guiActiveEditor = true, guiFormat = "F2", guiUnits = "m")]
        public double radius = 2.5;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiActiveEditor = true, guiFormat = "F3", guiUnits = " t")]
        public float partMass = 1;
        [KSPField]
        public bool showPartMass = true;
        [KSPField]
        public double powerThrustMultiplier = 1;
        [KSPField]
        public float wasteHeatMultiplier = 1;
        [KSPField]
        public bool maintainsPropellantBuffer = true;
        [KSPField]
        public double minimumPropellantBuffer = 0.01;
        [KSPField]
        public string propellantBufferResourceName = "LqdHydrogen";
        [KSPField]
        public string runningEffectName = String.Empty;
        [KSPField]
        public string powerEffectName = String.Empty;

        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_ChargedParticleMaximumPercentageUsage", guiFormat = "F3")]//CP max fraction usage
        private double _chargedParticleMaximumPercentageUsage;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_MaxChargedParticlesPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Max CP Power
        private double _max_charged_particles_power;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RequestedParticles", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Requested Particles
        private double _charged_particles_requested;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RecievedParticles", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Recieved Particles
        private double _charged_particles_received;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RequestedElectricity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Requested Electricity
        private double _requestedElectricPower;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RecievedElectricity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Recieved Electricity
        private double _recievedElectricPower;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Thrust", guiUnits = " kN")]//Thrust
        private double _engineMaxThrust;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Consumption", guiUnits = " kg/s")]//Consumption
        private double calculatedConsumptionPerSecond;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_ThrotleExponent")]//Throtle Exponent
        protected double throtleExponent = 1;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_MaximumChargedPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Maximum ChargedPower
        protected double maximumChargedPower;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_PowerThrustModifier", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F1")]//Power Thrust Modifier
        protected double powerThrustModifier;
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Minimumisp", guiUnits = " s", guiFormat = "F1")]//Minimum isp
        protected double minimum_isp;
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Maximumisp", guiUnits = " s", guiFormat = "F1")]//Maximum isp
        protected double maximum_isp;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_PowerRatio")]//Power Ratio
        protected double megajoulesRatio;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_EngineIsp")]//Engine Isp
        protected double engineIsp;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_EngineFuelFlow")]//Engine Fuel Flow
        protected float engineFuelFlow;
        [KSPField(guiActive = false)]
        protected double chargedParticleRatio;
        [KSPField(guiActive = false)]
        protected double max_theoretical_thrust;
        [KSPField(guiActive = false)]
        protected double max_theoratical_fuel_flow_rate;
        [KSPField(guiActive = false)]
        protected double currentIsp;
        [KSPField(guiActive = false)]
        protected float currentThrust;
        [KSPField(guiActive = false)]
        protected double wasteheatConsumption;

        //Internal
        UI_FloatRange simulatedThrottleFloatRange;
        ModuleEnginesFX _attached_engine;
        ModuleEnginesWarp _attached_warpable_engine;
        ResourceBuffers resourceBuffers;
        PartResourceDefinition propellantBufferResourceDefinition;
        Guid id = Guid.NewGuid();

        IFNChargedParticleSource _attached_reactor;

        int _attached_reactor_distance;
        double exchanger_thrust_divisor;
        double _previous_charged_particles_received;
        double max_power_multiplier;
        double powerBufferMax;

        public IFNChargedParticleSource AttachedReactor
        {
            get { return _attached_reactor; }
            private set
            {
                _attached_reactor = value;
                if (_attached_reactor == null)
                    return;
                _attached_reactor.AttachThermalReciever(id, radius);
            }
        }

        public double GetNozzleFlowRate()
        {
            return _attached_engine.maxFuelFlow;
        }

        public bool PropellantAbsorbsNeutrons { get { return false; } }

        public bool RequiresPlasmaHeat { get { return false; } }

        public bool RequiresThermalHeat { get { return false; } }

        public float CurrentThrottle { get { return _attached_engine.currentThrottle > 0 ? (maximum_isp == minimum_isp ? _attached_engine.currentThrottle : 1) : 0; } }

        public bool RequiresChargedPower { get { return true; } }

        public override void OnStart(PartModule.StartState state)
        {
            if (maintainsPropellantBuffer)
                propellantBufferResourceDefinition = PartResourceLibrary.Instance.GetDefinition(propellantBufferResourceName);

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, 1.0e+6, true));
            resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);
            resourceBuffers.Init(this.part);

            _attached_warpable_engine = this.part.FindModuleImplementing<ModuleEnginesWarp>();
            _attached_engine = _attached_warpable_engine;

            if (_attached_engine != null)
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";

            ConnectToReactor();

            UpdateEngineStats(true);

            max_power_multiplier = Math.Log10(maximum_isp / minimum_isp);

            throtleExponent = Math.Abs(Math.Log10(_attached_reactor.MinimumChargdIspMult / _attached_reactor.MaximumChargedIspMult));

            simulatedThrottleFloatRange = Fields["simulatedThrottle"].uiControlEditor as UI_FloatRange;
            simulatedThrottleFloatRange.onFieldChanged += UpdateFromGUI;

            if (_attached_reactor == null)
            {
                Debug.LogWarning("[KSPI]: InterstellarMagneticNozzleControllerFX.OnStart no IChargedParticleSource found for MagneticNozzle!");
                return;
            }
            exchanger_thrust_divisor = radius >= _attached_reactor.Radius ? 1 : radius * radius / _attached_reactor.Radius / _attached_reactor.Radius;

            InitializesPropellantBuffer();

            if (_attached_engine != null && _attached_engine is ModuleEnginesFX)
            {
                if (!String.IsNullOrEmpty(runningEffectName))
                    part.Effect(runningEffectName, 0, -1);
                if (!String.IsNullOrEmpty(powerEffectName))
                    part.Effect(powerEffectName, 0, -1);
            }

            Fields["partMass"].guiActiveEditor = showPartMass;
            Fields["partMass"].guiActive = showPartMass;
        }

        private void InitializesPropellantBuffer()
        {
            if (maintainsPropellantBuffer && string.IsNullOrEmpty(propellantBufferResourceName) == false && part.Resources[propellantBufferResourceName] == null)
            {
                Debug.Log("[KSPI]: Added " + propellantBufferResourceName + " buffer to MagneticNozzle");
                var newResourceNode = new ConfigNode("RESOURCE");
                newResourceNode.AddValue("name", propellantBufferResourceName);
                newResourceNode.AddValue("maxAmount", minimumPropellantBuffer);
                newResourceNode.AddValue("amount", minimumPropellantBuffer);

                part.AddResource(newResourceNode);
            }

            var bufferResource = part.Resources[propellantBufferResourceName];
            if (maintainsPropellantBuffer && bufferResource != null)
                bufferResource.amount = bufferResource.maxAmount;
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI]: attach " + part.partInfo.title);

                if (HighLogic.LoadedSceneIsEditor && _attached_engine != null)
                {
                    ConnectToReactor();

                    UpdateEngineStats(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.OnEditorAttach " + e.Message);
            }
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateEngineStats(true);
        }

        private void ConnectToReactor()
        {
            // first try to look in part
            _attached_reactor = this.part.FindModuleImplementing<IFNChargedParticleSource>();

            // try to find nearest
            if (_attached_reactor == null)
                _attached_reactor = BreadthFirstSearchForChargedParticleSource(10, 1);

            if (_attached_reactor != null)
                _attached_reactor.ConnectWithEngine(this);
        }

        private IFNChargedParticleSource BreadthFirstSearchForChargedParticleSource(int stackdepth, int parentdepth)
        {
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                IFNChargedParticleSource particleSource = FindChargedParticleSource(part, currentDepth, parentdepth);

                if (particleSource != null)
                {
                    _attached_reactor_distance = currentDepth;
                    return particleSource;
                }
            }
            return null;
        }

        private IFNChargedParticleSource FindChargedParticleSource(Part currentpart, int stackdepth, int parentdepth)
        {
            if (currentpart == null)
                return null;

            if (stackdepth == 0)
                return currentpart.FindModulesImplementing<IFNChargedParticleSource>().FirstOrDefault();

            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null))
            {
                IFNChargedParticleSource particleSource = FindChargedParticleSource(attachNodes.attachedPart, (stackdepth - 1), parentdepth);

                if (particleSource != null)
                    return particleSource;
            }

            if (parentdepth > 0)
            {
                IFNChargedParticleSource particleSource = FindChargedParticleSource(currentpart.parent, (stackdepth - 1), (parentdepth - 1));

                if (particleSource != null)
                    return particleSource;
            }

            return null;
        }

        private void UpdateEngineStats(bool useThrustCurve)
        {
            if (_attached_reactor == null || _attached_engine == null)
                return;

             // set Isp
            var joules_per_amu = _attached_reactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / GameConstants.dilution_factor;
            var calculatedIsp = Math.Sqrt(joules_per_amu * 2 / GameConstants.ATOMIC_MASS_UNIT) / GameConstants.STANDARD_GRAVITY;

            // calculte max and min isp
            minimum_isp = calculatedIsp * _attached_reactor.MinimumChargdIspMult;
            maximum_isp = calculatedIsp * _attached_reactor.MaximumChargedIspMult;

            if (useThrustCurve)
            {
                var currentIsp = Math.Min(maximum_isp, minimum_isp / Math.Pow(simulatedThrottle / 100, throtleExponent));

                FloatCurve newAtmosphereCurve = new FloatCurve();
                newAtmosphereCurve.Add(0, (float)currentIsp);
                newAtmosphereCurve.Add(0.002f, 0);
                _attached_engine.atmosphereCurve = newAtmosphereCurve;

                // set maximum fuel flow
                powerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                maximumChargedPower = _attached_reactor.MaximumChargedPower;
                powerBufferMax = maximumChargedPower / 10000;

                _engineMaxThrust = powerThrustModifier * maximumChargedPower / currentIsp / GameConstants.STANDARD_GRAVITY;
                var max_fuel_flow_rate = _engineMaxThrust / currentIsp / GameConstants.STANDARD_GRAVITY;
                _attached_engine.maxFuelFlow = (float)max_fuel_flow_rate;
                _attached_engine.maxThrust = (float)_engineMaxThrust;

                FloatCurve newThrustCurve = new FloatCurve();
                newThrustCurve.Add(0, (float)_engineMaxThrust);
                newThrustCurve.Add(0.001f, 0);

                _attached_engine.thrustCurve = newThrustCurve;
                _attached_engine.useThrustCurve = true;
            }
        }

        public virtual void Update()
        {
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight)
                UpdateEngineStats(false);
            else
                UpdateEngineStats(true);
        }

        // FixedUpdate is also called in the Editor
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            UpdateRunningEffect();
            UpdatePowerEffect();
        }


        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_attached_engine == null)
                return;

            if (_attached_engine.currentThrottle > 0 && !exhaustAllowed)
            {
                string message = AttachedReactor.MayExhaustInLowSpaceHomeworld
                    ? Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_PostMsg1")//"Engine halted - Radioactive exhaust not allowed towards or inside homeworld atmosphere"
                    : Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_PostMsg2");//"Engine halted - Radioactive exhaust not allowed towards or near homeworld atmosphere"

                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                vessel.ctrlState.mainThrottle = 0;

                // Return to realtime
                if (vessel.packed)
                    TimeWarp.SetRate(0, true);
            }

            _chargedParticleMaximumPercentageUsage = _attached_reactor != null ? _attached_reactor.ChargedParticlePropulsionEfficiency : 0;

            if (_chargedParticleMaximumPercentageUsage > 0)
            {
                if (_attached_reactor.Part != this.part)
                {
                    resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);
                    resourceBuffers.UpdateBuffers();
                }

                maximumChargedPower =  _attached_reactor.MaximumChargedPower;
                var currentMaximumChargedPower = maximum_isp == minimum_isp ? maximumChargedPower * _attached_engine.currentThrottle : maximumChargedPower;

                _max_charged_particles_power = currentMaximumChargedPower * exchanger_thrust_divisor * _attached_reactor.ChargedParticlePropulsionEfficiency;
                _charged_particles_requested = exhaustAllowed && _attached_engine.isOperational && _attached_engine.currentThrottle > 0 ? _max_charged_particles_power : 0;

                _charged_particles_received = _charged_particles_requested > 0 ? consumeFNResourcePerSecond(_charged_particles_requested, ResourceSettings.Config.ChargedParticleInMegawatt) : 0;

                // update Isp
                currentIsp = !_attached_engine.isOperational || _attached_engine.currentThrottle == 0 ? maximum_isp : Math.Min(maximum_isp, minimum_isp / Math.Pow(_attached_engine.currentThrottle, throtleExponent));

                var powerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                var max_engine_thrust_at_max_isp = powerThrustModifier * _charged_particles_received / maximum_isp / GameConstants.STANDARD_GRAVITY;

                var calculatedConsumptionInTon = max_engine_thrust_at_max_isp / maximum_isp / GameConstants.STANDARD_GRAVITY;

                UpdatePropellantBuffer(calculatedConsumptionInTon);

                // convert reactor product into propellants when possible and generate addition propellant from reactor fuel consumption
                chargedParticleRatio = currentMaximumChargedPower > 0 ? _charged_particles_received / currentMaximumChargedPower : 0;
                _attached_reactor.UseProductForPropulsion(chargedParticleRatio, calculatedConsumptionInTon);

                calculatedConsumptionPerSecond = calculatedConsumptionInTon * 1000;

                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    if (_attached_engine.isOperational && _attached_engine.currentThrottle > 0)
                    {
                        wasteheatConsumption = _charged_particles_received > _previous_charged_particles_received
                            ? _charged_particles_received + (_charged_particles_received - _previous_charged_particles_received)
                            : _charged_particles_received - (_previous_charged_particles_received - _charged_particles_received);

                        _previous_charged_particles_received = _charged_particles_received;
                    }
                    //else if (_previous_charged_particles_received > 0)
                    //{
                    //    wasteheatConsumption = _previous_charged_particles_received;
                    //    _previous_charged_particles_received = 0;
                    //}
                    else
                    {
                        wasteheatConsumption = 0;
                        _charged_particles_received = 0;
                        _previous_charged_particles_received = 0;
                    }

                    consumeFNResourcePerSecond(wasteheatConsumption, ResourceSettings.Config.WasteHeatInMegawatt);
                }

                if (_charged_particles_received == 0)
                {
                    _chargedParticleMaximumPercentageUsage = 0;

                    UpdateRunningEffect();
                    UpdatePowerEffect();
                }

                // calculate power cost
                var ispPowerCostMultiplier = 1 + max_power_multiplier - Math.Log10(currentIsp / minimum_isp);
                var minimumEnginePower = _attached_reactor.MagneticNozzlePowerMult * _charged_particles_received * ispPowerCostMultiplier * 0.005 * Math.Max(_attached_reactor_distance, 1);
                var neededBufferPower = Math.Min(getResourceAvailability(ResourceSettings.Config.ElectricPowerInMegawatt) ,  Math.Min(Math.Max(powerBufferMax - powerBufferStore, 0), minimumEnginePower));
                _requestedElectricPower = minimumEnginePower + neededBufferPower;

                _recievedElectricPower = CheatOptions.InfiniteElectricity || _requestedElectricPower == 0
                    ? _requestedElectricPower
                    : consumeFNResourcePerSecond(_requestedElectricPower, ResourceSettings.Config.ElectricPowerInMegawatt);

                // adjust power buffer
                var powerSurplus = _recievedElectricPower - minimumEnginePower;
                if (powerSurplus < 0)
                {
                    var powerFromBuffer = Math.Min(-powerSurplus, powerBufferStore);
                    _recievedElectricPower += powerFromBuffer;
                    powerBufferStore -= powerFromBuffer;
                }
                else
                    powerBufferStore += powerSurplus;

                // calculate Power factor
                megajoulesRatio = Math.Min(_recievedElectricPower / minimumEnginePower, 1);
                megajoulesRatio = (double.IsNaN(megajoulesRatio) || double.IsInfinity(megajoulesRatio)) ? 0 : megajoulesRatio;
                var scaledPowerFactor = Math.Pow(megajoulesRatio, 0.5);

                double effectiveThrustRatio = 1;

                _engineMaxThrust = 0;
                if (_max_charged_particles_power > 0)
                {
                    var max_thrust = powerThrustModifier * _charged_particles_received * scaledPowerFactor / currentIsp / GameConstants.STANDARD_GRAVITY;

                    var effective_thrust = Math.Max(max_thrust - (radius * radius * vessel.atmDensity * 100), 0);

                    effectiveThrustRatio = max_thrust > 0 ? effective_thrust / max_thrust : 0;

                    _engineMaxThrust = _attached_engine.currentThrottle > 0
                        ? Math.Max(effective_thrust, 1e-9)
                        : Math.Max(max_thrust, 1e-9);
                }

                // set isp
                FloatCurve newAtmosphereCurve = new FloatCurve();
                engineIsp = _attached_engine.currentThrottle > 0 ? (currentIsp * scaledPowerFactor * effectiveThrustRatio) : currentIsp;
                newAtmosphereCurve.Add(0, (float)engineIsp, 0, 0);
                _attached_engine.atmosphereCurve = newAtmosphereCurve;

                var max_effective_fuel_flow_rate = !double.IsInfinity(_engineMaxThrust) && !double.IsNaN(_engineMaxThrust) && currentIsp > 0
                    ? _engineMaxThrust / currentIsp / GameConstants.STANDARD_GRAVITY / (_attached_engine.currentThrottle > 0 ? _attached_engine.currentThrottle : 1)
                    : 0;

                max_theoretical_thrust = powerThrustModifier * maximumChargedPower * _chargedParticleMaximumPercentageUsage / currentIsp / GameConstants.STANDARD_GRAVITY;
                max_theoratical_fuel_flow_rate = max_theoretical_thrust / currentIsp / GameConstants.STANDARD_GRAVITY;

                // set maximum flow
                engineFuelFlow = _attached_engine.currentThrottle > 0 ? Math.Max((float)max_effective_fuel_flow_rate, 1e-9f) : (float)max_theoratical_fuel_flow_rate;

                _attached_engine.maxFuelFlow = engineFuelFlow;
                _attached_engine.useThrustCurve = false;

                // This whole thing may be inefficient, but it should clear up some confusion for people.
                if (_attached_engine.getFlameoutState) return;

                if (_attached_engine.currentThrottle < 0.01)
                    _attached_engine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu1");//"offline"
                else if (megajoulesRatio < 0.75 && _requestedElectricPower > 0)
                    _attached_engine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu2");//"Insufficient Electricity"
                else if (effectiveThrustRatio < 0.01 && vessel.atmDensity > 0)
                    _attached_engine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu3");//"Too dense atmospherere"
            }
            else
            {
                _chargedParticleMaximumPercentageUsage = 0;
                _attached_engine.maxFuelFlow = 0.0000000001f;
                _recievedElectricPower = 0;
                _charged_particles_requested = 0;
                _charged_particles_received = 0;
                _engineMaxThrust = 0;
            }

            currentThrust = _attached_engine.GetCurrentThrust();
        }

        private void UpdatePowerEffect()
        {
            if (string.IsNullOrEmpty(powerEffectName))
                return;

            var powerEffectRatio = exhaustAllowed && _attached_engine != null && _attached_engine.isOperational && _chargedParticleMaximumPercentageUsage > 0 && currentThrust > 0 ? _attached_engine.currentThrottle : 0;
            part.Effect(powerEffectName, powerEffectRatio, -1);
        }

        private void UpdateRunningEffect()
        {
            if (string.IsNullOrEmpty(runningEffectName))
                return;

            var runningEffectRatio = exhaustAllowed && _attached_engine != null && _attached_engine.isOperational && _chargedParticleMaximumPercentageUsage > 0 && currentThrust > 0 ? _attached_engine.currentThrottle : 0;
            part.Effect(runningEffectName, runningEffectRatio, -1);
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate()
        {
            if (_attached_engine == null)
                return;

            exhaustAllowed = AllowedExhaust();
        }

        public void UpdatePropellantBuffer(double calculatedConsumptionInTon)
        {
            if (propellantBufferResourceDefinition == null)
                return;

            PartResource propellantPartResource = part.Resources[propellantBufferResourceName];

            if (propellantPartResource == null || propellantBufferResourceDefinition.density == 0)
                return;

            var newMaxAmount = Math.Max(minimumPropellantBuffer, 2 * TimeWarp.fixedDeltaTime * calculatedConsumptionInTon / propellantBufferResourceDefinition.density);

            var storageShortage = Math.Max(0, propellantPartResource.amount - newMaxAmount);

            propellantPartResource.maxAmount = newMaxAmount;
            propellantPartResource.amount = Math.Min(newMaxAmount, propellantPartResource.amount);

            if (storageShortage > 0)
                part.RequestResource(propellantBufferResourceName, -storageShortage);
        }

        public override string GetInfo()
        {
            return "";
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title;
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
    }
}
