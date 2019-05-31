namespace FNPlugin.Propulsion
{
    public class ExtendedPropellant : Propellant
    {
        private string _secondaryPropellantName;
        private PartResourceDefinition _resourceDefinition;

        public string StoragePropellantName
        {
            get { return _secondaryPropellantName; }
        }

        public PartResourceDefinition ResourceDefinition
        {
            get { return _resourceDefinition; }
        }

        public new void Load(ConfigNode node)
        {
            base.Load(node);

            _resourceDefinition = PartResourceLibrary.Instance.GetDefinition(base.name);

            _secondaryPropellantName = node.HasValue("storageName") ? node.GetValue("storageName") : name;
        }
    }
}
