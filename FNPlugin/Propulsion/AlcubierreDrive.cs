using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class AlcubierreDrive : ResourceSuppliableModule
    {
        // persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false)]
        public bool IsCharging = false;
        [KSPField(isPersistant = true)]
        private double existing_warp_speed;
        [KSPField(isPersistant = true)]
        public bool warpInit = false;
        [KSPField(isPersistant = true)]
        public int selected_factor = -1;

        // non persistant
        [KSPField]
        public int InstanceID;
        [KSPField]
        public bool IsSlave;
        [KSPField]
        public string AnimationName = String.Empty;
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public string originalName = "";
        [KSPField]
        public float effectSize1 = 0;
        [KSPField]
        public float effectSize2 = 0;
        [KSPField]
        public string upgradeTechReq = "";
        [KSPField]
        public double powerRequirementMultiplier = 1;

        [KSPField(guiActiveEditor = true, guiName = "Reaction Wheel Strength")]
        public float reactionWheelStrength = 100;
        [KSPField]
        public float activeWheelStrength;
        [KSPField]
        public long maxPowerTimeout = 50;
        [KSPField]
        public long headingChangedTimeout = 50;

        [KSPField] 
        public float warpPowerMultTech0 = 10;
        [KSPField]
        public float warpPowerMultTech1 = 20;

        [KSPField]
        public double wasteheatRatio = 0.5;
        [KSPField(isPersistant = false)]
        public double wasteheatRatioUpgraded = 0.25;
        [KSPField(isPersistant = false)]
        public double wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
        public string warpdriveType = "Alcubierre Drive";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Rate Index")]
        public int currentRateIndex;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Warp engine mass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Total Warp Power", guiFormat = "F1", guiUnits = " t")]
        public float sumOfAlcubierreDrives;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Vessel Total Mass", guiFormat = "F4", guiUnits = " t")]
        public float vesselTotalMass;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Warp to Mass Ratio", guiFormat = "F4")]
        public float warpToMassRatio;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Gravity At Surface", guiUnits = " m/s\xB2", guiFormat = "F5")]
        public double gravityAtSeaLevel;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Gravity Vessel Pull", guiUnits = " m/s\xB2", guiFormat = "F5")]
        public double gravityPull;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Gravity Drag Ratio", guiFormat = "F5")]
        public double gravityDragRatio;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Gravity Drag %", guiUnits = "%", guiFormat = "F3")]
        public double gravityDragPercentage;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "GravityRatio")]
        public double gravityRatio;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Warp Gravity Limit", guiUnits = "c", guiFormat = "F4")]
        public double maximumWarpForGravityPull;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Warp Altitude Limit", guiUnits = "c", guiFormat = "F4")]
        public double maximumWarpForAltitude;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Warp Weighted Limit", guiUnits = "c", guiFormat = "F4")]
        public double maximumWarpWeighted;


        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Max Allowed Throtle", guiUnits = "c", guiFormat = "F4")]
        public double maximumAllowedWarpThrotle;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Selected Speed", guiUnits = "c", guiFormat = "F4")]
        public double warpEngineThrottle;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Speed of light")]
        public double speedOfLight;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Magnitude Diff")]
        public double magnitudeDiff;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Req Exotic Matter", guiUnits = " MW", guiFormat = "F2")]
        public double exotic_power_required = 1000;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Abs Min Power Warp", guiUnits = " MW", guiFormat = "F4")]
        public double minPowerRequirementForLightSpeed;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Cur Power for Warp ", guiUnits = " MW", guiFormat = "F4")]
        public double currentPowerRequirementForWarp;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Power Max Speed", guiUnits = " MW", guiFormat = "F4")]
        public double powerRequirementForMaximumAllowedLightSpeed;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Exit Speed", guiUnits = " m/s", guiFormat = "F3")]
        public double exitSpeed;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Exit Apoapsis", guiUnits = " km", guiFormat = "F3")]
        public double exitApoapsis;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Exit Periapsis", guiUnits = " km", guiFormat = "F3")]
        public double exitPeriapsis;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Exit Eccentricity", guiFormat = "F3")]
        public double exitEccentricity;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Exit Mean Anomaly", guiUnits = "\xB0", guiFormat = "F3")]
        public double exitMeanAnomaly;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Exit Burn to Circularize", guiUnits = " m/s", guiFormat = "F3")]
        public double exitBurnCircularize;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string driveStatus;


        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public string serialisedwarpvector;

        //private double[] engine_throtle = { 0.001, 0.0016, 0.0025, 0.004, 0.0063, 0.01, 0.016, 0.025, 0.04, 0.063, 0.1, 0.16, 0.25, 0.40, 0.63, 1.0, 1.6, 2.5, 4.0, 6.3, 10, 16, 25, 40, 63, 100, 160, 250, 400, 630, 1000 };
        private double[] engine_throtle = { 0.001, 0.0013, 0.0016, 0.002, 0.0025, 0.0032, 0.004, 0.005, 0.0063, 0.008, 0.01, 0.013, 0.016, 0.02, 0.025, 0.032, 0.04, 0.05, 0.063, 0.08, 0.1, 0.13, 0.16, 0.2, 0.25, 0.32, 0.4, 0.5, 0.63, 0.8, 1, 1.3, 1.6, 2, 2.5, 3.2, 4, 5, 6.3, 8, 10, 13, 16, 20, 25, 32, 40, 50, 63, 80, 100, 130, 160, 200, 250, 320, 400, 500, 630, 800, 1000 };

        private GameObject warp_effect;
        private GameObject warp_effect2;
        private Texture[] warp_textures;
        private Texture[] warp_textures2;
        private AudioSource warp_sound;

        private double tex_count;
        private float previousDeltaTime;
        private float warp_size = 50000;

        private bool vesselWasInOuterspace;
        private bool hasrequiredupgrade;
        private bool render_window;

        private Rect windowPosition;
        private AnimationState[] animationState;
        private Vector3d heading_act;
        private Vector3d active_part_heading;
        private List<AlcubierreDrive> alcubierreDrives;

        private float windowPositionX = 200;
        private float windowPositionY = 100;

        private GUIStyle bold_black_style;
        private GUIStyle text_black_style;

        private int windowID = 252824373;
        private int old_selected_factor = 0;
        private int minimum_selected_factor;
        private int maximumWarpSpeedFactor;
        private int minimumPowerAllowedFactor;
        private long insufficientPowerTimeout = 10;

        private long initiateWarpTimeout;
        private long counterCurrent;
        private long counterPreviousChange;

        private PartResource wasteheatPowerResource;
        private PartResource exoticMatterResource;

        private Renderer warp_effect1_renderer;
        private Renderer warp_effect2_renderer;

        private Collider warp_effect1_collider;
        private Collider warp_effect2_collider;

        private Orbit predictedExitOrbit;
        private PartResourceDefinition exoticResourceDefinition;
        private CelestialBody warpInitialMainBody;
        private ModuleReactionWheel moduleReactionWheel;

        [KSPEvent(guiActive = true, guiName = "Warp Control Window", active = true, guiActiveUnfocused = true, unfocusedRange = 5f, guiActiveUncommand = true)]
        public void ToggleWarpControlWindow()
        {
            render_window = !render_window;
        }

        [KSPEvent(guiActive = true, guiName = "Start Charging", active = true)]
        public void StartCharging()
        {
            Debug.Log("[KSPI] - Start Charging pressed");

            if (IsEnabled) return;

            if (warpToMassRatio < 1)
            {
                ScreenMessages.PostScreenMessage("Warp Power to Vessel Mass is to low to create a stable warp field");
                return;
            }

            insufficientPowerTimeout = maxPowerTimeout;
            IsCharging = true;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Charging", active = false)]
        public void StopCharging()
        {
            Debug.Log("[KSPI] - Stop Charging button pressed");
            IsCharging = false;

            // flush all exotic matter
            double exoticMatterAmount;
            double exoticMatterMaxAmount;
            part.GetConnectedResourceTotals(exoticResourceDefinition.id, out exoticMatterAmount, out exoticMatterMaxAmount);

            part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, exoticMatterAmount);
        }

        [KSPEvent(guiActive = true, guiName = "Activate Warp Drive", active = true)]
        public void ActivateWarpDrive()
        {
            Debug.Log("[KSPI] - Activate Warp Drive button pressed");
            if (IsEnabled) return;

            if (warpToMassRatio < 1)
            {
                const string message = "Not enough warp power to warp vessel";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            Vessel vess = this.part.vessel;
            if (vess.altitude <= PluginHelper.getMaxAtmosphericAltitude(vess.mainBody) && vess.mainBody.flightGlobalsIndex != 0)
            {
                const string message = "Cannot activate warp drive within the atmosphere!";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            double exoticMatterAvailable;
            double totalExoticMatterAvailable;
            part.GetConnectedResourceTotals(exoticResourceDefinition.id, out exoticMatterAvailable, out totalExoticMatterAvailable);

            if (exoticMatterAvailable < exotic_power_required)
            {
                const string message = "Warp drive isn't fully charged yet for Warp!";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            if (maximumWarpSpeedFactor < selected_factor)
                selected_factor = minimumPowerAllowedFactor;

            double newWarpfactor = engine_throtle[selected_factor];

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(newWarpfactor);

            if (!CheatOptions.InfiniteElectricity && currentPowerRequirementForWarp > getStableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES))
            {
                const string message = "Warp power requirement is higher that maximum power supply!";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            IsCharging = false;
            initiateWarpTimeout = 10;
        }

        private int GetMaximumFactor(double lightspeed)
        {
            var maxFactor = 0;

            for (var i = 0; i < engine_throtle.Count(); i++)
            {
                if (engine_throtle[i] > lightspeed)
                    return maxFactor;
                maxFactor = i;
            }
            return maxFactor;
        }

        private void InitiateWarp()
        {
            Debug.Log("[KSPI] - InitiateWarp started");
            if (maximumWarpSpeedFactor < selected_factor)
                selected_factor = minimumPowerAllowedFactor;

            var newWarpSpeed = engine_throtle[selected_factor];

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(newWarpSpeed);

            double powerReturned = CheatOptions.InfiniteElectricity 
                ? currentPowerRequirementForWarp
                : consumeFNResourcePerSecond(currentPowerRequirementForWarp, ResourceManager.FNRESOURCE_MEGAJOULES);

            if (powerReturned < 0.99 * currentPowerRequirementForWarp)
            {
                initiateWarpTimeout--;

                if (initiateWarpTimeout == 1)
                {
                    while (selected_factor != minimum_selected_factor)
                    {
                        Debug.Log("[KSPI] - call ReduceWarpPower");
                        ReduceWarpPower();
                        newWarpSpeed = engine_throtle[selected_factor];
                        currentPowerRequirementForWarp = GetPowerRequirementForWarp(newWarpSpeed);
                        if (powerReturned >= currentPowerRequirementForWarp)
                            return;
                    }
                }
                if (initiateWarpTimeout == 0)
                {
                    var message = "Not enough power to initiate warp!" + powerReturned + " " + currentPowerRequirementForWarp;
                    Debug.Log("[KSPI] - " + message);
                    ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    IsCharging = true;
                    return;
                }
            }

            initiateWarpTimeout = 0; // stop initiating to warp
            vesselWasInOuterspace = (this.vessel.altitude > this.vessel.mainBody.atmosphereDepth * 10);
 
            // consume all exotic matter to create warp field
            part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, exotic_power_required);

            warp_sound.Play();
            warp_sound.loop = true;

            warpInitialMainBody = vessel.mainBody;

            // prevent g-force effects
            part.vessel.IgnoreGForces(1);

            active_part_heading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);

            heading_act = active_part_heading * speedOfLight * newWarpSpeed;
            serialisedwarpvector = ConfigNode.WriteVector(heading_act);

            if (!this.vessel.packed)
                vessel.GoOnRails();

            //var newHeading = vessel.orbit.vel + heading_act;

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + heading_act, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            if (!this.vessel.packed)
                vessel.GoOffRails();
            
            IsEnabled = true;

            existing_warp_speed = newWarpSpeed;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Warp Drive", active = false)]
        public void DeactivateWarpDrive()
        {
            Debug.Log("[KSPI] - Deactivate Warp Drive event called");

            if (!IsEnabled)
            {
                Debug.Log("[KSPI] - canceled, Warp Drive is already inactive");
                return;
            }

            // prevent g-force effects
            part.vessel.IgnoreGForces(1);

            // mark warp to be disabled
            IsEnabled = false;
            // Disable sound
            warp_sound.Stop();

            Vector3d heading = heading_act;
            heading.x = -heading.x;
            heading.y = -heading.y;
            heading.z = -heading.z;

            if (!this.vessel.packed)
                vessel.GoOnRails();

            var newHeading = vessel.orbit.vel + heading;
            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, newHeading, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            if (!this.vessel.packed)
                vessel.GoOffRails();

            if (warpInitialMainBody != null && vessel.mainBody != warpInitialMainBody)
            {
                Debug.Log("[KSPI] - Develocitize");
                Develocitize();
            }
        }

        [KSPEvent(guiActive = true, guiName = "Increase Warp Speed (+)", active = true)]
        public void ToggleWarpSpeedUp()
        {
            Debug.Log("[KSPI] - Warp Throttle (+) button pressed");
            selected_factor++;
            if (selected_factor >= engine_throtle.Length)
                selected_factor = engine_throtle.Length - 1;

            if (!IsEnabled)
                old_selected_factor = selected_factor;
        }

        private void ToggleWarpSpeedUp3()
        {
            Debug.Log("[KSPI] - 3x Speed Up pressed");

            for (var i = 0; i < 3; i++)
            {
                selected_factor++;

                if (selected_factor < engine_throtle.Length) continue;

                selected_factor = engine_throtle.Length - 1;
                break;
            }

            if (!IsEnabled)
                old_selected_factor = selected_factor;
        }

        private void ToggleWarpSpeedUp10()
        {
            Debug.Log("[KSPI] - 10x Speed Up pressed");

            for (var i = 0; i < 10; i++)
            {
                selected_factor++;

                if (selected_factor < engine_throtle.Length) continue;

                selected_factor = engine_throtle.Length - 1;
                break;
            }

            if (!IsEnabled)
                old_selected_factor = selected_factor;
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, guiName = "Decrease Warp Speed (-)", active = true)]
        public void ToggleWarpSpeedDown()
        {
            Debug.Log("[KSPI] - Warp Throttle (-) button pressed");
            selected_factor--;
            if (selected_factor < 0)
                selected_factor = 0;

            if (!IsEnabled)
                old_selected_factor = selected_factor;
        }

        private void ToggleWarpSpeedDown3()
        {
            Debug.Log("[KSPI] - 3x Speed Down pressed");

            for (var i = 0; i < 3; i++)
            {
                selected_factor--;

                if (selected_factor >= 0) continue;

                selected_factor = 0;
                break;
            }

            if (!IsEnabled)
                old_selected_factor = selected_factor;
        }

        private void ToggleWarpSpeedDown10()
        {
            Debug.Log("[KSPI] - 10x Speed Down pressed");

            for (var i = 0; i  < 10; i++)
            {
                selected_factor--;

                if (selected_factor >= 0) continue;

                selected_factor = 0;
                break;
            }

            if (!IsEnabled)
                old_selected_factor = selected_factor;
        }

        [KSPEvent(guiActive = true, guiName = "Reduce Warp Power", active = true)]
        public void ReduceWarpPower()
        {
            Debug.Log("[KSPI] - Reduce Warp Power button pressed");
            if (selected_factor == minimum_selected_factor) return;

            if (selected_factor < minimum_selected_factor)
                ToggleWarpSpeedUp();
            else if (selected_factor > minimum_selected_factor)
                ToggleWarpSpeedDown();
        }

        [KSPAction("Toggle Warp Control Window")]
        public void ToggleWarpControlWindowAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggled Warp Control Window");
            ToggleWarpControlWindow();
        }

        [KSPAction("Start Charging")]
        public void StartChargingAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Start Charging Action activated");
            StartCharging();
        }

        [KSPAction("Stop Charging")]
        public void StopChargingAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Stop Charging Action activated");
            StopCharging();
        }

        [KSPAction("Toggle Charging")]
        public void ToggleChargingAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggle Charging Action activated");
            if (IsCharging)
                StopCharging();
            else
                StartCharging();
        }

        [KSPAction("Reduce Warp Power")]
        public void ReduceWarpDriveAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - ReduceWarpPower action activated");
            ReduceWarpPower();
        }

        [KSPAction("Activate Warp Drive")]
        public void ActivateWarpDriveAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Activate Warp Drive action activated");
            ActivateWarpDrive();
        }

        [KSPAction("Deactivate Warp Drive")]
        public void DeactivateWarpDriveAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Deactivate Warp Drive action activated");
            DeactivateWarpDrive();
        }

        [KSPAction("Increase Warp Speed (+)")]
        public void ToggleWarpSpeedUpAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggle Warp SpeedUp pressed");
            ToggleWarpSpeedUp();
        }

        [KSPAction("Increase Warp Speed x3 (+)")]
        public void ToggleWarpSpeedUpAction3(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggle Warp Speed Up x3 pressed");
            ToggleWarpSpeedUp3();
        }

        [KSPAction("Increase Warp Speed x3 (+)")]
        public void ToggleWarpSpeedUpAction10(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggle Warp Speed Up x10 pressed");
            ToggleWarpSpeedUp10();
        }


        [KSPAction("Decrease Warp Speed (-)")]
        public void ToggleWarpSpeedDownAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggle Warp Speed Down pressed");
            ToggleWarpSpeedDown();
        }

        [KSPAction("Decrease Warp Speed x3 (-)")]
        public void ToggleWarpSpeedDownAction3(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggle Warp Speed Down x3 pressed");
            ToggleWarpSpeedDown3();
        }

        [KSPAction("Decrease Warp Speed x10 (-)")]
        public void ToggleWarpSpeedDownAction10(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Toggle Warp Speed Down x10 pressed");
            ToggleWarpSpeedDown10();
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitDrive()
        {
            Debug.Log("[KSPI] - Retrofit button pressed");
            if (ResearchAndDevelopment.Instance == null) return;

            if (isupgraded || ResearchAndDevelopment.Instance.Science < UpgradeCost()) return;

            isupgraded = true;
            warpdriveType = upgradedName;

            ResearchAndDevelopment.Instance.AddScience(-UpgradeCost(), TransactionReasons.RnDPartPurchase);
        }

        private float UpgradeCost()
        {
            return 0;
        }

        public override void OnStart(PartModule.StartState state)
        {
            windowPosition = new Rect(windowPositionX, windowPositionY, 260, 100);
            windowID = new System.Random(part.GetInstanceID()).Next(int.MaxValue);

            moduleReactionWheel = part.FindModuleImplementing<ModuleReactionWheel>();

            exoticResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.ExoticMatter);

            wasteheatPowerResource = part.Resources[ResourceManager.FNRESOURCE_WASTEHEAT];
            exoticMatterResource = part.Resources[InterstellarResourcesConfiguration.Instance.ExoticMatter];

            speedOfLight = PluginHelper.SpeedOfLight;
            
            // reset Exotic Matter Capacity
            if (exoticMatterResource != null)
            {
                var ratio = Math.Min(1, Math.Max(0, exoticMatterResource.amount / exoticMatterResource.maxAmount));
                exoticMatterResource.maxAmount = 0.001;
                exoticMatterResource.amount = exoticMatterResource.maxAmount * ratio;
            }

            InstanceID = GetInstanceID();

            if (IsSlave)
                Debug.Log("KSPI - AlcubierreDrive Slave " + InstanceID + " Started");
            else
                Debug.Log("KSPI - AlcubierreDrive Master " + InstanceID + " Started");

            if (!String.IsNullOrEmpty(AnimationName))
                animationState = SetUpAnimation(AnimationName, this.part);

            try
            {
                Events["StartCharging"].active = !IsSlave;
                Events["StopCharging"].active = !IsSlave;
                Events["ActivateWarpDrive"].active = !IsSlave;
                Events["DeactivateWarpDrive"].active = !IsSlave;
                Events["ToggleWarpSpeedUp"].active = !IsSlave;
                Events["ToggleWarpSpeedDown"].active = !IsSlave;
                Events["ReduceWarpPower"].active = !IsSlave;
                Events["ToggleWarpControlWindow"].active = !IsSlave;

                //Fields["exotic_power_required"].guiActive = !IsSlave;
                Fields["warpEngineThrottle"].guiActive = !IsSlave;
                Fields["maximumAllowedWarpThrotle"].guiActive = !IsSlave;
                Fields["warpToMassRatio"].guiActive = !IsSlave;
                //Fields["vesselTotalMass"].guiActive = !IsSlave;
                
                Fields["minPowerRequirementForLightSpeed"].guiActive = !IsSlave;
                Fields["currentPowerRequirementForWarp"].guiActive = !IsSlave;
                Fields["sumOfAlcubierreDrives"].guiActive = !IsSlave;
                Fields["powerRequirementForMaximumAllowedLightSpeed"].guiActive = !IsSlave;

                Actions["StartChargingAction"].guiName = Events["StartCharging"].guiName = String.Format("Start Charging");
                Actions["StopChargingAction"].guiName = Events["StopCharging"].guiName = String.Format("Stop Charging");
                Actions["ToggleChargingAction"].guiName = String.Format("Toggle Charging");
                Actions["ActivateWarpDriveAction"].guiName = Events["ActivateWarpDrive"].guiName = String.Format("Activate Warp Drive");
                Actions["DeactivateWarpDriveAction"].guiName = Events["DeactivateWarpDrive"].guiName = String.Format("Deactivate Warp Drive");
                Actions["ToggleWarpSpeedUpAction"].guiName = Events["ToggleWarpSpeedUp"].guiName = String.Format("Warp Speed (+)");
                Actions["ToggleWarpSpeedDownAction"].guiName = Events["ToggleWarpSpeedDown"].guiName = String.Format("Warp Speed (-)");

                minimum_selected_factor = engine_throtle.ToList().IndexOf(engine_throtle.First(w => w == 1f));
                if (selected_factor == -1)
                    selected_factor = minimum_selected_factor;

                hasrequiredupgrade = PluginHelper.upgradeAvailable(upgradeTechReq);
                if (hasrequiredupgrade)
                    isupgraded = true;

                warpdriveType = isupgraded ? upgradedName : originalName;

                if (state == StartState.Editor) return;

                UpdateWateheatBuffer(0.95);

                if (!IsSlave)
                {
                    Debug.Log("KSPI - AlcubierreDrive Create Slaves");
                    alcubierreDrives = part.vessel.FindPartModulesImplementing<AlcubierreDrive>();
                    foreach (var drive in alcubierreDrives)
                    {
                        var driveId = drive.GetInstanceID();

                        if (driveId == InstanceID) continue;

                        drive.IsSlave = true;
                        Debug.Log("KSPI - AlcubierreDrive " + driveId + " != " + InstanceID);
                    }
                }

                Debug.Log("KSPI - AlcubierreDrive OnStart step C ");

                this.part.force_activate();
                if (serialisedwarpvector != null)
                    heading_act = ConfigNode.ParseVector3D(serialisedwarpvector);

                warp_effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                warp_effect2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

                warp_effect1_renderer = warp_effect.GetComponent<Renderer>();
                warp_effect2_renderer = warp_effect2.GetComponent<Renderer>();

                warp_effect1_collider = warp_effect.GetComponent<Collider>();
                warp_effect2_collider = warp_effect2.GetComponent<Collider>();

                warp_effect1_collider.enabled = false;
                warp_effect2_collider.enabled = false;

                Vector3 shipPos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
                Vector3 endBeamPos = shipPos + transform.up * warp_size;
                Vector3 midPos = (shipPos - endBeamPos) / 2.0f;

                warp_effect.transform.localScale = new Vector3(effectSize1, midPos.magnitude, effectSize1);
                warp_effect.transform.position = new Vector3(midPos.x, shipPos.y + midPos.y, midPos.z);
                warp_effect.transform.rotation = part.transform.rotation;

                warp_effect2.transform.localScale = new Vector3(effectSize2, midPos.magnitude, effectSize2);
                warp_effect2.transform.position = new Vector3(midPos.x, shipPos.y + midPos.y, midPos.z);
                warp_effect2.transform.rotation = part.transform.rotation;

                //warp_effect.layer = LayerMask.NameToLayer("Ignore Raycast");
                //warp_effect.renderer.material = new Material(KSP.IO.File.ReadAllText<AlcubierreDrive>("AlphaSelfIllum.shader"));

                warp_effect1_renderer.material.shader = Shader.Find("Unlit/Transparent");
                warp_effect2_renderer.material.shader = Shader.Find("Unlit/Transparent");

                warp_textures = new Texture[33];

                const string warpTecturePath = "WarpPlugin/ParticleFX/warp";
                for (var i = 0; i < 11; i++)
                {
                    warp_textures[i] = GameDatabase.Instance.GetTexture((i > 0)
                        ? warpTecturePath + (i + 1)
                        : warpTecturePath, false);
                }

                warp_textures[11] = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warp10", false);
                for (var i = 12; i < 33; i++)
                {
                    var j = i > 17 ? 34 - i : i;
                    warp_textures[i] = GameDatabase.Instance.GetTexture(j > 1 ?
                        warpTecturePath + (j + 1) : warpTecturePath, false);
                }

                warp_textures2 = new Texture[33];

                const string warprTecturePath = "WarpPlugin/ParticleFX/warpr";
                for (var i = 0; i < 11; i++)
                {
                    warp_textures2[i] = GameDatabase.Instance.GetTexture((i > 0)
                        ? warprTecturePath + (i + 1)
                        : warprTecturePath, false);
                }

                warp_textures2[11] = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warpr10", false);
                for (var i = 12; i < 33; i++)
                {
                    var j = i > 17 ? 34 - i : i;
                    warp_textures2[i] = GameDatabase.Instance.GetTexture(j > 1 ?
                        warprTecturePath + (j + 1) : warprTecturePath, false);
                }

                warp_effect1_renderer.material.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);
                warp_effect2_renderer.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, 0.1f);

                warp_effect1_renderer.material.mainTexture = warp_textures[0];
                warp_effect1_renderer.receiveShadows = false;
                //warp_effect.layer = LayerMask.NameToLayer ("Ignore Raycast");
                //warp_effect.collider.isTrigger = true;
                warp_effect2_renderer.material.mainTexture = warp_textures2[0];
                warp_effect2_renderer.receiveShadows = false;
                warp_effect2_renderer.material.mainTextureOffset = new Vector2(-0.2f, -0.2f);
                //warp_effect2.layer = LayerMask.NameToLayer ("Ignore Raycast");
                //warp_effect2.collider.isTrigger = true;
                warp_effect2_renderer.material.renderQueue = 1000;
                warp_effect1_renderer.material.renderQueue = 1001;
                /*gameObject.AddComponent<Light>();
                gameObject.light.color = Color.cyan;
                gameObject.light.intensity = 1f;
                gameObject.light.range = 4000f;
                gameObject.light.type = LightType.Spot;
                gameObject.light.transform.position = end_beam_pos;
                gameObject.light.cullingMask = ~0;*/

                //warp_effect.transform.localScale.y = 2.5f;
                //warp_effect.transform.localScale.z = 200f;

                warp_sound = gameObject.AddComponent<AudioSource>();
                warp_sound.clip = GameDatabase.Instance.GetAudioClip("WarpPlugin/Sounds/warp_sound");
                warp_sound.volume = GameSettings.SHIP_VOLUME;

                //warp_sound.panLevel = 0;
                warp_sound.panStereo = 0;
                warp_sound.rolloffMode = AudioRolloffMode.Linear;
                warp_sound.Stop();

                if (IsEnabled)
                {
                    warp_sound.Play();
                    warp_sound.loop = true;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - AlcubierreDrive OnStart 1 Exception " + e.Message);
            }

            warpdriveType = originalName;

        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsEditor) return;

            float massOfAlcubiereDrives;
            vessel.GetVesselAndModuleMass<AlcubierreDrive>(out vesselTotalMass, out massOfAlcubiereDrives);

            sumOfAlcubierreDrives = massOfAlcubiereDrives * (isupgraded ? warpPowerMultTech1 : warpPowerMultTech0);
            warpToMassRatio = vesselTotalMass > 0 ? sumOfAlcubierreDrives / vesselTotalMass : 0;
        }

        public override void OnUpdate()
        {
            Events["StartCharging"].active = !IsSlave && !IsCharging;
            Events["StopCharging"].active = !IsSlave && IsCharging;
            Events["ActivateWarpDrive"].active = !IsSlave && !IsEnabled;
            Events["DeactivateWarpDrive"].active = !IsSlave && IsEnabled;
            Fields["exitSpeed"].guiActive = IsEnabled;
            Fields["driveStatus"].guiActive = !IsSlave && IsCharging;

            if (moduleReactionWheel != null)
            {
                activeWheelStrength = IsEnabled ? reactionWheelStrength * warpToMassRatio : reactionWheelStrength;

                moduleReactionWheel.PitchTorque = IsEnabled ? reactionWheelStrength * warpToMassRatio : reactionWheelStrength;
                moduleReactionWheel.YawTorque = IsEnabled ? reactionWheelStrength * warpToMassRatio : reactionWheelStrength;
                moduleReactionWheel.RollTorque = IsEnabled ? reactionWheelStrength * warpToMassRatio : reactionWheelStrength;
            }

            if (ResearchAndDevelopment.Instance != null)
                Events["RetrofitDrive"].active = !IsSlave && !isupgraded && ResearchAndDevelopment.Instance.Science >= UpgradeCost() && hasrequiredupgrade;
            else
                Events["RetrofitDrive"].active = false;

            if (animationState == null) return;

            foreach (var anim in animationState)
            {
                if ((IsEnabled || IsCharging) && anim.normalizedTime < 1) { anim.speed = 1; }
                if ((IsEnabled || IsCharging) && anim.normalizedTime >= 1)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 1;
                }

                if (!IsEnabled && !IsCharging && anim.normalizedTime > 0) { anim.speed = -1; }
                if (IsEnabled || IsCharging || !(anim.normalizedTime <= 0)) continue;

                anim.speed = 0;
                anim.normalizedTime = 0;
            }
        }

        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            if (vessel == null) return;

            UpdateWateheatBuffer();

            warpEngineThrottle = engine_throtle[selected_factor];

            gravityPull = FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude;
            gravityAtSeaLevel = vessel.mainBody.GeeASL * GameConstants.STANDARD_GRAVITY;
            gravityRatio = gravityAtSeaLevel > 0 ? Math.Min(1, gravityPull / gravityAtSeaLevel) : 0;
            gravityDragRatio = Math.Pow(Math.Min(1, 1 - gravityRatio), Math.Max(1, Math.Sqrt(gravityAtSeaLevel)));
            gravityDragPercentage = (1 - gravityDragRatio) * 100;
            maximumWarpForGravityPull = gravityPull > 0 ? 1 / gravityPull : 0;
            maximumWarpForAltitude =  Math.Abs(vessel.altitude / speedOfLight);
            maximumWarpWeighted = gravityRatio * maximumWarpForGravityPull + (1 - gravityRatio) * maximumWarpForAltitude;
            maximumWarpSpeedFactor = GetMaximumFactor(maximumWarpWeighted);
            maximumAllowedWarpThrotle = engine_throtle[maximumWarpSpeedFactor];
            minimumPowerAllowedFactor = maximumWarpSpeedFactor > minimum_selected_factor ? maximumWarpSpeedFactor : minimum_selected_factor;

            if (alcubierreDrives != null)
                sumOfAlcubierreDrives = alcubierreDrives.Sum(p => p.partMass * (p.isupgraded ? warpPowerMultTech1 : warpPowerMultTech0)); 

            vesselTotalMass = vessel.GetTotalMass();
            if (sumOfAlcubierreDrives != 0 && vesselTotalMass != 0)
            {
                warpToMassRatio = sumOfAlcubierreDrives / vesselTotalMass;
                exotic_power_required = (GameConstants.initial_alcubierre_megajoules_required * vesselTotalMass * powerRequirementMultiplier) / warpToMassRatio;
            }

            minPowerRequirementForLightSpeed = GetPowerRequirementForWarp(1);
            powerRequirementForMaximumAllowedLightSpeed = GetPowerRequirementForWarp(engine_throtle[maximumWarpSpeedFactor]);
            currentPowerRequirementForWarp = GetPowerRequirementForWarp(engine_throtle[selected_factor]);

            // calculate Exotic Matter Capacity
            if (exoticMatterResource == null || double.IsNaN(exotic_power_required) || double.IsInfinity(exotic_power_required) || !(exotic_power_required > 0)) return;

            var ratio = Math.Min(1, Math.Max(0, exoticMatterResource.amount / exoticMatterResource.maxAmount));
            exoticMatterResource.maxAmount = exotic_power_required;
            exoticMatterResource.amount = exoticMatterResource.maxAmount * ratio;
        }

        public override void OnFixedUpdate()
        {
            counterCurrent++;

            if (initiateWarpTimeout > 0)
                InitiateWarp();

            Orbit currentOrbit = vessel.orbitDriver.orbit;
            var universalTime = Planetarium.GetUniversalTime();

            if (IsEnabled)
            {
                var reverseHeadingWarp = new Vector3d(-heading_act.x, -heading_act.y, -heading_act.z);
                var currentOrbitalVelocity = vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(universalTime);
                var newDirection = currentOrbitalVelocity + reverseHeadingWarp;

                long multiplier = 0;
                do
                {
                    // first predict dropping out of warp
                    predictedExitOrbit = new Orbit(currentOrbit);
                    predictedExitOrbit.UpdateFromStateVectors(currentOrbit.pos, newDirection, vessel.orbit.referenceBody, universalTime);

                    // then calculated predicted gravity breaking
                    if (warpInitialMainBody != null && vessel.mainBody != warpInitialMainBody)
                    {
                        Vector3d retrogradeNormalizedVelocity = newDirection.normalized * -multiplier;
                        Vector3d velocityToCancel = predictedExitOrbit.getOrbitalVelocityAtUT(universalTime) * gravityDragRatio;

                        predictedExitOrbit.UpdateFromStateVectors(currentOrbit.pos, retrogradeNormalizedVelocity - velocityToCancel, currentOrbit.referenceBody, universalTime);
                    }

                    multiplier += 1;
                } while (double.IsNaN(predictedExitOrbit.getOrbitalVelocityAtUT(universalTime).magnitude));

                // update expected exit orbit data
                exitPeriapsis = predictedExitOrbit.PeA / 1000;
                exitApoapsis = predictedExitOrbit.ApA / 1000;
                exitSpeed = predictedExitOrbit.getOrbitalVelocityAtUT(universalTime).magnitude;
                exitEccentricity = predictedExitOrbit.eccentricity * 180 / Math.PI;
                exitMeanAnomaly = predictedExitOrbit.meanAnomaly;
                exitBurnCircularize = DeltaVToCircularize(predictedExitOrbit);
            }
            else
            {
                exitPeriapsis = currentOrbit.PeA / 1000;
                exitApoapsis = currentOrbit.ApA / 1000;
                exitSpeed = currentOrbit.getOrbitalVelocityAtUT(universalTime).magnitude;
                exitEccentricity = currentOrbit.eccentricity;
                exitMeanAnomaly = currentOrbit.meanAnomaly * 180 / Math.PI;
                exitBurnCircularize = DeltaVToCircularize(currentOrbit);
            }

            Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
            Vector3 end_beam_pos = ship_pos + part.transform.up * warp_size;
            Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f;

            warp_effect.transform.rotation = part.transform.rotation;
            warp_effect.transform.localScale = new Vector3(effectSize1, mid_pos.magnitude, effectSize1);
            warp_effect.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
            warp_effect.transform.rotation = part.transform.rotation;

            warp_effect2.transform.rotation = part.transform.rotation;
            warp_effect2.transform.localScale = new Vector3(effectSize2, mid_pos.magnitude, effectSize2);
            warp_effect2.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
            warp_effect2.transform.rotation = part.transform.rotation;

            warp_effect1_renderer.material.mainTexture = warp_textures[((int)tex_count) % warp_textures.Length];
            warp_effect2_renderer.material.mainTexture = warp_textures2[((int)tex_count + 8) % warp_textures.Length];

            warpEngineThrottle = engine_throtle[selected_factor];

            tex_count += warpEngineThrottle;

            WarpdriveCharging();

            UpdateWarpSpeed();
        }

        private static double DeltaVToCircularize(Orbit orbit)
        {
            var rAp = orbit.ApR;
            var rPe = orbit.PeR;
            var mu = orbit.referenceBody.gravParameter;

            //∆v_circ_burn = sqrt(mu / r_ap) - sqrt((r_pe * mu) / (r_ap * (r_pe + r_ap) / 2) ) 
            return Math.Abs(Sqrt(mu / rAp) - Sqrt((rPe * mu) / (rAp * (rPe + rAp) / 2)));
        }

        private static double Sqrt(double value)
        {
            if (value < 0)
                return -Math.Sqrt(-1 * value);
            else
                return Math.Sqrt(value);
        }

        //Computes the deltaV of the burn needed to circularize an orbit at a given UT.
        public static double DeltaVToCircularize(Orbit o, double UT)
        {
            var desiredVelocity = CircularOrbitSpeed(o.referenceBody, o.radius) ;
            var actualVelocity = o.getOrbitalVelocityAtUT(UT).magnitude;
            return desiredVelocity - actualVelocity;
        }

        //Computes the speed of a circular orbit of a given radius for a given body.
        private static double CircularOrbitSpeed(CelestialBody body, double radius)
        {
            //v = sqrt(GM/r)
            return Math.Sqrt(body.gravParameter / radius);
        }

        private void WarpdriveCharging()
        {
            double currentExoticMatter;
            double maxExoticMatter;

            part.GetConnectedResourceTotals(exoticResourceDefinition.id, out currentExoticMatter, out maxExoticMatter);

            if (IsCharging)
            {
                var maxPowerRequired = (maxExoticMatter - currentExoticMatter) / 0.001;

                var powerDraw = CheatOptions.InfiniteElectricity
                    ? maxPowerRequired
                    : Math.Max(minPowerRequirementForLightSpeed, getStableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES));

                var resourceBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);

                var effectiveResourceThrotling = resourceBarRatio > 1d/3d ? 1 : resourceBarRatio * 3;

                var powerReturned = CheatOptions.InfiniteElectricity 
                    ? powerDraw
                    : consumeFNResourcePerSecond(effectiveResourceThrotling * powerDraw, ResourceManager.FNRESOURCE_MEGAJOULES);

                if (powerReturned < 0.99 * minPowerRequirementForLightSpeed)
                    insufficientPowerTimeout--;
                else
                    insufficientPowerTimeout = maxPowerTimeout;

                if (insufficientPowerTimeout < 0)
                {
                    insufficientPowerTimeout--;
                    Debug.Log("[KSPI] - Not enough power to initiate stable warp field!");
                    ScreenMessages.PostScreenMessage("Not enough power to initiate stable warp field!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    StopCharging();

                    return;
                }

                if (currentExoticMatter < exotic_power_required)
                {
                    part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, -powerReturned * 0.001 * TimeWarp.fixedDeltaTime);
                }

                ProduceWasteheat(powerReturned);
            }

            if (!IsEnabled)
            {
                if (currentExoticMatter < exotic_power_required)
                {
                    var electricalCurrentPct = 100 * currentExoticMatter / exotic_power_required;
                    driveStatus = String.Format("Charging: ") + electricalCurrentPct.ToString("0.00") + String.Format("%");
                }
                else
                {
                    driveStatus = "Ready.";
                }

                warp_effect2_renderer.enabled = false;
                warp_effect1_renderer.enabled = false;
            }
            else
            {
                driveStatus = "Active.";

                warp_effect2_renderer.enabled = true;
                warp_effect1_renderer.enabled = true;
            }
        }

        private void ProduceWasteheat(double powerReturned)
        {
            if (!CheatOptions.IgnoreMaxTemperature)
                supplyFNResourcePerSecond(powerReturned * 
                    (isupgraded 
                        ? wasteheatRatioUpgraded 
                        : wasteheatRatio), ResourceManager.FNRESOURCE_WASTEHEAT);
        }

        private void UpdateWateheatBuffer(double maxWasteheatRatio = 1)
        {
            if (wasteheatPowerResource != null && Math.Abs(TimeWarp.fixedDeltaTime - previousDeltaTime) > float.Epsilon)
            {
                var adjustedWasteheatRatio = Math.Min(wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount, maxWasteheatRatio);
                wasteheatPowerResource.maxAmount = part.mass * TimeWarp.fixedDeltaTime * 2.0e+5 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * adjustedWasteheatRatio;
            }

            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        private double GetPowerRequirementForWarp(double lightspeedFraction)
        {
            var sqrtSpeed = Math.Sqrt(lightspeedFraction);

            var powerModifier = lightspeedFraction < 1 
                ? 1 / sqrtSpeed 
                : sqrtSpeed;

            return powerModifier * exotic_power_required;
        }

        private void UpdateWarpSpeed()
        {
            currentRateIndex = TimeWarp.CurrentRateIndex;

            if (!IsEnabled || exotic_power_required <= 0) return;

            var newLightSpeed = engine_throtle[selected_factor];

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(newLightSpeed);

            var availablePower = CheatOptions.InfiniteElectricity 
                ? currentPowerRequirementForWarp
                : getStableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES);

            double powerReturned;

            if (CheatOptions.InfiniteElectricity)
                powerReturned = currentPowerRequirementForWarp;
            else
            {
                powerReturned = consumeFNResourcePerSecond(currentPowerRequirementForWarp, ResourceManager.FNRESOURCE_MEGAJOULES);
                ProduceWasteheat(powerReturned);
            }

            // detect power shortage
            if (currentPowerRequirementForWarp > availablePower)
                insufficientPowerTimeout = -1;
            else if (powerReturned < 0.99 * currentPowerRequirementForWarp)
                insufficientPowerTimeout--;
            else
                insufficientPowerTimeout = 10;

            var minimumAltitudeDistance = speedOfLight * TimeWarp.fixedDeltaTime * newLightSpeed;
            if (vessel.altitude < (vessel.mainBody.atmosphere ? vessel.mainBody.atmosphereDepth + minimumAltitudeDistance : minimumAltitudeDistance))
            {
                if (vesselWasInOuterspace)
                {
                    var message = vessel.mainBody.atmosphere 
                        ? "Droped out of warp because too close to atmosphere" 
                        : "Droped out of warp because too close to surface";

                    Debug.Log("[KSPI] - " + message);
                    ScreenMessages.PostScreenMessage(message, 5);
                    DeactivateWarpDrive();
                    return;
                }
            }
            else
                vesselWasInOuterspace = true;

            // retreive vessel heading
            var newPartHeading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);

            // detect any changes in vessel heading and heading stability
            magnitudeDiff = (active_part_heading - newPartHeading).magnitude;

            // determine if we need to change speed and heading
            var hasPowerShortage = insufficientPowerTimeout < 0;
            var hasHeadingChanged = magnitudeDiff > 0.001 && counterCurrent > counterPreviousChange + headingChangedTimeout;
            var hasWarpFactorChange = Math.Abs(existing_warp_speed - newLightSpeed) > float.Epsilon;
            var hasGavityPullInbalance = maximumWarpSpeedFactor < selected_factor;

            if (hasGavityPullInbalance)
                selected_factor = maximumWarpSpeedFactor;

            if (!CheatOptions.InfiniteElectricity && hasPowerShortage)
            {
                if (selected_factor == minimumPowerAllowedFactor || selected_factor == minimum_selected_factor ||
                    (newLightSpeed < 1 && warpEngineThrottle >= maximumAllowedWarpThrotle && powerReturned < 0.99 * currentPowerRequirementForWarp))
                {
                    string message;
                    if (powerReturned < 0.99 * currentPowerRequirementForWarp)
                        message = "Critical Power supply at " + (powerReturned / currentPowerRequirementForWarp * 100).ToString("0.0" ) + "% , deactivating warp";
                    else
                        message = "Critical Power shortage while at minimum speed, deactivating warp";

                    Debug.Log("[KSPI] - " + message);
                    ScreenMessages.PostScreenMessage(message, 5);
                    DeactivateWarpDrive();
                    return;
                }
                var insufficientMessage = "Insufficient Power at " + (powerReturned / currentPowerRequirementForWarp * 100).ToString("0.0") + ", reducing power drain";
                Debug.Log("[KSPI] - " + insufficientMessage);
                ScreenMessages.PostScreenMessage(insufficientMessage, 5);
                ReduceWarpPower();
            }

            if (!hasWarpFactorChange && !hasPowerShortage && !hasHeadingChanged && !hasGavityPullInbalance) return;

            if (hasHeadingChanged)
                counterPreviousChange = counterCurrent;

            newLightSpeed = engine_throtle[selected_factor];
            existing_warp_speed = newLightSpeed;

            var reverseHeading = new Vector3d(-heading_act.x, -heading_act.y, -heading_act.z);

            heading_act = newPartHeading * speedOfLight * newLightSpeed;
            serialisedwarpvector = ConfigNode.WriteVector(heading_act);

            active_part_heading = newPartHeading;

            var previousRotation = vessel.transform.rotation;

            // prevent g-force effects for next frame
            part.vessel.IgnoreGForces(1);

            if (!this.vessel.packed)
                vessel.GoOnRails();

            var newVelocity = vessel.orbit.vel + reverseHeading + heading_act;

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, newVelocity, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            if (!this.vessel.packed)
                vessel.GoOffRails();

            // only rotate durring normal time
            if (!this.vessel.packed)
                vessel.SetRotation(previousRotation);
        }

        private static AnimationState[] SetUpAnimation(string animationName, Part part)
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }

        private void Develocitize()
        {
            // This code is inspired quite heavily by HyperEdit's OrbitEditor.cs
            var universalTime = Planetarium.GetUniversalTime();
            Orbit currentOrbit = vessel.orbitDriver.orbit;
            Vector3d currentOrbitalVelocity = currentOrbit.getOrbitalVelocityAtUT(universalTime);
            Vector3d progradeNormalizedVelocity = currentOrbitalVelocity.normalized;
            Vector3d velocityToCancel = currentOrbitalVelocity;

            // apply gravity drag modifier
            velocityToCancel *= gravityDragRatio; 

            // Extremely small velocities cause the game to mess up very badly, so try something small and increase...
            long multiplier = 0;
            Orbit newOribit;
            do
            {
                Vector3d retrogradeNormalizedVelocity = progradeNormalizedVelocity * -multiplier;

                newOribit = new Orbit(currentOrbit);
                newOribit.UpdateFromStateVectors(currentOrbit.pos, retrogradeNormalizedVelocity - velocityToCancel, currentOrbit.referenceBody, universalTime);

                multiplier += 1;
            } while (double.IsNaN(newOribit.getOrbitalVelocityAtUT(universalTime).magnitude));

            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.landedAt = string.Empty;

            // I'm actually not sure what this is for... but HyperEdit does it.
            // I had weird problems when I took it out, anyway.
            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
                //print("[WSXDV] NullReferenceException");
            }
            var allVessels = FlightGlobals.fetch == null 
                ? (IEnumerable<Vessel>)new[] { vessel } 
                : FlightGlobals.Vessels;

            foreach (var currentVessel in allVessels.Where(v => v.packed == false))
            {
                currentVessel.GoOnRails();
            }
            // End HyperEdit code I don't really understand

            currentOrbit.inclination = newOribit.inclination;
            currentOrbit.eccentricity = newOribit.eccentricity;
            currentOrbit.semiMajorAxis = newOribit.semiMajorAxis;
            currentOrbit.LAN = newOribit.LAN;
            currentOrbit.argumentOfPeriapsis = newOribit.argumentOfPeriapsis;
            currentOrbit.meanAnomalyAtEpoch = newOribit.meanAnomalyAtEpoch;
            currentOrbit.epoch = newOribit.epoch;
            currentOrbit.Init();
            currentOrbit.UpdateFromUT(universalTime);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        public void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, "Warp Control Interface");
        }

        protected void PrintToGUILayout(string label, string value, GUIStyle boldStyle, GUIStyle textStyle, int witdhLabel = 130, int witdhValue = 130)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, boldStyle, GUILayout.Width(witdhLabel));
            GUILayout.Label(value, textStyle, GUILayout.Width(witdhValue));
            GUILayout.EndHorizontal();
        }


        private void Window(int windowID)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                InitializeStyles();

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    render_window = false;

                GUILayout.BeginVertical();

                PrintToGUILayout("Type", part.partInfo.title, bold_black_style, text_black_style);
                PrintToGUILayout("Warp Power", sumOfAlcubierreDrives.ToString("0.0") + " t", bold_black_style, text_black_style);
                PrintToGUILayout("Vessel Mass", vesselTotalMass.ToString("0.000") + " t", bold_black_style, text_black_style);
                PrintToGUILayout("Warp To Mass Ratio", "1:" + warpToMassRatio.ToString("0.000"), bold_black_style, text_black_style);
                PrintToGUILayout("Gravity At Surface", gravityAtSeaLevel.ToString("0.00000") + " m/s\xB2", bold_black_style, text_black_style);
                PrintToGUILayout("Gravity Vessel Pull", gravityPull.ToString("0.00000") + " m/s\xB2", bold_black_style, text_black_style);
                PrintToGUILayout("Gravity Drag", gravityDragPercentage.ToString("0.000") + "%", bold_black_style, text_black_style);
                //PrintToGUILayout("Maximum Warp Limit", maximumWarpForGravityPull.ToString("0.0000") + " c", bold_black_style, text_black_style);
                PrintToGUILayout("Max Allowed Throtle", maximumAllowedWarpThrotle.ToString("0.0000") + " c", bold_black_style, text_black_style);
                PrintToGUILayout("Current Selected Speed", warpEngineThrottle.ToString("0.0000") + " c", bold_black_style, text_black_style);
                PrintToGUILayout("Cur Power for Warp", currentPowerRequirementForWarp.ToString("0.000") + " MW", bold_black_style, text_black_style);

                PrintToGUILayout("Exit Speed", exitSpeed.ToString("0.000") + " m/s", bold_black_style, text_black_style);
                PrintToGUILayout("Exit Apoapsis", exitApoapsis.ToString("0.000") + " km", bold_black_style, text_black_style);
                PrintToGUILayout("Exit Periapsis", exitPeriapsis.ToString("0.000") + " km", bold_black_style, text_black_style);
                PrintToGUILayout("Exit Eccentricity", exitEccentricity.ToString("0.000"), bold_black_style, text_black_style);
                PrintToGUILayout("Exit Mean Anomaly", exitMeanAnomaly.ToString("0.000") + "\xB0", bold_black_style, text_black_style);
                PrintToGUILayout("Exit Burn to Circularize", exitBurnCircularize.ToString("0.000") + " m/s", bold_black_style, text_black_style);

                PrintToGUILayout("Status", driveStatus, bold_black_style, text_black_style);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("(-) Speed", GUILayout.MinWidth(130)))
                    ToggleWarpSpeedDown();
                if (GUILayout.Button("(+) Speed", GUILayout.MinWidth(130)))
                    ToggleWarpSpeedUp();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("(-) Speed x3", GUILayout.MinWidth(130)))
                    ToggleWarpSpeedDown3();
                if (GUILayout.Button("(+) Speed x3", GUILayout.MinWidth(130)))
                    ToggleWarpSpeedUp3();
                GUILayout.EndHorizontal(); 

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("(-) Speed x10", GUILayout.MinWidth(130)))
                    ToggleWarpSpeedDown10();
                if (GUILayout.Button("(+) Speed x10", GUILayout.MinWidth(130)))
                    ToggleWarpSpeedUp10();
                GUILayout.EndHorizontal();                

                if (!IsEnabled && !IsCharging &&  GUILayout.Button("Start Charging", GUILayout.ExpandWidth(true)))
                    StartCharging();

                if (!IsEnabled && IsCharging && GUILayout.Button("Stop Charging", GUILayout.ExpandWidth(true)))
                    StopCharging();
                if (!IsEnabled && GUILayout.Button("Activate Warp", GUILayout.ExpandWidth(true)))
                    ActivateWarpDrive();
                if (IsEnabled && GUILayout.Button("Deactivate Warp", GUILayout.ExpandWidth(true)))
                    DeactivateWarpDrive();

                GUILayout.EndVertical();
                GUI.DragWindow();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - AlcubierreDrive Window(" + windowID + "): " + e.Message);
            }
        }

        private void InitializeStyles()
        {
            if (bold_black_style == null)
            {
                bold_black_style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    font = PluginHelper.MainFont
                };
            }

            if (text_black_style == null)
            {
                text_black_style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    font = PluginHelper.MainFont
                };
            }
        }

    }
}
