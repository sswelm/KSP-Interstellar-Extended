using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using FNPlugin.Extensions;
using OpenResourceSystem;

namespace FNPlugin
{
	class DaedalusEngineController : FNResourceSuppliableModule, IUpgradeableModule 
    {
        // Persistant
		[KSPField(isPersistant = true)]
		bool IsEnabled;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Upgraded")]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        bool rad_safety_features = true;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Lightspeed Limiter", guiUnits = "c"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 1, minValue = 0.005f)]
        public float speedLimit = 1;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fuel Limiter", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        public float fuelLimit = 100;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Maximise Thrust"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool maximizeThrust = true;

        [KSPField(isPersistant = false, guiActive = false, guiName = "DeltaTime")]
        public float fixedDeltaTime;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Light Speed", guiFormat = "F1" , guiUnits = " m/s")]
        public double speedOfLight;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Vessel Speed", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio = 0;
        [KSPField(isPersistant = true, guiActive = false, guiName = "Initial Speed")]
        public double initialSpeed;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Relativity", guiFormat = "F10")]
        public double relativity;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Time Dilation", guiFormat = "F10")]
        public double timeDilation;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Universal Time", guiFormat = "F4", guiUnits = " s")]
        public double universalTime;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Mission Time" , guiFormat = "F4", guiUnits = " s")]
        public double missionTime ;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Vessel Time", guiFormat = "F4", guiUnits = " s")]
        public double vesselLifetime;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Radiation Hazard To")]
		public string radhazardstr = "";
        [KSPField(isPersistant = false, guiActive = false, guiName = "Temperature")]
        public string temperatureStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Mass", guiUnits = " t")]
        public float partMass = 1;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Fusion Ratio", guiFormat = "F6")]
        public double fusionRatio = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Recieved Ratio", guiFormat = "F6")]
        public double recievedRatio = 0;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true,  guiName = "Deuterium")]
        public string deuteriumAmounts;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Helium-3")]
        public string helium3Amounts;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fusion", guiFormat = "F2", guiUnits = "%")]
        public double fusionPercentage = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Fuel Flow", guiFormat = "F8", guiUnits = " U")]
        public double calculatedFuelflow = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Helium-3 Usage", guiFormat = "F2", guiUnits = " L/day")]
        public double helium3UsageDay = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Deuterium Usage", guiFormat = "F2", guiUnits = " L/day")]
        public double deuteriumUsageDay = 0;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Stored Throtle")]
        public float storedThrotle = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Benchmark", guiFormat = "F3", guiUnits = " ms")]
        public double onFixedUpdateBenchmark;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Effective Thrust", guiFormat = "F2", guiUnits = " kN")]
        public double effectiveThrust = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Effective Isp", guiFormat = "F2", guiUnits = "s")]
        public double effectiveIsp = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Fuel Remaining", guiFormat = "F3", guiUnits = "%")]
        private double percentageFuelRemaining; 

        [KSPField(isPersistant = false)]
        public float powerRequirement = 2500;
        [KSPField(isPersistant = false)]
        public float maxThrust = 300;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 1200;
        [KSPField(isPersistant = false)]
        public float maxAtmosphereDensity = 0.001f;

        [KSPField(isPersistant = false)]
        public float efficiency = 0.25f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded = 0.5f;
        [KSPField(isPersistant = false)]
        public float leathalDistance = 2000;
        [KSPField(isPersistant = false)]
        public float killDivider = 50;

        [KSPField(isPersistant = false)]
        public float fusionWasteHeat = 2500;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded = 10000;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float powerRequirementMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float maxTemp = 3200;

        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public string originalName = "Prototype Deadalus IC Fusion Engine";
        [KSPField(isPersistant = false)]
        public string upgradedName = "Deadalus IC Fusion Engine";

        // Gui
        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
        public string engineType = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName= "upgrade tech")]
        public string upgradeTechReq = null;

        protected bool hasrequiredupgrade = false;
		protected bool radhazard = false;
        private bool warpToReal = false;

		protected float engineIsp = 0;
		protected double standard_tritium_rate = 0;
        protected ModuleEngines curEngineT;

        private double propellantAverageDensity;
        private double densityLqdDeuterium;
        private double densityLqdHelium3;
        private double deteuriumFraction;
        private double helium3Fraction;

        private BaseEvent deactivateRadSafetyEvent;
        private BaseEvent activateRadSafetyEvent;
        private BaseEvent retrofitEngineEvent;
        private BaseField radhazardstrField;

        private PartResourceDefinition deteuriumResourceDefinition;
        private PartResourceDefinition helium3ResourceDefinition;

        private List<PartResource> deteuriumPartResources;
        private List<PartResource> helium3PartResources;

        private double percentageDeteuriumFuelRemaining;
        private double percentageHelium3FuelRemaining;

        private Stopwatch stopWatch;

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

        public float Efficiency { get { return isupgraded ? efficiencyUpgraded : efficiency; } }
        public float MaximumThrust { get { return isupgraded ? maxThrustUpgraded : maxThrust; } }
        public float FusionWasteHeat { get { return isupgraded ? fusionWasteHeatUpgraded : fusionWasteHeat; } }

        public double PowerRequirement
        {
            get
            {
                return powerRequirement * powerRequirementMultiplier;
            }
        }

        public void upgradePartModule()
        {
            engineType = upgradedName;
            isupgraded = true;
        }

        #endregion

        public override void OnStart(PartModule.StartState state) 
        {
            try
            {
                stopWatch = new Stopwatch();

                speedOfLight = GameConstants.speedOfLight * PluginHelper.SpeedOfLightMult;

                deteuriumResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdDeuterium);
                helium3ResourceDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdHelium3);

                densityLqdDeuterium = deteuriumResourceDefinition.density;
                densityLqdHelium3 = helium3ResourceDefinition.density;

                deteuriumFraction = densityLqdDeuterium / (densityLqdHelium3 + densityLqdDeuterium);
                helium3Fraction = densityLqdHelium3 / (densityLqdHelium3 + densityLqdDeuterium);

                propellantAverageDensity = densityLqdDeuterium / densityLqdHelium3;

                part.maxTemp = maxTemp;
                part.thermalMass = 1;
                part.thermalMassModifier = 1;

                engineType = originalName;
                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();

                if (curEngineT == null) return;

                engineIsp = curEngineT.atmosphereCurve.Evaluate(0);

                // if we can upgrade, let's do so
                if (isupgraded)
                    upgradePartModule();
                else if (this.HasTechsRequiredToUpgrade())
                    hasrequiredupgrade = true;

                // calculate WasteHeat Capacity
                part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT].maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;

                if (state == StartState.Editor && this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }

                if (state == StartState.Editor)
                {
                    // fetch all parts 
                    var fetchedShipParts = EditorLogic.fetch.ship.parts;

                    deteuriumPartResources = fetchedShipParts
                        .Where(m => m.Resources.Contains(InterstellarResourcesConfiguration.Instance.LqdDeuterium))
                        .Select(m => m.Resources[InterstellarResourcesConfiguration.Instance.LqdDeuterium]).ToList();
                    helium3PartResources = fetchedShipParts
                        .Where(m => m.Resources.Contains(InterstellarResourcesConfiguration.Instance.LqdHelium3))
                        .Select(m => m.Resources[InterstellarResourcesConfiguration.Instance.LqdHelium3]).ToList();
                }
                else
                {
                    //deteuriumPartResources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdDeuterium).ToList();
                    deteuriumPartResources = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == InterstellarResourcesConfiguration.Instance.LqdDeuterium)).ToList();

                    //helium3PartResources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdHelium3).ToList();
                    deteuriumPartResources = part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == InterstellarResourcesConfiguration.Instance.LqdHelium3)).ToList();
                }

                // bind with fields and events
                deactivateRadSafetyEvent = Events["DeactivateRadSafety"];
                activateRadSafetyEvent = Events["ActivateRadSafety"];
                retrofitEngineEvent = Events["RetrofitEngine"];
                radhazardstrField = Fields["radhazardstr"];
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error OnStart " + e.Message + " stack " + e.StackTrace);
            }
		}

        public void Update()
        {
            try
            {
                if (HighLogic.LoadedSceneIsEditor)
                    return;

                double deteuriumAmount;
                double deteuriumMaxAmount;
                part.GetConnectedResourceTotals(deteuriumResourceDefinition.id, out deteuriumAmount, out deteuriumMaxAmount);

                percentageDeteuriumFuelRemaining = deteuriumAmount / deteuriumMaxAmount * 100;
                deuteriumAmounts = percentageDeteuriumFuelRemaining.ToString("0.0000") + "% " + deteuriumMaxAmount.ToString("0") + " L";

                double helium3Amount;
                double helium3MaxAmount;
                part.GetConnectedResourceTotals(helium3ResourceDefinition.id, out helium3Amount, out helium3MaxAmount);

                percentageHelium3FuelRemaining = helium3Amount / helium3MaxAmount * 100;
                helium3Amounts = percentageHelium3FuelRemaining.ToString("0.0000") + "% " + helium3MaxAmount.ToString("0") + " L";

                percentageFuelRemaining = Math.Min(percentageDeteuriumFuelRemaining, percentageHelium3FuelRemaining);

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

                int kerbal_hazard_count = 0;
                foreach (Vessel vess in FlightGlobals.Vessels)
                {
                    double distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);
                    if (distance < leathalDistance && vess != this.vessel)
                        kerbal_hazard_count += vess.GetCrewCount();
                }

                if (kerbal_hazard_count > 0)
                {
                    radhazard = true;
                    if (kerbal_hazard_count > 1)
                        radhazardstr = kerbal_hazard_count.ToString() + " Kerbals.";
                    else
                        radhazardstr = kerbal_hazard_count.ToString() + " Kerbal.";

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
                foreach (FXGroup fx_group in part.fxGroups)
                {
                    fx_group.setActive(false);
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
                if (initialSpeed == 0 || vessel.missionTime == 0)
                    initialSpeed = vessel.obt_speed;

                lightSpeedRatio = Math.Min(Math.Max(vessel.obt_speed - initialSpeed, 0) / speedOfLight, 0.9999999999);

                relativity = 1 / Math.Sqrt(1 - lightSpeedRatio * lightSpeedRatio);

                timeDilation = 1 / relativity;
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

                temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
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
                missionTime = vessel.missionTime;
                fixedDeltaTime = TimeWarp.fixedDeltaTime;
                universalTime = Planetarium.GetUniversalTime();
                CalculateTimeDialation();

                if (vessel.missionTime > 0)
                {
                    if (vesselLifetime == 0)
                        vesselLifetime = vessel.missionTime;
                    else
                        vesselLifetime += TimeWarp.fixedDeltaTime * timeDilation;
                }
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

                float throttle = curEngineT.currentThrottle > 0 ? Mathf.Max(curEngineT.currentThrottle, 0.01f) : 0;

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

                if (throttle > 0 && !this.vessel.packed)
                {
                    UpdateAtmosphericCurve();

                    fusionRatio = ProcessPowerAndWasteHeat(throttle, TimeWarp.fixedDeltaTime);

                    // Update FuelFlow
                    effectiveThrust = timeDilation * timeDilation * MaximumThrust * fusionRatio;
                    calculatedFuelflow = effectiveThrust / effectiveIsp / PluginHelper.GravityConstant;
                    curEngineT.maxFuelFlow = (float)calculatedFuelflow;
                    curEngineT.maxThrust = (float)effectiveThrust;

                    // calculate day usage
                    var demandedMass = calculatedFuelflow / propellantAverageDensity;
                    var deteuriumRequestAmount = demandedMass * deteuriumFraction / densityLqdDeuterium;
                    var helium3RequestAmount = demandedMass * helium3Fraction / densityLqdHelium3;
                    deuteriumUsageDay = deteuriumRequestAmount * PluginHelper.SecondsInDay;
                    helium3UsageDay = helium3RequestAmount * PluginHelper.SecondsInDay;

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
                            ? ProcessPowerAndWasteHeat(1, TimeWarp.fixedDeltaTime) 
                            : ProcessPowerAndWasteHeat(storedThrotle, TimeWarp.fixedDeltaTime);

                    if (TimeWarp.fixedDeltaTime > 20)
                    {
                        var deltaCalculations = (float)Math.Ceiling(TimeWarp.fixedDeltaTime / 20);
                        var deltaTimeStep = TimeWarp.fixedDeltaTime / deltaCalculations;

                        for (int step = 0; step < deltaCalculations; step++)
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
                    if (!(percentageFuelRemaining > (100 - fuelLimit) || lightSpeedRatio > speedLimit))
                    {
                        warpToReal = false;
                        vessel.ctrlState.mainThrottle = 0;
                    }

                    deuteriumUsageDay = 0;
                    helium3UsageDay = 0;
                    fusionPercentage = 0;

                    UpdateAtmosphericCurve();

                    effectiveThrust = timeDilation * timeDilation * MaximumThrust;

                    var maxFuelFlow = effectiveThrust / effectiveIsp / PluginHelper.GravityConstant;
                    curEngineT.maxFuelFlow = (float)maxFuelFlow;
                    curEngineT.maxThrust = (float)effectiveThrust;
                }

                stopWatch.Stop();
                onFixedUpdateBenchmark = stopWatch.ElapsedTicks * (1d / Stopwatch.Frequency) * 1000d;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error UpdateTime " + e.Message + " stack " + e.StackTrace);
            }
        }

        private void UpdateAtmosphericCurve()
        {
            FloatCurve newAtmosphereCurve = new FloatCurve();
            newAtmosphereCurve.Add(0, (float)effectiveIsp);
            newAtmosphereCurve.Add(maxAtmosphereDensity, 0);
            curEngineT.atmosphereCurve = newAtmosphereCurve;
        }

        private void PersistantThrust(float fixedDeltaTime, double universalTime, Vector3d thrustUV, float vesselMass)
        {
            var timeDilationMaximumThrust = timeDilation * timeDilation * MaximumThrust;
            var timeDialationEngineIsp = timeDilation * engineIsp;

            double demandMass;
            CalculateDeltaVV(vesselMass, fixedDeltaTime, timeDilationMaximumThrust * fusionRatio, timeDialationEngineIsp, propellantAverageDensity, thrustUV, out demandMass);

            var deteuriumRequestAmount = demandMass * deteuriumFraction / densityLqdDeuterium;
            var helium3RequestAmount = demandMass * helium3Fraction / densityLqdHelium3;

            deuteriumUsageDay = deteuriumRequestAmount / fixedDeltaTime * PluginHelper.SecondsInDay;
            helium3UsageDay = helium3RequestAmount / fixedDeltaTime * PluginHelper.SecondsInDay;

            if (CheatOptions.InfinitePropellant)
                recievedRatio = 1;
            else
            {
                // request deteurium
                var recievedDeuterium = part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, deteuriumRequestAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                var deuteriumShortage = deteuriumRequestAmount - recievedDeuterium;
                recievedDeuterium += part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, deuteriumShortage, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                deuteriumShortage = deteuriumRequestAmount - recievedDeuterium;

                // request helium3
                var recievedHelium3 = part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, helium3RequestAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                var helium3Shortage = helium3RequestAmount - recievedHelium3;
                recievedHelium3 += part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, helium3Shortage, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                helium3Shortage = helium3RequestAmount - recievedHelium3;

                recievedRatio = deteuriumRequestAmount > 0 && helium3RequestAmount > 0
                    ? Math.Min(recievedDeuterium / deteuriumRequestAmount, recievedHelium3 / helium3RequestAmount) * fusionRatio
                    : 0;
            }

            effectiveThrust = timeDilationMaximumThrust * recievedRatio;

            var deltaVV = CalculateDeltaVV(vesselMass, fixedDeltaTime, effectiveThrust, timeDialationEngineIsp, propellantAverageDensity, thrustUV, out demandMass);

            if (recievedRatio > 0.01)
                vessel.orbit.Perturb(deltaVV, universalTime);
        }

        private double ProcessPowerAndWasteHeat(float throtle, float fixedDeltaTime)
        {
            // Calculate Fusion Ratio
            var powerRequirementFixed = PowerRequirement * fixedDeltaTime;
            var requestedPower = (curEngineT.thrustPercentage / 100d) * throtle * powerRequirementFixed;

            var recievedPower = CheatOptions.InfiniteElectricity 
                ? requestedPower 
                : consumeFNResource(requestedPower, FNResourceManager.FNRESOURCE_MEGAJOULES);

            var plasma_ratio = powerRequirementFixed > 0 ? recievedPower / powerRequirementFixed : 0;
            var fusionRatio = plasma_ratio >= 1 ? 1 : plasma_ratio > 0.01 ? plasma_ratio : 0;


            if (!CheatOptions.IgnoreMaxTemperature)
            {
                // Lasers produce Wasteheat
                supplyFNResource(recievedPower * (1 - Efficiency), FNResourceManager.FNRESOURCE_WASTEHEAT);

                // The Aborbed wasteheat from Fusion
                supplyFNResource(FusionWasteHeat * wasteHeatMultiplier * fusionRatio * fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
            }

            fusionPercentage = fusionRatio * 100d;

            return fusionRatio;
        }

        // Calculate DeltaV vector and update resource demand from mass (demandMass)
        public static Vector3d CalculateDeltaVV(float totalMass, float deltaTime, double thrust, double isp, double averageDensity, Vector3d thrustUV, out double demandMass)
        {
            // Mass flow rate
            var massFlowRate = thrust / (isp * GameConstants.STANDARD_GRAVITY);
            // Change in mass over time interval dT
            var dm = massFlowRate * deltaTime;
            // Resource demand from propellants with mass
            demandMass = averageDensity > 0 ? dm / averageDensity : 0;
            // Mass at end of time interval dT
            var finalMass = totalMass - dm;
            // deltaV amount
            var deltaV = finalMass > 0 && totalMass > 0 
                ? isp * GameConstants.STANDARD_GRAVITY * Math.Log(totalMass / finalMass) 
                : 0;

            // Return deltaV vector
            return deltaV * thrustUV;
        }

        private void KillKerbalsWithRadiation(float throttle)
        {
            if (!radhazard || throttle <= 0 || rad_safety_features) return;

            System.Random rand = new System.Random(new System.DateTime().Millisecond);
            List<Vessel> vessels_to_remove = new List<Vessel>();
            List<ProtoCrewMember> crew_to_remove = new List<ProtoCrewMember>();
            double death_prob = TimeWarp.fixedDeltaTime;

            foreach (Vessel vess in FlightGlobals.Vessels)
            {
                double distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                double inv_sq_dist = distance / killDivider;
                double inv_sq_mult = 1d / inv_sq_dist / inv_sq_dist;
                foreach (ProtoCrewMember crew_member in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1d - death_prob * inv_sq_mult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        crew_to_remove.Add(crew_member);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5f, ScreenMessageStyle.UPPER_CENTER);
                        vessels_to_remove.Add(vess);
                    }
                }
            }

            foreach (Vessel vess in vessels_to_remove)
            {
                vess.rootPart.Die();
            }

            foreach (ProtoCrewMember crew_member in crew_to_remove)
            {
                Vessel vess = FlightGlobals.Vessels.Find(p => p.GetVesselCrew().Contains(crew_member));
                Part part = vess.Parts.Find(p => p.protoModuleCrew.Contains(crew_member));
                part.RemoveCrewmember(crew_member);
                crew_member.Die();
            }
        }

        public override string getResourceManagerDisplayName() 
        {
            return engineType;
        }

        public override int getPowerPriority() 
        {
            return 2;
        }
	}
}

