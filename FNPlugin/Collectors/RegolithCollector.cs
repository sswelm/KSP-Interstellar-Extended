﻿using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Collectors
{
    class RegolithCollector : ResourceSuppliableModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool bIsEnabled = false;
        [KSPField(isPersistant = true)]
        public double dLastActiveTime;
        [KSPField(isPersistant = true)]
        public double dLastPowerPercentage;
        [KSPField(isPersistant = true)]
        public double dLastRegolithConcentration;

        // Part properties
        [KSPField(guiActiveEditor = true, guiName = "Drill size", guiUnits = " m\xB3")]
        public double drillSize = 0; // Volume of the collector's drill. Raise in part config (for larger drills) to make collecting faster.
        [KSPField(guiActiveEditor = true, guiName = "Drill effectiveness", guiFormat = "P1")]
        public double effectiveness = 1; // Effectiveness of the drill. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(guiActiveEditor = true, guiName = "MW Requirements", guiUnits = " MW")]
        public double mwRequirements = 1; // MW requirements of the drill. Affects heat produced.
        [KSPField(guiActiveEditor = true, guiName = "Waste Heat Modifier", guiFormat = "P1")]
        public double wasteHeatModifier = 1; // How much of the power requirements ends up as heat. Change in part cfg, treat as a percentage (1 = 100%). Higher modifier means more energy ends up as waste heat.

        // GUI
        [KSPField(guiActive = true, guiName = "Regolith Concentration", guiFormat = "P1")]
        protected string strRegolithConc = "";
        [KSPField(guiActive = true, guiName = "Distance from the sun")]
        protected string strStarDist = "";
        [KSPField(guiActive = true, guiName = "Drill status")]
        protected string strCollectingStatus = "";
        [KSPField(guiActive = true, guiName = "Power Usage")]
        protected string strReceivedPower = "";
        [KSPField(guiActive = true, guiName = "Altitude", guiUnits = " m")]
        protected string strAltitude = "";

        [KSPField(isPersistant = true, guiActive = true, guiName = "Resource Production", guiUnits = " Unit/s")]
        public double resourceProduction;

        // internals
        protected double dResourceFlow = 0;

        [KSPEvent(guiActive = true, guiName = "Activate Drill", active = true)]
        public void ActivateCollector()
        {
            if (IsCollectLegal() == true) // will only be activated if the collecting of resource is legal
            {
                bTouchDown = TryRaycastToHitTerrain(); // check if there's ground within reach and if the drill is deployed
                if (bTouchDown == false) // if not, no collecting
                {
                    ScreenMessages.PostScreenMessage("Regolith drill not in contact with ground. Make sure drill is deployed and can reach the terrain.", 3, ScreenMessageStyle.LOWER_CENTER);
                    DisableCollector();
                    return;
                }
                bIsEnabled = true;
                OnUpdate();
            }
        }

        [KSPEvent(guiActive = true, guiName = "Disable Drill", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnUpdate();
        }

        [KSPAction("Activate Drill")]
        public void ActivateScoopAction(KSPActionParam param)
        {
            ActivateCollector();
        }

        [KSPAction("Disable Drill")]
        public void DisableScoopAction(KSPActionParam param)
        {
            DisableCollector();
        }

        [KSPAction("Toggle Drill")]
        public void ToggleScoopAction(KSPActionParam param)
        {
            if (bIsEnabled)
                DisableCollector();
            else
                ActivateCollector();
        }

        
        protected double dDistanceFromStar = 0; // distance of the current vessel from the system's star
        protected double dConcentrationRegolith = 0; // regolith concentration at the current location
        protected double dRegolithSpareCapacity = 0; // spare capacity for the regolith on the vessel
        protected double dRegolithDensity; // 'density' of regolith at the current spot
        protected double dTotalWasteHeatProduction = 0; // total waste heat produced in the cycle
        protected double dAltitude = 0; // current terrain altitude
        protected bool bTouchDown = false; // helper bool, is the part touching the ground
        uint counter = 0; // helper counter for update cycles, so that we can only do some calculations once in a while
        uint anotherCounter = 0; // helper counter for fixedupdate cycles, so that we can only do some calculations once in a while (I don't want to add complexity by using the previous counter in two places - also update and fixedupdate cycles can be out of sync, apparently)
        protected double dFinalConcentration;
        AbundanceRequest regolithRequest = new AbundanceRequest // create a new request object that we'll reuse to get the current stock-system resource concentration
        {
            ResourceType = HarvestTypes.Planetary,
            ResourceName = "Regolith",
            BodyId = 1, // this will need to be updated before 'sending the request'
            Latitude = 0, // this will need to be updated before 'sending the request'
            Longitude = 0, // this will need to be updated before 'sending the request'
            Altitude = 0, // this will need to be updated before 'sending the request'
            CheckForLock = false
        };
        protected CelestialBody localStar;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return; // collecting won't work in editor

            Debug.Log("[KSPI]: RegolithCollector on " + part.name + " was Force Activated");
            this.part.force_activate();

            localStar = GetCurrentStar();

            // this bit goes through parts that contain animations and disables the "Status" field in GUI part window so that it's less crowded
            var MAGlist = part.FindModulesImplementing<ModuleAnimateGeneric>();
            foreach (ModuleAnimateGeneric MAG in MAGlist)
            {
                MAG.Fields["status"].guiActive = false;
                MAG.Fields["status"].guiActiveEditor = false;
            }

            // verify collector was enabled 
            if (!bIsEnabled) return;

            // verify a timestamp is available
            if (dLastActiveTime == 0) return;

            // verify any power was available in previous state
            if (dLastPowerPercentage < 0.01) return;

            // verify vessel is landed, not splashed and not in atmosphere
            if (IsCollectLegal() == false) return;

            // calculate time difference since last time the vessel was active
            double dTimeDifference = (Planetarium.GetUniversalTime() - dLastActiveTime) * 55;

            //bTouchDown = this.part.GroundContact; // is the drill touching the ground?

            // collect regolith for the amount of time that passed since last time (i.e. take care of offline collecting)
            CollectRegolith(dTimeDifference, true);
        }


        public override void OnUpdate()
        {
            Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled

            Fields["strReceivedPower"].guiActive = bIsEnabled;

            /* Regolith concentration doesn't really need to be calculated and updated in gui on every update. 
             * By hiding it behind a counter that only runs this code once per ten cycles, it should be more performance friendly.
            */
            if (++counter % 10 == 0) // increment counter then check if it is the tenth update
            {
                //dConcentrationRegolith = CalculateRegolithConcentration(FlightGlobals.currentMainBody.position, localStar.transform.position, vessel.altitude);
                dConcentrationRegolith = GetFinalConcentration();

                /* If collecting is legal, update the regolith concentration in GUI, otherwise pass a zero string. 
                 * This way we shouldn't get readings when the vessel is flying or splashed or on a planet with an atmosphere.
                 */
                strRegolithConc = IsCollectLegal() ? dConcentrationRegolith.ToString("P0") : "0"; // F1 string format means fixed point number with one decimal place (i.e. number 1234.567 would be formatted as 1234.5). I might change this eventually to P1 or P0 (num multiplied by hundred and percentage sign with 1 or 0 dec. places).
                // Also update the current altitude in GUI
                strAltitude = (vessel.altitude < 15000) ? (vessel.altitude).ToString("F0") : "Too damn high";
            }          
        }

        public override void OnFixedUpdate()
        {
            if (FlightGlobals.fetch != null)
            {
                if (!bIsEnabled)
                {
                    strCollectingStatus = "Disabled";
                    strStarDist = UpdateDistanceInGUI(); // passes the distance to the GUI
                    return;
                }

                // won't collect in atmosphere, while splashed and while flying
                if (IsCollectLegal() == false)
                {
                    DisableCollector();
                    return;
                }
             
                strStarDist = UpdateDistanceInGUI();

                // collect solar wind for a single frame
                CollectRegolith(TimeWarp.fixedDeltaTime, false);

                // store current time in case vesel is unloaded
                dLastActiveTime = (float)Planetarium.GetUniversalTime();

                // store current solar wind concentration in case vessel is unloaded
                //dLastRegolithConcentration = CalculateRegolithConcentration(FlightGlobals.currentMainBody.position, localStar.transform.position, vessel.altitude);
                dLastRegolithConcentration = GetFinalConcentration();

                /* This bit will check if the regolith drill has not lost contact with ground. Raycasts are apparently not all that expensive, but still, 
                 * the counter will delay the check so that it runs only once per hundred cycles. This should be enough and should make it more performance friendly and
                 * also less prone to kraken glitches. It also makes sure that this doesn't run before the vessel is fully loaded and shown to the player.
                 */
                if (++anotherCounter % 100 == 0)
                {
                    bTouchDown = TryRaycastToHitTerrain();
                    if (bTouchDown == false) // if not, disable collecting
                    {
                        ScreenMessages.PostScreenMessage("Regolith drill not in contact with ground. Disabling drill.", 3, ScreenMessageStyle.LOWER_CENTER);
                        DisableCollector();
                        return;
                    }
                }
            }
        }

        /** 
         * This function should allow this module to work in solar systems other than the vanilla KSP one as well. There are some instances where it will fail (systems with a black hole instead of a star etc).
         * It checks current reference body's temperature at 0 altitude. If it is less than 2k K, it checks this body's reference body next and so on.
         */
        protected CelestialBody GetCurrentStar()
        {
            int iDepth = 0;
            var star = FlightGlobals.currentMainBody;
            while ((iDepth < 10) && (star.GetTemperature(0) < 2000))
            {
                star = star.referenceBody;
                iDepth++;
            }
            if ((star.GetTemperature(0) < 2000) || (star.name == "Galactic Core"))
                star = null;

            return star;
        }

        // checks if the vessel is not in atmosphere and if it can therefore collect regolith. Also checks if the vessel is landed and if it is not splashed (not sure if non atmospheric bodies can have oceans in KSP or modded galaxies, let's put this in to be sure)
        private bool IsCollectLegal()
        {
            bool bCanCollect = false;


            if (vessel.checkLanded() == false || vessel.checkSplashed() == true)
            {
                strStarDist = UpdateDistanceInGUI();
                strRegolithConc = "0";
                return bCanCollect;
            }

            else if (FlightGlobals.currentMainBody.atmosphere == true) // won't collect in atmosphere
            {
                strStarDist = UpdateDistanceInGUI();
                strRegolithConc = "0";
                return bCanCollect;
            }
            else
            {
                bCanCollect = true;
                return bCanCollect; // all checks green, ok to collect
            }
        }

        // this snippet returns true if the part is extended
        private bool IsDrillExtended()
        {
            ModuleAnimationGroup thisPartsAnimGroup = this.part.FindModuleImplementing<ModuleAnimationGroup>();
            return thisPartsAnimGroup.isDeployed;
        }

        private bool TryRaycastToHitTerrain()
        {
            Vector3d partPosition = this.part.transform.position; // find the position of the transform in 3d space
            double scaleFactor = this.part.rescaleFactor; // what is the rescale factor of the drill?
            float drillDistance = (float)(5 * scaleFactor); // adjust the distance for the ray with the rescale factor, needs to be a float for raycast. The 5 is just about the reach of the drill.

            RaycastHit hit = new RaycastHit(); // create a variable that stores info about hit colliders etc.
            LayerMask terrainMask = 32768; // layermask in unity, number 1 bitshifted to the left 15 times (1 << 15), (terrain = 15, the bitshift is there so that the mask bits are raised; this is a good reading about that: http://answers.unity3d.com/questions/8715/how-do-i-use-layermasks.html)
            Ray drillPartRay = new Ray(partPosition, -part.transform.up); // this ray will start at the part's center and go down in local space coordinates (Vector3d.down is in world space)

            /* This little bit will fire a ray from the part, straight down, in the distance that the part should be able to reach.
             * It returns true if there is solid terrain in the reach AND the drill is extended. Otherwise false.
             * This is actually needed because stock KSP terrain detection is not really dependable. This module was formerly using just part.GroundContact 
             * to check for contact, but that seems to be bugged somehow, at least when paired with this drill - it works enough times to pass tests, but when testing 
             * this module in a difficult terrain, it just doesn't work properly. 
            */
            Physics.Raycast(drillPartRay, out hit, drillDistance, terrainMask); // use the defined ray, pass info about a hit, go the proper distance and choose the proper layermask 
            if (hit.collider != null)
            {
                if (IsDrillExtended() == true)
                {
                    return true;
                }
            }
            return false;
        }

        private double GetFinalConcentration()
        {
            dFinalConcentration = CalculateRegolithConcentration(FlightGlobals.currentMainBody.position, localStar.transform.position, vessel.altitude);
            dFinalConcentration += AdjustConcentrationForLocation();
            return dFinalConcentration;
        }

        private double AdjustConcentrationForLocation()
        {
            double concentration = 0;
            regolithRequest.BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex;
            regolithRequest.Latitude = vessel.latitude;
            regolithRequest.Longitude = vessel.longitude;
            regolithRequest.Altitude = vessel.altitude;
            concentration = ResourceMap.Instance.GetAbundance(regolithRequest);
            return concentration;
        }

        // calculates regolith concentration - right now just based on the distance of the planet from the sun, so planets will have uniform distribution. We might add latitude as a factor etc.
        private static double CalculateRegolithConcentration(Vector3d planetPosition, Vector3d sunPosition, double altitude)
        {
            double dAvgMunDistance = GameConstants.kerbin_sun_distance; // if my reasoning is correct, this is not only the average distance of Kerbin, but also for the Mun. Maybe this is obvious to everyone else or wrong, but I'm tired, so there.
            
             
             /* I decided to incorporate an altitude modifier. According to https://curator.jsc.nasa.gov/lunar/letss/regolith.pdf, most regolith on Moon is deposited in
             * higher altitudes. This is great from a gameplay perspective, because it makes an incentive for players to collect regolith in more difficult circumstances 
             * (i.e. landing on highlands instead of flats etc.) and breaks the flatter-is-better base building strategy at least a bit.
             * This check will divide current altitude by 2500. At that arbitrarily-chosen altitude, we should be getting the basic concentration for the planet. 
             * Go to a higher terrain and you will find **more** regolith. The + 500 shift is there so that even at altitude of 0 (i.e. Minmus flats etc.) there will
             * still be at least SOME regolith to be mined.
             */
            double dAltModifier = (altitude + 500) / 2500;
            double dConcentration =  dAltModifier * (dAvgMunDistance / (Vector3d.Distance(planetPosition, sunPosition))); // get a basic concentration. Should range from numbers lower than one for planets further away from the sun, to about 2.5 at Moho
            return dConcentration;
        }

        // calculates the distance to sun
        private static double CalculateDistanceToSun(Vector3d vesselPosition, Vector3d sunPosition)
        {
            double dDistance = Vector3d.Distance(vesselPosition, sunPosition);
            return dDistance;
        }

        // helper function for readying the distance for the GUI
        private string UpdateDistanceInGUI()
        {
            string distance = ((CalculateDistanceToSun(part.transform.position, localStar.transform.position) - localStar.Radius) / 1000).ToString("F0") + " km";
            return distance;
        }


        // the main collecting function
        private void CollectRegolith(double deltaTimeInSeconds, bool offlineCollecting)
        {
            //Debug.Log("Inside Collect function.");
            //dConcentrationRegolith = CalculateRegolithConcentration(FlightGlobals.currentMainBody.position, localStar.transform.position, vessel.altitude);
            dConcentrationRegolith = GetFinalConcentration();

            string strRegolithResourceName = InterstellarResourcesConfiguration.Instance.Regolith;
            double dPowerRequirementsMW = PluginHelper.PowerConsumptionMultiplier * mwRequirements; // change the mwRequirements number in part config to change the power consumption

            // gets density of the regolith resource
            dRegolithDensity = (double)(decimal)PartResourceLibrary.Instance.GetDefinition(strRegolithResourceName).density;

            var partsThatContainRegolith = part.GetConnectedResources(strRegolithResourceName);
            dRegolithSpareCapacity = partsThatContainRegolith.Sum(r => r.maxAmount - r.amount);

            if (offlineCollecting)
            {
                dConcentrationRegolith = dLastRegolithConcentration; // if resolving offline collection, pass the saved value, because OnStart doesn't resolve the above function CalculateRegolithConcentration correctly
            }



            if (dConcentrationRegolith > 0 && (dRegolithSpareCapacity > 0))
            {
                // calculate available power
                double dPowerReceivedMW = Math.Max(consumeFNResource(dPowerRequirementsMW * TimeWarp.fixedDeltaTime, ResourceManager.FNRESOURCE_MEGAJOULES, TimeWarp.fixedDeltaTime), 0);
                double dNormalisedRevievedPowerMW = dPowerReceivedMW / TimeWarp.fixedDeltaTime;

                // if power requirement sufficiently low, retreive power from KW source
                if (dPowerRequirementsMW < 2 && dNormalisedRevievedPowerMW <= dPowerRequirementsMW)
                {
                    double dRequiredKW = (dPowerRequirementsMW - dNormalisedRevievedPowerMW) * 1000;
                    double dReceivedKW = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, dRequiredKW * TimeWarp.fixedDeltaTime);
                    dPowerReceivedMW += (dReceivedKW / 1000);
                }

                dLastPowerPercentage = offlineCollecting ? dLastPowerPercentage : (float)(dPowerReceivedMW / dPowerRequirementsMW / TimeWarp.fixedDeltaTime);

                // show in GUI
                strCollectingStatus = "Collecting regolith";
            }

            else
            {
                dLastPowerPercentage = 0;
                dPowerRequirementsMW = 0;
            }

            // set the GUI string to state the number of KWs received if the MW requirements were lower than 2, otherwise in MW
            strReceivedPower = dPowerRequirementsMW < 2
                ? (dLastPowerPercentage * dPowerRequirementsMW * 1000).ToString("0.0") + " KW / " + (dPowerRequirementsMW * 1000).ToString("0.0") + " KW"
                : (dLastPowerPercentage * dPowerRequirementsMW).ToString("0.0") + " MW / " + dPowerRequirementsMW.ToString("0.0") + " MW";
            
            /** The first important bit.
             * This determines how much solar wind will be collected. Can be tweaked in part configs by changing the collector's effectiveness.
             * */

            resourceProduction = dConcentrationRegolith * drillSize * dRegolithDensity * effectiveness * dLastPowerPercentage;

            double dResourceChange = resourceProduction * deltaTimeInSeconds;

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting)
            {
                string strNumberFormat = dResourceChange > 100 ? "0" : "0.00";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("The Regolith Drill collected " + dResourceChange.ToString(strNumberFormat) + " units of " + strRegolithResourceName, 10.0f, ScreenMessageStyle.LOWER_CENTER);
            }

            // this is the second important bit - do the actual change of the resource amount in the vessel
            dResourceFlow = part.RequestResource(strRegolithResourceName, -dResourceChange);

            dResourceFlow = -dResourceFlow / TimeWarp.fixedDeltaTime;

            /* This takes care of wasteheat production (but takes into account if waste heat mechanics weren't disabled).
             * It's affected by two properties of the drill part - its power requirements and waste heat production percentage.
             * More power hungry drills will produce more heat. More effective drills will produce less heat. More effective power hungry drills should produce
             * less heat than less effective power hungry drills. This should allow us to bring some variety into parts, if we want to.
             */
            
            if (!CheatOptions.IgnoreMaxTemperature) // is this player not using no-heat cheat mode?
            {
                dTotalWasteHeatProduction = dPowerRequirementsMW * wasteHeatModifier; // calculate amount of heat to be produced
                supplyFNResourcePerSecond(dTotalWasteHeatProduction, ResourceManager.FNRESOURCE_WASTEHEAT); // push the heat onto them
            }
            
        }

    }
}
