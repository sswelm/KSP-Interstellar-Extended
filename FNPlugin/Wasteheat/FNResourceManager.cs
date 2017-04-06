using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    public class FNResourceManager : ORSResourceManager 
    {
        private const double passive_temp_p4 = 2947.295521;

        private const int labelWidth = 240;
        private const int valueWidth = 120;

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

            windowPosition = new Rect(xPos, yPos, labelWidth + valueWidth, 100);
        }

        protected override void pluginSpecificImpl() 
        {
            if (String.Equals(this.resource_name, FNResourceManager.FNRESOURCE_WASTEHEAT) && !PluginHelper.IsThermalDissipationDisabled) 
            {   
                // passive dissip of waste heat - a little bit of this
                double vessel_mass = my_vessel.GetTotalMass();
                double passive_dissip = passive_temp_p4 * GameConstants.stefan_const * vessel_mass * 2.0;
                internl_power_extract += passive_dissip * TimeWarp.fixedDeltaTime;

                if (my_vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(my_vessel.mainBody)) 
                { 
                    // passive convection - a lot of this
                    double pressure = FlightGlobals.getStaticPressure(my_vessel.transform.position) / 100;
                    double delta_temp = 20;
                    double conv_power_dissip = pressure * delta_temp * vessel_mass * 2.0 * GameConstants.rad_const_h / 1e6 * TimeWarp.fixedDeltaTime;
                    internl_power_extract += conv_power_dissip;
                }
            }
        }
                
        protected override void doWindow(int windowID) 
        {
            bold_label = new GUIStyle(GUI.skin.label);
            bold_label.fontStyle = FontStyle.Bold;
            green_label = new GUIStyle(GUI.skin.label);
            green_label.normal.textColor = Color.green;
            red_label = new GUIStyle(GUI.skin.label);
            red_label.normal.textColor = Color.red;
            //right_align = new GUIStyle(GUI.skin.label);
            //right_align.alignment = TextAnchor.UpperRight;
            GUIStyle net_style;
            GUIStyle net_style2;

            if (render_window)
            {
                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    render_window = false;
            }
            
            GUILayout.Space(2);
            GUILayout.BeginVertical();

            //GUILayout.Toolbar(0, new string[] { "POWER", "THERMAL", "WASTEHEAT" });

            ////GUILayout.BeginHorizontal();
            ////GUILayout.Label("Update Counter", bold_label, GUILayout.ExpandWidth(true));
            ////GUILayout.Label(updateCounter.ToString(), GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
            ////GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Theoretical Supply",bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_stable_supply), GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Supply", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_supply), GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Distribution", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_total_power_supplied), GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power Demand", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_resource_demand), GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
            GUILayout.EndHorizontal();

            double new_power_supply = getDemandSupply(); //stored_supply - stored_resource_demand;
            double net_utilisation_supply = getDemandStableSupply(); //stored_resource_demand / stored_stable_supply;
            
            if (new_power_supply < -0.001) 
                net_style = red_label;
            else
                net_style = green_label;

            if (net_utilisation_supply > 1.001) 
                net_style2 = red_label;
            else 
                net_style2 = green_label;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Net Power", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(new_power_supply), net_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
            GUILayout.EndHorizontal();

            if (!double.IsNaN(net_utilisation_supply) && !double.IsInfinity(net_utilisation_supply)) 
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Utilisation", bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label((net_utilisation_supply).ToString("P3"), net_style2, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Component",bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label("Demand", bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
            GUILayout.Label("Priority", bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
            GUILayout.EndHorizontal();

            if (power_draw_list_archive != null) 
            {
                var groupedPowerDraws = power_draw_list_archive.GroupBy(m => m.Key.getResourceManagerDisplayName());

                foreach (var group in groupedPowerDraws)
                {
                    var sumOfPowerDraw = group.Sum(m => m.Value.Power_draw);
                    var sumOfPowerConsume = group.Sum(m => m.Value.Power_consume);
                    var sumOfConsumePercentage = sumOfPowerDraw > 0 ? sumOfPowerConsume / sumOfPowerDraw * 100 : 0;

                    // parts less than 1 MW are summed up
                    if (sumOfPowerDraw < 1)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(group.Key + " (" + group.Count() + ")", GUILayout.ExpandWidth(true));
                        GUILayout.Label(getPowerFormatString(sumOfPowerDraw) + " " + sumOfConsumePercentage.ToString("0.0") + "%", GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                        GUILayout.Label(group.First().Key.getPowerPriority().ToString(), GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        foreach (KeyValuePair<ORSResourceSuppliable, PowerConsumption> power_kvp in group)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(group.Key, GUILayout.ExpandWidth(true));
                            var consumePercentage = power_kvp.Value.Power_draw > 0 ? power_kvp.Value.Power_consume / power_kvp.Value.Power_draw * 100 : 0;
                            GUILayout.Label(getPowerFormatString(power_kvp.Value.Power_draw) + " " + consumePercentage.ToString("0.0") + "%", GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                            GUILayout.Label(power_kvp.Key.getPowerPriority().ToString(), GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }

            if (resource_name == ORSResourceManager.FNRESOURCE_MEGAJOULES)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("DC Electrical System", GUILayout.ExpandWidth(true));
                GUILayout.Label(getPowerFormatString(stored_charge_demand), GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.Label("0", GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

    }
}
