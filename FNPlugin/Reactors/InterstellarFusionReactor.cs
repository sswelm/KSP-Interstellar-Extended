using System;
using System.Linq;
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
        public double fusionEnergyGainFactorMk1 = 10;
        [KSPField(isPersistant = false)]
		public double fusionEnergyGainFactorMk2 = 20;
        [KSPField(isPersistant = false)]
		public double fusionEnergyGainFactorMk3 = 40;
        [KSPField(isPersistant = false)]
		public double fusionEnergyGainFactorMk4 = 80;
        [KSPField(isPersistant = false)]
		public double fusionEnergyGainFactorMk5 = 120;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Maintance")]
        public string electricPowerMaintenance;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Plasma Ratio")]
        public double plasma_ratio = 1.0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Plasma Modifier", guiFormat = "F6")]
        public double plasma_modifier = 1.0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Lithium Modifier", guiFormat = "F6")]
        public double lithium_modifier = 1.0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Is Swapping Fuel Mode")]
        public bool isSwappingFuelMode = false;

        //public float
        protected PartResource lithiumPartResource = null;

        public double MaximumChargedIspMult { get { return 100; } }

		public double MinimumChargdIspMult { get { return 1; } }

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
                    : powerIsAffectedByLithium && totalAmountLithium > 0
                        ? Math.Sqrt(totalAmountLithium / totalMaxAmountLithium)
                        : 0.001;

                return lithium_modifier;
            }
        }

        public override double MaximumThermalPower
        {
            get { return Math.Max(base.MaximumThermalPower * PlasmaModifier * LithiumModifier, 0.000000001f); }
        }

        public override double MaximumChargedPower
        {
            get { return base.MaximumChargedPower * PlasmaModifier; }
        }

        public virtual double CurrentMeVPerChargedProduct { get { return CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct : 0; } }

        public override bool IsFuelNeutronRich { get { return !CurrentFuelMode.Aneutronic; } }

        public double PowerRequirement { get { return RawPowerOutput / FusionEnergyGainFactor; } }

        public double NormalizedPowerRequirment { get { return PowerRequirement * CurrentFuelMode.NormalisedPowerRequirements; } }


        public double FusionEnergyGainFactor
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

            CurrentFuelMode = fuel_modes[fuel_mode];
            fuel_mode_name = CurrentFuelMode.ModeGUIName;

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initial_fuel_mode)
                SwitchToNextFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }

        private bool FullFuelRequirments()
        {
            return HasAllFuels() && FuelRequiresLab(CurrentFuelMode.RequiresLab);
        }

        private bool HasAllFuels()
        {
            if (CheatOptions.InfinitePropellant)
                return true;

            bool hasAllFuels = true;

            foreach (var fuel in current_fuel_variants_sorted.First().ReactorFuels)
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

            CurrentFuelMode = fuel_modes[fuel_mode];
            fuel_mode_name = CurrentFuelMode.ModeGUIName;

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
            if (string.IsNullOrEmpty(fuel_mode_name) && fuel_modes.Any(m => m.ModeGUIName == fuel_mode_name))
                CurrentFuelMode = fuel_modes.First(m => m.ModeGUIName == fuel_mode_name);
            else if (fuelmode_index >= 0 && fuel_modes.Any(m => m.Index == fuelmode_index))
                CurrentFuelMode = fuel_modes.First(m => m.Index == fuelmode_index);
            else if (fuel_modes.Any(m => m.Index == fuel_mode))
                CurrentFuelMode = fuel_modes.First(m => m.Index == fuel_mode);
            else
                CurrentFuelMode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();

            fuel_mode = fuel_modes.IndexOf(CurrentFuelMode);
            fuel_mode_name = CurrentFuelMode.ModeGUIName;
        }
    }
}