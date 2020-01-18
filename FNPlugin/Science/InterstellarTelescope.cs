using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Extensions;
using FNPlugin.Constants;
using KSP.Localization;

namespace FNPlugin
{
    class InterstellarTelescope : ModuleModableScienceGenerator
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool telescopeIsEnabled;
        [KSPField(isPersistant = true)]
        public double lastActiveTime;
        [KSPField(isPersistant = true)]
        public double lastMaintained;
        [KSPField(isPersistant = true)]
        public bool telescopeInit;
        [KSPField(isPersistant = true)]
        public bool dpo;
        [KSPField(isPersistant = true)]
        public double helium_depleted_time;
        [KSPField(isPersistant = true)]
        public double science_awaiting_addition;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Telescope_Performance")]//Performance
        public string performPcnt = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Telescope_Science")]//Science
        public string sciencePerDay = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Telescope_GLens")]//G-Lens
        public string gLensStr = "";

        //Internal
        protected double perform_factor_d = 0;
        protected double perform_exponent = 0;
        protected double science_rate = 0;
        protected double helium_time_scale = 0;

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Telescope_DeepFieldSurvey", active = false)]//Deep Field Survey
        public void beginOberservations()
        {
            telescopeIsEnabled = true;
            dpo = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Telescope_DirectPlanetaryObservation", active = false)]//Direct Planetary Observation
        public void beginOberservations2()
        {
            telescopeIsEnabled = true;
            dpo = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Telescope_StopSurvey", active = false)]//Stop Survey
        public void stopOberservations()
        {
            telescopeIsEnabled = false;
        }

        [KSPEvent(guiName = "#LOC_KSPIE_Telescope_PerformMaintenance", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]//Perform Maintenance
        public void maintainTelescope()
        {
            lastMaintained = Planetarium.GetUniversalTime();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            base.canDeploy = true;

            if (telescopeInit == false || lastMaintained == 0)
            {
                telescopeInit = true;
                lastMaintained = (float)Planetarium.GetUniversalTime();
            }

            if (telescopeIsEnabled && lastActiveTime > 0)
            {
                calculateTimeToHeliumDepletion();

                double t0 = lastActiveTime - lastMaintained;
                double t1 = Math.Min(Planetarium.GetUniversalTime(), helium_depleted_time) - lastMaintained;
                if (t1 > t0)
                {
                    double a = -GameConstants.telescopePerformanceTimescale;
                    double base_science = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                    double time_diff = Math.Min(Planetarium.GetUniversalTime(), helium_depleted_time) - lastActiveTime;
                    double avg_science_rate = 0.5*base_science * ( Math.Exp(a * t1)  + Math.Exp(a * t0) );
                    double science_to_add = avg_science_rate / 28800 * time_diff;
                    lastActiveTime = Planetarium.GetUniversalTime();
                    science_awaiting_addition += science_to_add;
                }
            }
        }

        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("ExpInterstellarTelescope");
            if (experiment == null) return false;

            if (science_awaiting_addition > 0)
            {
                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.InSpaceHigh, vessel.mainBody, "", "");
                if (subject == null)
                    return false;

                subject.subjectValue = PluginHelper.getScienceMultiplier(vessel);
                subject.scienceCap = 167 * subject.subjectValue;   //PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex,false);
                subject.dataScale = 1.25f;

                float remaining_base_science = (subject.scienceCap - subject.science) / subject.subjectValue;
                science_awaiting_addition = Math.Min(science_awaiting_addition, remaining_base_science);

                // transmission of zero data breaks the experiment result dialog box
                data_size = Math.Max(float.Epsilon, science_awaiting_addition * subject.dataScale);
                science_data = new ScienceData((float)data_size, 1, 0, subject.id, "Infrared Telescope Data");

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

        protected override void cleanUpScienceData()
        {
            science_awaiting_addition = 0;
        }

        private CelestialBody localStar;
        public CelestialBody LocalStar
        {
            get
            {
                if (localStar == null)
                {
                    localStar = vessel.GetLocalStar();
                }
                return localStar;
            }
        }

        private CelestialBody homeworld;
        public CelestialBody Homeworld
        {
            get
            {
                if (homeworld == null)
                {
                    var planetarium = Planetarium.fetch;
                    if (planetarium != null)
                        homeworld = planetarium.Home;
                }
                return homeworld;
            }
        }

        public override void OnUpdate()
        {
            if (vessel.IsInAtmosphere()) telescopeIsEnabled = false;

            Events["beginOberservations"].active = !vessel.IsInAtmosphere() && !telescopeIsEnabled;
            Events["stopOberservations"].active = telescopeIsEnabled;
            Fields["sciencePerDay"].guiActive = telescopeIsEnabled;
            performPcnt = (perform_factor_d * 100).ToString("0.0") + "%";
            sciencePerDay = (science_rate * 28800 * PluginHelper.getScienceMultiplier(vessel)).ToString("0.00") + " "+Localizer.Format("#LOC_KSPIE_Telescope_ScienceperDa");//Science/Day

            double current_au = Vector3d.Distance(vessel.transform.position, LocalStar.position) / Vector3d.Distance(Homeworld.position, LocalStar.position);
            
            List<ITelescopeController> telescope_controllers = vessel.FindPartModulesImplementing<ITelescopeController>();

            if (telescope_controllers.Any(tscp => tscp.CanProvideTelescopeControl))
            {
                if (current_au >= 548 && !vessel.IsInAtmosphere())
                {
                    if (vessel.orbit.eccentricity < 0.8)
                    {
                        Events["beginOberservations2"].active = true;
                        gLensStr = (telescopeIsEnabled && dpo) ? Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu1") : Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu2");//"Ongoing.""Available"
                    }
                    else
                    {
                        Events["beginOberservations2"].active = false;
                        gLensStr = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu3", vessel.orbit.eccentricity.ToString("0.0"));//"Eccentricity: " +  + "; < 0.8 Required"
                    }
                }
                else
                {
                    Events["beginOberservations2"].active = false;
                    gLensStr = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu4", current_au.ToString("0.0"));// + " AU; Required 548 AU"
                }
            }
            else
            {
                Events["beginOberservations2"].active = false;
                gLensStr = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu5");//"Science Lab/Computer Core required"
            }

            if (helium_time_scale <= 0) performPcnt = Localizer.Format("#LOC_KSPIE_Telescope_Glensstatu6");//"Helium Coolant Deprived."

        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                calculateTimeToHeliumDepletion();

                if (ResearchAndDevelopment.Instance != null)
                {
                    if (helium_time_scale <= 0) telescopeIsEnabled = false;

                    perform_exponent = -(Planetarium.GetUniversalTime() - lastMaintained) * GameConstants.telescopePerformanceTimescale;
                    perform_factor_d = Math.Exp(perform_exponent);

                    if (telescopeIsEnabled)
                    {
                        double base_science = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                        science_rate = base_science * perform_factor_d / 28800;
                        if (!double.IsNaN(science_rate) && !double.IsInfinity(science_rate))
                            science_awaiting_addition += science_rate * TimeWarp.fixedDeltaTime;

                        lastActiveTime = Planetarium.GetUniversalTime();
                    }
                }
            }
        }

        private void calculateTimeToHeliumDepletion()
        {
            var helium_resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.LqdHelium4).ToList();
            var max_helium = helium_resources.Sum(hr => hr.maxAmount);
            var cur_helium = helium_resources.Sum(hr => hr.amount);
            var helium_fraction = (max_helium > 0) ? cur_helium / max_helium : cur_helium;
            helium_time_scale = 1.0 / GameConstants.helium_boiloff_fraction * helium_fraction;
            helium_depleted_time = helium_time_scale + Planetarium.GetUniversalTime();
        }
    }
}
