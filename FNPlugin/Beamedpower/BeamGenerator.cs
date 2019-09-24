using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.Localization;
using TweakScale;
using UnityEngine;

namespace FNPlugin.Microwave
{
    [KSPModule("Integrated Beam Generator")]
    class IntegratedBeamGenerator : BeamGenerator { }

    [KSPModule("Beam Generator")]
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
        public string techLevelMk1 = "start";
        [KSPField(isPersistant = false)]
        public string techLevelMk2;
        [KSPField(isPersistant = false)]
        public string techLevelMk3;
        [KSPField(isPersistant = false)]
        public string techLevelMk4;
        [KSPField(isPersistant = false)]
        public string techLevelMk5;
        [KSPField(isPersistant = false)]
        public string techLevelMk6;

        [KSPField(isPersistant = false)]
        public double powerMassFraction = 0.5;
        [KSPField(isPersistant = false)]
        public bool fixedMass = false;

        ConfigNode[] beamConfigurationNodes;
        BeamConfiguration activeConfiguration;
        BaseField chooseField;

        int techLevel;

        private void DetermineTechLevel()
        {
            techLevel = 0;
            if (PluginHelper.UpgradeAvailable(techLevelMk2))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(techLevelMk3))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(techLevelMk4))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(techLevelMk5))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(techLevelMk6))
                techLevel++;
        }

        private int GetTechLevelFromTechId(string techid)
        {
            if (techid == techLevelMk6)
                return 6;
            else if (techid == techLevelMk5)
                return 5;
            else if (techid == techLevelMk4)
                return 4;
            else if (techid == techLevelMk3)
                return 3;
            else if (techid == techLevelMk2)
                return 2;
            else if (techid == techLevelMk1)
                return 1;
            else 
                return 1;
        }

        private string GetColorCodeFromTechId(string techid)
        {
            if (techid == techLevelMk6)
                return "<color=#ee9900ff>";
            else if (techid == techLevelMk5)
                return "<color=#ffaa00ff>";
            else if (techid == techLevelMk4)
                return "<color=#ffbb00ff>";
            else if (techid == techLevelMk3)
                return "<color=#ffcc00ff>";
            else if (techid == techLevelMk2)
                return "<color=#ffdd00ff>";
            else if (techid == techLevelMk1)
                return "<color=#ffff00ff>";
            else
                return "<color=#ffff00ff>";
        }

        private IList<BeamConfiguration> _inlineConfigurations;

        private IList<BeamConfiguration> _beamConfigurations;

        public IList<BeamConfiguration> BeamConfigurations 
        {
            get
            {
                if (_beamConfigurations == null)
                {
                    // ToDo: remove once inline beam configuration is fully implemented
                    var moduleConfigurations = part.FindModulesImplementing<BeamConfiguration>();

                    _beamConfigurations = moduleConfigurations
                        .Where(m => PluginHelper.HasTechRequirementOrEmpty(m.techRequirement0))
                        .OrderByDescending(m => m.wavelength).ToList();

                    Debug.Log("[KSPI]: Found " + _beamConfigurations.Count + " BeamConfigurations");
                }
                return _beamConfigurations;
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
            DetermineTechLevel();
        }

        private void InitializeWavelengthSelector()
        {
            Debug.Log("[KSPI]: Setup Transmit Beams Configurations for " + part.partInfo.title);

            chooseField = Fields["selectedBeamConfiguration"];
            chooseField.guiActive = CheatOptions.NonStrictAttachmentOrientation || (canSwitchWavelengthInFlight && BeamConfigurations.Count > 1);

            var chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            var chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            var names = BeamConfigurations.Select(m => m.beamWaveName).ToArray();

            if (chooseOptionEditor != null)
                chooseOptionEditor.options = names;

            if (chooseOptionFlight != null)
                chooseOptionFlight.options = names;

            if (wavelength != 0)
            {
                // find first wavelength with equal or shorter wavelength
                activeConfiguration = BeamConfigurations.FirstOrDefault(m => m.wavelength <= wavelength);

                if (activeConfiguration == null)
                    activeConfiguration = selectedBeamConfiguration < BeamConfigurations.Count ? BeamConfigurations[selectedBeamConfiguration] : BeamConfigurations.FirstOrDefault();

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
                return (wavelength * 1.0e+3) + " mm";
            else if (wavelength > 7.5e-7)
                return (wavelength * 1.0e+6) + " µm";
            else if (wavelength > 1.0e-9)
                return (wavelength * 1.0e+9) + " nm";
            else
                return (wavelength * 1.0e+12) + " pm";
        }

        private void UpdateEfficiencyPercentage()
        {
            techLevel = -1;

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

            if (BeamConfigurations.Count <= 1 || activeConfiguration == null)
                return;

            var lowestWavelengthDifference = Math.Abs(currentWavelength - activeConfiguration.wavelength);

            foreach (var currentConfig in BeamConfigurations)
            {
                var configWaveLengthDifference = Math.Abs(currentWavelength - currentConfig.wavelength);

                if (!(configWaveLengthDifference < lowestWavelengthDifference)) continue;

                activeConfiguration = currentConfig;
                lowestWavelengthDifference = configWaveLengthDifference;
                selectedBeamConfiguration = BeamConfigurations.IndexOf(currentConfig);
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

        public override void OnLoad(ConfigNode node)
        {
            beamConfigurationNodes =  node.GetNodes("BeamConfiguration");

            if (beamConfigurationNodes.Any())
            {
                Debug.Log("[KSPI]: OnLoad Found " + beamConfigurationNodes.Count() + " BeamConfigurations");
            }

            var inlineConfigurations = new  List<BeamConfiguration>();

            foreach (var beamConfigurationNode in beamConfigurationNodes)
            {
                var beamConfiguration = new BeamConfiguration
                {
                    beamWaveName = beamConfigurationNode.GetValue("beamWaveName"),
                    wavelength = Double.Parse(beamConfigurationNode.GetValue("wavelength")),
                    atmosphericAbsorptionPercentage = Double.Parse(beamConfigurationNode.GetValue("atmosphericAbsorptionPercentage")),
                    waterAbsorptionPercentage = Double.Parse(beamConfigurationNode.GetValue("waterAbsorptionPercentage")),

                    techRequirement0 = beamConfigurationNode.HasValue("techRequirement0")? beamConfigurationNode.GetValue("techRequirement0"): null,
                    techRequirement1 = beamConfigurationNode.HasValue("techRequirement1")? beamConfigurationNode.GetValue("techRequirement1"): null,
                    techRequirement2 = beamConfigurationNode.HasValue("techRequirement2")? beamConfigurationNode.GetValue("techRequirement2"): null,
                    techRequirement3 = beamConfigurationNode.HasValue("techRequirement3")? beamConfigurationNode.GetValue("techRequirement3"): null,
                    efficiencyPercentage0 = beamConfigurationNode.HasValue("efficiencyPercentage0")? double.Parse(beamConfigurationNode.GetValue("efficiencyPercentage0")): 0,
                    efficiencyPercentage1 = beamConfigurationNode.HasValue("efficiencyPercentage1")? double.Parse(beamConfigurationNode.GetValue("efficiencyPercentage1")): 0,
                    efficiencyPercentage2 = beamConfigurationNode.HasValue("efficiencyPercentage2")? double.Parse(beamConfigurationNode.GetValue("efficiencyPercentage2")): 0,
                    efficiencyPercentage3 = beamConfigurationNode.HasValue("efficiencyPercentage3")? double.Parse(beamConfigurationNode.GetValue("efficiencyPercentage3")): 0
                };

                inlineConfigurations.Add(beamConfiguration);
            }

            _inlineConfigurations = inlineConfigurations.OrderByDescending(m => m.wavelength).ToList();
        }

        public override string GetInfo()
        {
            var sb = new StringBuilder();

            sb.Append("<size=10>");
            sb.AppendLine("Type: " + beamTypeName);
            sb.AppendLine("Can Switch In Flight: " + DisplayBoolean(canSwitchWavelengthInFlight));
            sb.AppendLine("</size>");

            if (!string.IsNullOrEmpty(techLevelMk2))
            {
                sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_BeamGenerator_upgradeTechnologies") + ":</color><size=10>");
                if (!string.IsNullOrEmpty(techLevelMk1)) sb.AppendLine("<color=#ffff00ff>Mk1:</color> " + Localizer.Format(PluginHelper.GetTechTitleById(techLevelMk1)));
                if (!string.IsNullOrEmpty(techLevelMk2)) sb.AppendLine("<color=#ffdd00ff>Mk2:</color> " + Localizer.Format(PluginHelper.GetTechTitleById(techLevelMk2)));
                if (!string.IsNullOrEmpty(techLevelMk3)) sb.AppendLine("<color=#ffcc00ff>Mk3:</color> " + Localizer.Format(PluginHelper.GetTechTitleById(techLevelMk3)));
                if (!string.IsNullOrEmpty(techLevelMk4)) sb.AppendLine("<color=#ffbb00ff>Mk4:</color> " + Localizer.Format(PluginHelper.GetTechTitleById(techLevelMk4)));
                if (!string.IsNullOrEmpty(techLevelMk5)) sb.AppendLine("<color=#ffaa00ff>Mk5:</color> " + Localizer.Format(PluginHelper.GetTechTitleById(techLevelMk5)));
                sb.Append("</size>");
                sb.AppendLine("");
            }

            if (_inlineConfigurations.Count <= 0) return sb.ToString();

            sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_BeamGenerator_atmosphericAbsorbtion") + ":</color>");
            foreach (var beamConfiguration in _inlineConfigurations)
            {
                sb.Append("<size=10>" + ExtendWithSpace(beamConfiguration.atmosphericAbsorptionPercentage + "%", 4));
                sb.Append("<color=#00ff00ff> " + beamConfiguration.beamWaveName + "</color>");                
                sb.AppendLine("</size>");
            }

            sb.AppendLine("");
            sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_BeamGenerator_beamEfficiencies") + ":</color>");

            foreach (var beamConfiguration in _inlineConfigurations)
            {
                sb.AppendLine("<size=10><color=#00ff00ff>" + beamConfiguration.beamWaveName + "</color> <color=#00e600ff>(" + WavelenthToText(beamConfiguration.wavelength) + ")</color>");
                sb.Append("  ");
                if (beamConfiguration.efficiencyPercentage0 > 0) sb.Append(GetColorCodeFromTechId(beamConfiguration.techRequirement0) + "Mk" + GetTechLevelFromTechId(beamConfiguration.techRequirement0) + ":</color> " + beamConfiguration.efficiencyPercentage0 + "% ");
                if (beamConfiguration.efficiencyPercentage1 > 0) sb.Append(GetColorCodeFromTechId(beamConfiguration.techRequirement1) + "Mk" + GetTechLevelFromTechId(beamConfiguration.techRequirement1) + ":</color> " + beamConfiguration.efficiencyPercentage1 + "% ");
                if (beamConfiguration.efficiencyPercentage2 > 0) sb.Append(GetColorCodeFromTechId(beamConfiguration.techRequirement2) + "Mk" + GetTechLevelFromTechId(beamConfiguration.techRequirement2) + ":</color> " + beamConfiguration.efficiencyPercentage2 + "% ");
                if (beamConfiguration.efficiencyPercentage3 > 0) sb.Append(GetColorCodeFromTechId(beamConfiguration.techRequirement3) + "Mk" + GetTechLevelFromTechId(beamConfiguration.techRequirement3) + ":</color> " + beamConfiguration.efficiencyPercentage3 + "% ");
                sb.AppendLine("</size>");
            }

            return sb.ToString();
        }

        private string DisplayBoolean(bool value)
        {
            return value ? "<color=green>Ѵ</color>" : "<color=red>X</color>";
        }

        private string ExtendWithSpace(string input, int targetlength)
        {
            return input + AddSpaces(targetlength - input.Length);
        }

        private string AddSpaces(int length)
        {
            string result = "";
            for (int i = 0; i < length; i++)
            {
                result += " ";
            }
            return result;
        }
    }
}
