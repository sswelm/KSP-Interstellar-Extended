using FNPlugin.Extensions;
using FNPlugin.Power;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Wasteheat 
{
    [KSPModule("Radiator")]
    class IntegratedRadiator : FNRadiator { }

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
        [KSPField(isPersistant = false)]
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
        [KSPField(isPersistant = true)]
        public double currentRadTemp;

        // non persistant
        [KSPField(guiName = "Max Vacuum Temp", guiFormat = "F0", guiUnits = "K")]
        public double maxVacuumTemperature = maximumRadiatorTempInSpace;
        [KSPField(guiName = "Max Atmosphere Temp", guiFormat = "F0", guiUnits = "K")]
        public double maxAtmosphereTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(guiName = "Max Current Temp", guiFormat = "F0", guiUnits = "K")]
        public double maxCurrentRadiatorTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(guiName = "Space Radiator Bonus", guiFormat = "F0", guiUnits = "K")]
        public double spaceRadiatorBonus;
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
        [KSPField]
        public bool isPassive = false;
        [KSPField(guiName = "Converction Bonus")]
        public double convectiveBonus = 1;
        [KSPField]
        public string animName = "";
        [KSPField]
        public string thermalAnim = "";
        [KSPField]
        public string originalName = "";
        [KSPField]
        public float upgradeCost = 100;
        [KSPField]
        public bool maintainResourceBuffers = true;
        [KSPField]
        public float emissiveColorPower = 3;
        [KSPField]
        public float colorRatioExponent = 1;
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public bool keepMaxPartTempEqualToMaxRadiatorTemp = true;
        [KSPField]
        public string colorHeat = "_EmissiveColor";
        [KSPField]
        public string emissiveTextureLocation = "";
        [KSPField]
        public string bumpMapTextureLocation = "";
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
        [KSPField(guiName = "Dynamic Pressure Stress", guiActive = true, guiFormat = "P2")]
        public double dynamicPressureStress;
        [KSPField(guiName = "Max Energy Transfer", guiFormat = "F2")]
        private double _maxEnergyTransfer;
        [KSPField(guiActiveEditor = true, guiName = "Max Radiator Temperature", guiFormat = "F0")]
        public float maxRadiatorTemperature = maximumRadiatorTempInSpace;
        [KSPField(guiName = "Upgrade Techs")]
        public int nrAvailableUpgradeTechs;
        [KSPField(guiName = "Has Surface Upgrade")]
        public bool hasSurfaceAreaUpgradeTechReq;
        [KSPField]
        public float atmosphereToleranceModifier = 1;
        [KSPField]
        public double atmosphericMultiplier;
        [KSPField]
        public double externalTemperature;
        [KSPField(guiName = "Effective Tempererature")]
        public float displayTemperature;
        [KSPField(guiName = "Color Ratio")]
        public float colorRatio;
        [KSPField]
        public double deltaTemp;
        [KSPField]
        public double verticalSpeed;
        [KSPField]
        public double spaceRadiatorModifier;
        [KSPField]
        public double combinedPresure;
        [KSPField]
        public double oxidationModifier;

        const string kspShaderLocation = "KSP/Emissive/Bumped Specular";
        const int RADIATOR_DELAY = 20;
        const int DEPLOYMENT_DELAY = 6;

        static float drapperPoint = 500; // 798
        static float maximumRadiatorTempInSpace = 4500;
        static float maximumRadiatorTempAtOneAtmosphere = 1200;
        static float maxSpaceTempBonus;
        static float temperatureRange;

        // minimize garbage by recycling variablees
        private double stefanArea;
        private double thermalPowerDissipPerSecond;
        private double radiatedThermalPower;
        private double convectedThermalPower;   

        private bool active;
        private bool isGraphene;
        private bool startWithCircradiator;
        private bool startWithRadialRadiator;
        private bool startWithLargeFlatRadiator;

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
        private AnimationState anim;
        private Renderer[] renderArray;
        private AnimationState[] heatStates;
        private ModuleDeployableRadiator _moduleDeployableRadiator;
        private ModuleActiveRadiator _moduleActiveRadiator;
        private ModuleDeployablePart.DeployState radiatorState;
        private ResourceBuffers resourceBuffers;

        private Queue<double> radTempQueue = new Queue<double>(20);
        private Queue<double> externalTempQueue = new Queue<double>(20);

        private static AnimationCurve redTempColorChannel;
        private static AnimationCurve greenTempColorChannel;
        private static AnimationCurve blueTempColorChannel;

        public static void InitializeTemperatureColorChannels()
        {
            if (redTempColorChannel != null)
                return;

            redTempColorChannel = new AnimationCurve();
            redTempColorChannel.AddKey(500, 0 / 255f);
            redTempColorChannel.AddKey(800, 100 / 255f);
            redTempColorChannel.AddKey(1000, 200 / 255f);
            redTempColorChannel.AddKey(1250, 255 / 255f);
            redTempColorChannel.AddKey(1500, 255 / 255f);
            redTempColorChannel.AddKey(2000, 255 / 255f);
            redTempColorChannel.AddKey(2680, 255 / 255f);
            redTempColorChannel.AddKey(3000, 255 / 255f);
            redTempColorChannel.AddKey(3200, 255 / 255f);
            redTempColorChannel.AddKey(3500, 255 / 255f);
            redTempColorChannel.AddKey(4000, 255 / 255f);
            redTempColorChannel.AddKey(4200, 255 / 255f);
            redTempColorChannel.AddKey(4500, 255 / 255f);
            redTempColorChannel.AddKey(5000, 255 / 255f);

            greenTempColorChannel = new AnimationCurve();
            greenTempColorChannel.AddKey(500, 0 / 255f);
            greenTempColorChannel.AddKey(800, 0 / 255f);
            greenTempColorChannel.AddKey(1000, 0 / 255f);
            greenTempColorChannel.AddKey(1250, 0 / 255f);
            greenTempColorChannel.AddKey(1500, 30 / 255f);
            greenTempColorChannel.AddKey(2000, 100 / 255f);
            greenTempColorChannel.AddKey(2680, 180 / 255f);
            greenTempColorChannel.AddKey(3000, 230 / 255f);
            greenTempColorChannel.AddKey(3200, 255 / 255f);
            greenTempColorChannel.AddKey(3500, 255 / 255f);
            greenTempColorChannel.AddKey(4000, 255 / 255f);
            greenTempColorChannel.AddKey(4200, 255 / 255f);
            greenTempColorChannel.AddKey(4500, 255 / 255f);
            greenTempColorChannel.AddKey(5000, 255 / 255f);

            blueTempColorChannel = new AnimationCurve();
            blueTempColorChannel.AddKey(500, 0 / 255f);
            blueTempColorChannel.AddKey(800, 0 / 255f);
            blueTempColorChannel.AddKey(1000, 0 / 255f);
            blueTempColorChannel.AddKey(1500, 0 / 255f);
            blueTempColorChannel.AddKey(2000, 0 / 255f);
            blueTempColorChannel.AddKey(2680, 0 / 255f);
            blueTempColorChannel.AddKey(3000, 0 / 255f);
            blueTempColorChannel.AddKey(3200, 0 / 255f);
            blueTempColorChannel.AddKey(3500, 76 / 255f);
            blueTempColorChannel.AddKey(4000, 140 / 255f);
            blueTempColorChannel.AddKey(4200, 169 / 255f);
            blueTempColorChannel.AddKey(4500, 200 / 255f);
            blueTempColorChannel.AddKey(5000, 255 / 255f);

            for (int i = 0; i < redTempColorChannel.keys.Length; i++)
            {
                redTempColorChannel.SmoothTangents(i, 0);
            }
            for (int i = 0; i < greenTempColorChannel.keys.Length; i++)
            {
                greenTempColorChannel.SmoothTangents(i, 0);
            }
            for (int i = 0; i < blueTempColorChannel.keys.Length; i++)
            {
                blueTempColorChannel.SmoothTangents(i, 0);
            }
        }

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

        private double GetMaximumTemperatureForGen(GenerationType generationType)
        {
            var generation = (int)generationType;

            if (generation >= (int)GenerationType.Mk6 && isGraphene)
                return RadiatorProperties.RadiatorTemperatureMk6;
            if (generation >= (int)GenerationType.Mk5 && isGraphene)
                return RadiatorProperties.RadiatorTemperatureMk5;
            if (generation >= (int)GenerationType.Mk4)
                return RadiatorProperties.RadiatorTemperatureMk4;
            if (generation >= (int)GenerationType.Mk3)
                return RadiatorProperties.RadiatorTemperatureMk3;
            if (generation >= (int)GenerationType.Mk2)
                return RadiatorProperties.RadiatorTemperatureMk2;
            else
                return RadiatorProperties.RadiatorTemperatureMk1;
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
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech5))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech4))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech2))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech1))
                nrAvailableUpgradeTechs++;

            // determine tech levels
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

            if (!radiator_vessel.Any())
                return maximumRadiatorTempInSpace;

            if (radiator_vessel.Any())
            {
                var maxRadiatorTemperature = radiator_vessel.Max(r => r.MaxRadiatorTemperature);
                var totalRadiatorsMass = radiator_vessel.Sum(r =>  (double)(decimal) r.part.mass);

                return radiator_vessel.Sum(r => Math.Min(1, r.GetAverateRadiatorTemperature() / r.MaxRadiatorTemperature) * maxRadiatorTemperature * (r.part.mass / totalRadiatorsMass));
            }
            else
                return maximumRadiatorTempInSpace;
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

            Debug.Log("[KSPI]: DeployRadiator Called ");

            Deploy();
        }

        private void Deploy()
        {
            if (preventShieldedDeploy && (part.ShieldedFromAirstream || radiator_deploy_delay < RADIATOR_DELAY)) 
            {
                //Debug.Log("[KSPI]: Deploy Aborted, Part is shielded or nor ready");
                return;
            }

            Debug.Log("[KSPI]: Deploy Called ");

            if (_moduleDeployableRadiator != null)
                _moduleDeployableRadiator.Extend();

            ActivateRadiator();

            if (deployAnimation == null) return;

            deployAnimation[animName].enabled = true;
            deployAnimation[animName].speed = 0.5f;
            deployAnimation[animName].normalizedTime = 0f;
            deployAnimation.Blend(animName, 2);
        }

        private void ActivateRadiator()
        {
            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Activate();

            radiatorIsEnabled = true;
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
            Debug.Log("[KSPI]: Retract Called ");

            if (_moduleDeployableRadiator != null)
            {
                _moduleDeployableRadiator.hasPivot = true;
                _moduleDeployableRadiator.Retract();
            }

            DeactivateRadiator();

            if (deployAnimation == null) return;

            deployAnimation[animName].enabled = true;
            deployAnimation[animName].speed = -0.5f;
            deployAnimation[animName].normalizedTime = 1;
            deployAnimation.Blend(animName, 2);
        }

        private void DeactivateRadiator()
        {
            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Shutdown();

            radiatorIsEnabled = false;
        }

        [KSPAction("Deploy Radiator")]
        public void DeployRadiatorAction(KSPActionParam param) 
        {
            Debug.Log("[KSPI]: DeployRadiatorAction Called ");
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
                Debug.Log("[KSPI]: ToggleRadiatorAction Called ");
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
            radiator_deploy_delay = 0;

            DetermineGenerationType();

            isGraphene = !String.IsNullOrEmpty(surfaceAreaUpgradeTechReq);
            maximumRadiatorTempInSpace = (float)RadiatorProperties.RadiatorTemperatureMk6;
            maxSpaceTempBonus = maximumRadiatorTempInSpace - maximumRadiatorTempAtOneAtmosphere;
            temperatureRange = maximumRadiatorTempInSpace - drapperPoint;

            kspShader = Shader.Find(kspShaderLocation);
            maxRadiatorTemperature = (float)MaxRadiatorTemperature;

            part.heatConvectiveConstant = convectiveBonus;
            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            effectiveRadiatorArea = EffectiveRadiatorArea;
            stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            startWithCircradiator = part.name.StartsWith("circradiator");
            startWithRadialRadiator = part.name.StartsWith("RadialRadiator");
            startWithLargeFlatRadiator = part.name.StartsWith("LargeFlatRadiator");

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
                _maxEnergyTransfer = radiatorArea * PhysicsGlobals.StefanBoltzmanConstant * Math.Pow(MaxRadiatorTemperature, 4) * 0.001;

                _moduleActiveRadiator.maxEnergyTransfer = _maxEnergyTransfer;
                _moduleActiveRadiator.overcoolFactor = 0.20 + ((int)CurrentGenerationType * 0.025);

                if (radiatorIsEnabled)
                    _moduleActiveRadiator.Activate();
                else
                    _moduleActiveRadiator.Shutdown();
            }

            if (state == StartState.Editor)
                return;

            if (isAutomated && !isDeployable)
            {
                ActivateRadiator();
            }

            for (var i = 0; i < 20; i++ )
            {
                radTempQueue.Enqueue(currentRadTemp);
            }        

            InitializeTemperatureColorChannels();

            ApplyColorHeat();

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            renderArray = part.FindModelComponents<Renderer>().ToArray();

            if (radiatorInit == false)
                radiatorInit = true;

            radiatorTempStr = maxRadiatorTemperature + "K";

            maxVacuumTemperature = isGraphene ? Math.Min(maxVacuumTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk4, maxRadiatorTemperature);
            maxAtmosphereTemperature = isGraphene ? Math.Min(maxAtmosphereTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk3, maxRadiatorTemperature);

            UpdateMaxCurrentTemperature();

            if (keepMaxPartTempEqualToMaxRadiatorTemp)
            {
                var partSkinTemperature = Math.Min(part.skinTemperature, maxCurrentRadiatorTemperature * 0.99);
                if (double.IsNaN(partSkinTemperature) == false)
                    part.skinTemperature = partSkinTemperature;

                var partTemperature = Math.Min(part.temperature, maxCurrentRadiatorTemperature * 0.99);
                if (double.IsNaN(partTemperature) == false)
                    part.temperature = partTemperature;

                if (double.IsNaN(maxCurrentRadiatorTemperature) == false)
                {
                    part.skinMaxTemp = maxCurrentRadiatorTemperature;
                    part.maxTemp = maxCurrentRadiatorTemperature;
                }
            }

            if (maintainResourceBuffers)
            {
                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 2.0e+6));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                resourceBuffers.Init(this.part);
            }
        }

        void radiatorIsEnabled_OnValueModified(object arg1)
        {
            Debug.Log("[KSPI]: radiatorIsEnabled_OnValueModified " + arg1);

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
            radiator_deploy_delay++;

            if  (_moduleDeployableRadiator != null && (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED ||
                                                       _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)) {
                if (radiatorState != _moduleDeployableRadiator.deployState) 
                {
                    part.SendMessage("GeometryPartModuleRebuildMeshData");
                    Debug.Log("[KSPI]: Updating geometry mesh due to radiator deployment.");
                }
                radiatorState = _moduleDeployableRadiator.deployState;
            }

            stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            oxidationModifier = 0;

            UpdateMaxCurrentTemperature();

            if (keepMaxPartTempEqualToMaxRadiatorTemp && double.IsNaN(maxCurrentRadiatorTemperature) == false)
            {
                part.skinMaxTemp = maxCurrentRadiatorTemperature;
                part.maxTemp = maxCurrentRadiatorTemperature;
            }

            thermalPowerConvStrField.guiActive = convectedThermalPower > 0;

            // synchronize states
            if (_moduleDeployableRadiator != null && pivotEnabled && showControls)
            {
                if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)
                    ActivateRadiator();
                else if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED)
                    DeactivateRadiator();
            }

            radiatorIsEnabledField.guiActive = !isPassive && showControls;
            radiatorIsEnabledField.guiActiveEditor = !isPassive && showControls;

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

            radiatorTempStr = CurrentRadiatorTemperature.ToString("0.0") + "K / " + maxCurrentRadiatorTemperature.ToString("0.0") + "K";

            partTempStr = Math.Max(part.skinTemperature, part.temperature).ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";

            if (showColorHeat)
                ApplyColorHeat();
        }

        private void UpdateMaxCurrentTemperature()
        {
            if (vessel.mainBody.atmosphereContainsOxygen && vessel.staticPressurekPa > 0)
            {
                combinedPresure = vessel.staticPressurekPa + vessel.dynamicPressurekPa * 0.2;

                if (combinedPresure > 101.325)
                {
                    var extraPresure = combinedPresure - 101.325;
                    var ratio = extraPresure / 101.325;
                    if (ratio <= 1)
                        ratio *= ratio;
                    else
                        ratio = Math.Sqrt(ratio);
                    oxidationModifier = 1 + ratio * 0.1;
                }
                else
                    oxidationModifier = Math.Pow(combinedPresure / 101.325, 0.25);

                spaceRadiatorModifier = Math.Max(0.25, Math.Min(0.95, 0.95 + vessel.verticalSpeed * 0.002));

                spaceRadiatorBonus = (1 / spaceRadiatorModifier) * maxSpaceTempBonus * (1 - (oxidationModifier));

                maxCurrentRadiatorTemperature = Math.Min(maxVacuumTemperature, Math.Max(PhysicsGlobals.SpaceTemperature, maxAtmosphereTemperature + spaceRadiatorBonus));
            }
            else
            {
                combinedPresure = 0;
                spaceRadiatorModifier = 0.95;
                spaceRadiatorBonus = maxSpaceTempBonus;
                maxCurrentRadiatorTemperature = maxVacuumTemperature;
            }
            verticalSpeed = vessel.verticalSpeed;
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

                if (resourceBuffers != null)
                {
                    resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, radiatorIsEnabled ? this.part.mass : this.part.mass * 1e-3);
                    resourceBuffers.UpdateBuffers();
                }

                // get resource bar ratio at start of frame
                ResourceManager wasteheatManager = getManagerForVessel(ResourceManager.FNRESOURCE_WASTEHEAT);

                if (Double.IsNaN(wasteheatManager.TemperatureRatio))
                {
                    Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Single.IsNaN detected in TemperatureRatio");
                    return;
                }

                // ToDo replace wasteheatManager.SqrtResourceBarRatioBegin by ResourceBarRatioBegin after generators hotbath takes into account expected temperature
                radiator_temperature_temp_val = Math.Min(maxRadiatorTemperature * wasteheatManager.TemperatureRatio, maxCurrentRadiatorTemperature);

                atmosphericMultiplier = Math.Sqrt(vessel.atmDensity);
                externalTemperature = vessel.externalTemperature;

                deltaTemp = Math.Max(radiator_temperature_temp_val - Math.Max(externalTemperature * Math.Min(1, atmosphericMultiplier), PhysicsGlobals.SpaceTemperature), 0);
                var deltaTempToPowerFour = deltaTemp * deltaTemp * deltaTemp * deltaTemp;

                if (radiatorIsEnabled)
                {
                    if (!CheatOptions.IgnoreMaxTemperature && wasteheatManager.ResourceBarRatioBegin >= 1 && CurrentRadiatorTemperature >= maxRadiatorTemperature)
                    {
                        explode_counter++;
                        if (explode_counter > 25)
                            part.explode();
                    }
                    else
                        explode_counter = 0;

                    thermalPowerDissipPerSecond = (double)wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * stefanArea;

                    if (Double.IsNaN(thermalPowerDissipPerSecond))
                        Debug.LogWarning("[KSPI]: FNRadiator: FixedUpdate Single.IsNaN detected in thermalPowerDissipPerSecond");

                    radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(thermalPowerDissipPerSecond, wasteheatManager) : 0;

                    if (Double.IsNaN(radiatedThermalPower))
                        Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Single.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + thermalPowerDissipPerSecond + ")");

                    instantaneous_rad_temp = CalculateInstantaniousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;

                    if (_moduleDeployableRadiator)
                        _moduleDeployableRadiator.hasPivot = pivotEnabled;
                }
                else
                {
                    thermalPowerDissipPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * stefanArea * 0.5;

                    radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(thermalPowerDissipPerSecond, wasteheatManager) : 0;

                    instantaneous_rad_temp = CalculateInstantaniousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;
                }

                if (vessel.atmDensity > 0)
                {
                    atmosphere_modifier = vessel.atmDensity * convectiveBonus + vessel.speed.Sqrt();

                    var heatTransferCooficient = 0.0005; // 500W/m2/K
                    var temperatureDifference = Math.Max(0, CurrentRadiatorTemperature - vessel.externalTemperature);
                    var submergedModifier = Math.Max(part.submergedPortion * 10, 1);

                    var convPowerDissip = wasteheatManager.RadiatorEfficiency * atmosphere_modifier * temperatureDifference * effectiveRadiatorArea * heatTransferCooficient * submergedModifier;

                    if (!radiatorIsEnabled)
                        convPowerDissip = convPowerDissip * 0.25;

                    convectedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(convPowerDissip, wasteheatManager) : 0;

                    if (radiator_deploy_delay >= DEPLOYMENT_DELAY)
                        DeployMentControl();
                }
                else
                {
                    convectedThermalPower = 0;

                    if (radiatorIsEnabled || !isAutomated || !canRadiateHeat || !showControls || radiator_deploy_delay < DEPLOYMENT_DELAY) return;

                    Debug.Log("[KSPI]: FixedUpdate Automated Deployment ");
                    Deploy();
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception on " +  part.name + " durring FNRadiator.FixedUpdate with message " + e.Message);
            }
        }

        private double CalculateInstantaniousRadTemp()
        {
            var result = Math.Min(maxCurrentRadiatorTemperature, radiator_temperature_temp_val);

            if (result.IsInfinityOrNaN())
                Debug.LogError("[KSPI]: FNRadiator: FixedUpdate IsNaN or Infinity detected in CalculateInstantaniousRadTemp");

            return result;
        }

        private void DeployMentControl()
        {
            dynamicPressureStress = 4 * vessel.dynamicPressurekPa;

            if (dynamicPressureStress > 1)
            {
                if (!isDeployable || !radiatorIsEnabled) return;

                if (isAutomated)
                {
                    Debug.Log("[KSPI]: DeployMentControl Auto Retracted, stress at " + dynamicPressureStress.ToString("P2") + "%");
                    Retract();
                }
                else
                {
                    if (CheatOptions.UnbreakableJoints) return;

                    Debug.Log("[KSPI]: DeployMentControl Decoupled!");
                    part.deactivate();
                    part.decouple(1);
                }
            }
            else if (!radiatorIsEnabled && isAutomated && canRadiateHeat && showControls && (!preventShieldedDeploy || !part.ShieldedFromAirstream))
            {
                // Suppress message spam on repeated deploy attempts due to radiator delay
                //if (radiator_deploy_delay > DEPLOYMENT_DELAY)
                Debug.Log("[KSPI]: DeployMentControl Auto Deploy");
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


        public double CurrentRadiatorTemperature 
        {
            get 
            {
                return currentRadTemp;
            }
            set
            {
                if (!double.IsNaN(value) || !double.IsInfinity(value) || value != 0)
                    currentRadTemp = value;

                radTempQueue.Enqueue(currentRadTemp);
                if (radTempQueue.Count > 20)
                    radTempQueue.Dequeue();
                externalTempQueue.Enqueue(vessel.externalTemperature);
                if (externalTempQueue.Count > 20)
                    externalTempQueue.Dequeue();
            }
        }

        public double GetAverateRadiatorTemperature()
        {
            return Math.Max(externalTempQueue.Count > 0 ? externalTempQueue.Average() : vessel.externalTemperature, radTempQueue.Count > 0 ? radTempQueue.Average() : currentRadTemp);
        }

        public override string GetInfo()
        {
            DetermineGenerationType();

            RadiatorProperties.Initialize();

            effectiveRadiatorArea = EffectiveRadiatorArea;
            stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            var sb = new StringBuilder();
            sb.Append("<size=11>");

            sb.Append(String.Format("Base surface area: {0:F2} m\xB2 \n", radiatorArea));
            sb.Append(String.Format("Surface area / Mass : {0:F2}\n", radiatorArea / part.mass));

            sb.Append(String.Format("Surface Area Bonus: {0:P0}\n", String.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? 0 : surfaceAreaUpgradeMult - 1 ));
            sb.Append(String.Format("Atm Convection Bonus: {0:P0}\n", convectiveBonus - 1));

            sb.Append(String.Format("\nMaximum Waste Heat Radiated\nMk1: {0:F0} K {1:F3} MW\n", RadiatorProperties.RadiatorTemperatureMk1, stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk1, 4)));

            sb.Append(String.Format("Mk2: {0:F0} K {1:F3} MW\n", RadiatorProperties.RadiatorTemperatureMk2, stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk2, 4)));
            sb.Append(String.Format("Mk3: {0:F0} K {1:F3} MW\n", RadiatorProperties.RadiatorTemperatureMk3, stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk3, 4)));
            sb.Append(String.Format("Mk4: {0:F0} K {1:F3} MW\n", RadiatorProperties.RadiatorTemperatureMk4, stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk4, 4)));

            if (String.IsNullOrEmpty(surfaceAreaUpgradeTechReq)) return sb.ToString();

            sb.Append(String.Format("Mk5: {0:F0} K {1:F3} MW\n", RadiatorProperties.RadiatorTemperatureMk5, stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk5, 4)));
            sb.Append(String.Format("Mk6: {0:F0} K {1:F3} MW\n", RadiatorProperties.RadiatorTemperatureMk6, stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk6, 4)));

            var convection = 0.9 * effectiveRadiatorArea * convectiveBonus;
            var disapation = stefanArea * Math.Pow(900, 4);

            sb.Append(String.Format("\nMaximum @ 1 atmosphere : 1200 K, dissipation: {0:F3} MW\n, convection: {1:F3} MW\n", disapation, convection));

            sb.Append("</size>");

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
            displayTemperature = (float)GetAverateRadiatorTemperature();

            colorRatio =  displayTemperature < drapperPoint ? 0 : Mathf.Min(1, (Mathf.Max(0, displayTemperature - drapperPoint) / temperatureRange) * 1.05f);

            if (heatStates != null && heatStates.Any())
            {
                SetHeatAnimationRatio(colorRatio.Sqrt());
            }
            else if (!string.IsNullOrEmpty(colorHeat) && colorRatioExponent != 0)
            {
                if (renderArray == null)
                    return;

                float colorRatioRed = 0;
                float colorRatioGreen = 0;
                float colorRatioBlue = 0;

                if (displayTemperature >= drapperPoint)
                {
                    colorRatioRed = redTempColorChannel.Evaluate(displayTemperature);
                    colorRatioGreen = greenTempColorChannel.Evaluate(displayTemperature);
                    colorRatioBlue = blueTempColorChannel.Evaluate(displayTemperature);
                }

                var effectiveColorRatio = Mathf.Pow(colorRatio, colorRatioExponent);

                var emissiveColor = new Color(colorRatioRed, colorRatioGreen, colorRatioBlue, effectiveColorRatio);

                for (var i = 0; i < renderArray.Count(); i++)
                {
                    renderer = renderArray[i];

                    if (renderer == null || renderer.material == null)
                        continue;

                    if (renderer.material.shader != null && renderer.material.shader.name != kspShaderLocation)
                        renderer.material.shader = kspShader;

                    if (!string.IsNullOrEmpty(emissiveTextureLocation))
                    {
                        if (renderer.material.GetTexture("_Emissive") == null)
                            renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture(emissiveTextureLocation, false));
                    }

                    if (!string.IsNullOrEmpty(bumpMapTextureLocation))
                    {
                        if (renderer.material.GetTexture("_BumpMap") == null)
                            renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture(bumpMapTextureLocation, false));
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