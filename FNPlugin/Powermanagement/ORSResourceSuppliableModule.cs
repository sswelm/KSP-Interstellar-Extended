using System;
using System.Collections.Generic;
using UnityEngine;

namespace FNPlugin
{
    public abstract class ORSResourceSuppliableModule : PartModule, ORSResourceSuppliable, IORSResourceSupplier
    {
        [KSPField(isPersistant = false, guiActive = false, guiName = "Update Counter")]
        public long updateCounter = 0;

        protected Dictionary<String, double> fnresource_supplied = new Dictionary<String, double>();
        protected String[] resources_to_supply;
        protected double timeWarpFixedDeltaTime;

        public void receiveFNResource(double power, String resourcename)
        {
            if (fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied[resourcename] = power;
            else
                fnresource_supplied.Add(resourcename, power);
        }

        public double consumeFNResource(double power_fixed, String resourcename)
        {
            power_fixed = Math.Max(power_fixed, 0);
            double fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);

            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            double power_taken_fixed = Math.Max(Math.Min(power_fixed, fnresource_supplied[resourcename] * fixedDeltaTime), 0);
            fnresource_supplied[resourcename] -= power_taken_fixed / fixedDeltaTime;
            manager.powerDrawFixed(this, power_fixed, power_taken_fixed);

            return power_taken_fixed;
        }

        public double consumeFNResourcePerSecond(double power_per_second, String resourcename, ORSResourceManager manager = null)
        {
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

        public double supplyFNResourceFixed(double supply, String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixed(this, Math.Max(supply, 0));
        }

        public double supplyFNResourcePerSecond(double supply, String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double supplyFNResourceFixedWithMax(double supply, double maxsupply, String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyFixedWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyFNResourcePerSecondWithMax(double supply, double maxsupply, String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.powerSupplyPerSecondWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyManagedFNResourcePerSecond(double supply, String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.managedPowerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double getNeededPowerSupplyPerSecondWithMinimumRatio(double supply, double ratio_min, String resourcename, ORSResourceManager manager = null)
        {
            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                Debug.LogError("[KSPI] - failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.getNeededPowerSupplyPerSecondWithMinimumRatio(Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double supplyManagedFNResourcePerSecondWithMinimumRatio(double supply, double ratio_min, String resourcename, ORSResourceManager manager = null)
        {
            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;
            
            return manager.managedPowerSupplyPerSecondWithMinimumRatio(this, Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

		public double managedProvidedPowerSupplyPerSecondMinimumRatio(double requested_power, double maximum_power, double ratio_min, String resourcename, ORSResourceManager manager = null)
        {
            if (manager == null)
                manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            var result = manager.managedRequestedPowerSupplyPerSecondMinimumRatio(this, requested_power, Math.Max(maximum_power, 0), Math.Max(ratio_min, 0));

	        return result.currentProvided;
        }

		public PowerGenerated managedPowerSupplyPerSecondMinimumRatio(double requested_power, double maximum_power, double ratio_min, String resourcename, ORSResourceManager manager = null)
		{
			if (manager == null)
				manager = getManagerForVessel(resourcename);
			if (manager == null)
				return null;

			return manager.managedRequestedPowerSupplyPerSecondMinimumRatio(this, requested_power, Math.Max(maximum_power, 0), Math.Max(ratio_min, 0));
		}

        public double getCurrentResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentResourceDemand;
        }

        public double getStableResourceSupply(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.StableResourceSupply;
        }

        public double getCurrentHighPriorityResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentHighPriorityResourceDemand;
        }

        public double getAvailableResourceSupply(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.CurrentHighPriorityResourceDemand, 0);
        }

        public double getResourceSupply(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceSupply;
        }

        public double GetOverproduction(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getOverproduction();
        }

        public double getDemandStableSupply(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getDemandStableSupply();
        }

        public double getResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceDemand;
        }

        public double GetRequiredResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetRequiredResourceDemand();
        }

        public double GetCurrentUnfilledResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetCurrentUnfilledResourceDemand();
        }

        public double GetPowerSupply(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.PowerSupply;
        }

        public double GetCurrentResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentRresourceDemand;
        }

        public double getResourceBarRatio(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceBarRatio;
        }

        public double getSpareResourceCapacity(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getSpareResourceCapacity();
        }

        public double getResourceAvailability(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getResourceAvailability();
        }

        public double getTotalResourceCapacity(String resourcename)
        {
            ORSResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.getTotalResourceCapacity();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor || resources_to_supply == null) return;

            foreach (String resourcename in resources_to_supply)
            {
                ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

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

        protected double TimeWarpFixedDeltaTime
        {
            get { return (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7); }
        }

        public override void OnFixedUpdate()
        {
            timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;

            try
            {
                updateCounter++;

                if (resources_to_supply == null) return;

                foreach (String resourcename in resources_to_supply)
                {
                    ORSResourceManager resource_manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                    if (resource_manager == null)
                    {
                        resource_manager = createResourceManagerForResource(resourcename);
                        Debug.Log("[KSPI] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
                    }

                    if (resource_manager.PartModule == null || resource_manager.PartModule.vessel != this.vessel || resource_manager.Counter < updateCounter )
                        resource_manager.UpdatePartModule(this);

                    if (resource_manager.PartModule == this)
                        resource_manager.update(updateCounter);
                }

                var priority_manager = getSupplyPriorityManager(this.vessel);
                if (priority_manager != null && priority_manager.ProcessingPart == null || priority_manager.ProcessingPart.vessel != this.vessel || priority_manager.Counter < updateCounter)
                {
                    priority_manager.UpdatePartModule(this);
                }

                if (priority_manager != null && priority_manager.ProcessingPart == this)
                    priority_manager.UpdateResourceSuppliables(updateCounter, timeWarpFixedDeltaTime);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in ORSResourceSuppliableModule.OnFixedUpdate " + e.Message);
                throw;
            }
        }

        public void RemoveItselfAsManager()
        {
            foreach (String resourcename in resources_to_supply)
            {
                ORSResourceManager resource_manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                if (resource_manager != null && resource_manager.PartModule == this)
                    resource_manager.UpdatePartModule(null);
            }
        }

        public virtual string getResourceManagerDisplayName()
        {
            return ClassName;
        }

        public virtual int getPowerPriority()
        {
            return 2;
        }

        protected virtual ORSResourceManager createResourceManagerForResource(string resourcename)
        {
            return getOvermanagerForResource(resourcename).createManagerForVessel(this);
        }

        protected virtual ORSResourceOvermanager getOvermanagerForResource(string resourcename)
        {
            return ORSResourceOvermanager.getResourceOvermanagerForResource(resourcename);
        }

        protected virtual ORSResourceManager getManagerForVessel(string resourcename)
        {
            return getManagerForVessel(resourcename, vessel);
        }

        protected virtual ORSResourceManager getManagerForVessel(string resourcename, Vessel vessel)
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

        public abstract void OnFixedUpdateResourceSuppliable(double fixedDeltaTime);
    }
}
