using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Microwave;
using FNPlugin.Redist;
using FNPlugin.Extensions;

namespace FNPlugin.Beamedpower
{
    class PhasedArrayTransmitter : BeamedPowerTransmitter { }

    class MicrowavePowerTransmitter : BeamedPowerTransmitter { }

    class BeamedPowerLaserTransmitter : BeamedPowerTransmitter { }

    class BeamedPowerTransmitter : ResourceSuppliableModule, IMicrowavePowerTransmitter, IScalarModule
    {
        //Persistent 
        [KSPField(isPersistant = true)]
        public string partId;
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool relay;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Can Relay")]
        public bool canRelay;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Merging Beams")]
        public bool mergingBeams;
        [KSPField(isPersistant = true)]
        public double nuclear_power = 0;
        [KSPField(isPersistant = true)]
        public double solar_power = 0;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Power Capacity", guiUnits = " MW", guiFormat = "F2")]
        public double power_capacity = 0;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Transmit WaveLength m", guiFormat = "F8", guiUnits = " m")]
        public double wavelength = 0;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Transmit WaveLength SI")]
        public string wavelengthText;
        [KSPField(isPersistant = true)]
        public double atmosphericAbsorption = 0.1;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Min Relay WaveLength", guiFormat = "F8", guiUnits = " m")]
        public double minimumRelayWavelenght;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Max Relay WaveLength", guiFormat = "F8", guiUnits = " m")]
        public double maximumRelayWavelenght;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Transmit WaveLength WL Name")]
        public string wavelengthName;
        [KSPField(isPersistant = true)]
        public double aperture = 1;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName= "Is Mirror")]
        public bool isMirror = false;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Can merge beams")]
        public bool isBeamMerger = false;
        [KSPField(isPersistant = true)]
        public bool forceActivateAtStartup = false;
        [KSPField(isPersistant = true)]
        public bool hasLinkedReceivers = false;

        //Non Persistent 
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Air Absorbtion Percentage")]
        public double atmosphericAbsorptionPercentage;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Water Absorbtion Percentage")]
        public double waterAbsorptionPercentage;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Absorbtion Percentage", guiFormat = "F4", guiUnits = "%")]
        public double totalAbsorptionPercentage;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Body")]
        public string body_name;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Biome Name")]
        public string biome_desc;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Moisture Modifier", guiFormat = "F4")]
        public double moistureModifier;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public bool canFunctionOnSurface = true;

        [KSPField(isPersistant = false)]
        public double maximumPower = 10000;
        [KSPField(isPersistant = false)]
        public float atmosphereToleranceModifier = 1;
        [KSPField(isPersistant = false)]
        public string animName = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Can Transmit")]
        public bool canTransmit = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Build in Relay")]
        public bool buildInRelay = false;
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public int compatibleBeamTypes = 1;


        [KSPField(isPersistant = true, guiActiveEditor = true)]
        public double nativeWaveLength = 0.003189281;
        [KSPField(isPersistant = true, guiActiveEditor = false)]
        public double nativeAtmosphericAbsorptionPercentage = 10;

        //GUI 
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Aperture Diameter", guiFormat = "F2", guiUnits = " m")]
        public double apertureDiameter = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusStr;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Transmission Efficiency", guiUnits = "%")]
        public double transmissionEfficiencyPercentage;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Transmission Strength"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]
        public float transmitPower = 100;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Wall to Beam Power")]
        public string beamedpower;

        [KSPField]
        public bool canBeActive;
        [KSPField]
        protected int nearbyPartsCount;

        // Near Future Compatibility properties
        [KSPField(isPersistant = false)]
        public double powerMult = 1;
        [KSPField(isPersistant = false)]
        public double powerHeatMultiplier = 1;

        protected string scalarModuleID = Guid.NewGuid().ToString();
        protected EventData<float, float> onMoving;
        protected EventData<float> onStop;

        //Internal
        protected Animation anim;
        protected List<ISolarPower> solarCells;
        protected BeamedPowerReceiver part_receiver;
        protected List<BeamedPowerReceiver> vessel_recievers;
        protected BeamGenerator activeBeamGenerator;
        protected List<BeamGenerator> beamGenerators;
        protected ModuleAnimateGeneric genericAnimation;  

        public bool CanMove { get { return true; } }

        public float GetScalar { get { return 1; } }

        public EventData<float, float> OnMoving { get { return onMoving; } }

        public EventData<float> OnStop { get { return onStop; } }

        public string ScalarModuleID { get { return scalarModuleID; } }

        public bool IsMoving()
        {
            return anim != null ? anim.isPlaying : false;
        }

        public void SetScalar(float t)
        {
            if (anim != null)
            {
                if (t > 0.5)
                {
                    anim[animName].speed = 1;
                    anim[animName].normalizedTime = 0;
                    anim.Blend(animName, part.mass);
                }
                else
                {
                    anim[animName].speed = -1;
                    anim[animName].normalizedTime = 1;
                    anim.Blend(animName, part.mass);
                }
            }
        }

        public void SetUIRead(bool state)
        {
            // ignore
        }
        public void SetUIWrite(bool state)
        {
            // ignore
        }

        [KSPEvent(guiActive = true, guiName = "Activate Transmitter", active = false)]
        public void ActivateTransmitter()
        {
            if (relay) return;

            this.part.force_activate();
            forceActivateAtStartup = true;

            if (anim != null)
            {
                anim[animName].speed = 1;
                anim[animName].normalizedTime = 0;
                anim.Blend(animName, part.mass);
            }
            IsEnabled = true;

            // update wavelength
            this.wavelength = Wavelength;
            minimumRelayWavelenght = wavelength * 0.99;
            maximumRelayWavelenght = wavelength * 1.01;

            this.wavelengthText = WavelenthToText(wavelength);
            this.wavelengthName = WavelengthName;
            atmosphericAbsorption = CombinedAtmosphericAbsorption;
        }

        private string WavelenthToText( double wavelength)
        {
            if (wavelength > 1.0e-3)
                return (wavelength * 1.0e+3).ToString() + " mm";
            else if (wavelength > 7.5e-7)
                return (wavelength * 1.0e+6).ToString() + " µm";
            else if (wavelength > 1.0e-9)
                return (wavelength * 1.0e+9).ToString() + " nm";
            else
                return (wavelength * 1.0e+12).ToString() + " pm";
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Transmitter", active = false)]
        public void DeactivateTransmitter()
        {
            if (relay) return;

            ScreenMessages.PostScreenMessage("Transmitter deactivated", 4.0f, ScreenMessageStyle.UPPER_CENTER);
 
            if (anim != null)
            {
                anim[animName].speed = -1;
                anim[animName].normalizedTime = 1;
                anim.Blend(animName, part.mass);
            }
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Relay", active = false)]
        public void ActivateRelay()
        {
            if (IsEnabled || relay) return;

            if (anim != null)
            {
                anim[animName].speed = 1;
                anim[animName].normalizedTime = 0;
                anim.Blend(animName, part.mass);
            }

            vessel_recievers = this.vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != this.part).ToList();

            UpdateRelayWavelength();

            relay = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Relay", active = false)]
        public void DeactivateRelay()
        {
            if (!relay) return;

            ScreenMessages.PostScreenMessage("Relay deactivated", 4, ScreenMessageStyle.UPPER_CENTER);

            if (anim != null)
            {
                anim[animName].speed = 1;
                anim[animName].normalizedTime = 0;
                anim.Blend(animName, part.mass);
            }

            relay = false;
        }

        private void UpdateRelayWavelength()
        {
            // update stored variables
            this.wavelength = Wavelength;
            this.wavelengthText = WavelenthToText(wavelength);
            this.wavelengthName = WavelengthName;
            this.atmosphericAbsorption = CombinedAtmosphericAbsorption;

            if (isMirror)
            {
                this.hasLinkedReceivers = true;
                return;
            }

            // collected all recievers relevant for relay
            var recieversConfiguredForRelay = vessel_recievers.Where(m => m.linkedForRelay).ToList();

            // add build in relay if it can be used for relay
            if (part_receiver != null && buildInRelay)
                recieversConfiguredForRelay.Add(part_receiver);

            // determin if we can activat relay
            this.hasLinkedReceivers = recieversConfiguredForRelay.Count > 0;

            // use all avialable recievers
            if (this.hasLinkedReceivers)
            {
                minimumRelayWavelenght = recieversConfiguredForRelay.Min(m => m.minimumWavelength);
                maximumRelayWavelenght = recieversConfiguredForRelay.Max(m => m.maximumWavelength);
            }
        }

        [KSPAction("Activate Transmitter")]
        public void ActivateTransmitterAction(KSPActionParam param)
        {
            ActivateTransmitter();
        }

        [KSPAction("Deactivate Transmitter")]
        public void DeactivateTransmitterAction(KSPActionParam param)
        {
            DeactivateTransmitter();
        }

        [KSPAction("Activate Relay")]
        public void ActivateRelayAction(KSPActionParam param)
        {
            ActivateRelay();
        }

        [KSPAction("Deactivate Relay")]
        public void DeactivateRelayAction(KSPActionParam param)
        {
            DeactivateRelay();
        }

        public override void OnStart(PartModule.StartState state)
        {
            onMoving = new EventData<float, float>("transmitterMoving");
            onStop = new EventData<float>("transmitterStop");

            power_capacity = maximumPower * powerMult;

            if (String.IsNullOrEmpty(partId))
                partId = Guid.NewGuid().ToString();

            // store  aperture
            aperture = apertureDiameter;

            ConnectToBeamGenerator();

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                return;
            }

            genericAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().FirstOrDefault(m => m.animationName == animName);

            solarCells = vessel.FindPartModulesImplementing<ISolarPower>();

            vessel_recievers = this.vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != this.part).ToList();
            part_receiver = part.FindModulesImplementing<BeamedPowerReceiver>().FirstOrDefault();

            UpdateRelayWavelength();

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if ( anim != null &&  part_receiver == null)
            {
                anim[animName].layer = 1;
                if (IsEnabled)
                {
                    anim[animName].normalizedTime = 0;
                    anim[animName].speed = 1;
                }
                else
                {
                    anim[animName].normalizedTime = 1;
                    anim[animName].speed = -1;
                }
                //anim.Play();
                anim.Blend(animName, part.mass);
            }

            if (forceActivateAtStartup)
                this.part.force_activate();
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            ConnectToBeamGenerator();
        }

        private void ConnectToBeamGenerator()
        {
            try
            {
                // connect with bbeam gnerators 
                beamGenerators = part.FindModulesImplementing<BeamGenerator>().Where(m => (m.beamType & compatibleBeamTypes) == m.beamType).ToList();

                if (beamGenerators.Count == 0 && part.parent != null)
                {
                    beamGenerators.AddRange(part.parent.FindModulesImplementing<BeamGenerator>().Where(m => (m.beamType & compatibleBeamTypes) == m.beamType));
                }

                if (beamGenerators.Count == 0)
                {
                    var attachedParts = part.attachNodes.Where(m => m.attachedPart != null).Select(m => m.attachedPart).ToList();

                    var parentParts = attachedParts.Where(m => m.parent != null && m.parent != this.part).Select(m => m.parent).ToList();
                    var indirectParts = attachedParts.SelectMany(m => m.attachNodes.Where(l => l.attachedPart != null && l.attachedPart != this.part).Select(l => l.attachedPart)).ToList();

                    attachedParts.AddRange(indirectParts);
                    attachedParts.AddRange(parentParts);

                    var nearbyParts = attachedParts.Distinct().ToList();
                    nearbyPartsCount = nearbyParts.Count();

                    var nearbyGenerators = nearbyParts.Select(m => m.FindModuleImplementing<BeamGenerator>()).Where(l => l != null);
                    var availableGenerators = nearbyGenerators.SelectMany(m => m.FindBeamGenerators(m.part)).Where(m => (m.beamType & compatibleBeamTypes) == m.beamType).Distinct();

                    beamGenerators.AddRange(availableGenerators);
                }

                activeBeamGenerator = beamGenerators.FirstOrDefault();

                if (activeBeamGenerator != null && activeBeamGenerator.part != this.part)
                    activeBeamGenerator.UpdateMass(this.maximumPower);
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] Microwave Transmitter OnStart search for beamGenerator: " + ex);
            }
        }

        public bool CanBeActive
        {
            get
            {
                if (anim == null) 
                    return true;

                var pressure = part.atmDensity;      
                var dynamic_pressure = 0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325.0;

                if (dynamic_pressure <= 0) return true;

                var pressureLoad = (dynamic_pressure / 1.4854428818159e-3) * 100;
                if (pressureLoad > 100 * atmosphereToleranceModifier)
                    return false;
                else 
                    return true;
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                power_capacity = maximumPower * powerMult;
                return;
            }

            UpdateRelayWavelength();

            totalAbsorptionPercentage = atmosphericAbsorptionPercentage + waterAbsorptionPercentage;
            atmosphericAbsorption = totalAbsorptionPercentage / 100;

            bool vesselInSpace = (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL);
            bool receiver_on = part_receiver != null && part_receiver.isActive();
            canBeActive = CanBeActive;

            if (anim != null && !canBeActive && IsEnabled && part.vessel.isActiveVessel && !CheatOptions.UnbreakableJoints)
            {
                if (relay)
                {
                    var message = "Disabled relay because of static pressure atmosphere";
                    ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                    Debug.Log("KSPI - " + message);
                    DeactivateRelay();
                }
                else
                {
                    var message = "Disabled transmitter because of static pressure atmosphere";
                    ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                    Debug.Log("KSPI - " + message);
                    DeactivateTransmitter();
                }
            }

            var canOperateInCurrentEnvironment = this.canFunctionOnSurface || vesselInSpace;
            var vesselCanTransmit = canTransmit && canOperateInCurrentEnvironment;

            Events["ActivateTransmitter"].active = activeBeamGenerator != null && vesselCanTransmit && !IsEnabled && !relay && !receiver_on && canBeActive;
            Events["DeactivateTransmitter"].active = IsEnabled;

            canRelay = this.hasLinkedReceivers && canOperateInCurrentEnvironment;

            Events["ActivateRelay"].active = canRelay && !IsEnabled && !relay && !receiver_on && canBeActive;
            Events["DeactivateRelay"].active = relay;

            mergingBeams = IsEnabled && canRelay && isBeamMerger;

            bool isTransmitting = IsEnabled && !relay;

            Fields["apertureDiameter"].guiActive = isTransmitting; 
            Fields["beamedpower"].guiActive = isTransmitting && canBeActive;
            Fields["transmitPower"].guiActive = part_receiver == null || !part_receiver.isActive();

            bool isLinkedForRelay = part_receiver != null && part_receiver.linkedForRelay;

            bool receiverNotInUse = !isLinkedForRelay && !receiver_on && !IsRelay;

            Fields["moistureModifier"].guiActive = receiverNotInUse;
            Fields["totalAbsorptionPercentage"].guiActive = receiverNotInUse;
            Fields["wavelength"].guiActive = receiverNotInUse;
            Fields["wavelengthName"].guiActive = receiverNotInUse;

            if (IsEnabled)
            {
                statusStr = "Transmitter Active";
            }
            else if (relay)
            {
                statusStr = "Relay Active";
            }
            else
            {
                if (isLinkedForRelay)
                    statusStr = "Is Linked For Relay";
                else if (receiver_on)
                    statusStr = "Receiver active";
                else if (canRelay)
                    statusStr = "Is ready for relay";
                else if (beamGenerators.Count == 0)
                    statusStr = "No beam generator found";
                else
                    statusStr = "Inactive.";
            }

            if (activeBeamGenerator == null)
            {
                var wavelegthField = Fields["wavelength"];
                wavelegthField.guiActive = false;
                wavelegthField.guiActiveEditor = false;

                var atmosphericAbsorptionPercentageField = Fields["atmosphericAbsorptionPercentage"];
                atmosphericAbsorptionPercentageField.guiActive = false;
                atmosphericAbsorptionPercentageField.guiActiveEditor = false;

                var waterAbsorptionPercentageField = Fields["waterAbsorptionPercentage"];
                waterAbsorptionPercentageField.guiActive = false;
                waterAbsorptionPercentageField.guiActiveEditor = false;

                return;
            }

            wavelength = activeBeamGenerator.wavelength;
            wavelengthText = WavelenthToText(wavelength);
            atmosphericAbsorptionPercentage = activeBeamGenerator.atmosphericAbsorptionPercentage;
            waterAbsorptionPercentage = activeBeamGenerator.waterAbsorptionPercentage * moistureModifier;

            double inputPower = nuclear_power + solar_power;
            if (inputPower > 1000)
            {
                if (inputPower > 1e6)
                    beamedpower = (inputPower / 1e6).ToString("0.000") + " GW";
                else
                    beamedpower = (inputPower / 1000).ToString("0.000") + " MW";
            }
            else
                beamedpower = inputPower.ToString("0.000") + " KW";

            solarCells = vessel.FindPartModulesImplementing<ISolarPower>();
        }

        public override void OnFixedUpdate()
        {
            if (!part.enabled)
                base.OnFixedUpdate();
        }

        public void FixedUpdate()
        {
            if (activeBeamGenerator != null)
                transmissionEfficiencyPercentage = activeBeamGenerator.efficiencyPercentage;

            if (!HighLogic.LoadedSceneIsFlight) return;

            nuclear_power = 0;
            solar_power = 0;

            CollectBiomeData();

            base.OnFixedUpdate();

            if (activeBeamGenerator != null && IsEnabled && !relay)
            {
                double reactorPowerTransmissionRatio = transmitPower / 100d;
                double transmissionWasteRatio = (100 - activeBeamGenerator.efficiencyPercentage) / 100d;
                double transmissionEfficiencyRatio = activeBeamGenerator.efficiencyPercentage / 100d;

                double requestedPower;

                if (CheatOptions.InfiniteElectricity)
                {
                    requestedPower = power_capacity;
                }
                else
                {
                    var availablePower = getAvailableStableSupply(ResourceManager.FNRESOURCE_MEGAJOULES); 
                    var resourceBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);

                    var effectiveResourceThrotling = resourceBarRatio > ResourceManager.ONE_THIRD ? 1 : resourceBarRatio * 3;

                    requestedPower = Math.Min(power_capacity, effectiveResourceThrotling * availablePower * reactorPowerTransmissionRatio);
                }

                var receivedPower = CheatOptions.InfiniteElectricity
                    ? requestedPower
                    : consumeFNResourcePerSecond(requestedPower, ResourceManager.FNRESOURCE_MEGAJOULES);

                nuclear_power += 1000 * transmissionEfficiencyRatio * receivedPower;

                solar_power += 1000 * transmissionEfficiencyRatio * solarCells.Sum(m => m.SolarPower);

                // generate wasteheat for converting electric power to beamed power
                if (!CheatOptions.IgnoreMaxTemperature)
                    supplyFNResourcePerSecond(receivedPower * transmissionWasteRatio, ResourceManager.FNRESOURCE_WASTEHEAT);
            }

            // extract solar power from stable power
            nuclear_power -= solar_power;

            if (double.IsInfinity(nuclear_power) || double.IsNaN(nuclear_power) || nuclear_power < 0)
                nuclear_power = 0;

            if (double.IsInfinity(solar_power) || double.IsNaN(solar_power) || solar_power < 0)
                solar_power = 0;
        }

        private void CollectBiomeData()
        {
            try
            {
                moistureModifier = 0;
                biome_desc = string.Empty;

                if (part.vessel == null) return;

                double lat = vessel.latitude * Math.PI / 180d;
                double lon = vessel.longitude * Math.PI / 180d;

                if (part.vessel.mainBody == null) return;

                body_name = part.vessel.mainBody.name;

                if (part.vessel.mainBody.BiomeMap == null) return;

                var attribute = part.vessel.mainBody.BiomeMap.GetAtt(lat, lon);

                if (attribute == null) return;

                biome_desc = attribute.name;

                double cloud_variance;
                if (body_name == "Kerbin")
                {
                    if (biome_desc == "Desert" || biome_desc == "Ice Caps" || biome_desc == "BadLands")
                        moistureModifier = 0.4;
                    else if (biome_desc == " Water")
                        moistureModifier = 1;
                    else
                        moistureModifier = 0.8;

                    cloud_variance = 0.5d + (Planetarium.GetUniversalTime() % 3600 / 7200d);
                }
                else
                    cloud_variance = 1;

                double latitude_variance = ((180d - lat) / 180d);

                moistureModifier = 2 * moistureModifier * latitude_variance * cloud_variance;
            }
            catch (NullReferenceException e)
            {
                Debug.LogError("[KSPI]: exception in CollectBiomeData " + e.Message + " at " + e.StackTrace);
            }
        }

        public double getPowerCapacity()
        {
            return power_capacity;
        }

        public double Wavelength
        {
            get { return activeBeamGenerator != null ? activeBeamGenerator.wavelength : nativeWaveLength; }
        }

        public string WavelengthName
        {
            get { return activeBeamGenerator != null ? activeBeamGenerator.beamWaveName : ""; }
        }

        public double CombinedAtmosphericAbsorption
        {
            get { return activeBeamGenerator != null
                ? (atmosphericAbsorptionPercentage + waterAbsorptionPercentage) / 100d 
                : nativeAtmosphericAbsorptionPercentage / 100d; } 
        }

        public double getNuclearPower()
        {
            return nuclear_power;
        }

        public double getSolarPower()
        {
            return solar_power;
        }

        public bool IsRelay
        {
            get { return relay;  }
        }

        public bool isActive()
        {
            return IsEnabled;
        }

        public static IVesselRelayPersistence getVesselRelayPersistenceForVessel(Vessel vessel)
        {
            // find all active tranmitters configured for relay
            var relays = vessel.FindPartModulesImplementing<BeamedPowerTransmitter>().Where(m => m.IsRelay || m.mergingBeams).ToList();
            if (relays.Count == 0)
                return null;

            var relayPersistance = new VesselRelayPersistence(vessel);
            relayPersistance.IsActive = true;

            if (relayPersistance.IsActive)
                return relayPersistance;
            
            foreach (var relay in relays)
            {
                var transmitData = relayPersistance.SupportedTransmitWavelengths.FirstOrDefault(m => m.wavelength == relay.wavelength);
                if (transmitData == null)
                {
                    // Add guid if missing
                    relay.partId = string.IsNullOrEmpty(relay.partId) 
                        ? Guid.NewGuid().ToString() 
                        : relay.partId;

                    relayPersistance.SupportedTransmitWavelengths.Add(new WaveLengthData()
                    {
                        partId = new Guid(relay.partId),
                        count = 1,
                        apertureSum = relay.aperture,
                        powerCapacity = relay.power_capacity,
                        wavelength = relay.Wavelength,
                        minWavelength = relay.minimumRelayWavelenght,
                        maxWavelength = relay.maximumRelayWavelenght,
                        isMirror = relay.isMirror,
                        atmosphericAbsorption = relay.CombinedAtmosphericAbsorption
                    });
                }
                else
                {
                    transmitData.count++;
                    transmitData.apertureSum += relay.aperture;
                    transmitData.powerCapacity += relay.power_capacity;
                }
            }

            relayPersistance.Aperture = relays.Average(m => m.aperture) * Approximate.Sqrt(relays.Count);
            relayPersistance.PowerCapacity = relays.Sum(m => m.getPowerCapacity());
            relayPersistance.MinimumRelayWavelenght = relays.Min(m => m.minimumRelayWavelenght);
            relayPersistance.MaximumRelayWavelenght = relays.Max(m => m.maximumRelayWavelenght);

            return relayPersistance;
        }

        public static IVesselMicrowavePersistence getVesselMicrowavePersistanceForVessel(Vessel vessel)
        {
            var transmitters = vessel.FindPartModulesImplementing<BeamedPowerTransmitter>().Where(m => m.IsEnabled).ToList();
            if (transmitters.Count == 0)
                return null;

            var vesselTransmitters = new VesselMicrowavePersistence(vessel);
            vesselTransmitters.IsActive = true;

            foreach (var transmitter in transmitters)
            {
                // Add guid if missing
                transmitter.partId = string.IsNullOrEmpty(transmitter.partId)
                    ? Guid.NewGuid().ToString()
                    : transmitter.partId;

                var transmitData = vesselTransmitters.SupportedTransmitWavelengths.FirstOrDefault(m => m.wavelength == transmitter.wavelength);
                if (transmitData == null)
                {
                    vesselTransmitters.SupportedTransmitWavelengths.Add(new WaveLengthData()
                    {
                        partId = new Guid(transmitter.partId),
                        count = 1,
                        apertureSum = transmitter.aperture,
                        wavelength = transmitter.Wavelength,
                        minWavelength = transmitter.Wavelength * 0.99,
                        maxWavelength = transmitter.Wavelength * 1.01,
                        nuclearPower = transmitter.nuclear_power,
                        solarPower = transmitter.solar_power,
                        powerCapacity = transmitter.power_capacity,
                        isMirror = transmitter.isMirror,
                        atmosphericAbsorption = transmitter.CombinedAtmosphericAbsorption
                    });
                }
                else
                {
                    transmitData.count++;
                    transmitData.apertureSum += transmitter.aperture;
                    transmitData.nuclearPower += transmitter.nuclear_power;
                    transmitData.solarPower += transmitter.solar_power;
                    transmitData.powerCapacity += transmitter.power_capacity;
                }
            }

            vesselTransmitters.Aperture = transmitters.Average(m => m.aperture) * transmitters.Count.Sqrt();
            vesselTransmitters.NuclearPower = transmitters.Sum(m => m.getNuclearPower());
            vesselTransmitters.SolarPower = transmitters.Sum(m => m.getSolarPower());
            vesselTransmitters.PowerCapacity = transmitters.Sum(m => m.getPowerCapacity());

            return vesselTransmitters;
        }

        /// <summary>
        /// Collect anything that can act like a transmitter, including relays
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static IVesselMicrowavePersistence getVesselMicrowavePersistanceForProtoVessel(Vessel vessel)
        {
            var transmitter = new VesselMicrowavePersistence(vessel);
            int totalCount = 0;
            double totalAperture = 0.0;
            double totalNuclearPower = 0.0;
            double totalSolarPower = 0.0;
            double totalPowerCapacity = 0.0;

            foreach (var protopart in vessel.protoVessel.protoPartSnapshots)
            {
                foreach (var protomodule in protopart.modules)
                {
                    if (protomodule.moduleName != "MicrowavePowerTransmitter")
                        continue;

                    // filter on active transmitters
                    bool transmitterIsEnabled = bool.Parse(protomodule.moduleValues.GetValue("IsEnabled"));
                    if (!transmitterIsEnabled)
                        continue;

                    var aperture = double.Parse(protomodule.moduleValues.GetValue("aperture"));
                    var nuclearPower = double.Parse(protomodule.moduleValues.GetValue("nuclear_power"));
                    var solarPower = double.Parse(protomodule.moduleValues.GetValue("solar_power"));
                    var powerCapacity = double.Parse(protomodule.moduleValues.GetValue("power_capacity"));
                    var wavelength = double.Parse(protomodule.moduleValues.GetValue("wavelength"));

                    totalCount++;
                    totalAperture += aperture;
                    totalNuclearPower += nuclearPower;
                    totalSolarPower += solarPower;
                    totalPowerCapacity += powerCapacity;

                    var transmitData = transmitter.SupportedTransmitWavelengths.FirstOrDefault(m => m.wavelength == wavelength);
                    if (transmitData == null)
                    {
                        bool isMirror = bool.Parse(protomodule.moduleValues.GetValue("isMirror"));
                        string partId = protomodule.moduleValues.GetValue("partId");

                        transmitter.SupportedTransmitWavelengths.Add(new WaveLengthData()
                        {
                            partId = new Guid(partId),
                            count = 1,
                            apertureSum = aperture,
                            wavelength = wavelength,
                            minWavelength = wavelength * 0.99,
                            maxWavelength = wavelength * 1.01,
                            isMirror = isMirror,
                            nuclearPower = nuclearPower,
                            solarPower = solarPower,
                            powerCapacity = powerCapacity,
                            atmosphericAbsorption = double.Parse(protomodule.moduleValues.GetValue("atmosphericAbsorption"))
                        });
                    }
                    else
                    {
                        transmitData.count++;
                        transmitData.apertureSum += aperture;
                        transmitData.nuclearPower += nuclearPower;
                        transmitData.solarPower += solarPower;
                        transmitData.powerCapacity += powerCapacity;
                    }
                }
            }

            transmitter.Aperture = totalAperture;
            transmitter.NuclearPower = totalNuclearPower;
            transmitter.SolarPower = totalSolarPower;
            transmitter.PowerCapacity = totalPowerCapacity;
            transmitter.IsActive = totalCount > 0;

            return transmitter;
        }

        public static IVesselRelayPersistence getVesselRelayPersistanceForProtoVessel(Vessel vessel)
        {
            var relayVessel = new VesselRelayPersistence(vessel);
            int totalCount = 0;
            double totalAperture = 0;
            double totalPowerCapacity = 0;
            double minimumRelayWavelength = 1;
            double maximumRelayWavelenght = 0;

            foreach (var protopart in vessel.protoVessel.protoPartSnapshots)
            {
                foreach (var protomodule in protopart.modules)
                {
                    if (protomodule.moduleName != "MicrowavePowerTransmitter")
                        continue;

                    bool inRelayMode = bool.Parse(protomodule.moduleValues.GetValue("relay"));

                    bool isMergingBeams = false;
                    if (protomodule.moduleValues.HasValue("mergingBeams"))
                    {
                        try { bool.TryParse(protomodule.moduleValues.GetValue("mergingBeams"), out isMergingBeams); }
                        catch (Exception e) { UnityEngine.Debug.LogError("[KSPI]: Exception while reading mergingBeams" + e.Message); }
                    }

                    // filter on transmitters
                    if (inRelayMode || isMergingBeams)
                    {
                        var wavelength = double.Parse(protomodule.moduleValues.GetValue("wavelength"));
                        var isMirror = bool.Parse(protomodule.moduleValues.GetValue("isMirror"));
                        var aperture = double.Parse(protomodule.moduleValues.GetValue("aperture"));
                        var powerCapacity = double.Parse(protomodule.moduleValues.GetValue("power_capacity"));

                        totalCount++;
                        totalAperture += aperture;
                        totalPowerCapacity += powerCapacity;

                        var relayWavelenghtMin = double.Parse(protomodule.moduleValues.GetValue("minimumRelayWavelenght"));
                        if (relayWavelenghtMin < minimumRelayWavelength)
                            minimumRelayWavelength = relayWavelenghtMin;

                        var relayWavelenghtMax = double.Parse(protomodule.moduleValues.GetValue("maximumRelayWavelenght"));
                        if (relayWavelenghtMax > maximumRelayWavelenght)
                            maximumRelayWavelenght = relayWavelenghtMax;

                        var relayData = relayVessel.SupportedTransmitWavelengths.FirstOrDefault(m => m.wavelength == wavelength);
                        if (relayData == null)
                        {
                            string partId = protomodule.moduleValues.GetValue("partId");

                            relayVessel.SupportedTransmitWavelengths.Add(new WaveLengthData()
                            {
                                partId = new Guid(partId),
                                count = 1,
                                apertureSum = aperture,
                                powerCapacity = powerCapacity,
                                wavelength = wavelength,
                                minWavelength = relayWavelenghtMin,
                                maxWavelength = relayWavelenghtMax, 
                                isMirror = isMirror,
                                atmosphericAbsorption = double.Parse(protomodule.moduleValues.GetValue("atmosphericAbsorption"))
                            });
                        }
                        else
                        {
                            relayData.count++;
                            relayData.apertureSum += aperture;
                            relayData.powerCapacity += powerCapacity;
                        }
                    }
                }
            }

            relayVessel.Aperture = totalAperture;
            relayVessel.PowerCapacity = totalPowerCapacity;
            relayVessel.IsActive = totalCount > 0;
            relayVessel.MinimumRelayWavelenght = minimumRelayWavelength;
            relayVessel.MaximumRelayWavelenght = maximumRelayWavelenght;

            return relayVessel;
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Aperture Diameter: " + apertureDiameter + " m");
            info.AppendLine("Can Mirror power: " + isMirror.ToString());
            info.AppendLine("Can Transmit power: " + canTransmit.ToString());
            info.AppendLine("Can Relay independantly " + buildInRelay.ToString());

            return info.ToString();
        }
    }
}
