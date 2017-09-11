using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    public class FNResourceManager : ORSResourceManager 
    {
        private const double passive_temp_p4 = 2947.295521;

        private int labelWidth = 240;
        private int valueWidth = 55;
        private int priorityWidth = 30;
        private int overviewWidth = 65;

        GUIStyle left_bold_label;
        GUIStyle right_bold_label;
        GUIStyle green_label;
        GUIStyle red_label;
        GUIStyle left_aligned_label;
        GUIStyle right_aligned_label;

        public FNResourceManager(PartModule pm, String resource_name) : base(pm, resource_name) 
        {
            int xPos = 0;
            int yPos = 0;

            if (resource_name == ORSResourceManager.FNRESOURCE_MEGAJOULES)
            {
                xPos = 50;
                yPos = 50;
            }
            else if (resource_name == ORSResourceManager.FNRESOURCE_THERMALPOWER)
            {
                xPos = 600;
                yPos = 50;
            }
            else if (resource_name == ORSResourceManager.FNRESOURCE_CHARGED_PARTICLES)
            {
                xPos = 50;
                yPos = 600;
            }
            else if (resource_name == ORSResourceManager.FNRESOURCE_WASTEHEAT)
            {
                xPos = 600;
                yPos = 600;
            }

            windowPosition = new Rect(xPos, yPos, labelWidth + valueWidth + priorityWidth, 50);
        }

        protected override void pluginSpecificImpl() 
        {
            if (String.Equals(this.resource_name, FNResourceManager.FNRESOURCE_WASTEHEAT) && !PluginHelper.IsThermalDissipationDisabled) 
            {   
                // passive dissip of waste heat - a little bit of this
                double vessel_mass = my_vessel.GetTotalMass();
                double passive_dissip = passive_temp_p4 * GameConstants.stefan_const * vessel_mass * 2;
                internl_power_extract_fixed += passive_dissip * TimeWarp.fixedDeltaTime;

                if (my_vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(my_vessel.mainBody)) 
                { 
                    // passive convection - a lot of this
                    double pressure = FlightGlobals.getStaticPressure(my_vessel.transform.position) / 100;
                    double delta_temp = 20;
                    double conv_power_dissip = pressure * delta_temp * vessel_mass * 2.0 * GameConstants.rad_const_h / 1e6 * TimeWarp.fixedDeltaTime;
                    internl_power_extract_fixed += conv_power_dissip;
                }
            }
        }
                
        protected override void doWindow(int windowID) 
        {
            if (left_bold_label == null)
            {
                left_bold_label = new GUIStyle(GUI.skin.label);
                left_bold_label.fontStyle = FontStyle.Bold;
                left_bold_label.font = PluginHelper.MainFont;
            }

            if (right_bold_label == null)
            {
                right_bold_label = new GUIStyle(GUI.skin.label);
                right_bold_label.fontStyle = FontStyle.Bold;
                right_bold_label.font = PluginHelper.MainFont;
                right_bold_label.alignment = TextAnchor.MiddleRight;
            }

            if (green_label == null)
            {
                green_label = new GUIStyle(GUI.skin.label);
                green_label.normal.textColor = resource_name == ORSResourceManager.FNRESOURCE_WASTEHEAT ? Color.red : Color.green;
                green_label.font = PluginHelper.MainFont;
                green_label.alignment = TextAnchor.MiddleRight;
            }

            if (red_label == null)
            {
                red_label = new GUIStyle(GUI.skin.label);
                red_label.normal.textColor = resource_name == ORSResourceManager.FNRESOURCE_WASTEHEAT ? Color.green : Color.red;
                red_label.font = PluginHelper.MainFont;
                red_label.alignment = TextAnchor.MiddleRight;
            }

            if (left_aligned_label == null)
            {
                left_aligned_label = new GUIStyle(GUI.skin.label);
                left_aligned_label.fontStyle = FontStyle.Normal;
                left_aligned_label.font = PluginHelper.MainFont;
            }

            if (right_aligned_label == null)
            {
                right_aligned_label = new GUIStyle(GUI.skin.label);
                right_aligned_label.fontStyle = FontStyle.Normal;
                right_aligned_label.font = PluginHelper.MainFont;
                right_aligned_label.alignment = TextAnchor.MiddleRight;
            }

            if (render_window && GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                render_window = false;

            GUILayout.Space(2);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Theoretical Supply",left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_stable_supply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Supply", left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_supply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            if (resource_name == ORSResourceManager.FNRESOURCE_MEGAJOULES)
            {
                var stored_supply_percentage = stored_supply != 0 ? stored_total_power_supplied / stored_supply * 100 : 0; 

                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Distribution", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label(stored_supply_percentage.ToString("0.000") + "%", right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power Demand", left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_resource_demand), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            double new_power_supply = getOverproduction(); 
            double net_utilisation_supply = getDemandStableSupply();

            GUIStyle net_poer_style = new_power_supply < -0.001 ? red_label : green_label;
            GUIStyle utilisation_style = net_utilisation_supply > 1.001 ? red_label : green_label;

            GUILayout.BeginHorizontal();
            var new_power_label = (resource_name == ORSResourceManager.FNRESOURCE_WASTEHEAT) ? "Net Change" : "Net Power";
            GUILayout.Label(new_power_label, left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(new_power_supply), net_poer_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            if (!double.IsNaN(net_utilisation_supply) && !double.IsInfinity(net_utilisation_supply)) 
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Utilisation", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label((net_utilisation_supply).ToString("P3"), utilisation_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
                GUILayout.EndHorizontal();
            }

            if (power_supply_list_archive != null)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Producer Component", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label("Supply", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.Label("Max", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.EndHorizontal();

                var groupedPowerSupply = power_supply_list_archive.GroupBy(m => m.Key.getResourceManagerDisplayName());

                foreach (var group in groupedPowerSupply)
                {
                    var sumOfCurrentSupply = group.Sum(m => m.Value.averageSupply);
                    var sumOfMaximumSupply = group.Sum(m => m.Value.maximumSupply);

                    // skip anything with less then 0.00 KW
                    if (sumOfCurrentSupply < 0.00005)
                        continue;

                    GUILayout.BeginHorizontal();

                    string name = group.Key;
                    var count = group.Count();
                    if (count > 1)
                        name = count + " " + name;

                    GUILayout.Label(name, left_aligned_label, GUILayout.ExpandWidth(true));
                    GUILayout.Label(getPowerFormatString(sumOfCurrentSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                    GUILayout.Label(getPowerFormatString(sumOfMaximumSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                    GUILayout.EndHorizontal();
                }
            }

            if (power_draw_list_archive != null) 
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Consumer Component", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label("Demand", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.Label("Rank", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(priorityWidth));
                GUILayout.EndHorizontal();

                var groupedPowerDraws = power_draw_list_archive.GroupBy(m => m.Key.getResourceManagerDisplayName());

                foreach (var group in groupedPowerDraws)
                {
                    var sumOfPowerDraw = group.Sum(m => m.Value.Power_draw);
                    var sumOfPowerConsume = group.Sum(m => m.Value.Power_consume);
                    var sumOfConsumePercentage = sumOfPowerDraw > 0 ? sumOfPowerConsume / sumOfPowerDraw * 100 : 0;

                    GUILayout.BeginHorizontal();

                    string name = group.Key;
                    var count = group.Count();
                    if (count > 1)
                        name = count + " " + name;
                    if (resource_name == ORSResourceManager.FNRESOURCE_MEGAJOULES && sumOfConsumePercentage < 99.5)
                        name = name + " " + sumOfConsumePercentage.ToString("0") + "%";

                    GUILayout.Label(name, left_aligned_label, GUILayout.ExpandWidth(true));

                    GUILayout.Label(getPowerFormatString(sumOfPowerDraw), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                    GUILayout.Label(group.First().Key.getPowerPriority().ToString(), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(priorityWidth));
                    GUILayout.EndHorizontal();
                }
            }

            if (resource_name == ORSResourceManager.FNRESOURCE_MEGAJOULES)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("DC Electrical System", left_aligned_label, GUILayout.ExpandWidth(true));
                GUILayout.Label(getPowerFormatString(stored_charge_demand), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.Label("0", right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(priorityWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

    }
}
