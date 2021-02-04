using System;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class TPResourceManager : ResourceManager
    {
        public TPResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ThermalPowerInMegawatt, FnResourceFlowTypeEven)
        {
            WindowPosition = new Rect(600, 50, LabelWidth + ValueWidth + PriorityWidth, 50);
        }
    }
}
