using FNPlugin.Constants;
using FNPlugin.Power;
using FNPlugin.Propulsion;
using FNPlugin.Redist;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class InterstellarMagneticNozzleControllerFX : ResourceSuppliableModule, IEngineNoozle
    {
        //Persistent
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Simulated Throttle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float simulatedThrottle = 0.5f;
        [KSPField(isPersistant = true)]
        double powerBufferStore;

        // Non Persistant fields
        [KSPField(guiActiveEditor = true, guiUnits = "m")]
        public float radius = 2.5f;
        [KSPField(guiActiveEditor = true, guiUnits = " t")]
        public float partMass = 1;
        [KSPField]
        public bool showPartMass = true;
        [KSPField]
        public double powerThrustMultiplier = 1;
        [KSPField]
        public float wasteHeatMultiplier = 1;
        [KSPField(guiName = "Max CP Power", guiUnits = " MW", guiFormat = "F3")]
        private double _max_charged_particles_power;
        [KSPField(guiName = "Requested Particles", guiUnits = " MW")]
        private double _charged_particles_requested;
        [KSPField(guiName = "Recieved Particles", guiUnits = " MW")]
        private double _charged_particles_received;
        [KSPField(guiName = "Requested Electricity", guiUnits = " MW")]
        private double _requestedElectricPower;
        [KSPField(guiName = "Recieved Electricity", guiUnits = " MW")]
        private double _recievedElectricPower;
        [KSPField(guiName = "Thrust", guiUnits = " kN")]
        private double _engineMaxThrust;
        [KSPField(guiActive = true, guiName = "Calculated", guiUnits = " kg/s")]
        private double calculatedConsumptionPerSecond;

        [KSPField(guiName = "Throtle Exponent")]
        protected double throtleExponent = 1;
        [KSPField(guiName = "Maximum ChargedPower", guiUnits = " MW", guiFormat = "F1")]
        protected double maximumChargedPower;
        [KSPField(guiName = "Power Thrust Modifier", guiUnits = " MW", guiFormat = "F1")]
        protected double powerThrustModifier;
        [KSPField(guiActiveEditor = true, guiName = "Minimum isp", guiUnits = " s", guiFormat = "F1")]
        protected double minimum_isp;
        [KSPField(guiActiveEditor = true, guiName = "Maximum isp", guiUnits = " s", guiFormat = "F1")]
        protected double maximum_isp;
        [KSPField(guiName = "Power Ratio")]
        protected double megajoulesRatio;

        //Internal
        UI_FloatRange simulatedThrottleFloatRange;
        ModuleEnginesFX _attached_engine;
        ModuleEnginesWarp _attached_warpable_engine;
        IChargedParticleSource _attached_reactor;
        ResourceBuffers resourceBuffers;

        int _attached_reactor_distance;
        double exchanger_thrust_divisor;
        double _previous_charged_particles_received;
        double max_power_multiplier;
        double powerBufferMax;

        public double GetNozzleFlowRate()
        {
            return _attached_engine.maxFuelFlow;
        }

        public float CurrentThrottle {  get { return _attached_engine.currentThrottle > 0 ? 1 : 0; } }

        public bool RequiresChargedPower { get { return true; } }

        public override void OnStart(PartModule.StartState state)
        {
            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 1.0e+6, true));
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
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
 
            Fields["partMass"].guiActiveEditor = showPartMass;

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

                    UpdateEngineStats(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnEditorAttach " + e.Message);
            }
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateEngineStats(true);
        }

        private void ConnectToReactor()
        {
            // first try to look in part
            _attached_reactor = this.part.FindModuleImplementing<IChargedParticleSource>();

            // try to find nearest
            if (_attached_reactor == null)
                _attached_reactor = BreadthFirstSearchForChargedParticleSource(10, 1);

            if (_attached_reactor != null)
                _attached_reactor.ConnectWithEngine(this);
        }

        private IChargedParticleSource BreadthFirstSearchForChargedParticleSource(int stackdepth, int parentdepth)
        {
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                IChargedParticleSource particleSource = FindChargedParticleSource(part, currentDepth, parentdepth);

                if (particleSource != null)
                {
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

            if (_attached_engine == null)
                return;

            if (_attached_reactor != null && _attached_reactor.ChargedParticlePropulsionEfficiency > 0)
            {
                if (_attached_reactor.Part != this.part)
                {
                    resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                    resourceBuffers.UpdateBuffers();
                }

                _max_charged_particles_power = _attached_reactor.MaximumChargedPower * exchanger_thrust_divisor * _attached_reactor.ChargedParticlePropulsionEfficiency;
                _charged_particles_requested = _attached_engine.isOperational && _attached_engine.currentThrottle > 0 ? _max_charged_particles_power : 0;
                _charged_particles_received = consumeFNResourcePerSecond(_charged_particles_requested, ResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                // convert reactor product into propellants when possible
                var chargedParticleRatio = _attached_reactor.MaximumChargedPower > 0 ? _charged_particles_received / _attached_reactor.MaximumChargedPower : 0;

                // update Isp
                var currentIsp = !_attached_engine.isOperational || _attached_engine.currentThrottle == 0 ? maximum_isp : Math.Min(maximum_isp, minimum_isp / Math.Pow(_attached_engine.currentThrottle, throtleExponent));

                var powerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                var max_engine_thrust_at_max_isp = powerThrustModifier * _charged_particles_received / maximum_isp / GameConstants.STANDARD_GRAVITY;
                var calculatedConsumptionInTon = max_engine_thrust_at_max_isp / maximum_isp / GameConstants.STANDARD_GRAVITY;

                // generate addition propellant from reactor fuel consumption
                _attached_reactor.UseProductForPropulsion(chargedParticleRatio, calculatedConsumptionInTon);

                calculatedConsumptionPerSecond = calculatedConsumptionInTon * 1000;

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

                // calculate power cost
                var ispPowerCostMultiplier = 1 + max_power_multiplier - Math.Log10(currentIsp / minimum_isp);
                var minimumEnginePower = _attached_reactor.MagneticNozzlePowerMult * _charged_particles_received * ispPowerCostMultiplier * 0.005 * Math.Max(_attached_reactor_distance, 1);
                var neededBufferPower = Math.Min(Math.Max(powerBufferMax - powerBufferStore, 0), minimumEnginePower);
                _requestedElectricPower = minimumEnginePower + neededBufferPower;

                _recievedElectricPower = CheatOptions.InfiniteElectricity
                    ? _requestedElectricPower
                    : consumeFNResourcePerSecond(_requestedElectricPower, ResourceManager.FNRESOURCE_MEGAJOULES);

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

                double atmoIspFactor = 1;

                _engineMaxThrust = 0;
                if (_max_charged_particles_power > 0)
                {
                    var enginethrust_from_recieved_particles = powerThrustModifier * _charged_particles_received * scaledPowerFactor / currentIsp / GameConstants.STANDARD_GRAVITY;

                    var effective_thrust = Math.Max(enginethrust_from_recieved_particles - (radius * radius * vessel.atmDensity * 100), 0);

                    var max_theoretical_thrust = powerThrustModifier * _max_charged_particles_power / currentIsp / GameConstants.STANDARD_GRAVITY;

                    atmoIspFactor = max_theoretical_thrust > 0 ? effective_thrust / max_theoretical_thrust : 0;

                    _engineMaxThrust = _attached_engine.currentThrottle > 0
                        ? Math.Max(effective_thrust, 0.000000001)
                        : Math.Max(max_theoretical_thrust, 0.000000001);
                }

                // set isp
                FloatCurve newAtmosphereCurve = new FloatCurve();
                newAtmosphereCurve.Add(0, (float)(currentIsp * scaledPowerFactor * atmoIspFactor), 0, 0);
                _attached_engine.atmosphereCurve = newAtmosphereCurve;

                var max_fuel_flow_rate = !double.IsInfinity(_engineMaxThrust) && !double.IsNaN(_engineMaxThrust) && currentIsp > 0
                    ? _engineMaxThrust / currentIsp / GameConstants.STANDARD_GRAVITY / (_attached_engine.currentThrottle > 0 ? _attached_engine.currentThrottle : 1)
                    : 0;

                // set maximum flow
                _attached_engine.maxFuelFlow = Math.Max((float)max_fuel_flow_rate, 0.0000000001f);
                _attached_engine.useThrustCurve = false;

                // This whole thing may be inefficient, but it should clear up some confusion for people.
                if (_attached_engine.getFlameoutState) return;

                if (_attached_engine.currentThrottle < 0.99)
                    _attached_engine.status = "offline";
                else if (megajoulesRatio < 0.75 && _requestedElectricPower > 0)
                    _attached_engine.status = "Insufficient Electricity";
                else if (atmoIspFactor < 0.01)
                    _attached_engine.status = "Too dense atmospherere";
            } 
            else 
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