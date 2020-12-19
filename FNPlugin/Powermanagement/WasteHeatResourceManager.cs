using FNPlugin.Constants;
using System;
using FNPlugin.Resources;
using FNPlugin.Wasteheat;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class WasteHeatResourceManager : ResourceManager
    {
        private const double _maximumRadiatorTempInSpace = 4500;
        private const double maximumRadiatorTempAtOneAtmosphere = 1200;
        private const double PASSIVE_TEMP_P4 = 2947.295521;

        private double _maxSpaceTempBonus = RadiatorProperties.RadiatorTemperatureMk6 - maximumRadiatorTempAtOneAtmosphere;

        public double TemperatureRatio { get; private set; }

        public double RadiatorEfficiency { get; private set; }

        public double AtmosphericMultiplier { get; private set; }

        public double MaxCurrentRadiatorTemperature { get; private set; }

        public WasteHeatResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, ResourceSettings.Config.WasteHeatInMegawatt, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(600, 600, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
            TemperatureRatio = 0.0;
            RadiatorEfficiency = 0.0;
        }

        protected override double AdjustSupplyComplete(double timeWarpDT, double powerToExtract)
        {
            if (Vessel.altitude <= PluginHelper.GetMaxAtmosphericAltitude(Vessel.mainBody))
            {
                // passive convection - a lot of this
                double pressure = FlightGlobals.getStaticPressure(Vessel.transform.position) / GameConstants.EarthAtmospherePressureAtSeaLevel;
                powerToExtract += 40.0e-6 * GameConstants.rad_const_h * pressure * Vessel.totalMass * timeWarpDT;
            }
            else
            {
                //powerToExtract += 2.0 * PASSIVE_TEMP_P4 * GameConstants.stefan_const * Vessel.totalMass * timeWarpDT;
            }
            return powerToExtract;
        }

        public override void update(long counter)
        {
            base.update(counter);

            //UpdateMaxCurrentTemperature();
            AtmosphericMultiplier = Vessel.atmDensity > 0 ? Math.Sqrt(Vessel.atmDensity) : 0;
            TemperatureRatio = Math.Pow(ResourceFillFraction, 0.75);
            RadiatorEfficiency = 1.0 - Math.Pow(1.0 - ResourceFillFraction, 400.0);
        }

        //private void UpdateMaxCurrentTemperature()
        //{
        //    if (Vessel.mainBody.atmosphereContainsOxygen && Vessel.staticPressurekPa > 0)
        //    {
        //        var combinedPressure = Vessel.staticPressurekPa + Vessel.dynamicPressurekPa * 0.2;
        //        double oxidationModifier;

        //        if (combinedPressure > GameConstants.EarthAtmospherePressureAtSeaLevel)
        //        {
        //            var extraPressure = combinedPressure - GameConstants.EarthAtmospherePressureAtSeaLevel;
        //            var ratio = extraPressure / GameConstants.EarthAtmospherePressureAtSeaLevel;
        //            if (ratio <= 1)
        //                ratio *= ratio;
        //            else
        //                ratio = Math.Sqrt(ratio);
        //            oxidationModifier = 1 + ratio * 0.1;
        //        }
        //        else
        //            oxidationModifier = Math.Pow(combinedPressure / GameConstants.EarthAtmospherePressureAtSeaLevel, 0.25);

        //        double spaceRadiatorModifier = Math.Max(0.25, Math.Min(0.95, 0.95 + Vessel.verticalSpeed * 0.002));

        //        double spaceRadiatorBonus = (1 / spaceRadiatorModifier) * _maxSpaceTempBonus * (1 - oxidationModifier);

        //        MaxCurrentRadiatorTemperature = Math.Min(_maximumRadiatorTempInSpace, Math.Max(PhysicsGlobals.SpaceTemperature, maximumRadiatorTempAtOneAtmosphere + spaceRadiatorBonus));
        //    }
        //    else
        //    {
        //        MaxCurrentRadiatorTemperature = _maximumRadiatorTempInSpace;
        //    }
        //}
    }
}
