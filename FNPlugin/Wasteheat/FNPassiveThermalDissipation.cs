using System;

namespace FNPlugin.Wasteheat
{
    class FNPassiveThermalDissipation: PartModule
    {
        // configuration
        [KSPField(guiActiveEditor = true, guiActive = false, guiName = "Deployed Surface Area", guiUnits = " m\xB2", guiFormat = "F3")]
        public double deployedSurfaceArea = 0;
        [KSPField(guiActiveEditor = true, guiActive = false, guiName = "Folded Surface Area", guiUnits = " m\xB2", guiFormat = "F3")]
        public double foldedSurfaceArea = 0;
        [KSPField]
        public double thermalMassModifier = 1;
        [KSPField] 
        public double emissiveConstantFront = 0.025;
        [KSPField]
        public double emissiveConstantBack = 0.1;
        [KSPField]
        public double minimumAngle = 0.025;

        // state
        [KSPField(isPersistant = true)]
        public double storedPartTemperature;
        [KSPField(isPersistant = true)]
        public double storedPartSkinTemperature;
        [KSPField(isPersistant = true)]
        public bool isInitialized;

        // GUI
        [KSPField(guiActive = true, guiName = "Heat Dissipation", guiUnits = " MW", guiFormat = "F3")]//Dissipation
        public double dissipationInMegaJoules;
        [KSPField(guiActive = true, guiName = "Heat Absorption", guiUnits = " MW", guiFormat = "F3")] //Absorption
        public double deltaEnergyIncreaseInMegajoules;
        [KSPField(guiActive = true, guiName = "Stock SolarFlux", guiFormat = "F1")]//Solar Flux
        public double stockSolarFlux;
        [KSPField(guiActive = true, guiName = "Simulated SolarFlux", guiFormat = "F1")]//Solar Flux
        public double simulatedSolarFlux;
        [KSPField(guiActive = false, guiFormat = "F0", guiUnits = " m")]
        public double realDistanceToSun;
        [KSPField(guiActive = false, guiFormat = "F0", guiUnits = " m")]
        public double distanceFromStarCenterToVessel;
        [KSPField(guiActive = true, guiName = "Cosine Factor", guiFormat = "F4")] 
        public double cosAngle;

        // session variables
        private int _countDown;
        private double _thermalMassPerKilogram;
        private ModuleDeployableSolarPanel _deployableSolarPanel;

        public double SurfaceArea
        {
            get
            {
                if (_deployableSolarPanel == null)
                    return Math.Max(deployedSurfaceArea, foldedSurfaceArea);

                return _deployableSolarPanel.deployState == ModuleDeployablePart.DeployState.EXTENDED ? deployedSurfaceArea : foldedSurfaceArea;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor)
                return;

            _countDown = 50;
            part.thermalMassModifier = thermalMassModifier;
            _deployableSolarPanel = part.FindModuleImplementing<ModuleDeployableSolarPanel>();

            if (isInitialized) return;

            storedPartTemperature = part.temperature;
            storedPartSkinTemperature = part.skinTemperature;

            isInitialized = true;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            MaintainPartTemperatureAtStartup();

            if (!(SurfaceArea > 0) || !(emissiveConstantFront > 0)) return;

            _thermalMassPerKilogram = part.mass * part.thermalMassModifier * PhysicsGlobals.StandardSpecificHeatCapacity * 1e-3;

            ProcessHeatAbsorption();

            ProcessHeatDissipation();
        }

        private void ProcessHeatDissipation()
        {
            dissipationInMegaJoules = 0;
            // front
            {
                var effectiveSurfaceArea = SurfaceArea * emissiveConstantFront;
                var temperatureDelta = System.Math.Max(0, part.skinTemperature - 4);
                var dissipationFront = PluginHelper.GetBlackBodyDissipation(effectiveSurfaceArea, temperatureDelta) * 1e-6;
                dissipationInMegaJoules += dissipationFront;
                var temperatureChange = TimeWarp.fixedDeltaTime * -(dissipationFront / _thermalMassPerKilogram);

                if (!double.IsNaN(temperatureChange))
                    part.skinTemperature = Math.Max(4, part.skinTemperature + temperatureChange);
            }

            // back
            {
                var effectiveSurfaceArea = SurfaceArea * emissiveConstantBack;
                var temperatureDelta = System.Math.Max(0, part.temperature - 4);
                var dissipationBack =  PluginHelper.GetBlackBodyDissipation(effectiveSurfaceArea, temperatureDelta) * 1e-6;
                dissipationInMegaJoules += dissipationBack;
                var temperatureChange = TimeWarp.fixedDeltaTime * -(dissipationBack / _thermalMassPerKilogram);

                if (!double.IsNaN(temperatureChange))
                    part.temperature = Math.Max(4, part.temperature + temperatureChange);
            }
        }

        private void MaintainPartTemperatureAtStartup()
        {
            if (_countDown > 0)
            {
                part.temperature = storedPartTemperature;
                part.skinTemperature = storedPartSkinTemperature;
                _countDown--;
            }
            else
            {
                storedPartTemperature = part.temperature;
                storedPartSkinTemperature = part.skinTemperature;
            }
        }

        private void ProcessHeatAbsorption()
        {
            stockSolarFlux = vessel.solarFlux;

            var astronomicalUnit = FlightGlobals.GetHomeBody().orbit.semiMajorAxis;

            if (FlightIntegrator.sunBody != null)
            {
                Vector3d scaledSpace = ScaledSpace.LocalToScaledSpace(vessel.transform.position);
                Vector3d position = FlightIntegrator.sunBody.scaledBody.transform.position;
                Vector3d vector3d = position - scaledSpace;
                var distance = vector3d.magnitude;
                distanceFromStarCenterToVessel = distance * ScaledSpace.ScaleFactor;
                cosAngle = Math.Max(minimumAngle, Math.Min(1, Math.Abs(Vector3d.Dot(vector3d.normalized, this.vessel.transform.up))));

                //// normalize vector using distance
                //var sunVector = (Vector3)(vector3d / distance);

                //// create a ray from vessel to sun
                //Ray ray = new Ray(scaledSpace, sunVector);

                //var sunLayerMask = 1 << LayerMask.NameToLayer("Scaled Scenery");
                //if (Physics.Raycast(ray, out var sunBodyFluxHit, float.MaxValue, sunLayerMask))
                //    calculatedDistanceToSun = ScaledSpace.ScaleFactor * sunBodyFluxHit.distance;
                //else
                //    calculatedDistanceToSun = distance * ScaledSpace.ScaleFactor - FlightIntegrator.sunBody.Radius;
            }

            if (FlightIntegrator.ActiveVesselFI != null)
            {
                realDistanceToSun = FlightIntegrator.ActiveVesselFI.realDistanceToSun;
                var starRadius = vessel.mainBody.Radius;

                //var theta = Math.Asin(starRadius / distanceFromStarCenterToVessel);
                //var omega = 2 * Math.PI * (1 - Math.Cos(theta));
                //var distanceInAu = calculatedDistanceToSun / astronomicalUnit;
                //simulatedSolarFlux = solarRadiance * omega;

                var surfaceAreaSun = 4 * Math.PI * starRadius * starRadius;
                var solarRadiance = 4 * astronomicalUnit * astronomicalUnit * PhysicsGlobals.SolarLuminosityAtHome / surfaceAreaSun;

                //classicSolarFlux = solarRadiance * Math.PI * Math.Pow(starRadius / realDistanceToSun, 2);

                var starRadiusToDistanceVessel = starRadius / (distanceFromStarCenterToVessel - (starRadius * 0.7581877534));
                simulatedSolarFlux = solarRadiance * Math.PI * starRadiusToDistanceVessel * starRadiusToDistanceVessel;

                deltaEnergyIncreaseInMegajoules = cosAngle * simulatedSolarFlux * SurfaceArea * emissiveConstantFront * 1e-6;

                if (CheatOptions.IgnoreMaxTemperature)
                    return;

                var deltaTemperatureChange = TimeWarp.fixedDeltaTime * (deltaEnergyIncreaseInMegajoules / _thermalMassPerKilogram);

                if (!double.IsNaN(deltaTemperatureChange))
                    part.skinTemperature = Math.Max(4, part.skinTemperature + deltaTemperatureChange);
            }
        }
    }
}
