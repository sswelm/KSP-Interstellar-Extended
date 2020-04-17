using FNPlugin.Wasteheat;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Reactors
{
    [KSPModule("Nuclear Salt Water Reactor")]
    class InterstellarNSWR : InterstellarFissionMSRGC { }

    [KSPModule("Nuclear Thermal Reactor")]
    class InterstellarFissionNTR : InterstellarFissionMSRGC { }

    [KSPModule("Fission Reactor")]
    class InterstellarFissionReactor : InterstellarFissionMSRGC { }

    [KSPModule("Molten Salt Reactor")]
    class InterstellarMoltenSaltReactor : InterstellarFissionMSRGC { }

    [KSPModule("Fission Reactor")]
    class InterstellarFissionMSRGC : InterstellarReactor, INuclearFuelReprocessable
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField]
        public double actinidesModifer;
        [KSPField]
        public double temperatureThrotleExponent = 0.5;
        [KSPField(guiActive = false)]
        public double temp_scale;
        [KSPField(guiActive = false)]
        public double temp_diff;
        [KSPField(guiActive = false)]
        public double minimumTemperature;

        PartResourceDefinition fluorineGasDefinition;
        PartResourceDefinition depletedFuelDefinition;
        PartResourceDefinition enrichedUraniumDefinition;
        PartResourceDefinition oxygenGasDefinition;

        double fluorineDepletedFuelVolumeMultiplier;
        double enrichedUraniumVolumeMultiplier;
        double depletedToEnrichVolumeMultplier;
        double oxygenDepletedUraniumVolumeMultipler;
        double reactorFuelMaxAmount;

        public double WasteToReprocess { get { return part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides) ? part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount : 0; } }

        [KSPEvent(guiName = "#LOC_KSPIE_FissionMSRGC_SwapFuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.5f)]//Swap Fuel
        public void SwapFuelMode()
        {
            if (!part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides) || part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount > 0.01) return;
            defuelCurrentFuel();
            if (IsCurrentFuelDepleted())
            {
                DisableResources();
                SwitchFuelType();
                EnableResources();
                Refuel();
            }
        }

        [KSPEvent(guiName = "#LOC_KSPIE_FissionMSRGC_SwapFuel", guiActiveEditor = true, guiActive = false)]//Swap Fuel
        public void EditorSwapFuel()
        {
            if (fuel_modes.Count == 1)
                return;

            DisableResources();
            SwitchFuelType();
            EnableResources();

            var modesAvailable = checkFuelModes();
            // Hide Switch Mode button if theres only one mode for the selected fuel type available
            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = modesAvailable > 1;
        }

        [KSPEvent(guiName = "#LOC_KSPIE_FissionMSRGC_SwitchMode", guiActiveEditor = true, guiActiveUnfocused = true, guiActive = true)]//Switch Mode
        public void SwitchMode()
        {
            var startFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            var currentFirstFuelType = startFirstFuelType;

            // repeat until found same or differnt fuelmode with same kind of primary fuel
            do
            {
                fuel_mode++;
                if (fuel_mode >= fuel_modes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuel_modes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName != startFirstFuelType.ResourceName);

            fuelModeStr = CurrentFuelMode.ModeGUIName;

            int modesAvailable = checkFuelModes();
            // Hide Switch Mode button if theres only one mode for the selected fuel type available
            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = modesAvailable > 1;
        }

        [KSPEvent(guiName = "#LOC_KSPIE_FissionMSRGC_ManualRestart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Restart
        public void ManualRestart()
        {
            // verify any of the fuel types has at least 50% avaialbility inside the reactor
            if (CurrentFuelMode.Variants.Any(variant => variant.ReactorFuels.All(fuel => GetLocalResourceRatio(fuel) > 0.5)))
                IsEnabled = true;
        }

        [KSPEvent(guiName = "#LOC_KSPIE_FissionMSRGC_ManualShutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Shutdown
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        [KSPEvent(guiName = "#LOC_KSPIE_FissionMSRGC_Refuel", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Refuel
        public void Refuel()
        {
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                // avoid exceptions, just in case
                if (!part.Resources.Contains(fuel.ResourceName) || !part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides)) return;

                var fuel_reactor = part.Resources[fuel.ResourceName];
                var actinides_reactor = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides];
                var fuel_resources = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == fuel.ResourceName && r != fuel_reactor)).ToList();

                double spare_capacity_for_fuel = fuel_reactor.maxAmount - actinides_reactor.amount - fuel_reactor.amount;
                fuel_resources.ForEach(res =>
                {
                    double resource_available = res.amount;
                    double resource_added = Math.Min(resource_available, spare_capacity_for_fuel);
                    fuel_reactor.amount += resource_added;
                    res.amount -= resource_added;
                    spare_capacity_for_fuel -= resource_added;
                });
            }
        }

        public override bool IsFuelNeutronRich { get { return !CurrentFuelMode.Aneutronic; } }

        public override bool IsNuclear { get { return true; } }

        public override double MaximumThermalPower
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return base.MaximumThermalPower;

                if (CheatOptions.UnbreakableJoints)
                {
                    actinidesModifer = 1;
                    return base.MaximumThermalPower;
                }

                var actinideResourse = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides];

                if (actinideResourse != null && actinideResourse.maxAmount > 0)
                {
                    double fuel_actinide_mass_ratio = Math.Pow(actinideResourse.amount / actinideResourse.maxAmount, 1 / CurrentFuelMode.NormalisedReactionRate);

                    actinidesModifer = Math.Sqrt(fuel_actinide_mass_ratio);

                    return base.MaximumThermalPower * actinidesModifer;
                }

                return base.MaximumThermalPower;
            }
        }

        public override double CoreTemperature
        {
            get
            {
                if (!CheatOptions.IgnoreMaxTemperature && HighLogic.LoadedSceneIsFlight && !isupgraded && powerPcnt > min_throttle * 100)
                {
                    var baseCoreTemperature = base.CoreTemperature;

                    if (minimumTemperature > 0)
                        temp_scale = minimumTemperature;
                    else if (vessel != null && FNRadiator.hasRadiatorsForVessel(vessel))
                        temp_scale = FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
                    else
                        temp_scale = baseCoreTemperature / 2;

                    temp_diff = (baseCoreTemperature - temp_scale) * Math.Pow(powerPcnt / 100, temperatureThrotleExponent);
                    return Math.Min(temp_scale + temp_diff, baseCoreTemperature);
                }
                else
                    return base.CoreTemperature;
            }
        }

        public override void OnUpdate()
        {
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            Events["Refuel"].active = Events["Refuel"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["Refuel"].guiName = "Refuel " + (CurrentFuelMode != null ? CurrentFuelMode.ModeGUIName : "");
            Events["SwapFuelMode"].active = Events["SwapFuelMode"].guiActiveUnfocused = fuel_modes.Count > 1 && !IsEnabled && !decay_ongoing;
            Events["SwapFuelMode"].guiActive = Events["SwapFuelMode"].guiActiveUnfocused = fuel_modes.Count > 1;

            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = checkFuelModes() > 1;
            Events["EditorSwapFuel"].guiActiveEditor = fuel_modes.Count > 1;

            base.OnUpdate();
        }

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("[KSPI]: OnStart MSRGC " + part.name);

            // start as normal
            base.OnStart(state);

            // auto switch if current fuel mode is depleted
            if (IsCurrentFuelDepleted())
            {
                fuel_mode++;
                if (fuel_mode >= fuel_modes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuel_modes[fuel_mode];
            }

            fuelModeStr = CurrentFuelMode.ModeGUIName;

            oxygenGasDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.OxygenGas);
            fluorineGasDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.FluorineGas);
            depletedFuelDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.DepletedFuel);
            enrichedUraniumDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.EnrichedUrarium);

            depletedToEnrichVolumeMultplier = (double)(decimal)enrichedUraniumDefinition.density / (double)(decimal)depletedFuelDefinition.density;
            fluorineDepletedFuelVolumeMultiplier = ((19 * 4) / 232d) * ((double)(decimal)depletedFuelDefinition.density / fluorineGasDefinition.density);
            enrichedUraniumVolumeMultiplier = (232d / (16 * 2 + 232d)) * ((double)(decimal)depletedFuelDefinition.density / enrichedUraniumDefinition.density);
            oxygenDepletedUraniumVolumeMultipler = ((16 * 2) / (16 * 2 + 232d)) * ((double)(decimal)depletedFuelDefinition.density / oxygenGasDefinition.density);

            var mainReactorFuel = part.Resources.Get(CurrentFuelMode.Variants.First().ReactorFuels.First().ResourceName);
            if (mainReactorFuel != null)
                reactorFuelMaxAmount = part.Resources.Get(CurrentFuelMode.Variants.First().ReactorFuels.First().ResourceName).maxAmount;

            foreach (ReactorFuelType fuelMode in fuel_modes)
            {
                foreach (ReactorFuel fuel in fuelMode.Variants.First().ReactorFuels)
                {
                    var resource = part.Resources.Get(fuel.ResourceName);
                    if (resource == null)
                        // non-tweakable resources
                        part.Resources.Add(fuel.ResourceName, 0, 0, true, false, false, true, 0);
                }
            }

            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = checkFuelModes() > 1;
            Events["SwapFuelMode"].guiActive = Events["SwapFuelMode"].guiActiveUnfocused = fuel_modes.Count > 1;
            Events["EditorSwapFuel"].guiActiveEditor = fuel_modes.Count > 1;
        }

        public override void OnFixedUpdate()
        {
            // if reactor is overloaded with actinides, stop functioning
            if (IsEnabled && part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides))
            {
                if (part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount >= part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].maxAmount)
                {
                    part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].maxAmount;
                    IsEnabled = false;
                }
            }
            base.OnFixedUpdate();
        }

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }

        public double ReprocessFuel(double rate)
        {
            if (part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides))
            {
                var actinides = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides];
                var new_actinides_amount = Math.Max(actinides.amount - rate, 0);
                var actinides_change = actinides.amount - new_actinides_amount;
                actinides.amount = new_actinides_amount;

                var depleted_fuels_request = actinides_change * 0.2;
                var depleted_fuels_produced = -Part.RequestResource(depletedFuelDefinition.id, -depleted_fuels_request, ResourceFlowMode.STAGE_PRIORITY_FLOW);

                // first try to replace depletedfuel with enriched uranium
                var enrichedUraniumRequest = depleted_fuels_produced * enrichedUraniumVolumeMultiplier;
                var enrichedUraniumRetrieved = Part.RequestResource(enrichedUraniumDefinition.id, enrichedUraniumRequest, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                var receivedEnrichedUraniumFraction = enrichedUraniumRequest > 0 ? enrichedUraniumRetrieved / enrichedUraniumRequest : 0;

                // if missing fluorine is dumped
                var oxygenChange = -Part.RequestResource(oxygenGasDefinition.id, -depleted_fuels_produced * oxygenDepletedUraniumVolumeMultipler * receivedEnrichedUraniumFraction, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                var fluorineChange = -Part.RequestResource(fluorineGasDefinition.id, -depleted_fuels_produced * fluorineDepletedFuelVolumeMultiplier * (1 - receivedEnrichedUraniumFraction), ResourceFlowMode.STAGE_PRIORITY_FLOW);

                var reactorFuels = CurrentFuelMode.Variants.First().ReactorFuels;
                var sum_useage_per_mw = reactorFuels.Sum(fuel => fuel.AmountFuelUsePerMJ * fuelUsePerMJMult);

                foreach (ReactorFuel fuel in reactorFuels)
                {
                    var fuel_resource = part.Resources[fuel.ResourceName];
                    var powerFraction = sum_useage_per_mw > 0.0 ? fuel.AmountFuelUsePerMJ * fuelUsePerMJMult / sum_useage_per_mw : 1;
                    var new_fuel_amount = Math.Min(fuel_resource.amount + ((depleted_fuels_produced * 4) + (depleted_fuels_produced * receivedEnrichedUraniumFraction)) * powerFraction * depletedToEnrichVolumeMultplier, fuel_resource.maxAmount);
                    fuel_resource.amount = new_fuel_amount;
                }

                return actinides_change;
            }
            return 0;
        }

        // This Methods loads the correct fuel mode
        public override void SetDefaultFuelMode()
        {
            if (fuel_modes == null)
            {
                Debug.Log("[KSPI]: MSRC SetDefaultFuelMode - load fuel modes");
                fuel_modes = GetReactorFuelModes();
            }

            CurrentFuelMode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
        }

        private void DisableResources()
        {
            bool editor = HighLogic.LoadedSceneIsEditor;
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                var resource = part.Resources.Get(fuel.ResourceName);
                if (resource != null)
                {
                    if (editor)
                    {
                        resource.amount = 0;
                        resource.isTweakable = false;
                    }
                    resource.maxAmount = 0;
                    
                }
            }
        }

        private void EnableResources()
        {
            bool editor = HighLogic.LoadedSceneIsEditor;
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                var resource = part.Resources.Get(fuel.ResourceName);
                if (resource != null)
                {
                    if (editor)
                    {
                        resource.amount = reactorFuelMaxAmount;
                        resource.isTweakable = true;
                    }
                    resource.maxAmount = reactorFuelMaxAmount;
                }
            }
        }

        private void SwitchFuelType()
        {
            var startFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            var currentFirstFuelType = startFirstFuelType;

            do
            {
                fuel_mode++;
                if (fuel_mode >= fuel_modes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuel_modes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName == startFirstFuelType.ResourceName);

            fuelModeStr = CurrentFuelMode.ModeGUIName;
        }

        private void defuelCurrentFuel()
        {
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                var fuel_reactor = part.Resources[fuel.ResourceName];
                var swap_resource_list = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == fuel.ResourceName && r != fuel_reactor)).ToList();

                swap_resource_list.ForEach(res =>
                {
                    double spare_capacity_for_fuel = res.maxAmount - res.amount;
                    double fuel_added = Math.Min(fuel_reactor.amount, spare_capacity_for_fuel);
                    fuel_reactor.amount -= fuel_added;
                    res.amount += fuel_added;
                });
            }
        }

        private bool IsCurrentFuelDepleted()
        {
            return CurrentFuelMode.Variants.First().ReactorFuels.Any(fuel => GetFuelAvailability(fuel) < 0.001);
        }


        // Returns the number of fuelmodes available for the currently selected fueltype 
        public int checkFuelModes()
        {
            int modesAvailable = 0;
            var fuelType = CurrentFuelMode.Variants.First().ReactorFuels.First().FuelName;
            for (int n = 0; n < fuel_modes.Count; n++)
            {
                var current_mode = fuel_modes[n].Variants.First().ReactorFuels.First().FuelName;
                if (current_mode == fuelType)
                {
                    modesAvailable++;
                }
            }
            return modesAvailable;
        }
    }
}
