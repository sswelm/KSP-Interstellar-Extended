using FNPlugin.Extensions;
using FNPlugin.Propulsion;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin
{
    class ISRUScoop : ResourceSuppliableModule
    {
        public const string GROUP = "ISRUScoop";
        public const string GROUP_TITLE = "#LOC_KSPIE_ISRUScoop_groupName";

        // persistent fields
        [KSPField(isPersistant = true)]
        public bool scoopIsEnabled;
        [KSPField(isPersistant = true)]
        public int currentresource;
        [KSPField(isPersistant = true)]
        public double last_active_time;
        [KSPField(isPersistant = true)]
        public double last_power_percentage ;

        // part proterties
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ISRUScoop_ScoopedAir", guiFormat = "F6")]//Scooped Air
        public double scoopair = 0;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = false)]
        public double powerReqMult = 1;
        [KSPField(groupName = GROUP, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ISRUScoop_Mass", guiUnits = " t")]//Mass
        public float partMass = 0;

        // GUI
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_AtmosphericDensity")]//Density
        public string atmosphericDensity;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_resoucesflow")]//Collected
        public string resflow;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_Resource")]//Resource
        public string currentresourceStr;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_Percentage", guiUnits = "%")]//Percentage
        public double rescourcePercentage;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_Storage")]//Storage
        public string resourceStoragename;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_RecievedPower")]//Power
        public string recievedPower;
        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_TraceAtmosphere")]//Trace Atmosphere
        public string densityFractionOfUpperAthmosphere;


        // internals
        protected double resflowf = 0;

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_ActivateScoop", active = true)]//Activate Scoop
        public void ActivateScoop()
        {
            scoopIsEnabled = true;
            OnUpdate();
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_DisableScoop", active = true)]//Disable Scoop
        public void DisableScoop()
        {
            scoopIsEnabled = false;
            OnUpdate();
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_ISRUScoop_ToggleResource", active = true)]//Toggle Resource
        public void ToggleResource()
        {
            currentresource++;

            if (AtmosphericResourceHandler.GetAtmosphericResourceName(vessel.mainBody, currentresource) == null
                && AtmosphericResourceHandler.GetAtmosphericResourceContent(vessel.mainBody, currentresource) > 0
                && currentresource != 0)
            {
                ToggleResource();
            }

            if (currentresource >= AtmosphericResourceHandler.GetAtmosphericCompositionForBody(vessel.mainBody).Count)
                currentresource = 0;

            resflow = String.Empty;
            resflowf = 0;
        }

        [KSPAction("Activate Scoop")]
        public void ActivateScoopAction(KSPActionParam param)
        {
            ActivateScoop();
        }

        [KSPAction("Disable Scoop")]
        public void DisableScoopAction(KSPActionParam param)
        {
            DisableScoop();
        }

        [KSPAction("Toggle Scoop")]
        public void ToggleScoopAction(KSPActionParam param)
        {
            if (scoopIsEnabled)
                DisableScoop();
            else
                ActivateScoop();
        }

        [KSPAction("Toggle Resource")]
        public void ToggleToggleResourceAction(KSPActionParam param)
        {
            ToggleResource();
        }

        public override void OnStart(PartModule.StartState state)
        {
            Actions["ToggleToggleResourceAction"].guiName = Events["ToggleResource"].guiName = Localizer.Format("#LOC_KSPIE_ISRUScoop_ToggleResource");//String.Format("Toggle Resource")

            if (state == StartState.Editor)  return;

            Debug.Log("[KSPI]: ISRUScoop on " + part.name + " was Force Activated");
            this.part.force_activate();

            // verify if body has atmosphere at all
            if (!vessel.mainBody.atmosphere) return;

            // verify scoop was enabled
            if (!scoopIsEnabled) return;

            // verify a timestamp is available
            if (last_active_time == 0) return;

            // verify any power was available in previous save
            if (last_power_percentage < 0.01) return;

            // verify altitude is not too high
            if (vessel.altitude > (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody) * PluginHelper.MaxAtmosphericAltitudeMult))
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ISRUScoop_Altitudetoohigh"), 10, ScreenMessageStyle.LOWER_CENTER);//"Vessel is too high for resource accumulation"
                return;
            }

            // verify altitude is not too low
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)))
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ISRUScoop_Altitudetoolow"), 10, ScreenMessageStyle.LOWER_CENTER);//"Vessel is too low for resource accumulation"
                return;
            }

            // verify eccentricity
            if (vessel.orbit.eccentricity > 0.1)
            {
                string message = Localizer.Format("#LOC_KSPIE_ISRUScoop_Eccentricitytoohigh", vessel.orbit.eccentricity.ToString("0.0000"));//"Eccentricity of <<1>> is too High for resource accumulations"
                ScreenMessages.PostScreenMessage(message, 10.0f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // verify that an electric or Thermal engine is available with high enough ISP
            var highIspEngine = part.vessel.parts.Find(p =>
                p.FindModulesImplementing<ElectricEngineControllerFX>().Any(e => e.baseISP > 4200) ||
                p.FindModulesImplementing<ThermalEngineController>().Any(e => e.AttachedReactor.CoreTemperature > 40000));
            if (highIspEngine == null)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ISRUScoop_NohighenoughISP"), 10, ScreenMessageStyle.LOWER_CENTER);//"No engine available, with high enough Isp and propelant switch ability to compensate for atmospheric drag"
                return;
            }

            // calculate time past since last frame
            double time_diff = (Planetarium.GetUniversalTime() - last_active_time) * 55;

            // scoop atmosphere for entire duration
            ScoopAtmosphere(time_diff, true);
        }

        public override void OnUpdate()
        {
            Events["ActivateScoop"].active = !scoopIsEnabled;
            Events["DisableScoop"].active = scoopIsEnabled;
            Events["ToggleResource"].active = scoopIsEnabled;

            Fields["resflow"].guiActive = scoopIsEnabled;
            Fields["currentresourceStr"].guiActive = scoopIsEnabled;
            Fields["resourceStoragename"].guiActive = scoopIsEnabled;

            double resourcePercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(vessel.mainBody, currentresource)*100;
            string resourceDisplayName = AtmosphericResourceHandler.GetAtmosphericResourceDisplayName(vessel.mainBody, currentresource);
            if (resourceDisplayName != null)
                currentresourceStr = resourceDisplayName + "(" + resourcePercentage + "%)";

            UpdateResourceFlow();
        }

        public override void OnFixedUpdate()
        {
            if (!scoopIsEnabled) return;

            if (!vessel.mainBody.atmosphere) return;

            // scoop atmosphere for a single frame
            ScoopAtmosphere(TimeWarp.fixedDeltaTime, false);

            // store current time in case vessel is unloaded
            last_active_time = Planetarium.GetUniversalTime();
        }

        private void ScoopAtmosphere(double deltaTimeInSeconds, bool offlineCollecting)
        {
            string currentResourceName = AtmosphericResourceHandler.GetAtmosphericResourceName(vessel.mainBody, currentresource);
            string resourceDisplayName = AtmosphericResourceHandler.GetAtmosphericResourceDisplayName(vessel.mainBody, currentresource);

            if (currentResourceName == null)
            {
                resflowf = 0;
                recievedPower = Localizer.Format("#LOC_KSPIE_ISRUScoop_error");//"error"
                densityFractionOfUpperAthmosphere = Localizer.Format("#LOC_KSPIE_ISRUScoop_error");//"error"
                return;
            }

            // map ors resource to kspi resource
            if (PluginHelper.OrsResourceMappings == null || !PluginHelper.OrsResourceMappings.TryGetValue(currentResourceName, out resourceStoragename))
                resourceStoragename = currentResourceName;
            else if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(resourceStoragename))
                resourceStoragename = currentResourceName;

            var definition = PartResourceLibrary.Instance.GetDefinition(resourceStoragename);

            if (definition == null)
                return;

            double resourceDensity = definition.density;
            double maxAltitudeAtmosphere = PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody);

            double upperAtmosphereFraction = Math.Max(0, (vessel.altitude - maxAltitudeAtmosphere) / Math.Max(0.000001, maxAltitudeAtmosphere * PluginHelper.MaxAtmosphericAltitudeMult - maxAltitudeAtmosphere));
            double upperAtmosphereDensity = 1 - upperAtmosphereFraction;

            double airDensity = part.vessel.atmDensity + (PluginHelper.MinAtmosphericAirDensity * upperAtmosphereDensity);
            atmosphericDensity = airDensity.ToString("0.00000000");

            var hydrogenTax = 0.4 * Math.Sin(upperAtmosphereFraction * Math.PI * 0.5);
            var heliumTax = 0.2 * Math.Sin(upperAtmosphereFraction * Math.PI);

            double resourceFraction = (1.0 - hydrogenTax - heliumTax) * AtmosphericResourceHandler.GetAtmosphericResourceContent(vessel.mainBody, currentresource);

            // increase density hydrogen
            if (resourceDisplayName == ResourceSettings.Config.HydrogenGas)
                resourceFraction += hydrogenTax;
            else if (resourceDisplayName == ResourceSettings.Config.Helium4Gas)
                resourceFraction += heliumTax;

            densityFractionOfUpperAthmosphere = upperAtmosphereDensity.ToString("P3");
            rescourcePercentage = resourceFraction * 100;
            if (resourceFraction <= 0 || vessel.altitude > (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody) * PluginHelper.MaxAtmosphericAltitudeMult))
            {
                resflowf = 0;
                recievedPower = Localizer.Format("#LOC_KSPIE_ISRUScoop_off");//"off"
                densityFractionOfUpperAthmosphere = Localizer.Format("#LOC_KSPIE_ISRUScoop_toohigh");//"too high"
                rescourcePercentage = 0;
                return;
            }

            double airspeed = part.vessel.srf_velocity.magnitude + 40.0;
            double air = airspeed * airDensity * 0.001 * scoopair / resourceDensity;
            double scoopedAtm = air * resourceFraction;
            double powerRequirementsInMegawatt = 40.0 * scoopair * PluginHelper.PowerConsumptionMultiplier * powerReqMult;

            if (scoopedAtm > 0 && part.GetResourceSpareCapacity(resourceStoragename) > 0)
            {
                // Determine available power, using EC if below 2 MW required
                double powerReceivedInMegawatt = consumeMegawatts(powerRequirementsInMegawatt, true,
                    false, powerRequirementsInMegawatt < 2.0);

                last_power_percentage = offlineCollecting ? last_power_percentage : powerReceivedInMegawatt / powerRequirementsInMegawatt;
            }
            else
            {
                last_power_percentage = 0;
                powerRequirementsInMegawatt = 0;
            }

            recievedPower = PluginHelper.getFormattedPowerString(last_power_percentage * powerRequirementsInMegawatt) + " / " + PluginHelper.getFormattedPowerString(powerRequirementsInMegawatt);

            double resourceChange = scoopedAtm * last_power_percentage * deltaTimeInSeconds;

            if (offlineCollecting)
            {
                string format = resourceChange > 100 ? "0" : "0.00";
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ISRUScoop_CollectedMsg") +" " + resourceChange.ToString(format) + " " + resourceStoragename, 10.0f, ScreenMessageStyle.LOWER_CENTER);//Atmospheric Scoop collected
            }

            resflowf = part.RequestResource(resourceStoragename, -resourceChange);
            resflowf = -resflowf / TimeWarp.fixedDeltaTime;
            UpdateResourceFlow();
        }

        private void UpdateResourceFlow()
        {
            resflow = resflowf.ToString("0.0000000");
        }

        public override string getResourceManagerDisplayName()
        {
            return part.partInfo.title;
        }

    }
}
