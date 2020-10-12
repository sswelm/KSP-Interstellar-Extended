using System;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class TPResourceManager : ResourceManager
    {
        public TPResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, FNRESOURCE_THERMALPOWER, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(600, 50, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
