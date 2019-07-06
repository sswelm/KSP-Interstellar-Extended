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
        [KSPField( guiActive = true,  guiName = "Current Solar Power", guiUnits = " MW", guiFormat="F5")]
        public double megaJouleSolarPowerSupply;
        [KSPField(guiActive = true, guiName = "Maximum Solar Power", guiUnits = " MW", guiFormat = "F5")]
        public double solarMaxSupply = 0;

        [KSPField(guiActive = false, guiName = "AU", guiFormat = "F0", guiUnits = " m")]
        public double astronomicalUnit;

        [KSPField(guiActive = false)]
        public double solar_supply = 0;
        [KSPField(guiActive = false)]
        public double chargeRate;
        [KSPField(guiActive = false)]
        public double sunAOA;

        BeamedPowerReceiver _microwavePowerReceiver;
        ModuleDeployableSolarPanel _solarPanel;
        ResourceBuffers _resourceBuffers;
        ResourceType outputType = 0;
        List<StarLight> stars;        
      
        public double SolarPower
        {
            get { return solar_supply; }
        }

        bool active = false;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            // calculate Astronomical unit on homeworld semiMajorAxis when missing
            if (astronomicalUnit == 0)
                astronomicalUnit = FlightGlobals.GetHomeBody().orbit.semiMajorAxis;

            _microwavePowerReceiver = part.FindModuleImplementing<BeamedPowerReceiver>();

            _solarPanel = (ModuleDeployableSolarPanel)this.part.FindModuleImplementing<ModuleDeployableSolarPanel>();
            if (_solarPanel == null) return;

            part.force_activate();

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            if (_solarPanel.resourceName == ResourceManager.FNRESOURCE_MEGAJOULES)
                outputType = ResourceType.megajoule;
            else if (_solarPanel.resourceName == ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE)
                outputType = ResourceType.electricCharge;
            else
                outputType = ResourceType.other;

            // only manager power buffer when microwave receiver is not available
            if (outputType !=  ResourceType.other && _microwavePowerReceiver == null)
            {
                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES));
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE));
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, (double)(decimal)(outputType == ResourceType.electricCharge ? _solarPanel.chargeRate * 0.001f : _solarPanel.chargeRate));
                _resourceBuffers.UpdateVariable(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, (double)(decimal)(outputType == ResourceType.electricCharge ? _solarPanel.chargeRate : _solarPanel.chargeRate * 1000));
                _resourceBuffers.Init(this.part);
            }

            stars = KopernicusHelper.Stars;
        }

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            active = true;
            base.OnFixedUpdate();
        }


        public void FixedUpdate()
        {
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
            if (_solarPanel == null) return;

            if (outputType == ResourceType.other) return;
          
            chargeRate = (double)(decimal)_solarPanel.chargeRate;
            sunAOA = 0;

            double efficency = _solarPanel._efficMult > 0 
                ? _solarPanel._efficMult
                : (double)(decimal)_solarPanel.temperatureEfficCurve.Evaluate((Single)part.skinTemperature) * (double)(decimal)_solarPanel.timeEfficCurve.Evaluate((Single)((Planetarium.GetUniversalTime() - _solarPanel.launchUT) * 1.15740740740741E-05)) * (double)(decimal)_solarPanel.efficiencyMult;
            
            double maxSupply = 0;
            double solar_rate = 0;

            CalculateSolarFlowRate(efficency, ref maxSupply, ref solar_rate);

            if (_resourceBuffers != null)
                _resourceBuffers.UpdateBuffers();

            // extract power otherwise we end up with double power
            part.RequestResource(_solarPanel.resourceName, solar_rate * fixedDeltaTime);

            // provide power to supply manager
            solar_supply = outputType == ResourceType.megajoule ? solar_rate : solar_rate * 0.001;
            solarMaxSupply = outputType == ResourceType.megajoule ? maxSupply : maxSupply * 0.001;

            megaJouleSolarPowerSupply = supplyFNResourcePerSecondWithMax(solar_supply, solarMaxSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        private void CalculateSolarFlowRate(double efficency, ref double maxSupply, ref double solar_rate)
        {
            if (_solarPanel.deployState != ModuleDeployablePart.DeployState.EXTENDED)
                return;

            foreach (var star in stars)
            {
                double _distMult = GetSolarDistanceMultiplier(vessel, star.star, astronomicalUnit) * star.relativeLuminocity;
                double _maxSupply = chargeRate * _distMult * efficency;
                maxSupply += _maxSupply;

                Vector3d trackDirection = (star.star.position - _solarPanel.panelRotationTransform.position).normalized;

                var trackingLOS = GetLineOfSight(_solarPanel, star, trackDirection);

                if (trackingLOS)
                {
                    double _sunAOA;

                    if (_solarPanel.panelType == ModuleDeployableSolarPanel.PanelType.FLAT)
                        _sunAOA = (double)(decimal)Mathf.Clamp(Vector3.Dot(_solarPanel.trackingDotTransform.forward, trackDirection), 0f, 1f);
                    else if (_solarPanel.panelType != ModuleDeployableSolarPanel.PanelType.CYLINDRICAL)
                        _sunAOA = 0.25;
                    else
                    {
                        Vector3 direction;
                        if (_solarPanel.alignType == ModuleDeployableSolarPanel.PanelAlignType.PIVOT)
                            direction = _solarPanel.trackingDotTransform.forward;
                        else if (_solarPanel.alignType != ModuleDeployableSolarPanel.PanelAlignType.X)
                            direction = _solarPanel.alignType != ModuleDeployablePart.PanelAlignType.Y ? part.partTransform.forward : part.partTransform.up;
                        else
                            direction = part.partTransform.right;

                        _sunAOA = (1d - (double)(decimal)Math.Abs(Vector3.Dot(direction, trackDirection))) * 0.318309873;
                    }

                    sunAOA += _sunAOA;
                    solar_rate += _maxSupply * _sunAOA;
                }
            }
        }

        private static bool GetLineOfSight(ModuleDeployableSolarPanel solarPanel, StarLight star, Vector3d trackDir)
        {
            CelestialBody old = solarPanel.trackingBody;
            solarPanel.trackingTransformLocal = star.star.transform;
            solarPanel.trackingTransformScaled = star.star.scaledBody.transform;
            string blockingObject = "";
            var trackingLOS = solarPanel.CalculateTrackingLOS(trackDir, ref blockingObject);
            solarPanel.trackingTransformLocal = old.transform;
            solarPanel.trackingTransformScaled = old.scaledBody.transform;
            return trackingLOS;
        }

        private static double GetSolarDistanceMultiplier(Vessel vessel, CelestialBody star, double astronomicalUnit)
        {
            var distanceToSurfaceStar = (vessel.CoMD - star.position).magnitude - star.Radius;
            var distanceInAU = distanceToSurfaceStar / astronomicalUnit;
            return 1d / (distanceInAU * distanceInAU);
        }      
    }
}