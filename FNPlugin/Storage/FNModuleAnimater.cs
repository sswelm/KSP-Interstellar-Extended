using System;
using UnityEngine;
using KSP.Localization;

/* AdvancedAnimator was made by Christophe Savard (stupid_chris) and is licensed under CC-BY-SA. You are free to share and modify this code freely
 * under the attribution clause to me. You can contact me on the forums for more information. */

namespace FNPlugin
{
    public class FNModuleAnimator : PartModule
    {
        #region KSPFields
        [KSPField]
        public string animationName = string.Empty;
        [KSPField]
        public int layer = 1;
        [KSPField]
        public string guiEnableName = string.Empty;
        [KSPField]
        public string guiDisableName = string.Empty;
        [KSPField]
        public string actionEnableName = string.Empty;
        [KSPField]
        public string actionDisableName = string.Empty;
        [KSPField]
        public string actionToggleName = string.Empty;
        [KSPField]
        public float animationSpeed = 1f;
        [KSPField]
        public bool oneShot = false;
        [KSPField]
        public bool activeEditor = true;
        [KSPField]
        public bool activeFlight = true;
        [KSPField]
        public bool externalToEVAOnly = true;
        [KSPField]
        public bool activeUnfocused = true;
        [KSPField]
        public float unfocusedRange = 5f;
        [KSPField(isPersistant = true)]
        new public bool enabled = false;
        [KSPField(isPersistant = true)]
        public bool played = true;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ModuleAnimater_Status")]//Status
        public string status = "Enable";
        #endregion

        #region Fields
        private bool initiated = false;
        #endregion

        #region Part GUI
        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "#LOC_KSPIE_ModuleAnimater_Toggle", unfocusedRange = 5)]//Toggle
        public void GUIToggle()
        {
            if (this.enabled) { Disable(); }
            else { Enable(); }
        }
        #endregion

        #region Action Groups
        [KSPAction("Enable")]
        public void ActionEnable(KSPActionParam param)
        {
            Enable();
        }

        [KSPAction("Disable")]
        public void ActionDisable(KSPActionParam param)
        {
            Disable();
        }

        [KSPAction("Toggle")]
        public void ActionToggle(KSPActionParam param)
        {
            GUIToggle();
        }
        #endregion

        #region Methods
        private void Enable()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.oneShot && this.played) { return; }
                this.played = true;
            }
            if (CheckAnimationPlaying()) { PlayAnimation(this.animationSpeed, GetAnimationTime()); }
            else { PlayAnimation(this.animationSpeed, 0); }
            this.enabled = true;
            this.part.Effect("onAnimationDeploy");
            SetName();
        }

        private void Disable()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.oneShot && this.played) { return; }
                this.played = true;
            }
            if (CheckAnimationPlaying()) { PlayAnimation(-this.animationSpeed, GetAnimationTime()); }
            else { PlayAnimation(-this.animationSpeed, 1); }
            Events["GUIToggle"].guiName = guiDisableName;
            this.enabled = false;
            this.part.Effect("onAnimationRetract");
            SetName();
        }

        private void SetName()
        {
            BaseEvent toggle = Events["GUIToggle"];
            if (!string.IsNullOrEmpty(this.guiEnableName) && !string.IsNullOrEmpty(this.guiDisableName)) { toggle.guiName = this.enabled ? this.guiDisableName : this.guiEnableName; }
        }

        private void InitiateAnimation()
        {
            foreach (Animation animation in this.part.FindModelAnimators(this.animationName))
            {
                AnimationState state = animation[this.animationName];
                state.normalizedTime = this.enabled ? 1 : 0;
                state.normalizedSpeed = 0;
                state.enabled = false;
                state.wrapMode = WrapMode.Clamp;
                state.layer = layer;
                animation.Play(this.animationName);
            }
            this.initiated = true;
        }

        private void PlayAnimation(float animationSpeed, float animationTime)
        {
            //Plays the animation
            foreach (Animation animation in this.part.FindModelAnimators(this.animationName))
            {
                AnimationState state = animation[this.animationName];
                state.normalizedTime = animationTime;
                state.normalizedSpeed = animationSpeed;
                state.enabled = true;
                state.wrapMode = WrapMode.Clamp;
                animation.Play(this.animationName);
            }
        }

        private bool CheckAnimationPlaying()
        {
            //Checks if a given animation is playing
            foreach (Animation animation in this.part.FindModelAnimators(this.animationName))
            {
                return animation.IsPlaying(this.animationName);
            }
            return false;
        }

        private float GetAnimationTime()
        {
            foreach (Animation animation in this.part.FindModelAnimators(this.animationName))
            {
                return animation[this.animationName].normalizedTime;
            }
            return 0f;
        }
        #endregion

        #region Functions
        private void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (CheckAnimationPlaying())
            {
                if (this.enabled) { this.status = Localizer.Format("#LOC_KSPIE_ModuleAnimater_Deploying"); }//"Deploying..."
                else { this.status = Localizer.Format("#LOC_KSPIE_ModuleAnimater_Retracting"); }//"Retracting..."
            }
            else
            {
                if (this.oneShot && this.played) { this.status = Localizer.Format("#LOC_KSPIE_ModuleAnimater_Locked"); }//"Locked."
                else
                {
                    if (this.enabled) { this.status = Localizer.Format("#LOC_KSPIE_ModuleAnimater_Deployed"); }//"Deployed"
                    else { this.status = Localizer.Format("#LOC_KSPIE_ModuleAnimater_Retracted"); }//"Retracted"
                }
            }
        }

        private void LateUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (this.initiated)
            {
                foreach (Animation animation in this.part.FindModelAnimators(animationName))
                {
                    animation.Stop(animationName);
                }
                this.initiated = false;
            }
        }
        #endregion

        #region Overrides
        public override void OnStart(PartModule.StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor) { return; }

            //In case of errors
            if (string.IsNullOrEmpty(this.animationName) || this.part.FindModelAnimators(this.animationName).Length <= 0)
            {
                Events.ForEach(e => e.active = false);
                Actions.ForEach(a => a.active = false);
                return;
            }

            //Initiates the animation
            InitiateAnimation();

            //Sets the action groups/part GUI
            BaseEvent toggle = Events["GUIToggle"];
            BaseAction aEnable = Actions["ActionEnable"], aDisable = Actions["ActionDisable"], aToggle = Actions["ActionToggle"];

            toggle.guiActiveEditor = this.activeEditor;
            toggle.guiActive = this.activeFlight;
            toggle.guiActiveUnfocused = this.activeUnfocused;
            toggle.externalToEVAOnly = this.externalToEVAOnly;
            toggle.unfocusedRange = this.unfocusedRange;
            SetName();

            if (string.IsNullOrEmpty(this.actionEnableName)) { aEnable.active = false; }
            else { aEnable.guiName = this.actionEnableName; }

            if (string.IsNullOrEmpty(this.actionDisableName)) { aDisable.active = false; }
            else { aDisable.guiName = this.actionDisableName; }

            if (string.IsNullOrEmpty(this.actionToggleName)) { aToggle.active = false; }
            else { aToggle.guiName = this.actionToggleName; }
        }
        #endregion
    }
}
