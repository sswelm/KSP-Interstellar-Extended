using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class MegajoulesResourceManager : ResourceManager
    {
        private readonly PartResourceDefinition electricResourceDefinition;

        private double _lastEcNeeded;
        // MJ requested for conversion into EC
        private double _lastMjConverted;
        private double _mjConverted;

        private double auxiliaryElectricChargeRate;

        public void AuxiliaryResourceSupplied(double rate)
        {
            auxiliaryElectricChargeRate = rate;
            _mjConverted = 0;
        }

        public double MjConverted => _mjConverted;

        protected override double AuxiliaryResourceDemand => _lastMjConverted + auxiliaryElectricChargeRate;

        public override double CurrentSurplus => Math.Max(0.0, base.CurrentSurplus - _lastMjConverted);

        public MegajoulesResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ElectricPowerInMegawatt, FnResourceFlowTypeSmallestFirst)
        {
            WindowPosition = new Rect(50, 50, LabelWidth + ValueWidth + PriorityWidth, 50);
            electricResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.ElectricPowerInKilowatt);
            _lastEcNeeded = 0.0;
            _lastMjConverted = 0.0;
            _mjConverted = 0.0;
        }

        protected override void DoWindowFinal()
        {
            var providedAuxiliaryPower = _lastMjConverted + auxiliaryElectricChargeRate;

            if (providedAuxiliaryPower > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_DCElectricalSystem"), leftAlignedLabel, GUILayout.ExpandWidth(true));//"DC Electrical System"
                GUILayout.Label(PluginHelper.GetFormattedPowerString(providedAuxiliaryPower), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));
                GUILayout.Label("0", rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(PriorityWidth));
                GUILayout.EndHorizontal();
            }
        }

        protected override void DoWindowInitial()
        {
            if (ResourceSupply >= ResourceDemand * 0.5)
            {
                double storedSupplyRatio = ResourceSupply != 0.0 ? Math.Min(1.0, Math.Max(0.0, TotalPowerSupplied / ResourceSupply)) : 0.0;
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_CurrentDistribution"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Current Distribution"
                GUILayout.Label(storedSupplyRatio.ToString("P2"), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OverviewWidth));
                GUILayout.EndHorizontal();
            }
        }

        private void SupplyEC(double timeWarpDT, double ecToSupply)
        {
            ecToSupply = Math.Min(ecToSupply, current.Supply * GameConstants.ecPerMJ * timeWarpDT);

            double powerConverted;

            if (Kerbalism.IsLoaded)
                powerConverted = ecToSupply / GameConstants.ecPerMJ / timeWarpDT;
            else
                powerConverted = part.RequestResource(electricResourceDefinition.id, -ecToSupply) / -GameConstants.ecPerMJ / timeWarpDT;

            current.Supply -= powerConverted;
            current.StableSupply -= powerConverted;
            current.TotalSupplied += powerConverted;
            current.Demand += ecToSupply / GameConstants.ecPerMJ / timeWarpDT;
            _mjConverted += powerConverted;
        }

        protected override void SupplyPriority(double timeWarpDT, int priority)
        {
            part.GetConnectedResourceTotals(electricResourceDefinition.id, out double amount, out double maxAmount);

            double ecNeeded = Kerbalism.IsLoaded ? 0 : maxAmount - amount;
            double ratio = maxAmount > 0 ? ecNeeded / maxAmount : 0;
            if (amount.IsInfinityOrNaN() || ecNeeded <= 0.0)
            {
                if (priority == 0)
                    current.Demand += auxiliaryElectricChargeRate;
                return;
            }

            if (priority == 0)
            {
                _lastMjConverted = _mjConverted;
                _mjConverted = 0.0;
                if (last.StableSupply > 0.0)
                {
                    // Supply up to 1 EC/s (trickle charge)
                    SupplyEC(timeWarpDT, timeWarpDT * ratio);
                }
                else
                {
                    // Add a demand for the difference in EC only
                    double demand = (ecNeeded - _lastEcNeeded) / GameConstants.ecPerMJ / timeWarpDT;
                    current.Demand += demand;
                    _mjConverted += demand;
                }
                _lastEcNeeded = ecNeeded;
            }
            else if (priority == 1 && last.StableSupply > 0.0)
            {
                // Supply up to the full missing EC amount

                SupplyEC(timeWarpDT, ecNeeded);
            }

            if (priority == 0)
                current.Demand += auxiliaryElectricChargeRate;
        }
    }
}
