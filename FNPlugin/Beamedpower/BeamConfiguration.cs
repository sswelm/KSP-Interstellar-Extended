namespace FNPlugin.Microwave
{
    [KSPModule("#LOC_KSPIE_BeamConfiguration_MouduleName")]//Beamed Power Transmit Configuration
    public class BeamConfiguration : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_BeamWaveName")]//Wavelength Name
        public string beamWaveName = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_Wavelength", guiFormat = "F9", guiUnits = " m")]//Wavelength
        public double wavelength = 0.003189281;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_AtmosphericAbsorption", guiFormat = "F4", guiUnits = "%")]//Atmospheric Absorption
        public double atmosphericAbsorptionPercentage = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_WaterAbsorption", guiFormat = "F4", guiUnits = "%")]//Water Absorption
        public double waterAbsorptionPercentage = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage0", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 0
        public double efficiencyPercentage0 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement0")]//Tech Requirement 0
        public string techRequirement0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage1", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 1
        public double efficiencyPercentage1 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement1")]//Tech Requirement 1
        public string techRequirement1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage2", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 2
        public double efficiencyPercentage2 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement2")]//Tech Requirement 2
        public string techRequirement2 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage3", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 3
        public double efficiencyPercentage3 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement3")]//Tech Requirement 3
        public string techRequirement3 = "";
    }
}
