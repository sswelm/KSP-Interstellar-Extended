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

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double powerRequirement = 625;
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double powerRequirementUpgraded = 1250;
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double powerRequirementUpgraded2 = 2500;

        [KSPField(isPersistant = false)]
        public bool selectableIsp = false;

        [KSPField(isPersistant = false)]
        public double maxAtmosphereDensity = 0.001;
        [KSPField(isPersistant = false)]
        public double leathalDistance = 2000;
        [KSPField(isPersistant = false)]
        public double killDivider = 50;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double fusionWasteHeat = 625;
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double fusionWasteHeatUpgraded = 2500;
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public double fusionWasteHeatUpgraded2 = 10000;

        // Use for SETI Mode
        [KSPField(isPersistant = false)]
        public double wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public double powerRequirementMultiplier = 1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false)]
        public double powerMultiplier;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Fusion Ratio", guiFormat = "F2")]
        public double fusionRatio;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Power Requirement", guiFormat = "F2", guiUnits = " MW")]
        public double enginePowerRequirement;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Laser Wasteheat", guiFormat = "F2", guiUnits = " MW")]
        public double laserWasteheat;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Absorbed Wasteheat", guiFormat = "F2", guiUnits = " MW")]
        public double absorbedWasteheat;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Radiator Temp")]
        public double coldBathTemp;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Radiator Temp")]
        public float maxTempatureRadiators;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Performance Radiators")]
        public double radiatorPerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Emisiveness")]
        public double partEmissiveConstant;


        // abstracts
        protected abstract float SelectedIsp { get; set; }
        protected abstract float MinIsp { get; set; }
        protected abstract float MaxIsp { get; }
        protected abstract float MaxMin { get; }
        protected abstract float MaxSteps { get; }
        protected abstract float MaxThrustEfficiencyByIspPower { get; }
        protected abstract float NeutronAbsorptionFractionAtMinIsp { get; }
        protected abstract FloatCurve BaseFloatCurve { get; set; }

        // protected
        protected bool hasrequiredupgrade = false;
        protected bool radhazard = false;
        protected double standard_megajoule_rate = 0;
        protected double standard_deuterium_rate = 0;
        protected double standard_tritium_rate = 0;
        protected string FuelConfigName = "Fusion Type";

        
        protected float CurveMaxISP;

        protected double Altitude, lastAltitude;



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

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public void upgradePartModule() { }

        #endregion

        public double MaximumThrust
        {
            get
            {
                return FullTrustMaximum * Math.Pow((MinIsp / SelectedIsp), MaxThrustEfficiencyByIspPower) * MinIsp / CurveMaxISP;               
            }
        }

        public double FusionWasteHeat
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

        public double FullTrustMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return MaxThrust;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return MaxThrustUpgraded;
                else
                    return MaxThrustUpgraded2;
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

        public double CurrentPowerRequirement
        {
            get
            {
                return PowerRequirementMaximum * powerRequirementMultiplier * throttle;
            }
        }

        public double PowerRequirementMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return powerRequirement * powerMult();
                else if (EngineGenerationType == GenerationType.Mk2)
                    return powerRequirementUpgraded * powerMult();
                else
                    return powerRequirementUpgraded2 * powerMult(); 
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
        private double powerMult ()
        {
            //powerMultiplier =(FuelConfigurations.Count > 0 ? ActiveConfiguration.powerMult : 1) * (scale == 0 ? 1 : Math.Pow(scale, 2));
            powerMultiplier = FuelConfigurations.Count > 0 ? ActiveConfiguration.powerMult : 1;
            return powerMultiplier;
        }

        public void FCUpdate()
        {
            if (vessel.loaded && Altitude != lastAltitude)
            {
                FCSetup();
                lastAltitude = Altitude;
            }
        }

        public void FCSetup()
        {
            try
            {
                Altitude = vessel.atmDensity;

                BaseField IspField = Fields["localIsp"];

                UI_FloatRange[] IspController = { IspField.uiControlFlight as UI_FloatRange, IspField.uiControlEditor as UI_FloatRange };

                IspField.OnValueModified += IspField_OnValueModified;

                for (int I = 0; I < IspController.Length; I++)
                {
                    float akIsp = SelectedIsp;
                    float akMinIsp = IspController[I].minValue;
                    float akMaxIsp = IspController[I].maxValue;
                    float StepIncrement = IspController[I].stepIncrement;
                    float StepNumb = (akIsp - akMinIsp) / StepIncrement;

                    if (StepNumb < 0)
                        StepNumb = 0;
                    else
                        if (StepNumb > MaxSteps) StepNumb = MaxSteps;

                    akMinIsp = (float)Math.Round(BaseFloatCurve.Evaluate((float)Altitude));

                    if (akMinIsp < 1)
                        akMinIsp = 1;

                    akMaxIsp = (float)Math.Round(akMinIsp / MaxMin);
                    StepIncrement = (akMaxIsp - akMinIsp) / 100;

                    IspController[I].minValue = akMinIsp;
                    IspController[I].maxValue = akMaxIsp;
                    IspController[I].stepIncrement = StepIncrement;

                    SelectedIsp = akMinIsp + StepIncrement * StepNumb;
                    I++;
                }
                lastAltitude = Altitude;

            }
            catch (Exception e)
            {
                Debug.LogError("FusionEngine FCUpdate exception: " + e.Message);
            }
        }

        private void IspField_OnValueModified(object arg1)
        {
            
        }

        public override void UpdateFuel(bool isEditor = false)
        {
            base.UpdateFuel(isEditor);
            if (!isEditor)
            {

                Debug.Log("Fusion Gui Updated");
                BaseFloatCurve = ActiveConfiguration.atmosphereCurve;
                standard_deuterium_rate = GetRatio(InterstellarResourcesConfiguration.Instance.LqdDeuterium);
                standard_tritium_rate = GetRatio(InterstellarResourcesConfiguration.Instance.LqdTritium);
                CurveMaxISP = GetMaxKey(BaseFloatCurve);
                FCSetup();
                Debug.Log("Curve Max ISP:" + CurveMaxISP);
            }

        }

        public override void OnStart(PartModule.StartState state)
        {
            try
            {
                Fields["selectedFuel"].guiName = "Fusion Type";
               
                part.maxTemp = maxTemp;
                part.thermalMass = 1;
                part.thermalMassModifier = 1;

                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();
                if (curEngineT == null)
                {
                    Debug.LogError("FusionEngine OnStart Engine not found");
                    return;
                }
                BaseFloatCurve = curEngineT.atmosphereCurve;

                CurveMaxISP = GetMaxKey(BaseFloatCurve);
                if (hasMultipleConfigurations) FCSetup();

                standard_deuterium_rate = GetRatio(InterstellarResourcesConfiguration.Instance.LqdDeuterium);
                standard_tritium_rate = GetRatio(InterstellarResourcesConfiguration.Instance.LqdTritium);

                DetermineTechLevel();

                // calculate WasteHeat Capacity
                var wasteheatPowerResource = part.Resources.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);
                if (wasteheatPowerResource != null)
                {
                    var wasteheat_ratio = Math.Min(wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount, 0.95);
                    wasteheatPowerResource.maxAmount = part.mass * 2.0e+4 * wasteHeatMultiplier;
                    wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * wasteheat_ratio;
                }

                if (state != StartState.Editor)
                    part.emissiveConstant = maxTempatureRadiators > 0 ? 1 - coldBathTemp / maxTempatureRadiators : 0.01;
            }
            catch (Exception e)
            {
                Debug.LogError("FusionEngine OnStart eception: " + e.Message);
            }
            base.OnStart(state);
        }

        private double GetRatio(string akPropName)
        {
            double akRatio = curEngineT.propellants.FirstOrDefault(pr => pr.name == akPropName) != null ? curEngineT.propellants.FirstOrDefault(pr => pr.name == akPropName).ratio : 0;

            return akRatio;
        }
        private void SetRatio(string akPropName, float akRatio)
        {
            if (curEngineT.propellants.FirstOrDefault(pr => pr.name == akPropName) != null)
            {
                curEngineT.propellants.FirstOrDefault(pr => pr.name == akPropName).ratio = akRatio;
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
                    radhazardstr = kerbal_hazard_count.ToString() + " Kerbals.";
                else
                    radhazardstr = kerbal_hazard_count.ToString() + " Kerbal.";

                Fields["radhazardstr"].guiActive = true;
            }
            else
            {
                Fields["radhazardstr"].guiActive = false;
                radhazard = false;
                radhazardstr = "None.";
            }
            if (selectableIsp) FCUpdate();
            else
            {
                Fields["localIsp"].guiActive = selectableIsp;
                Fields["localIsp"].guiActiveEditor = selectableIsp;
                SelectedIsp = MinIsp;
            }
         //   Fields["selectedFuelConfiguration"].guiName = FuelConfigName;
            base.OnUpdate();
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

        public void UpdateISP()
        {
                FloatCurve newIsp = new FloatCurve();
                Altitude = vessel.atmDensity;
                float OrigISP = BaseFloatCurve.Evaluate((float)Altitude);

                FCUpdate();
                newIsp.Add((float)Altitude, SelectedIsp);
                curEngineT.atmosphereCurve = newIsp;
                MinIsp = OrigISP;
        }


        public override void OnFixedUpdate()
        {
          //  base.OnFixedUpdate();
            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
            MinIsp = BaseFloatCurve.Evaluate((float)Altitude);

            // part.ona

            if (curEngineT == null || !curEngineT.isEnabled) return;

            throttle = curEngineT.currentThrottle > MinThrottleRatio ? curEngineT.currentThrottle : 0;

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown("Inertial Fusion cannot operate in atmosphere!");

                if (radhazard && rad_safety_features)
                    ShutDown("Engines throttled down as they presently pose a radiation hazard");

                if (SelectedIsp <= 10)
                    ShutDown("Engine Stall");
            }

            KillKerbalsWithRadiation(throttle);

            if (throttle > 0 )
            {
                // Calculate Fusion Ratio
                enginePowerRequirement = CurrentPowerRequirement;
                var requestedPowerPerSecond = enginePowerRequirement;

                var availablePower = getAvailableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
                var resourceBarRatio = getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES);
                var effectivePowerThrotling = resourceBarRatio > FNResourceManager.ONE_THIRD ? 1 : resourceBarRatio * 3;

                var requestedPower = Math.Min(requestedPowerPerSecond, availablePower * effectivePowerThrotling);

                double recievedPowerPerSecond = CheatOptions.InfiniteElectricity
                    ? requestedPowerPerSecond
                    : consumeFNResourcePerSecond(requestedPower, FNResourceManager.FNRESOURCE_MEGAJOULES);

                var plasma_ratio = recievedPowerPerSecond / requestedPowerPerSecond;
                fusionRatio = plasma_ratio;

                laserWasteheat = recievedPowerPerSecond * (1 - LaserEfficiency);

                // Lasers produce Wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                    supplyFNResourcePerSecond(laserWasteheat, FNResourceManager.FNRESOURCE_WASTEHEAT);

                // The Aborbed wasteheat from Fusion
                var rateMultplier = MinIsp / SelectedIsp;
                var neutronbsorbionBonus = 1 - NeutronAbsorptionFractionAtMinIsp * (1 - ((SelectedIsp - MinIsp) / (MaxIsp - MinIsp)));
                absorbedWasteheat = FusionWasteHeat * wasteHeatMultiplier * fusionRatio * throttle * neutronbsorbionBonus;
                supplyFNResourcePerSecond(absorbedWasteheat, FNResourceManager.FNRESOURCE_WASTEHEAT);

                // change ratio propellants Hydrogen/Fusion
                SetRatio(InterstellarResourcesConfiguration.Instance.LqdDeuterium, (float)standard_deuterium_rate / rateMultplier);
                SetRatio(InterstellarResourcesConfiguration.Instance.LqdTritium, (float)standard_tritium_rate / rateMultplier);

                // Update ISP
                var currentIsp = SelectedIsp;
                UpdateISP();

                maximumThrust = MaximumThrust;

                // Update FuelFlow
                var maxFuelFlow = fusionRatio * maximumThrust / currentIsp / PluginHelper.GravityConstant;

                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = (float)maximumThrust;

                if (!curEngineT.getFlameoutState && plasma_ratio < 0.75 && recievedPowerPerSecond > 0)
                    curEngineT.status = "Insufficient Electricity";
            }
            else
            {
                enginePowerRequirement = 0;
                absorbedWasteheat = 0;
                laserWasteheat = 0;
                fusionRatio = 0;
                var currentIsp = SelectedIsp;

                maximumThrust = MaximumThrust;

                UpdateISP();
                curEngineT.maxThrust = (float)maximumThrust;
                var rateMultplier = MinIsp / SelectedIsp;

                var maxFuelFlow = maximumThrust / currentIsp / PluginHelper.GravityConstant;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                SetRatio(InterstellarResourcesConfiguration.Instance.LqdDeuterium, (float)standard_deuterium_rate / rateMultplier);
                SetRatio(InterstellarResourcesConfiguration.Instance.LqdTritium, (float)standard_tritium_rate / rateMultplier);
            }

            coldBathTemp = FNRadiator.getAverageRadiatorTemperatureForVessel(vessel);
            maxTempatureRadiators = FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
            radiatorPerformance = Math.Max(1 - (coldBathTemp / maxTempatureRadiators), 0.000001);
            partEmissiveConstant = part.emissiveConstant;
            base.OnFixedUpdate();
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
                double distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                double inv_sq_dist = distance / killDivider;
                double inv_sq_mult = 1.0 / inv_sq_dist / inv_sq_dist;
                foreach (ProtoCrewMember crew_member in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1.0 - death_prob * inv_sq_mult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        crew_to_remove.Add(crew_member);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
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
            return part.partInfo.title;
        }

        public override int getPowerPriority()
        {
            return 4;
        }    
    }
}
