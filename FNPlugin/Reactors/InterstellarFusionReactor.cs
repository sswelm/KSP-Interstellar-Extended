using System;
using System.Linq;
using KSP.Localization;
using FNPlugin.Powermanagement.Interfaces;
using UnityEngine;

namespace FNPlugin.Reactors
{
    abstract class InterstellarFusionReactor : InterstellarReactor, IFNChargedParticleSource
    {
        [KSPField(isPersistant = true)] public bool allowJumpStart = true;

        [KSPField] public int powerPriority = 0;
        [KSPField] public bool powerIsAffectedByLithium = true;

        [KSPField] public double minimumLithiumModifier = 0.001;
        [KSPField] public double maximumLithiumModifier = 1;
        [KSPField] public double lithiumModifierExponent = 0.5;
        [KSPField] public double maximumChargedIspMult = 100;
        [KSPField] public double minimumChargedIspMult = 1;
        [KSPField] public double maintenancePowerWasteheatRatio = 0.1;
        [KSPField] public double reactorRatioThreshold = 0.000005;
        [KSPField] public double minReactorRatio = 0;

        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionPB_Maintance")]
        public string electricPowerMaintenance;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionPB_PlasmaRatio")]
        public double plasma_ratio = 1;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionPB_PlasmaModifier", guiFormat = "F6")]
        public double plasma_modifier = 1;
        [KSPField(advancedTweakable = true, guiActive = false, groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionPB_RequiredRatio", guiFormat = "F3")]
        public double required_reactor_ratio;

        // Properties
        public double MaximumChargedIspMult => maximumChargedIspMult;
        public double MinimumChargdIspMult => minimumChargedIspMult;
        public override double MaximumThermalPower => Math.Max(base.MaximumThermalPower * PlasmaModifier * lithium_modifier, 0);
        public override double MaximumChargedPower => base.MaximumChargedPower * PlasmaModifier;
        public override bool IsFuelNeutronRich => !CurrentFuelMode.Aneutronic && CurrentFuelMode.NeutronsRatio > 0;
        public double NormalizedPowerRequirement => PowerRequirement * CurrentFuelMode.NormalizedPowerRequirements;

        public double PowerRequirement
        {
            get
            {
                var gainFactor = FusionEnergyGainFactor;
                return gainFactor > 0 ? RawPowerOutput / gainFactor : 0;
            }
        }

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
                var modifier = CheatOptions.InfinitePropellant || !powerIsAffectedByLithium || ChargedPowerRatio >= 1 || minimumLithiumModifier >= 1 ? 1
                    : totalAmountLithium > 0
                        ? Math.Min(maximumLithiumModifier, Math.Max(minimumLithiumModifier, Math.Pow(totalAmountLithium / totalMaxAmountLithium, lithiumModifierExponent)))
                        : minimumLithiumModifier;

                return modifier;
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

        public override void OnStart(StartState state)
        {
            InitialiseGainFactors();

            base.OnStart(state);
            Fields["lithium_modifier"].guiActive = powerIsAffectedByLithium;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FissionPB_NextFusionMode", active = true)]//Next Fusion Mode
        public void NextFusionModeEvent()
        {
            SwitchToNextFuelMode(fuel_mode);
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FissionPB_PreviousFusionMode", active = true)]//Previous Fusion Mode
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

        protected override void WindowReactorControlSpecificOverride()
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

            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_FissionPB_CurrentMaxMaintenance"), electricPowerMaintenance, boldStyle, textStyle);//"Current/Max Fusion Maintenance"
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

            if (fuelModes == null)
            {
                Debug.Log("[KSPI]: FusionReactor SetDefaultFuelMode - load fuel modes");
                fuelModes = GetReactorFuelModes();
            }

            if (!string.IsNullOrEmpty(fuel_mode_name) && fuelModes.Any(m => m.ModeGUIName == fuel_mode_name))
            {
                CurrentFuelMode = fuelModes.First(m => m.ModeGUIName == fuel_mode_name);
            }
            else if (!string.IsNullOrEmpty(fuel_mode_variant) && fuelModes.Any(m => m.Variants.Any(l => l.Name == fuel_mode_variant)))
            {
                CurrentFuelMode = fuelModes.First(m => m.Variants.Any(l => l.Name == fuel_mode_variant));
            }
            else if (fuelmode_index >= 0 && fuelModes.Any(m => m.Index == fuelmode_index))
            {
                CurrentFuelMode = fuelModes.First(m => m.Index == fuelmode_index);
            }
            else if (fuelModes.Any(m => m.Index == fuel_mode))
            {
                CurrentFuelMode = fuelModes.First(m => m.Index == fuel_mode);
            }
            else
            {
                CurrentFuelMode = (fuel_mode < fuelModes.Count) ? fuelModes[fuel_mode] : fuelModes.FirstOrDefault();
            }

            fuel_mode = fuelModes.IndexOf(CurrentFuelMode);
            fuelmode_index = fuel_mode;
        }

        public override int getPowerPriority()
        {
            return powerPriority;
        }
    }
}
