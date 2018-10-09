using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Propulsion
{
    public interface IFNEngineNoozle : IEngineNoozle
    {
        bool RequiresPlasmaHeat { get; }
        bool RequiresThermalHeat { get; }
		bool PropellantAbsorbsNeutrons { get; }
    }
}
