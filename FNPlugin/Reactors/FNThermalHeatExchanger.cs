using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Propulsion;
using FNPlugin.Constants;
using FNPlugin.Redist;

namespace FNPlugin.Reactors
{
    class FNThermalHeatExchanger : ResourceSuppliableModule, IPowerSource
    {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled = true;

        //Persistent False
        [KSPField(isPersistant = false)]
        public double radius = 2.5;
        [KSPField(isPersistant = false)]
        public double heatTransportationEfficiency = 0.7f;
        [KSPField(isPersistant = false)]
        public double maximumPowerRecieved = 6;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Thermal Power")]
        public string thermalpower;

        // internal
        protected float _thermalpower;


        // reference types
        protected Dictionary<Guid, double> connectedRecievers = new Dictionary<Guid, double>();
        protected Dictionary<Guid, double> connectedRecieversFraction = new Dictionary<Guid, double>();
        protected double connectedRecieversSum;

        protected double storedIsThermalEnergyGeneratorActive;
        protected double currentIsThermalEnergyGeneratorActive;

        public bool SupportMHD { get { return false; } }

        public Part Part { get { return this.part; } }

        public double ThermalPropulsionWasteheatModifier { get { return 1; } }

        public int ProviderPowerPriority { get { return 2; } }

        public double MinimumThrottle { get { return 0; } }

        public double PowerRatio { get { return 1; } }
        public double ConsumedFuelFixed { get { return 0; } }

        public double RawTotalPowerProduced { get { return _thermalpower * TimeWarp.fixedDeltaTime; } }

        public int SupportedPropellantAtoms { get { return GameConstants.defaultSupportedPropellantAtoms; } }

        public int SupportedPropellantTypes { get { return GameConstants.defaultSupportedPropellantTypes; } }

        public bool FullPowerForNonNeutronAbsorbants { get { return true; } }

        public double ReactorSpeedMult { get { return 1; } }

        public double ThermalProcessingModifier { get { return 1; } }

        public double EfficencyConnectedThermalEnergyGenerator { get { return storedIsThermalEnergyGeneratorActive; } }

        public double EfficencyConnectedChargedEnergyGenerator { get { return 0; } }


        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio, bool isMHD) 
        { 
            NotifyActiveThermalEnergyGenerator(efficency, power_ratio);
        }

        public void NotifyActiveThermalEnergyGenerator(double efficency, double power_ratio)
        { 
            currentIsThermalEnergyGeneratorActive = efficency; 
        }

        public void NotifyActiveChargedEnergyGenerator(double efficency, double power_ratio) { }

        public bool IsThermalSource { get { return true; } }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType) { return false; }

        public double ChargedPowerRatio { get { return 0; } }

        public double RawMaximumPower { get { return maximumPowerRecieved; } }

        public double NormalisedMaximumPower { get { return MaximumPower; } }

        public IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        public IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        //-----------------------------------------------------------------------------------------------

        public void AttachThermalReciever(Guid key, double radius)
        {
            try
            {
                UnityEngine.Debug.Log("[KSPI]: InterstellarReactor.ConnectReciever: Guid: " + key + " radius: " + radius);

                if (!connectedRecievers.ContainsKey(key))
                {
                    connectedRecievers.Add(key, radius);
                    connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                    connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
                }
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[KSPI]: InterstellarReactor.ConnectReciever exception: " + error.Message);
            }
        }

        public void DetachThermalReciever(Guid key)
        {
            if (connectedRecievers.ContainsKey(key))
            {
                connectedRecievers.Remove(key);
                connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
            }
        }

        public double GetFractionThermalReciever(Guid key)
        {
            double result;
            if (connectedRecieversFraction.TryGetValue(key, out result))
                return result;
            else
                return 0;
        }

        public double ProducedThermalHeat { get { return 0; } }

        public double ProducedChargedPower { get { return 0; } }

        private double _consumedThermalHeat;
        public double RequestedThermalHeat
        {
            get { return _consumedThermalHeat; }
            set { _consumedThermalHeat = value; }
        }

        public void ConnectWithEngine(IEngineNoozle engine) { }

        public void DisconnectWithEngine(IEngineNoozle engine) { }

        public double ProducedWasteHeat { get { return 0; } }

        public double PowerBufferBonus { get { return 0; } }

        public double ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public double ThermalPropulsionEfficiency { get { return 1; } }
        public double PlasmaPropulsionEfficiency { get { return 0; } }
        public double ChargedParticlePropulsionEfficiency { get { return 0; } }

        public double ThermalEnergyEfficiency { get { return 1; } }
        public double PlasmaEnergyEfficiency { get { return 0; } }
        public double ChargedParticleEnergyEfficiency { get { return 0; } }

        public bool IsSelfContained { get { return false; } }

        public double CoreTemperature { get { return 1500; } }

        public double HotBathTemperature { get { return CoreTemperature * 1.5f; } }

        public double StableMaximumReactorPower { get { return MaximumThermalPower; } }

        public double MaximumPower { get { return MaximumThermalPower; } }

        public double MaximumThermalPower { get { return _thermalpower; } }

        public virtual double MaximumChargedPower { get { return 0; } }

        public virtual float RequestedEngineThrottle { get; set; }

        public double MinimumPower { get { return 0; } }

        public bool IsVolatileSource { get { return false; } }

        public bool IsActive { get { return IsEnabled; } }

        public bool IsNuclear { get { return false; } }


        [KSPEvent(guiActive = true, guiName = "Activate Heat Exchanger", active = false)]
        public void ActivateHeatExchanger()
        {
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Heat Exchanger", active = true)]
        public void DeactivateHeatExchanger()
        {
            IsEnabled = false;
        }

        [KSPAction("Activate Heat Exchanger")]
        public void ActivateHeatExchangerAction(KSPActionParam param)
        {
            ActivateHeatExchanger();
        }

        [KSPAction("Deactivate Heat Exchanger")]
        public void DeactivateHeatExchangerAction(KSPActionParam param)
        {
            DeactivateHeatExchanger();
        }

        [KSPAction("Toggle Heat Exchanger")]
        public void ToggleHeatExchangerAction(KSPActionParam param)
        {
            IsEnabled = !IsEnabled;
        }

        int activeExchangers = 0;

        public void setupThermalPower()
        {
            activeExchangers = FNThermalHeatExchanger.getActiveExchangersForVessel(vessel);
            _thermalpower = (float)getStableResourceSupply(ResourceManager.FNRESOURCE_THERMALPOWER) / activeExchangers;
        }

        public override void OnStart(PartModule.StartState state)
        {
            Actions["ActivateHeatExchangerAction"].guiName = Events["ActivateHeatExchanger"].guiName = String.Format("Activate Heat Exchanger");
            Actions["DeactivateHeatExchangerAction"].guiName = Events["DeactivateHeatExchanger"].guiName = String.Format("Deactivate Heat Exchanger");
            Actions["ToggleHeatExchangerAction"].guiName = String.Format("Toggle Heat Exchanger");

            String[] resources_to_supply = { ResourceManager.FNRESOURCE_THERMALPOWER };
            this.resources_to_supply = resources_to_supply;

            base.OnStart(state);

            if (state == StartState.Editor) { return; }

            UnityEngine.Debug.Log("[KSPI]: FNThermalHeatExchanger on " + part.name + " was Force Activated");
            this.part.force_activate();

            setupThermalPower();
        }

        public override void OnUpdate()
        {
            Events["ActivateHeatExchanger"].active = !IsEnabled;
            Events["DeactivateHeatExchanger"].active = IsEnabled;

            thermalpower = _thermalpower.ToString() + "MW";
        }

        public override void OnFixedUpdate()
        {
            storedIsThermalEnergyGeneratorActive = currentIsThermalEnergyGeneratorActive;
            currentIsThermalEnergyGeneratorActive = 0;

            base.OnFixedUpdate();
            setupThermalPower();
        }

        public double GetCoreTempAtRadiatorTemp(double radTemp)
        {
            return 1500;
        }

        public double GetThermalPowerAtTemp(double temp)
        {
            return _thermalpower;
        }

        public double Radius
        {
            get { return radius; }
        }

        public bool isActive()
        {
            return IsEnabled;
        }

        public void EnableIfPossible()
        {
            IsEnabled = true;
        }

        public bool shouldScaleDownJetISP()
        {
            return false;
        }

        public bool isVolatileSource()
        {
            return false;
        }

        public float getMinimumThermalPower()
        {
            return 0;
        }

        public static int getActiveExchangersForVessel(Vessel vess)
        {
            int activeExchangers = 0;
            List<FNThermalHeatExchanger> mthes = vess.FindPartModulesImplementing<FNThermalHeatExchanger>();
            foreach (FNThermalHeatExchanger mthe in mthes)
            {
                if (mthe.isActive())
                {
                    activeExchangers++;
                }
            }
            return activeExchangers;
        }
    }
}