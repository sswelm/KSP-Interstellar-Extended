using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Powermanagement;
using System;
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

        [KSPField] public double saturationMinAngVel = 1 * (Math.PI / 180);
        [KSPField] public double saturationMaxAngVel = 15 * (Math.PI / 180);
        [KSPField] public double saturationMinTorqueFactor = 5 * 0.01;

        // Gui
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true)] public float maxRollTorque;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true)] public float maxPitchTorque;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true)] public float maxYawTorque;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public float torqueRoll;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public float torquePitch;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public float torqueYaw;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public float totalRol;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public float totalPitch;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public float totalYaw;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public float magnitude;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false)] public double powerFactor;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiFormat = "F4")] public double headingSpinDelta;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiFormat = "F2")] public double rotationSpinDelta;

        private readonly Queue<double> _torqueRatioQueue = new Queue<double>();

        private double maxPowerCostMj;
        private double _bufferPower;
        private double _bufferOvercapacity;

        private ModuleReactionWheel _reactionWheel;
        private Vector3d previousPartHeading;
        private Vector3d previousPartRotation;

        public override void OnStart(StartState state)
        {
            _reactionWheel = part.FindModuleImplementing<ModuleReactionWheel>();

            if (_reactionWheel == null)
                return;

            var actuatorModeCycleField = _reactionWheel.Fields[nameof(_reactionWheel.actuatorModeCycle)];
            var authorityLimiterField = _reactionWheel.Fields[nameof(_reactionWheel.authorityLimiter)];
            var stateStringField = _reactionWheel.Fields[nameof(_reactionWheel.stateString)];
            var onToggleEvent = _reactionWheel.Events[nameof(_reactionWheel.OnToggle)];

            actuatorModeCycleField.group = new BasePAWGroup(Group, GroupTitle, false);
            authorityLimiterField.group = new BasePAWGroup(Group, GroupTitle, false);
            stateStringField.group = new BasePAWGroup(Group, GroupTitle, false);
            onToggleEvent.group = new BasePAWGroup(Group, GroupTitle, false);

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
            vessel.GoOffRails();
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            if (_reactionWheel == null) return;

            // detect if vessel is heading
            headingSpinDelta = (previousPartHeading - part.transform.up).magnitude;
            rotationSpinDelta = (previousPartRotation - part.transform.rotation.eulerAngles).magnitude;
            previousPartRotation = part.transform.rotation.eulerAngles;
            previousPartHeading = part.transform.up;

            StabilizeWhenPossible(1e-5f, 0.01f,  1f, 1f);

            var torqueRatio = GetTorqueRatio();

            _torqueRatioQueue.Enqueue(torqueRatio);
            if (_torqueRatioQueue.Count > 4)
                _torqueRatioQueue.Dequeue();

            maxPowerCostMj = maxPowerCost / GameConstants.ecPerMJ;
            var requiredPower = torqueRatio * maxPowerCostMj;
            var requestedPower = Math.Max(1e-8, _torqueRatioQueue.Max() * maxPowerCostMj - _bufferOvercapacity);
            var receivedPower = ConsumeMegawatts(requestedPower, true, true, true);

            _bufferPower += receivedPower - requiredPower;
            _bufferOvercapacity = Math.Min(maxBufferCapacity,  Math.Max(0, _bufferPower - maxBufferCapacity));
            _bufferPower = Math.Min(_bufferPower, maxBufferCapacity);

            powerFactor = requiredPower > 0 ? Math.Min(1, (receivedPower + _bufferPower) / requiredPower) : 1;

            UpdateReactionWheelTorque();
        }

        private float GetTorqueRatio()
        {
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
            return torqueRatio;
        }

        private void UpdateReactionWheelTorque()
        {
            if (powerFactor.IsInfinityOrNaN()) return;

            var velSaturationTorqueFactor =
                Math.Max(
                    1.0 - Math.Min(
                        Math.Max(vessel.angularVelocityD.magnitude - saturationMinAngVel, 0.0) / saturationMaxAngVel,
                        1.0),
                    saturationMinTorqueFactor);

            _reactionWheel.RollTorque = (float) (velSaturationTorqueFactor * powerFactor * maxRollTorque);
            _reactionWheel.PitchTorque = (float) (velSaturationTorqueFactor * powerFactor * maxPitchTorque);
            _reactionWheel.YawTorque = (float) (velSaturationTorqueFactor * powerFactor * maxYawTorque);
        }

        private bool StabilizeWhenPossible(float headingThreshold, float rotationThreshold, float pitchThreshold, float yawThreshold)
        {
            if (headingSpinDelta < headingThreshold
                && rotationSpinDelta <= rotationThreshold
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
                return true;
            }

            return false;
        }
    }
}
