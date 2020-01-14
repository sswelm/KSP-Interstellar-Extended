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

        protected List<Part> similarParts;
        protected String[] resources_to_supply;

        protected int partNrInList;
        protected double timeWarpFixedDeltaTime;

        private Dictionary<String, double> fnresource_supplied = new Dictionary<String, double>();

        public void receiveFNResource(double power, String resourcename)
        {
            if (double.IsNaN(power) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI]: receiveFNResource illegal values.");
                return;
            }

            fnresource_supplied[resourcename] = power;
        }

        public double consumeFNResource(double power_fixed, String resourcename, double fixedDeltaTime = 0)
        {
            if (power_fixed.IsInfinityOrNaN() || double.IsNaN(fixedDeltaTime) || String.IsNullOrEmpty(resourcename)) 
            {
                Debug.Log("[KSPI]: consumeFNResource illegal values.");
                return 0;
            }

            power_fixed = Math.Max(power_fixed, 0);

            var manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            double availablePower;
            if (!fnresource_supplied.TryGetValue(resourcename, out availablePower))
                fnresource_supplied[resourcename] = 0;

            fixedDeltaTime = fixedDeltaTime > 0 ? fixedDeltaTime : (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);

            double power_taken_fixed = Math.Max(Math.Min(power_fixed, availablePower * fixedDeltaTime), 0);

            fnresource_supplied[resourcename] -= (power_taken_fixed / fixedDeltaTime);
            manager.powerDrawFixed(this, power_fixed, power_taken_fixed);

            return power_taken_fixed;
        }

        public double consumeFNResourcePerSecond(double power_requested_per_second, String resourcename, ResourceManager manager = null)
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

            double availablePower;
            if (!fnresource_supplied.TryGetValue(resourcename, out availablePower))
                fnresource_supplied[resourcename] = 0;

            power_requested_per_second = Math.Max(power_requested_per_second, 0);

            double power_taken_per_second = Math.Max(Math.Min(power_requested_per_second, availablePower), 0);
            fnresource_supplied[resourcename] -= power_taken_per_second;

            manager.powerDrawPerSecond(this, power_requested_per_second, power_taken_per_second);

            return power_taken_per_second;
        }


        public double consumeFNResourcePerSecond(double power_requested_per_second, double maximum_power_requested_per_second, String resourcename, ResourceManager manager = null)
        {
            if (power_requested_per_second.IsInfinityOrNaN() || maximum_power_requested_per_second.IsInfinityOrNaN())
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecond was called with illegal values");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            double availablePower;
            if (!fnresource_supplied.TryGetValue(resourcename, out availablePower))
                fnresource_supplied[resourcename] = 0;

            var power_taken_per_second = Math.Max(Math.Min(power_requested_per_second, availablePower), 0);
            fnresource_supplied[resourcename] -= power_taken_per_second;

            manager.powerDrawPerSecond(this, power_requested_per_second, Math.Max(maximum_power_requested_per_second, 0), power_taken_per_second);

            return power_taken_per_second;
        }

        public double consumeFNResourcePerSecondBuffered(double requestedPowerPerSecond, String resourcename, double limitBarRatio = 0.1, ResourceManager manager = null)
        {
            if (requestedPowerPerSecond.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecondBuffered was called with illegal value");
                return 0;
            }

            requestedPowerPerSecond = Math.Max(requestedPowerPerSecond, 0);

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            double availablePower;
            if (!fnresource_supplied.TryGetValue(resourcename, out availablePower))
                fnresource_supplied[resourcename] = 0;
 
            var power_taken_per_second = Math.Max(Math.Min(requestedPowerPerSecond, availablePower), 0);
            fnresource_supplied[resourcename] -= power_taken_per_second;

            // supplement with buffer power if needed and available
            var powerShortage = requestedPowerPerSecond - availablePower;
            if (powerShortage > 0)
            {
                var currentCapacity = manager.getTotalResourceCapacity();
                var currentAmount = currentCapacity * manager.ResourceBarRatioBegin;
                var fixedPowerShortage = powerShortage * timeWarpFixedDeltaTime;

                if (currentAmount - fixedPowerShortage > currentCapacity * limitBarRatio)
                    power_taken_per_second += (part.RequestResource(resourcename, fixedPowerShortage) / timeWarpFixedDeltaTime);
            }

            manager.powerDrawPerSecond(this, requestedPowerPerSecond, power_taken_per_second);

            return power_taken_per_second;
        }

        public double supplyFNResourceFixed(double supply, String resourcename)
        {
            if (supply.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: supplyFNResourceFixed was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixed(this, Math.Max(supply, 0));
        }

        public double supplyFNResourcePerSecond(double supply, String resourcename)
        {
            if (supply.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecond  was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double supplyFNResourceFixedWithMax(double supply, double maxsupply, String resourcename)
        {
            if (supply.IsInfinityOrNaN() || maxsupply.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: supplyFNResourceFixedWithMax  was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixedWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyFNResourcePerSecondWithMaxAndEfficiency(double supply, double maxsupply, double efficiencyRatio, String resourcename)
        {
            if (supply.IsInfinityOrNaN() || maxsupply.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecondWithMaxAndEfficiency was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecondWithMaxAndEfficiency(this, Math.Max(supply, 0), Math.Max(maxsupply, 0), efficiencyRatio);
        }

        public double supplyFNResourcePerSecondWithMax(double supply, double maxsupply, String resourcename)
        {
            if (supply.IsInfinityOrNaN() || maxsupply.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecondWithMax was called with illegal value");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecondWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyManagedFNResourcePerSecond(double supply, String resourcename)
        {
            if (supply.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: supplyManagedFNResourcePerSecond  was called with illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.managedPowerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double getNeededPowerSupplyPerSecondWithMinimumRatio(double supply, double ratio_min, String resourcename, ResourceManager manager = null)
        {
            if (supply.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getNeededPowerSupplyPerSecondWithMinimumRatio was called with illegal values.");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getNeededPowerSupplyPerSecondWithMinimumRatio(Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double supplyManagedFNResourcePerSecondWithMinimumRatio(double supply, double ratio_min, String resourcename, ResourceManager manager = null)
        {
            if (supply.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: supplyManagedFNResourcePerSecondWithMinimumRatio illegal values.");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.managedPowerSupplyPerSecondWithMinimumRatio(this, Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double managedProvidedPowerSupplyPerSecondMinimumRatio(double requested_power, double maximum_power, double ratio_min, String resourcename, ResourceManager manager = null)
        {
            if (requested_power.IsInfinityOrNaN()|| maximum_power.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: managedProvidedPowerSupplyPerSecondMinimumRatio illegal values.");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            var result = manager.managedRequestedPowerSupplyPerSecondMinimumRatio(this, requested_power, Math.Max(maximum_power, 0), Math.Max(ratio_min, 0));

            return result.currentSupply;
        }

        public PowerGenerated managedPowerSupplyPerSecondMinimumRatio(double requested_power, double maximum_power, double ratio_min, String resourcename, ResourceManager manager = null)
        {
            if (requested_power.IsInfinityOrNaN() || maximum_power.IsInfinityOrNaN() || ratio_min.IsInfinityOrNaN() || String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: managedPowerSupplyPerSecondMinimumRatio illegal values.");
                return null;
            }

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return null;

            return manager.managedRequestedPowerSupplyPerSecondMinimumRatio(this, requested_power, Math.Max(maximum_power, 0), Math.Max(ratio_min, 0));
        }

        public double getCurrentResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getCurrentResourceDemand resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentResourceDemand;
        }

        public double getTotalPowerSupplied(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getTotalPowerSupplied resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.TotalPowerSupplied;
        }

        public double getStableResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getCurrentResourceDemand resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.StableResourceSupply;
        }

        public double getCurrentHighPriorityResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getCurrentHighPriorityResourceDemand resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentHighPriorityResourceDemand;
        }

        public double getAvailableStableSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.CurrentHighPriorityResourceDemand, 0);
        }

        public double getAvailablePrioritisedStableSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedStableSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.GetStablePriorityResourceSupply(getPowerPriority()), 0);
        }

        public double getAvailablePrioritisedCurrentSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedCurrentSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.CurrentResourceSupply - manager.GetCurrentPriorityResourceSupply(getPowerPriority()), 0);
        }

        public double GetCurrentPriorityResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedCurrentSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetCurrentPriorityResourceSupply(getPowerPriority());
        }

        public double getStablePriorityResourceSupply(String resourcename, int priority)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetStablePriorityResourceSupply(priority);
        }

        public double getPriorityResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetStablePriorityResourceSupply(getPowerPriority());
        }

        public double getAvailableResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return Math.Max(manager.CurrentResourceSupply - manager.CurrentHighPriorityResourceDemand, 0);
        }

        public double getCurrentResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: getResourceSupply resourcename is null or empty");
                return 0;
            }
            
            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.CurrentResourceSupply;
        }

        public double GetCurrentSurplus(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: GetOverproduction resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.getCurrentSurplus();
        }

        public double getDemandStableSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: getDemandStableSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getDemandStableSupply();
        }

        public double getResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getResourceDemand resourcename is null or empty");
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

        public double GetRequiredResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: GetRequiredResourceDemand resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetRequiredResourceDemand();
        }

        public double GetCurrentUnfilledResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: GetRequiredResourceDemand resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetCurrentUnfilledResourceDemand();
        }

        public double GetPowerSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: GetPowerSupply resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.PowerSupply;
        }

        public double GetCurrentResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: GetCurrentResourceDemand resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentResourceDemand;
        }

        public double getResourceBarRatio(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getResourceBarRatio resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return (double)manager.ResourceBarRatioBegin;
        }

        public double getResourceBarFraction(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getResourceBarFraction resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceBarRatioBegin;
        }

        public double getSqrtResourceBarRatio(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getResourceBarRatio resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.SqrtResourceBarRatioBegin;
        }

        public double getResourceBarRatioEnd(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getResourceBarRatioEnd resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceBarRatioEnd;
        }

        public double getSpareResourceCapacity(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getSpareResourceCapacity resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getSpareResourceCapacity();
        }

        public double getResourceAvailability(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getResourceAvailability resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getResourceAvailability();
        }

        public double getTotalResourceCapacity(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) 
            {
                Debug.LogError("[KSPI]: getTotalResourceCapacity resourcename is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getTotalResourceCapacity();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor || resources_to_supply == null) return;

            part.OnJustAboutToBeDestroyed -= OnJustAboutToBeDestroyed;
            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;

            foreach (String resourcename in resources_to_supply)
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

        protected double TimeWarpFixedDeltaTime
        {
            get { return (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7); }
        }

        public override void OnFixedUpdate()
        {
            timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;

            updateCounter++;

            if (resources_to_supply == null) return;

            foreach (String resourcename in resources_to_supply)
            {
                var overmanager = getOvermanagerForResource(resourcename);

                ResourceManager resource_manager = null;;

                if (overmanager != null)
                    resource_manager = overmanager.getManagerForVessel(vessel);

                if (resource_manager == null)
                {
                    similarParts = null;
                    resource_manager = CreateResourceManagerForResource(resourcename);

                     Debug.Log("[KSPI]: ResourceSuppliableModule.OnFixedUpdate created Resourcemanager for Vessel " + vessel.GetName() + " for " + resourcename + " with ResourseManagerId " + resource_manager.Id + " with OvermanagerId" + resource_manager.Id);
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
            foreach (String resourcename in resources_to_supply)
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
