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
        protected bool IsEnabled;
        [KSPField(isPersistant = true)]
        protected bool relay;
        [KSPField(isPersistant = true)]
        protected double nuclear_power = 0;
        [KSPField(isPersistant = true)]
        protected double solar_power = 0;
        [KSPField(isPersistant = true)]
        protected double power_capacity = 0;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double wavelength = 0;
        [KSPField(isPersistant = true)]
        public double atmosphericAbsorption = 0.1;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Min Relay Wavelength", guiFormat = "F8", guiUnits = " m")]
        public double minimumRelayWavelenght;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Max Relay Wavelength", guiFormat = "F8", guiUnits = " m")]
        public double maximumRelayWavelenght;
        [KSPField(isPersistant = true)]
        public double aperture = 1;

        //Non Persistent 
        [KSPField(isPersistant = false)]
        public float atmosphereToleranceModifier = 1;
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true)]
        public bool canTransmit = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true)]
        public bool canRelay = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true)]
        public bool canFunctionOnSurface = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true)]
        public bool isMirror = false;
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public int compatibleBeamTypes = 1;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double nativeWaveLength = 0.003189281;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public double nativeAtmosphericAbsorptionPercentage = 10; 

        //GUI 
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Aperture Diameter", guiFormat = "F2", guiUnits = " m")]
        public double apertureDiameter = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Transmitter")]
        public string statusStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Beamed Power")]
        public string beamedpower;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Transmission"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float transmitPower = 100;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Maximum Power", guiUnits = " MW", guiFormat = "F2")]
        public float maximumPower = 10000;

        [KSPField(isPersistant = false)]
        public float powerMult = 1;

        //Internal
        protected Animation anim;
        protected bool hasLinkedReceivers = false;
        protected float displayed_solar_power = 0;

        protected List<ModuleDeployableSolarPanel> panels;
        protected MicrowavePowerReceiver part_receiver;
        protected List<MicrowavePowerReceiver> vessel_recievers;
        protected BeamGenerator beamGenerator;

        [KSPEvent(guiActive = true, guiName = "Activate Transmitter", active = true)]
        public void ActivateTransmitter()
        {
            if (relay) return;

            if (anim != null)
            {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, part.mass);
            }
            IsEnabled = true;

            // update wavelength
            wavelength = Wavelength;
            atmosphericAbsorption = AtmosphericAbsorption;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Transmitter", active = false)]
        public void DeactivateTransmitter()
        {
            if (relay) return;
 
            if (anim != null)
            {
                anim[animName].speed = -1f;
                anim[animName].normalizedTime = 1f;
                anim.Blend(animName, part.mass);
            }
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Relay", active = true)]
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

        [KSPEvent(guiActive = true, guiName = "Deactivate Relay", active = true)]
        public void DeactivateRelay()
        {
            if (!relay) return;

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
                this.atmosphericAbsorption = AtmosphericAbsorption;
                this.hasLinkedReceivers = true;
                return;
            }

            // update stored variables
            this.wavelength = Wavelength;
            this.atmosphericAbsorption = AtmosphericAbsorption;

            // collected all recievers relevant for relay
            var recieversConfiguredForRelay = vessel_recievers.Where(m => m.linkedForRelay).ToList();

            // add build in relay if it can be used for relay
            if (part_receiver != null && canRelay)
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

            ScreenMessages.PostScreenMessage("Microwave Transmitter Updated Wvelength", 10.0f, ScreenMessageStyle.UPPER_CENTER);

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
                anim.Play();
            }

            
            this.part.force_activate();
            ScreenMessages.PostScreenMessage("Microwave Transmitter Force Activated", 5.0f, ScreenMessageStyle.UPPER_CENTER);
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
                // first look locally
                beamGenerator = part.FindModulesImplementing<BeamGenerator>().FirstOrDefault(m => (m.beamType & compatibleBeamTypes) == m.beamType);
                if (beamGenerator != null)
                    beamGenerator.fixedMass = true;

                // then look at parent
                if (beamGenerator == null && part.parent != null)
                    beamGenerator = part.parent.FindModulesImplementing<BeamGenerator>().FirstOrDefault(m => (m.beamType & compatibleBeamTypes) == m.beamType);

                // otherwise find first compatible part attached
                if (beamGenerator == null)
                {
                    beamGenerator = part.attachNodes
                        .Where(m => m.attachedPart != null)
                        .Select(m => m.attachedPart)
                        .SelectMany(m => m.FindModulesImplementing<BeamGenerator>())
                        .FirstOrDefault(m => (m.beamType & compatibleBeamTypes) == m.beamType);
                }

                if (beamGenerator != null)
                {
                    beamGenerator.UpdateMass(this.maximumPower);
                    wavelength = beamGenerator.wavelength;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSP Interstellar] Microwave Transmitter OnStart search for beamGenerator: " + ex);
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

        public override void OnUpdate()
        {
            UpdateRelayWavelength();

            bool vesselInSpace = (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL);
            bool receiver_on = part_receiver != null && part_receiver.isActive();
            bool canBeActive = CanBeActive;

            if (anim != null && !canBeActive && IsEnabled)
            {
                if (relay)
                    DeactivateRelay();
                else
                    DeactivateTransmitter();
            }

            var canOperateInCurrentEnvironment = this.canFunctionOnSurface || vesselInSpace;
            var vesselCanTransmit = canTransmit && canOperateInCurrentEnvironment;

            Events["ActivateTransmitter"].active = beamGenerator != null && vesselCanTransmit && !IsEnabled && !relay && !receiver_on && canBeActive;
            Events["DeactivateTransmitter"].active = beamGenerator != null && vesselCanTransmit && IsEnabled && !relay;

            var vesselCanRelay = this.hasLinkedReceivers && canOperateInCurrentEnvironment;

            Events["ActivateRelay"].active = vesselCanRelay && !IsEnabled && !relay && !receiver_on && canBeActive;
            Events["DeactivateRelay"].active = vesselCanRelay && IsEnabled && relay;

            Fields["beamedpower"].guiActive = IsEnabled && !relay && canBeActive;
            Fields["transmitPower"].guiActive = IsEnabled && !relay;

            if (IsEnabled)
            {
                if (relay)
                    statusStr = "Relay Active";
                else
                    statusStr = "Transmitter Active";
            }
            else
                statusStr = "Inactive.";

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
            nuclear_power = 0;
            solar_power = 0;
            displayed_solar_power = 0;
            power_capacity = maximumPower;

            base.OnFixedUpdate();
            if (beamGenerator != null && IsEnabled && !relay)
            {
                var availableReactorPower = Math.Max(getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) - getCurrentHighPriorityResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES), 0);
               
                var requestedPower = Math.Min(maximumPower, availableReactorPower * transmitPower / 100);
                var receivedPower = consumeFNResource(requestedPower * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);

                nuclear_power += beamGenerator.efficiencyPercentage * receivedPower * 10 / TimeWarp.fixedDeltaTime;

                // generate wasteheat for converting lectric power to beamed power
                supplyFNResource(receivedPower * (100 - beamGenerator.efficiencyPercentage) * 0.01, FNResourceManager.FNRESOURCE_WASTEHEAT);

                foreach (ModuleDeployableSolarPanel panel in panels)
                {
                    double output = panel.flowRate;

                    // attempt to retrieve all solar power output
                    if (output == 0.0)
                    {
                        var partModulesList = panel.part.parent.Modules;
                        foreach (var module in partModulesList)
                        {
                            var solarmodule = module as ModuleDeployableSolarPanel;
                            if (solarmodule != null)
                                output += solarmodule.flowRate;
                        }
                    }

                    double spower = part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, output * TimeWarp.fixedDeltaTime);

                    var distanceBetweenVesselAndSun  = Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                    var distanceBetweenSunAndKerbin = Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                    double inv_square_mult = Math.Pow(distanceBetweenSunAndKerbin, 2) / Math.Pow(distanceBetweenVesselAndSun, 2);

                    displayed_solar_power += (float)(spower / TimeWarp.fixedDeltaTime);
                    //scale solar power to what it would be in Kerbin orbit for file storage
                    solar_power += (spower / TimeWarp.fixedDeltaTime / inv_square_mult);
                }
            }

            if (double.IsInfinity(nuclear_power) || double.IsNaN(nuclear_power))
                nuclear_power = 0;

            if (double.IsInfinity(solar_power) || double.IsNaN(solar_power))
                solar_power = 0;
        }

        public double getPowerCapacity()
        {
            return power_capacity > 0 ? power_capacity : maximumPower;
        }

        public double Wavelength
        {
            get { return beamGenerator != null ? beamGenerator.wavelength : nativeWaveLength; }
        }

        public double AtmosphericAbsorption
        {
            get { return beamGenerator != null ? beamGenerator.atmosphericAbsorptionPercentage / 100 : nativeAtmosphericAbsorptionPercentage / 100; }
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

            relay.SupportedTransmitWavelengths.AddRange(relays.Select(m => new WaveLengthData() { wavelength = m.Wavelength, atmosphericAbsorption = m.AtmosphericAbsorption }).Distinct());
            
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

            vesselTransmitters.SupportedTransmitWavelengths.AddRange(transmitters.Select(m => new WaveLengthData() { wavelength = m.Wavelength, atmosphericAbsorption = m.AtmosphericAbsorption }));

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
            var totalWaveLength = 0.0;
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
                        bool IsEnabled = bool.Parse(protomodule.moduleValues.GetValue("IsEnabled"));

                        // filter on active transmitters
                        if (IsEnabled)
                        {
                            totalCount++;
                            totalWaveLength += double.Parse(protomodule.moduleValues.GetValue("wavelength"));
                            totalAperture += double.Parse(protomodule.moduleValues.GetValue("aperture"));
                            totalNuclearPower += double.Parse(protomodule.moduleValues.GetValue("nuclear_power"));
                            totalSolarPower += double.Parse(protomodule.moduleValues.GetValue("solar_power"));
                            totalPowerCapacity += double.Parse(protomodule.moduleValues.GetValue("power_capacity"));

                            var wavelength = double.Parse(protomodule.moduleValues.GetValue("wavelength"));
                            if (!transmitter.SupportedTransmitWavelengths.Any(m => m.wavelength == wavelength))
                            {
                                transmitter.SupportedTransmitWavelengths.Add(new WaveLengthData()
                                {
                                    wavelength = wavelength,
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
                            if (!relay.SupportedTransmitWavelengths.Any(m => m.wavelength == wavelength))
                            {
                                relay.SupportedTransmitWavelengths.Add(new WaveLengthData() 
                                {
                                    wavelength = wavelength,
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
