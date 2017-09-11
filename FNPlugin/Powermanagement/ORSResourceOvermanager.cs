using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    public class ORSResourceOvermanager 
    {
        protected static Dictionary<String, ORSResourceOvermanager> resources_managers = new Dictionary<String, ORSResourceOvermanager>();

        public static ORSResourceOvermanager getResourceOvermanagerForResource(String resource_name) 
        {
            ORSResourceOvermanager fnro;

            if (!resources_managers.TryGetValue(resource_name, out fnro))
            {
                fnro = new ORSResourceOvermanager(resource_name);
                resources_managers.Add(resource_name, fnro);
            }

            return fnro;
        }

        protected Dictionary<Vessel, ORSResourceManager> managers;
        protected String resource_name;

        public ORSResourceOvermanager() {}

        public ORSResourceOvermanager(String name) 
        {
            managers = new Dictionary<Vessel, ORSResourceManager>();
            this.resource_name = name;
        }

        public bool hasManagerForVessel(Vessel vess) 
        {
            return managers.ContainsKey(vess);
        }

        public ORSResourceManager getManagerForVessel(Vessel vess) 
        {
            ORSResourceManager manager;

            managers.TryGetValue(vess, out manager);

            return manager;
        }

        public void deleteManagerForVessel(Vessel vess) 
        {
            managers.Remove(vess);
        }

        public void deleteManager(ORSResourceManager manager) 
        {
            managers.Remove(manager.Vessel);
        }

        public virtual ORSResourceManager createManagerForVessel(PartModule pm) 
        {
            ORSResourceManager megamanager = new ORSResourceManager(pm, resource_name);
            managers.Add(pm.vessel, megamanager);
            return megamanager;
        }
    }
}
