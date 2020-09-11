using System;

namespace FNPlugin 
{
    public interface IResourceSupplier 
    {
        Guid Id { get; }

        string getResourceManagerDisplayName();

        double supplyFNResourceFixed(double supply, String resourceName);
    }
}
