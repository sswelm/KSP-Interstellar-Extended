using System;
using UnityEngine;
using KSP.Localization;

namespace InterstellarFuelSwitch
{
    // Orginal Author: Shadowmage

    //simple animation module that should eliminate many of the issues associated with the stock animation system.
    //supports multiple animations per part through blending and layering
    //supports multiple same-named animations per part.
    //supports optional enabling of the animation in editor -or- flight scene
    //allows for custom name specification for action group and gui action names
    //?supports basic single resource use for things like lights
    //?supports basic resource generation for things like Fuel Cells 
    public class InterstellarAnimate : PartModule
    {
        //    CONFIG FIELD
        //    the name of the animation
        [KSPField]
        public string animationName;

        //    CONFIG FIELD
        //    the GUI name for the deploy action (and action group name for deploy action)
        [KSPField]
        public string actionDeployName = Localizer.Format("#LOC_IFS_Animate_Deploy");//"Deploy"

        //    CONFIG FIELD
        //    the GUI name for the retract action (and action group name for retract action)
        [KSPField]
        public string actionRetractName = Localizer.Format("#LOC_IFS_Animate_Retract");//"Retract"

        //    CONFIG FIELD
        //    the action group name for the 'toggle state' action
        [KSPField]
        public string actionToggleName = Localizer.Format("#LOC_IFS_Animate_ToggleStatus");//"Toggle Status"
        
        [KSPField]
        public string animStatusName = Localizer.Format("#LOC_IFS_Animate_AnimState");//"AnimState"
        
        public bool showAnimState = true;    

        //    CONFIG FIELD
        //    should the animation be available in the editor?
        [KSPField]
        public bool editorEnabled = true;

        //    CONFIG FIELD
        //    should the animation be available in flight?
        [KSPField]
        public bool flightEnabled = true;

        //    CONFIG FIELD
        //    should the animation be available in flight while shielded from airstream/occluded?
        [KSPField]
        public bool toggleShielded = false;

        //    CONFIG FIELD
        //    the layer to set the animation to
        [KSPField]
        public int animationLayer = 0;

        //    CONFIG FIELD
        //    used so that external modules can know what animation module they are supposed to control/query
        [KSPField]
        public int animationModule = 0;

        //  CONFIG FIELD
        //    used to track module deployment status for on reload.
        [KSPField(isPersistant=true, guiName = "#LOC_IFS_Animate_AnimState", guiActive = true)]//AnimState
        public string deployedStatus = Localizer.Format("#LOC_IFS_Animate_Retract").ToUpper();//"RETRACTED"

        private SSTUAnimState animationState = SSTUAnimState.RETRACTED;

        //cached list of animationData
        private Animation[] deployAnimation;    

        //panel state enum, each represents a discrete state
        public enum SSTUAnimState
        {
            EXTENDED,
            EXTENDING,
            RETRACTED,
            RETRACTING,
        }

        // default constructor
        public InterstellarAnimate()
        {

        }

        #region KSP Actions

        //KSP Action Group 'Deploy'
        [KSPAction("Deploy")]
        public void deployAction(KSPActionParam param)
        {
            toggle ();
        }

        //KSP Action Group 'Deploy'
        [KSPAction("Retract")]
        public void retractAction(KSPActionParam param)
        {
            toggle ();
        }

        //KSP Action Group 'Toggle'
        [KSPAction("Toggle")]
        public void toggleAction(KSPActionParam param)
        {
            toggle ();
        }

        [KSPEvent (name= "deployEvent", guiName = "#LOC_IFS_Animate_Deploy", guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = true, unfocusedRange = 4f, guiActiveEditor = true)]//Deploy
        public void deployEvent()
        {
            toggle ();
        }

        [KSPEvent (name= "retractEvent", guiName = "#LOC_IFS_Animate_Retract", guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = true, unfocusedRange = 4f, guiActiveEditor = true)]//Retract
        public void retractEvent()
        {
            toggle ();
        }

        #endregion

        #region KSP Methods

        public override void OnStart (StartState state)
        {
            base.OnStart (state);
            print ("SSTUAnimate OnStart");
            initializeAnimation ();
            if(animationState != SSTUAnimState.RETRACTED)
            {
                if(animationState==SSTUAnimState.EXTENDED || animationState==SSTUAnimState.EXTENDING)
                {
                    playAnimationForward();                    
                    setAnimationNormTime(1);//should set everything to the 'extended' state
                    setAnimationState(SSTUAnimState.EXTENDED);
                }
                else if(animationState==SSTUAnimState.RETRACTING)
                {
                    playAnimationReverse();
                    setAnimationNormTime(0);
                    setAnimationState(SSTUAnimState.RETRACTED);
                }
            }            
            initializeGuiLabels ();
            updateGuiLabels();//force initial status update for gui labels
        }

        public override void OnLoad (ConfigNode node)
        {
            base.OnLoad (node);
            print ("SSTUAnimate OnLoad");
            
            //parse any previously saved deployedStatus value, or fallback to RETRACTED if none found/errors occur
            try
            {
                animationState = (SSTUAnimState)Enum.Parse(typeof(SSTUAnimState), deployedStatus);
            }
            catch(Exception ex)
            {
                Debug.LogError("[KSPI]: InterstellarAnimate in OnLoad: " + ex.Message);
                animationState = SSTUAnimState.RETRACTED;
            }
            updateGuiLabels();
        }

        //update tracked animation state for the part based on actual animation status
        public override void OnUpdate()
        {
            //should check animation state to see if should progress to next state
            if (animationState == SSTUAnimState.EXTENDING && !isAnimationPlaying ())
            {
                print ("Animation finished playing forwards");
                setAnimationState (SSTUAnimState.EXTENDED);
                updateGuiLabels();
            }
            else if (animationState == SSTUAnimState.RETRACTING && !isAnimationPlaying ())
            {
                print ("Animation finished playing reverse");
                setAnimationState (SSTUAnimState.RETRACTED);
                updateGuiLabels();
            }
            else
            {

            }
            //TODO update action and event status
        }

        #endregion

        #region public accessibilty methods
        // to be used by external modules checking on the state of this animation
        public SSTUAnimState getAnimationState()
        {
            return animationState;
        }

        #endregion

        #region private utility methods

        private void toggle()
        {
            print ("SSTUAnimate toggle -- state: "+animationState);
            if (animationState == SSTUAnimState.EXTENDED || animationState == SSTUAnimState.EXTENDING)
            {
                playAnimationReverse ();
                if(animationState == SSTUAnimState.EXTENDED)
                {
                    setAnimationNormTime(1);
                }
                setAnimationState(SSTUAnimState.RETRACTING);    
                updateGuiLabels();
            }
            else if (animationState == SSTUAnimState.RETRACTED || animationState == SSTUAnimState.RETRACTING)
            {
                //check for shielded from airstream status if animation cares about such things
                if(toggleShielded)
                {

                    if(part!=null && part.ShieldedFromAirstream)
                    {
                        print ("Cannot deploy while shielded from airstream");
                        //TODO print proper message to GUI
                        return;
                    }
                }
                if(animationState==SSTUAnimState.RETRACTED)
                {
                    setAnimationNormTime(0);
                }
                playAnimationForward();
                setAnimationState(SSTUAnimState.EXTENDING);
                updateGuiLabels();
            }
        }
        
        //update GUI labels only when called -- updates their availability based on current animation state
        private void updateGuiLabels()
        {            
            print ("SSTUAnimate updateGuiLabels");
            if(animationState==SSTUAnimState.EXTENDED || animationState==SSTUAnimState.EXTENDING)
            {                
                Events ["deployEvent"].guiActiveEditor = false;
                Events ["deployEvent"].guiActive = false;
                
                Events ["retractEvent"].guiActiveEditor = editorEnabled;
                Events ["retractEvent"].guiActive = flightEnabled;            
            }
            else
            {                
                Events ["deployEvent"].guiActiveEditor = editorEnabled;
                Events ["deployEvent"].guiActive = flightEnabled;
                
                Events ["retractEvent"].guiActiveEditor = false;
                Events ["retractEvent"].guiActive = false;
            }
        }

        //internal call from toggle
        private void playAnimationForward()
        {
            print ("SSTUAnimate playAnimationForward");
            setAnimationSpeed (1);
            setAnimationEnabled (true);
            startAnimation ();
        }

        //internal call from toggle
        private void playAnimationReverse()
        {
            print ("SSTUAnimate playAnimationReverse");
            setAnimationSpeed (-1);
            setAnimationEnabled (true);
            startAnimation ();
        }

        //internal call to set animationState enum value
        private void setAnimationState(SSTUAnimState newState)
        {
            print ("SSTUAnimate setAnimationState");
            animationState = newState;
            deployedStatus = animationState.ToString();
        }

        //internal call to set the animationSpeed of the currently referenced animation(s)
        private void setAnimationSpeed(float speed)
        {
            print ("SSTUAnimate setAnimationSpeed");
            foreach(Animation anim in deployAnimation)
            {
                anim[animationName].speed = speed;
            }
        }

        //internal call to set the normalizedTime of the currently referenced animation(s)
        private void setAnimationNormTime(float time)
        {
            print ("SSTUAnimate setAnimationNormTime");
            foreach(Animation anim in deployAnimation)
            {
                anim[animationName].normalizedTime = time;
            }
        }

        //internal call to enable or disable status of the currently referenced animation(s)
        private void setAnimationEnabled(bool enabled)
        {
            print ("SSTUAnimate setAnimationEnabled");
            foreach(Animation anim in deployAnimation)
            {
                anim[animationName].enabled = enabled;
            }
        }

        //internal call to start playing the currently referenced animation(s) at the currently set speed and time
        private void startAnimation()
        {
            print ("SSTUAnimate startAnimation");
            setAnimationEnabled (true);
            foreach(Animation anim in deployAnimation)
            {
                anim.Play(animationName);
            }
        }

        //internal call to stop playing the currently referenced animation at its current time, does not update/set to start/end
        private void stopAnimation()
        {
            print ("SSTUAnimate stopAnimation");
            setAnimationEnabled (false);
            foreach(Animation anim in deployAnimation)
            {
                anim.Stop(animationName);
            }
        }

        ///internal call to check all referenced animation(s) to see if any of them are still playing.
        //will return true if -any- animation is playing, will only return false if -no- animations are still playing
        private bool isAnimationPlaying()
        {
            bool playing = false;
            foreach(Animation anim in deployAnimation)
            {
                if(anim[animationName].enabled)
                {
                    playing = true;
                }
            }
            return playing;
        }

        //internal call to find and set up animations from config with config values
        private void initializeAnimation()
        {
            print ("SSTUAnimate initializeAnimation");
            deployAnimation = part.FindModelAnimators(animationName);
            if (deployAnimation == null || deployAnimation.Length <= 0) 
            {
                print ("Could not find or load animation for name: "+animationName);
                return;
            }
            setAnimationSpeed(1);//set to play forward by default
            setAnimationNormTime(0);//and set the start time to 0 (beginning) by default
            foreach(Animation anim in deployAnimation)
            {
                anim[animationName].layer = animationLayer; //set to the config animation layer
                anim[animationName].blendMode = AnimationBlendMode.Blend;//enable animation blending, for increased chance for animations to play nicely
                anim[animationName].wrapMode = WrapMode.Once;//use WrapMode.Once to enforce self-ending animation
            }
        }
                
        //initialize GUI labels from OnStart, sets their initial names and availability from the config values
        private void initializeGuiLabels()
        {
            print ("SSTUAnimate initializeGuiLabels");
            Actions ["deployAction"].guiName = actionDeployName;
            Actions ["retractAction"].guiName = actionRetractName;
            Actions ["toggleAction"].guiName = actionToggleName;
            
            Events ["deployEvent"].guiName = actionDeployName;
            Events ["deployEvent"].guiActiveEditor = editorEnabled;
            Events ["deployEvent"].guiActive = flightEnabled;
            
            Events ["retractEvent"].guiName = actionRetractName;
            Events ["retractEvent"].guiActiveEditor = editorEnabled;
            Events ["retractEvent"].guiActive = flightEnabled;
            
            Fields ["deployedStatus"].guiName = animStatusName;
            Fields ["deployedStatus"].guiActive = showAnimState;
        }

        #endregion

    }
}

