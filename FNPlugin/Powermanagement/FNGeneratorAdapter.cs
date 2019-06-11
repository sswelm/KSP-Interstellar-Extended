using FNPlugin.Power;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Generator Adapter")]
    class FNGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Power input", guiUnits = " MW", guiFormat = "F5")]
        public double powerGeneratorPowerInput;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Power output", guiUnits = " MW", guiFormat = "F5")]
        public double powerGeneratorPowerOutput;

        [KSPField]
        public double inputRate = 0;
        [KSPField]
        public bool showDisplayStatus = true;
        [KSPField]
        public bool showEfficiency = true;
        [KSPField]
        public bool showPowerInput = true;
        [KSPField]
        public bool showPowerOuput = true;
        [KSPField]
        public int index = 0;
		[KSPField]
		public bool maintainsBuffer = true;

        
        private ModuleGenerator moduleGenerator;

        private ResourceType outputType = 0;
        private ResourceType inputType = 0;

        private bool active = false;
        private ResourceBuffers resourceBuffers;
        private ModuleResource mockInputResource;
        
        private ModuleResource moduleInputResource;
        private ModuleResource moduleOutputResource;

        public override void OnStart(StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                var modules = part.FindModulesImplementing<ModuleGenerator>();

                moduleGenerator = modules.Count > index ? modules[index] : null;

                if (moduleGenerator == null) return;

                String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
                this.resources_to_supply = resources_to_supply;
                base.OnStart(state);

				if (maintainsBuffer)
					resourceBuffers = new ResourceBuffers();

                outputType = ResourceType.other;
                inputType = ResourceType.other;

                moduleInputResource = moduleGenerator.resHandler.inputResources.FirstOrDefault();
                if (moduleInputResource != null)
                {
                    if (moduleInputResource.name == ResourceManager.FNRESOURCE_MEGAJOULES)
                        inputType = ResourceType.megajoule;
                    else if (moduleInputResource.name == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                        inputType = ResourceType.electricCharge;

                    if (inputRate != 0)
                        moduleInputResource.rate = inputRate;
                }

                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.outputResources)
                {
                    // assuming only one of those two is present
                    if (moduleResource.name == ResourceManager.FNRESOURCE_MEGAJOULES)
                    {
                        outputType = ResourceType.megajoule;

						if (maintainsBuffer)
                            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES, moduleResource.rate));

                        mockInputResource = new ModuleResource();
                        mockInputResource.name = moduleResource.name;
                        mockInputResource.id = moduleResource.name.GetHashCode();
                        moduleGenerator.resHandler.inputResources.Add(mockInputResource);
                        moduleOutputResource = moduleResource;
                        break;
                    }
                    if (moduleResource.name == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                    {
                        outputType = ResourceType.electricCharge;

						if (maintainsBuffer)
                            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, moduleResource.rate));

                        mockInputResource = new ModuleResource();
                        mockInputResource.name = moduleResource.name;
                        mockInputResource.id = moduleResource.name.GetHashCode();
                        moduleGenerator.resHandler.inputResources.Add(mockInputResource);
                        moduleOutputResource = moduleResource;
                        break;
                    }
                }

				if (maintainsBuffer)
					resourceBuffers.Init(this.part);

                Fields["powerGeneratorPowerInput"].guiName = Fields["powerGeneratorPowerInput"].guiName + " " + (index + 1);
                Fields["powerGeneratorPowerOutput"].guiName = Fields["powerGeneratorPowerOutput"].guiName + " " + (index + 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnStart " + e.Message);
                throw;
            }
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            if (moduleGenerator == null)
                return;

            moduleGenerator.Fields["efficiency"].guiActive = showEfficiency;
            moduleGenerator.Fields["displayStatus"].guiActive = showDisplayStatus;

            this.Fields["powerGeneratorPowerInput"].guiActive = moduleInputResource != null &&  showPowerInput;
            this.Fields["powerGeneratorPowerOutput"].guiActive = moduleOutputResource != null && showPowerOuput;
        }

        public override void OnFixedUpdate()
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight) return;

                if (moduleGenerator == null) return;

                active = true;
                base.OnFixedUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnFixedUpdate " + e.Message);
                throw;
            }
        }


        public void FixedUpdate()
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight) return;

                if (moduleGenerator == null) return;

                if (!active)
                    base.OnFixedUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnFixedUpdate " + e.Message);
                throw;
            }
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title;
        }

        public override int getPowerPriority()
        {
            return 1;
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            try
            {
                powerGeneratorPowerOutput = 0;
                powerGeneratorPowerInput = 0;

                if (moduleGenerator == null) return;

                if (outputType == ResourceType.other) return;

                if (!moduleGenerator.generatorIsActive && !moduleGenerator.isAlwaysActive)
                    return;

                if (maintainsBuffer)
                    resourceBuffers.UpdateBuffers();

                if (moduleOutputResource != null)
                {
                    double generatorOutputRate = moduleOutputResource.rate;
                    mockInputResource.rate = generatorOutputRate;

                    double generatorSupply = outputType == ResourceType.megajoule ? generatorOutputRate : generatorOutputRate / 1000;

                    powerGeneratorPowerOutput = supplyFNResourcePerSecondWithMax(generatorSupply, generatorSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
                }

                if (moduleInputResource != null)
                {
                    double generatorInputRate = moduleInputResource.rate;
                    powerGeneratorPowerInput = inputType == ResourceType.megajoule ? generatorInputRate : generatorInputRate / 1000;
                }

            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
    }
}
