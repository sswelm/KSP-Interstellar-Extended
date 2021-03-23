namespace FNPlugin.Microwave
{
    [KSPModule("#LOC_KSPIE_BeamConfiguration_MouduleName")]//Beamed Power Transmit Configuration
    public class BeamConfiguration : PartModule
    {
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_BeamWaveName")] public string beamWaveName = "";
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement0")] public string techRequirement0 = "";
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement1")] public string techRequirement1 = "";
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement2")] public string techRequirement2 = "";
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_Wavelength", guiFormat = "F9", guiUnits = " m")] public double wavelength = 0.003189281;
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_AtmosphericAbsorption", guiFormat = "F4", guiUnits = "%")] public double atmosphericAbsorptionPercentage = 1;
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_WaterAbsorption", guiFormat = "F4", guiUnits = "%")] public double waterAbsorptionPercentage = 1;
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage0", guiFormat = "F0", guiUnits = "%")] public double efficiencyPercentage0 = 0;
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage1", guiFormat = "F0", guiUnits = "%")] public double efficiencyPercentage1 = 0;
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage2", guiFormat = "F0", guiUnits = "%")] public double efficiencyPercentage2 = 0;
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage3", guiFormat = "F0", guiUnits = "%")] public double efficiencyPercentage3 = 0;
        [KSPField(guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement3")]//Tech Requirement 3
        public string techRequirement3 = "";
    }
}
