using System;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class ChargedPowerResourceManager : ResourceManager
    {
        public ChargedPowerResourceManager(Guid overmanagerId, ResourceSuppliableModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ChargedPowerInMegawatt, FnResourceFlowTypeEven)
        {
            SetWindowPosition(pm.cpx, pm.cpy, 50, 600);
        }

        protected override void DoWindowFinal()
        {
            PartModule.cpx = (int)WindowPosition.x;
            PartModule.cpy = (int)WindowPosition.y;
        }
    }
}
