using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Extensions
{
    class ResourceBuffers
    {
        abstract public class Config
        {
            public String ResourceName { get; private set; }
            public PartResource BufferedResource { get; private set; }

            public Config(String resourceName)
            {
                this.ResourceName = resourceName;
            }

            public virtual bool UpdateRequired() { return true; }

            protected abstract void UpdateBufferForce();

            public virtual void Init(Part part)
            {
                BufferedResource = part.Resources[ResourceName];
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
            public double VariableMultiplier { get; private set; } = 1.0d;
            protected double BaseResourceMax { get; set; }
            private bool VariableChanged { get; set; } = false;

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
                if (BufferedResource != null)
                {
                    // Calculate amount to max ratio
                    var resourceRatio = Math.Max(0, Math.Min(1, BufferedResource.maxAmount > 0 ? BufferedResource.amount / BufferedResource.maxAmount : 0));
                    BufferedResource.maxAmount = Math.Max(0.0001, BaseResourceMax);
                    BufferedResource.amount = Math.Max(0, resourceRatio * BufferedResource.maxAmount);
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

            private bool Initialized { get; set; } = false;
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
                if (BufferedResource != null)
                {
                    float timeMultiplier = HighLogic.LoadedSceneIsFlight ? TimeWarp.fixedDeltaTime : 0.02f;
                    double maxWasteHeatRatio = ClampInitialMaxAmount && !Initialized ? 0.95d : 1.0d;

                    // Calculate amount to max ratio
                    var resourceRatio = Math.Max(0, Math.Min(maxWasteHeatRatio, BufferedResource.maxAmount > 0 ? BufferedResource.amount / BufferedResource.maxAmount : 0));
                    BufferedResource.maxAmount = Math.Max(0.0001, timeMultiplier * BaseResourceMax);
                    BufferedResource.amount = Math.Max(0, resourceRatio * BufferedResource.maxAmount);
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
                if (BufferedResource != null)
                {
                    InitialMaxAmount = BufferedResource.maxAmount;
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
                Debug.LogError("[KSPI] - Resource = " + resourceName + " doesn't have variable buffer config!");
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
