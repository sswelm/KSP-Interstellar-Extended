using FNPlugin.Constants;
using FNPlugin.Power;
using System;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Generator Adapter")]
    class FNGeneratorAdapter : ResourceSuppliableModule
    {
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FNGeneratorAdapter_CurrentPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Generator current power
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

        private ModuleGenerator _moduleGenerator;
        private ResourceType _outputType = 0;
        private ResourceBuffers _resourceBuffers;
        private ModuleResource _mockInputResource;
        private ModuleResource _moduleOutputResource;
        private BaseField _efficiencyField;
        private BaseField _displayStatusField;

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

                _moduleGenerator = modules.Count > index ? modules[index] : null;

                if (_moduleGenerator == null)
                {
                    Debug.LogWarning("[KSPI]: disabling FNGeneratorAdapter, failed to find ModuleGenerator");
                    return;
                }

                string[] resourcesToSupply = { ResourceManager.FNRESOURCE_MEGAJOULES };
                this.resources_to_supply = resourcesToSupply;
                base.OnStart(state);

                if (maintainsBuffer)
                    _resourceBuffers = new ResourceBuffers();

                _outputType = ResourceType.other;
                foreach (ModuleResource moduleResource in _moduleGenerator.resHandler.outputResources)
                {
                    if (moduleResource.name != ResourceManager.FNRESOURCE_MEGAJOULES && (moduleResource.name != ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE))
                        continue;

                    // assuming only one of those two is present
                    _outputType = moduleResource.name == ResourceManager.FNRESOURCE_MEGAJOULES ? ResourceType.megajoule : ResourceType.electricCharge;

                    if (maintainsBuffer)
                        _resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(moduleResource.name, 50));

                    _mockInputResource = new ModuleResource
                    {
                        name = moduleResource.name,
                        id = moduleResource.name.GetHashCode()
                    };

                    _moduleGenerator.resHandler.inputResources.Add(_mockInputResource);
                    _moduleOutputResource = moduleResource;
                    break;
                }

                if (maintainsBuffer)
                    _resourceBuffers.Init(part);

                _efficiencyField = _moduleGenerator.Fields[nameof(ModuleGenerator.efficiency)];
                _displayStatusField = _moduleGenerator.Fields[nameof(ModuleGenerator.displayStatus)];

                _efficiencyField.guiActive = showEfficiency;
                _displayStatusField.guiActive = showDisplayStatus;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnStart " + e.ToString());
                throw;
            }
        }

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (_moduleGenerator == null) return;

            active = true;
            base.OnFixedUpdate();
        }


        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (_moduleGenerator == null) return;

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
            if (_moduleGenerator == null) return;

            if (_outputType == ResourceType.other) return;

            var generatorRate = _moduleOutputResource.rate;
            _mockInputResource.rate = generatorRate;

            double generatorSupply = _outputType == ResourceType.megajoule ? generatorRate :
                generatorRate / GameConstants.ecPerMJ;

            if (maintainsBuffer)
                _resourceBuffers.UpdateBuffers();

            megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(generatorSupply,
                generatorSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
        }
    }
}
