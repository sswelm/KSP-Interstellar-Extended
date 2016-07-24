using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Maximise Thrust"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool maximizeThrust = true;

        [KSPField(isPersistant = false, guiActive = true, guiName = "DeltaTime")]
        public float fixedDeltaTime;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Light Speed", guiFormat = "F2" , guiUnits = "m/s")]
        public double speedOfLight;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Vessel Speed", guiFormat = "F8", guiUnits = "c")]
        public double lightSpeedRatio = 0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Relativity", guiFormat = "F8")]
        public double relativity;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Time Dilation", guiFormat = "F8")]
        public double timeDilation;

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

        [KSPField(isPersistant = false, guiActive = false, guiName = "Deuterium Shortage", guiFormat = "F6")]
        public double deuteriumShortage;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Heliume3 Shortage", guiFormat = "F6")]
        public double helium3Shortage;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true,  guiName = "Deuterium")]
        public string deuteriumAmounts;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Helium-3")]
        public string helium3Amounts;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fusion", guiFormat = "F2", guiUnits = "%")]
        public double fusionPercentage = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Effective Thrust", guiFormat = "F2", guiUnits = " kN")]
        public double effectiveThrust = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Fuel Flow", guiFormat = "F8", guiUnits = " U")]
        public double calculatedFuelflow = 0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Helium-3 Usage", guiFormat = "F2", guiUnits = " L/day")]
        public double helium3UsageDay = 0;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Deuterium Usage", guiFormat = "F2", guiUnits = " L/day")]
        public double deuteriumUsageDay = 0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Stored Throtle")]
        public float storedThrotle = 0;

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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineType = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName= "upgrade tech")]
        public string upgradeTechReq = null;

        protected bool hasrequiredupgrade = false;
		protected bool radhazard = false;
        //protected bool hasPersistedThrust;
		protected float engineIsp = 0;
		protected double standard_tritium_rate = 0;
        protected ModuleEngines curEngineT;

        private double propellantAverageDensity;
        private double densityLqdDeuterium;
        private double densityLqdHelium3;
        private double deteuriumFraction;
        private double helium3Fraction;

        private List<PartResource> deteuriumPartResources;
        private List<PartResource> helium3PartResources;

        private bool warpToReal = false;
        

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
            speedOfLight = GameConstants.speedOfLight * PluginHelper.SpeedOfLightMult;
            densityLqdDeuterium = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdDeuterium).density;
            densityLqdHelium3 = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.LqdHelium3).density;
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
                deteuriumPartResources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdDeuterium).ToList();
                helium3PartResources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdHelium3).ToList();
            }
		}

        public void Update()
        {
            var deteuriumMaxAmount = deteuriumPartResources.Sum(m => m.maxAmount);
            var deteuriumAmount = deteuriumPartResources.Sum(m => m.amount);
            deuteriumAmounts = (deteuriumAmount / deteuriumMaxAmount * 100).ToString("0.0000") +"% " +  deteuriumMaxAmount.ToString("0") + " L";

            var helium3MaxAmount = helium3PartResources.Sum(m => m.maxAmount);
            var helium3Amount = helium3PartResources.Sum(m => m.amount);
            helium3Amounts = (helium3Amount / helium3MaxAmount * 100).ToString("0.0000") + "% " + helium3MaxAmount.ToString("0") + " L";
        }

		public override void OnUpdate() 
        {
            if (curEngineT == null) return;

            // When transitioning from timewarp to real update throttle
            if (warpToReal)
            {
                vessel.ctrlState.mainThrottle = storedThrotle;
                warpToReal = false;
            }

            Events["DeactivateRadSafety"].active = rad_safety_features;
            Events["ActivateRadSafety"].active = !rad_safety_features;
            Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;

			if (curEngineT.isOperational && !IsEnabled) 
            {
				IsEnabled = true;
				part.force_activate ();
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
					radhazardstr = kerbal_hazard_count.ToString () + " Kerbals.";
                else 
					radhazardstr = kerbal_hazard_count.ToString () + " Kerbal.";
				
				Fields["radhazardstr"].guiActive = true;
			} 
            else 
            {
				Fields["radhazardstr"].guiActive = false;
				radhazard = false;
				radhazardstr = "None.";
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

        public void FixedUpdate()
        {
            lightSpeedRatio = vessel.obt_speed / speedOfLight;

            relativity = 1 / Math.Sqrt(1 - Math.Pow(lightSpeedRatio, 2));

            timeDilation = 1 / relativity;

            fixedDeltaTime = TimeWarp.fixedDeltaTime;

            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
        }

		public override void OnFixedUpdate()
        {
            if (curEngineT == null) return;

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

            if (throttle > 0 && !this.vessel.packed && lightSpeedRatio <= 0.999999)
            {
                fusionRatio = ProcessPowerAndWaste(throttle);

                // Update ISP
                FloatCurve newAtmosphereCurve = new FloatCurve();
                newAtmosphereCurve.Add(0, (float)engineIsp);
                newAtmosphereCurve.Add(maxAtmosphereDensity, 0);
                
                curEngineT.atmosphereCurve = newAtmosphereCurve;

                // Update FuelFlow
                effectiveThrust = timeDilation * MaximumThrust * fusionRatio;
                calculatedFuelflow = effectiveThrust / engineIsp / PluginHelper.GravityConstant;
                curEngineT.maxFuelFlow = (float)calculatedFuelflow;
                curEngineT.maxThrust = (float)effectiveThrust;

                deuteriumUsageDay = curEngineT.currentThrottle * calculatedFuelflow * 1600 * PluginHelper.SecondsInDay;
                helium3UsageDay = curEngineT.currentThrottle * calculatedFuelflow * 1600 * PluginHelper.SecondsInDay;

                if (!curEngineT.getFlameoutState && fusionRatio < 0.01)
                {
                    curEngineT.status = "Insufficient Electricity";
                }
            }
            else if (this.vessel.packed && curEngineT.enabled && FlightGlobals.ActiveVessel == vessel && throttle > 0 && lightSpeedRatio <= 0.999999)
            {
                warpToReal = true; // Set to true for transition to realtime

                fusionRatio = maximizeThrust ? ProcessPowerAndWaste(1) : ProcessPowerAndWaste(storedThrotle) ;

                effectiveThrust = timeDilation * MaximumThrust * fusionRatio;

                double demandMass;
                CalculateDeltaVV(this.vessel.GetTotalMass(), TimeWarp.fixedDeltaTime, effectiveThrust, engineIsp, propellantAverageDensity, this.part.transform.up, out demandMass);

                var deteuriumRequestAmount = demandMass * deteuriumFraction / densityLqdDeuterium;
                var helium3RequestAmount = demandMass * helium3Fraction / densityLqdHelium3;

                deuteriumUsageDay = deteuriumRequestAmount / TimeWarp.fixedDeltaTime * (double)PluginHelper.SecondsInDay;
                helium3UsageDay = helium3RequestAmount / TimeWarp.fixedDeltaTime * (double)PluginHelper.SecondsInDay;

                // request deteurium
                var recievedDeuterium = part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, deteuriumRequestAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                deuteriumShortage = deteuriumRequestAmount - recievedDeuterium;
                recievedDeuterium += part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdDeuterium, deuteriumShortage, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                deuteriumShortage = deteuriumRequestAmount - recievedDeuterium;

                // request helium3
                var recievedHelium3 = part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, helium3RequestAmount, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                helium3Shortage = helium3RequestAmount - recievedHelium3;
                recievedHelium3 += part.RequestResource(InterstellarResourcesConfiguration.Instance.LqdHelium3, helium3Shortage, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                helium3Shortage = helium3RequestAmount - recievedHelium3;

                recievedRatio = deteuriumRequestAmount > 0 && helium3RequestAmount > 0 
                    ? Math.Min(recievedDeuterium / deteuriumRequestAmount, recievedHelium3 / helium3RequestAmount) * fusionRatio
                    : 0;

                effectiveThrust = timeDilation * MaximumThrust * recievedRatio;

                var deltaVV = CalculateDeltaVV(this.vessel.GetTotalMass(), TimeWarp.fixedDeltaTime, effectiveThrust, engineIsp, propellantAverageDensity, this.part.transform.up, out demandMass);

                if (recievedRatio > 0.01)
                    vessel.orbit.Perturb(deltaVV, Planetarium.GetUniversalTime());
            }
            else
            {
                deuteriumUsageDay = 0;
                helium3UsageDay = 0;
                fusionPercentage = 0;

                FloatCurve newAtmosphereCurve = new FloatCurve();
                newAtmosphereCurve.Add(0, engineIsp);
                newAtmosphereCurve.Add(maxAtmosphereDensity, 0);
                curEngineT.atmosphereCurve = newAtmosphereCurve;

                effectiveThrust = timeDilation * MaximumThrust;

                var maxFuelFlow = effectiveThrust / engineIsp / PluginHelper.GravityConstant;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;
                curEngineT.maxThrust = (float)effectiveThrust;
            }
        }

        private double ProcessPowerAndWaste(float throtle)
        {
            // Calculate Fusion Ratio
            var powerRequirementFixed = PowerRequirement * TimeWarp.fixedDeltaTime;
            var requestedPower = (curEngineT.thrustPercentage / 100) * throtle * powerRequirementFixed;
            var recievedPower = consumeFNResource(requestedPower, FNResourceManager.FNRESOURCE_MEGAJOULES);
            var plasma_ratio = powerRequirementFixed > 0 ? recievedPower / powerRequirementFixed : 0;
            var fusionRatio = plasma_ratio >= 1 ? 1 : plasma_ratio > 0.01 ? plasma_ratio : 0;

            // Lasers produce Wasteheat
            supplyFNResource(recievedPower * (1 - Efficiency), FNResourceManager.FNRESOURCE_WASTEHEAT);

            // The Aborbed wasteheat from Fusion
            supplyFNResource(FusionWasteHeat * wasteHeatMultiplier * fusionRatio * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);

            fusionPercentage = fusionRatio * 100;

            return fusionRatio;
        }

        // Calculate DeltaV vector and update resource demand from mass (demandMass)
        public virtual Vector3d CalculateDeltaVV(double totalMass, double deltaTime, double thrust, double isp, double averageDensity, Vector3d thrustUV, out double demandMass)
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
                float distance = (float)Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                float inv_sq_dist = distance / killDivider;
                float inv_sq_mult = 1.0f / inv_sq_dist / inv_sq_dist;
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
            return engineType;
        }

        public override int getPowerPriority() 
        {
            return 1;
        }
	}
}

