using FNPlugin.Constants;
using FNPlugin.Power;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using System;
using UnityEngine;

namespace FNPlugin.Powermanagement
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

        private PartModule _moduleGenerator;
        private BaseField _fieldStatus;
        private BaseField _fieldGenerated;
        private BaseField _fieldEfficiency;
        private BaseField _fieldMax;
        private ResourceBuffers _resourceBuffers;

        private bool active;

        public override void OnStart(StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                if (part.Modules.Contains("FissionGenerator"))
                {
                    _moduleGenerator = part.Modules["FissionGenerator"];
                    _fieldStatus = _moduleGenerator.Fields["Status"];
                    _fieldGenerated = _moduleGenerator.Fields["CurrentGeneration"];
                    _fieldEfficiency = _moduleGenerator.Fields["Efficiency"];
                    _fieldMax = _moduleGenerator.Fields["PowerGeneration"];
                }

                if (_moduleGenerator == null) return;

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
            if (!HighLogic.LoadedSceneIsFlight || _moduleGenerator == null) return;

            active = true;
            base.OnFixedUpdate();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || _moduleGenerator == null) return;

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
            if (_moduleGenerator == null || _fieldStatus == null || _fieldGenerated == null) return;

            bool status = _fieldStatus.GetValue<bool>(_moduleGenerator);

            float generatorRate = status ? _fieldGenerated.GetValue<float>(_moduleGenerator) : 0;
            float generatorMax = _fieldMax.GetValue<float>(_moduleGenerator);
            float generatorEfficiency = _fieldEfficiency.GetValue<float>(_moduleGenerator);

            efficiency = generatorEfficiency.ToString("P2");

            //extract power otherwise we end up with double power
            part.RequestResource(ResourceSettings.Config.ElectricChargePower, generatorRate * fixedDeltaTime);

            double megajoulesRate = generatorRate / GameConstants.ecPerMJ;
            double maxMegajoulesRate = generatorMax / GameConstants.ecPerMJ;

            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, megajoulesRate);
            _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, part.mass);
            _resourceBuffers.UpdateBuffers();

            megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(megajoulesRate, maxMegajoulesRate, ResourceManager.FNRESOURCE_MEGAJOULES);

            if (CheatOptions.IgnoreMaxTemperature) return;

            double maxWasteheat = generatorEfficiency > 0.0 ? maxMegajoulesRate * (1.0 / generatorEfficiency - 1.0) : maxMegajoulesRate;
            double throttledWasteheat = generatorEfficiency > 0.0 ? megajoulesRate * (1.0 / generatorEfficiency - 1.0) : megajoulesRate;
            supplyFNResourcePerSecondWithMax(throttledWasteheat, maxWasteheat, ResourceManager.FNRESOURCE_WASTEHEAT);
        }
    }
}
