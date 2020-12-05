using FNPlugin.Constants;
using FNPlugin.Powermanagement;
using FNPlugin.Redist;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class VABThermalUI : MonoBehaviour
    {
        public static bool renderWindow;

        protected int thermalWindowId = 825462;
        protected Rect windowPosition = new Rect(300, 60, 300, 100);
        protected GUIStyle bold_label;
        protected GUIStyle green_label;
        protected GUIStyle red_label;
        protected GUIStyle orange_label;
        protected double total_source_power;
        protected double source_temp_at_100pc;
        protected double source_temp_at_30pc;
        protected double rad_max_dissip;
        protected double total_area;
        protected double total_area_display;
        protected double min_source_power;
        protected double resting_radiator_temp_at_100pcnt;
        protected double resting_radiator_temp_at_30pcnt;
        protected double average_rad_temp;
        protected int n_rads;

        protected double au_scale = 1;
        protected bool has_generators;
        protected double generator_efficiency_at_100pcnt;
        protected double generator_efficiency_at_30pcnt;

        public void Start()
        {
            if (PluginHelper.usingToolbar)
                renderWindow = false;
        }


        public void Update()
        {
            if (!renderWindow)
                return;

            // thermal logic
            List<IPowerSource> thermalSources = new List<IPowerSource>();
            List<FNRadiator> radiators = new List<FNRadiator>();
            List<ModuleDeployableSolarPanel> panels = new List<ModuleDeployableSolarPanel>();
            List<FNGenerator> generators = new List<FNGenerator>();

            foreach (Part p in EditorLogic.fetch.ship.parts)
            {
                thermalSources.AddRange(p.FindModulesImplementing<IPowerSource>());
                radiators.AddRange(p.FindModulesImplementing<FNRadiator>());
                panels.AddRange(p.FindModulesImplementing<ModuleDeployableSolarPanel>());
                generators.AddRange(p.FindModulesImplementing<FNGenerator>());
            }

            total_source_power = 0;
            min_source_power = 0;
            source_temp_at_100pc = double.MaxValue;
            source_temp_at_30pc = double.MaxValue;

            foreach (IPowerSource powerSource in thermalSources)
            {
                double combinedMaxStableMegaWattPower = 0;

                var connectedThermalPowerGenerator = powerSource.ConnectedThermalElectricGenerator;
                var connectedChargedPowerGenerator = powerSource.ConnectedChargedParticleElectricGenerator;

                // when connected to a thermal source, assume most thermal energy thermal power can end up in the radiators
                if (connectedThermalPowerGenerator != null)
                    combinedMaxStableMegaWattPower += (1 - powerSource.ChargedPowerRatio) * connectedThermalPowerGenerator.MaxStableMegaWattPower;

                if (connectedChargedPowerGenerator != null)
                {
                    // when a thermal source is not connected to a thermal power generator, all thermal power ends up in the radiators
                    if (connectedThermalPowerGenerator == null)
                        combinedMaxStableMegaWattPower += (1 - powerSource.ChargedPowerRatio) * connectedChargedPowerGenerator.MaxStableMegaWattPower;

                    // only non directly converted power emd up in the radiators
                    var chargedPowerGenerator = connectedChargedPowerGenerator as FNGenerator;
                    if (chargedPowerGenerator != null)
                        combinedMaxStableMegaWattPower += powerSource.ChargedPowerRatio * connectedChargedPowerGenerator.MaxStableMegaWattPower * (1 - chargedPowerGenerator.maxEfficiency);
                }

                // only take reactor power in account when its actually connected to a power generator
                if (connectedThermalPowerGenerator == null && connectedChargedPowerGenerator == null) continue;

                double coreTempAtRadiatorTempAt100Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_100pcnt);
                double coreTempAtRadiatorTempAt30Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_30pcnt);

                source_temp_at_100pc = Math.Min(coreTempAtRadiatorTempAt100Percent, source_temp_at_100pc);
                source_temp_at_30pc = Math.Min(coreTempAtRadiatorTempAt30Percent, source_temp_at_30pc);

                total_source_power += Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt100Percent));
                min_source_power += Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt30Percent) * 0.3);
            }

            foreach (ModuleDeployableSolarPanel panel in panels)
            {
                total_source_power += panel.chargeRate * 0.0005/au_scale/au_scale;
            }

            n_rads = 0;
            rad_max_dissip = 0;
            average_rad_temp = 0;
            total_area = 0;
            total_area_display = 0;

            foreach (FNRadiator radiator in radiators)
            {
                total_area_display += radiator.BaseRadiatorArea;
                double effectiveArea = radiator.EffectiveRadiatorArea;
                total_area += effectiveArea;
                double maxRadTemperature = radiator.MaxRadiatorTemperature;
                maxRadTemperature = Math.Min(maxRadTemperature, source_temp_at_100pc);
                n_rads += 1;
                var tempToPowerFour = maxRadTemperature * maxRadTemperature * maxRadTemperature * maxRadTemperature;
                rad_max_dissip += GameConstants.stefan_const * effectiveArea * tempToPowerFour / 1e6;
                average_rad_temp += maxRadTemperature;
            }
            average_rad_temp = n_rads != 0 ? average_rad_temp / n_rads : double.NaN;

            double radRatio = total_source_power / rad_max_dissip;
            double radRatio30Pc = min_source_power / rad_max_dissip;

            resting_radiator_temp_at_100pcnt = ((!double.IsInfinity(radRatio) && !double.IsNaN(radRatio)) ? Math.Pow(radRatio,0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_30pcnt = ((!double.IsInfinity(radRatio) && !double.IsNaN(radRatio)) ? Math.Pow(radRatio30Pc, 0.25) : 0) * average_rad_temp;

            if (generators.Count > 0)
            {
                var maximumGeneratedPower = generators.Sum(m => m.maximumGeneratorPowerMJ);
                var averageEfficiency = generators.Sum(m => m.maxEfficiency * m.maximumGeneratorPowerMJ) / maximumGeneratedPower;

                has_generators = true;
                generator_efficiency_at_100pcnt = source_temp_at_100pc >= double.MaxValue || double.IsInfinity(resting_radiator_temp_at_100pcnt) || double.IsNaN(resting_radiator_temp_at_100pcnt) ? 0 : 1 - resting_radiator_temp_at_100pcnt / source_temp_at_100pc;
                generator_efficiency_at_100pcnt = Math.Max(averageEfficiency * generator_efficiency_at_100pcnt,0);
                generator_efficiency_at_30pcnt = source_temp_at_30pc >= double.MaxValue || double.IsInfinity(resting_radiator_temp_at_30pcnt) || double.IsNaN(resting_radiator_temp_at_30pcnt) ? 0 : 1 - resting_radiator_temp_at_30pcnt / source_temp_at_100pc;
                generator_efficiency_at_30pcnt = Math.Max((averageEfficiency) * generator_efficiency_at_30pcnt, 0);
            }
            else
                has_generators = false;

            if (source_temp_at_100pc >= double.MaxValue)
                source_temp_at_100pc = -1;

            if (source_temp_at_30pc >= double.MaxValue)
                source_temp_at_30pc = -1;
        }

        protected void OnGUI()
        {
            if (renderWindow)
                windowPosition = GUILayout.Window(thermalWindowId, windowPosition, Window, Localizer.Format("#LOC_KSPIE_VABThermalUI_title"));//"Interstellar Thermal Mechanics Helper"
        }

        private void Window(int windowId)
        {
            green_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.green}};
            red_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.red}};
            orange_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
            bold_label = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold};

            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                renderWindow = false;

            GUIStyle radiatorLabel = green_label;

            if (rad_max_dissip < total_source_power)
            {
                radiatorLabel = orange_label;
                if (rad_max_dissip < min_source_power)
                    radiatorLabel = red_label;
            }

            GUILayout.BeginVertical();
            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Distance from Kerbol: /AU (Kerbin = 1)", GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true));
            //GUILayout.EndHorizontal();
            //GUILayout.BeginHorizontal();
            //au_scale = GUILayout.HorizontalSlider((float)au_scale, 0.001f, 8f, GUILayout.ExpandWidth(true));
            //GUILayout.Label(au_scale.ToString("0.000")+ " AU", GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalHeatProduction"), bold_label, GUILayout.ExpandWidth(true));//"Total Heat Production:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(total_source_power), GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ThermalSourceTemperature100"), bold_label, GUILayout.ExpandWidth(true));//"Thermal Source Temperature at 100%:"
            string sourceTempString = (source_temp_at_100pc < 0) ? "N/A" : source_temp_at_100pc.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ThermalSourceTemperature30"), bold_label, GUILayout.ExpandWidth(true));//"Thermal Source Temperature at 30%:"
            string sourceTempString2 = (source_temp_at_30pc < 0) ? "N/A" : source_temp_at_30pc.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString2, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalNumberofRadiators"), bold_label, GUILayout.ExpandWidth(true));//"Total Number of Radiators:"
            GUILayout.Label(n_rads.ToString(), GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalAreaRadiators"), bold_label, GUILayout.ExpandWidth(true));//"Total Area Radiators:"
            GUILayout.Label(total_area_display + " m\xB2", GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorMaximumDissipation"), bold_label, GUILayout.ExpandWidth(true));//"Radiator Maximum Dissipation:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(rad_max_dissip), radiatorLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            string restingRadiatorTempAt100PcntStr = (!double.IsInfinity(resting_radiator_temp_at_100pcnt) && !double.IsNaN(resting_radiator_temp_at_100pcnt)) ? resting_radiator_temp_at_100pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorRestingTemperatureat100"), bold_label, GUILayout.ExpandWidth(true));//"Radiator Resting Temperature at 100% Power:"
            GUILayout.Label(restingRadiatorTempAt100PcntStr, radiatorLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            string restingRadiatorTempAt30PcntStr = (!double.IsInfinity(resting_radiator_temp_at_30pcnt) && !double.IsNaN(resting_radiator_temp_at_30pcnt)) ? resting_radiator_temp_at_30pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorRestingTemperatureat30"), bold_label, GUILayout.ExpandWidth(true));//"Radiator Resting Temperature at 30% Power:"
            GUILayout.Label(restingRadiatorTempAt30PcntStr, radiatorLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();

            if (has_generators)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingGeneratorEfficiencyat100"), bold_label, GUILayout.ExpandWidth(true));//"Resting Generator Efficiency at 100% Power:"
                GUILayout.Label((generator_efficiency_at_100pcnt*100).ToString("0.00") + "%", radiatorLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingGeneratorEfficiencyat30"), bold_label, GUILayout.ExpandWidth(true));//"Resting Generator Efficiency at 30% Power:"
                GUILayout.Label((generator_efficiency_at_30pcnt * 100).ToString("0.00") + "%", radiatorLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
