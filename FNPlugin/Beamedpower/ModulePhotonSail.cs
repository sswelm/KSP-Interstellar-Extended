using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;
using FNPlugin.Resources;

namespace FNPlugin.Beamedpower
{
    class BeamEffect
    {
        public GameObject solar_effect;
        public Renderer solar_effect_renderer;
        public Collider solar_effect_collider;
    }

    class ReceivedBeamedPower
    {
        public double receivedPower;
        public double pitchAngle;
    }

    class ModulePhotonSail : PartModule, IBeamedPowerReceiver
    {
        // Persistent Variables
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true)]
        public double previousPeA;
        [KSPField(isPersistant = true)]
        public double previousAeA;
        [KSPField(isPersistant = true)]
        public float previousFixedDeltaTime;

        // Persistent False
        [KSPField]
        public double reflectedPhotonRatio = 0.975;
        [KSPField(guiActiveEditor = true, guiName = "Photovoltaic film Area", guiUnits = " m\xB2")]
        public double photovoltaicArea = 1;
        [KSPField(guiActiveEditor = true, guiName = "Surface Area", guiUnits = " m\xB2")]
        public double surfaceArea = 144400;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "Diameter", guiUnits = " m", guiFormat = "F3")]
        public double diameter;
        [KSPField(guiActiveEditor = true, guiName = "Mass", guiUnits = " t", guiFormat = "F3")]
        public double partMass;
        

        [KSPField]
        public float effectSize1 = 1.25f;
        [KSPField]
        public string animName = "";
        [KSPField]
        public float initialAnimationSpeed = 1;
        [KSPField]
        public float initialAnimationNormalizedTime = 0.5f;
        [KSPField]
        public float initialAnimationTargetWeight = 0.01f;

        // GUI
        [KSPField(guiActive = false, guiName = "Solar Flux Measured", guiFormat = "F4", guiUnits = " W/m\xB2")]
        public double solarFluxMeasured;
        [KSPField(guiActive = true, guiName = "Solar Flux", guiFormat = "F4", guiUnits = " W/m\xB2")]
        public double solarFluxInWatt;
        [KSPField(guiActive = true, guiName = "Solar Force Max", guiFormat = "F4", guiUnits = " N")]
        public double totalForceInNewtonFromSolarEnergy = 0;
        [KSPField(guiActive = true, guiName = "Solar Force Sail", guiFormat = "F4", guiUnits = " N")]
        public double solar_force_d = 0;
        [KSPField(guiActive = true, guiName = "Solar Acceleration")]
        public string solarAcc;
        [KSPField(guiActive = true, guiName = "Solar Pitch Angle", guiFormat = "F3", guiUnits = "°")]
        public double solarSailAngle = 0;
        [KSPField(guiActive = false, guiName = "Network power", guiFormat = "F4", guiUnits = " MW")]
        public double maxNetworkPower;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Beamed Photon Throttle", guiUnits = "%"), UI_FloatRange(stepIncrement = 1, maxValue = 100, minValue = 0)]
        public float beamedPowerThrottle = 0;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Beamed Push Direction"), UI_Toggle(disabledText = "Backward", enabledText = "Forward")]
        public bool beamedPowerForwardDirection = true;
        [KSPField(guiActive = true, guiName = "Beamed Power Energy", guiFormat = "F4", guiUnits = " MW")]
        public double availableBeamedPhotonPower;

        [KSPField(guiActive = true, guiName = "Beamed Potential Force", guiFormat = "F4", guiUnits = " N")]
        public double totalForceInNewtonFromBeamedPower = 0;
        [KSPField(guiActive = true, guiName = "Beamed Pitch Angle", guiFormat = "F3", guiUnits = "°")]
        public double weightedBeamPowerPitch;
        [KSPField(guiActive = true, guiName = "Beamed Spotsize", guiFormat = "F3", guiUnits = " m")]
        public double averageSpotsize;
        [KSPField(guiActive = true, guiName = "Beamed Sail Force", guiFormat = "F4", guiUnits = " N")]
        public double beamed_force_d = 0;
        [KSPField(guiActive = true, guiName = "Beamed Acceleration")]
        public string beamedAcc;

        [KSPField(guiActive = false, guiName = "Atmospheric Density", guiUnits = " kg/m2")]
        public double atmosphericGasKgPerSquareMeter;
        [KSPField(guiActive = true, guiName = "Maximum Drag", guiUnits = " N/m2")]
        public float maximumDragPerSquareMeter;
        [KSPField(guiActive = true, guiName = "Diffuse Drag", guiUnits = " N")]
        public float diffuseSailDragInNewton;
        [KSPField(guiActive = true, guiName = "Specular Drag", guiUnits = " N")]
        public float specularSailDragInNewton;

        [KSPField(guiActive = true, guiName = "Abs Periapsis Change", guiFormat = "F3", guiUnits = " m/s")]
        public double periapsisChange;
        [KSPField(guiActive = true, guiName = "Abs Apapsis Change", guiFormat = "F3", guiUnits = " m/s")]
        public double apapsisChange;
        [KSPField(guiActive = true, guiName = "Orbit Diameter Change", guiFormat = "F3", guiUnits = " m/s")]
        public double orbitSizeChange;
        [KSPField(guiActive = false, guiName = "Low Orbit Modifier")]
        public double lowOrbitModifier;

        // conversion from rad to degree
        const double radToDegreeMult = 180d / Math.PI;

        // Display numbers for force and acceleration
        bool scaleBeamToAnimation = false;
        double solar_acc_d = 0;
        double beamed_acc_d = 0;
        float animationNormalizedTime;

        Animation solarSailAnim = null;
        CelestialBody _localStar;

        List<ReceivedBeamedPower> receivedBeamedPowerList = new List<ReceivedBeamedPower>();
        IDictionary<VesselMicrowavePersistence, KeyValuePair<MicrowaveRoute, IList<VesselRelayPersistence>>> _transmitDataCollection;
        Dictionary<Vessel, ReceivedPowerData> received_power = new Dictionary<Vessel, ReceivedPowerData>();

        Queue<double> periapsisChangeQueue = new Queue<double>(20);
        Queue<double> apapsisChangeQueue = new Queue<double>(20);

        private static Dictionary<string, float> starLuminosity = new Dictionary<string, float>();

        BeamEffect []beamEffectArray;

        public int ReceiverType { get { return 7; } }                       // receiver from either top or bottom

        public double Diameter { get { return diameter; } }

        public double ApertureMultiplier { get { return 1; } }

        public double MinimumWavelength { get { return 0.000000620; } }     // receive optimally from red visible light

        public double MaximumWavelength { get { return 0.001; } }           // receive up to maximum infrared

        public double HighSpeedAtmosphereFactor { get { return 1; } }

        public double FacingThreshold { get { return 0; } }

        public double FacingSurfaceExponent { get { return 1; } }

        public double FacingEfficiencyExponent { get { return 0; } }    // can receive beamed power from any angle

        public double SpotsizeNormalizationExponent { get { return 1; } }

        public bool CanBeActiveInAtmosphere { get { return false; } }

        public Vessel Vessel { get { return vessel; } }

        public Part Part { get { return part; } }

        // GUI to deploy sail
        [KSPEvent(guiActiveEditor = true,  guiActive = true, guiName = "Deploy Sail", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]
        public void DeploySail()
        {
            if (string.IsNullOrEmpty(animName) || solarSailAnim == null)
                return;

            scaleBeamToAnimation = true;
            solarSailAnim[animName].speed = 0.5f;
            solarSailAnim[animName].normalizedTime = 0f;
            solarSailAnim.Blend(animName, 0.1f);

            IsEnabled = true;
        }

        // GUI to retract sail
        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Retract Sail", active = false, guiActiveUncommand = true, guiActiveUnfocused = true)]
        public void RetractSail()
        {
            if (animName == null || solarSailAnim == null)
                return;

            scaleBeamToAnimation = true;
            solarSailAnim[animName].speed = -0.5f;
            solarSailAnim[animName].normalizedTime = 1f;
            solarSailAnim.Blend(animName, 0.1f);

            IsEnabled = false;
        }

        // Initialization
        public override void OnStart(PartModule.StartState state)
        {
            diameter = Math.Sqrt(surfaceArea);

            if (animName != null)
                solarSailAnim = part.FindModelAnimators(animName).FirstOrDefault();

            // start with deployed sail  when enabled
            if (IsEnabled)
            {
                solarSailAnim[animName].speed = initialAnimationSpeed;
                solarSailAnim[animName].normalizedTime = initialAnimationNormalizedTime;
                solarSailAnim.Blend(animName, initialAnimationTargetWeight);
            }

            if (state == StartState.None || state == StartState.Editor)
                return;

            this.part.force_activate();

            CreateBeamArray();

            CompileStarData();
        }

        // Scan the Kopernicus config nodes and extract luminosity values
        protected void CompileStarData()
        {
            // Only need to do this once 
            if (starLuminosity.Count > 0)
                return;

            ConfigNode[] nodeLevel1 = GameDatabase.Instance.GetConfigNodes("Kopernicus");

            if (nodeLevel1.Length > 0)
                Debug.Log("[KSPI] - Loading Kopernicus Configuration Data");
            else
                Debug.LogWarning("[KSPI] - Failed to find Kopernicus Configuration Data");

            for (int i = 0; i < nodeLevel1.Length; i++)
            {
                ConfigNode[] nodeLevel2 = nodeLevel1[i].GetNodes("Body");
                for (int j = 0; j < nodeLevel2.Length; j++)
                {
                    string bodyName = nodeLevel2[j].GetValue("name");
                    ConfigNode[] nodeLevel3 = nodeLevel2[j].GetNodes("Properties");
                    for (int k = 0; k < nodeLevel3.Length; k++)
                    {
                        string stLum = nodeLevel3[k].GetValue("starLuminosity");
                        if (stLum != null)
                        {
                            float Lum;
                            float.TryParse(stLum, out Lum);
                            starLuminosity.Add(bodyName, Lum);
                        }
                    }
                }
            }
        }

        private void CreateBeamArray()
        {
            beamEffectArray = new BeamEffect[10];

            for (var i = 0; i < beamEffectArray.Length; i++)
            {
                beamEffectArray[i] = CreateBeam(1001 + i);
            }
        }

        private BeamEffect CreateBeam(int renderQueue)
        {
            var beam = new BeamEffect();

            beam.solar_effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.solar_effect.transform.localScale = Vector3.zero;
            beam.solar_effect.transform.position = Vector3.zero;
            beam.solar_effect.transform.rotation = part.transform.rotation;
            
            beam.solar_effect_collider = beam.solar_effect.GetComponent<Collider>();
            beam.solar_effect_collider.enabled = false;

            beam.solar_effect_renderer = beam.solar_effect.GetComponent<Renderer>();
            beam.solar_effect_renderer.material.shader = Shader.Find("Unlit/Transparent");
            beam.solar_effect_renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.1f);
            beam.solar_effect_renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/infrared", false);
            beam.solar_effect_renderer.material.renderQueue = renderQueue;
            beam.solar_effect_renderer.receiveShadows = false;            

            return beam;
        }

        public void Update()
        {
            // Sail deployment GUI
            Events["DeploySail"].active = !IsEnabled;
            Events["RetractSail"].active = IsEnabled;

            if (HighLogic.LoadedSceneIsFlight)
                return;

            diameter = Math.Sqrt(surfaceArea);
            partMass = part.mass;
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            maxNetworkPower = 0;
            availableBeamedPhotonPower = 0;
            animationNormalizedTime = solarSailAnim[animName].normalizedTime;
            
            // update local star
            _localStar = PluginHelper.GetCurrentStar();

            // update available beamed power transmitters
            _transmitDataCollection = BeamedPowerHelper.GetConnectedTransmitters(this);

            foreach (var transmitData in _transmitDataCollection)
            {
                VesselMicrowavePersistence transmitter = transmitData.Key;
                KeyValuePair<MicrowaveRoute, IList<VesselRelayPersistence>> routeRelayData = transmitData.Value;
                //MicrowaveRoute route = routeRelayData.Key;

                ReceivedPowerData beamedPowerData;
                if (!received_power.TryGetValue(transmitter.Vessel, out beamedPowerData))
                {
                    beamedPowerData = new ReceivedPowerData
                    {
                        Receiver = this,
                        Transmitter = transmitter
                    };
                    received_power[beamedPowerData.Transmitter.Vessel] = beamedPowerData;
                }

                beamedPowerData.NetworkPower = 0;
                beamedPowerData.AvailablePower = 0;
                beamedPowerData.Route = routeRelayData.Key;
                beamedPowerData.Distance = beamedPowerData.Route.Distance;
                

                foreach(var wavelengthData in transmitter.SupportedTransmitWavelengths)
                {
                    var transmittedPower = (wavelengthData.nuclearPower + wavelengthData.solarPower) / 1000d;

                    maxNetworkPower += transmittedPower;

                    beamedPowerData.NetworkPower += transmittedPower;

                    var currentWavelengthBeamedPower = transmittedPower * beamedPowerData.Route.Efficiency;

                    availableBeamedPhotonPower += currentWavelengthBeamedPower;

                    beamedPowerData.AvailablePower += currentWavelengthBeamedPower; 
                }
            }

            // Text fields (acc & force)
            Fields["solarAcc"].guiActive = IsEnabled;
            Fields["beamedAcc"].guiActive = IsEnabled;

            solarAcc = solar_acc_d.ToString("E") + " m/s";
            beamedAcc = beamed_acc_d.ToString("E") + " m/s"; ;
        }

        public override void OnFixedUpdate()
        {
            totalForceInNewtonFromSolarEnergy = 0;
            totalForceInNewtonFromBeamedPower = 0;
            solar_force_d = 0;
            solar_acc_d = 0;
            beamed_force_d = 0;
            beamed_acc_d = 0;

            if (FlightGlobals.fetch == null || _localStar == null || part == null || vessel == null)
                return;

            receivedBeamedPowerList.Clear();

            TimeWarp.GThreshold = 2;

            UpdateChangeGui();

            for (var i = 0; i < beamEffectArray.Length; i++)
            {
                UpdateBeams(beamEffectArray[i], Vector3d.zero, 0, 0);
            }

            var vesselMassInKg = vessel.totalMass * 1000;
            var universalTime = Planetarium.GetUniversalTime();
            Vector3d positionVessel = vessel.orbit.getPositionAtUT(universalTime);

			receivedBeamedPowerList.Clear();

            int beamcounter = 0;
            foreach (var receivedPowerData in received_power.Values.Where(m => m.AvailablePower > 1))
            {
                averageSpotsize = receivedPowerData.Route.Spotsize;

                Vector3d beamedPowerSource = receivedPowerData.Transmitter.Vessel.GetWorldPos3D();
                GenerateForce(beamcounter++, beamedPowerSource, positionVessel, receivedPowerData.AvailablePower * 1e6, universalTime, false, vesselMassInKg, Math.Max(effectSize1, receivedPowerData.Route.Spotsize / 4));
            }

            if (receivedBeamedPowerList.Count > 0)
            {
                var totalBeamedPower = receivedBeamedPowerList.Sum(m => m.receivedPower);
                var totalPitch = receivedBeamedPowerList.Sum(m => m.pitchAngle * m.receivedPower / totalBeamedPower);
                weightedBeamPowerPitch = totalPitch / receivedBeamedPowerList.Count;
            }

            // update solar flux
            UpdateSolarFluxLocalStar(universalTime);

            Vector3d localStarPosition =  _localStar.getPositionAtUT(universalTime);
            GenerateForce(0, localStarPosition, positionVessel, solarFluxInWatt * surfaceArea, universalTime, true, vesselMassInKg, 1);

            // calculate drag
            ApplyDrag(universalTime, vesselMassInKg);
        }

        private void UpdateSolarFluxLocalStar(double universalTime)
        {
            solarFluxMeasured = vessel.solarFlux;

            var vesselIsSun = inSun(vessel.orbit, _localStar, universalTime);

            solarFluxInWatt = vesselIsSun ? solarFluxAtDistance(part.vessel, _localStar, GetLuminosity()) : 0;
        }

        private float GetLuminosity()
        {
            float lumonisity;
            if (!starLuminosity.TryGetValue(_localStar.name, out lumonisity))
                lumonisity = 1;
            return lumonisity;
        }

        private void ApplyDrag(double universalTime, double vesselMassInKg)
        {
            var simulatedAltitude = Math.Max(vessel.mainBody.atmosphereDepth,  vessel.altitude);
            var atmosphereDensity = AtmosphericFloatCurves.GetAtmosphericGasDensityKgPerCubicMeter(vessel.mainBody, simulatedAltitude);

            atmosphericGasKgPerSquareMeter = part.vessel.atmDensity > 0
                ? (1 - (part.vessel.atmDensity / vessel.mainBody.atmospherePressureSeaLevel)) * atmosphereDensity 
                : atmosphereDensity;

            var effectiveSurfaceArea = surfaceArea * (IsEnabled ? 1 : 0);
            var specularRatio = Math.Max(0, Math.Min(1, part.skinTemperature / part.skinMaxTemp));
            var diffuseRatio = 1 - specularRatio;
            var maximumDragCcoefficient = (4 * specularRatio) + (3.3 * diffuseRatio);
            var cosOrbitRaw = Vector3d.Dot(this.part.transform.up, part.vessel.obt_velocity.normalized);
            var cosObitalDrag = Math.Abs(cosOrbitRaw);
            var squaredCosOrbitalDrag = cosObitalDrag * cosObitalDrag;
            var baseDragForce = 0.5 * atmosphericGasKgPerSquareMeter * vessel.obt_speed * vessel.obt_speed;
            lowOrbitModifier = Math.Max(0, Math.Min(1, (1000 + simulatedAltitude - vessel.mainBody.atmosphereDepth) / vessel.mainBody.atmosphereDepth));			
            maximumDragPerSquareMeter = (float)(lowOrbitModifier * baseDragForce * maximumDragCcoefficient);
            
            // apply specular Drag
            Vector3d partNormal = this.part.transform.up;
            if (cosOrbitRaw < 0)
                partNormal = -partNormal;

            var specularDragCoefficient = lowOrbitModifier * 4 * squaredCosOrbitalDrag;
            var specularDragPerSquareMeter =  specularDragCoefficient * baseDragForce * specularRatio;
            var specularDragInNewton = specularDragPerSquareMeter * effectiveSurfaceArea * baseDragForce * specularRatio;
            specularSailDragInNewton = (float)specularDragInNewton;
            var specularDragForceFixed = specularDragInNewton * TimeWarp.fixedDeltaTime * partNormal;
            var specularDragDeceleration = -specularDragForceFixed / vesselMassInKg;

            ChangeVesselVelocity(this.vessel, universalTime, specularDragDeceleration);

            // apply Diffuse Drag
            var diffuseDragCoefficient = lowOrbitModifier * (2 + squaredCosOrbitalDrag * 1.3);
            var diffuseDragPerSquareMeter = diffuseDragCoefficient * baseDragForce * diffuseRatio;
            var diffuseDragInNewton = diffuseDragPerSquareMeter * cosObitalDrag * effectiveSurfaceArea;
            diffuseSailDragInNewton = (float)diffuseDragInNewton;
            var diffuseDragForceFixed = diffuseDragInNewton * TimeWarp.fixedDeltaTime * part.vessel.obt_velocity.normalized;
            var diffuseDragDeceleration = -diffuseDragForceFixed / vesselMassInKg;

            ChangeVesselVelocity(this.vessel, universalTime, diffuseDragDeceleration);
        }

        private static void ChangeVesselVelocity(Vessel vessel, double universalTime, Vector3d acceleration)
        {
            if (double.IsNaN(acceleration.x) || double.IsNaN(acceleration.y) || double.IsNaN(acceleration.z))
                return;

            if (vessel.packed)
                vessel.orbit.Perturb(acceleration, universalTime);
            else
                vessel.ChangeWorldVelocity(acceleration);
        }

        private void GenerateForce(int index, Vector3d positionPowerSource, Vector3d positionVessel, double availableEnergyInWatt, double universalTime, bool isSun, double vesselMassInKg, double beamspotsize)
        {
            // calculate vector between vessel and star/transmitter
            Vector3d powerSourceToVesselVector = positionVessel - positionPowerSource;

            // take part vector 
            Vector3d partNormal = this.part.transform.up;

            // Magnitude of force proportional to cosine-squared of angle between sun-line and normal
            var cosConeAngle = Vector3d.Dot(powerSourceToVesselVector.normalized, partNormal);

            var cosConeAngleIsNegative = cosConeAngle < 0;

            // If normal points away from sun, negate so our force is always away from the sun
            // so that turning the backside towards the sun thrusts correctly
            if (cosConeAngleIsNegative)
            {
                // recalculate Magnitude of force proportional to cosine-squared of angle between sun-line and normal
                partNormal = -partNormal;
                cosConeAngle = Vector3d.Dot(powerSourceToVesselVector.normalized, partNormal);
            }

            // convert cosine angle  into angle in radian
            var pitchAngleInRad = Math.Acos(cosConeAngle);

            // convert radian into angle in degree
            var pitchAngleInDegree = pitchAngleInRad * radToDegreeMult;

            // skip beamed power in undesireable direction
            if (isSun)
            {
                solarSailAngle = pitchAngleInDegree;

                part.RequestResource("ElectricCharge", -photovoltaicArea * solarFluxInWatt * 0.2 * cosConeAngle * TimeWarp.fixedDeltaTime);  
            }
            else if ((beamedPowerForwardDirection && cosConeAngleIsNegative) || (!beamedPowerForwardDirection && !cosConeAngleIsNegative))
                return;

            // convert energy into momentum
            var radiationPresure = (isSun ? 2 : beamedPowerThrottle / 50) * availableEnergyInWatt / GameConstants.speedOfLight;

            // calculate solar light force at current location
            var maximumPhotonForceInNewton = reflectedPhotonRatio * radiationPresure;

            // calculate effective radiation presure on solarsail
            var radiationPresureOnSail = isSun ? radiationPresure * cosConeAngle : radiationPresure;

            // register force 
            if (isSun)
                totalForceInNewtonFromSolarEnergy += maximumPhotonForceInNewton * sign(cosConeAngleIsNegative);
            else
                totalForceInNewtonFromBeamedPower += maximumPhotonForceInNewton * sign(cosConeAngleIsNegative);

            if (!IsEnabled)
                return;

            // draw beamed power rays
            if (!isSun && index < 10)
            {
                var scaleModifier = beamedPowerThrottle > 0 ? (scaleBeamToAnimation || beamspotsize * 4 <= diameter ? 1 : 4) : 0;
                var animationModifier = animationNormalizedTime > 0 ? (animationNormalizedTime > 0.6 ? (animationNormalizedTime - 0.6) * 2.5 : 0) : 1;
                var beamSpotsize = beamedPowerThrottle > 0 ? Mathf.Max((float)(animationModifier * cosConeAngle * diameter / 4), (float)beamspotsize) : 0;
                UpdateBeams(beamEffectArray[index], powerSourceToVesselVector, scaleModifier, beamSpotsize);
            }

            // calculate the vector at 90 degree angle in the direction of the vector
            var tangantVector = (powerSourceToVesselVector - (Vector3.Dot(powerSourceToVesselVector, partNormal)) * partNormal).normalized;

            // old : F = 2 PA cos α cos α n
            // new F = P A cos α [(1 + ρ ) cos α n − (1 − ρ ) sin α t] 
            // where P: solar radiation pressure, A: sail area, α: sail pitch angle, t: sail tangential vector, ρ: reflection coefficien
            var effectiveForce = radiationPresureOnSail * ((1 + reflectedPhotonRatio) * cosConeAngle * partNormal - (1 - reflectedPhotonRatio) * Math.Sin(pitchAngleInRad) * tangantVector);

            // Calculate acceleration from sunlight
            Vector3d photonAccel = effectiveForce / vesselMassInKg;

            // Acceleration from sunlight per second
            Vector3d fixedPhotonAccel = photonAccel * TimeWarp.fixedDeltaTime;

            // Apply acceleration when valid
            ChangeVesselVelocity(this.vessel, universalTime, fixedPhotonAccel);

            // Update displayed force & acceleration
            if (isSun)
            {
                solar_force_d += effectiveForce.magnitude * sign(cosConeAngleIsNegative);
                solar_acc_d += photonAccel.magnitude * sign(cosConeAngleIsNegative);
            }
            else
            {
                receivedBeamedPowerList.Add(new ReceivedBeamedPower { pitchAngle = pitchAngleInDegree, receivedPower = availableEnergyInWatt });

                beamed_force_d += effectiveForce.magnitude * sign(cosConeAngleIsNegative);
                beamed_acc_d += photonAccel.magnitude * sign(cosConeAngleIsNegative);
            }
        }

        private static int sign(bool cosConeAngleIsNegative)
        {
            return (cosConeAngleIsNegative ? -1 : 1);
        }

        private void UpdateBeams(BeamEffect beameffect,  Vector3d powerSourceToVesselVector, float scaleModifer = 1, float beamSize = 1)
        {
            var endBeamPos = part.transform.position + (powerSourceToVesselVector.normalized * 10000);
            var midPos = part.transform.position - endBeamPos;
            var timeCorrection = TimeWarp.CurrentRate > 1 ? -vessel.obt_velocity * TimeWarp.fixedDeltaTime : Vector3d.zero;

            var solarVectorX = powerSourceToVesselVector.normalized.x * 90;
            var solarVectorY = powerSourceToVesselVector.normalized.y * 90 - 90;
            var solarVectorZ = powerSourceToVesselVector.normalized.z * 90;

            beameffect.solar_effect.transform.localRotation = new Quaternion((float)solarVectorX, (float)solarVectorY, (float)solarVectorZ, 0);
            beameffect.solar_effect.transform.localScale = new Vector3(beamSize, 10000 * scaleModifer, beamSize);
            beameffect.solar_effect.transform.position = new Vector3((float)(part.transform.position.x + midPos.x + timeCorrection.x), (float)(part.transform.position.y + midPos.y + timeCorrection.y), (float)(part.transform.position.z + midPos.z + timeCorrection.z));
        }

        private void UpdateChangeGui()
        {
            var averageFixedDeltaTime = (previousFixedDeltaTime + TimeWarp.fixedDeltaTime) / 2f;

            periapsisChangeQueue.Enqueue((vessel.orbit.PeA - previousPeA) / averageFixedDeltaTime);
            if (periapsisChangeQueue.Count > 20)
                periapsisChangeQueue.Dequeue();
            periapsisChange = periapsisChangeQueue.Count > 10 
                ?  periapsisChangeQueue.OrderBy(m => m).Skip(5).Take(10).Average() 
                : periapsisChangeQueue.Average();

            apapsisChangeQueue.Enqueue((vessel.orbit.ApA - previousAeA) / averageFixedDeltaTime);
            if (apapsisChangeQueue.Count > 20)
                apapsisChangeQueue.Dequeue();
            apapsisChange = apapsisChangeQueue.Count > 10
                ? apapsisChangeQueue.OrderBy(m => m).Skip(5).Take(10).Average()
                : apapsisChangeQueue.Average();

            orbitSizeChange = periapsisChange + apapsisChange;

            previousPeA = vessel.orbit.PeA;
            previousAeA = vessel.orbit.ApA;
            previousFixedDeltaTime = TimeWarp.fixedDeltaTime;
        }

        private static double solarFluxAtDistance(Vessel vessel, CelestialBody star, double luminosity)
        {
            const double kerbin_distance = 13599840256;

            var toStar = vessel.CoMD - star.position;
            var distance = toStar.magnitude - star.Radius;
            var distAU = distance / kerbin_distance;
            return luminosity * 1360 / (distAU * distAU);
        }

        // Test if an orbit at UT is in sunlight
        public bool inSun(Orbit orbit, CelestialBody sun,  double UT)
        {
            Vector3d vesselPosition = orbit.getPositionAtUT(UT);
            Vector3d sunPosition = sun.getPositionAtUT(UT);
            Vector3d bminusa = sunPosition - vesselPosition;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                // the sun should not block line of sight to the sun
                if (referenceBody.name == sun.name)
                    continue;

                Vector3d refminusa = referenceBody.getPositionAtUT(UT) - vesselPosition;

                if (Vector3d.Dot(refminusa, bminusa) <= 0)
                    continue;

                var normalizedBminusa = bminusa.normalized;

                var cosReferenceSunNormB = Vector3d.Dot(refminusa, normalizedBminusa);

                if (cosReferenceSunNormB >= bminusa.magnitude)
                    continue;

                Vector3d tang = refminusa - cosReferenceSunNormB * normalizedBminusa;
                if (tang.magnitude < referenceBody.Radius)
                    return false;
            }
            return true;
        }


    }
}
