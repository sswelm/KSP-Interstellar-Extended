using KSP.Localization;
using UnityEngine;

namespace FNPlugin.Science
{
    class FNSeismicProbe : ModuleModableScienceGenerator
    {
        // Persistent True
        [KSPField(isPersistant = true)] public bool probeIsEnabled;

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

            SaveState();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_SeismicProbe_StopRecording", active = false)]//Stop Recording
        public void DeactivateProbe()
        {
            probeIsEnabled = false;
            SaveState();
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            canDeploy = true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Events[nameof(ActivateProbe)].active = !probeIsEnabled;
            Events[nameof(DeactivateProbe)].active = probeIsEnabled;
        }

        protected override bool GenerateScienceData()
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

            ConfigNode config = PluginHelper.GetPluginSaveFile();

            if (!config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper()))
                return false;

            ConfigNode planetData = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
            foreach (ConfigNode probeData in planetData.nodes)
            {
                if (!probeData.name.Contains("IMPACT_"))
                    continue;

                science_vess_ref = probeData.name;
                bool transmitted = false;
                string vessel_name = "";
                float distribution_factor = 0;

                if (probeData.HasValue("transmitted"))
                    transmitted = bool.Parse(probeData.GetValue("transmitted"));
                if (probeData.HasValue("vesselname"))
                    vessel_name = probeData.GetValue("vesselname");
                if (probeData.HasValue("distribution_factor"))
                    distribution_factor = float.Parse(probeData.GetValue("distribution_factor"));

                if (transmitted)
                    continue;

                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.SrfLanded, vessel.mainBody, vessel.mainBody.name + "'s surface.", vessel.mainBody.name + "'s surface.");
                if (subject == null)
                    return false;

                subject.subjectValue = PluginHelper.GetScienceMultiplier(vessel);
                subject.scienceCap = 10 * experiment.baseValue * subject.subjectValue;

                float baseScience = experiment.baseValue * distribution_factor;
                data_size = baseScience * subject.dataScale;
                scienceData = new ScienceData((float)data_size, 1, 0, subject.id, "Impactor Data");

                result_string = Localizer.Format("#LOC_KSPIE_SeismicProbe_Resultmsg2", vessel_name,vessel.mainBody.name,vessel.mainBody.name);// + " impacted into " +  + " producing seismic activity.  From this data, information on the structure of " +  + "'s crust can be determined."

                float scienceAmount = baseScience * subject.subjectValue;
                recovery_value = scienceAmount * subject.scientificValue;
                transmit_value = recovery_value;
                ref_value = subject.scienceCap;

                return true;
            }
            return false;
        }

        protected override void CleanUpScienceData()
        {
            if (science_vess_ref == null)
                return;

            ConfigNode config = PluginHelper.GetPluginSaveFile();
            if (!config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper()))
                return;

            ConfigNode planetData = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
            if (!planetData.HasNode(science_vess_ref))
                return;

            ConfigNode impactNode = planetData.GetNode(science_vess_ref);
            if (impactNode.HasValue("transmitted"))
                impactNode.SetValue("transmitted", "True");

            config.Save(PluginHelper.PluginSaveFilePath);
        }

        protected void SaveState()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                ConfigNode config = PluginHelper.GetPluginSaveFile();
                string vesselId = vessel.id.ToString();
                if (config.HasNode("VESSEL_SEISMIC_PROBE_" + vesselId))
                {
                    ConfigNode probeNode = config.GetNode("VESSEL_SEISMIC_PROBE_" + vesselId);

                    if (probeNode.HasValue("is_active"))
                        probeNode.SetValue("is_active", probeIsEnabled.ToString());
                    else
                        probeNode.AddValue("is_active", probeIsEnabled.ToString());

                    if (probeNode.HasValue("celestial_body"))
                        probeNode.SetValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());
                    else
                        probeNode.AddValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());

                }
                else
                {
                    ConfigNode probeNode = config.AddNode("VESSEL_SEISMIC_PROBE_" + vesselId);
                    probeNode.AddValue("is_active", probeIsEnabled.ToString());
                    probeNode.AddValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());
                }
                config.Save(PluginHelper.PluginSaveFilePath);
            }
        }
    }
}
