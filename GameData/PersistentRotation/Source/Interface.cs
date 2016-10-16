using System;
using UnityEngine;
using KSP.UI.Screens;
using System.IO;

namespace PersistentRotation
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class Interface : MonoBehaviour
    { 
        public static Interface instance { get; private set; }

        private Data data;

        private Vessel activeVessel
        {
            get { return Main.instance.activeVessel; }
        }

        private IButton button;
        private ApplicationLauncherButton stockButton;

        int mainGuid;
        int bodyGuid;
        int configGuid;

        Rect MainWindowRect;
        Rect BodyWindowRect;
        Rect ConfigWindowRect;

        bool hidden = false; //If F2 is pressed
        bool disabled = false; //If Toolbar is pressed, overwritten by LoadGUI

        bool showMainWindow = true; //Window visible, overwritten by LoadGUI
        bool showBodyWindow = false;
        bool showConfigWindow = false;

        bool MainWindowActive = false; // minimize-maximize, overwritten by LoadGUI
        int visibilityMode = 1; // 1: Always Visible, 2: Stock Toolbar, 3: Blizzys Toolbar, overwritten by LoadGUI

        int mode = 1; // 1: rotation, 2: momentum
        public string desiredRPMstr = "";

        Texture close = GameDatabase.Instance.GetTexture("PersistentRotation/Textures/close_w", false);
        Texture options = GameDatabase.Instance.GetTexture("PersistentRotation/Textures/options_w", false);

        String GetPath()
        {
            if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/PersistentRotation/PluginData"))
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/GameData/PersistentRotation/PluginData");

            return KSPUtil.ApplicationRootPath + "/GameData/PersistentRotation/PluginData/Config.cfg";
        }

        private void Awake()
        {
            instance = this;

            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);

            mainGuid = Guid.NewGuid().GetHashCode();
            bodyGuid = Guid.NewGuid().GetHashCode();
            configGuid = Guid.NewGuid().GetHashCode();

            MainWindowRect = new Rect(Screen.width / 4, 0, 200, 10); //Overwritten by LoadGUI
            BodyWindowRect = new Rect((Screen.width / 2) - 75, Screen.height / 4, 150, 10);
            ConfigWindowRect = new Rect((Screen.width / 2) - 100, Screen.height / 4, 200, 10);

            LoadGUI();

            if (visibilityMode == 2)
            {
                CreateStockToolbar();
            }
            else if (visibilityMode == 3)
            {
                CreateBlizzyToolbar();
            }
        }
        private void Start()
        {
            data = Data.instance;
        }
        private void OnDestroy()
        {
            instance = null;

            SaveGUI();

            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);

            DeleteBlizzyToolbar();
            DeleteStockToolbar();
        }

        void OnGUI()
        {
            if (!hidden && !disabled)
            {
                if (showMainWindow)
                {
                    MainWindowRect = GUILayout.Window(mainGuid, MainWindowRect, MainGUI, "PersistentRotation");
                }
                if (showBodyWindow)
                {
                    BodyWindowRect = GUILayout.Window(bodyGuid, BodyWindowRect, BodyGUI, "Select Body");
                }
                if (showConfigWindow)
                {
                    ConfigWindowRect = GUILayout.Window(configGuid, ConfigWindowRect, ConfigGUI, "Configure");
                }
            }
        }
        void OnShowUI()
        {
            hidden = false;
        }
        void OnHideUI()
        {
            hidden = true;
        }
        private void MainGUI(int windowID)
        {
            Data.PRVessel v = data.FindPRVessel(activeVessel);
            Texture toggle;

            if (MainWindowActive)
            {
                toggle = GameDatabase.Instance.GetTexture("PersistentRotation/Textures/minimize_w", false);
            }
            else
            {
                toggle = GameDatabase.Instance.GetTexture("PersistentRotation/Textures/maximize_w", false);
            }

            GUILayout.BeginVertical();

            //Minimize und Option Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(options))
            {
                showConfigWindow = !showConfigWindow;
            }
            if (GUILayout.Button(toggle))
            {
                MainWindowActive = !MainWindowActive;
                MainWindowRect = new Rect(MainWindowRect.x, MainWindowRect.y, 200, 10);
            }
            GUILayout.EndHorizontal();

            //If maximized
            if (MainWindowActive)
            {
                if (activeVessel.IsControllable)
                {
                    GUILayout.BeginHorizontal();
                    if (v.rotationModeActive)
                        GUI.contentColor = Color.green;
                    else
                        GUI.contentColor = Color.red;

                    if (GUILayout.Button("Rotation", GUILayout.ExpandWidth(true)))
                    {
                        mode = 1;
                    }

                    if (v.momentumModeActive)
                        GUI.contentColor = Color.green;
                    else
                        GUI.contentColor = Color.red;

                    if (GUILayout.Button("Momentum", GUILayout.ExpandWidth(true)))
                    {
                        mode = 2;
                    }

                    GUI.contentColor = Color.white;

                    GUILayout.EndHorizontal();

                    GUILayout.Space(15f);

                    if (mode == 1)
                    {
                        if (GUILayout.Button("Relative Rotation", GUILayout.ExpandWidth(true)))
                        {
                            showBodyWindow = !showBodyWindow;
                        }


                        if (v.reference != null)
                        {
                            if (v.dynamicReference)
                            {
                                GUILayout.Label("Reference: Dynamic (" + v.reference.GetName() + ")");
                            }
                            else
                                GUILayout.Label("Reference: " + v.reference.GetName());
                        }
                        else
                        {
                            GUILayout.Label("Reference: None");
                        }

                        string _text = "Activate";
                        if (v.rotationModeActive)
                        {
                            _text = "Deactivate";
                        }

                        if (GUILayout.Button(_text, GUILayout.ExpandWidth(true)))
                        {
                            if(v.rotationModeActive == false)
                            {
                                v.rotationModeActive = true;
                                v.momentumModeActive = false;

                                if (v.reference != null)
                                    v.direction = (v.reference.GetTransform().position - activeVessel.transform.position).normalized;
                                v.rotation = activeVessel.transform.rotation;
                                v.planetariumRight = Planetarium.right;
                            }
                            else
                            {
                                v.rotationModeActive = false;
                            }

                        }
                    }
                    else if (mode == 2)
                    {
                        GUILayout.Label("Overwrite RPM:");
                        GUILayout.BeginHorizontal();
                        desiredRPMstr = GUILayout.TextField(desiredRPMstr);
                        GUILayout.Label(" RPM");
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10f);
                        string _text = "Activate";
                        if (v.momentumModeActive)
                        {
                            _text = "Deactivate";
                        }

                        if (GUILayout.Button(_text, GUILayout.ExpandWidth(true)))
                        {
                            v.rotationModeActive = false;

                            if (v.momentumModeActive)
                            {
                                v.momentumModeActive = false;
                            }
                            else
                            {
                                try
                                {
                                    v.desiredRPM = float.Parse(desiredRPMstr);
                                    v.momentumModeActive = true;
                                }
                                catch
                                {
                                    desiredRPMstr = v.desiredRPM.ToString();
                                }
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label("Vessel is not controllable.");
                }
                GUI.DragWindow();
            }
            GUILayout.EndVertical();
        }
        private void BodyGUI(int windowID)
        {
            Data.PRVessel v = data.FindPRVessel(activeVessel);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(close))
            {
                showBodyWindow = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Select target in map \nto set as reference");
            if (GUILayout.Button("Set", GUILayout.ExpandWidth(true)))
            {
                if (activeVessel.targetObject != null)
                {
                    if (activeVessel.targetObject.GetType() == typeof(CelestialBody) || activeVessel.targetObject.GetType() == typeof(Vessel))
                        v.reference = activeVessel.targetObject;
                    v.dynamicReference = false;
                }
                else
                {
                    v.dynamicReference = false;
                    v.reference = null;
                }
            }
            if (GUILayout.Button("Unset", GUILayout.ExpandWidth(true)))
            {
                v.reference = null;
                v.dynamicReference = false;
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Sun", GUILayout.ExpandWidth(true)))
            {
                v.reference = Sun.Instance.sun;
                v.dynamicReference = false;
            }
            if (GUILayout.Button(activeVessel.mainBody.name, GUILayout.ExpandWidth(true)))
            {
                v.reference = activeVessel.mainBody;
                v.dynamicReference = false;
            }
            if (GUILayout.Button("Dynamic", GUILayout.ExpandWidth(true)))
            {
                v.dynamicReference = true;
                //v.reference = v.vessel.mainBody; --> This should not be necessary!
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        private void ConfigGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(close))
            {
                showConfigWindow = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("GUI Visibility Mode");

            if (visibilityMode == 1)
                GUI.contentColor = Color.green;
            else
                GUI.contentColor = Color.red;

            if (GUILayout.Button("Always Visible", GUILayout.ExpandWidth(true)))
            {
                visibilityMode = 1;
                DeleteBlizzyToolbar();
                DeleteStockToolbar();
            }

            if (visibilityMode == 2)
                GUI.contentColor = Color.green;
            else
                GUI.contentColor = Color.red;

            if (GUILayout.Button("Stock Toolbar", GUILayout.ExpandWidth(true)))
            {
                visibilityMode = 2;
                CreateStockToolbar();
                DeleteBlizzyToolbar();
            }

            if (visibilityMode == 3)
                GUI.contentColor = Color.green;
            else
                GUI.contentColor = Color.red;

            if (ToolbarManager.ToolbarAvailable)
            {
                if (GUILayout.Button("Blizzy's Toolbar", GUILayout.ExpandWidth(true)))
                {
                    visibilityMode = 3;
                    CreateBlizzyToolbar();
                    DeleteStockToolbar();
                }
            }

            GUI.contentColor = Color.white;

            GUILayout.Space(20);
            GUILayout.Label("Default Reference Body");

            if (data.defaultReferenceMode == Data.DefaultReferenceMode.NONE)
                GUI.contentColor = Color.green;
            else
                GUI.contentColor = Color.red;

            if (GUILayout.Button("None", GUILayout.ExpandWidth(true)))
            {
                data.defaultReferenceMode = Data.DefaultReferenceMode.NONE;
            }

            if (data.defaultReferenceMode == Data.DefaultReferenceMode.DYNAMIC)
                GUI.contentColor = Color.green;
            else
                GUI.contentColor = Color.red;

            if (GUILayout.Button("Dynamic", GUILayout.ExpandWidth(true)))
            {
                data.defaultReferenceMode = Data.DefaultReferenceMode.DYNAMIC;
            }

            GUI.contentColor = Color.white;

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        void CreateBlizzyToolbar()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                // regular button
                button = ToolbarManager.Instance.add("PersistentRotation", "button");
                button.TexturePath = "PersistentRotation/Textures/texture";
                button.ToolTip = "Toggle PersistentRotation";
                button.OnClick += (e) =>
                {
                    disabled = !disabled;
                };
            }
        }
        void DeleteBlizzyToolbar()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                if (button != null)
                {
                    button.Destroy();
                }
            }
        }
        void CreateStockToolbar()
        {
            if (!stockButton)
            {
                stockButton = ApplicationLauncher.Instance.AddModApplication(
                () =>
                {
                    disabled = !disabled;
                },
                () =>
                {
                    disabled = !disabled;
                },
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT,
                GameDatabase.Instance.GetTexture("PersistentRotation/Textures/texture", false)
                );
            }
        }
        void DeleteStockToolbar()
        {
            if (stockButton)
            {
                ApplicationLauncher.Instance.RemoveModApplication(stockButton);
                stockButton = null;
            }
        }
        void SaveGUI()
        {
            try
            {
                ConfigNode save = new ConfigNode();
                ConfigNode.CreateConfigFromObject(this, 0, save);

                save.AddValue("disabled", disabled.ToString());
                save.AddValue("mode", visibilityMode.ToString());
                save.AddValue("active", MainWindowActive.ToString());

                save.AddValue("showMain", showMainWindow.ToString());
                save.AddValue("xMain", MainWindowRect.x.ToString());
                save.AddValue("yMain", MainWindowRect.y.ToString());

                save.AddValue("showBody", showBodyWindow.ToString());
                save.AddValue("xBody", BodyWindowRect.x.ToString());
                save.AddValue("yBody", BodyWindowRect.y.ToString());

                save.AddValue("showConfig", showConfigWindow.ToString());
                save.AddValue("xConfig", ConfigWindowRect.x.ToString());
                save.AddValue("yConfig", ConfigWindowRect.y.ToString());

                save.Save(GetPath());
            }
            catch (Exception e) { Debug.Log("[PR] Saving not successful: " + e.Message); }
        }
        void LoadGUI()
        {
            ConfigNode load = ConfigNode.Load(GetPath());
            if (load == null)
            {
                Debug.Log("[PR] Cfg file is empty or not existent!");
                return;
            }

            foreach (ConfigNode.Value s in load.values)
            {
                if (s.name == "disabled")
                    disabled = Convert.ToBoolean(s.value);
                if (s.name == "mode")
                    visibilityMode = Convert.ToInt32(s.value);
                if (s.name == "active")
                    MainWindowActive = Convert.ToBoolean(s.value);

                if (s.name == "showMain")
                    showMainWindow = Convert.ToBoolean(s.value);
                if (s.name == "xMain")
                    MainWindowRect.x = Convert.ToSingle(s.value);
                if (s.name == "yMain")
                    MainWindowRect.y = Convert.ToSingle(s.value);

                if (s.name == "showBody")
                    showBodyWindow = Convert.ToBoolean(s.value);
                if (s.name == "xBody")
                    BodyWindowRect.x = Convert.ToSingle(s.value);
                if (s.name == "yBody")
                    BodyWindowRect.y = Convert.ToSingle(s.value);

                if (s.name == "showConfig")
                    showConfigWindow = Convert.ToBoolean(s.value);
                if (s.name == "xConfig")
                    ConfigWindowRect.x = Convert.ToSingle(s.value);
                if (s.name == "yConfig")
                    ConfigWindowRect.y = Convert.ToSingle(s.value);
            }
        }
    }
}