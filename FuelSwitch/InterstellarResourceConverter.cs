using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    class ResourceStats
    {
        public PartResourceDefinition definition;
        public double maxAmount = 0;
        public double currentAmount = 0;
        public double amountRatio = 0;
        public double retrieveAmount = 0;
        public double transferRate = 1;
        public double normalizedDensity = 0;
        public double conversionRatio = 0;
    }

    class InterstellarEquilibrium : InterstellarResourceConverter  { }

    class InterstellarResourceConverter : PartModule
    {
        // persistant control
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_ResourceConverter_ConvertPercentage", guiUnits = "%"), UI_FloatRange()]
        public float convertPercentage = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_ResourceConverter_PowerPercentage", guiUnits = "%"), UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1)]
        public float powerUsagePercentage = 0;

        // configs
        [KSPField]
        public bool showPowerUsageFloatRange = false;
        [KSPField]
        public bool showControlToggle = false;
        [KSPField]
        public string sliderText = string.Empty;
        [KSPField]
        public float percentageMaxValue = 100;
        [KSPField]
        public float percentageMinValue = -100;
        [KSPField]
        public float percentageStepIncrement = 1;
        [KSPField]
        public bool percentageSymetry = true;
        [KSPField]
        public string primaryResourceNames = string.Empty;
        [KSPField]
        public string secondaryResourceNames = string.Empty;
        [KSPField]
        public double primaryConversionEnergyCost = 0.001;
        [KSPField]
        public double secondaryConversionEnergyCost = 0.001;
        [KSPField]
        public string primaryConversionEnergyResource = "ElectricCharge";
        [KSPField]
        public string secondaryConversionEnergResource = "ElectricCharge";
        [KSPField]
        public float primaryConversionEnergyMult = 1;
        [KSPField]
        public float secondaryConversionEnergMult = 1;

        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double primaryconversionRatio;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double secondaryconversionRatio;

        [KSPField(guiActive = false)]
        public double neededAmount;
        [KSPField(guiActive = false)]
        public double availableSpaceInTarget;
        [KSPField(guiActive = false)]
        bool retreivePrimary;
        [KSPField(guiActive = false)]
        bool retrieveSecondary;
        [KSPField(guiActive = false)]
        public double transferRate = 0;
        [KSPField(guiActive = false)]
        public double conversionRatio = 0;

        [KSPField]
        public double maxPowerPrimary = 10;
        [KSPField]
        public double maxPowerSecondary = 10;
        [KSPField]
        public bool requiresPrimaryLocalInEditor = true;
        [KSPField]
        public bool requiresPrimaryLocalInFlight = true;

        [KSPField]
        public bool primaryConversionCostPower = true;
        [KSPField]
        public bool secondaryConversionCostPower = true;


        [KSPField]
        public double primaryNormalizedDensity = 0.001;
        [KSPField]
        public double secondaryNormalizedDensity = 0.001;
        [KSPField]
        public double requestedPower;

        PartResourceDefinition definitionPrimaryPowerResource;
        PartResourceDefinition definitionSecondaryPowerResource;

        BaseField convertPercentageField;
        List<ResourceStats> primaryResources;
        List<ResourceStats> secondaryResources;
        UI_FloatRange convertPecentageEditorFloatRange;
        UI_FloatRange convertPecentageFlightFloatRange;

        bool hasNullDefinitions = false;

        public float PowerUsagePercentageRatio
        {
            get { return 1 - (showPowerUsageFloatRange  ? powerUsagePercentage / 100 : 1); }
        }

        public override void OnStart(PartModule.StartState state)
        {
            definitionPrimaryPowerResource = PartResourceLibrary.Instance.GetDefinition(primaryConversionEnergyResource);
            definitionSecondaryPowerResource = PartResourceLibrary.Instance.GetDefinition(secondaryConversionEnergResource);

            Fields["powerUsagePercentage"].guiActiveEditor = showPowerUsageFloatRange;
            Fields["powerUsagePercentage"].guiActive = showPowerUsageFloatRange;

            convertPercentageField = Fields["convertPercentage"];

            primaryResources = primaryResourceNames.Split(';').Select(m => new ResourceStats() { definition = PartResourceLibrary.Instance.GetDefinition(m.Trim()) } ).ToList();
            secondaryResources = secondaryResourceNames.Split(';').Select(m => new ResourceStats() { definition = PartResourceLibrary.Instance.GetDefinition(m.Trim()) }).ToList();

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
                {
                    resource.normalizedDensity = resource.definition.density;
                }
                else if (resource.definition.density == 0)
                {
                    Debug.LogWarning("[IFS] - " + resource + " have ZERO density");
                }
                else
                {
                    Debug.LogError("[IFS] - " + resource + " have " + resource.definition.density + " density");
                }
            }

            foreach (var resource in secondaryResources)
            {
                if (resource.definition.density > 0)
                {
                    resource.normalizedDensity = resource.definition.density;
                }
                else if (resource.definition.density == 0)
                {
                    Debug.LogWarning("[IFS] - " + resource + " have ZERO density");
                }
                else
                {
                    Debug.LogError("[IFS] - " + resource + " have " + resource.definition.density + " density");
                }
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
                    primary.conversionRatio = secondary.definition.unitCost / primary.definition.unitCost;
                    secondary.conversionRatio = primary.definition.unitCost / secondary.definition.unitCost;
                }

                if (primary.normalizedDensity == 0)
                    primary.normalizedDensity = primaryNormalizedDensity;
                if (secondary.normalizedDensity == 0)
                    secondary.normalizedDensity = secondaryNormalizedDensity;

                if (secondary.conversionRatio == 0 && secondary.conversionRatio == 0)
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

            convertPecentageFlightFloatRange = convertPercentageField.uiControlFlight as UI_FloatRange;
            convertPecentageFlightFloatRange.maxValue = percentageMaxValue;
            convertPecentageFlightFloatRange.minValue = percentageMinValue;
            convertPecentageFlightFloatRange.stepIncrement = percentageStepIncrement;
            convertPecentageFlightFloatRange.affectSymCounterparts = percentageSymetry ? UI_Scene.All : UI_Scene.None;

            convertPecentageEditorFloatRange = convertPercentageField.uiControlEditor as UI_FloatRange;
            convertPecentageEditorFloatRange.maxValue = percentageMaxValue;
            convertPecentageEditorFloatRange.minValue = percentageMinValue;
            convertPecentageEditorFloatRange.stepIncrement = percentageStepIncrement;
            convertPecentageEditorFloatRange.affectSymCounterparts = percentageSymetry ? UI_Scene.All : UI_Scene.None;
        }

        public void Update()
        {
            // exit if definition was not found
            if (hasNullDefinitions)
            {
                convertPercentageField.guiActive = false;
                return;
            }

            // in edit mode only show when primary resources are present
            if (requiresPrimaryLocalInEditor && HighLogic.LoadedSceneIsEditor)
            {
                convertPercentageField.guiActiveEditor = primaryResources.All(m => part.Resources.Contains( m.definition.id));
                return;
            }

            retreivePrimary = false;
            retrieveSecondary = false;

            // in flight mode, hide control if primary resources are not present 
            if (requiresPrimaryLocalInFlight && HighLogic.LoadedSceneIsFlight && !primaryResources.All(m => part.Resources.Contains(m.definition.id)))
            {
                 // hide interface and exit
                 convertPercentageField.guiActive = false;
                 return;
            }

            if (HighLogic.LoadedSceneIsEditor)
                return;

            foreach (var resource in primaryResources)
            {
                double currentAmount;
                double maxAmount;

                part.GetConnectedResourceTotals(resource.definition.id, out currentAmount, out maxAmount);

                if (maxAmount == 0)
                {
                    convertPercentageField.guiActive = false;
                    return;
                }

                resource.currentAmount = currentAmount;
                resource.maxAmount = maxAmount;
            }

            foreach (var resource in secondaryResources)
            {
                double currentAmount;
                double maxAmount;
                part.GetConnectedResourceTotals(resource.definition.id, out currentAmount, out maxAmount);

                if (maxAmount == 0)
                {
                    convertPercentageField.guiActive = false;
                    return;
                }

                resource.currentAmount = currentAmount;
                resource.maxAmount = maxAmount;
            }

            convertPercentageField.guiActive = true;

            if (convertPercentage == 0)
                return;

            primaryResources.ForEach(m => m.amountRatio = m.currentAmount / m.maxAmount);
            secondaryResources.ForEach(m => m.amountRatio = m.currentAmount / m.maxAmount);

            var percentageRatio = Math.Abs(convertPercentage) / 100d;

            if (convertPercentage > 0 )
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
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            transferRate = 0;
            conversionRatio = 0;

            if (retreivePrimary && primaryResources.Any(r => r.retrieveAmount > 0))
            {
                foreach (var primaryResource in primaryResources)
                {
                    transferRate = primaryResource.transferRate;
                    var fixedTransferRate = transferRate * TimeWarp.fixedDeltaTime;

                    if (fixedTransferRate == 0)
                        continue;

                    double powerReceivedRatio = 1;
                    if (primaryConversionCostPower)
                    {
                        double powerCurrentAmount;
                        double powerMaxAmount;
                        part.GetConnectedResourceTotals(definitionPrimaryPowerResource.id, out powerCurrentAmount, out powerMaxAmount);

                        var powerStorageRatio = powerMaxAmount > 0 ? powerCurrentAmount / powerMaxAmount : 0;
                        var availablePower = powerMaxAmount * Math.Max(0, powerStorageRatio - PowerUsagePercentageRatio);

                        var transferRatio = primaryResource.retrieveAmount >= fixedTransferRate ? 1 : primaryResource.retrieveAmount / fixedTransferRate;
                        var maximumPower = transferRatio * maxPowerPrimary * TimeWarp.fixedDeltaTime * primaryConversionEnergyMult;

                        requestedPower = Math.Min(availablePower, maximumPower);
                        var receivedPower = part.RequestResource(definitionPrimaryPowerResource.id, requestedPower);
                        powerReceivedRatio = requestedPower > 0 ? receivedPower / requestedPower : 0;
                    }

                    var fixedRequest = Math.Min(fixedTransferRate, primaryResource.retrieveAmount);
                    var receivedSourceAmountFixed = part.RequestResource(primaryResource.definition.id, fixedRequest * powerReceivedRatio);

                    double createdAmount = 0;
                    foreach(var secondary in secondaryResources)
                    {
                        conversionRatio = secondary.conversionRatio;
                        var requestedTargetAmount = -receivedSourceAmountFixed * conversionRatio;
                        var receivedTargetAmount = part.RequestResource(secondary.definition.id, requestedTargetAmount) / conversionRatio;
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

                    var fixedTransferRate = transferRate * TimeWarp.fixedDeltaTime;

                    if (fixedTransferRate == 0)
                        continue;
                    
                    double powerReceiverRatio = 1;
                    if (secondaryConversionCostPower)
                    {
                        double powerCurrentAmount;
                        double powerMaxAmount;
                        part.GetConnectedResourceTotals(definitionSecondaryPowerResource.id, out powerCurrentAmount, out powerMaxAmount);

                        var powerStorageRatio = powerMaxAmount > 0 ? powerCurrentAmount / powerMaxAmount : 0;
                        var availablePower = powerMaxAmount * Math.Max(0, powerStorageRatio - PowerUsagePercentageRatio);

                        var transferRatio = secondaryResource.retrieveAmount >= fixedTransferRate ? 1 : secondaryResource.retrieveAmount / fixedTransferRate;
                        var maximumPower = transferRatio * maxPowerPrimary * TimeWarp.fixedDeltaTime * secondaryConversionEnergMult;

                        requestedPower = Math.Min(availablePower, maximumPower);
                        var receivedPower = part.RequestResource(definitionSecondaryPowerResource.id, requestedPower);
                        powerReceiverRatio = requestedPower > 0 ? receivedPower / requestedPower : 0;
                    }

                    var fixedRequest = Math.Min(fixedTransferRate, secondaryResource.retrieveAmount);
                    var receivedSourceAmountFixed = part.RequestResource(secondaryResource.definition.id, fixedRequest * powerReceiverRatio);

                    double createdAmount = 0;
                    foreach (var primary in primaryResources)
                    {
                        conversionRatio = primary.conversionRatio;
                        var requestedTargetAmount = -receivedSourceAmountFixed * conversionRatio;
                        var receivedTargetAmount = part.RequestResource(primary.definition.id, requestedTargetAmount) / conversionRatio;
                        createdAmount += receivedTargetAmount;
                    }

                    var returned = part.RequestResource(secondaryResource.definition.id, createdAmount + receivedSourceAmountFixed);
                    secondaryResource.retrieveAmount = secondaryResource.retrieveAmount - receivedSourceAmountFixed - returned;
                }
            }
        }

        public override string GetInfo()
        {
            return "Primary: " + primaryResourceNames + "\n" + "Secondary: " + secondaryResourceNames ;
        }
    }
}
