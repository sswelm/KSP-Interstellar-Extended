using FNPlugin.Power;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    class RadiatorManager
    {
        private static Dictionary<Vessel, RadiatorManager> managers = new Dictionary<Vessel,RadiatorManager>();

        public static RadiatorManager Update(FNRadiator radiator)
        {
            RadiatorManager manager;

            managers.TryGetValue(radiator.vessel, out manager);

            if (manager == null || manager.UpdatingRadiator == null || (manager.UpdatingRadiator != radiator && manager.Counter < radiator.updateCounter))
                manager = CreateManager(radiator);

            if (manager != null && manager.UpdatingRadiator == radiator)
                manager.Update();

            return manager;
        }

        private static RadiatorManager CreateManager(FNRadiator radiator)
        {
            RadiatorManager manager = new RadiatorManager(radiator);

            managers[radiator.vessel] = manager;

            return manager;
        }

        private RadiatorManager(FNRadiator radiator)
        {
            UpdatingRadiator = radiator;

            // determine number of upgrade techs
            NrAvailableUpgradeTechs = 1;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech4))
                NrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech3))
                NrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech2))
                NrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech1))
                NrAvailableUpgradeTechs++;

            // determine fusion tech levels
            if (NrAvailableUpgradeTechs == 5)
                CurrentGenerationType = GenerationType.Mk5;
            else if (NrAvailableUpgradeTechs == 4)
                CurrentGenerationType = GenerationType.Mk4;
            else if (NrAvailableUpgradeTechs == 3)
                CurrentGenerationType = GenerationType.Mk3;
            else if (NrAvailableUpgradeTechs == 2)
                CurrentGenerationType = GenerationType.Mk2;
            else
                CurrentGenerationType = GenerationType.Mk1;

            MaxVacuumTemperatureTitanium = PluginHelper.RadiatorTemperatureMk3;
            if (CurrentGenerationType == GenerationType.Mk5)
                MaxVacuumTemperatureGraphene = PluginHelper.RadiatorTemperatureMk5;
            else if (CurrentGenerationType == GenerationType.Mk4)
                MaxVacuumTemperatureGraphene = PluginHelper.RadiatorTemperatureMk4;
            else if (CurrentGenerationType == GenerationType.Mk3)
                MaxVacuumTemperatureTitanium = MaxVacuumTemperatureGraphene = PluginHelper.RadiatorTemperatureMk3;
            else if (CurrentGenerationType == GenerationType.Mk2)
                MaxVacuumTemperatureTitanium = MaxVacuumTemperatureGraphene = PluginHelper.RadiatorTemperatureMk2;
            else
                MaxVacuumTemperatureTitanium = MaxVacuumTemperatureGraphene = PluginHelper.RadiatorTemperatureMk1;
        }

        public FNRadiator UpdatingRadiator { get; private set;}
        public GenerationType CurrentGenerationType { get; private set; }
        public int NrAvailableUpgradeTechs { get; private set; }
        public long Counter { get; private set; }
        public double WasteHeatRatio { get; private set; }
        public double MaxVacuumTemperatureGraphene { get; private set; }
        public double MaxVacuumTemperatureTitanium { get; private set; }

        private double external_temperature;

        public void Update()
        {
            Counter = UpdatingRadiator.updateCounter;

            WasteHeatRatio = UpdatingRadiator.getResourceBarRatio(ResourceManager.FNRESOURCE_WASTEHEAT);
            var sqrtWasteHeatRatio = Math.Sqrt(WasteHeatRatio);

            //var efficiency = 1 - Math.Pow(1 - WasteHeatRatio, 400);

            if (Double.IsNaN(WasteHeatRatio))
            {
                Debug.LogError("KSPI - FNRadiator: FixedUpdate Single.IsNaN detected in WasteHeatRatio");
                return;
            }
            external_temperature = FlightGlobals.getExternalTemperature(UpdatingRadiator.vessel.transform.position);
            var normalized_atmosphere = Math.Min(UpdatingRadiator.vessel.atmDensity, 1);

            // titanium radiator
            var radiator_temperature_temp_val_titanium = external_temperature + Math.Min((MaxVacuumTemperatureTitanium - external_temperature) * sqrtWasteHeatRatio, MaxVacuumTemperatureTitanium - external_temperature);

            // graphene radiator
            var atmosphereModifierVacuum = Math.Max(Math.Min(1 - UpdatingRadiator.vessel.atmDensity, 1), 0);
            var atmosphereModifierAtmosphere = Math.Max(normalized_atmosphere, 0);
            var maxCurrentTemperatureGraphene = 1200 * atmosphereModifierAtmosphere + MaxVacuumTemperatureGraphene * atmosphereModifierVacuum;
            var radiator_temperature_temp_val_graphene = external_temperature + Math.Min((MaxVacuumTemperatureGraphene - external_temperature) * sqrtWasteHeatRatio, maxCurrentTemperatureGraphene - external_temperature);
        }
    }


    [KSPModule("Radiator")]
    class StackFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class FlatFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class FNRadiator : ResourceSuppliableModule    
    {
        // persitant
        [KSPField(isPersistant = true, guiActive = true, guiName = "Radiator Cooling"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts= UI_Scene.All)]
        public bool radiatorIsEnabled = false;
        [KSPField(isPersistant = true)]
        public bool canRadiateHeat = true;
        [KSPField(isPersistant = true)]
        public bool radiatorInit;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Automated"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool isAutomated = true;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Pivot"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool pivotEnabled = true;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Prevent Shielded Deploy"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool preventShieldedDeploy = true;
        [KSPField(isPersistant = true)]
        public bool showRetractButton = false;
        [KSPField(isPersistant = true)]
        public bool showControls = true;

        // non persistant
        [KSPField(guiName = "Max Vacuum Temp", guiFormat = "F0", guiUnits = "K")]
        public float maxVacuumTemperature = 4400;
        [KSPField(guiName = "Max Atmosphere Temp", guiFormat = "F0", guiUnits = "K")]
        public float maxAtmosphereTemperature = 1200;
        [KSPField(guiName = "Max Current Temp", guiFormat = "F0", guiUnits = "K")]
        public double maxCurrentTemperature = 1200;
        [KSPField]
        public string radiatorTypeMk1 = "NaK Loop Radiator";
        [KSPField]
        public string radiatorTypeMk2 = "Mo Li Heat Pipe Mk1";
        [KSPField]
        public string radiatorTypeMk3 = "Mo Li Heat Pipe Mk2";
        [KSPField]
        public string radiatorTypeMk4 = "Graphene Radiator Mk1";
        [KSPField]
        public string radiatorTypeMk5 = "Graphene Radiator Mk2";
        [KSPField]
        public string radiatorTypeMk6 = "Graphene Radiator Mk3";
        [KSPField]
        public bool showColorHeat = true;
        [KSPField]
        public string surfaceAreaUpgradeTechReq = null;
        [KSPField]
        public double surfaceAreaUpgradeMult = 1.6;
        [KSPField(guiName = "Mass", guiUnits = " t")]
        public float partMass;
        [KSPField]
        public bool isDeployable = false;
        [KSPField(guiName = "Converction Bonus")]
        public float convectiveBonus = 1;
        [KSPField]
        public string animName = "";
        [KSPField]
        public string thermalAnim = "";
        [KSPField]
        public string originalName = "";
        [KSPField]
        public float upgradeCost = 100;
        [KSPField]
        public float temperatureColorDivider = 1;
        [KSPField]
        public float emissiveColorPower = 3;
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public string colorHeat = "_EmissiveColor";
        [KSPField(guiActive = false, guiName = "Atmosphere Modifier")]
        public double atmosphere_modifier;
        [KSPField(guiName = "Type")]
        public string radiatorType;
        [KSPField(guiActive = true, guiName = "Rad Temp")]
        public string radiatorTempStr;
        [KSPField(guiActive = true, guiName = "Part Temp")]
        public string partTempStr;
        [KSPField(guiActiveEditor = true, guiName = "Surface Area", guiFormat = "F2", guiUnits = " m\xB2")]
        public double radiatorArea = 1;
        [KSPField(guiName = "Eff Surface Area", guiFormat = "F2", guiUnits = " m\xB2")]
        public double effectiveRadiativeArea = 1;
        [KSPField]
        public double areaMultiplier = 1;
        [KSPField(guiName = "Effective Area", guiFormat = "F2", guiUnits = " m\xB2")]
        public double effectiveRadiatorArea;
        [KSPField(guiActive = true, guiName = "Power Radiated")]
        public string thermalPowerDissipStr;
        [KSPField(guiActive = true, guiName = "Power Convected")]
        public string thermalPowerConvStr;
        [KSPField(guiName = "Rad Upgrade Cost")]
        public string upgradeCostStr;
        [KSPField(guiName = "Radiator Start Temp")]
        public double radiator_temperature_temp_val;
        [KSPField]
        public double instantaneous_rad_temp;
        [KSPField(guiName = "WasteHeat Ratio")]
        public double wasteheatRatio;
        [KSPField(guiName = "Max Energy Transfer", guiFormat = "F2")]
        private double _maxEnergyTransfer;
        [KSPField(guiActiveEditor = true, guiName = "Max Radiator Temperature", guiFormat = "F0")]
        public float maxRadiatorTemperature = 4400;
        [KSPField(guiName = "Upgrade Techs")]
        public int nrAvailableUpgradeTechs;
        [KSPField(guiName = "Has Surface Upgrade")]
        public bool hasSurfaceAreaUpgradeTechReq;
        [KSPField]
        public float atmosphereToleranceModifier = 1;

        const string kspShaderLocation = "KSP/Emissive/Bumped Specular";
        const int RADIATOR_DELAY = 20;
        const int FRAME_DELAY = 9;
        const int DEPLOYMENT_DELAY = 6;

        // minimize garbage by recycling variablees
        private double stefanArea;
        private double thermalPowerDissipPerSecond;
        private double radiatedThermalPower;
        private double convectedThermalPower;

        [KSPField(guiActive = true, guiName = "Oxidation Modifier", guiFormat = "F2")]
        public double oxidationModifier;

        private double external_temperature;
        private double temperatureDifferenceCurrentWithExternal;
        private double temperatureDifferenceMaximumWithExternal;

        private bool active;
        private long update_count;
        private bool isGraphene;

        private int radiator_deploy_delay;
        private int explode_counter;

        private BaseEvent deployRadiatorEvent;
        private BaseEvent retractRadiatorEvent;

        private BaseField thermalPowerConvStrField;
        private BaseField radiatorIsEnabledField;
        private BaseField isAutomatedField;
        private BaseField pivotEnabledField;

        private Shader kspShader;
        private Renderer renderer;
        private Animation deployAnimation;
        private Color emissiveColor;
        private AnimationState anim;
        private Renderer[] renderArray;
        private AnimationState[] heatStates;
        private ModuleDeployableRadiator _moduleDeployableRadiator;
        private ModuleActiveRadiator _moduleActiveRadiator;
        private ModuleDeployablePart.DeployState radiatorState;
        private ResourceBuffers resourceBuffers;

        private Queue<double> temperatureQueue = new Queue<double>(10);

        private static Dictionary<Vessel, List<FNRadiator>> radiators_by_vessel = new Dictionary<Vessel, List<FNRadiator>>();

        private static List<FNRadiator> GetRadiatorsForVessel(Vessel vessel)
        {
            List<FNRadiator> vessel_radiator;
            if (radiators_by_vessel.TryGetValue(vessel, out vessel_radiator)) 
                return vessel_radiator;

            vessel_radiator = vessel.FindPartModulesImplementing<FNRadiator>().ToList();
            radiators_by_vessel.Add(vessel, vessel_radiator);

            return vessel_radiator;
        }

        public GenerationType CurrentGenerationType { get; private set; }

        public ModuleActiveRadiator ModuleActiveRadiator { get { return _moduleActiveRadiator; } }

        public double MaxRadiatorTemperature
        {
            get
            {
                return GetMaximumTemperatureForGen(CurrentGenerationType);
            }
        }

        private double GetMaximumTemperatureForGen(GenerationType generation)
        {
            if (generation == GenerationType.Mk6)
                return PluginHelper.RadiatorTemperatureMk6;
            if (generation == GenerationType.Mk5)
                return PluginHelper.RadiatorTemperatureMk5;
            if (generation == GenerationType.Mk4)
                return PluginHelper.RadiatorTemperatureMk4;
            if (generation == GenerationType.Mk3)
                return PluginHelper.RadiatorTemperatureMk3;
            if (generation == GenerationType.Mk2)
                return PluginHelper.RadiatorTemperatureMk2;
            return PluginHelper.RadiatorTemperatureMk1;
        }

        public double EffectiveRadiatorArea
        {
            get 
            {
                effectiveRadiativeArea = PluginHelper.RadiatorAreaMultiplier * areaMultiplier * radiatorArea;

                return hasSurfaceAreaUpgradeTechReq 
                    ? effectiveRadiativeArea * surfaceAreaUpgradeMult 
                    : effectiveRadiativeArea; 
            }
        }

        private void DetermineGenerationType()
        {
            // check if we have SurfaceAreaUpgradeTechReq 
            hasSurfaceAreaUpgradeTechReq = PluginHelper.UpgradeAvailable(surfaceAreaUpgradeTechReq);

            // determine number of upgrade techs
            nrAvailableUpgradeTechs = 1;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech5))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech4))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech2))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech1))
                nrAvailableUpgradeTechs++;

            // determine fusion tech levels
            if (nrAvailableUpgradeTechs == 6)
                CurrentGenerationType = GenerationType.Mk6;
            else if (nrAvailableUpgradeTechs == 5)
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
                if (CurrentGenerationType == GenerationType.Mk6)
                    return radiatorTypeMk6;
                if (CurrentGenerationType == GenerationType.Mk5)
                    return radiatorTypeMk5;
                if (CurrentGenerationType == GenerationType.Mk4)
                    return radiatorTypeMk4;
                if (CurrentGenerationType == GenerationType.Mk3)
                    return radiatorTypeMk3;
                if (CurrentGenerationType == GenerationType.Mk2)
                    return radiatorTypeMk2;
                return radiatorTypeMk1;
            }
        }

        public static void Reset()
        {
            radiators_by_vessel.Clear();
        }

        public static bool hasRadiatorsForVessel(Vessel vess) 
        {
            return GetRadiatorsForVessel(vess).Any();
        }

        public static double getAverageRadiatorTemperatureForVessel(Vessel vess) 
        {
            var radiator_vessel = GetRadiatorsForVessel(vess);

            if (radiator_vessel.Any())
                return radiator_vessel.Max(r => r.GetAverateRadiatorTemperature());
            else
                return 4400;
        }

        public static float getAverageMaximumRadiatorTemperatureForVessel(Vessel vess) 
        {
            var radiator_vessel = GetRadiatorsForVessel(vess);

            float average_temp = 0;
            float n_radiators = 0;

            foreach (FNRadiator radiator in radiator_vessel)
            {
                if (radiator == null) continue;

                average_temp += radiator.maxRadiatorTemperature;
                n_radiators += 1;
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

            Debug.Log("[KSPI] - DeployRadiator Called ");

            Deploy();
        }

        private void Deploy()
        {
            if (preventShieldedDeploy && (part.ShieldedFromAirstream || radiator_deploy_delay < RADIATOR_DELAY)) 
            {
                //Debug.Log("[KSPI] - Deploy Aborted, Part is shielded or nor ready");
                return;
            }

            Debug.Log("[KSPI] - Deploy Called ");

            if (_moduleDeployableRadiator != null)
                _moduleDeployableRadiator.Extend();

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Activate();

            radiatorIsEnabled = true;

            if (deployAnimation == null) return;

            deployAnimation[animName].enabled = true;
            deployAnimation[animName].speed = 0.5f;
            deployAnimation[animName].normalizedTime = 0f;
            deployAnimation.Blend(animName, 2);
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
            Debug.Log("[KSPI] - Retract Called ");

            if (_moduleDeployableRadiator != null)
            {
                _moduleDeployableRadiator.hasPivot = true;
                _moduleDeployableRadiator.Retract();
            }

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Shutdown();

            radiatorIsEnabled = false;

            if (deployAnimation == null) return;

            deployAnimation[animName].enabled = true;
            deployAnimation[animName].speed = -0.5f;
            deployAnimation[animName].normalizedTime = 1;
            deployAnimation.Blend(animName, 2);
        }

        [KSPAction("Deploy Radiator")]
        public void DeployRadiatorAction(KSPActionParam param) 
        {
            Debug.Log("[KSPI] - DeployRadiatorAction Called ");
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
                Debug.Log("[KSPI] - ToggleRadiatorAction Called ");
                DeployRadiator();
            }
        }

        public override void OnStart(StartState state)
        {
            String[] resourcesToSupply = { ResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resourcesToSupply;

            base.OnStart(state);

            radiatedThermalPower = 0;
            convectedThermalPower = 0;
            CurrentRadiatorTemperature = 0;
            update_count = 0;
            radiator_deploy_delay = 0;
            explode_counter = 0;

            DetermineGenerationType();

            kspShader = Shader.Find(kspShaderLocation);
            maxRadiatorTemperature = (float)MaxRadiatorTemperature;

            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            effectiveRadiatorArea = EffectiveRadiatorArea;
            stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            deployRadiatorEvent = Events["DeployRadiator"];
            retractRadiatorEvent = Events["RetractRadiator"];

            thermalPowerConvStrField = Fields["thermalPowerConvStr"];
            radiatorIsEnabledField = Fields["radiatorIsEnabled"];
            isAutomatedField = Fields["isAutomated"];
            pivotEnabledField = Fields["pivotEnabled"];

            var preventDeplyField = Fields["preventShieldedDeploy"];
            preventDeplyField.guiActive = isDeployable;
            preventDeplyField.guiActiveEditor = isDeployable;

            Actions["DeployRadiatorAction"].guiName = Events["DeployRadiator"].guiName = "Deploy Radiator";
            Actions["ToggleRadiatorAction"].guiName = String.Format("Toggle Radiator");
            Actions["RetractRadiatorAction"].guiName = "Retract Radiator";

            Events["RetractRadiator"].guiName = "Retract Radiator";

            var myAttachedEngine = part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                partMass = part.mass;
                Fields["partMass"].guiActiveEditor = true;
                Fields["partMass"].guiActive = true;
                Fields["convectiveBonus"].guiActiveEditor = true;
            }

            if (!String.IsNullOrEmpty(thermalAnim))
            {
                heatStates = PluginHelper.SetUpAnimation(thermalAnim, this.part);

                if (heatStates != null)
                    SetHeatAnimationRatio(0);
            }

            deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            if (deployAnimation != null)
            {
                deployAnimation[animName].layer = 1;
                deployAnimation[animName].speed = 0;

                deployAnimation[animName].normalizedTime = radiatorIsEnabled ? 1 : 0;
            }
          
            _moduleActiveRadiator = part.FindModuleImplementing<ModuleActiveRadiator>();
            if (_moduleActiveRadiator != null)
            {
                _moduleActiveRadiator.Events["Activate"].guiActive = false;
                _moduleActiveRadiator.Events["Shutdown"].guiActive = false;
            }
            _moduleDeployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            if (_moduleDeployableRadiator != null)
                radiatorState = _moduleDeployableRadiator.deployState;

            var radiatorfield = Fields["radiatorIsEnabled"];
            radiatorfield.guiActive = showControls;
            radiatorfield.guiActiveEditor = showControls;
            radiatorfield.OnValueModified += radiatorIsEnabled_OnValueModified;

            var automatedfield = Fields["isAutomated"];
            automatedfield.guiActive = showControls;
            automatedfield.guiActiveEditor = showControls;

            var pivotfield = Fields["pivotEnabled"];
            pivotfield.guiActive = showControls;
            pivotfield.guiActiveEditor = showControls;

            if (_moduleActiveRadiator != null)
            {
                var generationValue = 1 + ((int)CurrentGenerationType);
                _maxEnergyTransfer = radiatorArea * 2500 * Math.Pow(generationValue, 1.5);
                _moduleActiveRadiator.maxEnergyTransfer = _maxEnergyTransfer;
                _moduleActiveRadiator.overcoolFactor = 0.20 + ((int)CurrentGenerationType * 0.025);
            }

            if (state == StartState.Editor) return;

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            renderArray = part.FindModelComponents<Renderer>().ToArray();

            if (radiatorInit == false)
                radiatorInit = true;

            part.maxTemp = maxRadiatorTemperature;

            radiatorTempStr = maxRadiatorTemperature + "K";

            isGraphene = !String.IsNullOrEmpty(surfaceAreaUpgradeTechReq);

            maxVacuumTemperature = isGraphene ? Math.Min(maxVacuumTemperature, maxRadiatorTemperature) : Math.Min((float)PluginHelper.RadiatorTemperatureMk3, maxRadiatorTemperature);
            maxAtmosphereTemperature = isGraphene ? Math.Min(maxAtmosphereTemperature, maxRadiatorTemperature) : Math.Min((float)PluginHelper.RadiatorTemperatureMk3, maxRadiatorTemperature);

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 2.0e+6));
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.Init(this.part);
        }

        void radiatorIsEnabled_OnValueModified(object arg1)
        {
            Debug.Log("[KSPI] - radiatorIsEnabled_OnValueModified " + arg1);

            isAutomated = false;

            if (radiatorIsEnabled)
                Deploy();
            else
                Retract();
        }

        public void Update()
        {
            partMass = part.mass;

            var isDeployStateUndefined = _moduleDeployableRadiator == null 
                || _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDING 
                || _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTING;

            var canbeActive = showControls && isDeployable && isDeployStateUndefined;

            deployRadiatorEvent.active = canbeActive && !radiatorIsEnabled;
            retractRadiatorEvent.active = canbeActive && radiatorIsEnabled;
        }

        public override void OnUpdate() // is called while in flight
        {
            update_count++;
            radiator_deploy_delay++;

            //if (update_count < FRAME_DELAY)
            //    return;

            update_count = 0;

            if  (_moduleDeployableRadiator != null && (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED ||
                                                       _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)) {
                if (radiatorState != _moduleDeployableRadiator.deployState) 
                {
                    part.SendMessage("GeometryPartModuleRebuildMeshData");
                    Debug.Log("[KSPI] - Updating geometry mesh due to radiator deployment.");
                }
                radiatorState = _moduleDeployableRadiator.deployState;
            }

            stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            external_temperature = Math.Max(vessel.externalTemperature, PhysicsGlobals.SpaceTemperature);

            oxidationModifier = 0;

            if (vessel.mainBody.atmosphereContainsOxygen)
            {
                oxidationModifier = Math.Sqrt(vessel.staticPressurekPa + vessel.dynamicPressurekPa * 0.1) * 0.1;

                var spaceRatiatorBonus = (maxVacuumTemperature - maxAtmosphereTemperature) * (1 - oxidationModifier);
                if (spaceRatiatorBonus < 0)
                    spaceRatiatorBonus = -Math.Sqrt(Math.Abs(spaceRatiatorBonus));

                maxCurrentTemperature = Math.Max(0, maxAtmosphereTemperature + spaceRatiatorBonus);
            }
            else
                maxCurrentTemperature = maxVacuumTemperature;

            temperatureDifferenceCurrentWithExternal = maxCurrentTemperature - external_temperature;
            temperatureDifferenceMaximumWithExternal = maxRadiatorTemperature - external_temperature;

            thermalPowerConvStrField.guiActive = convectedThermalPower > 0;

            // synchronize states
            if (_moduleDeployableRadiator != null && pivotEnabled && showControls)
            {
                if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)
                    radiatorIsEnabled = true;
                else if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED)
                    radiatorIsEnabled = false;
            }

            radiatorIsEnabledField.guiActive = showControls;
            radiatorIsEnabledField.guiActiveEditor = showControls;

            isAutomatedField.guiActive = showControls && isDeployable;
            isAutomatedField.guiActiveEditor = showControls && isDeployable;

            pivotEnabledField.guiActive = showControls && isDeployable;
            pivotEnabledField.guiActiveEditor = showControls && isDeployable;

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
                ApplyColorHeat();
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

                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, radiatorIsEnabled ? this.part.mass : this.part.mass * 1e-3);
                resourceBuffers.UpdateBuffers();

                // get resource bar ratio at start of frame
                ResourceManager wasteheatManager = getManagerForVessel(ResourceManager.FNRESOURCE_WASTEHEAT);
                wasteheatRatio = wasteheatManager.ResourceBarRatioBegin;

                if (Double.IsNaN(wasteheatRatio))
                {
                    Debug.LogError("[KSPI] - FNRadiator: FixedUpdate Single.IsNaN detected in wasteheatRatio");
                    return;
                }

                radiator_temperature_temp_val = external_temperature + Math.Min(temperatureDifferenceMaximumWithExternal * wasteheatManager.SqrtResourceBarRatioBegin, temperatureDifferenceCurrentWithExternal);

                var deltaTemp = Math.Max(radiator_temperature_temp_val - Math.Max(external_temperature * Math.Min(1, vessel.atmDensity), PhysicsGlobals.SpaceTemperature), 0);
                var deltaTempToPowerFour = deltaTemp * deltaTemp * deltaTemp * deltaTemp;

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

                    thermalPowerDissipPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * stefanArea;

                    if (Double.IsNaN(thermalPowerDissipPerSecond))
                        Debug.LogWarning("[KSPI] - FNRadiator: FixedUpdate Single.IsNaN detected in thermalPowerDissipPerSecond");

                    radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(thermalPowerDissipPerSecond, wasteheatManager) : 0;

                    if (Double.IsNaN(radiatedThermalPower))
                        Debug.LogError("[KSPI] - FNRadiator: FixedUpdate Single.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + thermalPowerDissipPerSecond + ")");

                    instantaneous_rad_temp = CalculateInstantaniousRadTemp(external_temperature);

                    CurrentRadiatorTemperature = instantaneous_rad_temp;

                    if (_moduleDeployableRadiator)
                        _moduleDeployableRadiator.hasPivot = pivotEnabled;
                }
                else
                {
                    thermalPowerDissipPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * stefanArea * 0.5;

                    radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(thermalPowerDissipPerSecond, wasteheatManager) : 0;

                    instantaneous_rad_temp = CalculateInstantaniousRadTemp(external_temperature);

                    CurrentRadiatorTemperature = instantaneous_rad_temp;
                }

                if (vessel.atmDensity > 0)
                {
                    atmosphere_modifier = vessel.atmDensity + vessel.dynamicPressurekPa * 1.01325e-2;

                    var convPowerDissip = wasteheatManager.RadiatorEfficiency * atmosphere_modifier * Math.Max(0, CurrentRadiatorTemperature - external_temperature) * effectiveRadiatorArea * 0.001 * convectiveBonus * Math.Max(part.submergedPortion * 10, 1);

                    if (!radiatorIsEnabled)
                        convPowerDissip = convPowerDissip * 0.25;

                    convectedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(convPowerDissip, wasteheatManager) : 0;

                    if (update_count == DEPLOYMENT_DELAY)
                        DeployMentControl(atmosphere_modifier);
                }
                else
                {
                    convectedThermalPower = 0;

                    if (radiatorIsEnabled || !isAutomated || !canRadiateHeat || !showControls || update_count != DEPLOYMENT_DELAY) return;

                    Debug.Log("[KSPI] - FixedUpdate Automated Deployment ");
                    Deploy();
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception on " +  part.name + " durring FNRadiator.FixedUpdate with message " + e.Message);
            }
        }

        private double CalculateInstantaniousRadTemp(double externalTemperature)
        {
            var result = Math.Max(radiator_temperature_temp_val, externalTemperature);

            if (Double.IsNaN(result))
                Debug.LogError("[KSPI] - FNRadiator: FixedUpdate Single.IsNaN detected in instantaneous_rad_temp after reading external temperature");

            return result;
        }

        private void DeployMentControl(double dynamic_pressure)
        {
            if (dynamic_pressure > 0 && (atmosphereToleranceModifier * dynamic_pressure * 6.73659786) > 100)
            {
                if (!isDeployable || !radiatorIsEnabled) return;

                if (isAutomated)
                {
                    Debug.Log("[KSPI] - DeployMentControl Auto Retracted");
                    Retract();
                }
                else
                {
                    if (CheatOptions.UnbreakableJoints) return;

                    Debug.Log("[KSPI] - DeployMentControl Decoupled!");
                    part.deactivate();
                    part.decouple(1);
                }
            }
            else if (!radiatorIsEnabled && isAutomated && canRadiateHeat && showControls && (!preventShieldedDeploy || !part.ShieldedFromAirstream))
            {
                // Suppress message spam on repeated deploy attempts due to radiator delay
                if (radiator_deploy_delay == 0)
                    Debug.Log("[KSPI] - DeployMentControl Auto Deploy");
                Deploy();
            }
        }

        private double consumeWasteHeatPerSecond(double wasteheatToConsume, ResourceManager wasteheatManager)
        {
            if (!radiatorIsEnabled) return 0;

            var consumedWasteheat = CheatOptions.IgnoreMaxTemperature || wasteheatToConsume == 0
                ? wasteheatToConsume 
                : consumeFNResourcePerSecond(wasteheatToConsume, ResourceManager.FNRESOURCE_WASTEHEAT, wasteheatManager);

            return Double.IsNaN(consumedWasteheat) ? 0 : consumedWasteheat;
        }

        protected double CurrentRadTemp;
        public double CurrentRadiatorTemperature 
        {
            get 
            {
                return CurrentRadTemp;
            }
            set
            {
                CurrentRadTemp = value;
                temperatureQueue.Enqueue(CurrentRadTemp);
                if (temperatureQueue.Count > 10)
                    temperatureQueue.Dequeue();
            }
        }

        public double GetAverateRadiatorTemperature()
        {
            return temperatureQueue.Count > 0 ? temperatureQueue.Average() : CurrentRadTemp;
        }

        public override string GetInfo()
        {
            DetermineGenerationType();
            effectiveRadiatorArea = EffectiveRadiatorArea;
            stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            var sb = new StringBuilder();

            sb.Append(String.Format("Base surface area: {0:F2} m\xB2 \n", radiatorArea));
            sb.Append(String.Format("Surface area / Mass : {0:F2}\n", radiatorArea / part.mass));

            sb.Append(String.Format("Surface Area Bonus: {0:P0}\n", String.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? 0 : surfaceAreaUpgradeMult - 1 ));
            sb.Append(String.Format("Atm Convection Bonus: {0:P0}\n", convectiveBonus - 1));

            sb.Append(String.Format("\nMaximum Waste Heat Radiated\nMk1: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk1, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk1, 4)));

            sb.Append(String.Format("Mk2: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk2, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk2, 4)));
            sb.Append(String.Format("Mk3: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk3, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk3, 4)));

            if (String.IsNullOrEmpty(surfaceAreaUpgradeTechReq)) return sb.ToString();

            sb.Append(String.Format("Mk4: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk4, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk4, 4)));
            sb.Append(String.Format("Mk5: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk5, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk5, 4)));

            var convection = 0.9 * effectiveRadiatorArea * convectiveBonus;
            var disapation = stefanArea * Math.Pow(900, 4);

            sb.Append(String.Format("\nMaximum @ 1 atmosphere : 1200 K, dissipation: {0:F3} MW\n, convection: {1:F3} MW\n", disapation, convection));

            return sb.ToString();
        }

        public override int getPowerPriority() 
        {
            return 3;
        }

        private void SetHeatAnimationRatio(float colorRatio)
        {
            var heatstatesCount = heatStates.Count();
            for (var i = 0; i < heatstatesCount; i++)
            {
                anim = heatStates[i];
                if (anim == null)
                    continue;
                anim.normalizedTime = colorRatio;
            }
        }

        private void ApplyColorHeat()
        {
            //Account for Draper Point
            const double maxTemperature = 4400;
            const double drapperPoint = 798;
            const double temperatureRange = maxTemperature - drapperPoint;

            var simulatedTempRatio = radiatorIsEnabled ? (CurrentRadiatorTemperature - drapperPoint) / temperatureRange : 0;
            var stockTempRatio = (part.temperature - drapperPoint) / temperatureRange;
            var colorRatio = Math.Min(Math.Max(simulatedTempRatio, stockTempRatio), 1);

            if (heatStates != null && heatStates.Any())
            {
                SetHeatAnimationRatio(Mathf.Min((float)(colorRatio * colorRatio), 1));
            }
            else if (!string.IsNullOrEmpty(colorHeat))
            {
                if (renderArray == null)
                    return;

                var temperatureRatio = colorRatio / temperatureColorDivider;

                var colorRatioRed = Math.Pow(temperatureRatio, emissiveColorPower);
                var colorRatioGreen = Math.Pow(temperatureRatio, emissiveColorPower * 2) * 0.6;
                var colorRatioBlue = Math.Pow(temperatureRatio, emissiveColorPower * 4) * 0.3;

                emissiveColor = new Color((float)colorRatioRed, (float)colorRatioGreen, (float)colorRatioBlue, (float)wasteheatRatio);

                var renderArrayCount = renderArray.Count();
                for (var i = 0; i < renderArrayCount; i++)
                {
                    renderer = renderArray[i];

                    if (renderer == null || renderer.material == null)
                        continue;

                    if (renderer.material.shader != null && renderer.material.shader.name != kspShaderLocation)
                        renderer.material.shader = kspShader;

                    if (part.name.StartsWith("circradiator"))
                    {
                        if (renderer.material.GetTexture("_Emissive") == null)
                            renderer.material.SetTexture("_Emissive",
                                GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/circradiatorKT/texture1_e", false));

                        if (renderer.material.GetTexture("_BumpMap") == null)
                            renderer.material.SetTexture("_BumpMap",
                                GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/circradiatorKT/texture1_n", false));
                    }
                    else if (part.name.StartsWith("RadialRadiator"))
                    {
                        if (renderer.material.GetTexture("_Emissive") == null)
                            renderer.material.SetTexture("_Emissive",
                                GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/RadialHeatRadiator/d_glow", false));
                    }
                    else if (part.name.StartsWith("LargeFlatRadiator"))
                    {
                        if (renderer.material.GetTexture("_Emissive") == null)
                            renderer.material.SetTexture("_Emissive",
                                GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/LargeFlatRadiator/glow", false));

                        if (renderer.material.GetTexture("_BumpMap") == null)
                            renderer.material.SetTexture("_BumpMap",
                                GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Radiators/LargeFlatRadiator/radtex_n", false));
                    }

                    renderer.material.SetColor(colorHeat, emissiveColor);

                }
            }
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title;
        }
    }
}