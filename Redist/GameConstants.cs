namespace FNPlugin.Constants 
{
    public class GameConstants
    {
        public const double kerbin_sun_distance = 13599840256;
        public const double averageKerbinSolarFlux = 1409.285;  // this seems to be the average flux at Kerbin just above the atmosphere (from my tests)
        public const double avogadroConstant = 6.022140857e+23; // number of atoms in 1 mol
        public const double basePowerConsumption = 5;
        public const double baseAMFPowerConsumption = 5000;
        public const double baseCentriPowerConsumption = 43.5;
        public const double baseELCPowerConsumption = 40;
        public const double baseAnthraquiononePowerConsumption = 5;
        public const double basePechineyUgineKuhlmannPowerConsumption = 5;
        public const double baseHaberProcessPowerConsumption = 20;
        public const double baseUraniumAmmonolysisPowerConsumption = 12;
        public const double anthraquinoneEnergyPerTon = 1834.321;
        public const double haberProcessEnergyPerTon = 34200;
        public const double waterElectrolysisEnergyPerTon = 18159;
        public const double aluminiumElectrolysisEnergyPerTon = 35485.714;
        public const double pechineyUgineKuhlmannEnergyPerTon = 1021;
        public const double EarthAtmospherePressureAtSeaLevel = 101.325;
        public const double EarthRadius = 6371000;
        public const double aluminiumElectrolysisMassRatio = 1.5;
        public const double deuterium_abudance = 0.00015625;
        public const double deuterium_timescale = 0.0016667;
        public const double baseReprocessingRate = 400;
        public const double baseScienceRate = 0.1;
        public const double baseUraniumAmmonolysisRate = 0.0002383381;   
        public const double microwave_angle = 3.64773814E-10;
        public const double microwave_dish_efficiency = 0.85;
        public const double microwave_alpha = 0.00399201596806387225548902195609;
        public const double microwave_beta = 1 - microwave_alpha;
        public const double stefan_const = 5.67036713e-8;  // Stefan-Botzman const for watts / m2
        public const double rad_const_h = 1000;
        public const double atmospheric_non_precooled_limit = 740;
        public const double initial_alcubierre_megajoules_required = 100;
        public const double telescopePerformanceTimescale = 2.1964508725630127431022388314009e-8;
        public const double telescopeBaseScience = 0.1666667;
        public const double telescopeGLensScience = 5;
        public const double speedOfLight = 299792458;
        public const double lightSpeedSquared = speedOfLight * speedOfLight;
        public const double tritiumBreedRate = 428244.662271 / 0.17639 / 1.25;  // 0.222678566;
        public const double helium_boiloff_fraction = 1.667794e-8;
        public const double ammoniaHydrogenFractionByMass = 0.17647;
        public const double KERBIN_YEAR_IN_DAYS = 426.08;
        public const double ELECTRON_CHARGE = 1.602176565e-19;
        public const double ATOMIC_MASS_UNIT =  1.660538921e-27;
        public const double STANDARD_GRAVITY = 9.80665;
        public const double dilution_factor = 15000.0;
        public const double IspCoreTemperatureMultiplier = 22.371670613;
        public const double BaseThrustPowerMultiplier = 2000;
        public const double HighCoreTempThrustMultiplier = 1600;
        public const float MaxThermalNozzleIsp = 2997.13f;
        public const double EngineHeatProduction = 1000;
        public const double AirflowHeatMultiplier = 1;
        public const double ecPerMJ = 1000.0;

        public const int KEBRIN_HOURS_DAY = 8;
        public const int SECONDS_IN_HOUR = 3600;
        public const int KEBRIN_DAY_SECONDS = SECONDS_IN_HOUR * KEBRIN_HOURS_DAY;

        public const int defaultSupportedPropellantAtoms = 511; // any atom type
        public const int defaultSupportedPropellantTypes = 127; // any molecular type
    }
}
