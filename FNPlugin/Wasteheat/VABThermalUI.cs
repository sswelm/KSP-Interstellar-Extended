using FNPlugin.Constants;
using FNPlugin.Powermanagement;
using FNPlugin.Redist;
using KSP.Localization;
using System;
using System.Collections.Generic;
using FNPlugin.Propulsion;
using UniLinq;
using UnityEngine;

namespace FNPlugin.Wasteheat
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class VABThermalUI : MonoBehaviour
    {
        public static bool renderWindow;

        private int numberOfRadiators;
        private int thermalWindowId = 825462;
        private bool has_generators;

        private const int labelWidth = 300;
        private const int valueWidth = 80;

        private Rect windowPosition = new Rect(500, 500, labelWidth + valueWidth, 100);
        private GUIStyle bold_label;
        private GUIStyle green_label;
        private GUIStyle red_label;
        private GUIStyle orange_label;

        private double total_source_power;
        private double source_temp_at_100pc;
        private double source_temp_at_30pc;
        private double rad_max_dissip;
        private double total_area;
        private double min_source_power;
        private double resting_radiator_temp_at_100pcnt;
        private double resting_radiator_temp_at_30pcnt;
        private double average_rad_temp;
        private double au_scale = 1;
        private double generator_efficiency_at_100pcnt;
        private double generator_efficiency_at_30pcnt;

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
            List<FNGenerator> generators = new List<FNGenerator>();
            List<ModuleDeployableSolarPanel> solarPanels = new List<ModuleDeployableSolarPanel>();
            List<ThermalEngineController> thermalEngines = new List<ThermalEngineController>();
            List<BeamedPowerReceiver> beamedReceivers = new List<BeamedPowerReceiver>();

            foreach (var part in EditorLogic.fetch.ship.parts)
            {
                thermalSources.AddRange(part.FindModulesImplementing<IPowerSource>());
                radiators.AddRange(part.FindModulesImplementing<FNRadiator>());
                solarPanels.AddRange(part.FindModulesImplementing<ModuleDeployableSolarPanel>());
                generators.AddRange(part.FindModulesImplementing<FNGenerator>());
                thermalEngines.AddRange(part.FindModulesImplementing<ThermalEngineController>());
                beamedReceivers.AddRange(part.FindModulesImplementing<BeamedPowerReceiver>());
            }

            total_source_power = 0;
            min_source_power = 0;

            source_temp_at_100pc = double.MaxValue;
            source_temp_at_30pc = double.MaxValue;

            double totalTemperaturePowerAt100Percent = 0;
            double totalTemperaturePowerAt30Percent = 0;

            // first calculate reactors
            foreach (var powerSource in thermalSources)
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

                    // only non directly converted power end up in the radiators
                    var chargedPowerGenerator = connectedChargedPowerGenerator as FNGenerator;
                    if (chargedPowerGenerator != null)
                        combinedMaxStableMegaWattPower += powerSource.ChargedPowerRatio * connectedChargedPowerGenerator.MaxStableMegaWattPower * (1 - chargedPowerGenerator.maxEfficiency);
                }

                // only take reactor power in account when its actually connected to a power generator
                if (connectedThermalPowerGenerator == null && connectedChargedPowerGenerator == null) continue;

                double coreTempAtRadiatorTempAt100Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_100pcnt);
                double coreTempAtRadiatorTempAt30Percent = powerSource.GetCoreTempAtRadiatorTemp(resting_radiator_temp_at_30pcnt);

                var effectivePowerAt100Percent = Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt100Percent));
                var effectivePowerAt30Percent = Math.Min(combinedMaxStableMegaWattPower, powerSource.GetThermalPowerAtTemp(coreTempAtRadiatorTempAt30Percent) * 0.3);

                totalTemperaturePowerAt100Percent += coreTempAtRadiatorTempAt100Percent * effectivePowerAt100Percent;
                totalTemperaturePowerAt30Percent += coreTempAtRadiatorTempAt30Percent * effectivePowerAt30Percent;

                total_source_power += effectivePowerAt100Percent;
                min_source_power += effectivePowerAt30Percent;
            }

            // calculated weighted core temperatures
            if (total_source_power > 0)
                source_temp_at_100pc = totalTemperaturePowerAt100Percent / total_source_power;
            if (min_source_power > 0)
                source_temp_at_30pc = totalTemperaturePowerAt30Percent / min_source_power;

            // calculate effect of on demand beamed power
            foreach (var beamedReceiver in beamedReceivers)
            {
                // only count receiver that are activated
                if (!beamedReceiver.receiverIsEnabled)
                    continue;

                var maxReceiverWasteheatProduction = beamedReceiver.MaximumRecievePower * (1 - beamedReceiver.activeBandwidthConfiguration.MaxEfficiencyPercentage * 0.01);
                total_source_power += maxReceiverWasteheatProduction;
                min_source_power += 0.3 * maxReceiverWasteheatProduction;
            }

            foreach (var thermalNozzle in thermalEngines)
            {
                var nozzleWasteheatProduction = thermalNozzle.ReactorWasteheatModifier * thermalNozzle.AttachedReactor.NormalisedMaximumPower;
                total_source_power += nozzleWasteheatProduction;
                min_source_power += nozzleWasteheatProduction * 0.3;
            }

            foreach (var solarPanel in solarPanels)
            {
                total_source_power += solarPanel.chargeRate * 0.0005/au_scale/au_scale;
            }

            numberOfRadiators = 0;
            rad_max_dissip = 0;
            average_rad_temp = 0;
            total_area = 0;

            foreach (FNRadiator radiator in radiators)
            {
                total_area += radiator.BaseRadiatorArea;
                double effectiveArea = radiator.EffectiveRadiatorArea;
                double maxRadTemperature = radiator.MaxRadiatorTemperature;
                maxRadTemperature = Math.Min(maxRadTemperature, source_temp_at_100pc);
                numberOfRadiators++;
                var tempToPowerFour = maxRadTemperature * maxRadTemperature * maxRadTemperature * maxRadTemperature;
                rad_max_dissip += GameConstants.stefan_const * effectiveArea * tempToPowerFour / 1e6;
                average_rad_temp += maxRadTemperature;
            }
            average_rad_temp = numberOfRadiators != 0 ? average_rad_temp / numberOfRadiators : double.NaN;

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
            if (green_label == null)
                green_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.green}};
            if (red_label == null)
                red_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.red}};
            if (orange_label == null)
                orange_label = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
            if (bold_label == null)
                bold_label = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold};

            var guiLabelWidth = GUILayout.MinWidth(labelWidth);
            var guiValueWidth = GUILayout.MinWidth(valueWidth);

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
            GUILayout.BeginHorizontal();
            GUILayout.Label("Distance from Kerbol: /AU (Kerbin = 1)", GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            au_scale = GUILayout.HorizontalSlider((float)au_scale, 0.001f, 8f, GUILayout.ExpandWidth(true), guiLabelWidth);
            GUILayout.Label(au_scale.ToString("0.000")+ " AU", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalHeatProduction"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Heat Production:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(total_source_power), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ThermalSourceTemperature100"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Thermal Source Temperature at 100%:"
            string sourceTempString = (source_temp_at_100pc < 0) ? "N/A" : source_temp_at_100pc.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_ThermalSourceTemperature30"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Thermal Source Temperature at 30%:"
            string sourceTempString2 = (source_temp_at_30pc < 0) ? "N/A" : source_temp_at_30pc.ToString("0.0") + " K";
            GUILayout.Label(sourceTempString2, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalNumberofRadiators"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Number of Radiators:"
            GUILayout.Label(numberOfRadiators.ToString(), GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_TotalAreaRadiators"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Total Area Radiators:"
            GUILayout.Label(total_area + " m\xB2", GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorMaximumDissipation"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Maximum Dissipation:"
            GUILayout.Label(PluginHelper.getFormattedPowerString(rad_max_dissip), radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string restingRadiatorTempAt100PcntStr = (!double.IsInfinity(resting_radiator_temp_at_100pcnt) && !double.IsNaN(resting_radiator_temp_at_100pcnt)) ? resting_radiator_temp_at_100pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorRestingTemperatureat100"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Resting Temperature at 100% Power:"
            GUILayout.Label(restingRadiatorTempAt100PcntStr, radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string restingRadiatorTempAt30PcntStr = (!double.IsInfinity(resting_radiator_temp_at_30pcnt) && !double.IsNaN(resting_radiator_temp_at_30pcnt)) ? resting_radiator_temp_at_30pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RadiatorRestingTemperatureat30"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Radiator Resting Temperature at 30% Power:"
            GUILayout.Label(restingRadiatorTempAt30PcntStr, radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
            GUILayout.EndHorizontal();

            if (has_generators)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingGeneratorEfficiencyat100"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Resting Generator Efficiency at 100% Power:"
                GUILayout.Label((generator_efficiency_at_100pcnt*100).ToString("0.00") + "%", radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_VABThermalUI_RestingGeneratorEfficiencyat30"), bold_label, GUILayout.ExpandWidth(true), guiLabelWidth);//"Resting Generator Efficiency at 30% Power:"
                GUILayout.Label((generator_efficiency_at_30pcnt * 100).ToString("0.00") + "%", radiatorLabel, GUILayout.ExpandWidth(false), guiValueWidth);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
