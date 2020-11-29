using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Resources
{
    public class AtmosphericResourceHandler
    {
        private static readonly Dictionary<int, Dictionary<string, AtmosphericResource>> AtmosphericResourceByBodyId = new Dictionary<int, Dictionary<string, AtmosphericResource>>();
        private static readonly Dictionary<string, Dictionary<string, AtmosphericResource>> AtmosphericResourceByBodyName = new Dictionary<string, Dictionary<string, AtmosphericResource>>();

        public static double GetAtmosphericResourceContent(CelestialBody body, string resourceName)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            return bodyAtmosphericComposition.TryGetValue(resourceName, out var resource) ? resource.ResourceAbundance : 0;
        }

        public static double GetAtmosphericResourceContent(CelestialBody body, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].ResourceAbundance : 0;
        }

        public static string GetAtmosphericResourceName(CelestialBody body, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].ResourceName : null;
        }

        public static string GetAtmosphericResourceDisplayName(CelestialBody body, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].DisplayName : null;
        }

        private static Dictionary<string, AtmosphericResource> GetAtmosphericCompositionForKnownCelestial(string celestialBodyName)
        {
            // first attempt to lookup if its already stored
            if (AtmosphericResourceByBodyName.TryGetValue(celestialBodyName, out var bodyAtmosphericComposition))
                return bodyAtmosphericComposition;

            bodyAtmosphericComposition = CreateFromKspiDefinitionFile(celestialBodyName);

            // add to database for future reference
            AtmosphericResourceByBodyName.Add(celestialBodyName, bodyAtmosphericComposition);

            return bodyAtmosphericComposition;
        }

        private static Dictionary<string, AtmosphericResource> CreateFromKspiDefinitionFile(string celestialBodyName)
        {
            Debug.Log("[KSPI]: searching for atmosphere definition for " + celestialBodyName);

            try
            {
                foreach (var atmosphericResourcePack in GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI"))
                {
                    Debug.Log("[KSPI]: Loading atmospheric data from pack: " + (atmosphericResourcePack.HasValue("name") ? atmosphericResourcePack.GetValue("name") : "unknown pack"));

                    var atmosphericResourceList = atmosphericResourcePack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == celestialBodyName).ToList();
                    if (atmosphericResourceList.Any())
                    {
                        Debug.Log("[KSPI]: found atmospheric resource list for " + celestialBodyName);

                        // create atmospheric definition from pack
                        return atmosphericResourceList.Select(orsc => new AtmosphericResource(
                            orsc.HasValue("resourceName")
                                ? orsc.GetValue("resourceName")
                                : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName")))
                            .ToDictionary(m => m.ResourceName);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]:  CreateFromKspiDefinitionFile for " + celestialBodyName + " exception " + ex.Message);
            }

            Debug.LogWarning("[KSPI]: Failed to find atmospheric resource list for " + celestialBodyName);
            return new Dictionary<string, AtmosphericResource>();
        }

        public static Dictionary<string, AtmosphericResource> GetAtmosphericCompositionForBody(CelestialBody body)
        {
            // first attempt to lookup if its already stored
            if (AtmosphericResourceByBodyId.TryGetValue(body.flightGlobalsIndex, out var bodyAtmosphericComposition))
                return bodyAtmosphericComposition;

            try
            {
                // look for Kspi defined atmospheric compositions
                bodyAtmosphericComposition = CreateFromKspiDefinitionFile(body.name);

                // add from stock resource definitions if missing
                Debug.Log("[KSPI]: adding stock resource definitions for " + body.name);
                GenerateCompositionFromResourceAbundances(body, bodyAtmosphericComposition);

                var sumOfResourceAbundances = bodyAtmosphericComposition.Values.Sum(m => m.ResourceAbundance);
                Debug.Log("[KSPI]: sum of all resource abundance = " + sumOfResourceAbundances);

                // if abundance does not approximate 1, create one base on celestial body characteristics
                if (sumOfResourceAbundances < 0.8 || sumOfResourceAbundances > 1.2)
                    bodyAtmosphericComposition = GenerateCompositionFromCelestialBody(body);

                // Add rare and isotopes resources
                Debug.Log("[KSPI]: adding trace resources and isotopess to " + body.name);
                AddRaresAndIsotopesToAtmosphereComposition(bodyAtmosphericComposition, body);

                // add missing stock resources
                Debug.Log("[KSPI]: adding missing stock defined resources to " + body.name);
                AddMissingStockResources(body, bodyAtmosphericComposition);

                // add to dictionaries for future reference
                AtmosphericResourceByBodyId.Add(body.flightGlobalsIndex, bodyAtmosphericComposition);
                AtmosphericResourceByBodyName.Add(body.name, bodyAtmosphericComposition);

                Debug.Log("[KSPI]: Successfully Finished loading atmospheric composition for " + body.name);
                return bodyAtmosphericComposition;
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]:  GetAtmosphericCompositionForBody " + body.name + " exception " + ex.Message);
            }

            Debug.LogWarning("[KSPI]: Failed loading atmospheric composition for " + body.name);
            bodyAtmosphericComposition = new Dictionary<string, AtmosphericResource>();
            return bodyAtmosphericComposition;
        }

        private static Dictionary<string, AtmosphericResource> GenerateCompositionFromCelestialBody(CelestialBody body)
        {
            try
            {
                // return empty is no atmosphere
                if (!body.atmosphere)
                {
                    Debug.Log("[KSPI]: celestial body " + body.name + " is missing an atmosphere");
                    return new Dictionary<string, AtmosphericResource>();
                }

                Debug.Log("[KSPI]: Start GenerateCompositionFromCelestialBody for " + body.name);

                // Lookup homeworld
                var homeworld = FlightGlobals.Bodies.First(b => b.isHomeWorld);
                var pressureAtSurface = body.GetPressure(0);

                Debug.Log("[KSPI]: determined " + homeworld.name + " to be the home world");

                Debug.Log("[KSPI]: surface pressure " + body.name + " is " + pressureAtSurface + " kPa");
                Debug.Log("[KSPI]: surface pressure " + homeworld.name + " is " + homeworld.GetPressure(0) + " kPa");
                Debug.Log("[KSPI]: atmospheric Density " + body.name + " is " + body.atmDensityASL);
                Debug.Log("[KSPI]: atmospheric Density " + homeworld.name + " is " + homeworld.atmDensityASL);
                Debug.Log("[KSPI]: mass " + body.name + " is " + body.Mass);
                Debug.Log("[KSPI]: mass " + homeworld.name + " is " + homeworld.Mass);
                Debug.Log("[KSPI]: atmosphere MolarMass " + body.name + " is " + body.atmosphereMolarMass);
                Debug.Log("[KSPI]: atmosphere MolarMass " + homeworld.name + " is " + homeworld.atmosphereMolarMass);
                Debug.Log("[KSPI]: atmosphere Depth " + body.name + " is " + body.atmosphereDepth);
                Debug.Log("[KSPI]: atmosphere Depth " + homeworld.name + " is " + homeworld.atmosphereDepth);
                Debug.Log("[KSPI]: density " + body.name + " is " + body.Density);
                Debug.Log("[KSPI]: density " + homeworld.name + " is " + homeworld.Density);
                Debug.Log("[KSPI]: temperature " + body.name + " is " + body.atmosphereTemperatureSeaLevel + " K");
                Debug.Log("[KSPI]: temperature " + homeworld.name + " is " + homeworld.atmosphereTemperatureSeaLevel + " K");

                // determine if the planet is a gas planet
                if (body.Mass > homeworld.Mass * 10 && pressureAtSurface >= 1000)
                {
                    // Check if the planet  hot gas planet
                    if (body.Mass < homeworld.Mass * 25 && body.atmosphereTemperatureSeaLevel > 700) // Higher than 700 K
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Gliese 436b");
                        return GetAtmosphericCompositionForKnownCelestial("Gliese436b");
                    }

                    if (body.atmosphereTemperatureSeaLevel > 1400) // Class V: Silicate clouds
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere is a Class V: Silicate clouds planet");
                        return GetAtmosphericCompositionForKnownCelestial("Jupiter");
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 900) // Class IV: Alkali metals
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere is a Class IV: Alkali metals planet");
                        return GetAtmosphericCompositionForKnownCelestial("Jupiter");
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 300) // Class III: Cloudless
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere is a Class III: Cloudless planet");
                        return GetAtmosphericCompositionForKnownCelestial("Jupiter");
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 150) // Class II: Water clouds
                    {
                        if (body.atmosphereDepth < 500000 )
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Gauss");
                            return GetAtmosphericCompositionForKnownCelestial("Gauss");
                        }
                        else
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Otho");
                            return GetAtmosphericCompositionForKnownCelestial("Otho");
                        }
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 80)  // Class I: Ammonia clouds
                    {
                        // Check if its a super giant
                        if (body.Mass > homeworld.Mass * 80)
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Jupiter");
                            return GetAtmosphericCompositionForKnownCelestial("Jupiter");
                        }
                        if (body.Mass > homeworld.Mass * 60)
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Nero");
                            return GetAtmosphericCompositionForKnownCelestial("Nero");
                        }

                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Saturn");
                        return GetAtmosphericCompositionForKnownCelestial("Saturn");
                    }
                    else // ice giant
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Uranus");
                        return GetAtmosphericCompositionForKnownCelestial("Uranus");
                    }
                }

                if (pressureAtSurface >= 1000 && body.atmosphereTemperatureSeaLevel > 500)	// Higher than 1000 kPa and 500 K
                {
                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Venus");
                    return GetAtmosphericCompositionForKnownCelestial("Venus");
                }

                if (body.atmosphereTemperatureSeaLevel < 200) // // Colder than 200K
                {
                    if (body.atmosphereTemperatureSeaLevel > 150)
                    {
                        if (pressureAtSurface < 100)
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Gratian");
                            return GetAtmosphericCompositionForKnownCelestial("Gratian");
                        }
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Titan");
                        return GetAtmosphericCompositionForKnownCelestial("Titan");
                    }

                    if (body.atmosphereTemperatureSeaLevel < 50) // Surface temperature colder than 50K
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Pluto");
                        return GetAtmosphericCompositionForKnownCelestial("Pluto");
                    }

                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Triton");
                    return GetAtmosphericCompositionForKnownCelestial("Triton");
                }

                if (body.atmosphereContainsOxygen)
                {
                    if (body.Mass > homeworld.Mass * 5)
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Tellumo");
                        return GetAtmosphericCompositionForKnownCelestial("Tellumo");
                    }

                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Earth");
                    return GetAtmosphericCompositionForKnownCelestial("Earth");
                }

                if (body.atmosphereTemperatureSeaLevel > 270)
                {
                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Niven");
                    return GetAtmosphericCompositionForKnownCelestial("Niven");
                }

                // Otherwise is a boring Mars like planet
                Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Mars");
                return GetAtmosphericCompositionForKnownCelestial("Mars");
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]: Exception while generating atmosphere composition from celestial atmosphere properties for " + body.name + " : " + ex);
            }

            return new Dictionary<string, AtmosphericResource>();
        }

        private static void GenerateCompositionFromResourceAbundances(CelestialBody body, Dictionary<string, AtmosphericResource> bodyComposition)
        {
            try
            {
                // mundane resources
                AddResource(ResourceSettings.Config.AmmoniaLqd, "Ammonia", body, bodyComposition, new[] { "LqdAmmonia", "AmmoniaGas", "Ammonia", "NH3"}, 1);
                AddResource(ResourceSettings.Config.ArgonLqd, "Argon", body, bodyComposition, new[] { "LqdArgon", "ArgonGas", "Argon", "Ar" }, 1);
                AddResource(ResourceSettings.Config.CarbonDioxideLqd, "CarbonDioxide", body, bodyComposition, new[] { "LqdCO2", "CarbonDioxideGas", "CarbonDioxide", "CO2" }, 1);
                AddResource(ResourceSettings.Config.CarbonMonoxideLqd, "CarbonMonoxide", body, bodyComposition, new[] { "LqdCO", "CarbonMonoxideGas", "CarbonMonoxide", "CO", }, 1);
                AddResource(ResourceSettings.Config.ChlorineGas, "Chlorine", body, bodyComposition, new[] { "Chlorine", "ChlorineGas", "LqdChlorine", "Cl", }, 1);
                AddResource(ResourceSettings.Config.WaterHeavy, "HeavyWater", body, bodyComposition, new[] { "DeuteriumWater", "HeavyWater", "D2O" }, 1);
                AddResource(ResourceSettings.Config.KryptonLqd, "Krypton", body, bodyComposition, new[] { "LqdKrypton", "KryptonGas", "Krypton" , "Kr"}, 1);
                AddResource(ResourceSettings.Config.MethaneLqd, "Methane", body, bodyComposition, new[] { "LqdMethane", "MethaneGas", "Methane", "CH4" }, 1);
                AddResource(ResourceSettings.Config.NitrogenLqd, "Nitrogen", body, bodyComposition, new[] { "LqdNitrogen", "NitrogenGas", "Nitrogen", "N", "N2" }, 1);
                AddResource(ResourceSettings.Config.NeonLqd, "Neon", body, bodyComposition, new[] { "LqdNeon", "NeonGas", "Neon", "Ne" }, 1);
                AddResource(ResourceSettings.Config.OxygenLqd, "Oxygen", body, bodyComposition, new[] { "LqdOxygen", "OxygenGas", "Oxygen", "O", "O2" }, 1);
                AddResource(ResourceSettings.Config.WaterPure, "LqdWater", body, bodyComposition, new[] { "LqdWater", "Water", "WaterGas", "DihydrogenMonoxide", "H2O", "DHMO" }, 1);
                AddResource(ResourceSettings.Config.XenonLqd, "Xenon", body, bodyComposition, new[] { "LqdXenon", "XenonGas", "Xenon", "Xe" }, 1);
                AddResource(ResourceSettings.Config.Sodium, "Sodium", body, bodyComposition, new[] { "LqdSodium", "SodiumGas", "Sodium", "Natrium", "Na" }, 1);
                AddResource(ResourceSettings.Config.Lithium7, "Lithium", body, bodyComposition, new[] { "Lithium", "LithiumGas", "Lithium7", "Li", "Li7" }, 1);
                AddResource(ResourceSettings.Config.Lithium6, "Lithium6", body, bodyComposition, new[] { "Lithium6", "Lithium-6", "Li6" }, 1);
                AddResource(ResourceSettings.Config.HydrogenLqd , "Hydrogen", body, bodyComposition, new[] { "LqdHydrogen", "HydrogenGas", "Hydrogen", "LiquidHydrogen", "H2", "Protium", "LqdProtium", "H" }, 1);
                AddResource(ResourceSettings.Config.Helium4Lqd, "Helium", body, bodyComposition, new[] { "LqdHe4", "Helium4Gas", "Helium4", "Helium-4", "He4Gas", "He4", "LqdHelium", "Helium", "HeliumGas", "He" }, 1);

                // exotic isotopes
                AddResource(ResourceSettings.Config.Helium3Lqd, "Helium-3", body, bodyComposition, new[] { "LqdHe3", "Helium3Gas", "Helium3", "Helium-3", "He3Gas", "He3", "LqdHelium3" }, 5);
                AddResource(ResourceSettings.Config.DeuteriumLqd, "Deuterium", body, bodyComposition, new[] { "LqdDeuterium", "DeuteriumGas", "Deuterium", "D" }, 5);
                AddResource(ResourceSettings.Config.TritiumLqd, "Tritium", body, bodyComposition, new[] { "LqdTritium", "TritiumGas", "Tritium", "T" }, 5);
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]: Exception while generating atmospheric composition from defined abundances for " + body.name + " : " + ex);
            }
        }

        private static void AddMissingStockResources(CelestialBody body, Dictionary<string, AtmosphericResource> bodyComposition)
        {
            // fetch all atmospheric resources
            var allResources = ResourceMap.Instance.FetchAllResourceNames(HarvestTypes.Atmospheric);

            Debug.Log("[KSPI]: AddMissingStockResources : found " + allResources.Count + " resources");

            foreach (var resourceName in allResources)
            {
                // add resource if missing
                AddMissingResource(resourceName, body, bodyComposition);
            }
        }

        private static void AddMissingResource(string resourceName, CelestialBody body, Dictionary<string, AtmosphericResource> bodyComposition)
        {
            // verify it is a defined resource
            var definition = PartResourceLibrary.Instance.GetDefinition(resourceName);
            if (definition == null)
            {
                Debug.LogWarning("[KSPI]: AddMissingResource : Failed to find resource definition for '" + resourceName + "'");
                return;
            }

            // skip it already registered or used as a Synonym
            if (bodyComposition.Values.Any(m => m.ResourceName == definition.name || m.DisplayName == definition.displayName || m.Synonyms.Contains(definition.name)))
            {
                Debug.Log("[KSPI]: AddMissingResource : Already found existing composition for '" + resourceName + "'");
                return;
            }

            // retrieve abundance
            var abundance = GetAbundance(definition.name, body);
            if (abundance <= 0)
            {
                Debug.LogWarning("[KSPI]: AddMissingResource : Abundance for resource '" + resourceName + "' was " + abundance);
                return;
            }

            // create resource from definition and abundance
            var resource = new AtmosphericResource(definition, abundance);

            // add to composition
            Debug.Log("[KSPI]: AddMissingResource : add resource '" + resourceName + "'");
            bodyComposition.Add(resource.ResourceName, resource);
        }

        private static void AddRaresAndIsotopesToAtmosphereComposition(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition, CelestialBody body)
        {
            SupplementWithHeavyWater(bodyAtmosphericComposition);

            SupplementWithHelium(bodyAtmosphericComposition);

            SupplementWithHelium3(bodyAtmosphericComposition, body);

            AddDeuteriumWhenMissing(bodyAtmosphericComposition);

            SupplementWithNitrogen15(bodyAtmosphericComposition);
        }

        private static void SupplementWithHeavyWater(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition)
        {
            // add heavyWater based on water abundance in atmosphere
            if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.WaterHeavy) &&
                bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.WaterPure, out var pureWaterResource))
            {
                var heavyWaterAbundance = pureWaterResource.ResourceAbundance / 6420;
                bodyAtmosphericComposition.Add(
                    ResourceSettings.Config.WaterHeavy,
                    new AtmosphericResource(ResourceSettings.Config.WaterHeavy, heavyWaterAbundance, ResourceSettings.Config.WaterHeavy));
            }
            else if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.WaterHeavy) &&
                 bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.WaterRaw, out var rawWaterResource))
            {
                var heavyWaterAbundance = rawWaterResource.ResourceAbundance / 6420;
                bodyAtmosphericComposition.Add(
                    ResourceSettings.Config.WaterHeavy,
                    new AtmosphericResource(ResourceSettings.Config.WaterHeavy, heavyWaterAbundance, ResourceSettings.Config.WaterHeavy));
            }
        }

        private static void SupplementWithHelium(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition)
        {
            // add helium4 comparable to earth
            if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.Helium4Lqd))
            {
                const double helium4Abundance = 5.2e-6;
                Debug.Log("[KSPI]: Added helium-4 to atmosphere with abundance " + helium4Abundance);
                bodyAtmosphericComposition.Add(ResourceSettings.Config.Helium4Lqd,
                    new AtmosphericResource(ResourceSettings.Config.Helium4Lqd, helium4Abundance, "Helium-4"));
            }
            else
            {
                Debug.Log("[KSPI]: Helium is already present in atmosphere specification at " +
                          bodyAtmosphericComposition[ResourceSettings.Config.Helium4Lqd].ResourceAbundance);
            }
        }

        private static void SupplementWithHelium3(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition, CelestialBody body)
        {
            // if helium3 is undefined, but liquid helium is, derive it
            if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.Helium3Lqd) &&
                bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.Helium4Lqd, out var heliumLqd))
            {
                var helium3Abundance = body.GetPressure(0) > 1000
                    ? heliumLqd.ResourceAbundance * 0.001
                    : heliumLqd.ResourceAbundance * 1.38e-6;

                Debug.Log("[KSPI]: Added helium-3 to atmosphere either abundance " + helium3Abundance);
                bodyAtmosphericComposition.Add(ResourceSettings.Config.Helium3Lqd,
                    new AtmosphericResource(ResourceSettings.Config.Helium3Lqd, helium3Abundance, "Helium-3"));
            }
            // if helium3 is undefined, but helium gas is, derive it
            else if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.Helium3Lqd) &&
                bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.Helium4Gas, out var heliumGas))
            {
                var helium3Abundance = body.GetPressure(0) > 1000
                    ? heliumGas.ResourceAbundance * 0.001
                    : heliumGas.ResourceAbundance * 1.38e-6;

                Debug.Log("[KSPI]: Added helium-3 to atmosphere either abundance " + helium3Abundance);
                bodyAtmosphericComposition.Add(ResourceSettings.Config.Helium3Lqd,
                    new AtmosphericResource(ResourceSettings.Config.Helium3Lqd, helium3Abundance, "Helium-3"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.Helium3Lqd))
            {
                Debug.Log("[KSPI]: Helium-3 is already present in atmosphere specification at " +
                          bodyAtmosphericComposition[ResourceSettings.Config.Helium3Lqd].ResourceAbundance);
            }
            else
            {
                Debug.Log("[KSPI]: No Helium is present in atmosphere specification, helium-4 will not be added");
            }
        }

        private static void SupplementWithNitrogen15(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition)
        {
            // if nitrogen-15 is undefined, but nitrogen is, derive it
            if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.Nitrogen15Lqd) &&
                bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.NitrogenLqd, out var nitrogenLqd))
            {
                var nitrogen15Abundance = nitrogenLqd.ResourceAbundance * 0.00364;
                Debug.Log("[KSPI]: Added nitrogen-15 to atmosphere with abundance " + nitrogen15Abundance);
                bodyAtmosphericComposition.Add(ResourceSettings.Config.Nitrogen15Lqd,
                    new AtmosphericResource(ResourceSettings.Config.Nitrogen15Lqd, nitrogen15Abundance,
                        "Nitrogen-15"));
            }
            // if nitrogen-15 is undefined, but nitrogen is, derive it
            if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.Nitrogen15Lqd) &&
                bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.NitrogenGas, out var nitrogenGas))
            {
                var nitrogen15Abundance = nitrogenGas.ResourceAbundance * 0.00364;
                Debug.Log("[KSPI]: Added nitrogen-15 to atmosphere with abundance " + nitrogen15Abundance);
                bodyAtmosphericComposition.Add(ResourceSettings.Config.Nitrogen15Lqd,
                    new AtmosphericResource(ResourceSettings.Config.Nitrogen15Lqd, nitrogen15Abundance,
                        "Nitrogen-15"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.Nitrogen15Lqd))
            {
                Debug.Log("[KSPI]: Nitrogen-15 is already present in atmosphere specification at " +
                          bodyAtmosphericComposition[ResourceSettings.Config.Nitrogen15Lqd].ResourceAbundance);
            }
            else
            {
                Debug.Log("[KSPI]: No Nitrogen is present in atmosphere specification, nitrogen-15 will not be added");
            }
        }

        private static void AddDeuteriumWhenMissing(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition)
        {
            if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.DeuteriumLqd) &&
                bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.HydrogenLqd, out var hydrogenLqd))
            {
                var deuteriumAbundance = hydrogenLqd.ResourceAbundance / 6420;
                Debug.Log("[KSPI]: Added deuterium to atmosphere with abundance " + deuteriumAbundance);
                bodyAtmosphericComposition.Add(ResourceSettings.Config.DeuteriumLqd,
                    new AtmosphericResource(ResourceSettings.Config.DeuteriumLqd, deuteriumAbundance, "Deuterium"));
            }
            else if (!bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.DeuteriumGas) &&
                     bodyAtmosphericComposition.TryGetValue(ResourceSettings.Config.HydrogenGas, out var hydrogenGas))
            {
                var deuteriumAbundance = hydrogenGas.ResourceAbundance / 6420;
                Debug.Log("[KSPI]: Added deuterium to atmosphere with abundance " + deuteriumAbundance);
                bodyAtmosphericComposition.Add(ResourceSettings.Config.DeuteriumLqd,
                    new AtmosphericResource(ResourceSettings.Config.DeuteriumLqd, deuteriumAbundance, "Deuterium"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(ResourceSettings.Config.DeuteriumLqd))
            {
                Debug.Log("[KSPI]: Deuterium is already present in atmosphere specification at " +
                          bodyAtmosphericComposition[ResourceSettings.Config.DeuteriumLqd].ResourceAbundance);
            }
            else
            {
                Debug.Log("[KSPI]: No Hydrogen is present in atmosphere specification, deuterium will not be added");
            }
        }

        private static void AddResource(string outputResourceName, string displayName, CelestialBody body, IDictionary<string, AtmosphericResource> atmosphericResourcesByName, string[] variantNames, double abundanceExponent = 1)
        {
            double finalAbundance;

            AtmosphericResource existingResource = FindAnyExistingAtmosphereVariant(atmosphericResourcesByName, variantNames);
            if (existingResource != null)
            {
                finalAbundance = existingResource.ResourceAbundance;
                Debug.Log("[KSPI]: using kspie resource definition " + outputResourceName + " for " + body.name + " with abundance " + finalAbundance);
            }
            else
            {
                var abundances = new[] { GetAbundance(outputResourceName, body) }.Concat(variantNames.Select(m => GetAbundance(m, body)));
                finalAbundance = abundances.Max();
                Debug.Log("[KSPI]: looked up stock resource definition " + outputResourceName + " for " + body.name + " with abundance " + finalAbundance);

                if (abundanceExponent != 1)
                    finalAbundance = Math.Pow(finalAbundance / 100, abundanceExponent) * 100;
            }

            var resource = new AtmosphericResource(outputResourceName, finalAbundance, displayName, variantNames);
            if (resource.ResourceAbundance <= 0) return;


            if (atmosphericResourcesByName.TryGetValue(outputResourceName, out existingResource))
                atmosphericResourcesByName.Remove(existingResource.ResourceName);
            atmosphericResourcesByName.Add(resource.ResourceName, resource);
        }

        private static AtmosphericResource FindAnyExistingAtmosphereVariant(IDictionary<string, AtmosphericResource> bodyComposition, string[] variants)
        {
            foreach (var variantName in variants)
            {
                if (bodyComposition.TryGetValue(variantName, out var existingResource))
                    return existingResource;
            }
            return null;
        }

        private static float GetAbundance(string resourceName, CelestialBody body)
        {
            return ResourceMap.Instance.GetAbundance(CreateRequest(resourceName, body));
        }

        private static AbundanceRequest CreateRequest(string resourceName, CelestialBody body)
        {
            return new AbundanceRequest
            {
                ResourceType = HarvestTypes.Atmospheric,
                ResourceName = resourceName,
                BodyId = body.flightGlobalsIndex,
                CheckForLock = false,
            };
        }
    }
}
