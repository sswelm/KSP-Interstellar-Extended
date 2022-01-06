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
        [KSPField(isPersistant = true)] public bool hasAdjustPowerPercentage;   // ToDo remove in the future

        // Settings
        [KSPField] public string animName = "";
        [KSPField] public string upgradeTechReq = "";
        [KSPField] public string upgradeCostStr = "";

        [KSPField] public bool useResourceBuffers = true;
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
        public float powerPercentage = 100;
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
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F3", guiActive = true)]
        public double electrical_power_currently_needed;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_InitialGeneratorPowerEC", guiUnits = " kW", guiFormat = "F0")]
        public double initialGeneratorPowerEC;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "Maximum Generator Power", guiUnits = " Mw", guiFormat = "F3")]
        public double maximumGeneratorPowerMJ;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "Received Power Per Second", guiUnits = " Mw")]
        public double received_power_per_second;

        // privates

        private bool _shouldUseChargedPower;
        private bool _playPowerDownSound = true;
        private bool _playPowerUpSound = true;
        private bool _hasRequiredUpgrade;
        private bool _checkedConnectivity;

        private double _targetMass;
        private double _initialMass;
        private double _chargedPowerReceived;
        private double _maxReactorPower;
        private double _megawattBufferAmount;
        private double _hotColdBathRatio;
        private double _heatExchangerThrustDivisor = 1;
        private double _requestedPostReactorPower;
        private double _electricPowerPerSecond;
        private double _outputPower;
        private double _powerDownFraction;

        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public bool _appliesBalance;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _requestedPostThermalPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _postEffectiveInputPowerPerSecond;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maxElectricPowerPerSecond;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _totalEfficiency;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _powerNeededForCurrentEfficiency;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _requestedChargedPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maximumChargedPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _chargedPowerRequestRatio;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _megajoulesFraction;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _reactorPowerRequested;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maxThermalPower;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _effectiveMaximumThermalPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maxStableMegaWattPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maxChargedPowerForThermalGenerator;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maxChargedPowerForChargedGenerator;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maxAllowedChargedPower;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _thermalPowerPlanned;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _effectiveMaxThermalPowerRatio;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _requestedThermalPower;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _thermalPowerReceived;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _thermalPowerRequestRatio;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _secondThermalRequest;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _requestedPostChargedPower;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maxUnfilledDemand;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _possibleSpareCapacity;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _maximumElectricPower;
        [KSPField(advancedTweakable = true, guiActive = true, groupName = "Debug", groupDisplayName = "Debug", groupStartCollapsed = true)] public double _attachedMaxThermalUsageRatio;

        private Animation _animation;
        private PowerStates _powerState;
        private IFNPowerSource _attachedPowerSource;
        private ResourceBuffers _resourceBuffers;
        private ModuleGenerator _stockModuleGenerator;
        private MethodInfo _powerGeneratorReliablityEvent;
        private ModuleResource _outputModuleResource;
        private PartModule _powerGeneratorProcessController;
        private BaseField _powerGeneratorCapacity;
        private BaseField _moduleGeneratorEfficientBaseField;
        private BaseEvent _moduleGeneratorShutdownBaseEvent;
        private BaseEvent _moduleGeneratorActivateBaseEvent;

        private readonly Queue<double> _powerDemandQueue = new Queue<double>();

        public IFNPowerSource AttachedPowerSource => _attachedPowerSource;

        public string UpgradeTechnology => upgradeTechReq;

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "Reset", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]
        public void Reset()
        {
            _powerGeneratorCapacity?.SetValue(0, _powerGeneratorProcessController);

            _powerGeneratorReliablityEvent?.Invoke(_powerGeneratorProcessController, new object[] { false });
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
                if (_attachedPowerSource == null)
                    return;

                Debug.Log("[KSPI]: detach " + part.partInfo.title);
                if (chargedParticleMode && _attachedPowerSource.ConnectedChargedParticleElectricGenerator != null)
                    _attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                if (!chargedParticleMode && _attachedPowerSource.ConnectedThermalElectricGenerator != null)
                    _attachedPowerSource.ConnectedThermalElectricGenerator = null;
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

            resourcesToSupply = new[] { ResourceSettings.Config.ElectricPowerInMegawatt, ResourceSettings.Config.WasteHeatInMegawatt, ResourceSettings.Config.ThermalPowerInMegawatt, ResourceSettings.Config.ChargedPowerInMegawatt };

            if (useResourceBuffers)
            {
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
            }

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

            AdjustPowerPercentage();

            UpdateHeatExchangedThrustDivisor();
        }

        private void AdjustPowerPercentage()
        {
            // adjust power percentage to for recharging to buffer
            if (hasAdjustPowerPercentage) return;

            if (_attachedPowerSource == null) return;

            if ( Math.Abs(powerPercentage - 100) < 0.1)
                powerPercentage = _attachedPowerSource.DefaultPowerGeneratorPercentage;

            hasAdjustPowerPercentage = true;
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
                _powerGeneratorProcessController = partModule;
                var type = _powerGeneratorProcessController.GetType();
                _powerGeneratorReliablityEvent =  type.GetMethod("ReliablityEvent");
                _powerGeneratorCapacity = _powerGeneratorProcessController.Fields["capacity"];
                break;
            }

            _stockModuleGenerator = part.FindModuleImplementing<ModuleGenerator>();

            if (_stockModuleGenerator == null) return;

            _outputModuleResource = _stockModuleGenerator.resHandler.outputResources.FirstOrDefault(m => m.name == ResourceSettings.Config.ElectricPowerInKilowatt);

            if (_outputModuleResource == null) return;

            _moduleGeneratorShutdownBaseEvent = _stockModuleGenerator.Events[nameof(ModuleGenerator.Shutdown)];
            if (_moduleGeneratorShutdownBaseEvent != null)
            {
                _moduleGeneratorShutdownBaseEvent.guiActive = false;
                _moduleGeneratorShutdownBaseEvent.guiActiveEditor = false;
            }

            _moduleGeneratorActivateBaseEvent = _stockModuleGenerator.Events[nameof(ModuleGenerator.Activate)];
            if (_moduleGeneratorActivateBaseEvent != null)
            {
                _moduleGeneratorActivateBaseEvent.guiActive = false;
                _moduleGeneratorActivateBaseEvent.guiActiveEditor = false;
            }

            _moduleGeneratorEfficientBaseField = _stockModuleGenerator.Fields[nameof(ModuleGenerator.efficiency)];
            if (_moduleGeneratorEfficientBaseField != null)
            {
                _moduleGeneratorEfficientBaseField.guiActive = false;
                _moduleGeneratorEfficientBaseField.guiActiveEditor = false;
            }

            initialGeneratorPowerEC = _outputModuleResource.rate;
            maximumGeneratorPowerEC = _outputModuleResource.rate;
            maximumGeneratorPowerMJ = maximumGeneratorPowerEC / GameConstants.ecPerMJ;
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
            if (_attachedPowerSource != null)
            {
                if (chargedParticleMode)
                    _attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                else
                    _attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }

            // first look if part contains an thermal source
            _attachedPowerSource = part.FindModulesImplementing<IFNPowerSource>().FirstOrDefault();
            if (_attachedPowerSource != null)
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
            _attachedPowerSource = searchResult.Source;

            Debug.Log("[KSPI]: successfully connected to " + _attachedPowerSource.Part.partInfo.title);

            ConnectToPowerSource();
        }

        private void ConnectToPowerSource()
        {
            //connect with source
            if (chargedParticleMode)
                _attachedPowerSource.ConnectedChargedParticleElectricGenerator = this;
            else
                _attachedPowerSource.ConnectedThermalElectricGenerator = this;

            UpdateTargetMass();

            UpdateHeatExchangedThrustDivisor();

            UpdateModuleGeneratorOutput();
        }

        private void UpdateModuleGeneratorOutput()
        {
            if (_attachedPowerSource == null)
                return;

            var maximumPower = isLimitedByMinThrottle ? _attachedPowerSource.MinimumPower : _attachedPowerSource.MaximumPower;
            maximumGeneratorPowerMJ = maximumPower * maxEfficiency * _heatExchangerThrustDivisor;

            var generatorRate = maximumGeneratorPowerMJ * GameConstants.ecPerMJ;

            if (_outputModuleResource != null)
                _outputModuleResource.rate = maximumGeneratorPowerMJ * GameConstants.ecPerMJ;
            else
            {
                if (HighLogic.LoadedSceneIsFlight)
                    _powerGeneratorCapacity?.SetValue(0, _powerGeneratorProcessController);
                else
                    _powerGeneratorCapacity?.SetValue(generatorRate, _powerGeneratorProcessController);
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
                if (_attachedPowerSource == null)
                {
                    _targetMass = _initialMass;
                    return;
                }

                if (chargedParticleMode && _attachedPowerSource.ChargedParticleEnergyEfficiency > 0)
                    powerUsageEfficiency = _attachedPowerSource.ChargedParticleEnergyEfficiency;
                else if (isMHD && _attachedPowerSource.PlasmaEnergyEfficiency > 0)
                    powerUsageEfficiency = _attachedPowerSource.PlasmaEnergyEfficiency;
                else if (_attachedPowerSource.ThermalEnergyEfficiency > 0)
                    powerUsageEfficiency = _attachedPowerSource.ThermalEnergyEfficiency;
                else
                    powerUsageEfficiency = 1;

                var rawMaximumPower = _attachedPowerSource.RawMaximumPowerForPowerGeneration * powerUsageEfficiency;
                maximumTheoreticalPower = PluginHelper.GetFormattedPowerString(rawMaximumPower * CapacityRatio * maxEfficiency);

                // verify if mass calculation is active
                if (!calculatedMass)
                {
                    _targetMass = _initialMass;
                    return;
                }

                // update part mass
                if (rawMaximumPower > 0 && rawPowerToMassDivider > 0)
                    _targetMass = massModifier * _attachedPowerSource.ThermalProcessingModifier * rawMaximumPower * Math.Pow(CapacityRatio, capacityToMassExponent) / rawPowerToMassDivider;
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
            if (_attachedPowerSource == null)
                return -1;

            var coreTemperature = _attachedPowerSource.GetCoreTempAtRadiatorTemp(coldBathTemperature);

            var plasmaTemperature = coreTemperature <= _attachedPowerSource.HotBathTemperature
                ? coreTemperature
                : _attachedPowerSource.HotBathTemperature + Math.Pow(coreTemperature - _attachedPowerSource.HotBathTemperature, coreTemperateHotBathExponent);

            double temperature;
            if (!isMHD)
                temperature = _attachedPowerSource.HotBathTemperature;
            else
            {
                if (_attachedPowerSource.SupportMHD)
                    temperature = plasmaTemperature;
                else
                {
                    var chargedPowerModifier = _attachedPowerSource.ChargedPowerRatio * _attachedPowerSource.ChargedPowerRatio;
                    temperature = plasmaTemperature * chargedPowerModifier + (1 - chargedPowerModifier) * _attachedPowerSource.HotBathTemperature; // for fusion reactors connected to MHD
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

            if (_attachedPowerSource == null && _checkedConnectivity == false)
            {
                _checkedConnectivity = true;
                var message = "Warning: " + part.partInfo.title + " is not connected to a power source!";
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            if (ResearchAndDevelopment.Instance != null)
            {
                Events[nameof(RetrofitGenerator)].active = !isUpgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasRequiredUpgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
            }
            else
                Events[nameof(RetrofitGenerator)].active = false;

            if (_playPowerUpSound && _animation != null)
            {
                _playPowerDownSound = IsEnabled;
                _playPowerUpSound = !IsEnabled;
                _animation[animName].speed = IsEnabled ? 1 : -1;
                _animation[animName].normalizedTime = IsEnabled ? 0 : 1;
                _animation.Blend(animName, 2);
            }

            if (IsEnabled)
            {
                OutputPower = PluginHelper.GetFormattedPowerString(-_outputPower);
                overallEfficiencyStr = (_totalEfficiency * 100).ToString("0.00") + "%";

                _maximumElectricPower = _totalEfficiency >= 0
                    ? !chargedParticleMode
                        ? _totalEfficiency * Math.Max(_maxChargedPowerForThermalGenerator, _maxThermalPower)
                        : _totalEfficiency * _maxChargedPowerForChargedGenerator
                    : 0;

                MaxPowerStr = PluginHelper.GetFormattedPowerString(_maximumElectricPower);
            }
            else
            {
                OutputPower = Localizer.Format("#LOC_KSPIE_Generator_Offline"); //"Generator Offline"
                _maximumElectricPower = 0;
            }

            if (_moduleGeneratorEfficientBaseField != null)
            {
                _moduleGeneratorEfficientBaseField.guiActive = false;
                _moduleGeneratorEfficientBaseField.guiActiveEditor = false;
            }
        }

        public double RawGeneratorSourcePower
        {
            get
            {
                if (_attachedPowerSource == null || !IsEnabled)
                    return 0;

                var maxPowerUsageRatio =
                    chargedParticleMode
                        ? _attachedPowerSource.ChargedParticleEnergyEfficiency
                        : isMHD
                            ? _attachedPowerSource.PlasmaEnergyEfficiency
                            : _attachedPowerSource.ThermalEnergyEfficiency;

                var rawMaxPower = isLimitedByMinThrottle
                    ? _attachedPowerSource.MinimumPower
                    : HighLogic.LoadedSceneIsEditor
                        ? _attachedPowerSource.MaximumPower
                        : _attachedPowerSource.StableMaximumReactorPower;

                return rawMaxPower * _attachedPowerSource.PowerRatio * maxPowerUsageRatio * CapacityRatio;
            }
        }

        public double MaxEfficiency => maxEfficiency;

        public double MaxStableMegaWattPower => RawGeneratorSourcePower * maxEfficiency ;

        private void UpdateHeatExchangedThrustDivisor()
        {
            if (_attachedPowerSource == null || _attachedPowerSource.Radius <= 0 || radius <= 0)
            {
                _heatExchangerThrustDivisor = 1;
                return;
            }

            _heatExchangerThrustDivisor = radius > _attachedPowerSource.Radius
                ? _attachedPowerSource.Radius * _attachedPowerSource.Radius / radius / radius
                : radius * radius / _attachedPowerSource.Radius / _attachedPowerSource.Radius;
        }

        private void UpdateGeneratorPower()
        {
            if (_attachedPowerSource == null) return;

            if (!chargedParticleMode) // thermal or plasma mode
            {
                coldBathTempDisplay = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel);

                hotBathTemp = GetHotBathTemperature(coldBathTempDisplay);

                coldBathTemp = coldBathTempDisplay * 0.75;
            }

            if (HighLogic.LoadedSceneIsEditor)
                UpdateHeatExchangedThrustDivisor();

            var attachedPowerSourceRatio = _attachedPowerSource.PowerRatio;

            var rawThermalPower = isLimitedByMinThrottle
                ? _attachedPowerSource.MinimumPower
                : _attachedPowerSource.MaximumThermalPower * Math.Min(1, PowerControlRatio) * CapacityRatio;

            var rawChargedPower = _attachedPowerSource.MaximumChargedPower * Math.Min(1, PowerControlRatio) * CapacityRatio;
            var rawReactorPower = rawThermalPower + rawChargedPower;

            if (!(attachedPowerSourceRatio > 0))
            {
                _maxChargedPowerForThermalGenerator = rawChargedPower;
                _maxThermalPower = rawThermalPower;
                _maxReactorPower = rawReactorPower;
                return;
            }

            _attachedMaxThermalUsageRatio = isMHD
                ? _attachedPowerSource.PlasmaEnergyEfficiency
                : _attachedPowerSource.ThermalEnergyEfficiency;

            //var potentialThermalPower = (isMHD || !_appliesBalance ? rawReactorPower: rawThermalPower) / attachedPowerSourceRatio;
            //_maxAllowedChargedPower = rawChargedPower * (chargedParticleMode ? _attachedPowerSource.ChargedPowerRatio : 1);
            _maxAllowedChargedPower = rawChargedPower;

            _maxThermalPower = _attachedMaxThermalUsageRatio * Math.Min(rawReactorPower, rawReactorPower / attachedPowerSourceRatio);
            _maxChargedPowerForThermalGenerator = _attachedMaxThermalUsageRatio * Math.Min(rawChargedPower, (1 / attachedPowerSourceRatio) * _maxAllowedChargedPower);
            _maxChargedPowerForChargedGenerator = _attachedPowerSource.ChargedParticleEnergyEfficiency * Math.Min(rawChargedPower, (1 / attachedPowerSourceRatio) * _maxAllowedChargedPower);

            _maxReactorPower = chargedParticleMode || (!_attachedPowerSource.IsConnectedToChargedGenerator && _maxThermalPower == 0)
                ? _maxChargedPowerForChargedGenerator
                : _maxThermalPower;
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
            if (IsEnabled && _attachedPowerSource != null && FNRadiator.HasRadiatorsForVessel(vessel))
            {
                _appliesBalance = _attachedPowerSource.ShouldApplyBalance(chargedParticleMode ? ElectricGeneratorType.charged_particle : ElectricGeneratorType.thermal);

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

                if (_stockModuleGenerator != null)
                    _stockModuleGenerator.generatorIsActive = _maxStableMegaWattPower > 0;

                _powerDownFraction = 1;

                var wasteheatRatio = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel) / 4500;
                var overheatingModifier = wasteheatRatio < 0.9 ? 1 : (1 - wasteheatRatio) * 10;

                var thermalPowerRatio = 1 - _attachedPowerSource.ChargedPowerRatio;
                var chargedPowerRatio = _attachedPowerSource.ChargedPowerRatio;



                if (!chargedParticleMode) // thermal mode
                {
                    _hotColdBathRatio = Math.Max(Math.Min(1 - coldBathTemp / hotBathTemp, 1), 0);

                    _totalEfficiency = Math.Min(maxEfficiency, _hotColdBathRatio * maxEfficiency);

                    if (_hotColdBathRatio <= 0.01 || coldBathTemp <= 0 || hotBathTemp <= 0 || _maxReactorPower <= 0) return;

                    electrical_power_currently_needed = CalculateElectricalPowerCurrentlyNeeded();

                    _powerNeededForCurrentEfficiency = electrical_power_currently_needed / _totalEfficiency;

                    _reactorPowerRequested = Math.Max(0, Math.Min(_maxReactorPower, _powerNeededForCurrentEfficiency));
                    _requestedPostReactorPower = Math.Max(0, _attachedPowerSource.MinimumPower - _reactorPowerRequested);

                    _effectiveMaxThermalPowerRatio = _attachedPowerSource.IsConnectedToChargedGenerator
                        ? thermalPowerRatio
                        : thermalPowerRatio + (1 - thermalPowerRatio) * (1 - Math.Max(_attachedPowerSource.RequestedChargedThrottle, _attachedPowerSource.RequestedPlasmaThrottle));

                    _thermalPowerPlanned = thermalPowerRatio * Math.Max(0, Math.Min(_maxThermalPower * _effectiveMaxThermalPowerRatio, _powerNeededForCurrentEfficiency));

                    _requestedPostThermalPower = Math.Max(0, _attachedPowerSource.MinimumPower * thermalPowerRatio - _thermalPowerPlanned);

                    if ( chargedPowerRatio < 1)
                    {
                        _effectiveMaximumThermalPower = _attachedPowerSource.MaximumThermalPower * Math.Min(1, PowerControlRatio) * CapacityRatio * _attachedMaxThermalUsageRatio;
                        _requestedThermalPower = Math.Min(_thermalPowerPlanned, _effectiveMaximumThermalPower);
                        _thermalPowerReceived = ConsumeFnResourcePerSecond(_requestedThermalPower, ResourceSettings.Config.ThermalPowerInMegawatt);
                        _thermalPowerRequestRatio = Math.Min(1, _effectiveMaximumThermalPower > 0 ? _requestedThermalPower / _effectiveMaximumThermalPower : 0);
                        _attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEfficiency, _thermalPowerRequestRatio, isMHD, isLimitedByMinThrottle ? part.mass * 0.05 : part.mass);
                    }
                    else
                        _thermalPowerReceived = 0;

                    var totalPowerReceived = _thermalPowerReceived;

                    _shouldUseChargedPower = chargedPowerRatio > 0;

                    // Collect charged power when needed
                    if (chargedPowerRatio >= 1)
                    {
                        _requestedChargedPower = _reactorPowerRequested;
                        _requestedChargedPower *= 1 - Math.Max(_attachedPowerSource.CurrentChargedPropulsionRatio, _attachedPowerSource.CurrentPlasmaPropulsionRatio);

                        _chargedPowerReceived = ConsumeFnResourcePerSecond(_requestedChargedPower, ResourceSettings.Config.ChargedPowerInMegawatt);

                        var maximumChargedPower = _attachedPowerSource.MaximumChargedPower * powerUsageEfficiency * CapacityRatio;
                        var chargedPowerRequestRatio = Math.Min(1, maximumChargedPower > 0 ? _requestedChargedPower / maximumChargedPower : 0);

                        _attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEfficiency, chargedPowerRequestRatio, isMHD, isLimitedByMinThrottle ? part.mass * 0.05 : part.mass);
                    }
                    else if (_shouldUseChargedPower && _thermalPowerReceived < _reactorPowerRequested)
                    {
                        _requestedChargedPower = Math.Min(Math.Min(_reactorPowerRequested - _thermalPowerReceived, _maxChargedPowerForThermalGenerator), Math.Max(0, _maxReactorPower - _thermalPowerReceived));
                        _requestedChargedPower *= 1 - Math.Max(_attachedPowerSource.CurrentChargedPropulsionRatio, _attachedPowerSource.CurrentPlasmaPropulsionRatio);

                        _chargedPowerReceived = ConsumeFnResourcePerSecond(_requestedChargedPower, ResourceSettings.Config.ChargedPowerInMegawatt);
                    }
                    else
                    {
                        ConsumeFnResourcePerSecond(0, ResourceSettings.Config.ChargedPowerInMegawatt);
                        _chargedPowerReceived = 0;
                        _requestedChargedPower = 0;
                    }

                    totalPowerReceived += _chargedPowerReceived;

                    // any shortage should be consumed again from remaining thermalPower
                    if (_shouldUseChargedPower && chargedPowerRatio < 1 && totalPowerReceived < _reactorPowerRequested)
                    {
                        _secondThermalRequest = Math.Max(0, _thermalPowerPlanned - totalPowerReceived);
                        _thermalPowerReceived += ConsumeFnResourcePerSecond(_secondThermalRequest, ResourceSettings.Config.ThermalPowerInMegawatt);
                    }

                    if (!CheatOptions.IgnoreMaxTemperature)
                    {
                        ConsumeFnResourcePerSecond(_thermalPowerReceived * _totalEfficiency, ResourceSettings.Config.WasteHeatInMegawatt);
                        SupplyFnResourcePerSecond(_chargedPowerReceived * (1 - _totalEfficiency), ResourceSettings.Config.WasteHeatInMegawatt);
                    }
                }
                else // charged particle mode
                {
                    _hotColdBathRatio = 1;

                    _totalEfficiency = maxEfficiency;

                    if (_totalEfficiency <= 0) return;

                    electrical_power_currently_needed = CalculateElectricalPowerCurrentlyNeeded();

                    _megajoulesFraction = GetResourceBarFraction(ResourceSettings.Config.ElectricPowerInMegawatt);

                    _powerNeededForCurrentEfficiency = electrical_power_currently_needed / _totalEfficiency;

                    _requestedChargedPower = overheatingModifier * Math.Max(0, Math.Min(_maxChargedPowerForChargedGenerator, _powerNeededForCurrentEfficiency));
                    _requestedPostChargedPower = overheatingModifier * Math.Max(0, (_attachedPowerSource.MinimumPower * chargedPowerRatio) - _requestedChargedPower);

                    _maximumChargedPower = _attachedPowerSource.MaximumChargedPower * _attachedPowerSource.ChargedParticleEnergyEfficiency;
                    _chargedPowerRequestRatio = Math.Min(1, _maximumChargedPower > 0 ? _requestedChargedPower / _maximumChargedPower : 0);

                    _attachedPowerSource.NotifyActiveChargedEnergyGenerator(_totalEfficiency, Math.Min(1, _chargedPowerRequestRatio * (2.1 - _megajoulesFraction)), part.mass);

                    _chargedPowerReceived = ConsumeFnResourcePerSecond(_requestedChargedPower, ResourceSettings.Config.ChargedPowerInMegawatt);

                    if (!CheatOptions.IgnoreMaxTemperature)
                    {
                        var wasteheatFactor = 1 - _totalEfficiency;
                        SupplyFnResourcePerSecond(_chargedPowerReceived * (1 - _totalEfficiency), ResourceSettings.Config.WasteHeatInMegawatt);
                    }
                }

                received_power_per_second = _thermalPowerReceived + _chargedPowerReceived;
                var effectiveInputPowerPerSecond = received_power_per_second * _totalEfficiency;

                _electricPowerPerSecond = Math.Max(effectiveInputPowerPerSecond * powerOutputMultiplier, 0);
                if (!chargedParticleMode)
                {
                    //var effectiveMaxThermalPowerRatio = (!_attachedPowerSource.IsConnectedToChargedGenerator && _maxThermalPower == 0) || isMHD || !_appliesBalance ? 1 : thermalPowerRatio;

                    _maxElectricPowerPerSecond = _effectiveMaxThermalPowerRatio * _attachedPowerSource.StableMaximumReactorPower * _attachedPowerSource.PowerRatio * powerUsageEfficiency * _totalEfficiency * CapacityRatio;
                }
                else
                    _maxElectricPowerPerSecond = overheatingModifier * _maxChargedPowerForChargedGenerator * _totalEfficiency;

                var availableGeneratorRate = Math.Max(0, maximumGeneratorPowerMJ - _electricPowerPerSecond) * GameConstants.ecPerMJ;

                _attachedPowerSource.UpdateAuxiliaryPowerSource(availableGeneratorRate);

                var requiredElectricCharge = (GetRequiredElectricCharge() * GameConstants.ecPerMJ);

                if (_outputModuleResource != null)
                    _outputModuleResource.rate = requiredElectricCharge;

                _outputPower = isLimitedByMinThrottle
                    ? -SupplyManagedFnResourcePerSecond(_electricPowerPerSecond, ResourceSettings.Config.ElectricPowerInMegawatt)
                    : -SupplyFnResourcePerSecondWithMaxAndEfficiency(_electricPowerPerSecond, _maxElectricPowerPerSecond, _hotColdBathRatio, ResourceSettings.Config.ElectricPowerInMegawatt);
            }
            else
            {
                _electricPowerPerSecond = 0;
                _maxElectricPowerPerSecond = 0;
                generatorInit = true;
                SupplyManagedFnResourcePerSecond(0, ResourceSettings.Config.ElectricPowerInMegawatt);

                if (_stockModuleGenerator != null && _stockModuleGenerator.generatorIsActive)
                    _stockModuleGenerator.Shutdown();

                if (IsEnabled && !vessel.packed)
                {
                    if (_attachedPowerSource == null)
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
            if (_attachedPowerSource == null) return;

            double postThermalPowerReceived = 0;
            double postChargedPowerReceived = 0;

            if (!chargedParticleMode) // thermal mode
            {
                if (_attachedPowerSource.ChargedPowerRatio < 1)
                    postThermalPowerReceived += ConsumeFnResourcePerSecond(_requestedPostThermalPower, ResourceSettings.Config.ThermalPowerInMegawatt);

                var powerReceived = _thermalPowerReceived + _chargedPowerReceived + postThermalPowerReceived;

                // Collect charged power when needed
                if (_attachedPowerSource.ChargedPowerRatio >= 1)
                {
                    postChargedPowerReceived += ConsumeFnResourcePerSecond(_requestedPostReactorPower, ResourceSettings.Config.ChargedPowerInMegawatt);
                }
                else if (_shouldUseChargedPower && powerReceived < _reactorPowerRequested)
                {
                    var postPowerRequest = Math.Min(Math.Min(_requestedPostReactorPower - powerReceived, _maxChargedPowerForThermalGenerator), Math.Max(0, _maxReactorPower - powerReceived));

                    postChargedPowerReceived += ConsumeFnResourcePerSecond(postPowerRequest, ResourceSettings.Config.ChargedPowerInMegawatt);
                }
            }
            else // charged power mode
            {
                postChargedPowerReceived += ConsumeFnResourcePerSecond(_requestedPostChargedPower, ResourceSettings.Config.ChargedPowerInMegawatt);
            }

            if (!CheatOptions.IgnoreMaxTemperature)
            {
                ConsumeFnResourcePerSecond(postThermalPowerReceived * _totalEfficiency, ResourceSettings.Config.WasteHeatInMegawatt);
                SupplyFnResourcePerSecond(postChargedPowerReceived * (1 - _totalEfficiency), ResourceSettings.Config.WasteHeatInMegawatt);
            }

            _postEffectiveInputPowerPerSecond = Math.Max(0, Math.Min((postThermalPowerReceived + postChargedPowerReceived) * _totalEfficiency, _maximumElectricPower - _electricPowerPerSecond));

            SupplyManagedFnResourcePerSecond(_postEffectiveInputPowerPerSecond, ResourceSettings.Config.ElectricPowerInMegawatt);
        }

        private void UpdateBuffers()
        {
            if (!useResourceBuffers)
                return;

            if (!maintainsMegaWattPowerBuffer)
                return;

            if (controlWasteHeatBuffer)
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);

            if (_maxStableMegaWattPower > 0)
            {
                _powerState = PowerStates.PowerOnline;

                var stablePowerForBuffer = chargedParticleMode
                       ? _attachedPowerSource.ChargedPowerRatio * _maxStableMegaWattPower
                       : _appliesBalance ? (1 - _attachedPowerSource.ChargedPowerRatio) * _maxStableMegaWattPower : _maxStableMegaWattPower;

                _megawattBufferAmount = (minimumBufferSize * 50) + (_attachedPowerSource.PowerBufferBonus + 1) * stablePowerForBuffer;
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, _megawattBufferAmount);
                if (!Kerbalism.IsLoaded)
                    _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, _megawattBufferAmount);
            }

            _resourceBuffers.UpdateBuffers();
        }

        private double CalculateElectricalPowerCurrentlyNeeded()
        {
            if (isLimitedByMinThrottle)
                return _attachedPowerSource.MinimumPower;

            var currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));

            _powerDemandQueue.Enqueue(currentUnfilledResourceDemand);
            if (_powerDemandQueue.Count > 4)
                _powerDemandQueue.Dequeue();

            var spareResourceCapacity = GetSpareResourceCapacity(ResourceSettings.Config.ElectricPowerInMegawatt);
            _maxStableMegaWattPower = MaxStableMegaWattPower;


            //var megajoulesFraction = GetResourceBarFraction(ResourceSettings.Config.ElectricPowerInMegawatt);
            //var possibleSpareResourceCapacityFilling = (1 - megajoulesFraction) * _maxStableMegaWattPower; // Math.Min(spareResourceCapacity, _maxStableMegaWattPower);

            _possibleSpareCapacity = Math.Min(_maxStableMegaWattPower, spareResourceCapacity);

            _maxUnfilledDemand = _powerDemandQueue.Max();

            return Math.Min(_maximumElectricPower, Math.Min(1, PowerControlRatio) * _maxUnfilledDemand + Math.Max(0, PowerControlRatio - 1) * _possibleSpareCapacity);
        }

        private void PowerDown()
        {
            if (_powerState != PowerStates.PowerOffline && _attachedPowerSource != null)
            {
                if (_powerDownFraction > 0)
                    _powerDownFraction -= 0.01;

                if (_powerDownFraction <= 0)
                    _powerState = PowerStates.PowerOffline;

                _megawattBufferAmount = minimumBufferSize * 50 + (_attachedPowerSource.PowerBufferBonus + 1) * _maxStableMegaWattPower * _powerDownFraction;
            }
            else
                _megawattBufferAmount = minimumBufferSize * 50;


            if (useResourceBuffers)
            {
                if (controlWasteHeatBuffer)
                    _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);

                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, _megawattBufferAmount);
                if (!Kerbalism.IsLoaded)
                    _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt,
                        _megawattBufferAmount);
                _resourceBuffers.UpdateBuffers();
            }
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

            return _attachedPowerSource?.ProviderPowerPriority ?? base.getPowerPriority();
        }
    }
}

