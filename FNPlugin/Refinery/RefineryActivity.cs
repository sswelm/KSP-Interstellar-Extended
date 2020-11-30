using FNPlugin.Constants;
using UnityEngine;

namespace FNPlugin.Refinery
{
    internal enum RefineryType { Heating = 1, Cryogenics = 2, Electrolysis = 4, Synthesize = 8,  }

    public class RefineryActivity: PartModule
    {
        [KSPField(guiActiveEditor = true, guiName = "Size Multiplier")]
        public double sizeModifier = 1;

        public static int labelWidth = 180;
        public static int valueWidth = 180;

        protected Part _part;
        protected Vessel _vessel;
        protected GUIStyle _bold_label;
        protected GUIStyle _value_label;
        protected GUIStyle _value_label_green;
        protected GUIStyle _value_label_red;
        protected GUIStyle _value_label_number;

        protected string _status = "";
        protected bool _allowOverflow;
        protected double _current_power;
        protected double _current_rate;
        protected double _effectiveMaxPower;

        public double CurrentPower => _current_power;

        public string ActivityName { get; protected set; }
        public string Formula { get; protected set; }

        public double PowerRequirements { get; protected set; }
        public double EnergyPerTon { get; protected set; }


        public virtual void UpdateGUI()
        {
            if (_bold_label == null)
                _bold_label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };
            if (_value_label == null)
                _value_label = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };
            if (_value_label_green == null)
                _value_label_green = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, normal = {textColor = Color.green}};
            if (_value_label_red == null)
                _value_label_red = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, normal = {textColor = Color.red}};
            if (_value_label_number == null)
                _value_label_number = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, alignment = TextAnchor.MiddleRight };
        }

        public override string ToString()
        {
            return ActivityName;
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();
            sb.AppendLine(ActivityName);

            if (!string.IsNullOrEmpty(Formula))
                sb.AppendLine(Formula);

            double capacity = sizeModifier * PowerRequirements;
            if (capacity > 0)
            {
                sb.Append("Power: ").AppendLine(PluginHelper.getFormattedPowerString(capacity));

                if (EnergyPerTon > 0.0)
                {
                    sb.Append("Energy: ").Append(PluginHelper.getFormattedPowerString(EnergyPerTon)).AppendLine("/t");
                    sb.Append("Energy: ").Append((1.0 / EnergyPerTon).ToString("F3")).AppendLine(" t/MW");

                    double production = capacity / EnergyPerTon;
                    sb.Append("Production: ").Append(production.ToString("F3")).AppendLine(" t/sec");
                    sb.Append("Production: ").Append((production * 60.0).ToString("F1")).AppendLine(" t/min");
                    sb.Append("Production: ").Append((production * GameConstants.SECONDS_IN_HOUR).ToString("F0")).AppendLine(" t/hr");
                }
            }

            return sb.ToStringAndRelease();
        }
    }
}
