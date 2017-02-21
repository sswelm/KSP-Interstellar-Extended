using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Refinery
{
    interface IRefineryActivity
    {
        // 1 seperation
        // 2 desconstrution
        // 3 construction

        int RefineryType { get; }

        String ActivityName { get; }

        double CurrentPower { get; }

        bool HasActivityRequirements { get; }

        double PowerRequirements { get; }

        String Status { get; }

        void UpdateFrame(double rateMultiplier, double powerFraction,  double powerModidier, bool allowOverfow, double fixedDeltaTime);

        void UpdateGUI();
    }
}
