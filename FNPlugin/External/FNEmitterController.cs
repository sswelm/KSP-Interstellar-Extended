using System;
using System.Linq;
using FNPlugin.Storage;
using UnityEngine;

namespace FNPlugin.External
{
    public class FNEmitterController:  PartModule
    {
        // Persistant input
        [KSPField(isPersistant = true)]
        public double reactorActivityFraction;
        [KSPField(isPersistant = true)]
        public double fuelNeutronsFraction;
        [KSPField(isPersistant = true)]
        public double lithiumNeutronAbsorbtionFraction;
        [KSPField(isPersistant = true)]
        public double exhaustActivityFraction;
        [KSPField(isPersistant = true)]
        public double radioavtiveFuelLeakFraction;
        [KSPField(isPersistant = true)]
        public bool exhaustProducesNeutronRadiation = false;
        [KSPField(isPersistant = true)]
        public bool exhaustProducesGammaRadiation = false;

        //Setting
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "Max Gamma Radiation")]
        public double maxRadiation = 0.02;
        [KSPField]
        public double neutronsExhaustRadiationMult = 16;
        [KSPField]
        public double gammaRayExhaustRadiationMult = 4;
        [KSPField]
        public double neutronScatteringRadiationMult = 20;
        [KSPField]
        public double radius = 1;

        // Gui
        [KSPField(guiActive = false, guiName = "Distance Radiation Modifier", guiFormat = "F5")]
        public double averageDistanceModifier;
        [KSPField(guiActive = false, guiName = "Average Distance To Crew", guiFormat = "F5")]
        public double averageCrewDistanceToEmitter;
        [KSPField(guiActive = false, guiName = "Average Crew Mass Protection", guiUnits = " g/cm2", guiFormat = "F5")]
        public double averageCrewMassProtection;
        [KSPField(guiActive = false, guiName = "Average Lead Equivalant Thickness", guiUnits = " cm", guiFormat = "F5")]
        public double averageCrewLeadEquivalantThickness;
        [KSPField(guiActive = false, guiName = "Average GammaRays Attenuation", guiFormat = "F5")]
        public double averageLeadGammaAttenuation;
        [KSPField(guiActive = false, guiName = "Average Neutron Attenuation", guiFormat = "F5")]
        public double averageNeutronAttenuation;
        [KSPField(guiActive = false, guiName = "Emitter Radiation Rate")]
        public double emitterRadiationRate;
        [KSPField(guiActive = false, guiName = "Gamma Transparency")]
        public double gammaTransparency;

        // Output
        [KSPField]
        public double reactorCoreNeutronRadiation;
        [KSPField]
        public double reactorCoreGammaRadiation;
        [KSPField]
        public double lostFissionFuelRadiation;
        [KSPField]
        public double fissionExhaustRadiation;
        [KSPField]
        public double fissionFragmentRadiation;

        // Privates
        PartModule emitterModule;
        BaseField emitterRadiationField;

        public override void OnStart(PartModule.StartState state)
        {
            InitializeKerbalismEmitter();
        }

        public virtual void Update()
        {
            UpdateKerbalismEmitter();
        }

        private void InitializeKerbalismEmitter()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            bool found = false;

            foreach (PartModule module in part.Modules)
            {
                if (module.moduleName == "Emitter")
                {
                    emitterModule = module;

                    emitterRadiationField = module.Fields["radiation"];
                    if (emitterRadiationField != null)
                        emitterRadiationField.SetValue(maxRadiation * reactorActivityFraction, emitterModule);

                    found = true;
                    break;
                }
            }

            if (found)
                UnityEngine.Debug.Log("[KSPI]: FNEmitterController Found Emitter");
            else
                UnityEngine.Debug.LogError("[KSPI]: FNEmitterController failed to find Emitter");
        }

        private void UpdateKerbalismEmitter()
        {
            if (emitterModule == null)
                return;

            if (emitterRadiationField == null)
                return;

            if (maxRadiation == 0)
                return;

            int totalCrew = vessel.GetCrewCount();
            if (totalCrew == 0)
                return;

            double totalDistancePart = 0;
            double totalCrewMassShielding = 0;

            Vector3 reactorPosition = part.transform.position;

            foreach (Part partWithCrew in vessel.parts.Where(m => m.protoModuleCrew.Count > 0))
            {
                int partCrewCount = partWithCrew.protoModuleCrew.Count;

                double distanceToPart = (reactorPosition - partWithCrew.transform.position).magnitude;

                totalDistancePart += distanceToPart * partCrewCount / radius;

                var habitat = partWithCrew.FindModuleImplementing<KerbalismHabitatController>();
                if (habitat != null)
                {
                    var habitatSurface = habitat.Surface;
                    if (habitatSurface > 0)
                        totalCrewMassShielding = (partWithCrew.resourceMass / habitatSurface) * partCrewCount;
                }
            }

            averageCrewMassProtection = Math.Max(0, totalCrewMassShielding / totalCrew);
            averageCrewDistanceToEmitter = Math.Max(1, totalDistancePart / totalCrew);
            averageCrewLeadEquivalantThickness = 20 * (averageCrewMassProtection / 0.2268);

            averageLeadGammaAttenuation = Math.Pow(1 - 0.9, averageCrewLeadEquivalantThickness / 5);
            averageNeutronAttenuation = Math.Pow(1 - 0.5, averageCrewLeadEquivalantThickness / 6.8);

            gammaTransparency = Kerbalism.GammaTransparency(vessel.mainBody, vessel.altitude);

            averageDistanceModifier = 1 / (averageCrewDistanceToEmitter * averageCrewDistanceToEmitter);

            var averageDistanceGammaRayShieldingAttenuation = averageDistanceModifier * averageLeadGammaAttenuation;
            var averageDistanceNeutronShieldingAttenuation = averageDistanceModifier * averageNeutronAttenuation;

            var maxCoreRadiation = maxRadiation * reactorActivityFraction;

            reactorCoreGammaRadiation = maxCoreRadiation * averageDistanceGammaRayShieldingAttenuation;
            reactorCoreNeutronRadiation = maxCoreRadiation * averageDistanceNeutronShieldingAttenuation * part.atmDensity * fuelNeutronsFraction * neutronScatteringRadiationMult * Math.Max(0, 1 - lithiumNeutronAbsorbtionFraction);

            var maxEngineRadiation = maxRadiation * exhaustActivityFraction;

            lostFissionFuelRadiation = maxEngineRadiation * averageDistanceNeutronShieldingAttenuation * (1 + part.atmDensity) * radioavtiveFuelLeakFraction * neutronsExhaustRadiationMult;
            fissionExhaustRadiation = maxEngineRadiation * averageDistanceNeutronShieldingAttenuation * (1 + part.atmDensity) * (exhaustProducesNeutronRadiation ? 0 : neutronsExhaustRadiationMult);
            fissionFragmentRadiation = maxEngineRadiation * averageDistanceGammaRayShieldingAttenuation * (exhaustProducesGammaRadiation ? 0 : gammaRayExhaustRadiationMult);

            emitterRadiationRate = 0;
            emitterRadiationRate += reactorCoreGammaRadiation;
            emitterRadiationRate += reactorCoreNeutronRadiation;
            emitterRadiationRate += lostFissionFuelRadiation;
            emitterRadiationRate += fissionExhaustRadiation;
            emitterRadiationRate += fissionFragmentRadiation;

            emitterRadiationRate = gammaTransparency > 0 ? emitterRadiationRate / gammaTransparency : 0;

            SetRadiation(emitterRadiationRate);
        }

        public void SetRadiation(double radiation)
        {
            if (emitterRadiationField == null || emitterModule == null)
                return;

            if (double.IsInfinity(radiation) || double.IsNaN(radiation))
            {
                Debug.LogError("[KSPI]: InterstellarReactor emitterRadiationRate = " + radiation);
                return;
            }

            emitterRadiationField.SetValue(radiation, emitterModule);
        }
    }
}
