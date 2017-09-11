using System;
using System.Collections.Generic;

namespace FNPlugin 
{
    public class FNResourceOvermanager : ORSResourceOvermanager 
    {

        public static new FNResourceOvermanager getResourceOvermanagerForResource(String resource_name) 
        {
            FNResourceOvermanager fnro;
            ORSResourceOvermanager orsResourcManager;

            if (!ORSResourceOvermanager.resources_managers.TryGetValue(resource_name, out orsResourcManager))
            {
                fnro = new FNResourceOvermanager(resource_name);
                ORSResourceOvermanager.resources_managers.Add(resource_name, fnro);
            }
            else
                fnro = (FNResourceOvermanager)orsResourcManager;

            return fnro;
        }

        public FNResourceOvermanager(String name) 
        {
            managers = new Dictionary<Vessel, ORSResourceManager>();
            this.resource_name = name;
        }

        public override ORSResourceManager createManagerForVessel(PartModule pm) 
        {
            FNResourceManager megamanager = new FNResourceManager(pm, resource_name);
            managers.Add(pm.vessel, megamanager);
            return megamanager;
        }

    }
}
