using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    class ResourceBuffers
    {
        public class WasteHeatConfig
        {
            public double WasteHeatMultiplier { get; private set; } = 1.0d;
            public double WasteHeatPerUnitMass { get; private set; } = 1e+5;
            public double InitialMaxWasteHeatRatio { get; private set; } = 1.0d;
            public bool   ScalesWithTime { get; private set; } = true;

            public WasteHeatConfig(double wasteHeatMultiplier, double wasteHeatPerUnitMass, double initialMaxWasteHeatRatio = 1.0d, bool scalesWithTime = true)
            {
                this.WasteHeatMultiplier = wasteHeatMultiplier;
                this.WasteHeatPerUnitMass = wasteHeatPerUnitMass;
                this.ScalesWithTime = scalesWithTime;
                this.InitialMaxWasteHeatRatio = Math.Max(0, Math.Min(1, initialMaxWasteHeatRatio));
            }
        }

        protected WasteHeatConfig wasteHeatConfig;
        protected PartResource wasteHeatResource;
        protected double partMass;
        protected double partBaseWasteHeat;
        protected float previousDeltaTime;
        protected bool initialized;

        public ResourceBuffers(WasteHeatConfig wasteHeatConfig)
        {
            this.wasteHeatConfig = wasteHeatConfig;
        }

        public void Init(Part part)
        {
            partMass = part.mass;
            wasteHeatResource = part.Resources[ResourceManager.FNRESOURCE_WASTEHEAT];
            partBaseWasteHeat = partMass * wasteHeatConfig.WasteHeatPerUnitMass * wasteHeatConfig.WasteHeatMultiplier;
            UpdateBuffers();
            initialized = true;
        }

        public void UpdateBuffers()
        {
            if ((wasteHeatConfig.ScalesWithTime || !initialized) && Math.Abs(TimeWarp.fixedDeltaTime - previousDeltaTime) > float.Epsilon)
            {
                if (wasteHeatResource != null)
                {
                    float timeMultiplier = HighLogic.LoadedSceneIsFlight && wasteHeatConfig.ScalesWithTime ? TimeWarp.fixedDeltaTime : 1;
                    double maxWasteHeatRatio = initialized ? 1 : wasteHeatConfig.InitialMaxWasteHeatRatio;
                    // calculate WasteHeat Capacity
                    var wasteHeatRatio = Math.Max(0, Math.Min(maxWasteHeatRatio, wasteHeatResource.amount / wasteHeatResource.maxAmount));
                    wasteHeatResource.maxAmount = Math.Max(0.0001, TimeWarp.fixedDeltaTime * partBaseWasteHeat);
                    wasteHeatResource.amount = Math.Max(0, Math.Min(wasteHeatResource.maxAmount, wasteHeatRatio * wasteHeatResource.maxAmount));
                }
            }
            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }
    }
}
