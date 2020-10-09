using FNPlugin.Extensions;
using FNPlugin.Power;
using KSP.Localization;
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
    class HeatPumpRadiator : FNRadiator
    {
        // Duplicate code from UniversalCrustExtractor.cs
        // Original: WhatsUnderneath()
        // Changes: returns amount of drill underground.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillReach", guiUnits = " m\xB3")]//Drill reach
        public float drillReach = 5; // How far can the drill actually reach? Used in calculating raycasts to hit ground down below the part. The 5 is just about the reach of the generic drill. Change in part cfg for different models.
        private bool IsDrillUnderground(out double undergroundAmount)
        {
            Vector3d partPosition = part.transform.position; // find the position of the transform in 3d space
            var scaleFactor = part.rescaleFactor; // what is the rescale factor of the drill?
            var drillDistance = drillReach * scaleFactor; // adjust the distance for the ray with the rescale factor, needs to be a float for raycast.

            undergroundAmount = 0;

            RaycastHit hit = new RaycastHit(); // create a variable that stores info about hit colliders etc.
            LayerMask terrainMask = 32768; // layermask in unity, number 1 bitshifted to the left 15 times (1 << 15), (terrain = 15, the bitshift is there so that the mask bits are raised; this is a good reading about that: http://answers.unity3d.com/questions/8715/how-do-i-use-layermasks.html)
            Ray drillPartRay = new Ray(partPosition, -part.transform.up); // this ray will start at the part's center and go down in local space coordinates (Vector3d.down is in world space)

            /* This little bit will fire a ray from the part, straight down, in the distance that the part should be able to reach.
             * It returns the resulting RayCastHit.
             * 
             * This is actually needed because stock KSP terrain detection is not really dependable. This module was formerly using just part.GroundContact 
             * to check for contact, but that seems to be bugged somehow, at least when paired with this drill - it works enough times to pass tests, but when testing 
             * this module in a difficult terrain, it just doesn't work properly. (I blame KSP planet meshes + Unity problems with accuracy further away from origin). 
            */
            Physics.Raycast(drillPartRay, out hit, drillDistance, terrainMask); // use the defined ray, pass info about a hit, go the proper distance and choose the proper layermask 

            // hit anything?
            if (hit.collider == null) return false;

            // how much is underground?
            undergroundAmount = drillDistance - hit.distance;

            return true;
        }
        // Duplicate code end

        [KSPField(isPersistant = false, guiActive = true, guiName = "Distance underground", guiFormat = "F2", guiUnits = "m")]
        public double undergroundAmount;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Effective size", guiFormat = "F2", guiUnits = "m")]
        public double effectiveSize;

        [KSPField(guiActive = true, guiName = "Cool Temp", guiFormat = "F2", guiUnits = "K")] public double coolTemp;
        [KSPField(guiActive = true, guiName = "Hot Temp", guiFormat = "F2", guiUnits = "K")] public double hotTemp;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Underground Temp", guiFormat = "F2", guiUnits = "K")] public double undergroundTemp;

        private const double meanGroundTempDistance = 10;
        private int frameSkipper;

        private void UpdateEffectiveSize()
        {
            effectiveSize = drillReach;
            undergroundAmount = 0;
            
            if (_radiatorState != ModuleDeployablePart.DeployState.EXTENDED) return;
            if (!IsDrillUnderground(out undergroundAmount)) return;

            effectiveSize += (10 * Math.Round(undergroundAmount, 2));
            
            // Distance reaches mean ground temp region? Time for a Natural bonus.
            if (undergroundAmount >= meanGroundTempDistance)
            {
                effectiveSize *= Math.Max(1.25, Math.Log(undergroundAmount - meanGroundTempDistance, Math.E));
            }
        }

        // Override the external temperature to be the average between the
        // hottest and the coldest temperature observed.
        public new double ExternalTemp()
        {
            if(coolTemp == 0 || hotTemp == 0)
            {
                return base.ExternalTemp();
            }

            return (coolTemp + hotTemp) / 2;
        }

        public new void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if ((++frameSkipper % 10) == 0)
            {
                // This does not need to run all the time.

                UpdateEffectiveSize();
                var undergroundTempField = Fields[nameof(undergroundTemp)];

                if (vessel != null && vessel.Landed && _radiatorState == ModuleDeployablePart.DeployState.EXTENDED)
                {
                    if (vessel.externalTemperature < coolTemp || coolTemp == 0) coolTemp = vessel.externalTemperature;
                    if (vessel.externalTemperature > hotTemp || hotTemp == 0) hotTemp = vessel.externalTemperature;
                    undergroundTempField.guiActive = true;
                }
                else
                {
                    coolTemp = hotTemp = 0;
                    undergroundTempField.guiActive = false;
                }
            }

            base.FixedUpdate();
        }
    }

    [KSPModule("Radiator")]
    class FNRadiator : ResourceSuppliableModule
    {
        // persitant
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_Cooling"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Radiator Cooling--Off--On
        public bool radiatorIsEnabled;
        [KSPField(isPersistant = false)]
        public bool canRadiateHeat = true;
        [KSPField(isPersistant = true)]
        public bool radiatorInit;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_Automated"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Automated-Off-On
        public bool isAutomated = true;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PivotOff"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Pivot--Off--On
        public bool pivotEnabled = true;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PreventShieldedDeploy"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Prevent Shielded Deploy-Off-On
        public bool preventShieldedDeploy = true;
        [KSPField(isPersistant = true)]
        public bool showRetractButton = false;
        [KSPField(isPersistant = true)]
        public bool showControls = true;
        [KSPField(isPersistant = true)]
        public double currentRadTemp;

        // non persistent
        [KSPField(guiName = "#LOC_KSPIE_Radiator_MaxVacuumTemp", guiFormat = "F0", guiUnits = "K")]//Max Vacuum Temp
        public double maxVacuumTemperature = _maximumRadiatorTempInSpace;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_MaxAtmosphereTemp", guiFormat = "F0", guiUnits = "K")]//Max Atmosphere Temp
        public double maxAtmosphereTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_MaxCurrentTemp", guiFormat = "F0", guiUnits = "K")]//Max Current Temp
        public double maxCurrentRadiatorTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_SpaceRadiatorBonus", guiFormat = "F0", guiUnits = "K")]//Space Radiator Bonus
        public double spaceRadiatorBonus;
        [KSPField]
        public string radiatorTypeMk1 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk1");//NaK Loop Radiator
        [KSPField]
        public string radiatorTypeMk2 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk2");//Mo Li Heat Pipe Mk1
        [KSPField]
        public string radiatorTypeMk3 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk3");//"Mo Li Heat Pipe Mk2"
        [KSPField]
        public string radiatorTypeMk4 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk4");//"Graphene Radiator Mk1"
        [KSPField]
        public string radiatorTypeMk5 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk5");//"Graphene Radiator Mk2"
        [KSPField]
        public string radiatorTypeMk6 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk6");//"Graphene Radiator Mk3"
        [KSPField]
        public bool showColorHeat = true;
        [KSPField]
        public string surfaceAreaUpgradeTechReq = null;
        [KSPField]
        public double surfaceAreaUpgradeMult = 1.6;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_Mass", guiUnits = " t")]//Mass
        public float partMass;
        [KSPField]
        public bool isDeployable = false;
        [KSPField]
        public bool isPassive = false;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_ConverctionBonus")]//Converction Bonus
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
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_Radiator_AtmosphereModifier")]//Atmosphere Modifier
        public double atmosphere_modifier;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_Type")]//Type
        public string radiatorType;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Radiator_RadiatorTemp")]//Rad Temp
        public string radiatorTempStr;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Radiator_PartTemp")]//Part Temp
        public string partTempStr;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_SurfaceArea", guiFormat = "F2", guiUnits = " m\xB2")]//Surface Area
        public double radiatorArea = 1;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_EffSurfaceArea", guiFormat = "F2", guiUnits = " m\xB2")]//Eff Surface Area
        public double effectiveRadiativeArea = 1;
        [KSPField]
        public double areaMultiplier = 1;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_EffectiveArea", guiFormat = "F2", guiUnits = " m\xB2")]//Effective Area
        public double effectiveRadiatorArea;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Radiator_PowerRadiated")]//Power Radiated
        public string thermalPowerDissipStr;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Radiator_PowerConvected")]//Power Convected
        public string thermalPowerConvStr;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_RadUpgradeCost")]//Rad Upgrade Cost
        public string upgradeCostStr;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_RadiatorStartTemp")]//Radiator Start Temp
        public double radiator_temperature_temp_val;
        [KSPField]
        public double instantaneous_rad_temp;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_DynamicPressureStress", guiActive = true, guiFormat = "P2")]//Dynamic Pressure Stress
        public double dynamicPressureStress;
        [KSPField(guiName = "#LOC_KSPIE_Radiator_MaxEnergyTransfer", guiFormat = "F2")]//Max Energy Transfer
        private double _maxEnergyTransfer;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_MaxRadiatorTemperature", guiFormat = "F0")]//Max Radiator Temperature
        public float maxRadiatorTemperature = _maximumRadiatorTempInSpace;

        [KSPField] public int nrAvailableUpgradeTechs;
        [KSPField] public bool hasSurfaceAreaUpgradeTechReq;
        [KSPField] public float atmosphereToleranceModifier = 1;
        [KSPField] public double atmosphericMultiplier;
        [KSPField] public float displayTemperature;
        [KSPField] public float colorRatio;
        [KSPField] public double deltaTemp;
        [KSPField] public double verticalSpeed;
        [KSPField] public double spaceRadiatorModifier;
        [KSPField] public double oxidationModifier;
        [KSPField] public double temperatureDifference;
        [KSPField] public double submergedModifier;
        [KSPField] public bool clarifyFunction;
        [KSPField] public double sphericalCowInAVaccum;

        const string kspShaderLocation = "KSP/Emissive/Bumped Specular";
        const int RADIATOR_DELAY = 20;
        const int DEPLOYMENT_DELAY = 6;

        private const float drapperPoint = 500; // 798

        static float _maximumRadiatorTempInSpace = 4500;
        static float maximumRadiatorTempAtOneAtmosphere = 1200;
        static float _maxSpaceTempBonus;
        static float _temperatureRange;

        // minimize garbage by recycling variables
        private double _stefanArea;
        private double _thermalPowerDissipationPerSecond;
        private double _radiatedThermalPower;
        private double _convectedThermalPower;

        private bool _active;
        private bool _isGraphene;

        private int _radiatorDeployDelay;
        private int _explodeCounter;

        private BaseEvent _deployRadiatorEvent;
        private BaseEvent _retractRadiatorEvent;

        private BaseField _thermalPowerConvStrField;
        private BaseField _radiatorIsEnabledField;
        private BaseField _isAutomatedField;
        private BaseField _pivotEnabledField;

        private Shader _kspShader;
        private Renderer _renderer;
        private Animation _deployAnimation;
        private AnimationState _anim;
        private Renderer[] _renderArray;
        private AnimationState[] _heatStates;
        private ModuleDeployableRadiator _moduleDeployableRadiator;
        private ModuleActiveRadiator _moduleActiveRadiator;
        internal ModuleDeployablePart.DeployState _radiatorState;
        private ResourceBuffers _resourceBuffers;

        private readonly Queue<double> _radTempQueue = new Queue<double>(20);
        private readonly Queue<double> _externalTempQueue = new Queue<double>(20);

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
            if (radiators_by_vessel.TryGetValue(vessel, out var vesselRadiator))
                return vesselRadiator;

            vesselRadiator = vessel.FindPartModulesImplementing<FNRadiator>().ToList();
            radiators_by_vessel.Add(vessel, vesselRadiator);

            return vesselRadiator;
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

            if (generation >= (int)GenerationType.Mk6 && _isGraphene)
                return RadiatorProperties.RadiatorTemperatureMk6;
            if (generation >= (int)GenerationType.Mk5 && _isGraphene)
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
                if (radiatorArea == 0)
                {
                    clarifyFunction = true;

                    if (MeshRadiatorSize(out var size) == true)
                    {
                        radiatorArea = Math.Round(size);
                    }

                    if (radiatorArea == 0)
                    {
                        // The Liquid Metal Cooled Reactor shows a tiny surface space, so this should not be an else statement
                        radiatorArea = 1;
                    }
                }

                effectiveRadiativeArea = PluginHelper.RadiatorAreaMultiplier * areaMultiplier * radiatorArea;

                // Because I have absolutely no idea what I'm doing, I'm taking some short cuts and major simplifications.
                // This is the radius of a circular radiator, (operating in a vacuum)
                sphericalCowInAVaccum = (effectiveRadiativeArea / Mathf.PI).Sqrt();

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
                return _maximumRadiatorTempInSpace;

            if (radiator_vessel.Any())
            {
                var maxRadiatorTemperature = radiator_vessel.Max(r => r.MaxRadiatorTemperature);
                var totalRadiatorsMass = radiator_vessel.Sum(r => (double)(decimal)r.part.mass);

                return radiator_vessel.Sum(r => Math.Min(1, r.GetAverageRadiatorTemperature() / r.MaxRadiatorTemperature) * maxRadiatorTemperature * (r.part.mass / totalRadiatorsMass));
            }
            else
                return _maximumRadiatorTempInSpace;
        }

        public static float getAverageMaximumRadiatorTemperatureForVessel(Vessel vess)
        {
            var radiatorVessel = GetRadiatorsForVessel(vess);

            float averageTemp = 0;
            float nRadiators = 0;

            foreach (FNRadiator radiator in radiatorVessel)
            {
                if (radiator == null) continue;

                averageTemp += radiator.maxRadiatorTemperature;
                nRadiators += 1;
            }

            if (nRadiators > 0)
                averageTemp = averageTemp / nRadiators;
            else
                averageTemp = 0;

            return averageTemp;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_DeployRadiator", active = true)]//Deploy Radiator
        public void DeployRadiator()
        {
            isAutomated = false;

            Debug.Log("[KSPI]: DeployRadiator Called ");

            Deploy();
        }

        private void Deploy()
        {
            if (preventShieldedDeploy && (part.ShieldedFromAirstream || _radiatorDeployDelay < RADIATOR_DELAY))
            {
                return;
            }

            Debug.Log("[KSPI]: Deploy Called ");

            if (_moduleDeployableRadiator != null)
                _moduleDeployableRadiator.Extend();

            ActivateRadiator();

            if (_deployAnimation == null) return;

            _deployAnimation[animName].enabled = true;
            _deployAnimation[animName].speed = 0.5f;
            _deployAnimation[animName].normalizedTime = 0f;
            _deployAnimation.Blend(animName, 2);
        }

        private void ActivateRadiator()
        {
            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Activate();

            radiatorIsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_RetractRadiator", active = true)]//Retract Radiator
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

            if (_deployAnimation == null) return;

            _deployAnimation[animName].enabled = true;
            _deployAnimation[animName].speed = -0.5f;
            _deployAnimation[animName].normalizedTime = 1;
            _deployAnimation.Blend(animName, 2);
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

        public bool MeshRadiatorSize(out double size)
        {
            size = 0;

            var mf = part.FindModelComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.Log("MeshRadiatorSize: Cannot find a MeshFilter in GetComponent");
                return false;
            }
            if (mf.mesh == null)
            {
                Debug.Log("MeshRadiatorSize: Cannot find a Mesh");
                return false;
            }

            var triangles = mf.mesh.triangles;
            var vertices = mf.mesh.vertices;

            double totalArea = 0.0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 corner = vertices[triangles[i]];
                Vector3 a = vertices[triangles[i + 1]] - corner;
                Vector3 b = vertices[triangles[i + 2]] - corner;

                totalArea += Vector3.Cross(a, b).magnitude;
            }

            if (totalArea.IsInfinityOrNaNorZero() == true)
            {
                Debug.Log("MeshRadiatorSize: total_area is IsInfinityOrNaNorZero :(");
                return false;
            }

            // convert from Mesh size to in game size. rescaleFactor changes when TweakScale modifies the size of the part.
            size = (totalArea / 2.0) * part.rescaleFactor;

            Debug.Log($"MeshRadiatorSize: surface_area is {size}, rescale_factor is {part.rescaleFactor}, and scale_factor is {part.scaleFactor}. Total radiator size is {size}");

            return true;
        }

        public double ExternalTemp()
        {
            // subclass may override, if needed
            return (vessel == null) ? PhysicsGlobals.SpaceTemperature : vessel.externalTemperature;
        }

        public override void OnStart(StartState state)
        {
            string[] resourcesToSupply = { ResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resourcesToSupply;

            base.OnStart(state);

            _radiatedThermalPower = 0;
            _convectedThermalPower = 0;
            CurrentRadiatorTemperature = 0;
            _radiatorDeployDelay = 0;

            DetermineGenerationType();

            _isGraphene = !string.IsNullOrEmpty(surfaceAreaUpgradeTechReq);
            _maximumRadiatorTempInSpace = (float)RadiatorProperties.RadiatorTemperatureMk6;
            _maxSpaceTempBonus = _maximumRadiatorTempInSpace - maximumRadiatorTempAtOneAtmosphere;
            _temperatureRange = _maximumRadiatorTempInSpace - drapperPoint;

            _kspShader = Shader.Find(kspShaderLocation);
            maxRadiatorTemperature = (float)MaxRadiatorTemperature;

            part.heatConvectiveConstant = convectiveBonus;
            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            effectiveRadiatorArea = EffectiveRadiatorArea;
            _stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            _deployRadiatorEvent = Events[nameof(DeployRadiator)];
            _retractRadiatorEvent = Events[nameof(RetractRadiator)];

            _thermalPowerConvStrField = Fields[nameof(thermalPowerConvStr)];
            _radiatorIsEnabledField = Fields[nameof(radiatorIsEnabled)];
            _isAutomatedField = Fields[nameof(isAutomated)];
            _pivotEnabledField = Fields[nameof(pivotEnabled)];

            var preventDeployField = Fields[nameof(preventShieldedDeploy)];
            preventDeployField.guiActive = isDeployable;
            preventDeployField.guiActiveEditor = isDeployable;

            Actions[nameof(DeployRadiatorAction)].guiName = Events[nameof(DeployRadiator)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_DeployRadiator");//"Deploy Radiator"
            Actions[nameof(ToggleRadiatorAction)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_ToggleRadiator");//String.Format("Toggle Radiator")
            Actions[nameof(RetractRadiatorAction)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_RetractRadiator");//"Retract Radiator"

            Events[nameof(RetractRadiator)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_RetractRadiator");//"Retract Radiator"

            var myAttachedEngine = part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                partMass = part.mass;
                Fields[nameof(partMass)].guiActiveEditor = true;
                Fields[nameof(partMass)].guiActive = true;
                Fields[nameof(convectiveBonus)].guiActiveEditor = true;
            }

            if (!string.IsNullOrEmpty(thermalAnim))
            {
                _heatStates = PluginHelper.SetUpAnimation(thermalAnim, part);

                if (_heatStates != null)
                    SetHeatAnimationRatio(0);
            }

            _deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            if (_deployAnimation != null)
            {
                _deployAnimation[animName].layer = 1;
                _deployAnimation[animName].speed = 0;

                _deployAnimation[animName].normalizedTime = radiatorIsEnabled ? 1 : 0;
            }

            _moduleActiveRadiator = part.FindModuleImplementing<ModuleActiveRadiator>();

            if (_moduleActiveRadiator != null)
            {
                _moduleActiveRadiator.Events[nameof(_moduleActiveRadiator.Activate)].guiActive = false;
                _moduleActiveRadiator.Events[nameof(_moduleActiveRadiator.Shutdown)].guiActive = false;
            }
            _moduleDeployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            if (_moduleDeployableRadiator != null)
                _radiatorState = _moduleDeployableRadiator.deployState;

            var radiatorfield = Fields[nameof(radiatorIsEnabled)];
            radiatorfield.guiActive = showControls;
            radiatorfield.guiActiveEditor = showControls;
            radiatorfield.OnValueModified += radiatorIsEnabled_OnValueModified;

            var automatedfield = Fields[nameof(isAutomated)];
            automatedfield.guiActive = showControls;
            automatedfield.guiActiveEditor = showControls;

            var pivotfield = Fields[nameof(pivotEnabled)];
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

            for (var i = 0; i < 20; i++)
            {
                _radTempQueue.Enqueue(currentRadTemp);
            }

            InitializeTemperatureColorChannels();

            ApplyColorHeat();

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            _renderArray = part.FindModelComponents<Renderer>().ToArray();

            if (radiatorInit == false)
                radiatorInit = true;

            radiatorTempStr = maxRadiatorTemperature + "K";

            maxVacuumTemperature = _isGraphene ? Math.Min(maxVacuumTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk4, maxRadiatorTemperature);
            maxAtmosphereTemperature = _isGraphene ? Math.Min(maxAtmosphereTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk3, maxRadiatorTemperature);

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
                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 2.0e+6));
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                _resourceBuffers.Init(this.part);
            }

            Fields[nameof(dynamicPressureStress)].guiActive = isDeployable;
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

            var canBeActive = showControls && isDeployable && isDeployStateUndefined;

            _deployRadiatorEvent.active = canBeActive && !radiatorIsEnabled;
            _retractRadiatorEvent.active = canBeActive && radiatorIsEnabled;
        }

        public override void OnUpdate() // is called while in flight
        {

            _radiatorDeployDelay++;

            if (_moduleDeployableRadiator != null && (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED ||
                                                       _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED))
            {
                if (_radiatorState != _moduleDeployableRadiator.deployState)
                {
                    part.SendMessage("GeometryPartModuleRebuildMeshData");
                    Debug.Log("[KSPI]: Updating geometry mesh due to radiator deployment.");
                }
                _radiatorState = _moduleDeployableRadiator.deployState;
            }

            _stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            oxidationModifier = 0;

            UpdateMaxCurrentTemperature();

            if (keepMaxPartTempEqualToMaxRadiatorTemp && double.IsNaN(maxCurrentRadiatorTemperature) == false)
            {
                part.skinMaxTemp = maxCurrentRadiatorTemperature;
                part.maxTemp = maxCurrentRadiatorTemperature;
            }

            _thermalPowerConvStrField.guiActive = _convectedThermalPower > 0;

            // synchronize states
            if (_moduleDeployableRadiator != null && pivotEnabled && showControls)
            {
                if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)
                    ActivateRadiator();
                else if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED)
                    DeactivateRadiator();
            }

            _radiatorIsEnabledField.guiActive = !isPassive && showControls;
            _radiatorIsEnabledField.guiActiveEditor = !isPassive && showControls;

            _isAutomatedField.guiActive = showControls && isDeployable;
            _isAutomatedField.guiActiveEditor = showControls && isDeployable;

            _pivotEnabledField.guiActive = showControls && isDeployable;
            _pivotEnabledField.guiActiveEditor = showControls && isDeployable;

            if (radiatorIsEnabled && canRadiateHeat)
            {
                thermalPowerDissipStr = PluginHelper.getFormattedPowerString(_radiatedThermalPower, "0.0", "0.000");
                thermalPowerConvStr = PluginHelper.getFormattedPowerString(_convectedThermalPower, "0.0", "0.000");
            }
            else
            {
                thermalPowerDissipStr = Localizer.Format("#LOC_KSPIE_Radiator_disabled");//"disabled"
                thermalPowerConvStr = Localizer.Format("#LOC_KSPIE_Radiator_disabled");//"disabled"
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
                var combinedPressure = vessel.staticPressurekPa + vessel.dynamicPressurekPa * 0.2;

                if (combinedPressure > 101.325)
                {
                    var extraPressure = combinedPressure - 101.325;
                    var ratio = extraPressure / 101.325;
                    if (ratio <= 1)
                        ratio *= ratio;
                    else
                        ratio = Math.Sqrt(ratio);
                    oxidationModifier = 1 + ratio * 0.1;
                }
                else
                    oxidationModifier = Math.Pow(combinedPressure / 101.325, 0.25);

                spaceRadiatorModifier = Math.Max(0.25, Math.Min(0.95, 0.95 + vessel.verticalSpeed * 0.002));

                spaceRadiatorBonus = (1 / spaceRadiatorModifier) * _maxSpaceTempBonus * (1 - oxidationModifier);

                maxCurrentRadiatorTemperature = Math.Min(maxVacuumTemperature, Math.Max(PhysicsGlobals.SpaceTemperature, maxAtmosphereTemperature + spaceRadiatorBonus));
            }
            else
            {
                spaceRadiatorModifier = 0.95;
                spaceRadiatorBonus = _maxSpaceTempBonus;
                maxCurrentRadiatorTemperature = maxVacuumTemperature;
            }
            verticalSpeed = vessel.verticalSpeed;
        }

        public override void OnFixedUpdate()
        {
            _active = true;
            base.OnFixedUpdate();
        }
        private double PartRotationDistance()
        {
            // how much did we rotate
            var rb = part.GetComponent<Rigidbody>();
            if (rb == null)
            {
                // should not happen.
                return 0;
            }

            // rb.angularVelocity.magnitude in radians/second
            double tmp = 180 * Math.Abs(rb.angularVelocity.magnitude);
            // calculate the linear velocity
            double tmpVelocity = tmp / (Mathf.PI * sphericalCowInAVaccum);
            // and then distance traveled.
            double distanceTraveled = effectiveRadiatorArea * tmpVelocity;

            return distanceTraveled;
        }

        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;

                if (!_active)
                    base.OnFixedUpdate();

                if (_resourceBuffers != null)
                {
                    _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, radiatorIsEnabled ? this.part.mass : this.part.mass * 1e-3);
                    _resourceBuffers.UpdateBuffers();
                }

                // get resource bar ratio at start of frame
                var wasteheatManager = getManagerForVessel(ResourceManager.FNRESOURCE_WASTEHEAT);

                if (double.IsNaN(wasteheatManager.TemperatureRatio))
                {
                    Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Single.IsNaN detected in TemperatureRatio");
                    return;
                }

                // ToDo replace wasteheatManager.SqrtResourceBarRatioBegin by ResourceBarRatioBegin after generators hotbath takes into account expected temperature
                radiator_temperature_temp_val = Math.Min(maxRadiatorTemperature * wasteheatManager.TemperatureRatio, maxCurrentRadiatorTemperature);

                atmosphericMultiplier = Math.Sqrt(vessel.atmDensity);

                deltaTemp = Math.Max(radiator_temperature_temp_val - Math.Max(ExternalTemp() * Math.Min(1, atmosphericMultiplier), PhysicsGlobals.SpaceTemperature), 0);
                var deltaTempToPowerFour = deltaTemp * deltaTemp * deltaTemp * deltaTemp;

                if (radiatorIsEnabled)
                {
                    if (!CheatOptions.IgnoreMaxTemperature && wasteheatManager.ResourceBarRatioBegin >= 1 && CurrentRadiatorTemperature >= maxRadiatorTemperature)
                    {
                        _explodeCounter++;
                        if (_explodeCounter > 25)
                            part.explode();
                    }
                    else
                        _explodeCounter = 0;

                    _thermalPowerDissipationPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * _stefanArea;

                    if (double.IsNaN(_thermalPowerDissipationPerSecond))
                        Debug.LogWarning("[KSPI]: FNRadiator: FixedUpdate Single.IsNaN detected in _thermalPowerDissipationPerSecond");

                    _radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(_thermalPowerDissipationPerSecond, wasteheatManager) : 0;

                    if (double.IsNaN(_radiatedThermalPower))
                        Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Single.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + _thermalPowerDissipationPerSecond + ")");

                    instantaneous_rad_temp = CalculateInstantaneousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;

                    if (_moduleDeployableRadiator)
                        _moduleDeployableRadiator.hasPivot = pivotEnabled;
                }
                else
                {
                    _thermalPowerDissipationPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * _stefanArea * 0.5;

                    _radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(_thermalPowerDissipationPerSecond, wasteheatManager) : 0;

                    instantaneous_rad_temp = CalculateInstantaneousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;
                }

                if (vessel.atmDensity > 0)
                {
                    atmosphere_modifier = vessel.atmDensity * convectiveBonus + vessel.speed.Sqrt() + PartRotationDistance().Sqrt();

                    const double heatTransferCoefficient = 0.0005; // 500W/m2/K
                    temperatureDifference = Math.Max(0, CurrentRadiatorTemperature - ExternalTemp());
                    submergedModifier = Math.Max(part.submergedPortion * 10, 1);

                    var convPowerDissipation = wasteheatManager.RadiatorEfficiency * atmosphere_modifier * temperatureDifference * effectiveRadiatorArea * heatTransferCoefficient * submergedModifier;

                    if (!radiatorIsEnabled)
                        convPowerDissipation = convPowerDissipation * 0.25;

                    _convectedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(convPowerDissipation, wasteheatManager) : 0;

                    if (_radiatorDeployDelay >= DEPLOYMENT_DELAY)
                        DeploymentControl();
                }
                else
                {
                    submergedModifier = 0;
                    temperatureDifference = 0;
                    _convectedThermalPower = 0;

                    if (radiatorIsEnabled || !isAutomated || !canRadiateHeat || !showControls || _radiatorDeployDelay < DEPLOYMENT_DELAY) return;

                    //Debug.Log("[KSPI]: FixedUpdate Automated Deployment ");
                    Deploy();
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception on " + part.name + " during FNRadiator.FixedUpdate with message " + e.Message);
            }
        }

        private double CalculateInstantaneousRadTemp()
        {
            var result = Math.Min(maxCurrentRadiatorTemperature, radiator_temperature_temp_val);

            if (result.IsInfinityOrNaN())
                Debug.LogError("[KSPI]: FNRadiator: FixedUpdate IsNaN or Infinity detected in CalculateInstantaneousRadTemp");

            return result;
        }

        private void DeploymentControl()
        {
            dynamicPressureStress = 4 * vessel.dynamicPressurekPa;

            if (dynamicPressureStress > 1)
            {
                if (!isDeployable || !radiatorIsEnabled) return;

                if (isAutomated)
                {
                    Debug.Log("[KSPI]: DeploymentControl Auto Retracted, stress at " + dynamicPressureStress.ToString("P2") + "%");
                    Retract();
                }
                else
                {
                    if (CheatOptions.UnbreakableJoints) return;

                    Debug.Log("[KSPI]: DeploymentControl Decoupled!");
                    part.deactivate();
                    part.decouple(1);
                }
            }
            else if (!radiatorIsEnabled && isAutomated && canRadiateHeat && showControls && (!preventShieldedDeploy || !part.ShieldedFromAirstream))
            {
                // Suppress message spam on repeated deploy attempts due to radiator delay
                //if (radiator_deploy_delay > DEPLOYMENT_DELAY)
                Debug.Log("[KSPI]: DeploymentControl Auto Deploy");
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
            get => currentRadTemp;
            set
            {
                if (!value.IsInfinityOrNaNorZero())
                {
                    currentRadTemp = value;
                    _radTempQueue.Enqueue(currentRadTemp);
                    if (_radTempQueue.Count > 20)
                        _radTempQueue.Dequeue();
                }

                var currentExternalTemp = PhysicsGlobals.SpaceTemperature;

                if (vessel != null && vessel.atmDensity > 0)
                    currentExternalTemp = ExternalTemp() * Math.Min(1, vessel.atmDensity);

                _externalTempQueue.Enqueue(Math.Max(PhysicsGlobals.SpaceTemperature, currentExternalTemp));
                if (_externalTempQueue.Count > 20)
                    _externalTempQueue.Dequeue();
            }
        }

        private double GetAverageRadiatorTemperature()
        {
            return Math.Max(_externalTempQueue.Count > 0 ? _externalTempQueue.Average() : Math.Max(PhysicsGlobals.SpaceTemperature, vessel.externalTemperature), _radTempQueue.Count > 0 ? _radTempQueue.Average() : currentRadTemp);
        }

        public override string GetInfo()
        {
            DetermineGenerationType();

            RadiatorProperties.Initialize();

            effectiveRadiatorArea = EffectiveRadiatorArea;
            _stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            var sb = new StringBuilder();
            sb.Append("<size=11>");

            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_radiatorArea") + $" {radiatorArea:F2} m\xB2 \n");//Base surface area:
            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_Area_Mass") + $" : {radiatorArea / part.mass:F2}\n");//Surface area / Mass

            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_Area_Bonus") + $" {(string.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? 0 : surfaceAreaUpgradeMult - 1):P0}\n");//Surface Area Bonus:
            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_AtmConvectionBonus") + $" {convectiveBonus - 1:P0}\n");//Atm Convection Bonus:

            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_MaximumWasteHeatRadiatedMk1") + $" {RadiatorProperties.RadiatorTemperatureMk1:F0} K {_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk1, 4):F3} MW\n");//\nMaximum Waste Heat Radiated\nMk1:

            sb.Append($"Mk2: {RadiatorProperties.RadiatorTemperatureMk2:F0} K {_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk2, 4):F3} MW\n");
            sb.Append($"Mk3: {RadiatorProperties.RadiatorTemperatureMk3:F0} K {_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk3, 4):F3} MW\n");
            sb.Append($"Mk4: {RadiatorProperties.RadiatorTemperatureMk4:F0} K {_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk4, 4):F3} MW\n");

            if (String.IsNullOrEmpty(surfaceAreaUpgradeTechReq)) return sb.ToString();

            sb.Append($"Mk5: {RadiatorProperties.RadiatorTemperatureMk5:F0} K {_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk5, 4):F3} MW\n");
            sb.Append($"Mk6: {RadiatorProperties.RadiatorTemperatureMk6:F0} K {_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk6, 4):F3} MW\n");

            var convection = 0.9 * effectiveRadiatorArea * convectiveBonus;
            var dissipation = _stefanArea * Math.Pow(900, 4);

            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_Maximumat1atmosphere", $"{dissipation:F3}", $"{convection:F3}"));
            //String.Format("\nMaximum @ 1 atmosphere : 1200 K, dissipation: {0:F3} MW\n, convection: {1:F3} MW\n", disapation, convection)

            sb.Append("</size>");

            return sb.ToString();
        }

        public override int getPowerPriority()
        {
            return 3;
        }

        private void SetHeatAnimationRatio(float color)
        {
            var heatStatesCount = _heatStates.Count();
            for (var i = 0; i < heatStatesCount; i++)
            {
                _anim = _heatStates[i];
                if (_anim == null)
                    continue;
                _anim.normalizedTime = color;
            }
        }

        private void ApplyColorHeat()
        {
            displayTemperature = (float)GetAverageRadiatorTemperature();

            colorRatio = displayTemperature < drapperPoint ? 0 : Mathf.Min(1, (Mathf.Max(0, displayTemperature - drapperPoint) / _temperatureRange) * 1.05f);

            if (_heatStates != null && _heatStates.Any())
            {
                SetHeatAnimationRatio(colorRatio.Sqrt());
            }
            else if (!string.IsNullOrEmpty(colorHeat) && colorRatioExponent != 0)
            {
                if (_renderArray == null)
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

                for (var i = 0; i < _renderArray.Count(); i++)
                {
                    _renderer = _renderArray[i];

                    if (_renderer == null || _renderer.material == null)
                        continue;

                    if (_renderer.material.shader != null && _renderer.material.shader.name != kspShaderLocation)
                        _renderer.material.shader = _kspShader;

                    if (!string.IsNullOrEmpty(emissiveTextureLocation))
                    {
                        if (_renderer.material.GetTexture("_Emissive") == null)
                            _renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture(emissiveTextureLocation, false));
                    }

                    if (!string.IsNullOrEmpty(bumpMapTextureLocation))
                    {
                        if (_renderer.material.GetTexture("_BumpMap") == null)
                            _renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture(bumpMapTextureLocation, false));
                    }

                    _renderer.material.SetColor(colorHeat, emissiveColor);
                }
            }
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title + (clarifyFunction ? " (radiator)" : "");
        }
    }
}