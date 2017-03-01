using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin
{
    class ReactorProduction
    {
        public ReactorProduct fuelmode;
        public double mass;
    }

    public class ResourceGroup
    {
        public string name;
        public List<PartResourceDefinition> resources;
    }


    class ReactorFuelModeGroup
    {
        public ReactorFuelModeGroup(IEnumerable<ReactorFuelMode> reactorFuelModes)
        {
            Variants = reactorFuelModes.ToList();

            ResourceGroups = new List<ResourceGroup>();
            foreach (var group in Variants.SelectMany(m => m.ReactorFuels).GroupBy(m => m.FuelName))
            {
                ResourceGroups.Add(new ResourceGroup()
                {
                    name = group.Key,
                    resources = group.Select(m => m.Definition).Distinct().ToList()
                });
            }

            var first = Variants.First();

            Index = first.Index;
            ModeGUIName = first.ModeGUIName;
            TechLevel = first.TechLevel;
            TechRequirement = first.TechRequirement;
            SupportedReactorTypes = first.SupportedReactorTypes;
            Aneutronic = first.Aneutronic;
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
        public bool RequiresLab { get; private set; }
        public bool RequiresUpgrade { get; private set; }
        public float ChargedPowerRatio { get; private set; }
        public double MeVPerChargedProduct { get; private set; }
        public float NormalisedReactionRate { get; private set; }
        public float NormalisedPowerRequirements { get; private set; }
        public int TechLevel { get; private set; }
        public float NeutronsRatio { get; private set; }
        public float TritiumBreedModifier { get; private set; }
        public double FuelEfficencyMultiplier { get; private set; }

        public List<ReactorFuelMode> Variants { get; private set; }
        public List<ResourceGroup> ResourceGroups { get; private set; }

        // Methods
        public List<ReactorFuelMode> GetVariantsOrderedByFuelRatio(Part part, double FuelEfficiency, double powerToSupply, double fuelUsePerMJMult)
        {
            foreach (var fuelMode in Variants)
            {
                fuelMode.FuelRatio = fuelMode.ReactorFuels.Min(fuel => fuel.GetFuelRatio(part, FuelEfficiency, powerToSupply, fuelUsePerMJMult));
            }

            return Variants.OrderByDescending(m => m.FuelRatio).ThenBy(m => m.Position).ToList();
        }
    }

    class ReactorFuelMode
    {
        protected int _reactor_type;
        protected int _index;
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
        protected double _fuel_efficency_multiplier;
        protected bool _requires_lab;
        protected bool _requires_upgrade;
        protected int _techLevel;
        protected bool _aneutronic;

        public ReactorFuelMode(ConfigNode node)
        {
            _mode_gui_name = node.GetValue("GUIName");
            _reactor_type = Convert.ToInt32(node.GetValue("ReactorType"));
            _index = node.HasValue("Index") ? int.Parse(node.GetValue("Index")) : 0;

            _techRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : String.Empty;

            _reactionRate = node.HasValue("NormalisedReactionRate") ? Single.Parse(node.GetValue("NormalisedReactionRate")) : 1;
            _powerMultiplier = node.HasValue("NormalisedPowerMultiplier") ? Single.Parse(node.GetValue("NormalisedPowerMultiplier")) : 1;
            _normpowerrequirements = node.HasValue("NormalisedPowerConsumption") ? Single.Parse(node.GetValue("NormalisedPowerConsumption")) : 1;
            _charged_power_ratio = node.HasValue("ChargedParticleRatio") ? Single.Parse(node.GetValue("ChargedParticleRatio")) : 0;

            _mev_per_charged_product = node.HasValue("MeVPerChargedProduct") ? Double.Parse(node.GetValue("MeVPerChargedProduct")) : 0;
            _neutrons_ratio = node.HasValue("NeutronsRatio") ? Single.Parse(node.GetValue("NeutronsRatio")) : 1;
            _fuel_efficency_multiplier = node.HasValue("FuelEfficiencyMultiplier") ? Double.Parse(node.GetValue("FuelEfficiencyMultiplier")) : 1;

            _requires_lab = node.HasValue("RequiresLab") ? Boolean.Parse(node.GetValue("RequiresLab")) : false;
            _requires_upgrade = node.HasValue("RequiresUpgrade") ? Boolean.Parse(node.GetValue("RequiresUpgrade")) : false;
            _techLevel = node.HasValue("TechLevel") ? Int32.Parse(node.GetValue("TechLevel")) : 0;
            _aneutronic = node.HasValue("Aneutronic") ? Boolean.Parse(node.GetValue("Aneutronic")) : false;

            ConfigNode[] fuel_nodes = node.GetNodes("FUEL");
            _fuels = fuel_nodes.Select(nd => new ReactorFuel(nd)).ToList();

            ConfigNode[] products_nodes = node.GetNodes("PRODUCT");
            _products = products_nodes.Select(nd => new ReactorProduct(nd)).ToList();
        }

        public int SupportedReactorTypes { get { return _reactor_type; } }

        public int Index { get { return _index; } }

        public string ModeGUIName { get { return _mode_gui_name; } }

        public string TechRequirement { get { return _techRequirement; } }

        public IList<ReactorFuel> ReactorFuels { get { return _fuels; } }

        public IList<ReactorProduct> ReactorProducts { get { return _products; } }

        public bool Aneutronic { get { return _aneutronic; } }

        public bool RequiresLab { get { return _requires_lab; } }

        public bool RequiresUpgrade { get { return _requires_upgrade; } }

        public float ChargedPowerRatio { get { return _charged_power_ratio; } }

        public double MeVPerChargedProduct { get { return _mev_per_charged_product; } }

        public float NormalisedReactionRate { get { return _reactionRate * _powerMultiplier; } }

        public float NormalisedPowerRequirements { get { return _normpowerrequirements; } }

        public int TechLevel { get { return _techLevel; } }

        public float NeutronsRatio { get { return _neutrons_ratio; } }

        public float TritiumBreedModifier { get { return _neutrons_ratio; } }

        public double FuelEfficencyMultiplier { get { return _fuel_efficency_multiplier; } }

        public int Position { get; set; }

        public double FuelRatio { get; set; }
    }
}
