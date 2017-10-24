using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    public interface IResourceSuppliable 
    {
        void receiveFNResource(double power_supplied, String resourcename);
		double consumeFNResource(double power_to_consume, String resourcename, double fixedDeltaTime = 0);
        string getResourceManagerDisplayName();
        int getPowerPriority();
    }
}
