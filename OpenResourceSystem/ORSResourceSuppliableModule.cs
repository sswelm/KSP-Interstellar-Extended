using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem
{
    public abstract class ORSResourceSuppliableModule : PartModule, ORSResourceSuppliable, IORSResourceSupplier
    {
        protected Dictionary<String, double> fnresource_supplied = new Dictionary<String, double>();
        protected Dictionary<String, ORSResourceManager> fnresource_managers = new Dictionary<String, ORSResourceManager>();
        protected String[] resources_to_supply;

        public void receiveFNResource(double power, String resourcename)
        {
            if (fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied[resourcename] = power;
            else
                fnresource_supplied.Add(resourcename, power);
        }

        public double consumeFNResource(double power, String resourcename)
        {
            power = Math.Max(power, 0);

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            double power_taken = Math.Max(Math.Min(power, fnresource_supplied[resourcename] * TimeWarp.fixedDeltaTime), 0);
            fnresource_supplied[resourcename] -= power_taken;
            
            manager.powerDrawFixed(this, power, power_taken);

            return power_taken;
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

        public double getDemandSupply(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.getDemandSupply();
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

        public double getCurrentUnfilledResourceDemand(String resourcename)
        {
            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            if (manager == null)
            {
                UnityEngine.Debug.LogWarning("ORS - did not find manager for vessel");
                return 0;
            }

            return manager.getCurrentUnfilledResourceDemand();
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
                ORSResourceManager manager;

                if (getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                {
                    manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
                    if (manager == null)
                    {
                        manager = createResourceManagerForResource(resourcename);
                        print("[ORS] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
                    }
                }
                else
                {
                    manager = createResourceManagerForResource(resourcename);

                    print("[ORS] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
                }
            }
        }

        public override void OnFixedUpdate()
        {
            if (resources_to_supply == null) return;

            foreach (String resourcename in resources_to_supply)
            {
                ORSResourceOvermanager overmanager = getOvermanagerForResource(resourcename);
                ORSResourceManager manager = overmanager.getManagerForVessel(vessel);

                if (manager == null)
                {
                    manager = createResourceManagerForResource(resourcename);
                    print("[ORS] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
                }

                if (manager.PartModule == null || manager.PartModule.vessel != this.vessel || manager.IsUpdatedAtLeastOnce == false)
                {
                    manager.updatePartModule(this);
                    print("[ORS] Updated PartModule of Manager for " + resourcename + "  to " + this.part.partInfo.title);
                }

                if (manager.PartModule == this)
                    manager.update();
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
    }
}
