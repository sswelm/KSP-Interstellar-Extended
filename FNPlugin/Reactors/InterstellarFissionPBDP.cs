using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Fission Reactor")]
    class InterstellarFissionPBDP : InterstellarReactor, IChargedParticleSource
    {
        // Persistant False
        [KSPField(isPersistant = false)]
        public bool heatThrottling = false;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiUnits= "%", guiName = "Overheating", guiFormat = "F3")]
        public double overheatPercentage;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Wasteheat Ratio")]
        public double resourceBarRatio;
        [KSPField(isPersistant = false)]
        public float thermalRatioEfficiencyModifier = 0.81f;
        [KSPField(isPersistant = false)]
        public float maximumChargedIspMult = 114f;
        [KSPField(isPersistant = false)]
        public float minimumChargdIspMult = 11.4f;
        [KSPField(isPersistant = false)]
        public float coreTemperatureWasteheatPower = 0.3f;
        [KSPField(isPersistant = false)]
        public float coreTemperatureWasteheatModifier = -0.2f;
        [KSPField(isPersistant = false)]
        public float coreTemperatureWasteheatMultiplier = 1.25f;

        private double optimalTempDifference;
     

        [KSPEvent(guiName = "Manual Restart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]
        public void ManualRestart()
        {
            IsEnabled = true;
        }

        [KSPEvent(guiName = "Manual Shutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        public float MaximumChargedIspMult { get { return maximumChargedIspMult; } }

        public float MinimumChargdIspMult { get { return minimumChargdIspMult; } }

        public double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsFuelNeutronRich { get { return current_fuel_mode != null && !current_fuel_mode.Aneutronic; } }

        public override double MaximumThermalPower { get { return base.MaximumThermalPower * (float)ThermalRatioEfficiency; } }

        public override double MaximumChargedPower { get { return base.MaximumChargedPower * (float)ThermalRatioEfficiency; } }

        private double ThermalRatioEfficiency
        {
            get 
            { 
                return reactorType == 4 || heatThrottling 
                    ? Math.Pow((ZeroPowerTemp - CoreTemperature) / optimalTempDifference, thermalRatioEfficiencyModifier) 
                    : 1; 
            }
        }

        private double OptimalTemp { get { return base.CoreTemperature; } }

        private double ZeroPowerTemp { get { return base.CoreTemperature * 1.25f; } }

        public override bool IsNuclear { get { return true; } }

        public override double CoreTemperature
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight && (reactorType == 4 || heatThrottling) ) 
                {
                    resourceBarRatio = CheatOptions.IgnoreMaxTemperature ? 0 : getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT);

                    var temperatureIncrease = Math.Max(Math.Pow(resourceBarRatio, coreTemperatureWasteheatPower) + coreTemperatureWasteheatModifier, 0) * coreTemperatureWasteheatMultiplier * optimalTempDifference;

                    return Math.Min(Math.Max(OptimalTemp + temperatureIncrease, OptimalTemp), ZeroPowerTemp);
                } 
                return base.CoreTemperature;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            overheatPercentage = (1 - ThermalRatioEfficiency) * 100;

            optimalTempDifference = ZeroPowerTemp - OptimalTemp;
        }
      

        public override void OnUpdate()
        {
            overheatPercentage = (1 - ThermalRatioEfficiency) * 100;
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            base.OnUpdate();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }

        public override double GetCoreTempAtRadiatorTemp(double rad_temp)
        {
            if (heatThrottling)
            {
                double pfr_temp = 0;

                if (!double.IsNaN(rad_temp) && !double.IsInfinity(rad_temp))
                    pfr_temp = Math.Min(Math.Max(rad_temp * 1.5, OptimalTemp), ZeroPowerTemp);
                else
                    pfr_temp = OptimalTemp;

                return pfr_temp;
            }
            return base.GetCoreTempAtRadiatorTemp(rad_temp);
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
