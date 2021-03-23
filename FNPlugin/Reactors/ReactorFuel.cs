using FNPlugin.Extensions;
using System;
using UnityEngine;

namespace FNPlugin.Reactors
{
    class ReactorFuel
    {
        public ReactorFuel(ConfigNode node)
        {
            FuelName = node.GetValue("name");
            Ratio = node.HasValue("ratio") ? Convert.ToDouble(node.GetValue("ratio")) : 1;
            Simulate = node.HasValue("simulate") && bool.Parse(node.GetValue("simulate"));
            ResourceName = node.HasValue("resource") ? node.GetValue("resource") : FuelName;
            TonsFuelUsePerMj = Convert.ToDouble(node.GetValue("UsagePerMW"));
            Unit = node.GetValue("Unit");
            ConsumeGlobal = !node.HasValue("consumeGlobal") || bool.Parse(node.GetValue("consumeGlobal"));

            Definition = PartResourceLibrary.Instance.GetDefinition(ResourceName);
            if (Definition == null)
                Debug.LogError("[KSPI]: No definition found for resource '" + ResourceName + "' for ReactorFuel " + FuelName);
            else
            {
                DensityInTon = (double)(decimal)Definition.density;
                DensityInKg = DensityInTon * 1000;
                AmountFuelUsePerMj = TonsFuelUsePerMj / DensityInTon;
            }
        }

        public PartResourceDefinition Definition { get; }

        public double Ratio { get; }
        public bool ConsumeGlobal { get; }
        public double DensityInTon { get; }
        public double DensityInKg { get; }
        public bool Simulate { get; }
        public double AmountFuelUsePerMj { get; }
        public double TonsFuelUsePerMj { get; }
        public string FuelName { get; }
        public string ResourceName { get; }
        public string Unit { get; }

        public double GetFuelRatio(Part part, double fuelEfficiency, double megajoules, double fuelUsePerMjMult, bool simulate)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            if (simulate)
                return 1;

            var fuelUseForPower = GetFuelUseForPower(fuelEfficiency, megajoules, fuelUsePerMjMult);

            return fuelUseForPower > 0 ?  Math.Min(GetFuelAvailability(part) / fuelUseForPower, 1) : 0;
        }

        public double GetFuelUseForPower(double efficiency, double megajoules, double fuelUsePerMjMult)
        {
            return efficiency > 0 ?  AmountFuelUsePerMj * fuelUsePerMjMult * megajoules / efficiency : 0;
        }

        public double GetFuelAvailability(Part part)
        {
            if (!ConsumeGlobal)
            {
                if (part.Resources.Contains(ResourceName))
                    return part.Resources[ResourceName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetResourceAvailable(Definition);
            else
                return part.FindAmountOfAvailableFuel(ResourceName, 4);
        }

    }

    class ReactorProduct
    {
        public ReactorProduct(ConfigNode node)
        {
            FuelName = node.GetValue("name");
            ResourceName = node.HasValue("resource") ? node.GetValue("resource") : FuelName;
            Unit = node.GetValue("Unit");
            Simulate = node.HasValue("simulate") && bool.Parse(node.GetValue("simulate"));
            IsPropellant = !node.HasValue("isPropellant") || bool.Parse(node.GetValue("isPropellant"));
            ProduceGlobal = !node.HasValue("produceGlobal") || bool.Parse(node.GetValue("produceGlobal"));
            TonsProductUsePerMj = Convert.ToDouble(node.GetValue("ProductionPerMW"));

            Definition = PartResourceLibrary.Instance.GetDefinition(FuelName);
            if (Definition == null)
                Debug.LogError("[KSPI]: No definition found for ReactorProduct '" + ResourceName + "'");
            else
            {
                DensityInTon = (double)(decimal)Definition.density;
                DensityInKg = DensityInTon * 1000;
                AmountProductUsePerMj = DensityInTon > 0 ? TonsProductUsePerMj / DensityInTon : 0;
            }
        }

        public PartResourceDefinition Definition { get; }

        public bool ProduceGlobal { get; }

        public bool IsPropellant { get; }
        public bool Simulate { get; }
        public double DensityInTon { get; }
        public double DensityInKg { get; }
        public double AmountProductUsePerMj { get; }
        public double TonsProductUsePerMj { get; }
        public string FuelName { get; }
        public string ResourceName { get; }
        public string Unit { get; }

        public double GetProductionForPower(double efficiency, double megajoules, double productionPerMjMult)
        {
            return AmountProductUsePerMj * productionPerMjMult * megajoules / efficiency;
        }
    }
}
