using System;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class DefaultResourceManager : ResourceManager
    {
        public DefaultResourceManager(Guid overmanagerId, PartModule pm, string resource) : base(overmanagerId, pm, resource, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(100, 100, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
