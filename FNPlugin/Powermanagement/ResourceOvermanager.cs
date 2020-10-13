using System;
using System.Collections.Generic;
using UnityEngine;

namespace FNPlugin 
{
    public class ResourceOvermanager 
    {
        private static readonly Dictionary<string, ResourceOvermanager> resourceOverManagers = new Dictionary<string, ResourceOvermanager>();

        public static ResourceOvermanager getResourceOvermanagerForResource(string resource_name) 
        {
            if (!resourceOverManagers.TryGetValue(resource_name, out ResourceOvermanager fnro))
            {
                fnro = new ResourceOvermanager(resource_name);
                Debug.Log("[KSPI]: Created new ResourceOvermanager for resource " + resource_name + " with ID " + fnro.Id);
                resourceOverManagers.Add(resource_name, fnro);
            }

            return fnro;
        }

        public static void Reset()
        {
            resourceOverManagers.Clear();
        }

        public static void ResetForVessel(Vessel vess)
        {
            foreach (var pair in resourceOverManagers)
                pair.Value.deleteManagerForVessel(vess);
        }

        public Guid Id { get; }

        protected readonly IDictionary<Vessel, ResourceManager> managers;
        protected string resourceName;

        public ResourceOvermanager(string name) 
        {
            Id = Guid.NewGuid();
            managers = new Dictionary<Vessel, ResourceManager>();
            resourceName = name;
        }

        public ResourceManager CreateManagerForVessel(PartModule pm)
        {
            var resourcemanager = ResourceManagerFactory.Create(Id, pm, resourceName);
            managers.Add(pm.vessel, resourcemanager);
            return resourcemanager;
        }

        public void deleteManagerForVessel(Vessel vess)
        {
            managers.Remove(vess);
        }

        public void deleteManager(ResourceManager manager)
        {
            managers.Remove(manager.Vessel);
        }

        public ResourceManager getManagerForVessel(Vessel vess) 
        {
            if (vess == null)
                return null;

            managers.TryGetValue(vess, out ResourceManager manager);
            return manager;
        }

        public bool hasManagerForVessel(Vessel vess)
        {
            return managers.ContainsKey(vess);
        }
    }
}
