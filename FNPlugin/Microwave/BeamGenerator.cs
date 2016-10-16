using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Microwave
{
    [KSPModule("Beam Generator")]
    class BeamGenerator : PartModule, IPartMassModifier, IRescalable<BeamGenerator>
    {
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false)]
        public int beamType = 1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Beam Type")]
        public string beamTypeName = "";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Wavelength", guiFormat = "F9", guiUnits = " m")]
        public double wavelength = 0.003189281;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Atmospheric Absorption", guiFormat = "F4", guiUnits = "%")]
        public double atmosphericAbsorptionPercentage = 10;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Power to Beam Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage = 90;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Stored Mass")]
        public float storedMassMultiplier;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Initial Mass", guiUnits = " t")]
        public float initialMass;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Target Mass", guiUnits = " t")]
        public double targetMass = 1;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Part Mass", guiUnits = " t")]
        public float partMass;

        [KSPField(isPersistant = false)]
        public float powerMassFraction = 0.5f;
        [KSPField(isPersistant = false)]
        public bool fixedMass = false;

        public void Update()
        {
            partMass = part.mass;
        }

        public void UpdateMass(double maximumPower)
        {
            targetMass = maximumPower * powerMassFraction * 0.001;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("BeamGenerator.OnRescale called with " + factor.absolute.linear);
                storedMassMultiplier = Mathf.Pow(factor.absolute.linear, 3);
                initialMass = part.prefabMass * storedMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("BeamGenerator.OnRescale" + e.Message);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            targetMass = part.prefabMass * storedMassMultiplier;
            initialMass = part.prefabMass * storedMassMultiplier;

            if (initialMass == 0)
                initialMass = part.prefabMass;
            if (targetMass == 0)
                targetMass = part.prefabMass;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            var moduleMassDelta = fixedMass ? initialMass : targetMass - initialMass;

            return (float)moduleMassDelta;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Beam type: " + beamTypeName);
            info.AppendLine("wavelength: " + wavelength + "m");
            info.AppendLine("atmospheric Absorption: " + atmosphericAbsorptionPercentage + "%");
            info.AppendLine("Power to Beam Efficiency: " + efficiencyPercentage + "%");

            return info.ToString();
        }
    }
}
