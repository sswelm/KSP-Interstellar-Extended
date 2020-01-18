using System;
using System.Linq;
using FNPlugin.Redist;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Reactors
{
    abstract class InterstellarFusionReactor : InterstellarReactor, IFNChargedParticleSource
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool allowJumpStart = true;

        [KSPField]
        public double magneticNozzlePowerMult = 1;
        [KSPField]
        public int powerPriority = 0;
        [KSPField]
        public bool powerIsAffectedByLithium = true;

        [KSPField]
        public double minimumLithiumModifier = 0.001;
        [KSPField]
        public double maximumLithiumModifier = 1;
        [KSPField]
        public double lithiumModifierExponent = 0.5;
        [KSPField]
        public double maximumChargedIspMult = 100;
        [KSPField]
        public double minimumChargdIspMult = 1;
        [KSPField]
        public double maintenancePowerWasteheatRatio = 0.1;

        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FissionPB_Maintance")]//Maintance
        public string electricPowerMaintenance;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FissionPB_PlasmaRatio")]//Plasma Ratio
        public double plasma_ratio = 1;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FissionPB_PlasmaModifier", guiFormat = "F6")]//Plasma Modifier
        public double plasma_modifier = 1;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FissionPB_IsSwappingFuelMode")]//Is Swapping Fuel Mode
        public bool isSwappingFuelMode;

        [KSPField]
        public double reactorRatioThreshold = 0.000005;
        [KSPField]
        public double minReactorRatio = 0;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FissionPB_RequiredRatio", guiFormat = "F4")]//Required Ratio
        public double required_reactor_ratio;

        public double MaximumChargedIspMult { get { return maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return minimumChargdIspMult; } }

        public override double StableMaximumReactorPower 
        { 
            get 
            {
                var stablePower = base.StableMaximumReactorPower;

                return stablePower * ChargedPowerRatio + stablePower * ThermalPowerRatio * lithium_modifier;
            } 
        }

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
                var modifier = CheatOptions.InfinitePropellant || !powerIsAffectedByLithium || minimumLithiumModifier == 1 ? 1
                    : totalAmountLithium > 0
                        ? Math.Min(maximumLithiumModifier, Math.Max(minimumLithiumModifier, Math.Pow(totalAmountLithium / totalMaxAmountLithium, lithiumModifierExponent)))
                        : minimumLithiumModifier;

                return modifier;
            }
        }

        public override double MaximumThermalPower { get { return Math.Max(base.MaximumThermalPower * PlasmaModifier * lithium_modifier, 0); } }

        public override double MaximumChargedPower { get { return base.MaximumChargedPower * PlasmaModifier; }  }

        public override double MagneticNozzlePowerMult { get { return magneticNozzlePowerMult; } }

        public override bool IsFuelNeutronRich { get { return !CurrentFuelMode.Aneutronic && CurrentFuelMode.NeutronsRatio > 0; } }

        public double PowerRequirement { get { return RawPowerOutput / FusionEnergyGainFactor; } }

        public double NormalizedPowerRequirment { get { return PowerRequirement * CurrentFuelMode.NormalisedPowerRequirements; } }

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

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FissionPB_NextFusionMode", active = true)]//Next Fusion Mode
        public void NextFusionModeEvent()
        {
            SwitchToNextFuelMode(fuel_mode);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FissionPB_PreviousFusionMode", active = true)]//Previous Fusion Mode
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
            if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_FissionPB_NextModebutton"), GUILayout.ExpandWidth(true)))//"Next Fusion Mode"
            {
                NextFusionModeEvent();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_FissionPB_PreviousModebutton"), GUILayout.ExpandWidth(true)))//"Previous Fusion Mode"
            {
                PreviousFusionModeEvent();
            }
            GUILayout.EndHorizontal();

            PrintToGUILayout(Localizer.Format("#LOC_KSPIE_FissionPB_CurrentMaxMaintenance"), electricPowerMaintenance, bold_style, text_style);//"Current/Max Fusion Maintenance"
        }

        public override void OnFixedUpdate()
        {
            lithium_modifier = LithiumModifier;

            base.OnFixedUpdate();

            // determine amount of power needed
            required_reactor_ratio = Math.Max(minReactorRatio, reactor_power_ratio >= reactorRatioThreshold ? reactor_power_ratio : 0);
        }

        public override void SetDefaultFuelMode()
        {
            Debug.Log("[KSPI]: FusionReactor SetDefaultFuelMode");

            if (fuel_modes == null)
            {
                Debug.Log("[KSPI]: FusionReactor SetDefaultFuelMode - load fuel modes");
                fuel_modes = GetReactorFuelModes();
            }

            if (!string.IsNullOrEmpty(fuel_mode_name) && fuel_modes.Any(m => m.ModeGUIName == fuel_mode_name))
            {
                CurrentFuelMode = fuel_modes.First(m => m.ModeGUIName == fuel_mode_name);
            }
            else if (!string.IsNullOrEmpty(fuel_mode_variant) && fuel_modes.Any(m => m.Variants.Any(l => l.Name == fuel_mode_variant)))
            {
                CurrentFuelMode = fuel_modes.First(m => m.Variants.Any(l => l.Name == fuel_mode_variant));
            }
            else if (fuelmode_index >= 0 && fuel_modes.Any(m => m.Index == fuelmode_index))
            {
                CurrentFuelMode = fuel_modes.First(m => m.Index == fuelmode_index);
            }
            else if (fuel_modes.Any(m => m.Index == fuel_mode))
            {
                CurrentFuelMode = fuel_modes.First(m => m.Index == fuel_mode);
            }
            else
            {
                CurrentFuelMode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
            }

            fuel_mode = fuel_modes.IndexOf(CurrentFuelMode);
        }

        public override int getPowerPriority()
        {
            return powerPriority;
        }
    }
}