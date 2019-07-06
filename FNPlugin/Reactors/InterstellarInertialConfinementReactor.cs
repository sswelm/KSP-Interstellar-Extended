using System;
using FNPlugin.Extensions;

namespace FNPlugin.Reactors
{
    [KSPModule("Particle Accelerator")]
    class FNParticleAccelerator : InterstellarInertialConfinementReactor { }

    [KSPModule("Quantum Singularity Reactor")]
    class QuantumSingularityReactor : InterstellarInertialConfinementReactor { }

    [KSPModule("Confinement Fusion Reactor")]
    class IntegratedInertialConfinementReactor : InterstellarInertialConfinementReactor {}

    [KSPModule("Confinement Fusion Engine")]
    class IntegratedInertialConfinementEngine : InterstellarInertialConfinementReactor { }

    [KSPModule("Confinement Fusion Reactor")]
    class InertialConfinementReactor : InterstellarInertialConfinementReactor { }

    [KSPModule("Inertial Confinement Fusion Reactor")]
    class InterstellarInertialConfinementReactor : InterstellarFusionReactor
    {
        // Configs
        [KSPField(guiActiveEditor = true)]
        public string primaryInputResource = ResourceManager.FNRESOURCE_MEGAJOULES;
        [KSPField(guiActiveEditor = true)]
        public string secondaryInputResource = ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE;
        [KSPField]
        public double primaryInputMultiplier = 1;
        [KSPField]
        public double secondaryInputMultiplier = 1000;
        [KSPField]
        public bool canJumpstart = true;
        [KSPField]
        public bool usePowerManagerForPrimaryInputPower = true;
        [KSPField]
        public bool usePowerManagerForSecondaryInputPower = true;
        [KSPField]
        public bool canChargeJumpstart = true;
        [KSPField]
        public float startupPowerMultiplier = 1;
        [KSPField]
        public float startupCostGravityMultiplier = 0;
        [KSPField]
        public float startupCostGravityExponent = 1;
        [KSPField]
        public float startupMaximumGeforce = 10000;
        [KSPField]
        public float startupMinimumChargePercentage = 0;
        [KSPField]
        public double geeForceMaintenancePowerMultiplier = 0;

        // Persistant
        [KSPField(isPersistant = true)]
        public double accumulatedElectricChargeInMW;
        [KSPField(guiActiveEditor = true, guiName = "Power Affects Maintenance")]
        public bool powerControlAffectsMaintenance = true;
        [KSPField(isPersistant = true, guiName = "Startup"), UI_Toggle(disabledText = "Off", enabledText = "Charging")]
        public bool isChargingForJumpstart;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Max Secondary Power Usage"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 1)]
        public float maxSecondaryPowerUsage = 90;

        // UI Display
        [KSPField(guiActive = false, guiUnits = "%", guiName = "Minimum Throtle", guiFormat = "F2")]
        public double minimumThrottlePercentage;
        [KSPField(guiActive = true, guiName = "Charge")]
        public string accumulatedChargeStr = String.Empty;
        [KSPField(guiActive = false, guiName = "Fusion Power Requirement", guiFormat = "F2")]
        public double currentLaserPowerRequirements = 0;

        [KSPField(guiActive = false)]
        public double gravityDivider;

        double power_consumed;
        bool fusion_alert;
        int jumpstartPowerTime;
        double framesPlasmaRatioIsGood;

        BaseField isChargingField;
        BaseField accumulatedChargeStrField;
        PartResourceDefinition primaryInputResourceDefinition;
        PartResourceDefinition secondaryInputResourceDefinition;

        public override double PlasmaModifier
        {
            get { return plasma_ratio; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            isChargingField = Fields["isChargingForJumpstart"];
            accumulatedChargeStrField = Fields["accumulatedChargeStr"];

            isChargingField.guiActiveEditor = false;

            base.OnStart(state);

            if (state != StartState.Editor && allowJumpStart)
            {
                if (startDisabled)
                {
                    allowJumpStart = false;
                    IsEnabled = false;
                }
                else
                {
                    jumpstartPowerTime = 50;
                    IsEnabled = true;
                    reactor_power_ratio = 1;
                }

                UnityEngine.Debug.LogWarning("[KSPI]: InterstellarInertialConfinementReactor.OnStart allowJumpStart");
            }

            primaryInputResourceDefinition = !string.IsNullOrEmpty(primaryInputResource)
                ? PartResourceLibrary.Instance.GetDefinition(primaryInputResource)
                : null;

            secondaryInputResourceDefinition = !string.IsNullOrEmpty(secondaryInputResource)
                ? PartResourceLibrary.Instance.GetDefinition(secondaryInputResource)
                : null;
        }

        public override void StartReactor()
        {
            // instead of starting the reactor right away, we always first have to charge it
            isChargingForJumpstart = true;
        }

        public override double MinimumThrottle
        {
            get
            {
                var currentMinimumThrottle = (powerPercentage > 0 && base.MinimumThrottle > 0)
                    ? Math.Min(base.MinimumThrottle / PowerRatio, 1)
                    : base.MinimumThrottle;

                minimumThrottlePercentage = currentMinimumThrottle * 100;

                return currentMinimumThrottle;
            }
        }

        public double LaserPowerRequirements
        {
            get
            {
                currentLaserPowerRequirements =
                    CurrentFuelMode == null
                    ? PowerRequirement
                    : powerControlAffectsMaintenance
                        ? PowerRatio * NormalizedPowerRequirment
                        : NormalizedPowerRequirment;

                if (geeForceMaintenancePowerMultiplier > 0)
                    currentLaserPowerRequirements += Math.Abs(currentLaserPowerRequirements * geeForceMaintenancePowerMultiplier * part.vessel.geeForce);

                return currentLaserPowerRequirements * primaryInputMultiplier;
            }
        }

        public double GravityDivider
        {
            get { return startupCostGravityMultiplier * Math.Pow(FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude, startupCostGravityExponent); }
        }

        public double StartupPower
        {
            get
            {
                var startupPower = startupPowerMultiplier * LaserPowerRequirements;
                if (startupCostGravityMultiplier > 0)
                {
                    gravityDivider = GravityDivider;
                    startupPower = gravityDivider > 0 ? startupPower / gravityDivider : startupPower;
                }

                return startupPower;
            }
        }

        public override bool shouldScaleDownJetISP()
        {
            return !isupgraded;
        }

        public override void Update()
        {
            base.Update();

            isChargingField.guiActive = !IsEnabled && HighLogic.LoadedSceneIsFlight && canChargeJumpstart && part.vessel.geeForce < startupMaximumGeforce;
            isChargingField.guiActiveEditor = false;
        }

        public override void OnUpdate()
        {
            if (!CheatOptions.InfiniteElectricity && !isChargingForJumpstart && !isSwappingFuelMode && getCurrentResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES) && getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 && IsEnabled && !fusion_alert)
            {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            }
            else
                fusion_alert = false;

            if (isChargingField.guiActive)
            {
                accumulatedChargeStr = PluginHelper.getFormattedPowerString(accumulatedElectricChargeInMW, "0.0", "0.000")
                    + " / " + PluginHelper.getFormattedPowerString(StartupPower, "0.0", "0.000");
            }
            else if (part.vessel.geeForce > startupMaximumGeforce)
                accumulatedChargeStr = part.vessel.geeForce.ToString("0.000") + "g > " + startupMaximumGeforce + "g";
            else
                accumulatedChargeStr = String.Empty;

            accumulatedChargeStrField.guiActive = plasma_ratio < 1;

            electricPowerMaintenance = PluginHelper.getFormattedPowerString(power_consumed) + " / " + PluginHelper.getFormattedPowerString(LaserPowerRequirements);

            if (startupAnimation != null && !initialized)
            {
                if (IsEnabled)
                {
                    //animationScalar = startupAnimation.GetScalar;
                    if (animationStarted == 0)
                    {
                        startupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Activate));
                        animationStarted = Planetarium.GetUniversalTime();
                    }
                    else if (!startupAnimation.IsMoving())
                    {
                        startupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Deactivate));
                        animationStarted = 0;
                        initialized = true;
                        isDeployed = true;
                    }
                }
                else // Not Enabled
                {
                    // continiously start
                    startupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Activate));
                    startupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Deactivate));
                }
            }
            else if (startupAnimation == null)
            {
                isDeployed = true;
            }

            // call base class
            base.OnUpdate();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            UpdateLoopingAnimation(ongoing_consumption_rate * powerPercentage / 100);

            if (!IsEnabled && !isChargingForJumpstart)
            {
                plasma_ratio = 0;
                power_consumed = 0;
                allowJumpStart = false;
                if (accumulatedElectricChargeInMW > 0)
                    accumulatedElectricChargeInMW -= 0.01 * accumulatedElectricChargeInMW;
                return;
            }

            ProcessCharging();

            // quit if no fuel available
            if (stored_fuel_ratio <= 0.01)
            {
                plasma_ratio = 0;
                return;
            }

            var powerRequested = LaserPowerRequirements * required_reactor_ratio;

            double primaryPowerReceived;
            if (!CheatOptions.InfiniteElectricity && powerRequested > 0)
            {
                primaryPowerReceived = usePowerManagerForPrimaryInputPower
                    ? consumeFNResourcePerSecondBuffered(powerRequested, primaryInputResource, 0.1)
                    : part.RequestResource(primaryInputResourceDefinition.id, powerRequested * timeWarpFixedDeltaTime, ResourceFlowMode.STAGE_PRIORITY_FLOW) / timeWarpFixedDeltaTime;
            }
            else
                primaryPowerReceived = powerRequested;

            if (maintenancePowerWasteheatRatio > 0)
                supplyFNResourcePerSecond(maintenancePowerWasteheatRatio * primaryPowerReceived, ResourceManager.FNRESOURCE_WASTEHEAT);

            // calculate effective primary power ratio
            var powerReceived = primaryPowerReceived;
            var powerRequirmentMetRatio = powerRequested > 0 ? powerReceived / powerRequested : 1;

            // retrieve any shortage from secondary buffer
            if (secondaryInputMultiplier > 0 && secondaryInputResourceDefinition != null && !CheatOptions.InfiniteElectricity && IsEnabled && powerReceived < powerRequested)
            {
                double currentSecondaryRatio;
                double currentSecondaryCapacity;
                double currentSecondaryAmount;

                if (usePowerManagerForSecondaryInputPower)
                {
                    currentSecondaryRatio = getResourceBarRatio(secondaryInputResource);
                    currentSecondaryCapacity = getTotalResourceCapacity(secondaryInputResource);
                    currentSecondaryAmount = currentSecondaryCapacity * currentSecondaryRatio;
                }
                else
                {
                    part.GetConnectedResourceTotals(secondaryInputResourceDefinition.id, out currentSecondaryAmount, out currentSecondaryCapacity);
                    currentSecondaryRatio = currentSecondaryCapacity > 0 ? currentSecondaryAmount / currentSecondaryCapacity : 0;
                }

                var secondaryPowerMaxRatio = ((double)(decimal)maxSecondaryPowerUsage) / 100d;

                // only use buffer if we have sufficient in storage
                if (currentSecondaryRatio > secondaryPowerMaxRatio)
                {
                    // retreive megawatt ratio
                    var powerShortage = (1 - powerRequirmentMetRatio) * powerRequested;
                    var maxSecondaryConsumption = currentSecondaryAmount - (secondaryPowerMaxRatio * currentSecondaryCapacity);
                    var requestedSecondaryPower = Math.Min(maxSecondaryConsumption, powerShortage * secondaryInputMultiplier * timeWarpFixedDeltaTime);
                    var secondaryPowerReceived = part.RequestResource(secondaryInputResource, requestedSecondaryPower);
                    powerReceived += secondaryPowerReceived / secondaryInputMultiplier / timeWarpFixedDeltaTime;
                    powerRequirmentMetRatio = powerRequested > 0 ? powerReceived / powerRequested : 1;
                }
            }

            // adjust power to optimal power
            power_consumed = LaserPowerRequirements * powerRequirmentMetRatio;

            // verify if we need startup with accumulated power
            if (canJumpstart && timeWarpFixedDeltaTime <= 0.1 && accumulatedElectricChargeInMW > 0 && power_consumed < StartupPower && (accumulatedElectricChargeInMW + power_consumed) >= StartupPower)
            {
                var shortage = StartupPower - power_consumed;
                if (shortage <= accumulatedElectricChargeInMW)
                {
                    //ScreenMessages.PostScreenMessage("Attempting to Jump start", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                    power_consumed += accumulatedElectricChargeInMW;
                    accumulatedElectricChargeInMW -= shortage;
                    jumpstartPowerTime = 50;
                }
            }

            if (isSwappingFuelMode)
            {
                plasma_ratio = 1;
                isSwappingFuelMode = false;
            }
            else if (jumpstartPowerTime > 0)
            {
                plasma_ratio = 1;
                jumpstartPowerTime--;
            }
            else if (framesPlasmaRatioIsGood > 0) // maintain reactor
            {
                plasma_ratio = Math.Round(LaserPowerRequirements > 0 ? power_consumed / LaserPowerRequirements : 1, 4);
                allowJumpStart = plasma_ratio >= 1;
            }
            else  // starting reactor
            {
                plasma_ratio = Math.Round(StartupPower > 0 ? power_consumed / StartupPower : 1, 4);
                allowJumpStart = plasma_ratio >= 1;
            }

            if (plasma_ratio > 0.999)
            {
                plasma_ratio = 1;
                isChargingForJumpstart = false;
                IsEnabled = true;
                if (framesPlasmaRatioIsGood < 100)
                    framesPlasmaRatioIsGood += 1;
                if (framesPlasmaRatioIsGood > 10)
                    accumulatedElectricChargeInMW = 0;
            }
            else
            {
                var treshhold = 10 * (1 - plasma_ratio);
                if (framesPlasmaRatioIsGood >= treshhold)
                {
                    framesPlasmaRatioIsGood -= treshhold;
                    plasma_ratio = 1;
                }
            }
        }

        private void UpdateLoopingAnimation(double ratio)
        {
            if (loopingAnimation == null)
                return;

            if (!isDeployed)
                return;

            if (!IsEnabled)
            {
                if (!initialized || shutdownAnimation == null || loopingAnimation.IsMoving()) return;

                if (!(animationStarted >= 0))
                {
                    animationStarted = Planetarium.GetUniversalTime();
                    shutdownAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Activate));
                }
                else if (!shutdownAnimation.IsMoving())
                {
                    shutdownAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Deactivate));
                    initialized = false;
                    isDeployed = true;
                }
                return;
            }

            if (!loopingAnimation.IsMoving())
                loopingAnimation.Toggle();
        }

        private void ProcessCharging()
        {
            if (!canJumpstart || !isChargingForJumpstart || !(part.vessel.geeForce < startupMaximumGeforce)) return;

            var neededPower = Math.Max(StartupPower - accumulatedElectricChargeInMW, 0);

            if (neededPower <= 0)
                return;

            var availableStablePower = getStableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES);

            var minimumChargingPower = startupMinimumChargePercentage * RawPowerOutput;
            if (startupCostGravityMultiplier > 0)
            {
                gravityDivider = GravityDivider;
                minimumChargingPower = gravityDivider > 0 ? minimumChargingPower / gravityDivider : minimumChargingPower;
            }

            if (availableStablePower < minimumChargingPower)
            {
                if (startupCostGravityMultiplier > 0)
                    ScreenMessages.PostScreenMessage("Curent you need at least " + minimumChargingPower.ToString("F0") + " MW to charge the reactor. Move closer to gravity well to reduce amount needed", 1f, ScreenMessageStyle.UPPER_CENTER);
                else
                    ScreenMessages.PostScreenMessage("You need at least " + minimumChargingPower.ToString("F0") + " MW to charge the reactor", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                var megaJouleRatio = usePowerManagerForPrimaryInputPower 
                    ? getResourceBarRatio(primaryInputResource)
                    : part.GetResourceRatio(primaryInputResource);

                var primaryPowerRequest = Math.Min(neededPower, availableStablePower * megaJouleRatio);

                // verify we amount of power collected exceeds treshhold
                var returnedPrimaryPower = CheatOptions.InfiniteElectricity
                    ? neededPower
                    : usePowerManagerForPrimaryInputPower
                        ? consumeFNResourcePerSecond(primaryPowerRequest, primaryInputResource)
                        : part.RequestResource(primaryInputResource, primaryPowerRequest * timeWarpFixedDeltaTime);

                var powerPerSecond = usePowerManagerForPrimaryInputPower ? returnedPrimaryPower : returnedPrimaryPower / timeWarpFixedDeltaTime;

                if (!CheatOptions.IgnoreMaxTemperature && maintenancePowerWasteheatRatio > 0)
                    supplyFNResourcePerSecond(0.05 * powerPerSecond, ResourceManager.FNRESOURCE_WASTEHEAT);

                if (powerPerSecond >= minimumChargingPower)
                    accumulatedElectricChargeInMW += returnedPrimaryPower * timeWarpFixedDeltaTime;
                else
                {
                    if (startupCostGravityMultiplier > 0)
                        ScreenMessages.PostScreenMessage("Curent you need at least " + minimumChargingPower.ToString("F0") + " MW to charge the reactor. Move closer to gravity well to reduce amount needed", 5f, ScreenMessageStyle.UPPER_CENTER);
                    else
                        ScreenMessages.PostScreenMessage("You need at least " + minimumChargingPower.ToString("F0") + " MW to charge the reactor", 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            // secondry try to charge from secondary Power Storage
            neededPower = StartupPower - accumulatedElectricChargeInMW;
            if (secondaryInputMultiplier > 0 && neededPower > 0 && startupMinimumChargePercentage <= 0)
            {
                var requestedSecondaryPower = neededPower * secondaryInputMultiplier;

                var secondaryPowerReceived = usePowerManagerForSecondaryInputPower
                    ? consumeFNResource(requestedSecondaryPower, secondaryInputResource)
                    : part.RequestResource(secondaryInputResource, requestedSecondaryPower);

                accumulatedElectricChargeInMW += secondaryPowerReceived / secondaryInputMultiplier;
            }
        }
    }
}