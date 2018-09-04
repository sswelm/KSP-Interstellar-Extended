using System;
using System.Collections.Generic;
using UnityEngine;

namespace FNPlugin 
{
    public class ResourceOvermanager 
    {
        private static Dictionary<String, ResourceOvermanager> resources_managers = new Dictionary<String, ResourceOvermanager>();

        public Guid Id { get; private set; }

        public static void Reset()
        {
            resources_managers.Clear();
        }

        public static ResourceOvermanager getResourceOvermanagerForResource(String resource_name) 
        {
            ResourceOvermanager fnro = null;

            if (!resources_managers.TryGetValue(resource_name, out fnro))
            {
                fnro = new ResourceOvermanager(resource_name);
                if (resource_name == ResourceManager.FNRESOURCE_MEGAJOULES)
                    Debug.Log("[KSPI] - Created new ResourceOvermanager for resource " + resource_name + " with Id" + fnro.Id);
                resources_managers.Add(resource_name, fnro);
            }

            return fnro;
        }

        protected Dictionary<Vessel, ResourceManager> managers;
        protected String resource_name;

        public ResourceOvermanager(String name) 
        {
            Id = Guid.NewGuid();
            managers = new Dictionary<Vessel, ResourceManager>();
            this.resource_name = name;
        }

        public bool hasManagerForVessel(Vessel vess) 
        {
            return managers.ContainsKey(vess);
        }

        public ResourceManager getManagerForVessel(Vessel vess) 
        {
            if (vess == null)
                return null;

            ResourceManager manager;

            managers.TryGetValue(vess, out manager);

            return manager;
        }

        public void deleteManagerForVessel(Vessel vess) 
        {
            managers.Remove(vess);
        }

        public void deleteManager(ResourceManager manager) 
        {
            managers.Remove(manager.Vessel);
        }

        public ResourceManager CreateManagerForVessel(PartModule pm) 
        {
            ResourceManager resourcemanager = new ResourceManager(Id, pm, resource_name);
            managers.Add(pm.vessel, resourcemanager);
            return resourcemanager;
        }
    }
}
