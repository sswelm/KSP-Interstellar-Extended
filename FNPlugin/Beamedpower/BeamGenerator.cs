using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Microwave
{
    [KSPModule("Integrated Beam Generator")]
    class IntegratedBeamGenerator : BeamGenerator { }

    [KSPModule("Beam Generator Module")]
    class BeamGeneratorModule : BeamGenerator { }

    [KSPModule("Beam Generator")]
    class BeamGenerator : PartModule, IPartMassModifier, IRescalable<BeamGenerator>
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Wavelength")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedBeamConfiguration;

        [KSPField(isPersistant = true)]
        public bool isInitialized = false;
        [KSPField(isPersistant = false)]
        public bool canSwitchWavelengthInFlight = true;
        [KSPField(isPersistant = false)]
        public bool isLoaded = false;

        [KSPField(guiActiveEditor = true, guiName = "Generator Type")]
        public string beamTypeName = "";
        [KSPField(guiActiveEditor = true, guiActive = false)]
        public int beamType = 1;
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "Wavelength Name")]
        public string beamWaveName = "";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Wavelength in meter", guiFormat = "F9", guiUnits = " m")]
        public double wavelength;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "WaveLength in SI")]
        public string wavelengthText;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Atmospheric Absorption", guiFormat = "F3", guiUnits = "%")]
        public double atmosphericAbsorptionPercentage = 10;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Water Absorption", guiFormat = "F3", guiUnits = "%")]
        public double waterAbsorptionPercentage = 10;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Power to Beam Efficiency", guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage = 90;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Stored Mass")]
        public double storedMassMultiplier;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Initial Mass", guiUnits = " t")]
        public double initialMass;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Target Mass", guiUnits = " t")]
        public double targetMass = 1;
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Part Mass", guiUnits = " t")]
        public float partMass;
        [KSPField(isPersistant = true)]
        public double maximumPower;

        [KSPField(isPersistant = false)]
        public double powerMassFraction = 0.5;
        [KSPField(isPersistant = false)]
        public bool fixedMass = false;

        private BeamConfiguration activeConfiguration;
        private BaseField chooseField;

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

                    Debug.Log("[KSPI]: Found " + beamConfigurations.Count + " BeamConfigurations");
                }
                return beamConfigurations;
            }
        }

        public IEnumerable<BeamGenerator> FindBeamGenerators(Part origin)
        {
            var attachedParts = part.attachNodes.Where(m => m.attachedPart != null && m.attachedPart != origin);
            var attachedBeanGenerators = attachedParts.Select(m => m.attachedPart.FindModuleImplementing<BeamGenerator>()).Where(m => m != null);

            List<BeamGenerator> indirectBeamGenerators = attachedBeanGenerators.SelectMany(m => m.FindBeamGenerators(m.part)).ToList();
            indirectBeamGenerators.Insert(0, this);
            return indirectBeamGenerators;
        }

        // Note: do note remove, it is called by KSP
        public void Update()
        {
            partMass = part.mass;
        }

        public void UpdateMass(double maximumPower)
        {
            this.maximumPower = maximumPower;
            targetMass = maximumPower * powerMassFraction * 0.001;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("BeamGenerator.OnRescale called with " + factor.absolute.linear);
                storedMassMultiplier = Math.Pow((double)(decimal)factor.absolute.linear, 3);
                initialMass = (double)(decimal)part.prefabMass * storedMassMultiplier;
                if (maximumPower > 0)
                    targetMass = maximumPower * powerMassFraction * 0.001;
                else
                    targetMass = initialMass;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: BeamGenerator.OnRescale" + e.Message);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            targetMass = part.prefabMass * storedMassMultiplier;
            initialMass = part.prefabMass * storedMassMultiplier;

            if (initialMass == 0)
                initialMass = (double)(decimal)part.prefabMass;
            if (targetMass == 0)
                targetMass = (double)(decimal)part.prefabMass;

            InitializeWavelengthSelector();
        }

        private void InitializeWavelengthSelector()
        {
            Debug.Log("[KSPI]: Setup Transmit Beams Configurations for " + part.partInfo.title);

            chooseField = Fields["selectedBeamConfiguration"];
            chooseField.guiActive = CheatOptions.NonStrictAttachmentOrientation || (canSwitchWavelengthInFlight && BeamConfigurations.Count > 1);

            var chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            var chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            var names = BeamConfigurations.Select(m => m.beamWaveName).ToArray();
            chooseOptionEditor.options = names;
            chooseOptionFlight.options = names;

            if (wavelength != 0)
            {
                activeConfiguration = BeamConfigurations.SingleOrDefault(m => m.wavelength == wavelength);
                if (activeConfiguration != null)
                    selectedBeamConfiguration = BeamConfigurations.IndexOf(activeConfiguration);
            }

            UpdateFromGUI(chooseField, selectedBeamConfiguration);

            // connect on change event
            chooseOptionEditor.onFieldChanged = UpdateFromGUI;
            chooseOptionFlight.onFieldChanged = UpdateFromGUI;
        }

        public override void OnUpdate()
        {
            chooseField.guiActive = CheatOptions.NonStrictAttachmentOrientation || (canSwitchWavelengthInFlight && BeamConfigurations.Count > 1);
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            if (!BeamConfigurations.Any())
                return;

            if (isLoaded == false)
                LoadInitialConfiguration();
            else
            {
                if (selectedBeamConfiguration < BeamConfigurations.Count)
                {
                    activeConfiguration = BeamConfigurations[selectedBeamConfiguration];
                }
                else
                {
                    Debug.LogWarning("[KSPI]: selectedBeamConfiguration < BeamConfigurations.Count, selecting last");
                    selectedBeamConfiguration = BeamConfigurations.Count - 1;
                    activeConfiguration = BeamConfigurations.Last();
                }
            }

            if (activeConfiguration == null)
            {
                Debug.Log("[KSPI]: UpdateFromGUI no activeConfiguration found");
                return;
            }

            beamWaveName = activeConfiguration.beamWaveName;
            wavelength = activeConfiguration.wavelength;
            wavelengthText = WavelenthToText(wavelength);
            atmosphericAbsorptionPercentage = activeConfiguration.atmosphericAbsorptionPercentage;
            waterAbsorptionPercentage = activeConfiguration.waterAbsorptionPercentage;

            UpdateEfficiencyPercentage();
        }

        private string WavelenthToText(double wavelength)
        {
            if (wavelength > 1.0e-3)
                return (wavelength * 1.0e+3).ToString() + " mm";
            else if (wavelength > 7.5e-7)
                return (wavelength * 1.0e+6).ToString() + " µm";
            else if (wavelength > 1.0e-9)
                return (wavelength * 1.0e+9).ToString() + " nm";
            else
                return (wavelength * 1.0e+12).ToString() + " pm";
        }

        private void UpdateEfficiencyPercentage()
        {
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
            var moduleMassDelta = fixedMass ? 0 : targetMass - initialMass;

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
            return info.ToString();
        }
    }
}
