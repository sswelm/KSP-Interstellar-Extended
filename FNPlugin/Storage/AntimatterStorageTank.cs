using FNPlugin.Constants;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using TweakScale;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Antimatter Storage")]
    class AntimatterStorageTank : ResourceSuppliableModule, IPartMassModifier, IRescalable<FNGenerator>, IPartCostModifier
    {
        public const string GROUP = "AntimatterStorageTank";
        public const string GROUP_TITLE = "#LOC_KSPIE_AntimatterStorageTank_groupName";

        // persistent
        [KSPField(isPersistant = true)]
        public double chargeStatus = 1000;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiUnits = "K", guiName = "#LOC_KSPIE_AntimatterStorageTank_MaxTemperature"), UI_FloatRange(stepIncrement = 10f, maxValue = 1000f, minValue = 40f)]//Maximum Temperature
        public float maxTemperature = 340;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiUnits = "g", guiName = "#LOC_KSPIE_AntimatterStorageTank_MaxAcceleration"), UI_FloatRange(stepIncrement = 0.05f, maxValue = 10f, minValue = 0.05f)]//Maximum Acceleration
        public float maxGeeforce = 1;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_AntimatterStorageTank_ModuleCost")]//Module Cost
        public double moduleCost;
        [KSPField]
        public double resourceCost;
        [KSPField]
        public double projectedCost;
        [KSPField]
        public double targetCost;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StoredMass")]//Stored Mass
        public double storedMassMultiplier = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StoredTargetMass")]//Stored Target Mass
        public double storedTargetMassMultiplier = 1;

        [KSPField(isPersistant = true)]
        public double storedResourceCostMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double storedInitialCostMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double storedTargetCostMultiplier = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_ScalingFactor")]//Scaling Factor
        public double storedScalingfactor = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_AntomatterDensity")]//Antomatter Density
        public double antimatterDensity;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_InitialMass", guiUnits = " t", guiFormat = "F3")]//Initial Mass
        public double initialMass;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_TargetMass", guiUnits = " t", guiFormat = "F3")]//Target Mass
        public double targetMass;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_DeltaMass", guiUnits = " t", guiFormat = "F3")]//Delta Mass
        public float moduleMassDelta;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_AttachedTanksCount")]//Attached Tanks Count
        public double attachedAntimatterTanksCount;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_ResourceRatio", guiFormat = "F3")]//Resource Ratio
        public double resourceRatio;
        [KSPField(isPersistant = true)]
        public double emptyCost;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_TechLevel")]//Tech Level
        public int techLevel;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_AntimatterStorageTank_Storedamount")]//Stored amount
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
        public float explosionPotentialMultiplier = 90000;
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
        public string resourceName = ResourceSettings.Config.AntiProtium;
        [KSPField]
        public double massTemperatureDivider = 12000;
        [KSPField]
        public double massGeeforceDivider = 40;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_AntimatterStorageTank_RequiredPower")]//Required Power
        public double effectivePowerNeeded;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_AntimatterStorageTank_Exploding")]//Exploding
        public bool exploding;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_AntimatterStorageTank_Charge")]//Charge
        public string chargeStatusStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_AntimatterStorageTank_Status")]//Status
        public string statusStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_Current")]//Current
        public string capacityStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_Maximum")]//Maximum
        public string maxAmountStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_Cur_MaxTemp", guiFormat = "F0")]//Cur/Max Temp
        public string TemperatureStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_CurMaxGeeforce")]//Cur/Max Geeforce
        public string GeeforceStr;
        [KSPField]
        public bool canExplodeFromHeat = false;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_PartMass", guiUnits = " t", guiFormat = "F3" )]//Part Mass
        public double partMass;
        [KSPField]
        public bool calculatedMass = false;
        [KSPField]
        public bool canExplodeFromGeeForce = false;
        [KSPField]
        public double currentGeeForce;
        [KSPField]
        public bool GForcesIgnored;

        bool _isJustAboutToDie;
        bool _showAntimatterFields;
        bool _charging;
        bool _shouldCharge;

        float _explosionTime = 0.35f;
        float _explosionSize = 5000;
        float _curExplosionSize;

        double _minimumAntimatterAmount;
        double _antimatterDensityModifier;
        double _effectiveMaxGeeforce;
        double _previousSpeed;
        double _previousFixedTime;

        int _startupTimeout;
        int _powerExplodeCounter;
        int _geeforceExplodeCounter;
        int _temperatureExplodeCounter;

        GameObject _lightGameObject;
        ModuleAnimateGeneric _deploymentAnimation;
        PartResourceDefinition _antimatterDefinition;
        List<AntimatterStorageTank> _attachedAntimatterTanks;

        BaseField _capacityStrField;
        BaseField _maxAmountStrField;
        BaseField _temperatureStrField;
        BaseField _geeforceStrField;

        readonly Queue<double> _geeforceQueue = new Queue<double>(20);

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StartCharging", active = true)]//Start Charging
        public void StartCharge()
        {
            _shouldCharge = true;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_StopCharging", active = true)]//Stop Charging
        public void StopCharge()
        {
            _shouldCharge = false;
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
                chargeStatus = maxCharge;
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
            double costReduction = 0;
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource != null)
                costReduction = maxStorage * antimatterResource.info.unitCost;

            resourceCost = 0;
            foreach (var resource in part.Resources)
            {
                resourceCost += resource.amount * resource.info.unitCost;
            }

            emptyCost = part.partInfo.cost - costReduction;
            projectedCost = emptyCost * storedInitialCostMultiplier + resourceCost;
            targetCost = emptyCost * storedTargetCostMultiplier + resourceCost;
            moduleCost = targetCost - projectedCost;

            // Hack to prevent weird cost due to changed resource amounts
            if (moduleCost == 0 && storedInitialCostMultiplier == 1 && antimatterResource != null)
            {
                moduleCost = (antimatterResource.maxAmount - maxStorage) * antimatterResource.info.unitCost;
            }

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

            targetMass = ((((double)(decimal)maxTemperature - 40) / massTemperatureDivider) + (maxGeeforce / massGeeforceDivider)) * storedTargetMassMultiplier;
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
            if (antimatterResource == null || antimatterResource.amount <= _minimumAntimatterAmount) return;

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

            _lightGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _lightGameObject.GetComponent<Collider>().enabled = false;
            _lightGameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            _lightGameObject.AddComponent<Light>();
            var renderer = _lightGameObject.GetComponent<Renderer>();
            renderer.material.shader = Shader.Find("Unlit/Transparent");
            renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/explode", false);
            renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.9f);
            var light = _lightGameObject.GetComponent<Light>();
            _lightGameObject.transform.position = part.transform.position;
            light.type = LightType.Point;
            light.color = Color.white;
            light.range = 100f;
            light.intensity = 500000.0f;
            light.renderMode = LightRenderMode.ForcePixel;
            Destroy(_lightGameObject, 0.25f);
            exploding = true;
        }

        public override void OnStart(StartState state)
        {
            _deploymentAnimation = part.FindModuleImplementing<ModuleAnimateGeneric>();

            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;
            part.OnJustAboutToDie += OnJustAboutToDie;

            _antimatterDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);

            _antimatterDensityModifier = 1e-17 / _antimatterDefinition.density;

            antimatterDensity = _antimatterDefinition.density;

            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
            {
                var alternativeResource = part.Resources.OrderBy(m => m.maxAmount).FirstOrDefault();
                if (alternativeResource != null)
                    antimatterResource = alternativeResource;
                else
                    return;
            }

            // determine TechLevel maximum storage amount only in editor
            if (state == StartState.Editor && maxStorage != 0)
            {
                DetermineTechLevel();
                var currentStorageRatio = antimatterResource.amount / antimatterResource.maxAmount;
                antimatterResource.maxAmount = maxStorage * storedResourceCostMultiplier * StorageCapacityModifier;
                antimatterResource.amount = antimatterResource.maxAmount * currentStorageRatio;
            }

            // charge if there is any significant antimatter
            _shouldCharge = antimatterResource.amount > _minimumAntimatterAmount;

            partMass = part.mass;
            initialMass = part.prefabMass * storedMassMultiplier;

            Fields[nameof(techLevel)].guiActiveEditor = maxStorage != 0;
            _capacityStrField = Fields[nameof(capacityStr)];
            _maxAmountStrField = Fields[nameof(maxAmountStr)];
            _temperatureStrField = Fields[nameof(TemperatureStr)];
            _geeforceStrField = Fields[nameof(GeeforceStr)];

            _geeforceQueue.Enqueue(0);
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


        private bool HasSignificantAmountOfAntimatter()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null) return false;
            return antimatterResource.amount > _minimumAntimatterAmount;
        }


        void OnJustAboutToDie()
        {
            Debug.Log("[KSPI]: OnJustAboutToDie called on " + part.name);

            _isJustAboutToDie = true;
        }

        void OnJustAboutToBeDestroyed()
        {
            Debug.Log("[KSPI]: OnJustAboutToBeDestroyed called on " + part.name);

            if (!_isJustAboutToDie)
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

            if (antimatterResource.amount <= _minimumAntimatterAmount)
            {
                Debug.Log("[KSPI]: antimatterResource.amount <= minimumAntimatterAmount");
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
            else if (chargeStatus <= 0)
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
            _attachedAntimatterTanks?.ForEach(m => m.UpdateMass());

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

            _attachedAntimatterTanks = part.attachNodes.Where(m => m.nodeType == AttachNode.NodeType.Stack && m.attachedPart != null).Select(m => m.attachedPart.FindModuleImplementing<AntimatterStorageTank>()).Where(m => m != null).ToList();
            _attachedAntimatterTanks.ForEach(m => m.UpdateMass());
            attachedAntimatterTanksCount = _attachedAntimatterTanks.Count();
        }

        public void Update()
        {
            var antimatterResource = part.Resources[resourceName];

            _showAntimatterFields = antimatterResource != null && antimatterResource.resourceName == resourceName;

            _temperatureStrField.guiActive = canExplodeFromHeat;
            _temperatureStrField.guiActiveEditor = canExplodeFromHeat;
            _geeforceStrField.guiActive = canExplodeFromGeeForce;
            _geeforceStrField.guiActiveEditor = canExplodeFromGeeForce;

            if (antimatterResource == null)
            {
                antimatterResource = part.Resources.OrderByDescending(m => m.maxAmount).FirstOrDefault();
                if (antimatterResource == null)
                    return;
            }

            _capacityStrField.guiActive = _showAntimatterFields;
            _capacityStrField.guiActiveEditor = _showAntimatterFields;
            _maxAmountStrField.guiActive = _showAntimatterFields;
            _maxAmountStrField.guiActiveEditor = _showAntimatterFields;

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
            if (_deploymentAnimation != null && _deploymentAnimation.GetScalar == 0 && newRatio != resourceRatio && HighLogic.LoadedSceneIsEditor)
            {
                // open up
                _deploymentAnimation.Toggle();
            }

            resourceRatio = newRatio;
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsEditor)
            {
                chargeStatus = maxCharge;

                Fields[nameof(maxGeeforce)].guiActiveEditor = canExplodeFromGeeForce;
                Fields[nameof(maxTemperature)].guiActiveEditor = canExplodeFromHeat;
                return;
            }

            chargeStatusStr = chargeStatus.ToString("0.0") + " / " + maxCharge.ToString("0.0");
            TemperatureStr = part.temperature.ToString("0") + " / " + maxTemperature.ToString("0");
            GeeforceStr = _effectiveMaxGeeforce == 0
                ? maxGeeforce.ToString("0.0") + " when full"
                : currentGeeForce.ToString("0.000") + " / " + _effectiveMaxGeeforce.ToString("0.000");

            _minimumAntimatterAmount = _antimatterDensityModifier * antimatterResource.maxAmount;

            Events[nameof(StartCharge)].active = antimatterResource.amount <= _minimumAntimatterAmount && !_shouldCharge;
            Events[nameof(StopCharge)].active = antimatterResource.amount <= _minimumAntimatterAmount && _shouldCharge;

            if (maxCharge <= 60 && !_charging && antimatterResource.amount > _minimumAntimatterAmount)
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg6", chargeStatus.ToString("0")), 0.5f, ScreenMessageStyle.UPPER_CENTER);//"Warning!: Antimatter storage unpowered, tank explosion in: " +  + "s"

            if (antimatterResource.amount > _minimumAntimatterAmount)
                statusStr = _charging ? Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu1") : Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu2");//"Charging.""Discharging!"
            else
                statusStr = _shouldCharge ? Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu1") : Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Statu3");//"Charging.""No Power Required."
        }

        private void UpdateTolerances()
        {
            var significantAntimatter = HasSignificantAmountOfAntimatter();
            if (canExplodeFromHeat && significantAntimatter)
                part.maxTemp = maxTemperature;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            GForcesIgnored = PluginHelper.GForcesIgnored;

            if (!vessel.packed)
            {
                var newGeeForce = PluginHelper.GForcesIgnored ? 0 : vessel.geeForce;
                currentGeeForce = _geeforceQueue.Any(m => m > 0) ?  _geeforceQueue.Where(m => m > 0).Min() : _geeforceQueue.Average();
                _geeforceQueue.Enqueue(newGeeForce);
                if (_geeforceQueue.Count > 20)
                    _geeforceQueue.Dequeue();
            }
            else
            {
                var acceleration = PluginHelper.GForcesIgnored ? 0 : (Math.Max(0, (Math.Abs(_previousSpeed - vessel.obt_speed) / (Math.Max(TimeWarp.fixedDeltaTime, _previousFixedTime)))));
                currentGeeForce = _geeforceQueue.Any(m => m > 0) ? _geeforceQueue.Where(m => m > 0).Min() : _geeforceQueue.Average();
                _geeforceQueue.Enqueue(acceleration / GameConstants.STANDARD_GRAVITY);
                if (_geeforceQueue.Count > 20)
                    _geeforceQueue.Dequeue();
            }
            _previousSpeed = vessel.obt_speed;
            _previousFixedTime = TimeWarp.fixedDeltaTime;

            MaintainContainment();

            ExplodeContainer();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_AntimatterStorageTank_SelfDestruct", active = true)]//Self Destruct
        public void SelfDestruct()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
                return;

            if (HighLogic.LoadedSceneIsEditor || antimatterResource.amount <= _minimumAntimatterAmount) return;

            DoExplode();//"Antimatter container exploded because self destruct was activated"
        }

        private void MaintainContainment()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null)
                return;

            if (chargeStatus > 0 && antimatterResource.amount > _minimumAntimatterAmount)
            {
                // chargeStatus is in seconds
                chargeStatus -= vessel.packed ? 0.05f : TimeWarp.fixedDeltaTime;
            }

            if (!_shouldCharge && antimatterResource.amount <= _minimumAntimatterAmount) return;

            var powerModifier = canExplodeFromGeeForce
                ? (resourceRatio * (currentGeeForce / 10) * 0.8) + ((part.temperature / 1000) * 0.2)
                :  Math.Pow(resourceRatio, 2);

            effectivePowerNeeded = chargeNeeded * powerModifier;

            if (effectivePowerNeeded > 0.0)
            {
                double powerRequest = (chargeStatus >= maxCharge ? 1.0 : 2.0) *
                    effectivePowerNeeded * TimeWarp.fixedDeltaTime;
                double chargeToAdd = consumeMegawatts(powerRequest / GameConstants.ecPerMJ,
                    true, true, true) * GameConstants.ecPerMJ / effectivePowerNeeded;
                chargeStatus += chargeToAdd;

                if (chargeToAdd >= TimeWarp.fixedDeltaTime)
                    _charging = true;
                else
                {
                    _charging = false;
                    if (TimeWarp.CurrentRateIndex > 3 && (antimatterResource.amount > _minimumAntimatterAmount))
                    {
                        TimeWarp.SetRate(3, true);
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg7", TimeWarp.CurrentRate,antimatterResource.resourceName), 1, ScreenMessageStyle.UPPER_CENTER);//"Cannot Time Warp faster than " +  + "x while " +  + " Tank is Unpowered"
                    }
                }
            }

            if (_startupTimeout > 0)
                _startupTimeout--;

            if (_startupTimeout == 0 && antimatterResource.amount > _minimumAntimatterAmount)
            {
                //verify temperature
                if (!CheatOptions.IgnoreMaxTemperature &&  canExplodeFromHeat && part.temperature > (double)(decimal)maxTemperature)
                {
                    _temperatureExplodeCounter++;
                    if (_temperatureExplodeCounter > 20)
                        DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg8"));//"Antimatter container exploded due to reaching critical temperature"
                }
                else
                    _temperatureExplodeCounter = 0;

                //verify geeforce
                _effectiveMaxGeeforce = resourceRatio > 0 ? Math.Min(10, maxGeeforce / resourceRatio) : 10;
                if (!CheatOptions.UnbreakableJoints && canExplodeFromGeeForce)
                {
                    if (vessel.missionTime > 0)
                    {
                        if (currentGeeForce > _effectiveMaxGeeforce)
                        {
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg9"), 1, ScreenMessageStyle.UPPER_CENTER);//"ALERT: geeforce at critical!"
                            _geeforceExplodeCounter++;
                            if (_geeforceExplodeCounter > 30)
                                DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg10"));//"Antimatter container exploded due to reaching critical geeforce"
                        }
                        else if (TimeWarp.CurrentRateIndex > maximumTimewarpWithGeeforceWarning && currentGeeForce > _effectiveMaxGeeforce - 0.02)
                        {
                            TimeWarp.SetRate(maximumTimewarpWithGeeforceWarning, true);
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg11", TimeWarp.CurrentRate), 1, ScreenMessageStyle.UPPER_CENTER);//"ALERT: Cannot Time Warp faster than " +  + "x while geeforce near maximum tolerance!"
                        }
                        else if (currentGeeForce > _effectiveMaxGeeforce - 0.04)
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg12", (currentGeeForce / _effectiveMaxGeeforce * 100).ToString("F2")), 1, ScreenMessageStyle.UPPER_CENTER);//"ALERT: geeforce at " +  + "%  tolerance!"
                        else
                            _geeforceExplodeCounter = 0;
                    }
                    else if (currentGeeForce > _effectiveMaxGeeforce)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg13"), 1, ScreenMessageStyle.UPPER_CENTER);//"Warning: geeforce tolerance exceeded but sustanable while the mission timer has not started"
                }
                else
                    _geeforceExplodeCounter = 0;

                //verify power
                if (chargeStatus <= 0)
                {
                    chargeStatus = 0;
                    if (!CheatOptions.InfiniteElectricity && antimatterResource.amount > 0.00001 * antimatterResource.maxAmount)
                    {
                        _powerExplodeCounter++;
                        if (_powerExplodeCounter > 20)
                            DoExplode(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Postmsg14"));//"Antimatter container exploded due to running out of power"
                    }
                }
                else
                    _powerExplodeCounter = 0;
            }
            else
            {
                _effectiveMaxGeeforce = 0;
                _temperatureExplodeCounter = 0;
                _geeforceExplodeCounter = 0;
                _powerExplodeCounter = 0;
            }

            if (chargeStatus > maxCharge)
                chargeStatus = maxCharge;
        }

        private void ExplodeContainer()
        {
            var antimatterResource = part.Resources[resourceName];
            if (antimatterResource == null || antimatterResource.resourceName != resourceName)
                return;

            if (!exploding || _lightGameObject == null) return;

            _explosionSize = Mathf.Sqrt((float)antimatterResource.amount) * 5;

            _curExplosionSize += TimeWarp.fixedDeltaTime * _explosionSize * _explosionSize / _explosionTime;
            _lightGameObject.transform.localScale = new Vector3(Mathf.Sqrt(_curExplosionSize), Mathf.Sqrt(_curExplosionSize), Mathf.Sqrt(_curExplosionSize));
            _lightGameObject.GetComponent<Light>().range = Mathf.Sqrt(_curExplosionSize) * 15f;
            _lightGameObject.GetComponent<Collider>().enabled = false;

            TimeWarp.SetRate(0, true);
            vessel.GoOffRails();

            var listOfVesselsToExplode = FlightGlobals.Vessels.ToArray();
            foreach (var vesselToExplode in listOfVesselsToExplode)
            {
                if (Vector3d.Distance(vesselToExplode.transform.position, vessel.transform.position) > _explosionSize) continue;

                if (vesselToExplode.packed) continue;

                var partsToExplode = vesselToExplode.Parts.ToArray();
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

            part.explode();
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Getinfo1") + (chargeNeeded * 2).ToString("0") + " KW");//"Maximum Power Requirements: "
            if (canExplodeFromGeeForce)
                info.AppendLine(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Getinfo2"));//"Maximum Geeforce: 10 G"
            if (canExplodeFromHeat)
                info.AppendLine(Localizer.Format("#LOC_KSPIE_AntimatterStorageTank_Getinfo3"));//"Maximum Temperature: 1000 K"

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
