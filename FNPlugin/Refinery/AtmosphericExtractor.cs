using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AtmosphericExtractor : RefineryActivity, IRefineryActivity
    {
        public AtmosphericExtractor()
        {
            ActivityName = "Atmospheric Extraction";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        // persistent
        [KSPField(isPersistant = true)]
        protected int lastBodyID = -1; // ID of the last body. Allows us to skip some expensive calls

        [KSPField(isPersistant = true)]
        public bool isDeployed;
        [KSPField(guiActive = false)]
        public float normalizedTime = -1;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_SurfaceArea", guiFormat = "F3")]//Surface Area
        public double surfaceArea = 1;
        [KSPField]
        public double buildInAirIntake;
        [KSPField]
        public double atmosphereConsumptionRatio;
        [KSPField]
        public string animName = "";

        /* Individual percentages of all consituents of the local atmosphere. These are bound to be found in different
         * concentrations in all atmospheres. These are persistent because getting them every update through 
         * the functions (see way below) would be wasteful. I'm placing them up here to make them easier to spot.
         */

        [KSPField(isPersistant = true)]
        protected double _ammoniaPercentage; 
        [KSPField(isPersistant = true)]
        protected double _argonPercentage; // percentage of argon in the local atmosphere
        [KSPField(isPersistant = true)]
        protected double _chlorinePercentage;
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

        private Animation _scoopAnimation;
        private double _fixedConsumptionRate;
        private double _consumptionStorageRatio;
        private double _intakeModifier;

        private PartResourceDefinition _atmosphere;

        // all the gases that it should be possible to collect from atmospheres
        private PartResourceDefinition _ammonia;
        private PartResourceDefinition _argon;
        private PartResourceDefinition _chlorine;
        private PartResourceDefinition _dioxide;
        private PartResourceDefinition _helium3;
        private PartResourceDefinition _helium4;
        private PartResourceDefinition _hydrogen;
        private PartResourceDefinition _methane;
        private PartResourceDefinition _monoxide;
        private PartResourceDefinition _neon;
        private PartResourceDefinition _nitrogen;
        private PartResourceDefinition _nitrogen15;
        private PartResourceDefinition _oxygen;
        private PartResourceDefinition _water; // water vapour can form a part of atmosphere as well
        private PartResourceDefinition _heavyWater;
        private PartResourceDefinition _xenon;
        private PartResourceDefinition _deuterium;
        private PartResourceDefinition _krypton;
        private PartResourceDefinition _sodium;

        double _atmosphereConsumptionRate;

        double _ammoniaProductionRate;
        double _argonProductionRate;
        double _chlorineProductionRate;
        double _dioxideProductionRate;
        double _helium3ProductionRate;
        double _helium4ProductionRate;
        double _hydrogenProductionRate;
        double _methaneProductionRate;
        double _monoxideProductionRate;
        double _neonProductionRate;
        double _nitrogenProductionRate;
        double _nitrogen15ProductionRate;
        double _oxygenProductionRate;
        double _waterProductionRate;
        double _heavyWaterProductionRate;
        double _xenonProductionRate;
        double _deuteriumProductionRate;
        double _kryptonProductionRate;
        double _sodiumProductionRate;

        string _atmosphereResourceName;

        string _ammoniaResourceName;
        string _argonResourceName;
        string _chlorineResourceName;
        string _dioxideResourceName;
        string _helium3ResourceName;
        string _helium4ResourceName;
        string _hydrogenResourceName;
        string _methaneResourceName;
        string _monoxideResourceName;
        string _neonResourceName;
        string _nitrogenResourceName;
        string _nitrogen15ResourceName;
        string _oxygenResourceName;
        string _waterResourceName;
        string _heavyWaterResourceName;
        string _xenonResourceName;
        string _deuteriumResourceName;
        string _kryptonResourceName;
        string _sodiumResourceName;


        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_DeployScoop", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]//Deploy Scoop
        public void DeployScoop()
        {
            RunAnimation(animName, _scoopAnimation, 0.5f, 0);
            isDeployed = true;
        }

        // GUI to retract sail
        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_RetractScoop", active = false, guiActiveUncommand = true, guiActiveUnfocused = true)]//Retract Scoop
        public void RetractScoop()
        {
            RunAnimation(animName, _scoopAnimation, -0.5f, 1);
            isDeployed = false;
        }

        public RefineryType RefineryType => RefineryType.Cryogenics;

        public bool HasActivityRequirements()
        {
            return true;
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;
            _intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>();

            if (!string.IsNullOrEmpty(animName))
            {
                _scoopAnimation = part.FindModelAnimators(animName).First();

                if (_scoopAnimation != null)
                {
                    _scoopAnimation[animName].speed = 0;
                    _scoopAnimation[animName].normalizedTime = isDeployed ? 1 : 0;
                    _scoopAnimation.Blend(animName);
                }
            }

            // get the name of all relevant resources
            _atmosphereResourceName = InterstellarResourcesConfiguration._INTAKEATMOSPHERE;

            _ammoniaResourceName = InterstellarResourcesConfiguration._LIQUID_AMMONIA;
            _argonResourceName = InterstellarResourcesConfiguration._LIQUID_ARGON;
            _chlorineResourceName = InterstellarResourcesConfiguration._CHLORINE;
            _dioxideResourceName = InterstellarResourcesConfiguration._LIQUID_CO2;
            _monoxideResourceName = InterstellarResourcesConfiguration._CARBONMONOXIDE_LIQUID;
            _helium3ResourceName = InterstellarResourcesConfiguration._HELIUM3_LIQUID;
            _helium4ResourceName = InterstellarResourcesConfiguration._HELIUM4_LIQUID;
            _hydrogenResourceName = InterstellarResourcesConfiguration._HYDROGEN_LIQUID;
            _methaneResourceName = InterstellarResourcesConfiguration._LIQUID_METHANE;
            _neonResourceName = InterstellarResourcesConfiguration._NEON_LIQUID;
            _nitrogenResourceName = InterstellarResourcesConfiguration._NITROGEN_LIQUID;
            _nitrogen15ResourceName = InterstellarResourcesConfiguration._LIQUID_NITROGEN_15;
            _oxygenResourceName = InterstellarResourcesConfiguration._LIQUID_OXYGEN;
            _waterResourceName = InterstellarResourcesConfiguration._LIQUID_WATER;
            _heavyWaterResourceName = InterstellarResourcesConfiguration._LIQUID_HEAVYWATER;
            _xenonResourceName = InterstellarResourcesConfiguration._LIQUID_XENON;
            _deuteriumResourceName = InterstellarResourcesConfiguration._DEUTERIUM_LIQUID;
            _kryptonResourceName = InterstellarResourcesConfiguration._LIQUID_KRYPTON;

            _sodiumResourceName = InterstellarResourcesConfiguration.Instance.Sodium;
            
            // get the densities of all relevant resources
            _atmosphere = PartResourceLibrary.Instance.GetDefinition(_atmosphereResourceName);
            _ammonia = PartResourceLibrary.Instance.GetDefinition(_ammoniaResourceName);
            _argon = PartResourceLibrary.Instance.GetDefinition(_argonResourceName);
            _chlorine = PartResourceLibrary.Instance.GetDefinition(_chlorineResourceName);
            _dioxide = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName);
            _helium3 = PartResourceLibrary.Instance.GetDefinition(_helium3ResourceName);
            _helium4 = PartResourceLibrary.Instance.GetDefinition(_helium4ResourceName);
            _hydrogen = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName);
            _methane = PartResourceLibrary.Instance.GetDefinition(_methaneResourceName);
            _monoxide = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName);
            _neon = PartResourceLibrary.Instance.GetDefinition(_neonResourceName);
            _nitrogen = PartResourceLibrary.Instance.GetDefinition(_nitrogenResourceName);
            _nitrogen15 = PartResourceLibrary.Instance.GetDefinition(_nitrogen15ResourceName);
            _oxygen = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName);
            _water = PartResourceLibrary.Instance.GetDefinition(_waterResourceName);
            _heavyWater = PartResourceLibrary.Instance.GetDefinition(_heavyWaterResourceName);
            _xenon = PartResourceLibrary.Instance.GetDefinition(_xenonResourceName);
            _deuterium = PartResourceLibrary.Instance.GetDefinition(_deuteriumResourceName);
            _krypton = PartResourceLibrary.Instance.GetDefinition(_kryptonResourceName);
            _sodium = PartResourceLibrary.Instance.GetDefinition(_sodiumResourceName);
        }

        double _maxCapacityAtmosphereMass;

        double _maxCapacityAmmoniaMass;
        double _maxCapacityArgonMass;
        double _maxCapacityChlorineMass;
        double _maxCapacityDioxideMass;
        double _maxCapacityHelium3Mass;
        double _maxCapacityHelium4Mass;
        double _maxCapacityHydrogenMass;
        double _maxCapacityMethaneMass;
        double _maxCapacityMonoxideMass;
        double _maxCapacityNeonMass;
        double _maxCapacityNitrogenMass;
        double _maxCapacityNitrogen15Mass;
        double _maxCapacityOxygenMass;
        double _maxCapacityWaterMass;
        double _maxCapacityHeavyWaterMass;
        double _maxCapacityXenonMass;
        double _maxCapacityDeuteriumMass;
        double _maxCapacityKryptonMass;
        double _maxCapacitySodiumMass;
       
        double _availableAtmosphereMass;

        double _spareRoomAtmosphereMass;
        double _spareRoomAmmoniaMass;
        double _spareRoomArgonMass;
        double _spareRoomChlorineMass;
        double _spareRoomDioxideMass;
        double _spareRoomHelium3Mass;
        double _spareRoomHelium4Mass;
        double _spareRoomHydrogenMass;
        double _spareRoomMethaneMass;
        double _spareRoomMonoxideMass;
        double _spareRoomNeonMass;
        double _spareRoomNitrogenMass;
        double _spareRoomNitrogen15Mass;
        double _spareRoomOxygenMass;
        double _spareRoomWaterMass;
        double _spareRoomHeavyWaterMass;
        double _spareRoomXenonMass;
        double _spareRoomDeuteriumMass;
        double _spareRoomKryptonMass;
        double _spareRoomSodiumMass;

        List<AtmosphericIntake> _intakesList; // create a new list for keeping track of atmo intakes

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            ExtractAir(rateMultiplier, powerFraction, productionModifier, allowOverflow, fixedDeltaTime, false);

            UpdateStatusMessage();
        }
        /* This is just a short cycle that gets the total air production of all the intakes on the vessel per cycle
         * and then stores the value in the persistent totalAirValue, so that this process can access it when offline collecting.
         * tempAir is just a variable used to temporarily hold the total while cycling through parts, then gets reset at every engine update.
         */
        public double GetTotalAirScoopedPerSecond()
        {
             // add any atmosphere intake part on the vessel to our list
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
            _current_rate = CurrentPower / EnergyPerTon;

            try
            {
                // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
                // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
                _part.GetResourceMass(_atmosphere, out _spareRoomAtmosphereMass, out _maxCapacityAtmosphereMass);
                _part.GetResourceMass(_ammonia, out _spareRoomAmmoniaMass, out _maxCapacityAmmoniaMass);
                _part.GetResourceMass(_argon, out _spareRoomArgonMass, out _maxCapacityArgonMass);
                _part.GetResourceMass(_chlorine, out _spareRoomChlorineMass, out _maxCapacityChlorineMass);
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
                _part.GetResourceMass(_heavyWater, out _spareRoomHeavyWaterMass, out _maxCapacityHeavyWaterMass);
                _part.GetResourceMass(_xenon, out _spareRoomXenonMass, out _maxCapacityXenonMass);
                _part.GetResourceMass(_deuterium, out _spareRoomDeuteriumMass, out _maxCapacityDeuteriumMass);
                _part.GetResourceMass(_krypton, out _spareRoomKryptonMass, out _maxCapacityKryptonMass);
                _part.GetResourceMass(_sodium, out _spareRoomSodiumMass, out _maxCapacitySodiumMass);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ExtractAir GetResourceMass Exception: " + e.Message);
            }

            // determine the amount of resources needed for processing (i.e. intake atmosphere) that the vessel actually holds
            _availableAtmosphereMass = _maxCapacityAtmosphereMass - _spareRoomAtmosphereMass;
            if (_scoopAnimation != null)
            {
                var animationState = _scoopAnimation[animName];
                normalizedTime = animationState.normalizedTime == 0
                    ? isDeployed ? 1 : 0
                    : animationState.normalizedTime;
            }
            else
                normalizedTime = 1;

            // intake can only function when heading towards orbital path
            _intakeModifier = _scoopAnimation == null ? 1 : Math.Max(0, Vector3d.Dot(part.transform.up, part.vessel.obt_velocity.normalized));

            try
            {
                // calculate build in scoop capacity
                buildInAirIntake = normalizedTime <= 0.2 ? 0 :
                    AtmosphericFloatCurves.GetAtmosphericGasDensityKgPerCubicMeter(_vessel) * (1 + _vessel.obt_speed) * surfaceArea * _intakeModifier * Math.Sqrt((normalizedTime - 0.2) * 1.25);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ExtractAir GetAtmosphericGasDensityKgPerCubicMeter Exception: " + e.Message);
            }


            atmosphereConsumptionRatio = offlineCollecting ? 1
                    : _current_rate > 0
                        ? Math.Min(_current_rate, buildInAirIntake + _availableAtmosphereMass) / _current_rate
                        : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * atmosphereConsumptionRatio;

            // begin the intake atmosphere processing
            // check if there is anything to consume and if there is spare room for at least one of the products
            if (_fixedConsumptionRate > 0 && (
                _spareRoomAmmoniaMass > 0 ||
                _spareRoomArgonMass > 0 ||
                _spareRoomChlorineMass > 0 ||
                _spareRoomHydrogenMass > 0 ||
                _spareRoomHelium3Mass > 0 ||
                _spareRoomHelium4Mass > 0 ||
                _spareRoomMonoxideMass > 0 ||
                _spareRoomNitrogenMass > 0 ||
                _spareRoomNitrogen15Mass > 0 ||
                _spareRoomDioxideMass > 0 || 
                _spareRoomMethaneMass > 0 ||
                _spareRoomNeonMass > 0 || 
                _spareRoomWaterMass > 0 || 
                _spareRoomHeavyWaterMass > 0 ||
                _spareRoomOxygenMass > 0 ||
                _spareRoomXenonMass > 0 ||
                _spareRoomDeuteriumMass > 0 ||
                _spareRoomKryptonMass > 0 ||
                _spareRoomSodiumMass > 0 )) 
            {
                /* Now to get the actual percentages from AtmosphericResourceHandler Freethinker extended.
                 * Calls getAtmosphericResourceContent which calls getAtmosphericCompositionForBody which (if there's no definition, i.e. we're using a custom solar system
                 * with weird and fantastic new planets) in turn calls the new GenerateCompositionFromCelestialBody function Freethinker created, which creates a composition
                 * for the upper-level functions based on the planet's size and temperatures. So even though this is calling one method, it's actually going through two or three
                 *  total. Since we like CPUs and want to save them the hassle, let's close this off behind a cheap check.
                */
                if (FlightGlobals.currentMainBody.flightGlobalsIndex != lastBodyID) // did we change a SOI since last time? If yes, get new percentages. Should work the first time as well, since lastBodyID starts as -1, while bodies in the list start at 0
                    try
                    {
                        Debug.Log("[KSPI]: looking up Atmosphere contents for " + FlightGlobals.currentMainBody.name);

                        // remember, all these are persistent. Once we get them, we won't need to calculate them again until we change SOI
                        _ammoniaPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _ammoniaResourceName);
                        _argonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _argonResourceName);
                        _chlorinePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _chlorineResourceName);
                        _monoxidePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _monoxideResourceName);
                        _dioxidePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _dioxideResourceName);
                        _helium3Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _helium3ResourceName);
                        _helium4Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _helium4ResourceName);
                        _hydrogenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _hydrogenResourceName);
                        _methanePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _methaneResourceName);
                        _neonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _neonResourceName);
                        _nitrogenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _nitrogenResourceName);
                        _nitrogen15Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _nitrogen15ResourceName);
                        _oxygenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _oxygenResourceName);
                        _waterPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _waterResourceName);
                        _heavywaterPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _heavyWaterResourceName);
                        _xenonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _xenonResourceName);
                        _deuteriumPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _deuteriumResourceName);
                        _kryptonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _kryptonResourceName);
                        _sodiumPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _sodiumResourceName);

                        lastBodyID = FlightGlobals.currentMainBody.flightGlobalsIndex; // reassign the id of current body to the lastBodyID variable, ie. remember this planet, so that we skip this check next time!
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[KSPI]: ExtractAir getAtmosphericResourceContent Exception: " + e.Message);
                    }

                if (offlineCollecting) // if we're collecting offline, we don't need to actually consume the resource, just provide the lines below with a number
                {
                    _atmosphereConsumptionRate = Math.Min(_current_rate, buildInAirIntake + GetTotalAirScoopedPerSecond());
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Postmsg1", _atmosphereResourceName, fixedDeltaTime.ToString("F0")), 60.0f, ScreenMessageStyle.UPPER_CENTER);//"The atmospheric extractor processed " +  + " for " +  + " seconds"
                }
                else
                {
                    // how much of the consumed atmosphere is going to end up as these?
                    var fixedMaxAmmoniaRate = _fixedConsumptionRate * _ammoniaPercentage;
                    var fixedMaxArgonRate = _fixedConsumptionRate * _argonPercentage;
                    var fixedMaxChlorineRate = _fixedConsumptionRate * _chlorinePercentage;
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
                    var fixedMaxPossibleChlorineRate = allowOverflow ? fixedMaxChlorineRate : Math.Min(_spareRoomChlorineMass, fixedMaxChlorineRate);
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
                    var chlRatio = (fixedMaxChlorineRate == 0) ? 0 : fixedMaxPossibleChlorineRate / fixedMaxChlorineRate;
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

                    // calculate atmospheric consumption per second
                    _atmosphereConsumptionRate = buildInAirIntake;

                    // calculate missing atmospheric which can be extracted from air intakes
                    var remainingConsumptionNeeded = Math.Max(0, buildInAirIntake - max_atmospheric_consumption_rate);

                    // add the consumed atmosphere total atmospheric consumption rate
                    _atmosphereConsumptionRate += _part.RequestResource(_atmosphereResourceName, remainingConsumptionNeeded / _atmosphere.density) / fixedDeltaTime * _atmosphere.density;
                }
                
                // produce the resources
                _ammoniaProductionRate = _ammoniaPercentage == 0 ? 0 : -_part.RequestResource(_ammoniaResourceName, -_atmosphereConsumptionRate * _ammoniaPercentage * fixedDeltaTime / _atmosphere.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _atmosphere.density;
                _argonProductionRate = _argonPercentage == 0 ? 0 : -_part.RequestResource(_argonResourceName, -_atmosphereConsumptionRate * _argonPercentage * fixedDeltaTime / _argon.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _argon.density;
                _chlorineProductionRate = _chlorinePercentage == 0 ? 0 : -_part.RequestResource(_chlorineResourceName, -_atmosphereConsumptionRate * _chlorinePercentage * fixedDeltaTime / _chlorine.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _chlorine.density;
                _dioxideProductionRate = _dioxidePercentage == 0 ? 0 : -_part.RequestResource(_dioxideResourceName, -_atmosphereConsumptionRate * _dioxidePercentage * fixedDeltaTime / _dioxide.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _dioxide.density;
                _helium3ProductionRate = _helium3Percentage == 0 ? 0 : -_part.RequestResource(_helium3ResourceName, -_atmosphereConsumptionRate * _helium3Percentage * fixedDeltaTime / _helium3.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium3.density;
                _helium4ProductionRate = _helium4Percentage == 0 ? 0 : -_part.RequestResource(_helium4ResourceName, -_atmosphereConsumptionRate * _helium4Percentage * fixedDeltaTime / _helium4.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium4.density;
                _hydrogenProductionRate = _hydrogenPercentage == 0 ? 0 : -_part.RequestResource(_hydrogenResourceName, -_atmosphereConsumptionRate * _hydrogenPercentage * fixedDeltaTime / _hydrogen.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen.density;
                _methaneProductionRate = _methanePercentage == 0 ? 0 : -_part.RequestResource(_methaneResourceName, -_atmosphereConsumptionRate * _methanePercentage * fixedDeltaTime / _methane.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _methane.density;
                _monoxideProductionRate = _monoxidePercentage == 0 ? 0 : -_part.RequestResource(_monoxideResourceName, -_atmosphereConsumptionRate * _monoxidePercentage * fixedDeltaTime / _monoxide.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide.density;
                _neonProductionRate = _neonPercentage == 0 ? 0 : -_part.RequestResource(_neonResourceName, -_atmosphereConsumptionRate * _neonPercentage * fixedDeltaTime / _neon.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neon.density;
                _nitrogenProductionRate = _nitrogenPercentage == 0 ? 0 : -_part.RequestResource(_nitrogenResourceName, -_atmosphereConsumptionRate * _nitrogenPercentage * fixedDeltaTime / _nitrogen.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen.density;
                _nitrogen15ProductionRate = _nitrogen15Percentage == 0 ? 0 : -_part.RequestResource(_nitrogen15ResourceName, -_atmosphereConsumptionRate * _nitrogen15Percentage * fixedDeltaTime / _nitrogen15.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen15.density;
                _oxygenProductionRate = _oxygenPercentage == 0 ? 0 : -_part.RequestResource(_oxygenResourceName, -_atmosphereConsumptionRate * _oxygenPercentage * fixedDeltaTime / _oxygen.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen.density;
                _waterProductionRate = _waterPercentage == 0 ? 0 : -_part.RequestResource(_waterResourceName, -_atmosphereConsumptionRate * _waterPercentage * fixedDeltaTime / _water.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _water.density;
                _heavyWaterProductionRate = _heavywaterPercentage == 0 ? 0 : -_part.RequestResource(_heavyWaterResourceName, -_atmosphereConsumptionRate * _heavywaterPercentage * fixedDeltaTime / _heavyWater.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _heavyWater.density;
                _xenonProductionRate = _xenonPercentage == 0 ? 0 : -_part.RequestResource(_xenonResourceName, -_atmosphereConsumptionRate * _xenonPercentage * fixedDeltaTime / _xenon.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _xenon.density;
                _deuteriumProductionRate = _deuteriumPercentage == 0 ? 0 : -_part.RequestResource(_deuteriumResourceName, -_atmosphereConsumptionRate * _deuteriumPercentage * fixedDeltaTime / _deuterium.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium.density;
                _kryptonProductionRate = _kryptonPercentage == 0 ? 0 : -_part.RequestResource(_kryptonResourceName, -_atmosphereConsumptionRate * _kryptonPercentage * fixedDeltaTime / _krypton.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _krypton.density;
                _sodiumProductionRate = _sodiumPercentage == 0 ? 0 : -_part.RequestResource(_sodiumResourceName, -_atmosphereConsumptionRate * _sodiumPercentage * fixedDeltaTime / _sodium.density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _sodium.density;
            }
            else
            {
                _atmosphereConsumptionRate = 0;
                _ammoniaProductionRate = 0;
                _argonProductionRate = 0;
                _dioxideProductionRate = 0;
                _helium3ProductionRate = 0;
                _helium4ProductionRate = 0;
                _hydrogenProductionRate = 0;
                _methaneProductionRate = 0;
                _monoxideProductionRate = 0;
                _neonProductionRate = 0;
                _nitrogenProductionRate = 0;
                _nitrogen15ProductionRate = 0;
                _oxygenProductionRate = 0;
                _waterProductionRate = 0;
                _heavyWaterProductionRate = 0;
                _xenonProductionRate = 0;
                _deuteriumProductionRate = 0;
                _kryptonProductionRate = 0;
                _sodiumProductionRate = 0;
            }
        }


        public override void UpdateGUI()
        {
            base.UpdateGUI();

            if (_bold_label == null)
                _bold_label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };
            if (_value_label == null)
                _value_label = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };
            if (_value_label_green == null)
                _value_label_green = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, normal = {textColor = Color.green} };
            if (_value_label_red == null)
                _value_label_red = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, normal = {textColor = Color.red} };
            if (_value_label_number == null)
                _value_label_number = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, alignment = TextAnchor.MiddleRight };

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_AtmoConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Intake Atmo. Consumption"
            GUILayout.Label(((_atmosphereConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));//
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_AtmoAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Intake Atmo. Available"
            GUILayout.Label(_availableAtmosphereMass.ToString("0.0000") + " mT / " + _maxCapacityAtmosphereMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Name"), _bold_label, GUILayout.Width(valueWidth));//"Name"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Abundance"), _bold_label, GUILayout.Width(valueWidth));//"Abundance"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Productionpersecond"), _bold_label, GUILayout.Width(valueWidth));//"Production per second"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Productionperhour"), _bold_label, GUILayout.Width(valueWidth));//"Production per hour"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_SpareRoom"), _bold_label, GUILayout.Width(valueWidth));//"Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Stored"), _bold_label, GUILayout.Width(valueWidth));//"Stored"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_MaxCapacity"), _bold_label, GUILayout.Width(valueWidth));//"Max Capacity"
            GUILayout.EndHorizontal();

            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Hydrogen"), _hydrogenPercentage, _hydrogenProductionRate, _spareRoomHydrogenMass, _maxCapacityHydrogenMass);//"Hydrogen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Deuterium"), _deuteriumPercentage, _deuteriumProductionRate, _spareRoomDeuteriumMass, _maxCapacityDeuteriumMass);//"Deuterium"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Helium3"), _helium3Percentage, _helium3ProductionRate, _spareRoomHelium3Mass, _maxCapacityHelium3Mass);//"Helium-3"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Helium"), _helium4Percentage, _helium4ProductionRate, _spareRoomHelium4Mass, _maxCapacityHelium4Mass);//"Helium"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Nitrogen"), _nitrogenPercentage, _nitrogenProductionRate, _spareRoomNitrogenMass, _maxCapacityNitrogenMass);//"Nitrogen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Nitrogen15"), _nitrogen15Percentage, _nitrogen15ProductionRate, _spareRoomNitrogen15Mass, _maxCapacityNitrogen15Mass);//"Nitrogen-15"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Oxygen"), _oxygenPercentage, _oxygenProductionRate, _spareRoomOxygenMass, _maxCapacityOxygenMass);//"Oxygen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Argon"), _argonPercentage, _argonProductionRate, _spareRoomArgonMass, _maxCapacityArgonMass);//"Argon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Chlorine"), _chlorinePercentage, _chlorineProductionRate, _spareRoomChlorineMass, _maxCapacityChlorineMass);//"Chlorine"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Neon"), _neonPercentage, _neonProductionRate, _spareRoomNeonMass, _maxCapacityNeonMass);//"Neon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Krypton"), _kryptonPercentage, _kryptonProductionRate, _spareRoomKryptonMass, _maxCapacityKryptonMass);//"Krypton"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Ammonia"), _ammoniaPercentage, _ammoniaProductionRate, _spareRoomAmmoniaMass, _maxCapacityAmmoniaMass);//"Ammonia"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Water"), _waterPercentage, _waterProductionRate, _spareRoomWaterMass, _maxCapacityWaterMass);//"Water"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_HeavyWater"), _heavywaterPercentage, _heavyWaterProductionRate, _spareRoomHeavyWaterMass, _maxCapacityHeavyWaterMass);//"Heavy Water"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_CarbonMonoxide"), _monoxidePercentage, _monoxideProductionRate, _spareRoomMonoxideMass, _maxCapacityMonoxideMass);//"Carbon Monoxide"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_CarbonDioxide"), _dioxidePercentage, _dioxideProductionRate, _spareRoomDioxideMass, _maxCapacityDioxideMass);//"Carbon Dioxide"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Methane"), _methanePercentage, _methaneProductionRate, _spareRoomMethaneMass, _maxCapacityMethaneMass);//"Methane"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Xenon"), _xenonPercentage, _xenonProductionRate, _spareRoomXenonMass, _maxCapacityXenonMass);//"Xenon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Sodium"), _sodiumPercentage, _sodiumProductionRate, _spareRoomSodiumMass, _maxCapacitySodiumMass);//"Sodium"
        }

        private void DisplayResourceExtraction(string resourceName,  double percentage, double productionRate, double spareRoom, double maximumCapacity)
        {
            if (percentage <= 0)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Label(resourceName, _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label((percentage * 100).ToString("##.######") + "%", _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label(productionRate.ToString("##.######") + " U/s", _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label((productionRate * GameConstants.SECONDS_IN_HOUR).ToString("##.######") + " U/h", _value_label, GUILayout.Width(valueWidth));
            if (maximumCapacity > 0)
            {
                if (spareRoom > 0)
                    GUILayout.Label((spareRoom).ToString("##.######") + " t", _value_label, GUILayout.Width(valueWidth));
                else
                    GUILayout.Label("", _value_label, GUILayout.Width(valueWidth));

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
            if (normalizedTime == 0)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg1");//"Scoop is not deployed"
            else if (_intakeModifier == 0)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg2");//"Scoop is not heading into orbital direction"
            else if (_atmosphereConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg3");//"Extracting atmosphere"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg5");//"Insufficient Storage, try allowing overflow"
        }

        public void PrintMissingResources() 
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_PostMsg") + " " + InterstellarResourcesConfiguration._INTAKEATMOSPHERE, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }

        public void Update()
        {
            // Sail deployment GUI
            Events[nameof(DeployScoop)].active = _scoopAnimation != null && !isDeployed ;
            Events[nameof(RetractScoop)].active = _scoopAnimation != null && isDeployed;
        }

        private static void RunAnimation(string animationName, Animation anim, float speed, float aTime)
        {
            if (animationName == null || anim == null || string.IsNullOrEmpty(animationName))
                return;

            anim[animationName].speed = speed;
            if (anim.IsPlaying(animationName))
                return;

            anim[animationName].wrapMode = WrapMode.Default;
            anim[animationName].normalizedTime = aTime;
            anim.Blend(animationName, 1);
        }
    }
}
