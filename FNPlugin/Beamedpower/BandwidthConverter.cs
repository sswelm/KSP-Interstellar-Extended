using System.Text;
using System;
using System.Linq;
using KSP.Localization;
using KSP.UI.Screens;

namespace FNPlugin.Microwave
{
    [KSPModule("Rectenna Bandwidth Converter")]
    class RectennaConverter : BandwidthConverter {}


    [KSPModule("Beamed Power Bandwidth Converter")]
    class BandwidthConverter : PartModule
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

        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage1 = 45;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage1 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage1 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement1 = "";

        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage2 = 45;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage2 = 0;
        [KSPField( guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage2 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement2 = "";

        [KSPField(isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage3 = 45;
        [KSPField(isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage3 = 0;
        [KSPField(isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage3 = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public string techRequirement3 = "";

        public void Initialize()
        {
            if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement3))
                AvailableTechLevel = 3;
            else if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement2))
                AvailableTechLevel = 2;
            else if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement1))
                AvailableTechLevel = 1;
            else if (PluginHelper.HasTechRequirementOrEmpty(techRequirement0))
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

            if (!string.IsNullOrEmpty(techRequirement0))
            {
                info.AppendLine("Mk1 technode: \n" + PluginHelper.GetTechTitleById(techRequirement0));
                info.AppendLine("Mk1 efficiency: " + efficiencyPercentage0 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement1))
            {
                info.AppendLine("Mk2 technode: \n" + PluginHelper.GetTechTitleById(techRequirement1));
                info.AppendLine("Mk2 efficiency: " + efficiencyPercentage1 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement2))
            {
                info.AppendLine("Mk3 technode: \n" + PluginHelper.GetTechTitleById(techRequirement2));
                info.AppendLine("Mk3 efficiency: " + efficiencyPercentage2 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement3))
            {
                info.AppendLine("Mk4 technode: \n" + PluginHelper.GetTechTitleById(techRequirement3));
                info.AppendLine("Mk4 efficiency: " + efficiencyPercentage2 + "%");
            }
            info.AppendLine("</size>");

            return info.ToString();
        }

    }


}
