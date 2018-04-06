using System;
using System.Linq;
using FNPlugin.Reactors.Interfaces;
using UnityEngine;

namespace FNPlugin.Reactors
{
    abstract class InterstellarFusionReactor : InterstellarReactor, IChargedParticleSource
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = true)]
        public string fuel_mode_name = string.Empty;
        [KSPField(isPersistant = true, guiActive = false)]
        public bool allowJumpStart = true;


        [KSPField]
        public int powerPriority = 0;
        [KSPField]
        public bool powerIsAffectedByLithium = true;

        [KSPField]
        public double maximumChargedIspMult = 100;
        [KSPField]
        public double minimumChargdIspMult = 1;
        [KSPField]
        public double maintenancePowerWasteheatRatio = 0.1;

        [KSPField(guiActive = false, guiName = "Maintance")]
        public string electricPowerMaintenance;
        [KSPField(guiActive = true, guiName = "Plasma Ratio")]
        public double plasma_ratio = 1;
        [KSPField(guiActive = false, guiName = "Plasma Modifier", guiFormat = "F6")]
        public double plasma_modifier = 1;
        [KSPField(guiActive = false, guiName = "Lithium Modifier", guiFormat = "F6")]
        public double lithium_modifier = 1;
        [KSPField(guiActive = false, guiName = "Is Swapping Fuel Mode")]
        public bool isSwappingFuelMode;

        [KSPField]
        public double reactorRatioThreshold = 0.000005;
        [KSPField]
        public double minReactorRatio = 0;
        [KSPField(guiActive = false, guiName = "Required Ratio", guiFormat = "F4")]
        public double required_reactor_ratio;

        public GenerationType FuelModeTechLevel
        {
            get { return (GenerationType)fuelModeTechLevel; }
            private set { fuelModeTechLevel = (int)value; }
        }


        public double MaximumChargedIspMult { get { return maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return minimumChargdIspMult; } }

        public override double StableMaximumReactorPower { get { return base.StableMaximumReactorPower * LithiumModifier; } }

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
                lithium_modifier = CheatOptions.InfinitePropellant || !powerIsAffectedByLithium ? 1
                    : totalAmountLithium > 0
                        ? Math.Sqrt(totalAmountLithium / totalMaxAmountLithium)
                        : 0.001;


                return lithium_modifier;
            }
        }

        public override double MaximumThermalPower
        {
            get { return Math.Max(base.MaximumThermalPower * PlasmaModifier * LithiumModifier, 0); }
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
                switch (FuelModeTechLevel)
                {
                    case GenerationType.Mk7:
                        return fusionEnergyGainFactorMk7;
                    case GenerationType.Mk6:
                        return fusionEnergyGainFactorMk6;
                    case GenerationType.Mk5:
                        return fusionEnergyGainFactorMk5;
                    case GenerationType.Mk4:
                        return fusionEnergyGainFactorMk4;
                    case GenerationType.Mk3:
                        return fusionEnergyGainFactorMk3;
                    case GenerationType.Mk2:
                        return fusionEnergyGainFactorMk2;
                    default:
                        return fusionEnergyGainFactorMk1;
                }
            }
        }

        private void InitialiseGainFactors()
        {
            if (fusionEnergyGainFactorMk2 == 0)
                fusionEnergyGainFactorMk2 = fusionEnergyGainFactorMk1;
            if (fusionEnergyGainFactorMk3 == 0)
                fusionEnergyGainFactorMk3 = fusionEnergyGainFactorMk2;
            if (fusionEnergyGainFactorMk4 == 0)
                fusionEnergyGainFactorMk4 = fusionEnergyGainFactorMk3;
            if (fusionEnergyGainFactorMk5 == 0)
                fusionEnergyGainFactorMk5 = fusionEnergyGainFactorMk4;
            if (fusionEnergyGainFactorMk6 == 0)
                fusionEnergyGainFactorMk6 = fusionEnergyGainFactorMk5;
            if (fusionEnergyGainFactorMk7 == 0)
                fusionEnergyGainFactorMk7 = fusionEnergyGainFactorMk6;
        }

        public override void OnStart(PartModule.StartState state)
        {
            InitialiseGainFactors();

            base.OnStart(state);
            Fields["lithium_modifier"].guiActive = powerIsAffectedByLithium;
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

        private void SwitchToNextFuelMode(int initialFuelMode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count)
                fuel_mode = 0;

            stored_fuel_ratio = 1;
            CurrentFuelMode = fuel_modes[fuel_mode];
            fuel_mode_name = CurrentFuelMode.ModeGUIName;

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initialFuelMode)
                SwitchToNextFuelMode(initialFuelMode);

            isSwappingFuelMode = true;
        }

        private void SwitchToPreviousFuelMode(int initialFuelMode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuel_modes.Count - 1;

            CurrentFuelMode = fuel_modes[fuel_mode];
            fuel_mode_name = CurrentFuelMode.ModeGUIName;

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initialFuelMode)
                SwitchToPreviousFuelMode(initialFuelMode);

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

            var hasAllFuels = true;
            foreach (var fuel in current_fuel_variants_sorted.First().ReactorFuels)
            {
                if (!(GetFuelRatio(fuel, FuelEfficiency, NormalisedMaximumPower) < 1)) continue;

                hasAllFuels = false;
                break;
            }
            return hasAllFuels;
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

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            // determine amount of power needed
            required_reactor_ratio = Math.Max(minReactorRatio, reactor_power_ratio >= reactorRatioThreshold ? reactor_power_ratio : 0);
        }

        protected override void SetDefaultFuelMode()
        {
            if (fuel_modes == null)
                return;

            if (!string.IsNullOrEmpty(fuel_mode_name) && fuel_modes.Any(m => m.ModeGUIName == fuel_mode_name))
                CurrentFuelMode = fuel_modes.First(m => m.ModeGUIName == fuel_mode_name);
            else if (fuelmode_index >= 0 && fuel_modes.Any(m => m.Index == fuelmode_index))
                CurrentFuelMode = fuel_modes.First(m => m.Index == fuelmode_index);
            else if (fuel_modes.Any(m => m.Index == fuel_mode))
                CurrentFuelMode = fuel_modes.First(m => m.Index == fuel_mode);
            else
                CurrentFuelMode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();

            fuel_mode = fuel_modes.IndexOf(CurrentFuelMode);

            if (CurrentFuelMode != null)
                fuel_mode_name = CurrentFuelMode.ModeGUIName;
        }

        public override int getPowerPriority()
        {
            return powerPriority;
        }
    }
}