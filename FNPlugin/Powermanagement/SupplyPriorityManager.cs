using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class SupplyPriorityManager
    {
        private static Dictionary<Vessel, SupplyPriorityManager> supply_priority_managers = new Dictionary<Vessel, SupplyPriorityManager>();

        public Guid Id { get; private set; }
        public Vessel Vessel { get; private set; }
        public PartModule ProcessingPart { get; private set; }

		private List<ResourceSuppliableModule> suppliable_modules = new List<ResourceSuppliableModule>();

        public static void Reset()
        {
            supply_priority_managers.Clear();
        }

        public static SupplyPriorityManager GetSupplyPriorityManagerForVessel(Vessel vessel) 
        {
            if (vessel == null)
                return null;

            if (!supply_priority_managers.TryGetValue(vessel, out var manager))
            {
                Debug.Log("[KSPI]: Creating new supply priority manager for " + vessel.GetName());
                manager = new SupplyPriorityManager(vessel);
                
                supply_priority_managers.Add(vessel, manager);
            }
            return manager;
        }

        public void UpdatePartModule(PartModule partModule)
        {
            Vessel = partModule.vessel;
            ProcessingPart = partModule;
        }

        public SupplyPriorityManager(Vessel vessel)
        {
            Id = Guid.NewGuid();
            Vessel = vessel;
        }

        public void Register(ResourceSuppliableModule suppliable)
        {
            if (!suppliable_modules.Contains(suppliable))
            {
                suppliable_modules.Add(suppliable);
            }
        }

        public void Unregister(ResourceSuppliableModule suppliable)
        {
            if (!suppliable_modules.Contains(suppliable))
            {
                suppliable_modules.Remove(suppliable);
            }
        }

        public long Counter { get; private set; }

        public void UpdateResourceSuppliables(long  updateCounter, double fixedDeltaTime)
        {
            try
            {
                Counter = updateCounter;

                var suppliable_modules_prioritized = suppliable_modules.Where(m => m != null).OrderBy(m => m.getSupplyPriority()).ToList();

                suppliable_modules_prioritized.ForEach(s => s.OnFixedUpdateResourceSuppliable(fixedDeltaTime));

                suppliable_modules_prioritized.ForEach(s => s.OnPostResourceSuppliable(fixedDeltaTime));
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in SupplyPriorityManager.UpdateResourceSuppliables " + e.Message);
                throw;
            }
        }
       
    }

}
