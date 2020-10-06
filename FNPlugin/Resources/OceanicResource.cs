using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Resources
{
    class OceanicResource
    {
        public OceanicResource(string resourceName, double abundance, string displayName)
        {

            this.ResourceName = resourceName;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayName;
            this.Synonyms = new [] {resourceName}.ToList();
        }

        public OceanicResource(PartResourceDefinition definition, double abundance )
        {
            this.ResourceName = definition.name;
            this.ResourceAbundance = abundance;
            this.DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            this.Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public OceanicResource(string resourceName, double abundance, string displayName, string[] synonyms)
        {
            this.ResourceName = resourceName;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayName;
            this.Synonyms = synonyms.ToList();
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
                    this.Definition = PartResourceLibrary.Instance.GetDefinition(value);
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public double ResourceAbundance { get; }

        public List<string> Synonyms { get; }
    }
}
