using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using KSP.UI.Screens;

namespace FNPlugin.Beamedpower
{

    [KSPModule("Photon Reflection")]
    class PhotonReflectionDefinition : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string bandwidthName = "missing";
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double targetWavelength = 0;
        
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double minimumWavelength = 0.001;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double maximumWavelength = 1;

        [KSPField(guiActiveEditor = false, guiActive = false)]
        public int AvailableTechLevel;

        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage0 = 45;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage0 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage0 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techName0 = "start";

        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage1 = 45;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage1 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage1 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techName1 = "";

        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage2 = 45;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage2 = 0;
        [KSPField( guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage2 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement2 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techName2 = "";

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage3 = 45;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage3 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage3 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techRequirement3 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techName3 = "";

        public void Initialize()
        {
            if (HasTech(techRequirement3))
                AvailableTechLevel = 3;
            else if (HasTech(techRequirement2))
                AvailableTechLevel = 2;
            else if (HasTech(techRequirement1))
                AvailableTechLevel = 1;
            else if (HasTech(techRequirement0))
                AvailableTechLevel = 0;
        }

        public double MaxElectricEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return electricEfficiencyPercentage0 > 0 ? electricEfficiencyPercentage0 : efficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return electricEfficiencyPercentage1 > 0 ? electricEfficiencyPercentage1 : efficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return electricEfficiencyPercentage2 > 0 ? electricEfficiencyPercentage2 : efficiencyPercentage2;
                else if (AvailableTechLevel == 3)
                    return electricEfficiencyPercentage3 > 0 ? electricEfficiencyPercentage3 : efficiencyPercentage3;
                else
                    return 0;
            }
        }

        public double MaxThermalEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return thermalEfficiencyPercentage0 > 0 ? thermalEfficiencyPercentage0 : efficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return thermalEfficiencyPercentage1 > 0 ? thermalEfficiencyPercentage1 : efficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return thermalEfficiencyPercentage2 > 0 ? thermalEfficiencyPercentage2 : efficiencyPercentage2;
                else if (AvailableTechLevel == 3)
                    return thermalEfficiencyPercentage3 > 0 ? thermalEfficiencyPercentage3 : efficiencyPercentage3;
                else
                    return 0;
            }
        }

        public double MaxEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return efficiencyPercentage0 > 0 ? efficiencyPercentage0 : thermalEfficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return efficiencyPercentage1 > 0 ? efficiencyPercentage1 : thermalEfficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return efficiencyPercentage2 > 0 ? efficiencyPercentage2 : thermalEfficiencyPercentage2;
                else if (AvailableTechLevel == 3)
                    return efficiencyPercentage3 > 0 ? efficiencyPercentage3 : thermalEfficiencyPercentage3;
                else
                    return 0;
            }
        }

        public double TargetWavelength
        {
            get 
            {
                if (targetWavelength == 0)
                    targetWavelength = (minimumWavelength + maximumWavelength) / 2;
                
                return targetWavelength;
            }
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("<size=10>");
            info.AppendLine("Name: " + bandwidthName);
            info.AppendLine("Bandwidth start: " + minimumWavelength + " m");
            info.AppendLine("Bandwidth end: " + maximumWavelength + " m");

            if (!string.IsNullOrEmpty(techRequirement0) && !string.IsNullOrEmpty(techRequirement1))
            {
                info.AppendLine("Mk1 technode: " + (String.IsNullOrEmpty(techName0) ? techName0 : techRequirement0));
                info.AppendLine("Mk1 efficiency: " + efficiencyPercentage0 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement1))
            {
                info.AppendLine("Mk2 technode: " + (String.IsNullOrEmpty(techName1) ? techName1 : techRequirement1));
                info.AppendLine("Mk2 efficiency: " + efficiencyPercentage1 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement2))
            {
                info.AppendLine("Mk3 technode: " + (String.IsNullOrEmpty(techName2) ? techName2 : techRequirement2));
                info.AppendLine("Mk3 efficiency: " + efficiencyPercentage2 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement3))
            {
                info.AppendLine("Mk4 technode: " + (String.IsNullOrEmpty(techName3) ? techName3 : techRequirement3));
                info.AppendLine("Mk4 efficiency: " + efficiencyPercentage2 + "%");
            }
            info.AppendLine("</size>");

            return info.ToString();
        }

        public static bool HasTech(string techid)
        {
            return ResearchAndDevelopment.Instance.GetTechState(techid) != null;
        }
    }




}
