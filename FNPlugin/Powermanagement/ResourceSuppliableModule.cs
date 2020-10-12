using FNPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    abstract class ResourceSuppliableModule : PartModule, IResourceSuppliable, IResourceSupplier
    {
        [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ResourceManager_UpdateCounter")]//Update Counter
        public long updateCounter;

        protected readonly Dictionary<Guid, double> connectedReceivers = new Dictionary<Guid, double>();
        protected readonly Dictionary<Guid, double> connectedReceiversFraction = new Dictionary<Guid, double>();

        protected List<Part> similarParts;
        protected string[] resources_to_supply;

        public Guid Id { get; private set;}

        protected int partNrInList;

        private readonly Dictionary<string, double> fnresource_supplied = new Dictionary<string, double>();

        public virtual void AttachThermalReciever(Guid key, double radius)
        {
            if (!connectedReceivers.ContainsKey(key))
                connectedReceivers.Add(key, radius);
        }

        public virtual void DetachThermalReciever(Guid key)
        {
            if (connectedReceivers.ContainsKey(key))
                connectedReceivers.Remove(key);
        }

        public virtual double GetFractionThermalReciever(Guid key)
        {
            if (connectedReceiversFraction.TryGetValue(key, out var result))
                return result;
            else
                return 0;
        }

        public void receiveFNResource(double power, string resourceName)
        {
            if (double.IsNaN(power) || string.IsNullOrEmpty(resourceName)) {
                Debug.Log("[KSPI]: receiveFNResource illegal values.");
                return;
            }

            fnresource_supplied[resourceName] = power;
        }

        public double consumeFNResource(double power_fixed, string resourcename, double fixedDeltaTime = 0)
        {
            if (power_fixed.IsInfinityOrNaN() || double.IsNaN(fixedDeltaTime) || string.IsNullOrEmpty(resourcename)) 
            {
                Debug.Log("[KSPI]: consumeFNResource illegal values.");
                return 0;
            }

            power_fixed = Math.Max(power_fixed, 0);

            var manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.TryGetValue(resourcename, out var availablePower))
                fnresource_supplied[resourcename] = 0;

            fixedDeltaTime = fixedDeltaTime > 0 ? fixedDeltaTime : TimeWarp.fixedDeltaTime;

            double power_taken_fixed = Math.Max(Math.Min(power_fixed, availablePower * fixedDeltaTime), 0);

            fnresource_supplied[resourcename] -= (power_taken_fixed / fixedDeltaTime);
            manager.powerDrawFixed(this, power_fixed, power_taken_fixed);

            return power_taken_fixed;
        }

        public double consumeFNResourcePerSecond(double power_requested_per_second, string resourcename, ResourceManager manager = null)
        {
            if (power_requested_per_second.IsInfinityOrNaN())
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecond was called with illegal value");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.TryGetValue(resourcename, out var availablePower))
                fnresource_supplied[resourcename] = 0;

            power_requested_per_second = Math.Max(power_requested_per_second, 0);

            double power_taken_per_second = Math.Max(Math.Min(power_requested_per_second, availablePower), 0);
            fnresource_supplied[resourcename] -= power_taken_per_second;

            manager.powerDrawPerSecond(this, power_requested_per_second, power_taken_per_second);

            return power_taken_per_second;
        }

        public double consumeFNResourcePerSecond(double power_requested_per_second, double maximum_power_requested_per_second, string resourceName, ResourceManager manager = null)
        {
            if (power_requested_per_second.IsInfinityOrNaN() || maximum_power_requested_per_second.IsInfinityOrNaN())
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecond was called with illegal values");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.TryGetValue(resourceName, out var availablePower))
                fnresource_supplied[resourceName] = 0;

            var power_taken_per_second = Math.Max(Math.Min(power_requested_per_second, availablePower), 0);
            fnresource_supplied[resourceName] -= power_taken_per_second;

            manager.powerDrawPerSecond(this, power_requested_per_second, Math.Max(maximum_power_requested_per_second, 0), power_taken_per_second);

            return power_taken_per_second;
        }

        public double consumeFNResourcePerSecondBuffered(double requestedPowerPerSecond, string resourceName, double limitBarRatio = 0.1, ResourceManager manager = null)
        {
            double timeWarpFixedDeltaTime = TimeWarp.fixedDeltaTime;
            if (requestedPowerPerSecond.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecondBuffered was called with illegal value");
                return 0;
            }

            requestedPowerPerSecond = Math.Max(requestedPowerPerSecond, 0);

            if (manager == null)
                manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.TryGetValue(resourceName, out var availablePower))
                fnresource_supplied[resourceName] = 0;
 
            var power_taken_per_second = Math.Max(Math.Min(requestedPowerPerSecond, availablePower), 0);
            fnresource_supplied[resourceName] -= power_taken_per_second;

            // supplement with buffer power if needed and available
            var powerShortage = requestedPowerPerSecond - availablePower;
            if (powerShortage > 0)
            {
                var currentCapacity = manager.GetTotalResourceCapacity();
                var currentAmount = currentCapacity * manager.ResourceFillFraction;
                var fixedPowerShortage = powerShortage * timeWarpFixedDeltaTime;

                if (currentAmount - fixedPowerShortage > currentCapacity * limitBarRatio)
                    power_taken_per_second += (part.RequestResource(resourceName, fixedPowerShortage) / timeWarpFixedDeltaTime);
            }

            manager.powerDrawPerSecond(this, requestedPowerPerSecond, power_taken_per_second);

            return power_taken_per_second;
        }

        public double supplyFNResourceFixed(double supply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourceFixed was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixed(this, Math.Max(supply, 0));
        }

        public double supplyFNResourcePerSecond(double supply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecond  was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double supplyFNResourceFixedWithMax(double supply, double maxSupply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || maxSupply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourceFixedWithMax  was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixedWithMax(this, Math.Max(supply, 0), Math.Max(maxSupply, 0));
        }

        public double supplyFNResourcePerSecondWithMaxAndEfficiency(double supply, double maxsupply, double efficiencyRatio, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || maxsupply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecondWithMaxAndEfficiency was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecondWithMaxAndEfficiency(this, Math.Max(supply, 0), Math.Max(maxsupply, 0), efficiencyRatio);
        }

        public double supplyFNResourcePerSecondWithMax(double supply, double maxsupply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || maxsupply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecondWithMax was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecondWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyManagedFNResourcePerSecond(double supply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyManagedFNResourcePerSecond  was called with illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.managedPowerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double getNeededPowerSupplyPerSecondWithMinimumRatio(double supply, double ratio_min, string resourceName, ResourceManager manager = null)
        {
            if (supply.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getNeededPowerSupplyPerSecondWithMinimumRatio was called with illegal values.");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.GetNeededPowerSupplyPerSecondWithMinimumRatio(Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double supplyManagedFNResourcePerSecondWithMinimumRatio(double supply, double ratio_min, string resourceName, ResourceManager manager = null)
        {
            if (supply.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyManagedFNResourcePerSecondWithMinimumRatio illegal values.");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.managedPowerSupplyPerSecondWithMinimumRatio(this, Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double managedProvidedPowerSupplyPerSecondMinimumRatio(double requested_power, double maximum_power, double ratio_min, string resourceName, ResourceManager manager = null)
        {
            if (requested_power.IsInfinityOrNaN()|| maximum_power.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName)) 
            {
                Debug.LogError("[KSPI]: managedProvidedPowerSupplyPerSecondMinimumRatio illegal values.");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            var result = manager.managedRequestedPowerSupplyPerSecondMinimumRatio(this, requested_power, Math.Max(maximum_power, 0), Math.Max(ratio_min, 0));

            return result.CurrentSupply;
        }

        public PowerGenerated managedPowerSupplyPerSecondMinimumRatio(double requested_power, double maximum_power, double ratio_min, string resourceName, ResourceManager manager = null)
        {
            if (requested_power.IsInfinityOrNaN() || maximum_power.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName)) 
            {
                Debug.LogError("[KSPI]: managedPowerSupplyPerSecondMinimumRatio illegal values.");
                return null;
            }

            if (manager == null)
                manager = getManagerForVessel(resourceName);
            if (manager == null)
                return null;

            return manager.managedRequestedPowerSupplyPerSecondMinimumRatio(this, requested_power, Math.Max(maximum_power, 0), Math.Max(ratio_min, 0));
        }

        public double getTotalPowerSupplied(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getTotalPowerSupplied resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.TotalPowerSupplied;
        }

        public double getStableResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getCurrentResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.StableResourceSupply;
        }

        public double getCurrentHighPriorityResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getCurrentHighPriorityResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceDemandHighPriority;
        }

        public double getAvailableStableSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.ResourceDemandHighPriority, 0);
        }

        public double getAvailablePrioritisedStableSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedStableSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.GetStablePriorityResourceSupply(getPowerPriority()), 0);
        }

        public double getAvailablePrioritisedCurrentSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedCurrentSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.ResourceSupply - manager.GetCurrentPriorityResourceSupply(getPowerPriority()), 0);
        }

        public double GetCurrentPriorityResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedCurrentSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetCurrentPriorityResourceSupply(getPowerPriority());
        }

        public double getStablePriorityResourceSupply(string resourcename, int priority)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetStablePriorityResourceSupply(priority);
        }

        public double getPriorityResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetStablePriorityResourceSupply(getPowerPriority());
        }

        public double getAvailableResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return Math.Max(manager.ResourceSupply - manager.ResourceDemandHighPriority, 0);
        }

        public double getCurrentResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: getResourceSupply resourceName is null or empty");
                return 0;
            }
            
            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.ResourceSupply;
        }

        public double GetCurrentSurplus(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: GetCurrentSurplus resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.CurrentSurplus;
        }

        public double getDemandStableSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: getDemandStableSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.DemandStableSupply;
        }

        public double getResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.ResourceDemand;
        }

        public double GetRequiredResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: GetRequiredResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.RequiredResourceDemand;
        }

        public double GetCurrentUnfilledResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: GetRequiredResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentUnfilledResourceDemand;
        }

        public double GetPowerSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) 
            {
                Debug.LogError("[KSPI]: GetPowerSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.CurrentResourceSupply;
        }

        public double getResourceBarRatio(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getResourceBarRatio resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return (double)manager.ResourceFillFraction;
        }

        public double getResourceBarFraction(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getResourceBarFraction resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceFillFraction;
        }

        public double getSpareResourceCapacity(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getSpareResourceCapacity resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetSpareResourceCapacity();
        }

        public double getResourceAvailability(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getResourceAvailability resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetResourceAvailability();
        }

        public double getTotalResourceCapacity(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getTotalResourceCapacity resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetTotalResourceCapacity();
        }

        public override void OnStart(PartModule.StartState state)
        {
            Id = Guid.NewGuid();

            if (state == StartState.Editor || resources_to_supply == null) return;

            part.OnJustAboutToBeDestroyed -= OnJustAboutToBeDestroyed;
            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;

            foreach (string resourcename in resources_to_supply)
            {
                ResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                if (manager == null)
                {
                    similarParts = null;
                    manager = CreateResourceManagerForResource(resourcename);

                    Debug.Log("[KSPI]: ResourceSuppliableModule.OnStart created Resource Manager for Vessel " + vessel.GetName() + " for " + resourcename + " with manager Id " + manager.Id + " and overmanager id " + manager.OverManagerId);
                }
            }

            var priorityManager = getSupplyPriorityManager(this.vessel);
            if (priorityManager != null)
                priorityManager.Register(this);
        }

        private void OnJustAboutToBeDestroyed()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            Debug.LogWarning("[KSPI]: detecting supplyable part " + part.partInfo.title + " is being destroyed");

            var priority_manager = getSupplyPriorityManager(this.vessel);
            if (priority_manager != null)
                priority_manager.Unregister(this);
        }

        public override void OnFixedUpdate()
        {
            double timeWarpFixedDeltaTime = TimeWarp.fixedDeltaTime;

            updateCounter++;

            if (resources_to_supply == null) return;

            foreach (string resourcename in resources_to_supply)
            {
                var overmanager = getOvermanagerForResource(resourcename);

                ResourceManager resource_manager = null;;

                if (overmanager != null)
                    resource_manager = overmanager.getManagerForVessel(vessel);

                if (resource_manager == null)
                {
                    similarParts = null;
                    resource_manager = CreateResourceManagerForResource(resourcename);

                    Debug.Log("[KSPI]: ResourceSuppliableModule.OnFixedUpdate created Resourcemanager for Vessel " + vessel.GetName() + " for " + resourcename + " with ResourceManagerId " + resource_manager.Id + " and OvermanagerId" + resource_manager.Id);
                }

                if (resource_manager != null)
                {
                    if (resource_manager.PartModule == null || resource_manager.PartModule.vessel != this.vessel || resource_manager.Counter < updateCounter)
                        resource_manager.UpdatePartModule(this);

                    if (resource_manager.PartModule == this)
                        resource_manager.update(updateCounter);
                }
            }

            var priority_manager = getSupplyPriorityManager(this.vessel);
            if (priority_manager != null)
            {
                priority_manager.Register(this);

                if (priority_manager.ProcessingPart == null || priority_manager.ProcessingPart.vessel != this.vessel || priority_manager.Counter < updateCounter)
                    priority_manager.UpdatePartModule(this);

                if (priority_manager.ProcessingPart == this)
                    priority_manager.UpdateResourceSuppliables(updateCounter, timeWarpFixedDeltaTime);
            }
        }

        public void RemoveItselfAsManager()
        {
            foreach (string resourcename in resources_to_supply)
            {
                var overmanager = getOvermanagerForResource(resourcename);

                if (overmanager == null)
                    continue;

                ResourceManager resource_manager = overmanager.getManagerForVessel(vessel);

                if (resource_manager != null && resource_manager.PartModule == this)
                    resource_manager.UpdatePartModule(null);
            }
        }

        public virtual string getResourceManagerDisplayName()
        {
            string displayName = part.partInfo.title;

            if (similarParts == null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            if (similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }

        // default priority
        public virtual int getPowerPriority()
        {
            return 2;
        }

        public virtual int getSupplyPriority()
        {
            return getPowerPriority();
        }

        private ResourceManager CreateResourceManagerForResource(string resourcename)
        {
            return getOvermanagerForResource(resourcename).CreateManagerForVessel(this);
        }

        private ResourceOvermanager getOvermanagerForResource(string resourcename)
        {
            return ResourceOvermanager.getResourceOvermanagerForResource(resourcename);
        }

        protected ResourceManager getManagerForVessel(string resourcename)
        {
            return getManagerForVessel(resourcename, vessel);
        }

        private ResourceManager getManagerForVessel(string resourcename, Vessel vessel)
        {
            var overmanager = getOvermanagerForResource(resourcename);
            if (overmanager == null)
            {
                Debug.LogError("[KSPI]: ResourceSuppliableModule failed to find " + resourcename + " Overmanager for " + vessel.name);
                return null;
            }
            return overmanager.getManagerForVessel(vessel);
        }

        private SupplyPriorityManager getSupplyPriorityManager(Vessel vessel)
        {
            return SupplyPriorityManager.GetSupplyPriorityManagerForVessel(vessel);
        }

        public virtual void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {

        }

        public virtual void OnPostResourceSuppliable(double fixedDeltaTime)
        {

        }
    }
}
