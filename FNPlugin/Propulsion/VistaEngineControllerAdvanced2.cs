using FNPlugin.Constants;
using FNPlugin.Power;
using FNPlugin.Wasteheat;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin
{
    class VistaEngineControllerAdvanced2 : FusionEngineControllerBase2
    {
        const float maxMin = defaultMinIsp / defaultMaxIsp;
        const float defaultMaxIsp = 27200;
        const float defaultMinIsp = 15500;
        const float defaultSteps = (defaultMaxIsp - defaultMinIsp) / 100;
        const float stepNumb = 0;

        // Persistant setting
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_SelectedIsp"), UI_FloatRange(stepIncrement = defaultSteps, maxValue = defaultMaxIsp, minValue = defaultMinIsp)]//Selected Isp
        public float localIsp = defaultMinIsp + (stepNumb * defaultSteps);

        // settings
        [KSPField]
        public double neutronAbsorptionFractionAtMinIsp = 0.5;
        [KSPField]
        public double maxThrustEfficiencyByIspPower = 2;

        public float minIsp = 15500;
        public FloatCurve atmophereCurve;

        protected override FloatCurve OrigFloatCurve
        {
            get
            {
                return atmophereCurve;
            }
            set
            {
                bool test = atmophereCurve != null ? atmophereCurve.Evaluate(0) == 0 : true;

                if (test)
                {

                    atmophereCurve = value;
                }
            }
        }

        protected override float SelectedIsp { get { return localIsp; } set { if (value > 0) { localIsp = value; } } }
        protected override float MinIsp { get { return minIsp; } set { if (value <= 10) { minIsp = value + .01f;  } else { minIsp = value; } } }
        protected override float MaxIsp { get { return minIsp / maxMin; } }
        protected override float MaxMin { get { return maxMin; } }
        protected override double MaxThrustEfficiencyByIspPower { get { return maxThrustEfficiencyByIspPower; } }
        protected override double NeutronAbsorptionFractionAtMinIsp { get { return neutronAbsorptionFractionAtMinIsp; } }
    }

    //class DaedalusEngineControllerAdvanced : FusionEngineControllerBase
    //{
    //    const float maxIsp = 10000000f;
    //    //const float minIsp = 1000000f;
    //    //const float steps = (maxIsp - minIsp) / 100f;

    //    // Persistant setting
    //    //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Selected Isp"), UI_FloatRange(stepIncrement = steps, maxValue = maxIsp, minValue = minIsp)]
    //    public float localIsp = maxIsp;

    //    // settings
    //    [KSPField(isPersistant = false)]
    //    public float neutronAbsorptionFractionAtMinIsp = 0.5f;
    //    [KSPField(isPersistant = false)]
    //    public float maxThrustEfficiencyByIspPower = 2f;

    //    protected override float SelectedIsp { get { return localIsp; } }
    //    protected override float MaxIsp { get { return maxIsp; } }
    //    protected override float MaxThrustEfficiencyByIspPower { get { return maxThrustEfficiencyByIspPower; } }
    //    protected override float NeutronAbsorptionFractionAtMinIsp { get { return neutronAbsorptionFractionAtMinIsp; } }
    //}

    abstract class FusionEngineControllerBase2 : ResourceSuppliableModule, IUpgradeableModule
    {
        // Persistant
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool rad_safety_features = true;

        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk3 = 0.05f;

        // None Persistant
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_RadiationHazard")]//Radiation Hazard To
        public string radhazardstr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_Temperature")]//Temperature
        public string temperatureStr = "";

        [KSPField(isPersistant = false)]
        public float powerRequirement = 625;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded = 1250;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded2 = 2500;

        [KSPField(isPersistant = false)]
        public float maxThrust = 75;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 300;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded2 = 1200;

        [KSPField(isPersistant = false)]
        public double maxAtmosphereDensity = 0.001;
        [KSPField(isPersistant = false)]
        public float leathalDistance = 2000;
        [KSPField(isPersistant = false)]
        public float killDivider = 50;

        [KSPField(isPersistant = false)]
        public double efficiency = 0.19f;
        [KSPField(isPersistant = false)]
        public double efficiencyUpgraded = 0.38;
        [KSPField(isPersistant = false)]
        public double efficiencyUpgraded2 = 0.76;

        [KSPField(isPersistant = false)]
        public float fusionWasteHeat = 625;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded = 2500;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded2 = 10000;

        // Use for SETI Mode
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float powerRequirementMultiplier = 1;

        [KSPField(isPersistant = false)]
        public float maxTemp = 2500;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_upgradetech1")]//upgrade tech 1
        public string upgradeTechReq = "advFusionReactions";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_upgradetech2")]//upgrade tech 2
        public string upgradeTechReq2 = "exoticReactions";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_MaxThrust", guiUnits = " kN")]//Max Thrust
        public float maximumThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_CurrentThrotle", guiFormat = "F2")]//Current Throtle
        public float throttle;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_FusionRatio", guiFormat = "F2")]//Fusion Ratio
        public float fusionRatio;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_PowerRequirement", guiFormat = "F2", guiUnits = " MW")]//Power Requirement
        public float enginePowerRequirement;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_LaserWasteheat", guiFormat = "F2", guiUnits = " MW")]//Laser Wasteheat
        public double laserWasteheat;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_AbsorbedWasteheat", guiFormat = "F2", guiUnits = " MW")]//Absorbed Wasteheat
        public double absorbedWasteheat;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_RadiatorTemp")]//Radiator Temp
        public float coldBathTemp;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_MaxRadiatorTemp")]//Max Radiator Temp
        public float maxTempatureRadiators;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_PerformanceRadiators")]//Performance Radiators
        public float radiatorPerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_Emisiveness")]//Emisiveness
        public float partEmissiveConstant;



        // abstracts
        protected abstract float SelectedIsp { get; set; }
        protected abstract float MinIsp { get; set; }
        protected abstract float MaxIsp { get; }
        protected abstract float MaxMin { get; }
        protected abstract double MaxThrustEfficiencyByIspPower { get; }
        protected abstract double NeutronAbsorptionFractionAtMinIsp { get; }
        protected abstract FloatCurve OrigFloatCurve { get; set; }


        // protected
        protected bool hasrequiredupgrade = false;
        protected bool radhazard = false;
        protected double standard_megajoule_rate = 0;
        protected double standard_deuterium_rate = 0;
        protected double standard_tritium_rate = 0;
        protected ModuleEngines curEngineT;
        protected float CurveMaxISP;
        protected ResourceBuffers resourceBuffers;

        protected double Altitude, lastAltitude;

        public GenerationType EngineGenerationType { get; private set; }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_DeactivateRadSafety", active = true)]//Disable Radiation Safety
        public void DeactivateRadSafety()
        {
            rad_safety_features = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_ActivateRadSafety", active = false)]//Activate Radiation Safety
        public void ActivateRadSafety()
        {
            rad_safety_features = true;
        }

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public void upgradePartModule() { }

        public void DetermineTechLevel()
        {
            int numberOfUpgradeTechs = 1;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq2))
                numberOfUpgradeTechs++;

            if (numberOfUpgradeTechs == 3)
                EngineGenerationType = GenerationType.Mk3;
            else if (numberOfUpgradeTechs == 2)
                EngineGenerationType = GenerationType.Mk2;
            else
                EngineGenerationType = GenerationType.Mk2;
        }

        #endregion

        public double MaximumThrust
        {
            get
            {

                return FullTrustMaximum * Math.Pow((MinIsp / SelectedIsp), MaxThrustEfficiencyByIspPower) * MinIsp / CurveMaxISP;
            }
        }

        public float FusionWasteHeat
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return fusionWasteHeat;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return fusionWasteHeatUpgraded;
                else
                    return fusionWasteHeatUpgraded2;
            }
        }

        public float FullTrustMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return maxThrust;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return maxThrustUpgraded;
                else
                    return maxThrustUpgraded2;
            }
        }

        public double LaserEfficiency
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return efficiency;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return efficiencyUpgraded;
                else
                    return efficiencyUpgraded2;
            }
        }

        public float CurrentPowerRequirement
        {
            get
            {
                return PowerRequirementMaximum * powerRequirementMultiplier * throttle;
            }
        }

        public float PowerRequirementMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return powerRequirement;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return powerRequirementUpgraded;
                else
                    return powerRequirementUpgraded2;
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
                else
                    return minThrottleRatioMk3;
            }
        }
        public void FCsetup()
        {
            try
            {
                if (vessel.loaded)
                {

                    BaseField IspField = Fields["localIsp"];
                 
                    UI_FloatRange[] IspController = { IspField.uiControlFlight as UI_FloatRange, IspField.uiControlEditor as UI_FloatRange };

                    

                    for (int I = 0; I < IspController.Length; I++)
                    {
                        float akIsp = SelectedIsp;
                        float akMinIsp = IspController[I].minValue;
                        float akMaxIsp = IspController[I].maxValue;
                        float StepIncrement = IspController[I].stepIncrement;

                        float StepNumb = (akIsp - akMinIsp) / StepIncrement;
                        akMinIsp = (float)Math.Round(OrigFloatCurve.Evaluate((float)Altitude));
                        if (akMinIsp < 1) { akMinIsp = 1; }
                        akMaxIsp = (float)Math.Round(akMinIsp / MaxMin);
                        StepIncrement = (akMaxIsp - akMinIsp) / 100;

                        IspController[I].minValue = akMinIsp;
                        IspController[I].maxValue = akMaxIsp;
                        IspController[I].stepIncrement = StepIncrement;

                        SelectedIsp = akMinIsp + StepIncrement * StepNumb;
                        I++;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FusionEngine FCsetup exception: " + e.Message);
            }
        }
     
       
        public override void OnStart(PartModule.StartState state)
        {
            try
            {

                part.maxTemp = maxTemp;
                part.thermalMass = 1;
                part.thermalMassModifier = 1;
                EngineGenerationType = GenerationType.Mk1;
                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();
                if (curEngineT == null)
                {
                    Debug.LogError("FusionEngine OnStart Engine not found");
                    return;
                }
                OrigFloatCurve = curEngineT.atmosphereCurve;

                CurveMaxISP = GetMaxKey(OrigFloatCurve);

                standard_deuterium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.LqdDeuterium).ratio;
                standard_tritium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.LqdTritium).ratio;

                DetermineTechLevel();

                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, 2.0e+4, true));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                resourceBuffers.Init(this.part);

                if (state != StartState.Editor)
                    part.emissiveConstant = maxTempatureRadiators > 0 ? 1 - coldBathTemp / maxTempatureRadiators : 0.01;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FusionEngine OnStart eception: " + e.Message);
            }
        }
        

        public override void OnUpdate()
        {
            if (curEngineT == null) return;

            Events["DeactivateRadSafety"].active = rad_safety_features;
            Events["ActivateRadSafety"].active = !rad_safety_features;

            if (curEngineT.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                UnityEngine.Debug.Log("[KSPI]: VistaEngineAdvanced on " + part.name + " was Force Activated");
                part.force_activate();
            }

            int kerbal_hazard_count = 0;
            foreach (Vessel vess in FlightGlobals.Vessels)
            {
                float distance = (float)Vector3d.Distance(vessel.transform.position, vess.transform.position);
                if (distance < leathalDistance && vess != this.vessel)
                    kerbal_hazard_count += vess.GetCrewCount();
            }

            if (kerbal_hazard_count > 0)
            {
                radhazard = true;
                if (kerbal_hazard_count > 1)
                    radhazardstr = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_kerbalhazardcount2", kerbal_hazard_count.ToString());// + " Kerbals."
                else
                    radhazardstr = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_kerbalhazardcount1", kerbal_hazard_count.ToString());// + " Kerbal."

                Fields["radhazardstr"].guiActive = true;
            }
            else
            {
                Fields["radhazardstr"].guiActive = false;
                radhazard = false;
                radhazardstr = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_None");//"None."
            }
        }

        public void ShutDown(string reason)
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

        public float GetMaxKey(FloatCurve akCurve)
        {
            int i = 0;
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
         public void  UpdateISP()
        {
           
            FloatCurve newIsp = new FloatCurve();
            Altitude = vessel.atmDensity;
            float OrigISP = OrigFloatCurve.Evaluate((float)Altitude);
            if (Altitude != lastAltitude){FCsetup(); lastAltitude = Altitude;} // save resources when it's out of the atmoshpere. 
            newIsp.Add((float)Altitude, SelectedIsp);
            curEngineT.atmosphereCurve = newIsp;
            MinIsp = OrigISP;

        }

  
        public override void OnFixedUpdate()
        {
            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
            MinIsp = OrigFloatCurve.Evaluate((float)Altitude);

           // part.ona

           if (curEngineT == null) return;

            throttle = curEngineT.currentThrottle > MinThrottleRatio ? curEngineT.currentThrottle : 0;

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg1"));//"Inertial Fusion cannot operate in atmosphere!"

                if (radhazard && rad_safety_features)
                    ShutDown(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg2"));//"Engines throttled down as they presently pose a radiation hazard"
               
                if (SelectedIsp <= 10) 
                    ShutDown(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg4"));//"Engine Stall"
            }

            KillKerbalsWithRadiation(throttle);

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.UpdateBuffers();

            if (throttle > 0)
            {
                // Calculate Fusion Ratio
                enginePowerRequirement = CurrentPowerRequirement;

                var recievedPowerFixed = CheatOptions.InfiniteElectricity
                    ? enginePowerRequirement
                    : consumeFNResourcePerSecond(enginePowerRequirement, ResourceManager.FNRESOURCE_MEGAJOULES);

                var plasma_ratio = recievedPowerFixed / enginePowerRequirement;
                fusionRatio = plasma_ratio >= 1 ? 1 : plasma_ratio > 0.75f ? Mathf.Pow((float)plasma_ratio, 6) : 0;

                laserWasteheat = recievedPowerFixed * (1 - LaserEfficiency);

                // Lasers produce Wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                    supplyFNResourcePerSecond(enginePowerRequirement, ResourceManager.FNRESOURCE_WASTEHEAT);

                // The Absorbed wasteheat from Fusion
                var rateMultplier = MinIsp / SelectedIsp;
                var neutronbsorbionBonus = 1 - NeutronAbsorptionFractionAtMinIsp * (1 - ((SelectedIsp - MinIsp) / (MaxIsp - MinIsp)));
                absorbedWasteheat = FusionWasteHeat * wasteHeatMultiplier * fusionRatio * throttle * neutronbsorbionBonus;
                supplyFNResourcePerSecond(absorbedWasteheat, ResourceManager.FNRESOURCE_WASTEHEAT);

                // change ratio propellants Hydrogen/Fusion
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.LqdDeuterium).ratio = (float)standard_deuterium_rate / rateMultplier;
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.LqdTritium).ratio = (float)standard_tritium_rate / rateMultplier;

                // Update ISP
                var currentIsp = SelectedIsp;
                UpdateISP();

                // Update FuelFlow
                var maxFuelFlow = fusionRatio * MaximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;
                maximumThrust = (float)MaximumThrust;

                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = maximumThrust;
                

                if (!curEngineT.getFlameoutState && plasma_ratio < 0.75 && recievedPowerFixed > 0)
                    curEngineT.status = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_statu");//"Insufficient Electricity"
            }
            else
            {
                enginePowerRequirement = 0;
                absorbedWasteheat = 0;
                laserWasteheat = 0;
                fusionRatio = 0;
                var currentIsp = SelectedIsp;

                UpdateISP();
                curEngineT.maxThrust = (float)MaximumThrust;
                var rateMultplier = MinIsp / SelectedIsp;

                var maxFuelFlow = MaximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.LqdDeuterium).ratio = (float)(standard_deuterium_rate) / rateMultplier;
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.LqdTritium).ratio = (float)(standard_tritium_rate) / rateMultplier;
            }
      
            coldBathTemp = (float)FNRadiator.getAverageRadiatorTemperatureForVessel(vessel);
            maxTempatureRadiators = (float)FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
            radiatorPerformance = Mathf.Max(1 - (coldBathTemp / maxTempatureRadiators), 0.000001f);
            partEmissiveConstant = (float)part.emissiveConstant;
        }

        private void KillKerbalsWithRadiation(float throttle)
        {
            if (!radhazard || throttle <= 0.00 || rad_safety_features) return;

            System.Random rand = new System.Random(new System.DateTime().Millisecond);
            List<Vessel> vessels_to_remove = new List<Vessel>();
            List<ProtoCrewMember> crew_to_remove = new List<ProtoCrewMember>();
            double death_prob = TimeWarp.fixedDeltaTime;

            foreach (Vessel vess in FlightGlobals.Vessels)
            {
                float distance = (float)Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                float inv_sq_dist = distance / killDivider;
                float inv_sq_mult = 1.0f / inv_sq_dist / inv_sq_dist;
                foreach (ProtoCrewMember crew_member in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1.0 - death_prob * inv_sq_mult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg3", crew_member.name), 5.0f, ScreenMessageStyle.UPPER_CENTER);//<<1>> was killed by Neutron Radiation!"
                        crew_to_remove.Add(crew_member);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg3", crew_member.name), 5.0f, ScreenMessageStyle.UPPER_CENTER);// <<1>> was killed by Neutron Radiation!"
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

        public override int getPowerPriority()
        {
            return 3;
        }
    }

}

