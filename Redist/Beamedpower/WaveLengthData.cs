using System;

namespace FNPlugin.Beamedpower
{
    public class WaveLengthData
    {
        public Guid partId { get; set; }
        public bool isMirror { get; set; }
        public int count { get; set; }
        public double apertureSum { get; set; }
        public double wavelength { get; set; }
        public double minWavelength { get; set; }
        public double maxWavelength { get; set; }
        public double atmosphericAbsorption { get; set; }
        public double nuclearPower { get; set; }
        public double solarPower { get; set; }
        public double powerCapacity { get; set; }

        public override int GetHashCode()
        {
            return this.wavelength.GetHashCode();
        }
    }
}
