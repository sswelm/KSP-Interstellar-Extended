using FNPlugin.Power;
using System;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Generator Adapter")]
    class FNGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Generator current power", guiUnits = " MW", guiFormat = "F5")]
        public double megaJouleGeneratorPowerSupply;
        [KSPField()]
        public int index = 0;
        
        private ModuleGenerator moduleGenerator;

        private ResourceType outputType = 0;
        private bool active = false;
        private ResourceBuffers resourceBuffers;
        private ModuleResource mockInputResource;
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


                resourceBuffers = new ResourceBuffers();
                outputType = ResourceType.other;
                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.outputResources)
                {
                    // assuming only one of those two is present
                    if (moduleResource.name == ResourceManager.FNRESOURCE_MEGAJOULES)
                    {
                        outputType = ResourceType.megajoule;
                        resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(ResourceManager.FNRESOURCE_MEGAJOULES, 50));

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
                        resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, 50));

                        mockInputResource = new ModuleResource();
                        mockInputResource.name = moduleResource.name;
                        mockInputResource.id = moduleResource.name.GetHashCode();
                        moduleGenerator.resHandler.inputResources.Add(mockInputResource);
                        moduleOutputResource = moduleResource;
                        break;
                    }
                }
                resourceBuffers.Init(this.part);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnStart " + e.Message);
                throw;
            }
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
                if (moduleGenerator == null) return;

                if (outputType == ResourceType.other) return;

                double generatorRate = moduleOutputResource.rate;
                mockInputResource.rate = generatorRate;

                double generatorSupply = outputType == ResourceType.megajoule ? generatorRate : generatorRate / 1000;

                resourceBuffers.UpdateBuffers();

                megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(generatorSupply, generatorSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
    }
}
