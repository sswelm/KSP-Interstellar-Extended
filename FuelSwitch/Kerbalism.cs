using System;
using System.Reflection;
using UnityEngine;

namespace InterstellarFuelSwitch
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
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (!loadedAssembly.name.StartsWith("Kerbalism") ||
                    loadedAssembly.name.EndsWith("Bootstrap")) continue;

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
                Debug.Log("[IFS]: Found Kerbalism assemblyName Version " + kerbalismVersionStr);

                Type simulation = null;

                try { simulation = kerbalismAssembly.GetType("KERBALISM.Sim"); } catch (Exception e) { Debug.LogException(e); }

                if (simulation != null)
                {
                    Debug.Log("[IFS]: Found KERBALISM.Sim");
                    try
                    {
                        var vesselTemperature = simulation.GetMethod("Temperature");
                        if (vesselTemperature != null)
                            Debug.Log("[IFS]: Found KERBALISM.Sim.Temperature Method");
                        else
                            Debug.LogError("[IFS]: Failed to find KERBALISM.Sim.Temperature Method");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                else
                    Debug.LogError("[IFS]: Failed to find KERBALISM.Sim");

                return;
            }
            Debug.Log("[IFS]: KERBALISM was not found");
        }

        public static bool IsLoaded { get; }
    }
}
