using FNPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TweakScale;
using UnityEngine;

namespace FNPlugin
{
    enum GenerationType { Mk1 = 0, Mk2 = 1, Mk3 = 2, Mk4 = 3, Mk5 = 4, Mk6 = 5, Mk7 = 6, Mk8 = 7, Mk9 = 8 }

    abstract class EngineECU2 : ResourceSuppliableModule, IRescalable<EngineECU2>
    {
        [KSPField(guiActive = true, guiName = "Max Thrust", guiUnits = " kN", guiFormat = "F4")]
        public double maximumThrust;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fuel Config")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedFuel = 0;

        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool Launched = false;
        [KSPField(isPersistant = true)]
        public double scale = 1;
        [KSPField(isPersistant = true)]
        public bool hideEmpty = false;
        [KSPField(isPersistant = true)]
        public int selectedTank = 0;
        [KSPField(isPersistant = true)]
        public string selectedTankName = "";

        // None Persistant VAB
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 1")]
        public string upgradeTechReq1;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 2")]
        public string upgradeTechReq2;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 3")]
        public string upgradeTechReq3;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 4")]
        public string upgradeTechReq4;

        // Gui
        [KSPField(guiActive = true, guiName = "Thrust Power", guiUnits = " GW", guiFormat = "F3")]
        public double thrustPower;
        
        // Settings
        [KSPField]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField]
        public float minThrottleRatioMk3 = 0.05f;
        [KSPField]
        public float minThrottleRatioMk4 = 0.05f;
        [KSPField]
        public float minThrottleRatioMk5 = 0.05f;

        [KSPField]
        public double thrustmultiplier = 1;
        [KSPField]
        public bool isLoaded = false;
        [KSPField]
        public bool resourceSwitching = true;

        [KSPField(guiActiveEditor = true)]
        public float maxThrust = 150;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustUpgraded1 = 300;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustUpgraded2 = 500;
        [KSPField( guiActiveEditor = true)]
        public float maxThrustUpgraded3 = 800;
        [KSPField( guiActiveEditor = true)]
        public float maxThrustUpgraded4 = 1200;

        [KSPField]
        public double efficiency = 0.19;
        [KSPField]
        public double efficiencyUpgraded1 = 0.25;
        [KSPField]
        public double efficiencyUpgraded2 = 0.44;
        [KSPField]
        public double efficiencyUpgraded3 = 0.65;
        [KSPField]
        public double efficiencyUpgraded4 = 0.76;

        // Use for SETI Mode
        [KSPField]
        public float maxTemp = 2500;
        [KSPField]
        public float upgradeCost = 100;

        [KSPField(guiActive = false)]
        public double rateMultplier = 1;

        [KSPField]
        public float throttle;

        public ModuleEngines curEngineT;
        public ModuleEnginesWarp curEngineWarp;

        public bool hasMultipleConfigurations = false;

        protected IList<FuelConfiguration> _activeConfigurations;
        protected FuelConfiguration _currentActiveConfiguration;
        protected List<FuelConfiguration> _fuelConfigurationWithEffect;

        private UIPartActionWindow tweakableUI;
        private UI_ChooseOption chooseOptionEditor;
        private UI_ChooseOption chooseOptionFlight;
 
        public GenerationType EngineGenerationType { get; private set; }

        public double MaxThrust {  get { return maxThrust * thrustMult(); } }
        public double MaxThrustUpgraded1 { get { return maxThrustUpgraded1 * thrustMult(); } }
        public double MaxThrustUpgraded2 { get { return maxThrustUpgraded2 * thrustMult(); } }
        public double MaxThrustUpgraded3 { get { return maxThrustUpgraded3 * thrustMult(); } }
        public double MaxThrustUpgraded4 { get { return maxThrustUpgraded4 * thrustMult(); } }

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

        [KSPEvent(active = true, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "Hide Unusable Configurations")]
        public void HideFuels()
        {
            hideEmpty = true;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            //UpdateusefulConfigurations();
            InitializeFuelSelector();
            Debug.Log("[KSPI]: HideFuels calls UpdateFuel");
            UpdateFuel();
        }

        [KSPEvent(active = false, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "Show All Configurations")]
        public void ShowFuels()
        {
            FuelConfiguration CurConfig = CurrentActiveConfiguration;
            hideEmpty = false;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            selectedFuel = ActiveConfigurations.IndexOf(CurConfig);
            InitializeFuelSelector();
            Debug.Log("[KSPI]: ShowFuels calls UpdateFuel");
            UpdateFuel();
        }

        public void InitializeGUI()
        {
            InitializeFuelSelector();
            InitializeHideFuels();
        }

        private void InitializeFuelSelector()
        {
            Debug.Log("[KSPI]: Setup Fuels Configurations for " + part.partInfo.title);

            var chooseField = Fields["selectedFuel"];
            chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            _activeConfigurations = ActiveConfigurations;

            if (_activeConfigurations.Count <= 1)
            {
                chooseField.guiActive = false;
                chooseField.guiActiveEditor = false;
                selectedFuel = 0;
            }
            else
            {
                chooseField.guiActive = true;
                chooseField.guiActiveEditor = true;
                if (selectedFuel >= _activeConfigurations.Count) selectedFuel = _activeConfigurations.Count - 1;
                _currentActiveConfiguration = _activeConfigurations[selectedFuel];
            }

            Debug.Log("[KSPI]: Selected Fuel # " + selectedFuel);

            var names = _activeConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            chooseOptionEditor.options = names;
            chooseOptionFlight.options = names;

            // connect on change event
            if (chooseField.guiActive) 
                chooseOptionFlight.onFieldChanged = UpdateFlightGUI;
            if (chooseField.guiActiveEditor) 
                chooseOptionEditor.onFieldChanged = UpdateEditorGUI;
            _currentActiveConfiguration = _activeConfigurations[selectedFuel];
        }

        public void FixedUpdate()
        {
            if (_fuelConfigurationWithEffect != null)
                _fuelConfigurationWithEffect.ForEach(prop => part.Effect(prop.effectname, 0, -1));
            if (_currentActiveConfiguration != null && !string.IsNullOrEmpty(_currentActiveConfiguration.effectname))
                part.Effect(_currentActiveConfiguration.effectname, curEngineT.currentThrottle, -1);
        }

        private void InitializeHideFuels()
        {
            BaseEvent[] EventList = { Events["HideFuels"], Events["ShowFuels"] };
            foreach (BaseEvent akEvent in EventList)
            {
                if (FuelConfigurations.Count <= 1)
                    akEvent.guiActive = false;
                else
                    akEvent.guiActive = true;
            }
        }

        public FuelConfiguration CurrentActiveConfiguration
        {
            get
            {
                if (_currentActiveConfiguration == null) 
                    _currentActiveConfiguration = ActiveConfigurations[selectedFuel];
                return _currentActiveConfiguration;
            }
        }

        private List<FuelConfiguration> fuelConfigurations;

        public List<FuelConfiguration> FuelConfigurations
        {
            get
            {
                if (fuelConfigurations == null)
                    fuelConfigurations = part.FindModulesImplementing<FuelConfiguration>().Where(c => c.requiredTechLevel <= (int)EngineGenerationType).ToList();
                return fuelConfigurations;
            }
        }

        private double thrustMult()
        {
            return FuelConfigurations.Count > 0 ? CurrentActiveConfiguration.thrustMult : 1;
        }

        private void UpdateEditorGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("[KSPI]: Editor Gui Updated");
            UpdateFromGUI(field, oldFieldValueObj);
            selectedTank = selectedFuel;
            selectedTankName = FuelConfigurations[selectedFuel].ConfigName;
            //UpdateResources();           
        }

        private void UpdateFlightGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateFromGUI(field, oldFieldValueObj);
            Debug.Log("[KSPI]: UpdateFlightGUI calls UpdateFuel");
            UpdateFuel();
        }

        public virtual void UpdateFuel(bool isEditor = false)
        {
            Debug.Log("[KSPI]: Update Fuel with " + CurrentActiveConfiguration.fuelConfigurationName);

            ConfigNode akPropellants = new ConfigNode();

            int I = 0;
            int N = 0;
            while (I < CurrentActiveConfiguration.Fuels.Length)
            {
                if (CurrentActiveConfiguration.Ratios[I] > 0)
                {
                    Debug.Log("[KSPI]: Load propellant " + CurrentActiveConfiguration.Fuels[I]);
                    akPropellants.AddNode(LoadPropellant(CurrentActiveConfiguration.Fuels[I], CurrentActiveConfiguration.Ratios[I]));
                }
                else
                    N++;
                I++;
            }
            //if (N + 1 >= akConfig.Fuels.Length) 
            //    Fields["selectedFuel"].guiActive = false;

            akPropellants.AddValue("maxThrust", 1);
            akPropellants.AddValue("maxFuelFlow", 1);

            if (curEngineT != null)
            {
                curEngineT.Load(akPropellants);
                curEngineT.atmosphereCurve = CurrentActiveConfiguration.atmosphereCurve;
            }

            if (!isEditor)
            {
                vessel.ClearStaging();
                vessel.ResumeStaging();
            }

            UpdateEngineWarpFuels();
        }

        private void UpdateEngineWarpFuels()
        {
            if (curEngineWarp != null && CurrentActiveConfiguration != null)
            {
                //Debug.Log("[KSPI]: UpdateEngineWarp with fuels " + string.Join(", ", CurrentActiveConfiguration.Fuels) 
                //    + " with ratios " + string.Join(", ", CurrentActiveConfiguration.Ratios.Select(m => m.ToString("F3")).ToArray()));

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
            //else
            //    Debug.Log("[KSPI]: UpdateEngineWarpFuels skipped");
        }

        //private void UpdateResources()
        //{
        //	if (!resourceSwitching)
        //		return;

        //	Debug.Log("[KSPI]: Update Resources");

        //	ConfigNode akResources = new ConfigNode();
        //	FuelConfiguration akConfig = new FuelConfiguration();

        //	if (selectedTankName == "")
        //		selectedTankName = FuelConfigurations[selectedTank].ConfigName;
        //	else if (FuelConfigurations[selectedTank].ConfigName == selectedTankName)
        //		akConfig = FuelConfigurations[selectedTank];
        //	else
        //	{
        //		selectedTank = FuelConfigurations.IndexOf(FuelConfigurations.FirstOrDefault(g => g.ConfigName == selectedTankName));
        //		akConfig = FuelConfigurations[selectedTank];
        //	}

        //	int I = 0;
        //	int N = 0;

        //	while (I < part.Resources.Count)
        //	{
        //		part.Resources.Remove(part.Resources[I]);
        //		I++;
        //	}

        //	part.Resources.Clear();

        //	I = 0;
        //	N = 0;
        //	while (I < akConfig.Fuels.Length)
        //	{
        //		Debug.Log("[KSPI]: Resource: " + akConfig.Fuels[I] + " has a " + akConfig.MaxAmount[I] + " tank.");
        //		if (akConfig.MaxAmount[I] > 0)
        //		{
        //			Debug.Log("[KSPI]: Loaded Resource: " + akConfig.Fuels[I]);
        //			part.AddResource(LoadResource(akConfig.Fuels[I], akConfig.Amount[I], akConfig.MaxAmount[I]));
        //		}
        //		else N++;
        //		I++;
        //	}

        //	if (N + 1 >= akConfig.Fuels.Length) 
        //		Fields["selectedFuel"].guiActive = false;

        //	Debug.Log("[KSPI]: New Fuels: " + akConfig.Fuels.Length);
        //	if (tweakableUI == null)
        //		tweakableUI = part.FindActionWindow();
        //	if (tweakableUI != null)
        //		tweakableUI.displayDirty = true;

        //	Debug.Log("[KSPI]: Resources Updated");
        //}    

        private ConfigNode LoadPropellant(string akName, float akRatio)
        {
            Debug.Log("[KSPI]: LoadPropellant: " + akName);
            //    Debug.Log("Ratio: "+ akRatio);

            ConfigNode PropellantNode = new ConfigNode().AddNode("PROPELLANT");
            PropellantNode.AddValue("name", akName);
            PropellantNode.AddValue("ratio", akRatio);
            PropellantNode.AddValue("DrawGauge", true);

            return PropellantNode;
        }

        private ConfigNode LoadResource(string akName, float akAmount, float akMax)
        {
           // Debug.Log("Resource: "+akName + " Added");
            ConfigNode ResourceNode = new ConfigNode().AddNode("RESOURCE");
            ResourceNode.AddValue("name", akName);
            ResourceNode.AddValue("amount", akAmount);
            ResourceNode.AddValue("maxAmount", akMax);
            return ResourceNode;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("[KSPI]: UpdateFromGUI is called with " + selectedFuel);

            if (!_activeConfigurations.Any())
            {
                Debug.Log("[KSPI]: UpdateFromGUI no FuelConfigurations found");
                return;
            }

            if (selectedFuel < _activeConfigurations.Count)
            {
                Debug.Log("[KSPI]: UpdateFromGUI " + selectedFuel + " < orderedFuelGenerators.Count");
                _currentActiveConfiguration = _activeConfigurations[selectedFuel];
            }
            else
            {
                Debug.Log("[KSPI]: UpdateFromGUI " + selectedFuel + " >= orderedFuelGenerators.Count");
                selectedFuel = _activeConfigurations.Count - 1;
                _currentActiveConfiguration = _activeConfigurations.Last();
            }

            if (_currentActiveConfiguration == null)
            {
                Debug.Log("[KSPI]: UpdateFromGUI no activeConfiguration found");
                return;
            }
        }

        private void UpdateActiveConfiguration()
        {
            if (_currentActiveConfiguration == null)
                return;

            string previousFuelConfigurationName = _currentActiveConfiguration.fuelConfigurationName;

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
            isLoaded = true;
            // find maxIsp closes to target maxIsp
            _currentActiveConfiguration = FuelConfigurations.FirstOrDefault();
            selectedFuel = 0;

            if (FuelConfigurations.Count > 1)
                hasMultipleConfigurations = true;
        }

        public override void OnStart(StartState state)
        {
            try
            {
                Debug.Log("[KSPI]: Start State: " + state.ToString());
                Debug.Log("[KSPI]: Already Launched: " + Launched);

                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();
                curEngineWarp = this.part.FindModuleImplementing<ModuleEnginesWarp>();

                InitializeGUI();

                _fuelConfigurationWithEffect = FuelConfigurations.Where(m => !string.IsNullOrEmpty(m.effectname)).ToList();
                _fuelConfigurationWithEffect.ForEach(prop => part.Effect(prop.effectname, 0, -1));

                if (state == StartState.Editor)
                {
                    Debug.Log("[KSPI]: Editor");
                    hideEmpty = false;
                    selectedTank = selectedFuel;
                    selectedTankName = FuelConfigurations[selectedFuel].ConfigName;
                    //UpdateResources();
                }
                else
                {
                    hideEmpty = true;
                    if (state == StartState.PreLaunch) // startstate normally == prelaunch,landed
                    {
                        Debug.Log("[KSPI]: PreLaunch");
                        hideEmpty = true;
                        //UpdateResources();
                        //UpdateusefulConfigurations();
                        InitializeFuelSelector();
                    }
                    else
                    {
                        Debug.Log("[KSPI]: No PreLaunch");
                    }
                }

				Debug.Log("[KSPI]: OnStart calls UpdateFuel");
				UpdateFuel();

                Events["ShowFuels"].active = hideEmpty;
                Events["HideFuels"].active = !hideEmpty;
            }
            catch (Exception e)
            {
                Debug.LogError("EngineECU2 OnStart eception: " + e.Message);
            }
            
            base.OnStart(state);
        }

        public virtual void OnRescale(TweakScale.ScalingFactor akFactor)
        {
            scale = (double)(decimal)akFactor.absolute.linear;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
        }
        
        private IList<FuelConfiguration> usefulConfigurations;
        public IList<FuelConfiguration> UsefulConfigurations
        {
            get
            {
                //if (usefulConfigurations == null)
                usefulConfigurations = GetUsableConfigurations(FuelConfigurations);
                if (usefulConfigurations == null)
                {
                    Debug.Log("[KSPI]: UsefulConfigurations Broke!");
                    return FuelConfigurations;
                }

                return usefulConfigurations;
            }
        }

        
        public IList<FuelConfiguration> ActiveConfigurations
        {
            get
            {
                return hideEmpty ? UsefulConfigurations : FuelConfigurations;
            }
        }

        public IList<FuelConfiguration> GetUsableConfigurations(IList<FuelConfiguration> akConfigs)
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
                    //Debug.Log("[KSPI]: Added fuel configuration: " + akConfigs[I].fuelConfigurationName);
                }
                else 
                    if (I < selectedFuel && I > 0) 
                        selectedFuel--;
                I++;
            }

            return nwConfigs;
        }

        public bool ConfigurationHasFuel(FuelConfiguration akConfig)
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
                        //Debug.Log("[KSPI]: Resource: " + akConfig.Fuels[I] + " has " + akAmount);

                        if (akAmount == 0)
                        {
                            if (akMaxAmount > 0)
                            {
                                if (akResource.name != "IntakeAtm")
                                {
                                    //Debug.Log("[KSPI]: Resource: " + akConfig.Fuels[I] + " is empty, but that is ok");
                                    result = false;
                                    I = akConfig.Fuels.Length;
                                }
                            }
                            else
                            {
                                //Debug.Log("[KSPI]: Resource: " + akConfig.Fuels[I] + " is missing, it will be removed from the list");
                                result = false;
                                I = akConfig.Fuels.Length;
                            }
                        }
                    }
                    else
                    {
                        //Debug.Log("[KSPI]: Resource: " + akConfig.Fuels[I] + " is not defined");
                        result = false;
                        I = akConfig.Fuels.Length;
                    }
                }
                I++;
            }
            return result;
        }
    }

    class FuelConfiguration : PartModule, IRescalable<FuelConfiguration>
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuel Configuration")]
        public string fuelConfigurationName = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Required Tech Level")]
        public int requiredTechLevel = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuels")]
        public string fuels = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ratios")]
        public string ratios = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Amount")]
        public string amount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "TypeMasks")]
        public string typeMasks = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Amount")]
        public string maxAmount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thrust Mult")]
        public float thrustMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Power Mult")]
        public float powerMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Neutron Ratio")]
        public float neutronRatio = 0.8f;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Wasteheat Mult")]
        public float wasteheatMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Has Isp Throttling")]
        public bool hasIspThrottling = true;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmopheric Curve")]
        public FloatCurve atmosphereCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore ISP")]
        public string ignoreForIsp = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore Thrust")]
        public string ignoreForThrustCurve = "";

        [KSPField(isPersistant = false)]
        public string effectname = null;
        [KSPField(isPersistant = true)]
        public double Scale = 1;
        [KSPField(isPersistant = true)]
        private string akConfigName = "";

        [KSPField(isPersistant = true)]
        private string strAmount="";
        [KSPField(isPersistant = true)]
        private string strMaxAmount="";

        private float[] akAmount = new float[0];
        private float[] akMaxAmount = new float[0];
        private int[] akTypeMask = new int[0];

        private string[] akFuels = new string[0];
        private bool[] akIgnoreIsp = new bool[0];
        private bool[] akIgnoreThrust = new bool[0];
        private float[] akRatio = new float[0];

        public string ConfigName
        {
            get
            {
                if (akConfigName == "") akConfigName = fuelConfigurationName;
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

        public float[] Amount
        {
            get
            {
                if (akAmount.Length == 0) 
                    akAmount = StringToFloatArray(StrAmount);
                return VolumeTweaked(akAmount);
            }
        }

        public float[] MaxAmount
        {
            get
            {
                if (akMaxAmount.Length == 0) 
                    akMaxAmount = StringToFloatArray(StrMaxAmount);
                return VolumeTweaked(akMaxAmount);
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

        private string StrMaxAmount
        {
            get
            {
                if (strMaxAmount == "") strMaxAmount = maxAmount;
                return strMaxAmount;
            }
        }

        private string StrAmount
        {
            get
            {
                if (strAmount == "") 
                    strAmount = amount;
                return strAmount;
            }
        }

        public bool[] IgnoreForIsp
        {
            get
            {
                if (ignoreForIsp == "") akIgnoreIsp = falseBoolArray();
                else if (akIgnoreIsp.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreIsp;
            }
        }

        public bool[] IgnoreForThrust
        {
            get
            {
                if (ignoreForThrustCurve == "") akIgnoreThrust = falseBoolArray();
                else if (akIgnoreThrust.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreThrust;
            }
        }

        private float[] VolumeTweaked(float[] akFloat)
        {
            float[] akTweaked = new float[akFloat.Length];

            if (Scale != 1 && Scale > 0)
            {
                int I = 0;
                while (I < akFloat.Length)
                {
                    var scaleToPowerThree = Scale * Scale * Scale;
                    akTweaked[I] = (float)(akFloat[I] * scaleToPowerThree);
                    I++;
                }
                akFloat = akTweaked.ToArray();
            }
            return akFloat;
        }

        private bool[] falseBoolArray()
        {
            List<bool> akBoolList = new List<bool>();
            int I = 0;
            while (I < akFuels.Length)
            {
                akBoolList.Add(false);
                I++;
            }
            return akBoolList.ToArray();
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

                List<int> akInt = new List<int>();
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
            List<float> akFloat = new List<float>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akFloat.Add((float)Convert.ToDouble(arString[I]));
                I++;
            }
            return akFloat.ToArray();
        }

        private string FloatArrayToString(float[] akFloat)
        {
            string akstring = "";
            int I = 1;
            akstring += akFloat[0];
            while (I < akFloat.Length)
            {
                akstring = akstring + ", " + akFloat[I];
                I++;
            }
            maxAmount = akstring;
            return akstring;
        }

        private bool[] StringToBoolArray(string akString)
        {
            List<bool> akBool = new List<bool>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akBool.Add(Convert.ToBoolean(arString[I]));
                I++;
            }
            return akBool.ToArray();
        }
        private void Refresh()
        {
            akConfigName = "";
            strMaxAmount = maxAmount;
            akMaxAmount = new float[0];
           int i = 0;
            while (i < Amount.Length)
            {
                if (Amount[i] > MaxAmount[i]) Amount[i] = MaxAmount[i];
                i++;
            }
        }

        private void SaveAmount(ShipConstruct Ship)
        {
            try
            {
                int i = 0;
                if (part.Resources.Count == Amount.Length)
                    while (i < Amount.Length)
                    {
                       // Debug.Log("Saving " + part.Resources[i].resourceName + " Amount " + part.Resources[i].amount);
                        akAmount[i] = (float)part.Resources[i].amount;
                        i++;
                    }

                strAmount = FloatArrayToString(Amount);
            }
            catch (Exception e)
            {
                Debug.LogError("Save Amount Error: " + e);
            }
        }       

        public virtual void OnRescale(ScalingFactor factor)
        {
            Scale = (double)(decimal)factor.absolute.linear;
        }

        public override void OnStart(StartState state)
        {
            if (fuelConfigurationName != akConfigName || StrMaxAmount != maxAmount) 
                Refresh();

            GameEvents.onEditorShipModified.Add(SaveAmount);
            base.OnStart(state);
        }

    }
}
