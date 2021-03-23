using System;
using System.Linq;
using FNPlugin.Extensions;
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
    class InterstellarFissionMSRGC : InterstellarReactor, IFNNuclearFuelReprocessable
    {
        [KSPField] public double actinidesModifer = 1;
        [KSPField] public double temperatureThrottleExponent = 0.5;
        [KSPField] public double minimumTemperature = 0;
        [KSPField] public double poisonsProcessingSpeedMult = 0.05;
        [KSPField] public bool canDumpActinides = false;
        [KSPField] public double reactorMainFuelMaxAmount;

        private BaseEvent _manualRestartEvent;
        private PartResourceDefinition _actinideDefinition;
        private PartResourceDefinition _protactinium233Definition;
        private double reactorMainFuelDensityInTon;

        // Properties
        public override bool IsFuelNeutronRich => !CurrentFuelMode.Aneutronic;
        public override bool IsNuclear => true;

        public double WasteToReprocess => part.Resources.Contains(ResourceSettings.Config.Actinides) ? part.Resources[ResourceSettings.Config.Actinides].amount : 0;

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiName = "#LOC_KSPIE_FissionMSRGC_Dump_Actinides", guiActiveEditor = false, guiActive = true)]
        public void DumpActinides()
        {
            var actinides = part.Resources.Get(_actinideDefinition.id);
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

                double poisonCurAmount = 0;
                double poisonMaxAmount = 0;

                var actinidesResource = _actinideDefinition != null
                    ? part.Resources.Get(_actinideDefinition.id)
                    : part.Resources[ResourceSettings.Config.Actinides];

                if (actinidesResource != null && actinidesResource.amount.IsInfinityOrNaN())
                {
                    poisonCurAmount += actinidesResource.amount;
                    poisonMaxAmount += actinidesResource.maxAmount;
                }

                var protectResource = _protactinium233Definition != null
                    ? part.Resources.Get(_protactinium233Definition.id)
                    : part.Resources[ResourceSettings.Config.Protactinium233];

                if (protectResource != null && protectResource.amount.IsInfinityOrNaN())
                {
                    poisonCurAmount += protectResource.amount;
                    poisonMaxAmount += protectResource.maxAmount;
                }

                if (poisonMaxAmount <= 0)
                    return base.MaximumThermalPower;

                var fuelActinideMassRatio = 1 - poisonCurAmount / poisonMaxAmount;

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

        public override void OnStart(StartState state)
        {
            Debug.Log("[KSPI]: OnStart MSRGC " + part.name);

            _actinideDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.Actinides);
            _protactinium233Definition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.Protactinium233);

            // start as normal
            base.OnStart(state);

            fuelModeStr = CurrentFuelMode.ModeGUIName;

            _manualRestartEvent = Events[nameof(ManualRestart)];

            Events[nameof(DumpActinides)].guiActive = canDumpActinides;
            Events[nameof(SwitchMode)].guiActiveEditor = Events[nameof(SwitchMode)].guiActive = Events[nameof(SwitchMode)].guiActiveUnfocused = CheckFuelModes() > 1;
            Events[nameof(SwapFuelMode)].guiActive = Events[nameof(SwapFuelMode)].guiActiveUnfocused = fuelModes.Count > 1;
            Events[nameof(EditorSwapFuel)].guiActiveEditor = fuelModes.Count > 1;

            if (!CurrentFuelMode.Variants.Any(m => m.FuelRatio > 0))
                return;

            ReactorFuelMode currentVariant = CurrentFuelMode.Variants.First(m => m.FuelRatio > 0);

            var firstReactorFuel = currentVariant.ReactorFuels.First();

            var initialReactorFuel = part.Resources.Get(firstReactorFuel.ResourceName);
            if (initialReactorFuel != null)
            {
                if (reactorMainFuelMaxAmount == 0)
                    reactorMainFuelMaxAmount = initialReactorFuel.maxAmount;
                reactorMainFuelDensityInTon = firstReactorFuel.DensityInTon;
            }
            else
            {
                if (reactorMainFuelMaxAmount == 0)
                {
                    // assume the densest resource is nuclear fuel
                    PartResource densestFuel = part.Resources.OrderByDescending(m => m.info.density).FirstOrDefault();
                    if (densestFuel != null)
                        reactorMainFuelMaxAmount = densestFuel.maxAmount;
                    else
                        reactorMainFuelMaxAmount = part.mass * 100;
                }
            }

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

        public double ReprocessFuel(double rate, double deltaTime, double productionModifier, Part processor)
        {
            if (!hasStarted)
                OnStart(StartState.None);

            double poisonsAmount = 0;

            var actinides = part.Resources[ResourceSettings.Config.Actinides];
            if (actinides != null)
                poisonsAmount += actinides.amount;

            var protactinium233 = part.Resources[ResourceSettings.Config.Protactinium233];
            if (protactinium233 != null)
                poisonsAmount += protactinium233.amount;

            if (poisonsAmount <= 0)
                return 0;

            if (reactorMainFuelMaxAmount <= 0)
                reactorMainFuelMaxAmount = part.mass * 100;

            var poisonsProcessingSpeed = poisonsProcessingSpeedMult * productionModifier * deltaTime * (poisonsAmount / reactorMainFuelMaxAmount);
            var effectivePoisonsCapacity = Math.Min(rate, poisonsProcessingSpeed);

            double massPoisonDecrease = 0;
            if (actinides != null)
            {
                var actinidesCapacity = effectivePoisonsCapacity * (actinides.amount / poisonsAmount);
                var processorActinides = processor.Resources[ResourceSettings.Config.Actinides];
                if (processorActinides != null)
                {
                    var storedActinides = Math.Min(processorActinides.maxAmount - processorActinides.amount, actinidesCapacity);
                    processorActinides.amount = processorActinides.amount + storedActinides;

                    massPoisonDecrease += storedActinides * actinides.info.density;
                    actinides.amount = Math.Max(actinides.amount - storedActinides, 0);
                }
            }

            if (protactinium233 != null)
            {
                var protactinium233Capacity = effectivePoisonsCapacity * (protactinium233.amount / poisonsAmount);
                var processorProtactinium233 = processor.Resources[ResourceSettings.Config.Protactinium233];
                if (processorProtactinium233 != null)
                {
                    var storedProtactinium233 = Math.Min(processorProtactinium233.maxAmount - processorProtactinium233.amount, protactinium233Capacity);
                    processorProtactinium233.amount = processorProtactinium233.amount + storedProtactinium233;

                    massPoisonDecrease += storedProtactinium233 * protactinium233.info.density;
                    protactinium233.amount = Math.Max(protactinium233.amount - storedProtactinium233, 0);
                }
            }

            if (massPoisonDecrease == 0)
                return effectivePoisonsCapacity;

            var reactorFuels = CurrentFuelVariant?.ReactorFuels;
            if (reactorFuels == null)
                return effectivePoisonsCapacity;

            var reactorProducts = CurrentFuelVariant.ReactorProducts;

            var totalProductMassUsagePerMw = reactorProducts.Sum(fuel => fuel.TonsProductUsePerMj * fuelUsePerMJMult);
            var totalPoisonMassUsagePerMw = reactorProducts
                .Where(m => m.ResourceName == ResourceSettings.Config.Actinides || m.ResourceName == "Protactinium-233")
                .Sum(fuel => fuel.TonsProductUsePerMj * fuelUsePerMJMult);

            var poisonRatio = totalPoisonMassUsagePerMw / totalProductMassUsagePerMw;

            var totalProcessingMass = poisonRatio > 0 ? massPoisonDecrease / poisonRatio : 0;
            var totalFuelMassUsagePerMw = reactorFuels.Sum(fuel => fuel.TonsFuelUsePerMj * fuelUsePerMJMult);

            foreach (var reactorFuel in reactorFuels)
            {
                var processorFuelResource = processor.Resources[reactorFuel.ResourceName];
                if (processorFuelResource == null || processorFuelResource.amount <= 0)
                    continue;

                var reactorFuelResource = part.Resources[reactorFuel.ResourceName];
                if (reactorFuelResource == null)
                    continue;

                var availableReactorStorage = reactorFuelResource.maxAmount - reactorFuelResource.amount;
                if (availableReactorStorage <= 0)
                    continue;

                var powerFraction = totalFuelMassUsagePerMw > 0 ? reactorFuel.TonsFuelUsePerMj * fuelUsePerMJMult / totalFuelMassUsagePerMw : 1;
                var requiredFuelAmount = Math.Min(availableReactorStorage, totalProcessingMass * powerFraction / reactorFuelResource.info.density);

                var availableFuelAmount = Math.Min(requiredFuelAmount, processorFuelResource.amount);
                processorFuelResource.amount -= availableFuelAmount;

                reactorFuelResource.amount = reactorFuelResource.amount + availableFuelAmount;
            }

            return effectivePoisonsCapacity;
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
            var firstVariant = CurrentFuelMode.Variants.First();

            foreach (ReactorFuel fuel in firstVariant.ReactorFuels)
            {
                var resource = part.Resources.Get(fuel.ResourceName);
                if (resource == null) continue;

                resource.maxAmount = 0;

                if (HighLogic.LoadedSceneIsEditor)
                {
                    resource.amount = 0;
                    resource.isTweakable = false;
                }
            }
        }

        private void EnableResources()
        {
            if (reactorMainFuelMaxAmount <= 0)
                reactorMainFuelMaxAmount = part.mass * 100;

            // calculate total
            var firstVariant = CurrentFuelMode.Variants.First();

            var totalTonsFuelUsePerMj = firstVariant.ReactorFuels.Sum(m => m.TonsFuelUsePerMj);

            foreach (ReactorFuel fuel in firstVariant.ReactorFuels)
            {
                var resource = part.Resources.Get(fuel.ResourceName);
                if (resource == null) continue;

                var weightedAmount = reactorMainFuelMaxAmount * (fuel.TonsFuelUsePerMj / totalTonsFuelUsePerMj) * ( reactorMainFuelDensityInTon / fuel.DensityInTon);

                resource.maxAmount = weightedAmount;

                if (HighLogic.LoadedSceneIsEditor)
                {
                    resource.amount = weightedAmount;
                    resource.isTweakable = true;
                }
            }

            UpdatePartActionWindow();
        }

        private void UpdatePartActionWindow()
        {
            var window = FindObjectsOfType<UIPartActionWindow>().FirstOrDefault(w => w.part == part);
            if (window == null) return;

            foreach (UIPartActionWindow actionWindow in FindObjectsOfType<UIPartActionWindow>())
            {
                if (window.part != part) continue;
                actionWindow.ClearList();
                actionWindow.displayDirty = true;
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
                    double fuelAdded = Math.Min(fuelReactor.amount, res.maxAmount - res.amount);
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
            var fuelName = fuelMode.ReactorFuels.First().FuelName;
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
