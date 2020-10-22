using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Animated Container allows an animation to correspond with the percentage of a particular resource or all resources in a container.

namespace FNPlugin
{
    [KSPModule("Inflatable Storage Tank")]
    public class InflatableStorageTank : AnimatedContainerContent { };

    [KSPModule("Animated Container")]
    public class AnimatedContainerContent : PartModule
    {
        [KSPField(isPersistant = false)]
        public string animationName;
        [KSPField(isPersistant = false)]
        public string resourceName;
        [KSPField(isPersistant = false)]
        public double animationExponent = 1;
        [KSPField(isPersistant = false)]
        public double maximumRatio = 1;

        [KSPField(isPersistant = false, guiName = "#LOC_IFS_AnimatedContainerContent_AnimationRatio", guiActiveEditor = false, guiActive = false, guiFormat = "F3")]//Animation Ratio
        public float animationRatio;

        private AnimationState[] containerStates;

        public override void OnStart(PartModule.StartState state)
        {
            containerStates = SetUpAnimation(animationName, this.part);           
        }

        void Update()
        {
            double resourceRatio = -1;

            if (!string.IsNullOrEmpty(resourceName))
            {
                var animatedResource = part.Resources[resourceName];
                if (animatedResource != null)
                    resourceRatio = animatedResource.maxAmount > 0 ? animatedResource.amount / animatedResource.maxAmount : 0;
            }

            if (resourceRatio == -1)
            {
                var resourcesWithDensity = part.Resources.Where(m => m.info.density > 0).ToList();
                if (resourcesWithDensity.Count == 0)
                    resourcesWithDensity = part.Resources.ToList();

                var sumMaxAmount = resourcesWithDensity.Sum(m => m.maxAmount);
                var sumAmount = resourcesWithDensity.Sum(m => m.amount);
                resourceRatio = sumMaxAmount > 0 ? sumAmount / sumMaxAmount : 0;
            }

            var multiplier = maximumRatio == 1 ? 1 : maximumRatio > 0 ? 1 / maximumRatio : 1;
            var multipledRatio = multiplier == 1 ? resourceRatio : Math.Min(multiplier * resourceRatio, 1);
            var manipulatedRatio = animationExponent == 1 ? multipledRatio : Math.Pow(multipledRatio, animationExponent);

            animationRatio = (float)Math.Round(manipulatedRatio, 3);

            foreach (var cs in containerStates)
            {
                cs.normalizedTime = animationRatio;
            }
        }

        private static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
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

