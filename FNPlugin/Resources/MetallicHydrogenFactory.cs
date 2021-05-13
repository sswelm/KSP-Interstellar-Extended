using System;
using System.Linq;
using FNPlugin.Powermanagement;
using KSP.Localization;

namespace FNPlugin.Resources
{
    class MetallicHydrogenFactory : ResourceSuppliableModule
    {
        public const string Group = "MetallicHydrogenFactory";
        public const string GroupTitle = "MetallicHydrogen Factory";

        [KSPField(isPersistant = true)] public double lastActiveTime;
        [KSPField(isPersistant = true)] public double megajoulesPerSecond;

        [KSPField] public string loopingAnimationName = null;
        [KSPField] public double efficiency = 0.001;
        [KSPField] public double productionRate;
        [KSPField] public string activateTitle = "#LOC_KSPIE_MetallicHydrogenFactory_produce";
        [KSPField] public string outputResourceName = "MtlHydrogen";
        [KSPField] public string inputResourceName = "LqdHydrogen";
        [KSPField] public double powerRequirementMultiplier = 1;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, isPersistant = true),
         UI_Toggle(disabledText = "#LOC_KSPIE_MetallicHydrogenFactory_Off", enabledText = "#LOC_KSPIE_MetallicHydrogenFactory_On")]//OffOn
        public bool isActive = false;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, isPersistant = true, guiName = "#LOC_KSPIE_MetallicHydrogenFactory_powerPecentage"),
         UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 1)]
        public float powerPercentage = 100;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MetallicHydrogenFactory_productionRate")]
        public string productionRateTxt;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = true,
            guiName = "#LOC_KSPIE_MetallicHydrogenFactory_MaximumPowercapacity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Maximum Power capacity
        public double powerCapacity = 1000;

        private PartResourceDefinition _liquidHydrogenDefinition;
        private PartResourceDefinition _metallicHydrogenDefinition;
        private ModuleAnimateGeneric _loopingAnimation;
        private string _disabledText;

        public override void OnStart(StartState state)
        {
            _liquidHydrogenDefinition = PartResourceLibrary.Instance.GetDefinition(inputResourceName);
            _metallicHydrogenDefinition = PartResourceLibrary.Instance.GetDefinition(outputResourceName);

            if (state == StartState.Editor)
                return;

            if (!string.IsNullOrEmpty(loopingAnimationName))
                _loopingAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault(m => m.animationName == loopingAnimationName);

            _disabledText = Localizer.Format("#LOC_KSPIE_MetallicHydrogenFactory_disabled");

            Fields[nameof(isActive)].guiName = Localizer.Format(activateTitle);

            if (!isActive)
                return;

            var deltaTime = Planetarium.GetUniversalTime() - lastActiveTime;

            Produce(megajoulesPerSecond, deltaTime);
        }

        public override void OnUpdate()
        {
            if (!isActive)
            {
                productionRateTxt = _disabledText;
                return;
            }

            lastActiveTime = Planetarium.GetUniversalTime();

            double ratePerDay = productionRate * PluginSettings.Config.SecondsInDay;

            if (ratePerDay > 0.1)
            {
                if (ratePerDay > 1000)
                    productionRateTxt = (ratePerDay / 1e3).ToString("0.0000") + " mt/" + Localizer.Format("#LOC_KSPIE_MetallicHydrogenFactory_perday");//day
                else
                    productionRateTxt = (ratePerDay).ToString("0.0000") + " kg/" + Localizer.Format("#LOC_KSPIE_MetallicHydrogenFactory_perday");//day
            }
            else
            {
                if (ratePerDay > 0.1e-3)
                    productionRateTxt = (ratePerDay * 1e3).ToString("0.0000") + " g/" + Localizer.Format("#LOC_KSPIE_MetallicHydrogenFactory_perday");//day
                else
                    productionRateTxt = (ratePerDay * 1e6).ToString("0.0000") + " mg/" + Localizer.Format("#LOC_KSPIE_MetallicHydrogenFactory_perday");//day
            }
        }

        public void FixedUpdate()
        {
            if (!isActive)
                return;

            if (_loopingAnimation != null && !_loopingAnimation.IsMoving())
                _loopingAnimation.Toggle();

            // first check available hydrogen
            part.GetConnectedResourceTotals(_liquidHydrogenDefinition.id, out double amount, out double maxAmount);
            if (amount <= 0)
                return;

            var availablePower = GetAvailableStableSupply(ResourceSettings.Config.ElectricPowerInMegawatt);
            var resourceBarRatio = GetResourceBarRatio(ResourceSettings.Config.ElectricPowerInMegawatt);

            var effectiveResourceThrottling = resourceBarRatio > 1d / 3d ? 1 : resourceBarRatio * 3;

            var requestedPower = powerPercentage * 0.01 * Math.Min(powerCapacity * powerRequirementMultiplier, effectiveResourceThrottling * availablePower);

            var receivedPower = CheatOptions.InfiniteElectricity
                ? requestedPower
                : ConsumeFnResourcePerSecond(requestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);

            megajoulesPerSecond = receivedPower * efficiency;

            // produce wasteheat
            SupplyFnResourcePerSecond(receivedPower * (1 - efficiency), ResourceSettings.Config.WasteHeatInMegawatt);

            Produce(megajoulesPerSecond, TimeWarp.fixedDeltaTime);
        }

        private void Produce(double effectivePowerMegajoulesPerSecond, double fixedDeltaTime)
        {
            var unitsInTon = effectivePowerMegajoulesPerSecond / 1000 / 216 / powerRequirementMultiplier;

            var inputResourceInLiter = unitsInTon / _liquidHydrogenDefinition.density * fixedDeltaTime;

            var consumedResourceInLiters = part.RequestResource(_liquidHydrogenDefinition.id, inputResourceInLiter, ResourceFlowMode.STAGE_PRIORITY_FLOW);

            var consumedMass = consumedResourceInLiters * _liquidHydrogenDefinition.density;

            var outputResourceInLiter = consumedMass / _metallicHydrogenDefinition.density;

            var currentRate = -part.RequestResource(_metallicHydrogenDefinition.id, -outputResourceInLiter, ResourceFlowMode.STAGE_PRIORITY_FLOW);

            productionRate = 1000 * currentRate * _metallicHydrogenDefinition.density / fixedDeltaTime;
        }
    }
}
