using System;
using UnityEngine;

namespace FNPlugin.Refinery
{    
    enum RefineryType { heating = 1, cryogenics = 2, electrolysis = 4, synthesize = 8,  } 

    public abstract class RefineryActivityBase
    {
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

        public double CurrentPower { get { return _current_power; } }

        public virtual void UpdateGUI()
        {
            if (_bold_label == null)
                _bold_label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };
            if (_value_label == null)
                _value_label = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };
            if (_value_label_green == null)
            {
                _value_label_green = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont};
                _value_label_green.normal.textColor = Color.green;
            }
            if (_value_label_red == null)
            {
                _value_label_red = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };
                _value_label_red.normal.textColor = Color.red;
            }
            if (_value_label_number == null)
                _value_label_number = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, alignment = TextAnchor.MiddleRight };
        }
    }

    interface IRefineryActivity
    {
        // 1 seperation
        // 2 desconstrution
        // 3 construction

        RefineryType RefineryType { get; }

        String ActivityName { get; }

        double CurrentPower { get; }

        bool HasActivityRequirements();

        double PowerRequirements { get; }

        String Status { get; }

        void UpdateFrame(double rateMultiplier, double powerFraction,  double powerModidier, bool allowOverfow, double fixedDeltaTime, bool isStartup = false);

        void UpdateGUI();

        void PrintMissingResources();

        void Initialize(Part part);
    }
}
