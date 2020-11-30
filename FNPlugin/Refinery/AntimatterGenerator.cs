using FNPlugin.Constants;

namespace FNPlugin.Refinery
{
    class AntimatterGenerator : RefineryActivity
    {
        public double ProductionRate => _current_rate;

        private readonly double _efficiency = 0.01149;   // base efficiency

        public double Efficiency => _efficiency;

        readonly PartResourceDefinition _antimatterDefinition;

        public AntimatterGenerator(Part part, double efficiencyMultiplier, PartResourceDefinition antimatterDefinition)
        {
            _efficiency *= efficiencyMultiplier;
            _part = part;
            _vessel = part.vessel;
            _antimatterDefinition = antimatterDefinition;

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

            if (techLevel >= 5)
                _efficiency /= 20;
            if (techLevel == 4)
                _efficiency /= 100;
            else if (techLevel == 3)
                _efficiency /= 500;
            else if (techLevel == 2)
                _efficiency /= 2000;
            else if (techLevel == 1)
                _efficiency /= 10000;
            else
                _efficiency /= 50000;
        }

        public void Produce(double energyProvidedInMegajoules)
        {
            if (energyProvidedInMegajoules <= 0)
                return;

            double antimatterUnits = energyProvidedInMegajoules * 1E6 / GameConstants.lightSpeedSquared / 2000 / _antimatterDefinition.density * _efficiency;

            _current_rate = -_part.RequestResource(_antimatterDefinition.id, -antimatterUnits, ResourceFlowMode.STAGE_PRIORITY_FLOW);
        }
    }
}
