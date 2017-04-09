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
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            if (!fnresource_supplied.ContainsKey(resourcename))
                fnresource_supplied.Add(resourcename, 0);

            double power_taken = Math.Max(Math.Min(power, fnresource_supplied[resourcename] * TimeWarp.fixedDeltaTime), 0);
            fnresource_supplied[resourcename] -= power_taken;
            ORSResourceManager mega_manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

            mega_manager.powerDraw(this, power, power_taken);
            return power_taken;
        }

        public double supplyFNResource(double supply, String resourcename)
        {
            supply = Math.Max(supply, 0);
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.powerSupply(this, supply);
        }

        public double supplyFNResourceFixedMax(double supply, double maxsupply, String resourcename)
        {
            supply = Math.Max(supply, 0);
            maxsupply = Math.Max(maxsupply, 0);

            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.powerSupplyFixedMax(this, supply, maxsupply);
        }

        public double supplyManagedFNResource(double supply, String resourcename)
        {
            supply = Math.Max(supply, 0);
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.managedPowerSupply(this, supply);
        }

        public double supplyManagedFNResourceWithMinimumRatio(double supply, double ratio_min, String resourcename)
        {
            supply = Math.Max(supply, 0);
            ratio_min = Math.Max(ratio_min, 0);

            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
            {
                UnityEngine.Debug.LogWarning("did not find manager for vessel");
                return 0;
            }

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.managedPowerSupplyWithMinimumRatio(this, supply, ratio_min);
        }

        public double getCurrentResourceDemand(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.CurrentResourceDemand;
        }

        public double getStableResourceSupply(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.StableResourceSupply;
        }

        public double getCurrentHighPriorityResourceDemand(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.CurrentHighPriorityResourceDemand;
        }

        public double getResourceSupply(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.ResourceSupply;
        }

        public double getDemandSupply(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.getDemandSupply();
        }

        public double getDemandStableSupply(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.getDemandStableSupply();
        }

        public double getResourceDemand(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.ResourceDemand;
        }

        public double GetRequiredResourceDemand(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.GetRequiredResourceDemand();
        }

        public double getCurrentUnfilledResourceDemand(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.getCurrentUnfilledResourceDemand();
        }

        public double GetPowerSupply(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.PowerSupply;
        }

        public double GetCurrentResourceDemand(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.CurrentRresourceDemand;
        }

        public double getResourceBarRatio(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.ResourceBarRatio;
        }

        public double getSpareResourceCapacity(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.getSpareResourceCapacity();
        }

        public double getResourceAvailability(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            return manager.getResourceAvailability();
        }


        public double getTotalResourceCapacity(String resourcename)
        {
            if (!getOvermanagerForResource(resourcename).hasManagerForVessel(vessel))
                return 0;

            ORSResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);
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
