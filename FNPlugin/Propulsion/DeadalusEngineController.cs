using FNPlugin.Extensions;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FNPlugin
{
    [KSPModule("Fusion Engine")]
    class FusionEngineController : DaedalusEngineController { }

    [KSPModule("Inertial Confinement Fusion Engine")]
    class DaedalusEngineController : ResourceSuppliableModule, IUpgradeableModule 
    {
        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public bool rad_safety_features = true;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_speedLimit", guiUnits = "c"), UI_FloatRange(stepIncrement = 1 / 3f, maxValue = 1, minValue = 1 / 3f)]
        public float speedLimit = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_fuelLimit", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        public float fuelLimit = 100;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_maximizeThrust"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool maximizeThrust = true;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_powerUsage")]
        public string powerUsage;
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_fusionFuel")]
        public string fusionFuel = "FusionPellets";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_temperatureStr")]
        public string temperatureStr = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_speedOfLight", guiUnits = " m/s")]
        public double speedOfLight;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_relativity", guiFormat = "F10")]
        public double relativity;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")]
        public double timeDilation;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_radhazardstr")]
        public string radhazardstr = "";
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiUnits = " t")]
        public float partMass = 1;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fusionRatio", guiFormat = "F6")]
        public double fusionRatio = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsRatio")]
        public string fuelAmountsRatio;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_thrustPowerInTeraWatt", guiFormat = "F2", guiUnits = " TW")]
        public double thrustPowerInTeraWatt = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_calculatedFuelflow", guiFormat = "F6", guiUnits = " U")]
        public double calculatedFuelflow = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateKgPerSecond", guiFormat = "F6", guiUnits = " kg/s")]
        public double massFlowRateKgPerSecond;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateTonPerHour", guiFormat = "F6", guiUnits = " t/h")]
        public double massFlowRateTonPerHour;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_storedThrotle")]
        public float storedThrotle = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveMaxThrustInKiloNewton", guiFormat = "F2", guiUnits = " kN")]
        public double effectiveMaxThrustInKiloNewton = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveIsp", guiFormat = "F2", guiUnits = "s")]
        public double effectiveIsp = 0;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_worldSpaceVelocity", guiFormat = "F3", guiUnits = " m/s")]
        public double worldSpaceVelocity;

        [KSPField]
        public double universalTime;
        [KSPField]
        public float powerRequirement = 2500;

        [KSPField]
        public float maxThrust = 0;
        [KSPField]
        public float maxThrustUpgraded = 1200;

        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk1 = 300;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk2 = 500;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk3 = 800;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk4 = 1200;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk5 = 1500;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk6 = 2000;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk7 = 2500;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustMk8 = 3000;

        [KSPField]
        public float maxAtmosphereDensity = 0;
        [KSPField]
        public double efficiency = 0.25;
        [KSPField]
        public double efficiencyUpgraded = 0.5;
        [KSPField]
        public float leathalDistance = 2000;
        [KSPField]
        public float killDivider = 50;
        [KSPField]
        public float fusionWasteHeat = 2500;
        [KSPField]
        public float fusionWasteHeatUpgraded = 10000;
        [KSPField]
        public float wasteHeatMultiplier = 1;
        [KSPField]
        public float powerRequirementMultiplier = 1;
        [KSPField]
        public float maxTemp = 3200;
        [KSPField]
        public int powerPriority = 4;
        [KSPField]
        public float upgradeCost = 100;
        [KSPField]
        public string originalName = "Prototype Deadalus IC Fusion Engine";
        [KSPField]
        public string upgradedName = "Deadalus IC Fusion Engine";

        // Gui
        [KSPField(guiActiveEditor = true, guiName= "upgrade tech")]
        public string upgradeTechReq = null;

        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 1")]
        public string upgradeTechReq1;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 2")]
        public string upgradeTechReq2;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 3")]
        public string upgradeTechReq3;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 4")]
        public string upgradeTechReq4;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 5")]
        public string upgradeTechReq5;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 6")]
        public string upgradeTechReq6;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 7")]
        public string upgradeTechReq7;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 8")]
        public string upgradeTechReq8;

        bool hasrequiredupgrade;
        bool radhazard;
        bool warpToReal;
        float engineIsp;
        double percentageFuelRemaining;

        Stopwatch stopWatch;
        ModuleEngines curEngineT;
        BaseEvent deactivateRadSafetyEvent;
        BaseEvent activateRadSafetyEvent;
        BaseEvent retrofitEngineEvent;
        BaseField radhazardstrField;
        PartResourceDefinition fusionFuelResourceDefinition;

        private int _engineGenerationType;
        public GenerationType EngineGenerationType
        {
            get { return (GenerationType) _engineGenerationType; }
            private set { _engineGenerationType = (int) value; }
        }

        [KSPEvent(guiActive = true, guiName = "Disable Radiation Safety", active = true)]
        public void DeactivateRadSafety() 
        {
            rad_safety_features = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Radiation Safety", active = false)]
        public void ActivateRadSafety() 
        {
            rad_safety_features = true;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        private double Efficiency { get { return isupgraded ? efficiencyUpgraded : efficiency; } }

        private float MaximumThrust
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1:
                        return maxThrustMk1 + maxThrust;
                        break;
                    case (int)GenerationType.Mk2:
                        return maxThrustMk2 + maxThrustUpgraded;
                        break;
                    case (int)GenerationType.Mk3:
                        return maxThrustMk3;
                        break;
                    case (int)GenerationType.Mk4:
                        return maxThrustMk4;
                        break;
                    case (int)GenerationType.Mk5:
                        return maxThrustMk5;
                        break;
                    case (int)GenerationType.Mk6:
                        return maxThrustMk6;
                        break;
                    case (int)GenerationType.Mk7:
                        return maxThrustMk7;
                        break;
                    default:
                        return maxThrustMk8;
                        break;
                }
            }
        }
        private float FusionWasteHeat { get { return isupgraded ? fusionWasteHeatUpgraded : fusionWasteHeat; } }

        private double EffectivePowerRequirement
        {
            get
            {
                return powerRequirement * powerRequirementMultiplier;
            }
        }

        public void upgradePartModule()
        {
            isupgraded = true;
        }

        #endregion

        public override void OnStart(StartState state) 
        {
            try
            {
                stopWatch = new Stopwatch();
                speedOfLight = GameConstants.speedOfLight * PluginHelper.SpeedOfLightMult;
                fusionFuelResourceDefinition = PartResourceLibrary.Instance.GetDefinition(fusionFuel);

                part.maxTemp = maxTemp;
                part.thermalMass = 1;
                part.thermalMassModifier = 1;

                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();

                if (curEngineT == null) return;

                engineIsp = curEngineT.atmosphereCurve.Evaluate(0);

                // if we can upgrade, let's do so
                if (isupgraded)
                    upgradePartModule();
                else if (this.HasTechsRequiredToUpgrade())
                    hasrequiredupgrade = true;

                if (state == StartState.Editor && this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }

                DetermineTechLevel();

                // bind with fields and events
                deactivateRadSafetyEvent = Events["DeactivateRadSafety"];
                activateRadSafetyEvent = Events["ActivateRadSafety"];
                retrofitEngineEvent = Events["RetrofitEngine"];
                radhazardstrField = Fields["radhazardstr"];

                Fields["upgradeTechReq1"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq1);
                Fields["upgradeTechReq2"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq2);
                Fields["upgradeTechReq3"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq3);
                Fields["upgradeTechReq4"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq4);
                Fields["upgradeTechReq5"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq5);
                Fields["upgradeTechReq6"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq6);
                Fields["upgradeTechReq7"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq7);
                Fields["upgradeTechReq8"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq8);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnStart " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void DetermineTechLevel()
        {
            var numberOfUpgradeTechs = 0;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq1))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq2))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq3))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq4))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq5))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq6))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq7))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq8))
                numberOfUpgradeTechs++;

            EngineGenerationType = (GenerationType) numberOfUpgradeTechs;
        }

        public void Update()
        {
            try
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    powerUsage = (EffectivePowerRequirement / 1000d).ToString("0.000") + " GW";
                    return;
                }

                double fusionFuelCurrentAmount;
                double fusionFuelMaxAmount;
                part.GetConnectedResourceTotals(fusionFuelResourceDefinition.id, out fusionFuelCurrentAmount, out fusionFuelMaxAmount);

                percentageFuelRemaining = fusionFuelCurrentAmount / fusionFuelMaxAmount * 100;
                fuelAmountsRatio = percentageFuelRemaining.ToString("0.0000") + "% " + fusionFuelMaxAmount.ToString("0") + " L";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error Update " + e.Message + " stack " + e.StackTrace);
            }
        }

        public override void OnUpdate() 
        {
            try
            {
                if (curEngineT == null) return;

                // When transitioning from timewarp to real update throttle
                if (warpToReal)
                {
                    vessel.ctrlState.mainThrottle = storedThrotle;
                    warpToReal = false;
                }

                deactivateRadSafetyEvent.active = rad_safety_features;
                activateRadSafetyEvent.active = !rad_safety_features;
                retrofitEngineEvent.active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;

                if (curEngineT.isOperational && !IsEnabled)
                {
                    IsEnabled = true;
                    part.force_activate();
                }

                var kerbalHazardCount = 0;
                foreach (var vess in FlightGlobals.Vessels)
                {
                    var distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);
                    if (distance < leathalDistance && vess != this.vessel)
                        kerbalHazardCount += vess.GetCrewCount();
                }

                if (kerbalHazardCount > 0)
                {
                    radhazard = true;
                    if (kerbalHazardCount > 1)
                        radhazardstr = kerbalHazardCount + " Kerbals.";
                    else
                        radhazardstr = kerbalHazardCount + " Kerbal.";

                    radhazardstrField.guiActive = true;
                }
                else
                {
                    radhazardstrField.guiActive = false;
                    radhazard = false;
                    radhazardstr = "None.";
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnUpdate " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void ShutDown(string reason)
        {
            try
            {
                curEngineT.Events["Shutdown"].Invoke();
                curEngineT.currentThrottle = 0;
                curEngineT.requestedThrottle = 0;

                ScreenMessages.PostScreenMessage(reason, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                foreach (var fxGroup in part.fxGroups)
                {
                    fxGroup.setActive(false);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error ShutDown " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void CalculateTimeDialation()
        {
            try
            {
                worldSpaceVelocity = vessel.orbit.GetFrameVel().magnitude;

                lightSpeedRatio = Math.Min(worldSpaceVelocity / speedOfLight, 0.9999999999);

                timeDilation = Math.Sqrt(1 - (lightSpeedRatio * lightSpeedRatio));

                relativity = 1 / timeDilation;                
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error CalculateTimeDialation " + e.Message + " stack " + e.StackTrace);
            }
        }

        public void FixedUpdate()
        {
            try
            {
                if (HighLogic.LoadedSceneIsEditor)
                    return;

                if (!IsEnabled)
                    UpdateTime();

                temperatureStr = part.temperature.ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error FixedUpdate " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void UpdateTime()
        {
            try
            {
                universalTime = Planetarium.GetUniversalTime();
                CalculateTimeDialation();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error UpdateTime " + e.Message + " stack " + e.StackTrace);
            }
        }

        public override void OnFixedUpdate()
        {
            try
            {
                if (curEngineT == null) return;

                stopWatch.Reset();
                stopWatch.Start();

                UpdateTime();

                var throttle = curEngineT.currentThrottle > 0 ? Mathf.Max(curEngineT.currentThrottle, 0.01f) : 0;

                if (throttle > 0)
                {
                    if (vessel.atmDensity > maxAtmosphereDensity)
                        ShutDown("Inertial Fusion cannot operate in atmosphere!");

                    if (radhazard && rad_safety_features)
                        ShutDown("Engines throttled down as they presently pose a radiation hazard");
                }

                KillKerbalsWithRadiation(throttle);

                if (!this.vessel.packed && !warpToReal)
                    storedThrotle = vessel.ctrlState.mainThrottle;

                // Update ISP
                effectiveIsp = timeDilation * engineIsp;

                UpdateAtmosphericCurve(effectiveIsp);

                if (throttle > 0 && !this.vessel.packed)
                {
                    if (part.vessel.geeForce <= 2)
                        part.vessel.IgnoreGForces(1);

                    fusionRatio = ProcessPowerAndWasteHeat(throttle);

                    // Update FuelFlow
                    effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust * fusionRatio;
                    calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / PluginHelper.GravityConstant;
                    massFlowRateKgPerSecond = curEngineT.currentThrottle * calculatedFuelflow * 1000;

                    if (!curEngineT.getFlameoutState && fusionRatio < 0.01)
                    {
                        curEngineT.status = "Insufficient Electricity";
                    }
                }
                else if (this.vessel.packed && curEngineT.enabled && FlightGlobals.ActiveVessel == vessel && throttle > 0 && percentageFuelRemaining > (100 - fuelLimit) && lightSpeedRatio < speedLimit)
                {
                    warpToReal = true; // Set to true for transition to realtime

                    fusionRatio = CheatOptions.InfiniteElectricity 
                        ? 1 
                        : maximizeThrust 
                            ? ProcessPowerAndWasteHeat(1) 
                            : ProcessPowerAndWasteHeat(storedThrotle);

                    effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust * fusionRatio;
                    calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / PluginHelper.GravityConstant;
                    massFlowRateKgPerSecond = calculatedFuelflow * 1000;

                    if (TimeWarp.fixedDeltaTime > 20)
                    {
                        var deltaCalculations = (float)Math.Ceiling(TimeWarp.fixedDeltaTime / 20);
                        var deltaTimeStep = TimeWarp.fixedDeltaTime / deltaCalculations;

                        for (var step = 0; step < deltaCalculations; step++)
                        {
                            PersistantThrust(deltaTimeStep, universalTime + (step * deltaTimeStep), this.part.transform.up, this.vessel.GetTotalMass());
                            CalculateTimeDialation();
                        }
                    }
                    else
                        PersistantThrust(TimeWarp.fixedDeltaTime, universalTime, this.part.transform.up, this.vessel.GetTotalMass());
                }
                else
                {
                    powerUsage = "0.000 GW / " + (EffectivePowerRequirement / 1000d).ToString("0.000") + " GW";

                    if (!(percentageFuelRemaining > (100 - fuelLimit) || lightSpeedRatio > speedLimit))
                    {
                        warpToReal = false;
                        vessel.ctrlState.mainThrottle = 0;
                    }

                    effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                    calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / PluginHelper.GravityConstant;
                    massFlowRateKgPerSecond = 0;
                }

                curEngineT.maxFuelFlow = (float)calculatedFuelflow;
                curEngineT.maxThrust = (float)effectiveMaxThrustInKiloNewton;
                
                massFlowRateTonPerHour = massFlowRateKgPerSecond * 3.6;
                thrustPowerInTeraWatt = effectiveMaxThrustInKiloNewton * 500 * effectiveIsp * PluginHelper.GravityConstant * 1e-12;

                stopWatch.Stop();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error UpdateTime " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void UpdateAtmosphericCurve(double isp)
        {
            var newAtmosphereCurve = new FloatCurve();
            newAtmosphereCurve.Add(0, (float)isp);
            newAtmosphereCurve.Add(maxAtmosphereDensity, 0);
            curEngineT.atmosphereCurve = newAtmosphereCurve;
        }

        private void PersistantThrust(float modifiedFixedDeltaTime, double modifiedUniversalTime, Vector3d thrustVector, float vesselMass)
        {
            var timeDilationMaximumThrust = timeDilation * timeDilation * MaximumThrust * (maximizeThrust ? 1 : storedThrotle);
            var timeDialationEngineIsp = timeDilation * engineIsp;

            double demandMass;
            thrustVector.CalculateDeltaVV(vesselMass, modifiedFixedDeltaTime, timeDilationMaximumThrust * fusionRatio, timeDialationEngineIsp, out demandMass);

            var fusionFuelRequestAmount = demandMass / fusionFuelResourceDefinition.density;

            double recievedRatio;
            if (CheatOptions.InfinitePropellant)
                recievedRatio = 1;
            else
            {
                var recievedFusionFuel = part.RequestResource(fusionFuelResourceDefinition.id, fusionFuelRequestAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                recievedRatio = fusionFuelRequestAmount > 0 ? recievedFusionFuel/ fusionFuelRequestAmount : 0;
            }

            effectiveMaxThrustInKiloNewton = timeDilationMaximumThrust * recievedRatio;

            if (!(recievedRatio > 0.01)) return;

            var deltaVv = thrustVector.CalculateDeltaVV(vesselMass, modifiedFixedDeltaTime, effectiveMaxThrustInKiloNewton, timeDialationEngineIsp, out demandMass);
            vessel.orbit.Perturb(deltaVv, modifiedUniversalTime);
        }

        private double ProcessPowerAndWasteHeat(float throtle)
        {
            // Calculate Fusion Ratio
            var effectivePowerRequirement = EffectivePowerRequirement;
            var thrustPercentage = (double)(decimal)curEngineT.thrustPercentage;
            var requestedPower = (thrustPercentage * 0.01) * throtle * effectivePowerRequirement;

            var recievedPower = CheatOptions.InfiniteElectricity 
                ? requestedPower
                : consumeFNResourcePerSecond(requestedPower, ResourceManager.FNRESOURCE_MEGAJOULES);

            var plasmaRatio = effectivePowerRequirement > 0 ? recievedPower / requestedPower : 0;
            var wasteheatFusionRatio = plasmaRatio >= 1 ? 1 : plasmaRatio > 0.01 ? plasmaRatio : 0;

            powerUsage = (recievedPower / 1000d).ToString("0.000") + " GW / " + (effectivePowerRequirement * 0.001).ToString("0.000") + " GW";

            if (CheatOptions.IgnoreMaxTemperature) 
                return wasteheatFusionRatio;

            // Lasers produce Wasteheat
            supplyFNResourcePerSecond(recievedPower * (1 - Efficiency), ResourceManager.FNRESOURCE_WASTEHEAT);

            // The Aborbed wasteheat from Fusion
            supplyFNResourcePerSecond(FusionWasteHeat * wasteHeatMultiplier * wasteheatFusionRatio, ResourceManager.FNRESOURCE_WASTEHEAT);

            return wasteheatFusionRatio;
        }

        private void KillKerbalsWithRadiation(float throttle)
        {
            if (!radhazard || throttle <= 0 || rad_safety_features) return;

            var vesselsToRemove = new List<Vessel>();
            var crewToRemove = new List<ProtoCrewMember>();

            foreach (var vess in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                var invSqDist = distance / killDivider;
                var invSqMult = 1d / invSqDist / invSqDist;

                foreach (var crewMember in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1d - TimeWarp.fixedDeltaTime * invSqMult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(crewMember.name + " was killed by Neutron Radiation!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        crewToRemove.Add(crewMember);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(crewMember.name + " was killed by Neutron Radiation!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        vesselsToRemove.Add(vess);
                    }
                }
            }

            foreach (var vess in vesselsToRemove)
            {
                vess.rootPart.Die();
            }

            foreach (var crewMember in crewToRemove)
            {
                var vess = FlightGlobals.Vessels.Find(p => p.GetVesselCrew().Contains(crewMember));
                var partWithCrewMember = vess.Parts.Find(p => p.protoModuleCrew.Contains(crewMember));
                partWithCrewMember.RemoveCrewmember(crewMember);
                crewMember.Die();
            }
        }

        public override int getPowerPriority() 
        {
            return powerPriority;
        }
    }
}

