using FNPlugin.Power;
using FNPlugin.Wasteheat;
using System;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Near Future Fission Generator Adapter")]
    class FNFissionGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Currentpower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F5")]//Generator current power
        public double megaJouleGeneratorPowerSupply;
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Efficiency")]//Efficiency
        public string OverallEfficiency;

        private PartModule moduleGenerator;
        private BaseField _field_status;
        private BaseField _field_generated;
        private BaseField _field_addedToTanks;
        private BaseField _field_max;

        private bool active = false;
        private ResourceBuffers resourceBuffers;

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

                OverallEfficiency = "10%";

                String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT };
                this.resources_to_supply = resources_to_supply;
                base.OnStart(state);

                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, 50));
                resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(1, 2.0e+5, true));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, (double)(decimal)this.part.mass);
                resourceBuffers.Init(this.part);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNFissionGeneratorAdapter.OnStart " + e.Message);
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
                Debug.LogError("[KSPI]: Exception in FNFissionGeneratorAdapter.OnFixedUpdate " + e.Message);
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
                Debug.LogError("[KSPI]: Exception in FNFissionGeneratorAdapter.OnFixedUpdate " + e.Message);
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

                bool status = _field_status.GetValue<bool>(moduleGenerator);

                float generatorRate = status ? _field_generated.GetValue<float>(moduleGenerator) : 0;
                float generatorMax = _field_max.GetValue<float>(moduleGenerator);

                // extract power otherwise we end up with double power
                if (_field_addedToTanks != null)
                {
                    part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, (double)_field_addedToTanks.GetValue<float>(moduleGenerator));
                }

                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                resourceBuffers.UpdateBuffers();

                megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(generatorRate / 1000, generatorMax / 1000, ResourceManager.FNRESOURCE_MEGAJOULES);

                if (!CheatOptions.IgnoreMaxTemperature)
                    supplyFNResourcePerSecond(generatorRate / 10000.0d, ResourceManager.FNRESOURCE_WASTEHEAT);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNFissionGeneratorAdapter.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
    }
}
