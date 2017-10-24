using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace FNPlugin
{
    public static class VesselExtensions
    {
        public static void GetVesselAndModuleMass<T>(this Vessel vessel, out float totalmass, out float modulemass) where T : class
        {
            totalmass = 0;
            modulemass = 0;

            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
            foreach (var currentPart in parts)
            {
                totalmass += currentPart.mass;
                if (currentPart.FindModuleImplementing<T>() != null)
                    modulemass += currentPart.mass;
            }
        }

        public static bool HasAnyModulesImplementing<T>(this Vessel vessel) where T: class
        {
            return vessel.FindPartModulesImplementing<T>().Any();
        }

        public static bool IsInAtmosphere(this Vessel vessel)
        {
            if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) return true;
            return false;
        }

        public static double GetTemperatureofColdestThermalSource(this Vessel vess)
        {
            List<IPowerSource> active_reactors = vess.FindPartModulesImplementing<IPowerSource>().Where(ts => ts.IsActive && ts.IsThermalSource).ToList();
            return active_reactors.Any() ? active_reactors.Min(ts => ts.CoreTemperature) : double.MaxValue;
        }

        public static double GetAverageTemperatureofOfThermalSource(this Vessel vess)
        {
            List<IPowerSource> active_reactors = vess.FindPartModulesImplementing<IPowerSource>().Where(ts => ts.IsActive && ts.IsThermalSource).ToList();
            return active_reactors.Any() ? active_reactors.Sum(r => r.HotBathTemperature) / active_reactors.Count : 0;
        }

        public static bool HasAnyActiveThermalSources(this Vessel vess) 
        {
            return vess.FindPartModulesImplementing<IPowerSource>().Where(ts => ts.IsActive).Any();
        }
    }
}
