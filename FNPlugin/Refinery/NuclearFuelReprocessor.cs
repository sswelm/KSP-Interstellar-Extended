using FNPlugin.Constants;
using FNPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace FNPlugin.Refinery
{
    class NuclearFuelReprocessor : RefineryActivityBase, IRefineryActivity
    {
        double _fixed_current_rate = 0;
        double _remaining_to_reprocess = 0;
        double _remaining_seconds = 0;
        
        public RefineryType RefineryType { get { return RefineryType.synthesize; } }

        public String ActivityName { get { return "Nuclear Fuel Reprocessing"; } }

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Actinides).Any(rs => rs.amount < rs.maxAmount);
        }

        public double PowerRequirements { get { return PluginHelper.BasePowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public void Initialize(Part part)
        {
            this._part = part;
            _vessel = part.vessel;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false) 
        {
            _current_power = PowerRequirements * rateMultiplier;
            List<INuclearFuelReprocessable> nuclear_reactors = _vessel.FindPartModulesImplementing<INuclearFuelReprocessable>();
            double remaining_capacity_to_reprocess = GameConstants.baseReprocessingRate * fixedDeltaTime / PluginHelper.SecondsInDay * rateMultiplier;
            double enum_actinides_change = 0;
            foreach (INuclearFuelReprocessable nuclear_reactor in nuclear_reactors)
            {
                double actinides_change = nuclear_reactor.ReprocessFuel(remaining_capacity_to_reprocess);
                enum_actinides_change += actinides_change;
                remaining_capacity_to_reprocess = Math.Max(0, remaining_capacity_to_reprocess - actinides_change);
            }
            _remaining_to_reprocess = nuclear_reactors.Sum(nfr => nfr.WasteToReprocess);
            _fixed_current_rate = enum_actinides_change;
            _current_rate = _fixed_current_rate / fixedDeltaTime;
            _remaining_seconds = _remaining_to_reprocess / _fixed_current_rate/ fixedDeltaTime;
            _status = _fixed_current_rate > 0 ? Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_statu1") : _remaining_to_reprocess > 0 ? Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_statu2") : Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_statu3");//"Online""Power Deprived""No Fuel To Reprocess"
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            if (_remaining_seconds > 0 && !double.IsNaN(_remaining_seconds) && !double.IsInfinity(_remaining_seconds))
            {
                int hrs = (int) (_remaining_seconds / 3600);
                int mins = (int) ((_remaining_seconds - hrs*3600)/60);
                int secs = (hrs * 60 + mins) % ((int)(_remaining_seconds / 60));
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_TimeRemaining"), _bold_label, GUILayout.Width(labelWidth));//"Time Remaining"
                GUILayout.Label(hrs + " " + Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_hoursLabel") + " " + mins + " " + Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_minutesLabel") + " " + secs + " " + Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_secondsLabel"), _value_label, GUILayout.Width(valueWidth));//hours""minutes""seconds
            }
            GUILayout.EndHorizontal();
        }

        public double getActinidesRemovedPerHour() 
        {
            return _current_rate * 3600.0;
        }

        public double getRemainingAmountToReprocess() 
        {
            return _remaining_to_reprocess;
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_Postmsg") + " " + InterstellarResourcesConfiguration.Instance.Actinides, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
