using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    public class AtmosphericResourceHandler 
    {
        protected static Dictionary<int, List<AtmosphericResource>> atmospheric_resource_by_id = new Dictionary<int, List<AtmosphericResource>>();
        protected static Dictionary<string, List<AtmosphericResource>> atmospheric_resource_by_name = new Dictionary<string, List<AtmosphericResource>>();

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

        public static List<AtmosphericResource> GetAtmosphericCompositionForBody(string celestrialBodyName)
        {
            List<AtmosphericResource> bodyAtmosphericComposition;

            // first attempt to lookup if its already stored
            if (atmospheric_resource_by_name.TryGetValue(celestrialBodyName, out bodyAtmosphericComposition))
                return bodyAtmosphericComposition;
            else
            {
                bodyAtmosphericComposition = new List<AtmosphericResource>();

                try
                {
                    ConfigNode atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

                    Debug.Log("[KSPI] Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));
                    if (atmospheric_resource_pack != null)
                    {
                        Debug.Log("[KSPI] - searching for atmosphere definition data for " + celestrialBodyName);
                        List<ConfigNode> atmospheric_resource_list = atmospheric_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == celestrialBodyName).ToList();
                        if (atmospheric_resource_list.Any())
                        {
                            Debug.Log("[KSPI] - found atmospheric resource list for " + celestrialBodyName);

                            // create atmospheric definition from file
                            bodyAtmosphericComposition = atmospheric_resource_list.Select(orsc => new AtmosphericResource(
                                orsc.HasValue("resourceName") 
                                    ? orsc.GetValue("resourceName") 
                                    : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();
                        }
                        else
                            Debug.LogWarning("[KSPI] - Failed to find atmospheric resource list for " + celestrialBodyName);
                    }
                    else
                        Debug.LogError("[KSPI] - Failed to load atmospheric data");

                    // add to database for future reference
                    atmospheric_resource_by_name.Add(celestrialBodyName, bodyAtmosphericComposition);

                    // lookup celestrial body
                    CelestialBody celestialBody = FlightGlobals.Bodies.FirstOrDefault(b => b.name == celestrialBodyName);
                    if (celestialBody != null)
                        atmospheric_resource_by_id.Add(celestialBody.flightGlobalsIndex, bodyAtmosphericComposition);
                    else
                        Debug.LogWarning("[KSPI] - Failed to find FlightGlobalId for " + celestrialBodyName);
                }
                catch (Exception ex)
                {
                    Debug.Log("[KSPI] - Exception while loading atmospheric resources from name : " + ex.ToString());
                }

                return bodyAtmosphericComposition;
            }
        }

        public static List<AtmosphericResource> GetAtmosphericCompositionForBody(CelestialBody celestialBody)
        {
            return GetAtmosphericCompositionForBody(celestialBody.flightGlobalsIndex);
        }

        public static List<AtmosphericResource> GetAtmosphericCompositionForBody(int refBody)
        {
            List<AtmosphericResource> bodyAtmosphericComposition;

            // first attempt to lookup if its already stored
            if (atmospheric_resource_by_id.TryGetValue(refBody, out bodyAtmosphericComposition))
                return bodyAtmosphericComposition;
            else
            {
                bodyAtmosphericComposition = new List<AtmosphericResource>();

                try
                {
                    CelestialBody celestialBody = FlightGlobals.Bodies[refBody];

                    ConfigNode atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

                    Debug.Log("[KSPI] Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));
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
                        else
                            Debug.LogWarning("[KSPI] - Failed to find atmospheric resource list for " + celestialBody.name);
                    }
                    else
                        Debug.LogError("[KSPI] - Failed to load atmospheric data");

                    // add from stock resource definitions if missing
                    Debug.Log("[KSPI] - adding stock resource definitions");
                    GenerateCompositionFromResourceAbundances(refBody, bodyAtmosphericComposition);

                    Debug.Log("[KSPI] - sum of all resource abundance = " + bodyAtmosphericComposition.Sum(m => m.ResourceAbundance));
                    // if no atmsphere is created, create one base on celestrialbody characteristics
                    if (bodyAtmosphericComposition.Sum(m => m.ResourceAbundance) < 0.5)
                        bodyAtmosphericComposition = GenerateCompositionFromCelestialBody(celestialBody);

                    // Add rare and isotopes resources
                    Debug.Log("[KSPI] - adding trace resources and isotopess");
                    AddRaresAndIsotopesToAdmosphereComposition(bodyAtmosphericComposition, celestialBody);

                    // add missing stock resources
                    Debug.Log("[KSPI] - adding missing stock defined resources");
                    AddMissingStockResources(refBody, bodyAtmosphericComposition);

                    // sort on resource abundance
                    bodyAtmosphericComposition = bodyAtmosphericComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();

                    // add to database for future reference
                    atmospheric_resource_by_id.Add(refBody, bodyAtmosphericComposition);
                    atmospheric_resource_by_name.Add(celestialBody.name, bodyAtmosphericComposition);
                }
                catch (Exception ex)
                {
                    Debug.Log("[KSPI] - Exception while loading atmospheric resources from id: " + ex.ToString());
                }
            }

            return bodyAtmosphericComposition;
        }


        public static List<AtmosphericResource> GenerateCompositionFromCelestialBody(CelestialBody celestialBody)
        {
            Debug.Log("[KSPI] - start to generate composition from celestrial body " + celestialBody.name);
            List<AtmosphericResource> bodyAtmosphericComposition = new List<AtmosphericResource>();

            try
            {
                // return empty is no atmosphere
                if (!celestialBody.atmosphere)
                {
                    Debug.Log("[KSPI] - celestrial body " + celestialBody.name + " is missing an atmosphere");
                    return bodyAtmosphericComposition;
                }

                // Lookup homeworld
                CelestialBody homeworld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);
                double currentPresureAtSurface = celestialBody.GetPressure(0);
                double homeworldPresureAtSurface = homeworld.GetPressure(0);

                Debug.Log("[KSPI] - determined " + homeworld.name + " to be the home world");
                Debug.Log("[KSPI] - surface presure " + celestialBody.name + " is " + currentPresureAtSurface);
                Debug.Log("[KSPI] - surface presure " + homeworld.name + " is " + homeworldPresureAtSurface);
                Debug.Log("[KSPI] - mass " + celestialBody.name + " is " + celestialBody.Mass);
                Debug.Log("[KSPI] - mass " + homeworld.name + " is " + celestialBody.Mass);

                if (celestialBody.Mass > homeworld.Mass * 10 && currentPresureAtSurface > 1000)
                {
                    float minimumTemperature;
                    float maximumTemperature;

                    celestialBody.atmosphereTemperatureCurve.FindMinMaxValue(out minimumTemperature, out maximumTemperature);

                    if (celestialBody.Density < 1)
                    {
                        Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Saturn" );
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody("Saturn");
                    }
                    else if (minimumTemperature < 80)
                    {
                        Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Uranus");
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody("Uranus");
                    }
                    else
                    {
                        Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Jupiter");
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody("Jupiter");
                    }
                }
                else
                {
                    if (celestialBody.atmosphereContainsOxygen)
                    {
                        Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Earth");
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody("Earth");
                    }
                    else if (currentPresureAtSurface > 200)
                    {
                        Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Venus");
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody("Venus");
                    }
                    else
                    {
                        Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Mars");
                        bodyAtmosphericComposition = GetAtmosphericCompositionForBody("Mars");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating atmosphere composition from celestrial atmosphere properties : " + ex.ToString());
            }

            return bodyAtmosphericComposition;
        }

        public static List<AtmosphericResource> GenerateCompositionFromResourceAbundances(int refBody, List<AtmosphericResource> bodyComposition)
        {
            try
            {
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_AMMONIA, "LqdAmmonia", "NH3", "Ammonia", "Ammonia");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_ARGON, "LqdArgon", "ArgonGas", "Argon", "Argon");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_CO2, "LqdCO2", "CO2", "CarbonDioxide", "CarbonDioxide");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_CO, "LqdCO", "CO", "CarbonMonoxide", "CarbonMonoxide");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_DEUTERIUM, "LqdDeuterium", "DeuteriumGas", "Deuterium", "Deuterium");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_TRITIUM, "LqdTritium", "TritiumGas", "Tritium", "Tritium");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_HEAVYWATER, "DeuteriumWater", "D2O", "HeavyWater", "HeavyWater");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_KRYPTON, "LqdKrypton", "KryptonGas", "Krypton", "Krypton");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_METHANE, "LqdMethane", "MethaneGas", "Methane", "Methane");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_NITROGEN, "LqdNitrogen", "NitrogenGas", "Nitrogen", "Nitrogen");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_NEON, "LqdNeon", "NeonGas", "Neon", "Neon");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_OXYGEN, "LqdOxygen", "OxygenGas", "Oxygen", "Oxygen");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_WATER, "LqdWater", "H2O", "Water", "Water");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration._LIQUID_XENON, "LqdXenon", "XenonGas", "Xenon", "Xenon");

                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Sodium, "LqdSodium", "SodiumGas", "Sodium", "Sodium");

                AddResource(InterstellarResourcesConfiguration._LIQUID_HELIUM_4, "Helium-4", refBody, bodyComposition, new[] { "LqdHe4", "Helium4Gas", "Helium4", "Helium-4", "He4Gas", "He4", "LqdHelium", "Helium", "HeliumGas" });
                AddResource(InterstellarResourcesConfiguration._LIQUID_HELIUM_3, "Helium-3", refBody, bodyComposition, new[] { "LqdHe3", "Helium3Gas", "Helium3", "Helium-3", "He3Gas", "He3" });
                AddResource(InterstellarResourcesConfiguration._LIQUID_HYDROGEN, "Hydrogen", refBody, bodyComposition, new[] { "LqdHydrogen", "HydrogenGas", "Hydrogen", "LiquidHydrogen", "H2", "Protium", "LqdProtium" });
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating atmospheric composition from defined abundances : " + ex.ToString());
            }

            return bodyComposition;
        }

        private static void AddMissingStockResources(int refBody, List<AtmosphericResource> bodyComposition)
        {
            // fetch all atmospheric resources
            var allResources = ResourceMap.Instance.FetchAllResourceNames(HarvestTypes.Atmospheric);

            Debug.Log("[KSPI] - AddMissingStockResources : found " + allResources.Count + " resources");

            foreach (var resoureName in allResources)
            {
                // add resource if missing
                AddMissingResource(resoureName, refBody, bodyComposition);
            }
        }

        private static void AddMissingResource(string resourname, int refBody, List<AtmosphericResource> bodyComposition)
        {
            // verify it is a defined resource
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourname);
            if (definition == null)
            {
                Debug.LogWarning("[KSPI] - AddMissingResource : Failed to find resource definition for '" + resourname + "'");
                return;
            }

            // skip it already registred or used as a Synonym
            if (bodyComposition.Any(m => m.ResourceName == definition.name || m.DisplayName == definition.displayName || m.Synonyms.Contains(definition.name)))
            {
                Debug.Log("[KSPI] - AddMissingResource : Already found existing composition for '" + resourname + "'");
                return;
            }

            // retreive abundance
            var abundance = GetAbundance(definition.name, refBody);
            if (abundance <= 0)
            {
                Debug.LogWarning("[KSPI] - AddMissingResource : Abundance for resource '" + resourname + "' was " + abundance);
                return;
            }

            // create resource from definition and abundance
            var Resource = new AtmosphericResource(definition, abundance);

            // add to composition
            Debug.Log("[KSPI] - AddMissingResource : add resource '" + resourname + "'");
            bodyComposition.Add(Resource);
        }

        private static void AddRaresAndIsotopesToAdmosphereComposition(List<AtmosphericResource> bodyAtmosphericComposition, CelestialBody celestialBody)
        {
            // add heavywater based on water abundance in atmosphere
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HEAVYWATER) && bodyAtmosphericComposition.Any(m => m.ResourceName == "Water"))
            {
                var water = bodyAtmosphericComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_WATER);
                var heavywaterAbundance = water.ResourceAbundance / 6420;
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER, heavywaterAbundance, InterstellarResourcesConfiguration._LIQUID_HEAVYWATER));
            }

            // add helium4 comparable to earth
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HELIUM_4))
            {
                var helium4Abundance = 5.2e-6;
                Debug.Log("[KSPI] - added helum-4 to atmosphere with abundance " + helium4Abundance);
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_HELIUM_4, helium4Abundance, "Helium-4"));
            }
            else
                Debug.Log("[KSPI] - Helium is already present in atmosphere specification at " + bodyAtmosphericComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HELIUM_4).ResourceAbundance);

            // if helium3 is undefined, but helium is, derive it
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HELIUM_3) && bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HELIUM_4))
            {

                var helium = bodyAtmosphericComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HELIUM_4);
                var helium3Abundance = celestialBody.GetPressure(0) > 1000
                    ? helium.ResourceAbundance * 0.001
                    : helium.ResourceAbundance * 1.38e-6;

                Debug.Log("[KSPI] - added helum-3 to atmosphere eith abundance " + helium3Abundance);
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_HELIUM_3, helium3Abundance, "Helium-3"));
            }
            else if (bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HELIUM_3))
                Debug.Log("[KSPI] - helium-3 is already present in atmosphere specification at " + bodyAtmosphericComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HELIUM_3).ResourceAbundance);
            else
                Debug.Log("[KSPI] - No Helium is present in atmosphere specification, helium-4 will not be added");

            // if deteurium is undefined, but hydrogen is, derive it
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_DEUTERIUM) && bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HYDROGEN))
            {
                var hydrogen = bodyAtmosphericComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_HYDROGEN);
                var deuteriumAbundance = hydrogen.ResourceAbundance / 6420;
                Debug.Log("[KSPI] - added deuterium to atmosphere with abundance " + deuteriumAbundance);
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_DEUTERIUM, deuteriumAbundance, "Deuterium"));
            }
            else if (bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_DEUTERIUM))
                Debug.Log("[KSPI] - deuterium is already present in atmosphere specification at " + bodyAtmosphericComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_DEUTERIUM).ResourceAbundance);
            else 
                Debug.Log("[KSPI] - No Hydrogen is present in atmosphere specification, deuterium will not be added");

            // if nitrogen-15 is undefined, but nitrogen is, derive it
            if (!bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_NITROGEN_15) && bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_NITROGEN))
            {
                var nitrogen = bodyAtmosphericComposition.FirstOrDefault(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_NITROGEN);
                var nitrogen15Abundance = nitrogen.ResourceAbundance * 0.00364;
                Debug.Log("[KSPI] - added nitrogen-15 to atmosphere with abundance " + nitrogen15Abundance);
                bodyAtmosphericComposition.Add(new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15, nitrogen15Abundance, "Nitrogen-15"));
            }
            else if (bodyAtmosphericComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_NITROGEN_15))
                Debug.Log("[KSPI] - nitrogen-15 is already present in atmosphere specification at " + bodyAtmosphericComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration._LIQUID_NITROGEN_15).ResourceAbundance);
            else
                Debug.Log("[KSPI] - No Nitrogen is present in atmosphere specification, nitrogen-15 will not be added");
        }



        private static void AddResource(string outputResourname, string displayname, int refBody, List<AtmosphericResource> bodyComposition, string[] variants)
        {
            var abundances = new[] { GetAbundance(outputResourname, refBody) }.Concat(variants.Select(m => GetAbundance(m, refBody)));

            var Resource = new AtmosphericResource(outputResourname, abundances.Max(), displayname, variants);
            if (Resource.ResourceAbundance > 0)
            {
                var existingResource = bodyComposition.FirstOrDefault(a => a.ResourceName == outputResourname);
                if (existingResource != null)
                {
                    Debug.Log("[KSPI] - replaced resource " + outputResourname + " with stock defined abundance " + Resource.ResourceAbundance);
                    bodyComposition.Remove(existingResource);
                }
                bodyComposition.Add(Resource);
            }
        }

        private static void AddResource(int refBody, List<AtmosphericResource> bodyComposition, string outputResourname, string inputResource1, string inputResource2, string inputResource3, string displayname)
        {
            var abundances = new[] { GetAbundance(inputResource1, refBody), GetAbundance(inputResource2, refBody), GetAbundance(inputResource2, refBody) };

            var Resource = new AtmosphericResource(outputResourname, abundances.Max(), displayname, new[] { inputResource1, inputResource2, inputResource3 });
            if (Resource.ResourceAbundance > 0)
            {
                var existingResource = bodyComposition.FirstOrDefault(a => a.ResourceName == outputResourname);
                if (existingResource != null)
                {
                    Debug.Log("[KSPI] - replaced resource " + outputResourname + " with stock defined abundance " + Resource.ResourceAbundance);
                    bodyComposition.Remove(existingResource);
                }
                bodyComposition.Add(Resource);
            }
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
