using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class SolarWindCollector : ResourceSuppliableModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool bIsEnabled = false;
        [KSPField(isPersistant = true)]
        public double dLastActiveTime;
        [KSPField(isPersistant = true)]
        public double dLastPowerPercentage;
        [KSPField(isPersistant = true)]
        public double dLastMagnetoStrength;
        [KSPField(isPersistant = true)]
        public double dLastSolarConcentration;
        [KSPField(isPersistant = true)]
        public double dLastHydrogenConcentration;
        [KSPField(isPersistant = true)]
        protected bool bIsExtended = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Ionizing"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        protected bool bIonizing = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        protected float powerPercentage = 100;      

        // Part properties
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Surface area", guiUnits = " m\xB2")]
        public double surfaceArea = 0; // Surface area of the panel.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Magnetic area", guiUnits = " m\xB2")]
        public double magneticArea = 0; // Surface area of the panel.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Collector effectiveness", guiFormat = "P1")]
        public double effectiveness = 1; // Effectiveness of the panel. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Magnetic Power Requirements", guiUnits = " MW")]
        public double mwRequirements = 1; // MW requirements of the collector panel.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Ionisation Power Requirements", guiUnits = " MW")]
        public double ionRequirements = 100; // MW requirements of the collector panel.
        [KSPField(isPersistant = false)]
        public string animName = "";
        [KSPField(isPersistant = false)]
        public string ionAnimName = "";
        [KSPField(isPersistant = false)]
        public double solarCheatMultiplier = 1000;  // Amount of boosted Solar wind activity
        [KSPField(isPersistant = false)]
        public double interstellarCheatMultiplier = 100000;  // Amount of boosted Interstellar hydrogen activity
        [KSPField(isPersistant = false)]
        public double collectMultiplier = 1;

        // GUI
        [KSPField(guiActive = true, guiName = "Solar Wind Ions", guiUnits = " mol/m\xB2")]
        protected float fSolarWindConcentration;
        [KSPField(guiActive = true, guiName = "Interstellar Hydrogen", guiUnits = " mol/m\xB2")]
        protected float fInterstellarHydrogenConcentration;
        [KSPField(guiActive = true, guiName = "Atmosphere Particles", guiUnits = " mol/m\xB2")]
        protected float fAtmosphereConcentration;
        [KSPField(guiActive = true, guiName = "Neutral Atmospheric H", guiUnits = " mol/m\xB2")]
        protected float fNeutralHydrogenConcentration;
        [KSPField(guiActive = true, guiName = "Ionized Atmospheric H", guiUnits = " mol/m\xB2")]
        protected float fIonizedHydrogenConcentration;

        [KSPField(guiActive = true, guiName = "Atmospheric Drag", guiFormat="F4", guiUnits= " N")]
        protected double atmosphericDragInNewton;

        [KSPField(guiActive = false, guiName = "Distance from the sun")]
        protected string strStarDist = "";
        [KSPField(guiActive = true, guiName = "Status")]
        protected string strCollectingStatus = "";
        [KSPField(guiActive = true, guiName = "Power Usage")]
        protected string strReceivedPower = "";
        [KSPField(guiActive = true, guiName = "Magnetosphere shielding effect", guiUnits = " %")]
        protected string strMagnetoStrength = "";

        // internals
        float newNormalTime;
        double dWindResourceFlow = 0;
        double dHydrogenResourceFlow = 0;

        static FloatCurve particlesAtmosphereCurbeM;
        static FloatCurve particlesHydrogenCubeM;
        static FloatCurve hydrogenIonsCubeCM;

        [KSPEvent(guiActive = true, guiName = "Activate Collector", active = true)]
        public void ActivateCollector()
        {
            this.part.force_activate();
            bIsEnabled = true;
            OnUpdate();
            if (IsCollectLegal() == true)
            {
                UpdatePartAnimation();
            }
        }

        [KSPEvent(guiActive = true, guiName = "Disable Collector", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnUpdate();
            // folding nimation will only play if the collector was extended before being disabled
            if (bIsExtended == true)
            {
                UpdatePartAnimation();
            }
        }

        [KSPAction("Activate Collector")]
        public void ActivateScoopAction(KSPActionParam param)
        {
            ActivateCollector();
        }

        [KSPAction("Disable Collector")]
        public void DisableScoopAction(KSPActionParam param)
        {
            DisableCollector();
        }

        [KSPAction("Toggle Collector")]
        public void ToggleScoopAction(KSPActionParam param)
        {
            if (bIsEnabled)
                DisableCollector();
            else
                ActivateCollector();
        }

        double solarwindMolarMassConcentrationPerSquareMeterPerSecond = 0;
        double hydrogenMolarMassConcentrationPerSquareMeterPerSecond = 0;
        double dSolarWindSpareCapacity;
        double dHydrogenSpareCapacity;
        double dSolarWindDensity;
        double dHydrogenDensity;
        double dMagnetoSphereStrengthRatio = 0;
        double dShieldedEffectiveness = 0;
        float previousPowerPercentage;
        bool previosIonisationState = false;
        string strSolarWindResourceName;
        string strHydrogenResourceName;

        Animation deployAnimation;
        Animation ionisationAnimation;
        CelestialBody localStar;

        public override void OnStart(PartModule.StartState state)
        {
            // get the part's animation
            deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            ionisationAnimation = part.FindModelAnimators(ionAnimName).FirstOrDefault();
            previousPowerPercentage = powerPercentage;
            previosIonisationState = bIonizing;
            if (ionisationAnimation != null)
            {
                ionisationAnimation[ionAnimName].speed = 0;
                ionisationAnimation[ionAnimName].normalizedTime = bIonizing ? powerPercentage / 100 : 0; // normalizedTime at 1 is the end of the animation
                ionisationAnimation.Blend(ionAnimName);
            }

            if (state == StartState.Editor) return; // collecting won't work in editor

            InitializeAtmosphereParticles();

            localStar = GetCurrentStar();

            // get resource name solar wind
            strSolarWindResourceName = InterstellarResourcesConfiguration.Instance.SolarWind;
            strHydrogenResourceName = InterstellarResourcesConfiguration.Instance.Hydrogen;

            // gets density of resources
            dSolarWindDensity = PartResourceLibrary.Instance.GetDefinition(strSolarWindResourceName).density;
            dHydrogenDensity = PartResourceLibrary.Instance.GetDefinition(strHydrogenResourceName).density; ;

            // this bit goes through parts that contain animations and disables the "Status" field in GUI so that it's less crowded
            List<ModuleAnimateGeneric> MAGlist = part.FindModulesImplementing<ModuleAnimateGeneric>();
            foreach (ModuleAnimateGeneric MAG in MAGlist)
            {
                MAG.Fields["status"].guiActive = false;
                MAG.Fields["status"].guiActiveEditor = false;
            }

            // verify collector was enabled 
            if (!bIsEnabled) return;

            // verify a timestamp is available
            if (dLastActiveTime == 0) return;

            // verify any power was available in previous state
            if (dLastPowerPercentage < 0.01) return;

            // verify altitude is not too low
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)))
            {
                ScreenMessages.PostScreenMessage("Solar Wind Collection Error, vessel in atmosphere", 10, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // if the part should be extended (from last time), go to the extended animation
            if (bIsExtended && deployAnimation != null)
            {
                deployAnimation[animName].normalizedTime = 1;
            }

            // calculate time difference since last time the vessel was active
            double dTimeDifference = (Planetarium.GetUniversalTime() - dLastActiveTime) * 55;

            // collect solar wind for entire duration
            CollectSolarWind(dTimeDifference, true);
        }

        private static void InitializeAtmosphereParticles()
        {
            if (particlesAtmosphereCurbeM == null)
            {
                particlesAtmosphereCurbeM = new FloatCurve();
                particlesAtmosphereCurbeM.Add(0, 2.55e+25f);
                particlesAtmosphereCurbeM.Add(2, 2.09e+25f);
                particlesAtmosphereCurbeM.Add(4, 1.70e+25f);
                particlesAtmosphereCurbeM.Add(6, 1.37e+25f);
                particlesAtmosphereCurbeM.Add(8, 1.09e+25f);
                particlesAtmosphereCurbeM.Add(10, 8.60e+24f);
                particlesAtmosphereCurbeM.Add(12, 6.49e+24f);
                particlesAtmosphereCurbeM.Add(14, 4.74e+24f);
                particlesAtmosphereCurbeM.Add(16, 3.46e+24f);
                particlesAtmosphereCurbeM.Add(18, 2.53e+24f);
                particlesAtmosphereCurbeM.Add(20, 1.85e+24f);
                particlesAtmosphereCurbeM.Add(22, 1.34e+24f);
                particlesAtmosphereCurbeM.Add(24, 9.76e+23f);
                particlesAtmosphereCurbeM.Add(26, 7.12e+23f);
                particlesAtmosphereCurbeM.Add(28, 5.21e+23f);
                particlesAtmosphereCurbeM.Add(30, 3.83e+23f);
                particlesAtmosphereCurbeM.Add(32, 2.81e+23f);
                particlesAtmosphereCurbeM.Add(34, 2.06e+23f);
                particlesAtmosphereCurbeM.Add(36, 1.51e+23f);
                particlesAtmosphereCurbeM.Add(38, 1.12e+23f);
                particlesAtmosphereCurbeM.Add(40, 8.31e+22f);
                particlesAtmosphereCurbeM.Add(42, 6.23e+22f);
                particlesAtmosphereCurbeM.Add(44, 4.70e+22f);
                particlesAtmosphereCurbeM.Add(46, 3.56e+22f);
                particlesAtmosphereCurbeM.Add(48, 2.74e+22f);
                particlesAtmosphereCurbeM.Add(50, 2.14e+22f);
                particlesAtmosphereCurbeM.Add(52, 1.68e+22f);
                particlesAtmosphereCurbeM.Add(54, 1.33e+22f);
                particlesAtmosphereCurbeM.Add(56, 1.05e+22f);
                particlesAtmosphereCurbeM.Add(58, 8.24e+21f);
                particlesAtmosphereCurbeM.Add(60, 6.44e+21f);
                particlesAtmosphereCurbeM.Add(65, 3.39e+21f);
                particlesAtmosphereCurbeM.Add(70, 1.72e+21f);
                particlesAtmosphereCurbeM.Add(75, 8.30e+20f);
                particlesAtmosphereCurbeM.Add(80, 3.84e+20f);
                particlesAtmosphereCurbeM.Add(85, 1.71e+20f);
                particlesAtmosphereCurbeM.Add(90, 7.12e+19f);
                particlesAtmosphereCurbeM.Add(95, 2.92e+19f);
                particlesAtmosphereCurbeM.Add(100,  1.19e+19f);
                particlesAtmosphereCurbeM.Add(120,  5.11e+17f);
                particlesAtmosphereCurbeM.Add(140,  9.32e+16f);
                particlesAtmosphereCurbeM.Add(160,  3.16e+16f);
                particlesAtmosphereCurbeM.Add(180,  1.40e+16f);
                particlesAtmosphereCurbeM.Add(200,  7.18e+15f);
                particlesAtmosphereCurbeM.Add(300,  6.51e+14f);
                particlesAtmosphereCurbeM.Add(400,  9.13e+13f);
                particlesAtmosphereCurbeM.Add(500,  2.19e+13f);
                particlesAtmosphereCurbeM.Add(600,  4.89e+12f);
                particlesAtmosphereCurbeM.Add(700,  1.14e+12f);
                particlesAtmosphereCurbeM.Add(800,  5.86e+11f);
                particlesAtmosphereCurbeM.Add(1000, 2.06e+11f);
                particlesAtmosphereCurbeM.Add(2000, 1.03e+11f);
                particlesAtmosphereCurbeM.Add(4000, 5.16e+10f);
                particlesAtmosphereCurbeM.Add(8000, 2.58e+10f);
                particlesAtmosphereCurbeM.Add(16000, 0);
            }

            if (particlesHydrogenCubeM == null)
            {
                particlesHydrogenCubeM = new FloatCurve();
                particlesHydrogenCubeM.Add(0, 0);
                particlesHydrogenCubeM.Add(72.5f, 1.747e+9f);
                particlesHydrogenCubeM.Add(73.0f, 2.872e+9f);
                particlesHydrogenCubeM.Add(73.5f, 5.154e+9f);
                particlesHydrogenCubeM.Add(74.0f, 1.009e+10f);
                particlesHydrogenCubeM.Add(74.5f, 2.138e+10f);
                particlesHydrogenCubeM.Add(75.0f, 4.836e+10f);
                particlesHydrogenCubeM.Add(75.5f, 1.144e+11f);
                particlesHydrogenCubeM.Add(76.0f, 2.760e+11f);
                particlesHydrogenCubeM.Add(76.5f, 6.612e+11f);
                particlesHydrogenCubeM.Add(77.0f, 1.531e+12f);
                particlesHydrogenCubeM.Add(77.5f, 3.351e+12f);
                particlesHydrogenCubeM.Add(78.0f, 6.813e+12f);
                particlesHydrogenCubeM.Add(78.5f, 1.274e+13f);
                particlesHydrogenCubeM.Add(79.0f, 2.180e+13f);
                particlesHydrogenCubeM.Add(79.5f, 3.420e+13f);
                particlesHydrogenCubeM.Add(80.0f, 4.945e+13f);
                particlesHydrogenCubeM.Add(80.5f, 6.637e+13f);
                particlesHydrogenCubeM.Add(81.0f, 8.346e+13f);
                particlesHydrogenCubeM.Add(81.5f, 9.920e+13f);
                particlesHydrogenCubeM.Add(82.0f, 1.124e+14f);
                particlesHydrogenCubeM.Add(82.5f, 1.225e+14f);
                particlesHydrogenCubeM.Add(83.0f, 1.292e+14f);
                particlesHydrogenCubeM.Add(83.5f, 1.328e+14f);
                particlesHydrogenCubeM.Add(84.0f, 1.335e+14f);
                particlesHydrogenCubeM.Add(84.5f, 1.320e+14f);
                particlesHydrogenCubeM.Add(85.0f, 1.287e+14f);
                particlesHydrogenCubeM.Add(85.5f, 1.241e+14f);
                particlesHydrogenCubeM.Add(86.0f, 1.187e+14f);
                particlesHydrogenCubeM.Add(87, 1.066e+13f);
                particlesHydrogenCubeM.Add(88, 9.426e+13f);
                particlesHydrogenCubeM.Add(89, 8.257e+13f);
                particlesHydrogenCubeM.Add(90, 7.203e+13f);
                particlesHydrogenCubeM.Add(91, 6.276e+13f);
                particlesHydrogenCubeM.Add(92, 5.474e+13f);
                particlesHydrogenCubeM.Add(93, 4.786e+13f);
                particlesHydrogenCubeM.Add(94, 4.198e+13f);
                particlesHydrogenCubeM.Add(95, 3.698e+13f);
                particlesHydrogenCubeM.Add(96, 3.272e+13f);
                particlesHydrogenCubeM.Add(97, 2.909e+13f);
                particlesHydrogenCubeM.Add(98, 2.598e+13f);
                particlesHydrogenCubeM.Add(99, 2.332e+13f);
                particlesHydrogenCubeM.Add(100, 2.101e+13f);
                particlesHydrogenCubeM.Add(101, 1.901e+13f);
                particlesHydrogenCubeM.Add(102, 1.726e+13f);
                particlesHydrogenCubeM.Add(103, 1.572e+13f);
                particlesHydrogenCubeM.Add(104, 1.435e+13f);
                particlesHydrogenCubeM.Add(105, 1.313e+13f);
                particlesHydrogenCubeM.Add(106, 1.203e+13f);
                particlesHydrogenCubeM.Add(107, 1.104e+13f);
                particlesHydrogenCubeM.Add(108, 1.013e+13f);
                particlesHydrogenCubeM.Add(109, 9.299e+13f);
                particlesHydrogenCubeM.Add(110, 8.534e+12f);
                particlesHydrogenCubeM.Add(111, 7.827e+12f);
                particlesHydrogenCubeM.Add(112, 7.173e+12f);
                particlesHydrogenCubeM.Add(113, 6.569e+12f);
                particlesHydrogenCubeM.Add(114, 6.012e+12f);
                particlesHydrogenCubeM.Add(115, 5.500e+12f);
                particlesHydrogenCubeM.Add(120, 3.551e+12f);
                particlesHydrogenCubeM.Add(125, 2.477e+12f);
                particlesHydrogenCubeM.Add(130, 1.805e+12f);
                particlesHydrogenCubeM.Add(140, 1.029e+12f);
                particlesHydrogenCubeM.Add(150, 6.468e+11f);
                particlesHydrogenCubeM.Add(160, 4.485e+11f);
                particlesHydrogenCubeM.Add(170, 3.400e+11f);
                particlesHydrogenCubeM.Add(180, 2.774e+11f);
                particlesHydrogenCubeM.Add(190, 2.394e+11f);
                particlesHydrogenCubeM.Add(200, 2.154e+11f);
                particlesHydrogenCubeM.Add(210, 1.995e+11f);
                particlesHydrogenCubeM.Add(220, 1.887e+11f);
                particlesHydrogenCubeM.Add(230, 1.809e+11f);
                particlesHydrogenCubeM.Add(240, 1.752e+11f);
                particlesHydrogenCubeM.Add(250, 1.707e+11f);
                particlesHydrogenCubeM.Add(300, 1.569e+11f);
                particlesHydrogenCubeM.Add(350, 1.477e+11f);
                particlesHydrogenCubeM.Add(400, 1.399e+11f);
                particlesHydrogenCubeM.Add(450, 1.327e+11f);
                particlesHydrogenCubeM.Add(500, 1.260e+11f);
                particlesHydrogenCubeM.Add(550, 1.198e+11f);
                particlesHydrogenCubeM.Add(600, 1.139e+11f);
                particlesHydrogenCubeM.Add(650, 1.085e+11f);
                particlesHydrogenCubeM.Add(700, 1.033e+11f);
                particlesHydrogenCubeM.Add(750, 9.848e+10f);
                particlesHydrogenCubeM.Add(800, 9.393e+10f);
                particlesHydrogenCubeM.Add(850, 8.965e+10f);
                particlesHydrogenCubeM.Add(900, 8.562e+10f);
                particlesHydrogenCubeM.Add(950, 8.182e+10f);
                particlesHydrogenCubeM.Add(1000, 7.824e+10f);
                particlesHydrogenCubeM.Add(2000, 3.912e+10f);
                particlesHydrogenCubeM.Add(4000, 1.956e+10f);
                particlesHydrogenCubeM.Add(8000, 9.780e+9f);
                particlesHydrogenCubeM.Add(16000, 0);
            }

            if ( hydrogenIonsCubeCM == null)
            {
                hydrogenIonsCubeCM = new FloatCurve();
                hydrogenIonsCubeCM.Add(0, 0);
                hydrogenIonsCubeCM.Add(284, 0);
                hydrogenIonsCubeCM.Add(285, 1.00e+8f);
                hydrogenIonsCubeCM.Add(299, 2.49e+8f);
                hydrogenIonsCubeCM.Add(312, 4.40e+8f);
                hydrogenIonsCubeCM.Add(334, 6.34e+8f);
                hydrogenIonsCubeCM.Add(359, 8.83e+8f);
                hydrogenIonsCubeCM.Add(377, 1.11e+9f);
                hydrogenIonsCubeCM.Add(396, 1.26e+9f);
                hydrogenIonsCubeCM.Add(411, 1.36e+9f);
                hydrogenIonsCubeCM.Add(437, 1.51e+9f);
                hydrogenIonsCubeCM.Add(464, 1.65e+9f);
                hydrogenIonsCubeCM.Add(504, 1.78e+9f);
                hydrogenIonsCubeCM.Add(536, 1.86e+9f);
                hydrogenIonsCubeCM.Add(588, 2.00e+9f);
                hydrogenIonsCubeCM.Add(641, 2.20e+9f);
                hydrogenIonsCubeCM.Add(678, 2.40e+9f);
                hydrogenIonsCubeCM.Add(715, 2.64e+9f);
                hydrogenIonsCubeCM.Add(754, 2.94e+9f);
                hydrogenIonsCubeCM.Add(793, 3.33e+9f);
                hydrogenIonsCubeCM.Add(829, 3.76e+9f);
                hydrogenIonsCubeCM.Add(864, 4.26e+9f);
                hydrogenIonsCubeCM.Add(904, 4.97e+9f);
                hydrogenIonsCubeCM.Add(939, 5.72e+9f);
                hydrogenIonsCubeCM.Add(968, 6.43e+9f);
                hydrogenIonsCubeCM.Add(992, 7.07e+9f);
                hydrogenIonsCubeCM.Add(1010, 7.61e+9f);
                hydrogenIonsCubeCM.Add(1040, 7.95e+9f);
                hydrogenIonsCubeCM.Add(1070, 8.15e+9f);
                hydrogenIonsCubeCM.Add(1100, 8.13e+9f);
                hydrogenIonsCubeCM.Add(1140, 7.89e+9f);
                hydrogenIonsCubeCM.Add(1170, 7.57e+9f);
                hydrogenIonsCubeCM.Add(1190, 7.21e+9f);
                hydrogenIonsCubeCM.Add(1240, 6.46e+9f);
                hydrogenIonsCubeCM.Add(2000, 3.8e+9f);
                hydrogenIonsCubeCM.Add(4000, 1.9e+9f);
                hydrogenIonsCubeCM.Add(8000, 9.5e+8f);
                hydrogenIonsCubeCM.Add(16000, 0);
            }
        }


        public override void OnUpdate()
        {
            Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled

            Fields["strReceivedPower"].guiActive = bIsEnabled;           

            var dSolarWindConcentration = CalculateSolarwindIonConcentration(part.vessel.solarFlux, solarCheatMultiplier);
            var dInterstellarHydrogenConcentration = CalculateInterstellarIonConcentration(vessel.obt_speed, interstellarCheatMultiplier);
            solarwindMolarMassConcentrationPerSquareMeterPerSecond = dSolarWindConcentration + (bIonizing ? dInterstellarHydrogenConcentration : 0);

            var dAtmosphereConcentration = CalculateCurrentAtmosphereConcentration(vessel);
            var dAtmosphericHydrogenConcentration = CalculateCurrentHydrogenConcentration(vessel);
            var dIonizedHydrogenConcentration = CalculateCurrentHydrogenIonsConcentration(vessel);
            hydrogenMolarMassConcentrationPerSquareMeterPerSecond = bIonizing ? dAtmosphericHydrogenConcentration : dIonizedHydrogenConcentration;

            fSolarWindConcentration = (float)dSolarWindConcentration;
            fInterstellarHydrogenConcentration = (float)dInterstellarHydrogenConcentration;
            fAtmosphereConcentration = (float)dAtmosphereConcentration;
            fNeutralHydrogenConcentration = (float)dAtmosphericHydrogenConcentration;
            fIonizedHydrogenConcentration = (float)dIonizedHydrogenConcentration;

            dMagnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));
            strMagnetoStrength = UpdateMagnetoStrengthInGUI();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            UpdateIonisationAnimation();

            if (FlightGlobals.fetch != null)
            {
                if (!bIsEnabled)
                {
                    strCollectingStatus = "Disabled";
                    strStarDist = UpdateDistanceInGUI(); // passes the distance to the GUI
                    return;
                }

                // won't collect in atmosphere
                if (IsCollectLegal() == false)
                {
                    DisableCollector();
                    return;
                }

                strStarDist = UpdateDistanceInGUI();

                // collect solar wind for a single frame
                CollectSolarWind(TimeWarp.fixedDeltaTime, false);

                // store current time in case vesel is unloaded
                dLastActiveTime = Planetarium.GetUniversalTime();
                
                // store current strength of the magnetic field in case vessel is unloaded
                dLastMagnetoStrength = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

                // store current solar wind concentration in case vessel is unloaded
                dLastSolarConcentration = solarwindMolarMassConcentrationPerSquareMeterPerSecond; //CalculateSolarWindConcentration(part.vessel.solarFlux);
                dLastHydrogenConcentration = hydrogenMolarMassConcentrationPerSquareMeterPerSecond;
            }
        }

        /** 
         * This function should allow this module to work in solar systems other than the vanilla KSP one as well. Credit to Freethinker's MicrowavePowerReceiver code.
         * It checks current reference body's temperature at 0 altitude. If it is less than 2k K, it checks this body's reference body next and so on.
         */
        protected CelestialBody GetCurrentStar()
        {
            int iDepth = 0;
            var star = FlightGlobals.currentMainBody;
            while ((iDepth < 10) && (star.GetTemperature(0) < 2000))
            {
                star = star.referenceBody;
                iDepth++;
            }
            if ((star.GetTemperature(0) < 2000) || (star.name == "Galactic Core"))
                star = null;

            return star;
        }

        /* Calculates the strength of the magnetosphere. Will return 1 if in atmosphere, otherwise a ratio of max atmospheric altitude to current 
         * altitude - so the ratio slowly lowers the higher the vessel is. Once above 10 times the max atmo altitude, 
         * it returns 0 (we consider this to be the end of the magnetosphere's reach). The atmospheric check is there to make the GUI less messy.
        */
        private static double GetMagnetosphereRatio(double altitude, double maxatmoaltitude)
        {
            double dRatio; // helper double for this function

            // atmospheric check for the sake of GUI
            if (altitude <= maxatmoaltitude)
            {
                dRatio = 1;
                return dRatio;
            }
            else
                dRatio = (altitude < (maxatmoaltitude * 10)) ? maxatmoaltitude / altitude : 0;
            return dRatio;
        }

        // checks if the vessel is not in atmosphere and if it can therefore collect solar wind. Could incorporate other checks if needed.
        private bool IsCollectLegal()
        {
            bool bCanCollect = false;

            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody))) // won't collect in atmosphere
            {
                ScreenMessages.PostScreenMessage("Solar wind collection not possible in atmosphere", 10, ScreenMessageStyle.LOWER_CENTER);
                strStarDist = UpdateDistanceInGUI();
                fSolarWindConcentration = 0;
                return bCanCollect;
            }
            else
                return bCanCollect = true;
        }

        private void UpdateIonisationAnimation()
        {
            if (!bIonizing)
            {
                previousPowerPercentage = powerPercentage;
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = 0;
                    ionisationAnimation.Blend(ionAnimName);
                }
                return;
            }

            if (powerPercentage < previousPowerPercentage)
            {
                newNormalTime = Math.Min(Math.Max(powerPercentage / 100, previousPowerPercentage / 100 - TimeWarp.fixedDeltaTime / 2), 1);
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = newNormalTime;
                    ionisationAnimation.Blend(ionAnimName);
                }
                previousPowerPercentage = newNormalTime * 100;
            }
            else if (powerPercentage > previousPowerPercentage)
            {
                newNormalTime = Math.Min(Math.Max(0, previousPowerPercentage / 100 + TimeWarp.fixedDeltaTime / 2), powerPercentage / 100);
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = newNormalTime;
                    ionisationAnimation.Blend(ionAnimName);
                }
                previousPowerPercentage = newNormalTime * 100;
            }
            else
            {
                if (ionisationAnimation != null)
                {
                    ionisationAnimation[ionAnimName].speed = 0;
                    ionisationAnimation[ionAnimName].normalizedTime = powerPercentage / 100;
                    ionisationAnimation.Blend(ionAnimName);
                }
            }
        }

        private void UpdatePartAnimation()
        {
            // if extended, plays the part folding animation
            if (bIsExtended)
            {
                if (deployAnimation != null)
                {
                    deployAnimation[animName].speed = -1; // speed of 1 is normal playback, -1 is reverse playback (so in this case we go from the end of animation backwards)
                    deployAnimation[animName].normalizedTime = 1; // normalizedTime at 1 is the end of the animation
                    deployAnimation.Blend(animName, part.mass);
                }
                bIsExtended = false;
            }
            else
            {
                // if folded, plays the part extending animation
                if (deployAnimation != null)
                {
                    deployAnimation[animName].speed = 1;
                    deployAnimation[animName].normalizedTime = 0; // normalizedTime at 0 is the start of the animation
                    deployAnimation.Blend(animName, part.mass);
                }
                bIsExtended = true;
            }
        }

        // calculates solar wind concentration
        private static double CalculateSolarwindIonConcentration(double flux, double solarCheatMultiplier)
        {
            var dAvgKerbinSolarFlux = 1409.285; // this seems to be the average flux at Kerbin just above the atmosphere (from my tests)
            var dAvgSolarWindPerCubM = 6 * 1000000; // various sources differ, most state that there are around 6 particles per cm^3, so around 6000000 per m^3 (some sources go up to 10/cm^3 or even down to 2/cm^3, most are around 6/cm^3).


            const double solarWindSpeed = 500000; // Average Solar win speed 500 km/s
            const double avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol 

            double dMolalSolarConcentration = (flux / dAvgKerbinSolarFlux) * dAvgSolarWindPerCubM * solarWindSpeed * solarCheatMultiplier / avogadroConstant;

            return dMolalSolarConcentration; // in mol / m2 / sec
        }

        private static double CalculateInterstellarIonConcentration(double vesselSpeed, double interstellarCheatMultiplier)
        {
            const double  dAverageInterstellarHydrogenPerCubM = 1 * 1000000;
            const double avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol

            var interstellarHydrogenConcentration = dAverageInterstellarHydrogenPerCubM * interstellarCheatMultiplier * vesselSpeed / avogadroConstant;

            return interstellarHydrogenConcentration; // in mol / m2 / sec
        }

        private static double CalculateCurrentAtmosphereConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereParticlesPerCubM = Math.Max(0, particlesAtmosphereCurbeM.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / 101.325);

            const double avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol

            var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / avogadroConstant;

            return atmosphereConcentration;
        }

        private static double CalculateCurrentHydrogenConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereParticlesPerCubM = Math.Max(0, particlesHydrogenCubeM.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / 101.325);

            const double avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol

            var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / avogadroConstant;

            return atmosphereConcentration;
        }

        private static double CalculateCurrentHydrogenIonsConcentration(Vessel vessel)
        {
            if (!vessel.mainBody.atmosphere)
                return 0;

            var comparibleEarthAltitudeInKm = vessel.altitude / vessel.mainBody.atmosphereDepth * 84;
            var atmosphereParticlesPerCubM = Math.Max(0, hydrogenIonsCubeCM.Evaluate((float)comparibleEarthAltitudeInKm)) * (vessel.mainBody.atmospherePressureSeaLevel / 101.325);

            const double avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol

            var atmosphereConcentration = atmosphereParticlesPerCubM * vessel.obt_speed / avogadroConstant;

            return atmosphereConcentration;
        }

        // calculates the distance to sun
        private static double CalculateDistanceToSun(Vector3d vesselPosition, Vector3d sunPosition)
        {
            return Vector3d.Distance(vesselPosition, sunPosition);
        }

        // helper function for readying the distance for the GUI
        private string UpdateDistanceInGUI()
        {
            return ((CalculateDistanceToSun(part.transform.position, localStar.transform.position) - localStar.Radius) / 1000).ToString("F0") + " km";
        }

        private string UpdateMagnetoStrengthInGUI()
        {
            return (GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody))* 100).ToString("F1");
        }

        // the main collecting function
        private void CollectSolarWind(double deltaTimeInSeconds, bool offlineCollecting)
        {
            var ionizationPowerCost =  bIonizing ? ionRequirements *  Math.Pow(powerPercentage / 100d, 2) : 0;

            var magneticPowerCost = mwRequirements * Math.Pow(powerPercentage / 100d, 2);

            var dPowerRequirementsMW = PluginHelper.PowerConsumptionMultiplier * (magneticPowerCost + ionizationPowerCost); // change the mwRequirements number in part config to change the power consumption

            // checks for free space in solar wind 'tanks'
            dSolarWindSpareCapacity = part.GetResourceSpareCapacity(strSolarWindResourceName);
            dHydrogenSpareCapacity = part.GetResourceSpareCapacity(strHydrogenResourceName);

            if (offlineCollecting)
            {
                solarwindMolarMassConcentrationPerSquareMeterPerSecond = dLastSolarConcentration; // if resolving offline collection, pass the saved value, because OnStart doesn't resolve the function at line 328
                hydrogenMolarMassConcentrationPerSquareMeterPerSecond = dLastHydrogenConcentration;
            }

            if ((solarwindMolarMassConcentrationPerSquareMeterPerSecond > 0 || hydrogenMolarMassConcentrationPerSquareMeterPerSecond > 0) && (dSolarWindSpareCapacity > 0 || dHydrogenSpareCapacity > 0))
            {
                // calculate available power
                var dNormalisedRevievedPowerMW = Math.Max(consumeFNResourcePerSecond(dPowerRequirementsMW, ResourceManager.FNRESOURCE_MEGAJOULES), 0);

                // if power requirement sufficiently low, retreive power from KW source
                if (dPowerRequirementsMW < 2 && dNormalisedRevievedPowerMW <= dPowerRequirementsMW)
                {
                    var dRequiredKW = (dPowerRequirementsMW - dNormalisedRevievedPowerMW) * 1000;
                    var dReceivedKW = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, dRequiredKW * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                    dNormalisedRevievedPowerMW += (dReceivedKW / 1000);
                }

                dLastPowerPercentage = offlineCollecting ? dLastPowerPercentage : (dNormalisedRevievedPowerMW / dPowerRequirementsMW);

                // show in GUI
                strCollectingStatus = "Collecting solar wind";
            }
            else
            {
                dLastHydrogenConcentration = 0;
                dLastPowerPercentage = 0;
                dPowerRequirementsMW = 0;
            }

            // set the GUI string to state the number of KWs received if the MW requirements were lower than 2, otherwise in MW
            strReceivedPower = dPowerRequirementsMW < 2
                ? (dLastPowerPercentage * dPowerRequirementsMW * 1000).ToString("0.0") + " KW / " + (dPowerRequirementsMW * 1000).ToString("0.0") + " KW"
                : (dLastPowerPercentage * dPowerRequirementsMW).ToString("0.0") + " MW / " + dPowerRequirementsMW.ToString("0.0") + " MW";

            // get the shielding effect provided by the magnetosphere
            dMagnetoSphereStrengthRatio = GetMagnetosphereRatio(vessel.altitude, PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody));

            // if online collecting, get the old values instead (simplification for the time being)
            if (offlineCollecting)
                dMagnetoSphereStrengthRatio = dLastMagnetoStrength;

            if (dMagnetoSphereStrengthRatio == 0)
                dShieldedEffectiveness = 1;
            else
                dShieldedEffectiveness = (1 - dMagnetoSphereStrengthRatio);

            var effectiveSurfaceArea = surfaceArea + (magneticArea * powerPercentage / 100);

            var production = collectMultiplier * effectiveSurfaceArea * effectiveness * dShieldedEffectiveness * dLastPowerPercentage * deltaTimeInSeconds;

            /** The first important bit.
             * This determines how much solar wind will be collected. Can be tweaked in part configs by changing the collector's effectiveness.
             * */
            double dSolarWindResourceChange = solarwindMolarMassConcentrationPerSquareMeterPerSecond * 1.9 / 1e-6 * production / dSolarWindDensity;

            // if the vessel has been out of focus, print out the collected amount for the player
            if (offlineCollecting)
            {
                string strNumberFormat = dSolarWindResourceChange > 100 ? "0" : "0.00";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("We collected " + dSolarWindResourceChange.ToString(strNumberFormat) + " units of " + strSolarWindResourceName, 10, ScreenMessageStyle.LOWER_CENTER);
            }

            // this is the second important bit - do the actual change of the resource amount in the vessel
            dWindResourceFlow = -part.RequestResource(strSolarWindResourceName, -dSolarWindResourceChange);

            double dHydrogenResourceChange = hydrogenMolarMassConcentrationPerSquareMeterPerSecond / 1e-6 * production / dHydrogenDensity;

            if (offlineCollecting)
            {
                string strNumberFormat = dHydrogenResourceChange > 100 ? "0" : "0.00";
                // let the player know that offline collecting worked
                ScreenMessages.PostScreenMessage("We collected " + dHydrogenResourceChange.ToString(strNumberFormat) + " units of " + strHydrogenResourceName, 10, ScreenMessageStyle.LOWER_CENTER);
            }

            dHydrogenResourceFlow = -part.RequestResource(strSolarWindResourceName, -dHydrogenResourceChange);


            var atmosphericDensityKgPerSquareMeter = hydrogenMolarMassConcentrationPerSquareMeterPerSecond * 1e-3;

            atmosphericDragInNewton = 0.5 * effectiveSurfaceArea * atmosphericDensityKgPerSquareMeter * vessel.obt_speed * vessel.obt_speed;

            if (!this.vessel.packed)
            {
                part.Rigidbody.AddForce(part.vessel.velocityD.normalized * -(float)atmosphericDragInNewton * 1e-3, ForceMode.Force);
            }
        }

    }
}

