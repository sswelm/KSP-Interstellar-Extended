using System;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class CPResourceManager : ResourceManager
    {
        public CPResourceManager(Guid overmanagerId, PartModule pm, string resource_name) : base(overmanagerId, pm, resource_name, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(50, 600, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
