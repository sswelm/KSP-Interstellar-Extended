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
        public static int versionRevision;

        static Assembly KerbalismAssembly;
        static Type Sim;

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

                    versionMajor = loadedAssembly.versionMajor;
                    versionMinor = loadedAssembly.versionMinor;
                    versionRevision = loadedAssembly.versionRevision;

                    try
                    {
                        Sim = KerbalismAssembly.GetType("KERBALISM.Sim");
                        if (Sim != null)
                            Debug.Log("[KSPI]: Found Sim");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    //RegisterSensor = SCANUtils.GetMethod("registerSensorExternal");
                    //UnregisterSensor = SCANUtils.GetMethod("unregisterSensorExternal");
                    //GetCoverage = SCANUtils.GetMethod("GetCoverage");
                    break;
                }
            }
        }
    }
}
