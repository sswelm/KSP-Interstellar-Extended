using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public static class PartResourceLibraryExtension
    {
        public static PartResourceDefinition GetDefinitionSafe(this PartResourceLibrary part, string name)
        {
            return PartResourceLibrary.Instance.resourceDefinitions.Cast<PartResourceDefinition>().FirstOrDefault(m => m.name == name);
        }
    }
}
