using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace FNPlugin
{
    public static class Kerbalism
    {
        public static int versionMajor;
        public static int versionMinor;
        public static int versionMajorRevision;
        public static int versionMinorRevision;

        static Assembly KerbalismAssembly;
        static Type Sim;
        static MethodInfo VesselTemperature;

        static Kerbalism()
        {
            Debug.Log("[KSPI]: Looking for Kerbalism Assembly");
            Debug.Log("[KSPI]: AssemblyLoader.loadedAssemblies contains " + AssemblyLoader.loadedAssemblies.Count + " assemblies");
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                //Debug.Log("[KSPI]: current Assembly: " + assemby.name);

                if (loadedAssembly.name.StartsWith("Kerbalism") && loadedAssembly.name.EndsWith("Bootstrap") == false)
                {
                    Debug.Log("[KSPI]: Found " + loadedAssembly.name + " Assembly");

                    KerbalismAssembly = loadedAssembly.assembly;

                    AssemblyName assemblyName = KerbalismAssembly.GetName();

                    versionMajor = assemblyName.Version.Major;
                    versionMinor = assemblyName.Version.Minor;
                    versionMajorRevision = assemblyName.Version.MajorRevision;
                    versionMinorRevision = assemblyName.Version.MinorRevision;

                    try
                    {
                        Sim = KerbalismAssembly.GetType("KERBALISM.Sim");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

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

                    break;
                }
            }
        }
    }
}
