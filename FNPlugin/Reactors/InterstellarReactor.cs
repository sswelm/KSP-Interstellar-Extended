using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;
using FNPlugin.Propulsion;
using FNPlugin.Extensions;
using KSP.Localization;

namespace FNPlugin
{
    [KSPModule("#LOC_KSPIE_Reactor_moduleName")]
    class InterstellarReactor : ResourceSuppliableModule, IPowerSource, IRescalable<InterstellarReactor>
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
        [KSPField(isPersistant = true)]
        public int fuelmode_index = -1;
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool isDeployed = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool breedtritium;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true)]
        public double ongoing_consumption_rate;
        [KSPField(isPersistant = true)]
        public bool reactorInit;
        [KSPField(isPersistant = true)]
        public bool reactorBooted;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_startEnabled"), UI_Toggle(disabledText = "True", enabledText = "False")]
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
        public double thermal_power_ratio = 1;
        [KSPField(isPersistant = true)]
        public double charged_power_ratio = 1;
        [KSPField(isPersistant = true)]
        public double reactor_power_ratio = 1;
        [KSPField(isPersistant = true)]
        public double power_request_ratio;
        [KSPField(isPersistant = true)]
        public double maximum_thermal_request_ratio;
        [KSPField(isPersistant = true)]
        public double maximum_charged_request_ratio;
        [KSPField(isPersistant = true)]
        public double maximum_reactor_request_ratio;
        [KSPField(isPersistant = true)]
        public double thermalThrottleRatio;
        [KSPField(isPersistant = true)]
        public double chargedThrottleRatio;

        [KSPField(isPersistant = true)]
        public double storedIsThermalEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)]
        public double storedIsChargedEnergyGeneratorEfficiency;
        [KSPField(isPersistant = true)]
        public double storedGeneratorThermalEnergyRequestRatio;
        [KSPField(isPersistant = true)]
        public double storedGeneratorChargedEnergyRequestRatio;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_electricPriority"), UI_FloatRange(stepIncrement = 1, maxValue = 5, minValue = 0)]
        public float electricPowerPriority = 2;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_powerPercentage"), UI_FloatRange(stepIncrement = 1/3f, maxValue = 100, minValue = 10)]
        public float powerPercentage = 100;
        [KSPField(isPersistant = true)]
        public double ongoing_total_power_generated;
        [KSPField(isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_Reactor_thermalPower", guiFormat = "F6")]
        protected double ongoing_thermal_power_generated;
        [KSPField(isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_Reactor_chargedPower ", guiFormat = "F6")]
        protected double ongoing_charged_power_generated;
        
        [KSPField]
        public float minimumPowerPercentage = 10;
        [KSPField]
        public string upgradeTechReqMk2 = null;
        [KSPField]
        public string upgradeTechReqMk3 = null;
        [KSPField]
        public string upgradeTechReqMk4 = null;
        [KSPField]
        public string upgradeTechReqMk5 = null;

        [KSPField]
        public double minimumThrottleMk1 = 0;
        [KSPField]
        public double minimumThrottleMk2 = 0;
        [KSPField]
        public double minimumThrottleMk3 = 0;
        [KSPField]
        public double minimumThrottleMk4 = 0;
        [KSPField]
        public double minimumThrottleMk5 = 0;

        [KSPField]
        public double fuelEfficencyMk1 = 0;
        [KSPField]
        public double fuelEfficencyMk2 = 0;
        [KSPField]
        public double fuelEfficencyMk3 = 0;
        [KSPField]
        public double fuelEfficencyMk4 = 0;
        [KSPField]
        public double fuelEfficencyMk5 = 0;

        [KSPField]
        public double coreTemperatureMk1 = 0;
        [KSPField]
        public double coreTemperatureMk2 = 0;
        [KSPField]
        public double coreTemperatureMk3 = 0;
        [KSPField]
        public double coreTemperatureMk4 = 0;
        [KSPField]
        public double coreTemperatureMk5 = 0;

        [KSPField]
        public double basePowerOutputMk1 = 0;
        [KSPField]
        public double basePowerOutputMk2 = 0;
        [KSPField]
        public double basePowerOutputMk3 = 0;
        [KSPField]
        public double basePowerOutputMk4 = 0;
        [KSPField]
        public double basePowerOutputMk5 = 0;

        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk1", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk1;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk2", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk2;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk3", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk3;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk4", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk4;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_powerOutputMk5", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F3")]
        public double powerOutputMk5;

        // Settings
        [KSPField]
        public bool supportMHD = false;
        [KSPField]
        public int reactorModeTechBonus = 0;
        [KSPField]
        public bool canBeCombinedWithLab = false;
        [KSPField]
        public bool canBreedTritium = false;
        [KSPField]
        public bool canDisableTritiumBreeding = true;
        [KSPField]
        public bool disableAtZeroThrottle = false;
        [KSPField]
        public bool controlledByEngineThrottle = false;
        [KSPField]
        public bool showShutDownInFlight = false;
        [KSPField]
        public double powerScaleExponent = 3;
        [KSPField]
        public double safetyPowerReductionFraction = 0.95;
        [KSPField]
        public double emergencyPowerShutdownFraction = 0.99;
        [KSPField]
        public double breedDivider = 100000;
        [KSPField]
        public double bonusBufferFactor = 0.05;
        [KSPField]
        public double heatTransportationEfficiency = 0.85;
        [KSPField]
        public double ReactorTemp = 0;
        [KSPField]
        public double powerOutputMultiplier = 1;
        [KSPField]
        public double upgradedReactorTemp = 0;
        [KSPField]
        public string animName = "";
        [KSPField]
        public string loopingAnimationName = "";
        [KSPField]
        public string startupAnimationName = "";
        [KSPField]
        public string shutdownAnimationName = "";
        [KSPField]
        public double reactorSpeedMult = 1;
        [KSPField]
        public double powerRatio;
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public string originalName = "";
        [KSPField]
        public float upgradeCost = 0;
        [KSPField(guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_Reactor_connectionRadius")]
        public double radius = 2.5;
        [KSPField]
        public double minimumThrottle = 0;
        [KSPField]
        public bool canShutdown = true;
        [KSPField]
        public bool consumeGlobal = false;
        [KSPField]
        public int reactorType = 0;
        [KSPField]
        public double fuelEfficiency = 1;
        [KSPField]
        public double upgradedFuelEfficiency = 1;
        [KSPField]
        public bool containsPowerGenerator = false;
        [KSPField]
        public double fuelUsePerMJMult = 1;
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public double hotBathTemperature = 0;
        [KSPField]
        public double alternatorPowerKW = 0;
        [KSPField]
        public bool hasAlternator = false;
        [KSPField]
        public double thermalPropulsionEfficiency = 1;
        [KSPField]
        public double thermalEnergyEfficiency = 1;
        [KSPField]
        public double chargedParticleEnergyEfficiency = 1;
        [KSPField]
        public double chargedParticlePropulsionEfficiency = 1;
        [KSPField]
        public double maxGammaRayPower = 0;
        [KSPField]
        public bool hasBuoyancyEffects = false;
        [KSPField]
        public double geeForceMultiplier = 2;
        [KSPField]
        public double geeForceTreshHold = 1.5;
        [KSPField]
        public double minGeeForceModifier = 0.01;
        [KSPField]
        public double neutronEmbrittlementLifepointsMax = 100;
        [KSPField]
        public double neutronEmbrittlementDivider = 1e+9;
        [KSPField]
        public double hotBathModifier = 1;
        [KSPField]
        public double thermalProcessingModifier = 1;
        [KSPField]
        public int supportedPropellantAtoms = GameConstants.defaultSupportedPropellantAtoms;
        [KSPField]
        public int supportedPropellantTypes = GameConstants.defaultSupportedPropellantTypes;
        [KSPField]
        public bool fullPowerForNonNeutronAbsorbants = true;
        [KSPField]
        public bool showSpecialisedUI = true;
        [KSPField]
        public bool fastNeutrons = true;
        [KSPField]
        public bool canUseNeutronicFuels = true;
        [KSPField]
        public bool canUseGammaRayFuels = true;

        [KSPField]
        public string bimodelUpgradeTechReq = String.Empty;
        [KSPField]
        public string powerUpgradeTechReq = String.Empty;
        [KSPField]
        public double powerUpgradeTechMult = 1;
        [KSPField]
        public double powerUpgradeCoreTempMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_rawPowerOutput", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit", guiFormat = "F4")]
        public double currentRawPowerOutput;

        [KSPField]
        public double PowerOutput = 0;
        [KSPField]
        public double upgradedPowerOutput = 0;
        [KSPField]
        public string upgradeTechReq = String.Empty;
        [KSPField]
        public bool shouldApplyBalance;
        [KSPField]
        public double tritium_molar_mass_ratio = 3.0160 / 7.0183;
        [KSPField]
        public double helium_molar_mass_ratio = 4.0023 / 7.0183;

        // GUI strings
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_coreTemperature")]
        public string coretempStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_reactorStatus")]
        public string statusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorFuelMode")]       
        public string fuelModeStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_connectedRecievers")]
        public string connectedRecieversStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorSurface", guiUnits = " m\xB3")]
        public double reactorSurface;

        [KSPField]
        protected double max_power_to_supply = 0;
        [KSPField]
        protected double requested_thermal_to_supply_per_second;
        [KSPField]
        protected double max_thermal_to_supply_per_second;
        [KSPField]
        protected double requested_charged_to_supply_per_second;
        [KSPField]
        protected double max_charged_to_supply_per_second;
        [KSPField]
        protected double min_throttle;

        // Gui
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Reactor_reactorMass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField]
        public double maximumThermalPowerEffective = 0;
        [KSPField]
        public double geeForceModifier;

        // shared variabels
        protected bool decay_ongoing = false;
        protected bool initialized = false;
        protected double animationStarted = 0;
        protected double powerPcnt;
        protected double totalAmountLithium = 0;
        protected double totalMaxAmountLithium = 0;  

        protected GUIStyle bold_style;
        protected GUIStyle text_style;
        protected List<ReactorFuelType> fuel_modes;
        protected List<ReactorFuelMode> current_fuel_variants_sorted;
        protected ReactorFuelMode current_fuel_variant;
        protected AnimationState[] pulseAnimation;
        protected ModuleAnimateGeneric startupAnimation;
        protected ModuleAnimateGeneric shutdownAnimation;
        protected ModuleAnimateGeneric loopingAnimation;

        Rect windowPosition;
        ReactorFuelType current_fuel_mode;
        PartResourceDefinition lithium6_def;
        PartResourceDefinition tritium_def;
        PartResourceDefinition helium_def;

        List<ReactorProduction> reactorProduction = new List<ReactorProduction>();
        List<IEngineNoozle> connectedEngines = new List<IEngineNoozle>();
        Queue<double> averageGeeForce = new Queue<double>();
        Dictionary<Guid, double> connectedRecievers = new Dictionary<Guid, double>();
        Dictionary<Guid, double> connectedRecieversFraction = new Dictionary<Guid, double>();

        double connectedRecieversSum;
        double partBaseWasteheat;

        double tritiumBreedingMassAdjustment;
        double heliumBreedingMassAdjustment;
        double staticBreedRate;
        double currentIsThermalEnergyGeneratorEfficiency;
        double currentIsChargedEnergyGenratorEfficiency;
        double currentGeneratorThermalEnergyRequestRatio;
        double currentGeneratorChargedEnergyRequestRatio;
        double lithium_consumed_per_second;
        double tritium_produced_per_second;
        double helium_produced_per_second;

        float previousDeltaTime;

        long update_count;
        long last_draw_update;       

        int windowID = 90175467;
        int nrAvailableUpgradeTechs;
        int deactivate_timer = 0;

        bool? hasBimodelUpgradeTechReq;
        bool isConnectedToThermalGenerator;
        bool isFixedUpdatedCalled;
        bool render_window = false;

        public ReactorFuelType CurrentFuelMode
        {
            get { return current_fuel_mode; }
            set
            {
                current_fuel_mode = value;
                fuelmode_index = current_fuel_mode.Index;
                max_power_to_supply = Math.Max(MaximumPower * TimeWarpFixedDeltaTime, 0);
                current_fuel_variants_sorted = current_fuel_mode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, max_power_to_supply, fuelUsePerMJMult);
                current_fuel_variant = current_fuel_variants_sorted.First();
            }
        }

        public double PowerRatio  
        { 
            get 
            {
                powerRatio = (double)(decimal)(powerPercentage / 100);

                return powerRatio; 
            } 
        }

        public bool SupportMHD { get { return supportMHD; } }

        public double ProducedThermalHeat { get { return ongoing_thermal_power_generated; } }

        public int ProviderPowerPriority { get { return (int)electricPowerPriority; } }

        public double RequestedThermalHeat { get;  set; }

        public double RawTotalPowerProduced  { get { return ongoing_total_power_generated; } }

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

                part.RequestResource(product.fuelmode.ResourceName, fuelAmount);
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
            get
            {
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

        public double EfficencyConnectedThermalEnergyGenerator { get { return storedIsThermalEnergyGeneratorEfficiency; } }

        public double EfficencyConnectedChargedEnergyGenerator { get { return storedIsChargedEnergyGeneratorEfficiency; } }


        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio)
        {
            currentIsThermalEnergyGeneratorEfficiency = efficency;
            currentGeneratorThermalEnergyRequestRatio = power_ratio;
            isConnectedToThermalGenerator = true;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio)
        {
            currentIsChargedEnergyGenratorEfficiency = efficency;
            currentGeneratorChargedEnergyRequestRatio = power_ratio;
        }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType)
        {
            shouldApplyBalance = isConnectedToThermalGenerator && generatorType == ElectricGeneratorType.thermal && storedIsThermalEnergyGeneratorEfficiency > 0 && storedIsChargedEnergyGeneratorEfficiency > 0;

            return shouldApplyBalance;
        }

        public bool IsThermalSource { get { return true; } }

        public double ThermalProcessingModifier { get { return thermalProcessingModifier; } }

        public Part Part { get { return this.part; } }

        public double ProducedWasteHeat { get { return ongoing_total_power_generated; } }

        public void AttachThermalReciever(Guid key, double radius)
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

        public double GetFractionThermalReciever(Guid key)
        {
            double result;
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
                storedPowerMultiplier = Math.Pow(factor.absolute.linear, powerScaleExponent);

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

            connectedRecieversSum = connectedRecievers.Sum(r => Math.Pow(r.Value, 2));
            connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => Math.Pow(a.Value, 2) / connectedRecieversSum);

            reactorSurface = Math.Pow(radius, 2);
            connectedRecieversStr = connectedRecievers.Count() + " (" + connectedRecieversSum.ToString("0.000") + " m2)";
        }

        public double ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public bool HasBimodelUpgradeTechReq
        {
            get
            {
                if (hasBimodelUpgradeTechReq == null)
                    hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
                return (bool)hasBimodelUpgradeTechReq;
            }
        }

        public double ChargedParticlePropulsionEfficiency { get { return chargedParticlePropulsionEfficiency; } }

        public double ThermalPropulsionEfficiency { get { return thermalPropulsionEfficiency; } }

        public double ThermalEnergyEfficiency { get { return thermalEnergyEfficiency; } }

        public double ChargedParticleEnergyEfficiency { get { return chargedParticleEnergyEfficiency; } }

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

                return baseEfficency * CurrentFuelMode.FuelEfficencyMultiplier;
            }
        }

        public int ReactorType { get { return reactorType; } }

        public virtual string TypeName { get { return part.partInfo.title; } }

        public virtual double ChargedPowerRatio
        {
            get
            {
                return CurrentFuelMode != null
                    ? CurrentFuelMode.ChargedPowerRatio
                    : 0;
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

                var modifiedBaseCoreTemperature = baseCoreTemperature * EffectiveEmbrittlemenEffectRatio;

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

        public double EffectiveEmbrittlemenEffectRatio
        {
            get { return CheatOptions.UnbreakableJoints ? 1 : Math.Sin(ReactorEmbrittlemenConditionRatio * Math.PI * 0.5); }
        }

        public virtual double ReactorEmbrittlemenConditionRatio
        {
            get { return Math.Min(Math.Max(1 - (neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), maxEmbrittlementFraction), 1); }      
        }

        public virtual double NormalisedMaximumPower
        {
            get { return RawPowerOutput * EffectiveEmbrittlemenEffectRatio * (CurrentFuelMode == null ? 1 : CurrentFuelMode.NormalisedReactionRate); }        
        }

        public virtual double MinimumPower { get { return MaximumPower * MinimumThrottle; } }

        public virtual double MaximumThermalPower 
        { 
            get 
            {
                var power = PowerRatio * NormalisedMaximumPower;

                return (ChargedParticleEnergyEfficiency == 0 && ChargedParticlePropulsionEfficiency == 0) ? power  : power * (1 - ChargedPowerRatio); 
            } 
        }

        public virtual double MaximumChargedPower 
        { 
            get { return (ChargedParticleEnergyEfficiency == 0 && ChargedParticlePropulsionEfficiency == 0) ? 0 : PowerRatio * NormalisedMaximumPower * ChargedPowerRatio; } 
        }

        public double ReactorSpeedMult { get { return reactorSpeedMult; } }

        public virtual bool IsNuclear { get { return false; } }

        public virtual bool IsActive { get { return IsEnabled; } }

        public virtual bool IsVolatileSource { get { return false; } }

        public virtual bool IsFuelNeutronRich { get { return false; } }

        public virtual double MaximumPower { get { return MaximumThermalPower + MaximumChargedPower; } }

        public virtual double StableMaximumReactorPower { get { return IsEnabled ? NormalisedMaximumPower : 0; } }

        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

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

                stored_fuel_ratio = 1;
                IsEnabled = true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Reactor_reactorControlWindow", active = true, guiActiveUnfocused = true, unfocusedRange = 5f, guiActiveUncommand = true)]
        public void ToggleReactorControlWindow()
        {
            render_window = !render_window;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_activateReactor", active = false)]
        public void ActivateReactor()
        {
            StartReactor();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Reactor_deactivateReactor", active = true)]
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

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Reactor_enableTritiumBreeding", active = false)]
        public void StartBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Reactor_disableTritiumBreeding", active = true)]
        public void StopBreedTritiumEvent()
        {
            if (!IsFuelNeutronRich) return;

            breedtritium = false;
        }

        [KSPAction("#LOC_KSPIE_Reactor_activateReactor")]
        public void ActivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            StartReactor();
        }

        [KSPAction("#LOC_KSPIE_Reactor_deactivateReactor")]
        public void DeactivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            DeactivateReactor();
        }

        [KSPAction("#LOC_KSPIE_Reactor_toggleReactor")]
        public void ToggleReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            IsEnabled = !IsEnabled;
        }

        private bool CanPartUpgradeAlternative()
        {
            if (PluginHelper.PartTechUpgrades == null)
            {
                print("[KSPI] - PartTechUpgrades is not initialized");
                return false;
            }

            string upgradetechName;
            if (!PluginHelper.PartTechUpgrades.TryGetValue(part.name, out upgradetechName))
            {
                print("[KSPI] - PartTechUpgrade entry is not found for part '" + part.name + "'");
                return false;
            }

            print("[KSPI] - Found matching Interstellar upgradetech for part '" + part.name + "' with technode " + upgradetechName);

            return PluginHelper.upgradeAvailable(upgradetechName);
        }

        public void DeterminePowerOutput()
        {
            powerOutputMk1 = basePowerOutputMk1 * storedPowerMultiplier;
            powerOutputMk2 = basePowerOutputMk2 * storedPowerMultiplier;
            powerOutputMk3 = basePowerOutputMk3 * storedPowerMultiplier;
            powerOutputMk4 = basePowerOutputMk4 * storedPowerMultiplier;
            powerOutputMk5 = basePowerOutputMk5 * storedPowerMultiplier;

            // if Mk powerOutput is missing, try use lagacy values
            if (powerOutputMk1 == 0)
                powerOutputMk1 = PowerOutput;
            if (powerOutputMk2 == 0)
                powerOutputMk2 = upgradedPowerOutput;
            if (powerOutputMk3 == 0)
                powerOutputMk3 = upgradedPowerOutput * powerUpgradeTechMult;

            // initialise power output when missing
            if (powerOutputMk2 == 0)
                powerOutputMk2 = powerOutputMk1 * 1.5;
            if (powerOutputMk3 == 0)
                powerOutputMk3 = powerOutputMk2 * 1.5;
            if (powerOutputMk4 == 0)
                powerOutputMk4 = powerOutputMk3 * 1.5;
            if (powerOutputMk5 == 0)
                powerOutputMk5 = powerOutputMk4 * 1.5;

            if (minimumThrottleMk1 == 0)
                minimumThrottleMk1 = minimumThrottle;
            if (minimumThrottleMk2 == 0)
                minimumThrottleMk2 = minimumThrottleMk1;
            if (minimumThrottleMk3 == 0)
                minimumThrottleMk3 = minimumThrottleMk2;
            if (minimumThrottleMk4 == 0)
                minimumThrottleMk4 = minimumThrottleMk3;
            if (minimumThrottleMk5 == 0)
                minimumThrottleMk5 = minimumThrottleMk4;
        }

        public override void OnStart(PartModule.StartState state)
        {
            UpdateReactorCharacteristics();

            windowPosition = new Rect(windowPositionX, windowPositionY, 300, 100);
            previousDeltaTime = TimeWarp.fixedDeltaTime - 1.0e-6f;
            hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirementOrEmpty(bimodelUpgradeTechReq);
            staticBreedRate = 1 / powerOutputMultiplier / breedDivider / GameConstants.tritiumBreedRate;

            var powerPercentageField = Fields["powerPercentage"];
            UI_FloatRange[] powerPercentageFloatRange = { powerPercentageField.uiControlFlight as UI_FloatRange, powerPercentageField.uiControlEditor as UI_FloatRange };
            powerPercentageFloatRange[0].minValue = minimumPowerPercentage;
            powerPercentageFloatRange[1].minValue = minimumPowerPercentage;

            if (!part.Resources.Contains(ResourceManager.FNRESOURCE_THERMALPOWER))
            {
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", ResourceManager.FNRESOURCE_THERMALPOWER);
                node.AddValue("maxAmount", PowerOutput);
                node.AddValue("amount", 0);
                part.AddResource(node);
            }

            // while in edit mode, listen to on attach/detach event
            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
            }

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_THERMALPOWER, ResourceManager.FNRESOURCE_WASTEHEAT, ResourceManager.FNRESOURCE_CHARGED_PARTICLES, ResourceManager.FNRESOURCE_MEGAJOULES };
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

            // calculate WasteHeat Capacity
            partBaseWasteheat = part.mass * 1e+5 * wasteHeatMultiplier;

            if (!reactorInit)
            {
                if (startDisabled)
                {
                    last_active_time = Planetarium.GetUniversalTime() - 4d * PluginHelper.SecondsInDay;
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

            tritium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.TritiumGas);
            helium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Helium4Gas);
            lithium6_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Lithium6);

            tritiumBreedingMassAdjustment = tritium_molar_mass_ratio * lithium6_def.density / tritium_def.density;
            heliumBreedingMassAdjustment = helium_molar_mass_ratio * lithium6_def.density / helium_def.density;

            if (IsEnabled && last_active_time > 0)
                DoPersistentResourceUpdate();

            if (!String.IsNullOrEmpty(animName))
                pulseAnimation = PluginHelper.SetUpAnimation(animName, this.part);
            if (!String.IsNullOrEmpty(loopingAnimationName))
                loopingAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == loopingAnimationName);
            if (!String.IsNullOrEmpty(startupAnimationName))
                startupAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == startupAnimationName);
            if (!String.IsNullOrEmpty(shutdownAnimationName))
                shutdownAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == shutdownAnimationName);

            // only force activate if not with a engine model
            var myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                Debug.Log("[KSPI] - Force activate called on " + part.name);
                this.part.force_activate();
                Fields["partMass"].guiActiveEditor = true;
                Fields["radius"].guiActiveEditor = true;
                Fields["connectedRecieversStr"].guiActiveEditor = true;
                Fields["heatTransportationEfficiency"].guiActiveEditor = true;
            }
            else
                Debug.Log("[KSPI] - skipped calling Force on " + part.name);

            Fields["reactorSurface"].guiActiveEditor = showSpecialisedUI;
        }

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
            try
            {
                Debug.Log("[KSPI] - attach " + part.partInfo.title);
                foreach (var node in part.attachNodes)
                {
                    if (node.attachedPart == null) continue;

                    var generator = node.attachedPart.FindModuleImplementing<FNGenerator>();
                    if (generator != null)
                        generator.FindAndAttachToPowerSource();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Reactor.OnEditorAttach " + e.Message);
            }
        }

        private void OnEditorDetach()
        {
            try
            {
                Debug.Log("[KSPI] - detach " + part.partInfo.title);
                if (ConnectedChargedParticleElectricGenerator != null)
                    ConnectedChargedParticleElectricGenerator.FindAndAttachToPowerSource();

                if (ConnectedThermalElectricGenerator != null)
                    ConnectedThermalElectricGenerator.FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Reactor.OnEditorDetach " + e.Message);
            }
        }

        public virtual void Update()
        {
            currentRawPowerOutput = RawPowerOutput;

            Events["DeactivateReactor"].guiActive = HighLogic.LoadedSceneIsFlight && showShutDownInFlight && IsEnabled;

            if (HighLogic.LoadedSceneIsEditor)
            {
                reactorSurface = Math.Pow(radius, 2);
            }
        }

        protected void UpdateFuelMode()
        {
            fuelModeStr = CurrentFuelMode != null ? CurrentFuelMode.ModeGUIName : "null";
        }

        public override void OnUpdate()
        {
            Events["StartBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && !breedtritium && IsFuelNeutronRich && IsEnabled;
            Events["StopBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && breedtritium && IsFuelNeutronRich && IsEnabled;
            UpdateFuelMode();

            coretempStr = CoreTemperature.ToString("0") + " K";
            if (update_count - last_draw_update > 10)
            {
                if (IsEnabled && CurrentFuelMode != null)
                {
                    if (CheatOptions.InfinitePropellant || stored_fuel_ratio > 0.99)
                        statusStr = "Active (" + powerPcnt.ToString("0.000") + "%)";
                    else
                    {
                        if (current_fuel_variant != null)
                            statusStr = current_fuel_variant.ReactorFuels.OrderBy(fuel => GetFuelAvailability(fuel)).First().ResourceName + " Deprived";
                    }
                }
                else
                {
                    if (powerPcnt > 0)
                        statusStr = "Decay Heating (" + powerPcnt.ToString("0.000") + "%)";
                    else
                        statusStr = "Offline";
                }

                last_draw_update = update_count;
            }

            update_count++;
            partMass = part.mass;
        }

        /// <summary>
        /// FixedUpdate is also called when not activated before OnFixedUpdate
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                DeterminePowerOutput();
                maximumThermalPowerEffective = MaximumThermalPower;
                return;
            }

            if (!enabled)
                base.OnFixedUpdate();

            if (!isFixedUpdatedCalled)
            {
                isFixedUpdatedCalled = true;
                UpdateCapacities(stored_fuel_ratio);
            }
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            base.OnFixedUpdate();

            //UpdateWasteheatBuffer(TimeWarp.fixedDeltaTime, 1);

            StoreGeneratorRequests();

            decay_ongoing = false;

            var maximumPower = MaximumPower;

            if (IsEnabled && maximumPower > 0)
            {
                if (ReactorIsOverheating())
                {
                    if (FlightGlobals.ActiveVessel == vessel)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_Reactor_reactorIsOverheating"), 5.0f, ScreenMessageStyle.UPPER_CENTER);

                    IsEnabled = false;
                    return;
                }

                max_power_to_supply = Math.Max(maximumPower * timeWarpFixedDeltaTime, 0);

                if (hasBuoyancyEffects && !CheatOptions.UnbreakableJoints)
                {
                    averageGeeForce.Enqueue(part.vessel.geeForce);
                    if (averageGeeForce.Count > 50)
                        averageGeeForce.Dequeue();

                    geeForceModifier = Math.Min(Math.Max(1 - ((averageGeeForce.Average() - geeForceTreshHold) * geeForceMultiplier), minGeeForceModifier), 1);
                }
                else
                    geeForceModifier = 1;

                current_fuel_variants_sorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, max_power_to_supply * geeForceModifier, fuelUsePerMJMult);
                current_fuel_variant = current_fuel_variants_sorted.FirstOrDefault();
                
                stored_fuel_ratio = CheatOptions.InfinitePropellant ? 1 : current_fuel_variant != null ? Math.Min(current_fuel_variant.FuelRatio, 1) : 0;

                LookForAlternativeFuelTypes();

                UpdateCapacities(stored_fuel_ratio);

                if (stored_fuel_ratio > 0.0001 && stored_fuel_ratio < 0.99)
                {
                    string message = Localizer.Format("#LOC_KSPIE_Reactor_ranOutOfFuelFor") + " " + CurrentFuelMode.ModeGUIName;
                    Debug.Log("[KSPI] - " + message);
                    ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
                }
             
                thermalThrottleRatio = connectedEngines.Any(m => !m.RequiresChargedPower) ? connectedEngines.Where(m => !m.RequiresChargedPower).Max(e => e.CurrentThrottle) : 0;
                chargedThrottleRatio = connectedEngines.Any(m => m.RequiresChargedPower) ? connectedEngines.Where(m => m.RequiresChargedPower).Max(e => e.CurrentThrottle) : 0;

                var thermal_propulsion_ratio = thermalPropulsionEfficiency * thermalThrottleRatio;
                var charged_propulsion_ratio = chargedParticlePropulsionEfficiency * chargedThrottleRatio;
                var thermal_generator_ratio = thermalEnergyEfficiency * storedGeneratorThermalEnergyRequestRatio;
                var charged_generator_ratio = chargedParticleEnergyEfficiency * storedGeneratorChargedEnergyRequestRatio;

                maximum_thermal_request_ratio = Math.Min(thermal_propulsion_ratio + thermal_generator_ratio, 1);
                maximum_charged_request_ratio = Math.Min(charged_propulsion_ratio + charged_generator_ratio, 1);
                maximum_reactor_request_ratio = Math.Max(maximum_thermal_request_ratio, maximum_charged_request_ratio);

                var power_access_modifier = Math.Max(
                    Math.Max(
                        connectedEngines.Any(m => !m.RequiresChargedPower) ? 1 : 0,
                        connectedEngines.Any(m => m.RequiresChargedPower) ? 1 : 0),
                   Math.Max(
                        storedIsThermalEnergyGeneratorEfficiency > 0 ? 1 : 0,
                        storedIsChargedEnergyGeneratorEfficiency > 0 ? 1 : 0
                   ));

                var maximumChargedPower = MaximumChargedPower;
                var maximumThermalPower = MaximumThermalPower;

                power_request_ratio = Math.Max(Math.Max(thermalThrottleRatio, chargedThrottleRatio), Math.Max(storedGeneratorThermalEnergyRequestRatio, storedGeneratorChargedEnergyRequestRatio));

                var safetyThrotleModifier = GetSafetyOverheatPreventionRatio();
                max_charged_to_supply_per_second = maximumChargedPower * stored_fuel_ratio * geeForceModifier * safetyThrotleModifier * power_access_modifier;
                requested_charged_to_supply_per_second = max_charged_to_supply_per_second * power_request_ratio * maximum_charged_request_ratio;

                var chargedParticlesManager = getManagerForVessel(ResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                var thermalHeatManager = getManagerForVessel(ResourceManager.FNRESOURCE_THERMALPOWER);

                min_throttle = stored_fuel_ratio > 0 ? MinimumThrottle / stored_fuel_ratio : 1;
                var needed_charged_power_per_second = getNeededPowerSupplyPerSecondWithMinimumRatio(max_charged_to_supply_per_second, min_throttle, ResourceManager.FNRESOURCE_CHARGED_PARTICLES, chargedParticlesManager);
                charged_power_ratio = Math.Min(maximum_charged_request_ratio, maximumChargedPower > 0 ? needed_charged_power_per_second / maximumChargedPower : 0);
                         
                max_thermal_to_supply_per_second = maximumThermalPower * stored_fuel_ratio * geeForceModifier * safetyThrotleModifier * power_access_modifier;
                requested_thermal_to_supply_per_second = max_thermal_to_supply_per_second * power_request_ratio * maximum_thermal_request_ratio;

                var needed_thermal_power_per_second = getNeededPowerSupplyPerSecondWithMinimumRatio(max_thermal_to_supply_per_second, min_throttle, ResourceManager.FNRESOURCE_THERMALPOWER, thermalHeatManager);
                thermal_power_ratio = Math.Min(maximum_thermal_request_ratio, maximumThermalPower > 0 ? needed_thermal_power_per_second / maximumThermalPower : 0);

                var speedDivider = reactorSpeedMult > 0 ? 20 / reactorSpeedMult : 20;
                reactor_power_ratio = Math.Min(maximum_reactor_request_ratio, (maximum_reactor_request_ratio + Math.Min(maximum_reactor_request_ratio, Math.Max(charged_power_ratio, thermal_power_ratio)) * speedDivider) / (speedDivider + 1));

                ongoing_charged_power_generated = managedProvidedPowerSupplyPerSecondMinimumRatio(requested_charged_to_supply_per_second, max_charged_to_supply_per_second, reactor_power_ratio, ResourceManager.FNRESOURCE_CHARGED_PARTICLES, chargedParticlesManager);
                ongoing_thermal_power_generated = managedProvidedPowerSupplyPerSecondMinimumRatio(requested_thermal_to_supply_per_second, max_thermal_to_supply_per_second, reactor_power_ratio, ResourceManager.FNRESOURCE_THERMALPOWER, thermalHeatManager);
                ongoing_total_power_generated = ongoing_thermal_power_generated + ongoing_charged_power_generated;

                // ignore very small values
                //if (ongoing_total_power_generated < 0.00005 / powerOutputMultiplier)
                //    ongoing_total_power_generated = 0;

                var total_power_received_fixed = ongoing_total_power_generated * timeWarpFixedDeltaTime;

                if (!CheatOptions.UnbreakableJoints && CurrentFuelMode.NeutronsRatio > 0 && CurrentFuelMode.NeutronsRatio > 0)
                    neutronEmbrittlementDamage += ongoing_total_power_generated * timeWarpFixedDeltaTime * CurrentFuelMode.NeutronsRatio / neutronEmbrittlementDivider;
                
                if (!CheatOptions.IgnoreMaxTemperature)
                    supplyFNResourcePerSecondWithMax(ongoing_total_power_generated, NormalisedMaximumPower, ResourceManager.FNRESOURCE_WASTEHEAT);

                ongoing_consumption_rate = ongoing_total_power_generated / maximumPower; 

                PluginHelper.SetAnimationRatio((float)Math.Pow(ongoing_consumption_rate, 4), pulseAnimation);

                powerPcnt = 100 * ongoing_consumption_rate;

                // consume fuel
                if (!CheatOptions.InfinitePropellant)
                {
                    foreach (ReactorFuel fuel in current_fuel_variant.ReactorFuels)
                    {
                        ConsumeReactorFuel(fuel, total_power_received_fixed / geeForceModifier);
                    }

                    // refresh production list
                    reactorProduction.Clear();

                    // produce reactor products
                    foreach (ReactorProduct product in current_fuel_variant.ReactorProducts)
                    {
                        var massProduced = ProduceReactorProduct(product, total_power_received_fixed / geeForceModifier);
                        reactorProduction.Add(new ReactorProduction() { fuelmode = product, mass = massProduced });
                    }
                }

                BreedTritium(ongoing_thermal_power_generated, timeWarpFixedDeltaTime);

                if (Planetarium.GetUniversalTime() != 0)
                    last_active_time = Planetarium.GetUniversalTime();
            }
            else if (!IsEnabled && IsNuclear && MaximumPower > 0 && (Planetarium.GetUniversalTime() - last_active_time <= 3 * PluginHelper.SecondsInDay))
            {
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                double power_fraction = 0.1 * Math.Exp(-(Planetarium.GetUniversalTime() - last_active_time) / PluginHelper.SecondsInDay / 24.0 * 9.0);
                double power_to_supply = Math.Max(MaximumPower * power_fraction, 0);
                ongoing_thermal_power_generated = supplyManagedFNResourcePerSecondWithMinimumRatio(power_to_supply, 1, ResourceManager.FNRESOURCE_THERMALPOWER);
                ongoing_total_power_generated = ongoing_thermal_power_generated;
                BreedTritium(ongoing_thermal_power_generated, timeWarpFixedDeltaTime);
                ongoing_consumption_rate = MaximumPower > 0 ? ongoing_thermal_power_generated / MaximumPower : 0;
                powerPcnt = 100 * ongoing_consumption_rate;
                decay_ongoing = true;
            }
            else
            {
                ongoing_total_power_generated = 0;
                reactor_power_ratio = 0;
                PluginHelper.SetAnimationRatio(0, pulseAnimation);
                powerPcnt = 0;
            }

            if (!IsEnabled)
            {
                var thermalPowerResource = part.Resources[ResourceManager.FNRESOURCE_THERMALPOWER];
                if (thermalPowerResource != null)
                {
                    thermalPowerResource.maxAmount = 0.0001;
                    thermalPowerResource.amount = 0;
                }

                var chargedPowerResource = part.Resources[ResourceManager.FNRESOURCE_CHARGED_PARTICLES];
                if (chargedPowerResource != null)
                {
                    chargedPowerResource.maxAmount = 0.0001;
                    chargedPowerResource.amount = 0;
                }
            }
        }

        private void LookForAlternativeFuelTypes()
        {
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType1);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType2);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType3);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType4);
            SwitchToAlternativeFuelWhenAvailable(CurrentFuelMode.AlternativeFuelType5);
        }

        private void SwitchToAlternativeFuelWhenAvailable(string alternativeFuelTypeName)
        {
            if (stored_fuel_ratio >= 0.99)
                return;

            if (String.IsNullOrEmpty(alternativeFuelTypeName))
                return;

            // look for most advanced version
            var alternativeFuelType = fuel_modes.LastOrDefault(m => m.ModeGUIName.Contains(alternativeFuelTypeName));
            if (alternativeFuelType == null)
            {
                Debug.LogWarning("[KSPI] - failed to find fueltype " + alternativeFuelTypeName);
                return;
            }

            Debug.Log("[KSPI] - searching fuelmodes for alternative for fuel type " + alternativeFuelTypeName);
            var alternative_fuel_variants_sorted = alternativeFuelType.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, max_power_to_supply, fuelUsePerMJMult);

            if (alternative_fuel_variants_sorted == null)
                return;

            var alternative_fuel_variant = alternative_fuel_variants_sorted.FirstOrDefault();
            if (alternative_fuel_variant == null)
            {
                Debug.LogError("[KSPI] - failed to find any variant for fueltype " + alternativeFuelTypeName);
                return;
            }

            if (alternative_fuel_variant.FuelRatio < 0.99)
            {
                Debug.LogWarning("[KSPI] - failed to find sufficient resource for " + alternative_fuel_variant.Name);
                return;
            }

            var message = Localizer.Format("#LOC_KSPIE_Reactor_switchingToAlternativeFuelMode") + " " + alternativeFuelType.ModeGUIName;
            Debug.Log("[KSPI] - " + message);
            ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);

            CurrentFuelMode = alternativeFuelType;
            stored_fuel_ratio = current_fuel_variant.FuelRatio;
        }

        private void StoreGeneratorRequests()
        {
            storedIsThermalEnergyGeneratorEfficiency = currentIsThermalEnergyGeneratorEfficiency;
            storedIsChargedEnergyGeneratorEfficiency = currentIsChargedEnergyGenratorEfficiency;
            currentIsThermalEnergyGeneratorEfficiency = 0;
            currentIsChargedEnergyGenratorEfficiency = 0;
            storedGeneratorThermalEnergyRequestRatio = Math.Min(1, currentGeneratorThermalEnergyRequestRatio);
            storedGeneratorChargedEnergyRequestRatio = Math.Min(1, currentGeneratorChargedEnergyRequestRatio);
            currentGeneratorThermalEnergyRequestRatio = 0;
            currentGeneratorChargedEnergyRequestRatio = 0;
        }

        private void UpdateCapacities(double fuel_ratio)
        {
            var timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;

            // calculate thermalpower capacity
            if (TimeWarp.fixedDeltaTime != previousDeltaTime)
            {
                var thermalPowerResource = part.Resources[ResourceManager.FNRESOURCE_THERMALPOWER];
                if (thermalPowerResource != null)
                {
                    var requiredThermalCapacity = Math.Max(0.0001, 4 * MaximumThermalPower * timeWarpFixedDeltaTime);
                    var thermalPowerRatio = thermalPowerResource.amount / thermalPowerResource.maxAmount;

                    thermalPowerResource.maxAmount = requiredThermalCapacity;

                    if (reactorBooted)
                        thermalPowerResource.amount = Math.Max(0, Math.Min(requiredThermalCapacity, thermalPowerRatio * requiredThermalCapacity));
                    else
                        thermalPowerResource.amount = thermalPowerResource.maxAmount * fuel_ratio;
                }

                var chargedPowerResource = part.Resources[ResourceManager.FNRESOURCE_CHARGED_PARTICLES];
                if (chargedPowerResource != null)
                {
                    var requiredChargedCapacity = Math.Max(0.0001, 4 * MaximumChargedPower * timeWarpFixedDeltaTime);
                    var chargedPowerRatio = chargedPowerResource.amount / chargedPowerResource.maxAmount;

                    chargedPowerResource.maxAmount = requiredChargedCapacity;

                    if (reactorBooted)
                        chargedPowerResource.amount = Math.Max(0, Math.Min(requiredChargedCapacity, chargedPowerRatio * requiredChargedCapacity));
                    else
                        chargedPowerResource.amount = chargedPowerResource.maxAmount * fuel_ratio;
                }

                reactorBooted = true;

                var wasteheatPowerResource = part.Resources[ResourceManager.FNRESOURCE_WASTEHEAT];
                if (wasteheatPowerResource != null)
                {
                    // calculate WasteHeat Capacity
                    partBaseWasteheat = part.mass * 1e+5 * wasteHeatMultiplier;

                    var wasteheat_ratio = wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount;

                    wasteheatPowerResource.maxAmount = timeWarpFixedDeltaTime * partBaseWasteheat; ;
                    wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * wasteheat_ratio;
                }
            }
            else
            {
                var thermalPowerResource = part.Resources[ResourceManager.FNRESOURCE_THERMALPOWER];
                if (thermalPowerResource != null)
                {
                    var thermalPowerRatio = Math.Max(0, Math.Min(1,thermalPowerResource.amount / thermalPowerResource.maxAmount));
                    thermalPowerResource.maxAmount = Math.Max(0.0001, 4 * timeWarpFixedDeltaTime * MaximumThermalPower);
                    thermalPowerResource.amount = Math.Max(0, Math.Min(thermalPowerResource.maxAmount, thermalPowerRatio * thermalPowerResource.maxAmount));
                }

                var chargedPowerResource = part.Resources[ResourceManager.FNRESOURCE_CHARGED_PARTICLES];
                if (chargedPowerResource != null)
                {
                    var chargedPowerRatio = Math.Max(0, Math.Min(1, chargedPowerResource.amount / chargedPowerResource.maxAmount));
                    chargedPowerResource.maxAmount = Math.Max(0.0001, 4 * timeWarpFixedDeltaTime * MaximumChargedPower);
                    chargedPowerResource.amount = Math.Max(0, Math.Min(chargedPowerResource.maxAmount, chargedPowerRatio * chargedPowerResource.maxAmount));
                }

                var wasteheatPowerResource = part.Resources[ResourceManager.FNRESOURCE_WASTEHEAT];
                if (wasteheatPowerResource != null)
                {
                    var wasteHeatRatio = Math.Max(0, Math.Min(1, wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount));
                    wasteheatPowerResource.maxAmount = Math.Max(0.0001, timeWarpFixedDeltaTime * partBaseWasteheat);
                    wasteheatPowerResource.amount = Math.Max(0, Math.Min(wasteheatPowerResource.maxAmount, wasteHeatRatio * wasteheatPowerResource.maxAmount));
                }
            }

            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        protected double GetFuelRatio(ReactorFuel reactorFuel, double fuelEfficency, double megajoules)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            var fuelUseForPower = reactorFuel.GetFuelUseForPower(fuelEfficency, megajoules, fuelUsePerMJMult);

            return fuelUseForPower > 0 ? GetFuelAvailability(reactorFuel) / fuelUseForPower : 0;
        }

        private void BreedTritium(double neutron_power_received_each_second, double fixedDeltaTime)
        {
            if (!breedtritium || neutron_power_received_each_second <= 0 || fixedDeltaTime <= 0)
            {
                tritium_produced_per_second = 0;
                helium_produced_per_second = 0;
                return;
            }

            var partResourceLithium6 = part.Resources[InterstellarResourcesConfiguration.Instance.Lithium6];
            if (partResourceLithium6 != null)
            {
                totalAmountLithium = partResourceLithium6.amount;
                totalMaxAmountLithium = partResourceLithium6.maxAmount;
            }
            else
            {
                totalAmountLithium = 0;
                totalMaxAmountLithium = 0;
            }

            double ratioLithium6 = totalAmountLithium > 0 ? totalAmountLithium / totalMaxAmountLithium : 0;

            // calculate current maximum litlium consumption
            var breed_rate = CurrentFuelMode.TritiumBreedModifier * staticBreedRate * neutron_power_received_each_second * fixedDeltaTime * Math.Sqrt(ratioLithium6);
            var lith_rate = breed_rate / lithium6_def.density;

            // get spare room tritium
            var spareRoomTritiumAmount = part.GetResourceSpareCapacity(tritium_def);

            // limit lithium consumption to maximum tritium storage
            var maximumTritiumProduction = lith_rate * tritiumBreedingMassAdjustment;
            var maximumLitiumConsumtionRatio = maximumTritiumProduction > 0 ? Math.Min(maximumTritiumProduction, spareRoomTritiumAmount) / maximumTritiumProduction : 0;
            var lithium_request = lith_rate * maximumLitiumConsumtionRatio;

            // consume the lithium
            var lith_used = CheatOptions.InfinitePropellant
                ? lithium_request
                : part.RequestResource(lithium6_def.id, lithium_request, ResourceFlowMode.STACK_PRIORITY_SEARCH);

            // calculate effective lithium used for tritium breeding
            lithium_consumed_per_second = lith_used / fixedDeltaTime;

            // caculate products
            var tritium_production = lith_used * tritiumBreedingMassAdjustment;
            var helium_production = lith_used * heliumBreedingMassAdjustment;

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

        public double Radius
        {
            get { return radius; }
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

            sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_getInfoReactorInfo"));
            sb.AppendLine(originalName);
            sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_thermalPower") + ": " + PluginHelper.getFormattedPowerString(powerOutputMk1));
            sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_coreTemperature") + ": " + coreTemperatureMk1.ToString("0") + "K");
            sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_fuelEfficiency") + ": " + (fuelEfficencyMk1 * 100.0).ToString("0.00") + "%");
            sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_getInfoFuelModes"));
            basic_fuelmodes.ForEach(fm =>
            {
                sb.AppendLine("---");
                sb.AppendLine(fm.ModeGUIName);
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_powerMultiplier") + ": " + fm.NormalisedReactionRate.ToString("0.00"));
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_chargedParticleRatio") + ": " + fm.ChargedPowerRatio.ToString("0.00"));
                sb.AppendLine(Localizer.Format("#LOC_KSPIE_Reactor_totalEnergyDensity") + ": " + fm.ReactorFuels.Sum(fuel => fuel.EnergyDensity).ToString("0.00") + " MJ/kg");
                foreach (ReactorFuel fuel in fm.ReactorFuels)
                {
                    sb.AppendLine(fuel.ResourceName + " " + fuel.AmountFuelUsePerMJ * fuelUsePerMJMult * PowerOutput * fm.NormalisedReactionRate * PluginHelper.SecondsInDay / fuelEfficiency + fuel.Unit + "/day");
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

            // determine avialable variants
            var persistant_fuel_variants_sorted = CurrentFuelMode.GetVariantsOrderedByFuelRatio(this.part, FuelEfficiency, delta_time_diff * ongoing_total_power_generated, fuelUsePerMJMult);

            // consume fuel
            foreach (ReactorFuel fuel in persistant_fuel_variants_sorted.First().ReactorFuels)
            {
                ConsumeReactorFuel(fuel, delta_time_diff * ongoing_total_power_generated);
            }

            // produce reactor products
            foreach (ReactorProduct product in persistant_fuel_variants_sorted.First().ReactorProducts)
            {
                ProduceReactorProduct(product, delta_time_diff * ongoing_total_power_generated);
            }

            // breed tritium
            BreedTritium(ongoing_total_power_generated * (1 - CurrentFuelMode.ChargedPowerRatio), delta_time_diff);

        }

        protected bool ReactorIsOverheating()
        {
            if (!CheatOptions.IgnoreMaxTemperature && getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT) >= emergencyPowerShutdownFraction && canShutdown)
            {
                deactivate_timer++;
                if (deactivate_timer > 3)
                    return true;
            }
            else
                deactivate_timer = 0;

            return false;
        }

        protected double GetSafetyOverheatPreventionRatio()
        {
            if (CheatOptions.IgnoreMaxTemperature)
                return 1;

            var wasteheatRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);
            if (wasteheatRatio < safetyPowerReductionFraction)
                return 1;

            return  1 - (wasteheatRatio - safetyPowerReductionFraction) / (emergencyPowerShutdownFraction - safetyPowerReductionFraction);
        }

        protected List<ReactorFuelType> GetReactorFuelModes()
        {
            ConfigNode[] fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");

            var filteredFuelModes = fuelmodes.Select(node => new ReactorFuelMode(node))
                .Where(fm =>
                       fm.AllFuelResourcesDefinitionsAvailable
                    && fm.AllProductResourcesDefinitionsAvailable 
                    && (fm.SupportedReactorTypes & ReactorType) == ReactorType
                    && PluginHelper.HasTechRequirementOrEmpty(fm.TechRequirement)
                    && ReactorTechLevel >= fm.TechLevel
                    && (fm.Aneutronic || canUseNeutronicFuels)
                    && maxGammaRayPower >= fm.GammaRayEnergy
                    ).ToList();

            for (int i = 0; i < filteredFuelModes.Count; i++)
            {
                filteredFuelModes[i].Position = i;
            }

            Debug.Log("[KSPI] - found " + filteredFuelModes.Count + " valid fuel types");

            //return filteredFuelModes;
            var groups = filteredFuelModes.GroupBy(mode => mode.ModeGUIName).Select(group => new ReactorFuelType(group)).ToList();

            Debug.Log("[KSPI] - grouped them into " + groups.Count + " valid fuel modes");

            return groups;
        }

        protected bool FuelRequiresLab(bool requiresLab)
        {
            bool isConnectedToLab = part.IsConnectedToModule("ScienceModule", 10);

            return !requiresLab || isConnectedToLab && canBeCombinedWithLab;
        }

        protected virtual void setDefaultFuelMode()
        {
            max_power_to_supply = Math.Max(MaximumPower * TimeWarpFixedDeltaTime, 0);
            CurrentFuelMode = fuel_modes.FirstOrDefault();

            if (CurrentFuelMode == null)
                print("[KSPI] - Warning : CurrentFuelMode is null");
            else
                print("[KSPI] - CurrentFuelMode = " + CurrentFuelMode.ModeGUIName);
        }

        protected double ConsumeReactorFuel(ReactorFuel fuel, double MJpower)
        {
            if (MJpower < (0.000005 / powerOutputMultiplier))
                return 0;

            var consume_amount_in_unit_of_storage = MJpower * fuel.AmountFuelUsePerMJ * fuelUsePerMJMult / FuelEfficiency;

            if (!fuel.ConsumeGlobal)
            {
                if (part.Resources.Contains(fuel.ResourceName))
                {
                    double amount = Math.Min(consume_amount_in_unit_of_storage, part.Resources[fuel.ResourceName].amount);
                    part.Resources[fuel.ResourceName].amount -= amount;
                    return amount;
                }
                else
                    return 0;
            }
            return part.RequestResource(fuel.Definition.id, consume_amount_in_unit_of_storage, ResourceFlowMode.ALL_VESSEL);
        }

        protected virtual double ProduceReactorProduct(ReactorProduct product, double MJpower)
        {
            if (product.Definition == null)
                return 0;

            var product_supply = MJpower * product.AmountProductUsePerMJ * fuelUsePerMJMult / FuelEfficiency;

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                {
                    var partResource = part.Resources[product.ResourceName];
                    double availableStorage = partResource.maxAmount - partResource.amount;
                    double possibleAmount = Math.Min(product_supply, availableStorage);
                    part.Resources[product.ResourceName].amount += possibleAmount;
                    return product_supply * product.DensityInTon;
                }
                else
                    return 0;
            }

            part.RequestResource(product.Definition.id, -product_supply, ResourceFlowMode.ALL_VESSEL);
            return product_supply * product.DensityInTon;
        }

        protected double GetFuelAvailability(ReactorFuel fuel)
        {
            if (fuel == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability fuel null");

            if (!fuel.ConsumeGlobal)
                return GetLocalResourceAmount(fuel);

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(fuel.Definition);
            else
                return part.FindAmountOfAvailableFuel(fuel.ResourceName, 4);
        }

        protected double GetLocalResourceRatio(ReactorFuel fuel)
        {
            if (part.Resources.Contains(fuel.ResourceName))
                return part.Resources[fuel.ResourceName].amount / part.Resources[fuel.ResourceName].maxAmount;
            else
                return 0;
        }

        protected double GetLocalResourceAmount(ReactorFuel fuel)
        {
            if (part.Resources.Contains(fuel.ResourceName))
                return part.Resources[fuel.ResourceName].amount;
            else
                return 0;
        }

        protected double GetFuelAvailability(PartResourceDefinition definition)
        {
            if (definition == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability definition null");

            if (definition.resourceTransferMode == ResourceTransferMode.NONE)
            {
                if (part.Resources.Contains(definition.name))
                    return part.Resources[definition.name].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(definition);
            else
                return part.FindAmountOfAvailableFuel(definition.name, 4);
        }

        protected double GetProductAvailability(ReactorProduct product)
        {
            if (product == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability product null");

            if (product.Definition == null)
            {
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability product definition null");
                return 0;
            }

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                    return part.Resources[product.ResourceName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(product.Definition);
            else
                return part.FindAmountOfAvailableFuel(product.ResourceName, 4);
        }

        protected double GetMaxProductAvailability(ReactorProduct product)
        {
            if (product == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability product null");

            if (product.Definition == null)
                return 0;

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.ResourceName))
                    return part.Resources[product.ResourceName].maxAmount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceMaxAvailable(product.Definition);
            else
                return part.FindMaxAmountOfAvailableFuel(product.ResourceName, 4);
        }

        public void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, Localizer.Format("#LOC_KSPIE_Reactor_reactorControlWindow"));
        }

        protected void PrintToGUILayout(string label, string value, GUIStyle bold_style, GUIStyle text_style, int witdhLabel = 150, int witdhValue = 150)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, bold_style, GUILayout.Width(witdhLabel));
            GUILayout.Label(value, text_style, GUILayout.Width(witdhValue));
            GUILayout.EndHorizontal();
        }

        protected virtual void WindowReactorSpecificOverride() { }

        private void Window(int windowID)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                if (bold_style == null)
                    bold_style = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold, font = PluginHelper.MainFont};

                if (text_style == null)
                    text_style = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Normal,font = PluginHelper.MainFont};

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    render_window = false;

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label(TypeName, bold_style, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                PrintToGUILayout("Reactor Embrittlement", (100 * (1 - ReactorEmbrittlemenConditionRatio)).ToString("0.000000") + "%", bold_style, text_style);
                PrintToGUILayout("Radius", radius + "m", bold_style, text_style);
                PrintToGUILayout("Core Temperature", coretempStr, bold_style, text_style);
                PrintToGUILayout("Status", statusStr, bold_style, text_style);
                PrintToGUILayout("Fuel Mode", fuelModeStr, bold_style, text_style);

                WindowReactorSpecificOverride();

                PrintToGUILayout("Max Power Output", PluginHelper.getFormattedPowerString(NormalisedMaximumPower, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(RawPowerOutput, "0.0", "0.000"), bold_style, text_style);

                if (ChargedPowerRatio < 1.0)
                    PrintToGUILayout("Thermal Power", PluginHelper.getFormattedPowerString(ongoing_thermal_power_generated, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(MaximumThermalPower, "0.0", "0.000"), bold_style, text_style);
                if (ChargedPowerRatio > 0)
                    PrintToGUILayout("Charged Power", PluginHelper.getFormattedPowerString(ongoing_charged_power_generated, "0.0", "0.000") + " / " + PluginHelper.getFormattedPowerString(MaximumChargedPower, "0.0", "0.000"), bold_style, text_style);

                if (CurrentFuelMode != null && current_fuel_variant.ReactorFuels != null)
                {
                    if (IsFuelNeutronRich && breedtritium && canBreedTritium)
                    {
                        PrintToGUILayout("Fuel Neutron Breed Rate", 100 * CurrentFuelMode.NeutronsRatio + "% ", bold_style, text_style);

                        var tritium_kg_day = tritium_produced_per_second * tritium_def.density * 1000 * PluginHelper.SecondsInDay;
                        PrintToGUILayout("Tritium Breed Rate", tritium_kg_day.ToString("0.000000") + " kg/day ", bold_style, text_style);

                        var helium_kg_day = helium_produced_per_second * helium_def.density * 1000 * PluginHelper.SecondsInDay;
                        PrintToGUILayout("Helium Breed Rate", helium_kg_day.ToString("0.000000") + " kg/day ", bold_style, text_style);

                        double totalLithium6Amount;
                        double totalLithium6MaxAmount;
                        part.GetConnectedResourceTotals(lithium6_def.id, out totalLithium6Amount, out totalLithium6MaxAmount);

                        PrintToGUILayout("Lithium Reserves", totalLithium6Amount.ToString("0.000") + " L / " + totalLithium6MaxAmount.ToString("0.000") + " L", bold_style, text_style);

                        var lithium_consumption_day = lithium_consumed_per_second * PluginHelper.SecondsInDay;
                        PrintToGUILayout("Lithium Consumption", lithium_consumption_day.ToString("0.00000") + " L/day", bold_style, text_style);
                        var lithium_lifetime_total_days = lithium_consumption_day > 0 ? totalLithium6Amount / lithium_consumption_day : 0;

                        var lithium_lifetime_years = Math.Floor(lithium_lifetime_total_days / GameConstants.KERBIN_YEAR_IN_DAYS);
                        var lithium_lifetime_years_remainder_in_days = lithium_lifetime_total_days % GameConstants.KERBIN_YEAR_IN_DAYS;

                        var lithium_lifetime_remaining_days = Math.Floor(lithium_lifetime_years_remainder_in_days);
                        var lithium_lifetime_remaining_days_remainer = lithium_lifetime_years_remainder_in_days % 1;

                        var lithium_lifetime_remaining_hours = lithium_lifetime_remaining_days_remainer * PluginHelper.SecondsInDay / GameConstants.SECONDS_IN_HOUR;

                        if (lithium_lifetime_years < 1e9)
                        {
                            if (lithium_lifetime_years <= 0)
                                PrintToGUILayout("Lithium Remaining", lithium_lifetime_remaining_days + " days " + lithium_lifetime_remaining_hours.ToString("0.0000") + " hours", bold_style, text_style);
                            else if (lithium_lifetime_years < 1e3)
                                PrintToGUILayout("Lithium Remaining", lithium_lifetime_years + " years " + lithium_lifetime_remaining_days + " days " + lithium_lifetime_remaining_hours.ToString("0.0000") + " hours", bold_style, text_style);
                            else if (lithium_lifetime_years < 1e6)
                                PrintToGUILayout("Lithium Remaining", lithium_lifetime_years + " years " + lithium_lifetime_remaining_days + " days ", bold_style, text_style);
                            else
                                PrintToGUILayout("Lithium Remaining", lithium_lifetime_years + " years " , bold_style, text_style);
                        }

                        double totalTritiumAmount;
                        double totalTritiumMaxAmount;
                        part.GetConnectedResourceTotals(tritium_def.id, out totalTritiumAmount, out totalTritiumMaxAmount);

                        var MassTritiumAmount = totalTritiumAmount * tritium_def.density * 1000;
                        var MassTritiumMaxAmount = totalTritiumMaxAmount * tritium_def.density * 1000;

                        PrintToGUILayout("Tritium Storage", MassTritiumAmount.ToString("0.000000") + " kg / " + MassTritiumMaxAmount.ToString("0.000000") + " kg", bold_style, text_style);

                        double totalHeliumAmount;
                        double totalHeliumMaxAmount;
                        part.GetConnectedResourceTotals(helium_def.id, out totalHeliumAmount, out totalHeliumMaxAmount);

                        var MassHeliumAmount = totalHeliumAmount * helium_def.density * 1000;
                        var MassHeliumMaxAmount = totalHeliumMaxAmount * helium_def.density * 1000;

                        PrintToGUILayout("Helium Storage", MassHeliumAmount.ToString("0.000000") + " kg / " + MassHeliumMaxAmount.ToString("0.000000") + " kg", bold_style, text_style);
                    }
                    else
                        PrintToGUILayout("Is Neutron rich", IsFuelNeutronRich.ToString(), bold_style, text_style);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Fuel", bold_style, GUILayout.Width(150));
                    GUILayout.EndHorizontal();


                    foreach (var fuel in current_fuel_variant.ReactorFuels)
                    {
                        if (fuel == null)
                            continue;

                        var resourceVariantsDefinitions = CurrentFuelMode.ResourceGroups.First(m => m.name == fuel.FuelName).resourceVariantsMetaData;

                        var availableRessources = resourceVariantsDefinitions
                            .Select(m => new {resourceDefinition = m.resourceDefinition, ratio = m.ratio}).Distinct()
                            .Select(d => new { definition = d.resourceDefinition, amount = GetFuelAvailability(d.resourceDefinition), effectiveDensity = d.resourceDefinition.density * d.ratio})
                            .Where(m => m.amount > 0).ToList();

                        var availabilityInTon = availableRessources.Sum(m => m.amount * m.effectiveDensity);

                        PrintToGUILayout(fuel.FuelName + " Reserves", PluginHelper.formatMassStr(availabilityInTon) + " (" + availableRessources.Count + " variants)", bold_style, text_style);

                        var ton_fuel_use_per_hour = ongoing_total_power_generated * fuel.TonsFuelUsePerMJ * fuelUsePerMJMult / FuelEfficiency * PluginHelper.SecondsInHour;
                        var kg_fuel_use_per_hour = ton_fuel_use_per_hour * 1000;
                        var kg_fuel_use_per_day = kg_fuel_use_per_hour * PluginHelper.HoursInDay;

                        PrintToGUILayout(fuel.FuelName + " Consumption ", PluginHelper.formatMassStr(ton_fuel_use_per_hour) + " / hour", bold_style, text_style);

                        if (kg_fuel_use_per_day > 0)
                        {
                            var fuel_lifetime_d = availabilityInTon * 1000 / kg_fuel_use_per_day;
                            var lifetime_years = Math.Floor(fuel_lifetime_d / GameConstants.KERBIN_YEAR_IN_DAYS);
                            if (lifetime_years < 1e9)
                            {
                                if (lifetime_years > 0)
                                {
                                    var lifetime_years_day_remainder = lifetime_years < 1e+6 ? fuel_lifetime_d % GameConstants.KERBIN_YEAR_IN_DAYS : 0;
                                    PrintToGUILayout(fuel.FuelName + " Lifetime", (double.IsNaN(lifetime_years) ? "-" : lifetime_years + " years " + (lifetime_years_day_remainder).ToString("0.00")) + " days", bold_style, text_style);
                                }
                                else
                                    PrintToGUILayout(fuel.FuelName + " Lifetime", (double.IsNaN(fuel_lifetime_d) ? "-" : (fuel_lifetime_d).ToString("0.00")) + " days", bold_style, text_style);
                            }
                            else
                                PrintToGUILayout(fuel.FuelName + " Lifetime", "", bold_style, text_style);
                        }
                        else
                            PrintToGUILayout(fuel.FuelName + " Lifetime", "", bold_style, text_style);
                    }

                    if (current_fuel_variant.ReactorProducts.Count > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Products", bold_style, GUILayout.Width(150));
                        GUILayout.EndHorizontal();

                        foreach (var product in current_fuel_variant.ReactorProducts)
                        {
                            if (product == null)
                                continue;

                            double availabilityInTon = GetProductAvailability(product) * product.DensityInTon;
                            double maxAvailabilityInTon = GetMaxProductAvailability(product) * product.DensityInTon;

                            GUILayout.BeginHorizontal();
                            GUILayout.Label(product.FuelName + " Storage", bold_style, GUILayout.Width(150));
                            GUILayout.Label(PluginHelper.formatMassStr(availabilityInTon, "0.00000") + " / " + PluginHelper.formatMassStr(maxAvailabilityInTon, "0.00000"), text_style, GUILayout.Width(150));
                            GUILayout.EndHorizontal();

                            double hour_production_in_Ton = ongoing_total_power_generated * product.TonsProductUsePerMJ * fuelUsePerMJMult / FuelEfficiency * PluginHelper.SecondsInHour;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(product.FuelName + " Production", bold_style, GUILayout.Width(150));
                            GUILayout.Label(PluginHelper.formatMassStr(hour_production_in_Ton) + " / hour", text_style, GUILayout.Width(150));
                            GUILayout.EndHorizontal();
                        }
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

        public override string getResourceManagerDisplayName()
        {
            var displayName = part.partInfo.title;
            if (fuel_modes.Count > 1 )
                displayName += " (" + fuelModeStr + ")";
            if (similarParts != null && similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }
    }
}