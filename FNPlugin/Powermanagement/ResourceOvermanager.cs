using System;
using System.Collections.Generic;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    public class ResourceOvermanager
    {
        private static readonly Dictionary<string, ResourceOvermanager> resourceOverManagers = new Dictionary<string, ResourceOvermanager>();

        public static ResourceOvermanager GetResourceOvermanagerForResource(string resource_name)
        {
            if (resourceOverManagers.TryGetValue(resource_name, out ResourceOvermanager fnro))
                return fnro;

            fnro = new ResourceOvermanager(resource_name);
            Debug.Log("[KSPI]: Created new ResourceOvermanager for resource " + resource_name + " with ID " + fnro.Id);
            resourceOverManagers.Add(resource_name, fnro);

            return fnro;
        }

        public static void Reset()
        {
            resourceOverManagers.Clear();
        }

        public static void ResetForVessel(Vessel vess)
        {
            foreach (var pair in resourceOverManagers)
                pair.Value.DeleteManagerForVessel(vess);
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

        public ResourceManager CreateManagerForVessel(ResourceSuppliableModule pm)
        {
            var resourceManager = ResourceManagerFactory.Create(Id, pm, resourceName);
            managers.Add(pm.vessel, resourceManager);
            return resourceManager;
        }

        public void DeleteManagerForVessel(Vessel vess)
        {
            managers.Remove(vess);
        }

        public void DeleteManager(ResourceManager manager)
        {
            managers.Remove(manager.Vessel);
        }

        public ResourceManager GetManagerForVessel(Vessel vess)
        {
            if (vess == null)
                return null;

            managers.TryGetValue(vess, out ResourceManager manager);
            return manager;
        }

        public bool HasManagerForVessel(Vessel vess)
        {
            return managers.ContainsKey(vess);
        }
    }
}
