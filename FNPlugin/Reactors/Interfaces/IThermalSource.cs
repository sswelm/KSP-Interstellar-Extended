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


    public interface IThermalSource : IThermalReciever
    {
        Part Part { get; }


        /// <summary>
        /// // The absolute maximum amount of power the thermalsource can possbly produce
        /// </summary>
        float RawMaximumPower { get; }



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

        float PowerBufferBonus { get; }

        double StableMaximumReactorPower { get; }

        float MinimumThrottle { get; }

        double MaximumPower { get; }

        double MinimumPower { get; }

        double ChargedPowerRatio { get; }

        double MaximumThermalPower { get; }

        double MaximumChargedPower { get; }

        float CoreTemperature { get; }

        float HotBathTemperature { get; }

        bool IsSelfContained { get; }

        bool IsActive { get; }

        bool IsVolatileSource { get; }

        float GetRadius();

        bool IsNuclear { get; }

		void EnableIfPossible();

        bool shouldScaleDownJetISP();

        float GetCoreTempAtRadiatorTemp(float rad_temp);

        double GetThermalPowerAtTemp(float temp);

        bool IsThermalSource { get; }

        float ThermalPropulsionEfficiency { get; }

        float ThermalEnergyEfficiency { get; }

        float ChargedParticleEnergyEfficiency { get; }

        float ChargedParticlePropulsionEfficiency { get; }

        double EfficencyConnectedThermalEnergyGenerator { get; }

        double EfficencyConnectedChargedEnergyGenerator { get; }

        IElectricPowerSource ConnectedThermalElectricGenerator { get; set; }

        IElectricPowerSource ConnectedChargedParticleElectricGenerator { get; set; }

        void NotifyActiveThermalEnergyGenrator(double efficency, ElectricGeneratorType generatorType);

        void NotifyActiveChargedEnergyGenrator(double efficency, ElectricGeneratorType generatorType);

        bool ShouldApplyBalance(ElectricGeneratorType generatorType);

        void ConnectWithEngine(IEngineNoozle engine);


	}
}

