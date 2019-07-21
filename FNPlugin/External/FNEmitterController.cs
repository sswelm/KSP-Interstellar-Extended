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
        [KSPField(isPersistant = true, guiName = "Fuel Neutrons Fraction")]
        public double fuelNeutronsFraction = 0.02;
        [KSPField(isPersistant = true)]
        public double lithiumNeutronAbsorbtionFraction;
        [KSPField(isPersistant = true)]
        public double exhaustActivityFraction;
        [KSPField(isPersistant = true)]
        public double radioactiveFuelLeakFraction;
        [KSPField(isPersistant = true)]
        public bool exhaustProducesNeutronRadiation = false;
        [KSPField(isPersistant = true)]
        public bool exhaustProducesGammaRadiation = false;

        //Setting
        [KSPField(guiActiveEditor = true, guiName = "Max Gamma Radiation")]
        public double maxRadiation = 0.02;
        [KSPField]
        public double neutronsExhaustRadiationMult = 1;
        [KSPField]
        public double gammaRayExhaustRadiationMult = 0.5;
        [KSPField]
        public double neutronScatteringRadiationMult = 20;
        [KSPField]
        public double diameter = 1;
        [KSPField]
        public double height = 0;
        [KSPField]
        public double habitatMassMultiplier = 20;
        [KSPField]
        public double reactorMassMultiplier = 10; 

        // Gui
        [KSPField(guiActive = false, guiName = "Distance Radiation Modifier", guiFormat = "F5")]
        public double averageDistanceModifier;
        [KSPField(guiActive = false, guiName = "Average Distance To Crew", guiFormat = "F5")]
        public double averageCrewDistanceToEmitter;
        [KSPField(guiActive = false, guiName = "Average Crew Mass Protection", guiUnits = " g/cm2", guiFormat = "F5")]
        public double averageCrewMassProtection;
        [KSPField(guiActive = false, guiName = "Reactor Shadow Mass Protection")]
        public double reactorShadowShieldMassProtection;
        [KSPField(guiActive = false, guiName = "reactor Lead Shielding Thickness", guiUnits = " cm", guiFormat = "F5")]
        public double reactorLeadShieldingThickness;
        [KSPField(guiActive = false, guiName = "Average Habitat Lead Thickness", guiUnits = " cm", guiFormat = "F5")]
        public double averageHabitatLeadEquivalantThickness;
        [KSPField(guiActive = false, guiName = "Reactor Shadow Shield Lead Thickness", guiUnits = " cm", guiFormat = "F5")]
        public double reactorShadowShieldLeadThickness;
        [KSPField(guiActive = false, guiName = "Reactor GammaRays Attenuation", guiFormat = "F5")]
        public double reactorShieldingGammaAttenuation;
        [KSPField(guiActive = false, guiName = "Reactor Neutron Attenuation", guiFormat = "F5")]
        public double reactorShieldingNeutronAttenuation;
        [KSPField(guiActive = false, guiName = "Average GammaRays Attenuation", guiFormat = "F5")]
        public double averageHabitatLeadGammaAttenuation;
        [KSPField(guiActive = false, guiName = "Average Neutron Attenuation", guiFormat = "F5")]
        public double averageHabitaNeutronAttenuation;
        [KSPField(guiActive = false, guiName = "Emitter Radiation Rate")]
        public double emitterRadiationRate;
        [KSPField(guiActive = false, guiName = "Gamma Transparency")]
        public double gammaTransparency;

        // Output
        [KSPField(guiActive = false, guiName = "Reactor Core Neutron Radiation")]
        public double reactorCoreNeutronRadiation;
        [KSPField(guiActive = false, guiName = "Reactor Core Gamma Radiation")]
        public double reactorCoreGammaRadiation;
        [KSPField(guiActive = false, guiName = "Lost Fission Fuel Radiation")]
        public double lostFissionFuelRadiation;
        [KSPField(guiActive = false, guiName = "Fission Exhaust Radiation")]
        public double fissionExhaustRadiation;
        [KSPField(guiActive = false, guiName = "Fission Fragment Radiation")]
        public double fissionFragmentRadiation;

        // Privates
        PartModule emitterModule;
        BaseField emitterRadiationField;
        PartResource shieldingPartResource;

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
            if (Kerbalism.versionMajor == 0)
                return;

            shieldingPartResource = part.Resources["Shielding"];
            if (shieldingPartResource != null)
            {
                var radius = diameter * 0.5;
                if (height == 0)
                    height = diameter;

                var ratio = shieldingPartResource.amount / shieldingPartResource.maxAmount;
                shieldingPartResource.maxAmount = (2 * Math.PI * radius * radius) + (2 * Math.PI * radius * height);    // 2 π r2 + 2 π r h 
                shieldingPartResource.amount = shieldingPartResource.maxAmount * ratio;
            }

            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            bool found = false;
            foreach (PartModule module in part.Modules)
            {
                if (module.moduleName == "Emitter")
                {
                    emitterModule = module;

                    emitterRadiationField = module.Fields["radiation"];

                    found = true;
                    break;
                }
            }

            if (found)
                UnityEngine.Debug.Log("[KSPI]: FNEmitterController Found Emitter");
            else
                UnityEngine.Debug.LogWarning("[KSPI]: FNEmitterController failed to find Emitter");
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

                totalDistancePart += distanceToPart * partCrewCount / diameter;

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

            reactorLeadShieldingThickness = shieldingPartResource != null ? (shieldingPartResource.info.density / 0.2268) * 20 * shieldingPartResource.amount / shieldingPartResource.maxAmount : 0;
            averageHabitatLeadEquivalantThickness = habitatMassMultiplier * averageCrewMassProtection / 0.2268;
            reactorShadowShieldLeadThickness = reactorMassMultiplier * reactorShadowShieldMassProtection;

            reactorShieldingGammaAttenuation = Math.Pow(1 - 0.9, reactorLeadShieldingThickness / 5);
            reactorShieldingNeutronAttenuation = Math.Pow(1 - 0.5, reactorLeadShieldingThickness / 6.8);

            averageHabitatLeadGammaAttenuation = Math.Pow(1 - 0.9, (averageHabitatLeadEquivalantThickness + reactorShadowShieldLeadThickness) / 5);
            averageHabitaNeutronAttenuation = Math.Pow(1 - 0.5, averageHabitatLeadEquivalantThickness / 6.8);

            gammaTransparency = Kerbalism.HasRadiationFixes ? 1 : Kerbalism.GammaTransparency(vessel.mainBody, vessel.altitude);

            averageDistanceModifier = 1 / (averageCrewDistanceToEmitter * averageCrewDistanceToEmitter);

            var averageDistanceGammaRayShieldingAttenuation = averageDistanceModifier * averageHabitatLeadGammaAttenuation;
            var averageDistanceNeutronShieldingAttenuation = averageDistanceModifier * averageHabitaNeutronAttenuation;

            var maxCoreRadiation = maxRadiation * reactorActivityFraction;

            reactorCoreGammaRadiation = maxCoreRadiation * averageDistanceGammaRayShieldingAttenuation * reactorShieldingGammaAttenuation;
            reactorCoreNeutronRadiation = maxCoreRadiation * averageDistanceNeutronShieldingAttenuation * reactorShieldingNeutronAttenuation * part.atmDensity * fuelNeutronsFraction * neutronScatteringRadiationMult * Math.Max(0, 1 - lithiumNeutronAbsorbtionFraction);

            var maxEngineRadiation = maxRadiation * exhaustActivityFraction;

            lostFissionFuelRadiation = maxEngineRadiation * averageDistanceNeutronShieldingAttenuation * (1 + part.atmDensity) * radioactiveFuelLeakFraction * neutronsExhaustRadiationMult;
            fissionExhaustRadiation = maxEngineRadiation * averageDistanceNeutronShieldingAttenuation * (1 + part.atmDensity) * (exhaustProducesNeutronRadiation ? neutronsExhaustRadiationMult : 0);
            fissionFragmentRadiation = maxEngineRadiation * averageDistanceGammaRayShieldingAttenuation * (exhaustProducesGammaRadiation ? gammaRayExhaustRadiationMult : 0);

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
