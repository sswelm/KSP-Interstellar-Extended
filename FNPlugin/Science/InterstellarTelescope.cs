using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;

namespace FNPlugin.Science
{
    class InterstellarTelescope : ModuleModableScienceGenerator
    {
        // Persistent True
        [KSPField(isPersistant = true)] public bool telescopeIsEnabled;
        [KSPField(isPersistant = true)] public double lastActiveTime;
        [KSPField(isPersistant = true)] public double lastMaintained;
        [KSPField(isPersistant = true)] public bool telescopeInit;
        [KSPField(isPersistant = true)] public bool dpo;
        [KSPField(isPersistant = true)] public double helium_depleted_time;
        [KSPField(isPersistant = true)] public double science_awaiting_addition;

        //GUI
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Telescope_Performance")] public string performPcnt = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Telescope_Science")] public string sciencePerDay = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Telescope_GLens")] public string gLensStr = "";

        //Internal
        protected double perform_factor_d;
        protected double perform_exponent;
        protected double science_rate;
        protected double helium_time_scale;

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Telescope_DeepFieldSurvey", active = false)]//Deep Field Survey
        public void BeginOberservations()
        {
            telescopeIsEnabled = true;
            dpo = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Telescope_DirectPlanetaryObservation", active = false)]//Direct Planetary Observation
        public void BeginOberservations2()
        {
            telescopeIsEnabled = true;
            dpo = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Telescope_StopSurvey", active = false)]//Stop Survey
        public void StopOberservations()
        {
            telescopeIsEnabled = false;
        }

        [KSPEvent(guiName = "#LOC_KSPIE_Telescope_PerformMaintenance", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]//Perform Maintenance
        public void MaintainTelescope()
        {
            lastMaintained = Planetarium.GetUniversalTime();
        }

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) return;

            canDeploy = true;

            if (telescopeInit == false || lastMaintained == 0)
            {
                telescopeInit = true;
                lastMaintained = (float)Planetarium.GetUniversalTime();
            }

            if (!telescopeIsEnabled || !(lastActiveTime > 0))
                return;

            CalculateTimeToHeliumDepletion();

            double t0 = lastActiveTime - lastMaintained;
            double t1 = Math.Min(Planetarium.GetUniversalTime(), helium_depleted_time) - lastMaintained;

            if (!(t1 > t0)) return;

            double a = -GameConstants.telescopePerformanceTimescale;
            double baseScience = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
            double timeDiff = Math.Min(Planetarium.GetUniversalTime(), helium_depleted_time) - lastActiveTime;
            double avgScienceRate = 0.5 * baseScience * ( Math.Exp(a * t1)  + Math.Exp(a * t0) );
            double scienceToAdd = avgScienceRate / 28800 * timeDiff;
            lastActiveTime = Planetarium.GetUniversalTime();
            science_awaiting_addition += scienceToAdd;
        }

        protected override bool GenerateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            if (experiment == null) return false;

            if (science_awaiting_addition > 0)
            {
                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.InSpaceHigh, vessel.mainBody, "", "");
                if (subject == null)
                    return false;

                subject.subjectValue = PluginHelper.GetScienceMultiplier(vessel);
                subject.scienceCap = 167 * subject.subjectValue;   //PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex,false);
                subject.dataScale = 1.25f;

                float remainingBaseScience = (subject.scienceCap - subject.science) / subject.subjectValue;
                science_awaiting_addition = Math.Min(science_awaiting_addition, remainingBaseScience);

                // transmission of zero data breaks the experiment result dialog box
                data_size = Math.Max(float.Epsilon, science_awaiting_addition * subject.dataScale);
                scienceData = new ScienceData((float)data_size, 1, 0, subject.id, "Infrared Telescope Data");

                result_title = Localizer.Format("#LOC_KSPIE_Telescope_Resulttitle");//"Infrared Telescope Experiment"
                result_string = Localizer.Format("#LOC_KSPIE_Telescope_Resultmsg", vessel.mainBody.name);//"Infrared telescope observations were recovered from the vicinity of " +  + "."

                recovery_value = science_awaiting_addition;
                transmit_value = recovery_value;
                xmit_scalar = 1;
                ref_value = subject.scienceCap;

                return true;
            }
            return false;
        }

        protected override void CleanUpScienceData()
        {
            science_awaiting_addition = 0;
        }

        private CelestialBody localStar;
        public CelestialBody LocalStar
        {
            get
            {
                if (localStar == null)
                    localStar = vessel.GetLocalStar();

                return localStar;
            }
        }

        private CelestialBody homeworld;
        public CelestialBody Homeworld
        {
            get
            {
                if (homeworld != null)
                    return homeworld;

                var planetarium = Planetarium.fetch;
                if (planetarium != null)
                    homeworld = planetarium.Home;
                return homeworld;
            }
        }

        public override void OnUpdate()
        {
            if (vessel.IsInAtmosphere()) telescopeIsEnabled = false;

            Events[nameof(BeginOberservations)].active = !vessel.IsInAtmosphere() && !telescopeIsEnabled;
            Events[nameof(StopOberservations)].active = telescopeIsEnabled;
            Fields[nameof(sciencePerDay)].guiActive = telescopeIsEnabled;
            performPcnt = (perform_factor_d * 100).ToString("0.0") + "%";
            sciencePerDay = (science_rate * 28800 * PluginHelper.GetScienceMultiplier(vessel)).ToString("0.00") + " "+Localizer.Format("#LOC_KSPIE_Telescope_ScienceperDay");//Science/Day

            List<ITelescopeController> telescopeControllers = vessel.FindPartModulesImplementing<ITelescopeController>();

            if (telescopeControllers.Any(tscp => tscp.CanProvideTelescopeControl))
            {
                double currentAu = Vector3d.Distance(vessel.transform.position, LocalStar.position) / Vector3d.Distance(Homeworld.position, LocalStar.position);

                if (currentAu >= 548 && !vessel.IsInAtmosphere())
                {
                    if (vessel.orbit.eccentricity < 0.8)
                    {
                        Events[nameof(BeginOberservations2)].active = true;
                        gLensStr = (telescopeIsEnabled && dpo) ? Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu1") : Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu2");//"Ongoing.""Available"
                    }
                    else
                    {
                        Events[nameof(BeginOberservations2)].active = false;
                        gLensStr = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu3", vessel.orbit.eccentricity.ToString("0.0"));//"Eccentricity: " +  + "; < 0.8 Required"
                    }
                }
                else
                {
                    Events[nameof(BeginOberservations2)].active = false;
                    gLensStr = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu4", currentAu.ToString("0.0"));// + " AU; Required 548 AU"
                }
            }
            else
            {
                Events[nameof(BeginOberservations2)].active = false;
                gLensStr = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu5");//"Science Lab/Computer Core required"
            }

            if (helium_time_scale <= 0) performPcnt = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu6");//"Helium Coolant Deprived."

        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            CalculateTimeToHeliumDepletion();

            if (ResearchAndDevelopment.Instance == null)
                return;

            if (helium_time_scale <= 0) telescopeIsEnabled = false;

            perform_exponent = -(Planetarium.GetUniversalTime() - lastMaintained) * GameConstants.telescopePerformanceTimescale;
            perform_factor_d = Math.Exp(perform_exponent);

            if (telescopeIsEnabled)
            {
                double baseScience = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                science_rate = baseScience * perform_factor_d / 28800;
                if (!double.IsNaN(science_rate) && !double.IsInfinity(science_rate))
                    science_awaiting_addition += science_rate * TimeWarp.fixedDeltaTime;

                lastActiveTime = Planetarium.GetUniversalTime();
            }
        }

        private void CalculateTimeToHeliumDepletion()
        {
            var heliumResources = part.GetConnectedResources(ResourceSettings.Config.Helium4Lqd).ToList();
            var maxHelium = heliumResources.Sum(hr => hr.maxAmount);
            var curHelium = heliumResources.Sum(hr => hr.amount);
            var heliumFraction = (maxHelium > 0) ? curHelium / maxHelium : curHelium;
            helium_time_scale = 1.0 / GameConstants.helium_boiloff_fraction * heliumFraction;
            helium_depleted_time = helium_time_scale + Planetarium.GetUniversalTime();
        }
    }
}
