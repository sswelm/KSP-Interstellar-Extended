using FNPlugin.Constants;
using UnityEngine;

namespace FNPlugin.Refinery
{
    public enum RefineryType { None = 0, Heating = 1, Cryogenics = 2, Electrolysis = 4, Synthesize = 8 }

    abstract class RefineryActivity: PartModule, IRefineryActivity
    {
        [KSPField(guiActiveEditor = true, guiName = "Size Multiplier")]
        public double sizeModifier = 1;

        public static int labelWidth = 180;
        public static int valueWidth = 180;

        protected Part _part;
        protected Vessel _vessel;
        protected GUIStyle _bold_label;
        protected GUIStyle _value_label;
        protected GUIStyle _value_label_green;
        protected GUIStyle _value_label_red;
        protected GUIStyle _value_label_number;

        protected string _status = "";
        protected bool _allowOverflow;
        protected double _current_power;
        protected double _current_rate;
        protected double _effectiveMaxPower;

        private BaseEvent _toggleEvent;

        private InterstellarRefineryController _refineryController;

        public double CurrentPower => _current_power;

        public string ActivityName { get; protected set; }
        public string Formula { get; protected set; }

        public double PowerRequirements { get; protected set; }
        public double EnergyPerTon { get; protected set; }

        public virtual RefineryType RefineryType { get; } = RefineryType.None;
        public virtual string Status { get; } = "";

        [KSPEvent(groupDisplayName = InterstellarRefineryController.GroupTitle, groupName = InterstellarRefineryController.Group, guiActive = false, guiName = "Toggle", active = true)]//Toggle RefineryActivity
        public void ToggleWindow()
        {
            if (_refineryController == null)
                return;

            _refineryController.ToggleRefinery(this);
        }

        public abstract void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier,
            bool allowOverflow, double fixedDeltaTime, bool isStartup = false);

        public abstract bool HasActivityRequirements();

        public abstract void PrintMissingResources();

        public virtual void Initialize(Part localPart, InterstellarRefineryController controller)
        {
            _part = localPart;
            _vessel = localPart.vessel;
            _refineryController = controller;

            if (Events != null)
            {
                _toggleEvent = Events[nameof(ToggleWindow)];
                _toggleEvent.guiActive = true;
            }
        }

        public override void OnUpdate()
        {
            if (_toggleEvent == null || _refineryController == null)
                return;

            var isActive = _refineryController.IsActive(this);

            _toggleEvent.guiName = (isActive ? "Stop " : "Start ") + ActivityName;
        }

        public virtual void UpdateGUI()
        {
            if (_bold_label == null)
                _bold_label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };
            if (_value_label == null)
                _value_label = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };
            if (_value_label_green == null)
                _value_label_green = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, normal = {textColor = Color.green}};
            if (_value_label_red == null)
                _value_label_red = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, normal = {textColor = Color.red}};
            if (_value_label_number == null)
                _value_label_number = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont, alignment = TextAnchor.MiddleRight };
        }

        public override string ToString()
        {
            return ActivityName;
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();
            sb.AppendLine(ActivityName);

            if (!string.IsNullOrEmpty(Formula))
                sb.AppendLine(Formula);

            double capacity = sizeModifier * PowerRequirements;
            if (capacity > 0)
            {
                sb.Append("Power: ").AppendLine(PluginHelper.GetFormattedPowerString(capacity));

                if (EnergyPerTon > 0.0)
                {
                    sb.Append("Energy: ").Append(PluginHelper.GetFormattedPowerString(EnergyPerTon)).AppendLine("/t");
                    sb.Append("Energy: ").Append((1.0 / EnergyPerTon).ToString("F3")).AppendLine(" t/MW");

                    double production = capacity / EnergyPerTon;
                    sb.Append("Production: ").Append(production.ToString("F3")).AppendLine(" t/sec");
                    sb.Append("Production: ").Append((production * 60.0).ToString("F1")).AppendLine(" t/min");
                    sb.Append("Production: ").Append((production * GameConstants.SECONDS_IN_HOUR).ToString("F0")).AppendLine(" t/hr");
                }
            }

            return sb.ToStringAndRelease();
        }
    }
}
