using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Propulsion
{
    public interface IEngineNoozle
    {
        int Fuel_mode { get; }
        ConfigNode[] getPropellants();
        double GetNozzleFlowRate();
        float CurrentThrottle { get; }
    }
}
