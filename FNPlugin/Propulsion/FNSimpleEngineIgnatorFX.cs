using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    class FNSimpleEngineIgnatorFX : PartModule
    {
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Initial Ignitions")]
        public int initialIgnitions = -1;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Available Ignitions")]
        public int remainingIgnitions = -1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Reload required experience level")]
        public int reloadRequiresExperienceLevel = 1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Reload requires Engeneer")]
        public bool reloadRequiresEngineer = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Reload requires landed vessel")]
        public bool reloadRequiresLandedVessel = true;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Currently Ignited")]
        public bool previousEngineIgnited;

        ModuleEnginesFX _engineFX;
        BaseEvent _reloadIgnitorEvent;

        public override void OnStart(StartState state)
        {
            _engineFX = part.FindModuleImplementing<ModuleEnginesFX>();
            _reloadIgnitorEvent = Events["ReloadIgnitor"];

            if (state == StartState.Editor)
                remainingIgnitions = initialIgnitions;
        }

        [KSPEvent(name = "ReloadIgnitor", guiName = "Reload Ignitor", active = true, externalToEVAOnly = true, guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ReloadIgnitor()
        {
            var kervalOnEva = FlightGlobals.ActiveVessel.GetVesselCrew().First();

            if (reloadRequiresLandedVessel && part.vessel.Landed == false)
            {
                var message = "Failed to reload Ignitor. Vessel must be landed on firm ground";
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 10.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            var meetProfessionRequirement = reloadRequiresEngineer == false || kervalOnEva.experienceTrait.Title.Equals("Engineer");

            if (meetProfessionRequirement && kervalOnEva.experienceLevel >= reloadRequiresExperienceLevel)
            {
                remainingIgnitions = initialIgnitions;
            }
            else
            {
                var message = reloadRequiresEngineer
                    ? "Failed to reload Ignitor. Requires at least a level " + reloadRequiresExperienceLevel + " Engineer"
                    : "Failed to reload Ignitor. Requires at least a level " + reloadRequiresExperienceLevel + " Kerbal";
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 10.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public override void OnUpdate()
        {
            if (FlightGlobals.ActiveVessel != null)
                _reloadIgnitorEvent.guiActiveUnfocused = FlightGlobals.ActiveVessel.isEVA;
        }

        public void FixedUpdate()
        {
            if (_engineFX == null)
                return;

            if (previousEngineIgnited == false && _engineFX.requestedThrottle > 0)
                AttempToIgniteEngine();

            previousEngineIgnited = _engineFX.EngineIgnited;
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
            }
        }
    }
}
