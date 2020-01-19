using System;
using System.Text;
using KSP.Localization;

namespace FNPlugin.Beamedpower
{

    [KSPModule("#LOC_PhotonSail_PhotonReflectionModuleName")]//Photon Reflection
    class PhotonReflectionDefinition : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        private string bandwidthName = Localizer.Format("#LOC_PhotonSail_missing");//"missing"
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double targetWavelength = 0;        
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double minimumWavelength = 0.001;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double maximumWavelength = 1;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public int AvailableTechLevel;

        // Tech 0
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double reflectionPercentage0 = 20;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double photovoltaicPercentage0 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techName0 = "start";

        // Tech 1
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double reflectionPercentage1 = 20;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double photovoltaicPercentage1 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techName1 = "";

        // Tech 2
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double reflectionPercentage2 = 20;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double photovoltaicPercentage2 = 0;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string techRequirement2 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string techName2 = "";

        // Tech 3
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double reflectionPercentage3 = 45;
        [KSPField(guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double photovoltaicPercentage3 = 0;
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

        public double PhotonReflectionPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return reflectionPercentage0;
                else if (AvailableTechLevel == 1)
                    return reflectionPercentage1;
                else if (AvailableTechLevel == 2)
                    return reflectionPercentage2;
                else if (AvailableTechLevel == 3)
                    return reflectionPercentage3;
                else
                    return 0;
            }
        }

        public double PhotonPhotovoltaicPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return photovoltaicPercentage0;
                else if (AvailableTechLevel == 1)
                    return photovoltaicPercentage1;
                else if (AvailableTechLevel == 2)
                    return photovoltaicPercentage2;
                else if (AvailableTechLevel == 3)
                    return photovoltaicPercentage3;
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

        public string BandwidthName { get { return bandwidthName; } set { bandwidthName = value; } }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("<size=10>");
            info.AppendLine(Localizer.Format("#LOC_PhotonSail_NameGetInfo") +": " + BandwidthName);//Name
            info.AppendLine(Localizer.Format("#LOC_PhotonSail_BandwidthstartGetInfo") +": " + minimumWavelength + " m");//Bandwidth start
            info.AppendLine(Localizer.Format("#LOC_PhotonSail_BandwidthendGetInfo") +": " + maximumWavelength + " m");//Bandwidth end

            if (!string.IsNullOrEmpty(techRequirement0) && !string.IsNullOrEmpty(techRequirement1))
            {
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk1technode") +": " + (String.IsNullOrEmpty(techName0) ? techName0 : techRequirement0));//Mk1 technode
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk1reflection") +": " + reflectionPercentage0 + "%");//Mk1 reflection
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk1photovoltaic")+": " + photovoltaicPercentage0 + "%");//Mk1 photovoltaic
            }
            if (!string.IsNullOrEmpty(techRequirement1))
            {
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk2technode") +": " + (String.IsNullOrEmpty(techName1) ? techName1 : techRequirement1));//Mk2 technode
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk2reflection") +": " + reflectionPercentage1 + "%");//Mk2 reflection
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk1photovoltaic") +": " + photovoltaicPercentage1 + "%");//Mk1 photovoltaic
            }
            if (!string.IsNullOrEmpty(techRequirement2))
            {
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk3technode") + ": " + (String.IsNullOrEmpty(techName2) ? techName2 : techRequirement2));//Mk3 technode
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk3reflection") + ": " + reflectionPercentage2 + "%");//Mk3 reflection
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk1photovoltaic") +": " + photovoltaicPercentage2 + "%");//Mk1 photovoltaic
            }
            if (!string.IsNullOrEmpty(techRequirement3))
            {
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk4technode")+": " + (String.IsNullOrEmpty(techName3) ? techName3 : techRequirement3));//Mk4 technode
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk4reflection") +": " + reflectionPercentage3 + "%");//Mk4 reflection
                info.AppendLine(Localizer.Format("#LOC_PhotonSail_Mk1photovoltaic") +": " + photovoltaicPercentage3 + "%");//Mk1 photovoltaic
            }
            info.AppendLine("</size>");

            return info.ToString();
        }

        public static bool HasTech(string techid)
        {
            //return ResearchAndDevelopment.Instance.GetTechState(techid) != null;

            return TechnologyHelper.UpgradeAvailable(techid);
        }
    }




}
