using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{    
    enum RefineryType { Heating = 1, Cryogenics = 2, Electrolysis = 4, Synthesize = 8,  } 

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
        public double PowerRequirements { get; protected set; }

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
            sb.AppendLine("Power Requirement: " + sizeModifier * PowerRequirements + " MW");

            return sb.ToString();
        }
    }

    interface IRefineryActivity
    {
        // 1 seperation
        // 2 desconstrution
        // 3 construction

        RefineryType RefineryType { get; }

        string ActivityName { get;}

        double CurrentPower { get; }

        bool HasActivityRequirements();

        double PowerRequirements { get; }

        string Status { get; }

        void UpdateFrame(double rateMultiplier, double powerFraction,  double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false);

        void UpdateGUI();

        void PrintMissingResources();

        void Initialize(Part part);
    }
}
