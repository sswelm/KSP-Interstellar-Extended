using FNPlugin.Redist;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Extensions
{
    public static class VesselExtensions
    {
        /// <summary>Tests whether two vessels have line of sight to each other</summary>
        /// <returns><c>true</c> if a straight line from a to b is not blocked by any celestial body; 
        /// otherwise, <c>false</c>.</returns>
        public static bool HasLineOfSightWith(this Vessel vessA, Vessel vessB, double freeDistance = 2500, double min_height = double.NaN)
        {
            Vector3d vesselA = vessA.transform.position;
            Vector3d vesselB = vessB.transform.position;

            if (freeDistance > 0 && Vector3d.Distance(vesselA, vesselB) < freeDistance)           // if both vessels are within active view
                return true;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                Vector3d bodyFromA = referenceBody.position - vesselA;
                Vector3d bFromA = vesselB - vesselA;

                // Is body at least roughly between satA and satB?
                if (Vector3d.Dot(bodyFromA, bFromA) <= 0) continue;

                Vector3d bFromANorm = bFromA.normalized;

                if (Vector3d.Dot(bodyFromA, bFromANorm) >= bFromA.magnitude) continue;

                // Above conditions guarantee that Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm 
                // lies between the origin and bFromA
                Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm;

                var effective_minimum_height = double.IsNaN(min_height) ? (referenceBody.atmosphere ? 5 : -500) : min_height;

                if (lateralOffset.magnitude < referenceBody.Radius - effective_minimum_height) return false;
            }
            return true;
        }

        public static bool LineOfSightToSun(this Vessel vessel, CelestialBody star)
        {
            Vector3d vesselPosition = vessel.GetVesselPos();
            Vector3d startPosition = star.position;

            return vesselPosition.LineOfSightToSun(startPosition);
        }

         /** 
         * This function should allow this module to work in solar systems other than the vanilla KSP one as well. Credit to Freethinker's MicrowavePowerReceiver code.
         * It checks current reference body's temperature at 0 altitude. If it is less than 2k K, it checks this body's reference body next and so on.
         */
        public static CelestialBody GetLocalStar(this Vessel vessel)
        {
            //if (Sun.Instance != null && Sun.Instance.sun != null)
            //    return Sun.Instance.sun;

            //var planetarium = Planetarium.fetch;
            //if (planetarium != null)
            //	return planetarium.Sun;

            var iDepth = 0;
            var star = vessel.mainBody;

            while ((iDepth < 10) && (star.GetTemperature(0) < 2000))
            {
                //if (star.name == "Valentine")
                //    return star;

                star = star.referenceBody;
                iDepth++;
            }
            if ((star.GetTemperature(0) < 2000) || (star.name == "Galactic Core"))
                star = null;

            return star;
        }

        public static Vector3d GetVesselPos(this Vessel v)
        {
            return (v.state == Vessel.State.ACTIVE)
                ? v.CoMD
                : v.GetWorldPos3D();
        }

        public static List<T> GetVesselAndModuleMass<T>(this Vessel vessel, out double totalmass, out double modulemass) where T : class
        {
            totalmass = 0;
            modulemass = 0;

            var modulelist = new List<T>();

            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
            foreach (var currentPart in parts)
            {
                totalmass += currentPart.mass;
                var module = currentPart.FindModuleImplementing<T>();
                if (module == null) continue;

                modulemass += currentPart.mass;
                modulelist.Add(module);
            }
            return modulelist;
        }

        public static bool HasAnyModulesImplementing<T>(this Vessel vessel) where T: class
        {
            return vessel.FindPartModulesImplementing<T>().Any();
        }

        public static bool IsInAtmosphere(this Vessel vessel)
        {
            if (vessel.altitude <= vessel.mainBody.atmosphereDepth) return true;
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
