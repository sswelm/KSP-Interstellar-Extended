using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem 
{
    public interface IORSResourceSupplier 
    {
        string getResourceManagerDisplayName();

        double supplyFNResourceFixed(double supply, String resourcename);
    }
}
