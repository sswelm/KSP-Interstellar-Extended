using FNPlugin.Constants;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class ComputerCore : ModuleModableScienceGenerator, ITelescopeController, IUpgradeableModule
    {
        // Persistant
        [KSPField(isPersistant = true, guiActive = true, guiName = "Name")]
        public string nameStr = "";
        [KSPField(guiActive = true, guiName = "Data Collection Rate")]
        public string scienceRate;
        [KSPField(isPersistant = true, guiName = "AI Online", guiActive = true, guiActiveEditor = true), UI_Toggle(disabledText = "Off", enabledText = "On", scene = UI_Scene.All)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true, guiName = "Powered", guiActive = true, guiActiveEditor = false)]
        public bool IsPowered = false;
        [KSPField(isPersistant = true, guiActiveEditor = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public double electrical_power_ratio;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true, guiName = "Data stored", guiActive = true, guiActiveEditor = false)]
        public double science_to_add;
        [KSPField(isPersistant = true)]
        public bool coreInit = false;
        //[KSPField]
        //public double alternatorPower = 0.001;
        [KSPField]
        public string upgradeTechReq = null;
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public string originalName = "";
        [KSPField]
        public float upgradeCost = 100;
        [KSPField]
        public float megajouleRate = 1;
        [KSPField]
        public float upgradedMegajouleRate = 10;
        [KSPField]
        public double powerReqMult = 1;
        [KSPField]
        public double activeAIControlDistance = 9.460525284e20;    // diameter of milkyway
        [KSPField]
        public double inactiveAIControlDistance = 100000;

        //Gui
        [KSPField]
        const double baseScienceRate = 0.3;
        [KSPField(guiActive = true, guiName = "Type")]
        public string computercoreType;
        [KSPField(guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;        

        // Privates
        double science_rate_f;
        double effectivePowerRequirement;

        ConfigNode _experiment_node;
        BaseField _nameStrField;
        BaseField _isEnabledField;
        BaseField _isPoweredField;
        BaseField _upgradeCostStrField;
        BaseField _scienceRateField;
        BaseEvent _retrofitCoreEvent;
        ModuleDataTransmitter _moduleDataTransmitter;
        ModuleCommand moduleCommand;

        //Properties
        public String UpgradeTechnology { get { return upgradeTechReq; } }
        public bool CanProvideTelescopeControl {  get { return isupgraded && IsEnabled && IsPowered; }  }

        // Events
        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitCore()
        {
            if (ResearchAndDevelopment.Instance == null) { return; }
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        // Public Overrides
        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { ResourceManager.FNRESOURCE_THERMALPOWER, ResourceManager.FNRESOURCE_CHARGED_PARTICLES, ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT, };
            this.resources_to_supply = resources_to_supply;

            _isEnabledField = Fields["IsEnabled"];
            _isPoweredField = Fields["IsPowered"];
            _upgradeCostStrField = Fields["upgradeCostStr"];
            _retrofitCoreEvent = Events["RetrofitCore"];
            _nameStrField = Fields["nameStr"];
            _scienceRateField = Fields["scienceRate"];

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

            if (isupgraded || !PluginHelper.TechnologyIsInUse)
                upgradePartModule();
            else
                computercoreType = originalName;

            if (IsEnabled)
            {
                double time_diff = Planetarium.GetUniversalTime() - last_active_time;
                double altitude_multiplier = vessel.altitude / vessel.mainBody.Radius;
                altitude_multiplier = Math.Max(altitude_multiplier, 1);

                var scienceMultiplier = PluginHelper.getScienceMultiplier(vessel);

                double science_to_increment = baseScienceRate * time_diff / GameConstants.KEBRIN_DAY_SECONDS * electrical_power_ratio * scienceMultiplier / (Math.Sqrt(altitude_multiplier));
                science_to_increment = (double.IsNaN(science_to_increment) || double.IsInfinity(science_to_increment)) ? 0 : science_to_increment;
                science_to_add += science_to_increment;
            } 

            effectivePowerRequirement = (isupgraded ? upgradedMegajouleRate : megajouleRate) * powerReqMult;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (_moduleDataTransmitter != null)
                _moduleDataTransmitter.antennaPower = IsEnabled && IsPowered ? activeAIControlDistance : inactiveAIControlDistance;

            if (ResearchAndDevelopment.Instance != null)
                _retrofitCoreEvent.active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost;
            else
                _retrofitCoreEvent.active = false;

            var isUpgradedOrNoActiveScience = isupgraded || !PluginHelper.TechnologyIsInUse;

            _isEnabledField.guiActive = isUpgradedOrNoActiveScience;
            _upgradeCostStrField.guiActive = !isupgraded;
            _nameStrField.guiActive = isUpgradedOrNoActiveScience;
            _scienceRateField.guiActive = isUpgradedOrNoActiveScience;
            _isPoweredField.guiActive = isUpgradedOrNoActiveScience;

            double scienceratetmp =  science_rate_f * GameConstants.KEBRIN_DAY_SECONDS * PluginHelper.getScienceMultiplier(vessel);
            scienceRate = scienceratetmp.ToString("0.000") + "/ Day";

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            //

            if (isupgraded && IsEnabled)
            {
                var power_returned = CheatOptions.InfiniteElectricity
                    ? effectivePowerRequirement
                    : consumeFNResourcePerSecond(effectivePowerRequirement, ResourceManager.FNRESOURCE_MEGAJOULES);

                electrical_power_ratio = power_returned / effectivePowerRequirement;
                IsPowered = electrical_power_ratio > 0.99;

                if (IsPowered)
                {
                    double altitude_multiplier = Math.Max(vessel.altitude / vessel.mainBody.Radius, 1);

                    var scienceMultiplier = PluginHelper.getScienceMultiplier(vessel);

                    science_rate_f = baseScienceRate * scienceMultiplier / GameConstants.KEBRIN_DAY_SECONDS * power_returned / effectivePowerRequirement / Math.Sqrt(altitude_multiplier);

                    if (ResearchAndDevelopment.Instance != null && !double.IsInfinity(science_rate_f) && !double.IsNaN(science_rate_f))
                        science_to_add += science_rate_f * TimeWarp.fixedDeltaTime;
                }
                else
                {
                    // return any unused power
                    part.RequestResource(ResourceManager.FNRESOURCE_MEGAJOULES, -power_returned * TimeWarp.fixedDeltaTime);
                }
            }
            else
            {
                IsPowered = false;
                science_rate_f = 0;
                electrical_power_ratio = 0;
                science_to_add = 0;
            }

            last_active_time = Planetarium.GetUniversalTime();
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            //supplyFNResourcePerSecondWithMax(alternatorPower, alternatorPower, ResourceManager.FNRESOURCE_MEGAJOULES);

            //part.temperature = part.temperature + (TimeWarp.fixedDeltaTime * 1000 * alternatorPower / (part.thermalMass * 0.8));
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

        public override int getPowerPriority()
        {
            return 2;
        }

        public override int getSupplyPriority()
        {
            return 1;
        }

        public override string GetInfo()
        {
            string desc = "Power Requirements: " + megajouleRate.ToString("0.0") + " MW\n";
            return desc + "Upgraded Power Requirements: " + upgradedMegajouleRate.ToString("0.0") + " MW\n";

        }

        // IUpgradeableModule
        public void upgradePartModule()
        {
            computercoreType = upgradedName;
            if (nameStr == "")
            {
                ConfigNode[] namelist = GameDatabase.Instance.GetConfigNodes("AI_CORE_NAME");
                System.Random rands = new System.Random();
                ConfigNode myName = namelist[rands.Next(0, namelist.Length)];
                nameStr = myName.GetValue("name");
            }

            isupgraded = true;
            canDeploy = true;

            _experiment_node = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION").FirstOrDefault(nd => nd.GetValue("id") == experimentID);
        }

        // Privates
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
                Debug.Log("[KSPI]: Exception Generation Experiment Result: " + ex.Message + ": " + ex.StackTrace);
                return " has detected a glitch in the universe and recommends checking your installation of KSPInterstellar.";
            }
        }
    }
}

