using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace FNPlugin
{
    public static class Kerbalism
    {
        public static int versionMajor;
        public static int versionMinor;
        public static int versionMajorRevision;
        public static int versionMinorRevision;
        public static int versionBuild;
        public static int versionRevision;

        static Assembly KerbalismAssembly;
        static Type Sim;
        static MethodInfo VesselTemperature;

        static Kerbalism()
        {
            Debug.Log("[KSPI]: Looking for Kerbalism Assembly");
            Debug.Log("[KSPI]: AssemblyLoader.loadedAssemblies contains " + AssemblyLoader.loadedAssemblies.Count + " assemblies");
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name.StartsWith("Kerbalism") && loadedAssembly.name.EndsWith("Bootstrap") == false)
                {
                    Debug.Log("[KSPI]: Found " + loadedAssembly.name + " Assembly");

                    KerbalismAssembly = loadedAssembly.assembly;

                    AssemblyName assemblyName = KerbalismAssembly.GetName();

                    versionMajor = assemblyName.Version.Major;
                    versionMinor = assemblyName.Version.Minor;
                    versionMajorRevision = assemblyName.Version.MajorRevision;
                    versionMinorRevision = assemblyName.Version.MinorRevision;
                    versionBuild = assemblyName.Version.Build;
                    versionRevision = assemblyName.Version.Revision;

                    var kerbalismversionstr = string.Format("{0}.{1}.{2}.{3}.{4}.{5}", versionMajor, versionMinor, versionRevision, versionBuild, versionMajorRevision, versionMinorRevision);
                    Debug.Log("[KSPI]: Found Kerbalism assemblyName Version " + kerbalismversionstr);

                    try { Sim = KerbalismAssembly.GetType("KERBALISM.Sim"); } catch (Exception e) { Debug.LogException(e); }

                    if (Sim != null)
                    {
                        Debug.Log("[KSPI]: Found KERBALISM.Sim");
                        try
                        {
                            VesselTemperature = Sim.GetMethod("Temperature");
                            if (VesselTemperature != null)
                                Debug.Log("[KSPI]: Found KERBALISM.Sim.Temperature Method");
                            else
                                Debug.LogError("[KSPI]: Failed to find KERBALISM.Sim.Temperature Method");
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    else
                        Debug.LogError("[KSPI]: Failed to find KERBALISM.Sim");

                    return;
                }
            }
            Debug.Log("[KSPI]: KERBALISM was not found");
        }

        public static bool IsLoaded
        {
            get { return versionMajor > 0; }
        }

        public static bool HasRadiationFixes
        {
            get { return versionMajor >= 3 && versionMinor >= 1; }
        }

        // return proportion of ionizing radiation not blocked by atmosphere
        public static double GammaTransparency(CelestialBody body, double altitude)
        {
            // deal with underwater & fp precision issues
            altitude = Math.Abs(altitude);

            // get pressure
            double static_pressure = body.GetPressure(altitude);
            if (static_pressure > 0.0)
            {
                // get density
                double density = body.GetDensity(static_pressure, body.GetTemperature(altitude));

                // math, you know
                double Ra = body.Radius + altitude;
                double Ya = body.atmosphereDepth - altitude;
                double path = Math.Sqrt(Ra * Ra + 2.0 * Ra * Ya + Ya * Ya) - Ra;
                double factor = body.GetSolarPowerFactor(density) * Ya / path;

                // poor man atmosphere composition contribution
                if (body.atmosphereContainsOxygen || body.ocean)
                {
                    factor = 1.0 - Math.Pow(1.0 - factor, 0.015);
                }
                return factor;
            }
            return 1.0;
        }
    }
}
