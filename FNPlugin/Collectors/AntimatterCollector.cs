﻿using System;
using System.Linq;
using FNPlugin.Extensions;
using FNPlugin.Resources;

namespace FNPlugin 
{
    class AntimatterCollector : ResourceSuppliableModule    
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Collecting"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool active = true;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Flux")]
        public string ParticleFlux;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Rate", guiFormat = "F4", guiUnits = " mg/hour")]
        public double collectionRate;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Collection Multiplier")]
        public double collectionMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Field Strength Multiplier", guiFormat = "F2")]
        public double celestrialBodyFieldStrengthMod = 1;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Can collect")]
        public bool canCollect = true;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Usage", guiUnits = " KW/s")]
        public double powerReqKW;
        [KSPField(isPersistant = true, guiActive = false)]
        public double flux;

        private PartResourceDefinition _antimatterDef;
        private ModuleAnimateGeneric _moduleAnimateGeneric;
        private CelestialBody _homeworld;

        private double _effectiveFlux;
        private double _offlineResource;
        
        public override void OnStart(PartModule.StartState state) 
        {
            _antimatterDef = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Antimatter);

            _moduleAnimateGeneric = part.FindModuleImplementing<ModuleAnimateGeneric>();

            powerReqKW = Math.Pow(collectionMultiplier, 0.9);

            if (state == StartState.Editor) return;

            _homeworld = FlightGlobals.fetch.bodies.First(m => m.isHomeWorld == true);

            if (last_active_time == 0 || !(vessel.orbit.eccentricity < 1) || !active || !canCollect) return;

            var vesselAvgAlt = (vessel.orbit.ApA + vessel.orbit.PeA) / 2;
            flux = collectionMultiplier * 0.5 * (vessel.mainBody.GetBeltAntiparticles(_homeworld, vesselAvgAlt, vessel.orbit.inclination) + vessel.mainBody.GetBeltAntiparticles(_homeworld, vesselAvgAlt, 0.0));
            var timeDiff = Planetarium.GetUniversalTime() - last_active_time;
            _offlineResource = timeDiff * flux;
        }

        public override void OnUpdate() 
        {
            var lat = vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            celestrialBodyFieldStrengthMod = MagneticFieldDefinitionsHandler.GetMagneticFieldDefinitionForBody(vessel.mainBody).StrengthMult;
            flux = collectionMultiplier * vessel.mainBody.GetBeltAntiparticles(_homeworld, vessel.altitude, lat);
            ParticleFlux = flux.ToString("E");
            collectionRate = _effectiveFlux * PluginHelper.SecondsInHour;
            canCollect = _moduleAnimateGeneric == null ? true :  _moduleAnimateGeneric.GetScalar == 1;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!active || !canCollect)
            {
                _effectiveFlux = 0;
                return;
            }

            var fixedPowerReqKW = powerReqKW * TimeWarp.fixedDeltaTime;
            // first attemp to get power more megajoule network
            var fixedRecievedChargeKW = CheatOptions.InfiniteElectricity
                ? fixedPowerReqKW
                : consumeFNResource(fixedPowerReqKW * 0.001, ResourceManager.FNRESOURCE_MEGAJOULES, TimeWarp.fixedDeltaTime) * 1000;
            // alternativly attempt to use electric charge
            if (fixedRecievedChargeKW <= fixedPowerReqKW)
                fixedRecievedChargeKW += part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, fixedPowerReqKW - fixedRecievedChargeKW);
            var powerRatio = fixedPowerReqKW > 0 ? fixedRecievedChargeKW / fixedPowerReqKW : 0;
            
            _effectiveFlux = powerRatio * flux;
            part.RequestResource(_antimatterDef.id, -_effectiveFlux * TimeWarp.fixedDeltaTime - _offlineResource, ResourceFlowMode.STACK_PRIORITY_SEARCH);
            _offlineResource = 0;
            last_active_time = Planetarium.GetUniversalTime();
        }

        public override int getPowerPriority()
        {
            return 4;
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title;
        }

    }
}
