using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Constants;
using FNPlugin.Extensions;

namespace FNPlugin
{
    public class PowerDistribution
    {
        public double Power_requested { get; set; }
        public double Power_consumed { get; set; }
    }

    public class PowerGenerated
    {
        public PowerGenerated()
        {
            efficiencyRatio = 1;
        }

        public double currentSupply { get; set; }
        public double averageSupply { get; set; }
        public double currentProvided { get; set; }
        public double maximumSupply { get; set; }
        public double minimumSupply { get; set; }
        public double efficiencyRatio { get; set; }
    }

    public class PowerProduction
    {
        public string component { get; set; }

        public double averageSupply { get; set; }

        public double maximumSupply { get; set; }
    }

    public class PowerConsumption
    {
        public string component { get; set; }

        public double sum { get; set; }

        public int priority { get; set; }
    }

    public class ResourceManager
    {
        public Guid OverManagerId { get; private set; }
        public Guid Id { get; private set; }

        public const string STOCK_RESOURCE_ELECTRICCHARGE = "ElectricCharge";
        public const string FNRESOURCE_MEGAJOULES = "Megajoules";
        public const string FNRESOURCE_CHARGED_PARTICLES = "ChargedParticles";
        public const string FNRESOURCE_THERMALPOWER = "ThermalPower";
        public const string FNRESOURCE_WASTEHEAT = "WasteHeat";

        public const double ONE_THIRD = 1.0 / 3.0;
        private const int FNRESOURCE_FLOWTYPE_SMALLEST_FIRST = 0;
        private const int FNRESOURCE_FLOWTYPE_EVEN = 1;

        Vessel my_vessel;
        Part my_part;
        PartModule my_partmodule;

        PartResourceDefinition resourceDefinition;
        PartResourceDefinition wasteheatResourceDefinition;
        PartResourceDefinition electricResourceDefinition;
        PartResourceDefinition megajouleResourceDefinition;
        PartResourceDefinition thermalpowerResourceDefinition;
        PartResourceDefinition chargedpowerResourceDefinition;

        Dictionary<IResourceSuppliable, PowerDistribution> power_consumption;
        Dictionary<IResourceSupplier, PowerGenerated> power_produced;
        Dictionary<IResourceSupplier, Queue<double>> power_produced_history = new Dictionary<IResourceSupplier, Queue<double>>();

        string resource_name;

        double currentPowerSupply = 0;
        double stable_supply = 0;

        double stored_stable_supply = 0;
        double stored_resource_demand = 0;
        double stored_current_hp_demand = 0;
        double stored_current_demand = 0;
        double stored_current_charge_demand = 0;
        double stored_supply = 0;
        double stored_total_power_supplied = 0;

        double current_resource_demand = 0;
        double high_priority_resource_demand = 0;
        double charge_resource_demand = 0;
        double total_power_distributed = 0;
        double internl_power_extract_fixed = 0;
        double resource_bar_ratio_begin = 0;

        double current_requested_amount = 0;
        double current_consumed_amount = 0;

        double previous_stock_electric_charge_shortage;

        int flow_type = 0;
        List<KeyValuePair<IResourceSuppliable, PowerDistribution>> power_draw_list_archive;
        List<KeyValuePair<IResourceSupplier, PowerGenerated>> power_supply_list_archive;

        protected Queue<double> efficiencyQueue = new Queue<double>();

        bool render_window = false;
        bool producesWasteHeat;

        Rect windowPosition = new Rect(50, 50, 300, 100);
        int windowID = 36549835;

        double temperature_ratio = 0;
        double sqrt_resource_bar_ratio_begin = 0;
        double resource_bar_ratio_end = 0;
        double radiator_efficiency;

        const double passive_temp_p4 = 2947.295521;

        const int labelWidth = 240;
        const int valueWidth = 55;
        const int priorityWidth = 30;
        const int overviewWidth = 65;

        GUIStyle left_bold_label;
        GUIStyle right_bold_label;
        GUIStyle green_label;
        GUIStyle red_label;
        GUIStyle left_aligned_label;
        GUIStyle right_aligned_label;

        public double CurrentRemainingRequestedAmount
        {
            get { return Math.Max(0, ResourceSupply - CurrentRequestedAmount); }
        }

        public double CurrentRemainingConsumedAmount
        {
            get { return Math.Max(0, ResourceSupply - CurrentConsumedAmount); }
        }

        public double CurrentRequestedAmount
        {
            get { return current_requested_amount; }
        }

        public double CurrentConsumedAmount
        {
            get { return current_consumed_amount; }
        }

        public Rect WindowPosition
        {
            get { return windowPosition; }
            set { windowPosition = value; }
        }

        public int WindowID
        {
            get { return windowID; }
            set { windowID = value; }
        }

        public ResourceManager(Guid overmanagerId, PartModule pm, String resource_name)
        {
            OverManagerId = overmanagerId;
            Id = Guid.NewGuid();

            int xPos = 0;
            int yPos = 0;

            if (resource_name == ResourceManager.FNRESOURCE_MEGAJOULES)
            {
                xPos = 50;
                yPos = 50;
            }
            else if (resource_name == ResourceManager.FNRESOURCE_THERMALPOWER)
            {
                xPos = 600;
                yPos = 50;
            }
            else if (resource_name == ResourceManager.FNRESOURCE_CHARGED_PARTICLES)
            {
                xPos = 50;
                yPos = 600;
            }
            else if (resource_name == ResourceManager.FNRESOURCE_WASTEHEAT)
            {
                xPos = 600;
                yPos = 600;
            }

            windowPosition = new Rect(xPos, yPos, labelWidth + valueWidth + priorityWidth, 50);

            my_vessel = pm.vessel;
            my_part = pm.part;
            my_partmodule = pm;

            windowID = new System.Random(resource_name.GetHashCode()).Next(int.MinValue, int.MaxValue);

            power_consumption = new Dictionary<IResourceSuppliable, PowerDistribution>();
            power_produced = new Dictionary<IResourceSupplier, PowerGenerated>();

            this.resource_name = resource_name;

            resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resource_name);
            wasteheatResourceDefinition = PartResourceLibrary.Instance.GetDefinition(FNRESOURCE_WASTEHEAT);
            electricResourceDefinition = PartResourceLibrary.Instance.GetDefinition(STOCK_RESOURCE_ELECTRICCHARGE);
            megajouleResourceDefinition = PartResourceLibrary.Instance.GetDefinition(FNRESOURCE_MEGAJOULES);
            thermalpowerResourceDefinition = PartResourceLibrary.Instance.GetDefinition(FNRESOURCE_THERMALPOWER);
            chargedpowerResourceDefinition = PartResourceLibrary.Instance.GetDefinition(FNRESOURCE_CHARGED_PARTICLES);

            producesWasteHeat = resourceDefinition.id == thermalpowerResourceDefinition.id || resourceDefinition.id == chargedpowerResourceDefinition.id;

            if (resource_name == FNRESOURCE_WASTEHEAT || resource_name == FNRESOURCE_THERMALPOWER || resource_name == FNRESOURCE_CHARGED_PARTICLES)
                flow_type = FNRESOURCE_FLOWTYPE_EVEN;
            else
                flow_type = FNRESOURCE_FLOWTYPE_SMALLEST_FIRST;
        }

        public void powerDrawFixed(IResourceSuppliable pm, double power_draw, double power_cosumtion)
        {
            if (power_draw.IsInfinityOrNaN())
                return;
            if (power_cosumtion.IsInfinityOrNaN())
                return;

            var timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;
            var power_draw_per_second = power_draw / timeWarpFixedDeltaTime;
            var power_cosumtion_per_second = power_cosumtion / timeWarpFixedDeltaTime;

            PowerDistribution powerDistribution;
            if (!power_consumption.TryGetValue(pm, out powerDistribution))
            {
                powerDistribution = new PowerDistribution();
                power_consumption.Add(pm, powerDistribution);
            }
            powerDistribution.Power_requested += power_draw_per_second;
            powerDistribution.Power_consumed += power_cosumtion_per_second;
        }

        public void powerDrawPerSecond(IResourceSuppliable pm, double power_requested, double power_consumed)
        {
            if (power_requested.IsInfinityOrNaN())
                return;
            if (power_consumed.IsInfinityOrNaN())
                return;

            current_requested_amount += power_requested;
            current_consumed_amount += power_consumed;

            PowerDistribution powerDistribution;
            if (!power_consumption.TryGetValue(pm, out powerDistribution))
            {
                powerDistribution = new PowerDistribution();
                power_consumption.Add(pm, powerDistribution);
            }
            powerDistribution.Power_requested += power_requested;
            powerDistribution.Power_consumed += power_consumed;
        }

        public double powerSupplyFixed(IResourceSupplier pm, double power)
        {
            if (power.IsInfinityOrNaN())
                return 0;

            var current_power_supply_per_second = power / TimeWarpFixedDeltaTime;

            currentPowerSupply += current_power_supply_per_second;
            stable_supply += current_power_supply_per_second;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += current_power_supply_per_second;
            powerGenerated.currentProvided += current_power_supply_per_second;
            powerGenerated.maximumSupply += current_power_supply_per_second;

            return power;
        }

        public double powerSupplyPerSecond(IResourceSupplier pm, double power)
        {
            if (power.IsInfinityOrNaN())
                return 0;

            currentPowerSupply += power;
            stable_supply += power;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += power;
            powerGenerated.currentProvided += power;
            powerGenerated.maximumSupply += power;

            return power;
        }

        public double powerSupplyFixedWithMax(IResourceSupplier pm, double power, double maxpower)
        {
            if (power.IsInfinityOrNaN())
                return 0;
            if (maxpower.IsInfinityOrNaN())
                return 0;

            var timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;

            var current_power_supply_per_second = power / timeWarpFixedDeltaTime;
            var maximum_power_supply_per_second = maxpower / timeWarpFixedDeltaTime;

            currentPowerSupply += current_power_supply_per_second;
            stable_supply += maximum_power_supply_per_second;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += current_power_supply_per_second;
            powerGenerated.currentProvided += current_power_supply_per_second;
            powerGenerated.maximumSupply += maximum_power_supply_per_second;

            return power;
        }

        public double powerSupplyPerSecondWithMaxAndEfficiency(IResourceSupplier pm, double power, double maxpower, double efficiencyRatio)
        {
            if (power.IsInfinityOrNaN())
                return 0;
            if (maxpower.IsInfinityOrNaN())
                return 0;
            if (efficiencyRatio.IsInfinityOrNaN())
                return 0;

            currentPowerSupply += power;
            stable_supply += maxpower;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += power;
            powerGenerated.maximumSupply += maxpower;
            powerGenerated.efficiencyRatio = efficiencyRatio;

            return power;
        }

        public double powerSupplyPerSecondWithMax(IResourceSupplier pm, double power, double maxpower)
        {
            if (power.IsInfinityOrNaN())
                return 0;
            if (maxpower.IsInfinityOrNaN())
                return 0;

            currentPowerSupply += power;
            stable_supply += maxpower;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += power;
            powerGenerated.maximumSupply += maxpower;

            return power;
        }

        public double managedPowerSupplyPerSecond(IResourceSupplier pm, double power)
        {
            return managedPowerSupplyPerSecondWithMinimumRatio(pm, power, 0);
        }

        public double getResourceAvailability()
        {
            double amount;
            double maxAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

            return amount;
        }

        public double getSpareResourceCapacity()
        {
            double amount;
            double maxAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

            return maxAmount - amount;
        }

        public double getTotalResourceCapacity()
        {
            double amount;
            double maxAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

            return maxAmount;
        }

        public double getNeededPowerSupplyPerSecondWithMinimumRatio(double power, double ratio_min)
        {
            var minimum_power_per_second = power * ratio_min;
            var needed_power_per_second = Math.Min(power, (Math.Max(GetCurrentUnfilledResourceDemand(), minimum_power_per_second)));

            return needed_power_per_second;
        }

        public PowerGenerated managedRequestedPowerSupplyPerSecondMinimumRatio(IResourceSupplier pm, double available_power, double maximum_power, double ratio_min)
        {
            if (available_power.IsInfinityOrNaN())
                return new PowerGenerated();
            if (maximum_power.IsInfinityOrNaN())
                return new PowerGenerated();
            if (ratio_min.IsInfinityOrNaN())
                return new PowerGenerated();

            var minimum_power_per_second = maximum_power * ratio_min;

            var provided_demand_power_per_second = Math.Min(maximum_power, Math.Max(minimum_power_per_second, Math.Max(available_power, GetCurrentUnfilledResourceDemand())));
            var managed_supply_per_second = Math.Min(maximum_power, Math.Max(minimum_power_per_second, Math.Min(available_power, GetRequiredResourceDemand())));

            currentPowerSupply += managed_supply_per_second;
            stable_supply += maximum_power;

            var addedPower = new PowerGenerated
            {
                currentSupply = managed_supply_per_second,
                currentProvided = provided_demand_power_per_second,
                maximumSupply = maximum_power,
                minimumSupply = minimum_power_per_second
            };

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
                power_produced.Add(pm, addedPower);
            else
            {
                powerGenerated.currentSupply += addedPower.currentSupply;
                powerGenerated.currentProvided += addedPower.currentProvided;
                powerGenerated.maximumSupply += addedPower.maximumSupply;
                powerGenerated.minimumSupply += addedPower.minimumSupply;
            }

            return addedPower;
        }

        public double managedPowerSupplyPerSecondWithMinimumRatio(IResourceSupplier pm, double maximum_power, double ratio_min)
        {
            if (maximum_power.IsInfinityOrNaN())
                return 0;
            if (ratio_min.IsInfinityOrNaN())
                return 0;

            var minimum_power_per_second = maximum_power * ratio_min;

            var provided_demand_power_per_second = Math.Min(maximum_power, Math.Max(GetCurrentUnfilledResourceDemand(), minimum_power_per_second));
            var required_power_per_second = Math.Max(GetRequiredResourceDemand(), minimum_power_per_second);
            var managed_supply_per_second = Math.Min(maximum_power, required_power_per_second);

            currentPowerSupply += managed_supply_per_second;
            stable_supply += maximum_power;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }

            powerGenerated.currentSupply += managed_supply_per_second;
            powerGenerated.currentProvided += provided_demand_power_per_second;
            powerGenerated.maximumSupply += maximum_power;
            powerGenerated.minimumSupply += minimum_power_per_second;

            return provided_demand_power_per_second;
        }

        public double StableResourceSupply { get { return stored_stable_supply; } }
        public double ResourceSupply { get { return stored_supply; } }
        public double ResourceDemand { get { return stored_resource_demand; } }
        public double CurrentResourceDemand { get { return current_resource_demand; } }
        public double CurrentHighPriorityResourceDemand { get { return stored_current_hp_demand + stored_current_charge_demand; } }
        public double PowerSupply { get { return currentPowerSupply; } }
        public double ResourceBarRatioBegin { get { return resource_bar_ratio_begin; } }

        public double TemperatureRatio { get { return temperature_ratio; } }
        public double SqrtResourceBarRatioBegin { get { return sqrt_resource_bar_ratio_begin; } }
        public double RadiatorEfficiency { get { return radiator_efficiency; } }
        public double ResourceBarRatioEnd { get { return resource_bar_ratio_end; } }

        public Vessel Vessel { get { return my_vessel; } }
        public PartModule PartModule { get { return my_partmodule; } }

        public double getOverproduction()
        {
            return stored_supply - stored_resource_demand;
        }

        public double getDemandStableSupply()
        {
            return stored_stable_supply > 0 ? stored_resource_demand / stored_stable_supply : 1;
        }

        public double GetCurrentUnfilledResourceDemand()
        {
            return current_resource_demand - currentPowerSupply;
        }

        public double GetRequiredResourceDemand()
        {
            return GetCurrentUnfilledResourceDemand() + getSpareResourceCapacity();
        }

        public void UpdatePartModule(PartModule pm)
        {
            if (pm != null)
            {
                my_vessel = pm.vessel;
                my_part = pm.part;
                my_partmodule = pm;
            }
            else
                my_partmodule = null;
        }

        public bool IsUpdatedAtLeastOnce { get; set; }

        public long Counter { get; private set; }

        public void update(long counter)
        {
            var timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;

            IsUpdatedAtLeastOnce = true;
            Counter = counter;

            stored_supply = currentPowerSupply;
            stored_stable_supply = stable_supply;
            stored_resource_demand = current_resource_demand;
            stored_current_demand = current_resource_demand;
            stored_current_hp_demand = high_priority_resource_demand;
            stored_current_charge_demand = charge_resource_demand;
            stored_total_power_supplied = total_power_distributed;

            current_requested_amount = 0;
            current_consumed_amount = 0;

            current_resource_demand = 0;
            high_priority_resource_demand = 0;
            charge_resource_demand = 0;
            total_power_distributed = 0;

            double availableResourceAmount;
            double maxResouceAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out availableResourceAmount, out maxResouceAmount);

            if (double.IsNaN(availableResourceAmount) || double.IsInfinity(availableResourceAmount))
                availableResourceAmount = 0;

            if (maxResouceAmount > 0 && !double.IsNaN(maxResouceAmount) && !double.IsNaN(availableResourceAmount))
                resource_bar_ratio_end = availableResourceAmount / maxResouceAmount;
            else
                resource_bar_ratio_end = 0.0001;

            var missingResourceAmount = (maxResouceAmount - availableResourceAmount);
            currentPowerSupply += availableResourceAmount;

            var high_priority_demand_supply_ratio = high_priority_resource_demand > 0
                ? Math.Min((currentPowerSupply - stored_current_charge_demand) / stored_current_hp_demand, 1)
                : 1;

            var demand_supply_ratio = stored_current_demand > 0
                ? Math.Min((currentPowerSupply - stored_current_charge_demand - stored_current_hp_demand) / stored_current_demand, 1)
                : 1;

            if (resourceDefinition.id == megajouleResourceDefinition.id && stored_stable_supply <= 0)
            {
                double amount;
                double maxAmount;

                my_part.GetConnectedResourceTotals(electricResourceDefinition.id, out amount, out maxAmount);

                double current_stock_electric_charge_shortage = maxAmount - amount;

                double stock_electric_charge_needed =  current_stock_electric_charge_shortage - previous_stock_electric_charge_shortage;
                if (stock_electric_charge_needed > 0)
                {
                    var deltaResourceDemand = stock_electric_charge_needed / 1000 / timeWarpFixedDeltaTime;
                    current_resource_demand += deltaResourceDemand;
                    charge_resource_demand += deltaResourceDemand;
                }

                previous_stock_electric_charge_shortage = current_stock_electric_charge_shortage;
            }

            //First supply minimal Amount of Stock ElectricCharge resource to keep probe core and life support functioning
            if (resourceDefinition.id == megajouleResourceDefinition.id && stored_stable_supply > 0)
            {
                double amount;
                double maxAmount;

                my_part.GetConnectedResourceTotals(electricResourceDefinition.id, out amount, out maxAmount);

                if (!double.IsNaN(amount))
                {
                    var stock_electric_charge_needed = Math.Min(timeWarpFixedDeltaTime, maxAmount - amount);
                    if (stock_electric_charge_needed > 0)
                    {
                        var deltaResourceDemand = stock_electric_charge_needed / 1000 / timeWarpFixedDeltaTime;
                        current_resource_demand += deltaResourceDemand;
                        charge_resource_demand += deltaResourceDemand;
                    }

                    var power_supplied = Math.Min(currentPowerSupply * 1000 * timeWarpFixedDeltaTime, stock_electric_charge_needed);
                    if (power_supplied > 0)
                    {
                        var fixed_provided_electric_charge_in_MW = power_supplied.IsInfinityOrNaN() ? 0 : my_part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, (double)-(power_supplied + 0.01)) / 1000;
                        var provided_electric_charge_per_second = fixed_provided_electric_charge_in_MW / timeWarpFixedDeltaTime;
                        total_power_distributed += -provided_electric_charge_per_second;
                        currentPowerSupply += provided_electric_charge_per_second;
                    }
                }
            }

            var sumPowerProduced = power_produced.Sum(m => m.Value.currentSupply);

            var supplyEfficiencyRatio = power_produced.Count > 0 && sumPowerProduced > 0 ? power_produced.Sum(m => m.Value.efficiencyRatio * (m.Value.currentSupply / sumPowerProduced)) : 0;

            power_supply_list_archive = power_produced.OrderByDescending(m => m.Value.maximumSupply).ToList();

            // store current supply and update average
            power_supply_list_archive.ForEach(m =>
            {
                Queue<double> queue;

                if (!power_produced_history.TryGetValue(m.Key, out queue))
                {
                    queue = new Queue<double>(10);
                    power_produced_history.Add(m.Key, queue);
                }

                if (queue.Count > 10)
                    queue.Dequeue();
                queue.Enqueue(m.Value.currentSupply);

                m.Value.averageSupply = queue.Average();
            });

            List<KeyValuePair<IResourceSuppliable, PowerDistribution>> power_draw_items = power_consumption.OrderBy(m => m.Value.Power_requested).ToList();

            power_draw_list_archive = power_draw_items.ToList();
            power_draw_list_archive.Reverse();

            // check priority 0 parts like fusion reactors that need to be fed before any other parts can consume large amounts of power
            foreach (KeyValuePair<IResourceSuppliable, PowerDistribution> power_kvp in power_draw_items)
            {
                IResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 0)
                {
                    var power = power_kvp.Value.Power_requested;

                    if (!double.IsNaN(power) && !double.IsInfinity(power))
                    {
                        current_resource_demand += power;
                        high_priority_resource_demand += power;
                    }

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * high_priority_demand_supply_ratio;

                    var power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0);

                    if (!double.IsNaN(power_supplied) && !double.IsInfinity(power_supplied))
                    {
                        currentPowerSupply -= power_supplied;
                        total_power_distributed += power_supplied;
                    }

                    //notify of supply
                    resourceSuppliable.receiveFNResource(power_supplied, this.resource_name);
                }
            }

            //Prioritise supplying stock ElectricCharge for High power
            if (resourceDefinition.id == megajouleResourceDefinition.id && stored_stable_supply > 0)
            {
                double amount;
                double maxAmount;

                my_part.GetConnectedResourceTotals(electricResourceDefinition.id, out amount, out maxAmount);

                if (!amount.IsInfinityOrNaN())
                {
                    var stock_electric_charge_needed = maxAmount - amount;

                    var power_supplied = Math.Min(currentPowerSupply * 1000 * timeWarpFixedDeltaTime, stock_electric_charge_needed);
                    if (stock_electric_charge_needed > 0)
                    {
                        var deltaResourceDemand = stock_electric_charge_needed / 1000 / timeWarpFixedDeltaTime;
                        current_resource_demand += deltaResourceDemand;
                        charge_resource_demand += deltaResourceDemand;
                    }


                    if (power_supplied > 0)
                    {
                        var fixed_provided_electric_charge_in_MW = power_supplied.IsInfinityOrNaN() ? 0 :  my_part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, -power_supplied) / 1000;

                        var provided_electric_charge_per_second = fixed_provided_electric_charge_in_MW / timeWarpFixedDeltaTime;
                        total_power_distributed += -provided_electric_charge_per_second;
                        currentPowerSupply += provided_electric_charge_per_second;
                    }
                }
            }

            // check priority 1 parts
            foreach (KeyValuePair<IResourceSuppliable, PowerDistribution> power_kvp in power_draw_items)
            {
                IResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 1)
                {
                    var power = power_kvp.Value.Power_requested;

                    // efficiency throtling
                    if (supplyEfficiencyRatio < 0.14 && resourceDefinition.id == megajouleResourceDefinition.id)
                        power *= Math.Max(0, supplyEfficiencyRatio - 0.02) / 0.12;

                    if (!double.IsNaN(power) && !double.IsInfinity(power))
                    {
                        current_resource_demand += power;
                        high_priority_resource_demand += power;
                    }

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * high_priority_demand_supply_ratio;

                    var power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0);

                    if (!double.IsNaN(power_supplied) && !double.IsInfinity(power_supplied))
                    {
                        currentPowerSupply -= power_supplied;
                        total_power_distributed += power_supplied;

                        //notify of supply
                        resourceSuppliable.receiveFNResource((double)power_supplied, this.resource_name);
                    }
                }
            }

            // check priority 2 parts
            foreach (KeyValuePair<IResourceSuppliable, PowerDistribution> power_kvp in power_draw_items)
            {
                IResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 2)
                {
                    var power = power_kvp.Value.Power_requested;

                    // efficiency throtling
                    if (supplyEfficiencyRatio < 0.16 && resourceDefinition.id == megajouleResourceDefinition.id)
                        power *= Math.Max(0, supplyEfficiencyRatio - 0.02) / 0.14;

                    if (!double.IsNaN(power) && !double.IsInfinity(power))
                        current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * demand_supply_ratio;

                    var power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0);

                    if (!double.IsNaN(power_supplied) && !double.IsInfinity(power_supplied))
                    {
                        currentPowerSupply -= power_supplied;
                        total_power_distributed += power_supplied;

                        //notify of supply
                        resourceSuppliable.receiveFNResource((double)power_supplied, this.resource_name);
                    }
                }
            }

            // check priority 3 parts like engines and nuclear reactors
            foreach (KeyValuePair<IResourceSuppliable, PowerDistribution> power_kvp in power_draw_items)
            {
                IResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 3)
                {
                    var power = power_kvp.Value.Power_requested;

                    // efficiency throtling
                    if (resourceDefinition.id == megajouleResourceDefinition.id && supplyEfficiencyRatio > 0 && supplyEfficiencyRatio < 0.18)
                        power *= Math.Max(0, supplyEfficiencyRatio - 0.02) / 0.16;

                    if (!double.IsNaN(power) && !double.IsInfinity(power))
                        current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * demand_supply_ratio;

                    var power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0);

                    if (!double.IsNaN(power_supplied) && !double.IsInfinity(power_supplied))
                    {
                        currentPowerSupply -= power_supplied;
                        total_power_distributed += power_supplied;

                        //notify of supply
                        resourceSuppliable.receiveFNResource((double)power_supplied, this.resource_name);
                    }
                }
            }

            // check priority 4 parts like antimatter reactors, engines and transmitters
            foreach (KeyValuePair<IResourceSuppliable, PowerDistribution> power_kvp in power_draw_items)
            {
                IResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 4)
                {
                    var power = power_kvp.Value.Power_requested;

                    // efficiency throtling
                    if (supplyEfficiencyRatio < 0.2 && resourceDefinition.id == megajouleResourceDefinition.id)
                        power *= Math.Max(0, supplyEfficiencyRatio - 0.02) / 0.18;

                    current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * demand_supply_ratio;

                    var power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0);

                    if (!double.IsNaN(power_supplied) && !double.IsInfinity(power_supplied))
                    {
                        currentPowerSupply -= power_supplied;
                        total_power_distributed += power_supplied;

                        //notify of supply
                        resourceSuppliable.receiveFNResource((double)power_supplied, this.resource_name);
                    }
                }
            }

            // check priority 5 parts and higher
            foreach (KeyValuePair<IResourceSuppliable, PowerDistribution> power_kvp in power_draw_items)
            {
                IResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() >= 5)
                {
                    var power = power_kvp.Value.Power_requested;

                    // efficiency throtling
                    if (supplyEfficiencyRatio < 0.22 && resourceDefinition.id == megajouleResourceDefinition.id)
                        power *= Math.Max(0, supplyEfficiencyRatio - 0.02) / 0.2;

                    current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * demand_supply_ratio;

                    var power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0);

                    if (!double.IsNaN(power_supplied) && !double.IsInfinity(power_supplied))
                    {
                        currentPowerSupply -= power_supplied;
                        total_power_distributed += power_supplied;

                        //notify of supply
                        resourceSuppliable.receiveFNResource((double)power_supplied, this.resource_name);
                    }
                }
            }

            // substract avaialble resource amount to get delta resource change
            currentPowerSupply -= Math.Max(availableResourceAmount, 0);
            internl_power_extract_fixed = -currentPowerSupply * timeWarpFixedDeltaTime;

            if (resourceDefinition.id == wasteheatResourceDefinition.id)
            {
                // passive dissip of waste heat - a little bit of this
                var vessel_mass = my_vessel.totalMass;
                var passive_dissip = passive_temp_p4 * GameConstants.stefan_const * vessel_mass * 2;
                internl_power_extract_fixed += passive_dissip * timeWarpFixedDeltaTime;

                if (my_vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(my_vessel.mainBody))
                {
                    // passive convection - a lot of this
                    double pressure = FlightGlobals.getStaticPressure(my_vessel.transform.position) * 0.01;
                    double conv_power_dissip = pressure * 40 * vessel_mass * GameConstants.rad_const_h / 1e6 * TimeWarp.fixedDeltaTime;
                    internl_power_extract_fixed += conv_power_dissip;
                }
            }

            if (internl_power_extract_fixed > 0)
                internl_power_extract_fixed = Math.Min(internl_power_extract_fixed, availableResourceAmount);
            else
                internl_power_extract_fixed = Math.Max(internl_power_extract_fixed, -missingResourceAmount);

            if (!internl_power_extract_fixed.IsInfinityOrNaN())
            {
                my_part.RequestResource(resourceDefinition.id, (double)internl_power_extract_fixed);
            }

            my_part.GetConnectedResourceTotals(resourceDefinition.id, out availableResourceAmount, out maxResouceAmount);

            if (!maxResouceAmount.IsInfinityOrNaNorZero() && !availableResourceAmount.IsInfinityOrNaN())
                resource_bar_ratio_begin =  Math.Max(0, Math.Min(1,  availableResourceAmount / maxResouceAmount));
            else
                resource_bar_ratio_begin = resourceDefinition.id == wasteheatResourceDefinition.id ? 0.999 : 0;

            if (resource_name == ResourceManager.FNRESOURCE_WASTEHEAT)
            {
                sqrt_resource_bar_ratio_begin = Math.Sqrt(resource_bar_ratio_begin);

                temperature_ratio = Math.Pow((double)resource_bar_ratio_begin, 0.75);

                radiator_efficiency = 1 - Math.Pow(1 - resource_bar_ratio_begin, 400);
            }

            //calculate total input and output
            //var total_current_supplied = power_produced.Sum(m => m.Value.currentSupply);
            //var total_current_provided = power_produced.Sum(m => m.Value.currentProvided);
            //var total_power_consumed = power_consumption.Sum(m => m.Value.Power_consume);
            //var total_power_min_supplied = power_produced.Sum(m => m.Value.minimumSupply);

            ////generate wasteheat from used thermal power + thermal store
            //if (!CheatOptions.IgnoreMaxTemperature && total_current_produced > 0 && 
            //    (resourceDefinition.id == thermalpowerResourceDefinition.id || resourceDefinition.id == chargedpowerResourceDefinition.id))
            //{
            //    var min_supplied_fixed = TimeWarp.fixedDeltaTime * total_power_min_supplied;
            //    var used_or_stored_power_fixed = TimeWarp.fixedDeltaTime * Math.Min(total_power_consumed, total_current_produced) + Math.Max(-actual_stored_power, 0);
            //    var wasteheat_produced_fixed = Math.Max(min_supplied_fixed, used_or_stored_power_fixed);

            //    var effective_wasteheat_ratio = Math.Max(wasteheat_produced_fixed / (total_current_produced * TimeWarp.fixedDeltaTime), 1);

            //    ORSResourceManager manager = ORSResourceOvermanager.getResourceOvermanagerForResource(ResourceManager.FNRESOURCE_WASTEHEAT).getManagerForVessel(my_vessel);

            //    foreach (var supplier_key_value in power_produced)
            //    {
            //        if (supplier_key_value.Value.currentSupply > 0)
            //        {
            //            manager.powerSupplyPerSecondWithMax(supplier_key_value.Key, supplier_key_value.Value.currentSupply * effective_wasteheat_ratio, supplier_key_value.Value.maximumSupply * effective_wasteheat_ratio);
            //        }
            //    }
            //}

            currentPowerSupply = 0;
            stable_supply = 0;

            power_produced.Clear();
            power_consumption.Clear();
        }

        protected double TimeWarpFixedDeltaTime
        {
            get { return (double)(decimal)TimeWarp.fixedDeltaTime; }
        }

        public void showWindow()
        {
            render_window = true;
        }

        public void hideWindow()
        {
            render_window = false;
        }

        public void OnGUI()
        {
            if (my_vessel == FlightGlobals.ActiveVessel && render_window)
            {
                string title = resource_name + " Management Display";
                windowPosition = GUILayout.Window(windowID, windowPosition, doWindow, title);
            }
        }

        protected string getPowerFormatString(double power)
        {
            var absPower = Math.Abs(power);

            if (absPower >= 1000000)
            {
                if (absPower > 100000000)
                    return (power / 1000000).ToString("0") + " TW";
                else if (absPower > 10000000)
                    return (power / 1000000).ToString("0.0") + " TW";
                else
                    return (power / 1000000).ToString("0.00") + " TW";
            }
            else if (absPower >= 1000)
            {
                if (absPower > 100000)
                    return (power / 1000).ToString("0") + " GW";
                else if (absPower > 10000)
                    return (power / 1000).ToString("0.0") + " GW";
                else
                    return (power / 1000).ToString("0.00") + " GW";
            }
            else if (power >= 1)
            {
                if (absPower > 100)
                    return power.ToString("0") + " MW";
                else if (absPower > 10)
                    return power.ToString("0.0") + " MW";
                else
                    return power.ToString("0.00") + " MW";
            }
            else if (absPower >= 0.001)
            {
                if (absPower >= 0.1)
                    return (power * 1000).ToString("0") + " KW";
                else if (absPower >= 0.01)
                    return (power * 1000).ToString("0.0") + " KW";
                else
                    return (power * 1000).ToString("0.00") + " KW";
            }
            else
                return (power * 1000000).ToString("0") + " W";
        }

        protected void doWindow(int windowID)
        {
            if (left_bold_label == null)
            {
                left_bold_label = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    font = PluginHelper.MainFont
                };
            }

            if (right_bold_label == null)
            {
                right_bold_label = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (green_label == null)
            {
                green_label = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = resource_name == ResourceManager.FNRESOURCE_WASTEHEAT ? Color.red : Color.green },
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (red_label == null)
            {
                red_label = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = resource_name == ResourceManager.FNRESOURCE_WASTEHEAT ? Color.green : Color.red },
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (left_aligned_label == null)
            {
                left_aligned_label = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    font = PluginHelper.MainFont
                };
            }

            if (right_aligned_label == null)
            {
                right_aligned_label = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (render_window && GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                render_window = false;

            GUILayout.Space(2);
            GUILayout.BeginVertical();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Resource Manager Id", left_bold_label, GUILayout.ExpandWidth(true));
            //GUILayout.Label(Id.ToString(), right_aligned_label, GUILayout.ExpandWidth(true), GUILayout.MinWidth(labelWidth));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Over Manager Id", left_bold_label, GUILayout.ExpandWidth(true));
            //GUILayout.Label(OverManagerId.ToString(), right_aligned_label, GUILayout.ExpandWidth(true), GUILayout.MinWidth(labelWidth));
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Theoretical Supply", left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString((double)stored_stable_supply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Supply", left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString((double)stored_supply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            if (resource_name == ResourceManager.FNRESOURCE_MEGAJOULES)
            {
                var stored_supply_percentage = stored_supply != 0 ? stored_total_power_supplied / stored_supply : 0;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Distribution", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label(stored_supply_percentage.ToString("P2"), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power Demand", left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString((double)stored_resource_demand), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            double new_power_supply = (double)getOverproduction();
            double net_utilisation_supply = (double)getDemandStableSupply();

            GUIStyle net_poer_style = new_power_supply < -0.001 ? red_label : green_label;
            GUIStyle utilisation_style = net_utilisation_supply > 1.001 ? red_label : green_label;

            GUILayout.BeginHorizontal();
            var new_power_label = (resource_name == ResourceManager.FNRESOURCE_WASTEHEAT) ? "Net Change" : "Net Power";
            GUILayout.Label(new_power_label, left_bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(new_power_supply), net_poer_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
            GUILayout.EndHorizontal();

            if (!double.IsNaN(net_utilisation_supply) && !double.IsInfinity(net_utilisation_supply))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Utilisation", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label((net_utilisation_supply).ToString("P2"), utilisation_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(overviewWidth));
                GUILayout.EndHorizontal();
            }

            if (power_supply_list_archive != null)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Producer Component", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label("Supply", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.Label("Max", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.EndHorizontal();

                var groupedPowerSupply = power_supply_list_archive.GroupBy(m => m.Key.getResourceManagerDisplayName());

                var sumarizedList = new List<PowerProduction>();

                foreach (var group in groupedPowerSupply)
                {
                    var sumOfCurrentAverageSupply = group.Sum(m => m.Value.averageSupply);
                    var sumOfMaximumSupply = group.Sum(m => m.Value.maximumSupply);

                    // skip anything with less then 0.00 KW
                    if (sumOfCurrentAverageSupply < 0.0000005 && sumOfMaximumSupply < 0.0000005)
                        continue;

                    string name = group.Key;
                    var count = group.Count();
                    if (count > 1)
                        name = count + " " + name;

                    sumarizedList.Add(new PowerProduction() { component = name, averageSupply = (double)sumOfCurrentAverageSupply, maximumSupply = (double)sumOfMaximumSupply });
                }

                foreach (var production in sumarizedList.OrderByDescending(m => m.averageSupply))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(production.component, left_aligned_label, GUILayout.ExpandWidth(true));
                    GUILayout.Label(getPowerFormatString(production.averageSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                    GUILayout.Label(getPowerFormatString(production.maximumSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                    GUILayout.EndHorizontal();
                }
            }

            if (power_draw_list_archive != null)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Consumer Component", left_bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label("Demand", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.Label("Rank", right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(priorityWidth));
                GUILayout.EndHorizontal();

                var sumarizedList = new List<PowerConsumption>();

                var groupedPowerDraws = power_draw_list_archive.GroupBy(m => m.Key.getResourceManagerDisplayName());

                foreach (var group in groupedPowerDraws)
                {
                    var sumOfPowerDraw = group.Sum(m => m.Value.Power_requested);
                    var sumOfPowerConsume = group.Sum(m => m.Value.Power_consumed);
                    var sumOfConsumePercentage = sumOfPowerDraw > 0 ? sumOfPowerConsume / sumOfPowerDraw * 100 : 0;

                    var name = group.Key;
                    var count = group.Count();
                    if (count > 1)
                        name = count + " " + name;
                    if (resource_name == ResourceManager.FNRESOURCE_MEGAJOULES && sumOfConsumePercentage < 99.5)
                        name = name + " " + sumOfConsumePercentage.ToString("0") + "%";

                    sumarizedList.Add(new PowerConsumption() { component = name, sum = (double)sumOfPowerDraw, priority = group.First().Key.getPowerPriority() });
                }

                foreach (var consumption in sumarizedList.OrderByDescending(m => m.sum))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(consumption.component, left_aligned_label, GUILayout.ExpandWidth(true));
                    GUILayout.Label(getPowerFormatString(consumption.sum), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                    GUILayout.Label(consumption.priority.ToString(), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(priorityWidth));
                    GUILayout.EndHorizontal();
                }
            }

            if (resource_name == ResourceManager.FNRESOURCE_MEGAJOULES)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("DC Electrical System", left_aligned_label, GUILayout.ExpandWidth(true));
                GUILayout.Label(getPowerFormatString((double)stored_current_charge_demand), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(valueWidth));
                GUILayout.Label("0", right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(priorityWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

    }
}