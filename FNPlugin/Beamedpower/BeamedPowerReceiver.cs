using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Microwave;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
using FNPlugin.Propulsion;
using FNPlugin.Redist;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using UnityEngine;

namespace FNPlugin.Beamedpower
{
    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName1")]//Solar Power Receiver Dish
    class SolarBeamedPowerReceiverDish : SolarBeamedPowerReceiver { } // receives less of a power capacity nerve in NF mode

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName2")]//Solar Power Receiver
    class SolarBeamedPowerReceiver : BeamedPowerReceiver {} // receives less of a power cpacity nerve in NF mode

    //---------------------------------------------------------

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName3")]//Microwave Power Receiver Dish
    class MicrowavePowerReceiverDish : MicrowavePowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName4")]//Microwave Power Receiver Panel
    class MicrowavePowerReceiverPanel : MicrowavePowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName5")]//Microwave Power Receiver
    class MicrowavePowerReceiver : BeamedPowerReceiver { }

    //---------------------------------------------------

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName6")]//Photovoltaic Power Receiver Dish
    class PhotovoltaicPowerReceiverDish : PhotovoltaicPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName7")]//Photovoltaic Power Receiver Dish
    class PhotovoltaicPowerReceiverPanel : PhotovoltaicPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName8")]//Photovoltaic Power Receiver
    class PhotovoltaicPowerReceiver : BeamedPowerReceiver { }

    //---------------------------------------------------

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName9")]//Rectenna Power Receiver Dish
    class RectennaPowerReceiverDish : RectennaPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName10")]//Rectenna Power Receiver Dish
    class RectennaPowerReceiverPanel : RectennaPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName11")]//Rectenna Power Receiver
    class RectennaPowerReceiver : BeamedPowerReceiver { }

    //---------------------------------------------------

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName12")]//Thermal Power Panel Receiver Panel
    class ThermalPowerReceiverPanel : ThermalPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName13")]//Thermal Power Panel Receiver Dish
    class ThermalPowerReceiverDish : ThermalPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName14")]//Thermal Power Receiver
    class ThermalPowerReceiver : BeamedPowerReceiver { }

    //------------------------------------------------------

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName15")]//Beamed Power Receiver Panel
    class BeamedPowerReceiverPanel : BeamedPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName16")]//Beamed Power Receiver Dish
    class BeamedPowerReceiverDish : BeamedPowerReceiver { }

    [KSPModule("#LOC_KSPIE_BeamPowerReceiver_ModulueName17")]//Beamed Power Receiver
    class BeamedPowerReceiver : ResourceSuppliableModule, IFNPowerSource, IElectricPowerGeneratorSource, IBeamedPowerReceiver // tweakscales with exponent 2.5
    {
        private const int LabelWidth = 200;
        private const int WideLabelWidth = 250;
        private const int ValueWidthWide = 100;
        private const int ValueWidthNormal = 65;

        public const string GROUP = "BeamedPowerReceiver";
        public const string GROUP_TITLE = "#LOC_KSPIE_BeamPowerReceiver_groupName";

        //Persistent True
        [KSPField(isPersistant = true)] public double storedTemp;
        [KSPField(isPersistant = true)] public float windowPositionX = 200;
        [KSPField(isPersistant = true)] public float windowPositionY = 100;
        [KSPField(isPersistant = true)] public bool forceActivateAtStartup;
        [KSPField(isPersistant = true)] public double thermalSolarInputMegajoules;
        [KSPField(isPersistant = true)] public double thermalSolarInputMegajoulesMax;
        [KSPField(isPersistant = true)] public double storedGeneratorThermalEnergyRequestRatio;
        [KSPField(isPersistant = true)] public double storedIsThermalEnergyGeneratorEfficiency;

        [KSPField(isPersistant = true)] protected double total_sat_efficiency_fraction;
        [KSPField(isPersistant = true)] protected double total_beamed_power;
        [KSPField(isPersistant = true)] protected double total_beamed_power_max;
        [KSPField(isPersistant = true)] protected double total_beamed_wasteheat;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_Bandwidth")]//Bandwidth
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedBandwidthConfiguration;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "Minimum Consumption %"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]
        public float minimumConsumptionPercentage = 0;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "Maximum Consumption %"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]
        public float maximumConsumptionPercentage = 100;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_EnabledReceiver")]//Enabled
        public bool receiverIsEnabled;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_LinkedforRelay")]//Linked for Relay
        public bool linkedForRelay;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_TargetWavelength", guiFormat = "F5")]//Target Wavelength
        public double targetWavelength;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_MaxInputPower", guiFormat = "F3", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Max Input Power
        public double powerInputMegajoulesMax;

        //Persistent False
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_WasteheatRatio", guiFormat = "F6")]//Wasteheat Ratio
        public double wasteheatRatio;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_WasteheatElectricEfficiency", guiFormat = "F6")]//Wasteheat Electric Efficiency
        public double wasteheatElectricConversionEfficiency;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_BeamedElectricEfficiency", guiFormat = "F6")]//Beamed Electric Efficiency
        public double effectiveBeamedPowerElectricEfficiency;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_SolarElectricEfficiency", guiFormat = "F6")]//Solar Electric Efficiency
        public double effectiveSolarThermalElectricEfficiency;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = true, guiName = "Max Power Capacity", guiFormat = "F3", guiUnits = " MW")]
        public double maximumReceiverPowerCapacity;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_TargetWavelength")]
        public string bandWidthName;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_ReceiverDiameter", guiFormat = "F3", guiUnits = " m")]//Receiver Diameter
        public double diameter = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_IsThermalReceiverSlave")]//Is Slave
        public bool isThermalReceiverSlave = false;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_InputPower", guiFormat = "F3", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Input Power
        public double powerInputMegajoules;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_ThermalPower", guiFormat = "F3", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Thermal Power
        public double ThermalPower;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_Radius", guiFormat = "F2", guiUnits = " m")]//Radius
        public double radius = 2.5;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_MinWavelength")]//min Wavelength
        public double minimumWavelength = 0.00000001;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = true,  guiName = "#LOC_KSPIE_BeamPowerReceiver_MaxWavelength")]//max Wavelength
        public double maximumWavelength = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_HotbathTechLevel")]//Hotbath TechLevel
        public int hothBathtechLevel;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_HotBathTemperature", guiUnits = " K")]//HotBath Temperature
        public double hothBathTemperature = 3200;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_CoreTemperature")]//Core Temperature
        public string coreTempererature;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_ProducedPower")]//Produced Power
        public string beamedpower;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_SatellitesConnected")]//Satellites Connected
        public string connectedsats;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_RelaysConnected")]//Relays Connected
        public string connectedrelays;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_NetworkDepth")]//Network Depth
        public string networkDepthString;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_SlavesAmount")]//Connected Slaves
        public int slavesAmount;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_SlavesPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Slaves Power
        public double slavesPower;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_AvailableThermalPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Available Thermal Power
        public double total_thermal_power_available;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_AvailableThermalPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Thermal Power Supply
        public double total_thermal_power_provided;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_MaxThermalPowerSupply", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Max Thermal Power Supply
        public double total_thermal_power_provided_max;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_MaximumInputPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F3")]//Maximum Input Power
        public double maximumPower = 0;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = false,  guiName = "#LOC_KSPIE_BeamPowerReceiver_MaximumElectricPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F3")]//Maximum Electric Power
        public double maximumThermalPower = 0;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_Dissipation", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F3")]//Dissipation
        public double dissipationInMegaJoules;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_SolarFacingFactor", guiFormat = "F4")]//Sun Facing Factor
        public double solarFacingFactor;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_SolarFlux", guiFormat = "F4")]//Solar Flux
        public double solarFlux;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_Generator_InitialGeneratorPowerEC", guiUnits = " kW")]//Offscreen Power Generation
        public double initialGeneratorPowerEC;

        // Buttons
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_PowerMode"), UI_Toggle(disabledText = "#LOC_KSPIE_BeamPowerReceiver_ElectricMode", enabledText = "#LOC_KSPIE_BeamPowerReceiver_ThermalMode")]//Power Mode--Electric--Thermal
        public bool thermalMode;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_BeamPowerReceiver_RadiatorMode"), UI_Toggle(disabledText = "#LOC_KSPIE_BeamPowerReceiver_BeamedPowerMode", enabledText = "#LOC_KSPIE_BeamPowerReceiver_RadiatorMode")]//Function--Beamed Power--Radiator
        public bool radiatorMode = false;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_SolarPowerMode"), UI_Toggle(disabledText = "#LOC_KSPIE_BeamPowerReceiver_BeamedPowerMode", enabledText = "#LOC_KSPIE_BeamPowerReceiver_SolarMode")]//Power Mode--Beamed Power--Solar Only
        public bool solarPowerMode = true;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_ShowWindow"), UI_Toggle(disabledText = "#LOC_KSPIE_BeamPowerReceiver_WindowHide", enabledText = "#LOC_KSPIE_BeamPowerReceiver_WindowShow")]//Power Reciever Interface--Hidden--Shown
        public bool showWindow;

        // Settings
        [KSPField] public bool isThermalReceiver = false;
        [KSPField] public bool isEnergyReceiver = true;
        [KSPField] public bool maintainResourceBuffers = true;
        [KSPField] public bool canSwitchBandwidthInEditor = false;
        [KSPField] public bool canSwitchBandwidthInFlight = false;
        [KSPField] public bool autoDeploy = true;
        [KSPField] public bool canLinkup = true;
        [KSPField] public bool isMirror = false;
        [KSPField] public bool showBandWidthName = false;
        [KSPField] public bool showSelectedBandwidthConfiguration = true;

        [KSPField] public int connectStackdepth = 4;
        [KSPField] public int connectParentdepth = 2;
        [KSPField] public int connectSurfacedepth = 2;
        [KSPField] public int receiverType = 0;
        [KSPField] public int supportedPropellantAtoms = 511;
        [KSPField] public int supportedPropellantTypes = 127;

        [KSPField] public float defaultPowerGeneratorGeneratorPercentage = 200;
        [KSPField] public double maximumElectricPower = 0;
        [KSPField] public double electricWasteheatExponent = 1;
        [KSPField] public double electricMaxEfficiency = 1;
        [KSPField] public double facingThreshold = 0;
        [KSPField] public double facingSurfaceExponent = 1;
        [KSPField] public double facingEfficiencyExponent = 0.1;
        [KSPField] public double spotsizeNormalizationExponent = 1;
        [KSPField] public double solarReceptionEfficiency = 0;
        [KSPField] public double solarElectricEfficiency = 0.33;
        [KSPField] public double solarReceptionSurfaceArea = 0;
        [KSPField] public double solarFacingExponent = 1;
        [KSPField] public double alternatorRatio = 1;
        [KSPField] public double minCoolingFactor = 1;
        [KSPField] public double engineHeatProductionMult = 1;
        [KSPField] public double plasmaHeatProductionMult = 1;
        [KSPField] public double engineWasteheatProductionMult = 1;
        [KSPField] public double plasmaWasteheatProductionMult = 1;
        [KSPField] public double heatTransportationEfficiency = 0.7;
        [KSPField] public double thermalPowerBufferMult = 2;
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public double wasteHeatModifier = 1;
        [KSPField] public double apertureMultiplier = 1;
        [KSPField] public double highSpeedAtmosphereFactor = 0;
        [KSPField] public double atmosphereToleranceModifier = 1;
        [KSPField] public double thermalPropulsionEfficiency = 1;
        [KSPField] public double thermalEnergyEfficiency = 1;
        [KSPField] public double thermalProcessingModifier = 1;
        [KSPField] public double magneticNozzlePowerMult = 1;
        [KSPField] public double magneticNozzleMhdMult = 4;

        [KSPField] public double hothBathTemperatureMk1 = 2000;
        [KSPField] public double hothBathTemperatureMk2 = 2500;
        [KSPField] public double hothBathTemperatureMk3 = 3000;
        [KSPField] public double hothBathTemperatureMk4 = 3500;
        [KSPField] public double hothBathTemperatureMk5 = 4000;
        [KSPField] public double hothBathTemperatureMk6 = 4500;

        [KSPField] public string animName = "";
        [KSPField] public string animTName = "";
        [KSPField] public string upgradeTechReqMk2 = "heatManagementSystems";
        [KSPField] public string upgradeTechReqMk3 = "advHeatManagement";
        [KSPField] public string upgradeTechReqMk4 = "specializedRadiators";
        [KSPField] public string upgradeTechReqMk5 = "exoticRadiators";
        [KSPField] public string upgradeTechReqMk6 = "extremeRadiators";

        [KSPField] public double powerCapacityEfficiency;
        [KSPField] public double powerMult = 1;
        [KSPField] public double currentIsThermalEnergyGeneratorEfficiency;
        [KSPField] public double currentGeneratorThermalEnergyRequestRatio;

        private BaseField _beamedpowerField;
        private BaseField _linkedForRelayField;
        private BaseField _slavesAmountField;
        private BaseField _thermalPowerField;
        private BaseField _minimumConsumptionPercentageField;
        private BaseField _maximumConsumptionPercentageField;
        private BaseField _selectedBandwidthConfigurationField;
        private BaseField _maximumWavelengthField;
        private BaseField _minimumWavelengthField;
        private BaseField _solarFacingFactorField;
        private BaseField _solarFluxField;
        private BaseField _coreTempereratureField;
        private BaseField _bandWidthNameField;
        private BaseField _connectedsatsField;
        private BaseField _connectedrelaysField;
        private BaseField _networkDepthStringField;

        private BaseEvent _linkReceiverBaseEvent;
        private BaseEvent _unlinkReceiverBaseEvent;
        private BaseEvent _activateReceiverBaseEvent;
        private BaseEvent _disableReceiverBaseEvent;

        private FNRadiator fnRadiator;
        private ModuleGenerator stockModuleGenerator;
        private ModuleResource mockInputResource;
        private ModuleResource outputModuleResource;
        private BaseEvent moduleGeneratorShutdownBaseEvent;
        private BaseEvent moduleGeneratorActivateBaseEvent;
        private BaseField moduleGeneratorEfficientBaseField;

        private ModuleDeployableSolarPanel deployableSolarPanel;
        private ModuleDeployableRadiator deployableRadiator;
        private ModuleDeployableAntenna deployableAntenna;

        private Queue<double> beamedPowerQueue = new Queue<double>(10);
        private Queue<double> beamedPowerMaxQueue = new Queue<double>(10);
        private Queue<double> solarFluxQueue = new Queue<double>(50);

        //Internal
        private int _connectedsatsi;
        private int _connectedrelaysi;
        private int _networkDepth;
        private int _activeSatsIncr;

        private bool _hasTransmitter;
        private bool _isLoaded;

        private int restartCounter;
        private int _windowId;
        private int _initializationCountdown;
        private long _deactivateTimer;

        private double _totalConversionWasteHeatProduction;
        private double _powerDownFraction;

        private Animation animT;
        private GUIStyle _boldBlackStyle;
        private GUIStyle _textBlackStyle;
        private Rect _windowPosition;
        private BeamedPowerTransmitter partTransmitter;
        private ModuleAnimateGeneric genericAnimation;
        private CelestialBody localStar;
        private PowerStates _powerState;
        private ResourceBuffers _resourceBuffers;
        private Dictionary<Guid, double> _connectedReceiverFractionById = new Dictionary<Guid, double>();

        private readonly List<IFNEngineNoozle> _connectedEngines = new List<IFNEngineNoozle>();
        private readonly Dictionary<Vessel, ReceivedPowerData> _receivedPower = new Dictionary<Vessel, ReceivedPowerData>();
        private readonly List<BeamedPowerReceiver> thermalReceiverSlaves = new List<BeamedPowerReceiver>();
        private readonly Dictionary<Guid, double> connectedReceiverById = new Dictionary<Guid, double>();

        // Properties
        public float DefaultPowerGeneratorPercentage => defaultPowerGeneratorGeneratorPercentage;
        public double FuelRatio => 1;
        public double MagneticNozzlePowerMult => magneticNozzlePowerMult;
        public double MagneticNozzleMhdMult => magneticNozzleMhdMult;
        public bool MayExhaustInAtmosphereHomeworld => true;
        public bool MayExhaustInLowSpaceHomeworld => true;
        public double MinThermalNozzleTempRequired => 0;
        public double CurrentMeVPerChargedProduct => 0;
        public double CurrentPlasmaPropulsionRatio => 0;
        public double CurrentChargedPropulsionRatio => 0;
        public bool UsePropellantBaseIsp => false;
        public bool CanUseAllPowerForPlasma => false;
        public bool CanProducePower => ProducedThermalHeat > 0;
        public double MinCoolingFactor => minCoolingFactor;
        public double PlasmaAfterburnerRange => 2;
        public double EngineHeatProductionMult => engineHeatProductionMult;
        public double PlasmaHeatProductionMult => plasmaHeatProductionMult;
        public double EngineWasteheatProductionMult => engineWasteheatProductionMult;
        public double PlasmaWasteheatProductionMult => plasmaWasteheatProductionMult;
        public int ReceiverType => receiverType;
        public double Diameter => diameter;
        public double ApertureMultiplier => apertureMultiplier;
        public double MaximumWavelength => maximumWavelength;
        public double MinimumWavelength => minimumWavelength;
        public double HighSpeedAtmosphereFactor => highSpeedAtmosphereFactor;
        public double FacingThreshold => facingThreshold;
        public double FacingSurfaceExponent => facingSurfaceExponent;
        public double FacingEfficiencyExponent => facingEfficiencyExponent;
        public double SpotsizeNormalizationExponent => spotsizeNormalizationExponent;
        public Part Part => part;
        public Vessel Vessel => vessel;
        public int ProviderPowerPriority => 1;
        public double ConsumedFuelFixed => 0;
        public double ProducedThermalHeat => powerInputMegajoules;
        public double ProducedChargedPower => 0;
        public double PowerRatio => maximumConsumptionPercentage / 100d;
        public double ProducedPower => ProducedThermalHeat;
        public bool SupportMHD => false;
        public double MinimumThrottle => 0;
        public double AverageEfficiencyFraction => total_beamed_power > 0 ? total_sat_efficiency_fraction / total_beamed_power : 0;
        public int SupportedPropellantAtoms => supportedPropellantAtoms;
        public int SupportedPropellantTypes => supportedPropellantTypes;
        public bool FullPowerForNonNeutronAbsorbants => true;
        public double ReactorSpeedMult => 1;
        public double ThermalProcessingModifier => thermalProcessingModifier;
        public double ThermalPropulsionWasteheatModifier => 1;
        public double EfficencyConnectedThermalEnergyGenerator => storedIsThermalEnergyGeneratorEfficiency;
        public double EfficencyConnectedChargedEnergyGenerator => 0;
        public bool IsThermalSource => isThermalReceiver;
        public void UseProductForPropulsion(double ratio, double propellantMassPerSecond, PartResourceDefinition[] resource) { }
        public double RawMaximumPowerForPowerGeneration => powerInputMegajoulesMax;
        public double RawMaximumPower => MaximumReceiverPowerCapacity;
        public double RawTotalPowerProduced => ThermalPower * TimeWarp.fixedDeltaTime;
        public double ChargedPowerRatio => 0;
        public double PowerBufferBonus => 0;
        public double ThermalTransportationEfficiency => heatTransportationEfficiency;
        public double ThermalPropulsionEfficiency => thermalPropulsionEfficiency;
        public double PlasmaPropulsionEfficiency => 0;
        public double ChargedParticlePropulsionEfficiency => 0;
        public double ThermalEnergyEfficiency => thermalEnergyEfficiency;
        public double PlasmaEnergyEfficiency => 0;
        public double ChargedParticleEnergyEfficiency => 0;
        public bool IsSelfContained => false;
        public double CoreTemperature => hothBathTemperature;
        public double MaxCoreTemperature => hothBathTemperature;
        public double HotBathTemperature => hothBathTemperature;
        public double StableMaximumReactorPower => RawMaximumPower;
        public double MaximumPower => MaximumThermalPower;
        public double MaximumThermalPower => HighLogic.LoadedSceneIsEditor ? maximumThermalPower * powerMult : ThermalPower;
        public double NormalisedMaximumPower => ThermalPower;
        public double MaximumChargedPower => 0;
        public double MinimumPower => 0;
        public bool IsVolatileSource => true;
        public bool IsActive => receiverIsEnabled;
        public bool IsNuclear => false;
        public double MaximumReceiverPowerCapacity => CalculateMaximumReceiverPowerCapacity(CalculateDynamicPressure());

        public double PowerCapacityEfficiency
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight || CheatOptions.IgnoreMaxTemperature || isThermalReceiver)
                    return 1;

                return 1 - wasteheatRatio * GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);
            }
        }

        public double WasteheatElectricConversionEfficiency
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight || CheatOptions.IgnoreMaxTemperature || electricWasteheatExponent == 0) return 1;

                if (electricWasteheatExponent == 1)
                    return 1 - wasteheatRatio;
                else
                    return 1 - Math.Pow(wasteheatRatio, electricWasteheatExponent);
            }
        }

        public double CalculateMaximumReceiverPowerCapacity(double dynamicPressure)
        {
            var maxPower = thermalMode && maximumThermalPower > 0
                ? maximumThermalPower
                : maximumElectricPower > 0
                    ? maximumElectricPower
                    : maximumPower;

            var scaledPower = maxPower * powerMult;

            return DetermineCanBeActiveInAtmosphere(dynamicPressure) ? scaledPower : scaledPower * highSpeedAtmosphereFactor;
        }

        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        public void Restart(int counter)
        {
            restartCounter = counter;
        }

        public void UpdateAuxiliaryPowerSource(double available) { }
        public bool IsConnectedToChargedGenerator => ConnectedThermalElectricGenerator != null;
        public bool IsConnectedToThermalGenerator => ConnectedChargedParticleElectricGenerator != null;

        public void RemoveOtherVesselData()
        {
            var deleteList = new List<Vessel>();

            foreach(var r in  _receivedPower)
            {
                if (r.Key != vessel)
                    deleteList.Add(r.Key);
            }

            foreach(Vessel otherVessel in  deleteList)
            {
                _receivedPower.Remove(otherVessel);
            }
        }

        public void Reset()
        {
            Debug.Log("[KSPI]: BeamedPowerReceiver reset called");
            _receivedPower.Clear();
        }

        public void FindAndAttachToPowerSource() {}

        private void DetermineTechLevel()
        {
            hothBathtechLevel = 1;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk2)) hothBathtechLevel++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk3)) hothBathtechLevel++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk4)) hothBathtechLevel++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk5)) hothBathtechLevel++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReqMk6)) hothBathtechLevel++;
        }

        private void DetermineCoreTemperature()
        {
            switch (hothBathtechLevel)
            {
                case 1: hothBathTemperature = hothBathTemperatureMk1; break;
                case 2: hothBathTemperature = hothBathTemperatureMk2; break;
                case 3: hothBathTemperature = hothBathTemperatureMk3; break;
                case 4: hothBathTemperature = hothBathTemperatureMk4; break;
                case 5: hothBathTemperature = hothBathTemperatureMk5; break;
                case 6: hothBathTemperature = hothBathTemperatureMk6; break;
                default:
                    break;
            }
        }

        public void RegisterAsSlave(BeamedPowerReceiver receiver)
        {
            thermalReceiverSlaves.Add(receiver);
        }

        public void ConnectWithEngine(IEngineNoozle engine)
        {
            var fnEngine = engine as IFNEngineNoozle;
            if (fnEngine == null)
                return;

            if (!_connectedEngines.Contains(fnEngine))
                _connectedEngines.Add(fnEngine);
        }

        public void DisconnectWithEngine(IEngineNoozle engine)
        {
            var fnEngine = engine as IFNEngineNoozle;
            if (fnEngine == null)
                return;

            if (_connectedEngines.Contains(fnEngine))
                _connectedEngines.Remove(fnEngine);
        }

        public void NotifyActiveThermalEnergyGenerator(double efficiency, double power_ratio, bool isMHD, double mass)
        {
            NotifyActiveThermalEnergyGenerator(efficiency, power_ratio);
        }

        public void NotifyActiveThermalEnergyGenerator(double efficiency, double power_ratio)
        {
            currentIsThermalEnergyGeneratorEfficiency = efficiency;
            currentGeneratorThermalEnergyRequestRatio = power_ratio;
        }

        public void NotifyActiveChargedEnergyGenerator(double efficiency, double power_ratio) { }

        public void NotifyActiveChargedEnergyGenerator(double efficiency, double power_ratio, double mass) { }


        [KSPField(guiActive = false)] public double _requestedThermalThrottle;
        public double RequestedThermalThrottle
        {
            get => _requestedThermalThrottle;
            private set => _requestedThermalThrottle = value;
        }

        [KSPField(guiActive = false)] public double _requestedPlasmaThrottle;
        public double RequestedPlasmaThrottle
        {
            get => _requestedPlasmaThrottle;
            private set => _requestedPlasmaThrottle = value;
        }

        [KSPField(guiActive = false)] public double _requestedChargedThrottle;
        public double RequestedChargedThrottle
        {
            get => _requestedChargedThrottle;
            private set => _requestedChargedThrottle = value;
        }

        public double NormalizedPowerMultiplier => 1;

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType) { return false; }

        public override void AttachThermalReciever(Guid key, double radius)
        {
            if (connectedReceiverById.ContainsKey(key)) return;

            connectedReceiverById.Add(key, radius);
            var connectedReceiversSum = connectedReceiverById.Sum(r => r.Value);
            _connectedReceiverFractionById = connectedReceiverById.ToDictionary(a => a.Key, a => a.Value / connectedReceiversSum);
        }

        public double ProducedWasteHeat { get { return _totalConversionWasteHeatProduction; } }

        public void Refresh() { }

        public override void DetachThermalReciever(Guid key)
        {
            if (!connectedReceiverById.ContainsKey(key)) return;

            connectedReceiverById.Remove(key);
            var connectedReceiversSum = connectedReceiverById.Sum(r => r.Value);
            _connectedReceiverFractionById = connectedReceiverById.ToDictionary(a => a.Key, a => a.Value / connectedReceiversSum);
        }

        public override double GetFractionThermalReciever(Guid key)
        {
            return _connectedReceiverFractionById.TryGetValue(key, out double result) ? result : 0.0;
        }

        [KSPAction("Toggle Receiver Interface")]
        public void ToggleWindow()
        {
            showWindow = !showWindow;
        }

        private void ActivateRecieverState(bool forced = false)
        {
            receiverIsEnabled = true;
            forceActivateAtStartup = true;

            // force activate to trigger any fairings and generators
            Debug.Log("[KSPI]: BeamedPowerReceiver was force activated on  " + part.name);
            this.part.force_activate();

            ShowDeployAnimation(forced);
        }

        private void ShowDeployAnimation(bool forced)
        {
            Debug.Log("[KSPI]: MicrowaveReceiver ShowDeployAnimation is called ");

            if (deployableAntenna != null)
            {
                deployableAntenna.Extend();
            }

            if (deployableSolarPanel != null)
            {
                deployableSolarPanel.Extend();
            }

            if (deployableRadiator != null)
            {
                deployableRadiator.Extend();
            }

            if (genericAnimation != null && genericAnimation.GetScalar < 1)
            {
                genericAnimation.Toggle();
            }

            if (fnRadiator != null && fnRadiator.ModuleActiveRadiator != null)
                fnRadiator.ModuleActiveRadiator.Activate();
        }

        private void DeactivateRecieverState(bool forced = false)
        {
            receiverIsEnabled = false;
            forceActivateAtStartup = false;
            ShowUndeployAnimation(forced);
        }

        private void ShowUndeployAnimation(bool forced)
        {
            if (deployableAntenna != null)
                deployableAntenna.Retract();

            if (deployableSolarPanel != null)
                deployableSolarPanel.Retract();

            if (deployableRadiator != null)
                deployableRadiator.Retract();

            if (genericAnimation != null && genericAnimation.GetScalar > 0 )
                genericAnimation.Toggle();

            if (fnRadiator != null && fnRadiator.ModuleActiveRadiator != null)
                fnRadiator.ModuleActiveRadiator.Shutdown();
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_LinkReceiver", active = true)]//Link Receiver for Relay
        public void LinkReceiver()
        {
            linkedForRelay = true;

            ShowDeployAnimation(true);
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_UnlinkReceiver", active = true)]//Unlink Receiver for Relay
        public void UnlinkReceiver()
        {
            linkedForRelay = false;

            ShowUndeployAnimation(true);
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_ActivateReceiver", active = true)]//Activate Receiver
        public void ActivateReceiver()
        {
            ActivateRecieverState();
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_BeamPowerReceiver_DisableReceiver", active = true)]//Disable Receiver
        public void DisableReceiver()
        {
            DeactivateRecieverState();
        }

        [KSPAction("Activate Receiver")]
        public void ActivateReceiverAction(KSPActionParam param)
        {
            ActivateReceiver();
        }

        [KSPAction("Disable Receiver")]
        public void DisableReceiverAction(KSPActionParam param)
        {
            DisableReceiver();
        }

        [KSPAction("Toggle Receiver")]
        public void ToggleReceiverAction(KSPActionParam param)
        {
            if (receiverIsEnabled)
                DisableReceiver();
            else
                ActivateReceiver();
        }

        public void SetActiveBandwidthConfigurationByWaveLength(double wavelength)
        {
            var foundBandwidthConfiguration = BandwidthConverters.FirstOrDefault(m => m.minimumWavelength < wavelength && m.maximumWavelength > wavelength);

            if (foundBandwidthConfiguration == null) return;

            _isLoaded = true;
            Debug.Log("[KSPI]: BeamedPowerReceiver - found " + foundBandwidthConfiguration.bandwidthName);
            activeBandwidthConfiguration = foundBandwidthConfiguration;
            UpdateProperties();
            selectedBandwidthConfiguration = BandwidthConverters.IndexOf(activeBandwidthConfiguration);
            UpdatePartActionWindow();
        }

        private void UpdatePartActionWindow()
        {
            var window = FindObjectsOfType<UIPartActionWindow>().FirstOrDefault(w => w.part == part);
            if (window == null) return;

            foreach (UIPartActionWindow actionWindow in FindObjectsOfType<UIPartActionWindow>())
            {
                if (window.part != part) continue;
                actionWindow.ClearList();
                actionWindow.displayDirty = true;
            }
        }

        public BandwidthConverter activeBandwidthConfiguration;

        private List<BandwidthConverter> _bandwidthConverters;
        public  List<BandwidthConverter> BandwidthConverters
        {
            get
            {
                if (_bandwidthConverters != null)
                    return _bandwidthConverters;

                var availableBandwidthConverters = part.FindModulesImplementing<BandwidthConverter>().Where(m => PluginHelper.HasTechRequirementOrEmpty(m.techRequirement0));

                _bandwidthConverters = availableBandwidthConverters.OrderByDescending(m => m.TargetWavelength).ToList();

                // initialize maximum tech level
                _bandwidthConverters.ForEach(b => b.Initialize());

                return _bandwidthConverters;
            }
        }

        public static double PhotonicLaserMomentum(double Lambda, uint Time, ulong Wattage)//Lamdba= Wavelength in nanometers, Time in seconds, Wattage in normal Watts, returns momentum of whole laser
        {
            double energySingle = 6.626e-34 * 3e8 / Lambda;
            double photonImpulse = Wattage * Time / energySingle;
            double momentumSingle = 6.626e-34 / Lambda;
            double momentumWhole = momentumSingle * photonImpulse;

            return 2 * momentumWhole; //output is in Newtons per second
        }

        public static double PhotonicLaserMomentum2(double Lambda, int Time, long Wattage)//Lamdba= Wavelength in nanometers, Time in seconds, Wattage in normal Watts, returns momentum of whole laser
        {
            double energyLaser = Wattage * Time;

            var energySingle = 6.626e-34 * 3e8 / Lambda;
            var photonImpulse = energyLaser / energySingle;
            var momentumSingle = (6.626e-34 / (3e8 * Lambda)) * 3e8;
            var momentumWhole = momentumSingle * photonImpulse;

            return 2 * momentumWhole;
        }

        public override void OnStart(StartState state)
        {
            string[] resourcesToSupply =
            {
                ResourceSettings.Config.ElectricPowerInMegawatt, ResourceSettings.Config.WasteHeatInMegawatt,
                ResourceSettings.Config.ThermalPowerInMegawatt
            };

            this.resourcesToSupply = resourcesToSupply;
            base.OnStart(state);

            DetermineTechLevel();
            DetermineCoreTemperature();
            ConnectToModuleGenerator();

            // while in edit mode, listen to on attach/detach event
            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
            }

            InitializeThermalModeSwitcher();

            InitializeBandwidthSelector();

            Fields[nameof(hothBathtechLevel)].guiActiveEditor = isThermalReceiver;
            Fields[nameof(hothBathTemperature)].guiActiveEditor = isThermalReceiver;

            _linkReceiverBaseEvent = Events[nameof(LinkReceiver)];
            _unlinkReceiverBaseEvent = Events[nameof(UnlinkReceiver)];
            _activateReceiverBaseEvent = Events[nameof(ActivateReceiver)];
            _disableReceiverBaseEvent = Events[nameof(DisableReceiver)];

            coreTempererature = CoreTemperature.ToString("0.0") + " K";
            _coreTempereratureField = Fields[nameof(coreTempererature)];

            // hide heat fields when not a thermal receiver
            Fields[nameof(total_thermal_power_provided)].guiActive = IsThermalSource || isThermalReceiverSlave;
            Fields[nameof(total_thermal_power_provided_max)].guiActive = IsThermalSource || isThermalReceiverSlave;

            if (IsThermalSource && !isThermalReceiverSlave)
            {
                _coreTempereratureField.guiActive = true;
                _coreTempereratureField.guiActiveEditor = true;
            }
            else
            {
                _coreTempereratureField.guiActive = false;
                _coreTempereratureField.guiActiveEditor = false;
            }

            // Determine currently maximum and minimum wavelength
            if (BandwidthConverters.Any())
            {
                if (canSwitchBandwidthInEditor)
                {
                    minimumWavelength = activeBandwidthConfiguration.minimumWavelength;
                    maximumWavelength = activeBandwidthConfiguration.maximumWavelength;
                }
                else
                {
                    minimumWavelength = BandwidthConverters.Min(m => m.minimumWavelength);
                    maximumWavelength = BandwidthConverters.Max(m => m.maximumWavelength);
                }
            }

            deployableAntenna = part.FindModuleImplementing<ModuleDeployableAntenna>();
            if (deployableAntenna != null)
            {
                deployableAntenna.Events[nameof(ModuleDeployableAntenna.Extend)].guiActive = false;
                deployableAntenna.Events[nameof(ModuleDeployableAntenna.Retract)].guiActive = false;
            }

            deployableSolarPanel = part.FindModuleImplementing<ModuleDeployableSolarPanel>();
            HideSlavePartModules();

            var isInSolarModeField = Fields[nameof(solarPowerMode)];
            isInSolarModeField.guiActive = deployableSolarPanel != null || solarReceptionSurfaceArea > 0;
            isInSolarModeField.guiActiveEditor = deployableSolarPanel != null || solarReceptionSurfaceArea > 0;

            var dissipationInMegaJoulesField = Fields[nameof(dissipationInMegaJoules)];
            dissipationInMegaJoulesField.guiActive = isMirror;

            if (deployableSolarPanel == null)
                solarPowerMode = false;

            if (!isMirror)
            {
                fnRadiator = part.FindModuleImplementing<FNRadiator>();
                if (fnRadiator != null)
                {
                    if (fnRadiator.isDeployable)
                    {
                        _activateReceiverBaseEvent.guiName =
                            Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_Deploy"); //"Deploy"
                        _disableReceiverBaseEvent.guiName =
                            Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_Retract"); //"Retract"
                    }
                    else
                    {
                        _activateReceiverBaseEvent.guiName =
                            Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_Enable"); //"Enable"
                        _disableReceiverBaseEvent.guiName =
                            Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_Disable"); //"Disable"
                    }

                    fnRadiator.showControls = false;
                    fnRadiator.canRadiateHeat = radiatorMode;
                    fnRadiator.radiatorIsEnabled = radiatorMode;
                }

                var radiatorModeField = Fields[nameof(radiatorMode)];
                radiatorModeField.guiActive = fnRadiator != null;
                radiatorModeField.guiActiveEditor = fnRadiator != null;
            }

            if (state == StartState.Editor) return;

            _windowPosition = new Rect(windowPositionX, windowPositionY, LabelWidth * 2 + ValueWidthWide * 1 + ValueWidthNormal * 10, 100);

            // create the id for the GUI window
            _windowId = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);

            localStar = GetCurrentStar();

            // compensate for stock solar initialization heating bug
            _initializationCountdown = 10;

            if (forceActivateAtStartup)
            {
                Debug.Log("[KSPI]: BeamedPowerReceiver on " + part.name + " was Force Activated");
                part.force_activate();
            }

            if (isThermalReceiverSlave)
            {
                var result = PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part, (s) => s is BeamedPowerReceiver && (BeamedPowerReceiver) s != this, connectStackdepth, connectParentdepth, connectSurfacedepth, true);

                if (result?.Source == null)
                    Debug.LogWarning("[KSPI]: MicrowavePowerReceiver - BreadthFirstSearchForThermalSource-Failed to find thermal receiver");
                else
                    ((BeamedPowerReceiver) result.Source).RegisterAsSlave(this);
            }

            if (maintainResourceBuffers)
            {
                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier * wasteHeatModifier, 2.0e+5));
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ThermalPowerInMegawatt, thermalPowerBufferMult));
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricPowerInMegawatt));
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ThermalPowerInMegawatt, StableMaximumReactorPower);
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, StableMaximumReactorPower);

                if (!Kerbalism.IsLoaded)
                {
                    _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricPowerInKilowatt, 100.0));
                    _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, StableMaximumReactorPower);
                }

                _resourceBuffers.Init(part);
            }

            // look for any transmitter partModule
            partTransmitter = part.FindModuleImplementing<BeamedPowerTransmitter>();
            if (partTransmitter != null)
                _hasTransmitter = true;

            deployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            if (deployableRadiator != null)
            {
                try
                {
                    deployableRadiator.Events["Extend"].guiActive = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: Error while disabling radiator button " + e.Message + " at " + e.StackTrace);
                }
            }

            if (!string.IsNullOrEmpty(animTName))
            {
                animT = part.FindModelAnimators(animTName).FirstOrDefault();
                if (animT != null)
                {
                    animT[animTName].enabled = true;
                    animT[animTName].layer = 1;
                    animT[animTName].normalizedTime = 0;
                    animT[animTName].speed = 0.001f;

                    animT.Sample();
                }
            }

            genericAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().FirstOrDefault(m => m.animationName == animName);
        }

        private void HideSlavePartModules()
        {
            if (deployableSolarPanel == null) return;

            var extendEvent = deployableSolarPanel.Events[nameof(ModuleDeployableSolarPanel.Extend)];
            extendEvent.guiActive = false;
            extendEvent.guiActive = false;

            var retractEvent = deployableSolarPanel.Events[nameof(ModuleDeployableSolarPanel.Retract)];
            retractEvent.guiActive = false;
            retractEvent.guiActive = false;
        }

        private void UpdateBuffers()
        {
            try
            {
                _powerDownFraction = 1;
                _powerState = PowerStates.PowerOnline;

                if (!maintainResourceBuffers) return;

                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ThermalPowerInMegawatt, StableMaximumReactorPower);
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, StableMaximumReactorPower);
                if (!Kerbalism.IsLoaded)
                    _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, StableMaximumReactorPower);
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
                _resourceBuffers.UpdateBuffers();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: MicrowavePowerReceiver.UpdateBuffers " + e.Message);
            }
        }

        private void PowerDown()
        {
            if (_powerState == PowerStates.PowerOffline) return;

            if (_powerDownFraction > 0)
                _powerDownFraction -= 0.01;

            if (_powerDownFraction <= 0)
                _powerState = PowerStates.PowerOffline;

            if (!maintainResourceBuffers) return;

            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ThermalPowerInMegawatt, StableMaximumReactorPower * _powerDownFraction);
            _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInMegawatt, StableMaximumReactorPower * _powerDownFraction);

            _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, part.mass);
            if (!Kerbalism.IsLoaded)
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, StableMaximumReactorPower * _powerDownFraction);
            _resourceBuffers.UpdateBuffers();
        }

        /// <summary>
        /// Event handler called when part is attached to another part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI]: attach " + part.partInfo.title);
                foreach (var node in part.attachNodes)
                {
                    if (node.attachedPart == null) continue;

                    var generator = node.attachedPart.FindModuleImplementing<FNGenerator>();
                    if (generator != null)
                        generator.FindAndAttachToPowerSource();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: MicrowavePowerReceiver.OnEditorAttach " + e.Message);
            }
        }

        /// <summary>
        /// Event handler called when part is detached from vessel
        /// </summary>
        private void OnEditorDetach()
        {
            try
            {
                Debug.Log("[KSPI]: detach " + part.partInfo.title);

                ConnectedChargedParticleElectricGenerator?.FindAndAttachToPowerSource();
                ConnectedThermalElectricGenerator?.FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Reactor.OnEditorDetach " + e.Message);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            part.temperature = storedTemp;
            part.skinTemperature = storedTemp;
        }

        private void InitializeThermalModeSwitcher()
        {
            // ensure valid values
            if (isThermalReceiver && !isEnergyReceiver)
                thermalMode = true;
            else if (!isThermalReceiver && isEnergyReceiver)
                thermalMode = false;

            var isInThermalModeField = Fields[nameof(thermalMode)];

            isInThermalModeField.guiActive = isThermalReceiver && isEnergyReceiver;
            isInThermalModeField.guiActiveEditor = isThermalReceiver && isEnergyReceiver;
        }

        private void InitializeBandwidthSelector()
        {
            Debug.Log("[KSPI]: Setup Receiver BandWidth Configurations for " + part.partInfo.title);

            _maximumWavelengthField = Fields[nameof(maximumWavelength)];
            _minimumWavelengthField = Fields[nameof(minimumWavelength)];
            _solarFacingFactorField = Fields[nameof(solarFacingFactor)];
            _linkedForRelayField = Fields[nameof(linkedForRelay)];
            _slavesAmountField = Fields[nameof(slavesAmount)];
            _thermalPowerField = Fields[nameof(ThermalPower)];
            _minimumConsumptionPercentageField = Fields[nameof(minimumConsumptionPercentage)];
            _maximumConsumptionPercentageField = Fields[nameof(maximumConsumptionPercentage)];
            _beamedpowerField = Fields[nameof(beamedpower)];
            _solarFluxField = Fields[nameof(solarFlux)];

            _connectedsatsField = Fields[nameof(connectedsats)];
            _connectedrelaysField = Fields[nameof(connectedrelays)];
            _networkDepthStringField = Fields[nameof(networkDepthString)];

            _bandWidthNameField = Fields[nameof(bandWidthName)];
            _bandWidthNameField.guiActiveEditor = showBandWidthName || !canSwitchBandwidthInEditor;
            _bandWidthNameField.guiActive = showBandWidthName || !canSwitchBandwidthInFlight;

            _selectedBandwidthConfigurationField = Fields[nameof(selectedBandwidthConfiguration)];
            _selectedBandwidthConfigurationField.guiActiveEditor = showSelectedBandwidthConfiguration && canSwitchBandwidthInEditor;
            _selectedBandwidthConfigurationField.guiActive = showSelectedBandwidthConfiguration && canSwitchBandwidthInFlight;

            var names = BandwidthConverters.Select(m => m.bandwidthName).ToArray();

            if (_selectedBandwidthConfigurationField.uiControlEditor is UI_ChooseOption chooseOptionEditor)
            {
                chooseOptionEditor.options = names;
                chooseOptionEditor.onFieldChanged = UpdateFromGui;
            }

            if (_selectedBandwidthConfigurationField.uiControlFlight is UI_ChooseOption chooseOptionFlight)
            {
                chooseOptionFlight.options = names;
                chooseOptionFlight.onFieldChanged = UpdateFromGui;
            }

            UpdateFromGui(_selectedBandwidthConfigurationField, selectedBandwidthConfiguration);
        }

        private void LoadInitialConfiguration()
        {
            _isLoaded = true;

            var currentWavelength = targetWavelength != 0 ? targetWavelength : 1;

            Debug.Log("[KSPI]: LoadInitialConfiguration initialize initial beam configuration with wavelength target " + currentWavelength);

            // find wavelength closes to target wavelength
            activeBandwidthConfiguration = BandwidthConverters.First();
            bandWidthName = activeBandwidthConfiguration.bandwidthName;
            selectedBandwidthConfiguration = 0;
            var lowestWavelengthDifference = Math.Abs(currentWavelength - activeBandwidthConfiguration.TargetWavelength);

            if (!BandwidthConverters.Any()) return;

            foreach (var currentConfig in BandwidthConverters)
            {
                var configWaveLengthDifference = Math.Abs(currentWavelength - currentConfig.TargetWavelength);

                if (!(configWaveLengthDifference < lowestWavelengthDifference)) continue;

                activeBandwidthConfiguration = currentConfig;
                lowestWavelengthDifference = configWaveLengthDifference;
                selectedBandwidthConfiguration = BandwidthConverters.IndexOf(currentConfig);
                bandWidthName = activeBandwidthConfiguration.bandwidthName;
            }
        }

        private void UpdateFromGui(BaseField field, object oldFieldValueObj)
        {
            if (!BandwidthConverters.Any())
                return;

            if (_isLoaded == false)
                LoadInitialConfiguration();
            else
            {
                if (selectedBandwidthConfiguration < BandwidthConverters.Count)
                    activeBandwidthConfiguration = BandwidthConverters[selectedBandwidthConfiguration];
                else
                {
                    selectedBandwidthConfiguration = BandwidthConverters.Count - 1;
                    activeBandwidthConfiguration = BandwidthConverters.Last();
                }
            }

            if (activeBandwidthConfiguration == null)
                Debug.LogWarning("[KSPI]: BeamedPowerReceiver UpdateFromGUI failed to find BandwidthConfiguration");
            else
                UpdateProperties();
        }

        private void UpdateProperties()
        {
            targetWavelength = activeBandwidthConfiguration.TargetWavelength;
            bandWidthName = activeBandwidthConfiguration.bandwidthName;

            // update wavelength we can receive
            if (canSwitchBandwidthInEditor)
            {
                minimumWavelength = activeBandwidthConfiguration.minimumWavelength;
                maximumWavelength = activeBandwidthConfiguration.maximumWavelength;
            }
            else
            {
                minimumWavelength = BandwidthConverters.Min(m => m.minimumWavelength);
                maximumWavelength = BandwidthConverters.Max(m => m.maximumWavelength);
            }
        }

        public bool CanBeActiveInAtmosphere => DetermineCanBeActiveInAtmosphere(CalculateDynamicPressure());

        public bool DetermineCanBeActiveInAtmosphere(double dynamicPressure)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (deployableAntenna != null && deployableAntenna.isBreakable)
                    return !deployableAntenna.ShouldBreakFromPressure();

                if (deployableRadiator != null && deployableRadiator.isBreakable)
                    return !deployableRadiator.ShouldBreakFromPressure();

                if (deployableSolarPanel != null && deployableSolarPanel.isBreakable)
                    return !deployableSolarPanel.ShouldBreakFromPressure();
            }

            if (genericAnimation == null)
                return true;

            if (dynamicPressure <= 0) return true;

            var pressureLoad = dynamicPressure / 1.4854428818159e-3 * 100;

            return !(pressureLoad > 100 * atmosphereToleranceModifier);
        }

        public double CalculateDynamicPressure()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return 0;

            var pressure = FlightGlobals.getStaticPressure(vessel.GetVesselPos()) / 100;
            var dynamicPressure = 0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325;
            return dynamicPressure;
        }

        protected CelestialBody GetCurrentStar()
        {
            var depth = 0;
            var star = FlightGlobals.currentMainBody;
            while ((depth < 10) && (star.GetTemperature(0) < 2000))
            {
                star = star.referenceBody;
                depth++;
            }

            if ((star.GetTemperature(0) < 2000) || (star.name == "Galactic Core"))
                star = null;

            return star;
        }

        public void Update()
        {
            maximumReceiverPowerCapacity = MaximumReceiverPowerCapacity;
            var transmitterOn = _hasTransmitter && (partTransmitter.IsEnabled || partTransmitter.relay);
            var canBeActive = CanBeActiveInAtmosphere;

            _linkReceiverBaseEvent.active = canLinkup && !linkedForRelay && !receiverIsEnabled && !transmitterOn && canBeActive;
            _unlinkReceiverBaseEvent.active = linkedForRelay;

            _activateReceiverBaseEvent.active = !linkedForRelay && !receiverIsEnabled && !transmitterOn && canBeActive;
            _disableReceiverBaseEvent.active = receiverIsEnabled;

            var isNotRelayingOrTransmitting = !linkedForRelay && !transmitterOn;

            _beamedpowerField.guiActive = isNotRelayingOrTransmitting;
            _linkedForRelayField.guiActive = canLinkup && isNotRelayingOrTransmitting;

            _slavesAmountField.guiActive = thermalMode && slavesAmount > 0;
            _thermalPowerField.guiActive = isThermalReceiverSlave || thermalMode;

            _maximumConsumptionPercentageField.guiActive = receiverIsEnabled;
            _minimumConsumptionPercentageField.guiActive = receiverIsEnabled;
            _minimumWavelengthField.guiActive = receiverIsEnabled;
            _maximumWavelengthField.guiActive = receiverIsEnabled;

            HideSlavePartModules();
        }

        public override void OnUpdate()
        {
            _connectedsatsField.guiActive = _connectedsatsi > 0;
            _connectedrelaysField.guiActive = _connectedrelaysi > 0;
            _networkDepthStringField.guiActive = _networkDepth > 0;

            _solarFacingFactorField.guiActive = solarReceptionSurfaceArea > 0;
            _solarFluxField.guiActive = solarReceptionSurfaceArea > 0;

            _selectedBandwidthConfigurationField.guiActive = (CheatOptions.NonStrictAttachmentOrientation || canSwitchBandwidthInFlight) && receiverIsEnabled; ;

            if (IsThermalSource)
                coreTempererature = CoreTemperature.ToString("0.0") + " K";

            beamedpower = receiverIsEnabled ? PluginHelper.GetFormattedPowerString(ProducedPower) : Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_BeamPowerOffline");

            connectedsats = $"{_connectedsatsi}/{BeamedPowerSources.instance.globalTransmitters.Count}";
            connectedrelays = $"{_connectedrelaysi}/{BeamedPowerSources.instance.globalRelays.Count}";
            networkDepthString = _networkDepth.ToString();

            CalculateInputPower();

            if (deployableAntenna != null)
            {
                deployableAntenna.Events[nameof(deployableAntenna.Extend)].guiActive = false;
                deployableAntenna.Events[nameof(deployableAntenna.Retract)].guiActive = false;
            }
        }

        private double GetSolarFacingFactor(CelestialBody star, Vector3 vesselPosition)
        {
            if (star == null) return 0;

            Vector3d solarDirectionVector = (star.transform.position - vesselPosition).normalized;

            if (receiverType == 9)
                return 1;
            else if (receiverType == 3)
                return Math.Max(0, 1 - Vector3d.Dot(part.transform.forward, solarDirectionVector)) / 2;
            else
                return Math.Max(0, Vector3d.Dot(part.transform.up, solarDirectionVector));
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsEditor && !part.enabled)
                base.OnFixedUpdate();
        }

        private void OnGUI()
        {
            if (vessel == FlightGlobals.ActiveVessel && showWindow && !HighLogic.LoadedSceneIsEditor)
                _windowPosition = GUILayout.Window(_windowId, _windowPosition, DrawGui, Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_InterfaceWindowTitle"));//"Power Receiver Interface"
        }

        private void DrawGui(int window)
        {
            windowPositionX = _windowPosition.x;
            windowPositionY = _windowPosition.y;

            InitializeStyles();

            if (GUI.Button(new Rect(_windowPosition.width - 20, 2, 18, 18), "x"))
                showWindow = false;

            GUILayout.BeginVertical();

            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel1"), part.partInfo.title, _boldBlackStyle, _textBlackStyle, 200, 400);//"Receiver Type"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel2"), diameter.ToString("F2"), _boldBlackStyle, _textBlackStyle, 200, 400);//"Receiver Diameter"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel3"), part.vessel.mainBody.name + " @ " + DistanceToText(part.vessel.altitude), _boldBlackStyle, _textBlackStyle, 200, 400);//"Receiver Location"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel4"), powerCapacityEfficiency.ToString("P1"), _boldBlackStyle, _textBlackStyle, 200, 400);//"Power Capacity Efficiency"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel5"), PluginHelper.GetFormattedPowerString(total_beamed_power), _boldBlackStyle, _textBlackStyle, 200, 400);//"Total Current Beamed Power"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel6"), PluginHelper.GetFormattedPowerString(total_beamed_power_max), _boldBlackStyle, _textBlackStyle, 200, 400);//"Total Maximum Beamed Power"
            PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel7"), PluginHelper.GetFormattedPowerString(total_beamed_wasteheat), _boldBlackStyle, _textBlackStyle, 200, 400);//"Total Wasteheat Production"

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel8"), _boldBlackStyle, GUILayout.Width(LabelWidth));//"Transmitter"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel9"), _boldBlackStyle, GUILayout.Width(LabelWidth));//"Location"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel10"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Aperture"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel11"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Facing"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel12"), _boldBlackStyle, GUILayout.Width(ValueWidthWide));//"Transmit Power"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel13"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Distance"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel14"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Spotsize"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel15"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Wavelength"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel16"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Network Power"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel17"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Available Power"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel18"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Consumed Power"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel19"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Network Efficiency"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel20"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Receiver Efficiency"
            GUILayout.EndHorizontal();

            foreach (ReceivedPowerData receivedPowerData in _receivedPower.Values)
            {
                if (receivedPowerData.Wavelengths == string.Empty)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(receivedPowerData.Transmitter.Vessel.name, _textBlackStyle, GUILayout.Width(LabelWidth));
                GUILayout.Label(receivedPowerData.Transmitter.Vessel.mainBody.name + " @ " + DistanceToText(receivedPowerData.Transmitter.Vessel.altitude), _textBlackStyle, GUILayout.Width(LabelWidth));
                GUILayout.Label((receivedPowerData.Transmitter.Aperture).ToString("##.######") + " m", _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(receivedPowerData.Route.FacingFactor.ToString("P3"), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(PluginHelper.GetFormattedPowerString(receivedPowerData.TransmitPower), _textBlackStyle, GUILayout.Width(ValueWidthWide));
                GUILayout.Label(DistanceToText(receivedPowerData.Route.Distance), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(SpotsizeToText(receivedPowerData.Route.Spotsize), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(receivedPowerData.Wavelengths, _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(PluginHelper.GetFormattedPowerString(receivedPowerData.NetworkPower), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(PluginHelper.GetFormattedPowerString(receivedPowerData.AvailablePower), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(PluginHelper.GetFormattedPowerString(receivedPowerData.ConsumedPower), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label(receivedPowerData.Route.Efficiency.ToString("P2"), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.Label((receivedPowerData.ReceiverEfficiency * 0.01).ToString("P1"), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                GUILayout.EndHorizontal();
            }

            if (_receivedPower.Values.Any(m => m.Relays.Count > 0))
            {
                PrintToGuiLayout(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel21"), "", _boldBlackStyle, _textBlackStyle, 200);//"Relays"

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel22"), _boldBlackStyle, GUILayout.Width(WideLabelWidth));//"Transmitter"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel23"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Relay Nr"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel24"), _boldBlackStyle, GUILayout.Width(WideLabelWidth));//"Relay Name"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel25"), _boldBlackStyle, GUILayout.Width(LabelWidth));//"Relay Location"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel26"), _boldBlackStyle, GUILayout.Width(ValueWidthWide));//"Max Capacity"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_WinLabel27"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Aperture"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_Diameter"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Diameter"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_MinimumWavelength"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Min Wavelength"
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_MaximumWavelength"), _boldBlackStyle, GUILayout.Width(ValueWidthNormal));//"Max Wavelength"
                GUILayout.EndHorizontal();

                foreach (ReceivedPowerData receivedPowerData in _receivedPower.Values)
                {
                    for (int r = 0; r < receivedPowerData.Relays.Count; r++)
                    {
                        VesselRelayPersistence vesselPersistence = receivedPowerData.Relays[r];

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(r == 0 ? receivedPowerData.Transmitter.Vessel.name : "", _textBlackStyle, GUILayout.Width(WideLabelWidth));
                        GUILayout.Label(r.ToString(), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                        GUILayout.Label(vesselPersistence.Vessel.name, _textBlackStyle, GUILayout.Width(WideLabelWidth));
                        GUILayout.Label(vesselPersistence.Vessel.mainBody.name + " @ " + DistanceToText(vesselPersistence.Vessel.altitude), _textBlackStyle, GUILayout.Width(LabelWidth));
                        GUILayout.Label(PluginHelper.GetFormattedPowerString(vesselPersistence.PowerCapacity), _textBlackStyle, GUILayout.Width(ValueWidthWide));
                        GUILayout.Label(vesselPersistence.Aperture + " m", _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                        GUILayout.Label(vesselPersistence.Diameter + " m", _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                        GUILayout.Label(WavelengthToText(vesselPersistence.MinimumRelayWavelenght), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                        GUILayout.Label(WavelengthToText(vesselPersistence.MaximumRelayWavelenght), _textBlackStyle, GUILayout.Width(ValueWidthNormal));
                        GUILayout.EndHorizontal();
                    }
                }
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
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

        protected void PrintToGuiLayout(string label, string value, GUIStyle boldStyle, GUIStyle textStyle, int widthLabel = 130, int widthValue = 130)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, boldStyle, GUILayout.Width(widthLabel));
            GUILayout.Label(value, textStyle, GUILayout.Width(widthValue));
            GUILayout.EndHorizontal();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            RequestedThermalThrottle = _connectedEngines.Any(m => m.RequiresThermalHeat) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.RequestedThrottle)) : 0;
            RequestedPlasmaThrottle = _connectedEngines.Any(m => m.RequiresPlasmaHeat) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.RequestedThrottle)) : 0;
            RequestedChargedThrottle = _connectedEngines.Any(m => m.RequiresChargedPower) ? Math.Min(1, _connectedEngines.Where(m => m.RequiresPlasmaHeat).Sum(e => e.RequestedThrottle)) : 0;

            powerCapacityEfficiency = PowerCapacityEfficiency;

            StoreGeneratorRequests();

            wasteheatRatio = CheatOptions.IgnoreMaxTemperature ? 0 : GetResourceBarRatio(ResourceSettings.Config.WasteHeatInMegawatt);

            CalculateThermalSolarPower();

            if (isMirror && receiverIsEnabled)
            {
                var thermalMassPerKilogram = part.mass * part.thermalMassModifier * PhysicsGlobals.StandardSpecificHeatCapacity * 1e-3;
                dissipationInMegaJoules = PluginHelper.GetBlackBodyDissipation(solarReceptionSurfaceArea, part.temperature) * 1e-6; ;
                var temperatureChange = fixedDeltaTime * -(dissipationInMegaJoules / thermalMassPerKilogram);
                part.temperature = part.temperature + temperatureChange;
            }

            if (_initializationCountdown > 0)
            {
                _initializationCountdown--;

                part.temperature = storedTemp;
                part.skinTemperature = storedTemp;
            }
            else
                storedTemp = part.temperature;

            if (receiverIsEnabled && radiatorMode)
            {
                if (fnRadiator != null)
                {
                    fnRadiator.canRadiateHeat = true;
                    fnRadiator.radiatorIsEnabled = true;
                }
                PowerDown();
                return;
            }

            if (fnRadiator != null)
            {
                fnRadiator.canRadiateHeat = false;
                fnRadiator.radiatorIsEnabled = false;
            }

            try
            {
                if (restartCounter > 0)
                {
                    restartCounter--;
                    RemoveOtherVesselData();
                    OnUpdate();
                }

                UpdatePowerInput();

                if (receiverIsEnabled && !radiatorMode)
                {
                    if (wasteheatRatio >= 0.95 && !isThermalReceiver && !solarPowerMode )
                    {
                        receiverIsEnabled = false;
                        _deactivateTimer++;
                        if (FlightGlobals.ActiveVessel == vessel && _deactivateTimer > 2)
                            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_OverheatingMsg"), 5f, ScreenMessageStyle.UPPER_CENTER);//"Warning Dangerous Overheating Detected: Emergency beam power shutdown occuring NOW!"
                        PowerDown();
                        return;
                    }

                    // add alternator power
                    AddAlternatorPower();

                    // update energy buffers
                    UpdateBuffers();

                    if (isThermalReceiverSlave || thermalMode)
                    {
                        slavesAmount = thermalReceiverSlaves.Count;
                        slavesPower = thermalReceiverSlaves.Sum(m => m.total_thermal_power_provided);

                        total_thermal_power_available = thermalSolarInputMegajoules + total_beamed_power + slavesPower;
                        total_thermal_power_provided = Math.Min(MaximumReceiverPowerCapacity, total_thermal_power_available);
                        total_thermal_power_provided_max = Math.Min(MaximumReceiverPowerCapacity, total_beamed_power_max + thermalSolarInputMegajoulesMax);

                        if (!isThermalReceiverSlave && total_thermal_power_provided > 0)
                        {
                            var thermalEngineThrottleRatio = _connectedEngines.Any(m => !m.RequiresChargedPower) ? _connectedEngines.Where(m => !m.RequiresChargedPower).Max(e => e.CurrentThrottle) : 0;
                            var minimumRatio =  Math.Max(minimumConsumptionPercentage / 100d, Math.Max(storedGeneratorThermalEnergyRequestRatio, thermalEngineThrottleRatio));

                            var powerGeneratedResult = ManagedPowerSupplyPerSecondMinimumRatio(total_thermal_power_provided, total_thermal_power_provided_max, minimumRatio, ResourceSettings.Config.ThermalPowerInMegawatt);

                            if (!CheatOptions.IgnoreMaxTemperature)
                            {
                                var supplyRatio = powerGeneratedResult.CurrentSupply / total_thermal_power_provided;
                                var finalThermalWasteheat = powerGeneratedResult.CurrentSupply + supplyRatio * _totalConversionWasteHeatProduction;

                                SupplyFnResourcePerSecondWithMax(finalThermalWasteheat, total_thermal_power_provided_max, ResourceSettings.Config.WasteHeatInMegawatt);
                            }

                            var thermalPowerRatio = total_thermal_power_available > 0 ? powerGeneratedResult.CurrentSupply / total_thermal_power_available : 0;

                            foreach (var item in _receivedPower)
                            {
                                item.Value.ConsumedPower = item.Value.AvailablePower * thermalPowerRatio;
                            }

                            foreach (var slave in thermalReceiverSlaves)
                            {
                                foreach (var item in slave._receivedPower)
                                {
                                    item.Value.ConsumedPower = item.Value.AvailablePower * thermalPowerRatio;
                                }
                            }
                        }

                        if (animT != null)
                        {
                            animT[animTName].normalizedTime = maximumReceiverPowerCapacity > 0 ? (float)Math.Min(total_thermal_power_provided / maximumReceiverPowerCapacity, 1) : 0;
                            animT.Sample();
                        }

                        ThermalPower = ThermalPower <= 0
                            ? total_thermal_power_provided
                            : total_thermal_power_provided * GameConstants.microwave_alpha + GameConstants.microwave_beta * ThermalPower;
                    }
                    else
                    {
                        wasteheatElectricConversionEfficiency = WasteheatElectricConversionEfficiency;
                        effectiveSolarThermalElectricEfficiency = wasteheatElectricConversionEfficiency * solarElectricEfficiency;
                        effectiveBeamedPowerElectricEfficiency = wasteheatElectricConversionEfficiency * electricMaxEfficiency;

                        var totalBeamedElectricPowerAvailable = thermalSolarInputMegajoules * effectiveSolarThermalElectricEfficiency + total_beamed_power * effectiveBeamedPowerElectricEfficiency;
                        var totalBeamedElectricPowerProvided = Math.Min(MaximumReceiverPowerCapacity, totalBeamedElectricPowerAvailable);

                        if (!(totalBeamedElectricPowerProvided > 0)) return;

                        var minimumRequestedPower = MaximumReceiverPowerCapacity * (minimumConsumptionPercentage / 100d);
                        var calculatedMinimumRatio = Math.Min(1, minimumRequestedPower / totalBeamedElectricPowerProvided);

                        var powerGeneratedResult = ManagedPowerSupplyPerSecondMinimumRatio(totalBeamedElectricPowerProvided, totalBeamedElectricPowerProvided, calculatedMinimumRatio, ResourceSettings.Config.ElectricPowerInMegawatt);
                        var supplyRatio = powerGeneratedResult.CurrentProvided / totalBeamedElectricPowerProvided;

                        // only generate wasteheat from beamed power when actually using the energy
                        if (!CheatOptions.IgnoreMaxTemperature)
                        {
                            var solarWasteheat = thermalSolarInputMegajoules * (1 - effectiveSolarThermalElectricEfficiency);
                            SupplyFnResourcePerSecond(supplyRatio * _totalConversionWasteHeatProduction + supplyRatio * solarWasteheat, ResourceSettings.Config.WasteHeatInMegawatt);
                        }

                        foreach (var item in _receivedPower)
                        {
                            item.Value.ConsumedPower = item.Value.AvailablePower * supplyRatio;
                        }
                    }
                }
                else
                {
                    total_thermal_power_provided = 0;
                    total_sat_efficiency_fraction = 0;
                    total_beamed_power = 0;
                    total_beamed_power_max = 0;
                    total_beamed_wasteheat = 0;

                    powerInputMegajoules = 0;
                    powerInputMegajoulesMax = 0;

                    thermalSolarInputMegajoules = 0;
                    thermalSolarInputMegajoulesMax = 0;

                    solarFacingFactor = 0;
                    ThermalPower = 0;

                    PowerDown();

                    if (stockModuleGenerator != null)
                        stockModuleGenerator.generatorIsActive = false;

                    if (animT == null) return;

                    animT[animTName].normalizedTime = 0;
                    animT.Sample();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in MicrowavePowerReceiver.OnFixedUpdateResourceSuppliable " + e.Message + " at " + e.StackTrace);
            }
        }

        private void StoreGeneratorRequests()
        {
            storedIsThermalEnergyGeneratorEfficiency = currentIsThermalEnergyGeneratorEfficiency;
            currentIsThermalEnergyGeneratorEfficiency = 0;

            storedGeneratorThermalEnergyRequestRatio = Math.Min(1, currentGeneratorThermalEnergyRequestRatio);
            currentGeneratorThermalEnergyRequestRatio = 0;
        }

        // Is called during OnUpdate to reduce processor load
        private void CalculateInputPower()
        {
            _totalConversionWasteHeatProduction = 0;

            if (wasteheatRatio >= 0.95 && !isThermalReceiver) return;

            // reset all output variables at start of loop
            total_beamed_power = 0;
            total_beamed_power_max = 0;
            total_sat_efficiency_fraction = 0;
            total_beamed_wasteheat = 0;
            _connectedsatsi = 0;
            _connectedrelaysi = 0;
            _networkDepth = 0;
            _activeSatsIncr = 0;


            if (!solarPowerMode)
            {
                _deactivateTimer = 0;

                var usedRelays = new HashSet<VesselRelayPersistence>();

                foreach (var beamedPowerData in _receivedPower.Values)
                {
                    beamedPowerData.IsAlive = false;
                }

                //loop all connected beamed power transmitters
                foreach (var connectedTransmitterEntry in InterstellarBeamedPowerHelper.GetConnectedTransmitters(this))
                {
                    var transmitter = connectedTransmitterEntry.Key;

                    if (!_receivedPower.TryGetValue(transmitter.Vessel, out ReceivedPowerData beamedPowerData))
                    {
                        Debug.Log("[KSPI]: Added ReceivedPowerData for " + transmitter.Vessel.name);
                        beamedPowerData = new ReceivedPowerData
                        {
                            Receiver = this,
                            Transmitter = transmitter
                        };
                        _receivedPower[beamedPowerData.Transmitter.Vessel] = beamedPowerData;
                    }

                    // first reset owm received power to get correct amount received by others
                    beamedPowerData.IsAlive = true;
                    beamedPowerData.AvailablePower = 0;
                    beamedPowerData.NetworkPower = 0;
                    beamedPowerData.Wavelengths = string.Empty;
                    beamedPowerData.CurrentRecievedPower = 0;
                    beamedPowerData.MaximumReceivedPower = 0;
                    beamedPowerData.ReceiverEfficiency = 0;

                    KeyValuePair<MicrowaveRoute, IList<VesselRelayPersistence>> keyValuePair = connectedTransmitterEntry.Value;
                    beamedPowerData.Route = keyValuePair.Key;
                    beamedPowerData.Relays = keyValuePair.Value;

                    // convert initial beamed power from source into MegaWatt
                    beamedPowerData.TransmitPower = transmitter.getAvailablePowerInMW();

                    beamedPowerData.NetworkCapacity = beamedPowerData.Relays != null && beamedPowerData.Relays.Count > 0
                        ? Math.Min(beamedPowerData.TransmitPower, beamedPowerData.Relays.Min(m => m.PowerCapacity))
                        : beamedPowerData.TransmitPower;

                    // calculate maximum power available from beamed power network
                    beamedPowerData.PowerUsageOthers = GetEnumeratedPowerFromSatelliteForAllLoadedVessels(beamedPowerData.Transmitter, this);

                    // add to available network power
                    beamedPowerData.NetworkPower = beamedPowerData.NetworkCapacity;

                    // initialize remaining power
                    beamedPowerData.RemainingPower = Math.Max(0, beamedPowerData.NetworkCapacity - beamedPowerData.PowerUsageOthers);

                    foreach (var powerBeam in beamedPowerData.Transmitter.SupportedTransmitWavelengths)
                    {
                        // select active or compatible brandWith Converter
                        var selectedBrandWith = canSwitchBandwidthInEditor
                            ? activeBandwidthConfiguration
                            : BandwidthConverters.FirstOrDefault(m => (powerBeam.wavelength >= m.minimumWavelength && powerBeam.wavelength <= m.maximumWavelength));

                        // skip if no compatible receiver bandwidth found
                        if (selectedBrandWith == null)
                            continue;

                        var maximumRoutePower = (powerBeam.nuclearPower + powerBeam.solarPower) *beamedPowerData.Route.Efficiency * 0.001;

                        // subtract any power already received by other receivers
                        var remainingPowerInBeam = Math.Min(beamedPowerData.RemainingPower, maximumRoutePower);

                        // skip if no power remaining
                        if (remainingPowerInBeam <= 0)
                            continue;

                        // construct displayed wavelength
                        if (beamedPowerData.Wavelengths.Length > 0)
                            beamedPowerData.Wavelengths += ",";
                        beamedPowerData.Wavelengths += WavelengthToText(powerBeam.wavelength);

                        // take into account maximum route capacity
                        var beamNetworkPower = beamedPowerData.Relays != null && beamedPowerData.Relays.Count > 0
                            ? Math.Min(remainingPowerInBeam, beamedPowerData.Relays.Min(m => m.PowerCapacity))
                            : remainingPowerInBeam;

                        // subtract from remaining power
                        beamedPowerData.RemainingPower = Math.Max(0, beamedPowerData.RemainingPower - beamNetworkPower);

                        var currentRecievalPower = Math.Min(maximumReceiverPowerCapacity * PowerRatio, maximumReceiverPowerCapacity * powerCapacityEfficiency);
                        var maximumRecievalPower = maximumReceiverPowerCapacity * powerCapacityEfficiency;

                        // get effective beamtoPower efficiency
                        var efficiencyPercentage = thermalMode
                            ? selectedBrandWith.MaxThermalEfficiencyPercentage
                            : selectedBrandWith.MaxElectricEfficiencyPercentage;

                        // convert to fraction
                        var efficiencyFraction = efficiencyPercentage / 100;

                        // limit by amount of beampower the reciever is able to process
                        var satPower = Math.Min(currentRecievalPower, beamNetworkPower * efficiencyFraction);
                        var satPowerMax = Math.Min(maximumRecievalPower, beamNetworkPower * efficiencyFraction);
                        var satWasteheat = Math.Min(currentRecievalPower, beamNetworkPower * (1 - efficiencyFraction));

                        // calculate wasteheat by power conversion
                        var conversionWasteheat = (thermalMode ? 0.05 : 1) * satPower * (1 - efficiencyFraction);

                        // generate conversion wasteheat
                        _totalConversionWasteHeatProduction += conversionWasteheat; // + missedPowerPowerWasteheat;

                        // register amount of raw power recieved
                        beamedPowerData.CurrentRecievedPower = satPower;
                        beamedPowerData.MaximumReceivedPower = satPowerMax;
                        beamedPowerData.ReceiverEfficiency = efficiencyPercentage;
                        beamedPowerData.AvailablePower = satPower > 0 && efficiencyFraction > 0 ? satPower / efficiencyFraction : 0;

                        // convert raw power into effective power
                        total_beamed_power += satPower;
                        total_beamed_power_max += satPowerMax;
                        total_beamed_wasteheat += satWasteheat;
                        total_sat_efficiency_fraction += satPower * efficiencyFraction;

                        if (!(satPower > 0)) continue;

                        _activeSatsIncr++;

                        if (beamedPowerData.Relays == null) continue;

                        foreach (var relay in beamedPowerData.Relays)
                        {
                            usedRelays.Add(relay);
                        }
                        _networkDepth = Math.Max(_networkDepth, beamedPowerData.Relays.Count);
                    }
                }

                _connectedsatsi = _activeSatsIncr;
                _connectedrelaysi = usedRelays.Count;
            }
            else
            {
                _receivedPower.Clear();
            }

            //remove dead entries
            var deadEntries = _receivedPower.Where(m => !m.Value.IsAlive).ToList();
            foreach(var entry in deadEntries)
            {
                Debug.LogWarning("[KSPI]: Removed received power from " + entry.Key.name);
                _receivedPower.Remove(entry.Key);
            }
        }

        private void UpdatePowerInput()
        {
            beamedPowerQueue.Enqueue(total_beamed_power);
            if (total_beamed_power > 0)
            {
                beamedPowerQueue.Enqueue(total_beamed_power);
                beamedPowerQueue.Dequeue();
            }
            if (beamedPowerQueue.Count > 20)
                beamedPowerQueue.Dequeue();

            beamedPowerMaxQueue.Enqueue(total_beamed_power_max);
            if (total_beamed_power_max > 0)
            {
                beamedPowerMaxQueue.Enqueue(total_beamed_power_max);
                beamedPowerMaxQueue.Dequeue();
            }
            if (beamedPowerMaxQueue.Count > 20)
                beamedPowerMaxQueue.Dequeue();

            total_beamed_power = beamedPowerQueue.Average();
            total_beamed_power_max = beamedPowerMaxQueue.Average();

            powerInputMegajoules = total_beamed_power + thermalSolarInputMegajoules;
            powerInputMegajoulesMax = total_beamed_power_max + thermalSolarInputMegajoulesMax;
        }

        private void CalculateThermalSolarPower()
        {
            if (solarReceptionSurfaceArea <= 0 || solarReceptionEfficiency <= 0)
                return;

            solarFluxQueue.Enqueue(part.vessel.solarFlux);

            if (solarFluxQueue.Count > 50)
                solarFluxQueue.Dequeue();

            solarFlux = solarFluxQueue.Count > 10
                ? solarFluxQueue.OrderBy(m => m).Skip(10).Take(30).Average()
                : solarFluxQueue.Average();

            thermalSolarInputMegajoulesMax = solarReceptionSurfaceArea * (solarFlux / 1e+6) * solarReceptionEfficiency;
            solarFacingFactor = Math.Pow(GetSolarFacingFactor(localStar, part.WCoM), solarFacingExponent);
            thermalSolarInputMegajoules = thermalSolarInputMegajoulesMax * solarFacingFactor;
        }

        private void AddAlternatorPower()
        {
            if (alternatorRatio == 0)
                return;

            if (!receiverIsEnabled || radiatorMode)
                return;

            var alternatorPower = alternatorRatio * powerInputMegajoules * 0.001;

            var alternatorWasteheat = Math.Min(alternatorPower * AverageEfficiencyFraction, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt) * AverageEfficiencyFraction);

            SupplyFnResourcePerSecond(alternatorPower, ResourceSettings.Config.ElectricPowerInMegawatt);
            SupplyFnResourcePerSecond(alternatorWasteheat, ResourceSettings.Config.WasteHeatInMegawatt);

            if (stockModuleGenerator != null)
                stockModuleGenerator.generatorIsActive = alternatorPower > 0;

            if (outputModuleResource == null) return;

            outputModuleResource.rate = alternatorPower * GameConstants.ecPerMJ;
            mockInputResource.rate = alternatorPower * -GameConstants.ecPerMJ;
        }

        public double MaxStableMegaWattPower => isThermalReceiver ? 0 : powerInputMegajoules;

        public virtual double GetCoreTempAtRadiatorTemp(double radTemp)
        {
            return CoreTemperature;
        }

        public double GetThermalPowerAtTemp(double temp)
        {
            return ThermalPower;
        }

        public double Radius => radius;

        public bool isActive()
        {
            return receiverIsEnabled;
        }

        public bool shouldScaleDownJetISP()
        {
            return false;
        }

        public void EnableIfPossible()
        {
            if (!receiverIsEnabled && autoDeploy)
                ActivateRecieverState();
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_BeamPowerReceiver_Getinfo", diameter);//"Diameter: " +  + " m"
        }

        public double GetCurrentReceivedPower(VesselMicrowavePersistence vmp)
        {
            if (receiverIsEnabled && _receivedPower.TryGetValue(vmp.Vessel, out var data))
            {
                return data.CurrentRecievedPower;
            }

            return 0;
        }

        public static double GetEnumeratedPowerFromSatelliteForAllLoadedVessels(VesselMicrowavePersistence vmp, BeamedPowerReceiver currentReceiver)
        {
            double enumeratedPower = 0;
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.isEVA)
                    continue;

                var receivers = vessel.FindPartModulesImplementing<BeamedPowerReceiver>();
                foreach (BeamedPowerReceiver receiver in receivers)
                {
                    if (currentReceiver == receiver)
                        continue;

                    enumeratedPower += receiver.GetCurrentReceivedPower(vmp);
                }
            }
            return enumeratedPower;
        }

        public override int getPowerPriority()
        {
            return 1;
        }

        private string WavelengthToText(double wavelength)
        {
            if (wavelength > 1.0e-3)
                return (wavelength * 1.0e+3) + " mm";
            else if (wavelength > 7.5e-7)
                return (wavelength * 1.0e+6)+ " µm";
            else if (wavelength > 1.0e-9)
                return (wavelength * 1.0e+9) + " nm";
            else
                return (wavelength * 1.0e+12)+ " pm";
        }

        private string DistanceToText(double distance)
        {
            if (distance >= 1.0e+16)
                return (distance / 1.0e+15).ToString("0.00") + " Pm";
            else if (distance >= 1.0e+13)
                return (distance / 1.0e+12).ToString("0.00") + " Tm";
            else if (distance >= 1.0e+10)
                return (distance / 1.0e+9).ToString("0.00") + " Gm";
            else if (distance >= 1.0e+7)
                return (distance / 1.0e+6).ToString("0.00") + " Mm";
            else if (distance >= 1.0e+4)
                return (distance / 1.0e+3).ToString("0.00") + " km";
            else
                return distance.ToString("0") + " m";
        }

        private string SpotsizeToText(double spotsize)
        {
            if (spotsize > 1.0e+3)
                return (spotsize * 1.0e-3).ToString("0.000") + " km";
            else if (spotsize > 1)
                return spotsize.ToString("0.00") + " m";
            else
                return (spotsize * 1.0e+3).ToString("0") + " mm";
        }

        private void ConnectToModuleGenerator()
        {
            stockModuleGenerator = part.FindModuleImplementing<ModuleGenerator>();

            if (stockModuleGenerator == null)
                return;

            outputModuleResource = stockModuleGenerator.resHandler.outputResources.FirstOrDefault(m => m.name == ResourceSettings.Config.ElectricPowerInKilowatt);

            if (outputModuleResource == null) return;

            moduleGeneratorShutdownBaseEvent = stockModuleGenerator.Events[nameof(ModuleGenerator.Shutdown)];
            if (moduleGeneratorShutdownBaseEvent != null)
            {
                moduleGeneratorShutdownBaseEvent.guiActive = false;
                moduleGeneratorShutdownBaseEvent.guiActiveEditor = false;
            }

            moduleGeneratorActivateBaseEvent = stockModuleGenerator.Events[nameof(ModuleGenerator.Activate)];
            if (moduleGeneratorActivateBaseEvent != null)
            {
                moduleGeneratorActivateBaseEvent.guiActive = false;
                moduleGeneratorActivateBaseEvent.guiActiveEditor = false;
            }

            moduleGeneratorEfficientBaseField = stockModuleGenerator.Fields[nameof(ModuleGenerator.efficiency)];
            if (moduleGeneratorEfficientBaseField != null)
            {
                moduleGeneratorEfficientBaseField.guiActive = false;
                moduleGeneratorEfficientBaseField.guiActiveEditor = false;
            }

            initialGeneratorPowerEC = outputModuleResource.rate;

            mockInputResource = new ModuleResource
            {
                name = outputModuleResource.name, id = outputModuleResource.name.GetHashCode()
            };

            stockModuleGenerator.resHandler.inputResources.Add(mockInputResource);
        }
    }
}
