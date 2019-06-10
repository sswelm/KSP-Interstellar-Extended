using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Resources 
{
    public class AtmosphericResourceHandler 
    {
        private static Dictionary<int, Dictionary<string, AtmosphericResource>> atmospheric_resource_by_body_id = new Dictionary<int, Dictionary<string, AtmosphericResource>>();
        private static Dictionary<string, Dictionary<string, AtmosphericResource>> atmospheric_resource_by_body_name = new Dictionary<string, Dictionary<string, AtmosphericResource>>();

        public static double getAtmosphericResourceContent(CelestialBody body, string resourcename) 
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            AtmosphericResource resource;
            return bodyAtmosphericComposition.TryGetValue(resourcename, out resource) ? resource.ResourceAbundance : 0;
        }

        public static double getAtmosphericResourceContent(CelestialBody body, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].ResourceAbundance : 0;
        }

        public static string getAtmosphericResourceName(CelestialBody body, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].ResourceName : null;
        }

        public static string getAtmosphericResourceDisplayName(CelestialBody body, int resource)
        {
            var bodyAtmosphericComposition = GetAtmosphericCompositionForBody(body);
            return bodyAtmosphericComposition.Count > resource ? bodyAtmosphericComposition.Values.ToList()[resource].DisplayName : null;
        }

        private static Dictionary<string, AtmosphericResource> GetAtmosphericCompositionForKnownCelestrial(string celestrialBodyName)
        {
            Dictionary<string, AtmosphericResource> bodyAtmosphericComposition;

            // first attempt to lookup if its already stored
            if (atmospheric_resource_by_body_name.TryGetValue(celestrialBodyName, out bodyAtmosphericComposition))
                return bodyAtmosphericComposition;

            bodyAtmosphericComposition = CreateFromKspiDefinitionFile(celestrialBodyName);

            // add to database for future reference
            atmospheric_resource_by_body_name.Add(celestrialBodyName, bodyAtmosphericComposition);

            return bodyAtmosphericComposition;
        }

        private static Dictionary<string, AtmosphericResource> CreateFromKspiDefinitionFile(string celestrialBodyName)
        {
            Debug.Log("[KSPI]: searching for atmosphere definition for " + celestrialBodyName);

            try
            {

                foreach (var atmospheric_resource_pack in GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI"))
                {
                    Debug.Log("[KSPI]: Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));

                    var atmospheric_resource_list = atmospheric_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == celestrialBodyName).ToList();
                    if (atmospheric_resource_list.Any())
                    {
                        Debug.Log("[KSPI]: found atmospheric resource list for " + celestrialBodyName);

                        // create atmospheric definition from pack
                        return atmospheric_resource_list.Select(orsc => new AtmosphericResource(
                            orsc.HasValue("resourceName")
                                ? orsc.GetValue("resourceName")
                                : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName")))
                            .ToDictionary(m => m.ResourceName);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]:  CreateFromKspiDefinitionFile for " + celestrialBodyName + " exception " + ex.Message);
            }

            Debug.LogWarning("[KSPI]: Failed to find atmospheric resource list for " + celestrialBodyName);
            return new Dictionary<string, AtmosphericResource>();
        }

        public static Dictionary<string, AtmosphericResource> GetAtmosphericCompositionForBody(CelestialBody body)
        {
            Dictionary<string, AtmosphericResource> bodyAtmosphericComposition;

            // first attempt to lookup if its already stored
            if (atmospheric_resource_by_body_id.TryGetValue(body.flightGlobalsIndex, out bodyAtmosphericComposition))
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

                // if abundance does not approximate 1, create one base on celestrialbody characteristics
                if (sumOfResourceAbundances < 0.8 || sumOfResourceAbundances > 1.2)
                    bodyAtmosphericComposition = GenerateCompositionFromCelestialBody(body);

                // Add rare and isotopes resources
                Debug.Log("[KSPI]: adding trace resources and isotopess to " + body.name);
                AddRaresAndIsotopesToAdmosphereComposition(bodyAtmosphericComposition, body);

                // add missing stock resources
                Debug.Log("[KSPI]: adding missing stock defined resources to " + body.name);
                AddMissingStockResources(body, bodyAtmosphericComposition);

                // add to dictionaries for future reference
                atmospheric_resource_by_body_id.Add(body.flightGlobalsIndex, bodyAtmosphericComposition);
                atmospheric_resource_by_body_name.Add(body.name, bodyAtmosphericComposition);

                Debug.Log("[KSPI]: Succesfully Finished loading atmospheric composition for " + body.name);
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
                    Debug.Log("[KSPI]: celestrial body " + body.name + " is missing an atmosphere");
                    return new Dictionary<string, AtmosphericResource>();
                }

                Debug.Log("[KSPI]: Start GenerateCompositionFromCelestialBody for " + body.name);

                // Lookup homeworld
                var homeworld = FlightGlobals.Bodies.First(b => b.isHomeWorld);
                var presureAtSurface = body.GetPressure(0);

                Debug.Log("[KSPI]: determined " + homeworld.name + " to be the home world");

                Debug.Log("[KSPI]: surface presure " + body.name + " is " + presureAtSurface + " kPa");
                Debug.Log("[KSPI]: surface presure " + homeworld.name + " is " + homeworld.GetPressure(0) + " kPa");
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
                if (body.Mass > homeworld.Mass * 10 && presureAtSurface >= 1000)
                {
                    // Check if the planet  hot gas planet
                    if (body.Mass < homeworld.Mass * 25 && body.atmosphereTemperatureSeaLevel > 700) // Higher than 700 K
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Gliese 436b");
                        return GetAtmosphericCompositionForKnownCelestrial("Gliese436b");
                    }

                    if (body.atmosphereTemperatureSeaLevel > 1400) // Class V: Silicate clouds
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere is a Class V: Silicate clouds planet");
                        return GetAtmosphericCompositionForKnownCelestrial("Jupiter");
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 900) // Class IV: Alkali metals
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere is a Class IV: Alkali metals planet");
                        return GetAtmosphericCompositionForKnownCelestrial("Jupiter");
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 300) // Class III: Cloudless
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere is a Class III: Cloudless planet");
                        return GetAtmosphericCompositionForKnownCelestrial("Jupiter");
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 150) // Class II: Water clouds
                    {
                        if (body.atmosphereDepth < 500000 )
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Gauss");
                            return GetAtmosphericCompositionForKnownCelestrial("Gauss");
                        }
                        else
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Otho");
                            return GetAtmosphericCompositionForKnownCelestrial("Otho");
                        }
                    }
                    else if (body.atmosphereTemperatureSeaLevel > 80)  // Class I: Ammonia clouds
                    {
                        // Check if its a super giant
                        if (body.Mass > homeworld.Mass * 80)
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Jupiter");
                            return GetAtmosphericCompositionForKnownCelestrial("Jupiter");
                        }
                        if (body.Mass > homeworld.Mass * 60)
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Nero");
                            return GetAtmosphericCompositionForKnownCelestrial("Nero");
                        }

                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Saturn");
                        return GetAtmosphericCompositionForKnownCelestrial("Saturn");
                    }
                    else // ice giant
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Uranus");
                        return GetAtmosphericCompositionForKnownCelestrial("Uranus");
                    }
                }

                if (presureAtSurface >= 1000 && body.atmosphereTemperatureSeaLevel > 500)	// Higher than 1000 kPa and 500 K
                {
                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Venus");
                    return GetAtmosphericCompositionForKnownCelestrial("Venus");
                }

                if (body.atmosphereTemperatureSeaLevel < 200) // // Colder than 200K
                {
                    if (body.atmosphereTemperatureSeaLevel > 150)
                    {
                        if (presureAtSurface < 100)
                        {
                            Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Gratian");
                            return GetAtmosphericCompositionForKnownCelestrial("Gratian");
                        }
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Titan");
                        return GetAtmosphericCompositionForKnownCelestrial("Titan");
                    }

                    if (body.atmosphereTemperatureSeaLevel < 50) // Surface temperature colder than 50K
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Pluto");
                        return GetAtmosphericCompositionForKnownCelestrial("Pluto");
                    }

                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Triton");
                    return GetAtmosphericCompositionForKnownCelestrial("Triton");
                }

                if (body.atmosphereContainsOxygen)
                {
                    if (body.Mass > homeworld.Mass * 5)
                    {
                        Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Tellumo");
                        return GetAtmosphericCompositionForKnownCelestrial("Tellumo");
                    }

                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Earth");
                    return GetAtmosphericCompositionForKnownCelestrial("Earth");
                }

                if (body.atmosphereTemperatureSeaLevel > 270)
                {
                    Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Niven");
                    return GetAtmosphericCompositionForKnownCelestrial("Niven");
                }


                // Otherwise is a boring Mars like planet
                Debug.Log("[KSPI]: determined " + body.name + " atmosphere to be like Mars");
                return GetAtmosphericCompositionForKnownCelestrial("Mars");
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]: Exception while generating atmosphere composition from celestrial atmosphere properties for " + body.name + " : " + ex);
            }

            return new Dictionary<string, AtmosphericResource>();
        }

        private static void GenerateCompositionFromResourceAbundances(CelestialBody body, Dictionary<string, AtmosphericResource> bodyComposition)
        {
            try
            {
                // mundane resources
                AddResource(InterstellarResourcesConfiguration._LIQUID_AMMONIA, "Ammonia", body, bodyComposition, new[] { "LqdAmmonia", "AmmoniaGas", "Ammonia", "NH3"}, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_ARGON, "Argon", body, bodyComposition, new[] { "LqdArgon", "ArgonGas", "Argon", "Ar" }, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_CO2, "CarbonDioxide", body, bodyComposition, new[] { "LqdCO2", "CarbonDioxideGas", "CarbonDioxide", "CO2" }, 1);
                AddResource(InterstellarResourcesConfiguration._CARBONMONOXIDE_LIQUID, "CarbonMonoxide", body, bodyComposition, new[] { "LqdCO", "CarbonMonoxideGas", "CarbonMonoxide", "CO", }, 1);
                AddResource(InterstellarResourcesConfiguration._CHLORINE, "Chlorine", body, bodyComposition, new[] { "Chlorine", "ChlorineGas", "LqdChlorine", "Cl", }, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER, "HeavyWater", body, bodyComposition, new[] { "DeuteriumWater", "HeavyWater", "D2O" }, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_KRYPTON, "Krypton", body, bodyComposition, new[] { "LqdKrypton", "KryptonGas", "Krypton" , "Kr"}, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_METHANE, "Methane", body, bodyComposition, new[] { "LqdMethane", "MethaneGas", "Methane", "CH4" }, 1);
                AddResource(InterstellarResourcesConfiguration._NITROGEN_LIQUID, "Nitrogen", body, bodyComposition, new[] { "LqdNitrogen", "NitrogenGas", "Nitrogen", "N", "N2" }, 1);
                AddResource(InterstellarResourcesConfiguration._NEON_LIQUID, "Neon", body, bodyComposition, new[] { "LqdNeon", "NeonGas", "Neon", "Ne" }, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_OXYGEN, "Oxygen", body, bodyComposition, new[] { "LqdOxygen", "OxygenGas", "Oxygen", "O", "O2" }, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_WATER, "LqdWater", body, bodyComposition, new[] { "LqdWater", "Water", "WaterGas", "DihydrogenMonoxide", "H2O", "DHMO" }, 1);
                AddResource(InterstellarResourcesConfiguration._LIQUID_XENON, "Xenon", body, bodyComposition, new[] { "LqdXenon", "XenonGas", "Xenon", "Xe" }, 1);
                AddResource(InterstellarResourcesConfiguration._SODIUM, "Sodium", body, bodyComposition, new[] { "LqdSodium", "SodiumGas", "Sodium", "Natrium", "Na" }, 1);
                AddResource(InterstellarResourcesConfiguration._LITHIUM7, "Lithium7", body, bodyComposition, new[] { "Lithium", "LithiumGas", "Lithium7", "Li", "Li7" }, 1);
                AddResource(InterstellarResourcesConfiguration._LITHIUM6, "Lithium6", body, bodyComposition, new[] { "Lithium6", "Lithium-6", "Li6" }, 1);
                AddResource(InterstellarResourcesConfiguration._HYDROGEN_LIQUID, "Hydrogen", body, bodyComposition, new[] { "LqdHydrogen", "HydrogenGas", "Hydrogen", "LiquidHydrogen", "H2", "Protium", "LqdProtium", "H" }, 1);
                AddResource(InterstellarResourcesConfiguration._HELIUM4_LIQUID, "Helium-4", body, bodyComposition, new[] { "LqdHe4", "Helium4Gas", "Helium4", "Helium-4", "He4Gas", "He4", "LqdHelium", "Helium", "HeliumGas", "He" }, 1);

                // exotic isotopes
                AddResource(InterstellarResourcesConfiguration._HELIUM3_LIQUID, "Helium-3", body, bodyComposition, new[] { "LqdHe3", "Helium3Gas", "Helium3", "Helium-3", "He3Gas", "He3", "LqdHelium3" }, 5);
                AddResource(InterstellarResourcesConfiguration._DEUTERIUM_LIQUID, "Deuterium", body, bodyComposition, new[] { "LqdDeuterium", "DeuteriumGas", "Deuterium", "D" }, 5);
                AddResource(InterstellarResourcesConfiguration._LIQUID_TRITIUM, "Tritium", body, bodyComposition, new[] { "LqdTritium", "TritiumGas", "Tritium", "T" }, 5);
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

            foreach (var resoureName in allResources)
            {
                // add resource if missing
                AddMissingResource(resoureName, body, bodyComposition);
            }
        }

        private static void AddMissingResource(string resourname, CelestialBody body, Dictionary<string, AtmosphericResource> bodyComposition)
        {
            // verify it is a defined resource
            var definition = PartResourceLibrary.Instance.GetDefinition(resourname);
            if (definition == null)
            {
                Debug.LogWarning("[KSPI]: AddMissingResource : Failed to find resource definition for '" + resourname + "'");
                return;
            }

            // skip it already registred or used as a Synonym
            if (bodyComposition.Values.Any(m => m.ResourceName == definition.name || m.DisplayName == definition.displayName || m.Synonyms.Contains(definition.name)))
            {
                Debug.Log("[KSPI]: AddMissingResource : Already found existing composition for '" + resourname + "'");
                return;
            }

            // retreive abundance
            var abundance = GetAbundance(definition.name, body);
            if (abundance <= 0)
            {
                Debug.LogWarning("[KSPI]: AddMissingResource : Abundance for resource '" + resourname + "' was " + abundance);
                return;
            }

            // create resource from definition and abundance
            var resource = new AtmosphericResource(definition, abundance);

            // add to composition
            Debug.Log("[KSPI]: AddMissingResource : add resource '" + resourname + "'");
            bodyComposition.Add(resource.ResourceName, resource);
        }

        private static void AddRaresAndIsotopesToAdmosphereComposition(IDictionary<string, AtmosphericResource> bodyAtmosphericComposition, CelestialBody body)
        {
            // add heavywater based on water abundance in atmosphere
            AtmosphericResource water;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._LIQUID_WATER, out water))
            {
                var heavywaterAbundance = water.ResourceAbundance / 6420;
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER,  new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_HEAVYWATER, heavywaterAbundance, InterstellarResourcesConfiguration._LIQUID_HEAVYWATER));
            }

            // add helium4 comparable to earth
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._HELIUM4_LIQUID))
            {
                const double helium4Abundance = 5.2e-6;
                Debug.Log("[KSPI]: Added helum-4 to atmosphere with abundance " + helium4Abundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._HELIUM4_LIQUID, new AtmosphericResource(InterstellarResourcesConfiguration._HELIUM4_LIQUID, helium4Abundance, "Helium-4"));
            }
            else
                Debug.Log("[KSPI]: Helium is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._HELIUM4_LIQUID].ResourceAbundance);

            // if helium3 is undefined, but helium is, derive it
            AtmosphericResource helium;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._HELIUM3_LIQUID) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._HELIUM4_LIQUID, out helium))
            {

                var helium3Abundance = body.GetPressure(0) > 1000
                    ? helium.ResourceAbundance * 0.001
                    : helium.ResourceAbundance * 1.38e-6;

                Debug.Log("[KSPI]: Added helium-3 to atmosphere eith abundance " + helium3Abundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._HELIUM3_LIQUID, new AtmosphericResource(InterstellarResourcesConfiguration._HELIUM3_LIQUID, helium3Abundance, "Helium-3"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._HELIUM3_LIQUID))
                Debug.Log("[KSPI]: Helium-3 is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._HELIUM3_LIQUID].ResourceAbundance);
            else
                Debug.Log("[KSPI]: No Helium is present in atmosphere specification, helium-4 will not be added");

            // if deteurium is undefined, but hydrogen is, derive it
            AtmosphericResource hydrogen;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._DEUTERIUM_LIQUID) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._HYDROGEN_LIQUID, out hydrogen))
            {
                var deuteriumAbundance = hydrogen.ResourceAbundance / 6420;
                Debug.Log("[KSPI]: Added deuterium to atmosphere with abundance " + deuteriumAbundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._DEUTERIUM_LIQUID, new AtmosphericResource(InterstellarResourcesConfiguration._DEUTERIUM_LIQUID, deuteriumAbundance, "Deuterium"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._DEUTERIUM_LIQUID))
                Debug.Log("[KSPI]: Deuterium is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._DEUTERIUM_LIQUID].ResourceAbundance);
            else 
                Debug.Log("[KSPI]: No Hydrogen is present in atmosphere specification, deuterium will not be added");

            // if nitrogen-15 is undefined, but nitrogen is, derive it
            AtmosphericResource nitrogen;
            if (!bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15) && bodyAtmosphericComposition.TryGetValue(InterstellarResourcesConfiguration._NITROGEN_LIQUID, out nitrogen))
            {
                var nitrogen15Abundance = nitrogen.ResourceAbundance * 0.00364;
                Debug.Log("[KSPI]: Added nitrogen-15 to atmosphere with abundance " + nitrogen15Abundance);
                bodyAtmosphericComposition.Add(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15, new AtmosphericResource(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15, nitrogen15Abundance, "Nitrogen-15"));
            }
            else if (bodyAtmosphericComposition.ContainsKey(InterstellarResourcesConfiguration._LIQUID_NITROGEN_15))
                Debug.Log("[KSPI]: Nitrogen-15 is already present in atmosphere specification at " + bodyAtmosphericComposition[InterstellarResourcesConfiguration._LIQUID_NITROGEN_15].ResourceAbundance);
            else
                Debug.Log("[KSPI]: No Nitrogen is present in atmosphere specification, nitrogen-15 will not be added");
        }

        private static void AddResource(string outputResourname, string displayname, CelestialBody body, IDictionary<string, AtmosphericResource> atmosphericResourcesByName, string[] variantNames, double abundanceExponent = 1)
        {
            double finalAbundance;

            AtmosphericResource existingResource = FindAnyExistingAtmosphereVariant(atmosphericResourcesByName, variantNames);
            if (existingResource != null)
            {
                finalAbundance = existingResource.ResourceAbundance;
                Debug.Log("[KSPI]: using kspie resource definition " + outputResourname + " for " + body.name + " with abundance " + finalAbundance);
            }
            else
            {
                var abundances = new[] { GetAbundance(outputResourname, body) }.Concat(variantNames.Select(m => GetAbundance(m, body)));
                finalAbundance = abundances.Max();
                Debug.Log("[KSPI]: looked up stock resource definition " + outputResourname + " for " + body.name + " with abundance " + finalAbundance);

                if (abundanceExponent != 1)
                    finalAbundance = Math.Pow(finalAbundance / 100, abundanceExponent) * 100;
            }

            var resource = new AtmosphericResource(outputResourname, finalAbundance, displayname, variantNames);
            if (resource.ResourceAbundance <= 0) return;


            if (atmosphericResourcesByName.TryGetValue(outputResourname, out existingResource))
                atmosphericResourcesByName.Remove(existingResource.ResourceName);
            atmosphericResourcesByName.Add(resource.ResourceName, resource);
        }

        private static AtmosphericResource FindAnyExistingAtmosphereVariant(IDictionary<string, AtmosphericResource> bodyComposition, string[] variants)
        {
            AtmosphericResource existingResource;
            foreach (var variantName in variants)
            {
                if (bodyComposition.TryGetValue(variantName, out existingResource))
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