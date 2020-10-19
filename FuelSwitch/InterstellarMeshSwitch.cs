using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    public interface IHaveFuelTankSetup
    {
        void SwitchToFuelTankSetup(string fuelTankSetup);
    }

    class meshConfiguration
    {
        public List<Transform> objectTransforms;
        public string fuelTankSetup;
        public string objectDisplay;
        public string tankSwitchName;
        public string indexName;
    }

    [KSPModule("#LOC_IFS_MeshSwitch_moduleName")]
    public class InterstellarMeshSwitch : PartModule, IHaveFuelTankSetup
    {
        [KSPField]
        public string moduleID = "0";
        [KSPField]
        public string switcherDescription = "#LOC_IFS_MeshSwitch_MeshName";
        [KSPField]
        public string tankSwitchNames = string.Empty;
        [KSPField]
        public string indexNames = string.Empty;
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
        public bool orderByIndexNames = false;
        [KSPField]
        public bool showCurrentObjectName = false;
        [KSPField]
        public bool hasSwitchChooseOption = true;
        [KSPField]
        public bool initialized;
        [KSPField(guiActiveEditor = false, guiName = "#LOC_IFS_MeshSwitch_currentObjectName")]
        public string currentObjectName = string.Empty;

        [KSPField(isPersistant = true, guiActiveEditor = true)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
        public int selectedObject;
        
        private List<List<Transform>> objectTransforms = new List<List<Transform>>();
        private List<meshConfiguration> meshConfigurationList = new List<meshConfiguration>();
        private InterstellarFuelSwitch fuelSwitch;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "#LOC_IFS_MeshSwitch_nextSetup")]
        public void nextObjectEvent()
        {
            selectedObject++;
            if (selectedObject >= meshConfigurationList.Count)
                selectedObject = 0;

            SwitchToObject(selectedObject, true);            
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "#LOC_IFS_MeshSwitch_previousetup")]
        public void previousObjectEvent()
        {
            selectedObject--;
            if (selectedObject < 0)
                selectedObject = meshConfigurationList.Count - 1;

            SwitchToObject(selectedObject, true);        
        }

        private List<List<Transform>> ParseObjectNames()
        {
            var objectTransforms = new List<List<Transform>>();

            var objectBatchNames = objects.Split(';');
            if (objectBatchNames.Length > 0)
            {
                for (var batchCount = 0; batchCount < objectBatchNames.Length; batchCount++)
                {
                    var newObjects = new List<Transform>();
                    var objectNames = objectBatchNames[batchCount].Split(',');
                    for (var objectCount = 0; objectCount < objectNames.Length; objectCount++)
                    {
                        var newTransform = part.FindModelTransform(objectNames[objectCount].Trim(' '));
                        if (newTransform != null)
                            newObjects.Add(newTransform);
                        else
                            newObjects.Add(null);
                    }
                    objectTransforms.Add(newObjects);
                }
            }

            return objectTransforms; 
        }

        private void SwitchToObject(int objectNumber, bool calledByPlayer)
        {
            SetObject(objectNumber, calledByPlayer);

            if (HighLogic.LoadedSceneIsFlight || !updateSymmetry)
                return;

            for (var i = 0; i < part.symmetryCounterparts.Count; i++)
            {
                var symSwitch = part.symmetryCounterparts[i].GetComponents<InterstellarMeshSwitch>();
                for (var j = 0; j < symSwitch.Length; j++)
                {
                    var ims = symSwitch[j];
                    int prevObject = ims.selectedObject;
                    if (ims.moduleID == moduleID && prevObject != selectedObject)
                    {
                        ims.selectedObject = selectedObject;
                        symSwitch[j].SetObject(objectNumber, calledByPlayer);
                    }
                }
            }
        }

        private void SetObject(int objectNumber, bool calledByPlayer)
        {
            InitializeData();

            // first disable all transforms
            for (var i = 0; i < objectTransforms.Count; i++)
            {
                var currentTransforms = objectTransforms[i];

                for (var j = 0; j < currentTransforms.Count; j++)
                {
                    Transform transform = currentTransforms[j];
                    if (transform == null) continue;

                    transform.gameObject.SetActive(false);

                    if (!affectColliders) continue;

                    var collider = transform.gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = false;
                }
            }

            // enable the selected one last because there might be several entries with the same object, and we don't want to disable it after it's been enabled.
            if (objectNumber >= 0 && objectNumber < meshConfigurationList.Count)
            {
                var currentTransforms = meshConfigurationList[objectNumber].objectTransforms;

                for (var i = 0; i < currentTransforms.Count; i++)
                {
                    Transform transform = currentTransforms[i];
                    if (transform == null) continue;

                    transform.gameObject.SetActive(true);

                    if (!affectColliders) continue;

                    var colloder = transform.gameObject.GetComponent<Collider>();

                    if (colloder == null) continue;

                    colloder.enabled = true;
                }
            }

            if (useFuelSwitchModule && fuelSwitch != null && objectNumber >= 0 && objectNumber < meshConfigurationList.Count)
            {
                fuelSwitch.SelectTankSetup(meshConfigurationList[objectNumber].fuelTankSetup, calledByPlayer);
            }

            SetCurrentObjectName();
        }

        private void SetCurrentObjectName()
        {
            currentObjectName = selectedObject >= 0 && selectedObject < meshConfigurationList.Count ? Localizer.Format(meshConfigurationList[selectedObject].objectDisplay) : "";
        }

        public override void OnStart(PartModule.StartState state)
        {
            InitializeData(true);

            SwitchToObject(selectedObject, false);

            Fields["currentObjectName"].guiActiveEditor = showCurrentObjectName;

            var nextButton = Events["nextObjectEvent"];
            nextButton.guiActiveEditor = showSwitchButtons;

            var prevButton = Events["previousObjectEvent"];
            prevButton.guiActiveEditor = showSwitchButtons;

            var chooseField = Fields["selectedObject"];
            chooseField.guiName = Localizer.Format(switcherDescription);
            chooseField.guiActiveEditor = hasSwitchChooseOption;

            var chooseOption = chooseField.uiControlEditor as UI_ChooseOption;
            if (chooseOption != null)
            {
                chooseOption.options = meshConfigurationList.Select(m => m.tankSwitchName).ToArray();
                chooseOption.onFieldChanged = UpdateFromGUI;
            }

            if (!showPreviousButton) 
                Events["previousObjectEvent"].guiActiveEditor = false;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            SwitchToObject(selectedObject, true);
        }

        public void SwitchToFuelTankSetup(string fuelTankSetup)
        {
            var configuration = meshConfigurationList.FirstOrDefault(m => m.fuelTankSetup == fuelTankSetup);
            
            if (configuration != null)
                Debug.Log("[IFS]: SwitchToFuelTankSetup fuelTankSetup matches " + fuelTankSetup);

            if (configuration == null)
            {
                configuration = meshConfigurationList.FirstOrDefault(m => m.indexName == fuelTankSetup);
                if (configuration != null)
                    Debug.Log("[IFS]: SwitchToFuelTankSetup indexName matches " + fuelTankSetup);
            }

            if (configuration == null)
            {
                configuration = meshConfigurationList.FirstOrDefault(m => m.objectDisplay == fuelTankSetup);
                if (configuration != null)
                    Debug.Log("[IFS]: SwitchToFuelTankSetup objectDisplay matches " + fuelTankSetup);
            }

            if (configuration == null)
            {
                configuration = meshConfigurationList.FirstOrDefault(m => m.tankSwitchName == fuelTankSetup);
                if (configuration != null)
                    Debug.Log("[IFS]: SwitchToFuelTankSetup tankSwitchName matches " + fuelTankSetup);
            }

            if (configuration != null)
            {
                //Debug.Log("[IFS]: SwitchToFuelTankSetup found " + fuelTankSetup);
                selectedObject = meshConfigurationList.IndexOf(configuration);
                SwitchToObject(selectedObject, true);
            }
            else
                Debug.LogWarning("[IFS]: SwitchToFuelTankSetup is missing " + fuelTankSetup);
        }

        public void InitializeData(bool forced = false)
        {
            if (initialized && !forced) return;

            // you can't have fuel switching without symmetry, it breaks the editor GUI.
            if (useFuelSwitchModule)
                updateSymmetry = true;

            objectTransforms = ParseObjectNames();
            var fuelTankSetupList = ParseTools.ParseNames(fuelTankSetups);
            var objectDisplayList = ParseTools.ParseNames(objectDisplayNames);
            var indexNamesList = ParseTools.ParseNames(indexNames);
            var tankSwitchNamesList = ParseTools.ParseNames(tankSwitchNames);

            // we need to clear the list because InitializeData might be called multiple times
            meshConfigurationList.Clear();

            for (var i = 0; i < objectDisplayList.Count; i++)
            {
                meshConfigurationList.Add(new meshConfiguration()
                {
                    objectDisplay = objectDisplayList[i],
                    tankSwitchName = Localizer.Format(i < tankSwitchNamesList.Count ? tankSwitchNamesList[i] : objectDisplayList[i]),
                    indexName = i < indexNamesList.Count ? indexNamesList[i] : objectDisplayList[i],
                    fuelTankSetup = i < fuelTankSetupList.Count ? fuelTankSetupList[i] : objectDisplayList[i],
                    objectTransforms = i < objectTransforms.Count ? objectTransforms[i] : new List<Transform>()
                });
            }

            if (orderByIndexNames)
            {
                if (debugMode)
                    Debug.Log("[IFS] - InterstellarMeshSwitch " + part.GetInstanceID() + " order meshConfigurationList on indexName");
                meshConfigurationList.Sort((a, b) => a.indexName.CompareTo(b.indexName));
            }

            if (debugMode)
                foreach (var config in meshConfigurationList)
                {
                    Debug.Log("fuelTankSetup:" + config.fuelTankSetup + " indexName:" + config.indexName + " objectDisplay: " + config.objectDisplay + " tankSwitchName:" + config.tankSwitchName);
                }

            if (useFuelSwitchModule)
            {
                var fuelSwitches = part.FindModulesImplementing<InterstellarFuelSwitch>();

                if (fuelSwitches.Any() && !string.IsNullOrEmpty(searchTankId))
                        fuelSwitch = fuelSwitches.FirstOrDefault(m => m.tankId == searchTankId);

                if (fuelSwitch == null)
                    fuelSwitch = fuelSwitches.FirstOrDefault();

                if (fuelSwitch == null)
                    useFuelSwitchModule = false;
                else 
                {
                    var matchingObject = fuelSwitch.FindMatchingConfig(this);

                    if (HighLogic.LoadedSceneIsFlight || matchingObject >= 0)
                    {
                        if (debugMode)
                            Debug.Log("[IFS] - InterstellarMeshSwitch " + part.GetInstanceID() + " sets selectedObject to matching object " + matchingObject);
                        selectedObject = matchingObject;
                    }
                }
            }
            initialized = true;
        }

        public override string GetInfo()
        {
            if (showInfo)
            {
                var variantList = ParseTools.ParseNames(objectDisplayNames.Length > 0 ? objectDisplayNames : objects);

                var info = StringBuilderCache.Acquire();
                info.Append(Localizer.Format("#LOC_IFS_MeshSwitch_GetInfo")).AppendLine(":");

                foreach (var t in variantList)
                {
                    info.AppendLine(t);
                }
                return info.ToStringAndRelease();
            }
            else
                return string.Empty;
        }
    }    
}
