using FNPlugin.Power;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin  
{
    class AtmosphericIntake : PartModule
    {
        // persistents
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_AtmosphericIntake_Airrate", guiActiveEditor = false, guiActive = true, guiFormat = "F5")]//Air / sec
        public double finalAir;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AtmosphericIntake_AtmosphericIntake"), UI_Toggle(disabledText = "#LOC_KSPIE_AtmosphericIntake_AtmosphericIntake_Closed", enabledText = "#LOC_KSPIE_AtmosphericIntake_AtmosphericIntake_Open", affectSymCounterparts = UI_Scene.None)] //Atmospheric Intake   Closed  Open                                                          //Mass Ratio
        public bool intakeOpen = true;

        [KSPField]
        public double atmosphereDensityMultiplier = 1; //0.001292 / 0.005;
        [KSPField]
        public double intakeSpeed = 10;
        [KSPField]
        public string intakeTransformName = "";

        [KSPField(guiName = "Vessel Speed", guiUnits = "m/s")]
        public double vesselSpeed;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_AtmosphereFlow", guiActive = true, guiUnits = " U", guiFormat = "F3"  )]//Atmosphere Flow
        public double airFlow;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_AtmosphereSpeed", guiActive = false, guiUnits = " m/s", guiFormat = "F3")]//Atmosphere Speed
        public double airSpeed;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_AirThisUpdate", guiActive = false, guiFormat ="F6")]//Air This Update
        public double airThisUpdate;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_IntakeAngle",  guiActive = true, guiFormat = "F3")]//Intake Angle
        public float intakeAngle = 0;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_Area", guiActiveEditor = true, guiActive = true, guiFormat = "F4")]//Area
        public double area = 0.01;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_unitScalar", guiActive = false, guiActiveEditor = true)]//unitScalar
        public double unitScalar = 0.2;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_storesResource", guiActive = false, guiActiveEditor = false)]//storesResource
        public bool storesResource = false;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_IntakeExposure", guiActive = false, guiActiveEditor = false, guiFormat = "F1")]//Intake Exposure
        public double intakeExposure = 0;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_UpperAtmoDensity", guiActive = false, guiActiveEditor = false, guiFormat = "F3")]//Trace atmo. density
        public double upperAtmoDensity;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_AirDensity", guiActive = false,   guiFormat = "F3")]//Air Density
        public double airDensity;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_TechBonus", guiActive = true, guiActiveEditor = true, guiFormat = "F3")]//Tech Bonus
        public double jetTechBonus;
        [KSPField(guiName = "#LOC_KSPIE_AtmosphericIntake_UpperAtmoFraction", guiActive = false, guiFormat = "F3")]//Upper Atmo Fraction
        public double upperAtmoFraction;

        [KSPField]
        public string JetUpgradeTech1;
        [KSPField]
        public string JetUpgradeTech2;
        [KSPField]
        public string JetUpgradeTech3;
        [KSPField]
        public string JetUpgradeTech4;
        [KSPField]
        public string JetUpgradeTech5;

        [KSPField]
        public bool hasJetUpgradeTech1;
        [KSPField]
        public bool hasJetUpgradeTech2;
        [KSPField]
        public bool hasJetUpgradeTech3;
        [KSPField]
        public bool hasJetUpgradeTech4;
        [KSPField]
        public bool hasJetUpgradeTech5;

        private double startupCount;

        PartResourceDefinition _resourceAtmosphereDefinition;
        ModuleResourceIntake _moduleResourceIntake;
        ResourceBuffers resourceBuffers;

        // this property will be accessed by the atmospheric extractor
        public double FinalAir
        {
            get { return finalAir; }
        }

        // property getter for the sake of seawater extractor
        public bool IntakeEnabled
        {
            get { return intakeOpen && (_moduleResourceIntake != null ? _moduleResourceIntake.intakeEnabled : true); }
        }

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("[KSPI]: AtmosphericIntake OnStart Reading PluginHelper Upgrades");

            JetUpgradeTech1 = PluginHelper.JetUpgradeTech1;
            JetUpgradeTech2 = PluginHelper.JetUpgradeTech2;
            JetUpgradeTech3 = PluginHelper.JetUpgradeTech3;
            JetUpgradeTech4 = PluginHelper.JetUpgradeTech4;
            JetUpgradeTech5 = PluginHelper.JetUpgradeTech5;

            hasJetUpgradeTech1 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech1);
            hasJetUpgradeTech2 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech2);
            hasJetUpgradeTech3 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech3);
            hasJetUpgradeTech4 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech4);
            hasJetUpgradeTech5 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech5);

            var jetTech = Convert.ToInt32(hasJetUpgradeTech1) * 1.2f + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f * Convert.ToInt32(hasJetUpgradeTech3) + 2.0736f * Convert.ToInt32(hasJetUpgradeTech4) + 2.48832f * Convert.ToInt32(hasJetUpgradeTech5);
            jetTechBonus = 5 * (1 + (jetTech / 9.92992f));

            _moduleResourceIntake = this.part.FindModulesImplementing<ModuleResourceIntake>().FirstOrDefault(m => m.resourceName == InterstellarResourcesConfiguration._INTAKE_AIR);
            _resourceAtmosphereDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);

            if (_moduleResourceIntake == null)
                Debug.LogWarning("[KSPI]: ModuleResourceIntake with IntakeAir is missing on " + part.partInfo.title);

            var field = Fields["intakeOpen"];
            var flightToggle = field.uiControlFlight as UI_Toggle;
            var editorToggle = field.uiControlEditor as UI_Toggle;

            flightToggle.onFieldChanged = IntakeOpenChanged;
            editorToggle.onFieldChanged = IntakeOpenChanged;

            UpdateResourceIntakeConfiguration();

            if (state == StartState.Editor) return; // don't do any of this stuff in editor

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere, 300, area * unitScalar * 100));
            resourceBuffers.Init(this.part);
        }

        private void IntakeOpenChanged(BaseField field, object oldFieldValueObj)
        {
            if (_moduleResourceIntake == null)
                return;

            if (intakeOpen)
                _moduleResourceIntake.Activate();
            else
                _moduleResourceIntake.Deactivate();
        }

        private void UpdateResourceIntakeConfiguration()
        {
            if (_moduleResourceIntake == null)
                return;

            area = _moduleResourceIntake.area;
            intakeTransformName = _moduleResourceIntake.intakeTransformName;
            unitScalar = _moduleResourceIntake.unitScalar;
            intakeSpeed = _moduleResourceIntake.intakeSpeed;
        }

        public void FixedUpdate()
        {
            if (vessel == null) // No vessel? No collecting
                return;

            if (!vessel.mainBody.atmosphere) // No atmosphere? No collecting
                return;

            IntakeThatAir(); // collect intake atmosphere for the timeframe
        }

        private void UpdateAtmosphereBuffer()
        {
            if (intakeOpen && resourceBuffers != null)
            {
                resourceBuffers.UpdateBuffers();
            }
        }

        public void IntakeThatAir()
        {
            UpdateAtmosphereBuffer();

            // do not return anything when intakes are closed
            if (_moduleResourceIntake != null)
            {
                if (!_moduleResourceIntake.intakeEnabled)
                {
                    ResetVariables();

                    intakeOpen = false;

                    UpdateAtmosphereAmount();

                    return;
                }
                else
                {
                    intakeOpen = true;
                }
            }
            else if (intakeOpen == false)
            {
                ResetVariables();

                UpdateAtmosphereAmount();
                return;
            }

            var vesselFlyingVector = vessel.altitude < part.vessel.mainBody.atmosphereDepth * 0.5 
                ? vessel.GetSrfVelocity() 
                : vessel.GetObtVelocity();

            vesselSpeed = vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL
                ? vessel.obt_speed 
                : vessel.speed;

            intakeAngle = Mathf.Clamp(Vector3.Dot(vesselFlyingVector.normalized, part.transform.up.normalized), 0, 1);
            airSpeed = (intakeAngle * vesselSpeed * unitScalar) + (intakeSpeed * jetTechBonus);
            intakeExposure = airSpeed * area;

            airFlow = atmosphereDensityMultiplier * vessel.atmDensity * intakeExposure / _resourceAtmosphereDefinition.density;
            airThisUpdate = airFlow * TimeWarp.fixedDeltaTime;

            if (part.vessel.atmDensity < PluginHelper.MinAtmosphericAirDensity && vessel.altitude < part.vessel.mainBody.scienceValues.spaceAltitudeThreshold) // only collect when it is possible and relevant
            {
                upperAtmoFraction = Math.Max(0, (vessel.altitude / (part.vessel.mainBody.scienceValues.spaceAltitudeThreshold))); // calculate the fraction of the atmosphere
                var spaceAirDensity = PluginHelper.MinAtmosphericAirDensity * (1 - upperAtmoFraction);             // calculate the space atmospheric density
                airDensity = Math.Max(part.vessel.atmDensity, spaceAirDensity);                             // display amount of density
                upperAtmoDensity = Math.Max(0, spaceAirDensity - part.vessel.atmDensity);                   // calculate effective addition upper atmosphere density
                var space_airFlow = intakeAngle * upperAtmoDensity * intakeExposure / _resourceAtmosphereDefinition.density; // how much of that air is our intake catching
                airThisUpdate = airThisUpdate + (space_airFlow * TimeWarp.fixedDeltaTime);                  // increase how much  air do we get per update 
            }

            if (startupCount > 10)
            {
                // take the final airThisUpdate value and assign it to the finalAir property (this will in turn get used by atmo extractor)
                finalAir = airThisUpdate / TimeWarp.fixedDeltaTime;
            }
            else
                startupCount++;

            UpdateAtmosphereAmount();
        }

        private void UpdateAtmosphereAmount()
        {
            if (!storesResource)
            {
                var _intake_atmosphere_resource = part.Resources[InterstellarResourcesConfiguration.Instance.IntakeAtmosphere];
                if (_intake_atmosphere_resource == null)
                    return;

                airThisUpdate = airThisUpdate >= 0
                    ? (airThisUpdate <= _intake_atmosphere_resource.maxAmount
                        ? airThisUpdate
                        : _intake_atmosphere_resource.maxAmount)
                    : 0;
                _intake_atmosphere_resource.amount = airThisUpdate;
            }
            else
                part.RequestResource(_resourceAtmosphereDefinition.id, -airThisUpdate, ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE); // create the resource, finally
        }

        private void ResetVariables()
        {
            airSpeed = 0;
            airThisUpdate = 0;
            finalAir = 0;
            intakeExposure = 0;
            airFlow = 0;
        }
    }
}
