using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Propulsion;

namespace FNPlugin
{
    class InterstellarMagneticNozzleControllerFX : FNResourceSuppliableModule, IEngineNoozle
    {
		//Persistent False
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiUnits = "m")]
		public float radius;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiUnits = " t")]
        public float partMass;
        [KSPField(isPersistant = false)]
        public double powerThrustMultiplier = 1.0;
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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Throtle Exponent")]
        protected double throtleExponent = 1;

		//remove then possible
        public bool static_updating = true;
        public bool static_updating2 = true;

		//Internal
		protected ModuleEnginesFX _attached_engine;
        protected ModuleEnginesWarp _attached_warpable_engine;
		protected IChargedParticleSource _attached_reactor;
        protected int _attached_reactor_distance;
        protected double exchanger_thrust_divisor;
        protected double calculatedIsp;
        protected double _previous_charged_particles_received;

        protected double minimum_isp;
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
			// calculate WasteHeat Capacity
			var wasteheatPowerResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);
			if (wasteheatPowerResource != null)
			{
				var wasteheat_ratio = Math.Min(wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount, 0.95);
				wasteheatPowerResource.maxAmount = part.mass * 2.0e+4 * wasteHeatMultiplier;
				wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * wasteheat_ratio;
			}
            
            if (state == StartState.Editor) return;

            _attached_engine = this.part.FindModuleImplementing<ModuleEnginesFX>();
            _attached_warpable_engine = _attached_engine as ModuleEnginesWarp;

            if (_attached_engine != null)
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";
            else
                Debug.Log("[KSPI] - InterstellarMagneticNozzleControllerFX.OnStart no ModuleEnginesFX found for MagneticNozzle!");

            // first try to look in part
            _attached_reactor = this.part.FindModuleImplementing<IChargedParticleSource>();

            // try to find nearest
            if (_attached_reactor == null)
                _attached_reactor = BreadthFirstSearchForChargedParticleSource(10, 1);

            if (_attached_reactor == null)
            {
                Debug.Log("[KSPI] - InterstellarMagneticNozzleControllerFX.OnStart no IChargedParticleSource found for MagneticNozzle!");
                return;
            }

            double joules_per_amu = _attached_reactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / GameConstants.dilution_factor;
            calculatedIsp = Math.Sqrt(joules_per_amu * 2.0 / GameConstants.ATOMIC_MASS_UNIT) / PluginHelper.GravityConstant;

            minimum_isp = calculatedIsp * _attached_reactor.MinimumChargdIspMult;
            maximum_isp = calculatedIsp * _attached_reactor.MaximumChargedIspMult;
            max_power_multiplier = Math.Log10(maximum_isp / minimum_isp);

            throtleExponent = Math.Abs(Math.Log10(_attached_reactor.MinimumChargdIspMult / _attached_reactor.MaximumChargedIspMult));

            exchanger_thrust_divisor = radius > _attached_reactor.GetRadius()
                ? _attached_reactor.GetRadius() * _attached_reactor.GetRadius() / radius / radius
                : radius * radius / _attached_reactor.GetRadius() / _attached_reactor.GetRadius();
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
           
		public void FixedUpdate() 
        {
            if (HighLogic.LoadedSceneIsFlight && _attached_engine != null && _attached_reactor != null && _attached_reactor.ChargedParticlePropulsionEfficiency > 0)
            {
                _max_charged_particles_power = _attached_reactor.MaximumChargedPower * exchanger_thrust_divisor * _attached_reactor.ChargedParticlePropulsionEfficiency;
                _charged_particles_requested = _attached_engine.isOperational && _attached_engine.currentThrottle > 0 ? _max_charged_particles_power : 0; 
                _charged_particles_received = consumeFNResourcePerSecond(_charged_particles_requested, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                // convert reactor product into propellants when possible
                var chargedParticleRatio = _attached_reactor.MaximumChargedPower > 0 ? _charged_particles_received / _attached_reactor.MaximumChargedPower : 0;

                var consumedByEngine = _attached_warpable_engine != null ? _attached_warpable_engine.propellantUsed : 0;
                _hydrogenProduction = !CheatOptions.InfinitePropellant && chargedParticleRatio > 0 ? _attached_reactor.UseProductForPropulsion(chargedParticleRatio, consumedByEngine) : 0;

                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    if (_attached_engine.isOperational && _attached_engine.currentThrottle > 0)
                    {
                        consumeFNResourcePerSecond(_charged_particles_received, FNResourceManager.FNRESOURCE_WASTEHEAT);
                        _previous_charged_particles_received = _charged_particles_received;
                    }
                    else if (_previous_charged_particles_received > 1)
                    {
                        consumeFNResourcePerSecond(_previous_charged_particles_received, FNResourceManager.FNRESOURCE_WASTEHEAT);
                        _previous_charged_particles_received /= 2;
                    }
                    else
                    {
                        _charged_particles_received = 0;
                        _previous_charged_particles_received = 0;
                    }
                }

                // update Isp
                double current_isp = !_attached_engine.isOperational || _attached_engine.currentThrottle == 0 ? maximum_isp : Math.Min(maximum_isp, minimum_isp / Math.Pow(_attached_engine.currentThrottle, throtleExponent));

                var ispPowerCostMultiplier = 1 + max_power_multiplier - Math.Log10(current_isp / minimum_isp);

                _requestedElectricPower = _charged_particles_received * ispPowerCostMultiplier * 0.005 * Math.Max(_attached_reactor_distance, 1);

                _recievedElectricPower = CheatOptions.InfiniteElectricity
                    ? _requestedElectricPower
                    : consumeFNResourcePerSecond(_requestedElectricPower, FNResourceManager.FNRESOURCE_MEGAJOULES);

                var megajoules_ratio = _recievedElectricPower / _requestedElectricPower;
                megajoules_ratio = (double.IsNaN(megajoules_ratio) || double.IsInfinity(megajoules_ratio)) ? 0 : megajoules_ratio;

                FloatCurve new_isp = new FloatCurve();
                new_isp.Add(0, (float)(current_isp * megajoules_ratio), 0, 0);
                _attached_engine.atmosphereCurve = new_isp;

                double atmo_thrust_factor = Math.Min(1.0, Math.Max(1.0 - Math.Pow(vessel.atmDensity, 0.2), 0));

                _engineMaxThrust = 0;
                if (_max_charged_particles_power > 0)
                {
                    double powerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                    var enginethrust_from_recieved_particles = powerThrustModifier * _charged_particles_received * megajoules_ratio * atmo_thrust_factor / current_isp / PluginHelper.GravityConstant;
                    var max_theoretical_thrust = powerThrustModifier * _max_charged_particles_power * atmo_thrust_factor / current_isp / PluginHelper.GravityConstant;

                    _engineMaxThrust = _attached_engine.currentThrottle > 0
                        ? Math.Max(enginethrust_from_recieved_particles, 0.000000001)
                        : Math.Max(max_theoretical_thrust, 0.000000001);
                }

                var max_fuel_flow_rate = !double.IsInfinity(_engineMaxThrust) && !double.IsNaN(_engineMaxThrust) && current_isp > 0
                    ? _engineMaxThrust / current_isp / PluginHelper.GravityConstant / (_attached_engine.currentThrottle > 0 ? _attached_engine.currentThrottle : 1)
                    : 0;

                // set maximum flow
                _attached_engine.maxFuelFlow = Math.Max((float)max_fuel_flow_rate, 0.0000000001f);

                // This whole thing may be inefficient, but it should clear up some confusion for people.
                if (!_attached_engine.getFlameoutState)
                {
                    if (megajoules_ratio < 0.75 && _requestedElectricPower > 0)
                        _attached_engine.status = "Insufficient Electricity";
                    else if (atmo_thrust_factor < 0.75)
                        _attached_engine.status = "Too dense atmospherere";
                }
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