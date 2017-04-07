using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    abstract class FNResourceSuppliableModule : ORSResourceSuppliableModule
    {
        protected List<Part> similarParts;
        protected int partNrInList;

        public override void OnStart(PartModule.StartState state)
        {
            if (vessel != null && vessel.parts != null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            base.OnStart(state);
        }

        protected override ORSResourceManager createResourceManagerForResource(string resourcename) 
        {
            return getOvermanagerForResource(resourcename).createManagerForVessel(this);
        }

        protected override ORSResourceOvermanager getOvermanagerForResource(string resourcename) 
        {
            return FNResourceOvermanager.getResourceOvermanagerForResource(resourcename);
        }

        public override string getResourceManagerDisplayName()
        {
            string displayName = part.partInfo.title;
            if (similarParts != null && similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }
    }
}
