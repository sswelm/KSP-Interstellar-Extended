using FNPlugin.Power;
using FNPlugin.Constants;
using FNPlugin.Wasteheat;
using FNPlugin.External;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin
{
    abstract class FusionECU2 : EngineECU2
    {
        // Persistant
        [KSPField(isPersistant = true)]
        bool rad_safety_features = true;

        // None Persistant
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_Radhazardstr")]//Radiation Hazard To
        public string radhazardstr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_Temperature")]//Temperature
        public string temperatureStr = "";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionECU2_Fuels")]//Fuels
        public string fuels;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionECU2_Ratios")]//Ratios
        public string ratios;

        [KSPField]
        public string fuelSwitchName = "Fusion Type";
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_PowerRequirementMk1", guiFormat = "F3", guiUnits = " MW")]//Power Requirement Mk1
        public double powerRequirement = 0;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_PowerRequirementMk2", guiFormat = "F3", guiUnits = " MW")]//Power Requirement Mk2
        public double powerRequirementUpgraded1 = 0;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_PowerRequirementMk3", guiFormat = "F3", guiUnits = " MW")]//Power Requirement Mk3
        public double powerRequirementUpgraded2 = 0;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_PowerRequirementMk4", guiFormat = "F3", guiUnits = " MW")]//Power Requirement Mk4
        public double powerRequirementUpgraded3 = 0;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_PowerRequirementMk5", guiFormat = "F3", guiUnits = " MW")]//Power Requirement Mk5
        public double powerRequirementUpgraded4 = 0;

        [KSPField]
        public bool selectableIsp = false;
        [KSPField]
        public double maxAtmosphereDensity = 0.001;
        [KSPField]
        public double leathalDistance = 2000;
        [KSPField]
        public double killDivider = 0;

        [KSPField]
        public double fusionWasteHeat = 625;
        [KSPField]
        public double fusionWasteHeatUpgraded1 = 2500;
        [KSPField]
        public double fusionWasteHeatUpgraded2 = 5000;
        [KSPField]
        public double fusionWasteHeatUpgraded3 = 7500;
        [KSPField]
        public double fusionWasteHeatUpgraded4 = 10000;

        // Use for SETI Mode
        [KSPField(isPersistant = false)]
        public double wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public double powerRequirementMultiplier = 1;

        // Debugging variables
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double powerMultiplier = 1;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public bool hasIspThrottling = true;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionECU2_Isp", guiFormat = "F3", guiUnits = " s")]//Isp
        public float currentIsp;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double neutronbsorbionBonus;

        //[KSPField(isPersistant = true, guiName = "Use MJ Battery"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        //public bool useMegajouleBattery = false;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_AvailablePower", guiFormat = "F3", guiUnits = " MW")]//Available Power
        public double availablePower;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_PowerRequirement", guiFormat = "F3", guiUnits = " MW")]//Power Requirement
        public double enginePowerRequirement;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionECU2_LaserWasteheat", guiFormat = "F3", guiUnits = " MW")]//Laser Wasteheat
        public double laserWasteheat;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FusionECU2_AbsorbedWasteheat", guiFormat = "F3", guiUnits = " MW")]//Absorbed Wasteheat
        public double absorbedWasteheat;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_RadiatorTemp")]//Radiator Temp
        public double coldBathTemp;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_MaxRadiatorTemp")]//Max Radiator Temp
        public float maxTempatureRadiators;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_PerformanceRadiators")]//Performance Radiators
        public double radiatorPerformance;
        [KSPField(guiName = "#LOC_KSPIE_FusionECU2_Emisiveness")]//Emisiveness
        public double partEmissiveConstant;
        [KSPField]
        protected float curveMaxISP; // ToDo: make sure it is properly initialized after  comming from assembly 
        [KSPField]
        public double radius = 1;

        // abstracts
        protected abstract float InitialGearRatio { get; }
        protected abstract float SelectedIsp { get; set; }
        protected abstract float MinIsp { get; set; }
        protected abstract float MaxIsp { get; }
        protected abstract float GearDivider { get; }
        protected abstract float MaxSteps { get; }
        protected abstract float MaxThrustEfficiencyByIspPower { get; }
        protected abstract float NeutronAbsorptionFractionAtMinIsp { get; }
        protected abstract FloatCurve BaseFloatCurve { get; set; }
        protected abstract bool ShowIspThrottle { get; set; } 

        // protected
        protected bool hasrequiredupgrade = false;
        protected bool radhazard = false;
        protected double standard_tritium_rate = 0;
        protected string FuelConfigName = "Fusion Type";
        protected double Altitude;
        protected double lastAltitude;

        protected ResourceBuffers resourceBuffers;
        protected FNEmitterController emitterController;        

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_DisableRadiationSafety", active = true)]//Disable Radiation Safety
        public void DeactivateRadSafety()
        {
            rad_safety_features = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_ActivateRadiationSafety", active = false)]//Activate Radiation Safety
        public void ActivateRadSafety()
        {
            rad_safety_features = true;
        }

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq1; } }

        public void upgradePartModule() { }

        #endregion

        private void InitializeKerbalismEmitter()
        {
            if (!Kerbalism.IsLoaded)
                return;

            emitterController = part.FindModuleImplementing<FNEmitterController>();
            
            if (emitterController == null)
                UnityEngine.Debug.LogWarning("[KSPI]: No Emitter Found om " + part.partInfo.title);
        }

        private void UpdateKerbalismEmitter()
        {
            if (emitterController == null)
                return;

            emitterController.reactorActivityFraction = fusionRatio;
            emitterController.exhaustActivityFraction = fusionRatio;
            emitterController.fuelNeutronsFraction = CurrentActiveConfiguration.neutronRatio;
        }

        public double MaximumThrust
        {
            get
            {
                return FullTrustMaximum * Math.Pow((MinIsp / SelectedIsp), MaxThrustEfficiencyByIspPower) * MinIsp / curveMaxISP;               
            }
        }

        public double FusionWasteHeat
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return fusionWasteHeat * WasteheatMult();
                else if (EngineGenerationType == GenerationType.Mk2)
                    return fusionWasteHeatUpgraded1 * WasteheatMult();
                else if (EngineGenerationType == GenerationType.Mk3)
                    return fusionWasteHeatUpgraded2 * WasteheatMult();
                else if (EngineGenerationType == GenerationType.Mk4)
                    return fusionWasteHeatUpgraded3 * WasteheatMult();
                else
                    return fusionWasteHeatUpgraded4 * WasteheatMult();
            }
        }

        public double FullTrustMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return MaxThrust;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return MaxThrustUpgraded1;
                else if (EngineGenerationType == GenerationType.Mk3)
                    return MaxThrustUpgraded2;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return MaxThrustUpgraded3;
                else
                    return MaxThrustUpgraded4;
            }
        }

        public double LaserEfficiency
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return efficiency;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return efficiencyUpgraded1;
                else if (EngineGenerationType == GenerationType.Mk3)
                    return efficiencyUpgraded2;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return efficiencyUpgraded3;
                else
                    return efficiencyUpgraded4;
            }
        }

        public double CurrentMaximumPowerRequirement
        {
            get
            {
                enginePowerRequirement = PowerRequirementMaximum*powerRequirementMultiplier;
                return enginePowerRequirement;
            }
        }

        public double PowerRequirementMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return powerRequirement * PowerMult();
                else if (EngineGenerationType == GenerationType.Mk2)
                    return powerRequirementUpgraded1 * PowerMult();
                else if (EngineGenerationType == GenerationType.Mk3)
                    return powerRequirementUpgraded2 * PowerMult();
                else if (EngineGenerationType == GenerationType.Mk4)
                    return powerRequirementUpgraded3 * PowerMult();
                else
                    return powerRequirementUpgraded4 * PowerMult(); 
            }
        }

        public float MinThrottleRatio
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return minThrottleRatioMk1;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return minThrottleRatioMk2;
                else if (EngineGenerationType == GenerationType.Mk3)
                    return minThrottleRatioMk3;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return minThrottleRatioMk4;
                else
                    return minThrottleRatioMk5;
            }
        }
        private double PowerMult ()
        {
            return FuelConfigurations.Count > 0 ? CurrentActiveConfiguration.powerMult : 1;
        }

        public bool HasIspThrottling()
        {
            return FuelConfigurations.Count > 0 ? CurrentActiveConfiguration.hasIspThrottling : true;
        }

        private double WasteheatMult()
        {
            return FuelConfigurations.Count > 0 ? CurrentActiveConfiguration.wasteheatMult : 1;
        }

        private void FcUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && vessel == null)
                return;

            FcSetup();
            lastAltitude = Altitude;
        }

        private void FcSetup()
        {
            if (HighLogic.LoadedSceneIsFlight)
                Altitude = vessel.atmDensity;
            else
                Altitude = 0;

            try
            {

                BaseField ispField = Fields["localIsp"];

                UI_FloatRange[] ispController = { ispField.uiControlFlight as UI_FloatRange, ispField.uiControlEditor as UI_FloatRange };

                ispField.OnValueModified += IspField_OnValueModified;

                for (int I = 0; I < ispController.Length; I++)
                {
                    float akIsp = SelectedIsp;
                    float akMinIsp = ispController[I].minValue;
                    float akMaxIsp = ispController[I].maxValue;
                    float stepIncrement = ispController[I].stepIncrement;
                    float stepNumb = stepIncrement > 0 ? (akIsp - akMinIsp) / stepIncrement : 0;

                    if (stepNumb < 0)
                        stepNumb = 0;
                    else
                        if (stepNumb > MaxSteps) stepNumb = MaxSteps;

                    akMinIsp = (float)Math.Round(BaseFloatCurve.Evaluate((float)Altitude));

                    if (akMinIsp < 1)
                        akMinIsp = 1;

                    akMaxIsp = GearDivider > 0 ? (float)Math.Round(akMinIsp / GearDivider) : akMinIsp;
                    stepIncrement = (akMaxIsp - akMinIsp) / 100;

                    ispController[I].minValue = akMinIsp;
                    ispController[I].maxValue = akMaxIsp;
                    ispController[I].stepIncrement = stepIncrement;

                    SelectedIsp = akMinIsp + stepIncrement * stepNumb;
                    I++;
                }
                lastAltitude = Altitude;

            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FusionEngine FcSetup exception: " + e.Message);
            }
        }

        private void IspField_OnValueModified(object arg1)
        {
            
        }

        public override void UpdateFuel(bool isEditor = false)
        {
            base.UpdateFuel(isEditor);

            //if (isEditor) return;

            Debug.Log("[KSPI]: Fusion Gui Updated");
            BaseFloatCurve = CurrentActiveConfiguration.atmosphereCurve;
            curveMaxISP = GetMaxKey(BaseFloatCurve);
            FcSetup();
            Debug.Log("[KSPI]: Curve Max ISP:" + curveMaxISP);
        }

        public override void OnStart(StartState state)
        {
            try
            {
                if (state.ToString().Contains(StartState.PreLaunch.ToString()))
                {
                    Debug.Log("[KSPI]: PreLaunch uses InitialGearRatio:" + InitialGearRatio);
                    SelectedIsp = ((MaxIsp - MinIsp) * Math.Max(0, Math.Min(1, InitialGearRatio))) + MinIsp;
                }

                Fields["selectedFuel"].guiName = fuelSwitchName;

                Fields["enginePowerRequirement"].guiActive = powerRequirement > 0;
                Fields["laserWasteheat"].guiActive = powerRequirement > 0 && fusionWasteHeat > 0;
                Fields["absorbedWasteheat"].guiActive = powerRequirement > 0 && fusionWasteHeat > 0;
                Fields["fusionRatio"].guiActive = powerRequirement > 0;

                Fields["powerRequirement"].guiActiveEditor = powerRequirement > 0;
                Fields["powerRequirementUpgraded1"].guiActiveEditor = powerRequirementUpgraded1 > 0;
                Fields["powerRequirementUpgraded2"].guiActiveEditor = powerRequirementUpgraded2 > 0;
                Fields["powerRequirementUpgraded3"].guiActiveEditor = powerRequirementUpgraded3 > 0;
                Fields["powerRequirementUpgraded4"].guiActiveEditor = powerRequirementUpgraded4 > 0;

                Fields["fusionWasteHeat"].guiActiveEditor = fusionWasteHeat > 0;
                Fields["fusionWasteHeatUpgraded1"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq1);
                Fields["fusionWasteHeatUpgraded2"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq2);
                Fields["fusionWasteHeatUpgraded3"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq3);
                Fields["fusionWasteHeatUpgraded4"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq4);
               
                part.maxTemp = maxTemp;
                part.thermalMass = 1;
                part.thermalMassModifier = 1;

                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();
                if (curEngineT == null)
                {
                    Debug.LogError("[KSPI]: FusionEngine OnStart Engine not found");
                    return;
                }
                BaseFloatCurve = curEngineT.atmosphereCurve;

                curveMaxISP = GetMaxKey(BaseFloatCurve);
                if (hasMultipleConfigurations) FcSetup();

                InitializeKerbalismEmitter();

                DetermineTechLevel();

                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 1.0e+4, true));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                resourceBuffers.Init(this.part);

                if (state != StartState.Editor)
                    part.emissiveConstant = maxTempatureRadiators > 0 ? 1 - coldBathTemp / maxTempatureRadiators : 0.01;

                base.OnStart(state);

                Fields["localIsp"].guiActive = selectableIsp;
                Fields["localIsp"].guiActiveEditor = selectableIsp;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FusionEngine OnStart eception: " + e.Message);
            }
        }

        private double GetRatio(string akPropName)
        {
            var firstOrDefault = curEngineT.propellants.FirstOrDefault(pr => pr.name == akPropName);

            return firstOrDefault != null ? firstOrDefault.ratio : 0;
        }

        private void SetRatio(string akPropName, float akRatio)
        {
            var firstOrDefault = curEngineT.propellants.FirstOrDefault(pr => pr.name == akPropName);
            if (firstOrDefault != null)
                firstOrDefault.ratio = akRatio;
        }

        public override void OnUpdate()
        {
            if (curEngineT == null) return;

            Events["DeactivateRadSafety"].active = rad_safety_features;
            Events["ActivateRadSafety"].active = !rad_safety_features;

            if (curEngineT.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                UnityEngine.Debug.Log("[KSPI]: FusionECU2 on " + part.name + " was Force Activated");
                part.force_activate();
            }

            var kerbalHazardCount = 0;
            foreach (var vess in FlightGlobals.Vessels)
            {
                var distance = (float)Vector3d.Distance(vessel.transform.position, vess.transform.position);
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

                Fields["radhazardstr"].guiActive = true;
            }
            else
            {
                Fields["radhazardstr"].guiActive = false;
                radhazard = false;
                radhazardstr = "None.";
            }

            Fields["localIsp"].guiActive = selectableIsp;
            Fields["localIsp"].guiActiveEditor = selectableIsp;

            if (selectableIsp) 
                FcUpdate();
            else
                SelectedIsp = MinIsp;

            base.OnUpdate();
        }

        private void ShutDown(string reason)
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

        private float GetMaxKey(FloatCurve akCurve)
        {
            var i = 0;
            float max = 0;
            var keys = akCurve.Curve.keys;

            while (i < keys.Length)
            {
                var currentKey = keys[i];
                if (max < currentKey.value)
                    max = currentKey.value;
                i++;
            }

            return max;
        }

        private void UpdateAtmosphereCurveInVab(float currentIsp)
        {
            FcUpdate();
            curEngineT.atmosphereCurve = BaseFloatCurve;
            MinIsp = currentIsp;
        }

        private void UpdateAtmosphereCurve(float currentIsp)
        {
            var newIsp = new FloatCurve();
            Altitude = vessel.atmDensity;
            var origIsp = BaseFloatCurve.Evaluate((float)Altitude);

            FcUpdate();
            newIsp.Add((float)Altitude, currentIsp);
            curEngineT.atmosphereCurve = newIsp;
            MinIsp = origIsp;
        }

        // Is called in the VAB
        public virtual void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                SetRatios();

                currentIsp = hasIspThrottling ? SelectedIsp : MinIsp;
                UpdateAtmosphereCurveInVab(currentIsp);

                maximumThrust = hasIspThrottling ? MaximumThrust : FullTrustMaximum;

                // Update FuelFlow
                maxFuelFlow = maximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;

                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = (float)maximumThrust;
            }
        }

        public override void OnFixedUpdate()
        {
            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
            MinIsp = BaseFloatCurve.Evaluate((float)Altitude);

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.UpdateBuffers();

            if (curEngineT == null || !curEngineT.isEnabled) return;

            throttle = curEngineT.currentThrottle > MinThrottleRatio ? curEngineT.currentThrottle : 0;

            if (throttle > 0)
            {
                if (maxAtmosphereDensity >= 0 && vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg1"));//"Inertial Fusion cannot operate in atmosphere!"

                if (radhazard && rad_safety_features)
                    ShutDown(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg2"));//"Engines throttled down as they presently pose a radiation hazard"

                if (SelectedIsp <= 10)
                    ShutDown(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg3"));//"Engine Stall"
            }

            KillKerbalsWithRadiation(fusionRatio);

            hasIspThrottling = HasIspThrottling();

            ShowIspThrottle = hasIspThrottling;

            availablePower = Math.Max(getResourceAvailability(ResourceManager.FNRESOURCE_MEGAJOULES), getAvailablePrioritisedStableSupply(ResourceManager.FNRESOURCE_MEGAJOULES));

            if (throttle > 0 )
            {
                var requestedPowerPerSecond = throttle * CurrentMaximumPowerRequirement;

                //var resourceBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);
                //var effectivePowerThrotling = useMegajouleBattery ? 1 : resourceBarRatio > 0.1 ? 1 : resourceBarRatio * 10;

                var requestedPower = Math.Min(requestedPowerPerSecond, availablePower);

                var recievedPowerPerSecond = requestedPower <= 0 ? 0 
                    : CheatOptions.InfiniteElectricity
                        ? requestedPowerPerSecond
                        : consumeFNResourcePerSecond(requestedPower, ResourceManager.FNRESOURCE_MEGAJOULES);

                fusionRatio = requestedPowerPerSecond > 0 ? Math.Min(1, recievedPowerPerSecond / requestedPowerPerSecond) : 1;

                laserWasteheat = recievedPowerPerSecond * (1 - LaserEfficiency);

                // Lasers produce Wasteheat
                if (!CheatOptions.IgnoreMaxTemperature && laserWasteheat > 0)
                    supplyFNResourcePerSecond(laserWasteheat, ResourceManager.FNRESOURCE_WASTEHEAT);

                // The Aborbed wasteheat from Fusion
                rateMultplier = hasIspThrottling ? Math.Pow(SelectedIsp / MinIsp, 2) : 1;
                neutronbsorbionBonus = hasIspThrottling ? 1 - NeutronAbsorptionFractionAtMinIsp * (1 - ((SelectedIsp - MinIsp) / (MaxIsp - MinIsp))) : 0.5;
                absorbedWasteheat = FusionWasteHeat * wasteHeatMultiplier * fusionRatio * throttle * neutronbsorbionBonus;
                supplyFNResourcePerSecond(absorbedWasteheat, ResourceManager.FNRESOURCE_WASTEHEAT);

                SetRatios();

                currentIsp = hasIspThrottling ? SelectedIsp : MinIsp;
                UpdateAtmosphereCurve(currentIsp);
                maximumThrust = hasIspThrottling ? MaximumThrust : FullTrustMaximum;

                // Update FuelFlow
                maxFuelFlow = fusionRatio * maximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;

                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = (float)maximumThrust;

                if (!curEngineT.getFlameoutState && fusionRatio < 0.9 && recievedPowerPerSecond > 0)
                    curEngineT.status = Localizer.Format("#LOC_KSPIE_FusionECU2_statu1");//"Insufficient Electricity"
            }
            else
            {
                enginePowerRequirement = 0;
                absorbedWasteheat = 0;
                laserWasteheat = 0;

                var requestedPowerPerSecond = CurrentMaximumPowerRequirement;

                fusionRatio = requestedPowerPerSecond > 0 ? Math.Min(1, availablePower / requestedPowerPerSecond) : 1;

                currentIsp = hasIspThrottling ? SelectedIsp : MinIsp;
                maximumThrust = hasIspThrottling ? MaximumThrust : FullTrustMaximum;

                UpdateAtmosphereCurve(currentIsp);
                
                rateMultplier = hasIspThrottling ? Math.Pow(SelectedIsp / MinIsp, 2) : 1;

                maxFuelFlow = fusionRatio * maximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;

                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = (float)maximumThrust;

                SetRatios();
            }

            coldBathTemp = FNRadiator.getAverageRadiatorTemperatureForVessel(vessel);
            maxTempatureRadiators = FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
            radiatorPerformance = Math.Max(1 - (coldBathTemp / maxTempatureRadiators), 0.000001);
            partEmissiveConstant = part.emissiveConstant;
            base.OnFixedUpdate();
        }

        private void SetRatios()
        {
            fuels = string.Join(" : ", CurrentActiveConfiguration.Fuels);
            ratios = string.Join(" : ", CurrentActiveConfiguration.Ratios.Select(m => m.ToString()).ToArray());

            var typeMaskCount = CurrentActiveConfiguration.TypeMasks.Count();
            for (var i = 0; i < CurrentActiveConfiguration.Fuels.Count(); i++)
            {
                if (i < typeMaskCount && (CurrentActiveConfiguration.TypeMasks[i] & 1) == 1)
                {
                    SetRatio(CurrentActiveConfiguration.Fuels[i], (float)(CurrentActiveConfiguration.Ratios[i] * rateMultplier));
                }
            }
        }

        private void KillKerbalsWithRadiation(double radiationRatio)
        {
            UpdateKerbalismEmitter();

            if (!radhazard || radiationRatio <= 0.00 || rad_safety_features || killDivider <= 0) return;

            //System.Random rand = new System.Random(new System.DateTime().Millisecond);
            var vesselsToRemove = new List<Vessel>();
            var crewToRemove = new List<ProtoCrewMember>();
            double deathProb = TimeWarp.fixedDeltaTime;

            foreach (var vess in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                var invSqDist = distance / killDivider;
                var invSqMult = 1.0 / invSqDist / invSqDist;

                foreach (var crewMember in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1.0 - deathProb * invSqMult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg4", crewMember.name), 5.0f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Neutron Radiation!"
                        crewToRemove.Add(crewMember);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg5", crewMember.name), 5.0f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Neutron Radiation!"
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
                var crewpart = vess.Parts.Find(p => p.protoModuleCrew.Contains(crewMember));
                crewpart.RemoveCrewmember(crewMember);
                crewMember.Die();
            }
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title;
        }

        public override int getPowerPriority()
        {
            return 4;
        }    
    }
}
