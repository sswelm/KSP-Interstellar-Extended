using FNPlugin.Constants;
using FNPlugin.Powermanagement;
using KSP.Localization;
using System;
using TweakScale;

namespace FNPlugin
{
    [KSPModule("Cryostat")]
    class ModuleStorageCryostat: FNModuleCryostat {}

    [KSPModule("Cryostat")]
    class FNModuleCryostat : ResourceSuppliableModule, IRescalable<FNModuleCryostat>
    {
        public const string Group = "IFSCryostat";
        public const string GroupTitle = "#LOC_IFS_Cryostat_groupName";
        public const string StockResourceElectricCharge = "ElectricCharge";

        // Persistent
        [KSPField(isPersistant = true)]
        public double storedTemp = 0;

        // Configuration
        [KSPField] public string resourceName = "";
        [KSPField] public string resourceGUIName = "";
        [KSPField] public double boilOffRate = 0;
        [KSPField] public double powerReqKW = 0;
        [KSPField] public double powerReqMult = 1;
        [KSPField] public double boilOffMultiplier = 0;
        [KSPField] public double boilOffBase = 10000;
        [KSPField] public double boilOffAddition = 0;
        [KSPField] public double boilOffTemp = 20.271;
        [KSPField] public double convectionMod = 1;
        [KSPField] public bool showPower = true;
        [KSPField] public bool showBoiloff = true;
        [KSPField] public bool showTemp = true;
        [KSPField] public bool warningShown;
        [KSPField] public int initializationCountdown = 10;
        [KSPField] public double kerbalismBoiloffMultiplier = 1000;

        [KSPField] public string coolingPostfix = "Cooling";
        [KSPField] public string heatingPostfix = "Heating";
        [KSPField] public string boiloffPostfix = "Boiloff";
        [KSPField] public string boiloffPrefix = "_";

        //GUI
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_IFS_Cryostat_Power")]//Power
        public string powerStatusStr = string.Empty;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_IFS_Cryostat_Boiloff")]//Boiloff
        public string boiloffStr;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_IFS_Cryostat_Temperature", guiFormat = "F0", guiUnits = " K")]//Temperature
        public double externalTemperature;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_IFS_Cryostat_internalboiloff")]//internal boiloff
        public double currentBoiloff;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_IFS_Cryostat_Cooling"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_On", enabledText = "#LOC_IFS_Cryostat_Off")]//Cooling--On--Off
        public bool isDisabled = false;

        private BaseField isDisabledField;
        private BaseField boiloffStrField;
        private BaseField powerStatusStrField;
        private BaseField externalTemperatureField;

        private double environmentBoiloff;
        private double environmentFactor;
        private double receivedPowerKw;
        private double previousReceivedPowerKw;
        private double currentPowerReq;
        private double previousPowerReq;
        private double previousPowerUsage;
        private double maxBoiloff;

        private bool requiresPower;
        private float previousDeltaTime;
        private string boiloffResourceName;
        private string coolingResourceName;
        private string heatingResourceName;

        private ScalingFactor _factor;
        private PartResourceDefinition _electricChargeDefinition;

        public override void OnStart(StartState state)
        {
            enabled = true;

            boiloffResourceName = boiloffPrefix + resourceName + boiloffPostfix;
            coolingResourceName = "_" + resourceName + coolingPostfix;
            heatingResourceName = "_" + resourceName + heatingPostfix;

            _electricChargeDefinition = PartResourceLibrary.Instance.GetDefinition(StockResourceElectricCharge);

            // compensate for stock solar initialization heating issues
            part.temperature = storedTemp;
            requiresPower = powerReqKW > 0;

            isDisabledField = Fields[nameof(isDisabled)];
            boiloffStrField = Fields[nameof(boiloffStr)];
            powerStatusStrField = Fields[nameof(powerStatusStr)];
            externalTemperatureField = Fields[nameof(externalTemperature)];

            if (state == StartState.Editor)
                return;

            part.temperature = storedTemp;
            part.skinTemperature = storedTemp;

            // if electricCharge buffer is missing, add it.
            if (part.Resources.Contains(StockResourceElectricCharge)) return;

            ConfigNode node = new ConfigNode("RESOURCE");
            node.AddValue("name", StockResourceElectricCharge);
            node.AddValue("maxAmount", powerReqKW > 0 ? powerReqKW / 50 : 1);
            node.AddValue("amount", powerReqKW > 0 ? powerReqKW / 50 : 1);
            part.AddResource(node);
        }

        private void UpdateElectricChargeBuffer(double currentPowerUsage)
        {
            if (Kerbalism.IsLoaded)
                return;

            var electricChargeResource = part.Resources[StockResourceElectricCharge];
            if (electricChargeResource != null && (TimeWarp.fixedDeltaTime != previousDeltaTime || previousPowerUsage != currentPowerUsage))
            {
                var requiredCapacity = 2 * currentPowerUsage * TimeWarp.fixedDeltaTime;
                var bufferRatio = electricChargeResource.maxAmount > 0 ? electricChargeResource.amount / electricChargeResource.maxAmount : 0;

                electricChargeResource.maxAmount = requiredCapacity;
                electricChargeResource.amount = bufferRatio * requiredCapacity;
            }

            previousPowerUsage = currentPowerUsage;
            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        public void Update()
        {
            storedTemp = part.temperature;
            if (initializationCountdown > 0)
                initializationCountdown--;

            var cryostatResource = part.Resources[resourceName];

            if (cryostatResource != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    isDisabledField.guiActiveEditor = true;
                    return;
                }

                isDisabledField.guiActive = powerReqKW > 0;

                bool coolingIsRelevant = cryostatResource.amount > 0.0000001 && (boilOffRate > 0 || requiresPower);

                powerStatusStrField.guiActive = showPower && requiresPower && coolingIsRelevant;
                boiloffStrField.guiActive = showBoiloff && currentBoiloff > 0.0000001;
                externalTemperatureField.guiActive = showTemp && coolingIsRelevant;

                if (!coolingIsRelevant)
                {
                    currentPowerReq = 0;
                    return;
                }

                var atmosphereModifier = convectionMod == -1 ? 0 : convectionMod + part.atmDensity / (convectionMod + 1);

                externalTemperature = part.temperature;
                if (double.IsNaN(externalTemperature) || double.IsInfinity(externalTemperature))
                {
                    part.temperature = part.skinTemperature;
                    externalTemperature = part.skinTemperature;
                }

                var temperatureModifier = Math.Max(0, externalTemperature - boilOffTemp) / 300; //273.15;

                environmentFactor = atmosphereModifier * temperatureModifier;

                if (powerReqKW > 0)
                {
                    currentPowerReq = powerReqKW * 0.2 * environmentFactor * powerReqMult;
                    UpdatePowerStatusSting();
                }
                else
                    currentPowerReq = 0;

                environmentBoiloff = environmentFactor * boilOffMultiplier * boilOffBase;
            }
            else
            {
                boiloffStrField.guiActive = false;
                powerStatusStrField.guiActive = false;

                if (HighLogic.LoadedSceneIsEditor)
                    isDisabledField.guiActiveEditor = false;
                else
                    isDisabledField.guiActive = false;
            }
        }

        private void UpdatePowerStatusSting()
        {
            powerStatusStr = currentPowerReq < 1.0e+3
                ? receivedPowerKw.ToString("0.00") + " KW / " + currentPowerReq.ToString("0.00") + " KW"
                : currentPowerReq < 1.0e+6
                    ? (receivedPowerKw / 1.0e+3).ToString("0.000") + " MW / " + (currentPowerReq / 1.0e+3).ToString("0.000") + " MW"
                    : (receivedPowerKw / 1.0e+6).ToString("0.000") + " GW / " + (currentPowerReq / 1.0e+6).ToString("0.000") + " GW";
        }

        // FixedUpdate is also called while not staged
        public void FixedUpdate()
        {
            var cryostatResource = part.Resources[resourceName];
            if (cryostatResource == null || double.IsPositiveInfinity(currentPowerReq) || double.IsNaN(currentPowerReq))
            {
                maxBoiloff = 0;
                return;
            }

            var fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);

            if (!isDisabled && currentPowerReq > 0)
            {
                UpdateElectricChargeBuffer(Math.Max(currentPowerReq, 0.1 * powerReqKW));

                receivedPowerKw = FixedReceivedChargeKw(currentPowerReq, fixedDeltaTime);
            }
            else
                receivedPowerKw = 0;

            var sizeMultiplier = _factor.absolute.quadratic == 0 ? 1 : _factor.absolute.quadratic;

            maxBoiloff = boilOffRate + boilOffAddition * environmentBoiloff * part.partInfo.partSize * sizeMultiplier;

            bool hasExtraBoiloff = initializationCountdown == 0 && powerReqKW > 0 && currentPowerReq > 0 && receivedPowerKw < currentPowerReq && previousReceivedPowerKw < previousPowerReq;

            var powerRatioModifier = currentPowerReq > 0 ? Math.Min(1, Math.Max(0, receivedPowerKw / currentPowerReq)) : 0;

            currentBoiloff = maxBoiloff * (1 - powerRatioModifier);

            var boiloffResource = part.Resources[boiloffResourceName];
            if (boiloffResource != null)
            {
                boiloffResource.maxAmount = maxBoiloff * kerbalismBoiloffMultiplier;
                boiloffResource.amount = currentBoiloff * kerbalismBoiloffMultiplier;
            }
            else if (hasExtraBoiloff && currentBoiloff > 0.0000000001)
            {
                cryostatResource.amount = Math.Max(0, cryostatResource.amount - currentBoiloff * fixedDeltaTime);
            }
            else
            {
                warningShown = false;
                currentBoiloff = 0;
            }

            boiloffStr = currentBoiloff.ToString("0.000000") + " U/s " + cryostatResource.resourceName;

            if (currentBoiloff > 0 && hasExtraBoiloff && part.vessel.isActiveVessel && !warningShown)
            {
                warningShown = true;
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_IFS_Cryostat_boiloffMsg", boiloffStr), 5, ScreenMessageStyle.UPPER_CENTER);//"Warning: <<1>> Boiloff"
            }

            previousPowerReq = currentPowerReq;
            previousReceivedPowerKw = receivedPowerKw;
        }

        private double FixedReceivedChargeKw(double powerReqKw, double deltaTime)
        {
            if (powerReqKw <= float.Epsilon)
                return powerReqKw;

            if (CheatOptions.InfiniteElectricity)
                return powerReqKw;

            var coolingResource = part.Resources[coolingResourceName];
            var heatingResource = part.Resources[heatingResourceName];

            if (coolingResource != null && heatingResource != null)
            {
                coolingResource.maxAmount = powerReqKw;
                coolingResource.amount = powerReqKw;
                heatingResource.maxAmount = powerReqKw;
                heatingResource.amount = powerReqKw;

                return part.RequestResource(_electricChargeDefinition.id, powerReqKw * deltaTime, true) / deltaTime;
            }

            var receivedChargeKw = consumeMegawatts(powerReqKw / GameConstants.ecPerMJ, true, true, true) * GameConstants.ecPerMJ;
            if (receivedChargeKw <= powerReqKw)
                receivedChargeKw += part.RequestResource(_electricChargeDefinition.id, powerReqKw * deltaTime) / deltaTime;

            return receivedChargeKw;
        }

        public void OnRescale(ScalingFactor factor)
        {
            _factor = factor;
        }

        public override string getResourceManagerDisplayName()
        {
            return resourceGUIName + " " + Localizer.Format("#LOC_KSPIE_ModuleCryostat_Cryostat");//Cryostat
        }

        public override int getPowerPriority()
        {
            return 2;
        }

        public override string GetInfo()
        {
            double envMod = ((convectionMod <= -1.0) ? 0.0 : convectionMod + 1.0 / (convectionMod + 1.0)) * Math.Max(0.0, 300.0 - boilOffTemp) / 300.0;
            return $"{resourceName} @ {boilOffTemp:F1} K\nPower Requirements: {powerReqKW * 0.2 * powerReqMult * envMod:F1} KW";
        }
    }
}
