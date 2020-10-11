using System;
using System.Collections.Generic;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class CPResourceManager : ResourceManager
    {
        public CPResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, FNRESOURCE_CHARGED_PARTICLES, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(50, 600, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
