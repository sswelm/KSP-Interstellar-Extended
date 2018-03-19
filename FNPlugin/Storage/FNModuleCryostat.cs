using System;


namespace FNPlugin
{
    class ModuleStorageCryostat: FNModuleCryostat {}

    [KSPModule("Cryostat")]
    class FNModuleCryostat : ResourceSuppliableModule
    {
        // Persistant
        [KSPField(isPersistant = true, guiActive = true, guiName = "Cooling"), UI_Toggle(disabledText = "On", enabledText = "Off")]
        public bool isDisabled = false;
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
        public int initializationCountdown = 1000;

        //GUI
        [KSPField(isPersistant = false, guiActive = false, guiName = "Power")]
        public string powerStatusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Boiloff")]
        public string boiloffStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Temperature", guiFormat = "F3", guiUnits = " K")]
        public double externalTemperature;
        [KSPField(isPersistant = false, guiActive = false, guiName = "internal boiloff")]
        public double boiloff;

        private BaseField isDisabledField;
        private BaseField boiloffStrField;
        private BaseField powerStatusStrField;
        private BaseField externalTemperatureField;
            
        private double environmentBoiloff;
        private double environmentFactor;
        private double recievedPowerKW;
        private double previousRecievedPowerKW;
        private double currentPowerReq;
        private double previousPowerReq;
        private double previousPowerUsage;
       
        private bool requiresPower;
        private float previousDeltaTime;

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

            if (state == StartState.Editor)
                return;

            // if electricCharge buffer is missing, add it.
            if (!part.Resources.Contains(InterstellarResourcesConfiguration.Instance.ElectricCharge))
            {
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", InterstellarResourcesConfiguration.Instance.ElectricCharge);
                node.AddValue("maxAmount", powerReqKW > 0 ? powerReqKW / 50 : 1);
                node.AddValue("amount", powerReqKW > 0  ? powerReqKW / 50 : 1);
                part.AddResource(node);
            }            
        }

        private void UpdateElectricChargeBuffer(double currentPowerUsage)
        {
            var _electricCharge_resource = part.Resources[InterstellarResourcesConfiguration.Instance.ElectricCharge];
            if (_electricCharge_resource != null && (TimeWarp.fixedDeltaTime != previousDeltaTime || previousPowerUsage != currentPowerUsage))
            {
                var requiredCapacity = 2 * currentPowerUsage * TimeWarp.fixedDeltaTime;
                var bufferRatio = _electricCharge_resource.maxAmount > 0 ? _electricCharge_resource.amount / _electricCharge_resource.maxAmount : 0;

                _electricCharge_resource.maxAmount = requiredCapacity;
                _electricCharge_resource.amount =  bufferRatio * requiredCapacity;
            }

            previousPowerUsage = currentPowerUsage;
            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        public void Update()
        {
            var cryostat_resource = part.Resources[resourceName];

            if (cryostat_resource != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    isDisabledField.guiActiveEditor = true;
                    return;
                }

                isDisabledField.guiActive = true;

                bool coolingIsRelevant = cryostat_resource.amount > 0.0000001 && (boilOffRate > 0 || requiresPower);

                powerStatusStrField.guiActive = showPower && requiresPower;
                boiloffStrField.guiActive = showBoiloff && boiloff > 0.00001;
                externalTemperatureField.guiActive = showTemp && coolingIsRelevant;

                if (!coolingIsRelevant)
                    return;

                var atmosphereModifier = convectionMod == -1 ? 0 : convectionMod + part.atmDensity / (convectionMod + 1);

                externalTemperature = part.temperature;
                if (Double.IsNaN(externalTemperature))
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

            if (initializationCountdown > 0)
            {
                part.temperature = storedTemp;
                part.skinTemperature = storedTemp;
                initializationCountdown--;
            }
            else
            {
                storedTemp = part.temperature;
            }

            if (!isDisabled &&  currentPowerReq > 0)
            {
                UpdateElectricChargeBuffer(Math.Max(currentPowerReq, 0.1 * powerReqKW));

                var fixedPowerReqKW = currentPowerReq * TimeWarp.fixedDeltaTime;

                var fixedRecievedChargeKW = CheatOptions.InfiniteElectricity 
                    ? fixedPowerReqKW
                    : consumeFNResource(fixedPowerReqKW / 1000, ResourceManager.FNRESOURCE_MEGAJOULES) * 1000;

                if (fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += part.RequestResource(ResourceManager.FNRESOURCE_MEGAJOULES, (fixedPowerReqKW - fixedRecievedChargeKW) / 1000) * 1000;

                if (currentPowerReq < 1000 && fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, fixedPowerReqKW - fixedRecievedChargeKW);

                recievedPowerKW = fixedRecievedChargeKW / TimeWarp.fixedDeltaTime;
            }
            else
                recievedPowerKW = 0;

            bool hasExtraBoiloff = initializationCountdown == 0 && powerReqKW > 0 && currentPowerReq > 0 && recievedPowerKW < currentPowerReq && previousRecievedPowerKW < previousPowerReq;

            var boiloffReducuction = !hasExtraBoiloff ? boilOffRate : boilOffRate + (boilOffAddition * (1 - recievedPowerKW / currentPowerReq));

            boiloff = CheatOptions.IgnoreMaxTemperature ||  boiloffReducuction <= 0 ? 0 : boiloffReducuction * environmentBoiloff;

            if (boiloff > 0.0000000001)
            {
                cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - boiloff * TimeWarp.fixedDeltaTime);
                boiloffStr = boiloff.ToString("0.0000000") + " L/s " + cryostat_resource.resourceName;

                if (hasExtraBoiloff && part.vessel.isActiveVessel && !warningShown)
                {
                    warningShown = true;
                    ScreenMessages.PostScreenMessage("Warning: " + boiloffStr + " Boiloff", 5, ScreenMessageStyle.UPPER_CENTER);
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
            return resourceGUIName + " Cryostat";
        }

        public override int getPowerPriority()
        {
            return 2;
        }

        public override string GetInfo()
        {
            return "Power Requirements: " + (powerReqKW * 0.1).ToString("0.0") + " KW\n Powered Boil Off Fraction: " 
                + boilOffRate * PluginHelper.SecondsInDay + " /day\n Unpowered Boil Off Fraction: " + (boilOffRate + boilOffAddition) * boilOffMultiplier * PluginHelper.SecondsInDay + " /day";
        }
    }
}

