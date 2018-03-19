using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Near Future Fission Generator Adapter")]
    class FNFissionGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Generator current power", guiUnits = " MW", guiFormat = "F5")]
        public double megaJouleGeneratorPowerSupply;

        private PartModule moduleGenerator;
        private BaseField _field_status;
        private BaseField _field_generated;
        private BaseField _field_addedToTanks;
        private BaseField _field_max;

        private bool active = false;
        private float previousDeltaTime;
        private double fixedElectricChargeBufferSize;

        public override void OnStart(StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                if (part.Modules.Contains("FissionGenerator"))
                {
                    moduleGenerator = part.Modules["FissionGenerator"];
                    _field_status = moduleGenerator.Fields["Status"];
                    _field_generated = moduleGenerator.Fields["CurrentGeneration"];
                    _field_addedToTanks = moduleGenerator.Fields["AddedToFuelTanks"];
                    _field_max = moduleGenerator.Fields["PowerGeneration"];
                }

                if (moduleGenerator == null) return;

                String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
                this.resources_to_supply = resources_to_supply;
                base.OnStart(state);

                previousDeltaTime = TimeWarp.fixedDeltaTime;

                var electricChargePartResource = part.Resources[ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE];
                if (electricChargePartResource != null)
                {
                    fixedElectricChargeBufferSize = electricChargePartResource.maxAmount * 50;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNFissionGeneratorAdapter.OnStart " + e.Message);
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
                Debug.LogError("[KSPI] - Exception in FNFissionGeneratorAdapter.OnFixedUpdate " + e.Message);
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
                Debug.LogError("[KSPI] - Exception in FNFissionGeneratorAdapter.OnFixedUpdate " + e.Message);
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
                if (_field_status == null) return;
                if (_field_generated == null) return;

                var electricChargePartResource = part.Resources[ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE];
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

                bool status = _field_status.GetValue<bool>(moduleGenerator);

                float generatorRate = status ? _field_generated.GetValue<float>(moduleGenerator) : 0;
                float generatorMax = _field_max.GetValue<float>(moduleGenerator);

                // extract power otherwise we end up with double power
                if (_field_addedToTanks != null)
                {
                    part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, _field_addedToTanks.GetValue<float>(moduleGenerator));
                }

                megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(generatorRate / 1000, generatorMax / 1000, ResourceManager.FNRESOURCE_MEGAJOULES);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNFissionGeneratorAdapter.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
    }
}
