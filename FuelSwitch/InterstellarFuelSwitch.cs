using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using TweakScale;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    // habitat state
    public enum State
    {
        disabled,        // hab is disabled
        enabled,         // hab is enabled
        pressurizing,    // hab is pressurizing (between uninhabited and habitats)
        depressurizing,  // hab is depressurizing (between enabled and disabled)
    }

    public class IFSresource
    {
        public string name;
        public double amount;
        public double maxAmount;
        public double density;

        public IFSresource(string name)
        {
            this.name = name;
            var resourceDefinition = PartResourceLibrary.Instance.GetDefinition(name);

            if (resourceDefinition == null) return;

            density = resourceDefinition.density;
        }

        public double FullMass { get { return maxAmount * density; } }
    }

    public class IFSmodularTank
    {
        public bool hasTech;
        public string GuiName = String.Empty;
        public string SwitchName = String.Empty;
        public string Composition = String.Empty;
        public string techReq;
        public double tankCost;
        public double tankMass;
        public double resourceMassDivider;
        public double resourceMassDividerAddition;
        public double habitatVolume;
        public double habitatSurface;
        public int crewCapacity;

        public List<IFSresource> Resources = new List<IFSresource>();

        public double FullResourceMass { get { return Resources.Sum(m => m.FullMass); } }
    }

    [KSPModule("#LOC_IFS_FuelSwitch_moduleName")]
    public class InterstellarFuelSwitch : PartModule, IRescalable<InterstellarFuelSwitch>, IPartCostModifier, IPartMassModifier
    {
        // Persistants
        [KSPField(isPersistant = true)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedTankSetup = -1;
        [KSPField(isPersistant = true)]
        public string configuredAmounts = "";
        [KSPField(isPersistant = true)]
        public string configuredFlowStates = "";
        [KSPField(isPersistant = true)]
        public string selectedTankSetupTxt;
        [KSPField(isPersistant = true)]
        public bool configLoaded = false;
        [KSPField(isPersistant = true)]
        public string initialTankSetup;
        [KSPField(isPersistant = true)]
        public double storedFactorMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double storedSurfaceMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double storedVolumeMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double baseMassMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double initialMassMultiplier = 1;
        [KSPField(isPersistant = true)]
        public float windowPositionX = 1200;
        [KSPField(isPersistant = true)]
        public float windowPositionY = 150;

        // Config properties
        [KSPField]
        public float windowWidth = 200;
        [KSPField]
        public string moduleID = "0";
        [KSPField]
        public string tankId = string.Empty;
        [KSPField]
        public string resourceGui = string.Empty;
        [KSPField]
        public string tankSwitchNames = string.Empty;
        [KSPField]
        public string crewCapacity = string.Empty;
        [KSPField]
        public string habitatVolume = string.Empty;
        [KSPField]
        public string habitatSurface = string.Empty;
        [KSPField]
        public string bannedResourceNames = string.Empty;
        [KSPField]
        public string switcherDescription = "#LOC_IFS_FuelSwitch_switcherDescription";  // Tank
        [KSPField]
        public string resourceNames = "ElectricCharge;LiquidFuel,Oxidizer;MonoPropellant";
        [KSPField]
        public string resourceAmounts = string.Empty;
        [KSPField]
        public string resourceRatios = string.Empty;
        [KSPField]
        public string initialResourceAmounts = string.Empty;
        [KSPField]
        public bool ignoreInitialCost = false;
        [KSPField]
        public bool adaptiveTankSelection = false;
        [KSPField]
        public float basePartMass = 0;
        [KSPField]
        public double baseResourceMassDivider = 0;
        [KSPField]
        public string tankResourceMassDivider = string.Empty;
        [KSPField]
        public string tankResourceMassDividerAddition = string.Empty;
        [KSPField]
        public bool overrideMassWithTankDividers = false;
        [KSPField]
        public bool orderBySwitchName = false;
        [KSPField]
        public string tankMass = "";
        [KSPField]
        public string tankTechReq = "";
        [KSPField]
        public string tankCost = "";
        [KSPField]
        public string boilOffTemp = "";
        [KSPField]
        public bool displayTankCost = false;
        [KSPField]
        public bool displayWetDryMass = true;
        [KSPField]
        public bool hasSwitchChooseOption = true;
        [KSPField]
        public bool hasGUI = true;
        [KSPField]
        public bool availableInFlight = false;
        [KSPField]
        public bool availableInEditor = true;
        [KSPField]
        public bool returnDryMass = false;
        [KSPField]
        public string inEditorSwitchingTechReq;
        [KSPField]
        public string inFlightSwitchingTechReq;
        [KSPField]
        public bool useTextureSwitchModule = false;
        [KSPField]
        public bool showTankName = true;
        [KSPField]
        public bool showInfo = true;    // if false, does not feed info to the part list pop up info menu
        [KSPField]
        public string moduleInfoTemplate;      
        [KSPField]
        public string moduleInfoParams;         
        [KSPField]
        public string resourcesFormat = "0.0000";
        [KSPField]
        public bool canSwitchWithFullTanks = false;
        [KSPField]
        public bool allowedToSwitch;
        [KSPField]
        public bool updateModuleCost = true;
        [KSPField]
        public bool controlCrewCapacity = false;

        // Gui
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_tankGuiName")] // Tank name
        public string tankGuiName = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_maxWetDryMass")] // Dry/Wet Mass
        public string maxWetDryMass = "";
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_massRatioStr")] // Mass Ratio
        public string massRatioStr = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_crewCapacityStr")] // Crew Capacity
        public string crewCapacityStr = "";
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_switchWindow"), UI_Toggle(disabledText = "Hidden", enabledText = "Shown", affectSymCounterparts = UI_Scene.None)] // Mass Ratio
        public bool render_window = false;

        // Debug
        [KSPField]
        public bool debugMode = false;
        [KSPField]
        public float moduleCost;
        [KSPField]
        public double dryMass;
        [KSPField]
        public double initialMass;
        [KSPField]
        public double moduleMassDelta;
        [KSPField]
        public float defaultMass;
        [KSPField]
        public string defaultTank = "";
        [KSPField]
        public string resourceAmountStr0 = "";
        [KSPField]
        public string resourceAmountStr1 = "";
        [KSPField]
        public string resourceAmountStr2 = "";
        [KSPField]
        public string resourceAmountStr3 = "";

        [KSPField]
        public double volumeExponent = 3;
        [KSPField]
        public double massExponent = 3;
        [KSPField]
        public double baseMassExponent = 0;
        [KSPField]
        public double tweakscaleMassExponent = 3;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_totalMass", guiUnits = " t", guiFormat = "F4")]			// Total mass
        public double totalMass;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_maxResourceCost", guiFormat = "F3", guiUnits = " Ѵ")]  // Max Wet cost
        public double maxResourceCost = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_dryCost", guiFormat = "F3", guiUnits = " Ѵ")]           // Dry Tank cost
        public double dryCost = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_resourceCost", guiFormat = "F3", guiUnits = " Ѵ")]      // Resource cost
        public double resourceCost = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_totalCost", guiFormat = "F3", guiUnits = " Ѵ")]         // Total Tank cost
        public double totalCost = 0;

        List<IFSmodularTank> _modularTankList = new List<IFSmodularTank>();
        InterstellarTextureSwitch2 textureSwitch;
        IFSmodularTank selectedTank;
        HashSet<string> activeResourceList = new HashSet<string>();
        Rect windowPosition;

        bool _initialized;
        bool closeAterSwitch;

        int _numberOfAvailableTanks;
        int _windowID;

        double _partResourceMaxAmountFraction0;
        double _partResourceMaxAmountFraction1;
        double _partResourceMaxAmountFraction2;
        double _partResourceMaxAmountFraction3;

        PartResource _partResource0;
        PartResource _partResource1;
        PartResource _partResource2;
        PartResource _partResource3;

        PartResourceDefinition _partRresourceDefinition0;
        PartResourceDefinition _partRresourceDefinition1;
        PartResourceDefinition _partRresourceDefinition2;
        PartResourceDefinition _partRresourceDefinition3;

        BaseField _field0;
        BaseField _field1;
        BaseField _field2;
        BaseField _field3;

        BaseField _massRatioStrField;
        BaseField _maxWetDryMassField;
        BaseField _crewCapacityField;
        BaseField _tankGuiNameField;
        BaseField _chooseField;

        BaseEvent _nextTankSetupEvent;
        BaseEvent _previousTankSetupEvent;

        PartModule habitatModule;
        BaseField habitatVolumeField;
        BaseField habitatSurfaceField;
        BaseField habitatStateField;
        BaseField habitatToggleField;

        MethodInfo onStartMethod;

        IHaveFuelTankSetup _fuelTankSetupControl;

        static HashSet<string> _researchedTechs;

        [KSPAction("Show Switch Tank Window")]
        public void ToggleSwitchWindowwAction(KSPActionParam param)
        {
            Debug.Log("[IFS] - Toggled Switch Window");
            render_window = !render_window;
        }

        public virtual void OnRescale(ScalingFactor factor)
        {
            try
            {
                var factorAbsoluteLinear = (double)(decimal)factor.absolute.linear;
                storedFactorMultiplier = factorAbsoluteLinear;
                storedSurfaceMultiplier = factorAbsoluteLinear * factorAbsoluteLinear;
                storedVolumeMultiplier = Math.Pow(factorAbsoluteLinear, volumeExponent);

                baseMassMultiplier = Math.Pow(factorAbsoluteLinear, baseMassExponent == 0 ? massExponent : baseMassExponent);
                initialMassMultiplier = Math.Pow(factorAbsoluteLinear, tweakscaleMassExponent);

                initialMass = (double)(decimal)part.prefabMass * initialMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: OnRescale Error: " + e.Message);
                throw;
            }
        }

        public int FindMatchingConfig(IHaveFuelTankSetup control = null)
        {
            if (control != null)
                _fuelTankSetupControl = control;

            InitializeData();

            if (selectedTankSetup == -1 && !string.IsNullOrEmpty(defaultTank))
                selectedTankSetupTxt = Localizer.Format(defaultTank);

            var matchingGuiTank = 
                _modularTankList.FirstOrDefault(t => t.GuiName == selectedTankSetupTxt) ??
                _modularTankList.FirstOrDefault(t => t.SwitchName == selectedTankSetupTxt) ?? 
                _modularTankList.FirstOrDefault(t => t.Composition == selectedTankSetupTxt);

            if (matchingGuiTank != null)
                return _modularTankList.IndexOf(matchingGuiTank);

            var numberOfResources = part.Resources.Count(r => activeResourceList.Contains(r.resourceName));

            if (numberOfResources == 0)
                return -1;

            for (var i = 0; i < _modularTankList.Count; i++)
            {
                var modularTank = _modularTankList[i];

                var isSimilar = true;

                // check if number of resources match
                if (modularTank.Resources.Count != part.Resources.Count(r => activeResourceList.Contains(r.resourceName)))
                    isSimilar = false;
                else
                {
                    // check if all tank resources are present
                    foreach (var resource in modularTank.Resources)
                    {
                        if (part.Resources.Contains(resource.name)) continue;

                        isSimilar = false;
                        break;
                    }
                }

                if (isSimilar)
                    return i;
            }

            return -1;
        }

        public override void OnStart(PartModule.StartState state)
        {
            try
            {
                _crewCapacityField = Fields["crewCapacityStr"];
                _crewCapacityField.guiActive = controlCrewCapacity;
                _crewCapacityField.guiActiveEditor = controlCrewCapacity;

                _massRatioStrField = Fields["massRatioStr"];
                _massRatioStrField.guiActive = displayWetDryMass;
                _massRatioStrField.guiActiveEditor = displayWetDryMass;

                _maxWetDryMassField = Fields["maxWetDryMass"];
                _maxWetDryMassField.guiActive = displayWetDryMass;
                _maxWetDryMassField.guiActiveEditor = displayWetDryMass;

                initialMass = (double)(decimal)part.prefabMass * initialMassMultiplier;

                if (initialMass == 0)
                    initialMass = (double)(decimal)part.prefabMass;

                defaultTank = Localizer.Format(defaultTank);

                _windowID = new System.Random(part.GetInstanceID()).Next(int.MaxValue);

                windowPosition = new Rect(windowPositionX, windowPositionY, windowWidth, 10);

                InitializeData();

                InitializeKerbalismHabitat();

                if (adaptiveTankSelection || selectedTankSetup == -1)
                {
                    if (selectedTankSetup == -1)
                        initialTankSetup = String.Join(";", part.Resources.Select(m => m.resourceName).ToArray());

                    var matchingIndex = FindMatchingConfig();
                    if (matchingIndex != -1)
                    {
                        selectedTank = _modularTankList[matchingIndex];
                        selectedTankSetupTxt = selectedTank.GuiName;
                    }
                    else if (state == StartState.Editor)
                    {
                        var desiredTank = 
                            _modularTankList.FirstOrDefault(m => m.GuiName == defaultTank) ??
                            _modularTankList.FirstOrDefault(m => m.SwitchName == defaultTank) ?? 
                            _modularTankList.FirstOrDefault(m => m.Composition == defaultTank);

                        if (desiredTank == null)
                            selectedTank = _modularTankList[0];
                        else
                            selectedTank = desiredTank;

                        selectedTankSetupTxt = selectedTank.GuiName;
                    }
                }

                this.enabled = true;

                AssignResourcesToPart();

                _chooseField = Fields["selectedTankSetup"];

                _chooseField.guiName = Localizer.Format(switcherDescription);
                _chooseField.guiActiveEditor = hasSwitchChooseOption && availableInEditor && _modularTankList.Count > 1;
                _chooseField.guiActive = hasSwitchChooseOption && availableInFlight && _modularTankList.Count > 1;

                var chooseOptionEditor = _chooseField.uiControlEditor as UI_ChooseOption;
                if (chooseOptionEditor != null)
                {
                    chooseOptionEditor.options = _modularTankList.Select(s => s.SwitchName).ToArray();
                    chooseOptionEditor.onFieldChanged = UpdateFromGUI;
                }

                var chooseOptionFlight = _chooseField.uiControlFlight as UI_ChooseOption;
                if (chooseOptionFlight != null)
                {
                    chooseOptionFlight.options = _modularTankList.Select(s => s.SwitchName).ToArray();
                    chooseOptionFlight.onFieldChanged = UpdateFromGUI;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: OnStart Error: " + e.Message);
                throw;
            }
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            SwitchOrAssign((int)oldFieldValueObj);
        }

        private void SwitchOrAssign(int oldFieldValueObj)
        {
            try
            {
                var currentTank = _modularTankList[selectedTankSetup];

                if (!currentTank.hasTech || (controlCrewCapacity && part.protoModuleCrew != null && currentTank.crewCapacity < part.protoModuleCrew.Count))
                {
                    if (oldFieldValueObj < selectedTankSetup || (oldFieldValueObj == _modularTankList.Count - 1 && selectedTankSetup == 0))
                        nextTankSetupEvent();
                    else
                        previousTankSetupEvent();
                }
                else
                    AssignResourcesToPart(true, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: SwitchOrAssign Error: " + e.Message);
                throw;
            }
        }

        // Called by external classes
        public int SelectTankSetup(int newTankIndex, bool calledByPlayer)
        {
            return SelectTankSetup(newTankIndex.ToString(CultureInfo.InvariantCulture), calledByPlayer);
        }

        // Called by external classes
        public int SelectTankSetup(string newTankName, bool calledByPlayer)
        {
            try
            {
                InitializeData();

                var desiredTank = _modularTankList.FirstOrDefault(m => m.GuiName == newTankName) ??
                                  _modularTankList.FirstOrDefault(m => m.SwitchName == newTankName) ?? 
                                  _modularTankList.FirstOrDefault(m => m.Composition == newTankName);

                if (desiredTank == null)
                {
                    int index;
                    if (int.TryParse(newTankName, out index))
                        desiredTank = index < _modularTankList.Count ? _modularTankList[index] : null;
                }

                if (desiredTank == null)
                    return -1;

                var oldSelectedTankSetup = selectedTankSetup;
                selectedTankSetup = _modularTankList.IndexOf(desiredTank);
                selectedTankSetupTxt = desiredTank.GuiName;

                // check if we are allowed to select this tank
                SwitchOrAssign(oldSelectedTankSetup);

                return selectedTankSetup;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: SelectTankSetup Error: " + e.Message);
                throw;
            }
        }

        public override void OnAwake()
        {
            try
            {
                if (configLoaded)
                    InitializeData();
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: OnAwake Error: " + e.Message);
                throw;
            }
        }

        public override void OnLoad(ConfigNode partNode)
        {
            base.OnLoad(partNode);

            if (!configLoaded)
                InitializeData();

            configLoaded = true;
        }

        private void InitializeData()
        {
            try
            {
                // Prevent execution to once per Scene switch
                if (_initialized)
                    return;

                _field0 = Fields["resourceAmountStr0"];
                _field1 = Fields["resourceAmountStr1"];
                _field2 = Fields["resourceAmountStr2"];
                _field3 = Fields["resourceAmountStr3"];
                _tankGuiNameField = Fields["tankGuiName"];

                availableInEditor = String.IsNullOrEmpty(inEditorSwitchingTechReq) ? availableInEditor : HasTech(inEditorSwitchingTechReq);
                availableInFlight = String.IsNullOrEmpty(inFlightSwitchingTechReq) ? availableInFlight : HasTech(inFlightSwitchingTechReq);

                SetupTankList();

                if (HighLogic.LoadedSceneIsGame)
                {
                    foreach (var modularTank in _modularTankList)
                    {
                        modularTank.hasTech = HasTech(modularTank.techReq);
                    }
                    _numberOfAvailableTanks = _modularTankList.Count(m => m.hasTech);
                }

                _nextTankSetupEvent = Events["nextTankSetupEvent"];
                _nextTankSetupEvent.guiActive = hasGUI && availableInFlight;

                _previousTankSetupEvent = Events["previousTankSetupEvent"];
                _previousTankSetupEvent.guiActive = hasGUI && availableInFlight;

                Fields["dryCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;
                Fields["resourceCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;
                Fields["maxResourceCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;
                Fields["totalCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;

                if (useTextureSwitchModule)
                {
                    textureSwitch = part.GetComponent<InterstellarTextureSwitch2>(); // only looking for first, not supporting multiple fuel switchers
                    if (textureSwitch == null)
                        useTextureSwitchModule = false;
                }

                _initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: InitializeData Error: " + e.Message);
                throw;
            }
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Switch Tank")]
        public void switchTankEvent()
        {
            closeAterSwitch = true;
            render_window = true;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_nextTankSetupText")]
        public void nextTankSetupEvent()
        {
            try
            {
                Debug.Assert(_numberOfAvailableTanks > 0, "No available IFS tank has the proper tech.");

                selectedTankSetup++;

                if (selectedTankSetup >= _modularTankList.Count)
                    selectedTankSetup = 0;

                var currentTank = _modularTankList[selectedTankSetup];

                if (!currentTank.hasTech || (controlCrewCapacity && currentTank.crewCapacity < part.protoModuleCrew.Count))
                    nextTankSetupEvent();

                AssignResourcesToPart(true, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: nextTankSetupEvent Error: " + e.Message);
                throw;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_previousTankSetupText")]
        public void previousTankSetupEvent()
        {
            try
            {
                Debug.Assert(_numberOfAvailableTanks > 0, "No available IFS tank has the proper tech.");

                selectedTankSetup--;

                if (selectedTankSetup < 0)
                    selectedTankSetup = _modularTankList.Count - 1;

                if (!_modularTankList[selectedTankSetup].hasTech)
                    previousTankSetupEvent();

                AssignResourcesToPart(true, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: previousTankSetupEvent Error: " + e.Message);
                throw;
            }
        }

        private void AssignResourcesToPart(bool calledByPlayer = false, bool affectSymCounterparts = false)
        {
            try
            {
                // destroying a resource messes up the gui in editor, but not in flight.
                var currentResources = SetupTankInPart(part, calledByPlayer);

                // update GUI part
                ConfigureResourceMassGui(currentResources);
                UpdateTankName();
                UpdateTexture(calledByPlayer);

                // update Dry Mass
                dryMass = 0;
                UpdateDryMass();
                UpdateGuiResourceMass();
                UpdateCost();

                if (HighLogic.LoadedSceneIsEditor && affectSymCounterparts)
                {
                    foreach (var symPart in part.symmetryCounterparts)
                    {
                        var symSwitch = string.IsNullOrEmpty(tankId)
                            ? symPart.FindModulesImplementing<InterstellarFuelSwitch>().FirstOrDefault()
                            : symPart.FindModulesImplementing<InterstellarFuelSwitch>().FirstOrDefault(m => m.tankId == tankId);

                        if (symSwitch == null) continue;

                        symSwitch.selectedTankSetup = selectedTankSetup;
                        symSwitch.selectedTankSetupTxt = selectedTankSetupTxt;
                        symSwitch.AssignResourcesToPart(calledByPlayer, false);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: AssignResourcesToPart Error: " + e.Message);
                throw;
            }
        }

        public void UpdateTexture(bool calledByPlayer)
        {
            if (textureSwitch != null)
                textureSwitch.SelectTankSetup(selectedTankSetup, calledByPlayer);
        }

        public void UpdateTankName()
        {
            tankGuiName = _modularTankList[selectedTankSetup].GuiName;

            var tankGuiNameIsNotEmpty = !String.IsNullOrEmpty(tankGuiName);
            _tankGuiNameField.guiActive = showTankName && tankGuiNameIsNotEmpty;
            _tankGuiNameField.guiActiveEditor = showTankName && tankGuiNameIsNotEmpty;
        }

        private List<string> SetupTankInPart(Part currentPart, bool calledByPlayer)
        {
            try
            {
                FindSelectedTank(calledByPlayer);

                // update txt and index for future
                selectedTankSetupTxt = selectedTank.GuiName;
                selectedTankSetup = _modularTankList.IndexOf(selectedTank);

                // create new ResourceNode
                var newResources = new List<string>();
                var newResourceNodes = new List<ConfigNode>();
                var parsedConfigAmount = new List<double>();
                var parsedConfigFlowStates = new List<bool>();

                // parse configured amounts
                if (configuredAmounts.Length > 0)
                {
                    // empty configuration if switched by user
                    if (calledByPlayer)
                        configuredAmounts = String.Empty;

                    var configAmounts = configuredAmounts.Split(',');
                    foreach (var item in configAmounts)
                    {
                        double value;
                        if (double.TryParse(item, out value))
                            parsedConfigAmount.Add(value);
                    }

                    // empty configuration if in flight
                    if (!HighLogic.LoadedSceneIsEditor)
                        configuredAmounts = String.Empty;
                }

                if (configuredFlowStates.Length > 0)
                {
                    // empty configuration if switched by user
                    if (calledByPlayer)
                        configuredFlowStates = String.Empty;

                    var configFlowStates = configuredFlowStates.Split(',');
                    foreach (var item in configFlowStates)
                    {
                        bool value;
                        if (bool.TryParse(item, out value))
                            parsedConfigFlowStates.Add(value);
                    }

                    // empty configuration if in flight
                    if (!HighLogic.LoadedSceneIsEditor)
                        configuredFlowStates = String.Empty;
                }

                if (controlCrewCapacity)
                {
                    var newCrewCapacity = (int)Math.Round(selectedTank.crewCapacity * storedSurfaceMultiplier);
                    Debug.Log("[IFS]: SetupTankInPart Set CrewCapacity : " + newCrewCapacity);
                    part.CrewCapacity = newCrewCapacity;
                    part.crewTransferAvailable = newCrewCapacity > 0;
                }

                UpdateKerbalismHabitat(selectedTank.habitatVolume, selectedTank.habitatSurface);

                for (var resourceId = 0; resourceId < selectedTank.Resources.Count; resourceId++)
                {
                    var selectedTankResource = selectedTank.Resources[resourceId];

                    if (selectedTankResource.name == "Structural")
                        continue;

                    newResources.Add(selectedTankResource.name);

                    var newResourceNode = new ConfigNode("RESOURCE");
                    var maxAmount = selectedTankResource.maxAmount * storedVolumeMultiplier;

                    newResourceNode.AddValue("name", selectedTankResource.name);
                    newResourceNode.AddValue("maxAmount", maxAmount);

                    PartResource existingResource = null;
                    if (!HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsEditor && !calledByPlayer))
                    {
                        foreach (var partResource in currentPart.Resources)
                        {
                            if (!partResource.resourceName.Equals(selectedTankResource.name)) continue;

                            existingResource = partResource;
                            break;
                        }
                    }

                    double resourceNodeAmount;

                    if (existingResource != null)
                        resourceNodeAmount = Math.Min((existingResource.amount / existingResource.maxAmount) * maxAmount, maxAmount);
                    else if (!HighLogic.LoadedSceneIsEditor && resourceId < parsedConfigAmount.Count)
                        resourceNodeAmount = parsedConfigAmount[resourceId];
                    else if (!HighLogic.LoadedSceneIsEditor && calledByPlayer)
                        resourceNodeAmount = 0.0;
                    else
                        resourceNodeAmount = selectedTank.Resources[resourceId].amount * storedVolumeMultiplier;

                    newResourceNode.AddValue("amount", resourceNodeAmount);

                    if (existingResource != null)
                        newResourceNode.AddValue("flowState", existingResource.flowState);
                    else if (resourceId < parsedConfigFlowStates.Count)
                        newResourceNode.AddValue("flowState", parsedConfigFlowStates[resourceId]);

                    newResourceNodes.Add(newResourceNode);
                }

                var finalResourceNodes = new List<ConfigNode>();
                if (newResourceNodes.Count > 0)
                {
                    finalResourceNodes.AddRange(newResourceNodes);
                    newResourceNodes.Clear();
                }

                foreach (var resource in currentPart.Resources)
                {
                    if (activeResourceList.Contains(resource.resourceName)) continue;

                    var newResourceNode = new ConfigNode("RESOURCE");
                    newResourceNode.AddValue("name", resource.resourceName);
                    newResourceNode.AddValue("maxAmount", resource.maxAmount);
                    newResourceNode.AddValue("amount", resource.amount);
                    newResourceNode.AddValue("flowState", resource.flowState);

                    finalResourceNodes.Add(newResourceNode);
                }

                // add any remaining new nodes
                if (newResourceNodes.Count > 0)
                {
                    finalResourceNodes.AddRange(newResourceNodes);
                    newResourceNodes.Clear();
                }

                // remove all resources
                currentPart.Resources.Clear();

                // add new or exisitng resources
                if (finalResourceNodes.Count > 0)
                {
                    foreach (var resourceNode in finalResourceNodes)
                    {
                        currentPart.AddResource(resourceNode);
                    }
                }

                UpdateCost();

                UpdatePartActionWindow();

                return newResources;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: SetupTankInPart Error: " + e.Message);
                throw;
            }
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

        private void FindSelectedTank(bool calledByPlayer)
        {
            try
            {
                // first find selected tank on index
                selectedTank = calledByPlayer && selectedTankSetup >= 0 && selectedTankSetup < _modularTankList.Count ? _modularTankList[selectedTankSetup] : null;

                // find based on guiname, switchname or contents
                if (selectedTank == null)
                {
                    var matchingIndex = FindMatchingConfig();
                    if (matchingIndex >= 0)
                        selectedTank = _modularTankList[matchingIndex];
                }

                // if still no tank found create a tank based on current tank contents
                if (selectedTank == null && (HighLogic.LoadedSceneIsFlight || _modularTankList.Count == 0) && part.Resources.Any(m => m.info.density > 0))
                {
                    var resourcesWithMass = part.Resources.Where(m => m.info.density > 0);

                    var concatinatedGuiName = string.Join("+", resourcesWithMass.Select(r => r.info.displayName).ToArray());

                    Debug.LogWarning("[IFS]: Constructing new tank definition for " + part.name + " with name " + concatinatedGuiName);

                    var ifsResources = resourcesWithMass.Select(r => new IFSresource(r.resourceName)
                    {
                        amount = r.amount / storedVolumeMultiplier,
                        maxAmount = r.maxAmount / storedVolumeMultiplier
                    }).ToList();

                    var concatinatedSwitchName = string.Join("+", resourcesWithMass.Select(r => r.info.abbreviation).ToArray());

                    selectedTank = new IFSmodularTank
                    {
                        SwitchName = concatinatedSwitchName,
                        Composition = concatinatedSwitchName,
                        GuiName = concatinatedGuiName,
                        Resources = ifsResources
                    };

                    _modularTankList.Add(selectedTank);
                }

                // otherwise select first tank
                if (selectedTank == null)
                {
                    Debug.Log("[IFS]: Defaulting selected tank to first tank in collection");
                    selectedTank = _modularTankList[0];
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: FindSelectedTank " + e.Message);
            }
        }

        // only called after tank switching
        public void ConfigureResourceMassGui(List<string> newResources)
        {
            _partRresourceDefinition0 = newResources.Count > 0 ? PartResourceLibrary.Instance.GetDefinition(newResources[0]) : null;
            _partRresourceDefinition1 = newResources.Count > 1 ? PartResourceLibrary.Instance.GetDefinition(newResources[1]) : null;
            _partRresourceDefinition2 = newResources.Count > 2 ? PartResourceLibrary.Instance.GetDefinition(newResources[2]) : null;
            _partRresourceDefinition3 = newResources.Count > 3 ? PartResourceLibrary.Instance.GetDefinition(newResources[3]) : null;

            _field0.guiName = _partRresourceDefinition0 != null ? _partRresourceDefinition0.name : ":";
            _field1.guiName = _partRresourceDefinition1 != null ? _partRresourceDefinition1.name : ":";
            _field2.guiName = _partRresourceDefinition2 != null ? _partRresourceDefinition2.name : ":";
            _field3.guiName = _partRresourceDefinition3 != null ? _partRresourceDefinition3.name : ":";

            _field0.guiActive = _partRresourceDefinition0 != null;
            _field1.guiActive = _partRresourceDefinition1 != null;
            _field2.guiActive = _partRresourceDefinition2 != null;
            _field3.guiActive = _partRresourceDefinition3 != null;

            _field0.guiActiveEditor = _partRresourceDefinition0 != null;
            _field1.guiActiveEditor = _partRresourceDefinition1 != null;
            _field2.guiActiveEditor = _partRresourceDefinition2 != null;
            _field3.guiActiveEditor = _partRresourceDefinition3 != null;

            _partResource0 = _partRresourceDefinition0 == null ? null : part.Resources[newResources[0]];
            _partResource1 = _partRresourceDefinition1 == null ? null : part.Resources[newResources[1]];
            _partResource2 = _partRresourceDefinition2 == null ? null : part.Resources[newResources[2]];
            _partResource3 = _partRresourceDefinition3 == null ? null : part.Resources[newResources[3]];

            _partResourceMaxAmountFraction0 = _partResource0 == null ? 0 : _partResource0.maxAmount * 0.001;
            _partResourceMaxAmountFraction1 = _partResource1 == null ? 0 : _partResource1.maxAmount * 0.001;
            _partResourceMaxAmountFraction2 = _partResource2 == null ? 0 : _partResource2.maxAmount * 0.001;
            _partResourceMaxAmountFraction3 = _partResource3 == null ? 0 : _partResource3.maxAmount * 0.001;
        }

        private double UpdateCost()
        {
            dryCost = (double)(decimal)part.partInfo.cost * initialMassMultiplier;

            if (selectedTankSetup >= 0 && selectedTankSetup < _modularTankList.Count)
                dryCost += _modularTankList[selectedTankSetup].tankCost * initialMassMultiplier;

            resourceCost = 0;
            maxResourceCost = 0;

            if (_partRresourceDefinition0 == null || _partResource0 == null)
            {
                totalCost = dryCost;
                return 0;
            }

            var preserveInitialCost = false;
            if (!ignoreInitialCost && !string.IsNullOrEmpty(initialTankSetup))
            {
                preserveInitialCost = true;
                var initialTankSetupArray = initialTankSetup.Split(';');


                var initialTankSetupArrayCount = initialTankSetupArray.Count();
                for (var i = 0; i < initialTankSetupArrayCount; i++)
                {
                    var resourcename = initialTankSetupArray[i];
                    if (part.Resources.Contains(resourcename)) continue;

                    preserveInitialCost = false;
                    break;
                }
            }

            var isSmaller = storedFactorMultiplier < 0.999;
            var isLarger = storedFactorMultiplier > 1.001;

            var unitCost0 = (double)(decimal)_partRresourceDefinition0.unitCost;
            resourceCost += unitCost0 * _partResource0.amount;
            maxResourceCost += unitCost0 * _partResource0.maxAmount;

            if (_partRresourceDefinition1 == null || _partResource1 == null)
            {
                if (preserveInitialCost)
                {
                    totalCost = dryCost - maxResourceCost + resourceCost;
                    return 0;
                }
                else
                {
                    totalCost = dryCost + resourceCost;
                    return !isSmaller && !isLarger ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
                }
            }

            var unitCost1 = (double)(decimal)_partRresourceDefinition1.unitCost;
            resourceCost += unitCost1 * _partResource1.amount;
            maxResourceCost += unitCost1 * _partResource1.maxAmount;

            if (_partRresourceDefinition2 == null || _partResource2 == null)
            {
                if (preserveInitialCost)
                {
                    totalCost = dryCost - maxResourceCost + resourceCost;
                    return 0;
                }
                else
                {
                    totalCost = dryCost + resourceCost;
                    return !isSmaller && !isLarger ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
                }
            }

            var unitCost2 = (double)(decimal)_partRresourceDefinition2.unitCost;
            resourceCost += unitCost2 * _partResource2.amount;
            maxResourceCost += unitCost2 * _partResource2.maxAmount;

            if (_partRresourceDefinition3 == null || _partResource3 == null)
            {
                if (preserveInitialCost)
                {
                    totalCost = dryCost - maxResourceCost + resourceCost;
                    return 0;
                }
                else
                {
                    totalCost = dryCost + resourceCost;
                    return !isSmaller && !isLarger ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
                }
            }

            var unitCost3 = (double)(decimal)_partRresourceDefinition3.unitCost;
            resourceCost += unitCost3 * _partResource3.amount;
            maxResourceCost += unitCost3 * _partResource3.maxAmount;

            if (preserveInitialCost)
            {
                totalCost = dryCost - maxResourceCost + resourceCost;
                return 0;
            }
            else
            {
                totalCost = dryCost + resourceCost;
                return !isSmaller && !isLarger ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
            }
        }

        private void UpdateDryMass()
        {
            if (dryMass != 0 && HighLogic.LoadedSceneIsFlight) return;

            // update Dry Mass
            dryMass = CalculateDryMass();

            UpdateMassRatio();
        }

        private double CalculateDryMass()
        {
            if (selectedTank == null && selectedTankSetup >= 0 && selectedTankSetup < _modularTankList.Count)
            {
                selectedTank = _modularTankList[selectedTankSetup];
            }

            double mass = basePartMass * baseMassMultiplier;
            if (selectedTank != null)
            {
                var totalTankResourceMassDivider = selectedTank.resourceMassDivider + selectedTank.resourceMassDividerAddition;

                if (overrideMassWithTankDividers && totalTankResourceMassDivider > 0)
                    mass = selectedTank.FullResourceMass / totalTankResourceMassDivider * initialMassMultiplier;
                else
                {
                    mass += selectedTank.tankMass * baseMassMultiplier;

                    // use baseResourceMassDivider if specified
                    if (baseResourceMassDivider > 0)
                        mass += (selectedTank.FullResourceMass / baseResourceMassDivider * initialMassMultiplier);

                    // use resourceMassDivider if specified
                    if (totalTankResourceMassDivider > 0)
                        mass += (selectedTank.FullResourceMass / totalTankResourceMassDivider) * initialMassMultiplier;
                }
            }

            // prevent 0 mass
            if (mass <= 0)
                mass = (double)(decimal)part.prefabMass * initialMassMultiplier;

            return mass;
        }

        private string FormatMassStr(double amount)
        {
            if (amount >= 1)
                return (amount).ToString(resourcesFormat) + " t";
            if (amount >= 1e-3)
                return (amount * 1e3).ToString(resourcesFormat) + " kg";
            if (amount >= 1e-6)
                return (amount * 1e6).ToString(resourcesFormat) + " g";
            else
                return (amount * 1e9).ToString(resourcesFormat) + " mg";
        }

        private void UpdateGuiResourceMass()
        {
            var missing0 = _partRresourceDefinition0 == null || _partResource0 == null;
            var missing1 = _partRresourceDefinition1 == null || _partResource1 == null;
            var missing2 = _partRresourceDefinition2 == null || _partResource2 == null;
            var missing3 = _partRresourceDefinition3 == null || _partResource3 == null;

            if (_massRatioStrField != null)
            {
                _massRatioStrField.guiActive = !missing0;
                _massRatioStrField.guiActiveEditor = !missing0;
            }

            if (_maxWetDryMassField != null)
            {
                _maxWetDryMassField.guiActive = !missing0;
                _maxWetDryMassField.guiActiveEditor = !missing0;
            }

            var currentResourceMassAmount0 = missing0 ? 0 : (double)(decimal)_partRresourceDefinition0.density * _partResource0.amount;
            var currentResourceMassAmount1 = missing1 ? 0 : (double)(decimal)_partRresourceDefinition1.density * _partResource1.amount;
            var currentResourceMassAmount2 = missing2 ? 0 : (double)(decimal)_partRresourceDefinition2.density * _partResource2.amount;
            var currentResourceMassAmount3 = missing3 ? 0 : (double)(decimal)_partRresourceDefinition3.density * _partResource3.amount;

            totalMass = dryMass + currentResourceMassAmount0 + currentResourceMassAmount1 + currentResourceMassAmount2 + currentResourceMassAmount3;

            resourceAmountStr0 = missing0 ? String.Empty : FormatMassStr(currentResourceMassAmount0);
            resourceAmountStr1 = missing1 ? String.Empty : FormatMassStr(currentResourceMassAmount1);
            resourceAmountStr2 = missing2 ? String.Empty : FormatMassStr(currentResourceMassAmount2);
            resourceAmountStr3 = missing3 ? String.Empty : FormatMassStr(currentResourceMassAmount3);
        }

        private void UpdateMassRatio()
        {
            var maxResourceMassAmount0 = _partRresourceDefinition0 == null || _partResource0 == null ? 0 : (double)(decimal)_partRresourceDefinition0.density * _partResource0.maxAmount;
            var maxResourceMassAmount1 = _partRresourceDefinition1 == null || _partResource1 == null ? 0 : (double)(decimal)_partRresourceDefinition1.density * _partResource1.maxAmount;
            var maxResourceMassAmount2 = _partRresourceDefinition2 == null || _partResource2 == null ? 0 : (double)(decimal)_partRresourceDefinition2.density * _partResource2.maxAmount;
            var maxResourceMassAmount3 = _partRresourceDefinition3 == null || _partResource3 == null ? 0 : (double)(decimal)_partRresourceDefinition3.density * _partResource3.maxAmount;

            if (!displayWetDryMass) return;

            var wetMass = maxResourceMassAmount0 + maxResourceMassAmount1 + maxResourceMassAmount2 + maxResourceMassAmount3;

            if (wetMass > 0 && dryMass > 0)
                massRatioStr = ToRoundedString(1 / (dryMass / wetMass));

            maxWetDryMass = string.Format("{0} t / {1} t", ToStringWithFixedDigits(dryMass), ToStringWithFixedDigits(wetMass));

            crewCapacityStr = string.Format("{0} / {1}", part.protoModuleCrew.Count, part.CrewCapacity); 
        }

        private string ToRoundedString(double value)
        {
            var differenceWithRounded = Math.Abs(value - Math.Round(value, 0));

            if (differenceWithRounded > 0.05)
                return "1 : " + value.ToString("0.0");
            else if (differenceWithRounded > 0.005)
                return "1 : " + value.ToString("0.00");
            else if (differenceWithRounded > 0.0005)
                return "1 : " + value.ToString("0.000");
            else
                return "1 : " + value.ToString("0");
        }

        private string ToStringWithFixedDigits(double value)
        {
            if (value >= 1000000)
                return value.ToString("0");
            else if (value >= 100000)
                return value.ToString("0.0");
            else if (value >= 10000)
                return value.ToString("0.00");
            else if (value >= 1000)
                return value.ToString("0.000");
            else if (value >= 100)
                return value.ToString("0.0000");
            else if (value >= 10)
                return value.ToString("0.00000");
            else
                return value.ToString("0.000000");
        }

        // Note: do note remove, it is called by KSP
        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                UpdateGuiResourceMass();

                allowedToSwitch = availableInFlight && _numberOfAvailableTanks > 1 &&
                                    (canSwitchWithFullTanks || (
                                      (_partResource0 == null || _partResource0.amount < _partResourceMaxAmountFraction0) &&
                                      (_partResource1 == null || _partResource1.amount < _partResourceMaxAmountFraction1) &&
                                      (_partResource2 == null || _partResource2.amount < _partResourceMaxAmountFraction2) &&
                                      (_partResource3 == null || _partResource3.amount < _partResourceMaxAmountFraction3))
                                    );

                // show/hide choose option
                _chooseField.guiActive = allowedToSwitch;

                // show/hide switch buttons
                _nextTankSetupEvent.guiActive = hasGUI && allowedToSwitch;
                _previousTankSetupEvent.guiActive = hasGUI && allowedToSwitch;

                return;
            }

            // update Dry Mass
            UpdateDryMass();
            UpdateGuiResourceMass();
            UpdateCost();

            configuredAmounts = String.Empty; ;
            configuredFlowStates = String.Empty;

            foreach (var resoure in part.Resources)
            {
                configuredAmounts += resoure.amount + ",";
                configuredFlowStates += resoure.flowState + ",";
            }
        }

        private void SetupTankList()
        {
            try
            {
                var weightList = ParseTools.ParseDoubles(tankMass, () => tankMass);
                var tankCostList = ParseTools.ParseDoubles(tankCost, () => tankCost);

                var tankResourceMassDividerList = ParseTools.ParseDoubles(tankResourceMassDivider, () => tankResourceMassDivider);
                var tankResourceMassDividerAdditionList = ParseTools.ParseDoubles(tankResourceMassDividerAddition, () => tankResourceMassDividerAddition);

                var crewCapacityArray = ParseTools.ParseDoubles(crewCapacity, () => crewCapacity);
                var habitatVolumeArray = ParseTools.ParseDoubles(habitatVolume, () => habitatVolume);
                var habitatSurfaceArray = ParseTools.ParseDoubles(habitatSurface, () => habitatSurface);

                // First find the amounts each tank type is filled with
                var resourceList = new List<List<double>>();
                var initialResourceList = new List<List<double>>();
                var boilOffTempList = new List<List<double>>();
                var latendHeatVaporationList = new List<List<double>>();
                
                var resourceTankAbsoluteAmountArray = resourceAmounts.Split(';');
                var resourceTankRatioAmountArray = resourceRatios.Split(';');
                var initialResourceTankArray = initialResourceAmounts.Split(';');
                var boilOffTempTankArray = boilOffTemp.Split(';');
                var tankNameArray = resourceNames.Split(';');
                var tankTechReqArray = tankTechReq.Split(';');
                var tankGuiNameArray = resourceGui.Split(';');
                var tankSwitcherNameArray = tankSwitchNames.Split(';');

                // if initial resource ammount is missing or not complete, use full amount
                if (initialResourceAmounts.Equals(String.Empty) ||
                    initialResourceTankArray.Length != resourceTankAbsoluteAmountArray.Length)
                    initialResourceTankArray = resourceTankAbsoluteAmountArray;

                var maxLengthTankArray = Math.Max(resourceTankAbsoluteAmountArray.Length, resourceTankRatioAmountArray.Length);

                for (var tankCounter = 0; tankCounter < maxLengthTankArray; tankCounter++)
                {
                    resourceList.Add(new List<double>());
                    initialResourceList.Add(new List<double>());
                    boilOffTempList.Add(new List<double>());
                    latendHeatVaporationList.Add(new List<double>());

                    var resourceAmountArray = resourceTankAbsoluteAmountArray[tankCounter].Trim().Split(',');
                    var initialResourceAmountArray = initialResourceTankArray[tankCounter].Trim().Split(',');

                    // if missing or not complete, use full amount
                    if (initialResourceAmounts.Equals(String.Empty) ||
                        initialResourceAmountArray.Length != resourceAmountArray.Length)
                        initialResourceAmountArray = resourceAmountArray;

                    for (var amountCounter = 0; amountCounter < resourceAmountArray.Length; amountCounter++)
                    {
                        try
                        {
                            if (tankCounter >= resourceList.Count || amountCounter >= resourceAmountArray.Count()) continue;

                            resourceList[tankCounter].Add(double.Parse(resourceAmountArray[amountCounter].Trim()));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("[IFS]: " + part.name + " error parsing resourceTankAmountArray amount " + tankCounter + "/" + amountCounter +
                                      ": '" + resourceTankAbsoluteAmountArray[tankCounter] + "': '" + resourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < initialResourceList.Count && amountCounter < initialResourceAmountArray.Count())
                                initialResourceList[tankCounter].Add(ParseTools.ParseDouble(initialResourceAmountArray[amountCounter]));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("[IFS]: " + part.name + " error parsing initialResourceList amount " + tankCounter + "/" + amountCounter +
                                      ": '" + initialResourceList[tankCounter] + "': '" + initialResourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }
                    }
                }

                // Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)
                for (var currentResourceCounter = 0; currentResourceCounter < tankNameArray.Length; currentResourceCounter++)
                {
                    // create a new modularTank
                    var modularTank = new IFSmodularTank();
                    _modularTankList.Add(modularTank);

                    // initialiseSwitchName
                    if (currentResourceCounter < tankSwitcherNameArray.Length)
                        modularTank.SwitchName = Localizer.Format(tankSwitcherNameArray[currentResourceCounter]);

                    // initialize Gui name if possible
                    if (currentResourceCounter < tankGuiNameArray.Length)
                        modularTank.GuiName = Localizer.Format(tankGuiNameArray[currentResourceCounter]);

                    if (currentResourceCounter < crewCapacityArray.Count)
                        modularTank.crewCapacity = (int)crewCapacityArray[currentResourceCounter];

                    if (currentResourceCounter < habitatVolumeArray.Count)
                        modularTank.habitatVolume = habitatVolumeArray[currentResourceCounter];

                    if (currentResourceCounter < habitatSurfaceArray.Count)
                        modularTank.habitatSurface = habitatSurfaceArray[currentResourceCounter];

                    // initialise tech requirement but ignore first
                    if (currentResourceCounter != 0 && currentResourceCounter < tankTechReqArray.Length)
                        modularTank.techReq = tankTechReqArray[currentResourceCounter].Trim(' ');

                    // initialise tank mass
                    if (currentResourceCounter < weightList.Count)
                        modularTank.tankMass = weightList[currentResourceCounter];

                    if (currentResourceCounter < tankResourceMassDividerList.Count)
                        modularTank.resourceMassDivider = tankResourceMassDividerList[currentResourceCounter];

                    if (currentResourceCounter < tankResourceMassDividerAdditionList.Count)
                        modularTank.resourceMassDividerAddition = tankResourceMassDividerAdditionList[currentResourceCounter];

                    // initialise tank cost
                    if (currentResourceCounter < tankCostList.Count)
                        modularTank.tankCost = tankCostList[currentResourceCounter];

                    var resourceNameArray = tankNameArray[currentResourceCounter].Split(',');

                    modularTank.Composition = string.Join("+", resourceNameArray.Select(r => r).ToArray());

                    for (var nameCounter = 0; nameCounter < resourceNameArray.Length; nameCounter++)
                    {
                        var resourceName = resourceNameArray[nameCounter].Trim(' ');
                        var newResource = new IFSresource(resourceName);

                        if (!activeResourceList.Contains(resourceName))
                            activeResourceList.Add(resourceName);

                        if (resourceList[currentResourceCounter] != null && nameCounter < resourceList[currentResourceCounter].Count)
                        {
                            newResource.maxAmount = resourceList[currentResourceCounter][nameCounter];
                            newResource.amount = initialResourceList[currentResourceCounter][nameCounter];
                        }

                        modularTank.Resources.Add(newResource);
                    }

                    var extraActiveResourceList = bannedResourceNames.Split(';');
                    foreach (var resourceName in extraActiveResourceList)
                    {
                        if (!activeResourceList.Contains(resourceName))
                            activeResourceList.Add(resourceName);
                    }

                    // ensure there is always a gui name
                    if (string.IsNullOrEmpty(modularTank.GuiName))
                    {
                        Debug.Log("[IFS]: " + part.name + " modularTank.GuiName is null");

                        var names = modularTank.Resources.Select(m => m.name);
                        modularTank.GuiName = String.Empty;
                        foreach (var name in names)
                        {
                            if (!String.IsNullOrEmpty(modularTank.GuiName))
                                modularTank.GuiName += "+";
                            modularTank.GuiName += name;
                        }
                    }

                    // use guiTankName is switchName is missing
                    if (string.IsNullOrEmpty(modularTank.SwitchName))
                        modularTank.SwitchName = modularTank.GuiName;
                }

                if (orderBySwitchName)
                {
                    _modularTankList = _modularTankList.OrderBy(m => m.SwitchName).ToList();
                }

                if (debugMode)
                {
                    foreach (var config in _modularTankList)
                    {
                        string description =  "[IFS]: " + part.name + " Composition :" + config.Composition + " GuiName:" + config.GuiName + " SwitchName: " + config.SwitchName + " Resources: ";
                        description += string.Join(",", config.Resources.Select(m => m.name).ToArray());
                        Debug.Log(description);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: SetupTankList Error: " + e.Message);
                throw;
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            moduleCost = updateModuleCost ? (float)UpdateCost() : 0;

            return moduleCost;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return HighLogic.LoadedSceneIsFlight ? ModifierChangeWhen.STAGED : ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            this.defaultMass = defaultMass;

            if (returnDryMass)
                return (float)dryMass;
            else
            {
                UpdateDryMass();

                moduleMassDelta = dryMass - initialMass;

                return (float)moduleMassDelta;
            }
        }

        public override string GetInfo()
        {
            if (!showInfo) return string.Empty;

            var info = new StringBuilder();

            if (!string.IsNullOrEmpty(moduleInfoTemplate))
            {
                var parameters = new List<string>();

                if (!string.IsNullOrEmpty(moduleInfoParams))
                {
                    parameters = moduleInfoParams.Split(';').ToList();

                    // translate parameters
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        parameters[i] = Localizer.Format(parameters[i]);
                    }
                }

                var lines = moduleInfoTemplate.Split(new[] { "<br/>" }, StringSplitOptions.None).ToList();

                var parameterArray = parameters.ToArray();

                lines.ForEach(line => info.AppendLine(Localizer.Format(line, parameterArray)));

                return info.ToString();
            }

            info.AppendLine(Localizer.Format("#LOC_IFS_FuelSwitch_GetInfo") + ":");
            info.Append("<size=10>");
            info.AppendLine();

            foreach (var module in _modularTankList)
            {
                var multi = (module.Resources.Count > 1);

                if (multi)
                {
                    info.Append("<color=#00ff00ff>");
                    info.Append(module.SwitchName);
                    info.Append("</color>");
                    info.AppendLine();
                }

                foreach (var resource in module.Resources)
                {
                    if (multi)
                        info.Append("* ");

                    info.Append(Math.Round(resource.maxAmount, 0));
                    info.Append(" ");
                    info.Append("<color=#00ffffff>");
                    info.Append(resource.name);
                    info.Append("</color>");
                    info.AppendLine();
                }
            }
            info.Append("</size>");
            return info.ToString();
        }

        private static bool HasTech(string techid)
        {
            try
            {
                if (String.IsNullOrEmpty(techid))
                    return true;

                if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                    return true;

                if ((HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX))
                    return true;

                if (ResearchAndDevelopment.Instance == null)
                {
                    if (_researchedTechs == null)
                        LoadSaveFile();

                    return _researchedTechs != null && _researchedTechs.Contains(techid);
                }
                else
                {
                    var techstate = ResearchAndDevelopment.Instance.GetTechState(techid);
                    if (techstate != null)
                        return techstate.state == RDTech.State.Available;
                    else
                        return false;
                }
            }
            catch
            {
                Debug.LogError("[IFS] - Verify HasTech: " + techid);
                throw;
            }
        }

        private static void LoadSaveFile()
        {
            _researchedTechs = new HashSet<string>();

            var persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
            var config = ConfigNode.Load(persistentfile);
            var gameconf = config.GetNode("GAME");
            var scenarios = gameconf.GetNodes("SCENARIO");

            foreach (var scenario in scenarios)
            {
                if (scenario.GetValue("name") != "ResearchAndDevelopment") continue;

                var techs = scenario.GetNodes("Tech");
                foreach (var technode in techs)
                {
                    var technodename = technode.GetValue("id");
                    _researchedTechs.Add(technodename);
                }
            }
        }

        public void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(_windowID, windowPosition, Window, part.partInfo.title);
        }

        private void Window(int windowID)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                {
                    closeAterSwitch = false;
                    render_window = false;
                }

                GUILayout.BeginVertical();

                foreach (var tank in _modularTankList)
                {
                    if (tank.hasTech)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(tank.GuiName, GUILayout.ExpandWidth(true)))
                        {
                            selectedTankSetup = _modularTankList.IndexOf(tank);
                            AssignResourcesToPart(true, true);
                            if (_fuelTankSetupControl != null)
                                _fuelTankSetupControl.SwitchToFuelTankSetup(tank.SwitchName);
                            if (closeAterSwitch)
                            {
                                closeAterSwitch = false;
                                render_window = false;
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
                GUI.DragWindow();

            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: InterstellarFuelSwitch Window(" + windowID + "): " + e.Message);
                throw;
            }
        }

        private void InitializeKerbalismHabitat()
        {
            bool found = false;

            foreach (PartModule module in part.Modules)
            {
                if (module.moduleName == "Habitat")
                {
                    found = true;
                    habitatModule = module;                   
                    habitatVolumeField = module.Fields["volume"];
                    habitatSurfaceField = module.Fields["surface"];
                    habitatStateField = module.Fields["state"];
                    habitatToggleField = module.Fields["toggle"];

                    var habitatType = module.GetType();
                    onStartMethod = habitatType.GetMethod("OnStart");

                    if (onStartMethod != null)
                        UnityEngine.Debug.Log("[IFS]: Found onStartMethod");

                    break;
                }
            }

            if (found)
                UnityEngine.Debug.Log("[IFS]: Found Habitat PartModule on " + part.partInfo.title);
            else
                UnityEngine.Debug.LogWarning("[IFS]: No Habitat PartModule found on " + part.partInfo.title);
        }

        private void UpdateKerbalismHabitat(double volume, double surface)
        {
            try
            {

                if (habitatModule == null)
                {
                    UnityEngine.Debug.Log("[IFS]: No habitat found");
                    return;
                }

                if (habitatVolumeField != null)
                {
                    UnityEngine.Debug.Log("[IFS]: Set habitat Volume to " + volume);
                    habitatVolumeField.SetValue(volume, habitatModule);
                }

                if (habitatSurfaceField != null)
                {
                    UnityEngine.Debug.Log("[IFS]: Set habitat Surface to " + surface);
                    habitatSurfaceField.SetValue(surface, habitatModule);
                }

                if (habitatToggleField != null)
                {
                    UnityEngine.Debug.Log("[IFS]: Set habitat toggle to " + surface);
                    habitatToggleField.SetValue(volume > 0, habitatModule);
                }

                if (habitatStateField != null)
                {
                    var newState = volume > 0 ? State.enabled : State.disabled;

                    var oldValue = habitatStateField.GetValue<int>(habitatModule);
                    UnityEngine.Debug.Log("[IFS]: old state set to " + oldValue);

                    UnityEngine.Debug.Log("[IFS]: Set habitat state to " + newState);
                    habitatStateField.SetValue((int)newState, habitatModule);

                    var newValue = habitatStateField.GetValue<int>(habitatModule);
                    UnityEngine.Debug.Log("[IFS]: new  state set to " + newValue);
                }

                var atmosphere = part.Resources["Atmosphere"];
                if (atmosphere != null)
                {
                    UnityEngine.Debug.Log("[IFS]: Set Atmosphere to " + volume * 1e3);
                    atmosphere.amount = volume * 1e3;
                    atmosphere.maxAmount = volume * 1e3;
                }

                var wasteAtmosphere = part.Resources["WasteAtmosphere"];
                if (wasteAtmosphere != null)
                {
                    UnityEngine.Debug.Log("[IFS]: Set WasteAtmosphere to " + volume * 1e3);
                    wasteAtmosphere.amount = 0;
                    wasteAtmosphere.maxAmount = volume * 1e3;
                }

                var moistAtmosphere = part.Resources["MoistAtmosphere"];
                if (moistAtmosphere != null)
                {
                    UnityEngine.Debug.Log("[IFS]: Set MoistAtmosphere to " + volume * 1e3);
                    moistAtmosphere.amount = 0;
                    moistAtmosphere.maxAmount = volume * 1e3;
                }

                if (onStartMethod != null)
                {
                    UnityEngine.Debug.Log("[IFS]: Invoke onStartMethod");
                    onStartMethod.Invoke(habitatModule, new object[] { (int)PartModule.StartState.None });
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: UpdateKerbalismHabitat "+ e.Message);
            }
        }
    }
}
