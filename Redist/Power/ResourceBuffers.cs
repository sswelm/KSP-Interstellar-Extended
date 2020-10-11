using System;
using System.Collections.Generic;
using UnityEngine;

namespace FNPlugin.Power
{
    public class ResourceBuffers
    {
        abstract public class Config
        {
            public String ResourceName { get; private set; }

            public Config(String resourceName)
            {
                this.ResourceName = resourceName;
            }

            public virtual bool UpdateRequired() { return true; }

            protected abstract void UpdateBufferForce();

            protected Part part;

            public virtual void Init(Part part)
            {
                this.part = part;
            }

            public virtual void UpdateBuffer()
            {
                if (UpdateRequired())
                {
                    UpdateBufferForce();
                }
            }
        }

        public class VariableConfig : Config
        {
            private double _variableMultiplier = 1;
            public double VariableMultiplier 
            { 
                get {  return _variableMultiplier;  } 
                private set {_variableMultiplier = value; } 
            }

            protected double BaseResourceMax { get; set; }

            private bool VariableChanged = false;

            public VariableConfig(String resourceName) : base(resourceName) { }

            protected virtual void RecalculateBaseResourceMax()
            {
                BaseResourceMax = VariableMultiplier;
            }

            public void ConfigureVariable(double variableMultiplier)
            {
                if (this.VariableMultiplier != variableMultiplier)
                {
                    VariableChanged = true;
                    this.VariableMultiplier = variableMultiplier;
                    RecalculateBaseResourceMax();
                }
            }

            protected override void UpdateBufferForce()
            {
                var bufferedResource = part.Resources[ResourceName];
                if (bufferedResource != null)
                {
                    var resourceRatio = Math.Max(0, Math.Min(1, bufferedResource.maxAmount > 0 ? bufferedResource.amount / bufferedResource.maxAmount : 0));
                    bufferedResource.maxAmount = Math.Max(0.0001, BaseResourceMax);
                    bufferedResource.amount = Math.Max(0, resourceRatio * bufferedResource.maxAmount);
                }
            }

            public override bool UpdateRequired()
            {
                bool updateRequired = false;
                if (VariableChanged)
                {
                    updateRequired = true;
                    VariableChanged = false;
                }
                return updateRequired;
            }

        }

        public class TimeBasedConfig : VariableConfig
        {
            public bool ClampInitialMaxAmount { get; private set; }
            public double ResourceMultiplier { get; private set; }
            public double BaseResourceAmount { get; private set; }

            private bool Initialized = false;
            private float PreviousDeltaTime { get; set; }

            public TimeBasedConfig(String resourceName, double resourceMultiplier = 1.0d, double baseResourceAmount = 1.0d, bool clampInitialMaxAmount = false)
                : base(resourceName)
            {
                this.ClampInitialMaxAmount = clampInitialMaxAmount;
                this.ResourceMultiplier = resourceMultiplier;
                this.BaseResourceAmount = baseResourceAmount;
                RecalculateBaseResourceMax();
            }

            protected override void RecalculateBaseResourceMax()
            {
                // calculate Resource Capacity
                this.BaseResourceMax = ResourceMultiplier * BaseResourceAmount * VariableMultiplier;
            }

            protected override void UpdateBufferForce()
            {
                var bufferedResource = part.Resources[ResourceName];
                if (bufferedResource != null)
                {
                    double timeMultiplier = HighLogic.LoadedSceneIsFlight ? TimeWarp.fixedDeltaTime : 0.02;
                    double maxWasteHeatRatio = ClampInitialMaxAmount && !Initialized ? 0.95d : 1.0d;

                    var resourceRatio = Math.Max(0, Math.Min(maxWasteHeatRatio, bufferedResource.maxAmount > 0 ? bufferedResource.amount / bufferedResource.maxAmount : 0));
                    bufferedResource.maxAmount = Math.Max(0.0001, timeMultiplier * BaseResourceMax);
                    bufferedResource.amount = Math.Max(0, resourceRatio * bufferedResource.maxAmount);
                }
                Initialized = true;
            }

            public override bool UpdateRequired()
            {
                bool updateRequired = false;
                if (Math.Abs(TimeWarp.fixedDeltaTime - PreviousDeltaTime) > float.Epsilon || base.UpdateRequired())
                {
                    updateRequired = true;
                    PreviousDeltaTime = TimeWarp.fixedDeltaTime;
                }
                return updateRequired;
            }
        }

        public class MaxAmountConfig : TimeBasedConfig
        {
            public double InitialMaxAmount { get; private set; }
            public double MaxMultiplier { get; private set; }

            public MaxAmountConfig(String resourceName, double maxMultiplier)
                : base(resourceName, 1.0d, 1.0d, false)
            {
                this.MaxMultiplier = maxMultiplier;
            }

            public override void Init(Part part)
            {
                base.Init(part);
                var bufferedResource = part.Resources[ResourceName];
                if (bufferedResource != null)
                {
                    InitialMaxAmount = bufferedResource.maxAmount;
                    RecalculateBaseResourceMax();
                }
            }

            protected override void RecalculateBaseResourceMax()
            {
                // calculate Resource Capacity
                this.BaseResourceMax = InitialMaxAmount * MaxMultiplier;
            }
        }

        protected Dictionary<String, Config> resourceConfigs;

        public ResourceBuffers()
        {
            this.resourceConfigs = new Dictionary<String, Config>();
        }

        public void AddConfiguration(Config resourceConfig)
        {
            resourceConfigs.Add(resourceConfig.ResourceName, resourceConfig);
        }

        public void Init(Part part)
        {
            foreach (Config resourceConfig in resourceConfigs.Values)
            {
                resourceConfig.Init(part);
            }
            UpdateBuffers();
        }

        public void UpdateVariable(String resourceName, double variableMultiplier)
        {
            Config resourceConfig = resourceConfigs[resourceName];
            if (resourceConfig != null && resourceConfig is VariableConfig)
            {
                (resourceConfig as VariableConfig).ConfigureVariable(variableMultiplier);
            }
            else
            {
                Debug.LogError("[KSPI]: Resource = " + resourceName + " doesn't have variable buffer config!");
            }
        }

        public void UpdateBuffers()
        {
            foreach (Config resourceConfig in resourceConfigs.Values)
            {
                resourceConfig.UpdateBuffer();
            }
        }
    }
}
