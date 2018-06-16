using System;
using System.Collections.Generic;
using FNPlugin.Redist;

namespace FNPlugin 
{
    class WaveLengthData
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

    class VesselRelayPersistence : IVesselRelayPersistence 
    {
        Vessel vessel;
        bool isActive;
        double aperture;
        double power_capacity;
        double minimumRelayWavelenght;
        double maximumRelayWavelenght;

        public VesselRelayPersistence(Vessel vessel) 
        {
            this.vessel = vessel;
            SupportedTransmitWavelengths = new List<WaveLengthData>();
        }

        public List<WaveLengthData> SupportedTransmitWavelengths { get; private set; }

        public Vessel Vessel
        {
            get { return vessel; }
        }

        public bool IsActive
        {
            get { return this.isActive; }
            set { this.isActive = value; }
        }

        public double Aperture
        {
            get { return aperture != 0 ? this.aperture : 1; }
            set { this.aperture = value; }
        }

        public double PowerCapacity
        {
            get { return power_capacity != 0 ? this.power_capacity : 1000; }
            set { this.power_capacity = value; }
        }

        public double MinimumRelayWavelenght
        {
            get { return minimumRelayWavelenght != 0 ? minimumRelayWavelenght : 0.003189281; }
            set { minimumRelayWavelenght = value; }
        }

        public double MaximumRelayWavelenght
        {
            get { return maximumRelayWavelenght != 0 ? maximumRelayWavelenght: 0.008565499 ; }
            set { maximumRelayWavelenght = value; }
        }
    }
}
