using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Propulsion;

namespace FNPlugin.Redist
{
    public interface IFNPowerSource : IPowerSource
    {
        void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, bool isMHD);

        double EngineHeatProductionMult { get; }
    }
}
