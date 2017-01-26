using OpenResourceSystem;
using System;
using UnityEngine;

namespace FNPlugin  
{
    class AtmosphericIntake : PartModule
    {
        //protected Vector3 _intake_direction;
        protected PartResourceDefinition _resourceAtmosphere;




        //[KSPField(isPersistant = true)]
        //public double lastActiveTime;

        [KSPField(guiName = "Intake Speed", isPersistant = false, guiActive = true, guiFormat = "F3")]
        protected float _intake_speed;
        [KSPField(guiName = "Atmosphere Flow", guiUnits = "U", guiFormat = "F3", isPersistant = false, guiActive = false)]
        public double airFlow;
        [KSPField(guiName = "Atmosphere Speed", guiUnits = "M/s", guiFormat = "F3", isPersistant = false, guiActive = false)]
        public double airSpeed;
        [KSPField(guiName = "Air This Update", isPersistant = false, guiActive = true, guiFormat ="F6")]
        public double airThisUpdate;
        [KSPField(guiName = "intake Angle", isPersistant = false, guiActive = false)]
        public float intakeAngle = 0;

        [KSPField(guiName = "aoaThreshold", isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float aoaThreshold = 0.1f;
        [KSPField(isPersistant = false, guiName = "Area", guiActiveEditor = false, guiActive = false)]
        public double area = 0.01f;
        [KSPField(isPersistant = false)]
        public string intakeTransformName;
        [KSPField(isPersistant = false, guiName = "maxIntakeSpeed", guiActive = false, guiActiveEditor = false)]
        public float maxIntakeSpeed = 100;
        [KSPField(isPersistant = false, guiName = "unitScalar", guiActive = false, guiActiveEditor = false)]
        public double unitScalar = 0.2f;
		//[KSPField(isPersistant = false, guiName = "useIntakeCompensation", guiActiveEditor = false)]
		//public bool useIntakeCompensation = true;
        [KSPField(isPersistant = false, guiName = "storesResource", guiActiveEditor = true)]
        public bool storesResource = false;
        [KSPField(isPersistant = false, guiName = "Intake Exposure", guiActiveEditor = false, guiActive = false)]
        public double intakeExposure = 0;
        [KSPField(isPersistant = false, guiName = "Trace atmo. density", guiFormat ="P0", guiActiveEditor = false, guiActive = true)]
        public double upperAtmoDensity;
        [KSPField(guiName = "Air Density", isPersistant = false, guiActive = false, guiFormat = "F3")]
        public double airDensity;
        [KSPField(guiName = "Tech Bonus", isPersistant = false, guiActive = false, guiFormat = "F3")]
        public float jetTechBonusPercentage;
        [KSPField(guiName = "Max Atmo Altitude", isPersistant = false, guiActive = false, guiFormat = "F3")]
        public double maxAtmoAltitude;
        [KSPField(guiName = "Upper Atmo Fraction", isPersistant = false, guiActive = false, guiFormat = "F3")]
        public double upperAtmoFraction;

        // persistents
        [KSPField(isPersistant = true, guiName = "Final Air", guiActiveEditor = false, guiActive = true)]
        public double finalAir;

        public double startupCount;

        private ModuleResourceIntake _moduleResourceIntake;

        // this property will be accessed by the atmospheric extractor
        public double FinalAir
        {
            get { return finalAir; }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return; // don't do any of this stuff in editor

            _moduleResourceIntake = this.part.FindModuleImplementing<ModuleResourceIntake>();

            //Transform intakeTransform = part.FindModelTransform(intakeTransformName);
            //if (intakeTransform == null)
            //    Debug.Log("[KSPI] AtmosphericIntake unable to get intake transform for " + part.name);
            //_intake_direction = intakeTransform != null ? intakeTransform.forward.normalized : Vector3.forward;

            _resourceAtmosphere = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);

            // ToDo: connect with atmospheric intake to readout updated area
            // ToDo: change density of air to 

            _intake_speed = maxIntakeSpeed;

            bool hasJetUpgradeTech0 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech0);
            bool hasJetUpgradeTech1 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech1);
            bool hasJetUpgradeTech2 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech2);
            bool hasJetUpgradeTech3 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech3);

            var jetTechBonus = Convert.ToInt32(hasJetUpgradeTech0) + 1.2f * Convert.ToInt32(hasJetUpgradeTech1) + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f * Convert.ToInt32(hasJetUpgradeTech3);
            jetTechBonusPercentage = 1 + (jetTechBonus / 10.736f);

            // These are for offline collection capability. It's not really useful in the case of intakes, because they hold only very limited amount of the resource. As such, these are commented out (along with the persistent double lastActiveTime)
            //double timeDifference = (Planetarium.GetUniversalTime() - lastActiveTime) * 55; // why magical number 55? I don't know. It was in the old ISRUScoop class. It's a mystery.
            //IntakeThatAir(timeDifference, true); // collect intake atmosphere for the time we were away from this vessel
        }

        public void FixedUpdate()
        {
            if (vessel == null) // No vessel? No collecting
                return;

            if (!vessel.mainBody.atmosphere) // No atmosphere? No collecting
                return;

            //lastActiveTime = Planetarium.GetUniversalTime(); // store the current time in case the vessel is unloaded
            
            IntakeThatAir(TimeWarp.fixedDeltaTime, false); // collect intake atmosphere for the timeframe
        }

        public void IntakeThatAir(double deltaTimeInSecs, bool offlineCollecting)
        {
            airSpeed = vessel.speed + _intake_speed;
            intakeExposure = (airSpeed * unitScalar) + _intake_speed;
            intakeExposure *= area * unitScalar * jetTechBonusPercentage;
            airFlow = vessel.atmDensity * intakeExposure / _resourceAtmosphere.density;
            airThisUpdate = airFlow * TimeWarp.fixedDeltaTime;

            if (vessel.altitude > (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody))) // if this vessel is above atmosphere, it can still collect trace amounts of it
            {
                maxAtmoAltitude = PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody); // get the max atmospheric altitude for the current altitude
                upperAtmoFraction = Math.Max(0, (vessel.altitude - maxAtmoAltitude) / Math.Max(0.000001, maxAtmoAltitude * PluginHelper.MaxAtmosphericAltitudeMult - maxAtmoAltitude)); // calculate the fraction of the atmosphere
                upperAtmoDensity = 1 - upperAtmoFraction; // flip it around so that it gets lower as we rise up, away from the planet
                airDensity = part.vessel.atmDensity + (PluginHelper.MinAtmosphericAirDensity * upperAtmoDensity); // calculate the atmospheric density
                airFlow = airDensity * intakeExposure / _resourceAtmosphere.density; // how much of that air is our intake catching
                airThisUpdate = airFlow * TimeWarp.fixedDeltaTime; // how much air do we get per update
            }

            if (offlineCollecting == true) // if collecting offline, take the elapsed time into account, otherwise carry on
            {
                airThisUpdate *= deltaTimeInSecs;
                ScreenMessages.PostScreenMessage("The air intakes collected " + airThisUpdate.ToString("") + " units of " + (InterstellarResourcesConfiguration.Instance.IntakeAtmosphere), 3.0f, ScreenMessageStyle.LOWER_CENTER);
            }

            // do not return anything when intakes are closed
            if (_moduleResourceIntake != null && !_moduleResourceIntake.intakeEnabled)
            {
                airThisUpdate = 0;
                finalAir = 0;
                return;
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
                foreach (PartResource resource in part.Resources)
                {
                    if (resource.resourceName != _resourceAtmosphere.name)
                        continue;

                    airThisUpdate = airThisUpdate >= 0
                        ? (airThisUpdate <= resource.maxAmount
                            ? airThisUpdate
                            : resource.maxAmount)
                        : 0;
                    resource.amount = airThisUpdate;
                    break;
                }
            }
            else
            {
                part.RequestResource(_resourceAtmosphere.name, -airThisUpdate); // create the resource, finally
            }   
        }
    }
}
