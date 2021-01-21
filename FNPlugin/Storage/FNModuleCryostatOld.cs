using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using UnityEngine;

namespace FNPlugin
{
    [KSPModule("Cryostat")]
    class ModuleStorageCryostatOld: FNModuleCryostat {}

    [KSPModule("Cryostat")]
    class FNModuleCryostatOld : ResourceSuppliableModule
    {
        public const string GROUP = "FNModuleCryostat";
        public const string GROUP_TITLE = "Interstellar Cryostat";

        // Persistent
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_IFS_Cryostat_Cooling"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_On", enabledText = "#LOC_IFS_Cryostat_Off")]//Cooling--On--Off
        public bool isDisabled = false;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Cryostat_PowerBuffer"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_Off", enabledText = "#LOC_IFS_Cryostat_On")]//Cooling--On--Off
        public bool maintainElectricChargeBuffer = true;

        [KSPField(isPersistant = true)]
        public bool autoConfigElectricChargeBuffer = true;
        [KSPField(isPersistant = true)]
        public double storedTemp = 0;

        // Configuration
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
        private double _receivedPowerKw;
        private double _previousReceivedPowerKw;
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

                var exitingElectricCharge = part.Resources[ResourceSettings.Config.ElectricPowerInKilowatt];

                bool hasHigherThanDefaultBuffer = exitingElectricCharge != null && exitingElectricCharge.maxAmount > powerReqKW / 50;

                //Debug.Log("[KSPI]: FNModuleCryostat: hasHigherThanDefaultBuffer: " + hasHigherThanDefaultBuffer);

                maintainElectricChargeBuffer = hasHigherThanDefaultBuffer.IsFalse();
                autoConfigElectricChargeBuffer = false;
                return;
            }

            part.temperature = storedTemp;
            part.skinTemperature = storedTemp;

            // if electricCharge buffer is missing, add it.
            if (!part.Resources.Contains(ResourceSettings.Config.ElectricPowerInKilowatt))
            {
                var node = new ConfigNode("RESOURCE");
                node.AddValue("name", ResourceSettings.Config.ElectricPowerInKilowatt);
                node.AddValue("maxAmount", requiresPower ? powerReqKW / 50 : 1);
                node.AddValue("amount", requiresPower ? powerReqKW / 50 : 1);
                part.AddResource(node);
            }

            if (!maintainElectricChargeBuffer) return;
            if (Kerbalism.IsLoaded) return;

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceSettings.Config.ElectricPowerInKilowatt, 2));
            resourceBuffers.Init(part);
        }

        private void UpdateElectricChargeBuffer(double currentPowerUsage)
        {
            if (resourceBuffers == null)
                return;

            resourceBuffers.UpdateVariable(ResourceSettings.Config.ElectricPowerInKilowatt, currentPowerUsage);
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
            powerStatusStr = PluginHelper.GetFormattedPowerString(_receivedPowerKw / GameConstants.ecPerMJ) +
                " / " + PluginHelper.GetFormattedPowerString(currentPowerReq / GameConstants.ecPerMJ);
        }

        // FixedUpdate is also called while not staged
        public void FixedUpdate()
        {
            var cryostatResource = part.Resources[resourceName];
            if (cryostatResource == null || double.IsPositiveInfinity(currentPowerReq))
            {
                boiloff = 0;
                return;
            }

            double fixedDeltaTime = TimeWarp.fixedDeltaTime;

            if (!isDisabled && currentPowerReq > 0.0)
            {
                UpdateElectricChargeBuffer(Math.Max(currentPowerReq, 0.1 * powerReqKW));

                _receivedPowerKw = consumeMegawatts(currentPowerReq / GameConstants.ecPerMJ, true, true, true) * GameConstants.ecPerMJ;
            }
            else
                _receivedPowerKw = 0;

            bool hasExtraBoiloff = initializationCountdown == 0 && powerReqKW > 0 && currentPowerReq > 0 && _receivedPowerKw < currentPowerReq && _previousReceivedPowerKw < previousPowerReq;

            var boiloffReduction = !hasExtraBoiloff ? boilOffRate : boilOffRate + (boilOffAddition * (1 - _receivedPowerKw / currentPowerReq));

            boiloff = CheatOptions.IgnoreMaxTemperature ||  boiloffReduction <= 0 ? 0 : boiloffReduction * environmentBoiloff;

            if (boiloff > 1e-10)
            {
                var boilOffAmount = boiloff * fixedDeltaTime;

                cryostatResource.amount = Math.Max(0, cryostatResource.amount - boilOffAmount);

                boiloffStr = boiloff.ToString("0.0000000") + " L/s " + cryostatResource.resourceName;

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
                boiloffStr = "0.0000000 L/s " + cryostatResource.resourceName;
            }

            previousPowerReq = currentPowerReq;
            _previousReceivedPowerKw = _receivedPowerKw;
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
