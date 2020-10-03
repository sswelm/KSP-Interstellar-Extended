using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class AtmosphericExtractor : PartModule, IRefineryActivity
    {
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

        public static int labelWidth = 180;
        public static int valueWidth = 180;

        Animation scoopAnimation = null;
        Part _part;
        Vessel _vessel;
        GUIStyle _bold_label;
        GUIStyle _value_label;
        GUIStyle _value_label_green;
        GUIStyle _value_label_red;
        GUIStyle _value_label_number;        

        string _status = "";
        double _current_power;
        double _current_rate;
        double _effectiveMaxPower;

        // persistent
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


        double _fixedConsumptionRate;
        double _consumptionStorageRatio;
        double intakeModifier;

        PartResourceDefinition _atmosphere;

        // all the gases that it should be possible to collect from atmospheres
        PartResourceDefinition _ammonia;
        PartResourceDefinition _argon;
        PartResourceDefinition _chlorine;
        PartResourceDefinition _dioxide;
        PartResourceDefinition _helium3;
        PartResourceDefinition _helium4;
        PartResourceDefinition _hydrogen;
        PartResourceDefinition _methane;
        PartResourceDefinition _monoxide;
        PartResourceDefinition _neon;
        PartResourceDefinition _nitrogen;
        PartResourceDefinition _nitrogen15;
        PartResourceDefinition _oxygen;
        PartResourceDefinition _water; // water vapour can form a part of atmosphere as well
        PartResourceDefinition _heavyWater;
        PartResourceDefinition _xenon;
        PartResourceDefinition _deuterium;
        PartResourceDefinition _krypton;
        PartResourceDefinition _sodium;

        double _atmosphere_density;
        double _ammonia_density;
        double _argon_density;
        double _chlorine_density;
        double _dioxide_density;
        double _helium3_density;
        double _helium4_density;
        double _hydrogen_density;
        double _methane_density;
        double _monoxide_density;
        double _neon_density;
        double _nitrogen_density;
        double _nitrogen15_density;
        double _oxygen_density;
        double _water_density;
        double _heavywater_density;
        double _xenon_density;
        double _deuterium_density;
        double _krypton_density;
        double _sodium_density;

             
        double _atmosphere_consumption_rate;

        double _ammonia_production_rate;
        double _argon_production_rate;
        double _chlorine_production_rate;
        double _dioxide_production_rate;
        double _helium3_production_rate;
        double _helium4_production_rate;
        double _hydrogen_production_rate;
        double _methane_production_rate;
        double _monoxide_production_rate;
        double _neon_production_rate;
        double _nitrogen_production_rate;
        double _nitrogen15_production_rate;
        double _oxygen_production_rate;
        double _water_production_rate;
        double _heavywater_production_rate;
        double _xenon_production_rate;
        double _deuterium_production_rate;
        double _krypton_production_rate;
        double _sodium_production_rate;

        string _atmosphere_resource_name;

        string _ammonia_resource_name;
        string _argon_resource_name;
        string _chlorine_resource_name;
        string _dioxide_resource_name;
        string _helium3_resource_name;
        string _helium4_resource_name;
        string _hydrogen_resource_name;
        string _methane_resource_name;
        string _monoxide_resource_name;
        string _neon_resource_name;
        string _nitrogen_resource_name;
        string _nitrogen15_resource_name;
        string _oxygen_resource_name;
        string _water_resource_name;
        string _heavywater_resource_name;
        string _xenon_resource_name;
        string _deuterium_resource_name;
        string _krypton_resource_name;
        string _sodium_resource_name;


        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_DeployScoop", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]//Deploy Scoop
        public void DeployScoop()
        {
            runAnimation(animName, scoopAnimation, 0.5f, 0);
            isDeployed = true;
        }

        // GUI to retract sail
        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_RetractScoop", active = false, guiActiveUncommand = true, guiActiveUnfocused = true)]//Retract Scoop
        public void RetractScoop()
        {
            runAnimation(animName, scoopAnimation, -0.5f, 1);
            isDeployed = false;
        }

        public double CurrentPower => _current_power;

        public RefineryType RefineryType => RefineryType.Cryogenics;

        public string ActivityName => "Atmospheric Extraction";

        public bool HasActivityRequirements()
        {
            return true;
        }

        public double PowerRequirements => PluginHelper.BaseELCPowerConsumption;

        public string Status => string.Copy(_status);

        public void Initialize(Part part)
        {
            _part = part;
            _vessel = part.vessel;
            _intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>();

            if (!string.IsNullOrEmpty(animName))
            {
                scoopAnimation = part.FindModelAnimators(animName).First();

                if (scoopAnimation != null)
                {
                    scoopAnimation[animName].speed = 0;
                    scoopAnimation[animName].normalizedTime = isDeployed ? 1 : 0;
                    scoopAnimation.Blend(animName);
                }
            }

            // get the name of all relevant resources
            _atmosphere_resource_name = InterstellarResourcesConfiguration._INTAKEATMOSPHERE;

            _ammonia_resource_name = InterstellarResourcesConfiguration._LIQUID_AMMONIA;
            _argon_resource_name = InterstellarResourcesConfiguration._LIQUID_ARGON;
            _chlorine_resource_name = InterstellarResourcesConfiguration._CHLORINE;
            _dioxide_resource_name = InterstellarResourcesConfiguration._LIQUID_CO2;
            _monoxide_resource_name = InterstellarResourcesConfiguration._CARBONMONOXIDE_LIQUID;
            _helium3_resource_name = InterstellarResourcesConfiguration._HELIUM3_LIQUID;
            _helium4_resource_name = InterstellarResourcesConfiguration._HELIUM4_LIQUID;
            _hydrogen_resource_name = InterstellarResourcesConfiguration._HYDROGEN_LIQUID;
            _methane_resource_name = InterstellarResourcesConfiguration._LIQUID_METHANE;
            _neon_resource_name = InterstellarResourcesConfiguration._NEON_LIQUID;
            _nitrogen_resource_name = InterstellarResourcesConfiguration._NITROGEN_LIQUID;
            _nitrogen15_resource_name = InterstellarResourcesConfiguration._LIQUID_NITROGEN_15;
            _oxygen_resource_name = InterstellarResourcesConfiguration._LIQUID_OXYGEN;
            _water_resource_name = InterstellarResourcesConfiguration._LIQUID_WATER;
            _heavywater_resource_name = InterstellarResourcesConfiguration._LIQUID_HEAVYWATER;
            _xenon_resource_name = InterstellarResourcesConfiguration._LIQUID_XENON;
            _deuterium_resource_name = InterstellarResourcesConfiguration._DEUTERIUM_LIQUID;
            _krypton_resource_name = InterstellarResourcesConfiguration._LIQUID_KRYPTON;

            _sodium_resource_name = InterstellarResourcesConfiguration.Instance.Sodium;
            
            // get the densities of all relevant resources
            _atmosphere = PartResourceLibrary.Instance.GetDefinition(_atmosphere_resource_name);
            _ammonia = PartResourceLibrary.Instance.GetDefinition(_ammonia_resource_name);
            _argon = PartResourceLibrary.Instance.GetDefinition(_argon_resource_name);
            _chlorine = PartResourceLibrary.Instance.GetDefinition(_chlorine_resource_name);
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
            _heavyWater = PartResourceLibrary.Instance.GetDefinition(_heavywater_resource_name);
            _xenon = PartResourceLibrary.Instance.GetDefinition(_xenon_resource_name);
            _deuterium = PartResourceLibrary.Instance.GetDefinition(_deuterium_resource_name);
            _krypton = PartResourceLibrary.Instance.GetDefinition(_krypton_resource_name);
            _sodium = PartResourceLibrary.Instance.GetDefinition(_sodium_resource_name);

			_atmosphere_density = (double)(decimal)_atmosphere.density;
			_ammonia_density = (double)(decimal)_ammonia.density;
			_argon_density = (double)(decimal)_argon.density;
			_chlorine_density = (double)(decimal)_chlorine.density;
			_dioxide_density = (double)(decimal)_dioxide.density;
			_helium3_density = (double)(decimal)_helium3.density;
			_helium4_density = (double)(decimal)_helium4.density;
			_hydrogen_density = (double)(decimal)_hydrogen.density;
			_methane_density = (double)(decimal)_methane.density;
			_monoxide_density = (double)(decimal)_monoxide.density;
			_neon_density = (double)(decimal)_neon.density;
			_nitrogen_density = (double)(decimal)_nitrogen.density;
			_nitrogen15_density = (double)(decimal)_nitrogen15.density;
			_oxygen_density = (double)(decimal)_oxygen.density;
			_water_density = (double)(decimal)_water.density;
			_heavywater_density = (double)(decimal)_heavyWater.density;
			_xenon_density = (double)(decimal)_xenon.density;
			_deuterium_density = (double)(decimal)_deuterium.density;
			_krypton_density = (double)(decimal)_krypton.density;
			_sodium_density = (double)(decimal)_sodium.density;
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
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

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
            if (scoopAnimation != null)
            {
                var animationState = scoopAnimation[animName];
                normalizedTime = animationState.normalizedTime == 0
                    ? isDeployed ? 1 : 0
                    : animationState.normalizedTime;
            }
            else
                normalizedTime = 1;

            // intake can only function when heading towards orbital path
            intakeModifier = scoopAnimation == null ? 1 : Math.Max(0, Vector3d.Dot(part.transform.up, part.vessel.obt_velocity.normalized));

            try
            {
                // calculate build in scoop capacity
                buildInAirIntake = normalizedTime <= 0.2 ? 0 :
                    AtmosphericFloatCurves.GetAtmosphericGasDensityKgPerCubicMeter(_vessel) * (1 + _vessel.obt_speed) * surfaceArea * intakeModifier * Math.Sqrt((normalizedTime - 0.2) * 1.25);
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
                        _ammoniaPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _ammonia_resource_name);
                        _argonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _argon_resource_name);
                        _chlorinePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _chlorine_resource_name);
                        _monoxidePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _monoxide_resource_name);
                        _dioxidePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _dioxide_resource_name);
                        _helium3Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _helium3_resource_name);
                        _helium4Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _helium4_resource_name);
                        _hydrogenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _hydrogen_resource_name);
                        _methanePercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _methane_resource_name);
                        _neonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _neon_resource_name);
                        _nitrogenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _nitrogen_resource_name);
                        _nitrogen15Percentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _nitrogen15_resource_name);
                        _oxygenPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _oxygen_resource_name);
                        _waterPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _water_resource_name);
                        _heavywaterPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _heavywater_resource_name);
                        _xenonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _xenon_resource_name);
                        _deuteriumPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _deuterium_resource_name);
                        _kryptonPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _krypton_resource_name);
                        _sodiumPercentage = AtmosphericResourceHandler.getAtmosphericResourceContent(FlightGlobals.currentMainBody, _sodium_resource_name);

                        lastBodyID = FlightGlobals.currentMainBody.flightGlobalsIndex; // reassign the id of current body to the lastBodyID variable, ie. remember this planet, so that we skip this check next time!
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[KSPI]: ExtractAir getAtmosphericResourceContent Exception: " + e.Message);
                    }



                if (offlineCollecting) // if we're collecting offline, we don't need to actually consume the resource, just provide the lines below with a number
                {
                    _atmosphere_consumption_rate = Math.Min(_current_rate, buildInAirIntake + GetTotalAirScoopedPerSecond());
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Postmsg1", _atmosphere_resource_name, fixedDeltaTime.ToString("F0")), 60.0f, ScreenMessageStyle.UPPER_CENTER);//"The atmospheric extractor processed " +  + " for " +  + " seconds"
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
                    _atmosphere_consumption_rate = buildInAirIntake;

                    // calculate missing atmospheric which can be extracted from air intakes
                    var remainingConsumptionNeeded = Math.Max(0, buildInAirIntake - max_atmospheric_consumption_rate);

                    // add the consumed atmosphere total atmospheric consumption rate
                    _atmosphere_consumption_rate += _part.RequestResource(_atmosphere_resource_name, remainingConsumptionNeeded / _atmosphere_density) / fixedDeltaTime * _atmosphere_density;
                }
                
                // produce the resources
                _ammonia_production_rate = _ammoniaPercentage == 0 ? 0 : -_part.RequestResource(_ammonia_resource_name, -_atmosphere_consumption_rate * _ammoniaPercentage * fixedDeltaTime / _ammonia_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _ammonia_density;
                _argon_production_rate = _argonPercentage == 0 ? 0 : -_part.RequestResource(_argon_resource_name, -_atmosphere_consumption_rate * _argonPercentage * fixedDeltaTime / _argon_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _argon_density;
                _chlorine_production_rate = _chlorinePercentage == 0 ? 0 : -_part.RequestResource(_chlorine_resource_name, -_atmosphere_consumption_rate * _chlorinePercentage * fixedDeltaTime / _chlorine_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _chlorine_density;
                _dioxide_production_rate = _dioxidePercentage == 0 ? 0 : -_part.RequestResource(_dioxide_resource_name, -_atmosphere_consumption_rate * _dioxidePercentage * fixedDeltaTime / _dioxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _dioxide_density;
                _helium3_production_rate = _helium3Percentage == 0 ? 0 : -_part.RequestResource(_helium3_resource_name, -_atmosphere_consumption_rate * _helium3Percentage * fixedDeltaTime / _helium3_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium3_density;
                _helium4_production_rate = _helium4Percentage == 0 ? 0 : -_part.RequestResource(_helium4_resource_name, -_atmosphere_consumption_rate * _helium4Percentage * fixedDeltaTime / _helium4_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium4_density;
                _hydrogen_production_rate = _hydrogenPercentage == 0 ? 0 : -_part.RequestResource(_hydrogen_resource_name, -_atmosphere_consumption_rate * _hydrogenPercentage * fixedDeltaTime / _hydrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen_density;
                _methane_production_rate = _methanePercentage == 0 ? 0 : -_part.RequestResource(_methane_resource_name, -_atmosphere_consumption_rate * _methanePercentage * fixedDeltaTime / _methane_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _methane_density;
                _monoxide_production_rate = _monoxidePercentage == 0 ? 0 : -_part.RequestResource(_monoxide_resource_name, -_atmosphere_consumption_rate * _monoxidePercentage * fixedDeltaTime / _monoxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_density;
                _neon_production_rate = _neonPercentage == 0 ? 0 : -_part.RequestResource(_neon_resource_name, -_atmosphere_consumption_rate * _neonPercentage * fixedDeltaTime / _neon_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neon_density;
                _nitrogen_production_rate = _nitrogenPercentage == 0 ? 0 : -_part.RequestResource(_nitrogen_resource_name, -_atmosphere_consumption_rate * _nitrogenPercentage * fixedDeltaTime / _nitrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen_density;
                _nitrogen15_production_rate = _nitrogen15Percentage == 0 ? 0 : -_part.RequestResource(_nitrogen15_resource_name, -_atmosphere_consumption_rate * _nitrogen15Percentage * fixedDeltaTime / _nitrogen15_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen15_density;
                _oxygen_production_rate = _oxygenPercentage == 0 ? 0 : -_part.RequestResource(_oxygen_resource_name, -_atmosphere_consumption_rate * _oxygenPercentage * fixedDeltaTime / _oxygen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen_density;
                _water_production_rate = _waterPercentage == 0 ? 0 : -_part.RequestResource(_water_resource_name, -_atmosphere_consumption_rate * _waterPercentage * fixedDeltaTime / _water_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _water.density;
                _heavywater_production_rate = _heavywaterPercentage == 0 ? 0 : -_part.RequestResource(_heavywater_resource_name, -_atmosphere_consumption_rate * _heavywaterPercentage * fixedDeltaTime / _heavywater_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _heavywater_density;
                _xenon_production_rate = _xenonPercentage == 0 ? 0 : -_part.RequestResource(_xenon_resource_name, -_atmosphere_consumption_rate * _xenonPercentage * fixedDeltaTime / _xenon_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _xenon_density;
                _deuterium_production_rate = _deuteriumPercentage == 0 ? 0 : -_part.RequestResource(_deuterium_resource_name, -_atmosphere_consumption_rate * _deuteriumPercentage * fixedDeltaTime / _deuterium_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium_density;
                _krypton_production_rate = _kryptonPercentage == 0 ? 0 : -_part.RequestResource(_krypton_resource_name, -_atmosphere_consumption_rate * _kryptonPercentage * fixedDeltaTime / _krypton_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _krypton_density;
                _sodium_production_rate = _sodiumPercentage == 0 ? 0 : -_part.RequestResource(_sodium_resource_name, -_atmosphere_consumption_rate * _sodiumPercentage * fixedDeltaTime / _sodium_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _sodium_density;
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


        public void UpdateGUI()
        {
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
            GUILayout.Label(((_atmosphere_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));//
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

            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Hydrogen"), _hydrogenPercentage, _hydrogen_production_rate, _spareRoomHydrogenMass, _maxCapacityHydrogenMass);//"Hydrogen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Deuterium"), _deuteriumPercentage, _deuterium_production_rate, _spareRoomDeuteriumMass, _maxCapacityDeuteriumMass);//"Deuterium"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Helium3"), _helium3Percentage, _helium3_production_rate, _spareRoomHelium3Mass, _maxCapacityHelium3Mass);//"Helium-3"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Helium"), _helium4Percentage, _helium4_production_rate, _spareRoomHelium4Mass, _maxCapacityHelium4Mass);//"Helium"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Nitrogen"), _nitrogenPercentage, _nitrogen_production_rate, _spareRoomNitrogenMass, _maxCapacityNitrogenMass);//"Nitrogen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Nitrogen15"), _nitrogen15Percentage, _nitrogen15_production_rate, _spareRoomNitrogen15Mass, _maxCapacityNitrogen15Mass);//"Nitrogen-15"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Oxygen"), _oxygenPercentage, _oxygen_production_rate, _spareRoomOxygenMass, _maxCapacityOxygenMass);//"Oxygen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Argon"), _argonPercentage, _argon_production_rate, _spareRoomArgonMass, _maxCapacityArgonMass);//"Argon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Chlorine"), _chlorinePercentage, _chlorine_production_rate, _spareRoomChlorineMass, _maxCapacityChlorineMass);//"Chlorine"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Neon"), _neonPercentage, _neon_production_rate, _spareRoomNeonMass, _maxCapacityNeonMass);//"Neon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Krypton"), _kryptonPercentage, _krypton_production_rate, _spareRoomKryptonMass, _maxCapacityKryptonMass);//"Krypton"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Ammonia"), _ammoniaPercentage, _ammonia_production_rate, _spareRoomAmmoniaMass, _maxCapacityAmmoniaMass);//"Ammonia"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Water"), _waterPercentage, _water_production_rate, _spareRoomWaterMass, _maxCapacityWaterMass);//"Water"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_HeavyWater"), _heavywaterPercentage, _heavywater_production_rate, _spareRoomHeavyWaterMass, _maxCapacityHeavyWaterMass);//"Heavy Water"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_CarbonMonoxide"), _monoxidePercentage, _monoxide_production_rate, _spareRoomMonoxideMass, _maxCapacityMonoxideMass);//"Carbon Monoxide"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_CarbonDioxide"), _dioxidePercentage, _dioxide_production_rate, _spareRoomDioxideMass, _maxCapacityDioxideMass);//"Carbon Dioxide"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Methane"), _methanePercentage, _methane_production_rate, _spareRoomMethaneMass, _maxCapacityMethaneMass);//"Methane"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Xenon"), _xenonPercentage, _xenon_production_rate, _spareRoomXenonMass, _maxCapacityXenonMass);//"Xenon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Sodium"), _sodiumPercentage, _sodium_production_rate, _spareRoomSodiumMass, _maxCapacitySodiumMass);//"Sodium"
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
            else if (intakeModifier == 0)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg2");//"Scoop is not heading into orbital direction"
            else if (_atmosphere_consumption_rate > 0)
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
            Events["DeployScoop"].active = scoopAnimation != null && !isDeployed ;
            Events["RetractScoop"].active = scoopAnimation != null && isDeployed;
        }

        private static void runAnimation(string animationName, Animation anim, float speed, float aTime)
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
