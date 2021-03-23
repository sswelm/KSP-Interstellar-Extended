using FNPlugin.Power;
using System;
using FNPlugin.Resources;

namespace FNPlugin.Wasteheat
{
    internal sealed class WasteHeatBufferConfig : ResourceBuffers.VariableConfig
    {
        public bool ClampInitialMaxAmount { get; }
        public double ResourceMultiplier { get; }
        public double BaseResourceAmount { get; }

        private bool _initialized;

        public WasteHeatBufferConfig(double heatMultiplier = 1.0d, double baseHeatAmount = 1.0d, bool clampInitialMaxAmount = false) : base(ResourceSettings.Config.WasteHeatInMegawatt)
        {
            ClampInitialMaxAmount = clampInitialMaxAmount;
            ResourceMultiplier = heatMultiplier;
            BaseResourceAmount = baseHeatAmount;
            RecalculateBaseResourceMax();
        }

        protected override void RecalculateBaseResourceMax()
        {
            // calculate waste heat capacity
            BaseResourceMax = ResourceMultiplier * BaseResourceAmount * VariableMultiplier * 0.02;
        }

        protected override void UpdateBufferForce()
        {
            var bufferedResource = part.Resources[ResourceName];
            if (bufferedResource != null)
            {
                double maxWasteHeatRatio = ClampInitialMaxAmount && !_initialized ? 0.95d : 1.0d;

                var resourceRatio = Math.Max(0, Math.Min(maxWasteHeatRatio, bufferedResource.maxAmount > 0 ? bufferedResource.amount / bufferedResource.maxAmount : 0));
                bufferedResource.maxAmount = Math.Max(0.0001, BaseResourceMax);
                bufferedResource.amount = Math.Max(0, resourceRatio * BaseResourceMax);
            }
            _initialized = true;
        }
    }
}
