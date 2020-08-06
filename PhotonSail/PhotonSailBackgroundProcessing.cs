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
        public Vessel Vessel { get; set; }

        public uint SolarSailPersistentId { get; set; }

        public double TotalVesselMassInKg { get;  set; }
        public double TotalVesselMass { get;  set; }
        public bool? HasOpenSolarSail { get; set; }

        public ModulePhotonSail ModulePhotonSail { get; set; }
        public ProtoPartModuleSnapshot ProtoPartModuleSnapshot { get; set; }

        public VesselData(Vessel vessel)
        {
            Vessel = vessel;
        }

        public void UpdateMass(Vessel vessel)
        {
            TotalVesselMass = 0;

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
    public sealed class PhotonSailBackgroundProcessing : ScenarioModule
    {
        private static readonly Dictionary<Guid, VesselData> vesselDataDict = new Dictionary<Guid, VesselData>();

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            vesselDataDict.Clear();
        }

        /// <summary>
        /// Called by the part every refresh frame where it is active, which can be less frequent than FixedUpdate which is called every processing frame
        /// </summary>
        void FixedUpdate()
        {
            // for each vessel
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                // ignore Kerbals
                if (vessel.isEVA)
                    continue;

                // ignore landed or floating vessels
                if (vessel.LandedOrSplashed)
                    continue;

                // ignore irrelevant vessel types
                if (vessel.vesselType == VesselType.Debris
                    || vessel.vesselType == VesselType.Flag
                    || vessel.vesselType == VesselType.SpaceObject
                    || vessel.vesselType == VesselType.DeployedSciencePart
                    || vessel.vesselType == VesselType.DeployedScienceController
                )
                    continue;

                // ignore irrelevant vessel situations
                if ((vessel.situation & Vessel.Situations.PRELAUNCH) == Vessel.Situations.PRELAUNCH
                    || (vessel.situation & Vessel.Situations.FLYING) == Vessel.Situations.FLYING)
                    continue;

                // lookup vesselData
                if (!vesselDataDict.TryGetValue(vessel.id, out VesselData vesselData))
                {
                    vesselData = new VesselData(vessel);
                    vesselDataDict.Add(vessel.id, vesselData);
                }

                if (vessel.loaded)
                {
                    ProcessesLoadedVessel(vessel, vesselData);
                    continue;
                }

                // skip further processing if no open solar sail present
                if (vesselData.HasOpenSolarSail.HasValue && vesselData.HasOpenSolarSail.Value == false)
                    continue;

                // process solar sail if we already found a open solar sail
                if (vesselData.ProtoPartModuleSnapshot != null)
                {
                    ProtoPartSnapshot protoPartSnapshot = vessel.protoVessel.protoPartSnapshots
                        .FirstOrDefault(m => m.persistentId == vesselData.SolarSailPersistentId);

                    if (protoPartSnapshot != null)
                        BackgroundSolarSail(protoPartSnapshot, vessel, vesselData);
                    else
                    {
                        vesselData.HasOpenSolarSail = null;
                        vesselData.ProtoPartModuleSnapshot = null;
                        Debug.Log("[PhotonSailor]: Fail to find protoPartSnapshots " + vesselData.SolarSailPersistentId);
                    }
                    continue;
                }

                // look for a solar sail
                foreach (ProtoPartSnapshot protoPartSnapshot in vessel.protoVessel.protoPartSnapshots)
                {
                    BackgroundSolarSail(protoPartSnapshot, vessel, vesselData);

                    // break search if we found a open solar sail
                    if (vesselData.HasOpenSolarSail.HasValue && vesselData.HasOpenSolarSail.Value)
                        break;
                }
            }
        }

        private static void ProcessesLoadedVessel(Vessel vessel, VesselData vesselData)
        {
            vesselData.ModulePhotonSail = vessel.FindPartModuleImplementing<ModulePhotonSail>();

            vesselData.HasOpenSolarSail = vesselData.ModulePhotonSail?.IsEnabled;
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

                vesselData.SolarSailPersistentId = protoPartSnapshot.persistentId;

                // load modulePhotonSail IsEnabled
                if (!vesselData.ProtoPartModuleSnapshot.moduleValues.TryGetValue(nameof(vesselData.ModulePhotonSail.IsEnabled), ref vesselData.ModulePhotonSail.IsEnabled))
                    return false;

                if (vesselData.ModulePhotonSail.IsEnabled)
                    vesselData.HasOpenSolarSail = true;
                else
                    return false;
            }

            Transform vesselTransform = vessel.GetTransform();
            Vector3d positionVessel = vessel.GetWorldPos3D();
            double universalTime = Planetarium.GetUniversalTime();
            Orbit vesselOrbit = vessel.GetOrbit();
            Vector3d serializedOrbitalVelocity = vesselOrbit.getOrbitalVelocityAtUT(universalTime);
            Vector3d orbitalVector = serializedOrbitalVelocity.xzy;

            // update solar flux
            vesselData.ModulePhotonSail.UpdateSolarFlux(universalTime, positionVessel, vessel);
            // update vessel mass
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
