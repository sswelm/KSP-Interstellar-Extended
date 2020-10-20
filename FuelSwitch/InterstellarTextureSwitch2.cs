using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    [KSPModule("#LOC_IFS_TextureSwitch_moduleName")]
    public class InterstellarTextureSwitch2 : PartModule, IHaveFuelTankSetup
    {
        [KSPField]
        public string moduleID = "0";
        [KSPField]
        public string textureRootFolder = string.Empty;
        [KSPField]
        public string objectNames = string.Empty;
        [KSPField]
        public string textureNames = string.Empty;
        [KSPField]
        public string mapNames = string.Empty;
        [KSPField]
        public string textureDisplayNames = Localizer.Format("#LOC_IFS_TextureSwitch_displayname2");//"Default"
        [KSPField]
        public string statusText = Localizer.Format("#LOC_IFS_TextureSwitch_CurrentTexture");//"Current Texture"

        [KSPField(isPersistant = true, guiActiveEditor = true)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
        public int selectedTexture = 0;

        [KSPField]
        public string switcherDescription = "#LOC_IFS_TextureSwitch_TextureName";
        [KSPField]
        public bool hasSwitchChooseOption = true;
        [KSPField(isPersistant = true)]
        public string selectedMapURL = string.Empty;
        [KSPField]
        public bool showListButton = false;
        [KSPField]
        public bool debugMode = false;
        [KSPField]
        public bool switchableInFlight = false;
        [KSPField]
        public string additionalMapType = "_BumpMap";
        [KSPField]
        public bool mapIsNormal = true;
        [KSPField]
        public bool repaintableEVA = true;
        //[KSPField]
        //public Vector4 GUIposition = new Vector4(FSGUIwindowID.standardRect.x, FSGUIwindowID.standardRect.y, FSGUIwindowID.standardRect.width, FSGUIwindowID.standardRect.height);
        [KSPField]
        public bool showCurrentTextureName = false;
        [KSPField]
        public bool showSwitchButtons = false;
        [KSPField]
        public bool showPreviousButton = true;
        [KSPField]
        public bool useFuelSwitchModule = false;
        [KSPField]
        public string fuelTankSetups = "0";
        [KSPField]
        public bool showInfo = true;
        [KSPField]
        public bool updateSymmetry = true;

        private List<List<Material>> targetMats = new List<List<Material>>();
        private List<List<string>> texList = new List<List<string>>();

        private List<string> mapList = new List<string>();
        private List<string> objectList = new List<string>();
        private List<string> textureDisplayList = new List<string>();
        private List<string> fuelTankSetupList = new List<string>();

        private InterstellarFuelSwitch fuelSwitch;

        private bool initialized = false;

        InterstellarDebugMessages debug;

        [KSPField(guiActiveEditor = true, guiName = "#LOC_IFS_TextureSwitch_CurrentTexture")]//Current Texture
        public string currentTextureName = string.Empty;

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_TextureSwitch_DebugLog")]//Debug: Log Objects
        public void listAllObjects()
        {
            List<Transform> childList = ListChildren(part.transform);
            foreach (Transform t in childList)
            {
                Debug.Log("object: " + t.name);
            }
        }

        List<Transform> ListChildren(Transform a)
        {
            List<Transform> childList = new List<Transform>();
            foreach (Transform b in a)
            {
                childList.Add(b);
                childList.AddRange(ListChildren(b));
            }
            return childList;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_TextureSwitch_nextSetup")]
        public void nextTextureEvent()
        {
            selectedTexture++;
            if (selectedTexture >= texList.Count && selectedTexture >= mapList.Count)
                selectedTexture = 0;
            UseTextureAll(true);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_TextureSwitch_previousSetup")]
        public void previousTextureEvent()
        {
            selectedTexture--;
            if (selectedTexture < 0)
                selectedTexture = Mathf.Max(texList.Count - 1, mapList.Count - 1);
            UseTextureAll(true);
        }

        // Called by external classes
        public void SelectTankSetup(int newTankIndex, bool calledByPlayer)
        {
            var found = false;
            if (fuelTankSetupList != null)
            {
                var index = fuelTankSetupList.IndexOf(newTankIndex.ToString(CultureInfo.InvariantCulture));
                if (index >= 0)
                {
                    selectedTexture = index;
                    found = true;
                }
            }

            if (!found && newTankIndex < texList.Count && newTankIndex < mapList.Count)
            {
                selectedTexture = newTankIndex;
            }

            UseTextureAll(calledByPlayer);
        }

        public void SwitchToFuelTankSetup(string fuelTankSetup)
        {
            var index = textureDisplayList.IndexOf(fuelTankSetup);

            if (index < 0 && string.IsNullOrEmpty(fuelTankSetup))
            {
                for (int i = 0 ; i < textureDisplayList.Count ; i++)
                {
                    if (textureDisplayList[i].Contains(fuelTankSetup))
                    {
                        index = i;
                        break;
                    }
                    else if (fuelTankSetup.Contains(textureDisplayList[i]))
                    {
                        index = i;
                        break;
                    }
                }
            }

            if (index >= 0)
            {
                Debug.Log("[IFS] - SwitchToFuelTankSetup found " + fuelTankSetup);
                selectedTexture = index;
                UseTextureAll(true);
            }
            else
                Debug.LogWarning("[IFS] - SwitchToFuelTankSetup is missing " + fuelTankSetup);
        }

        [KSPEvent(guiActiveUnfocused = true, unfocusedRange = 5f, guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_TextureSwitch_Repaint")]//Repaint
        public void nextTextureEVAEvent()
        {
            nextTextureEvent();
        }

        public void UseTextureAll(bool calledByPlayer)
        {
            ApplyTexToPart(calledByPlayer);

            if (!updateSymmetry) return;

            for (var i = 0; i < part.symmetryCounterparts.Count; i++)
            {
                // check that the moduleID matches to make sure we don't target the wrong tex switcher
                InterstellarTextureSwitch2[] symSwitch = part.symmetryCounterparts[i].GetComponents<InterstellarTextureSwitch2>();
                for (int j = 0; j < symSwitch.Length; j++)
                {
                    if (symSwitch[j].moduleID != moduleID) continue;

                    symSwitch[j].selectedTexture = selectedTexture;
                    symSwitch[j].ApplyTexToPart(calledByPlayer);
                }
            }

        }

        private void ApplyTexToPart(bool calledByPlayer)
        {
            InitializeData();

            for (var objectIndex = 0; objectIndex < targetMats.Count; objectIndex++)
            {
                foreach (Material mat in targetMats[objectIndex])
                {
                    UseTextureOrMap(mat, objectIndex);
                }
            }

            if (!useFuelSwitchModule) return;

            debug.debugMessage("calling on InterstellarFuelSwitch tank setup " + selectedTexture);
            if (selectedTexture < fuelTankSetupList.Count)
            {
                var tankSelectionReult = fuelSwitch.SelectTankSetup(fuelTankSetupList[selectedTexture], calledByPlayer);

                if (tankSelectionReult == selectedTexture) return;

                selectedTexture = tankSelectionReult;
                UseTextureAll(calledByPlayer);
            }
            else
                debug.debugMessage("no such fuel tank setup");
        }

        public void UseTextureOrMap(Material targetMat, int objectIndex)
        {
            if (targetMat != null)
            {
                UseTexture(targetMat, objectIndex);

                UseMap(targetMat);
            }
            else
                debug.debugMessage("No target material in object.");
        }

        private void UseMap(Material targetMat)
        {
            debug.debugMessage("maplist count: " + mapList.Count + ", selectedTexture: " + selectedTexture + ", texlist Count: " + texList.Count);
            if (mapList.Count > selectedTexture)
            {
                if (GameDatabase.Instance.ExistsTexture(mapList[selectedTexture]))
                {
                    debug.debugMessage("map " + mapList[selectedTexture] + " exists in db");
                    targetMat.SetTexture(additionalMapType, GameDatabase.Instance.GetTexture(mapList[selectedTexture], mapIsNormal));
                    selectedMapURL = mapList[selectedTexture];

                    if (selectedTexture < textureDisplayList.Count && texList.Count == 0)
                    {
                        currentTextureName = textureDisplayList[selectedTexture];
                        debug.debugMessage("setting currentTextureName to " + textureDisplayList[selectedTexture]);
                    }
                    else
                        debug.debugMessage("not setting currentTextureName. selectedTexture is " + selectedTexture + ", texDispList count is" + textureDisplayList.Count + ", texList count is " + texList.Count);
                }
                else
                {
                    debug.debugMessage("map " + mapList[selectedTexture] + " does not exist in db");
                }
            }
            else
            {
                if (mapList.Count > selectedTexture) // why is this check here? will never happen.
                    debug.debugMessage("no such map: " + mapList[selectedTexture]);
                else
                {
                    debug.debugMessage("useMap, index out of range error, maplist count: " + mapList.Count + ", selectedTexture: " + selectedTexture);
                    for (var i = 0; i < mapList.Count; i++)
                    {
                        debug.debugMessage("map " + i + ": " + mapList[i]);
                    }
                }
            }
        }

        private void UseTexture(Material targetMat, int objectIndex)
        {
            if (texList.Count <= selectedTexture)
                return;

            var texListGroupData = texList[selectedTexture];

            var effectiveObjectIndex = texListGroupData.Count > objectIndex ? objectIndex : 0;
            var texture = texListGroupData[effectiveObjectIndex];

            if (GameDatabase.Instance.ExistsTexture(texture))
            {
                debug.debugMessage("assigning texture: " + texture);
                targetMat.mainTexture = GameDatabase.Instance.GetTexture(texture, false);

                if (selectedTexture > textureDisplayList.Count - 1)
                    currentTextureName = getTextureDisplayName(texture);
                else
                    currentTextureName = textureDisplayList[selectedTexture];
            }
            else
                debug.debugMessage("no such texture: " + texListGroupData[effectiveObjectIndex]);
        }

        public override string GetInfo()
        {
            if (showInfo)
            {
                var variantList = ParseTools.ParseNames(textureNames.Length > 0 ? textureNames : mapNames);
                textureDisplayList = ParseTools.ParseNames(textureDisplayNames);

                var info = StringBuilderCache.Acquire();
                info.AppendLine(Localizer.Format("#LOC_IFS_TextureSwitch_GetInfo"));//"Alternate textures available:"
                if (variantList.Count == 0)
                {
                    if (variantList.Count == 0)
                        info.AppendLine(Localizer.Format("#LOC_IFS_TextureSwitch_GetInfoNone"));//"None"
                }

                for (var i = 0; i < variantList.Count; i++)
                {
                    info.AppendLine(i > textureDisplayList.Count - 1 ?
                        getTextureDisplayName(variantList[i]) : textureDisplayList[i]);
                }

                info.AppendLine().AppendLine().Append(Localizer.Format("#LOC_IFS_TextureSwitch_GetInfoNext"));//Use the Next Texture button on the right click menu.
                return info.ToStringAndRelease();
            }
            else
                return string.Empty;
        }

        private string getTextureDisplayName(string longName)
        {
            var splitString = longName.Split('/');
            return splitString[splitString.Length - 1];
        }

        public override void OnStart(PartModule.StartState state)
        {
            InitializeData();

            UseTextureAll(false);

            if (showListButton) Events["listAllObjects"].guiActiveEditor = true;
            if (!repaintableEVA) Events["nextTextureEVAEvent"].guiActiveUnfocused = false;

            var nextTextureButton = Events["nextTextureEvent"];
            nextTextureButton.guiActive = switchableInFlight && showSwitchButtons;
            nextTextureButton.guiActiveEditor = showSwitchButtons;

            var prevTextureButton = Events["previousTextureEvent"];
            prevTextureButton.guiActive = switchableInFlight && showSwitchButtons;
            prevTextureButton.guiActiveEditor = showSwitchButtons;

            if (!showPreviousButton)
            {
                prevTextureButton.guiActive = false;
                prevTextureButton.guiActiveEditor = false;
            }

            var currentTextureField = Fields["currentTextureName"];
            currentTextureField.guiName = statusText;
            currentTextureField.guiActiveEditor = showCurrentTextureName;

            var chooseField = Fields["selectedTexture"];
            chooseField.guiName = Localizer.Format(switcherDescription);
            chooseField.guiActiveEditor = hasSwitchChooseOption;

            var chooseOption = chooseField.uiControlEditor as UI_ChooseOption;
            chooseOption.options = textureDisplayList.ToArray();
            chooseOption.onFieldChanged = UpdateFromGUI;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            UseTextureAll(true);
        }

        // runs the kind of commands that would normally be in OnStart, if they have not already been run. In case a method is called upon externally, but values have not been set up yet
        private void InitializeData()
        {
            if (initialized) return;

            debug = new InterstellarDebugMessages(debugMode, "InterstellarTextureSwitch2");

            // you can't have fuel switching without symmetry, it breaks the editor GUI.
            if (useFuelSwitchModule) 
                updateSymmetry = true;

            objectList = ParseTools.ParseNames(objectNames, true);
            mapList = ParseTools.ParseNames(mapNames, true, true, textureRootFolder);
            textureDisplayList = ParseTools.ParseNames(textureDisplayNames);
            fuelTankSetupList = ParseTools.ParseNames(fuelTankSetups);

            var textureNameGroups = textureNames.Split(';').ToArray();

            for (var i = 0; i < textureNameGroups.Count(); i++)
            {
                var texListGroup = ParseTools.ParseNames(textureNameGroups[i], true, true, textureRootFolder);

                texList.Add(texListGroup);
            }

            debug.debugMessage("found " + texList.Count + " textures, using number " + selectedTexture + ", found " + objectList.Count + " objects, " + mapList.Count + " maps");

            for (var i = 0; i < objectList.Count(); i++)
            {
                Transform[] targetObjectTransformArray = part.FindModelTransforms(objectList[i]);

                var matList = new List<Material>();

                foreach (Transform t in targetObjectTransformArray)
                {
                    if (t == null)
                        continue;

                    var renderer = t.gameObject.GetComponent<Renderer>();

                    // check for if the object even has a mesh. otherwise part list loading crashes
                    if (renderer == null) continue;

                    Material targetMat = renderer.material;
                    if (targetMat != null && !matList.Contains(targetMat))
                        matList.Add(targetMat);
                }

                targetMats.Add(matList);
            }

            if (useFuelSwitchModule)
            {
                fuelSwitch = part.GetComponent<InterstellarFuelSwitch>(); // only looking for first, not supporting multiple fuel switchers
                if (fuelSwitch == null)
                {
                    useFuelSwitchModule = false;
                    debug.debugMessage("no InterstellarFuelSwitch module found, despite useFuelSwitchModule being true");
                }
                else
                {
                    var matchingObject = fuelSwitch.FindMatchingConfig(this);
                    if (matchingObject >= 0)
                        selectedTexture = matchingObject;
                }
            }
            initialized = true;
        }
    }
}
