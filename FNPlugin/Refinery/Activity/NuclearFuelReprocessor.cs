using System;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using FNPlugin.Reactors;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class NuclearFuelReprocessor : RefineryActivity
    {
        public NuclearFuelReprocessor()
        {
            ActivityName = "Nuclear Fuel Reprocessing";
            PowerRequirements = PluginSettings.Config.BasePowerConsumption;
            EnergyPerTon = 1 / GameConstants.baseReprocessingRate;
        }

        private double _fixedCurrentRate;
        private double _remainingToReprocess;
        private double _remainingSeconds;

        public override string Status => string.Copy(_status);
        public override RefineryType RefineryType => RefineryType.Synthesize;

        public override bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(ResourceSettings.Config.Actinides).Any(rs => rs.amount < rs.maxAmount);
        }

        public override void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;

            var nuclearReactors = _vessel.FindPartModulesImplementing<IFNNuclearFuelReprocessable>();
            double remainingCapacityToReprocess = GameConstants.baseReprocessingRate * fixedDeltaTime / PluginSettings.Config.SecondsInDay * rateMultiplier;
            double enumActinidesChange = 0;

            foreach (var nuclearReactor in nuclearReactors)
            {
                double actinidesChange = nuclearReactor.ReprocessFuel(remainingCapacityToReprocess, fixedDeltaTime, productionModifier, _part);
                enumActinidesChange += actinidesChange;
                remainingCapacityToReprocess = Math.Max(0, remainingCapacityToReprocess - actinidesChange);
            }

            _remainingToReprocess = nuclearReactors.Sum(nfr => nfr.WasteToReprocess);
            _fixedCurrentRate = enumActinidesChange;
            _current_rate = _fixedCurrentRate / fixedDeltaTime;
            _remainingSeconds = _remainingToReprocess / _fixedCurrentRate/ fixedDeltaTime;
            _status = _fixedCurrentRate > 0 ? Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_statu1") : _remainingToReprocess > 0 ? Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_statu2") : Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_statu3");//"Online""Power Deprived""No Fuel To Reprocess"
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));

            if (_remainingSeconds > 0 && !double.IsNaN(_remainingSeconds) && !double.IsInfinity(_remainingSeconds))
            {
                int hours = (int) (_remainingSeconds / 3600);
                int minutes = (int) ((_remainingSeconds - hours*3600)/60);
                int secs = (hours * 60 + minutes) % ((int)(_remainingSeconds / 60));
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_TimeRemaining"), _bold_label, GUILayout.Width(labelWidth));//"Time Remaining"
                GUILayout.Label(hours + " " + Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_hoursLabel") + " " + minutes + " " + Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_minutesLabel") + " " + secs + " " + Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_secondsLabel"), _value_label, GUILayout.Width(valueWidth));//hours""minutes""seconds
            }

            GUILayout.EndHorizontal();
        }

        public double GetActinidesRemovedPerHour()
        {
            return _current_rate * 3600;
        }

        public double GetRemainingAmountToReprocess()
        {
            return _remainingToReprocess;
        }

        public override void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_NuclearFuelReprocessor_Postmsg") + " " + ResourceSettings.Config.Actinides, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
