using FNPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class WasteHeatResourceManager : ResourceManager
    {
        private readonly Queue<double> _resourceFillFractionQueueShort = new Queue<double>();
        private readonly Queue<double> _resourceFillFractionQueueLong = new Queue<double>();

        public double TemperatureRatio { get; private set; }

        public double RadiatorEfficiency { get; private set; }

        public double AtmosphericMultiplier { get; private set; }

        public WasteHeatResourceManager(Guid overmanagerId, ResourceSuppliableModule pm) : base(overmanagerId, pm, ResourceSettings.Config.WasteHeatInMegawatt, FnResourceFlowTypeEven)
        {
            SetWindowPosition(pm.whx, pm.why, 600, 600);
            TemperatureRatio = 0.0;
            RadiatorEfficiency = 0.0;
        }

        protected override void DoWindowFinal()
        {
            PartModule.whx = (int)WindowPosition.x;
            PartModule.why = (int)WindowPosition.y;
        }

        public override void Update(long counter)
        {
            base.Update(counter);

            if (Vessel == null)
                return;

            AtmosphericMultiplier = Vessel.atmDensity > 0 ? Math.Sqrt(Vessel.atmDensity) : 0;

            _resourceFillFractionQueueLong.Enqueue(ResourceFillFraction);
            if (_resourceFillFractionQueueLong.Count > 200)
                _resourceFillFractionQueueLong.Dequeue();

            _resourceFillFractionQueueShort.Enqueue(_resourceFillFractionQueueLong.Average());
            if (ResourceFillFraction > 0 && ResourceFillFraction < 1)
            {
                _resourceFillFractionQueueShort.Enqueue(ResourceFillFraction);
                if (_resourceFillFractionQueueShort.Count > 4)
                {
                    _resourceFillFractionQueueShort.Dequeue();
                    _resourceFillFractionQueueShort.Dequeue();
                }
            }
            else
            {
                if (_resourceFillFractionQueueShort.Count > 4)
                    _resourceFillFractionQueueShort.Dequeue();
            }
            var stabilizedFillFraction = _resourceFillFractionQueueShort.Average();

            TemperatureRatio = Math.Pow(stabilizedFillFraction, 0.75);
            RadiatorEfficiency = 1.0 - Math.Pow(1.0 - stabilizedFillFraction, 400.0);
        }
    }
}
