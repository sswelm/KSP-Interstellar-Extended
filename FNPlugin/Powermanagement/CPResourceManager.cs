using System;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class CPResourceManager : ResourceManager
    {
        public CPResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ChargedParticleInMegawatt, FnResourceFlowTypeEven)
        {
            WindowPosition = new Rect(50, 600, LabelWidth + ValueWidth + PriorityWidth, 50);
        }
    }
}
