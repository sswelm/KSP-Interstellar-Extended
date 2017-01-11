using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    [KSPModule("Omega Fusion Reactor")]
    class InterstellarInertialConfinementReactor : InterstellarFusionReactor
    {
        [KSPField(isPersistant = true)]
        protected double accumulatedElectricChargeInMW;

        // settings
        [KSPField(isPersistant = false)]
        protected bool canChargeJumpstart = true;
        [KSPField(isPersistant = false)]
        public float startupPowerMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float startupCostGravityMultiplier = 0;
        [KSPField(isPersistant = false)]
        public float startupMaximumGeforce = 10000;
        [KSPField(isPersistant = false)]
        public float startupMinimumChargePercentage = 0;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Power Affects Maintenance")]
        public bool powerControlAffectsMaintenance = false;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Control"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = 10)]
        public float powerPercentage = 100;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Startup"), UI_Toggle(disabledText = "Off", enabledText = "Charging")]
        public bool isChargingForJumpstart;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiUnits = "%", guiFormat = "F2", guiName = "Minimum Throtle")]
        public double minimumThrottlePercentage;

        // UI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge")]
        public string accumulatedChargeStr = String.Empty;
        //[KSPField(isPersistant = false, guiActive = true, guiName = "Scalar")]
        //public float animationScalar;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power Requirment")]
        public double currentLaserPowerRequirements = 0;

        // protected fields
        protected double power_consumed;
        protected bool fusion_alert = false;
        protected int shutdown_c = 0;
        protected int jumpstartPowerTime = 0;

        protected BaseField powerPercentageField;
        protected BaseField isChargingField;

        public override double PlasmaModifier
        {
            get { return (plasma_ratio >= 0.01 ? Math.Min(plasma_ratio, 1) : 0); }
        }

        public double PowerRatio
        {
            get { return powerPercentage / 100.0; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            powerPercentageField = Fields["powerPercentage"];
            isChargingField = Fields["isChargingForJumpstart"];

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

                UnityEngine.Debug.LogWarning("[KSPI] - InterstellarInertialConfinementReactor.OnStart allowJumpStart");
            }
            
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
                    current_fuel_mode == null 
                    ? PowerRequirement
                    : powerControlAffectsMaintenance 
                        ? PowerRatio * NormalizedPowerRequirment
                        : NormalizedPowerRequirment;
                return currentLaserPowerRequirements;
            }
	    }

        public double StartupPower
        {
            get 
            {
                var startupPower = startupPowerMultiplier * LaserPowerRequirements; 
                if (startupCostGravityMultiplier > 0)
                {
                    var gravityFactor = startupCostGravityMultiplier * FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude;
                    if (gravityFactor > 0)
                        startupPower = (float)(startupPower / gravityFactor);
                }

                return startupPower;
            }
        }
        
        public override bool shouldScaleDownJetISP() 
        {
            return isupgraded ? false : true;
        }

		public override double MaximumChargedPower 
        { 
            get 
            {
                return PowerRatio * base.MaximumChargedPower; 
            } 
        }

        public override double MaximumThermalPower
        {
            get
            {
                return PowerRatio * base.MaximumThermalPower;
            }
        }

        public override void Update()
        {
            base.Update();

            isChargingField.guiActive = !IsEnabled &&  HighLogic.LoadedSceneIsFlight && canChargeJumpstart && part.vessel.geeForce < startupMaximumGeforce ;
            isChargingField.guiActiveEditor = false;
        }

        public override void OnUpdate() 
        {
            if (!CheatOptions.InfiniteElectricity && !isChargingForJumpstart && !isSwappingFuelMode && getCurrentResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) && getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 && IsEnabled && !fusion_alert) 
            {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            } 
            else 
                fusion_alert = false;

            if (isChargingField.guiActive)
                accumulatedChargeStr = FNGenerator.getPowerFormatString(accumulatedElectricChargeInMW) + " / " + FNGenerator.getPowerFormatString(StartupPower);
            else if (part.vessel.geeForce > startupMaximumGeforce)
                accumulatedChargeStr = part.vessel.geeForce.ToString("0.000")  + "g > " + startupMaximumGeforce + "g";
            else
                accumulatedChargeStr = String.Empty;

            Fields["accumulatedChargeStr"].guiActive = plasma_ratio < 1;

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

            // determiine amount of power needed
            var powerRequested = LaserPowerRequirements * TimeWarp.fixedDeltaTime * Math.Max(reactor_power_ratio, 0.00001);

            // consume reactor power requirements
            var powerReceived = CheatOptions.InfiniteElectricity 
                ? powerRequested 
                : consumeFNResource(powerRequested, FNResourceManager.FNRESOURCE_MEGAJOULES);

            // retreive any shortage from buffer
            if (!CheatOptions.InfiniteElectricity &&  IsEnabled && powerReceived < powerRequested)
            {
                // retreive megawath ratio
                var megaWattStorageRatio = getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES);

                //only use buffer if we have sufficient in storage
                if (megaWattStorageRatio > 0.5)
                {
                    var powerRequirmentMetRatio = powerReceived / powerRequested;
                    powerReceived = powerReceived + part.RequestResource(FNResourceManager.FNRESOURCE_MEGAJOULES, (1 - powerRequirmentMetRatio) * powerRequested);
                }
            }

            // adjust power to optimal power
            power_consumed = LaserPowerRequirements * (powerReceived / powerRequested);

            // verify if we need startup with accumulated power
            if (TimeWarp.fixedDeltaTime <= 0.1 && accumulatedElectricChargeInMW > 0 && power_consumed < StartupPower && (accumulatedElectricChargeInMW + power_consumed) >= StartupPower)
            {
                var shortage = StartupPower - power_consumed;
                if (shortage <= accumulatedElectricChargeInMW)
                {
                    //ScreenMessages.PostScreenMessage("Attempting to Jump start", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                    power_consumed += (float)accumulatedElectricChargeInMW;
                    accumulatedElectricChargeInMW -= shortage;
                    jumpstartPowerTime = 50;
                }
            }

            //plasma_ratio = power_consumed / LaserPowerRequirements;
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
                plasma_ratio = (float)Math.Round(LaserPowerRequirements != 0.0f ? power_consumed / LaserPowerRequirements : 1.0f, 4);
                allowJumpStart = plasma_ratio >= 1;
            }
            else  // starting reactor
            {
                plasma_ratio = (float)Math.Round(LaserPowerRequirements != 0.0f ? power_consumed / StartupPower : 1.0f, 4);
                allowJumpStart = plasma_ratio >= 1;
            }


            if (plasma_ratio >= 0.99)
            {
                plasma_ratio = 1;
                isChargingForJumpstart = false;
                IsEnabled = true;
                if (framesPlasmaRatioIsGood < 100)
                    framesPlasmaRatioIsGood++;
                if (framesPlasmaRatioIsGood > 10)
                    accumulatedElectricChargeInMW = 0;
            }
            else
            {
                IsEnabled = false;
                framesPlasmaRatioIsGood = 0;

                if (plasma_ratio < 0.01)
                    plasma_ratio = 0;
            }
        }

        public void UpdateLoopingAnimation(double ratio)
        {
            if (loopingAnimation == null)
                return;

            if (!isDeployed)
                return;

            if (!IsEnabled)
            {
                if (initialized && shutdownAnimation != null && !loopingAnimation.IsMoving())
                {
                    if (animationStarted == 0)
                    {
                        animationStarted = Planetarium.GetUniversalTime();
                        shutdownAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Activate));
                    }
                    else if (!shutdownAnimation.IsMoving())
                    {
                        shutdownAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Deactivate));
                        initialized = false;
                        isDeployed = true;
                        //doOnce = false;
                    }
                }
                return;
            }

            if (!loopingAnimation.IsMoving())
                loopingAnimation.Toggle();
        }

        private void ProcessCharging()
        {
            if (isChargingForJumpstart && part.vessel.geeForce < startupMaximumGeforce)
            {
                var neededPower = StartupPower - accumulatedElectricChargeInMW;

                // first try to charge from Megajoule Storage
                if (neededPower > 0)
                {
                    // verify we amount of power collected exceeds treshhold
                    var returnedMegaJoulePower = CheatOptions.InfiniteElectricity 
                        ? neededPower 
                        : consumeFNResource(neededPower, FNResourceManager.FNRESOURCE_MEGAJOULES);

                    if (startupMinimumChargePercentage == 0 || returnedMegaJoulePower / TimeWarp.fixedDeltaTime > (startupMinimumChargePercentage * StartupPower))
                    {
                        accumulatedElectricChargeInMW += returnedMegaJoulePower;
                    }
                }

                // secondry try to charge from ElectricCharge Storage
                neededPower = StartupPower - accumulatedElectricChargeInMW;
                if (neededPower > 0 && startupMinimumChargePercentage == 0)
                    accumulatedElectricChargeInMW += part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, neededPower * 1000) / 1000;
            }
        }

        private int framesPlasmaRatioIsGood;

        public override string getResourceManagerDisplayName() 
        {
            return TypeName;
        }

        public override int getPowerPriority() 
        {
            return 1;
        }

    }
}
