using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
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

        private readonly Queue<double> ecOutput = new Queue<double>();

        public double MjConverted => _mjConverted;

        protected override double AuxiliaryResourceDemand => _lastMjConverted;

        public override double CurrentSurplus => Math.Max(0.0, base.CurrentSurplus - _lastMjConverted);

        public MegajoulesResourceManager(Guid overmanagerId, ResourceSuppliableModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ElectricPowerInMegawatt, FnResourceFlowTypeSmallestFirst)
        {
            SetWindowPosition(pm.epx, pm.epy, 50, 50);
            electricResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.ElectricPowerInKilowatt);
            _lastEcNeeded = 0.0;
            _lastMjConverted = 0.0;
            _mjConverted = 0.0;
        }

        protected override void DoWindowFinal()
        {
            PartModule.epx = (int)WindowPosition.x;
            PartModule.epy = (int)WindowPosition.y;

            var providedAuxiliaryPower = _lastMjConverted;

            ecOutput.Enqueue(providedAuxiliaryPower);
            if (ecOutput.Count > 10)
                ecOutput.Dequeue();

            var maxEcOutput = ecOutput.Max();

            if (!(maxEcOutput > 0.0005)) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_DCElectricalSystem"), leftAlignedLabel, GUILayout.ExpandWidth(true));//"DC Electrical System"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(maxEcOutput), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(ValueWidth));
            GUILayout.Label("0", rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(PriorityWidth));
            GUILayout.EndHorizontal();
        }

        protected override void DoWindowInitial()
        {
            double storedSupplyRatio = ResourceSupply != 0.0 ? Math.Min(1.0, Math.Max(0.0, TotalPowerSupplied / ResourceSupply)) : 0.0;
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_CurrentDistribution"), leftBoldLabel, GUILayout.ExpandWidth(true));//"Current Distribution"
            GUILayout.Label(storedSupplyRatio.ToString("P2"), rightAlignedLabel, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OverviewWidth));
            GUILayout.EndHorizontal();
        }

        private void SupplyEc(double timeWarpDt, double ecToSupply)
        {
            ecToSupply = Math.Min(ecToSupply, current.Supply * GameConstants.ecPerMJ * timeWarpDt);

            double powerConverted = part.RequestResource(electricResourceDefinition.id, -ecToSupply) / -GameConstants.ecPerMJ / timeWarpDt;

            current.Supply -= powerConverted;
            current.StableSupply -= powerConverted;
            current.TotalSupplied += powerConverted;
            current.Demand += ecToSupply / GameConstants.ecPerMJ / timeWarpDt;
            _mjConverted += powerConverted;
        }

        protected override void SupplyPriority(double timeWarpDt, int priority)
        {
            part.GetConnectedResourceTotals(electricResourceDefinition.id, out double amount, out double maxAmount);

            var minimumEc = Math.Max(0, maxAmount - amount - maxAmount / 2);
            double ecNeeded = Kerbalism.IsLoaded ? minimumEc : maxAmount - amount;

            double neededRatio = maxAmount > 0 ? ecNeeded / maxAmount : 0;
            if (amount.IsInfinityOrNaN() || ecNeeded <= 0.0)
            {
                return;
            }

            if (priority == 0)
            {
                _lastMjConverted = _mjConverted;
                _mjConverted = 0.0;
                if (last.StableSupply > 0.0)
                {
                    // Supply up to 1 EC/s (trickle charge)
                    SupplyEc(timeWarpDt, timeWarpDt * neededRatio);
                }
                else
                {
                    // Add a demand for the difference in EC only
                    double demand = (ecNeeded - _lastEcNeeded) / GameConstants.ecPerMJ / timeWarpDt;
                    current.Demand += demand;
                    _mjConverted += demand;
                }
                _lastEcNeeded = ecNeeded;
            }
            else if (priority == 1 && last.StableSupply > 0.0)
            {
                // Supply up to the full missing EC amount

                SupplyEc(timeWarpDt, ecNeeded);
            }
        }
    }
}
