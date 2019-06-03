using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FNPlugin.Extensions
{
    public static class PartExtensions
    {
        public static IEnumerable<PartResource> GetConnectedResources(this Part part, String resourcename)
        {
            return part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == resourcename));
        }

        public static void GetResourceMass(this Part part, PartResourceDefinition definition,  out double spareRoomMass, out double maximumMass) 
        {
            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, out currentAmount, out maxAmount);

            maximumMass = maxAmount * (double)(decimal)definition.density;
            spareRoomMass = (maxAmount - currentAmount) * (double)(decimal)definition.density;
        }

        public static double GetResourceSpareCapacity(this Part part, String resourcename)
        {
            var resourcDdefinition = PartResourceLibrary.Instance.GetDefinition(resourcename);
            if (resourcDdefinition == null)
                return 0;

            double currentAmount;
            double maxAmount;

            part.GetConnectedResourceTotals(resourcDdefinition.id, out currentAmount, out maxAmount);

            return maxAmount - currentAmount;
        }

        public static double GetResourceSpareCapacity(this Part part, PartResourceDefinition definition)
        {
            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, out currentAmount, out maxAmount);
            return maxAmount - currentAmount;
        }

        public static double GetResourceSpareCapacity(this Part part, PartResourceDefinition definition, ResourceFlowMode flowmode)
        {
            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, flowmode, out currentAmount, out maxAmount);
            return maxAmount - currentAmount;
        }

        public static double GetResourceAvailable(this Part part, PartResourceDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[KSPI]: PartResourceDefinition definition is NULL");
                return 0;
            }

            double currentAmount;
            double maxAmount;

            part.GetConnectedResourceTotals(definition.id, ResourceFlowMode.STAGE_PRIORITY_FLOW, out currentAmount, out maxAmount);
            return currentAmount;
        }

        public static double GetResourceAvailable(this Part part, PartResourceDefinition definition, ResourceFlowMode flowmode)
        {
            if (definition == null)
            {
                Debug.LogError("[KSPI]: PartResourceDefinition definition is NULL");
                return 0;
            }

            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, flowmode, out currentAmount, out maxAmount);
            return currentAmount;
        }

        public static double GetResourceAvailable(this Part part, ResourceFlowMode flowmode,  PartResourceDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[KSPI]: PartResourceDefinition definition is NULL");
                return 0;
            }

            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, flowmode, out currentAmount, out maxAmount);
            return currentAmount;
        }

        public static double GetResourceAvailable(this Part part, string name, ResourceFlowMode flowMode)
        {
            var definition = PartResourceLibrary.Instance.GetDefinition(name);

            if (definition == null)
            {
                Debug.LogError("[KSPI]: PartResourceDefinition definition is NULL");
                return 0;
            }

            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, flowMode, out currentAmount, out maxAmount);
            return currentAmount;
        }

        public static double GetResourceAvailable(this Part part, string name)
        {
            var definition = PartResourceLibrary.Instance.GetDefinition(name);

            if (definition == null)
            {
                Debug.LogError("[KSPI]: PartResourceDefinition definition for " + name + " not found");
                return 0;
            }

            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, out currentAmount, out maxAmount);
            return currentAmount;
        }

        public static double GetResourceRatio(this Part part, string name)
        {
            var definition = PartResourceLibrary.Instance.GetDefinition(name);

            if (definition == null)
            {
                Debug.LogError("[KSPI]: PartResourceDefinition definition is NULL");
                return 0;
            }

            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, out currentAmount, out maxAmount);
            return maxAmount > 0 ? currentAmount / maxAmount: 0;
        }

        public static double GetResourceMaxAvailable(this Part part, string name)
        {
            var definition = PartResourceLibrary.Instance.GetDefinition(name);

            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, out currentAmount, out maxAmount);
            return maxAmount;
        }

        public static double GetResourceMaxAvailable(this Part part, PartResourceDefinition definition)
        {
            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(definition.id, out currentAmount, out maxAmount);
            return maxAmount;
        }

        private static FieldInfo windowListField;

        /// <summary>
        /// Find the UIPartActionWindow for a part. Usually this is useful just to mark it as dirty.
        /// </summary>
        public static UIPartActionWindow FindActionWindow(this Part part)
        {
            if (part == null)
                return null;

            // We need to do quite a bit of piss-farting about with reflection to 
            // dig the thing out. We could just use Object.Find, but that requires hitting a heap more objects.
            UIPartActionController controller = UIPartActionController.Instance;
            if (controller == null)
                return null;

            if (windowListField == null)
            {
                Type cntrType = typeof(UIPartActionController);
                foreach (FieldInfo info in cntrType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (info.FieldType == typeof(List<UIPartActionWindow>))
                    {
                        windowListField = info;
                        goto foundField;
                    }
                }
                Debug.LogWarning("*PartUtils* Unable to find UIPartActionWindow list");
                return null;
            }
            foundField:

            List<UIPartActionWindow> uiPartActionWindows = (List<UIPartActionWindow>)windowListField.GetValue(controller);
            if (uiPartActionWindows == null)
                return null;

            return uiPartActionWindows.FirstOrDefault(window => window != null && window.part == part);
        }

        public static bool IsConnectedToModule(this Part currentPart, String partmodule, int maxChildDepth, Part previousPart = null)
        {
            bool found = currentPart.Modules.Contains(partmodule);
            if (found)
                return true;

            if (currentPart.parent != null && currentPart.parent != previousPart)
            {
                bool foundPart = IsConnectedToModule(currentPart.parent, partmodule, maxChildDepth, currentPart);
                if (foundPart)
                    return true;
            }

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    bool foundPart = IsConnectedToModule(child, partmodule, (maxChildDepth - 1), currentPart);
                    if (foundPart)
                        return true;
                }
            }

            return false;
        }

        public static bool IsConnectedToPart(this Part currentPart, String partname, int maxChildDepth, Part previousPart = null)
        {
            bool found = currentPart.name == partname;
            if (found)
                return true;

            if (currentPart.parent != null && currentPart.parent != previousPart)
            {
                bool foundPart = IsConnectedToPart(currentPart.parent, partname, maxChildDepth, currentPart);
                if (foundPart)
                    return true;
            }

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    bool foundPart = IsConnectedToPart(child, partname, (maxChildDepth - 1), currentPart);
                    if (foundPart)
                        return true;
                }
            }

            return false;
        }

        public static double FindAmountOfAvailableFuel(this Part currentPart, String resourcename, int maxChildDepth, Part previousPart = null)
        {
            double amount = 0;

            if (currentPart.Resources.Contains(resourcename))
            {
                var partResourceAmount = currentPart.Resources[resourcename].amount;
                //UnityEngine.Debug.Log("[KSPI]: found " + partResourceAmount.ToString("0.0000") + " " + resourcename + " resource in " + currentPart.name);
                amount += partResourceAmount;
            }

            if (currentPart.parent != null && currentPart.parent != previousPart)
                amount += FindAmountOfAvailableFuel(currentPart.parent, resourcename, maxChildDepth, currentPart);

            if (maxChildDepth <= 0) return amount;

            foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
            {
                amount += FindAmountOfAvailableFuel(child, resourcename, (maxChildDepth - 1), currentPart);
            }

            return amount;
        }

        public static double FindMaxAmountOfAvailableFuel(this Part currentPart, String resourcename, int maxChildDepth, Part previousPart = null)
        {
            double maxAmount = 0;

            if (currentPart.Resources.Contains(resourcename))
            {
                var partResourceAmount = currentPart.Resources[resourcename].maxAmount;
                //UnityEngine.Debug.Log("[KSPI]: found " + partResourceAmount.ToString("0.0000") + " " + resourcename + " resource in " + currentPart.name);
                maxAmount += partResourceAmount;
            }

            if (currentPart.parent != null && currentPart.parent != previousPart)
                maxAmount += FindMaxAmountOfAvailableFuel(currentPart.parent, resourcename, maxChildDepth, currentPart);

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    maxAmount += FindMaxAmountOfAvailableFuel(child, resourcename, (maxChildDepth - 1), currentPart);
                }
            }

            return maxAmount;
        }
    }
}
