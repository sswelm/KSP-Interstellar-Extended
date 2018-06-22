namespace FNPlugin.Beamedpower
{
    public interface IBeamedPowerReceiver
    {
        int ReceiverType { get; }

        double Diameter { get; }

        double ApertureMultiplier { get; }

        double MinimumWavelength { get; }

        double MaximumWavelength { get; }

        double HighSpeedAtmosphereFactor { get; }

        double FacingThreshold { get; }

        double FacingSurfaceExponent { get; }

        double FacingEfficiencyExponent { get; }

        double SpotsizeNormalizationExponent { get; }

        bool CanBeActiveInAtmosphere { get; }

        Vessel Vessel { get; }

        Part Part { get; }
    }
}
