using System;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class ThermalPowerResourceManager : ResourceManager
    {
        public ThermalPowerResourceManager(Guid overmanagerId, ResourceSuppliableModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ThermalPowerInMegawatt, FnResourceFlowTypeEven)
        {
            SetWindowPosition(pm.tpx, pm.tpy, 600, 50);
        }

        protected override void DoWindowFinal()
        {
            PartModule.tpx = (int)WindowPosition.x;
            PartModule.tpy = (int)WindowPosition.y;
        }
    }
}
