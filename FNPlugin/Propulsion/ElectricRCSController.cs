using FNPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    class ElectricRCSController : ResourceSuppliableModule 
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = true)]
        public string fuel_mode_name;
        [KSPField(isPersistant = false)]
        public string AnimationName = "";
        [KSPField(isPersistant = false)]
        public double efficiency = 0.8;
        [KSPField(isPersistant = false)]
        public int type = 16;
        [KSPField(isPersistant = false)]
        public float maxThrust = 1;
        [KSPField(isPersistant = false)]
        public float maxIsp = 2000;
        [KSPField(isPersistant = false)]
        public float minIsp = 250;
        [KSPField(isPersistant = false)]
        public string displayName = "";
        [KSPField(isPersistant = false)]
        public bool showConsumption = true;
        [KSPField(isPersistant = false)]
        public double powerMult = 1;
        [KSPField(isPersistant = false)]
        public double bufferMult = 8;

        [KSPField(isPersistant = true, guiActive = false)]
        public double storedPower = 0;
        [KSPField(isPersistant = true, guiActive = false)]
        public double maxStoredPower = 0;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_Power", advancedTweakable = true), UI_Toggle(disabledText = "#LOC_KSPIE_ElectricRCSController_Power_Off", enabledText = "#LOC_KSPIE_ElectricRCSController_Power_On", affectSymCounterparts = UI_Scene.All)]//Power--Off--On
        public bool powerEnabled = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_PropellantName")]//Propellant Name
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_ElectricRCSController_PropellantMaximumIsp")]//Propellant Maximum Isp
        public float maxPropellantIsp;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_ElectricRCSController_PropellantThrustMultiplier")]//Propellant Thrust Multiplier
        public double currentThrustMultiplier = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_ThrustIspMultiplier")]//Thrust / ISP Mult
        public string thrustIspMultiplier = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_ElectricRCSController_BaseThrust", guiUnits = " kN")]//Base Thrust
        public float baseThrust = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_CurrentTotalThrust", guiUnits = " kN")]//Current Total Thrust
        public float currentThrust;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_ElectricRCSController_Mass", guiUnits = " t")]//Mass
        public float partMass = 0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_Consumption")]//Consumption
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_HasSufficientPower")]//Is Powered
        public bool hasSufficientPower = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_Efficiency")]//Efficiency
        public string efficiencyStr = "";

        // internal
        private AnimationState[] rcsStates;
        private bool rcsIsOn;
        private bool rcsPartActive;

        private PartResourceDefinition definitionMegajoule;

        [KSPField(guiActive = false)]
        public double power_shortage;
        [KSPField(guiActive = false)]
        public double power_ratio = 1;
        [KSPField(guiActive = false)]
        public double power_requested_f = 0;
        [KSPField(guiActive = false)]
        public double additional_power_requested_f;
        [KSPField(guiActive = false)]
        public double power_recieved_f = 1;
        [KSPField(guiActive = false)]
        public double additional_power_recieved_f;

        private double heat_production_f = 0;
        private List<ElectricEnginePropellant> _propellants;
        private ModuleRCSFX attachedRCS;
        private bool oldPowerEnabled;
        private bool delayedVerificationPropellant;

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

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_NextPropellant", active = true)]//Next Propellant
        public void ToggleNextPropellantEvent()
        {
            SwitchToNextPropellant(_propellants.Count);
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricRCSController_PreviousPropellant", active = true)]//Previous Propellant
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

        private void SetupPropellants(bool moveNext = true, int maxSwitching = 0)
        {
            Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            fuel_mode_name = Current_propellant.PropellantName;

            if ((Current_propellant.SupportedEngines & type) != type)
            {
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }

            Propellant new_propellant = Current_propellant.Propellant;
            if (HighLogic.LoadedSceneIsFlight)
            {
                // you can have any fuel you want in the editor but not in flight
                var totalpartresources = part.GetConnectedResources(new_propellant.name).ToList();
                if (!totalpartresources.Any() && maxSwitching > 0)
                {
                    SwitchPropellant(moveNext, --maxSwitching);
                    return;
                }
            }

            if (PartResourceLibrary.Instance.GetDefinition(new_propellant.name) != null)
            {
                var effectiveIspMultiplier = type == 2 ? Current_propellant.DecomposedIspMult : Current_propellant.IspMultiplier;

                var moduleConfig = new ConfigNode("MODULE");

                moduleConfig.AddValue("thrusterPower", attachedRCS.thrusterPower.ToString("0.000"));
                moduleConfig.AddValue("resourceName", new_propellant.name);
                moduleConfig.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");

                ConfigNode propellantConfigNode = moduleConfig.AddNode("PROPELLANT");
                propellantConfigNode.AddValue("name", new_propellant.name);
                propellantConfigNode.AddValue("ratio", "1");
                propellantConfigNode.AddValue("DrawGauge", "True");
                propellantConfigNode.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");
                attachedRCS.Load(propellantConfigNode);

                currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;

                var effectiveBaseIsp = hasSufficientPower ? maxIsp : minIsp;

                maxPropellantIsp = (float)(effectiveBaseIsp * effectiveIspMultiplier * currentThrustMultiplier);

                var atmosphereCurve = new ConfigNode("atmosphereCurve");
                atmosphereCurve.AddValue("key", "0 " + (maxPropellantIsp).ToString("0.000"));
                if (type != 8)
                {
                    atmosphereCurve.AddValue("key", "1 " + (maxPropellantIsp * 0.5).ToString("0.000"));
                    atmosphereCurve.AddValue("key", "4 " + (maxPropellantIsp * 0.00001).ToString("0.000"));
                }
                else
                    atmosphereCurve.AddValue("key", "1 " + (maxPropellantIsp).ToString("0.000"));

                moduleConfig.AddNode(atmosphereCurve);

                attachedRCS.Load(moduleConfig);
            }
            else if (maxSwitching > 0)
            {
                Debug.Log("ElectricRCSController SetupPropellants switching mode because no definition found for " + new_propellant.name);
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }
        }

        [KSPAction("Toggle Power")]
        public void TogglePowerAction(KSPActionParam param)
        {
            powerEnabled = !powerEnabled;

            power_recieved_f = powerEnabled ? CheatOptions.InfiniteElectricity ? 1 : consumeFNResourcePerSecond(0.1, ResourceManager.FNRESOURCE_MEGAJOULES) : 0;
            hasSufficientPower = power_recieved_f >= 0.09;
            SetupPropellants();
            currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;
        }

        public override void OnStart(PartModule.StartState state) 
        {
            definitionMegajoule = PartResourceLibrary.Instance.GetDefinition(ResourceManager.FNRESOURCE_MEGAJOULES);

            try
            {
                attachedRCS = this.part.FindModuleImplementing<ModuleRCSFX>();

                // old legacy stuff
                if (baseThrust == 0 && maxThrust > 0)
                    baseThrust = maxThrust;

                if (partMass == 0)
                    partMass = part.mass;

                if (String.IsNullOrEmpty(displayName))
                    displayName = part.partInfo.title;

                String[] resources_to_supply = { ResourceManager.FNRESOURCE_WASTEHEAT };
                this.resources_to_supply = resources_to_supply;

                oldPowerEnabled = powerEnabled;
                efficiencyStr = (efficiency * 100).ToString() + "%";

                if (!String.IsNullOrEmpty(AnimationName))
                    rcsStates = PluginHelper.SetUpAnimation(AnimationName, this.part);

                // initialize propellant
                _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);

                delayedVerificationPropellant = true;
                // find correct fuel mode index
                if (!String.IsNullOrEmpty(fuel_mode_name))
                {
                    Debug.Log("[KSPI]: ElectricRCSController OnStart loaded fuelmode " + fuel_mode_name);
                    Current_propellant = _propellants.FirstOrDefault(p => p.PropellantName == fuel_mode_name);
                }
                if (Current_propellant != null && _propellants.Contains(Current_propellant))
                {
                    fuel_mode = _propellants.IndexOf(Current_propellant);
                    Debug.Log("[KSPI]: ElectricRCSController OnStart index of fuelmode " + Current_propellant.PropellantGUIName + " = " + fuel_mode);
                }

                base.OnStart(state);

                Fields["electricalPowerConsumptionStr"].guiActive = showConsumption;

                maxStoredPower = bufferMult * maxThrust * maxIsp * 9.81 / efficiency / 1000;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ElectricRCSController OnStart Error: " + e.Message);
                throw;
            }
         }

        public void Update()
        {
            if (Current_propellant == null) return;

            if (oldPowerEnabled != powerEnabled)
            {
                hasSufficientPower = powerEnabled;
                SetupPropellants(true, 0);
                oldPowerEnabled = powerEnabled;
            }

            propNameStr = Current_propellant.PropellantGUIName;

            thrustIspMultiplier = maxPropellantIsp + "s / " + currentThrustMultiplier;
        }

        public override void OnUpdate() 
        {
            if (delayedVerificationPropellant)
            {
                delayedVerificationPropellant = false;
                SetupPropellants(true, _propellants.Count);
                currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;
            }

            if (attachedRCS != null && vessel.ActionGroups[KSPActionGroup.RCS]) 
            {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                electricalPowerConsumptionStr = power_recieved_f.ToString("0.00") + " MW / " + power_requested_f.ToString("0.00") + " MW";
            } 
            else 
                Fields["electricalPowerConsumptionStr"].guiActive = false;

            if (rcsStates == null) return;

            rcsIsOn = this.vessel.ActionGroups.groups[3];

            var moduleImplementingRcs = part.FindModuleImplementing<ModuleRCS>();
            if (moduleImplementingRcs != null)
            {
                rcsPartActive = moduleImplementingRcs.isEnabled;
            }

            var rcsStatesCount = rcsStates.Count();
            for (var i = 0; i < rcsStatesCount; i++)
            {
                var anim = rcsStates[i];
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

        /// <summary>
        /// FixedUpdate is also called when not activated
        /// </summary>
        public void FixedUpdate()
        {
            power_requested_f = 0;
            power_recieved_f = 0;
            power_shortage = 0;
            additional_power_requested_f = 0;
            additional_power_recieved_f = 0;

            if (attachedRCS == null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

            currentThrust = attachedRCS.thrustForces.Sum(frc => frc);

            if (!vessel.ActionGroups[KSPActionGroup.RCS]) return;

            var forcesCount = attachedRCS.thrustForces.Count();

            if (powerEnabled)
            {
                if (CheatOptions.InfiniteElectricity)
                {
                    power_ratio = 1;
                }
                else
                {
                    var availablePower = getAvailableStableSupply(ResourceManager.FNRESOURCE_MEGAJOULES);

                    if (currentThrust > 0)
                    {
                        power_requested_f = 0.5 * powerMult * currentThrust * maxIsp * 9.81 / efficiency / 1000 / Current_propellant.ThrustMultiplier;

                        power_recieved_f = power_requested_f <= availablePower
                           ? consumeFNResourcePerSecond(power_requested_f, ResourceManager.FNRESOURCE_MEGAJOULES)
                           : 0;

                        var final_received_power = power_recieved_f;
                        if (power_recieved_f < power_requested_f)
                        {
                            power_shortage = power_requested_f - power_recieved_f;
                            if (power_shortage <= storedPower)
                            {
                                final_received_power = power_requested_f;
                                storedPower -= power_shortage;
                            }
                        }
                        else
                            power_shortage = 0;

                        power_ratio = power_requested_f > 0 ? Math.Min(final_received_power / power_requested_f, 1) : 1;
                    }

                    additional_power_requested_f = Math.Max(maxStoredPower - storedPower, 0);
                    if (additional_power_requested_f > 0 && additional_power_requested_f <= availablePower)
                    {
                        power_requested_f += additional_power_requested_f;
                        additional_power_recieved_f = consumeFNResourcePerSecond(additional_power_requested_f, ResourceManager.FNRESOURCE_MEGAJOULES);
                        storedPower += additional_power_recieved_f;
                    }

                    if (storedPower >= maxStoredPower)
                        power_ratio = 1;

                    var heatToProduce = (power_recieved_f + additional_power_recieved_f) * (1 - efficiency);

                    heat_production_f = CheatOptions.IgnoreMaxTemperature
                        ? heatToProduce
                        : supplyFNResourcePerSecond(heatToProduce, ResourceManager.FNRESOURCE_WASTEHEAT);
                }           
            }
            else
            {
                power_recieved_f = 0;
                power_ratio = 0;
            }

            if (hasSufficientPower && power_ratio <= 0.999 )
            {
                hasSufficientPower = false;
                SetupPropellants();
            }
            else if (!hasSufficientPower && power_ratio > 0.999)
            {
                hasSufficientPower = true;
                SetupPropellants();
            }

            // store any unused power
            if (!hasSufficientPower && power_recieved_f > 0)
            {
                storedPower += power_requested_f;
            }
        }

        public override string getResourceManagerDisplayName() 
        {
            return part.partInfo.title + " (" + propNameStr + ")";
        }
        public override int getPowerPriority()
        {
            return 2;
        }
    }
}
