using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Generator Adapter")]
    class FNGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Generator current power", guiUnits = " MW", guiFormat = "F5")]
        public double megaJouleGeneratorPowerSupply; 
        
        private ModuleGenerator moduleGenerator;
        private PartResourceDefinition outputDefinition;
        private PartResource megajoulePartResource;
        private PartResource electricChargePartResource;

        private resourceType outputType = 0;
        private bool active = false;
        private float previousDeltaTime;
        private double fixedMegajouleBufferSize;
        private double fixedElectricChargeBufferSize;
        private ModuleResource mockInputResource;

        public override void OnStart(StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                if (part.Modules.Contains("ModuleGenerator"))
                {
                    moduleGenerator = part.FindModuleImplementing<ModuleGenerator>();
                }

                if (moduleGenerator == null) return;

                part.force_activate();

                String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
                this.resources_to_supply = resources_to_supply;
                base.OnStart(state);

                previousDeltaTime = TimeWarp.fixedDeltaTime;

                outputType = resourceType.other;
                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.outputResources)
                {
                    // assuming only one of those two is present
                    if (moduleResource.name == ResourceManager.FNRESOURCE_MEGAJOULES)
                    {
                        outputType = resourceType.megajoule;

                        megajoulePartResource = part.Resources[ResourceManager.FNRESOURCE_MEGAJOULES];
                        if (megajoulePartResource != null)
                        {
                            fixedMegajouleBufferSize = megajoulePartResource.maxAmount * 50;
                        }
                        outputDefinition = PartResourceLibrary.Instance.GetDefinition(moduleResource.name);

                        mockInputResource = new ModuleResource();
                        mockInputResource.name = moduleResource.name;
                        resHandler.inputResources.Add(mockInputResource);

                        break;
                    }
                    if (moduleResource.name == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                    {
                        outputType = resourceType.electricCharge;

                        electricChargePartResource = part.Resources[ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE];
                        if (electricChargePartResource != null)
                        {
                            fixedElectricChargeBufferSize = electricChargePartResource.maxAmount * 50;
                        }
                        outputDefinition = PartResourceLibrary.Instance.GetDefinition(moduleResource.name);

                        mockInputResource = new ModuleResource();
                        mockInputResource.name = moduleResource.name;
                        resHandler.inputResources.Add(mockInputResource);

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNGeneratorAdapter.OnStart " + e.Message);
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
                Debug.LogError("[KSPI] - Exception in FNGeneratorAdapter.OnFixedUpdate " + e.Message);
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
                Debug.LogError("[KSPI] - Exception in FNGeneratorAdapter.OnFixedUpdate " + e.Message);
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

                if (outputType == resourceType.other) return;

                if (megajoulePartResource != null && fixedMegajouleBufferSize > 0 && TimeWarp.fixedDeltaTime != previousDeltaTime)
                {
                    double requiredMegawattCapacity = fixedMegajouleBufferSize * TimeWarp.fixedDeltaTime;
                    double previousMegawattCapacity = fixedMegajouleBufferSize * previousDeltaTime;
                    double ratio = megajoulePartResource.amount / megajoulePartResource.maxAmount;

                    megajoulePartResource.maxAmount = requiredMegawattCapacity;
                    megajoulePartResource.amount = TimeWarp.fixedDeltaTime > previousDeltaTime
                        ? Math.Max(0, Math.Min(requiredMegawattCapacity, megajoulePartResource.amount + requiredMegawattCapacity - previousMegawattCapacity))
                        : Math.Max(0, Math.Min(requiredMegawattCapacity, ratio * requiredMegawattCapacity));
                }

                if (electricChargePartResource != null && fixedElectricChargeBufferSize > 0 && TimeWarp.fixedDeltaTime != previousDeltaTime)
                {
                    double requiredElectricChargeCapacity = fixedElectricChargeBufferSize * TimeWarp.fixedDeltaTime;
                    double previousPreviousElectricCapacity = fixedElectricChargeBufferSize * previousDeltaTime;
                    double ratio = electricChargePartResource.amount / electricChargePartResource.maxAmount;

                    electricChargePartResource.maxAmount = requiredElectricChargeCapacity;
                    electricChargePartResource.amount = TimeWarp.fixedDeltaTime > previousDeltaTime
                        ? Math.Max(0, Math.Min(requiredElectricChargeCapacity, electricChargePartResource.amount + requiredElectricChargeCapacity - previousPreviousElectricCapacity))
                        : Math.Max(0, Math.Min(requiredElectricChargeCapacity, ratio * requiredElectricChargeCapacity));
                }
                previousDeltaTime = TimeWarp.fixedDeltaTime;

                ModuleResource mainResource = moduleGenerator.resHandler.outputResources[0];
                double generatorRate = mainResource.isDeprived ? 0 : mainResource.rate;

                // extract power otherwise we end up with double power
                mockInputResource.rate = generatorRate;

                double generatorSupply = outputType == resourceType.megajoule ? generatorRate : generatorRate / 1000;

                megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(generatorSupply, generatorSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNGeneratorAdapter.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
    }
}
