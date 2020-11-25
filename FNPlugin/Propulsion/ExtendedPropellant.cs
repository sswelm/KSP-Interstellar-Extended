namespace FNPlugin.Propulsion
{
    public class ExtendedPropellant : Propellant
    {
        private string _secondaryPropellantName;
        private PartResourceDefinition _resourceDefinition;

        public string StoragePropellantName => _secondaryPropellantName;

        public PartResourceDefinition ResourceDefinition => _resourceDefinition;

        public new void Load(ConfigNode node)
        {
            base.Load(node);

            _resourceDefinition = PartResourceLibrary.Instance.GetDefinition(base.name);

            _secondaryPropellantName = node.HasValue("storageName") ? node.GetValue("storageName") : name;
        }
    }
}
