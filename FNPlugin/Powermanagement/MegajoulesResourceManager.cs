using FNPlugin.Constants;
using FNPlugin.Extensions;
using KSP.Localization;
using System;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    internal class MegajoulesResourceManager : ResourceManager
    {
        private readonly PartResourceDefinition electricResourceDefinition;

        private double lastECNeeded;
        // MJ requested for conversion into EC
        private double lastMJConverted;
        private double mjConverted;

        protected override double AuxiliaryResourceDemand
        {
            get
            {
                return lastMJConverted;
            }
        }

        public override double CurrentSurplus
        {
            get
            {
                return Math.Max(0.0, base.CurrentSurplus - lastMJConverted);
            }
        }

        public MegajoulesResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, FNRESOURCE_MEGAJOULES, FNRESOURCE_FLOWTYPE_SMALLEST_FIRST)
        {
            WindowPosition = new Rect(50, 50, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
            electricResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.ElectricChargePower);
            lastECNeeded = 0.0;
            lastMJConverted = 0.0;
            mjConverted = 0.0;
        }

        protected override void DoWindowFinal()
        {
            if (lastMJConverted > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_DCElectricalSystem"), left_aligned_label, GUILayout.ExpandWidth(true));//"DC Electrical System"
                GUILayout.Label(PluginHelper.getFormattedPowerString(lastMJConverted), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(VALUE_WIDTH));
                GUILayout.Label("0", right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(PRIORITY_WIDTH));
                GUILayout.EndHorizontal();
            }
        }

        protected override void DoWindowInitial()
        {
            if (ResourceSupply >= ResourceDemand * 0.5)
            {
                double storedSupplyRatio = ResourceSupply != 0.0 ? Math.Min(1.0, Math.Max(0.0, TotalPowerSupplied / ResourceSupply)) : 0.0;
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_ResourceManager_CurrentDistribution"), left_bold_label, GUILayout.ExpandWidth(true));//"Current Distribution"
                GUILayout.Label(storedSupplyRatio.ToString("P2"), right_aligned_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(OVERVIEW_WIDTH));
                GUILayout.EndHorizontal();
            }
        }

        private void SupplyEC(double timeWarpDT, double ecToSupply)
        {
            ecToSupply = Math.Min(ecToSupply, current.Supply * GameConstants.ecPerMJ * timeWarpDT);
            double powerConverted = part.RequestResource(electricResourceDefinition.id, -ecToSupply) / -GameConstants.ecPerMJ / timeWarpDT;
            current.Supply -= powerConverted;
            current.StableSupply -= powerConverted;
            current.TotalSupplied += powerConverted;
            current.Demand += ecToSupply / GameConstants.ecPerMJ / timeWarpDT;
            mjConverted += powerConverted;
        }

        protected override void SupplyPriority(double timeWarpDT, int priority)
        {
            part.GetConnectedResourceTotals(electricResourceDefinition.id, out double amount, out double maxAmount);
            double ecNeeded = maxAmount - amount;
            if (amount.IsInfinityOrNaN() || ecNeeded <= 0.0)
                return;

            if (priority == 0)
            {
                lastMJConverted = mjConverted;
                mjConverted = 0.0;
                if (last.StableSupply > 0.0)
                {
                    // Supply up to 1 EC/s (trickle charge)
                    SupplyEC(timeWarpDT, timeWarpDT);
                }
                else
                {
                    // Add a demand for the difference in EC only
                    double demand = (ecNeeded - lastECNeeded) / GameConstants.ecPerMJ / timeWarpDT;
                    current.Demand += demand;
                    mjConverted += demand;
                }
                lastECNeeded = ecNeeded;
            }
            else if (priority == 1 && last.StableSupply > 0.0)
            {
                // Supply up to the full missing EC amount
                SupplyEC(timeWarpDT, ecNeeded);
            }
        }
    }
}
