using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Microwave
{
    class BandwidthConverter : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Wavelength Name")]
        public string bandwidthName = "missing";
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Target Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double targetWavelength = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Start Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double minimumWavelength = 0.001;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "End Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double maximumWavelength = 1.000;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Efficiency Old", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage0 = 45;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Electric Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage0 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Thermal Power Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage0 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Tech Requirement")]
        public string techRequirement0 = "";


        public double ElectricEfficiencyPercentage0
        {
            get { return electricEfficiencyPercentage0 > 0 ? electricEfficiencyPercentage0 : efficiencyPercentage0; }
        }

        public double ThermalEfficiencyPercentage0
        {
            get { return thermalEfficiencyPercentage0 > 0 ? thermalEfficiencyPercentage0 : ElectricEfficiencyPercentage0; }
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

    }


}
