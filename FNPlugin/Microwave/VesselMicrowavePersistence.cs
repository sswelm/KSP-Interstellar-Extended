using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    class VesselMicrowavePersistence
    {
        Vessel vessel;
        private bool isactive;

        private double aperture;
        private double nuclear_power;
        private double solar_power;
        private double power_capacity;

        public VesselMicrowavePersistence(Vessel vessel)
        {
            this.vessel = vessel;
            SupportedTransmitWavelengths = new List<WaveLengthData>();
        }

        public double getAvailablePower()
        {
            double power = 0;
            if (solar_power > 0.001 && PluginHelper.lineOfSightToSun(vessel))
            {
                var distanceBetweenVesselAndSun = Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                var distanceBetweenSunAndKerbin = Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                double inv_square_mult = Math.Pow(distanceBetweenVesselAndSun, 2) / Math.Pow(distanceBetweenSunAndKerbin, 2);
                power = solar_power / inv_square_mult;
            }

            power += nuclear_power;

            var finalpower = Math.Min(1000 * power_capacity, power);

            return finalpower;
        }

        public Vessel Vessel
        {
            get { return this.vessel; }
        }

        public bool IsActive
        {
            get { return this.isactive; }
            set { this.isactive = value; }
        }

        public double SolarPower
        {
            get { return this.solar_power; }
            set { this.solar_power = value; }
        }

        public double NuclearPower
        {
            get { return this.nuclear_power; }
            set { this.nuclear_power = value; }
        }

        public double Aperture
        {
            get { return aperture != 0 ? this.aperture : 5; }
            set { this.aperture = value; }
        }

        public double PowerCapacity
        {
            get { return power_capacity != 0 ? this.power_capacity : 2; }
            set { this.power_capacity = value; }
        }

        public List<WaveLengthData> SupportedTransmitWavelengths { get; private set; }
    }
}
