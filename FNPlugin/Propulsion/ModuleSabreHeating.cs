using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using System.Linq;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    class ModuleSabreHeating : PartModule
    {
        // State
        [KSPField(isPersistant = true)]
        public bool IsEnabled;

        // Configs
        [KSPField]
        public double missingPrecoolerProportionExponent = 0.5;

        // Gui
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ModuleSabreHeating_MissingPrecoolerRatio")]//Missing Precooler Ratio
        double missingPrecoolerRatio;

        // Modules
        ModuleEnginesFX rapier_engine;
        ModuleEngines rapier_engine2;

        // Help
        double _preCoolersActiveArea;
        double _openIntakesArea;
        double _temp1;
        double _temp2;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            rapier_engine = part.FindModulesImplementing<ModuleEnginesFX>().FirstOrDefault(e => e.engineID == "AirBreathing");
            rapier_engine2 = part.FindModulesImplementing<ModuleEngines>().FirstOrDefault();
        }

        public override void OnUpdate()
        {
            if (rapier_engine != null && rapier_engine.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                UnityEngine.Debug.Log("[KSPI]: ModuleSableHeating on " + part.name + " was Force Activated");
                part.force_activate();
            }

            if (rapier_engine2 != null && rapier_engine2.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                UnityEngine.Debug.Log("[KSPI]: ModuleSableHeating on " + part.name + " was Force Activated");
                part.force_activate();
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            try
            {
                var activePreCoolers =  vessel.FindPartModulesImplementing<FNModulePreecooler>().Where(prc => prc.functional).ToList();
                _preCoolersActiveArea = activePreCoolers.Any() ? activePreCoolers.Sum(prc => prc.area) : 0;

                var openIntakes = vessel.FindPartModulesImplementing<AtmosphericIntake>().Where(mre => mre.intakeOpen).ToList();
                _openIntakesArea = openIntakes.Any()  ? openIntakes.Sum(mre => mre.area) : 0;

                missingPrecoolerRatio = _openIntakesArea > 0 ? Math.Min(1, Math.Max(0, Math.Pow(Math.Max(0, _openIntakesArea - _preCoolersActiveArea) / _openIntakesArea, missingPrecoolerProportionExponent))) : 0;
                missingPrecoolerRatio = missingPrecoolerRatio.IsInfinityOrNaN() ? 1 : missingPrecoolerRatio;

                if (rapier_engine != null && vessel.atmDensity > 0)
                {
                    if (rapier_engine.isOperational && rapier_engine.currentThrottle > 0 && rapier_engine.useVelCurve)
                    {
                        _temp1 = Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 10.0 / GameConstants.atmospheric_non_precooled_limit) * part.maxTemp * missingPrecoolerRatio, 1);
                        if (_temp1 >= (part.maxTemp - 10))
                        {
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ModuleSabreHeating_PostMsg"), 5.0f, ScreenMessageStyle.UPPER_CENTER);//"Engine Shutdown: Catastrophic overheating was imminent!"
                            rapier_engine.Shutdown();
                            part.temperature = 1;
                            return;
                        }
                        else
                            part.temperature = _temp1;
                    }
                    else
                        part.temperature = 1;
                }

                if (rapier_engine2 != null && vessel.atmDensity > 0)
                {
                    if (rapier_engine2.isOperational && rapier_engine2.currentThrottle > 0 && rapier_engine2.useVelCurve)
                    {
                        _temp2 = Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20.0 / GameConstants.atmospheric_non_precooled_limit) * part.maxTemp * missingPrecoolerRatio, 1);
                        if (_temp2 >= (part.maxTemp - 10))
                        {
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ModuleSabreHeating_PostMsg"), 5.0f, ScreenMessageStyle.UPPER_CENTER);//"Engine Shutdown: Catastrophic overheating was imminent!"
                            rapier_engine2.Shutdown();
                            part.temperature = 1;
                        }
                        else
                            part.temperature = _temp2;
                    }
                    else
                        part.temperature = 1;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[KSPI]: ModuleSabreHeating threw Exception in FixedUpdate(): " + ex);
            }
        }
    }
}
