using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Resources
{
    class OceanicResource
    {
        public OceanicResource(string resourceName, double abundance, string displayName)
        {

            ResourceName = resourceName;
            ResourceAbundance = abundance;
            DisplayName = displayName;
            Synonyms = new [] {resourceName}.ToList();
        }

        public OceanicResource(PartResourceDefinition definition, double abundance )
        {
            ResourceName = definition.name;
            ResourceAbundance = abundance;
            DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public OceanicResource(string resourceName, double abundance, string displayName, string[] synonyms)
        {
            ResourceName = resourceName;
            ResourceAbundance = abundance;
            DisplayName = displayName;
            Synonyms = synonyms.ToList();
        }

        public string DisplayName { get; }

        private string _resourceName;

        public string ResourceName
        {
            get => _resourceName;
            set
            {
                _resourceName = value;
                if (value != null)
                    Definition = PartResourceLibrary.Instance.GetDefinition(value);
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public double ResourceAbundance { get; }

        public List<string> Synonyms { get; }
    }
}
