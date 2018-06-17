using System;

namespace FNPlugin.Redist
{
    public enum ElectricGeneratorType { unknown = 0, thermal = 1, charged_particle = 2 };

    public interface IThermalReciever
    {
        void AttachThermalReciever(Guid key, double radius);

        void DetachThermalReciever(Guid key);

        double GetFractionThermalReciever(Guid key);

        double ThermalTransportationEfficiency { get; }
    }
}

