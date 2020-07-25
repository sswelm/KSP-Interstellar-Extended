using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using UnityEngine;

namespace PhotonSail
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] {GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR})]
    public sealed class PhotonSailor : ScenarioModule
    {
        /// <summary> global access </summary>
        public static PhotonSailor Fetch { get; private set; } = null;

        private static Dictionary<Part, Vector3> initializedParts = new Dictionary<Part, Vector3>();

        public PhotonSailor()
        {
            // enable global access
            Fetch = this;
        }

        private void OnDestroy()
        {
            Fetch = null;
        }

        public override void OnLoad(ConfigNode node)
        {
            // everything in there will be called only one time : the first time a game is loaded from the main menu
        }

        void FixedUpdate()
        {
            // for each vessel
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.loaded || vessel.isEVA || vessel.isActiveVessel)
                    continue;

                if (vessel.vesselType == VesselType.Debris
                    || vessel.vesselType == VesselType.Flag
                    || vessel.vesselType == VesselType.SpaceObject 
                    || vessel.vesselType == VesselType.DeployedSciencePart 
                    || vessel.vesselType == VesselType.DeployedScienceController
                    )
                    continue;

                if ((Vessel.Situations.LANDED & vessel.situation) == Vessel.Situations.LANDED)
                    continue;

                if ((Vessel.Situations.SPLASHED & vessel.situation) == Vessel.Situations.SPLASHED)
                    continue;

                if ((Vessel.Situations.PRELAUNCH & vessel.situation) == Vessel.Situations.PRELAUNCH)
                    continue;

                double totalVesselMass = 0;

                // for each part
                foreach (ProtoPartSnapshot protoPartSnapshot in vessel.protoVessel.protoPartSnapshots)
                {
                    totalVesselMass += protoPartSnapshot.mass;
                    foreach (var resource in protoPartSnapshot.resources)
                    {
                        totalVesselMass += resource.amount * resource.definition.density;
                    }
                }

                Debug.Log("[PhotonSailor]: totalVesselMass: " + totalVesselMass);
                double totalVesselMassInKg = totalVesselMass * 1000;

                // for each part
                foreach (ProtoPartSnapshot protoPartSnapshot in vessel.protoVessel.protoPartSnapshots)
                {
                    BackgroundSolarSail(protoPartSnapshot, vessel, totalVesselMassInKg);
                }
            }
        }

        private static void BackgroundSolarSail(ProtoPartSnapshot protoPartSnapshot, Vessel vessel, double totalVesselMassInKg)
        {
            AvailablePart availablePart = PartLoader.getPartInfoByName(protoPartSnapshot.partName);

            // get part prefab (required for module properties)
            Part partPrefab = availablePart?.partPrefab;

            // get modulePhotonSail
            ModulePhotonSail modulePhotonSail = partPrefab?.FindModuleImplementing<ModulePhotonSail>();

            if (modulePhotonSail is null)
                return;

            ProtoPartModuleSnapshot protoPartModuleSnapshot = protoPartSnapshot.modules.FirstOrDefault(m => m.moduleName == nameof(ModulePhotonSail));

            if (protoPartModuleSnapshot == null)
                return;

            // load modulePhotonSail IsEnabled
            if (!protoPartModuleSnapshot.moduleValues.TryGetValue(nameof(modulePhotonSail.IsEnabled), ref modulePhotonSail.IsEnabled))
                return;

            if (!modulePhotonSail.IsEnabled)
                return;

            Transform vesselTransform = vessel.GetTransform();
            Vector3d positionVessel = vessel.GetWorldPos3D();

            // update solar flux
            modulePhotonSail.UpdateSolarFlux(Planetarium.GetUniversalTime(), positionVessel, vessel);

            foreach (var starLight in KopernicusHelper.Stars)
            {
                Vector3d vesselNormal = vesselTransform.up.normalized;

                // calculate vector between vessel and star transmitter
                Vector3d powerSourceToVesselVector = positionVessel - starLight.position;

                // Magnitude of force proportional to cosine-squared of angle between sun-line and normal
                var cosConeAngle = Vector3d.Dot(powerSourceToVesselVector.normalized, vesselNormal);

                // If normal points away from sun, negate so our force is always away from the sun
                // so that turning the backside towards the sun thrusts correctly
                if (cosConeAngle < 0)
                {
                    vesselNormal = -vesselNormal;
                    cosConeAngle = -cosConeAngle;
                }

                //Debug.Log("[PhotonSailor]: starLight.solarFlux " + starLight.solarFlux + " modulePhotonSail.surfaceArea " + modulePhotonSail.surfaceArea);

                var energyOnSailInWatt = starLight.solarFlux * modulePhotonSail.surfaceArea;

                // calculate effective radiation pressure on solarSail
                var reflectedRadiationPressureOnSail = 2 * energyOnSailInWatt / GameConstants.speedOfLight * cosConeAngle;

                var photonReflectionRatio = 1;

                var reflectedPhotonForceVector = vesselNormal * reflectedRadiationPressureOnSail * photonReflectionRatio * cosConeAngle;

                // calculate acceleration
                var totalAccelerationVector = totalVesselMassInKg > 0 ? reflectedPhotonForceVector / totalVesselMassInKg : Vector3d.zero;

                //Debug.Log("[PhotonSailor]: apply force " + totalAccelerationVector.magnitude);

                // apply force
                ModulePhotonSail.ChangeVesselVelocity(vessel, Planetarium.GetUniversalTime(), totalAccelerationVector * TimeWarp.fixedDeltaTime);
            }
        }
    }
}
