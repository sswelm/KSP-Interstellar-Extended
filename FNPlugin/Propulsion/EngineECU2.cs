using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TweakScale;

namespace FNPlugin
{
    enum GenerationType { Mk1, Mk2, Mk3, Mk4, Mk5 }
    abstract class EngineECU2 : FNResourceSuppliableModule, IRescalable<EngineECU2>
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fuel Config")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedFuel = 0;

        public bool hasMultipleConfigurations = false;
        private UIPartActionWindow tweakableUI;
      
       

        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool Launched = false;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk3 = 0.05f;
        [KSPField(isPersistant = true)]
        public float scale = 1;

        // None Persistant 

        [KSPField(isPersistant = false)]
        public float maxThrust = 75;
        public float MaxThrust { get { return maxThrust * thrustMult(); } }
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 300;
        public float MaxThrustUpgraded { get { return maxThrustUpgraded * thrustMult(); } }

        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded2 = 1200;
        public float MaxThrustUpgraded2 { get { return maxThrustUpgraded2 * thrustMult(); } }



        [KSPField(isPersistant = false)]
        public float efficiency = 0.19f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded = 0.38f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded2 = 0.76f;

        [KSPField(isPersistant = false)]
        public bool isLoaded = false;





        // Use for SETI Mode

        [KSPField(isPersistant = false)]
        public float maxTemp = 2500;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;


        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust", guiUnits = " kN")]
        public float maximumThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Throtle", guiFormat = "F2")]
        public float throttle;

        public ModuleEngines curEngineT;

        private FuelConfiguration activeConfiguration;


        public FuelConfiguration ActiveConfiguration
        {
            get
            {
                if (activeConfiguration == null) activeConfiguration = FuelConfigurations[selectedFuel];
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
                    fuelConfigurations = part.FindModulesImplementing<FuelConfiguration>().OrderByDescending(f => f.maxThrust).ToList();

                }
                return fuelConfigurations;
            }
        }
        private float thrustMult()
        {
            return (FuelConfigurations.Count > 0 ? ActiveConfiguration.thrustMult : 1) *
                        (float)(scale == 0 ? 1 : Math.Pow(scale, 2));
        }
        private void InitializeFuelSelector()
        {
            Debug.Log("[KSP Interstellar] Setup Transmit Fuels Configurations for " + part.partInfo.title);

            var chooseField = Fields["selectedFuel"];
            var chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            var chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            chooseField.guiActive = FuelConfigurations.Count > 1;
            chooseField.guiActiveEditor = FuelConfigurations.Count > 1;

            var names = FuelConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            chooseOptionEditor.options = names;
            chooseOptionFlight.options = names;

            //  UpdateFromGUI(chooseField, selectedFuel);

            // connect on change event
            if (chooseField.guiActive)
            {
                chooseOptionEditor.onFieldChanged = UpdateEditorGUI;
                chooseOptionFlight.onFieldChanged = UpdateFlightGUI;
            }
        }

        private void UpdateEditorGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("Editor Gui Updated");
            UpdateFromGUI(field, oldFieldValueObj);
            UpdateResources();

        }
        private void UpdateFlightGUI(BaseField field, object oldFieldValueObj)
        {

           
           
            UpdateFromGUI(field, oldFieldValueObj);
            UpdateFuel();


        }

        public virtual void UpdateFuel()
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
            vessel.ClearStaging();
            vessel.ResumeStaging();


        }
        private void UpdateResources()
        {
            Debug.Log("Update Resources");

            ConfigNode akResources = new ConfigNode();
            

            int I = 0;
            int N = 0;

            while (I < part.Resources.Count)
            {
                part.Resources.Remove(part.Resources[I]);
                
                I++;
            }
            part.Resources.Clear();
            I = 0;

            // part.SetupResources();
            Debug.Log("Old Fuels: " + part.Resources.Count);
            while (I < ActiveConfiguration.Fuels.Length)
            {

                if (ActiveConfiguration.MaxAmount[I] > 0) part.AddResource(LoadResource(ActiveConfiguration.Fuels[I], ActiveConfiguration.Amount[I], ActiveConfiguration.MaxAmount[I]));
                else N++;
                I++;
            }
            if (N + 1 >= ActiveConfiguration.Fuels.Length) Fields["selectedFuel"].guiActive = false;

            Debug.Log("New Fuels: " + ActiveConfiguration.Fuels.Length);
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

            ConfigNode ResourceNode = new ConfigNode().AddNode("RESOURCE");
            ResourceNode.AddValue("name", akName);
            ResourceNode.AddValue("amount", akAmount);
            ResourceNode.AddValue("maxAmount", akMax);
            return ResourceNode;
        }


        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("[KSP Interstellar] UpdateFromGUI is called with " + selectedFuel);

            if (!FuelConfigurations.Any())
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI no FuelConfigurations found");
                return;
            }


            if (selectedFuel < FuelConfigurations.Count)
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI " + selectedFuel + " < orderedFuelGenerators.Count");
              //  FuelConfigurations[selectedFuel].Factor = ActiveConfiguration.Factor;
                activeConfiguration = FuelConfigurations[selectedFuel];
            }
            else
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI " + selectedFuel + " >= orderedFuelGenerators.Count");
                selectedFuel = FuelConfigurations.Count - 1;
              //  FuelConfigurations[selectedFuel].Factor = ActiveConfiguration.Factor;
                activeConfiguration = FuelConfigurations.Last();
            }


            if (activeConfiguration == null)
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI no activeConfiguration found");
                return;
            }

        }

        private void LoadInitialConfiguration()
        {
            isLoaded = true;

            //   var currentmaxIsp = maxIsp != 0 ? maxIsp : 1;

            //Debug.Log("[KSP Interstellar] UpdateFromGUI initialize initial fuel configuration with maxIsp target " + currentmaxIsp);

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
            Debug.Log("Start State: " + state.ToString());
            Debug.Log("Already Launched: " + Launched);
            InitializeFuelSelector();
            if (state.ToString().Contains(StartState.Editor.ToString()))
            {
                Debug.Log("Editor");
                UpdateResources();
               
            }
            else
            {
                UpdateFuel();
            }
            state.GetType();
            if (state.ToString().Contains(StartState.PreLaunch.ToString()))
            {
                Debug.Log("PreLaunch");

                    UpdateResources();
                
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
    }
    class FuelConfiguration : PartModule, IRescalable<FuelConfiguration>
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuel Configuration")]
        public string fuelConfigurationName = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuels")]
        public string fuels = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ratios")]
        public string ratios = "";
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Amount")]
        public string amount = "";
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Max Amount")]
        public string maxAmount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max ISP")]
        public float maxIsp = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust")]
        public float maxThrust = 1;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "ThrustMult")]
        public float thrustMult = 1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Max Power Requirement")]
        public float powerMult = 1;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmopheric Curve")]
        public FloatCurve atmosphereCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore ISP")]
        public string ignoreForIsp = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore Thrust")]
        public string ignoreForThrustCurve = "";

        [KSPField(isPersistant = true)]
        public float Scale = 1;




        private string[] akFuels = new string[0];
        private bool[] akIgnoreIsp = new bool[0];
        private bool[] akIgnoreThrust = new bool[0];
        private float[] akRatio = new float[0];
      
        private float[] akAmount = new float[0];
       
        private float[] akMaxAmount = new float[0];

        

        public string[] Fuels
        {

            get
            {

                if (akFuels.Length == 0) akFuels = Regex.Replace(fuels, " ", "").Split(',');


                return akFuels;
            }
        }

        public float T_thrustMult
        {
            get
            {
               
                return thrustMult * (float)(Math.Pow(Scale, 2) == 0 ? 1 : Math.Pow(Scale, 2));
            }
        }
        public float T_powerMult
        {
            get
            {

                return powerMult * (float)(Math.Pow(Scale, 2) == 0 ? 1 : Math.Pow(Scale, 2));
            }   
        }


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
                if (akAmount.Length == 0) akAmount = StringToFloatArray(amount);
                return VolumeTweaked(akAmount);
            }

        }
        public float[] MaxAmount
        {
            get
            {
                if (akMaxAmount.Length == 0) akMaxAmount = StringToFloatArray(maxAmount);
                return VolumeTweaked(akMaxAmount);

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
        private float[]VolumeTweaked(float[] akFloat)
        {
            Debug.Log("akFloat.length: " + akFloat.Length);
            float[] akTweaked = new float[akFloat.Length];
           
            if (Scale != 1 && Scale > 0 )
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
            akstring += akFloat[I];
            while (I < akFloat.Length)
            {
                akstring = akstring + ", " + akFloat;
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
        public virtual void OnRescale(ScalingFactor factor)
        {
            Debug.Log(fuelConfigurationName + " Rescaled");
            Scale = factor.absolute.linear;

        }


    }
}
