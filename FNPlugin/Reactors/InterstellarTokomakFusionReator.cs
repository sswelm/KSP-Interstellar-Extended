using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    class InterstellarTokamakFusionReactor : InterstellarFusionReactor
    {
        public bool fusion_alert = false;
        public double power_consumed = 0.0;
        public int jumpstartPowerTime = 0;
        public int fusionAlertFrames = 0;

        public double HeatingPowerRequirements 
		{ 
			get { 
				return current_fuel_mode == null
                    ? PowerRequirement
                    : PowerRequirement * current_fuel_mode.NormalisedPowerRequirements; 
			} 
		}

        public override void OnUpdate() 
        {
            base.OnUpdate();
            if (!isSwappingFuelMode && (!CheatOptions.InfiniteElectricity && getDemandStableSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) > 1.01) && IsEnabled && !fusion_alert)
                fusionAlertFrames++;
            else
            {
                fusion_alert = false;
                fusionAlertFrames = 0;
            }

            if (fusionAlertFrames > 2)
            {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 0.1f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            }

            electricPowerMaintenance = PluginHelper.getFormattedPowerString(power_consumed) + " / " + PluginHelper.getFormattedPowerString(HeatingPowerRequirements);
        }

        private float GetPlasmaRatio(double consumedPower)
        {
            return (float)Math.Round(HeatingPowerRequirements != 0.0f ? consumedPower / HeatingPowerRequirements : 1.0f, 4);
        }

        public override void StartReactor()
        {
            base.StartReactor();

            if (HighLogic.LoadedSceneIsEditor) return;

            // consume from any stored megajoule source
            power_consumed = CheatOptions.InfiniteElectricity 
                ? HeatingPowerRequirements
                : part.RequestResource(FNResourceManager.FNRESOURCE_MEGAJOULES, HeatingPowerRequirements * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime; 

            plasma_ratio = GetPlasmaRatio(power_consumed);
            UnityEngine.Debug.Log("[KSPI] - InterstellarTokamakFusionReactor StartReactor plasma_ratio " + plasma_ratio);
            allowJumpStart = plasma_ratio == 1;
            if (allowJumpStart)
            {
                ScreenMessages.PostScreenMessage("Starting fusion reaction", 5f, ScreenMessageStyle.LOWER_CENTER);
                jumpstartPowerTime = 100;
            }
            else
                ScreenMessages.PostScreenMessage("Not enough power to start fusion reaction", 5f, ScreenMessageStyle.LOWER_CENTER);
        }

        public override void OnFixedUpdate() 
        {
            base.OnFixedUpdate();
            if (IsEnabled) 
            {
                var powerRequest = HeatingPowerRequirements * TimeWarp.fixedDeltaTime;

                power_consumed = CheatOptions.InfiniteElectricity
                    ? powerRequest
                    : consumeFNResource(powerRequest, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;

                if(isSwappingFuelMode)
                {
                    plasma_ratio = 1;
                    isSwappingFuelMode = false;
                }
                else if (jumpstartPowerTime > 0)
                {
                    plasma_ratio = 1;
                    jumpstartPowerTime--;
                }
                else
                {
                    plasma_ratio = GetPlasmaRatio(power_consumed);
                    allowJumpStart = plasma_ratio == 1;
                }
            }
            else
            {
                plasma_ratio = 0;
                power_consumed = 0;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.Editor)
            {
                if (allowJumpStart)
                {
                    if (startDisabled)
                        allowJumpStart = false;
                    else
                        jumpstartPowerTime = 100;
                    
                    UnityEngine.Debug.Log("[KSPI] - Jumpstart InterstellarTokamakFusionReactor ");
                }
            }

            // call Interstellar Reactor Onstart
            base.OnStart(state);
        }

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
