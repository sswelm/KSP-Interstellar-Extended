using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem
{
    public abstract class ORSResourceSuppliableModule : PartModule, ORSResourceSuppliable, IORSResourceSupplier
    {
        protected Dictionary<String, double> fnresource_supplied = new Dictionary<String, double>();
        protected String[] resources_to_supply;

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

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            double power_taken_fixed = Math.Max(Math.Min(power_fixed, fnresource_supplied[resourcename] * TimeWarp.fixedDeltaTime), 0);
            fnresource_supplied[resourcename] -= power_taken_fixed / TimeWarp.fixedDeltaTime;
            
            manager.powerDrawFixed(this, power_fixed, power_taken_fixed);

            return power_taken_fixed;
        }

        public double consumeFNResourcePerSecond(double power_per_second, String resourcename)
        {
            power_per_second = Math.Max(power_per_second, 0);

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            double power_taken_per_second = Math.Max(Math.Min(power_per_second, fnresource_supplied[resourcename]), 0);
            fnresource_supplied[resourcename] -= power_taken_per_second;

            manager.powerDrawPerSecond(this, power_per_second, power_taken_per_second);

            return power_taken_per_second;
        }

        public double supplyFNResourceFixed(double supply, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.powerSupplyFixed(this, Math.Max(supply, 0));
        }

        public double supplyFNResourcePerSecond(double supply, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.powerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double supplyFNResourceFixedWithMax(double supply, double maxsupply, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.powerSupplyFixedWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyFNResourcePerSecondWithMax(double supply, double maxsupply, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.powerSupplyPerSecondWithMax(this, Math.Max(supply, 0), Math.Max(maxsupply, 0));
        }

        public double supplyManagedFNResourceFixed(double supply, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.managedPowerSupplyFixed(this, Math.Max(supply, 0));
        }

        public double supplyManagedFNResourcePerSecond(double supply, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.managedPowerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double supplyManagedFNResourceFixedWithMinimumRatio(double supply, double ratio_min, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.managedPowerSupplyFixedWithMinimumRatio(this, Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double supplyManagedFNResourcePerSecondWithMinimumRatio(double supply, double ratio_min, String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }
            
            return manager.managedPowerSupplyPerSecondWithMinimumRatio(this, Math.Max(supply, 0), Math.Max(ratio_min, 0));
        }

        public double getCurrentResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.CurrentResourceDemand;
        }

        public double getStableResourceSupply(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.StableResourceSupply;
        }

        public double getCurrentHighPriorityResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.CurrentHighPriorityResourceDemand;
        }

        public double getResourceSupply(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.ResourceSupply;
        }

        public double GetOverproduction(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.getOverproduction();
        }

        public double getDemandStableSupply(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.getDemandStableSupply();
        }

        public double getResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.ResourceDemand;
        }

        public double GetRequiredResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.GetRequiredResourceDemand();
        }

        public double GetCurrentUnfilledResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.GetCurrentUnfilledResourceDemand();
        }

        public double GetPowerSupply(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.PowerSupply;
        }

        public double GetCurrentResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.CurrentRresourceDemand;
        }

        public double getResourceBarRatio(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.ResourceBarRatio;
        }

        public double getSpareResourceCapacity(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.getSpareResourceCapacity();
        }

        public double getResourceAvailability(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.getResourceAvailability();
        }


        public double getTotalResourceCapacity(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

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

                    print("[ORS] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
                }
            }

            getSupplyPriorityManager(this.vessel).Register(this);
        }

        public override void OnFixedUpdate()
        {
            if (resources_to_supply == null) return;

            foreach (String resourcename in resources_to_supply)
            {
                ORSResourceManager resource_manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                if (resource_manager == null)
                {
                    resource_manager = createResourceManagerForResource(resourcename);
                    print("[ORS] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
                }

                if (resource_manager.PartModule == null || resource_manager.PartModule.vessel != this.vessel || resource_manager.IsUpdatedAtLeastOnce == false)
                {
                    resource_manager.updatePartModule(this);
                    print("[ORS] Updated PartModule of Manager for " + resourcename + "  to " + this.part.partInfo.title);
                }

                if (resource_manager.PartModule == this)
                    resource_manager.update();
            }

            var priority_manager = getSupplyPriorityManager(this.vessel);
            if (priority_manager.pocessingPart == null || priority_manager.pocessingPart.vessel != this.vessel)
            {
                priority_manager.pocessingPart = this;
            }

            if (priority_manager.pocessingPart == this)
                priority_manager.UpdateResourceSuppliables(TimeWarp.fixedDeltaTime);
        }

        public void RemoveItselfAsManager()
        {
            foreach (String resourcename in resources_to_supply)
            {
                ORSResourceManager resource_manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                if (resource_manager != null && resource_manager.PartModule == this)
                    resource_manager.updatePartModule(null);
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

        protected virtual SupplyPriorityManager getSupplyPriorityManager(Vessel vessel)
        {
            return SupplyPriorityManager.GetSupplyPriorityManagerForVessel(vessel);
        }

        public abstract void OnFixedUpdateResourceSuppliable(float fixedDeltaTime);
    }
}
