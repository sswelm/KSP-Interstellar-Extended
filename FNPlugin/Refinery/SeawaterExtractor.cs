using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class SeawaterExtractor : RefineryActivityBase, IRefineryActivity
    {
        // persistant
        [KSPField(isPersistant = true)]
        protected int lastBodyID = -1; // ID of the last body. Allows us to skip some expensive calls
        [KSPField(isPersistant = true)]
        protected double lastTotalLiquidScooped = 0; // we need to hold this for offline collecting

        [KSPField(isPersistant = true)]
        IDictionary<string, double> resourcePercentages = new Dictionary<string, double>(); // create a new persistent list for keeping track of percentages

        protected double _fixedConsumptionRate;

        public RefineryType RefineryType { get { return RefineryType.heating; } }

        public string ActivityName { get { return "Ocean Extraction"; } }

        public bool HasActivityRequirements() { return IsThereAnyLiquid();  }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public string Status { get { return string.Copy(_status); } }
        // end of IRefinery fields

        // characteristics of the intake liquid, a generic resource we 'collect' and process into resources. This will be the same on all planets, as the 'collection' doesn't rely on abundanceRequests etc. and the resource is not actually collected and stored anywhere anyway
        double _intakeLqdConsumptionRate;
        double _availableIntakeLiquidMass;

        PartResourceDefinition _intakeLiquidDefinition;

        // end of those

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            // get the definition of the 'generic' input resource
            _intakeLiquidDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeLiquid);

        }

        List<OceanicResource> localResources = new List<OceanicResource>(); // create a list for keeping track of localResources

        // variables for the ExtractSeawater function
        double currentResourceProductionRate;
        // end of variables for the ExtractSeawater function

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double timeDifference, bool isStartup = false)
        {
            ExtractSeawater(rateMultiplier, powerFraction, productionModifier, allowOverflow, timeDifference, false);

            updateStatusMessage();
        }
        // this is a function used for IRefinery HasActivityRequirements check
        public bool IsThereAnyLiquid()
        {
            if (GetTotalLiquidScoopedPerSecond() > 0 || _part.GetResourceAvailable(_intakeLiquidDefinition) > 0)
                return true;
            else
                return false;
        }

        /* This is just a short cycle that goes through the air intakes on the vessel, looks at which ones are submerged and multiplies the percentage of the part's submersion
         * with the amount of air it can intake (I'm taking the simplification that air intakes can also intake liquids and running with it). 
         * This value is later stored in the persistent totalAirValue, so that this process can access it when offline collecting.
         * tempLqd is just a variable used to temporarily hold the total amount while cycling through parts, then gets reset at every engine update.
         */
        public double GetTotalLiquidScoopedPerSecond()
        {
            var intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>(); // add any atmo intake part on the vessel to our list
            double tempLqd = 0; // reset tempLqd before we go into the list

            foreach (AtmosphericIntake intake in intakesList) // go through the list
            {
                if (intake.IntakeEnabled) // only process open intakes
                {
                    tempLqd += (intake.area * intake.part.submergedPortion); // add the current intake's liquid intake to our tempLqd. When done with the foreach cycle, we will have the total amount of liquid these intakes collect per cycle
                }
            }
            return tempLqd;
        }

        public void ExtractSeawater(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double timeDifference, bool offlineCollecting)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;

            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            // get the resource for the current body
            localResources = OceanicResourceHandler.GetOceanicCompositionForBody(FlightGlobals.currentMainBody);

            // determine the amount of liquid processed every frame
            _availableIntakeLiquidMass = GetTotalLiquidScoopedPerSecond() * _intakeLiquidDefinition.density;

            // this should determine how much resource this process can consume
            var fixedMaxLiquidConsumptionRate = _current_rate * timeDifference * _intakeLiquidDefinition.density;

            // supplement any missig liquidintake by intake
            var shortage = fixedMaxLiquidConsumptionRate > _availableIntakeLiquidMass ? fixedMaxLiquidConsumptionRate - _availableIntakeLiquidMass : 0;
            if (shortage > 0)
                _availableIntakeLiquidMass += _part.RequestResource(_intakeLiquidDefinition.id, shortage, ResourceFlowMode.ALL_VESSEL);

            var liquidConsumptionRatio = offlineCollecting ? 1
                    : fixedMaxLiquidConsumptionRate > 0
                        ? Math.Min(fixedMaxLiquidConsumptionRate, _availableIntakeLiquidMass) / fixedMaxLiquidConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * timeDifference * liquidConsumptionRatio;

            if (_fixedConsumptionRate <= 0) return;

            foreach (OceanicResource resource in localResources)
            {
                // get the name of the resource
                var currentResourceName = resource.ResourceName;

                if (currentResourceName == null) 
                    continue; // this resource does not interest us anymore

                var currentDefinition = PartResourceLibrary.Instance.GetDefinition(currentResourceName);
                if (currentDefinition == null) 
                    continue; // this resource is missing a resource definition

                // determine the spare room - gets parts that contain the current resource, gets the sum of their maxAmount - (current)amount and multiplies by density of resource
                var currentResourceSpareRoom = _part.GetConnectedResources(currentResourceName).Sum(r => r.maxAmount - r.amount) * currentDefinition.density;

                double currentResourcePercentage = 0;
                if (FlightGlobals.currentMainBody.flightGlobalsIndex != lastBodyID) // the calculations in here don't have to be run every update - we can instead store the percentage in a persistent dictionary with the resource name as key and retrieve it from there
                {
                    // determine the percentage (how much of the resource will be produced from the intake liquid)
                    currentResourcePercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, currentResourceName);

                    // we'll store the value for next time
                    if (resourcePercentages.ContainsKey(currentResourceName)) // check if the dictionary is storing a value with this key already
                        resourcePercentages.Remove(currentResourceName); // then first do a remove

                    resourcePercentages.Add(currentResourceName, currentResourcePercentage); // add the current percentage to a dictionary, for easy access
                }
                else // if we have been here already, just fetch the value from the dictionary
                {
                    double percentage;
                    if (resourcePercentages.TryGetValue(currentResourceName, out percentage))
                        currentResourcePercentage = percentage;
                    else
                    {
                        Debug.Log("[KSPI]: Could not retrieve resource percentage from dictionary, setting to zero");
                        currentResourcePercentage = 0;
                    }
                }
                // how much we should add per cycle
                var currentResourceMaxRate = _fixedConsumptionRate * currentResourcePercentage;

                // how much we actually CAN add per cycle (into the spare room in the vessel's tanks) - if the allowOverflow setting is on, dump it all in even though it won't fit (excess is lost), otherwise use the smaller of two values (spare room remaining and the full rate)
                var currentResourcePossibleRate = allowOverflow ? currentResourceMaxRate : Math.Min(currentResourceSpareRoom, currentResourceMaxRate);

                // calculate the ratio of rates, if the denominator is zero, assign zero outright to prevent problems
                var currentResourceRatio = (currentResourceMaxRate == 0) ? 0 : currentResourcePossibleRate / currentResourceMaxRate;

                // calculate the consumption rate of the intake liquid
                _intakeLqdConsumptionRate = (currentResourceRatio * _fixedConsumptionRate / _intakeLiquidDefinition.density) / timeDifference * _intakeLiquidDefinition.density;

                if (offlineCollecting) // if collecting offline, multiply by the elapsed time
                {
                    _intakeLqdConsumptionRate = _fixedConsumptionRate * timeDifference;
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SeawaterExtract_Postmsg1", _intakeLiquidDefinition.name, timeDifference.ToString("F0"), _intakeLqdConsumptionRate.ToString("F2")), 60.0f, ScreenMessageStyle.UPPER_CENTER);//"The ocean extractor processed " +  + " for " +  + " seconds, processing " +  + " units in total."
                }

                // calculate the rate of production
                var currentResourceTempProductionRate = _intakeLqdConsumptionRate * currentResourcePercentage;

                // add the produced resource
                currentResourceProductionRate = -_part.RequestResource(currentResourceName, -currentResourceTempProductionRate * timeDifference / currentDefinition.density, ResourceFlowMode.ALL_VESSEL) / timeDifference * currentDefinition.density;
            }

            if (lastBodyID != FlightGlobals.currentMainBody.flightGlobalsIndex)
            {
                // update lastBodyID to this planets ID (i.e. remember this body)
                lastBodyID = FlightGlobals.currentMainBody.flightGlobalsIndex;
            }
        }


        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SeawaterExtract_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SeawaterExtract_LqdConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Intake Lqd Consumption"
            GUILayout.Label(((_intakeLqdConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SeawaterExtract_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Production Rate"
            GUILayout.Label(((currentResourceProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            foreach (var resource in localResources)
            {
                if (resource == null || resource.ResourceName == null) 
                    continue; // this resource does not interest us anymore

                var resourceDensityUI = PartResourceLibrary.Instance.GetDefinition(resource.ResourceName).density; // gets the resource density (needed for the next two lines)
                var spareRoomLabel = (_part.GetConnectedResources(resource.ResourceName).Sum(r => r.maxAmount - r.amount) * resourceDensityUI).ToString("0.0000"); // gets spare room for the resource on the whole vessel
                var maxCapacityLabel = (_part.GetConnectedResources(resource.ResourceName).Sum(p => p.maxAmount) * resourceDensityUI).ToString("0.0000"); // gets the max capacity for the resource on the whole vessel

                double percentage;
                double resourcePercentageUI;

                if (resourcePercentages.TryGetValue(resource.ResourceName, out percentage))
                    resourcePercentageUI = percentage;
                else
                {
                    Debug.Log("[KSPI]: UI could not access resourcePercentage from dictionary, setting to zero");
                    resourcePercentageUI = 0;
                }

                var productionRateLabel = (((_intakeLqdConsumptionRate * resourcePercentageUI) * TimeWarp.fixedDeltaTime / resourceDensityUI) * GameConstants.SECONDS_IN_HOUR).ToString("0.0000"); // dirty calculation of the production rate cast to string and made hourly

                if (resourcePercentageUI > 0) // if the percentage is zero, there's no processing going on, so we don't really need to print it here
                {
                    // calculations done, print it out - first the Storage
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localizer.Format("#LOC_KSPIE_SeawaterExtract_ResourcesStorage", resource.ResourceName), _bold_label, GUILayout.Width(labelWidth));// + " Storage"
                    GUILayout.Label(spareRoomLabel + " mT / " + maxCapacityLabel + " mT", _value_label, GUILayout.Width(valueWidth));
                    GUILayout.EndHorizontal();

                    // next print out the production rates
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.ResourceName + Localizer.Format("#LOC_KSPIE_SeawaterExtract_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Production Rate"
                    GUILayout.Label((resourcePercentageUI * 100) + "% " + productionRateLabel + " mT/hour", _value_label, GUILayout.Width(valueWidth));
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void updateStatusMessage()
        {
            if (_intakeLqdConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_SeawaterExtract_Statumsg1");//"Extracting intake liquid"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_SeawaterExtract_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_SeawaterExtract_Statumsg3");//"Insufficient Storage, try allowing overflow"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SeawaterExtract_Postmsg2") + " " + InterstellarResourcesConfiguration.Instance.IntakeLiquid, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
