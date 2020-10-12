using System;
using System.Collections.Generic;

namespace FNPlugin
{
    public class PowerDistribution
    {
        public double PowerConsumed { get; set; }

        public double PowerCurrentRequest { get; set; }

        public double PowerMaximumRequest { get; set; }

        public PowerDistribution()
        {
            PowerConsumed = 0.0;
            PowerCurrentRequest = 0.0;
            PowerMaximumRequest = 0.0;
        }
    }

    public class PowerGenerated
    {
        public double AverageSupply { get; set; }

        public double CurrentProvided { get; set; }

        public double CurrentSupply { get; set; }

        public double EfficiencyRatio { get; set; }

        public Queue<double> History { get; set; }

        public double MaximumSupply { get; set; }

        public double MinimumSupply { get; set; }

        public PowerGenerated()
        {
            AverageSupply = 0.0;
            CurrentProvided = 0.0;
            CurrentSupply = 0.0;
            EfficiencyRatio = 1;
            History = null;
            MaximumSupply = 0.0;
            MinimumSupply = 0.0;
        }
    }

    internal sealed class PowerDistributionPair : IComparable<PowerDistributionPair>
    {
        public IResourceSuppliable Key { get; }

        public PowerDistribution Value { get; }

        public PowerDistributionPair(IResourceSuppliable key, PowerDistribution value)
        {
            Key = key;
            Value = value;
        }

        public int CompareTo(PowerDistributionPair other)
        {
            // Larger suppliers go first
            int priority = Key.getPowerPriority().CompareTo(other.Key.getPowerPriority());
            return (priority == 0) ? Value.PowerMaximumRequest.CompareTo(other.Value.
                PowerMaximumRequest) : priority;
        }

        public override bool Equals(object obj)
        {
            return obj is PowerDistributionPair other && CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    internal sealed class PowerGeneratedPair : IComparable<PowerGeneratedPair>
    {
        public IResourceSupplier Key { get; }

        public PowerGenerated Value { get; set; }

        public PowerGeneratedPair(IResourceSupplier key, PowerGenerated value)
        {
            Key = key;
            Value = value;
        }

        public int CompareTo(PowerGeneratedPair other)
        {
            // Larger suppliers go first
            return -Value.MaximumSupply.CompareTo(other.Value.MaximumSupply);
        }

        public override bool Equals(object obj)
        {
            return obj is PowerGeneratedPair other && CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    internal sealed class PowerProduction : IComparable<PowerProduction>
    {
        public double AverageSupply { get; }

        public string Component { get; }

        public double MaximumSupply { get; }

        public PowerProduction(string name, double average, double max)
        {
            Component = name;
            AverageSupply = average;
            MaximumSupply = max;
        }

        public int CompareTo(PowerProduction other)
        {
            // Largest first
            return -AverageSupply.CompareTo(other.AverageSupply);
        }
    }

    internal sealed class PowerConsumption : IComparable<PowerConsumption>
    {
        public string Component { get; }

        public double Sum { get; }

        public int Priority { get; }

        public PowerConsumption(string name, int priority, double sum)
        {
            Component = name;
            Priority = priority;
            Sum = sum;
        }

        public int CompareTo(PowerConsumption other)
        {
            return -Sum.CompareTo(other.Sum);
        }
    }

    public sealed class PowerStats
    {
        public double Demand { get; set; }

        public double DemandHighPriority { get; set; }

        public double StableSupply { get; set; }

        public double Supply { get; set; }

        public double TotalSupplied { get; set; }

        public PowerStats()
        {
            Reset();
        }

        public void CopyTo(PowerStats other)
        {
            // Avoids allocating on the heap, versus reassigning and instantiating
            other.Demand = Demand;
            other.DemandHighPriority = DemandHighPriority;
            other.StableSupply = StableSupply;
            other.Supply = Supply;
            other.TotalSupplied = TotalSupplied;
        }

        public void Reset()
        {
            Demand = 0.0;
            DemandHighPriority = 0.0;
            StableSupply = 0.0;
            Supply = 0.0;
            TotalSupplied = 0.0;
        }
    }
}
