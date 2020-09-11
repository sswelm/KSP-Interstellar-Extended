using System;

namespace FNPlugin 
{
    public interface IResourceSuppliable 
    {
        void receiveFNResource(double power_supplied, String resourceName);
        double consumeFNResource(double power_to_consume, String resourcename, double fixedDeltaTime = 0);
        string getResourceManagerDisplayName();
        int getPowerPriority();
    }
}
