using System;
using FNPlugin.Propulsion;

namespace FNPlugin
{
    public enum ElectricGeneratorType { unknown = 0, thermal = 1, charged_particle = 2 };

    public interface IThermalReciever
    {
        void AttachThermalReciever(Guid key, float radius);

        void DetachThermalReciever(Guid key);

        float GetFractionThermalReciever(Guid key);

        float ThermalTransportationEfficiency { get; }
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
        float ThermalProcessingModifier { get; }

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

        float GetRadius();

        bool IsNuclear { get; }

		void EnableIfPossible();

        bool shouldScaleDownJetISP();

        double GetCoreTempAtRadiatorTemp(double rad_temp);

        double GetThermalPowerAtTemp(double temp);

        bool IsThermalSource { get; }

        float ThermalPropulsionEfficiency { get; }

        float ThermalEnergyEfficiency { get; }

        float ChargedParticleEnergyEfficiency { get; }

        float ChargedParticlePropulsionEfficiency { get; }

        double EfficencyConnectedThermalEnergyGenerator { get; }

        double EfficencyConnectedChargedEnergyGenerator { get; }

        IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        void NotifyActiveThermalEnergyGenerator(double efficency, ElectricGeneratorType generatorType);

        void NotifyActiveChargedEnergyGenerator(double efficency, ElectricGeneratorType generatorType);

        bool ShouldApplyBalance(ElectricGeneratorType generatorType);

        void ConnectWithEngine(IEngineNoozle engine);

        void DisconnectWithEngine(IEngineNoozle engine);
	}
}

