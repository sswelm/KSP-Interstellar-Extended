using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FNPlugin.Propulsion;
using FNPlugin.Redist;

namespace FNPlugin.Wasteheat
{
    class FNHeatPump : ResourceSuppliableModule, IFNPowerSource
    {
        [KSPField(isPersistant = false)]
        public double heatTransportationEfficiency = 0.7f;
        [KSPField(isPersistant = false)]
        public double maximumPower = 1;

        // Control
        [KSPField(isPersistant = true, guiActive = true, guiName = "Minimum Consumption %"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]
        public float minimumConsumptionPercentage = 0;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Maximum Consumption %"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]
        public float maximumConsumptionPercentage = 100;


        public double ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }
        public Part Part { get { return part; } }
        public int ProviderPowerPriority { get { return 1; } }
        public double RawMaximumPower { get { return maximumPower; } }
        public bool SupportMHD { get { return false; } }
        public double PowerRatio { get { return maximumConsumptionPercentage / 100d; } }
        public double RawTotalPowerProduced { get { return ThermalPower * TimeWarp.fixedDeltaTime; } }


        public double ThermalPower { get; private set; }


        public double ThermalProcessingModifier { get; }
        public int SupportedPropellantAtoms { get; }
        public int SupportedPropellantTypes { get; }
        public bool FullPowerForNonNeutronAbsorbants { get; }
        public double ProducedThermalHeat { get; }
        public double ProducedChargedPower { get; }
        public double ProducedWasteHeat { get; }
        public double PowerBufferBonus { get; }
        public double StableMaximumReactorPower { get; }
        public double MinimumThrottle { get; }
        public double MaximumPower { get; }
        public double MinimumPower { get; }
        public double ChargedPowerRatio { get; }
        public double NormalisedMaximumPower { get; }
        public double MaximumThermalPower { get; }
        public double MaximumChargedPower { get; }
        public double CoreTemperature { get; }
        public double HotBathTemperature { get; }
        public bool IsSelfContained { get; }
        public bool IsActive { get; }
        public bool IsVolatileSource { get; }
        public double Radius { get; }
        public bool IsNuclear { get; }

        public void EnableIfPossible()
        {
        }

        public bool shouldScaleDownJetISP()
        {
            return true;
        }

        public double GetCoreTempAtRadiatorTemp(double radTemp)
        {
            return part.temperature;
        }

        public double GetThermalPowerAtTemp(double temp)
        {
            return part.temperature;
        }

        public bool IsThermalSource { get; }
        public double ConsumedFuelFixed { get; }
        public double ThermalPropulsionWasteheatModifier { get; }
        public double ThermalPropulsionEfficiency { get; }
        public double PlasmaPropulsionEfficiency { get; }
        public double ChargedParticlePropulsionEfficiency { get; }
        public double ThermalEnergyEfficiency { get; }
        public double PlasmaEnergyEfficiency { get; }
        public double ChargedParticleEnergyEfficiency { get; }
        public double EfficencyConnectedThermalEnergyGenerator { get; }
        public double EfficencyConnectedChargedEnergyGenerator { get; }
        public double ReactorSpeedMult { get; }
        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }
        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }
        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio)
        {

        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio)
        {

        }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType)
        {
            return false;
        }

        public void ConnectWithEngine(IEngineNoozle engine)
        {

        }

        public void DisconnectWithEngine(IEngineNoozle engine)
        {

        }

        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, bool isMHD, double mass)
        {

        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio, double mass)
        {

        }

        public double EngineHeatProductionMult { get; }
        public double PlasmaHeatProductionMult { get; }
        public double EngineWasteheatProductionMult { get; }
        public double PlasmaWasteheatProductionMult { get; }
        public double MinCoolingFactor { get; }
        public bool CanProducePower { get; }
        public double FuelRato { get; }
        public double MinThermalNozzleTempRequired { get; }
        public bool CanUseAllPowerForPlasma { get; }
        public bool UsePropellantBaseIsp { get; }
        public double CurrentMeVPerChargedProduct { get; }
        public bool MayExhaustInAtmosphereHomeworld { get; }
        public bool MayExhaustInLowSpaceHomeworld { get; }
        public double MagneticNozzlePowerMult { get; }

        public void UseProductForPropulsion(double ratio, double propellantMassPerSecond, PartResourceDefinition resource)
        {

        }

        public double RawMaximumPowerForPowerGeneration { get; }
        public double MaxCoreTemperature { get; }
    }
}
