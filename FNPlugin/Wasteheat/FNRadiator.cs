using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

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
            //var efficiency = 1 - Math.Pow(1 - WasteHeatRatio, 400);

            if (Double.IsNaN(WasteHeatRatio))
            {
                Debug.LogError("FNRadiator: FixedUpdate Single.IsNaN detected in WasteHeatRatio");
                return;
            }
            external_temperature = FlightGlobals.getExternalTemperature(UpdatingRadiator.vessel.transform.position);
            var normalized_atmosphere = Math.Min(UpdatingRadiator.vessel.atmDensity, 1);

            // titanium radiator
            var radiator_temperature_temp_val_titanium = external_temperature + Math.Min((MaxVacuumTemperatureTitanium - external_temperature) * Math.Sqrt(WasteHeatRatio), MaxVacuumTemperatureTitanium - external_temperature);

            // graphene radiator
            var atmosphereModifierVacuum = Math.Max(Math.Min(1 - UpdatingRadiator.vessel.atmDensity, 1), 0);
            var atmosphereModifierAtmosphere = Math.Max(normalized_atmosphere, 0);
            var maxCurrentTemperatureGraphene = 1200 * atmosphereModifierAtmosphere + MaxVacuumTemperatureGraphene * atmosphereModifierVacuum;
            var radiator_temperature_temp_val_graphene = external_temperature + Math.Min((MaxVacuumTemperatureGraphene - external_temperature) * Math.Sqrt(WasteHeatRatio), maxCurrentTemperatureGraphene - external_temperature);
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
        public float maxVacuumTemperature = 3700;
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
        [KSPField]
        public double dynamic_pressure;
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
        public float maxRadiatorTemperature = 3700;
        [KSPField(guiName = "Upgrade Techs")]
        public int nrAvailableUpgradeTechs;
        [KSPField(guiName = "Has Surface Upgrade")]
        public bool hasSurfaceAreaUpgradeTechReq;
        [KSPField]
        public float atmosphereToleranceModifier = 1;

        const string kspShader = "KSP/Emissive/Bumped Specular";
        const int RADIATOR_DELAY = 20;
        const int FRAME_DELAY = 9;
        const int DEPLOYMENT_DELAY = 6;

        // minimize garbade by recycling variablees
        private double thermalPowerDissipPerSecond;
        private double radiatedThermalPower;
        private double convectedThermalPower;
        private double normalizedAtmosphere;
        private double external_temperature;
        private double temperatureDifferenceCurrentWithExternal;
        private double temperatureDifferenceMaximumWithExternal;

        private bool active;
        private long update_count;

        private int radiator_deploy_delay;
        private int explode_counter;

        private BaseEvent deployRadiatorEvent;
        private BaseEvent retractRadiatorEvent;

        private BaseField thermalPowerConvStrField;
        private BaseField radiatorIsEnabledField;
        private BaseField isAutomatedField;
        private BaseField pivotEnabledField;
        
        private Renderer renderer;
        private Animation deployAnimation;
        private Color emissiveColor;
        private CelestialBody star;
        private AnimationState anim;
        private Renderer[] renderArray;
        private AnimationState[] heatStates;
        private ModuleDeployableRadiator _moduleDeployableRadiator;
        private ModuleActiveRadiator _moduleActiveRadiator;
        private ResourceManager wasteheatManager;
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
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech4))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech2))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(PluginHelper.RadiatorUpgradeTech1))
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
                return 3700;
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
            if (radiator_deploy_delay == 0)
                Debug.Log("[KSPI] - Deploy Called ");

            if (preventShieldedDeploy && (part.ShieldedFromAirstream || radiator_deploy_delay < RADIATOR_DELAY)) 
            {
                radiator_deploy_delay++;
                return;
            }

            radiator_deploy_delay = 0;

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

            maxRadiatorTemperature = (float)MaxRadiatorTemperature;

            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            effectiveRadiatorArea = EffectiveRadiatorArea;

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

            _maxEnergyTransfer = radiatorArea * 1000 * Math.Pow(1 + ((int)CurrentGenerationType), 1.5);

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.maxEnergyTransfer = _maxEnergyTransfer;

            if (state == StartState.Editor) return;

            var depth = 0;
            star = FlightGlobals.currentMainBody;
            while (depth < 10 && star != null && star.GetTemperature(0) < 2000)
            {
                star = star.referenceBody;
                depth++;
            }
            if (star == null)
                star = FlightGlobals.Bodies[0];

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            renderArray = part.FindModelComponents<Renderer>().ToArray();

            if (radiatorInit == false)
                radiatorInit = true;

            part.maxTemp = maxRadiatorTemperature;

            radiatorTempStr = maxRadiatorTemperature + "K";

            maxVacuumTemperature = String.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? Math.Min((float)PluginHelper.RadiatorTemperatureMk3, maxRadiatorTemperature) :  Math.Min(maxVacuumTemperature, maxRadiatorTemperature);
            maxAtmosphereTemperature = String.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? Math.Min((float)PluginHelper.RadiatorTemperatureMk3, maxRadiatorTemperature) : Math.Min(maxAtmosphereTemperature, maxRadiatorTemperature);

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

            if (update_count < FRAME_DELAY)
                return;

            update_count = 0;

            if  (_moduleDeployableRadiator != null && (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED ||
                                                       _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)) {
                if (radiatorState != _moduleDeployableRadiator.deployState) {
                    part.SendMessage("GeometryPartModuleRebuildMeshData");
                    Debug.Log("[KSPI] - Updating geometry mesh due to radiator deployment.");
                }
                radiatorState = _moduleDeployableRadiator.deployState;
            }

            external_temperature = FlightGlobals.getExternalTemperature(part.transform.position);
            normalizedAtmosphere = Math.Min(vessel.atmDensity, 1);
            effectiveRadiatorArea = EffectiveRadiatorArea;
            maxCurrentTemperature = maxAtmosphereTemperature * Math.Max(normalizedAtmosphere, 0) + maxVacuumTemperature * Math.Max(Math.Min(1 - vessel.atmDensity, 1), 0);
            
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

            isAutomatedField.guiActive = showControls;
            isAutomatedField.guiActiveEditor = showControls;

            pivotEnabledField.guiActive = showControls;
            pivotEnabledField.guiActiveEditor = showControls;

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

                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                resourceBuffers.UpdateBuffers();

                // get resource bar ratio at start of frame
                wasteheatManager = getManagerForVessel(ResourceManager.FNRESOURCE_WASTEHEAT);
                wasteheatRatio = wasteheatManager.ResourceBarRatioBegin;

                if (Double.IsNaN(wasteheatRatio))
                {
                    Debug.LogError("FNRadiator: FixedUpdate Single.IsNaN detected in wasteheatRatio");
                    return;
                }

                radiator_temperature_temp_val = external_temperature + Math.Min(temperatureDifferenceMaximumWithExternal * Math.Sqrt(wasteheatRatio), temperatureDifferenceCurrentWithExternal);

                var deltaTemp = Math.Max(radiator_temperature_temp_val - Math.Max(external_temperature * normalizedAtmosphere, 2.7), 0);

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

                    var efficiency = CalculateEfficiency();
                    thermalPowerDissipPerSecond = efficiency * Math.Pow(deltaTemp, 4) * GameConstants.stefan_const * effectiveRadiatorArea / 1e6;

                    if (Double.IsNaN(thermalPowerDissipPerSecond))
                        Debug.LogWarning("FNRadiator: FixedUpdate Single.IsNaN detected in fixed_thermal_power_dissip");

                    radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(thermalPowerDissipPerSecond) : 0;

                    if (Double.IsNaN(radiatedThermalPower))
                        Debug.LogError("FNRadiator: FixedUpdate Single.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + thermalPowerDissipPerSecond + ")");

                    instantaneous_rad_temp = CalculateInstantaniousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;

                    if (_moduleDeployableRadiator)
                        _moduleDeployableRadiator.hasPivot = pivotEnabled;
                }
                else
                {
                    var efficiency = CalculateEfficiency();
                    thermalPowerDissipPerSecond = efficiency * Math.Pow(Math.Max(deltaTemp - external_temperature, 0), 4) * GameConstants.stefan_const * effectiveRadiatorArea / 0.5e7;

                    radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(thermalPowerDissipPerSecond) : 0;

                    instantaneous_rad_temp = CalculateInstantaniousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;
                }

                if (vessel.atmDensity > 0)
                {
                    dynamic_pressure = 0.60205 * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude / 101325;
                    vessel.atmDensity += dynamic_pressure;

                    var efficiency = CalculateEfficiency();
                    var convPowerDissip = efficiency * vessel.atmDensity * Math.Max(0, CurrentRadiatorTemperature - external_temperature) * effectiveRadiatorArea * 0.001 * convectiveBonus * Math.Max(part.submergedPortion * 10, 1);

                    if (!radiatorIsEnabled)
                        convPowerDissip = convPowerDissip / 2;

                    convectedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(convPowerDissip) : 0;

                    if (update_count == DEPLOYMENT_DELAY)
                        DeployMentControl(dynamic_pressure);
                }
                else
                {
                    convectedThermalPower = 0;

                    if (radiatorIsEnabled || !isAutomated || !canRadiateHeat || !showControls ||
                        update_count != DEPLOYMENT_DELAY) return;

                    Debug.Log("[KSPI] - FixedUpdate Automated Deplotment ");
                    Deploy();
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception on " +  part.name + " durring FNRadiator.FixedUpdate with message " + e.Message);
            }
        }

        private double CalculateInstantaniousRadTemp()
        {
            var result = Math.Max(radiator_temperature_temp_val, Math.Max(FlightGlobals.getExternalTemperature(vessel.altitude, vessel.mainBody), 2.7));

            if (Double.IsNaN(result))
                Debug.LogError("FNRadiator: FixedUpdate Single.IsNaN detected in instantaneous_rad_temp after reading external temperature");

            return result;
        }

        private double CalculateEfficiency()
        {
            return 1 - Math.Pow(1 - wasteheatRatio, 400);
        }

        private void DeployMentControl(double dynamic_pressure)
        {
            if (dynamic_pressure > 0 && (atmosphereToleranceModifier * dynamic_pressure / 1.4854428818159e-3 * 100) > 100)
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

        private double consumeWasteHeatPerSecond(double wasteheatToConsume)
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

            var stefanArea = GameConstants.stefan_const * effectiveRadiatorArea;
            var sb = new StringBuilder();

            sb.Append(String.Format("Base surface area: {0:F2} m\xB2 \n", radiatorArea));
            sb.Append(String.Format("Surface area / Mass : {0:F2}\n", radiatorArea / part.mass));

            sb.Append(String.Format("Surface Area Bonus: {0:P0}\n", String.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? 0 : surfaceAreaUpgradeMult - 1 ));
            sb.Append(String.Format("Atm Convection Bonus: {0:P0}\n", convectiveBonus - 1));

            sb.Append(String.Format("\nMaximum Waste Heat Radiated\nMk1: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk1, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk1, 4) / 1e6));

            sb.Append(String.Format("Mk2: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk2, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk2, 4) / 1e6));
            sb.Append(String.Format("Mk3: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk3, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk3, 4) / 1e6));

            if (String.IsNullOrEmpty(surfaceAreaUpgradeTechReq)) return sb.ToString();

            sb.Append(String.Format("Mk4: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk4, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk4, 4) / 1e6));
            sb.Append(String.Format("Mk5: {0:F0} K {1:F3} MW\n", PluginHelper.RadiatorTemperatureMk5, stefanArea * Math.Pow(PluginHelper.RadiatorTemperatureMk5, 4) / 1e6));

            var convection = 900 * effectiveRadiatorArea * 1000 / 1e6 * convectiveBonus;
            var disapation = stefanArea * Math.Pow(900, 4) / 1e6;

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

        private void ColorHeat()
        {
            if (heatStates != null && heatStates.Any())
            {
                var radiatorTempRatio = Mathf.Min((float)CurrentRadiatorTemperature / maxRadiatorTemperature, 1);
                SetHeatAnimationRatio(radiatorTempRatio);
            }
            else if (!string.IsNullOrEmpty(colorHeat))
            {
                if (renderArray == null)
                    return;

                var radiatorTempRatio = Mathf.Min((float)CurrentRadiatorTemperature / maxRadiatorTemperature, 1);
                var partTempRatio = Mathf.Min(((float)part.temperature / maxRadiatorTemperature), 1);
                var colorRatioRed = Mathf.Pow(Math.Max(partTempRatio, radiatorTempRatio) / temperatureColorDivider, emissiveColorPower);
                var colorRatioGreen = Mathf.Pow(Math.Max(partTempRatio, radiatorTempRatio) / temperatureColorDivider, emissiveColorPower * 2) * 0.6f;
                var colorRatioBlue = Mathf.Pow(Math.Max(partTempRatio, radiatorTempRatio) / temperatureColorDivider, emissiveColorPower * 4) * 0.3f;

                emissiveColor = new Color(colorRatioRed, colorRatioGreen, colorRatioBlue, (float)wasteheatRatio);

                var renderArrayCount = renderArray.Count();
                for (var i = 0; i < renderArrayCount; i++)
                {
                    renderer = renderArray[i];

                    if (renderer == null || renderer.material == null)
                        continue;

                    if (renderer.material.shader != null && renderer.material.shader.name != kspShader)
                        renderer.material.shader = Shader.Find(kspShader);

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