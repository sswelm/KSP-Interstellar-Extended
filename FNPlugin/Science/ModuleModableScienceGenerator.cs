using System.Collections.Generic;
using System.Linq;
using FNPlugin.Powermanagement;
using KSP.UI.Screens.Flight.Dialogs;

namespace FNPlugin.Science
{
    class ModuleModableScienceGenerator : ResourceSuppliableModule, IScienceDataContainer
    {
        [KSPField(isPersistant = true)] public bool Deployed;
        [KSPField(isPersistant = true)] public string result_string;
        [KSPField(isPersistant = true)] public string result_title;
        [KSPField(isPersistant = true)] public double transmit_value;
        [KSPField(isPersistant = true)] public double recovery_value;
        [KSPField(isPersistant = true)] public double data_size;
        [KSPField(isPersistant = true)] public float xmit_scalar;
        [KSPField(isPersistant = true)] public float ref_value;
        [KSPField(isPersistant = true)] public bool data_gend;

        [KSPField] public bool canDeploy = false;
        [KSPField] public bool rerunnable = false;
        [KSPField] public string deployEventName = "";
        [KSPField] public string reviewEventName = "";
        [KSPField] public string resetEventName = "";
        [KSPField] public string experimentID = "";

        protected ScienceData scienceData;
        protected ExperimentResultDialogPage merdp;

        [KSPEvent(guiName = "#LOC_KSPIE_ScienceGenerator_Deploy", active = true, guiActive = true)]//Deploy
        public void DeployExperiment()
        {
            data_gend = GenerateScienceData();
            ReviewData();
            Deployed = true;
            CleanUpScienceData();
        }

        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam actParams)
        {
            DeployExperiment();
        }

        [KSPEvent(guiName = "#LOC_KSPIE_ScienceGenerator_Reset", active = true, guiActive = true)]//Reset
        public void ResetExperiment()
        {
            if (scienceData != null)
                DumpData(scienceData);

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
            if (scienceData != null)
            {
                if (merdp == null || !data_gend)
                {
                    merdp = new ExperimentResultDialogPage(
                        base.part,
                        this.scienceData,
                        1f,
                        0f,
                        false,
                        "",
                        true,
                        new ScienceLabSearch(vessel, scienceData),
                        this.EndExperiment,
                        this.KeepData,
                        this.SendDataToComms,
                        this.SendDataToLab);

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

        public override void OnStart(StartState state)
        {
        }

        public override void OnSave(ConfigNode node)
        {
            if (scienceData == null) return;

            ConfigNode scienceNode = node.AddNode("ScienceData");
            scienceData.Save(scienceNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!node.HasNode("ScienceData")) return;

            ConfigNode scienceNode = node.GetNode("ScienceData");
            scienceData = new ScienceData(scienceNode);
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

            if (scienceData == null)
                Deployed = false;
        }

        public bool IsRerunnable()
        {
            return rerunnable;
        }

        public int GetScienceCount()
        {
            if (scienceData != null)
                return 1;

            return 0;
        }

        public ScienceData[] GetData()
        {
            if (scienceData != null)
                return new [] { scienceData };
            else
                return new ScienceData[0];
        }

        public void ReviewDataItem(ScienceData science_data)
        {
            if (science_data == this.scienceData)
                ReviewData();
        }

        public void DumpData(ScienceData science_data)
        {
            if (science_data != this.scienceData) return;

            this.scienceData = null;
            merdp = null;
            result_string = ""; // null causes error in save process
            result_title = ""; // null causes error in save process
            transmit_value = 0;
            recovery_value = 0;
            Deployed = false;
        }

        public void ReturnData(ScienceData data)
        {
            // Do Nothing yet
            data.container = scienceData.container;
            data.dataAmount = scienceData.dataAmount;
            data.labValue = scienceData.labValue;
            data.labValue = scienceData.labValue;
            data.subjectID = scienceData.subjectID;
            data.title = scienceData.title;
            data.baseTransmitValue = scienceData.baseTransmitValue;
            data.transmitBonus = scienceData.transmitBonus;
            data.triggered = scienceData.triggered;
        }

        protected void EndExperiment(ScienceData science_data)
        {
            DumpData(science_data);
        }

        protected void SendDataToComms(ScienceData science_data)
        {
            List<IScienceDataTransmitter> list = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (list.Any() && science_data != null && data_gend)
            {
                merdp = null;
                var list2 = new List<ScienceData> {science_data};
                list.OrderBy(ScienceUtil.GetTransmitterScore).First<IScienceDataTransmitter>().TransmitData(list2);
                EndExperiment(science_data);
            }
        }

        protected void SendDataToLab(ScienceData science_data)
        {
            ModuleScienceLab moduleScienceLab = part.FindModuleImplementing<ModuleScienceLab>();
            if (moduleScienceLab == null || science_data == null || !data_gend) return;

            if (!(moduleScienceLab.dataStored + science_data.dataAmount <= moduleScienceLab.dataStorage)) return;

            moduleScienceLab.dataStored += science_data.labValue;
            EndExperiment(science_data);
        }

        protected void KeepData(ScienceData science_data)
        {

        }

        protected virtual bool GenerateScienceData()
        {
            return false;
        }

        protected virtual void CleanUpScienceData()
        {

        }

        public override int getPowerPriority()
        {
            return 4;
        }
    }
}
