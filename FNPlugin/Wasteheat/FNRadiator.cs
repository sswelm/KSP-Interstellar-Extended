using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    [KSPModule("Radiator")]
    class StackFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class FlatFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
	class FNRadiator : FNResourceSuppliableModule	
    {
        // persitant
        [KSPField(isPersistant = true, guiActive = true, guiName = "Radiator Cooling"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts= UI_Scene.All)]
		public bool radiatorIsEnabled = false;
        [KSPField(isPersistant = true, guiActive = false)]
        public bool canRadiateHeat = true;
        [KSPField(isPersistant = true)]
        public bool radiatorInit;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Automated"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool isAutomated = true;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Pivot"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool pivotEnabled = true;

        [KSPField(isPersistant = false)]
        public bool showColorHeat = true;
        [KSPField(isPersistant = true)]
        public bool showRetractButton = false;
        [KSPField(isPersistant = true)]
        public bool showControls = true;

        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk1 = 1850;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk2 = 2200;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk3 = 2616;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk4 = 3111;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk5 = 3700;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Vacuum Temp", guiFormat = "F0", guiUnits = "K")]
        public float maxVacuumTemperature = 3700;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Atmosphere Temp", guiFormat = "F0", guiUnits = "K")]
        public float maxAtmosphereTemperature = 1200;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Current Temp", guiFormat = "F0", guiUnits = "K")]
        public double maxCurrentTemperature = 1200;

        [KSPField(isPersistant = false)]
        public string radiatorTypeMk1 = "NaK Loop Radiator";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk2 = "Mo Li Heat Pipe Mk1";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk3 = "Mo Li Heat Pipe Mk2";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk4 = "Graphene Radiator Mk1";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk5 = "Graphene Radiator Mk2";

        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk2 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk3 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk4 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk5 = null;

        [KSPField(isPersistant = false, guiActive = false)]
        public string surfaceAreaUpgradeTechReq = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public float surfaceAreaUpgradeMult = 1.6f;

        // non persistant
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass;
		[KSPField(isPersistant = false)]
		public bool isDeployable = false;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Converction Bonus")]
		public float convectiveBonus = 1;
		[KSPField(isPersistant = false)]
		public string animName;
        [KSPField(isPersistant = false)]
        public string thermalAnim;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public float temperatureColorDivider = 1;
        [KSPField(isPersistant = false)]
        public float emissiveColorPower = 4;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public string colorHeat = "_EmissiveColor";
        [KSPField(isPersistant = false, guiActive = false)]
        public double dynamic_pressure;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Type")]
		public string radiatorType;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Rad Temp")]
		public string radiatorTempStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Part Temp")]
        public string partTempStr;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Surface Area", guiFormat = "F2", guiUnits = " m2")]
        public double radiatorArea = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Eff Surface Area", guiFormat = "F2", guiUnits = " m2")]
        public double effectiveRadiativeArea = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float areaMultiplier = 3;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Effective Area", guiFormat = "F2", guiUnits = " m2")]
        public double effectiveRadiatorArea;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Radiated")]
		public string thermalPowerDissipStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Convected")]
		public string thermalPowerConvStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Rad Upgrade Cost")]
		public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Radiator Start Temp")]
        public double radiator_temperature_temp_val;
        [KSPField(isPersistant = false, guiActive = false)]
        public double instantaneous_rad_temp;
        [KSPField(isPersistant = false, guiActive = false, guiName = "WasteHeat Ratio")]
        public double wasteheatRatio;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Energy Transfer", guiFormat = "F2")]
        private double _maxEnergyTransfer;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Radiator Temperature", guiFormat = "F0")]
        public float maxRadiatorTemperature = 3700;

        [KSPField(isPersistant = false)]
        public float atmosphereToleranceModifier = 1;

        const float rad_const_h = 1000;
        const String kspShader = "KSP/Emissive/Bumped Specular";

        private Queue<double> temperatureQueue = new Queue<double>(10);

		protected Animation deployAnim;
		protected double radiatedThermalPower;
		protected double convectedThermalPower;
		
		protected float directionrotate = 1;
        protected long update_count = 0;
		protected int explode_counter = 0;

        protected int nrAvailableUpgradeTechs;
        //protected bool doLegacyGraphics;

        private BaseEvent deployRadiatorEvent;
        private BaseEvent retractRadiatorEvent;

        private Color emissiveColor;
        private CelestialBody star;
        private Renderer[] renderArray;
        private AnimationState[] heatStates;
        private ModuleDeployableRadiator _moduleDeployableRadiator;
        private ModuleActiveRadiator _moduleActiveRadiator;

        private bool active;
        private bool hasSurfaceAreaUpgradeTechReq;
        private List<IPowerSource> list_of_thermal_sources;

        private static List<FNRadiator> list_of_all_radiators = new List<FNRadiator>();

        public GenerationType CurrentGenerationType { get; private set; }

        public ModuleActiveRadiator ModuleActiveRadiator { get { return _moduleActiveRadiator; } }

        public float MaxRadiatorTemperature
        {
            get
            {
                return GetMaximumTemperatureForGen((int)CurrentGenerationType);
            }
        }

        private float GetMaximumTemperatureForGen(int generation)
        {
            if (generation == 4)
                return radiatorTemperatureMk5;
            else if (generation == 3)
                return radiatorTemperatureMk4;
            else if (generation == 2)
                return radiatorTemperatureMk3;
            else if (generation == 1)
                return radiatorTemperatureMk2;
            else
                return radiatorTemperatureMk1;
        }

        public double EffectiveRadiatorArea
        {
            get 
            {
                effectiveRadiativeArea = areaMultiplier * radiatorArea;

                return hasSurfaceAreaUpgradeTechReq 
                    ? effectiveRadiativeArea * surfaceAreaUpgradeMult 
                    : effectiveRadiativeArea; 
            }
        }

        private void DetermineGenerationType()
        {
            // check if we have SurfaceAreaUpgradeTechReq 
            hasSurfaceAreaUpgradeTechReq = PluginHelper.upgradeAvailable(surfaceAreaUpgradeTechReq);

            // determine number of upgrade techs
            nrAvailableUpgradeTechs = 1;
            if (PluginHelper.upgradeAvailable(PluginHelper.RadiatorUpgradeTech3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(PluginHelper.RadiatorUpgradeTech2))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(PluginHelper.RadiatorUpgradeTech1))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(PluginHelper.RadiatorUpgradeTech0))
                nrAvailableUpgradeTechs++;

            // determine fusion tech levels
            if (nrAvailableUpgradeTechs == 5)
                CurrentGenerationType = GenerationType.Mk5;
            else if (nrAvailableUpgradeTechs == 4)
                CurrentGenerationType = GenerationType.Mk4;
            else if (nrAvailableUpgradeTechs == 3)
                CurrentGenerationType = GenerationType.Mk3;
            else if (nrAvailableUpgradeTechs == 2)
                CurrentGenerationType = GenerationType.Mk2;
            else
                CurrentGenerationType = GenerationType.Mk1;
        }

        private string RadiatorType
        {
            get
            {
                if (CurrentGenerationType == GenerationType.Mk5)
                    return radiatorTypeMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    return radiatorTypeMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    return radiatorTypeMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    return radiatorTypeMk2;
                else
                    return radiatorTypeMk1;
            }
        }

		public static List<FNRadiator> getRadiatorsForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);

            List<FNRadiator> list_of_radiators_for_vessel = new List<FNRadiator>();
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
					list_of_radiators_for_vessel.Add (radiator);
			}
			return list_of_radiators_for_vessel;
		}

		public static bool hasRadiatorsForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);

			bool has_radiators = false;
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
					has_radiators = true;
			}
			return has_radiators;
		}

		public static double getAverageRadiatorTemperatureForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);

            //double average_temp = 0;
            //int n_radiators = 0;

            //foreach (FNRadiator radiator in list_of_all_radiators.Where(r => r.vessel == vess))
            //{
            //    average_temp += radiator.GetAverateRadiatorTemperature();
            //    n_radiators += 1;
            //}

            //if (n_radiators > 0) 
            //    average_temp = average_temp / n_radiators;
            //else 
            //    average_temp = 0;

            //return average_temp;

            var filteredList = list_of_all_radiators.Where(r => r.vessel == vess).ToList();

            if (filteredList.Count == 0)
                return 3700;

            //return list_of_all_radiators.Max(r => r.radiator_temperature_temp_val);

            return filteredList.Max(r => r.GetAverateRadiatorTemperature());
		}

        public static float getAverageMaximumRadiatorTemperatureForVessel(Vessel vess) 
        {
            list_of_all_radiators.RemoveAll(item => item == null);
            float average_temp = 0;
            float n_radiators = 0;

            foreach (FNRadiator radiator in list_of_all_radiators) 
            {
                if (radiator.vessel == vess) 
                {
                    average_temp += radiator.maxRadiatorTemperature;
                    n_radiators += 1.0f;
                }
            }

            if (n_radiators > 0) 
                average_temp = average_temp / n_radiators;
            else 
                average_temp = 0;

            return average_temp;
        }

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Deploy Radiator", active = true)]
		public void DeployRadiator() 
        {
            isAutomated = false;

            UnityEngine.Debug.Log("[KSPI] - DeployRadiator Called ");

            Deploy();
		}

        private void Deploy()
        {
            UnityEngine.Debug.Log("[KSPI] - Deploy Called ");

            if (part.ShieldedFromAirstream)
                return;

            if (_moduleDeployableRadiator != null)
                _moduleDeployableRadiator.Extend();

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Activate();

            radiatorIsEnabled = true;

            if (deployAnim == null) return;

            deployAnim[animName].enabled = true;
            deployAnim[animName].speed = 0.5f;
            deployAnim[animName].normalizedTime = 0f;
            deployAnim.Blend(animName, 2f);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Retract Radiator", active = true)]
		public void RetractRadiator() 
        {
            if (!isDeployable) return;

            isAutomated = false;

            Retract();
		}

        private void Retract()
        {
            UnityEngine.Debug.Log("[KSPI] - Retract Called ");

            if (_moduleDeployableRadiator != null)
            {
                _moduleDeployableRadiator.hasPivot = true;
                _moduleDeployableRadiator.Retract();
            }

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Shutdown();

            radiatorIsEnabled = false;

            if (deployAnim == null) return;

            deployAnim[animName].enabled = true;
            deployAnim[animName].speed = -0.5f;
            deployAnim[animName].normalizedTime = 1f;
            deployAnim.Blend(animName, 2f);
        }

		[KSPAction("Deploy Radiator")]
		public void DeployRadiatorAction(KSPActionParam param) 
        {
            UnityEngine.Debug.Log("[KSPI] - DeployRadiatorAction Called ");
            DeployRadiator();
		}

		[KSPAction("Retract Radiator")]
		public void RetractRadiatorAction(KSPActionParam param) 
        {
			RetractRadiator();
		}

		[KSPAction("Toggle Radiator")]
		public void ToggleRadiatorAction(KSPActionParam param) 
        {
            if (radiatorIsEnabled)
                RetractRadiator();
            else
            {
                UnityEngine.Debug.Log("[KSPI] - ToggleRadiatorAction Called ");
                DeployRadiator();
            }
		}

        public override void OnStart(StartState state)
        {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resources_to_supply;

            base.OnStart(state);

            radiatedThermalPower = 0;
            convectedThermalPower = 0;
            CurrentRadiatorTemperature = 0;
            directionrotate = 1;
            update_count = 0;
            explode_counter = 0;

            DetermineGenerationType();

            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            effectiveRadiatorArea = EffectiveRadiatorArea;

            deployRadiatorEvent = Events["DeployRadiator"];
            retractRadiatorEvent = Events["RetractRadiator"];

            Actions["DeployRadiatorAction"].guiName = Events["DeployRadiator"].guiName = "Deploy Radiator";
            Actions["ToggleRadiatorAction"].guiName = String.Format("Toggle Radiator");

            Actions["RetractRadiatorAction"].guiName = "Retract Radiator";
            Events["RetractRadiator"].guiName = "Retract Radiator";

			// calculate WasteHeat Capacity
			var wasteheatPowerResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);
			if (wasteheatPowerResource != null)
			{
				var wasteheat_ratio = Math.Min(wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount, 0.95);
				wasteheatPowerResource.maxAmount = part.mass * 2.0e+4 * wasteHeatMultiplier;
				wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * wasteheat_ratio;
			}

            var myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                partMass = part.mass;
                Fields["partMass"].guiActiveEditor = true;
                Fields["partMass"].guiActive = true;
                Fields["convectiveBonus"].guiActiveEditor = true;
            }

            if (!String.IsNullOrEmpty(thermalAnim))
            {
                heatStates = SetUpAnimation(thermalAnim, this.part);
                SetHeatAnimationRatio(0);
            }
            //doLegacyGraphics = heatStates == null || (part.name.StartsWith("circradiator") || part.name.StartsWith("RadialRadiator") || (part.name.StartsWith("LargeFlatRadiator")));

            deployAnim = part.FindModelAnimators(animName).FirstOrDefault();
            if (deployAnim != null)
            {
                deployAnim[animName].layer = 1;
                deployAnim[animName].speed = 0;

                if (radiatorIsEnabled)
                    deployAnim[animName].normalizedTime = 1;
                else
                    deployAnim[animName].normalizedTime = 0;
            }

            _moduleDeployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            _moduleActiveRadiator = part.FindModuleImplementing<ModuleActiveRadiator>();
            if (_moduleActiveRadiator != null)
            {
                _moduleActiveRadiator.Events["Activate"].guiActive = false;
                _moduleActiveRadiator.Events["Shutdown"].guiActive = false;
            }

            BaseField radiatorfield = Fields["radiatorIsEnabled"];
            radiatorfield.guiActive = showControls;
            radiatorfield.guiActiveEditor = showControls;
            radiatorfield.OnValueModified += radiatorIsEnabled_OnValueModified;

            BaseField automatedfield = Fields["isAutomated"];
            automatedfield.guiActive = showControls;
            automatedfield.guiActiveEditor = showControls;

            BaseField pivotfield = Fields["pivotEnabled"];
            pivotfield.guiActive = showControls;
            pivotfield.guiActiveEditor = showControls;

            //if (_moduleDeployableRadiator == null && _moduleActiveRadiator != null && state != StartState.Editor && showControls)
            //{
            //    if (radiatorIsEnabled)
            //        Deploy();
            //    else
            //        Retract();
            //}

            _maxEnergyTransfer = radiatorArea * 1000 * areaMultiplier * (1 + ((int)CurrentGenerationType * 2));

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.maxEnergyTransfer = _maxEnergyTransfer;

            if (state == StartState.Editor) return;

            int depth = 0;
            star = FlightGlobals.currentMainBody;
            while (depth < 10 && star != null && star.GetTemperature(0) < 2000)
            {
                star = star.referenceBody;
                depth++;
            }
            if (star == null)
                star = FlightGlobals.Bodies[0];

            // find all thermal sources
            list_of_thermal_sources = vessel.FindPartModulesImplementing<IPowerSource>().Where(tc => tc.IsThermalSource).ToList();

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            if (state == PartModule.StartState.Docked)
            {
                base.OnStart(state);
                return;
            }

            // add to static list of all radiators
            FNRadiator.list_of_all_radiators.Add(this);

            renderArray = part.FindModelComponents<Renderer>().ToArray();

            if (radiatorInit == false)
                radiatorInit = true;

            maxRadiatorTemperature = MaxRadiatorTemperature;

            part.maxTemp = maxRadiatorTemperature;

            radiatorTempStr = maxRadiatorTemperature + "K";

            maxVacuumTemperature =  String.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? Math.Min(radiatorTemperatureMk3, maxRadiatorTemperature) :  Math.Min(maxVacuumTemperature, maxRadiatorTemperature);
            maxAtmosphereTemperature = String.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? Math.Min(radiatorTemperatureMk3, maxRadiatorTemperature) : Math.Min(maxAtmosphereTemperature, maxRadiatorTemperature); 
        }

        void radiatorIsEnabled_OnValueModified(object arg1)
        {
            UnityEngine.Debug.Log("[KSPI] - radiatorIsEnabled_OnValueModified " + arg1.ToString());

            isAutomated = false;

            if (radiatorIsEnabled)
                Deploy();
            else
                Retract();
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part)
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }

        public void Update()
        {
            partMass = part.mass;

            var isUndefined = _moduleDeployableRadiator == null 
                || _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDING 
                || _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTING;

            deployRadiatorEvent.active = showControls && !radiatorIsEnabled && isDeployable && isUndefined;
            retractRadiatorEvent.active = showControls && radiatorIsEnabled && isDeployable && isUndefined;
        }

        public override void OnUpdate() // is called while in flight
        {
            if (update_count > 8)
            {
                update_count = 0;

                Fields["thermalPowerConvStr"].guiActive = convectedThermalPower > 0;

                // synchronize states
                if (_moduleDeployableRadiator != null && pivotEnabled && showControls)
                {
                    if (_moduleDeployableRadiator.deployState == ModuleDeployableRadiator.DeployState.EXTENDED)
                        radiatorIsEnabled = true;
                    else if (_moduleDeployableRadiator.deployState == ModuleDeployableRadiator.DeployState.RETRACTED)
                        radiatorIsEnabled = false;
                }

                BaseField radiatorfield = Fields["radiatorIsEnabled"];
                radiatorfield.guiActive = showControls;
                radiatorfield.guiActiveEditor = showControls;

                BaseField automatedfield = Fields["isAutomated"];
                automatedfield.guiActive = showControls;
                automatedfield.guiActiveEditor = showControls;

                BaseField pivotfield = Fields["pivotEnabled"];
                pivotfield.guiActive = showControls;
                pivotfield.guiActiveEditor = showControls;

                if (radiatorIsEnabled && canRadiateHeat)
                {
                    thermalPowerDissipStr = PluginHelper.getFormattedPowerString(radiatedThermalPower, "0.0", "0.000");
                    thermalPowerConvStr = PluginHelper.getFormattedPowerString(convectedThermalPower, "0.0", "0.000");
                }
                else
                {
                    thermalPowerDissipStr = "disabled";
                    thermalPowerConvStr = "disabled";
                }

                radiatorTempStr = CurrentRadiatorTemperature.ToString("0.0") + "K / " + maxCurrentTemperature.ToString("0.0") + "K";

                partTempStr = part.temperature.ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";

                if (showColorHeat)
                    ColorHeat();
            }

            update_count++;
        }

        public override void OnFixedUpdate()
        {
            active = true;
            base.OnFixedUpdate();
        }

        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;

                if (!active)
                    base.OnFixedUpdate();

                effectiveRadiatorArea = EffectiveRadiatorArea;

                _maxEnergyTransfer = radiatorArea * 1000 * Math.Pow(1 + ((int)CurrentGenerationType), 1.5);

                if (_moduleActiveRadiator != null)
                    _moduleActiveRadiator.maxEnergyTransfer = _maxEnergyTransfer;

                double external_temperature = FlightGlobals.getExternalTemperature(vessel.transform.position);

                // get resource bar ratio at start of frame
                wasteheatRatio = getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT);

                if (Double.IsNaN(wasteheatRatio))
                    Debug.LogWarning("FNRadiator: FixedUpdate Single.IsNaN detected in wasteheatRatio");

                var normalized_atmosphere = Math.Min(vessel.atmDensity, 1);

                maxCurrentTemperature = maxAtmosphereTemperature * Math.Max(normalized_atmosphere, 0) + maxVacuumTemperature * Math.Max(Math.Min(1 - vessel.atmDensity, 1), 0);

                radiator_temperature_temp_val = external_temperature + Math.Min((maxRadiatorTemperature - external_temperature) * Math.Sqrt(wasteheatRatio), maxCurrentTemperature - external_temperature);

                var efficiency = 1 - Math.Pow(1 - wasteheatRatio, 400);
                double delta_temp = Math.Max(radiator_temperature_temp_val - Math.Max(external_temperature * normalized_atmosphere, 2.7), 0);

                if (radiatorIsEnabled)
                {
                    if (!CheatOptions.IgnoreMaxTemperature && wasteheatRatio >= 1 && CurrentRadiatorTemperature >= maxRadiatorTemperature)
                    {
                        explode_counter++;
                        if (explode_counter > 25)
                            part.explode();
                    }
                    else
                        explode_counter = 0;

                    double thermal_power_dissip_per_second = efficiency * Math.Pow(delta_temp, 4) * GameConstants.stefan_const * effectiveRadiatorArea / 1e6;

                    if (Double.IsNaN(thermal_power_dissip_per_second))
                        Debug.LogWarning("FNRadiator: FixedUpdate Single.IsNaN detected in fixed_thermal_power_dissip");

                    if (canRadiateHeat)
                        radiatedThermalPower = consumeWasteHeatPerSecond(thermal_power_dissip_per_second);
                    else
                        radiatedThermalPower = 0;

                    if (Double.IsNaN(radiatedThermalPower))
                        Debug.LogError("FNRadiator: FixedUpdate Single.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + thermal_power_dissip_per_second + ")");

                    instantaneous_rad_temp = Math.Max(radiator_temperature_temp_val, Math.Max(FlightGlobals.getExternalTemperature(vessel.altitude, vessel.mainBody), 2.7));

                    if (Double.IsNaN(instantaneous_rad_temp))
                        Debug.LogError("FNRadiator: FixedUpdate Single.IsNaN detected in instantaneous_rad_temp after reading external temperature");

                    CurrentRadiatorTemperature = instantaneous_rad_temp;

                    if (_moduleDeployableRadiator)
                        _moduleDeployableRadiator.hasPivot = pivotEnabled;
                }
                else
                {
                    double thermal_power_dissip_per_second = efficiency * Math.Pow(Math.Max(delta_temp - external_temperature, 0), 4) * GameConstants.stefan_const * effectiveRadiatorArea / 0.5e7;

                    if (canRadiateHeat)
                        radiatedThermalPower = consumeWasteHeatPerSecond(thermal_power_dissip_per_second);
                    else
                        radiatedThermalPower = 0;
                    
                    instantaneous_rad_temp = Math.Max(radiator_temperature_temp_val, Math.Max(FlightGlobals.getExternalTemperature(vessel.altitude, vessel.mainBody), 2.7));

                    CurrentRadiatorTemperature = instantaneous_rad_temp;
                }

                if (vessel.atmDensity > 0)
                {
                    double pressure = vessel.atmDensity;
                    dynamic_pressure = 0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325;
                    pressure += dynamic_pressure;

                    double convection_delta_temp = Math.Max(0, CurrentRadiatorTemperature - external_temperature);
                    double conv_power_dissip = efficiency * pressure * convection_delta_temp * effectiveRadiatorArea * rad_const_h / 1e6 * convectiveBonus;

                    if (!radiatorIsEnabled)
                        conv_power_dissip = conv_power_dissip / 2;

                    if (canRadiateHeat)
                        convectedThermalPower = consumeWasteHeatPerSecond(conv_power_dissip);
                    else
                        convectedThermalPower = 0;

                    if (update_count == 6)
                        DeployMentControl(dynamic_pressure);
                }
                else
                {
                    convectedThermalPower = 0;

                    if (!radiatorIsEnabled && isAutomated && canRadiateHeat && showControls && update_count == 6)
                    {
                        UnityEngine.Debug.Log("[KSPI] - FixedUpdate Automated Deplotment ");
                        Deploy();
                    }
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNReactor.FixedUpdate" + e.Message);
            }
        }

        private void DeployMentControl(double dynamic_pressure)
        {
            if (dynamic_pressure > 0 && (atmosphereToleranceModifier * dynamic_pressure / 1.4854428818159e-3 * 100) > 100)
            {
                if (isDeployable && radiatorIsEnabled)
                {
                    if (isAutomated)
                    {
                        UnityEngine.Debug.Log("[KSPI] - DeployMentControl Auto Retracted");
                        Retract();
                    }
                    else
                    {
                        if (!CheatOptions.UnbreakableJoints)
                        {
                            UnityEngine.Debug.Log("[KSPI] - DeployMentControl Decoupled!");
                            part.deactivate();
                            part.decouple(1);
                        }
                    }
                }
            }
            else if (!radiatorIsEnabled && isAutomated && canRadiateHeat && showControls && !part.ShieldedFromAirstream)
            {
                UnityEngine.Debug.Log("[KSPI] - DeployMentControl Auto Deploy");
                Deploy();
            }
        }

        public float GetAverageTemperatureofOfThermalSource(List<IPowerSource> active_thermal_sources)
        {
            return maxRadiatorTemperature;
        }

        private double consumeWasteHeatPerSecond(double wasteheatToConsume)
        {
            if (radiatorIsEnabled)
            {
                var consumedWasteheat = CheatOptions.IgnoreMaxTemperature || wasteheatToConsume == 0
                    ? wasteheatToConsume 
                    : consumeFNResourcePerSecond(wasteheatToConsume, FNResourceManager.FNRESOURCE_WASTEHEAT);

                if (Double.IsNaN(consumedWasteheat))
                    return 0;
                    
                return consumedWasteheat;
            }

            return 0;
        }

        public bool hasTechsRequiredToUpgrade()
        {
            if (HighLogic.CurrentGame == null) return false;

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return true;

            return false;
        }



        protected double current_rad_temp;
		public double CurrentRadiatorTemperature 
        {
            get 
            {
			    return current_rad_temp;
            }
            set
            {
                current_rad_temp = value;
                temperatureQueue.Enqueue(current_rad_temp);
                if (temperatureQueue.Count > 10)
                    temperatureQueue.Dequeue();
            }
		}

        public double GetAverateRadiatorTemperature()
        {
            return temperatureQueue.Count > 0 ? temperatureQueue.Average() : current_rad_temp;
        }

		public override string GetInfo() 
        {
            DetermineGenerationType();

            effectiveRadiatorArea = EffectiveRadiatorArea;

            var sb = new StringBuilder();

            sb.Append(String.Format("Maximum Waste Heat Radiated\nMk1: {0} MW\n\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk1, 4) / 1e6f));

            if (!String.IsNullOrEmpty(upgradeTechReqMk2))
                sb.Append(String.Format("Mk2: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk2, 4) / 1e6f));
            if (!String.IsNullOrEmpty(upgradeTechReqMk3))
                sb.Append(String.Format("Mk3: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk3, 4) / 1e6f));
            if (!String.IsNullOrEmpty(upgradeTechReqMk4))
                sb.Append(String.Format("Mk4: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk4, 4) / 1e6f));
            if (!String.IsNullOrEmpty(upgradeTechReqMk5))
                sb.Append(String.Format("Mk5: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk5, 4) / 1e6f));

            return sb.ToString();
        }

        public override int getPowerPriority() 
        {
            return 3;
        }

        private void SetHeatAnimationRatio (float colorRatio )
        {
            if (heatStates != null)
            {
                foreach (AnimationState anim in heatStates)
                {
                    anim.normalizedTime = colorRatio;
                }
                return;
            }

        }

        private void ColorHeat()
        {
                double currentTemperature = CurrentRadiatorTemperature;
                float partTempRatio = (float)Math.Min((part.temperature / maxRadiatorTemperature), 1);
                float radiatorTempRatio = (float)Math.Min(currentTemperature / maxRadiatorTemperature, 1);

                try
                {
                    if (heatStates == null && !string.IsNullOrEmpty(colorHeat))
                    {
                        var colorRatioRed = (float)Math.Pow(Math.Max(partTempRatio, radiatorTempRatio) / temperatureColorDivider, emissiveColorPower);
                        var colorRatioGreen = (float)Math.Pow(Math.Max(partTempRatio, radiatorTempRatio) / temperatureColorDivider, emissiveColorPower * 2) * 0.6f;
                        var colorRatioBlue = (float)Math.Pow(Math.Max(partTempRatio, radiatorTempRatio) / temperatureColorDivider, emissiveColorPower * 4) * 0.3f;

                        SetHeatAnimationRatio(radiatorTempRatio);

                        emissiveColor = new Color(colorRatioRed, colorRatioGreen, colorRatioBlue, (float)wasteheatRatio);

                        foreach (Renderer renderer in renderArray)
                        {
                            if (renderer.material.shader.name != kspShader)
                                renderer.material.shader = Shader.Find(kspShader);

                            if (part.name.StartsWith("circradiator"))
                            {
                                if (renderer.material.GetTexture("_Emissive") == null)
                                    renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/circradiatorKT/texture1_e", false));

                                if (renderer.material.GetTexture("_BumpMap") == null)
                                    renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/circradiatorKT/texture1_n", false));
                            }
                            else if (part.name.StartsWith("RadialRadiator"))
                            {
                                if (renderer.material.GetTexture("_Emissive") == null)
                                    renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/RadialHeatRadiator/d_glow", false));
                            }
                            else if (part.name.StartsWith("LargeFlatRadiator"))
                            {
                                if (renderer.material.GetTexture("_Emissive") == null)
                                    renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/LargeFlatRadiator/glow", false));

                                if (renderer.material.GetTexture("_BumpMap") == null)
                                    renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/LargeFlatRadiator/radtex_n", false));
                            }

                            renderer.material.SetColor(colorHeat, emissiveColor);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI] - FNReactor.ColorHeat tail" + e.Message);
                }
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title;
        }
	}
}