﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FNPlugin.Propulsion;
using FNPlugin.Extensions;

namespace FNPlugin
{
    class MicrowavePowerReceiver : FNResourceSuppliableModule, IThermalSource, IElectricPowerSource
    {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool receiverIsEnabled;
        [KSPField(isPersistant = true)]
        public bool animatonDeployed = false;
        [KSPField(isPersistant = true)]
        public double wasteheatRatio = 0;
        [KSPField(isPersistant = true)]
        public bool linkedForRelay;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Receive Efficiency", guiUnits = "%", guiFormat = "F0")]
        public double efficiencyPercentage = GameConstants.microwave_dish_efficiency;

        //Persistent False
        [KSPField(isPersistant = false, guiActive = false, guiName = "instance ID")]
        public int instanceId;
        [KSPField(isPersistant = false)]
        public float powerMult = 1;
        [KSPField(isPersistant = false)]
        public float facingThreshold = 0;
        [KSPField(isPersistant = false)]
        public float facingExponent = 1;
        [KSPField(isPersistant = false)]
        public bool canLinkup = true;

        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public string animTName;
        [KSPField(isPersistant = false)]
        public string animGenericName;

        //[KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Collector Area", guiUnits = " m2")]
        //public float collectorArea = 1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Diameter", guiUnits = " m")]
        public float diameter = 1;
        [KSPField(isPersistant = false)]
        public bool isThermalReceiver;
        [KSPField(isPersistant = false)]
        public bool isThermalReceiverSlave = false;
        [KSPField(isPersistant = false)]
        public float ThermalTemp;
        [KSPField(isPersistant = false)]
        public double ThermalPower;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Radius", guiUnits = " m")]
        public float radius;

        [KSPField(isPersistant = false)]
        public double minimumWavelength = 0.00000001f;
        [KSPField(isPersistant = false)]
        public double maximumWavelength = 1f; 
        [KSPField(isPersistant = false)]
        public float heatTransportationEfficiency = 0.7f;
        [KSPField(isPersistant = false)]
        public float powerHeatExponent = 0.7f;
        [KSPField(isPersistant = false)]
        public float powerHeatMultiplier = 20f;
        [KSPField(isPersistant = false)]
        public float powerHeatBase = 3500f;
        [KSPField(isPersistant = false)]
        public float receiverType = 0;
        [KSPField(isPersistant = false)]
        public float receiverFracionBonus = 0;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float apertureMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float highSpeedAtmosphereFactor = 0;
        [KSPField(isPersistant = false)]
        public float atmosphereToleranceModifier = 1;
        [KSPField(isPersistant = false)]
        public float thermalPropulsionEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float thermalEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float chargedParticleEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float thermalProcessingModifier = 1;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Temperature")]
        public string coreTempererature;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Input Power")]
        public string beamedpower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Satellites Connected")]
        public string connectedsats;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Relays Connected")]
        public string connectedrelays;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Network Depth")]
        public string networkDepthString;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Recieve Efficiency")]
        public string toteff;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Reception"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float receiptPower = 100;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Maximum Input Power", guiUnits = " MW", guiFormat = "F2")]
        public float maximumPower = 10000;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power Source", guiFormat = "F2", guiUnits = "MW")]
        public double maxAvailablePowerFromSource;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Route Efficiency", guiFormat = "F4", guiUnits = "%")]
        public double routeEfficiency;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Other Power Usage", guiFormat = "F2", guiUnits = " MW")]
        public double currentPowerUsageByOtherRecievers;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Remaining Beamed Power", guiFormat = "F2", guiUnits = " MW")]
        public double remainingPowerFromSource;

        protected BaseField _radiusField;
        protected BaseField _coreTempereratureField;

        //Internal 
        protected bool waitForAnimationToComplete = false;
        protected double waste_heat_production;
        protected float connectedRecieversSum;

        protected Dictionary<Vessel, double> received_power = new Dictionary<Vessel, double>();
        protected List<MicrowavePowerReceiver> thermalReceiverSlaves = new List<MicrowavePowerReceiver>();
        protected MicrowavePowerReceiver mother;

        // reference types
        protected Dictionary<Guid, float> connectedRecievers = new Dictionary<Guid, float>();
        protected Dictionary<Guid, float> connectedRecieversFraction = new Dictionary<Guid, float>();
        

        protected double storedIsThermalEnergyGenratorActive;
        protected double currentIsThermalEnergyGenratorActive;

        public Part Part { get { return this.part; } }

        public double ProducedThermalHeat { get { return powerInputMegajoules; } }

        private double _requestedThermalHeat;
        public double RequestedThermalHeat
        {
            get { return _requestedThermalHeat; }
            set { _requestedThermalHeat = value; }
        }
        public double ThermalEfficiency
        {
            get { return HighLogic.LoadedSceneIsFlight ? (1 - getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT)) : 1; }
        }

        public double MaximumRecievePower
        {
            get
            {
                var power = maximumPower * powerMult;
                return CanBeActiveInAtmosphere ? power : power * highSpeedAtmosphereFactor;
            }
        }

        public void RegisterAsSlave(MicrowavePowerReceiver receiver)
        {
            thermalReceiverSlaves.Add(receiver);
        }

        public float MinimumThrottle { get { return 0; } }

        public void ConnectWithEngine(IEngineNoozle engine) { }

        public void DisconnectWithEngine(IEngineNoozle engine) { }

        public int SupportedPropellantAtoms { get { return GameConstants.defaultSupportedPropellantAtoms; } }

        public int SupportedPropellantTypes { get { return GameConstants.defaultSupportedPropellantTypes; } }

        public bool FullPowerForNonNeutronAbsorbants { get { return true; } }

        public float ThermalProcessingModifier { get { return thermalProcessingModifier; } }

        public double EfficencyConnectedThermalEnergyGenerator { get { return storedIsThermalEnergyGenratorActive; } }

        public double EfficencyConnectedChargedEnergyGenerator { get { return 0; } }

        public IElectricPowerSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerSource ConnectedChargedParticleElectricGenerator { get; set; }

        public void NotifyActiveThermalEnergyGenrator(double efficency, ElectricGeneratorType generatorType)
        {
            currentIsThermalEnergyGenratorActive = efficency;
        }

        public void NotifyActiveChargedEnergyGenrator(double efficency, ElectricGeneratorType generatorType) { }

        public bool IsThermalSource
        {
            get { return this.isThermalReceiver; }
        }

        public float RawMaximumPower { get { return (float)MaximumRecievePower; } }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType) { return false; }

        public void AttachThermalReciever(Guid key, float radius)
        {
            try
            {
                //UnityEngine.Debug.Log("[KSPI] - InterstellarReactor.ConnectReciever: Guid: " + key + " radius: " + radius);

                if (!connectedRecievers.ContainsKey(key))
                {
                    connectedRecievers.Add(key, radius);
                    connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                    connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
                }
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[KSPI] - InterstellarReactor.ConnectReciever exception: " + error.Message);
            }
        }

        public double ProducedWasteHeat { get { return (float)waste_heat_production; } }

        public void Refresh() { }

        public void DetachThermalReciever(Guid key)
        {
            if (connectedRecievers.ContainsKey(key))
            {
                connectedRecievers.Remove(key);
                connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
            }
        }

        public float GetFractionThermalReciever(Guid key)
        {
            float result;
            if (connectedRecieversFraction.TryGetValue(key, out result))
                return result;
            else
                return 0;
        }

        protected PartResource wasteheatResource;
        protected PartResource megajouleResource;

        protected Animation anim;
        protected Animation animT;

        protected MicrowavePowerTransmitter part_transmitter;
        protected ModuleAnimateGeneric genericAnimation;

        protected int connectedsatsi = 0;
        protected int connectedrelaysi = 0;
        protected int networkDepth = 0;
        protected long deactivate_timer = 0;
        protected double efficiency_d = 0;
        protected double powerInputMegajoules = 0;
        protected double powerInputKW = 0;
        protected double partBaseWasteheat;
        protected double partBaseMegajoules;

        protected bool has_transmitter = false;

        private static readonly double microwaveAngleTan = Math.Tan(GameConstants.microwave_angle);//this doesn't change during game so it's readonly 
        private static readonly double microwaveAngleTanSquared = microwaveAngleTan * microwaveAngleTan;

        public double ChargedPowerRatio { get { return 0; } }

        public float PowerBufferBonus { get { return 0; } }

        public float ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public float ThermalEnergyEfficiency { get { return thermalEnergyEfficiency; } }

        public float ChargedParticleEnergyEfficiency { get { return 0; } }

        public float ChargedParticlePropulsionEfficiency { get { return 0; } }

        public bool IsSelfContained { get { return false; } }

        public double CoreTemperature { get { return powerHeatBase; } }

        public double HotBathTemperature { get { return CoreTemperature * 1.5f; } }

        public double StableMaximumReactorPower { get { return RawMaximumPower; } }

        public double MaximumPower { get { return MaximumThermalPower; } }

        public double MaximumThermalPower { get { return ThermalPower; } }

        public double MaximumChargedPower { get { return 0; } }

        public double MinimumPower { get { return 0; } }

        public bool IsVolatileSource { get { return true; } }

        public bool IsActive { get { return receiverIsEnabled; } }

        public bool IsNuclear { get { return false; } }

        public float ThermalPropulsionEfficiency { get { return thermalPropulsionEfficiency; } }

        [KSPEvent(guiActive = true, guiName = "Link Receiver", active = true)]
        public void LinkReceiver()
        {
            linkedForRelay = true;

            ShowDeployAnumation(true);
        }

        [KSPEvent(guiActive = true, guiName = "Unlink Receiver", active = true)]
        public void UnlinkReceiver()
        {
            linkedForRelay = false;

            ShowUndeployAnimation(true);
        }


        [KSPEvent(guiActive = true, guiName = "Activate Receiver", active = true)]
        public void ActivateReceiver()
        {
            ActivateRecieverState();
        }

        private void ActivateRecieverState(bool forced = false)
        {
            receiverIsEnabled = true;

            ShowDeployAnumation(forced);
        }

        private void ShowDeployAnumation(bool forced)
        {
            if (anim != null)
            {
                if (forced || !animatonDeployed)
                {
                    waitForAnimationToComplete = true;
                    animatonDeployed = true;

                    if (anim[animName].normalizedTime == 1)
                        anim[animName].normalizedTime = 0;

                    anim[animName].speed = 1f;
                    anim.Blend(animName, 2f);
                }
            }

            if (genericAnimation != null)
            {
                genericAnimation.Toggle();
            }
        }

        [KSPEvent(guiActive = true, guiName = "Disable Receiver", active = true)]
        public void DisableReceiver()
        {
            DeactivateRecieverState();
        }

        private void DeactivateRecieverState(bool forced = false)
        {
            receiverIsEnabled = false;

            ShowUndeployAnimation(forced);
        }

        private void ShowUndeployAnimation(bool forced)
        {
            if (anim != null)
            {
                if (forced || animatonDeployed)
                {
                    waitForAnimationToComplete = true;
                    animatonDeployed = false;

                    if (anim[animName].normalizedTime == 0)
                        anim[animName].normalizedTime = 1;

                    anim[animName].speed = -1f;
                    anim.Blend(animName, 2f);
                }
            }

            if (genericAnimation != null)
            {
                genericAnimation.Toggle();
            }
        }

        [KSPAction("Activate Receiver")]
        public void ActivateReceiverAction(KSPActionParam param)
        {
            ActivateReceiver();
        }

        [KSPAction("Disable Receiver")]
        public void DisableReceiverAction(KSPActionParam param)
        {
            DisableReceiver();
        }

        [KSPAction("Toggle Receiver")]
        public void ToggleReceiverAction(KSPActionParam param)
        {
            if (receiverIsEnabled)
                DisableReceiver();
            else
                ActivateReceiver();
        }
        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_THERMALPOWER };

            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            instanceId = GetInstanceID();
            _radiusField = Fields["radius"];
            _coreTempereratureField = Fields["coreTempererature"];

            if (IsThermalSource)
            {
                _radiusField.guiActive = true;
                _radiusField.guiActiveEditor = true;
                _coreTempereratureField.guiActive = true;
                _coreTempereratureField.guiActiveEditor = true;
            }
            else
            {
                _radiusField.guiActive = false;
                _radiusField.guiActiveEditor = false;
                _coreTempereratureField.guiActive = false;
                _coreTempereratureField.guiActiveEditor = false;
            }

            if (state == StartState.Editor) { return; }

            if (isThermalReceiverSlave && part.parent != null)
            {
                mother = part.parent.FindModuleImplementing<MicrowavePowerReceiver>();
                if (mother != null)
                    mother.RegisterAsSlave(this);
                else if (part.parent.parent != null)
                {
                    mother = part.parent.parent.FindModuleImplementing<MicrowavePowerReceiver>();
                    if (mother != null)
                        mother.RegisterAsSlave(this);
                }
            }

            wasteheatResource = part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT];
            megajouleResource = part.Resources[FNResourceManager.FNRESOURCE_MEGAJOULES];

            // calculate WasteHeat Capacity
            partBaseWasteheat = part.mass * 1.0e+4 * wasteHeatMultiplier + (StableMaximumReactorPower * 0.05);
            if (wasteheatResource != null)
            {
                wasteheatResource.maxAmount = partBaseWasteheat;
                wasteheatResource.amount = wasteheatResource.maxAmount * wasteheatRatio;
            }

            // calculate Power Capacity
            partBaseMegajoules = StableMaximumReactorPower * 0.05;
            if (megajouleResource != null)
            {
                var ratio = Math.Max(1, megajouleResource.amount / megajouleResource.maxAmount);
                megajouleResource.maxAmount = partBaseMegajoules;
                megajouleResource.amount = partBaseMegajoules * ratio;
            }

            if (part.FindModulesImplementing<MicrowavePowerTransmitter>().Count == 1)
            {
                part_transmitter = part.FindModulesImplementing<MicrowavePowerTransmitter>().First();
                has_transmitter = true;
            }

            if (!String.IsNullOrEmpty(animTName))
            {
                animT = part.FindModelAnimators(animTName).FirstOrDefault();
                if (animT != null)
                {
                    animT[animTName].enabled = true;
                    animT[animTName].layer = 1;
                    animT[animTName].normalizedTime = 0f;
                    animT[animTName].speed = 0.001f;

                    animT.Sample();
                }
            }

            if (!String.IsNullOrEmpty(animName))
            {
                anim = part.FindModelAnimators(animName).FirstOrDefault();
            }

            if (!String.IsNullOrEmpty(animGenericName))
                genericAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == animGenericName);

            this.part.force_activate();

            if (part_transmitter == null)
            {
                if (receiverIsEnabled)
                {
                    ScreenMessages.PostScreenMessage("Microwave Receiver Activates", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                    ActivateRecieverState(true);
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Microwave Receiver Deactivates", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                    DeactivateRecieverState(true);
                }
            }
            else
            {
                if (genericAnimation != null)
                {
                    genericAnimation.Toggle();
                }
            }


        }

        public bool CanBeActiveInAtmosphere
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return true;

                if (anim == null)
                    return true;

                var pressure = FlightGlobals.getStaticPressure(vessel.transform.position) / 100;
                var dynamic_pressure = 0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325;

                if (dynamic_pressure <= 0) return true;

                var pressureLoad = dynamic_pressure / 1.4854428818159e-3 * 100;
                if (pressureLoad > 100 * atmosphereToleranceModifier)
                    return false;
                else
                    return true;
            }
        }

        public override void OnUpdate()
        {
            bool transmitter_on = has_transmitter && part_transmitter.isActive();
            bool canBeActive = CanBeActiveInAtmosphere;

            Events["LinkReceiver"].active = canLinkup && !linkedForRelay && !receiverIsEnabled && !transmitter_on && canBeActive;
            Events["UnlinkReceiver"].active = linkedForRelay;
            
            Events["ActivateReceiver"].active = !linkedForRelay && !receiverIsEnabled && !transmitter_on && canBeActive;
            Events["DisableReceiver"].active = receiverIsEnabled;
          
            Fields["toteff"].guiActive = (connectedsatsi > 0 || connectedrelaysi > 0);

            if (IsThermalSource)
                coreTempererature = CoreTemperature.ToString("0.0") + " K";

            if (receiverIsEnabled)
            {
                if (powerInputKW > 1000)
                    beamedpower = (powerInputKW / 1000).ToString("0.00") + "MW";
                else
                    beamedpower = powerInputKW.ToString("0.00") + "KW";
            }
            else
                beamedpower = "Offline.";

            connectedsats = string.Format("{0}/{1}", connectedsatsi, MicrowaveSources.instance.globalTransmitters.Count);
            connectedrelays = string.Format("{0}/{1}", connectedrelaysi, MicrowaveSources.instance.globalRelays.Count);
            networkDepthString = networkDepth.ToString();
            toteff = (efficiency_d * 100).ToString("0.00") + "%";

            if (receiverIsEnabled && anim != null && (!waitForAnimationToComplete || (!anim.isPlaying && waitForAnimationToComplete)))
            {
                waitForAnimationToComplete = false;

                if (connectedsatsi > 0 || connectedrelaysi > 0)
                {
                    if (!animatonDeployed)
                    {
                        //ScreenMessages.PostScreenMessage("Enable Microwave Receiver Tmp", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        animatonDeployed = true;

                        if (anim[animName].normalizedTime == 1f)
                            anim[animName].normalizedTime = 0f;

                        anim[animName].speed = 1f;
                        anim.Blend(animName, 2f);
                    }
                }
                else
                {
                    if (animatonDeployed)
                    {
                        //ScreenMessages.PostScreenMessage("Disable Microwave Receiver Tmp", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        animatonDeployed = false;

                        if (anim[animName].normalizedTime == 0)
                            anim[animName].normalizedTime = 1f;

                        anim[animName].speed = -1f;
                        anim.Blend(animName, 2f);
                    }
                }
            }
        }

        uint counter = 0;       // OnFixedUpdate cycle counter

        private double GetAtmosphericEfficiency(Vessel v)
        {
            return Math.Exp(-(FlightGlobals.getStaticPressure(v.transform.position) / 100) / 5);
        }

        private double GetAtmosphericEfficiency(double transmitterPresure, double recieverPressure, double waveLengthAbsorbtion, double distanceInMeter, Vessel recieverVessel, Vessel transmitterVessel) 
        {
            // if both in space, efficiency is 100%
            if (transmitterPresure == 0 && recieverPressure == 0)
                return 1;

            var atmosphereDepthInMeter = transmitterVessel.mainBody.atmosphereDepth;

            // calculate the weighted distance a signal has to travel through the atmosphere
            double atmosphericDistance;
            if (recieverVessel.mainBody == transmitterVessel.mainBody)
            {
                var recieverAltitudeModifier = recieverVessel.altitude > atmosphereDepthInMeter ? atmosphereDepthInMeter / recieverVessel.altitude : 1;
                var transmitterAltitudeModifier = transmitterVessel.altitude > atmosphereDepthInMeter ? atmosphereDepthInMeter / transmitterVessel.altitude : 1;
                atmosphericDistance = transmitterAltitudeModifier * recieverAltitudeModifier * distanceInMeter;
            }
            else
            {
                // use fixed atmospheric distance when not in the same SOI
                atmosphericDistance = atmosphereDepthInMeter * 2;
            }

            double absortion = Math.Pow(atmosphericDistance, Math.Sqrt(Math.Pow(transmitterPresure, 2) + Math.Pow(recieverPressure, 2))) / atmosphereDepthInMeter * waveLengthAbsorbtion;

            return Math.Exp(-absortion);
        }

        public override void OnFixedUpdate()
        {
            storedIsThermalEnergyGenratorActive = currentIsThermalEnergyGenratorActive;
            currentIsThermalEnergyGenratorActive = 0;
            wasteheatRatio = Math.Min(1, getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT));

            base.OnFixedUpdate();
            if (receiverIsEnabled)
            {
                if (wasteheatRatio >= 0.95 && !isThermalReceiver)
                {
                    receiverIsEnabled = false;
                    deactivate_timer++;
                    if (FlightGlobals.ActiveVessel == vessel && deactivate_timer > 2)
                        ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency microwave power shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);

                    return;
                }

                if ((++counter + instanceId) % 11 == 0)       // recalculate input once per 10 physics cycles. Relay route algorythm is too expensive
                {
                    double total_power = 0;
                    int activeSatsIncr = 0;
                    connectedsatsi = 0;
                    connectedrelaysi = 0;
                    networkDepth = 0;
                    deactivate_timer = 0;

                    efficiency_d = efficiencyPercentage * 0.01; 

                    HashSet<VesselRelayPersistence> usedRelays = new HashSet<VesselRelayPersistence>();
                    //Transmitters power calculation
                    foreach (var connectedTransmitterEntry in GetConnectedTransmitters())
                    {
                        VesselMicrowavePersistence transmitterPersistance = connectedTransmitterEntry.Key;

                        Vessel transmitterVessel = transmitterPersistance.Vessel;

                        // first reset owm recieved power to get correct amount recieved by others
                        received_power[transmitterVessel] = 0;

                        KeyValuePair<double, IEnumerable<VesselRelayPersistence>> keyvaluepair = connectedTransmitterEntry.Value;
                        routeEfficiency = keyvaluepair.Key;
                        IEnumerable<VesselRelayPersistence> relays = keyvaluepair.Value;

                        // calculate maximum power avialable from beamed power network
                        currentPowerUsageByOtherRecievers = MicrowavePowerReceiver.getEnumeratedPowerFromSatelliteForAllLoadedVesssels(transmitterPersistance);

                        // convert initial beamed power from source into MegaWatt
                        maxAvailablePowerFromSource = transmitterPersistance.getAvailablePower() / 1000;

                        // subtract any power already recieved by other recievers
                        remainingPowerFromSource = Math.Max(0, (maxAvailablePowerFromSource * routeEfficiency) - currentPowerUsageByOtherRecievers);

                        // take into account maximum route capacity
                        double satPowerCap = relays != null && relays.Count() > 0 ? Math.Min(remainingPowerFromSource, relays.Min(m => m.PowerCapacity)) : remainingPowerFromSource;

                        // determin power allowed power
                        var maxAllowedRecievalPower = MaximumRecievePower * Math.Min(ThermalEfficiency, (receiptPower / 100.0f));

                        // limit by amount of beampower the reciever is able to process
                        double satPower = Math.Min(maxAllowedRecievalPower, satPowerCap * efficiency_d); 

                        // register amount of raw power recieved
                        received_power[transmitterVessel] = efficiency_d > 0 ? satPower / efficiency_d : satPower;

                        // convert raw power into effecive power
                        total_power += satPower;

                        if (satPower > 0)
                        {
                            activeSatsIncr++;
                            if (relays != null)
                            {
                                foreach (var relay in relays)
                                {
                                    usedRelays.Add(relay);
                                }
                                networkDepth = Math.Max(networkDepth, relays.Count());
                            }
                        }
                    }

                    connectedsatsi = activeSatsIncr;
                    connectedrelaysi = usedRelays.Count;

                    powerInputMegajoules = total_power;
                    powerInputKW = powerInputMegajoules * 1000.0f;

                    // add alternator power
                    part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, -powerInputMegajoules * TimeWarp.fixedDeltaTime);
                }

                if (powerInputMegajoules > 0 && wasteheatResource != null)
                {
                    var ratio = wasteheatResource.amount / wasteheatResource.maxAmount;

                    wasteheatResource.maxAmount = partBaseWasteheat + powerInputMegajoules * TimeWarp.fixedDeltaTime;
                    wasteheatResource.amount = wasteheatResource.maxAmount * ratio;
                }

                if (isThermalReceiverSlave || isThermalReceiver)
                {
                    var cur_thermal_power = supplyFNResource(powerInputMegajoules * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER) / TimeWarp.fixedDeltaTime;
                    var total_thermal_power = isThermalReceiver ? cur_thermal_power + thermalReceiverSlaves.Sum(m => m.ThermalPower) : cur_thermal_power;

                    if (animT != null)
                    {
                        animT[animTName].normalizedTime = (float)Math.Min(total_thermal_power / maximumPower, 1);
                        animT.Sample();
                    }

                    if (ThermalPower <= 0)
                        ThermalPower = total_thermal_power;
                    else
                        ThermalPower = total_thermal_power * GameConstants.microwave_alpha + (1.0f - GameConstants.microwave_alpha) * ThermalPower;
                }
                else 
                {
                    supplyFNResource(powerInputMegajoules * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    waste_heat_production = powerInputMegajoules * (100 - efficiencyPercentage) * 0.01;
                    supplyFNResource(waste_heat_production * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
                }
            }
            else
            {
                connectedsatsi = 0;
                connectedrelaysi = 0;
                received_power.Clear();
                ThermalPower = 0;

                if (animT != null)
                {
                    animT[animTName].normalizedTime = 0;
                    animT.Sample();
                }
            }
        }

        public double MaxStableMegaWattPower
        {
            get { return isThermalReceiver ? 0 : powerInputMegajoules; }
        }

        public virtual float GetCoreTempAtRadiatorTemp(float rad_temp)
        {
            return 3500;
        }

        public double GetThermalPowerAtTemp(float temp)
        {
            return ThermalPower;
        }

        public float GetRadius()
        {
            return radius;
        }

        public bool isActive()
        {
            return receiverIsEnabled;
        }

        public bool shouldScaleDownJetISP()
        {
            return false;
        }

        public void EnableIfPossible()
        {
            if (!receiverIsEnabled)
                receiverIsEnabled = true;
        }

        public override string GetInfo()
        {
            return "Diameter: " + diameter + " m";
        }

        public double getPowerFromSatellite(VesselMicrowavePersistence vmp)
        {
            if (received_power.ContainsKey(vmp.Vessel) && receiverIsEnabled)
                return received_power[vmp.Vessel];

            return 0;
        }

        public static double getEnumeratedPowerFromSatelliteForAllLoadedVesssels(VesselMicrowavePersistence vmp)
        {
            double enumerated_power = 0;
            foreach (Vessel vess in FlightGlobals.Vessels)
            {
                List<MicrowavePowerReceiver> receivers = vess.FindPartModulesImplementing<MicrowavePowerReceiver>();
                foreach (MicrowavePowerReceiver receiver in receivers)
                {
                    enumerated_power += receiver.getPowerFromSatellite(vmp);
                }
            }
            return enumerated_power;
        }

        #region RelayRouting
        protected double ComputeVisibilityAndDistance(VesselRelayPersistence relay, Vessel targetVessel)
        {
            return PluginHelper.HasLineOfSightWith(relay.Vessel, targetVessel, 0)
                ? Vector3d.Distance(PluginHelper.getVesselPos(relay.Vessel), PluginHelper.getVesselPos(targetVessel))
                : -1;
        }

        protected double ComputeDistance(Vessel v1, Vessel v2)
        {
            return Vector3d.Distance(PluginHelper.getVesselPos(v1), PluginHelper.getVesselPos(v2));
        }

        protected double ComputeSpotSize(double distanceToSpot, double wavelength, double transmitterAperture)
        {
            if (transmitterAperture == 0)
                transmitterAperture = 1;

            if (wavelength == 0)
                wavelength = 0.03;

            var spotSize = (distanceToSpot * wavelength) / (transmitterAperture * PluginHelper.ApertureDiameterMult * apertureMultiplier);

            return spotSize;
        }

        protected double ComputeDistanceFacingEfficiency(double spotSize, double facingFactor, double recieverDiameter)
        {
            if (facingFactor == 0)
                return 0;

            if (recieverDiameter == 0)
                recieverDiameter = 1;

            var distanceFacingEfficiency = Math.Sqrt(Math.Min(1, recieverDiameter * facingFactor / spotSize));

            return distanceFacingEfficiency;
        }

        protected double ComputeFacingFactor(Vessel transmitterVessel)
        {
            // retrun if no recieval is possible
            if (highSpeedAtmosphereFactor == 0 && !CanBeActiveInAtmosphere)
                return 0;

            double facingFactor;

            Vector3d directionVector = (PluginHelper.getVesselPos(transmitterVessel) - this.vessel.transform.position).normalized;

            if (receiverType == 4) // used by single pivoting solar arrays
            {
                facingFactor = Math.Min(1 -Math.Abs(Vector3d.Dot(part.transform.right, directionVector)), 1);
            }
            else if (receiverType == 3)
            {
                //Scale energy reception based on angle of reciever to transmitter from back
                facingFactor = Math.Max(0, -Vector3d.Dot(part.transform.forward, directionVector));
            }
            else if (receiverType == 2)
            {
                // get the best result of inline and directed reciever
                facingFactor = Math.Min(1 - Math.Abs(Vector3d.Dot(part.transform.up, directionVector)), 1);
            }
            else if (receiverType == 1)
            {
                // recieve from sides
                facingFactor = Math.Min(1 - Math.Abs(Vector3d.Dot(part.transform.up, directionVector)), 1);
            }
            else // receiverType == 0
            {
                //Scale energy reception based on angle of reciever to transmitter from top
                facingFactor = Math.Max(0, Vector3d.Dot(part.transform.up, directionVector));
            }

            if (facingFactor > facingThreshold)
                facingFactor = Math.Pow(facingFactor - facingThreshold, facingExponent);
            else
                facingFactor = 0;

            if (receiverType == 2)
            {
                var facingFactorB = Math.Round(0.499 + Math.Max(0, Vector3d.Dot(part.transform.up, directionVector)));
                facingFactor = Math.Max(facingFactor, facingFactorB);
            }

            return CanBeActiveInAtmosphere ? facingFactor : highSpeedAtmosphereFactor * facingFactor;
        }

        /// <summary>
        /// Returns transmitters which to which this vessel can connect, route efficiency and relays used for each one.
        /// </summary>
        /// <param name="maxHops">Maximum number of relays which can be used for connection to transmitter</param>
        protected IDictionary<VesselMicrowavePersistence, KeyValuePair<double, IEnumerable<VesselRelayPersistence>>> GetConnectedTransmitters(int maxHops = 25)
        {
            //these two dictionaries store transmitters and relays and best currently known route to them which is replaced if better one is found. 

            var transmitterRouteDictionary = new Dictionary<VesselMicrowavePersistence, MicrowaveRoute>(); // stores all transmitter we can have a connection with
            var relayRouteDictionary = new Dictionary<VesselRelayPersistence, MicrowaveRoute>();

            var transmittersToCheck = new List<VesselMicrowavePersistence>();//stores all transmiters to which we want to connect

            double recieverAtmosphericPresure = FlightGlobals.getStaticPressure(this.vessel.transform.position) / 100;

            foreach (VesselMicrowavePersistence transmitter in MicrowaveSources.instance.globalTransmitters.Values)
            {
                //first check for direct connection from current vessel to transmitters, will always be optimal
                if (transmitter.getAvailablePower() <= 0) continue;

                //ignore if no power or transmitter is on the same vessel
                if (transmitter.Vessel == vessel) continue;

                if (PluginHelper.HasLineOfSightWith(this.vessel, transmitter.Vessel))
                {
                    var possibleWavelengths = new List<MicrowaveRoute>();
                    double distanceInMeter = ComputeDistance(this.vessel, transmitter.Vessel);
                    double facingFactor = ComputeFacingFactor(transmitter.Vessel);
                    double transmitterAtmosphericPresure = FlightGlobals.getStaticPressure(transmitter.Vessel.transform.position) / 100;

                    foreach (var wavelenghtData in transmitter.SupportedTransmitWavelengths)
                    {
                        if (wavelenghtData.wavelength.NotWithin(this.maximumWavelength, this.minimumWavelength))
                            continue;

                        double spotsize = ComputeSpotSize(distanceInMeter, wavelenghtData.wavelength, transmitter.Aperture);
                        double distanceFacingEfficiency = ComputeDistanceFacingEfficiency(spotsize, facingFactor, this.diameter);
                        double atmosphereEfficency = GetAtmosphericEfficiency(transmitterAtmosphericPresure, recieverAtmosphericPresure, wavelenghtData.atmosphericAbsorption, distanceInMeter, this.vessel, transmitter.Vessel);
                        double transmitterEfficency = distanceFacingEfficiency * atmosphereEfficency;

                        possibleWavelengths.Add(new MicrowaveRoute(transmitterEfficency, distanceInMeter, facingFactor, spotsize)); 
                    }

                    var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null : 
                        possibleWavelengths.SingleOrDefault(m => m.Efficiency ==  possibleWavelengths.Max(n => n.Efficiency));

                    if (mostEfficientWavelength != null)
                    {
                        //store in dictionary that optimal route to this transmitter is direct connection, can be replaced if better route is found
                        transmitterRouteDictionary[transmitter] = mostEfficientWavelength;
                    }
                }

                // add all tranmitters that are not located on the recieving vessel
                transmittersToCheck.Add(transmitter);
            }

            //this algorithm processes relays in groups in which elements of the first group must be visible from receiver, 
            //elements from the second group must be visible by at least one element from previous group and so on...

            var relaysToCheck = new List<VesselRelayPersistence>();//relays which we have to check - all active relays will be here
            var currentRelayGroup = new List<KeyValuePair<VesselRelayPersistence, int>>();//relays which are in line of sight, and we have not yet checked what they can see. Their index in relaysToCheck is also stored

            int relayIndex = 0;
            foreach (VesselRelayPersistence relay in MicrowaveSources.instance.globalRelays.Values)
            {
                if (!relay.IsActive) continue;

                if (PluginHelper.HasLineOfSightWith(this.vessel, relay.Vessel))
                {
                    var possibleWavelengths = new List<MicrowaveRoute>();
                    double distanceInMeter = ComputeDistance(this.vessel, relay.Vessel);
                    double facingFactor = ComputeFacingFactor(relay.Vessel);
                    double transmitterAtmosphericPresure = FlightGlobals.getStaticPressure(relay.Vessel.transform.position) / 100;

                    foreach (var wavelenghtData in relay.SupportedTransmitWavelengths)
                    {
                        if (wavelenghtData.wavelength.NotWithin(this.maximumWavelength, this.minimumWavelength))
                            continue;

                        double spotsize = ComputeSpotSize(distanceInMeter, wavelenghtData.wavelength, relay.Aperture);
                        double distanceFacingEfficiency = ComputeDistanceFacingEfficiency(spotsize, facingFactor, this.diameter);

                        double atmosphereEfficency = GetAtmosphericEfficiency(transmitterAtmosphericPresure, recieverAtmosphericPresure, wavelenghtData.atmosphericAbsorption, distanceInMeter, this.vessel, relay.Vessel);
                        double transmitterEfficency = distanceFacingEfficiency * atmosphereEfficency;

                        possibleWavelengths.Add(new MicrowaveRoute(transmitterEfficency, distanceInMeter, facingFactor, spotsize));
                    }

                    var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null :
                        possibleWavelengths.SingleOrDefault(m => m.Efficiency == possibleWavelengths.Max(n => n.Efficiency));

                    if (mostEfficientWavelength != null)
                    {
                        //store in dictionary that optimal route to this relay is direct connection, can be replaced if better route is found
                        relayRouteDictionary[relay] = mostEfficientWavelength;
                        currentRelayGroup.Add(new KeyValuePair<VesselRelayPersistence, int>(relay, relayIndex));
                    }
                }
                relaysToCheck.Add(relay);
                relayIndex++;
            }

            int hops = 0; //number of hops between relays

            //pre-compute distances and visibility thus limiting number of checks to (Nr^2)/2 + NrNt +Nr + Nt
            if (hops < maxHops && transmittersToCheck.Any())
            {
                double[,] relayToRelayDistances = new double[relaysToCheck.Count, relaysToCheck.Count];
                double[,] relayToTransmitterDistances = new double[relaysToCheck.Count, transmittersToCheck.Count];

                for (int i = 0; i < relaysToCheck.Count; i++)
                {
                    var relay = relaysToCheck[i];
                    for (int j = i + 1; j < relaysToCheck.Count; j++)
                    {
                        double visibilityAndDistance = ComputeVisibilityAndDistance(relay, relaysToCheck[j].Vessel);
                        relayToRelayDistances[i, j] = visibilityAndDistance;
                        relayToRelayDistances[j, i] = visibilityAndDistance;
                    }
                    for (int t = 0; t < transmittersToCheck.Count; t++)
                    {
                        relayToTransmitterDistances[i, t] = ComputeVisibilityAndDistance(relay, transmittersToCheck[t].Vessel);
                    }
                }

                HashSet<int> coveredRelays = new HashSet<int>();

                //runs as long as there is any relay to which we can connect and maximum number of hops have not been breached
                while (hops < maxHops && currentRelayGroup.Any())
                {
                    var nextRelayGroup = new List<KeyValuePair<VesselRelayPersistence, int>>();//will put every relay which is in line of sight of any relay from currentRelayGroup here
                    foreach (var relayEntry in currentRelayGroup) //relays visible from receiver in first iteration, then relays visible from them etc....
                    {
                        VesselRelayPersistence relayPersistance = relayEntry.Key;
                        MicrowaveRoute relayRoute = relayRouteDictionary[relayPersistance];// current best route for this relay
                        double relayRouteFacingFactor = relayRoute.FacingFactor;// it's always facing factor from the beggining of the route
                        double relayAtmosphericPresure = FlightGlobals.getStaticPressure(relayPersistance.Vessel.transform.position) / 100;

                        for (int t = 0; t < transmittersToCheck.Count; t++)//check if this relay can connect to transmitters
                        {
                            double distanceInMeter = relayToTransmitterDistances[relayEntry.Value, t];

                            //it's >0 if it can see
                            if (distanceInMeter <= 0) continue;

                            VesselMicrowavePersistence transmitterToCheck = transmittersToCheck[t];
                            double newDistance = relayRoute.Distance + distanceInMeter;// total distance from receiver by this relay to transmitter
                            double transmitterAtmosphericPresure = FlightGlobals.getStaticPressure(transmitterToCheck.Vessel.transform.position) / 100;
                            var possibleWavelengths = new List<MicrowaveRoute>();

                            foreach (var transmitterWavelenghtData in transmitterToCheck.SupportedTransmitWavelengths)
                            {
                                if (transmitterWavelenghtData.wavelength.NotWithin(relayPersistance.MaximumRelayWavelenght, relayPersistance.MinimumRelayWavelenght))
                                    continue;

                                double spotsize = ComputeSpotSize(distanceInMeter, transmitterWavelenghtData.wavelength, transmitterToCheck.Aperture);
                                double distanceFacingEfficiency = ComputeDistanceFacingEfficiency(spotsize, 1, relayPersistance.Aperture);

                                double atmosphereEfficency = GetAtmosphericEfficiency(transmitterAtmosphericPresure, relayAtmosphericPresure, transmitterWavelenghtData.atmosphericAbsorption, distanceInMeter, transmitterToCheck.Vessel, relayPersistance.Vessel);
                                double efficiencyTransmitterToRelay = distanceFacingEfficiency * atmosphereEfficency;
                                double efficiencyForRoute = efficiencyTransmitterToRelay * relayRoute.Efficiency;

                                possibleWavelengths.Add(new MicrowaveRoute(efficiencyForRoute, newDistance, relayRouteFacingFactor, spotsize, relayPersistance));
                            }

                             var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null :
                                    possibleWavelengths.SingleOrDefault(m => m.Efficiency == possibleWavelengths.Max(n => n.Efficiency));

                            if (mostEfficientWavelength != null)
                            {
                                //this will return true if there is already a route to this transmitter
                                MicrowaveRoute currentOptimalRoute;
                                if (transmitterRouteDictionary.TryGetValue(transmitterToCheck, out currentOptimalRoute))
                                {
                                    if (currentOptimalRoute.Efficiency < mostEfficientWavelength.Efficiency)
                                    {
                                        //if route using this relay is better then replace the old route
                                        transmitterRouteDictionary[transmitterToCheck] = mostEfficientWavelength;
                                    }
                                }
                                else
                                {
                                    //there is no other route to this transmitter yet known so algorithm puts this one as optimal
                                    transmitterRouteDictionary[transmitterToCheck] = mostEfficientWavelength;
                                }
                            }
                        }

                        for (int r = 0; r < relaysToCheck.Count; r++)
                        {
                            var nextRelay = relaysToCheck[r];
                            if (nextRelay == relayPersistance)
                                continue;

                            double distanceToNextRelay = relayToRelayDistances[relayEntry.Value, r];
                            if (distanceToNextRelay <= 0) continue;

                            var possibleWavelengths = new List<MicrowaveRoute>();
                            var relayToNextRelayDistance = relayRoute.Distance + distanceToNextRelay;

                            foreach (var transmitterWavelenghtData in relayEntry.Key.SupportedTransmitWavelengths)
                            {
                                if (transmitterWavelenghtData.wavelength.NotWithin(relayPersistance.MaximumRelayWavelenght, relayPersistance.MinimumRelayWavelenght))
                                    continue;

                                double spotsize = ComputeSpotSize(distanceToNextRelay, transmitterWavelenghtData.wavelength, relayEntry.Key.Aperture);
                                double efficiencyByThisRelay = ComputeDistanceFacingEfficiency(spotsize, 1, relayPersistance.Aperture);
                                double efficiencyForRoute = efficiencyByThisRelay * relayRoute.Efficiency;

                                possibleWavelengths.Add(new MicrowaveRoute(efficiencyForRoute, relayToNextRelayDistance, relayRouteFacingFactor, spotsize, relayPersistance));
                            }

                            var mostEfficientWavelength = possibleWavelengths.Count == 0 ? null :
                                possibleWavelengths.SingleOrDefault(m => m.Efficiency == possibleWavelengths.Max(n => n.Efficiency));

                            if (mostEfficientWavelength != null)
                            {
                                MicrowaveRoute currentOptimalPredecessor;
                                if (relayRouteDictionary.TryGetValue(nextRelay, out currentOptimalPredecessor))
                                //this will return true if there is already a route to next relay
                                {
                                    //if route using this relay is better
                                    if (currentOptimalPredecessor.Efficiency < mostEfficientWavelength.Efficiency)
                                    {
                                        //we put it in dictionary as optimal
                                        relayRouteDictionary[nextRelay] = mostEfficientWavelength;
                                    }
                                }
                                else //there is no other route to this relay yet known so we put this one as optimal
                                {
                                    relayRouteDictionary[nextRelay] = mostEfficientWavelength;
                                }

                                if (!coveredRelays.Contains(r))
                                {
                                    nextRelayGroup.Add(new KeyValuePair<VesselRelayPersistence, int>(nextRelay, r));
                                    //in next iteration we will check what next relay can see
                                    coveredRelays.Add(r);
                                }
                            }
                        }
                    }
                    currentRelayGroup = nextRelayGroup;
                    //we don't have to check old relays so we just replace whole List
                    hops++;
                }

            }

            //building final result
            var resultDictionary = new Dictionary<VesselMicrowavePersistence, KeyValuePair<double, IEnumerable<VesselRelayPersistence>>>();

            foreach (var transmitterEntry in transmitterRouteDictionary)
            {
                Stack<VesselRelayPersistence> relays = new Stack<VesselRelayPersistence>();//Last in, first out so relay visible from receiver will always be first
                VesselRelayPersistence relay = transmitterEntry.Value.PreviousRelay;
                while (relay != null)
                {
                    relays.Push(relay);
                    relay = relayRouteDictionary[relay].PreviousRelay;
                }
                resultDictionary.Add(transmitterEntry.Key, new KeyValuePair<double, IEnumerable<VesselRelayPersistence>>(transmitterEntry.Value.Efficiency, relays));
                //Debug.Log("[KSP Interstellar]:   Add to Result Dictionary Transmitter power: " + transmitterEntry.Key.NuclearPower + " with route efficiency " + transmitterEntry.Value.Efficiency);
            }

            return resultDictionary; //connectedTransmitters;
        }
        #endregion RelayRouting

    }


}
