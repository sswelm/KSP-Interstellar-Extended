using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Microwave
{
    public class BeamConfiguration : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Wavelength Name")]
        public string beamWaveName = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Wavelength", guiFormat = "F9", guiUnits = " m")]
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
    }


}
