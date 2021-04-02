using System;

namespace FNPlugin
{
    public interface IResourceSupplier
    {
        Guid Id { get; }

        string getResourceManagerDisplayName();

        double SupplyFNResourceFixed(double supply, String resourceName);
    }
}
