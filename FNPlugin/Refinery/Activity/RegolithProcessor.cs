using FNPlugin.Collectors;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    [KSPModule("ISRU Regolith Processor")]
    class RegolithProcessor : RefineryActivity, IRefineryActivity
    {
        public RegolithProcessor()
        {
            ActivityName = "Regolith Process";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        private double _dFixedDeltaTime;
        private double _dFixedConsumptionRate;
        private double _dConsumptionStorageRatio;

        private double _dRegolithDensity;
        private double _dHydrogenDensity;
        private double _dDeuteriumDensity;
        private double _dLiquidHelium3Density;
        private double _dLiquidHelium4Density;
        private double _dMonoxideDensity;
        private double _dDioxideDensity;
        private double _dMethaneDensity;
        private double _dNitrogenDensity;
        private double _dWaterDensity;

        private double _fixedRegolithConsumptionRate;
        private double _regolithConsumptionRate;

        private double _dHydrogenProductionRate;
        private double _dDeuteriumProductionRate;
        private double _dLiquidHelium3ProductionRate;
        private double _dLiquidHelium4ProductionRate;
        private double _dMonoxideProductionRate;
        private double _dDioxideProductionRate;
        private double _dMethaneProductionRate;
        private double _dNitrogenProductionRate;
        private double _dWaterProductionRate;

        private string _strRegolithResourceName;
        private string _strHydrogenResourceName;
        private string _stDeuteriumResourceName;
        private string _strLiquidHelium3ResourceName;
        private string _strLiquidHelium4ResourceName;
        private string _strMonoxideResourceName;
        private string _strDioxideResourceName;
        private string _strMethaneResourceName;
        private string _strNitrogenResourceName;
        private string _strWaterResourceName;

        public RefineryType RefineryType => RefineryType.Heating;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_strRegolithResourceName).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);


        protected PartResourceDefinition deuteriumDefinition;

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _strRegolithResourceName = ResourceSettings.Config.Regolith;
            _strHydrogenResourceName = ResourceSettings.Config.HydrogenLqd;
            _stDeuteriumResourceName = ResourceSettings.Config.DeuteriumGas;
            _strLiquidHelium3ResourceName = ResourceSettings.Config.Helium3Gas;
            _strLiquidHelium4ResourceName = ResourceSettings.Config.Helium4Gas;
            _strMonoxideResourceName = ResourceSettings.Config.CarbonMonoxideGas;
            _strDioxideResourceName = ResourceSettings.Config.CarbonDioxideLqd;
            _strMethaneResourceName = ResourceSettings.Config.Methane;
            _strNitrogenResourceName = ResourceSettings.Config.Nitrogen;
            _strWaterResourceName = ResourceSettings.Config.Water;

            // should add Nitrogen15 and Argon

            _dRegolithDensity = PartResourceLibrary.Instance.GetDefinition(_strRegolithResourceName).density;
            _dHydrogenDensity = PartResourceLibrary.Instance.GetDefinition(_strHydrogenResourceName).density;
            _dDeuteriumDensity = PartResourceLibrary.Instance.GetDefinition(_stDeuteriumResourceName).density;
            _dLiquidHelium3Density = PartResourceLibrary.Instance.GetDefinition(_strLiquidHelium3ResourceName).density;
            _dLiquidHelium4Density = PartResourceLibrary.Instance.GetDefinition(_strLiquidHelium4ResourceName).density;
            _dMonoxideDensity = PartResourceLibrary.Instance.GetDefinition(_strMonoxideResourceName).density;
            _dDioxideDensity = PartResourceLibrary.Instance.GetDefinition(_strDioxideResourceName).density;
            _dMethaneDensity = PartResourceLibrary.Instance.GetDefinition(_strMethaneResourceName).density;
            _dNitrogenDensity = PartResourceLibrary.Instance.GetDefinition(_strNitrogenResourceName).density;
            _dWaterDensity = PartResourceLibrary.Instance.GetDefinition(_strWaterResourceName).density;

            deuteriumDefinition = PartResourceLibrary.Instance.GetDefinition(_stDeuteriumResourceName);
        }

        protected double dMaxCapacityRegolithMass;
        protected double dMaxCapacityHydrogenMass;
        protected double dMaxCapacityDeuteriumMass;
        protected double dMaxCapacityHelium3Mass;
        protected double dMaxCapacityHelium4Mass;
        protected double dMaxCapacityMonoxideMass;
        protected double dMaxCapacityDioxideMass;
        protected double dMaxCapacityMethaneMass;
        protected double dMaxCapacityNitrogenMass;
        protected double dMaxCapacityWaterMass;

        protected double dAvailableRegolithMass;
        protected double dSpareRoomHydrogenMass;
        protected double dSpareRoomDeuteriumMass;
        protected double dSpareRoomHelium3Mass;
        protected double dSpareRoomHelium4Mass;
        protected double dSpareRoomMonoxideMass;
        protected double dSpareRoomDioxideMass;
        protected double dSpareRoomMethaneMass;
        protected double dSpareRoomNitrogenMass;
        protected double dSpareRoomWaterMass;

        //double dFixedMaxDeuteriumRate;

        /* these are the constituents of regolith with their appropriate mass ratios. I'm using concentrations from lunar regolith, yes, I know regolith on other planets varies, let's keep this simple.
         * The exact fractions were calculated mostly from a chart that's also available on http://imgur.com/lpaE1Ah.
         */
        protected double dHydrogenMassByFraction = 0.3351424205;
        protected double dHelium3MassByFraction = 0.000054942036;
        protected double dHelium4MassByFraction = 0.1703203120;
        protected double dMonoxideMassByFraction = 0.1043898686;
        protected double dDioxideMassByFraction = 0.0934014614;
        protected double dMethaneMassByFraction = 0.0879072578;
        protected double dNitrogenMassByFraction = 0.0274710180;
        protected double dWaterMassByFraction = 0.18130871930;

        // deuterium/hydrogen: 13 ppm source https://www.researchgate.net/publication/234236795_Deuterium_content_of_lunar_material/link/5444faa20cf2e6f0c0fbff43/download
        protected double dDeuteriumMassByFraction = 0.000004355; // based on a measurement of 13 ppm of hydrogen beeing deuterium (13 ppm * 0.335 = 0.000004355)

        private double GetTotalExtractedPerSecond()
        {
            var collectorsList = _vessel.FindPartModulesImplementing<RegolithCollector>(); // add any atmo intake localPart on the vessel to our list
            return collectorsList.Where(m => m.bIsEnabled).Sum(m => m.resourceProduction);
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _dFixedDeltaTime = fixedDeltaTime;
            _effectiveMaxPower = PowerRequirements * productionModifier;
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine how much resource we have
            var partsThatContainRegolith = _part.GetConnectedResources(_strRegolithResourceName).ToList();
            var partsThatContainHydrogen = _part.GetConnectedResources(_strHydrogenResourceName).ToList();
            var partsThatContainDeuterium = _part.GetConnectedResources(_stDeuteriumResourceName).ToList();
            var partsThatContainLqdHelium3 = _part.GetConnectedResources(_strLiquidHelium3ResourceName).ToList();
            var partsThatContainLqdHelium4 = _part.GetConnectedResources(_strLiquidHelium4ResourceName).ToList();
            var partsThatContainMonoxide = _part.GetConnectedResources(_strMonoxideResourceName).ToList();
            var partsThatContainDioxide = _part.GetConnectedResources(_strDioxideResourceName).ToList(); ;
            var partsThatContainMethane = _part.GetConnectedResources(_strMethaneResourceName).ToList();
            var partsThatContainNitrogen = _part.GetConnectedResources(_strNitrogenResourceName).ToList();
            var partsThatContainWater = _part.GetConnectedResources(_strWaterResourceName).ToList();

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            dMaxCapacityRegolithMass = partsThatContainRegolith.Sum(p => p.maxAmount) * _dRegolithDensity;
            dMaxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _dHydrogenDensity;
            dMaxCapacityDeuteriumMass = partsThatContainDeuterium.Sum(p => p.maxAmount) * _dDeuteriumDensity;
            dMaxCapacityHelium3Mass = partsThatContainLqdHelium3.Sum(p => p.maxAmount) * _dLiquidHelium3Density;
            dMaxCapacityHelium4Mass = partsThatContainLqdHelium4.Sum(p => p.maxAmount) * _dLiquidHelium4Density;
            dMaxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _dMonoxideDensity;
            dMaxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dDioxideDensity;
            dMaxCapacityMethaneMass = partsThatContainMethane.Sum(p => p.maxAmount) * _dMethaneDensity;
            dMaxCapacityNitrogenMass = partsThatContainNitrogen.Sum(p => p.maxAmount) * _dNitrogenDensity;
            dMaxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _dWaterDensity;

            // determine the amount of resources needed for processing (i.e. regolith) that the vessel actually holds
            dAvailableRegolithMass = partsThatContainRegolith.Sum(r => r.amount) * _dRegolithDensity;

            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            dSpareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _dHydrogenDensity;
            dSpareRoomDeuteriumMass = partsThatContainDeuterium.Sum(r => r.maxAmount - r.amount) * deuteriumDefinition.density;
            dSpareRoomHelium3Mass = partsThatContainLqdHelium3.Sum(r => r.maxAmount - r.amount)  * _dLiquidHelium3Density;
            dSpareRoomHelium4Mass = partsThatContainLqdHelium4.Sum(r => r.maxAmount - r.amount) * _dLiquidHelium4Density;
            dSpareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _dMonoxideDensity;
            dSpareRoomDioxideMass = partsThatContainDioxide.Sum(r => r.maxAmount - r.amount) * _dDioxideDensity;
            dSpareRoomMethaneMass = partsThatContainMethane.Sum(r => r.maxAmount - r.amount) * _dMethaneDensity;
            dSpareRoomNitrogenMass = partsThatContainNitrogen.Sum(r => r.maxAmount - r.amount) * _dNitrogenDensity;
            dSpareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _dWaterDensity;

            // this should determine how much resource this process can consume
            var dFixedMaxRegolithConsumptionRate = _current_rate * fixedDeltaTime * _dRegolithDensity;

            // determine the amount of regolith collected
            var availableRegolithExtractionMassFixed = GetTotalExtractedPerSecond() * _dRegolithDensity * fixedDeltaTime;

            var dRegolithConsumptionRatio = dFixedMaxRegolithConsumptionRate > 0
                ? Math.Min(dFixedMaxRegolithConsumptionRate, Math.Max(availableRegolithExtractionMassFixed, dAvailableRegolithMass)) / dFixedMaxRegolithConsumptionRate
                : 0;

            _dFixedConsumptionRate = _current_rate * fixedDeltaTime * dRegolithConsumptionRatio;

            // begin the regolith processing
            if (_dFixedConsumptionRate > 0 && (
                dSpareRoomHydrogenMass > 0 ||
                dSpareRoomDeuteriumMass > 0 ||
                dSpareRoomHelium3Mass > 0 ||
                dSpareRoomHelium4Mass > 0 ||
                dSpareRoomMonoxideMass > 0 ||
                dSpareRoomDioxideMass > 0 ||
                dSpareRoomMethaneMass > 0 ||
                dSpareRoomNitrogenMass > 0 ||
                dSpareRoomWaterMass > 0)) // check if there is anything to consume and spare room for at least one of the products
            {

                double dFixedMaxHydrogenRate = _dFixedConsumptionRate * dHydrogenMassByFraction;
                double dFixedMaxDeuteriumRate = _dFixedConsumptionRate * dDeuteriumMassByFraction;
                double dFixedMaxHelium3Rate = _dFixedConsumptionRate * dHelium3MassByFraction;
                double dFixedMaxHelium4Rate = _dFixedConsumptionRate * dHelium4MassByFraction;
                double dFixedMaxMonoxideRate = _dFixedConsumptionRate * dMonoxideMassByFraction;
                double dFixedMaxDioxideRate = _dFixedConsumptionRate * dDioxideMassByFraction;
                double dFixedMaxMethaneRate = _dFixedConsumptionRate * dMethaneMassByFraction;
                double dFixedMaxNitrogenRate = _dFixedConsumptionRate * dNitrogenMassByFraction;
                double dFixedMaxWaterRate = _dFixedConsumptionRate * dWaterMassByFraction;

                double dFixedMaxPossibleHydrogenRate  = Math.Min(dSpareRoomHydrogenMass,  dFixedMaxHydrogenRate);
                double dFixedMaxPossibleDeuteriumRate = Math.Min(dSpareRoomDeuteriumMass, dFixedMaxDeuteriumRate);
                double dFixedMaxPossibleHelium3Rate   = Math.Min(dSpareRoomHelium3Mass,   dFixedMaxHelium3Rate);
                double dFixedMaxPossibleHelium4Rate   = Math.Min(dSpareRoomHelium4Mass,   dFixedMaxHelium4Rate);
                double dFixedMaxPossibleMonoxideRate  = Math.Min(dSpareRoomMonoxideMass,  dFixedMaxMonoxideRate);
                double dFixedMaxPossibleDioxideRate   = Math.Min(dSpareRoomDioxideMass,   dFixedMaxDioxideRate);
                double dFixedMaxPossibleMethaneRate   = Math.Min(dSpareRoomMethaneMass,   dFixedMaxMethaneRate);
                double dFixedMaxPossibleNitrogenRate  = Math.Min(dSpareRoomNitrogenMass,  dFixedMaxNitrogenRate);
                double dFixedMaxPossibleWaterRate     = Math.Min(dSpareRoomWaterMass,     dFixedMaxWaterRate);

                var ratios = new List<double> {
                    dFixedMaxPossibleHydrogenRate / dFixedMaxHydrogenRate,
                    dFixedMaxPossibleDeuteriumRate / dFixedMaxDeuteriumRate,
                    dFixedMaxPossibleHelium3Rate / dFixedMaxHelium3Rate,
                    dFixedMaxPossibleHelium4Rate / dFixedMaxHelium4Rate,
                    dFixedMaxPossibleMonoxideRate / dFixedMaxMonoxideRate,
                    dFixedMaxPossibleNitrogenRate / dFixedMaxNitrogenRate,
                    dFixedMaxPossibleWaterRate / dFixedMaxWaterRate,
                    dFixedMaxPossibleDioxideRate / dFixedMaxDioxideRate,
                    dFixedMaxPossibleMethaneRate / dFixedMaxMethaneRate };

                _dConsumptionStorageRatio =  allowOverflow ? ratios.Max(m => m) : ratios.Min(m => m);

                // this consumes the resource
                var fixedCollectedRegolith = _part.RequestResource(_strRegolithResourceName, _dConsumptionStorageRatio * _dFixedConsumptionRate / _dRegolithDensity, ResourceFlowMode.STACK_PRIORITY_SEARCH) * _dRegolithDensity;

                _fixedRegolithConsumptionRate = Math.Max(fixedCollectedRegolith, availableRegolithExtractionMassFixed);

                _regolithConsumptionRate = _fixedRegolithConsumptionRate / fixedDeltaTime;

                // this produces the products
                double dHydrogenRateTemp = _fixedRegolithConsumptionRate * dHydrogenMassByFraction;
                double dDeuteriumRateTemp = _fixedRegolithConsumptionRate * dDeuteriumMassByFraction;
                double dHelium3RateTemp = _fixedRegolithConsumptionRate * dHelium3MassByFraction;
                double dHelium4RateTemp = _fixedRegolithConsumptionRate * dHelium4MassByFraction;
                double dMonoxideRateTemp = _fixedRegolithConsumptionRate * dMonoxideMassByFraction;
                double dDioxideRateTemp = _fixedRegolithConsumptionRate * dDioxideMassByFraction;
                double dMethaneRateTemp = _fixedRegolithConsumptionRate * dMethaneMassByFraction;
                double dNitrogenRateTemp = _fixedRegolithConsumptionRate * dNitrogenMassByFraction;
                double dWaterRateTemp = _fixedRegolithConsumptionRate * dWaterMassByFraction;

                _dHydrogenProductionRate = -_part.RequestResource(_strHydrogenResourceName, -dHydrogenRateTemp  / _dHydrogenDensity) / fixedDeltaTime * _dHydrogenDensity;
                _dDeuteriumProductionRate = -_part.RequestResource(_stDeuteriumResourceName, -dDeuteriumRateTemp / _dDeuteriumDensity) / fixedDeltaTime * _dDeuteriumDensity;
                _dLiquidHelium3ProductionRate = -_part.RequestResource(_strLiquidHelium3ResourceName, -dHelium3RateTemp  / _dLiquidHelium3Density) / fixedDeltaTime * _dLiquidHelium3Density;
                _dLiquidHelium4ProductionRate = -_part.RequestResource(_strLiquidHelium4ResourceName, -dHelium4RateTemp  / _dLiquidHelium4Density) / fixedDeltaTime * _dLiquidHelium4Density;
                _dMonoxideProductionRate = -_part.RequestResource(_strMonoxideResourceName, -dMonoxideRateTemp  / _dMonoxideDensity) / fixedDeltaTime * _dMonoxideDensity;
                _dDioxideProductionRate = -_part.RequestResource(_strDioxideResourceName, -dDioxideRateTemp  / _dDioxideDensity) / fixedDeltaTime * _dDioxideDensity;
                _dMethaneProductionRate = -_part.RequestResource(_strMethaneResourceName, -dMethaneRateTemp  / _dMethaneDensity) / fixedDeltaTime * _dMethaneDensity;
                _dNitrogenProductionRate = -_part.RequestResource(_strNitrogenResourceName, -dNitrogenRateTemp  / _dNitrogenDensity) / fixedDeltaTime * _dNitrogenDensity;
                _dWaterProductionRate = -_part.RequestResource(_strWaterResourceName, -dWaterRateTemp  / _dWaterDensity) / fixedDeltaTime * _dWaterDensity;
            }
            else
            {
                _fixedRegolithConsumptionRate = 0;
                _dHydrogenProductionRate = 0;
                _dDeuteriumProductionRate = 0;
                _dLiquidHelium3ProductionRate = 0;
                _dLiquidHelium4ProductionRate = 0;
                _dMonoxideProductionRate = 0;
                _dDioxideProductionRate = 0;
                _dMethaneProductionRate = 0;
                _dNitrogenProductionRate = 0;
                _dWaterProductionRate = 0;
            }
            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Consumption"), _bold_label, GUILayout.Width(labelWidth));//"Regolith Consumption"
            GUILayout.Label(((_regolithConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Available"), _bold_label, GUILayout.Width(labelWidth));//"Regolith Available"
            GUILayout.Label(dAvailableRegolithMass.ToString("0.000000") + " mT / " + dMaxCapacityRegolithMass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Output"), _bold_label, GUILayout.Width(labelWidth));//"Output"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_ResourceName"), _bold_label, GUILayout.Width(labelWidth));//"Resource Name"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_SpareRoom"), _bold_label, GUILayout.Width(labelWidth));//"Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_MaximumStorage"), _bold_label, GUILayout.Width(labelWidth));//"Maximum Storage"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Production Rate"
            GUILayout.EndHorizontal();

            DisplayResourceOutput(_strHydrogenResourceName, dSpareRoomHydrogenMass, dMaxCapacityHydrogenMass, _dHydrogenProductionRate);
            DisplayResourceOutput(_stDeuteriumResourceName, dSpareRoomDeuteriumMass, dMaxCapacityDeuteriumMass, _dDeuteriumProductionRate);
            DisplayResourceOutput(_strLiquidHelium3ResourceName, dSpareRoomHelium3Mass, dMaxCapacityHelium3Mass, _dLiquidHelium3ProductionRate);
            DisplayResourceOutput(_strLiquidHelium4ResourceName, dSpareRoomHelium4Mass, dMaxCapacityHelium4Mass, _dLiquidHelium4ProductionRate);
            DisplayResourceOutput(_strMonoxideResourceName, dSpareRoomMonoxideMass, dMaxCapacityMonoxideMass, _dMonoxideProductionRate);
            DisplayResourceOutput(_strDioxideResourceName, dSpareRoomDioxideMass, dMaxCapacityDioxideMass, _dDioxideProductionRate);
            DisplayResourceOutput(_strMethaneResourceName, dSpareRoomMethaneMass, dMaxCapacityMethaneMass, _dMethaneProductionRate);
            DisplayResourceOutput(_strNitrogenResourceName, dSpareRoomNitrogenMass, dMaxCapacityNitrogenMass, _dNitrogenProductionRate);
            DisplayResourceOutput(_strWaterResourceName, dSpareRoomWaterMass, dMaxCapacityWaterMass, _dWaterProductionRate);
        }

        private void DisplayResourceOutput(string resourceName, double spareRoom, double maxCapacity, double productionRate)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(resourceName, _value_label, GUILayout.Width(labelWidth));
            GUILayout.Label(spareRoom.ToString("0.000000") + " mT", maxCapacity > 0 && spareRoom == 0 ? _value_label_red : _value_label, GUILayout.Width(labelWidth));
            GUILayout.Label(maxCapacity.ToString("0.000000") + " mT", maxCapacity == 0 ? _value_label_red : _value_label, GUILayout.Width(labelWidth));
            GUILayout.Label((productionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.000000") + " mT/hour", productionRate > 0 ? _value_label_green : _value_label, GUILayout.Width(labelWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_fixedRegolithConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_RegolithProcessor_Statumsg1");//"Processing of Regolith Ongoing"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_RegolithProcessor_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_RegolithProcessor_Statumsg3");//"Insufficient Storage, try allowing overflow"
        }

        public void PrintMissingResources()
        {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Postmsg") +" " + ResourceSettings.Config.Regolith, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
