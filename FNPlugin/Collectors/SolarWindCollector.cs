using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class SolarWindCollector : ResourceSuppliableModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool bIsEnabled = false;
        [KSPField(isPersistant = true)]
        public double dLastActiveTime;
        [KSPField(isPersistant = true)]
        public double dLastPowerPercentage;
        [KSPField(isPersistant = true)]
        public double dLastMagnetoStrength;
        [KSPField(isPersistant = true)]
        public double dLastSolarConcentration;
        [KSPField(isPersistant = true)]
        protected bool bIsExtended = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Ionization"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        protected bool bIonizing = false;

        // Part properties
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Surface area", guiUnits = " m\xB2")]
        public double surfaceArea = 0; // Surface area of the panel.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Collector effectiveness", guiFormat = "P1")]
        public double effectiveness = 1; // Effectiveness of the panel. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Magnetic Power Requirements", guiUnits = " MW")]
        public double mwRequirements = 1; // MW requirements of the collector panel.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Ionisation Power Requirements", guiUnits = " MW")]
        public double ionRequirements = 100; // MW requirements of the collector panel.
        [KSPField(isPersistant = false)]
        public string animName = "";
        [KSPField(isPersistant = false)]
        public string ionAnimName = "";
        [KSPField(isPersistant = false)]
        public double solarCheatMultiplier = 1000;  // Amount of boosted Solar wind activity
        [KSPField(isPersistant = false)]
        public double collectMultiplier = 1; 


        // GUI
        [KSPField(guiActive = true, guiName = "Solar Wind Concentration", guiUnits = " mol/m\xB3")]
        protected float solarWindConcentration;
        [KSPField(guiActive = true, guiName = "Distance from the sun")]
        protected string strStarDist = "";
        [KSPField(guiActive = true, guiName = "Status")]
        protected string strCollectingStatus = "";
        [KSPField(guiActive = true, guiName = "Power Usage")]
        protected string strReceivedPower = "";
        [KSPField(guiActive = true, guiName = "Magnetosphere shielding effect", guiUnits = " %")]
        protected string strMagnetoStrength = "";

        // internals
        protected double dResourceFlow = 0;

        [KSPEvent(guiActive = true, guiName = "Activate Collector", active = true)]
        public void ActivateCollector()
        {
            this.part.force_activate();
            bIsEnabled = true;
            OnUpdate();
            if (IsCollectLegal() == true)
            {
                UpdatePartAnimation();
            }
        }

        [KSPEvent(guiActive = true, guiName = "Disable Collector", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnUpdate();
            // folding nimation will only play if the collector was extended before being disabled
            if (bIsExtended == true)
            {
                UpdatePartAnimation();
            }

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

        double molarMassConcentrationPerSquareMeterPerSecond = 0;
        double dSolarWindSpareCapacity;
        double dSolarWindDensity;
        bool bPreviousIonizingState;
        double dMagnetoSphereStrengthRatio = 0;
        double dShieldedEffectiveness = 0;
        string strSolarWindResourceName;

        Animation deployAnimation;
        Animation ionisationAnimation;
        CelestialBody localStar;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return; // collecting won't work in editor

            //this.part.force_activate();

            localStar = GetCurrentStar();

            // get resource name solar wind
            strSolarWindResourceName = InterstellarResourcesConfiguration.Instance.SolarWind;

            // gets density of the solar wind resource
            dSolarWindDensity = PartResourceLibrary.Instance.GetDefinition(strSolarWindResourceName).density;

            // get the part's animation
            deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            ionisationAnimation = part.FindModelAnimators(ionAnimName).FirstOrDefault();

            // this bit goes through parts that contain animations and disables the "Status" field in GUI so that it's less crowded
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

            // verify altitude is not too low
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)))
            {
                ScreenMessages.PostScreenMessage("Solar Wind Collection Error, vessel in atmosphere", 10, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // if the part should be extended (from last time), go to the extended animation
            if (bIsExtended && deployAnimation != null)
            {
                deployAnimation[animName].normalizedTime = 1;
            }

            //if (bIonizing && ionisationAnimation != null)
            //{
            //    ionisationAnimation[ionAnimName].normalizedTime = 1;
            //    ionisationAnimation.Sample();
            //}
            //bPreviousIonizingState = bIonizing;

            // calculate time difference since last time the vessel was active
            double dTimeDifference = (Planetarium.GetUniversalTime() - dLastActiveTime) * 55;

            // collect solar wind for entire duration
            CollectSolarWind(dTimeDifference, true);
        }


        public override void OnUpdate()
        {
            Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled

            Fields["strReceivedPower"].guiActive = bIsEnabled;

            UpdateIonisationAnimation();

            var dSolarWindConcentration = CalculateSolarwindIonConcentration(part.vessel.solarFlux, solarCheatMultiplier);
            solarWindConcentration = (float)dSolarWindConcentration;
            var InterstellarHydrogenConcentration = CalculateInterstellarIonConcentration(part.vessel.speed);

            molarMassConcentrationPerSquareMeterPerSecond = dSolarWindConcentration + InterstellarHydrogenConcentration;

            dMagnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));
            strMagnetoStrength = UpdateMagnetoStrengthInGUI();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (FlightGlobals.fetch != null)
            {
                if (!bIsEnabled)
                {
                    strCollectingStatus = "Disabled";
                    strStarDist = UpdateDistanceInGUI(); // passes the distance to the GUI
                    return;
                }

                // won't collect in atmosphere
                if (IsCollectLegal() == false)
                {
                    DisableCollector();
                    return;
                }

                strStarDist = UpdateDistanceInGUI();

                // collect solar wind for a single frame
                CollectSolarWind(TimeWarp.fixedDeltaTime, false);

                // store current time in case vesel is unloaded
                dLastActiveTime = Planetarium.GetUniversalTime();
                
                // store current strength of the magnetic field in case vessel is unloaded
                dLastMagnetoStrength = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

                // store current solar wind concentration in case vessel is unloaded
                dLastSolarConcentration = molarMassConcentrationPerSquareMeterPerSecond; //CalculateSolarWindConcentration(part.vessel.solarFlux);
            }
        }

        /** 
         * This function should allow this module to work in solar systems other than the vanilla KSP one as well. Credit to Freethinker's MicrowavePowerReceiver code.
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

        /* Calculates the strength of the magnetosphere. Will return 1 if in atmosphere, otherwise a ratio of max atmospheric altitude to current 
         * altitude - so the ratio slowly lowers the higher the vessel is. Once above 10 times the max atmo altitude, 
         * it returns 0 (we consider this to be the end of the magnetosphere's reach). The atmospheric check is there to make the GUI less messy.
        */
        private static double GetMagnetosphereRatio(double altitude, double maxatmoaltitude)
        {
            double dRatio; // helper double for this function

            // atmospheric check for the sake of GUI
            if (altitude <= maxatmoaltitude)
            {
                dRatio = 1;
                return dRatio;
            }
            else
                dRatio = (altitude < (maxatmoaltitude * 10)) ? maxatmoaltitude / altitude : 0;
            return dRatio;
        }

        // checks if the vessel is not in atmosphere and if it can therefore collect solar wind. Could incorporate other checks if needed.
        private bool IsCollectLegal()
        {
            bool bCanCollect = false;

            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody))) // won't collect in atmosphere
            {
                ScreenMessages.PostScreenMessage("Solar wind collection not possible in atmosphere", 10, ScreenMessageStyle.LOWER_CENTER);
                strStarDist = UpdateDistanceInGUI();
                solarWindConcentration = 0;
                return bCanCollect;
            }
            else
                return bCanCollect = true;
        }

        private void UpdateIonisationAnimation()
        {
            if (bIonizing == bPreviousIonizingState)
                return;

            if (bPreviousIonizingState)
            {
                bPreviousIonizingState = false;
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = -1; // speed of 1 is normal playback, -1 is reverse playback (so in this case we go from the end of animation backwards)
                    ionisationAnimation[ionAnimName].normalizedTime = 1; // normalizedTime at 1 is the end of the animation
                    ionisationAnimation.Blend(ionAnimName, part.mass);
                }
            }
            else
            {
                bPreviousIonizingState = true;
                // if folded, plays the part extending animation
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 1;
                    ionisationAnimation[ionAnimName].normalizedTime = 0; // normalizedTime at 0 is the start of the animation
                    ionisationAnimation.Blend(ionAnimName, part.mass);
                }
            }
        }

        private void UpdatePartAnimation()
        {
            // if extended, plays the part folding animation
            if (bIsExtended)
            {
                if (deployAnimation != null)
                {
                    deployAnimation[animName].speed = -1; // speed of 1 is normal playback, -1 is reverse playback (so in this case we go from the end of animation backwards)
                    deployAnimation[animName].normalizedTime = 1; // normalizedTime at 1 is the end of the animation
                    deployAnimation.Blend(animName, part.mass);
                }
                bIsExtended = false;
            }
            else
            {
                // if folded, plays the part extending animation
                if (deployAnimation != null)
                {
                    deployAnimation[animName].speed = 1;
                    deployAnimation[animName].normalizedTime = 0; // normalizedTime at 0 is the start of the animation
                    deployAnimation.Blend(animName, part.mass);
                }
                bIsExtended = true;
            }
        }

        // calculates solar wind concentration
        private static double CalculateSolarwindIonConcentration(double flux, double solarCheatMultiplier)
        {
            var dAvgKerbinSolarFlux = 1409.285; // this seems to be the average flux at Kerbin just above the atmosphere (from my tests)
            var dAvgSolarWindPerCubM = 6 * 1000000; // various sources differ, most state that there are around 6 particles per cm^3, so around 6000000 per m^3 (some sources go up to 10/cm^3 or even down to 2/cm^3, most are around 6/cm^3).
            

            var solarWindSpeed = 500000; // Average Solar win speed 500 km/s
            var avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol 

            double dMolalSolarConcentration = (flux / dAvgKerbinSolarFlux) * dAvgSolarWindPerCubM * solarWindSpeed * solarCheatMultiplier / avogadroConstant;

            return dMolalSolarConcentration; // in mol / m2 / sec
        }

        private static double CalculateInterstellarIonConcentration(double vesselSpeed)
        {
            var dAverageInterstellarHydrogenPerCubM = 1 * 1000000;
            var avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol

            var interstellarHydrogenConcentration = dAverageInterstellarHydrogenPerCubM * vesselSpeed / avogadroConstant;

            return interstellarHydrogenConcentration; // in mol / m2 / sec
        }

        // calculates the distance to sun
        private static double CalculateDistanceToSun(Vector3d vesselPosition, Vector3d sunPosition)
        {
            return Vector3d.Distance(vesselPosition, sunPosition);
        }

        // helper function for readying the distance for the GUI
        private string UpdateDistanceInGUI()
        {
            return ((CalculateDistanceToSun(part.transform.position, localStar.transform.position) - localStar.Radius) / 1000).ToString("F0") + " km";
        }

        private string UpdateMagnetoStrengthInGUI()
        {
            return (GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody))* 100).ToString("F1");
        }

        // the main collecting function
        private void CollectSolarWind(double deltaTimeInSeconds, bool offlineCollecting)
        {
            var ionizationPowerCost = bIonizing ? ionRequirements : 0;

            var dPowerRequirementsMW = PluginHelper.PowerConsumptionMultiplier * (mwRequirements + ionizationPowerCost); // change the mwRequirements number in part config to change the power consumption

            // checks for free space in solar wind 'tanks'
            dSolarWindSpareCapacity = part.GetResourceSpareCapacity(strSolarWindResourceName);

            if (offlineCollecting)
                molarMassConcentrationPerSquareMeterPerSecond = dLastSolarConcentration; // if resolving offline collection, pass the saved value, because OnStart doesn't resolve the function at line 328

            if (molarMassConcentrationPerSquareMeterPerSecond > 0 && (dSolarWindSpareCapacity > 0))
            {
                // calculate available power
                var dNormalisedRevievedPowerMW = Math.Max(consumeFNResourcePerSecond(dPowerRequirementsMW, ResourceManager.FNRESOURCE_MEGAJOULES), 0);

                // if power requirement sufficiently low, retreive power from KW source
                if (dPowerRequirementsMW < 2 && dNormalisedRevievedPowerMW <= dPowerRequirementsMW)
                {
                    var dRequiredKW = (dPowerRequirementsMW - dNormalisedRevievedPowerMW) * 1000;
                    var dReceivedKW = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, dRequiredKW * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                    dNormalisedRevievedPowerMW += (dReceivedKW / 1000);
                }

                dLastPowerPercentage = offlineCollecting ? dLastPowerPercentage : (dNormalisedRevievedPowerMW / dPowerRequirementsMW);

                // show in GUI
                strCollectingStatus = "Collecting solar wind";
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

            // get the shielding effect provided by the magnetosphere
            dMagnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

            // if online collecting, get the old values instead (simplification for the time being)
            if (offlineCollecting)
                dMagnetoSphereStrengthRatio = dLastMagnetoStrength;

            if (dMagnetoSphereStrengthRatio == 0)
                dShieldedEffectiveness = 1;
            else
                dShieldedEffectiveness = (1 - dMagnetoSphereStrengthRatio);
            /** The first important bit.
             * This determines how much solar wind will be collected. Can be tweaked in part configs by changing the collector's effectiveness.
             * */
            double dResourceChange = (molarMassConcentrationPerSquareMeterPerSecond / 1000000 * collectMultiplier * surfaceArea / dSolarWindDensity) * effectiveness * dShieldedEffectiveness * dLastPowerPercentage * deltaTimeInSeconds;

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting)
            {
                string strNumberFormat = dResourceChange > 100 ? "0" : "0.00";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("The Solar Wind Collector collected " + dResourceChange.ToString(strNumberFormat) + " units of " + strSolarWindResourceName, 10.0f, ScreenMessageStyle.LOWER_CENTER);
            }

            // this is the second important bit - do the actual change of the resource amount in the vessel
            dResourceFlow = -part.RequestResource(strSolarWindResourceName, -dResourceChange) / TimeWarp.fixedDeltaTime;
        }

    }
}

