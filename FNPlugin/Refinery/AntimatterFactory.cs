using OpenResourceSystem;


namespace FNPlugin.Refinery 
{
    class AntimatterFactory : RefineryActivityBase
    {
        protected double current_rate = 0;
        protected double efficiency = 0.01149;

        public AntimatterFactory(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {

                if (PluginHelper.hasTech("ultraHighEnergyPhysics"))
                    efficiency = efficiency / 100;
                else if (PluginHelper.hasTech("highEnergyScience"))
                    efficiency = efficiency / 1000;
                else
                    efficiency = efficiency / 10000;
            }
        }

        public void produceAntimatterFrame(double rate_multiplier) 
        {
            double energy_provided = rate_multiplier * PluginHelper.BaseAMFPowerConsumption * 1E6;
            double antimatter_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Antimatter).density;
            double antimatter_mass = energy_provided / GameConstants.speedOfLight / GameConstants.speedOfLight / 200000.0f / antimatter_density * efficiency;
            current_rate = -ORSHelper.fixedRequestResource(_part, InterstellarResourcesConfiguration.Instance.Antimatter, -antimatter_mass * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
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
