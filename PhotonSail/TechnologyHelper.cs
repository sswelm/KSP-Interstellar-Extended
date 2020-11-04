using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhotonSail
{
    class TechnologyHelper
    {
        private static Dictionary<string, PartUpgradeHandler.Upgrade> _partUpgradeByName;

        public static Dictionary<string, PartUpgradeHandler.Upgrade> PartUpgradeByName
        {
            get
            {
                if (_partUpgradeByName != null)
                    return _partUpgradeByName;

                _partUpgradeByName = new Dictionary<string, PartUpgradeHandler.Upgrade>();

                // catalog part upgrades
                ConfigNode[] partUpgradeNodes = GameDatabase.Instance.GetConfigNodes("PARTUPGRADE");
                Debug.Log("[PhotonSail]: PartUpgradeByName found: " + partUpgradeNodes.Count() + " Part upgrades");

                for (int i = 0; i < partUpgradeNodes.Length; i++)
                {
                    var partUpgradeConfig = partUpgradeNodes[i];

                    var partUpgrade = new PartUpgradeHandler.Upgrade
                    {
                        name = partUpgradeConfig.GetValue("name"),
                        techRequired = partUpgradeConfig.GetValue("techRequired"),
                        manufacturer = partUpgradeConfig.GetValue("manufacturer")
                    };

                    if (_partUpgradeByName.ContainsKey(partUpgrade.name))
                    {
                        Debug.LogError("[PhotonSail]: Duplicate error: failed to add PARTUPGRADE " + partUpgrade.name + " with techRequired " + partUpgrade.techRequired + " from manufacturer " + partUpgrade.manufacturer);
                    }
                    else
                    {
                        Debug.Log("[PhotonSail]: PluginHelper indexed PARTUPGRADE " + partUpgrade.name + " with techRequired " + partUpgrade.techRequired + " from manufacturer " + partUpgrade.manufacturer);
                        _partUpgradeByName.Add(partUpgrade.name, partUpgrade);
                    }
                }

                return _partUpgradeByName;
            }
        }

        public static string GetTechTitleById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[PhotonSail]: GetTechTitleById - id is null");
                return id;
            }

            var result = ResearchAndDevelopment.GetTechnologyTitle(id);
            if (!string.IsNullOrEmpty(result))
                return result;

            if (PartUpgradeByName.TryGetValue(id, out var partUpgrade))
            {
                if (partUpgrade != null && !string.IsNullOrEmpty(partUpgrade.techRequired))
                {
                    Debug.LogError("[PhotonSail]: GetTechTitleById - id is null");
                    if (RDTechByName.TryGetValue(partUpgrade.techRequired, out var upgradeTechNode))
                        return upgradeTechNode?.title;
                }
                else if (partUpgrade != null)
                    Debug.LogError("[PhotonSail]: GetTechTitleById - partUpgrade is null");
                else
                    Debug.LogError("[PhotonSail]: GetTechTitleById - partUpgrade.techRequired is null");
            }

            if (RDTechByName.TryGetValue(id, out var techNode))
                return techNode.title;

            return id;
        }

        public static bool UpgradeAvailable(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            if (id == "true" || id == "always")
                return true;

            if (id == "false" || id == "none")
                return false;

            if (HighLogic.CurrentGame == null)
                return true;

            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                return true;

            if (PartUpgradeByName.TryGetValue(id, out var partUpgrade))
                id = partUpgrade.techRequired;

            if (HighLogic.CurrentGame != null)
                return !TechnologyIsInUse || HasTech(id);

            return false;
        }

        private static Dictionary<string, RDTech> rdTechByName;

        public static Dictionary<string, RDTech> RDTechByName
        {
            get
            {
                if (rdTechByName != null)
                    return rdTechByName;

                rdTechByName = new Dictionary<string, RDTech>();

                // catalog part upgrades
                ConfigNode[] techTree = GameDatabase.Instance.GetConfigNodes("TechTree");
                Debug.Log("[PhotonSail]: PluginHelper found: " + techTree.Count() + " TechTrees");

                foreach (var techTreeConfig in techTree)
                {
                    var techNodes = techTreeConfig.nodes;

                    Debug.Log("[PhotonSail]: PluginHelper found: " + techNodes.Count + " Technodes");
                    for (var j = 0; j < techNodes.Count; j++)
                    {
                        var techNode = techNodes[j];

                        var tech = new RDTech { techID = techNode.GetValue("id"), title = techNode.GetValue("title") };

                        if (rdTechByName.ContainsKey(tech.techID))
                            Debug.LogError("[PhotonSail]: Duplicate error: skipped technode id: " + tech.techID + " title: " + tech.title);
                        else
                        {
                            Debug.Log("[PhotonSail]: PluginHelper technode id: " + tech.techID + " title: " + tech.title);
                            rdTechByName.Add(tech.techID, tech);
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
