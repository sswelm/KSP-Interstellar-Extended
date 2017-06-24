using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Propulsion
{
    public interface IEngineNoozle
    {
        double GetNozzleFlowRate();
        float CurrentThrottle { get; }
        bool RequiresChargedPower { get; }
    }
}
