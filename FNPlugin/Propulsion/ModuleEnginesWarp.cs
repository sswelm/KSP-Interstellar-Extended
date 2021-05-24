using System;
using FNPlugin.Extensions;
using KSP.Localization;
using UnityEngine;

namespace FNPlugin.Propulsion
{
    public class ModuleEnginesThermalNozzle : ModuleEnginesWarp { }

    public class ModuleEnginesMagneticNozzle : ModuleEnginesWarp { }

    public class ModuleEnginesWarp : ModuleEnginesFX
    {
        [KSPField(isPersistant = true)]
        bool IsForceActivated;

        [KSPField] public double GThreshold = 15;
        [KSPField] public string propellant1 = "LqdHydrogen";
        [KSPField] public string propellant2;
        [KSPField] public string propellant3;
        [KSPField] public string propellant4;

        [KSPField] public double ratio1 = 1;
        [KSPField] public double ratio2;
        [KSPField] public double ratio3;
        [KSPField] public double ratio4;

        [KSPField] public double demandMass;
        [KSPField] public double remainingMass;

        [KSPField] public double fuelRatio;
        [KSPField] private double averageDensityInTonPerLiter;
        [KSPField] private double massPropellantRatio;
        [KSPField] private double ratioSumWithoutMass;

        [KSPField] public double massDelta;
        [KSPField] public double deltaV;

        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ModuleEnginesWarp_MassFlow")]//Mass Flow
        public double requestedFlow;
        [KSPField(guiActive = true, guiName = "#autoLOC_6001377", guiUnits = "#autoLOC_7001408", guiFormat = "F6")]
        public double thrust_d;

        private Transform _engineThrustTransform;
        private Vector3d _engineThrustTransformUp;

        private int _vesselChangedSoiCountdown;

        private double _realIsp;
        private double _thrustPersistent;
        private double _throttlePersistent;
        private double _ratioHeadingVersusRequest;
        private double _totalMassVessel;

        private double fuelWithMassPercentage1;
        private double fuelWithMassPercentage2;
        private double fuelWithMassPercentage3;
        private double fuelWithMassPercentage4;

        private double masslessFuelPercentage1;
        private double masslessFuelPercentage2;
        private double masslessFuelPercentage3;
        private double masslessFuelPercentage4;

        private double fuelRequestAmount1;
        private double fuelRequestAmount2;
        private double fuelRequestAmount3;
        private double fuelRequestAmount4;

        private double consumedPropellant1;
        private double consumedPropellant2;
        private double consumedPropellant3;
        private double consumedPropellant4;

        private PartResourceDefinition propellantResourceDefinition1;
        private PartResourceDefinition propellantResourceDefinition2;
        private PartResourceDefinition propellantResourceDefinition3;
        private PartResourceDefinition propellantResourceDefinition4;

        // Are we transitioning from timewarp to realtime?
        private bool _warpToReal;

        public void VesselChangedSoi()
        {
            _vesselChangedSoiCountdown = 10;
        }

        // Update
        public override void OnUpdate()
        {
            // stop engines and drop out of timewarp when X pressed
            if (vessel.packed && _throttlePersistent > 0 && Input.GetKeyDown(KeyCode.X))
            {
                // Return to realtime
                TimeWarp.SetRate(0, true);

                _throttlePersistent = 0;
                vessel.ctrlState.mainThrottle = (float)_throttlePersistent;
            }

            // When transitioning from timewarp to real update throttle
            if (_warpToReal)
            {
                vessel.ctrlState.mainThrottle = (float)_throttlePersistent;
                _warpToReal = false;
            }

            // hide stock thrust
            Fields[nameof(finalThrust)].guiActive = false;

            if (IsForceActivated || !isEnabled || !isOperational) return;

            IsForceActivated = true;
            Debug.Log("[KSPI]: ModuleEngineWarp on " + part.name + " was Force Activated");
            part.force_activate();
        }

        private void UpdateFuelFactors()
        {
            propellantResourceDefinition1 = !string.IsNullOrEmpty(propellant1) ? PartResourceLibrary.Instance.GetDefinition(propellant1) : null;
            propellantResourceDefinition2 = !string.IsNullOrEmpty(propellant2) ? PartResourceLibrary.Instance.GetDefinition(propellant2) : null;
            propellantResourceDefinition3 = !string.IsNullOrEmpty(propellant3) ? PartResourceLibrary.Instance.GetDefinition(propellant3) : null;
            propellantResourceDefinition4 = !string.IsNullOrEmpty(propellant4) ? PartResourceLibrary.Instance.GetDefinition(propellant4) : null;

            var ratioSumOverall = 0.0;
            var ratioSumWithMass = 0.0;
            var densitySum = 0.0;

            if (propellantResourceDefinition1 != null)
            {
                ratioSumOverall += ratio1;
                if (propellantResourceDefinition1.density > 0)
                {
                    ratioSumWithMass = ratio1;
                    densitySum += propellantResourceDefinition1.density * ratio1;
                }
            }
            if (propellantResourceDefinition2 != null)
            {
                ratioSumOverall += ratio2;
                if (propellantResourceDefinition2.density > 0)
                {
                    ratioSumWithMass = ratio2;
                    densitySum += propellantResourceDefinition2.density * ratio2;
                }
            }
            if (propellantResourceDefinition3 != null)
            {
                ratioSumOverall += ratio3;
                if (propellantResourceDefinition3.density > 0)
                {
                    ratioSumWithMass = ratio3;
                    densitySum += propellantResourceDefinition3.density * ratio3;
                }
            }
            if (propellantResourceDefinition4 != null)
            {
                ratioSumOverall += ratio4;
                if (propellantResourceDefinition4.density > 0)
                {
                    ratioSumWithMass = ratio4;
                    densitySum += propellantResourceDefinition4.density * ratio4;
                }
            }

            averageDensityInTonPerLiter = densitySum / ratioSumWithMass;
            massPropellantRatio = ratioSumWithMass / ratioSumOverall;
            ratioSumWithoutMass = ratioSumOverall - ratioSumWithMass;

            fuelWithMassPercentage1 = propellantResourceDefinition1 != null && propellantResourceDefinition1.density > 0 ? ratio1 / ratioSumWithMass : 0;
            fuelWithMassPercentage2 = propellantResourceDefinition2 != null && propellantResourceDefinition2.density > 0 ? ratio2 / ratioSumWithMass : 0;
            fuelWithMassPercentage3 = propellantResourceDefinition3 != null && propellantResourceDefinition3.density > 0 ? ratio3 / ratioSumWithMass : 0;
            fuelWithMassPercentage4 = propellantResourceDefinition4 != null && propellantResourceDefinition4.density > 0 ? ratio4 / ratioSumWithMass : 0;

            masslessFuelPercentage1 = propellantResourceDefinition1 != null && propellantResourceDefinition1.density <= 0 ? ratio1 / ratioSumWithoutMass : 0;
            masslessFuelPercentage2 = propellantResourceDefinition2 != null && propellantResourceDefinition2.density <= 0 ? ratio2 / ratioSumWithoutMass : 0;
            masslessFuelPercentage3 = propellantResourceDefinition3 != null && propellantResourceDefinition3.density <= 0 ? ratio3 / ratioSumWithoutMass : 0;
            masslessFuelPercentage4 = propellantResourceDefinition4 != null && propellantResourceDefinition4.density <= 0 ? ratio4 / ratioSumWithoutMass : 0;
        }

        private double CollectFuel(double requestedMass, ResourceFlowMode fuelMode = ResourceFlowMode.STACK_PRIORITY_SEARCH)
        {
            fuelRequestAmount1 = 0;
            fuelRequestAmount2 = 0;
            fuelRequestAmount3 = 0;
            fuelRequestAmount4 = 0;

            if (CheatOptions.InfinitePropellant)
                return 1;

            if (requestedMass == 0 || double.IsNaN(requestedMass) || double.IsInfinity(requestedMass))
                return 0;

            var propellantWithMassNeededInLiter = requestedMass / averageDensityInTonPerLiter;
            var overallAmountNeeded = propellantWithMassNeededInLiter / massPropellantRatio;
            var masslessResourceNeeded = overallAmountNeeded - propellantWithMassNeededInLiter;

            // first determine lowest available resource ratio
            double availableRatio = 1;
            if (propellantResourceDefinition1 != null && ratio1 > 0)
            {
                fuelRequestAmount1 = fuelWithMassPercentage1 > 0 ? fuelWithMassPercentage1 * propellantWithMassNeededInLiter : masslessFuelPercentage1 * masslessResourceNeeded;
                availableRatio = Math.Min(availableRatio, part.GetResourceAvailable(propellantResourceDefinition1, fuelMode) / fuelRequestAmount1);
            }
            if (propellantResourceDefinition2 != null && ratio2 > 0)
            {
                fuelRequestAmount2 = fuelWithMassPercentage2 > 0 ? fuelWithMassPercentage2 * propellantWithMassNeededInLiter : masslessFuelPercentage2 * masslessResourceNeeded;
                availableRatio = Math.Min(availableRatio, part.GetResourceAvailable(propellantResourceDefinition2, fuelMode) / fuelRequestAmount2);
            }
            if (propellantResourceDefinition3 != null && ratio3 > 0)
            {
                fuelRequestAmount3 = fuelWithMassPercentage3 > 0 ? fuelWithMassPercentage3 * propellantWithMassNeededInLiter : masslessFuelPercentage3 * masslessResourceNeeded;
                availableRatio = Math.Min(availableRatio, part.GetResourceAvailable(propellantResourceDefinition3, fuelMode) / fuelRequestAmount3);
            }
            if (propellantResourceDefinition4 != null && ratio4 > 0)
            {
                fuelRequestAmount4 = fuelWithMassPercentage4 > 0 ? fuelWithMassPercentage4 * propellantWithMassNeededInLiter : masslessFuelPercentage4 * masslessResourceNeeded;
                availableRatio = Math.Min(availableRatio, part.GetResourceAvailable(propellantResourceDefinition4, fuelMode) / fuelRequestAmount4);
            }

            // ignore insignificant amount
            if (availableRatio < 1e-6)
                return 0;

            consumedPropellant1 = 0;
            consumedPropellant2 = 0;
            consumedPropellant3 = 0;
            consumedPropellant4 = 0;

            double receivedRatio = 1;
            if (fuelRequestAmount1 > 0 && !double.IsNaN(fuelRequestAmount1) && !double.IsInfinity(fuelRequestAmount1))
            {
                consumedPropellant1 = part.RequestResource(propellantResourceDefinition1.id, fuelRequestAmount1 * availableRatio, fuelMode);
                receivedRatio = Math.Min(receivedRatio, fuelRequestAmount1 > 0 ? consumedPropellant1 / fuelRequestAmount1 : 0);
            }
            if (fuelRequestAmount2 > 0 && !double.IsNaN(fuelRequestAmount2) && !double.IsInfinity(fuelRequestAmount2))
            {
                consumedPropellant2 = part.RequestResource(propellantResourceDefinition2.id, fuelRequestAmount2 * availableRatio, fuelMode);
                receivedRatio = Math.Min(receivedRatio, fuelRequestAmount2 > 0 ? consumedPropellant2 / fuelRequestAmount2 : 0);
            }
            if (fuelRequestAmount3 > 0 && !double.IsNaN(fuelRequestAmount3) && !double.IsInfinity(fuelRequestAmount3))
            {
                consumedPropellant3 = part.RequestResource(propellantResourceDefinition3.id, fuelRequestAmount3 * availableRatio, fuelMode);
                receivedRatio = Math.Min(receivedRatio, fuelRequestAmount3 > 0 ? consumedPropellant3 / fuelRequestAmount3 : 0);
            }
            if (fuelRequestAmount4 > 0 && !double.IsNaN(fuelRequestAmount4) && !double.IsInfinity(fuelRequestAmount4))
            {
                consumedPropellant4 = part.RequestResource(propellantResourceDefinition4.id, fuelRequestAmount4 * availableRatio, fuelMode);
                receivedRatio = Math.Min(receivedRatio, fuelRequestAmount4 > 0 ? consumedPropellant4 / fuelRequestAmount4 : 0);
            }

            return Math.Min (receivedRatio, 1);
        }

        // Physics update
        public override void OnFixedUpdate()
        {
            if (_vesselChangedSoiCountdown > 0)
                _vesselChangedSoiCountdown--;

            if (FlightGlobals.fetch == null || !isEnabled) return;

            UpdateFuelFactors();

            if (double.IsNaN(requestedMassFlow) || double.IsInfinity(requestedMassFlow))
                Debug.LogWarning("[KSPI]: requestedMassFlow  is " + requestedMassFlow);
            if (double.IsNaN(realIsp) || double.IsInfinity(realIsp))
                Debug.LogWarning("[KSPI]: realIsp  is " + realIsp);
            if (double.IsNaN(finalThrust) || double.IsInfinity(finalThrust))
                Debug.LogWarning("[KSPI]: maxEffectiveThrust  is " + finalThrust);

            _realIsp = realIsp;

            requestedFlow = requestedMassFlow;
            _totalMassVessel = vessel.totalMass;

            // Check if we are in time warp mode
            if (!vessel.packed)
            {
                // allow throttle to be used up to Geeforce threshold
                TimeWarp.GThreshold = GThreshold;

                demandMass = requestedFlow * (double)(decimal)TimeWarp.fixedDeltaTime;

                // if not transitioning from warp to real
                // Update values to use during timewarp
                if (!_warpToReal)
                {
                    _throttlePersistent = vessel.ctrlState.mainThrottle;

                    if (_throttlePersistent == 0 && finalThrust < 0.0000005)
                        _thrustPersistent = 0;
                    else
                        _thrustPersistent = finalThrust;
                }

                _ratioHeadingVersusRequest = 0;
            }
            else
            {
                // Timewarp mode: perturb orbit using thrust
                _warpToReal = true; // Set to true for transition to realtime

                _thrustPersistent = requestedFlow * PhysicsGlobals.GravitationalAcceleration * _realIsp;

                // only persist thrust if active and non zero throttle or significant thrust
                if (getIgnitionState && (currentThrottle > 0 || _thrustPersistent > 0.0000005))
                {
                    _ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSoiCountdown > 0, _ratioHeadingVersusRequest == 1);
                    if (_ratioHeadingVersusRequest != 1)
                        return;

                    // determine maximum deltaV during this frame
                    demandMass = requestedFlow * (double)(decimal)TimeWarp.fixedDeltaTime;
                    remainingMass = _totalMassVessel - demandMass;

                    deltaV = _realIsp * PhysicsGlobals.GravitationalAcceleration * Math.Log(_totalMassVessel / remainingMass);

                    _engineThrustTransform = part.FindModelTransform(thrustVectorTransformName);
                    if (_engineThrustTransform == null)
                    {
                        _engineThrustTransform = part.transform;
                        _engineThrustTransformUp = _engineThrustTransform.up;
                    }
                    else
                        _engineThrustTransformUp = (Vector3d)_engineThrustTransform.forward * -1;

                    double persistentThrustDot = Vector3d.Dot(_engineThrustTransformUp, vessel.obt_velocity);
                    if (persistentThrustDot < 0 && (vessel.obt_velocity.magnitude <= deltaV * 2))
                    {
                        var message = Localizer.Format("#LOC_KSPIE_ModuleEnginesWarp_PostMsg1");//"Thrust warp stopped - orbital speed too low"
                        ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                        Debug.Log("[KSPI]: " + message);
                        TimeWarp.SetRate(0, true);
                        return;
                    }

                    fuelRatio = CollectFuel(demandMass, ResourceFlowMode.ALL_VESSEL);

                    // Calculate thrust and deltaV if demand output > 0
                    if (IsPositiveValidNumber(fuelRatio) && IsPositiveValidNumber(demandMass) && IsPositiveValidNumber(_totalMassVessel) && IsPositiveValidNumber(_realIsp))
                    {
                        remainingMass = vessel.totalMass - (demandMass * fuelRatio); // Mass at end of burn

                        massDelta = Math.Log(_totalMassVessel / remainingMass);
                        deltaV = _realIsp * PhysicsGlobals.GravitationalAcceleration * massDelta; // Delta V from burn
                        vessel.orbit.Perturb(deltaV * _engineThrustTransformUp, Planetarium.GetUniversalTime()); // Update vessel orbit

                        if (fuelRatio < 0.999)
                        {
                            var message = Localizer.Format("#LOC_KSPIE_ModuleEnginesWarp_PostMsg2");//"Thrust warp stopped - running out of propellant"
                            Debug.Log("[KSPI]: " + message);
                            ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                            // Return to realtime
                            TimeWarp.SetRate(0, true);
                        }
                    }
                    else if (demandMass > 0)
                    {
                        var message = Localizer.Format("#LOC_KSPIE_ModuleEnginesWarp_PostMsg3");//"Thrust warp stopped - propellant depleted"
                        Debug.Log("[KSPI]: " + message);
                        ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                        // Return to realtime
                        TimeWarp.SetRate(0, true);
                    }
                }
                else
                {
                    _ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSoiCountdown > 0);

                    _thrustPersistent = 0;
                    requestedFlow = 0;
                    demandMass = 0;
                    fuelRatio = 0;
                }
            }

            // Update display numbers
            thrust_d = _thrustPersistent;
        }

        private bool IsPositiveValidNumber(double variable)
        {
            return !double.IsNaN(variable) && !double.IsInfinity(variable) && variable > 0;
        }

        // Format thrust into mN, N, kN
        public static string FormatThrust(double thrust)
        {
            if (thrust < 1e-6)
                return Math.Round(thrust * 1e+9, 3) + " µN";
            if (thrust < 1e-3)
                return Math.Round(thrust * 1e+6, 3) + " mN";
            else if (thrust < 1)
                return Math.Round(thrust * 1e+3, 3) + " N";
            else
                return Math.Round(thrust, 3) + " kN";
        }
    }

}
