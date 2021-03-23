using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Resources
{
    public class AtmosphericResource
    {
        public AtmosphericResource(PartResourceDefinition definition, double abundance)
        {
            ResourceName = definition.name;
            ResourceAbundance = abundance;
            DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public AtmosphericResource(string resourcename, double abundance, string displayname)
        {
            ResourceName = resourcename;
            ResourceAbundance = abundance;
            DisplayName = displayname;
            Synonyms = new[] { resourcename }.ToList();
        }

        public AtmosphericResource(string resourcename, double abundance, string displayname, string[] synonyms)
        {
            ResourceName = resourcename;
            ResourceAbundance = abundance;
            DisplayName = displayname;
            Synonyms = synonyms.ToList();
        }

        public string DisplayName { get; }
        public string ResourceName {get; }
        public double ResourceAbundance { get; }
        public List<string> Synonyms { get; }
    }
}
