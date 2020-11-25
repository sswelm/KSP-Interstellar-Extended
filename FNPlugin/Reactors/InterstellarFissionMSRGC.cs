using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
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
        public double actinidesModifer = 1;
        [KSPField]
        public double temperatureThrotleExponent = 0.5;
        [KSPField(guiActive = false)]
        public double temp_scale;
        [KSPField(guiActive = false)]
        public double temp_diff;
        [KSPField(guiActive = false)]
        public double minimumTemperature = 0;
        [KSPField(guiActive = false)]
        public bool canDumpActinides = false;

        PartResourceDefinition fluorineGasDefinition;
        PartResourceDefinition depletedFuelDefinition;
        PartResourceDefinition enrichedUraniumDefinition;
        PartResourceDefinition oxygenGasDefinition;

        double fluorineDepletedFuelVolumeMultiplier;
        double enrichedUraniumVolumeMultiplier;
        double depletedToEnrichVolumeMultplier;
        double oxygenDepletedUraniumVolumeMultipler;
        double reactorFuelMaxAmount;

        public override bool IsFuelNeutronRich => !CurrentFuelMode.Aneutronic;

        public override bool IsNuclear => true;

        public double WasteToReprocess => part.Resources.Contains(ResourceSettings.Config.Actinides) ? part.Resources[ResourceSettings.Config.Actinides].amount : 0;

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionMSRGC_Dump_Actinides", guiActiveEditor = false, guiActive = true)]
        public void DumpActinides()
        {
            var actinides = part.Resources[ResourceSettings.Config.Actinides];
            if (actinides == null)
            {
                Debug.LogError("[KSPI]: actinides not found on " + part.partInfo.title);
                return;
            }

            var uranium233 = part.Resources[ResourceSettings.Config.Uranium233];
            if (uranium233 == null)
            {
                Debug.LogError("[KSPI]: uranium-233 not found on " + part.partInfo.title);
                return;
            }

            actinides.amount = 0;
            uranium233.amount = Math.Max(0, uranium233.amount - actinides.maxAmount);

            var message = Localizer.Format("#LOC_Dumped_Actinides");
            ScreenMessages.PostScreenMessage(message, 20.0f, ScreenMessageStyle.UPPER_CENTER);
            Debug.Log("[KSPI]: " + message);
        }

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionMSRGC_SwapFuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.5f)]//Swap Fuel
        public void SwapFuelMode()
        {
            if (!part.Resources.Contains(ResourceSettings.Config.Actinides) || part.Resources[ResourceSettings.Config.Actinides].amount > 0.01) return;
            DefuelCurrentFuel();
            if (IsCurrentFuelDepleted())
            {
                DisableResources();
                SwitchFuelType();
                EnableResources();
                Refuel();
            }
        }

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionMSRGC_SwapFuel", guiActiveEditor = true, guiActive = false)]//Swap Fuel
        public void EditorSwapFuel()
        {
            if (fuelModes.Count == 1)
                return;

            DisableResources();
            SwitchFuelType();
            EnableResources();

            var modesAvailable = CheckFuelModes();
            // Hide Switch Mode button if theres only one mode for the selected fuel type available
            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = modesAvailable > 1;
        }

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionMSRGC_SwitchMode", guiActiveEditor = true, guiActiveUnfocused = true, guiActive = true)]//Switch Mode
        public void SwitchMode()
        {
            var startFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            var currentFirstFuelType = startFirstFuelType;

            // repeat until found same or differnt fuelmode with same kind of primary fuel
            do
            {
                fuel_mode++;
                if (fuel_mode >= fuelModes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuelModes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName != startFirstFuelType.ResourceName);

            fuelModeStr = CurrentFuelMode.ModeGUIName;

            int modesAvailable = CheckFuelModes();
            // Hide Switch Mode button if theres only one mode for the selected fuel type available
            Events["SwitchMode"].guiActiveEditor = Events["SwitchMode"].guiActive = Events["SwitchMode"].guiActiveUnfocused = modesAvailable > 1;
        }

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionMSRGC_ManualRestart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Restart
        public void ManualRestart()
        {
            // verify any of the fuel types has at least 50% availability inside the reactor
            if (CurrentFuelMode.Variants.Any(variant => variant.ReactorFuels.All(fuel => GetLocalResourceRatio(fuel) > 0.5)))
                IsEnabled = true;
        }

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionMSRGC_ManualShutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Shutdown
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        [KSPEvent(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FissionMSRGC_Refuel", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Refuel
        public void Refuel()
        {
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                // avoid exceptions, just in case
                if (!part.Resources.Contains(fuel.ResourceName) || !part.Resources.Contains(ResourceSettings.Config.Actinides)) return;

                var fuelReactor = part.Resources[fuel.ResourceName];
                var actinidesReactor = part.Resources[ResourceSettings.Config.Actinides];
                var fuelResources = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == fuel.ResourceName && r != fuelReactor)).ToList();

                double spareCapacityForFuel = fuelReactor.maxAmount - actinidesReactor.amount - fuelReactor.amount;
                fuelResources.ForEach(res =>
                {
                    double resourceAvailable = res.amount;
                    double resourceAdded = Math.Min(resourceAvailable, spareCapacityForFuel);
                    fuelReactor.amount += resourceAdded;
                    res.amount -= resourceAdded;
                    spareCapacityForFuel -= resourceAdded;
                });
            }
        }

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

                var actinidesResource = part.Resources[ResourceSettings.Config.Actinides];

                if (actinidesResource != null)
                {
                    var fuelActinideMassRatio = 1 - actinidesResource.amount / actinidesResource.maxAmount;

                    actinidesModifer = Math.Pow(fuelActinideMassRatio * fuelActinideMassRatio, CurrentFuelMode.NormalisedReactionRate);

                    return base.MaximumThermalPower * actinidesModifer;
                }

                return base.MaximumThermalPower;
            }
        }

        protected override void WindowReactorStatusSpecificOverride()
        {
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_FissionMSRGC_Actinides_Poisoning"), (100 - actinidesModifer * 100).ToString("0.000000") + "%", boldStyle, textStyle);
        }

        public override double CoreTemperature
        {
            get
            {
                if (!CheatOptions.IgnoreMaxTemperature && HighLogic.LoadedSceneIsFlight && !isupgraded && powerPcnt >= minThrottle * 100)
                {
                    var baseCoreTemperature = base.CoreTemperature;

                    if (minimumTemperature > 0)
                        temp_scale = minimumTemperature;
                    else if (vessel != null && FNRadiator.HasRadiatorsForVessel(vessel))
                        temp_scale = FNRadiator.GetAverageMaximumRadiatorTemperatureForVessel(vessel);
                    else
                        temp_scale = baseCoreTemperature / 2;

                    temp_diff = (baseCoreTemperature - temp_scale) * Math.Pow(powerPcnt / 100, temperatureThrotleExponent);
                    return Math.Min(temp_scale + temp_diff, actinidesModifer * baseCoreTemperature);
                }
                else
                    return actinidesModifer * base.CoreTemperature;
            }
        }

        public override double MaxCoreTemperature => actinidesModifer * base.CoreTemperature;

        public override void OnUpdate()
        {
            Events[nameof(ManualShutdown)].active = Events[nameof(ManualShutdown)].guiActiveUnfocused = IsEnabled;
            Events[nameof(Refuel)].active = Events[nameof(Refuel)].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events[nameof(Refuel)].guiName = "Refuel " + (CurrentFuelMode != null ? CurrentFuelMode.ModeGUIName : "");
            Events[nameof(SwapFuelMode)].active = Events[nameof(SwapFuelMode)].guiActiveUnfocused = fuelModes.Count > 1 && !IsEnabled && !decay_ongoing;
            Events[nameof(SwapFuelMode)].guiActive = Events[nameof(SwapFuelMode)].guiActiveUnfocused = fuelModes.Count > 1;

            Events[nameof(SwitchMode)].guiActiveEditor = Events[nameof(SwitchMode)].guiActive = Events[nameof(SwitchMode)].guiActiveUnfocused = CheckFuelModes() > 1;
            Events[nameof(EditorSwapFuel)].guiActiveEditor = fuelModes.Count > 1;
            Events[nameof(DumpActinides)].guiActive = canDumpActinides;

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
                if (fuel_mode >= fuelModes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuelModes[fuel_mode];
            }

            fuelModeStr = CurrentFuelMode.ModeGUIName;

            oxygenGasDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.OxygenGas);
            fluorineGasDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.FluorineGas);
            depletedFuelDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.DepletedFuel);
            enrichedUraniumDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.EnrichedUranium);

            depletedToEnrichVolumeMultplier = enrichedUraniumDefinition.density / depletedFuelDefinition.density;
            fluorineDepletedFuelVolumeMultiplier = ((19 * 4) / 232d) * (depletedFuelDefinition.density / fluorineGasDefinition.density);
            enrichedUraniumVolumeMultiplier = (232d / (16 * 2 + 232d)) * (depletedFuelDefinition.density / enrichedUraniumDefinition.density);
            oxygenDepletedUraniumVolumeMultipler = ((16 * 2) / (16 * 2 + 232d)) * (depletedFuelDefinition.density / oxygenGasDefinition.density);

            var mainReactorFuel = part.Resources.Get(CurrentFuelMode.Variants.First().ReactorFuels.First().ResourceName);
            if (mainReactorFuel != null)
                reactorFuelMaxAmount = part.Resources.Get(CurrentFuelMode.Variants.First().ReactorFuels.First().ResourceName).maxAmount;

            foreach (ReactorFuelType fuelMode in fuelModes)
            {
                foreach (ReactorFuel fuel in fuelMode.Variants.First().ReactorFuels)
                {
                    var resource = part.Resources.Get(fuel.ResourceName);
                    if (resource == null)
                        // non-tweakable resources
                        part.Resources.Add(fuel.ResourceName, 0, 0, true, false, false, true, 0);
                }
            }

            Events[nameof(DumpActinides)].guiActive = canDumpActinides;
            Events[nameof(SwitchMode)].guiActiveEditor = Events[nameof(SwitchMode)].guiActive = Events[nameof(SwitchMode)].guiActiveUnfocused = CheckFuelModes() > 1;
            Events[nameof(SwapFuelMode)].guiActive = Events[nameof(SwapFuelMode)].guiActiveUnfocused = fuelModes.Count > 1;
            Events[nameof(EditorSwapFuel)].guiActiveEditor = fuelModes.Count > 1;
        }

        public override void OnFixedUpdate()
        {
            // if reactor is overloaded with actinides, stop functioning
            if (IsEnabled && part.Resources.Contains(ResourceSettings.Config.Actinides))
            {
                if (part.Resources[ResourceSettings.Config.Actinides].amount >= part.Resources[ResourceSettings.Config.Actinides].maxAmount)
                {
                    part.Resources[ResourceSettings.Config.Actinides].amount = part.Resources[ResourceSettings.Config.Actinides].maxAmount;
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
            if (part.Resources.Contains(ResourceSettings.Config.Actinides))
            {
                var actinides = part.Resources[ResourceSettings.Config.Actinides];
                var newActinidesAmount = Math.Max(actinides.amount - rate, 0);
                var actinidesChange = actinides.amount - newActinidesAmount;
                actinides.amount = newActinidesAmount;

                var depletedFuelsRequest = actinidesChange * 0.2;
                var depletedFuelsProduced = -Part.RequestResource(depletedFuelDefinition.id, -depletedFuelsRequest, ResourceFlowMode.STAGE_PRIORITY_FLOW);

                // first try to replace depletedFuel with enriched uranium
                var enrichedUraniumRequest = depletedFuelsProduced * enrichedUraniumVolumeMultiplier;
                var enrichedUraniumRetrieved = Part.RequestResource(enrichedUraniumDefinition.id, enrichedUraniumRequest, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                var receivedEnrichedUraniumFraction = enrichedUraniumRequest > 0 ? enrichedUraniumRetrieved / enrichedUraniumRequest : 0;

                // if missing fluorine is dumped
                var oxygenChange = -Part.RequestResource(oxygenGasDefinition.id, -depletedFuelsProduced * oxygenDepletedUraniumVolumeMultipler * receivedEnrichedUraniumFraction, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                var fluorineChange = -Part.RequestResource(fluorineGasDefinition.id, -depletedFuelsProduced * fluorineDepletedFuelVolumeMultiplier * (1 - receivedEnrichedUraniumFraction), ResourceFlowMode.STAGE_PRIORITY_FLOW);

                var reactorFuels = CurrentFuelMode.Variants.First().ReactorFuels;
                var sumUsagePerMw = reactorFuels.Sum(fuel => fuel.AmountFuelUsePerMJ * fuelUsePerMJMult);

                foreach (ReactorFuel fuel in reactorFuels)
                {
                    var fuelResource = part.Resources[fuel.ResourceName];
                    var powerFraction = sumUsagePerMw > 0.0 ? fuel.AmountFuelUsePerMJ * fuelUsePerMJMult / sumUsagePerMw : 1;
                    var newFuelAmount = Math.Min(fuelResource.amount + ((depletedFuelsProduced * 4) + (depletedFuelsProduced * receivedEnrichedUraniumFraction)) * powerFraction * depletedToEnrichVolumeMultplier, fuelResource.maxAmount);
                    fuelResource.amount = newFuelAmount;
                }

                return actinidesChange;
            }
            return 0;
        }

        // This Methods loads the correct fuel mode
        public override void SetDefaultFuelMode()
        {
            if (fuelModes == null)
            {
                Debug.Log("[KSPI]: MSRC SetDefaultFuelMode - load fuel modes");
                fuelModes = GetReactorFuelModes();
            }

            CurrentFuelMode = (fuel_mode < fuelModes.Count) ? fuelModes[fuel_mode] : fuelModes.FirstOrDefault();
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
                if (fuel_mode >= fuelModes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuelModes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName == startFirstFuelType.ResourceName);

            fuelModeStr = CurrentFuelMode.ModeGUIName;
        }

        private void DefuelCurrentFuel()
        {
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                var fuelReactor = part.Resources[fuel.ResourceName];
                var swapResourceList = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == fuel.ResourceName && r != fuelReactor)).ToList();

                swapResourceList.ForEach(res =>
                {
                    double spareCapacityForFuel = res.maxAmount - res.amount;
                    double fuelAdded = Math.Min(fuelReactor.amount, spareCapacityForFuel);
                    fuelReactor.amount -= fuelAdded;
                    res.amount += fuelAdded;
                });
            }
        }

        private bool IsCurrentFuelDepleted()
        {
            return CurrentFuelMode.Variants.First().ReactorFuels.Any(fuel => GetFuelAvailability(fuel) < 0.001);
        }


        // Returns the number of fuel-modes available for the currently selected fuel-type
        public int CheckFuelModes()
        {
            int modesAvailable = 0;
            var fuelType = CurrentFuelMode.Variants.First().ReactorFuels.First().FuelName;
            foreach (var reactorFuelType in fuelModes)
            {
                var currentMode = reactorFuelType.Variants.First().ReactorFuels.First().FuelName;
                if (currentMode == fuelType)
                {
                    modesAvailable++;
                }
            }
            return modesAvailable;
        }
    }
}
