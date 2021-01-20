using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public double FullMass => maxAmount * density;
    }

    public class IFSmodularTank
    {
        public bool hasTech;
        public string GuiName = string.Empty;
        public string SwitchName = string.Empty;
        public string Composition = string.Empty;
        public string techReq;
        public double tankCost;
        public double tankMass;
        public double resourceMassDivider;
        public double resourceMassDividerAddition;
        public double habitatVolume;
        public double habitatSurface;
        public int crewCapacity;

        public List<IFSresource> resources = new List<IFSresource>();

        public double FullResourceMass { get { return resources.Sum(m => m.FullMass); } }
    }

    [KSPModule("#LOC_IFS_FuelSwitch_moduleName")]
    public class InterstellarFuelSwitch : PartModule, IRescalable<InterstellarFuelSwitch>, IPartCostModifier, IPartMassModifier
    {
        public const string Group = "InterstellarFuelSwitch";
        public const string GroupTitle = "#LOC_IFS_FuelSwitch_groupName";

        private static readonly string[] LineBreaks = { "<br/>" };

        // Persistants
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedTankSetup = -1;
        [KSPField(isPersistant = true)] public string configuredAmounts = "";
        [KSPField(isPersistant = true)] public string configuredFlowStates = "";
        [KSPField(isPersistant = true)] public string selectedTankSetupTxt;
        [KSPField(isPersistant = true)] public bool configLoaded;
        [KSPField(isPersistant = true)] public string initialTankSetup;
        [KSPField(isPersistant = true)] public double storedFactorMultiplier = 1;
        [KSPField(isPersistant = true)] public double storedSurfaceMultiplier = 1;
        [KSPField(isPersistant = true)] public double storedVolumeMultiplier = 1;
        [KSPField(isPersistant = true)] public double baseMassMultiplier = 1;
        [KSPField(isPersistant = true)] public double initialMassMultiplier = 1;
        [KSPField(isPersistant = true)] public float windowPositionX = 1200;
        [KSPField(isPersistant = true)] public float windowPositionY = 150;

        // Config properties
        [KSPField] public float windowWidth = 200;
        [KSPField] public string moduleID = "0";
        [KSPField] public string tankId = string.Empty;
        [KSPField] public string resourceGui = string.Empty;
        [KSPField] public string tankSwitchNames = string.Empty;
        [KSPField] public string crewCapacity = string.Empty;
        [KSPField] public string habitatVolume = string.Empty;
        [KSPField] public string habitatSurface = string.Empty;
        [KSPField] public string bannedResourceNames = string.Empty;
        [KSPField] public string switcherDescription = "#LOC_IFS_FuelSwitch_switcherDescription";
        [KSPField] public string resourceNames = "ElectricCharge;LiquidFuel,Oxidizer;MonoPropellant";
        [KSPField] public string resourceAmounts = string.Empty;
        [KSPField] public string resourceRatios = string.Empty;
        [KSPField] public string initialResourceAmounts = string.Empty;
        [KSPField] public bool ignoreInitialCost = false;
        [KSPField] public bool adaptiveTankSelection = false;
        [KSPField] public float basePartMass = 0;
        [KSPField] public double baseResourceMassDivider = 0;
        [KSPField] public string tankResourceMassDivider = string.Empty;
        [KSPField] public string tankResourceMassDividerAddition = string.Empty;
        [KSPField] public bool overrideMassWithTankDividers = false;
        [KSPField] public bool orderBySwitchName = false;
        [KSPField] public string tankMass = "";
        [KSPField] public string tankTechReq = "";
        [KSPField] public string tankCost = "";
        [KSPField] public bool displayTankCost = false;
        [KSPField] public bool displayWetDryMass = true;
        [KSPField] public bool hasSwitchChooseOption = true;
        [KSPField] public bool hasGUI = true;
        [KSPField] public bool availableInFlight;
        [KSPField] public bool availableInEditor = true;
        [KSPField] public bool returnDryMass = false;
        [KSPField] public string inEditorSwitchingTechReq;
        [KSPField] public string inFlightSwitchingTechReq;
        [KSPField] public bool useTextureSwitchModule;
        [KSPField] public bool showTankName = true;
        [KSPField] public bool showInfo = true;    // if false, does not feed info to the part list pop up info menu
        [KSPField] public string moduleInfoTemplate;
        [KSPField] public string moduleInfoParams;
        [KSPField] public string resourcesFormatCompact = "0.000";
        [KSPField] public string resourcesFormat = "0.000000";
        [KSPField] public bool canSwitchWithFullTanks = false;
        [KSPField] public bool allowedToSwitch;
        [KSPField] public bool updateModuleCost = true;
        [KSPField] public bool controlCrewCapacity = false;

        // Gui
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_tankGuiName")] // Tank name
        public string tankGuiName = "";
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_maxWetDryMass")] // Dry/Wet Mass
        public string maxWetDryMass = "";
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_massRatioStr")] // Mass Ratio
        public string massRatioStr = "";
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_crewCapacityStr")] // Crew Capacity
        public string crewCapacityStr = "";

        [KSPField(groupName = Group)] public string resourceAmountStr0 = "";
        [KSPField(groupName = Group)] public string resourceAmountStr1 = "";
        [KSPField(groupName = Group)] public string resourceAmountStr2 = "";
        [KSPField(groupName = Group)] public string resourceAmountStr3 = "";

        // Debug
        [KSPField] public bool debugMode = false;
        [KSPField] public float moduleCost;
        [KSPField] public double dryMass;
        [KSPField] public double initialMass;
        [KSPField] public double moduleMassDelta;
        [KSPField] public float defaultMass;
        [KSPField] public string defaultTank = "";

        [KSPField] public double volumeExponent = 3;
        [KSPField] public double massExponent = 3;
        [KSPField] public double baseMassExponent = 0;
        [KSPField] public double tweakscaleMassExponent = 3;

        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_totalMass", guiUnits = " t", guiFormat = "F6")]			// Total mass
        public double totalMass;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_resourceCost")]      // Resource cost
        public string resourceCostStr;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_totalCost", guiUnits = " V", guiFormat = "F0")]         // Total Tank cost
        public double totalCost;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_switchWindow"), UI_Toggle(disabledText = "#LOC_IFS_FuelSwitch_WindowHidden", enabledText = "#LOC_IFS_FuelSwitch_WindowShown", affectSymCounterparts = UI_Scene.None)] // Mass Ratio  Hidden  Shown
        public bool render_window;

        List<IFSmodularTank> _modularTankList = new List<IFSmodularTank>();
        InterstellarTextureSwitch2 textureSwitch;
        IFSmodularTank selectedTank;
        readonly HashSet<string> activeResourceList = new HashSet<string>();
        Rect windowPosition;

        bool _initialized;
        bool closeAfterSwitch;

        int _numberOfAvailableTanks;
        int _windowId;

        double _partResourceMaxAmountFraction0;
        double _partResourceMaxAmountFraction1;
        double _partResourceMaxAmountFraction2;
        double _partResourceMaxAmountFraction3;

        PartResource _partResource0;
        PartResource _partResource1;
        PartResource _partResource2;
        PartResource _partResource3;

        PartResourceDefinition _partResourceDefinition0;
        PartResourceDefinition _partResourceDefinition1;
        PartResourceDefinition _partResourceDefinition2;
        PartResourceDefinition _partResourceDefinition3;

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
        BaseField volumeField;
        BaseField surfaceField;

        MethodInfo habitatOnStartMethod;

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

                UpdateHabitat(selectedTank);
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
                if (modularTank.resources.Count != part.Resources.Count(r => activeResourceList.Contains(r.resourceName)))
                    isSimilar = false;
                else
                {
                    // check if all tank resources are present
                    foreach (var resource in modularTank.resources)
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

        public override void OnStart(StartState state)
        {
            try
            {
                _crewCapacityField = Fields[nameof(crewCapacityStr)];
                _crewCapacityField.guiActive = controlCrewCapacity;
                _crewCapacityField.guiActiveEditor = controlCrewCapacity;

                _massRatioStrField = Fields[nameof(massRatioStr)];
                _massRatioStrField.guiActive = displayWetDryMass;
                _massRatioStrField.guiActiveEditor = displayWetDryMass;

                _maxWetDryMassField = Fields[nameof(maxWetDryMass)];
                _maxWetDryMassField.guiActive = displayWetDryMass;
                _maxWetDryMassField.guiActiveEditor = displayWetDryMass;

                initialMass = (double)(decimal)part.prefabMass * initialMassMultiplier;

                if (initialMass == 0)
                    initialMass = (double)(decimal)part.prefabMass;

                defaultTank = Localizer.Format(defaultTank);

                _windowId = new System.Random(part.GetInstanceID()).Next(int.MaxValue);

                windowPosition = new Rect(windowPositionX, windowPositionY, windowWidth, 10);

                InitializeData();

                InitializeKerbalismHabitat();

                if (adaptiveTankSelection || selectedTankSetup == -1)
                {
                    if (selectedTankSetup == -1)
                        initialTankSetup = string.Join(";", part.Resources.Select(m => m.resourceName).ToArray());

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

                        selectedTank = desiredTank ?? _modularTankList[0];

                        selectedTankSetupTxt = selectedTank.GuiName;
                    }
                }

                this.enabled = true;

                AssignResourcesToPart();

                _chooseField = Fields["selectedTankSetup"];

                _chooseField.guiName = Localizer.Format(switcherDescription);
                _chooseField.guiActiveEditor = hasSwitchChooseOption && availableInEditor && _modularTankList.Count > 1;
                _chooseField.guiActive = hasSwitchChooseOption && availableInFlight && _modularTankList.Count > 1;

                if (_chooseField.uiControlEditor is UI_ChooseOption chooseOptionEditor)
                {
                    chooseOptionEditor.options = _modularTankList.Select(s => s.SwitchName).ToArray();
                    chooseOptionEditor.onFieldChanged = UpdateFromGUI;
                }

                if (_chooseField.uiControlFlight is UI_ChooseOption chooseOptionFlight)
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
                        NextTankSetupEvent();
                    else
                        PreviousTankSetupEvent();
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
                    if (int.TryParse(newTankName, out var index))
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

                _field0 = Fields[nameof(resourceAmountStr0)];
                _field1 = Fields[nameof(resourceAmountStr1)];
                _field2 = Fields[nameof(resourceAmountStr2)];
                _field3 = Fields[nameof(resourceAmountStr3)];
                _tankGuiNameField = Fields[nameof(tankGuiName)];

                SetupTankList();

                if (HighLogic.LoadedSceneIsGame)
                {
                    foreach (var modularTank in _modularTankList)
                    {
                        modularTank.hasTech = HasTech(modularTank.techReq);
                    }
                    _numberOfAvailableTanks = _modularTankList.Count(m => m.hasTech);
                }

                availableInEditor = string.IsNullOrEmpty(inEditorSwitchingTechReq) ? availableInEditor : HasTech(inEditorSwitchingTechReq);
                availableInFlight = string.IsNullOrEmpty(inFlightSwitchingTechReq) ? availableInFlight : HasTech(inFlightSwitchingTechReq);

                _nextTankSetupEvent = Events[nameof(NextTankSetupEvent)];
                _nextTankSetupEvent.guiActive = hasGUI && availableInFlight;

                _previousTankSetupEvent = Events[nameof(PreviousTankSetupEvent)];
                _previousTankSetupEvent.guiActive = hasGUI && availableInFlight;

                Fields[nameof(resourceCostStr)].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;
                Fields[nameof(totalCost)].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;

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
                if (part.partInfo != null)
                    Debug.LogError("[IFS]: InterstellarFuelSwitch.InitializeData Error with " + part.partInfo.name + " '" +  part.partInfo.title + "' : " + e.Message);
                else
                    Debug.LogError("[IFS]: InterstellarFuelSwitch.InitializeData Error with " + e.Message);

                throw;
            }
        }

        [KSPEvent(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_switchTank")]//Switch Tank
        public void SwitchTankEvent()
        {
            closeAfterSwitch = true;
            render_window = true;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_nextTankSetupText")]
        public void NextTankSetupEvent()
        {
            try
            {
                selectedTankSetup++;

                if (selectedTankSetup >= _modularTankList.Count)
                    selectedTankSetup = 0;

                var currentTank = _modularTankList[selectedTankSetup];

                if (!currentTank.hasTech || (controlCrewCapacity && currentTank.crewCapacity < part.protoModuleCrew.Count))
                    NextTankSetupEvent();

                AssignResourcesToPart(true, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: nextTankSetupEvent Error: " + e.Message);
                throw;
            }
        }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_previousTankSetupText")]
        public void PreviousTankSetupEvent()
        {
            try
            {
                selectedTankSetup--;

                if (selectedTankSetup < 0)
                    selectedTankSetup = _modularTankList.Count - 1;

                if (!_modularTankList[selectedTankSetup].hasTech)
                    PreviousTankSetupEvent();

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

                if (!HighLogic.LoadedSceneIsEditor || !affectSymCounterparts) return;

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

            var tankGuiNameIsNotEmpty = !string.IsNullOrEmpty(tankGuiName);
            _tankGuiNameField.guiActive = showTankName && tankGuiNameIsNotEmpty;
            _tankGuiNameField.guiActiveEditor = showTankName && tankGuiNameIsNotEmpty;
        }

        private List<string> SetupTankInPart(Part currentPart, bool calledByPlayer)
        {
            FindSelectedTank(calledByPlayer);

            // update txt and index for future
            selectedTankSetupTxt = selectedTank.GuiName;
            selectedTankSetup = _modularTankList.IndexOf(selectedTank);

            // create new ResourceNode
            var newResources = new List<string>(8);
            var newResourceNodes = new List<ConfigNode>(8);
            var parsedConfigAmount = new List<double>(8);
            var parsedConfigFlowStates = new List<bool>(8);

            // parse configured amounts
            if (configuredAmounts.Length > 0)
            {
                // empty configuration if switched by user
                if (calledByPlayer)
                    configuredAmounts = string.Empty;

                var configAmounts = configuredAmounts.Split(',');
                foreach (var item in configAmounts)
                {
                    if (double.TryParse(item, out var value))
                        parsedConfigAmount.Add(value);
                }

                // empty configuration if in flight
                if (!HighLogic.LoadedSceneIsEditor)
                    configuredAmounts = string.Empty;
            }

            if (configuredFlowStates.Length > 0)
            {
                // empty configuration if switched by user
                if (calledByPlayer)
                    configuredFlowStates = string.Empty;

                var configFlowStates = configuredFlowStates.Split(',');
                foreach (var item in configFlowStates)
                {
                    if (bool.TryParse(item, out bool value))
                        parsedConfigFlowStates.Add(value);
                }

                // empty configuration if in flight
                if (!HighLogic.LoadedSceneIsEditor)
                    configuredFlowStates = string.Empty;
            }

            UpdateHabitat(selectedTank);

            for (var resourceId = 0; resourceId < selectedTank.resources.Count; resourceId++)
            {
                var selectedTankResource = selectedTank.resources[resourceId];

                if (selectedTankResource.name == "Structural")
                    continue;

                newResources.Add(selectedTankResource.name);

                var newResourceNode = new ConfigNode("RESOURCE");
                var maxAmount = selectedTankResource.maxAmount * storedVolumeMultiplier;

                newResourceNode.AddValue("name", selectedTankResource.name);
                newResourceNode.AddValue("maxAmount", maxAmount);

                var existingResource = currentPart.Resources[selectedTankResource.name];

                double resourceNodeAmount;

                if (existingResource != null)
                    resourceNodeAmount = Math.Min(existingResource.amount * maxAmount / existingResource.maxAmount, maxAmount);
                else if (!HighLogic.LoadedSceneIsEditor && resourceId < parsedConfigAmount.Count)
                    resourceNodeAmount = parsedConfigAmount[resourceId];
                else if (!HighLogic.LoadedSceneIsEditor && calledByPlayer)
                    resourceNodeAmount = 0.0;
                else
                    resourceNodeAmount = selectedTank.resources[resourceId].amount * storedVolumeMultiplier;

                newResourceNode.AddValue("amount", resourceNodeAmount);

                if (existingResource != null)
                    newResourceNode.AddValue("flowState", existingResource.flowState);
                else if (resourceId < parsedConfigFlowStates.Count)
                    newResourceNode.AddValue("flowState", parsedConfigFlowStates[resourceId]);

                newResourceNodes.Add(newResourceNode);
            }

            foreach (var resource in currentPart.Resources)
            {
                if (activeResourceList.Contains(resource.resourceName)) continue;

                var newResourceNode = new ConfigNode("RESOURCE");
                newResourceNode.AddValue("name", resource.resourceName);
                newResourceNode.AddValue("maxAmount", resource.maxAmount);
                newResourceNode.AddValue("amount", resource.amount);
                newResourceNode.AddValue("flowState", resource.flowState);
                newResourceNode.AddValue("flowMode", resource.flowMode);

                newResourceNodes.Add(newResourceNode);
            }

            currentPart.Resources.Clear();
            currentPart.SetupSimulationResources();
            GameEvents.onPartResourceListChange.Fire(currentPart);

            // add new or existing resources
            foreach (var resourceNode in newResourceNodes)
            {
                currentPart.AddResource(resourceNode);
            }

            UpdateCost();

            UpdatePartActionWindow();

            return newResources;
        }

        private void UpdatePartActionWindow()
        {
            var window = FindObjectsOfType<UIPartActionWindow>().FirstOrDefault(w => w.part == part);
            if (window == null) return;

            foreach (UIPartActionWindow actionWindow in FindObjectsOfType<UIPartActionWindow>())
            {
                if (window.part != part) continue;
                actionWindow.ClearList();
                actionWindow.displayDirty = true;
            }
        }

        private void FindSelectedTank(bool calledByPlayer)
        {
            // first find selected tank on index
            selectedTank = calledByPlayer && selectedTankSetup >= 0 && selectedTankSetup < _modularTankList.Count ? _modularTankList[selectedTankSetup] : null;

            // find based on GuiName, SwitchName or contents
            if (selectedTank == null)
            {
                var matchingIndex = FindMatchingConfig();
                if (matchingIndex >= 0)
                    selectedTank = _modularTankList[matchingIndex];
            }

            // if still no tank found create a tank based on current tank contents
            if (selectedTank == null && (HighLogic.LoadedSceneIsFlight || _modularTankList.Count == 0) && part.Resources.Any(m => m.info.density > 0))
            {
                var resourcesWithMass = part.Resources.Where(m => m.info.density > 0).ToList();

                var concatenatedGuiName = string.Join("+", resourcesWithMass.Select(r => r.info.displayName).ToArray());

                Debug.LogWarning("[IFS]: Constructing new tank definition for " + part.name + " with name " + concatenatedGuiName);

                var ifsResources = resourcesWithMass.Select(r => new IFSresource(r.resourceName)
                {
                    amount = r.amount / storedVolumeMultiplier,
                    maxAmount = r.maxAmount / storedVolumeMultiplier
                }).ToList();

                var concatenatedSwitchName = string.Join("+", resourcesWithMass.Select(r => r.info.abbreviation).ToArray());

                selectedTank = new IFSmodularTank
                {
                    SwitchName = concatenatedSwitchName,
                    Composition = concatenatedSwitchName,
                    GuiName = concatenatedGuiName,
                    resources = ifsResources
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

        // only called after tank switching
        public void ConfigureResourceMassGui(List<string> newResources)
        {
            _partResourceDefinition0 = newResources.Count > 0 ? PartResourceLibrary.Instance.GetDefinition(newResources[0]) : null;
            _partResourceDefinition1 = newResources.Count > 1 ? PartResourceLibrary.Instance.GetDefinition(newResources[1]) : null;
            _partResourceDefinition2 = newResources.Count > 2 ? PartResourceLibrary.Instance.GetDefinition(newResources[2]) : null;
            _partResourceDefinition3 = newResources.Count > 3 ? PartResourceLibrary.Instance.GetDefinition(newResources[3]) : null;

            _field0.guiName = _partResourceDefinition0 != null ? _partResourceDefinition0.name : ":";
            _field1.guiName = _partResourceDefinition1 != null ? _partResourceDefinition1.name : ":";
            _field2.guiName = _partResourceDefinition2 != null ? _partResourceDefinition2.name : ":";
            _field3.guiName = _partResourceDefinition3 != null ? _partResourceDefinition3.name : ":";

            _field0.guiActive = _partResourceDefinition0 != null && _partResourceDefinition0.isVisible;
            _field1.guiActive = _partResourceDefinition1 != null && _partResourceDefinition1.isVisible;
            _field2.guiActive = _partResourceDefinition2 != null && _partResourceDefinition2.isVisible;
            _field3.guiActive = _partResourceDefinition3 != null && _partResourceDefinition3.isVisible;

            _field0.guiActiveEditor = _partResourceDefinition0 != null && _partResourceDefinition0.isVisible;
            _field1.guiActiveEditor = _partResourceDefinition1 != null && _partResourceDefinition1.isVisible;
            _field2.guiActiveEditor = _partResourceDefinition2 != null && _partResourceDefinition2.isVisible;
            _field3.guiActiveEditor = _partResourceDefinition3 != null && _partResourceDefinition3.isVisible;

            _partResource0 = _partResourceDefinition0 == null ? null : part.Resources[newResources[0]];
            _partResource1 = _partResourceDefinition1 == null ? null : part.Resources[newResources[1]];
            _partResource2 = _partResourceDefinition2 == null ? null : part.Resources[newResources[2]];
            _partResource3 = _partResourceDefinition3 == null ? null : part.Resources[newResources[3]];

            _partResourceMaxAmountFraction0 = _partResource0?.maxAmount * 0.001 ?? 0;
            _partResourceMaxAmountFraction1 = _partResource1?.maxAmount * 0.001 ?? 0;
            _partResourceMaxAmountFraction2 = _partResource2?.maxAmount * 0.001 ?? 0;
            _partResourceMaxAmountFraction3 = _partResource3?.maxAmount * 0.001 ?? 0;
        }

        private double UpdateCost()
        {
            double dryCost = part.partInfo.cost * initialMassMultiplier;
            double resourceCost = 0.0, maxResourceCost = 0.0, delta;

            if (selectedTankSetup >= 0 && selectedTankSetup < _modularTankList.Count)
                dryCost += _modularTankList[selectedTankSetup].tankCost * initialMassMultiplier;

            bool preserveInitialCost = !ignoreInitialCost && !string.IsNullOrEmpty(initialTankSetup);
            bool isSmaller = storedFactorMultiplier < 0.999, isLarger = storedFactorMultiplier > 1.001;
            if (preserveInitialCost)
            {
                string[] initialTanks = initialTankSetup.Split(';');

                foreach (var resourceName in initialTanks)
                {
                    if (part.Resources.Contains(resourceName)) continue;
                    preserveInitialCost = false;
                    break;
                }
            }

            if (_partResourceDefinition0 != null && _partResource0 != null)
            {
                double unitCost0 = _partResourceDefinition0.unitCost;
                resourceCost += unitCost0 * _partResource0.amount;
                maxResourceCost += unitCost0 * _partResource0.maxAmount;
            }

            if (_partResourceDefinition1 != null && _partResource1 != null)
            {
                double unitCost1 = _partResourceDefinition1.unitCost;
                resourceCost += unitCost1 * _partResource1.amount;
                maxResourceCost += unitCost1 * _partResource1.maxAmount;
            }

            if (_partResourceDefinition2 != null && _partResource2 != null)
            {
                double unitCost2 = _partResourceDefinition2.unitCost;
                resourceCost += unitCost2 * _partResource2.amount;
                maxResourceCost += unitCost2 * _partResource2.maxAmount;
            }

            if (_partResourceDefinition3 != null && _partResource3 != null)
            {
                double unitCost3 = _partResourceDefinition3.unitCost;
                resourceCost += unitCost3 * _partResource3.amount;
                maxResourceCost += unitCost3 * _partResource3.maxAmount;
            }

            if (preserveInitialCost)
            {
                totalCost = dryCost - maxResourceCost + resourceCost;
                delta = 0.0;
            }
            else
            {
                totalCost = dryCost + resourceCost;
                delta = !isSmaller && !isLarger ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
            }
            resourceCostStr = $"{resourceCost:F0} V / {maxResourceCost:F0} V";
            return delta;
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

        private string FormatMassStrCompact(double amount)
        {
            if (amount >= 1)
                return (amount).ToString(resourcesFormatCompact) + " t";
            if (amount >= 1e-3)
                return (amount * 1e3).ToString(resourcesFormatCompact) + " kg";
            if (amount >= 1e-6)
                return (amount * 1e6).ToString(resourcesFormatCompact) + " g";
            else
                return (amount * 1e9).ToString(resourcesFormatCompact) + " mg";
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
            var missing0 = _partResourceDefinition0 == null || _partResource0 == null;
            var missing1 = _partResourceDefinition1 == null || _partResource1 == null;
            var missing2 = _partResourceDefinition2 == null || _partResource2 == null;
            var missing3 = _partResourceDefinition3 == null || _partResource3 == null;

            if (_massRatioStrField != null)
            {
                _massRatioStrField.guiActive = displayWetDryMass && !missing0;
                _massRatioStrField.guiActiveEditor = displayWetDryMass && !missing0;
            }

            if (_maxWetDryMassField != null)
            {
                _maxWetDryMassField.guiActive = displayWetDryMass && !missing0;
                _maxWetDryMassField.guiActiveEditor = displayWetDryMass && !missing0;
            }

            var currentResourceMassAmount0 = missing0 ? 0 : (double)(decimal)_partResourceDefinition0.density * _partResource0.amount;
            var currentResourceMassAmount1 = missing1 ? 0 : (double)(decimal)_partResourceDefinition1.density * _partResource1.amount;
            var currentResourceMassAmount2 = missing2 ? 0 : (double)(decimal)_partResourceDefinition2.density * _partResource2.amount;
            var currentResourceMassAmount3 = missing3 ? 0 : (double)(decimal)_partResourceDefinition3.density * _partResource3.amount;

            totalMass = dryMass + currentResourceMassAmount0 + currentResourceMassAmount1 + currentResourceMassAmount2 + currentResourceMassAmount3;

            resourceAmountStr0 = missing0 ? string.Empty : FormatMassStr(currentResourceMassAmount0);
            resourceAmountStr1 = missing1 ? string.Empty : FormatMassStr(currentResourceMassAmount1);
            resourceAmountStr2 = missing2 ? string.Empty : FormatMassStr(currentResourceMassAmount2);
            resourceAmountStr3 = missing3 ? string.Empty : FormatMassStr(currentResourceMassAmount3);
        }

        private void UpdateMassRatio()
        {
            var maxResourceMassAmount0 = _partResourceDefinition0 == null || _partResource0 == null ? 0 : (double)(decimal)_partResourceDefinition0.density * _partResource0.maxAmount;
            var maxResourceMassAmount1 = _partResourceDefinition1 == null || _partResource1 == null ? 0 : (double)(decimal)_partResourceDefinition1.density * _partResource1.maxAmount;
            var maxResourceMassAmount2 = _partResourceDefinition2 == null || _partResource2 == null ? 0 : (double)(decimal)_partResourceDefinition2.density * _partResource2.maxAmount;
            var maxResourceMassAmount3 = _partResourceDefinition3 == null || _partResource3 == null ? 0 : (double)(decimal)_partResourceDefinition3.density * _partResource3.maxAmount;

            if (!displayWetDryMass) return;

            var wetMass = maxResourceMassAmount0 + maxResourceMassAmount1 + maxResourceMassAmount2 + maxResourceMassAmount3;

            if (wetMass > 0 && dryMass > 0)
                massRatioStr = ToRoundedString(1 / (dryMass / wetMass));

            maxWetDryMass = $"{FormatMassStrCompact(dryMass)} / {FormatMassStrCompact(wetMass)}";
            crewCapacityStr = $"{part.protoModuleCrew.Count} / {part.CrewCapacity}";
        }

        private string ToRoundedString(double value)
        {
            var differenceWithRounded = Math.Abs(value - Math.Round(value, 0));

            if (differenceWithRounded > 0.05)
                return "1 : " + value.ToString("0.0");
            if (differenceWithRounded > 0.005)
                return "1 : " + value.ToString("0.00");
            if (differenceWithRounded > 0.0005)
                return "1 : " + value.ToString("0.000");

            return "1 : " + value.ToString("0");
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
            }
            else
            {
                // update Dry Mass
                UpdateDryMass();
                UpdateGuiResourceMass();
                UpdateCost();

                configuredAmounts = string.Empty;
                configuredFlowStates = string.Empty;

                foreach (var resource in part.Resources)
                {
                    configuredAmounts += resource.amount + ",";
                    configuredFlowStates += resource.flowState + ",";
                }
            }
        }

        private void SetupTankList()
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

            var resourceTankAbsoluteAmountArray = resourceAmounts.Split(';');
            var resourceTankRatioAmountArray = resourceRatios.Split(';');
            var tankNameArray = resourceNames.Split(';');
            var tankTechReqArray = tankTechReq.Split(';');
            var tankGuiNameArray = resourceGui.Split(';');
            var tankSwitcherNameArray = tankSwitchNames.Split(';');

            // if initial resource amount is missing or not complete, use full amount
            string[] initialResourceTankArray = string.IsNullOrEmpty(initialResourceAmounts)
                ? resourceTankAbsoluteAmountArray
                : initialResourceAmounts.Split(';');

            var maxLengthTankArray = Math.Max(resourceTankAbsoluteAmountArray.Length, resourceTankRatioAmountArray.Length);

            for (var tankCounter = 0; tankCounter < maxLengthTankArray; tankCounter++)
            {
                resourceList.Add(new List<double>());
                initialResourceList.Add(new List<double>());

                var resourceMaxAmountArray = resourceTankAbsoluteAmountArray[tankCounter].Trim().Split(',');
                var initialResourceAmountArray = tankCounter <  initialResourceTankArray.Length
                    ? initialResourceTankArray[tankCounter].Trim().Split(',')
                    : resourceTankAbsoluteAmountArray[tankCounter].Trim().Split(',');

                // if missing or not complete, use full amount
                if (string.IsNullOrEmpty(initialResourceAmounts) || initialResourceAmountArray.Length != resourceMaxAmountArray.Length)
                    initialResourceAmountArray = resourceMaxAmountArray;

                for (var amountCounter = 0; amountCounter < resourceMaxAmountArray.Length; amountCounter++)
                {
                    if (tankCounter >= resourceList.Count || amountCounter >= resourceMaxAmountArray.Length) continue;

                    resourceList[tankCounter].Add(ParseTools.ParseDouble(resourceMaxAmountArray[amountCounter]));

                    if (tankCounter < initialResourceList.Count && amountCounter < initialResourceAmountArray.Length)
                        initialResourceList[tankCounter].Add(ParseTools.ParseDouble(initialResourceAmountArray[amountCounter]));
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

                    modularTank.resources.Add(newResource);
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

                    var names = modularTank.resources.Select(m => m.name);
                    modularTank.GuiName = string.Empty;
                    foreach (var modularTankGuiName in names)
                    {
                        if (!string.IsNullOrEmpty(modularTank.GuiName))
                            modularTank.GuiName += "+";
                        modularTank.GuiName += modularTankGuiName;
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
                    description += string.Join(",", config.resources.Select(m => m.name).ToArray());
                    Debug.Log(description);
                }
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            moduleCost = updateModuleCost ? (float)UpdateCost() : 0.0f;

            return moduleCost;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return HighLogic.LoadedSceneIsFlight ? ModifierChangeWhen.STAGED : ModifierChangeWhen.CONSTANTLY;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return HighLogic.LoadedSceneIsFlight ? ModifierChangeWhen.STAGED : ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleMass(float mass, ModifierStagingSituation sit)
        {
            defaultMass = mass;

            if (returnDryMass)
                return (float) dryMass;

            UpdateDryMass();

            moduleMassDelta = dryMass - initialMass;

            return (float) moduleMassDelta;
        }

        public override string GetInfo()
        {
            if (!showInfo) return string.Empty;

            var info = StringBuilderCache.Acquire();

            if (!string.IsNullOrEmpty(moduleInfoTemplate))
            {
                string[] parameters;

                if (!string.IsNullOrEmpty(moduleInfoParams))
                {
                    parameters = moduleInfoParams.Split(';');

                    // translate parameters
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        parameters[i] = Localizer.Format(parameters[i]);
                    }
                }
                else
                    parameters = new string[0];

                foreach (string line in moduleInfoTemplate.Split(LineBreaks, StringSplitOptions.None))
                {
                    info.AppendLine(Localizer.Format(line, parameters));
                }
            }
            else
            {
                info.Append(Localizer.Format("#LOC_IFS_FuelSwitch_GetInfo")).AppendLine(":");
                info.AppendLine("<size=10>");

                foreach (var module in _modularTankList)
                {
                    bool multi = module.resources.Count > 1;

                    if (multi)
                    {
                        info.Append("<color=#00ff00ff>");
                        info.Append(module.SwitchName);
                        info.AppendLine("</color>");
                    }

                    foreach (var resource in module.resources)
                    {
                        if (multi)
                            info.Append("* ");

                        info.Append(resource.maxAmount.ToString("F0"));
                        info.Append(" <color=#00ffffff>");
                        info.Append(resource.name);
                        info.AppendLine("</color>");
                    }
                }
                info.Append("</size>");
            }
            return info.ToStringAndRelease();
        }

        private static bool HasTech(string techId)
        {
            if (string.IsNullOrEmpty(techId))
                return true;

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return true;

            if ((HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX))
                return true;

            if (ResearchAndDevelopment.Instance == null)
            {
                if (_researchedTechs == null)
                    LoadSaveFile();

                return _researchedTechs != null && _researchedTechs.Contains(techId);
            }

            return ResearchAndDevelopment.GetTechnologyState(techId) == RDTech.State.Available;
        }

        private static void LoadSaveFile()
        {
            _researchedTechs = new HashSet<string>();

            var persistentFile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
            var config = ConfigNode.Load(persistentFile);
            var configNode = config.GetNode("GAME");
            var scenarios = configNode.GetNodes("SCENARIO");

            foreach (var scenario in scenarios)
            {
                if (scenario.GetValue("name") != "ResearchAndDevelopment") continue;

                var techs = scenario.GetNodes("Tech");
                foreach (var techNode in techs)
                {
                    var techNodeName = techNode.GetValue("id");
                    _researchedTechs.Add(techNodeName);
                }
            }
        }

        public void OnGUI()
        {
            if (vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(_windowId, windowPosition, Window, part.partInfo.title);
        }

        private void Window(int windowId)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                {
                    closeAfterSwitch = false;
                    render_window = false;
                }

                GUILayout.BeginVertical();

                foreach (var tank in _modularTankList)
                {
                    if (!tank.hasTech) continue;

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(tank.GuiName, GUILayout.ExpandWidth(true)))
                    {
                        selectedTankSetup = _modularTankList.IndexOf(tank);
                        AssignResourcesToPart(true, true);
                        _fuelTankSetupControl?.SwitchToFuelTankSetup(tank.SwitchName);
                        if (closeAfterSwitch)
                        {
                            closeAfterSwitch = false;
                            render_window = false;
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUI.DragWindow();

            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: InterstellarFuelSwitch Window(" + windowId + "): " + e.Message);
                throw;
            }
        }

        private void InitializeKerbalismHabitat()
        {
            foreach (PartModule module in part.Modules)
            {
                if (module.moduleName != "Habitat") continue;

                habitatModule = module;
                habitatVolumeField = module.Fields["volume"];
                habitatSurfaceField = module.Fields["surface"];
                habitatStateField = module.Fields["state"];
                habitatToggleField = module.Fields["toggle"];
                habitatOnStartMethod = module.GetType().GetMethod("OnStart");
                volumeField = habitatModule.Fields["Volume"];
                surfaceField = habitatModule.Fields["Surface"];

                if (habitatOnStartMethod != null)
                    Debug.Log("[IFS]: Found onStartMethod");

                break;
            }
        }

        private void UpdateHabitat(IFSmodularTank currentTank)
        {
            if (currentTank == null)
                return;

            if (controlCrewCapacity)
            {
                var newCrewCapacity = (int)Math.Round(selectedTank.crewCapacity * storedSurfaceMultiplier);
                Debug.Log("[IFS]: SetupTankInPart Set CrewCapacity : " + newCrewCapacity);
                part.CrewCapacity = newCrewCapacity;
                part.crewTransferAvailable = newCrewCapacity > 0;
            }

            double volume = currentTank.habitatVolume;
            double surface = currentTank.habitatSurface;

            try
            {
                if (habitatModule == null)
                    return;

                habitatVolumeField?.SetValue(volume * storedVolumeMultiplier, habitatModule);
                habitatSurfaceField?.SetValue(surface * storedSurfaceMultiplier, habitatModule);
                habitatToggleField?.SetValue(volume > 0, habitatModule);

                if (habitatStateField != null)
                {
                    var newState = volume > 0 ? State.enabled : State.disabled;
                    habitatStateField.SetValue((int)newState, habitatModule);
                }

                var atmosphere = part.Resources["Atmosphere"];
                if (atmosphere != null)
                {
                    atmosphere.amount = volume * 1e3 * storedVolumeMultiplier;
                    atmosphere.maxAmount = volume * 1e3 * storedVolumeMultiplier;
                }

                var wasteAtmosphere = part.Resources["WasteAtmosphere"];
                if (wasteAtmosphere != null)
                {
                    wasteAtmosphere.amount = 0;
                    wasteAtmosphere.maxAmount = volume * 1e3 * storedVolumeMultiplier;
                }

                var moistAtmosphere = part.Resources["MoistAtmosphere"];
                if (moistAtmosphere != null)
                {
                    moistAtmosphere.amount = 0;
                    moistAtmosphere.maxAmount = volume * 1e3 * storedVolumeMultiplier;
                }

                if (habitatOnStartMethod != null)
                {
                    habitatOnStartMethod.Invoke(habitatModule, new object[] { (int)StartState.None });
                }

                if (volumeField != null)
                {
                    volumeField.guiActive = volume > 0;
                    volumeField.guiActiveEditor = volume > 0;
                }

                if (surfaceField != null)
                {
                    surfaceField.guiActive = surface > 0;
                    surfaceField.guiActiveEditor = surface > 0;
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[IFS]: UpdateKerbalismHabitat "+ e.Message);
            }
        }
    }
}
