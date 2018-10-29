using FNPlugin.Power;
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

    class StarLight
    {
        public CelestialBody star;
        public double luminocity;
    }

    [KSPModule("Solar Panel Adapter")]
    class FNSolarPanelWasteHeatModule : ResourceSuppliableModule, ISolarPower
    {
        [KSPField( guiActive = true,  guiName = "Current Solar Power", guiUnits = " MW", guiFormat="F5")]
        public double megaJouleSolarPowerSupply;
        [KSPField(guiActive = true, guiName = "Maximum Solar Power", guiUnits = " MW", guiFormat = "F5")]
        public double solarMaxSupply = 0;

        [KSPField(guiActive = true, guiName = "AU", guiFormat = "F0", guiUnits = " m")]
        public double astronomicalUnit;

        [KSPField(guiActive = false)]
        public double solar_supply = 0;
        [KSPField(guiActive = false)]
        public double chargeRate;
        [KSPField(guiActive = false)]
        public double sunAOA;

        MicrowavePowerReceiver _microwavePowerReceiver;
        ModuleDeployableSolarPanel _solarPanel;
        ResourceBuffers _resourceBuffers;
        ModuleResource _solarFlowRateResource;
        ResourceType outputType = 0;

        static List<StarLight> stars = new List<StarLight>();
      
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

            _microwavePowerReceiver = part.FindModuleImplementing<MicrowavePowerReceiver>();
            //if (_microwavePowerReceiver != null)
            //{
            //    Fields["megaJouleSolarPowerSupply"].guiActive = false;
            //    Fields["solarMaxSupply"].guiActive = false;
            //    return;
            //}

            _solarPanel = (ModuleDeployableSolarPanel)this.part.FindModuleImplementing<ModuleDeployableSolarPanel>();
            if (_solarPanel == null) return;

            _solarFlowRateResource = new ModuleResource();
            _solarFlowRateResource.name = _solarPanel.resourceName;
            resHandler.inputResources.Add(_solarFlowRateResource);

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

            ExtractKopernicusStarData("FNSolarPanel");
        }

        public override void OnFixedUpdate()
        {
            //if (_microwavePowerReceiver != null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

            active = true;
            base.OnFixedUpdate();
        }


        public void FixedUpdate()
        {
            //if (_microwavePowerReceiver != null) return;

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
            //if (_microwavePowerReceiver != null) return;

            if (_solarPanel == null) return;

            if (outputType == ResourceType.other) return;
          
            chargeRate = (double)(decimal)_solarPanel.chargeRate;
            sunAOA = 0;

            double efficency = _solarPanel._efficMult > 0 
                ? _solarPanel._efficMult 
                : (_solarPanel.temperatureEfficCurve.Evaluate((Single)part.skinTemperature) * _solarPanel.timeEfficCurve.Evaluate((Single)((Planetarium.GetUniversalTime() - _solarPanel.launchUT) * 1.15740740740741E-05)) * _solarPanel.efficiencyMult);
            
            double maxSupply = 0;
            double solar_rate = 0;

            CalculateSolarFlowRate(efficency, ref maxSupply, ref solar_rate);

            if (_resourceBuffers != null)
                _resourceBuffers.UpdateBuffers();

            // extract power otherwise we end up with double power
            _solarFlowRateResource.rate = solar_rate;

            // provide power to supply manager
            solar_supply = outputType == ResourceType.megajoule ? solar_rate : solar_rate * 0.001;
            solarMaxSupply = outputType == ResourceType.megajoule ? maxSupply : maxSupply * 0.001;

            megaJouleSolarPowerSupply = supplyFNResourcePerSecondWithMax(solar_supply, solarMaxSupply, ResourceManager.FNRESOURCE_MEGAJOULES);
        }

        private void CalculateSolarFlowRate(double efficency, ref double maxSupply, ref double solar_rate)
        {
            foreach (var star in stars)
            {
                double _distMult = GetSolarDistanceMultiplier(vessel, star.star, astronomicalUnit) * star.luminocity;
                double _maxSupply = chargeRate * _distMult * efficency;
                maxSupply += _maxSupply;

                Vector3d trackDir = (star.star.position - _solarPanel.panelRotationTransform.position).normalized;

                var trackingLOS = GetLineOfSight(_solarPanel, star, trackDir);

                if (trackingLOS)
                {
                    double _sunAOA;

                    if (_solarPanel.panelType == ModuleDeployableSolarPanel.PanelType.FLAT)
                        _sunAOA = (double)(decimal)Mathf.Clamp(Vector3.Dot(_solarPanel.trackingDotTransform.forward, trackDir), 0f, 1f);
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

                        _sunAOA = (1d - (double)(decimal)Math.Abs(Vector3.Dot(direction, trackDir))) * 0.318309873;
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
            return 1 / (distanceInAU * distanceInAU);
        }      

        // ToDo: Move to global library
        // Scan the Kopernicus config nodes and extract Kopernicus star data
        protected static void ExtractKopernicusStarData(string modulename)
        {
            // Only need to execute this once 
            if (stars.Count > 0)
                return;

            var debugPrefix = "[" + modulename + "] - ";

            var celestrialBodies = FlightGlobals.Bodies.ToDictionary(m => m.name);

            ConfigNode[] nodeLevel1 = GameDatabase.Instance.GetConfigNodes("Kopernicus");

            if (nodeLevel1.Length > 0)
                Debug.Log(debugPrefix + "Loading Kopernicus Configuration Data");
            else
                Debug.LogWarning(debugPrefix + "Failed to find Kopernicus Configuration Data");

            for (int i = 0; i < nodeLevel1.Length; i++)
            {
                ConfigNode[] celestrialBodyNode = nodeLevel1[i].GetNodes("Body");

                Debug.Log(debugPrefix + "Found " + celestrialBodyNode.Length + " celestrial bodies");

                for (int j = 0; j < celestrialBodyNode.Length; j++)
                {
                    string bodyName = celestrialBodyNode[j].GetValue("name");

                    bool usesSunTemplate = false;

                    ConfigNode sunNode = celestrialBodyNode[j].GetNode("Template");

                    if (sunNode != null)
                    {
                        string templateName = sunNode.GetValue("name");
                        usesSunTemplate = templateName == "Sun";
                        if (usesSunTemplate)
                            Debug.Log(debugPrefix + "Will use default Sun template for " + bodyName);
                    }

                    ConfigNode propertiesNode = celestrialBodyNode[j].GetNode("Properties");

                    float luminocity = 0;

                    if (propertiesNode != null)
                    {
                        string starLuminosityText = propertiesNode.GetValue("starLuminosity");

                        if (string.IsNullOrEmpty(starLuminosityText))
                        {
                            if (usesSunTemplate)
                                Debug.LogWarning(debugPrefix + "starLuminosity in Properties ConfigNode is missing, defaulting to template");
                        }
                        else
                        {
                            float.TryParse(starLuminosityText, out luminocity);
                            CelestialBody celestialBody;

                            if (luminocity > 0 && celestrialBodies.TryGetValue(bodyName, out celestialBody))
                            {
                                Debug.Log(debugPrefix + "Added Star " + celestialBody.name + " with luminocity " + luminocity);
                                stars.Add(new StarLight() { star = celestialBody, luminocity = (double)(decimal)luminocity });
                            }
                            else
                                Debug.LogWarning(debugPrefix + "Failed to initialize star " + bodyName);
                        }
                    }

                    if (usesSunTemplate && luminocity == 0)
                    {
                        CelestialBody celestialBody;

                        if (celestrialBodies.TryGetValue(bodyName, out celestialBody))
                        {
                            Debug.Log(debugPrefix + "Added Star " + celestialBody.name + " with default luminocity of 1");
                            stars.Add(new StarLight() { star = celestialBody, luminocity = 1 });
                        }
                        else
                            Debug.LogWarning(debugPrefix + "Failed to initialize star " + bodyName + " as with a default luminocity of 1");
                    }
                }
            }

            // add local sun if kopernicus configuration was not found or did not contain any star
            var homePlanetSun = Planetarium.fetch.Sun;
            if (!stars.Any(m => m.star.name == homePlanetSun.name))
            {
                Debug.LogWarning("[PhotonSailor] - homeplanet star was not found, adding homeplanet star as default sun");
                stars.Add(new StarLight() { star = Planetarium.fetch.Sun, luminocity = 1 });
            }
        }
    }
}