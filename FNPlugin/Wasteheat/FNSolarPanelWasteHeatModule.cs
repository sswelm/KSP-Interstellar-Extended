using FNPlugin.Power;
using System;
using UnityEngine;

namespace FNPlugin 
{
    enum resourceType
    {
        electricCharge, megajoule, other
    }

    public interface ISolarPower
    {
        double SolarPower { get; }
    }

    [KSPModule("Solar Panel Adapter")]
    class FNSolarPanelWasteHeatModule : ResourceSuppliableModule, ISolarPower
    {
        [KSPField( guiActive = true,  guiName = "Solar current power", guiUnits = " MW", guiFormat="F5")]
        public double megaJouleSolarPowerSupply;
        [KSPField(guiActive = false)]
        public double kerbalismPowerOutput;
        [KSPField(guiActive = false)]
        public double solar_supply = 0;
        [KSPField(guiActive = false, guiName = "Solar maximum power", guiUnits = " MW", guiFormat = "F5")]
        public double solarMaxSupply = 0;

        MicrowavePowerReceiver _microwavePowerReceiver;
        ModuleDeployableSolarPanel _solarPanel;
        BaseField _field_kerbalism_output;
        PartModule _warpfixer;
        ResourceBuffers _resourceBuffers;
        ModuleResource _solarFlowRateResource;

        resourceType outputType = 0;

        public double SolarPower
        {
            get { return solar_supply; }
        }

        bool active = false;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            _microwavePowerReceiver = part.FindModuleImplementing<MicrowavePowerReceiver>();
            if (_microwavePowerReceiver != null)
            {
                Fields["megaJouleSolarPowerSupply"].guiActive = false;
                Fields["solarMaxSupply"].guiActive = false;
                return;
            }

            if (part.Modules.Contains("WarpFixer"))
            {
                _warpfixer = part.Modules["WarpFixer"];
                _field_kerbalism_output = _warpfixer.Fields["field_output"];
            }

            _solarPanel = (ModuleDeployableSolarPanel)this.part.FindModuleImplementing<ModuleDeployableSolarPanel>();
            if (_solarPanel == null) return;

            _solarFlowRateResource = new ModuleResource();
            _solarFlowRateResource.name = _solarPanel.resourceName;
            resHandler.inputResources.Add(_solarFlowRateResource);

            part.force_activate();

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            _resourceBuffers = new ResourceBuffers();
            if (_solarPanel.resourceName == ResourceManager.FNRESOURCE_MEGAJOULES)
            {
                _resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(ResourceManager.FNRESOURCE_MEGAJOULES, 50));
                outputType = resourceType.megajoule;
            }
            else if (_solarPanel.resourceName == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
            {
                _resourceBuffers.AddConfiguration(new ResourceBuffers.MaxAmountConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, 50));
                outputType = resourceType.electricCharge;
            }
            else
                outputType = resourceType.other;

            _resourceBuffers.Init(this.part);
        }

        public override void OnFixedUpdate()
        {
            if (_microwavePowerReceiver != null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

            active = true;
            base.OnFixedUpdate();
        }


        public void FixedUpdate()
        {
            if (_microwavePowerReceiver != null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

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
            if (_microwavePowerReceiver != null) return;

            if (_solarPanel == null) return;

            if (outputType == resourceType.other) return;

            // readout kerbalism solar power output so we use it
            if (_field_kerbalism_output != null)
            {
                // if GUI is inactive, then Panel doesn't produce power since Kerbalism doesn't reset the value on occlusion
                // to be fixed in Kerbalism!
                kerbalismPowerOutput = _field_kerbalism_output.guiActive == true ? _field_kerbalism_output.GetValue<double>(_warpfixer) : 0;
            }

            // solarPanel.resHandler.outputResource[0].rate is zeroed by Kerbalism, flowRate is bogus.
            // So we need to assume that Kerbalism Power Output is ok (if present),
            // since calculating output from flowRate (or _flowRate) will not be possible.
            double solar_rate = kerbalismPowerOutput > 0 ? kerbalismPowerOutput :
                _solarPanel.flowRate > 0 ? _solarPanel.flowRate :
                _solarPanel.panelType == ModuleDeployableSolarPanel.PanelType.FLAT 
                    ? _solarPanel._flowRate 
                    : _solarPanel._flowRate * _solarPanel.chargeRate;

            double maxSupply = _solarPanel._distMult > 0
                ? _solarPanel.chargeRate * _solarPanel._distMult * _solarPanel._efficMult
                : solar_rate;

            _resourceBuffers.UpdateBuffers();

            // extract power otherwise we end up with double power
            _solarFlowRateResource.rate = solar_rate;

            // provide power to supply manager
            solar_supply = outputType == resourceType.megajoule ? solar_rate : solar_rate * 0.001;
            solarMaxSupply = outputType == resourceType.megajoule ? maxSupply : maxSupply * 0.001;

            megaJouleSolarPowerSupply = supplyFNResourcePerSecondWithMax(solar_supply, solarMaxSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
        }       
    }
}