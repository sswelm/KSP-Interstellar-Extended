using System;
using UnityEngine;

namespace FNPlugin  
{
    class AtmosphericIntake : PartModule
    {
        // persistents
        [KSPField(isPersistant = true, guiName = "Air / sec", guiActiveEditor = false, guiActive = true, guiFormat = "F5")]
        public double finalAir;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool intakeOpen;

        [KSPField]
        protected double _intake_speed;
        [KSPField(guiName = "Atmosphere Flow", guiActive = false, guiUnits = "U", guiFormat = "F3"  )]
        public double airFlow;
        [KSPField(guiName = "Atmosphere Speed", guiActive = false, guiUnits = "M/s", guiFormat = "F3")]
        public double airSpeed;
        [KSPField(guiName = "Air This Update", guiActive = false, guiFormat ="F6")]
        public double airThisUpdate;
        [KSPField(guiName = "Intake Angle",  guiActive = true, guiFormat = "F3")]
        public float intakeAngle = 0;
        [KSPField(guiName = "Area", guiActiveEditor = true, guiActive = false, guiFormat = "F3")]
        public double area = 0.01;
        [KSPField]
        public string intakeTransformName;
        [KSPField(guiName = "maxIntakeSpeed", guiActive = false, guiActiveEditor = false)]
        public double maxIntakeSpeed = 100;
        [KSPField(guiName = "unitScalar", guiActive = false, guiActiveEditor = false)]
        public double unitScalar = 0.2;
        [KSPField(guiName = "storesResource", guiActive = false, guiActiveEditor = true)]
        public bool storesResource = false;
        [KSPField(guiName = "Intake Exposure", guiActive = true, guiActiveEditor = false, guiFormat = "F1")]
        public double intakeExposure = 0;
        [KSPField(guiName = "Trace atmo. density", guiActive = false, guiActiveEditor = false, guiFormat = "F3")]
        public double upperAtmoDensity;
        [KSPField(guiName = "Air Density", guiActive = false,   guiFormat = "F3")]
        public double airDensity;
        [KSPField(guiName = "Tech Bonus", guiActive = false, guiFormat = "F3")]
        public double jetTechBonusPercentage;
        [KSPField(guiName = "Upper Atmo Fraction", guiActive = false, guiFormat = "F3")]
        public double upperAtmoFraction;

        double startupCount;
        float previousDeltaTime;
        double atmosphereBuffer;

        PartResourceDefinition _resourceAtmosphere;
        ModuleResourceIntake _moduleResourceIntake;

        // this property will be accessed by the atmospheric extractor
        public double FinalAir
        {
            get { return finalAir; }
        }

        // property getter for the sake of seawater extractor
        public bool IntakeEnabled
        {
          get { return this.part.FindModuleImplementing<ModuleResourceIntake>().intakeEnabled; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return; // don't do any of this stuff in editor

            bool hasJetUpgradeTech0 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech1);
            bool hasJetUpgradeTech1 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech2);
            bool hasJetUpgradeTech2 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech3);
            bool hasJetUpgradeTech3 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech4);

            var jetTechBonus = Convert.ToInt32(hasJetUpgradeTech0) + 1.2f * Convert.ToInt32(hasJetUpgradeTech1) + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f * Convert.ToInt32(hasJetUpgradeTech3);
            jetTechBonusPercentage = 10 * (1 + (jetTechBonus / 10.736f));

            _moduleResourceIntake = this.part.FindModuleImplementing<ModuleResourceIntake>();

            // if _moduleResourceIntake is null there SHOULD be an exception - and it's a good thing.
            area = _moduleResourceIntake.area;
            intakeTransformName = _moduleResourceIntake.intakeTransformName;
            unitScalar = _moduleResourceIntake.unitScalar;
 
            atmosphereBuffer = area * unitScalar * jetTechBonusPercentage * maxIntakeSpeed * 300 ;

            _resourceAtmosphere = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);
            _intake_speed = maxIntakeSpeed;
        }

        public void FixedUpdate()
        {
            if (vessel == null) // No vessel? No collecting
                return;

            if (!vessel.mainBody.atmosphere) // No atmosphere? No collecting
                return;

            IntakeThatAir(TimeWarp.fixedDeltaTime); // collect intake atmosphere for the timeframe
        }

        private void UpdateAtmosphereBuffer(bool intakesOpen)
        {
            var currentDeltaTime = intakesOpen ? TimeWarp.fixedDeltaTime : 0.02;

            var _intake_atmosphere_resource = part.Resources[InterstellarResourcesConfiguration.Instance.IntakeAtmosphere];
            if (_intake_atmosphere_resource != null && atmosphereBuffer > 0 && currentDeltaTime != previousDeltaTime)
            {
                double requiredAtmosphereCapacity = atmosphereBuffer * currentDeltaTime;
                double previousAtmosphereCapacity = atmosphereBuffer * previousDeltaTime;
                double atmosphereRatio = (_intake_atmosphere_resource.amount / _intake_atmosphere_resource.maxAmount);

                _intake_atmosphere_resource.maxAmount = requiredAtmosphereCapacity;

                _intake_atmosphere_resource.amount = currentDeltaTime > previousDeltaTime
                    ? Math.Max(0, Math.Min(requiredAtmosphereCapacity, _intake_atmosphere_resource.amount + requiredAtmosphereCapacity - previousAtmosphereCapacity))
                    : Math.Max(0, Math.Min(requiredAtmosphereCapacity, atmosphereRatio * requiredAtmosphereCapacity));
            }

            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }


        public void IntakeThatAir(double deltaTimeInSecs)
        {
            // do not return anything when intakes are closed
            if (_moduleResourceIntake != null && !_moduleResourceIntake.intakeEnabled)
            {
                airSpeed = 0;
                airThisUpdate = 0;
                finalAir = 0;
                intakeExposure = 0;
                airFlow = 0;
                intakeOpen = false;
                UpdateAtmosphereBuffer(false);
                return;
            }

            intakeOpen = true;
            UpdateAtmosphereBuffer(true);

            var vesselFlyingVector = vessel.altitude < part.vessel.mainBody.atmosphereDepth * 0.5 
                ? vessel.GetSrfVelocity() 
                : vessel.GetObtVelocity();

            var vesselSpeed = vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING 
                ? vessel.obt_speed 
                : vessel.speed;

            intakeAngle = Mathf.Clamp(Vector3.Dot(vesselFlyingVector.normalized, part.transform.up.normalized), 0, 1);
            airSpeed = intakeAngle * vesselSpeed + _intake_speed;
            intakeExposure = (airSpeed * unitScalar) + _intake_speed;
            intakeExposure *= area * unitScalar * jetTechBonusPercentage;
            airFlow = vessel.atmDensity * intakeExposure / _resourceAtmosphere.density;
            airThisUpdate = airFlow * TimeWarp.fixedDeltaTime;

            if (part.vessel.atmDensity < PluginHelper.MinAtmosphericAirDensity && vessel.altitude < part.vessel.mainBody.scienceValues.spaceAltitudeThreshold) // only collect when it is possible and relevant
            {
                upperAtmoFraction = Math.Max(0, (vessel.altitude / (part.vessel.mainBody.scienceValues.spaceAltitudeThreshold))); // calculate the fraction of the atmosphere
                var spaceAirDensity = PluginHelper.MinAtmosphericAirDensity * (1 - upperAtmoFraction);             // calculate the space atmospheric density
                airDensity = Math.Max(part.vessel.atmDensity, spaceAirDensity);                             // display amount of density
                upperAtmoDensity = Math.Max(0, spaceAirDensity - part.vessel.atmDensity);                   // calculate effective addition upper atmosphere density
                var space_airFlow = intakeAngle * upperAtmoDensity * intakeExposure / _resourceAtmosphere.density; // how much of that air is our intake catching
                airThisUpdate = airThisUpdate + (space_airFlow * TimeWarp.fixedDeltaTime);                  // increase how much  air do we get per update 
            }

            if (startupCount > 10)
            {
                // take the final airThisUpdate value and assign it to the finalAir property (this will in turn get used by atmo extractor)
                finalAir = airThisUpdate / TimeWarp.fixedDeltaTime;
            }
            else
                startupCount++;

            if (!storesResource)
            {
                var _intake_atmosphere_resource = part.Resources[InterstellarResourcesConfiguration.Instance.IntakeAtmosphere];
                airThisUpdate = airThisUpdate >= 0
                    ? (airThisUpdate <= _intake_atmosphere_resource.maxAmount
                        ? airThisUpdate
                        : _intake_atmosphere_resource.maxAmount)
                    : 0;
                _intake_atmosphere_resource.amount = airThisUpdate;
            }
            else
            {
                part.RequestResource(_resourceAtmosphere.id, -airThisUpdate, ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE); // create the resource, finally
            }


        }
    }
}
