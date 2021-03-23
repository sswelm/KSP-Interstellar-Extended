using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Resources
{
    class CrustalResource
    {
        public CrustalResource(string resourcename, double abundance, string displayname)
        {
            if (!String.IsNullOrEmpty(resourcename))
                Definition = PartResourceLibrary.Instance.GetDefinition(resourcename);

            ResourceName = resourcename;
            ResourceAbundance = abundance;
            DisplayName = displayname;
            Synonyms = new[] { resourcename }.ToList();
        }

        public CrustalResource(PartResourceDefinition definition, double abundance)
        {
            Definition = definition;
            ResourceName = definition.name;
            ResourceAbundance = abundance;
            DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public CrustalResource(string resourcename, double abundance, string displayname, string[] synonyms)
        {
            if (!string.IsNullOrEmpty(resourcename))
                Definition = PartResourceLibrary.Instance.GetDefinition(resourcename);

            ResourceName = resourcename;
            ResourceAbundance = abundance;
            DisplayName = displayname;
            Synonyms = synonyms.ToList();
        }

        public double Production { get; set; }
        public double MaxAmount { get; set; }
        public double Amount { get; set; }
        public double SpareRoom { get; set; }
        public PartResourceDefinition Definition { get; }
        public string DisplayName { get; }
        public string ResourceName { get; }
        public double ResourceAbundance { get; }
        public List<string> Synonyms { get;  }
    }
}
