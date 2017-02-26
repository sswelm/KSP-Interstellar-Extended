using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin.Refinery
{
    class SeawaterExtractor : IRefineryActivity
    {
        // persistant
        [KSPField(isPersistant = true)]
        protected int lastBodyID = -1; // ID of the last body. Allows us to skip some expensive calls
        [KSPField(isPersistant = true)]
        protected double lastTotalLiquidScooped = 0; // we need to hold this for offline collecting

        [KSPField(isPersistant = true)]
        IDictionary<string, double> resourcePercentages = new Dictionary<string, double>(); // create a new persistent list for keeping track of tempRates

        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";

        protected double _current_power;
        protected double _fixedConsumptionRate;
        protected double _consumptionStorageRatio;
        protected List<double> storageRatios;


        // IRefinery fields
        protected double _current_rate;

        private GUIStyle _bold_label;

        public RefineryType RefineryType { get { return RefineryType.heating; } }

        public String ActivityName { get { return "Seawater Extraction"; } }

        public double CurrentPower { get { return _current_power; } }

        private double _effectiveMaxPower;

        public bool HasActivityRequirements
        {
            get
            {
                return IsThereAnyLiquid();
            }
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }
        // end of IRefinery fields

        // characteristics of the intake liquid, a generic resource we 'collect' and process into resources. This will be the same on all planets, as the 'collection' doesn't rely on abundanceRequests etc. and the resource is not actually collected and stored anywhere anyway
        protected string _intakeLqdResourceName;
        double _intakeLqdDensity;
        double _intakeLqdConsumptionRate;
        protected double _availableIntakeLiquidMass;
        // end of those

        public SeawaterExtractor(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            // get the name of the 'generic' input resource
            _intakeLqdResourceName = InterstellarResourcesConfiguration.Instance.IntakeLiquid;
            _intakeLqdDensity = PartResourceLibrary.Instance.GetDefinition(_intakeLqdResourceName).density;

        }

        List<OceanicResource> localResources = new List<OceanicResource>(); // create a list for keeping track of localResources
        List<AtmosphericIntake> intakesList = new List<AtmosphericIntake>(); // create a new list for keeping track of atmospheric intakes

        // variables for the ExtractSeawater function
        string currentResourceName; // for holding the name of the current resource
        double currentResourceDensity; // for holding the density of the current resource
        double currentResourceSpareRoom; // for holding the amount of spare room for the resource on the whole vessel
        double currentResourcePercentage; // for holding the percentage of the resource in the current oceanic definition
        double currentResourceMaxRate;
        double currentResourcePossibleRate;
        double currentResourceRatio;
        double currentResourceTempProductionRate;
        double currentResourceProductionRate;
        // end of variables for the ExtractSeawater function
        
        // variables for UpdateGUI
        string resourceLabel;
        string spareRoomLabel;
        string maxCapacityLabel;
        string productionRateLabel;
        double resourcePercentageUI;
        double resourceDensityUI;
        // end of variables for UpdateGUI

        // variables for determining if the intakes are submerged and the needed calculations
        double tempLqd;
        double tempSubmergedPercentage;
        double tempArea;
        // end of those


        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double timeDifference)
        {
            ExtractSeawater(rateMultiplier, powerFraction, productionModifier, allowOverflow, timeDifference, false);

            updateStatusMessage();
        }
        // this is a function used for IRefinery HasActivityRequirements check
        public bool IsThereAnyLiquid()
        {
            if (GetTotalLiquidScoopedPerSecond() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /* This is just a short cycle that goes through the air intakes on the vessel, looks at which ones are submerged and multiplies the percentage of the part's submersion
         * with the amount of air it can intake (I'm taking the simplification that air intakes can also intake liquids and running with it). 
         * This value is later stored in the persistent totalAirValue, so that this process can access it when offline collecting.
         * tempLqd is just a variable used to temporarily hold the total amount while cycling through parts, then gets reset at every engine update.
         */
        public double GetTotalLiquidScoopedPerSecond()
        {
            intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>(); // add any atmo intake part on the vessel to our list
            tempLqd = 0; // reset tempLqd before we go into the list
            tempArea = 0;
            tempSubmergedPercentage = 0;
            foreach (AtmosphericIntake intake in intakesList) // go through the list
            {
                if (intake.IntakeEnabled == true) // only process open intakes
                {
                    tempArea = intake.area; // get the area of the intake part (basically size of intake)
                    tempSubmergedPercentage = intake.part.submergedPortion; // get the percentage of submersion of the intake part (0-1), works only when everything is fully loaded
                    tempLqd += (tempArea * tempSubmergedPercentage); // add the current intake's liquid intake to our tempLqd. When done with the foreach cycle, we will have the total amount of liquid these intakes collect per cycle
                }
            }
            return tempLqd;
        }

        public void ExtractSeawater(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double timeDifference, bool offlineCollecting)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;

            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;
            
            if (FlightGlobals.currentMainBody.flightGlobalsIndex != lastBodyID) // this will only fire the first time (because lastBodyID is set to start at -1 and flightGlobalsIndexes start at 0) and then when the Sphere of Influence is changed (vessel orbits another body)
            {
                localResources = OceanicResourceHandler.GetOceanicCompositionForBody(FlightGlobals.currentMainBody);
            }

            // somewhere here determine the consumption rate of the intake liquid (but do it generically as well?)
            _availableIntakeLiquidMass = GetTotalLiquidScoopedPerSecond() * _intakeLqdDensity;

            // this should determine how much resource this process can consume
            var fixedMaxLiquidConsumptionRate = _current_rate * timeDifference * _intakeLqdDensity;
            var liquidConsumptionRatio = offlineCollecting ? 1
                    : fixedMaxLiquidConsumptionRate > 0
                        ? Math.Min(fixedMaxLiquidConsumptionRate, _availableIntakeLiquidMass) / fixedMaxLiquidConsumptionRate
                        : 0;

            _fixedConsumptionRate = _current_rate * timeDifference * liquidConsumptionRatio;



            if (_fixedConsumptionRate > 0) // if there is anything to consume, proceed
            {

                foreach (OceanicResource resource in localResources)
                {
                    // get the name of the resource
                    currentResourceName = resource.ResourceName;

                    if (currentResourceName == null)
                    {
                        continue; // this resource does not interest us anymore
                    }
                    // get the density
                    currentResourceDensity = PartResourceLibrary.Instance.GetDefinition(currentResourceName).density;

                    // determine the spare room - gets parts that contain the current resource, gets the sum of their maxAmount - (current)amount and multiplies by density of resource
                    currentResourceSpareRoom = _part.GetConnectedResources(currentResourceName).Sum(r => r.maxAmount - r.amount) * currentResourceDensity;

                    if (FlightGlobals.currentMainBody.flightGlobalsIndex != lastBodyID) // the calculations in here don't have to be run every update - we can instead store the percentage in a persistent dictionary with the resource name as key and retrieve it from there
                    {
                        // determine the percentage (how much of the resource will be produced from the intake liquid)
                        currentResourcePercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, currentResourceName);

                        // we'll store the value for next time
                        if (resourcePercentages.ContainsKey(currentResourceName)) // check if the dictionary is storing a value with this key already
                        {
                            resourcePercentages.Remove(currentResourceName); // then first do a remove
                        }
                        resourcePercentages.Add(currentResourceName, currentResourcePercentage); // add the current percentage to a dictionary, for easy access
                    }
                    else // if we have been here already, just fetch the value from the dictionary
                    {
                        try
                        {
                            currentResourcePercentage = resourcePercentages[currentResourceName]; // load the value from dictionary using the name as the key

                        }
                        catch (Exception)
                        {
                            Debug.Log("[KSPI-SeawaterExtractor] Could not retrieve resource percentage from dictionary, setting to zero");
                            currentResourcePercentage = 0;
                        }
                    }
                    // how much we should add per cycle
                    currentResourceMaxRate = _fixedConsumptionRate * currentResourcePercentage;

                    // how much we actually CAN add per cycle (into the spare room in the vessel's tanks) - if the allowOverflow setting is on, dump it all in even though it won't fit (excess is lost), otherwise use the smaller of two values (spare room remaining and the full rate)
                    currentResourcePossibleRate = allowOverflow ? currentResourceMaxRate : Math.Min(currentResourceSpareRoom, currentResourceMaxRate);

                    // calculate the ratio of rates, if the denominator is zero, assign zero outright to prevent problems
                    currentResourceRatio = (currentResourceMaxRate == 0) ? 0 : currentResourcePossibleRate / currentResourceMaxRate;

                    
                    // calculate the consumption rate of the intake liquid
                    _intakeLqdConsumptionRate = (currentResourceRatio * _fixedConsumptionRate / _intakeLqdDensity) / timeDifference * _intakeLqdDensity;

                    if (offlineCollecting) // if collecting offline, multiply by the elapsed time
                    {
                        _intakeLqdConsumptionRate = _fixedConsumptionRate * timeDifference;
                        ScreenMessages.PostScreenMessage("The seawater extractor processed " + _intakeLqdResourceName + " for " + timeDifference.ToString("F0") + " seconds, processing " + _intakeLqdConsumptionRate.ToString("F2") + " units in total.", 60.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    
                    // calculate the rate of production
                    currentResourceTempProductionRate = _intakeLqdConsumptionRate * currentResourcePercentage;

                    // add the produced resource
                    currentResourceProductionRate = -_part.RequestResource(currentResourceName, -currentResourceTempProductionRate * timeDifference / currentResourceDensity, ResourceFlowMode.ALL_VESSEL) / timeDifference * currentResourceDensity;
                }

                if (lastBodyID != FlightGlobals.currentMainBody.flightGlobalsIndex)
                {
                    // update lastBodyID to this planets ID (i.e. remember this body)
                    lastBodyID = FlightGlobals.currentMainBody.flightGlobalsIndex;
                }

            }

           
        }


        public void UpdateGUI()
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Intake Lqd Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_intakeLqdConsumptionRate * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            foreach (OceanicResource resource in localResources)
            {
                // helper variables and calculations - we shouldn't access the variables used in the Extract function, because this is a different loop
                resourceLabel = resource.ResourceName; // name of the resource
                if (resourceLabel == null)
                {
                    continue; // this resource does not interest us anymore
                }
                resourceDensityUI = PartResourceLibrary.Instance.GetDefinition(resourceLabel).density; // gets the resource density (needed for the next two lines)
                spareRoomLabel = (_part.GetConnectedResources(resourceLabel).Sum(r => r.maxAmount - r.amount) * resourceDensityUI).ToString("0.0000"); // gets spare room for the resource on the whole vessel
                maxCapacityLabel = (_part.GetConnectedResources(resourceLabel).Sum(p => p.maxAmount) * resourceDensityUI).ToString("0.0000"); // gets the max capacity for the resource on the whole vessel
                
                try
                {
                    resourcePercentageUI = resourcePercentages[resourceLabel];
                }
                catch (Exception)
                {
                    Debug.Log("[KSPI-SeawaterExtractor] UI could not access resourcePercentage from dictionary, setting to zero");
                    resourcePercentageUI = 0;
                }

                productionRateLabel = (((_intakeLqdConsumptionRate * resourcePercentageUI) * TimeWarp.fixedDeltaTime / resourceDensityUI) * GameConstants.HOUR_SECONDS).ToString("0.0000"); // dirty calculation of the production rate cast to string and made hourly

                if (resourcePercentageUI > 0) // if the percentage is zero, there's no processing going on, so we don't really need to print it here
                {
                    // calculations done, print it out - first the Storage
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resourceLabel + " Storage", _bold_label, GUILayout.Width(labelWidth));
                    GUILayout.Label(spareRoomLabel + " mT / " + maxCapacityLabel + " mT", GUILayout.Width(valueWidth));
                    GUILayout.EndHorizontal();

                    // next print out the production rates
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resourceLabel + "Production Rate", _bold_label, GUILayout.Width(labelWidth));
                    GUILayout.Label((resourcePercentageUI * 100) + "% " + productionRateLabel + " mT/hour", GUILayout.Width(valueWidth));
                    GUILayout.EndHorizontal();
                }
            }

        }

        private void updateStatusMessage()
        {
            if (_intakeLqdConsumptionRate > 0)
                _status = "Extracting intake liquid";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage, try allowing overflow";
        }
    }
}
