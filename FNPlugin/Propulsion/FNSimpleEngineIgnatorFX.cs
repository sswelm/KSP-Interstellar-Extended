using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Propulsion
{
    class FNSimpleEngineIgnatorFX : PartModule
    {
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNSimpleEngineIgnatorFX_InitialIgnitions")]//Ignitions
        public int initialIgnitions = -1;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_FNSimpleEngineIgnatorFX_RemainingIgnitions")]//Ignitions
        public int remainingIgnitions = -1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNSimpleEngineIgnatorFX_ReloadRequiredExperienceLevel")]//Reload required experience level
        public int reloadRequiresExperienceLevel = 1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNSimpleEngineIgnatorFX_ReloadRequiresEngeneer")]//Reload requires Engeneer
        public bool reloadRequiresEngineer = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FNSimpleEngineIgnatorFX_ReloadRequiresLandedVessel")]//Reload requires landed vessel
        public bool reloadRequiresLandedVessel = true;

        float maxFuelFlow;
        float previousThrottle;

        ModuleEnginesFX _engineFX;
        BaseEvent _reloadIgnitorEvent;

        public override void OnStart(StartState state)
        {
            _engineFX = part.FindModuleImplementing<ModuleEnginesFX>();
            _reloadIgnitorEvent = Events["ReloadIgnitor"];
            maxFuelFlow = _engineFX.maxFuelFlow;

            if (remainingIgnitions == 0 && _engineFX != null)
                _engineFX.maxFuelFlow = 1e-10f;

            if (state == StartState.Editor)
                remainingIgnitions = initialIgnitions;
        }

        [KSPEvent(name = "ReloadIgnitor", guiName = "#LOC_KSPIE_FNSimpleEngineIgnatorFX_ReloadIgnitors", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Reload Ignitors
        public void ReloadIgnitor()
        {
            var kervalOnEva = FlightGlobals.ActiveVessel.GetVesselCrew().First();

            if (reloadRequiresLandedVessel && part.vessel.Landed == false)
            {
                var message = Localizer.Format("#LOC_KSPIE_FNSimpleEngineIgnatorFX_PostMsg1");//"Failed to reload Ignitors. Vessel must be landed on firm ground"
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 6.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            var meetProfessionRequirement = reloadRequiresEngineer == false || kervalOnEva.experienceTrait.Title.Equals("Engineer");

            if (meetProfessionRequirement && kervalOnEva.experienceLevel >= reloadRequiresExperienceLevel)
            {
                var message = Localizer.Format("#LOC_KSPIE_FNSimpleEngineIgnatorFX_PostMsg2");//"Reloaded Ignitor"
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 6.0f, ScreenMessageStyle.UPPER_CENTER);
                remainingIgnitions = initialIgnitions;
                if (_engineFX != null)
                    _engineFX.maxFuelFlow = maxFuelFlow;
            }
            else
            {
                var message = reloadRequiresEngineer
                    ? Localizer.Format("#LOC_KSPIE_FNSimpleEngineIgnatorFX_PostMsg3", reloadRequiresExperienceLevel)//"Failed to reload Ignitor. Requires at least a level " +  + " Engineer"
                    : Localizer.Format("#LOC_KSPIE_FNSimpleEngineIgnatorFX_PostMsg4", reloadRequiresExperienceLevel);//"Failed to reload Ignitor. Requires at least a level " +  + " Kerbal"
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 6.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public void FixedUpdate()
        {
            if (_engineFX == null)
                return;

            if (previousThrottle == 0 && _engineFX.requestedThrottle > 0)
                AttempToIgniteEngine();

            if (remainingIgnitions == 0 && _engineFX.currentThrottle == 0)
                _engineFX.maxFuelFlow = 1e-10f;

            previousThrottle = _engineFX.requestedThrottle;
        }

        private void AttempToIgniteEngine()
        {
            if (initialIgnitions < 0)
                return;

            if (remainingIgnitions > 0)
                remainingIgnitions--;
            else
            {
                var message = Localizer.Format("#LOC_KSPIE_FNSimpleEngineIgnatorFX_PostMsg5");//"Failed to ignite engine, no remaining ignitors!"
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 10f, ScreenMessageStyle.UPPER_CENTER);

                _engineFX.part.Effects.Event(_engineFX.flameoutEffectName, _engineFX.transform.hierarchyCount);
                _engineFX.SetRunningGroupsActive(false);

                foreach (BaseEvent baseEvent in _engineFX.Events)
                {
                    if (baseEvent.name.IndexOf("shutdown", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        baseEvent.Invoke();
                    }
                }
                _engineFX.SetRunningGroupsActive(false);
                _engineFX.maxFuelFlow = 1e-10f;
            }
        }
    }
}
