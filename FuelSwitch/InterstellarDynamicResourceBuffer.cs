using System;

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
                var ratio = Math.Min(1, resource.amount / resource.maxAmount);
                var newMaxAmount = bufferSize * (double)(decimal)TimeWarp.fixedDeltaTime * 50;
                resource.maxAmount = IsValidNumber(newMaxAmount) ? newMaxAmount : resource.maxAmount;
                var newAmount = resource.maxAmount * ratio;
                resource.amount = IsValidNumber(newAmount) ? newAmount : resource.amount;
            }
        }

        private bool IsValidNumber(double number)
        {
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }
    }
}
