using UnityEngine;
using KSP.Localization;

namespace FNPlugin
{
    class FNSeismicProbe : ModuleModableScienceGenerator
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool probeIsEnabled;

        protected long active_count = 0;
        protected string science_vess_ref;

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_SeismicProbe_RecordData", active = true)]//Record Seismic Data
        public void ActivateProbe()
        {
            if (vessel.Landed)
            {
                //PopupDialog.SpawnPopupDialog("Seismic Probe", "Surface will be monitored for impact events.", "OK", false, HighLogic.Skin);
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Seismic Probe", Localizer.Format("#LOC_KSPIE_SeismicProbe_Dialog_title"), Localizer.Format("#LOC_KSPIE_SeismicProbe_Dialog_message"), Localizer.Format("#LOC_KSPIE_SeismicProbe_Dialog_Button"), false, HighLogic.UISkin);//"Seismic Probe""Surface will be monitored for impact events.""OK"
                probeIsEnabled = true;
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SeismicProbe_Postmsg1"), 5f, ScreenMessageStyle.UPPER_CENTER);//"Must be landed to activate seismic probe."

            saveState();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_SeismicProbe_StopRecording", active = false)]//Stop Recording
        public void DeactivateProbe()
        {
            probeIsEnabled = false;
            saveState();
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            base.canDeploy = true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Events["ActivateProbe"].active = !probeIsEnabled;
            Events["DeactivateProbe"].active = probeIsEnabled;
        }

        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("FNSeismicProbeExperiment");
            if (experiment == null)
            {
                return false;
            }
            //ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.SrfLanded, vessel.mainBody, "surface");
            //if (subject == null) {
            //    return false;
            //}
            //subject.scientificValue = 1;
            //subject.scienceCap = float.MaxValue;
            //subject.science = 1;
            //subject.subjectValue = 1;
            result_title = Localizer.Format("#LOC_KSPIE_SeismicProbe_Resulttitle");//"Impactor Experiment"
            result_string = Localizer.Format("#LOC_KSPIE_SeismicProbe_Resultmsg");//"No useful seismic data has been recorded."
            transmit_value = 0;
            recovery_value = 0;
            data_size = 0;
            xmit_scalar = 1;
            ref_value = 1;

            // science_data = new ScienceData(0, 1, 0, subject.id, "data");

            ConfigNode config = PluginHelper.getPluginSaveFile();
            if (config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper()))
            {
                ConfigNode planet_data = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                foreach (ConfigNode probe_data in planet_data.nodes)
                {
                    if (probe_data.name.Contains("IMPACT_"))
                    {
                        science_vess_ref = probe_data.name;
                        bool transmitted = false;
                        string vessel_name = "";
                        float distribution_factor = 0;

                        if (probe_data.HasValue("transmitted"))
                            transmitted = bool.Parse(probe_data.GetValue("transmitted"));
                        if (probe_data.HasValue("vesselname"))
                            vessel_name = probe_data.GetValue("vesselname");
                        if (probe_data.HasValue("distribution_factor"))
                            distribution_factor = float.Parse(probe_data.GetValue("distribution_factor"));
                        
                        if (!transmitted)
                        {
                            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.SrfLanded, vessel.mainBody, vessel.mainBody.name + "'s surface.", vessel.mainBody.name + "'s surface.");
                            if (subject == null)
                                return false;
                            subject.subjectValue = PluginHelper.getScienceMultiplier(vessel);
                            subject.scienceCap = 10 * experiment.baseValue * subject.subjectValue;

                            float base_science = experiment.baseValue * distribution_factor;
                            data_size = base_science * subject.dataScale;
                            science_data = new ScienceData((float)data_size, 1, 0, subject.id, "Impactor Data");

                            result_string = Localizer.Format("#LOC_KSPIE_SeismicProbe_Resultmsg2", vessel_name,vessel.mainBody.name,vessel.mainBody.name);// + " impacted into " +  + " producing seismic activity.  From this data, information on the structure of " +  + "'s crust can be determined."

                            float science_amount = base_science * subject.subjectValue;
                            recovery_value = science_amount * subject.scientificValue;
                            transmit_value = recovery_value;
                            ref_value = subject.scienceCap;

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected override void cleanUpScienceData()
        {
            if (science_vess_ref != null)
            {
                ConfigNode config = PluginHelper.getPluginSaveFile();
                if (config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper()))
                {
                    ConfigNode planet_data = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                    if (planet_data.HasNode(science_vess_ref))
                    {
                        ConfigNode impact_node = planet_data.GetNode(science_vess_ref);
                        if (impact_node.HasValue("transmitted"))
                            impact_node.SetValue("transmitted", "True");

                        config.Save(PluginHelper.PluginSaveFilePath);
                    }
                }
            }
        }

        protected void saveState()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                ConfigNode config = PluginHelper.getPluginSaveFile();
                string vesselID = vessel.id.ToString();
                if (config.HasNode("VESSEL_SEISMIC_PROBE_" + vesselID))
                {
                    ConfigNode probe_node = config.GetNode("VESSEL_SEISMIC_PROBE_" + vesselID);

                    if (probe_node.HasValue("is_active"))
                        probe_node.SetValue("is_active", probeIsEnabled.ToString());
                    else
                        probe_node.AddValue("is_active", probeIsEnabled.ToString());

                    if (probe_node.HasValue("celestial_body"))
                        probe_node.SetValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());
                    else
                        probe_node.AddValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());

                }
                else
                {
                    ConfigNode probe_node = config.AddNode("VESSEL_SEISMIC_PROBE_" + vesselID);
                    probe_node.AddValue("is_active", probeIsEnabled.ToString());
                    probe_node.AddValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());
                }
                config.Save(PluginHelper.PluginSaveFilePath);
            }
        }
    }
}
