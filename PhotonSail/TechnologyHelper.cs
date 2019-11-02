using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using FNPlugin.Beamedpower;
using FNPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Beamedpower
{
    class TechnologyHelper
    {
        private static Dictionary<string, PartUpgradeHandler.Upgrade> partUpgradeByName;

        public static Dictionary<string, PartUpgradeHandler.Upgrade> PartUpgradeByName
        {
            get
            {
                if (partUpgradeByName == null)
                {
                    partUpgradeByName = new Dictionary<string, PartUpgradeHandler.Upgrade>();

                    // catalog part upgrades
                    ConfigNode[] partupgradeNodes = GameDatabase.Instance.GetConfigNodes("PARTUPGRADE");
                    Debug.Log("[KSPI]: PluginHelper found: " + partupgradeNodes.Count() + " Part upgrades");

                    for (int i = 0; i < partupgradeNodes.Length; i++)
                    {
                        var partUpgradeConfig = partupgradeNodes[i];

                        var partUpgrade = new PartUpgradeHandler.Upgrade();
                        partUpgrade.name = partUpgradeConfig.GetValue("name");
                        partUpgrade.techRequired = partUpgradeConfig.GetValue("techRequired");
                        partUpgrade.manufacturer = partUpgradeConfig.GetValue("manufacturer");

                        if (partUpgradeByName.ContainsKey(partUpgrade.name))
                        {
                            Debug.LogError("[KSPI]: Duplicate error: failed to add PARTUPGRADE" + partUpgrade.name + " with techRequired " + partUpgrade.techRequired + " from manufacturer " + partUpgrade.manufacturer);
                        }
                        else
                        {
                            Debug.Log("[KSPI]: PluginHelper indexed PARTUPGRADE " + partUpgrade.name + " with techRequired " + partUpgrade.techRequired + " from manufacturer " + partUpgrade.manufacturer);
                            partUpgradeByName.Add(partUpgrade.name, partUpgrade);
                        }
                    }
                }

                return partUpgradeByName;
            }
        }

        public static string GetTechTitleById(string id)
        {
            var result = ResearchAndDevelopment.GetTechnologyTitle(id);
            if (!String.IsNullOrEmpty(result))
                return result;

            PartUpgradeHandler.Upgrade partUpgrade;
            if (PartUpgradeByName.TryGetValue(id, out partUpgrade))
            {
                RDTech upgradeTechnode;
                if (RDTechByName.TryGetValue(partUpgrade.techRequired, out upgradeTechnode))
                    return upgradeTechnode.title;
            }

            RDTech technode;
            if (RDTechByName.TryGetValue(id, out technode))
                return technode.title;

            return id;
        }

        public static bool UpgradeAvailable(string id)
        {
            if (String.IsNullOrEmpty(id))
                return false;

            if (id == "true" || id == "always")
                return true;

            if (id == "false" || id == "none")
                return false;

            if (HighLogic.CurrentGame == null) return true;
            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return true;

            PartUpgradeHandler.Upgrade partUpgrade;
            if (PartUpgradeByName.TryGetValue(id, out partUpgrade))
            {
                //Debug.Log("[KSPI]: found PARTUPGRADE " + id + ", checking techRequired " + partUpgrade.techRequired);
                id = partUpgrade.techRequired;
            }

            if (HighLogic.CurrentGame != null)
            {
                return !TechnologyIsInUse || HasTech(id);
            }
            return false;
        }

        private static Dictionary<string, RDTech> rdTechByName;

        public static Dictionary<string, RDTech> RDTechByName
        {
            get
            {
                if (rdTechByName == null)
                {
                    rdTechByName = new Dictionary<string, RDTech>();

                    // catalog part upgrades
                    ConfigNode[] techtree = GameDatabase.Instance.GetConfigNodes("TechTree");
                    Debug.Log("[KSPI]: PluginHelper found: " + techtree.Count() + " TechTrees");

                    foreach (var techtreeConfig in techtree)
                    {
                        var technodes = techtreeConfig.nodes;

                        Debug.Log("[KSPI]: PluginHelper found: " + technodes.Count + " Technodes");
                        for (var j = 0; j < technodes.Count; j++)
                        {
                            var technode = technodes[j];

                            var tech = new RDTech { techID = technode.GetValue("id"), title = technode.GetValue("title") };

                            if (rdTechByName.ContainsKey(tech.techID))
                                Debug.LogError("[KSPI]: Duplicate error: skipped technode id: " + tech.techID + " title: " + tech.title);
                            else
                            {
                                Debug.Log("[KSPI]: PluginHelper technode id: " + tech.techID + " title: " + tech.title);
                                rdTechByName.Add(tech.techID, tech);
                            }
                        }
                    }
                }
                return rdTechByName;
            }
        }

        public static bool TechnologyIsInUse
        {
            get { return (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX); }
        }

        public static int HasTech(string techid, int increase)
        {
            return ResearchAndDevelopment.Instance.GetTechState(techid) != null ? increase : 0;
        }

        public static bool HasTech(string techid)
        {
            return ResearchAndDevelopment.Instance.GetTechState(techid) != null;
        }
    }
}
