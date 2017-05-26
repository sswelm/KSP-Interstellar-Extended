using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;
using TweakScale;

namespace FNPlugin
{
    class AntimatterStorageTank : FNResourceSuppliableModule, IPartMassModifier, IRescalable<FNGenerator>
    {
        [KSPField(isPersistant = true)]
        public double chargestatus = 1000;
        [KSPField(isPersistant = false)]
        public float massExponent = 3;
        [KSPField(isPersistant = false)]
        public float chargeNeeded = 100f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Exploding")]
        bool exploding = false;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge")]
        public string chargeStatusStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusStr;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Current")]
        public string capacityStr;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Maximum")]
        public string maxAmountStr;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "K",  guiName = "Maximum Temperature"), UI_FloatRange(stepIncrement = 10f, maxValue = 1000f, minValue = 40f)]
        public float maxTemperature = 1000;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "g", guiName = "Maximum Acceleration"), UI_FloatRange(stepIncrement = 0.1f, maxValue = 10f, minValue = 0.1f)]
        public float maxGeeforce = 10;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Cur/Max Temp", guiFormat = "F3")]
        public string TemperatureStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Cur/Max Geeforce")]
        public string GeeforceStr;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Stored Mass")]
        public double storedMassMultiplier = 1;
        [KSPField(isPersistant = true, guiActiveEditor = true)]
        public bool calculatedMass = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Part Mass", guiUnits = " t", guiFormat = "F3" )]
        public double partMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Initial Mass", guiUnits = " t", guiFormat = "F3")]
        public double initialMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Target Mass", guiUnits = " t", guiFormat = "F3")]
        public double targetMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Delta Mass", guiUnits = " t", guiFormat = "F3")]
        public double moduleMassDelta;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Attached Tanks Count")]
        public double attachedAntimatterTanksCount;



        bool charging = false;
        bool should_charge = false;
        double explosion_time = 0.35f;
        
        double explosion_size = 5000;
        double cur_explosion_size = 0;
        double current_antimatter = 0;

        int power_explode_counter = 0;
        int geeforce_explode_counter = 0;
        int temperature_explode_counter = 0;

        GameObject lightGameObject;
        PartResource antimatter;
        List<AntimatterStorageTank> attachedAntimatterTanks;

        [KSPEvent(guiActive = true, guiName = "Start Charging", active = true)]
        public void StartCharge()
        {
            should_charge = true;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Charging", active = true)]
        public void StopCharge()
        {
            should_charge = false;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("FNGenerator.OnRescale called with " + factor.absolute.linear);
                storedMassMultiplier = Math.Pow(factor.absolute.linear, massExponent);
                initialMass = part.prefabMass * storedMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnRescale " + e.Message);
            }
        }

        private void UpdateTargetMass()
        {
            // verify if mass calculation is active
            if (!calculatedMass)
            {
                targetMass = part.mass;
                return;
            }

            targetMass = (((maxTemperature - 30d) / 2000d) + (maxGeeforce / 20d)) * storedMassMultiplier;
            targetMass *= (1d - (0.2 * attachedAntimatterTanksCount));
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            if (HighLogic.LoadedSceneIsFlight)
                return ModifierChangeWhen.STAGED;
            else
                return ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (!calculatedMass)
                return 0;

            moduleMassDelta = (float)targetMass - initialMass;

            return (float)moduleMassDelta;
        }

        public void doExplode()
        {
            if (current_antimatter <= 0.1f) return;

            lightGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightGameObject.GetComponent<Collider>().enabled = false;
            lightGameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            lightGameObject.AddComponent<Light>();
            var renderer = lightGameObject.GetComponent<Renderer>();
            renderer.material.shader = Shader.Find("Unlit/Transparent");
            renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/explode", false);
            renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.9f);
            Light light = lightGameObject.GetComponent<Light>();
            lightGameObject.transform.position = part.transform.position;
            light.type = LightType.Point;
            light.color = Color.white;
            light.range = 100f;
            light.intensity = 500000.0f;
            light.renderMode = LightRenderMode.ForcePixel;
            //Destroy (lightGameObject.collider, 0.25f);
            Destroy(lightGameObject, 0.25f);

            //bool exist_parts_to_explode = true;
            //Part part_to_explode = null;
            exploding = true;
        }

        public override void OnStart(PartModule.StartState state)
        {
            antimatter = part.Resources[InterstellarResourcesConfiguration.Instance.Antimatter];

            partMass = part.mass;
            initialMass = part.prefabMass * storedMassMultiplier;

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;

                calculatedMass = true;
                UpdateTargetMass();
                return;
            }
            else
            {
                UpdateTargetMass();
            }
            
            this.part.force_activate();

            // charge if there is any antimatter
            should_charge = antimatter.amount > 0;

            this.enabled = true;

            UpdateAttachedTanks();
        }

        private void OnEditorAttach()
        {
            UpdateAttachedTanks();
            UpdateTargetMass();
        }

        private void OnEditorDetach()
        {
            if (attachedAntimatterTanks != null)
                attachedAntimatterTanks.ForEach(m => m.UpdateMass());

            UpdateAttachedTanks();
            UpdateTargetMass();
        }

        private void UpdateMass()
        {
            if (part.attachNodes == null) return;

            attachedAntimatterTanksCount = part.attachNodes.Where(m => m.nodeType == AttachNode.NodeType.Stack && m.attachedPart != null).Select(m => m.attachedPart.FindModuleImplementing<AntimatterStorageTank>()).Where(m => m != null).Count();
            UpdateTargetMass();
        }

        private void UpdateAttachedTanks()
        {
            if (part.attachNodes == null) return;

            attachedAntimatterTanks = part.attachNodes.Where(m => m.nodeType == AttachNode.NodeType.Stack && m.attachedPart != null).Select(m => m.attachedPart.FindModuleImplementing<AntimatterStorageTank>()).Where(m => m != null).ToList();
            attachedAntimatterTanks.ForEach(m => m.UpdateMass());
            attachedAntimatterTanksCount = attachedAntimatterTanks.Count();
        }

        public override void OnUpdate()
        {
            Events["StartCharge"].active = current_antimatter <= 0.1 && !should_charge;
            Events["StopCharge"].active = current_antimatter <= 0.1 && should_charge;

            chargeStatusStr = chargestatus.ToString("0.0") + " / " + GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE.ToString("0.0");
            TemperatureStr = part.temperature.ToString("0") + " / " + maxTemperature.ToString("0");
            GeeforceStr = part.vessel.geeForce.ToString("0.0") + " / " + maxGeeforce.ToString("0.0");

            if (chargestatus <= 60 && !charging && current_antimatter > 0.1)
                ScreenMessages.PostScreenMessage("Warning!: Antimatter storage unpowered, tank explosion in: " + chargestatus.ToString("0") + "s", 1.0f, ScreenMessageStyle.UPPER_CENTER);

            if (current_antimatter > 0.1)
            {
                if (charging)
                    statusStr = "Charging.";
                else
                    statusStr = "Discharging!";
            }
            else
            {
                if (should_charge)
                    statusStr = "Charging.";
                else
                    statusStr = "No Power Required.";
            }

            UpdateAmounts();
        }

        public void Update()
        {
            UpdateAmounts();
            UpdateTargetMass();
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight)
                return;
        }

        private void UpdateAmounts()
        {
            capacityStr = formatMassStr(antimatter.amount);
            maxAmountStr = formatMassStr(antimatter.maxAmount);
        }

        public override void OnFixedUpdate()
        {
            MaintainContainment();

            ExplodeContainer();
        }

        [KSPEvent(guiActive = true, guiName = "Self Destruct", active = true)]
        public void SelfDestruct()
        {
            doExplode();
        }

        private void MaintainContainment()
        {
            if (antimatter == null) return;

            float mult = 1;
            current_antimatter = antimatter.amount;

            if (chargestatus > 0 && (current_antimatter > 0.00001 * antimatter.maxAmount))
                chargestatus -= 1.0f * TimeWarp.fixedDeltaTime;

            if (chargestatus >= GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE)
                mult = 0.5f;

            if (!should_charge && current_antimatter <= 0.00001 * antimatter.maxAmount) return;

            var powerRequest = mult * 2.0 * chargeNeeded / 1000.0 * TimeWarp.fixedDeltaTime;

            double charge_to_add = CheatOptions.InfiniteElectricity
                ? powerRequest 
                : consumeFNResource(powerRequest, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000.0f / chargeNeeded;

            chargestatus += charge_to_add;

            if (charge_to_add < 2f * TimeWarp.fixedDeltaTime)
            {
                float more_charge_to_add = ORSHelper.fixedRequestResource(part, FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, mult * 2 * chargeNeeded * TimeWarp.fixedDeltaTime) / chargeNeeded;
                charge_to_add += more_charge_to_add;
                chargestatus += more_charge_to_add;
            }

            if (charge_to_add >= 1f * TimeWarp.fixedDeltaTime)
                charging = true;
            else
            {
                charging = false;
                if (TimeWarp.CurrentRateIndex > 3 && (current_antimatter > 0.00001 * antimatter.maxAmount))
                {
                    TimeWarp.SetRate(3, true);
                    ScreenMessages.PostScreenMessage("Cannot Time Warp faster than 50x while Antimatter Tank is Unpowered", 1.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            if (current_antimatter > 0.00001 * antimatter.maxAmount)
            {
                //verify temperature
                if (part.temperature > maxTemperature)
                {
                    temperature_explode_counter++;
                    if (temperature_explode_counter > 10)
                    {
                        Debug.Log("[KSPI] - Antimatter container exploded due to reaching critical temperature");
                        ScreenMessages.PostScreenMessage("Antimatter container exploded due to reaching critical temperature", 60.0f, ScreenMessageStyle.UPPER_CENTER);
                        doExplode();
                    }
                }
                else
                    temperature_explode_counter = 0;

                //verify geeforce
                if (part.vessel.geeForce > maxGeeforce)
                {
                    geeforce_explode_counter++;
                    if (geeforce_explode_counter > 10)
                    {
                        Debug.Log("[KSPI] - Antimatter container exploded due to reaching critical geeforce");
                        ScreenMessages.PostScreenMessage("Antimatter container exploded due to reaching critical geeforce", 60.0f, ScreenMessageStyle.UPPER_CENTER);
                        doExplode();
                    }
                }
                else
                    geeforce_explode_counter = 0;

                //verify power
                if (chargestatus <= 0)
                {
                    chargestatus = 0;
                    if (current_antimatter > 0.00001 * antimatter.maxAmount)
                    {
                        power_explode_counter++;
                        if (power_explode_counter > 10)
                        {
                            Debug.Log("[KSPI] - Antimatter container exploded due to running out of power");
                            ScreenMessages.PostScreenMessage("Antimatter container exploded due to running out of power", 60.0f, ScreenMessageStyle.UPPER_CENTER);
                            doExplode();
                        }
                    }
                }
                else
                    power_explode_counter = 0;
            }
            else
            {
                temperature_explode_counter = 0;
                geeforce_explode_counter = 0;
                power_explode_counter = 0;
            }
        

            if (chargestatus > GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE)
                chargestatus = GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE;
        }

        private void ExplodeContainer()
        {
            if (!exploding || lightGameObject == null) return;

            explosion_size = Math.Sqrt(current_antimatter) * 5.0;

            cur_explosion_size += TimeWarp.fixedDeltaTime * explosion_size * explosion_size / explosion_time;
            lightGameObject.transform.localScale = new Vector3(Mathf.Sqrt((float)cur_explosion_size), Mathf.Sqrt((float)cur_explosion_size), Mathf.Sqrt((float)cur_explosion_size));
            lightGameObject.GetComponent<Light>().range = Mathf.Sqrt((float)cur_explosion_size) * 15f;
            lightGameObject.GetComponent<Collider>().enabled = false;

            TimeWarp.SetRate(0, true);
            vessel.GoOffRails();

            Vessel[] list_of_vessels_to_explode = FlightGlobals.Vessels.ToArray();
            foreach (Vessel vess_to_explode in list_of_vessels_to_explode)
            {
                if (Vector3d.Distance(vess_to_explode.transform.position, vessel.transform.position) > explosion_size) continue;

                if (vess_to_explode.packed) continue;

                Part[] parts_to_explode = vess_to_explode.Parts.ToArray();
                foreach (Part part_to_explode in parts_to_explode)
                {
                    if (part_to_explode != null)
                        part_to_explode.explode();
                }
            }

            Part[] explode_parts = vessel.Parts.ToArray();
            foreach (Part explode_part in explode_parts)
            {
                if (explode_part != vessel.rootPart && explode_part != this.part)
                    explode_part.explode();
            }
            vessel.rootPart.explode();
            this.part.explode();
            //	this.part.explode ();
            //	vessel.rootPart.explode ();
        }

        public override string GetInfo()
        {
            return "Maximum Power Requirements: " + (chargeNeeded * 2).ToString("0") + " KW\nMinimum Power Requirements: " + chargeNeeded.ToString("0") + " KW";
        }

        public override int getPowerPriority()
        {
            return 1;
        }

        protected string formatMassStr(double amount)
        {
            if (amount >= 1000000000)
                return (amount / 1000000000).ToString("0.0000000") + " t";
            else if (amount >= 1000000)
                return (amount / 1000000).ToString("0.0000000") + " kg";
            else if (amount >= 1000)
                return (amount / 1000).ToString("0.0000000") + " g";
            else if (amount >= 1)
                return (amount).ToString("0.0000000") + " mg";
            else if (amount >= 1e-3)
                return (amount * 1e3).ToString("0.000000") + " ug";
            else if (amount > 1e-6)
                return (amount * 1e6).ToString("0.0000000") + " ng";
            else
                return (amount * 1e9).ToString("0.0000000") + " pg";
        }
    }

}

