using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        bool HasActivityRequirements { get; }

        double PowerRequirements { get; }

        String Status { get; }

        void UpdateFrame(double rateMultiplier, double powerFraction,  double powerModidier, bool allowOverfow, double fixedDeltaTime);

        void UpdateGUI();
    }
}
