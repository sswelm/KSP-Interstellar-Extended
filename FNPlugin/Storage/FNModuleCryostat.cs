using FNPlugin.Power;
using System;
using UnityEngine;
using KSP.Localization;
using FNPlugin.Extensions;

namespace FNPlugin
{
    [KSPModule("Cryostat")]
    class ModuleStorageCryostat: FNModuleCryostat {}

    [KSPModule("Cryostat")]
    class FNModuleCryostat : ResourceSuppliableModule
    {
        // Persistant
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_IFS_Cryostat_Cooling"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_On", enabledText = "#LOC_IFS_Cryostat_Off")]//Cooling--On--Off
        public bool isDisabled = false;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Cryostat_PowerBuffer"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_Off", enabledText = "#LOC_IFS_Cryostat_On")]//Cooling--On--Off
        public bool maintainElectricChargeBuffer = true;

        [KSPField(isPersistant = true)]
        public bool autoConfigElectricChargeBuffer = true;
        [KSPField(isPersistant = true)]
        public double storedTemp = 0;

        // Confiration
        [KSPField]
        public string resourceName = "";
        [KSPField]
        public string resourceGUIName = "";
        [KSPField]
        public double boilOffRate = 0;
        [KSPField]
        public double powerReqKW = 0;
        [KSPField]
        public double powerReqMult = 1;
        [KSPField]
        public double boilOffMultiplier = 0;
        [KSPField]
        public double boilOffBase = 10000;
        [KSPField]
        public double boilOffAddition = 0;
        [KSPField]
        public double boilOffTemp = 20.271;
        [KSPField]
        public double convectionMod = 1;
        [KSPField]
        public bool showPower = true;
        [KSPField]
        public bool showBoiloff = true;
        [KSPField]
        public bool showTemp = true;
        [KSPField]
        public bool warningShown;
        [KSPField]
        public int initializationCountdown = 10;

        //GUI
        [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Power")]//Power
        public string powerStatusStr = string.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Boiloff")]//Boiloff
        public string boiloffStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Temperature", guiFormat = "F3", guiUnits = " K")]//Temperature
        public double externalTemperature;
        [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Internalboiloff")]//internal boiloff
        public double boiloff;

        private BaseField isDisabledField;
        private BaseField boiloffStrField;
        private BaseField powerStatusStrField;
        private BaseField externalTemperatureField;
        private BaseField maintainElectricChargeBufferField;
            
        private double environmentBoiloff;
        private double environmentFactor;
        private double recievedPowerKW;
        private double previousRecievedPowerKW;
        private double currentPowerReq;
        private double previousPowerReq;
        private ResourceBuffers resourceBuffers;
       
        private bool requiresPower;

        public override void OnStart(PartModule.StartState state)
        {
            enabled = true;

            // compensate for stock solar initialisation heating issies
            part.temperature = storedTemp;
            requiresPower = powerReqKW > 0;

            isDisabledField = Fields["isDisabled"];
            boiloffStrField = Fields["boiloffStr"];
            powerStatusStrField = Fields["powerStatusStr"];
            externalTemperatureField = Fields["externalTemperature"];
            maintainElectricChargeBufferField = Fields["maintainElectricChargeBuffer"];

            if (state == StartState.Editor)
            {
                if (autoConfigElectricChargeBuffer)
                {
                    var exitingElectricCharge = part.Resources["ElectricCharge"];

                    bool hasHigherThanDefaultBuffer = exitingElectricCharge != null && exitingElectricCharge.maxAmount > powerReqKW / 50;

                    //Debug.Log("[KSPI]: FNModuleCryostat: hasHigherThanDefaultBuffer: " + hasHigherThanDefaultBuffer);

                    maintainElectricChargeBuffer = hasHigherThanDefaultBuffer.IsFalse();
                    autoConfigElectricChargeBuffer = false;
                }
                return;
            }

            part.temperature = storedTemp;
            part.skinTemperature = storedTemp;

            // if electricCharge buffer is missing, add it.
            if (!part.Resources.Contains(InterstellarResourcesConfiguration.Instance.ElectricCharge))
            {
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", InterstellarResourcesConfiguration.Instance.ElectricCharge);
                node.AddValue("maxAmount", requiresPower ? powerReqKW / 50 : 1);
                node.AddValue("amount", requiresPower ? powerReqKW / 50 : 1);
                part.AddResource(node);
            }

            if (maintainElectricChargeBuffer)
            {
                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(InterstellarResourcesConfiguration.Instance.ElectricCharge, 2));
                resourceBuffers.Init(this.part);
            }
        }

        private void UpdateElectricChargeBuffer(double currentPowerUsage)
        {
            if (resourceBuffers == null)
                return;

            resourceBuffers.UpdateVariable(InterstellarResourcesConfiguration.Instance.ElectricCharge, currentPowerUsage);
            resourceBuffers.UpdateBuffers();
        }

        public void Update()
        {
            storedTemp = part.temperature;
            if (initializationCountdown > 0)
                initializationCountdown--;

            var cryostat_resource = part.Resources[resourceName];

            if (cryostat_resource != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    isDisabledField.guiActiveEditor = true;
                    maintainElectricChargeBufferField.guiActiveEditor = true;
                    return;
                }

                isDisabledField.guiActive = requiresPower;
                maintainElectricChargeBufferField.guiActive = requiresPower;

                bool coolingIsRelevant = cryostat_resource.amount > 0.0000001 && (boilOffRate > 0 || requiresPower);

                powerStatusStrField.guiActive = showPower && requiresPower && coolingIsRelevant;
                boiloffStrField.guiActive = showBoiloff && boiloff > 0.00001;
                externalTemperatureField.guiActive = showTemp && coolingIsRelevant;

                if (!coolingIsRelevant)
                {
                    currentPowerReq = 0;
                    return;
                }

                var atmosphereModifier = convectionMod == -1 ? 0 : convectionMod + part.atmDensity / (convectionMod + 1);

                externalTemperature = part.temperature;
                if (Double.IsNaN(externalTemperature) || Double.IsInfinity(externalTemperature))
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
                {
                    isDisabledField.guiActiveEditor = false;
                    maintainElectricChargeBufferField.guiActiveEditor = false;
                }
                else
                {
                    isDisabledField.guiActive = false;
                    maintainElectricChargeBufferField.guiActive = false;
                }
            }
        }

        private void UpdatePowerStatusSting()
        {
            powerStatusStr = currentPowerReq < 1.0e+3
                ? recievedPowerKW.ToString("0.00") + " KW / " + currentPowerReq.ToString("0.00") + " KW"
                : currentPowerReq < 1.0e+6
                    ? (recievedPowerKW / 1.0e+3).ToString("0.000") + " MW / " + (currentPowerReq / 1.0e+3).ToString("0.000") + " MW"
                    : (recievedPowerKW / 1.0e+6).ToString("0.000") + " GW / " + (currentPowerReq / 1.0e+6).ToString("0.000") + " GW";
        }

        // FixedUpdate is also called while not staged
        public void FixedUpdate()
        {
            var cryostat_resource = part.Resources[resourceName];
            if (cryostat_resource == null || double.IsPositiveInfinity(currentPowerReq))
            {
                boiloff = 0;
                return;
            }

            var fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);

            if (!isDisabled &&  currentPowerReq > 0)
            {
                UpdateElectricChargeBuffer(Math.Max(currentPowerReq, 0.1 * powerReqKW));

                var fixedPowerReqKW = currentPowerReq * fixedDeltaTime;

                var fixedRecievedChargeKW = CheatOptions.InfiniteElectricity 
                    ? fixedPowerReqKW
                    : consumeFNResource(fixedPowerReqKW / 1000, ResourceManager.FNRESOURCE_MEGAJOULES) * 1000;

                if (fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += part.RequestResource(ResourceManager.FNRESOURCE_MEGAJOULES, (fixedPowerReqKW - fixedRecievedChargeKW) / 1000) * 1000;

                if (currentPowerReq < 1000 && fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, fixedPowerReqKW - fixedRecievedChargeKW);

                recievedPowerKW = fixedRecievedChargeKW / fixedDeltaTime;
            }
            else
                recievedPowerKW = 0;

            bool hasExtraBoiloff = initializationCountdown == 0 && powerReqKW > 0 && currentPowerReq > 0 && recievedPowerKW < currentPowerReq && previousRecievedPowerKW < previousPowerReq;

            var boiloffReducuction = !hasExtraBoiloff ? boilOffRate : boilOffRate + (boilOffAddition * (1 - recievedPowerKW / currentPowerReq));

            boiloff = CheatOptions.IgnoreMaxTemperature ||  boiloffReducuction <= 0 ? 0 : boiloffReducuction * environmentBoiloff;

            if (boiloff > 0.0000000001)
            {
                var boilOffAmount = boiloff * fixedDeltaTime;

                cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - boilOffAmount);

                boiloffStr = boiloff.ToString("0.0000000") + " L/s " + cryostat_resource.resourceName;

                if (hasExtraBoiloff && part.vessel.isActiveVessel && !warningShown)
                {
                    warningShown = true;
                    var message = Localizer.Format("#LOC_KSPIE_ModuleCryostat_Postmsg", boiloffStr);//"Warning: " +  + " Boiloff"
                    Debug.LogWarning("[KSPI]: FNModuleCryostat: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            else
            {
                warningShown = false;
                boiloffStr = "0.0000000 L/s " + cryostat_resource.resourceName;
            }

            previousPowerReq = currentPowerReq;
            previousRecievedPowerKW = recievedPowerKW;
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
            return "<size=10>" + resourceName + " @ " + boilOffTemp + " K</size>";

            //return "Power Requirements: " + (powerReqKW * 0.1).ToString("0.0") + " KW\n Powered Boil Off Fraction: " 
            //	+ boilOffRate * PluginHelper.SecondsInDay + " /day\n Unpowered Boil Off Fraction: " + (boilOffRate + boilOffAddition) * boilOffMultiplier * PluginHelper.SecondsInDay + " /day";
        }
    }
}

