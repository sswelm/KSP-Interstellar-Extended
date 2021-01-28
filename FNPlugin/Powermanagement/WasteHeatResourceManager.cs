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

        public WasteHeatResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, ResourceSettings.Config.WasteHeatInMegawatt, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(600, 600, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
            TemperatureRatio = 0.0;
            RadiatorEfficiency = 0.0;
        }

        public override void update(long counter)
        {
            base.update(counter);

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
