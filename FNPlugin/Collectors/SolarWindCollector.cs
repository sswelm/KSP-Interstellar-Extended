using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class SolarWindCollector : FNResourceSuppliableModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool bIsEnabled = false;
        [KSPField(isPersistant = true)]
        public float fLastActiveTime;
        [KSPField(isPersistant = true)]
        public float fLastPowerPercentage;

        // Part properties
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Surface area", guiUnits = "m\xB2")]
        public float surfaceArea = 0; // Surface area of the panel.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Collector effectiveness", guiFormat = "P1")]
        public float effectiveness = 1.0f; // Effectiveness of the panel. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "MW Requirements", guiUnits = "MW")]
        public float mwRequirements = 1.0f; // MW requirements of the collector panel.


        // GUI
        [KSPField(guiActive = true, guiName = "Solar Wind Concentration", guiUnits = "%")]
        protected string strSolarWindConc = "";
        [KSPField(guiActive = true, guiName = "Distance from the sun")]
        protected string strStarDist = "";
        [KSPField(guiActive = true, guiName = "Status")]
        protected string strCollectingStatus = "";
        [KSPField(guiActive = true, guiName = "Power Usage")]
        protected string strReceivedPower = "";
        // internals
        protected float fResourceFlow = 0;

        [KSPEvent(guiActive = true, guiName = "Activate Collector", active = true)]
        public void ActivateCollector()
        {
            bIsEnabled = true;
            OnUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "Disable Collector", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnUpdate();
        }

        [KSPAction("Activate Collector")]
        public void ActivateScoopAction(KSPActionParam param)
        {
            ActivateCollector();
        }

        [KSPAction("Disable Collector")]
        public void DisableScoopAction(KSPActionParam param)
        {
            DisableCollector();
        }

        [KSPAction("Toggle Collector")]
        public void ToggleScoopAction(KSPActionParam param)
        {
            if (bIsEnabled)
                DisableCollector();
            else
                ActivateCollector();
        }

        const double dKerbinDistance = 13599840256; // distance of Kerbin from Sun/Kerbol in meters (i.e. AU)
        protected double dDistanceFromStar = 0; // distance of the current vessel from the system's star
        protected double dConcentrationSolarWind = 0;
        protected double dSolarWindSpareCapacity;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return; // collecting won't work in editor

            this.part.force_activate();

            // verify collector was enabled 
            if (!bIsEnabled) return;

            // verify a timestamp is available
            if (fLastActiveTime == 0) return;

            // verify any power was available in previous state
            if (fLastPowerPercentage < 0.01) return;

            // here could be other checks, if deemed relevant - if the vessel is not too low/high, orbit eccentricity etc.

            // verify altitude is not too low
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)))
            {
                ScreenMessages.PostScreenMessage("Error, vessel is in atmosphere", 10.0f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // calculate time difference since last time the vessel was active
            double dTimeDifference = (Planetarium.GetUniversalTime() - fLastActiveTime) * 55;

            // collect solar wind for entire duration
            CollectSolarWind(dTimeDifference, true);
        }



        public override void OnUpdate()
        {
            Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled

            Fields["strReceivedPower"].guiActive = bIsEnabled;

            dConcentrationSolarWind = CalculateSolarWindConcentration();
            strSolarWindConc = (dConcentrationSolarWind * 100).ToString("F2");
            strStarDist = (dDistanceFromStar/1000).ToString("F2") + " km";
        }

        public override void OnFixedUpdate()
        {
            if (FlightGlobals.fetch != null)
            {
                if (!bIsEnabled)
                {
                    strCollectingStatus = "Disabled";
                    strStarDist = (CalculateDistanceToSun() / 1000).ToString("F2") + " km"; // trying to solve the bullshit
                    return;
                }
                // collect solar wind for a single frame
                CollectSolarWind(TimeWarp.fixedDeltaTime, false);

                // store current time in case vesel is unloaded
                fLastActiveTime = (float)Planetarium.GetUniversalTime();
            }
        }



        // calculates solar wind concentration
        private double CalculateSolarWindConcentration()
        {
            // check if the vessel is not in shade
            if (!PluginHelper.lineOfSightToSun(vessel))
            {
                strCollectingStatus = "No solar wind detected";
                return dConcentrationSolarWind = 0.0;
            }

            dConcentrationSolarWind = dKerbinDistance / CalculateDistanceToSun(); // In Kerbin vicinity, the solar wind concentration will be around 100% and it will rise the closer the vessel gets to the sun
            return dConcentrationSolarWind;
        }

        // calculates the distance to sun
        private double CalculateDistanceToSun()
        {
            Vector3d sunPosition = FlightGlobals.fetch.bodies[0].position;
            Vector3d vesselPosition = this.part.transform.position;
            return dDistanceFromStar = Vector3d.Distance(vesselPosition,sunPosition);
        }

        // the main collecting function
        private void CollectSolarWind(double deltaTimeInSeconds, bool offlineCollecting)
        {
            dConcentrationSolarWind = CalculateSolarWindConcentration();

            string strSolarWindResourceName = InterstellarResourcesConfiguration.Instance.SolarWind;
            double dPowerRequirementsMW = (double)PluginHelper.PowerConsumptionMultiplier * mwRequirements; // change the mwRequirements number in part config to change the power consumption

            // check for free space in solar wind 'tanks'
            dSolarWindSpareCapacity = part.GetResourceSpareCapacity(strSolarWindResourceName);

            if (dConcentrationSolarWind > 0 && (dSolarWindSpareCapacity > 0))
            {
                // calculate available power
                double dPowerReceivedMW = Math.Max((double)consumeFNResource(dPowerRequirementsMW * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES), 0);
                double dNormalisedRevievedPowerMW = dPowerReceivedMW / TimeWarp.fixedDeltaTime;

                // if power requirement sufficiently low, retreive power from KW source
                if (dPowerRequirementsMW < 2 && dNormalisedRevievedPowerMW <= dPowerRequirementsMW)
                {
                    double dRequiredKW = (dPowerRequirementsMW - dNormalisedRevievedPowerMW) * 1000;
                    double dReceivedKW = ORSHelper.fixedRequestResource(part, FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, dRequiredKW * TimeWarp.fixedDeltaTime);
                    dPowerReceivedMW += (dReceivedKW / 1000);
                }

                fLastPowerPercentage = offlineCollecting ? fLastPowerPercentage : (float)(dPowerReceivedMW / dPowerRequirementsMW / TimeWarp.fixedDeltaTime);

                // show in GUI
                strCollectingStatus = "Collecting solar wind";
            }
            else
            {
                fLastPowerPercentage = 0;
                dPowerRequirementsMW = 0;
            }

            // set the GUI string to state the number of KWs received if the MW requirements were lower than 2, otherwise in MW
            strReceivedPower = dPowerRequirementsMW < 2
                ? (fLastPowerPercentage * dPowerRequirementsMW * 1000).ToString("0.0") + " KW / " + (dPowerRequirementsMW * 1000).ToString("0.0") + " KW"
                : (fLastPowerPercentage * dPowerRequirementsMW).ToString("0.0") + " MW / " + dPowerRequirementsMW.ToString("0.0") + " MW";

            // this is the first important bit - how much solar wind has been collected
            double dResourceChange = ((dConcentrationSolarWind * surfaceArea)/1000) * effectiveness * fLastPowerPercentage * deltaTimeInSeconds;

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting)
            {
                string strNumberFormat = dResourceChange > 100 ? "0" : "0.00";
                ScreenMessages.PostScreenMessage("The Solar Wind Collector collected " + dResourceChange.ToString(strNumberFormat) + " units of " + strSolarWindResourceName, 10.0f, ScreenMessageStyle.LOWER_CENTER);
            }

            // this is the second important bit - do the actual change of the resource amount in the vessel
            fResourceFlow = (float)ORSHelper.fixedRequestResource(part, strSolarWindResourceName, -dResourceChange);
            fResourceFlow = -fResourceFlow / TimeWarp.fixedDeltaTime;
        }

    }
}

