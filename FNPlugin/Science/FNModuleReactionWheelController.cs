using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Powermanagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Science
{
    class FNModuleReactionWheelController : ResourceSuppliableModule
    {
        [KSPField(isPersistant = true)]
        public VesselAutopilot.AutopilotMode persistentAutopilotMode;

        public const string Group = "FNModuleReactionWheelController";
        public const string GroupTitle = "Reaction Wheel";

        // Settings
        [KSPField] public double maxPowerCost = 1;
        [KSPField] public double maxBufferCapacity = 0.01;

        [KSPField] public double saturationMinAngVel = 1 * (Math.PI / 180);
        [KSPField] public double saturationMaxAngVel = 15 * (Math.PI / 180);
        [KSPField] public double saturationMinTorqueFactor = 0.01;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "Auto Stabilizing"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
        public bool isAutoStabilizing = true;

        // Gui
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiFormat = "F4")] public double maxRollTorque;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiFormat = "F4")] public double maxPitchTorque;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiFormat = "F4")] public double maxYawTorque;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double torqueRoll;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double torquePitch;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double torqueYaw;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double rollFactor;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double pitchFactor;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double yawFactor;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double totalRoll;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double totalPitch;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double totalYaw;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double saturationRollFactor;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double saturationPitchFactor;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double saturationYawFactor;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiFormat = "F0", guiUnits = "%", guiName = "Roll Saturation")] public double saturationRollPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiFormat = "F0", guiUnits = "%", guiName = "Pitch Saturation")] public double saturationPitchPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiFormat = "F0", guiUnits = "%", guiName = "Yaw Saturation")] public double saturationYawPercentage;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double magnitude;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double powerFactor;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F4")] public double headingSpinDelta;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiFormat = "F2")] public double rotationSpinDelta;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "Is stabilized")] public bool isStable;

        private readonly Queue<double> _torqueRatioQueue = new Queue<double>();

        private double maxPowerCostMj;
        private double _bufferPower;
        private double _bufferOvercapacity;
        private double _fixedUpdateCount;

        private double angularMomentumSaturationRollTorqueFactor;
        private double angularMomentumSaturationPitchTorqueFactor;
        private double angularMomentumSaturationYawTorqueFactor;

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

            maxRollTorque = (double)(decimal)_reactionWheel.RollTorque;
            maxPitchTorque = (double)(decimal)_reactionWheel.PitchTorque;
            maxYawTorque = (double)(decimal)_reactionWheel.YawTorque;
        }

        [KSPEvent(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "Stabilize", active = true)]
        public void RemoveAngularMomentum()
        {
            StabilizeWhenPossible(0.005f, 1, 1, 1);
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            if (_reactionWheel == null) return;

            // save or restore AutopilotMode
            if (_fixedUpdateCount++ > 100)
                persistentAutopilotMode = vessel.Autopilot.Mode;
            else
                vessel.Autopilot.SetMode(persistentAutopilotMode);

            // detect if vessel is heading
            headingSpinDelta = (previousPartHeading - part.transform.up).magnitude / TimeWarp.deltaTime;
            rotationSpinDelta = (previousPartRotation - part.transform.rotation.eulerAngles).magnitude / TimeWarp.deltaTime;
            previousPartRotation = part.transform.rotation.eulerAngles;
            previousPartHeading = part.transform.up;

            var isStabilized = false;
            if (isAutoStabilizing)
                isStabilized = StabilizeWhenPossible(0.0005f, 1, 1, 1);

            var torqueRatio = GetTorqueRatio(isStabilized);

            _torqueRatioQueue.Enqueue(torqueRatio);
            if (_torqueRatioQueue.Count > 4)
                _torqueRatioQueue.Dequeue();

            maxPowerCostMj = maxPowerCost / GameConstants.ecPerMJ;
            var requiredPower = torqueRatio * maxPowerCostMj;
            var requestedPower = Math.Max(1e-8, _torqueRatioQueue.Max() * maxPowerCostMj - _bufferOvercapacity);
            var receivedPower = ConsumeMegawatts(requestedPower, true, true, true);

            _bufferPower =  Math.Max(0, _bufferPower + receivedPower - requiredPower);
            _bufferOvercapacity = Math.Min(maxBufferCapacity,  Math.Max(0, _bufferPower - maxBufferCapacity));
            _bufferPower = Math.Min(_bufferPower, maxBufferCapacity);

            powerFactor = requiredPower > 0 ? Math.Min(1, (receivedPower + _bufferPower) / requiredPower) : 1;

            UpdateReactionWheelTorque();
        }

        private double GetTorqueRatio(bool idStabilized)
        {
            var torque = _reactionWheel.GetAppliedTorque();

            torqueRoll = (double)(decimal)torque.y;
            torquePitch = (double)(decimal)torque.x;
            torqueYaw = (double)(decimal)torque.z;
            magnitude = (double)(decimal)torque.magnitude;

            var deltaTime = (double)(decimal)TimeWarp.deltaTime;


            if (!idStabilized)
            {
                totalRoll =  Math.Min(maxRollTorque  * 20, totalRoll  + deltaTime * torqueRoll);
                totalPitch = Math.Min(maxPitchTorque * 20, totalPitch + deltaTime * torquePitch);
                totalYaw =   Math.Min(maxYawTorque   * 20, totalYaw   + deltaTime * torqueYaw);
            }

            // breed torque
            totalRoll = totalRoll > 0
                ? Math.Max(0, totalRoll - TimeWarp.deltaTime * maxRollTorque * 0.05)
                : Math.Min(0, totalRoll + TimeWarp.deltaTime * maxRollTorque * 0.05);
            totalPitch = totalPitch > 0
                ? Math.Max(0, totalPitch - TimeWarp.deltaTime * maxPitchTorque * 0.05)
                : Math.Min(0, totalPitch + TimeWarp.deltaTime * maxPitchTorque * 0.05);
            totalYaw = totalYaw > 0
                ? Math.Max(0, totalYaw - TimeWarp.deltaTime * maxYawTorque * 0.05)
                : Math.Min(0, totalYaw + TimeWarp.deltaTime * maxYawTorque * 0.05);

            // calculate relative torque
            rollFactor = maxRollTorque > 0 ? Math.Abs(torqueRoll) / maxRollTorque : 0;
            pitchFactor = maxPitchTorque > 0 ? Math.Abs(torquePitch) / maxPitchTorque : 0;
            yawFactor = maxYawTorque > 0 ? Math.Abs(torqueYaw) / maxYawTorque : 0;

            angularMomentumSaturationRollTorqueFactor = Math.Min(Math.Max(Math.Abs(vessel.angularVelocityD.y) - saturationMinAngVel, 0.0) / saturationMaxAngVel, 1.0);
            angularMomentumSaturationPitchTorqueFactor = Math.Min(Math.Max(Math.Abs(vessel.angularVelocityD.x) - saturationMinAngVel, 0.0) / saturationMaxAngVel, 1.0);
            angularMomentumSaturationYawTorqueFactor = Math.Min(Math.Max(Math.Abs(vessel.angularVelocityD.z) - saturationMinAngVel, 0.0) / saturationMaxAngVel, 1.0);

            saturationRollFactor  = Math.Min(1, angularMomentumSaturationRollTorqueFactor + Math.Abs(totalRoll)   / (maxRollTorque  * 20));
            saturationPitchFactor = Math.Min(1, angularMomentumSaturationPitchTorqueFactor + Math.Abs(totalPitch) / (maxPitchTorque * 20));
            saturationYawFactor   = Math.Min(1, angularMomentumSaturationYawTorqueFactor + Math.Abs(totalYaw)     / (maxYawTorque   * 20));

            saturationRollPercentage  = saturationRollFactor  * 100;
            saturationPitchPercentage = saturationPitchFactor * 100;
            saturationYawPercentage   = saturationYawFactor   * 100;

            var torqueRatio = rollFactor + pitchFactor + yawFactor;
            return torqueRatio;
        }

        private void UpdateReactionWheelTorque()
        {
            if (powerFactor.IsInfinityOrNaN()) return;

            if (!saturationRollFactor.IsInfinityOrNaN())
                _reactionWheel.RollTorque = (float) (powerFactor * maxRollTorque * Math.Max(1 - saturationRollFactor, saturationMinTorqueFactor));

            if (!saturationPitchFactor.IsInfinityOrNaN())
                _reactionWheel.PitchTorque = (float) (powerFactor * maxPitchTorque * Math.Max(1 - saturationPitchFactor, saturationMinTorqueFactor));

            if (!saturationYawFactor.IsInfinityOrNaN())
                _reactionWheel.YawTorque = (float) (powerFactor * maxYawTorque * Math.Max(1 - saturationYawFactor, saturationMinTorqueFactor));
        }

        private bool StabilizeWhenPossible(float headingThreshold, float rotationThreshold, float pitchThreshold, float yawThreshold)
        {
            if (vessel.ctrlState.pitch > 0.5
                || vessel.ctrlState.roll > 0.5
                || vessel.ctrlState.yaw > 0.5
                || rollFactor > 0.5 || pitchFactor > 0.5 || yawFactor > 0.5
                || GameSettings.ROLL_LEFT.GetKey(true)
                || GameSettings.ROLL_RIGHT.GetKey(true)
                || GameSettings.PITCH_UP.GetKey(true)
                || GameSettings.PITCH_DOWN.GetKey(true)
                || GameSettings.YAW_LEFT.GetKey(true)
                || GameSettings.YAW_RIGHT.GetKey(true))
            {
                isStable = false;
            }

            if (!isStable && !vessel.packed
                          && vessel.Autopilot.Enabled
                          && pitchFactor < pitchThreshold * 0.05
                          && yawFactor < yawThreshold * 0.05
                          && headingSpinDelta < headingThreshold
                          && rotationSpinDelta < rotationThreshold
                          && !GameSettings.ROLL_LEFT.GetKey(true)
                          && !GameSettings.ROLL_RIGHT.GetKey(true)
                          && !GameSettings.PITCH_UP.GetKey(true)
                          && !GameSettings.PITCH_DOWN.GetKey(true)
                          && !GameSettings.YAW_LEFT.GetKey(true)
                          && !GameSettings.YAW_RIGHT.GetKey(true)
                          && Math.Abs(vessel.ctrlState.pitch) < pitchThreshold
                          && Math.Abs(vessel.ctrlState.yaw) < yawThreshold
                          && vessel.ctrlState.mainThrottle <= 0
                          && _reactionWheel.State == ModuleReactionWheel.WheelState.Active
                          && (vessel.situation == Vessel.Situations.ORBITING
                              || vessel.situation == Vessel.Situations.ESCAPING
                              || vessel.situation == Vessel.Situations.SUB_ORBITAL)
                )
            {
                totalRoll = totalRoll > 0
                    ? Math.Max(0, totalRoll - TimeWarp.deltaTime * Math.Abs(torqueRoll))
                    : Math.Min(0, totalRoll + TimeWarp.deltaTime * Math.Abs(torqueRoll));
                totalPitch = totalPitch > 0
                    ? Math.Max(0, totalPitch - TimeWarp.deltaTime * Math.Abs(torquePitch))
                    : Math.Min(0, totalPitch + TimeWarp.deltaTime * Math.Abs(torquePitch));
                totalYaw = totalYaw > 0
                    ? Math.Max(0, totalYaw - TimeWarp.deltaTime * Math.Abs(torqueYaw))
                    : Math.Min(0, totalYaw + TimeWarp.deltaTime * Math.Abs(torqueYaw));

                var mode = vessel.Autopilot.Mode;
                vessel.Autopilot.Disable();
                vessel.GoOnRails();
                vessel.GoOffRails();
                vessel.Autopilot.Enable();
                vessel.Autopilot.SetMode(mode);
                isStable = true;
                return true;
            }

            return false;
        }
    }
}
