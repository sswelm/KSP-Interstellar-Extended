using System;
using System.Linq;
using FNPlugin.Reactors.Interfaces;
using UnityEngine;
using FNPlugin.Propulsion;
using FNPlugin.Extensions;

namespace FNPlugin
{
    class InterstellarMagneticNozzleControllerFX : ResourceSuppliableModule, IEngineNoozle
    {
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "K", guiName = "Simulated Throttle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float simulatedThrottle = 0.5f;

        //Persistent False
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiUnits = "m")]
        public float radius = 2.5f;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiUnits = " t")]
        public float partMass = 1;
        [KSPField(isPersistant = false)]
        public double powerThrustMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;

        // Non Persistant
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Max CP Power", guiUnits = " MW", guiFormat = "F3")]
        private double _max_charged_particles_power;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Particles", guiUnits = " MW")]
        private double _charged_particles_requested;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Recieved Particles", guiUnits = " MW")]
        private double _charged_particles_received;
        
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Electricity", guiUnits = " MW")]
        private double _requestedElectricPower;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Recieved Electricity", guiUnits = " MW")]
        private double _recievedElectricPower;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust", guiUnits = " kN")]
        private double _engineMaxThrust;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Free")]
        private double _hydrogenProduction;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Throtle Exponent")]
        protected double throtleExponent = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Calculated Isp", guiUnits = " s", guiFormat = "F1")]
        protected double calculatedIsp;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Maximum ChargedPower", guiUnits = " MW", guiFormat = "F1")]
        protected double maximumChargedPower;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Power Thrust Modifier", guiUnits = " MW", guiFormat = "F1")]
        protected double powerThrustModifier;

        //remove then possible
        public bool static_updating = true;
        public bool static_updating2 = true;

        //Internal
        protected UI_FloatRange simulatedThrottleFloatRange;
        protected ModuleEnginesFX _attached_engine;
        protected ModuleEnginesWarp _attached_warpable_engine;
        protected IChargedParticleSource _attached_reactor;
        protected int _attached_reactor_distance;
        protected double exchanger_thrust_divisor;
        
        protected double _previous_charged_particles_received;
        protected ResourceBuffers resourceBuffers;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Minimum isp", guiUnits = " s", guiFormat = "F1")]
        protected double minimum_isp;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Maximum isp", guiUnits = " s", guiFormat = "F1")]
        protected double maximum_isp;

        protected double max_power_multiplier;

        public double GetNozzleFlowRate()
        {
            return _attached_engine.maxFuelFlow;
        }

        public float CurrentThrottle {  get { return _attached_engine.currentThrottle > 0 ? 1 : 0; } }

        public bool RequiresChargedPower { get { return true; } }
        

        public override void OnStart(PartModule.StartState state)
        {
            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 2.0e+4, true));
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.Init(this.part);

            _attached_warpable_engine = this.part.FindModuleImplementing<ModuleEnginesWarp>();
            _attached_engine = _attached_warpable_engine;

            if (_attached_engine != null)
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";

            ConnectToReactor();

            UpdateEngineStats();

            max_power_multiplier = Math.Log10(maximum_isp / minimum_isp);

            throtleExponent = Math.Abs(Math.Log10(_attached_reactor.MinimumChargdIspMult / _attached_reactor.MaximumChargedIspMult));

            simulatedThrottleFloatRange = Fields["simulatedThrottle"].uiControlEditor as UI_FloatRange;
            simulatedThrottleFloatRange.onFieldChanged += UpdateFromGUI;   

            //if (state == StartState.Editor) return;

            if (_attached_reactor == null)
            {
                Debug.Log("[KSPI] - InterstellarMagneticNozzleControllerFX.OnStart no IChargedParticleSource found for MagneticNozzle!");
                return;
            }
            exchanger_thrust_divisor = radius > _attached_reactor.Radius
                ? _attached_reactor.Radius * _attached_reactor.Radius / radius / radius
                : radius * radius / _attached_reactor.Radius / _attached_reactor.Radius;
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI] - attach " + part.partInfo.title);

                if (HighLogic.LoadedSceneIsEditor && _attached_engine != null)
                {
                    ConnectToReactor();

                    UpdateEngineStats();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnEditorAttach " + e.Message);
            }
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateEngineStats();
        }

        private void ConnectToReactor()
        {
            // first try to look in part
            _attached_reactor = this.part.FindModuleImplementing<IChargedParticleSource>();

            // try to find nearest
            if (_attached_reactor == null)
                _attached_reactor = BreadthFirstSearchForChargedParticleSource(10, 1);
        }

        private IChargedParticleSource BreadthFirstSearchForChargedParticleSource(int stackdepth, int parentdepth)
        {
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                IChargedParticleSource particleSource = FindChargedParticleSource(part, currentDepth, parentdepth);

                if (particleSource != null)
                {
                    particleSource.ConnectWithEngine(this);

                    _attached_reactor_distance = currentDepth;
                    return particleSource;
                }
            }
            return null;
        }

        private IChargedParticleSource FindChargedParticleSource(Part currentpart, int stackdepth, int parentdepth)
        {
            if (currentpart == null)
                return null;

            if (stackdepth == 0)
                return currentpart.FindModulesImplementing<IChargedParticleSource>().FirstOrDefault();

            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null))
            {
                IChargedParticleSource particleSource = FindChargedParticleSource(attachNodes.attachedPart, (stackdepth - 1), parentdepth);

                if (particleSource != null)
                    return particleSource;
            }

            if (parentdepth > 0)
            {
                IChargedParticleSource particleSource = FindChargedParticleSource(currentpart.parent, (stackdepth - 1), (parentdepth - 1));

                if (particleSource != null)
                    return particleSource;
            }

            return null;
        }

        private void UpdateEngineStats()
        {
            if (_attached_reactor == null || _attached_engine == null)
                return;

             // set Isp
            double joules_per_amu = _attached_reactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / GameConstants.dilution_factor;
            calculatedIsp = Math.Sqrt(joules_per_amu * 2.0 / GameConstants.ATOMIC_MASS_UNIT) / PluginHelper.GravityConstant;

            minimum_isp = calculatedIsp * _attached_reactor.MinimumChargdIspMult;
            maximum_isp = calculatedIsp * _attached_reactor.MaximumChargedIspMult;

            var currentIsp = Math.Min(maximum_isp, minimum_isp / Math.Pow(simulatedThrottle / 100, throtleExponent));

            FloatCurve newIsp = new FloatCurve();
            newIsp.Add(0, (float)currentIsp);
            //newIsp.Add(1, 0);
            _attached_engine.atmosphereCurve = newIsp;

            // set maximum fuel flow
            powerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
            maximumChargedPower = _attached_reactor.MaximumChargedPower;

            _engineMaxThrust = powerThrustModifier * maximumChargedPower / currentIsp / PluginHelper.GravityConstant;
            var max_fuel_flow_rate = _engineMaxThrust / currentIsp / PluginHelper.GravityConstant;
            _attached_engine.maxFuelFlow = (float)max_fuel_flow_rate;
            _attached_engine.maxThrust = (float)_engineMaxThrust;
        }

        // FixedUpdate is also called in the Editor
        public void FixedUpdate() 
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.UpdateBuffers();

            if (HighLogic.LoadedSceneIsFlight && _attached_engine != null && _attached_reactor != null && _attached_reactor.ChargedParticlePropulsionEfficiency > 0)
            {
                _max_charged_particles_power = _attached_reactor.MaximumChargedPower * exchanger_thrust_divisor * _attached_reactor.ChargedParticlePropulsionEfficiency;
                _charged_particles_requested = _attached_engine.isOperational && _attached_engine.currentThrottle > 0 ? _max_charged_particles_power : 0;
                _charged_particles_received = consumeFNResourcePerSecond(_charged_particles_requested, ResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                // convert reactor product into propellants when possible
                var chargedParticleRatio = _attached_reactor.MaximumChargedPower > 0 ? _charged_particles_received / _attached_reactor.MaximumChargedPower : 0;

                var consumedByEngine = _attached_warpable_engine != null ? _attached_warpable_engine.propellantUsed : 0;
                _hydrogenProduction = !CheatOptions.InfinitePropellant && chargedParticleRatio > 0 ? _attached_reactor.UseProductForPropulsion(chargedParticleRatio, consumedByEngine) : 0;

                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    if (_attached_engine.isOperational && _attached_engine.currentThrottle > 0)
                    {
                        consumeFNResourcePerSecond(_charged_particles_received, ResourceManager.FNRESOURCE_WASTEHEAT);
                        _previous_charged_particles_received = _charged_particles_received;
                    }
                    else if (_previous_charged_particles_received > 0)
                    {
                        consumeFNResourcePerSecond(_previous_charged_particles_received, ResourceManager.FNRESOURCE_WASTEHEAT);
                        _previous_charged_particles_received = 0;
                    }
                    else
                    {
                        _charged_particles_received = 0;
                        _previous_charged_particles_received = 0;
                    }
                }

                // update Isp
                var currentIsp = !_attached_engine.isOperational || _attached_engine.currentThrottle == 0 ? maximum_isp : Math.Min(maximum_isp, minimum_isp / Math.Pow(_attached_engine.currentThrottle, throtleExponent));

                var ispPowerCostMultiplier = 1 + max_power_multiplier - Math.Log10(currentIsp / minimum_isp);

                _requestedElectricPower =  _attached_reactor.MagneticNozzlePowerMult * _charged_particles_received * ispPowerCostMultiplier * 0.005 * Math.Max(_attached_reactor_distance, 1);

                _recievedElectricPower = CheatOptions.InfiniteElectricity
                    ? _requestedElectricPower
                    : consumeFNResourcePerSecond(_requestedElectricPower, ResourceManager.FNRESOURCE_MEGAJOULES);

                var megajoulesRatio = _recievedElectricPower / _requestedElectricPower;
                megajoulesRatio = (double.IsNaN(megajoulesRatio) || double.IsInfinity(megajoulesRatio)) ? 0 : megajoulesRatio;

                FloatCurve newIsp = new FloatCurve();
                newIsp.Add(0, (float)(currentIsp * megajoulesRatio), 0, 0);
                _attached_engine.atmosphereCurve = newIsp;

                var atmoThrustFactor = Math.Min(1.0, Math.Max(1.0 - Math.Pow(vessel.atmDensity, 0.2), 0));

                _engineMaxThrust = 0;
                if (_max_charged_particles_power > 0)
                {
                    double powerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                    var enginethrust_from_recieved_particles = powerThrustModifier * _charged_particles_received * megajoulesRatio * atmoThrustFactor / currentIsp / PluginHelper.GravityConstant;
                    var max_theoretical_thrust = powerThrustModifier * _max_charged_particles_power * atmoThrustFactor / currentIsp / PluginHelper.GravityConstant;

                    _engineMaxThrust = _attached_engine.currentThrottle > 0
                        ? Math.Max(enginethrust_from_recieved_particles, 0.000000001)
                        : Math.Max(max_theoretical_thrust, 0.000000001);
                }

                var max_fuel_flow_rate = !double.IsInfinity(_engineMaxThrust) && !double.IsNaN(_engineMaxThrust) && currentIsp > 0
                    ? _engineMaxThrust / currentIsp / PluginHelper.GravityConstant / (_attached_engine.currentThrottle > 0 ? _attached_engine.currentThrottle : 1)
                    : 0;

                // set maximum flow
                _attached_engine.maxFuelFlow = Math.Max((float)max_fuel_flow_rate, 0.0000000001f);

                // This whole thing may be inefficient, but it should clear up some confusion for people.
                if (_attached_engine.getFlameoutState) return;

                if (megajoulesRatio < 0.75 && _requestedElectricPower > 0)
                    _attached_engine.status = "Insufficient Electricity";
                else if (atmoThrustFactor < 0.75)
                    _attached_engine.status = "Too dense atmospherere";
            } 
            else if (_attached_engine != null)
            {
                _attached_engine.maxFuelFlow = 0.0000000001f;
                _recievedElectricPower = 0;
                _charged_particles_requested = 0;
                _charged_particles_received = 0;
                _engineMaxThrust = 0;
            }
        }

        public override string GetInfo() 
        {
            return "";
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title;
        }
    }
}