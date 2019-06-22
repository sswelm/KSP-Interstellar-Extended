using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    public class FNAnimation : PartModule, IMultipleDragCube
    {
        private List<IAnimatedModule> _Modules;
        private bool _hasBeenInitialized = false;

        private void FindModules()
        {
            if (vessel != null)
            {
                _Modules = part.FindModulesImplementing<IAnimatedModule>();
            }
        }

        [KSPField]
        public string startEventGUIName = "";

        [KSPField]
        public string endEventGUIName = "";

        [KSPField]
        public string actionGUIName = "";

        [KSPField]
        public int CrewCapacity = 0;

        [KSPField]
        public string deployAnimationName = "Deploy";

        [KSPField]
        public string secondaryAnimationName = "";

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        [KSPField(isPersistant = true, guiName = "Deployed", guiFormat = "P2")]
        public double partialDeployCostPaid = 0d;

        [KSPField(isPersistant = true)]
        public float inflatedCost = 0;

        [KSPField]
        public bool inflatable = false;

        [KSPField]
        public int PrimaryLayer = 2;

        [KSPField]
        public int SecondaryLayer = 3;

        [KSPField]
        public float inflatedMultiplier = -1;

        [KSPField]
        public bool shedOnInflate = false;

        [KSPField]
        public string ResourceCosts = "";

        [KSPField]
        public string ReplacementResource = "Construction";

        [KSPAction("Deploy Module")]
        public void DeployAction(KSPActionParam param)
        {
            DeployModule();
        }


        [KSPAction("Retract Module")]
        public void RetractAction(KSPActionParam param)
        {
            RetractModule();
        }


        [KSPAction("Toggle Module")]
        public void ToggleAction(KSPActionParam param)
        {
            if (isDeployed)
            {
                RetractModule();
            }
            else
            {
                DeployModule();
            }
        }

        public Animation DeployAnimation
        {
            get { return part.FindModelAnimators(deployAnimationName)[0]; }
        }

        public override void OnStart(StartState state)
        {
            Initialize();
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                CheckAnimationState();
            }
            catch (Exception ex)
            {
                print("ERROR IN USIAnimationOnLoad - " + ex.Message);
            }
        }

        public Animation SecondaryAnimation
        {
            get
            {
                try
                {
                    return part.FindModelAnimators(secondaryAnimationName)[0];
                }
                catch (Exception)
                {
                    print("[OKS] Could not find secondary animation - " + secondaryAnimationName);
                    return null;
                }
            }
        }

        [KSPEvent(guiName = "Deploy", guiActive = true, externalToEVAOnly = true, guiActiveEditor = true, active = true,
            guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void DeployModule()
        {
            if (!isDeployed)
            {
                if (!CheckAndConsumeResources())
                    return;

                if (CheckDeployConditions())
                {
                    PlayDeployAnimation();
                    ToggleEvent("DeployModule", false);
                    ToggleEvent("RetractModule", true);
                    CheckDeployConditions();
                    isDeployed = true;
                    EnableModules();
                    SetControlSurface(true);
                }
            }
        }

        private bool CheckDeployConditions()
        {
            if (inflatable)
            {
                if (shedOnInflate && !HighLogic.LoadedSceneIsEditor)
                {
                    for (int i = part.children.Count - 1; i >= 0; i--)
                    {
                        var p = part.children[i];
                        var pNode = p.srfAttachNode;
                        if (pNode.attachedPart == part)
                        {
                            p.decouple(0f);
                        }
                    }
                }

                if (inflatedMultiplier > 0)
                    ExpandResourceCapacity();
                if (CrewCapacity > 0)
                {
                    part.CrewCapacity = CrewCapacity;
                    if (CrewCapacity > 0)
                    {
                        part.CheckTransferDialog();
                        MonoUtilities.RefreshContextWindows(part);
                    }
                }
                var mods = part.FindModulesImplementing<ModuleResourceConverter>();
                var count = mods.Count;
                for (int i = 0; i < count; ++i)
                {
                    var m = mods[i];
                    m.EnableModule();
                }
                MonoUtilities.RefreshContextWindows(part);
            }
            return true;
        }

        private bool CheckAndConsumeResources()
        {
            var res = part.Resources[ReplacementResource];
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (res != null)
                    res.amount = res.maxAmount;
                return true;
            }

            if (ResCosts.Count == 0)
            {
                return true;
            }

            var resourcesNeeded = 1d - partialDeployCostPaid;
            var resourcesAvailable = FindResources();
            if (resourcesNeeded - resourcesAvailable > ResourceUtilities.FLOAT_TOLERANCE)
            {
                if (resourcesAvailable > ResourceUtilities.FLOAT_TOLERANCE)
                {
                    ConsumeResources(resourcesAvailable);
                    partialDeployCostPaid += resourcesAvailable;
                    Fields["partialDeployCostPaid"].guiActive = true;
                    DisplayMessage("Partially assembling module using: ", resourcesAvailable);
                    resourcesNeeded -= resourcesAvailable;
                }
                DisplayMessage("Missing resources to assemble module: ", resourcesNeeded);
                return false;
            }
            else
            {
                DisplayMessage("Assembling module using: ", resourcesNeeded);
                ConsumeResources(resourcesNeeded);
                partialDeployCostPaid = 0d;
                Fields["partialDeployCostPaid"].guiActive = false;
                return true;
            }
        }

        private void DisplayMessage(string header, double resourcesPercentage)
        {
            var resourcesText = String.Join(", ",
                ResCosts.Select(r => String.Format("{0:0} {1}", r.Ratio * resourcesPercentage, r.ResourceName)).ToArray());
            ScreenMessages.PostScreenMessage(header + resourcesText, 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        private double FindResources()
        {
            //return ResCosts.Select(FindResources).Min();

            return 1;
        }

        //private double FindResources(ResourceRatio resInfo)
        //{
        //    var resourceName = resInfo.ResourceName;
        //    var needed = resInfo.Ratio;
        //    if (needed < ResourceUtilities.FLOAT_TOLERANCE)
        //    {
        //        return 1d;
        //    }
        //    var available = 0d;
        //    var sourceParts = LogisticsTools.GetRegionalWarehouses(vessel, "USI_ModuleResourceWarehouse");

        //    foreach (var sourcePart in sourceParts)
        //    {
        //        if (sourcePart == part)
        //            continue;

        //        var warehouse = sourcePart.FindModuleImplementing<USI_ModuleResourceWarehouse>();

        //        if (resInfo.ResourceName != "ElectricCharge" && warehouse != null) //EC we're a lot less picky...
        //        {
        //            if (!warehouse.localTransferEnabled)
        //                continue;
        //        }
        //        if (sourcePart.Resources.Contains(resourceName))
        //        {
        //            available += sourcePart.Resources[resourceName].amount;
        //            if (available >= needed)
        //            {
        //                return 1d;
        //            }
        //        }
        //    }
        //    return available / needed;
        //}

        private void ConsumeResources(double percentage)
        {
            foreach (var resource in ResCosts)
            {
                //ConsumeResource(resource, percentage);
            }
        }

        //private void ConsumeResource(ResourceRatio resInfo, double percentage)
        //{
        //    var resourceName = resInfo.ResourceName;
        //    var needed = resInfo.Ratio * percentage;
        //    //Pull in from warehouses

        //    var sourceParts = LogisticsTools.GetRegionalWarehouses(vessel, "USI_ModuleResourceWarehouse");
        //    foreach (var sourcePart in sourceParts)
        //    {
        //        if (sourcePart == part)
        //            continue;
        //        var warehouse = sourcePart.FindModuleImplementing<USI_ModuleResourceWarehouse>();
        //        if (warehouse != null)
        //        {
        //            if (!warehouse.localTransferEnabled)
        //                continue;
        //        }
        //        if (sourcePart.Resources.Contains(resourceName))
        //        {
        //            var res = sourcePart.Resources[resourceName];
        //            if (res.amount >= needed)
        //            {
        //                res.amount -= needed;
        //                needed = 0;
        //                break;
        //            }
        //            else
        //            {
        //                needed -= res.amount;
        //                res.amount = 0;
        //            }
        //        }
        //    }
        //}


        [KSPEvent(guiName = "Retract", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false,
            active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void RetractModule()
        {
            if (isDeployed)
            {
                if (CheckRetractConditions())
                {
                    isDeployed = false;
                    ReverseDeployAnimation();
                    ToggleEvent("DeployModule", true);
                    ToggleEvent("RetractModule", false);
                    DisableModules();
                    SetControlSurface(false);
                    var res = part.Resources[ReplacementResource];
                    if (res != null)
                        res.amount = 0;
                }
            }
        }

        private bool CheckRetractConditions()
        {
            var canRetract = true;
            if (inflatable)
            {
                if (part.protoModuleCrew.Count > 0)
                {
                    var msg = string.Format("Unable to deflate {0} as it still contains crew members.",
                        part.partInfo.title);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                    canRetract = false;
                }
                if (canRetract)
                {
                    part.CrewCapacity = 0;
                    if (inflatedMultiplier > 0)
                        CompressResourceCapacity();
                    var modList = GetAffectedMods();
                    var count = modList.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        modList[i].DisableModule();
                    }
                    MonoUtilities.RefreshContextWindows(part);
                }
            }
            return canRetract;
        }

        public List<ModuleResourceConverter> GetAffectedMods()
        {
            var modList = new List<ModuleResourceConverter>();
            var modNames = new List<string> { "ModuleResourceConverter", "ModuleLifeSupportRecycler" };

            for (int i = 0; i < part.Modules.Count; i++)
            {
                if (modNames.Contains(part.Modules[i].moduleName))
                    modList.Add((ModuleResourceConverter)part.Modules[i]);
            }
            return modList;
        }

        private void PlayDeployAnimation(int speed = 1)
        {
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);
            SetDragState(1f);
        }

        public void ReverseDeployAnimation(int speed = -1)
        {
            if (secondaryAnimationName != "")
            {
                SecondaryAnimation.Stop(secondaryAnimationName);
            }
            DeployAnimation[deployAnimationName].time = DeployAnimation[deployAnimationName].length;
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);
            SetDragState(0f);
        }

        private void ToggleEvent(string eventName, bool state)
        {
            if (ResourceCosts != string.Empty)
            {
                Events[eventName].active = state;
                Events[eventName].guiActiveUnfocused = state;
                Events[eventName].externalToEVAOnly = true;
                Events[eventName].guiActive = false;
                Events[eventName].guiActiveEditor = state;
            }
            else
            {
                Events[eventName].active = state;
                Events[eventName].externalToEVAOnly = false;
                Events[eventName].guiActiveUnfocused = false;
                Events[eventName].guiActive = state;
                Events[eventName].guiActiveEditor = state;
            }
            if (inflatedMultiplier > 0)
            {
                Events[eventName].guiActiveEditor = false;
            }
        }


        public void Initialize()
        {
            try
            {
                _hasBeenInitialized = true;
                FindModules();
                SetupResourceCosts();
                SetupDeployMenus();
                DeployAnimation[deployAnimationName].layer = PrimaryLayer;
                if (secondaryAnimationName != "")
                {
                    SecondaryAnimation[secondaryAnimationName].layer = SecondaryLayer;
                }
                CheckAnimationState();
                UpdatemenuNames();
            }
            catch (Exception ex)
            {
                print("ERROR IN USI Animation Initialize - " + ex.Message);
            }
        }

        private void UpdatemenuNames()
        {
            if (startEventGUIName != "")
            {
                Events["DeployModule"].guiName = startEventGUIName;
                Actions["DeployAction"].guiName = startEventGUIName;
            }
            if (endEventGUIName != "")
            {
                Events["RetractModule"].guiName = endEventGUIName;
                Actions["RetractAction"].guiName = endEventGUIName;
            }
            if (actionGUIName != "")
                Actions["ToggleAction"].guiName = actionGUIName;
        }

        private void SetupDeployMenus()
        {
            if (ResourceCosts != String.Empty)
            {
                Events["DeployModule"].guiActiveUnfocused = true;
                Events["DeployModule"].externalToEVAOnly = true;
                Events["DeployModule"].unfocusedRange = 10f;
                Events["DeployModule"].guiActive = false;
                Events["RetractModule"].guiActive = false;
                Events["RetractModule"].guiActiveUnfocused = true;
                Events["RetractModule"].externalToEVAOnly = true;
                Events["RetractModule"].unfocusedRange = 10f;

                Actions["DeployAction"].active = false;
                Actions["RetractAction"].active = false;
                Actions["ToggleAction"].active = false;
            }
        }


        private void DisableModules()
        {
            if (vessel == null || _Modules == null) return;
            for (int i = 0, iC = _Modules.Count; i < iC; ++i)
            {
                _Modules[i].DisableModule();
            }
        }

        private void EnableModules()
        {
            if (vessel == null || _Modules == null)
                return;

            for (int i = 0, iC = _Modules.Count; i < iC; ++i)
            {
                var mod = _Modules[i];
                if (mod.IsSituationValid())
                    mod.EnableModule();
            }
        }

        private void SetControlSurface(bool state)
        {
            var mcs = part.FindModuleImplementing<ModuleControlSurface>();
            if (mcs == null)
                return;

            mcs.ignorePitch = !state;
            mcs.ignoreRoll = !state;
            mcs.ignoreYaw = !state;
            mcs.isEnabled = state;
        }


        private void CheckAnimationState()
        {
            if (part.protoModuleCrew.Count > 0 && inflatable)
            {
                //We got them in here somehow....
                isDeployed = true;
            }
            if (isDeployed)
            {
                ToggleEvent("DeployModule", false);
                ToggleEvent("RetractModule", true);
                PlayDeployAnimation(1000);
                CheckDeployConditions();
                EnableModules();
            }
            else
            {
                ToggleEvent("DeployModule", true);
                ToggleEvent("RetractModule", false);
                ReverseDeployAnimation(-1000);
                DisableModules();
            }
            SetControlSurface(isDeployed);
        }

        private void ExpandResourceCapacity()
        {
            try
            {
                var rCount = part.Resources.Count;
                for (int i = 0; i < rCount; ++i)
                {
                    var res = part.Resources[i];
                    if (res.maxAmount < inflatedMultiplier)
                    {
                        double oldMaxAmount = res.maxAmount;
                        res.maxAmount *= inflatedMultiplier;
                        inflatedCost += (float)((res.maxAmount - oldMaxAmount) * res.info.unitCost);
                    }
                }
            }
            catch (Exception ex)
            {
                print("Error in ExpandResourceCapacity - " + ex.Message);
            }
        }

        private void CompressResourceCapacity()
        {
            try
            {
                var rCount = part.Resources.Count;
                for (int i = 0; i < rCount; ++i)
                {
                    var res = part.Resources[i];
                    if (res.maxAmount > inflatedMultiplier)
                    {
                        res.maxAmount /= inflatedMultiplier;
                        if (res.amount > res.maxAmount)
                            res.amount = res.maxAmount;
                    }
                }
                inflatedCost = 0.0f;
            }
            catch (Exception ex)
            {
                print("Error in CompressResourceCapacity - " + ex.Message);
            }
        }


        public void FixedUpdate()
        {
            if (!_hasBeenInitialized)
                Initialize();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (isDeployed && secondaryAnimationName != "")
            {
                try
                {
                    if (!SecondaryAnimation.isPlaying && !DeployAnimation.isPlaying)
                    {
                        SecondaryAnimation[secondaryAnimationName].speed = 1;
                        SecondaryAnimation.Play(secondaryAnimationName);
                    }
                }
                catch (Exception ex)
                {
                    print("Error in OnUpdate - USI Animation - " + ex.Message);
                }
            }
        }

        public List<ResourceRatio> ResCosts;

        private void SetupResourceCosts()
        {
            ResCosts = new List<ResourceRatio>();
            if (String.IsNullOrEmpty(ResourceCosts))
                return;

            var resources = ResourceCosts.Split(',');
            for (int i = 0; i < resources.Length; i += 2)
            {
                ResCosts.Add(new ResourceRatio
                {
                    ResourceName = resources[i],
                    Ratio = double.Parse(resources[i + 1])
                });
            }
        }

        private void SetDragState(float b)
        {
            part.DragCubes.SetCubeWeight("A", b);
            part.DragCubes.SetCubeWeight("B", 1f - b);

            if (part.DragCubes.Procedural)
                part.DragCubes.ForceUpdate(true, true);
        }

        public override string GetInfo()
        {
            if (String.IsNullOrEmpty(ResourceCosts))
                return "";

            var output = new StringBuilder("Resource Cost:\n\n");
            var resources = ResourceCosts.Split(',');
            for (int i = 0; i < resources.Length; i += 2)
            {
                output.Append(string.Format("{0} {1}\n", double.Parse(resources[i + 1]), resources[i]));
            }
            return output.ToString();
        }

        public string[] GetDragCubeNames()
        {
            return new string[] { "A", "B" };
        }

        public bool UsesProceduralDragCubes()
        {
            return false;
        }

        public bool IsMultipleCubesActive { get { return true; } }

        public void AssumeDragCubePosition(string name)
        {
            var anim = part.FindModelAnimators(deployAnimationName)[0];
            if (anim == null)
            {
                enabled = false;
                return;
            }

            if (anim[deployAnimationName] == null)
            {
                enabled = false;
                return;
            }


            anim[deployAnimationName].speed = 0f;
            anim[deployAnimationName].enabled = true;
            anim[deployAnimationName].weight = 1f;

            switch (name)
            {
                case "A":
                    anim[deployAnimationName].normalizedTime = 1f;
                    break;
                case "B":
                    anim[deployAnimationName].normalizedTime = 0f;
                    break;
            }
        }
    }
}



