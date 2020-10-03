using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    [KSPModule("ISRU Refinery")]
    class InterstellarRefineryController : PartModule
    {
        [KSPField(isPersistant = true, guiActive = false)]
        protected bool refinery_is_enabled;
        [KSPField(isPersistant = true, guiActive = false)]
        protected bool lastOverflowSettings;
        [KSPField(isPersistant = true, guiActive = false)]
        protected double lastActiveTime;
        [KSPField(isPersistant = true, guiActive = false)]
        protected double lastPowerRatio;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Refinery_Current")]//Current
        protected string lastActivityName = "";
        [KSPField(isPersistant = true, guiActive = false)]
        protected string lastClassName = "";

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_RefineryType")]//Refinery Type
        public int refineryType = 0;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Refinery_PowerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]//Power Control
        public float powerPercentage = 100;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_Refinery_Status")]//Status
        public string status_str = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_BaseProduction", guiFormat = "F3")]//Base Production
        public double baseProduction = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_ProductionMultiplier", guiFormat = "F3")]//Production Multiplier
        public double productionMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_PowerReqMultiplier", guiFormat = "F3")]//Power Req Multiplier
        public double powerReqMult = 1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_PowerRequirement", guiFormat = "F3", guiUnits = " MW")]//Power Requirement
        public double currentPowerReq;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_PowerAvailable", guiUnits = "%", guiFormat = "F3")]//Power Available
        public double utilisationPercentage;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_ConsumedPower", guiFormat = "F3", guiUnits = " MW")]//Consumed Power
        public double consumedPowerMW;

        protected IRefineryActivity _current_activity = null;
        protected IPowerSupply powerSupply;

        private List<IRefineryActivity> availableRefineries;
        private Rect _window_position = new Rect(50, 50, RefineryActivity.labelWidth * 4, 150);
        private int _window_ID;
        private bool _render_window;
        private GUIStyle _bold_label;
        private GUIStyle _value_label;
        private GUIStyle _enabled_button;
        private GUIStyle _disabled_button;

        private bool _overflowAllowed;

        /*

        [KSPEvent(guiActive = true, guiName = "Test Atmosphere", active = true)]
        public void SampleAtmosphere()
        {
            CelestialBody celestialBody = vessel.mainBody;

            AtmosphericResourceHandler.GenerateCompositionFromCelestialBody(celestialBody);

            Debug.Log("[KSPI]: determined " + celestialBody.name + " to be current celestrial body");

            // Lookup homeworld
            CelestialBody homeworld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);

            Debug.Log("[KSPI]: determined " + homeworld.name + " to be the home world");

            double presureAtSurface = celestialBody.GetPressure(0);

            Debug.Log("[KSPI]: surface presure " + celestialBody.name + " is " + presureAtSurface);
            Debug.Log("[KSPI]: surface presure " + homeworld.name + " is " + homeworld.GetPressure(0));
            Debug.Log("[KSPI]: mass " + celestialBody.name + " is " + celestialBody.Mass);
            Debug.Log("[KSPI]: mass " + homeworld.name + " is " + celestialBody.Mass);

            List<AtmosphericResource> resources = AtmosphericResourceHandler.GetAtmosphericCompositionForBody(part.vessel.mainBody);

            foreach (var resource in resources)
            {
                ScreenMessages.PostScreenMessage(resource.DisplayName + " " + resource.ResourceName + " " + resource.ResourceAbundance, 6.0f, ScreenMessageStyle.LOWER_CENTER);
            }
        }

        [KSPEvent(guiActive = true, guiName = "Sample Ocean", active = true)]
        public void SampleOcean()
        {
            List<OceanicResource> resources = OceanicResourceHandler.GetOceanicCompositionForBody(part.vessel.mainBody).ToList();

            foreach (var resource in resources)
            {
                Definition definition = PartResourceLibrary.Instance.GetDefinition(resource.ResourceName);

                string found = definition != null ? "D" : "U";
                ScreenMessages.PostScreenMessage(found + " " + resource.DisplayName + " " + resource.ResourceName + " " + resource.ResourceAbundance, 6.0f, ScreenMessageStyle.LOWER_CENTER);
            }
        }
         * 
         */

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Refinery_ToggleRefineryWindow", active = true)]//Toggle Refinery Window
        public void ToggleWindow()
        {
            _render_window = !_render_window;

            if (_render_window && availableRefineries.Count == 1)
                _current_activity = availableRefineries.First();
        }

        public override void OnStart(StartState state)
        {
            powerSupply = part.FindModuleImplementing<IPowerSupply>();

            if (powerSupply != null)
                powerSupply.DisplayName = Localizer.Format("#LOC_KSPIE_Refinery_started"); //"started"

            if (state == StartState.Editor) return;

            // load stored overflow setting
            _overflowAllowed = lastOverflowSettings;

            _window_ID = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);

            var refineriesList = part.FindModulesImplementing<IRefineryActivity>().ToList();

            if (refineryType > 0)
            {
                AddIfMissing(refineriesList, new AluminiumElectrolyser());
                AddIfMissing(refineriesList, new AmmoniaElectrolyzer());
                AddIfMissing(refineriesList, new AnthraquinoneProcessor());
                AddIfMissing(refineriesList, new AtmosphericExtractor());
                AddIfMissing(refineriesList, new CarbonDioxideElectroliser());
                AddIfMissing(refineriesList, new HaberProcess());
                AddIfMissing(refineriesList, new HeavyWaterElectroliser());
                AddIfMissing(refineriesList, new PartialOxidationMethane());
                AddIfMissing(refineriesList, new PeroxideProcess());
                AddIfMissing(refineriesList, new UF4Ammonolysiser());
                AddIfMissing(refineriesList, new RegolithProcessor());
                AddIfMissing(refineriesList, new ReverseWaterGasShift());
                AddIfMissing(refineriesList, new NuclearFuelReprocessor());
                AddIfMissing(refineriesList, new SabatierReactor());
                AddIfMissing(refineriesList, new OceanExtractor());
                AddIfMissing(refineriesList, new SolarWindProcessor());
                AddIfMissing(refineriesList, new WaterElectroliser());
                AddIfMissing(refineriesList, new WaterGasShift());

                availableRefineries = refineriesList
                    .Where(m => ((int) m.RefineryType & refineryType) == (int) m.RefineryType)
                    .OrderBy(a => a.ActivityName).ToList();
            }
            else
                availableRefineries = refineriesList.OrderBy(a => a.ActivityName).ToList();

            // initialize refineries
            availableRefineries.ForEach(m => m.Initialize(part));

            // load same 
            if (refinery_is_enabled && !string.IsNullOrEmpty(lastActivityName))
            {
                Debug.Log("[KSPI]: ISRU Refinery looking to restart " + lastActivityName);
                _current_activity = availableRefineries.FirstOrDefault(a => a.ActivityName == lastActivityName);

                if (_current_activity == null)
                {
                    Debug.Log("[KSPI]: ISRU Refinery looking to restart " + lastClassName);
                    _current_activity = availableRefineries.FirstOrDefault(a => a.GetType().Name == lastClassName);
                }
            }

            if (_current_activity != null)
            {
                bool hasRequirement = _current_activity.HasActivityRequirements();
                lastActivityName = _current_activity.ActivityName;

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
                    ((AtmosphericExtractor) _current_activity).ExtractAir(lastPowerRatio * productionModifier,
                        lastPowerRatio, productionModifier, lastOverflowSettings, timeDifference, true);
                else if (lastActivityName == "Seawater Extraction")
                    ((OceanExtractor) _current_activity).ExtractSeawater(lastPowerRatio * productionModifier,
                        lastPowerRatio, productionModifier, lastOverflowSettings, timeDifference, true);
                else
                    _current_activity.UpdateFrame(lastPowerRatio * productionModifier, lastPowerRatio,
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
            try
            {
                if (HighLogic.LoadedSceneIsEditor)
                    return;

                if (_current_activity == null)
                {
                    powerSupply.DisplayName = part.partInfo.title;
                    return;
                }

                powerSupply.DisplayName = part.partInfo.title + " (" + _current_activity.ActivityName + ")";
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: InterstellarRefineryController Exception " + e.Message);
            }
        }

        public override void OnUpdate()
        {
            status_str = Localizer.Format("#LOC_KSPIE_Refinery_Offline");//"Offline"

            if (_current_activity == null) return;

            status_str = _current_activity.Status;
        }

        public void FixedUpdate()
        {
            currentPowerReq = 0;

            if (!HighLogic.LoadedSceneIsFlight || !refinery_is_enabled || _current_activity == null)
            {
                lastActivityName = string.Empty;
                return;
            }

            currentPowerReq = powerReqMult * _current_activity.PowerRequirements * baseProduction;

            var powerRequest = currentPowerReq * (powerPercentage / 100);

            consumedPowerMW = CheatOptions.InfiniteElectricity
                ? powerRequest
                : powerSupply.ConsumeMegajoulesPerSecond(powerRequest);


            var shortage = Math.Max(currentPowerReq - consumedPowerMW, 0);

            var fixedDeltaTime = (double)(decimal)TimeWarp.fixedDeltaTime;

            var receivedElectricCharge = part.RequestResource("ElectricCharge", shortage * 1000 * fixedDeltaTime) / fixedDeltaTime;

            consumedPowerMW += receivedElectricCharge / 1000;

            var power_ratio = currentPowerReq > 0 ? consumedPowerMW / currentPowerReq : 0;

            utilisationPercentage = power_ratio * 100;

            var productionModifier = productionMult * baseProduction;

            _current_activity.UpdateFrame(power_ratio * productionModifier, power_ratio, productionModifier, _overflowAllowed, fixedDeltaTime);

            lastPowerRatio = power_ratio; // save the current power ratio in case the vessel is unloaded
            lastOverflowSettings = _overflowAllowed; // save the current overflow settings in case the vessel is unloaded
            lastActivityName = _current_activity.ActivityName; // take the string with the name of the current activity, store it in persistent string
            lastClassName = _current_activity.GetType().Name;
            lastActiveTime = Planetarium.GetUniversalTime();
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_Refinery_GetInfo");//"Refinery Module capable of advanced ISRU processing."
        }

        private void OnGUI()
        {
            if (this.vessel != FlightGlobals.ActiveVessel || !_render_window) return;

            _window_position = GUILayout.Window(_window_ID, _window_position, Window, Localizer.Format("#LOC_KSPIE_Refinery_WindowTitle"));//"ISRU Refinery Interface"
        }

        private void Window(int window)
        {
            if (_bold_label == null)
                _bold_label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont};

            if (_value_label == null)
                _value_label = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };

            if (_enabled_button == null)
                _enabled_button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };

            if (_disabled_button == null)
                _disabled_button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Normal, font = PluginHelper.MainFont };

            if (GUI.Button(new Rect(_window_position.width - 20, 2, 18, 18), "x"))
                _render_window = false;

            GUILayout.BeginVertical();

            if (_current_activity == null || !refinery_is_enabled) // if there is no processing going on or the refinery is not enabled
            {
                availableRefineries.ForEach(act => // per each activity (notice the end brackets are there, 13 lines below)
                {
                    GUILayout.BeginHorizontal();
                    bool hasRequirement = act.HasActivityRequirements(); // if the requirements for the activity are fulfilled
                    GUIStyle guiStyle = hasRequirement ? _enabled_button : _disabled_button; // either draw the enabled, bold button, or the disabled one

                    if (GUILayout.Button(act.ActivityName, guiStyle, GUILayout.ExpandWidth(true))) // if user clicks the button and has requirements for the activity
                    {
                        if (hasRequirement)
                        {
                            _current_activity = act; // the activity will be treated as the current activity
                            refinery_is_enabled = true; // refinery is now on
                        }
                        else
                            act.PrintMissingResources();

                    }
                    GUILayout.EndHorizontal();
                });
            }
            else
            {
                bool hasRequirement = _current_activity.HasActivityRequirements();

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
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_Refinery_CurrentActivity"), _bold_label, GUILayout.Width(RefineryActivity.labelWidth));//"Current Activity"
                GUILayout.Label(_current_activity.ActivityName, _value_label, GUILayout.Width(RefineryActivity.valueWidth * 2));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_Refinery_Status"), _bold_label, GUILayout.Width(RefineryActivity.labelWidth));//"Status"
                GUILayout.Label(_current_activity.Status, _value_label, GUILayout.Width(RefineryActivity.valueWidth * 2));
                GUILayout.EndHorizontal();

                // allow current activity to show feedback
                _current_activity.UpdateGUI();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Refinery_DeactivateProcess"), GUILayout.ExpandWidth(true)))//"Deactivate Process"
                {
                    refinery_is_enabled = false;
                    _current_activity = null;
                }
                GUILayout.EndHorizontal();


            }
            GUILayout.EndVertical();
            GUI.DragWindow();

        }

    }
}
