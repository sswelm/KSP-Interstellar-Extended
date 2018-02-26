using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace FNPlugin
{
    abstract class ResourceSuppliableModule : PartModule, IResourceSuppliable, IResourceSupplier
    {
        [KSPField(isPersistant = false, guiActive = false, guiName = "Update Counter")]
        public long updateCounter = 0;

        protected List<Part> similarParts;
        protected int partNrInList;
        protected Dictionary<String, double> fnresource_supplied = new Dictionary<String, double>();
        protected String[] resources_to_supply;
        protected double timeWarpFixedDeltaTime;

        public void receiveFNResource(double power, String resourcename)
        {
            if (double.IsNaN(power) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - receiveFNResource illegal values.");
                return;
            }

            if (fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied[resourcename] = power;
            else
                fnresource_supplied.Add(resourcename, power);
        }

        public double consumeFNResource(double power_fixed, String resourcename, double fixedDeltaTime = 0)
        {
            if (double.IsNaN(power_fixed) || double.IsNaN(fixedDeltaTime) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - consumeFNResource illegal values.");
                return 0;
            }

            power_fixed = Math.Max(power_fixed, 0);

            var manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            fixedDeltaTime = fixedDeltaTime > 0 ? fixedDeltaTime : (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);
            double power_taken_fixed = Math.Max(Math.Min(power_fixed, fnresource_supplied[resourcename] * fixedDeltaTime), 0);
            fnresource_supplied[resourcename] -= power_taken_fixed / fixedDeltaTime;
            manager.powerDrawFixed(this, power_fixed, power_taken_fixed);

            return power_taken_fixed;
        }

        public double consumeFNResourcePerSecond(double power_per_second, String resourcename, ResourceManager manager = null)
        {
            if (double.IsNaN(power_per_second) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - consumeFNResourcePerSecond illegal values.");
                return 0;
            }

            power_per_second = Math.Max(power_per_second, 0);

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            double power_taken_per_second = Math.Max(Math.Min(power_per_second, fnresource_supplied[resourcename]), 0);
            fnresource_supplied[resourcename] -= power_taken_per_second;

            manager.powerDrawPerSecond(this, power_per_second, power_taken_per_second);

            return power_taken_per_second;
        }

        public double consumeFNResourcePerSecondBuffered(double requestedPowerPerSecond, String resourcename, double limitBarRatio = 0.1, ResourceManager manager = null)
        {
            if (double.IsNaN(requestedPowerPerSecond) || String.IsNullOrEmpty(resourcename))
            {
                Debug.Log("[KSPI] - consumeFNResourcePerSecond illegal values.");
                return 0;
            }

            requestedPowerPerSecond = Math.Max(requestedPowerPerSecond, 0);

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            var avialablePower = fnresource_supplied[resourcename];
            var power_taken_per_second = Math.Max(Math.Min(requestedPowerPerSecond, avialablePower), 0);
            fnresource_supplied[resourcename] -= power_taken_per_second;

            // supplement with buffer power if needed and available
            var powerShortage = requestedPowerPerSecond - avialablePower;
            if (powerShortage > 0)
            {
                var currentCapacity = manager.getTotalResourceCapacity();
                var currentAmount = currentCapacity * manager.ResourceBarRatioBegin;
                var fixedPowerShortage = powerShortage * timeWarpFixedDeltaTime;

                if (currentAmount - fixedPowerShortage > currentCapacity * limitBarRatio)
                    power_taken_per_second += part.RequestResource(resourcename, fixedPowerShortage) / timeWarpFixedDeltaTime;
            }

            manager.powerDrawPerSecond(this, requestedPowerPerSecond, power_taken_per_second);

            return power_taken_per_second;
        }

        public double supplyFNResourceFixed(double supply, String resourcename)
        {
            if (double.IsNaN(supply) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - supplyFNResourceFixed illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixed(this, Math.Max(supply, 0));
        }

        public double supplyFNResourcePerSecond(double supply, String resourcename)
        {
            if (double.IsNaN(supply) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - supplyFNResourcePerSecond illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double supplyFNResourceFixedWithMax(double supply, double maxsupply, String resourcename)
        {
            if (double.IsNaN(supply) || double.IsNaN(maxsupply) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - supplyFNResourceFixedWithMax illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixedWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyFNResourcePerSecondWithMax(double supply, double maxsupply, String resourcename)
        {
            if (double.IsNaN(supply) || double.IsNaN(maxsupply) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - supplyFNResourcePerSecondWithMax illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecondWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyManagedFNResourcePerSecond(double supply, String resourcename)
        {
            if (double.IsNaN(supply) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - supplyManagedFNResourcePerSecond illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.managedPowerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double getNeededPowerSupplyPerSecondWithMinimumRatio(double supply, double ratio_min, String resourcename, ResourceManager manager = null)
        {
            if (double.IsNaN(supply) || double.IsNaN(ratio_min) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getNeededPowerSupplyPerSecondWithMinimumRatio illegal values.");
                return 0;
            }

            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                Debug.LogError("[KSPI] - failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.getNeededPowerSupplyPerSecondWithMinimumRatio(Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double supplyManagedFNResourcePerSecondWithMinimumRatio(double supply, double ratio_min, String resourcename, ResourceManager manager = null)
        {
            if (double.IsNaN(supply) || double.IsNaN(ratio_min) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - supplyManagedFNResourcePerSecondWithMinimumRatio illegal values.");
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
            if (double.IsNaN(requested_power) || double.IsNaN(maximum_power) || double.IsNaN(ratio_min) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - managedProvidedPowerSupplyPerSecondMinimumRatio illegal values.");
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
            if (double.IsNaN(requested_power) || double.IsNaN(maximum_power) || double.IsNaN(ratio_min) || String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - managedPowerSupplyPerSecondMinimumRatio illegal values.");
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
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getCurrentResourceDemand illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentResourceDemand;
        }

        public double getStableResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getCurrentResourceDemand illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.StableResourceSupply;
        }

        public double getCurrentHighPriorityResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getCurrentHighPriorityResourceDemand illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentHighPriorityResourceDemand;
        }

        public double getAvailableResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getAvailableResourceSupply illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.CurrentHighPriorityResourceDemand, 0);
        }

        public double getResourceSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getResourceSupply illegal values.");
                return 0;
            }
            
            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceSupply;
        }

        public double GetOverproduction(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - GetOverproduction illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getOverproduction();
        }

        public double getDemandStableSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getDemandStableSupply illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getDemandStableSupply();
        }

        public double getResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getResourceDemand illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceDemand;
        }

        public double GetRequiredResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - GetRequiredResourceDemand illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetRequiredResourceDemand();
        }

        public double GetCurrentUnfilledResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - GetRequiredResourceDemand illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetCurrentUnfilledResourceDemand();
        }

        public double GetPowerSupply(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - GetPowerSupply illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.PowerSupply;
        }

        public double GetCurrentResourceDemand(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - GetCurrentResourceDemand illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentResourceDemand;
        }

        public double getResourceBarRatio(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getResourceBarRatio illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceBarRatioBegin;
        }

        public double getResourceBarRatioEnd(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getResourceBarRatioEnd illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceBarRatioEnd;
        }

        public double getSpareResourceCapacity(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getSpareResourceCapacity illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getSpareResourceCapacity();
        }

        public double getResourceAvailability(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getResourceAvailability illegal values.");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getResourceAvailability();
        }

        public double getTotalResourceCapacity(String resourcename)
        {
            if (String.IsNullOrEmpty(resourcename)) {
                Debug.Log("[KSPI] - getTotalResourceCapacity illegal values.");
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

            if (vessel != null && vessel.parts != null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            foreach (String resourcename in resources_to_supply)
            {
                ResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                if (manager == null)
                {
                    manager = createResourceManagerForResource(resourcename);

                    print("[KSPI] - Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
                }
            }

            var priorityManager = getSupplyPriorityManager(this.vessel);
            if (priorityManager != null)
                priorityManager.Register(this);
        }

        private void OnJustAboutToBeDestroyed()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            UnityEngine.Debug.LogWarning("[KSPI] - detecting supplyable part " + part.partInfo.title + " is being destroyed");

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

                ResourceManager resource_manager =  overmanager != null ? overmanager.getManagerForVessel(vessel) : null;

                if (resource_manager == null)
                {
                    resource_manager = createResourceManagerForResource(resourcename);
                    Debug.Log("[KSPI] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
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
                ResourceManager resource_manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                if (resource_manager != null && resource_manager.PartModule == this)
                    resource_manager.UpdatePartModule(null);
            }
        }

        public virtual string getResourceManagerDisplayName()
        {
            string displayName = part.partInfo.title;
            if (similarParts != null && similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }

        public virtual int getPowerPriority()
        {
            return 2;
        }

        protected virtual ResourceManager createResourceManagerForResource(string resourcename)
        {
            return getOvermanagerForResource(resourcename).createManagerForVessel(this);
        }

        protected virtual ResourceOvermanager getOvermanagerForResource(string resourcename)
        {
            return ResourceOvermanager.getResourceOvermanagerForResource(resourcename);
        }

        protected virtual ResourceManager getManagerForVessel(string resourcename)
        {
            return getManagerForVessel(resourcename, vessel);
        }

        protected virtual ResourceManager getManagerForVessel(string resourcename, Vessel vessel)
        {
            var overmanager = getOvermanagerForResource(resourcename);
            if (overmanager == null)
            {
                Debug.LogError("[KSPI] - failed to find Overmanager");
                return null;
            }
            return overmanager.getManagerForVessel(vessel);
        }

        protected virtual SupplyPriorityManager getSupplyPriorityManager(Vessel vessel)
        {
            return SupplyPriorityManager.GetSupplyPriorityManagerForVessel(vessel);
        }

        public virtual void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {

        }
    }
}
