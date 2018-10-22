using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterstellarFuelSwitch
{
    class InterstellarDynamicResourceBuffer : PartModule
    {
        [KSPField]
        public string resourceName = null;
        [KSPField]
        public double bufferSize = 0;

        public void FixedUpdate()
        {
            if (bufferSize <= 0)
                return;

            if (string.IsNullOrEmpty(resourceName))
                return;

            var resource = part.Resources[resourceName];

            if (resource != null && resource.maxAmount > 0)
            {
                var ratio = resource.amount / resource.maxAmount;
                resource.maxAmount = bufferSize * (double)(decimal)TimeWarp.fixedDeltaTime * 50;
                resource.amount = resource.maxAmount * ratio;
            }
        }
    }
}
