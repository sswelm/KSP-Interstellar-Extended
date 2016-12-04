using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Microwave
{
    public class BeamConfiguration : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Wavelength Name")]
        public string beamWaveName = "";
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double wavelength = 0.003189281;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Atmospheric Absorption", guiFormat = "F4", guiUnits = "%")]
        public double atmosphericAbsorptionPercentage = 1;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Water Absorption", guiFormat = "F4", guiUnits = "%")]
        public double waterAbsorptionPercentage = 1;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Power to Beam Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage0 = 90;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement")]
        public string techRequirement0 = "";
    }


}
