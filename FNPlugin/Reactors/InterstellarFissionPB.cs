using System;

namespace FNPlugin.Reactors
{
    [KSPModule("Pebble Bed Fission Reactor")]
    class InterstellarPebbleBedFissionReactor : InterstellarFissionPB {}

    [KSPModule("Pebble Bed Fission Engine")]
    class InterstellarPebbleBedFissionEngine : InterstellarFissionPB { }
    
    class InterstellarFissionPB : InterstellarReactor
    {
        // Persistant False
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FissionPB_HeatThrottling")]//Heat Throttling
        public bool heatThrottling = false;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActiveEditor = false, guiActive = true, guiUnits = "%", guiName = "#LOC_KSPIE_FissionPB_Overheating", guiFormat = "F3")]//Overheating
        public double overheatPercentage;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_FissionPB_WasteheatRatio")]//Wasteheat Ratio
        public double resourceBarRatio;
        [KSPField(isPersistant = false)]
        public double thermalRatioEfficiencyModifier = 0.81;
        [KSPField(isPersistant = false)]
        public double maximumChargedIspMult = 114;
        [KSPField(isPersistant = false)]
        public double minimumChargdIspMult = 11.4;
        [KSPField(isPersistant = false)]
        public double coreTemperatureWasteheatPower = 0.3;
        [KSPField(isPersistant = false)]
        public double coreTemperatureWasteheatModifier = -0.2;
        [KSPField(isPersistant = false)]
        public double coreTemperatureWasteheatMultiplier = 1.25;

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionPB_ManualRestart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Restart
        public void ManualRestart()
        {
            IsEnabled = true;
        }

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionPB_ManualShutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Shutdown
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        public override bool IsFuelNeutronRich { get { return CurrentFuelMode != null && !CurrentFuelMode.Aneutronic; } }

        public override double MaximumThermalPower { get { return base.MaximumThermalPower * ThermalRatioEfficiency; } }

        public override double MaximumChargedPower { get { return base.MaximumChargedPower * ThermalRatioEfficiency; } }

        public override double StableMaximumReactorPower { get { return IsEnabled ? NormalisedMaximumPower * ThermalRatioEfficiency : 0; } }

        private double ThermalRatioEfficiency
        {
            get
            {
                return reactorType == 4 || heatThrottling
                    ? Math.Pow((ZeroPowerTemp - CoreTemperature) / OptimalTempDifference, thermalRatioEfficiencyModifier)
                    : 1;
            }
        }

        private double OptimalTemp { get { return base.CoreTemperature; } }

        private double ZeroPowerTemp { get { return base.CoreTemperature * 1.25f; } }

        private double OptimalTempDifference { get { return ZeroPowerTemp - OptimalTemp; } }

        public override bool IsNuclear { get { return true; } }

        public override double CoreTemperature
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight && heatThrottling)
                {
                    resourceBarRatio = CheatOptions.IgnoreMaxTemperature ? 0 : getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);

                    var temperatureIncrease = Math.Max(Math.Pow(resourceBarRatio, coreTemperatureWasteheatPower) + coreTemperatureWasteheatModifier, 0) * coreTemperatureWasteheatMultiplier * OptimalTempDifference;

                    return Math.Min(Math.Max(OptimalTemp + temperatureIncrease, OptimalTemp), ZeroPowerTemp);
                }
                return base.CoreTemperature;
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
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            base.OnUpdate();
        }

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }

        public override double GetCoreTempAtRadiatorTemp(double radTemp)
        {
            if (heatThrottling)
            {
                double pfr_temp = 0;

                if (!double.IsNaN(radTemp) && !double.IsInfinity(radTemp))
                    pfr_temp = Math.Min(Math.Max(radTemp * 1.5, OptimalTemp), ZeroPowerTemp);
                else
                    pfr_temp = OptimalTemp;

                return pfr_temp;
            }
            return base.GetCoreTempAtRadiatorTemp(radTemp);
        }

        public override double GetThermalPowerAtTemp(double temp)
        {
            if (reactorType == 4 || heatThrottling)
            {
                double rel_temp_diff;
                if (temp > OptimalTemp && temp < ZeroPowerTemp)
                    rel_temp_diff = Math.Pow((ZeroPowerTemp - temp) / (ZeroPowerTemp - OptimalTemp), thermalRatioEfficiencyModifier);
                else
                    rel_temp_diff = 1;

                return MaximumPower * rel_temp_diff;
            }
            return base.GetThermalPowerAtTemp(temp);
        }


    }
}
