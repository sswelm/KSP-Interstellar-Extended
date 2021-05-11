using KSP.UI.Screens;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class RDColoredUpgradeIcon : MonoBehaviour
    {
        RDNode selectedNode;

        readonly Color greenColor = new Color(0.717f, 0.819f, 0.561f);

        FieldInfo _fieldInfoRdPartList;

        public void Update()
        {
            // Do nothing if there is no PartList
            if (RDController.Instance == null || RDController.Instance.partList == null)
                return;

            // In case the node is deselected
            if (RDController.Instance.node_selected == null)
                selectedNode = null;

            // Do nothing if the tooltip hasn't changed since last update
            if (selectedNode == RDController.Instance.node_selected)
                return;

            // Get the the selected node and partList ui object
            selectedNode = RDController.Instance.node_selected;

            // retrieve fieldInfo type
            if (_fieldInfoRdPartList == null)
                _fieldInfoRdPartList = typeof(RDPartList).GetField("partListItems", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

            if (_fieldInfoRdPartList == null)
                return;

            var value = _fieldInfoRdPartList.GetValue(RDController.Instance.partList);

            var rdPartListItems = value as List<RDPartListItem>;
            if (rdPartListItems == null)
                return;

            var upgradedTemplateItems = rdPartListItems.Where(p => !p.isPart && p.upgrade != null).ToList();

            if (upgradedTemplateItems.Count == 0)
                return;

            foreach (var item in upgradedTemplateItems)
            {
                if (item == null || item.gameObject == null)
                    continue;

                var image = item.gameObject.GetComponent<UnityEngine.UI.Image>();

                if (image != null)
                    image.color = greenColor;
            }
        }
    }
}
