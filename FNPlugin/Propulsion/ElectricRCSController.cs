using FNPlugin.Extensions;
using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    [KSPModule("Electric RCS Controller")]
    class ElectricRCSController : ResourceSuppliableModule
    {
        public const string Group = "InterstellarRCSModule";
        public const string GroupTitle = "#LOC_KSPIE_RCSModule_groupName";

        [KSPField(isPersistant = true)] public int fuel_mode;
        [KSPField(isPersistant = true)] public string fuel_mode_name;
        [KSPField(isPersistant = false)] public string AnimationName = "";
        [KSPField(isPersistant = true)] public double storedPower;
        [KSPField(isPersistant = true)] public double maxStoredPower;

        [KSPField] public int rcsIndex = 0;
        [KSPField] public int type = 16;
        [KSPField] public float maxThrust = 1;
        [KSPField] public float maxIsp = 2000;
        [KSPField] public float minIsp = 250;
        [KSPField] public string displayName = "";
        [KSPField] public bool showConsumption = true;
        [KSPField] public double powerMult = 1;
        [KSPField] public double bufferMult = 8;
        [KSPField] public double efficiency = 0.8;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_HasSufficientPower")]//Is Powered
        public bool hasSufficientPower;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_PropellantName")]//Propellant Name
        public string propNameStr = "";
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricRCSController_PropellantMaximumIsp")]//Propellant Maximum Isp
        public float maxPropellantIsp;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricRCSController_PropellantThrustMultiplier")]//Propellant Thrust Multiplier
        public double currentThrustMultiplier = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_ElectricRCSController_ThrustIspMultiplier")]//Thrust / ISP Mult
        public string thrustIspMultiplier = "";
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_CurrentTotalThrust", guiFormat = "F2", guiUnits = " kN")]//Current Total Thrust
        public float currentThrust;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle,  guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricRCSController_Mass", guiFormat = "F3", guiUnits = " t")]//Mass
        public float partMass;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_Consumption")]//Consumption
        public string powerConsumptionStr = "";
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_Efficiency")]//Efficiency
        public string efficiencyStr = "";
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_Power", advancedTweakable = true), UI_Toggle(disabledText = "#LOC_KSPIE_ElectricRCSController_Power_Off", enabledText = "#LOC_KSPIE_ElectricRCSController_Power_On", affectSymCounterparts = UI_Scene.All)]//Power--Off--On
        public bool powerEnabled = true;

        // internal
        private readonly IList<ElectricEnginePropellant> _propellants;
        private bool _oldPowerEnabled;
        private bool _delayedVerificationPropellant;
        private ModuleRCSFX _attachedRcs;
        private BaseField _powerConsumptionStrField;
        private AnimationState[] _rcsStates;

        public ElectricEnginePropellant CurrentPropellant { get; set; }

        public ElectricRCSController()
        {
            hasSufficientPower = true;
            _propellants = new List<ElectricEnginePropellant>(32);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var moduleRcsFx = part.FindModuleImplementing<ModuleRCSFX>();
            if (moduleRcsFx == null) return;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.rcsEnabled)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.rcsEnabled)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.thrustPercentage)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.thrustPercentage)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.thrustCurveDisplay)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.thrustCurveDisplay)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.thrustCurveRatio)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.thrustCurveRatio)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableYaw)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableYaw)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.enablePitch)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.enablePitch)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableRoll)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableRoll)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableX)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableX)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableY)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableY)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableZ)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.enableZ)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.useThrottle)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.useThrottle)].Attribute.groupDisplayName = GroupTitle;

            moduleRcsFx.Fields[nameof(ModuleRCSFX.realISP)].Attribute.groupName = Group;
            moduleRcsFx.Fields[nameof(ModuleRCSFX.realISP)].Attribute.groupDisplayName = GroupTitle;
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

        [KSPEvent(groupName = Group, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_NextPropellant", active = true)]//Next Propellant
        public void ToggleNextPropellantEvent()
        {
            SwitchToNextPropellant();
        }

        [KSPEvent(groupName = Group, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_PreviousPropellant", active = true)]//Previous Propellant
        public void TogglePreviousPropellantEvent()
        {
            SwitchToPreviousPropellant();
        }

        // FixedUpdate is also called when not activated
        public void FixedUpdate()
        {
            if (_attachedRcs != null && HighLogic.LoadedSceneIsFlight && vessel.ActionGroups[KSPActionGroup.RCS])
            {
                bool powerConsumed = false;

                var forces = _attachedRcs.thrustForces;
                int forcesCount = forces.Length;
                currentThrust = 0.0f;
                for (var i = 0; i < forcesCount; i++)
                {
                    currentThrust += forces[i];
                }

                double received = 0.0, requested = 0.0;
                if (powerEnabled)
                {
                    if (currentThrust > 0.0f)
                    {
                        requested = 0.5 * powerMult * currentThrust * maxIsp * PhysicsGlobals.GravitationalAcceleration
                                    / (efficiency * 1000.0 * CurrentPropellant.ThrustMultiplier);
                    }
                    received = ConsumeMegawatts(requested + Math.Min(requested, Math.Max(0.0, maxStoredPower - storedPower) / TimeWarp.fixedDeltaTime));
                    SupplyFnResourcePerSecond(received * (1.0 - efficiency), ResourceSettings.Config.WasteHeatInMegawatt);

                    double totalPower = storedPower + received * TimeWarp.fixedDeltaTime, energyNeed = requested * TimeWarp.fixedDeltaTime;
                    if (totalPower >= energyNeed)
                    {
                        powerConsumed = true;
                        totalPower -= energyNeed;
                    }
                    storedPower = totalPower;
                }
                powerConsumptionStr = PluginHelper.GetFormattedPowerString(received) + " / " +
                    PluginHelper.GetFormattedPowerString(requested);

                if (hasSufficientPower != powerConsumed)
                {
                    hasSufficientPower = powerConsumed;
                    SetPropellant(true);
                }
            }
        }

        private void LoadConfig()
        {
            double effectiveIspMultiplier = type == (int)ElectricEngineType.ARCJET ? CurrentPropellant.DecomposedIspMult : CurrentPropellant.IspMultiplier;
            string propName = CurrentPropellant.Propellant.name;

            var moduleConfig = new ConfigNode("MODULE");
            moduleConfig.AddValue("thrusterPower", _attachedRcs.thrusterPower.ToString("F3"));
            moduleConfig.AddValue("resourceName", propName);
            moduleConfig.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");

            var propellantConfigNode = moduleConfig.AddNode("PROPELLANT");
            propellantConfigNode.AddValue("name", propName);
            propellantConfigNode.AddValue("ratio", "1");
            propellantConfigNode.AddValue("DrawGauge", "True");
            propellantConfigNode.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");
            _attachedRcs.Load(propellantConfigNode);

            currentThrustMultiplier = hasSufficientPower ? CurrentPropellant.ThrustMultiplier : CurrentPropellant.ThrustMultiplierCold;

            var effectiveBaseIsp = hasSufficientPower ? maxIsp : minIsp;
            maxPropellantIsp = (float)(effectiveBaseIsp * effectiveIspMultiplier * currentThrustMultiplier);

            var atmosphereCurve = new ConfigNode("atmosphereCurve");
            atmosphereCurve.AddValue("key", "0 " + maxPropellantIsp.ToString("F3"));
            if (type == (int)ElectricEngineType.VACUUMTHRUSTER)
            {
                atmosphereCurve.AddValue("key", "1 " + (maxPropellantIsp).ToString("F3"));
            }
            else
            {
                atmosphereCurve.AddValue("key", "1 " + (maxPropellantIsp * 0.5).ToString("F3"));
                atmosphereCurve.AddValue("key", "4 " + (maxPropellantIsp * 0.00001).ToString("F3"));
            }

            moduleConfig.AddNode(atmosphereCurve);
            _attachedRcs.Load(moduleConfig);
            thrustIspMultiplier = currentThrustMultiplier.ToString("F2") + " / " + maxPropellantIsp.ToString("F0") + " s";
        }

        public override void OnStart(StartState state)
        {
            var rcs = part.FindModulesImplementing<ModuleRCSFX>();
            int fm;
            _attachedRcs = (rcsIndex >= rcs.Count) ? null : rcs[rcsIndex];

            if (partMass == 0)
                partMass = part.mass;

            resourcesToSupply = new [] { ResourceSettings.Config.WasteHeatInMegawatt };

            _oldPowerEnabled = powerEnabled;
            efficiencyStr = efficiency.ToString("P1");

            if (!string.IsNullOrEmpty(AnimationName))
                _rcsStates = PluginHelper.SetUpAnimation(AnimationName, part);

            // Only allow _propellants that are compatible with this engine type
            _propellants.Clear();
            foreach (var propellant in ElectricEnginePropellant.GetPropellantsEngineForType(type))
            {
                if ((propellant.SupportedEngines & type) != 0)
                    _propellants.Add(propellant);
            }

            if (_propellants.Count < 1)
            {
                Debug.LogError("[KSPI]: No _propellants available for RCS type " + type + "!");
            }

            _delayedVerificationPropellant = true;
            // find correct fuel mode index
            if (!string.IsNullOrEmpty(fuel_mode_name))
            {
                foreach (var propellant in _propellants)
                {
                    if (propellant.PropellantName != fuel_mode_name) continue;

                    Debug.Log("[KSPI]: ElectricRCSController set fuel mode " + fuel_mode_name);
                    CurrentPropellant = propellant;
                    break;
                }
            }

            if (CurrentPropellant != null && (fm = _propellants.IndexOf(CurrentPropellant)) >= 0)
            {
                fuel_mode = fm;
            }
            SetPropellant(true);

            base.OnStart(state);

            _powerConsumptionStrField = Fields[nameof(powerConsumptionStr)];
            _powerConsumptionStrField.guiActive = showConsumption;

            maxStoredPower = bufferMult * maxThrust * powerMult * maxIsp * PhysicsGlobals.GravitationalAcceleration / (efficiency * 1000.0);
        }

        public override string GetInfo()
        {
            var returnStr = StringBuilderCache.Acquire();

            maxStoredPower = 0.5 * powerMult * maxThrust * maxIsp * PhysicsGlobals.GravitationalAcceleration / (efficiency * 1000.0);

            returnStr.AppendLine("Max Power:" + maxStoredPower.ToString("F3") + " MW");

            return returnStr.ToStringAndRelease();
        }

        public override int getPowerPriority()
        {
            return 2;
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title + " (" + propNameStr + ")";
        }

        private void MovePropellant(bool moveNext)
        {
            if (moveNext)
            {
                fuel_mode++;
                if (fuel_mode >= _propellants.Count)
                    fuel_mode = 0;
            }
            else
            {
                fuel_mode--;
                if (fuel_mode < 0)
                    fuel_mode = _propellants.Count - 1;
            }
        }

        public void OnEditorAttach()
        {
            _delayedVerificationPropellant = true;
        }

        public override void OnUpdate()
        {
            bool rcsIsOn = vessel.ActionGroups[KSPActionGroup.RCS];

            if (_delayedVerificationPropellant)
            {
                _delayedVerificationPropellant = false;
                SetPropellant(true);
            }

            _powerConsumptionStrField.guiActive = _attachedRcs != null && rcsIsOn;

            if (part.Modules.Contains("ModuleWaterfallFX"))
                WaterfallIntegration.RCSPower(part, _attachedRcs.thrusterTransformName, powerEnabled);

            if (_rcsStates == null) return;

            bool rcsPartActive = false;

            var moduleImplementingRcs = part.FindModuleImplementing<ModuleRCS>();
            if (moduleImplementingRcs != null)
            {
                rcsPartActive = moduleImplementingRcs.isEnabled;
            }

            int n = _rcsStates.Length;
            bool extend = _attachedRcs != null && _attachedRcs.rcsEnabled && rcsIsOn && rcsPartActive;
            for (var i = 0; i < n; i++)
            {
                var anim = _rcsStates[i];
                if (extend)
                {
                    if (anim.normalizedTime >= 1.0f)
                    {
                        anim.normalizedTime = 1.0f;
                        anim.speed = 0.0f;
                    }
                    else
                        anim.speed = 1.0f;
                }
                else
                {
                    if (anim.normalizedTime <= 0.0f)
                    {
                        anim.normalizedTime = 0.0f;
                        anim.speed = 0.0f;
                    }
                    else
                        anim.speed = -1.0f;
                }
            }
        }

        private void SetPropellant(bool moveNext)
        {
            int attempts = _propellants.Count;
            do
            {
                CurrentPropellant = _propellants[fuel_mode];
                fuel_mode_name = CurrentPropellant.PropellantName;

                var propellant = CurrentPropellant.Propellant;
                string propName = propellant.name;
                if ((!HighLogic.LoadedSceneIsFlight || part.GetConnectedResources(propName).
                    Any()) && PartResourceLibrary.Instance.GetDefinition(propName) != null)
                {
                    propNameStr = CurrentPropellant.PropellantGUIName;
                    LoadConfig();
                    break;
                }
                else
                {
                    Debug.Log("[KSPI]: ElectricRCSController switching mode, cannot use " + propName);
                    MovePropellant(moveNext);
                }
            } while (--attempts >= 0);
        }

        protected void SwitchToNextPropellant()
        {
            MovePropellant(true);
            SetPropellant(true);
        }

        protected void SwitchToPreviousPropellant()
        {
            MovePropellant(false);
            SetPropellant(false);
        }

        [KSPAction("Toggle Power")]
        public void TogglePowerAction(KSPActionParam _)
        {
            powerEnabled = !powerEnabled;

            SetPropellant(true);
        }

        public void Update()
        {
            if (CurrentPropellant == null) return;

            if (_oldPowerEnabled == powerEnabled) return;

            hasSufficientPower = powerEnabled;
            SetPropellant(true);
            _oldPowerEnabled = powerEnabled;
        }
    }
}
