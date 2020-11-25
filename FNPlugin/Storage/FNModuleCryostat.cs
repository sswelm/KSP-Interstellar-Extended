using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using KSP.Localization;
using System;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Cryostat")]
    class ModuleStorageCryostat: FNModuleCryostat {}

    [KSPModule("Cryostat")]
    class FNModuleCryostat : ResourceSuppliableModule
    {
        public const string GROUP = "FNModuleCryostat";
        public const string GROUP_TITLE = "Interstellar Cryostat";

        // Persistant
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_IFS_Cryostat_Cooling"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_On", enabledText = "#LOC_IFS_Cryostat_Off")]//Cooling--On--Off
        public bool isDisabled = false;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Cryostat_PowerBuffer"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_Off", enabledText = "#LOC_IFS_Cryostat_On")]//Cooling--On--Off
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
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Power")]//Power
        public string powerStatusStr = string.Empty;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Boiloff")]//Boiloff
        public string boiloffStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Temperature", guiFormat = "F2", guiUnits = " K")]//Temperature
        public double externalTemperature;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Internalboiloff")]//internal boiloff
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

            // compensate for stock solar initialization heating issues
            part.temperature = storedTemp;
            requiresPower = powerReqKW > 0;

            isDisabledField = Fields[nameof(isDisabled)];
            boiloffStrField = Fields[nameof(boiloffStr)];
            powerStatusStrField = Fields[nameof(powerStatusStr)];
            externalTemperatureField = Fields[nameof(externalTemperature)];
            maintainElectricChargeBufferField = Fields[nameof(maintainElectricChargeBuffer)];

            if (state == StartState.Editor)
            {
                if (!autoConfigElectricChargeBuffer) return;

                var exitingElectricCharge = part.Resources[ResourceSettings.Config.ElectricChargePower];

                bool hasHigherThanDefaultBuffer = exitingElectricCharge != null && exitingElectricCharge.maxAmount > powerReqKW / 50;

                //Debug.Log("[KSPI]: FNModuleCryostat: hasHigherThanDefaultBuffer: " + hasHigherThanDefaultBuffer);

                maintainElectricChargeBuffer = hasHigherThanDefaultBuffer.IsFalse();
                autoConfigElectricChargeBuffer = false;
                return;
            }

            part.temperature = storedTemp;
            part.skinTemperature = storedTemp;

            // if electricCharge buffer is missing, add it.
            if (!part.Resources.Contains(ResourceSettings.Config.ElectricChargePower))
            {
                var node = new ConfigNode("RESOURCE");
                node.AddValue("name", ResourceSettings.Config.ElectricChargePower);
                node.AddValue("maxAmount", requiresPower ? powerReqKW / 50 : 1);
                node.AddValue("amount", requiresPower ? powerReqKW / 50 : 1);
                part.AddResource(node);
            }

            if (maintainElectricChargeBuffer)
            {
                resourceBuffers = new ResourceBuffers();
                resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricChargePower, 2));
                resourceBuffers.Init(this.part);
            }
        }

        private void UpdateElectricChargeBuffer(double currentPowerUsage)
        {
            if (resourceBuffers == null)
                return;

            resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricChargePower, currentPowerUsage);
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

                double atmosphereModifier = convectionMod == -1 ? 0 : convectionMod + part.atmDensity / (convectionMod + 1);

                externalTemperature = part.temperature;
                if (Double.IsNaN(externalTemperature) || Double.IsInfinity(externalTemperature))
                {
                    part.temperature = part.skinTemperature;
                    externalTemperature = part.skinTemperature;
                }

                var temperatureModifier = Math.Max(0, externalTemperature - boilOffTemp) / 300;

                environmentFactor = atmosphereModifier * temperatureModifier;

                if (powerReqKW > 0)
                {
                    currentPowerReq = powerReqKW * 0.2 * environmentFactor * powerReqMult;
                    UpdatePowerStatusString();
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

        private void UpdatePowerStatusString()
        {
            powerStatusStr = PluginHelper.getFormattedPowerString(recievedPowerKW / GameConstants.ecPerMJ) +
                " / " + PluginHelper.getFormattedPowerString(currentPowerReq / GameConstants.ecPerMJ);
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

            double fixedDeltaTime = TimeWarp.fixedDeltaTime;

            if (!isDisabled && currentPowerReq > 0.0)
            {
                UpdateElectricChargeBuffer(Math.Max(currentPowerReq, 0.1 * powerReqKW));

                recievedPowerKW = consumeMegawatts(currentPowerReq /
                    GameConstants.ecPerMJ, true, true, true) * GameConstants.ecPerMJ;
            }
            else
                recievedPowerKW = 0;

            bool hasExtraBoiloff = initializationCountdown == 0 && powerReqKW > 0 && currentPowerReq > 0 && recievedPowerKW < currentPowerReq && previousRecievedPowerKW < previousPowerReq;

            var boiloffReduction = !hasExtraBoiloff ? boilOffRate : boilOffRate + (boilOffAddition * (1 - recievedPowerKW / currentPowerReq));

            boiloff = CheatOptions.IgnoreMaxTemperature ||  boiloffReduction <= 0 ? 0 : boiloffReduction * environmentBoiloff;

            if (boiloff > 1e-10)
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
            double envMod = ((convectionMod <= -1.0) ? 0.0 : convectionMod + 1.0 /
                (convectionMod + 1.0)) * Math.Max(0.0, 300.0 - boilOffTemp) / 300.0;
            return string.Format("{0} @ {1:F1} K\nPower Requirements: {2:F1} KW", resourceName,
                boilOffTemp, powerReqKW * 0.2 * powerReqMult * envMod);
        }
    }
}

