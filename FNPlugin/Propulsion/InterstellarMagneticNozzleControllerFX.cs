using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
using FNPlugin.Powermanagement.Interfaces;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    class InterstellarMagneticNozzleControllerFX : ResourceSuppliableModule, IFNEngineNoozle
    {
        public const string Group = "MagneticNozzleController";
        public const string GroupTitle = "#LOC_KSPIE_MagneticNozzleControllerFX_groupName";
        public const float MinimumFlowRate = 1e-10f;

        //Persistent
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_SimulatedThrottle")
         , UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]//Simulated Throttle
        public float simulatedThrottle = 0.5f;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "MHD Power %")
         , UI_FloatRange(stepIncrement = 1f, maxValue = 200, minValue = 0, affectSymCounterparts = UI_Scene.All)]
        public float mhdPowerGenerationPercentage = 101;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Supply Priority")
         , UI_FloatRange(stepIncrement = 1f, maxValue = 4, minValue = 0, affectSymCounterparts = UI_Scene.All)]
        public float supplyPriority = 0;

        [KSPField(isPersistant = true)] double powerBufferStore;
        [KSPField(isPersistant = true)] bool exhaustAllowed = true;

        // public
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_ThermalNozzleController_Radius", guiActiveEditor = true, guiFormat = "F2", guiUnits = "m")]
        public double radius = 2.5;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiActiveEditor = true, guiFormat = "F3", guiUnits = " t")]
        public float partMass = 1;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Minimumisp", guiUnits = " s", guiFormat = "F1")]
        protected double minimum_isp;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Maximumisp", guiUnits = " s", guiFormat = "F1")]
        protected double maximum_isp;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_ThrotleExponent")]
        protected double throtleExponent = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_PowerRatio")]
        protected double megajoulesRatio;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_EngineIsp")]
        protected double engineIsp;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_EngineFuelFlow")]
        protected float engineFuelFlow;

        // advancedTweakables
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_MaximumChargedPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        protected double maximumChargedPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_ChargedParticleMaximumPercentageUsage", guiFormat = "F3")]
        public double _chargedParticleMaximumPercentageUsage;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_MaxChargedParticlesPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double _max_charged_particles_power;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RequestedParticles", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double _charged_particles_requested;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RecievedParticles", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double _charged_particles_received;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RequestedElectricity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double _requestedElectricPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RecievedElectricity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double _recievedElectricPower;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Thrust", guiUnits = " kN")]
        public double _engineMaxThrust;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Consumption", guiUnits = " kg/s")]
        public double calculatedConsumptionPerSecond;

        [KSPField] public bool showPartMass = true;
        [KSPField] public string propellantBufferResourceName = "LqdHydrogen";
        [KSPField] public string runningEffectName = string.Empty;
        [KSPField] public string powerEffectName = string.Empty;
        [KSPField] public float currentThrust;
        [KSPField] public float wasteHeatMultiplier = 1;
        [KSPField] public double powerThrustMultiplier = 1;
        [KSPField] public double minimumPropellantBuffer = 0.01;
        [KSPField] public double wasteHeatBufferMassMult = 2.0e+5;
        [KSPField] public double chargedParticleRatio;
        [KSPField] public double currentIsp;
        [KSPField] public double chargedParticleRatioThreshold = 0.0002;
        [KSPField] public double megajoulesRatioThreshold = 0.02;

        //Internal
        private UI_FloatRange simulatedThrottleFloatRange;
        private ModuleEnginesFX _attachedEngine;
        private ModuleEnginesMagneticNozzle _attachedPersistentEngine;
        private ResourceBuffers resourceBuffers;
        private readonly Guid id = Guid.NewGuid();

        private PartResourceDefinition[] partResourceDefinitions;

        private IFNChargedParticleSource _attachedReactor;

        private bool _checkedConnectivity;
        private int _attachedReactorDistance;
        private double _exchangerThrustDivisor;
        private double _maxPowerMultiplier;
        private double powerBufferMax;
        private double _mhdTrustIspModifier = 1;
        private double _effectiveThrustRatio;
        private double _maxTheoreticalThrust;

        public IFNChargedParticleSource AttachedReactor
        {
            get => _attachedReactor;
            private set
            {
                _attachedReactor = value;
                _attachedReactor?.AttachThermalReciever(id, radius);
            }
        }

        public double GetNozzleFlowRate() { return _attachedEngine.maxFuelFlow; }

        public bool PropellantAbsorbsNeutrons => false;
        public bool IsPlasmaNozzle => false;
        public bool RequiresPlasmaHeat => false;
        public bool RequiresThermalHeat => false;
        public float CurrentThrottle => !_attachedEngine.flameout && _attachedEngine.currentThrottle > 0 ? (maximum_isp == minimum_isp ? _attachedEngine.currentThrottle : 1) : 0;
        public double RequestedThrottle => _attachedEngine.currentThrottle;

        public bool RequiresChargedPower => true;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            _attachedPersistentEngine = part.FindModuleImplementing<ModuleEnginesMagneticNozzle>();
            if (_attachedPersistentEngine != null)
            {
                _attachedPersistentEngine.Fields[nameof(ModuleEnginesMagneticNozzle.thrust_d)].Attribute.groupName = Group;
                _attachedPersistentEngine.Fields[nameof(ModuleEnginesMagneticNozzle.thrust_d)].Attribute.groupDisplayName = GroupTitle;
            }
            else
                Debug.LogError("[KSPI]: InterstellarMagneticNozzleControllerFX - failed to find ModuleEnginesMagneticNozzle during load for " + part.name);

            var myAttachedEngine = part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine != null)
            {
                myAttachedEngine.Fields[nameof(ModuleEngines.status)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.status)].Attribute.groupDisplayName = GroupTitle;

                myAttachedEngine.Fields[nameof(ModuleEngines.realIsp)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.realIsp)].Attribute.groupDisplayName = GroupTitle;

                myAttachedEngine.Fields[nameof(ModuleEngines.finalThrust)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.finalThrust)].Attribute.groupDisplayName = GroupTitle;

                myAttachedEngine.Fields[nameof(ModuleEngines.propellantReqMet)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.propellantReqMet)].Attribute.groupDisplayName = GroupTitle;

                myAttachedEngine.Fields[nameof(ModuleEngines.fuelFlowGui)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.fuelFlowGui)].Attribute.groupDisplayName = GroupTitle;

                myAttachedEngine.Fields[nameof(ModuleEngines.thrustPercentage)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.thrustPercentage)].Attribute.groupDisplayName = GroupTitle;

                myAttachedEngine.Fields[nameof(ModuleEngines.independentThrottle)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.independentThrottle)].Attribute.groupDisplayName = GroupTitle;

                myAttachedEngine.Fields[nameof(ModuleEngines.independentThrottlePercentage)].Attribute.groupName = Group;
                myAttachedEngine.Fields[nameof(ModuleEngines.independentThrottlePercentage)].Attribute.groupDisplayName = GroupTitle;
            }
            else
                Debug.LogError("[KSPI]: InterstellarMagneticNozzleControllerFX - failed to find engine during load for " + part.name);
        }

        public override void OnStart(StartState state)
        {
            resourcesToSupply = new[]{ ResourceSettings.Config.ElectricPowerInMegawatt };

            CreateDefinitionList();

            if (part.FindModuleImplementing<IFNPowerSource>() == null)
            {
                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, wasteHeatBufferMassMult, true));
                resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
                resourceBuffers.Init(part);
            }

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
            }

            _attachedPersistentEngine = part.FindModuleImplementing<ModuleEnginesMagneticNozzle>();

            if (_attachedPersistentEngine == null)
                Debug.LogWarning("[KSPI]: InterstellarMagneticNozzleControllerFX Error " + part.partInfo.title + " failed to find ModuleEnginesMagneticNozzle");

            _attachedEngine = part.FindModuleImplementing<ModuleEnginesFX>();

            if (_attachedEngine != null)
            {
                _attachedEngine.Fields[nameof(ModuleEnginesThermalNozzle.finalThrust)].guiFormat = "F5";

                if (!string.IsNullOrEmpty(runningEffectName))
                    part.Effect(runningEffectName, 0, -1);
                if (!string.IsNullOrEmpty(powerEffectName))
                    part.Effect(powerEffectName, 0, -1);
            }
            else
            {
                Debug.LogError("[KSPI]: InterstellarMagneticNozzleControllerFX Error " + part.partInfo.title + " failed to find ModuleEnginesFX");
            }

            ConnectToReactor();

            UpdateEngineStats(true);

            //InitializesPropellantBuffer();

            Fields[nameof(partMass)].guiActiveEditor = showPartMass;
            Fields[nameof(partMass)].guiActive = showPartMass;

            simulatedThrottleFloatRange = Fields[nameof(simulatedThrottle)].uiControlEditor as UI_FloatRange;
            if (simulatedThrottleFloatRange != null)
                simulatedThrottleFloatRange.onFieldChanged += UpdateFromGUI;
        }

        private void CreateDefinitionList()
        {
            var stringArray = propellantBufferResourceName.Split(',');
            partResourceDefinitions = stringArray.Select(m => PartResourceLibrary.Instance.GetDefinition(m.Trim())).ToArray();
        }

        //private void InitializesPropellantBuffer()
        //{
        //    if (maintainsPropellantBuffer && string.IsNullOrEmpty(propellantBufferResourceName) == false && part.Resources[propellantBufferResourceName] == null)
        //    {
        //        Debug.Log("[KSPI]: Added " + propellantBufferResourceName + " buffer to MagneticNozzle");
        //        var newResourceNode = new ConfigNode("RESOURCE");
        //        newResourceNode.AddValue("name", propellantBufferResourceName);
        //        newResourceNode.AddValue("maxAmount", minimumPropellantBuffer);
        //        newResourceNode.AddValue("amount", minimumPropellantBuffer);

        //        part.AddResource(newResourceNode);
        //    }

        //    var bufferResource = part.Resources[propellantBufferResourceName];
        //    if (maintainsPropellantBuffer && bufferResource != null)
        //        bufferResource.amount = bufferResource.maxAmount;
        //}

        private double CalculateElectricalPowerCurrentlyNeeded(double maximumElectricPower)
        {
            var currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));
            var spareResourceCapacity = GetSpareResourceCapacity(ResourceSettings.Config.ElectricPowerInMegawatt);
            var powerRequestRatio = mhdPowerGenerationPercentage * 0.01;
            return Math.Min(maximumElectricPower, currentUnfilledResourceDemand * Math.Min(1, powerRequestRatio) + spareResourceCapacity * Math.Max(0, powerRequestRatio - 1));
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            Debug.Log("[KSPI]: Attaching " + part.partInfo.title);

            if (!HighLogic.LoadedSceneIsEditor || _attachedEngine == null) return;

            ConnectToReactor();
        }

        /// <summary>
        /// Event handler which is called when part is detached from thermal source
        /// </summary>
        public void OnEditorDetach()
        {
            Debug.Log("[KSPI]: Detaching " + part.partInfo.title);

            _attachedReactor?.DisconnectWithEngine(this);

            _attachedReactor = null;

            UpdateEngineStats(true);
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateEngineStats(true);
        }

        private void ConnectToReactor()
        {
            // first try to look in part
            _attachedReactor = part.FindModuleImplementing<IFNChargedParticleSource>() ?? BreadthFirstSearchForChargedParticleSource(10, 1);

            if (_attachedReactor == null)
                return;

            _attachedReactor.ConnectWithEngine(this);

            UpdateEngineStats(true);
        }

        private IFNChargedParticleSource BreadthFirstSearchForChargedParticleSource(int stackdepth, int parentdepth)
        {
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                IFNChargedParticleSource particleSource = FindChargedParticleSource(part, currentDepth, parentdepth);

                if (particleSource == null) continue;

                _attachedReactorDistance = currentDepth;
                return particleSource;
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
            if (_attachedReactor == null || _attachedEngine == null)
            {
                minimum_isp = 0;
                maximum_isp = 0;
                _engineMaxThrust = 0;
                powerBufferMax = 0;
                maximumChargedPower = 0;
                return;
            }

            // set Isp
            var joulesPerAmu = _attachedReactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / GameConstants.dilution_factor;
            var calculatedIsp = Math.Sqrt(joulesPerAmu * 2 / GameConstants.ATOMIC_MASS_UNIT) / PhysicsGlobals.GravitationalAcceleration;

            // calculate max and min isp
            minimum_isp = calculatedIsp * _attachedReactor.MinimumChargdIspMult;
            maximum_isp = calculatedIsp * _attachedReactor.MaximumChargedIspMult;

            _maxPowerMultiplier = Math.Log10(maximum_isp / minimum_isp);

            if (!useThrustCurve) return;

            throtleExponent = Math.Abs(Math.Log10(_attachedReactor.MinimumChargdIspMult / _attachedReactor.MaximumChargedIspMult));

            var isp = Math.Min(maximum_isp, minimum_isp / Math.Pow(simulatedThrottle / 100, throtleExponent));

            FloatCurve newAtmosphereCurve = new FloatCurve();
            newAtmosphereCurve.Add(0, (float)isp);
            newAtmosphereCurve.Add(0.002f, 0);
            _attachedEngine.atmosphereCurve = newAtmosphereCurve;

            // set maximum fuel flow
            var powerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
            maximumChargedPower = _attachedReactor.MaximumChargedPower;
            powerBufferMax = maximumChargedPower / 10000;

            _engineMaxThrust = powerThrustModifier * maximumChargedPower / isp / PhysicsGlobals.GravitationalAcceleration;
            var maxFuelFlowRate = _engineMaxThrust / isp / PhysicsGlobals.GravitationalAcceleration;
            _attachedEngine.maxFuelFlow = (float)maxFuelFlowRate;
            _attachedEngine.maxThrust = (float)_engineMaxThrust;

            FloatCurve newThrustCurve = new FloatCurve();
            newThrustCurve.Add(0, (float)_engineMaxThrust);
            newThrustCurve.Add(0.001f, 0);

            _attachedEngine.thrustCurve = newThrustCurve;
            _attachedEngine.useThrustCurve = true;
        }

        public virtual void Update()
        {
            partMass = part.mass;

            UpdateEngineStats(!HighLogic.LoadedSceneIsFlight);
        }

        // FixedUpdate is also called in the Editor
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            UpdateRunningEffect();
            UpdatePowerEffect();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (_attachedEngine == null)
                return;

            if (_attachedEngine.currentThrottle > 0 && !exhaustAllowed)
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

            _chargedParticleMaximumPercentageUsage = _attachedReactor?.ChargedParticlePropulsionEfficiency ?? 0;

            if (_attachedReactor != null && _chargedParticleMaximumPercentageUsage > 0)
            {
                if (_attachedReactor.Part != part && resourceBuffers != null)
                {
                    resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
                    resourceBuffers.UpdateBuffers();
                }

                var currentThrottle = Math.Max(vessel.ctrlState.mainThrottle, _attachedEngine.currentThrottle);

                maximumChargedPower =  _attachedReactor.MaximumChargedPower;
                var currentMaximumChargedPower = maximum_isp == minimum_isp ? maximumChargedPower * currentThrottle : maximumChargedPower;

                _exchangerThrustDivisor = radius >= _attachedReactor.Radius ? 1 : radius * radius / _attachedReactor.Radius / _attachedReactor.Radius;

                _max_charged_particles_power = currentMaximumChargedPower * _exchangerThrustDivisor * _attachedReactor.ChargedParticlePropulsionEfficiency;
                _charged_particles_requested = exhaustAllowed && currentThrottle > 0 ? _max_charged_particles_power : 0;

                _charged_particles_received = _charged_particles_requested > 0 ? ConsumeFnResourcePerSecond(_charged_particles_requested, ResourceSettings.Config.ChargedPowerInMegawatt) : 0;

                // update Isp
                currentIsp = currentThrottle == 0 ? maximum_isp : Math.Min(maximum_isp, minimum_isp / Math.Pow(currentThrottle, throtleExponent));

                var thrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                var maxEngineThrustAtMaxIsp = thrustModifier * _charged_particles_received / maximum_isp / PhysicsGlobals.GravitationalAcceleration;

                var calculatedConsumptionInTon = maxEngineThrustAtMaxIsp / maximum_isp / PhysicsGlobals.GravitationalAcceleration;

                //UpdatePropellantBuffer(calculatedConsumptionInTon);

                // convert reactor product into propellants when possible and generate addition propellant from reactor fuel consumption
                chargedParticleRatio = currentMaximumChargedPower > 0 ? _charged_particles_received / currentMaximumChargedPower : 0;
                _attachedReactor.UseProductForPropulsion(chargedParticleRatio, calculatedConsumptionInTon, partResourceDefinitions);

                calculatedConsumptionPerSecond = calculatedConsumptionInTon * 1000;

                //if (!CheatOptions.IgnoreMaxTemperature)
                //{
                //    if (currentThrottle > 0)
                //    {
                //        _wasteheatConsumption = _charged_particles_received;
                //        if (_charged_particles_received > _previousChargedParticlesReceived)
                //            _wasteheatConsumption = _charged_particles_received + (_charged_particles_received - _previousChargedParticlesReceived);

                //        _previousChargedParticlesReceived = _charged_particles_received;
                //    }
                //    else
                //    {
                //        _wasteheatConsumption = 0;
                //        _charged_particles_received = 0;
                //        _previousChargedParticlesReceived = 0;
                //    }

                //    ConsumeFnResourcePerSecond(_wasteheatConsumption, ResourceSettings.Config.WasteHeatInMegawatt);
                //}

                if (_charged_particles_received == 0)
                {
                    _chargedParticleMaximumPercentageUsage = 0;

                    UpdateRunningEffect();
                    UpdatePowerEffect();
                }

                // calculate power cost
                var ispPowerCostMultiplier = 1 + _maxPowerMultiplier - Math.Log10(currentIsp / minimum_isp);
                var minimumElectricEnginePower = _attachedReactor.MagneticNozzlePowerMult * _charged_particles_received * ispPowerCostMultiplier * 0.005 * Math.Max(_attachedReactorDistance, 1);


                var neededBufferPower = Math.Min(GetResourceAvailability(ResourceSettings.Config.ElectricPowerInMegawatt) ,  Math.Min(Math.Max(powerBufferMax - powerBufferStore, 0), minimumElectricEnginePower));
                _requestedElectricPower = minimumElectricEnginePower + neededBufferPower;

                _recievedElectricPower = CheatOptions.InfiniteElectricity || _requestedElectricPower == 0
                    ? _requestedElectricPower
                    : ConsumeFnResourcePerSecond(_requestedElectricPower, ResourceSettings.Config.ElectricPowerInMegawatt);

                // adjust power buffer
                var powerSurplus = _recievedElectricPower - minimumElectricEnginePower;
                if (powerSurplus < 0)
                {
                    var powerFromBuffer = Math.Min(-powerSurplus, powerBufferStore);
                    _recievedElectricPower += powerFromBuffer;
                    powerBufferStore -= powerFromBuffer;
                }
                else
                    powerBufferStore += powerSurplus;

                // calculate Power factor
                megajoulesRatio = Math.Min(_recievedElectricPower / minimumElectricEnginePower, 1);
                megajoulesRatio = megajoulesRatio.IsInfinityOrNaN() ? 0 : megajoulesRatio;
                var scaledPowerFactor = Math.Pow(megajoulesRatio, 0.5);

                if (currentThrottle > 0)
                {
                    bool canConsumePropellant = true;
                    foreach (var partResourceDefinition in partResourceDefinitions)
                    {
                        if (part.RequestResource(partResourceDefinition.id, 0.001, true) <= 0)
                            canConsumePropellant = false;
                    }

                    // enable/disable engine
                    _attachedEngine.enabled = canConsumePropellant &&
                                              chargedParticleRatio > chargedParticleRatioThreshold &&
                                              megajoulesRatio > megajoulesRatioThreshold &&
                                              AttachedReactor.ConsumedFuelFixed > 0;
                }

                if (_attachedEngine.enabled == false)
                    _attachedEngine.currentThrottle = 0;

                var requiredElectricalPowerFromMhd = CalculateElectricalPowerCurrentlyNeeded(Math.Min(scaledPowerFactor * _charged_particles_received, minimumElectricEnginePower * 2));

                // convert part of the exhaust energy directly into electric power
                if (scaledPowerFactor > 0 && _charged_particles_received > 0)
                {
                    var availableElectricPower = scaledPowerFactor * _charged_particles_received;
                    var suppliedElectricPower = SupplyFnResourcePerSecondWithMax(requiredElectricalPowerFromMhd, availableElectricPower, ResourceSettings.Config.ElectricPowerInMegawatt);
                    _mhdTrustIspModifier = 1 - suppliedElectricPower / _charged_particles_received;
                }
                else
                    _mhdTrustIspModifier = 1;

                if (_max_charged_particles_power > 0)
                {
                    var maxThrust = _mhdTrustIspModifier * thrustModifier * _charged_particles_received * scaledPowerFactor / currentIsp / PhysicsGlobals.GravitationalAcceleration;
                    var effectiveThrust = Math.Max(maxThrust - (radius * radius * vessel.atmDensity * 100), 0);

                    _effectiveThrustRatio = maxThrust > 0 ? effectiveThrust / maxThrust : 0;
                    _engineMaxThrust = currentThrottle > 0 ? Math.Max(effectiveThrust, 1e-9) : Math.Max(maxThrust, 1e-9);
                }
                else
                {
                    _effectiveThrustRatio = 1;
                    _engineMaxThrust = 0;
                }

                // set isp
                FloatCurve newAtmosphereCurve = new FloatCurve();
                engineIsp = currentThrottle > 0 ? _mhdTrustIspModifier * currentIsp * scaledPowerFactor * _effectiveThrustRatio : currentIsp;
                newAtmosphereCurve.Add(0, (float)engineIsp, 0, 0);
                _attachedEngine.atmosphereCurve = newAtmosphereCurve;

                var maxEffectiveFuelFlowRate = !double.IsInfinity(_engineMaxThrust) && !double.IsNaN(_engineMaxThrust) && currentIsp > 0
                    ? _engineMaxThrust / currentIsp / PhysicsGlobals.GravitationalAcceleration / (currentThrottle > 0 ? currentThrottle : 1)
                    : 0;

                _maxTheoreticalThrust = thrustModifier * maximumChargedPower * _chargedParticleMaximumPercentageUsage / currentIsp / PhysicsGlobals.GravitationalAcceleration;
                var maxTheoreticalFuelFlowRate = _maxTheoreticalThrust / currentIsp / PhysicsGlobals.GravitationalAcceleration;

                // set maximum flow
                engineFuelFlow = currentThrottle > 0 ? Mathf.Max((float)maxEffectiveFuelFlowRate, MinimumFlowRate) : (float)maxTheoreticalFuelFlowRate;

                _attachedEngine.maxFuelFlow = engineFuelFlow;
                _attachedEngine.useThrustCurve = false;

                // This whole thing may be inefficient, but it should clear up some confusion for people.
                if (_attachedEngine.getFlameoutState) return;

                if (_attachedEngine.currentThrottle < 0.01)
                    _attachedEngine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu1");//"offline"
                else if (megajoulesRatio < 0.75 && _requestedElectricPower > 0)
                    _attachedEngine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu2");//"Insufficient Electricity"
                else if (_effectiveThrustRatio < 0.01 && vessel.atmDensity > 0)
                    _attachedEngine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu3");//"Too dense atmosphere"
            }
            else
            {
                _chargedParticleMaximumPercentageUsage = 0;
                _attachedEngine.maxFuelFlow = 0.0000000001f;
                _recievedElectricPower = 0;
                _charged_particles_requested = 0;
                _charged_particles_received = 0;
                _engineMaxThrust = 0;
            }

            currentThrust = _attachedEngine.GetCurrentThrust();
        }

        private void UpdatePowerEffect()
        {
            if (string.IsNullOrEmpty(powerEffectName))
                return;

            var powerEffectRatio = exhaustAllowed && _attachedEngine != null && _attachedEngine.isOperational && _chargedParticleMaximumPercentageUsage > 0 && currentThrust > 0 ? _attachedEngine.currentThrottle : 0;
            part.Effect(powerEffectName, powerEffectRatio, -1);
        }

        private void UpdateRunningEffect()
        {
            if (string.IsNullOrEmpty(runningEffectName))
                return;

            var runningEffectRatio = exhaustAllowed && _attachedEngine != null && _attachedEngine.isOperational && _chargedParticleMaximumPercentageUsage > 0 && engineFuelFlow > MinimumFlowRate ? _attachedEngine.currentThrottle : 0;
            part.Effect(runningEffectName, runningEffectRatio, -1);
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate()
        {
            if (_attachedEngine == null)
                return;

            if (_attachedReactor == null && _checkedConnectivity == false)
            {
                _checkedConnectivity = true;
                var message = "Warning: " + part.partInfo.title + " is not connected to a power source!";
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            exhaustAllowed = AllowedExhaust();
        }

        //public void UpdatePropellantBuffer(double calculatedConsumptionInTon)
        //{
        //    if (!maintainsPropellantBuffer)
        //        return;

        //    PartResource propellantPartResource = part.Resources.Get(propellantBufferResourceDefinition1.id);

        //    if (propellantPartResource == null || propellantBufferResourceDefinition1.density == 0)
        //        return;

        //    var newMaxAmount = Math.Max(minimumPropellantBuffer, 2 * TimeWarp.fixedDeltaTime * calculatedConsumptionInTon / propellantBufferResourceDefinition1.density);

        //    var storageShortage = Math.Max(0, propellantPartResource.amount - newMaxAmount);

        //    propellantPartResource.maxAmount = newMaxAmount;
        //    propellantPartResource.amount = Math.Min(newMaxAmount, propellantPartResource.amount);

        //    if (storageShortage > 0)
        //        part.RequestResource(propellantBufferResourceDefinition1.id, -storageShortage);
        //}

        public override string GetInfo()
        {
            return "";
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title + " (engine)";
        }

        public override int GetSupplyPriority()
        {
            return (int)Math.Round(supplyPriority);
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

            if (AttachedReactor.MayExhaustInAtmosphereHomeworld)
                return true;

            var minAltitude = AttachedReactor.MayExhaustInLowSpaceHomeworld ? homeworld.atmosphereDepth : homeworld.scienceValues.spaceAltitudeThreshold;

            if (distanceToSurfaceHomeworld < minAltitude)
                return false;

            if (AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 10 * homeworld.Radius)
                return true;

            if (!AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 20 * homeworld.Radius)
                return true;

            var radiusDividedByAltitude = (homeworld.Radius + minAltitude) / toHomeworld.magnitude;

            var coneAngle = 45 * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude;

            var allowedExhaustAngle = coneAngle + Math.Tanh(radiusDividedByAltitude) * (180 / Math.PI);

            if (allowedExhaustAngle < 3)
                return true;

            return currentExhaustAngle > allowedExhaustAngle;
        }
    }
}
