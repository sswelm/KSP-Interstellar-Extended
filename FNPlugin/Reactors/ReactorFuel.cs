using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    class ReactorFuel 
    {
        protected double _tons_fuel_usage_per_mw;
        protected double _amountFuelUsePerMJ;
        protected string _fuel_name;
        protected double _density;
        protected double _densityInKg;
        protected string _unit;
        protected bool _consumeGlobal;
        

        public ReactorFuel(ConfigNode node) 
        {
            _fuel_name = node.GetValue("name");
            _tons_fuel_usage_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            _unit = node.GetValue("Unit");
            _density = PartResourceLibrary.Instance.GetDefinition(_fuel_name).density;
            _densityInKg = _density * 1000;
            _amountFuelUsePerMJ = _tons_fuel_usage_per_mw / _density;
            _consumeGlobal = node.HasValue("consumeGlobal") ? Boolean.Parse(node.GetValue("consumeGlobal")) : true;
        }

        public bool ConsumeGlobal { get { return _consumeGlobal; } }

        public double DensityInTon { get { return _density; } }

        public double DensityInKg { get { return _densityInKg; } }

        public double AmountFuelUsePerMJ { get { return _amountFuelUsePerMJ; } }

        public double TonsFuelUsePerMJ { get { return _tons_fuel_usage_per_mw; } }

        public double EnergyDensity { get { return 0.001/_tons_fuel_usage_per_mw; } }

        public string FuelName { get { return _fuel_name; } }

        public string Unit { get { return _unit; } }

        public double GetFuelUseForPower(double efficiency, double megajoules, double fuelUsePerMJMult)
        {
            return AmountFuelUsePerMJ * fuelUsePerMJMult * megajoules / efficiency;
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

        public ReactorProduct(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _density = PartResourceLibrary.Instance.GetDefinition(_fuel_name).density;
            _densityInKg = _density * 1000;

            _tons_product_usage_per_mw = Convert.ToDouble(node.GetValue("ProductionPerMW"));
            _amountProductUsePerMJ = _tons_product_usage_per_mw / _density;
            _unit = node.GetValue("Unit");
            _produceGlobal = node.HasValue("produceGlobal") ? Boolean.Parse(node.GetValue("produceGlobal")) : false;
        }

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
