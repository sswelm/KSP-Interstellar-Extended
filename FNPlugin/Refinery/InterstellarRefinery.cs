using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Powermanagement;
using FNPlugin.Refinery.Activity;
using FNPlugin.Resources;
using KSP.Localization;
using UnityEngine;

namespace FNPlugin.Refinery
{
    [KSPModule("Nuclear ISRU Refinery")]
    class NuclearRefineryController : InterstellarRefineryController { }

    [KSPModule("ISRU Refinery")]
    public class InterstellarRefineryController : PartModule
    {
        public const string Group = "Refinery";
        public const string GroupTitle = "Refinery";

        // isPersistent
        [KSPField(isPersistant = true)] protected bool refinery_is_enabled;
        [KSPField(isPersistant = true)] protected bool lastOverflowSettings;
        [KSPField(isPersistant = true)] protected double lastActiveTime;
        [KSPField(isPersistant = true)] protected double lastPowerRatio;
        [KSPField(isPersistant = true)] protected string lastClassName = "";

        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActive = true, isPersistant = true, guiName = "#LOC_KSPIE_Refinery_Current")]//Current
        protected string lastActivityName = "";
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActive = true, isPersistant = true, guiName = "#LOC_KSPIE_Refinery_ToggleRefineryWindow"), UI_Toggle(disabledText = "hidden", enabledText = "shown")]
        public bool showWindow;
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActive = true, isPersistant = true, guiName = "#LOC_KSPIE_Refinery_PowerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]//Power Control
        public float powerPercentage = 100;

        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_RefineryType")] public int refineryType = 0;
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_ProductionMultiplier", guiFormat = "F3")] public double productionMult = 1;
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_PowerReqMultiplier", guiFormat = "F3")] public double powerReqMult = 1;
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_BaseProduction", guiFormat = "F3")] public double baseProduction = 1;

        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_Refinery_Status")] public string status_str = string.Empty;
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_Refinery_PowerRequirement", guiFormat = "F3", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")] public double currentPowerReq;
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_Refinery_ConsumedPower", guiFormat = "F3", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")] public double consumedPowerMW;
        [KSPField(groupDisplayName = GroupTitle, groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_Refinery_PowerAvailable", guiUnits = "%", guiFormat = "F3")] public double utilisationPercentage;

        protected IRefineryActivity currentActivity;
        protected IPowerSupply powerSupply;

        private List<IRefineryActivity> availableRefineries;
        private Rect _windowPosition = new Rect(50, 50, RefineryActivity.labelWidth * 4, 150);

        private GUIStyle _boldLabel;
        private GUIStyle _valueLabel;
        private GUIStyle _enabledButton;
        private GUIStyle _disabledButton;

        private int _windowId;
        private bool _overflowAllowed;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var interstellarPowerSupply = part.FindModuleImplementing<InterstellarPowerSupply>();
            if (interstellarPowerSupply != null)
            {
                interstellarPowerSupply.Fields[nameof(InterstellarPowerSupply.totalPowerSupply)].Attribute.groupName = Group;
                interstellarPowerSupply.Fields[nameof(InterstellarPowerSupply.totalPowerSupply)].Attribute.groupDisplayName = GroupTitle;

                interstellarPowerSupply.Fields[nameof(InterstellarPowerSupply.displayName)].Attribute.groupName = Group;
                interstellarPowerSupply.Fields[nameof(InterstellarPowerSupply.displayName)].Attribute.groupDisplayName = GroupTitle;

                interstellarPowerSupply.Fields[nameof(InterstellarPowerSupply.powerPriority)].Attribute.groupName = Group;
                interstellarPowerSupply.Fields[nameof(InterstellarPowerSupply.powerPriority)].Attribute.groupDisplayName = GroupTitle;
            }
        }

        public override void OnStart(StartState state)
        {
            powerSupply = part.FindModuleImplementing<IPowerSupply>();

            if (powerSupply != null)
                powerSupply.DisplayName = Localizer.Format("#LOC_KSPIE_Refinery_started"); //"started"

            if (state == StartState.Editor) return;

            // load stored overflow setting
            _overflowAllowed = lastOverflowSettings;

            _windowId = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);

            var refineriesList = part.FindModulesImplementing<IRefineryActivity>().ToList();

            if (refineryType > 0)
            {
                AddIfMissing(refineriesList, new AluminiumElectrolyzer());
                AddIfMissing(refineriesList, new AmmoniaElectrolyzer());
                AddIfMissing(refineriesList, new AnthraquinoneProcessor());
                AddIfMissing(refineriesList, new AtmosphereProcessor());
                AddIfMissing(refineriesList, new CarbonDioxideElectrolyzer());
                AddIfMissing(refineriesList, new HaberProcess());
                AddIfMissing(refineriesList, new HeavyWaterElectrolyzer());
                AddIfMissing(refineriesList, new PartialMethaneOxidation());
                AddIfMissing(refineriesList, new PeroxideProcess());
                AddIfMissing(refineriesList, new UF4Ammonolysiser());
                AddIfMissing(refineriesList, new RegolithProcessor());
                AddIfMissing(refineriesList, new ReverseWaterGasShift());
                AddIfMissing(refineriesList, new NuclearFuelReprocessor());
                AddIfMissing(refineriesList, new SabatierReactor());
                AddIfMissing(refineriesList, new OceanProcessor());
                AddIfMissing(refineriesList, new SolarWindProcessor());
                AddIfMissing(refineriesList, new WaterElectrolyzer());
                AddIfMissing(refineriesList, new WaterGasShift());

                availableRefineries = refineriesList
                    .Where(m => ((int) m.RefineryType & refineryType) == (int) m.RefineryType)
                    .OrderBy(a => a.ActivityName).ToList();
            }
            else
                availableRefineries = refineriesList.OrderBy(a => a.ActivityName).ToList();

            // initialize refineries
            foreach (var availableRefinery in availableRefineries)
            {
                try
                {
                    availableRefinery.Initialize(part, this);
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: Failed to initialize " + availableRefinery.ActivityName + " with exception: " + e.Message);
                }
            }

            // load same
            if (refinery_is_enabled && !string.IsNullOrEmpty(lastActivityName))
            {
                Debug.Log("[KSPI]: ISRU Refinery looking to restart " + lastActivityName);
                currentActivity = availableRefineries.FirstOrDefault(a => a.ActivityName == lastActivityName);

                if (currentActivity == null)
                {
                    Debug.Log("[KSPI]: ISRU Refinery looking to restart " + lastClassName);
                    currentActivity = availableRefineries.FirstOrDefault(a => a.GetType().Name == lastClassName);
                }
            }

            if (currentActivity != null)
            {
                bool hasRequirement = currentActivity.HasActivityRequirements();
                lastActivityName = currentActivity.ActivityName;

                Debug.Log("[KSPI]: ISRU Refinery initializing " + lastActivityName + " for which hasRequirement: " + hasRequirement);

                var timeDifference = (Planetarium.GetUniversalTime() - lastActiveTime);

                if (timeDifference > 0.01)
                {
                    string message = Localizer.Format("#LOC_KSPIE_Refinery_Postmsg1", lastActivityName, timeDifference.ToString("0")); //"IRSU performed " +  + " for " +  + " seconds"
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 20, ScreenMessageStyle.LOWER_CENTER);
                }

                var productionModifier = productionMult * baseProduction;
                if (lastActivityName == "Atmospheric Extraction")
                    ((AtmosphereProcessor) currentActivity).ExtractAir(lastPowerRatio * productionModifier,
                        lastPowerRatio, productionModifier, lastOverflowSettings, timeDifference, true);
                else if (lastActivityName == "Seawater Extraction")
                    ((OceanProcessor) currentActivity).ExtractSeawater(lastPowerRatio * productionModifier,
                        lastPowerRatio, productionModifier, lastOverflowSettings, timeDifference, true);
                else
                    currentActivity.UpdateFrame(lastPowerRatio * productionModifier, lastPowerRatio,
                        productionModifier, lastOverflowSettings, timeDifference, true);
            }
        }

        private void AddIfMissing(List<IRefineryActivity> list, IRefineryActivity refinery)
        {
            if (list.All(m => m.ActivityName != refinery.ActivityName))
                list.Add(refinery);
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            if (powerSupply == null)
                return;

            if (currentActivity == null)
                powerSupply.DisplayName = part.partInfo.title;
            else
                powerSupply.DisplayName = part.partInfo.title + " (" + currentActivity.ActivityName + ")";
        }

        public override void OnUpdate()
        {
            status_str = currentActivity == null ? Localizer.Format("#LOC_KSPIE_Refinery_Offline") : currentActivity.Status;
        }

        public void FixedUpdate()
        {
            currentPowerReq = 0;

            if (!HighLogic.LoadedSceneIsFlight || !refinery_is_enabled || currentActivity == null)
            {
                lastActivityName = string.Empty;
                return;
            }

            currentPowerReq = powerReqMult * currentActivity.PowerRequirements * baseProduction;

            var requestedPowerRatio = powerPercentage / 100;

            var powerRequest = currentPowerReq * requestedPowerRatio;

            consumedPowerMW = CheatOptions.InfiniteElectricity
                ? powerRequest
                : powerSupply.ConsumeMegajoulesPerSecond(powerRequest);

            var shortage = Math.Max(powerRequest - consumedPowerMW, 0);

            var fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);

            var receivedElectricCharge = part.RequestResource(ResourceSettings.Config.ElectricPowerInKilowatt, shortage *
                GameConstants.ecPerMJ * fixedDeltaTime) / fixedDeltaTime;

            consumedPowerMW += receivedElectricCharge / GameConstants.ecPerMJ;

            var receivedPowerRatio = currentPowerReq > 0 ? consumedPowerMW / currentPowerReq : 0;

            utilisationPercentage = receivedPowerRatio * 100;

            var productionModifier = productionMult * baseProduction;

            currentActivity.UpdateFrame(requestedPowerRatio * receivedPowerRatio * productionModifier, requestedPowerRatio * receivedPowerRatio, requestedPowerRatio * productionModifier, _overflowAllowed, fixedDeltaTime);

            lastPowerRatio = receivedPowerRatio; // save the current power ratio in case the vessel is unloaded
            lastOverflowSettings = _overflowAllowed; // save the current overflow settings in case the vessel is unloaded
            lastActivityName = currentActivity.ActivityName; // take the string with the name of the current activity, store it in persistent string
            lastClassName = currentActivity.GetType().Name;
            lastActiveTime = Planetarium.GetUniversalTime();
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_Refinery_GetInfo");//"Refinery Module capable of advanced ISRU processing."
        }

        private void OnGUI()
        {
            if (vessel != FlightGlobals.ActiveVessel || !showWindow) return;

            _windowPosition = GUILayout.Window(_windowId, _windowPosition, Window, Localizer.Format("#LOC_KSPIE_Refinery_WindowTitle"));//"ISRU Refinery Interface"
        }

        public bool IsActive(IRefineryActivity activity)
        {
            return refinery_is_enabled && currentActivity == activity;
        }

        public void ToggleRefinery(IRefineryActivity activity)
        {
            if (refinery_is_enabled)
            {
                DeactivateRefinery();
                return;
            }

            ActivateRefinery(activity);
        }

        private void ActivateRefinery(IRefineryActivity activity)
        {
            bool hasRequirement = activity.HasActivityRequirements(); // if the requirements for the activity are fulfilled

            if (hasRequirement)
            {
                currentActivity = activity; // the activity will be treated as the current activity
                refinery_is_enabled = true; // refinery is now on
            }
            else
                activity.PrintMissingResources();
        }

        public void DeactivateRefinery()
        {
            refinery_is_enabled = false;
            currentActivity = null;
        }

        private void Window(int window)
        {
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont};

            if (_valueLabel == null)
                _valueLabel = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };

            if (_enabledButton == null)
                _enabledButton = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };

            if (_disabledButton == null)
                _disabledButton = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Normal, font = PluginHelper.MainFont };

            if (GUI.Button(new Rect(_windowPosition.width - 20, 2, 18, 18), "x"))
                showWindow = false;

            GUILayout.BeginVertical();

            if (currentActivity == null || !refinery_is_enabled) // if there is no processing going on or the refinery is not enabled
            {
                availableRefineries.ForEach(activity => // per each activity (notice the end brackets are there, 13 lines below)
                {
                    GUILayout.BeginHorizontal();
                    bool hasRequirement = activity.HasActivityRequirements(); // if the requirements for the activity are fulfilled
                    GUIStyle guiStyle = hasRequirement ? _enabledButton : _disabledButton; // either draw the enabled, bold button, or the disabled one

                    var buttonText = string.IsNullOrEmpty(activity.Formula) ? activity.ActivityName : activity.ActivityName + " : " + activity.Formula;

                    if (GUILayout.Button(buttonText, guiStyle, GUILayout.ExpandWidth(true))) // if user clicks the button and has requirements for the activity
                        ToggleRefinery(activity);
                    GUILayout.EndHorizontal();
                });
            }
            else
            {
                bool hasRequirement = currentActivity.HasActivityRequirements();

                // show button to enable/disable resource overflow
                GUILayout.BeginHorizontal();
                if (_overflowAllowed)
                {
                    if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Refinery_DisableOverflow"), GUILayout.ExpandWidth(true)))//"Disable Overflow"
                        _overflowAllowed = false;
                }
                else
                {
                    if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Refinery_EnableOverflow"), GUILayout.ExpandWidth(true)))//"Enable Overflow"
                        _overflowAllowed = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_Refinery_CurrentActivity"), _boldLabel, GUILayout.Width(RefineryActivity.labelWidth));//"Current Activity"
                GUILayout.Label(currentActivity.ActivityName, _valueLabel, GUILayout.Width(RefineryActivity.valueWidth * 2));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_Refinery_Status"), _boldLabel, GUILayout.Width(RefineryActivity.labelWidth));//"Status"
                GUILayout.Label(currentActivity.Status, _valueLabel, GUILayout.Width(RefineryActivity.valueWidth * 2));
                GUILayout.EndHorizontal();

                // allow current activity to show feedback
                currentActivity.UpdateGUI();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Refinery_DeactivateProcess"), GUILayout.ExpandWidth(true)))//"Deactivate Process"
                {
                    refinery_is_enabled = false;
                    currentActivity = null;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
