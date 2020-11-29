using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens.Flight.Dialogs;

namespace FNPlugin
{

    class ModuleModableScienceGenerator : ResourceSuppliableModule, IScienceDataContainer
    {
        [KSPField(isPersistant = false)]
        public bool canDeploy = false;
        [KSPField(isPersistant = true)]
        public bool Deployed;
        [KSPField(isPersistant = true)]
        public string result_string;
        [KSPField(isPersistant = true)]
        public string result_title;
        [KSPField(isPersistant = true)]
        public double transmit_value;
        [KSPField(isPersistant = true)]
        public double recovery_value;
        [KSPField(isPersistant = true)]
        public double data_size;
        [KSPField(isPersistant = true)]
        public float xmit_scalar;
        [KSPField(isPersistant = true)]
        public float ref_value;
        [KSPField(isPersistant = true)]
        public bool data_gend = false;

        [KSPField(isPersistant = false)]
        public bool rerunnable = false;
        [KSPField(isPersistant = false)]
        public string deployEventName = "";
        [KSPField(isPersistant = false)]
        public string reviewEventName = "";
        [KSPField(isPersistant = false)]
        public string resetEventName = "";
        [KSPField(isPersistant = false)]
        public string experimentID = "";

        protected ScienceData science_data;

        //protected ModableExperimentResultDialogPage merdp;
        protected ExperimentResultDialogPage merdp;


        [KSPEvent(guiName = "#LOC_KSPIE_ScienceGenerator_Deploy", active = true, guiActive = true)]//Deploy
        public void DeployExperiment()
        {
            data_gend = generateScienceData();
            ReviewData();
            Deployed = true;
            cleanUpScienceData();
        }

        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam actParams)
        {
            DeployExperiment();
        }

        [KSPEvent(guiName = "#LOC_KSPIE_ScienceGenerator_Reset", active = true, guiActive = true)]//Reset
        public void ResetExperiment()
        {
            if (science_data != null)
                DumpData(science_data);

            Deployed = false;
        }

        [KSPAction("Reset")]
        public void ResetAction(KSPActionParam actParams)
        {
            ResetExperiment();
        }

        [KSPEvent(guiName = "#LOC_KSPIE_ScienceGenerator_ReviewData", active = true, guiActive = true)]//Review Data
        public void ReviewData()
        {
            if (science_data != null)
            {
                if (merdp == null || !data_gend)
                {
                    merdp = new ExperimentResultDialogPage(
                        base.part,
                        this.science_data,
                        1f,
                        0f,
                        false,
                        "",
                        true,
                        new ScienceLabSearch(this.vessel, this.science_data),
                        this.endExperiment,
                        this.keepData,
                        this.sendDataToComms,
                        this.sendDataToLab);

                    //merdp = new ModableExperimentResultDialogPage(
                    //		base.part,
                    //		this.science_data,
                    //		this.science_data.baseTransmitValue,
                    //		0,
                    //		false,
                    //		"",
                    //		true,
                    //		false,
                    //		new Callback<ScienceData>(this.endExperiment),
                    //		new Callback<ScienceData>(this.keepData),
                    //		new Callback<ScienceData>(this.sendDataToComms),
                    //		new Callback<ScienceData>(this.sendDataToLab));
                    //merdp.setUpScienceData(result_title, result_string, (float)transmit_value, (float)recovery_value, (float)data_size, xmit_scalar, ref_value);
                }
                ExperimentsResultDialog.DisplayResult(merdp);
            }
            else
                ResetExperiment();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;
        }

        public override void OnSave(ConfigNode node)
        {
            if (science_data == null) return;

            ConfigNode science_node = node.AddNode("ScienceData");
            science_data.Save(science_node);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!node.HasNode("ScienceData")) return;

            ConfigNode science_node = node.GetNode("ScienceData");
            science_data = new ScienceData(science_node);
        }

        public override void OnUpdate()
        {
            Events[nameof(DeployExperiment)].guiName = deployEventName;
            Events[nameof(ResetExperiment)].guiName = resetEventName;
            Events[nameof(ReviewData)].guiName = reviewEventName;

            Events[nameof(DeployExperiment)].active = canDeploy && !Deployed;
            Events[nameof(ResetExperiment)].active = canDeploy && Deployed;
            Events[nameof(ReviewData)].active = canDeploy && Deployed;

            Actions[nameof(DeployAction)].guiName = deployEventName;
            Actions[nameof(DeployAction)].active = canDeploy;

            if (science_data == null)
                Deployed = false;
        }

        public bool IsRerunnable()
        {
            return rerunnable;
        }

        public int GetScienceCount()
        {
            if (science_data != null)
                return 1;

            return 0;
        }

        public ScienceData[] GetData()
        {
            if (science_data != null)
                return new ScienceData[] { science_data };
            else
                return new ScienceData[0];
        }

        public void ReviewDataItem(ScienceData science_data)
        {
            if (science_data == this.science_data)
                ReviewData();
        }

        public void DumpData(ScienceData science_data)
        {
            if (science_data != this.science_data) return;

            this.science_data = null;
            merdp = null;
            result_string = ""; // null causes error in save process
            result_title = ""; // null causes error in save proccess
            transmit_value = 0;
            recovery_value = 0;
            Deployed = false;
        }

        public void ReturnData(ScienceData data)
        {
            // Do Nothing yet
            data.container = science_data.container;
            data.dataAmount = science_data.dataAmount;
            data.labValue = science_data.labValue;
            data.labValue = science_data.labValue;
            data.subjectID = science_data.subjectID;
            data.title = science_data.title;
            data.baseTransmitValue = science_data.baseTransmitValue;
            data.transmitBonus = science_data.transmitBonus;
            data.triggered = science_data.triggered;
        }

        protected void endExperiment(ScienceData science_data)
        {
            DumpData(science_data);
        }

        protected void sendDataToComms(ScienceData science_data)
        {
            List<IScienceDataTransmitter> list = base.vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (list.Any<IScienceDataTransmitter>() && science_data != null && data_gend)
            {
                merdp = null;
                List<ScienceData> list2 = new List<ScienceData>();
                list2.Add(science_data);
                list.OrderBy(ScienceUtil.GetTransmitterScore).First<IScienceDataTransmitter>().TransmitData(list2);
                endExperiment(science_data);
            }
        }

        protected void sendDataToLab(ScienceData science_data)
        {
            ModuleScienceLab moduleScienceLab = part.FindModuleImplementing<ModuleScienceLab>();
            if (moduleScienceLab == null || science_data == null || !data_gend) return;

            if (!(moduleScienceLab.dataStored + science_data.dataAmount <= moduleScienceLab.dataStorage)) return;

            moduleScienceLab.dataStored += science_data.labValue;
            endExperiment(science_data);
        }

        protected void keepData(ScienceData science_data)
        {

        }

        protected virtual bool generateScienceData()
        {
            return false;
        }

        protected virtual void cleanUpScienceData()
        {

        }

        public override int getPowerPriority()
        {
            return 4;
        }
    }
}
