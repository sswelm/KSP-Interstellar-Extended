using System;
using System.Linq;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
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
        [KSPField] public double actinidesModifer = 1;
        [KSPField] public double temperatureThrottleExponent = 0.5;
        [KSPField] public double minimumTemperature = 0;
        [KSPField] public bool canDumpActinides = false;

        private PartResourceDefinition fluorineGasDefinition;
        private PartResourceDefinition depletedFuelDefinition;
        private PartResourceDefinition enrichedUraniumDefinition;
        private PartResourceDefinition oxygenGasDefinition;

        private BaseEvent _manualRestartEvent;

        private double fluorineDepletedFuelVolumeMultiplier;
        private double enrichedUraniumVolumeMultiplier;
        private double depletedToEnrichVolumeMultiplier;
        private double oxygenDepletedUraniumVolumeMultiplier;
        private double reactorFuelMaxAmount;

        public override bool IsFuelNeutronRich => !CurrentFuelMode.Aneutronic;

        public override bool IsNuclear => true;

        public double WasteToReprocess => part.Resources.Contains(ResourceSettings.Config.Actinides) ? part.Resources[ResourceSettings.Config.Actinides].amount : 0;

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_Dump_Actinides", guiActiveEditor = false, guiActive = true)]
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

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_SwapFuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.5f)]//Swap Fuel
        public void SwapFuelMode()
        {
            if (!part.Resources.Contains(ResourceSettings.Config.Actinides) || part.Resources[ResourceSettings.Config.Actinides].amount > 0.01) return;
            DefaultCurrentFuel();
            if (!IsCurrentFuelDepleted()) return;

            DisableResources();
            SwitchFuelType();
            EnableResources();
            Refuel();
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_SwapFuel", guiActiveEditor = true, guiActive = false)]//Swap Fuel
        public void EditorSwapFuel()
        {
            if (fuelModes.Count == 1)
                return;

            DisableResources();
            SwitchFuelType();
            EnableResources();

            var modesAvailable = CheckFuelModes();
            // Hide Switch Mode button if there iss only one mode for the selected fuel type available
            Events[nameof(SwitchMode)].guiActiveEditor = Events[nameof(SwitchMode)].guiActive = Events[nameof(SwitchMode)].guiActiveUnfocused = modesAvailable > 1;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_SwitchMode", guiActiveEditor = true, guiActiveUnfocused = true, guiActive = true)]//Switch Mode
        public void SwitchMode()
        {
            var startFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();

            // repeat until found same or different fuel-mode with same kind of primary fuel
            ReactorFuel currentFirstFuelType;
            do
            {
                fuel_mode++;
                if (fuel_mode >= fuelModes.Count)
                    fuel_mode = 0;

                fuelmode_index = fuel_mode;
                CurrentFuelMode = fuelModes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName != startFirstFuelType.ResourceName);

            fuelModeStr = CurrentFuelMode.ModeGUIName;

            int modesAvailable = CheckFuelModes();
            // Hide Switch Mode button if there is only one mode for the selected fuel type available
            Events[nameof(SwitchMode)].guiActiveEditor = Events[nameof(SwitchMode)].guiActive = Events[nameof(SwitchMode)].guiActiveUnfocused = modesAvailable > 1;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_ManualRestart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Restart
        public void ManualRestart()
        {
            // verify any of the fuel types has at least 50% availability inside the reactor
            if (CurrentFuelMode.Variants.Any(variant => variant.ReactorFuels.All(fuel => GetLocalResourceRatio(fuel) > 0.5)))
                IsEnabled = true;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_ManualShutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Manual Shutdown
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_Refuel", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]//Refuel
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

                if (actinidesResource == null)
                    return base.MaximumThermalPower;

                var fuelActinideMassRatio = actinidesResource.maxAmount > 0 ?  1 - actinidesResource.amount / actinidesResource.maxAmount : 1;

                actinidesModifer = Math.Pow(fuelActinideMassRatio * fuelActinideMassRatio, CurrentFuelMode.ReactionRatePowerMultiplier);

                return base.MaximumThermalPower * actinidesModifer;
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
                if (CheatOptions.IgnoreMaxTemperature || !HighLogic.LoadedSceneIsFlight || isupgraded || !(powerPercent >= minThrottle * 100))
                    return actinidesModifer * base.CoreTemperature;

                var baseCoreTemperature = base.CoreTemperature;

                double tempScale;
                if (minimumTemperature > 0)
                    tempScale = minimumTemperature;
                else if (vessel != null && FNRadiator.HasRadiatorsForVessel(vessel))
                    tempScale = FNRadiator.GetAverageMaximumRadiatorTemperatureForVessel(vessel);
                else
                    tempScale = baseCoreTemperature / 2;

                var tempDiff = (baseCoreTemperature - tempScale) * Math.Pow(powerPercent / 100, temperatureThrottleExponent);
                return Math.Min(tempScale + tempDiff, actinidesModifer * baseCoreTemperature);
            }
        }

        public override double MaxCoreTemperature => actinidesModifer * base.CoreTemperature;

        public override void OnUpdate()
        {
            Events[nameof(ManualShutdown)].active = Events[nameof(ManualShutdown)].guiActiveUnfocused = IsEnabled;
            Events[nameof(Refuel)].active = Events[nameof(Refuel)].guiActiveUnfocused = !IsEnabled && !ongoingDecay;
            Events[nameof(Refuel)].guiName = "Refuel " + (CurrentFuelMode != null ? CurrentFuelMode.ModeGUIName : "");
            Events[nameof(SwapFuelMode)].active = Events[nameof(SwapFuelMode)].guiActiveUnfocused = fuelModes.Count > 1 && !IsEnabled && !ongoingDecay;
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

            fuelModeStr = CurrentFuelMode.ModeGUIName;

            _manualRestartEvent = Events[nameof(ManualRestart)];

            oxygenGasDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.OxygenGas);
            fluorineGasDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.FluorineGas);
            depletedFuelDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.DepletedFuel);
            enrichedUraniumDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.EnrichedUranium);

            depletedToEnrichVolumeMultiplier = enrichedUraniumDefinition.density / depletedFuelDefinition.density;
            fluorineDepletedFuelVolumeMultiplier = ((19 * 4) / 232d) * (depletedFuelDefinition.density / fluorineGasDefinition.density);
            enrichedUraniumVolumeMultiplier = (232d / (16 * 2 + 232d)) * (depletedFuelDefinition.density / enrichedUraniumDefinition.density);
            oxygenDepletedUraniumVolumeMultiplier = ((16 * 2) / (16 * 2 + 232d)) * (depletedFuelDefinition.density / oxygenGasDefinition.density);

            Events[nameof(DumpActinides)].guiActive = canDumpActinides;
            Events[nameof(SwitchMode)].guiActiveEditor = Events[nameof(SwitchMode)].guiActive = Events[nameof(SwitchMode)].guiActiveUnfocused = CheckFuelModes() > 1;
            Events[nameof(SwapFuelMode)].guiActive = Events[nameof(SwapFuelMode)].guiActiveUnfocused = fuelModes.Count > 1;
            Events[nameof(EditorSwapFuel)].guiActiveEditor = fuelModes.Count > 1;

            if (!CurrentFuelMode.Variants.Any(m => m.FuelRatio > 0))
                return;

            ReactorFuelMode currentVariant = CurrentFuelMode.Variants.First(m => m.FuelRatio > 0);
            var initialReactorFuel = part.Resources.Get(currentVariant.ReactorFuels.First().ResourceName);
            if (initialReactorFuel != null)
                reactorFuelMaxAmount = part.Resources.Get(initialReactorFuel.resourceName).maxAmount;

            foreach (var fuelMode in fuelModes)
            {
                foreach (var reactorFuel in fuelMode.Variants.First().ReactorFuels)
                {
                    var resource = part.Resources.Get(reactorFuel.ResourceName);
                    if (resource == null)
                        // non-tweakable resources
                        part.Resources.Add(reactorFuel.ResourceName, 0, 0, true, false, false, true, 0);
                }
            }
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
            if (!part.Resources.Contains(ResourceSettings.Config.Actinides)) return 0;

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
            Part.RequestResource(oxygenGasDefinition.id, -depletedFuelsProduced * oxygenDepletedUraniumVolumeMultiplier * receivedEnrichedUraniumFraction, ResourceFlowMode.STAGE_PRIORITY_FLOW);
            Part.RequestResource(fluorineGasDefinition.id, -depletedFuelsProduced * fluorineDepletedFuelVolumeMultiplier * (1 - receivedEnrichedUraniumFraction), ResourceFlowMode.STAGE_PRIORITY_FLOW);

            var reactorFuels = CurrentFuelMode.Variants.First().ReactorFuels;
            var sumUsagePerMw = reactorFuels.Sum(fuel => fuel.AmountFuelUsePerMj * fuelUsePerMJMult);

            foreach (ReactorFuel fuel in reactorFuels)
            {
                var fuelResource = part.Resources[fuel.ResourceName];
                var powerFraction = sumUsagePerMw > 0.0 ? fuel.AmountFuelUsePerMj * fuelUsePerMJMult / sumUsagePerMw : 1;
                var newFuelAmount = Math.Min(fuelResource.amount + ((depletedFuelsProduced * 4) + (depletedFuelsProduced * receivedEnrichedUraniumFraction)) * powerFraction * depletedToEnrichVolumeMultiplier, fuelResource.maxAmount);
                fuelResource.amount = newFuelAmount;
            }

            return actinidesChange;
        }

        // This Methods loads the correct fuel mode
        public override void SetDefaultFuelMode()
        {
            if (fuelModes == null)
            {
                Debug.Log("[KSPI]: MSRC SetDefaultFuelMode - load fuel modes");
                fuelModes = GetReactorFuelModes();
            }

            CurrentFuelMode = fuel_mode < fuelModes.Count ? fuelModes[fuel_mode] : fuelModes.FirstOrDefault();
        }

        private void DisableResources()
        {
            bool editor = HighLogic.LoadedSceneIsEditor;
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                var resource = part.Resources.Get(fuel.ResourceName);
                if (resource == null) continue;

                if (editor)
                {
                    resource.amount = 0;
                    resource.isTweakable = false;
                }
                resource.maxAmount = 0;
            }
        }

        private void EnableResources()
        {
            bool editor = HighLogic.LoadedSceneIsEditor;
            foreach (ReactorFuel fuel in CurrentFuelMode.Variants.First().ReactorFuels)
            {
                var resource = part.Resources.Get(fuel.ResourceName);
                if (resource == null) continue;

                if (editor)
                {
                    resource.amount = reactorFuelMaxAmount;
                    resource.isTweakable = true;
                }
                resource.maxAmount = reactorFuelMaxAmount;
            }
        }

        private void SwitchFuelType()
        {
            var startFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            ReactorFuel currentFirstFuelType;
            var initialFuelMode = fuel_mode;
            do
            {
                fuel_mode++;
                if (fuel_mode >= fuelModes.Count)
                    fuel_mode = 0;

                CurrentFuelMode = fuelModes[fuel_mode];
                currentFirstFuelType = CurrentFuelMode.Variants.First().ReactorFuels.First();
            }
            while (currentFirstFuelType.ResourceName == startFirstFuelType.ResourceName && initialFuelMode != fuel_mode);

            fuelModeStr = CurrentFuelMode.ModeGUIName;
        }

        private void DefaultCurrentFuel()
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
            var modesAvailable = 0;
            var fuelMode = CurrentFuelMode.Variants.First();
            string fuelName = fuelMode.ReactorFuels.First().FuelName;
            foreach (var reactorFuelType in fuelModes)
            {
                var currentFuelMode = reactorFuelType.Variants.First();
                var reactorFuel = currentFuelMode.ReactorFuels.First();
                if (reactorFuel.FuelName == fuelName)
                    modesAvailable++;
            }

            return modesAvailable;
        }

        public override void Update()
        {
            base.Update();

            if (_manualRestartEvent != null)
                _manualRestartEvent.externalToEVAOnly = !CheatOptions.NonStrictAttachmentOrientation;
        }
    }
}
