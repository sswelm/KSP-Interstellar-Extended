﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
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

        public ReactorFuel(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _ratio = node.HasValue("ratio") ? Convert.ToDouble(node.GetValue("ratio")) : 1;
            _resource_name = node.HasValue("resource") ? node.GetValue("resource") : _fuel_name;
            _tons_fuel_usage_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            _unit = node.GetValue("Unit");
            _consumeGlobal = node.HasValue("consumeGlobal") ? Boolean.Parse(node.GetValue("consumeGlobal")) : true;

            Definition = PartResourceLibrary.Instance.GetDefinition(_resource_name);
            if (Definition == null)
                Debug.LogError("[KSPI] - No definition found for resource '" + _resource_name + "' for ReactorFuel " + _fuel_name);
            else
            {
                _density = Definition.density;
                _densityInKg = _density * 1000;
                _amountFuelUsePerMJ = _tons_fuel_usage_per_mw / _density;
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public double Ratio { get { return _ratio; } }

        public bool ConsumeGlobal { get { return _consumeGlobal; } }

        public double DensityInTon { get { return _density; } }

        public double DensityInKg { get { return _densityInKg; } }

        public double AmountFuelUsePerMJ { get { return _amountFuelUsePerMJ; } }

        public double TonsFuelUsePerMJ { get { return _tons_fuel_usage_per_mw; } }

        public double EnergyDensity { get { return _tons_fuel_usage_per_mw > 0 ?  0.001 / _tons_fuel_usage_per_mw : 0; } }

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
        double _tons_product_usage_per_mw;
        string _fuel_name;
        string _resource_name;
        double _density;
        double _densityInKg;
        string _unit;

        bool _produceGlobal;
        double _amountProductUsePerMJ;

        public ReactorProduct(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _resource_name = node.HasValue("resource") ? node.GetValue("resource") : _fuel_name;
            _unit = node.GetValue("Unit");
            _produceGlobal = node.HasValue("produceGlobal") ? Boolean.Parse(node.GetValue("produceGlobal")) : false;
            _tons_product_usage_per_mw = Convert.ToDouble(node.GetValue("ProductionPerMW"));

            Definition = PartResourceLibrary.Instance.GetDefinition(_fuel_name);
            if (Definition == null)
                Debug.LogError("[KSPI] - No definition found for ReactorProduct '" + _resource_name + "'");
            else
            {
                _density = Definition.density;
                _densityInKg = _density * 1000;
                _amountProductUsePerMJ = _tons_product_usage_per_mw / _density;
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public bool ProduceGlobal { get { return _produceGlobal; } }

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
