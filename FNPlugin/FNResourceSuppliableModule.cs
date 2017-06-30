using OpenResourceSystem;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin 
{
    abstract class FNResourceSuppliableModule : ORSResourceSuppliableModule
    {
        protected List<Part> similarParts;
        protected int partNrInList;

        public override void OnStart(PartModule.StartState state)
        {
            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;

            if (vessel != null && vessel.parts != null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            base.OnStart(state);
        }

        private void OnJustAboutToBeDestroyed()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            UnityEngine.Debug.LogWarning("[KSPI] - detecting supplyable part " + part.partInfo.title + " is being destroyed");

            var priority_manager = getSupplyPriorityManager(this.vessel);
            priority_manager.Unregister(this);
            if (priority_manager.processingPart == this)
                priority_manager = null;
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
     
        public override void OnFixedUpdateResourceSuppliable(float fixedDeltaTime)
        {
            // Nothing yet but do not remove
        }
    }
}
