using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    public class AntimatterCollector : PartModule    
    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Flux")]
        public string ParticleFlux;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Rate", guiFormat = "F4", guiUnits = " mg/day")]
        public double collectionRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Multiplier")]
        public double collectionMultiplier;
        [KSPField(isPersistant = true)]
        public double last_active_time;

        protected PartResourceDefinition antimatter_def;
        
        public override void OnStart(PartModule.StartState state) 
        {
            antimatter_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Antimatter);

            if (state == StartState.Editor) return;

            if (last_active_time != 0 && vessel.orbit.eccentricity < 1) 
            {
                double lat = vessel.mainBody.GetLatitude(vessel.transform.position);
                double vessel_avg_alt = (vessel.orbit.ApR + vessel.orbit.PeR) / 2.0;
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
            double flux = collectionMultiplier * vessel.mainBody.GetBeltAntiparticles(vessel.altitude, lat);
            ParticleFlux = flux.ToString("E");
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            
            double lat = vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            double flux = collectionMultiplier * vessel.mainBody.GetBeltAntiparticles(vessel.altitude, lat);
            part.RequestResource(antimatter_def.id, -flux * TimeWarp.fixedDeltaTime, ResourceFlowMode.STACK_PRIORITY_SEARCH);
            last_active_time = Planetarium.GetUniversalTime();
            collectionRate = flux * PluginHelper.SecondsInDay;
        }
    }
}
