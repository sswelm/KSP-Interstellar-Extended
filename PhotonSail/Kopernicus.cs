using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PhotonSail
{
    class StarLight
    {
        public CelestialBody star;
        public Vector3d position;
        public double relativeLuminocity;
        public double solarFlux;
        public bool hasLineOfSight;
    }

    static class KopernicusHelper
    {
        /// <summary>
        /// Retrieves list of Starlight data
        /// </summary>
        static public List<StarLight> Stars
        {
            get
            {
                if (stars == null)
                    stars = ExtractStarData(moduleName);
                return stars;
            }
        }

        public static bool IsStar(CelestialBody body)
        {
            return GetLuminocity(body) > 0;
        }

        public static double GetLuminocity(CelestialBody body)
        {
            if (stars == null )
                stars = ExtractStarData(moduleName);
            if (starsByBody == null)
                starsByBody = stars.ToDictionary(m => m.star);

            StarLight starlight;
            if (starsByBody.TryGetValue(body, out starlight))
                return starlight.relativeLuminocity;
            else
                return 0;
        }

        static List<StarLight> stars;
        static Dictionary<CelestialBody, StarLight> starsByBody;

        const string moduleName = "PhotonSailor";
        const double kerbinAU = 13599840256;
        const double kerbalLuminocity = 3.1609409786213e+24;


        /// <summary>
        /// // Scan the Kopernicus config nodes and extract Kopernicus star data
        /// </summary>
        /// <param name="modulename"></param>
        /// <returns></returns>
        private static List<StarLight> ExtractStarData(string modulename)
        {
            var debugPrefix = "[" + modulename + "] - ";

            List<StarLight> stars = new List<StarLight>();

            var celestrialBodiesByName = FlightGlobals.Bodies.ToDictionary(m => m.name);

            ConfigNode[] kopernicusNodes = GameDatabase.Instance.GetConfigNodes("Kopernicus");

            if (kopernicusNodes.Length > 0)
                Debug.Log(debugPrefix + "Loading Kopernicus Configuration Data");
            else
                Debug.LogWarning(debugPrefix + "Failed to find Kopernicus Configuration Data");

            for (int i = 0; i < kopernicusNodes.Length; i++)
            {
                ConfigNode[] bodies = kopernicusNodes[i].GetNodes("Body");

                Debug.Log(debugPrefix + "Found " + bodies.Length + " celestrial bodies");

                for (int j = 0; j < bodies.Length; j++)
                {
                    ConfigNode currentBody = bodies[j];

                    string bodyName = currentBody.GetValue("name");

                    CelestialBody celestialBody = null;

                    celestrialBodiesByName.TryGetValue(bodyName, out celestialBody);
                    if (celestialBody == null)
                    {
                        Debug.LogWarning(debugPrefix + "Failed to find celestrialbody " + bodyName);
                        continue;
                    }

                    double solarLuminocity = 0;
                    bool usesSunTemplate = false;

                    ConfigNode sunNode = currentBody.GetNode("Template");
                    if (sunNode != null)
                    {
                        string templateName = sunNode.GetValue("name");
                        usesSunTemplate = templateName == "Sun";
                        if (usesSunTemplate)
                            Debug.Log(debugPrefix + "Will use default Sun template for " + bodyName);
                    }

                    if (!usesSunTemplate)
                        continue;

                    ConfigNode scaledVersionsNode = currentBody.GetNode("ScaledVersion");
                    if (scaledVersionsNode != null)
                    {
                        ConfigNode lightsNode = scaledVersionsNode.GetNode("Light");
                        if (lightsNode != null)
                        {
                            string luminosityText = lightsNode.GetValue("luminosity");
                            if (string.IsNullOrEmpty(luminosityText))
                                Debug.LogWarning(debugPrefix + "luminosity is missing in Light ConfigNode for " + bodyName);
                            else
                            {
                                double luminosity;
                                if (double.TryParse(luminosityText, out luminosity))
                                {
                                    solarLuminocity = (4 * Math.PI * kerbinAU * kerbinAU * luminosity) / kerbalLuminocity;
                                    Debug.Log(debugPrefix + "calculated solarLuminocity " + solarLuminocity + " based on luminosity " + luminosity + " for " + bodyName);
                                }
                                else
                                    Debug.LogError(debugPrefix + "Error converting " + luminosityText + " into luminosity for " + bodyName);
                            }
                        }
                        else
                            Debug.LogWarning(debugPrefix + "failed to find Light node for " + bodyName);

                    }
                    else
                        Debug.LogWarning(debugPrefix + "failed to find ScaledVersion node for " + bodyName);


                    ConfigNode propertiesNode = currentBody.GetNode("Properties");
                    if (propertiesNode != null)
                    {
                        string starLuminosityText = propertiesNode.GetValue("starLuminosity");

                        if (string.IsNullOrEmpty(starLuminosityText))
                        {
                            if (usesSunTemplate)
                                Debug.LogWarning(debugPrefix + "starLuminosity is missing in ConfigNode for " + bodyName);
                        }
                        else
                        {
                            double.TryParse(starLuminosityText, out solarLuminocity);

                            if (solarLuminocity > 0)
                            {
                                Debug.Log(debugPrefix + "Added Star " + celestialBody.name + " with defined luminocity " + solarLuminocity);
                                stars.Add(new StarLight() { star = celestialBody, relativeLuminocity = solarLuminocity });
                                continue;
                            }
                        }
                    }

                    if (solarLuminocity > 0)
                    {
                        Debug.Log(debugPrefix + "Added Star " + celestialBody.name + " with calculated luminocity of " + solarLuminocity);
                        stars.Add(new StarLight() { star = celestialBody, relativeLuminocity = solarLuminocity });
                    }
                    else
                    {
                        Debug.Log(debugPrefix + "Added Star " + celestialBody.name + " with default luminocity of 1");
                        stars.Add(new StarLight() { star = celestialBody, relativeLuminocity = 1 });
                    }

                }
            }

            // add local sun if kopernicus configuration was not found or did not contain any star
            var homePlanetSun = Planetarium.fetch.Sun;
            if (!stars.Any(m => m.star.name == homePlanetSun.name))
            {
                Debug.LogWarning(debugPrefix + "homeplanet star was not found, adding homeplanet star as default sun");
                stars.Add(new StarLight() { star = Planetarium.fetch.Sun, relativeLuminocity = 1 });
            }

            return stars;
        }

        public static bool LineOfSightToSun(Vector3d vesselPosition, CelestialBody star)
        {
            return LineOfSightToTransmitter(vesselPosition, star.position, star.name);
        }

        public static bool LineOfSightToTransmitter(Vector3d vesselPosition, Vector3d transmitterPosition, string ignoreBody = "")
        {
            Vector3d bminusa = transmitterPosition - vesselPosition;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                // the star should not block line of sight to the sun
                if (referenceBody.name == ignoreBody)
                    continue;

                Vector3d refminusa = referenceBody.position - vesselPosition;

                if (Vector3d.Dot(refminusa, bminusa) <= 0)
                    continue;

                var normalizedBminusa = bminusa.normalized;

                var cosReferenceSunNormB = Vector3d.Dot(refminusa, normalizedBminusa);

                if (cosReferenceSunNormB >= bminusa.magnitude)
                    continue;

                Vector3d tang = refminusa - cosReferenceSunNormB * normalizedBminusa;
                if (tang.magnitude < referenceBody.Radius)
                    return false;
            }
            return true;
        }


    }
}
