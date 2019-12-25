using System;
using System.Collections.Generic;
using FNPlugin.Resources;

namespace FNPlugin.Extensions
{
    public static class CelestialBodyExtensions
    {
        static Dictionary<string, BeltData> BeltDataCache = new Dictionary<string, BeltData>();

        class BeltData
        {
            public double density;
            public double ampere;
        }

        const double sqrt2 = 1.4142135624;
        const double sqrt2divPi = 0.79788456080;

        public static double GetBeltAntiparticles(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            if (body.flightGlobalsIndex != 0 && altitude <= PluginHelper.getMaxAtmosphericAltitude(body))  
                return 0;

            BeltData beltdata;

            if (!BeltDataCache.TryGetValue(body.name, out beltdata))
            {
                double relrp = body.Radius / homeworld.Radius;
                double relrt = body.rotationPeriod / homeworld.rotationPeriod;

                beltdata = new BeltData() 
                {
                    density = body.Mass / homeworld.Mass * relrp / relrt * 50,
                    ampere = 1.5 * homeworld.Radius * relrp / sqrt2, 
                };

                BeltDataCache.Add(body.name, beltdata);
            }

            double beltparticles = beltdata.density
                * sqrt2divPi 
                * Math.Pow(altitude, 2)
                * Math.Exp(-Math.Pow(altitude, 2) / (2 * Math.Pow(beltdata.ampere, 2))) 
                / (Math.Pow(beltdata.ampere, 3));

            if (KopernicusHelper.GetLuminocity(body) > 0)
                beltparticles /= 1000;

            if (body.atmosphere)
                beltparticles *= body.atmosphereDepth / 70000;
            else
                beltparticles *= 0.01;

            return beltparticles * Math.Abs(Math.Cos(lat / 180 * Math.PI)) * body.specialMagneticFieldScaling();
        }


        public static double GetProtonRadiationLevel(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;

            double atmosphere = FlightGlobals.getStaticPressure(altitude, body) / 101.325;
            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;
            double peakbelt = body.GetPeakProtonBeltAltitude(homeworld, altitude, lat);
            double altituded = altitude;
            double a = peakbelt / sqrt2;
            double beltparticles = sqrt2divPi * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2 * Math.Pow(a, 2))) / (Math.Pow(a, 3));
            beltparticles = beltparticles * relrp / relrt * 50;

            if (KopernicusHelper.IsStar(body))
                beltparticles /= 1000;

            if (body.atmosphere)
                beltparticles *= body.atmosphereDepth / 70000;
            else
                beltparticles *= 0.01;

            beltparticles = beltparticles * Math.Abs(Math.Cos(lat)) * body.specialMagneticFieldScaling() * Math.Exp(-atmosphere);

            return beltparticles;
        }

        public static double GetPeakProtonBeltAltitude(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            double relrp = body.Radius / homeworld.Radius;
            double peakbelt = 1.5 * homeworld.Radius * relrp;
            return peakbelt;
        }

        public static double GetElectronRadiationLevel(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            double atmosphere = FlightGlobals.getStaticPressure(altitude, body) / 101.325;
            double atmosphere_height = PluginHelper.getMaxAtmosphericAltitude(body);
            double atmosphere_scaling = Math.Exp(-atmosphere);

            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double peakbelt2 = body.GetPeakElectronBeltAltitude(homeworld, altitude, lat);
            double altituded = altitude;
            double b = peakbelt2 / sqrt2;
            double beltparticles = 0.9 * sqrt2divPi * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2 * Math.Pow(b, 2))) / (Math.Pow(b, 3));
            beltparticles = beltparticles * relrp / relrt * 50;

            if (KopernicusHelper.IsStar(body))
                beltparticles /= 1000;

            if (body.atmosphere)
                beltparticles *= body.atmosphereDepth / 70000;
            else
                beltparticles *= 0.01;

            beltparticles = beltparticles * Math.Abs(Math.Cos(lat)) * body.specialMagneticFieldScaling() * atmosphere_scaling;

            return beltparticles;
        }

        public static double GetPeakElectronBeltAltitude(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            double relrp = body.Radius / homeworld.Radius;
            double peakbelt = 6.0 * homeworld.Radius * relrp;
            return peakbelt;
        }

        public static double specialMagneticFieldScaling(this CelestialBody body)
        {
            return MagneticFieldDefinitionsHandler.GetMagneticFieldDefinitionForBody(body.name).StrengthMult; 
        }

        public static double GetBeltMagneticFieldMagnitude(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;

            double relmp = body.Mass / homeworld.Mass;
            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double altituded = altitude + body.Radius;
            double Bmag = VanAllen.B0 / relrt * relmp * Math.Pow((body.Radius / altituded), 3) * Math.Sqrt(1 + 3 * Math.Pow(Math.Cos(mlat), 2)) * body.specialMagneticFieldScaling();

            if (KopernicusHelper.IsStar(body))
                Bmag /= 1000;

            if (body.atmosphere)
                Bmag *= body.atmosphereDepth / 70000;
            else
                Bmag *= 0.01;

            return Bmag;
        }

        public static double GetBeltMagneticFieldRadial(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;

            double relmp = body.Mass / homeworld.Mass;
            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double altituded = altitude + body.Radius;
            double Bmag = -2 / relrt * relmp * VanAllen.B0 * Math.Pow((body.Radius / altituded), 3) * Math.Cos(mlat) * body.specialMagneticFieldScaling();

            if (KopernicusHelper.GetLuminocity(body) > 0)
                Bmag /= 1000;

            if (body.atmosphere)
                Bmag *= body.atmosphereDepth / 70000;
            else
                Bmag *= 0.01;

            return Bmag;
        }

        public static double getBeltMagneticFieldAzimuthal(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;

            double relmp = body.Mass / homeworld.Mass;
            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double altituded = altitude + body.Radius;
            double Bmag = -relmp * VanAllen.B0 / relrt * Math.Pow((body.Radius / altituded), 3) * Math.Sin(mlat) * body.specialMagneticFieldScaling();

            if (KopernicusHelper.IsStar(body))
                Bmag /= 1000;

            if (body.atmosphere)
                Bmag *= body.atmosphereDepth / 70000;
            else
                Bmag *= 0.01;

            return Bmag;
        }
    }
}
