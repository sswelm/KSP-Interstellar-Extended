using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Propulsion
{
    public interface IFNEngineNoozle : IEngineNoozle
    {
        Part part { get; }
        bool RequiresPlasmaHeat { get; }
        bool RequiresThermalHeat { get; }
		bool PropellantAbsorbsNeutrons { get; }
    }
}
