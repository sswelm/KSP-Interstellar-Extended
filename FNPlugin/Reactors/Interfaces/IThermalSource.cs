using System;
using FNPlugin.Propulsion;

namespace FNPlugin
{
    public enum ElectricGeneratorType { unknown = 0, thermal = 1, charged_particle = 2 };

    public interface IThermalReciever
    {
        void AttachThermalReciever(Guid key, double radius);

        void DetachThermalReciever(Guid key);

        double GetFractionThermalReciever(Guid key);

        double ThermalTransportationEfficiency { get; }
    }


    public interface IPowerSource : IThermalReciever
    {
        Part Part { get; }

        int ProviderPowerPriority { get; }

        /// <summary>
        /// // The absolute maximum amount of power the thermalsource can possbly produce
        /// </summary>
        double RawMaximumPower { get; }

        bool SupportMHD { get; }

        double PowerRatio { get; }

        double RawTotalPowerProduced { get; }

        /// <summary>
        /// Influences the Mass in Electric Generator
        /// </summary>
        double ThermalProcessingModifier { get; }

        int SupportedPropellantAtoms { get; }

        int SupportedPropellantTypes { get; }

        bool FullPowerForNonNeutronAbsorbants { get; }

        double ProducedThermalHeat { get; }

        double RequestedThermalHeat { get; set; }

        double ProducedWasteHeat { get; }

        double PowerBufferBonus { get; }

        double StableMaximumReactorPower { get; }

        double MinimumThrottle { get; }

        double MaximumPower { get; }

        double MinimumPower { get; }

        double ChargedPowerRatio { get; }

        double NormalisedMaximumPower { get; }

        double MaximumThermalPower { get; }

        double MaximumChargedPower { get; }

        double CoreTemperature { get; }

        double HotBathTemperature { get; }

        bool IsSelfContained { get; }

        bool IsActive { get; }

        bool IsVolatileSource { get; }

        double Radius {get; }

        bool IsNuclear { get; }

        void EnableIfPossible();

        bool shouldScaleDownJetISP();

        double GetCoreTempAtRadiatorTemp(double rad_temp);

        double GetThermalPowerAtTemp(double temp);

        bool IsThermalSource { get; }

        double ThermalPropulsionEfficiency { get; }

        double ThermalEnergyEfficiency { get; }

        double ChargedParticleEnergyEfficiency { get; }

        double ChargedParticlePropulsionEfficiency { get; }

        double EfficencyConnectedThermalEnergyGenerator { get; }

        double EfficencyConnectedChargedEnergyGenerator { get; }

        double ReactorSpeedMult { get; }

        IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio);

        void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio);

        bool ShouldApplyBalance(ElectricGeneratorType generatorType);

        void ConnectWithEngine(IEngineNoozle engine);

        void DisconnectWithEngine(IEngineNoozle engine);

        
    }
}

