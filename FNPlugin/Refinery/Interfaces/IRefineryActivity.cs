using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Refinery
{    
    enum RefineryType { heating = 1, cryogenics = 2, electrolysis = 4, synthesize = 8,  } 

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
