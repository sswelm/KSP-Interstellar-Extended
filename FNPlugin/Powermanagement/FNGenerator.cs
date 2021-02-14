using FNPlugin.Beamedpower;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Powermanagement.Interfaces;
using FNPlugin.Reactors;
using FNPlugin.Redist;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    enum PowerStates { PowerOnline, PowerOffline };

    [KSPModule("Thermal Electric Effect Generator")]
    class ThermalElectricEffectGenerator : FNGenerator {}

    [KSPModule("Integrated Thermal Electric Power Generator")]
    class IntegratedThermalElectricPowerGenerator : FNGenerator { }

    [KSPModule("Thermal Electric Power Generator")]
    class ThermalElectricPowerGenerator : FNGenerator {}

    [KSPModule("Integrated Charged Particles Power Generator")]
    class IntegratedChargedParticlesPowerGenerator : FNGenerator {}

    [KSPModule("Charged Particles Power Generator")]
    class ChargedParticlesPowerGenerator : FNGenerator {}

    [KSPModule(" Generator")]
    class FNGenerator : ResourceSuppliableModule, IUpgradeableModule, IFNElectricPowerGeneratorSource, IPartMassModifier, IRescalable<FNGenerator>
    {
        public const string GROUP = "FNGenerator";
        public const string GROUP_TITLE = "#LOC_KSPIE_Generator_groupName";

        // Persistent
        [KSPField(isPersistant = true)] public bool IsEnabled = true;
        [KSPField(isPersistant = true)] public bool generatorInit;
        [KSPField(isPersistant = true)] public bool isUpgraded;
        [KSPField(isPersistant = true)] public bool chargedParticleMode = false;
        [KSPField(isPersistant = true)] public double storedMassMultiplier;
        [KSPField(isPersistant = true)] public double maximumElectricPower;
        [KSPField(isPersistant = true)] public bool hasAdjustPowerPercentage;   // ToDo remove in the future

        // Settings
        [KSPField] public string animName = "";
        [KSPField] public string upgradeTechReq = "";
        [KSPField] public string upgradeCostStr = "";

        [KSPField] public bool calculatedMass = false;
        [KSPField] public bool isHighPower = false;
        [KSPField] public bool isMHD = false;
        [KSPField] public bool isLimitedByMinThrottle = false;
        [KSPField] public bool maintainsMegaWattPowerBuffer = true;
        [KSPField] public bool showSpecialisedUI = true;
        [KSPField] public bool showDetailedInfo = true;
        [KSPField] public bool controlWasteHeatBuffer = true;

        [KSPField] public float upgradeCost = 1;
        [KSPField] public float powerCapacityMaxValue = 100;
        [KSPField] public float powerCapacityMinValue = 0.5f;
        [KSPField] public float powerCapacityStepIncrement = 0.5f;

        [KSPField] public double efficiencyMk1;
        [KSPField] public double efficiencyMk2;
        [KSPField] public double efficiencyMk3;
        [KSPField] public double efficiencyMk4;
        [KSPField] public double efficiencyMk5;
        [KSPField] public double efficiencyMk6;
        [KSPField] public double efficiencyMk7;
        [KSPField] public double efficiencyMk8;
        [KSPField] public double efficiencyMk9;

        [KSPField] public string Mk2TechReq = "";
        [KSPField] public string Mk3TechReq = "";
        [KSPField] public string Mk4TechReq = "";
        [KSPField] public string Mk5TechReq = "";
        [KSPField] public string Mk6TechReq = "";
        [KSPField] public string Mk7TechReq = "";
        [KSPField] public string Mk8TechReq = "";
        [KSPField] public string Mk9TechReq = "";

        [KSPField] public double powerOutputMultiplier = 1;
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public double baseHeatAmount = 2.0e+5;
        [KSPField] public double massModifier = 1;
        [KSPField] public double rawPowerToMassDivider = 1000;
        [KSPField] public double coreTemperateHotBathExponent = 0.7;
        [KSPField] public double capacityToMassExponent = 0.7;
        [KSPField] public double minimumBufferSize = 0;
        [KSPField] public double coldBathTemp = 500;
        [KSPField] public double maximumGeneratorPowerEC;

        // Gui
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_powerCapacity"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerCapacity = 100;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_powerControl"), UI_FloatRange(stepIncrement = 1f, maxValue = 200f, minValue = 1f)]
        public float powerPercentage = 101;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_maxGeneratorEfficiency", guiFormat = "P1")]
        public double maxEfficiency;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_maxUsageRatio")]
        public double powerUsageEfficiency;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_maxTheoreticalPower", guiFormat = "F2")]
        public string maximumTheoreticalPower;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiFormat = "F2", guiName = "#LOC_KSPIE_Generator_radius", guiUnits = " m")]
        public double radius = 2.5;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_partMass", guiUnits = " t", guiFormat = "F3")]
        public float partMass;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_currentElectricPower", guiUnits = " MW_e", guiFormat = "F2")]
        public string OutputPower;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_MaximumElectricPower")]//Maximum Electric Power
        public string MaxPowerStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_ElectricEfficiency")]//Electric Efficiency
        public string overallEfficiencyStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_coldBathTemp", guiUnits = " K", guiFormat = "F0")]
        public double coldBathTempDisplay = 500;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_hotBathTemp", guiUnits = " K", guiFormat = "F0")]
        public double hotBathTemp = 300;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F3")]
        public double electrical_power_currently_needed;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_InitialGeneratorPowerEC", guiUnits = " kW", guiFormat = "F0")]
        public double initialGeneratorPowerEC;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "Maximum Generator Power", guiUnits = " Mw", guiFormat = "F3")]
        public double maximumGeneratorPowerMJ;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "Received Power Per Second", guiUnits = " Mw")]
        public double received_power_per_second;

        // privates
        private bool _appliesBalance;
        private bool _shouldUseChargedPower;
        private bool _playPowerDownSound = true;
        private bool _playPowerUpSound = true;
        private bool _hasRequiredUpgrade;

        private double _chargedPowerReceived;
        private double _requestedChargedPower;
        private double _reactorPowerRequested;
        private double _targetMass;
        private double _initialMass;
        private double _maxThermalPower;
        private double _effectiveMaximumThermalPower;
        private double _maxStableMegaWattPower;
        private double _maxChargedPowerForThermalGenerator;
        private double _maxChargedPowerForChargedGenerator;
        private double _maxAllowedChargedPower;
        private double _maxReactorPower;
        private double _megawattBufferAmount;
        private double hotColdBathRatio;
        private double _heatExchangerThrustDivisor = 1;
        private double _thermalPowerReceived;
        private double _requestedPostChargedPower;
        private double _requestedPostThermalPower;
        private double _requestedPostReactorPower;
        private double _electricPowerPerSecond;
        private double _maxElectricPowerPerSecond;
        private double _totalEfficiency;
        private double _outputPower;
        private double _powerDownFraction;

        private Animation _animation;
        private PowerStates _powerState;
        private IFNPowerSource attachedPowerSource;
        private ResourceBuffers _resourceBuffers;
        private ModuleGenerator stockModuleGenerator;
        private MethodInfo _powerGeneratorReliablityEvent;
        private ModuleResource _outputModuleResource;

        private PartModule powerGeneratorProcessController;

        private BaseField _powerGeneratorCapacity;
        private BaseField moduleGeneratorEfficientBaseField;

        private BaseEvent moduleGeneratorShutdownBaseEvent;
        private BaseEvent moduleGeneratorActivateBaseEvent;

        private readonly Queue<double> _powerDemandQueue = new Queue<double>();

        public string UpgradeTechnology => upgradeTechReq;

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "Reset", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]
        public void Reset()
        {
            _powerGeneratorCapacity?.SetValue(0, powerGeneratorProcessController);

            _powerGeneratorReliablityEvent?.Invoke(powerGeneratorProcessController, new object[] { false });
        }


        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Generator_activateGenerator", active = true)]
        public void ActivateGenerator()
        {
            IsEnabled = true;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Generator_deactivateGenerator", active = false)]
        public void DeactivateGenerator()
        {
            IsEnabled = false;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Generator_retrofitGenerator", active = true)]
        public void RetrofitGenerator()
        {
            if (ResearchAndDevelopment.Instance == null) return;

            if (isUpgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        [KSPAction("#LOC_KSPIE_Generator_activateGenerator")]
        public void ActivateGeneratorAction(KSPActionParam param)
        {
            ActivateGenerator();
        }

        [KSPAction("#LOC_KSPIE_Generator_deactivateGenerator")]
        public void DeactivateGeneratorAction(KSPActionParam param)
        {
            DeactivateGenerator();
        }

        [KSPAction("#LOC_KSPIE_Generator_toggleGenerator")]
        public void ToggleGeneratorAction(KSPActionParam param)
        {
            IsEnabled = !IsEnabled;
        }

        public virtual void OnRescale(ScalingFactor factor)
        {
            Debug.Log("[KSPI]: FNGenerator.OnRescale called with " + factor.absolute.linear);
            storedMassMultiplier = Math.Pow((double) (decimal) factor.absolute.linear, 3);
            _initialMass = (double) (decimal) part.prefabMass * storedMassMultiplier;

            UpdateHeatExchangedThrustDivisor();
            UpdateModuleGeneratorOutput();
        }

        public void Refresh()
        {
            Debug.Log("[KSPI]: FNGenerator Refreshed");
            UpdateTargetMass();
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (!calculatedMass)
                return 0;

            var moduleMassDelta = (float)(_targetMass - _initialMass);

            return moduleMassDelta;
        }

        public void upgradePartModule()
        {
            isUpgraded = true;
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI]: attach " + part.partInfo.title);
                FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.OnEditorAttach " + e.Message);
            }
        }

        /// <summary>
        /// Event handler which is called when part is deptach to a exiting part
        /// </summary>
        private void OnEditorDetach()
        {
            try
            {
                if (attachedPowerSource == null)
                    return;

                Debug.Log("[KSPI]: detach " + part.partInfo.title);
                if (chargedParticleMode && attachedPowerSource.ConnectedChargedParticleElectricGenerator != null)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                if (!chargedParticleMode && attachedPowerSource.ConnectedThermalElectricGenerator != null)
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.OnEditorDetach " + e.Message);
            }
        }

        private void OnDestroyed()
        {
            try
            {
                OnEditorDetach();

                RemoveItselfAsManager();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception on " + part.name + " during FNGenerator.OnDestroyed with message " + e.Message);
            }
        }

        public override void OnStart(StartState state)
        {
            ConnectToModuleGenerator();

            this.resourcesToSupply = new[] { ResourceSettings.Config.ElectricPowerInMegawatt, ResourceSettings.Config.WasteHeatInMegawatt, ResourceSettings.Config.ThermalPowerInMegawatt, ResourceSettings.Config.ChargedParticleInMegawatt };

            // adjust power percentage to for recharging to buffer
            if (!hasAdjustPowerPercentage)
            {
                if (Math.Abs(powerPercentage - 100) < float.Epsilon)
                    powerPercentage = 101;

                hasAdjustPowerPercentage = true;
            }

            _resourceBuffers = new ResourceBuffers();

            _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricPowerInMegawatt));

            if(!Kerbalism.IsLoaded)
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricPowerInKilowatt, 1000 / powerOutputMultiplier));

            if (controlWasteHeatBuffer)
            {
                _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, baseHeatAmount, true));
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            }

            _resourceBuffers.Init(part);

            base.OnStart(state);

            var prefabMass = (double)(decimal)part.prefabMass;
            _targetMass = prefabMass * storedMassMultiplier;
            _initialMass = prefabMass * storedMassMultiplier;

            if (_initialMass == 0)
                _initialMass = prefabMass;
            if (_targetMass == 0)
                _targetMass = prefabMass;

            InitializeEfficiency();

            var powerCapacityField = Fields["powerCapacity"];
            powerCapacityField.guiActiveEditor = !isLimitedByMinThrottle;

            if (powerCapacityField.uiControlEditor is UI_FloatRange powerCapacityFloatRange)
            {
                powerCapacityFloatRange.maxValue = powerCapacityMaxValue;
                powerCapacityFloatRange.minValue = powerCapacityMinValue;
                powerCapacityFloatRange.stepIncrement = powerCapacityStepIncrement;
            }

            if (state  == StartState.Editor)
            {
                powerCapacity = Math.Max(powerCapacityMinValue, powerCapacity);
                powerCapacity = Math.Min(powerCapacityMaxValue, powerCapacity);
            }

            Fields[nameof(partMass)].guiActive = Fields[nameof(partMass)].guiActiveEditor = calculatedMass;
            Fields[nameof(powerPercentage)].guiActive = Fields[nameof(powerPercentage)].guiActiveEditor = showSpecialisedUI;
            Fields[nameof(radius)].guiActiveEditor = showSpecialisedUI;

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isUpgraded = true;
                    _hasRequiredUpgrade = true;
                    upgradePartModule();
                }
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
                part.OnEditorDestroy += OnEditorDetach;

                part.OnJustAboutToBeDestroyed += OnDestroyed;
                part.OnJustAboutToDie += OnDestroyed;

                FindAndAttachToPowerSource();
                return;
            }

            if (this.HasTechsRequiredToUpgrade())
                _hasRequiredUpgrade = true;

            // only force activate if no certain part modules are not present and not limited by minimum throttle
            if (!isLimitedByMinThrottle && part.FindModuleImplementing<BeamedPowerReceiver>() == null && part.FindModuleImplementing<InterstellarReactor>() == null)
            {
                Debug.Log("[KSPI]: Generator on " + part.name + " was Force Activated");
                part.force_activate();
            }

            _animation = part.FindModelAnimators(animName).FirstOrDefault();
            if (_animation != null)
            {
                _animation[animName].layer = 1;
                if (!IsEnabled)
                {
                    _animation[animName].normalizedTime = 1;
                    _animation[animName].speed = -1;
                }
                else
                {
                    _animation[animName].normalizedTime = 0;
                    _animation[animName].speed = 1;
                }
                _animation.Play();
            }

            if (generatorInit == false)
            {
                IsEnabled = true;
            }

            if (isUpgraded)
                upgradePartModule();

            FindAndAttachToPowerSource();

            UpdateHeatExchangedThrustDivisor();
        }

        private void ConnectToModuleGenerator()
        {
            if (maintainsMegaWattPowerBuffer == false)
                return;

            foreach (var partModule in part.Modules)
            {
                if (partModule.ClassName != "ProcessController") continue;
                var tittleField = partModule.Fields["title"];
                if (tittleField == null) continue;
                var title = (string) tittleField.GetValue(partModule);
                if (title != "Power Generator") continue;
                powerGeneratorProcessController = partModule;
                var type = powerGeneratorProcessController.GetType();
                _powerGeneratorReliablityEvent =  type.GetMethod("ReliablityEvent");
                _powerGeneratorCapacity = powerGeneratorProcessController.Fields["capacity"];
                break;
            }

            stockModuleGenerator = part.FindModuleImplementing<ModuleGenerator>();

            if (stockModuleGenerator == null) return;

            _outputModuleResource = stockModuleGenerator.resHandler.outputResources.FirstOrDefault(m => m.name == ResourceSettings.Config.ElectricPowerInKilowatt);

            if (_outputModuleResource == null) return;

            moduleGeneratorShutdownBaseEvent = stockModuleGenerator.Events[nameof(ModuleGenerator.Shutdown)];
            if (moduleGeneratorShutdownBaseEvent != null)
            {
                moduleGeneratorShutdownBaseEvent.guiActive = false;
                moduleGeneratorShutdownBaseEvent.guiActiveEditor = false;
            }

            moduleGeneratorActivateBaseEvent = stockModuleGenerator.Events[nameof(ModuleGenerator.Activate)];
            if (moduleGeneratorActivateBaseEvent != null)
            {
                moduleGeneratorActivateBaseEvent.guiActive = false;
                moduleGeneratorActivateBaseEvent.guiActiveEditor = false;
            }

            moduleGeneratorEfficientBaseField = stockModuleGenerator.Fields[nameof(ModuleGenerator.efficiency)];
            if (moduleGeneratorEfficientBaseField != null)
            {
                moduleGeneratorEfficientBaseField.guiActive = false;
                moduleGeneratorEfficientBaseField.guiActiveEditor = false;
            }

            initialGeneratorPowerEC = _outputModuleResource.rate;

            if (maximumGeneratorPowerEC > 0)
            {
                //outputModuleResource.rate = maximumGeneratorPowerEC;
            }

            maximumGeneratorPowerEC = _outputModuleResource.rate;
            maximumGeneratorPowerMJ = maximumGeneratorPowerEC / GameConstants.ecPerMJ;

            //mockInputResource = new ModuleResource
            //{
            //    name = outputModuleResource.name, id = outputModuleResource.name.GetHashCode()
            //};

            //stockModuleGenerator.resHandler.inputResources.Add(mockInputResource);
        }

        private void InitializeEfficiency()
        {
            if (efficiencyMk1 == 0)
                efficiencyMk1 = 0.1;
            if (efficiencyMk2 == 0)
                efficiencyMk2 = efficiencyMk1;
            if (efficiencyMk3 == 0)
                efficiencyMk3 = efficiencyMk2;
            if (efficiencyMk4 == 0)
                efficiencyMk4 = efficiencyMk3;
            if (efficiencyMk5 == 0)
                efficiencyMk5 = efficiencyMk4;
            if (efficiencyMk6 == 0)
                efficiencyMk6 = efficiencyMk5;
            if (efficiencyMk7 == 0)
                efficiencyMk7 = efficiencyMk6;
            if (efficiencyMk8 == 0)
                efficiencyMk8 = efficiencyMk7;
            if (efficiencyMk9 == 0)
                efficiencyMk9 = efficiencyMk8;

            if (string.IsNullOrEmpty(Mk2TechReq))
                Mk2TechReq = upgradeTechReq;

            int techLevel = 1;
            if (PluginHelper.UpgradeAvailable(Mk9TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk8TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk7TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk6TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk5TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk4TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk3TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk2TechReq))
                techLevel++;

            if (techLevel >= 9)
                maxEfficiency = efficiencyMk9;
            else if (techLevel == 8)
                maxEfficiency = efficiencyMk8;
            else if (techLevel >= 7)
                maxEfficiency = efficiencyMk7;
            else if (techLevel == 6)
                maxEfficiency = efficiencyMk6;
            else if (techLevel == 5)
                maxEfficiency = efficiencyMk5;
            else if (techLevel == 4)
                maxEfficiency = efficiencyMk4;
            else if (techLevel == 3)
                maxEfficiency = efficiencyMk3;
            else if (techLevel == 2)
                maxEfficiency = efficiencyMk2;
            else
                maxEfficiency = efficiencyMk1;
        }

        /// <summary>
        /// Finds the nearest available thermalsource and update effective part mass
        /// </summary>
        public void FindAndAttachToPowerSource()
        {
            // disconnect
            if (attachedPowerSource != null)
            {
                if (chargedParticleMode)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                else
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }

            // first look if part contains an thermal source
            attachedPowerSource = part.FindModulesImplementing<IFNPowerSource>().FirstOrDefault();
            if (attachedPowerSource != null)
            {
                ConnectToPowerSource();
                Debug.Log("[KSPI]: Found power source locally");
                return;
            }

            if (!part.attachNodes.Any() || part.attachNodes.All(m => m.attachedPart == null))
            {
                Debug.Log("[KSPI]: not connected to any parts yet");
                UpdateTargetMass();
                return;
            }

            Debug.Log("[KSPI]: generator is currently connected to " + part.attachNodes.Count + " parts");
            // otherwise look for other non self contained thermal sources that is not already connected

            var searchResult = chargedParticleMode
                ? FindChargedParticleSource()
                : isMHD
                    ? FindPlasmaPowerSource()
                    : FindThermalPowerSource();

            // quit if we failed to find anything
            if (searchResult == null)
            {
                Debug.LogWarning("[KSPI]: Failed to find power source");
                return;
            }

            // verify cost is not higher than 1
            var partDistance = (int)Math.Max(Math.Ceiling(searchResult.Cost), 0);
            if (partDistance > 1)
            {
                Debug.LogWarning("[KSPI]: Found power source but at too high cost");
                return;
            }

            // update attached thermalsource
            attachedPowerSource = searchResult.Source;

            Debug.Log("[KSPI]: successfully connected to " + attachedPowerSource.Part.partInfo.title);

            ConnectToPowerSource();
        }

        private void ConnectToPowerSource()
        {
            //connect with source
            if (chargedParticleMode)
                attachedPowerSource.ConnectedChargedParticleElectricGenerator = this;
            else
                attachedPowerSource.ConnectedThermalElectricGenerator = this;

            UpdateTargetMass();

            UpdateHeatExchangedThrustDivisor();

            UpdateModuleGeneratorOutput();
        }

        private void UpdateModuleGeneratorOutput()
        {
            if (attachedPowerSource == null)
                return;

            var maximumPower = isLimitedByMinThrottle ? attachedPowerSource.MinimumPower : attachedPowerSource.MaximumPower;
            maximumGeneratorPowerMJ = maximumPower * maxEfficiency * _heatExchangerThrustDivisor;

            var generatorRate = maximumGeneratorPowerMJ * GameConstants.ecPerMJ;

            if (_outputModuleResource != null)
                _outputModuleResource.rate = maximumGeneratorPowerMJ * GameConstants.ecPerMJ;
            else
            {
                if (HighLogic.LoadedSceneIsFlight)
                    _powerGeneratorCapacity?.SetValue(0, powerGeneratorProcessController);
                else
                    _powerGeneratorCapacity?.SetValue(generatorRate, powerGeneratorProcessController);
            }
        }

        private PowerSourceSearchResult FindThermalPowerSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                    p => p.IsThermalSource && p.ConnectedThermalElectricGenerator == null && p.ThermalEnergyEfficiency > 0,
                    3, 3, 3, true);
            return searchResult;
        }

        private PowerSourceSearchResult FindPlasmaPowerSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                     p => p.IsThermalSource && p.ConnectedChargedParticleElectricGenerator == null && p.PlasmaEnergyEfficiency > 0,
                     3, 3, 3, true);
            return searchResult;
        }

        private PowerSourceSearchResult FindChargedParticleSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                     p => p.IsThermalSource && p.ConnectedChargedParticleElectricGenerator == null && p.ChargedParticleEnergyEfficiency > 0,
                     3, 3, 3, true);
            return searchResult;
        }

        private void UpdateTargetMass()
        {
            try
            {
                if (attachedPowerSource == null)
                {
                    _targetMass = _initialMass;
                    return;
                }

                if (chargedParticleMode && attachedPowerSource.ChargedParticleEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.ChargedParticleEnergyEfficiency;
                else if (isMHD && attachedPowerSource.PlasmaEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.PlasmaEnergyEfficiency;
                else if (attachedPowerSource.ThermalEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.ThermalEnergyEfficiency;
                else
                    powerUsageEfficiency = 1;

                var rawMaximumPower = attachedPowerSource.RawMaximumPowerForPowerGeneration * powerUsageEfficiency;
                maximumTheoreticalPower = PluginHelper.GetFormattedPowerString(rawMaximumPower * CapacityRatio * maxEfficiency);

                // verify if mass calculation is active
                if (!calculatedMass)
                {
                    _targetMass = _initialMass;
                    return;
                }

                // update part mass
                if (rawMaximumPower > 0 && rawPowerToMassDivider > 0)
                    _targetMass = massModifier * attachedPowerSource.ThermalProcessingModifier * rawMaximumPower * Math.Pow(CapacityRatio, capacityToMassExponent) / rawPowerToMassDivider;
                else
                    _targetMass = _initialMass;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.UpdateTargetMass " + e.Message);
            }
        }

        public double CapacityRatio => (double)(decimal)powerCapacity * 0.01;

        public double PowerControlRatio => (double)(decimal)powerPercentage * 0.01;

        public double GetHotBathTemperature(double coldBathTemperature)
        {
            if (attachedPowerSource == null)
                return -1;

            var coreTemperature = attachedPowerSource.GetCoreTempAtRadiatorTemp(coldBathTemperature);

            var plasmaTemperature = coreTemperature <= attachedPowerSource.HotBathTemperature
                ? coreTemperature
                : attachedPowerSource.HotBathTemperature + Math.Pow(coreTemperature - attachedPowerSource.HotBathTemperature, coreTemperateHotBathExponent);

            double temperature;
            if (_appliesBalance || !isMHD)
                temperature = attachedPowerSource.HotBathTemperature;
            else
            {
                if (attachedPowerSource.SupportMHD)
                    temperature = plasmaTemperature;
                else
                {
                    var chargedPowerModifier = attachedPowerSource.ChargedPowerRatio * attachedPowerSource.ChargedPowerRatio;
                    temperature = plasmaTemperature * chargedPowerModifier + (1 - chargedPowerModifier) * attachedPowerSource.HotBathTemperature; // for fusion reactors connected to MHD
                }
            }

            return temperature;
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            Events[nameof(ActivateGenerator)].active = !IsEnabled && showSpecialisedUI;
            Events[nameof(DeactivateGenerator)].active = IsEnabled && showSpecialisedUI;
            Fields[nameof(overallEfficiencyStr)].guiActive = showDetailedInfo && IsEnabled;
            Fields[nameof(MaxPowerStr)].guiActive = showDetailedInfo && IsEnabled;
            Fields[nameof(coldBathTempDisplay)].guiActive = showDetailedInfo && !chargedParticleMode;
            Fields[nameof(hotBathTemp)].guiActive = showDetailedInfo && !chargedParticleMode;

            if (ResearchAndDevelopment.Instance != null)
            {
                Events[nameof(RetrofitGenerator)].active = !isUpgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasRequiredUpgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
            }
            else
                Events[nameof(RetrofitGenerator)].active = false;

            if (IsEnabled)
            {
                if (_playPowerUpSound && _animation != null)
                {
                    _playPowerDownSound = true;
                    _playPowerUpSound = false;
                    _animation[animName].speed = 1;
                    _animation[animName].normalizedTime = 0;
                    _animation.Blend(animName, 2);
                }
            }
            else
            {
                if (_playPowerDownSound && _animation != null)
                {
                    _playPowerDownSound = false;
                    _playPowerUpSound = true;
                    _animation[animName].speed = -1;
                    _animation[animName].normalizedTime = 1;
                    _animation.Blend(animName, 2);
                }
            }

            if (IsEnabled)
            {
                var percentOutputPower = _totalEfficiency * 100.0;
                var outputPowerReport = -_outputPower;

                OutputPower = PluginHelper.GetFormattedPowerString(outputPowerReport);
                overallEfficiencyStr = percentOutputPower.ToString("0.00") + "%";

                maximumElectricPower = _totalEfficiency >= 0
                    ? !chargedParticleMode
                        ? _totalEfficiency * _maxThermalPower
                        : _totalEfficiency * _maxChargedPowerForChargedGenerator
                    : 0;

                MaxPowerStr = PluginHelper.GetFormattedPowerString(maximumElectricPower);
            }
            else
                OutputPower = Localizer.Format("#LOC_KSPIE_Generator_Offline");//"Generator Offline"

            if (moduleGeneratorEfficientBaseField != null)
            {
                moduleGeneratorEfficientBaseField.guiActive = false;
                moduleGeneratorEfficientBaseField.guiActiveEditor = false;
            }
        }

        public double RawGeneratorSourcePower
        {
            get
            {
                if (attachedPowerSource == null || !IsEnabled)
                    return 0;

                var maxPowerUsageRatio =
                    chargedParticleMode
                        ? attachedPowerSource.ChargedParticleEnergyEfficiency
                        : isMHD
                            ? attachedPowerSource.PlasmaEnergyEfficiency
                            : attachedPowerSource.ThermalEnergyEfficiency;

                var rawMaxPower = isLimitedByMinThrottle
                    ? attachedPowerSource.MinimumPower
                    : HighLogic.LoadedSceneIsEditor
                        ? attachedPowerSource.MaximumPower
                        : attachedPowerSource.StableMaximumReactorPower;

                return rawMaxPower * attachedPowerSource.PowerRatio * maxPowerUsageRatio * CapacityRatio;
            }
        }

        public double MaxEfficiency => maxEfficiency;

        public double MaxStableMegaWattPower => RawGeneratorSourcePower * maxEfficiency ;

        private void UpdateHeatExchangedThrustDivisor()
        {
            if (attachedPowerSource == null || attachedPowerSource.Radius <= 0 || radius <= 0)
            {
                _heatExchangerThrustDivisor = 1;
                return;
            }

            _heatExchangerThrustDivisor = radius > attachedPowerSource.Radius
                ? attachedPowerSource.Radius * attachedPowerSource.Radius / radius / radius
                : radius * radius / attachedPowerSource.Radius / attachedPowerSource.Radius;
        }

        private void UpdateGeneratorPower()
        {
            if (attachedPowerSource == null) return;

            if (!chargedParticleMode) // thermal or plasma mode
            {
                //averageRadiatorTemperatureQueue.Enqueue(FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel));

                //while (averageRadiatorTemperatureQueue.Count > 10)
                //    averageRadiatorTemperatureQueue.Dequeue();

                //coldBathTempDisplay = averageRadiatorTemperatureQueue.Average();
                coldBathTempDisplay = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel);

                hotBathTemp = GetHotBathTemperature(coldBathTempDisplay);

                coldBathTemp = coldBathTempDisplay * 0.75;
            }

            if (HighLogic.LoadedSceneIsEditor)
                UpdateHeatExchangedThrustDivisor();

            var attachedPowerSourceRatio = attachedPowerSource.PowerRatio;
            _effectiveMaximumThermalPower = attachedPowerSource.MaximumThermalPower * Math.Min(1, PowerControlRatio) * CapacityRatio;

            var rawThermalPower = isLimitedByMinThrottle ? attachedPowerSource.MinimumPower : _effectiveMaximumThermalPower;
            var rawChargedPower = attachedPowerSource.MaximumChargedPower * Math.Min(1, PowerControlRatio) * CapacityRatio;
            var rawReactorPower = rawThermalPower + rawChargedPower;

            if (!(attachedPowerSourceRatio > 0))
            {
                _maxChargedPowerForThermalGenerator = rawChargedPower;
                _maxThermalPower = rawThermalPower;
                _maxReactorPower = rawReactorPower;
                return;
            }

            var attachedPowerSourceMaximumThermalPowerUsageRatio = isMHD
                ? attachedPowerSource.PlasmaEnergyEfficiency
                : attachedPowerSource.ThermalEnergyEfficiency;

            var potentialThermalPower = (isMHD || !_appliesBalance ? rawReactorPower: rawThermalPower) / attachedPowerSourceRatio;
            _maxAllowedChargedPower = rawChargedPower * (chargedParticleMode ? attachedPowerSource.ChargedParticleEnergyEfficiency : 1);

            _maxThermalPower = attachedPowerSourceMaximumThermalPowerUsageRatio * Math.Min(rawReactorPower, potentialThermalPower);
            _maxChargedPowerForThermalGenerator = attachedPowerSourceMaximumThermalPowerUsageRatio    * Math.Min(rawChargedPower, (1 / attachedPowerSourceRatio) * _maxAllowedChargedPower);
            _maxChargedPowerForChargedGenerator = attachedPowerSource.ChargedParticleEnergyEfficiency * Math.Min(rawChargedPower, (1 / attachedPowerSourceRatio) * _maxAllowedChargedPower);
            _maxReactorPower = chargedParticleMode ? _maxChargedPowerForChargedGenerator : _maxThermalPower;
        }

        // Update is called in the editor
        // ReSharper disable once UnusedMember.Global
        public void Update()
        {
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight) return;

            UpdateTargetMass();

            UpdateHeatExchangedThrustDivisor();

            UpdateModuleGeneratorOutput();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (IsEnabled && attachedPowerSource != null && FNRadiator.HasRadiatorsForVessel(vessel))
            {
                _appliesBalance = isMHD || attachedPowerSource.ShouldApplyBalance(chargedParticleMode ? ElectricGeneratorType.charged_particle : ElectricGeneratorType.thermal);

                UpdateGeneratorPower();

                // check if MaxStableMegaWattPower is changed
                _maxStableMegaWattPower = MaxStableMegaWattPower;

                UpdateBuffers();

                generatorInit = true;

                // don't produce any power when our reactor has stopped
                if (_maxStableMegaWattPower <= 0)
                {
                    _electricPowerPerSecond = 0;
                    _maxElectricPowerPerSecond = 0;
                    PowerDown();

                    return;
                }

                if (stockModuleGenerator != null)
                    stockModuleGenerator.generatorIsActive = _maxStableMegaWattPower > 0;

                _powerDownFraction = 1;

                //var wasteheatRatio = getResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);
                var wasteheatRatio = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel) / 4500;
                var overheatingModifier = wasteheatRatio < 0.9 ? 1 : (1 - wasteheatRatio) * 10;

                var thermalPowerRatio = 1 - attachedPowerSource.ChargedPowerRatio;
                var chargedPowerRatio = attachedPowerSource.ChargedPowerRatio;

                if (!chargedParticleMode) // thermal mode
                {
                    hotColdBathRatio = Math.Max(Math.Min(1 - coldBathTemp / hotBathTemp, 1), 0);

                    _totalEfficiency = Math.Min(maxEfficiency, hotColdBathRatio * maxEfficiency);

                    if (hotColdBathRatio <= 0.01 || coldBathTemp <= 0 || hotBathTemp <= 0 || _maxThermalPower <= 0) return;

                    electrical_power_currently_needed = CalculateElectricalPowerCurrentlyNeeded();

                    var effectiveThermalPowerNeededForElectricity = electrical_power_currently_needed / _totalEfficiency;

                    _reactorPowerRequested = Math.Max(0, Math.Min(_maxReactorPower, effectiveThermalPowerNeededForElectricity));
                    _requestedPostReactorPower = Math.Max(0, attachedPowerSource.MinimumPower - _reactorPowerRequested);

                    var thermalPowerRequested = Math.Max(0, Math.Min(_maxThermalPower, effectiveThermalPowerNeededForElectricity));
                    thermalPowerRequested *= _appliesBalance && chargedPowerRatio < 1 ? thermalPowerRatio : 1;
                    _requestedPostThermalPower = Math.Max(0, attachedPowerSource.MinimumPower * thermalPowerRatio - thermalPowerRequested);

                    double initialThermalPowerReceived;
                    if (chargedPowerRatio < 1)
                    {
                        var requestedThermalPower = Math.Min(thermalPowerRequested, _effectiveMaximumThermalPower);
                        initialThermalPowerReceived = ConsumeFnResourcePerSecond(requestedThermalPower, ResourceSettings.Config.ThermalPowerInMegawatt);
                        var thermalPowerRequestRatio = Math.Min(1, _effectiveMaximumThermalPower > 0 ? requestedThermalPower / attachedPowerSource.MaximumThermalPower : 0);
                        attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEfficiency, thermalPowerRequestRatio, isMHD, isLimitedByMinThrottle ? part.mass * 0.05 : part.mass);
                    }
                    else
                        initialThermalPowerReceived = 0;

                    _thermalPowerReceived = initialThermalPowerReceived;
                    var totalPowerReceived = _thermalPowerReceived;

                    _shouldUseChargedPower = chargedPowerRatio > 0;

                    // Collect charged power when needed
                    if (chargedPowerRatio >= 1)
                    {
                        _requestedChargedPower = _reactorPowerRequested;

                        _chargedPowerReceived = ConsumeFnResourcePerSecond(_requestedChargedPower, ResourceSettings.Config.ChargedParticleInMegawatt);

                        var maximumChargedPower = attachedPowerSource.MaximumChargedPower * powerUsageEfficiency * CapacityRatio;
                        var chargedPowerRequestRatio = Math.Min(1, maximumChargedPower > 0 ? thermalPowerRequested / maximumChargedPower : 0);

                        attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEfficiency, chargedPowerRequestRatio, isMHD, isLimitedByMinThrottle ? part.mass * 0.05 : part.mass);
                    }
                    else if (_shouldUseChargedPower && _thermalPowerReceived < _reactorPowerRequested)
                    {
                        _requestedChargedPower = Math.Min(Math.Min(_reactorPowerRequested - _thermalPowerReceived, _maxChargedPowerForThermalGenerator), Math.Max(0, _maxReactorPower - _thermalPowerReceived));
                        _chargedPowerReceived = ConsumeFnResourcePerSecond(_requestedChargedPower, ResourceSettings.Config.ChargedParticleInMegawatt);
                    }
                    else
                    {
                        ConsumeFnResourcePerSecond(0, ResourceSettings.Config.ChargedParticleInMegawatt);
                        _chargedPowerReceived = 0;
                        _requestedChargedPower = 0;
                    }

                    totalPowerReceived += _chargedPowerReceived;

                    // any shortage should be consumed again from remaining thermalPower
                    if (_shouldUseChargedPower && chargedPowerRatio < 1 && totalPowerReceived < _reactorPowerRequested)
                    {
                        var finalRequest = Math.Max(0, _reactorPowerRequested - totalPowerReceived);
                        _thermalPowerReceived += ConsumeFnResourcePerSecond(finalRequest, ResourceSettings.Config.ThermalPowerInMegawatt);
                    }
                }
                else // charged particle mode
                {
                    hotColdBathRatio = 1;

                    _totalEfficiency = maxEfficiency;

                    if (_totalEfficiency <= 0) return;

                    electrical_power_currently_needed = CalculateElectricalPowerCurrentlyNeeded();

                    _requestedChargedPower = overheatingModifier * Math.Max(0, Math.Min(_maxAllowedChargedPower, electrical_power_currently_needed / _totalEfficiency));
                    _requestedPostChargedPower = overheatingModifier * Math.Max(0, (attachedPowerSource.MinimumPower * chargedPowerRatio) - _requestedChargedPower);

                    var maximumChargedPower = attachedPowerSource.MaximumChargedPower * attachedPowerSource.ChargedParticleEnergyEfficiency;
                    var chargedPowerRequestRatio = Math.Min(1, maximumChargedPower > 0 ? _requestedChargedPower / maximumChargedPower : 0);
                    attachedPowerSource.NotifyActiveChargedEnergyGenerator(_totalEfficiency, chargedPowerRequestRatio, part.mass);

                    _chargedPowerReceived = ConsumeFnResourcePerSecond(_requestedChargedPower, ResourceSettings.Config.ChargedParticleInMegawatt);
                }

                received_power_per_second = _thermalPowerReceived + _chargedPowerReceived;
                var effectiveInputPowerPerSecond = received_power_per_second * _totalEfficiency;

                if (!CheatOptions.IgnoreMaxTemperature)
                    ConsumeFnResourcePerSecond(effectiveInputPowerPerSecond, ResourceSettings.Config.WasteHeatInMegawatt);

                _electricPowerPerSecond = Math.Max(effectiveInputPowerPerSecond * powerOutputMultiplier, 0);
                if (!chargedParticleMode)
                {
                    var effectiveMaxThermalPowerRatio = isMHD || !_appliesBalance ? 1 : thermalPowerRatio;
                    _maxElectricPowerPerSecond = effectiveMaxThermalPowerRatio * attachedPowerSource.StableMaximumReactorPower * attachedPowerSource.PowerRatio * powerUsageEfficiency * _totalEfficiency * CapacityRatio;
                }
                else
                    _maxElectricPowerPerSecond = overheatingModifier * _maxChargedPowerForChargedGenerator * _totalEfficiency;

                var availableGeneratorRate = Math.Max(0, maximumGeneratorPowerMJ - _electricPowerPerSecond) * GameConstants.ecPerMJ;

                attachedPowerSource.UpdateAuxiliaryPowerSource(availableGeneratorRate);

                var requiredElectricCharge = (GetRequiredElectricCharge() * GameConstants.ecPerMJ);

                if (_outputModuleResource != null)
                    _outputModuleResource.rate = requiredElectricCharge;

                _outputPower = isLimitedByMinThrottle
                    ? -SupplyManagedFnResourcePerSecond(_electricPowerPerSecond, ResourceSettings.Config.ElectricPowerInMegawatt)
                    : -SupplyFnResourcePerSecondWithMaxAndEfficiency(_electricPowerPerSecond, _maxElectricPowerPerSecond, hotColdBathRatio, ResourceSettings.Config.ElectricPowerInMegawatt);
            }
            else
            {
                _electricPowerPerSecond = 0;
                _maxElectricPowerPerSecond = 0;
                generatorInit = true;
                SupplyManagedFnResourcePerSecond(0, ResourceSettings.Config.ElectricPowerInMegawatt);

                if (stockModuleGenerator != null && stockModuleGenerator.generatorIsActive)
                    stockModuleGenerator.Shutdown();

                if (IsEnabled && !vessel.packed)
                {
                    if (attachedPowerSource == null)
                    {
                        IsEnabled = false;
                        var message = Localizer.Format("#LOC_KSPIE_Generator_Msg1");//"Generator Shutdown: No Attached Power Source"
                        Debug.Log("[KSPI]: " + message);
                        ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }
                    else if ( !FNRadiator.HasRadiatorsForVessel(vessel))
                    {
                        IsEnabled = false;
                        var message = Localizer.Format("#LOC_KSPIE_Generator_Msg2");//"Generator Shutdown: No radiators available!"
                        Debug.Log("[KSPI]: " + message);
                        ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }
                }
                else
                {
                    PowerDown();
                }
            }
        }


        public override void OnPostResourceSuppliable(double fixedDeltaTime)
        {
            if (attachedPowerSource == null) return;

            double postThermalPowerReceived = 0;
            double postChargedPowerReceived = 0;

            if (!chargedParticleMode) // thermal mode
            {
                if (attachedPowerSource.ChargedPowerRatio < 1)
                {
                    postThermalPowerReceived += ConsumeFnResourcePerSecond(_requestedPostThermalPower, ResourceSettings.Config.ThermalPowerInMegawatt);
                }

                var powerReceived = _thermalPowerReceived + _chargedPowerReceived + postThermalPowerReceived;

                // Collect charged power when needed
                if (attachedPowerSource.ChargedPowerRatio >= 1)
                {
                    postChargedPowerReceived += ConsumeFnResourcePerSecond(_requestedPostReactorPower, ResourceSettings.Config.ChargedParticleInMegawatt);
                }
                else if (_shouldUseChargedPower && powerReceived < _reactorPowerRequested)
                {
                    var postPowerRequest = Math.Min(Math.Min(_requestedPostReactorPower - powerReceived, _maxChargedPowerForThermalGenerator), Math.Max(0, _maxReactorPower - powerReceived));

                    postChargedPowerReceived += ConsumeFnResourcePerSecond(postPowerRequest, ResourceSettings.Config.ChargedParticleInMegawatt);
                }
            }
            else // charged power mode
            {
                postChargedPowerReceived += ConsumeFnResourcePerSecond(_requestedPostChargedPower, ResourceSettings.Config.ChargedParticleInMegawatt);
            }

            var postEffectiveInputPowerPerSecond = Math.Max(0, Math.Min((postThermalPowerReceived + postChargedPowerReceived) * _totalEfficiency, maximumElectricPower - _electricPowerPerSecond));

            if (!CheatOptions.IgnoreMaxTemperature)
                ConsumeFnResourcePerSecond(postEffectiveInputPowerPerSecond, ResourceSettings.Config.WasteHeatInMegawatt);

            SupplyManagedFnResourcePerSecond(postEffectiveInputPowerPerSecond, ResourceSettings.Config.ElectricPowerInMegawatt);
        }

        private void UpdateBuffers()
        {
            if (!maintainsMegaWattPowerBuffer)
                return;

            if (controlWasteHeatBuffer)
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);

            if (_maxStableMegaWattPower > 0)
            {
                _powerState = PowerStates.PowerOnline;

                var stablePowerForBuffer = chargedParticleMode
                       ? attachedPowerSource.ChargedPowerRatio * _maxStableMegaWattPower
                       : _appliesBalance ? (1 - attachedPowerSource.ChargedPowerRatio) * _maxStableMegaWattPower : _maxStableMegaWattPower;

                _megawattBufferAmount = (minimumBufferSize * 50) + (attachedPowerSource.PowerBufferBonus + 1) * stablePowerForBuffer;
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, _megawattBufferAmount);
                if (!Kerbalism.IsLoaded)
                    _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, _megawattBufferAmount);
            }

            _resourceBuffers.UpdateBuffers();
        }

        private double CalculateElectricalPowerCurrentlyNeeded()
        {
            if (isLimitedByMinThrottle)
                return attachedPowerSource.MinimumPower;

            var currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));

            _powerDemandQueue.Enqueue(currentUnfilledResourceDemand);
            if (_powerDemandQueue.Count > 4)
                _powerDemandQueue.Dequeue();

            var spareResourceCapacity = GetSpareResourceCapacity(ResourceSettings.Config.ElectricPowerInMegawatt);
            _maxStableMegaWattPower = MaxStableMegaWattPower;

            var possibleSpareResourceCapacityFilling = Math.Min(spareResourceCapacity, _maxStableMegaWattPower);

            return Math.Min(maximumElectricPower, Math.Min(1, PowerControlRatio) * _powerDemandQueue.Max() + Math.Max(0, PowerControlRatio - 1) * possibleSpareResourceCapacityFilling);
        }

        private void PowerDown()
        {
            if (_powerState != PowerStates.PowerOffline)
            {
                if (_powerDownFraction > 0)
                    _powerDownFraction -= 0.01;

                if (_powerDownFraction <= 0)
                    _powerState = PowerStates.PowerOffline;

                _megawattBufferAmount = minimumBufferSize * 50 + (attachedPowerSource.PowerBufferBonus + 1) * _maxStableMegaWattPower * _powerDownFraction;
            }
            else
                _megawattBufferAmount = minimumBufferSize * 50;

            if (controlWasteHeatBuffer)
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);

            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, _megawattBufferAmount);
            if (!Kerbalism.IsLoaded)
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, _megawattBufferAmount);
            _resourceBuffers.UpdateBuffers();
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append("<color=#7fdfffff>").Append(Localizer.Format("#LOC_KSPIE_Generator_upgradeTechnologies")).AppendLine("</color>");
            sb.Append("<size=10>");

            if (!string.IsNullOrEmpty(Mk2TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk2TechReq)));
            if (!string.IsNullOrEmpty(Mk3TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk3TechReq)));
            if (!string.IsNullOrEmpty(Mk4TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk4TechReq)));
            if (!string.IsNullOrEmpty(Mk5TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk5TechReq)));
            if (!string.IsNullOrEmpty(Mk6TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk6TechReq)));
            if (!string.IsNullOrEmpty(Mk7TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk7TechReq)));
            if (!string.IsNullOrEmpty(Mk8TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk8TechReq)));
            if (!string.IsNullOrEmpty(Mk9TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk9TechReq)));

            sb.Append("</size><color=#7fdfffff>").Append(Localizer.Format("#LOC_KSPIE_Generator_conversionEfficiency")).AppendLine("</color>");
            sb.Append("<size=10>Mk1: ").AppendLine(efficiencyMk1.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk2TechReq))
                sb.Append("Mk2: ").AppendLine(efficiencyMk2.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk3TechReq))
                sb.Append("Mk3: ").AppendLine(efficiencyMk3.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk4TechReq))
                sb.Append("Mk4: ").AppendLine(efficiencyMk4.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk5TechReq))
                sb.Append("Mk5: ").AppendLine(efficiencyMk5.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk6TechReq))
                sb.Append("Mk6: ").AppendLine(efficiencyMk6.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk7TechReq))
                sb.Append("Mk7: ").AppendLine(efficiencyMk7.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk8TechReq))
                sb.Append("Mk8: ").AppendLine(efficiencyMk8.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk9TechReq))
                sb.Append("Mk9: ").AppendLine(efficiencyMk9.ToString("P0"));
            sb.Append("</size>");

            return sb.ToStringAndRelease();
        }

        public override string getResourceManagerDisplayName()
        {
            if (isLimitedByMinThrottle)
                return base.getResourceManagerDisplayName();

            var displayName = part.partInfo.title + " " + Localizer.Format("#LOC_KSPIE_Generator_partdisplay");//(generator)

            if (similarParts == null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            if (similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }

        public override int getPowerPriority()
        {
            return 0;
        }

        public override int GetSupplyPriority()
        {
            if (isLimitedByMinThrottle)
                return 1;

            return attachedPowerSource?.ProviderPowerPriority ?? base.getPowerPriority();
        }
    }
}

