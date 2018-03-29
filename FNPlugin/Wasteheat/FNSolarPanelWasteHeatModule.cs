using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin 
{
    enum resourceType
    {
        electricCharge, megajoule, other
    }

    [KSPModule("Solar Panel Adapter")]
    class FNSolarPanelWasteHeatModule : ResourceSuppliableModule 
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true,  guiName = "Solar current power", guiUnits = " MW", guiFormat="F5")]
        public double megaJouleSolarPowerSupply;
        
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double kerbalismPowerOutput;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double solar_supply = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public double solar_maxSupply = 0;

        private MicrowavePowerReceiver microwavePowerReceiver;
        private ModuleDeployableSolarPanel solarPanel;
        private resourceType outputType = 0;
        private BaseField _field_kerbalism_output;
        private PartModule warpfixer;
        private ResourceBuffers resourceBuffers;

        private bool active = false;

        public override void OnStart(PartModule.StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                microwavePowerReceiver = part.FindModuleImplementing<MicrowavePowerReceiver>();
                if (microwavePowerReceiver != null)
                {
                    Fields["megaJouleSolarPowerSupply"].guiActive = false;
                    return;
                }

                if (part.Modules.Contains("WarpFixer"))
                {
                    warpfixer = part.Modules["WarpFixer"];
                    _field_kerbalism_output = warpfixer.Fields["field_output"];
                }

                part.force_activate();

                String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
                this.resources_to_supply = resources_to_supply;
                base.OnStart(state);

                solarPanel = (ModuleDeployableSolarPanel)this.part.FindModuleImplementing<ModuleDeployableSolarPanel>();

                if (solarPanel == null) return;

                resourceBuffers = new ResourceBuffers();
                if (solarPanel.resourceName == ResourceManager.FNRESOURCE_MEGAJOULES)
                {
                    resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES, 10));
                    outputType = resourceType.megajoule;
                }
                else if (solarPanel.resourceName == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                {
                    resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, 10));
                    outputType = resourceType.electricCharge;
                }
                else
                {
                    outputType = resourceType.other;
                }

                resourceBuffers.Init(this.part);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNSolarPanelWasteHeatModule.OnStart " + e.Message);
                throw;
            }
        }

        public override void OnFixedUpdate() 
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight) return;

                if (microwavePowerReceiver != null) return;

                active = true;
                base.OnFixedUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNSolarPanelWasteHeatModule.OnFixedUpdate " + e.Message);
                throw;
            }
        }


        public void FixedUpdate()
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight) return;

                if (microwavePowerReceiver != null) return;

                if (!active)
                    base.OnFixedUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNSolarPanelWasteHeatModule.OnFixedUpdate " + e.Message);
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
                if (microwavePowerReceiver != null) return;

                if (solarPanel == null) return;

                if (outputType == resourceType.other) return;

                // readout kerbalism solar power output so we use it
                if (_field_kerbalism_output != null)
                {
                    // if GUI is inactive, then Panel doesn't produce power since Kerbalism doesn't reset the value on occlusion
                    // to be fixed in Kerbalism!
                    kerbalismPowerOutput = _field_kerbalism_output.guiActive == true ? _field_kerbalism_output.GetValue<double>(warpfixer) : 0;
                }

                // solarPanel.resHandler.outputResource[0].rate is zeroed by Kerbalism, flowRate is bogus.
                // So we need to assume that Kerbalism Power Output is ok (if present),
                // since calculating output from flowRate (or _flowRate) will not be possible.
                double solar_rate = kerbalismPowerOutput > 0 ? kerbalismPowerOutput : 
                    solarPanel.flowRate > 0 ? solarPanel.flowRate :
                    solarPanel.panelType == ModuleDeployableSolarPanel.PanelType.FLAT ? solarPanel._flowRate :
                    solarPanel._flowRate * solarPanel.chargeRate;

                double maxSupply = solarPanel._distMult > 0
                    ? solarPanel.chargeRate * solarPanel._distMult * solarPanel._efficMult 
                    : solar_rate;

                if (outputType == resourceType.megajoule)
                    resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, maxSupply);
                else
                    resourceBuffers.UpdateVariable(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, maxSupply);

                resourceBuffers.UpdateBuffers();

                // extract power otherwise we end up with double power
                // TODO MISSING IMPLEMENTATION

                solar_supply = outputType == resourceType.megajoule ? solar_rate : solar_rate / 1000;
                solar_maxSupply = outputType == resourceType.megajoule ? maxSupply : maxSupply / 1000;

                megaJouleSolarPowerSupply = supplyFNResourcePerSecondWithMax(solar_supply, solar_maxSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNSolarPanelWasteHeatModule.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
	}
}

