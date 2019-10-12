using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Propulsion;

namespace FNPlugin.Redist
{
    public interface IFNPowerSource : IPowerSource
    {
        void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, bool isMHD, double mass);

        void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio, double mass);

        double EngineHeatProductionMult { get; }

        double PlasmaHeatProductionMult { get; }

        double EngineWasteheatProductionMult { get; }

        double PlasmaWasteheatProductionMult { get; }

        double MinCoolingFactor { get; }

        bool CanProducePower { get; }

        double FuelRato { get; }

        double MinThermalNozzleTempRequired { get; }

        bool CanUseAllPowerForPlasma { get; }

        bool UsePropellantBaseIsp { get; }

        double CurrentMeVPerChargedProduct { get; }

        bool MayExhaustInAtmosphereHomeworld { get; }

        bool MayExhaustInLowSpaceHomeworld { get; }

        double MagneticNozzlePowerMult { get; }

        void UseProductForPropulsion(double ratio, double propellantMassPerSecond, PartResourceDefinition resource);

        void Activate();
    }
}
