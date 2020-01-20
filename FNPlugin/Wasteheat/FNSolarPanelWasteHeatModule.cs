using FNPlugin.Power;
using FNPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    enum ResourceType
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
        [KSPField( guiActive = true,  guiName = "#LOC_KSPIE_SolarPanelWH_CurrentSolarPower", guiUnits = " MW", guiFormat= "F5")]//Current Solar Power
        public double megaJouleSolarPowerSupply;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_SolarPanelWH_MaximumSolarPower", guiUnits = " MW", guiFormat = "F5")]//Maximum Solar Power
        public double solarMaxSupply = 0;
        [KSPField(guiActive = false, guiName = "AU", guiFormat = "F0", guiUnits = " m")]
        public double astronomicalUnit;

        [KSPField]
        public string resourceName;
        [KSPField]
        public double solar_supply = 0;
        [KSPField]
        public float chargeRate;
        [KSPField]
        public float efficiencyMult;
        [KSPField]
        public double _efficMult;
        [KSPField]
        public double sunAOA;
        [KSPField]
        public double calculatedEfficency;
        //[KSPField(guiActive = true)]
        //public float flowRate;
        //[KSPField(guiActive = true)]
        //public double _flowRate;
        [KSPField]
        public double kerbalism_nominalRate;
        [KSPField]
        public string kerbalism_panelState;
        [KSPField]
        public string kerbalism_panelOutput;
        [KSPField]
        public double kerbalism_panelPower;

        [KSPField]
        public double scale = 1;
        [KSPField]
        double maxSupply;
        [KSPField]
        double solarRate;
        [KSPField]
        public double outputResourceRate;
        [KSPField]
        public double outputResourceCurrentRequest;

        BeamedPowerReceiver _microwavePowerReceiver;
        ModuleDeployableSolarPanel _solarPanel;
        ResourceBuffers _resourceBuffers;
        ResourceType _outputType = 0;
        List<StarLight> _stars;

        BaseField megaJouleSolarPowerSupplyField;
        BaseField solarMaxSupplyField;
        BaseField _field_kerbalism_nominalRate;
        BaseField _field_kerbalism_panelStatus;

        ModuleResource outputResource;
        PartModule solarPanelFixer;
      
        public double SolarPower
        {
            get { return solar_supply; }
        }

        bool _active = false;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            megaJouleSolarPowerSupplyField = Fields["megaJouleSolarPowerSupply"];
            solarMaxSupplyField = Fields["solarMaxSupply"];

            if (part.Modules.Contains("SolarPanelFixer"))
            {
                solarPanelFixer = part.Modules["SolarPanelFixer"];

                _field_kerbalism_nominalRate = solarPanelFixer.Fields["nominalRate"];
                _field_kerbalism_panelStatus = solarPanelFixer.Fields["panelStatus"];
            }

            // calculate Astronomical unit on homeworld semiMajorAxis when missing
            if (astronomicalUnit == 0)
                astronomicalUnit = FlightGlobals.GetHomeBody().orbit.semiMajorAxis;

            _microwavePowerReceiver = part.FindModuleImplementing<BeamedPowerReceiver>();

            _solarPanel = (ModuleDeployableSolarPanel)this.part.FindModuleImplementing<ModuleDeployableSolarPanel>();
            if (_solarPanel == null) return;

            if (this.part.FindModuleImplementing<ModuleJettison>() == null)
            {
                UnityEngine.Debug.Log("[KSPI]: FNSolarPanelWasteHeatModule Force Activated  " + part.name);
                part.force_activate();
            }

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            outputResource = _solarPanel.resHandler.outputResources.FirstOrDefault();
            resourceName = _solarPanel.resourceName;

            if (resourceName == ResourceManager.FNRESOURCE_MEGAJOULES)
                _outputType = ResourceType.megajoule;
            else if (resourceName == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                _outputType = ResourceType.electricCharge;
            else
                _outputType = ResourceType.other;

            // only manage power buffer when microwave receiver is not available
            if (_outputType !=  ResourceType.other && _microwavePowerReceiver == null)
            {
                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES));
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE));
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, (double)(decimal)(_outputType == ResourceType.electricCharge ? _solarPanel.chargeRate * 0.001f : _solarPanel.chargeRate));
                _resourceBuffers.UpdateVariable(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, (double)(decimal)(_outputType == ResourceType.electricCharge ? _solarPanel.chargeRate : _solarPanel.chargeRate * 1000));
                _resourceBuffers.Init(part);
            }

            _stars = KopernicusHelper.Stars;
        }

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            _active = true;
            base.OnFixedUpdate();
        }


        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!_active)
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

        public override void OnUpdate()
        {
            if (megaJouleSolarPowerSupplyField != null)
                megaJouleSolarPowerSupplyField.guiActive = solarMaxSupply > 0;

            if (solarMaxSupplyField != null)
                solarMaxSupplyField.guiActive = solarMaxSupply > 0;
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (_solarPanel == null) return;

            if (_field_kerbalism_nominalRate != null)
            {
                kerbalism_nominalRate = _field_kerbalism_nominalRate.GetValue<double>(solarPanelFixer);
                kerbalism_panelState = _field_kerbalism_panelStatus.GetValue<string>(solarPanelFixer);

                var kerbalism_panelStateArray = kerbalism_panelState.Split(' ');

                kerbalism_panelOutput = kerbalism_panelStateArray[0];

                double.TryParse(kerbalism_panelOutput, out kerbalism_panelPower);
            }

            if (outputResource != null)
            {
                outputResourceRate = outputResource.rate;
                outputResourceCurrentRequest = outputResource.currentRequest;
            }

            if (_outputType == ResourceType.other) return;

            //_flowRate = _solarPanel._flowRate;
            //flowRate = _solarPanel.flowRate;
            chargeRate = _solarPanel.chargeRate;
            efficiencyMult = _solarPanel.efficiencyMult;
            _efficMult = _solarPanel._efficMult;

            calculatedEfficency = _solarPanel._efficMult > 0 
                ? _solarPanel._efficMult
                : (double)(decimal)_solarPanel.temperatureEfficCurve.Evaluate((Single)part.skinTemperature) * (double)(decimal)_solarPanel.timeEfficCurve.Evaluate((Single)((Planetarium.GetUniversalTime() - _solarPanel.launchUT) * 1.15740740740741E-05)) * (double)(decimal)_solarPanel.efficiencyMult;
            
            maxSupply = 0;
            solarRate = 0;

            sunAOA = 0;
            CalculateSolarFlowRate(calculatedEfficency / scale, ref maxSupply, ref solarRate);

            if (_resourceBuffers != null)
                _resourceBuffers.UpdateBuffers();

            if (kerbalism_panelPower > 0)
                part.RequestResource(_solarPanel.resourceName, kerbalism_panelPower * fixedDeltaTime);
            else if (outputResourceCurrentRequest > 0)
                part.RequestResource(_solarPanel.resourceName, outputResourceCurrentRequest);
            //else if (flowRate > 0)
            //    part.RequestResource(_solarPanel.resourceName, flowRate * fixedDeltaTime);
            //else if (_flowRate> 0)
            //    part.RequestResource(_solarPanel.resourceName, _flowRate * fixedDeltaTime);
            else
                part.RequestResource(_solarPanel.resourceName, solarRate * fixedDeltaTime);

            // provide power to supply manager
            solar_supply = _outputType == ResourceType.megajoule ? solarRate : solarRate * 0.001;
            solarMaxSupply = _outputType == ResourceType.megajoule ? maxSupply : maxSupply * 0.001;

            megaJouleSolarPowerSupply = supplyFNResourcePerSecondWithMax(solar_supply, solarMaxSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        private void CalculateSolarFlowRate(double efficency, ref double maxSupply, ref double solarRate)
        {
            if (_solarPanel.deployState != ModuleDeployablePart.DeployState.EXTENDED)
                return;

            foreach (var star in _stars)
            {
                double distMult = GetSolarDistanceMultiplier(vessel, star.star, astronomicalUnit) * star.relativeLuminocity;
                double starMaxSupply = chargeRate * distMult * efficency;
                maxSupply += starMaxSupply;

                Vector3d trackDirection = (star.star.position - _solarPanel.panelRotationTransform.position).normalized;

                bool trackingLos = GetLineOfSight(_solarPanel, star, trackDirection);

                if (!trackingLos) continue;

                double sunAoa;

                if (_solarPanel.panelType == ModuleDeployableSolarPanel.PanelType.FLAT)
                    sunAoa = (double)(decimal)Mathf.Clamp(Vector3.Dot(_solarPanel.trackingDotTransform.forward, trackDirection), 0f, 1f);
                else if (_solarPanel.panelType != ModuleDeployableSolarPanel.PanelType.CYLINDRICAL)
                    sunAoa = 0.25;
                else
                {
                    Vector3 direction;
                    if (_solarPanel.alignType == ModuleDeployablePart.PanelAlignType.PIVOT)
                        direction = _solarPanel.trackingDotTransform.forward;
                    else if (_solarPanel.alignType != ModuleDeployablePart.PanelAlignType.X)
                        direction = _solarPanel.alignType != ModuleDeployablePart.PanelAlignType.Y ? part.partTransform.forward : part.partTransform.up;
                    else
                        direction = part.partTransform.right;

                    sunAoa = (1d - (double)(decimal)Math.Abs(Vector3.Dot(direction, trackDirection))) * 0.318309873;
                }

                sunAOA += sunAoa;
                solarRate += starMaxSupply * sunAoa;
            }
        }

        private static bool GetLineOfSight(ModuleDeployablePart solarPanel, StarLight star, Vector3d trackDir)
        {
            var old = solarPanel.trackingBody;
            solarPanel.trackingTransformLocal = star.star.transform;
            solarPanel.trackingTransformScaled = star.star.scaledBody.transform;
            string blockingObject = "";
            var trackingLos = solarPanel.CalculateTrackingLOS(trackDir, ref blockingObject);
            solarPanel.trackingTransformLocal = old.transform;
            solarPanel.trackingTransformScaled = old.scaledBody.transform;
            return trackingLos;
        }

        private static double GetSolarDistanceMultiplier(Vessel vessel, CelestialBody star, double astronomicalUnit)
        {
            var distanceToSurfaceStar = (vessel.CoMD - star.position).magnitude - star.Radius;
            var distanceInAu = distanceToSurfaceStar / astronomicalUnit;
            return 1d / (distanceInAu * distanceInAu);
        }      
    }
}