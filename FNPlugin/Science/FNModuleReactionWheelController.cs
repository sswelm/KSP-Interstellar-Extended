using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Powermanagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Science
{
    class FNModuleReactionWheelController : ResourceSuppliableModule
    {
        public const string Group = "FNModuleReactionWheelController";
        public const string GroupTitle = "Reaction Wheel";

        // Settings
        [KSPField] public double maxPowerCost = 1;
        [KSPField] public double maxBufferCapacity = 0.01;

        // Gui
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true)] public float maxRollTorque;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true)] public float maxPitchTorque;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true)] public float maxYawTorque;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true)] public float torqueRoll;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true)] public float torquePitch;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true)] public float torqueYaw;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true)] public float totalRol;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true)] public float totalPitch;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true)] public float totalYaw;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = true)] public float magnitude;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = true)] public double powerRatio;

        private ModuleReactionWheel _reactionWheel;

        private readonly Queue<double> _torqueRatioQueue = new Queue<double>();

        private double maxPowerCostMj;
        private double _bufferPower;
        private double _bufferOvercapacity;

        private Vector3d previousPartHeading;
        private Vector3d previousPartRotation;

        public override void OnStart(PartModule.StartState state)
        {
            _reactionWheel = part.FindModuleImplementing<ModuleReactionWheel>();

            if (_reactionWheel == null)
                return;

            maxRollTorque = _reactionWheel.RollTorque;
            maxPitchTorque = _reactionWheel.PitchTorque;
            maxYawTorque = _reactionWheel.YawTorque;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "Stabilize", active = true)]
        public void RemoveAngularMomentum()
        {
            StabilizeWhenPossible(1e-4f, 0.01f, 1, 1);
        }

        private void StabilizeRotation()
        {
            if (vessel.packed) return;

            vessel.GoOnRails();
            //vessel.SetRotation(vessel.transform.rotation);
            vessel.GoOffRails();
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            if (_reactionWheel == null) return;

            StabilizeWhenPossible(1e-5f, 0.01f,  1f, 1f);

            var torque = _reactionWheel.GetAppliedTorque();

            torqueRoll = torque.y;
            torquePitch = torque.x;
            torqueYaw = torque.z;
            magnitude = torque.magnitude;

            totalRol += torqueRoll;
            totalPitch += torquePitch;
            totalYaw += torqueYaw;

            var rollRatio = maxRollTorque > 0 ? Mathf.Abs(torqueRoll) / maxRollTorque : 0;
            var pitchRatio = maxPitchTorque > 0 ? Mathf.Abs(torquePitch) / maxPitchTorque : 0;
            var yawRatio = maxYawTorque > 0 ? Mathf.Abs(torqueYaw) / maxYawTorque : 0;
            var torqueRatio = rollRatio + pitchRatio + yawRatio;

            _torqueRatioQueue.Enqueue(torqueRatio);
            if (_torqueRatioQueue.Count > 4)
                _torqueRatioQueue.Dequeue();

            maxPowerCostMj = maxPowerCost / GameConstants.ecPerMJ;

            var requiredPower = (torqueRatio * maxPowerCostMj);

            var requestedPower = System.Math.Max(1e-8, (_torqueRatioQueue.Max() * maxPowerCostMj) - _bufferOvercapacity);

            var receivedPower = ConsumeMegawatts(requestedPower, true, true, true);

            _bufferPower += receivedPower - requiredPower;
            _bufferOvercapacity = System.Math.Min(maxBufferCapacity,  System.Math.Max(0, _bufferPower - maxBufferCapacity));
            _bufferPower = System.Math.Min(_bufferPower, maxBufferCapacity);

            powerRatio = requiredPower > 0 ? System.Math.Min(1, (receivedPower + _bufferPower) / requiredPower) : 1;

            if (!powerRatio.IsInfinityOrNaN())
            {
                _reactionWheel.RollTorque = (float) (powerRatio * maxRollTorque);
                _reactionWheel.PitchTorque = (float) (powerRatio * maxPitchTorque);
                _reactionWheel.YawTorque = (float) (powerRatio * maxYawTorque);
            }
        }

        private bool StabilizeWhenPossible(float headingThreshold, float rotationThreshold, float pitchThreshold, float yawThreshold)
        {
            bool isStabilized = false;

            // detect if vessel is heading
            var headingMagnitudeDiff = (previousPartHeading - part.transform.up).magnitude;
            var rotationMagnitudeDiff = (previousPartRotation - part.transform.rotation.eulerAngles).magnitude;

            if (headingMagnitudeDiff < headingThreshold
                && rotationMagnitudeDiff <= rotationThreshold
                && vessel.ctrlState.mainThrottle <= 0
                && !GameSettings.ROLL_LEFT.GetKey(true)
                && !GameSettings.ROLL_RIGHT.GetKey(true)
                && !GameSettings.PITCH_UP.GetKey(true)
                && !GameSettings.PITCH_DOWN.GetKey(true)
                && !GameSettings.YAW_LEFT.GetKey(true)
                && !GameSettings.YAW_RIGHT.GetKey(true)
                && System.Math.Abs(vessel.ctrlState.pitch) < pitchThreshold
                && System.Math.Abs(vessel.ctrlState.yaw) < yawThreshold
                && _reactionWheel.State == ModuleReactionWheel.WheelState.Active
                && (vessel.situation == Vessel.Situations.ORBITING
                    || vessel.situation == Vessel.Situations.ESCAPING
                    || vessel.situation == Vessel.Situations.SUB_ORBITAL)
                )
            {
                StabilizeRotation();
                isStabilized = true;
            }

            previousPartRotation = part.transform.rotation.eulerAngles;
            previousPartHeading = part.transform.up;

            return isStabilized;
        }
    }
}
