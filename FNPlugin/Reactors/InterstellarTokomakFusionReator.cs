using FNPlugin.Resources;
using KSP.Localization;
using System;

namespace FNPlugin.Reactors
{
    [KSPModule("Magnetic Confinement Fusion Engine")]
    class  InterstellarTokamakFusionEngine : InterstellarTokamakFusionReactor {}

    [KSPModule("Magnetic Confinement Fusion Reactor")]
    class InterstellarTokamakFusionReactor : InterstellarFusionReactor
    {
        // persistent
        [KSPField(isPersistant = true)] public double storedPlasmaEnergyRatio;

        // configs
        [KSPField] public double plasmaBufferSize = 10;
        [KSPField] public double minimumHeatingRequirements = 0.1;
        [KSPField] public double heatingRequestExponent = 1.5;

        // help varaiables
        public bool fusion_alert;
        public int jumpstartPowerTime;
        public int fusionAlertFrames;
        public double power_consumed;
        public double heatingPowerRequirements;

        public double HeatingPowerRequirements
        {
            get
            {
                heatingPowerRequirements = CurrentFuelMode == null
                    ? PowerRequirement
                    : PowerRequirement * CurrentFuelMode.NormalizedPowerRequirements;

                if (heatingPowerRequirements <= 0)
                    return 0;

                heatingPowerRequirements = Math.Max(heatingPowerRequirements * Math.Pow(required_reactor_ratio, heatingRequestExponent), heatingPowerRequirements * minimumHeatingRequirements);

                return heatingPowerRequirements;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!isSwappingFuelMode && (!CheatOptions.InfiniteElectricity && GetDemandStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt) > 1.01
                                                                          && GetResourceBarRatio(ResourceSettings.Config.ElectricPowerInMegawatt) < 0.25) && IsEnabled && !fusion_alert)
                fusionAlertFrames++;
            else
            {
                fusion_alert = false;
                fusionAlertFrames = 0;
            }

            if (fusionAlertFrames > 2)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_TokomakFusionReator_PostMsg1"), 0.1f, ScreenMessageStyle.UPPER_CENTER);//"Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended."
                fusion_alert = true;
            }

            electricPowerMaintenance = PluginHelper.GetFormattedPowerString(power_consumed) + " / " + PluginHelper.GetFormattedPowerString(heatingPowerRequirements);
        }

        private double GetPlasmaRatio(double receivedPowerPerSecond, double fusionPowerRequirement)
        {
            if (fusionPowerRequirement <= 0)
                return 1;

            if (receivedPowerPerSecond >= fusionPowerRequirement)
            {
                storedPlasmaEnergyRatio += ((receivedPowerPerSecond - fusionPowerRequirement) / PowerRequirement);
                receivedPowerPerSecond = fusionPowerRequirement;
            }
            else
            {
                if (required_reactor_ratio > 0)
                {
                    storedPlasmaEnergyRatio -= (fusionPowerRequirement - receivedPowerPerSecond) / PowerRequirement;
                }

                receivedPowerPerSecond = Math.Min(1, storedPlasmaEnergyRatio) * fusionPowerRequirement;
            }

            return Math.Round(fusionPowerRequirement > 0 ? receivedPowerPerSecond / fusionPowerRequirement : 1, 4);
        }

        public override void StartReactor()
        {
            base.StartReactor();

            if (HighLogic.LoadedSceneIsEditor) return;

            var fixedDeltaTime = (double)(decimal)TimeWarp.fixedDeltaTime;

            var availablePower = GetResourceAvailability(ResourceSettings.Config.ElectricPowerInMegawatt);

            var fusionPowerRequirement = PowerRequirement;

            if (availablePower >= fusionPowerRequirement)
            {
                // consume from any stored megajoule source
                power_consumed = CheatOptions.InfiniteElectricity
                    ? fusionPowerRequirement
                    : part.RequestResource(ResourceSettings.Config.ElectricPowerInMegawatt, fusionPowerRequirement * fixedDeltaTime) / fixedDeltaTime;
            }
            else
            {
                var message = Localizer.Format("#LOC_KSPIE_TokomakFusionReator_PostMsg2", fusionPowerRequirement.ToString("F2"));//"Not enough power to start fusion reactor, it requires at " +  + " MW"
                UnityEngine.Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // determine if we have received enough power
            plasma_ratio = GetPlasmaRatio(power_consumed, fusionPowerRequirement);
            UnityEngine.Debug.Log("[KSPI]: InterstellarTokamakFusionReactor StartReactor plasma_ratio " + plasma_ratio);
            allowJumpStart = plasma_ratio > 0.99;
            if (allowJumpStart)
            {
                storedPlasmaEnergyRatio = 1;
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_TokomakFusionReator_PostMsg3"), 5f, ScreenMessageStyle.LOWER_CENTER);//"Starting fusion reaction"
                jumpstartPowerTime = 10;
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_TokomakFusionReator_PostMsg4"), 5f, ScreenMessageStyle.LOWER_CENTER);//"Not enough power to start fusion reaction"
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (IsEnabled)
            {
                var fusionPowerRequirement = HeatingPowerRequirements;

                if (fusionPowerRequirement <= 0)
                {
                    plasma_ratio = 1;
                    power_consumed = 0;
                    return;
                }

                var requestedPower = reactor_power_ratio <= 0 ? 0
                    : fusionPowerRequirement + ((plasmaBufferSize - storedPlasmaEnergyRatio) * PowerRequirement);

                // consume power from managed power source
                power_consumed = CheatOptions.InfiniteElectricity
                    ? requestedPower
                    : ConsumeFnResourcePerSecond(requestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);

                if (maintenancePowerWasteheatRatio > 0)
                    SupplyFnResourcePerSecond(maintenancePowerWasteheatRatio * power_consumed,
                        ResourceSettings.Config.WasteHeatInMegawatt);

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
                else
                {
                    plasma_ratio = GetPlasmaRatio(power_consumed, fusionPowerRequirement);
                    allowJumpStart = plasma_ratio > 0.99;
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
                    {
                        storedPlasmaEnergyRatio = plasmaBufferSize;
                        jumpstartPowerTime = 10;
                    }

                    UnityEngine.Debug.Log("[KSPI]: Jumpstart InterstellarTokamakFusionReactor ");
                }
            }

            base.OnStart(state);
        }

        public override void UpdateEditorPowerOutput()
        {
            base.UpdateEditorPowerOutput();
            required_reactor_ratio = 1.0;
            electricPowerMaintenance = PluginHelper.GetFormattedPowerString(HeatingPowerRequirements) + " / " + PluginHelper.GetFormattedPowerString(HeatingPowerRequirements);
        }
    }
}
