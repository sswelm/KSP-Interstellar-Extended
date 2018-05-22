using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin.Beamedpower
{
    class ModuleSolarSail : PartModule
    {
        // Persistent Variables
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true)]
        protected double previousPeA;
        [KSPField(isPersistant = true)]
        protected double previousAeA;

        // Persistent False
        [KSPField]
        public double reflectedPhotonRatio = 1f;
        [KSPField(guiActiveEditor = true, guiUnits = " m\xB2")]
        public double surfaceArea = 144400;
        [KSPField]
        public string animName;

        // GUI
        // Force and Acceleration
        [KSPField(guiActive = true, guiName = "Solar Force")]
        public string solarForceAtDistance;
        [KSPField(guiActive = true, guiName = "Sail Force")]
        public string forceAcquired = "";
        [KSPField(guiActive = true, guiName = "Acceleration")]
        public string solarAcc = "";
        [KSPField(isPersistant = true, guiActive = true, guiName = "Sailed Delta V", guiFormat = "F3", guiUnits = " m/s")]
        public double sailedDeltaV;

        [KSPField(guiActive = true, guiName = "Cosine Angle", guiFormat = "F6")]
        public double cosConeAngle;

        [KSPField(guiActive = true, guiName = "Abs Periapsis Change", guiFormat = "F5")]
        public double periapsisChange;
        [KSPField(guiActive = true, guiName = "Abs Apapsis Change", guiFormat = "F5")]
        public double apapsisChange;
        [KSPField(guiActive = true, guiName = "Orbit size Change", guiFormat = "F5")]
        public double orbitSizeChange;

        protected Transform surfaceTransform = null;
        protected Animation solarSailAnim = null;

        // Reference distance for calculating thrust: sun to Kerbin (m)
        const double kerbin_distance = 13599840256;
        // Solar pressure N/m^2 at reference distance
        const double thrust_coeff = 9.08e-6;

        // Display numbers for force and acceleration
        protected double solar_force_d = 0;
        protected double solar_acc_d = 0;
        protected double solarForceAtDistance_d = 0;



        protected CelestialBody localStar;

        private Queue<double> periapsisChangeQueue = new Queue<double>(10);
        private Queue<double> apapsisChangeQueue = new Queue<double>(10);

        // GUI to deploy sail
        [KSPEvent(guiActive = true, guiName = "Deploy Sail", active = true)]
        public void DeploySail()
        {
            if (animName != null && solarSailAnim != null)
            {
                solarSailAnim[animName].speed = 1f;
                solarSailAnim[animName].normalizedTime = 0f;
                solarSailAnim.Blend(animName, 2f);
            }
            IsEnabled = true;
        }

        // GUI to retract sail
        [KSPEvent(guiActive = true, guiName = "Retract Sail", active = false)]
        public void RetractSail()
        {
            if (animName != null && solarSailAnim != null)
            {
                solarSailAnim[animName].speed = -1f;
                solarSailAnim[animName].normalizedTime = 1f;
                solarSailAnim.Blend(animName, 2f);
            }
            IsEnabled = false;
        }

        // Initialization
        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.None && state != StartState.Editor)
            {
                if (animName != null)
                    solarSailAnim = part.FindModelAnimators(animName).FirstOrDefault();

                if (IsEnabled)
                {
                    solarSailAnim[animName].speed = 1f;
                    solarSailAnim[animName].normalizedTime = 0f;
                    solarSailAnim.Blend(animName, 0.1f);
                }

                this.part.force_activate();
            }
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            localStar = PluginHelper.GetCurrentStar();            

            // Sail deployment GUI
            Events["DeploySail"].active = !IsEnabled;
            Events["RetractSail"].active = IsEnabled;

            // Text fields (acc & force)
            Fields["solarAcc"].guiActive = IsEnabled;
            Fields["forceAcquired"].guiActive = IsEnabled;

            forceAcquired = solar_force_d.ToString("E") + " N";
            solarAcc = solar_acc_d.ToString("E") + " m/s";
            solarForceAtDistance = solarForceAtDistance_d.ToString("E") + " N/m\xB2";
        }

        public override void OnFixedUpdate()
        {
            solarForceAtDistance_d = 0;
            solar_force_d = 0;
            solar_acc_d = 0;
            cosConeAngle = 0;

            if (FlightGlobals.fetch == null || localStar == null || part == null || vessel == null ||  !IsEnabled)
                return;

            UpdateChangeGui();

            double UT = Planetarium.GetUniversalTime();
            Vector3d positionSun = localStar.getPositionAtUT(UT);
            Vector3d positionVessel = vessel.orbit.getPositionAtUT(UT);

            // Not in sunlight
            if (!inSun(UT, positionSun, positionVessel))
                return;

            Vector3d ownsunPosition = positionVessel - positionSun;
            Vector3d partNormal = this.part.transform.up;

            // If normal points away from sun, negate so our force is always away from the sun
            // so that turning the backside towards the sun thrusts correctly
            if (Vector3d.Dot(this.part.transform.up, ownsunPosition) < 0)
                partNormal = -partNormal;

            // Magnitude of force proportional to cosine-squared of angle between sun-line and normal
            cosConeAngle = Vector3.Dot(ownsunPosition.normalized, partNormal);

            solarForceAtDistance_d = SolarForceAtDistance(positionSun, positionVessel);

            // Force from sunlight
            Vector3d solarForce = CalculateSolarForce(this, partNormal, cosConeAngle, solarForceAtDistance_d);

            // Acceleration from sunlight
            Vector3d solarAccel = solarForce / vessel.GetTotalMass() / 1000d;

            // Add To Sailed DeltaV
            sailedDeltaV += solarAccel.magnitude;

            // Acceleration from sunlight pr second
            Vector3d fixedSolatAccel = solarAccel * TimeWarp.fixedDeltaTime;

            // Apply acceleration
            if (this.vessel.packed)
                vessel.orbit.Perturb(fixedSolatAccel, UT);
            else
                vessel.ChangeWorldVelocity(fixedSolatAccel);

            // Update displayed force & acceleration
            solar_force_d = solarForce.magnitude;
            solar_acc_d = solarAccel.magnitude;
        }

        private void UpdateChangeGui()
        {
            periapsisChangeQueue.Enqueue((vessel.orbit.PeA - previousPeA) / TimeWarp.fixedDeltaTime);
            if (periapsisChangeQueue.Count > 10)
                periapsisChangeQueue.Dequeue();
            periapsisChange = periapsisChangeQueue.Average();

            apapsisChangeQueue.Enqueue((vessel.orbit.ApA - previousAeA) / TimeWarp.fixedDeltaTime);
            if (apapsisChangeQueue.Count > 10)
                apapsisChangeQueue.Dequeue();
            apapsisChange = apapsisChangeQueue.Average();

            orbitSizeChange = periapsisChange + apapsisChange;

            previousPeA = vessel.orbit.PeA;
            previousAeA = vessel.orbit.ApA;
        }

        // Test if an orbit at UT is in sunlight
        public static bool inSun(double UT, Vector3d positionSun, Vector3d positionVessel)
        {
            Vector3d bminusa = positionSun - positionVessel;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                // the sun should not block line of sight to the sun
                if (referenceBody.flightGlobalsIndex == 0)
                    continue;

                Vector3d refminusa = referenceBody.getPositionAtUT(UT) - positionVessel;                

                if (Vector3d.Dot(refminusa, bminusa) > 0 && Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude)
                {
                    Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                    if (tang.magnitude < referenceBody.Radius)
                        return false;
                }
            }
            return true;
        }

        private static double SolarForceAtDistance(Vector3d sunPosition, Vector3d ownPosition)
        {
            double distance_from_sun = Vector3.Distance(sunPosition, ownPosition);
            return thrust_coeff * kerbin_distance * kerbin_distance / distance_from_sun / distance_from_sun;
        }

        // Calculate solar force as function of
        // sail, orbit, transform, and UT
        public static Vector3d CalculateSolarForce(ModuleSolarSail sail, Vector3d partNormal, double cosConeAngle, double solarForceAtDistance)
        {
            return partNormal * cosConeAngle * cosConeAngle * sail.surfaceArea * sail.reflectedPhotonRatio * solarForceAtDistance;
        }
    }
}
