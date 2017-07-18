namespace FNPlugin.Refinery 
{
    class AntimatterFactory : RefineryActivityBase
    {
        protected double current_rate = 0;
        protected double efficiency = 0.01149;

        PartResourceDefinition antimattterDefinition;

        public AntimatterFactory(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            antimattterDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Antimatter);

            if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {

                if (PluginHelper.upgradeAvailable("ultraHighEnergyPhysics"))
                    efficiency = efficiency / 100;
                else if (PluginHelper.upgradeAvailable("appliedHighEnergyPhysics"))
                    efficiency = efficiency / 500;
                else if (PluginHelper.upgradeAvailable("highEnergyScience"))
                    efficiency = efficiency / 1000;
                else
                    efficiency = efficiency / 10000;
            }
        }

        public void produceAntimatterFrame(double rate_multiplier) 
        {
            double energy_provided_in_joules = rate_multiplier * PluginHelper.BaseAMFPowerConsumption * 1E6;
            double antimatter_units = energy_provided_in_joules / GameConstants.lightSpeedSquared / 2000 / antimattterDefinition.density * efficiency;

            current_rate = -_part.RequestResource(antimattterDefinition.id, -antimatter_units * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
        }

        public double getAntimatterProductionRate() 
        {
            return current_rate;
        }

        public double getAntimatterProductionEfficiency() 
        {
            return efficiency;
        }
    }
}
