using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AtmosphericExtractor : RefineryActivityBase, IRefineryActivity
    {
        new private const int valueWidth = 100;

        // persistant
        [KSPField(isPersistant = true)]
        protected int lastBodyID = -1; // ID of the last body. Allows us to skip some expensive calls

        /* Individual percentages of all consituents of the local atmosphere. These are bound to be found in different
         * concentrations in all atmospheres. These are persistant because getting them every update through 
         * the functions (see way below) would be wasteful. I'm placing them up here to make them easier to spot.
         */

        [KSPField(isPersistant = true)]
        protected double _ammoniaPercentage; 
        [KSPField(isPersistant = true)]
        protected double _argonPercentage; // percentage of argon in the local atmosphere
        [KSPField(isPersistant = true)]
        protected double _dioxidePercentage; // percentage of carbon dioxide in the local atmosphere
        [KSPField(isPersistant = true)]
        protected double _helium3Percentage; // etc.
        [KSPField(isPersistant = true)]
        protected double _helium4Percentage;
        [KSPField(isPersistant = true)]
        protected double _hydrogenPercentage;
        [KSPField(isPersistant = true)]
        protected double _methanePercentage;
        [KSPField(isPersistant = true)]
        protected double _monoxidePercentage;
        [KSPField(isPersistant = true)]
        protected double _neonPercentage;
        [KSPField(isPersistant = true)]
        protected double _nitrogenPercentage;
        [KSPField(isPersistant = true)]
        protected double _nitrogen15Percentage;
        [KSPField(isPersistant = true)]
        protected double _oxygenPercentage;
        [KSPField(isPersistant = true)]
        protected double _waterPercentage; 
        [KSPField(isPersistant = true)]
        protected double _heavywaterPercentage; 
        [KSPField(isPersistant = true)]
        protected double _xenonPercentage; 
        [KSPField(isPersistant = true)]
        protected double _deuteriumPercentage;
        [KSPField(isPersistant = true)]
        protected double _kryptonPercentage;
        [KSPField(isPersistant = true)]
        protected double _sodiumPercentage;

        protected double _fixedConsumptionRate;
        protected double _consumptionStorageRatio;

        protected PartResourceDefinition _atmosphere;

        // all the gases that it should be possible to collect from atmospheres
        protected PartResourceDefinition _ammonia;
        protected PartResourceDefinition _argon;
        protected PartResourceDefinition _dioxide;
        protected PartResourceDefinition _helium3;
        protected PartResourceDefinition _helium4;
        protected PartResourceDefinition _hydrogen;
        protected PartResourceDefinition _methane;
        protected PartResourceDefinition _monoxide;
        protected PartResourceDefinition _neon;
        protected PartResourceDefinition _nitrogen;
        protected PartResourceDefinition _nitrogen15;
        protected PartResourceDefinition _oxygen;
        protected PartResourceDefinition _water; // water vapour can form a part of atmosphere as well
        protected PartResourceDefinition _heavywater;
        protected PartResourceDefinition _xenon;
        protected PartResourceDefinition _deuterium;
        protected PartResourceDefinition _krypton;
        protected PartResourceDefinition _sodium;
             
        protected double _atmosphere_consumption_rate;

        protected double _ammonia_production_rate;
        protected double _argon_production_rate;
        protected double _dioxide_production_rate;
        protected double _helium3_production_rate;
        protected double _helium4_production_rate;
        protected double _hydrogen_production_rate;
        protected double _methane_production_rate;
        protected double _monoxide_production_rate;
        protected double _neon_production_rate;
        protected double _nitrogen_production_rate;
        protected double _nitrogen15_production_rate;
        protected double _oxygen_production_rate;
        protected double _water_production_rate;
        protected double _heavywater_production_rate;
        protected double _xenon_production_rate;
        protected double _deuterium_production_rate;
        protected double _krypton_production_rate;
        protected double _sodium_production_rate;

        protected string _atmosphere_resource_name;
        protected string _ammonia_resource_name;
        protected string _argon_resource_name;
        protected string _dioxide_resource_name;
        protected string _helium3_resource_name;
        protected string _helium4_resource_name;
        protected string _hydrogen_resource_name;
        protected string _methane_resource_name;
        protected string _monoxide_resource_name;
        protected string _neon_resource_name;
        protected string _nitrogen_resource_name;
        protected string _nitrogen15_resource_name;
        protected string _oxygen_resource_name;
        protected string _water_resource_name;
        protected string _heavywater_resource_name;
        protected string _xenon_resource_name;
        protected string _deuterium_resource_name;
        protected string _krypton_resource_name;
        protected string _sodium_resource_name;

        public RefineryType RefineryType { get { return RefineryType.cryogenics; } }

        public String ActivityName { get { return "Atmospheric Extraction"; } }

        public bool HasActivityRequirements
        {
            get
            {
                return _vessel.atmDensity > 0 ||  _part.GetConnectedResources(_atmosphere_resource_name).Any(rs => rs.amount > 0);
            }
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public AtmosphericExtractor(Part part)
        {
            _part = part;
            _vessel = part.vessel;
            _intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>();

            // get the name of all relevant resources
            _atmosphere_resource_name = InterstellarResourcesConfiguration._INTAKEATMOSPHERE;

            _ammonia_resource_name = InterstellarResourcesConfiguration._LIQUID_AMMONIA;
            _argon_resource_name = InterstellarResourcesConfiguration._LIQUID_ARGON;
            _dioxide_resource_name = InterstellarResourcesConfiguration._LIQUID_CO2;
            _monoxide_resource_name = InterstellarResourcesConfiguration._LIQUID_CO;
            _helium3_resource_name = InterstellarResourcesConfiguration._LIQUID_HELIUM_3;
            _helium4_resource_name = InterstellarResourcesConfiguration._LIQUID_HELIUM_4;
            _hydrogen_resource_name = InterstellarResourcesConfiguration._LIQUID_HYDROGEN;
            _methane_resource_name = InterstellarResourcesConfiguration._LIQUID_METHANE;
            _neon_resource_name = InterstellarResourcesConfiguration._LIQUID_NEON;
            _nitrogen_resource_name = InterstellarResourcesConfiguration._LIQUID_NITROGEN;
            _nitrogen15_resource_name = InterstellarResourcesConfiguration._LIQUID_NITROGEN_15;
            _oxygen_resource_name = InterstellarResourcesConfiguration._LIQUID_OXYGEN;
            _water_resource_name = InterstellarResourcesConfiguration._LIQUID_WATER;
            _heavywater_resource_name = InterstellarResourcesConfiguration._LIQUID_HEAVYWATER;
            _xenon_resource_name = InterstellarResourcesConfiguration._LIQUID_XENON;
            _deuterium_resource_name = InterstellarResourcesConfiguration._LIQUID_DEUTERIUM;
            _krypton_resource_name = InterstellarResourcesConfiguration._LIQUID_KRYPTON;

            _sodium_resource_name = InterstellarResourcesConfiguration.Instance.Sodium;
            
            // get the densities of all relevant resources
            _atmosphere = PartResourceLibrary.Instance.GetDefinition(_atmosphere_resource_name);
            _ammonia = PartResourceLibrary.Instance.GetDefinition(_ammonia_resource_name);
            _argon = PartResourceLibrary.Instance.GetDefinition(_argon_resource_name);
            _dioxide = PartResourceLibrary.Instance.GetDefinition(_dioxide_resource_name);
            _helium3 = PartResourceLibrary.Instance.GetDefinition(_helium3_resource_name);
            _helium4 = PartResourceLibrary.Instance.GetDefinition(_helium4_resource_name);
            _hydrogen = PartResourceLibrary.Instance.GetDefinition(_hydrogen_resource_name);
            _methane = PartResourceLibrary.Instance.GetDefinition(_methane_resource_name);
            _monoxide = PartResourceLibrary.Instance.GetDefinition(_monoxide_resource_name);
            _neon = PartResourceLibrary.Instance.GetDefinition(_neon_resource_name);
            _nitrogen = PartResourceLibrary.Instance.GetDefinition(_nitrogen_resource_name);
            _nitrogen15 = PartResourceLibrary.Instance.GetDefinition(_nitrogen15_resource_name);
            _oxygen = PartResourceLibrary.Instance.GetDefinition(_oxygen_resource_name);
            _water = PartResourceLibrary.Instance.GetDefinition(_water_resource_name);
            _heavywater = PartResourceLibrary.Instance.GetDefinition(_heavywater_resource_name);
            _xenon = PartResourceLibrary.Instance.GetDefinition(_xenon_resource_name);
            _deuterium = PartResourceLibrary.Instance.GetDefinition(_deuterium_resource_name);
            _krypton = PartResourceLibrary.Instance.GetDefinition(_krypton_resource_name);
            _sodium = PartResourceLibrary.Instance.GetDefinition(_sodium_resource_name);
        }

        protected double _maxCapacityAtmosphereMass;
        protected double _maxCapacityAmmoniaMass;
        protected double _maxCapacityArgonMass;
        protected double _maxCapacityDioxideMass;
        protected double _maxCapacityHelium3Mass;
        protected double _maxCapacityHelium4Mass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityMethaneMass;
        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityNeonMass;
        protected double _maxCapacityNitrogenMass;
        protected double _maxCapacityNitrogen15Mass;
        protected double _maxCapacityOxygenMass;
        protected double _maxCapacityWaterMass;
        protected double _maxCapacityHeavyWaterMass;
        protected double _maxCapacityXenonMass;
        protected double _maxCapacityDeuteriumMass;
        protected double _maxCapacityKryptonMass;
        protected double _maxCapacitySodiumMass;
       
        protected double _availableAtmosphereMass;

        protected double _spareRoomAtmosphereMass;
        protected double _spareRoomAmmoniaMass;
        protected double _spareRoomArgonMass;
        protected double _spareRoomDioxideMass;
        protected double _spareRoomHelium3Mass;
        protected double _spareRoomHelium4Mass;
        protected double _spareRoomHydrogenMass;
        protected double _spareRoomMethaneMass;
        protected double _spareRoomMonoxideMass;
        protected double _spareRoomNeonMass;
        protected double _spareRoomNitrogenMass;
        protected double _spareRoomNitrogen15Mass;
        protected double _spareRoomOxygenMass;
        protected double _spareRoomWaterMass;
        protected double _spareRoomHeavyWaterMass;
        protected double _spareRoomXenonMass;
        protected double _spareRoomDeuteriumMass;
        protected double _spareRoomKryptonMass;
        protected double _spareRoomSodiumMass;

        List<AtmosphericIntake> _intakesList; // create a new list for keeping track of atmo intakes

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime)
        {
            ExtractAir(rateMultiplier, powerFraction, productionModidier, allowOverflow, fixedDeltaTime, false);

            UpdateStatusMessage();
        }
        /* This is just a short cycle that gets the total air production of all the intakes on the vessel per cycle
         * and then stores the value in the persistent totalAirValue, so that this process can access it when offline collecting.
         * tempAir is just a variable used to temporarily hold the total while cycling through parts, then gets reset at every engine update.
         */
        public double GetTotalAirScoopedPerSecond()
        {
             // add any atmo intake part on the vessel to our list
            double tempAir = 0; // reset tempAir before we go into the list
            foreach (AtmosphericIntake intake in _intakesList) // go through the list
            {
                // add the current intake's finalAir to our tempAir. When done with the foreach cycle, we will have the total amount of air these intakes collect per cycle
                tempAir += intake.FinalAir;
            }
            return tempAir;
        }

        public void ExtractAir(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool offlineCollecting)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            _part.GetResourceMass(_atmosphere, out _spareRoomAmmoniaMass, out _maxCapacityAtmosphereMass);
            _part.GetResourceMass(_ammonia, out _spareRoomAtmosphereMass, out _maxCapacityAmmoniaMass);
            _part.GetResourceMass(_argon, out _spareRoomArgonMass, out _maxCapacityArgonMass);
            _part.GetResourceMass(_dioxide, out _spareRoomDioxideMass, out _maxCapacityDioxideMass);
            _part.GetResourceMass(_helium3, out _spareRoomHelium3Mass, out _maxCapacityHelium3Mass);
            _part.GetResourceMass(_helium4, out _spareRoomHelium4Mass, out _maxCapacityHelium4Mass);
            _part.GetResourceMass(_hydrogen, out _spareRoomHydrogenMass, out _maxCapacityHydrogenMass);
            _part.GetResourceMass(_methane, out _spareRoomMethaneMass, out _maxCapacityMethaneMass);
            _part.GetResourceMass(_monoxide, out _spareRoomMonoxideMass, out _maxCapacityMonoxideMass);
            _part.GetResourceMass(_neon, out _spareRoomNeonMass, out _maxCapacityNeonMass);
            _part.GetResourceMass(_nitrogen, out _spareRoomNitrogenMass, out _maxCapacityNitrogenMass);
            _part.GetResourceMass(_nitrogen15, out _spareRoomNitrogen15Mass, out _maxCapacityNitrogen15Mass);
            _part.GetResourceMass(_oxygen, out _spareRoomOxygenMass, out _maxCapacityOxygenMass);
            _part.GetResourceMass(_water, out _spareRoomWaterMass, out _maxCapacityWaterMass);
            _part.GetResourceMass(_heavywater, out _spareRoomHeavyWaterMass, out _maxCapacityHeavyWaterMass);
            _part.GetResourceMass(_xenon, out _spareRoomXenonMass, out _maxCapacityXenonMass);
            _part.GetResourceMass(_deuterium, out _spareRoomDeuteriumMass, out _maxCapacityDeuteriumMass);
            _part.GetResourceMass(_krypton, out _spareRoomKryptonMass, out _maxCapacityKryptonMass);
            _part.GetResourceMass(_sodium, out _spareRoomSodiumMass, out _maxCapacitySodiumMass);

            // determine the amount of resources needed for processing (i.e. intake atmosphere) that the vessel actually holds
            _availableAtmosphereMass = _maxCapacityAtmosphereMass - _spareRoomAmmoniaMass;

            // this should determine how much resource this process can consume
            var fixedMaxAtmosphereConsumptionRate = _current_rate * fixedDeltaTime * _atmosphere.density;
            var buildInAirIntake = fixedMaxAtmosphereConsumptionRate * _vessel.atmDensity;

            var atmosphereConsumptionRatio = offlineCollecting ? 1 
                    : fixedMaxAtmosphereConsumptionRate > 0
                        ? Math.Min(fixedMaxAtmosphereConsumptionRate, buildInAirIntake + _availableAtmosphereMass) / fixedMaxAtmosphereConsumptionRate
                        : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * atmosphereConsumptionRatio;

            // begin the intake atmosphere processing
            // check if there is anything to consume and if there is spare room for at least one of the products
            if (_fixedConsumptionRate > 0 && (
                _spareRoomHydrogenMass > 0 || _spareRoomHelium3Mass > 0 || _spareRoomHelium4Mass > 0 || _spareRoomMonoxideMass > 0 ||
                _spareRoomNitrogenMass > 0 || _spareRoomNitrogen15Mass > 0 || _spareRoomArgonMass > 0 || _spareRoomDioxideMass > 0 || _spareRoomMethaneMass > 0 ||
                _spareRoomNeonMass > 0 || _spareRoomWaterMass > 0 || _spareRoomHeavyWaterMass > 0 || _spareRoomOxygenMass > 0 || 
                _spareRoomXenonMass > 0 || _spareRoomDeuteriumMass > 0 || _spareRoomKryptonMass > 0 || _spareRoomSodiumMass > 0 ||  _spareRoomAmmoniaMass > 0)) 
            {
                /* Now to get the actual percentages from ORSAtmosphericResourceHandler Freethinker extended.
                 * Calls getAtmosphericResourceContent which calls getAtmosphericCompositionForBody which (if there's no definition, i.e. we're using a custom solar system
                 * with weird and fantastic new planets) in turn calls the new GenerateCompositionFromCelestialBody function Freethinker created, which creates a composition
                 * for the upper-level functions based on the planet's size and temperatures. So even though this is calling one method, it's actually going through two or three
                 *  total. Since we like CPUs and want to save them the hassle, let's close this off behind a cheap check.
                */
                if (FlightGlobals.currentMainBody.flightGlobalsIndex != lastBodyID) // did we change a SOI since last time? If yes, get new percentages. Should work the first time as well, since lastBodyID starts as -1, while bodies in the list start at 0
                {
                    Debug.Log("[KSPI] - looking up Atmosphere contents for " + FlightGlobals.currentMainBody.name);

                    // remember, all these are persistent. Once we get them, we won't need to calculate them again until we change SOI
                    _ammoniaPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _ammonia_resource_name);
                    _argonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _argon_resource_name);
                    _monoxidePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _monoxide_resource_name);
                    _dioxidePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _dioxide_resource_name);
                    _helium3Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _helium3_resource_name);
                    _helium4Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _helium4_resource_name);
                    _hydrogenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _hydrogen_resource_name);
                    _methanePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _methane_resource_name);
                    _neonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _neon_resource_name);
                    _nitrogenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _nitrogen_resource_name);
                    _nitrogen15Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _nitrogen15_resource_name);
                    _oxygenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _oxygen_resource_name);
                    _waterPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _water_resource_name);
                    _heavywaterPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _heavywater_resource_name);
                    _xenonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _xenon_resource_name);
                    _deuteriumPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _deuterium_resource_name);
                    _kryptonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _krypton_resource_name);
                    _sodiumPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _sodium_resource_name);

                    lastBodyID = FlightGlobals.currentMainBody.flightGlobalsIndex; // reassign the id of current body to the lastBodyID variable, ie. remember this planet, so that we skip this check next time!
                }

                if (offlineCollecting) // if we're collecting offline, we don't need to actually consume the resource, just provide the lines below with a number
                {
                    // calculate consumption
                    var internal_consumption = _fixedConsumptionRate * _vessel.atmDensity;
                    var external_consumption = GetTotalAirScoopedPerSecond() * fixedDeltaTime;

                    var totalProcessed = Math.Min(_fixedConsumptionRate, internal_consumption + external_consumption);
                    ScreenMessages.PostScreenMessage("The atmospheric extractor processed " + _atmosphere_resource_name + " for " + fixedDeltaTime.ToString("F0") + " seconds", 60.0f, ScreenMessageStyle.UPPER_CENTER);

                    _atmosphere_consumption_rate = totalProcessed / fixedDeltaTime;
                }
                else
                {
                    // how much of the consumed atmosphere is going to end up as these?
                    var fixedMaxAmmoniaRate = _fixedConsumptionRate * _ammoniaPercentage;
                    var fixedMaxArgonRate = _fixedConsumptionRate * _argonPercentage;
                    var fixedMaxDioxideRate = _fixedConsumptionRate * _dioxidePercentage;
                    var fixedMaxHelium3Rate = _fixedConsumptionRate * _helium3Percentage;
                    var fixedMaxHelium4Rate = _fixedConsumptionRate * _helium4Percentage;
                    var fixedMaxHydrogenRate = _fixedConsumptionRate * _hydrogenPercentage;
                    var fixedMaxMethaneRate = _fixedConsumptionRate * _methanePercentage;
                    var fixedMaxMonoxideRate = _fixedConsumptionRate * _monoxidePercentage;
                    var fixedMaxNeonRate = _fixedConsumptionRate * _neonPercentage;
                    var fixedMaxNitrogenRate = _fixedConsumptionRate * _nitrogenPercentage;
                    var fixedMaxNitrogen15Rate = _fixedConsumptionRate * _nitrogen15Percentage;
                    var fixedMaxOxygenRate = _fixedConsumptionRate * _oxygenPercentage;
                    var fixedMaxWaterRate = _fixedConsumptionRate * _waterPercentage;
                    var fixedMaxHeavyWaterRate = _fixedConsumptionRate * _heavywaterPercentage;
                    var fixedMaxXenonRate = _fixedConsumptionRate * _xenonPercentage;
                    var fixedMaxDeuteriumRate = _fixedConsumptionRate * _deuteriumPercentage;
                    var fixedMaxKryptonRate = _fixedConsumptionRate * _kryptonPercentage;
                    var fixedMaxSodiumRate = _fixedConsumptionRate * _sodiumPercentage;

                    // how much can we add to the tanks per cycle? If allowOverflow is on, just push it all in, regardless of if the tank can hold the amount. Otherwise adjust accordingly
                    var fixedMaxPossibleAmmoniaRate = allowOverflow ? fixedMaxAmmoniaRate : Math.Min(_spareRoomAmmoniaMass, fixedMaxAmmoniaRate);
                    var fixedMaxPossibleArgonRate = allowOverflow ? fixedMaxArgonRate : Math.Min(_spareRoomArgonMass, fixedMaxArgonRate);
                    var fixedMaxPossibleDioxideRate = allowOverflow ? fixedMaxDioxideRate : Math.Min(_spareRoomDioxideMass, fixedMaxDioxideRate);
                    var fixedMaxPossibleHelium3Rate = allowOverflow ? fixedMaxHelium3Rate : Math.Min(_spareRoomHelium3Mass, fixedMaxHelium3Rate);
                    var fixedMaxPossibleHelium4Rate = allowOverflow ? fixedMaxHelium4Rate : Math.Min(_spareRoomHelium4Mass, fixedMaxHelium4Rate);
                    var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                    var fixedMaxPossibleMethaneRate = allowOverflow ? fixedMaxMethaneRate : Math.Min(_spareRoomMethaneMass, fixedMaxMethaneRate);
                    var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                    var fixedMaxPossibleNeonRate = allowOverflow ? fixedMaxNeonRate : Math.Min(_spareRoomNeonMass, fixedMaxNeonRate);
                    var fixedMaxPossibleNitrogenRate = allowOverflow ? fixedMaxNitrogenRate : Math.Min(_spareRoomNitrogenMass, fixedMaxNitrogenRate);
                    var fixedMaxPossibleNitrogen15Rate = allowOverflow ? fixedMaxNitrogen15Rate : Math.Min(_spareRoomNitrogen15Mass, fixedMaxNitrogen15Rate);
                    var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);
                    var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);
                    var fixedMaxPossibleHeavyWaterRate = allowOverflow ? fixedMaxHeavyWaterRate : Math.Min(_spareRoomHeavyWaterMass, fixedMaxHeavyWaterRate);
                    var fixedMaxPossibleXenonRate = allowOverflow ? fixedMaxXenonRate : Math.Min(_spareRoomXenonMass, fixedMaxXenonRate);
                    var fixedMaxPossibleDeuteriumRate = allowOverflow ? fixedMaxDeuteriumRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxDeuteriumRate);
                    var fixedMaxPossibleKryptonRate = allowOverflow ? fixedMaxKryptonRate : Math.Min(_spareRoomKryptonMass, fixedMaxKryptonRate);
                    var fixedMaxPossibleSodiumRate = allowOverflow ? fixedMaxSodiumRate : Math.Min(_spareRoomSodiumMass, fixedMaxSodiumRate);

                    // Check if the denominator for each is zero (in that case, assign zero outright, so that we don't end up with an infinite mess on our hands)
                    var ammRatio = (fixedMaxAmmoniaRate == 0) ? 0 : fixedMaxPossibleAmmoniaRate / fixedMaxAmmoniaRate;
                    var arRatio = (fixedMaxArgonRate == 0) ? 0 : fixedMaxPossibleArgonRate / fixedMaxArgonRate;
                    var dioxRatio = (fixedMaxDioxideRate == 0) ? 0 : fixedMaxPossibleDioxideRate / fixedMaxDioxideRate;
                    var he3Ratio = (fixedMaxHelium3Rate == 0) ? 0 : fixedMaxPossibleHelium3Rate / fixedMaxHelium3Rate;
                    var he4Ratio = (fixedMaxHelium4Rate == 0) ? 0 : fixedMaxPossibleHelium4Rate / fixedMaxHelium4Rate;
                    var hydroRatio = (fixedMaxHydrogenRate == 0) ? 0 : fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                    var methRatio = (fixedMaxMethaneRate == 0) ? 0 : fixedMaxPossibleMethaneRate / fixedMaxMethaneRate;
                    var monoxRatio = (fixedMaxMonoxideRate == 0) ? 0 : fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate;
                    var neonRatio = (fixedMaxNeonRate == 0) ? 0 : fixedMaxPossibleNeonRate / fixedMaxNeonRate;
                    var nitroRatio = (fixedMaxNitrogenRate == 0) ? 0 : fixedMaxPossibleNitrogenRate / fixedMaxNitrogenRate;
                    var nitro15Ratio = (fixedMaxNitrogen15Rate == 0) ? 0 : fixedMaxPossibleNitrogen15Rate / fixedMaxNitrogen15Rate;
                    var oxyRatio = (fixedMaxOxygenRate == 0) ? 0 : fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                    var waterRatio = (fixedMaxWaterRate == 0) ? 0 : fixedMaxPossibleWaterRate / fixedMaxWaterRate;
                    var heavywaterRatio = (fixedMaxHeavyWaterRate == 0) ? 0 : fixedMaxPossibleHeavyWaterRate / fixedMaxHeavyWaterRate;
                    var xenonRatio = (fixedMaxXenonRate == 0) ? 0 : fixedMaxPossibleXenonRate / fixedMaxXenonRate;
                    var deuteriumRatio = (fixedMaxDeuteriumRate == 0) ? 0 : fixedMaxPossibleDeuteriumRate / fixedMaxDeuteriumRate;
                    var kryptonRatio = (fixedMaxKryptonRate == 0) ? 0 : fixedMaxPossibleKryptonRate / fixedMaxKryptonRate;
                    var sodiumRatio = (fixedMaxSodiumRate == 0) ? 0 : fixedMaxPossibleSodiumRate / fixedMaxSodiumRate;

                    /* finds a non-zero minimum of all the ratios (calculated above, as fixedMaxPossibleZZRate / fixedMaxZZRate). It needs to be non-zero 
                    * so that the collecting works even when some of consitutents are absent from the local atmosphere (ie. when their definition is zero).
                    * Otherwise the consumptionStorageRatio would be zero and thus no atmosphere would be consumed. */
                    _consumptionStorageRatio = new [] { ammRatio, arRatio, dioxRatio, he3Ratio, he4Ratio, hydroRatio, methRatio, monoxRatio, neonRatio, nitroRatio, nitro15Ratio, oxyRatio, waterRatio, heavywaterRatio, xenonRatio, deuteriumRatio, kryptonRatio, sodiumRatio }.Where(x => x > 0).Min();

                    var max_atmospheric_consumption_rate = _consumptionStorageRatio * _fixedConsumptionRate;

                    // first consume atmosphere with build in air intakes
                    var internal_consumpion = _fixedConsumptionRate * _vessel.atmDensity;

                    // calculate amospereic consumption per second
                    _atmosphere_consumption_rate = internal_consumpion / fixedDeltaTime;

                    // calculate missing atmsophere which can be extracted from air intakes
                    var remainingConsumptionNeeded = Math.Max(0, internal_consumpion - max_atmospheric_consumption_rate);

                    // add the consumed atmosphere total atmopheric consumption rate
                    _atmosphere_consumption_rate += _part.RequestResource(_atmosphere_resource_name, remainingConsumptionNeeded / _atmosphere.density) / fixedDeltaTime * _atmosphere.density;
                }
                
                // produce the resources
                _ammonia_production_rate =	_ammoniaPercentage == 0 ?	0 : -_part.RequestResource(_ammonia_resource_name, -_atmosphere_consumption_rate * _ammoniaPercentage * fixedDeltaTime / _ammonia.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _ammonia.density;
                _argon_production_rate = _argonPercentage == 0 ? 0 : -_part.RequestResource(_argon_resource_name, -_atmosphere_consumption_rate * _argonPercentage * fixedDeltaTime / _argon.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _argon.density;
                _dioxide_production_rate = _dioxidePercentage == 0 ? 0 : -_part.RequestResource(_dioxide_resource_name, -_atmosphere_consumption_rate * _dioxidePercentage * fixedDeltaTime / _dioxide.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _dioxide.density;
                _helium3_production_rate = _helium3Percentage == 0 ? 0 : -_part.RequestResource(_helium3_resource_name, -_atmosphere_consumption_rate * _helium3Percentage * fixedDeltaTime / _helium3.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium3.density;
                _helium4_production_rate = _helium4Percentage == 0 ? 0 : -_part.RequestResource(_helium4_resource_name, -_atmosphere_consumption_rate * _helium4Percentage * fixedDeltaTime / _helium4.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium4.density;
                _hydrogen_production_rate = _hydrogenPercentage == 0 ? 0 : -_part.RequestResource(_hydrogen_resource_name, -_atmosphere_consumption_rate * _hydrogenPercentage * fixedDeltaTime / _hydrogen.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen.density;
                _methane_production_rate = _methanePercentage == 0 ? 0 : -_part.RequestResource(_methane_resource_name, -_atmosphere_consumption_rate * _methanePercentage * fixedDeltaTime / _methane.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _methane.density;
                _monoxide_production_rate = _monoxidePercentage == 0 ? 0 : -_part.RequestResource(_monoxide_resource_name, -_atmosphere_consumption_rate * _monoxidePercentage * fixedDeltaTime / _monoxide.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide.density;
                _neon_production_rate = _neonPercentage == 0 ? 0 : -_part.RequestResource(_neon_resource_name, -_atmosphere_consumption_rate * _neonPercentage * fixedDeltaTime / _neon.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neon.density;
                _nitrogen_production_rate = _nitrogenPercentage == 0 ? 0 : -_part.RequestResource(_nitrogen_resource_name, -_atmosphere_consumption_rate * _nitrogenPercentage * fixedDeltaTime / _nitrogen.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen.density;
                _nitrogen15_production_rate = _nitrogen15Percentage == 0 ? 0 : -_part.RequestResource(_nitrogen15_resource_name, -_atmosphere_consumption_rate * _nitrogen15Percentage * fixedDeltaTime / _nitrogen15.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen15.density;
                _oxygen_production_rate = _oxygenPercentage == 0 ? 0 : -_part.RequestResource(_oxygen_resource_name, -_atmosphere_consumption_rate * _oxygenPercentage * fixedDeltaTime / _oxygen.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen.density;
                _water_production_rate = _waterPercentage == 0 ? 0 : -_part.RequestResource(_water_resource_name, -_atmosphere_consumption_rate * _waterPercentage * fixedDeltaTime / _water.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _water.density;
                _heavywater_production_rate = _heavywaterPercentage == 0 ? 0 : -_part.RequestResource(_heavywater_resource_name, -_atmosphere_consumption_rate * _heavywaterPercentage * fixedDeltaTime / _heavywater.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _heavywater.density;
                _xenon_production_rate = _xenonPercentage == 0 ? 0 : -_part.RequestResource(_xenon_resource_name, -_atmosphere_consumption_rate * _xenonPercentage * fixedDeltaTime / _xenon.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _xenon.density;
                _deuterium_production_rate = _deuteriumPercentage == 0 ? 0 : -_part.RequestResource(_deuterium_resource_name, -_atmosphere_consumption_rate * _deuteriumPercentage * fixedDeltaTime / _deuterium.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium.density;
                _krypton_production_rate = _kryptonPercentage == 0 ? 0 : -_part.RequestResource(_krypton_resource_name, -_atmosphere_consumption_rate * _kryptonPercentage * fixedDeltaTime / _krypton.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _krypton.density;
                _sodium_production_rate = _sodiumPercentage == 0 ? 0 : -_part.RequestResource(_sodium_resource_name, -_atmosphere_consumption_rate * _sodiumPercentage * fixedDeltaTime / _sodium.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _sodium.density;
            }
            else
            {
                _atmosphere_consumption_rate = 0;
                _ammonia_production_rate = 0;
                _argon_production_rate = 0;
                _dioxide_production_rate = 0;
                _helium3_production_rate = 0;
                _helium4_production_rate = 0;
                _hydrogen_production_rate = 0;
                _methane_production_rate = 0;
                _monoxide_production_rate = 0;
                _neon_production_rate = 0;
                _nitrogen_production_rate = 0;
                _nitrogen15_production_rate = 0;
                _oxygen_production_rate = 0;
                _water_production_rate = 0;
                _heavywater_production_rate = 0;
                _xenon_production_rate = 0;
                _deuterium_production_rate = 0;
                _krypton_production_rate = 0;
                _sodium_production_rate = 0;
            }
        }


        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Intake Atmo. Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_atmosphere_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Intake Atmo. Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableAtmosphereMass.ToString("0.0000") + " mT / " + _maxCapacityAtmosphereMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", _bold_label, GUILayout.Width(valueWidth));
            GUILayout.Label("Abundance", _bold_label, GUILayout.Width(valueWidth));
            GUILayout.Label("Production per second", _bold_label, GUILayout.Width(valueWidth));
            GUILayout.Label("Production per hour", _bold_label, GUILayout.Width(valueWidth));
            GUILayout.Label("Spare Room", _bold_label, GUILayout.Width(valueWidth));
            GUILayout.Label("Stored", _bold_label, GUILayout.Width(valueWidth));
            GUILayout.Label("Max Capacity", _bold_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            DisplayResourceExtraction("Hydrogen", _hydrogenPercentage, _hydrogen_production_rate, _spareRoomHydrogenMass, _maxCapacityHydrogenMass);
            DisplayResourceExtraction("Deuterium", _deuteriumPercentage, _deuterium_production_rate, _spareRoomDeuteriumMass, _maxCapacityDeuteriumMass);
            DisplayResourceExtraction("Helium-3", _helium3Percentage, _helium3_production_rate, _spareRoomHelium3Mass, _maxCapacityHelium3Mass);
            DisplayResourceExtraction("Helium", _helium4Percentage, _helium4_production_rate, _spareRoomHelium4Mass, _maxCapacityHelium4Mass);
            DisplayResourceExtraction("Nitrogen", _nitrogenPercentage, _nitrogen_production_rate, _spareRoomNitrogenMass, _maxCapacityNitrogenMass);
            DisplayResourceExtraction("Nitrogen-15", _nitrogen15Percentage, _nitrogen15_production_rate, _spareRoomNitrogen15Mass, _maxCapacityNitrogen15Mass);
            DisplayResourceExtraction("Oxygen", _oxygenPercentage, _oxygen_production_rate, _spareRoomOxygenMass, _maxCapacityOxygenMass);
            DisplayResourceExtraction("Argon", _oxygenPercentage, _argon_production_rate, _spareRoomArgonMass, _maxCapacityArgonMass);
            DisplayResourceExtraction("Neon", _oxygenPercentage, _neon_production_rate, _spareRoomNeonMass, _maxCapacityNeonMass);
            DisplayResourceExtraction("Krypton", _kryptonPercentage, _krypton_production_rate, _spareRoomKryptonMass, _maxCapacityKryptonMass);
            DisplayResourceExtraction("Ammonia", _ammoniaPercentage, _ammonia_production_rate, _spareRoomAmmoniaMass, _maxCapacityAmmoniaMass);
            DisplayResourceExtraction("Water", _waterPercentage, _water_production_rate, _spareRoomWaterMass, _maxCapacityWaterMass);
            DisplayResourceExtraction("Heavy Water", _heavywaterPercentage, _heavywater_production_rate, _spareRoomHeavyWaterMass, _maxCapacityHeavyWaterMass);
            DisplayResourceExtraction("Carbon Monoxide", _monoxidePercentage, _monoxide_production_rate, _spareRoomMonoxideMass, _maxCapacityMonoxideMass);
            DisplayResourceExtraction("Carbon Dioxide", _dioxidePercentage, _dioxide_production_rate, _spareRoomDioxideMass, _maxCapacityDioxideMass);
            DisplayResourceExtraction("Methane", _methanePercentage, _methane_production_rate, _spareRoomMethaneMass, _maxCapacityMethaneMass);
            DisplayResourceExtraction("Xenon", _xenonPercentage, _xenon_production_rate, _spareRoomXenonMass, _maxCapacityXenonMass);
            DisplayResourceExtraction("Sodium", _sodiumPercentage, _sodium_production_rate, _spareRoomSodiumMass, _maxCapacitySodiumMass);
        }

        private void DisplayResourceExtraction(string resourceName,  double percentage, double productionRate, double spareRoom, double maximumCapacity)
        {
            if (percentage <= 0)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Label(resourceName, _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label((percentage * 100).ToString("##.######") + "%", _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label(productionRate.ToString("##.######") + " U/s", _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label((productionRate * GameConstants.HOUR_SECONDS).ToString("##.######") + " U/h", _value_label, GUILayout.Width(valueWidth));
            if (spareRoom > 0)
            {
                GUILayout.Label((spareRoom).ToString("##.######") + " t", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label((maximumCapacity - spareRoom).ToString("##.######") + " t", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label((maximumCapacity).ToString("##.######") + " t", _value_label, GUILayout.Width(valueWidth));
            }
            else
            {
                GUILayout.Label("", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label("", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label("", _value_label, GUILayout.Width(valueWidth));
            }
            GUILayout.EndHorizontal();
        } 

        private void UpdateStatusMessage()
        {
            if (_atmosphere_consumption_rate > 0)
                _status = "Extracting atmosphere";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage, try allowing overflow";
        }

        public void PrintMissingResources() 
        {
            ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration._INTAKEATMOSPHERE, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
