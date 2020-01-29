using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    public interface IResourceSupplier 
    {
        Guid Id { get; }

        string getResourceManagerDisplayName();

        double supplyFNResourceFixed(double supply, String resourcename);
    }
}
