using FNPlugin.Extensions;
using System;
using UnityEngine;

namespace FNPlugin.Reactors
{
    class ReactorFuel
    {
        double _tons_fuel_usage_per_mw;
        double _amountFuelUsePerMJ;
        string _fuel_name;
        string _resource_name;
        double _density;
        double _densityInKg;
        double _ratio;
        string _unit;
        bool _consumeGlobal;
        bool _simulate;

        public ReactorFuel(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _ratio = node.HasValue("ratio") ? Convert.ToDouble(node.GetValue("ratio")) : 1;
            _simulate = node.HasValue("simulate") ? Boolean.Parse(node.GetValue("simulate")) : false;
            _resource_name = node.HasValue("resource") ? node.GetValue("resource") : _fuel_name;
            _tons_fuel_usage_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            _unit = node.GetValue("Unit");
            _consumeGlobal = node.HasValue("consumeGlobal") ? Boolean.Parse(node.GetValue("consumeGlobal")) : true;

            Definition = PartResourceLibrary.Instance.GetDefinition(_resource_name);
            if (Definition == null)
                Debug.LogError("[KSPI]: No definition found for resource '" + _resource_name + "' for ReactorFuel " + _fuel_name);
            else
            {
                _density = (double)(decimal)Definition.density;
                _densityInKg = _density * 1000;
                _amountFuelUsePerMJ = _tons_fuel_usage_per_mw / _density;
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public double Ratio { get { return _ratio; } }

        public bool ConsumeGlobal { get { return _consumeGlobal; } }

        public double DensityInTon { get { return _density; } }

        public double DensityInKg { get { return _densityInKg; } }

        public bool Simulate { get { return _simulate; } }

        public double AmountFuelUsePerMJ { get { return _amountFuelUsePerMJ; } }

        public double TonsFuelUsePerMJ { get { return _tons_fuel_usage_per_mw; } }

        public double EnergyDensity { get { return _tons_fuel_usage_per_mw > 0 ?  0.001 / _tons_fuel_usage_per_mw : 0; } }

        public string FuelName { get { return _fuel_name; } }

        public string ResourceName { get { return _resource_name; } }

        public string Unit { get { return _unit; } }

        public double GetFuelRatio(Part part, double fuelEfficency, double megajoules, double fuelUsePerMJMult, bool simulate)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            if (simulate)
                return 1;

            var fuelUseForPower = this.GetFuelUseForPower(fuelEfficency, megajoules, fuelUsePerMJMult);

            return fuelUseForPower > 0 ?  Math.Min(this.GetFuelAvailability(part) / fuelUseForPower, 1) : 0;
        }

        public double GetFuelUseForPower(double efficiency, double megajoules, double fuelUsePerMJMult)
        {
            return efficiency > 0 ?  AmountFuelUsePerMJ * fuelUsePerMJMult * megajoules / efficiency : 0;
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
        double _tons_product_usage_per_mw;
        string _fuel_name;
        string _resource_name;
        double _density;
        double _densityInKg;
        string _unit;
        bool _isPropellant;
        bool _produceGlobal;
        bool _simulate;
        double _amountProductUsePerMJ;

        public ReactorProduct(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _resource_name = node.HasValue("resource") ? node.GetValue("resource") : _fuel_name;
            _unit = node.GetValue("Unit");
            _simulate = node.HasValue("simulate") ? Boolean.Parse(node.GetValue("simulate")) : false;
            _isPropellant = node.HasValue("isPropellant") ? Boolean.Parse(node.GetValue("isPropellant")) : true;
            _produceGlobal = node.HasValue("produceGlobal") ? Boolean.Parse(node.GetValue("produceGlobal")) : true;
            _tons_product_usage_per_mw = Convert.ToDouble(node.GetValue("ProductionPerMW"));

            Definition = PartResourceLibrary.Instance.GetDefinition(_fuel_name);
            if (Definition == null)
                Debug.LogError("[KSPI]: No definition found for ReactorProduct '" + _resource_name + "'");
            else
            {
                _density = (double)(decimal)Definition.density;
                _densityInKg = _density * 1000;
                _amountProductUsePerMJ = _density > 0 ? _tons_product_usage_per_mw / _density : 0;
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public bool ProduceGlobal { get { return _produceGlobal; } }

        public bool IsPropellant { get { return _isPropellant; } }

        public bool Simulate { get { return _simulate; } }

        public double DensityInTon { get { return _density; } }

        public double DensityInKg { get { return _densityInKg; } }

        public double AmountProductUsePerMJ { get { return _amountProductUsePerMJ; } }

        public double TonsProductUsePerMJ { get { return _tons_product_usage_per_mw; } }

        public double EnergyDensity { get { return 0.001 / _tons_product_usage_per_mw; } }

        public string FuelName { get { return _fuel_name; } }

        public string ResourceName { get { return _resource_name; } }

        public string Unit { get { return _unit; } }

        public double GetProductionForPower(double efficiency, double megajoules, double productionPerMJMult)
        {
            return AmountProductUsePerMJ * productionPerMJMult * megajoules / efficiency;
        }
    }
}
