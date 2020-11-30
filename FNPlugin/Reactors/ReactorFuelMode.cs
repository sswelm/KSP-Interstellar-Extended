using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Reactors
{
    class ReactorProduction
    {
        public ReactorProduct fuelmode;
        public double mass;
    }

    class FuelResourceMetaData
    {
        public FuelResourceMetaData(PartResourceDefinition resourceDefinition, double ratio)
        {
            this.resourceDefinition = resourceDefinition;
            this.ratio = ratio;
        }

        public PartResourceDefinition resourceDefinition;
        public double ratio = 1;
    }

    class ResourceGroupMetaData
    {
        public string name;
        public List<FuelResourceMetaData> resourceVariantsMetaData;
    }


    class ReactorFuelType
    {
        public ReactorFuelType(IEnumerable<ReactorFuelMode> reactorFuelModes)
        {
            Variants = reactorFuelModes.ToList();

            ResourceGroups = new List<ResourceGroupMetaData>();
            foreach (var group in Variants.SelectMany(m => m.ReactorFuels).GroupBy(m => m.FuelName))
            {
                ResourceGroups.Add(new ResourceGroupMetaData()
                {
                    name = group.Key,
                    resourceVariantsMetaData = group.Select(m => new FuelResourceMetaData(m.Definition, m.Ratio)).Distinct().ToList()
                });
            }

            var first = Variants.First();

            AlternativeFuelType1 = first.AlternativeFuelType1;
            AlternativeFuelType2 = first.AlternativeFuelType2;
            AlternativeFuelType3 = first.AlternativeFuelType3;
            AlternativeFuelType4 = first.AlternativeFuelType4;
            AlternativeFuelType5 = first.AlternativeFuelType5;

            Index = first.Index;
            ModeGUIName = first.ModeGUIName;
            TechLevel = first.TechLevel;
            MinimumFusionGainFactor = first.MinimumFusionGainFactor;
            TechRequirement = first.TechRequirement;
            SupportedReactorTypes = first.SupportedReactorTypes;
            Aneutronic = first.Aneutronic;
            GammaRayEnergy = first.GammaRayEnergy;
            RequiresLab = first.RequiresLab;
            RequiresUpgrade = first.RequiresUpgrade;
            ChargedPowerRatio = first.ChargedPowerRatio;
            MeVPerChargedProduct = first.MeVPerChargedProduct;
            NormalisedReactionRate = first.NormalisedReactionRate;
            NormalisedPowerRequirements = first.NormalisedPowerRequirements;
            NeutronsRatio = first.NeutronsRatio;
            TritiumBreedModifier = first.TritiumBreedModifier;
            FuelEfficencyMultiplier = first.FuelEfficencyMultiplier;
        }

        public int SupportedReactorTypes { get; private set; }
        public int Index { get; private set; }
        public string ModeGUIName { get; private set; }
        public string TechRequirement { get; private set; }
        public bool Aneutronic { get; private set; }
        public double GammaRayEnergy { get; private set; }
        public bool RequiresLab { get; private set; }
        public bool RequiresUpgrade { get; private set; }
        public float ChargedPowerRatio { get; private set; }
        public double MeVPerChargedProduct { get; private set; }
        public float NormalisedReactionRate { get; private set; }
        public float NormalisedPowerRequirements { get; private set; }
        public int TechLevel { get; private set; }
        public int MinimumFusionGainFactor { get; private set; }
        public float NeutronsRatio { get; private set; }
        public float TritiumBreedModifier { get; private set; }
        public double FuelEfficencyMultiplier { get; private set; }

        public string AlternativeFuelType1 { get; private set; }
        public string AlternativeFuelType2 { get; private set; }
        public string AlternativeFuelType3 { get; private set; }
        public string AlternativeFuelType4 { get; private set; }
        public string AlternativeFuelType5 { get; private set; }

        public List<ReactorFuelMode> Variants { get; private set; }
        public List<ResourceGroupMetaData> ResourceGroups { get; private set; }

        // Methods
        public List<ReactorFuelMode> GetVariantsOrderedByFuelRatio(Part part, double fuelEfficiency, double powerToSupply, double fuelUsePerMjMult, bool allowSimulate = true)
        {
            foreach (var fuelMode in Variants)
            {
                fuelMode.FuelRatio = fuelMode.ReactorFuels.Min(fuel => fuel.GetFuelRatio(part, fuelEfficiency, powerToSupply, fuelUsePerMjMult, allowSimulate ? fuel.Simulate : false));
            }

            return Variants.OrderByDescending(m => m.FuelRatio).ThenBy(m => m.Position).ToList();
        }
    }

    class ReactorFuelMode
    {
        protected int _reactor_type;
        protected int _index;
        protected string _name;
        protected string _mode_gui_name;
        protected string _techRequirement;
        protected List<ReactorFuel> _fuels;
        protected List<ReactorProduct> _products;
        protected float _reactionRate;
        protected float _powerMultiplier;
        protected float _normpowerrequirements;
        protected float _charged_power_ratio;
        protected double _mev_per_charged_product;
        protected float _neutrons_ratio;
        protected float _tritium_breed_multiplier;
        protected double _fuel_efficency_multiplier;
        protected bool _requires_lab;
        protected bool _requires_upgrade;
        protected int _techLevel;
        protected int _minimumQ;
        protected bool _aneutronic;

        protected double _gammaRayEnergy;
        protected double _fuelUseInGramPerTeraJoule;
        protected double _gigawattPerGram;

        protected string _alternativeFuelType1;
        protected string _alternativeFuelType2;
        protected string _alternativeFuelType3;
        protected string _alternativeFuelType4;
        protected string _alternativeFuelType5;

        public ReactorFuelMode(ConfigNode node)
        {
            _name = node.GetValue("name");
            _mode_gui_name = node.GetValue("GUIName");
            _reactor_type = Convert.ToInt32(node.GetValue("ReactorType"));
            _index = node.HasValue("Index") ? int.Parse(node.GetValue("Index")) : 0;

            _techRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : String.Empty;

            _alternativeFuelType1 = node.HasValue("AlternativeFuelType1") ? node.GetValue("AlternativeFuelType1") : String.Empty;
            _alternativeFuelType2 = node.HasValue("AlternativeFuelType2") ? node.GetValue("AlternativeFuelType2") : String.Empty;
            _alternativeFuelType3 = node.HasValue("AlternativeFuelType3") ? node.GetValue("AlternativeFuelType3") : String.Empty;
            _alternativeFuelType4 = node.HasValue("AlternativeFuelType4") ? node.GetValue("AlternativeFuelType4") : String.Empty;
            _alternativeFuelType5 = node.HasValue("AlternativeFuelType5") ? node.GetValue("AlternativeFuelType5") : String.Empty;

            _reactionRate = node.HasValue("NormalisedReactionRate") ? Single.Parse(node.GetValue("NormalisedReactionRate")) : 1;
            _powerMultiplier = node.HasValue("NormalisedPowerMultiplier") ? Single.Parse(node.GetValue("NormalisedPowerMultiplier")) : 1;
            _normpowerrequirements = node.HasValue("NormalisedPowerConsumption") ? Single.Parse(node.GetValue("NormalisedPowerConsumption")) : 1;
            _charged_power_ratio = node.HasValue("ChargedParticleRatio") ? Single.Parse(node.GetValue("ChargedParticleRatio")) : 0;

            _mev_per_charged_product = node.HasValue("MeVPerChargedProduct") ? Double.Parse(node.GetValue("MeVPerChargedProduct")) : 0;
            _neutrons_ratio = node.HasValue("NeutronsRatio") ? Single.Parse(node.GetValue("NeutronsRatio")) : 1;
            _tritium_breed_multiplier = node.HasValue("TritiumBreedMultiplier") ? Single.Parse(node.GetValue("TritiumBreedMultiplier")) : 1;
            _fuel_efficency_multiplier = node.HasValue("FuelEfficiencyMultiplier") ? Double.Parse(node.GetValue("FuelEfficiencyMultiplier")) : 1;

            _requires_lab = node.HasValue("RequiresLab") ? Boolean.Parse(node.GetValue("RequiresLab")) : false;
            _requires_upgrade = node.HasValue("RequiresUpgrade") ? Boolean.Parse(node.GetValue("RequiresUpgrade")) : false;
            _techLevel = node.HasValue("TechLevel") ? Int32.Parse(node.GetValue("TechLevel")) : 0;
            _minimumQ = node.HasValue("MinimumQ") ? Int32.Parse(node.GetValue("MinimumQ")) : 0;
            _aneutronic = node.HasValue("Aneutronic") ? Boolean.Parse(node.GetValue("Aneutronic")) : false;
            _gammaRayEnergy = node.HasValue("GammaRayEnergy") ? Double.Parse(node.GetValue("GammaRayEnergy")) : 0;


            ConfigNode[] fuel_nodes = node.GetNodes("FUEL");
            _fuels = fuel_nodes.Select(nd => new ReactorFuel(nd)).ToList();

            ConfigNode[] products_nodes = node.GetNodes("PRODUCT");
            _products = products_nodes.Select(nd => new ReactorProduct(nd)).ToList();

            AllFuelResourcesDefinitionsAvailable = _fuels.All(m => m.Definition != null);
            AllProductResourcesDefinitionsAvailable = _products.All(m => m.Definition != null);

            var totalTonsFuelUsePerMJ = _fuels.Sum(m => m.TonsFuelUsePerMJ);

            _fuelUseInGramPerTeraJoule = totalTonsFuelUsePerMJ * 1e12;

            _gigawattPerGram = 1 / (totalTonsFuelUsePerMJ * 1e9);
        }

        public string AlternativeFuelType1 { get { return _alternativeFuelType1; } }
        public string AlternativeFuelType2 { get { return _alternativeFuelType2; } }
        public string AlternativeFuelType3 { get { return _alternativeFuelType3; } }
        public string AlternativeFuelType4 { get { return _alternativeFuelType4; } }
        public string AlternativeFuelType5 { get { return _alternativeFuelType5; } }

        public int SupportedReactorTypes { get { return _reactor_type; } }

        public int Index { get { return _index; } }

        public string Name { get { return _name; } }

        public string ModeGUIName { get { return _mode_gui_name; } }

        public string TechRequirement { get { return _techRequirement; } }

        public IList<ReactorFuel> ReactorFuels { get { return _fuels; } }

        public IList<ReactorProduct> ReactorProducts { get { return _products; } }

        public bool Aneutronic { get { return _aneutronic; } }

        public double GammaRayEnergy { get { return _gammaRayEnergy; } }

        public bool RequiresLab { get { return _requires_lab; } }

        public bool RequiresUpgrade { get { return _requires_upgrade; } }

        public float ChargedPowerRatio { get { return _charged_power_ratio; } }

        public double MeVPerChargedProduct { get { return _mev_per_charged_product; } }

        public float NormalisedReactionRate { get { return _reactionRate * _powerMultiplier; } }

        public float NormalisedPowerRequirements { get { return _normpowerrequirements; } }

        public int TechLevel { get { return _techLevel; } }

        public int MinimumFusionGainFactor { get { return _minimumQ; } }

        public float NeutronsRatio { get { return _neutrons_ratio; } }

        public float TritiumBreedModifier { get { return _tritium_breed_multiplier; } }    // calcula

        public double FuelEfficencyMultiplier { get { return _fuel_efficency_multiplier; } }

        public double FuelUseInGramPerTeraJoule { get { return _fuelUseInGramPerTeraJoule; } }

        public double GigawattPerGram { get { return _gigawattPerGram; } }

        public int Position { get; set; }

        public double FuelRatio { get; set; }

        public bool AllFuelResourcesDefinitionsAvailable { get; private set; }
        public bool AllProductResourcesDefinitionsAvailable { get; private set; }

    }
}
