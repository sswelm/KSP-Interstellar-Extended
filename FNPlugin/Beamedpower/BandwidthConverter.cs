using KSP.Localization;

namespace FNPlugin.Microwave
{
    [KSPModule("Rectenna Bandwidth Converter")]//#LOC_KSPIE_RectennaBandwidthConverter
    class RectennaConverter : BandwidthConverter {}


    [KSPModule("Beamed Power Bandwidth Converter")]//#LOC_KSPIE_BeamedPowerBandwidthConverter
    class BandwidthConverter : PartModule
    {
        [KSPField(groupName = BeamedPowerReceiver.GROUP, groupDisplayName = BeamedPowerReceiver.GROUP_TITLE, isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string bandwidthName = Localizer.Format("#LOC_KSPIE_BandwidthCoverter_missing");//"missing"
        [KSPField(groupName = BeamedPowerReceiver.GROUP, isPersistant = true, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double targetWavelength = 0;

        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double minimumWavelength = 0.001;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double maximumWavelength = 1;

        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false)]
        public int AvailableTechLevel;

        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage0 = 45;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage0 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage0 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false)]
        public string techRequirement0 = "";

        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage1 = 45;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage1 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage1 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false)]
        public string techRequirement1 = "";

        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage2 = 45;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage2 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage2 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, guiActiveEditor = false, guiActive = false)]
        public string techRequirement2 = "";

        [KSPField(groupName = BeamedPowerReceiver.GROUP, isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage3 = 45;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage3 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage3 = 0;
        [KSPField(groupName = BeamedPowerReceiver.GROUP, isPersistant = false, guiActive = false)]
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
            var info = StringBuilderCache.Acquire();

            info.AppendLine("<size=10>");
            info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_InfoName")).Append(": ").AppendLine(bandwidthName);//Name
            info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Bandwidthstart")).Append(": ").Append(minimumWavelength).AppendLine(" m");//Bandwidth start
            info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Bandwidthend")).Append(": ").Append(maximumWavelength).AppendLine(" m");//Bandwidth end

            if (!string.IsNullOrEmpty(techRequirement0))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk1technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement0)));//Mk1 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk1efficiency")).Append(": ");
                info.Append(efficiencyPercentage0.ToString("F0")).AppendLine("%");//Mk1 efficiency
            }
            if (!string.IsNullOrEmpty(techRequirement1))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk2technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement1)));//Mk2 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk2efficiency")).Append(": ");
                info.Append(efficiencyPercentage1.ToString("F0")).AppendLine("%");//Mk2 efficiency
            }
            if (!string.IsNullOrEmpty(techRequirement2))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk3technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement2)));//Mk3 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk3efficiency")).Append(": ");
                info.Append(efficiencyPercentage2.ToString("F0")).AppendLine("%");//Mk3 efficiency
            }
            if (!string.IsNullOrEmpty(techRequirement3))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk4technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement3)));//Mk4 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthCoverter_Mk4efficiency")).Append(": ");
                info.Append(efficiencyPercentage3.ToString("F0")).AppendLine("%");//Mk4 efficiency
            }
            info.AppendLine("</size>");

            return info.ToStringAndRelease();
        }
    }
}
