using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Resources
{
    class OceanicResourceHandler
    {
        protected static Dictionary<int, List<OceanicResource>> body_oceanic_resource_list = new Dictionary<int, List<OceanicResource>>();

        public static double getOceanicResourceContent(int refBody, string resourceName)
        {
            List<OceanicResource> bodyOceanicComposition = GetOceanicCompositionForBody(refBody);
            OceanicResource resource = bodyOceanicComposition.FirstOrDefault(oor => oor.ResourceName == resourceName);
            return resource?.ResourceAbundance ?? 0;
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
            var bodyOceanicComposition = new List<OceanicResource>(); // create an object list for holding all the resources
            try
            {
                // check if there's a composition for this body
                if (body_oceanic_resource_list.ContainsKey(refBody)) 
                {
                    // skip all the other stuff and return the composition we already have
                    return body_oceanic_resource_list[refBody]; 
                }

                CelestialBody celestialBody = FlightGlobals.Bodies[refBody]; // create a celestialBody object referencing the current body (makes it easier on us in the next lines)

                // create composition from kspi oceanic definition file
                bodyOceanicComposition = CreateFromKspiOceanDefinitionFile(refBody, celestialBody);

                // add from stock resource definitions if missing
                GenerateCompositionFromResourceAbundances(refBody, bodyOceanicComposition); // calls the generating function below

                // if no ocean definition is created, create one based on celestialBody characteristics
                if (bodyOceanicComposition.Sum(m => m.ResourceAbundance) < 0.5)
                    bodyOceanicComposition = GenerateCompositionFromCelestialBody(celestialBody);

                // Add rare and isotopic resources
                AddRaresAndIsotopesToOceanComposition(bodyOceanicComposition);

                // add missing stock resources
                AddMissingStockResources(refBody, bodyOceanicComposition);

                // sort on resource abundance
                bodyOceanicComposition = bodyOceanicComposition.OrderByDescending(bacd => bacd.ResourceAbundance).ToList();

                // add to database for future reference
                body_oceanic_resource_list.Add(refBody, bodyOceanicComposition);
            }
            catch (Exception ex)
            {
                Debug.Log("[KSPI]: Exception while loading oceanic resources : " + ex.ToString());
            }
            return bodyOceanicComposition;
        }

        private static List<OceanicResource> CreateFromKspiOceanDefinitionFile(int refBody, CelestialBody celestialBody)
        {
            var bodyOceanicComposition = new List<OceanicResource>();

            ConfigNode oceanicResourcePack = GameDatabase.Instance.GetConfigNodes("OCEANIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();

            Debug.Log("[KSPI] Loading oceanic data from pack: " + (oceanicResourcePack.HasValue("name") ? oceanicResourcePack.GetValue("name") : "unknown pack"));
            if (oceanicResourcePack != null)
            {
                Debug.Log("[KSPI]: searching for ocean definition for " + celestialBody.name);
                List<ConfigNode> oceanicResourceList = oceanicResourcePack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == FlightGlobals.Bodies[refBody].name).ToList();
                if (oceanicResourceList.Any())
                    bodyOceanicComposition = oceanicResourceList.Select(orsc => new OceanicResource(orsc.HasValue("resourceName") ? orsc.GetValue("resourceName") : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();
            }
            return bodyOceanicComposition;
        }

        public static List<OceanicResource> GenerateCompositionFromCelestialBody(CelestialBody celestialBody) // generates oceanic composition based on planetary characteristics
        {
            var bodyOceanicComposition = new List<OceanicResource>(); // instantiate a new list that this function will be returning

            // return empty if there's no ocean
            if (!celestialBody.ocean)
                return bodyOceanicComposition;

            try
            {
                // Lookup homeworld
                CelestialBody homeworld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);

                if (homeworld == null)
                {
                    Debug.LogError("[KSPI]: Failed to find homeworld for GenerateCompositionFromCelestialBody");
                    return bodyOceanicComposition;
                }

                double pressureAtSurface = celestialBody.GetPressure(0);

                if (celestialBody.Mass < (homeworld.Mass / 1000) && pressureAtSurface > 0 && pressureAtSurface < 10) // is it tiny and has only trace atmosphere?
                {
                    // it is similar to Enceladus, use that as a template
                    bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Enceladus").flightGlobalsIndex);
                }
                else if (celestialBody.Mass < (homeworld.Mass / 100) && pressureAtSurface > 0 && pressureAtSurface < 100) // is it still tiny, but a bit larger and has thin atmosphere?
                {
                    // it is Europa-like, use Europa as a template
                    bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Europa").flightGlobalsIndex);
                }
                else if (celestialBody.Mass < (homeworld.Mass / 40) && pressureAtSurface > 0 && pressureAtSurface < 10 && celestialBody.atmosphereContainsOxygen) // if it is significantly smaller than the homeworld and has trace atmosphere with oxygen
                {
                    // it is Ganymede-like, use Ganymede as a template
                    bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Ganymede").flightGlobalsIndex);
                }
                else if (celestialBody.Mass < homeworld.Mass && pressureAtSurface > 140) // if it is smaller than the homeworld and has pressure at least 140kPA
                {
                    // it is Titan-like, use Titan as a template
                    bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Titan").flightGlobalsIndex);
                }
                else if (pressureAtSurface > 200)
                {
                    // it is Venus-like/Eve-like, use Eve as a template
                    bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Eve").flightGlobalsIndex);
                }
                else if (celestialBody.Mass > (homeworld.Mass / 2) && celestialBody.Mass < homeworld.Mass && pressureAtSurface < 100) // it's at least half as big as the homeworld and has significant atmosphere
                {
                    // it is Laythe-like, use Laythe as a template
                    bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Laythe").flightGlobalsIndex);
                }
                else if (celestialBody.atmosphereContainsOxygen)
                {
                    // it is Earth-like, use Earth as a template
                    bodyOceanicComposition = GetOceanicCompositionForBody(FlightGlobals.Bodies.Single(b => b.name == "Earth").flightGlobalsIndex);
                }
                else
                {
                    // nothing yet
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]: Exception while generating oceanic composition from celestial ocean properties : " + ex.ToString());
            }

            return bodyOceanicComposition;
        }

        public static List<OceanicResource> GenerateCompositionFromResourceAbundances(int refBody, List<OceanicResource> bodyComposition)
        {
            try
            {
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Ammonia, "LqdAmmonia", "NH3", "Ammonia", "Ammonia");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Argon, "LqdArgon", "ArgonGas", "Argon", "Argon");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.CarbonDioxide, "LqdCO2", "CO2", "CarbonDioxide", "CarbonDioxide");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.CarbonMoxoxide, "LqdCO", "CO", "CarbonMonoxide", "CarbonMonoxide");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.DeuteriumGas, "LqdDeuterium", "DeuteriumGas", "Deuterium", "Deuterium");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.HeavyWater, "DeuteriumWater", "D2O", "HeavyWater", "HeavyWater");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.KryptonGas, "LqdKrypton", "KryptonGas", "Krypton", "Krypton");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Methane, "LqdMethane", "MethaneGas", "Methane", "Methane");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Nitrogen, "LqdNitrogen", "NitrogenGas", "Nitrogen", "Nitrogen");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.NeonGas, "LqdNeon", "NeonGas", "Neon", "Neon");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.OxygenGas, "LqdOxygen", "OxygenGas", "Oxygen", "Oxygen");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Sodium, "LqdSodium", "SodiumGas", "Sodium", "Sodium");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.Water, "LqdWater", "H2O", "Water", "Water");
                AddResource(refBody, bodyComposition, InterstellarResourcesConfiguration.Instance.XenonGas, "LqdXenon", "XenonGas", "Xenon", "Xenon");

                AddResource(InterstellarResourcesConfiguration.Instance.LqdHelium4, "Helium-4", refBody, bodyComposition, new[] { "LqdHe4", "Helium4Gas", "Helium4", "Helium-4", "He4Gas", "He4", "LqdHelium", "Helium", "HeliumGas" });
                AddResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, "Helium-3", refBody, bodyComposition, new[] { "LqdHe3", "Helium3Gas", "Helium3", "Helium-3", "He3Gas", "He3" });
                AddResource(InterstellarResourcesConfiguration.Instance.Hydrogen, "Hydrogen", refBody, bodyComposition, new[] { "LqdHydrogen", "HydrogenGas", "Hydrogen", "H2", "Protium", "LqdProtium"});
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]: Exception while generating oceanic composition from defined abundances : " + ex.ToString());
            }

            return bodyComposition;
        }

        private static void AddMissingStockResources(int refBody, List<OceanicResource> bodyComposition)
        {
            // fetch all oceanic resources
            var allOceanicResources = ResourceMap.Instance.FetchAllResourceNames(HarvestTypes.Oceanic);

            Debug.Log("[KSPI]: AddMissingStockResources : found " + allOceanicResources.Count + " resources");

            foreach (var resourceName in allOceanicResources)
            {
                // add resource if missing
                AddMissingResource(resourceName, refBody, bodyComposition);
            }
        }

        private static void AddMissingResource(string resourceName, int refBody, List<OceanicResource> bodyComposition)
        {
            // verify it is a defined resource
            var definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            if (definition == null)
            {
                Debug.LogWarning("[KSPI]: AddMissingResource : Failed to find resource definition for '" + resourceName + "'");
                return;
            }

            // skip it already registered or used as a Synonym
            if (bodyComposition.Any(m => m.ResourceName == definition.name || m.DisplayName == definition.displayName || m.Synonyms.Contains(definition.name)))
            {
                Debug.Log("[KSPI]: AddMissingResource : Already found existing composition for '" + resourceName + "'");
                return;
            }

            // retrieve abundance
            var abundance = GetAbundance(definition.name, refBody);
            if (abundance <= 0)
            {
                Debug.LogWarning("[KSPI]: AddMissingResource : Abundance for resource '" + resourceName + "' was " + abundance);
                return;
            }

            // create oceanic resource from definition and abundance
            var oceanicResource = new OceanicResource(definition, abundance);

            // add to oceanic composition
            Debug.Log("[KSPI]: AddMissingResource : add resource '" + resourceName + "'");
            bodyComposition.Add(oceanicResource);
        }

        private static void AddResource(string outputResourceName, string displayName, int refBody, List<OceanicResource> bodyOceanicComposition, string[] variants)
        {
            var abundances = new[] { GetAbundance(outputResourceName, refBody)}.Concat(variants.Select(m => GetAbundance(m, refBody)));

            var oceanicResource = new OceanicResource(outputResourceName, abundances.Max(), displayName, variants);
            if (oceanicResource.ResourceAbundance > 0)
            {
                var existingResource = bodyOceanicComposition.FirstOrDefault(a => a.ResourceName == outputResourceName);
                if (existingResource != null)
                {
                    Debug.Log("[KSPI]: replaced resource " + outputResourceName + " with stock defined abundance " + oceanicResource.ResourceAbundance);
                    bodyOceanicComposition.Remove(existingResource);
                }
                bodyOceanicComposition.Add(oceanicResource);
            }
        }

        private static void AddResource(int refBody, List<OceanicResource> bodyOceanicComposition, string outputResourceName, string inputResource1, string inputResource2, string inputResource3, string displayname)
        {
            var abundances = new[] { GetAbundance(inputResource1, refBody), GetAbundance(inputResource2, refBody), GetAbundance(inputResource2, refBody) };

            var oceanicResource = new OceanicResource(outputResourceName, abundances.Max(), displayname, new[] { inputResource1, inputResource2, inputResource3 });
            if (oceanicResource.ResourceAbundance > 0)
            {
                var existingResource = bodyOceanicComposition.FirstOrDefault(a => a.ResourceName == outputResourceName);
                if (existingResource != null)
                {
                    Debug.Log("[KSPI]: replaced resource " + outputResourceName + " with stock defined abundance " + oceanicResource.ResourceAbundance);
                    bodyOceanicComposition.Remove(existingResource);
                }
                bodyOceanicComposition.Add(oceanicResource);
            }
        }

        private static void AddRaresAndIsotopesToOceanComposition(List<OceanicResource> bodyOceanicComposition)
        {
            Debug.Log("[KSPI]: Checking for missing rare isotopes");

            // add Heavy Water based on water abundance in ocean
            if (bodyOceanicComposition.All(m => m.ResourceName != InterstellarResourcesConfiguration.Instance.HeavyWater) && bodyOceanicComposition.Any(m => m.ResourceName == "Water" || m.ResourceName == "LqdWater"))
            {
                Debug.Log("[KSPI]: Added heavy water based on presence water in ocean");
                var waterResource = bodyOceanicComposition.FirstOrDefault(m => m.ResourceName == "Water") ?? bodyOceanicComposition.FirstOrDefault(m => m.ResourceName == "LqdWater");

                if (waterResource != null)
                {
                    var heavyWaterAbundance = waterResource.ResourceAbundance / 6420;
                    bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.HeavyWater, heavyWaterAbundance, "HeavyWater", new[] {"HeavyWater", "D2O", "DeuteriumWater"}));
                }
            }

            if (bodyOceanicComposition.All(m => m.ResourceName != InterstellarResourcesConfiguration.Instance.Lithium6) && bodyOceanicComposition.Any(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Lithium7))
            {
                Debug.Log("[KSPI]: Added lithium-6 based on presence Lithium in ocean");
                var lithium = bodyOceanicComposition.First(m => m.ResourceName == InterstellarResourcesConfiguration.Instance.Lithium7);
                var heavyWaterAbundance = lithium.ResourceAbundance * 0.0759;
                bodyOceanicComposition.Add(new OceanicResource(InterstellarResourcesConfiguration.Instance.Lithium6, heavyWaterAbundance, "Lithium-6", new[] { "Lithium6", "Lithium-6", "Li6", "Li-6" }));
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
                ResourceType = HarvestTypes.Oceanic,
                ResourceName = resourceName,
                BodyId = refBody,
                CheckForLock = false
            };
        }
    }
}
