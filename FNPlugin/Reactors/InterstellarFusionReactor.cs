using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    abstract class InterstellarFusionReactor : InterstellarReactor, IChargedParticleSource
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField(isPersistant = true)]
        public string fuel_mode_name = string.Empty;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool allowJumpStart = true;

        [KSPField(isPersistant = false)]
        public bool powerIsAffectedByLithium = true;

        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk1 = 10;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk2 = 20;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk3 = 40;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk4 = 80;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk5 = 120;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Maintance")]
        public string electricPowerMaintenance;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Plasma Ratio")]
        public float plasma_ratio = 1.0f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Plasma Modifier", guiFormat = "F6")]
        public float plasma_modifier = 1.0f;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Lithium Modifier", guiFormat = "F6")]
        public double lithium_modifier = 1.0f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Is Swapping Fuel Mode")]
        public bool isSwappingFuelMode = false;

        //public float
        protected PartResource lithiumPartResource = null;

        public float MaximumChargedIspMult { get { return 100 ; } }

        public float MinimumChargdIspMult { get { return 1; } }

        public override double StableMaximumReactorPower { get { return IsEnabled && plasma_ratio >= 1 ? RawPowerOutput : 0; } }

        public virtual double PlasmaModifier
        {
            get 
            {
                plasma_modifier = plasma_ratio >= 1 ? 1 : 0;
                return plasma_modifier;
            }
        }

        public double LithiumModifier
        {
            get 
            {
                lithium_modifier = CheatOptions.InfinitePropellant ? 1 
                    : powerIsAffectedByLithium && lithiumPartResource != null && lithiumPartResource.maxAmount > 0 
                        ? Math.Sqrt(lithiumPartResource.amount / lithiumPartResource.maxAmount) 
                        : 1;

                return lithium_modifier;
            }
        }

        public override double MaximumThermalPower
        {
            get {  return Math.Max(base.MaximumThermalPower * PlasmaModifier * LithiumModifier, 0.000000001f); }
        }

        public override double MaximumChargedPower 
        {
            get { return base.MaximumChargedPower * PlasmaModifier; } 
        }

        public virtual double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsFuelNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        public double PowerRequirement { get { return RawPowerOutput / FusionEnergyGainFactor; } }

        public double NormalizedPowerRequirment { get { return PowerRequirement * current_fuel_mode.NormalisedPowerRequirements; } }


        public float FusionEnergyGainFactor
        {
            get
            {
                if (CurrentGenerationType == GenerationType.Mk5)
                    return fusionEnergyGainFactorMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    return fusionEnergyGainFactorMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    return fusionEnergyGainFactorMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    return fusionEnergyGainFactorMk2;
                else
                    return fusionEnergyGainFactorMk1;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            lithiumPartResource = part.Resources.FirstOrDefault(r => r.resourceName == InterstellarResourcesConfiguration.Instance.Lithium7);

            // call Interstellar Reactor Onstart
            base.OnStart(state);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Next Fusion Mode", active = true)]
        public void NextFusionModeEvent()
        {
            SwitchToNextFuelMode(fuel_mode);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Previous Fusion Mode", active = true)]
        public void PreviousFusionModeEvent()
        {
            SwitchToPreviousFuelMode(fuel_mode);
        }

        [KSPAction("Next Fusion Mode")]
        public void NextFusionModeAction(KSPActionParam param)
        {
            NextFusionModeEvent();
        }

        [KSPAction("Previous Fusion Mode")]
        public void PreviousFusionModeAction(KSPActionParam param)
        {
            PreviousFusionModeEvent();
        }

        private void SwitchToNextFuelMode(int initial_fuel_mode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count)
                fuel_mode = 0;

            current_fuel_mode = fuel_modes[fuel_mode];
            fuel_mode_name = current_fuel_mode.ModeGUIName;

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initial_fuel_mode)
                SwitchToNextFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }

        private bool FullFuelRequirments()
        {
            return HasAllFuels() && FuelRequiresLab(current_fuel_mode.RequiresLab);
        }

        private bool HasAllFuels()
        {
            if (CheatOptions.InfinitePropellant)
                return true;

            bool hasAllFuels = true;

            foreach (var fuel in current_fuel_mode.ReactorFuels)
            {
                if (GetFuelRatio(fuel, FuelEfficiency, NormalisedMaximumPower) < 1)
                {
                    hasAllFuels = false;
                    break;
                }
            }
            return hasAllFuels;
        }

        private void SwitchToPreviousFuelMode(int initial_fuel_mode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuel_modes.Count - 1;

            current_fuel_mode = fuel_modes[fuel_mode];
            fuel_mode_name = current_fuel_mode.ModeGUIName;

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initial_fuel_mode)
                SwitchToPreviousFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }

        protected override void WindowReactorSpecificOverride()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Next Fusion Mode", GUILayout.ExpandWidth(true)))
            {
                NextFusionModeEvent();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous Fusion Mode", GUILayout.ExpandWidth(true)))
            {
                PreviousFusionModeEvent();
            }
            GUILayout.EndHorizontal();

            PrintToGUILayout("Fusion Maintenance", electricPowerMaintenance, bold_style, text_style);
        }

        protected override void setDefaultFuelMode()
        {
            if (String.IsNullOrEmpty(fuel_mode_name) && fuel_modes.Any(m => m.ModeGUIName == fuel_mode_name))
            {
                current_fuel_mode = fuel_modes.First(m => m.ModeGUIName == fuel_mode_name);
                fuel_mode = fuel_modes.IndexOf(current_fuel_mode);
                return;
            }

            current_fuel_mode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
            fuel_mode_name = current_fuel_mode.ModeGUIName;
        }
    }
}
