using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    public static class ResourceManagerFactory
    {
        // Create the appropriate instance
        public static ResourceManager Create(Guid id, ResourceSuppliableModule pm, string resourceName)
        {
            ResourceManager result;

            if (resourceName == ResourceSettings.Config.ElectricPowerInMegawatt)
                result = new MegajoulesResourceManager(id, pm);
            else if (resourceName == ResourceSettings.Config.WasteHeatInMegawatt)
                result = new WasteHeatResourceManager(id, pm);
            else if(resourceName == ResourceSettings.Config.ChargedPowerInMegawatt)
                result = new ChargedPowerResourceManager(id, pm);
            else if(resourceName == ResourceSettings.Config.ThermalPowerInMegawatt)
                result = new ThermalPowerResourceManager(id, pm);
            else
                result = new DefaultResourceManager(id, pm, resourceName);

            return result;
        }
    }

    public abstract class ResourceManager
    {
        public const int FnResourceFlowTypeSmallestFirst = 0;
        public const int FnResourceFlowTypeEven = 1;

        protected const int LabelWidth = 305;
        protected const int ValueWidth = 55;
        protected const int PriorityWidth = 30;
        protected const int OverviewWidth = 65;
        protected const int MaxPriority = 6;
        protected const int PowerHistoryLen = 10;

        protected readonly List<PartResource> partResources = new List<PartResource>();
        protected readonly IDictionary<IResourceSuppliable, PowerDistribution> consumptionRequests;

        private readonly IDictionary<IResourceSupplier, PowerGenerated> productionTemp;
        private readonly IDictionary<IResourceSupplier, PowerGenerated> productionRequests;

        private readonly List<PowerDistributionPair> powerConsumers;
        private readonly List<PowerGeneratedPair> powerProducers;

        private readonly double[] currentDistributed;
        private readonly double[] stableDistributed;

        protected Part part;
        protected readonly PowerStats current, last;
        protected readonly string resourceName;
        protected readonly int flowType;
        protected readonly PartResourceDefinition resourceDefinition;

        private readonly int _windowId;
        private bool renderWindow;

        protected GUIStyle leftBoldLabel;
        protected GUIStyle rightBoldLabel;
        protected GUIStyle greenLabel;
        protected GUIStyle redLabel;
        protected GUIStyle leftAlignedLabel;
        protected GUIStyle rightAlignedLabel;

        protected virtual double AuxiliaryResourceDemand => 0.0;

        public long Counter { get; private set; }

        public double CurrentConsumption { get; private set; }

        public double CurrentResourceSupply => current.Supply;

        public virtual double CurrentSurplus => Math.Max(0.0, current.Supply - CurrentConsumption);

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

        public ResourceSuppliableModule PartModule { get; private set; }

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

        protected ResourceManager(Guid overmanagerId, ResourceSuppliableModule pm, string resourceName, int flowType)
        {
            OverManagerId = overmanagerId;
            Id = Guid.NewGuid();

            this.flowType = flowType;
            this.resourceName = resourceName;
            Vessel = pm.vessel;
            part = pm.part;
            PartModule = pm;
            renderWindow = false;

            _windowId = new System.Random(resourceName.GetHashCode()).Next(int.MinValue, int.MaxValue);
            WindowPosition = new Rect(0, 0, LabelWidth + ValueWidth + PriorityWidth, 50);

            currentDistributed = new double[MaxPriority];
            stableDistributed = new double[MaxPriority];
            // Cannot use SortedDictionary as the priority for some items is dynamic
            consumptionRequests = new Dictionary<IResourceSuppliable, PowerDistribution>(64);
            // Must be kept separately as the producer list gets rebuilt every update
            productionTemp = new Dictionary<IResourceSupplier, PowerGenerated>(64);
            productionRequests = new Dictionary<IResourceSupplier, PowerGenerated>(64);
            powerConsumers = new List<PowerDistributionPair>(64);
            powerProducers = new List<PowerGeneratedPair>(64);

            resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            last = new PowerStats();
            current = new PowerStats();
            ResourceFillFraction = 0.0;
        }

        protected virtual double AdjustSupplyComplete(double timeWarpDt, double powerToExtract)
        {
            return powerToExtract;
        }

        protected void DoWindow(int windowId)
        {
            double netChange = ResourceNetChange;
            double netUtilization = DemandStableSupply;

            if (leftBoldLabel == null)
            {
                leftBoldLabel = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    font = PluginHelper.MainFont
                };
            }

            if (rightBoldLabel == null)
            {
                rightBoldLabel = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (greenLabel == null)
            {
                greenLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = resourceName == ResourceSettings.Config.WasteHeatInMegawatt ? Color.red : Color.green },
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (redLabel == null)
            {
                redLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = resourceName == ResourceSettings.Config.WasteHeatInMegawatt ? Color.green : Color.red },
                    font = PluginHelper.MainFont,
                    alignment = TextAnchor.MiddleRight
                };
            }

            if (leftAlignedLabel == null)
            {
                leftAlignedLabel = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    font = PluginHelper.MainFont
                };
            }

            if (rightAlignedLabel == null)
            {
                rightAlignedLabel = new GUIStyle(GUI.skin.label)
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
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_TheoreticalSupply"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Theoretical Supply"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(StableResourceSupply), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OverviewWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_CurrentSupply"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Current Supply"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(ResourceSupply), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OverviewWidth));
            GUILayout.EndHorizontal();

            DoWindowInitial();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_PowerDemand"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Power Demand"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(ResourceDemand), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OverviewWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            string newPowerLabel = (resourceName == ResourceSettings.Config.WasteHeatInMegawatt)
                ? Localizer.Format("#LOC_KSPIE_ResourceManager_NetChange")
                : Localizer.Format("#LOC_KSPIE_ResourceManager_NetPower");//"Net Change""Net Power"

            GUILayout.Label(newPowerLabel, leftBoldLabel, GUILayout.ExpandWidth(true));

            GUIStyle netPowerStyle = netChange < -0.001 ? redLabel : greenLabel;

            GUILayout.Label(PluginHelper.GetFormattedPowerString(netChange), netPowerStyle, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OverviewWidth));
            GUILayout.EndHorizontal();

            if (!netUtilization.IsInfinityOrNaN() && (resourceName != ResourceSettings.Config.ElectricPowerInMegawatt || netUtilization < 2.0 || ResourceSupply >= last.Demand))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Utilisation"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Utilisation"

                GUIStyle utilisationStyle = netUtilization > 1.001 ? redLabel : greenLabel;

                GUILayout.Label(netUtilization.ToString("P2"), utilisationStyle, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OverviewWidth));
                GUILayout.EndHorizontal();
            }

            if (powerProducers != null)
            {
                var summaryList = new List<PowerProduction>(16);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_ProducerComponent"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Producer Component"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Supply"), rightBoldLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));//"Supply"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Max"), rightBoldLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));//"Max"
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
                    GUILayout.Label(production.Component, leftAlignedLabel, GUILayout.ExpandWidth(true));
                    GUILayout.Label(PluginHelper.GetFormattedPowerString(production.AverageSupply), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));
                    GUILayout.Label(PluginHelper.GetFormattedPowerString(production.MaximumSupply), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));
                    GUILayout.EndHorizontal();
                }
            }

            if (powerConsumers != null)
            {
                var summaryList = new List<PowerConsumption>(16);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_ConsumerComponent"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Consumer Component"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Demand"), rightBoldLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));//"Demand"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_Rank"), rightBoldLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(PriorityWidth));//"Rank"
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
                    double utilization = sumRequest > 0.0 ? sumConsumed / sumRequest : 0.0;

                    string name = group.Key;
                    int count = group.Count();
                    if (count > 1)
                        name = count + " * " + name;

                    var utilizationTolerance = sumRequest > 0.1 ? 0.995 : 0.9;
                    if (sumRequest > 0.0000015 && resourceName == ResourceSettings.Config.ElectricPowerInMegawatt && utilization < utilizationTolerance)
                        name = name + " " + utilization.ToString("P0");

                    summaryList.Add(new PowerConsumption(name, priority, sumRequest));
                }
                summaryList.Sort();

                foreach (var consumption in summaryList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(consumption.Component, leftAlignedLabel, GUILayout.ExpandWidth(true));
                    GUILayout.Label(PluginHelper.GetFormattedPowerString(consumption.Sum), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));
                    GUILayout.Label(consumption.Priority.ToString(), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(PriorityWidth));
                    GUILayout.EndHorizontal();
                }
            }

            DoWindowFinal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        protected virtual void DoWindowInitial() { }

        protected virtual void DoWindowFinal() { }

        private PowerGenerated GetProductionRequest(IResourceSupplier pm, double maximumPower, double requiredDemand, double minPower)
        {
            double managedSupply = Math.Min(maximumPower, Math.Max(minPower, requiredDemand));

            current.Supply += managedSupply;
            current.StableSupply += maximumPower;

            if (!productionRequests.TryGetValue(pm, out PowerGenerated powerGenerated))
            {
                productionRequests.Add(pm, powerGenerated = new PowerGenerated());
            }

            powerGenerated.CurrentProvided = managedSupply;
            powerGenerated.CurrentSupply += managedSupply;
            powerGenerated.MaximumSupply += maximumPower;
            powerGenerated.MinimumSupply += minPower;

            return powerGenerated;
        }

        public double GetResourceAvailability()
        {
            if (Kerbalism.IsLoaded)
            {
                GetAvailableResources(out var amount, out _);
                return amount;
            }
            else
            {
                part.GetConnectedResourceTotals(resourceDefinition.id, out double amount, out _);
                return amount;
            }
        }

        public double GetSpareResourceCapacity()
        {
            if (Kerbalism.IsLoaded)
            {
                GetAvailableResources(out var amount, out var maxAmount);
                return maxAmount - amount;
            }
            else
            {
                part.GetConnectedResourceTotals(resourceDefinition.id, out double amount, out var maxAmount);
                return maxAmount - amount;
            }
        }

        public double GetTotalResourceCapacity()
        {
            if (Kerbalism.IsLoaded)
            {
                GetAvailableResources(out _, out var maxAmount);
                return maxAmount;
            }
            else
            {
                part.GetConnectedResourceTotals(resourceDefinition.id, out _, out var maxAmount);
                return maxAmount;
            }
        }

        public double GetNeededPowerSupplyPerSecondWithMinimumRatio(double power, double ratioMin)
        {
            return Math.Min(power, Math.Max(CurrentUnfilledResourceDemand, power * ratioMin));
        }

        public double GetCurrentPriorityResourceSupply(int priority)
        {
            double total = AuxiliaryResourceDemand;
            int maxPriority = Math.Min(priority, MaxPriority);

            for (int i = 0; i < maxPriority; i++)
            {
                total += currentDistributed[i];
            }

            return total;
        }

        public double GetStablePriorityResourceSupply(int priority)
        {
            double total = AuxiliaryResourceDemand;
            int maxPriority = Math.Min(priority, MaxPriority);

            for (int i = 0; i < maxPriority; i++)
            {
                total += stableDistributed[i];
            }

            return total;
        }

        public double ManagedPowerSupplyPerSecond(IResourceSupplier pm, double power)
        {
            return ManagedPowerSupplyPerSecondWithMinimumRatio(pm, power, 0.0);
        }

        public double ManagedPowerSupplyPerSecondWithMinimumRatio(IResourceSupplier pm, double maximumPower, double ratioMin)
        {
            if (maximumPower.IsInfinityOrNaN() || ratioMin.IsInfinityOrNaN())
                return 0.0;

            double minPower = maximumPower * ratioMin;
            double providedPower = Math.Min(maximumPower, Math.Max(minPower, CurrentUnfilledResourceDemand));

            GetProductionRequest(pm, maximumPower, RequiredResourceDemand, minPower).CurrentProvided += providedPower;

            return providedPower;
        }

        public PowerGenerated ManagedRequestedPowerSupplyPerSecondMinimumRatio(IResourceSupplier pm, double availablePower, double maximumPower, double ratioMin)
        {
            if (availablePower.IsInfinityOrNaN() || maximumPower.IsInfinityOrNaN() || ratioMin.IsInfinityOrNaN())
                return new PowerGenerated();

            double minPower = maximumPower * ratioMin;
            double providedPower = Math.Min(maximumPower, Math.Max(minPower, Math.Max(availablePower, CurrentUnfilledResourceDemand)));

            var request = GetProductionRequest(pm, maximumPower, Math.Min(availablePower, RequiredResourceDemand), minPower);
            request.CurrentProvided = Math.Min(providedPower, request.CurrentProvided);

            return request;
        }

        public void OnGUI()
        {
            if (Vessel == FlightGlobals.ActiveVessel && renderWindow)
            {
                string title = resourceName + " " + Localizer.Format("#LOC_KSPIE_ResourceManager_title");//Management Display
                WindowPosition = GUILayout.Window(_windowId, WindowPosition, DoWindow, title);
            }
        }

        public void PowerDrawFixed(IResourceSuppliable pm, double powerDraw, double powerConsumption)
        {
            if (powerDraw.IsInfinityOrNaN() || powerConsumption.IsInfinityOrNaN())
                return;

            double timeWarpDt = Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);
            double powerPerSecond = powerDraw / timeWarpDt;
            PowerDrawPerSecond(pm, powerPerSecond, powerPerSecond, powerConsumption / timeWarpDt);
        }

        public void PowerDrawPerSecond(IResourceSuppliable pm, double powerRequested, double powerConsumed)
        {
            PowerDrawPerSecond(pm, powerRequested, powerRequested, powerConsumed);
        }

        public void PowerDrawPerSecond(IResourceSuppliable pm, double powerCurrentRequested, double powerMaximumRequested, double powerConsumed)
        {
            if (powerCurrentRequested.IsInfinityOrNaN() || powerMaximumRequested.IsInfinityOrNaN() || powerConsumed.IsInfinityOrNaN())
                return;

            CurrentConsumption += powerConsumed;

            if (!consumptionRequests.TryGetValue(pm, out PowerDistribution powerDistribution))
            {
                consumptionRequests.Add(pm, powerDistribution = new PowerDistribution());
            }

            powerDistribution.PowerCurrentRequest += powerCurrentRequested;
            powerDistribution.PowerMaximumRequest += powerMaximumRequested;
            powerDistribution.PowerConsumed += powerConsumed;
        }

        public double PowerSupplyFixed(IResourceSupplier pm, double power)
        {
            double powerFixed = power / Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);
            return PowerSupplyPerSecondWithMaxAndEfficiency(pm, powerFixed, powerFixed, 1.0);
        }

        public double PowerSupplyPerSecond(IResourceSupplier pm, double power)
        {
            return PowerSupplyPerSecondWithMaxAndEfficiency(pm, power, power, 1.0);
        }

        public double PowerSupplyFixedWithMax(IResourceSupplier pm, double power, double maxPower)
        {
            double timeWarpDt = Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);
            return PowerSupplyPerSecondWithMaxAndEfficiency(pm, power / timeWarpDt, maxPower / timeWarpDt, 1.0);
        }

        public double PowerSupplyPerSecondWithMaxAndEfficiency(IResourceSupplier pm, double power, double maxPower, double efficiencyRatio)
        {
            if (power.IsInfinityOrNaN() || maxPower.IsInfinityOrNaN() || efficiencyRatio.IsInfinityOrNaN())
                return 0.0;

            power = Math.Min(power, maxPower);

            current.Supply += power;
            current.StableSupply += maxPower;

            if (!productionRequests.TryGetValue(pm, out PowerGenerated powerGenerated))
            {
                productionRequests.Add(pm, powerGenerated = new PowerGenerated());
            }
            powerGenerated.CurrentSupply += power;
            powerGenerated.CurrentProvided += power;
            powerGenerated.MaximumSupply += maxPower;
            powerGenerated.EfficiencyRatio = efficiencyRatio;

            return power;
        }

        public double PowerSupplyPerSecondWithMax(IResourceSupplier pm, double power, double maxPower)
        {
            return PowerSupplyPerSecondWithMaxAndEfficiency(pm, power, maxPower, 1.0);
        }

        public void ShowWindow()
        {
            renderWindow = true;
        }

        protected virtual void SupplyPriority(double timeWarpDt, int priority) { }

        public virtual void Update(long counter)
        {
            double timeWarpDt = Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);
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

            for (int i = 0; i < MaxPriority; i++)
            {
                currentDistributed[i] = 0.0;
                stableDistributed[i] = 0.0;
            }

            //part.GetConnectedResourceTotals(resourceDefinition.id, out double availableAmount, out double maxAmount);
            partResources.Clear();
            partResources.AddRange(Vessel.parts.SelectMany(p => p.Resources.Where(r => r.info.id == resourceDefinition.id)));
            GetAvailableResources(out var availableAmount, out var maxAmount);

            if (availableAmount.IsInfinityOrNaN())
                availableAmount = 0.0;

            double hpSupplyDemandRatio = last.DemandHighPriority > 0.0 ? Math.Min((current.Supply - AuxiliaryResourceDemand) / last.DemandHighPriority, 1.0) : 1.0;
            double supplyDemandRatio = last.Demand > 0.0 ? Math.Min((current.Supply - AuxiliaryResourceDemand - last.DemandHighPriority) / last.Demand, 1.0) : 1.0;

            current.Supply += availableAmount;
            current.StableSupply += availableAmount;

            // Avoid leaking power producer history if they are removed from the vessel
            foreach (var pair in powerProducers)
                productionTemp.Add(pair.Key, pair.Value);
            powerProducers.Clear();
            // Must be resorted on each update as the production can be dynamic
            foreach (var pair in productionRequests)
            {
                var key = pair.Key;
                var production = pair.Value;

                powerProducers.Add(new PowerGeneratedPair(pair.Key, production));
                sumPowerProduced += production.MaximumSupply;
                supplyEfficiencyRatio += production.EfficiencyRatio * production.MaximumSupply;

                var history = productionTemp.TryGetValue(key, out PowerGenerated old) ? old.History : new Queue<double>(PowerHistoryLen);

                if (history.Count > PowerHistoryLen)
                    history.Dequeue();
                history.Enqueue(production.CurrentSupply);
                production.AverageSupply = history.Max();
                production.History = history;
            }
            if (sumPowerProduced > 0.0 && powerProducers.Count > 0)
                supplyEfficiencyRatio /= sumPowerProduced;

            powerProducers.Sort();
            productionTemp.Clear();

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
                int priority = Math.Min(resourceSuppliable.getPowerPriority(), MaxPriority - 1);
                double minRatio = 0.10 + 0.02 * priority;
                double maxRequest = demand.PowerMaximumRequest, curRequest = demand.PowerCurrentRequest;

                // Process any in-between priority requests across all the available priorities
                while (priority > prevPriority)
                {
                    SupplyPriority(timeWarpDt, ++prevPriority);
                }

                // Efficiency throttling - prefer starving low priority consumers if supply efficiency is very low
                if (supplyEfficiencyRatio < minRatio && resourceName == ResourceSettings.Config.ElectricPowerInMegawatt)
                    maxRequest *= Math.Max(0.0, supplyEfficiencyRatio) / minRatio;

                if (!maxRequest.IsInfinityOrNaNorZero())
                {
                    current.Demand += maxRequest;
                    if (priority == 0)
                        current.DemandHighPriority += maxRequest;
                }

                if (flowType == FnResourceFlowTypeEven)
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

            // Process any priority requests not run due to low priority items not existing
            while (MaxPriority - 1 > prevPriority)
            {
                SupplyPriority(timeWarpDt, ++prevPriority);
            }

            // subtract available resource amount to get delta resource change
            double supply = current.Supply - Math.Max(availableAmount, 0);
            double missingAmount = maxAmount - availableAmount;
            double powerToExtract = AdjustSupplyComplete(timeWarpDt, -supply * timeWarpDt);

            // Update storage
            powerToExtract = powerToExtract > 0.0 ? Math.Min(powerToExtract, availableAmount) : Math.Max(powerToExtract, -missingAmount);

            if (!powerToExtract.IsInfinityOrNaN())
            {
                if (Kerbalism.IsLoaded)
                    RequestResource(partResources, powerToExtract, maxAmount, availableAmount);
                else
                    part.RequestResource(resourceDefinition.id, powerToExtract);
            }

            // Update resource fill fraction
            GetAvailableResources(out var finalAvailableAmount, out var finalMaxAmount);
            if (!maxAmount.IsInfinityOrNaNorZero() && !finalAvailableAmount.IsInfinityOrNaN())
                ResourceFillFraction = Math.Max(0.0, Math.Min(1.0, finalAvailableAmount / finalMaxAmount));
            else
                ResourceFillFraction = 0.0;

            current.Supply = 0.0;
            current.StableSupply = 0.0;
        }

        private void GetAvailableResources(out double availableAmount, out double maxAmount)
        {
            maxAmount = 0;
            availableAmount = 0;

            foreach (var partResource in partResources)
            {
                maxAmount += partResource.maxAmount;
                availableAmount += partResource.amount;
                partResource.flowState = !Kerbalism.IsLoaded;
            }
        }

        private static void RequestResource(List<PartResource> partResources, double powerToExtract, double maxAmount, double availableAmount)
        {
            foreach (var partResource in partResources)
            {
                availableAmount = RequestResource(partResource, powerToExtract, maxAmount, availableAmount);
            }
        }

        private static double RequestResource(PartResource partResource, double powerToExtract, double maxAmount, double availableAmount)
        {
            var newAmount = partResource.amount;
            var requested = powerToExtract * (partResource.maxAmount / maxAmount);
            newAmount -= requested;
            var shortage = newAmount < 0 ? newAmount : 0;
            availableAmount += requested - shortage;
            partResource.amount = Math.Max(0, newAmount);
            return availableAmount;
        }

        public void UpdatePartModule(ResourceSuppliableModule pm)
        {
            if (pm != null)
            {
                Vessel = pm.vessel;
                part = pm.part;
                PartModule = pm;

                if (resourceName == ResourceSettings.Config.ElectricPowerInMegawatt)
                    SetWindowPosition(pm.epx, pm.epy, (int)WindowPosition.x, (int)WindowPosition.y);
                else if (resourceName == ResourceSettings.Config.WasteHeatInMegawatt)
                    SetWindowPosition(pm.whx, pm.why, (int)WindowPosition.x, (int)WindowPosition.y);
                else if (resourceName == ResourceSettings.Config.ChargedPowerInMegawatt)
                    SetWindowPosition(pm.cpx, pm.cpy, (int)WindowPosition.x, (int)WindowPosition.y);
                else if (resourceName == ResourceSettings.Config.ThermalPowerInMegawatt)
                    SetWindowPosition(pm.tpx, pm.tpy, (int)WindowPosition.x, (int)WindowPosition.y);
            }
            else
            {
                Vessel = null;
                part = null;
                PartModule = null;
            }
        }

        protected void SetWindowPosition(int storedX, int storedY, int defaultX, int defaultY)
        {
            var xPosition = storedX == 0 ? defaultX : storedX;
            var yPosition = storedY == 0 ? defaultY : storedY;

            WindowPosition = new Rect(xPosition, yPosition, LabelWidth + ValueWidth + PriorityWidth, 50);
        }
    }
}
