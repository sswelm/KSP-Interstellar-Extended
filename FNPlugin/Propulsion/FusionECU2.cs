using FNPlugin.Power;
using FNPlugin.Constants;
using FNPlugin.Wasteheat;
using FNPlugin.External;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    abstract class FusionECU2 : EngineECU2
    {
        // Persistant
        [KSPField(isPersistant = true)]
        bool rad_safety_features = true;

        // None Persistant
        [KSPField(isPersistant = false, guiActive = true, guiName = "Radiation Hazard To")]
        public string radhazardstr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature")]
        public string temperatureStr = "";

        [KSPField]
        public string fuelSwitchName = "Fusion Type";
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
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public float currentIsp;
        [KSPField(guiActive = false, guiActiveEditor = false)]
        public double neutronbsorbionBonus;


        [KSPField(isPersistant = true, guiName = "Use MJ Battery"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool useMegajouleBattery = false;

        [KSPField(guiActive = false, guiName = "Fusion Ratio", guiFormat = "F2")]
        public double fusionRatio;
        [KSPField(guiActive = true, guiName = "Power Requirement", guiFormat = "F2", guiUnits = " MW")]
        public double enginePowerRequirement;
        [KSPField(guiActive = true, guiName = "Laser Wasteheat", guiFormat = "F2", guiUnits = " MW")]
        public double laserWasteheat;
        [KSPField(guiActive = true, guiName = "Absorbed Wasteheat", guiFormat = "F2", guiUnits = " MW")]
        public double absorbedWasteheat;
        [KSPField(guiName = "Radiator Temp")]
        public double coldBathTemp;
        [KSPField(guiName = "Max Radiator Temp")]
        public float maxTempatureRadiators;
        [KSPField(guiName = "Performance Radiators")]
        public double radiatorPerformance;
        [KSPField(guiName = "Emisiveness")]
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

        public double CurrentPowerRequirement
        {
            get
            {
                enginePowerRequirement = PowerRequirementMaximum*powerRequirementMultiplier * throttle;
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
            if (!HighLogic.LoadedSceneIsFlight ||  vessel == null || !vessel.loaded || Altitude == lastAltitude) return;

            FcSetup();
            lastAltitude = Altitude;
        }

        private void FcSetup()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            try
            {
                Altitude = vessel.atmDensity;

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

            if (isEditor) return;

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
                Fields["laserWasteheat"].guiActive = powerRequirement > 0;
                Fields["absorbedWasteheat"].guiActive = powerRequirement > 0;
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
            while (i < akCurve.Curve.keys.Length)
            {
                if (max < akCurve.Curve.keys[i].value)
                {
                    max = akCurve.Curve.keys[i].value;
                }

                i++;
            }

            return max;
        }

        private void UpdateAtmosphereCurve(float currentIsp )
        {
                var newIsp = new FloatCurve();
                Altitude = vessel.atmDensity;
                var origIsp = BaseFloatCurve.Evaluate((float)Altitude);

                FcUpdate();
                newIsp.Add((float)Altitude, currentIsp);
                curEngineT.atmosphereCurve = newIsp;
                MinIsp = origIsp;
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
                    ShutDown("Inertial Fusion cannot operate in atmosphere!");

                if (radhazard && rad_safety_features)
                    ShutDown("Engines throttled down as they presently pose a radiation hazard");

                if (SelectedIsp <= 10)
                    ShutDown("Engine Stall");
            }

            KillKerbalsWithRadiation(fusionRatio);

            hasIspThrottling = HasIspThrottling();

            ShowIspThrottle = hasIspThrottling;

            if (throttle > 0 )
            {
                // Calculate Fusion Ratio
                var requestedPowerPerSecond = CurrentPowerRequirement;

                var availablePower = useMegajouleBattery 
                    ? getResourceAvailability(ResourceManager.FNRESOURCE_MEGAJOULES) 
                    : getAvailablePrioritisedStableSupply(ResourceManager.FNRESOURCE_MEGAJOULES);

                var resourceBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);
                var effectivePowerThrotling = useMegajouleBattery ? 1 : resourceBarRatio > 0.1 ? 1 : resourceBarRatio * 10;

                var requestedPower = Math.Min(requestedPowerPerSecond, availablePower * effectivePowerThrotling);

                var recievedPowerPerSecond = CheatOptions.InfiniteElectricity || requestedPower <= 0
                    ? requestedPowerPerSecond
                    : consumeFNResourcePerSecond(requestedPower, ResourceManager.FNRESOURCE_MEGAJOULES);

                fusionRatio = requestedPowerPerSecond > 0 ? recievedPowerPerSecond / requestedPowerPerSecond : 1;

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
                var maxFuelFlow = fusionRatio * maximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;

                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = (float)maximumThrust;

                if (!curEngineT.getFlameoutState && fusionRatio < 0.9 && recievedPowerPerSecond > 0)
                    curEngineT.status = "Insufficient Electricity";
            }
            else
            {
                enginePowerRequirement = 0;
                absorbedWasteheat = 0;
                laserWasteheat = 0;
                fusionRatio = 0;
                currentIsp = hasIspThrottling ? SelectedIsp : MinIsp;
                maximumThrust = hasIspThrottling ? MaximumThrust : FullTrustMaximum;

                UpdateAtmosphereCurve(currentIsp);
                curEngineT.maxThrust = (float)maximumThrust;
                rateMultplier = hasIspThrottling ? Math.Pow(SelectedIsp / MinIsp, 2) : 1;

                var maxFuelFlow = maximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;

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
                        ScreenMessages.PostScreenMessage(crewMember.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        crewToRemove.Add(crewMember);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(crewMember.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
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
