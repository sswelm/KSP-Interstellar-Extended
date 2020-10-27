using FNPlugin.Power;
using FNPlugin.Wasteheat;
using System;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Near Future Fission Generator Adapter")]
    class FNFissionGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Currentpower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F5")]//Generator current power
        public double megaJouleGeneratorPowerSupply;
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Efficiency")]//Efficiency
        public string efficiency;

        [KSPField]
        public float wasteHeatMultiplier = 0.01f;

        private PartModule moduleGenerator;
        private BaseField _field_status;
        private BaseField _field_generated;
        private BaseField _field_efficiency;
        private BaseField _field_max;

        private bool active;
        private ResourceBuffers _resourceBuffers;

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
                    _field_efficiency = moduleGenerator.Fields["Efficiency"];
                    _field_max = moduleGenerator.Fields["PowerGeneration"];
                }

                if (moduleGenerator == null) return;

                string[] resourcesToSupply = { ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT };
                this.resources_to_supply = resourcesToSupply;
                base.OnStart(state);

                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES));
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE));
                _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, 2.0e+5, true));
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, part.mass);
                _resourceBuffers.Init(part);
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
                float generatorEfficiency = _field_efficiency.GetValue<float>(moduleGenerator);

                efficiency = (generatorEfficiency * 100).ToString("F2") + " %";

                //extract power otherwise we end up with double power
                part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, generatorRate * fixedDeltaTime);

                var megajoulesRate = generatorRate / 1000;
                var maxMegajoulesRate = generatorMax / 1000;

                _resourceBuffers.UpdateVariable(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, generatorRate);
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, megajoulesRate);
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, part.mass);
                _resourceBuffers.UpdateBuffers();

                megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(megajoulesRate, maxMegajoulesRate, ResourceManager.FNRESOURCE_MEGAJOULES);

                var maxWasteheat = generatorEfficiency > 0 ? maxMegajoulesRate / generatorEfficiency : maxMegajoulesRate;

                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    supplyFNResourcePerSecondWithMax(maxWasteheat, maxWasteheat, ResourceManager.FNRESOURCE_WASTEHEAT);
                    consumeFNResourcePerSecond(maxWasteheat * generatorEfficiency, ResourceManager.FNRESOURCE_WASTEHEAT);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNFissionGeneratorAdapter.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
    }
}
