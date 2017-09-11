using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    enum resourceType
    {
        electricCharge, megajoule, other
    }

    [KSPModule("Solar Panel Adapter")]
	class FNSolarPanelWasteHeatModule : FNResourceSuppliableModule 
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
        private PartResourceDefinition outputDefinition;
        private resourceType outputType = 0;
        private PartResource megajoulePartResource;
        private PartResource electricChargePartResource;
        private BaseField _field_kerbalism_output;
        private PartModule warpfixer;

        private bool active = false;
        private float previousDeltaTime;
        private double fixedMegajouleBufferSize;
        private double fixedElectricChargeBufferSize;

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

                String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES };
                this.resources_to_supply = resources_to_supply;
                base.OnStart(state);

                previousDeltaTime = TimeWarp.fixedDeltaTime;

                solarPanel = (ModuleDeployableSolarPanel)this.part.FindModuleImplementing<ModuleDeployableSolarPanel>();

                if (solarPanel == null) return;

                if (solarPanel.resourceName == FNResourceManager.FNRESOURCE_MEGAJOULES)
                {
                    outputType = resourceType.megajoule;

                    megajoulePartResource = part.Resources[FNResourceManager.FNRESOURCE_MEGAJOULES];
                    if (megajoulePartResource != null)
                    {
                        fixedMegajouleBufferSize = megajoulePartResource.maxAmount * 50;
                    }
                }
                else if (solarPanel.resourceName == FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                {
                    outputType = resourceType.electricCharge;

                    electricChargePartResource = part.Resources[FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE];
                    if (electricChargePartResource != null)
                    {
                        fixedElectricChargeBufferSize = electricChargePartResource.maxAmount * 50;
                    }
                }
                else
                    outputType = resourceType.other;

                outputDefinition = PartResourceLibrary.Instance.GetDefinition(solarPanel.resourceName);
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

                double solar_rate = solarPanel.flowRate > 0 
                    ? solarPanel.flowRate
                    : solarPanel.panelType == ModuleDeployableSolarPanel.PanelType.FLAT 
                        ? solarPanel._flowRate 
                        : solarPanel._flowRate * solarPanel.chargeRate;

                double maxSupply = solarPanel._distMult > 0
                    ? solarPanel.chargeRate * solarPanel._distMult * solarPanel._efficMult 
                    : solar_rate;

                // readout kerbalism solar power output so we can remove it
                if (_field_kerbalism_output != null)
                    kerbalismPowerOutput = _field_kerbalism_output.GetValue<double>(warpfixer);

                // extract power otherwise we end up with double power
                var power_reduction = solarPanel.flowRate > 0 ? solarPanel.flowRate : kerbalismPowerOutput;
                part.RequestResource(outputDefinition.id, power_reduction * TimeWarp.fixedDeltaTime);

                solar_supply = outputType == resourceType.megajoule ? solar_rate : solar_rate / 1000;
                solar_maxSupply = outputType == resourceType.megajoule ? maxSupply : maxSupply / 1000;

                megaJouleSolarPowerSupply = supplyFNResourcePerSecondWithMax(solar_supply, solar_maxSupply, FNResourceManager.FNRESOURCE_MEGAJOULES);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNSolarPanelWasteHeatModule.OnFixedUpdateResourceSuppliable " + e.Message);
                throw;
            }
        }
	}
}

