using System;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class CPResourceManager : ResourceManager
    {
        public CPResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ChargedParticleInMegawatt, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(50, 600, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
