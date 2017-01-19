using FNPlugin.Microwave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class MicrowavePowerTransmitter : FNResourceSuppliableModule
    {
        //Persistent 
        [KSPField(isPersistant = true)]
        protected string partId;
        [KSPField(isPersistant = true)]
        protected bool IsEnabled;
        [KSPField(isPersistant = true)]
        protected bool relay;
        [KSPField(isPersistant = true)]
        protected double nuclear_power = 0;
        [KSPField(isPersistant = true)]
        protected double solar_power = 0;
        [KSPField(isPersistant = true)]
        protected double power_capacity = 0;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Transmit Wavelength", guiFormat = "F8", guiUnits = " m")]
        public double wavelength = 0;
        [KSPField(isPersistant = true)]
        public double atmosphericAbsorption = 0.1;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Min Relay Wavelength", guiFormat = "F8", guiUnits = " m")]
        public double minimumRelayWavelenght;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Max Relay Wavelength", guiFormat = "F8", guiUnits = " m")]
        public double maximumRelayWavelenght;
        [KSPField(isPersistant = true)]
        public double aperture = 1;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName= "Is Mirror")]
        public bool isMirror = false;
        [KSPField(isPersistant = true)]
        public bool forceActivateAtStartup = false;

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
        public float atmosphereToleranceModifier = 1;
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Can Transmit")]
        public bool canTransmit = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Build in Relay")]
        public bool buildInRelay = false;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public int compatibleBeamTypes = 1;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double nativeWaveLength = 0.003189281;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public double nativeAtmosphericAbsorptionPercentage = 10;

        //GUI 
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Aperture Diameter", guiFormat = "F2", guiUnits = " m")]
        public double apertureDiameter = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Transmit Status")]
        public string statusStr;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Transmission Efficiency", guiUnits = "%")]
        public double transmissionEfficiencyPercentage;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Reactor Power Transmission"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float transmitPower = 100;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Solar Power Transmission"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float solarPowertransmission = 100;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Maximum Power", guiUnits = " MW", guiFormat = "F2")]
        public double maximumPower = 10000;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Wall to Beam Power")]
        public string beamedpower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Direct Solar Power", guiFormat = "F2")]
        protected double displayed_solar_power = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Has Linked Receivers")]
        public bool hasLinkedReceivers = false;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Can be active")]
        public bool canBeActive;
        [KSPField(isPersistant = false)]
        public float powerMult = 1;

        //Internal
        protected Animation anim;
        protected List<ModuleDeployableSolarPanel> panels;
        protected MicrowavePowerReceiver part_receiver;
        protected List<MicrowavePowerReceiver> vessel_recievers;
        protected BeamGenerator activeBeamGenerator;
        protected List<BeamGenerator> beamGenerators;

        [KSPEvent(guiActive = true, guiName = "Activate Transmitter", active = false)]
        public void ActivateTransmitter()
        {
            if (relay) return;

            this.part.force_activate();
            forceActivateAtStartup = true;

            if (anim != null)
            {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, part.mass);
            }
            IsEnabled = true;

            // update wavelength
            wavelength = Wavelength;
            atmosphericAbsorption = CombinedAtmosphericAbsorption;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Transmitter", active = false)]
        public void DeactivateTransmitter()
        {
            if (relay) return;

            ScreenMessages.PostScreenMessage("Transmitter deactivated", 4.0f, ScreenMessageStyle.UPPER_CENTER);
 
            if (anim != null)
            {
                anim[animName].speed = -1f;
                anim[animName].normalizedTime = 1f;
                anim.Blend(animName, part.mass);
            }
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Relay", active = false)]
        public void ActivateRelay()
        {
            if (IsEnabled) return;

            if (anim != null)
            {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, part.mass);
            }

            vessel_recievers = this.vessel.FindPartModulesImplementing<MicrowavePowerReceiver>().Where(m => m.part != this.part).ToList();

            UpdateRelayWavelength();

            IsEnabled = true;
            relay = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Relay", active = false)]
        public void DeactivateRelay()
        {
            if (!relay) return;

            ScreenMessages.PostScreenMessage("Relay deactivated", 4.0f, ScreenMessageStyle.UPPER_CENTER);

            if (anim != null)
            {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, part.mass);
            }
            IsEnabled = false;
            relay = false;
        }

        private void UpdateRelayWavelength()
        {
            if (isMirror)
            {
                this.wavelength = Wavelength;
                this.atmosphericAbsorption = CombinedAtmosphericAbsorption;
                this.hasLinkedReceivers = true;
                return;
            }

            // update stored variables
            this.wavelength = Wavelength;
            this.atmosphericAbsorption = CombinedAtmosphericAbsorption;

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

            panels = vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>();

            vessel_recievers = this.vessel.FindPartModulesImplementing<MicrowavePowerReceiver>().Where(m => m.part != this.part).ToList();
            part_receiver = part.FindModulesImplementing<MicrowavePowerReceiver>().FirstOrDefault();

            UpdateRelayWavelength();

            //ScreenMessages.PostScreenMessage("Microwave Transmitter Updated Wvelength", 10.0f, ScreenMessageStyle.UPPER_CENTER);

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if ( anim != null &&  part_receiver == null)
            {
                anim[animName].layer = 1;
                if (IsEnabled)
                {
                    //ScreenMessages.PostScreenMessage("Microwave Transmitter Activates", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                    anim[animName].normalizedTime = 0f;
                    anim[animName].speed = 1f;
                }
                else
                {
                    //ScreenMessages.PostScreenMessage("Microwave Transmitter Deactivates", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                    anim[animName].normalizedTime = 1f;
                    anim[animName].speed = -1f;
                }
                //anim.Play();
                anim.Blend(animName, part.mass);
            }

            if (forceActivateAtStartup)
                this.part.force_activate();
            //ScreenMessages.PostScreenMessage("Microwave Transmitter Force Activated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
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
                    var attachedParts =  part.attachNodes
                        .Where(m => m.attachedPart != null)
                        .Select(m => m.attachedPart)
                        .SelectMany(m => m.FindModulesImplementing<BeamGenerator>())
                        .Where(m => (m.beamType & compatibleBeamTypes) == m.beamType);

                    beamGenerators.AddRange(attachedParts);
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

                var pressure = FlightGlobals.getStaticPressure(vessel.transform.position) / 100f;
                var dynamic_pressure = 0.5f * pressure * 1.2041f * vessel.srf_velocity.sqrMagnitude / 101325.0f;

                if (dynamic_pressure <= 0) return true;

                var pressureLoad = (dynamic_pressure / 1.4854428818159e-3f) * 100;
                if (pressureLoad > 100 * atmosphereToleranceModifier)
                    return false;
                else 
                    return true;
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            UpdateRelayWavelength();



            //Debug.Log("[KSP Interstellar] UpdateFromGUI updated wave data");

            totalAbsorptionPercentage = atmosphericAbsorptionPercentage + waterAbsorptionPercentage;
            atmosphericAbsorption = totalAbsorptionPercentage / 100;

            bool vesselInSpace = (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL);
            bool receiver_on = part_receiver != null && part_receiver.isActive();
            canBeActive = CanBeActive;

            if (anim != null && !canBeActive && IsEnabled)
            {
                if (relay)
                    DeactivateRelay();
                else
                    DeactivateTransmitter();
            }

            var canOperateInCurrentEnvironment = this.canFunctionOnSurface || vesselInSpace;
            var vesselCanTransmit = canTransmit && canOperateInCurrentEnvironment;

            Events["ActivateTransmitter"].active = activeBeamGenerator != null && vesselCanTransmit && !IsEnabled && !relay && !receiver_on && canBeActive;
            Events["DeactivateTransmitter"].active = activeBeamGenerator != null && vesselCanTransmit && IsEnabled && !relay;

            var transmitterCanRelay = this.hasLinkedReceivers && canOperateInCurrentEnvironment;

            Events["ActivateRelay"].active = transmitterCanRelay && !IsEnabled && !relay && !receiver_on && canBeActive;
            Events["DeactivateRelay"].active = transmitterCanRelay && IsEnabled && relay;

            bool isTransmitting = IsEnabled && !relay;

            Fields["apertureDiameter"].guiActive = isTransmitting; 
            Fields["beamedpower"].guiActive = isTransmitting && canBeActive;
            Fields["transmitPower"].guiActive = isTransmitting;
            Fields["solarPowertransmission"].guiActive = isTransmitting;
            Fields["displayed_solar_power"].guiActive = isTransmitting && displayed_solar_power > 0;

            if (IsEnabled)
            {
                if (relay)
                    statusStr = "Relay Active";
                else
                    statusStr = "Transmitter Active";
            }
            else
                statusStr = "Inactive.";

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
            atmosphericAbsorptionPercentage = activeBeamGenerator.atmosphericAbsorptionPercentage;
            waterAbsorptionPercentage = activeBeamGenerator.waterAbsorptionPercentage * moistureModifier;

            double inputPower = nuclear_power + displayed_solar_power;
            if (inputPower > 1000)
            {
                if (inputPower > 1e6)
                    beamedpower = (inputPower / 1e6).ToString("0.000") + " GW";
                else
                    beamedpower = (inputPower / 1000).ToString("0.000") + " MW";
            }
            else
                beamedpower = inputPower.ToString("0.000") + " KW";
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
            displayed_solar_power = 0;
            power_capacity = maximumPower;

            CollectBiomeData();

            base.OnFixedUpdate();

            if (activeBeamGenerator != null && IsEnabled && !relay)
            {
                float reactorPowerTransmissionRatio = transmitPower / 100;
                float solarPowertransmissionRatio = solarPowertransmission / 100;

                double transmissionWasteRatio = (100 - activeBeamGenerator.efficiencyPercentage) / 100;
                double transmissionEfficiencyRatio = activeBeamGenerator.efficiencyPercentage / 100;

                double requestedPower;

                if (CheatOptions.InfiniteElectricity)
                {
                    requestedPower = maximumPower;
                }
                else
                {
                    var availableReactorPower = Math.Max(getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) - getCurrentHighPriorityResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES), 0);

                    requestedPower = Math.Min(maximumPower, getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) * availableReactorPower * reactorPowerTransmissionRatio);
                }

                var fixedRequestedPower = requestedPower * TimeWarp.fixedDeltaTime;

                var receivedPowerFixedDelta = CheatOptions.InfiniteElectricity 
                    ? fixedRequestedPower
                    : consumeFNResource(fixedRequestedPower, FNResourceManager.FNRESOURCE_MEGAJOULES);

                nuclear_power += 1000 * reactorPowerTransmissionRatio * transmissionEfficiencyRatio * receivedPowerFixedDelta / TimeWarp.fixedDeltaTime;

                // generate wasteheat for converting electric power to beamed power
                if (!CheatOptions.IgnoreMaxTemperature)
                    supplyFNResource(receivedPowerFixedDelta * transmissionWasteRatio, FNResourceManager.FNRESOURCE_WASTEHEAT);

                foreach (ModuleDeployableSolarPanel panel in panels)
                {
                    var multiplier = panel.resourceName == FNResourceManager.FNRESOURCE_MEGAJOULES ? 1000 
                        : panel.resourceName == FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE ? 1 : 0;

                    displayed_solar_power += panel._flowRate * multiplier;

                    solar_power += panel.chargeRate * panel._distMult * panel._efficMult * multiplier;

                    //panel.alignType = ModuleDeployablePart.PanelAlignType.X;
                    //panel.panelType = ModuleDeployableSolarPanel.PanelType.CYLINDRICAL;

                    //double output = panel.flowRate;

                    //double spower = part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, transmissionEfficiencyRatio * output * TimeWarp.fixedDeltaTime * solarPowertransmissionRatio);

                    //displayed_solar_power += spower / TimeWarp.fixedDeltaTime;

                    ////scale solar power to what it would be in Kerbin orbit for file storage
                    //var distanceBetweenVesselAndSun  = Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                    //var distanceBetweenSunAndKerbin = Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                    //double inv_square_mult = Math.Pow(distanceBetweenSunAndKerbin, 2) / Math.Pow(distanceBetweenVesselAndSun, 2);

                    //var effectiveSolarPower = spower / TimeWarp.fixedDeltaTime / inv_square_mult;

                    //solar_power += effectiveSolarPower;

                    //// solar power converted to beamed power also generates wasteheat
                    ////supplyFNResource(effectiveSolarPower * TimeWarp.fixedDeltaTime * transmissionWasteRatio, FNResourceManager.FNRESOURCE_WASTEHEAT);
                }
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
                    if (biome_desc == "Desert" || biome_desc == "Ice Caps")
                        moistureModifier = 0.5;
                    else if (biome_desc == " Water")
                        moistureModifier = 1;
                    else if (biome_desc == "BadLands")
                        moistureModifier = 0.3;
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
                Debug.LogError("[KSPI] - exception in CollectBiomeData " + e.Message + " at " + e.StackTrace);
            }
        }

        public double getPowerCapacity()
        {
            return power_capacity > 0 ? power_capacity : maximumPower;
        }

        public double Wavelength
        {
            get { return activeBeamGenerator != null ? activeBeamGenerator.wavelength : nativeWaveLength; }
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

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title;
        }

        public static VesselRelayPersistence getVesselRelayPersistenceForVessel(Vessel vessel)
        {
            // find all active tranmitters configured for relay
            List<MicrowavePowerTransmitter> relays = vessel.FindPartModulesImplementing<MicrowavePowerTransmitter>().Where(m => m.IsRelay && m.IsEnabled).ToList();

            var relay = new VesselRelayPersistence(vessel);
            relay.IsActive = relays.Count > 0;

            if (!relay.IsActive)
                return relay;

            // Add guid if missing
            relays.ForEach(m => m.partId = string.IsNullOrEmpty(m.partId) ? Guid.NewGuid().ToString() : m.partId);

            relay.SupportedTransmitWavelengths.AddRange(relays.Select(m => new WaveLengthData() 
            {
                partId = new Guid(m.partId), 
                wavelength = m.Wavelength,
                isMirror = m.isMirror,
                atmosphericAbsorption = m.CombinedAtmosphericAbsorption 
            })
            .Distinct());
            
            relay.Aperture = relays.Average(m => m.aperture) * Math.Sqrt(relays.Count);
            relay.PowerCapacity = relays.Sum(m => m.getPowerCapacity());
            relay.MinimumRelayWavelenght = relays.Min(m => m.minimumRelayWavelenght);
            relay.MaximumRelayWavelenght = relays.Max(m => m.maximumRelayWavelenght);

            return relay;
        }

        public static VesselMicrowavePersistence getVesselMicrowavePersistanceForVessel(Vessel vessel)
        {
            List<MicrowavePowerTransmitter> transmitters = vessel.FindPartModulesImplementing<MicrowavePowerTransmitter>().Where(m => m.IsEnabled).ToList();

            var vesselTransmitters = new VesselMicrowavePersistence(vessel);
            vesselTransmitters.IsActive = transmitters.Count > 0;

            if (!vesselTransmitters.IsActive)
                return vesselTransmitters;

            // Add guid if missing
            transmitters.ForEach(m => m.partId = string.IsNullOrEmpty(m.partId) ? Guid.NewGuid().ToString() : m.partId );

            vesselTransmitters.SupportedTransmitWavelengths.AddRange(transmitters.Select(m => new WaveLengthData() 
            { 
                partId = new Guid(m.partId), 
                wavelength = m.Wavelength, 
                isMirror = m.isMirror,
                atmosphericAbsorption = m.CombinedAtmosphericAbsorption 
            }));

            vesselTransmitters.Aperture = transmitters.Average(m => m.aperture) * Math.Sqrt(transmitters.Count);
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
        public static VesselMicrowavePersistence getVesselMicrowavePersistanceForProtoVessel(Vessel vessel)
        {
            var transmitter = new VesselMicrowavePersistence(vessel);

            var totalCount = 0;
            var totalAperture = 0.0;
            var totalNuclearPower = 0.0;
            var totalSolarPower = 0.0;
            var totalPowerCapacity = 0.0;

            foreach (var protopart in vessel.protoVessel.protoPartSnapshots)
            {
                foreach (var protomodule in protopart.modules)
                {
                    if (protomodule.moduleName == "MicrowavePowerTransmitter")
                    {
                        bool transmitterIsEnabled = bool.Parse(protomodule.moduleValues.GetValue("IsEnabled"));

                        // filter on active transmitters
                        if (transmitterIsEnabled)
                        {
                            totalCount++;
                            totalAperture += double.Parse(protomodule.moduleValues.GetValue("aperture"));
                            totalNuclearPower += double.Parse(protomodule.moduleValues.GetValue("nuclear_power"));
                            totalSolarPower += double.Parse(protomodule.moduleValues.GetValue("solar_power"));
                            totalPowerCapacity += double.Parse(protomodule.moduleValues.GetValue("power_capacity"));

                            double wavelength = double.Parse(protomodule.moduleValues.GetValue("wavelength"));

                            if (!transmitter.SupportedTransmitWavelengths.Any(m => m.wavelength == wavelength))
                            {
                                bool isMirror = false;
                                try { bool.TryParse(protomodule.moduleValues.GetValue("isMirror"), out isMirror); }
                                catch (Exception e) { UnityEngine.Debug.LogError("[KSPI] - Exception while reading isMirror" + e.Message); }

                                string partId = null;
                                try { protomodule.moduleValues.GetValue("partId"); }
                                catch (Exception e) { UnityEngine.Debug.LogError("[KSPI] - Exception while reading partId" + e.Message); }

                                if (String.IsNullOrEmpty(partId))
                                    try
                                    {
                                        partId = Guid.NewGuid().ToString();
                                        protomodule.moduleValues.SetValue("partId", partId, true);
                                        //UnityEngine.Debug.Log("[KSPI] - Writen partId " + partId);
                                    }
                                    catch (Exception e) { UnityEngine.Debug.LogError("[KSPI] - Exception while writing partId" + e.Message); }

                                transmitter.SupportedTransmitWavelengths.Add(new WaveLengthData()
                                {
                                    partId = partId == null ? Guid.Empty : new Guid(partId),
                                    wavelength = wavelength,
                                    isMirror = isMirror,
                                    atmosphericAbsorption = double.Parse(protomodule.moduleValues.GetValue("atmosphericAbsorption"))
                                });
                            }
                        }
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

        public static VesselRelayPersistence getVesselRelayPersistanceForProtoVessel(Vessel vessel)
        {
            var relay = new VesselRelayPersistence(vessel);

            int totalCount = 0;
            double totalAperture = 0;
            double totalPowerCapacity = 0;
            double minimumRelayWavelength = 1;
            double maximumRelayWavelenght = 0;

            foreach (var protopart in vessel.protoVessel.protoPartSnapshots)
            {

                foreach (var protomodule in protopart.modules)
                {
                    if (protomodule.moduleName == "MicrowavePowerTransmitter")
                    {
                        bool isRelay = bool.Parse(protomodule.moduleValues.GetValue("relay"));
                        bool IsEnabled = bool.Parse(protomodule.moduleValues.GetValue("IsEnabled"));

                        // filter on transmitters
                        if (IsEnabled && isRelay)
                        {
                            totalCount++;
                            totalAperture += double.Parse(protomodule.moduleValues.GetValue("aperture"));
                            totalPowerCapacity += double.Parse(protomodule.moduleValues.GetValue("power_capacity"));

                            var relayWavelenghtMin = double.Parse(protomodule.moduleValues.GetValue("minimumRelayWavelenght"));
                            if (relayWavelenghtMin < minimumRelayWavelength)
                                minimumRelayWavelength = relayWavelenghtMin;

                            var relayWavelenghtMax = double.Parse(protomodule.moduleValues.GetValue("maximumRelayWavelenght"));
                            if (relayWavelenghtMax > maximumRelayWavelenght)
                                maximumRelayWavelenght = relayWavelenghtMax;

                            var wavelength = double.Parse(protomodule.moduleValues.GetValue("wavelength"));

                            bool isMirror = false;
                            try { bool.TryParse(protomodule.moduleValues.GetValue("isMirror"), out isMirror); }
                            catch (Exception e) { UnityEngine.Debug.LogError("[KSPI] - Exception while reading isMirror" + e.Message); }

                            if (!relay.SupportedTransmitWavelengths.Any(m => m.wavelength == wavelength))
                            {
                                string partId = null;
                                try { partId = protomodule.moduleValues.GetValue("partId"); }
                                catch (Exception e) { UnityEngine.Debug.LogError("[KSPI] - Exception while reading partId" + e.Message); }

                                if (String.IsNullOrEmpty(partId))
                                    try{
                                        partId = Guid.NewGuid().ToString();
                                        protomodule.moduleValues.SetValue("partId", partId, true);
                                        UnityEngine.Debug.Log("[KSPI] - Writen partId " + partId);
                                    }
                                    catch (Exception e) { UnityEngine.Debug.LogError("[KSPI] - Exception while writing partId" + e.Message); }

                                relay.SupportedTransmitWavelengths.Add(new WaveLengthData() 
                                {
                                    partId = partId == null ? Guid.Empty : new Guid(partId),
                                    wavelength = wavelength,
                                    isMirror = isMirror,
                                    atmosphericAbsorption = double.Parse(protomodule.moduleValues.GetValue("atmosphericAbsorption"))
                                });
                            }
                        }
                    }
                }
            }

            relay.Aperture = totalAperture;
            relay.PowerCapacity = totalPowerCapacity;
            relay.IsActive = totalCount > 0;
            relay.MinimumRelayWavelenght = minimumRelayWavelength;
            relay.MaximumRelayWavelenght = maximumRelayWavelenght;

            return relay;
        }
    }
}
