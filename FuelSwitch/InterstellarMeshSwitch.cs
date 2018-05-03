using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    [KSPModule("#LOC_IFS_MeshSwitch_moduleName")]
    public class InterstellarMeshSwitch : PartModule 
    {
        [KSPField]
        public int moduleID = 0;
        [KSPField]
        public string switcherDescription = "#LOC_IFS_MeshSwitch_MeshName";
        [KSPField]
        public string tankSwitchNames = string.Empty;
        [KSPField]
        public string objectDisplayNames = string.Empty;
        [KSPField]
        public bool showPreviousButton = true;
        [KSPField]
        public bool useFuelSwitchModule = false;
        [KSPField]
        public string searchTankId = "";
        [KSPField]
        public string fuelTankSetups = "0";
        [KSPField]
        public string objects = string.Empty;
        [KSPField]
        public bool updateSymmetry = true;
        [KSPField]
        public bool affectColliders = true;
        [KSPField]
        public bool showInfo = true;
        [KSPField]
        public bool debugMode = false;
        [KSPField]
        public bool showSwitchButtons = false;
        [KSPField]
        public bool showCurrentObjectName =false;
        [KSPField]
        public bool hasSwitchChooseOption = true;

        [KSPField(isPersistant = true, guiActiveEditor = true)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
        public int selectedObject;

        private List<List<Transform>> objectTransforms = new List<List<Transform>>();
        private List<string> fuelTankSetupList = new List<string>();
        private List<string> objectDisplayList = new List<string>();
        private List<string> tankSwitchNamesList = new List<string>();

        private InterstellarFuelSwitch fuelSwitch;
        //private InterstellarDebugMessages debug;

        private bool initialized;


        [KSPField(guiActiveEditor = false, guiName = "#LOC_IFS_MeshSwitch_currentObjectName")]
        public string currentObjectName = string.Empty;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "#LOC_IFS_MeshSwitch_nextSetup")]
        public void nextObjectEvent()
        {
            selectedObject++;
            if (selectedObject >= objectDisplayList.Count)
                selectedObject = 0;

            SwitchToObject(selectedObject, true);            
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "#LOC_IFS_MeshSwitch_previousetup")]
        public void previousObjectEvent()
        {
            selectedObject--;
            if (selectedObject < 0)
                selectedObject = objectDisplayList.Count - 1;

            SwitchToObject(selectedObject, true);        
        }

        private void ParseObjectNames()
        {
            var objectBatchNames = objects.Split(';');
            if (objectBatchNames.Length < 1)
            {
                //debug.debugMessage("InterstellarMeshSwitch: Found no object names in the object list");
            }
            else
            {
                objectTransforms.Clear();
                for (var batchCount = 0; batchCount < objectBatchNames.Length; batchCount++)
                {
                    var newObjects = new List<Transform>();
                    var objectNames = objectBatchNames[batchCount].Split(',');
                    for (var objectCount = 0; objectCount < objectNames.Length; objectCount++)
                    {
                        var newTransform = part.FindModelTransform(objectNames[objectCount].Trim(' '));
                        if (newTransform != null)
                        {
                            newObjects.Add(newTransform);
                            //debug.debugMessage("InterstellarMeshSwitch: added object to list: " + objectNames[objectCount]);
                        }
                        else
                        {
                            newObjects.Add(null);
                            //debug.debugMessage("InterstellarMeshSwitch: could not find object " + objectNames[objectCount]);
                        }
                    }
                    if (newObjects.Count > 0)
                        objectTransforms.Add(newObjects);
                }
            }
        }

        private void SwitchToObject(int objectNumber, bool calledByPlayer)
        {
            //Debug.Log("[IFS] - InterstellarMeshSwitch SwitchToObject(int objectNumber = " + objectNumber + ", bool calledByPlayer = " + calledByPlayer + ") ");

            SetObject(objectNumber, calledByPlayer);

            //Debug.Log("[IFS] - InterstellarMeshSwitch SwitchToObject if (!updateSymmetry)");

            if (!updateSymmetry)
                return;

            //Debug.Log("[IFS] - InterstellarMeshSwitch SwitchToObject for (var i = 0; i < part.symmetryCounterparts.Count; i++)");
            for (var i = 0; i < part.symmetryCounterparts.Count; i++)
            {
                var symSwitch = part.symmetryCounterparts[i].GetComponents<InterstellarMeshSwitch>();
                for (var j = 0; j < symSwitch.Length; j++)
                {
                    if (symSwitch[j].moduleID != moduleID) continue;

                    symSwitch[j].selectedObject = selectedObject;
                    symSwitch[j].SetObject(objectNumber, calledByPlayer);
                }
            }

        }

        private void SetObject(int objectNumber, bool calledByPlayer)
        {
            //Debug.Log("[IFS] - InterstellarMeshSwitch SetObject(int objectNumber = " + objectNumber + ", bool calledByPlayer = " + calledByPlayer + ") ");

            InitializeData();

            //Debug.Log("[IFS] - InterstellarMeshSwitch SetObject for (var i = 0; i < objectTransforms.Count; i++)");
            // first disable all transforms
            for (var i = 0; i < objectTransforms.Count; i++)
            {
                for (var j = 0; j < objectTransforms[i].Count; j++)
                {
                    //debug.debugMessage("[IFS] - InterstellarMeshSwitch: Setting object enabled");

                    Transform transform = objectTransforms[i][j];
                    if (transform == null) continue;

                    transform.gameObject.SetActive(false);

                    if (!affectColliders) continue;

                    //debug.debugMessage("[IFS] - InterstellarMeshSwitch: setting collider states");
                    var collider = objectTransforms[i][j].gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = false;
                }
            }

            //Debug.Log("[IFS] - InterstellarMeshSwitch SetObject if (objectNumber >= 0 && objectNumber < objectTransforms.Count)");
            // enable the selected one last because there might be several entries with the same object, and we don't want to disable it after it's been enabled.
            if (objectNumber >= 0 && objectNumber < objectTransforms.Count)
            {
                for (var i = 0; i < objectTransforms[objectNumber].Count; i++)
                {
                    Transform transform = objectTransforms[objectNumber][i];
                    if (transform == null) continue;

                    transform.gameObject.SetActive(true);

                    if (!affectColliders) continue;

                    var colloder = transform.gameObject.GetComponent<Collider>();

                    if (colloder == null) continue;

                    //debug.debugMessage("[IFS] - InterstellarMeshSwitch: Setting collider true on new active object");
                    colloder.enabled = true;
                }
            }

            //Debug.Log("[IFS] - InterstellarMeshSwitch SetObject if (useFuelSwitchModule)");
            if (useFuelSwitchModule)
            {
                if (fuelSwitch != null && objectNumber >= 0 && objectNumber < fuelTankSetupList.Count)
                {
                    //debug.debugMessage("[IFS] - InterstellarMeshSwitch: calling on InterstellarFuelSwitch tank setup " + objectNumber);
                    fuelSwitch.SelectTankSetup(fuelTankSetupList[objectNumber], calledByPlayer);
                }
                else
                {
                    //debug.debugMessage("[IFS] - InterstellarMeshSwitch: no such fuel tank setup");
                }
            }

            //Debug.Log("[IFS] - InterstellarMeshSwitch SetObject SetCurrentObjectName())");
            SetCurrentObjectName();
            //Debug.Log("[IFS] - InterstellarMeshSwitch Finished SetObject)");
        }

        private void SetCurrentObjectName()
        {
            currentObjectName = selectedObject >= 0 && selectedObject < objectDisplayList.Count  ? Localizer.Format(objectDisplayList[selectedObject]) : "";
        }

        public override void OnStart(PartModule.StartState state)
        {
            InitializeData();

            //Debug.Log("[IFS] - InterstellarMeshSwitch SwitchToObject(selectedObject, false)");
            SwitchToObject(selectedObject, false);

            //Debug.Log("[IFS] - InterstellarMeshSwitch bind with currentObjectName");
            Fields["currentObjectName"].guiActiveEditor = showCurrentObjectName;

            //Debug.Log("[IFS] - InterstellarMeshSwitch bind with nextObjectEvent");
            var nextButton = Events["nextObjectEvent"];
            nextButton.guiActiveEditor = showSwitchButtons;

            //Debug.Log("[IFS] - InterstellarMeshSwitch bind with previousObjectEvent");
            var prevButton = Events["previousObjectEvent"];
            prevButton.guiActiveEditor = showSwitchButtons;

            //Debug.Log("[IFS] - InterstellarMeshSwitch bind with selectedObject");
            var chooseField = Fields["selectedObject"];
            chooseField.guiName = Localizer.Format(switcherDescription);
            chooseField.guiActiveEditor = hasSwitchChooseOption;

            var chooseOption = chooseField.uiControlEditor as UI_ChooseOption;
            if (chooseOption != null)
            {
                chooseOption.options = tankSwitchNamesList.ToArray();
                chooseOption.onFieldChanged = UpdateFromGUI;
            }

            if (!showPreviousButton) 
                Events["previousObjectEvent"].guiActiveEditor = false;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            //Debug.Log("[IFS] - InterstellarMeshSwitch UpdateFromGUI(BaseField field, object oldFieldValueObj");
            SwitchToObject(selectedObject, true);
        }

        public void InitializeData()
        {
            try
            {
                if (initialized) return;

                //Debug.Log("[IFS] - InterstellarMeshSwitch InitializeData()");


                //debug = new InterstellarDebugMessages(debugMode, "InterstellarMeshSwitch");
                
                // you can't have fuel switching without symmetry, it breaks the editor GUI.
                if (useFuelSwitchModule)
                    updateSymmetry = true;

                //Debug.Log("[IFS] - InterstellarMeshSwitch ParseObjectNames");
                ParseObjectNames();

                //Debug.Log("[IFS] - InterstellarMeshSwitch ParseTools.ParseNames(fuelTankSetups)");
                fuelTankSetupList = ParseTools.ParseNames(fuelTankSetups);

                //Debug.Log("[IFS] - InterstellarMeshSwitch ParseTools.ParseNames(objectDisplayNames)");
                objectDisplayList = ParseTools.ParseNames(objectDisplayNames);


                tankSwitchNamesList = new List<string>();

                // add any missing names
                //Debug.Log("[IFS] - InterstellarMeshSwitch ParseTools.ParseNames(tankSwitchNames)");
                var tankSwitchNamesListTmp = ParseTools.ParseNames(tankSwitchNames);

                //Debug.Log("[IFS] - InterstellarMeshSwitch for (var i = 0; i < objectDisplayList.Count; i++)");
                for (var i = 0; i < objectDisplayList.Count; i++)
                {
                    tankSwitchNamesList.Add(Localizer.Format(i < tankSwitchNamesListTmp.Count ? tankSwitchNamesListTmp[i] : objectDisplayList[i]));
                }

                //Debug.Log("[IFS] - InterstellarMeshSwitch if (useFuelSwitchModule)");
                if (useFuelSwitchModule)
                {
                    var fuelSwitches = part.FindModulesImplementing<InterstellarFuelSwitch>();

                    if (fuelSwitches.Any() && !string.IsNullOrEmpty(searchTankId))
                    {
                         fuelSwitch = fuelSwitches.FirstOrDefault(m => m.tankId == searchTankId);
                    }

                    if (fuelSwitch == null)
                        fuelSwitch = fuelSwitches.FirstOrDefault();

                    if (fuelSwitch == null)
                    {
                        useFuelSwitchModule = false;
                        //debug.debugMessage("[IFS] - no FSfuelSwitch module found, despite useFuelSwitchModule being true");
                    }
                    else //if (HighLogic.LoadedSceneIsFlight)
                    {
                        //Debug.Log("[IFS] - caling fuelSwitch FindMatchingConfig");
                        //debug.debugMessage("[IFS] - caling fuelSwitch FindMatchingConfig");


                        var matchingObject = fuelSwitch.FindMatchingConfig();

                        if (HighLogic.LoadedSceneIsFlight || matchingObject >= 0)
                        {
                            selectedObject = matchingObject;
                            Debug.LogWarning("[IFS] - selectedObject set to " + selectedObject);
                        }
                        //debug.debugMessage("[IFS] - selectedObject set to " + selectedObject);
                    }

                }
                initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - InterstellarMeshSwitch.InitializeData Error: " + e.Message);
                throw;
            }
        }

        public override string GetInfo()
        {
            if (showInfo)
            {
                var variantList = ParseTools.ParseNames(objectDisplayNames.Length > 0 ? objectDisplayNames : objects);

                var info = new StringBuilder();
                info.AppendLine(Localizer.Format("#LOC_IFS_MeshSwitch_GetInfo") + ":");

                foreach (var t in variantList)
                {
                    info.AppendLine(t);
                }
                return info.ToString();
            }
            else
                return string.Empty;
        }
    }
    
}
