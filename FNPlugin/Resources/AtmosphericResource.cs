using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Resources
{
    public class AtmosphericResource
    {
        public AtmosphericResource(PartResourceDefinition definition, double abundance)
        {
            this.ResourceName = definition.name;
            this.ResourceAbundance = abundance;
            this.DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            this.Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public AtmosphericResource(string resourcename, double abundance, string displayname)
        {
            this.ResourceName = resourcename;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayname;
            this.Synonyms = new[] { resourcename }.ToList();
        }

        public AtmosphericResource(string resourcename, double abundance, string displayname, string[] synonyms)
        {
            this.ResourceName = resourcename;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayname;
            this.Synonyms = synonyms.ToList();
        }

        public string DisplayName { get; private set; }
        public string ResourceName {get; private set;}
        public double ResourceAbundance { get; private set; }
        public List<string> Synonyms { get; private set; }
    }
}
