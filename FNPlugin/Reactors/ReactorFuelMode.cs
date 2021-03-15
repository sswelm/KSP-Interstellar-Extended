using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;

namespace FNPlugin.Reactors
{
    class ReactorProduction
    {
        public ReactorProduct fuelMode;
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
        public double ratio;
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
            AlternativeFuelType6 = first.AlternativeFuelType6;

            Index = first.Index;
            ModeGUIName = first.ModeGuiName;
            DisplayName = first.DisplayName;
            TechLevel = first.TechLevel;
            MinimumFusionGainFactor = first.MinimumQ;
            TechRequirement = first.TechRequirement;
            SupportedReactorTypes = first.SupportedReactorTypes;
            Aneutronic = first.Aneutronic;
            Hidden = first.Hidden;
            GammaRayEnergy = first.GammaRayEnergy;
            RequiresLab = first.RequiresLab;
            RequiresUpgrade = first.RequiresUpgrade;
            ChargedPowerRatio = first.ChargedPowerRatio;
            MeVPerChargedProduct = first.MeVPerChargedProduct;
            ReactionRatePowerMultiplier = first.ReactionRatePowerMultiplier;
            NormalizedPowerMultiplier = first.NormalizedPowerMultiplier;
            NormalizedPowerRequirements = first.NormalizedPowerRequirements;
            NeutronsRatio = first.NeutronsRatio;
            TritiumBreedModifier = first.TritiumBreedModifier;
            FuelEfficiencyMultiplier = first.FuelEfficiencyMultiplier;
        }

        public bool Aneutronic { get;}
        public bool Hidden { get; }
        public bool RequiresLab { get;  }
        public bool RequiresUpgrade { get; }

        public int TechLevel { get; }
        public int MinimumFusionGainFactor { get; }
        public int SupportedReactorTypes { get; }
        public int Index { get; }

        public float NeutronsRatio { get;}
        public float TritiumBreedModifier { get;}
        public float ReactionRatePowerMultiplier { get; }
        public float NormalizedPowerMultiplier { get; }
        public float NormalizedPowerRequirements { get; }
        public float ChargedPowerRatio { get; }

        public double MeVPerChargedProduct { get; }
        public double FuelEfficiencyMultiplier { get; }
        public double GammaRayEnergy { get; }

        public string AlternativeFuelType1 { get;}
        public string AlternativeFuelType2 { get;}
        public string AlternativeFuelType3 { get;}
        public string AlternativeFuelType4 { get;}
        public string AlternativeFuelType5 { get; }
        public string AlternativeFuelType6 { get; }

        public string ModeGUIName { get; }
        public string DisplayName { get; }
        public string TechRequirement { get; }

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
        protected float reactionRate;
        protected float normalizedPowerMultiplier;

        public ReactorFuelMode(ConfigNode node)
        {
            Name = node.GetValue("name");
            ModeGuiName = node.GetValue("GUIName");
            DisplayName = node.HasValue("DisplayName") ? Localizer.Format(node.GetValue("DisplayName")) : ModeGuiName;
            SupportedReactorTypes = Convert.ToInt32(node.GetValue("ReactorType"));
            Index = node.HasValue("Index") ? int.Parse(node.GetValue("Index")) : 0;
            Hidden = node.HasValue("Hidden") && bool.Parse(node.GetValue("Hidden"));

            TechRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : string.Empty;

            AlternativeFuelType1 = node.HasValue("AlternativeFuelType1") ? node.GetValue("AlternativeFuelType1") : string.Empty;
            AlternativeFuelType2 = node.HasValue("AlternativeFuelType2") ? node.GetValue("AlternativeFuelType2") : string.Empty;
            AlternativeFuelType3 = node.HasValue("AlternativeFuelType3") ? node.GetValue("AlternativeFuelType3") : string.Empty;
            AlternativeFuelType4 = node.HasValue("AlternativeFuelType4") ? node.GetValue("AlternativeFuelType4") : string.Empty;
            AlternativeFuelType5 = node.HasValue("AlternativeFuelType5") ? node.GetValue("AlternativeFuelType5") : string.Empty;
            AlternativeFuelType6 = node.HasValue("AlternativeFuelType6") ? node.GetValue("AlternativeFuelType6") : string.Empty;

            reactionRate = node.HasValue("NormalisedReactionRate") ? float.Parse(node.GetValue("NormalisedReactionRate")) : 1;
            normalizedPowerMultiplier = node.HasValue("NormalisedPowerMultiplier") ? float.Parse(node.GetValue("NormalisedPowerMultiplier")) : 1;
            NormalizedPowerRequirements = node.HasValue("NormalisedPowerConsumption") ? float.Parse(node.GetValue("NormalisedPowerConsumption")) : 1;
            ChargedPowerRatio = node.HasValue("ChargedParticleRatio") ? float.Parse(node.GetValue("ChargedParticleRatio")) : 0;

            MeVPerChargedProduct = node.HasValue("MeVPerChargedProduct") ? double.Parse(node.GetValue("MeVPerChargedProduct")) : 0;
            NeutronsRatio = node.HasValue("NeutronsRatio") ? float.Parse(node.GetValue("NeutronsRatio")) : 1;
            TritiumBreedModifier = node.HasValue("TritiumBreedMultiplier") ? float.Parse(node.GetValue("TritiumBreedMultiplier")) : 1;
            FuelEfficiencyMultiplier = node.HasValue("FuelEfficiencyMultiplier") ? double.Parse(node.GetValue("FuelEfficiencyMultiplier")) : 1;

            RequiresLab = node.HasValue("RequiresLab") && bool.Parse(node.GetValue("RequiresLab"));
            RequiresUpgrade = node.HasValue("RequiresUpgrade") && bool.Parse(node.GetValue("RequiresUpgrade"));
            TechLevel = node.HasValue("TechLevel") ? int.Parse(node.GetValue("TechLevel")) : 0;
            MinimumQ = node.HasValue("MinimumQ") ? int.Parse(node.GetValue("MinimumQ")) : 0;
            Aneutronic = node.HasValue("Aneutronic") && bool.Parse(node.GetValue("Aneutronic"));
            GammaRayEnergy = node.HasValue("GammaRayEnergy") ? double.Parse(node.GetValue("GammaRayEnergy")) : 0;


            ConfigNode[] fuelNodes = node.GetNodes("FUEL");
            ReactorFuels = fuelNodes.Select(nd => new ReactorFuel(nd)).ToList();

            ConfigNode[] productsNodes = node.GetNodes("PRODUCT");
            ReactorProducts = productsNodes.Select(nd => new ReactorProduct(nd)).ToList();

            AllFuelResourcesDefinitionsAvailable = ReactorFuels.All(m => m.Definition != null);
            AllProductResourcesDefinitionsAvailable = ReactorProducts.All(m => m.Definition != null);

            var totalTonsFuelUsePerMj = ReactorFuels.Sum(m => m.TonsFuelUsePerMj);

            FuelUseInGramPerTeraJoule = totalTonsFuelUsePerMj * 1e12;

            GigawattPerGram = 1 / (totalTonsFuelUsePerMj * 1e9);
        }

        public float NormalizedPowerMultiplier => normalizedPowerMultiplier;
        public float ReactionRatePowerMultiplier => reactionRate * normalizedPowerMultiplier;

        public IList<ReactorFuel> ReactorFuels { get; }
        public IList<ReactorProduct> ReactorProducts { get; }

        public string AlternativeFuelType1 { get; }
        public string AlternativeFuelType2 { get; }
        public string AlternativeFuelType3 { get; }
        public string AlternativeFuelType4 { get; }
        public string AlternativeFuelType5 { get; }
        public string AlternativeFuelType6 { get; }

        public string Name { get; }
        public string ModeGuiName { get; }
        public string DisplayName { get; }
        public string TechRequirement { get; }

        public int SupportedReactorTypes { get; }
        public int Index { get; }
        public int Position { get; set; }
        public int TechLevel { get; }
        public int MinimumQ { get; }

        public bool Hidden { get; }
        public bool RequiresLab { get; }
        public bool RequiresUpgrade { get; }
        public bool Aneutronic { get; }
        public bool AllFuelResourcesDefinitionsAvailable { get; }
        public bool AllProductResourcesDefinitionsAvailable { get; }

        public double FuelEfficiencyMultiplier { get; }
        public double FuelUseInGramPerTeraJoule { get; }
        public double GigawattPerGram { get; }
        public double GammaRayEnergy { get; }
        public double MeVPerChargedProduct { get; }
        public double FuelRatio { get; set; }

        public float NormalizedPowerRequirements { get; }
        public float NeutronsRatio { get; }
        public float TritiumBreedModifier { get; }
        public float ChargedPowerRatio { get; }

    }
}
