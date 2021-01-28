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
            ModeGUIName = first.ModeGuiName;
            TechLevel = first.TechLevel;
            MinimumFusionGainFactor = first.MinimumFusionGainFactor;
            TechRequirement = first.TechRequirement;
            SupportedReactorTypes = first.SupportedReactorTypes;
            Aneutronic = first.Aneutronic;
            Hidden = first.Hidden;
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

        public int SupportedReactorTypes { get; }
        public int Index { get; }
        public string ModeGUIName { get; }
        public string TechRequirement { get; }
        public bool Aneutronic { get;}
        public bool Hidden { get; }
        public double GammaRayEnergy { get;  }
        public bool RequiresLab { get;  }
        public bool RequiresUpgrade { get; }
        public float ChargedPowerRatio { get; }
        public double MeVPerChargedProduct { get;}
        public float NormalisedReactionRate { get;}
        public float NormalisedPowerRequirements { get;}
        public int TechLevel { get; }
        public int MinimumFusionGainFactor { get; }
        public float NeutronsRatio { get;}
        public float TritiumBreedModifier { get;}
        public double FuelEfficencyMultiplier { get; }

        public string AlternativeFuelType1 { get;}
        public string AlternativeFuelType2 { get;}
        public string AlternativeFuelType3 { get;}
        public string AlternativeFuelType4 { get;}
        public string AlternativeFuelType5 { get; }

        public List<ReactorFuelMode> Variants { get;}
        public List<ResourceGroupMetaData> ResourceGroups { get; }

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
            Name = node.GetValue("name");
            ModeGuiName = node.GetValue("GUIName");
            SupportedReactorTypes = Convert.ToInt32(node.GetValue("ReactorType"));
            Index = node.HasValue("Index") ? int.Parse(node.GetValue("Index")) : 0;
            Hidden = node.HasValue("Hidden") && bool.Parse(node.GetValue("Hidden"));

            TechRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : string.Empty;

            _alternativeFuelType1 = node.HasValue("AlternativeFuelType1") ? node.GetValue("AlternativeFuelType1") : string.Empty;
            _alternativeFuelType2 = node.HasValue("AlternativeFuelType2") ? node.GetValue("AlternativeFuelType2") : string.Empty;
            _alternativeFuelType3 = node.HasValue("AlternativeFuelType3") ? node.GetValue("AlternativeFuelType3") : string.Empty;
            _alternativeFuelType4 = node.HasValue("AlternativeFuelType4") ? node.GetValue("AlternativeFuelType4") : string.Empty;
            _alternativeFuelType5 = node.HasValue("AlternativeFuelType5") ? node.GetValue("AlternativeFuelType5") : string.Empty;

            _reactionRate = node.HasValue("NormalisedReactionRate") ? float.Parse(node.GetValue("NormalisedReactionRate")) : 1;
            _powerMultiplier = node.HasValue("NormalisedPowerMultiplier") ? float.Parse(node.GetValue("NormalisedPowerMultiplier")) : 1;
            _normpowerrequirements = node.HasValue("NormalisedPowerConsumption") ? float.Parse(node.GetValue("NormalisedPowerConsumption")) : 1;
            _charged_power_ratio = node.HasValue("ChargedParticleRatio") ? float.Parse(node.GetValue("ChargedParticleRatio")) : 0;

            _mev_per_charged_product = node.HasValue("MeVPerChargedProduct") ? double.Parse(node.GetValue("MeVPerChargedProduct")) : 0;
            _neutrons_ratio = node.HasValue("NeutronsRatio") ? float.Parse(node.GetValue("NeutronsRatio")) : 1;
            _tritium_breed_multiplier = node.HasValue("TritiumBreedMultiplier") ? float.Parse(node.GetValue("TritiumBreedMultiplier")) : 1;
            _fuel_efficency_multiplier = node.HasValue("FuelEfficiencyMultiplier") ? double.Parse(node.GetValue("FuelEfficiencyMultiplier")) : 1;

            _requires_lab = node.HasValue("RequiresLab") && bool.Parse(node.GetValue("RequiresLab"));
            _requires_upgrade = node.HasValue("RequiresUpgrade") && bool.Parse(node.GetValue("RequiresUpgrade"));
            _techLevel = node.HasValue("TechLevel") ? int.Parse(node.GetValue("TechLevel")) : 0;
            _minimumQ = node.HasValue("MinimumQ") ? int.Parse(node.GetValue("MinimumQ")) : 0;
            _aneutronic = node.HasValue("Aneutronic") && bool.Parse(node.GetValue("Aneutronic"));
            _gammaRayEnergy = node.HasValue("GammaRayEnergy") ? double.Parse(node.GetValue("GammaRayEnergy")) : 0;


            ConfigNode[] fuelNodes = node.GetNodes("FUEL");
            ReactorFuels = fuelNodes.Select(nd => new ReactorFuel(nd)).ToList();

            ConfigNode[] productsNodes = node.GetNodes("PRODUCT");
            ReactorProducts = productsNodes.Select(nd => new ReactorProduct(nd)).ToList();

            AllFuelResourcesDefinitionsAvailable = ReactorFuels.All(m => m.Definition != null);
            AllProductResourcesDefinitionsAvailable = ReactorProducts.All(m => m.Definition != null);

            var totalTonsFuelUsePerMj = ReactorFuels.Sum(m => m.TonsFuelUsePerMj);

            _fuelUseInGramPerTeraJoule = totalTonsFuelUsePerMj * 1e12;

            _gigawattPerGram = 1 / (totalTonsFuelUsePerMj * 1e9);
        }

        public string AlternativeFuelType1 => _alternativeFuelType1;
        public string AlternativeFuelType2 => _alternativeFuelType2;
        public string AlternativeFuelType3 => _alternativeFuelType3;
        public string AlternativeFuelType4 => _alternativeFuelType4;
        public string AlternativeFuelType5 => _alternativeFuelType5;

        public int SupportedReactorTypes { get; }
        public int Index { get; }
        public bool Hidden { get; }

        public string Name { get; }

        public string ModeGuiName { get; }

        public string TechRequirement { get; }

        public IList<ReactorFuel> ReactorFuels { get; }

        public IList<ReactorProduct> ReactorProducts { get; }

        public bool Aneutronic => _aneutronic;

        public double GammaRayEnergy => _gammaRayEnergy;

        public bool RequiresLab => _requires_lab;

        public bool RequiresUpgrade => _requires_upgrade;

        public float ChargedPowerRatio => _charged_power_ratio;

        public double MeVPerChargedProduct => _mev_per_charged_product;

        public float NormalisedReactionRate => _reactionRate * _powerMultiplier;

        public float NormalisedPowerRequirements => _normpowerrequirements;

        public int TechLevel => _techLevel;

        public int MinimumFusionGainFactor => _minimumQ;

        public float NeutronsRatio => _neutrons_ratio;

        public float TritiumBreedModifier => _tritium_breed_multiplier;

        public double FuelEfficencyMultiplier => _fuel_efficency_multiplier;

        public double FuelUseInGramPerTeraJoule => _fuelUseInGramPerTeraJoule;

        public double GigawattPerGram => _gigawattPerGram;

        public int Position { get; set; }
        public double FuelRatio { get; set; }
        public bool AllFuelResourcesDefinitionsAvailable { get; }
        public bool AllProductResourcesDefinitionsAvailable { get; }
    }
}
