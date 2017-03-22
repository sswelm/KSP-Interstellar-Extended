using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace FNPlugin
{
    class ModuleStorageCryostat: FNModuleCryostat {}

    [KSPModule("Cyrostat")]
    class FNModuleCryostat : FNResourceSuppliableModule
    {
        // Persistant
        [KSPField(isPersistant = true)]
        bool isDisabled;
        [KSPField(isPersistant = true)]
        public double storedTemp = 0;

        // Confuration
        [KSPField(isPersistant = false)]
        public string resourceName = "";
        [KSPField(isPersistant = false)]
        public string resourceGUIName = "";
        [KSPField(isPersistant = false)]
        public double resourceRatioExp = 0.5;
        [KSPField(isPersistant = false, guiActive = false)]
        public double boilOffRate = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public double powerReqKW = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public float powerReqMult = 1f;
        [KSPField(isPersistant = false)]
        public double boilOffMultiplier = 0;
        [KSPField(isPersistant = false)]
        public double boilOffBase = 10000;
        [KSPField(isPersistant = false)]
        public double boilOffAddition = 0;
        [KSPField(isPersistant = false)]
        public double boilOffTemp = 20.271;
        [KSPField(isPersistant = false)]
        public double convectionMod = 1;
        [KSPField(isPersistant = false)]
        public int maxStoreAmount = 0;

        [KSPField(isPersistant = false)]
        public bool showPower = true;
        [KSPField(isPersistant = false)]
        public bool showBoiloff = true;
        [KSPField(isPersistant = false)]
        public bool showTemp = true;
        [KSPField(isPersistant = false)]
        public bool warningShown = false;

        //GUI
        [KSPField(isPersistant = false)]
        public string StartActionName = "Activate Cooling";
        [KSPField(isPersistant = false)]
        public string StopActionName = "Deactivate Cooling";
        [KSPField(isPersistant = false, guiActive = false, guiName = "Power")]
        public string powerStatusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Boiloff")]
        public string boiloffStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Environment Factor")]
        public double environmentFactor;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Temperature", guiFormat = "F3", guiUnits = " K")]
        public double externalTemperature;
        [KSPField(isPersistant = false, guiActive = false, guiName = "internal boiloff")]
        public double boiloff;

        PartResource _electricCharge_resource;
        PartResource _cryostat_resource;
        double recievedPowerKW;
        double previousRecievedPowerKW;
        double currentPowerReq;
        double previousPowerReq;
        int initializationCountdown;

        float previousDeltaTime;
        double previousPowerUsage;        

        [KSPEvent(guiName = "Deactivate Cooling", guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false)]
        public void Deactivate()
        {
            isDisabled = true;
        }

        [KSPEvent(guiName = "Activate Cooling", guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false)]
        public void Activate()
        {
            isDisabled = false;
        }

        public override void OnStart(PartModule.StartState state)
        {
            Events["Activate"].guiName = StartActionName;
            Events["Deactivate"].guiName = StopActionName;

            this.enabled = true;

            // compensate for stock solar initialisation heating bug
            part.temperature = storedTemp;
            initializationCountdown = 50;

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

            // store reference to local electric charge buffer
            _electricCharge_resource = part.Resources[InterstellarResourcesConfiguration.Instance.ElectricCharge];
        }

        private void UpdateElectricChargeBuffer(double currentPowerUsage)
        {
            if (_electricCharge_resource != null && currentPowerUsage > 0 && TimeWarp.fixedDeltaTime != previousDeltaTime || previousPowerUsage != currentPowerUsage)
            {
                double requiredCapacity = 2 * currentPowerUsage * TimeWarp.fixedDeltaTime;
                double previousCapacity = 2 * currentPowerUsage * previousDeltaTime;
                double bufferRatio = (_electricCharge_resource.amount / _electricCharge_resource.maxAmount);

                _electricCharge_resource.maxAmount = requiredCapacity;

                _electricCharge_resource.amount = TimeWarp.fixedDeltaTime > previousDeltaTime
                    ? Math.Max(0, Math.Min(requiredCapacity, _electricCharge_resource.amount + requiredCapacity - previousCapacity))
                    : Math.Max(0, Math.Min(requiredCapacity, bufferRatio * requiredCapacity));
            }

            previousPowerUsage = currentPowerUsage;
            previousDeltaTime = TimeWarp.fixedDeltaTime;
        }

        public override void OnUpdate()
        {
            if (part.Resources.Contains(resourceName))
                _cryostat_resource = part.Resources[resourceName];
            else
                _cryostat_resource = null;

            if (_cryostat_resource != null)
            {
                var requiresPower = powerReqKW > 0;

                bool coolingIsRelevant = _cryostat_resource.amount > 0 && (boilOffRate > 0 || requiresPower);

                Events["Activate"].active = isDisabled && requiresPower;
                Events["Deactivate"].active = !isDisabled && requiresPower;
                Fields["powerStatusStr"].guiActive = showPower && requiresPower;
                Fields["boiloffStr"].guiActive = showBoiloff && boiloff > 0.00001;
                Fields["externalTemperature"].guiActive = showTemp && coolingIsRelevant;

                if (!coolingIsRelevant)
                    return;

                var atmosphereModifier = convectionMod == -1 ? 0 : convectionMod + (FlightGlobals.getStaticPressure(vessel.transform.position) / 100) / (convectionMod + 1);

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

                    powerStatusStr = currentPowerReq < 1.0e+3
                        ? recievedPowerKW.ToString("0.00") + " KW / " + currentPowerReq.ToString("0.00") + " KW"
                        : currentPowerReq < 1.0e+6
                            ? (recievedPowerKW / 1.0e+3).ToString("0.000") + " MW / " + (currentPowerReq / 1.0e+3).ToString("0.000") + " MW"
                            : (recievedPowerKW / 1.0e+6).ToString("0.000") + " GW / " + (currentPowerReq / 1.0e+6).ToString("0.000") + " GW";
                }
                else
                    currentPowerReq = 0;
            }
            else
            {
                Events["Activate"].active = false;
                Events["Deactivate"].active = false;
                Fields["powerStatusStr"].guiActive = false;
                Fields["boiloffStr"].guiActive = false;
            }
        }

        // FixedUpdate is also called while not staged
        public void FixedUpdate()
        {
            if (initializationCountdown > 0)
            {
                part.temperature = storedTemp;
                initializationCountdown--;
            }

            if (_cryostat_resource == null || _cryostat_resource.amount <= 0.0000001)
            {
                boiloff = 0;
                return;
            }

            if (!isDisabled && currentPowerReq > 0)
            {
                UpdateElectricChargeBuffer(Math.Max(currentPowerReq, 0.01 * powerReqKW));

                var fixedPowerReqKW = currentPowerReq * TimeWarp.fixedDeltaTime;

                double fixedRecievedChargeKW = CheatOptions.InfiniteElectricity 
                    ? fixedPowerReqKW / 1000 
                    : consumeFNResource(fixedPowerReqKW / 1000, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000;

                if (fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += part.RequestResource(FNResourceManager.FNRESOURCE_MEGAJOULES, (fixedPowerReqKW - fixedRecievedChargeKW) / 1000) * 1000;

                if (currentPowerReq < 1000 && fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, fixedPowerReqKW - fixedRecievedChargeKW);

                if (currentPowerReq < 1000 && fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += part.RequestResource(FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, fixedPowerReqKW - fixedRecievedChargeKW);

                recievedPowerKW = fixedRecievedChargeKW / TimeWarp.fixedDeltaTime;
            }
            else
                recievedPowerKW = 0;

            bool hasExtraBoiloff = powerReqKW > 0 && recievedPowerKW < currentPowerReq && previousRecievedPowerKW < previousPowerReq;

            var boiloffReducuction = !hasExtraBoiloff
                    ? boilOffRate
                    : (boilOffRate + (boilOffAddition * (1 - recievedPowerKW / currentPowerReq)));

            boiloff = CheatOptions.IgnoreMaxTemperature ||  boiloffReducuction <= 0 
                ? 0 
                : environmentFactor * boiloffReducuction * boilOffMultiplier * boilOffBase;

            if (boiloff > 0.0000000001)
            {
                _cryostat_resource.amount = Math.Max(0, _cryostat_resource.amount - boiloff * TimeWarp.fixedDeltaTime);
                boiloffStr = boiloff.ToString("0.000000") + " L/s " + _cryostat_resource.resourceName;

                if (hasExtraBoiloff && part.vessel.isActiveVessel && !warningShown)
                {
                    warningShown = true;
                    ScreenMessages.PostScreenMessage("Warning: " + boiloffStr + " Boiloff", 5, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            else
            {
                warningShown = false;
                boiloffStr = "0.000000 L/s " + _cryostat_resource.resourceName;
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
            return "Power Requirements: " + (powerReqKW * 0.1).ToString("0.0") + " KW\n Powered Boil Off Fraction: " + boilOffRate * PluginHelper.SecondsInDay + " /day\n Unpowered Boil Off Fraction: " + (boilOffRate + boilOffAddition) * boilOffMultiplier * PluginHelper.SecondsInDay + " /day";
        }
    }
}

