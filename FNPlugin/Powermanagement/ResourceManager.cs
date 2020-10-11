using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;
using KSP.Localization;
using FNPlugin.Powermanagement;

using PowerHistory = System.Collections.Generic.Queue<double>;

namespace FNPlugin
{
    public static class ResourceManagerFactory
    {
        // Create the appropriate instance
        public static ResourceManager Create(Guid id, PartModule pm, string resourceName)
        {
            ResourceManager result;
            switch (resourceName)
            {
            case ResourceManager.FNRESOURCE_MEGAJOULES:
                result = new MegajoulesResourceManager(id, pm);
                break;
            case ResourceManager.FNRESOURCE_WASTEHEAT:
                result = new WasteHeatResourceManager(id, pm);
                break;
            case ResourceManager.FNRESOURCE_CHARGED_PARTICLES:
                result = new CPResourceManager(id, pm);
                break;
            case ResourceManager.FNRESOURCE_THERMALPOWER:
                result = new TPResourceManager(id, pm);
                break;
            default:
                result = new DefaultResourceManager(id, pm, resourceName);
                break;
            }
            return result;
        }
    }

    public abstract class ResourceManager
    {
        public const string FNRESOURCE_MEGAJOULES = "Megajoules";
        public const string FNRESOURCE_CHARGED_PARTICLES = "ChargedParticles";
        public const string FNRESOURCE_THERMALPOWER = "ThermalPower";
        public const string FNRESOURCE_WASTEHEAT = "WasteHeat";
        public const string STOCK_RESOURCE_ELECTRICCHARGE = "ElectricCharge";

        public const int FNRESOURCE_FLOWTYPE_SMALLEST_FIRST = 0;
        public const int FNRESOURCE_FLOWTYPE_EVEN = 1;

        protected const int LABEL_WIDTH = 240;
        protected const int VALUE_WIDTH = 55;
        protected const int PRIORITY_WIDTH = 30;
        protected const int OVERVIEW_WIDTH = 65;

        protected const int MAX_PRIORITY = 6;
        private const int POWER_HISTORY_LEN = 10;

        public static string getPowerFormatString(double power)
        {
            var absPower = Math.Abs(power);
            string suffix;

            if (absPower >= 1e6)
            {
                suffix = " TW";
                absPower *= 1e-6;
                power *= 1e-6;
            }
            else if (absPower >= 1000)
            {
                suffix = " GW";
                absPower *= 1e-3;
                power *= 1e-3;
            }
            else if (absPower >= 1)
            {
                suffix = " MW";
            }
            else if (absPower >= 0.001)
            {
                suffix = " KW";
                absPower *= 1e3;
                power *= 1e3;
            }
            else
            {
                return (power * 1e6).ToString("0") + " W";
            }
            if (absPower > 100.0)
                return power.ToString("0") + suffix;
            else if (absPower > 10.0)
                return power.ToString("0.0") + suffix;
            else
                return power.ToString("0.00") + suffix;
        }

        protected readonly IDictionary<IResourceSuppliable, PowerDistribution> consumptionRequests;
        private readonly IDictionary<IResourceSupplier, PowerHistory> productionHistory;
        protected readonly IDictionary<IResourceSupplier, PowerGenerated> productionRequests;
        private readonly List<PowerDistributionPair> powerConsumers;
        private readonly List<PowerGeneratedPair> powerProducers;
        private readonly double[] currentDistributed;
        private readonly double[] stableDistributed;
        protected readonly PowerStats current, last;

        protected readonly string resourceName;
        protected readonly int flowType;
        protected Part part;
        protected readonly PartResourceDefinition resourceDefinition;

        private bool renderWindow;
        protected GUIStyle left_bold_label;
        protected GUIStyle right_bold_label;
        protected GUIStyle green_label;
        protected GUIStyle red_label;
        protected GUIStyle left_aligned_label;
        protected GUIStyle right_aligned_label;
        private readonly int windowID;

        protected virtual double AuxiliaryResourceDemand
        {
            get
            {
                return 0.0;
            }
        }

        public long Counter { get; private set; }

        public double CurrentConsumption { get; private set; }

        public double CurrentResourceSupply => current.Supply;

        public virtual double CurrentSurplus
        {
            get
            {
                return Math.Max(0.0, current.Supply - CurrentConsumption);
            }
        }

        public double CurrentUnfilledResourceDemand => current.Demand - current.Supply;

        public double DemandStableSupply
        {
            get
            {
                double ss = last.StableSupply;
                return ss > 0.0 ? last.Demand / ss : 1.0;
            }
        }

        public Guid Id { get; }

        public Guid OverManagerId { get; }

        public PartModule PartModule { get; private set; }

        public double RequiredResourceDemand => CurrentUnfilledResourceDemand + GetSpareResourceCapacity();

        public double ResourceDemand => last.Demand;

        public double ResourceDemandHighPriority => last.DemandHighPriority;

        public double ResourceFillFraction { get; private set; }

        public double ResourceNetChange => last.Supply - last.Demand;

        public double ResourceSupply => last.Supply;

        public double StableResourceSupply => last.StableSupply;

        public double TotalPowerSupplied => last.TotalSupplied;

        public Vessel Vessel { get; private set; }

        public Rect WindowPosition { get; protected set; }

        protected ResourceManager(Guid overmanagerId, PartModule pm, string resource_name, int flow_type)
        {
            OverManagerId = overmanagerId;
            Id = Guid.NewGuid();

            flowType = flow_type;
            resourceName = resource_name;
            Vessel = pm.vessel;
            part = pm.part;
            PartModule = pm;
            renderWindow = false;

            windowID = new System.Random(resource_name.GetHashCode()).Next(int.MinValue, int.MaxValue);
            WindowPosition = new Rect(0, 0, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);

            currentDistributed = new double[MAX_PRIORITY];
            stableDistributed = new double[MAX_PRIORITY];
            // Cannot use SortedDictionary as the priority for some items is dynamic
            consumptionRequests = new Dictionary<IResourceSuppliable, PowerDistribution>(64);
            // Must be kept separately as the producer list gets rebuilt every update
            productionHistory = new Dictionary<IResourceSupplier, PowerHistory>(64);
            productionRequests = new Dictionary<IResourceSupplier, PowerGenerated>(64);
            powerConsumers = new List<PowerDistributionPair>(64);
            powerProducers = new List<PowerGeneratedPair>(64);

            resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resource_name);
            last = new PowerStats();
            current = new PowerStats();
            ResourceFillFraction = 0.0;
        }

        protected virtual double AdjustSupplyComplete(double timeWarpDT, double powerToExtract)
        {
            return powerToExtract;
        }

        protected void DoWindow(int windowID)
        {
            double netChange = ResourceNetChange;
            double netUtilization = DemandStableSupply;

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
                    normal = { textColor = resourceName == FNRESOURCE_WASTEHEAT ? Color.red : Color.green },
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (red_label == null)
            {
                red_label = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = resourceName == FNRESOURCE_WASTEHEAT ? Color.green : Color.red },
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

            if (renderWindow && GUI.Button(new Rect(WindowPosition.width - 20, 2, 18, 18), "x"))
                renderWindow = false;

            GUILayout.Space(2);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_TheoreticalSupply"), left_bold_label, GUILayout.ExpandWidth(true));//"Theoretical Supply"
            GUILayout.Label(getPowerFormatString(StableResourceSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OVERVIEW_WIDTH));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_CurrentSupply"), left_bold_label, GUILayout.ExpandWidth(true));//"Current Supply"
            GUILayout.Label(getPowerFormatString(ResourceSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OVERVIEW_WIDTH));
            GUILayout.EndHorizontal();

            DoWindowInitial();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_PowerDemand"), left_bold_label, GUILayout.ExpandWidth(true));//"Power Demand"
            GUILayout.Label(getPowerFormatString(ResourceDemand), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OVERVIEW_WIDTH));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string new_power_label = (resourceName == FNRESOURCE_WASTEHEAT) ? Localizer.Format("#LOC_KSPIE_ResourceManager_NetChange") : Localizer.Format("#LOC_KSPIE_ResourceManager_NetPower");//"Net Change""Net Power"
            GUILayout.Label(new_power_label, left_bold_label, GUILayout.ExpandWidth(true));

            GUIStyle net_poer_style = netChange < -0.001 ? red_label : green_label;

            GUILayout.Label(getPowerFormatString(netChange), net_poer_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OVERVIEW_WIDTH));
            GUILayout.EndHorizontal();

            if (!netUtilization.IsInfinityOrNaN() && (resourceName != FNRESOURCE_MEGAJOULES || netUtilization < 2.0 || ResourceSupply >= last.Demand))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Utilisation"), left_bold_label, GUILayout.ExpandWidth(true));//"Utilisation"

                GUIStyle utilisation_style = netUtilization > 1.001 ? red_label : green_label;

                GUILayout.Label(netUtilization.ToString("P2"), utilisation_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OVERVIEW_WIDTH));
                GUILayout.EndHorizontal();
            }

            if (powerProducers != null)
            {
                var summaryList = new List<PowerProduction>(16);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_ProducerComponent"), left_bold_label, GUILayout.ExpandWidth(true));//"Producer Component"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Supply"), right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(VALUE_WIDTH));//"Supply"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Max"), right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(VALUE_WIDTH));//"Max"
                GUILayout.EndHorizontal();

                foreach (var group in powerProducers.GroupBy(m => m.Key.getResourceManagerDisplayName()))
                {
                    double sumAverage = 0.0, sumMaximum = 0.0;
                    foreach (var pair in group)
                    {
                        var produced = pair.Value;
                        sumAverage += produced.AverageSupply;
                        sumMaximum += produced.MaximumSupply;
                    }

                    // skip anything with less then 0.00 KW
                    if (sumAverage >= 5e-7 || sumMaximum >= 5e-7)
                    {
                        string name = group.Key;
                        int count = group.Count();
                        if (count > 1)
                            name = count + " * " + name;
                        summaryList.Add(new PowerProduction(name, sumAverage, sumMaximum));
                    }
                }
                summaryList.Sort();

                foreach (var production in summaryList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(production.Component, left_aligned_label, GUILayout.ExpandWidth(true));
                    GUILayout.Label(getPowerFormatString(production.AverageSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(VALUE_WIDTH));
                    GUILayout.Label(getPowerFormatString(production.MaximumSupply), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(VALUE_WIDTH));
                    GUILayout.EndHorizontal();
                }
            }

            if (powerConsumers != null)
            {
                var summaryList = new List<PowerConsumption>(16);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_ConsumerComponent"), left_bold_label, GUILayout.ExpandWidth(true));//"Consumer Component"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Demand"), right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(VALUE_WIDTH));//"Demand"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Rank"), right_bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(PRIORITY_WIDTH));//"Rank"
                GUILayout.EndHorizontal();

                foreach (var group in powerConsumers.GroupBy(m => m.Key.getResourceManagerDisplayName()))
                {
                    double sumRequest = 0.0, sumConsumed = 0.0;
                    int priority = 0;
                    foreach (var pair in group)
                    {
                        var consumed = pair.Value;
                        priority = pair.Key.getPowerPriority();
                        sumRequest += consumed.PowerMaximumRequest;
                        sumConsumed += consumed.PowerConsumed;
                    }
                    double utilization = sumRequest > 0.0 ? 100.0 * sumConsumed / sumRequest : 0.0;

                    string name = group.Key;
                    int count = group.Count();
                    if (count > 1)
                        name = count + " * " + name;
                    if (sumRequest > 0.0 && resourceName == FNRESOURCE_MEGAJOULES && utilization < 99.5)
                        name = name + " " + utilization.ToString("0") + "%";

                    summaryList.Add(new PowerConsumption(name, priority, sumRequest));
                }
                summaryList.Sort();

                foreach (var consumption in summaryList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(consumption.Component, left_aligned_label, GUILayout.ExpandWidth(true));
                    GUILayout.Label(getPowerFormatString(consumption.Sum), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(VALUE_WIDTH));
                    GUILayout.Label(consumption.Priority.ToString(), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(PRIORITY_WIDTH));
                    GUILayout.EndHorizontal();
                }
            }

            DoWindowFinal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        protected virtual void DoWindowInitial() { }

        protected virtual void DoWindowFinal() { }

        public double GetResourceAvailability()
        {
            part.GetConnectedResourceTotals(resourceDefinition.id, out double amount, out _);
            return amount;
        }

        public double GetSpareResourceCapacity()
        {
            part.GetConnectedResourceTotals(resourceDefinition.id, out double amount, out double maxAmount);
            return maxAmount - amount;
        }

        public double GetTotalResourceCapacity()
        {
            part.GetConnectedResourceTotals(resourceDefinition.id, out _, out double maxAmount);
            return maxAmount;
        }

        public double GetNeededPowerSupplyPerSecondWithMinimumRatio(double power, double ratio_min)
        {
            return Math.Min(power, Math.Max(CurrentUnfilledResourceDemand, power * ratio_min));
        }

        public double GetCurrentPriorityResourceSupply(int priority)
        {
            double total = AuxiliaryResourceDemand;
            int maxPriority = Math.Min(priority, MAX_PRIORITY);

            for (int i = 0; i < maxPriority; i++)
            {
                total += currentDistributed[i];
            }

            return total;
        }

        public double GetStablePriorityResourceSupply(int priority)
        {
            double total = AuxiliaryResourceDemand;
            int maxPriority = Math.Min(priority, MAX_PRIORITY);

            for (int i = 0; i < maxPriority; i++)
            {
                total += stableDistributed[i];
            }

            return total;
        }

        public void HideWindow()
        {
            renderWindow = false;
        }

        public double managedPowerSupplyPerSecond(IResourceSupplier pm, double power)
        {
            return managedPowerSupplyPerSecondWithMinimumRatio(pm, power, 0.0);
        }

        public double managedPowerSupplyPerSecondWithMinimumRatio(IResourceSupplier pm, double maximum_power, double ratio_min)
        {
            if (maximum_power.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN())
                return 0.0;
            double provided = Math.Min(maximum_power, Math.Max(maximum_power * ratio_min, CurrentUnfilledResourceDemand));
            managedRequestedPowerSupplyPerSecondMinimumRatio(pm, 0.0, maximum_power, ratio_min);
            return provided;
        }

        public PowerGenerated managedRequestedPowerSupplyPerSecondMinimumRatio(IResourceSupplier pm, double available_power, double maximum_power, double ratio_min)
        {
            if (available_power.IsInfinityOrNaN() || maximum_power.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN())
                return new PowerGenerated();

            double minPowerPerSecond = maximum_power * ratio_min;
            double providedPowerPerSecond = Math.Min(maximum_power, Math.Max(minPowerPerSecond, Math.Max(available_power, CurrentUnfilledResourceDemand)));
            double managedSupplyPerSecond = Math.Min(maximum_power, Math.Max(minPowerPerSecond, Math.Min(available_power, RequiredResourceDemand)));

            current.Supply += managedSupplyPerSecond;
            current.StableSupply += maximum_power;

            if (!productionRequests.TryGetValue(pm, out PowerGenerated powerGenerated))
            {
                productionRequests.Add(pm, powerGenerated = new PowerGenerated());
            }

            powerGenerated.CurrentSupply += managedSupplyPerSecond;
            // Demand was updated since the first access in this method!
            powerGenerated.CurrentProvided += Math.Min(providedPowerPerSecond, CurrentUnfilledResourceDemand);
            powerGenerated.MaximumSupply += maximum_power;
            powerGenerated.MinimumSupply += minPowerPerSecond;

            return powerGenerated;
        }

        public void OnGUI()
        {
            if (Vessel == FlightGlobals.ActiveVessel && renderWindow)
            {
                string title = resourceName + " " + Localizer.Format("#LOC_KSPIE_ResourceManager_title");//Management Display
                WindowPosition = GUILayout.Window(windowID, WindowPosition, DoWindow, title);
            }
        }

        public void powerDrawFixed(IResourceSuppliable pm, double power_draw, double power_consumption)
        {
            if (power_draw.IsInfinityOrNaN() || power_consumption.IsInfinityOrNaN())
                return;

            double timeWarpDT = TimeWarp.fixedDeltaTime;
            double powerPerSecond = power_draw / timeWarpDT;
            powerDrawPerSecond(pm, powerPerSecond, powerPerSecond, power_consumption / timeWarpDT);
        }

        public void powerDrawPerSecond(IResourceSuppliable pm, double power_requested, double power_consumed)
        {
            powerDrawPerSecond(pm, power_requested, power_requested, power_consumed);
        }

        public void powerDrawPerSecond(IResourceSuppliable pm, double power_current_requested, double power_maximum_requested, double power_consumed)
        {
            if (power_current_requested.IsInfinityOrNaN() || power_maximum_requested.IsInfinityOrNaN() || power_consumed.IsInfinityOrNaN())
                return;

            CurrentConsumption += power_consumed;

            if (!consumptionRequests.TryGetValue(pm, out PowerDistribution powerDistribution))
            {
                consumptionRequests.Add(pm, powerDistribution = new PowerDistribution());
            }

            powerDistribution.PowerCurrentRequest += power_current_requested;
            powerDistribution.PowerMaximumRequest += power_maximum_requested;
            powerDistribution.PowerConsumed += power_consumed;
        }

        public double powerSupplyFixed(IResourceSupplier pm, double power)
        {
            double powerFixed = power / TimeWarp.fixedDeltaTime;
            return powerSupplyPerSecondWithMaxAndEfficiency(pm, powerFixed, powerFixed, 1.0);
        }

        public double powerSupplyPerSecond(IResourceSupplier pm, double power)
        {
            return powerSupplyPerSecondWithMaxAndEfficiency(pm, power, power, 1.0);
        }

        public double powerSupplyFixedWithMax(IResourceSupplier pm, double power, double maxpower)
        {
            double timeWarpDT = TimeWarp.fixedDeltaTime;
            return powerSupplyPerSecondWithMaxAndEfficiency(pm, power / timeWarpDT, maxpower / timeWarpDT, 1.0);
        }

        public double powerSupplyPerSecondWithMaxAndEfficiency(IResourceSupplier pm, double power, double maxpower, double efficiencyRatio)
        {
            if (power.IsInfinityOrNaN() || maxpower.IsInfinityOrNaN() || efficiencyRatio.IsInfinityOrNaN())
                return 0.0;

            current.Supply += power;
            current.StableSupply += maxpower;

            if (!productionRequests.TryGetValue(pm, out PowerGenerated powerGenerated))
            {
                productionRequests.Add(pm, powerGenerated = new PowerGenerated());
            }
            powerGenerated.CurrentSupply += power;
            powerGenerated.CurrentProvided += power;
            powerGenerated.MaximumSupply += maxpower;
            powerGenerated.EfficiencyRatio = efficiencyRatio;

            return power;
        }

        public double powerSupplyPerSecondWithMax(IResourceSupplier pm, double power, double maxpower)
        {
            return powerSupplyPerSecondWithMaxAndEfficiency(pm, power, maxpower, 1.0);
        }

        public void ShowWindow()
        {
            renderWindow = true;
        }

        protected virtual void SupplyPriority(double timeWarpDT, int priority) { }

        public virtual void update(long counter)
        {
            double timeWarpDT = TimeWarp.fixedDeltaTime;
            double sumPowerProduced = 0.0, supplyEfficiencyRatio = 0.0;
            int prevPriority = -1;

            if (part == null)
            {
                Debug.LogError("[KSPI] ResourceManager has no attached part!");
                return;
            }

            Counter = counter;
            current.CopyTo(last);
            CurrentConsumption = 0.0;
            current.Demand = 0.0;
            current.DemandHighPriority = 0.0;
            current.TotalSupplied = 0.0;
            for (int i = 0; i < MAX_PRIORITY; i++)
            {
                currentDistributed[i] = 0.0;
                stableDistributed[i] = 0.0;
            }

            part.GetConnectedResourceTotals(resourceDefinition.id, out double availableAmount, out double maxAmount);
            if (availableAmount.IsInfinityOrNaN())
                availableAmount = 0.0;

            double hpSupplyDemandRatio = last.DemandHighPriority > 0.0 ?
                Math.Min((current.Supply - AuxiliaryResourceDemand) / last.DemandHighPriority, 1.0) : 1.0;
            double supplyDemandRatio = last.Demand > 0.0 ?
                Math.Min((current.Supply - AuxiliaryResourceDemand - last.DemandHighPriority) / last.Demand, 1.0) : 1.0;

            current.Supply += availableAmount;
            current.StableSupply += availableAmount;

            powerProducers.Clear();
            // Must be resorted on each update as the production can be dynamic
            foreach (var pair in productionRequests)
            {
                var key = pair.Key;
                var production = pair.Value;
                double currentSupply = production.CurrentSupply;

                powerProducers.Add(new PowerGeneratedPair(pair.Key, production));
                sumPowerProduced += currentSupply;
                supplyEfficiencyRatio += production.EfficiencyRatio * currentSupply;

                if (!productionHistory.TryGetValue(key, out PowerHistory queue))
                    productionHistory.Add(key, queue = new PowerHistory(POWER_HISTORY_LEN));
                if (queue.Count > POWER_HISTORY_LEN)
                    queue.Dequeue();
                queue.Enqueue(production.CurrentSupply);
                production.AverageSupply = queue.Average();
            }
            if (sumPowerProduced > 0.0 && powerProducers.Count > 0)
                supplyEfficiencyRatio /= sumPowerProduced;
            powerProducers.Sort();

            powerConsumers.Clear();
            // Must be resorted on each update as the priorities can be dynamic
            foreach (var pair in consumptionRequests)
                powerConsumers.Add(new PowerDistributionPair(pair.Key, pair.Value));
            powerConsumers.Sort();
            // There used to be a Reverse() here but it was non-functional as the result was ignored
            productionRequests.Clear();
            consumptionRequests.Clear();

            foreach (var pair in powerConsumers)
            {
                var resourceSuppliable = pair.Key;
                var demand = pair.Value;
                int priority = Math.Min(resourceSuppliable.getPowerPriority(), MAX_PRIORITY - 1);
                double minRatio = 0.10 + 0.02 * priority;
                double maxRequest = demand.PowerMaximumRequest, curRequest = demand.PowerCurrentRequest;

                // Process any in-between priority requests across all the available priorities
                while (priority > prevPriority)
                {
                    prevPriority++;
                    SupplyPriority(timeWarpDT, prevPriority);
                }

                // Efficiency throttling - prefer starving low priority consumers if supply efficiency is very low
                if (supplyEfficiencyRatio < minRatio && resourceName == FNRESOURCE_MEGAJOULES)
                    maxRequest *= Math.Max(0.0, supplyEfficiencyRatio) / minRatio;

                if (!maxRequest.IsInfinityOrNaNorZero())
                {
                    current.Demand += maxRequest;
                    if (priority == 0)
                        current.DemandHighPriority += maxRequest;
                }

                if (flowType == FNRESOURCE_FLOWTYPE_EVEN)
                    maxRequest *= (priority == 0 || priority == 1) ? hpSupplyDemandRatio : supplyDemandRatio;

                double powerSupplied = Math.Max(Math.Min(current.Supply, curRequest), 0.0);
                if (!powerSupplied.IsInfinityOrNaNorZero())
                {
                    current.Supply -= powerSupplied;                       
                    current.TotalSupplied += powerSupplied;
                    currentDistributed[priority] += powerSupplied;
                }

                double stableSupplied = Math.Max(Math.Min(current.StableSupply, maxRequest), 0.0);
                if (!stableSupplied.IsInfinityOrNaNorZero())
                {
                    current.StableSupply -= stableSupplied;
                    stableDistributed[priority] += stableSupplied;
                }

                // notify of supply
                resourceSuppliable.receiveFNResource(powerSupplied, resourceName);
            }

            // substract available resource amount to get delta resource change
            double supply = current.Supply - Math.Max(availableAmount, 0);
            double missingAmount = maxAmount - availableAmount;
            double powerToExtract = AdjustSupplyComplete(timeWarpDT, -supply * timeWarpDT);

            // Update storage
            if (powerToExtract > 0.0)
                powerToExtract = Math.Min(powerToExtract, availableAmount);
            else
                powerToExtract = Math.Max(powerToExtract, -missingAmount);

            if (!powerToExtract.IsInfinityOrNaN())
            {
                availableAmount += part.RequestResource(resourceDefinition.id, powerToExtract);
            }

            // Update resource fill fraction
            if (!maxAmount.IsInfinityOrNaNorZero() && !availableAmount.IsInfinityOrNaN())
                ResourceFillFraction = Math.Max(0.0, Math.Min(1.0, availableAmount / maxAmount));
            else
                ResourceFillFraction = 0.0;

            current.Supply = 0.0;
            current.StableSupply = 0.0;
        }

        public void UpdatePartModule(PartModule pm)
        {
            if (pm != null)
            {
                Vessel = pm.vessel;
                part = pm.part;
                PartModule = pm;
            }
            else
            {
                Vessel = null;
                part = null;
                PartModule = null;
            }
        }
    }
}
