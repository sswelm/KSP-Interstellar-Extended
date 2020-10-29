using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Collectors
{
    [KSPModule("Solar Wind Collector")]
    class SolarWindCollector : ResourceSuppliableModule
    {
        public const string GROUP = "SolarWindCollector";
        public const string GROUP_TITLE = "#LOC_KSPIE_SolarwindCollector_groupName";

        // Persistent True
        [KSPField(isPersistant = true)]
        public bool bIsEnabled;
        [KSPField(isPersistant = true)]
        public double dLastActiveTime;
        [KSPField(isPersistant = true)]
        public double dLastPowerRatio;
        [KSPField(isPersistant = true)]
        public double dLastMagnetoStrength;
        [KSPField(isPersistant = true)]
        public double interstellarDustMolesPerCubicMeter;
        [KSPField(isPersistant = true)]
        public double dInterstellarIonsConcentrationPerSquareMeter;
        [KSPField(isPersistant = true)]
        public bool bIsExtended;
        [KSPField(isPersistant = true)]
        public double hydrogenMolarMassPerSquareMeterPerSecond;
        [KSPField(isPersistant = true)]
        public double heliumMolarMassPerSquareMeterPerSecond;
        [KSPField(isPersistant = true)]
        public double solarWindMolesPerSquareMeterPerSecond;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionisation"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 0)]
        protected float ionizationPercentage;
        [KSPField(groupName = GROUP, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_magneticField"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 0)]
        protected float powerPercentage = 100;
        [KSPField(groupName = GROUP, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_pulsation"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 0)]
        protected float pulsatingPercentage = 100;

        // Part properties
        [KSPField(groupName = GROUP, guiActiveEditor = false, guiName = "#LOC_KSPIE_SolarwindCollector_surfaceArea", guiUnits = " m\xB2")]
        public double surfaceArea = 0; // Surface area of the panel.
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_magneticArea", guiUnits = " m\xB2")]
        public double magneticArea = 0; // Magnetic area of the panel.
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_effectiveness", guiFormat = "P1")]
        public double effectiveness = 1; // Effectiveness of the panel. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_mwRequirements", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double mwRequirements = 1; // MW requirements of the collector panel.
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_superConductingRatio", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double superConductingRatio = 0.05; // MW requirements of the collector panel.
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionRequirements", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
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
        public double solarWindSpeed = 5e5;              // Average Solar win speed 500 km/s
        [KSPField]
        public double avgSolarWindPerCubM = 6e6;        // various sources differ, most state that there are around 6 particles per cm^3, so around 6000000 per m^3 (some sources go up to 10/cm^3 or even down to 2/cm^3, most are around 6/cm^3).

        // GUI
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_effectiveSurfaceArea", guiFormat = "F3", guiUnits = " km\xB2")]
        protected double effectiveSurfaceAreaInSquareKiloMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_effectiveDiameter", guiFormat = "F3", guiUnits = " km")]
        protected double effectiveDiameterInKilometer;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarWindIons", guiUnits = " mol/m\xB2/s")]
        protected float fSolarWindConcentrationPerSquareMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarIons", guiUnits = " mol/m\xB2/s")] 
        protected float fInterstellarIonsPerSquareMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarParticles", guiUnits = " mol/m\xB3")]
        protected float fInterstellarIonsPerCubicMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphereParticles", guiUnits = " mol/m\xB3")]
        protected float fAtmosphereConcentration;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_neutralHydrogenConcentration", guiUnits = " mol/m\xB3")]
        protected float fNeutralHydrogenConcentration;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_neutralHeliumConcentration", guiUnits = " mol/m\xB3")]
        protected float fNeutralHeliumConcentration;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionizedHydrogenConcentration", guiUnits = " mol/m\xB3")]
        protected float fIonizedHydrogenConcentration;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionizedHeliumConcentration", guiUnits = " mol/m\xB3")]
        protected float fIonizedHeliumConcentration;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericIonsDensity", guiUnits = " kg/m\xB3")]
        protected float fAtmosphereIonsKgPerSquareMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericGasDensity", guiUnits = " kg/m\xB3")]
        protected float fAtmosphereGasKgPerSquareMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarIonsDensity", guiUnits = " kg/m\xB2")]
        protected float fInterstellarIonsKgPerSquareMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindMassDensity", guiUnits = " kg/m\xB2")]
        protected float fSolarWindKgPerSquareMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solidVesselDrag", guiUnits = " N/m\xB2")]
        protected float solidVesselDragInNewton;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericGasDrag", guiUnits = " N/m\xB2")]
        protected float fAtmosphericGasDragInNewton;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphericIonDrag", guiUnits = " N/m\xB2")]
        protected float fAtmosphericIonDragInNewton;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindDrag", guiUnits = " N/m\xB2")]
        protected float fSolarWindDragInNewtonPerSquareMeter;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarDrag", guiUnits = " N/m\xB2")]
        protected float fInterstellarDustDragInNewton;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_ionisationFacingFactor", guiFormat = "F3")]
        protected double ionizationFacingFactor;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_orbitalDragOnVessel", guiUnits = " kN")]
        protected float fEffectiveOrbitalDragInKiloNewton;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindForceOnVessel", guiUnits = " N")]
        protected float fSolarWindVesselForceInNewton;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_magnetosphereStrengthRatio", guiFormat = "F3")]
        protected double magnetoSphereStrengthRatio;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindFacingFactor", guiFormat = "F3")]
        protected double solarWindAngleOfAttackFactor = 0;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarwindCollectionModifier", guiFormat = "F3")]
        protected double solarwindProductionModifiers = 0;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarWindMassCollected", guiUnits = " g/h")]
        protected float fSolarWindCollectedGramPerHour;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarMassCollected", guiUnits = " g/h")]
        protected float fInterstellarIonsCollectedGramPerHour;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphereHydrogenCollected", guiUnits = " g/h")]
        protected float fHydrogenCollectedGramPerHour;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_atmosphereHeliumCollected", guiUnits = " g/h")]
        protected float fHeliumCollectedGramPerHour;

        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_distanceFromSun", guiFormat = "F1", guiUnits = " km")]
        protected double distanceToLocalStar;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_status")]
        protected string strCollectingStatus = "";
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_powerUsage")]
        protected string strReceivedPower = "";
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_magnetosphereShieldingStrength", guiUnits = " %")]
        protected string strMagnetoStrength = "";

        //[KSPField(guiActive = true, guiName = "Belt Radiation Flux")]
        //protected double beltRadiationFlux;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_verticalSpeed", guiFormat = "F1", guiUnits = " m/s")]
        protected double verticalSpeed;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_heliosphereFactor", guiFormat = "F3")]
        protected double heliosphereFactor;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_relativeSolarwindSpeed", guiFormat = "F3")]
        protected double relativeSolarWindSpeed;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_interstellarDensityFactor", guiFormat = "F3")]
        protected double interstellarDensityFactor;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_solarWindDensityFactor", guiFormat = "F3")]
        protected double solarwindDensityFactor;

        // internals
        double _dWindResourceFlow = 0;
        double _dHydrogenResourceFlow = 0;
        double _dHeliumResourceFlow = 0;
        double _heliumRequirementTonPerSecond;
        
        double _dSolarWindSpareCapacity;
        double _dHydrogenSpareCapacity;
        double _dShieldedEffectiveness = 0;

        double _effectiveIonizationFactor;
        double _effectiveNonIonizationFactor;
        float _newNormalTime;
        float _previousIonizationPercentage;

        Animation _deployAnimation;
        Animation _ionizationAnimation;
        CelestialBody _localStar;
        CelestialBody _homeWorld; 

        PartResourceDefinition _helium4GasResourceDefinition;
        PartResourceDefinition _lqdHelium4ResourceDefinition;
        PartResourceDefinition _hydrogenResourceDefinition;
        PartResourceDefinition _solarWindResourceDefinition;

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_activateCollector", active = true)]
        public void ActivateCollector()
        {
            Debug.Log("[KSPI]: SolarwindCollector on " + part.name + " was Force Activated");
            part.force_activate();

            bIsEnabled = true;
            OnUpdate();
            if (IsCollectLegal())
                UpdatePartAnimation();
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_SolarwindCollector_disableCollector", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            ionizationPercentage = 0;
            OnUpdate();

            // folding animation will only play if the collector was extended before being disabled
            if (bIsExtended)
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

        public bool IsIonizing => ionizationPercentage > 0;

        public override void OnStart(PartModule.StartState state)
        {
            // get the part's animation
            _deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            _ionizationAnimation = part.FindModelAnimators(ionAnimName).FirstOrDefault();
            _previousIonizationPercentage = ionizationPercentage;

            if (_ionizationAnimation != null)
            {
                _ionizationAnimation[ionAnimName].speed = 0;
                _ionizationAnimation[ionAnimName].normalizedTime = IsIonizing ? ionizationPercentage * 0.01f : 0; // normalizedTime at 1 is the end of the animation
                _ionizationAnimation.Blend(ionAnimName);
            }

            if (state == StartState.Editor) return; // collecting won't work in editor

            _heliumRequirementTonPerSecond = heliumRequirement * 1e-6 / GameConstants.SECONDS_IN_HOUR ;
            _helium4GasResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Helium4Gas);
            _lqdHelium4ResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdHelium4);
            _solarWindResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.SolarWind);
            _hydrogenResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen);

            _localStar = KopernicusHelper.GetLocalStar(vessel.mainBody);
            _homeWorld = FlightGlobals.GetHomeBody();

            // this bit goes through parts that contain animations and disables the "Status" field in GUI so that it's less crowded
            var genericAnimateList = part.FindModulesImplementing<ModuleAnimateGeneric>();
            foreach (ModuleAnimateGeneric mag in genericAnimateList)
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
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarwindCollector_PostMsg1"), 10, ScreenMessageStyle.LOWER_CENTER);//"Solar Wind Collection Error, vessel in atmosphere"
                return;
            }

            // if the part should be extended (from last time), go to the extended animation
            if (bIsExtended && _deployAnimation != null)
                _deployAnimation[animName].normalizedTime = 1;

            // calculate time difference since last time the vessel was active
            var dTimeDifference =  Math.Abs(Planetarium.GetUniversalTime() - dLastActiveTime);

            // increase buffer to allow processing
            var solarWindBuffer = part.Resources[_solarWindResourceDefinition.name];
            solarWindBuffer.maxAmount = 100 * part.mass * dTimeDifference;

            // collect solar wind for entire duration
            CollectSolarWind(dTimeDifference, true);
        }

        public override void OnUpdate()
        {
            _localStar = vessel.GetLocalStar();
            verticalSpeed = vessel.mainBody == _localStar ? vessel.verticalSpeed : 0;
            heliosphereFactor = Math.Min(1, CalculateHeliosphereRatio(vessel, _localStar, _homeWorld));
            interstellarDensityFactor = heliosphereFactor == 0 ? 0 : Math.Max(0, AtmosphericFloatCurves.Instance.InterstellarDensityRatio.Evaluate((float)heliosphereFactor * 100));
            solarwindDensityFactor = Math.Max(0, 1 - interstellarDensityFactor);
            relativeSolarWindSpeed = solarwindDensityFactor * (solarWindSpeed - verticalSpeed);

            solarWindMolesPerSquareMeterPerSecond = CalculateSolarwindIonMolesPerSquareMeter(avgSolarWindPerCubM * solarCheatMultiplier, vessel, Math.Abs(relativeSolarWindSpeed));
            interstellarDustMolesPerCubicMeter = CalculateInterstellarMoleConcentration(vessel, interstellarDensityCubeCm, interstellarDensityFactor);

            var maxInterstellarDustMolesPerSquareMeter = vessel.obt_speed * interstellarDustMolesPerCubicMeter;

            var currentInterstellarIonRatio = vessel.mainBody == _localStar 
                ? Math.Max(interstellarIonRatio, 1 - heliosphereFactor * heliosphereFactor) 
                : interstellarIonRatio;

            dInterstellarIonsConcentrationPerSquareMeter = maxInterstellarDustMolesPerSquareMeter * (_effectiveIonizationFactor * (1 - currentInterstellarIonRatio) + _effectiveNonIonizationFactor * currentInterstellarIonRatio);

            if (vessel.mainBody != _localStar)
            {
                var dAtmosphereConcentration = AtmosphericFloatCurves.CalculateCurrentAtmosphereConcentration(vessel);
                var dHydrogenParticleConcentration = CalculateCurrentHydrogenParticleConcentration(vessel);
                var dHeliumParticleConcentration = CalculateCurrentHeliumParticleConcentration(vessel);
                var dIonizedHydrogenConcentration = CalculateCurrentHydrogenIonsConcentration(vessel);
                var dIonizedHeliumConcentration = CalculateCurrentHeliumIonsConcentration(vessel);

                hydrogenMolarMassPerSquareMeterPerSecond = _effectiveIonizationFactor * dHydrogenParticleConcentration + _effectiveNonIonizationFactor * dIonizedHydrogenConcentration;
                heliumMolarMassPerSquareMeterPerSecond = _effectiveIonizationFactor * dHeliumParticleConcentration + _effectiveNonIonizationFactor * dIonizedHeliumConcentration;

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

            Events[nameof(ActivateCollector)].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events[nameof(DisableCollector)].active = bIsEnabled; // will show the button when the process IS enabled

            Fields[nameof(strReceivedPower)].guiActive = bIsEnabled;
            Fields[nameof(verticalSpeed)].guiActive = verticalSpeed > 0;
            Fields[nameof(heliosphereFactor)].guiActive = heliosphereFactor > 0;
            Fields[nameof(fInterstellarDustDragInNewton)].guiActive = fInterstellarDustDragInNewton > 0;
            Fields[nameof(fAtmosphereIonsKgPerSquareMeter)].guiActive = fAtmosphereIonsKgPerSquareMeter > 0;
            Fields[nameof(fAtmosphereGasKgPerSquareMeter)].guiActive = fAtmosphereGasKgPerSquareMeter > 0;
            Fields[nameof(fAtmosphereConcentration)].guiActive = fAtmosphereConcentration > 0;
            Fields[nameof(fNeutralHydrogenConcentration)].guiActive = fNeutralHydrogenConcentration > 0;
            Fields[nameof(fNeutralHeliumConcentration)].guiActive = fNeutralHeliumConcentration > 0;
            Fields[nameof(fIonizedHydrogenConcentration)].guiActive = fIonizedHydrogenConcentration > 0;
            Fields[nameof(fIonizedHeliumConcentration)].guiActive = fIonizedHeliumConcentration > 0;
            Fields[nameof(fSolarWindVesselForceInNewton)].guiActive = fSolarWindVesselForceInNewton > 0;
            Fields[nameof(fSolarWindKgPerSquareMeter)].guiActive = fSolarWindKgPerSquareMeter > 0;
            Fields[nameof(fAtmosphericIonDragInNewton)].guiActive = fAtmosphericIonDragInNewton > 0;
            Fields[nameof(fAtmosphericGasDragInNewton)].guiActive = fAtmosphericGasDragInNewton > 0;
            Fields[nameof(solidVesselDragInNewton)].guiActive = solidVesselDragInNewton > 0;
            Fields[nameof(solarwindProductionModifiers)].guiActive = solarwindProductionModifiers > 0;
            Fields[nameof(fSolarWindCollectedGramPerHour)].guiActive = fSolarWindCollectedGramPerHour > 0;
            Fields[nameof(fHydrogenCollectedGramPerHour)].guiActive = fHydrogenCollectedGramPerHour > 0;
            Fields[nameof(fHeliumCollectedGramPerHour)].guiActive = fHeliumCollectedGramPerHour > 0;
            Fields[nameof(fInterstellarIonsPerCubicMeter)].guiActive = fInterstellarIonsPerCubicMeter > 0;
            Fields[nameof(fInterstellarIonsPerSquareMeter)].guiActive = fInterstellarIonsPerSquareMeter > 0;
            Fields[nameof(fInterstellarIonsKgPerSquareMeter)].guiActive = fInterstellarIonsKgPerSquareMeter > 0;
            Fields[nameof(fEffectiveOrbitalDragInKiloNewton)].guiActive = fEffectiveOrbitalDragInKiloNewton > 0;
            Fields[nameof(fSolarWindDragInNewtonPerSquareMeter)].guiActive = fSolarWindDragInNewtonPerSquareMeter > 0;
            Fields[nameof(fSolarWindConcentrationPerSquareMeter)].guiActive = fSolarWindConcentrationPerSquareMeter > 0;
            Fields[nameof(fInterstellarIonsCollectedGramPerHour)].guiActive = fInterstellarIonsCollectedGramPerHour > 0;
        }

        public double SquareDragFactor => ((100d - pulsatingPercentage) / 100d) + squareVelocityDragRatio;

        public double CollectionRatio => pulsatingPercentage / 100d;

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            var solarWindBuffer = part.Resources[_solarWindResourceDefinition.name];
            if (solarWindBuffer != null)
                solarWindBuffer.maxAmount = 10 * part.mass * TimeWarp.fixedDeltaTime;

            UpdateIonizationAnimation();

            if (FlightGlobals.fetch == null) return;

            UpdateDistanceInGui(); // passes the distance to the GUI

            if (!bIsEnabled)
            {
                strCollectingStatus = Localizer.Format("#LOC_KSPIE_SolarwindCollector_Disabled");//"Disabled"
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

            // store current time in case vessel is unloaded
            dLastActiveTime = Planetarium.GetUniversalTime();
                
            // store current strength of the magnetic field in case vessel is unloaded
            dLastMagnetoStrength = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));
        }


        /* Calculates the strength of the magnetosphere. Will return 1 if in atmosphere, otherwise a ratio of max atmospheric altitude to current 
         * altitude - so the ratio slowly lowers the higher the vessel is. Once above 10 times the max atmosphere altitude, 
         * it returns 0 (we consider this to be the end of the magnetosphere reach). The atmospheric check is there to make the GUI less messy.
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
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarwindCollector_PostMsg2"), 10, ScreenMessageStyle.LOWER_CENTER);//"Solar wind collection not possible in low atmosphere"
                fSolarWindConcentrationPerSquareMeter = 0;
                return false;
            }
            else
                return true;
        }

        private void UpdateIonizationAnimation()
        {
            if (!IsIonizing)
            {
                _previousIonizationPercentage = ionizationPercentage;
                if (_ionizationAnimation == null) return;

                _ionizationAnimation[ionAnimName].speed = 0;
                _ionizationAnimation[ionAnimName].normalizedTime = 0;
                _ionizationAnimation.Blend(ionAnimName);
                return;
            }

            if (ionizationPercentage < _previousIonizationPercentage)
            {
                _newNormalTime = Math.Min(Math.Max(ionizationPercentage / 100, _previousIonizationPercentage / 100 - TimeWarp.fixedDeltaTime / 2), 1);
                if (_ionizationAnimation != null)
                {
                    _ionizationAnimation[ionAnimName].speed = 0;
                    _ionizationAnimation[ionAnimName].normalizedTime = _newNormalTime;
                    _ionizationAnimation.Blend(ionAnimName);
                }
                _previousIonizationPercentage = _newNormalTime * 100;
            }
            else if (ionizationPercentage > _previousIonizationPercentage)
            {
                _newNormalTime = Math.Min(Math.Max(0, _previousIonizationPercentage / 100 + TimeWarp.fixedDeltaTime / 2), ionizationPercentage / 100);
                if (_ionizationAnimation != null)
                {
                    _ionizationAnimation[ionAnimName].speed = 0;
                    _ionizationAnimation[ionAnimName].normalizedTime = _newNormalTime;
                    _ionizationAnimation.Blend(ionAnimName);
                }
                _previousIonizationPercentage = _newNormalTime * 100;
            }
            else
            {
                if (_ionizationAnimation == null) return;

                _ionizationAnimation[ionAnimName].speed = 0;
                _ionizationAnimation[ionAnimName].normalizedTime = ionizationPercentage / 100;
                _ionizationAnimation.Blend(ionAnimName);
            }
        }

        private void UpdatePartAnimation()
        {
            // if extended, plays the part folding animation
            if (bIsExtended)
            {
                if (_deployAnimation != null)
                {
                    _deployAnimation[animName].speed = -1; // speed of 1 is normal playback, -1 is reverse playback (so in this case we go from the end of animation backwards)
                    _deployAnimation[animName].normalizedTime = 1; // normalizedTime at 1 is the end of the animation
                    _deployAnimation.Blend(animName, part.mass);
                }
                bIsExtended = false;
            }
            else
            {
                // if folded, plays the part extending animation
                if (_deployAnimation != null)
                {
                    _deployAnimation[animName].speed = 1;
                    _deployAnimation[animName].normalizedTime = 0; // normalizedTime at 0 is the start of the animation
                    _deployAnimation.Blend(animName, part.mass);
                }
                bIsExtended = true;
            }
        }

        // calculates solar wind concentration
        private static double CalculateSolarwindIonMolesPerSquareMeter(double solarWindPerCubM, Vessel vessel, double solarWindSpeed)
        {
            var dMolarSolarConcentration = (vessel.solarFlux / GameConstants.averageKerbinSolarFlux) * solarWindPerCubM * solarWindSpeed / PhysicsGlobals.AvogadroConstant;

            return Math.Abs(dMolarSolarConcentration); // in mol / m2 / sec
        }

        private static double CalculateInterstellarMoleConcentration(Vessel vessel, double densityInterstellarDensityCubeCm , double interstellarDensity)
        {
            double dAverageInterstellarHydrogenPerCubM = densityInterstellarDensityCubeCm * 1e6;

            var interstellarHydrogenConcentration = dAverageInterstellarHydrogenPerCubM / PhysicsGlobals.AvogadroConstant;

            var interstellarDensityModifier = Math.Max(0, interstellarDensity);

            return interstellarHydrogenConcentration * interstellarDensityModifier; // in mol / m2 / sec
        }

        private static double CalculateHeliosphereRatio(Vessel vessel, CelestialBody localStar, CelestialBody homeworld)
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

            var comparableEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparableEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparableEarthAltitudeInKm <= 1000
                ? Math.Max(0, AtmosphericFloatCurves.Instance.ParticlesHydrogenCubePerMeter.Evaluate((float)comparableEarthAltitudeInKm))
                : 2.101e+13f * (1 / (Math.Pow(20 / radiusModifier, (comparableEarthAltitudeInKm - 1000) / 1000)));

            var atmosphereConcentration = atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        private static double CalculateCurrentHeliumParticleConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparableEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparableEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparableEarthAltitudeInKm <= 1000
                ? Math.Max(0, AtmosphericFloatCurves.Instance.ParticlesHeliumnPerCubePerCm.Evaluate((float)comparableEarthAltitudeInKm))
                : 8.196E+05f * (1 / (Math.Pow(20 / radiusModifier, (comparableEarthAltitudeInKm - 1000) / 1000)));

            var atmosphereConcentration = 1e+6 * atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        private static double CalculateCurrentHydrogenIonsConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparableEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparableEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparableEarthAltitudeInKm <= 1240
                ? Math.Max(0, AtmosphericFloatCurves.Instance.HydrogenIonsPerCubeCm.Evaluate((float)comparableEarthAltitudeInKm))
                : 6.46e+9f * (1 / (Math.Pow(20 / radiusModifier, (comparableEarthAltitudeInKm - 1240) / 1240)));

            var atmosphereConcentration = 1e+6 * atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        private static double CalculateCurrentHeliumIonsConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparableEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereParticlesPerCubM = comparableEarthAltitudeInKm > (64000 * radiusModifier) ? 0 : comparableEarthAltitudeInKm <= 1240
                ? Math.Max(0, AtmosphericFloatCurves.Instance.HydrogenIonsPerCubeCm.Evaluate((float)comparableEarthAltitudeInKm))
                : 6.46e+9f * (1 / (Math.Pow(20 / radiusModifier, (comparableEarthAltitudeInKm - 1240) / 1240)));

            var atmosphereConcentration = 0.5 * 1e+6 * atmosphereMultiplier * atmosphereParticlesPerCubM * vessel.obt_speed / PhysicsGlobals.AvogadroConstant;

            return float.IsInfinity((float)atmosphereConcentration) ? 0 : atmosphereConcentration;
        }

        private static double CalculateAtmosphereIonAtSolarMinimumDayTimeAtKgPerCubicMeter(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere || vessel.mainBody.atmosphereDepth <= 0)
                return 0;

            var comparableEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereMultiplier = vessel.mainBody.atmospherePressureSeaLevel / GameConstants.EarthAtmospherePressureAtSeaLevel;
            var radiusModifier = vessel.mainBody.Radius / GameConstants.EarthRadius;

            var atmosphereIonsPerCubCm = comparableEarthAltitudeInKm > (64000 * radiusModifier) ? 0
                : comparableEarthAltitudeInKm <= 1000
                    ? Math.Max(0, AtmosphericFloatCurves.Instance.IonSolarMinimumDayTimeCubeCm.Evaluate((float)comparableEarthAltitudeInKm))
                    : 1.17724306767693e+004f * (1 / (Math.Pow(20, (comparableEarthAltitudeInKm - 1000) / 1000)));

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
            if (_localStar == null)
                distanceToLocalStar = _homeWorld.orbit.semiMajorAxis;
            else
                distanceToLocalStar = ((CalculateDistanceToSun(part.transform.position, _localStar.transform.position) - _localStar.Radius) * 0.001);
        }

        private string UpdateMagnetoStrengthInGui()
        {
            return (GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) * 100).ToString("F1");
        }

        // the main collecting function
        private void CollectSolarWind(double deltaTimeInSeconds, bool offlineCollecting)
        {
            var ionizationPowerCost = IsIonizing ? ionRequirements * Math.Pow(ionizationPercentage * 0.01, 2) : 0;

            var magneticPulsatingPowerCost = mwRequirements * (pulsatingPercentage * 0.01);
            var magneticSuperconductorPowerReqCost = mwRequirements * superConductingRatio;

            var magneticPowerCost = (magneticPulsatingPowerCost + magneticSuperconductorPowerReqCost) * Math.Pow(powerPercentage * 0.01, 2);
            var dPowerRequirementsMw = powerReqMult * PluginHelper.PowerConsumptionMultiplier * (magneticPowerCost + ionizationPowerCost); // change the mwRequirements number in part config to change the power consumption
            var dWasteheatProductionMw = powerReqMult * PluginHelper.PowerConsumptionMultiplier * (magneticPulsatingPowerCost + magneticSuperconductorPowerReqCost * 0.05 + ionizationPowerCost * 0.3);

            // checks for free space in solar wind 'tanks'
            _dSolarWindSpareCapacity = part.GetResourceSpareCapacity(_solarWindResourceDefinition.name);
            _dHydrogenSpareCapacity = part.GetResourceSpareCapacity(_hydrogenResourceDefinition.name);

            if (solarWindMolesPerSquareMeterPerSecond > 0 || hydrogenMolarMassPerSquareMeterPerSecond > 0 || interstellarDustMolesPerCubicMeter > 0)
            {
                var requiredHeliumMass = deltaTimeInSeconds * _heliumRequirementTonPerSecond;

                var heliumGasRequired = requiredHeliumMass / _helium4GasResourceDefinition.density;
                var receivedHeliumGas = part.RequestResource(_helium4GasResourceDefinition.id, heliumGasRequired);
                var receivedHeliumGasMass = receivedHeliumGas * _helium4GasResourceDefinition.density;

                var massHeliumMassShortage = (requiredHeliumMass - receivedHeliumGasMass);

                var lqdHeliumRequired = massHeliumMassShortage / _lqdHelium4ResourceDefinition.density;
                var receivedLqdHelium = part.RequestResource(_lqdHelium4ResourceDefinition.id, lqdHeliumRequired);
                var receiverLqdHeliumMass = receivedLqdHelium * _lqdHelium4ResourceDefinition.density;

                var heliumRatio = Math.Min(1,  requiredHeliumMass > 0 ? (receivedHeliumGasMass + receiverLqdHeliumMass) / requiredHeliumMass : 0);

                // calculate available power
                var receivedPowerMw = consumeFNResourcePerSecond(dPowerRequirementsMw * heliumRatio, ResourceManager.FNRESOURCE_MEGAJOULES);

                // if power requirement sufficiently low, retreive power from KW source
                //if (dPowerRequirementsMw < 2 && revievedPowerMw <= dPowerRequirementsMw)
                //{
                //    var requiredKw = (dPowerRequirementsMw - revievedPowerMw) * 1000;
                //    var receivedKw = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, heliumRatio * requiredKw * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                //    revievedPowerMw += (receivedKw * 0.001);
                //}

                dLastPowerRatio = offlineCollecting ? dLastPowerRatio : (dPowerRequirementsMw > 0 ? receivedPowerMw / dPowerRequirementsMw : 0);

                supplyManagedFNResourcePerSecond(dWasteheatProductionMw * dLastPowerRatio, ResourceManager.FNRESOURCE_WASTEHEAT);

                // show in GUI
                strCollectingStatus = Localizer.Format("#LOC_KSPIE_SolarwindCollector_Collecting");//"Collecting solar wind"
            }
            else
            {
                dLastPowerRatio = 0;
                dPowerRequirementsMw = 0;
            }

            strReceivedPower = PluginHelper.getFormattedPowerString(dLastPowerRatio * dPowerRequirementsMw) + " / " +
                PluginHelper.getFormattedPowerString(dPowerRequirementsMw);

            // get the shielding effect provided by the magnetosphere
            magnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

            // if online collecting, get the old values instead (simplification for the time being)
            if (offlineCollecting)
                magnetoSphereStrengthRatio = dLastMagnetoStrength;

            if (Math.Abs(magnetoSphereStrengthRatio) < float.Epsilon)
                _dShieldedEffectiveness = 1;
            else
                _dShieldedEffectiveness = 1 - magnetoSphereStrengthRatio;

            var effectiveMagneticSurfaceAreaInSquareMeter = magneticArea * powerPercentage * 0.01;
            effectiveSurfaceAreaInSquareKiloMeter = effectiveMagneticSurfaceAreaInSquareMeter * 1e-6;
            effectiveDiameterInKilometer = 2 * Math.Sqrt(effectiveSurfaceAreaInSquareKiloMeter / Math.PI);

            Vector3d solarWindDirectionVector = _localStar.transform.position - vessel.transform.position;
            if (relativeSolarWindSpeed < 0)
                solarWindDirectionVector *= -1;

            solarWindAngleOfAttackFactor =  Math.Max(0, Vector3d.Dot(part.transform.up, solarWindDirectionVector.normalized) - 0.5) * 2;
            solarwindProductionModifiers = collectMultiplier * effectiveness * _dShieldedEffectiveness * dLastPowerRatio * solarWindAngleOfAttackFactor;

            var effectiveMagneticSurfaceAreaForCollection = effectiveMagneticSurfaceAreaInSquareMeter * CollectionRatio;

            var solarWindGramCollectedPerSecond = solarWindMolesPerSquareMeterPerSecond * solarwindProductionModifiers * effectiveMagneticSurfaceAreaForCollection * 1.9;
            var interstellarGramCollectedPerSecond = dInterstellarIonsConcentrationPerSquareMeter * effectiveMagneticSurfaceAreaForCollection * 1.9;

            // determines how much solar wind will be collected. Can be tweaked in part configs by changing the collector's effectiveness.
            var dSolarDustResourceChange = (solarWindGramCollectedPerSecond + interstellarGramCollectedPerSecond) * deltaTimeInSeconds * 1e-6 / _solarWindResourceDefinition.density;
            _dWindResourceFlow = -part.RequestResource(_solarWindResourceDefinition.id, -dSolarDustResourceChange);

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting && dSolarDustResourceChange > 0)
            {
                var strNumberFormat = dSolarDustResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarwindCollector_PostMsg3", dSolarDustResourceChange.ToString(strNumberFormat),_solarWindResourceDefinition.name), 10, ScreenMessageStyle.LOWER_CENTER);//"We collected <<1>> units of <<2>>
            }

            var dHydrogenCollectedPerSecond = hydrogenMolarMassPerSquareMeterPerSecond * effectiveMagneticSurfaceAreaForCollection;
            var dHydrogenResourceChange = dHydrogenCollectedPerSecond * deltaTimeInSeconds * 1e-6 / _hydrogenResourceDefinition.density;
            _dHydrogenResourceFlow = -part.RequestResource(_hydrogenResourceDefinition.id, -dHydrogenResourceChange);

            if (offlineCollecting && dHydrogenResourceChange > 0)
            {
                var strNumberFormat = dHydrogenResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarwindCollector_PostMsg3", dHydrogenResourceChange.ToString(strNumberFormat),_hydrogenResourceDefinition.name), 10, ScreenMessageStyle.LOWER_CENTER);//"We collected <<1>> units of <<2>> 
            }

            var dHeliumCollectedPerSecond = heliumMolarMassPerSquareMeterPerSecond * effectiveMagneticSurfaceAreaForCollection;
            var dHeliumResourceChange = dHeliumCollectedPerSecond * deltaTimeInSeconds * 1e-6 / _helium4GasResourceDefinition.density;
            _dHeliumResourceFlow = -part.RequestResource(_helium4GasResourceDefinition.id, -dHeliumResourceChange);

            if (offlineCollecting && dHeliumResourceChange > 0)
            {
                var strNumberFormat = dHeliumResourceChange > 100 ? "0" : "0.000";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarwindCollector_PostMsg3", _dHeliumResourceFlow.ToString(strNumberFormat),_helium4GasResourceDefinition.name), 10, ScreenMessageStyle.LOWER_CENTER);//"We collected <<1>> units of <<2>>
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

            ionizationFacingFactor = Math.Max(0, Vector3d.Dot(part.transform.up, part.vessel.obt_velocity.normalized) - 0.5) * 2;

            var atmosphereModifier = Math.Max(0, 1 - Math.Pow(vessel.atmDensity, 0.2));

            var heliosphereIonizationBonus = heliosphereFactor > 0.5 && heliosphereFactor < 1 ?  Math.Pow(1 - heliosphereFactor, 0.25) : 0;

            _effectiveIonizationFactor = ionizationFacingFactor * atmosphereModifier * Math.Max(heliosphereIonizationBonus, Math.Pow(ionizationPercentage * 0.01, 2));
            _effectiveNonIonizationFactor = 1 - _effectiveIonizationFactor;

            var effectiveSolidVesselDrag = !bIsExtended ? 0 : surfaceArea * vessel.obt_speed * vessel.obt_speed * (atmosphericGasKgPerSquareMeter + interstellarDustKgPerSquareMeter);

            var cosAngleToGravityVector = 1 - Math.Abs(Vector3d.Dot(part.vessel.gravityTrue.normalized, part.vessel.obt_velocity.normalized));

            var effectiveMagneticAtmosphereDrag = cosAngleToGravityVector * (_effectiveIonizationFactor * atmosphericGasDragInNewton + _effectiveNonIonizationFactor * atmosphericIonDragInNewton);
            var effectiveInterstellarDrag = interstellarDustDragInNewton * (_effectiveIonizationFactor + _effectiveNonIonizationFactor * interstellarIonRatio);

            var dEffectiveOrbitalVesselDragInNewton = effectiveSolidVesselDrag + effectiveMagneticSurfaceAreaInSquareMeter * (effectiveMagneticAtmosphereDrag + effectiveInterstellarDrag);

            var solarDustKgPerSquareMeter = solarWindMolesPerSquareMeterPerSecond * 1e-3 * 1.9;
            var solarWindDragInNewtonPerSquareMeter = solarDustKgPerSquareMeter + (SquareDragFactor * Math.Abs(relativeSolarWindSpeed) * solarDustKgPerSquareMeter);
            var dSolarWindVesselForceInNewton = solarWindDragInNewtonPerSquareMeter * effectiveMagneticSurfaceAreaInSquareMeter;

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
            fSolarWindDragInNewtonPerSquareMeter = (float)solarWindDragInNewtonPerSquareMeter;
            fSolarWindVesselForceInNewton = (float)dSolarWindVesselForceInNewton;

            if (offlineCollecting)
                return;

            var totalVesselMassInKg = part.vessel.totalMass * 1000;
            if (totalVesselMassInKg <= 0)
                return;

            var universalTime = Planetarium.GetUniversalTime();
            if (universalTime <= 0)
                return;

            var orbitalDragDeltaVv = deltaTimeInSeconds * part.vessel.obt_velocity.normalized * -dEffectiveOrbitalVesselDragInNewton / totalVesselMassInKg;
            var solarPushDeltaVv = deltaTimeInSeconds * solarWindDirectionVector.normalized * -fSolarWindVesselForceInNewton / totalVesselMassInKg;
            TimeWarp.GThreshold = 2;

            if (!orbitalDragDeltaVv.x.IsInfinityOrNaN() && !orbitalDragDeltaVv.y.IsInfinityOrNaN() && !orbitalDragDeltaVv.z.IsInfinityOrNaN())
            {
                if (vessel.packed)
                    vessel.orbit.Perturb(orbitalDragDeltaVv, universalTime);
                else
                    vessel.ChangeWorldVelocity(orbitalDragDeltaVv);
            }
            else
                Debug.LogError("[KSPI]: Illegal orbitalDragDeltaVv: " + orbitalDragDeltaVv);

            if (!solarPushDeltaVv.x.IsInfinityOrNaN() && !solarPushDeltaVv.y.IsInfinityOrNaN() && !solarPushDeltaVv.z.IsInfinityOrNaN())
            {
                if (vessel.packed)
                    vessel.orbit.Perturb(solarPushDeltaVv, universalTime);
                else
                    vessel.ChangeWorldVelocity(solarPushDeltaVv);
            }
            else
                Debug.LogError("[KSPI]: Illegal solarPushDeltaVv: " + orbitalDragDeltaVv);
        }

        public override int getPowerPriority()
        {
            return 4;
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();

            sb.Append("Deployed Surface Area: ").Append(surfaceArea.ToString("F2")).AppendLine("m\xB2");
            sb.Append("Deployed Magnetic Area: ").Append((magneticArea * 1e-6).ToString("F2")).AppendLine("km\xB2");

            return sb.ToStringAndRelease();
        }
    }
}

