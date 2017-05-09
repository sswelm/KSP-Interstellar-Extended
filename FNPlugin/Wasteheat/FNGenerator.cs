using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;
using FNPlugin.Extensions;

namespace FNPlugin
{
    enum PowerStates { powerChange, powerOnline, powerDown, powerOffline };

    [KSPModule("Super Capacitator")]
    class KspiSuperCapacitator : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Max Capacity", guiUnits = " MWe")]
        public float maxStorageCapacityMJ = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;
    }

    [KSPModule("Electrical Generator")]
    class FNGenerator : FNResourceSuppliableModule, IUpgradeableModule, IElectricPowerGeneratorSource, IPartMassModifier, IRescalable<FNGenerator>
    {
        [KSPField(isPersistant = true, guiActive = true)]
        public bool IsEnabled = true;
        [KSPField(isPersistant = true)]
        public bool generatorInit = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool chargedParticleMode = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Control"), UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        public float powerPercentage = 100;

        // Persistent False
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Is MHD")]
        public bool isMHD = false;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Is Limited by min throtle")]
        public bool isLimitedByMinThrotle = false;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public bool calculatedMass = false;
        [KSPField(isPersistant = false)]
        public float pCarnotEff = 0.32f;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradedpCarnotEff = 0.64f;
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radius")]
        public float radius;
        [KSPField(isPersistant = false)]
        public string altUpgradedName;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float directConversionEff = 0.5f;
        [KSPField(isPersistant = false)]
        public float upgradedDirectConversionEff = 0.865f;
        [KSPField(isPersistant = false)]
        public double carnotEff;
        [KSPField(isPersistant = false)]
        public bool maintainsMegaWattPowerBuffer = true;
        [KSPField(isPersistant = false)]
        public bool fullPowerBuffer = false;
        [KSPField(isPersistant = false)]
        public bool showSpecialisedUI = true;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Stable Power")]
        public double maxStableMegaWattPower;

        /// <summary>
        /// MW Power to part mass divider, need to be lower for SETI/NFE mode 
        /// </summary>
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public double rawPowerToMassDivider = 1000;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public double massModifier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Reactor Raw Power", guiFormat = "F4")]
        public double rawMaximumPower;

        // Debugging
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Stored Mass")]
        public float storedMassMultiplier;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Part Mass", guiUnits = " t", guiFormat = "F3")]
        public float partMass;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Target Mass", guiUnits = " t")]
        public double targetMass;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Initial Mass", guiUnits = " t")]
        public float initialMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Delta Mass", guiUnits = " t")]
        public float moduleMassDelta;

        // GUI
        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Charged Power", guiUnits = " MW", guiFormat = "F4")]
        public double maxChargedPower;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Thermal Power", guiUnits = " MW", guiFormat = "F4")]
        public double maxThermalPower;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Reactor Power", guiUnits = " MW", guiFormat = "F4")]
        public double maxReactorPower;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Type")]
        public string generatorType;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Power")]
        public string OutputPower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Power")]
        public string MaxPowerStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
        public string OverallEfficiency;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Upgrade Cost")]
        public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Required Capacity", guiUnits = " MW_e")]
        public double requiredMegawattCapacity;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Heat Exchange Divisor")]
        public float heat_exchanger_thrust_divisor;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Requested Power", guiUnits = " MJ", guiFormat = "F3")]
        public double requested_power_per_second;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Cold Bath Temp", guiUnits = "K",  guiFormat = "F3")]
        public double coldBathTemp = 500;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Hot Bath Temp", guiUnits = "K", guiFormat = "F3")]
        public double hotBathTemp = 300;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Spare Fill Cap", guiUnits = " MW", guiFormat = "F3")]
        public double possibleSpareResourceCapacityFilling;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Current Demand", guiUnits = " MW", guiFormat = "F3")]
        public double currentUnfilledResourceDemand;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power Needed", guiUnits = " MW", guiFormat = "F3")]
        public double electrical_power_currently_needed;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Applies Balance")]
        public bool applies_balance;

        // Internal
        protected double outputPower;
        protected double _totalEff;
        protected double powerDownFraction;

        protected bool play_down = true;
        protected bool play_up = true;
        protected bool hasrequiredupgrade = false;

        protected long last_draw_update = 0;
        protected long update_count = 0;

        protected int partDistance;
        protected int shutdown_counter = 0;
        protected int startcount = 0;
        
        protected float powerCustomSettingFraction;
        protected double _previousMaxStableMegaWattPower;
        protected float previousDeltaTime;

        protected PartResource megajouleResource;
        protected PowerStates _powerState;

        protected Animation anim;
        protected Queue<double> averageRadiatorTemperatureQueue = new Queue<double>();

        protected IPowerSource attachedPowerSource;

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        [KSPEvent(guiActive = true, guiName = "Activate Generator", active = true)]
        public void ActivateGenerator()
        {
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Generator", active = false)]
        public void DeactivateGenerator()
        {
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitGenerator()
        {
            if (ResearchAndDevelopment.Instance == null) return;

            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        [KSPAction("Activate Generator")]
        public void ActivateGeneratorAction(KSPActionParam param)
        {
            ActivateGenerator();
        }

        [KSPAction("Deactivate Generator")]
        public void DeactivateGeneratorAction(KSPActionParam param)
        {
            DeactivateGenerator();
        }

        [KSPAction("Toggle Generator")]
        public void ToggleGeneratorAction(KSPActionParam param)
        {
            IsEnabled = !IsEnabled;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("FNGenerator.OnRescale called with " + factor.absolute.linear);
                storedMassMultiplier = Mathf.Pow(factor.absolute.linear, 3);
                initialMass = part.prefabMass * storedMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnRescale " + e.Message);
            }
        }

        public void Refresh()
        {
            Debug.Log("FNGenerator Refreshed" );
            UpdateTargetMass();
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (!calculatedMass)
                return 0;

            moduleMassDelta = (float)targetMass - initialMass;

            return moduleMassDelta;
        }

        public void upgradePartModule()
        {
            isupgraded = true;
            pCarnotEff = upgradedpCarnotEff;
            directConversionEff = this.upgradedDirectConversionEff;
            generatorType = chargedParticleMode ? altUpgradedName : upgradedName;
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI] - attach " + part.partInfo.title);
                FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnEditorAttach " + e.Message);
            }
        }

        /// <summary>
        /// Event handler which is called when part is deptach to a exiting part
        /// </summary>
        private void OnEditorDetach()
        {
            try
            {
                Debug.Log("[KSPI] - detach " + part.partInfo.title);
                if (chargedParticleMode && attachedPowerSource.ConnectedChargedParticleElectricGenerator != null)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                if (!chargedParticleMode && attachedPowerSource.ConnectedThermalElectricGenerator != null)
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnEditorDetach " + e.Message);
            }
        }

        private void OnDestroyed()
        {
            try
            {
                OnEditorDetach();

                RemoveItselfAsManager();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnDestroyed " + e.Message);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_THERMALPOWER, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES };
            this.resources_to_supply = resources_to_supply;

            if (state == PartModule.StartState.Docked)
            {
                base.OnStart(state);
                return;
            }

            // calculate WasteHeat Capacity
            if (maintainsMegaWattPowerBuffer)
            {
                var wasteheatPowerResource = part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT];
                var ratio = wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount;
                wasteheatPowerResource.maxAmount = part.mass * 1.0e+3 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * ratio;
            }

            previousDeltaTime = TimeWarp.fixedDeltaTime - 1.0e-6f;
            megajouleResource = part.Resources[FNResourceManager.FNRESOURCE_MEGAJOULES];

            base.OnStart(state);
            generatorType = originalName;

            targetMass = part.prefabMass * storedMassMultiplier;
            initialMass = part.prefabMass * storedMassMultiplier;

            if (initialMass == 0)
                initialMass = part.prefabMass;
            if (targetMass == 0)
                targetMass = part.prefabMass;

            Fields["partMass"].guiActive = Fields["partMass"].guiActiveEditor = calculatedMass;

            Fields["maxChargedPower"].guiActive = chargedParticleMode;
            Fields["maxThermalPower"].guiActive = !chargedParticleMode;

            Fields["powerPercentage"].guiActive = Fields["powerPercentage"].guiActiveEditor = showSpecialisedUI;
            Fields["generatorType"].guiActive = Fields["generatorType"].guiActiveEditor = showSpecialisedUI;
            //Fields["massModifier"].guiActive = Fields["massModifier"].guiActiveEditor = showSpecialisedUI;
            Fields["radius"].guiActive = Fields["radius"].guiActiveEditor = showSpecialisedUI;
            //Fields["rawPowerToMassDivider"].guiActive = Fields["rawPowerToMassDivider"].guiActiveEditor =  showSpecialisedUI;

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    hasrequiredupgrade = true;
                    upgradePartModule();
                }
                part.OnEditorAttach += OnEditorAttach;

                part.OnEditorDetach += OnEditorDetach;
                part.OnEditorDestroy += OnEditorDetach;

                part.OnJustAboutToBeDestroyed += OnDestroyed;
                part.OnJustAboutToDie += OnDestroyed;

                FindAndAttachToPowerSource();
                return;
            }

            if (this.HasTechsRequiredToUpgrade())
                hasrequiredupgrade = true;

            // only force activate if no certain partmodules are not present and not limited by minimum throtle
            if (!isLimitedByMinThrotle && part.FindModuleImplementing<MicrowavePowerReceiver>() == null)
            {
                Debug.Log("[WarpPlugin] Generator Force Activated");
                this.part.force_activate();
            }

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null)
            {
                anim[animName].layer = 1;
                if (!IsEnabled)
                {
                    anim[animName].normalizedTime = 1f;
                    anim[animName].speed = -1f;
                }
                else
                {
                    anim[animName].normalizedTime = 0f;
                    anim[animName].speed = 1f;
                }
                anim.Play();
            }

            if (generatorInit == false)
            {
                generatorInit = true;
                IsEnabled = true;
            }

            if (isupgraded)
                upgradePartModule();

            FindAndAttachToPowerSource();

            UpdateHeatExchangedThrustDivisor();
        }

        /// <summary>
        /// Finds the nearest avialable thermalsource and update effective part mass
        /// </summary>
        public void FindAndAttachToPowerSource()
        {
            partDistance = 0;

            // disconnect
            if (attachedPowerSource != null)
            {
                if (chargedParticleMode)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                else
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }

            // first look if part contains an thermal source
            attachedPowerSource = part.FindModulesImplementing<IPowerSource>().FirstOrDefault();
            if (attachedPowerSource != null)
            {
                ConnectToPowerSource();
                Debug.Log("[KSPI] - Found power source localy");
                return;
            }

            if (!part.attachNodes.Any() || part.attachNodes.All(m => m.attachedPart == null))
            {
                Debug.Log("[KSPI] - not connected to any parts yet");
                UpdateTargetMass();
                return;
            }

            Debug.Log("[KSPI] - generator is currently connected to " + part.attachNodes.Count  + " parts");
            // otherwise look for other non selfcontained thermal sources that is not already connected
            PowerSourceSearchResult searchResult;
            if (chargedParticleMode)
                searchResult = FindChargedParticleSource();
            else
                searchResult = FindThermalPowerSource();

            // quit if we failed to find anything
            if (searchResult == null)
            {
                Debug.LogWarning("[KSPI] - Failed to find power source");
                return;
            }

            // verify cost is not higher than 1
            partDistance = (int)Math.Max(Math.Ceiling(searchResult.Cost), 0);
            if (partDistance > 1)
            {
                Debug.LogWarning("[KSPI] - Found power source but at too high cost");
                return;
            }

            // update attached thermalsource
            attachedPowerSource = searchResult.Source;

            Debug.Log("[KSPI] - succesfully connected to " + attachedPowerSource.Part.partInfo.title);

            ConnectToPowerSource();
        }

        private void ConnectToPowerSource()
        {
            //connect with source
            if (chargedParticleMode)
                attachedPowerSource.ConnectedChargedParticleElectricGenerator = this;
            else
                attachedPowerSource.ConnectedThermalElectricGenerator = this;

            UpdateTargetMass();
        }

        private PowerSourceSearchResult FindThermalPowerSource()
        {
            PowerSourceSearchResult searchResult = 
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                    p => p.IsThermalSource
                        && p.ConnectedThermalElectricGenerator == null
                        && p.ThermalEnergyEfficiency > 0, 3, 3, 3, true);
            return searchResult;
        }

        private PowerSourceSearchResult FindChargedParticleSource()
        {
            PowerSourceSearchResult searchResult = 
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                     p => p.IsThermalSource
                         && p.ConnectedChargedParticleElectricGenerator == null
                         && p.ChargedParticleEnergyEfficiency > 0, 3, 3, 3, true);
            return searchResult;
        }

        private void UpdateTargetMass()
        {
            try
            {
                if (attachedPowerSource == null)
                {
                    targetMass = initialMass;
                    return;
                }

                // verify if mass calculation is active
                if (!calculatedMass)
                {
                    targetMass = initialMass;
                    return;
                }

                // update part mass
                rawMaximumPower = attachedPowerSource.RawMaximumPower;
                if (rawMaximumPower > 0 && rawPowerToMassDivider > 0)
                    targetMass = (massModifier * attachedPowerSource.ThermalProcessingModifier * rawMaximumPower) / rawPowerToMassDivider;
                else
                    targetMass = initialMass;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.UpdateTargetMass " + e.Message);
            }
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            powerCustomSettingFraction = powerPercentage / 100;

            Events["ActivateGenerator"].active = !IsEnabled && showSpecialisedUI;
            Events["DeactivateGenerator"].active = IsEnabled && showSpecialisedUI;
            Fields["OverallEfficiency"].guiActive = IsEnabled;
            Fields["MaxPowerStr"].guiActive = IsEnabled;
            Fields["coldBathTemp"].guiActive = !chargedParticleMode;
            Fields["hotBathTemp"].guiActive = !chargedParticleMode;

            if (ResearchAndDevelopment.Instance != null)
            {
                Events["RetrofitGenerator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
            }
            else
                Events["RetrofitGenerator"].active = false;

            Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;

            if (IsEnabled)
            {
                if (play_up && anim != null)
                {
                    play_down = true;
                    play_up = false;
                    anim[animName].speed = 1f;
                    anim[animName].normalizedTime = 0f;
                    anim.Blend(animName, 2f);
                }
            }
            else
            {
                if (play_down && anim != null)
                {
                    play_down = false;
                    play_up = true;
                    anim[animName].speed = -1f;
                    anim[animName].normalizedTime = 1f;
                    anim.Blend(animName, 2f);
                }
            }

            if (IsEnabled)
            {
                double percentOutputPower = _totalEff * 100.0;
                double outputPowerReport = -outputPower;
                if (update_count - last_draw_update > 10)
                {
                    OutputPower = PluginHelper.getFormattedPowerString(outputPowerReport, "0.0", "0.000");
                    OverallEfficiency = percentOutputPower.ToString("0.00") + "%";

                    MaxPowerStr = (_totalEff >= 0)
                        ? !chargedParticleMode
                            ? PluginHelper.getFormattedPowerString(maxThermalPower * _totalEff * powerCustomSettingFraction, "0.0", "0.000")
                            : PluginHelper.getFormattedPowerString(maxChargedPower * _totalEff * powerCustomSettingFraction, "0.0", "0.000")
                        : (0).ToString() + "MW";

                    last_draw_update = update_count;
                }
            }
            else
                OutputPower = "Generator Offline";

            update_count++;
        }

        public double getMaxPowerOutput()
        {
            if (chargedParticleMode)
                return maxChargedPower * _totalEff;
            else
                return maxThermalPower * _totalEff;
        }


        public bool isActive() { return IsEnabled; }

        public IPowerSource getThermalSource() { return attachedPowerSource; }

        public double MaxStableMegaWattPower
        {
            get
            {
                return attachedPowerSource != null && IsEnabled
                    ? chargedParticleMode
                        ? attachedPowerSource.StableMaximumReactorPower * 0.85
                        : attachedPowerSource.StableMaximumReactorPower * pCarnotEff
                    : 0;
            }
        }

        private void UpdateHeatExchangedThrustDivisor()
        {
            if (attachedPowerSource == null) return;

            if (attachedPowerSource.GetRadius() <= 0 || radius <= 0)
            {
                heat_exchanger_thrust_divisor = 1;
                return;
            }

            heat_exchanger_thrust_divisor = radius > attachedPowerSource.GetRadius()
                ? attachedPowerSource.GetRadius() * attachedPowerSource.GetRadius() / radius / radius
                : radius * radius / attachedPowerSource.GetRadius() / attachedPowerSource.GetRadius();
        }

        public void UpdateGeneratorPower()
        {
            if (attachedPowerSource == null) return;

            if (!chargedParticleMode) // thermal mode
            {
                hotBathTemp = isMHD && attachedPowerSource.SupportMHD
                    ? Math.Pow(1 - getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 2) * attachedPowerSource.CoreTemperature 
                    : attachedPowerSource.HotBathTemperature;  

                averageRadiatorTemperatureQueue.Enqueue(FNRadiator.getAverageRadiatorTemperatureForVessel(vessel) * 0.75);
                coldBathTemp = averageRadiatorTemperatureQueue.Average();

                int targetBufferLength = (int)Math.Ceiling(Math.Pow(TimeWarp.fixedDeltaTime, 0.5)) + 1;

                while (averageRadiatorTemperatureQueue.Count >= targetBufferLength)
                {
                    averageRadiatorTemperatureQueue.Dequeue();
                }
            }

            if (HighLogic.LoadedSceneIsEditor)
                UpdateHeatExchangedThrustDivisor();

            maxThermalPower = isLimitedByMinThrotle 
                    ? attachedPowerSource.MinimumPower 
                    : attachedPowerSource.MaximumThermalPower * powerCustomSettingFraction;

            maxChargedPower = attachedPowerSource.MaximumChargedPower * powerCustomSettingFraction;
            maxReactorPower = maxThermalPower + maxChargedPower;
        }

        // Update is called in the editor 
        public void Update()
        {
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight) return;

            UpdateTargetMass();

            Fields["targetMass"].guiActive = attachedPowerSource != null && attachedPowerSource.Part != this.part;
        }

        /// <summary>
        /// FixedUpdate is called every frame also called when not activated
        /// </summary>
        public void FixedUpdate()
        {
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }

        public override void OnFixedUpdateResourceSuppliable(float fixedDeltaTime)
        {
            if (IsEnabled && attachedPowerSource != null && FNRadiator.hasRadiatorsForVessel(vessel))
            {
                UpdateGeneratorPower();

                // check if MaxStableMegaWattPower is changed
                maxStableMegaWattPower = fullPowerBuffer && attachedPowerSource != null
                    ? attachedPowerSource.StableMaximumReactorPower
                    : MaxStableMegaWattPower;

                if (maintainsMegaWattPowerBuffer)
                    UpdateMegaWattPowerBuffer(maxStableMegaWattPower);

                // don't produce any power when our reactor has stopped
                if (maxStableMegaWattPower <= 0)
                {
                    PowerDown();
                    return;
                }
                else
                    powerDownFraction = 1;

                double electricdtps = 0;
                double max_electricdtps = 0;

                if (!chargedParticleMode) // thermal mode
                {
                    carnotEff = Math.Max(Math.Min(1.0f - coldBathTemp / hotBathTemp, 1), 0);

                    _totalEff = Math.Min(pCarnotEff, carnotEff * pCarnotEff * attachedPowerSource.ThermalEnergyEfficiency);

                    if (_totalEff <= 0.01 || coldBathTemp <= 0 || hotBathTemp <= 0 || maxThermalPower <= 0)
                    {
                        requested_power_per_second = 0;
                        return;
                    }

                    attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEff, ElectricGeneratorType.thermal);

                    double thermal_power_currently_needed_per_second = CalculateElectricalPowerCurrentlyNeeded();

                    var effective_thermal_power_needed = thermal_power_currently_needed_per_second / _totalEff;

                    var adjusted_thermal_power_needed = applies_balance
                        ? effective_thermal_power_needed
                        : effective_thermal_power_needed * (1 - attachedPowerSource.ChargedPowerRatio);

                    double thermal_power_requested_per_second = Math.Max(Math.Min(maxThermalPower, adjusted_thermal_power_needed), attachedPowerSource.MinimumPower * (1 - attachedPowerSource.ChargedPowerRatio));
                    double reactor_power_requested_per_second = Math.Max(Math.Min(maxReactorPower, effective_thermal_power_needed), attachedPowerSource.MinimumPower);

                    requested_power_per_second = thermal_power_requested_per_second;

                    attachedPowerSource.RequestedThermalHeat = thermal_power_requested_per_second;

                    double thermal_power_received_per_second = consumeFNResourcePerSecond(thermal_power_requested_per_second, FNResourceManager.FNRESOURCE_THERMALPOWER);

                    if (attachedPowerSource.EfficencyConnectedChargedEnergyGenerator == 0 && thermal_power_received_per_second < reactor_power_requested_per_second && attachedPowerSource.ChargedPowerRatio > 0.001)
                    {
                        var requested_charged_power_per_second = Math.Min(reactor_power_requested_per_second - thermal_power_received_per_second, maxChargedPower);

                        if (requested_charged_power_per_second < 0.000025)
                            thermal_power_received_per_second += requested_charged_power_per_second;
                        else
                            thermal_power_received_per_second += consumeFNResourcePerSecond(requested_charged_power_per_second, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                    }

                    var effective_input_power_per_second = thermal_power_received_per_second * _totalEff;

                    if (!CheatOptions.IgnoreMaxTemperature)
                        consumeFNResourcePerSecond(effective_input_power_per_second, FNResourceManager.FNRESOURCE_WASTEHEAT);

                    electricdtps = Math.Max(effective_input_power_per_second, 0.0);

                    var effectiveMaxThermalPower = attachedPowerSource.EfficencyConnectedChargedEnergyGenerator == 0 ? maxReactorPower : maxThermalPower;

                    max_electricdtps = effectiveMaxThermalPower * _totalEff;
                }
                else // charged particle mode
                {
                    _totalEff = isupgraded ? upgradedDirectConversionEff : directConversionEff;

                    attachedPowerSource.NotifyActiveChargedEnergyGenerator(_totalEff, ElectricGeneratorType.charged_particle);

                    if (_totalEff <= 0) return;

                    double charged_power_currently_needed = CalculateElectricalPowerCurrentlyNeeded();

                    requested_power_per_second = Math.Max(Math.Min(maxChargedPower, charged_power_currently_needed / _totalEff), attachedPowerSource.MinimumPower * attachedPowerSource.ChargedPowerRatio);

                    double input_power = consumeFNResource(requested_power_per_second * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                    var effective_input_power = input_power * _totalEff;

                    if (!CheatOptions.IgnoreMaxTemperature)
                        consumeFNResource(effective_input_power, FNResourceManager.FNRESOURCE_WASTEHEAT);

                    electricdtps = Math.Max(effective_input_power / TimeWarp.fixedDeltaTime, 0.0);
                    max_electricdtps = maxChargedPower * _totalEff;
                }
                outputPower = -supplyFNResourcePerSecondWithMax(electricdtps, max_electricdtps, FNResourceManager.FNRESOURCE_MEGAJOULES);
            }
            else
            {
                if (attachedPowerSource != null)
                    attachedPowerSource.RequestedThermalHeat = 0;

                previousDeltaTime = TimeWarp.fixedDeltaTime;
                if (IsEnabled && !vessel.packed)
                {
                    if (!FNRadiator.hasRadiatorsForVessel(vessel))
                    {
                        IsEnabled = false;
                        Debug.Log("[WarpPlugin] Generator Shutdown: No radiators available!");
                        ScreenMessages.PostScreenMessage("Generator Shutdown: No radiators available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }

                    if (attachedPowerSource == null)
                    {
                        IsEnabled = false;
                        Debug.Log("[WarpPlugin] Generator Shutdown: No reactor available!");
                        ScreenMessages.PostScreenMessage("Generator Shutdown: No reactor available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }
                }
                else
                {
                    PowerDown();
                }
            }
        }

        private void UpdateMegaWattPowerBuffer(double maxStableMegaWattPower)
        {
            if (maxStableMegaWattPower != _previousMaxStableMegaWattPower)
                _powerState = PowerStates.powerChange;

            _previousMaxStableMegaWattPower = maxStableMegaWattPower;

            if (maxStableMegaWattPower > 0 && (TimeWarp.fixedDeltaTime != previousDeltaTime || _powerState != PowerStates.powerOnline))
            {
                _powerState = PowerStates.powerOnline;

                var megaWattBufferingBonus = attachedPowerSource.PowerBufferBonus * maxStableMegaWattPower;
                requiredMegawattCapacity = Math.Max(0.0001, TimeWarp.fixedDeltaTime * maxStableMegaWattPower + megaWattBufferingBonus);
                var previousMegawattCapacity = Math.Max(0.0001, previousDeltaTime * maxStableMegaWattPower + megaWattBufferingBonus);

                if (megajouleResource != null)
                {
                    var mageJouleRatio = megajouleResource.amount / megajouleResource.maxAmount;
                    megajouleResource.maxAmount = requiredMegawattCapacity;

                    megajouleResource.amount = requiredMegawattCapacity > previousMegawattCapacity
                        ? Math.Max(0, Math.Min(requiredMegawattCapacity, megajouleResource.amount + requiredMegawattCapacity - previousMegawattCapacity))
                        : Math.Max(0, Math.Min(requiredMegawattCapacity, mageJouleRatio * requiredMegawattCapacity));
                }

                if (part.Resources.Contains(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE))
                {
                    PartResource electricChargeResource = part.Resources[FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE];
                    var electricChargeRatio = electricChargeResource.amount / electricChargeResource.maxAmount;
                    electricChargeResource.maxAmount = requiredMegawattCapacity;

                    electricChargeResource.amount = requiredMegawattCapacity > previousMegawattCapacity
                        ? Math.Max(0, Math.Min(requiredMegawattCapacity, electricChargeResource.amount + requiredMegawattCapacity - previousMegawattCapacity))
                        : Math.Max(0, Math.Min(requiredMegawattCapacity, electricChargeRatio * requiredMegawattCapacity));
                }
            }
            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        private double CalculateElectricalPowerCurrentlyNeeded()
        {
            if (isLimitedByMinThrotle)
                return attachedPowerSource.MinimumPower;

            var powerBufferRatio = Math.Min( 1 - getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES), 1);

            double exponent;
            if (TimeWarp.fixedDeltaTime > 10000)
                exponent = 1;
            else if (TimeWarp.fixedDeltaTime > 100)
                exponent = 0.5;
            else if (TimeWarp.fixedDeltaTime > 1)
                exponent = 0.25;
            else if (TimeWarp.fixedDeltaTime > 0.1)
                exponent = 0.125;
            else
                exponent = 0;

            var scaledPowerBufferRatio = Math.Pow(powerBufferRatio, exponent);

            currentUnfilledResourceDemand = GetCurrentUnfilledResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES);

            possibleSpareResourceCapacityFilling = Math.Min(2 * getSpareResourceCapacity(FNResourceManager.FNRESOURCE_MEGAJOULES) * scaledPowerBufferRatio, 2 * MaxStableMegaWattPower * scaledPowerBufferRatio);

            applies_balance = attachedPowerSource.ShouldApplyBalance(chargedParticleMode ? ElectricGeneratorType.charged_particle : ElectricGeneratorType.thermal);

            if (applies_balance)
            {
                var chargedPowerPerformance = attachedPowerSource.EfficencyConnectedChargedEnergyGenerator * attachedPowerSource.ChargedPowerRatio;
                var thermalPowerPerformance = attachedPowerSource.EfficencyConnectedThermalEnergyGenerator * (1 - attachedPowerSource.ChargedPowerRatio);

                var totalPerformance = chargedPowerPerformance + thermalPowerPerformance;

                var balancePerformanceRatio = totalPerformance == 0 ? 0
                    : chargedParticleMode
                        ? chargedPowerPerformance / totalPerformance
                        : thermalPowerPerformance / totalPerformance;

                electrical_power_currently_needed = (currentUnfilledResourceDemand + possibleSpareResourceCapacityFilling) * balancePerformanceRatio;
            }
            else
            {
                electrical_power_currently_needed = currentUnfilledResourceDemand + possibleSpareResourceCapacityFilling;
            }

            return electrical_power_currently_needed;
        }

        private void PowerDown()
        {
            if (_powerState != PowerStates.powerOffline)
            {
                if (powerDownFraction <= 0)
                    _powerState = PowerStates.powerOffline;
                else
                    powerDownFraction -= 0.01;

                PartResource megajouleResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_MEGAJOULES);
                if (megajouleResource != null)
                {
                    megajouleResource.maxAmount = Math.Max(0.0001, megajouleResource.maxAmount * powerDownFraction);
                    megajouleResource.amount = Math.Min(megajouleResource.maxAmount, megajouleResource.amount);
                }

                PartResource electricChargeResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE);
                if (electricChargeResource != null)
                {
                    electricChargeResource.maxAmount = Math.Max(0.0001, electricChargeResource.maxAmount * powerDownFraction);
                    electricChargeResource.amount = Math.Min(electricChargeResource.maxAmount, electricChargeResource.amount);
                }
            }
            else
            {
                PartResource megajouleResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_MEGAJOULES);
                if (megajouleResource != null)
                {
                    megajouleResource.maxAmount = 0.0001;
                    megajouleResource.amount = 0;
                }

                PartResource electricChargeResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE);
                if (electricChargeResource != null)
                {
                    electricChargeResource.maxAmount = 0.0001;
                    electricChargeResource.amount = 0;
                }
            }
        }

        public override string GetInfo()
        {
            return String.Format("Percent of Carnot Efficiency: {0}%\n-Upgrade Information-\n Upgraded Percent of Carnot Efficiency: {1}%", pCarnotEff * 100, upgradedpCarnotEff * 100);
        }

        public override string getResourceManagerDisplayName()
        {
            var result = base.getResourceManagerDisplayName();
            if (isLimitedByMinThrotle)
                return result;

            if (attachedPowerSource != null && attachedPowerSource.Part != null)
                result += " (" + attachedPowerSource.Part.partInfo.title + ")";
            return result;
        }

        public override int getPowerPriority()
        {
            if (isLimitedByMinThrotle)
                return 1;

            if (attachedPowerSource == null)
                return base.getPowerPriority();

            return attachedPowerSource.ProviderPowerPriority;
        }
    }
}

