using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Extensions
{
    class OceanicResourceHandler
    {
        protected static Dictionary<int, List<OceanicResource>> body_oceanic_resource_list = new Dictionary<int, List<OceanicResource>>();

        public static double getOceanicResourceContent(int refBody, string resourcename)
        {
            List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
            OceanicResource resource = bodyOceanicComposition.FirstOrDefault(oor => oor.ResourceName == resourcename);
            return resource != null ? resource.ResourceAbundance : 0;
        }

        public static double getOceanicResourceContent(int refBody, int resource)
        {
            List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) return bodyOceanicComposition[resource].ResourceAbundance;
            return 0;
        }

        public static string getOceanicResourceName(int refBody, int resource)
        {
            List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource)
            {
                return bodyOceanicComposition[resource].ResourceName;
            }
            return null;
        }

        public static string getOceanicResourceDisplayName(int refBody, int resource)
        {
            List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource)
            {
                return bodyOceanicComposition[resource].DisplayName;
            }
            return null;
        }

        public static List<OceanicResource> GetOceanicCompositionForBody(CelestialBody celestialBody) // getter that uses celestial body as an argument
        {
            return GetOceanicCompositionForBody(celestialBody.flightGlobalsIndex); // calls the function that uses refBody int as an argument
        }

        public static List<OceanicResource> GetOceanicCompositionForBody(int refBody) // function for getting or creating oceanic composition
        {
            List<OceanicResource> bodyOceanicComposition = new List<OceanicResource>(); // create an object list for holding all the resources
            try
            {
                if (body_oceanic_resource_list.ContainsKey(refBody)) // if there's a composition for this body
                {
                    return body_oceanic_resource_list[refBody]; // skip all the other stuff and return the composition we already have
                }
                else
                {
                    CelestialBody celestialBody = FlightGlobals.Bodies[refBody]; // create a celestialBody object referencing the current body (makes it easier on us in the next lines)

                    ConfigNode oceanic_resource_pack = GameDatabase.Instance.GetConfigNodes("OCEANIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();
                    //ConfigNode oceanic_resource_pack = GameDatabase.Instance.GetConfigNodes("OCEANIC_RESOURCE_PACK_DEFINITION").FirstOrDefault(c => c.name == "KSPI_OceanicPack");

                    Debug.Log("[ORS] Loading oceanic data from pack: " + (oceanic_resource_pack.HasValue("name") ? oceanic_resource_pack.GetValue("name") : "unknown pack"));
                    if (oceanic_resource_pack != null)
                    {
                        Debug.Log("[KSPI] - searching for ocean definition for " + celestialBody.name);
                        List<ConfigNode> oceanic_resource_list = oceanic_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == FlightGlobals.Bodies[refBody].name).ToList();
                        if (oceanic_resource_list.Any())
                        {
                            bodyOceanicComposition = oceanic_resource_list.Select(orsc => new OceanicResource(orsc.HasValue("resourceName") ? orsc.GetValue("resourceName") : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();
                            if (bodyOceanicComposition.Any())
                            {
                                bodyOceanicComposition = bodyOceanicComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();
                                body_oceanic_resource_list.Add(refBody, bodyOceanicComposition);
                            }
                        }
                    }

                    // add from stock resource definitions if missing
                    Debug.Log("[KSPI] - adding stock resources");
                    GenerateCompositionFromResourceAbundances(refBody, bodyOceanicComposition); // calls the generating function below

                    Debug.Log("[KSPI] - sum of resource abundances = " + bodyOceanicComposition.Sum(m => m.ResourceAbundance));
                    // if no ocean definition is created, create one based on celestialBody characteristics
                    if (bodyOceanicComposition.Sum(m => m.ResourceAbundance) < 0.5)
                        bodyOceanicComposition = GenerateCompositionFromCelestialBody(celestialBody);

                    // Add rare and isotopic resources
                    AddRaresAndIsotopesToOceanComposition(bodyOceanicComposition, celestialBody);

                    // sort on resource abundance
                    bodyOceanicComposition = bodyOceanicComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();

                    // add to database for future reference
                    body_oceanic_resource_list.Add(refBody, bodyOceanicComposition);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[ORS] - Exception while loading oceanic resources : " + ex.ToString());
            }
            return bodyOceanicComposition;
        }

        public static List<OceanicResource> GenerateCompositionFromCelestialBody(CelestialBody celestialBody) // generates oceanic composition based on planetary characteristics
        {
            List<OceanicResource> bodyOceanicComposition = new List<OceanicResource>(); // instantiate a new list that this function will be returning

            try
            {
                // return empty if there's no ocean
                if (!celestialBody.ocean)
                    return bodyOceanicComposition;

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
                        // it is a Saturn-like, use Saturn as template
                        bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Saturn").flightGlobalsIndex);
                    }
                    else if (minimumTemperature < 80)
                    {
                        // it is a Uranus-like planet, use Uranus as template
                        bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Uranus").flightGlobalsIndex);
                    }
                    else
                    {
                        // it is Jupiter-Like, use Jupiter as template
                        bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Jupiter").flightGlobalsIndex);
                    }
                }
                else
                {
                    if (celestialBody.atmosphereContainsOxygen)
                    {
                        // it is Earth-like, use Earth as template
                        bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Earth").flightGlobalsIndex);
                    }
                    else if (presureAtSurface > 200)
                    {
                        // it is Venus-like, use Venus as template
                        bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Venus").flightGlobalsIndex);
                    }
                    else
                    {
                        // it is Mars-like, use Mars as template
                        bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Mars").flightGlobalsIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating oceanic composition from celestial ocean properties : " + ex.ToString());
            }

            return bodyOceanicComposition;
        }

        public static List<OceanicResource> GenerateCompositionFromResourceAbundances(int refBody, List<OceanicResource> bodyOceanicComposition)
        {
            try
            {
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.Water, "LqdWater", "H2O", "Water", "Water");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.HeavyWater, "DeuteriumWater", "D2O", "HeavyWater", "HeavyWater");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.Water, "LqdNitrogen", "NitrogenGas", "Nitrogen", "Nitrogen");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.Oxygen, "LqdOxygen", "OxygenGas", "Oxygen", "Oxygen");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.CarbonDioxide, "LqdCO2", "CO2", "CarbonDioxide", "CarbonDioxide");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.CarbonMoxoxide, "LqdCO", "CO", "CarbonMonoxide", "CarbonMonoxide");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.Methane, "LqdMethane", "MethaneGas", "Methane", "Methane");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.Argon, "LqdArgon", "ArgonGas", "Argon", "Argon");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.Hydrogen, "LqdHydrogen", "HydrogenGas", "Hydrogen", "Hydrogen");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.LqdDeuterium, "LqdDeuterium", "DeuteriumGas", "Deuterium", "Deuterium");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.NeonGas, "LqdNeon", "NeonGas", "Neon", "Neon");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.XenonGas, "LqdXenon", "XenonGas", "Xenon", "Xenon");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.KryptonGas, "LqdKrypton", "KryptonGas", "Krypton", "Krypton");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.LqdHelium4, "LqdHelium", "HeliumGas", "Helium", "Helium-4");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.LqdHelium3, "LqdHe3", "Helium3Gas", "Helium3", "Helium-3");
                AddResource(refBody, bodyOceanicComposition, InterstellarResourcesConfiguration.Instance.Sodium, "LqdSodium", "SodiumGas", "Sodium", "Sodium");
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating oceanic composition from defined abundances : " + ex.ToString());
            }

            return bodyOceanicComposition;
        }

        private static void AddResource(int refBody, List<OceanicResource> bodyOceanicComposition, string outputResourname, string inputResource1, string inputResource2, string inputResource3, string displayname)
        {
            var abundances = new[] { GetAbundance(inputResource1, refBody), GetAbundance(inputResource2, refBody), GetAbundance(inputResource2, refBody) };

            var OceanicResource = new OceanicResource(outputResourname, abundances.Max(), displayname);
            if (OceanicResource.ResourceAbundance > 0)
            {
                var existingResource = bodyOceanicComposition.FirstOrDefault(a => a.ResourceName == outputResourname);
                if (existingResource != null)
                {
                    Debug.Log("[KSPI] - replaced resource " + outputResourname + " with stock defined abundance " + OceanicResource.ResourceAbundance);
                    bodyOceanicComposition.Remove(existingResource);
                }
                bodyOceanicComposition.Add(OceanicResource);
            }
        }

        private static void AddRaresAndIsotopesToOceanComposition(List<OceanicResource> bodyOceanicComposition, CelestialBody celestialBody)
        {
            // add heavywater based on water abundance in ocean
            if (!bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.HeavyWater) && bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Water))
            {
                var water = bodyOceanicComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Water);
                var heavywaterAbundance = water.ResourceAbundance / 6420;
                bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.HeavyWater, heavywaterAbundance, "HeavyWater"));
            }

            // add helium4 comparable to earth
            if (!bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4))
            {
                var helium4Abundance = 5.2e-6;
                Debug.Log("[KSPI] - added helium-4 to ocean with abundance " + helium4Abundance);
                bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.LqdHelium4, helium4Abundance, "Helium-4"));
            }
            else
                Debug.Log("[KSPI] -  helum-4 is already present in ocean, specification at " + bodyOceanicComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4).ResourceAbundance);

            // if helium3 is undefined, but helium is, derive it
            if (!bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium3) && bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4))
            {

                var helium = bodyOceanicComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium4);

                var helium3Abundance = celestialBody.GetPressure(0) > 1000
                    ? helium.ResourceAbundance * 0.001
                    : helium.ResourceAbundance * 1.38e-6;

                Debug.Log("[KSPI] - added helium-3 to ocean with abundance " + helium3Abundance);
                bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, helium3Abundance, "Helium-3"));
            }
            else if (bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium3))
                Debug.Log("[KSPI] -  helium-3 is already present in ocean, specification at " + bodyOceanicComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdHelium3).ResourceAbundance);
            else
                Debug.Log("[KSPI] -  No helium is present in ocean specification, helium-4 will not be added");

            // if deteurium is undefined, but hydrogen is, derive it
            if (!bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdDeuterium) && bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Hydrogen))
            {
                var hydrogen = bodyOceanicComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Hydrogen);

                var deuteriumAbundance = hydrogen.ResourceAbundance / 6420;
                Debug.Log("[KSPI] - added deuterium to ocean with abundance " + deuteriumAbundance);
                bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, deuteriumAbundance, "Deuterium"));
            }
            else if (bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdDeuterium))
                Debug.Log("[KSPI] - deuterium is already present in ocean specification at " + bodyOceanicComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.LqdDeuterium).ResourceAbundance);
            else
                Debug.Log("[KSPI] - No hydrogen is present in ocean specification, deuterium will not be added");
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
