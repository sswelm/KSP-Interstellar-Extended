using FNPlugin.Constants;
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

                resources_to_supply = new string[] { ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT };
                base.OnStart(state);

                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES));
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
            if (!HighLogic.LoadedSceneIsFlight || moduleGenerator == null) return;

            active = true;
            base.OnFixedUpdate();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || moduleGenerator == null) return;

            if (!active)
                base.OnFixedUpdate();
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
            if (moduleGenerator == null || _field_status == null || _field_generated == null) return;

            bool status = _field_status.GetValue<bool>(moduleGenerator);

            float generatorRate = status ? _field_generated.GetValue<float>(moduleGenerator) : 0;
            float generatorMax = _field_max.GetValue<float>(moduleGenerator);
            float generatorEfficiency = _field_efficiency.GetValue<float>(moduleGenerator);

            efficiency = generatorEfficiency.ToString("P2");

            //extract power otherwise we end up with double power
            part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, generatorRate * fixedDeltaTime);

            double megajoulesRate = generatorRate / GameConstants.ecPerMJ;
            double maxMegajoulesRate = generatorMax / GameConstants.ecPerMJ;

            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, megajoulesRate);
            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, part.mass);
            _resourceBuffers.UpdateBuffers();

            megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(megajoulesRate, maxMegajoulesRate, ResourceManager.FNRESOURCE_MEGAJOULES);

            if (!CheatOptions.IgnoreMaxTemperature)
            {
                double maxWasteheat = generatorEfficiency > 0.0 ? maxMegajoulesRate * (1.0 /
                    generatorEfficiency - 1.0) : maxMegajoulesRate;
                double throttledWasteheat = generatorEfficiency > 0.0 ? megajoulesRate * (1.0 /
                    generatorEfficiency - 1.0) : megajoulesRate;
                supplyFNResourcePerSecondWithMax(throttledWasteheat, maxWasteheat,
                    ResourceManager.FNRESOURCE_WASTEHEAT);
            }
        }
    }
}
