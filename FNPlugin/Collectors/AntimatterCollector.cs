using System;

namespace FNPlugin 
{
    class AntimatterCollector : FNResourceSuppliableModule    
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Collecting"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool active = true;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Flux")]
        public string ParticleFlux;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Rate", guiFormat = "F4", guiUnits = " mg/day")]
        public double collectionRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Multiplier")]
        public double collectionMultiplier = 1;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Can collect")]
        public bool canCollect = true;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Usage", guiUnits = " KW/s")]
        public double powerReqKW;
        [KSPField(isPersistant = true, guiActive = false)]
        public double flux;

        private PartResourceDefinition antimatter_def;
        private ModuleAnimateGeneric _moduleAnimateGeneric;

        
        public override void OnStart(PartModule.StartState state) 
        {
            antimatter_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Antimatter);

            _moduleAnimateGeneric = part.FindModuleImplementing<ModuleAnimateGeneric>();

            powerReqKW = Math.Pow(collectionMultiplier, 0.9);

            if (state == StartState.Editor) return;

            if (last_active_time != 0 && vessel.orbit.eccentricity < 1 && active && canCollect) 
            {
                double lat = vessel.mainBody.GetLatitude(vessel.transform.position);
                double vessel_avg_alt = (vessel.orbit.ApR + vessel.orbit.PeR) / 2;
                double vessel_inclination = vessel.orbit.inclination;
                double flux = collectionMultiplier * 0.5 * (vessel.mainBody.GetBeltAntiparticles(vessel_avg_alt, vessel_inclination) + vessel.mainBody.GetBeltAntiparticles(vessel_avg_alt, 0.0));
                double time_diff = Planetarium.GetUniversalTime() - last_active_time;
                double antimatter_to_add = time_diff * flux;
                part.RequestResource(antimatter_def.id, -antimatter_to_add, ResourceFlowMode.STACK_PRIORITY_SEARCH);
            }
        }

        public override void OnUpdate() 
        {
            double lat = vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            flux = collectionMultiplier * vessel.mainBody.GetBeltAntiparticles(vessel.altitude, lat);
            ParticleFlux = flux.ToString("E");

            canCollect = _moduleAnimateGeneric != null ? _moduleAnimateGeneric.GetScalar == 1 : true;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!active || !canCollect)
            {
                collectionRate = 0;
                return;
            }

            var fixedPowerReqKW = powerReqKW * TimeWarp.fixedDeltaTime;
            // first attemp to get power more megajoule network
            var fixedRecievedChargeKW = CheatOptions.InfiniteElectricity
                ? fixedPowerReqKW
                : consumeFNResource(fixedPowerReqKW / 1000, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000;
            // alternativly attempt to use electric charge
            if (fixedRecievedChargeKW <= fixedPowerReqKW)
                fixedRecievedChargeKW += part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, fixedPowerReqKW - fixedRecievedChargeKW);
            var powerRatio = fixedPowerReqKW > 0 ? fixedRecievedChargeKW / fixedPowerReqKW : 0;
            
            var effectiveFlux = powerRatio * flux;
            part.RequestResource(antimatter_def.id, -effectiveFlux * TimeWarp.fixedDeltaTime, ResourceFlowMode.STACK_PRIORITY_SEARCH);
            last_active_time = Planetarium.GetUniversalTime();
            collectionRate = effectiveFlux * PluginHelper.SecondsInDay;
        }

        public override int getPowerPriority()
        {
            return 4;
        }
    }
}
