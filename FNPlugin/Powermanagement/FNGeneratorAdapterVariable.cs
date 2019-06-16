using FNPlugin.Power;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Generator Adapter")]
    class FNGeneratorAdapterVariable : ResourceSuppliableModule
    {
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Power input", guiUnits = " MW", guiFormat = "F6")]
        public double powerGeneratorPowerInput;
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Power output", guiUnits = " MW", guiFormat = "F6")]
        public double powerGeneratorPowerOutput;

        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true)]
        public double initialMaxBufferSize;
        [KSPField(isPersistant = true)]
        private double initialInputAmount;
        [KSPField(isPersistant = true, guiActive = true)]
        private double initialOutputAmount;

        [KSPField]
        public bool showDisplayStatus = true;
        [KSPField]
        public bool showEfficiency = true;
        [KSPField]
        public bool showPowerInput = true;
        [KSPField]
        public bool showPowerOuput = true;
        [KSPField]
        public bool offlineProcessing = false;
        [KSPField]
        public int index = 0;
        [KSPField]
        public bool maintainsBuffer = true;
        [KSPField]
        public double maximumPowerGeneration = 0;
        [KSPField]
        public double currentMegajoulesDemand;
        [KSPField]
        public double currentMegajoulesSupply;
        [KSPField]
        public double inputDivider = 0;
        [KSPField]
        public double inputRate = 0;
        [KSPField]
        public double outputRate = 0;
        [KSPField]
        public double inputAmount;
        [KSPField]
        public double inputMaxAmount;

        [KSPField]
        private double generatorOutputRateInElectricCharge;
       
        private ModuleGenerator moduleGenerator;       
        private ResourceBuffers resourceBuffers;

        private ModuleResource mockInputResource;        
        private ModuleResource moduleInputResource;
        private ModuleResource moduleOutputResource;

        private BaseField efficiencyField;
        private BaseField displayStatusField;
        private BaseField powerGeneratorPowerInputField;
        private BaseField powerGeneratorPowerOutputField;
        private BaseField moduleGeneratorEfficienctBaseField;
        
        private ResourceType outputType = 0;
        private ResourceType inputType = 0;

        private double generatorInputRate;



        private bool active = false;

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

                if (maintainsBuffer)
                    resourceBuffers = new ResourceBuffers();

                outputType = ResourceType.other;
                inputType = ResourceType.other;

                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.inputResources)
                {
                    if (moduleResource.name == ResourceManager.FNRESOURCE_MEGAJOULES)
                        inputType = ResourceType.megajoule;
                    else if (moduleResource.name == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                        inputType = ResourceType.electricCharge;

                    if (inputType != ResourceType.other)
                    {
                        moduleInputResource = moduleResource;

                        if (inputRate != 0)
                            moduleInputResource.rate = inputRate;

                        initialInputAmount = moduleInputResource.rate;

                        break;
                    }
                }

                if (offlineProcessing && moduleInputResource != null && last_active_time > 0 && powerGeneratorPowerInput > 0)
                {
                    var timePassedSinceLastProcessing = Planetarium.GetUniversalTime() - last_active_time;

                    var consumption = timePassedSinceLastProcessing * powerGeneratorPowerInput;

                    part.RequestResource(moduleInputResource.name, consumption);

                    var message =  timePassedSinceLastProcessing + " seconds passed durring which " +  consumption + " " + moduleInputResource.name + " was consumed ";

                    Debug.Log("[KSPI]: " + message);
                }

                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.outputResources)
                {
                    // assuming only one of those two is present
                    if (moduleResource.name == ResourceManager.FNRESOURCE_MEGAJOULES)
                        outputType = ResourceType.megajoule;
                    else if (moduleResource.name == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                        outputType = ResourceType.electricCharge;

                    if (outputType != ResourceType.other)
                    {
                        if (maintainsBuffer)
                        {
                            var bufferResource = part.Resources[moduleResource.name];
                            if (bufferResource != null)
                            {
                                if (initialMaxBufferSize == 0)
                                    initialMaxBufferSize = bufferResource.maxAmount;
                                else
                                    bufferResource.maxAmount = initialMaxBufferSize;
                            }

                            resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(moduleResource.name, 50));
                        }

                        mockInputResource = new ModuleResource();
                        mockInputResource.name = moduleResource.name;
                        mockInputResource.id = moduleResource.name.GetHashCode();
                        moduleGenerator.resHandler.inputResources.Add(mockInputResource);

                        moduleOutputResource = moduleResource;

                        if (outputRate != 0)
                            moduleOutputResource.rate = outputRate;

                        initialOutputAmount = moduleOutputResource.rate;

                        moduleGeneratorEfficienctBaseField = moduleGenerator.Fields["efficiency"];
                        if (moduleGeneratorEfficienctBaseField != null)
                        {
                            moduleGeneratorEfficienctBaseField.guiActive = false;
                            moduleGeneratorEfficienctBaseField.guiActiveEditor = false;
                        }  

                        break;
                    }
                }

                if (maintainsBuffer)
                    resourceBuffers.Init(this.part);

                efficiencyField = moduleGenerator.Fields["efficiency"];
                displayStatusField = moduleGenerator.Fields["displayStatus"];

                powerGeneratorPowerInputField = Fields["powerGeneratorPowerInput"];
                powerGeneratorPowerOutputField = Fields["powerGeneratorPowerOutput"];

                if (index > 0)
                {
                    powerGeneratorPowerInputField.guiName = powerGeneratorPowerInputField.guiName + " " + (index + 1);
                    powerGeneratorPowerOutputField.guiName = powerGeneratorPowerOutputField.guiName + " " + (index + 1);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnStart " + e.Message);
                throw;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Save InputOutput", active = true)]
        public void SaveCurrentInputOutput()
        {
            moduleGenerator.Shutdown();

            var newResourcesConfigNode = new ConfigNode();

            var inputConfigNode = newResourcesConfigNode.AddNode("INPUT_RESOURCE");
            inputConfigNode.AddValue("name", "Megajoules");
            inputConfigNode.AddValue("ratio", generatorInputRate);

            var outConfigNode = newResourcesConfigNode.AddNode("OUTPUT_RESOURCE");
            inputConfigNode.AddValue("name", "Megajoules");
            inputConfigNode.AddValue("ratio", generatorOutputRateInElectricCharge);

            moduleGenerator.Load(newResourcesConfigNode);

            moduleGenerator.Activate();
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            if (moduleGenerator == null)
                return;

            efficiencyField.guiActive = showEfficiency;
            displayStatusField.guiActive = showDisplayStatus;

            powerGeneratorPowerInputField.guiActive = moduleInputResource != null && showPowerInput;
            powerGeneratorPowerOutputField.guiActive = moduleOutputResource != null && showPowerOuput;
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
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.FixedUpdate " + e.Message);
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
            if (maximumPowerGeneration == 0)
                return 1;
            else
                return 2;
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            try
            {
                last_active_time = Planetarium.GetUniversalTime();

                powerGeneratorPowerOutput = 0;
                powerGeneratorPowerInput = 0;

                if (moduleGenerator == null) return;

                if (outputType == ResourceType.other) return;

                if (!moduleGenerator.generatorIsActive && !moduleGenerator.isAlwaysActive)
                {
                    mockInputResource.rate = 0;
                    supplyFNResourcePerSecondWithMax(0, 0, ResourceManager.FNRESOURCE_MEGAJOULES);
                    return;
                }

                if (maintainsBuffer)
                    resourceBuffers.UpdateBuffers();

                if (maximumPowerGeneration != 0)
                {
                    currentMegajoulesDemand =  Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES));
                    currentMegajoulesSupply = Math.Min(currentMegajoulesDemand, maximumPowerGeneration);
                }

                if (moduleInputResource != null)
                {
                    if (currentMegajoulesSupply > 0)
                        moduleInputResource.rate = currentMegajoulesSupply;
                    else
                        moduleInputResource.rate = initialInputAmount;

                    part.GetConnectedResourceTotals(moduleInputResource.id, out inputAmount, out inputMaxAmount);

                    var availableRatio = Math.Min(1, inputAmount / (moduleInputResource.rate * fixedDeltaTime));

                    currentMegajoulesSupply *= availableRatio;
                    moduleInputResource.rate *= availableRatio;

                    generatorInputRate = moduleInputResource.rate;
                    powerGeneratorPowerInput = inputType == ResourceType.megajoule ? generatorInputRate : generatorInputRate / 1000;
                }

                if (moduleOutputResource != null)
                {
                    generatorOutputRateInElectricCharge = maximumPowerGeneration > 0 
                        ? currentMegajoulesSupply * (outputType == ResourceType.megajoule ? 1 : 1000 )
                        : initialOutputAmount;

                    if (maximumPowerGeneration > 0)
                        moduleOutputResource.rate = 1 + generatorOutputRateInElectricCharge;
                    else
                        moduleOutputResource.rate = 1 + generatorOutputRateInElectricCharge;

                    mockInputResource.rate = generatorOutputRateInElectricCharge;

                    double generatorSupplyInMegajoules = outputType == ResourceType.megajoule ? generatorOutputRateInElectricCharge : generatorOutputRateInElectricCharge / 1000;

                    powerGeneratorPowerOutput = supplyFNResourcePerSecondWithMax(generatorSupplyInMegajoules, generatorSupplyInMegajoules, ResourceManager.FNRESOURCE_MEGAJOULES);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
    }
}
