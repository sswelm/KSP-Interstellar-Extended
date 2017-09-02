using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class ComputerCore : ModuleModableScienceGenerator, ITelescopeController, IUpgradeableModule
    {
        [KSPField(isPersistant = false)]
        const double baseScienceRate = 0.3;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string computercoreType;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Name")]
        public string nameStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Data Collection Rate")]
        public string scienceRate;

        [KSPField(isPersistant = false)]
        public string upgradeTechReq = null;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public float megajouleRate;
        [KSPField(isPersistant = false)]
        public float upgradedMegajouleRate;
        [KSPField(isPersistant = false)]
        public double powerReqMult = 1;

        [KSPField(isPersistant = true, guiName = "AI Online", guiActive = true, guiActiveEditor = true), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true, guiName = "Powered", guiActive = true, guiActiveEditor = true)]
        public bool IsPowered = false;
        [KSPField(isPersistant = true, guiActiveEditor = true,  guiActive = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public double electrical_power_ratio;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true)]
        public double science_to_add;
        [KSPField(isPersistant = true)]
        public bool coreInit = false;

        protected double science_rate_f;

        private double effectivePowerRequirement; 

        private ConfigNode _experiment_node;

        private ModuleDataTransmitter _moduleDataTransmitter;

        protected ModuleCommand moduleCommand;
        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public bool CanProvideTelescopeControl
        {
            get { return isupgraded && IsEnabled && IsPowered; }
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitCore()
        {
            if (ResearchAndDevelopment.Instance == null) { return; }
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }
                return;
            }

            this.part.force_activate();

            _moduleDataTransmitter = part.FindModuleImplementing<ModuleDataTransmitter>();
            moduleCommand = part.FindModuleImplementing<ModuleCommand>();

            Fields["IsEnabled"].guiActive = isupgraded;
            Fields["IsEnabled"].guiActiveEditor = isupgraded;

            if ((isupgraded || !PluginHelper.TechnologyIsInUse) && IsEnabled)
            {
                upgradePartModule();

                double now = Planetarium.GetUniversalTime();
                double time_diff = now - last_active_time;
                double altitude_multiplier = vessel.altitude / vessel.mainBody.Radius;
                altitude_multiplier = Math.Max(altitude_multiplier, 1);

                var scienceMultiplier = PluginHelper.getScienceMultiplier(vessel);

                double science_to_increment = baseScienceRate * time_diff / GameConstants.KEBRIN_DAY_SECONDS * electrical_power_ratio * scienceMultiplier / (Math.Sqrt(altitude_multiplier));
                science_to_increment = (double.IsNaN(science_to_increment) || double.IsInfinity(science_to_increment)) ? 0 : science_to_increment;
                science_to_add += science_to_increment;

                //var curReaction = this.part.Modules["ModuleReactionWheel"] as ModuleReactionWheel;
                //curReaction.PitchTorque = 5;
                //curReaction.RollTorque = 5;
                //curReaction.YawTorque = 5;
            } 
            else
                computercoreType = originalName;

            effectivePowerRequirement = (isupgraded ? upgradedMegajouleRate : megajouleRate) * powerReqMult;
        }



        public override void OnUpdate()
        {
            base.OnUpdate();

            if (_moduleDataTransmitter != null)
            {
                _moduleDataTransmitter.antennaPower = IsEnabled && IsPowered ? 5000000000000000 : 50000;
            }

            if (ResearchAndDevelopment.Instance != null)
                Events["RetrofitCore"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost;
            else
                Events["RetrofitCore"].active = false;

            Fields["upgradeCostStr"].guiActive = !isupgraded;
            Fields["nameStr"].guiActive = isupgraded;
            Fields["scienceRate"].guiActive = isupgraded;

            double scienceratetmp =  science_rate_f * GameConstants.KEBRIN_DAY_SECONDS * PluginHelper.getScienceMultiplier(vessel);
            scienceRate = scienceratetmp.ToString("0.000") + "/Day";

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (isupgraded  && IsEnabled)
            {
                double power_returned = CheatOptions.InfiniteElectricity 
                    ? upgradedMegajouleRate
                    : consumeFNResourcePerSecond(effectivePowerRequirement, FNResourceManager.FNRESOURCE_MEGAJOULES);

                electrical_power_ratio = power_returned / effectivePowerRequirement;
                IsPowered = electrical_power_ratio > 0.99;

                if (IsPowered)
                {
                    double altitude_multiplier = vessel.altitude / vessel.mainBody.Radius;
                    altitude_multiplier = Math.Max(altitude_multiplier, 1);

                    var scienceMultiplier = PluginHelper.getScienceMultiplier(vessel);

                    science_rate_f = baseScienceRate * scienceMultiplier / GameConstants.KEBRIN_DAY_SECONDS * power_returned / effectivePowerRequirement / Math.Sqrt(altitude_multiplier);

                    if (ResearchAndDevelopment.Instance != null && !double.IsInfinity(science_rate_f) && !double.IsNaN(science_rate_f))
                        science_to_add += science_rate_f * TimeWarp.fixedDeltaTime;
                }
                else
                {
                    // return any unused power
                    part.RequestResource(FNResourceManager.FNRESOURCE_MEGAJOULES, -power_returned * TimeWarp.fixedDeltaTime);
                }
            }
            else
            {
                IsPowered = false;
                science_rate_f = 0;
                electrical_power_ratio = 0;
                science_to_add = 0;
            }

            //else
            //{
            //    if (moduleCommand != null)
            //    {
            //        var fixedNeededPower = megajouleRate * TimeWarp.fixedDeltaTime;
            //        float power_returned = consumeFNResource(fixedNeededPower, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
            //        var electrical_power_ratio = Math.Round(power_returned / megajouleRate, 1);
            //        moduleCommand.enabled = electrical_power_ratio == 1;
            //    }
            //}

            last_active_time = Planetarium.GetUniversalTime();
        }

        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            if (experiment == null)
				return false;

            if (science_to_add > 0)
            {
				ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ScienceUtil.GetExperimentSituation(vessel), vessel.mainBody, "", "");
				if (subject == null)
					return false;
				subject.subjectValue = PluginHelper.getScienceMultiplier(vessel);
				subject.scienceCap = 167 * subject.subjectValue; 
				subject.dataScale = 1.25f;

				double remaining_base_science = (subject.scienceCap - subject.science) / subject.subjectValue;
				science_to_add = Math.Min(science_to_add, remaining_base_science);

				// transmission of zero data breaks the experiment result dialog box
				data_size = Math.Max(float.Epsilon, science_to_add * subject.dataScale);
				science_data = new ScienceData((float)data_size, 1, 0, subject.id, "Science Lab Data");

				result_title = experiment.experimentTitle;
                result_string = this.nameStr + " " + getRandomExperimentResult();

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

        public void upgradePartModule()
        {
            computercoreType = upgradedName;
            if (nameStr == "")
            {
                ConfigNode[] namelist = ComputerCore.getNames();
                System.Random rands = new System.Random();
                ConfigNode myName = namelist[rands.Next(0, namelist.Length)];
                nameStr = myName.GetValue("name");
            }

            isupgraded = true;
            canDeploy = true;

            _experiment_node = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION").FirstOrDefault(nd => nd.GetValue("id") == experimentID);
        }

        public static ConfigNode[] getNames()
        {
            ConfigNode[] namelist = GameDatabase.Instance.GetConfigNodes("AI_CORE_NAME");
            return namelist;
        }

        public override int getPowerPriority()
        {
            return 2;
        }

        public override string GetInfo()
        {
            string desc = "Power Requirements: " + megajouleRate.ToString("0.0") + " MW\n";
            desc = desc + "Upgraded Power Requirements: " + upgradedMegajouleRate.ToString("0.0") + " MW\n";
            return desc;
        }

        private string getRandomExperimentResult()
        {
            try
            {
                System.Random rnd = new System.Random();
                String[] result_strs = _experiment_node.GetNode("RESULTS").GetValuesStartsWith("default");
                int indx = rnd.Next(result_strs.Length);
                return result_strs[indx];
            } 
            catch (Exception ex)
            {
                Debug.Log("[KSPI] Exception Generation Experiment Result: " + ex.Message + ": " + ex.StackTrace);
                return " has detected a glitch in the universe and recommends checking your installation of KSPInterstellar.";
            }
        }

    }
}

