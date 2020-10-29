using FNPlugin.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class InterstellarRCSModule : ResourceSuppliableModule 
    {
        public const string GROUP = "InterstellarRCSModule";
        public const string GROUP_TITLE = "#LOC_KSPIE_RCSModule_groupName";

        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = false)]
        public string AnimationName = "";
        [KSPField(isPersistant = false)]
        public double efficency = 0.8;
        [KSPField(isPersistant = false)]
        public int type = 16;
        [KSPField(isPersistant = false)]
        public float maxThrust = 1;
        [KSPField(isPersistant = false)]
        public float maxIsp = 544;
        [KSPField(isPersistant = false)]
        public float minIsp = 272;
        [KSPField(isPersistant = false)]
        string displayName = "";

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_ThrustLimiter", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.05f, maxValue = 100, minValue = 5)]//Thrust Limiter
        public float thrustLimiter = 100;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_IsPowered")]//Is Powered
        public bool hasSufficientPower = true;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_RCSModule_BaseThrust", guiUnits = " kN")]//Max Thrust
        public float baseThrust = 0;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_Efficency")]//Efficency
        public string efficencyStr = "";
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_PropellantName")]//Propellant Name
        public string propNameStr = "";
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_PropellantMaximumIsp")]//Propellant Maximum Isp
        public float maxPropellantIsp;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_PropellantThrustMultiplier")]//Propellant Thrust Multiplier
        public double currentThrustMultiplier;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_MaxThrust")]//Max Thrust
        public string thrustStr;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_CurrentThrust", guiUnits = " kN")]//Current Thrust
        public float currentThrust;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_RCSModule_Mass", guiFormat = "F3", guiUnits = " t")]//Mass
        public float partMass = 0;

        // GUI
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_Power")]//Power
        public string electricalPowerConsumptionStr = "";
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_HeatProduction")]//Heat Production
        public string heatProductionStr = "";
        [KSPField(groupName = GROUP, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_Power"), UI_Toggle(disabledText = "Off", enabledText = "On")]//Power
        public bool powerEnabled = true;

        // internal
        private AnimationState[] rcsStates;
        private bool rcsIsOn;
        private bool rcsPartActive;

        private double power_ratio = 1;
        private double power_requested_f = 0;
        private double power_recieved_f = 1;
        private double heat_production_f = 0;
        private List<ElectricEnginePropellant> _propellants;
        private ModuleRCS attachedRCS;
        private double efficencyModifier;
        private float currentMaxThrust;
        private float oldThrustLimiter;
        private bool oldPowerEnabled;
        private int insufficientPowerTimout = 2;

        public ElectricEnginePropellant Current_propellant { get; set; }

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

        [KSPEvent(groupName = GROUP, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_NextPropellant", active = true)]//Next Propellant
        public void ToggleNextPropellantEvent()
        {
            SwitchToNextPropellant(_propellants.Count);
        }

        [KSPEvent(groupName = GROUP, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_RCSModule_PreviousPropellant", active = true)]//Previous Propellant
        public void TogglePreviousPropellantEvent()
        {
            SwitchToPreviousPropellant(_propellants.Count);
        }

        protected void SwitchPropellant(bool next, int maxSwitching)
        {
            if (next)
                SwitchToNextPropellant(maxSwitching);
            else
                SwitchToPreviousPropellant(maxSwitching);
        }

        protected void SwitchToNextPropellant(int maxSwitching)
        {
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
                fuel_mode = 0;

            SetupPropellants(true, maxSwitching);
        }

        protected void SwitchToPreviousPropellant(int maxSwitching)
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = _propellants.Count - 1;

            SetupPropellants(false, maxSwitching);
        }

        private void SetupPropellants(bool moveNext, int maxSwitching)
        {
            Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            if ((Current_propellant.SupportedEngines & type) != type)
            {
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }
            Propellant new_propellant = Current_propellant.Propellant;

            if (HighLogic.LoadedSceneIsFlight)
            {
                // you can have any fuel you want in the editor but not in flight
                //List<PartResource> totalpartresources = part.GetConnectedResources(new_propellant.name).ToList();
                //if (!totalpartresources.Any() && maxSwitching > 0)
                //{
                //    SwitchPropellant(moveNext, --maxSwitching);
                //    return;
                //}

                var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(new_propellant.name);
                
                double amount;
                double maxAmount;
                part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

                if (maxAmount > 0  && maxSwitching > 0)
                {
                    SwitchPropellant(moveNext, --maxSwitching);
                    return;
                }
            }

            if (PartResourceLibrary.Instance.GetDefinition(new_propellant.name) != null)
            {
                currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;

                var moduleConfig = new ConfigNode("MODULE");
                moduleConfig.AddValue("name", "ModuleRCSFX");
                moduleConfig.AddValue("thrusterPower", ((thrustLimiter / 100) * currentThrustMultiplier * baseThrust / Current_propellant.IspMultiplier).ToString("0.000"));
                moduleConfig.AddValue("resourceName", new_propellant.name);
                moduleConfig.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");

                maxPropellantIsp = (float)((hasSufficientPower ? maxIsp : minIsp) * Current_propellant.IspMultiplier * currentThrustMultiplier);

                var atmosphereCurve = new ConfigNode("atmosphereCurve");
                atmosphereCurve.AddValue("key", "0 " + (maxPropellantIsp).ToString("0.000"));
                atmosphereCurve.AddValue("key", "1 " + (maxPropellantIsp * 0.5).ToString("0.000"));
                atmosphereCurve.AddValue("key", "4 " + (maxPropellantIsp * 0.00001).ToString("0.000"));
                moduleConfig.AddNode(atmosphereCurve);

                attachedRCS.Load(moduleConfig);
            }
            else if (maxSwitching > 0)
            {
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }
        }

        public override void OnStart(PartModule.StartState state) 
        {
            // old legacy stuff
            if (baseThrust == 0 && maxThrust > 0)
                baseThrust = maxThrust;

            if (partMass == 0)
                partMass = part.mass;

            if (String.IsNullOrEmpty(displayName))
                displayName = part.partInfo.title;

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resources_to_supply;

            attachedRCS = this.part.FindModuleImplementing<ModuleRCS>();
            oldThrustLimiter = thrustLimiter;
            oldPowerEnabled = powerEnabled;
            efficencyModifier = GameConstants.STANDARD_GRAVITY * 0.5 / 1000 / efficency;
            efficencyStr = (efficency * 100).ToString() + "%";

            if (!String.IsNullOrEmpty(AnimationName))
                rcsStates = PluginHelper.SetUpAnimation(AnimationName, this.part);

            // initialize propellant
            _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            SetupPropellants(true, _propellants.Count);
            currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;

            base.OnStart(state);
         }

        public void Update()
        {
            if (Current_propellant == null) return;

            if (oldThrustLimiter != thrustLimiter)
            {
                SetupPropellants(true, 0);
                oldThrustLimiter = thrustLimiter;
            }

            if (oldPowerEnabled != powerEnabled)
            {
                hasSufficientPower = powerEnabled;
                SetupPropellants(true, 0);
                oldPowerEnabled = powerEnabled;
            }

            propNameStr = Current_propellant.PropellantGUIName;

            currentMaxThrust = (float)(baseThrust / Current_propellant.IspMultiplier * currentThrustMultiplier);

            thrustStr = attachedRCS.thrusterPower.ToString("0.000") + " / " + currentMaxThrust.ToString("0.000") + " kN";
        }

        public override void OnUpdate() 
        {
            if (attachedRCS != null && vessel.ActionGroups[KSPActionGroup.RCS]) 
            {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                electricalPowerConsumptionStr = PluginHelper.getFormattedPowerString(power_recieved_f) + " / " + PluginHelper.getFormattedPowerString(power_requested_f);
                heatProductionStr = PluginHelper.getFormattedPowerString(heat_production_f);
            } 
            else 
            {
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
            }

            if (rcsStates == null) return;

            rcsIsOn = this.vessel.ActionGroups.groups[3];
            foreach (ModuleRCS rcs in part.FindModulesImplementing<ModuleRCS>())
            {
                rcsPartActive = rcs.isEnabled;
            }

            foreach (AnimationState anim in rcsStates)
            {
                if (attachedRCS.rcsEnabled && rcsIsOn && rcsPartActive && anim.normalizedTime < 1) { anim.speed = 1; }
                if (attachedRCS.rcsEnabled && rcsIsOn && rcsPartActive && anim.normalizedTime >= 1)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 1;
                }
                if ((!attachedRCS.rcsEnabled || !rcsIsOn || !rcsPartActive) && anim.normalizedTime > 0) { anim.speed = -1; }
                if ((!attachedRCS.rcsEnabled || !rcsIsOn || !rcsPartActive) && anim.normalizedTime <= 0)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 0;
                }
            }
        }

        public void FixedUpdate()
        {
            currentThrust = 0;

            if (attachedRCS == null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!vessel.ActionGroups[KSPActionGroup.RCS]) return;

            currentThrust = attachedRCS.thrustForces.Sum(frc => frc);

            if (powerEnabled)
            {
                float curve_eval_point = (float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position) / 100, 1.0);
                float currentIsp = attachedRCS.atmosphereCurve.Evaluate(curve_eval_point);

                power_requested_f = currentThrust * currentIsp * efficencyModifier / currentThrustMultiplier;

                power_recieved_f = CheatOptions.InfiniteElectricity 
                    ? power_requested_f
                    : consumeFNResourcePerSecond(power_requested_f, ResourceManager.FNRESOURCE_MEGAJOULES);

                double heat_to_produce = power_recieved_f * (1 - efficency);

                heat_production_f = CheatOptions.IgnoreMaxTemperature 
                    ? heat_to_produce
                    : supplyFNResourcePerSecond(heat_to_produce, ResourceManager.FNRESOURCE_WASTEHEAT);

                power_ratio = power_requested_f > 0 ? Math.Min(power_recieved_f / power_requested_f, 1) : 1;
            }
            else
            {
                power_ratio = 0;
                insufficientPowerTimout = 0;
            }

            if (hasSufficientPower && power_ratio < 0.9 && power_recieved_f < 0.01 )
            {
                if (insufficientPowerTimout < 1)
                {
                    hasSufficientPower = false;
                    SetupPropellants(true, 0);
                }
                else
                    insufficientPowerTimout--;
            }
            else if (!hasSufficientPower && power_ratio > 0.9 && power_recieved_f > 0.01)
            {
                insufficientPowerTimout = 2;
                hasSufficientPower = true;
                SetupPropellants(true, 0);
            }
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title;
        }
    }
}
