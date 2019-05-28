namespace FNPlugin.Propulsion
{
    public class ThermalEngineFuel
    {
        private int _index;
        
        private string _fuelmode;

        private bool _isLFO;
        private bool _is_jet;

        private int _atomType = 1;
        private int _propType = 1;

        private double _propellantSootFactorFullThrotle;
        private double _propellantSootFactorMinThrotle;
        private double _propellantSootFactorEquilibrium;
        private double _minDecompositionTemp;
        private double _maxDecompositionTemp;
        private double _decompositionEnergy;
        private double _baseIspMultiplier;
        private double _fuelToxicity;
        private double _ispPropellantMultiplier;
        private double _thrustPropellantMultiplier;
        private string _fuelTechRequirement;
        private double _fuelCoolingFactor;
        private bool _fuelRequiresUpgrade;

        public string FuelTechRequirement { get { return _fuelTechRequirement; } }
        public double FuelCoolingFactor { get { return _fuelCoolingFactor; } }
        public bool FuelRequiresUpgrade { get { return _fuelRequiresUpgrade; } }
        public int Index { get { return _index; } }
        public string Fuelmode { get {return _fuelmode;}}
        public double PropellantSootFactorFullThrotle { get { return _propellantSootFactorFullThrotle; } }
        public double PropellantSootFactorMinThrotle { get { return _propellantSootFactorMinThrotle; } }
        public double PropellantSootFactorEquilibrium { get { return _propellantSootFactorEquilibrium; } }
        public double MinDecompositionTemp { get { return _minDecompositionTemp; } }
        public double MaxDecompositionTemp { get { return _maxDecompositionTemp; } }
        public double DecompositionEnergy { get { return _decompositionEnergy; } }
        public double BaseIspMultiplier { get { return _baseIspMultiplier; } }
        public double FuelToxicity { get { return _fuelToxicity; } }
        public bool IsLFO { get { return _isLFO; } }
        public bool IsJet { get { return _is_jet; } }
        public int AtomType { get { return _atomType; } }
        public int PropType { get { return _propType; } }

        public double IspPropellantMultiplier { get { return _ispPropellantMultiplier; } }
        public double ThrustPropellantMultiplier { get { return _thrustPropellantMultiplier; } }

        public ThermalEngineFuel(ConfigNode node, int index)
        {
            _fuelmode = node.GetValue("guiName");
            _isLFO = node.HasValue("isLFO") ? bool.Parse(node.GetValue("isLFO")) : false;
            _is_jet = node.HasValue("isJet") ? bool.Parse(node.GetValue("isJet")) : false;

            _propellantSootFactorFullThrotle = node.HasValue("maxSootFactor") ? double.Parse(node.GetValue("maxSootFactor")) : 0;
            _propellantSootFactorMinThrotle = node.HasValue("minSootFactor") ? double.Parse(node.GetValue("minSootFactor")) : 0;
            _propellantSootFactorEquilibrium = node.HasValue("levelSootFraction") ? double.Parse(node.GetValue("levelSootFraction")) : 0;
            _minDecompositionTemp = node.HasValue("MinDecompositionTemp") ? double.Parse(node.GetValue("MinDecompositionTemp")) : 0;
            _maxDecompositionTemp = node.HasValue("MaxDecompositionTemp") ? double.Parse(node.GetValue("MaxDecompositionTemp")) : 0;
            _decompositionEnergy = node.HasValue("DecompositionEnergy") ? double.Parse(node.GetValue("DecompositionEnergy")) : 0;
            _baseIspMultiplier = node.HasValue("BaseIspMultiplier") ? double.Parse(node.GetValue("BaseIspMultiplier")) : 0;
            _fuelTechRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : string.Empty;
            _fuelCoolingFactor = node.HasValue("coolingFactor") ? float.Parse(node.GetValue("coolingFactor")) : 1;
            _fuelToxicity = node.HasValue("Toxicity") ? double.Parse(node.GetValue("Toxicity")) : 0;

            _fuelRequiresUpgrade = node.HasValue("RequiresUpgrade") ? bool.Parse(node.GetValue("RequiresUpgrade")) : false;
            _atomType = node.HasValue("atomType") ? int.Parse(node.GetValue("atomType")) : 1;
            _propType = node.HasValue("propType") ? int.Parse(node.GetValue("propType")) : 1;
            _ispPropellantMultiplier = node.HasValue("ispMultiplier") ? double.Parse(node.GetValue("ispMultiplier")) : 1;
            _thrustPropellantMultiplier = node.HasValue("thrustMultiplier") ? double.Parse(node.GetValue("thrustMultiplier")) : 1;
        }

    }
}
