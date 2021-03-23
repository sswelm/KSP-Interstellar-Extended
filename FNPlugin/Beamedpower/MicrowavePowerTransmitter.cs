using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Beamedpower
{
    class PhasedArrayTransmitter : BeamedPowerTransmitter { }

    class MicrowavePowerTransmitter : BeamedPowerTransmitter { }

    class BeamedPowerLaserTransmitter : BeamedPowerTransmitter { }

    class BeamedPowerTransmitter : ResourceSuppliableModule, IMicrowavePowerTransmitter //, IScalarModule
    {
        public const string Group = "BeamedPowerTransmitter";
        public const string GroupTitle = "#LOC_KSPIE_MicrowavePowerTransmitter_groupName";

        //Persistent
        [KSPField(isPersistant = true)] public double aperture = 1;
        [KSPField(isPersistant = true)] public double diameter = 0;
        [KSPField(isPersistant = true)] public bool forceActivateAtStartup;
        [KSPField(isPersistant = true)] public bool hasLinkedReceivers;
        [KSPField(isPersistant = true)] public double nativeAtmosphericAbsorptionPercentage = 10;
        [KSPField(isPersistant = true)] public string partId;
        [KSPField(isPersistant = true)] public bool IsEnabled;
        [KSPField(isPersistant = true)] public bool relay;
        [KSPField(isPersistant = true)] public double nuclear_power;
        [KSPField(isPersistant = true)] public double solar_power;
        [KSPField(isPersistant = true)] public double atmosphericAbsorption = 0.1;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitPower"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]//Transmission Strength
        public float transmitPower = 100;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_CanRelay")]//Can Relay
        public bool canRelay;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_IsMirror")]//Is Mirror
        public bool isMirror = false;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_Canmergebeams")]//Can merge beams
        public bool isBeamMerger = false;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MergingBeams")]//Merging Beams
        public bool mergingBeams;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_PowerCapacity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Power Capacity
        public double power_capacity;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitWaveLengthm", guiFormat = "F8", guiUnits = " m")]//Transmit WaveLength m
        public double wavelength;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitWaveLengthSI")]//Transmit WaveLength SI
        public string wavelengthText;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitWaveLengthWLName")]//Transmit WaveLength WL Name
        public string wavelengthName;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MinRelayWaveLength", guiFormat = "F8", guiUnits = " m")]//Min Relay WaveLength
        public double minimumRelayWavelenght;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MaxRelayWaveLength", guiFormat = "F8", guiUnits = " m")]//Max Relay WaveLength
        public double maximumRelayWavelenght;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_NativeWavelength", guiFormat = "F8", guiUnits = " m")]
        public double nativeWaveLength = 0.003189281;

        //Non Persistent
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_AtmosphericAbsorptionPercentage", guiFormat = "F2", guiUnits = "%")]//Air Absorption Percentage
        public double atmosphericAbsorptionPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_WaterAbsorptionPercentage", guiFormat = "F2", guiUnits = "%")]//Water Absorption Percentage
        public double waterAbsorptionPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TotalAbsorptionPercentage", guiFormat = "F2", guiUnits = "%")]//Absorption Percentage
        public double totalAbsorptionPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_Bodyname")]//Body
        public string body_name;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false)]
        public string biome_desc;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MoistureModifier", guiFormat = "F3")]//Moisture Modifier
        public double moistureModifier;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false)]
        public bool canFunctionOnSurface = true;



        //GUI
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_CanTransmit")]//Can Transmit
        public bool canTransmit = false;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_BuildinRelay")]//Build in Relay
        public bool buildInRelay = false;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_ApertureDiameter", guiFormat = "F2", guiUnits = " m")]//Aperture Diameter
        public double apertureDiameter;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_Status")]//Status
        public string statusStr;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmissionEfficiency", guiFormat = "F1", guiUnits = "%")]//Transmission Efficiency
        public double transmissionEfficiencyPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_BeamedPower")]//Wall to Beam Power
        public string beamedpower;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_AvailablePower", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", advancedTweakable = true)]//Available Power
        public double availablePower;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_RequestedPower", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", advancedTweakable = true)]//Requested Power
        public double requestedPower;

        // Near Future Compatibility config properties
        [KSPField] public double powerMult = 1;
        [KSPField] public double powerHeatMultiplier = 1;
        [KSPField] public bool canPivot = true;        // determines if effective aperture is affected on surface
        [KSPField] public double maximumPower = 10000;
        [KSPField] public float atmosphereToleranceModifier = 1;
        [KSPField] public string animName = "";
        [KSPField] public bool canBeActive;
        [KSPField] protected int nearbyPartsCount;
        [KSPField] public int compatibleBeamTypes = 1;

        private readonly string scalarModuleId = Guid.NewGuid().ToString();

        private EventData<float, float> onMoving;
        private EventData<float> onStop;

        public BeamedPowerReceiver partReceiver;
        public BeamGenerator activeBeamGenerator;

        //Internal
        private List<ISolarPower> solarCells;
        private List<BeamedPowerReceiver> vesselReceivers;
        private List<BeamGenerator> beamGenerators;
        private ModuleAnimateGeneric genericAnimation;

        private BaseEvent activateTransmitterEvent;
        private BaseEvent deactivateTransmitterEvent;
        private BaseEvent activateRelayEvent;
        private BaseEvent deactivateRelayEvent;

        private BaseField apertureDiameterField;
        private BaseField beamedPowerField;
        private BaseField transmitPowerField;
        private BaseField totalAbsorptionPercentageField;
        private BaseField wavelengthField;
        private BaseField wavelengthNameField;

        public bool CanMove => true;
        public float GetScalar => 1;
        public bool IsRelay => relay;
        public EventData<float, float> OnMoving => onMoving;
        public EventData<float> OnStop => onStop;
        public string ScalarModuleID => scalarModuleId;

        public void SetUIRead(bool state) { }
        public void SetUIWrite(bool state) { }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_ActivateTransmitter", active = false)]//Activate Transmitter
        public void ActivateTransmitter()
        {
            if (relay) return;

            Debug.Log("[KSPI]: BeamedPowerTransmitter on " + part.name + " was Force Activated");
            part.force_activate();
            forceActivateAtStartup = true;

            if (genericAnimation != null && genericAnimation.GetScalar < 1)
                genericAnimation.Toggle();

            IsEnabled = true;

            // update wavelength
            wavelength = Wavelength;
            minimumRelayWavelenght = wavelength * 0.99;
            maximumRelayWavelenght = wavelength * 1.01;

            wavelengthText = WavelengthToText(wavelength);
            wavelengthName = WavelengthName;
            atmosphericAbsorption = CombinedAtmosphericAbsorption;
        }

        private string WavelengthToText( double beamWavelength)
        {
            if (beamWavelength > 1.0e-3)
                return (beamWavelength * 1.0e+3) + " mm";
            else if (beamWavelength > 7.5e-7)
                return (beamWavelength * 1.0e+6) + " µm";
            else if (beamWavelength > 1.0e-9)
                return (beamWavelength * 1.0e+9) + " nm";
            else
                return (beamWavelength * 1.0e+12) + " pm";
        }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateTransmitter", active = false)]//Deactivate Transmitter
        public void DeactivateTransmitter()
        {
            if (relay) return;

            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateTransmitter_Msg"), 4.0f, ScreenMessageStyle.UPPER_CENTER);//"Transmitter deactivated"

            if (genericAnimation != null && genericAnimation.GetScalar > 0)
                genericAnimation.Toggle();

            IsEnabled = false;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_ActivateRelay", active = false)]//Activate Relay
        public void ActivateRelay()
        {
            if (IsEnabled || relay) return;

            if (genericAnimation != null && genericAnimation.GetScalar < 1)
                genericAnimation.Toggle();

            vesselReceivers = vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != this.part).ToList();

            UpdateRelayWavelength();

            relay = true;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateRelay", active = false)]//Deactivate Relay
        public void DeactivateRelay()
        {
            if (!relay) return;

            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateRelay_Msg"), 4, ScreenMessageStyle.UPPER_CENTER);//"Relay deactivated"

            if (genericAnimation != null && genericAnimation.GetScalar > 0)
                genericAnimation.Toggle();

            relay = false;
        }

        private void UpdateRelayWavelength()
        {
            // update stored variables
            wavelength = Wavelength;
            wavelengthText = WavelengthToText(wavelength);
            wavelengthName = WavelengthName;
            atmosphericAbsorption = CombinedAtmosphericAbsorption;

            if (isMirror)
            {
                hasLinkedReceivers = true;
                return;
            }

            // collected all receivers relevant for relay
            var receiversConfiguredForRelay = vesselReceivers.Where(m => m.linkedForRelay).ToList();

            // add build in relay if it can be used for relay
            if (partReceiver != null && buildInRelay)
                receiversConfiguredForRelay.Add(partReceiver);

            // determine if we can activate relay
            hasLinkedReceivers = receiversConfiguredForRelay.Count > 0;

            // use all available receivers
            if (hasLinkedReceivers)
            {
                minimumRelayWavelenght = receiversConfiguredForRelay.Min(m => m.minimumWavelength);
                maximumRelayWavelenght = receiversConfiguredForRelay.Max(m => m.maximumWavelength);

                diameter = receiversConfiguredForRelay.Max(m => m.diameter);
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

        public override void OnStart(StartState state)
        {
            onMoving = new EventData<float, float>("transmitterMoving");
            onStop = new EventData<float>("transmitterStop");

            power_capacity = maximumPower * powerMult;

            if (string.IsNullOrEmpty(partId))
                partId = Guid.NewGuid().ToString();

            // store  aperture and diameter
            aperture = apertureDiameter;
            diameter = apertureDiameter;

            partReceiver = part.FindModulesImplementing<BeamedPowerReceiver>().FirstOrDefault();
            genericAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().FirstOrDefault(m => m.animationName == animName);

            ConnectToBeamGenerator();

            activateRelayEvent = Events[nameof(ActivateRelay)];
            deactivateRelayEvent = Events[nameof(DeactivateRelay)];
            activateTransmitterEvent = Events[nameof(ActivateTransmitter)];
            deactivateTransmitterEvent = Events[nameof(DeactivateTransmitter)];

            wavelengthField = Fields[nameof(wavelengthText)];
            beamedPowerField = Fields[nameof(beamedpower)];
            transmitPowerField = Fields[nameof(transmitPower)];
            wavelengthNameField = Fields[nameof(wavelengthName)];
            apertureDiameterField = Fields[nameof(apertureDiameter)];
            totalAbsorptionPercentageField = Fields[nameof(totalAbsorptionPercentage)];

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                return;
            }

            solarCells = vessel.FindPartModulesImplementing<ISolarPower>();
            vesselReceivers = vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != this.part).ToList();

            UpdateRelayWavelength();

            if (forceActivateAtStartup)
            {
                Debug.Log("[KSPI]: BeamedPowerTransmitter on " + part.name + " was Force Activated");
                part.force_activate();
            }
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
            // connect with beam generators
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

            if (activeBeamGenerator != null)
            {
                activeBeamGenerator.Connect(this);

                if (activeBeamGenerator.part != this.part)
                    activeBeamGenerator.UpdateMass(this.maximumPower);
            }
        }

        public bool CanBeActive
        {
            get
            {
                if (vessel == null)
                    return true;

                var pressure = part.atmDensity;
                var dynamicPressure = 0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325.0;

                if (dynamicPressure <= 0) return true;

                var pressureLoad = (dynamicPressure / 1.4854428818159e-3) * 100;

                if (pressureLoad > 100 * atmosphereToleranceModifier)
                    return false;
                else
                    return true;
            }
        }

        public void Update()
        {
            bool vesselInSpace = vessel == null || vessel.situation == Vessel.Situations.PRELAUNCH  || vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL;
            bool receiverOn = partReceiver != null && partReceiver.isActive();

            var canOperateInCurrentEnvironment = canFunctionOnSurface || vesselInSpace;
            var vesselCanTransmit = canTransmit && canOperateInCurrentEnvironment;

            canRelay = hasLinkedReceivers && canOperateInCurrentEnvironment;
            canBeActive = CanBeActive;
            mergingBeams = IsEnabled && canRelay && isBeamMerger;

            activateTransmitterEvent.active = activeBeamGenerator != null && vesselCanTransmit && !IsEnabled && !relay && !receiverOn && canBeActive;
            deactivateTransmitterEvent.active = IsEnabled;

            activateRelayEvent.active = canRelay && !IsEnabled && !relay && !receiverOn && canBeActive;
            deactivateRelayEvent.active = relay;

            if (!HighLogic.LoadedSceneIsFlight)
            {
                power_capacity = maximumPower * powerMult;
                return;
            }

            UpdateRelayWavelength();

            totalAbsorptionPercentage = atmosphericAbsorptionPercentage + waterAbsorptionPercentage;
            atmosphericAbsorption = totalAbsorptionPercentage / 100;

            if (!canBeActive && IsEnabled && part.vessel.isActiveVessel && !CheatOptions.UnbreakableJoints)
            {
                if (relay)
                {
                    var message = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Disabledrelay_Msg");//"Disabled relay because of static pressure atmosphere"
                    ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                    Debug.Log("KSPI - " + message);
                    DeactivateRelay();
                }
                else
                {
                    var message = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Disabledtransmitter_Msg");//"Disabled transmitter because of static pressure atmosphere"
                    ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                    Debug.Log("KSPI - " + message);
                    DeactivateTransmitter();
                }
            }

            bool isTransmitting = IsEnabled && !relay;
            bool isLinkedForRelay = partReceiver != null && partReceiver.linkedForRelay;
            bool receiverNotInUse = !isLinkedForRelay && !receiverOn && !IsRelay;

            apertureDiameterField.guiActive = isTransmitting;
            beamedPowerField.guiActive = isTransmitting && canBeActive;
            transmitPowerField.guiActive = partReceiver == null || !partReceiver.isActive();

            totalAbsorptionPercentageField.guiActive = receiverNotInUse;
            wavelengthField.guiActive = receiverNotInUse;
            wavelengthNameField.guiActive = receiverNotInUse;

            if (IsEnabled)
                statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu1");//"Transmitter Active"
            else if (relay)
                statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu2");//"Relay Active"
            else
            {
                if (isLinkedForRelay)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu3");//"Is Linked For Relay"
                else if (receiverOn)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu4");//"Receiver active"
                else if (canRelay)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu5");//"Is ready for relay"
                else if (beamGenerators.Count == 0)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu6");//"No beam generator found"
                else
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu7");//"Inactive."
            }

            if (activeBeamGenerator == null)
            {
                var waveLengthField = Fields[nameof(wavelength)];
                waveLengthField.guiActive = false;
                waveLengthField.guiActiveEditor = false;

                var atmosphericAbsorptionPercentageField = Fields[nameof(atmosphericAbsorptionPercentage)];
                atmosphericAbsorptionPercentageField.guiActive = false;
                atmosphericAbsorptionPercentageField.guiActiveEditor = false;

                var waterAbsorptionPercentageField = Fields[nameof(waterAbsorptionPercentage)];
                waterAbsorptionPercentageField.guiActive = false;
                waterAbsorptionPercentageField.guiActiveEditor = false;

                return;
            }

            wavelength = activeBeamGenerator.wavelength;
            wavelengthText = WavelengthToText(wavelength);
            atmosphericAbsorptionPercentage = activeBeamGenerator.atmosphericAbsorptionPercentage;
            waterAbsorptionPercentage = activeBeamGenerator.waterAbsorptionPercentage * moistureModifier;

            beamedpower = PluginHelper.GetFormattedPowerString((nuclear_power + solar_power) / GameConstants.ecPerMJ);
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
            availablePower = 0;
            requestedPower = 0;

            CollectBiomeData();

            base.OnFixedUpdate();

            if (activeBeamGenerator != null && IsEnabled && !relay)
            {
                double powerTransmissionRatio = (double)(decimal)transmitPower / 100d;
                double transmissionWasteRatio = (100 - activeBeamGenerator.efficiencyPercentage) / 100d;
                double transmissionEfficiencyRatio = activeBeamGenerator.efficiencyPercentage / 100d;

                availablePower = GetAvailableStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt);

                if (CheatOptions.InfiniteElectricity)
                {
                    requestedPower = power_capacity;
                }
                else
                {
                    var megajoulesRatio = GetResourceBarRatio(ResourceSettings.Config.ElectricPowerInMegawatt);
                    var wasteheatRatio = GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);

                    var effectiveResourceThrottling = Math.Min(megajoulesRatio > 0.5 ? 1 : megajoulesRatio * 2, wasteheatRatio < 0.9 ? 1 : (1  - wasteheatRatio) * 10);

                    requestedPower = Math.Min(Math.Min(power_capacity, availablePower) * powerTransmissionRatio, effectiveResourceThrottling * availablePower);
                }

                double receivedPower = CheatOptions.InfiniteElectricity ? requestedPower :
                    ConsumeFnResourcePerSecond(requestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);

                nuclear_power += GameConstants.ecPerMJ * transmissionEfficiencyRatio * receivedPower;

                solar_power += GameConstants.ecPerMJ * transmissionEfficiencyRatio * solarCells.Sum(m => m.SolarPower);

                // generate wasteheat for converting electric power to beamed power
                if (!CheatOptions.IgnoreMaxTemperature)
                    SupplyFnResourcePerSecond(receivedPower * transmissionWasteRatio, ResourceSettings.Config.WasteHeatInMegawatt);
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

            double cloudVariance;
            if (body_name == "Kerbin" || body_name == "Earth")
            {
                if (biome_desc == "Desert" || biome_desc == "Ice Caps" || biome_desc == "BadLands")
                    moistureModifier = 0.4;
                else if (biome_desc == "Water")
                    moistureModifier = 1;
                else
                    moistureModifier = 0.8;

                cloudVariance = 0.5d + (Planetarium.GetUniversalTime() % 3600 / 7200d);
            }
            else
                cloudVariance = 1;

            double latitudeVariance = (180d - lat) / 180d;

            moistureModifier = 2 * moistureModifier * latitudeVariance * cloudVariance;
        }

        public double PowerCapacity => power_capacity;

        public double Wavelength => activeBeamGenerator != null ? activeBeamGenerator.wavelength : nativeWaveLength;

        public string WavelengthName => activeBeamGenerator != null ? activeBeamGenerator.beamWaveName : "";

        public double CombinedAtmosphericAbsorption => activeBeamGenerator != null ? (atmosphericAbsorptionPercentage + waterAbsorptionPercentage) / 100d : nativeAtmosphericAbsorptionPercentage / 100d;

        public double getNuclearPower()
        {
            return nuclear_power;
        }

        public double getSolarPower()
        {
            return solar_power;
        }

        public bool isActive()
        {
            return IsEnabled;
        }

        public static IVesselRelayPersistence GetVesselRelayPersistenceForVessel(Vessel vessel)
        {
            // find all active transmitters configured for relay
            var relays = vessel.FindPartModulesImplementing<BeamedPowerTransmitter>().Where(m => m.IsRelay || m.mergingBeams).ToList();
            if (relays.Count == 0)
                return null;

            var relayPersistence = new VesselRelayPersistence(vessel) {IsActive = true};

            //if (relayPersistence.IsActive)
            //    return relayPersistence;

            foreach (var relay in relays)
            {
                var transmitData = relayPersistence.SupportedTransmitWavelengths.FirstOrDefault(m => m.wavelength == relay.wavelength);
                if (transmitData == null)
                {
                    // Add guid if missing
                    relay.partId = string.IsNullOrEmpty(relay.partId)
                        ? Guid.NewGuid().ToString()
                        : relay.partId;

                    relayPersistence.SupportedTransmitWavelengths.Add(new WaveLengthData()
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

            relayPersistence.Aperture = relays.Average(m => m.aperture) * Math.Sqrt(relays.Count);
            relayPersistence.Diameter = relays.Average(m => m.diameter);
            relayPersistence.PowerCapacity = relays.Sum(m => m.PowerCapacity);
            relayPersistence.MinimumRelayWavelenght = relays.Min(m => m.minimumRelayWavelenght);
            relayPersistence.MaximumRelayWavelenght = relays.Max(m => m.maximumRelayWavelenght);

            return relayPersistence;
        }

        public static IVesselMicrowavePersistence GetVesselMicrowavePersistenceForVessel(Vessel vessel)
        {
            var transmitters = vessel.FindPartModulesImplementing<BeamedPowerTransmitter>().Where(m => m.IsEnabled).ToList();
            if (transmitters.Count == 0)
                return null;

            var vesselTransmitters = new VesselMicrowavePersistence(vessel) {IsActive = true};

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
            vesselTransmitters.PowerCapacity = transmitters.Sum(m => m.PowerCapacity);

            return vesselTransmitters;
        }

        /// <summary>
        /// Collect anything that can act like a transmitter, including relays
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static IVesselMicrowavePersistence GetVesselMicrowavePersistenceForProtoVessel(Vessel vessel)
        {
            var transmitter = new VesselMicrowavePersistence(vessel);
            int totalCount = 0;

            double totalAperture = 0.0;
            double totalNuclearPower = 0.0;
            double totalSolarPower = 0.0;
            double totalPowerCapacity = 0.0;

            foreach (var protoPart in vessel.protoVessel.protoPartSnapshots)
            {
                foreach (var protoModule in protoPart.modules)
                {
                    if (protoModule.moduleName != "MicrowavePowerTransmitter" && protoModule.moduleName != "PhasedArrayTransmitter" && protoModule.moduleName != "BeamedPowerLaserTransmitter")
                        continue;

                    // filter on active transmitters
                    var transmitterIsEnabled = bool.Parse(protoModule.moduleValues.GetValue("IsEnabled"));
                    if (!transmitterIsEnabled)
                        continue;

                    var aperture = double.Parse(protoModule.moduleValues.GetValue("aperture"));
                    var nuclearPower = double.Parse(protoModule.moduleValues.GetValue("nuclear_power"));
                    var solarPower = double.Parse(protoModule.moduleValues.GetValue("solar_power"));
                    var powerCapacity = double.Parse(protoModule.moduleValues.GetValue("power_capacity"));
                    var wavelength = double.Parse(protoModule.moduleValues.GetValue("wavelength"));

                    totalCount++;
                    totalAperture += aperture;
                    totalNuclearPower += nuclearPower;
                    totalSolarPower += solarPower;
                    totalPowerCapacity += powerCapacity;

                    var transmitData = transmitter.SupportedTransmitWavelengths.FirstOrDefault(m => m.wavelength == wavelength);
                    if (transmitData == null)
                    {
                        bool isMirror = bool.Parse(protoModule.moduleValues.GetValue("isMirror"));
                        string partId = protoModule.moduleValues.GetValue("partId");

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
                            atmosphericAbsorption = double.Parse(protoModule.moduleValues.GetValue("atmosphericAbsorption"))
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

        public static IVesselRelayPersistence GetVesselRelayPersistenceForProtoVessel(Vessel vessel)
        {
            var relayVessel = new VesselRelayPersistence(vessel);
            int totalCount = 0;

            double totalDiameter = 0;
            double totalAperture = 0;
            double totalPowerCapacity = 0;
            double minimumRelayWavelength = 1;
            double maximumRelayWavelenght = 0;

            foreach (var protoPart in vessel.protoVessel.protoPartSnapshots)
            {
                foreach (var protoModule in protoPart.modules)
                {
                    if (protoModule.moduleName != "MicrowavePowerTransmitter" && protoModule.moduleName != "PhasedArrayTransmitter" && protoModule.moduleName != "BeamedPowerLaserTransmitter")
                        continue;

                    bool inRelayMode = bool.Parse(protoModule.moduleValues.GetValue("relay"));

                    bool isMergingBeams = false;
                    if (protoModule.moduleValues.HasValue("mergingBeams"))
                        isMergingBeams = bool.Parse(protoModule.moduleValues.GetValue("mergingBeams"));

                    // filter on transmitters
                    if (inRelayMode || isMergingBeams)
                    {
                        var wavelength = double.Parse(protoModule.moduleValues.GetValue("wavelength"));
                        var isMirror = bool.Parse(protoModule.moduleValues.GetValue("isMirror"));
                        var aperture = double.Parse(protoModule.moduleValues.GetValue("aperture"));
                        var powerCapacity = double.Parse(protoModule.moduleValues.GetValue("power_capacity"));

                        var diameter = protoModule.moduleValues.HasValue("diameter") ? double.Parse(protoModule.moduleValues.GetValue("diameter")) : aperture;

                        totalCount++;
                        totalAperture += aperture;
                        totalDiameter += diameter;
                        totalPowerCapacity += powerCapacity;

                        var relayWavelenghtMin = double.Parse(protoModule.moduleValues.GetValue("minimumRelayWavelenght"));
                        if (relayWavelenghtMin < minimumRelayWavelength)
                            minimumRelayWavelength = relayWavelenghtMin;

                        var relayWavelenghtMax = double.Parse(protoModule.moduleValues.GetValue("maximumRelayWavelenght"));
                        if (relayWavelenghtMax > maximumRelayWavelenght)
                            maximumRelayWavelenght = relayWavelenghtMax;

                        var relayData = relayVessel.SupportedTransmitWavelengths.FirstOrDefault(m => m.wavelength == wavelength);
                        if (relayData == null)
                        {
                            string partId = protoModule.moduleValues.GetValue("partId");

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
                                atmosphericAbsorption = double.Parse(protoModule.moduleValues.GetValue("atmosphericAbsorption"))
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

            relayVessel.Aperture = (totalAperture / totalCount) * Approximate.Sqrt(totalCount);
            relayVessel.Diameter = totalDiameter / totalCount;
            relayVessel.PowerCapacity = totalPowerCapacity;
            relayVessel.IsActive = totalCount > 0;
            relayVessel.MinimumRelayWavelenght = minimumRelayWavelength;
            relayVessel.MaximumRelayWavelenght = maximumRelayWavelenght;

            return relayVessel;
        }

        public override string GetInfo()
        {
            var info = StringBuilderCache.Acquire();

            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info1"));//Aperture Diameter
            info.Append(": ").Append(apertureDiameter.ToString("F1")).AppendLine(" m");
            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info2"));//Can Mirror power
            info.Append(": ").AppendLine(RUIutils.GetYesNoUIString(isMirror));
            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info3"));//Can Transmit power
            info.Append(": ").AppendLine(RUIutils.GetYesNoUIString(canTransmit));
            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info4"));//Can Relay independently
            info.Append(": ").AppendLine(RUIutils.GetYesNoUIString(buildInRelay));

            return info.ToStringAndRelease();
        }
    }
}
