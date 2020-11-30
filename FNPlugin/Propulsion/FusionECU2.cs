using FNPlugin.Constants;
using FNPlugin.External;
using FNPlugin.Power;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    abstract class FusionECU2 : EngineECU2
    {
        // Persistent
        [KSPField(isPersistant = true)]
        bool rad_safety_features = true;

        // None Persistent fields
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionECU2_Fuels")]//Fuels
        public string fuels;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionECU2_Ratios")]//Ratios
        public string ratios;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionECU2_Isp", guiFormat = "F3", guiUnits = " s")]//Isp
        public float currentIsp;

        [KSPField]
        public string fuelSwitchName = "Fusion Type";

        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_FusionECU2_PowerRequirementMk1", guiActiveEditor = true, guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Power Requirement Mk1
        public double powerRequirementMax;

        [KSPField]
        public double powerRequirement = 0;
        [KSPField]
        public double powerRequirementUpgraded1 = 0;
        [KSPField]
        public double powerRequirementUpgraded2 = 0;
        [KSPField]
        public double powerRequirementUpgraded3 = 0;
        [KSPField]
        public double powerRequirementUpgraded4 = 0;

        [KSPField]
        public double powerProduction = 0;
        [KSPField]
        public double powerProductionUpgraded1 = 0;
        [KSPField]
        public double powerProductionUpgraded2 = 0;
        [KSPField]
        public double powerProductionUpgraded3 = 0;
        [KSPField]
        public double powerProductionUpgraded4 = 0;

        [KSPField]
        public bool selectableIsp = false;
        [KSPField]
        public double maxAtmosphereDensity = -1;
        [KSPField]
        public double leathalDistance = 2000;
        [KSPField]
        public double killDivider = 0;
        [KSPField]
        public int powerPriority = 4;

        [KSPField(groupName = GROUP, guiName = "Wasteheat Mk1", guiActiveEditor = true, guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double fusionWasteHeatMax;

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
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double neutronbsorbionBonus;

        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_AvailablePower", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Available Power
        public double availablePower;
        [KSPField(groupName = GROUP, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_FusionECU2_MaxPowerRequirement", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Max Power Requirement
        public double currentMaximumPowerRequirement;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_MaxPowerProduction", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Max Power Production
        public double currentMaximumPowerProduction;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionECU2_LaserWasteheat", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Laser Wasteheat
        public double laserWasteheat;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_FusionECU2_AbsorbedWasteheat", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Absorbed Wasteheat
        public double absorbedWasteheat;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_FusionECU2_RadiatorTemp")]//Radiator Temp
        public double coldBathTemp;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_FusionECU2_MaxRadiatorTemp")]//Max Radiator Temp
        public float maxTempatureRadiators;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_FusionECU2_PerformanceRadiators")]//Performance Radiators
        public double radiatorPerformance;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_FusionECU2_Emisiveness")]//Emisiveness
        public double partEmissiveConstant;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_Temperature")]//Temperature
        public string temperatureStr = "";
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_Radhazardstr")]//Radiation Hazard To
        public string radhazardstr = "";
        [KSPField]
        protected float curveMaxISP; // ToDo: make sure it is properly initialized after  comming from assembly
        [KSPField]
        public double radius = 1;

        [KSPField]
        public double requiredPowerPerSecond;
        [KSPField]
        public double producedPowerPerSecond;
        [KSPField]
        public double requestedPowerPerSecond;
        [KSPField]
        public double recievedPowerPerSecond;

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

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_DisableRadiationSafety", active = true)]//Disable Radiation Safety
        public void DeactivateRadSafety()
        {
            rad_safety_features = false;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_ActivateRadiationSafety", active = false)]//Activate Radiation Safety
        public void ActivateRadSafety()
        {
            rad_safety_features = true;
        }

        #region IUpgradeableModule

        public string UpgradeTechnology => upgradeTechReq1;

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

        public double MaximumThrust => FullTrustMaximum * Math.Pow((MinIsp / SelectedIsp), MaxThrustEfficiencyByIspPower) * MinIsp / curveMaxISP;

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

        public double GetCurrentMaximumPowerRequirement()
        {
            return PowerRequirementMaximum * powerRequirementMultiplier * PowerMult();
        }

        public double PowerRequirementMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return powerRequirement;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return powerRequirementUpgraded1;
                else if (EngineGenerationType == GenerationType.Mk3)
                    return powerRequirementUpgraded2;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return powerRequirementUpgraded3;
                else
                    return powerRequirementUpgraded4;
            }
        }

        public double GetCurrentMaximumPowerProduction()
        {
            return PowerProductionMaximum * powerRequirementMultiplier * PowerMult();
        }

        public double PowerProductionMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return powerProduction;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return powerProductionUpgraded1;
                else if (EngineGenerationType == GenerationType.Mk3)
                    return powerProductionUpgraded2;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return powerProductionUpgraded3;
                else
                    return powerProductionUpgraded4;
            }
        }

        private double PowerMult ()
        {
            return FuelConfigurations.Count > 0 ? CurrentActiveConfiguration.powerMult : 1;
        }

        public bool HasIspThrottling()
        {
            return FuelConfigurations.Count <= 0 || CurrentActiveConfiguration.hasIspThrottling;
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

            BaseField ispField = Fields["localIsp"];

            UI_FloatRange[] ispController = { ispField.uiControlFlight as UI_FloatRange, ispField.uiControlEditor as UI_FloatRange };

            ispField.OnValueModified += IspField_OnValueModified;

            for (int i = 0; i < ispController.Length; i++)
            {
                float akIsp = SelectedIsp;
                float akMinIsp = ispController[i].minValue;
                float akMaxIsp = ispController[i].maxValue;
                float stepIncrement = ispController[i].stepIncrement;
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

                ispController[i].minValue = akMinIsp;
                ispController[i].maxValue = akMaxIsp;
                ispController[i].stepIncrement = stepIncrement;

                SelectedIsp = akMinIsp + stepIncrement * stepNumb;
                i++;
            }
            lastAltitude = Altitude;
        }

        private void IspField_OnValueModified(object arg1)
        {

        }

        public override void UpdateFuel(bool isEditor = false)
        {
            base.UpdateFuel(isEditor);

            Debug.Log("[KSPI]: Fusion Gui Updated");
            BaseFloatCurve = CurrentActiveConfiguration.atmosphereCurve;
            curveMaxISP = GetMaxKey(BaseFloatCurve);
            FcSetup();
            Debug.Log("[KSPI]: Curve Max ISP:" + curveMaxISP);
        }

        public override void OnStart(StartState state)
        {
            if (state.ToString().Contains(StartState.PreLaunch.ToString()))
            {
                Debug.Log("[KSPI]: PreLaunch uses InitialGearRatio:" + InitialGearRatio);
                SelectedIsp = ((MaxIsp - MinIsp) * Math.Max(0, Math.Min(1, InitialGearRatio))) + MinIsp;
            }

            Fields[nameof(selectedFuel)].guiName = fuelSwitchName;

            Fields[nameof(currentMaximumPowerRequirement)].guiActive = powerRequirement > 0;
            Fields[nameof(laserWasteheat)].guiActive = powerRequirement > 0 && fusionWasteHeat > 0;
            Fields[nameof(absorbedWasteheat)].guiActive = powerRequirement > 0 && fusionWasteHeat > 0;
            Fields[nameof(fusionRatio)].guiActive = powerRequirement > 0;

            Fields[nameof(powerRequirementMax)].guiActiveEditor = powerRequirement > 0;
            Fields[nameof(fusionWasteHeatMax)].guiActiveEditor = fusionWasteHeat > 0;

            part.maxTemp = maxTemp;
            part.thermalMass = 1;
            part.thermalMassModifier = 1;

            curEngineT = part.FindModuleImplementing<ModuleEngines>();
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
            powerRequirementMax = PowerRequirementMaximum;
            fusionWasteHeatMax = FusionWasteHeat;

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, 1.0e+4, true));
            resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);
            resourceBuffers.Init(this.part);

            if (state != StartState.Editor)
                part.emissiveConstant = maxTempatureRadiators > 0 ? 1 - coldBathTemp / maxTempatureRadiators : 0.01;

            base.OnStart(state);

            Fields["localIsp"].guiActive = selectableIsp;
            Fields["localIsp"].guiActiveEditor = selectableIsp;
        }

        private double GetRatio(string akPropName)
        {
            var firstOrDefault = curEngineT.propellants.FirstOrDefault(pr => pr.name == akPropName);

            return firstOrDefault?.ratio ?? 0;
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

            Events[nameof(DeactivateRadSafety)].active = rad_safety_features;
            Events[nameof(ActivateRadSafety)].active = !rad_safety_features;

            if (curEngineT.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                UnityEngine.Debug.Log("[KSPI]: FusionECU2 on " + part.name + " was Force Activated");
                part.force_activate();
            }

            var kerbalHazardCount = 0;
            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                var distance = (float)Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);
                if (distance < leathalDistance && currentVessel != vessel)
                    kerbalHazardCount += currentVessel.GetCrewCount();
            }

            if (kerbalHazardCount > 0)
            {
                radhazard = true;
                if (kerbalHazardCount > 1)
                    radhazardstr = kerbalHazardCount + " Kerbals.";
                else
                    radhazardstr = kerbalHazardCount + " Kerbal.";

                Fields[nameof(radhazardstr)].guiActive = true;
            }
            else
            {
                Fields[nameof(radhazardstr)].guiActive = false;
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
            curEngineT.Events[nameof(curEngineT.Shutdown)].Invoke();
            curEngineT.currentThrottle = 0;
            curEngineT.requestedThrottle = 0;

            ScreenMessages.PostScreenMessage(reason, 5.0f, ScreenMessageStyle.UPPER_CENTER);
            HideExhaust();
        }

        private void HideExhaust()
        {
            foreach (var fxGroup in part.fxGroups)
            {
                fxGroup.setActive(false);
            }
        }

        private void ShowExhaust()
        {
            foreach (var fxGroup in part.fxGroups)
            {
                fxGroup.setActive(true);
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
            //MinIsp = currentIsp;
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

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            temperatureStr = part.temperature.ToString("F0") + "K / " + part.maxTemp.ToString("F0") + "K";
            MinIsp = BaseFloatCurve.Evaluate((float)Altitude);

            resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);
            resourceBuffers.UpdateBuffers();

            if (curEngineT == null || !curEngineT.isEnabled) return;

            if (curEngineT.requestedThrottle > 0)
            {
                //if (vessel.atmDensity > maxAtmosphereDensity || vessel.atmDensity > _currentActiveConfiguration.maxAtmosphereDensity)
                //    ShutDown(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg1"));//"Inertial Fusion cannot operate in atmosphere!"

                if (radhazard && rad_safety_features)
                    ShutDown(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg2"));//"Engines throttled down as they presently pose a radiation hazard"

                //if (MinIsp <= 100)
                //    ShutDown(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg3"));//"Engine Stall"
            }

            KillKerbalsWithRadiation(fusionRatio);

            hasIspThrottling = HasIspThrottling();

            ShowIspThrottle = hasIspThrottling;

            availablePower = Math.Max(getResourceAvailability(ResourceSettings.Config.ElectricPowerInMegawatt), getAvailablePrioritisedStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt));

            currentMaximumPowerProduction = GetCurrentMaximumPowerProduction();
            currentMaximumPowerRequirement = GetCurrentMaximumPowerRequirement();

            requiredPowerPerSecond = curEngineT.currentThrottle * currentMaximumPowerRequirement;

            if (curEngineT.currentThrottle > 0)
            {
                requestedPowerPerSecond = Math.Min(requiredPowerPerSecond, availablePower);

                recievedPowerPerSecond = requestedPowerPerSecond <= 0 ? 0
                    : CheatOptions.InfiniteElectricity
                        ? requiredPowerPerSecond
                        : consumeFNResourcePerSecond(requestedPowerPerSecond, ResourceSettings.Config.ElectricPowerInMegawatt);

                fusionRatio = requiredPowerPerSecond > 0 ? Math.Min(1, recievedPowerPerSecond / requiredPowerPerSecond) : 1;

                var inefficiency = 1 - LaserEfficiency;

                laserWasteheat = recievedPowerPerSecond * inefficiency;
                producedPowerPerSecond = fusionRatio * currentMaximumPowerProduction;

                if (!CheatOptions.InfiniteElectricity && currentMaximumPowerProduction > 0)
                    supplyFNResourcePerSecondWithMax(producedPowerPerSecond, currentMaximumPowerProduction, ResourceSettings.Config.ElectricPowerInMegawatt);

                // Lasers produce Wasteheat
                if (!CheatOptions.IgnoreMaxTemperature && laserWasteheat > 0)
                    supplyFNResourcePerSecondWithMax(laserWasteheat, currentMaximumPowerRequirement * inefficiency, ResourceSettings.Config.WasteHeatInMegawatt);

                // The Absorbed wasteheat from Fusion
                rateMultplier = hasIspThrottling ? Math.Pow(SelectedIsp / MinIsp, 2) : 1;
                neutronbsorbionBonus = hasIspThrottling ? 1 - NeutronAbsorptionFractionAtMinIsp * (1 - ((SelectedIsp - MinIsp) / (MaxIsp - MinIsp))) : 0.5;
                absorbedWasteheat = FusionWasteHeat * wasteHeatMultiplier * fusionRatio * curEngineT.currentThrottle * neutronbsorbionBonus;
                supplyFNResourcePerSecond(absorbedWasteheat, ResourceSettings.Config.WasteHeatInMegawatt);

                SetRatios();

                currentIsp = hasIspThrottling ? SelectedIsp : MinIsp;
                UpdateAtmosphereCurve(currentIsp);
                maximumThrust = hasIspThrottling ? MaximumThrust : FullTrustMaximum;

                // Update FuelFlow
                maxFuelFlow = fusionRatio * maximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;

                if ((maxAtmosphereDensity >= 0 && vessel.atmDensity > maxAtmosphereDensity)
                      || (_currentActiveConfiguration.maxAtmosphereDensity >= 0 && vessel.atmDensity > _currentActiveConfiguration.maxAtmosphereDensity))
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg1"), 1.0f, ScreenMessageStyle.UPPER_CENTER);
                    curEngineT.maxFuelFlow = 1e-10f;
                    curEngineT.maxThrust = Mathf.Max((float)maximumThrust, 0.0001f);
                    HideExhaust();
                }
                else if (MinIsp < _currentActiveConfiguration.minIsp)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg3"), 1.0f, ScreenMessageStyle.UPPER_CENTER);
                    curEngineT.maxFuelFlow = 1e-10f;
                    curEngineT.maxThrust = Mathf.Max((float)maximumThrust, 0.0001f);
                    HideExhaust();
                }
                else
                {
                    curEngineT.maxFuelFlow = Mathf.Max((float)maxFuelFlow, 1e-10f);
                    curEngineT.maxThrust = Mathf.Max((float)maximumThrust, 0.0001f);
                }

                if (!curEngineT.getFlameoutState && fusionRatio < 0.9 && recievedPowerPerSecond > 0)
                    curEngineT.status = Localizer.Format("#LOC_KSPIE_FusionECU2_statu1");//"Insufficient Electricity"
            }
            else
            {
                absorbedWasteheat = 0;
                laserWasteheat = 0;
                requestedPowerPerSecond = 0;
                recievedPowerPerSecond = 0;

                fusionRatio = requiredPowerPerSecond > 0 ? Math.Min(1, availablePower / requiredPowerPerSecond) : 1;

                currentIsp = hasIspThrottling ? SelectedIsp : MinIsp;
                maximumThrust = hasIspThrottling ? MaximumThrust : FullTrustMaximum;

                UpdateAtmosphereCurve(currentIsp);

                rateMultplier = hasIspThrottling ? Math.Pow(SelectedIsp / MinIsp, 2) : 1;

                maxFuelFlow = fusionRatio * maximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;

                if ((maxAtmosphereDensity >= 0 && vessel.atmDensity > maxAtmosphereDensity)
                    || (_currentActiveConfiguration.maxAtmosphereDensity >= 0 && vessel.atmDensity > _currentActiveConfiguration.maxAtmosphereDensity))
                {
                    curEngineT.maxFuelFlow = 1e-10f;
                    curEngineT.maxThrust = Mathf.Max((float)maximumThrust, 0.0001f);
                    HideExhaust();
                }
                else if (MinIsp < _currentActiveConfiguration.minIsp)
                {
                    curEngineT.maxFuelFlow = 1e-10f;
                    curEngineT.maxThrust = Mathf.Max((float)maximumThrust, 0.0001f);
                    HideExhaust();
                }
                else
                {
                    curEngineT.maxFuelFlow = Mathf.Max((float)maxFuelFlow, 1e-10f);
                    curEngineT.maxThrust = Mathf.Max((float)maximumThrust, 0.0001f);
                }

                SetRatios();
            }

            coldBathTemp = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel);
            maxTempatureRadiators = FNRadiator.GetAverageMaximumRadiatorTemperatureForVessel(vessel);
            radiatorPerformance = Math.Max(1 - (coldBathTemp / maxTempatureRadiators), 0.000001);
            partEmissiveConstant = part.emissiveConstant;
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

            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);

                if (distance >= leathalDistance || currentVessel == this.vessel || currentVessel.GetCrewCount() <= 0) continue;

                var invSqDistance = distance / killDivider;
                var invSqMultiplier = 1.0 / invSqDistance / invSqDistance;

                foreach (var crewMember in currentVessel.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1.0 - deathProb * invSqMultiplier)) continue;

                    if (!currentVessel.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg4", crewMember.name), 5.0f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Neutron Radiation!"
                        crewToRemove.Add(crewMember);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_FusionECU2_PostMsg5", crewMember.name), 5.0f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Neutron Radiation!"
                        vesselsToRemove.Add(currentVessel);
                    }
                }
            }

            foreach (var currentVessel in vesselsToRemove)
            {
                currentVessel.rootPart.Die();
            }

            foreach (var crewMember in crewToRemove)
            {
                var foundVessel = FlightGlobals.Vessels.Find(p => p.GetVesselCrew().Contains(crewMember));
                var crewPart = foundVessel.Parts.Find(p => p.protoModuleCrew.Contains(crewMember));
                crewPart.RemoveCrewmember(crewMember);
                crewMember.Die();
            }
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title;
        }

        public override int getPowerPriority()
        {
            // when providing surplus power, we want to be one of the first to consume and therefore provide power
            return PowerProductionMaximum > PowerRequirementMaximum ? 1 : powerPriority;
        }

        public override int getSupplyPriority()
        {
            return 1;
        }
    }
}
