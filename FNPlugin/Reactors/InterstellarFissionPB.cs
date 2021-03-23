using FNPlugin.Wasteheat;
using System;

namespace FNPlugin.Reactors
{
    [KSPModule("Pebble Bed Fission Reactor")]
    class InterstellarPebbleBedFissionReactor : InterstellarFissionPB {}

    [KSPModule("Pebble Bed Fission Engine")]
    class InterstellarPebbleBedFissionEngine : InterstellarFissionPB { }

    class InterstellarFissionPB : InterstellarReactor
    {
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiName = "#LOC_KSPIE_FissionPB_HeatThrottling")]//Heat Throttling
        public bool heatThrottling = false;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = true, guiUnits = "%", guiName = "#LOC_KSPIE_FissionPB_Overheating", guiFormat = "F3")]//Overheating
        public double overheatPercentage;

        [KSPField(isPersistant = false)] public double thermalRatioEfficiencyModifier = 0.81;
        [KSPField(isPersistant = false)] public double maximumChargedIspMult = 114;
        [KSPField(isPersistant = false)] public double minimumChargdIspMult = 11.4;

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionPB_ManualRestart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Restart
        public void ManualRestart()
        {
            IsEnabled = true;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionPB_ManualShutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Shutdown
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        public override bool IsFuelNeutronRich => CurrentFuelMode != null && !CurrentFuelMode.Aneutronic;
        public override double MaximumThermalPower => base.MaximumThermalPower * ThermalRatioEfficiency;
        public override double MaximumChargedPower => base.MaximumChargedPower * ThermalRatioEfficiency;
        public override double StableMaximumReactorPower => IsEnabled ? NormalisedMaximumPower * ThermalRatioEfficiency : 0;

        private double ThermalRatioEfficiency =>
            reactorType == 4 || heatThrottling
                ? Math.Pow((ZeroPowerTemp - CoreTemperature) / OptimalTempDifference, thermalRatioEfficiencyModifier)
                : 1;

        private double OptimalTemp => base.CoreTemperature;
        private double ZeroPowerTemp => base.CoreTemperature * 1.25f;
        private double OptimalTempDifference => ZeroPowerTemp - OptimalTemp;
        public override bool IsNuclear => true;

        public override double CoreTemperature
        {
            get
            {
                 var radiatorTemperature = HighLogic.LoadedSceneIsEditor || CheatOptions.IgnoreMaxTemperature ? 0 : FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel);

                 return GetCoreTempAtRadiatorTemp(radiatorTemperature);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            overheatPercentage = (1 - ThermalRatioEfficiency) * 100;
        }


        public override void OnUpdate()
        {
            overheatPercentage = (1 - ThermalRatioEfficiency) * 100;
            Events[nameof(ManualRestart)].active = Events[nameof(ManualRestart)].guiActiveUnfocused = !IsEnabled && !ongoingDecay;
            Events[nameof(ManualShutdown)].active = Events[nameof(ManualShutdown)].guiActiveUnfocused = IsEnabled;
            base.OnUpdate();
        }

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }

        public override double GetCoreTempAtRadiatorTemp(double radTemp)
        {
            if (!heatThrottling) return base.CoreTemperature;

            double pfrTemp;
            if (!double.IsNaN(radTemp) && !double.IsInfinity(radTemp))
                pfrTemp = Math.Min(Math.Max(radTemp * 1.5, OptimalTemp), ZeroPowerTemp);
            else
                pfrTemp = OptimalTemp;

            return pfrTemp;
        }

        public override double GetThermalPowerAtTemp(double temp)
        {
            if (reactorType != 4 && !heatThrottling) return base.GetThermalPowerAtTemp(temp);

            double relTempDiff;
            if (temp > OptimalTemp && temp < ZeroPowerTemp)
                relTempDiff = Math.Pow((ZeroPowerTemp - temp) / OptimalTempDifference, thermalRatioEfficiencyModifier);
            else
                relTempDiff = 1;

            return MaximumPower * relTempDiff;
        }
    }
}
