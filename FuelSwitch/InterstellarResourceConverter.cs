using System;
using System.Collections.Generic;
using System.Linq;

namespace InterstellarFuelSwitch
{
    class ResourceStats
    {
        public PartResourceDefinition definition;
        public double maxAmount;
        public double currentAmount;
        public double amountRatio;
        public double retrieveAmount;
        public double transferRate = 1;
        public double normalizedDensity;
        public double conversionRatio;
    }

    class InterstellarEquilibrium : InterstellarResourceConverter  { }

    class InterstellarResourceConverter : PartModule
    {
        public const string GROUP = "InterstellarResourceConverter";
        public const string GROUP_TITLE = "";

        // configs
        [KSPField] public bool showPowerUsageFloatRange = false;
        [KSPField] public bool showControlToggle = false;
        [KSPField] public string sliderText = string.Empty;
        [KSPField] public float percentageMaxValue = 100;
        [KSPField] public float percentageMinValue = -100;
        [KSPField] public float percentageStepIncrement = 1;
        [KSPField] public bool percentageSymetry = true;
        [KSPField] public string primaryResourceNames = string.Empty;
        [KSPField] public string secondaryResourceNames = string.Empty;
        [KSPField] public double primaryConversionEnergyCost = 0.001;
        [KSPField] public double secondaryConversionEnergyCost = 0.001;
        [KSPField] public string primaryConversionEnergyResource = "ElectricCharge";
        [KSPField] public string secondaryConversionEnergResource = "ElectricCharge";
        [KSPField] public float primaryConversionEnergyMult = 1;
        [KSPField] public float secondaryConversionEnergMult = 1; [KSPField(guiActive = false, guiUnits = " U/s")] public float primaryChange;
        [KSPField(guiUnits = " U/s")] public float secondaryChange;

        [KSPField] public double primaryconversionRatio;
        [KSPField] public double secondaryconversionRatio;

        [KSPField] public double neededAmount;
        [KSPField] public double availableSpaceInTarget;
        [KSPField] bool retreivePrimary;
        [KSPField] bool retrieveSecondary;
        [KSPField] public double transferRate = 0;
        [KSPField] public double conversionRatio = 0;

        [KSPField] public double maxPowerPrimary = 10;
        [KSPField] public double maxPowerSecondary = 10;
        [KSPField] public bool requiresPrimaryLocalInEditor = true;
        [KSPField] public bool requiresPrimaryLocalInFlight = true;
        [KSPField] public bool primaryConversionCostPower = true;
        [KSPField] public bool secondaryConversionCostPower = true;

        [KSPField] public double primaryNormalizedDensity = 0.001;
        [KSPField] public double secondaryNormalizedDensity = 0.001;
        [KSPField] public double requestedPower;

        // persistent control
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_ResourceConverter_ConvertPercentage", guiUnits = "%"), UI_FloatRange]
        public float convertPercentage = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_ResourceConverter_PowerPercentage", guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1)]
        public float powerUsagePercentage = 0;

        PartResourceDefinition definitionPrimaryPowerResource;
        PartResourceDefinition definitionSecondaryPowerResource;

        BaseField primaryChangeField;
        BaseField secondaryChangeField;
        BaseField convertPercentageField;
        List<ResourceStats> primaryResources;
        List<ResourceStats> secondaryResources;
        UI_FloatRange convertPercentageEditorFloatRange;
        UI_FloatRange convertPercentageFlightFloatRange;

        int changedFieldCounter;
        bool hasNullDefinitions;

        public float PowerUsagePercentageRatio => 1 - (showPowerUsageFloatRange  ? powerUsagePercentage / 100 : 1);

        public override void OnStart(StartState state)
        {
            definitionPrimaryPowerResource = PartResourceLibrary.Instance.GetDefinition(primaryConversionEnergyResource);
            definitionSecondaryPowerResource = PartResourceLibrary.Instance.GetDefinition(secondaryConversionEnergResource);

            primaryChangeField = Fields[nameof(primaryChange)];
            secondaryChangeField = Fields[nameof(secondaryChange)];

            Fields[nameof(powerUsagePercentage)].guiActiveEditor = showPowerUsageFloatRange;
            Fields[nameof(powerUsagePercentage)].guiActive = showPowerUsageFloatRange;

            convertPercentageField = Fields[nameof(convertPercentage)];

            primaryResources = primaryResourceNames.Split(';').Select(m => new ResourceStats() { definition = PartResourceLibrary.Instance.GetDefinition(m.Trim()) } ).ToList();
            secondaryResources = secondaryResourceNames.Split(';').Select(m => new ResourceStats() { definition = PartResourceLibrary.Instance.GetDefinition(m.Trim()) }).ToList();

            primaryChangeField.guiName = primaryResources.First().definition.name;
            secondaryChangeField.guiName = secondaryResources.First().definition.name;

            hasNullDefinitions = primaryResources.Any(m => m.definition == null) || secondaryResources.Any(m => m.definition == null);
            if (hasNullDefinitions)
            {
                convertPercentageField.guiActiveEditor = false;
                convertPercentageField.guiActive = false;
                return;
            }

            foreach (var resource in primaryResources)
            {
                if (resource.definition.density > 0)
                    resource.normalizedDensity = resource.definition.density;
            }

            foreach (var resource in secondaryResources)
            {
                if (resource.definition.density > 0)
                    resource.normalizedDensity = resource.definition.density;
            }

            if (primaryResources.Count == 1 && secondaryResources.Count == 1)
            {
                var primary = primaryResources.First();
                var secondary = secondaryResources.First();

                if (primary.normalizedDensity > 0 && secondary.normalizedDensity > 0)
                {
                    primary.conversionRatio = secondary.normalizedDensity / primary.normalizedDensity;
                    secondary.conversionRatio = primary.normalizedDensity / secondary.normalizedDensity;
                }
                else if (primary.definition.unitCost > 0 && secondary.definition.unitCost > 0)
                {
                    primary.conversionRatio = (double)(decimal)secondary.definition.unitCost / (double)(decimal)primary.definition.unitCost;
                    secondary.conversionRatio = (double)(decimal)primary.definition.unitCost / (double)(decimal)secondary.definition.unitCost;
                }

                if (primary.normalizedDensity == 0)
                    primary.normalizedDensity = primaryNormalizedDensity;
                if (secondary.normalizedDensity == 0)
                    secondary.normalizedDensity = secondaryNormalizedDensity;

                if (primary.conversionRatio == 0 && secondary.conversionRatio == 0)
                {
                    if (primary.normalizedDensity > 0 && secondary.normalizedDensity > 0)
                    {
                        primary.conversionRatio = secondary.normalizedDensity / primary.normalizedDensity;
                        secondary.conversionRatio = primary.normalizedDensity / secondary.normalizedDensity;
                    }
                    else
                    {
                        primary.conversionRatio = 1;
                        secondary.conversionRatio = 1;
                    }
                }
            }

            primaryconversionRatio = primaryResources.First().conversionRatio;
            secondaryconversionRatio = secondaryResources.First().conversionRatio;

            primaryResources.ForEach(m => m.transferRate = maxPowerPrimary / primaryConversionEnergyCost / 1000 / m.normalizedDensity);
            secondaryResources.ForEach(m => m.transferRate = maxPowerSecondary / secondaryConversionEnergyCost / 1000 / m.normalizedDensity);

            // if slider text is missing, generate it
            if (string.IsNullOrEmpty(sliderText))
                convertPercentageField.guiName = String.Join("+", primaryResources.Select(m => m.definition.name).ToArray()) + "<->" + String.Join("+", secondaryResources.Select(m => m.definition.name).ToArray());
            else
                convertPercentageField.guiName = sliderText;

            convertPercentageFlightFloatRange = convertPercentageField.uiControlFlight as UI_FloatRange;
            if (convertPercentageFlightFloatRange != null)
            {
                convertPercentageFlightFloatRange.maxValue = percentageMaxValue;
                convertPercentageFlightFloatRange.minValue = percentageMinValue;
                convertPercentageFlightFloatRange.stepIncrement = percentageStepIncrement;
                convertPercentageFlightFloatRange.affectSymCounterparts = percentageSymetry ? UI_Scene.All : UI_Scene.None;
            }

            convertPercentageEditorFloatRange = convertPercentageField.uiControlEditor as UI_FloatRange;
            if (convertPercentageEditorFloatRange != null)
            {
                convertPercentageEditorFloatRange.maxValue = percentageMaxValue;
                convertPercentageEditorFloatRange.minValue = percentageMinValue;
                convertPercentageEditorFloatRange.stepIncrement = percentageStepIncrement;
                convertPercentageEditorFloatRange.affectSymCounterparts = percentageSymetry ? UI_Scene.All : UI_Scene.None;
            }
        }

        public void Update()
        {
            // exit if definition was not found
            if (hasNullDefinitions)
            {
                convertPercentageField.guiActive = false;
                primaryChangeField.guiActive = false;
                secondaryChangeField.guiActive = false;
                return;
            }

            // in edit mode only show when primary resources are present
            if (requiresPrimaryLocalInEditor && HighLogic.LoadedSceneIsEditor)
            {
                convertPercentageField.guiActiveEditor = primaryResources.All(m => part.Resources.Contains( m.definition.id));
                return;
            }

            // in flight mode, hide control if primary resources are not present
            if (requiresPrimaryLocalInFlight && HighLogic.LoadedSceneIsFlight && !primaryResources.All(m => part.Resources.Contains(m.definition.id)))
            {
                 // hide interface and exit
                 convertPercentageField.guiActive = false;
                 primaryChangeField.guiActive = false;
                 secondaryChangeField.guiActive = false;
                 return;
            }

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            foreach (var resource in primaryResources)
            {
                part.GetConnectedResourceTotals(resource.definition.id, out var currentAmount, out var maxAmount);

                if (maxAmount == 0)
                {
                    convertPercentageField.guiActive = false;
                    primaryChangeField.guiActive = false;
                    secondaryChangeField.guiActive = false;
                    return;
                }

                resource.currentAmount = currentAmount;
                resource.maxAmount = maxAmount;
            }

            foreach (var resource in secondaryResources)
            {
                part.GetConnectedResourceTotals(resource.definition.id, out var currentAmount, out var maxAmount);

                if (maxAmount == 0)
                {
                    convertPercentageField.guiActive = false;
                    primaryChangeField.guiActive = false;
                    secondaryChangeField.guiActive = false;
                    return;
                }

                resource.currentAmount = currentAmount;
                resource.maxAmount = maxAmount;
            }

            convertPercentageField.guiActive = true;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            var fixedDeltaTime = (double)(decimal)TimeWarp.fixedDeltaTime;

            // only process if we have some meaningfull resource
            if (!convertPercentageField.guiActive)
                return;

            changedFieldCounter++;

            foreach (var resource in primaryResources)
            {
                part.GetConnectedResourceTotals(resource.definition.id, out var currentAmount, out var maxAmount);

                if (maxAmount == 0)
                    return;

                resource.currentAmount = currentAmount;
                resource.maxAmount = maxAmount;
            }

            foreach (var resource in secondaryResources)
            {
                part.GetConnectedResourceTotals(resource.definition.id, out var currentAmount, out var maxAmount);

                if (maxAmount == 0)
                    return;

                resource.currentAmount = currentAmount;
                resource.maxAmount = maxAmount;
            }

            primaryResources.ForEach(m => m.amountRatio = m.currentAmount / m.maxAmount);
            secondaryResources.ForEach(m => m.amountRatio = m.currentAmount / m.maxAmount);

            var percentageRatio = Math.Abs(convertPercentage) / 100d;

            retreivePrimary = false;
            retrieveSecondary = false;

            if (convertPercentage > 0)
            {
                if (secondaryResources.Any(m => percentageRatio > m.amountRatio))
                {
                    retreivePrimary = true;
                    neededAmount = secondaryResources.Min(m => Math.Max(percentageRatio - m.amountRatio, 0) * m.maxAmount / m.conversionRatio);
                    primaryResources.ForEach(m => m.retrieveAmount = neededAmount);
                }
                else if (percentageMinValue < 0)
                {
                    retrieveSecondary = true;
                    availableSpaceInTarget = primaryResources.Min(m => (m.maxAmount - m.currentAmount) / m.conversionRatio);
                    secondaryResources.ForEach(m => m.retrieveAmount = Math.Min((Math.Max(m.amountRatio - percentageRatio, 0)) * m.maxAmount, availableSpaceInTarget));
                }
            }
            else if (convertPercentage < 0)
            {
                if (primaryResources.Any(m => percentageRatio > m.amountRatio))
                {
                    retrieveSecondary = true;
                    neededAmount = primaryResources.Min(m => Math.Max(percentageRatio - m.amountRatio, 0) * m.maxAmount / m.conversionRatio);
                    secondaryResources.ForEach(m => m.retrieveAmount = neededAmount);
                }
                else if (percentageMaxValue > 0)
                {
                    retreivePrimary = true;
                    availableSpaceInTarget = secondaryResources.Min(m => (m.maxAmount - m.currentAmount) / m.conversionRatio);
                    primaryResources.ForEach(m => m.retrieveAmount = Math.Min((Math.Max(m.amountRatio - percentageRatio, 0)) * m.maxAmount, availableSpaceInTarget));
                }
            }

            transferRate = 0;
            conversionRatio = 0;

            if (retreivePrimary && primaryResources.Any(r => r.retrieveAmount > 0))
            {
                foreach (var primaryResource in primaryResources)
                {
                    transferRate = primaryResource.transferRate;
                    var fixedTransferRate = transferRate * fixedDeltaTime;

                    if (fixedTransferRate == 0)
                        continue;

                    double powerReceivedRatio = 1;
                    if (primaryConversionCostPower)
                    {
                        part.GetConnectedResourceTotals(definitionPrimaryPowerResource.id, out var powerCurrentAmount, out var powerMaxAmount);

                        var powerStorageRatio = powerMaxAmount > 0 ? powerCurrentAmount / powerMaxAmount : 0;
                        var availablePower = powerMaxAmount * Math.Max(0, powerStorageRatio - PowerUsagePercentageRatio);

                        var transferRatio = primaryResource.retrieveAmount >= fixedTransferRate ? 1 : primaryResource.retrieveAmount / fixedTransferRate;
                        var maximumPower = transferRatio * maxPowerPrimary * fixedDeltaTime * primaryConversionEnergyMult;

                        requestedPower = Math.Min(availablePower, maximumPower);
                        var receivedPower = part.RequestResource(definitionPrimaryPowerResource.id, requestedPower);
                        powerReceivedRatio = requestedPower > 0 ? receivedPower / requestedPower : 0;
                    }

                    var fixedRequest = Math.Min(fixedTransferRate, primaryResource.retrieveAmount);

                    var fixedPrimaryRequest = fixedRequest * powerReceivedRatio;

                    var receivedSourceAmountFixed = part.RequestResource(primaryResource.definition.id, fixedPrimaryRequest);

                    primaryChange = -(float)(receivedSourceAmountFixed / fixedDeltaTime);

                    if (primaryChange != 0)
                        changedFieldCounter = 0;

                    primaryChangeField.guiActive = changedFieldCounter < 50;

                    double createdAmount = 0;

                    foreach (var secondary in secondaryResources)
                    {
                        conversionRatio = secondary.conversionRatio;
                        var requestedTargetAmount = -receivedSourceAmountFixed * conversionRatio;

                        var secondaryRequestResult = part.RequestResource(secondary.definition.id, requestedTargetAmount);

                        secondaryChange = -(float)(secondaryRequestResult / fixedDeltaTime);
                        secondaryChangeField.guiActive = secondaryChange != 0;

                        var receivedTargetAmount = secondaryRequestResult / conversionRatio;

                        createdAmount += receivedTargetAmount;
                    }

                    var returned = part.RequestResource(primaryResource.definition.id, createdAmount + receivedSourceAmountFixed);
                    primaryResource.retrieveAmount = primaryResource.retrieveAmount - receivedSourceAmountFixed - returned;
                }
            }
            else if (retrieveSecondary && secondaryResources.Any(r => r.retrieveAmount > 0))
            {
                foreach (var secondaryResource in secondaryResources)
                {
                    transferRate = secondaryResource.transferRate;

                    var fixedTransferRate = transferRate * fixedDeltaTime;

                    if (fixedTransferRate == 0)
                        continue;

                    double powerReceiverRatio = 1;
                    if (secondaryConversionCostPower)
                    {
                        part.GetConnectedResourceTotals(definitionSecondaryPowerResource.id, out var powerCurrentAmount, out var powerMaxAmount);

                        var powerStorageRatio = powerMaxAmount > 0 ? powerCurrentAmount / powerMaxAmount : 0;
                        var availablePower = powerMaxAmount * Math.Max(0, powerStorageRatio - PowerUsagePercentageRatio);

                        var transferRatio = secondaryResource.retrieveAmount >= fixedTransferRate ? 1 : secondaryResource.retrieveAmount / fixedTransferRate;
                        var maximumPower = transferRatio * maxPowerPrimary * fixedDeltaTime * secondaryConversionEnergMult;

                        requestedPower = Math.Min(availablePower, maximumPower);
                        var receivedPower = part.RequestResource(definitionSecondaryPowerResource.id, requestedPower);
                        powerReceiverRatio = requestedPower > 0 ? receivedPower / requestedPower : 0;
                    }

                    var fixedRequest = Math.Min(fixedTransferRate, secondaryResource.retrieveAmount);
                    var receivedSourceAmountFixed = part.RequestResource(secondaryResource.definition.id, fixedRequest * powerReceiverRatio);

                    secondaryChange = -(float)(receivedSourceAmountFixed / fixedDeltaTime);
                    secondaryChangeField.guiActive = secondaryChange != 0;

                    double createdAmount = 0;

                    foreach (var primary in primaryResources)
                    {
                        conversionRatio = primary.conversionRatio;
                        var requestedTargetAmount = -receivedSourceAmountFixed * conversionRatio;

                        var primaryRequestResult = part.RequestResource(primary.definition.id, requestedTargetAmount);

                        primaryChange = -(float)(primaryRequestResult / fixedDeltaTime);

                        if (primaryChange != 0)
                            changedFieldCounter = 0;

                        primaryChangeField.guiActive = changedFieldCounter < 50;

                        var receivedTargetAmount = primaryRequestResult / conversionRatio;

                        createdAmount += receivedTargetAmount;
                    }

                    var returned = part.RequestResource(secondaryResource.definition.id, createdAmount + receivedSourceAmountFixed);
                    secondaryResource.retrieveAmount = secondaryResource.retrieveAmount - receivedSourceAmountFixed - returned;
                }
            }
            else
            {
                secondaryChangeField.guiActive = changedFieldCounter < 50;
                primaryChangeField.guiActive = changedFieldCounter < 50;
            }
        }

        public override string GetInfo()
        {
            return "Primary: " + primaryResourceNames + "\n" + "Secondary: " + secondaryResourceNames ;
        }

        /// <summary>
        /// Call on on offline vessel to process Resource Converter
        /// </summary>
        /// <param name="vessel"></param>
        public static void UpdateResourceConverterOffline(Vessel vessel)
        {
            foreach (var protoPart in vessel.protoVessel.protoPartSnapshots)
            {
                var interstellarResourceConverter = protoPart.modules.FirstOrDefault(m => m.moduleName == "InterstellarResourceConverter");

                if (interstellarResourceConverter == null) continue;

                string primaryConversionEnergyResource = interstellarResourceConverter.moduleValues.GetValue("primaryConversionEnergyResource");
                string secondaryConversionEnergyResource = interstellarResourceConverter.moduleValues.GetValue("secondaryConversionEnergResource");

                int matchCount = 0;

                foreach (var protoResource in protoPart.resources)
                {
                    if (protoResource.resourceName == primaryConversionEnergyResource)
                    {
                        protoResource.amount = protoResource.maxAmount;
                        matchCount++;
                    }
                    else if (protoResource.resourceName == secondaryConversionEnergyResource)
                    {
                        protoResource.amount = protoResource.maxAmount;
                        matchCount++;
                    }

                    if (matchCount == 2)
                        break;
                }
            }
        }
    }
}
