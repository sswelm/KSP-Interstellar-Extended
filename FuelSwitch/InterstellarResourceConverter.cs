using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    class ResourceStats
    {
        public PartResourceDefinition definition;
        public double maxAmount = 0;
        public double currentAmount = 0;
        public double amountRatio = 0;
        public double retrieve = 0;
        public double transferRate = 1;
        public double normalizedDensity = 1;
        public double conversionRatio = 1;
    }

    class InterstellarEquilibrium : InterstellarResourceConverter  { }

    class InterstellarResourceConverter : PartModule
    {
        // persistant
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Convert", guiUnits = "%"), UI_FloatRange()]
        public float convertPercentage = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false,  guiName = "Control"), UI_Toggle(disabledText = "Primary", enabledText = "Secondary")]
        public bool positiveSliderControlsPrimary = false;

        // configs
        [KSPField]
        public bool showControlToggle = false;
        [KSPField]
        public string sliderText = string.Empty;
        [KSPField]
        public float percentageMaxValue = 100;
        [KSPField]
        public float percentageMinValue = -100;
        [KSPField]
        public float percentageStepIncrement = 10;
        [KSPField]
        public bool percentageSymetry = true;
        [KSPField]
        public string primaryResourceNames = string.Empty;
        [KSPField]
        public string secondaryResourceNames = string.Empty;
        [KSPField]
        public double maxTransferAmountPrimary = 0;
        [KSPField]
        public double maxTransferAmountSecondary = 0;

        [KSPField]
        public bool requiresPrimaryLocalInEditor = true;
        [KSPField]
        public bool requiresPrimaryLocalInFlight = true;

        BaseField positiveSliderControlsField;
        BaseField convertPercentageField;
        List<ResourceStats> primaryResources;
        List<ResourceStats> secondaryResources;
        UI_FloatRange convertPecentageEditorFloatRange;
        UI_FloatRange convertPecentageFlightFloatRange;
        bool hasNullDefinitions = false;
        bool retreivePrimary;
        bool retrieveSecondary;
        bool maxTransferAmountPrimaryIsMissing = false;
        bool maxTransferAmountSecondaryIsMissing = false;


        public override void OnStart(PartModule.StartState state)
        {
            positiveSliderControlsField = Fields["positiveSliderControlsPrimary"];
            positiveSliderControlsField.guiActive = showControlToggle;
            positiveSliderControlsField.guiActiveEditor = showControlToggle;

            convertPercentageField = Fields["convertPercentage"];

            maxTransferAmountPrimaryIsMissing = maxTransferAmountPrimary <= 0;
            maxTransferAmountSecondaryIsMissing = maxTransferAmountSecondary <= 0;

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
                if (resource.definition.density > 0 && resource.definition.volume > 0)
                    resource.normalizedDensity = resource.definition.density / resource.definition.volume;
            }

            foreach (var resource in secondaryResources)
            {
                if (resource.definition.density > 0 && resource.definition.volume > 0)
                    resource.normalizedDensity = resource.definition.density / resource.definition.volume;
            }

            if (primaryResources.Count == 1 && secondaryResources.Count == 1)
            {
                var primary = primaryResources.First();
                var secondary = secondaryResources.First();

                if (primary.normalizedDensity > 0 && secondary.normalizedDensity > 0)
                {
                    secondary.conversionRatio = (primary.normalizedDensity * primary.definition.volume) / (secondary.normalizedDensity * secondary.definition.volume);
                    primary.conversionRatio = (secondary.normalizedDensity * secondary.definition.volume) / (primary.normalizedDensity * primary.definition.volume);
                }
                else if (primary.definition.unitCost > 0 && secondary.definition.unitCost > 0)
                {
                    secondary.conversionRatio = primary.definition.unitCost / secondary.definition.unitCost;
                    primary.conversionRatio = secondary.definition.unitCost / primary.definition.unitCost;
                }
            }
            
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
                return;

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

            var convertPercentageRatio = 1 - Math.Abs(convertPercentage) / 100d;

            if (convertPercentage > 0 && primaryResources.Any(m => convertPercentageRatio < m.amountRatio))
            {
                retreivePrimary = true;

                if (positiveSliderControlsPrimary)
                {
                    var availableSpaceInTarget = secondaryResources.Min(m => (m.maxAmount - m.currentAmount) / m.conversionRatio);
                    primaryResources.ForEach(m => m.retrieve = Math.Min((Math.Max(m.amountRatio - convertPercentageRatio, 0)) * m.maxAmount, availableSpaceInTarget));
                }
                else
                {
                    var neededAmount = secondaryResources.Min(m => Math.Max((1 - convertPercentageRatio) - m.amountRatio, 0) * m.maxAmount / m.conversionRatio);
                    primaryResources.ForEach(m => m.retrieve = neededAmount);
                }

                primaryResources.ForEach(m => m.transferRate = maxTransferAmountPrimary);
            }
            if (convertPercentage < 0 && secondaryResources.Any(m => convertPercentageRatio < m.amountRatio))
            {
                retrieveSecondary = true;

                var availableSpaceInTarget = primaryResources.Min(m => (m.maxAmount - m.currentAmount) / m.conversionRatio);
                secondaryResources.ForEach(m => m.retrieve = Math.Max((Math.Min(m.amountRatio - convertPercentageRatio, 0)) * m.maxAmount, availableSpaceInTarget));
                secondaryResources.ForEach(m => m.transferRate = maxTransferAmountPrimary);
            }
        }

        public void FixedUpdate()
        {
            if (retreivePrimary  && maxTransferAmountPrimary > 0)
            {
                foreach (var resource in primaryResources)
                {
                    var fixedRequest = Math.Min(resource.transferRate * TimeWarp.fixedDeltaTime, resource.retrieve);
                    var receivedAmount = part.RequestResource(resource.definition.id, fixedRequest);

                    double createdAmount = 0;
                    foreach(var secondary in secondaryResources)
                    {
                        createdAmount += part.RequestResource(secondary.definition.id, -receivedAmount * secondary.conversionRatio) / secondary.conversionRatio;
                    }

                    var resturned = part.RequestResource(resource.definition.id, createdAmount + receivedAmount);
                    resource.retrieve = resource.retrieve - receivedAmount - resturned;
                }
            }
            else if (retrieveSecondary && maxTransferAmountSecondary > 0)
            {
                foreach (var resource in secondaryResources)
                {
                    var fixedRequest = Math.Min(resource.transferRate * TimeWarp.fixedDeltaTime, resource.retrieve);
                    var receivedAmount = part.RequestResource(resource.definition.id, fixedRequest);

                    double createdAmount = 0;
                    foreach (var primary in primaryResources)
                    {
                        createdAmount += part.RequestResource(primary.definition.id, -receivedAmount * primary.conversionRatio) / primary.conversionRatio;
                    }

                    var resturned = part.RequestResource(resource.definition.id, createdAmount + receivedAmount);
                    resource.retrieve = resource.retrieve - receivedAmount - resturned;
                }
            }
        }

        public override string GetInfo()
        {
            return "Primary: " + primaryResourceNames + "\n"
                 + "Secondary: " + secondaryResourceNames ;
        }
    }
}
