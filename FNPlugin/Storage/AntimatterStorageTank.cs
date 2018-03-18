using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;

namespace FNPlugin
{
    [KSPModule("Antimatter Storage")]
    class AntimatterStorageTank : ResourceSuppliableModule, IPartMassModifier, IRescalable<FNGenerator>, IPartCostModifier
    {
        [KSPField(isPersistant = true)]
        public double chargestatus = 1000;
        [KSPField(isPersistant = false)]
        public double maxCharge = 1000;
        [KSPField(isPersistant = false)]
        public float massExponent = 3;
        [KSPField(isPersistant = false)]
        public double chargeNeeded = 100;
        [KSPField(isPersistant = false)]
        public string resourceName = "Antimatter";

        [KSPField(isPersistant = false, guiActive = false, guiName = "Required Power")]
        public double effectivePowerNeeded;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Exploding")]
        public bool exploding = false;
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

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Module Cost")]
        public float moduleCost = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Stored Mass")]
        public double storedMassMultiplier = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Scaling Factor")]
        public double storedScalingfactor = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Fixed Delta Time")]
        public double fixedDeltaTime;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Antomatter Density")]
        public double antimatterDensity;

        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool calculatedMass = false;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool canExplodeFromGeeForce = false;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public bool canExplodeFromHeat = false;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Part Mass", guiUnits = " t", guiFormat = "F3" )]
        public double partMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Initial Mass", guiUnits = " t", guiFormat = "F3")]
        public double initialMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Target Mass", guiUnits = " t", guiFormat = "F3")]
        public double targetMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Delta Mass", guiUnits = " t", guiFormat = "F3")]
        public float moduleMassDelta;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Attached Tanks Count")]
        public double attachedAntimatterTanksCount;
        [KSPField(isPersistant = true, guiName = "Resource Ratio", guiActiveEditor = false, guiActive = false, guiFormat = "F3")]
        public double resourceRatio;

        [KSPField(isPersistant = true)]
        public float emptyCost = 0;
        [KSPField(isPersistant = true)]
        public float dryCost = 0;
        [KSPField(isPersistant = true)]
        public double partCost;

        //[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiUnits = "%", guiName = "Anti Hydrogen"), UI_FloatRange(stepIncrement = 0.1f, maxValue = 100f, minValue = 0f)]
        //public float resourceFloatRange = 0;

        bool isJustAboutToDie = false;
        bool showAntimatterFields;
        bool charging = false;
        bool should_charge = false;
        float explosion_time = 0.35f;
        
        float explosion_size = 5000;
        float cur_explosion_size = 0;
        double minimimAnimatterAmount = 0;
        double antimatterDensityModifier;

        int startup_timeout = 200;
        int power_explode_counter = 0;
        int geeforce_explode_counter = 0;
        int temperature_explode_counter = 0;

        GameObject lightGameObject;
        ModuleAnimateGeneric deploymentAnimation;
        PartResourceDefinition antimatterDefinition;
        List<AntimatterStorageTank> attachedAntimatterTanks;

        BaseField capacityStrField;
        BaseField maxAmountStrField;
        BaseField TemperatureStrField;
        BaseField GeeforceStrField;

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
                storedScalingfactor = factor.absolute.linear;
                storedMassMultiplier = Math.Pow(storedScalingfactor, massExponent);
                initialMass = part.prefabMass * storedMassMultiplier;
                chargestatus = maxCharge;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - AntimatterStorageTank.OnRescale " + e.Message);
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            if (storedMassMultiplier == 1 && emptyCost != 0)
                moduleCost = emptyCost; 
            else
                moduleCost = dryCost * Mathf.Pow((float)storedScalingfactor, 3);

            return moduleCost;
        }
        
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return ModifierChangeWhen.CONSTANTLY;
            else
                return ModifierChangeWhen.STAGED;
        }

        private void UpdateTargetMass()
        {
            // verify if mass calculation is active
            if (!calculatedMass)
            {
                targetMass = part.mass;
                return;
            }

            targetMass = (((maxTemperature - 30d) / 2000d) + ((double)(decimal)maxGeeforce / 20d)) * storedMassMultiplier;
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

            moduleMassDelta = (float)(targetMass - initialMass);

            return moduleMassDelta;
        }

        public void doExplode(string reason = null)
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null || antimatterResource.amount <= minimimAnimatterAmount) return;

            if (antimatterResource.resourceName != resourceName)
            {
                ScreenMessages.PostScreenMessage("List all" + antimatterResource.info.displayName, 10.0f, ScreenMessageStyle.UPPER_CENTER);
                antimatterResource.amount = 0;
                return;
            }

            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log("[KSPI] - " + reason);
                ScreenMessages.PostScreenMessage(reason, 10.0f, ScreenMessageStyle.UPPER_CENTER);
            }

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
            Destroy(lightGameObject, 0.25f);
            exploding = true;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (canExplodeFromHeat)
                part.maxTemp = (double)(decimal)maxTemperature;

            if (canExplodeFromGeeForce)
            {
                part.crashTolerance = maxGeeforce;
                part.gTolerance = maxGeeforce;
            }

            deploymentAnimation = part.FindModuleImplementing<ModuleAnimateGeneric>();

            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;
            part.OnJustAboutToDie += OnJustAboutToDie; 

            antimatterDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);

            antimatterDensityModifier = 1e-17 / antimatterDefinition.density;

            antimatterDensity = (double)(decimal)antimatterDefinition.density;

            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
            {
                var alternativeResource = part.Resources.OrderBy(m => m.maxAmount).FirstOrDefault();
                if (alternativeResource != null)
                    antimatterResource = alternativeResource;
                else
                    return;
            }

            // charge if there is any significant antimatter
            should_charge = antimatterResource.amount > minimimAnimatterAmount;

            partMass = part.mass;
            initialMass = part.prefabMass * storedMassMultiplier;

            Fields["partMass"].guiActive = calculatedMass;
            Fields["partMass"].guiActiveEditor = calculatedMass;

            capacityStrField = Fields["capacityStr"];
            maxAmountStrField = Fields["maxAmountStr"];
            TemperatureStrField = Fields["TemperatureStr"];
            GeeforceStrField = Fields["GeeforceStr"];

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;

                UpdateTargetMass();
                return;
            }
            else
                UpdateTargetMass();

            this.enabled = true;

            UpdateAttachedTanks();
        }

        

        void OnJustAboutToDie()
        {
            Debug.Log("[KSPI] - OnJustAboutToDie called on " + part.name);

            isJustAboutToDie = true;
        }

        void OnJustAboutToBeDestroyed()
        {
            Debug.Log("[KSPI] - OnJustAboutToBeDestroyed called on " + part.name);

            if (!isJustAboutToDie)
            {
                Debug.Log("[KSPI] - isJustAboutToDie == false");
                return;
            }

            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
            {
                Debug.Log("[KSPI] - antimatterResource == null");
                return;
            }

            if (antimatterResource.resourceName != resourceName)
            {
                Debug.Log("[KSPI] - antimatterResource.resourceName != resourceName");
                return;
            }

            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("[KSPI] - !HighLogic.LoadedSceneIsFlight");
                return;
            }

            if (antimatterResource.amount <= minimimAnimatterAmount)
            {
                Debug.Log("[KSPI] - antimatterResource.amount <= minimimAnimatterAmount");
                return;
            }

            if (!FlightGlobals.VesselsLoaded.Contains(this.vessel))
            {
                Debug.Log("[KSPI] - !FlightGlobals.VesselsLoaded.Contains(this.vessel)");
                return;
            }

            if (part.temperature >= part.maxTemp)
                doExplode("Antimatter container exploded because antimatter melted and breached containment");
            else if (part.vessel.geeForce >= part.gTolerance)
                doExplode("Antimatter container exploded because exceeding gee force Tolerance");
            else if (chargestatus <= 0)
                doExplode("Antimatter container exploded because containment was unpowered");
            else
                doExplode("Antimatter container exploded for unknown reason");

            part.OnJustAboutToBeDestroyed -= OnJustAboutToBeDestroyed;

            ExplodeContainer();
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

        public void Update()
        {
            var antimatterResource = part.Resources[resourceName];

            showAntimatterFields = antimatterResource != null && antimatterResource.resourceName == resourceName;

            TemperatureStrField.guiActive = canExplodeFromHeat;
            TemperatureStrField.guiActiveEditor = canExplodeFromHeat;
            GeeforceStrField.guiActive = canExplodeFromGeeForce;
            GeeforceStrField.guiActiveEditor = canExplodeFromGeeForce;

            if (antimatterResource == null)
            {
                antimatterResource = part.Resources.OrderByDescending(m => m.maxAmount).FirstOrDefault();
                if (antimatterResource == null)
                    return;
            }

            capacityStrField.guiActive = showAntimatterFields;
            capacityStrField.guiActiveEditor = showAntimatterFields;
            maxAmountStrField.guiActive = showAntimatterFields;
            maxAmountStrField.guiActiveEditor = showAntimatterFields;

            capacityStr = PluginHelper.formatMassStr(antimatterResource.amount * antimatterDensity);
            maxAmountStr = PluginHelper.formatMassStr(antimatterResource.maxAmount * antimatterDensity);

            UpdateTargetMass();

            var newRatio = antimatterResource.amount / antimatterResource.maxAmount;

            // if closed and changed
            if (deploymentAnimation != null && deploymentAnimation.GetScalar == 0 && newRatio != resourceRatio && HighLogic.LoadedSceneIsEditor)
            {
                // open up
                deploymentAnimation.Toggle();
            }

            resourceRatio = newRatio;
            partMass = part.mass;
            partCost = part.partInfo.cost;

            if (HighLogic.LoadedSceneIsEditor)
            {
                chargestatus = maxCharge;

                Fields["maxGeeforce"].guiActiveEditor = canExplodeFromGeeForce;
                Fields["maxTemperature"].guiActiveEditor = canExplodeFromHeat;
                return;
            }

            chargeStatusStr = chargestatus.ToString("0.0") + " / " + maxCharge.ToString("0.0");
            TemperatureStr = part.temperature.ToString("0") + " / " + maxTemperature.ToString("0");
            GeeforceStr = part.vessel.geeForce.ToString("0.0") + " / " + maxGeeforce.ToString("0.0");

            minimimAnimatterAmount = antimatterDensityModifier * antimatterResource.maxAmount;

            Events["StartCharge"].active = antimatterResource.amount <= minimimAnimatterAmount && !should_charge;
            Events["StopCharge"].active = antimatterResource.amount <= minimimAnimatterAmount && should_charge;

            if (maxCharge <= 60 && !charging && antimatterResource.amount > minimimAnimatterAmount)
                ScreenMessages.PostScreenMessage("Warning!: Antimatter storage unpowered, tank explosion in: " + chargestatus.ToString("0") + "s", 0.5f, ScreenMessageStyle.UPPER_CENTER);

            if (antimatterResource.amount > minimimAnimatterAmount)
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
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime,7);

            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
                return;

            MaintainContainment();

            ExplodeContainer();
        }

        [KSPEvent(guiActive = true, guiName = "Self Destruct", active = true)]
        public void SelfDestruct()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
                return;

            if (HighLogic.LoadedSceneIsEditor || antimatterResource.amount <= minimimAnimatterAmount) return;

            doExplode("Antimatter container exploded because self destruct was activated");
        }

        private void MaintainContainment()
        {
            var antimatterResource = part.Resources[resourceName];
            if (chargestatus > 0 && antimatterResource.amount > minimimAnimatterAmount)
                chargestatus -= fixedDeltaTime;

            if (!should_charge && antimatterResource.amount <= minimimAnimatterAmount) return;

            var powerModifier = canExplodeFromGeeForce 
                ? (resourceRatio * (part.vessel.geeForce / 10) * 0.8) + ((part.temperature / 1000) * 0.2) 
                :  Math.Pow(resourceRatio, 2);

            effectivePowerNeeded = chargeNeeded * powerModifier;

            if (effectivePowerNeeded > 0)
            {
                var mult = chargestatus >= maxCharge ? 0.5 : 1;
                var powerRequest = mult * 2 * effectivePowerNeeded / 1000 * fixedDeltaTime;

                // first try to accespower  megajoules
                double charge_to_add = CheatOptions.InfiniteElectricity
                    ? powerRequest
                    : consumeFNResource(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES) * 1000 / effectivePowerNeeded;

                chargestatus += charge_to_add;

                // alternatively  just look for any reserves of stored megajoules
                if (charge_to_add == 0 && effectivePowerNeeded > 0)
                {
                    double more_charge_to_add = part.RequestResource(ResourceManager.FNRESOURCE_MEGAJOULES, powerRequest) * 1000 / effectivePowerNeeded;

                    charge_to_add += more_charge_to_add;
                    chargestatus += more_charge_to_add;
                }

                // if still not found any power attempt to find any electricc charge to survive
                if (charge_to_add < fixedDeltaTime && effectivePowerNeeded > 0)
                {
                    double more_charge_to_add = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, mult * 2 * effectivePowerNeeded * fixedDeltaTime) / effectivePowerNeeded;
                    charge_to_add += more_charge_to_add;
                    chargestatus += more_charge_to_add;
                }

                if (charge_to_add >= fixedDeltaTime)
                    charging = true;
                else
                {
                    charging = false;
                    if (TimeWarp.CurrentRateIndex > 3 && (antimatterResource.amount > minimimAnimatterAmount))
                    {
                        TimeWarp.SetRate(3, true);
                        ScreenMessages.PostScreenMessage("Cannot Time Warp faster than 50x while " + antimatterResource.resourceName + " Tank is Unpowered", 1, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
            }

            if (startup_timeout > 0)
                startup_timeout--;

            if (startup_timeout == 0 && antimatterResource.amount > minimimAnimatterAmount)
            {
                //verify temperature
                if (!CheatOptions.IgnoreMaxTemperature &&  canExplodeFromHeat && part.temperature > maxTemperature)
                {
                    temperature_explode_counter++;
                    if (temperature_explode_counter > 10)
                        doExplode("Antimatter container exploded due to reaching critical temperature");
                }
                else
                    temperature_explode_counter = 0;

                //verify geeforce
                if (!CheatOptions.UnbreakableJoints && canExplodeFromGeeForce && part.vessel.geeForce > maxGeeforce)
                {
                    geeforce_explode_counter++;
                    if (geeforce_explode_counter > 10)
                        doExplode("Antimatter container exploded due to reaching critical geeforce");
                }
                else
                    geeforce_explode_counter = 0;

                //verify power
                if (chargestatus <= 0)
                {
                    chargestatus = 0;
                    if (!CheatOptions.InfiniteElectricity && antimatterResource.amount > 0.00001 * antimatterResource.maxAmount)
                    {
                        power_explode_counter++;
                        if (power_explode_counter > 10)
                            doExplode("Antimatter container exploded due to running out of power");
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

            if (chargestatus > maxCharge)
                chargestatus = maxCharge;
        }

        private void ExplodeContainer()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null || antimatterResource.resourceName != resourceName)
                return;

            if (!exploding || lightGameObject == null) return;

            explosion_size = Mathf.Sqrt((float)antimatterResource.amount) * 5;

            cur_explosion_size += (float)fixedDeltaTime * explosion_size * explosion_size / explosion_time;
            lightGameObject.transform.localScale = new Vector3(Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size));
            lightGameObject.GetComponent<Light>().range = Mathf.Sqrt(cur_explosion_size) * 15f;
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
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.AppendLine("Maximum Power Requirements: " + (chargeNeeded * 2).ToString("0") + " KW");
            if (canExplodeFromGeeForce)
                info.AppendLine("Maximum Geeforce: 10 G");
            if (canExplodeFromHeat)
                info.AppendLine("Maximum Geeforce: 1000 K");

            return info.ToString();
        }

        public override int getPowerPriority()
        {
            return 1;
        }


    }

}

