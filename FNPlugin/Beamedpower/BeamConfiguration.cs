using System.Text;
using KSP.Localization;

namespace FNPlugin.Microwave
{
    [KSPModule("Beamed Power Transmit Configuration")]
    public class BeamConfiguration : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Wavelength Name")]
        public string beamWaveName = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double wavelength = 0.003189281;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Atmospheric Absorption", guiFormat = "F4", guiUnits = "%")]
        public double atmosphericAbsorptionPercentage = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Water Absorption", guiFormat = "F4", guiUnits = "%")]
        public double waterAbsorptionPercentage = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Power to Beam Efficiency 0", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage0 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement 0")]
        public string techRequirement0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Power to Beam Efficiency 1", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage1 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement 1")]
        public string techRequirement1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Power to Beam Efficiency 2", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage2 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement 2")]
        public string techRequirement2 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Power to Beam Efficiency 3", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage3 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement 3")]
        public string techRequirement3 = "";


        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Name: " + beamWaveName);
            info.AppendLine("Wavelength: " + wavelength);

            if (!string.IsNullOrEmpty(techRequirement0))
            {
                info.AppendLine("Mk1 technode: \n" + Localizer.Format(PluginHelper.GetTechTitleById(techRequirement0)));
                info.AppendLine("Mk1 efficiency: " + efficiencyPercentage0 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement1))
            {
                info.AppendLine("Mk2 technode: \n" + Localizer.Format(PluginHelper.GetTechTitleById(techRequirement1)));
                info.AppendLine("Mk2 efficiency: " + efficiencyPercentage1 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement2))
            {
                info.AppendLine("Mk3 technode: \n" + Localizer.Format(PluginHelper.GetTechTitleById(techRequirement2)));
                info.AppendLine("Mk3 efficiency: " + efficiencyPercentage2 + "%");
            }
            if (!string.IsNullOrEmpty(techRequirement3))
            {
                info.AppendLine("Mk4 technode: \n" + Localizer.Format(PluginHelper.GetTechTitleById(techRequirement3)));
                info.AppendLine("Mk4 efficiency: " + efficiencyPercentage3 + "%");
            }

            return info.ToString();
        }
    }
}
