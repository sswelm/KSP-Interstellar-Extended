using FNPlugin.Power;
using System;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Generator Adapter")]
    class FNGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FNGeneratorAdapter_CurrentPower", guiUnits = " MW", guiFormat = "F6")]//Generator current power
        public double megaJouleGeneratorPowerSupply;
        [KSPField]
        public int index = 0;
        [KSPField]
        public bool maintainsBuffer = true;
        [KSPField]
        public bool showDisplayStatus = true;
        [KSPField]
        public bool showEfficiency = true;
        [KSPField]
        public bool active;

        private ModuleGenerator moduleGenerator;
        private ResourceType outputType = 0;
        private ResourceBuffers resourceBuffers;
        private ModuleResource mockInputResource;
        private ModuleResource moduleOutputResource;
        private BaseField efficiencyField;
        private BaseField displayStatusField;

        public override void OnStart(StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                var beamedPowerReceiver = part.FindModuleImplementing<BeamedPowerReceiver>();
                if (beamedPowerReceiver != null)
                {
                    Debug.LogWarning("[KSPI]: disabling FNGeneratorAdapter, found BeamedPowerReceiver");
                    return;
                }

                var generator = part.FindModuleImplementing<FNGenerator>();
                if (generator != null)
                {
                    Debug.LogWarning("[KSPI]: disabling FNGeneratorAdapter, found FNGenerator");
                    return;
                }

                var modules = part.FindModulesImplementing<ModuleGenerator>();

                moduleGenerator = modules.Count > index ? modules[index] : null;

                if (moduleGenerator == null)
                {
                    Debug.LogWarning("[KSPI]: disabling FNGeneratorAdapter, failed to find ModuleGenerator");
                    return;
                }

                string[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
                this.resources_to_supply = resources_to_supply;
                base.OnStart(state);

                if (maintainsBuffer)
                    resourceBuffers = new ResourceBuffers();

                outputType = ResourceType.other;
                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.outputResources)
                {
                    if (moduleResource.name != ResourceManager.FNRESOURCE_MEGAJOULES && (moduleResource.name != ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE))
                        continue;

                    // assuming only one of those two is present
                    if (moduleResource.name == ResourceManager.FNRESOURCE_MEGAJOULES)
                        outputType = ResourceType.megajoule;
                    else
                        outputType = ResourceType.electricCharge;

                    if (maintainsBuffer)
                        resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(moduleResource.name, 50));

                    mockInputResource = new ModuleResource();
                    mockInputResource.name = moduleResource.name;
                    mockInputResource.id = moduleResource.name.GetHashCode();
                    moduleGenerator.resHandler.inputResources.Add(mockInputResource);
                    moduleOutputResource = moduleResource;
                    break;
                }

                if (maintainsBuffer)
                    resourceBuffers.Init(this.part);

                efficiencyField = moduleGenerator.Fields["efficiency"];
                displayStatusField = moduleGenerator.Fields["displayStatus"];

                efficiencyField.guiActive = showEfficiency;
                displayStatusField.guiActive = showDisplayStatus;
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

                var generatorRate = moduleOutputResource.rate;
                mockInputResource.rate = generatorRate;

                double generatorSupply = outputType == ResourceType.megajoule ? generatorRate : generatorRate / 1000;

                if (maintainsBuffer)
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