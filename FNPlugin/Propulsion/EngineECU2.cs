using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FNPlugin
{
    enum GenerationType { Mk1 = 0, Mk2 = 1, Mk3 = 2, Mk4 = 3, Mk5 = 4, Mk6 = 5, Mk7 = 6, Mk8 = 7, Mk9 = 8 }

    abstract class EngineECU2 : ResourceSuppliableModule
    {
        [KSPField(guiActive = true, guiActiveEditor = true,  guiName = "#LOC_KSPIE_EngineECU2_MaxThrust", guiUnits = " kN", guiFormat = "F3")]//Max Thrust
        public double maximumThrust;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_FusionECU2_MaximumFuelFlow", guiFormat = "F3")]//Maximum FuelFlow
        public double maxFuelFlow;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_EngineECU2_FuelConfig")]//Fuel Config
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedFuel = 0;

        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool Launched = false;
        [KSPField(isPersistant = true)]
        public bool hideEmpty = false;
        [KSPField(isPersistant = true)]
        public int selectedTank = 0;
        [KSPField(isPersistant = true)]
        public string selectedTankName = "";

        // None Persistant VAB
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_EngineECU2_upgradetech1")]//upgrade tech 1
        public string upgradeTechReq1;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_EngineECU2_upgradetech2")]//upgrade tech 2
        public string upgradeTechReq2;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_EngineECU2_upgradetech3")]//upgrade tech 3
        public string upgradeTechReq3;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_EngineECU2_upgradetech4")]//upgrade tech 4
        public string upgradeTechReq4;

        // Gui
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_EngineECU2_ThrustPower", guiUnits = " GW", guiFormat = "F3")]//Thrust Power
        public double thrustPower;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_EngineECU2_FusionRatio", guiFormat = "F3")]//Fusion Ratio
        public double fusionRatio;

        [KSPField]
        public string intakeResource = "IntakeAtm";
        [KSPField]
        public double thrustmultiplier = 1;
        [KSPField]
        public float maxThrust = 150;
        [KSPField]
        public float maxThrustUpgraded1 = 300;
        [KSPField]
        public float maxThrustUpgraded2 = 500;
        [KSPField]
        public float maxThrustUpgraded3 = 800;
        [KSPField]
        public float maxThrustUpgraded4 = 1200;

        [KSPField]
        public double efficiency = 1;
        [KSPField]
        public double efficiencyUpgraded1 = 1;
        [KSPField]
        public double efficiencyUpgraded2 = 1;
        [KSPField]
        public double efficiencyUpgraded3 = 1;
        [KSPField]
        public double efficiencyUpgraded4 = 1;

        // Use for SETI Mode
        [KSPField]
        public float maxTemp = 2500;
        [KSPField]
        public float upgradeCost = 100;
        [KSPField]
        public double rateMultplier = 1;

        public ModuleEngines curEngineT;
        public ModuleEnginesWarp curEngineWarp;

        public bool hasMultipleConfigurations = false;

        protected IList<FuelConfiguration> _activeConfigurations;
        protected FuelConfiguration _currentActiveConfiguration;
        protected List<FuelConfiguration> _fuelConfigurationWithEffect;

        private UI_ChooseOption chooseOptionEditor;
        private UI_ChooseOption chooseOptionFlight;
 
        public GenerationType EngineGenerationType { get; private set; }

        public double MaxThrust {  get { return maxThrust * ThrustMult(); } }
        public double MaxThrustUpgraded1 { get { return maxThrustUpgraded1 * ThrustMult(); } }
        public double MaxThrustUpgraded2 { get { return maxThrustUpgraded2 * ThrustMult(); } }
        public double MaxThrustUpgraded3 { get { return maxThrustUpgraded3 * ThrustMult(); } }
        public double MaxThrustUpgraded4 { get { return maxThrustUpgraded4 * ThrustMult(); } }

        protected void DetermineTechLevel()
        {
            var numberOfUpgradeTechs = 0;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq1))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq2))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq3))
                numberOfUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq4))
                numberOfUpgradeTechs++;

            EngineGenerationType = (GenerationType)numberOfUpgradeTechs;
        }

        [KSPEvent(active = true, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "#LOC_KSPIE_EngineECU2_HideUnusableConfigurations")]//Hide Unusable Configurations
        public void HideFuels()
        {
            hideEmpty = true;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            Debug.Log("[KSPI]: HideFuels calls InitializeFuelSelector");
            InitializeFuelSelector();
            Debug.Log("[KSPI]: HideFuels calls UpdateFuel");
            UpdateFuel();
        }

        [KSPEvent(active = false, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "#LOC_KSPIE_EngineECU2_ShowAllConfigurations")]//Show All Configurations
        public void ShowFuels()
        {
            FuelConfiguration curConfig = CurrentActiveConfiguration;
            hideEmpty = false;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            selectedFuel = ActiveConfigurations.IndexOf(curConfig);
            Debug.Log("[KSPI]: ShowFuels calls InitializeFuelSelector");
            InitializeFuelSelector();
            Debug.Log("[KSPI]: ShowFuels calls UpdateFuel");
            UpdateFuel();
        }

        public void InitializeGUI()
        {
            Debug.Log("[KSPI]: InitializeGUI InitializeFuelSelector");
            InitializeFuelSelector();
            Debug.Log("[KSPI]: InitializeGUI InitializeHideFuels");
            InitializeHideFuels();
        }

        [KSPAction("Next Propellant")]
        public void TogglePropellantAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: Next Propellant called") ;
            var chooseField = Fields["selectedFuel"];

            var names = _activeConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            int newValue = selectedFuel + 1;
            if (newValue >= names.Count())
                newValue = 0;

            chooseField.SetValue(newValue, this);

            UpdateFlightGUI(chooseField, selectedFuel);

            UpdatePartActionWindow();
        }

        [KSPAction("Previous Propellant")]
        public void PreviousPropellant(KSPActionParam param)
        {
            Debug.Log("[KSPI]: Previous Propellant called");
            var chooseField = Fields["selectedFuel"];

            var names = _activeConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            int newValue = selectedFuel - 1;
            if (newValue < 0)
                newValue = names.Count() - 1;

            chooseField.SetValue(newValue, this);

            UpdateFlightGUI(chooseField, selectedFuel);

            UpdatePartActionWindow();
        }

        private void InitializeFuelSelector()
        {
            Debug.Log("[KSPI]: InitializeFuelSelector Setup Fuels Configurations for " + part.partInfo.title);

            var chooseField = Fields["selectedFuel"];

            chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            Debug.Log("[KSPI]: InitializeFuelSelector call ActiveConfigurations hideEmpty = " + hideEmpty);
            _activeConfigurations = ActiveConfigurations;

            if (_activeConfigurations.Count <= 1)
            {
                chooseField.guiActive = false;
                chooseField.guiActiveEditor = false;
                selectedFuel = 0;
                selectedTankName = "";
            }
            else
            {
                chooseField.guiActive = true;
                chooseField.guiActiveEditor = true;

                Debug.Log("[KSPI]: InitializeFuelSelector Looking for config # " + selectedTankName);
                _currentActiveConfiguration = _activeConfigurations.FirstOrDefault(m => m.ConfigName == selectedTankName);

                if (_currentActiveConfiguration != null)
                {
                    selectedFuel = _activeConfigurations.IndexOf(_currentActiveConfiguration);
                    Debug.Log("[KSPI]: InitializeFuelSelector Found config # " + selectedTankName + " with index " + selectedFuel);
                }
                else if (_activeConfigurations.Count > 0 )
                {
                    selectedFuel = 0;
                    Debug.Log("[KSPI]: InitializeFuelSelector Selecting fuel index # " + selectedFuel);
                    _currentActiveConfiguration = _activeConfigurations[selectedFuel];
                    selectedTankName = _currentActiveConfiguration.fuelConfigurationName;
                }
            }

            var names = _activeConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            if (chooseOptionEditor != null)
                chooseOptionEditor.options = names;

            if (chooseOptionFlight != null)
                chooseOptionFlight.options = names;

            // connect on change event
            if (chooseField.guiActive && chooseOptionFlight != null)
                chooseOptionFlight.onFieldChanged = UpdateFlightGUI;
            if (chooseField.guiActiveEditor && chooseOptionEditor != null)
                chooseOptionEditor.onFieldChanged = UpdateEditorGUI;
            _currentActiveConfiguration = _activeConfigurations[selectedFuel];
        }

        public void FixedUpdate()
        {
            if (_fuelConfigurationWithEffect != null)
                _fuelConfigurationWithEffect.ForEach(prop => part.Effect(prop.effectname, 0, -1));
            if (_currentActiveConfiguration != null && !string.IsNullOrEmpty(_currentActiveConfiguration.effectname))
                part.Effect(_currentActiveConfiguration.effectname, (float)(curEngineT.currentThrottle * fusionRatio), -1);
        }

        private void InitializeHideFuels()
        {
            BaseEvent[] eventList = { Events["HideFuels"], Events["ShowFuels"] };
            foreach (BaseEvent akEvent in eventList)
            {
                akEvent.guiActive = FuelConfigurations.Count > 1;
            }
        }

        public FuelConfiguration CurrentActiveConfiguration
        {
            get 
            {
                return _currentActiveConfiguration ?? (_currentActiveConfiguration = ActiveConfigurations[selectedFuel]);
            }
        }

        private List<FuelConfiguration> _fuelConfigurations;

        public List<FuelConfiguration> FuelConfigurations
        {
            get {
                return _fuelConfigurations ??
                       (_fuelConfigurations =
                           part.FindModulesImplementing<FuelConfiguration>()
                               .Where(c => c.requiredTechLevel <= (int) EngineGenerationType)
                               .ToList());
            }
        }

        private double ThrustMult()
        {
            return FuelConfigurations.Count > 0 ? CurrentActiveConfiguration.thrustMult : 1;
        }

        private void UpdateEditorGUI(BaseField field, object oldFieldValueObj)
        {
            UnityEngine.Debug.Log("[KSPI]: UpdateEditorGUI called");

            foreach (var counterpart in part.symmetryCounterparts)
            {
                var symmetryEngine = counterpart.FindModulesImplementing<EngineECU2>().FirstOrDefault();
                if (symmetryEngine != null)
                    symmetryEngine.SymmetricUpdateEditorGUI(field, oldFieldValueObj);
            }

            UpdateFromGUI(field, oldFieldValueObj);
            UpdateFuel();

            selectedTank = selectedFuel;
            selectedTankName = FuelConfigurations[selectedFuel].ConfigName;   
        }

        public void SymmetricUpdateEditorGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateFromGUI(field, oldFieldValueObj);
            selectedTank = selectedFuel;
            selectedTankName = FuelConfigurations[selectedFuel].ConfigName;  
        }

        private void UpdateFlightGUI(BaseField field, object oldFieldValueObj)
        {
            foreach (var counterpart in part.symmetryCounterparts)
            {
                var symmetryEngine = counterpart.FindModulesImplementing<EngineECU2>().FirstOrDefault();
                if (symmetryEngine != null)
                    symmetryEngine.SymmetricUpdateFlightGUI(field, oldFieldValueObj);
            }

            UpdateFromGUI(field, oldFieldValueObj);
            UpdateFuel();
        }

        private void SymmetricUpdateFlightGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateFromGUI(field, oldFieldValueObj);
            UpdateFuel();
        }

        public virtual void UpdateFuel(bool isEditor = false)
        {
            Debug.Log("[KSPI]: Update Fuel with " + CurrentActiveConfiguration.fuelConfigurationName);

            var akPropellants = new ConfigNode();

            int I = 0;
            int N = 0;
            while (I < CurrentActiveConfiguration.Fuels.Length)
            {
                if (CurrentActiveConfiguration.Ratios[I] > 0)
                {
                    var currentFuel = CurrentActiveConfiguration.Fuels[I];
                    var propellantConfig = LoadPropellant(currentFuel, CurrentActiveConfiguration.Ratios[I]);
                    akPropellants.AddNode(propellantConfig);
                }
                else
                    N++;
                I++;
            }

            akPropellants.AddValue("maxThrust", 1);
            akPropellants.AddValue("maxFuelFlow", 1);

            if (curEngineT != null)
            {
                curEngineT.Load(akPropellants);

                //bools
                curEngineT.atmChangeFlow = CurrentActiveConfiguration.atmChangeFlow;                
                curEngineT.clampPropReceived = CurrentActiveConfiguration.clampPropReceived;
                curEngineT.useEngineResponseTime = CurrentActiveConfiguration.useEngineResponseTime;

                // floats
                curEngineT.flowMultCap = CurrentActiveConfiguration.flowMultCap;
                curEngineT.engineAccelerationSpeed = CurrentActiveConfiguration.engineAccelerationSpeed;
                curEngineT.engineDecelerationSpeed = CurrentActiveConfiguration.engineDecelerationSpeed;
                curEngineT.ignitionThreshold = CurrentActiveConfiguration.ignitionThreshold;
                curEngineT.exhaustDamageMultiplier = CurrentActiveConfiguration.exhaustDamageMultiplier;
                curEngineT.exhaustDamageDistanceOffset = CurrentActiveConfiguration.exhaustDamageDistanceOffset;

                curEngineT.useVelCurve = CurrentActiveConfiguration.useVelCurve;
                if (curEngineT.useVelCurve)
                {
                    Debug.Log("[KSPI]: UpdateFlightGUI engine load velCurve");
                    curEngineT.velCurve = CurrentActiveConfiguration.velCurve;
                }

                curEngineT.useAtmCurve = CurrentActiveConfiguration.useAtmCurve;
                if (curEngineT.useAtmCurve)
                {
                    Debug.Log("[KSPI]: UpdateFlightGUI engine load velCurve");
                    curEngineT.atmCurve = CurrentActiveConfiguration.atmCurve;
                }

                curEngineT.atmosphereCurve = CurrentActiveConfiguration.atmosphereCurve;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                vessel.ClearStaging();
                vessel.ResumeStaging();
            }

            UpdateEngineWarpFuels();
        }

        private void UpdateEngineWarpFuels()
        {
            if (curEngineWarp == null || CurrentActiveConfiguration == null) return;

            var typeMasks = CurrentActiveConfiguration.TypeMasks;
            var ratios = CurrentActiveConfiguration.Ratios;
            var fuels = CurrentActiveConfiguration.Fuels;

            var ratiosCount = fuels.Count();
            var fuelCount = fuels.Count();
            var typeMasksCount = typeMasks.Count();

            curEngineWarp.propellant1 = fuelCount > 0 ? fuels[0] : null;
            curEngineWarp.ratio1 = ratiosCount > 0 ? (double)(decimal)ratios[0] : 0;
            if (typeMasksCount > 0 && typeMasks[0] == 1)
                curEngineWarp.ratio1 *= rateMultplier;                

            curEngineWarp.propellant2 = fuelCount > 1 ? fuels[1] : null;
            curEngineWarp.ratio2 = ratiosCount > 1 ? (double)(decimal)ratios[1] : 0;
            if (typeMasksCount > 1 && typeMasks[1] == 1)
                curEngineWarp.ratio2 *= rateMultplier;

            curEngineWarp.propellant3 = fuelCount > 2 ? fuels[2] : null;
            curEngineWarp.ratio3 = ratiosCount > 2 ? (double)(decimal)ratios[2] : 0;
            if (typeMasksCount > 2 && typeMasks[2] == 1)
                curEngineWarp.ratio3 *= rateMultplier;

            curEngineWarp.propellant4 = fuelCount > 3 ? fuels[3] : null;
            curEngineWarp.ratio4 = ratiosCount > 3 ? (double)(decimal)ratios[3] : 0;
            if (typeMasksCount > 3 && typeMasks[3] == 1)
                curEngineWarp.ratio4 *= rateMultplier;
        }

        private ConfigNode LoadPropellant(string akName, float akRatio)
        {
            Debug.Log("[KSPI]: LoadPropellant: " + akName + " " + akRatio);

            var propellantNode = new ConfigNode().AddNode("PROPELLANT");
            propellantNode.AddValue("name", akName);
            propellantNode.AddValue("ratio", akRatio);
            propellantNode.AddValue("DrawGauge", true);

            return propellantNode;
        }

        private ConfigNode LoadResource(string akName, float akAmount, float akMax)
        {
            Debug.Log("LoadResource: " + akName + " " + akAmount + " " + akMax);

            var resourceNode = new ConfigNode().AddNode("RESOURCE");
            resourceNode.AddValue("name", akName);
            resourceNode.AddValue("amount", akAmount);
            resourceNode.AddValue("maxAmount", akMax);
            return resourceNode;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            if (!_activeConfigurations.Any())
                return;

            if (selectedFuel < _activeConfigurations.Count)
                _currentActiveConfiguration = _activeConfigurations[selectedFuel];
            else
            {
                selectedFuel = _activeConfigurations.Count - 1;
                _currentActiveConfiguration = _activeConfigurations.Last();
            }

            if (_currentActiveConfiguration != null)
                selectedTankName = _currentActiveConfiguration.ConfigName;
        }

        private void UpdateActiveConfiguration()
        {
            if (_currentActiveConfiguration == null)
                return;

            var previousFuelConfigurationName = _currentActiveConfiguration.fuelConfigurationName;

            _activeConfigurations = ActiveConfigurations;

            if (!_activeConfigurations.Any())
                return;

            chooseOptionFlight.options = _activeConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            var index = chooseOptionFlight.options.IndexOf(previousFuelConfigurationName);

            if (index >= 0)
                selectedFuel = index;

            if (selectedFuel < _activeConfigurations.Count)
                _currentActiveConfiguration = _activeConfigurations[selectedFuel];
            else
            {
                selectedFuel = _activeConfigurations.Count - 1;
                _currentActiveConfiguration = _activeConfigurations.Last();
            }

            if (_currentActiveConfiguration == null)
                return;

            if (previousFuelConfigurationName != _currentActiveConfiguration.fuelConfigurationName)
            {
                Debug.Log("[KSPI]: UpdateActiveConfiguration calls UpdateFuel");
                UpdateFuel();
            }
        }

        public override void OnUpdate()
        {
            UpdateActiveConfiguration();

            if (curEngineT != null)
            {
                thrustPower = curEngineT.finalThrust * curEngineT.realIsp * Constants.GameConstants.STANDARD_GRAVITY / 2e6;
                UpdateEngineWarpFuels();
            }


            base.OnUpdate();
        }

        private void LoadInitialConfiguration()
        {
            // find maxIsp closes to target maxIsp
            _currentActiveConfiguration = FuelConfigurations.FirstOrDefault();
            selectedFuel = 0;

            if (FuelConfigurations.Count > 1)
                hasMultipleConfigurations = true;
        }

        public override void OnStart(StartState state)
        {
            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resources_to_supply;

            try
            {
                Debug.Log("[KSPI]: Start Current State: " + (int)state + " " + state.ToString());
                Debug.Log("[KSPI]: OnStart Already Launched: " + Launched);

                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();
                curEngineWarp = this.part.FindModuleImplementing<ModuleEnginesWarp>();

                if ((state & StartState.PreLaunch) == StartState.PreLaunch)
                    hideEmpty = true;

                InitializeGUI();

                _fuelConfigurationWithEffect = FuelConfigurations.Where(m => !string.IsNullOrEmpty(m.effectname)).ToList();
                _fuelConfigurationWithEffect.ForEach(prop => part.Effect(prop.effectname, 0, -1));

                if (state == StartState.Editor)
                {
                    hideEmpty = false;
                    selectedTank = selectedFuel;
                    selectedTankName = FuelConfigurations[selectedFuel].ConfigName;
                }

                UpdateFuel();
                Events["ShowFuels"].active = hideEmpty;
                Events["HideFuels"].active = !hideEmpty;

                Fields["upgradeTechReq1"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq1);
                Fields["upgradeTechReq2"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq2);
                Fields["upgradeTechReq3"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq3);
                Fields["upgradeTechReq4"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq4);

                Fields["maxThrustUpgraded1"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq1);
                Fields["maxThrustUpgraded2"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq2);
                Fields["maxThrustUpgraded3"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq3);
                Fields["maxThrustUpgraded4"].guiActiveEditor = !String.IsNullOrEmpty(upgradeTechReq4);
            }
            catch (Exception e)
            {
                Debug.LogError("EngineECU2 OnStart eception: " + e.Message);
            }
            
            base.OnStart(state);
        }
      
        private IList<FuelConfiguration> _usefulConfigurations;
        public IList<FuelConfiguration> UsefulConfigurations
        {
            get
            {
                var allFuelConfigurations = FuelConfigurations;

                _usefulConfigurations = GetUsableConfigurations(allFuelConfigurations);
                if (_usefulConfigurations == null)
                {
                    Debug.LogError("[KSPI]: UsefulConfigurations Broke!");
                    return allFuelConfigurations;
                }

                if (_usefulConfigurations.Count == 0)
                {
                    Debug.LogWarning("[KSPI]: No UsefulConfigurations Found, Returning all available instead");
                    _usefulConfigurations = allFuelConfigurations;
                }

                return _usefulConfigurations;
            }
        }

        
        public IList<FuelConfiguration> ActiveConfigurations
        {
            get
            {
                return hideEmpty ? UsefulConfigurations : FuelConfigurations;
            }
        }

        private IList<FuelConfiguration> GetUsableConfigurations(IList<FuelConfiguration> akConfigs)
        {
            IList<FuelConfiguration> nwConfigs = new List<FuelConfiguration>();
            int I = 0;

            while (I < akConfigs.Count)
            {
                var currentConfig = akConfigs[I];

                if ((_currentActiveConfiguration != null && currentConfig.fuelConfigurationName == _currentActiveConfiguration.fuelConfigurationName) 
                    || ConfigurationHasFuel(currentConfig))
                {
                    nwConfigs.Add(currentConfig);
                }
                else 
                    if (I < selectedFuel && I > 0) 
                        selectedFuel--;
                I++;
            }

            return nwConfigs;
        }

        private bool ConfigurationHasFuel(FuelConfiguration akConfig)
        {
            bool result = true;
            int I = 0;
            while (I < akConfig.Fuels.Length)
            {
                if (akConfig.Ratios[I] > 0)
                {
                    double akAmount = 0;
                    double akMaxAmount = 0;

                    var akResource = PartResourceLibrary.Instance.GetDefinition(akConfig.Fuels[I]);

                    if (akResource != null)
                    {
                        part.GetConnectedResourceTotals(akResource.id, out akAmount, out akMaxAmount);

                        if (akAmount == 0)
                        {
                            if (akMaxAmount > 0)
                            {
                                if (akResource.name != intakeResource)
                                {
                                    result = false;
                                    I = akConfig.Fuels.Length;
                                }
                            }
                            else
                            {
                                result = false;
                                I = akConfig.Fuels.Length;
                            }
                        }
                    }
                    else
                    {
                        result = false;
                        I = akConfig.Fuels.Length;
                    }
                }
                I++;
            }
            return result;
        }

        private void UpdatePartActionWindow()
        {
            var window = FindObjectsOfType<UIPartActionWindow>().FirstOrDefault(w => w.part == part);
            if (window != null)
            {
                foreach (UIPartActionWindow actionwindow in FindObjectsOfType<UIPartActionWindow>())
                {
                    if (window.part != part) continue;
                    actionwindow.ClearList();
                    actionwindow.displayDirty = true;
                }
            }
        }
    }

    class FuelConfiguration : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_FuelConfiguration")]//Fuel Configuration
        public string fuelConfigurationName = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_RequiredTechLevel")]//Required Tech Level
        public int requiredTechLevel = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_Fuels")]//Fuels
        public string fuels = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_Ratios")]//Ratios
        public string ratios = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_TypeMasks")]//TypeMasks
        public string typeMasks = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_ThrustMult")]//Thrust Mult
        public float thrustMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_PowerMult")]//Power Mult
        public float powerMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_NeutronRatio")]//Neutron Ratio
        public float neutronRatio = 0.8f;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_WasteheatMult")]//Wasteheat Mult
        public float wasteheatMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_HasIspThrottling")]//Has Isp Throttling
        public bool hasIspThrottling = true;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public FloatCurve atmosphereCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public FloatCurve velCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public FloatCurve atmCurve = new FloatCurve();

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_IgnoreISP")]//Ignore ISP
        public string ignoreForIsp = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_EngineECU2_IgnoreThrust")]//Ignore Thrust
        public string ignoreForThrustCurve = "";

        [KSPField]
        public bool atmChangeFlow = false;
        [KSPField]
        public bool useVelCurve = false;
        [KSPField]
        public bool useAtmCurve = false;
        [KSPField]
        public bool clampPropReceived = false;
        [KSPField]
        public bool useEngineResponseTime = false;
        [KSPField]
        public string effectname = null;
        [KSPField(isPersistant = true)]
        private string akConfigName = "";
        [KSPField]
        public float flowMultCap = float.MaxValue;
        [KSPField]
        public float ignitionThreshold = 0.1f;
        [KSPField]
        public double exhaustDamageMultiplier = 165;
        [KSPField]
        public double exhaustDamageDistanceOffset = 0;
        [KSPField]
        public float engineAccelerationSpeed = 0.2f;
        [KSPField]
        public float engineDecelerationSpeed = 0.1f;

        private int[] akTypeMask = new int[0];
        private string[] akFuels = new string[0];
        private float[] akRatio = new float[0];

        public string ConfigName
        {
            get
            {
                if (akConfigName == "") 
                    akConfigName = fuelConfigurationName;
                return akConfigName;
            }
        }

        public string[] Fuels
        {
            get
            {
                if (akFuels.Length == 0)
                    akFuels = Regex.Replace(fuels, " ", "").Split(',');
                return akFuels;
            }
        }

        public float[] Ratios
        {
            get
            {
                if (akRatio.Length == 0)
                    akRatio = StringToFloatArray(ratios);
                return akRatio;
            }
        }

        public int[] TypeMasks
        {
            get
            {
                if (akTypeMask.Length == 0)
                    akTypeMask = StringToIntArray(typeMasks);
                return akTypeMask;
            }
        }

        private Int32[] StringToIntArray(string akString)
        {
            if (string.IsNullOrEmpty(akString))
            {
                Debug.LogError("[KSPI]: StringToIntArray is called with empty ");
                return new int[0];
            }

            try
            {
                var akInt = new List<int>();
                string[] arString = Regex.Replace(akString, " ", "").Split(',');
                int I = 0;
                while (I < arString.Length)
                {
                    akInt.Add(Convert.ToInt32(arString[I]));
                    I++;
                }
                return akInt.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception durring StringToIntArray: " + akString);
                throw (e);
            }
        }

        private float[] StringToFloatArray(string akString)
        {
            var akFloat = new List<float>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akFloat.Add((float)Convert.ToDouble(arString[I]));
                I++;
            }
            return akFloat.ToArray();
        }

        private bool[] StringToBoolArray(string akString)
        {
            var akBool = new List<bool>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akBool.Add(Convert.ToBoolean(arString[I]));
                I++;
            }
            return akBool.ToArray();
        }
    }
}
