using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;
using FNPlugin.Constants;
using KSP.Localization;

namespace FNPlugin
{
    [KSPModule("Antimatter Storage")]
    class AntimatterStorageTank : ResourceSuppliableModule, IPartMassModifier, IRescalable<FNGenerator>, IPartCostModifier
    {
        // persistent
        [KSPField(isPersistant = true)]
        public double chargestatus = 1000;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiUnits = "K", guiName = "#LOC_KSPIE_AntimatterStorageTank_MaxTemperature"), UI_FloatRange(stepIncrement = 10f, maxValue = 1000f, minValue = 40f)]//Maximum Temperature
        public float maxTemperature = 340;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiUnits = "g", guiName = "#LOC_KSPIE_AntimatterStorageTank_MaxAcceleration"), UI_FloatRange(stepIncrement = 0.05f, maxValue = 10f, minValue = 0.05f)]//Maximum Acceleration
        public float maxGeeforce = 1;

        [KSPField(guiName = "#LOC_KSPIE_AntimatterStorageTank_ModuleCost")]//Module Cost
        public double moduleCost;
        [KSPField]
        public double resourceCost;
        [KSPField]
        public double projectedCost;
        [KSPField]
        public double targetCost;

        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StoredMass")]//Stored Mass
        public double storedMassMultiplier = 1;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StoredTargetMass")]//Stored Target Mass
        public double storedTargetMassMultiplier = 1;

        [KSPField(isPersistant = true)]
        public double storedResourceCostMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double storedInitialCostMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double storedTargetCostMultiplier = 1;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_ScalingFactor")]//Scaling Factor
        public double storedScalingfactor = 1;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_AntomatterDensity")]//Antomatter Density
        public double antimatterDensity;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_InitialMass", guiUnits = " t", guiFormat = "F3")]//Initial Mass
        public double initialMass;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_TargetMass", guiUnits = " t", guiFormat = "F3")]//Target Mass
        public double targetMass;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_DeltaMass", guiUnits = " t", guiFormat = "F3")]//Delta Mass
        public float moduleMassDelta;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_AttachedTanksCount")]//Attached Tanks Count
        public double attachedAntimatterTanksCount;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_ResourceRatio", guiFormat = "F3")]//Resource Ratio
        public double resourceRatio;
        [KSPField(isPersistant = true)]
        public double emptyCost;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_TechLevel")]//Tech Level
        public int techLevel;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_Storedamount")]//Stored amount
        public double storedAmount;

        [KSPField]
        public double maxStorage = 0;

        [KSPField]
        public double Mk1AmountRatio = 1;
        [KSPField]
        public double Mk2AmountRatio = 1;
        [KSPField]
        public double Mk3AmountRatio = 1;
        [KSPField]
        public double Mk4AmountRatio = 1;
        [KSPField]
        public double Mk5AmountRatio = 1;
        [KSPField]
        public double Mk6AmountRatio = 1;
        [KSPField]
        public double Mk7AmountRatio = 1;

        [KSPField]
        public string Mk2Tech = "";
        [KSPField]
        public string Mk3Tech = "";
        [KSPField]
        public string Mk4Tech = "";
        [KSPField]
        public string Mk5Tech = "";
        [KSPField]
        public string Mk6Tech = "";
        [KSPField]
        public string Mk7Tech = "";

        //settings
        [KSPField]
        public float explosionPotentialMultiplier = 30000;
        [KSPField]
        public int maximumTimewarpWithGeeforceWarning = 3;
        [KSPField]
        public double maxCharge = 1000;
        [KSPField]
        public double massExponent = 3;
        [KSPField]
        public double massTargetExponent = 3;
        [KSPField] 
        public double dryCostInitialExponent = 2.5;
        [KSPField]
        public double dryCostTargetExponent = 2;
        [KSPField]
        public double chargeNeeded = 100;
        [KSPField]
        public string resourceName = "Antimatter";
        [KSPField]
        public double massTemperatureDivider = 12000;
        [KSPField]
        public double massGeeforceDivider = 40;
        [KSPField(guiName = "#LOC_KSPIE_AntimatterStorageTank_RequiredPower")]//Required Power
        public double effectivePowerNeeded;
        [KSPField(guiName = "#LOC_KSPIE_AntimatterStorageTank_Exploding")]//Exploding
        public bool exploding = false;
        [KSPField(guiName = "#LOC_KSPIE_AntimatterStorageTank_Charge")]//Charge
        public string chargeStatusStr;
        [KSPField(guiName = "#LOC_KSPIE_AntimatterStorageTank_Status")]//Status
        public string statusStr;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_Current")]//Current
        public string capacityStr;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_Maximum")]//Maximum
        public string maxAmountStr;
        [KSPField(guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_Cur_MaxTemp", guiFormat = "F3")]//Cur/Max Temp
        public string TemperatureStr;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_CurMaxGeeforce")]//Cur/Max Geeforce
        public string GeeforceStr;
        [KSPField]
        public bool canExplodeFromHeat = false;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_PartMass", guiUnits = " t", guiFormat = "F3" )]//Part Mass
        public double partMass;
        [KSPField]
        public bool calculatedMass = false;
        [KSPField]
        public bool canExplodeFromGeeForce = false;
        [KSPField]
        public double currentGeeForce;
        [KSPField]
        public bool GForcesIgnored;

        bool isJustAboutToDie = false;
        bool showAntimatterFields;
        bool charging = false;
        bool should_charge = false;

        float explosion_time = 0.35f;
        float explosion_size = 5000;
        float cur_explosion_size = 0;

        double minimimAnimatterAmount = 0;
        double antimatterDensityModifier;
        double effectiveMaxGeeforce;
        double previousSpeed;
        double previousFixedTime;

        int startup_timeout;
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

        Queue<double> geeforceQueue = new Queue<double>(20);

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StartCharging", active = true)]//Start Charging
        public void StartCharge()
        {
            should_charge = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StopCharging", active = true)]//Stop Charging
        public void StopCharge()
        {
            should_charge = false;
        }

        public virtual void OnRescale(ScalingFactor factor)
        {
            try
            {
                storedScalingfactor = factor.absolute.linear;

                storedResourceCostMultiplier = Math.Pow(storedScalingfactor, 3);
                storedMassMultiplier = Math.Pow(storedScalingfactor, massExponent);
                storedTargetMassMultiplier = Math.Pow(storedScalingfactor, massTargetExponent);
                storedInitialCostMultiplier = Math.Pow(storedScalingfactor, dryCostInitialExponent);
                storedTargetCostMultiplier = Math.Pow(storedScalingfactor, dryCostTargetExponent);

                initialMass = part.prefabMass * storedMassMultiplier;
                chargestatus = maxCharge;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: AntimatterStorageTank.OnRescale " + e.Message);
            }
        }

        public double StorageCapacityModifier
        {
            get
            {
                switch (techLevel)
                {
                    case 1:
                        return Mk1AmountRatio;
                    case 2:
                        return Mk2AmountRatio;
                    case 3:
                        return Mk3AmountRatio;
                    case 4:
                        return Mk4AmountRatio;
                    case 5:
                        return Mk5AmountRatio;
                    case 6:
                        return Mk6AmountRatio;
                    case 7:
                        return Mk7AmountRatio;
                    default:
                        return 1;
                }
            }
        }

        private void DetermineTechLevel()
        {
            techLevel = 1;
            if (PluginHelper.UpgradeAvailable(Mk2Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk3Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk4Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk5Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk6Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk7Tech))
                techLevel++;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return (float)ModuleCost();
        }

        private double ModuleCost()
        {
            emptyCost = part.partInfo.cost;

            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource != null)
                emptyCost -= maxStorage * antimatterResource.info.unitCost;

            resourceCost = 0;
            foreach (var resource in part.Resources)
            {
                resourceCost += resource.amount * resource.info.unitCost;
            }

            projectedCost = emptyCost * storedInitialCostMultiplier + resourceCost;
            targetCost = emptyCost * storedTargetCostMultiplier + resourceCost;
            moduleCost = targetCost - projectedCost;

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

            targetMass = ((((double)(decimal)maxTemperature - 40) / massTemperatureDivider) + ((double)(decimal)maxGeeforce / massGeeforceDivider)) * storedTargetMassMultiplier;
            targetMass *= (1 - (0.2 * attachedAntimatterTanksCount));
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

        public void DoExplode(string reason = null)
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null || antimatterResource.amount <= minimimAnimatterAmount) return;

            if (antimatterResource.resourceName != resourceName)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg1", antimatterResource.info.displayName), 10, ScreenMessageStyle.UPPER_CENTER);//"List all" + 
                antimatterResource.amount = 0;
                return;
            }

            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log("[KSPI]: " + reason);
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
            var light = lightGameObject.GetComponent<Light>();
            lightGameObject.transform.position = part.transform.position;
            light.type = LightType.Point;
            light.color = Color.white;
            light.range = 100f;
            light.intensity = 500000.0f;
            light.renderMode = LightRenderMode.ForcePixel;
            Destroy(lightGameObject, 0.25f);
            exploding = true;
        }

        public override void OnStart(StartState state)
        {
            deploymentAnimation = part.FindModuleImplementing<ModuleAnimateGeneric>();

            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;
            part.OnJustAboutToDie += OnJustAboutToDie; 

            antimatterDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);

            antimatterDensityModifier = 1e-17 / (double)(decimal)antimatterDefinition.density;

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

            // determine techlevel maximum storage amount only in editor
            if (state == StartState.Editor && maxStorage != 0)
            {
                DetermineTechLevel();
                var currentStorageRatio = antimatterResource.amount / antimatterResource.maxAmount;
                antimatterResource.maxAmount = maxStorage * storedResourceCostMultiplier * StorageCapacityModifier;
                antimatterResource.amount = antimatterResource.maxAmount * currentStorageRatio;
            }

            // charge if there is any significant antimatter
            should_charge = antimatterResource.amount > minimimAnimatterAmount;

            partMass = (double)(decimal)part.mass;
            initialMass = (double)(decimal)part.prefabMass * storedMassMultiplier;

            Fields["techLevel"].guiActiveEditor = maxStorage != 0;
            capacityStrField = Fields["capacityStr"];
            maxAmountStrField = Fields["maxAmountStr"];
            TemperatureStrField = Fields["TemperatureStr"];
            GeeforceStrField = Fields["GeeforceStr"];

            geeforceQueue.Enqueue(0);
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

            UpdateTolerances();

            UpdateAttachedTanks();
        }


        private bool HasSignificantAountOfAntimatter()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null) return false;
            return antimatterResource.amount > minimimAnimatterAmount;
        }
        

        void OnJustAboutToDie()
        {
            Debug.Log("[KSPI]: OnJustAboutToDie called on " + part.name);

            isJustAboutToDie = true;
        }

        void OnJustAboutToBeDestroyed()
        {
            Debug.Log("[KSPI]: OnJustAboutToBeDestroyed called on " + part.name);

            if (!isJustAboutToDie)
            {
                Debug.Log("[KSPI]: isJustAboutToDie == false");
                return;
            }

            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
            {
                Debug.Log("[KSPI]: antimatterResource == null");
                return;
            }

            if (antimatterResource.resourceName != resourceName)
            {
                Debug.Log("[KSPI]: antimatterResource.resourceName != resourceName");
                return;
            }

            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("[KSPI]: !HighLogic.LoadedSceneIsFlight");
                return;
            }

            if (antimatterResource.amount <= minimimAnimatterAmount)
            {
                Debug.Log("[KSPI]: antimatterResource.amount <= minimimAnimatterAmount");
                return;
            }

            if (!FlightGlobals.VesselsLoaded.Contains(this.vessel))
            {
                Debug.Log("[KSPI]: !FlightGlobals.VesselsLoaded.Contains(this.vessel)");
                return;
            }

            if (part.temperature >= part.maxTemp)
                DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg2"));//"Antimatter container exploded because antimatter melted and breached containment"
            else if (part.vessel.geeForce >= part.gTolerance)
                DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg3"));//"Antimatter container exploded because exceeding gee force Tolerance"
            else if (chargestatus <= 0)
                DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg4"));//"Antimatter container exploded because containment was unpowered"
            else
                DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg5"));//"Antimatter container exploded for unknown reason"

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

            attachedAntimatterTanksCount = part.attachNodes.Where(m => m.nodeType == AttachNode.NodeType.Stack && m.attachedPart != null).Select(m => m.attachedPart.FindModuleImplementing<AntimatterStorageTank>()).Count(m => m != null);
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

            // restore antimatter amount when stolen
            if (HighLogic.LoadedSceneIsEditor)
            {
                storedAmount = antimatterResource.amount;
            }
            else if (vessel.missionTime < 1 && storedAmount > 0 && antimatterResource.amount == 0)
            {
                antimatterResource.amount = storedAmount;
                storedAmount = 0;
            }

            capacityStr = PluginHelper.formatMassStr(antimatterResource.amount * antimatterDensity);
            maxAmountStr = PluginHelper.formatMassStr(antimatterResource.maxAmount * antimatterDensity);

            part.explosionPotential = (float)antimatterResource.amount * explosionPotentialMultiplier;

            UpdateTargetMass();

            UpdateTolerances();

            var newRatio = antimatterResource.amount / antimatterResource.maxAmount;

            // if closed and changed
            if (deploymentAnimation != null && deploymentAnimation.GetScalar == 0 && newRatio != resourceRatio && HighLogic.LoadedSceneIsEditor)
            {
                // open up
                deploymentAnimation.Toggle();
            }

            resourceRatio = newRatio;
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsEditor)
            {
                chargestatus = maxCharge;

                Fields["maxGeeforce"].guiActiveEditor = canExplodeFromGeeForce;
                Fields["maxTemperature"].guiActiveEditor = canExplodeFromHeat;
                return;
            }

            chargeStatusStr = chargestatus.ToString("0.0") + " / " + maxCharge.ToString("0.0");
            TemperatureStr = part.temperature.ToString("0") + " / " + maxTemperature.ToString("0");
            GeeforceStr = effectiveMaxGeeforce == 0
                ? maxGeeforce.ToString("0.0") + " when full" 
                : currentGeeForce.ToString("0.000") + " / " + effectiveMaxGeeforce.ToString("0.000");

            minimimAnimatterAmount = antimatterDensityModifier * antimatterResource.maxAmount;

            Events["StartCharge"].active = antimatterResource.amount <= minimimAnimatterAmount && !should_charge;
            Events["StopCharge"].active = antimatterResource.amount <= minimimAnimatterAmount && should_charge;

            if (maxCharge <= 60 && !charging && antimatterResource.amount > minimimAnimatterAmount)
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg6", chargestatus.ToString("0")), 0.5f, ScreenMessageStyle.UPPER_CENTER);//"Warning!: Antimatter storage unpowered, tank explosion in: " +  + "s"

            if (antimatterResource.amount > minimimAnimatterAmount)
                statusStr = charging ? Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu1") : Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu2");//"Charging.""Discharging!"
            else
                statusStr = should_charge ? Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu1") : Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu3");//"Charging.""No Power Required."
        }

        private void UpdateTolerances()
        {
            var significantAntimatter = HasSignificantAountOfAntimatter();
            if (canExplodeFromHeat && significantAntimatter)
                part.maxTemp = (double)(decimal)maxTemperature;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            GForcesIgnored = PluginHelper.GForcesIgnored;

            if (!vessel.packed)
            {
                var newGeeForce = PluginHelper.GForcesIgnored ? 0 : vessel.geeForce;
                currentGeeForce = geeforceQueue.Any(m => m > 0) ?  geeforceQueue.Where(m => m > 0).Min() : geeforceQueue.Average();
                geeforceQueue.Enqueue(newGeeForce);
                if (geeforceQueue.Count > 20)
                    geeforceQueue.Dequeue();
            }
            else
            {
                var acceleration = PluginHelper.GForcesIgnored ? 0 : (Math.Max(0, (Math.Abs(previousSpeed - vessel.obt_speed) / (Math.Max(TimeWarp.fixedDeltaTime, previousFixedTime)))));
                currentGeeForce = geeforceQueue.Any(m => m > 0) ? geeforceQueue.Where(m => m > 0).Min() : geeforceQueue.Average();
                geeforceQueue.Enqueue(acceleration / GameConstants.STANDARD_GRAVITY);
                if (geeforceQueue.Count > 20)
                    geeforceQueue.Dequeue();
            }
            previousSpeed = vessel.obt_speed;
            previousFixedTime = TimeWarp.fixedDeltaTime;              

            MaintainContainment();

            ExplodeContainer();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_SelfDestruct", active = true)]//Self Destruct
        public void SelfDestruct()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
                return;

            if (HighLogic.LoadedSceneIsEditor || antimatterResource.amount <= minimimAnimatterAmount) return;

            DoExplode();//"Antimatter container exploded because self destruct was activated"
        }

        private void MaintainContainment()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
                return;

            if (chargestatus > 0 && antimatterResource.amount > minimimAnimatterAmount)
            {
                chargestatus -= vessel.packed ? 0.05f : TimeWarp.fixedDeltaTime;
            }

            if (!should_charge && antimatterResource.amount <= minimimAnimatterAmount) return;

            var powerModifier = canExplodeFromGeeForce
                ? (resourceRatio * (currentGeeForce / 10) * 0.8) + ((part.temperature / 1000) * 0.2) 
                :  Math.Pow(resourceRatio, 2);

            effectivePowerNeeded = chargeNeeded * powerModifier;

            if (effectivePowerNeeded > 0)
            {
                var mult = chargestatus >= maxCharge ? 0.5 : 1;
                var powerRequest = mult * 2 * effectivePowerNeeded / 1000 * TimeWarp.fixedDeltaTime;

                // first try to accespower  megajoules
                var chargeToAdd = CheatOptions.InfiniteElectricity
                    ? powerRequest
                    : consumeFNResource(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES) * 1000 / effectivePowerNeeded;

                chargestatus += chargeToAdd;

                // alternatively  just look for any reserves of stored megajoules
                if (chargeToAdd == 0 && effectivePowerNeeded > 0)
                {
                    var moreChargeToAdd = part.RequestResource(ResourceManager.FNRESOURCE_MEGAJOULES, powerRequest) * 1000 / effectivePowerNeeded;

                    chargeToAdd += moreChargeToAdd;
                    chargestatus += moreChargeToAdd;
                }

                // if still not found any power attempt to find any electric charge to survive
                if (chargeToAdd < TimeWarp.fixedDeltaTime && effectivePowerNeeded > 0)
                {
                    var moreChargeToAdd = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, mult * 2 * effectivePowerNeeded * TimeWarp.fixedDeltaTime) / effectivePowerNeeded;
                    chargeToAdd += moreChargeToAdd;
                    chargestatus += moreChargeToAdd;
                }

                if (chargeToAdd >= TimeWarp.fixedDeltaTime)
                    charging = true;
                else
                {
                    charging = false;
                    if (TimeWarp.CurrentRateIndex > 3 && (antimatterResource.amount > minimimAnimatterAmount))
                    {
                        TimeWarp.SetRate(3, true);
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg7", TimeWarp.CurrentRate,antimatterResource.resourceName), 1, ScreenMessageStyle.UPPER_CENTER);//"Cannot Time Warp faster than " +  + "x while " +  + " Tank is Unpowered"
                    }
                }
            }

            if (startup_timeout > 0)
                startup_timeout--;

            if (startup_timeout == 0 && antimatterResource.amount > minimimAnimatterAmount)
            {
                //verify temperature
                if (!CheatOptions.IgnoreMaxTemperature &&  canExplodeFromHeat && part.temperature > (double)(decimal)maxTemperature)
                {
                    temperature_explode_counter++;
                    if (temperature_explode_counter > 20)
                        DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg8"));//"Antimatter container exploded due to reaching critical temperature"
                }
                else
                    temperature_explode_counter = 0;

                //verify geeforce
                effectiveMaxGeeforce = resourceRatio > 0 ? Math.Min(10, (double)(decimal)maxGeeforce / resourceRatio) : 10;
                if (!CheatOptions.UnbreakableJoints && canExplodeFromGeeForce)
                {
                    if (vessel.missionTime > 0)
                    {
                        if (currentGeeForce > effectiveMaxGeeforce)
                        {
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg9"), 1, ScreenMessageStyle.UPPER_CENTER);//"ALERT: geeforce at critical!"
                            geeforce_explode_counter++;
                            if (geeforce_explode_counter > 30)
                                DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg10"));//"Antimatter container exploded due to reaching critical geeforce"
                        }
                        else if (TimeWarp.CurrentRateIndex > maximumTimewarpWithGeeforceWarning && currentGeeForce > effectiveMaxGeeforce - 0.02)
                        {
                            TimeWarp.SetRate(maximumTimewarpWithGeeforceWarning, true);
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg11", TimeWarp.CurrentRate), 1, ScreenMessageStyle.UPPER_CENTER);//"ALERT: Cannot Time Warp faster than " +  + "x while geeforce near maximum tolerance!"
                        }
                        else if (currentGeeForce > effectiveMaxGeeforce - 0.04)
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg12", (currentGeeForce / effectiveMaxGeeforce * 100).ToString("F2")), 1, ScreenMessageStyle.UPPER_CENTER);//"ALERT: geeforce at " +  + "%  tolerance!"
                        else
                            geeforce_explode_counter = 0;
                    }
                    else if (currentGeeForce > effectiveMaxGeeforce)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg13"), 1, ScreenMessageStyle.UPPER_CENTER);//"Warning: geeforce tolerance exceeded but sustanable while the mission timer has not started"
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
                        if (power_explode_counter > 20)
                            DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg14"));//"Antimatter container exploded due to running out of power"
                    }
                }
                else
                    power_explode_counter = 0;
            }
            else
            {
                effectiveMaxGeeforce = 0;
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

            cur_explosion_size += TimeWarp.fixedDeltaTime * explosion_size * explosion_size / explosion_time;
            lightGameObject.transform.localScale = new Vector3(Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size));
            lightGameObject.GetComponent<Light>().range = Mathf.Sqrt(cur_explosion_size) * 15f;
            lightGameObject.GetComponent<Collider>().enabled = false;

            TimeWarp.SetRate(0, true);
            vessel.GoOffRails();

            var listOfVesselsToExplode = FlightGlobals.Vessels.ToArray();
            foreach (var vessToExplode in listOfVesselsToExplode)
            {
                if (Vector3d.Distance(vessToExplode.transform.position, vessel.transform.position) > explosion_size) continue;

                if (vessToExplode.packed) continue;

                var partsToExplode = vessToExplode.Parts.ToArray();
                foreach (var partToExplode in partsToExplode.Where(partToExplode => partToExplode != null))
                {
                    partToExplode.explode();
                }
            }

            var explodeParts = vessel.Parts.ToArray();
            foreach (var explodePart in explodeParts.Where(explodePart => explodePart != vessel.rootPart && explodePart != this.part))
            {
                explodePart.explode();
            }
            vessel.rootPart.explode();
            this.part.explode();
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Getinfo1") + (chargeNeeded * 2).ToString("0") + " KW");//"Maximum Power Requirements: "
            if (canExplodeFromGeeForce)
                info.AppendLine(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Getinfo2"));//"Maximum Geeforce: 10 G"
            if (canExplodeFromHeat)
                info.AppendLine(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Getinfo3"));//"Maximum Geeforce: 1000 K"

            return info.ToString();
        }

        public override int getPowerPriority()
        {
            return 0;
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title;
        }
    }

}

