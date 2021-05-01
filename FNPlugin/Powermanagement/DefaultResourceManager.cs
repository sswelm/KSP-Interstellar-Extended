using System;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class DefaultResourceManager : ResourceManager
    {
        public DefaultResourceManager(Guid overmanagerId, ResourceSuppliableModule pm, string resource) : base(overmanagerId, pm, resource, FnResourceFlowTypeEven)
        {
            WindowPosition = new Rect(100, 100, LabelWidth + ValueWidth + PriorityWidth, 50);
        }
    }
}
