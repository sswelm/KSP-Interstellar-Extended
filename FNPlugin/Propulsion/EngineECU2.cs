using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using TweakScale;

namespace FNPlugin
{

    enum GenerationType { Mk1 = 0, Mk2 = 1, Mk3 = 2, Mk4 = 3, Mk5 = 4 }

    abstract class EngineECU2 : FNResourceSuppliableModule, IRescalable<EngineECU2>
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fuel Config")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedFuel = 0;

        public bool hasMultipleConfigurations = false;
        private UIPartActionWindow tweakableUI;
        StartState CurState;

        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool Launched = false;
        [KSPField(isPersistant = true)]
        public double scale = 1;
        [KSPField(isPersistant = true)]
        public bool hideEmpty = false;
        [KSPField(isPersistant = true)]
        public int selectedTank = 0;
        [KSPField(isPersistant = true)]
        public string selectedTankName = "";

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "upgrade tech 1")]
        public string upgradeTechReq = "advFusionReactions";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "upgrade tech 2")]
        public string upgradeTechReq2 = "exoticReactions";

        // None Persistant 
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk3 = 0.05f;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true)]
        public double thrustmultiplier;

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public float maxThrust = 75;
        public double MaxThrust 
        { 
            get { return maxThrust * thrustMult(); } 
        }

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public float maxThrustUpgraded = 300;
        public double MaxThrustUpgraded 
        { 
            get { return maxThrustUpgraded * thrustMult(); } 
        }

        [KSPField(isPersistant = false, guiActiveEditor = true)]
        public float maxThrustUpgraded2 = 1200;
        public double MaxThrustUpgraded2 
        { 
            get { return maxThrustUpgraded2 * thrustMult(); } 
        }

        [KSPField(isPersistant = false)]
        public double efficiency = 0.19;
        [KSPField(isPersistant = false)]
        public double efficiencyUpgraded = 0.38;
        [KSPField(isPersistant = false)]
        public double efficiencyUpgraded2 = 0.76;

        [KSPField(isPersistant = false)]
        public bool isLoaded = false;

        // Use for SETI Mode
        [KSPField(isPersistant = false)]
        public float maxTemp = 2500;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust", guiUnits = " kN", guiFormat = "F4")]
        public double maximumThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Throtle", guiFormat = "F2")]
        public float throttle;

        public ModuleEngines curEngineT;
        private FuelConfiguration activeConfiguration;
 
        public GenerationType EngineGenerationType { get; private set; }

        public void DetermineTechLevel()
        {
            int numberOfUpgradeTechs = 1;
            if (PluginHelper.upgradeAvailable(upgradeTechReq))
                numberOfUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReq2))
                numberOfUpgradeTechs++;

            if (numberOfUpgradeTechs == 3)
                EngineGenerationType = GenerationType.Mk3;
            else if (numberOfUpgradeTechs == 2)
                EngineGenerationType = GenerationType.Mk2;
            else
                EngineGenerationType = GenerationType.Mk2;
        }

        [KSPEvent(active = true, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "Hide Unusable Configurations")]
        public void HideFuels()
        {
            hideEmpty = true;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            UpdateusefulConfigurations();
            InitializeFuelSelector();
            UpdateFuel();
        }
        [KSPEvent(active = false, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "Show All Configurations")]
        public void ShowFuels()
        {
            FuelConfiguration CurConfig = ActiveConfiguration;
            hideEmpty = false;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            selectedFuel = ActiveConfigurations.IndexOf(CurConfig);
            InitializeFuelSelector();
            UpdateFuel();


        }
        public void InitializeGUI()
        {
            InitializeFuelSelector();
            InitializeHideFuels();
        }
        private void InitializeFuelSelector()
        {
            Debug.Log("[KSPI] - Setup Fuels Configurations for " + part.partInfo.title);

            var chooseField = Fields["selectedFuel"];
            var chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            var chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;


            if (ActiveConfigurations.Count <= 1)
            {
                chooseField.guiActive = false;
                chooseField.guiActiveEditor = false;
                selectedFuel = 0;
            }
            else
            {
                chooseField.guiActive = true;
                chooseField.guiActiveEditor = true;
                if (selectedFuel >= ActiveConfigurations.Count) selectedFuel = ActiveConfigurations.Count - 1;
                activeConfiguration = ActiveConfigurations[selectedFuel];
            }

            Debug.Log("Selected Fuel # " + selectedFuel);

            var names = ActiveConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            chooseOptionEditor.options = names;
            chooseOptionFlight.options = names;

            // connect on change event
            if (chooseField.guiActive) chooseOptionFlight.onFieldChanged = UpdateFlightGUI;
            if (chooseField.guiActiveEditor) chooseOptionEditor.onFieldChanged = UpdateEditorGUI;
            activeConfiguration = ActiveConfigurations[selectedFuel];

        }
        private void InitializeHideFuels()
        {
            BaseEvent[] EventList = { Events["HideFuels"], Events["ShowFuels"] };
            foreach (BaseEvent akEvent in EventList)
            {
                if (FuelConfigurations.Count <= 1)
                {
                    akEvent.guiActive = false;
                    //akEvent.guiActiveEditor = false;
                }
                else
                {
                    akEvent.guiActive = true;
                    //akEvent.guiActiveEditor = true;
                }

            }
        }

        public FuelConfiguration ActiveConfiguration
        {
            get
            {
                if (activeConfiguration == null) activeConfiguration = ActiveConfigurations[selectedFuel];
                return activeConfiguration;
            }
        }

        private IList<FuelConfiguration> fuelConfigurations;

        public IList<FuelConfiguration> FuelConfigurations
        {
            get
            {
                if (fuelConfigurations == null)
                {
                    fuelConfigurations = part.FindModulesImplementing<FuelConfiguration>().Where(c => c.requiredTechLevel <= (int)EngineGenerationType).ToList();
                }
                return fuelConfigurations;
            }
        }
        private double thrustMult()
        {
            //thrustmultiplier = (FuelConfigurations.Count > 0 ? ActiveConfiguration.thrustMult : 1) * (scale == 0 ? 1 : Math.Pow(scale, 2));
            thrustmultiplier = FuelConfigurations.Count > 0 ? ActiveConfiguration.thrustMult : 1;
            return thrustmultiplier;
        }
    

        private void UpdateEditorGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("Editor Gui Updated");
            UpdateFromGUI(field, oldFieldValueObj);
            selectedTank = selectedFuel;
            selectedTankName = FuelConfigurations[selectedFuel].ConfigName;
            UpdateResources();
           
        }
        private void UpdateFlightGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateFromGUI(field, oldFieldValueObj);
            UpdateFuel();
        }

        public virtual void UpdateFuel(bool isEditor = false)
        {
            Debug.Log("Update Fuel");

            ConfigNode akPropellants = new ConfigNode();
           

            int I = 0;
            int N = 0;

            while (I < ActiveConfiguration.Fuels.Length)
            {
                if (ActiveConfiguration.Ratios[I] > 0) akPropellants.AddNode(LoadPropellant(ActiveConfiguration.Fuels[I], ActiveConfiguration.Ratios[I]));
                else N++;
                I++;
            }
            if (N + 1 >= ActiveConfiguration.Fuels.Length) Fields["selectedFuel"].guiActive = false;

            akPropellants.AddValue("maxThrust", 1);

            akPropellants.AddValue("maxFuelFlow", 1);

            curEngineT.Load(akPropellants);
            curEngineT.atmosphereCurve = ActiveConfiguration.atmosphereCurve;
            if (!isEditor)
            {
                vessel.ClearStaging();
                vessel.ResumeStaging();
            }
        }

        private void UpdateResources()
        {
            Debug.Log("Update Resources");

            ConfigNode akResources = new ConfigNode();
            FuelConfiguration akConfig = new FuelConfiguration();
            if (selectedTankName == "")
                selectedTankName = FuelConfigurations[selectedTank].ConfigName;
            else if
                (FuelConfigurations[selectedTank].ConfigName == selectedTankName)
                akConfig = FuelConfigurations[selectedTank];
            else
            {
                selectedTank = FuelConfigurations.IndexOf(FuelConfigurations.FirstOrDefault(g => g.ConfigName == selectedTankName));
                akConfig = FuelConfigurations[selectedTank];
            }


            int I = 0;
            int N = 0;

            while (I < part.Resources.Count)
            {
                part.Resources.Remove(part.Resources[I]);
                I++;
            }

            part.Resources.Clear();

            // part.SetupResources();

            I = 0;
            N = 0;
            while (I < akConfig.Fuels.Length)
            {
                Debug.Log("Resource: " + akConfig.Fuels[I] + " has a " + akConfig.MaxAmount[I] + " tank.");
                if (akConfig.MaxAmount[I] > 0)
                {
                    
                    part.AddResource(LoadResource(akConfig.Fuels[I], akConfig.Amount[I], akConfig.MaxAmount[I]));
                }
                else N++;
                I++;
            }

            if (N + 1 >= akConfig.Fuels.Length) Fields["selectedFuel"].guiActive = false;

            Debug.Log("New Fuels: " + akConfig.Fuels.Length);
            if (tweakableUI == null)
                tweakableUI = part.FindActionWindow();
            if (tweakableUI != null)
                tweakableUI.displayDirty = true;

            //     curEngineT.Save(akResources);
            Debug.Log("Resources Updated");
        }
    

        private ConfigNode LoadPropellant(string akName, float akRatio)
        {
            Debug.Log("Name: " + akName);
            //    Debug.Log("Ratio: "+ akRatio);

            ConfigNode PropellantNode = new ConfigNode().AddNode("PROPELLANT");
            PropellantNode.AddValue("name", akName);
            PropellantNode.AddValue("ratio", akRatio);
            PropellantNode.AddValue("DrawGauge", true);

            return PropellantNode;
        }
        private ConfigNode LoadResource(string akName, float akAmount, float akMax)
        {
           // Debug.Log("Resource: "+akName + " Added");
            ConfigNode ResourceNode = new ConfigNode().AddNode("RESOURCE");
            ResourceNode.AddValue("name", akName);
            ResourceNode.AddValue("amount", akAmount);
            ResourceNode.AddValue("maxAmount", akMax);
            return ResourceNode;
        }


        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("[KSPI] - UpdateFromGUI is called with " + selectedFuel);

            if (!FuelConfigurations.Any())
            {
                Debug.Log("[KSPI] - UpdateFromGUI no FuelConfigurations found");
                return;
            }

            if (selectedFuel < FuelConfigurations.Count)
            {
                Debug.Log("[KSPI] - UpdateFromGUI " + selectedFuel + " < orderedFuelGenerators.Count");
              //  FuelConfigurations[selectedFuel].Factor = ActiveConfiguration.Factor;
                activeConfiguration = FuelConfigurations[selectedFuel];
            }
            else
            {
                Debug.Log("[KSPI] - UpdateFromGUI " + selectedFuel + " >= orderedFuelGenerators.Count");
                selectedFuel = FuelConfigurations.Count - 1;
              //  FuelConfigurations[selectedFuel].Factor = ActiveConfiguration.Factor;
                activeConfiguration = FuelConfigurations.Last();
            }

            if (activeConfiguration == null)
            {
                Debug.Log("[KSPI] - UpdateFromGUI no activeConfiguration found");
                return;
            }
        }

        private void LoadInitialConfiguration()
        {
            isLoaded = true;

            //   var currentmaxIsp = maxIsp != 0 ? maxIsp : 1;

            //Debug.Log("[KSPI] - UpdateFromGUI initialize initial fuel configuration with maxIsp target " + currentmaxIsp);

            // find maxIsp closes to target maxIsp
            activeConfiguration = FuelConfigurations.FirstOrDefault();
            selectedFuel = 0;
            //     var lowestmaxIspDifference = Math.Abs(currentmaxIsp - activeConfiguration.maxIsp);
            if (FuelConfigurations.Count > 1)
            {
                hasMultipleConfigurations = true;
            }
        }

        public override void OnUpdate()
        {
            //    InitializeFuelSelector();
            //  if (activeConfiguration == null ) InitializeFuelSelector();
            base.OnUpdate();
        }
        public override void OnStart(StartState state)
        {
            try
            {
                Debug.Log("Start State: " + state.ToString());
                Debug.Log("Already Launched: " + Launched);
                CurState = state;
                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();

                InitializeGUI();


                if (state.ToString().Contains(StartState.Editor.ToString()))
                {
                    Debug.Log("Editor");
                    hideEmpty = false;
                    selectedTank = selectedFuel;
                    selectedTankName = FuelConfigurations[selectedFuel].ConfigName;
                    UpdateResources();
                    UpdateFuel(true);
                }
                else
                {


                    hideEmpty = true;
                    if (state.ToString().Contains(StartState.PreLaunch.ToString())) // startstate normally == prelaunch,landed
                    {
                        Debug.Log("PreLaunch");
                        hideEmpty = true;
                        UpdateResources();
                        UpdateusefulConfigurations();
                        InitializeFuelSelector();
                        UpdateFuel();
                    }

                }
                Events["ShowFuels"].active = hideEmpty;
                Events["HideFuels"].active = !hideEmpty;
            }
            catch (Exception e)
            {
                Debug.LogError("EngineECU2 OnStart eception: " + e.Message);
            }


            base.OnStart(state);
        }


        public virtual void OnRescale(TweakScale.ScalingFactor akFactor)
        {
            scale = akFactor.absolute.linear;
        }


        public override void OnInitialize()
        {
            //  InitializeFuelSelector();
            base.OnInitialize();
        }

        
        private IList<FuelConfiguration> usefulConfigurations;
        public IList<FuelConfiguration> UsefulConfigurations
        {
            get
            {
                if (usefulConfigurations == null)
                {
                    usefulConfigurations = GetUsableConfigurations(FuelConfigurations);
                }
                if (usefulConfigurations == null)
                {
                    Debug.Log("UsefulConfigurations Broke!");
                    return FuelConfigurations;
                }


                return usefulConfigurations;
            }
        }
        public IList<FuelConfiguration> ActiveConfigurations
        {
            get
            {
                return hideEmpty ? UsefulConfigurations : FuelConfigurations;
            }
        }
        public void UpdateusefulConfigurations()
        {
            IList<FuelConfiguration> akConfig = new List<FuelConfiguration>(usefulConfigurations);
            usefulConfigurations = GetUsableConfigurations(FuelConfigurations);
            if (akConfig.Equals(usefulConfigurations)) InitializeFuelSelector();
        }

        public IList<FuelConfiguration> GetUsableConfigurations(IList<FuelConfiguration> akConfigs)
        {
            IList<FuelConfiguration> nwConfigs = new List<FuelConfiguration>();
            int I = 0;
            while (I < akConfigs.Count)
            {
                if (ConfigurationHasFuel(akConfigs[I]))
                {
                    nwConfigs.Add(akConfigs[I]);
                    Debug.Log("Added: " + akConfigs[I].fuelConfigurationName);

                }
                else if (I < selectedFuel && I > 0) selectedFuel--;
                I++;
            }

            return nwConfigs;
        }
        public bool ConfigurationHasFuel(FuelConfiguration akConfig)
        {
            bool Test = true;
            int I = 0;
            while (I < akConfig.Fuels.Length)
            {
                if (akConfig.Ratios[I] > 0)
                {
                    double akAmount = 0;
                    double akMaxAmount = 0;
                    PartResource akResource = this.part.Resources.Get(akConfig.Fuels[I]);
                    if (akResource != null)
                    {

                      //  vessel.UpdateResourceSets();
                        part.GetConnectedResourceTotals(akResource.info.id, out akAmount, out akMaxAmount);
                        Debug.Log("Resource: " + akConfig.Fuels[I] + " has " + akAmount);
                        if (akAmount == 0 && akMaxAmount > 0)
                        {
                            Test = false;
                            I = akConfig.Fuels.Length;
                        }
                        /*       }
                               else
                               {
                                   part.vessel.resour
                               }*/
                    }
                    else
                    {
                        Debug.Log("Resource: " + akConfig.Fuels[I] + " has " + 0);
                        Test = false;
                        I = akConfig.Fuels.Length;
                    }

                }

                I++;
            }

            return Test;
        }
    }
    class FuelConfiguration : PartModule, IRescalable<FuelConfiguration>
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuel Configuration")]
        public string fuelConfigurationName = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Required Tech Level")]
        public int requiredTechLevel = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuels")]
        public string fuels = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ratios")]
        public string ratios = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Amount")]
        public string amount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Amount")]
        public string maxAmount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max ISP")]
        public float maxIsp = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust")]
        public float maxThrust = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "ThrustMult")]
        public float thrustMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Power Requirement")]
        public float powerMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmopheric Curve")]
        public FloatCurve atmosphereCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore ISP")]
        public string ignoreForIsp = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore Thrust")]
        public string ignoreForThrustCurve = "";

        [KSPField(isPersistant = true)]
        public float Scale = 1;
        [KSPField(isPersistant = true)]
        private string akConfigName = "";
        [KSPField(isPersistant = true)]
        private string strAmount="";
        [KSPField(isPersistant = true)]
        private string strMaxAmount="";


        private float[] akAmount = new float[0];
        private float[] akMaxAmount = new float[0];

        private string[] akFuels = new string[0];
        private bool[] akIgnoreIsp = new bool[0];
        private bool[] akIgnoreThrust = new bool[0];
        private float[] akRatio = new float[0];

        public string ConfigName
        {
            get
            {
                if (akConfigName == "") akConfigName = fuelConfigurationName;
                return akConfigName;
            }
        }
        public string[] Fuels
        {
            get
            {
                if (akFuels.Length == 0) akFuels = Regex.Replace(fuels, " ", "").Split(',');
                return akFuels;
            }
        }

        //public float T_thrustMult
        //{
        //    get
        //    {
        //        return thrustMult * (float)(Math.Pow(Scale, 2) == 0 ? 1 : Math.Pow(Scale, 2));
        //    }
        //}

        //public float T_powerMult
        //{
        //    get
        //    {
        //        return powerMult * (float)(Math.Pow(Scale, 2) == 0 ? 1 : Math.Pow(Scale, 2));
        //    }
        //}

        public float[] Ratios
        {
            get
            {
                if (akRatio.Length == 0) akRatio = StringToFloatArray(ratios);

                return akRatio;
            }
        }

        public float[] Amount
        {
            get
            {
                if (akAmount.Length == 0) akAmount = StringToFloatArray(StrAmount);
                return VolumeTweaked(akAmount);
            }
        }


        public float[] MaxAmount
        {
            get
            {
                if (akMaxAmount.Length == 0) akMaxAmount = StringToFloatArray(StrMaxAmount);
                return VolumeTweaked(akMaxAmount);
            }

        }

        private string StrMaxAmount
        {
            get
            {
                if (strMaxAmount == "") strMaxAmount = maxAmount;
                return strMaxAmount;
            }
        }
        private string StrAmount
        {
            get
            {
                if (strAmount == "") strAmount = amount;
                return strAmount;
            }
        }
        public bool[] IgnoreForIsp
        {
            get
            {
                if (ignoreForIsp == "") akIgnoreIsp = falseBoolArray();
                else if (akIgnoreIsp.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreIsp;
            }

        }
        public bool[] IgnoreForThrust
        {
            get
            {
                if (ignoreForThrustCurve == "") akIgnoreThrust = falseBoolArray();
                else if (akIgnoreThrust.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreThrust;
            }
        }

        private float[] VolumeTweaked(float[] akFloat)
        {
          //  Debug.Log("akFloat.length: " + akFloat.Length);
            float[] akTweaked = new float[akFloat.Length];

            if (Scale != 1 && Scale > 0)
            {
                int I = 0;
                while (I < akFloat.Length)
                {
                    akTweaked[I] = (float)(akFloat[I] * Math.Pow(Scale, 3));
                    I++;
                }
                akFloat = akTweaked.ToArray();
            }
            return akFloat;
        }

        private bool[] falseBoolArray()
        {
            List<bool> akBoolList = new List<bool>();
            int I = 0;
            while (I < akFuels.Length)
            {
                akBoolList.Add(false);
                I++;
            }
            return akBoolList.ToArray();
        }

        private float[] StringToFloatArray(string akString)
        {
            List<float> akFloat = new List<float>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akFloat.Add((float)Convert.ToDouble(arString[I]));
                I++;
            }
            return akFloat.ToArray();
        }

        private string FloatArrayToString(float[] akFloat)
        {
            string akstring = "";
            int I = 1;
            akstring += akFloat[0];
            while (I < akFloat.Length)
            {
                akstring = akstring + ", " + akFloat[I];
                I++;
            }
            maxAmount = akstring;
            return akstring;
        }

        private bool[] StringToBoolArray(string akString)
        {
            List<bool> akBool = new List<bool>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akBool.Add(Convert.ToBoolean(arString[I]));
                I++;
            }
            return akBool.ToArray();
        }
        private void Refresh()
        {

            akConfigName = "";
            strMaxAmount = maxAmount;
            akMaxAmount = new float[0];
           int i = 0;
            while (i < Amount.Length)
            {
                if (Amount[i] > MaxAmount[i]) Amount[i] = MaxAmount[i];
                i++;
            }

        }
        private void SaveAmount(ShipConstruct Ship)
        {

          //  Debug.Log("Ship Modified!");
            try
            {


                // Debug.Log("akAmont Length: " + akAmount.Length);
                // Debug.Log("Amount Length: " + Amount.Length);
               // Debug.Log("Part Resources Length: " + part.Resources.Count);
                int i = 0;
                if (part.Resources.Count == Amount.Length)
                    while (i < Amount.Length)
                    {
                       // Debug.Log("Saving " + part.Resources[i].resourceName + " Amount " + part.Resources[i].amount);
                        akAmount[i] = (float)part.Resources[i].amount;
                        i++;
                    }

                strAmount = FloatArrayToString(Amount);

            }
            catch (Exception e)
            {
                Debug.LogError("Save Amount Error: " + e);
            }
        }
       

        public virtual void OnRescale(ScalingFactor factor)
        {
            
            Scale = factor.absolute.linear;
          //  Debug.Log(fuelConfigurationName + " Rescaled to " + Scale);
        }

        public override void OnStart(StartState state)
        {
            if (fuelConfigurationName != akConfigName || StrMaxAmount != maxAmount) Refresh();

            GameEvents.onEditorShipModified.Add(SaveAmount);
            base.OnStart(state);

        }

    }
}
