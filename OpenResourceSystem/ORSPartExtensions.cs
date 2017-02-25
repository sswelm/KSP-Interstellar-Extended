using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem 
{
    public static class ORSPartExtensions
    {
        public static double ImprovedRequestResource(this Part part, String resourcename, double resource_amount)
        {
            return ORSHelper.fixedRequestResource(part, resourcename, resource_amount);
        }

        public static double GetResourceSpareCapacity(this Part part, String resourcename)
        {
            return ORSHelper.fixedRequestResourceSpareCapacity(part, resourcename);
        }
    }
}