namespace FNPlugin
{
    class ChemicalEngineTag : PartModule { }

	class ZPinchFusionEngine : VistaECU2 { }

    class ChemicalEngine : VistaECU2 { }

    class InertialFusionEngine : VistaECU2 { }

    class VistaECU2 : FusionECU2, IUpgradeableModule
    {
        const float maxMin = defaultMinIsp / defaultMaxIsp;
        const float defaultMaxIsp = 27200;
        const float defaultMinIsp = 15500;
        const float defaultMaxSteps = 100;
        const float defaultSteps = (defaultMaxIsp - defaultMinIsp) / defaultMaxSteps;
        const float stepNumb = 0;

        // Persistent setting
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_VistaECU2_SelectedIsp"), UI_FloatRange(stepIncrement = defaultSteps, maxValue = defaultMaxIsp, minValue = defaultMinIsp)]//Selected Isp
        public float localIsp = defaultMinIsp + (stepNumb * defaultSteps);
       
        // settings
        [KSPField]
        public float neutronAbsorptionFractionAtMinIsp = 0.5f;
        [KSPField]
        public float maxThrustEfficiencyByIspPower = 2;
        [KSPField]
        public float gearDivider = -1;
        [KSPField]
        public float minIsp = defaultMinIsp;
        [KSPField]
        public float initialGearRatio = 0;

        private FloatCurve _atmosphereCurve;

        protected override FloatCurve BaseFloatCurve
        {
            get { return _atmosphereCurve ?? curEngineT.atmosphereCurve; }
            set { _atmosphereCurve = value; }
        }

        protected override bool ShowIspThrottle 
        { 
            get { return Fields["localIsp"].guiActive; } 
            set { Fields["localIsp"].guiActive = value; } 
        } 

        protected override float InitialGearRatio => initialGearRatio;
        protected override float SelectedIsp { get => localIsp; set { if (value > 0) { localIsp = value; } } }
        protected override float MinIsp { get => minIsp; set { if (value <= 10) { minIsp = value + .01f; } else { minIsp = value; } } }
        protected override float MaxIsp => minIsp / maxMin;
        protected override float GearDivider => gearDivider >= 0 ? gearDivider : maxMin;
        protected override float MaxSteps => defaultMaxSteps;
        protected override float MaxThrustEfficiencyByIspPower => maxThrustEfficiencyByIspPower;
        protected override float NeutronAbsorptionFractionAtMinIsp => neutronAbsorptionFractionAtMinIsp;
    }
}
