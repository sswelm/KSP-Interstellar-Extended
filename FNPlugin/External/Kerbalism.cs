using System;
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

        static Kerbalism()
        {
            Debug.Log("[KSPI]: Looking for Kerbalism Assembly");
            Debug.Log("[KSPI]: AssemblyLoader.loadedAssemblies contains " + AssemblyLoader.loadedAssemblies.Count + " assemblies");
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (!loadedAssembly.name.StartsWith("Kerbalism") ||
                    loadedAssembly.name.EndsWith("Bootstrap")) continue;

                Debug.Log("[KSPI]: Found " + loadedAssembly.name + " Assembly");

                var kerbalismAssembly = loadedAssembly.assembly;

                AssemblyName assemblyName = kerbalismAssembly.GetName();

                versionMajor = assemblyName.Version.Major;
                versionMinor = assemblyName.Version.Minor;
                versionMajorRevision = assemblyName.Version.MajorRevision;
                versionMinorRevision = assemblyName.Version.MinorRevision;
                versionBuild = assemblyName.Version.Build;
                versionRevision = assemblyName.Version.Revision;

                IsLoaded = versionMajor > 0;

                var kerbalismVersionStr = $"{versionMajor}.{versionMinor}.{versionRevision}.{versionBuild}.{versionMajorRevision}.{versionMinorRevision}";
                Debug.Log("[KSPI]: Found Kerbalism assemblyName Version " + kerbalismVersionStr);

                Type simulation = null;

                try { simulation = kerbalismAssembly.GetType("KERBALISM.Sim"); } catch (Exception e) { Debug.LogException(e); }

                if (simulation != null)
                {
                    Debug.Log("[KSPI]: Found KERBALISM.Sim");
                    try
                    {
                        var vesselTemperature = simulation.GetMethod("Temperature");
                        if (vesselTemperature != null)
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
            Debug.Log("[KSPI]: KERBALISM was not found");
        }

        public static bool IsLoaded { get; }

        public static bool HasRadiationFixes => versionMajor >= 3 && versionMinor >= 1;

        // return proportion of ionizing radiation not blocked by atmosphere
        public static double GammaTransparency(CelestialBody body, double altitude)
        {
            // deal with underwater & fp precision issues
            altitude = Math.Abs(altitude);

            // get pressure
            double staticPressure = body.GetPressure(altitude);
            if (staticPressure > 0.0)
            {
                // get density
                double density = body.GetDensity(staticPressure, body.GetTemperature(altitude));

                // math, you know
                double radius = body.Radius + altitude;
                double depth = body.atmosphereDepth - altitude;
                double path = Math.Sqrt(radius * radius + 2.0 * radius * depth + depth * depth) - radius;
                double factor = body.GetSolarPowerFactor(density) * depth / path;

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
