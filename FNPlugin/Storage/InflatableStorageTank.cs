using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//AnimatedContainer allows an animation to correspond with the percentage of a particular resource or all resources in a container.

namespace FNPlugin
{
    [KSPModule("Inflatable Storage Tank")]
    public class InflatableStorageTank : PartModule
    {
        [KSPField(isPersistant = false)]
        public string animationName;
        [KSPField(isPersistant = false)]
        public string resourceName = "";
        [KSPField(isPersistant = false)]
        public double animationExponent = 1;
        [KSPField(isPersistant = false)]
        public double maximumRatio = 1;

        [KSPField(isPersistant = false, guiName = "Animation Ratio",  guiActiveEditor = true, guiActive = true, guiFormat = "F3")]
        public float animationRatio;

        private AnimationState[] containerStates;

        public override void OnStart(PartModule.StartState state)
        {
            containerStates = SetUpAnimation(animationName, this.part);           
        }

        void Update()
        {
            double resourceRatio = -1;

            if (!String.IsNullOrEmpty(resourceName))
            {
                PartResource animatedResource = part.Resources.FirstOrDefault(m => m.resourceName == resourceName);

                if (animatedResource != null)
                    resourceRatio = animatedResource.amount / animatedResource.maxAmount;
            }

            if (resourceRatio == -1)
            {
                var allResources = part.Resources.Where(m => m != null);

                var sumMaxAmount = allResources.Sum(m => m.maxAmount);
                var sumAmount = allResources.Sum(m => m.amount);

                resourceRatio = sumMaxAmount > 0 ? sumAmount / sumMaxAmount : 0;
            }

            var multiplier = maximumRatio > 0 ? 1 / maximumRatio : 1;
            animationRatio = (float)Math.Round(Math.Pow(Math.Min(multiplier * resourceRatio, 1), animationExponent), 3);

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

