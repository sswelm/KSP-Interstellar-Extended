using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Propulsion;

namespace FNPlugin.Redist
{
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

        double ProducedChargedPower { get; }

        //double RequestedThermalHeat { get; set; }

        double MaxCoreTemperature { get; }

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

        double Radius { get; }

        bool IsNuclear { get; }

        void EnableIfPossible();

        bool shouldScaleDownJetISP();

        double GetCoreTempAtRadiatorTemp(double radTemp);

        double GetThermalPowerAtTemp(double temp);

        bool IsThermalSource { get; }

        double ConsumedFuelFixed { get; }

        double ThermalPropulsionWasteheatModifier { get; }

        double ThermalPropulsionEfficiency { get; }
        double PlasmaPropulsionEfficiency { get; }
        double ChargedParticlePropulsionEfficiency { get; }

        double ThermalEnergyEfficiency { get; }
        double PlasmaEnergyEfficiency { get; }
        double ChargedParticleEnergyEfficiency { get; }

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
