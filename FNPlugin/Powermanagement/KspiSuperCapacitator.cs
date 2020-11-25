using FNPlugin.Extensions;
using FNPlugin.Resources;
using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    [KSPModule("Air Lithium Battery")]
    class AirLithiumBattery : KspiSuperCapacitator { }

    [KSPModule("Super Lithium Battery")]
    class SuperLithiumBattery : KspiSuperCapacitator { }

    [KSPModule("Super Capacitator")]
    class KspiSuperCapacitator : ResourceSuppliableModule
    {
        [KSPField(groupName = FNBatteryGenerator.GROUP, groupDisplayName = FNBatteryGenerator.GROUP_TITLE, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_KspiSuperCapacitator_MaxCapacity", guiUnits = " MJe")]//Max Capacity
        public float maxStorageCapacityMJ = 0;
        [KSPField(groupName = FNBatteryGenerator.GROUP, groupDisplayName = FNBatteryGenerator.GROUP_TITLE, isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_KspiSuperCapacitator_Mass", guiUnits = " t")]//Mass
        public float partMass = 0;

        [KSPField(groupName = FNBatteryGenerator.GROUP, guiActiveEditor = false)]
        public string powerResourceName = "Megajoules";
        [KSPField(groupName = FNBatteryGenerator.GROUP, guiActiveEditor = false)]
        public string electricChargeResourceName = ResourceSettings.ElectricCharge;

        [KSPField]
        public double powerConversionRate = 1000;
        [KSPField]
        public double megajoulesAfterLoad = 0;

        public override void OnStart(StartState state)
        {
            string[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
            this.resources_to_supply = resources_to_supply;

            part.force_activate();
        }

        public override void OnSave(ConfigNode node)
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            var powerResource = part.Resources[powerResourceName];

            if (powerResource == null)
                return;

            var electricCharge = part.Resources[electricChargeResourceName];

            if (electricCharge == null)
                return;

            if (megajoulesAfterLoad > 0)
            {
                Debug.Log("[KSPI]: KspiSuperCapacitator OnSave update " + powerResourceName + " amount to " + megajoulesAfterLoad);

                if (megajoulesAfterLoad.IsInfinityOrNaN().IsFalse())
                    powerResource.amount = megajoulesAfterLoad;
                megajoulesAfterLoad = 0;
            }

            var availableEmptyElectricCharge = Math.Max(0, electricCharge.maxAmount - electricCharge.amount);

            if (powerResource.amount > 0)
            {
                var newElectricChargeMaxAmount = electricCharge.maxAmount + (powerResource.maxAmount * powerConversionRate);
                var newElectricChargeAmount = electricCharge.amount + (powerResource.amount * powerConversionRate);

                if (newElectricChargeMaxAmount.IsInfinityOrNaN().IsFalse())
                {
                    electricCharge.maxAmount = newElectricChargeMaxAmount;
                    electricCharge.amount = newElectricChargeAmount;
                }

                var megaJouleDecreaseMax = powerConversionRate == 0 ? 0 : availableEmptyElectricCharge / powerConversionRate;
                var megeJouleDecrease = Math.Min(powerResource.amount, megaJouleDecreaseMax);

                var newPowerAmount = Math.Max(0, powerResource.amount - megeJouleDecrease);

                if (newPowerAmount.IsInfinityOrNaN().IsFalse())
                    powerResource.amount = newPowerAmount;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            var powerResource = part.Resources[powerResourceName];

            if (powerResource == null)
                return;

            var electricCharge = part.Resources[electricChargeResourceName];

            if (electricCharge == null)
                return;

            if (part.protoPartSnapshot == null)
                return;

            // detect amount of power used offline and adjust megajoules
            var protoElectricCharge = part.protoPartSnapshot.resources.FirstOrDefault(m => m.resourceName == electricChargeResourceName);

            if (protoElectricCharge == null)
                return;

            var protoPowerResource = part.protoPartSnapshot.resources.FirstOrDefault(m => m.resourceName == powerResourceName);

            if (protoPowerResource != null)
            {
                powerResource.maxAmount = protoPowerResource.maxAmount;
                powerResource.amount = protoPowerResource.amount;
            }

            var deltaElectricChargeMaxAmount = protoElectricCharge.maxAmount - electricCharge.maxAmount;

            megajoulesAfterLoad = deltaElectricChargeMaxAmount == 0 ? 0 : Math.Max(0, (protoElectricCharge.amount - electricCharge.maxAmount) / deltaElectricChargeMaxAmount) * powerResource.maxAmount;
        }

        public void Update()
        {
            partMass = part.mass;
        }
    }
}
