using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static System.String;

namespace FNPlugin.Propulsion
{
    [KSPModule("#LOC_KSPIE_AlcubierreDrive_partModuleName")]
    class AlcubierreDrive : ResourceSuppliableModule
    {
        public const string Group = "AlcubierreDrive";
        public const string GroupTitle = "#LOC_KSPIE_AlcubierreDrive_groupName";
        public const string warpEffect1ShaderPath = "Unlit/Transparent";
        public const string warpEffect2ShaderPath = "Unlit/Transparent";

        // Persistent
        [KSPField(isPersistant = true)] public bool IsEnabled;
        [KSPField(isPersistant = true)] public bool IsCharging;
        [KSPField(isPersistant = true)] private double existing_warp_speed;
        [KSPField(isPersistant = true)] public int selected_factor = -1;
        [KSPField(isPersistant = true)] public int target_factor = -1;
        [KSPField(isPersistant = true)] public bool isupgraded;
        [KSPField(isPersistant = true)] public string serialisedwarpvector;

        // Setting
        [KSPField] public long InstanceID;
        [KSPField] public long maxPowerTimeout = 50;

        [KSPField] public bool useRotateStability = true;
        [KSPField] public bool allowWarpTurning = true;

        [KSPField] public string upgradeTechReq = "";
        [KSPField] public string warpSoundPath = "WarpPlugin/Sounds/warp_sound";
        [KSPField] public string AnimationName = "";
        [KSPField] public string upgradedName = "";
        [KSPField] public string originalName = "";

        //public const string warpWhiteFlashPath = "WarpPlugin/ParticleFX/warp10";
        //public const string warpRedFlashPath = "WarpPlugin/ParticleFX/warpr10";
        //public const string warpTexturePath = "WarpPlugin/ParticleFX/warp";
        //public const string warprTexturePath = "WarpPlugin/ParticleFX/warpr";

        [KSPField] public float effectSize1 = 0;
        [KSPField] public float effectSize2 = 0;
        [KSPField] public float warpSize = 50000;
        [KSPField] public float warpPowerMultTech0 = 10;
        [KSPField] public float warpPowerMultTech1 = 20;
        [KSPField] public float headingChangedTimeout = 50;

        [KSPField] public double warpSoundPitchExp = 0.1;
        [KSPField] public double warpPowerReqMult = 0.5;
        [KSPField] public double responseMultiplier = 0.005;
        [KSPField] public double antigravityMultiplier = 2;
        [KSPField] public double GThreshold = 2;
        [KSPField] public double powerRequirementMultiplier = 1;
        [KSPField] public double wasteheatRatio = 0.5;
        [KSPField] public double wasteheatRatioUpgraded = 0.25;
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public double maximumWarpWeighted;
        [KSPField] public double magnitudeDiff;
        [KSPField] public double exotic_power_required = 1000;
        [KSPField] public double gravityMaintenancePowerMultiplier = 4;

        //GUI
        [KSPField(groupName = Group, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_SafetyDistance", guiUnits = " Km"), UI_FloatRange(minValue = 0, maxValue = 200, stepIncrement = 1)]//Safety Distance
        public float spaceSafetyDistance = 30;
        [KSPField(groupName = Group, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_AntigravityPercentage"), UI_FloatRange(minValue = 0, maxValue = 200, stepIncrement = 2)]//Exotic Matter Percentage
        public float antigravityPercentage;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_warpdriveType")]
        public string warpdriveType = "Alcubierre Drive";
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_engineMass", guiFormat = "F3", guiUnits = " t")]
        public float partMass;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_warpStrength", guiFormat = "F1", guiUnits = " t")]
        public float warpStrength = 1;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_totalWarpPower", guiFormat = "F1", guiUnits = " t")]
        public float totalWarpPower;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_vesselTotalMass", guiFormat = "F3", guiUnits = " t")]
        public double vesselTotalMass;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_warpToMassRatio", guiFormat = "F4")]
        public double warpToMassRatio;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_gravityAtSeaLevel", guiUnits = " m/s\xB2", guiFormat = "F5")]
        public double gravityAtSeaLevel;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_gravityVesselPull", guiUnits = " m/s\xB2", guiFormat = "F5")]
        public double gravityPull;
        [KSPField(groupName = Group, guiActive = false,  guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_gravityDragRatio", guiFormat = "F5")]
        public double gravityDragRatio;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_gravityDragPercentage", guiUnits = "%", guiFormat = "F3")]
        public double gravityDragPercentage;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_gravityRatio")]
        public double gravityRatio;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_maxWarpGravityLimit", guiUnits = "c", guiFormat = "F4")]
        public double maximumWarpForGravityPull;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_maxWarpAltitudeLimit", guiUnits = "c", guiFormat = "F4")]
        public double maximumWarpForAltitude;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_maxAllowedThrotle", guiUnits = "c", guiFormat = "F4")]
        public double maximumAllowedWarpThrotle;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentSelectedSpeed", guiUnits = "c", guiFormat = "F4")]
        public double warpEngineThrottle;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_minPowerReqForLightSpeed", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F4")]
        public double minPowerRequirementForLightSpeed;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentPowerReqForWarp", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F4")]
        public double currentPowerRequirementForWarp;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_powerReqForMaxAllowedSpeed", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F4")]
        public double powerRequirementForMaximumAllowedLightSpeed;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_PowerRequirementForSlowedSubLightSpeed", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F4")]//Power Requirement For Slowed SubLightSpeed
        public double powerRequirementForSlowedSubLightSpeed;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentWarpExitSpeed", guiUnits = " m/s", guiFormat = "F3")]
        public double exitSpeed;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentWarpExitApoapsis", guiUnits = " km", guiFormat = "F3")]
        public double exitApoapsis;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentWarpExitPeriapsis", guiUnits = " km", guiFormat = "F3")]
        public double exitPeriapsis;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentWarpExitEccentricity", guiFormat = "F3")]
        public double exitEccentricity;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentWarpExitMeanAnomaly", guiUnits = "\xB0", guiFormat = "F3")]
        public double exitMeanAnomaly;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_currentWarpExitBurnToCircularize", guiUnits = " m/s", guiFormat = "F3")]
        public double exitBurnCircularize;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_status")]
        public string driveStatus;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_CosineAngleToClosestBody", guiFormat = "F3")]//Cos Angle To Closest Body
        private double cosineAngleToClosestBody;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_Distancetoclosestbody", guiFormat = "F0", guiUnits = " m")]//Distance to closest body
        private double distanceToClosestBody;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_Nameofclosestbody")]//Name of closest body
        string closestCelestrialBodyName;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_AllowedWarpDistancePerFrame", guiFormat = "F3")]//Max distance per frame
        private double allowedWarpDistancePerFrame;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_MaximumWarpSpeed", guiFormat = "F3")]//Maximum Warp Speed
        private double maximumWarpSpeed;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_Safetydistance", guiFormat = "F3", guiUnits = " m")]//Safety distance
        private double safetyDistance;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_DropoutDistance", guiFormat = "F3", guiUnits = " m")]//Dropout Distance
        private double dropoutDistance;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_AvailablePower", guiFormat = "F3", guiUnits = "MJ")]//Available Power for Warp
        private double availablePower;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_GravityAcceleration", guiFormat = "F3", guiUnits = " m/s\xB2")]//Gravity Acceleration
        private double gravityAcceleration;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_AntiGravityAcceleration", guiFormat = "F3", guiUnits = " m/s\xB2")]//Anti Gravity Acceleration
        private double antigravityAcceleration;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_VerticalSpeed", guiFormat = "F3", guiUnits = " m/s")]//Vertical Speed
        private double verticalSpeed;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_MaintenancePowerReq", guiFormat = "F3", guiUnits = " m/s")]//Maintenance Power Req
        private double requiredExoticMaintenancePower;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_ChargePowerDraw", guiFormat = "F3", guiUnits = " m/s")]//Charge Power Draw
        private double chargePowerDraw;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_AlcubierreDrive_MaxChargePowerRequired", guiFormat = "F3", guiUnits = " m/s")]//Max Charge Power Required
        private double currentPowerRequired;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_WarpWindow"), UI_Toggle(disabledText = "#LOC_KSPIE_AlcubierreDrive_WarpWindow_Hidden", enabledText = "#LOC_KSPIE_AlcubierreDrive_WarpWindow_Shown", affectSymCounterparts = UI_Scene.None)]//Warp Window--Hidden--Shown
        public bool showWindow;
        [KSPField(groupName = Group, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_AutoRendevousCircularize"), UI_Toggle(disabledText = "#LOC_KSPIE_AlcubierreDrive_False", enabledText = "#LOC_KSPIE_AlcubierreDrive_True", affectSymCounterparts = UI_Scene.All)]//Auto Rendevous/Circularize-False-True
        public bool matchExitToDestinationSpeed = true;
        [KSPField(groupName = Group, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_AutoMaximizeWarpSpeed"), UI_Toggle(disabledText = "#LOC_KSPIE_AlcubierreDrive_Disabled", enabledText = "#LOC_KSPIE_AlcubierreDrive_Enabled", affectSymCounterparts = UI_Scene.All)]//Auto Maximize Warp Speed -Disabled-Enabled
        public bool maximizeWarpSpeed = false;
        [KSPField(groupName = Group, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AlcubierreDrive_AutoHoldAltitude"), UI_Toggle(disabledText = "#LOC_KSPIE_AlcubierreDrive_Disabled", enabledText = "#LOC_KSPIE_AlcubierreDrive_Enabled", affectSymCounterparts = UI_Scene.All)]//Auto Hold Altitude--Disabled--Enabled
        public bool holdAltitude;
        [KSPField(groupName = Group, isPersistant = true, guiActiveEditor = true, guiName = "Main Control"), UI_Toggle(disabledText = "#LOC_KSPIE_AlcubierreDrive_Enabled", enabledText = "#LOC_KSPIE_AlcubierreDrive_Disabled", affectSymCounterparts = UI_Scene.None)]
        public bool IsSlave;
        [KSPField(groupName = Group, isPersistant = true, guiActiveEditor = true, guiName = "Trail Effects"), UI_Toggle(disabledText = "#LOC_KSPIE_AlcubierreDrive_Enabled", enabledText = "#LOC_KSPIE_AlcubierreDrive_Disabled", affectSymCounterparts = UI_Scene.All)]
        public bool hideTrail = false;

        // Debugging
        double _receivedExoticMaintenancePower;
        double _exoticMatterMaintenanceRatio;
        double _exoticMatterProduced;
        double _minimumExoticMatterMaintenanceRatio;
        double _stablePowerSupply;
        double _currentRequiredExoticMatter;

        private readonly double[] _engineThrottle = { 0.001, 0.0013, 0.0016, 0.002, 0.0025, 0.0032, 0.004, 0.005, 0.0063, 0.008, 0.01, 0.013, 0.016, 0.02, 0.025, 0.032, 0.04, 0.05, 0.063, 0.08, 0.1, 0.13, 0.16, 0.2, 0.25, 0.32, 0.4, 0.5, 0.63, 0.8, 1, 1.3, 1.6, 2, 2.5, 3.2, 4, 5, 6.3, 8, 10, 13, 16, 20, 25, 32, 40, 50, 63, 80, 100, 130, 160, 200, 250, 320, 400, 500, 630, 800, 1000, 1300, 1600, 2000, 2500, 3200, 4000, 5000, 6300, 8000, 10000, 13000, 16000, 20000, 25000, 32000, 40000, 50000, 63000, 80000, 100000 };

        private GameObject _warpEffect;
        private GameObject _warpEffect2;
        private Texture[] _warpTextures;
        private Texture[] _warpTextures2;
        private AudioSource _warpSound;

        double _currentExoticMatter;
        double _maxExoticMatter;
        double _exoticMatterRatio;
        double _texCount;

        private bool _stopWarpSoonAsPossible;
        private bool _startWarpAsSoonAsPossible;
        private bool _activeTrail;
        private bool _vesselWasInOuterSpace;
        private bool _hasRequiredUpgrade;
        private bool _selectedTargetVesselIsClosest;

        private float _windowPositionX = 200;
        private float _windowPositionY = 100;

        private int _windowId = 252824373;
        private int _minimumSelectedFactor;
        private int _maximumWarpSpeedFactor;
        private int _minimumPowerAllowedFactor;
        private int _warpTrailTimeout;

        private long _insufficientPowerTimeout = 10;
        private long _initiateWarpTimeout;
        private long _counterCurrent;
        private long _counterPreviousChange;

        private Renderer _warpEffect1Renderer;
        private Renderer _warpEffect2Renderer;

        private Collider _warpEffect1Collider;
        private Collider _warpEffect2Collider;

        private GUIStyle _boldBlackStyle;
        private GUIStyle _textBlackStyle;

        private Vector3d _headingAct;
        private Vector3d _activePartHeading;

        private CelestialBody _warpInitialMainBody;
        private CelestialBody _closestCelestialBody;

        private Rect windowPosition;
        private AnimationState[] animationState;
        private List<AlcubierreDrive> _alcubierreDrives;
        private PartResourceDefinition _exoticResourceDefinition;
        private Vector3d _departureVelocity;
        private ModuleReactionWheel _moduleReactionWheel;
        private ResourceBuffers _resourceBuffers;

        //private Texture2D warpWhiteFlash;
        //private Texture2D warpRedFlash;

        private float WarpPower => isupgraded ? warpPowerMultTech1 : warpPowerMultTech0;

        private BaseField antigravityField;

        // Actions
        [KSPAction("#LOC_KSPIE_AlcubierreDrive_Toggle_WarpWindow")]
        public void ToggleNextPropellantAction(KSPActionParam param)
        {
            showWindow = !showWindow;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_startChargingDrive", active = true)]
        public void StartCharging()
        {
            Debug.Log("[KSPI]: Start Charging pressed");

            if (IsEnabled) return;

            if (warpToMassRatio < 1)
            {
                Debug.Log("[KSPI]: warpToMassRatio: " + warpToMassRatio + " is less than 1" );
                var message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_warpStrenthToLowForVesselMass");
                ScreenMessages.PostScreenMessage(message);
                return;
            }

            _insufficientPowerTimeout = maxPowerTimeout;
            IsCharging = true;
            holdAltitude = false;
            antigravityPercentage = 100;

            UpdateAllWarpDriveEffects();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_stopChargingDrive", active = false)]
        public void StopCharging()
        {
            Debug.Log("[KSPI]: Stop Charging button pressed");
            IsCharging = false;
            holdAltitude = false;
            _stopWarpSoonAsPossible = false;
            _startWarpAsSoonAsPossible = false;
            antigravityPercentage = 0;

            UpdateAllWarpDriveEffects();
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_activateWarpDrive")]
        public void ActivateWarpDriveAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: Activate Warp Drive action activated");
            StartWarpAsSoonAsPossible();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_activateWarpDrive", active = true)]
        public void ActivateWarpDrive()
        {
            Debug.Log("[KSPI]: Activate Warp Drive button pressed");
            StartWarpAsSoonAsPossible();
        }

        private void ActivateWarpDrive(bool feedback)
        {
            if (IsEnabled) return;

            if (warpToMassRatio < 1)
            {
                if (feedback)
                {
                    var message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_notEnoughWarpPowerToVesselMass");
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                return;
            }

            if (!CheatOptions.IgnoreMaxTemperature && vessel.atmDensity > 0)
            {
                if (feedback)
                {
                    var message =
                        Localizer.Format("#LOC_KSPIE_AlcubierreDrive_cannotActivateWarpdriveWithinAtmosphere");
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                return;
            }

            if (antigravityPercentage < 0.5 || (_minimumExoticMatterMaintenanceRatio < 0.995 && antigravityPercentage >= 0.5))
            {
                if (feedback)
                {
                    var message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_warpdriveIsNotFullyChargedForWarp");
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                return;
            }

            if (_maximumWarpSpeedFactor < selected_factor)
                selected_factor = _minimumPowerAllowedFactor;

            if (!CheatOptions.InfiniteElectricity && GetPowerRequirementForWarp(_engineThrottle[selected_factor]) >
                GetStableResourceSupply(ResourceSettings.Config.ElectricPowerInMegawatt))
            {
                if (feedback)
                {
                    var message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_warpPowerReqIsHigherThanMaxPowerSupply");
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                return;
            }

            IsCharging = false;
            _stopWarpSoonAsPossible = false;
            _startWarpAsSoonAsPossible = false;
            antigravityPercentage = 0;
            _initiateWarpTimeout = 10;
        }

        private int GetMaximumFactor(double lightspeed)
        {
            var maxFactor = 0;

            for (var i = 0; i < _engineThrottle.Length; i++)
            {
                if (_engineThrottle[i] > lightspeed)
                    return maxFactor;
                maxFactor = i;
            }
            return maxFactor;
        }

        private void InitiateWarp()
        {
            Debug.Log("[KSPI]: InitiateWarp started");

            // ensure we cannot go faster than the local allowed speed
            if (_maximumWarpSpeedFactor < selected_factor)
                selected_factor = _minimumPowerAllowedFactor;

            // prevent vessel to start faster than speed of light
            if (selected_factor > _minimumSelectedFactor)
                selected_factor = _minimumSelectedFactor;

            var selectedWarpSpeed = _engineThrottle[selected_factor];

            // verify if we are not warping into main body
            var cosineAngleToMainBody = Vector3d.Dot(part.transform.up.normalized, (vessel.CoMD - vessel.mainBody.position).normalized);

            var headingModifier = Math.Abs(Math.Min(0, cosineAngleToMainBody));

            allowedWarpDistancePerFrame = PluginSettings.Config.SpeedOfLight * TimeWarp.fixedDeltaTime * selectedWarpSpeed * headingModifier;
            safetyDistance = spaceSafetyDistance * 1000 * headingModifier;

            if (vessel.altitude < ((vessel.mainBody.atmosphere ? vessel.mainBody.atmosphereDepth : 20000) + allowedWarpDistancePerFrame + safetyDistance))
            {
                var message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_msg1", vessel.mainBody.name);//"Warp initiation aborted, cannot warp into " +
                Debug.Log("[KSPI]: " + message);
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                _initiateWarpTimeout = 0;
                return;
            }

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(selectedWarpSpeed);

            var powerReturned = CheatOptions.InfiniteElectricity
                ? currentPowerRequirementForWarp
                : ConsumeFnResourcePerSecond(currentPowerRequirementForWarp, ResourceSettings.Config.ElectricPowerInMegawatt);

            if (powerReturned < 0.99 * currentPowerRequirementForWarp)
            {
                _initiateWarpTimeout--;

                if (_initiateWarpTimeout == 1)
                {
                    while (selected_factor != _minimumSelectedFactor)
                    {
                        Debug.Log("[KSPI]: call ReduceWarpPower");
                        ReduceWarpPower();
                        selectedWarpSpeed = _engineThrottle[selected_factor];
                        currentPowerRequirementForWarp = GetPowerRequirementForWarp(selectedWarpSpeed);
                        if (powerReturned >= currentPowerRequirementForWarp)
                            return;
                    }
                }
                if (_initiateWarpTimeout == 0)
                {
                    var message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_msg2") + powerReturned + " " + currentPowerRequirementForWarp;//"Not enough power to initiate warp!"
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    IsCharging = true;
                    return;
                }
            }

            _initiateWarpTimeout = 0; // stop initiating to warp

            _vesselWasInOuterSpace = false;

            // consume all exotic matter to create warp field
            part.RequestResource(ResourceSettings.Config.ExoticMatter, exotic_power_required);

            _warpSound.Play();
            _warpSound.loop = true;

            // prevent g-force effects for current and next frame
            PluginHelper.IgnoreGForces(part, 2);

            _warpInitialMainBody = vessel.mainBody;
            _departureVelocity = vessel.orbit.GetFrameVel();

            _activePartHeading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);

            _headingAct = _activePartHeading * PluginSettings.Config.SpeedOfLight * selectedWarpSpeed;
            serialisedwarpvector = ConfigNode.WriteVector(_headingAct);

            if (!vessel.packed)
                vessel.GoOnRails();

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + _headingAct, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            if (!vessel.packed)
                vessel.GoOffRails();

            IsEnabled = true;
            _activeTrail = true;

            UpdateAllWarpDriveEffects();

            existing_warp_speed = selectedWarpSpeed;
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_deactivateWarpDrive")]
        public void DeactivateWarpDriveAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: Deactivate Warp Drive button pressed");
            StopWarpSoonAsPossible();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_deactivateWarpDrive", active = false)]
        public void DeactivateWarpDrive()
        {
            Debug.Log("[KSPI]: Deactivate Warp Drive action activated");
            StopWarpSoonAsPossible();
        }

        public void DeactivateWarpDrive(bool controlled)
        {
            Debug.Log("[KSPI]: Deactivate Warp Drive event called");

            _stopWarpSoonAsPossible = false;

            if (!IsEnabled)
            {
                Debug.Log("[KSPI]: canceled, Warp Drive is already inactive");
                return;
            }

            // update list of warp drives
            _alcubierreDrives = part.vessel.FindPartModulesImplementing<AlcubierreDrive>();

            // disable visual effect current drive
            IsEnabled = false;
            UpdateEffect(IsEnabled, IsCharging, IsEnabled && _warpTrailTimeout == 0, selected_factor);

            // update visuals all warp drives
            _alcubierreDrives.ForEach(m => m.IsEnabled = false);
            UpdateAllWarpDriveEffects();

            // Disable sound
            _warpSound.Stop();

            Vector3d reverseWarpHeading =  new Vector3d(-_headingAct.x, -_headingAct.y, -_headingAct.z);

            // prevent g-force effects for current and next frame
            PluginHelper.IgnoreGForces(part, 2);

            // puts the ship back into a simulated orbit and re-enables physics
            if (!vessel.packed)
                vessel.GoOnRails();

            if (controlled && matchExitToDestinationSpeed && _departureVelocity != Vector3d.zero)
            {
                Debug.Log("[KSPI]: vessel departure velocity " + _departureVelocity.x + " " + _departureVelocity.y + " " + _departureVelocity.z);
                Vector3d reverseInitialDepartureVelocity = new Vector3d(-_departureVelocity.x, -_departureVelocity.y, -_departureVelocity.z);

                // remove vessel departure speed in world
                reverseWarpHeading += reverseInitialDepartureVelocity;

                // add celestial body orbit speed to match speed in world
                if (vessel.mainBody.orbit != null)
                    reverseWarpHeading += vessel.mainBody.orbit.GetFrameVel();
            }

            var universalTime = Planetarium.GetUniversalTime();

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + reverseWarpHeading, vessel.orbit.referenceBody, universalTime);

            // disables physics and puts the ship into a propagated orbit , is this still needed?
            if (!vessel.packed)
                vessel.GoOffRails();

            if (controlled && matchExitToDestinationSpeed && vessel.atmDensity == 0)
            {
                Vector3d circularizationVector;

                var vesselTarget = FlightGlobals.fetch.VesselTarget;
                if (vesselTarget != null)
                {
                    var reverseExitVelocityVector = new Vector3d(-vessel.orbit.vel.x, -vessel.orbit.vel.y, -vessel.orbit.vel.z);
                    vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + reverseExitVelocityVector, vessel.orbit.referenceBody, universalTime);
                    var targetOrbitVector = vesselTarget.GetOrbit().getOrbitalVelocityAtUT(universalTime);
                    circularizationVector = new Vector3d(targetOrbitVector.x, targetOrbitVector.y, targetOrbitVector.z) + reverseExitVelocityVector;
                }
                else
                {
                    var timeAtApoapsis = vessel.orbit.timeToAp < vessel.orbit.period / 2
                        ? vessel.orbit.timeToAp + universalTime
                        : universalTime - (vessel.orbit.period - vessel.orbit.timeToAp);

                    var reverseExitVelocityVector = new Vector3d(-vessel.orbit.vel.x, -vessel.orbit.vel.y, -vessel.orbit.vel.z);
                    var velocityVectorAtApoapsis = vessel.orbit.getOrbitalVelocityAtUT(timeAtApoapsis);
                    var circularOrbitSpeed = CircularOrbitSpeed(vessel.mainBody, vessel.orbit.altitude + vessel.mainBody.Radius);
                    var horizontalVelocityVectorAtApoapsis = new Vector3d(velocityVectorAtApoapsis.x, velocityVectorAtApoapsis.y, 0);
                    circularizationVector = horizontalVelocityVectorAtApoapsis.normalized * (circularOrbitSpeed - horizontalVelocityVectorAtApoapsis.magnitude) + reverseExitVelocityVector;
                }

                vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + circularizationVector, vessel.orbit.referenceBody, universalTime);
            }

            if (_warpInitialMainBody == null || vessel.mainBody == _warpInitialMainBody) return;

            if (KopernicusHelper.IsStar(part.vessel.mainBody)) return;

            if (!matchExitToDestinationSpeed && vessel.mainBody != _warpInitialMainBody)
               Develocitize();
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_increaseWarpSpeed")]
        public void ToggleWarpSpeedUpAction(KSPActionParam param)
        {
            ToggleWarpSpeedUp();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_increaseWarpSpeed", active = true)]
        public void ToggleWarpSpeedUp()
        {
            Debug.Log("[KSPI]: Warp Throttle (+) button pressed");
            selected_factor++;
            if (selected_factor >= _engineThrottle.Length)
                selected_factor = _engineThrottle.Length - 1;
            target_factor = selected_factor;
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_increaseWarpSpeed3")]
        public void ToggleWarpSpeedUp3Action(KSPActionParam param)
        {
            ToggleWarpSpeedUp3();
        }

        private void ToggleWarpSpeedUp3()
        {
            Debug.Log("[KSPI]: 3x Speed Up pressed");

            for (var i = 0; i < 3; i++)
            {
                selected_factor++;

                if (selected_factor < _engineThrottle.Length) continue;

                selected_factor = _engineThrottle.Length - 1;
                break;
            }
            target_factor = selected_factor;
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_increaseWarpSpeed10")]
        public void ToggleWarpSpeedUp10Action(KSPActionParam param)
        {
            ToggleWarpSpeedUp10();
        }

        private void ToggleWarpSpeedUp10()
        {
            Debug.Log("[KSPI]: 10x Speed Up pressed");

            for (var i = 0; i < 10; i++)
            {
                selected_factor++;

                if (selected_factor < _engineThrottle.Length) continue;

                selected_factor = _engineThrottle.Length - 1;
                break;
            }
            target_factor = selected_factor;
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_decreaseWarpSpeed")]
        public void ToggleWarpSpeedDownAction(KSPActionParam param)
        {
            ToggleWarpSpeedDown();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveUnfocused = true, guiName = "#LOC_KSPIE_AlcubierreDrive_decreaseWarpSpeed", active = true)]
        public void ToggleWarpSpeedDown()
        {
            Debug.Log("[KSPI]: Warp Throttle (-) button pressed");
            selected_factor--;
            if (selected_factor < 0)
                selected_factor = 0;
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_decreaseWarpSpeed3")]
        public void ToggleWarpSpeedDown3Action(KSPActionParam param)
        {
            ToggleWarpSpeedDown3();
        }

        private void ToggleWarpSpeedDown3()
        {
            Debug.Log("[KSPI]: 3x Speed Down pressed");

            for (var i = 0; i < 3; i++)
            {
                selected_factor--;

                if (selected_factor >= 0) continue;

                selected_factor = 0;
                break;
            }
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_decreaseWarpSpeed10")]
        public void ToggleWarpSpeedDown10Action(KSPActionParam param)
        {
            ToggleWarpSpeedDown10();
        }

        private void ToggleWarpSpeedDown10()
        {
            Debug.Log("[KSPI]: 10x Speed Down pressed");

            for (var i = 0; i  < 10; i++)
            {
                selected_factor--;

                if (selected_factor >= 0) continue;

                selected_factor = 0;
                break;
            }
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_ReduceWarpPower")]
        public void ReduceWarpPowerAction(KSPActionParam param)
        {
            ReduceWarpPower();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_ReduceWarpPower", active = true)]//Reduce Warp Power
        public void ReduceWarpPower()
        {
            Debug.Log("[KSPI]: Reduce Warp Power button pressed");
            if (selected_factor == _minimumSelectedFactor) return;

            if (selected_factor < _minimumSelectedFactor)
                ToggleWarpSpeedUp();
            else if (selected_factor > _minimumSelectedFactor)
                ToggleWarpSpeedDown();
        }

        [KSPAction("Increase Exotic Matter Percentage")]
        public void IncreaseAntiGravityAction(KSPActionParam param)
        {
            if (antigravityPercentage < 200)
                antigravityPercentage += 5;
        }

        [KSPAction("Decrease Exotic Matter Percentage")]
        public void DecreaseAntiGravityAction(KSPActionParam param)
        {
            if (antigravityPercentage > 0)
                antigravityPercentage -= 5;
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_startChargingDrive")]
        public void StartChargingAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: Start Charging Action activated");
            StartCharging();
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_stopChargingDrive")]
        public void StopChargingAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: Stop Charging Action activated");
            StopCharging();
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_toggleChargingDrive")]
        public void ToggleChargingAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: Toggle Charging Action activated");
            if (IsCharging)
                StopCharging();
            else
                StartCharging();
        }

        [KSPAction("#LOC_KSPIE_AlcubierreDrive_reducePowerConsumption")]
        public void ReduceWarpDriveAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: ReduceWarpPower action activated");
            ReduceWarpPower();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_AlcubierreDrive_retrofit", active = true)]
        public void RetrofitDrive()
        {
            Debug.Log("[KSPI]: Retrofit button pressed");
            if (ResearchAndDevelopment.Instance == null) return;

            if (isupgraded || ResearchAndDevelopment.Instance.Science < UpgradeCost()) return;

            isupgraded = true;
            warpdriveType = upgradedName;

            ResearchAndDevelopment.Instance.AddScience(-UpgradeCost(), TransactionReasons.RnDPartPurchase);
        }

        private static float UpgradeCost()
        {
            return 0;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.Editor)
                vesselTotalMass = vessel.GetTotalMass();

            windowPosition = new Rect(_windowPositionX, _windowPositionY, 260, 100);
            _windowId = new System.Random(part.GetInstanceID()).Next(int.MaxValue);

            _moduleReactionWheel = part.FindModuleImplementing<ModuleReactionWheel>();

            _exoticResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.ExoticMatter);

            InstanceID = GetInstanceID();

            if (IsSlave)
                Debug.Log("[KSPI] - AlcubierreDrive Slave " + InstanceID + " Started");
            else
                Debug.Log("[KSPI] - AlcubierreDrive Master " + InstanceID + " Started");

            if (!IsNullOrEmpty(AnimationName))
                animationState = PluginHelper.SetUpAnimation(AnimationName, part);

            _resourceBuffers = new ResourceBuffers();
            _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, 2.0e+5, true));
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            _resourceBuffers.Init(part);

            try
            {
                Events[nameof(StartCharging)].active = !IsSlave;
                Events[nameof(StopCharging)].active = !IsSlave;
                Events[nameof(ActivateWarpDrive)].active = !IsSlave;
                Events[nameof(DeactivateWarpDrive)].active = !IsSlave;
                Events[nameof(ToggleWarpSpeedUp)].active = !IsSlave;
                Events[nameof(ToggleWarpSpeedDown)].active = !IsSlave;
                Events[nameof(ReduceWarpPower)].active = !IsSlave;
                Fields[nameof(showWindow)].guiActive = !IsSlave;
                Fields[nameof(matchExitToDestinationSpeed)].guiActive = !IsSlave;
                Fields[nameof(maximizeWarpSpeed)].guiActive = !IsSlave;
                Fields[nameof(holdAltitude)].guiActive = !IsSlave;
                Fields[nameof(spaceSafetyDistance)].guiActive = !IsSlave;
                Fields[nameof(maximumAllowedWarpThrotle)].guiActive = !IsSlave;
                Fields[nameof(warpToMassRatio)].guiActive = !IsSlave;
                Fields[nameof(minPowerRequirementForLightSpeed)].guiActive = !IsSlave;
                Fields[nameof(currentPowerRequirementForWarp)].guiActive = !IsSlave;
                Fields[nameof(totalWarpPower)].guiActive = !IsSlave;
                Fields[nameof(powerRequirementForMaximumAllowedLightSpeed)].guiActive = !IsSlave;
                Fields[nameof(antigravityAcceleration)].guiActive = !IsSlave;

                var holdAltitudeField = Fields[nameof(holdAltitude)];
                if (holdAltitudeField != null)
                {
                    if (holdAltitudeField.uiControlFlight is UI_Toggle holdAltitudeToggle)
                        holdAltitudeToggle.onFieldChanged += HoldAltitudeChanged;
                }

                antigravityField = Fields[nameof(antigravityPercentage)];
                if (antigravityField != null)
                {
                    antigravityField.guiActive = !IsSlave;
                    if (antigravityField.uiControlFlight is UI_FloatRange antigravityFloatRange)
                    {
                        antigravityFloatRange.controlEnabled = !IsSlave;
                        antigravityFloatRange.onFieldChanged += AntigravityFloatChanged;
                    }
                }

                _minimumSelectedFactor = _engineThrottle.ToList().IndexOf(_engineThrottle.First(w => Math.Abs(w - 1) < float.Epsilon));
                if (selected_factor == -1)
                    selected_factor = _minimumSelectedFactor;

                _hasRequiredUpgrade = PluginHelper.UpgradeAvailable(upgradeTechReq);
                if (_hasRequiredUpgrade)
                    isupgraded = true;

                warpdriveType = isupgraded ? upgradedName : originalName;

                if (state == StartState.Editor) return;

                // if all drives are slaves, convert the current into a master
                _alcubierreDrives = part.vessel.FindPartModulesImplementing<AlcubierreDrive>();
                if (_alcubierreDrives.All(m => m.IsSlave))
                {
                    Debug.Log("[KSPI]: All drives were in slave mode, converting drive " + InstanceID + " into master");
                    IsSlave = false;
                }

                // if current is not a slave, turn all other drives into slaves
                if (!IsSlave)
                {
                    foreach (var drive in _alcubierreDrives)
                    {
                        var driveId = drive.GetInstanceID();

                        if (driveId == InstanceID) continue;

                        Debug.Log("[KSPI]: Created AlcubierreDrive Slave for drive " + driveId);
                        drive.IsSlave = true;
                    }
                }

                Debug.Log("[KSPI]: AlcubierreDrive on " + part.name + " was Force Activated");
                this.part.force_activate();

                if (serialisedwarpvector != null)
                    _headingAct = ConfigNode.ParseVector3D(serialisedwarpvector);

                _warpEffect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _warpEffect2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

                _warpEffect1Renderer = _warpEffect.GetComponent<Renderer>();
                _warpEffect2Renderer = _warpEffect2.GetComponent<Renderer>();

                _warpEffect1Collider = _warpEffect.GetComponent<Collider>();
                _warpEffect2Collider = _warpEffect2.GetComponent<Collider>();

                _warpEffect1Collider.enabled = false;
                _warpEffect2Collider.enabled = false;

                var shipPos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
                var endBeamPos = shipPos + transform.up * warpSize;
                var midPos = (shipPos - endBeamPos) / 2.0f;

                _warpEffect.transform.localScale = new Vector3(effectSize1, midPos.magnitude, effectSize1);
                _warpEffect.transform.position = new Vector3(midPos.x, shipPos.y + midPos.y, midPos.z);
                _warpEffect.transform.rotation = part.transform.rotation;

                _warpEffect2.transform.localScale = new Vector3(effectSize2, midPos.magnitude, effectSize2);
                _warpEffect2.transform.position = new Vector3(midPos.x, shipPos.y + midPos.y, midPos.z);
                _warpEffect2.transform.rotation = part.transform.rotation;

                //warp_effect.layer = LayerMask.NameToLayer("Ignore Raycast");
                //warp_effect.renderer.material = new Material(KSP.IO.File.ReadAllText<AlcubierreDrive>("AlphaSelfIllum.shader"));

                _warpEffect1Renderer.material.shader = Shader.Find(warpEffect1ShaderPath);
                _warpEffect2Renderer.material.shader = Shader.Find(warpEffect2ShaderPath);

                //warpWhiteFlash = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warp10", false);
                //warpRedFlash = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warpr10", false);

                _warpTextures = new Texture[32];

                const string warpTexturePath = "WarpPlugin/ParticleFX/warp";
                for (var i = 0; i < 16; i++)
                {
                    _warpTextures[i] = GameDatabase.Instance.GetTexture(i > 0
                        ? warpTexturePath + (i + 1)
                        : warpTexturePath, false);
                }

                //warp_textures[11] = warpWhiteFlash;
                for (var i = 16; i < 32; i++)
                {
                    var j = 31 - i;
                    _warpTextures[i] = GameDatabase.Instance.GetTexture(j > 0
                      ? warpTexturePath + (j + 1)
                      : warpTexturePath, false);
                }

                _warpTextures2 = new Texture[32];

                const string warprTexturePath = "WarpPlugin/ParticleFX/warpr";
                for (var i = 0; i < 16; i++)
                {
                    _warpTextures2[i] = GameDatabase.Instance.GetTexture(i > 0
                        ? warprTexturePath + (i + 1)
                        : warprTexturePath, false);
                }

                //warp_textures2[11] = warpRedFlash;
                for (var i = 16; i < 32; i++)
                {
                    var j = 31 - i;
                    _warpTextures2[i] = GameDatabase.Instance.GetTexture(j > 0
                        ? warprTexturePath + (j + 1)
                        : warprTexturePath, false);
                }

                _warpEffect1Renderer.material.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);
                _warpEffect2Renderer.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, 0.1f);

                _warpEffect1Renderer.material.mainTexture = _warpTextures[0];
                _warpEffect1Renderer.receiveShadows = false;
                //warp_effect.layer = LayerMask.NameToLayer ("Ignore Raycast");
                //warp_effect.collider.isTrigger = true;
                _warpEffect2Renderer.material.mainTexture = _warpTextures2[0];
                _warpEffect2Renderer.receiveShadows = false;
                _warpEffect2Renderer.material.mainTextureOffset = new Vector2(-0.2f, -0.2f);
                //warp_effect2.layer = LayerMask.NameToLayer ("Ignore Raycast");
                //warp_effect2.collider.isTrigger = true;
                _warpEffect2Renderer.material.renderQueue = 1000;
                _warpEffect1Renderer.material.renderQueue = 1001;

                _warpEffect2Renderer.enabled = false;
                _warpEffect1Renderer.enabled = false;

                /*gameObject.AddComponent<Light>();
                gameObject.light.color = Color.cyan;
                gameObject.light.intensity = 1f;
                gameObject.light.range = 4000f;
                gameObject.light.type = LightType.Spot;
                gameObject.light.transform.position = end_beam_pos;
                gameObject.light.cullingMask = ~0;*/

                //warp_effect.transform.localScale.y = 2.5f;
                //warp_effect.transform.localScale.z = 200f;

                _warpSound = gameObject.AddComponent<AudioSource>();
                _warpSound.clip = GameDatabase.Instance.GetAudioClip(warpSoundPath);
                _warpSound.volume = GameSettings.SHIP_VOLUME;

                //warp_sound.panLevel = 0;
                _warpSound.panStereo = 0;
                _warpSound.rolloffMode = AudioRolloffMode.Linear;
                _warpSound.Stop();

                if (IsEnabled)
                {
                    _warpSound.Play();
                    _warpSound.loop = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: AlcubierreDrive OnStart 1 Exception " + e.Message);
            }

            warpdriveType = originalName;

            // update visuals all warp drives
            UpdateAllWarpDriveEffects();
        }

        private void AntigravityFloatChanged(BaseField field, object oldFieldValueObj)
        {
            holdAltitude = false;
        }

        private void HoldAltitudeChanged(BaseField field, object oldFieldValueObj)
        {
            antigravityPercentage = (float)((decimal)Math.Round(antigravityPercentage / 5) * 5);
        }

        public void VesselChangedSoi()
        {
            if (IsSlave)
                return;

            Debug.Log("[KSPI]: AlcubierreDrive Vessel Changed SOI");
        }

        public void Update()
        {
            partMass = part.mass;
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            var parts = EditorLogic.fetch.ship.parts;

            var warpDriveList = parts.Select(currentPart => currentPart
                .FindModuleImplementing<AlcubierreDrive>())
                .Where(alcubierreDrive => alcubierreDrive != null).ToList();

            var vesselDryMass = parts.Sum(m => m.mass);
            var vesselWetMass = parts.Sum(m => m.Resources.Sum(r => r.amount * r.info.density));
            vesselTotalMass = vesselDryMass + vesselWetMass;

            totalWarpPower = warpDriveList.Sum(w => w.warpStrength * WarpPower);
            warpToMassRatio = vesselTotalMass > 0 ? totalWarpPower / vesselTotalMass : 0;
        }

        public override void OnUpdate()
        {
            Events[nameof(StartCharging)].active = !IsSlave && !IsCharging;
            Events[nameof(StopCharging)].active = !IsSlave && IsCharging;
            Events[nameof(DeactivateWarpDrive)].active = !IsSlave && IsEnabled;
            Fields[nameof(driveStatus)].guiActive = !IsSlave && IsCharging;

            if (_moduleReactionWheel != null)
            {
                _moduleReactionWheel.Fields[nameof(_moduleReactionWheel.authorityLimiter)].guiActive = false;
                _moduleReactionWheel.Fields[nameof(_moduleReactionWheel.actuatorModeCycle)].guiActive = false;
                _moduleReactionWheel.Fields[nameof(_moduleReactionWheel.stateString)].guiActive = false;
                _moduleReactionWheel.Events[nameof(_moduleReactionWheel.OnToggle)].guiActive = false;

                var warpPower = WarpPower;

                _moduleReactionWheel.PitchTorque = _activeTrail ? 5 * part.mass * warpPower : 0;
                _moduleReactionWheel.YawTorque = _activeTrail ? 5 * part.mass * warpPower : 0;
                _moduleReactionWheel.RollTorque = _activeTrail ? 5 * part.mass * warpPower : 0;
            }

            if (ResearchAndDevelopment.Instance != null)
                Events[nameof(RetrofitDrive)].active = !IsSlave && !isupgraded && ResearchAndDevelopment.Instance.Science >= UpgradeCost() && _hasRequiredUpgrade;
            else
                Events[nameof(RetrofitDrive)].active = false;

            if (IsSlave) return;

            UpdateAllWarpDriveEffects();
        }

        private void UpdateAllWarpDriveEffects()
        {
            foreach (var drive in _alcubierreDrives)
            {
                drive.UpdateEffect(IsEnabled, IsCharging, IsEnabled && _warpTrailTimeout == 0, selected_factor);
            }
        }

        private void UpdateEffect(bool effectEnabled, bool isCharging, bool activeTrail, int selectedFactor)
        {
            this._activeTrail = activeTrail;
            this.selected_factor = selectedFactor;

            if (animationState == null) return;

            foreach (var anim in animationState)
            {
                if ((effectEnabled || isCharging) && anim.normalizedTime < 1)
                    anim.speed = 1;

                if ((effectEnabled || isCharging) && anim.normalizedTime >= 1)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 1;
                }

                if (!effectEnabled && !isCharging && anim.normalizedTime > 0)
                    anim.speed = -1;

                if (effectEnabled || isCharging || !(anim.normalizedTime <= 0))
                    continue;

                anim.speed = 0;
                anim.normalizedTime = 0;
            }
        }

        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            if (!IsSlave)
                PluginHelper.UpdateIgnoredGForces();

            if (vessel == null)
                return;

            warpEngineThrottle = _engineThrottle[selected_factor];

            distanceToClosestBody = DistanceToClosestBody(vessel, out _closestCelestialBody, out _selectedTargetVesselIsClosest);

            closestCelestrialBodyName = _closestCelestialBody.name;

            gravityPull = FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude;
            gravityAtSeaLevel = _closestCelestialBody.GeeASL * PhysicsGlobals.GravitationalAcceleration;
            gravityRatio = gravityAtSeaLevel > 0 ? Math.Min(1, gravityPull / gravityAtSeaLevel) : 0;
            gravityDragRatio = Math.Pow(Math.Min(1, 1 - gravityRatio), Math.Max(1, Math.Sqrt(gravityAtSeaLevel)));
            gravityDragPercentage = (1 - gravityDragRatio) * 100;

            maximumWarpForGravityPull = gravityPull > 0 ? 1 / gravityPull : 0;

            cosineAngleToClosestBody = Vector3d.Dot(part.transform.up.normalized, (vessel.CoMD - _closestCelestialBody.position).normalized);

            var cosineAngleModifier = _selectedTargetVesselIsClosest ? 0.25 : (1 + 0.5 * cosineAngleToClosestBody);

            maximumWarpForAltitude = 0.1 * cosineAngleModifier * distanceToClosestBody / PluginSettings.Config.SpeedOfLight / TimeWarp.fixedDeltaTime;
            maximumWarpWeighted = (gravityRatio * maximumWarpForGravityPull) + ((1 - gravityRatio) * maximumWarpForAltitude);
            maximumWarpSpeed = Math.Min(maximumWarpWeighted, maximumWarpForAltitude);
            _maximumWarpSpeedFactor = GetMaximumFactor(maximumWarpSpeed);
            maximumAllowedWarpThrotle = _engineThrottle[_maximumWarpSpeedFactor];
            _minimumPowerAllowedFactor = _maximumWarpSpeedFactor > _minimumSelectedFactor ? _maximumWarpSpeedFactor : _minimumSelectedFactor;

            if (_alcubierreDrives != null)
                totalWarpPower = _alcubierreDrives.Sum(p => p.warpStrength * WarpPower);

            vesselTotalMass = vessel.GetTotalMass();

            if (_alcubierreDrives != null && totalWarpPower != 0 && vesselTotalMass != 0)
            {
                warpToMassRatio = totalWarpPower / vesselTotalMass;
                exotic_power_required = (GameConstants.initial_alcubierre_megajoules_required * vesselTotalMass * powerRequirementMultiplier) / warpToMassRatio;

                // spread exotic matter over all coils
                var exoticMatterResource = part.Resources[ResourceSettings.Config.ExoticMatter];
                exoticMatterResource.maxAmount = exotic_power_required / _alcubierreDrives.Count;
            }

            minPowerRequirementForLightSpeed = GetPowerRequirementForWarp(1);
            powerRequirementForSlowedSubLightSpeed = GetPowerRequirementForWarp(_engineThrottle.First());
            powerRequirementForMaximumAllowedLightSpeed = GetPowerRequirementForWarp(maximumAllowedWarpThrotle);
            currentPowerRequirementForWarp = GetPowerRequirementForWarp(_engineThrottle[selected_factor]);

            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);
            _resourceBuffers.UpdateBuffers();
        }


        private double GetHighestThrottleForAvailablePower()
        {
            foreach (var lightspeedFraction in _engineThrottle.Where(s => s <= maximumAllowedWarpThrotle).Reverse())
            {
                var requiredPower = GetPowerRequirementForWarp(lightspeedFraction);

                if (availablePower > requiredPower)
                    return lightspeedFraction;
            }

            return 1;
        }

        private void MaximizeWarpSpeed()
        {
            var fastestLightspeed = GetHighestThrottleForAvailablePower();

            if (fastestLightspeed > warpEngineThrottle)
            {
                selected_factor = _engineThrottle.IndexOf(fastestLightspeed);
                currentPowerRequirementForWarp = GetPowerRequirementForWarp(fastestLightspeed);
                warpEngineThrottle = fastestLightspeed;
            }
        }

        private double GetLowestThrottleForAvailablePower()
        {
            foreach (var lightspeedFraction in _engineThrottle.Where(s => s < 1))
            {
                var requiredPower = GetPowerRequirementForWarp(lightspeedFraction);

                if (availablePower > requiredPower)
                    return lightspeedFraction;
            }

            return 1;
        }

        private void MinimizeWarpSpeed()
        {
            var slowestSublightSpeed = GetLowestThrottleForAvailablePower();

            if (!(slowestSublightSpeed < warpEngineThrottle))
                return;

            selected_factor = _engineThrottle.IndexOf(slowestSublightSpeed);
            currentPowerRequirementForWarp = GetPowerRequirementForWarp(slowestSublightSpeed);
            warpEngineThrottle = slowestSublightSpeed;
        }

        public override void OnFixedUpdate()
        {
            distanceToClosestBody = DistanceToClosestBody(vessel, out _closestCelestialBody, out _selectedTargetVesselIsClosest);

            if (_initiateWarpTimeout > 0)
                InitiateWarp();

            Orbit currentOrbit = vessel.orbitDriver.orbit;
            double universalTime = Planetarium.GetUniversalTime();

            if (IsEnabled)
            {
                _counterCurrent++;

                // disable any geeforce effects during warp
                PluginHelper.IgnoreGForces(part, 2);
                var reverseHeadingWarp = new Vector3d(-_headingAct.x, -_headingAct.y, -_headingAct.z);
                Vector3d currentOrbitalVelocity = vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(universalTime);
                Vector3d newDirection = currentOrbitalVelocity + reverseHeadingWarp;

                Orbit predictedExitOrbit;

                long multiplier = 0;
                do
                {
                    // first predict dropping out of warp
                    predictedExitOrbit = new Orbit(currentOrbit);
                    predictedExitOrbit.UpdateFromStateVectors(currentOrbit.pos, newDirection, vessel.orbit.referenceBody, universalTime);

                    // then calculated predicted gravity breaking
                    if (_warpInitialMainBody != null && vessel.mainBody != _warpInitialMainBody && !KopernicusHelper.IsStar(part.vessel.mainBody))
                    {
                        Vector3d retrogradeNormalizedVelocity = newDirection.normalized * -multiplier;
                        Vector3d velocityToCancel = predictedExitOrbit.getOrbitalVelocityAtUT(universalTime) * gravityDragRatio;

                        predictedExitOrbit.UpdateFromStateVectors(currentOrbit.pos, retrogradeNormalizedVelocity - velocityToCancel, currentOrbit.referenceBody, universalTime);
                    }

                    multiplier += 1;
                } while (multiplier < 10000 && double.IsNaN(predictedExitOrbit.getOrbitalVelocityAtUT(universalTime).magnitude));

                // update expected exit orbit data
                exitPeriapsis = predictedExitOrbit.PeA * 0.001;
                exitApoapsis = predictedExitOrbit.ApA * 0.001;
                exitSpeed = predictedExitOrbit.getOrbitalVelocityAtUT(universalTime).magnitude;
                exitEccentricity = predictedExitOrbit.eccentricity * 180 / Math.PI;
                exitMeanAnomaly = predictedExitOrbit.meanAnomaly;
                exitBurnCircularize = DeltaVToCircularize(predictedExitOrbit);
            }
            else
            {
                exitPeriapsis = currentOrbit.PeA * 0.001;
                exitApoapsis = currentOrbit.ApA * 0.001;
                exitSpeed = currentOrbit.getOrbitalVelocityAtUT(universalTime).magnitude;
                exitEccentricity = currentOrbit.eccentricity;
                exitMeanAnomaly = currentOrbit.meanAnomaly * 180 / Math.PI;
                exitBurnCircularize = DeltaVToCircularize(currentOrbit);
            }

            if (!IsSlave)
            {
                if (_stopWarpSoonAsPossible)
                {
                    if (warpEngineThrottle >= 100)
                    {
                        selected_factor = _minimumSelectedFactor;
                    }
                    else
                    {
                        DeactivateWarpDrive(true);
                    }
                }
            }

            warpEngineThrottle = _engineThrottle[selected_factor];

            _warpSound.pitch = (float)Math.Pow(warpEngineThrottle, warpSoundPitchExp);
            _warpSound.volume = GameSettings.SHIP_VOLUME;

            _texCount += warpEngineThrottle;

            if (!IsSlave)
            {
                WarpDriveCharging();

                if (_startWarpAsSoonAsPossible)
                {
                    if (antigravityPercentage >= 100)
                        ActivateWarpDrive(false);
                    else
                        _startWarpAsSoonAsPossible = false;
                }

                UpdateWarpSpeed();
            }

            // update animation
            if (IsEnabled)
            {
                driveStatus = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_Active");//"Active."
            }
            else
            {
                if (!IsSlave)
                {
                    part.GetConnectedResourceTotals(_exoticResourceDefinition.id, out _currentExoticMatter, out _maxExoticMatter);

                    if (_currentExoticMatter < exotic_power_required * 0.999 * 0.5)
                    {
                        var electricalCurrentPct = Math.Min(100, 100 * _currentExoticMatter/(exotic_power_required * 0.5));
                        driveStatus = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_Charging") + electricalCurrentPct.ToString("0.00") + "%";//String.Format("Charging: ")
                    }
                    else
                        driveStatus = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_Ready");//"Ready."
                }
            }

            if (_activeTrail && !hideTrail && _warpTrailTimeout == 0)
            {
                var shipPos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
                var endBeamPos = shipPos + part.transform.up * warpSize;
                var midPos = (shipPos - endBeamPos) / 2f;

                _warpEffect.transform.rotation = part.transform.rotation;
                _warpEffect.transform.localScale = new Vector3(effectSize1, midPos.magnitude, effectSize1);
                _warpEffect.transform.position = new Vector3(shipPos.x + midPos.x, shipPos.y + midPos.y, shipPos.z + midPos.z);
                _warpEffect.transform.rotation = part.transform.rotation;

                _warpEffect2.transform.rotation = part.transform.rotation;
                _warpEffect2.transform.localScale = new Vector3(effectSize2, midPos.magnitude, effectSize2);
                _warpEffect2.transform.position = new Vector3(shipPos.x + midPos.x, shipPos.y + midPos.y, shipPos.z + midPos.z);
                _warpEffect2.transform.rotation = part.transform.rotation;

                _warpEffect1Renderer.material.mainTexture = _warpTextures[((int)_texCount) % _warpTextures.Length];
                _warpEffect2Renderer.material.mainTexture = _warpTextures2[((int)_texCount + 8) % _warpTextures2.Length];

                _warpEffect2Renderer.enabled = true;
                _warpEffect1Renderer.enabled = true;
            }
            else
            {
                _warpEffect2Renderer.enabled = false;
                _warpEffect1Renderer.enabled = false;
            }

            if (_warpTrailTimeout > 0)
                _warpTrailTimeout--;
        }

        private static double DeltaVToCircularize(Orbit orbit)
        {
            var rAp = orbit.ApR;
            var rPe = orbit.PeR;
            var mu = orbit.referenceBody.gravParameter;

            return Math.Abs(Sqrt(mu / rAp) - Sqrt((rPe * mu) / (rAp * (rPe + rAp) / 2)));
        }

        private static double Sqrt(double value)
        {
            if (value < 0)
                return -Math.Sqrt(-1 * value);
            else
                return Math.Sqrt(value);
        }

        //Computes the deltaV of the burn needed to circularize an orbit at a given UT.
        public static double DeltaVToCircularize(Orbit o, double UT)
        {
            var desiredVelocity = CircularOrbitSpeed(o.referenceBody, o.radius) ;
            var actualVelocity = o.getOrbitalVelocityAtUT(UT).magnitude;
            return desiredVelocity - actualVelocity;
        }

        //Computes the speed of a circular orbit of a given radius for a given body.
        private static double CircularOrbitSpeed(CelestialBody body, double radius)
        {
            return Math.Sqrt(body.gravParameter / radius);
        }

        private void WarpDriveCharging()
        {
            part.GetConnectedResourceTotals(_exoticResourceDefinition.id, out _currentExoticMatter, out _maxExoticMatter);

            vesselTotalMass = vessel.GetTotalMass();
            if (totalWarpPower != 0 && vesselTotalMass != 0)
            {
                warpToMassRatio = totalWarpPower / vesselTotalMass;
                exotic_power_required = (GameConstants.initial_alcubierre_megajoules_required * vesselTotalMass * powerRequirementMultiplier) / warpToMassRatio;
                _exoticMatterRatio = exotic_power_required > 0 ? Math.Min(1, _currentExoticMatter / exotic_power_required) : 0;
            }
            else
            {
                exotic_power_required = 0;
                _exoticMatterRatio = 0;
            }

            GenerateAntiGravity();

            // maintenance power depend on vessel mass and experienced geeforce

            var maximumExoticMaintenancePower = vesselTotalMass * powerRequirementMultiplier * vessel.gravityForPos.magnitude * gravityMaintenancePowerMultiplier;
            var minimumExoticMaintenancePower = 0.5 * 0.5 * maximumExoticMaintenancePower;

            requiredExoticMaintenancePower = _exoticMatterRatio * _exoticMatterRatio * maximumExoticMaintenancePower;

            var overheatingRatio = GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);

            var overheatModifier = overheatingRatio < 0.9 ? 1 : (1 - overheatingRatio) * 10;

            _receivedExoticMaintenancePower = CheatOptions.InfiniteElectricity
                   ? requiredExoticMaintenancePower
                   : ConsumeFnResourcePerSecond(overheatModifier * requiredExoticMaintenancePower, ResourceSettings.Config.ElectricPowerInMegawatt);

            _minimumExoticMatterMaintenanceRatio = minimumExoticMaintenancePower > 0 ? _receivedExoticMaintenancePower / minimumExoticMaintenancePower : 0;

            _exoticMatterMaintenanceRatio = requiredExoticMaintenancePower > 0 ? _receivedExoticMaintenancePower / requiredExoticMaintenancePower : 0;

            ProduceWasteheat(_receivedExoticMaintenancePower);

            _exoticMatterProduced = (1 - _exoticMatterMaintenanceRatio) * -_maxExoticMatter;

            if ((IsCharging || antigravityPercentage > 0 || _exoticMatterRatio > 0 ) && !IsEnabled)
            {
                availablePower = CheatOptions.InfiniteElectricity
                    ? currentPowerRequirementForWarp
                    : _stablePowerSupply;

                var exoticMatterRequirement = antigravityPercentage * 0.005 * exotic_power_required;

                _currentRequiredExoticMatter = exoticMatterRequirement - _currentExoticMatter;

                currentPowerRequired = _currentRequiredExoticMatter * 1000 / TimeWarp.fixedDeltaTime * powerRequirementMultiplier;

                if (currentPowerRequired < 0)
                {
                    _exoticMatterProduced += currentPowerRequired;
                    chargePowerDraw = 0;
                }
                else
                {
                    _stablePowerSupply = GetAvailableStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt);

                    chargePowerDraw = CheatOptions.InfiniteElectricity
                        ? currentPowerRequired
                        : Math.Min(currentPowerRequired, Math.Max(minPowerRequirementForLightSpeed, _stablePowerSupply));

                    var resourceBarRatio = GetResourceBarRatio(ResourceSettings.Config.ElectricPowerInMegawatt);
                    var effectiveResourceThrottling = resourceBarRatio > 0.5 ? 1 : resourceBarRatio * 2;

                    _exoticMatterProduced = CheatOptions.InfiniteElectricity
                        ? chargePowerDraw
                        : ConsumeFnResourcePerSecond(overheatModifier * chargePowerDraw * effectiveResourceThrottling, ResourceSettings.Config.ElectricPowerInMegawatt);

                    if (!CheatOptions.InfinitePropellant && _stablePowerSupply < minPowerRequirementForLightSpeed)
                        _insufficientPowerTimeout--;
                    else
                        _insufficientPowerTimeout = maxPowerTimeout;

                    if (_insufficientPowerTimeout < 0)
                    {
                        _insufficientPowerTimeout--;

                        var message = overheatModifier < 0.99 ? Localizer.Format("#LOC_KSPIE_AlcubierreDrive_msg3") :  //"Shutdown Alcubierre Drive due to overheating"
                            Localizer.Format("#LOC_KSPIE_AlcubierreDrive_notEnoughElectricPowerForWarp");

                        Debug.Log("[KSPI]: " + message);
                        ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        StopCharging();

                        return;
                    }
                }

                ProduceWasteheat(_exoticMatterProduced);
            }

            var producedExoticMatter = Math.Min(_currentRequiredExoticMatter,  _exoticMatterProduced * 0.001 * TimeWarp.fixedDeltaTime / powerRequirementMultiplier);

            part.RequestResource(ResourceSettings.Config.ExoticMatter, -producedExoticMatter);
        }

        private void GenerateAntiGravity()
        {
            _exoticMatterRatio = exotic_power_required > 0 ? Math.Min(1, _currentExoticMatter / exotic_power_required) : 0;

            gravityAcceleration = vessel.gravityForPos.magnitude;

            var antigravityForceVector = vessel.gravityForPos * -_exoticMatterRatio * antigravityMultiplier;

            antigravityAcceleration = antigravityForceVector.magnitude;

            if (antigravityAcceleration > 0)
                TimeWarp.GThreshold = GThreshold;

            if (!double.IsNaN(antigravityForceVector.x) && !double.IsNaN(antigravityForceVector.y) && !double.IsNaN(antigravityForceVector.z))
            {
                if (vessel.packed)
                    vessel.orbit.Perturb(antigravityForceVector * TimeWarp.fixedDeltaTime, Planetarium.GetUniversalTime());
                else
                    vessel.ChangeWorldVelocity(antigravityForceVector * TimeWarp.fixedDeltaTime);
            }

            verticalSpeed = vessel.verticalSpeed;

            _stablePowerSupply = GetAvailableStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt);

            if (!holdAltitude) return;

            var orbitMultiplier = vessel.orbit.PeA > vessel.mainBody.atmosphereDepth ? 0 : 1 - Math.Min(1, vessel.horizontalSrfSpeed  /  CircularOrbitSpeed(vessel.mainBody, vessel.mainBody.Radius + vessel.altitude));
            var responseFactor = responseMultiplier * _stablePowerSupply / exotic_power_required;
            antigravityPercentage = (float)Math.Max(0, Math.Min(100 * orbitMultiplier + (gravityAcceleration != 0 ? responseFactor * -verticalSpeed / gravityAcceleration / TimeWarp.fixedDeltaTime : 0), 200));
        }

        private void ProduceWasteheat(double powerReturned)
        {
            if (!CheatOptions.IgnoreMaxTemperature)
                SupplyFnResourcePerSecond(powerReturned *
                    (isupgraded
                        ? wasteheatRatioUpgraded
                        : wasteheatRatio), ResourceSettings.Config.WasteHeatInMegawatt);
        }

        private double GetPowerRequirementForWarp(double lightspeedFraction)
        {
            var sqrtSpeed = Math.Sqrt(lightspeedFraction);

            var powerModifier = lightspeedFraction < 1
                ? 1 / sqrtSpeed
                : sqrtSpeed;

            return powerModifier * exotic_power_required * warpPowerReqMult;
        }

        private void UpdateWarpSpeed()
        {
            if (!IsEnabled || exotic_power_required <= 0) return;

            var selectedLightSpeed = _engineThrottle[selected_factor];

            currentPowerRequirementForWarp = GetPowerRequirementForWarp(selectedLightSpeed);

            availablePower = CheatOptions.InfiniteElectricity
                ? currentPowerRequirementForWarp
                : GetAvailableStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt);

            double powerReturned;

            if (CheatOptions.InfiniteElectricity)
                powerReturned = currentPowerRequirementForWarp;
            else
            {
                powerReturned = ConsumeFnResourcePerSecond(currentPowerRequirementForWarp, ResourceSettings.Config.ElectricPowerInMegawatt) ;
                ProduceWasteheat(powerReturned);
            }

            var headingModifier = FlightGlobals.fetch.VesselTarget == null ? Math.Abs(Math.Min(0, cosineAngleToClosestBody)) : 1;

            allowedWarpDistancePerFrame = PluginSettings.Config.SpeedOfLight * TimeWarp.fixedDeltaTime * selectedLightSpeed * headingModifier;

            safetyDistance = FlightGlobals.fetch.VesselTarget == null ? spaceSafetyDistance * 1000 : 0;

            var minimumSpaceAltitude = FlightGlobals.fetch.VesselTarget == null ? (_closestCelestialBody.atmosphere ? _closestCelestialBody.atmosphereDepth : 0) : 0;

            dropoutDistance = minimumSpaceAltitude + allowedWarpDistancePerFrame + safetyDistance;

            if ((!CheatOptions.IgnoreMaxTemperature && vessel.atmDensity > 0) ||   distanceToClosestBody <= dropoutDistance)
            {
                if (_vesselWasInOuterSpace)
                {
                    var message = FlightGlobals.fetch.VesselTarget == null
                        ? _closestCelestialBody.atmosphere
                            ? "#LOC_KSPIE_AlcubierreDrive_droppedOutOfWarpTooCloseToAtmosphere"
                            : "#LOC_KSPIE_AlcubierreDrive_droppedOutOfWarpTooCloseToSurface"
                        : "#LOC_KSPIE_AlcubierreDrive_msg4";//"Dropped out of warp near target"

                    Debug.Log("[KSPI]: " + Localizer.Format(message));
                    ScreenMessages.PostScreenMessage(Localizer.Format(message), 5);
                    DeactivateWarpDrive(true);
                    _vesselWasInOuterSpace = false;
                    return;
                }
            }
            else
                _vesselWasInOuterSpace = true;

            // detect power shortage
            if (currentPowerRequirementForWarp > availablePower)
                _insufficientPowerTimeout = -1;
            else if (powerReturned < 0.99 * currentPowerRequirementForWarp)
                _insufficientPowerTimeout--;
            else
                _insufficientPowerTimeout = 10;

            // retrieve vessel heading
            var newPartHeading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);

            // detect any changes in vessel heading and heading stability
            magnitudeDiff = (_activePartHeading - newPartHeading).magnitude;

            // determine if we need to change speed and heading
            var hasPowerShortage = _insufficientPowerTimeout < 0;
            var hasHeadingChanged = magnitudeDiff > 0.0001 && _counterCurrent > _counterPreviousChange + headingChangedTimeout && allowWarpTurning;

            // Speedup to maximum speed when possible and requested
            if (maximizeWarpSpeed && magnitudeDiff <= 0.0001 && _maximumWarpSpeedFactor > selected_factor)
            {
                MaximizeWarpSpeed();
            }

            var hasWarpFactorChanged = Math.Abs(existing_warp_speed - selectedLightSpeed) > float.Epsilon;
            var hasGravityPullImbalance = _maximumWarpSpeedFactor < selected_factor;

            if (hasGravityPullImbalance)
                selected_factor = _maximumWarpSpeedFactor;

            if (!CheatOptions.InfiniteElectricity && hasPowerShortage)
            {
                if (availablePower < minPowerRequirementForLightSpeed)
                {
                    var message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_msg5", availablePower.ToString("0"), minPowerRequirementForLightSpeed.ToString("0"));//"Maximum power supply of " +  + " MW is insufficient power, you need at at least " +  + " MW of Power to maintain Lightspeed with current vessel. Please increase power supply, lower vessel mass or increase Warp Drive mass."
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5);
                    DeactivateWarpDrive(false);
                    return;
                }

                if (selected_factor == _minimumPowerAllowedFactor || selected_factor == _minimumSelectedFactor ||
                    (selectedLightSpeed < 1 && warpEngineThrottle >= maximumAllowedWarpThrotle && powerReturned < 0.99 * currentPowerRequirementForWarp))
                {
                    string message;
                    if (powerReturned < 0.99 * currentPowerRequirementForWarp)
                        message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_criticalPowerSupplyAt") + " " + (powerReturned / currentPowerRequirementForWarp).ToString("P1") + " " + Localizer.Format("#LOC_KSPIE_AlcubierreDrive_deactivatingWarpDrive");
                    else
                        message = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_criticalPowerShortageWhileAtMinimumSpeed") + " " + Localizer.Format("#LOC_KSPIE_AlcubierreDrive_deactivatingWarpDrive");

                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5);
                    DeactivateWarpDrive(true);
                    return;
                }
                var insufficientMessage = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_insufficientPowerPercentageAt") + " " + (powerReturned / currentPowerRequirementForWarp).ToString("P1") + " " + Localizer.Format("#LOC_KSPIE_AlcubierreDrive_reducingElectricPowerDrain");
                Debug.Log("[KSPI]: " + insufficientMessage);
                ScreenMessages.PostScreenMessage(insufficientMessage, 5);
                ReduceWarpPower();
            }

            if (!hasWarpFactorChanged && !hasPowerShortage && !hasHeadingChanged && !hasGravityPullImbalance)
                return;

            if (hasHeadingChanged)
                _counterPreviousChange = _counterCurrent;

            existing_warp_speed = _engineThrottle[selected_factor];

            var reverseHeading = new Vector3d(-_headingAct.x, -_headingAct.y, -_headingAct.z);

            _headingAct = newPartHeading * PluginSettings.Config.SpeedOfLight * existing_warp_speed;
            serialisedwarpvector = ConfigNode.WriteVector(_headingAct);

            _activePartHeading = newPartHeading;

            if (!vessel.packed && useRotateStability)
                OrbitPhysicsManager.HoldVesselUnpack();

            // puts the ship back into a simulated orbit and re-enables physics, is this still needed?
            if (!vessel.packed)
                vessel.GoOnRails();

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + reverseHeading + _headingAct, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            // disables physics and puts the ship into a propagated orbit , is this still needed?
            if (!vessel.packed)
                vessel.GoOffRails();

            // disable warp trail for 2 frames to prevent graphic glitches
            _warpTrailTimeout = 2;

            UpdateAllWarpDriveEffects();
        }

        private void Develocitize()
        {
            Debug.Log("[KSPI]: Develocitize");

            // This code is inspired quite heavily by HyperEdit's OrbitEditor.cs
            double universalTime = Planetarium.GetUniversalTime();
            Orbit currentOrbit = vessel.orbitDriver.orbit;
            Vector3d currentOrbitalVelocity = currentOrbit.getOrbitalVelocityAtUT(universalTime);
            Vector3d progradeNormalizedVelocity = currentOrbitalVelocity.normalized;
            Vector3d velocityToCancel = currentOrbitalVelocity;

            // apply gravity drag modifier
            velocityToCancel *= gravityDragRatio;

            // Extremely small velocities cause the game to mess up very badly, so try something small and increase...
            long multiplier = 0;
            Orbit newOrbit;
            do
            {
                Vector3d retrogradeNormalizedVelocity = progradeNormalizedVelocity * -multiplier;

                newOrbit = new Orbit(currentOrbit);
                newOrbit.UpdateFromStateVectors(currentOrbit.pos, retrogradeNormalizedVelocity - velocityToCancel, currentOrbit.referenceBody, universalTime);

                multiplier += 1;
            } while (multiplier < 10000 && double.IsNaN(newOrbit.getOrbitalVelocityAtUT(universalTime).magnitude));

            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.landedAt = Empty;

            // I'm actually not sure what this is for... but HyperEdit does it.
            // I had weird problems when I took it out, anyway.
            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning("[KSPI]: NullReferenceException during Develocitize");
            }
            var allVessels = FlightGlobals.fetch == null
                ? (IEnumerable<Vessel>)new[] { vessel }
                : FlightGlobals.Vessels;

            foreach (var currentVessel in allVessels.Where(v => v.packed == false))
            {
                currentVessel.GoOnRails();
            }
            // End HyperEdit code I don't really understand

            currentOrbit.inclination = newOrbit.inclination;
            currentOrbit.eccentricity = newOrbit.eccentricity;
            currentOrbit.semiMajorAxis = newOrbit.semiMajorAxis;
            currentOrbit.LAN = newOrbit.LAN;
            currentOrbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
            currentOrbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
            currentOrbit.epoch = newOrbit.epoch;
            currentOrbit.Init();
            currentOrbit.UpdateFromUT(universalTime);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        // ReSharper disable once UnusedMember.Global
        public void OnGUI()
        {
            if (vessel == FlightGlobals.ActiveVessel && showWindow)
                windowPosition = GUILayout.Window(_windowId, windowPosition, Window, Localizer.Format("#LOC_KSPIE_AlcubierreDrive_warpControlWindow"));
        }

        private void PrintToGUILayout(string label, string value, GUIStyle boldStyle, GUIStyle textStyle, int widthLabel = 170, int widthValue = 130)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, boldStyle, GUILayout.Width(widthLabel));
            GUILayout.Label(value, textStyle, GUILayout.Width(widthValue));
            GUILayout.EndHorizontal();
        }

        private void Window(int windowId)
        {
            try
            {
                _windowPositionX = windowPosition.x;
                _windowPositionY = windowPosition.y;

                InitializeStyles();

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    showWindow = false;

                GUILayout.BeginVertical();

                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_warpdriveType"), part.partInfo.title, _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_totalWarpPower"), totalWarpPower.ToString("0.0") + " t", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_engineMass"), vesselTotalMass.ToString("0.000") + " t", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_warpToMassRatio"), "1:" + warpToMassRatio.ToString("0.000"), _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_gravityAtSeaLevel"), gravityAtSeaLevel.ToString("0.00000") + " m/s\xB2", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_gravityVesselPull"), gravityPull.ToString("0.00000") + " m/s\xB2", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_gravityDragPercentage"), gravityDragPercentage.ToString("0.000") + "%", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_maxAllowedThrotle"), maximumAllowedWarpThrotle.ToString("0.0000") + " c", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentSelectedSpeed"), warpEngineThrottle.ToString("0.0000") + " c", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentPowerReqForWarp"), PluginHelper.GetFormattedPowerString(currentPowerRequirementForWarp), _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentWarpExitSpeed"), exitSpeed.ToString("0.000") + " m/s", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentWarpExitApoapsis"), exitApoapsis.ToString("0.000") + " km", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentWarpExitPeriapsis"), exitPeriapsis.ToString("0.000") + " km", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentWarpExitEccentricity"), exitEccentricity.ToString("0.000"), _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentWarpExitMeanAnomaly"), exitMeanAnomaly.ToString("0.000") + "\xB0", _boldBlackStyle, _textBlackStyle);
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_currentWarpExitBurnToCircularize"), exitBurnCircularize.ToString("0.000") + " m/s", _boldBlackStyle, _textBlackStyle);

                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_MaximumWarpForAltitude"), (maximumWarpForAltitude).ToString("0.000"), _boldBlackStyle, _textBlackStyle);//"Maximum Warp For Altitude"
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_Distancetoclosestbody"), (distanceToClosestBody * 0.001).ToString("0.000") + " km" , _boldBlackStyle, _textBlackStyle);//"Distance to closest body"
                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_Closestbody"), closestCelestrialBodyName, _boldBlackStyle, _textBlackStyle);//"Closest body"

                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_CosineToClosestBody"), cosineAngleToClosestBody.ToString("0.000"), _boldBlackStyle, _textBlackStyle);//"Cosine To Closest Body"

                PrintToGUILayout(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_status"), driveStatus, _boldBlackStyle, _textBlackStyle);

                var speedText = Localizer.Format("#LOC_KSPIE_AlcubierreDrive_speed");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("(-) " + speedText, GUILayout.MinWidth(150)))
                    ToggleWarpSpeedDown();
                if (GUILayout.Button("(+) " + speedText, GUILayout.MinWidth(150)))
                    ToggleWarpSpeedUp();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("(-) " + speedText + " x3", GUILayout.MinWidth(150)))
                    ToggleWarpSpeedDown3();
                if (GUILayout.Button("(+) " + speedText + " x3", GUILayout.MinWidth(150)))
                    ToggleWarpSpeedUp3();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("(-) " + speedText + " x10", GUILayout.MinWidth(150)))
                    ToggleWarpSpeedDown10();
                if (GUILayout.Button("(+) " + speedText + " x10", GUILayout.MinWidth(150)))
                    ToggleWarpSpeedUp10();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("(-) " + speedText + " MIN", GUILayout.MinWidth(150)))
                    MinimizeWarpSpeed();
                if (GUILayout.Button("(+) " + speedText + " MAX", GUILayout.MinWidth(150)))
                    MaximizeWarpSpeed ();
                GUILayout.EndHorizontal();

                if (!IsEnabled && GUILayout.Button(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_activateWarpDrive"), GUILayout.ExpandWidth(true)))
                {
                    Debug.Log("[KSPI]: Activate Warp Drive window button pressed");
                    StartWarpAsSoonAsPossible();
                }

                if (IsEnabled && GUILayout.Button(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_deactivateWarpDrive"), GUILayout.ExpandWidth(true)))
                {
                    Debug.Log("[KSPI]: Deactivate Warp Drive window button pressed");
                    StopWarpSoonAsPossible();
                }

                if (!IsEnabled && !IsCharging && GUILayout.Button(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_startChargingDrive"), GUILayout.ExpandWidth(true)))
                    StartCharging();

                if (!IsEnabled && IsCharging && GUILayout.Button(Localizer.Format("#LOC_KSPIE_AlcubierreDrive_stopChargingDrive"), GUILayout.ExpandWidth(true)))
                    StopCharging();

                GUILayout.EndVertical();
                GUI.DragWindow();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: AlcubierreDrive Window(" + windowId + "): " + e.Message);
            }
        }

        private void StartWarpAsSoonAsPossible()
        {
            if (antigravityPercentage < 100)
            {
                _startWarpAsSoonAsPossible = true;
                antigravityPercentage = 100;
                ActivateWarpDrive(false);
            }
            else
                ActivateWarpDrive(true);
        }

        private void StopWarpSoonAsPossible()
        {
            if (warpEngineThrottle < 100)
                DeactivateWarpDrive(true);
            else
                _stopWarpSoonAsPossible = true;
        }


        private void InitializeStyles()
        {
            if (_boldBlackStyle == null)
            {
                _boldBlackStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    font = PluginHelper.MainFont
                };
            }

            if (_textBlackStyle == null)
            {
                _textBlackStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal,
                    font = PluginHelper.MainFont
                };
            }
        }

        private static double DistanceToClosestBody(Vessel vessel, out CelestialBody closestBody, out bool targetVesselIsClosest)
        {
            var minimumDistance = vessel.altitude;
            closestBody = vessel.mainBody;
            targetVesselIsClosest = false;

            var vesselTarget = FlightGlobals.fetch.VesselTarget;
            if (vesselTarget != null)
            {
                var transform = vesselTarget.GetTransform();
                var toTarget = vessel.CoMD - transform.position;
                var distanceToTarget = toTarget.magnitude;

                if (distanceToTarget < minimumDistance)
                {
                    minimumDistance = distanceToTarget;
                    targetVesselIsClosest = true;
                }
            }

            if (vessel.orbit.closestEncounterBody != null)
            {
                var celestialBody = vessel.orbit.closestEncounterBody;
                var toBody = vessel.CoMD - celestialBody.position;
                var distanceToSurfaceBody = toBody.magnitude - celestialBody.Radius;

                if (distanceToSurfaceBody < minimumDistance)
                {
                    minimumDistance = distanceToSurfaceBody;
                    closestBody = celestialBody;
                    targetVesselIsClosest = false;
                }
            }

            if (vessel.mainBody.orbit != null && vessel.mainBody.orbit.referenceBody != null)
            {
                var celestialBody = vessel.mainBody.orbit.referenceBody;
                var toBody = vessel.CoMD - celestialBody.position;
                var distanceToSurfaceBody = toBody.magnitude - celestialBody.Radius;

                if (distanceToSurfaceBody < minimumDistance)
                {
                    minimumDistance = distanceToSurfaceBody;
                    closestBody = celestialBody;
                    targetVesselIsClosest = false;
                }

                foreach (var moon in celestialBody.orbitingBodies)
                {
                    var toMoon = vessel.CoMD - moon.position;
                    var distanceToSurfaceMoon = toMoon.magnitude - moon.Radius;

                    if (!(distanceToSurfaceMoon < minimumDistance)) continue;

                    minimumDistance = distanceToSurfaceMoon;
                    closestBody = moon;
                    targetVesselIsClosest = false;
                }
            }

            foreach (var planet in vessel.mainBody.orbitingBodies)
            {
                var toPlanet = vessel.CoMD - planet.position;
                var distanceToSurfacePlanet = toPlanet.magnitude - planet.Radius;

                if (distanceToSurfacePlanet < minimumDistance)
                {
                    minimumDistance = distanceToSurfacePlanet;
                    closestBody = planet;
                }

                foreach (var moon in planet.orbitingBodies)
                {
                    var toMoon = vessel.CoMD - moon.position;
                    var distanceToSurfaceMoon = toMoon.magnitude - moon.Radius;

                    if (!(distanceToSurfaceMoon < minimumDistance)) continue;

                    minimumDistance = distanceToSurfaceMoon;
                    closestBody = moon;
                    targetVesselIsClosest = false;
                }
            }

            return minimumDistance;
        }
    }
}
