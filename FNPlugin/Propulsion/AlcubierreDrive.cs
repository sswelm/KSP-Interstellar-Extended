using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class AlcubierreDrive : FNResourceSuppliableModule
    {
        // persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false)]
        public bool IsCharging = false;
        [KSPField(isPersistant = true)]
        private double existing_warpfactor;
        [KSPField(isPersistant = true)]
        public bool warpInit = false;
        [KSPField(isPersistant = true)]
        public int selected_factor = -1;

        // non persistant
        [KSPField(isPersistant = false)]
        public int InstanceID;
        [KSPField(isPersistant = false)]
        public bool IsSlave;
        [KSPField(isPersistant = false)]
        public string AnimationName = String.Empty;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float effectSize1;
        [KSPField(isPersistant = false)]
        public float effectSize2;
        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
        [KSPField(isPersistant = false)]
        public double powerRequirementMultiplier = 1;

        [KSPField(isPersistant = false)]
        public double wasteheatRatio = 0.5;
        [KSPField(isPersistant = false)]
        public double wasteheatRatioUpgraded = 0.25;
        [KSPField(isPersistant = false)]
        public double wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
        public string warpdriveType = "Alcubierre Drive";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Gravity At Sea Level", guiUnits = "g", guiFormat = "F4")]
        public double gravityAtSeaLevel;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Gravity Pull", guiUnits = "g", guiFormat = "F4")]
        public double gravityPull;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Gravity Drag", guiUnits = "%", guiFormat = "F4")]
        public double gravityDragPercentage;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Maximum Warp Limit", guiUnits = "c", guiFormat = "F4")]
        public double maximumWarpForGravityPull;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Mass", guiUnits = "t")]
        public float partMass;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Total Warp Power", guiFormat = "F4", guiUnits = "t")]
        protected double sumOfAlcubierreDrives;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Vessel Total Mass", guiFormat = "F4", guiUnits = "t")]
        public float vesselTotalMass;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Warp to Mass Ratio", guiFormat = "F4")]
        public double warpToMassRatio;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Magnitude Diff")]
        public double magnitudeDiff;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Req Exotic Matter", guiUnits = " MW", guiFormat = "F2")]
        protected double exotic_power_required = 1000;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Abs Min Power Warp", guiUnits = "MW", guiFormat = "F4")]
        public double minPowerRequirementForLightSpeed;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Cur Power for Warp ", guiUnits = "MW", guiFormat = "F4")]
        public double currentPowerRequirementForWarp;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Power Max Speed", guiUnits = "MW", guiFormat = "F4")]
        public double PowerRequirementForMaximumAllowedLightSpeed;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Selected Throttle", guiUnits = "c", guiFormat = "F4")]
        public double WarpEngineThrottle;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Max Allowed Throtle", guiUnits = "c", guiFormat = "F4")]
        public double maximumAllowedWarpThrotle;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string DriveStatus;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public string serialisedwarpvector;
        [KSPField(isPersistant = true)]
        public bool isDeactivatingWarpDrive = false;

        private double[] engine_throtle = { 0.001, 0.0016, 0.0025, 0.004, 0.0063, 0.01, 0.016, 0.025, 0.04, 0.063, 0.1, 0.16, 0.25, 0.40, 0.63, 1.0, 1.6, 2.5, 4.0, 6.3, 10, 16, 25, 40, 63, 100, 160, 250, 400, 630, 1000 };

        protected int old_selected_factor = 0;
        protected double tex_count;
        protected GameObject warp_effect;
        protected GameObject warp_effect2;
        protected Texture[] warp_textures;
        protected Texture[] warp_textures2;
        protected AudioSource warp_sound;
        protected const float warp_size = 50000;
        protected bool hasrequiredupgrade;
        private float previousDeltaTime;

        private AnimationState[] animationState;
        private Vector3d heading_act;
        private Vector3d active_part_heading;
        private List<AlcubierreDrive> alcubierreDrives;
        private int minimum_selected_factor;
        private int maximumWarpSpeedFactor;
        private int minimumPowerAllowedFactor;
        private int insufficientPowerTimeout = 10;
        private bool vesselWasInOuterspace;

        private long counterCurrent;
        private long counterPreviousChange;

        private PartResource wasteheatPowerResource;
        private PartResource exoticMatterResource;

        private Renderer warp_effect1_renderer;
        private Renderer warp_effect2_renderer;

        private Collider warp_effect1_collider;
        private Collider warp_effect2_collider;

        private PartResourceDefinition exoticResourceDefinition;

        private CelestialBody warpInitialMainBody;


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

            insufficientPowerTimeout = 10;
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

        [KSPEvent(guiActive = true, guiName = "Activate Warp Drive", active = true)]
        public void ActivateWarpDrive()
        {
            Debug.Log("[KSPI] - Activate Warp Drive button pressed");
            if (IsEnabled) return;

            isDeactivatingWarpDrive = false;

            if (warpToMassRatio < 1)
            {
                var message = "Not enough warp power to warp vessel";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            Vessel vess = this.part.vessel;
            if (vess.altitude <= PluginHelper.getMaxAtmosphericAltitude(vess.mainBody) && vess.mainBody.flightGlobalsIndex != 0)
            {
                var message = "Cannot activate warp drive within the atmosphere!";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            double exotic_matter_available;
            double total_exotic_matter_available;
            part.GetConnectedResourceTotals(exoticResourceDefinition.id, out exotic_matter_available, out total_exotic_matter_available);

            if (exotic_matter_available < exotic_power_required)
            {
                var message = "Warp drive isn't fully charged yet for Warp!";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            if (maximumWarpSpeedFactor < selected_factor)
                selected_factor = minimumPowerAllowedFactor;

            double new_warpfactor = engine_throtle[selected_factor];

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warpfactor);

            if (!CheatOptions.InfiniteElectricity && currentPowerRequirementForWarp > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES))
            {
                var message = "Warp power requirement is higher that maximum power supply!";
                Debug.Log("[KSPI] - " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            IsCharging = false;
            initiateWarpTimeout = 10;
        }

        private int initiateWarpTimeout;

        private int GetMaximumFactor(double lightspeed)
        {
            int maxFactor = 0;

            for (int i = 0; i < engine_throtle.Count(); i++)
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

            double new_warp_factor = engine_throtle[selected_factor];

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warp_factor);

            double power_returned = CheatOptions.InfiniteElectricity 
                ? currentPowerRequirementForWarp 
                : consumeFNResource(currentPowerRequirementForWarp * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;

            if (power_returned < 0.99 * currentPowerRequirementForWarp)
            {
                initiateWarpTimeout--;

                if (initiateWarpTimeout == 1)
                {
                    while (selected_factor != minimum_selected_factor)
                    {
                        Debug.Log("[KSPI] - call ReduceWarpPower");
                        ReduceWarpPower();
                        new_warp_factor = engine_throtle[selected_factor];
                        currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warp_factor);
                        if (power_returned >= currentPowerRequirementForWarp)
                            return;
                    }
                }
                if (initiateWarpTimeout == 0)
                {
                    var message = "Not enough power to initiate warp!" + power_returned + " " + currentPowerRequirementForWarp;
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

            heading_act = active_part_heading * GameConstants.warpspeed * new_warp_factor;
            serialisedwarpvector = ConfigNode.WriteVector(heading_act);

            Debug.Log("[KSPI] - GoOnRails");
            vessel.GoOnRails();

            var newHeading = vessel.orbit.vel + heading_act;
            Debug.Log("[KSPI] - UpdateFromStateVectors position x:" + vessel.orbit.pos.x + ",y:" + vessel.orbit.pos.y + ",z" + vessel.orbit.pos.z);
            Debug.Log("[KSPI] - UpdateFromStateVectors velocity x:" + newHeading.x + ",y:" + newHeading.y + ",z" + newHeading.z);

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + heading_act, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            Debug.Log("[KSPI] - GoOffRails");
            vessel.GoOffRails();
            
            IsEnabled = true;

            existing_warpfactor = new_warp_factor;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Warp Drive", active = false)]
        public void DeactivateWarpDrive()
        {
            Debug.Log("[KSPI] - Deactivate Warp Drive event called");
            if (!IsEnabled)
                return;

            // prevent g-force effects
            part.vessel.IgnoreGForces(1);

            IsEnabled = false;
            warp_sound.Stop();

            Vector3d heading = heading_act;
            heading.x = -heading.x;
            heading.y = -heading.y;
            heading.z = -heading.z;

            Debug.Log("[KSPI] - GoOnRails");
            vessel.GoOnRails();
            
            var newHeading = vessel.orbit.vel + heading;
            Debug.Log("[KSPI] - UpdateFromStateVectors position x:" + vessel.orbit.pos.x + ",y:" + vessel.orbit.pos.y + ",z" + vessel.orbit.pos.z);
            Debug.Log("[KSPI] - UpdateFromStateVectors velocity x:" + newHeading.x + ",y:" + newHeading.y + ",z" + newHeading.z);
            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, newHeading, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
            
            Debug.Log("[KSPI] - GoOffRails");
            vessel.GoOffRails();

            if (warpInitialMainBody != null && vessel.mainBody != warpInitialMainBody)
            {
                Develocitize();
            }
        }

        [KSPEvent(guiActive = true, guiName = "Warp Throttle (+)", active = true)]
        public void ToggleWarpSpeedUp()
        {
            Debug.Log("[KSPI] - Warp Throttle (+) button pressed");
            selected_factor++;
            if (selected_factor >= engine_throtle.Length)
                selected_factor = engine_throtle.Length - 1;

            if (!IsEnabled)
                old_selected_factor = selected_factor;
        }

        [KSPEvent(guiActive = true, guiName = "Warp Throttle (-)", active = true)]
        public void ToggleWarpSpeedDown()
        {
            Debug.Log("[KSPI] - Warp Throttle (-) button pressed");
            selected_factor--;
            if (selected_factor < 0)
                selected_factor = 0;

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

        [KSPAction("Reduce Warp Drive")]
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

        [KSPAction("Warp Speed (+)")]
        public void ToggleWarpSpeedUpAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Warp Speed (+) action activated");
            ToggleWarpSpeedUp();
        }

        [KSPAction("Warp Speed (-)")]
        public void ToggleWarpSpeedDownAction(KSPActionParam param)
        {
            Debug.Log("[KSPI] - Warp Speed (-) action activated");
            ToggleWarpSpeedDown();
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
            exoticResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.ExoticMatter);

            wasteheatPowerResource = part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT];
            exoticMatterResource = part.Resources[InterstellarResourcesConfiguration.Instance.ExoticMatter];
            
            // reset Exotic Matter Capacity
            if (exoticMatterResource != null)
            {
                part.mass = partMass;
                var ratio = Math.Min(1, Math.Max(0, exoticMatterResource.amount / exoticMatterResource.maxAmount));
                exoticMatterResource.maxAmount = 0.001;
                exoticMatterResource.amount = exoticMatterResource.maxAmount * ratio;
            }

            InstanceID = GetInstanceID();

            if (IsSlave)
                UnityEngine.Debug.Log("KSPI - AlcubierreDrive Slave " + InstanceID + " Started");
            else
                UnityEngine.Debug.Log("KSPI - AlcubierreDrive Master " + InstanceID + " Started");

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

                Fields["exotic_power_required"].guiActive = !IsSlave;
                Fields["WarpEngineThrottle"].guiActive = !IsSlave;
                Fields["maximumAllowedWarpThrotle"].guiActive = !IsSlave;
                Fields["warpToMassRatio"].guiActive = !IsSlave;
                Fields["vesselTotalMass"].guiActive = !IsSlave;
                Fields["DriveStatus"].guiActive = !IsSlave;
                Fields["minPowerRequirementForLightSpeed"].guiActive = !IsSlave;
                Fields["currentPowerRequirementForWarp"].guiActive = !IsSlave;
                Fields["sumOfAlcubierreDrives"].guiActive = !IsSlave;
                Fields["PowerRequirementForMaximumAllowedLightSpeed"].guiActive = !IsSlave;

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

                if (state == StartState.Editor) return;

                UpdateWateheatBuffer(0.95);

                if (!IsSlave)
                {
                    UnityEngine.Debug.Log("KSPI - AlcubierreDrive Create Slaves");
                    alcubierreDrives = part.vessel.FindPartModulesImplementing<AlcubierreDrive>();
                    foreach (var drive in alcubierreDrives)
                    {
                        var driveId = drive.GetInstanceID();
                        if (driveId != InstanceID)
                        {
                            drive.IsSlave = true;
                            UnityEngine.Debug.Log("KSPI - AlcubierreDrive " + driveId + " != " + InstanceID);
                        }
                    }
                }

                UnityEngine.Debug.Log("KSPI - AlcubierreDrive OnStart step C ");

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

                Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
                Vector3 end_beam_pos = ship_pos + transform.up * warp_size;
                Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f;

                warp_effect.transform.localScale = new Vector3(effectSize1, mid_pos.magnitude, effectSize1);
                warp_effect.transform.position = new Vector3(mid_pos.x, ship_pos.y + mid_pos.y, mid_pos.z);
                warp_effect.transform.rotation = part.transform.rotation;

                warp_effect2.transform.localScale = new Vector3(effectSize2, mid_pos.magnitude, effectSize2);
                warp_effect2.transform.position = new Vector3(mid_pos.x, ship_pos.y + mid_pos.y, mid_pos.z);
                warp_effect2.transform.rotation = part.transform.rotation;

                //warp_effect.layer = LayerMask.NameToLayer("Ignore Raycast");
                //warp_effect.renderer.material = new Material(KSP.IO.File.ReadAllText<AlcubierreDrive>("AlphaSelfIllum.shader"));

                warp_effect1_renderer.material.shader = Shader.Find("Unlit/Transparent");
                warp_effect2_renderer.material.shader = Shader.Find("Unlit/Transparent");

                warp_textures = new Texture[33];

                const string warp_tecture_path = "WarpPlugin/ParticleFX/warp";
                for (int i = 0; i < 11; i++)
                {
                    warp_textures[i] = GameDatabase.Instance.GetTexture((i > 0)
                        ? warp_tecture_path + (i + 1).ToString()
                        : warp_tecture_path, false);
                }

                warp_textures[11] = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warp10", false);
                for (int i = 12; i < 33; i++)
                {
                    int j = i > 17 ? 34 - i : i;
                    warp_textures[i] = GameDatabase.Instance.GetTexture(j > 1 ?
                        warp_tecture_path + (j + 1).ToString() : warp_tecture_path, false);
                }

                warp_textures2 = new Texture[33];

                const string warpr_tecture_path = "WarpPlugin/ParticleFX/warpr";
                for (int i = 0; i < 11; i++)
                {
                    warp_textures2[i] = GameDatabase.Instance.GetTexture((i > 0)
                        ? warpr_tecture_path + (i + 1).ToString()
                        : warpr_tecture_path, false);
                }

                warp_textures2[11] = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warpr10", false);
                for (int i = 12; i < 33; i++)
                {
                    int j = i > 17 ? 34 - i : i;
                    warp_textures2[i] = GameDatabase.Instance.GetTexture(j > 1 ?
                        warpr_tecture_path + (j + 1).ToString() : warpr_tecture_path, false);
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

                //light.

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

                bool manual_upgrade = false;
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    if (upgradeTechReq != null)
                    {
                        if (PluginHelper.hasTech(upgradeTechReq))
                            hasrequiredupgrade = true;
                        else if (upgradeTechReq == "none")
                            manual_upgrade = true;
                    }
                    else
                        manual_upgrade = true;
                }
                else
                    hasrequiredupgrade = true;

                if (warpInit == false)
                {
                    warpInit = true;
                    if (hasrequiredupgrade)
                        isupgraded = true;
                }

                if (manual_upgrade)
                    hasrequiredupgrade = true;


                if (isupgraded)
                    warpdriveType = upgradedName;
                else
                    warpdriveType = originalName;

                //warp_effect.transform.localScale.y = 2.5f;
                //warp_effect.transform.localScale.z = 200f;

                // disable charging at startup
                //IsCharging = false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - AlcubierreDrive OnStart 1 Exception " + e.Message);
            }

            warpdriveType = originalName;

        }


        public override void OnUpdate()
        {
            Events["StartCharging"].active = !IsSlave && !IsCharging;
            Events["StopCharging"].active = !IsSlave && IsCharging;
            Events["ActivateWarpDrive"].active = !IsSlave && !IsEnabled;
            Events["DeactivateWarpDrive"].active = !IsSlave && IsEnabled;

            if (ResearchAndDevelopment.Instance != null)
                Events["RetrofitDrive"].active = !IsSlave && !isupgraded && ResearchAndDevelopment.Instance.Science >= UpgradeCost() && hasrequiredupgrade;
            else
                Events["RetrofitDrive"].active = false;

            if (animationState != null)
            {
                foreach (AnimationState anim in animationState)
                {
                    if ((IsEnabled || IsCharging) && anim.normalizedTime < 1) { anim.speed = 1; }
                    if ((IsEnabled || IsCharging) && anim.normalizedTime >= 1)
                    {
                        anim.speed = 0;
                        anim.normalizedTime = 1;
                    }
                    if (!IsEnabled && !IsCharging && anim.normalizedTime > 0) { anim.speed = -1; }
                    if (!IsEnabled && !IsCharging && anim.normalizedTime <= 0)
                    {
                        anim.speed = 0;
                        anim.normalizedTime = 0;
                    }
                }
            }
        }

        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            UpdateWateheatBuffer();

            WarpEngineThrottle = engine_throtle[selected_factor];

            if (alcubierreDrives != null)
                sumOfAlcubierreDrives = alcubierreDrives.Sum(p => p.partMass * (p.isupgraded ? 20 : 10));

            if (vessel != null)
            {
                vesselTotalMass = vessel.GetTotalMass();
                gravityPull = FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude;
                gravityAtSeaLevel = vessel.mainBody.GeeASL * GameConstants.STANDARD_GRAVITY;
                gravityDragPercentage = (1 - Math.Pow((1 - gravityPull / gravityAtSeaLevel), 2)) * 100;

                maximumWarpForGravityPull = vessel.mainBody.flightGlobalsIndex != 0
                    ? 1 / (Math.Max(gravityPull - 0.006, 0.001) * 10)
                    : 1 / gravityPull;
                maximumWarpSpeedFactor = GetMaximumFactor(maximumWarpForGravityPull);
                maximumAllowedWarpThrotle = engine_throtle[maximumWarpSpeedFactor];
                minimumPowerAllowedFactor = maximumWarpSpeedFactor > minimum_selected_factor ? maximumWarpSpeedFactor : minimum_selected_factor;
            }

            if (sumOfAlcubierreDrives != 0 && vesselTotalMass != 0)
            {
                warpToMassRatio = sumOfAlcubierreDrives / vesselTotalMass;
                exotic_power_required = (GameConstants.initial_alcubierre_megajoules_required * vesselTotalMass * powerRequirementMultiplier) / warpToMassRatio;
            }

            minPowerRequirementForLightSpeed = GetPowerRequirementForWarp(1);
            PowerRequirementForMaximumAllowedLightSpeed = GetPowerRequirementForWarp(engine_throtle[maximumWarpSpeedFactor]);
            currentPowerRequirementForWarp = GetPowerRequirementForWarp(engine_throtle[selected_factor]);

            var exoticMatterResource = part.Resources.FirstOrDefault(r => r.resourceName == InterstellarResourcesConfiguration.Instance.ExoticMatter);
            // calculate Exotic Matter Capacity
            if (exoticMatterResource != null && !double.IsNaN(exotic_power_required) && !double.IsInfinity(exotic_power_required) && exotic_power_required > 0)
            {
                var ratio = Math.Min(1, Math.Max(0, exoticMatterResource.amount / exoticMatterResource.maxAmount));
                exoticMatterResource.maxAmount = exotic_power_required;
                exoticMatterResource.amount = exoticMatterResource.maxAmount * ratio;
            }

        }

        public override void OnFixedUpdate()
        {
            counterCurrent++;

            if (initiateWarpTimeout > 0)
                InitiateWarp();

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

            WarpEngineThrottle = engine_throtle[selected_factor];

            tex_count += WarpEngineThrottle;

            WarpdriveCharging();

            UpdateWarpSpeed();
        }

        private void UpdateWarpDriveStatus(float currentExoticMatter, double lostWarpFieldForWarp)
        {
            double TimeLeftInSec = Math.Ceiling(currentExoticMatter / lostWarpFieldForWarp);
            DriveStatus = "Warp for " + (int)(TimeLeftInSec / 60) + " min " + (int)(TimeLeftInSec % 60) + " sec";
        }

        private void WarpdriveCharging()
        {
            double currentExoticMatter = 0;
            double maxExoticMatter = 0;

            part.GetConnectedResourceTotals(exoticResourceDefinition.id, out currentExoticMatter, out maxExoticMatter);

            if (IsCharging)
            {
                double powerDraw = CheatOptions.InfiniteElectricity 
                    ? (maxExoticMatter - currentExoticMatter) / 0.001
                    : Math.Max(minPowerRequirementForLightSpeed, Math.Min((maxExoticMatter - currentExoticMatter) / 0.001, getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES)));

                var resourceBarRatio = getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES);

                var effectiveResourceThrotling = resourceBarRatio > 0.5 ? 1 : resourceBarRatio * 2;

                double power_returned = CheatOptions.InfiniteElectricity 
                    ? powerDraw
                    : consumeFNResourcePerSecond(effectiveResourceThrotling * powerDraw, FNResourceManager.FNRESOURCE_MEGAJOULES);

                if (power_returned < 0.99 * minPowerRequirementForLightSpeed)
                    insufficientPowerTimeout--;
                else
                    insufficientPowerTimeout = 10;

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
                    part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, -power_returned * 0.001 * TimeWarp.fixedDeltaTime);
                }

                ProduceWasteheat(power_returned);
            }

            if (!IsEnabled)
            {
                if (currentExoticMatter < exotic_power_required)
                {
                    double electrical_current_pct = 100.0 * currentExoticMatter / exotic_power_required;
                    DriveStatus = String.Format("Charging: ") + electrical_current_pct.ToString("0.00") + String.Format("%");
                }
                else
                {
                    DriveStatus = "Ready.";
                }

                warp_effect2_renderer.enabled = false;
                warp_effect1_renderer.enabled = false;
            }
            else
            {
                DriveStatus = "Active.";
;
                warp_effect2_renderer.enabled = true;
                warp_effect1_renderer.enabled = true;
            }
        }

        private void ProduceWasteheat(double power_returned)
        {
            if (!CheatOptions.IgnoreMaxTemperature)
                supplyFNResourcePerSecond(power_returned * 
                    (isupgraded 
                        ? wasteheatRatioUpgraded 
                        : wasteheatRatio), FNResourceManager.FNRESOURCE_WASTEHEAT);
        }

        private void UpdateWateheatBuffer(double maxWasteheatRatio = 1)
        {
            if (wasteheatPowerResource != null && TimeWarp.fixedDeltaTime != previousDeltaTime)
            {
                var wasteheat_ratio = Math.Min(wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount, maxWasteheatRatio);
                wasteheatPowerResource.maxAmount = part.mass * TimeWarp.fixedDeltaTime * 2.0e+5 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * wasteheat_ratio;
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

        public void UpdateWarpSpeed()
        {
            if (!IsEnabled || exotic_power_required <= 0) return;

            double new_warp_factor = engine_throtle[selected_factor];

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warp_factor);

            double available_power = CheatOptions.InfiniteElectricity 
                ? currentPowerRequirementForWarp
                : getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);

            double power_returned;

            if (CheatOptions.InfiniteElectricity)
                power_returned = currentPowerRequirementForWarp;
            else
            {
                power_returned = consumeFNResourcePerSecond(currentPowerRequirementForWarp, FNResourceManager.FNRESOURCE_MEGAJOULES);
                ProduceWasteheat(power_returned);
            }

            // retreive vessel heading
            Vector3d new_part_heading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);

            // detect any changes in vessel heading and heading stability
            magnitudeDiff = (active_part_heading - new_part_heading).magnitude;

            // detect power shortage
            if (currentPowerRequirementForWarp > available_power)
                insufficientPowerTimeout = -1;
            else if (power_returned < 0.99 * currentPowerRequirementForWarp)
                insufficientPowerTimeout--;
            else
                insufficientPowerTimeout = 10;

            if (this.vessel.altitude < this.vessel.mainBody.atmosphereDepth * (1 + 2 * Math.Pow(new_warp_factor, 0.25)))
            {
                if (vesselWasInOuterspace)
                {
                    Debug.Log("[KSPI] - Droped out of warp because too close to atmosphere");
                    DeactivateWarpDrive();
                    return;
                }
            }
            else
                vesselWasInOuterspace = true;

            // determine if we need to change speed and heading
            var hasPowerShortage = insufficientPowerTimeout < 0;
            var hasHeadingChanged = magnitudeDiff > 0.001 && counterCurrent > counterPreviousChange + 50;
            var hasWarpFactorChange = existing_warpfactor != new_warp_factor;
            var hasGavityPullInbalance = maximumWarpSpeedFactor < selected_factor;

            if (hasGavityPullInbalance)
                selected_factor = maximumWarpSpeedFactor;

            if (!CheatOptions.InfiniteElectricity && hasPowerShortage)
            {
                if (selected_factor == minimumPowerAllowedFactor || selected_factor == minimum_selected_factor || available_power < 0.63 * PowerRequirementForMaximumAllowedLightSpeed)
                {
                    string message;
                    if (available_power < 0.63 * PowerRequirementForMaximumAllowedLightSpeed)
                        message = "Critical Power supply at " + power_returned / PowerRequirementForMaximumAllowedLightSpeed * 100 + "% , deactivating warp";
                    else
                        message = "Critical Power shortage while at minimum speed, deactivating warp";

                    Debug.Log("[KSPI] - " + message);
                    ScreenMessages.PostScreenMessage(message, 5);
                    DeactivateWarpDrive();
                    return;
                }
                var insufficientMessage = "Insufficient Power " + power_returned.ToString("0.0") + " / " + currentPowerRequirementForWarp.ToString("0.0") + ", reducing power drain";
                Debug.Log("[KSPI] - " + insufficientMessage);
                ScreenMessages.PostScreenMessage(insufficientMessage, 5);
                ReduceWarpPower();
            }

            if (hasWarpFactorChange || hasPowerShortage || hasHeadingChanged || hasGavityPullInbalance)
            {
                counterPreviousChange = counterCurrent;

                new_warp_factor = engine_throtle[selected_factor];
                existing_warpfactor = new_warp_factor;

                Vector3d reverse_heading = new Vector3d(-heading_act.x, -heading_act.y, -heading_act.z);

                heading_act = new_part_heading * GameConstants.warpspeed * new_warp_factor;
                active_part_heading = new_part_heading;
                serialisedwarpvector = ConfigNode.WriteVector(heading_act);

                var previous_rotation = vessel.transform.rotation;

                // prevent g-force effects for next frame
                part.vessel.IgnoreGForces(1);

                vessel.GoOnRails();

                var new_velocity = vessel.orbit.vel + reverse_heading + heading_act;

                vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, new_velocity, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
                vessel.GoOffRails();

                if (TimeWarp.fixedDeltaTime == 0.02)
                    vessel.SetRotation(previous_rotation); 
            }
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part)
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

        public void Develocitize()
        {
            // This code is inspired quite heavily by HyperEdit's OrbitEditor.cs
            double universalTime = Planetarium.GetUniversalTime();
            Orbit currentOrbit = vessel.orbitDriver.orbit;
            Vector3d currentOrbitalVelocity = currentOrbit.getOrbitalVelocityAtUT(universalTime);
            Vector3d progradeNormalizedVelocity = currentOrbitalVelocity.normalized;

            Vector3d velocityToCancel = currentOrbitalVelocity;
            velocityToCancel *= Math.Pow((1 - gravityPull / gravityAtSeaLevel), 2); 
            Vector3d exVelocityToCancel = velocityToCancel;
            velocityToCancel += currentOrbitalVelocity;

            // Extremely small velocities cause the game to mess up very badly, so try something small and increase...
            float multiplier = 0;
            Orbit newOribit;
            do
            {
                Vector3d retrogradeNormalizedVelocity = progradeNormalizedVelocity * -multiplier;

                newOribit = new Orbit(currentOrbit);
                newOribit.UpdateFromStateVectors(currentOrbit.pos, retrogradeNormalizedVelocity - exVelocityToCancel, currentOrbit.referenceBody, universalTime);

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

    }
}
