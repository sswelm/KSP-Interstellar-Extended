using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    class SupplyPriorityManager
    {
        private static readonly Dictionary<Vessel, SupplyPriorityManager> SupplyPriorityManagers = new Dictionary<Vessel, SupplyPriorityManager>();

        public Guid Id { get; }
        public Vessel Vessel { get; private set; }
        public PartModule ProcessingPart { get; private set; }

		private readonly List<ResourceSuppliableModule> _suppliableModules = new List<ResourceSuppliableModule>();

        public static void Reset()
        {
            SupplyPriorityManagers.Clear();
        }

        public static SupplyPriorityManager GetSupplyPriorityManagerForVessel(Vessel vessel)
        {
            if (vessel == null)
                return null;

            if (SupplyPriorityManagers.TryGetValue(vessel, out var manager))
                return manager;

            Debug.Log("[KSPI]: Creating new supply priority manager for " + vessel.GetName());
            manager = new SupplyPriorityManager(vessel);

            SupplyPriorityManagers.Add(vessel, manager);
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
            if (!_suppliableModules.Contains(suppliable))
            {
                _suppliableModules.Add(suppliable);
            }
        }

        public void Unregister(ResourceSuppliableModule suppliable)
        {
            if (!_suppliableModules.Contains(suppliable))
            {
                _suppliableModules.Remove(suppliable);
            }
        }

        public long Counter { get; private set; }

        public void UpdateResourceSuppliables(long  updateCounter, double fixedDeltaTime)
        {
            try
            {
                Counter = updateCounter;

                var suppliableModulesPrioritized = _suppliableModules.Where(m => m != null).OrderBy(m => m.GetSupplyPriority()).ToList();

                suppliableModulesPrioritized.ForEach(s => s.OnFixedUpdateResourceSuppliable(fixedDeltaTime));

                suppliableModulesPrioritized.ForEach(s => s.OnPostResourceSuppliable(fixedDeltaTime));
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in SupplyPriorityManager.UpdateResourceSuppliables " + e.Message);
                throw;
            }
        }
    }
}
