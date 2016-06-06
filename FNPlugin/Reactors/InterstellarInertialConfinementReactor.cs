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
        [KSPField(isPersistant = false)]
        protected bool powerPercentageAffectsPowerRequirements = false;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Control"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = 10)]
        public float powerPercentage = 100;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Startup"), UI_Toggle(disabledText = "Off", enabledText = "Charging")]
        public bool isChargingForJumpstart;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiUnits = "%", guiFormat = "F2", guiName = "Minimum Throtle")]
        public float minimumThrottlePercentage;

        // UI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge")]
        public string accumulatedChargeStr = String.Empty;

        // protected fields
        protected float power_consumed;
        protected bool fusion_alert = false;
        protected int shutdown_c = 0;
        protected int jumpstartPowerTime = 0;

        protected BaseField powerPercentageField;
        protected BaseField isChargingField;

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
                    jumpstartPowerTime = 100;
                    IsEnabled = true;
                }

                UnityEngine.Debug.LogWarning("[KSPI] - InterstellarInertialConfinementReactor.OnStart allowJumpStart");
            }
            
        }

        public override float MinimumThrottle 
        {
            get 
            {
                var currentMinimumThrottle = (powerPercentage > 0 && base.MinimumThrottle > 0) 
                    ? Mathf.Min(base.MinimumThrottle / (powerPercentage / 100), 1) 
                    : base.MinimumThrottle;

                minimumThrottlePercentage = currentMinimumThrottle * 100;

                return currentMinimumThrottle;
            } 
        }

	    public float LaserPowerRequirements
	    {
		    get 
            { 
                return current_fuel_mode == null 
                    ? PowerRequirement 
                    : powerPercentageAffectsPowerRequirements 
                        ? powerPercentage * PowerRequirement * current_fuel_mode.NormalisedPowerRequirements 
                        : PowerRequirement * current_fuel_mode.NormalisedPowerRequirements;
            }
	    }

        public float StartupPower
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

		public override double MaximumThermalPower { get { 
				float plasmaModifier = (plasma_ratio >= 1.0 ? 1 : 0);
				return (powerPercentage / 100) *  NormalisedMaximumPower  * plasmaModifier * (1 - (float)ChargedPowerRatio); } }

		public override double MaximumChargedPower { get {
				float plasmaModifier = (plasma_ratio >= 1.0 ? 1 : 0);
				return (powerPercentage / 100) * NormalisedMaximumPower * plasmaModifier * (float)ChargedPowerRatio; } }

        public override void Update()
        {
            base.Update();

            isChargingField.guiActive = !IsEnabled &&  HighLogic.LoadedSceneIsFlight && canChargeJumpstart && part.vessel.geeForce < startupMaximumGeforce ;
            isChargingField.guiActiveEditor = false;
        }

        public override void OnUpdate() 
        {
            if (!isChargingForJumpstart && !isSwappingFuelMode && getCurrentResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) && getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 && IsEnabled && !fusion_alert) 
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

            //powerPercentageField.guiActive = !IsEnabled && !isChargingForJumpstart;

            electricPowerMaintenance = PluginHelper.getFormattedPowerString(power_consumed) + " / " + PluginHelper.getFormattedPowerString(LaserPowerRequirements);

            // call base class
            base.OnUpdate();
        }

        public override void OnFixedUpdate() 
        {
            base.OnFixedUpdate();

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

            // consume reactor power requirements
            power_consumed = consumeFNResource(LaserPowerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;

            // verify if we need startup with accumulated power
            if (TimeWarp.fixedDeltaTime <= 0.1 && accumulatedElectricChargeInMW > 0 && power_consumed < StartupPower && (accumulatedElectricChargeInMW + power_consumed) >= StartupPower)
            {
                var shortage = StartupPower - power_consumed;
                if (shortage <= accumulatedElectricChargeInMW)
                {
                    ScreenMessages.PostScreenMessage("Attempting to Jump start", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                    power_consumed += (float)accumulatedElectricChargeInMW;
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

        private void ProcessCharging()
        {
            if (isChargingForJumpstart && part.vessel.geeForce < startupMaximumGeforce)
            {
                var neededPower = StartupPower - accumulatedElectricChargeInMW;

                // first try to charge from Megajoule Storage
                if (neededPower > 0)
                {
                    // verify we amount of power collected exceeds treshhold
                    var returnedMegaJoulePower = consumeFNResource(neededPower, FNResourceManager.FNRESOURCE_MEGAJOULES);
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

        protected override void setDefaultFuelMode()
        {
            current_fuel_mode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
        }

    }
}
