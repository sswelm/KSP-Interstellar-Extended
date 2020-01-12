using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    class FNSimpleEngineIgnatorFX : PartModule
    {
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Ignitions")]
        public int initialIgnitions = -1;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Ignitions")]
        public int remainingIgnitions = -1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Reload required experience level")]
        public int reloadRequiresExperienceLevel = 1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Reload requires Engeneer")]
        public bool reloadRequiresEngineer = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Reload requires landed vessel")]
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

        [KSPEvent(name = "ReloadIgnitor", guiName = "Reload Ignitors", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.5f)]
        public void ReloadIgnitor()
        {
            var kervalOnEva = FlightGlobals.ActiveVessel.GetVesselCrew().First();

            if (reloadRequiresLandedVessel && part.vessel.Landed == false)
            {
                var message = "Failed to reload Ignitors. Vessel must be landed on firm ground";
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 6.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            var meetProfessionRequirement = reloadRequiresEngineer == false || kervalOnEva.experienceTrait.Title.Equals("Engineer");

            if (meetProfessionRequirement && kervalOnEva.experienceLevel >= reloadRequiresExperienceLevel)
            {
                var message = "Reloaded Ignitor";
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 6.0f, ScreenMessageStyle.UPPER_CENTER);
                remainingIgnitions = initialIgnitions;
                if (_engineFX != null)
                    _engineFX.maxFuelFlow = maxFuelFlow;
            }
            else
            {
                var message = reloadRequiresEngineer
                    ? "Failed to reload Ignitor. Requires at least a level " + reloadRequiresExperienceLevel + " Engineer"
                    : "Failed to reload Ignitor. Requires at least a level " + reloadRequiresExperienceLevel + " Kerbal";
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
                var message = "Failed to ignite engine, no remaining ignitors!";
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
