using OpenResourceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Collectors
{
    class RegolithCollector : FNResourceSuppliableModule
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
        //[KSPField(isPersistant = true)]
        //protected bool bIsExtended = false;


        // Part properties
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Drill size", guiUnits = " m\xB3")]
        public double drillSize = 0; // Volume of the collector's drill. Raise in part config (for larger drills) to make collecting faster.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Drill effectiveness", guiFormat = "P1")]
        public double effectiveness = 1.0; // Effectiveness of the drill. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "MW Requirements", guiUnits = " MW")]
        public double mwRequirements = 1.0; // MW requirements of the drill. Affects heat produced.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Waste Heat Modifier", guiFormat = "P1")]
        public double wasteHeatModifier = 1.0; // How much of the power requirements ends up as heat. Change in part cfg, treat as a percentage (1 = 100%). Higher modifier means more energy ends up as waste heat.
        //[KSPField(isPersistant = false)]
        //public string deployAnimName;
        //[KSPField(isPersistant = false)]
        //public string activeAnimName;
        /*
        [KSPField(isPersistant = false)]
        public string deployAnimName;
        [KSPField(isPersistant = false)]
        public string activeAnimName;
        */


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

        // internals
        protected double dResourceFlow = 0;

        [KSPEvent(guiActive = true, guiName = "Activate Drill", active = true)]
        public void ActivateCollector()
        {
            if (IsCollectLegal() == true) // will only be activated if the collecting of resource is legal
            {
                //UpdatePartAnimation();
                bTouchDown = this.part.GroundContact; // Is the drill touching the ground?
                if (bTouchDown == false) // if not, no collecting
                {
                    ScreenMessages.PostScreenMessage("Regolith drill not in contact with ground. Deploy drill fully first.", 3.0f, ScreenMessageStyle.LOWER_CENTER);
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
            /*
            // folding animation will only play if the collector was extended before being disabled
            if (bIsExtended == true)
            {
                UpdatePartAnimation();
            }
            */
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

        const double dKerbinDistance = 13599840256; // distance of Kerbin from Sun/Kerbol in meters (i.e. AU)
        protected double dDistanceFromStar = 0; // distance of the current vessel from the system's star
        protected double dConcentrationRegolith = 0;
        protected double dRegolithSpareCapacity = 0;
        protected double dRegolithDensity;
        protected double dTotalWasteHeatProduction = 0;
        protected double dAltitude = 0;
        protected Animation anim;
        protected bool bChangeState = false;
        protected bool bTouchDown = false;
        uint counter = 0; // counter for update cycles, so that we can only do some calculations once in a while
        uint anotherCounter = 0; // counter for fixedupdate cycles, so that we can only do some calculations once in a while (I don't want to add complexity by using the previous counter in two places)

        protected CelestialBody localStar;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return; // collecting won't work in editor

            this.part.force_activate();

            localStar = GetCurrentStar();

            /*
            // get the part's animation
            anim = part.FindModelAnimators(deployAnimName)[0];
            anim = part.FindModelAnimators(activeAnimName)[0];
            */

            // this bit goes through parts that contain animations and disables the "Status" field in GUI part window so that it's less crowded
            List<ModuleAnimateGeneric> MAGlist = part.FindModulesImplementing<ModuleAnimateGeneric>();
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

            /*
            // if the part should be extended (from last time), go to the extended animation
            if (bIsExtended == true && anim != null)
            {
                anim[deployAnimName].normalizedTime = 1f;
            }
            */

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
                dConcentrationRegolith = CalculateRegolithConcentration(FlightGlobals.currentMainBody.position, localStar.transform.position, vessel.altitude); 

                /* If collecting is legal, update the regolith concentration in GUI, otherwise pass a zero string. 
                 * This way we shouldn't get readings when the vessel is flying or splashed or on a planet with an atmosphere.
                 */
                strRegolithConc = IsCollectLegal() ? dConcentrationRegolith.ToString("F1") : "0"; // F1 string format means fixed point number with one decimal place (i.e. number 1234.567 would be formatted as 1234.5). I might change this eventually to P1 or P0 (num multiplied by hundred and percentage sign with 1 or 0 dec. places).
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
                dLastRegolithConcentration = CalculateRegolithConcentration(FlightGlobals.currentMainBody.position, localStar.transform.position, vessel.altitude);

                /* This bit will check if the regolith drill has not been retracted by the player while still running. The counter will 
                 * delay the check so that it runs only once per hundred cycles. This should be enough and should make it more performance friendly and
                 * also less prone to kraken glitches. It also makes sure that this doesn't run before the vessel is fully loaded and shown to the player.
                 * Like wtf, Squad/Unity? Why is FixedUpdate running while the player is still looking at the loading screen, waiting for the vessel to load?
                 */
                if (++anotherCounter % 100 == 0)
                {
                    bTouchDown = this.part.GroundContact; // Is the drill touching the ground?
                    if (bTouchDown == false) // if not, disable collecting
                    {
                        ScreenMessages.PostScreenMessage("Regolith drill not in contact with ground. Disabling drill.", 3.0f, ScreenMessageStyle.LOWER_CENTER);
                        DisableCollector();
                        return;
                    }
                }

            }
        }

        /** 
         * This function should allow this module to work in solar systems other than the vanilla KSP one as well. There are some instance where it will fail (systems with a black hole instead of a star etc).
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
                // ScreenMessages.PostScreenMessage("Regolith collection not possible, vessel is not properly landed", 10.0f, ScreenMessageStyle.LOWER_CENTER);
                strStarDist = UpdateDistanceInGUI();
                strRegolithConc = "0";
                return bCanCollect;
            }

            else if (FlightGlobals.currentMainBody.atmosphere == true) // won't collect in atmosphere
            {
                // ScreenMessages.PostScreenMessage("Regolith collection not possible in atmosphere", 10.0f, ScreenMessageStyle.LOWER_CENTER);
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


        //private bool ToggleBool()
        //{
        //    if (bIsExtended == true)
        //    {
        //        return bIsExtended = false;
        //    }
        //    else
        //        return bIsExtended = true;
        //}

        /*
        private void UpdatePartAnimation() 
        {
            // if folded, plays the part extending animation
            if (!bIsExtended)
            {
                
                if (anim != null)
                {
                    anim[deployAnimName].speed = 1f;
                    anim[deployAnimName].normalizedTime = 0f; // normalizedTime at 0 is the start of the animation
                    anim.Blend(deployAnimName, part.mass);
                    //StartCoroutine(PlayAnimAndWaitForIt(anim[animName].length)); 
                    //yield return new WaitForSeconds(anim[animName].length); // wait for the length of the animation
                    //Invoke("ToggleBool", anim[deployAnimName].length); // will invoke ToggleBool after the animation has been played completely
                }
                //else
                //ToggleBool();

                // PlayAnimAndWaitForIt(animName, 1f, 0f);
                ToggleBool();
                //bIsExtended = true;
                return;
            }

            // if extended, plays the part folding animation
            if (bIsExtended)
            {

                if (anim != null)
                {
                    anim[deployAnimName].speed = -1f; // speed of 1 is normal playback, -1 is reverse playback (so in this case we go from the end of animation backwards)
                    anim[deployAnimName].normalizedTime = 1f; // normalizedTime at 1 is the end of the animation
                    anim.Blend(deployAnimName, part.mass);
                    //StartCoroutine(WaitForAnim(anim[animName].length)); // wait for the length of the animation
                    //yield return new WaitForSeconds(anim[animName].length);
                    //Invoke("ToggleBool", anim[deployAnimName].length); // will invoke ToggleBool after the animation has been played completely
                }
                //else
                //ToggleBool();
                // PlayAnimAndWaitForIt(animName, -1f, 0f);
                ToggleBool();
                //bIsExtended = false;
                return;
            }
            return;
        }
        */
        // calculates regolith concentration - right now just based on the distance of the planet from the sun, so planets will have uniform distribution. We might add latitude as a factor etc.
        private static double CalculateRegolithConcentration(Vector3d planetPosition, Vector3d sunPosition, double altitude)
        {
            double dAvgMunDistance = 13599840256; // if my reasoning is correct, this is not only the average distance of Kerbin, but also for the Mun. Maybe this is obvious to everyone else or wrong, but I'm tired, so there.
            /* I decided to incorporate an altitude modifier. According to https://curator.jsc.nasa.gov/lunar/letss/regolith.pdf, most regolith on Moon is deposited in
             * higher altitudes. This is great from a gameplay perspective, because it makes an incentive for players to collect regolith in more difficult circumstances 
             * (i.e. landing on highlands instead of flats etc.) and breaks the flatter-is-better base building strategy at least a bit.
             * This check will divide current altitude by 2500. At that arbitrarily-chosen altitude, we should be getting the basic concentration for the planet. 
             * Go to a higher terrain and you will find **more** regolith. The + 500 shift is there so that even at altitude of 0 (i.e. Minmus flats etc.) there will
             * still be at least SOME regolith to be mined.
             */
            double dAltModifier = (altitude + 500.0) / 2500.0;
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
            dConcentrationRegolith = CalculateRegolithConcentration(FlightGlobals.currentMainBody.position, localStar.transform.position, vessel.altitude);

            string strRegolithResourceName = InterstellarResourcesConfiguration.Instance.Regolith;
            double dPowerRequirementsMW = (double)PluginHelper.PowerConsumptionMultiplier * mwRequirements; // change the mwRequirements number in part config to change the power consumption

            // gets density of the regolith resource
            dRegolithDensity = PartResourceLibrary.Instance.GetDefinition(strRegolithResourceName).density;

            //Debug.LogFormat("ResourceName {0}, PowerReqs {1}", strRegolithResourceName, dPowerRequirementsMW);
            // checks for free space in regolith tanks FIX THIS
            /*
            * dRegolithSpareCapacity = this.part.GetResourceSpareCapacity(strRegolithResourceName);
            * Debug.LogFormat("First assignement. dRegoSpareCap = {0}", dRegolithSpareCapacity);
            */
            var partsThatContainRegolith = part.GetConnectedResources(strRegolithResourceName);
            dRegolithSpareCapacity = partsThatContainRegolith.Sum(r => r.maxAmount - r.amount);
            //Debug.LogFormat("Second assignment. dRegoSpareCap = {0}", dRegolithSpareCapacity);

            //dRegolithSpareCapacity = getSpareResourceCapacity(strRegolithResourceName);
            //Debug.LogFormat("Third assignment. dRegoSpareCap = {0}", dRegolithSpareCapacity);


            //Debug.LogFormat("Concentration before offline check {0}", dConcentrationRegolith);
            if (offlineCollecting)
            {
                dConcentrationRegolith = dLastRegolithConcentration; // if resolving offline collection, pass the saved value, because OnStart doesn't resolve the above function CalculateRegolithConcentration correctly
            }



            //Debug.LogFormat("After offline check: dConcentrationRegolith is {0}, dRegolithSpareCapacity is {1}", dConcentrationRegolith, dRegolithSpareCapacity);
            if (dConcentrationRegolith > 0 && (dRegolithSpareCapacity > 0))
            {
                //Debug.Log("Inside power changing");
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
            
            //Debug.LogFormat("BIB. dConcentration = {0}, drillSize = {1}, dRegDens = {2}, effect = {3}, dLastPower = {4}, deltaTimeInSecs = {5}.", dConcentrationRegolith, drillSize, dRegolithDensity, effectiveness, dLastPowerPercentage, deltaTimeInSeconds);
            /** The first important bit.
             * This determines how much solar wind will be collected. Can be tweaked in part configs by changing the collector's effectiveness.
             * */
            double dResourceChange = (dConcentrationRegolith * drillSize * dRegolithDensity) * effectiveness * dLastPowerPercentage * deltaTimeInSeconds;

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting)
            {
                string strNumberFormat = dResourceChange > 100 ? "0" : "0.00";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("The Solar Wind Collector collected " + dResourceChange.ToString(strNumberFormat) + " units of " + strRegolithResourceName, 10.0f, ScreenMessageStyle.LOWER_CENTER);
            }

            // this is the second important bit - do the actual change of the resource amount in the vessel
            dResourceFlow = (float)ORSHelper.fixedRequestResource(part, strRegolithResourceName, -dResourceChange);
            dResourceFlow = -dResourceFlow / TimeWarp.fixedDeltaTime;

            /* This takes care of wasteheat production (but takes into account if waste heat mechanics weren't disabled).
             * It's affected by two properties of the drill part - its power requirements and waste heat production percentage.
             * More power hungry drills will produce more heat. More effective drills will produce less heat. More effective power hungry drills should produce
             * less heat than less effective power hungry drills. This should allow us to bring some variety into parts, if we want to.
             */
            
            if (!CheatOptions.IgnoreMaxTemperature) // is this player not using no-heat cheat mode?
            {
                dTotalWasteHeatProduction = dPowerRequirementsMW * wasteHeatModifier; // calculate amount of heat to be produced
                supplyFNResource(dTotalWasteHeatProduction * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT); // push the heat onto them
            }
            
        }

    }
}
