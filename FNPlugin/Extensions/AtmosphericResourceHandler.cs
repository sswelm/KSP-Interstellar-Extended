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
            List<AtmosphericResource> bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            AtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.ResourceName == resourcename);
            return resource != null ? resource.ResourceAbundance : 0;
        }

        public static double getAtmosphericResourceContentByDisplayName(int refBody, string resourcename) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            AtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.DisplayName == resourcename);
            return resource != null ? resource.ResourceAbundance : 0;
        }

        public static double getAtmosphericResourceContent(int refBody, int resource) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].ResourceAbundance;

            return 0;
        }

        public static string getAtmosphericResourceName(int refBody, int resource) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].ResourceName;

            return null;
        }

        public static string getAtmosphericResourceDisplayName(int refBody, int resource) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].DisplayName;

            return null;
        }

        public static List<AtmosphericResource> GetAtmosphericCompositionForBody(CelestialBody celestialBody)
        {
            return GetAtmosphericCompositionForBody(celestialBody.flightGlobalsIndex);
        }

        public static List<AtmosphericResource> GetAtmosphericCompositionForBody(int refBody) 
        {
            List<AtmosphericResource> bodyAtmosphericComposition = new List<AtmosphericResource>();

            try 
            {
                // first attemp to lookup if its already stored
                if (body_atmospheric_resource_list.ContainsKey(refBody))
                    return body_atmospheric_resource_list[refBody];
                else
                {
                    CelestialBody celestialBody = FlightGlobals.Bodies[refBody];

                    ConfigNode atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

                    Debug.Log("[ORS] Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));
                    if (atmospheric_resource_pack != null)
                    {
                        Debug.Log("[KSPI] - searching for atmosphere definition for " + celestialBody.name);
                        List<ConfigNode> atmospheric_resource_list = atmospheric_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == celestialBody.name).ToList();
                        if (atmospheric_resource_list.Any())
                        {
                            // create atmospheric definition from file
                            bodyAtmosphericComposition = atmospheric_resource_list.Select(orsc => new AtmosphericResource(orsc.HasValue("resourceName")
                                ? orsc.GetValue("resourceName")
                                : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();
                        }
                    }
                    else
                    {
                        Debug.LogError("[KSPI] - Failed to load atmospheric data");
                    }

                    // add from stock resource definitions if missing
                    Debug.Log("[KSPI] - adding stock resources" );
                    GenerateCompositionFromResourceAbundances(refBody, bodyAtmosphericComposition);

                    Debug.Log("[KSPI] - som of resource abundance = " + bodyAtmosphericComposition.Sum(m => m.ResourceAbundance));
                    // if no atmsphere is created, create one base on celestrialbody characteristics
                    if (bodyAtmosphericComposition.Sum(m => m.ResourceAbundance) < 0.5)
                        bodyAtmosphericComposition = GenerateCompositionFromCelestialBody(celestialBody);

                    // Add rare and isotopes resources
                    AddRaresAndIsotopesToAdmosphereComposition(bodyAtmosphericComposition, celestialBody);

                    // sort on resource abundance
                    bodyAtmosphericComposition = bodyAtmosphericComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();

                    // add to database for future reference
                    body_atmospheric_resource_list.Add(refBody, bodyAtmosphericComposition);

                }
            } 
            catch (Exception ex) 
            {
                Debug.Log("[KSPI] - Exception while loading atmospheric resources : " + ex.ToString());
            }
            return bodyAtmosphericComposition;
        }


        public static List<AtmosphericResource> GenerateCompositionFromCelestialBody(CelestialBody celestialBody)
        {
            List<AtmosphericResource> bodyAtmosphericComposition = new List<AtmosphericResource>();

            try
            {
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
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Saturn").flightGlobalsIndex);
                    }
                    else if (minimumTemperature < 80)
                    {
                        // it is a Uranus like planet, use Uranus as template
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Uranus").flightGlobalsIndex);
                    }
                    else
                    {
                        // it is Jupiter Like, use Jupiter as template
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Jupiter").flightGlobalsIndex);
                    }
                }
                else
                {
                    if (celestialBody.atmosphereContainsOxygen)
                    {
                        // it is a earth like use Earth as template
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Earth").flightGlobalsIndex);
                    }
                    else if (presureAtSurface > 200)
                    {
                        // it is a venus like use Venus as template
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Venus").flightGlobalsIndex);
                    }
                    else
                    {
                        // it is a mars like use Mars as template
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Mars").flightGlobalsIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating atmosphere composition from celestrial atmosphere properties : " + ex.ToString());
            }

            return bodyAtmosphericComposition;
        }

        public static List<AtmosphericResource> GenerateCompositionFromResourceAbundances(int refBody, List<AtmosphericResource> bodyAtmosphericComposition)
        {
            try
            {
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.Water, "LqdWater", "H2O", "Water", "Water");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.HeavyWater, "DeuteriumWater", "D2O", "HeavyWater", "HeavyWater");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.Water, "LqdNitrogen", "NitrogenGas", "Nitrogen", "Nitrogen");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.Oxygen, "LqdOxygen", "OxygenGas", "Oxygen", "Oxygen");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.CarbonDioxide, "LqdCO2", "CO2", "CarbonDioxide", "CarbonDioxide");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.CarbonMoxoxide, "LqdCO", "CO", "CarbonMonoxide", "CarbonMonoxide");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.Methane, "LqdMethane", "MethaneGas", "Methane", "Methane");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.Argon, "LqdArgon", "ArgonGas", "Argon", "Argon");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.Hydrogen, "LqdHydrogen", "HydrogenGas", "Hydrogen", "Hydrogen");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.LqdDeuterium, "LqdDeuterium", "DeuteriumGas", "Deuterium", "Deuterium");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.NeonGas, "LqdNeon", "NeonGas", "Neon", "Neon");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.XenonGas, "LqdXenon", "XenonGas", "Xenon", "Xenon");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.KryptonGas, "LqdKrypton", "KryptonGas", "Krypton", "Krypton");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.LqdHelium4, "LqdHelium", "HeliumGas", "Helium","Helium-4");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.LqdHelium3, "LqdHe3", "Helium3Gas", "Helium3", "Helium-3");
                AddResource(refBody, bodyAtmosphericComposition, InterstellarResourcesConfiguration.Instance.Sodium, "LqdSodium", "SodiumGas", "Sodium", "Sodium"); 
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating atmosphere composition from defined abundances : " + ex.ToString());
            }

            return bodyAtmosphericComposition;
        }

        private static void AddResource(int refBody, List<AtmosphericResource> bodyAtmosphericComposition, string outputResourname, string inputResource1, string inputResource2, string inputResource3, string displayname)
        {
            var abundances =  new []{ GetAbundance(inputResource1, refBody), GetAbundance(inputResource2, refBody), GetAbundance(inputResource2, refBody)};

            var atmosphericResource = new AtmosphericResource(outputResourname, abundances.Max(), displayname);
            if (atmosphericResource.ResourceAbundance > 0)
            {
                var existingResource = bodyAtmosphericComposition.FirstOrDefault(a => a.ResourceName == outputResourname);
                if (existingResource != null )
                {
                    Debug.Log("[KSPI] - replaced resource " + outputResourname + " with stock defined abundance " + atmosphericResource.ResourceAbundance);
                    bodyAtmosphericComposition.Remove(existingResource);
                }
                bodyAtmosphericComposition.Add(atmosphericResource);
            }
        }

        private static void AddRaresAndIsotopesToAdmosphereComposition(List<AtmosphericResource> bodyAtmosphericComposition, CelestialBody celestialBody)
        {
            // add heavywater based on water abundance in atmosphere
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.HeavyWater) && bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Water))
            {
                var water = bodyAtmosphericComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Water);
                var heavywaterAbundance = water.ResourceAbundance / 6420;
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.HeavyWater, heavywaterAbundance, "HeavyWater"));
            }

            // add helium4 comparable to earth
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4))
            {
                var helium4Abundance = 5.2e-6;
                Debug.Log("[KSPI] - added helum-4 to atmosphere with abundance " + helium4Abundance);
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.LqdHelium4, helium4Abundance, "Helium-4"));
            }
            else
                Debug.Log("[KSPI] -  helum-4 is already present in atmosphere specification at " + bodyAtmosphericComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4).ResourceAbundance);

            // if helium3 is undefined, but helium is, derive it
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium3) && bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4))
            {
                
                var helium = bodyAtmosphericComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4);

                var helium3Abundance = celestialBody.GetPressure(0) > 1000
                    ? helium.ResourceAbundance * 0.001
                    : helium.ResourceAbundance * 1.38e-6;

                Debug.Log("[KSPI] - added helum-3 to atmosphere eith abundance " + helium3Abundance);
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, helium3Abundance, "Helium-3"));
            }
            else if (bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium3))
                Debug.Log("[KSPI] -  helium-3 is already present in atmosphere specification at " + bodyAtmosphericComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium3).ResourceAbundance);
            else
                Debug.Log("[KSPI] -  No helium is present in atmosphere specification, helium-4 will not be added");

            // if deteurium is undefined, but hydrogen is, derive it
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdDeuterium) && bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Hydrogen))
            {
                var hydrogen = bodyAtmosphericComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Hydrogen);

                var deuteriumAbundance = hydrogen.ResourceAbundance / 6420;
                Debug.Log("[KSPI] - added deuterium to atmosphere with abundance " + deuteriumAbundance);
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, deuteriumAbundance, "Deuterium"));
            }
            else if (bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdDeuterium)) 
                Debug.Log("[KSPI] - deuterium is already present in atmosphere specification at " + bodyAtmosphericComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdDeuterium).ResourceAbundance);
            else 
                Debug.Log("[KSPI] - No hydrogen is present in atmosphere specification, deuterium will not be added");
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
