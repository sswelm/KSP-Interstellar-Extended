using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    [KSPModule("ISRU Refinery")]
    class InterstellarRefineryController : PartModule
    {
        [KSPField(isPersistant = true)]
        protected bool refinery_is_enabled;
        [KSPField(isPersistant = true)]
        protected bool lastOverflowSettings;
        [KSPField(isPersistant = true)]
        protected double lastActiveTime;
        [KSPField(isPersistant = true)]
        protected double lastPowerRatio;
        [KSPField(isPersistant = true)]
        protected string lastActivityName = "";

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Refinery Type")]
        public int refineryType = 255;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Control"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerPercentage = 100;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string status_str = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Base Production", guiFormat = "F3")]
        public double baseProduction = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Production Multiplier", guiFormat = "F3")]
        public double productionMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Power Req Multiplier", guiFormat = "F3")]
        public double powerReqMult = 1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Power Requirement", guiFormat = "F3", guiUnits = " MW")]
        public double currentPowerReq;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Power Available", guiUnits = "%", guiFormat = "F3")]
        public double utilisationPercentage;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Consumed Power", guiFormat = "F3", guiUnits = " MW")]
        public double consumedPowerMW;

        protected IRefineryActivity _current_activity = null;
        protected IPowerSupply powerSupply;

        private List<IRefineryActivity> _refinery_activities;
        private Rect _window_position = new Rect(50, 50, RefineryActivityBase.labelWidth * 4, 150);
        private int _window_ID;
        private bool _render_window;
        private GUIStyle _bold_label;
        private GUIStyle _value_label;
        private GUIStyle _enabled_button;
        private GUIStyle _disabled_button;

        /*

        [KSPEvent(guiActive = true, guiName = "Test Atmosphere", active = true)]
        public void SampleAtmosphere()
        {
            CelestialBody celestialBody = vessel.mainBody;

            AtmosphericResourceHandler.GenerateCompositionFromCelestialBody(celestialBody);

            Debug.Log("[KSPI] - determined " + celestialBody.name + " to be current celestrial body");

            // Lookup homeworld
            CelestialBody homeworld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);

            Debug.Log("[KSPI] - determined " + homeworld.name + " to be the home world");

            double presureAtSurface = celestialBody.GetPressure(0);

            Debug.Log("[KSPI] - surface presure " + celestialBody.name + " is " + presureAtSurface);
            Debug.Log("[KSPI] - surface presure " + homeworld.name + " is " + homeworld.GetPressure(0));
            Debug.Log("[KSPI] - mass " + celestialBody.name + " is " + celestialBody.Mass);
            Debug.Log("[KSPI] - mass " + homeworld.name + " is " + celestialBody.Mass);

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
                PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resource.ResourceName);

                string found = definition != null ? "D" : "U";
                ScreenMessages.PostScreenMessage(found + " " + resource.DisplayName + " " + resource.ResourceName + " " + resource.ResourceAbundance, 6.0f, ScreenMessageStyle.LOWER_CENTER);
            }
        }
         * 
         */

        [KSPEvent(guiActive = true, guiName = "Toggle Refinery Window", active = true)]
        public void ToggleWindow()
        {
            _render_window = !_render_window;
        }

        public override void OnStart(PartModule.StartState state)
        {
            powerSupply = part.FindModuleImplementing<IPowerSupply>();

            if (powerSupply != null)
                powerSupply.DisplayName = "started";

            if (state == StartState.Editor) return;

            // load stored overflow setting
            overflowAllowed = lastOverflowSettings;

            _window_ID = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);

            var unsortedList = new List<IRefineryActivity>();

            try
            {
                unsortedList.Add(new AnthraquinoneProcessor(this.part));
                unsortedList.Add(new NuclearFuelReprocessor(this.part));
                unsortedList.Add(new AluminiumElectrolyser(this.part));
                unsortedList.Add(new SabatierReactor(this.part));
                unsortedList.Add(new WaterElectroliser(this.part));
                unsortedList.Add(new HeavyWaterElectroliser(this.part));
                unsortedList.Add(new PeroxideProcess(this.part));
                unsortedList.Add(new UF4Ammonolysiser(this.part));
                unsortedList.Add(new HaberProcess(this.part));
                unsortedList.Add(new AmmoniaElectrolyzer(this.part));
                unsortedList.Add(new CarbonDioxideElectroliser(this.part));
                unsortedList.Add(new WaterGasShift(this.part));
                unsortedList.Add(new ReverseWaterGasShift(this.part));
                unsortedList.Add(new PartialOxidationMethane(this.part));
                unsortedList.Add(new SolarWindProcessor(this.part));
                unsortedList.Add(new RegolithProcessor(this.part));
                unsortedList.Add(new AtmosphericExtractor(this.part));
                unsortedList.Add(new SeawaterExtractor(this.part));
            }
            catch (Exception e)
            {
                Debug.LogException(e, new UnityEngine.Object() { name = "ISRU Refinery" });
                Debug.LogWarning("[KSPI] - ISRU Refinery Exception " + e.Message);
            }

            _refinery_activities = unsortedList.Where(m => ((int)m.RefineryType & this.refineryType) == (int)m.RefineryType).OrderBy(a => a.ActivityName).ToList();

            // load same 
            if (refinery_is_enabled && !string.IsNullOrEmpty(lastActivityName))
            {
                _current_activity = _refinery_activities.FirstOrDefault(a => a.ActivityName == lastActivityName);
            }

            if (_current_activity != null)
            {
                var productionRate = lastPowerRatio * productionMult * baseProduction;

                var timeDifference = (Planetarium.GetUniversalTime() - lastActiveTime);
                //string message = "[KSPI] - IRSU performed " + lastActivityName + " for " + timeDifference.ToString("0.0") + " seconds with production rate " + productionRate.ToString("0.0");
                //Debug.Log(message);
                //ScreenMessages.PostScreenMessage(message, 60.0f, ScreenMessageStyle.LOWER_CENTER);

                if (lastActivityName == "Atmospheric Extraction")
                    ((AtmosphericExtractor)_current_activity).ExtractAir(productionRate, lastPowerRatio, productionMult * baseProduction, lastOverflowSettings, timeDifference, true);
                else if (lastActivityName == "Seawater Extraction")
                    ((SeawaterExtractor)_current_activity).ExtractSeawater(productionRate, lastPowerRatio, productionMult * baseProduction, lastOverflowSettings, timeDifference, true);
                else
                    _current_activity.UpdateFrame(productionRate, lastPowerRatio, productionMult * baseProduction, lastOverflowSettings, timeDifference);
            }

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
                Debug.LogError("[KSPI] - InterstellarRefineryController Exception " + e.Message);
            }
        }

        public override void OnUpdate()
        {
            status_str = "Offline";

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

            var recievedElectricCharge = part.RequestResource("ElectricCharge", shortage * 1000 * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;

            consumedPowerMW += recievedElectricCharge / 1000;

            var power_ratio = currentPowerReq > 0 ? consumedPowerMW / currentPowerReq : 0;

            utilisationPercentage = power_ratio * 100;

            var productionModifier = productionMult * baseProduction;

            _current_activity.UpdateFrame(power_ratio * productionModifier, power_ratio, productionModifier, overflowAllowed, TimeWarp.fixedDeltaTime);

            lastPowerRatio = power_ratio; // save the current power ratio in case the vessel is unloaded
            lastOverflowSettings = overflowAllowed; // save the current overflow settings in case the vessel is unloaded
            lastActivityName = _current_activity.ActivityName; // take the string with the name of the current activity, store it in persistent string
            lastActiveTime = Planetarium.GetUniversalTime();
        }

        public override string GetInfo()
        {
            return "Refinery Module capable of advanced ISRU processing.";
        }

        private void OnGUI()
        {
            if (this.vessel != FlightGlobals.ActiveVessel || !_render_window) return;

            _window_position = GUILayout.Window(_window_ID, _window_position, Window, "ISRU Refinery Interface");
        }

        private bool overflowAllowed;

        private void Window(int window)
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
                _bold_label.font = PluginHelper.MainFont;
            }

            if (_value_label == null)
                _value_label = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };

            if (_enabled_button == null)
            {
                _enabled_button = new GUIStyle(GUI.skin.button);
                _enabled_button.fontStyle = FontStyle.Bold;
                _enabled_button.font = PluginHelper.MainFont;
            }

            if (_disabled_button == null)
            {
                _disabled_button = new GUIStyle(GUI.skin.button);
                _disabled_button.fontStyle = FontStyle.Normal;
                _disabled_button.font = PluginHelper.MainFont;
            }

            if (GUI.Button(new Rect(_window_position.width - 20, 2, 18, 18), "x"))
                _render_window = false;

            GUILayout.BeginVertical();

            if (_current_activity == null || !refinery_is_enabled) // if there is no processing going on or the refinery is not enabled
            {
                _refinery_activities.ForEach(act => // per each activity (notice the end brackets are there, 13 lines below)
                {
                    GUILayout.BeginHorizontal();
                    bool hasRequirement = act.HasActivityRequirements; // if the requirements for the activity are fulfilled
                    GUIStyle guistyle = hasRequirement ? _enabled_button : _disabled_button; // either draw the enabled, bold button, or the disabled one

                    if (GUILayout.Button(act.ActivityName, guistyle, GUILayout.ExpandWidth(true))) // if user clicks the button and has requirements for the activity
                    {
                        if(hasRequirement) { 
                            _current_activity = act; // the activity will be treated as the current activity
                            refinery_is_enabled = true; // refinery is now on
                         }
                        else
                        {
                            act.PrintMissingResources(); 
                        }

                    }
                    GUILayout.EndHorizontal();
                });
            }
            else
            {
                // show button to enable/disable resource overflow
                GUILayout.BeginHorizontal();
                if (overflowAllowed)
                {
                    if (GUILayout.Button("Disable Overflow", GUILayout.ExpandWidth(true)))
                        overflowAllowed = false;
                }
                else
                {
                    if (GUILayout.Button("Enable Overflow", GUILayout.ExpandWidth(true)))
                        overflowAllowed = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Activity", _bold_label, GUILayout.Width(RefineryActivityBase.labelWidth));
                GUILayout.Label(_current_activity.ActivityName, _value_label, GUILayout.Width(RefineryActivityBase.valueWidth));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Status", _bold_label, GUILayout.Width(RefineryActivityBase.labelWidth));
                GUILayout.Label(_current_activity.Status, _value_label, GUILayout.Width(RefineryActivityBase.valueWidth));
                GUILayout.EndHorizontal();

                // allow current activity to show feedback
                _current_activity.UpdateGUI();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Deactivate Process", GUILayout.ExpandWidth(true)))
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
