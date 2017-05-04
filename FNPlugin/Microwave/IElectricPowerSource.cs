using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public interface IElectricPowerGeneratorSource
    {
        double MaxStableMegaWattPower { get; }
        void Refresh();
        void FindAndAttachToPowerSource();
    }
}
