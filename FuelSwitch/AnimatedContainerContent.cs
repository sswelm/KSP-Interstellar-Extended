using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//AnimatedContainer allows an animation to correspond with the percentage of a particular resource in a container.

namespace InterstellarFuelSwitch
{
    public class AnimatedContainerContent : PartModule
    {
        [KSPField(isPersistant = false)]
        public string animationName;
        [KSPField(isPersistant = false)]
        public string resourceName;
        [KSPField(isPersistant = false)]
        public double animationExponent = 1;

        [KSPField(isPersistant = false, guiName = "Animation Ratio",  guiActiveEditor = true, guiActive = true, guiFormat = "F3")]
        public float animationRatio;

        private AnimationState[] containerStates;
        private PartResource animatedResource;
        

        public override void OnStart(PartModule.StartState state)
        {
            containerStates = SetUpAnimation(animationName, this.part);

            animatedResource = part.Resources[resourceName];
        }

        void Update()
        {
            animationRatio = (float)Math.Round( Math.Pow(animatedResource.amount / animatedResource.maxAmount, animationExponent), 3);

            foreach (var cs in containerStates)
            {
                cs.normalizedTime = animationRatio;
            }
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }
    }
}

