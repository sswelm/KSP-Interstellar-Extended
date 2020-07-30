using FNPlugin.Constants;
using FNPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhotonSail
{
    public class VesselData
    {
        public double TotalVesselMassInKg { get;  set; }
        public double TotalVesselMass { get;  set; }
        public bool? HasSolarSail { get; set; }
        public uint SolarSailPersistentId { get; set; }
        public ModulePhotonSail ModulePhotonSail { get; set; }
        public ProtoPartModuleSnapshot ProtoPartModuleSnapshot { get; set; }

        public void UpdateMass(Vessel vessel)
        {
            if (TotalVesselMass != 0)
                return;

            // for each part
            foreach (ProtoPartSnapshot protoPartSnapshot in vessel.protoVessel.protoPartSnapshots)
            {
                TotalVesselMass += protoPartSnapshot.mass;
                foreach (var resource in protoPartSnapshot.resources)
                {
                    TotalVesselMass += resource.amount * resource.definition.density;
                }
            }

            TotalVesselMassInKg = TotalVesselMass * 1000;
        }
    }


    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] {GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR})]
    public sealed class PhotonSailor : ScenarioModule
    {
        public Dictionary<uint, VesselData> vesselDataDict = new Dictionary<uint, VesselData>();

        /// <summary> global access </summary>
        public static PhotonSailor Fetch { get; private set; }

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
            base.OnLoad(node);
            // everything in there will be called only one time : the first time a game is loaded from the main menu
        }

        /// <summary>
        /// Called by the part every refresh frame where it is active, which can be less frequent than FixedUpdate which is called every processing frame
        /// </summary>
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

                if ((Vessel.Situations.LANDED & vessel.situation) == Vessel.Situations.LANDED
                    || (Vessel.Situations.SPLASHED & vessel.situation) == Vessel.Situations.SPLASHED
                    || (Vessel.Situations.PRELAUNCH & vessel.situation) == Vessel.Situations.PRELAUNCH 
                    || (Vessel.Situations.FLYING & vessel.situation) == Vessel.Situations.FLYING)
                    continue;

                // lookup vesselData
                if (!vesselDataDict.TryGetValue(vessel.persistentId, out VesselData vesselData))
                {
                    vesselData = new VesselData();
                    vesselDataDict.Add(vessel.persistentId, vesselData);
                }

                // skip further processing if no solar sail present
                if (vesselData.HasSolarSail.HasValue && !vesselData.HasSolarSail.Value)
                    continue;

                //reset mass to initiate a recalculation
                vesselData.TotalVesselMass = 0;

                if (vesselData.ProtoPartModuleSnapshot != null)
                {
                    ProtoPartSnapshot protoPartSnapshot = vessel.protoVessel.protoPartSnapshots.FirstOrDefault(m => m.persistentId == vesselData.SolarSailPersistentId);

                    if (protoPartSnapshot != null)
                        BackgroundSolarSail(protoPartSnapshot, vessel, vesselData);
                    else
                    {
                        vesselData.HasSolarSail = null;
                        vesselData.ProtoPartModuleSnapshot = null;
                        Debug.Log("[PhotonSailor]: Fail to find protoPartSnapshots " + vesselData.SolarSailPersistentId);
                    }
                }
                else
                {
                    foreach (ProtoPartSnapshot protoPartSnapshot in vessel.protoVessel.protoPartSnapshots)
                    {
                        if (BackgroundSolarSail(protoPartSnapshot, vessel, vesselData))
                            break;
                        else
                            vesselData.HasSolarSail = false;
                    }
                }
            }
        }

        private static bool BackgroundSolarSail(ProtoPartSnapshot protoPartSnapshot, Vessel vessel, VesselData vesselData)
        {
            if (vesselData.ProtoPartModuleSnapshot == null)
            {
                AvailablePart availablePart = PartLoader.getPartInfoByName(protoPartSnapshot.partName);

                // get modulePhotonSail
                vesselData.ModulePhotonSail = availablePart?.partPrefab?.FindModuleImplementing<ModulePhotonSail>();

                if (vesselData.ModulePhotonSail is null)
                    return false;

                vesselData.ProtoPartModuleSnapshot = protoPartSnapshot.modules.FirstOrDefault(m => m.moduleName == nameof(ModulePhotonSail));

                if (vesselData.ProtoPartModuleSnapshot == null)
                    return false;

                vesselData.HasSolarSail = true;
                vesselData.SolarSailPersistentId = protoPartSnapshot.persistentId;
            }

            // load modulePhotonSail IsEnabled
            if (!vesselData.ProtoPartModuleSnapshot.moduleValues.TryGetValue(nameof(vesselData.ModulePhotonSail.IsEnabled), ref vesselData.ModulePhotonSail.IsEnabled))
                return false;

            if (!vesselData.ModulePhotonSail.IsEnabled)
                return false;

            Transform vesselTransform = vessel.GetTransform();
            Vector3d positionVessel = vessel.GetWorldPos3D();
            double universalTime = Planetarium.GetUniversalTime();
            Orbit vesselOrbit = vessel.GetOrbit();
            Vector3d serializedOrbitalVelocity = vesselOrbit.getOrbitalVelocityAtUT(universalTime);
            Vector3d orbitalVector = serializedOrbitalVelocity.xzy;

            // update solar flux
            vesselData.ModulePhotonSail.UpdateSolarFlux(universalTime, positionVessel, vessel);
            // update mass
            vesselData.UpdateMass(vessel);

            var totalPhotonAccelerationVector = CalculatePhotonForceVector(vesselData, vesselTransform, positionVessel);

            var combinedDragDecelerationVector = CalculateDragVector(vessel, vesselData, orbitalVector, vesselOrbit);

            // apply force
            Perturb(vesselOrbit, (totalPhotonAccelerationVector + combinedDragDecelerationVector) * TimeWarp.fixedDeltaTime, universalTime, serializedOrbitalVelocity);

            return true;
        }

        private static Vector3d CalculatePhotonForceVector(VesselData vesselData, Transform vesselTransform, Vector3d positionVessel)
        {
            Vector3d totalPhotonAccelerationVector = Vector3d.zero;

            foreach (var starLight in KopernicusHelper.Stars)
            {
                Vector3d vesselNormal = vesselTransform.up.normalized;

                // Magnitude of force proportional to cosine-squared of angle between sun-line and normal
                var cosConeAngle = Vector3d.Dot((positionVessel - starLight.position).normalized, vesselNormal);

                // If normal points away from sun, negate so our force is always away from the sun
                // so that turning the backside towards the sun thrusts correctly
                if (cosConeAngle < 0)
                {
                    vesselNormal = -vesselNormal;
                    cosConeAngle = -cosConeAngle;
                }

                // calculate effective radiation pressure on solarSail
                var energyOnSailInWatt = starLight.solarFlux * vesselData.ModulePhotonSail.surfaceArea;
                var reflectedRadiationPressureOnSail = 2 * energyOnSailInWatt / GameConstants.speedOfLight * cosConeAngle;
                var reflectedPhotonForceVector = vesselNormal * reflectedRadiationPressureOnSail * cosConeAngle;

                // calculate acceleration
                var accelerationVector = vesselData.TotalVesselMassInKg > 0
                    ? reflectedPhotonForceVector / vesselData.TotalVesselMassInKg
                    : Vector3d.zero;

                totalPhotonAccelerationVector += accelerationVector;
            }

            return totalPhotonAccelerationVector;
        }

        private static Vector3d CalculateDragVector(Vessel vessel, VesselData vesselData, Vector3d orbitalVector, Orbit vesselOrbit)
        {
            var normalizedOrbitalVector = orbitalVector.normalized;
            var cosOrbitRaw = Vector3d.Dot(vessel.transform.up, normalizedOrbitalVector);
            var cosOrbitalDrag = Math.Abs(cosOrbitRaw);
            var squaredCosOrbitalDrag = cosOrbitalDrag * cosOrbitalDrag;
            var atmosphericGasKgPerSquareMeter = AtmosphericFloatCurves.GetAtmosphericGasDensityKgPerCubicMeter(vesselOrbit.referenceBody, vessel.altitude);

            var effectiveSpeedForDrag = Math.Max(0, orbitalVector.magnitude);
            var dragForcePerSquareMeter = atmosphericGasKgPerSquareMeter * 0.5 * effectiveSpeedForDrag * effectiveSpeedForDrag;
            var effectiveSurfaceArea = cosOrbitalDrag * vesselData.ModulePhotonSail.surfaceArea;

            // calculate normal
            Vector3d vesselNormal = vessel.transform.up;
            if (cosOrbitRaw < 0)
                vesselNormal = -vesselNormal;

            // calculate specular drag
            var specularDragCoefficient = squaredCosOrbitalDrag + 3 * squaredCosOrbitalDrag;
            var specularDragPerSquareMeter = specularDragCoefficient * dragForcePerSquareMeter * 0.1;
            var specularDragInNewton = specularDragPerSquareMeter * effectiveSurfaceArea;
            Vector3d specularDragForce = specularDragInNewton * vesselNormal;

            // calculate Diffuse Drag
            var diffuseDragCoefficient = 2 + squaredCosOrbitalDrag * 1.3;
            var diffuseDragPerSquareMeter = diffuseDragCoefficient * dragForcePerSquareMeter * 0.9;
            var diffuseDragInNewton = diffuseDragPerSquareMeter * effectiveSurfaceArea;
            Vector3d diffuseDragForceVector = diffuseDragInNewton * normalizedOrbitalVector * -1;

            Vector3d combinedDragDecelerationVector = vesselData.TotalVesselMassInKg > 0 
                ? (specularDragForce + diffuseDragForceVector) / vesselData.TotalVesselMassInKg 
                : Vector3d.zero;

            return combinedDragDecelerationVector;
        }

        public static void Perturb(Orbit orbit, Vector3d deltaVV, double universalTime, Vector3d serializedOrbitalVelocity)
        {
            if (deltaVV.magnitude == 0)
                return;

            // Update with current position and new velocity
            orbit.UpdateFromStateVectors(orbit.getRelativePositionAtUT(universalTime), serializedOrbitalVelocity + deltaVV.xzy, orbit.referenceBody, universalTime);
            orbit.Init();
            orbit.UpdateFromUT(universalTime);
        }
    }
}
