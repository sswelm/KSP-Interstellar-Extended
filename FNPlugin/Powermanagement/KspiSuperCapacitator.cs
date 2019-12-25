using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Reactors;
using FNPlugin.Redist;
using FNPlugin.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TweakScale;
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
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Max Capacity", guiUnits = " MWe")]
        public float maxStorageCapacityMJ = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField(guiActiveEditor = false)]
        public string powerResourceName = "Megajoules";
        [KSPField(guiActiveEditor = false)]
        public double powerConversionRate = 1000;

        private double megajoulesAfterLoad = 0;

        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES };
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

            var electricCharge = part.Resources["ElectricCharge"];

            if (electricCharge == null)
                return;

            if (megajoulesAfterLoad > 0)
            {
                Debug.Log("[KSPI]: KspiSuperCapacitator OnSave update " + powerResourceName + " amount to " + megajoulesAfterLoad);
                powerResource.amount = megajoulesAfterLoad;
                megajoulesAfterLoad = 0;
            }

            var availableEmptyElectricCharge = Math.Max(0, electricCharge.maxAmount - electricCharge.amount);

            if (powerResource.amount > 0)
            {
                Debug.Log("[KSPI]: KspiSuperCapacitator OnSave powerResource.amount = " + powerResource.amount);

                var newElectricChargeMaxAmount = electricCharge.maxAmount + (powerResource.maxAmount * powerConversionRate);
                var newElectricChargeAmount = electricCharge.amount + (powerResource.amount * powerConversionRate);

                electricCharge.maxAmount = newElectricChargeMaxAmount;
                electricCharge.amount = newElectricChargeAmount;

                Debug.Log("[KSPI]: KspiSuperCapacitator OnSave newElectricChargeAmount = " + newElectricChargeAmount);

                var megeJouleDecrease = Math.Min(powerResource.amount, availableEmptyElectricCharge / powerConversionRate);

                Debug.Log("[KSPI]: KspiSuperCapacitator OnSave decreased " + powerResourceName + " by " + megeJouleDecrease);

                powerResource.amount = Math.Max(0, powerResource.amount - megeJouleDecrease);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("[KSPI]: KspiSuperCapacitator OnLoad start");

            if (HighLogic.LoadedSceneIsEditor)
                return;

            var powerResource = part.Resources[powerResourceName];

            if (powerResource == null)
                return;

            var electricCharge = part.Resources["ElectricCharge"];

            if (electricCharge == null)
                return;

            if (part.protoPartSnapshot == null)
            {
                Debug.Log("[KSPI]: KspiSuperCapacitator OnLoad part.protoPartSnapshot == null");
                return;
            }

            // detect amount of power used offline and adjust megajoules
            var protoElectricCharge = part.protoPartSnapshot.resources.FirstOrDefault(m => m.resourceName == "ElectricCharge");

            if (protoElectricCharge == null)
            {
                Debug.Log("[KSPI]: KspiSuperCapacitator OnLoad protoElectricCharge == null");
                return;
            }

            Debug.Log("[KSPI]: KspiSuperCapacitator OnLoad protoElectricCharge " + protoElectricCharge.amount);

            var protoPowerResource = part.protoPartSnapshot.resources.FirstOrDefault(m => m.resourceName == powerResourceName);

            if (protoPowerResource != null)
            {
                Debug.Log("[KSPI]: KspiSuperCapacitator OnLoad protoPowerResource maxAmount " + protoPowerResource.maxAmount);
                Debug.Log("[KSPI]: KspiSuperCapacitator OnLoad protoPowerResource amount " + protoPowerResource.amount);

                powerResource.maxAmount = protoPowerResource.maxAmount;
                powerResource.amount = protoPowerResource.amount;
            }

            megajoulesAfterLoad = Math.Max(0, (protoElectricCharge.amount - electricCharge.maxAmount) / (protoElectricCharge.maxAmount - electricCharge.maxAmount)) * powerResource.maxAmount;

            Debug.Log("[KSPI]: KspiSuperCapacitator OnLoad megajoulesAfterLoad " + megajoulesAfterLoad);
        }
    }
}
