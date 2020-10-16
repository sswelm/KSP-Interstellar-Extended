using FNPlugin.Power;
using FNPlugin.Wasteheat;

namespace FNPlugin
{
    [KSPModule("Near Future Fission Generator Adapter")]
    class FNFissionGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Currentpower", guiUnits = " MW", guiFormat = "F5")]//Generator current power
        public double megaJouleGeneratorPowerSupply;
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Efficiency")]//Efficiency
        public string OverallEfficiency;

        private BaseField fieldGenerated;
        private BaseField fieldMax;
        private PartModule moduleGenerator;

        private bool active = false;
        private ResourceBuffers resourceBuffers;

        public override void OnStart(StartState state)
        {
            resources_to_supply = new string[] { ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT };
            base.OnStart(state);

            if (state != StartState.Editor)
            {
                if (part.Modules.Contains("FissionGenerator"))
                {
                    moduleGenerator = part.Modules["FissionGenerator"];
                    fieldGenerated = moduleGenerator.Fields["CurrentGeneration"];
                    fieldMax = moduleGenerator.Fields["PowerGeneration"];
                }

                if (moduleGenerator == null) return;

                OverallEfficiency = "10%";

                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, 50));
                resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(1, 2.0e+5, true));
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, part.mass);
                resourceBuffers.Init(part);
            }
        }

        public override void OnFixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && moduleGenerator != null)
            {
                active = true;
                base.OnFixedUpdate();
            }
        }


        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && moduleGenerator != null && !active)
            {
                base.OnFixedUpdate();
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
            if (moduleGenerator == null || fieldGenerated == null) return;

            float generatorRate = fieldGenerated.GetValue<float>(moduleGenerator);
            float generatorMax = fieldMax.GetValue<float>(moduleGenerator);

            // extract power otherwise we end up with double power
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, part.mass);
            resourceBuffers.UpdateBuffers();

            megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(generatorRate / 1000, generatorMax / 1000, ResourceManager.FNRESOURCE_MEGAJOULES);

            if (!CheatOptions.IgnoreMaxTemperature)
                supplyFNResourcePerSecond(generatorRate / 10000.0d, ResourceManager.FNRESOURCE_WASTEHEAT);
        }
    }
}
