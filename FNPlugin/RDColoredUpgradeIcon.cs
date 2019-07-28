// This code is released under the [unlicense](http://unlicense.org/).
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class RDColoredUpgradeIcon : MonoBehaviour
    {
        RDNode selectedNode;

        Color greenColor = new Color(0.717f, 0.819f, 0.561f);  // light green RGB(183,209,143)

        FieldInfo fieldInfoRDPartList;

        public void Update()
        {
            // Do nothing if there is no PartList
            if (RDController.Instance == null || RDController.Instance.partList == null) return;

            // In case the node is deselected
            if (RDController.Instance.node_selected == null)
                selectedNode = null; 

            // Do nothing if the tooltip hasn't changed since last update
            if (selectedNode == RDController.Instance.node_selected) 
                return;

            // Get the the selected node and partlist ui object
            selectedNode = RDController.Instance.node_selected;

            // retrieve fieldInfo type
            if (fieldInfoRDPartList == null)
                fieldInfoRDPartList = typeof(RDPartList).GetField("partListItems", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

            var items = (List<RDPartListItem>)fieldInfoRDPartList.GetValue(RDController.Instance.partList);

            //if (items == null)
             //   Debug.LogError("[KSPI]: RDColoredUpgradeIcon fieldInfoRDPartList faild to retrieve partlist ");

            var upgradedTemplateItems = items.Where(p => !p.isPart && p.upgrade != null).ToList();

            //if (upgradedTemplateItems.Count == 0)
            //    Debug.LogError("[KSPI]: RDColoredUpgradeIcon upgradedTemplateItems is empty ");

            foreach (RDPartListItem item in upgradedTemplateItems)
            {
                //Debug.Log("[KSPI]: RDColoredUpgradeIcon upgrade name " + item.upgrade.name + " upgrade techRequired: " + item.upgrade.techRequired);

                item.gameObject.GetComponent<UnityEngine.UI.Image>().color = greenColor;
            }
        }
    }
}
