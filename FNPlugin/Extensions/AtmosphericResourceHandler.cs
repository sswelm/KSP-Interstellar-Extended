using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;

namespace FNPlugin 
{
    public class AtmosphericResourceHandler 
    {
        protected static Dictionary<int, List<ORSAtmosphericResource>> body_atmospheric_resource_list = new Dictionary<int, List<ORSAtmosphericResource>>();

        public static double getAtmosphericResourceContent(int refBody, string resourcename) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            ORSAtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.getResourceName() == resourcename);
            return resource != null ? resource.getResourceAbundance() : 0;
        }

        public static double getAtmosphericResourceContentByDisplayName(int refBody, string resourcename) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            ORSAtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.getDisplayName() == resourcename);
            return resource != null ? resource.getResourceAbundance() : 0;
        }

        public static double getAtmosphericResourceContent(int refBody, int resource) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].getResourceAbundance();

            return 0;
        }

        public static string getAtmosphericResourceName(int refBody, int resource) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].getResourceName();

            return null;
        }

        public static string getAtmosphericResourceDisplayName(int refBody, int resource) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].getDisplayName();

            return null;
        }

        public static List<ORSAtmosphericResource> getAtmosphericCompositionForBody(int refBody) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = new List<ORSAtmosphericResource>();
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
                            bodyAtmosphericComposition = atmospheric_resource_list.Select(orsc => new ORSAtmosphericResource(orsc.HasValue("resourceName") 
                                ? orsc.GetValue("resourceName") 
                                : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();

                            if (bodyAtmosphericComposition.Any())
                            {
                                bodyAtmosphericComposition = bodyAtmosphericComposition.OrderByDescending(bacd => bacd.getResourceAbundance()).ToList();
                                body_atmospheric_resource_list.Add(refBody, bodyAtmosphericComposition);
                            }
                        }
                        else
                        {
                            // add empty definition
                            body_atmospheric_resource_list.Add(refBody, GenerateCompositionFromCelestialBody(refBody));
                        }
                    }
                    else
                    {
                        Debug.LogError("[ORS]Failed to load atmospheric data");
                    }
                }
            } 
            catch (Exception ex) 
            {
                Debug.Log("[ORS] - Exception while loading atmospheric resources : " + ex.ToString());
            }
            return bodyAtmosphericComposition;
        }


        public static List<ORSAtmosphericResource> GenerateCompositionFromCelestialBody(int refBody)
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = new List<ORSAtmosphericResource>();


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
                Debug.LogError("[ORS] - Exception while generating atmosphere composition from celestrial atmosphere properties : " + ex.ToString());
            }

            return bodyAtmosphericComposition;
        }

        public static List<ORSAtmosphericResource> GenerateCompositionFromResourceAbundances(int refBody)
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = new List<ORSAtmosphericResource>();

            try
            {
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Water, ResourceMap.Instance.GetAbundance(CreateRequest("Water", refBody)), "Water"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Nitrogen, ResourceMap.Instance.GetAbundance(CreateRequest("Nitrogen", refBody)), "Nitrogen"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Oxygen, ResourceMap.Instance.GetAbundance(CreateRequest("Oxygen", refBody)), "Oxygen"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.CarbonDioxide, ResourceMap.Instance.GetAbundance(CreateRequest("CarbonDioxide", refBody)), "CarbonDioxide"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.CarbonMoxoxide, ResourceMap.Instance.GetAbundance(CreateRequest("CarbonMoxoxide", refBody)), "CarbonMoxoxide"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Methane, ResourceMap.Instance.GetAbundance(CreateRequest("Methane", refBody)), "Methane"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Argon, ResourceMap.Instance.GetAbundance(CreateRequest("ArgonGas", refBody)), "Argon"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Hydrogen, ResourceMap.Instance.GetAbundance(CreateRequest("LqdHydrogen", refBody)), "Hydrogen"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Helium4Gas, ResourceMap.Instance.GetAbundance(CreateRequest("Helium", refBody)), "Helium-4"));
                bodyAtmosphericComposition.Add(new ORSAtmosphericResource(InterstellarResourcesConfiguration.Instance.Helium3Gas, ResourceMap.Instance.GetAbundance(CreateRequest("LqdHe3", refBody)), "Helium-3"));
            }
            catch (Exception ex)
            {
                Debug.LogError("[ORS] - Exception while generating atmosphere composition from defined abundances : " + ex.ToString());
            }

            return bodyAtmosphericComposition;
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
