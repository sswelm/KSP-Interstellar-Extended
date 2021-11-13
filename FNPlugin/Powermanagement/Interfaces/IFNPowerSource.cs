using FNPlugin.Redist;

namespace FNPlugin.Powermanagement
{
    public interface IFNPowerSource : IPowerSource
    {
        void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, bool isMHD, double mass);

        void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio, double mass);

        double RequestedThermalThrottle { get; }

        double RequestedPlasmaThrottle { get; }

        double RequestedChargedThrottle { get; }

        double NormalizedPowerMultiplier { get; }

        double PlasmaAfterburnerRange { get; }

        double EngineHeatProductionMult { get; }

        double PlasmaHeatProductionMult { get; }

        double EngineWasteheatProductionMult { get; }

        double PlasmaWasteheatProductionMult { get; }

        double MinCoolingFactor { get; }

        bool CanProducePower { get; }

        float DefaultPowerGeneratorPercentage { get; }

        double FuelRatio { get; }

        double MinThermalNozzleTempRequired { get; }

        bool CanUseAllPowerForPlasma { get; }

        double CurrentPlasmaPropulsionRatio { get; }

        double CurrentChargedPropulsionRatio { get; }

        bool UsePropellantBaseIsp { get; }

        double CurrentMeVPerChargedProduct { get; }

        bool MayExhaustInAtmosphereHomeworld { get; }

        bool MayExhaustInLowSpaceHomeworld { get; }

        double MagneticNozzlePowerMult { get; }

        double MagneticNozzleMhdMult { get; }

        void UseProductForPropulsion(double ratio, double propellantMassPerSecond, PartResourceDefinition[] resource);

        double RawMaximumPowerForPowerGeneration { get; }

        double MaxCoreTemperature { get; }

        void UpdateAuxiliaryPowerSource(double available);

        bool IsConnectedToChargedGenerator { get; }

        bool IsConnectedToThermalGenerator { get; }
    }
}
