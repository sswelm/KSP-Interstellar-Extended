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
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Wavelength")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedBeamConfiguration = 0;

        [KSPField(isPersistant = true)]
        public bool isInitialized = false;


        [KSPField(isPersistant = false)]
        public bool canSwitchWavelengthInFlight = true;
        [KSPField(isPersistant = false)]
        public bool isLoaded = false;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Generator Type")]
        public string beamTypeName = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false)]
        public int beamType = 1;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Wavelength Name")]
        public string beamWaveName = "";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Wavelength Length", guiFormat = "F9", guiUnits = " m")]
        public double wavelength;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Atmospheric Absorption", guiFormat = "F3", guiUnits = "%")]
        public double atmosphericAbsorptionPercentage = 10;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Water Absorption", guiFormat = "F3", guiUnits = "%")]
        public double waterAbsorptionPercentage = 10;
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


        private BeamConfiguration activeConfiguration;

        public BeamConfiguration ActiveConfiguration
        {
            get { return activeConfiguration; }
        }

        private IList<BeamConfiguration> beamConfigurations;

        public IList<BeamConfiguration> BeamConfigurations 
        {
            get
            {
                if (beamConfigurations == null)
                {
                    beamConfigurations = part.FindModulesImplementing<BeamConfiguration>()
                        .Where(m => PluginHelper.HasTechRequirementOrEmpty(m.techRequirement0))
                        .OrderByDescending(m => m.wavelength).ToList();
                }
                return beamConfigurations;
            }
        }

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
                Debug.LogError("[KSPI] - BeamGenerator.OnRescale" + e.Message);
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

            InitializeWavelengthSelector();
        }

        private void InitializeWavelengthSelector()
        {
            Debug.Log("[KSP Interstellar] Setup Transmit Beams Configurations for " + part.partInfo.title);

            var chooseField = Fields["selectedBeamConfiguration"];
            var chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            var chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            chooseField.guiActive = canSwitchWavelengthInFlight;

            var names = BeamConfigurations.Select(m => m.beamWaveName).ToArray();

            chooseOptionEditor.options = names;
            chooseOptionFlight.options = names;

            UpdateFromGUI(chooseField, selectedBeamConfiguration);

            // connect on change event
            chooseOptionEditor.onFieldChanged = UpdateFromGUI;
            chooseOptionFlight.onFieldChanged = UpdateFromGUI;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            //Debug.Log("[KSP Interstellar] UpdateFromGUI is called with " + selectedBeamConfiguration);

            if (!BeamConfigurations.Any())
            {
                //Debug.Log("[KSP Interstellar] UpdateFromGUI no BeamConfigurations found");
                return;
            }

            if (isLoaded == false)
                LoadInitialConfiguration();
            else
            {
                if (selectedBeamConfiguration < BeamConfigurations.Count)
                {
                    //Debug.Log("[KSP Interstellar] UpdateFromGUI " + selectedBeamConfiguration + " < orderedBeamGenerators.Count");
                    activeConfiguration = BeamConfigurations[selectedBeamConfiguration];
                }
                else
                {
                    //Debug.Log("[KSP Interstellar] UpdateFromGUI " + selectedBeamConfiguration + " >= orderedBeamGenerators.Count");
                    selectedBeamConfiguration = BeamConfigurations.Count - 1;
                    activeConfiguration = BeamConfigurations.Last();
                }
            }

            if (activeConfiguration == null)
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI no activeConfiguration found");
                return;
            }

            beamWaveName = activeConfiguration.beamWaveName;
            //Debug.Log("[KSP Interstellar] UpdateFromGUI set beamWaveName to " + beamWaveName);
            
            wavelength = activeConfiguration.wavelength;
            //Debug.Log("[KSP Interstellar] UpdateFromGUI set wavelenth to " + wavelength);
            
            atmosphericAbsorptionPercentage = activeConfiguration.atmosphericAbsorptionPercentage;
            //Debug.Log("[KSP Interstellar] UpdateFromGUI set atmosphericAbsorptionPercentage to " + atmosphericAbsorptionPercentage);

            waterAbsorptionPercentage = activeConfiguration.waterAbsorptionPercentage;
            //Debug.Log("[KSP Interstellar] UpdateFromGUI set waterAbsorptionPercentage to " + waterAbsorptionPercentage);

            UpdateEfficiencyPercentage();
        }

        private void UpdateEfficiencyPercentage()
        {
            //Debug.Log("[KSP Interstellar] UpdateFromGUI UpdateEfficiencyPercentage");

            var techLevel = -1;

            if (PluginHelper.HasTechRequirementAndNotEmpty(activeConfiguration.techRequirement3))
                techLevel++;
            if (PluginHelper.HasTechRequirementAndNotEmpty(activeConfiguration.techRequirement2))
                techLevel++;
            if (PluginHelper.HasTechRequirementAndNotEmpty(activeConfiguration.techRequirement1))
                techLevel++;
            if (PluginHelper.HasTechRequirementAndNotEmpty(activeConfiguration.techRequirement0))
                techLevel++;

            if (techLevel == 3)
                efficiencyPercentage = activeConfiguration.efficiencyPercentage3;
            else if (techLevel == 2)
                efficiencyPercentage = activeConfiguration.efficiencyPercentage2;
            else if (techLevel == 1)
                efficiencyPercentage = activeConfiguration.efficiencyPercentage1;
            else if (techLevel == 0)
                efficiencyPercentage = activeConfiguration.efficiencyPercentage0;
            else
                efficiencyPercentage = 0;
        }

        private void LoadInitialConfiguration()
        {
            isLoaded = true;

            var currentWavelength = wavelength != 0 ? wavelength : 1;

            //Debug.Log("[KSP Interstellar] UpdateFromGUI initialize initial beam configuration with wavelength target " + currentWavelength);

            // find wavelength closes to target wavelength
            activeConfiguration = BeamConfigurations.FirstOrDefault();
            selectedBeamConfiguration = 0;
            var lowestWavelengthDifference = Math.Abs(currentWavelength - activeConfiguration.wavelength);
            if (BeamConfigurations.Count > 1)
            {
                foreach (var currentConfig in BeamConfigurations)
                {
                    var configWaveLengthDifference = Math.Abs(currentWavelength - currentConfig.wavelength);
                    if (configWaveLengthDifference < lowestWavelengthDifference)
                    {
                        activeConfiguration = currentConfig;
                        lowestWavelengthDifference = configWaveLengthDifference;
                        selectedBeamConfiguration = BeamConfigurations.IndexOf(currentConfig);
                    }
                }
            }
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
            //info.AppendLine("wavelength: " + wavelength + "m");
            //info.AppendLine("atmospheric Absorption: " + atmosphericAbsorptionPercentage + "%");
            //info.AppendLine("Power to Beam Efficiency: " + efficiencyPercentage + "%");

            return info.ToString();
        }
    }
}
