namespace FNPlugin
{
    /// <summary>
    /// Storage class required for relay route calculation
    /// </summary>
    class MicrowaveRoute
    {
        public double Efficiency { get; set; }
        public WaveLengthData WavelengthData { get; private set; }
        public double WaveLength { get { return WavelengthData.wavelength; } }
        public double MinimumWaveLength { get { return WavelengthData.minWavelength; } }
        public double MaximumWaveLength { get { return WavelengthData.maxWavelength; } }
        public double Distance { get; set; }
        public double FacingFactor { get; set; }
        public VesselRelayPersistence PreviousRelay { get; set; }
        public double Spotsize { get; set; }

        public MicrowaveRoute(double efficiency, double distance, double facingFactor, double spotsize, WaveLengthData wavelengthData, VesselRelayPersistence previousRelay = null)
        {
            Efficiency = efficiency;
            Distance = distance;
            FacingFactor = facingFactor;
            PreviousRelay = previousRelay;
            Spotsize = spotsize;
            WavelengthData = wavelengthData;
        }
    }
}
