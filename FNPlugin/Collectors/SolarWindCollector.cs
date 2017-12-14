using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin
{
    class SolarWindCollector : ResourceSuppliableModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool bIsEnabled = false;
        [KSPField(isPersistant = true)]
        public double dLastActiveTime;
        [KSPField(isPersistant = true, guiActive = true,  guiName = "Power Ratio")]
        public double dLastPowerPercentage;
        [KSPField(isPersistant = true)]
        public double dLastMagnetoStrength;
        [KSPField(isPersistant = true)]
        public double dLastSolarConcentration;
        [KSPField(isPersistant = true)]
        public double dLastHydrogenConcentration;
        [KSPField(isPersistant = true)]
        protected bool bIsExtended = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Ionizing"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        protected bool bIonizing = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        protected float powerPercentage = 100;

        // Part properties
        [KSPField(guiActiveEditor = false, guiName = "Surface area", guiUnits = " km\xB2")]
        public double surfaceArea = 0; // Surface area of the panel.
        [KSPField(guiActiveEditor = true, guiName = "Magnetic area", guiUnits = " m\xB2")]
        public double magneticArea = 0; // Surface area of the panel.
        [KSPField(guiActiveEditor = true, guiName = "Collector effectiveness", guiFormat = "P1")]
        public double effectiveness = 1; // Effectiveness of the panel. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(guiActiveEditor = true, guiName = "Magnetic Power Requirements", guiUnits = " MW")]
        public double mwRequirements = 1; // MW requirements of the collector panel.
        [KSPField(guiActiveEditor = true, guiName = "Ionisation Power Requirements", guiUnits = " MW")]
        public double ionRequirements = 100; // MW requirements of the collector panel.

        [KSPField]
        public double squareVelocityDragRatio = 0.075;
        [KSPField]
        public double atmosphereIonRatio = 0.001;
        [KSPField] 
        public double heliumRequirement = 0.2;
        [KSPField]
        public string animName = "";
        [KSPField]
        public string ionAnimName = "";
        [KSPField]
        public double solarCheatMultiplier = 1;             // Amount of boosted Solar wind activity
        [KSPField]
        public double interstellarCheatMultiplier = 250;   // Amount of boosted Interstellar hydrogen activity
        [KSPField]
        public double collectMultiplier = 1;
        [KSPField]
        public double solarWindSpeed = 500000;              // Average Solar win speed 500 km/s

        // GUI
        [KSPField(guiActive = true, guiName = "Effective Surface Area", guiFormat = "F3", guiUnits = " km\xB2")]
        protected double effectiveSurfaceAreaInSquareKiloMeter;
        [KSPField(guiActive = true, guiName = "Effective Diamter", guiFormat = "F3", guiUnits = " km")]
        protected double effectiveDiamterInKilometer;
        [KSPField(guiActive = true, guiName = "Solar Wind Ions", guiUnits = " mol/m\xB2/s")]
        protected float fSolarWindConcentrationPerSquareMeter;
        [KSPField(guiActive = true, guiName = "Interstellar Ions", guiUnits = " mol/m\xB2/s")] 
        protected float fInterstellarIonsConcentrationPerSquareMeter;
        [KSPField(guiActive = true, guiName = "Interstellar Particles", guiUnits = " mol/m\xB3")]
        protected float fInterstellarIonsConcentrationPerCubicMeter;
        [KSPField(guiActive = true, guiName = "Atmosphere Particles", guiUnits = " mol/m\xB3")]
        protected float fAtmosphereConcentration;
        [KSPField(guiActive = true, guiName = "Neutral Atmospheric H", guiUnits = " mol/m\xB3")]
        protected float fNeutralHydrogenConcentration;
        [KSPField(guiActive = true, guiName = "Neutral Atmospheric He", guiUnits = " mol/m\xB3")]
        protected float fNeutralHeliumConcentration;
        [KSPField(guiActive = true, guiName = "Ionized Atmospheric H", guiUnits = " mol/m\xB3")]
        protected float fIonizedHydrogenConcentration;
        [KSPField(guiActive = true, guiName = "Ionized Atmospheric He", guiUnits = " mol/m\xB3")]
        protected float fIonizedHeliumConcentration;

        [KSPField(guiActive = true, guiName = "Atmospheric Density", guiUnits = " kg/m\xB3")]
        protected float fAtmosphereIonsKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "Interstellar Ions Density", guiUnits = " kg/m\xB3")]
        protected float fInterstellarIonsKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "Solarwind Mass Density", guiUnits = " kg/m\xB2")]
        protected float fSolarWindKgPerSquareMeter;

        [KSPField(guiActive = true, guiName = "Atmospheric Drag", guiUnits = " N/m\xB2")]
        protected float fAtmosphericDragInNewton;
        [KSPField(guiActive = true, guiName = "Solarwind Drag", guiUnits = " N/m\xB2")]
        protected float fSolarWindDragInNewtonPerSquareMeter;
        [KSPField(guiActive = true, guiName = "Interstellar Drag", guiUnits = " N/m\xB2")]
        protected float fInterstellarDustDragInNewton;

        [KSPField(guiActive = true, guiName = "Max Orbital Drag", guiUnits = " kN")]
        protected float fMaxOrbitalVesselDragInKiloNewton;
        [KSPField(guiActive = true, guiName = "Orbital Drag on Vessel", guiUnits = " kN")]
        protected float fEffectiveOrbitalVesselDragInKiloNewton;
        [KSPField(guiActive = true, guiName = "Solarwind Force on Vessel", guiUnits = " N")]
        protected float fSolarWindVesselForceInNewton;
        [KSPField(guiActive = true, guiName = "Magneto Sphere Strength Ratio", guiFormat = "F3")]
        protected double magnetoSphereStrengthRatio;
        [KSPField(guiActive = true, guiName = "Solarwind Facing Factor", guiFormat = "F3")]
        protected double solarWindAngleOfAttackRatio = 0;
        [KSPField(guiActive = true, guiName = "Solarwind Collection Modifier", guiFormat = "F3")]
        protected double solarwindProductionModifiers = 0;
        [KSPField(guiActive = true, guiName = "SolarWind Mass Collected", guiUnits = " g/h")]
        protected float fSolarWindCollectedGramPerHour;
        [KSPField(guiActive = true, guiName = "Interstellar Mass Collected", guiUnits = " g/h")]
        protected float fInterstellarIonsCollectedGramPerHour;
        [KSPField(guiActive = true, guiName = "Atm Hydrogen Mass Collected", guiUnits = " g/h")]
        protected float fHydrogenCollectedGramPerHour;
        [KSPField(guiActive = true, guiName = "Atm Helium Mass Collected", guiUnits = " g/h")]
        protected float fHeliumCollectedGramPerHour;

        [KSPField(guiActive = true, guiName = "Distance from the sun", guiFormat = "F1",  guiUnits = " km")]
        protected double distanceToLocalStar;
        [KSPField(guiActive = true, guiName = "Status")]
        protected string strCollectingStatus = "";
        [KSPField(guiActive = true, guiName = "Power Usage")]
        protected string strReceivedPower = "";
        [KSPField(guiActive = true, guiName = "Magnetosphere shielding effect", guiUnits = " %")]
        protected string strMagnetoStrength = "";

        // internals
        double effectiveSurfaceAreaInSquareMeter;
        double dWindResourceFlow = 0;
        double dHydrogenResourceFlow = 0;
	    double dHeliumResourceFlow = 0;
        double heliumRequirementTonPerSecond;
        double solarWindMolesPerSquareMeterPerSecond = 0;
        double interstellarDustMolesPerCubicMeter = 0;
        double hydrogenMolarMassConcentrationPerSquareMeterPerSecond = 0;
        double heliumMolarMassConcentrationPerSquareMeterPerSecond = 0;
        double dSolarWindSpareCapacity;
        double dHydrogenSpareCapacity;
        double dShieldedEffectiveness = 0;

        float newNormalTime;
        float previousPowerPercentage;

        bool previosIonisationState = false;

        Animation deployAnimation;
        Animation ionisationAnimation;
        CelestialBody localStar;

        PartResourceDefinition helium4GasResourceDefinition;
        PartResourceDefinition lqdHelium4ResourceDefinition;
        PartResourceDefinition hydrogenResourceDefinition;
		PartResourceDefinition solarWindResourceDefinition;

        [KSPEvent(guiActive = true, guiName = "Activate Collector", active = true)]
        public void ActivateCollector()
        {
            this.part.force_activate();
            bIsEnabled = true;
            OnUpdate();
            if (IsCollectLegal())
                UpdatePartAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Disable Collector", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnUpdate();
            // folding nimation will only play if the collector was extended before being disabled
            if (bIsExtended == true)
                UpdatePartAnimation();
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

        public override void OnStart(PartModule.StartState state)
        {
            // get the part's animation
            deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            ionisationAnimation = part.FindModelAnimators(ionAnimName).FirstOrDefault();
            previousPowerPercentage = powerPercentage;
            previosIonisationState = bIonizing;
            if (ionisationAnimation != null)
            {
                ionisationAnimation[ionAnimName].speed = 0;
                ionisationAnimation[ionAnimName].normalizedTime = bIonizing ? powerPercentage / 100 : 0; // normalizedTime at 1 is the end of the animation
                ionisationAnimation.Blend(ionAnimName);
            }

            if (state == StartState.Editor) return; // collecting won't work in editor

            heliumRequirementTonPerSecond = heliumRequirement * 1e-6 / GameConstants.SECONDS_IN_HOUR ;
            helium4GasResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Helium4Gas);
            lqdHelium4ResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdHelium4);
            solarWindResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.SolarWind);
            hydrogenResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen);

            localStar = GetCurrentStar();

            // this bit goes through parts that contain animations and disables the "Status" field in GUI so that it's less crowded
            var maGlist = part.FindModulesImplementing<ModuleAnimateGeneric>();
            foreach (ModuleAnimateGeneric mag in maGlist)
            {
                mag.Fields["status"].guiActive = false;
                mag.Fields["status"].guiActiveEditor = false;
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

            // calculate time difference since last time the vessel was active
            var dTimeDifference = (Planetarium.GetUniversalTime() - dLastActiveTime) * 55;

            // collect solar wind for entire duration
            CollectSolarWind(dTimeDifference, true);
        }

        public override void OnUpdate()
        {
            Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled
            Fields["strReceivedPower"].guiActive = bIsEnabled;           

            solarWindMolesPerSquareMeterPerSecond = CalculateSolarwindIonConcentration(part.vessel.solarFlux, solarCheatMultiplier, solarWindSpeed);
            interstellarDustMolesPerCubicMeter = CalculateInterstellarIonConcentration(interstellarCheatMultiplier);

            var dAtmosphereConcentration = CalculateCurrentAtmosphereConcentration(vessel);

            var dHydrogenParticleConcentration = CalculateCurrentHydrogenParticleConcentration(vessel);
            var dHeliumParticleConcentration = CalculateCurrentHeliumParticleConcentration(vessel);

            var dIonizedHydrogenConcentration = CalculateCurrentHydrogenIonsConcentration(vessel);
			var dIonizedHeliumConcentration = CalculateCurrentHeliumIonsConcentration(vessel);

            hydrogenMolarMassConcentrationPerSquareMeterPerSecond = bIonizing ? dHydrogenParticleConcentration : dIonizedHydrogenConcentration;
            heliumMolarMassConcentrationPerSquareMeterPerSecond = bIonizing ? dHeliumParticleConcentration: dIonizedHeliumConcentration;

            fSolarWindConcentrationPerSquareMeter = (float)solarWindMolesPerSquareMeterPerSecond;
            fInterstellarIonsConcentrationPerCubicMeter = (float)interstellarDustMolesPerCubicMeter;
            fAtmosphereConcentration = (float)dAtmosphereConcentration;
            fNeutralHydrogenConcentration = (float)dHydrogenParticleConcentration;
            fNeutralHeliumConcentration = (float)dHeliumParticleConcentration;

            fIonizedHydrogenConcentration = (float)dIonizedHydrogenConcentration;
            fIonizedHeliumConcentration = (float)dIonizedHeliumConcentration;

            magnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));
            strMagnetoStrength = UpdateMagnetoStrengthInGui();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            UpdateIonisationAnimation();

            if (FlightGlobals.fetch != null)
            {
                if (!bIsEnabled)
                {
                    strCollectingStatus = "Disabled";
                    distanceToLocalStar = UpdateDistanceInGui(); // passes the distance to the GUI
                    return;
                }

                // won't collect in atmosphere
                if (!IsCollectLegal())
                {
                    DisableCollector();
                    return;
                }

                distanceToLocalStar = UpdateDistanceInGui();

                // collect solar wind for a single frame
                CollectSolarWind(TimeWarp.fixedDeltaTime, false);

                // store current time in case vesel is unloaded
                dLastActiveTime = Planetarium.GetUniversalTime();
                
                // store current strength of the magnetic field in case vessel is unloaded
                dLastMagnetoStrength = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

                // store current solar wind concentration in case vessel is unloaded
                dLastSolarConcentration = solarWindMolesPerSquareMeterPerSecond; //CalculateSolarWindConcentration(part.vessel.solarFlux);
                dLastHydrogenConcentration = hydrogenMolarMassConcentrationPerSquareMeterPerSecond;
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
        private static double GetMagnetosphereRatio(double altitude, double maxAltitude)
        {
            // atmospheric check for the sake of GUI
            if (altitude <= maxAltitude)
                return 1;
            else
                return (altitude < (maxAltitude * 10)) ? maxAltitude / altitude : 0;
        }

        // checks if the vessel is not in atmosphere and if it can therefore collect solar wind. Could incorporate other checks if needed.
        private bool IsCollectLegal()
        {
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) / 2) // won't collect in atmosphere
            {
                ScreenMessages.PostScreenMessage("Solar wind collection not possible in low atmosphere", 10, ScreenMessageStyle.LOWER_CENTER);
                distanceToLocalStar = UpdateDistanceInGui();
                fSolarWindConcentrationPerSquareMeter = 0;
                return false;
            }
            else
                return true;
        }

        private void UpdateIonisationAnimation()
        {
            if (!bIonizing)
            {
                previousPowerPercentage = powerPercentage;
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = 0;
                    ionisationAnimation.Blend(ionAnimName);
                }
                return;
            }

            if (powerPercentage < previousPowerPercentage)
            {
                newNormalTime = Math.Min(Math.Max(powerPercentage / 100, previousPowerPercentage / 100 - TimeWarp.fixedDeltaTime / 2), 1);
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = newNormalTime;
                    ionisationAnimation.Blend(ionAnimName);
                }
                previousPowerPercentage = newNormalTime * 100;
            }
            else if (powerPercentage > previousPowerPercentage)
            {
                newNormalTime = Math.Min(Math.Max(0, previousPowerPercentage / 100 + TimeWarp.fixedDeltaTime / 2), powerPercentage / 100);
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = newNormalTime;
                    ionisationAnimation.Blend(ionAnimName);
                }
                previousPowerPercentage = newNormalTime * 100;
            }
            else
            {
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = powerPercentage / 100;
                    ionisationAnimation.Blend(ionAnimName);
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
        private static double CalculateSolarwindIonConcentration(double flux, double solarCheatMultiplier, double solarWindSpeed)
        {
            const int dAvgSolarWindPerCubM = 6 * 1000000; // various sources differ, most state that there are around 6 particles per cm^3, so around 6000000 per m^3 (some sources go up to 10/cm^3 or even down to 2/cm^3, most are around 6/cm^3).

            double dMolalSolarConcentration = (flux / GameConstants.averageKerbinSolarFlux) * dAvgSolarWindPerCubM * solarWindSpeed * solarCheatMultiplier / GameConstants.avogadroConstant;

            return dMolalSolarConcentration; // in mol / m2 / sec
        }

        private static double CalculateInterstellarIonConcentration(double interstellarCheatMultiplier)
        {
            const double  dAverageInterstellarHydrogenPerCubM = 1 * 1000000;

            var interstellarHydrogenConcentration = dAverageInterstellarHydrogenPerCubM * interstellarCheatMultiplier / GameConstants.avogadroConstant;

            return interstellarHydrogenConcentration; // in mol / m2 / sec
        }

        private static double CalculateCurrentAtmosphereConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereParticlesPerCubM = Math.Max(0, AtmosphericFloatCurves.Instance.ParticlesAtmosphereCubePerMeter.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel);

            var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / GameConstants.avogadroConstant;

            return atmosphereConcentration;
        }

        private static double CalculateCurrentHydrogenParticleConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereParticlesPerCubM = Math.Max(0, AtmosphericFloatCurves.Instance.ParticlesHydrogenCubePerMeter.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel);

            var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / GameConstants.avogadroConstant;

            return atmosphereConcentration;
        }

		private static double CalculateCurrentHeliumParticleConcentration(Vessel vessel)
		{
			if (!vessel.mainBody.atmosphere)
				return 0;

			var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
			var atmosphereParticlesPerCubM = Math.Max(0, 1e+6 * AtmosphericFloatCurves.Instance.ParticlesHeliumnPerCubePerCm.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel);

			var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / GameConstants.avogadroConstant;

			return atmosphereConcentration;
		}

        private static double CalculateCurrentHydrogenIonsConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereParticlesPerCubM = Math.Max(0, AtmosphericFloatCurves.Instance.HydrogenIonsPerCubeCm.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel);

            var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / GameConstants.avogadroConstant;

            return atmosphereConcentration;
        }

		// ToDo make CalculateCurrentHeliumIonsConcentration
		private static double CalculateCurrentHeliumIonsConcentration(Vessel vessel)
		{
			if (!vessel.mainBody.atmosphere)
				return 0;

			var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
			var atmosphereParticlesPerCubM = Math.Max(0, 0.5 * AtmosphericFloatCurves.Instance.HydrogenIonsPerCubeCm.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel);

			var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / GameConstants.avogadroConstant;

			return atmosphereConcentration;
		}

        private double GetAtmosphericGasDensityKgPerCubicMeter()
        {
            if (!vessel.mainBody.atmosphere)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphericDensityKgPerSquareMeter = Math.Max(0, AtmosphericFloatCurves.Instance.MassDensityAtmosphereGramPerCubeCm.Evaluate((float)comparibleEarthAltitudeInKm)) * 1e+3;
            return atmosphericDensityKgPerSquareMeter;
        }

        // calculates the distance to sun
        private static double CalculateDistanceToSun(Vector3d vesselPosition, Vector3d sunPosition)
        {
            return Vector3d.Distance(vesselPosition, sunPosition);
        }

        // helper function for readying the distance for the GUI
        private double UpdateDistanceInGui()
        {
            return ((CalculateDistanceToSun(part.transform.position, localStar.transform.position) - localStar.Radius) * 0.001);
        }

        private string UpdateMagnetoStrengthInGui()
        {
            return (GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) * 100).ToString("F1");
        }

        // the main collecting function
        private void CollectSolarWind(double deltaTimeInSeconds, bool offlineCollecting)
        {
            var ionizationPowerCost =  bIonizing ? ionRequirements *  Math.Pow(powerPercentage * 0.01, 2) : 0;
            var magneticPowerCost = mwRequirements * Math.Pow(powerPercentage * 0.01, 2);
            var dPowerRequirementsMw = PluginHelper.PowerConsumptionMultiplier * (magneticPowerCost + ionizationPowerCost); // change the mwRequirements number in part config to change the power consumption

            // checks for free space in solar wind 'tanks'
			dSolarWindSpareCapacity = part.GetResourceSpareCapacity(solarWindResourceDefinition.name);
			dHydrogenSpareCapacity = part.GetResourceSpareCapacity(hydrogenResourceDefinition.name);

            if (offlineCollecting)
            {
                solarWindMolesPerSquareMeterPerSecond = dLastSolarConcentration; // if resolving offline collection, pass the saved value, because OnStart doesn't resolve the function at line 328
                hydrogenMolarMassConcentrationPerSquareMeterPerSecond = dLastHydrogenConcentration;
            }

            if ((solarWindMolesPerSquareMeterPerSecond > 0 || hydrogenMolarMassConcentrationPerSquareMeterPerSecond > 0)) // && (dSolarWindSpareCapacity > 0 || dHydrogenSpareCapacity > 0))
            {
                var requiredHeliumMass = TimeWarp.fixedDeltaTime * heliumRequirementTonPerSecond;

                var heliumGasRequired = requiredHeliumMass / helium4GasResourceDefinition.density;
                var receivedHeliumGas = part.RequestResource(helium4GasResourceDefinition.id, heliumGasRequired);
                var receivedHeliumGasMass = receivedHeliumGas * helium4GasResourceDefinition.density;

                var massHeliumMassShortage = (requiredHeliumMass - receivedHeliumGasMass);

                var lqdHeliumRequired = massHeliumMassShortage / lqdHelium4ResourceDefinition.density;
                var receivedLqdHelium = part.RequestResource(lqdHelium4ResourceDefinition.id, lqdHeliumRequired);
                var receiverLqdHeliumMass = receivedLqdHelium * lqdHelium4ResourceDefinition.density;

                var heliumRatio = Math.Min(1,  requiredHeliumMass > 0 ? (receivedHeliumGasMass + receiverLqdHeliumMass) / requiredHeliumMass : 0);

                // calculate available power
                var revievedPowerMw = consumeFNResourcePerSecond(dPowerRequirementsMw * heliumRatio, ResourceManager.FNRESOURCE_MEGAJOULES);

                // if power requirement sufficiently low, retreive power from KW source
                if (dPowerRequirementsMw < 2 && revievedPowerMw <= dPowerRequirementsMw)
                {
                    var requiredKw = (dPowerRequirementsMw - revievedPowerMw) * 1000;
                    var receivedKw = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, heliumRatio * requiredKw * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                    revievedPowerMw += (receivedKw * 0.001);
                }

                dLastPowerPercentage = offlineCollecting ? dLastPowerPercentage : (dPowerRequirementsMw > 0 ? revievedPowerMw / dPowerRequirementsMw : 0);

                // show in GUI
                strCollectingStatus = "Collecting solar wind";
            }
            else
            {
                dLastHydrogenConcentration = 0;
                dLastPowerPercentage = 0;
                dPowerRequirementsMw = 0;
            }

            // set the GUI string to state the number of KWs received if the MW requirements were lower than 2, otherwise in MW
            strReceivedPower = dPowerRequirementsMw < 2
                ? (dLastPowerPercentage * dPowerRequirementsMw * 1000).ToString("0.0") + " KW / " + (dPowerRequirementsMw * 1000).ToString("0.0") + " KW"
                : (dLastPowerPercentage * dPowerRequirementsMw).ToString("0.0") + " MW / " + dPowerRequirementsMw.ToString("0.0") + " MW";

            // get the shielding effect provided by the magnetosphere
            magnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

            // if online collecting, get the old values instead (simplification for the time being)
            if (offlineCollecting)
                magnetoSphereStrengthRatio = dLastMagnetoStrength;

            if (Math.Abs(magnetoSphereStrengthRatio) < float.Epsilon)
                dShieldedEffectiveness = 1;
            else
                dShieldedEffectiveness = (1 - magnetoSphereStrengthRatio);

            effectiveSurfaceAreaInSquareMeter = surfaceArea + (magneticArea * powerPercentage * 0.01);
            effectiveSurfaceAreaInSquareKiloMeter = effectiveSurfaceAreaInSquareMeter * 1e-6;
            effectiveDiamterInKilometer = 2 * Math.Sqrt(effectiveSurfaceAreaInSquareKiloMeter / Math.PI);

            Vector3d solarDirectionVector = localStar.transform.position - vessel.transform.position;
            solarWindAngleOfAttackRatio =  Math.Max(0, Vector3d.Dot(part.transform.up, solarDirectionVector.normalized));
            solarwindProductionModifiers = collectMultiplier * effectiveness * dShieldedEffectiveness * dLastPowerPercentage * solarWindAngleOfAttackRatio;

            var solarWindGramCollectedPerSecond = solarWindMolesPerSquareMeterPerSecond * solarwindProductionModifiers * effectiveSurfaceAreaInSquareMeter * 1.9;

            var dInterstellarIonsConcentrationPerSquareMeter = vessel.obt_speed * interstellarDustMolesPerCubicMeter;

            var interstellarGramCollectedPerSecond = dInterstellarIonsConcentrationPerSquareMeter * effectiveSurfaceAreaInSquareMeter * 1.9;            

            /** The first important bit.
             * This determines how much solar wind will be collected. Can be tweaked in part configs by changing the collector's effectiveness.
             * */
			var dSolarDustResourceChange = (solarWindGramCollectedPerSecond + interstellarGramCollectedPerSecond) * deltaTimeInSeconds * 1e-6 / solarWindResourceDefinition.density;

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting)
            {
                var strNumberFormat = dSolarDustResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
				ScreenMessages.PostScreenMessage("We collected " + dSolarDustResourceChange.ToString(strNumberFormat) + " units of " + solarWindResourceDefinition.name, 10, ScreenMessageStyle.LOWER_CENTER);
            }

            // this is the second important bit - do the actual change of the resource amount in the vessel
			dWindResourceFlow = -part.RequestResource(solarWindResourceDefinition.id, -dSolarDustResourceChange);

            var dHydrogenCollectedPerSecond = hydrogenMolarMassConcentrationPerSquareMeterPerSecond * effectiveSurfaceAreaInSquareMeter;
            var dHydrogenResourceChange = dHydrogenCollectedPerSecond * 1e-6 / hydrogenResourceDefinition.density;
			dHydrogenResourceFlow = -part.RequestResource(hydrogenResourceDefinition.id, -dHydrogenResourceChange);

            if (offlineCollecting)
            {
                var strNumberFormat = dHydrogenResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
				ScreenMessages.PostScreenMessage("We collected " + dHydrogenResourceChange.ToString(strNumberFormat) + " units of " + hydrogenResourceDefinition.name, 10, ScreenMessageStyle.LOWER_CENTER);
            }

			var dHeliumCollectedPerSecond = heliumMolarMassConcentrationPerSquareMeterPerSecond * effectiveSurfaceAreaInSquareMeter;
			var dHeliumResourceChange = dHeliumCollectedPerSecond * 1e-6 / helium4GasResourceDefinition.density;
			dHeliumResourceFlow = -part.RequestResource(helium4GasResourceDefinition.id, -dHeliumResourceChange);

			if (offlineCollecting)
			{
				var strNumberFormat = dHeliumResourceChange > 100 ? "0" : "0.000";
				// let the player know that offline collecting worked
				ScreenMessages.PostScreenMessage("We collected " + dHeliumResourceFlow.ToString(strNumberFormat) + " units of " + helium4GasResourceDefinition.name, 10, ScreenMessageStyle.LOWER_CENTER);
			}

            var atmosphericGasKgPerSquareMeter = GetAtmosphericGasDensityKgPerCubicMeter();
            var solarDustKgPerSquareMeter = solarWindMolesPerSquareMeterPerSecond * 1e-3 * 1.9;
            var interstellarDustKgPerSquareMeter = interstellarDustMolesPerCubicMeter * 1e-3 * 1.9;
            var minimumAtmosphericMassMomentumChange = vessel.obt_speed * atmosphericGasKgPerSquareMeter;
            var atmosphericDragInNewton = minimumAtmosphericMassMomentumChange + (squareVelocityDragRatio * vessel.obt_speed * minimumAtmosphericMassMomentumChange);
            var minimumInterstellarMomentumChange = vessel.obt_speed * interstellarDustKgPerSquareMeter;
            var interstellarDustDragInNewton = minimumInterstellarMomentumChange + (squareVelocityDragRatio * vessel.obt_speed * minimumInterstellarMomentumChange);
            var maxOrbitalVesselDragInNewton = effectiveSurfaceAreaInSquareMeter * (atmosphericDragInNewton + interstellarDustDragInNewton);
            var dEffectiveOrbitalVesselDragInNewton = maxOrbitalVesselDragInNewton * (bIonizing ? 1 : atmosphereIonRatio);
            var solarwindDragInNewtonPerSquareMeter = solarDustKgPerSquareMeter + (squareVelocityDragRatio * solarWindSpeed * solarDustKgPerSquareMeter);
            var dSolarWindVesselForceInNewton = solarwindDragInNewtonPerSquareMeter * effectiveSurfaceAreaInSquareMeter;

            fInterstellarIonsConcentrationPerSquareMeter = (float) dInterstellarIonsConcentrationPerSquareMeter;
            fSolarWindCollectedGramPerHour = (float)(solarWindGramCollectedPerSecond * 3600);
            fInterstellarIonsCollectedGramPerHour = (float)interstellarGramCollectedPerSecond * 3600;
            fHydrogenCollectedGramPerHour = (float)dHydrogenCollectedPerSecond * 3600;
            fHeliumCollectedGramPerHour = (float)dHeliumCollectedPerSecond * 3600;
            fAtmosphereIonsKgPerSquareMeter = (float)atmosphericGasKgPerSquareMeter;
            fSolarWindKgPerSquareMeter = (float)solarDustKgPerSquareMeter;
            fInterstellarIonsKgPerSquareMeter = (float)interstellarDustKgPerSquareMeter;
            fAtmosphericDragInNewton = (float)atmosphericDragInNewton;
            fInterstellarDustDragInNewton = (float)interstellarDustDragInNewton;
            fMaxOrbitalVesselDragInKiloNewton = (float)(maxOrbitalVesselDragInNewton * 0.001);
            fEffectiveOrbitalVesselDragInKiloNewton = (float)(dEffectiveOrbitalVesselDragInNewton * 0.001);
            fSolarWindDragInNewtonPerSquareMeter = (float)solarwindDragInNewtonPerSquareMeter;
            fSolarWindVesselForceInNewton = (float)dSolarWindVesselForceInNewton;

            if (vessel.packed)
            {
                var totalVesselMassInKg = part.vessel.GetTotalMass() * 1000;
                if (totalVesselMassInKg <= 0)
                    return;

                var universalTime = Planetarium.GetUniversalTime();
                if (universalTime <= 0)
                    return;

                var orbitalDragDeltaVv = TimeWarp.fixedDeltaTime * part.vessel.obt_velocity.normalized * -dEffectiveOrbitalVesselDragInNewton / totalVesselMassInKg;
                vessel.orbit.Perturb(orbitalDragDeltaVv, universalTime);

                var solarPushDeltaVv = TimeWarp.fixedDeltaTime * solarDirectionVector.normalized * -fSolarWindVesselForceInNewton / totalVesselMassInKg;
                vessel.orbit.Perturb(solarPushDeltaVv, universalTime);
            }
            else
            {
                var vesselRegitBody = part.vessel.GetComponent<Rigidbody>();
                vesselRegitBody.AddForce(part.vessel.velocityD.normalized * -dEffectiveOrbitalVesselDragInNewton * 1e-3, ForceMode.Force);
                vesselRegitBody.AddForce(solarDirectionVector.normalized * -dSolarWindVesselForceInNewton * 1e-3, ForceMode.Force);
            }
        }



    }
}

