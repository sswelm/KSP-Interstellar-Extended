using FNPlugin.Constants;
using FNPlugin.Power;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    class VistaEngineControllerAdvanced : FusionEngineControllerBase
    {
        const float maxIsp = 27200f;
        const float minIsp = 15500f;
        const float steps = (maxIsp - minIsp) / 100f;

        // Persistant setting
        [KSPField(groupName = VistaEngineController.GROUP, groupDisplayName = VistaEngineController.GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_SelectedIsp"), UI_FloatRange(stepIncrement = steps, maxValue = maxIsp, minValue = minIsp)]//Selected Isp
        public float localIsp = minIsp;

        // settings
        [KSPField(isPersistant = false)]
        public double neutronAbsorptionFractionAtMinIsp = 0.5;
        [KSPField(isPersistant = false)]
        public double maxThrustEfficiencyByIspPower = 2;

        protected override float SelectedIsp { get { return localIsp; } }
        protected override float MaxIsp { get { return maxIsp; } }
        protected override double MaxThrustEfficiencyByIspPower { get { return maxThrustEfficiencyByIspPower; } }
        protected override double NeutronAbsorptionFractionAtMinIsp { get { return neutronAbsorptionFractionAtMinIsp; } }
    }

    abstract class FusionEngineControllerBase : ResourceSuppliableModule, IUpgradeableModule
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
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk4 = 0.05f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk5 = 0.05f;

        // None Persistant
        [KSPField(groupName = VistaEngineController.GROUP, groupDisplayName = VistaEngineController.GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_RadiationHazard")]//Radiation Hazard To
        public string radhazardstr = "";
        [KSPField(groupName = VistaEngineController.GROUP, groupDisplayName = VistaEngineController.GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_Temperature")]//Temperature
        public string temperatureStr = "";

        [KSPField(isPersistant = false)]
        public float powerRequirement = 625;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded = 1250;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded1 = 0;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded2 = 2500;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded3 = 2500;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded4 = 2500;

        [KSPField(isPersistant = false)]
        public float maxThrust = 75;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 300;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded1 = 0;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded2 = 1200;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded3 = 1200;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded4 = 1200;

        [KSPField(isPersistant = false)]
        public float maxAtmosphereDensity = 0.001f;
        [KSPField(isPersistant = false)]
        public float leathalDistance = 2000;
        [KSPField(isPersistant = false)]
        public float killDivider = 50;

        [KSPField(isPersistant = false)]
        public double efficiency = 0.19;
        [KSPField(isPersistant = false)]
        public double efficiencyUpgraded = 0.38;
        [KSPField(isPersistant = false)]
        public double efficiencyUpgraded2 = 0.76;

        [KSPField(isPersistant = false)]
        public float fusionWasteHeat = 625;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded = 2500;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded1 = 0;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded2 = 10000;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded3 = 10000;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded4 = 10000;

        // Use for SETI Mode
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float powerRequirementMultiplier = 1;

        [KSPField(isPersistant = false)]
        public float maxTemp = 2500;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;

        [KSPField(groupName = VistaEngineController.GROUP, groupDisplayName = VistaEngineController.GROUP_TITLE, isPersistant = false, guiActive = false, guiActiveEditor = true, guiName= "#LOC_KSPIE_VistaEngineControllerAdv_upgradetech1")]//upgrade tech 1
        public string upgradeTechReq = "advFusionReactions";
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_upgradetech2")]//upgrade tech 2
        public string upgradeTechReq2 = "exoticReactions";
        [KSPField(groupName = VistaEngineController.GROUP, groupDisplayName = VistaEngineController.GROUP_TITLE, isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_MaxThrust", guiUnits = " kN")]//Max Thrust
        public float maximumThrust;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_CurrentThrotle", guiFormat = "F2")]//Current Throtle
        public float throttle;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_FusionRatio", guiFormat = "F2")]//Fusion Ratio
        public double fusionRatio;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_PowerRequirement", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Power Requirement
        public float enginePowerRequirement;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_LaserWasteheat", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Laser Wasteheat
        public double laserWasteheat;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_AbsorbedWasteheat", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Absorbed Wasteheat
        public double absorbedWasteheat;

        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_RadiatorTemp")]//Radiator Temp
        public double coldBathTemp;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_MaxRadiatorTemp")]//Max Radiator Temp
        public double maxTempatureRadiators;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_PerformanceRadiators")]//Performance Radiators
        public double radiatorPerformance;
        [KSPField(groupName = VistaEngineController.GROUP, isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_Emisiveness")]//Emisiveness
        public double partEmissiveConstant;


        // abstracts
        protected abstract float SelectedIsp { get; }
        protected abstract float MaxIsp { get; }
        protected abstract double MaxThrustEfficiencyByIspPower { get; }
        protected abstract double NeutronAbsorptionFractionAtMinIsp { get; }

        // protected
        protected bool hasrequiredupgrade = false;
        protected bool radhazard = false;
        protected float minISP = 0;
        protected double standard_megajoule_rate = 0;
        protected double standard_deuterium_rate = 0;
        protected double standard_tritium_rate = 0;
        protected ModuleEngines curEngineT;
        protected ResourceBuffers resourceBuffers;

        public GenerationType EngineGenerationType { get; private set; }

        [KSPEvent(groupName = VistaEngineController.GROUP, guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_DeactivateRadSafety", active = true)]//Disable Radiation Safety
        public void DeactivateRadSafety()
        {
            rad_safety_features = false;
        }

        [KSPEvent(groupName = VistaEngineController.GROUP, guiActive = true, guiName = "#LOC_KSPIE_VistaEngineControllerAdv_ActivateRadSafety", active = false)]//Activate Radiation Safety
        public void ActivateRadSafety()
        {
            rad_safety_features = true;
        }

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public void upgradePartModule() {}

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

        public float MaximumThrust { get { return FullTrustMaximum * Mathf.Pow((minISP / SelectedIsp), (float)MaxThrustEfficiencyByIspPower); } }

        public float FusionWasteHeat
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return fusionWasteHeat;
                else if (EngineGenerationType == GenerationType.Mk2)
                {
                    return fusionWasteHeatUpgraded1 > 0 ? fusionWasteHeatUpgraded1 : fusionWasteHeatUpgraded;
                }
                else if (EngineGenerationType == GenerationType.Mk3)
                    return fusionWasteHeatUpgraded2;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return fusionWasteHeatUpgraded3;
                else
                    return fusionWasteHeatUpgraded4;
            }
        }

        public float FullTrustMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return maxThrust;
                else if (EngineGenerationType == GenerationType.Mk2)
                {
                    return maxThrustUpgraded1 > 0 ? maxThrustUpgraded1 : maxThrustUpgraded;
                }
                else if (EngineGenerationType == GenerationType.Mk3)
                    return maxThrustUpgraded2;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return maxThrustUpgraded3;
                else
                    return maxThrustUpgraded4;
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
                {
                    return powerRequirementUpgraded1 > 0 ? powerRequirementUpgraded1 : powerRequirementUpgraded;
                }
                else if (EngineGenerationType == GenerationType.Mk3)
                    return powerRequirementUpgraded2;
                else if (EngineGenerationType == GenerationType.Mk4)
                    return powerRequirementUpgraded3;
                else
                    return powerRequirementUpgraded4;
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
                    Debug.LogWarning("[KSPI]: FusionEngine OnStart Engine not found");
                    return;
                }

                minISP = curEngineT.atmosphereCurve.Evaluate(0);

                standard_deuterium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == ResourcesConfiguration.Instance.LqdDeuterium).ratio;
                standard_tritium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == ResourcesConfiguration.Instance.LqdTritium).ratio;

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
                float distance = (float)Vector3d.Distance (vessel.transform.position, vess.transform.position);
                if (distance < leathalDistance && vess != this.vessel)
                    kerbal_hazard_count += vess.GetCrewCount ();
            }

            if (kerbal_hazard_count > 0)
            {
                radhazard = true;
                if (kerbal_hazard_count > 1)
                    radhazardstr = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_kerbalhazardcount2", kerbal_hazard_count.ToString());// <<1>> Kerbals."
                else
                    radhazardstr = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_kerbalhazardcount1", kerbal_hazard_count.ToString());// <<2>> Kerbal."

                Fields["radhazardstr"].guiActive = true;
            }
            else
            {
                Fields["radhazardstr"].guiActive = false;
                radhazard = false;
                radhazardstr = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_None");//"None."
            }
        }

        private void ShutDown(string reason)
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

        public override void OnFixedUpdate()
        {
            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";

            if (curEngineT == null) return;

            throttle = curEngineT.currentThrottle > MinThrottleRatio ? curEngineT.currentThrottle : 0;

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg1"));//"Inertial Fusion cannot operate in atmosphere!"

                if (radhazard && rad_safety_features)
                    ShutDown(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg2"));//"Engines throttled down as they presently pose a radiation hazard"
            }

            KillKerbalsWithRadiation(throttle);

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.UpdateBuffers();

            if (throttle > 0)
            {
                // Calculate Fusion Ratio
                enginePowerRequirement = CurrentPowerRequirement;

                var recievedPower = CheatOptions.InfiniteElectricity
                    ? enginePowerRequirement
                    : consumeFNResourcePerSecond(enginePowerRequirement, ResourceManager.FNRESOURCE_MEGAJOULES);

                var plasma_ratio = recievedPower / enginePowerRequirement;
				fusionRatio = plasma_ratio >= 1 ? 1 : plasma_ratio > 0.75 ? plasma_ratio * plasma_ratio * plasma_ratio * plasma_ratio * plasma_ratio * plasma_ratio : 0;

                laserWasteheat = recievedPower * (1 - LaserEfficiency);

                // The Aborbed wasteheat from Fusion
                var rateMultplier = minISP / SelectedIsp;
                var neutronbsorbionBonus = 1 - NeutronAbsorptionFractionAtMinIsp * (1 - ((SelectedIsp - minISP) / (MaxIsp - minISP)));
                absorbedWasteheat = FusionWasteHeat * wasteHeatMultiplier * fusionRatio * throttle * neutronbsorbionBonus;

                // Lasers produce Wasteheat
                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    supplyFNResourcePerSecond(laserWasteheat, ResourceManager.FNRESOURCE_WASTEHEAT);
                    supplyFNResourcePerSecond(absorbedWasteheat, ResourceManager.FNRESOURCE_WASTEHEAT);
                }

                // change ratio propellants Hydrogen/Fusion
                curEngineT.propellants.FirstOrDefault(pr => pr.name == ResourcesConfiguration.Instance.LqdDeuterium).ratio = (float)(standard_deuterium_rate / rateMultplier);
                curEngineT.propellants.FirstOrDefault(pr => pr.name == ResourcesConfiguration.Instance.LqdTritium).ratio = (float)(standard_tritium_rate / rateMultplier);

                // Update ISP
                var currentIsp = SelectedIsp;
                var newISP = new FloatCurve();
                newISP.Add(0, currentIsp);
                newISP.Add(1, 0);
                curEngineT.atmosphereCurve = newISP;

                // Update FuelFlow
				var maxFuelFlow = fusionRatio * MaximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = MaximumThrust;

                maximumThrust = MaximumThrust;

                if (!curEngineT.getFlameoutState && plasma_ratio < 0.75 && recievedPower > 0)
                    curEngineT.status = Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_statu");//"Insufficient Electricity"
            }
            else
            {
                enginePowerRequirement = 0;
                absorbedWasteheat = 0;
                laserWasteheat = 0;
                fusionRatio = 0;

                var currentIsp = SelectedIsp;
                var newISP = new FloatCurve();
                newISP.Add(0, currentIsp);
                newISP.Add(1, 0);
                curEngineT.atmosphereCurve = newISP;
                curEngineT.maxThrust = MaximumThrust;
                var rateMultplier = minISP / SelectedIsp;

				var maxFuelFlow = MaximumThrust / currentIsp / GameConstants.STANDARD_GRAVITY;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.propellants.FirstOrDefault(pr => pr.name == ResourcesConfiguration.Instance.LqdDeuterium).ratio = (float)(standard_deuterium_rate / rateMultplier);
                curEngineT.propellants.FirstOrDefault(pr => pr.name == ResourcesConfiguration.Instance.LqdTritium).ratio = (float)(standard_tritium_rate / rateMultplier);
            }

            coldBathTemp = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel);
            maxTempatureRadiators = FNRadiator.GetAverageMaximumRadiatorTemperatureForVessel(vessel);
            radiatorPerformance = Math.Max(1 - (coldBathTemp / maxTempatureRadiators), 0.000001);
            partEmissiveConstant = part.emissiveConstant;
        }

        private void KillKerbalsWithRadiation(float throttle)
        {
            if (!radhazard || throttle <= 0.00 || rad_safety_features) return;

            System.Random rand = new System.Random(new System.DateTime().Millisecond);
            var vessels_to_remove = new List<Vessel>();
            var crew_to_remove = new List<ProtoCrewMember>();
            double death_prob = TimeWarp.fixedDeltaTime;

            foreach (Vessel vess in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                var inv_sq_dist = distance / killDivider;
                var inv_sq_mult = 1.0f / inv_sq_dist / inv_sq_dist;
                foreach (ProtoCrewMember crew_member in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1.0 - death_prob * inv_sq_mult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_VistaEngineControllerAdv_PostMsg3", crew_member.name), 5.0f, ScreenMessageStyle.UPPER_CENTER);// <<1>> was killed by Neutron Radiation!"
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
