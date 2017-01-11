using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;
using FNPlugin.Propulsion;

namespace FNPlugin
{
    class InterstellarReactor : FNResourceSuppliableModule, IThermalSource, IRescalable<InterstellarReactor>
   { 
        //public enum ReactorTypes
        //{
        //    FISSION_MSR = 1,
        //    FISSION_GFR = 2,
        //    FUSION_DT = 4,
        //    FUSION_GEN3 = 8,
        //    AIM_FISSION_FUSION = 16,
        //    ANTIMATTER = 32
        //}

        // Persistent True
        [KSPField(isPersistant = true, guiActive = false)]
        public bool IsEnabled;
        [KSPField(isPersistant = true, guiActive = false)]
        public bool isDeployed = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool breedtritium;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Consumption Rate", guiFormat = "F2")]
        public double ongoing_consumption_rate;
        [KSPField(isPersistant = true)]
        public bool reactorInit;
        [KSPField(isPersistant = true)]
        public bool reactorBooted;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Start Enabled"), UI_Toggle(disabledText = "True", enabledText = "False")]
        public bool startDisabled;
        [KSPField(isPersistant = true)]
        public double neutronEmbrittlementDamage;
        [KSPField(isPersistant = true)]
        public double maxEmbrittlementFraction = 0.5;
        [KSPField(isPersistant = true)]
        public float windowPositionX = 20;
        [KSPField(isPersistant = true)]
        public float windowPositionY = 20;
        [KSPField(isPersistant = true)]
        public int currentGenerationType;
        [KSPField(isPersistant = true)]
        public double storedPowerMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double stored_fuel_ratio = 1;
        [KSPField(isPersistant = true)]
        public double reactor_power_ratio = 1;

        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk2 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk3 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk4 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk5 = null;

        [KSPField(isPersistant = false)]
        public float minimumThrottleMk1 = 0;
        [KSPField(isPersistant = false)]
        public float minimumThrottleMk2 = 0;
        [KSPField(isPersistant = false)]
        public float minimumThrottleMk3 = 0;
        [KSPField(isPersistant = false)]
        public float minimumThrottleMk4 = 0;
        [KSPField(isPersistant = false)]
        public float minimumThrottleMk5 = 0;

        [KSPField(isPersistant = false)]
        public float fuelEfficencyMk1 = 0;
        [KSPField(isPersistant = false)]
        public float fuelEfficencyMk2 = 0;
        [KSPField(isPersistant = false)]
        public float fuelEfficencyMk3 = 0;
        [KSPField(isPersistant = false)]
        public float fuelEfficencyMk4 = 0;
        [KSPField(isPersistant = false)]
        public float fuelEfficencyMk5 = 0;

        [KSPField(isPersistant = false)]
        public float coreTemperatureMk1 = 0;
        [KSPField(isPersistant = false)]
        public float coreTemperatureMk2 = 0;
        [KSPField(isPersistant = false)]
        public float coreTemperatureMk3 = 0;
        [KSPField(isPersistant = false)]
        public float coreTemperatureMk4 = 0;
        [KSPField(isPersistant = false)]
        public float coreTemperatureMk5 = 0;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float basePowerOutputMk1 = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float basePowerOutputMk2 = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float basePowerOutputMk3 = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float basePowerOutputMk4 = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float basePowerOutputMk5 = 0;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Power Output Mk1", guiUnits = " MJ")]
        public double powerOutputMk1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Power Output Mk2", guiUnits = " MJ")]
        public double powerOutputMk2;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Power Output Mk3", guiUnits = " MJ")]
        public double powerOutputMk3;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Power Output Mk4", guiUnits = " MJ")]
        public double powerOutputMk4;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Power Output Mk5", guiUnits = " MJ")]
        public double powerOutputMk5;

        // Settings
        [KSPField(isPersistant = false)]
        public int reactorModeTechBonus = 0;
        [KSPField(isPersistant = false)]
        public bool canBeCombinedWithLab = false;
        [KSPField(isPersistant = false)]
        public bool canBreedTritium = false;
        [KSPField(isPersistant = false)]
        public bool canDisableTritiumBreeding = true;
        [KSPField(isPersistant = false)]
        public bool disableAtZeroThrottle = false;
        [KSPField(isPersistant = false)]
        public bool controlledByEngineThrottle = false;
        [KSPField(isPersistant = false)]
        public bool showShutDownInFlight = false;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public float powerScaleExponent = 3;

        [KSPField(isPersistant = false)]
        public float emergencyPowerShutdownFraction = 0.95f;
        [KSPField(isPersistant = false)]
        public float breedDivider = 100000.0f;
        [KSPField(isPersistant = false)]
        public double bonusBufferFactor = 0.05;
        [KSPField(isPersistant = false)]
        public float heatTransportationEfficiency = 0.85f;
        [KSPField(isPersistant = false)]
        public float ReactorTemp = 0;
        [KSPField(isPersistant = false)]
        public float powerOutputMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float upgradedReactorTemp = 0;
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public string loopingAnimationName;
        [KSPField(isPersistant = false)]
        public string startupAnimationName;
        [KSPField(isPersistant = false)]
        public string shutdownAnimationName;

        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Radius")]
        public float radius;
        [KSPField(isPersistant = false)]
        public float minimumThrottle = 0;
        [KSPField(isPersistant = false)]
        public bool canShutdown = true;
        [KSPField(isPersistant = false)]
        public bool consumeGlobal;
        [KSPField(isPersistant = false)]
        public int reactorType;
        [KSPField(isPersistant = false)]
        public float fuelEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float upgradedFuelEfficiency = 1;
        [KSPField(isPersistant = false)]
        public bool containsPowerGenerator = false;
        [KSPField(isPersistant = false)]
        public double fuelUsePerMJMult = 1f;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float hotBathTemperature = 0;

        [KSPField(isPersistant = false)]
        public float alternatorPowerKW = 0;
        [KSPField(isPersistant = false)]
        public float thermalPropulsionEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float thermalEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float chargedParticleEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float chargedParticlePropulsionEfficiency = 1;

        [KSPField(isPersistant = false)]
        public bool hasBuoyancyEffects = false;
        [KSPField(isPersistant = false)]
        public float geeForceMultiplier = 2;
        [KSPField(isPersistant = false)]
        public float geeForceTreshHold = 1.5f;
        [KSPField(isPersistant = false)]
        public float minGeeForceModifier = 0.01f;
        [KSPField(isPersistant = false)]
        public float neutronEmbrittlementLifepointsMax = 100;
        [KSPField(isPersistant = false)]
        public float neutronEmbrittlementDivider = 1e+9f;
        [KSPField(isPersistant = false)]
        public float hotBathModifier = 1;
        [KSPField(isPersistant = false)]
        public float thermalProcessingModifier = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public int supportedPropellantAtoms = GameConstants.defaultSupportedPropellantAtoms;
        [KSPField(isPersistant = false, guiActive = false)]
        public int supportedPropellantTypes = GameConstants.defaultSupportedPropellantTypes;
        [KSPField(isPersistant = false)]
        public bool fullPowerForNonNeutronAbsorbants = true;
        [KSPField(isPersistant = false)]
        public bool showSpecialisedUI = true;
        [KSPField(isPersistant = false)]
        public bool fastNeutrons = true;
        [KSPField(isPersistant = false)]
        public bool canUseNeutronicFuels = true;

        // Visible imput parameters 
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Bimodel upgrade tech")]
        public string bimodelUpgradeTechReq = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Extra upgrade tech")]
        public string powerUpgradeTechReq = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Extra upgrade Power Multiplier")]
        public float powerUpgradeTechMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Extra upgrade Core temp Mult")]
        public float powerUpgradeCoreTempMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Active Raw Power", guiUnits = " MJ")]
        public double currentRawPowerOutput;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Output (Basic)", guiUnits = " MW")]
        public float PowerOutput = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Output (Upgraded)", guiUnits = " MW")]
        public float upgradedPowerOutput = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Upgrade")]
        public string upgradeTechReq = String.Empty;

        // GUI strings
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Core Temp")]
        public string coretempStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Status")]
        public string statusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Thermal Power")]
        public string currentTPwr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Charged Power")]
        public string currentCPwr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Fuel")]
        public string fuelModeStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Connections Surface")]
        public string connectedRecieversStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Reactor Surface", guiUnits = " m2")]
        public float reactorSurface;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Power to Supply frame")]
        protected double max_power_to_supply = 0;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fixed Max Thermal Power")]
        protected double fixed_maximum_thermal_power;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fixed Max Charged Power")]
        protected double fixed_maximum_charged_power;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        protected double max_thermal_to_supply_fixed;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        protected double max_charged_to_supply_fixed;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max TP To Supply", guiFormat = "F6")]
        protected double max_thermal_to_supply_nominal;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max CP To Supply", guiFormat = "F6")]
        protected double max_charged_to_supply_nominal;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Min throttle")]
        protected double min_throttle;

        // Gui floats
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Part Mass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thermal Power", guiUnits = " MW", guiFormat = "F6")]
        public double maximumThermalPowerEffective = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Gee Force Mod")]
        public double geeForceModifier;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Power Produced", guiUnits = " MW", guiFormat = "F6")]
        public double ongoing_total_power_generated;

        [KSPField(isPersistant = false, guiActive = false)]
        protected double effective_minimum_throtle;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Neutron Power Generated", guiFormat = "F6")]
        protected double ongoing_neutron_power_generated;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Charged Power Generated", guiFormat = "F6")]
        protected double ongoing_charged_power_generated;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thermal Power Requested", guiFormat = "F6")]
        protected double ongoing_thermal_power_requested;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Charged Power Requested", guiFormat = "F6")]
        protected double ongoing_charged_power_requested;

        [KSPField(isPersistant = false, guiActive = false)]
        public bool initialized = false;
        [KSPField(isPersistant = true)]
        public double animationStarted = 0;

        // value types
        protected bool hasrequiredupgrade = false;
        protected int deactivate_timer = 0;
        protected List<ReactorFuelMode> fuel_modes;
        protected ReactorFuelMode current_fuel_mode;
        protected double powerPcnt;
        protected double lithium_consumed_per_second;
        protected double tritium_produced_per_second;
        protected double helium_produced_per_second;
        protected long update_count;
        protected long last_draw_update;
        protected double staticBreedRate;

        [KSPField(isPersistant = false, guiActive = false)]
        protected double raw_charged_power_received = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        protected double raw_thermal_power_received = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public double rawTotalPowerProduced = 0;

        [KSPField(isPersistant = false, guiActive = false)]
        protected double balanced_thermal_power_received = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        protected double balanced_charged_power_received = 0;

        protected GUIStyle bold_style;
        protected GUIStyle text_style;
        
        protected int nrAvailableUpgradeTechs;
        //protected double currentAnimatioRatio;
        protected double total_power_per_frame;
        protected bool decay_ongoing = false;
        protected Rect windowPosition;
        protected int windowID = 90175467;
        protected bool render_window = false;

        protected float previousDeltaTime;
        protected bool? hasBimodelUpgradeTechReq;
        protected List<IEngineNoozle> connectedEngines = new List<IEngineNoozle>();

        protected PartResourceDefinition lithium_def;
        protected  PartResourceDefinition tritium_def; 
        protected PartResourceDefinition helium_def;

        protected PartResource thermalPowerResource = null;
        protected PartResource chargedPowerResource = null;
        protected PartResource wasteheatPowerResource = null;

        // reference types
        protected Dictionary<Guid, float> connectedRecievers = new Dictionary<Guid, float>();
        protected Dictionary<Guid, float> connectedRecieversFraction = new Dictionary<Guid, float>();
        protected float connectedRecieversSum;

        protected double tritium_molar_mass_ratio = 3.0160 / 7.0183;
        protected double helium_molar_mass_ratio = 4.0023 / 7.0183;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Base Wasteheat", guiFormat = "F1")]
        protected double partBaseWasteheat;

        protected double storedIsThermalEnergyGenratorActive;
        protected double storedIsChargedEnergyGenratorActive;
        protected double currentIsThermalEnergyGenratorActive;
        protected double currentIsChargedEnergyGenratorActive;

        protected bool isFixedUpdatedCalled;
        protected AnimationState[] pulseAnimation;
        protected ModuleAnimateGeneric startupAnimation;
        protected ModuleAnimateGeneric shutdownAnimation;
        protected ModuleAnimateGeneric loopingAnimation;

        protected ElectricGeneratorType _firstGeneratorType;

        public List<ReactorProduction> reactorProduction = new List<ReactorProduction>();

        public double ProducedThermalHeat { get{ return ongoing_neutron_power_generated; } }

        private double _requestedThermalHeat;
        public double RequestedThermalHeat 
        { 
            get { return _requestedThermalHeat; } 
            set { _requestedThermalHeat = value; } 
        }

        public double RawTotalPowerProduced
        {
            get { return rawTotalPowerProduced; }
        }

        public double UseProductForPropulsion(double ratio, double consumedAmount)
        {
            if (ratio == 0) return 0;

            var hydrogenDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen);

            double hydrogenMassSum = 0;

            foreach (var product in reactorProduction)
            {
                if (product.mass == 0) continue;

                var effectiveMass = ratio * product.mass;

                // sum product mass
                hydrogenMassSum += effectiveMass;

                // remove product from store
                var fuelAmount = product.fuelmode.DensityInTon > 0 ? (effectiveMass / product.fuelmode.DensityInTon) : 0;
                if (fuelAmount == 0) continue;

                part.RequestResource(product.fuelmode.FuelName, fuelAmount);
            }

            var hydrogenAmount = Math.Min(hydrogenMassSum / hydrogenDefinition.density, consumedAmount);

            // at real time we need twise
            if (!this.vessel.packed)
                hydrogenAmount *= 2;

            return part.RequestResource(hydrogenDefinition.name, -hydrogenAmount);
        }

        public void ConnectWithEngine(IEngineNoozle engine)
        {
            if (!connectedEngines.Contains(engine))
                connectedEngines.Add(engine);
        }

        public void DisconnectWithEngine(IEngineNoozle engine)
        {
            if (connectedEngines.Contains(engine))
                connectedEngines.Remove(engine);
        }

        public GenerationType CurrentGenerationType 
        { 
            get
            {
                return (GenerationType)currentGenerationType;
            }
            private set
            {
                currentGenerationType = (int)value;
            }
        }

        public virtual double MinimumThrottle 
        { 
            get {
                if (CurrentGenerationType == GenerationType.Mk5)
                    return minimumThrottleMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    return minimumThrottleMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    return minimumThrottleMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    return minimumThrottleMk2;
                else
                    return minimumThrottleMk1;
            } 
        }


        public int SupportedPropellantAtoms { get { return supportedPropellantAtoms; } }

        public int SupportedPropellantTypes { get { return supportedPropellantTypes; } }

        public bool FullPowerForNonNeutronAbsorbants { get { return fullPowerForNonNeutronAbsorbants; } }

        public double EfficencyConnectedThermalEnergyGenerator { get { return storedIsThermalEnergyGenratorActive; } }

        public double EfficencyConnectedChargedEnergyGenerator { get { return storedIsChargedEnergyGenratorActive; } }


        public void NotifyActiveThermalEnergyGenerator(double efficency, ElectricGeneratorType generatorType)
        {
            currentIsThermalEnergyGenratorActive = efficency;
            if (_firstGeneratorType == ElectricGeneratorType.unknown)
                _firstGeneratorType = generatorType;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, ElectricGeneratorType generatorType)
        {
            currentIsChargedEnergyGenratorActive = efficency;
            if (_firstGeneratorType == ElectricGeneratorType.unknown)
                _firstGeneratorType = generatorType;
        }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType)
        {
            return generatorType == _firstGeneratorType && storedIsThermalEnergyGenratorActive > 0 && storedIsChargedEnergyGenratorActive > 0; 
        }

        public bool IsThermalSource  {  get { return true; } }

        public float ThermalProcessingModifier { get { return thermalProcessingModifier; } }

        public Part Part { get { return this.part; } }

        public float ChargedParticlePropulsionEfficiency { get { return chargedParticlePropulsionEfficiency; } }

        public double ProducedWasteHeat { get { return ongoing_total_power_generated ; } }

        public void AttachThermalReciever(Guid key, float radius)
        {
            if (!connectedRecievers.ContainsKey(key))
                connectedRecievers.Add(key, radius);
            UpdateConnectedRecieversStr();
        }

        public void DetachThermalReciever(Guid key)
        {
            if (connectedRecievers.ContainsKey(key))
                connectedRecievers.Remove(key);
            UpdateConnectedRecieversStr();
        }

        public float GetFractionThermalReciever(Guid key)
        {
            float result;
            if (connectedRecieversFraction.TryGetValue(key, out result))
                return result;
            else
                return 0;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                // calculate multipliers
                Debug.Log("InterstellarReactor.OnRescale called with " + factor.absolute.linear);
                storedPowerMultiplier = Mathf.Pow(factor.absolute.linear, powerScaleExponent);

                // update power
                DeterminePowerOutput();

                // refresh generators mass
                if (ConnectedThermalElectricGenerator != null)
                    ConnectedThermalElectricGenerator.Refresh();
                if (ConnectedChargedParticleElectricGenerator != null)
                    ConnectedChargedParticleElectricGenerator.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnRescale" + e.Message);
            }
        }

        private void UpdateConnectedRecieversStr()
        {
            if (connectedRecievers == null) return;

            connectedRecieversSum = connectedRecievers.Sum(r => Mathf.Pow(r.Value, 2));
            connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => Mathf.Pow(a.Value, 2) / connectedRecieversSum);

            reactorSurface = Mathf.Pow(radius, 2);
            connectedRecieversStr = connectedRecievers.Count() + " (" + connectedRecieversSum.ToString("0.000") + " m2)";
        }

        public float ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public bool HasBimodelUpgradeTechReq
        {
            get
            {
                if (hasBimodelUpgradeTechReq == null)
                    hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
                return (bool)hasBimodelUpgradeTechReq;
            }
        }

        public float ThermalEnergyEfficiency { get { return HasBimodelUpgradeTechReq ? thermalEnergyEfficiency : 0; } }

        public float ChargedParticleEnergyEfficiency {  get { return chargedParticleEnergyEfficiency; } }

        public bool IsSelfContained { get { return containsPowerGenerator; } }

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public double PowerBufferBonus { get { return this.bonusBufferFactor; } }

        public double RawMaximumPower { get { return RawPowerOutput; } }

        public virtual double FuelEfficiency
        {
            get
            {
                double baseEfficency;
                if (CurrentGenerationType == GenerationType.Mk5)
                    baseEfficency = fuelEfficencyMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    baseEfficency = fuelEfficencyMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    baseEfficency = fuelEfficencyMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    baseEfficency = fuelEfficencyMk2;
                else
                    baseEfficency = fuelEfficencyMk1;

                return baseEfficency * current_fuel_mode.FuelEfficencyMultiplier;
            }
        }

        public int ReactorType { get { return reactorType; } }

        public virtual string TypeName { get { return part.partInfo.title; } }

        public virtual double ChargedPowerRatio 
        { 
            get 
            { 
                return current_fuel_mode != null
                    ? current_fuel_mode.ChargedPowerRatio * ChargedParticleEnergyEfficiency
                    : 0f; 
            } 
        }

        public virtual double CoreTemperature 
        {
            get
            {
                double baseCoreTemperature;
                if (CurrentGenerationType == GenerationType.Mk5)
                    baseCoreTemperature = coreTemperatureMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    baseCoreTemperature = coreTemperatureMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    baseCoreTemperature = coreTemperatureMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    baseCoreTemperature = coreTemperatureMk2;
                else
                    baseCoreTemperature = coreTemperatureMk1;

                var modifiedBaseCoreTemperature = baseCoreTemperature * 
                    (CheatOptions.UnbreakableJoints ? 1 : Math.Max(maxEmbrittlementFraction,  Math.Pow(ReactorEmbrittlemenConditionRatio, 2)));

                return modifiedBaseCoreTemperature;
            }
        }

        public double HotBathTemperature 
        { 
            get
            {
                if (hotBathTemperature == 0)
                    return CoreTemperature * hotBathModifier;
                else
                    return hotBathTemperature;
            }
        }

        public float ThermalPropulsionEfficiency { get { return thermalPropulsionEfficiency; } }

        public virtual double ReactorEmbrittlemenConditionRatio 
        { 
            get { 
                return Math.Min(Math.Max(1 - (neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), maxEmbrittlementFraction), 1); 
            } 
        }

        public virtual double NormalisedMaximumPower
        {
            get
            {
                double normalised_fuel_factor = current_fuel_mode == null ? 1.0f : current_fuel_mode.NormalisedReactionRate;
                var result = RawPowerOutput * normalised_fuel_factor * (CheatOptions.UnbreakableJoints ? 1 : Math.Sin(ReactorEmbrittlemenConditionRatio) * Math.PI * 0.5);
                return result;
            }
        }

        public virtual double MinimumPower { get { return MaximumPower * MinimumThrottle; } }

        public virtual double MaximumThermalPower { get { return NormalisedMaximumPower * (1 - ChargedPowerRatio); } }

        public virtual double MaximumChargedPower { get { return NormalisedMaximumPower * ChargedPowerRatio; } }

        public virtual bool IsNuclear { get { return false; } }

        public virtual bool IsActive { get { return IsEnabled; } }

        public virtual bool IsVolatileSource { get { return false; } }

        public virtual bool IsFuelNeutronRich { get { return false; } }

        public virtual double MaximumPower { get { return MaximumThermalPower + MaximumChargedPower; } }

        public virtual double StableMaximumReactorPower { get { return IsEnabled ? RawPowerOutput : 0; } }

        public IElectricPowerSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerSource ConnectedChargedParticleElectricGenerator { get; set; }

        public double RawPowerOutput
        {
            get
            {
                double rawPowerOutput;
                if (CurrentGenerationType == GenerationType.Mk5)
                    rawPowerOutput = powerOutputMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    rawPowerOutput = powerOutputMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    rawPowerOutput = powerOutputMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    rawPowerOutput = powerOutputMk2;
                else
                    rawPowerOutput = powerOutputMk1;

                return rawPowerOutput * powerOutputMultiplier;
            }
        }

        public int ReactorTechLevel
        {
            get
            {
                return (int)CurrentGenerationType + reactorModeTechBonus;
            }
        }

        public virtual void StartReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                startDisabled = false;
            else
            {
                if (IsNuclear) return;

                IsEnabled = true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Reactor Control Window", active = true, guiActiveUnfocused = true, unfocusedRange = 5f, guiActiveUncommand = true)]
        public void ToggleReactorControlWindow()
        {
            render_window = !render_window;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Activate Reactor", active = false)]
        public void ActivateReactor()
        {
            StartReactor();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Deactivate Reactor", active = true)]
        public void DeactivateReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                startDisabled = true;
            else
            {
                if (IsNuclear) return;

                IsEnabled = false;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Enable Tritium Breeding", active = false)]
        public void StartBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Tritium Breeding", active = true)]
        public void StopBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = false;
        }

        [KSPAction("Activate Reactor")]
        public void ActivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            StartReactor();
        }

        [KSPAction("Deactivate Reactor")]
        public void DeactivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            DeactivateReactor();
        }

        [KSPAction("Toggle Reactor")]
        public void ToggleReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            IsEnabled = !IsEnabled;
        }

        private bool CanPartUpgradeAlternative()
        {
            if (PluginHelper.PartTechUpgrades == null)
            {
                print("[KSP Interstellar] PartTechUpgrades is not initialized");
                return false;
            }

            string upgradetechName;
            if (!PluginHelper.PartTechUpgrades.TryGetValue(part.name, out upgradetechName))
            {
                print("[KSP Interstellar] PartTechUpgrade entry is not found for part '" + part.name + "'");
                return false;
            }

            print("[KSP Interstellar] Found matching Interstellar upgradetech for part '" + part.name + "' with technode " + upgradetechName);

            return PluginHelper.upgradeAvailable(upgradetechName);
        }

        public void DeterminePowerOutput()
        {
            if (HighLogic.LoadedSceneIsEditor || powerOutputMk1 == 0)
            {
                powerOutputMk1 = basePowerOutputMk1 * storedPowerMultiplier;
                powerOutputMk2 = basePowerOutputMk2 * storedPowerMultiplier;
                powerOutputMk3 = basePowerOutputMk3 * storedPowerMultiplier;
                powerOutputMk4 = basePowerOutputMk4 * storedPowerMultiplier;
                powerOutputMk5 = basePowerOutputMk5 * storedPowerMultiplier;
            }

            // if Mk powerOutput is missing, try use lagacy values
            if (powerOutputMk1 == 0)
                powerOutputMk1 = PowerOutput;
            if (powerOutputMk2 == 0)
                powerOutputMk2 = upgradedPowerOutput;
            if (powerOutputMk3 == 0)
                powerOutputMk3 = upgradedPowerOutput * powerUpgradeTechMult;

            // initialise power output when missing
            if (powerOutputMk2 == 0)
                powerOutputMk2 = powerOutputMk1 * 1.5f;
            if (powerOutputMk3 == 0)
                powerOutputMk3 = powerOutputMk2 * 1.5f;
            if (powerOutputMk4 == 0)
                powerOutputMk4 = powerOutputMk3 * 1.5f;
            if (powerOutputMk5 == 0)
                powerOutputMk5 = powerOutputMk4 * 1.5f;

            if (minimumThrottleMk1 == 0)
                minimumThrottleMk1 = minimumThrottle;
            if (minimumThrottleMk2 == 0)
                minimumThrottleMk2 = minimumThrottleMk1;
            if (minimumThrottleMk3 == 0)
                minimumThrottleMk3 = minimumThrottleMk2;
            if (minimumThrottleMk4 == 0)
                minimumThrottleMk4 = minimumThrottleMk3;
        }

        public override void OnStart(PartModule.StartState state)
        {
            UpdateReactorCharacteristics();

            windowPosition = new Rect(windowPositionX, windowPositionY, 300, 100);
            _firstGeneratorType = ElectricGeneratorType.unknown;
            previousDeltaTime = TimeWarp.fixedDeltaTime - 1.0e-6f;
            hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
            staticBreedRate = 1 / powerOutputMultiplier / breedDivider / GameConstants.tritiumBreedRate;

            if (!part.Resources.Contains(FNResourceManager.FNRESOURCE_THERMALPOWER))
            {
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", FNResourceManager.FNRESOURCE_THERMALPOWER);
                node.AddValue("maxAmount", PowerOutput);
                node.AddValue("possibleAmount", 0);
                part.AddResource(node);
                //part.Resources.UpdateList();
                //part.vessel.UpdateVesselResourceSet();
            }

            // while in edit mode, listen to on attach event
            if (state == StartState.Editor)
                part.OnEditorAttach += OnEditorAttach;

            // initialise resource defenitions
            thermalPowerResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_THERMALPOWER);
            chargedPowerResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
            wasteheatPowerResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);

            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_THERMALPOWER, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES };
            this.resources_to_supply = resources_to_supply;

            windowID = new System.Random(part.GetInstanceID()).Next(int.MaxValue);
            base.OnStart(state);

            // configure reactor modes
            fuel_modes = GetReactorFuelModes();
            setDefaultFuelMode();
            UpdateFuelMode();

            if (state == StartState.Editor)
            {
                maximumThermalPowerEffective = MaximumThermalPower;
                coretempStr = CoreTemperature.ToString("0") + " K";
                return;
            }

            if (!reactorInit)
            {
                if (startDisabled)
                {
                    last_active_time = Planetarium.GetUniversalTime() - 4.0 * PluginHelper.SecondsInDay;
                    IsEnabled = false;
                    startDisabled = false;
                    breedtritium = false;
                }
                else
                {
                    IsEnabled = true;
                    breedtritium = true;
                }
                reactorInit = true;
            }

            tritium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdTritium);
            helium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdHelium4);
            lithium_def = fastNeutrons
                ? PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Lithium7)
                : PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Lithium6);

            if (IsEnabled && last_active_time > 0)
                DoPersistentResourceUpdate();

            if (!String.IsNullOrEmpty(animName))
                pulseAnimation = PluginHelper.SetUpAnimation(animName, this.part);
            if (!String.IsNullOrEmpty(loopingAnimationName))
                loopingAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == loopingAnimationName);
            if (!String.IsNullOrEmpty(startupAnimationName))
                startupAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == startupAnimationName );
            if (!String.IsNullOrEmpty(shutdownAnimationName))
                shutdownAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == shutdownAnimationName);

            // only force activate if not with a engine model
            var myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                this.part.force_activate();
                Fields["partMass"].guiActiveEditor = true;
                Fields["radius"].guiActiveEditor = true;
                Fields["connectedRecieversStr"].guiActiveEditor = true;
                Fields["heatTransportationEfficiency"].guiActiveEditor = true;
            }

            Fields["reactorSurface"].guiActiveEditor = showSpecialisedUI;
        }

        //public override void OnStartFinished(PartModule.StartState state)
        //{
        //    // calculate WasteHeat Capacity
        //    partBaseWasteheat = part.mass * 1.0e+5 * wasteHeatMultiplier + (StableMaximumReactorPower * 100);
        //}

        private void UpdateReactorCharacteristics()
        {
            DetermineGenerationType();

            DeterminePowerOutput();

            DetermineFuelEfficency();

            DetermineCoreTemperature();
        }

        private void DetermineCoreTemperature()
        {
            // if coretemperature is missing, first look at lagacy value
            if (coreTemperatureMk1 == 0)
                coreTemperatureMk1 = ReactorTemp;
            if (coreTemperatureMk2 == 0)
                coreTemperatureMk2 = upgradedReactorTemp;
            if (coreTemperatureMk3 == 0)
                coreTemperatureMk3 = upgradedReactorTemp * powerUpgradeCoreTempMult;

            // prevent initial values
            if (coreTemperatureMk1 == 0)
                coreTemperatureMk1 = 2500;
            if (coreTemperatureMk2 == 0)
                coreTemperatureMk2 = coreTemperatureMk1;
            if (coreTemperatureMk3 == 0)
                coreTemperatureMk3 = coreTemperatureMk2;
            if (coreTemperatureMk4 == 0)
                coreTemperatureMk4 = coreTemperatureMk3;
            if (coreTemperatureMk5 == 0)
                coreTemperatureMk5 = coreTemperatureMk4;
        }

        private void DetermineFuelEfficency()
        {
            // if fuel efficency is missing, try to use lagacy value
            if (fuelEfficencyMk1 == 0)
                fuelEfficencyMk1 = fuelEfficiency;
            if (fuelEfficencyMk2 == 0)
                fuelEfficencyMk2 = upgradedFuelEfficiency;

            // prevent any initial values
            if (fuelEfficencyMk1 == 0)
                fuelEfficencyMk1 = 1;
            if (fuelEfficencyMk2 == 0)
                fuelEfficencyMk2 = fuelEfficencyMk1;
            if (fuelEfficencyMk3 == 0)
                fuelEfficencyMk3 = fuelEfficencyMk2;
            if (fuelEfficencyMk4 == 0)
                fuelEfficencyMk4 = fuelEfficencyMk3;
            if (fuelEfficencyMk5 == 0)
                fuelEfficencyMk5 = fuelEfficencyMk4;
        }

        private void DetermineGenerationType()
        {
            // initialse tech requirment if missing 
            if (string.IsNullOrEmpty(upgradeTechReqMk2))
                upgradeTechReqMk2 = upgradeTechReq;
            if (string.IsNullOrEmpty(upgradeTechReqMk3))
                upgradeTechReqMk3 = powerUpgradeTechReq;

            // determine number of upgrade techs
            nrAvailableUpgradeTechs = 1;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk5))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk4))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk2))
                nrAvailableUpgradeTechs++;

            // show poweroutput when appropriate
            if (nrAvailableUpgradeTechs >= 5)
                Fields["powerOutputMk5"].guiActiveEditor = true;
            if (nrAvailableUpgradeTechs >= 4)
                Fields["powerOutputMk4"].guiActiveEditor = true;
            if (nrAvailableUpgradeTechs >= 3)
                Fields["powerOutputMk3"].guiActiveEditor = true;
            if (nrAvailableUpgradeTechs >= 2)
                Fields["powerOutputMk2"].guiActiveEditor = true;
            if (nrAvailableUpgradeTechs >= 1)
                Fields["powerOutputMk1"].guiActiveEditor = true;

            // determine fusion tech levels
            if (nrAvailableUpgradeTechs == 5)
                CurrentGenerationType = GenerationType.Mk5;
            else if (nrAvailableUpgradeTechs == 4)
                CurrentGenerationType = GenerationType.Mk4;
            else if (nrAvailableUpgradeTechs == 3)
                CurrentGenerationType = GenerationType.Mk3;
            else if (nrAvailableUpgradeTechs == 2)
                CurrentGenerationType = GenerationType.Mk2;
            else
                CurrentGenerationType = GenerationType.Mk1;
        }

        /// <summary>
        /// Event handler called when part is attached to another part
        /// </summary>
        private void OnEditorAttach()
        {
            foreach (var node in part.attachNodes)
            {
                if (node.attachedPart == null) continue;

                var generator = node.attachedPart.FindModuleImplementing<FNGenerator>();
                if (generator != null)
                    generator.FindAndAttachToThermalSource();
            }
        }

        public virtual void Update()
        {
            currentRawPowerOutput = RawPowerOutput;

            Events["DeactivateReactor"].guiActive = HighLogic.LoadedSceneIsFlight && showShutDownInFlight && IsEnabled;

            if (HighLogic.LoadedSceneIsEditor)
            {
                reactorSurface = Mathf.Pow(radius, 2);
            }
        }

        protected void UpdateFuelMode()
        {
            fuelModeStr = current_fuel_mode != null ? current_fuel_mode.ModeGUIName : "null";
        }

        public override void OnUpdate()
        {
            Events["StartBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && !breedtritium && IsFuelNeutronRich && IsEnabled;
            Events["StopBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && breedtritium && IsFuelNeutronRich && IsEnabled;
            UpdateFuelMode();

            coretempStr = CoreTemperature.ToString("0") + " K";
            if (update_count - last_draw_update > 10)
            {
                if (IsEnabled)
                {
                    if (CheatOptions.InfinitePropellant || (current_fuel_mode != null && !current_fuel_mode.ReactorFuels.Any(fuel => GetFuelAvailability(fuel) <= 0)))
                    {
                        currentTPwr = PluginHelper.getFormattedPowerString(ongoing_neutron_power_generated) + "_th";
                        currentCPwr = PluginHelper.getFormattedPowerString(ongoing_charged_power_generated) + "_cp";
                        statusStr = "Active (" + powerPcnt.ToString("0.00") + "%)";
                    }
                    else if (current_fuel_mode != null)
                    {
                        statusStr = current_fuel_mode.ReactorFuels.FirstOrDefault(fuel => GetFuelAvailability(fuel) <= 0).FuelName + " Deprived";
                    }
                }
                else
                {
                    if (powerPcnt > 0)
                        statusStr = "Decay Heating (" + powerPcnt.ToString("0.00") + "%)";
                    else
                        statusStr = "Offline";
                }

                last_draw_update = update_count;
            }
            //if (!vessel.isActiveVessel || part == null) RenderingManager.RemoveFromPostDrawQueue(0, OnGUI);
            update_count++;
            partMass = part.mass;
        }

        /// <summary>
        /// FixedUpdate is also called when not activated
        /// </summary>
        public void FixedUpdate() 
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                DeterminePowerOutput();
                maximumThermalPowerEffective = MaximumThermalPower;
                return;
            }

            if (!isFixedUpdatedCalled)
            {
                isFixedUpdatedCalled = true;
                UpdateCapacities(stored_fuel_ratio);
            }

            base.OnFixedUpdate();

            // add alternator power
            if (IsEnabled && alternatorPowerKW != 0)
            {
                part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, -alternatorPowerKW * TimeWarp.fixedDeltaTime);
                //part.temperature = part.temperature + (TimeWarp.fixedDeltaTime * alternatorPowerKW / 1000.0 / part.mass);
            }
        }

        //protected virtual void PowerMaintenance(double powerRatio)
        //{
        //    // override by children
        //}

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            storedIsThermalEnergyGenratorActive = currentIsThermalEnergyGenratorActive;
            storedIsChargedEnergyGenratorActive = currentIsChargedEnergyGenratorActive;
            currentIsThermalEnergyGenratorActive = 0;
            currentIsChargedEnergyGenratorActive = 0;

            decay_ongoing = false;

            if (IsEnabled && MaximumPower > 0)
            {
                if (ReactorIsOverheating())
                {
                    if (FlightGlobals.ActiveVessel == vessel)
                        ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency reactor shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);

                    IsEnabled = false;
                    return;
                }

                // Max Power
                var engineThrottleModifier = disableAtZeroThrottle && connectedEngines.Any() && connectedEngines.All(e => e.CurrentThrottle == 0) ? 0 : 1;
                max_power_to_supply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime, 0);

                geeForceModifier = !CheatOptions.UnbreakableJoints && hasBuoyancyEffects 
                    ? Math.Min(Math.Max(1 - ((part.vessel.geeForce - geeForceTreshHold) * geeForceMultiplier), minGeeForceModifier), 1) 
                    : 1;

                stored_fuel_ratio = CheatOptions.InfinitePropellant 
                    ? 1 
                    : Math.Min(current_fuel_mode.ReactorFuels.Min(fuel => GetFuelRatio(fuel, FuelEfficiency, max_power_to_supply * geeForceModifier)), 1);

                UpdateCapacities(stored_fuel_ratio);

                min_throttle = stored_fuel_ratio > 0 ? MinimumThrottle / stored_fuel_ratio : 1;
                effective_minimum_throtle = connectedEngines.Any()
                    ? Math.Max(connectedEngines.Max(e => e.CurrentThrottle), min_throttle)
                    : min_throttle;

                if (RequestedThermalHeat > 0)
                {
                    var requested_ratio = Math.Min(Math.Max((RequestedThermalHeat / MaximumThermalPower), 0), 1);
                    effective_minimum_throtle = Math.Max(effective_minimum_throtle, requested_ratio);
                }

                ongoing_charged_power_requested = GetRequiredResourceDemand(FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                ongoing_thermal_power_requested = GetRequiredResourceDemand(FNResourceManager.FNRESOURCE_THERMALPOWER);

                // Charged Power
                fixed_maximum_charged_power = MaximumChargedPower * TimeWarp.fixedDeltaTime;
                max_charged_to_supply_fixed = Math.Max(fixed_maximum_charged_power, 0) * stored_fuel_ratio * geeForceModifier * engineThrottleModifier;
                max_charged_to_supply_nominal = max_charged_to_supply_fixed / TimeWarp.fixedDeltaTime;

                raw_charged_power_received = supplyManagedFNResourceWithMinimumRatio(max_charged_to_supply_fixed, effective_minimum_throtle, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                double charged_power_ratio = max_charged_to_supply_fixed > 0 ? raw_charged_power_received / max_charged_to_supply_fixed : 0;

                // Thermal Power
                fixed_maximum_thermal_power = MaximumThermalPower * TimeWarp.fixedDeltaTime;
                max_thermal_to_supply_fixed = Math.Max(fixed_maximum_thermal_power, 0) * stored_fuel_ratio * geeForceModifier * engineThrottleModifier;
                max_thermal_to_supply_nominal = max_thermal_to_supply_fixed / TimeWarp.fixedDeltaTime;
                raw_thermal_power_received = supplyManagedFNResourceWithMinimumRatio(max_thermal_to_supply_fixed, effective_minimum_throtle, FNResourceManager.FNRESOURCE_THERMALPOWER);

                rawTotalPowerProduced = raw_thermal_power_received + raw_charged_power_received;

                // add additional power
                double thermal_power_ratio = max_thermal_to_supply_fixed > 0 && (1 - ChargedPowerRatio) > 0 ? raw_thermal_power_received / max_thermal_to_supply_fixed : 0;

                reactor_power_ratio = Math.Max(charged_power_ratio, thermal_power_ratio);

                var thermal_shortage_ratio = charged_power_ratio > thermal_power_ratio ? charged_power_ratio - thermal_power_ratio : 0;
                var chargedpower_shortagage_ratio = thermal_power_ratio > charged_power_ratio ? thermal_power_ratio - charged_power_ratio : 0;

                // fix any inbalance in power draw
                
                balanced_thermal_power_received = raw_thermal_power_received + (thermal_shortage_ratio * fixed_maximum_thermal_power * stored_fuel_ratio * geeForceModifier * engineThrottleModifier);
                balanced_charged_power_received = raw_charged_power_received + (chargedpower_shortagage_ratio * fixed_maximum_charged_power * stored_fuel_ratio * geeForceModifier * engineThrottleModifier);

                // update GUI
                ongoing_neutron_power_generated = balanced_thermal_power_received / TimeWarp.fixedDeltaTime;
                ongoing_charged_power_generated = balanced_charged_power_received / TimeWarp.fixedDeltaTime;

                // Total
                double total_power_received = balanced_thermal_power_received + balanced_charged_power_received;

                if (!CheatOptions.UnbreakableJoints)
                    neutronEmbrittlementDamage += total_power_received * current_fuel_mode.NeutronsRatio / neutronEmbrittlementDivider;

                ongoing_total_power_generated = total_power_received / TimeWarp.fixedDeltaTime;

                total_power_per_frame = total_power_received;
                ongoing_consumption_rate = total_power_received / MaximumPower / TimeWarp.fixedDeltaTime;

                
                PluginHelper.SetAnimationRatio((float)Math.Pow(ongoing_consumption_rate, 4), pulseAnimation);

                powerPcnt = 100 * ongoing_consumption_rate;

                // consume fuel
                if (!CheatOptions.InfinitePropellant)
                {
                    foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels)
                    {
                        ConsumeReactorFuel(fuel, total_power_received / geeForceModifier);
                    }

                    // refresh production list
                    reactorProduction.Clear();

                    // produce reactor products
                    foreach (ReactorProduct product in current_fuel_mode.ReactorProducts)
                    {
                        var massProduced = ProduceReactorProduct(product, total_power_received / geeForceModifier);

                        reactorProduction.Add(new ReactorProduction() { fuelmode = product, mass = massProduced });
                    }
                }

                // Waste Heat
                //if (!CheatOptions.IgnoreMaxTemperature)
                //    supplyFNResource(total_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated

                BreedTritium(ongoing_neutron_power_generated, TimeWarp.fixedDeltaTime);

                if (Planetarium.GetUniversalTime() != 0)
                    last_active_time = Planetarium.GetUniversalTime();
            }
            else if (IsEnabled && IsNuclear && MaximumPower > 0 && (Planetarium.GetUniversalTime() - last_active_time <= 3 * PluginHelper.SecondsInDay))
            {

                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                double power_fraction = 0.1 * Math.Exp(-(Planetarium.GetUniversalTime() - last_active_time) / PluginHelper.SecondsInDay / 24.0 * 9.0);
                double power_to_supply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime * power_fraction, 0);
                raw_thermal_power_received = supplyManagedFNResourceWithMinimumRatio(power_to_supply, 1, FNResourceManager.FNRESOURCE_THERMALPOWER);

                rawTotalPowerProduced = raw_thermal_power_received;

                ongoing_neutron_power_generated = raw_thermal_power_received / TimeWarp.fixedDeltaTime;
                BreedTritium(ongoing_neutron_power_generated, TimeWarp.fixedDeltaTime);

                ongoing_consumption_rate = MaximumPower > 0 ? raw_thermal_power_received / MaximumPower / TimeWarp.fixedDeltaTime : 0;

                //if (!CheatOptions.IgnoreMaxTemperature)
                //    supplyFNResource(raw_thermal_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated

                powerPcnt = 100 * ongoing_consumption_rate;
                decay_ongoing = true;

            }
            else
            {
                rawTotalPowerProduced = 0;
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                powerPcnt = 0;
            }

            if (!IsEnabled)
            {
                if (thermalPowerResource != null)
                {
                    thermalPowerResource.maxAmount = 0.0001;
                    thermalPowerResource.amount = 0;
                }

                if (chargedPowerResource != null)
                {
                    chargedPowerResource.maxAmount = 0.0001;
                    chargedPowerResource.amount = 0;
                }
            }

        }


        private void UpdateCapacities(double fuel_ratio)
        {
            // calculate thermalpower capacity
            if (TimeWarp.fixedDeltaTime != previousDeltaTime)
            {
                if (thermalPowerResource != null)
                {
                    //var stableThemalPowerBuffer = 10 * MaximumThermalPower;
                    var requiredThermalCapacity = Math.Max(0.0001, 10 * MaximumThermalPower * TimeWarp.fixedDeltaTime);
                    var previousThermalCapacity = Math.Max(0.0001, 10 * MaximumThermalPower * previousDeltaTime);
                    var thermalPowerRatio = thermalPowerResource.amount / thermalPowerResource.maxAmount;

                    thermalPowerResource.maxAmount = requiredThermalCapacity;

                    if (reactorBooted)
                    {
                        // adjust to
                        thermalPowerResource.amount = requiredThermalCapacity > previousThermalCapacity
                                ? Math.Max(0, Math.Min(requiredThermalCapacity, thermalPowerResource.amount + requiredThermalCapacity - previousThermalCapacity))
                                : Math.Max(0, Math.Min(requiredThermalCapacity, thermalPowerRatio * requiredThermalCapacity));
                    }
                    else
                    {
                        // to prevent starting up with wasteheat, boot with full power at bootup
                        thermalPowerResource.amount = thermalPowerResource.maxAmount * fuel_ratio;
                        reactorBooted = true;
                    }
                }

                if (chargedPowerResource != null)
                {
                    var stableChargedPower = 10 * MaximumChargedPower;
                    var requiredChargedCapacity = Math.Max(0.0001, 10 * MaximumChargedPower * TimeWarp.fixedDeltaTime);
                    var previousChargedCapacity = Math.Max(0.0001, 10 * MaximumChargedPower * previousDeltaTime);
                    var chargedPowerRatio = thermalPowerResource.amount / thermalPowerResource.maxAmount;

                    chargedPowerResource.maxAmount = requiredChargedCapacity;
                    chargedPowerResource.amount = requiredChargedCapacity > previousChargedCapacity
                        ? Math.Max(0, Math.Min(requiredChargedCapacity, chargedPowerResource.amount + requiredChargedCapacity - previousChargedCapacity))
                        : Math.Max(0, Math.Min(requiredChargedCapacity, chargedPowerRatio * requiredChargedCapacity));
                }

                if (wasteheatPowerResource != null)
                {
                    // calculate WasteHeat Capacity
                    partBaseWasteheat = part.mass * 1.0e+5 * wasteHeatMultiplier + (StableMaximumReactorPower * 100);

                    var requiredWasteheatCapacity = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * partBaseWasteheat);
                    var previousWasteheatCapacity = Math.Max(0.0001, 10 * previousDeltaTime * partBaseWasteheat);

                    var wasteHeatRatio = Math.Max(0, Math.Min(1, wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount));
                    wasteheatPowerResource.maxAmount = requiredWasteheatCapacity;
                    wasteheatPowerResource.amount = requiredWasteheatCapacity * wasteHeatRatio;
                }
            }
            else
            {
                if (thermalPowerResource != null)
                {
                    //var stableThemalPowerBuffer = RawPowerOutput * (1 - ChargedPowerRatio);
                    thermalPowerResource.maxAmount = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * MaximumThermalPower);
                    thermalPowerResource.amount = Math.Min(thermalPowerResource.amount, thermalPowerResource.maxAmount);
                }

                if (chargedPowerResource != null)
                {
                    //var stableChargedPower = RawPowerOutput * ChargedPowerRatio;
                    chargedPowerResource.maxAmount = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * MaximumChargedPower);
                    chargedPowerResource.amount = Math.Min(chargedPowerResource.amount, chargedPowerResource.maxAmount);
                }

                if (wasteheatPowerResource != null)
                {
                    var wasteHeatRatio = Math.Max(0, Math.Min(1, wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount));
                    var requiredWasteheatCapacity = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * partBaseWasteheat);
                    wasteheatPowerResource.maxAmount = requiredWasteheatCapacity;
                    wasteheatPowerResource.amount = requiredWasteheatCapacity * wasteHeatRatio;
                }
            }

            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        protected double GetFuelRatio(ReactorFuel reactorFuel, double fuelEfficency, double megajoules)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            var fuelUseForPower = reactorFuel.GetFuelUseForPower(fuelEfficency, megajoules, fuelUsePerMJMult);

            return GetFuelAvailability(reactorFuel) / fuelUseForPower;
        }

        private void BreedTritium(double neutron_power_received_each_second, double fixedDeltaTime)
        {
            if (!breedtritium || neutron_power_received_each_second <= 0 || fixedDeltaTime <= 0)
            {
                tritium_produced_per_second = 0;
                helium_produced_per_second = 0;
                return;
            }

            // calculate current maximum litlium consumption
            var breed_rate = current_fuel_mode.TritiumBreedModifier * staticBreedRate * neutron_power_received_each_second * fixedDeltaTime;
            var lith_rate = breed_rate / lithium_def.density;

            // get spare room tritium
            double amount;
            double maxAmount;
            part.GetConnectedResourceTotals(tritium_def.id, out amount, out maxAmount);
            var spareRoomTritiumAmount = maxAmount - amount; 

            // limit lithium consumption to maximum tritium storage
            var maximumTritiumProduction = lith_rate * tritium_molar_mass_ratio * lithium_def.density / tritium_def.density;
            var maximumLitiumConsumtionRatio = maximumTritiumProduction > 0 ?  Math.Min(maximumTritiumProduction, spareRoomTritiumAmount) / maximumTritiumProduction : 0;
            var lithium_request = lith_rate * maximumLitiumConsumtionRatio;

            // consume the lithium
            var lith_used = CheatOptions.InfinitePropellant 
                ? lithium_request 
                : fastNeutrons 
                    ? part.RequestResource(InterstellarResourcesConfiguration.Instance.Lithium7, lithium_request, ResourceFlowMode.STACK_PRIORITY_SEARCH)
                    : part.RequestResource(InterstellarResourcesConfiguration.Instance.Lithium6, lithium_request, ResourceFlowMode.STACK_PRIORITY_SEARCH);

            lithium_consumed_per_second = lith_used / fixedDeltaTime;

            // caculate products
            var tritium_production = lith_used * tritium_molar_mass_ratio * lithium_def.density / tritium_def.density;
            var helium_production = lith_used * helium_molar_mass_ratio * lithium_def.density / helium_def.density;

            // produce tritium and helium
            tritium_produced_per_second = CheatOptions.InfinitePropellant 
                ? tritium_production / fixedDeltaTime
                : -part.RequestResource(tritium_def.name, -tritium_production) / fixedDeltaTime;

            helium_produced_per_second = CheatOptions.InfinitePropellant  
                ? helium_production / fixedDeltaTime
                : -part.RequestResource(helium_def.name, -helium_production) / fixedDeltaTime;
        }

        public virtual double GetCoreTempAtRadiatorTemp(double rad_temp)
        {
            return CoreTemperature;
        }

        public virtual double GetThermalPowerAtTemp(double temp)
        {
            return MaximumPower;
        }

        public float GetRadius()
        {
            return radius;
        }

        public virtual bool shouldScaleDownJetISP()
        {
            return false;
        }

        public void EnableIfPossible()
        {
            if (!IsNuclear && !IsEnabled)
                IsEnabled = true;
        }

        public bool isVolatileSource()
        {
            return false;
        }

        public override string GetInfo()
        {
            UpdateReactorCharacteristics();

            ConfigNode[] fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");
            List<ReactorFuelMode> basic_fuelmodes = fuelmodes.Select(node => new ReactorFuelMode(node)).Where(fm => (fm.SupportedReactorTypes & reactorType) == reactorType).ToList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("REACTOR INFO");
            sb.AppendLine(originalName);
            sb.AppendLine("Thermal Power: " + PluginHelper.getFormattedPowerString(powerOutputMk1));
            sb.AppendLine("Core Temperature: " + coreTemperatureMk1.ToString("0") + "K");
            sb.AppendLine("Fuel Burnup: " + (fuelEfficencyMk1 * 100.0).ToString("0.00") + "%");
            sb.AppendLine("FUEL MODES");
            basic_fuelmodes.ForEach(fm =>
            {
                sb.AppendLine("---");
                sb.AppendLine(fm.ModeGUIName);
                sb.AppendLine("Power Multiplier: " + fm.NormalisedReactionRate.ToString("0.00"));
                sb.AppendLine("Charged Particle Ratio: " + fm.ChargedPowerRatio.ToString("0.00"));
                sb.AppendLine("Total Energy Density: " + fm.ReactorFuels.Sum(fuel => fuel.EnergyDensity).ToString("0.00") + " MJ/kg");
                foreach (ReactorFuel fuel in fm.ReactorFuels)
                {
                    sb.AppendLine(fuel.FuelName + " " + fuel.AmountFuelUsePerMJ * fuelUsePerMJMult * PowerOutput * fm.NormalisedReactionRate * PluginHelper.SecondsInDay / fuelEfficiency + fuel.Unit + "/day");
                }
                sb.AppendLine("---");
            });

            return sb.ToString();
        }

        protected void DoPersistentResourceUpdate()
        {
            if (CheatOptions.InfinitePropellant)
                return;

            // calculate delta time since last processing
            double delta_time_diff = Math.Max(Planetarium.GetUniversalTime() - last_active_time, 0);

            // consume fuel
            foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels)
            {
                ConsumeReactorFuel(fuel, delta_time_diff * ongoing_total_power_generated);
            }

            // produce reactor products
            foreach (ReactorProduct product in current_fuel_mode.ReactorProducts)
            {
                var massProduced = ProduceReactorProduct(product, delta_time_diff * ongoing_total_power_generated);
            }

            // breed tritium
            BreedTritium(ongoing_neutron_power_generated, delta_time_diff);
        }

        protected bool ReactorIsOverheating()
        {
            if (!CheatOptions.IgnoreMaxTemperature && getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= emergencyPowerShutdownFraction && canShutdown)
            {
                deactivate_timer++;
                if (deactivate_timer > 3)
                    return true;
            }
            else
                deactivate_timer = 0;

            return false;
        }

        protected List<ReactorFuelMode> GetReactorFuelModes()
        {
            ConfigNode[] fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");
			return fuelmodes.Select(node => new ReactorFuelMode(node))
                .Where(fm =>
                    (fm.SupportedReactorTypes & ReactorType) == ReactorType
                    && PluginHelper.HasTechRequirementOrEmpty(fm.TechRequirement)
                    && ReactorTechLevel >= fm.TechLevel
                    && (fm.Aneutronic || canUseNeutronicFuels)
                    ).ToList();
        }

        protected bool FuelRequiresLab(bool requiresLab)
        {
            bool isConnectedToLab = part.IsConnectedToModule("ScienceModule", 10);

            return !requiresLab || isConnectedToLab && canBeCombinedWithLab;
        }

        protected virtual void setDefaultFuelMode()
        {
            current_fuel_mode = fuel_modes.FirstOrDefault();

            if (current_fuel_mode == null)
                print("[KSP Interstellar] Warning : current_fuel_mode is null");
            else
                print("[KSP Interstellar] current_fuel_mode = " + current_fuel_mode.ModeGUIName);
        }

        protected double ConsumeReactorFuel(ReactorFuel fuel, double MJpower)
        {
            var consume_amount_in_unit_of_storage = MJpower * fuel.AmountFuelUsePerMJ * fuelUsePerMJMult / FuelEfficiency;
            
            if (!fuel.ConsumeGlobal)
            {
                if (part.Resources.Contains(fuel.FuelName))
                {
                    double amount = Math.Min(consume_amount_in_unit_of_storage, part.Resources[fuel.FuelName].amount);
                    part.Resources[fuel.FuelName].amount -= amount;
                    return amount;
                }
                else
                    return 0;
            }
            return part.RequestResource(fuel.FuelName, consume_amount_in_unit_of_storage);
        }

        protected virtual double ProduceReactorProduct(ReactorProduct product, double MJpower)
        {
            var product_supply = MJpower * product.AmountProductUsePerMJ * fuelUsePerMJMult / FuelEfficiency;

            //var effectiveAmount = produce_amount / FuelEfficiency;
            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.FuelName))
                {
                    double availableStorage = part.Resources[product.FuelName].maxAmount - part.Resources[product.FuelName].amount;
                    double possibleAmount = Math.Min(product_supply, availableStorage);
                    part.Resources[product.FuelName].amount += possibleAmount;
                    return product_supply * product.DensityInTon;
                }
                else
                    return 0;
            }

            part.RequestResource(product.FuelName, -product_supply);
            return product_supply * product.DensityInTon;
        }

        protected double GetFuelAvailability(ReactorFuel fuel)
        {
            if (fuel == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability fuel null");

            if (!fuel.ConsumeGlobal)
            {
                if (part.Resources.Contains(fuel.FuelName))
                    return part.Resources[fuel.FuelName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                //return part.GetConnectedResources(fuel.FuelName).Sum(rs => rs.amount);
                double amount;
                double maxAmount;
                var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(fuel.FuelName);
                part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);
                return amount;
            }
            else
                return part.FindAmountOfAvailableFuel(fuel.FuelName, 4);
        }

        protected double GetFuelAvailability(ReactorProduct product)
        {
            if (product == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability product null");

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.FuelName))
                    return part.Resources[product.FuelName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                //return part.GetConnectedResources(product.FuelName).Sum(rs => rs.amount);
                double amount;
                double maxAmount;
                var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(product.FuelName);
                part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);
                return amount;
            }
            else
                return part.FindAmountOfAvailableFuel(product.FuelName, 4);
        }

        protected double GetMaxFuelAvailability(ReactorProduct product)
        {
            if (product == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability product null");

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.FuelName))
                    return part.Resources[product.FuelName].maxAmount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                //return part.GetConnectedResources(product.FuelName).Sum(rs => rs.maxAmount);
                double amount;
                double maxAmount;
                var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(product.FuelName);
                part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);
                return maxAmount;
            }
            else
                return part.FindMaxAmountOfAvailableFuel(product.FuelName, 4);
        }

        public void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, "Reactor System Interface");
        }

        protected void PrintToGUILayout(string label, string value, GUIStyle bold_style, GUIStyle text_style,  int witdhLabel = 150, int witdhValue = 200)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, bold_style, GUILayout.Width(witdhLabel));
            GUILayout.Label(value, text_style, GUILayout.Width(witdhValue));
            GUILayout.EndHorizontal();
        }

        protected virtual void WindowReactorSpecificOverride()  {}

        private void Window(int windowID)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                bold_style = new GUIStyle(GUI.skin.label);
                bold_style.fontStyle = FontStyle.Bold;

                text_style = new GUIStyle(GUI.skin.label);
                text_style.fontStyle = FontStyle.Normal;

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    render_window = false;

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(TypeName, bold_style, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                PrintToGUILayout("Reactor Embrittlement", (100 * (1 - ReactorEmbrittlemenConditionRatio)).ToString("0.000") + "%", bold_style, text_style);
                PrintToGUILayout("Radius", radius.ToString() + "m", bold_style, text_style);
                PrintToGUILayout("Core Temperature", coretempStr, bold_style, text_style);
                PrintToGUILayout("Status", statusStr, bold_style, text_style);
                PrintToGUILayout("Fuel Mode", fuelModeStr, bold_style, text_style);

                WindowReactorSpecificOverride();

                PrintToGUILayout("Max Power Output", PluginHelper.getFormattedPowerString(NormalisedMaximumPower, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(RawPowerOutput, "0.0", "0.000"), bold_style, text_style);

                if (ChargedPowerRatio < 1.0)
                    PrintToGUILayout("Thermal Power", PluginHelper.getFormattedPowerString(ongoing_neutron_power_generated, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(MaximumThermalPower, "0.0", "0.000"), bold_style, text_style);
                if (ChargedPowerRatio > 0)
                    PrintToGUILayout("Charged Power", PluginHelper.getFormattedPowerString(ongoing_charged_power_generated, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(MaximumChargedPower, "0.0", "0.000"), bold_style, text_style);

                if (current_fuel_mode != null & current_fuel_mode.ReactorFuels != null)
                {
                    if (IsFuelNeutronRich && breedtritium && canBreedTritium)
                    {
                        double totalLithiumAmount;
                        double totalLithiumMaxAmount;
                        part.GetConnectedResourceTotals(lithium_def.id, out totalLithiumAmount, out totalLithiumMaxAmount);

                        double totalTritiumAmount;
                        double totalTritiumMaxAmount;
                        part.GetConnectedResourceTotals(tritium_def.id, out totalTritiumAmount, out totalTritiumMaxAmount);

                        double totalHeliumAmount;
                        double totalHeliumMaxAmount;
                        part.GetConnectedResourceTotals(helium_def.id, out totalHeliumAmount, out totalHeliumMaxAmount);

                        var MassHeliumAmount = totalHeliumAmount * helium_def.density * 1000;
                        var MassHeliumMaxAmount = totalHeliumMaxAmount * helium_def.density * 1000;

                        var MassTritiumAmount = totalTritiumAmount * tritium_def.density * 1000;
                        var MassTritiumMaxAmount = totalTritiumMaxAmount * tritium_def.density * 1000;

                        var tritium_kg_day = tritium_produced_per_second * tritium_def.density * 1000 * PluginHelper.SecondsInDay;

                        PrintToGUILayout("Tritium Breed Rate", 100 * current_fuel_mode.NeutronsRatio + "% " + tritium_kg_day.ToString("0.000000") + " kg/day ", bold_style, text_style);
                        PrintToGUILayout("Lithium Reserves", totalLithiumAmount.ToString("0.00000") + " L / " + totalLithiumMaxAmount.ToString("0.00000") + " L", bold_style, text_style);

                        var lithium_consumption_day = lithium_consumed_per_second * PluginHelper.SecondsInDay;
                        PrintToGUILayout("Lithium Consumption", lithium_consumption_day.ToString("0.00000") + " L/day", bold_style, text_style);
                        var lithium_lifetime_total_days = lithium_consumption_day > 0 ? totalLithiumAmount / lithium_consumption_day : 0;

                        int lithium_lifetime_years = (int)Math.Floor(lithium_lifetime_total_days / GameConstants.KERBIN_YEAR_IN_DAYS);
                        var lithium_lifetime_years_remainder_in_days = lithium_lifetime_total_days % GameConstants.KERBIN_YEAR_IN_DAYS;

                        int lithium_lifetime_remaining_days = (int)Math.Floor(lithium_lifetime_years_remainder_in_days);
                        var lithium_lifetime_remaining_days_remainer = lithium_lifetime_years_remainder_in_days % 1;

                        var lithium_lifetime_remaining_hours = lithium_lifetime_remaining_days_remainer * PluginHelper.SecondsInDay / GameConstants.HOUR_SECONDS;

                        PrintToGUILayout("Lithium Remaining", lithium_lifetime_years + " years " + lithium_lifetime_remaining_days + " days " + lithium_lifetime_remaining_hours.ToString("0.0000") + " hours", bold_style, text_style);

                        PrintToGUILayout("Tritium Storage", MassTritiumAmount.ToString("0.000000") + " kg / " + MassTritiumMaxAmount.ToString("0.000000") + " kg", bold_style, text_style);
                        PrintToGUILayout("Helium Storage", MassHeliumAmount.ToString("0.000000") + " kg / " + MassHeliumMaxAmount.ToString("0.000000") + " kg", bold_style, text_style);
                    }
                    else
                        PrintToGUILayout("Is Neutron rich", IsFuelNeutronRich.ToString(), bold_style, text_style);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Fuel", bold_style, GUILayout.Width(150));
                    GUILayout.EndHorizontal();

                    //double fuel_lifetime_d = double.MaxValue;
                    foreach (var fuel in current_fuel_mode.ReactorFuels)
                    {
                        double availabilityInKg = GetFuelAvailability(fuel) * fuel.DensityInKg;

                        PrintToGUILayout(fuel.FuelName + " Reserves", availabilityInKg.ToString("0.000000") + " kg", bold_style, text_style);
                        double kg_fuel_use_per_day = 1000 * total_power_per_frame * fuel.TonsFuelUsePerMJ * fuelUsePerMJMult / TimeWarp.fixedDeltaTime / FuelEfficiency * current_fuel_mode.NormalisedReactionRate * PluginHelper.SecondsInDay;

                        double fuel_lifetime_d = kg_fuel_use_per_day > 0 ? availabilityInKg / kg_fuel_use_per_day : 0;

                        int lifetime_years = (int)Math.Floor(fuel_lifetime_d / GameConstants.KERBIN_YEAR_IN_DAYS);
                        double lifetime_years_day_remainder = fuel_lifetime_d % GameConstants.KERBIN_YEAR_IN_DAYS;

                        PrintToGUILayout(fuel.FuelName + " Consumption ", PluginHelper.getFormatedMassString(kg_fuel_use_per_day, "0.000000") + "/day", bold_style, text_style);

                        if (lifetime_years > 0)
                            PrintToGUILayout(fuel.FuelName + " Lifetime", (double.IsNaN(lifetime_years) ? "-" : lifetime_years + " years " + (lifetime_years_day_remainder).ToString("0.00")) + " days", bold_style, text_style);
                        else
                            PrintToGUILayout(fuel.FuelName + " Lifetime", (double.IsNaN(fuel_lifetime_d) ? "-" : (fuel_lifetime_d).ToString("0.00")) + " days", bold_style, text_style);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Products", bold_style, GUILayout.Width(150));
                    GUILayout.EndHorizontal();

                    foreach (var product in current_fuel_mode.ReactorProducts)
                    {
                        double availabilityInKg = GetFuelAvailability(product) * product.DensityInKg;
                        double maxAvailabilityInKg = GetMaxFuelAvailability(product) * product.DensityInKg;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(product.FuelName + " Storage", bold_style, GUILayout.Width(150));
                        GUILayout.Label((availabilityInKg).ToString("0.0000") + " kg / " + (maxAvailabilityInKg).ToString("0.0000") + " kg", text_style, GUILayout.Width(150));
                        GUILayout.EndHorizontal();

                        double dayly_production_in_Kg = 1000 * total_power_per_frame * product.TonsProductUsePerMJ * fuelUsePerMJMult / TimeWarp.fixedDeltaTime / FuelEfficiency * current_fuel_mode.NormalisedReactionRate * PluginHelper.SecondsInDay;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(product.FuelName + " Production", bold_style, GUILayout.Width(150));
                        GUILayout.Label(dayly_production_in_Kg.ToString("0.000000") + " kg/day", text_style, GUILayout.Width(150));
                        GUILayout.EndHorizontal();
                    }

                    
                }

                if (!IsNuclear)
                {
                    GUILayout.BeginHorizontal();

                    if (IsEnabled && canShutdown && GUILayout.Button("Deactivate", GUILayout.ExpandWidth(true)))
                        DeactivateReactor();
                    if (!IsEnabled && GUILayout.Button("Activate", GUILayout.ExpandWidth(true)))
                        ActivateReactor();

                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (IsEnabled)
                    {
                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button("Shutdown", GUILayout.ExpandWidth(true)))
                            IsEnabled = false;

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
                GUI.DragWindow();
            }

            catch (Exception e)
            {
                Debug.LogError("[KSPI] - ElectricRCSController Window(" + windowID + "): " + e.Message);
                throw;
            }
        }
    }
}
