using System.Text;
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
            var sb = new StringBuilder();
            sb.AppendLine(ActivityName);

            if (!string.IsNullOrEmpty(Formula))
                sb.AppendLine(Formula);

            var capacity = sizeModifier * PowerRequirements;
            if (capacity > 0)
            {
                sb.AppendLine("Power: " + capacity + " MW");

                if (EnergyPerTon > 0)
                {
                    sb.AppendLine("Energy: " + (float) EnergyPerTon + " MW/t");
                    sb.AppendLine("Energy: " + (float)(1 / EnergyPerTon) + " t/MW");

                    var production = (float) (capacity / EnergyPerTon);
                    sb.AppendLine("Production: " + production + " t/sec");
                    sb.AppendLine("Production: " + production * 60 + " t/min");
                    sb.AppendLine("Production: " + production * 3600 + " t/hour");
                }
            }

            return sb.ToString();
        }
    }
}
