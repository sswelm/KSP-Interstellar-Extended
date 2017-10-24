using System;
using System.Collections.Generic;

namespace FNPlugin 
{
    public class ResourceOvermanager 
    {
        protected static Dictionary<String, ResourceOvermanager> resources_managers = new Dictionary<String, ResourceOvermanager>();
        protected Dictionary<Vessel, ResourceManager> managers;
        protected String resource_name;

        public static ResourceOvermanager getResourceOvermanagerForResource(String resource_name) 
        {
            ResourceOvermanager fnro;

            if (!resources_managers.TryGetValue(resource_name, out fnro))
            {
                fnro = new ResourceOvermanager(resource_name);
                resources_managers.Add(resource_name, fnro);
            }

            return fnro;
        }

	    public ResourceOvermanager(String name) 
        {
            managers = new Dictionary<Vessel, ResourceManager>();
            this.resource_name = name;
        }

        public bool hasManagerForVessel(Vessel vess) 
        {
            return managers.ContainsKey(vess);
        }

        public ResourceManager getManagerForVessel(Vessel vess) 
        {
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

        public virtual ResourceManager createManagerForVessel(PartModule pm) 
        {
            ResourceManager megamanager = new ResourceManager(pm, resource_name);
            managers.Add(pm.vessel, megamanager);
            return megamanager;
        }
    }
}
