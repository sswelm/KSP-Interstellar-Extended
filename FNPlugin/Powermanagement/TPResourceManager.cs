using System;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class TPResourceManager : ResourceManager
    {
        public TPResourceManager(Guid overmanagerId, PartModule pm, string resource_name) : base(overmanagerId, pm, resource_name, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(600, 50, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
