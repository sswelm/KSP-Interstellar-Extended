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
