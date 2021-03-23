using KSP.Localization;
using UnityEngine;

/* AdvancedAnimator was made by Christophe Savard (stupid_chris) and is licensed under CC-BY-SA. You are free to share and modify this code freely
 * under the attribution clause to me. You can contact me on the forums for more information. */

namespace FNPlugin.Storage
{
    public class FNModuleAnimator : PartModule
    {
        #region KSPFields
        [KSPField] public string animationName = string.Empty;
        [KSPField] public int layer = 1;
        [KSPField] public string guiEnableName = string.Empty;
        [KSPField] public string guiDisableName = string.Empty;
        [KSPField] public string actionEnableName = string.Empty;
        [KSPField] public string actionDisableName = string.Empty;
        [KSPField] public string actionToggleName = string.Empty;
        [KSPField] public float animationSpeed = 1f;
        [KSPField] public bool oneShot = false;
        [KSPField] public bool activeEditor = true;
        [KSPField] public bool activeFlight = true;
        [KSPField] public bool externalToEVAOnly = true;
        [KSPField] public bool activeUnfocused = true;
        [KSPField] public float unfocusedRange = 5f;

        [KSPField(isPersistant = true)] new public bool enabled;
        [KSPField(isPersistant = true)] public bool played = true;

        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ModuleAnimater_Status")] public string status = "Enable";
        #endregion

        #region Fields
        private bool initiated;
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
                if (oneShot && played) { return; }
                played = true;
            }

            if (CheckAnimationPlaying())
                PlayAnimation(animationSpeed, GetAnimationTime());
            else
                PlayAnimation(animationSpeed, 0);

            enabled = true;
            part.Effect("onAnimationDeploy");
            SetName();
        }

        private void Disable()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (oneShot && played) { return; }
                played = true;
            }

            if (CheckAnimationPlaying())
                PlayAnimation(-animationSpeed, GetAnimationTime());
            else
                PlayAnimation(-animationSpeed, 1);

            Events[nameof(GUIToggle)].guiName = guiDisableName;
            enabled = false;
            part.Effect("onAnimationRetract");
            SetName();
        }

        private void SetName()
        {
            BaseEvent toggle = Events["GUIToggle"];
            if (!string.IsNullOrEmpty(guiEnableName) && !string.IsNullOrEmpty(guiDisableName))
            {
                toggle.guiName = enabled ? guiDisableName : guiEnableName;
            }
        }

        private void InitiateAnimation()
        {
            foreach (Animation animation in part.FindModelAnimators(animationName))
            {
                AnimationState state = animation[animationName];
                state.normalizedTime = enabled ? 1 : 0;
                state.normalizedSpeed = 0;
                state.enabled = false;
                state.wrapMode = WrapMode.Clamp;
                state.layer = layer;
                animation.Play(animationName);
            }
            initiated = true;
        }

        private void PlayAnimation(float animationSpeed, float animationTime)
        {
            //Plays the animation
            foreach (Animation animation in part.FindModelAnimators(animationName))
            {
                AnimationState state = animation[animationName];
                state.normalizedTime = animationTime;
                state.normalizedSpeed = animationSpeed;
                state.enabled = true;
                state.wrapMode = WrapMode.Clamp;
                animation.Play(animationName);
            }
        }

        private bool CheckAnimationPlaying()
        {
            //Checks if a given animation is playing
            foreach (Animation animation in part.FindModelAnimators(animationName))
            {
                return animation.IsPlaying(animationName);
            }
            return false;
        }

        private float GetAnimationTime()
        {
            foreach (Animation animation in part.FindModelAnimators(animationName))
            {
                return animation[animationName].normalizedTime;
            }
            return 0f;
        }
        #endregion

        #region Functions
        private void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (CheckAnimationPlaying())
                status = Localizer.Format(enabled ? "#LOC_KSPIE_ModuleAnimater_Deploying" : "#LOC_KSPIE_ModuleAnimater_Retracting");
            else
            {
                if (oneShot && played)
                    status = Localizer.Format("#LOC_KSPIE_ModuleAnimater_Locked");//"Locked."
                else
                    status = Localizer.Format(enabled ? "#LOC_KSPIE_ModuleAnimater_Deployed" : "#LOC_KSPIE_ModuleAnimater_Retracted");
            }
        }

        private void LateUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!initiated) return;

            foreach (Animation animation in part.FindModelAnimators(animationName))
            {
                animation.Stop(animationName);
            }
            initiated = false;
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
                return;

            //In case of errors
            if (string.IsNullOrEmpty(animationName) || part.FindModelAnimators(animationName).Length <= 0)
            {
                Events.ForEach(e => e.active = false);
                Actions.ForEach(a => a.active = false);
                return;
            }

            //Initiates the animation
            InitiateAnimation();

            //Sets the action groups/part GUI
            BaseEvent toggle = Events[nameof(GUIToggle)];

            BaseAction aEnable = Actions[nameof(ActionEnable)], aDisable = Actions[nameof(ActionDisable)], aToggle = Actions[nameof(ActionToggle)];

            toggle.guiActiveEditor = activeEditor;
            toggle.guiActive = activeFlight;
            toggle.guiActiveUnfocused = activeUnfocused;
            toggle.externalToEVAOnly = externalToEVAOnly;
            toggle.unfocusedRange = unfocusedRange;
            SetName();

            if (string.IsNullOrEmpty(actionEnableName))
                aEnable.active = false;
            else
                aEnable.guiName = actionEnableName;

            if (string.IsNullOrEmpty(actionDisableName))
                aDisable.active = false;
            else
                aDisable.guiName = actionDisableName;

            if (string.IsNullOrEmpty(actionToggleName))
                aToggle.active = false;
            else
                aToggle.guiName = actionToggleName;
        }
        #endregion
    }
}
