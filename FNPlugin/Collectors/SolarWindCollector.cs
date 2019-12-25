using FNPlugin.Resources;
using FNPlugin.Extensions;
using FNPlugin.Redist;
using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Constants;

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
        public double dLastPowerRatio;
        [KSPField(isPersistant = true)]
        public double dLastMagnetoStrength;
        [KSPField(isPersistant = true)]
        double interstellarDustMolesPerCubicMeter;
        [KSPField(isPersistant = true)]
        public double dInterstellarIonsConcentrationPerSquareMeter;
        [KSPField(isPersistant = true)]
        public bool bIsExtended = false;
        [KSPField(isPersistant = true)]
        public double hydrogenMolarMassPerSquareMeterPerSecond;
        [KSPField(isPersistant = true)]
        public double heliumMolarMassPerSquareMeterPerSecond;
        [KSPField(isPersistant = true)]
        public double solarWindMolesPerSquareMeterPerSecond;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionisation"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 0)]
        protected float ionisationPercentage = 0;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_magneticField"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 0)]
        protected float powerPercentage = 100;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_pulsation"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 0)]
        protected float pulsatingPercentage = 100;

        // Part properties
        [KSPField(guiActiveEditor = false, guiName = "#LOC_KSPIE_SolarwindCollector_surfaceArea", guiUnits = " km\xB2")]
        public double surfaceArea = 0; // Surface area of the panel.
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_magneticArea", guiUnits = " m\xB2")]
        public double magneticArea = 0; // Surface area of the panel.
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_effectiveness", guiFormat = "P1")]
        public double effectiveness = 1; // Effectiveness of the panel. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_mwRequirements", guiUnits = " MW")]
        public double mwRequirements = 1; // MW requirements of the collector panel.
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_superConductingRatio", guiUnits = " MW")]
        public double superConductingRatio = 0.05; // MW requirements of the collector panel.
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionRequirements", guiUnits = " MW")]
        public double ionRequirements = 100; // MW requirements of the collector panel.

        [KSPField]
        public double powerReqMult = 1;
        [KSPField]
        public double squareVelocityDragRatio = 0.075;
        [KSPField]
        public double interstellarIonRatio = 0.001;
        [KSPField] 
        public double heliumRequirement = 0.2;
        [KSPField]
        public string animName = "";
        [KSPField]
        public string ionAnimName = "";
        [KSPField]
        public double solarCheatMultiplier = 1;             // Amount of boosted Solar wind activity
        [KSPField]
        public double interstellarDensityCubeCm = 50;       // Amount of Interstellar molecules per cubic cm
        [KSPField]
        public double collectMultiplier = 1;
        [KSPField]
        public double solarWindSpeed = 500000;              // Average Solar win speed 500 km/s
        [KSPField]
        public double avgSolarWindPerCubM = 6000000;        // various sources differ, most state that there are around 6 particles per cm^3, so around 6000000 per m^3 (some sources go up to 10/cm^3 or even down to 2/cm^3, most are around 6/cm^3).

        // GUI
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_effectiveSurfaceArea", guiFormat = "F3", guiUnits = " km\xB2")]
        protected double effectiveSurfaceAreaInSquareKiloMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_effectiveDiameter", guiFormat = "F3", guiUnits = " km")]
        protected double effectiveDiameterInKilometer;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarWindIons", guiUnits = " mol/m\xB2/s")]
        protected float fSolarWindConcentrationPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarIons", guiUnits = " mol/m\xB2/s")] 
        protected float fInterstellarIonsPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarParticles", guiUnits = " mol/m\xB3")]
        protected float fInterstellarIonsPerCubicMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphereParticles", guiUnits = " mol/m\xB3")]
        protected float fAtmosphereConcentration;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_neutralHydrogenConcentration", guiUnits = " mol/m\xB3")]
        protected float fNeutralHydrogenConcentration;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_neutralHeliumConcentration", guiUnits = " mol/m\xB3")]
        protected float fNeutralHeliumConcentration;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionizedHydrogenConcentration", guiUnits = " mol/m\xB3")]
        protected float fIonizedHydrogenConcentration;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionizedHeliumConcentration", guiUnits = " mol/m\xB3")]
        protected float fIonizedHeliumConcentration;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericIonsDensity", guiUnits = " kg/m\xB3")]
        protected float fAtmosphereIonsKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericGasDensity", guiUnits = " kg/m\xB3")]
        protected float fAtmosphereGasKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarIonsDensity", guiUnits = " kg/m\xB2")]
        protected float fInterstellarIonsKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindMassDensity", guiUnits = " kg/m\xB2")]
        protected float fSolarWindKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solidVesselDrag", guiUnits = " N/m\xB2")]
        protected float solidVesselDragInNewton;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericGasDrag", guiUnits = " N/m\xB2")]
        protected float fAtmosphericGasDragInNewton;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericIonDrag", guiUnits = " N/m\xB2")]
        protected float fAtmosphericIonDragInNewton;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindDrag", guiUnits = " N/m\xB2")]
        protected float fSolarWindDragInNewtonPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarDrag", guiUnits = " N/m\xB2")]
        protected float fInterstellarDustDragInNewton;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionisationFacingFactor", guiFormat = "F3")]
        protected double ionisationFacingFactor;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_orbitalDragOnVessel", guiUnits = " kN")]
        protected float fEffectiveOrbitalDragInKiloNewton;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindForceOnVessel", guiUnits = " N")]
        protected float fSolarWindVesselForceInNewton;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_magnetosphereStrengthRatio", guiFormat = "F3")]
        protected double magnetoSphereStrengthRatio;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindFacingFactor", guiFormat = "F3")]
        protected double solarWindAngleOfAttackFactor = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindCollectionModifier", guiFormat = "F3")]
        protected double solarwindProductionModifiers = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarWindMassCollected", guiUnits = " g/h")]
        protected float fSolarWindCollectedGramPerHour;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarMassCollected", guiUnits = " g/h")]
        protected float fInterstellarIonsCollectedGramPerHour;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphereHydrogenCollected", guiUnits = " g/h")]
        protected float fHydrogenCollectedGramPerHour;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphereHeliumCollected", guiUnits = " g/h")]
        protected float fHeliumCollectedGramPerHour;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_distanceFromSun", guiFormat = "F1", guiUnits = " km")]
        protected double distanceToLocalStar;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_status")]
        protected string strCollectingStatus = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_powerUsage")]
        protected string strReceivedPower = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_magnetosphereShieldingStrength", guiUnits = " %")]
        protected string strMagnetoStrength = "";

        //[KSPField(guiActive = true, guiName = "Belt Radiation Flux")]
        //protected double beltRadiationFlux;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_verticalSpeed", guiFormat = "F1", guiUnits = " m/s")]
        protected double verticalSpeed;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_heliosphereFactor", guiFormat = "F3")]
        protected double helioSphereFactor;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_relativeSolarwindSpeed", guiFormat = "F3")]
        protected double relativeSolarWindSpeed;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarDensityFactor", guiFormat = "F3")]
        protected double interstellarDensityFactor;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarWindDensityFactor", guiFormat = "F3")]
        protected double solarwindDensityFactor;

        // internals
        double effectiveMagneticSurfaceAreaInSquareMeter;
        double dWindResourceFlow = 0;
        double dHydrogenResourceFlow = 0;
        double dHeliumResourceFlow = 0;
        double heliumRequirementTonPerSecond;
        
        double dSolarWindSpareCapacity;
        double dHydrogenSpareCapacity;
        double dShieldedEffectiveness = 0;

        double effectiveIonisationFactor;
        double effectiveNonIonisationFactor;
        float newNormalTime;
        float previousIonisationPercentage;

        Animation deployAnimation;
        Animation ionisationAnimation;
        CelestialBody localStar;
        CelestialBody homeworld; 

        PartResourceDefinition helium4GasResourceDefinition;
        PartResourceDefinition lqdHelium4ResourceDefinition;
        PartResourceDefinition hydrogenResourceDefinition;
        PartResourceDefinition solarWindResourceDefinition;

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_activateCollector", active = true)]
        public void ActivateCollector()
        {
            Debug.Log("[KSPI]: SolarwindCollector on " + part.name + " was Force Activated");
            this.part.force_activate();

            bIsEnabled = true;
            OnUpdate();
            if (IsCollectLegal())
                UpdatePartAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_disableCollector", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            ionisationPercentage = 0;
            OnUpdate();
            // folding nimation will only play if the collector was extended before being disabled
            if (bIsExtended == true)
                UpdatePartAnimation();
        }

        [KSPAction("#LOC_KSPIE_SolarwindCollector_activateCollector")]
        public void ActivateScoopAction(KSPActionParam param)
        {
            ActivateCollector();
        }

        [KSPAction("#LOC_KSPIE_SolarwindCollector_disableCollector")]
        public void DisableScoopAction(KSPActionParam param)
        {
            DisableCollector();
        }

        [KSPAction("#LOC_KSPIE_SolarwindCollector_toggleCollector")]
        public void ToggleScoopAction(KSPActionParam param)
        {
            if (bIsEnabled)
                DisableCollector();
            else
                ActivateCollector();
        }

        public bool bIonizing
        {
            get { return ionisationPercentage > 0; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            // get the part's animation
            deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            ionisationAnimation = part.FindModelAnimators(ionAnimName).FirstOrDefault();
            previousIonisationPercentage = ionisationPercentage;

            if (ionisationAnimation != null)
            {
                ionisationAnimation[ionAnimName].speed = 0;
                ionisationAnimation[ionAnimName].normalizedTime = bIonizing ? ionisationPercentage * 0.01f : 0; // normalizedTime at 1 is the end of the animation
                ionisationAnimation.Blend(ionAnimName);
            }

            if (state == StartState.Editor) return; // collecting won't work in editor

            heliumRequirementTonPerSecond = heliumRequirement * 1e-6 / GameConstants.SECONDS_IN_HOUR ;
            helium4GasResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Helium4Gas);
            lqdHelium4ResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdHelium4);
            solarWindResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.SolarWind);
            hydrogenResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen);


            localStar = vessel.GetLocalStar();
            homeworld = FlightGlobals.GetHomeBody();

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
            if (dLastPowerRatio < 0.01) return;

            // verify altitude is not too low
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)))
            {
                ScreenMessages.PostScreenMessage("Solar Wind Collection Error, vessel in atmosphere", 10, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // if the part should be extended (from last time), go to the extended animation
            if (bIsExtended && deployAnimation != null)
                deployAnimation[animName].normalizedTime = 1;

            // calculate time difference since last time the vessel was active
            var dTimeDifference =  Math.Abs(Planetarium.GetUniversalTime() - dLastActiveTime);

            // increase bugger to allow procesing
            var solarWindBuffer = part.Resources[solarWindResourceDefinition.name];
            solarWindBuffer.maxAmount = 100 * part.mass * dTimeDifference;

            // collect solar wind for entire duration
            CollectSolarWind(dTimeDifference, true);
        }

        public override void OnUpdate()
        {
            localStar = vessel.GetLocalStar();
            verticalSpeed = vessel.mainBody == localStar ? vessel.verticalSpeed : 0;
            helioSphereFactor = Math.Min(1, CalculateHelioSphereRatio(vessel, localStar, homeworld));
            interstellarDensityFactor = helioSphereFactor == 0 ? 0 : Math.Max(0, AtmosphericFloatCurves.Instance.InterstellarDensityRatio.Evaluate((float)helioSphereFactor * 100));
            solarwindDensityFactor = Math.Max(0, 1 - interstellarDensityFactor);
            relativeSolarWindSpeed = solarwindDensityFactor * (solarWindSpeed - verticalSpeed);

            solarWindMolesPerSquareMeterPerSecond = CalculateSolarwindIonMolesPerSquareMeter(avgSolarWindPerCubM * solarCheatMultiplier, vessel, Math.Abs(relativeSolarWindSpeed));
            interstellarDustMolesPerCubicMeter = CalculateInterstellarMoleConcentration(vessel, interstellarDensityCubeCm, interstellarDensityFactor);

            var maxInterstellarDustMolesPerSquareMeter = vessel.obt_speed * interstellarDustMolesPerCubicMeter;

            var currentInterstellarIonRatio = vessel.mainBody == localStar 
                ? Math.Max(interstellarIonRatio, 1 - helioSphereFactor * helioSphereFactor) 
                : interstellarIonRatio;

            dInterstellarIonsConcentrationPerSquareMeter = maxInterstellarDustMolesPerSquareMeter * (effectiveIonisationFactor * (1 - currentInterstellarIonRatio) + effectiveNonIonisationFactor * currentInterstellarIonRatio);

            if (vessel.mainBody != localStar)
            {
                var dAtmosphereConcentration = AtmosphericFloatCurves.CalculateCurrentAtmosphereConcentration(vessel);
                var dHydrogenParticleConcentration = CalculateCurrentHydrogenParticleConcentration(vessel);
                var dHeliumParticleConcentration = CalculateCurrentHeliumParticleConcentration(vessel);
                var dIonizedHydrogenConcentration = CalculateCurrentHydrogenIonsConcentration(vessel);
                var dIonizedHeliumConcentration = CalculateCurrentHeliumIonsConcentration(vessel);

                hydrogenMolarMassPerSquareMeterPerSecond = effectiveIonisationFactor * dHydrogenParticleConcentration + effectiveNonIonisationFactor * dIonizedHydrogenConcentration;
                heliumMolarMassPerSquareMeterPerSecond = effectiveIonisationFactor * dHeliumParticleConcentration + effectiveNonIonisationFactor * dIonizedHeliumConcentration;

                fAtmosphereConcentration = (float)dAtmosphereConcentration;
                fNeutralHydrogenConcentration = (float)dHydrogenParticleConcentration;
                fNeutralHeliumConcentration = (float)dHeliumParticleConcentration;
                fIonizedHydrogenConcentration = (float)dIonizedHydrogenConcentration;
                fIonizedHeliumConcentration = (float)dIonizedHeliumConcentration;
            }
            else
            {
                hydrogenMolarMassPerSquareMeterPerSecond = 0;
                heliumMolarMassPerSquareMeterPerSecond = 0;
                fAtmosphereConcentration = 0;
                fNeutralHydrogenConcentration = 0;
                fNeutralHeliumConcentration = 0;
                fIonizedHydrogenConcentration = 0;
                fIonizedHeliumConcentration = 0;
            }

            fSolarWindConcentrationPerSquareMeter = (float)solarWindMolesPerSquareMeterPerSecond;
            fInterstellarIonsPerCubicMeter = (float)interstellarDustMolesPerCubicMeter;
            magnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));
            strMagnetoStrength = UpdateMagnetoStrengthInGui();

            Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled
            Fields["strReceivedPower"].guiActive = bIsEnabled;

            Fields["verticalSpeed"].guiActive = verticalSpeed > 0;
            Fields["helioSphereFactor"].guiActive = helioSphereFactor > 0;

            Fields["fInterstellarDustDragInNewton"].guiActive = fInterstellarDustDragInNewton > 0;
            Fields["fAtmosphereIonsKgPerSquareMeter"].guiActive = fAtmosphereIonsKgPerSquareMeter > 0;
            Fields["fAtmosphereGasKgPerSquareMeter"].guiActive = fAtmosphereGasKgPerSquareMeter > 0;
            Fields["fAtmosphereConcentration"].guiActive = fAtmosphereConcentration > 0;
            Fields["fNeutralHydrogenConcentration"].guiActive = fNeutralHydrogenConcentration > 0;
            Fields["fNeutralHeliumConcentration"].guiActive = fNeutralHeliumConcentration > 0;
            Fields["fIonizedHydrogenConcentration"].guiActive = fIonizedHydrogenConcentration > 0;
            Fields["fIonizedHeliumConcentration"].guiActive = fIonizedHeliumConcentration > 0;
            Fields["fSolarWindVesselForceInNewton"].guiActive = fSolarWindVesselForceInNewton > 0;
            Fields["fSolarWindKgPerSquareMeter"].guiActive = fSolarWindKgPerSquareMeter > 0;
            Fields["fAtmosphericIonDragInNewton"].guiActive = fAtmosphericIonDragInNewton > 0;
            Fields["fAtmosphericGasDragInNewton"].guiActive = fAtmosphericGasDragInNewton > 0;
            Fields["solidVesselDragInNewton"].guiActive = solidVesselDragInNewton > 0;
            Fields["solarwindProductionModifiers"].guiActive = solarwindProductionModifiers > 0;
            Fields["fSolarWindCollectedGramPerHour"].guiActive = fSolarWindCollectedGramPerHour > 0;
            Fields["fHydrogenCollectedGramPerHour"].guiActive = fHydrogenCollectedGramPerHour > 0;
            Fields["fHeliumCollectedGramPerHour"].guiActive = fHeliumCollectedGramPerHour > 0;
            Fields["fInterstellarIonsPerCubicMeter"].guiActive = fInterstellarIonsPerCubicMeter > 0;
            Fields["fInterstellarIonsPerSquareMeter"].guiActive = fInterstellarIonsPerSquareMeter > 0;
            Fields["fInterstellarIonsKgPerSquareMeter"].guiActive = fInterstellarIonsKgPerSquareMeter > 0;
            Fields["fEffectiveOrbitalDragInKiloNewton"].guiActive = fEffectiveOrbitalDragInKiloNewton > 0;
            Fields["fSolarWindDragInNewtonPerSquareMeter"].guiActive = fSolarWindDragInNewtonPerSquareMeter > 0;
            Fields["fSolarWindConcentrationPerSquareMeter"].guiActive = fSolarWindConcentrationPerSquareMeter > 0;
            Fields["fInterstellarIonsCollectedGramPerHour"].guiActive = fInterstellarIonsCollectedGramPerHour > 0;
        }

        public double SquareDragFactor
        {
            get { return ((100d - pulsatingPercentage) / 100d) + squareVelocityDragRatio; }
        }

        public double CollectionRatio
        {
            get { return pulsatingPercentage / 100d; }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            var solarWindBuffer = part.Resources[solarWindResourceDefinition.name];
            if (solarWindBuffer != null)
                solarWindBuffer.maxAmount = 10 * part.mass * TimeWarp.fixedDeltaTime;

            UpdateIonisationAnimation();

            if (FlightGlobals.fetch == null) return;

            UpdateDistanceInGui(); // passes the distance to the GUI

            if (!bIsEnabled)
            {
                strCollectingStatus = "Disabled";
                fEffectiveOrbitalDragInKiloNewton = 0;
                fSolarWindVesselForceInNewton = 0;
                    
                return;
            }

            // won't collect in atmosphere
            if (!IsCollectLegal())
            {
                DisableCollector();
                return;
            }

            // collect solar wind for a single frame
            CollectSolarWind(TimeWarp.fixedDeltaTime, false);

            // store current time in case vesel is unloaded
            dLastActiveTime = Planetarium.GetUniversalTime();
                
            // store current strength of the magnetic field in case vessel is unloaded
            dLastMagnetoStrength = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));
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
                previousIonisationPercentage = ionisationPercentage;
                if (ionisationAnimation == null) return;

                ionisationAnimation[ionAnimName].speed = 0;
                ionisationAnimation[ionAnimName].normalizedTime = 0;
                ionisationAnimation.Blend(ionAnimName);
                return;
            }

            if (ionisationPercentage < previousIonisationPercentage)
            {
                newNormalTime = Math.Min(Math.Max(ionisationPercentage / 100, previousIonisationPercentage / 100 - TimeWarp.fixedDeltaTime / 2), 1);
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = newNormalTime;
                    ionisationAnimation.Blend(ionAnimName);
                }
                previousIonisationPercentage = newNormalTime * 100;
            }
            else if (ionisationPercentage > previousIonisationPercentage)
            {
                newNormalTime = Math.Min(Math.Max(0, previousIonisationPercentage / 100 + TimeWarp.fixedDeltaTime / 2), ionisationPercentage / 100);
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = newNormalTime;
                    ionisationAnimation.Blend(ionAnimName);
                }
                previousIonisationPercentage = newNormalTime * 100;
            }
            else
            {
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = ionisationPercentage / 100;
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
        private static double CalculateSolarwindIonMolesPerSquareMeter(double solarWindPerCubM, Vessel vessel, double solarWindSpeed)
        {
            var dMolalSolarConcentration = (vessel.solarFlux / GameConstants.averageKerbinSolarFlux) * solarWindPerCubM * solarWindSpeed / PhysicsGlobals.AvogadroConstant;

            return Math.Abs(dMolalSolarConcentration); // in mol / m2 / sec
        }

        private static double CalculateInterstellarMoleConcentration(Vessel vessel, double densityInterstellarDencityCubeCm , double interstellarDensity)
        {
            double dAverageInterstellarHydrogenPerCubM = densityInterstellarDencityCubeCm * 1000000;

            var interstellarHydrogenConcentration = dAverageInterstellarHydrogenPerCubM / PhysicsGlobals.AvogadroConstant;

            var interstellarDensityModifier = Math.Max(0, interstellarDensity);

            return interstellarHydrogenConcentration * interstellarDensityModifier; // in mol / m2 / sec
        }

        private static double CalculateHelioSphereRatio(Vessel vessel, CelestialBody localStar, CelestialBody homeworld)
        {
            // when in the SOI of Kerbol that has an infinite SIO

            var influenceRatio = vessel.mainBody == localStar
                ? !double.IsInfinity(vessel.mainBody.sphereOfInfluence) && vessel.mainBody.sphereOfInfluence > 0
                    ? vessel.altitude / vessel.mainBody.sphereOfInfluence
                    : vessel.altitude / (homeworld.orbit.semiMinorAxis * 100)
                : 0;
            return influenceRatio;
        }

        private static double CalculateCurrentHydrogenParticleConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparibleEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparibleEarthAltitudeInKm <= 1000
                ? Math.Max(0, AtmosphericFloatCurves.Instance.ParticlesHydrogenCubePerMeter.Evaluate((float)comparibleEarthAltitudeInKm))
                : 2.101e+13f * (1 / (Math.Pow(20 / radiusModifier, (comparibleEarthAltitudeInKm - 1000) / 1000)));

            var atmosphereConcentration = atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        private static double CalculateCurrentHeliumParticleConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparibleEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparibleEarthAltitudeInKm <= 1000
                ? Math.Max(0, AtmosphericFloatCurves.Instance.ParticlesHeliumnPerCubePerCm.Evaluate((float)comparibleEarthAltitudeInKm))
                : 8.196E+05f * (1 / (Math.Pow(20 / radiusModifier, (comparibleEarthAltitudeInKm - 1000) / 1000)));

            var atmosphereConcentration = 1e+6 * atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        private static double CalculateCurrentHydrogenIonsConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparibleEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparibleEarthAltitudeInKm <= 1240
                ? Math.Max(0, AtmosphericFloatCurves.Instance.HydrogenIonsPerCubeCm.Evaluate((float)comparibleEarthAltitudeInKm))
                : 6.46e+9f * (1 / (Math.Pow(20 / radiusModifier, (comparibleEarthAltitudeInKm - 1240) / 1240)));

            var atmosphereConcentration = 1e+6 * atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        // ToDo make CalculateCurrentHeliumIonsConcentration
        private static double CalculateCurrentHeliumIonsConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparibleEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparibleEarthAltitudeInKm <= 1240
                ? Math.Max(0, AtmosphericFloatCurves.Instance.HydrogenIonsPerCubeCm.Evaluate((float)comparibleEarthAltitudeInKm))
                : 6.46e+9f * (1 / (Math.Pow(20 / radiusModifier, (comparibleEarthAltitudeInKm - 1240) / 1240)));

            var atmosphereConcentration = 0.5 * 1e+6 * atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        private static double CalculateAtmosphereIonAtSolarMinimumDayTimeAtKgPerCubicMeter(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereIonsPerCubCm = comparibleEarthAltitudeInKm > (64000 * radiusModifier) ? 0
                : comparibleEarthAltitudeInKm <= 1000
                    ? Math.Max(0, AtmosphericFloatCurves.Instance.IonSolarMinimumDayTimeCubeCm.Evaluate((float)comparibleEarthAltitudeInKm))
                    : 1.17724306767693e+004f * (1 / (Math.Pow(20, (comparibleEarthAltitudeInKm - 1000) / 1000)));

            var atmosphereConcentration = 1e+3 * atmosphereMultiplier * atmosphereIonsPerCubCm / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        // calculates the distance to sun
        private static double CalculateDistanceToSun(Vector3d vesselPosition, Vector3d sunPosition)
        {
            return Vector3d.Distance(vesselPosition, sunPosition);
        }

        // helper function for readying the distance for the GUI
        private void UpdateDistanceInGui()
        {
            if (localStar == null)
                distanceToLocalStar = double.PositiveInfinity;

            //distanceToLocalStar = part.vessel.distanceToSun * 0.001;

            distanceToLocalStar = ((CalculateDistanceToSun(part.transform.position, localStar.transform.position) - localStar.Radius) * 0.001);
        }

        private string UpdateMagnetoStrengthInGui()
        {
            return (GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) * 100).ToString("F1");
        }

        // the main collecting function
        private void CollectSolarWind(double deltaTimeInSeconds, bool offlineCollecting)
        {
            var ionizationPowerCost = bIonizing ? ionRequirements * Math.Pow(ionisationPercentage * 0.01, 2) : 0;

            var magneticPulsatingPowerCost = mwRequirements * (pulsatingPercentage * 0.01);
            var magneticSuperconductorPowerReqCost = mwRequirements * superConductingRatio;

            var magneticPowerCost = (magneticPulsatingPowerCost + magneticSuperconductorPowerReqCost) * Math.Pow(powerPercentage * 0.01, 2);
            var dPowerRequirementsMw = powerReqMult * PluginHelper.PowerConsumptionMultiplier * (magneticPowerCost + ionizationPowerCost); // change the mwRequirements number in part config to change the power consumption
            var dWasteheatProductionMw = powerReqMult * PluginHelper.PowerConsumptionMultiplier * (magneticPulsatingPowerCost + magneticSuperconductorPowerReqCost * 0.05 + ionizationPowerCost * 0.3);

            // checks for free space in solar wind 'tanks'
            dSolarWindSpareCapacity = part.GetResourceSpareCapacity(solarWindResourceDefinition.name);
            dHydrogenSpareCapacity = part.GetResourceSpareCapacity(hydrogenResourceDefinition.name);

            if ((solarWindMolesPerSquareMeterPerSecond > 0 || hydrogenMolarMassPerSquareMeterPerSecond > 0 || interstellarDustMolesPerCubicMeter > 0)) // && (dSolarWindSpareCapacity > 0 || dHydrogenSpareCapacity > 0))
            {
                var requiredHeliumMass = TimeWarp.fixedDeltaTime * heliumRequirementTonPerSecond;

                var heliumGasRequired = requiredHeliumMass / (double)(decimal)helium4GasResourceDefinition.density;
                var receivedHeliumGas = part.RequestResource(helium4GasResourceDefinition.id, heliumGasRequired);
                var receivedHeliumGasMass = receivedHeliumGas * (double)(decimal)helium4GasResourceDefinition.density;

                var massHeliumMassShortage = (requiredHeliumMass - receivedHeliumGasMass);

                var lqdHeliumRequired = massHeliumMassShortage / (double)(decimal)lqdHelium4ResourceDefinition.density;
                var receivedLqdHelium = part.RequestResource(lqdHelium4ResourceDefinition.id, lqdHeliumRequired);
                var receiverLqdHeliumMass = receivedLqdHelium * (double)(decimal)lqdHelium4ResourceDefinition.density;

                var heliumRatio = Math.Min(1,  requiredHeliumMass > 0 ? (receivedHeliumGasMass + receiverLqdHeliumMass) / requiredHeliumMass : 0);

                // calculate available power
                var revievedPowerMw = consumeFNResourcePerSecond(dPowerRequirementsMw * heliumRatio, ResourceManager.FNRESOURCE_MEGAJOULES);

                // if power requirement sufficiently low, retreive power from KW source
                //if (dPowerRequirementsMw < 2 && revievedPowerMw <= dPowerRequirementsMw)
                //{
                //    var requiredKw = (dPowerRequirementsMw - revievedPowerMw) * 1000;
                //    var receivedKw = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, heliumRatio * requiredKw * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                //    revievedPowerMw += (receivedKw * 0.001);
                //}

                dLastPowerRatio = offlineCollecting ? dLastPowerRatio : (dPowerRequirementsMw > 0 ? revievedPowerMw / dPowerRequirementsMw : 0);

                supplyManagedFNResourcePerSecond(dWasteheatProductionMw * dLastPowerRatio, ResourceManager.FNRESOURCE_WASTEHEAT);

                // show in GUI
                strCollectingStatus = "Collecting solar wind";
            }
            else
            {
                dLastPowerRatio = 0;
                dPowerRequirementsMw = 0;
            }            

            // set the GUI string to state the number of KWs received if the MW requirements were lower than 2, otherwise in MW
            strReceivedPower = dPowerRequirementsMw < 2
                ? (dLastPowerRatio * dPowerRequirementsMw * 1000).ToString("0.0") + " KW / " + (dPowerRequirementsMw * 1000).ToString("0.0") + " KW"
                : (dLastPowerRatio * dPowerRequirementsMw).ToString("0.0") + " MW / " + dPowerRequirementsMw.ToString("0.0") + " MW";

            // get the shielding effect provided by the magnetosphere
            magnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

            // if online collecting, get the old values instead (simplification for the time being)
            if (offlineCollecting)
                magnetoSphereStrengthRatio = dLastMagnetoStrength;

            if (Math.Abs(magnetoSphereStrengthRatio) < float.Epsilon)
                dShieldedEffectiveness = 1;
            else
                dShieldedEffectiveness = (1 - magnetoSphereStrengthRatio);

            effectiveMagneticSurfaceAreaInSquareMeter = (magneticArea * powerPercentage * 0.01);
            effectiveSurfaceAreaInSquareKiloMeter = effectiveMagneticSurfaceAreaInSquareMeter * 1e-6;
            effectiveDiameterInKilometer = 2 * Math.Sqrt(effectiveSurfaceAreaInSquareKiloMeter / Math.PI);

            Vector3d solarWindDirectionVector = localStar.transform.position - vessel.transform.position;
            if (relativeSolarWindSpeed < 0)
                solarWindDirectionVector *= -1;

            solarWindAngleOfAttackFactor =  Math.Max(0, Vector3d.Dot(part.transform.up, solarWindDirectionVector.normalized) - 0.5) * 2;
            solarwindProductionModifiers = collectMultiplier * effectiveness * dShieldedEffectiveness * dLastPowerRatio * solarWindAngleOfAttackFactor;

            var effectiveMagneticSurfaceAreaForCollection = effectiveMagneticSurfaceAreaInSquareMeter * CollectionRatio;
            var solarWindGramCollectedPerSecond = solarWindMolesPerSquareMeterPerSecond * solarwindProductionModifiers * effectiveMagneticSurfaceAreaForCollection * 1.9;
            var interstellarGramCollectedPerSecond = dInterstellarIonsConcentrationPerSquareMeter * effectiveMagneticSurfaceAreaForCollection * 1.9;

            /** The first important bit.
             * This determines how much solar wind will be collected. Can be tweaked in part configs by changing the collector's effectiveness.
             * */
            var dSolarDustResourceChange = (solarWindGramCollectedPerSecond + interstellarGramCollectedPerSecond) * deltaTimeInSeconds * 1e-6 / (double)(decimal)solarWindResourceDefinition.density;

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting && dSolarDustResourceChange > 0)
            {
                var strNumberFormat = dSolarDustResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("We collected " + dSolarDustResourceChange.ToString(strNumberFormat) + " units of " + solarWindResourceDefinition.name, 10, ScreenMessageStyle.LOWER_CENTER);
            }

            // this is the second important bit - do the actual change of the resource amount in the vessel
            dWindResourceFlow = -part.RequestResource(solarWindResourceDefinition.id, -dSolarDustResourceChange);

            var dHydrogenCollectedPerSecond = hydrogenMolarMassPerSquareMeterPerSecond * effectiveMagneticSurfaceAreaForCollection;
            var dHydrogenResourceChange = dHydrogenCollectedPerSecond * 1e-6 / (double)(decimal)hydrogenResourceDefinition.density;
            dHydrogenResourceFlow = -part.RequestResource(hydrogenResourceDefinition.id, -dHydrogenResourceChange);

            if (offlineCollecting && dHydrogenResourceChange > 0)
            {
                var strNumberFormat = dHydrogenResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("We collected " + dHydrogenResourceChange.ToString(strNumberFormat) + " units of " + hydrogenResourceDefinition.name, 10, ScreenMessageStyle.LOWER_CENTER);
            }

            var dHeliumCollectedPerSecond = heliumMolarMassPerSquareMeterPerSecond * effectiveMagneticSurfaceAreaForCollection;
            var dHeliumResourceChange = dHeliumCollectedPerSecond * 1e-6 / (double)(decimal)helium4GasResourceDefinition.density;
            dHeliumResourceFlow = -part.RequestResource(helium4GasResourceDefinition.id, -dHeliumResourceChange);

            if (offlineCollecting && dHeliumResourceChange > 0)
            {
                var strNumberFormat = dHeliumResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("We collected " + dHeliumResourceFlow.ToString(strNumberFormat) + " units of " + helium4GasResourceDefinition.name, 10, ScreenMessageStyle.LOWER_CENTER);
            }

            var atmosphericGasKgPerSquareMeter = AtmosphericFloatCurves.GetAtmosphericGasDensityKgPerCubicMeter(vessel.mainBody, vessel.altitude);
            var minAtmosphericGasMassMomentumChange = vessel.obt_speed * atmosphericGasKgPerSquareMeter;
            var atmosphericGasDragInNewton = minAtmosphericGasMassMomentumChange + (SquareDragFactor * vessel.obt_speed * minAtmosphericGasMassMomentumChange);

            var atmosphericIonsKgPerSquareMeter = CalculateAtmosphereIonAtSolarMinimumDayTimeAtKgPerCubicMeter(vessel);
            var minAtmosphericIonsMassMomentumChange = vessel.obt_speed * atmosphericIonsKgPerSquareMeter;
            var atmosphericIonDragInNewton = minAtmosphericIonsMassMomentumChange + (SquareDragFactor * vessel.obt_speed * minAtmosphericIonsMassMomentumChange);

            var interstellarDustKgPerSquareMeter = interstellarDustMolesPerCubicMeter * 1e-3 * 1.9;
            var minimumInterstellarMomentumChange = vessel.obt_speed * interstellarDustKgPerSquareMeter;
            var interstellarDustDragInNewton = minimumInterstellarMomentumChange + (SquareDragFactor * vessel.obt_speed * minimumInterstellarMomentumChange);

            ionisationFacingFactor = Math.Max(0, Vector3d.Dot(part.transform.up, part.vessel.obt_velocity.normalized) - 0.5) * 2;

            var atmosphereModifier = Math.Max(0, 1 - Math.Pow(vessel.atmDensity, 0.2));

            effectiveIonisationFactor = ionisationFacingFactor * atmosphereModifier * Math.Pow(ionisationPercentage * 0.01, 2);
            effectiveNonIonisationFactor = 1 - effectiveIonisationFactor;

            var effectiveSolidVesselDrag = !bIsExtended ? 0 : surfaceArea * vessel.obt_speed * vessel.obt_speed * (atmosphericGasKgPerSquareMeter + interstellarDustKgPerSquareMeter);

            var cosAngleToGravityVector = 1 - Math.Abs(Vector3d.Dot(part.vessel.gravityTrue.normalized, part.vessel.obt_velocity.normalized));

            var effectiveMagneticAtmosphereDrag = cosAngleToGravityVector * (effectiveIonisationFactor * atmosphericGasDragInNewton + effectiveNonIonisationFactor * atmosphericIonDragInNewton);
            var effectiveInterstellarDrag = interstellarDustDragInNewton * (effectiveIonisationFactor + effectiveNonIonisationFactor * interstellarIonRatio);

            var dEffectiveOrbitalVesselDragInNewton = effectiveSolidVesselDrag + effectiveMagneticSurfaceAreaInSquareMeter * (effectiveMagneticAtmosphereDrag + effectiveInterstellarDrag);

            var solarDustKgPerSquareMeter = solarWindMolesPerSquareMeterPerSecond * 1e-3 * 1.9;
            var solarwindDragInNewtonPerSquareMeter = solarDustKgPerSquareMeter + (SquareDragFactor * Math.Abs(relativeSolarWindSpeed) * solarDustKgPerSquareMeter);
            var dSolarWindVesselForceInNewton = solarwindDragInNewtonPerSquareMeter * effectiveMagneticSurfaceAreaInSquareMeter;

            fInterstellarIonsPerSquareMeter = (float) dInterstellarIonsConcentrationPerSquareMeter;
            fSolarWindCollectedGramPerHour = (float)solarWindGramCollectedPerSecond * 3600;
            fInterstellarIonsCollectedGramPerHour = (float)interstellarGramCollectedPerSecond * 3600;
            fHydrogenCollectedGramPerHour = (float)dHydrogenCollectedPerSecond * 3600;
            fHeliumCollectedGramPerHour = (float)dHeliumCollectedPerSecond * 3600;
            fAtmosphereGasKgPerSquareMeter =  (float)atmosphericGasKgPerSquareMeter;
            fAtmosphereIonsKgPerSquareMeter = (float)atmosphericIonsKgPerSquareMeter;
            fSolarWindKgPerSquareMeter = (float)solarDustKgPerSquareMeter;
            fInterstellarIonsKgPerSquareMeter = (float)interstellarDustKgPerSquareMeter;
            solidVesselDragInNewton = (float) effectiveSolidVesselDrag;
            fAtmosphericGasDragInNewton = (float)atmosphericGasDragInNewton;
            fAtmosphericIonDragInNewton = (float)atmosphericIonDragInNewton;
            fInterstellarDustDragInNewton = (float)interstellarDustDragInNewton;
            fEffectiveOrbitalDragInKiloNewton = (float)dEffectiveOrbitalVesselDragInNewton / 1000;
            fSolarWindDragInNewtonPerSquareMeter = (float)solarwindDragInNewtonPerSquareMeter;
            fSolarWindVesselForceInNewton = (float)dSolarWindVesselForceInNewton;

            var totalVesselMassInKg = part.vessel.totalMass * 1000;
            if (totalVesselMassInKg <= 0)
                return;

            var universalTime = Planetarium.GetUniversalTime();
            if (universalTime <= 0)
                return;
            
            var orbitalDragDeltaVv = TimeWarp.fixedDeltaTime * part.vessel.obt_velocity.normalized * -dEffectiveOrbitalVesselDragInNewton / totalVesselMassInKg;
            var solarPushDeltaVv = TimeWarp.fixedDeltaTime * solarWindDirectionVector.normalized * -fSolarWindVesselForceInNewton / totalVesselMassInKg;
            TimeWarp.GThreshold = 2;

            if (!double.IsNaN(orbitalDragDeltaVv.x) && !double.IsNaN(orbitalDragDeltaVv.y) && !double.IsNaN(orbitalDragDeltaVv.z))
            {
                if (vessel.packed)
                    vessel.orbit.Perturb(orbitalDragDeltaVv, universalTime);
                else
                    vessel.ChangeWorldVelocity(orbitalDragDeltaVv);
            }

            if (!double.IsNaN(solarPushDeltaVv.x) && !double.IsNaN(solarPushDeltaVv.y) && !double.IsNaN(solarPushDeltaVv.z))
            {
                if (vessel.packed)
                    vessel.orbit.Perturb(solarPushDeltaVv, universalTime);
                else
                    vessel.ChangeWorldVelocity(solarPushDeltaVv);
            }

            //var vesselRegitBody = part.vessel.GetComponent<Rigidbody>();
            //vesselRegitBody.AddForce(part.vessel.velocityD.normalized * -dEffectiveOrbitalVesselDragInNewton * 1e-3, ForceMode.Force);
            //vesselRegitBody.AddForce(solarWindDirectionVector.normalized * -dSolarWindVesselForceInNewton * 1e-3, ForceMode.Force);

        }

        public override int getPowerPriority()
        {
            return 4;
        }
    }
}

