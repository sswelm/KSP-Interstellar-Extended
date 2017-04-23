using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class RegolithProcessor : RefineryActivityBase, IRefineryActivity
    {
        protected double dFixedConsumptionRate;
        protected double dConsumptionStorageRatio;

        protected double dRegolithDensity;
        protected double dHydrogenDensity;
        protected double dLiquidHelium3Density;
        protected double dLiquidHelium4Density;
        protected double dMonoxideDensity;
        protected double dDioxideDensity;
        protected double dMethaneDensity;
        protected double dNitrogenDensity;
        protected double dWaterDensity;

        protected double fixed_regolithConsumptionRate;
        protected double regolithConsumptionRate;

        protected double dHydrogenProductionRate;
        protected double dLiquidHelium3ProductionRate;
        protected double dLiquidHelium4ProductionRate;
        protected double dMonoxideProductionRate;
        protected double dDioxideProductionRate;
        protected double dMethaneProductionRate;
        protected double dNitrogenProductionRate;
        protected double dWaterProductionRate;

        public RefineryType RefineryType { get { return RefineryType.heating; } }

        public String ActivityName { get { return "Regolith Process"; } }

        public bool HasActivityRequirements
        {
            get
            {
                return _part.GetConnectedResources(strRegolithResourceName).Any(rs => rs.amount > 0);
            }
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        protected string strRegolithResourceName;
        protected string strHydrogenResourceName;
        protected string strLiquidHelium3ResourceName;
        protected string strLiquidHelium4ResourceName;
        protected string strMonoxideResourceName;
        protected string strDioxideResourceName;
        protected string strMethaneResourceName;
        protected string strNitrogenResourceName;
        protected string strWaterResourceName;

        public RegolithProcessor(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            strRegolithResourceName = InterstellarResourcesConfiguration.Instance.Regolith;
            strHydrogenResourceName = InterstellarResourcesConfiguration.Instance.Hydrogen;
            strLiquidHelium3ResourceName = InterstellarResourcesConfiguration.Instance.LqdHelium3;
            strLiquidHelium4ResourceName = InterstellarResourcesConfiguration.Instance.LqdHelium4;
            strMonoxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            strDioxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            strMethaneResourceName = InterstellarResourcesConfiguration.Instance.Methane;
            strNitrogenResourceName = InterstellarResourcesConfiguration.Instance.Nitrogen;
            strWaterResourceName = InterstellarResourcesConfiguration.Instance.Water;

            dRegolithDensity = PartResourceLibrary.Instance.GetDefinition(strRegolithResourceName).density;
            dHydrogenDensity = PartResourceLibrary.Instance.GetDefinition(strHydrogenResourceName).density;
            dLiquidHelium3Density = PartResourceLibrary.Instance.GetDefinition(strLiquidHelium3ResourceName).density;
            dLiquidHelium4Density = PartResourceLibrary.Instance.GetDefinition(strLiquidHelium4ResourceName).density;
            dMonoxideDensity = PartResourceLibrary.Instance.GetDefinition(strMonoxideResourceName).density;
            dDioxideDensity = PartResourceLibrary.Instance.GetDefinition(strDioxideResourceName).density;
            dMethaneDensity = PartResourceLibrary.Instance.GetDefinition(strMethaneResourceName).density;
            dNitrogenDensity = PartResourceLibrary.Instance.GetDefinition(strNitrogenResourceName).density;
            dWaterDensity = PartResourceLibrary.Instance.GetDefinition(strWaterResourceName).density;
        }

        protected double dMaxCapacityRegolithMass;
        protected double dMaxCapacityHydrogenMass;
        protected double dMaxCapacityHelium3Mass;
        protected double dMaxCapacityHelium4Mass;
        protected double dMaxCapacityMonoxideMass;
        protected double dMaxCapacityDioxideMass;
        protected double dMaxCapacityMethaneMass;
        protected double dMaxCapacityNitrogenMass;
        protected double dMaxCapacityWaterMass;

        protected double dAvailableRegolithMass;
        protected double dSpareRoomHydrogenMass;
        protected double dSpareRoomHelium3Mass;
        protected double dSpareRoomHelium4Mass;
        protected double dSpareRoomMonoxideMass;
        protected double dSpareRoomDioxideMass;
        protected double dSpareRoomMethaneMass;
        protected double dSpareRoomNitrogenMass;
        protected double dSpareRoomWaterMass;

        /* these are the constituents of regolith with their appropriate mass ratios. I'm using concentrations from lunar regolith, yes, I know regolith on other planets varies, let's keep this simple.
         * The exact fractions were calculated mostly from a chart that's also available on http://imgur.com/lpaE1Ah.
         */
        protected double dHydrogenMassByFraction = 0.3351464205;
        protected double dHelium3MassByFraction = 0.000054942036;
        protected double dHelium4MassByFraction = 0.1703203120;
        protected double dMonoxideMassByFraction = 0.1043898686;
        protected double dDioxideMassByFraction = 0.0934014614;
        protected double dMethaneMassByFraction = 0.0879072578;
        protected double dNitrogenMassByFraction = 0.0274710180;
        protected double dWaterMassByFraction = 0.18130871930;

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime)
        {
            _effectiveMaxPower = PowerRequirements * productionModidier;
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            // determine how much resource we have
            var partsThatContainRegolith = _part.GetConnectedResources(strRegolithResourceName);
            var partsThatContainHydrogen = _part.GetConnectedResources(strHydrogenResourceName);
            var partsThatContainLqdHelium3 = _part.GetConnectedResources(strLiquidHelium3ResourceName);
            var partsThatContainLqdHelium4 = _part.GetConnectedResources(strLiquidHelium4ResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(strMonoxideResourceName);
            var partsThatContainDioxide = _part.GetConnectedResources(strDioxideResourceName);
            var partsThatContainMethane = _part.GetConnectedResources(strMethaneResourceName);
            var partsThatContainNitrogen = _part.GetConnectedResources(strNitrogenResourceName);
            var partsThatContainWater = _part.GetConnectedResources(strWaterResourceName);

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            dMaxCapacityRegolithMass = partsThatContainRegolith.Sum(p => p.maxAmount) * dRegolithDensity;
            dMaxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * dHydrogenDensity;
            dMaxCapacityHelium3Mass = partsThatContainLqdHelium3.Sum(p => p.maxAmount) * dLiquidHelium3Density;
            dMaxCapacityHelium4Mass = partsThatContainLqdHelium4.Sum(p => p.maxAmount) * dLiquidHelium4Density;
            dMaxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * dMonoxideDensity;
            dMaxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * dDioxideDensity;
            dMaxCapacityMethaneMass = partsThatContainMethane.Sum(p => p.maxAmount) * dMethaneDensity;
            dMaxCapacityNitrogenMass = partsThatContainNitrogen.Sum(p => p.maxAmount) * dNitrogenDensity;
            dMaxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * dWaterDensity;

            // determine the amount of resources needed for processing (i.e. regolith) that the vessel actually holds
            dAvailableRegolithMass = partsThatContainRegolith.Sum(r => r.amount) * dRegolithDensity;

            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            dSpareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * dHydrogenDensity;
            dSpareRoomHelium3Mass = partsThatContainLqdHelium3.Sum(r => r.maxAmount - r.amount) * dLiquidHelium3Density;
            dSpareRoomHelium4Mass = partsThatContainLqdHelium4.Sum(r => r.maxAmount - r.amount) * dLiquidHelium4Density;
            dSpareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * dMonoxideDensity;
            dSpareRoomDioxideMass = partsThatContainDioxide.Sum(r => r.maxAmount - r.amount) * dDioxideDensity;
            dSpareRoomMethaneMass = partsThatContainMethane.Sum(r => r.maxAmount - r.amount) * dMethaneDensity;
            dSpareRoomNitrogenMass = partsThatContainNitrogen.Sum(r => r.maxAmount - r.amount) * dNitrogenDensity;
            dSpareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * dWaterDensity;

            // this should determine how much resource this process can consume
            var dFixedMaxRegolithConsumptionRate = _current_rate * fixedDeltaTime * dRegolithDensity;
            var dRegolithConsumptionRatio = dFixedMaxRegolithConsumptionRate > 0
                ? Math.Min(dFixedMaxRegolithConsumptionRate, dAvailableRegolithMass) / dFixedMaxRegolithConsumptionRate
                : 0;

            dFixedConsumptionRate = _current_rate * fixedDeltaTime * dRegolithConsumptionRatio;

            // begin the regolith processing
            if (dFixedConsumptionRate > 0 && (dSpareRoomHydrogenMass > 0 || dSpareRoomHelium3Mass > 0 || dSpareRoomHelium4Mass > 0 || dSpareRoomMonoxideMass > 0 || dSpareRoomNitrogenMass > 0)) // check if there is anything to consume and spare room for at least one of the products
            {

                double dFixedMaxHydrogenRate = dFixedConsumptionRate * dHydrogenMassByFraction;
                double dFixedMaxHelium3Rate = dFixedConsumptionRate * dHelium3MassByFraction;
                double dFixedMaxHelium4Rate = dFixedConsumptionRate * dHelium4MassByFraction;
                double dFixedMaxMonoxideRate = dFixedConsumptionRate * dMonoxideMassByFraction;
                double dFixedMaxDioxideRate = dFixedConsumptionRate * dDioxideMassByFraction;
                double dFixedMaxMethaneRate = dFixedConsumptionRate * dMethaneMassByFraction;
                double dFixedMaxNitrogenRate = dFixedConsumptionRate * dNitrogenMassByFraction;
                double dFixedMaxWaterRate = dFixedConsumptionRate * dWaterMassByFraction;

                double dFixedMaxPossibleHydrogenRate = allowOverflow ? dFixedMaxHydrogenRate : Math.Min(dSpareRoomHydrogenMass, dFixedMaxHydrogenRate);
                double dFixedMaxPossibleHelium3Rate = allowOverflow ? dFixedMaxHelium3Rate : Math.Min(dSpareRoomHelium3Mass, dFixedMaxHelium3Rate);
                double dFixedMaxPossibleHelium4Rate = allowOverflow ? dFixedMaxHelium4Rate : Math.Min(dSpareRoomHelium4Mass, dFixedMaxHelium4Rate);
                double dFixedMaxPossibleMonoxideRate = allowOverflow ? dFixedMaxMonoxideRate : Math.Min(dSpareRoomMonoxideMass, dFixedMaxMonoxideRate);
                double dFixedMaxPossibleDioxideRate = allowOverflow ? dFixedMaxDioxideRate : Math.Min(dSpareRoomDioxideMass, dFixedMaxDioxideRate);
                double dFixedMaxPossibleMethaneRate = allowOverflow ? dFixedMaxMethaneRate : Math.Min(dSpareRoomMethaneMass, dFixedMaxMethaneRate);
                double dFixedMaxPossibleNitrogenRate = allowOverflow ? dFixedMaxNitrogenRate : Math.Min(dSpareRoomNitrogenMass, dFixedMaxNitrogenRate);
                double dFixedMaxPossibleWaterRate = allowOverflow ? dFixedMaxWaterRate : Math.Min(dSpareRoomWaterMass, dFixedMaxWaterRate);

                // finds the minimum of these eight numbers (fixedMaxPossibleZZRate / fixedMaxZZRate). Could be more pretty with a custom Min8() function. Seriously. Put in the work, me. This is hideous. But it should be more effective than doing it with array.
                dConsumptionStorageRatio = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(dFixedMaxPossibleHydrogenRate / dFixedMaxHydrogenRate, dFixedMaxPossibleHelium3Rate / dFixedMaxHelium3Rate), dFixedMaxPossibleHelium4Rate / dFixedMaxHelium4Rate), dFixedMaxPossibleMonoxideRate / dFixedMaxMonoxideRate), dFixedMaxPossibleNitrogenRate / dFixedMaxNitrogenRate), dFixedMaxPossibleWaterRate / dFixedMaxWaterRate), dFixedMaxPossibleDioxideRate / dFixedMaxDioxideRate), dFixedMaxPossibleMethaneRate / dFixedMaxMethaneRate);

                // this consumes the resource
                fixed_regolithConsumptionRate = _part.RequestResource(strRegolithResourceName, dConsumptionStorageRatio * dFixedConsumptionRate / dRegolithDensity, ResourceFlowMode.STACK_PRIORITY_SEARCH) / fixedDeltaTime * dRegolithDensity;
                regolithConsumptionRate = fixed_regolithConsumptionRate / fixedDeltaTime;

                // this produces the products
                double dHydrogenRateTemp = fixed_regolithConsumptionRate * dHydrogenMassByFraction;
                double dHelium3RateTemp = fixed_regolithConsumptionRate * dHelium3MassByFraction;
                double dHelium4RateTemp = fixed_regolithConsumptionRate * dHelium4MassByFraction;
                double dMonoxideRateTemp = fixed_regolithConsumptionRate * dMonoxideMassByFraction;
                double dDioxideRateTemp = fixed_regolithConsumptionRate * dDioxideMassByFraction;
                double dMethaneRateTemp = fixed_regolithConsumptionRate * dMethaneMassByFraction;
                double dNitrogenRateTemp = fixed_regolithConsumptionRate * dNitrogenMassByFraction;
                double dWaterRateTemp = fixed_regolithConsumptionRate * dWaterMassByFraction;

                dHydrogenProductionRate = -_part.RequestResource(strHydrogenResourceName, -dHydrogenRateTemp * fixedDeltaTime / dHydrogenDensity) / fixedDeltaTime * dHydrogenDensity;
                dLiquidHelium3ProductionRate = -_part.RequestResource(strLiquidHelium3ResourceName, -dHelium3RateTemp * fixedDeltaTime / dLiquidHelium3Density) / fixedDeltaTime * dLiquidHelium3Density;
                dLiquidHelium4ProductionRate = -_part.RequestResource(strLiquidHelium4ResourceName, -dHelium4RateTemp * fixedDeltaTime / dLiquidHelium4Density) / fixedDeltaTime * dLiquidHelium4Density;
                dMonoxideProductionRate = -_part.RequestResource(strMonoxideResourceName, -dMonoxideRateTemp * fixedDeltaTime / dMonoxideDensity) / fixedDeltaTime * dMonoxideDensity;
                dDioxideProductionRate = -_part.RequestResource(strDioxideResourceName, -dDioxideRateTemp * fixedDeltaTime / dDioxideDensity) / fixedDeltaTime * dDioxideDensity;
                dMethaneProductionRate = -_part.RequestResource(strMethaneResourceName, -dMethaneRateTemp * fixedDeltaTime / dMethaneDensity) / fixedDeltaTime * dMethaneDensity;
                dNitrogenProductionRate = -_part.RequestResource(strNitrogenResourceName, -dNitrogenRateTemp * fixedDeltaTime / dNitrogenDensity) / fixedDeltaTime * dNitrogenDensity;
                dWaterProductionRate = -_part.RequestResource(strWaterResourceName, -dWaterRateTemp * fixedDeltaTime / dWaterDensity) / fixedDeltaTime * dWaterDensity;
            }
            else
            {
                fixed_regolithConsumptionRate = 0;
                dHydrogenProductionRate = 0;
                dLiquidHelium3ProductionRate = 0;
                dLiquidHelium4ProductionRate = 0;
                dMonoxideProductionRate = 0;
                dDioxideProductionRate = 0;
                dMethaneProductionRate = 0;
                dNitrogenProductionRate = 0;
                dWaterProductionRate = 0;
            }
            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Regolith Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((regolithConsumptionRate * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Regolith Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dAvailableRegolithMass.ToString("0.0000") + " mT / " + dMaxCapacityRegolithMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomHydrogenMass.ToString("0.0000") + " mT / " + dMaxCapacityHydrogenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dHydrogenProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-3 Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomHelium3Mass.ToString("0.0000") + " mT / " + dMaxCapacityHelium3Mass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-3 Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dLiquidHelium3ProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-4 Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomHelium4Mass.ToString("0.0000") + " mT / " + dMaxCapacityHelium4Mass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Helium-4 Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dLiquidHelium4ProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Monoxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomMonoxideMass.ToString("0.0000") + " mT / " + dMaxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Monoxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dMonoxideProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Dioxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomDioxideMass.ToString("0.0000") + " mT / " + dMaxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Dioxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dDioxideProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Methane Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomMethaneMass.ToString("0.0000") + " mT / " + dMaxCapacityMethaneMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Methane Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dMethaneProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomNitrogenMass.ToString("0.0000") + " mT / " + dMaxCapacityNitrogenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dNitrogenProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Water Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(dSpareRoomWaterMass.ToString("0.0000") + " mT / " + dMaxCapacityWaterMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Water Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((dWaterProductionRate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (fixed_regolithConsumptionRate > 0)
                _status = "Processing of Regolith Ongoing";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage, try allowing overflow";
        }

    }
}
