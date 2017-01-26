using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    public class AtmosphericResourceHandler 
    {
        protected static Dictionary<int, List<AtmosphericResource>> body_atmospheric_resource_list = new Dictionary<int, List<AtmosphericResource>>();

        public static double getAtmosphericResourceContent(int refBody, string resourcename) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            AtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.ResourceName == resourcename);
            return resource != null ? resource.ResourceAbundance : 0;
        }

        public static double getAtmosphericResourceContentByDisplayName(int refBody, string resourcename) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            AtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.DisplayName == resourcename);
            return resource != null ? resource.ResourceAbundance : 0;
        }

        public static double getAtmosphericResourceContent(int refBody, int resource) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].ResourceAbundance;

            return 0;
        }

        public static string getAtmosphericResourceName(int refBody, int resource) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].ResourceName;

            return null;
        }

        public static string getAtmosphericResourceDisplayName(int refBody, int resource) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].DisplayName;

            return null;
        }

        public static List<AtmosphericResource> getAtmosphericCompositionForBody(int refBody) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = new List<AtmosphericResource>();
            try 
            {
                if (body_atmospheric_resource_list.ContainsKey(refBody)) 
                    return body_atmospheric_resource_list[refBody];
                else 
                {
                    ConfigNode atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

                    Debug.Log("[ORS] Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));
                    if (atmospheric_resource_pack != null) 
                    {
                        List<ConfigNode> atmospheric_resource_list = atmospheric_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == FlightGlobals.Bodies[refBody].name).ToList();
                        if (atmospheric_resource_list.Any())
                        {
                            // create atmospheric definition from file
                            bodyAtmosphericComposition = atmospheric_resource_list.Select(orsc => new AtmosphericResource(orsc.HasValue("resourceName") 
                                ? orsc.GetValue("resourceName") 
                                : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();

                            if (bodyAtmosphericComposition.Any())
                            {
                                bodyAtmosphericComposition = bodyAtmosphericComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();
                                body_atmospheric_resource_list.Add(refBody, bodyAtmosphericComposition);
                            }
                        }
                        else
                        {
                            var generatedCompostion = GenerateCompositionFromResourceAbundances(refBody);

                            // check if generated composition is valid
                            if (generatedCompostion.Sum(m => m.ResourceAbundance) > 0.5)
                            {
                                body_atmospheric_resource_list.Add(refBody, generatedCompostion);
                            }
                            else
                            {
                                // generated based on celestrialbody characteristics
                                body_atmospheric_resource_list.Add(refBody, GenerateCompositionFromCelestialBody(refBody));
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("[KSPI] - Failed to load atmospheric data");
                    }
                }
            } 
            catch (Exception ex) 
            {
                Debug.Log("[KSPI] - Exception while loading atmospheric resources : " + ex.ToString());
            }
            return bodyAtmosphericComposition;
        }


        public static List<AtmosphericResource> GenerateCompositionFromCelestialBody(int refBody)
        {
            List<AtmosphericResource> bodyAtmosphericComposition = new List<AtmosphericResource>();


            try
            {
                CelestialBody celestialBody = FlightGlobals.Bodies[refBody];

                // return empty is no atmosphere
                if (!celestialBody.atmosphere)
                    return bodyAtmosphericComposition;

                // Lookup homeworld
                CelestialBody homeworld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);

                double presureAtSurface = celestialBody.GetPressure(0);

                if (celestialBody.Mass > homeworld.Mass * 10 && presureAtSurface > 1000)
                {
                    float minimumTemperature;
                    float maximumTemperature;

                    celestialBody.atmosphereTemperatureCurve.FindMinMaxValue(out minimumTemperature, out maximumTemperature);

                    if (celestialBody.Density < 1)
                    {
                        // it is a gas Saturn like, use Uranus as template
                        bodyAtmosphericComposition = getAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Saturn").flightGlobalsIndex);
                    }
                    else if (minimumTemperature < 80)
                    {
                        // it is a Uranus like planet, use Uranus as template
                        bodyAtmosphericComposition = getAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Uranus").flightGlobalsIndex);
                    }
                    else
                    {
                        // it is Jupiter Like, use Jupiter as template
                        bodyAtmosphericComposition = getAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Jupiter").flightGlobalsIndex);
                    }
                }
                else
                {
                    if (celestialBody.atmosphereContainsOxygen)
                    {
                        // it is a earth like use Earth as template
                        bodyAtmosphericComposition = getAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Earth").flightGlobalsIndex);
                    }
                    else if (presureAtSurface > 200)
                    {
                        // it is a venus like use Venus as template
                        bodyAtmosphericComposition = getAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Venus").flightGlobalsIndex);
                    }
                    else
                    {
                        // it is a mars like use Mars as template
                        bodyAtmosphericComposition = getAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Mars").flightGlobalsIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating atmosphere composition from celestrial atmosphere properties : " + ex.ToString());
            }

            return bodyAtmosphericComposition;
        }

        public static List<AtmosphericResource> GenerateCompositionFromResourceAbundances(CelestialBody celestialBody)
        {
            return GenerateCompositionFromResourceAbundances(celestialBody.flightGlobalsIndex);
        }

        public static List<AtmosphericResource> GenerateCompositionFromResourceAbundances(int refBody)
        {
            List<AtmosphericResource> bodyAtmosphericComposition = new List<AtmosphericResource>();

            try
            {
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.Water, Math.Max(GetAbundance("LqdWater", refBody), GetAbundance("Water", refBody)), "Water"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.Nitrogen, Math.Max(GetAbundance("LqdNitrogen", refBody), GetAbundance("Nitrogen", refBody)), "Nitrogen"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.Oxygen, Math.Max(GetAbundance("LqdOxygen", refBody), GetAbundance("Oxygen", refBody)), "Oxygen"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.CarbonDioxide, Math.Max(GetAbundance("LqdCO2", refBody), GetAbundance("CarbonDioxide", refBody)), "CarbonDioxide"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.CarbonMoxoxide, Math.Max(GetAbundance("LqdCO", refBody), GetAbundance("CarbonMoxoxide", refBody)), "CarbonMoxoxide"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.Methane, Math.Max(GetAbundance("LqdMethane", refBody), GetAbundance("Methane", refBody)), "Methane"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.Argon, Math.Max(GetAbundance("LqdArgon", refBody), GetAbundance("ArgonGas", refBody)), "Argon"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.Hydrogen, Math.Max(GetAbundance("LqdHydrogen", refBody), GetAbundance("Hydrogen", refBody)), "Hydrogen"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.LqdHelium4, Math.Max(GetAbundance("LqdHelium", refBody), GetAbundance("Helium", refBody)), "Helium-4"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, Math.Max(GetAbundance("LqdHe3", refBody), GetAbundance("Helium3", refBody)), "Helium-3"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.NeonGas, Math.Max(GetAbundance("LqdNeon", refBody), GetAbundance("NeonGas", refBody)), "Neon"));
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.XenonGas, Math.Max(GetAbundance("LqdXenon", refBody), GetAbundance("XenonGas", refBody)), "Xenon"));
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating atmosphere composition from defined abundances : " + ex.ToString());
            }

            return bodyAtmosphericComposition;
        }

        private static float GetAbundance(string resourceName, int refBody)
        {
            return ResourceMap.Instance.GetAbundance(CreateRequest(resourceName, refBody));
        }

        public static AbundanceRequest CreateRequest(string resourceName, int refBody)
        {
            return new AbundanceRequest
            {
                ResourceType = HarvestTypes.Atmospheric,
                ResourceName = resourceName,
                BodyId = refBody,
                CheckForLock = false
            };
        }

    }
}
