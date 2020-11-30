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
                this.Definition = PartResourceLibrary.Instance.GetDefinition(resourcename);

            this.ResourceName = resourcename;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayname;
            this.Synonyms = new[] { resourcename }.ToList();


        }

        public CrustalResource(PartResourceDefinition definition, double abundance)
        {
            this.Definition = definition;
            this.ResourceName = definition.name;
            this.ResourceAbundance = abundance;
            this.DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            this.Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public CrustalResource(string resourcename, double abundance, string displayname, string[] synonyms)
        {
            if (!String.IsNullOrEmpty(resourcename))
                this.Definition = PartResourceLibrary.Instance.GetDefinition(resourcename);

            this.ResourceName = resourcename;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayname;
            this.Synonyms = synonyms.ToList();
        }

        public double Production { get; set; }
        public double MaxAmount { get; set; }
        public double Amount { get; set; }
        public double SpareRoom { get; set; }
        public PartResourceDefinition Definition { get; private set; }
        public string DisplayName { get; private set; }
        public string ResourceName { get; private set; }
        public double ResourceAbundance { get; private set; }
        public List<string> Synonyms { get; private set; }
    }
}
