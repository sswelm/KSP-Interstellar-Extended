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
    class FNGenerator : FNResourceSuppliableModule, IUpgradeableModule, IElectricPowerSource, IPartMassModifier, IRescalable<FNGenerator>
    {
        [KSPField(isPersistant = true, guiActive = true)]
        public bool IsEnabled = true;
        [KSPField(isPersistant = true)]
        public bool generatorInit = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool chargedParticleMode = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Control"), UI_FloatRange(stepIncrement = 5f, maxValue = 100f, minValue = 5f)]
        public float powerPercentage = 100;

        // Persistent False
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
        public float rawPowerToMassDivider = 1000f;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float massModifier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Reactor Raw Power", guiFormat = "F4")]
        public double rawMaximumPower;

        // Debugging
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Stored Mass")]
        public float storedMassMultiplier;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Part Mass", guiUnits = " t")]
        public float partMass;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Target Mass", guiUnits = " t")]
        public float targetMass;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Initial Mass", guiUnits = " t")]
        public float initialMass;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Delta Mass", guiUnits = " t")]
        public float moduleMassDelta;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Charged Power", guiUnits = " MW", guiFormat = "F4")]
        public double maxChargedPower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Thermal Power", guiUnits = " MW", guiFormat = "F4")]
        public double maxThermalPower;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Type")]
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
        [KSPField(isPersistant = false, guiActive = false, guiName = "Requested Power", guiUnits = " MW", guiFormat = "F2")]
        public double requestedPower_f;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Cold Bath Temp", guiUnits = "K",  guiFormat = "F2")]
        public double coldBathTemp = 500;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Hot Bath Temp", guiUnits = "K", guiFormat = "F2")]
        public double hotBathTemp = 300;

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
        protected IThermalSource attachedThermalSource;
        protected Animation anim;
        protected Queue<double> averageRadiatorTemperatureQueue = new Queue<double>();


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
                Debug.LogError("[KSPI] - FNGenerator.OnRescale" + e.Message);
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

            moduleMassDelta = targetMass - initialMass;

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
            FindAndAttachToThermalSource();
        }

        public override void OnStart(PartModule.StartState state)
        {
            //print("[KSP Interstellar]  Generator OnStart Begin " + startcount);

            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES, FNResourceManager.FNRESOURCE_WASTEHEAT };
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
                wasteheatPowerResource.maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;
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

                FindAndAttachToThermalSource();
                return;
            }

            if (this.HasTechsRequiredToUpgrade())
                hasrequiredupgrade = true;

            // only force activate if no certain partmodules are not present
            if (part.FindModuleImplementing<MicrowavePowerReceiver>() == null)
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

            FindAndAttachToThermalSource();

            UpdateHeatExchangedThrustDivisor();
        }

        /// <summary>
        /// Finds the nearest avialable thermalsource and update effective part mass
        /// </summary>
        public void FindAndAttachToThermalSource()
        {
            partDistance = 0;

            // disconnect
            if (attachedThermalSource != null)
            {
                if (chargedParticleMode)
                    attachedThermalSource.ConnectedChargedParticleElectricGenerator = null;
                else
                    attachedThermalSource.ConnectedThermalElectricGenerator = null;
            }

            // first look if part contains an thermal source
            attachedThermalSource = part.FindModulesImplementing<IThermalSource>().FirstOrDefault();
            if (attachedThermalSource != null)
                return;

            // otherwise look for other non selfcontained thermal sources
            var searchResult = ThermalSourceSearchResult.BreadthFirstSearchForThermalSource(part, p => p.IsThermalSource && p.ThermalEnergyEfficiency > 0 , 3, 3, 3, 3, true);
            if (searchResult == null) return;

            // verify cost is not higher than 1
            partDistance = (int)Math.Max(Math.Ceiling(searchResult.Cost) - 1, 0);
            if (partDistance > 0) return;

            // update attached thermalsource
            attachedThermalSource = searchResult.Source;

            //connect with source
            if (chargedParticleMode)
                attachedThermalSource.ConnectedChargedParticleElectricGenerator = this;
            else
                attachedThermalSource.ConnectedThermalElectricGenerator = this;

            UpdateTargetMass();
        }

        private void UpdateTargetMass()
        {
            // verify if mass calculation is active
            if (!calculatedMass)
                return;

            // update part mass

            if (attachedThermalSource.RawMaximumPower > 0 && rawPowerToMassDivider > 0)
            {
                rawMaximumPower = attachedThermalSource.RawMaximumPower;
                targetMass = (float)((massModifier * attachedThermalSource.ThermalProcessingModifier * rawMaximumPower) / rawPowerToMassDivider);
            }
            else
            {
                Debug.Log("targetMass = partmass = " + part.mass);
                targetMass = part.mass;
            }
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
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
                    OutputPower = getPowerFormatString(outputPowerReport) + "_e";
                    OverallEfficiency = percentOutputPower.ToString("0.00") + "%";

                    MaxPowerStr = (_totalEff >= 0)
                        ? !chargedParticleMode
                            ? getPowerFormatString(maxThermalPower * _totalEff * powerCustomSettingFraction) + "_e"
                            : getPowerFormatString(maxChargedPower * _totalEff * powerCustomSettingFraction) + "_e"
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

        public IThermalSource getThermalSource() { return attachedThermalSource; }

        public double MaxStableMegaWattPower
        {
            get
            {
                return attachedThermalSource != null && IsEnabled
                    ? chargedParticleMode
                        ? attachedThermalSource.StableMaximumReactorPower * 0.85
                        : attachedThermalSource.StableMaximumReactorPower * pCarnotEff
                    : 0;
            }
        }

        private void UpdateHeatExchangedThrustDivisor()
        {
            if (attachedThermalSource == null) return;

            if (attachedThermalSource.GetRadius() <= 0 || radius <= 0)
            {
                heat_exchanger_thrust_divisor = 1;
                return;
            }

            heat_exchanger_thrust_divisor = radius > attachedThermalSource.GetRadius()
                ? attachedThermalSource.GetRadius() * attachedThermalSource.GetRadius() / radius / radius
                : radius * radius / attachedThermalSource.GetRadius() / attachedThermalSource.GetRadius();
        }

        

        public void UpdateGeneratorPower()
        {
            if (attachedThermalSource == null) return;

            if (!chargedParticleMode)
            {
                hotBathTemp = attachedThermalSource.HotBathTemperature;  
                averageRadiatorTemperatureQueue.Enqueue(FNRadiator.getAverageRadiatorTemperatureForVessel(vessel) * 0.75);
                coldBathTemp = averageRadiatorTemperatureQueue.Sum() / averageRadiatorTemperatureQueue.Count;

                int targetBufferLength = (int)Math.Ceiling(Math.Pow(TimeWarp.fixedDeltaTime, 0.5)) + 1;

                while (averageRadiatorTemperatureQueue.Count >= targetBufferLength)
                {
                    averageRadiatorTemperatureQueue.Dequeue();
                }
            }

            if (HighLogic.LoadedSceneIsEditor)
                UpdateHeatExchangedThrustDivisor();

            maxThermalPower = attachedThermalSource.MaximumThermalPower * powerCustomSettingFraction;
            if (attachedThermalSource.EfficencyConnectedChargedEnergyGenerator == 0)
                maxThermalPower += attachedThermalSource.MaximumChargedPower;

            maxChargedPower = attachedThermalSource.MaximumChargedPower;
        }



        /// <summary>
        /// FixedUpdate is also called when not activated
        /// </summary>
        public void FixedUpdate()
        {
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight) return;

            Fields["targetMass"].guiActive = attachedThermalSource != null && attachedThermalSource.Part != this.part ;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            powerCustomSettingFraction = powerPercentage / 100;

            if (IsEnabled && attachedThermalSource != null && FNRadiator.hasRadiatorsForVessel(vessel))
            {
                UpdateGeneratorPower();

                // check if MaxStableMegaWattPower is changed
                maxStableMegaWattPower = fullPowerBuffer && attachedThermalSource != null 
                    ? attachedThermalSource.StableMaximumReactorPower 
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

                    _totalEff = Math.Min(pCarnotEff, carnotEff * pCarnotEff * attachedThermalSource.ThermalEnergyEfficiency);

                    if (_totalEff <= 0.01 || coldBathTemp <= 0 || hotBathTemp <= 0 || maxThermalPower <= 0)
                    {
                        requestedPower_f = 0;
                        //electricdtps = 0;
                        //max_electricdtps = 0;
                        //attachedThermalSource.RequestedThermalHeat = 0;
                        return;
                    }

                    attachedThermalSource.NotifyActiveThermalEnergyGenerator(_totalEff, ElectricGeneratorType.thermal);

                    double thermal_power_currently_needed = CalculateElectricalPowerCurrentlyNeeded();

                    double thermal_power_requested_fixed = Math.Max(Math.Min(maxThermalPower, thermal_power_currently_needed / _totalEff) * TimeWarp.fixedDeltaTime, 0);

                    requestedPower_f = thermal_power_requested_fixed / TimeWarp.fixedDeltaTime;

                    attachedThermalSource.RequestedThermalHeat = thermal_power_requested_fixed / TimeWarp.fixedDeltaTime;
                    double input_power = consumeFNResource(thermal_power_requested_fixed, FNResourceManager.FNRESOURCE_THERMALPOWER);

                    if (!(attachedThermalSource.EfficencyConnectedChargedEnergyGenerator > 0) && input_power < thermal_power_requested_fixed)
                        input_power += consumeFNResource(thermal_power_requested_fixed - input_power, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                    var effective_input_power = input_power * _totalEff;

                    if (!CheatOptions.IgnoreMaxTemperature)
                        consumeFNResource(effective_input_power, FNResourceManager.FNRESOURCE_WASTEHEAT);

                    electricdtps = Math.Max(effective_input_power / TimeWarp.fixedDeltaTime, 0.0);
                    max_electricdtps = maxThermalPower * _totalEff * powerCustomSettingFraction;
                }
                else // charged particle mode
                {
                    _totalEff = isupgraded ? upgradedDirectConversionEff : directConversionEff;

                    attachedThermalSource.NotifyActiveChargedEnergyGenerator(_totalEff, ElectricGeneratorType.charged_particle);

                    if (_totalEff <= 0) return;

                    double charged_power_currently_needed = CalculateElectricalPowerCurrentlyNeeded();

                    //var minimumPowerRequirement = maxChargedPower * _totalEff * attachedThermalSource.MinimumThrottle;

                    var charged_power_requested = Math.Max(Math.Min(maxChargedPower, charged_power_currently_needed / _totalEff) * TimeWarp.fixedDeltaTime, 0);

                    requestedPower_f = charged_power_requested / TimeWarp.fixedDeltaTime;

                    double input_power = consumeFNResource(charged_power_requested, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                    var effective_input_power = input_power * _totalEff;

                    if (!CheatOptions.IgnoreMaxTemperature)
                        consumeFNResource(effective_input_power, FNResourceManager.FNRESOURCE_WASTEHEAT);

                    electricdtps = Math.Max(effective_input_power / TimeWarp.fixedDeltaTime, 0.0);
                    max_electricdtps = maxChargedPower * _totalEff * powerCustomSettingFraction;
                }
                outputPower = -supplyFNResourceFixedMax(electricdtps * TimeWarp.fixedDeltaTime, max_electricdtps * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
            }
            else
            {
                if (attachedThermalSource != null)
                    attachedThermalSource.RequestedThermalHeat = 0;

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

                    if (attachedThermalSource == null)
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

                var megaWattBufferingBonus = attachedThermalSource.PowerBufferBonus * maxStableMegaWattPower;
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
            double electrical_power_currently_needed;

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

            var possibleSpareResourceCapacityFilling = Math.Min(getSpareResourceCapacity(FNResourceManager.FNRESOURCE_MEGAJOULES) * scaledPowerBufferRatio, MaxStableMegaWattPower * scaledPowerBufferRatio);

            if (attachedThermalSource.ShouldApplyBalance(chargedParticleMode ? ElectricGeneratorType.charged_particle : ElectricGeneratorType.thermal))
            {
                var chargedPowerPerformance = attachedThermalSource.EfficencyConnectedChargedEnergyGenerator * attachedThermalSource.ChargedPowerRatio;
                var thermalPowerPerformance = attachedThermalSource.EfficencyConnectedThermalEnergyGenerator * (1 - attachedThermalSource.ChargedPowerRatio);

                var totalPerformance = chargedPowerPerformance + thermalPowerPerformance;

                var balancePerformanceRatio = chargedParticleMode
                    ? chargedPowerPerformance / totalPerformance
                    : thermalPowerPerformance / totalPerformance;

                electrical_power_currently_needed = (getCurrentUnfilledResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) + possibleSpareResourceCapacityFilling) * balancePerformanceRatio;
            }
            else
                electrical_power_currently_needed = getCurrentUnfilledResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) + possibleSpareResourceCapacityFilling;

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

        public static string getPowerFormatString(double power)
        {
            if (power > 1000)
            {
                if (power > 20000)
                    return (power / 1000).ToString("0.0") + " GW";
                else
                    return (power / 1000).ToString("0.00") + " GW";
            }
            else
            {
                if (power > 20)
                    return power.ToString("0.0") + " MW";
                else
                {
                    if (power > 1)
                        return power.ToString("0.00") + " MW";
                    else
                        return (power * 1000).ToString("0.0") + " KW";
                }
            }
        }

    }
}

