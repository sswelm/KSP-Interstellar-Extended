using FNPlugin.Constants;

namespace FNPlugin.Refinery
{
    class FNResourceGenerator : RefineryActivity
    {
        public double ProductionRate => _current_rate;

        private readonly double _efficiency = 0.5;   // base efficiency

        public double Efficiency => _efficiency;

        private readonly PartResourceDefinition _inputResourceDefinition;
        private readonly PartResourceDefinition _outputResourceDefinition;

        public FNResourceGenerator(Part part, double efficiencyMultiplier, PartResourceDefinition inputResourceDefinition , PartResourceDefinition outputResourceDefinition)
        {
            _efficiency *= efficiencyMultiplier;
            _part = part;
            _vessel = part.vessel;

            _inputResourceDefinition = inputResourceDefinition;
            _outputResourceDefinition = outputResourceDefinition;

            int techLevel = 0;
            if (PluginHelper.UpgradeAvailable("ScienceLabUpgradeA"))
                techLevel++;
            if (PluginHelper.UpgradeAvailable("ScienceLabUpgradeB"))
                techLevel++;
            if (PluginHelper.UpgradeAvailable("ScienceLabUpgradeC"))
                techLevel++;
            if (PluginHelper.UpgradeAvailable("ScienceLabUpgradeD"))
                techLevel++;
            if (PluginHelper.UpgradeAvailable("ScienceLabUpgradeE"))
                techLevel++;
        }

        public void Produce(double energyProvidedInMegajoules, double efficiencyModifier)
        {
            if (energyProvidedInMegajoules <= 0)
                return;

            double unitsInTon = energyProvidedInMegajoules / 1000 / 216 * _efficiency * efficiencyModifier;

            //units
            var inputResourceInLiter = unitsInTon * _inputResourceDefinition.density;

            var consumedResource = _part.RequestResource(_outputResourceDefinition.id, inputResourceInLiter, ResourceFlowMode.STAGE_PRIORITY_FLOW);

            var outputResourceInLiter = consumedResource / _inputResourceDefinition.density * _outputResourceDefinition.density;

            _current_rate = -_part.RequestResource(_outputResourceDefinition.id, -outputResourceInLiter, ResourceFlowMode.STAGE_PRIORITY_FLOW);
        }

        public override void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            // do nothing
        }

        public override bool HasActivityRequirements()
        {
            return true;
        }

        public override void PrintMissingResources()
        {
            // do nothing
        }

        public override void Initialize(Part localPart, InterstellarRefineryController controller)
        {
            // do nothing
        }
    }
}
