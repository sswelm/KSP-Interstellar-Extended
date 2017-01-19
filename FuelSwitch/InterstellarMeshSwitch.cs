using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;
using System.Linq;

namespace InterstellarFuelSwitch
{
    public class InterstellarMeshSwitch : PartModule 
    {
        [KSPField]
        public int moduleID = 0;
        [KSPField]
        public string buttonName = "Next part variant";
        [KSPField]
        public string previousButtonName = "Prev part variant";
        [KSPField]
        public string switcherDescription = "Mesh";
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
        public int selectedObject = 0;

        private List<List<Transform>> objectTransforms = new List<List<Transform>>();
        private List<int> fuelTankSetupList = new List<int>();
        private List<string> objectDisplayList = new List<string>();
        private List<string> tankSwitchNamesList = new List<string>();

        private InterstellarFuelSwitch fuelSwitch;
        private InterstellarDebugMessages debug;

        private bool initialized = false;


        [KSPField(guiActiveEditor = false, guiName = "Current Variant")]
        public string currentObjectName = string.Empty;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "Next part variant")]
        public void nextObjectEvent()
        {
            selectedObject++;
            if (selectedObject >= objectDisplayList.Count)
                selectedObject = 0;

            switchToObject(selectedObject, true);            
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "Prev part variant")]
        public void previousObjectEvent()
        {
            selectedObject--;
            if (selectedObject < 0)
                selectedObject = objectDisplayList.Count - 1;

            switchToObject(selectedObject, true);            
        }

        private void parseObjectNames()
        {
            string[] objectBatchNames = objects.Split(';');
            if (objectBatchNames.Length < 1)
                debug.debugMessage("InterstellarMeshSwitch: Found no object names in the object list");
            else
            {
                objectTransforms.Clear();
                for (int batchCount = 0; batchCount < objectBatchNames.Length; batchCount++)
                {
                    List <Transform> newObjects = new List<Transform>();                        
                    string[] objectNames = objectBatchNames[batchCount].Split(',');
                    for (int objectCount = 0; objectCount < objectNames.Length; objectCount++)
                    {
                        Transform newTransform = part.FindModelTransform(objectNames[objectCount].Trim(' '));
                        if (newTransform != null)
                        {
                            newObjects.Add(newTransform);
                            debug.debugMessage("InterstellarMeshSwitch: added object to list: " + objectNames[objectCount]);
                        }
                        else
                            debug.debugMessage("InterstellarMeshSwitch: could not find object " + objectNames[objectCount]);
                    }
                    if (newObjects.Count > 0) 
                        objectTransforms.Add(newObjects);
                }
            }
        }

        private void switchToObject(int objectNumber, bool calledByPlayer)
        {
            setObject(objectNumber, calledByPlayer);

            if (!updateSymmetry)
                return;

            for (int i = 0; i < part.symmetryCounterparts.Count; i++)
            {
                InterstellarMeshSwitch[] symSwitch = part.symmetryCounterparts[i].GetComponents<InterstellarMeshSwitch>();
                for (int j = 0; j < symSwitch.Length; j++)
                {
                    if (symSwitch[j].moduleID == moduleID)
                    {
                        symSwitch[j].selectedObject = selectedObject;
                        symSwitch[j].setObject(objectNumber, calledByPlayer);
                    }
                }
            }

        }

        private void setObject(int objectNumber, bool calledByPlayer)
        {
            InitializeData();

            for (int i = 0; i < objectTransforms.Count; i++)
            {
                for (int j = 0; j < objectTransforms[i].Count; j++)
                {
                    debug.debugMessage("InterstellarMeshSwitch: Setting object enabled");
                    objectTransforms[i][j].gameObject.SetActive(false);
                    if (affectColliders)
                    {
                        debug.debugMessage("InterstellarMeshSwitch: setting collider states");
                        var collider = objectTransforms[i][j].gameObject.GetComponent<Collider>();
                        if (collider != null)
                            collider.enabled = false;
                    }
                }
            }

            if (objectNumber < objectTransforms.Count)
            {
                // enable the selected one last because there might be several entries with the same object, and we don't want to disable it after it's been enabled.
                for (int i = 0; i < objectTransforms[objectNumber].Count; i++)
                {
                    objectTransforms[objectNumber][i].gameObject.SetActive(true);
                    if (affectColliders)
                    {
                        var colloder = objectTransforms[objectNumber][i].gameObject.GetComponent<Collider>();
                        if (colloder != null)
                        {
                            debug.debugMessage("InterstellarMeshSwitch: Setting collider true on new active object");
                            colloder.enabled = true;
                        }
                    }
                }
            }


            if (useFuelSwitchModule)
            {
                debug.debugMessage("InterstellarMeshSwitch: calling on InterstellarFuelSwitch tank setup " + objectNumber);
                if (objectNumber < fuelTankSetupList.Count)
                    fuelSwitch.SelectTankSetup(fuelTankSetupList[objectNumber], calledByPlayer);
                else
                    debug.debugMessage("InterstellarMeshSwitch: no such fuel tank setup");
            }

            setCurrentObjectName();

        }

        private void setCurrentObjectName()
        {
            if (selectedObject > objectDisplayList.Count - 1)
                currentObjectName = "Unnamed"; 
            else
                currentObjectName = objectDisplayList[selectedObject];
        }

        public override void OnStart(PartModule.StartState state)
        {
            InitializeData();

            switchToObject(selectedObject, false);

            Fields["currentObjectName"].guiActiveEditor = showCurrentObjectName;

            var nextButton = Events["nextObjectEvent"];
            nextButton.guiName = buttonName;
            nextButton.guiActiveEditor = showSwitchButtons;

            var prevButton = Events["previousObjectEvent"];
            prevButton.guiName = previousButtonName;
            prevButton.guiActiveEditor = showSwitchButtons;

            var chooseField = Fields["selectedObject"];
            chooseField.guiName = switcherDescription;
            chooseField.guiActiveEditor = hasSwitchChooseOption;

            var chooseOption = chooseField.uiControlEditor as UI_ChooseOption;
            chooseOption.options = tankSwitchNamesList.ToArray();
            chooseOption.onFieldChanged = UpdateFromGUI;

            if (!showPreviousButton) 
                Events["previousObjectEvent"].guiActiveEditor = false;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            switchToObject(selectedObject, true);
        }

        public void InitializeData()
        {
            try
            {
                if (!initialized)
                {
                    debug = new InterstellarDebugMessages(debugMode, "InterstellarMeshSwitch");
                    // you can't have fuel switching without symmetry, it breaks the editor GUI.
                    if (useFuelSwitchModule)
                        updateSymmetry = true;

                    parseObjectNames();
                    fuelTankSetupList = ParseTools.ParseIntegers(fuelTankSetups);
                    objectDisplayList = ParseTools.ParseNames(objectDisplayNames);

                    tankSwitchNamesList = new List<string>();

                    // add any missing names
                    var tankSwitchNamesListTmp = ParseTools.ParseNames(tankSwitchNames);
                    for (int i = 0; i < objectDisplayList.Count; i++)
                    {
                        if (i < tankSwitchNamesListTmp.Count)
                            tankSwitchNamesList.Add(tankSwitchNamesListTmp[i]);
                        else
                            tankSwitchNamesList.Add(objectDisplayList[i]);
                    }

                    if (useFuelSwitchModule)
                    {
                        var fuelSwitches = part.FindModulesImplementing<InterstellarFuelSwitch>();

                        if (!String.IsNullOrEmpty(searchTankId))
                        {
                            fuelSwitch = fuelSwitches.FirstOrDefault(m => m.tankId == searchTankId);
                        }

                        if (fuelSwitch == null)
                            fuelSwitch = fuelSwitches.FirstOrDefault();

                        //searchTankId
                        if (fuelSwitch == null)
                        {
                            useFuelSwitchModule = false;
                            debug.debugMessage("no FSfuelSwitch module found, despite useFuelSwitchModule being true");
                        }
                    }
                    initialized = true;
                }
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
                List<string> variantList;
                if (objectDisplayNames.Length > 0)
                    variantList = ParseTools.ParseNames(objectDisplayNames);
                else
                    variantList = ParseTools.ParseNames(objects);

                StringBuilder info = new StringBuilder();
                info.AppendLine("Part variants available:");
                for (int i = 0; i < variantList.Count; i++)
                {
                    info.AppendLine(variantList[i]);
                }
                return info.ToString();
            }
            else
                return string.Empty;
        }
    }
    
}
