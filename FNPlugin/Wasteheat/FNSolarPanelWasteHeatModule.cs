using FNPlugin.Constants;
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
        public const string GROUP = "FNSolarPanelWasteHeatModule";
        public const string GROUP_TITLE = "Interstellar Solar Generator";

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_SolarPanelWH_CurrentSolarPower")]//Current Solar Power
        public string mjSolarSupply;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_SolarPanelWH_MaximumSolarPower")]//Maximum Solar Power
        public string mjMaxSupply;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "AU", guiFormat = "F0", guiUnits = " m")]
        public double astronomicalUnit;

        [KSPField]
        public double solarMaxSupply;
        [KSPField]
        public string resourceName;
        [KSPField]
        public double solar_supply;
        [KSPField]
        public float chargeRate;
        [KSPField]
        public double sunAOA;
        [KSPField]
        public double calculatedEfficency;
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
        public double outputResourceRate;
        [KSPField]
        public double outputResourceCurrentRequest;

        BeamedPowerReceiver _microwavePowerReceiver;
        ModuleDeployableSolarPanel _solarPanel;
        ResourceBuffers _resourceBuffers;
        ResourceType _outputType = 0;
        List<StarLight> _stars;

        private BaseField _mjSolarSupplyField;
        private BaseField _mjMaxSupplyField;
        private BaseField _fieldKerbalismNominalRate;
        private BaseField _fieldKerbalismPanelStatus;

        private ModuleResource _outputResource;
        private PartModule _solarPanelFixer;

        private bool _active;

        public double SolarPower => solar_supply;

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            _mjSolarSupplyField = Fields[nameof(mjSolarSupply)];
            _mjMaxSupplyField = Fields[nameof(mjMaxSupply)];

            if (part.Modules.Contains("SolarPanelFixer"))
            {
                _solarPanelFixer = part.Modules["SolarPanelFixer"];

                _fieldKerbalismNominalRate = _solarPanelFixer.Fields["nominalRate"];
                _fieldKerbalismPanelStatus = _solarPanelFixer.Fields["panelStatus"];
            }

            // calculate Astronomical unit on homeworld semiMajorAxis when missing
            if (astronomicalUnit <= 0)
                astronomicalUnit = FlightGlobals.GetHomeBody().orbit.semiMajorAxis;

            _microwavePowerReceiver = part.FindModuleImplementing<BeamedPowerReceiver>();

            _solarPanel = part.FindModuleImplementing<ModuleDeployableSolarPanel>();
            if (_solarPanel == null || _solarPanel.chargeRate <= 0) return;

            if (part.FindModuleImplementing<ModuleJettison>() == null)
            {
                Debug.Log("[KSPI]: FNSolarPanelWasteHeatModule Force Activated  " + part.name);
                part.force_activate();
            }

            string[] resourcesToSupply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resourcesToSupply;
            base.OnStart(state);

            _outputResource = _solarPanel.resHandler.outputResources.FirstOrDefault();

            resourceName = _solarPanel.resourceName;

            if (resourceName == ResourceManager.FNRESOURCE_MEGAJOULES)
                _outputType = ResourceType.megajoule;
            else if (resourceName == ResourceSettings.Config.ElectricChargePower)
                _outputType = ResourceType.electricCharge;
            else
                _outputType = ResourceType.other;

            // only manage power buffer when microwave receiver is not available
            if (_outputType !=  ResourceType.other && _microwavePowerReceiver == null)
            {
                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES));
                _resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricChargePower));
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, _outputType == ResourceType.electricCharge ? _solarPanel.chargeRate / GameConstants.ecPerMJ : _solarPanel.chargeRate);
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricChargePower, _outputType == ResourceType.electricCharge ? _solarPanel.chargeRate : _solarPanel.chargeRate * GameConstants.ecPerMJ);
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
            if (_mjSolarSupplyField != null)
                _mjSolarSupplyField.guiActive = solarMaxSupply > 0;

            if (_mjMaxSupplyField != null)
                _mjMaxSupplyField.guiActive = solarMaxSupply > 0;
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (_solarPanel == null) return;

            if (_fieldKerbalismNominalRate != null)
            {
                kerbalism_nominalRate = _fieldKerbalismNominalRate.GetValue<double>(_solarPanelFixer);
                kerbalism_panelState = _fieldKerbalismPanelStatus.GetValue<string>(_solarPanelFixer);

                var kerbalismPanelStateArray = kerbalism_panelState.Split(' ');

                kerbalism_panelOutput = kerbalismPanelStateArray[0];

                double.TryParse(kerbalism_panelOutput, out kerbalism_panelPower);
            }

            if (_outputResource != null && _solarPanel.deployState == ModuleDeployablePart.DeployState.EXTENDED)
            {
                outputResourceRate = _outputResource.rate;
                outputResourceCurrentRequest = _outputResource.currentRequest;
            }
            else
            {
                outputResourceRate = 0;
                outputResourceCurrentRequest = 0;
            }

            if (_outputType == ResourceType.other) return;

            chargeRate = _solarPanel.chargeRate;

            double age = (Planetarium.GetUniversalTime() - _solarPanel.launchUT) * 1.15740740740741E-05;
            calculatedEfficency = _solarPanel._efficMult > 0 ? _solarPanel._efficMult :
                _solarPanel.temperatureEfficCurve.Evaluate((float)part.skinTemperature) *
                _solarPanel.timeEfficCurve.Evaluate((float)age) * _solarPanel.efficiencyMult;

            double maxSupply = 0.0, solarRate = 0.0;
            sunAOA = 0;
            CalculateSolarFlowRate(calculatedEfficency / scale, ref maxSupply, ref solarRate);

            _resourceBuffers?.UpdateBuffers();

            if (_outputResource != null)
            {
                if (kerbalism_panelPower > 0)
                    part.RequestResource(_solarPanel.resourceName, kerbalism_panelPower * fixedDeltaTime);
                else if (_outputResource != null)
                    _outputResource.rate = 0;
                else
                    part.RequestResource(_solarPanel.resourceName, solarRate * fixedDeltaTime);
            }

            // provide power to supply manager
            solar_supply = _outputType == ResourceType.megajoule ? solarRate : solarRate / GameConstants.ecPerMJ;
            solarMaxSupply = _outputType == ResourceType.megajoule ? maxSupply : maxSupply / GameConstants.ecPerMJ;

            mjSolarSupply = PluginHelper.getFormattedPowerString(supplyFNResourcePerSecondWithMax(solar_supply, solarMaxSupply, ResourceManager.FNRESOURCE_MEGAJOULES));
            mjMaxSupply = PluginHelper.getFormattedPowerString(solarMaxSupply);
        }

        private void CalculateSolarFlowRate(double efficiency, ref double maximumSupply, ref double solarPowerRate)
        {
            if (_solarPanel.deployState != ModuleDeployablePart.DeployState.EXTENDED)
                return;

            foreach (var star in _stars)
            {
                double distanceMultiplier = GetSolarDistanceMultiplier(vessel, star.star, astronomicalUnit) * star.relativeLuminocity;
                double starMaxSupply = chargeRate * distanceMultiplier * efficiency;
                maximumSupply += starMaxSupply;

                Vector3d trackDirection = (star.star.position - _solarPanel.panelRotationTransform.position).normalized;

                bool trackingLos = GetLineOfSight(_solarPanel, star, trackDirection);

                if (!trackingLos) continue;

                double sunAoa;

                if (_solarPanel.panelType == ModuleDeployableSolarPanel.PanelType.FLAT)
                    sunAoa = Mathf.Clamp(Vector3.Dot(_solarPanel.trackingDotTransform.forward, trackDirection), 0f, 1f);
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

                    sunAoa = (1d - Math.Abs(Vector3.Dot(direction, trackDirection))) * 0.318309873;
                }

                sunAOA += sunAoa;
                solarPowerRate += starMaxSupply * sunAoa;
            }
        }

        private static bool GetLineOfSight(ModuleDeployablePart solarPanel, StarLight star, Vector3d trackDir)
        {
            var trackingBody = solarPanel.trackingBody;
            solarPanel.trackingTransformLocal = star.star.transform;
            solarPanel.trackingTransformScaled = star.star.scaledBody.transform;
            var blockingObject = "";
            var trackingLos = solarPanel.CalculateTrackingLOS(trackDir, ref blockingObject);
            solarPanel.trackingTransformLocal = trackingBody.transform;
            solarPanel.trackingTransformScaled = trackingBody.scaledBody.transform;
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
