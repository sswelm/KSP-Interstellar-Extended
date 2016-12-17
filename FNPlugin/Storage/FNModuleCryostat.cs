using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace FNPlugin
{
    [KSPModule("Cyrostat Tank")]
    class FNModuleCryostat : FNResourceSuppliableModule
    {
        // Persistant
        [KSPField(isPersistant = true)]
        bool isDisabled;
        [KSPField(isPersistant = true)]
        public double storedTemp;

        // Confuration
        [KSPField(isPersistant = false)]
        public string resourceName;
        [KSPField(isPersistant = false)]
        public string resourceGUIName;
        [KSPField(isPersistant = false)]
        public float resourceRatioExp = 0.5f;
        [KSPField(isPersistant = false)]
        public double boilOffRate;
        [KSPField(isPersistant = false, guiActive = false)]
        public float powerReqKW;
        //[KSPField(isPersistant = false)]
        //public float fullPowerReqKW = 0;
        [KSPField(isPersistant = false, guiActive = false)]
        public float powerReqMult = 1f;
        [KSPField(isPersistant = false)]
        public double boilOffMultiplier;
        [KSPField(isPersistant = false)]
        public float boilOffBase = 10000;
        [KSPField(isPersistant = false)]
        public double boilOffAddition;
        [KSPField(isPersistant = false)]
        public float boilOffTemp = 20.271f;
        [KSPField(isPersistant = false)]
        public float convectionMod = 1;
        [KSPField(isPersistant = false)]
        public int maxStoreAmount = 0;

        [KSPField(isPersistant = false)]
        public bool showPower = true;
        [KSPField(isPersistant = false)]
        public bool showBoiloff = true;
        [KSPField(isPersistant = false)]
        public bool showTemp = true;

        //GUI
        [KSPField(isPersistant = false)]
        public string StartActionName = "Activate Cooling";
        [KSPField(isPersistant = false)]
        public string StopActionName = "Deactivate Cooling";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStatusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Boiloff")]
        public string boiloffStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Environment Factor")]
        public double environmentFactor;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Temperature", guiFormat = "F3", guiUnits = " K")]
        public double externalTemperature;

        protected int initializationCountdown;
        protected double boiloff;
        protected PartResource cryostat_resource;
        protected double recievedPowerKW;
        protected double currentPowerReq;

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
        }

        public override void OnUpdate()
        {
            if (part.Resources.Contains(resourceName))
                cryostat_resource = part.Resources[resourceName];
            else
                cryostat_resource = null;

            if (cryostat_resource != null)
            {
                bool coolingIsRelevant = powerReqKW > 0 && cryostat_resource.amount > 0;

                Events["Activate"].active = isDisabled && coolingIsRelevant;
                Events["Deactivate"].active = !isDisabled && coolingIsRelevant;
                Fields["powerStatusStr"].guiActive = showPower && coolingIsRelevant;
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
                    //var resourceRatio = (float)Math.Pow(cryostat_resource.amount / cryostat_resource.maxAmount, resourceRatioExp);
                    //currentPowerReq = fullPowerReqKW > powerReqKW
                    //    ? powerReqKW + (fullPowerReqKW - powerReqKW) * resourceRatio
                    //    : fullPowerReqKW + (powerReqKW - fullPowerReqKW) * (1 - resourceRatio);
                    currentPowerReq = powerReqKW * 0.2f * environmentFactor * powerReqMult;

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

            if (cryostat_resource == null || cryostat_resource.amount <= 0.0000001)
            {
                boiloff = 0;
                return;
            }



            if (!isDisabled && currentPowerReq > 0)
            {
                var fixedPowerReqKW = currentPowerReq * TimeWarp.fixedDeltaTime;

                double fixedRecievedChargeKW = consumeFNResource(fixedPowerReqKW / 1000, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000;

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

            var boiloffReducuction = recievedPowerKW >= currentPowerReq
                    ? boilOffRate
                    : (boilOffRate + (boilOffAddition * (1 - recievedPowerKW / currentPowerReq)));

            boiloff = boiloffReducuction <= 0 ? 0 : environmentFactor * boiloffReducuction * boilOffMultiplier * boilOffBase;

            if (boiloff > 0.000001)
            {
                cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - boiloff * TimeWarp.fixedDeltaTime);
                boiloffStr = boiloff.ToString("0.000000") + " L/s " + cryostat_resource.resourceName;

                ScreenMessages.PostScreenMessage("Warning: " + boiloffStr + " Boiloff", 0.1f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
                boiloffStr = "0.000000 L/s " + cryostat_resource.resourceName;
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

