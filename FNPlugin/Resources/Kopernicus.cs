using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Resources
{
    class StarLight
    {
        public CelestialBody star;
        public double relativeLuminocity;
    }

    static class KopernicusHelper
    {
        static List<StarLight> stars;
        static Dictionary<CelestialBody, StarLight> starsByBody;

        public static CelestialBody GetLocalStar(CelestialBody body)
        {
            int depth = 0;
            CelestialBody current = body;
            while (depth < 10)
            {
                if (IsStar(current))
                    return current;

                current = current.referenceBody;
                depth++;
            }

            return null;
        }


        static private double astronomicalUnit;
        static public double AstronomicalUnit
        {
            get
            {
                if (astronomicalUnit == 0)
                    astronomicalUnit = FlightGlobals.GetHomeBody().orbit.semiMajorAxis;
                return astronomicalUnit;
            }
        }

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

        static public Dictionary<CelestialBody, StarLight> StarsByBody
        {
            get
            {
                if (starsByBody == null)
                    starsByBody = Stars.ToDictionary(m => m.star);
                return starsByBody;
            }
        }

        public static bool IsStar(CelestialBody body)
        {
            return GetLuminocity(body) > 0;
        }

        public static double GetLuminocity(CelestialBody body)
        {
            StarLight starlight;
            if (StarsByBody.TryGetValue(body, out starlight))
                return starlight.relativeLuminocity;
            else
                return 0;
        }       
        

        const string moduleName = "KSPI";
        const double kerbinAU = 13599840256;
        const double kerbalLuminocity = 3.1609409786213e+24;


        /// <summary>
        /// // Scan the Kopernicus config nodes and extract Kopernicus star data
        /// </summary>
        /// <param name="modulename"></param>
        /// <returns></returns>
        public static List<StarLight> ExtractStarData(string modulename)
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
                Debug.LogWarning(debugPrefix + "homeplanet localStar was not found, adding homeplanet localStar as default sun");
                stars.Add(new StarLight() { star = Planetarium.fetch.Sun, relativeLuminocity = 1 });
            }

            return stars;
        }


    }
}
