using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using System;
using System.Linq;

namespace FNPlugin
{
    class AntimatterCollector : ResourceSuppliableModule
    {
        public const string GROUP = "AntimatterCollector";
        public const string GROUP_TITLE = "#LOC_KSPIE_AntimatterCollector_groupName";

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterCollector_Collecting"), UI_Toggle(disabledText = "#LOC_KSPIE_AntimatterCollector_Collecting_Off", enabledText = "#LOC_KSPIE_AntimatterCollector_Collecting_On")]//Collecting--Off--On
        public bool active = true;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_ParticleFlux")]//Antimatter Flux
        public string ParticleFlux;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_CollectionRate", guiFormat = "F4", guiUnits = " mg/hour")]//Rate
        public double collectionRate;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterCollector_CollectionMultiplier")]//Collection Multiplier
        public double collectionMultiplier = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_CelestrialBodyFieldStrengthMod", guiFormat = "F2")]//Field Strength Multiplier
        public double celestrialBodyFieldStrengthMod = 1;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_CanCollect")]//Can collect
        public bool canCollect = true;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_PowerReqKW", guiUnits = " KW")]//Power Usage
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
            _antimatterDef = PartResourceLibrary.Instance.GetDefinition(ResourcesConfiguration.Instance.AntiProtium);

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

            double receivedPowerKW = consumeMegawatts(powerReqKW / GameConstants.ecPerMJ, true, false, true) * GameConstants.ecPerMJ;
            double powerRatio = powerReqKW > 0.0 ? receivedPowerKW / powerReqKW : 0.0;

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
