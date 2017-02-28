using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FNPlugin.Extensions;

namespace FNPlugin 
{
    class ReactorFuel 
    {
        protected double _tons_fuel_usage_per_mw;
        protected double _amountFuelUsePerMJ;
        protected string _fuel_name;
        protected string _resource_name;
        protected double _density;
        protected double _densityInKg;
        protected string _unit;
        protected bool _consumeGlobal;
        protected PartResourceDefinition _definition;

        public ReactorFuel(ConfigNode node) 
        {
            _fuel_name = node.GetValue("name");
            _resource_name = node.HasValue("resource") ? node.GetValue("resource") : _fuel_name;
            _unit = node.GetValue("Unit");
            _tons_fuel_usage_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            _definition = PartResourceLibrary.Instance.GetDefinitionSafe(_resource_name);
            _density =  _definition != null ? _definition.density : 0.0005;
            _densityInKg = _density * 1000;
            _amountFuelUsePerMJ = _tons_fuel_usage_per_mw / _density;
            _consumeGlobal = node.HasValue("consumeGlobal") ? Boolean.Parse(node.GetValue("consumeGlobal")) : true;
        }

        public PartResourceDefinition Definition { get { return _definition; } }

        public bool ConsumeGlobal { get { return _consumeGlobal; } }

        public double DensityInTon { get { return _density; } }

        public double DensityInKg { get { return _densityInKg; } }

        public double AmountFuelUsePerMJ { get { return _amountFuelUsePerMJ; } }

        public double TonsFuelUsePerMJ { get { return _tons_fuel_usage_per_mw; } }

        public double EnergyDensity { get { return 0.001/_tons_fuel_usage_per_mw; } }

        public string FuelName { get { return _fuel_name; } }

        public string ResourceName { get { return _resource_name; } }

        public string Unit { get { return _unit; } }

        public double GetFuelRatio(Part part, double fuelEfficency, double megajoules, double fuelUsePerMJMult)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            var fuelUseForPower = this.GetFuelUseForPower(fuelEfficency, megajoules, fuelUsePerMJMult);

            return Math.Min(this.GetFuelAvailability(part) / fuelUseForPower, 1);
        }

        public double GetFuelUseForPower(double efficiency, double megajoules, double fuelUsePerMJMult)
        {
            return AmountFuelUsePerMJ * fuelUsePerMJMult * megajoules / efficiency;
        }

        public double GetFuelAvailability(Part part)
        {
            if (!this.ConsumeGlobal)
            {
                if (part.Resources.Contains(this.ResourceName))
                    return part.Resources[this.ResourceName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(this.Definition);
            else
                return part.FindAmountOfAvailableFuel(this.ResourceName, 4);
        }

    }

    class ReactorProduct
    {
        protected double _tons_product_usage_per_mw;
        protected string _fuel_name;
        protected double _density;
        protected double _densityInKg;
        protected string _unit;

        protected bool _produceGlobal;
        protected double _amountProductUsePerMJ;
        protected PartResourceDefinition _definition;

        public ReactorProduct(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _unit = node.GetValue("Unit");

            _definition = PartResourceLibrary.Instance.GetDefinitionSafe(_fuel_name);
            _density = _definition != null ?_definition.density : 0.0005;
            _densityInKg = _density * 1000;
            _tons_product_usage_per_mw = Convert.ToDouble(node.GetValue("ProductionPerMW"));
            _amountProductUsePerMJ = _tons_product_usage_per_mw / _density;
            _produceGlobal = node.HasValue("produceGlobal") ? Boolean.Parse(node.GetValue("produceGlobal")) : false;
        }

        public PartResourceDefinition Definition { get { return _definition; } }

        public bool ProduceGlobal { get { return _produceGlobal; } }

        public double DensityInTon { get { return _density; } }

        public double DensityInKg { get { return _densityInKg; } }

        public double AmountProductUsePerMJ { get { return _amountProductUsePerMJ; } }

        public double TonsProductUsePerMJ { get { return _tons_product_usage_per_mw; } }

        public double EnergyDensity { get { return 0.001 / _tons_product_usage_per_mw; } }

        public string FuelName { get { return _fuel_name; } }

        public string Unit { get { return _unit; } }

        public double GetProductionForPower(double efficiency, double megajoules, double productionPerMJMult)
        {
            return AmountProductUsePerMJ * productionPerMJMult * megajoules / efficiency;
        }

    }


}
