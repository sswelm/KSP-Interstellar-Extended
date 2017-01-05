using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FNPlugin.Refinery; 

namespace FNPlugin 
{
    class ScienceModule : ModuleModableScienceGenerator, ITelescopeController, IUpgradeableModule
    {
        // persistant true
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public int active_mode = 0;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float electrical_power_ratio;
        [KSPField(isPersistant = true)]
        public double science_to_add;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusTitle;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Data Scan Rate")]
        public string scienceRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Scanned Data")]
        public string collectedScience;
        [KSPField(isPersistant = false, guiActive = true, guiName = "R")]
        public string reprocessingRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "A")]
        public string antimatterRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "E")]
        public string electrolysisRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "C")]
        public string centrifugeRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Efficiency")]
        public string antimatterProductionEfficiency;
        [KSPField(isPersistant = false)]
        public string beginResearchName = "Begin Scanning";

        // persistant false
        [KSPField(isPersistant = false)]
        public string animName1;
        [KSPField(isPersistant = false)]
        public string animName2;
        [KSPField(isPersistant = false)]
        public string upgradeTechReq = null;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 20;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = false)]
        public float powerReqMult = 1f;

        protected float megajoules_supplied = 0;
        protected String[] modes = { "Scanning", "Reprocessing", "Producing Antimatter", "Electrolysing", "Centrifuging" };
        protected float science_rate_f;
        protected float reprocessing_rate_f = 0;
        protected float crew_capacity_ratio;
        protected float antimatter_rate_f = 0;
        protected float electrolysis_rate_f = 0;
        protected float deut_rate_f = 0;
        protected bool play_down = true;
        protected Animation anim;
        protected Animation anim2;
        protected NuclearFuelReprocessor reprocessor;
        protected AntimatterFactory anti_factory;
        protected bool hasrequiredupgrade = false;

        

        public bool CanProvideTelescopeControl
        {
            get { return part.protoModuleCrew.Count > 0; }
        }

        [KSPEvent(guiActive = true, guiName = "Begin Scanning", active = true)]
        public void BeginResearch() 
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 0;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Reprocess Nuclear Fuel", active = true)]
        public void ReprocessFuel() 
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 1;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Antimatter Factory", active = true)]
        public void ActivateFactory() 
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 2;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Electrolysis", active = true)]
        public void ActivateElectrolysis() 
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 3;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Centrifuge", active = true)]
        public void ActivateCentrifuge() 
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 4;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Current Activity", active = false)]
        public void StopActivity() 
        {
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public void upgradePartModule()
        {
            canDeploy = true;
            isupgraded = true;
        }

        public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }
                return;
            }

            if (isupgraded)
                upgradePartModule();
            else
            {
                if (this.HasTechsRequiredToUpgrade())
                    hasrequiredupgrade = true;
            }

            // update gui names
            Events["BeginResearch"].guiName = beginResearchName;

            reprocessor = new NuclearFuelReprocessor(part);
            anti_factory = new AntimatterFactory(part);

            part.force_activate();

            anim = part.FindModelAnimators(animName1).FirstOrDefault();
            anim2 = part.FindModelAnimators(animName2).FirstOrDefault();
            if (anim != null && anim2 != null) 
            {

                anim[animName1].layer = 1;
                anim2[animName2].layer = 1;
                if (IsEnabled) 
                {
                    //anim [animName1].normalizedTime = 1f;
                    //anim2 [animName2].normalizedTime = 1f;
                    //anim [animName1].speed = -1f;
                    //anim2 [animName2].speed = -1f;
                    anim.Blend(animName1, 1, 0);
                    anim2.Blend(animName2, 1, 0);
                } 
                else 
                {
                    //anim [animName1].normalizedTime = 0f;
                    //anim2 [animName2].normalizedTime = 0f;
                    //anim [animName1].speed = 1f;
                    //anim2 [animName2].speed = 1f;
                    //anim.Blend (animName1, 0, 0);global_rate_multipliers
                    //anim2.Blend (animName2, 0, 0);
                    play_down = false;
                }
                //anim.Play ();
                //anim2.Play ();
            }

            if (IsEnabled && last_active_time != 0) 
            {
                double global_rate_multipliers = 1;
                crew_capacity_ratio = (part.protoModuleCrew.Count) / (part.CrewCapacity);
                global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;

                if (active_mode == 0) // Science persistence
                { 
                    var time_diff = Planetarium.GetUniversalTime() - last_active_time;
                    var altitude_multiplier = Math.Max((vessel.altitude / (vessel.mainBody.Radius)), 1);
                    var kerbalResearchSkillFactor = part.protoModuleCrew.Sum(proto_crew_member => GetKerbalScienceFactor(proto_crew_member) / 2f);

                    double science_to_increment = kerbalResearchSkillFactor * GameConstants.baseScienceRate * time_diff
                        / PluginHelper.SecondsInDay * electrical_power_ratio * global_rate_multipliers * PluginHelper.getScienceMultiplier(vessel)   //PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed) 
                        / (Math.Sqrt(altitude_multiplier));

                    science_to_increment = (double.IsNaN(science_to_increment) || double.IsInfinity(science_to_increment)) ? 0 : science_to_increment;
                    science_to_add += science_to_increment;

                }
                else if (active_mode == 2) // Antimatter persistence
                { 
                    double time_diff = Planetarium.GetUniversalTime() - last_active_time;

                    //List<PartResource> antimatter_resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Antimatter).ToList();
                    //float currentAntimatter_missing = (float) antimatter_resources.Sum(ar => ar.maxAmount-ar.amount);
                    var antimaterResourceDefinition =  PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Antimatter);
                    double amount;
                    double maxAmount;
                    part.GetConnectedResourceTotals(antimaterResourceDefinition.id, out amount, out maxAmount);
                    var currentAntimatter_missing = maxAmount - amount;

                    var total_electrical_power_provided = (electrical_power_ratio * (PluginHelper.BaseAMFPowerConsumption + PluginHelper.BasePowerConsumption) * 1E6);
                    var antimatter_mass = total_electrical_power_provided / GameConstants.warpspeed / GameConstants.warpspeed * 1E6 / 20000.0;
                    var antimatter_peristence_to_add = -Math.Min(currentAntimatter_missing, antimatter_mass * time_diff);
                    part.RequestResource(InterstellarResourcesConfiguration.Instance.Antimatter, antimatter_peristence_to_add);
                }
            }
        }

        public override void OnUpdate() 
        {
            base.OnUpdate();
            Events["BeginResearch"].active = isupgraded && !IsEnabled;
            Events["ReprocessFuel"].active = isupgraded && !IsEnabled;
            Events["ActivateFactory"].active = isupgraded && !IsEnabled;
            Events["ActivateElectrolysis"].active = false;
            Events["ActivateCentrifuge"].active = isupgraded && !IsEnabled && vessel.Splashed;
            Events["StopActivity"].active = isupgraded && IsEnabled;
            Fields["statusTitle"].guiActive = isupgraded;

            // only show retrofit btoon if we can actualy upgrade
            Events["RetrofitEngine"].active = ResearchAndDevelopment.Instance == null ? false : !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;

            if (IsEnabled) 
            {
                //anim [animName1].normalizedTime = 1f;
                statusTitle = modes[active_mode] + "...";
                Fields["scienceRate"].guiActive = false;

                Fields["collectedScience"].guiActive = false;
                Fields["reprocessingRate"].guiActive = false;
                Fields["antimatterRate"].guiActive = false;
                Fields["electrolysisRate"].guiActive = false;
                Fields["centrifugeRate"].guiActive = false;
                Fields["antimatterProductionEfficiency"].guiActive = false;
                Fields["powerStr"].guiActive = true;

                double currentpowertmp = electrical_power_ratio * PluginHelper.BasePowerConsumption * powerReqMult;
                powerStr = currentpowertmp.ToString("0.0000") + "MW / " + (powerReqMult * PluginHelper.BasePowerConsumption).ToString("0.0000") + "MW";
                if (active_mode == 0) // Research
                { 
                    Fields["scienceRate"].guiActive = true;
                    Fields["collectedScience"].guiActive = true;
                    double scienceratetmp = science_rate_f * PluginHelper.SecondsInDay * PluginHelper.getScienceMultiplier(vessel);
                    scienceRate = scienceratetmp.ToString("0.0000") + "/Day";
                    collectedScience = science_to_add.ToString("0.000000");
                }
                else if (active_mode == 1) // Fuel Reprocessing
                { 
                    Fields["reprocessingRate"].guiActive = true;
                    reprocessingRate = reprocessing_rate_f.ToString("0.0") + " Hours Remaining";
                }
                else if (active_mode == 2) // Antimatter
                {
                    currentpowertmp = electrical_power_ratio * PluginHelper.BaseAMFPowerConsumption * powerReqMult;
                    Fields["antimatterRate"].guiActive = true;
                    Fields["antimatterProductionEfficiency"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginHelper.BaseAMFPowerConsumption).ToString("0.00") + "MW";
                    antimatterProductionEfficiency = (anti_factory.getAntimatterProductionEfficiency() * 100).ToString("0.0000") + "%";
                    double antimatter_rate_per_day = antimatter_rate_f * PluginHelper.SecondsInDay;
                    
                    if (antimatter_rate_per_day > 0.1) 
                        antimatterRate = (antimatter_rate_per_day).ToString("0.0000") + " mg/day";
                    else 
                    {
                        if (antimatter_rate_per_day > 0.1e-3) 
                            antimatterRate = (antimatter_rate_per_day*1e3).ToString("0.0000") + " ug/day";
                        else 
                            antimatterRate = (antimatter_rate_per_day*1e6).ToString("0.0000") + " ng/day";
                    }
                }
                else if (active_mode == 3) // Electrolysis
                {
                    currentpowertmp = electrical_power_ratio * PluginHelper.BaseELCPowerConsumption * powerReqMult;
                    Fields["electrolysisRate"].guiActive = true;
                    double electrolysisratetmp = -electrolysis_rate_f * PluginHelper.SecondsInDay;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + "mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginHelper.BaseELCPowerConsumption).ToString("0.00") + "MW";
                }
                else if (active_mode == 4) // Centrifuge
                {
                    currentpowertmp = electrical_power_ratio * PluginHelper.BaseCentriPowerConsumption * powerReqMult;
                    Fields["centrifugeRate"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginHelper.BaseCentriPowerConsumption).ToString("0.00") + "MW";
                    float deut_per_hour = deut_rate_f * 3600;
                    centrifugeRate = deut_per_hour.ToString("0.00") + " Kg Deuterium/Hour";
                }
                else
                { 
                    // nothing 
                }
            } 
            else 
            {
                if (play_down) 
                {
                    anim[animName1].speed = -1f;
                    anim[animName1].normalizedTime = 1f;
                    anim.Blend(animName1, 2f);
                    anim2[animName2].speed = -1f;
                    anim2[animName2].normalizedTime = 1f;
                    anim2.Blend(animName2, 2f);
                    play_down = false;
                }

                //anim [animName1].normalizedTime = 0f;
                Fields["scienceRate"].guiActive = false;
                Fields["collectedScience"].guiActive = false;
                Fields["reprocessingRate"].guiActive = false;
                Fields["antimatterRate"].guiActive = false;
                Fields["powerStr"].guiActive = false;
                Fields["centrifugeRate"].guiActive = false;
                Fields["electrolysisRate"].guiActive = false;
                Fields["antimatterProductionEfficiency"].guiActive = false;

                if (crew_capacity_ratio > 0) 
                    statusTitle = "Idle";
                else 
                    statusTitle = "Not enough crew";
            }
        }

        private float GetKerbalScienceFactor(ProtoCrewMember kerbal)
        {
            float kerbalFactor;
            
            // initialise with profession
            if (kerbal.experienceTrait.Title == "Scientist")
                kerbalFactor = 1.5f;
            else if (kerbal.experienceTrait.Title == "Engineer")
                kerbalFactor = 1f;
            else
                kerbalFactor = 0.5f;

            // moidy by experience level
            kerbalFactor *= (kerbal.experienceLevel + 10f) / 10f;

            // final modify result by kerbal stupidity (+/- 10%)
            return kerbalFactor * (1.1f - (kerbal.stupidity / 5f));
        }

        public override void OnFixedUpdate()
        {
            float global_rate_multipliers = 1;
            crew_capacity_ratio = ((float)part.protoModuleCrew.Count) / ((float)part.CrewCapacity);
            global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;

            if (!IsEnabled) return;

            if (active_mode == 0)  // Research
            {
                var powerRequest = powerReqMult * PluginHelper.BasePowerConsumption * TimeWarp.fixedDeltaTime;

                double electrical_power_provided = CheatOptions.InfiniteElectricity
                    ? powerRequest
                    : consumeFNResource(powerRequest, FNResourceManager.FNRESOURCE_MEGAJOULES);

                electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BasePowerConsumption / powerReqMult);
                global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;

                float kerbalScienceSkillFactor = part.protoModuleCrew.Sum(proto_crew_member => GetKerbalScienceFactor(proto_crew_member) / 2f);
                float altitude_multiplier = Math.Max((float)(vessel.altitude / (vessel.mainBody.Radius)), 1);

                science_rate_f = (float)(kerbalScienceSkillFactor * GameConstants.baseScienceRate * PluginHelper.getScienceMultiplier(vessel) //PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed)
                    / PluginHelper.SecondsInDay * global_rate_multipliers
                    / (Mathf.Sqrt(altitude_multiplier)));

                if (ResearchAndDevelopment.Instance != null && !double.IsNaN(science_rate_f) && !double.IsInfinity(science_rate_f))
                {
                    //ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + science_rate_f * TimeWarp.fixedDeltaTime;
                    science_to_add += science_rate_f * TimeWarp.fixedDeltaTime;
                }
            }
            else if (active_mode == 1) // Fuel Reprocessing
            {
                var powerRequest = powerReqMult * PluginHelper.BasePowerConsumption * TimeWarp.fixedDeltaTime;

                double electrical_power_provided = CheatOptions.InfiniteElectricity 
                    ? powerRequest
                    : consumeFNResource(powerRequest, FNResourceManager.FNRESOURCE_MEGAJOULES);
                
                electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BasePowerConsumption / powerReqMult);
                global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;
                reprocessor.UpdateFrame(global_rate_multipliers, true);
                
                if (reprocessor.getActinidesRemovedPerHour() > 0)
                    reprocessing_rate_f = (float)(reprocessor.getRemainingAmountToReprocess() / reprocessor.getActinidesRemovedPerHour());
                else
                    IsEnabled = false;
            }
            else if (active_mode == 2) //Antimatter
            { 
                var powerRequest = powerReqMult * PluginHelper.BaseAMFPowerConsumption * TimeWarp.fixedDeltaTime;

                double electrical_power_provided = CheatOptions.InfiniteElectricity 
                    ? powerRequest 
                    : consumeFNResource(powerRequest, FNResourceManager.FNRESOURCE_MEGAJOULES);

                electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BaseAMFPowerConsumption / powerReqMult);
                global_rate_multipliers = crew_capacity_ratio * electrical_power_ratio;
                anti_factory.produceAntimatterFrame(global_rate_multipliers);
                antimatter_rate_f = (float)anti_factory.getAntimatterProductionRate();
            }
            else if (active_mode == 3)
            {
                IsEnabled = false;
            }
            else if (active_mode == 4) // Centrifuge
            { 
                if (vessel.Splashed)
                {
                    var powerRequest = powerReqMult * PluginHelper.BaseCentriPowerConsumption * TimeWarp.fixedDeltaTime;

                    double electrical_power_provided = CheatOptions.InfiniteElectricity 
                        ? powerRequest 
                        : consumeFNResource(powerRequest, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BaseCentriPowerConsumption / powerReqMult);
                    global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;
                    float deut_produced = (float)(global_rate_multipliers * GameConstants.deuterium_timescale * GameConstants.deuterium_abudance * 1000.0f);
                    deut_rate_f = -ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.LqdDeuterium, -deut_produced * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                }
                else
                {
                    ScreenMessages.PostScreenMessage("You must be splashed down to perform this activity.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    IsEnabled = false;
                }
            }

            if (electrical_power_ratio <= 0)
            {
                deut_rate_f = 0;
                electrolysis_rate_f = 0;
                science_rate_f = 0;
                antimatter_rate_f = 0;
                reprocessing_rate_f = 0;
            }

            last_active_time = (float)Planetarium.GetUniversalTime();
        }

        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            if (experiment == null) return false;

            if (science_to_add > 0)
            {
				ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ScienceUtil.GetExperimentSituation(vessel), vessel.mainBody, "");
				if (subject == null)
					return false;
				subject.subjectValue = PluginHelper.getScienceMultiplier(vessel);
				subject.scienceCap = 167 * subject.subjectValue; ///PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, false);
				subject.dataScale = 1.25f;

				float remaining_base_science = (subject.scienceCap - subject.science) / subject.subjectValue;
				science_to_add = Math.Min(science_to_add, remaining_base_science);

				// transmission of zero data breaks the experiment result dialog box
				data_size = Math.Max(float.Epsilon, science_to_add * subject.dataScale);
				science_data = new ScienceData((float)data_size, 1, 0, subject.id, "Science Lab Data");

				result_title = experiment.experimentTitle;
                result_string = "Science experiments were conducted in the vicinity of " + vessel.mainBody.name + ".";

				recovery_value = science_to_add;
                transmit_value = recovery_value;
                xmit_scalar = 1;
                
                ref_value = subject.scienceCap;

                return true;
			}
            return false;
        }

        protected override void cleanUpScienceData()
        {
            science_to_add = 0;
        }

        public override string getResourceManagerDisplayName() 
        {
            if (IsEnabled) 
                return "Science Lab (" + modes[active_mode] + ")";
            
            return "Science Lab";
        }


    }
}
