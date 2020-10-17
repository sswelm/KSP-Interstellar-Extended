using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    public class FNModuleRCSFX : ModuleRCS
    {
        // FNModuleRCSFX is a clone of ModuleRCSFX 0.4.2
        [KSPField]
        public bool useEffects = false;

        [KSPField]
        public string flameoutEffectName = "";

        [KSPField(guiActiveEditor = false)]
        public string RCS = "Enable/Disable for:";

        //[KSPField(guiActive = true)]
        public float curThrust = 0f;

        /// <summary>
        /// Fuel flow in tonnes/sec
        /// </summary>
        public double fuelFlow = 0f;

        float inputAngularX;
        float inputAngularY;
        float inputAngularZ;
        float inputLinearX;
        float inputLinearY;
        float inputLinearZ;

        private Vector3 inputLinear;
        private Vector3 inputAngular;
        private bool precision;
        private double exhaustVel = 0d;

        private const double invG = 1d / 9.80665d;

        /// <summary>
        /// If control actuation < this, ignore.
        /// </summary>
        [KSPField]
        float EPSILON = 0.05f; // 5% control actuation

        public float mixtureFactor;

        public FNModuleRCSFX()
        {
            flowMult = 1.0;
            fullThrust = false;
            fullThrustMin = 0.2f;
            ispMult = 1.0;
            precisionFactor = 0.1f;
            useZaxis = false;
            useLever = false;
            useThrottle = false;
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!node.HasNode("PROPELLANT") && node.HasValue("resourceName"))
            {
                ConfigNode c = new ConfigNode("PROPELLANT");
                c.AddValue("name", node.GetValue("resourceName"));
                c.AddValue("ratio", "1.0");
                if (node.HasValue("resourceFlowMode"))
                    c.AddValue("resourceFlowMode", node.GetValue("resourceFlowMode"));
                node.AddNode(c);
            }
            base.OnLoad(node);
            G = 9.80665f;
            fuelFlow = (double)thrusterPower / (double)atmosphereCurve.Evaluate(0f) * invG;
        }

        public override string GetInfo()
        {
            string text = base.GetInfo();
            return text;
        }

        public override void OnStart(StartState state)
        {
            if (useEffects) // use EFFECTS so don't do the base startup. That means we have to do this ourselves.
            {
                part.stackIcon.SetIcon(DefaultIcons.RCS_MODULE);
                part.stackIconGrouping = StackIconGrouping.SAME_TYPE;
                thrusterTransforms = new List<Transform>(part.FindModelTransforms(thrusterTransformName));
                if (thrusterTransforms == null || thrusterTransforms.Count == 0)
                {
                    Debug.Log("RCS module unable to find any transforms in part named " + thrusterTransformName);
                }

            }
            else
                base.OnStart(state);
        }

        new public void Update()
        {
            if (this.part.vessel == null)
                return;

            float ctrlZ = vessel.ctrlState.Z;
            if (useThrottle && ctrlZ < EPSILON && ctrlZ > -EPSILON) // only do this if not specifying axial thrust.
            {
                ctrlZ -= vessel.ctrlState.mainThrottle;
                ctrlZ = Mathf.Clamp(ctrlZ, -1f, 1f);
            }
            inputLinear = vessel.ReferenceTransform.rotation * new Vector3(enableX ? vessel.ctrlState.X : 0, enableZ ? ctrlZ : 0, enableY ? vessel.ctrlState.Y : 0);
            inputAngular = vessel.ReferenceTransform.rotation * new Vector3(enablePitch ? vessel.ctrlState.pitch : 0, enableRoll ? vessel.ctrlState.roll : 0, enableYaw ? vessel.ctrlState.yaw : 0);

            // Epsilon checks (min values)
            float EPSILON2 = EPSILON * EPSILON;
            inputAngularX = inputAngular.x;
            inputAngularY = inputAngular.y;
            inputAngularZ = inputAngular.z;
            inputLinearX = inputLinear.x;
            inputLinearY = inputLinear.y;
            inputLinearZ = inputLinear.z;
            if (inputAngularX * inputAngularX < EPSILON2)
                inputAngularX = 0;
            if (inputAngularY * inputAngularY < EPSILON2)
                inputAngularY = 0;
            if (inputAngularZ * inputAngularZ < EPSILON2)
                inputAngularZ = 0;
            if (inputLinearX * inputLinearX < EPSILON2)
                inputLinearX = 0;
            if (inputLinearY * inputLinearY < EPSILON2)
                inputLinearY = 0;
            if (inputLinearZ * inputLinearZ < EPSILON2)
                inputLinearZ = 0;
            inputLinear.x = inputLinearX;
            inputLinear.y = inputLinearY;
            inputLinear.z = inputLinearZ;
            inputAngular.x = inputAngularX;
            inputAngular.y = inputAngularY;
            inputAngular.z = inputAngularZ;

            precision = FlightInputHandler.fetch.precisionMode;
        }

        new public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            int fxC = thrusterFX.Count;
            if (TimeWarp.CurrentRate > 1.0f && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
            {

                for (int i = 0; i < fxC; ++i)
                {
                    FXGroup fx = thrusterFX[i];
                    fx.setActive(false);
                    fx.Power = 0f;
                }
                return;
            }

            // set starting params for loop
            bool success = false;
            curThrust = 0f;

            // set Isp/EV
            realISP = atmosphereCurve.Evaluate((float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres));
            exhaustVel = (double)realISP * (double)G * ispMult;

            //thrustForces.Clear();
            thrustForces = new List<float>().ToArray();

            if (rcsEnabled && !part.ShieldedFromAirstream)
            {
                if (vessel.ActionGroups[KSPActionGroup.RCS] != rcs_active)
                {
                    rcs_active = vessel.ActionGroups[KSPActionGroup.RCS];
                }
                if (vessel.ActionGroups[KSPActionGroup.RCS] && (inputAngular != Vector3.zero || inputLinear != Vector3.zero))
                {

                    // rb_velocity should include timewarp, right?
                    Vector3 CoM = vessel.CoM + vessel.rb_velocity * Time.fixedDeltaTime;

                    float effectPower = 0f;
                    int xformCount = thrusterTransforms.Count;
                    for (int i = 0; i < xformCount; ++i)
                    {
                        Transform xform = thrusterTransforms[i];
                        if (xform.position != Vector3.zero)
                        {
                            Vector3 position = xform.position;
                            Vector3 torque = Vector3.Cross(inputAngular, (position - CoM).normalized);

                            Vector3 thruster;
                            if (useZaxis)
                                thruster = xform.forward;
                            else
                                thruster = xform.up;
                            float thrust = Mathf.Max(Vector3.Dot(thruster, torque), 0f);
                            thrust += Mathf.Max(Vector3.Dot(thruster, inputLinear), 0f);

                            // thrust should now be normalized 0-1.

                            if (thrust > 0f)
                            {
                                if (fullThrust && thrust >= fullThrustMin)
                                    thrust = 1f;

                                if (precision)
                                {
                                    if (useLever)
                                    {
                                        //leverDistance = GetLeverDistanceOriginal(predictedCOM);
                                        //float leverDistance = GetLeverDistance(-thruster, CoM);
                                        float leverDistance = GetLeverDistance(xform, -thruster, CoM);

                                        if (leverDistance > 1)
                                        {
                                            thrust /= leverDistance;
                                        }
                                    }
                                    else
                                    {
                                        thrust *= precisionFactor;
                                    }
                                }

                                UpdatePropellantStatus();
                                float thrustForce = CalculateThrust(thrust, out success);

                                if (success)
                                {
                                    curThrust += thrustForce;
                                    //thrustForces.Add(thrustForce);
                                    var newForces = thrustForces.ToList();
                                    newForces.Add(thrustForce);
                                    thrustForces = newForces.ToArray();
                                    if (!isJustForShow)
                                    {
                                        Vector3 force = -thrustForce * thruster;

                                        part.Rigidbody.AddForceAtPosition(force, position, ForceMode.Force);
                                        //Debug.Log("Part " + part.name + " adding force " + force.x + "," + force.y + "," + force.z + " at " + position);
                                    }

                                    thrusterFX[i].Power = Mathf.Clamp(thrust, 0.1f, 1f);
                                    if (effectPower < thrusterFX[i].Power)
                                        effectPower = thrusterFX[i].Power;
                                    thrusterFX[i].setActive(thrustForce > 0);
                                }
                                else
                                {
                                    thrusterFX[i].Power = 0;
                                }
                            }
                            else
                            {
                                thrusterFX[i].Power = 0;
                            }
                        }
                    }
                }
            }
            if (!success)
            {
                for (int i = 0; i < fxC; ++i)
                {
                    FXGroup fx = thrusterFX[i];
                    fx.setActive(false);
                    fx.Power = 0f;
                }
            }

        }

        private void UpdatePropellantStatus()
        {
            if ((object)propellants != null)
            {
                int pCount = propellants.Count;
                for (int i = 0; i < pCount; ++i)
                    propellants[i].UpdateConnectedResources(part);
            }
        }

        new public float CalculateThrust(float totalForce, out bool success)
        {
            double massFlow = flowMult * fuelFlow * (double)totalForce;

            double propAvailable = 1.0d;

            if (!CheatOptions.InfinitePropellant)
                propAvailable = RequestPropellant(massFlow * TimeWarp.fixedDeltaTime);

            totalForce = (float)(massFlow * exhaustVel * propAvailable);

            success = (propAvailable > 0f); // had some fuel
            return totalForce;
        }
    }
}
