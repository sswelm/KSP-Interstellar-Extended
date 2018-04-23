using System;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin
{
    public class ModuleEnginesWarp : ModuleEnginesFX
    {
        // GUI display values
        // Thrust
        [KSPField(isPersistant = true)]
        bool IsForceActivated;

        [KSPField(guiActive = true, guiName = "Warp Thrust")]
        protected string Thrust = "";
        // Isp
        [KSPField(guiActive = true, guiName = "Warp Isp")]
        protected string Isp = "";
        // Throttle
        [KSPField(guiActive = true, guiName = "Warp Throttle")]
        protected string Throttle = "";

        
        //public double propellantUsed;
        [KSPField(guiActive = false, guiName = "Demand Fuel 1")]
        public double recievedFusionFuel1;
        [KSPField(guiActive = false, guiName = "Demand Fuel 2")]
        public double recievedFusionFuel2;
        [KSPField(guiActive = false, guiName = "Demand Fuel 3")]
        public double recievedFusionFuel3;
        [KSPField(guiActive = false, guiName = "Demand Fuel 4")]
        public double recievedFusionFuel4;

        // Resource used for deltaV and mass calculations
        //[KSPField]
        //public string resourceDeltaV = "LqdHydrogen";

        [KSPField]
        public string propellant1 = "LqdHydrogen";
        [KSPField]
        public string propellant2;
        [KSPField]
        public string propellant3;
        [KSPField]
        public string propellant4;

        [KSPField]
        public double ratio1 = 1;
        [KSPField]
        public double ratio2;
        [KSPField]
        public double ratio3;
        [KSPField]
        public double ratio4;

        //[KSPField(guiActive = false, guiName = "Calc Flow")]
        //public double calcualtedFlow;

        [KSPField(guiActive = false, guiName = "Mass Flow")]
        public double requestedFlow;

        // Numeric display values
        protected double thrust_d = 0;
        protected double isp_d = 0;
        protected double throttle_d = 0;

        // Persistent values to use during timewarp
        float IspPersistent = 0;
        float ThrustPersistent = 0;
        float ThrottlePersistent = 0;
        float previousThrottle = 0;

        private double fuelWithMassPercentage1;
        private double fuelWithMassPercentage2;
        private double fuelWithMassPercentage3;
        private double fuelWithMassPercentage4;

        private double masslessFuelPercentage1;
        private double masslessFuelPercentage2;
        private double masslessFuelPercentage3;
        private double masslessFuelPercentage4;

        PartResourceDefinition propellantResourceDefinition1;
        PartResourceDefinition propellantResourceDefinition2;
        PartResourceDefinition propellantResourceDefinition3;
        PartResourceDefinition propellantResourceDefinition4;

        // Are we transitioning from timewarp to reatime?
        bool _warpToReal = false;

        // Density of resource
        private double averageDensityForPopellantWithMass;
        private double massPropellantRatio;


        // Update
        public override void OnUpdate()
        {
            // When transitioning from timewarp to real update throttle
            if (_warpToReal)
            {
                vessel.ctrlState.mainThrottle = ThrottlePersistent;
                _warpToReal = false;
            }

            // Persistent thrust GUI
            Fields["Thrust"].guiActive = isEnabled;
            Fields["Isp"].guiActive = isEnabled;
            Fields["Throttle"].guiActive = isEnabled;

            // Update display values
            Thrust = FormatThrust(thrust_d);
            Isp = Math.Round(isp_d, 2) + " s";
            Throttle = Math.Round(throttle_d * 100) + "%";

            if (IsForceActivated || !isEnabled || !isOperational) return;

            IsForceActivated = true;
            part.force_activate();
        }

        private void UpdateFuelFactors()
        {
            propellantResourceDefinition1 = !String.IsNullOrEmpty(propellant1) ? PartResourceLibrary.Instance.GetDefinition(propellant1) : null;
            propellantResourceDefinition2 = !String.IsNullOrEmpty(propellant2) ? PartResourceLibrary.Instance.GetDefinition(propellant2) : null;
            propellantResourceDefinition3 = !String.IsNullOrEmpty(propellant3) ? PartResourceLibrary.Instance.GetDefinition(propellant3) : null;
            propellantResourceDefinition4 = !String.IsNullOrEmpty(propellant4) ? PartResourceLibrary.Instance.GetDefinition(propellant4) : null;

            var ratioSumOveral = 0.0;
            var ratioSumWithMass = 0.0;
            var densitySum = 0.0;

            if (propellantResourceDefinition1 != null)
            {
                ratioSumOveral += ratio1;
                if (propellantResourceDefinition1.density > 0)
                {
                    ratioSumWithMass = ratio1;
                    densitySum += propellantResourceDefinition1.density * ratio1;
                }
            }
            if (propellantResourceDefinition2 != null)
            {
                ratioSumOveral += ratio2;
                if (propellantResourceDefinition2.density > 0)
                {
                    ratioSumWithMass = ratio2;
                    densitySum += propellantResourceDefinition2.density * ratio2;
                }
            }
            if (propellantResourceDefinition3 != null)
            {
                ratioSumOveral += ratio3;
                if (propellantResourceDefinition3.density > 0)
                {
                    ratioSumWithMass = ratio3;
                    densitySum += propellantResourceDefinition3.density * ratio3;
                }
            }
            if (propellantResourceDefinition4 != null)
            {
                ratioSumOveral += ratio4;
                if (propellantResourceDefinition4.density > 0)
                {
                    ratioSumWithMass = ratio4;
                    densitySum += propellantResourceDefinition4.density * ratio4;
                }
            }
            
            averageDensityForPopellantWithMass = densitySum / ratioSumWithMass;
            massPropellantRatio = ratioSumWithMass / ratioSumOveral;
            var ratioSumWithoutMass = ratioSumOveral - ratioSumWithMass;

            fuelWithMassPercentage1 = propellantResourceDefinition1 != null && propellantResourceDefinition1.density > 0 ? ratio1 / ratioSumWithMass : 0;
            fuelWithMassPercentage2 = propellantResourceDefinition2 != null && propellantResourceDefinition2.density > 0 ? ratio2 / ratioSumWithMass : 0;
            fuelWithMassPercentage3 = propellantResourceDefinition3 != null && propellantResourceDefinition3.density > 0 ? ratio3 / ratioSumWithMass : 0;
            fuelWithMassPercentage4 = propellantResourceDefinition4 != null && propellantResourceDefinition4.density > 0 ? ratio4 / ratioSumWithMass : 0;

            masslessFuelPercentage1 = propellantResourceDefinition1 != null && propellantResourceDefinition1.density <= 0 ? ratio1 / ratioSumWithoutMass : 0;
            masslessFuelPercentage2 = propellantResourceDefinition2 != null && propellantResourceDefinition2.density <= 0 ? ratio2 / ratioSumWithoutMass : 0;
            masslessFuelPercentage3 = propellantResourceDefinition3 != null && propellantResourceDefinition3.density <= 0 ? ratio3 / ratioSumWithoutMass : 0;
            masslessFuelPercentage4 = propellantResourceDefinition4 != null && propellantResourceDefinition4.density <= 0 ? ratio4 / ratioSumWithoutMass : 0;
        }

        private double CollectFuel(double demandMass)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            double fuelRequestAmount1 = 0;
            double fuelRequestAmount2 = 0;
            double fuelRequestAmount3 = 0;
            double fuelRequestAmount4 = 0;

            var propellantWithMassNeeded = demandMass / averageDensityForPopellantWithMass;
            var overalAmountNeeded = propellantWithMassNeeded / massPropellantRatio;
            var masslessResourceNeeded = overalAmountNeeded - propellantWithMassNeeded;

            double availableRatio = 1;
            if (fuelWithMassPercentage1 > 0)
            {
                fuelRequestAmount1 = fuelWithMassPercentage1 * propellantWithMassNeeded;
                availableRatio = Math.Min(fuelRequestAmount1 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition1), availableRatio);
            }
            else if (masslessFuelPercentage1 > 0)
            {
                fuelRequestAmount1 = masslessFuelPercentage1 * masslessResourceNeeded;
                availableRatio = Math.Min(fuelRequestAmount1 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition1), availableRatio);
            }

            if (fuelWithMassPercentage2 > 0)
            {
                fuelRequestAmount2 = fuelWithMassPercentage2 * propellantWithMassNeeded;
                availableRatio = Math.Min(fuelRequestAmount2 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition2), availableRatio);
            }
            else if (masslessFuelPercentage2 > 0)
            {
                fuelRequestAmount2 = masslessFuelPercentage2 * masslessResourceNeeded;
                availableRatio = Math.Min(fuelRequestAmount1 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition2), availableRatio);
            }

            if (fuelWithMassPercentage3 > 0)
            {
                fuelRequestAmount3 = fuelWithMassPercentage3 * propellantWithMassNeeded;
                availableRatio = Math.Min(fuelRequestAmount3 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition3), availableRatio);
            }
            else if (masslessFuelPercentage3 > 0)
            {
                fuelRequestAmount3 = masslessFuelPercentage3 * masslessResourceNeeded;
                availableRatio = Math.Min(fuelRequestAmount1 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition3), availableRatio);
            }

            if (fuelWithMassPercentage4 > 0)
            {
                fuelRequestAmount4 = fuelWithMassPercentage4 * propellantWithMassNeeded;
                availableRatio = Math.Min(fuelRequestAmount4 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition4), availableRatio);
            }
            else if (masslessFuelPercentage4 > 0)
            {
                fuelRequestAmount4 = masslessFuelPercentage4 * masslessResourceNeeded;
                availableRatio = Math.Min(fuelRequestAmount1 / part.GetResourceAvailable(ResourceFlowMode.STACK_PRIORITY_SEARCH, propellantResourceDefinition4), availableRatio);
            }

            if (availableRatio <= float.Epsilon)
                return 0;

            double recievedRatio = 1;
            if (masslessFuelPercentage1 > 0)
            {
                recievedFusionFuel1 = part.RequestResource(propellantResourceDefinition1.id, fuelRequestAmount1 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                recievedRatio = Math.Min(recievedRatio, fuelRequestAmount1 > 0 ? recievedFusionFuel1 / fuelRequestAmount1 : 0);
            }
            if (masslessFuelPercentage2 > 0)
            {
                recievedFusionFuel2 = part.RequestResource(propellantResourceDefinition2.id, fuelRequestAmount2 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                recievedRatio = Math.Min(recievedRatio, fuelRequestAmount2 > 0 ? recievedFusionFuel2 / fuelRequestAmount2 : 0);
            }
            if (masslessFuelPercentage3 > 0)
            {
                recievedFusionFuel3 = part.RequestResource(propellantResourceDefinition3.id, fuelRequestAmount3 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                recievedRatio = Math.Min(recievedRatio, fuelRequestAmount3 > 0 ? recievedFusionFuel3 / fuelRequestAmount3 : 0);
            }
            if (masslessFuelPercentage4 > 0)
            {
                recievedFusionFuel4 = part.RequestResource(propellantResourceDefinition4.id, fuelRequestAmount4 * availableRatio, ResourceFlowMode.STACK_PRIORITY_SEARCH);
                recievedRatio = Math.Min(recievedRatio, fuelRequestAmount4 > 0 ? recievedFusionFuel4 / fuelRequestAmount4 : 0);
            }

            return recievedRatio;
        }

        // Initialization
        //public override void OnLoad(ConfigNode node)
        //{
        //	// Run base OnLoad method
        //	base.OnLoad(node);

        //	//UpdateDensity();
        //}

        // Physics update
        public override void OnFixedUpdate()
        {
            if (FlightGlobals.fetch == null || !isEnabled) return;

            UpdateFuelFactors();

            //UpdateDensity();

            // Realtime mode
            if (!vessel.packed)
            {
                TimeWarp.GThreshold = 2;

                //double mdot = requestedMassFlow;
                requestedFlow = this.requestedMassFlow;
                //calcualtedFlow = ThrustPersistent / (IspPersistent * 9.81); // Mass burn rate of engine
                //var dm = requestedFlow * TimeWarp.fixedDeltaTime; 
                //propellantUsed = dm / _density1; // Resource demand

                // if not transitioning from warp to real
                // Update values to use during timewarp
                if (!_warpToReal) //&& vessel.ctrlState.mainThrottle == previousThrottle)
                {
                    IspPersistent = realIsp;
                    ThrottlePersistent = vessel.ctrlState.mainThrottle;

                    this.CalculateThrust();
                    // verify we have thrust
                    if ((vessel.ctrlState.mainThrottle > 0 && finalThrust > 0) || (vessel.ctrlState.mainThrottle == 0 && finalThrust == 0))
                        ThrustPersistent = finalThrust;
                }
            }
            else //if (part.vessel.situation != Vessel.Situations.SUB_ORBITAL)
            {
                // Timewarp mode: perturb orbit using thrust
                _warpToReal = true; // Set to true for transition to realtime
                var universalTime = Planetarium.GetUniversalTime(); // Universal time

                requestedFlow = this.requestedMassFlow;
                //calcualtedFlow = ThrustPersistent / (IspPersistent * 9.81); // Mass burn rate of engine
                var demandMass = requestedFlow * TimeWarp.fixedDeltaTime; // Change in mass over dT

                var fuelRatio =	CollectFuel(demandMass);

                // Calculate thrust and deltaV if demand output > 0
                if (fuelRatio > 0)
                {
                    var vesselMass = this.vessel.GetTotalMass(); // Current mass
                    var m1 = vesselMass - demandMass; // Mass at end of burn
                    var deltaV = IspPersistent * PluginHelper.GravityConstant * Math.Log(vesselMass / m1); // Delta V from burn

                    Vector3d thrustV = this.part.transform.up; // Thrust direction
                    var deltaVV = deltaV * thrustV; // DeltaV vector
                    vessel.orbit.Perturb(deltaVV, universalTime); // Update vessel orbit
                }
                // Otherwise, if throttle is turned on, and demand out is 0, show warning
                    
                else if (ThrottlePersistent > 0)
                {
                    Debug.Log("Propellant depleted");
                }
            }
            

            // Update display numbers
            thrust_d = ThrustPersistent;
            isp_d = IspPersistent;
            throttle_d = ThrottlePersistent;
            previousThrottle = vessel.ctrlState.mainThrottle;
        }

        //private void UpdateDensity()
        //{
        //	// Initialize density of propellant used in deltaV and mass calculations
        //	var definition = PartResourceLibrary.Instance.GetDefinition(propellant1);
        //	if (definition != null)
        //		_density1 = PartResourceLibrary.Instance.GetDefinition(propellant1).density;
        //}

        // Format thrust into mN, N, kN
        public static string FormatThrust(double thrust)
        {
            if (thrust < 0.001)
                return Math.Round(thrust * 1000000.0, 3) + " mN";
            else if (thrust < 1.0)
                return Math.Round(thrust * 1000.0, 3) + " N";
            else
                return Math.Round(thrust, 3) + " kN";
        }
    }

}