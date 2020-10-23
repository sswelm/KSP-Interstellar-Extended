﻿using FNPlugin;
using FNPlugin.Beamedpower;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;

namespace PhotonSail
{
    class BeamEffect
    {
        public GameObject solar_effect;
        public Renderer solar_effect_renderer;
        public Collider solar_effect_collider;
    }

    class ReceivedBeamedPower
    {
        public double receivedPower;
        public double pitchAngle;
        public double spotsize;
        public double cosConeAngle;
    }

    class BeamRay
    {
        public double energyInGigaWatt;
        public double spotsize;
        public double cosConeAngle;
        public Vector3d powerSourceToVesselVector;
    }

    public class ModulePhotonSail : PartModule, IBeamedPowerReceiver, IPartMassModifier, IRescalable<ModulePhotonSail>
    {
        // Persistent Variables
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public double previousPeA;
        [KSPField(isPersistant = true)]
        public double previousAeA;
        [KSPField(isPersistant = true)]
        public float previousFixedDeltaTime;
        [KSPField(isPersistant = true)]
        public double storedMassMultiplier;

        // Persistent False
        [KSPField]
        public double reflectedPhotonRatio = 0.975;
        [KSPField]
        public double backsideEmissivity = 1;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_surfaceArea", guiUnits = " m\xB2", guiFormat = "F0")]//Sail Surface Area
        public double surfaceArea = 144400;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_diameter", guiUnits = " m", guiFormat = "F3")]//Sail Diameter
        public double diameter;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_partMass", guiUnits = " t")]//Sail Mass
        public float partMass;

        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_frontPhotovoltaicArea", guiUnits = " m\xB2", guiFormat = "F5")]//Sail Front Solar Cell Area
        public double frontPhotovoltaicArea = 1;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_backPhotovoltaicArea", guiUnits = " m\xB2", guiFormat = "F5")]//Sail Back Solar Cell Area
        public double backPhotovoltaicArea = 1;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_doorsPhotovoltaicArea", guiUnits = " m\xB2", guiFormat = "F5")]//Doors Solar Cell Area
        public double doorsPhotovoltaicArea = 1;

        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_minimumWavelength", guiUnits = " m")]//Sail Min wavelength
        public double minimumWavelength = 0.000000620;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_maximumWavelength", guiUnits = " m")]//Sail Max wavelength
        public double maximumWavelength = 0.01;

        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_PhotonSail_maxSailHeatDissipationInMegajoules", guiUnits = " MJ/s", guiFormat = "F3")]//Sail Max heat dissipation
        public double maxSailHeatDissipationInMegajoules;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_PhotonSail_currentSailHeatingInMegajoules", guiUnits = " MJ/s", guiFormat = "F3")]//Sail Cur heat dissipation
        public double currentSailHeatingInMegajoules;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_PhotonSail_sailHeatDissipationTemperature", guiUnits = " K", guiFormat = "F3")]//Sail Dissipation temperature
        public double sailHeatDissipationTemperature;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_PhotonSail_absorbedPhotonHeatInWatt", guiUnits = " J/s", guiFormat = "F3")]//Sail Absorbed heat
        public double absorbedPhotonHeatInWatt;

        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_SolarCellTech1")]//Solar Cell Tech 1
        public string solarPhotovoltaicTech1 = "photovoltaicSailUpgradeA";
        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_SolarCellTech2")]//Solar Cell Tech 2
        public string solarPhotovoltaicTech2 = "photovoltaicSailUpgradeB";
        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_SolarCellTech3")]//Solar Cell Tech 3
        public string solarPhotovoltaicTech3 = "photovoltaicSailUpgradeC";

        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_SolarPhotovoltaicEfficiency0", guiUnits = "%")]//Solar Cell Efficiency Mk 0
        public double solarPhotovoltaicEfficiency0 = 10;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_SolarPhotovoltaicEfficiency1", guiUnits = "%")]//Solar Cell Efficiency Mk 1
        public double solarPhotovoltaicEfficiency1 = 15;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_SolarPhotovoltaicEfficiency2", guiUnits = "%")]//Solar Cell Efficiency Mk 2
        public double solarPhotovoltaicEfficiency2 = 20;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_SolarPhotovoltaicEfficiency3", guiUnits = "%")]//Solar Cell Efficiency Mk 3
        public double solarPhotovoltaicEfficiency3 = 25;


        //[KSPField(guiActiveEditor = false, guiActive = false, guiName = "Max Sail irradiance", guiUnits = " W", guiFormat = "F0")]
        //public double maxKscLaserPowerInWatt;
        //[KSPField(guiActiveEditor = true, guiActive = true, guiName = "Max Sail irradiance", guiUnits = " GW", guiFormat = "F3")]
        //public double maxKscLaserPowerInGigaWatt;
        //[KSPField(guiActiveEditor = true, guiActive = true, guiName = "Max Beam irradiance", guiUnits = " GW/m\xB2", guiFormat = "F3")]
        //public double maxKscLaserIrradiance;

        [KSPField]
        public double kscPowerMult = 1e8;
        [KSPField]
        public double kscApertureMult = 1;
        [KSPField]
        public double massTechMultiplier = 1;
        [KSPField]
        public double heatMultiplier = 10;
        [KSPField]
        public float effectSize1 = 1.25f;
        [KSPField]
        public string animName = "";
        [KSPField]
        public float initialAnimationSpeed = 1;
        [KSPField]
        public float initialAnimationTargetWeight = 0.01f;
        [KSPField]
        public double startOfSailOpening = 0.54;

        [KSPField]
        public string kscLaserApertureName1 = "KscApertureUpgradeA";
        [KSPField]
        public string kscLaserApertureName2 = "KscApertureUpgradeB";
        [KSPField]
        public string kscLaserApertureName3 = "KscApertureUpgradeC";
        [KSPField]
        public string kscLaserApertureName4 = "KscApertureUpgradeD";
        [KSPField]
        public string kscLaserApertureName5 = "KscApertureUpgradeE";
        [KSPField]
        public string kscLaserApertureName6 = "KscApertureUpgradeF";
        [KSPField]
        public string kscLaserApertureName7 = "KscApertureUpgradeG";

        [KSPField]
        public string kscPowerUpgdradeName1 = "KscPowerUpgradeA";
        [KSPField]
        public string kscPowerUpgdradeName2 = "KscPowerUpgradeB";
        [KSPField]
        public string kscPowerUpgdradeName3 = "KscPowerUpgradeC";
        [KSPField]
        public string kscPowerUpgdradeName4 = "KscPowerUpgradeD";
        [KSPField]
        public string kscPowerUpgdradeName5 = "KscPowerUpgradeE";
        [KSPField]
        public string kscPowerUpgdradeName6 = "KscPowerUpgradeF";
        [KSPField]
        public string kscPowerUpgdradeName7 = "KscPowerUpgradeG";


        [KSPField]
        public int kscLaserApertureBonus0 = 50;
        [KSPField]
        public int kscLaserApertureBonus1 = 50;
        [KSPField]
        public int kscLaserApertureBonus2 = 100;
        [KSPField]
        public int kscLaserApertureBonus3 = 100;
        [KSPField]
        public int kscLaserApertureBonus4 = 100;
        [KSPField]
        public int kscLaserApertureBonus5 = 200;
        [KSPField]
        public int kscLaserApertureBonus6 = 200;
        [KSPField]
        public int kscLaserApertureBonus7 = 200;

        [KSPField]
        public int kscLaserPowerBonus0 = 50;
        [KSPField]
        public int kscLaserPowerBonus1 = 350;   // 100000
        [KSPField]
        public int kscLaserPowerBonus2 = 600;   // 200000
        [KSPField]
        public int kscLaserPowerBonus3 = 1000;  // 300000
        [KSPField]
        public int kscLaserPowerBonus4 = 2000;  // 500000
        [KSPField]
        public int kscLaserPowerBonus5 = 4000;  // 1000000
        [KSPField]
        public int kscLaserPowerBonus6 = 12000; // 3000000
        [KSPField]
        public int kscLaserPowerBonus7 = 30000; // 6000000

        [KSPField]
        public string massReductionTech1 = "metaMaterials";
        [KSPField]
        public string massReductionTech2 = "orbitalAssembly";
        [KSPField]
        public string massReductionTech3 = "orbitalMegastructures";
        [KSPField]
        public string massReductionTech4 = "exoticAlloys";
        [KSPField]
        public string massReductionTech5 = "nanolathing";

        [KSPField]
        public double massReductionMult1 = 2;
        [KSPField]
        public double massReductionMult2 = 2;
        [KSPField]
        public double massReductionMult3 = 2;
        [KSPField]
        public double massReductionMult4 = 2;
        [KSPField]
        public double massReductionMult5 = 2;

        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "#LOC_PhotonSail_KCSLaserPower", guiUnits = " GW", guiFormat = "F0")]//KCS Laser Power
        public double kscLaserPowerInGigaWatt;
        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_KCSLaserPower", guiUnits = " W", guiFormat = "F0")]//KCS Laser Power
        public double kscLaserPowerInWatt = 5e12;
        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_KSCCentralSpotsizeMult")]//KCS Laser Central Spotsize Mult
        public double kscCentralSpotsizeMult = 2;           // http://breakthroughinitiatives.org/i/docs/170919_bidders_briefing_zoom_room_final.pdf
        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_KSCSideSpotsizeMult")]//KCS Laser Side Spotsize Mult
        public double kscSideSpotsizeMult = 22;
        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_KSCCentralSpotEnergyRatio")]//KCS Laser Central Spot Ratio
        public double kscCentralSpotEnergyRatio = 0.7;    // http://breakthroughinitiatives.org/i/docs/170919_bidders_briefing_zoom_room_final.pdf
        [KSPField(guiActiveEditor = false, guiName = "#LOC_PhotonSail_KSCSideSpotEnergyRatio")]//KCS Laser Side Spot Ratio
        public double kscSideSpotEnergyRatio = 0.25; 
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_KSCLaserMinElevationAngle")]//KCS Laser Min Elevation Angle
        public double kscLaserMinElevationAngle = 70;

        [KSPField] 
        public int animatedRays = 400;
        [KSPField]
        public double kscAtmosphereAbsorbtionRatio = 0.11;
        [KSPField]
        public double kscLaserLatitude = -0.13133150339126601;
        [KSPField]
        public double kscLaserLongitude = -74.594841003417997;
        [KSPField]
        public double kscLaserAltitude = 20;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_KSCLaserAperture", guiUnits = " m")]//KCS Phased Array Aperture
        public double kscLaserAperture = 2000;        // 1 KM is used for starshot
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_KSCLaserWavelength", guiUnits = " m")]//KCS Laser Wavelength
        public double kscLaserWavelength = 1.06e-6; // 1.06 milimeter is used by project starshot
        [KSPField(guiActiveEditor = true, guiName = "#LOC_PhotonSail_KSCPhotonReflectionPercentage", guiFormat = "F5", guiUnits = "%")]//KCS Laser Reflection
        public double kscPhotonReflectionPercentage;

        //0.0000342; // @ 11 micrometer with unprotected coating   https://www.thorlabs.com/newgrouppage9.cfm?objectgroup_id=744

        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_SkinTemperature", guiFormat = "F3", guiUnits = " K°")]//Skin Temperature
        public double skinTemperature;
        [KSPField(guiActive = true, guiName = "#autoLOC_6001421", guiFormat = "F4", guiUnits = " EC/s")]
        public double photovoltaicFlowRate;
        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_PhotovoltalicPotential", guiFormat = "F4", guiUnits = " EC/s")]//photovoltalic Potential
        public double photovoltaicPotential;

        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_ExternalTemperature", guiFormat = "F4", guiUnits = " K°")]//External Temperature
        public double externalTemperature;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_DissipationInMegaJoules", guiFormat = "F4", guiUnits = "#LOC_KSPIE_Reactor_megajouleUnit")]//Current Skin Dissipation
        public double dissipationInMegaJoules;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_totalSolarFluxInWatt", guiFormat = "F3", guiUnits = " W/m\xB2")]//Solar Flux
        public double totalSolarFluxInWatt;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_totalForceInNewtonFromSolarEnergy", guiFormat = "F5", guiUnits = " N")]//Solar Force Max
        public double totalForceInNewtonFromSolarEnergy = 0;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_TotalSolarEnergyReceivedInMJ", guiFormat = "F5", guiUnits = " MJ/s")]//Solar Energy Received
        public double totalSolarEnergyReceivedInMJ;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_solar_force_d", guiFormat = "F5", guiUnits = " N")]//Solar Force Sail
        public double solar_force_d = 0;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_SolarAcc")]//Solar Acceleration
        public string solarAcc;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_SolarSailAngle", guiFormat = "F3", guiUnits = "°")]//Solar Pitch Angle
        public double solarSailAngle = 0;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_SolarfluxWasteheatInMegaJoules", guiFormat = "F3", guiUnits = " MJ/s")]//Solar Energy Absorbed
        public double solarfluxWasteheatInMegaJoules;

        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_maxNetworkPower", guiFormat = "F4", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Network power
        public double maxNetworkPower;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_PhotonSail_KSCBeamedPowerThrottle", guiUnits = "%"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = 0, requireFullControl = false)]//KCS Power Throttle
        public float kcsBeamedPowerThrottle = 0;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_PhotonSail_BeamedPowerThrottle", guiUnits = "%"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = 0, requireFullControl = false)]//Beamed Power Throttle
        public float beamedPowerThrottle = 0;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_PhotonSail_BeamedPowerForwardDirection"), UI_Toggle(disabledText = "#LOC_PhotonSail_Backward", enabledText = "#LOC_PhotonSail_Forward", requireFullControl = false)]//Beamed Push Direction-Backward-Forward
        public bool beamedPowerForwardDirection = true;

        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_AvailableBeamedKscEnergy", guiFormat = "F2", guiUnits = " W")]//Energy Available from KSC
        public double availableBeamedKscEnergy;
        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_ReceivedBeamedPowerFromKSC", guiFormat = "F2", guiUnits = " W")]//Energy Received from KSC
        public double receivedBeamedPowerFromKsc;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_KSCLaserElevationAngle", guiFormat = "F2", guiUnits = "°")]//KSC Laser Elevation Angle
        public double kscLaserElevationAngle;

        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_TotalReceivedBeamedPower", guiFormat = "F4", guiUnits = " MJ/s")]//Beamed Energy
        public double totalReceivedBeamedPower;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_TotalReceivedBeamedPower", guiFormat = "F4", guiUnits = " GJ/s")]//Beamed Energy
        public double totalReceivedBeamedPowerInGigaWatt;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_ConnectedTransmittersCount")]//Beamed Connections
        public int connectedTransmittersCount;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_TotalForceInNewtonFromBeamedPower", guiFormat = "F4", guiUnits = " N")]//Beamed Potential Force
        public double totalForceInNewtonFromBeamedPower = 0;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_WeightedBeamPowerPitch", guiFormat = "F3", guiUnits = "°")]//Beamed Pitch Angle
        public double weightedBeamPowerPitch;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_WeightedBeamedPowerSpotsize", guiFormat = "F3", guiUnits = " m")]//Beamed Spotsize
        public double weightedBeamedPowerSpotsize;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_BeamedSailForce", guiFormat = "F3", guiUnits = " N")]//Beamed Sail Force
        public double beamedSailForce = 0;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_BeamedAcc")]//Beamed Acceleration
        public string beamedAcc;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_BeamPowerWasteheatInMegaJoules", guiFormat = "F3", guiUnits = " MJ/s")]//Beamed Energy Absorbed
        public double beamPowerWasteheatInMegaJoules;

        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_AtmosphericGasKgPerSquareMeter", guiUnits = " kg/m\xB2")]//Atmospheric Density
        public double atmosphericGasKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_MaximumDragPerSquareMeter", guiUnits = " N/m\xB2")]//Maximum Drag
        public float maximumDragPerSquareMeter;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_WeightedDragCoefficient", guiFormat = "F3")]//Drag Coefficient
        public double weightedDragCoefficient;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_DragHeatInMegajoule", guiFormat = "F3", guiUnits = " MJ/s")]//Drag Heat Absorbed
        public double dragHeatInMegajoule;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_DiffuseSailDragInNewton", guiUnits = " N")]//Diffuse Drag
        public float diffuseSailDragInNewton;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_SpecularSailInNewton", guiUnits = " N")]//Specular Drag
        public float specularSailDragInNewton;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_PeriapsisChange", guiFormat = "F3", guiUnits = " m/s")]//Abs Periapsis Change
        public double periapsisChange;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_ApapsisChange", guiFormat = "F3", guiUnits = " m/s")]//Abs Apapsis Change
        public double apapsisChange;
        [KSPField(guiActive = true, guiName = "#LOC_PhotonSail_OrbitSizeChange", guiFormat = "F3", guiUnits = " m/s")]//Orbit Diameter Change
        public double orbitSizeChange;
        [KSPField(guiActive = false, guiName = "#LOC_PhotonSail_HasLineOfSightToKSC")]//Can See KCS
        public bool hasLineOfSightToKtc;

        [KSPField(guiActive = true)] 
        public double specularRatio;

        //[KSPField(isPersistant = true, guiActive = true, guiName = "Global Acceleration", guiUnits = "m/s"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = -100)]
        //public float globalAcceleration = 0;
        //[KSPField(isPersistant = true, guiActive = true, guiName = "Global Angle", guiUnits = "m/s"), UI_FloatRange(stepIncrement = 1, maxValue = 180, minValue = 0)]
        //public float globalAngle = 45;
        //[KSPField(isPersistant = true, guiActive = true, guiName = "Skin Heating", guiUnits = "m/s"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = -100)]
        //public float skinHeating = 0;

        double solar_acc_d;
        double beamed_acc_d;
        double sailSurfaceModifier;
        double initialMass;
        double kscPhotonReflection;
        double kscPhotovoltaic;
        double maxSailHeatDissipationInWatt;
        double currentSurfaceArea;
        double frontPhotovotalicRatio;
        double backPhotovotalicRatio;
        double doorPhotovotalicRatio;
        double solarPhotovoltaicEfficiencyFactor;
        double doorsPhotovoltaicKiloWatt;
        double energyOnSailnWatt;
        double dragHeatInJoule;

        int solarPhotovoltaicTechLevel;
        int beamCounter;
        int updateCounter;
        int buttonPressedTime = 10;

        List<PhotonReflectionDefinition> photonReflectionDefinitions = new List<PhotonReflectionDefinition>();
        List<ReceivedPowerData> connectedTransmitters = new List<ReceivedPowerData>();
        IDictionary<VesselMicrowavePersistence, KeyValuePair<MicrowaveRoute, IList<VesselRelayPersistence>>> _transmitDataCollection;

        IPowerSupply powerSupply;
        BeamEffect[] beamEffectArray;
        Texture2D beamTexture;
        Shader transparentShader;
        Animation solarSailAnim1;
        Animation solarSailAnim2;

        readonly List<BeamRay> beamRays = new List<BeamRay>();
        readonly List<ReceivedBeamedPower> receivedBeamedPowerList = new List<ReceivedBeamedPower>();
        readonly Dictionary<Vessel, ReceivedPowerData> received_power = new Dictionary<Vessel, ReceivedPowerData>();

        readonly Queue<double> periapsisChangeQueue = new Queue<double>(30);
        readonly Queue<double> apapsisChangeQueue = new Queue<double>(30);

        public int ReceiverType { get { return 7; } }                       // receiver from either top or bottom

        public double Diameter { get { return diameter; } }

        public double ApertureMultiplier { get { return 1; } }

        public double MinimumWavelength { get { return minimumWavelength; } }     // receive optimally from red visible light

        public double MaximumWavelength { get { return maximumWavelength; } }           // receive up to maximum infrared

        public double HighSpeedAtmosphereFactor { get { return 1; } }

        public double FacingThreshold { get { return 0; } }

        public double FacingSurfaceExponent { get { return 1; } }

        public double FacingEfficiencyExponent { get { return 0; } }    // can receive beamed power from any angle

        public double SpotsizeNormalizationExponent { get { return 1; } }

        public bool CanBeActiveInAtmosphere { get { return false; } }

        public Vessel Vessel { get { return vessel; } }

        public Part Part { get { return part; } }

        // GUI to deploy sail
        [KSPEvent(guiActiveEditor = true,  guiActive = true, guiName = "#LOC_PhotonSail_DeploySail", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]//Deploy Sail
        public void DeploySail()
        {
            Debug.Log("[PhotonSailor]: DeploySail called");
            RunAnimation(animName, solarSailAnim1, 0.5f, 0);
            RunAnimation(animName, solarSailAnim2, 0.5f, 0);
            IsEnabled = true;
            buttonPressedTime = updateCounter;
        }

        // GUI to retract sail
        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_PhotonSail_RetractSail", active = false, guiActiveUncommand = true, guiActiveUnfocused = true)]//Retract Sail
        public void RetractSail()
        {
            Debug.Log("[PhotonSailor]: RetractSail called");
            RunAnimation(animName, solarSailAnim1, -0.5f, 1);
            RunAnimation(animName, solarSailAnim2, -0.5f, 1);
            IsEnabled = false;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("FNGenerator.OnRescale called with " + factor.absolute.linear);
                storedMassMultiplier = Mathf.Pow(factor.absolute.linear, 2);
                initialMass = (double)(decimal)part.prefabMass * storedMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("[PhotonSailor]: FNGenerator.OnRescale " + e.Message);
            }
        }

        // Initialization
        public override void OnStart(PartModule.StartState state)
        {
            powerSupply = part.FindModuleImplementing<IPowerSupply>();

            if (powerSupply != null)
                powerSupply.DisplayName = Localizer.Format("#LOC_PhotonSail_powerSupply");//"started"

            diameter = Math.Sqrt(surfaceArea);

            frontPhotovotalicRatio = surfaceArea > 0 ? frontPhotovoltaicArea / surfaceArea : 0;
            backPhotovotalicRatio = surfaceArea > 0 ? backPhotovoltaicArea / surfaceArea : 0;
            doorPhotovotalicRatio = surfaceArea > 0 ? doorsPhotovoltaicArea / surfaceArea : 0;

            if (animName != null)
            {
                var animators = part.FindModelAnimators(animName);
                var animatorsCount = animators.Count();
                solarSailAnim1 = animatorsCount > 0 ? animators[0] : null;
                solarSailAnim2 = animatorsCount > 1 ? animators[1] : null;
            }

            photonReflectionDefinitions = part.FindModulesImplementing<PhotonReflectionDefinition>();

            GetPhotonStatisticsForWavelength(kscLaserWavelength, ref kscPhotonReflection, ref kscPhotovoltaic);

            kscPhotonReflectionPercentage = kscPhotonReflection * 100;

            // start with deployed sail  when enabled
            if (IsEnabled)
            {
                Debug.Log("[PhotonSailor]: Startup with solarSail deployed");

                if (solarSailAnim1 != null)
                {
                    solarSailAnim1[animName].speed = initialAnimationSpeed;
                    solarSailAnim1[animName].normalizedTime = 0;
                    solarSailAnim1.Blend(animName);
                }
                if (solarSailAnim2 != null)
                {
                    solarSailAnim2[animName].speed = initialAnimationSpeed;
                    solarSailAnim2[animName].normalizedTime = 0;
                    solarSailAnim2.Blend(animName);
                }
            }
            else
            {
                Debug.Log("[PhotonSailor]: Startup with solarSail retracted");
            }

            transparentShader = Shader.Find("Unlit/Transparent");
            beamTexture = GameDatabase.Instance.GetTexture("PhotonSail/ParticleFX/infrared2", false);

            DeterminePhotovoltaicEfficiency();

            DetermineKscLaserPower();

            DetermineKscLaserAperture();

            DetermineMassTechMultiplier();

            kscLaserPowerInGigaWatt = kscLaserPowerInWatt * 1e-9;

            InitializeMassVariables();

            if (state == StartState.None || state == StartState.Editor)
                return;

            UnityEngine.Debug.Log("[PhotonSailor]: ModulePhotonSail on " + part.name + " was Force Activated");
            this.part.force_activate();

            CreateBeamArray();
        }

        private void InitializeMassVariables()
        {
            double prefabMass = (double)(decimal)part.prefabMass;
            initialMass = prefabMass * storedMassMultiplier;
            if (initialMass == 0)
            {
                initialMass = (double)(decimal)part.mass;
                storedMassMultiplier = prefabMass > 0 ? initialMass / prefabMass : 0;
            }
        }

        private void DeterminePhotovoltaicEfficiency()
        {
            solarPhotovoltaicTechLevel = ResearchAndDevelopment.Instance == null ? 3 
                : HasTech(solarPhotovoltaicTech1, 1) + HasTech(solarPhotovoltaicTech2, 1) + HasTech(solarPhotovoltaicTech3, 1);

            if (solarPhotovoltaicTechLevel >= 3)
                solarPhotovoltaicEfficiencyFactor = solarPhotovoltaicEfficiency3;
            else if (solarPhotovoltaicTechLevel == 2)
                solarPhotovoltaicEfficiencyFactor = solarPhotovoltaicEfficiency2;
            else if (solarPhotovoltaicTechLevel == 1)
                solarPhotovoltaicEfficiencyFactor = solarPhotovoltaicEfficiency1;
            else
                solarPhotovoltaicEfficiencyFactor = solarPhotovoltaicEfficiency0;

            solarPhotovoltaicEfficiencyFactor /= 100;
        }

        private void DetermineKscLaserPower()
        {
            if (ResearchAndDevelopment.Instance == null)
                return;

            kscLaserPowerInWatt = kscLaserPowerBonus0;
            kscLaserPowerInWatt += HasUpgrade(kscPowerUpgdradeName1) ? kscLaserPowerBonus1 : 0;
            kscLaserPowerInWatt += HasUpgrade(kscPowerUpgdradeName2) ? kscLaserPowerBonus2 : 0;
            kscLaserPowerInWatt += HasUpgrade(kscPowerUpgdradeName3) ? kscLaserPowerBonus3 : 0;
            kscLaserPowerInWatt += HasUpgrade(kscPowerUpgdradeName4) ? kscLaserPowerBonus4 : 0;
            kscLaserPowerInWatt += HasUpgrade(kscPowerUpgdradeName5) ? kscLaserPowerBonus5 : 0;
            kscLaserPowerInWatt += HasUpgrade(kscPowerUpgdradeName6) ? kscLaserPowerBonus6 : 0;
            kscLaserPowerInWatt += HasUpgrade(kscPowerUpgdradeName7) ? kscLaserPowerBonus7 : 0;

            kscLaserPowerInWatt *= kscPowerMult;
        }

        private void DetermineKscLaserAperture()
        {
            UnityEngine.Debug.Log("[KSPI]: ModulePhotonSail start DetermineKscLaserAperture");

            if (ResearchAndDevelopment.Instance == null)
            {
                UnityEngine.Debug.Log("[KSPI]: ModulePhotonSail ResearchAndDevelopment.Instance == null");
                return;
            }

            kscLaserAperture = kscLaserApertureBonus0;
            kscLaserAperture += HasUpgrade(kscLaserApertureName1) ? kscLaserApertureBonus1 : 0;
            kscLaserAperture += HasUpgrade(kscLaserApertureName2) ? kscLaserApertureBonus2 : 0;
            kscLaserAperture += HasUpgrade(kscLaserApertureName3) ? kscLaserApertureBonus3 : 0;
            kscLaserAperture += HasUpgrade(kscLaserApertureName4) ? kscLaserApertureBonus4 : 0;
            kscLaserAperture += HasUpgrade(kscLaserApertureName5) ? kscLaserApertureBonus5 : 0;
            kscLaserAperture += HasUpgrade(kscLaserApertureName6) ? kscLaserApertureBonus6 : 0;
            kscLaserAperture += HasUpgrade(kscLaserApertureName7) ? kscLaserApertureBonus7 : 0;

            kscLaserAperture *= kscApertureMult;
        }

        private void DetermineMassTechMultiplier()
        {
            if (ResearchAndDevelopment.Instance == null)
            {
                massTechMultiplier = 1d/32d;
                return;
            }

            massTechMultiplier /= HasTech(massReductionTech1) ? massReductionMult1 : 1;
            massTechMultiplier /= HasTech(massReductionTech2) ? massReductionMult2 : 1;
            massTechMultiplier /= HasTech(massReductionTech3) ? massReductionMult3 : 1;
            massTechMultiplier /= HasTech(massReductionTech4) ? massReductionMult4 : 1;
            massTechMultiplier /= HasTech(massReductionTech5) ? massReductionMult5 : 1;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return (float)(initialMass * (massTechMultiplier - 1.0));
        }

        private void CreateBeamArray()
        {
            beamEffectArray = new BeamEffect[animatedRays];

            for (var i = 0; i < beamEffectArray.Length; i++)
            {
                beamEffectArray[i] = CreateBeam(1001 + i);
            }
        }

        private BeamEffect CreateBeam(int renderQueue)
        {
            var beam = new BeamEffect {solar_effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder)};

            beam.solar_effect.transform.localScale = Vector3.zero;
            beam.solar_effect.transform.position = Vector3.zero;
            beam.solar_effect.transform.rotation = part.transform.rotation;
           
            beam.solar_effect_collider = beam.solar_effect.GetComponent<Collider>();
            beam.solar_effect_collider.enabled = false;

            beam.solar_effect_renderer = beam.solar_effect.GetComponent<Renderer>();
            beam.solar_effect_renderer.material.shader = transparentShader;
            beam.solar_effect_renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 1);
            beam.solar_effect_renderer.material.mainTexture = beamTexture;
            beam.solar_effect_renderer.material.renderQueue = renderQueue;
            beam.solar_effect_renderer.receiveShadows = false;

            return beam;
        }

        public void Update()
        {
            // Sail deployment GUI
            Events[nameof(DeploySail)].active = !IsEnabled;
            Events[nameof(RetractSail)].active = IsEnabled;
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight)
                return;

            diameter = Math.Sqrt(surfaceArea);
            maxSailHeatDissipationInWatt = GetBlackBodyDissipation(surfaceArea, part.skinMaxTemp * 0.999);
            maxSailHeatDissipationInMegajoules = maxSailHeatDissipationInWatt * 1e-6;
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            updateCounter++;
            maxNetworkPower = 0;

            AnimationState animationState = solarSailAnim1[animName];

            var animationNormalizedTime = animationState.normalizedTime;

            var deploymentRatio = animationNormalizedTime > 0
                ? (animationNormalizedTime > startOfSailOpening ? (animationNormalizedTime - startOfSailOpening) * (1 / (1 - startOfSailOpening)) : 0)
                : IsEnabled && updateCounter - buttonPressedTime >= 10 ? 1 : 0;

            sailSurfaceModifier = deploymentRatio * deploymentRatio;
            currentSurfaceArea = surfaceArea * sailSurfaceModifier;

            externalTemperature = FlightGlobals.getExternalTemperature(part.transform.position, vessel.mainBody);
            maxSailHeatDissipationInWatt = GetBlackBodyDissipation(currentSurfaceArea, part.skinMaxTemp * 0.999 - externalTemperature);
            maxSailHeatDissipationInMegajoules = maxSailHeatDissipationInWatt * 1e-6;

            part.emissiveConstant = sailSurfaceModifier > 0 ? 0 : 1 - reflectedPhotonRatio;

            // update available beamed power transmitters
            _transmitDataCollection = BeamedPowerHelper.GetConnectedTransmitters(this);

            foreach (var transmitData in _transmitDataCollection)
            {
                VesselMicrowavePersistence transmitter = transmitData.Key;
                KeyValuePair<MicrowaveRoute, IList<VesselRelayPersistence>> routeRelayData = transmitData.Value;

                if (!received_power.TryGetValue(transmitter.Vessel, out var beamedPowerData))
                {
                    beamedPowerData = new ReceivedPowerData
                    {
                        Receiver = this,
                        Transmitter = transmitter
                    };
                    received_power[beamedPowerData.Transmitter.Vessel] = beamedPowerData;
                }

                beamedPowerData.NetworkPower = 0;
                beamedPowerData.AvailablePower = 0;
                beamedPowerData.Route = routeRelayData.Key;
                beamedPowerData.Distance = beamedPowerData.Route.Distance;
                beamedPowerData.UpdateCounter = updateCounter;
 
                foreach(var wavelengthData in transmitter.SupportedTransmitWavelengths)
                {
                    var transmittedPower = (wavelengthData.nuclearPower + wavelengthData.solarPower) * 0.001;

                    maxNetworkPower += transmittedPower;

                    beamedPowerData.NetworkPower += transmittedPower;

                    var currentWavelengthBeamedPower = transmittedPower * beamedPowerData.Route.Efficiency;

                    beamedPowerData.AvailablePower += currentWavelengthBeamedPower; 
                }
            }

            // reset any non updated record
            foreach (var beamedPowerData in received_power.Values)
            {
                if (beamedPowerData.UpdateCounter != updateCounter)
                {
                    beamedPowerData.NetworkPower = 0;
                    beamedPowerData.AvailablePower = 0;
                }
            }

            connectedTransmitters = received_power.Values.Where(m => m.AvailablePower > 0).OrderBy(m => m.AvailablePower).ToList();

            if (connectedTransmittersCount > 0)
                TimeWarp.GThreshold = 100000;
            else
                TimeWarp.GThreshold = 2;

            var showBeamedPowerFields = IsEnabled && (connectedTransmittersCount > 0 || hasLineOfSightToKtc) && totalReceivedBeamedPower > 0;
            Fields[nameof(totalReceivedBeamedPowerInGigaWatt)].guiActive = showBeamedPowerFields;
            Fields[nameof(connectedTransmittersCount)].guiActive = showBeamedPowerFields;
            Fields[nameof(totalForceInNewtonFromBeamedPower)].guiActive = showBeamedPowerFields;
            Fields[nameof(weightedBeamPowerPitch)].guiActive = showBeamedPowerFields;
            Fields[nameof(weightedBeamedPowerSpotsize)].guiActive = showBeamedPowerFields;
            Fields[nameof(beamedSailForce)].guiActive = showBeamedPowerFields;
            Fields[nameof(beamedAcc)].guiActive = showBeamedPowerFields;

            Fields[nameof(weightedDragCoefficient)].guiActive = maximumDragPerSquareMeter > 0;
            Fields[nameof(maximumDragPerSquareMeter)].guiActive = maximumDragPerSquareMeter > 0;
            Fields[nameof(dragHeatInMegajoule)].guiActive = maximumDragPerSquareMeter > 0;

            Fields[nameof(diffuseSailDragInNewton)].guiActive = diffuseSailDragInNewton > 0;
            Fields[nameof(specularSailDragInNewton)].guiActive = specularSailDragInNewton > 0;

            var relevantOrbitalData =
                (vessel.situation & Vessel.Situations.SPLASHED) != Vessel.Situations.SPLASHED && 
                (vessel.situation & Vessel.Situations.LANDED) != Vessel.Situations.LANDED &&
                (vessel.situation & Vessel.Situations.PRELAUNCH) != Vessel.Situations.PRELAUNCH && 
                (vessel.situation & Vessel.Situations.FLYING) != Vessel.Situations.FLYING; 

            Fields[nameof(periapsisChange)].guiActive = relevantOrbitalData;
            Fields[nameof(apapsisChange)].guiActive = relevantOrbitalData;
            Fields[nameof(orbitSizeChange)].guiActive = relevantOrbitalData;

            // Text fields (acc & force)
            Fields[nameof(solarAcc)].guiActive = IsEnabled;

            solarAcc = solar_acc_d.ToString("E") + " m/s";
            beamedAcc = beamed_acc_d.ToString("E") + " m/s"; ;
        }

        public override void OnFixedUpdate()
        {
            kscLaserElevationAngle = 0;
            skinTemperature = part.skinTemperature;
            availableBeamedKscEnergy = 0;
            receivedBeamedPowerFromKsc = 0;
            totalReceivedBeamedPower = 0;
            totalReceivedBeamedPowerInGigaWatt = 0;
            photovoltaicFlowRate = 0;
            photovoltaicPotential = 0;
            totalSolarEnergyReceivedInMJ = 0;
            totalForceInNewtonFromSolarEnergy = 0;
            totalForceInNewtonFromBeamedPower = 0;
            sailHeatDissipationTemperature = 0;
            solar_force_d = 0;
            solar_acc_d = 0;
            beamCounter = 0;
            beamedSailForce = 0;
            beamed_acc_d = 0;
            absorbedPhotonHeatInWatt = 0;
            dissipationInMegaJoules = 0;            

            beamRays.Clear();
            receivedBeamedPowerList.Clear();

            if (FlightGlobals.fetch == null || part == null || vessel == null)
                return;

            UpdateChangeGui();

            ResetBeams();            

            var vesselMassInKg = vessel.totalMass * 1000;
            var universalTime = Planetarium.GetUniversalTime();
            var positionVessel = vessel.orbit.getPositionAtUT(universalTime);
            var beamedPowerThrottleRatio = beamedPowerThrottle * 0.01f;
            var rentedBeamedPowerThrottleRatio = kcsBeamedPowerThrottle * 0.01f;

            // apply drag and calculate drag heat
            ApplyDrag(universalTime, vesselMassInKg);

            // update solar flux
            UpdateSolarFlux(universalTime, positionVessel, vessel);

            // unconditionally apply solarFlux energy for every star
            foreach (var starLight in KopernicusHelper.Stars)
            {
                GenerateForce(reflectedPhotonRatio, solarPhotovoltaicEfficiencyFactor, ref absorbedPhotonHeatInWatt, ref starLight.position, ref positionVessel, starLight.solarFlux, universalTime, vesselMassInKg);
            }

            // refresh connectedTransmittersCount
            connectedTransmittersCount = connectedTransmitters.Count;

            // apply photon pressure from Kerbal Space Station Beamed Power facility
            ProcessKscBeamedPower(vesselMassInKg, universalTime, ref positionVessel, (double)(decimal)beamedPowerThrottleRatio * (double)(decimal)rentedBeamedPowerThrottleRatio, ref absorbedPhotonHeatInWatt);

            // sort transmitter on most favoritable angle
            var sortedConnectedTransmitters =  connectedTransmitters.Select(transmitter =>
            {
                var normalizedPowerSourceToVesselVector = (positionVessel - transmitter.Transmitter.Vessel.GetWorldPos3D()).normalized;
                var cosConeAngle = Vector3d.Dot(normalizedPowerSourceToVesselVector, vessel.transform.up);
                if (cosConeAngle < 0)
                    cosConeAngle = Vector3d.Dot(normalizedPowerSourceToVesselVector, -vessel.transform.up);
                return new { cosConeAngle, transmitter };
            }).OrderByDescending(m => m.cosConeAngle).Select(m => m.transmitter);

            // apply photon pressure from every potential laser source
            foreach (var receivedPowerData in sortedConnectedTransmitters)
            {
                double photonReflection = 0;
                double photovoltaicEfficiency = 0;
                GetPhotonStatisticsForWavelength(receivedPowerData.Route.WaveLength, ref photonReflection, ref photovoltaicEfficiency);

                var availableTransmitterPowerInWatt = CheatOptions.IgnoreMaxTemperature 
                    ? receivedPowerData.AvailablePower * 1e+6
                    : Math.Min(receivedPowerData.AvailablePower * 1e+6, Math.Max(0, (maxSailHeatDissipationInWatt - absorbedPhotonHeatInWatt - dragHeatInJoule) / (1 - photonReflection)));

                Vector3d beamedPowerSource = receivedPowerData.Transmitter.Vessel.GetWorldPos3D();

                GenerateForce(photonReflection, photovoltaicEfficiency, ref absorbedPhotonHeatInWatt,  ref beamedPowerSource, ref positionVessel, beamedPowerThrottleRatio * availableTransmitterPowerInWatt, universalTime, vesselMassInKg, false, receivedPowerData.Route.Spotsize * 0.25);
            }

            // process statistical data
            if (receivedBeamedPowerList.Count > 0)
            {
                totalReceivedBeamedPower = receivedBeamedPowerList.Sum(m => m.receivedPower);
                if (totalReceivedBeamedPower > 0)
                {
                    weightedBeamPowerPitch = receivedBeamedPowerList.Sum(m => m.pitchAngle * m.receivedPower / totalReceivedBeamedPower);
                    weightedBeamedPowerSpotsize = receivedBeamedPowerList.Sum(m => m.spotsize * m.receivedPower / totalReceivedBeamedPower);
                    totalReceivedBeamedPowerInGigaWatt = totalReceivedBeamedPower * 1e-3;
                }
            }

            // generate electric power
            powerSupply.SupplyMegajoulesPerSecondWithMax(photovoltaicFlowRate, photovoltaicPotential);

            // apply wasteheat
            ProcessThermalDynamics(absorbedPhotonHeatInWatt);

            // display beamed rays
            AnimateRays();
        }

        private void GetPhotonStatisticsForWavelength(double wavelength, ref double reflection, ref double photovolaticEfficiency)
        {
            var reflectionDefinition = photonReflectionDefinitions.FirstOrDefault(m => (wavelength >= m.minimumWavelength && wavelength <= m.maximumWavelength));
            reflection = reflectionDefinition != null ? reflectionDefinition.PhotonReflectionPercentage * 0.01 : 0;
            photovolaticEfficiency = reflectionDefinition != null ? reflectionDefinition.PhotonPhotovoltaicPercentage * 0.01 : 0;
        }

        private void ProcessKscBeamedPower(double vesselMassInKg, double universalTime, ref Vector3d positionVessel, double beamedPowerThrottleRatio, ref double receivedHeatInWatt)
        {
            if (kscLaserPowerInWatt <= 0 || kscLaserAperture <= 0)
                return;

            var homeWorldBody = Planetarium.fetch.Home;
            Vector3d positionKscLaser = homeWorldBody.GetWorldSurfacePosition(kscLaserLatitude, kscLaserLongitude, kscLaserAltitude);
            Vector3d centerOfHomeWorld = homeWorldBody.position;

            hasLineOfSightToKtc = LineOfSightToTransmitter(positionVessel, positionKscLaser);

            if (hasLineOfSightToKtc)
            {
                // calculate spotSize and received power From Ktc
                Vector3d powerSourceToVesselVector = positionVessel - positionKscLaser;
                var beamAngleKscToCenterInDegree = Vector3d.Angle(powerSourceToVesselVector, positionVessel - centerOfHomeWorld);
                var beamAngleKscToVesselInDegree = Vector3d.Angle(centerOfHomeWorld - positionKscLaser, centerOfHomeWorld - positionVessel);

                kscLaserElevationAngle = 90 - beamAngleKscToCenterInDegree - beamAngleKscToVesselInDegree;
                var kscLaserElevationAngleInRadian = (Math.Sin(kscLaserElevationAngle * (double)(decimal)Mathf.Deg2Rad));
                var kscAtmosphereMultiplier = kscLaserElevationAngleInRadian > 0 ? 1 / kscLaserElevationAngleInRadian : 0;
                var atmosphereAbsorptionEfficiency = Math.Max(0, 1 - kscAtmosphereMultiplier * kscAtmosphereAbsorbtionRatio);

                var surfaceKscEnergy = CheatOptions.IgnoreMaxTemperature || kscPhotonReflection == 1 
                    ? kscLaserPowerInWatt
                    : Math.Min(kscLaserPowerInWatt, Math.Max(0, (maxSailHeatDissipationInWatt - receivedHeatInWatt - dragHeatInJoule) / (1 - kscPhotonReflection)));

                availableBeamedKscEnergy = atmosphereAbsorptionEfficiency * surfaceKscEnergy;

                if (Funding.Instance != null && Funding.Instance.Funds < availableBeamedKscEnergy * 1e-9)
                    return;

                if (beamedPowerThrottleRatio > 0 && kscLaserElevationAngle >= kscLaserMinElevationAngle)
                {
                    connectedTransmittersCount++;

                    var cosConeAngle = Vector3d.Dot(powerSourceToVesselVector.normalized, this.vessel.transform.up);
                    if (cosConeAngle < 0)
                        cosConeAngle = Vector3d.Dot(powerSourceToVesselVector.normalized, -this.vessel.transform.up);
                    var effectiveDiameter = cosConeAngle * diameter;

                    var labdaSpotSize = powerSourceToVesselVector.magnitude * kscLaserWavelength / kscLaserAperture;
                    var centralSpotSize = labdaSpotSize * kscCentralSpotsizeMult;
                    var sideSpotSize = labdaSpotSize * kscSideSpotsizeMult;

                    var centralSpotsizeRatio = centralSpotSize > 0 ? Math.Min(1, effectiveDiameter / centralSpotSize) : 1;
                    var sideSpotsizeRatio = sideSpotSize > 0 ? Math.Min(1, effectiveDiameter / sideSpotSize) : 1;
                    var sideSpotsizeToFourthPowerRatio = sideSpotsizeRatio * sideSpotsizeRatio * sideSpotsizeRatio * sideSpotsizeRatio;

                    var throtledPower = availableBeamedKscEnergy * beamedPowerThrottleRatio;
                    var receivedPowerFromCentralSpot = throtledPower * kscCentralSpotEnergyRatio * centralSpotsizeRatio;
                    var receivedPowerFromSideSpot = throtledPower * kscSideSpotEnergyRatio * sideSpotsizeToFourthPowerRatio;
                    receivedBeamedPowerFromKsc = receivedPowerFromCentralSpot + receivedPowerFromSideSpot;

                    //gausionRatio = Math.Pow(centralSpotsizeRatio, 0.3 + (0.6 * (1 - centralSpotsizeRatio)))

                    var usedEnergyInGW = GenerateForce(kscPhotonReflection, kscPhotovoltaic, ref receivedHeatInWatt, ref positionKscLaser, ref positionVessel, receivedBeamedPowerFromKsc, universalTime, vesselMassInKg, false, centralSpotSize * 0.25) * 1e-9;

                    //if (usedEnergyInGW > 0 && Funding.Instance != null)
                    //{
                    //    // subtract funds from account
                    //    Funding.Instance.AddFunds(-usedEnergyInGW * TimeWarp.fixedDeltaTime * 0.1, TransactionReasons.None);
                    //}
                }
            }
        }

        private void AnimateRays()
        {
            foreach (var ray in beamRays)
            {
                var availableSailDiameter = sailSurfaceModifier * ray.cosConeAngle * diameter * 0.25;
                var effect = Math.Ceiling(10 * Math.Pow(ray.energyInGigaWatt, 0.35));
                var effectCount = (int)effect;

                if (effect == 0)
                    continue;

                for (int i = 0; i < effectCount; i++)
                {
                    if (beamCounter < animatedRays)
                    {
                        var effectRatio = (effect - i) / effect;
                        var scale = ray.spotsize * 4 * effectRatio < diameter ? 1 : 2;
                        var spotSize = (float)Math.Max(availableSailDiameter * effectRatio, ray.spotsize * effectRatio);
                        UpdateVisibleBeam(part, beamEffectArray[beamCounter++], ray.powerSourceToVesselVector, scale, spotSize);
                    }
                }
            }
        }

        private void ProcessThermalDynamics(double absorbedPhotonHeatInWatt)
        {
            var thermalMassPerKilogram = (double)(decimal)part.mass * part.skinThermalMassModifier * PhysicsGlobals.StandardSpecificHeatCapacity * 1e-3;            

            // calculate heating
            solarfluxWasteheatInMegaJoules = (1 - reflectedPhotonRatio) * totalSolarEnergyReceivedInMJ * Math.Max(0, 1 - vessel.atmDensity);
            beamPowerWasteheatInMegaJoules = absorbedPhotonHeatInWatt * 1e-6 - solarfluxWasteheatInMegaJoules;
            currentSailHeatingInMegajoules = beamPowerWasteheatInMegaJoules + solarfluxWasteheatInMegaJoules + dragHeatInMegajoule;
            dissipationInMegaJoules = GetBlackBodyDissipation(currentSurfaceArea, Math.Max(0, part.skinTemperature - externalTemperature)) * 1e-6;

            if (currentSurfaceArea > 0 && thermalMassPerKilogram > 0)
            {
                sailHeatDissipationTemperature = Math.Pow(currentSailHeatingInMegajoules * 1e+6 / currentSurfaceArea / PhysicsGlobals.StefanBoltzmanConstant, 0.25);

                var relaxedSailHeatingInMegajoules = updateCounter > 10 ? currentSailHeatingInMegajoules : currentSailHeatingInMegajoules * (updateCounter * 0.1);

                var temperatureChange = (relaxedSailHeatingInMegajoules - dissipationInMegaJoules) / thermalMassPerKilogram; 

                var modifiedTemperature = part.skinTemperature + temperatureChange;

                if (part.skinTemperature < sailHeatDissipationTemperature)
                    part.skinTemperature = Math.Min(sailHeatDissipationTemperature, modifiedTemperature);
                else
                    part.skinTemperature = Math.Max(sailHeatDissipationTemperature, modifiedTemperature);
            }
        }

        private static double GetBlackBodyDissipation(double surfaceArea, double temperatureDelta)
        {
            return surfaceArea * PhysicsGlobals.StefanBoltzmanConstant * temperatureDelta * temperatureDelta * temperatureDelta * temperatureDelta;
        }

        private void ResetBeams()
        {
            foreach (var beamEffect in beamEffectArray)
            {
                UpdateVisibleBeam(part, beamEffect, Vector3d.zero, 0, 0);
            }
        }

        public void UpdateSolarFlux(double universalTime, Vector3d vesselPosition, Vessel vessel)
        {
            totalSolarFluxInWatt = 0;

            foreach (var starLight in KopernicusHelper.Stars)
            {
                starLight.position = starLight.star.position;
                starLight.hasLineOfSight = LineOfSightToSun(vesselPosition, starLight.star);
                starLight.solarFlux = starLight.hasLineOfSight ? solarFluxAtDistance(vessel, starLight.star, starLight.relativeLuminocity) : 0;
                totalSolarFluxInWatt += starLight.solarFlux;
            }
        }

        private void ApplyDrag(double universalTime, double vesselMassInKg)
        {
            atmosphericGasKgPerSquareMeter = AtmosphericFloatCurves.GetAtmosphericGasDensityKgPerCubicMeter(vessel);

            specularRatio = part.skinMaxTemp > 0 ? Math.Max(0, Math.Min(1, part.skinTemperature / part.skinMaxTemp)) : 1;
            var diffuseRatio = 1 - specularRatio;

            var maximumDragCoefficient = 4 * specularRatio + 3.3 * diffuseRatio;
            var normalizedOrbitalVector = vessel.obt_velocity.normalized;
            var cosOrbitRaw = Vector3d.Dot(vessel.transform.up, normalizedOrbitalVector);
            var cosOrbitalDrag = Math.Abs(cosOrbitRaw);
            var squaredCosOrbitalDrag = cosOrbitalDrag * cosOrbitalDrag;

            var siderealSpeed = vessel.mainBody.rotationPeriod > 0 ? 2 * vessel.mainBody.Radius * Math.PI / vessel.mainBody.rotationPeriod : 0;
            var effectiveSurfaceArea = cosOrbitalDrag * currentSurfaceArea * (IsEnabled ? 1 : 0);
            var highAltitudeDistance = vessel.mainBody.atmosphereDepth > 0 ? Math.Min(1, vessel.altitude / vessel.mainBody.atmosphereDepth) : 1;
            var lowOrbitModifier =  vessel.altitude > 0 ? Math.Min(1, vessel.mainBody.atmosphereDepth / vessel.altitude) : 1;

            var highOrbitModifier = Math.Sqrt(1 - lowOrbitModifier);
            var effectiveSpeedForDrag = Math.Max(0, vessel.obt_speed - siderealSpeed * lowOrbitModifier);
            var dragForcePerSquareMeter = atmosphericGasKgPerSquareMeter * 0.5 * effectiveSpeedForDrag * effectiveSpeedForDrag;
            maximumDragPerSquareMeter = (float)(dragForcePerSquareMeter * maximumDragCoefficient);
            
            // calculate specular Drag
            Vector3d vesselNormal = vessel.transform.up;
            if (cosOrbitRaw < 0)
                vesselNormal = -vesselNormal;

            var specularDragCoefficient = squaredCosOrbitalDrag + 3 * squaredCosOrbitalDrag * highOrbitModifier;
            var specularDragPerSquareMeter = specularDragCoefficient * dragForcePerSquareMeter * specularRatio;
            var specularDragInNewton = specularDragPerSquareMeter * effectiveSurfaceArea;
            specularSailDragInNewton = (float)specularDragInNewton;
            var specularDragForce = specularDragInNewton * vesselNormal;

            // calculate Diffuse Drag
            var diffuseDragCoefficient = 1 + highOrbitModifier + squaredCosOrbitalDrag * 1.3 * highOrbitModifier;
            var diffuseDragPerSquareMeter = diffuseDragCoefficient * dragForcePerSquareMeter * diffuseRatio;
            var diffuseDragInNewton = diffuseDragPerSquareMeter * effectiveSurfaceArea;
            diffuseSailDragInNewton = (float)diffuseDragInNewton;
            Vector3d diffuseDragForceVector = diffuseDragInNewton * normalizedOrbitalVector * -1;

            Vector3d combinedDragDecelerationVector = vesselMassInKg > 0 ? (specularDragForce + diffuseDragForceVector) / vesselMassInKg : Vector3d.zero;

            // apply drag to vessel
            var highAtmosphereModifier = highAltitudeDistance * highAltitudeDistance * highAltitudeDistance;

            ChangeVesselVelocity(vessel.orbit, vessel, universalTime, highAtmosphereModifier * combinedDragDecelerationVector * (double)(decimal)TimeWarp.fixedDeltaTime);

            weightedDragCoefficient = specularDragCoefficient * specularRatio + diffuseDragCoefficient * diffuseRatio;

            // increase temperature skin
            dragHeatInMegajoule = highAtmosphereModifier * dragForcePerSquareMeter * effectiveSurfaceArea * heatMultiplier * (0.1 + cosOrbitalDrag) * 1e-3;
            dragHeatInJoule = dragHeatInMegajoule * 1e+6;
        }

        public static void ChangeVesselVelocity(Orbit orbit, Vessel vessel, double universalTime, Vector3d acceleration)
        {
            if (double.IsNaN(acceleration.x) || double.IsNaN(acceleration.y) || double.IsNaN(acceleration.z))
                return;

            if (double.IsInfinity(acceleration.x) || double.IsInfinity(acceleration.y) || double.IsInfinity(acceleration.z))
                return;

            if (vessel.packed)
                orbit.Perturb(acceleration, universalTime);
            else
                vessel.ChangeWorldVelocity(acceleration);
        }

        private double GenerateForce(double photonReflectionRatio, double photovoltaicEfficiency, ref double receivedHeatInWatt, ref Vector3d positionPowerSource, ref Vector3d positionVessel, double availableEnergyInWatt, double universalTime, double vesselMassInKg, bool isSun = true, double beamSpotSize = 1)
        {
            // calculate vector between vessel and star transmitter
            Vector3d powerSourceToVesselVector = positionVessel - positionPowerSource;

            // take part vector 
            Vector3d vesselNormal = vessel.transform.up;
            var normalizedPowerSourceToVesselVector = powerSourceToVesselVector.normalized;

            // Magnitude of force proportional to cosine-squared of angle between sun-line and normal
            var cosConeAngle = Vector3d.Dot(normalizedPowerSourceToVesselVector, vesselNormal);

            var cosConeAngleIsNegative = cosConeAngle < 0;

            // If normal points away from sun/transmitter, negate so our force is always away from the sun/transmitter
            // so that turning the backside towards the sun thrusts correctly
            if (cosConeAngleIsNegative)
            {
                // recalculate Magnitude of force proportional to cosine-squared of angle between sun-line and normal
                vesselNormal = -vesselNormal;
                cosConeAngle = Vector3d.Dot(normalizedPowerSourceToVesselVector, vesselNormal);
            }

            // convert radian into angle in degree
            var pitchAngleInDegree = Math.Acos(cosConeAngle) * Orbit.Rad2Deg;
            if (double.IsNaN(pitchAngleInDegree) || double.IsInfinity(pitchAngleInDegree))
                pitchAngleInDegree = 0;

            if (isSun)
            {
                solarSailAngle = pitchAngleInDegree;
                energyOnSailnWatt = availableEnergyInWatt * currentSurfaceArea;
                totalSolarEnergyReceivedInMJ = energyOnSailnWatt * cosConeAngle * 1e-6;

                doorsPhotovoltaicKiloWatt = sailSurfaceModifier == 0 ? 0.4 * (1 - cosConeAngle) : Math.Max(0.05, cosConeAngle);
                doorsPhotovoltaicKiloWatt *= doorPhotovotalicRatio * energyOnSailnWatt * 0.001 * 0.9 * Math.Sqrt(photovoltaicEfficiency);
            }
            else
            {
                // skip beamed power in undesirable direction
                if ((beamedPowerForwardDirection && cosConeAngleIsNegative) || (!beamedPowerForwardDirection && !cosConeAngleIsNegative))
                    return 0;

                energyOnSailnWatt = availableEnergyInWatt * sailSurfaceModifier;
                doorsPhotovoltaicKiloWatt = sailSurfaceModifier > 0 && cosConeAngleIsNegative 
                    ? doorPhotovotalicRatio * energyOnSailnWatt * 0.001 * 0.9 * Math.Max(0.05, cosConeAngle) * Math.Sqrt(photovoltaicEfficiency): 0; 
            }

            // generate Photovoltaic power
            var currentSailPhotovoltaicRatio = cosConeAngleIsNegative ? frontPhotovotalicRatio : backPhotovotalicRatio;
            var maxSailPhotovoltaicEnergyInKiloWatt = energyOnSailnWatt * 0.001 * currentSailPhotovoltaicRatio * photovoltaicEfficiency;
            photovoltaicPotential += doorsPhotovoltaicKiloWatt + maxSailPhotovoltaicEnergyInKiloWatt;
            photovoltaicFlowRate += doorsPhotovoltaicKiloWatt + maxSailPhotovoltaicEnergyInKiloWatt * cosConeAngle;

            // convert energy into momentum
            var maxReflectedRadiationPressure = 2 * energyOnSailnWatt / GameConstants.speedOfLight;

            // calculate solar light force at current location
            var maximumPhotonForceInNewton = photonReflectionRatio * maxReflectedRadiationPressure;

            // calculate effective radiation pressure on SolarSail
            var reflectedRadiationPressureOnSail = isSun ? maxReflectedRadiationPressure * cosConeAngle : maxReflectedRadiationPressure;

            // register force 
            if (isSun)
                totalForceInNewtonFromSolarEnergy += maximumPhotonForceInNewton * Sign(cosConeAngleIsNegative);
            else
                totalForceInNewtonFromBeamedPower += maximumPhotonForceInNewton * Sign(cosConeAngleIsNegative);

            if (!IsEnabled)
                return 0;

            // create beamed power rays
            if (!isSun && sailSurfaceModifier > 0 && beamedPowerThrottle > 0)
            {
                var availableEnergyInGigaWatt = availableEnergyInWatt * 1e-9;
                BeamRay ray = beamRays.FirstOrDefault(m => Math.Abs(m.cosConeAngle - cosConeAngle) < 0.0001);

                if (ray != null)
                {
                    var totalEnergy = ray.energyInGigaWatt + availableEnergyInGigaWatt;

                    if (totalEnergy > 0)
                    {
                        ray.spotsize = totalEnergy > 0
                            ? (ray.spotsize * (ray.energyInGigaWatt / totalEnergy)) + (beamSpotSize * (availableEnergyInGigaWatt / totalEnergy)) 
                            : 0;
                    }

                    ray.energyInGigaWatt = totalEnergy;
                }
                else
                {
                    beamRays.Add(new BeamRay() 
                    { 
                        energyInGigaWatt = availableEnergyInGigaWatt, 
                        cosConeAngle = cosConeAngle, 
                        spotsize = beamSpotSize, 
                        powerSourceToVesselVector = powerSourceToVesselVector 
                    });
                }
            }

            // old : F = 2 PA cos α cos α n
            var reflectedPhotonForceVector = vesselNormal * reflectedRadiationPressureOnSail * photonReflectionRatio * cosConeAngle;

            // calculate the vector at 90 degree angle in the direction of the vector
            //var tangantVector = (powerSourceToVesselVector - (Vector3.Dot(powerSourceToVesselVector, partNormal)) * partNormal).normalized;
            // new F = P A cos α [(1 + ρ ) cos α n − (1 − ρ ) sin α t] 
            // where P: solar radiation pressure, A: sail area, α: sail pitch angle, t: sail tangential vector, ρ: reflection coefficient
            //var effectiveForce = radiationPressureOnSail * ((1 + reflectedPhotonRatio) * cosConeAngle * partNormal - (1 - reflectedPhotonRatio) * Math.Sin(pitchAngleInRad) * tangantVector);

            // calculate acceleration from absorbed photons
            Vector3d photonAbsorptionVector = -(positionPowerSource - positionVessel).normalized;

            // calculate ratio of non reflected photons
            var absorbedPhotonsRatio = 1 - photonReflectionRatio;

            // add received heat to total heat load
            receivedHeatInWatt += energyOnSailnWatt * absorbedPhotonsRatio;

            // calculate force from absorbed photons
            var absorbedPhotonForce = reflectedRadiationPressureOnSail * 0.5 * absorbedPhotonsRatio;

            // calculate force vector from absorbed photons
            var absorbedPhotonForceVector = absorbedPhotonForce * photonAbsorptionVector;

            // calculate emissivity of both sides of the sail
            var totalEmissivity = absorbedPhotonsRatio + backsideEmissivity;

            // calculate percentage of energy leaving through the receiving side, accelerating the vessel
            var pushDissipationRatio = totalEmissivity > 0 ? Math.Max(0, absorbedPhotonsRatio / totalEmissivity) : 0;

            // calculate percentage of energy leaving through the backside, decelerating the vessel
            var dragDissipationRatio = totalEmissivity > 0 ? Math.Max(0, backsideEmissivity / totalEmissivity) : 0;

            // calculate equilibrium drag force from dissipation at back side
            var dissipationDragForceVector = -vesselNormal * absorbedPhotonForce * dragDissipationRatio;

            // calculate equilibrium drag force from dissipation at back side
            var dissipationPushForceVector = vesselNormal * absorbedPhotonForce * pushDissipationRatio;

            // calculate sum of all force vectors
            Vector3d totalForceVector = reflectedPhotonForceVector + absorbedPhotonForceVector + dissipationPushForceVector + dissipationDragForceVector;

            // calculate acceleration
            var totalAccelerationVector = vesselMassInKg > 0 ? totalForceVector / vesselMassInKg: Vector3d.zero;

            // all force
            ChangeVesselVelocity(vessel.orbit, this.vessel, universalTime, totalAccelerationVector * TimeWarp.fixedDeltaTime);

            // Update displayed force & acceleration
            var signedForce = totalForceVector.magnitude * Sign(cosConeAngleIsNegative);
            var signedAccel = totalAccelerationVector.magnitude * Sign(cosConeAngleIsNegative);

            if (isSun)
            {
                solar_force_d += signedForce;
                solar_acc_d += signedAccel;
            }
            else
            {
                receivedBeamedPowerList.Add(new ReceivedBeamedPower { pitchAngle = pitchAngleInDegree, receivedPower = energyOnSailnWatt * 1e-6, spotsize = beamSpotSize, cosConeAngle = cosConeAngle });
                beamedSailForce += signedForce;
                beamed_acc_d += signedAccel;
            }

            return availableEnergyInWatt;
        }

        private void UpdateChangeGui()
        {
            var averageFixedDeltaTime = ((double)(decimal)previousFixedDeltaTime + (double)(decimal)TimeWarp.fixedDeltaTime) / 2;

            periapsisChangeQueue.Enqueue((vessel.orbit.PeA - previousPeA) / averageFixedDeltaTime);
            if (periapsisChangeQueue.Count > 30)
                periapsisChangeQueue.Dequeue();
            periapsisChange = periapsisChangeQueue.Count > 20 
                ?  periapsisChangeQueue.OrderBy(m => m).Skip(5).Take(20).Average() 
                : periapsisChangeQueue.Average();

            apapsisChangeQueue.Enqueue((vessel.orbit.ApA - previousAeA) / averageFixedDeltaTime);
            if (apapsisChangeQueue.Count > 30)
                apapsisChangeQueue.Dequeue();
            apapsisChange = apapsisChangeQueue.Count > 20
                ? apapsisChangeQueue.OrderBy(m => m).Skip(5).Take(20).Average()
                : apapsisChangeQueue.Average();

            orbitSizeChange = periapsisChange + apapsisChange;

            previousPeA = vessel.orbit.PeA;
            previousAeA = vessel.orbit.ApA;
            previousFixedDeltaTime = TimeWarp.fixedDeltaTime;
        }

        private static int Sign(bool cosConeAngleIsNegative)
        {
            return cosConeAngleIsNegative ? -1 : 1;
        }

        private static void UpdateVisibleBeam(Part part, BeamEffect beamEffect, Vector3d powerSourceToVesselVector, double scaleModifer = 1, float beamSize = 1, double beamLength = 200000)
        {
            var normalizedPowerSourceToVesselVector = powerSourceToVesselVector.normalized;
            var endBeamPos = part.transform.position + normalizedPowerSourceToVesselVector * beamLength;
            var midPos = part.transform.position - endBeamPos;
            var timeCorrection = TimeWarp.CurrentRate > 1 ? -part.vessel.obt_velocity * (double)(decimal)TimeWarp.fixedDeltaTime : Vector3d.zero;

            var solarVectorX = normalizedPowerSourceToVesselVector.x * 90;
            var solarVectorY = normalizedPowerSourceToVesselVector.y * 90 - 90;
            var solarVectorZ = normalizedPowerSourceToVesselVector.z * 90;

            beamEffect.solar_effect.transform.localRotation = new Quaternion((float)solarVectorX, (float)solarVectorY, (float)solarVectorZ, 0);
            beamEffect.solar_effect.transform.localScale = new Vector3(beamSize, (float)(beamLength * scaleModifer), beamSize);
            beamEffect.solar_effect.transform.position = new Vector3((float)(part.transform.position.x + midPos.x + timeCorrection.x), (float)(part.transform.position.y + midPos.y + timeCorrection.y), (float)(part.transform.position.z + midPos.z + timeCorrection.z));
        }

        private static double solarFluxAtDistance(Vessel vessel, CelestialBody star, double luminosity)
        {
            var toStar = vessel.CoMD - star.position;
            var distanceToSurfaceStar = toStar.magnitude - star.Radius;
            var scaledDistance = 1 + Math.Min(1, distanceToSurfaceStar / star.Radius);
            var nearStarDistance = star.Radius * 0.25 * scaledDistance * scaledDistance;
            var distanceForEffectiveDistance = Math.Max(distanceToSurfaceStar, nearStarDistance);
            var kerbinSunDistance = distanceForEffectiveDistance / GameConstants.kerbin_sun_distance;
            return luminosity * PhysicsGlobals.SolarLuminosityAtHome / (kerbinSunDistance * kerbinSunDistance);
        }

        private static void RunAnimation(string animationName, Animation anim, float speed, float aTime)
        {
            if (anim == null || string.IsNullOrEmpty(animationName))
                return;

            anim[animationName].speed = speed;
            if (anim.IsPlaying(animationName))
                return;

            anim[animationName].wrapMode = WrapMode.Default;
            anim[animationName].normalizedTime = aTime;
            anim.Blend(animationName, 1);
        }

        public static bool LineOfSightToSun(Vector3d vesselPosition, CelestialBody star)
        {
            return LineOfSightToTransmitter(vesselPosition, star.position, star.name);
        }

        public static bool LineOfSightToTransmitter(Vector3d vesselPosition, Vector3d transmitterPosition, string ignoreBody = "")
        {
            Vector3d bminusa = transmitterPosition - vesselPosition;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                // the star should not block line of sight to the sun
                if (referenceBody.name == ignoreBody)
                    continue;

                Vector3d refminusa = referenceBody.position - vesselPosition;

                if (Vector3d.Dot(refminusa, bminusa) <= 0)
                    continue;

                var normalizedBminusa = bminusa.normalized;

                var cosReferenceSunNormB = Vector3d.Dot(refminusa, normalizedBminusa);

                if (cosReferenceSunNormB >= bminusa.magnitude)
                    continue;

                Vector3d tang = refminusa - cosReferenceSunNormB * normalizedBminusa;
                if (tang.magnitude < referenceBody.Radius)
                    return false;
            }
            return true;
        }

        public static int GetTechCost(string techid, int techbonus)
        {
            if (String.IsNullOrEmpty(techid) || techid == "none")
                return 0;

            var techstate = ResearchAndDevelopment.Instance.GetTechState(techid);
            if (techstate != null)
            {
                var available = techstate.state == RDTech.State.Available;

                if (available)
                    return techstate.scienceCost + techbonus;
            }

            return 0;
        }

        public static int HasTech(string techid, int increase)
        {
            return HasTech(techid) ? increase : 0;
        }

        public static bool HasTech(string techid)
        {
            return TechnologyHelper.UpgradeAvailable(techid);
        }

        public static bool HasUpgrade(string name)
        {
            if (HighLogic.CurrentGame == null) return true;
            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return true;
            if (PartUpgradeManager.Handler.IsUnlocked(name)) return true;

            return TechnologyHelper.UpgradeAvailable(name);
        }
    }
}
