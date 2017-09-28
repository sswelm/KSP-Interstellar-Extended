using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    public class AtmosphericResourceHandler 
    {
        private static Dictionary<int, Dictionary<string, AtmosphericResource>> atmospheric_resource_by_body_id = new Dictionary<int, Dictionary<string, AtmosphericResource>>();
        private static Dictionary<string, Dictionary<string, AtmosphericResource>> atmospheric_resource_by_body_name = new Dictionary<string, Dictionary<string, AtmosphericResource>>();

        public static double getAtmosphericResourceContent(int refBody, string resourcename) 
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            AtmosphericResource resource;
            return bodyAtmosphericComposition.TryGetValue(resourcename, out resource) ? resource.ResourceAbundance : 0;
        }

        public static double getAtmosphericResourceContent(int refBody, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].ResourceAbundance : 0;
        }

        public static string getAtmosphericResourceName(int refBody, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].ResourceName : null;
        }

        public static string getAtmosphericResourceDisplayName(int refBody, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(refBody);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].DisplayName : null;
        }

        private static Dictionary<string, AtmosphericResource> GetAtmosphericCompositionForBody(string celestrialBodyName)
        {
            Dictionary<string, AtmosphericResource> bodyAtmosphericComposition;

            // first attempt to lookup if its already stored
            if (atmospheric_resource_by_body_name.TryGetValue(celestrialBodyName, out bodyAtmosphericComposition))
                return bodyAtmosphericComposition;

            // lookup celestrial body
            var celestialBody = FlightGlobals.Bodies.FirstOrDefault(b => b.name == celestrialBodyName);

            bodyAtmosphericComposition = CreateFromKspiDefinitionFile(celestrialBodyName);

            if (celestialBody != null)
                atmospheric_resource_by_body_id.Add(celestialBody.flightGlobalsIndex, bodyAtmosphericComposition);
            else
                Debug.LogWarning("[KSPI] - Failed to find FlightGlobalId for " + celestrialBodyName);

            // add to database for future reference
            atmospheric_resource_by_body_name.Add(celestrialBodyName, bodyAtmosphericComposition);

            return bodyAtmosphericComposition;
        }

        private static Dictionary<string, AtmosphericResource> CreateFromKspiDefinitionFile(string celestrialBodyName)
        {
            var atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

            Debug.Log("[KSPI] - Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));
            
            Debug.Log("[KSPI] - Searching for atmosphere definition data for " + celestrialBodyName);
            var atmospheric_resource_list = atmospheric_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == celestrialBodyName).ToList();
            if (atmospheric_resource_list.Any())
            {
                    Debug.Log("[KSPI] - found atmospheric resource list for " + celestrialBodyName);

                    // create atmospheric definition from file
                    return atmospheric_resource_list.Select(orsc => new AtmosphericResource(
                        orsc.HasValue("resourceName")
                            ? orsc.GetValue("resourceName")
                            : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName")))
                        .ToDictionary(m => m.ResourceName);
             }
             Debug.LogWarning("[KSPI] - Failed to find atmospheric resource list for " + celestrialBodyName);
            

            return new Dictionary<string, AtmosphericResource>();
        }

        private static Dictionary<string, AtmosphericResource> CreateFromKspiDefinitionFile(CelestialBody celestialBody)
        {
            var atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

            Debug.Log("[KSPI] Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));
            
            Debug.Log("[KSPI] - searching for atmosphere definition for " + celestialBody.name);
            var atmospheric_resource_list = atmospheric_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == celestialBody.name).ToList();
            if (atmospheric_resource_list.Any())
            {
                    // create atmospheric definition from file
                    return atmospheric_resource_list.Select(
                        orsc => new AtmosphericResource(orsc.HasValue("resourceName")
                            ? orsc.GetValue("resourceName")
                            : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName")))
                        .ToDictionary(m => m.ResourceName);
            }
            Debug.LogWarning("[KSPI] - Failed to find atmospheric resource list for " + celestialBody.name);

            return new Dictionary<string, AtmosphericResource>();
        }

        public static Dictionary<string, AtmosphericResource> GetAtmosphericCompositionForBody(CelestialBody celestialBody)
        {
            return GetAtmosphericCompositionForBody(celestialBody.flightGlobalsIndex);
        }

        public static Dictionary<string, AtmosphericResource> GetAtmosphericCompositionForBody(int refBody)
        {
            Dictionary<string, AtmosphericResource> bodyAtmosphericComposition;

            // first attempt to lookup if its already stored
            if (atmospheric_resource_by_body_id.TryGetValue(refBody, out bodyAtmosphericComposition))
                return bodyAtmosphericComposition;

            bodyAtmosphericComposition = new Dictionary<string, AtmosphericResource>();
            try
            {
                var celestialBody = FlightGlobals.Bodies[refBody];

                bodyAtmosphericComposition = CreateFromKspiDefinitionFile(celestialBody);

                // add from stock resource definitions if missing
                Debug.Log("[KSPI] - adding stock resource definitions for " + celestialBody.name);
                GenerateCompositionFromResourceAbundances(refBody, bodyAtmosphericComposition);

                Debug.Log("[KSPI] - sum of all resource abundance = " + bodyAtmosphericComposition.Values.Sum(m => m.ResourceAbundance));
                // if no atmosphere is created, create one base on celestrialbody characteristics
                if (bodyAtmosphericComposition.Values.Sum(m => m.ResourceAbundance) < 0.5)
                    bodyAtmosphericComposition = GenerateCompositionFromCelestialBody(celestialBody);

                // Add rare and isotopes resources
                Debug.Log("[KSPI] - adding trace resources and isotopess");
                AddRaresAndIsotopesToAdmosphereComposition(bodyAtmosphericComposition, celestialBody);

                // add missing stock resources
                Debug.Log("[KSPI] - adding missing stock defined resources");
                AddMissingStockResources(refBody, bodyAtmosphericComposition);

                // sort on resource abundance
                //bodyAtmosphericComposition = bodyAtmosphericComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();

                // add to database for future reference
                atmospheric_resource_by_body_id.Add(refBody, bodyAtmosphericComposition);
                atmospheric_resource_by_body_name.Add(celestialBody.name, bodyAtmosphericComposition);

                Debug.Log("[KSPI] - Succesfully Finished loading atmospheric composition");
            }
            catch (Exception ex)
            {
                Debug.Log("[KSPI] - Exception while loading atmospheric resources from id: " + ex);
            }

            return bodyAtmosphericComposition;
        }

        private static Dictionary<string, AtmosphericResource> GenerateCompositionFromCelestialBody(CelestialBody celestialBody)
        {
            try
            {
                // return empty is no atmosphere
                if (!celestialBody.atmosphere)
                {
                    Debug.Log("[KSPI] - celestrial body " + celestialBody.name + " is missing an atmosphere");
                    return new Dictionary<string, AtmosphericResource>();
                }

                // Lookup homeworld
                var homeworld = FlightGlobals.Bodies.First(b => b.isHomeWorld);
                var currentPresureAtSurface = celestialBody.GetPressure(0);
                var homeworldPresureAtSurface = homeworld.GetPressure(0);

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
                        return GetAtmosphericCompositionForBody("Saturn");
                    }
                    if (minimumTemperature < 80)
                    {
                        Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Uranus");
                        return GetAtmosphericCompositionForBody("Uranus");
                    }

                    Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Jupiter");
                    return GetAtmosphericCompositionForBody("Jupiter");
                }
                if (celestialBody.atmosphereContainsOxygen)
                {
                    Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Earth");
                    return GetAtmosphericCompositionForBody("Earth");
                }
                if (currentPresureAtSurface > 200)
                {
                    Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Venus");
                    return GetAtmosphericCompositionForBody("Venus");
                }
                Debug.Log("[KSPI] - determined " + celestialBody.name + " atmosphere to be like Mars");
                return GetAtmosphericCompositionForBody("Mars");
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI] - Exception while generating atmosphere composition from celestrial atmosphere properties : " + ex);
            }

            return new Dictionary<string, AtmosphericResource>();
        }

        private static void GenerateCompositionFromResourceAbundances(int refBody, Dictionary<string, AtmosphericResource> bodyComposition)
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
                Debug.LogError("[KSPI] - Exception while generating atmospheric composition from defined abundances : " + ex);
            }
        }

        private static void AddMissingStockResources(int refBody, Dictionary<string, AtmosphericResource> bodyComposition)
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

        private static void AddMissingResource(string resourname, int refBody, Dictionary<string, AtmosphericResource> bodyComposition)
        {
            // verify it is a defined resource
            var definition = PartResourceLibrary.Instance.GetDefinition(resourname);
            if (definition == null)
            {
                Debug.LogWarning("[KSPI] - AddMissingResource : Failed to find resource definition for '" + resourname + "'");
                return;
            }

            // skip it already registred or used as a Synonym
            if (bodyComposition.Values.Any(m => m.ResourceName == definition.name || m.DisplayName == definition.displayName || m.Synonyms.Contains(definition.name)))
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
            var resource = new AtmosphericResource(definition, abundance);

            // add to composition
            Debug.Log("[KSPI] - AddMissingResource : add resource '" + resourname + "'");
            bodyComposition.Add(resource.ResourceName, resource);
        }

        private static void AddRaresAndIsotopesToAdmosphereComposition(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition, CelestialBody celestialBody)
        {
            // add heavywater based on water abundance in atmosphere
            AtmosphericResource water;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._LIQUID_WATER, out water))
            {
                var heavywaterAbundance = water.ResourceAbundance / 6420;
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER,  new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER, heavywaterAbundance, InterstellarResourcesConfiguration._LIQUID_HEAVYWATER));
            }

            // add helium4 comparable to earth
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_HELIUM_4))
            {
                const double helium4Abundance = 5.2e-6;
                Debug.Log("[KSPI] - Added helum-4 to atmosphere with abundance " + helium4Abundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._LIQUID_HELIUM_4, new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_HELIUM_4, helium4Abundance, "Helium-4"));
            }
            else
                Debug.Log("[KSPI] - Helium is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._LIQUID_HELIUM_4].ResourceAbundance);

            // if helium3 is undefined, but helium is, derive it
            AtmosphericResource helium;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_HELIUM_3) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._LIQUID_HELIUM_4, out helium))
            {

                var helium3Abundance = celestialBody.GetPressure(0) > 1000
                    ? helium.ResourceAbundance * 0.001
                    : helium.ResourceAbundance * 1.38e-6;

                Debug.Log("[KSPI] - Added helium-3 to atmosphere eith abundance " + helium3Abundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._LIQUID_HELIUM_3, new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_HELIUM_3, helium3Abundance, "Helium-3"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_HELIUM_3))
                Debug.Log("[KSPI] - Helium-3 is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._LIQUID_HELIUM_3].ResourceAbundance);
            else
                Debug.Log("[KSPI] - No Helium is present in atmosphere specification, helium-4 will not be added");

            // if deteurium is undefined, but hydrogen is, derive it
            AtmosphericResource hydrogen;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_DEUTERIUM) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._LIQUID_HYDROGEN, out hydrogen))
            {
                var deuteriumAbundance = hydrogen.ResourceAbundance / 6420;
                Debug.Log("[KSPI] - Added deuterium to atmosphere with abundance " + deuteriumAbundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._LIQUID_DEUTERIUM, new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_DEUTERIUM, deuteriumAbundance, "Deuterium"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_DEUTERIUM))
                Debug.Log("[KSPI] - Deuterium is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._LIQUID_DEUTERIUM].ResourceAbundance);
            else 
                Debug.Log("[KSPI] - No Hydrogen is present in atmosphere specification, deuterium will not be added");

            // if nitrogen-15 is undefined, but nitrogen is, derive it
            AtmosphericResource nitrogen;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._LIQUID_NITROGEN, out nitrogen))
            {
                var nitrogen15Abundance = nitrogen.ResourceAbundance * 0.00364;
                Debug.Log("[KSPI] - Added nitrogen-15 to atmosphere with abundance " + nitrogen15Abundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15, new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15, nitrogen15Abundance, "Nitrogen-15"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15))
                Debug.Log("[KSPI] - Nitrogen-15 is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._LIQUID_NITROGEN_15].ResourceAbundance);
            else
                Debug.Log("[KSPI] - No Nitrogen is present in atmosphere specification, nitrogen-15 will not be added");
        }



        private static void AddResource(string outputResourname, string displayname, int refBody, IDictionary<string, AtmosphericResource> bodyComposition, string[] variants)
        {
            var abundances = new[] { GetAbundance(outputResourname, refBody) }.Concat(variants.Select(m => GetAbundance(m, refBody)));

            var resource = new AtmosphericResource(outputResourname, abundances.Max(), displayname, variants);
            if (resource.ResourceAbundance <= 0) return;

            AtmosphericResource existingResource;
            if (bodyComposition.TryGetValue(outputResourname, out existingResource))
            {
                Debug.Log("[KSPI] - Replaced resource " + outputResourname + " with stock defined abundance " + resource.ResourceAbundance);
                bodyComposition.Remove(existingResource.ResourceName);
            }
            bodyComposition.Add(resource.ResourceName, resource);
        }

        private static void AddResource(int refBody, IDictionary<string, AtmosphericResource> bodyComposition, string outputResourname, string inputResource1, string inputResource2, string inputResource3, string displayname)
        {
            var abundances = new[] { GetAbundance(inputResource1, refBody), GetAbundance(inputResource2, refBody), GetAbundance(inputResource2, refBody) };

            var resource = new AtmosphericResource(outputResourname, abundances.Max(), displayname, new[] { inputResource1, inputResource2, inputResource3 });
            if (resource.ResourceAbundance <= 0) return;

            AtmosphericResource existingResource;
            if (bodyComposition.TryGetValue(outputResourname, out existingResource))
            {
                Debug.Log("[KSPI] - replaced resource " + outputResourname + " with stock defined abundance " + resource.ResourceAbundance);
                bodyComposition.Remove(existingResource.ResourceName);
            }
            bodyComposition.Add(resource.ResourceName, resource);
        }

        private static float GetAbundance(string resourceName, int refBody)
        {
            return ResourceMap.Instance.GetAbundance(CreateRequest(resourceName, refBody));
        }

        private static AbundanceRequest CreateRequest(string resourceName, int refBody)
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