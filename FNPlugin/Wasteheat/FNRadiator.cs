using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
using FNPlugin.Propulsion;
using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Wasteheat
{
    [KSPModule("Radiator")]
    class IntegratedRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class StackFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class FlatFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class ActiveRadiator3 : ResourceSuppliableModule, IPartMassModifier, IPartCostModifier
    {
        public const string GROUP = "ActiveRadiator3";
        public const string GROUP_TITLE = "Interstellar Active Cooler";

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Power Priority", guiFormat = "F0", guiUnits = ""), UI_FloatRange(stepIncrement = 1.0F, maxValue = 5F, minValue = 0F)]
        public float powerPriority = 5;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiName = "Surface Area", guiFormat = "F0"), UI_FloatRange(stepIncrement = 1.0F, maxValue = 1000F, minValue = 1F)]
        public float surfaceArea = 1;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiName = "Surface Area Upgrade", guiFormat = "F0", guiUnits = " m2"), UI_FloatRange(stepIncrement = 1F, maxValue = 128F, minValue = 0F)]
        public float surfaceAreaUpgrade;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiName = "Pump Speed Upgrade", guiFormat = "F0", guiUnits = " m/s"), UI_FloatRange(stepIncrement = 1F, maxValue = 1024F, minValue = 0F)]
        public float pumpSpeedUpgrade;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Pump Speed"), UI_FloatRange(stepIncrement = 1.0F, maxValue = 1000F, minValue = 0F)]
        public float pumpSpeed = 1;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "intakeAtmSpecificHeatCapacity", guiFormat = "F0", guiUnits = "")]
        public double intakeAtmSpecificHeatCapacity;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "intakeLqdSpecificHeatCapacity", guiFormat = "F0", guiUnits = "")]
        public double intakeLqdSpecificHeatCapacity;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Air Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double airHeatTransferrable;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Water Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double waterHeatTransferrable;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Steam Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double steamHeatTransferrable;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Max Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double heatTransferrable;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Max Heat Supply", guiFormat = "F2", guiUnits = " K")]
        public double maxSupplyOfHeat;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Coolant Supply Used", guiFormat = "F2", guiUnits = "%")]
        public double intakeReduction;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Intake ATM Amount", guiFormat = "F2", guiUnits = "")]
        public double intakeAtmAmount;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Intake Lqd Amount", guiFormat = "F2", guiUnits = "")]
        public double intakeLqdAmount;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Air Coolant Total", guiFormat = "F2", guiUnits = "")]
        public double airCoolantTotal;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Water Coolant Total", guiFormat = "F2", guiUnits = "")]
        public double waterCoolantTotal;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "Steam Coolant Total", guiFormat = "F2", guiUnits = "")]
        public double steamCoolantTotal;

        // https://www.engineersedge.com/heat_transfer/convective_heat_transfer_coefficients__13378.htm
        // forced convection case
        [KSPField]
        public double airHeatTransferCoefficient = 0.002;
        [KSPField]
        public double lqdHeatTransferCoefficient = 0.03;
        [KSPField]
        public double powerDrawInJoules = 1; // How much power needed to run fans / etc. in joules.
        [KSPField]
        public double wasteHeatMultiplier = 1; // Reduce heat radiated in NF mode.

        private int intakeLqdId;
        private int intakeAtmId;

        private double intakeLqdDensity;
        private double intakeAtmDensity;

        private double waterBoilPointInKelvin = 400; // at some stage, calculate it properly

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Upgrades applied")]
        public string upgradeInformation;

        [KSPField] public string surfaceAreaUpgradeMk1;
        [KSPField] public string surfaceAreaUpgradeMk2;
        [KSPField] public string surfaceAreaUpgradeMk3;
        [KSPField] public string surfaceAreaUpgradeMk4;

        [KSPField] public string pumpSpeedUpgradeMk1;
        [KSPField] public string pumpSpeedUpgradeMk2;
        [KSPField] public string pumpSpeedUpgradeMk3;
        [KSPField] public string pumpSpeedUpgradeMk4;

        [KSPField] public string storageTechUpgradeMk1;
        [KSPField] public string storageTechUpgradeMk2;
        [KSPField] public string storageTechUpgradeMk3;
        [KSPField] public string storageTechUpgradeMk4;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk1"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk2"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk2;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk3"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk3;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk4"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk4;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk1"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk2"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk2;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk3"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk3;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk4"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk4;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk1"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk2"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk2;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk3"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk3;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk4"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk4;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "Max External Temp")] public double maxExternalTemp;

        [KSPField] public double defaultMaxExternalTemp = 900;
        [KSPField] public double defaultPumpSpeed = 1;
        [KSPField] public double defaultLqdStorage = 1;
        [KSPField] public double defaultSurfaceArea = 1;

        [KSPField] public float surfaceAreaUpgradeMassCost = 0.005F;
        [KSPField] public float pumpSpeedUpgradeMassCost = 0.01F;
        [KSPField] public float surfaceAreaUpgradePriceCost = 1000F;
        [KSPField] public float pumpSpeedUpgradePriceCost = 500F;

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Show debug information", active = true)]
        public void ToggleHeatPumpDebugAction()
        {
            BaseField[] debugFields = {
                Fields[nameof(surfaceArea)],
                Fields[nameof(intakeLqdSpecificHeatCapacity)],
                Fields[nameof(intakeAtmSpecificHeatCapacity)],
                Fields[nameof(pumpSpeed)],
                Fields[nameof(heatTransferrable)],
                Fields[nameof(maxSupplyOfHeat)],
                Fields[nameof(intakeReduction)],
                Fields[nameof(intakeAtmAmount)],
                Fields[nameof(intakeLqdAmount)],
                Fields[nameof(airCoolantTotal)],
                Fields[nameof(waterCoolantTotal)],
                Fields[nameof(steamCoolantTotal)],
                Fields[nameof(airHeatTransferrable)],
                Fields[nameof(waterHeatTransferrable)],
                Fields[nameof(steamHeatTransferrable)],
                Fields[nameof(hasSurfaceUpgradeMk1)],
                Fields[nameof(hasSurfaceUpgradeMk2)],
                Fields[nameof(hasSurfaceUpgradeMk3)],
                Fields[nameof(hasSurfaceUpgradeMk4)],
                Fields[nameof(hasPumpUpgradeMk1)],
                Fields[nameof(hasPumpUpgradeMk2)],
                Fields[nameof(hasPumpUpgradeMk3)],
                Fields[nameof(hasPumpUpgradeMk4)],
                Fields[nameof(hasStorageUpgradeMk1)],
                Fields[nameof(hasStorageUpgradeMk2)],
                Fields[nameof(hasStorageUpgradeMk3)],
                Fields[nameof(hasStorageUpgradeMk4)],
                Fields[nameof(upgradeInformation)],
            };

            var status = !debugFields[0].guiActive;

            foreach (var x in debugFields)
            {
                x.guiActive = status;
            }

        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Recalculate upgrades", active = true)]
        public void Recalculate()
        {
            processUpgrades();
        }

        private void handleUpgrades()
        {
            hasSurfaceUpgradeMk1 = PluginHelper.UpgradeAvailable(surfaceAreaUpgradeMk1);
            hasSurfaceUpgradeMk2 = PluginHelper.UpgradeAvailable(surfaceAreaUpgradeMk2);
            hasSurfaceUpgradeMk3 = PluginHelper.UpgradeAvailable(surfaceAreaUpgradeMk3);
            hasSurfaceUpgradeMk4 = PluginHelper.UpgradeAvailable(surfaceAreaUpgradeMk4);

            hasPumpUpgradeMk1 = PluginHelper.UpgradeAvailable(pumpSpeedUpgradeMk1);
            hasPumpUpgradeMk2 = PluginHelper.UpgradeAvailable(pumpSpeedUpgradeMk2);
            hasPumpUpgradeMk3 = PluginHelper.UpgradeAvailable(pumpSpeedUpgradeMk3);
            hasPumpUpgradeMk4 = PluginHelper.UpgradeAvailable(pumpSpeedUpgradeMk4);

            hasStorageUpgradeMk1 = PluginHelper.UpgradeAvailable(storageTechUpgradeMk1);
            hasStorageUpgradeMk2 = PluginHelper.UpgradeAvailable(storageTechUpgradeMk2);
            hasStorageUpgradeMk3 = PluginHelper.UpgradeAvailable(storageTechUpgradeMk3);
            hasStorageUpgradeMk4 = PluginHelper.UpgradeAvailable(storageTechUpgradeMk4);
        }

        private void processUpgrades()
        {
            upgradeInformation = $"{hasSurfaceUpgradeMk1}/{hasSurfaceUpgradeMk2}/{hasSurfaceUpgradeMk3}/{hasSurfaceUpgradeMk4}, {hasPumpUpgradeMk1}/{hasPumpUpgradeMk2}/{hasPumpUpgradeMk3}/{hasPumpUpgradeMk4}, {hasStorageUpgradeMk1}/{hasStorageUpgradeMk2}/{hasStorageUpgradeMk3}/{hasStorageUpgradeMk4}";

            pumpSpeed = (float)defaultPumpSpeed * part.rescaleFactor;
            pumpSpeed += pumpSpeedUpgrade;

            // pump speed is used as a direct multiplier
            if (hasPumpUpgradeMk1) pumpSpeed += 59;
            if (hasPumpUpgradeMk2) pumpSpeed += 120;
            if (hasPumpUpgradeMk3) pumpSpeed += 150;
            if (hasPumpUpgradeMk4) pumpSpeed += 270;

            var storage = defaultLqdStorage * part.rescaleFactor;


            // used to calculate coolant total, used as an indirect multiplier
            if (hasStorageUpgradeMk1) storage += 2499;
            if (hasStorageUpgradeMk2) storage += 2500;
            if (hasStorageUpgradeMk3) storage += 5000;
            if (hasStorageUpgradeMk4) storage += 10000;

            var surface = defaultSurfaceArea * part.rescaleFactor;
            surface += surfaceAreaUpgrade;

            var externalTemp = defaultMaxExternalTemp;

            if (hasSurfaceUpgradeMk1)
            {
                surface += 49;
                externalTemp += 225;
            }

            if (hasSurfaceUpgradeMk2)
            {
                surface += 75;
                externalTemp += 225;
            }

            if (hasSurfaceUpgradeMk3)
            {
                surface += 125;
                externalTemp += 225;
            }
            if (hasSurfaceUpgradeMk4)
            {
                surface += 250;
                externalTemp += 225;
            }

            maxExternalTemp = externalTemp;

            var resource = part.Resources["IntakeLqd"];
            resource.amount = 0;
            resource.maxAmount = storage;

            this.surfaceArea = (float)surface;

            var intakeatm = part.FindModuleImplementing<AtmosphericIntake>();
            var intakelqd = part.FindModuleImplementing<ModuleResourceIntake>();
            if(intakeatm == null || intakelqd == null)
            {
                Debug.Log("ActiveCoolingSystem - can't find atmospheric intake or intake lqd module");
                return;
            }

            intakeatm.intakeSpeed = pumpSpeed;
            intakelqd.intakeSpeed = pumpSpeed;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return (surfaceAreaUpgradeMassCost * surfaceAreaUpgrade) + (pumpSpeedUpgradeMassCost * pumpSpeedUpgrade);
        }
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return (surfaceAreaUpgradePriceCost * surfaceAreaUpgrade) + (pumpSpeedUpgradePriceCost * pumpSpeedUpgrade);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (state == StartState.Editor)
            {
                handleUpgrades();
            }
            processUpgrades();

            var intakeLqdDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.IntakeLiquid);
            var intakeAirDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.IntakeOxygenAir);
            var intakeAtmDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.IntakeAtmosphere);

            if (intakeLqdDefinition == null || intakeAirDefinition == null || intakeAtmDefinition == null)
            {
                Debug.Log("[ActiveCoolingSystemv3] Missing definitions :(");
                return;
            }

            intakeLqdSpecificHeatCapacity = intakeLqdDefinition.specificHeatCapacity;
            intakeAtmSpecificHeatCapacity = intakeAtmDefinition.specificHeatCapacity == 0 ? intakeAirDefinition.specificHeatCapacity : intakeAtmDefinition.specificHeatCapacity;

            intakeLqdId = intakeLqdDefinition.id;
            intakeAtmId = intakeAtmDefinition.id;

            intakeAtmDensity = intakeAtmDefinition.density;
            intakeLqdDensity = intakeLqdDefinition.density;
        }

        private double DrawPower()
        {
            // what does electricity look like, anyways?

            var powerNeeded = powerDrawInJoules;
            var powerAvail = consumeFNResourcePerSecond(powerNeeded, ResourceSettings.Config.ElectricPowerInMegawatt);

            return Math.Round(powerAvail / powerNeeded, 2);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            intakeAtmAmount = intakeLqdAmount = 0;

            if (null == vessel || null == part) return;

            part.GetConnectedResourceTotals(intakeAtmId, out intakeAtmAmount, out _);
            part.GetConnectedResourceTotals(intakeLqdId, out intakeLqdAmount, out _);

            if (intakeAtmAmount == 0 && intakeLqdAmount == 0) return;

            /* reduce the efficiency of the transfer if there is not enough power to run at 100% */
            var efficiency = DrawPower();
            if (efficiency == 0) return;

            var wasteheatManager = getManagerForVessel(ResourceSettings.Config.WasteHeatInMegawatt);

            maxSupplyOfHeat = wasteheatManager.CurrentSurplus + wasteheatManager.GetResourceAvailability();
            if (maxSupplyOfHeat == 0) return;

            var fixedDeltaTime = Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);

            airHeatTransferrable = waterHeatTransferrable = steamHeatTransferrable = heatTransferrable = 0;

            // find our baseline of how cold the intake should be. PhysicsGlobals.SpaceTemperature is there in
            // case of negative numbers later on, but that "should not happen".
            double coldTemp = Math.Max(PhysicsGlobals.SpaceTemperature, Math.Min(part.skinTemperature, Math.Min(part.temperature, Math.Min(vessel.atmosphericTemperature, vessel.externalTemperature))));

            // Peter Han has mentioned performance concerns with Get Average Radiator Temp, and suggested I use ResourceFillFraction as a short cut.
            // AntaresMC mentioned that the upgrade system should max out at 1800K, and that 900K should be the starting point.
            double hotTemp = Math.Max(coldTemp + 0.1, coldTemp + (wasteheatManager.ResourceFillFraction * maxExternalTemp));

            if (intakeAtmAmount > 0)
            {
                double deltaT = hotTemp - coldTemp;

                /*
                 * "Don't mind me, I'm just keeping your reactors cool!"
                 *
                 * /\/\
                 *   \_\  _..._
                 *   (" )(_..._)
                 *    ^^  // \\
                 *
                 * What kind of ant is good at adding things up? An accountant.
                 */

                airCoolantTotal =
                    // how much potential energy can the air absorb
                    (intakeAtmDensity * intakeAtmAmount * intakeAtmSpecificHeatCapacity);

                airHeatTransferrable = airHeatTransferCoefficient * efficiency * airCoolantTotal * pumpSpeed * deltaT * surfaceArea;
            }

            if (intakeLqdAmount > 0)
            {
                bool producesSteam = (hotTemp >= waterBoilPointInKelvin);

                /*
                 *           \/       \\
                 *     ___  _@@       @@_  ___
                 *    (___)(_)         (_)(___)
                 *    //|| ||           || ||\\
                 *
                 * What do you call two ants who have a baby together?
                 * Pair ants
                 */

                if (coldTemp < waterBoilPointInKelvin)
                {
                    double deltaT = Math.Min(waterBoilPointInKelvin, hotTemp) - coldTemp;

                    waterCoolantTotal =
                        // how much potential energy can the water absorb
                        (intakeLqdDensity * intakeLqdAmount * intakeLqdSpecificHeatCapacity);

                    waterHeatTransferrable = lqdHeatTransferCoefficient * efficiency * waterCoolantTotal * pumpSpeed * deltaT * surfaceArea;
                }

                /*
                 * Child: I saw some ants on the way to school today.
                 * Dad: How did you know they were going to school?
                 */

                if (producesSteam)
                {
                    double deltaT = hotTemp - Math.Max(waterBoilPointInKelvin, coldTemp);

                    steamCoolantTotal =
                        // A rule of thumb suggests that a gas takes up about 1000 times the volume of a solid or liquid.
                        // We also need to then convert from liters to cubic meters. * 1000 / 1000 = no op.
                        (intakeAtmDensity * intakeLqdAmount * intakeAtmSpecificHeatCapacity);

                    steamHeatTransferrable = airHeatTransferCoefficient * efficiency * steamCoolantTotal * pumpSpeed * deltaT * surfaceArea;
                }
            }

            // how much heat can we transfer in total
            heatTransferrable = wasteHeatMultiplier * (airHeatTransferrable + waterHeatTransferrable + steamHeatTransferrable);
            if (heatTransferrable == 0) return;

            /*
             *             "=.
             *            "=. \
             *               \ \
             *            _,-=\/=._        _.-,_
             *           /         \      /=-._ "-.
             *          |=-./~\___/~\    /     `-._\
             *          |   \o/   \o/   /         /
             *           \_   `~~~;/    |         |
             *             `~,._,-'    /          /
             *                | |      =-._      /
             *            _,-=/ \=-._     /|`-._/
             *          //           \\   )\
             *         /|             |)_.'/
             *        //|             |\_."   _.-\
             *       (|  \           /    _.`=    \
             *       ||   ":_    _.;"_.-;"   _.-=.:
             *    _-."/    / `-."\_."        =-_.;\
             *   `-_./   /             _.-=.    / \\
             *          |              =-_.;\ ."   \\
             *          \                   \\/     \\
             *          /\_                .'\\      \\
             *         //  `=_         _.-"   \\      \\
             *        //      `~-.=`"`'       ||      ||
             *  LGB   ||    _.-_/|            ||      |\_.-_
             *    _.-_/|   /_.-._/            |\_.-_  \_.-._\
             *   /_.-._/                      \_.-._\
             *
             *   "I expected cool ants. Where are the cool ants?!". That ant, probably.
             *
             *   What is the second biggest ant in the world? An elephant.
             *   What ant is bigger than that? A giant.
             */

            intakeReduction = 1;
            var actuallyReduced = heatTransferrable;

            if (maxSupplyOfHeat < heatTransferrable)
            {
                // To avoid the KSPIE screen saying that we're demanding far more WasteHeat than
                // it can supply, we cap the max amount of heat here so it looks like
                // input == output on the screen.
                actuallyReduced = maxSupplyOfHeat;

                // if we could transfer more heat than exists, then we'll reduce the amount of
                // coolant we use.
                intakeReduction = Math.Max(0.10, maxSupplyOfHeat / heatTransferrable);

                /*
                 *  \       /
                 *   \     /
                 *    \.-./
                 *   (o\^/o)  _   _   _     __
                 *    ./ \.\ ( )-( )-( ) .-'  '-.
                 *     {-} \(//  ||   \\/ (   )) '-.
                 *          //-__||__.-\\.       .-'
                 *         (/    ()     \)'-._.-'
                 *         ||    ||      \\
                 * MJP     ('    ('       ')
                 *
                 * How do you tell a girl ant and a boy ant apart?
                 * If it sinks, it's a girl ant. If it floats, it's
                 * a bouyant.
                 */
            }

            var heatTransferred = consumeFNResourcePerSecond(actuallyReduced, ResourceSettings.Config.WasteHeatInMegawatt);

            if (heatTransferred == 0) return;

            if (intakeAtmAmount > 0) part.RequestResource(intakeAtmId, intakeAtmAmount * intakeReduction * fixedDeltaTime);
            if (intakeLqdAmount > 0) part.RequestResource(intakeLqdId, intakeLqdAmount * intakeReduction * fixedDeltaTime);
        }
        public override int getPowerPriority()
        {
            return (int)powerPriority;
        }
    }


    [KSPModule("Radiator")]
    class HeatPumpRadiator : FNRadiator
    {
        // Duplicate code from UniversalCrustExtractor.cs
        // Original: WhatsUnderneath()
        // Changes: returns amount of drill underground.
        private bool IsDrillUnderground(out double undergroundAmount)
        {
            Vector3d partPosition = part.transform.position; // find the position of the transform in 3d space
            var scaleFactor = part.rescaleFactor; // what is the rescale factor of the drill?
            var drillDistance = drillReach * scaleFactor; // adjust the distance for the ray with the rescale factor, needs to be a float for raycast.

            undergroundAmount = 0;

            RaycastHit hit = new RaycastHit(); // create a variable that stores info about hit colliders etc.
            LayerMask terrainMask = 32768; // layermask in unity, number 1 bitshifted to the left 15 times (1 << 15), (terrain = 15, the bitshift is there so that the mask bits are raised; this is a good reading about that: http://answers.unity3d.com/questions/8715/how-do-i-use-layermasks.html)
            Ray drillPartRay = new Ray(partPosition, -part.transform.up); // this ray will start at the part's center and go down in local space coordinates (Vector3d.down is in world space)

            /* This little bit will fire a ray from the part, straight down, in the distance that the part should be able to reach.
             * It returns the resulting RayCastHit.
             *
             * This is actually needed because stock KSP terrain detection is not really dependable. This module was formerly using just part.GroundContact
             * to check for contact, but that seems to be bugged somehow, at least when paired with this drill - it works enough times to pass tests, but when testing
             * this module in a difficult terrain, it just doesn't work properly. (I blame KSP planet meshes + Unity problems with accuracy further away from origin).
            */
            Physics.Raycast(drillPartRay, out hit, drillDistance, terrainMask); // use the defined ray, pass info about a hit, go the proper distance and choose the proper layermask

            // hit anything?
            if (hit.collider == null) return false;

            // how much is underground?
            undergroundAmount = drillDistance - hit.distance;

            return true;
        }
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillReach", guiUnits = " m\xB3")]//Drill reach
        public float drillReach = 5; // How far can the drill actually reach? Used in calculating raycasts to hit ground down below the part. The 5 is just about the reach of the generic drill. Change in part cfg for different models.
        // Duplicate code end

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "Distance underground", guiFormat = "F1", guiUnits = "m")]
        public double undergroundAmount;

        [KSPField(groupName = GROUP, isPersistant = false, guiActive = false, guiName = "Radiator effective size", guiFormat = "F2", guiUnits = "m")]
        public double effectiveSize;

        [KSPField(groupName = GROUP, isPersistant = false, guiActive = false)]
        public double meanGroundTempDistance = 10;

        [KSPField(groupName = GROUP, guiActive = false, guiName = "Cool Temp", guiFormat = "F2", guiUnits = "K")]
        public double coolTemp;

        [KSPField(groupName = GROUP, guiActive = false, guiName = "Hot Temp", guiFormat = "F2", guiUnits = "K")]
        public double hotTemp;

        [KSPField(groupName = GROUP, isPersistant = false, guiActive = true, guiName = "Underground Temp", guiFormat = "F2", guiUnits = "K")]
        public double undergroundTemp;

        [KSPEvent(groupName = GROUP, guiActive = true, guiActiveEditor = false, guiName = "Toggle Heat Pump Information", active = true)]
        public void ToggleHeatPumpDebugAction()
        {
            var coolTempField = Fields[nameof(coolTemp)];
            var hotTempField = Fields[nameof(hotTemp)];
            var effectiveSizeField = Fields[nameof(effectiveSize)];

            var status = !coolTempField.guiActive;

            coolTempField.guiActive = hotTempField.guiActive = effectiveSizeField.guiActive = status;
        }

        private int _frameSkipper;

        private void UpdateEffectiveSize()
        {
            effectiveSize = drillReach;
            undergroundAmount = 0;

            // require the drill to be deployed
            if (_radiatorState != ModuleDeployablePart.DeployState.EXTENDED) return;
            // require the drill to be underground
            if (!IsDrillUnderground(out undergroundAmount)) return;
            if (undergroundAmount == 0) return;

            // reduced effectiveness in space
            if (vessel && vessel.atmDensity == 0)
            {
                // do not convect above ground in a vacuum
                effectiveSize -= (drillReach - undergroundAmount);
            }

            effectiveSize += 10 * undergroundAmount;
            effectiveSize = Math.Round(effectiveSize);

            // Distance reaches mean ground temp region? Time for a Natural bonus.
            if (undergroundAmount >= meanGroundTempDistance)
            {
                effectiveSize *= Math.Max(1.15, Math.Log(undergroundAmount - meanGroundTempDistance, Math.E));
            }

            effectiveSize = Math.Round(effectiveSize);
        }

        protected override double ExternalTemp()
        {
            if (coolTemp == 0 || hotTemp == 0)
            {
                return base.ExternalTemp();
            }

            // Weak approximation of the underground temp.
            return Math.Max(PhysicsGlobals.SpaceTemperature, (coolTemp + hotTemp) / 2 * 0.90);
        }

        public new void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if ((++_frameSkipper % 10) == 0)
            {
                // This code does not need to run all the time.
                var undergroundTempField = Fields[nameof(undergroundTemp)];

                if (vessel != null && vessel.Landed && _radiatorState == ModuleDeployablePart.DeployState.EXTENDED)
                {
                    if (vessel.externalTemperature < coolTemp || coolTemp == 0) coolTemp = vessel.externalTemperature;
                    if (vessel.externalTemperature > hotTemp || hotTemp == 0) hotTemp = vessel.externalTemperature;
                    undergroundTempField.guiActive = true;
                }
                else
                {
                    coolTemp = hotTemp = 0;
                    undergroundTempField.guiActive = false;
                }

                undergroundTemp = ExternalTemp();

                UpdateEffectiveSize();
                radiatorArea = effectiveSize;
                UpdateRadiatorArea();
            }

            base.FixedUpdate();
        }

        protected override bool CanConvect()
        {
            return undergroundAmount > 0;
        }

        protected override double AtmDensity()
        {
            return 1;
        }
    }

    [KSPModule("Radiator")]
    class FNRadiator : ResourceSuppliableModule
    {
        public const string GROUP = "FNRadiator";
        public const string GROUP_TITLE = "#LOC_KSPIE_Radiator_groupName";

        // persistent
        [KSPField(isPersistant = true)] public bool radiatorInit;
        [KSPField(isPersistant = true)] public bool showRetractButton = false;
        [KSPField(isPersistant = true)] public bool showControls = true;
        [KSPField(isPersistant = true)] public double currentRadTemp;
        [KSPField(isPersistant = true)] public bool clarifyFunction;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_Cooling"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Radiator Cooling--Off--On
        public bool radiatorIsEnabled;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_Automated"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Automated-Off-On
        public bool isAutomated = true;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PivotOff"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Pivot--Off--On
        public bool pivotEnabled = true;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PreventShieldedDeploy"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Prevent Shielded Deploy-Off-On
        public bool preventShieldedDeploy = true;

        // non persistent
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_Type")]//Type
        public string radiatorType;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_MaxVacuumTemp", guiFormat = "F0", guiUnits = "K")]//Max Vacuum Temp
        public double maxVacuumTemperature = _maximumRadiatorTempInSpace;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_MaxAtmosphereTemp", guiFormat = "F0", guiUnits = "K")]//Max Atmosphere Temp
        public double maxAtmosphereTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_MaxCurrentTemp", guiFormat = "F0", guiUnits = "K")]//Max Current Temp
        public double maxCurrentRadiatorTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_MaxRadiatorTemperature", guiFormat = "F0", guiUnits = "K")]//Max Radiator Temperature
        public double maxRadiatorTemperature = _maximumRadiatorTempInSpace;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_SpaceRadiatorBonus", guiFormat = "F0", guiUnits = "K")]//Space Radiator Bonus
        public double spaceRadiatorBonus;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_Mass", guiUnits = " t", guiFormat = "F3")]//Mass
        public float partMass;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_ConverctionBonus", guiUnits = "x", guiFormat = "F3")]//Converction Bonus
        public double convectiveBonus = 1;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_EffectiveArea", guiFormat = "F2", guiUnits = " m\xB2")]//Effective Area
        public double effectiveRadiatorArea;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE,  guiName = "#LOC_KSPIE_Radiator_SurfaceArea", guiFormat = "F2", guiUnits = " m\xB2")]//Surface Area
        public double radiatorArea = 1;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive  =  true, guiName = "#LOC_KSPIE_Radiator_SurfaceArea", guiFormat = "F2", guiUnits = " m\xB2")]//Surface Area
        public double baseRadiatorArea;

        [KSPField] public string radiatorTypeMk1 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk1");//NaK Loop Radiator
        [KSPField] public string radiatorTypeMk2 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk2");//Mo Li Heat Pipe Mk1
        [KSPField] public string radiatorTypeMk3 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk3");//"Mo Li Heat Pipe Mk2"
        [KSPField] public string radiatorTypeMk4 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk4");//"Graphene Radiator Mk1"
        [KSPField] public string radiatorTypeMk5 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk5");//"Graphene Radiator Mk2"
        [KSPField] public string radiatorTypeMk6 = Localizer.Format("#LOC_KSPIE_Radiator_radiatorTypeMk6");//"Graphene Radiator Mk3"

        [KSPField] public bool canRadiateHeat = true;
        [KSPField] public bool showColorHeat = true;
        [KSPField] public string surfaceAreaUpgradeTechReq = null;
        [KSPField] public double surfaceAreaUpgradeMult = 1.6;
        [KSPField] public bool isDeployable = false;
        [KSPField] public bool isPassive = false;
        [KSPField] public string animName = "";
        [KSPField] public string thermalAnim = "";
        [KSPField] public string originalName = "";
        [KSPField] public float upgradeCost = 100;
        [KSPField] public bool maintainResourceBuffers = true;
        [KSPField] public float emissiveColorPower = 3;
        [KSPField] public float colorRatioExponent = 1;
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public bool keepMaxPartTempEqualToMaxRadiatorTemp = true;
        [KSPField] public string colorHeat = "_EmissiveColor";
        [KSPField] public string emissiveTextureLocation = "";
        [KSPField] public string bumpMapTextureLocation = "";
        [KSPField] public double areaMultiplier = 1;

        // https://www.engineersedge.com/heat_transfer/convective_heat_transfer_coefficients__13378.htm
        //static public double airHeatTransferCoefficient = 0.001; // 100W/m2/K, range: 10 - 100, "Air"
        //static public double lqdHeatTransferCoefficient = 0.01; // 1000/m2/K, range: 100-1200, "Water in Free Convection"

        [KSPField] public string kspShaderLocation = "KSP/Emissive/Bumped Specular";
        [KSPField] public int RADIATOR_DELAY = 20;
        [KSPField] public int DEPLOYMENT_DELAY = 6;
        [KSPField] public float drapperPoint = 500; // 798

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_Radiator_RadiatorTemp")]//Rad Temp
        public string radiatorTempStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PartTemp")]//Part Temp
        public string partTempStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PowerRadiated")]//Power Radiated
        public string thermalPowerDissipStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PowerConvected")]//Power Convected
        public string thermalPowerConvStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_RadUpgradeCost")]//Rad Upgrade Cost
        public string upgradeCostStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_RadiatorStartTemp")]//Radiator Start Temp
        public double radiator_temperature_temp_val;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_DynamicPressureStress", guiActive = true, guiFormat = "P2")]//Dynamic Pressure Stress
        public double dynamicPressureStress;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_MaxEnergyTransfer", guiFormat = "F2")]//Max Energy Transfer
        private double _maxEnergyTransfer;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "Part Rotation Distance", guiFormat = "F2", guiUnits = "m/s")]
        public double partRotationDistance;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "Atmosphere Density", guiFormat = "F2", guiUnits = "")]
        public double atmDensity;

        public bool IsGraphene { get; private set; }

        // privates
        private double _instantaneousRadTemp;
        private int nrAvailableUpgradeTechs;
        private bool hasSurfaceAreaUpgradeTechReq;
        private float displayTemperature;
        private float colorRatio;
        private double deltaTemp;
        private double spaceRadiatorModifier;
        private double oxidationModifier;
        private double _attachedPartsModifier;
        private double _upgradeModifier;

        // statics
        static double _maximumRadiatorTempInSpace = 4500;
        static double maximumRadiatorTempAtOneAtmosphere = 1200;
        static double _maxSpaceTempBonus;
        static double _temperatureRange;

        // minimize garbage by recycling variables
        private double _stefanArea;
        private double _thermalPowerDissipationPerSecond;
        private double _radiatedThermalPower;
        private double _convectedThermalPower;

        private bool _active;


        private int _radiatorDeployDelay;
        private int _explodeCounter;

        private BaseEvent _deployRadiatorEvent;
        private BaseEvent _retractRadiatorEvent;

        private BaseField _thermalPowerConvStrField;
        private BaseField _radiatorIsEnabledField;
        private BaseField _isAutomatedField;
        private BaseField _pivotEnabledField;

        private Shader _kspShader;
        private Renderer _renderer;
        private Animation _deployAnimation;
        private AnimationState _anim;
        private Renderer[] _renderArray;
        private AnimationState[] _heatStates;
        private ModuleDeployableRadiator _moduleDeployableRadiator;
        private ModuleActiveRadiator _moduleActiveRadiator;
        internal ModuleDeployablePart.DeployState _radiatorState;
        private ResourceBuffers _resourceBuffers;

        private readonly Queue<double> _radTempQueue = new Queue<double>(20);
        private readonly Queue<double> _externalTempQueue = new Queue<double>(20);

        private static readonly Dictionary<Vessel, List<FNRadiator>> RadiatorsByVessel = new Dictionary<Vessel, List<FNRadiator>>();

        private static AnimationCurve redTempColorChannel;
        private static AnimationCurve greenTempColorChannel;
        private static AnimationCurve blueTempColorChannel;

        static private double _intakeLqdDensity;
        static private double _intakeAtmDensity;

        static private double _intakeAtmSpecificHeatCapacity;
        static private double _intakeLqdSpecificHeatCapacity;

        public GenerationType CurrentGenerationType { get; private set; }

        public ModuleActiveRadiator ModuleActiveRadiator => _moduleActiveRadiator;

        public double MaxRadiatorTemperature => maxRadiatorTemperature;

        public static void InitializeTemperatureColorChannels()
        {
            if (redTempColorChannel != null)
                return;

            redTempColorChannel = new AnimationCurve();
            greenTempColorChannel = new AnimationCurve();
            blueTempColorChannel = new AnimationCurve();
            redTempColorChannel = new AnimationCurve();
            greenTempColorChannel = new AnimationCurve();
            blueTempColorChannel = new AnimationCurve();

            redTempColorChannel.AddKey(500, 0 / 255f); greenTempColorChannel.AddKey(500, 0 / 255f); blueTempColorChannel.AddKey(500, 0 / 255f);
            redTempColorChannel.AddKey(800, 100 / 255f); greenTempColorChannel.AddKey(800, 0 / 255f); blueTempColorChannel.AddKey(800, 0 / 255f);

            redTempColorChannel.AddKey(1000, 255 / 255f); greenTempColorChannel.AddKey(1000, 56 / 255f); blueTempColorChannel.AddKey(1000, 0 / 255f);
            redTempColorChannel.AddKey(1100, 255 / 255f); greenTempColorChannel.AddKey(1100, 71 / 255f); blueTempColorChannel.AddKey(1100, 0 / 255f);
            redTempColorChannel.AddKey(1200, 255 / 255f); greenTempColorChannel.AddKey(1200, 83 / 255f); blueTempColorChannel.AddKey(1200, 0 / 255f);
            redTempColorChannel.AddKey(1300, 255 / 255f); greenTempColorChannel.AddKey(1300, 93 / 255f); blueTempColorChannel.AddKey(1300, 0 / 255f);
            redTempColorChannel.AddKey(1400, 255 / 255f); greenTempColorChannel.AddKey(1400, 101 / 255f); blueTempColorChannel.AddKey(1400, 0 / 255f);
            redTempColorChannel.AddKey(1500, 255 / 255f); greenTempColorChannel.AddKey(1500, 109 / 255f); blueTempColorChannel.AddKey(1500, 0 / 255f);
            redTempColorChannel.AddKey(1600, 255 / 255f); greenTempColorChannel.AddKey(1600, 115 / 255f); blueTempColorChannel.AddKey(1600, 0 / 255f);
            redTempColorChannel.AddKey(1700, 255 / 255f); greenTempColorChannel.AddKey(1700, 121 / 255f); blueTempColorChannel.AddKey(1700, 0 / 255f);
            redTempColorChannel.AddKey(1800, 255 / 255f); greenTempColorChannel.AddKey(1800, 126 / 255f); blueTempColorChannel.AddKey(1800, 0 / 255f);
            redTempColorChannel.AddKey(1900, 255 / 255f); greenTempColorChannel.AddKey(1900, 131 / 255f); blueTempColorChannel.AddKey(1900, 0 / 255f);
            redTempColorChannel.AddKey(2000, 255 / 255f); greenTempColorChannel.AddKey(2000, 137 / 255f); blueTempColorChannel.AddKey(2000, 18 / 255f);
            redTempColorChannel.AddKey(2100, 255 / 255f); greenTempColorChannel.AddKey(2100, 142 / 255f); blueTempColorChannel.AddKey(2100, 33 / 255f);
            redTempColorChannel.AddKey(2200, 255 / 255f); greenTempColorChannel.AddKey(2200, 147 / 255f); blueTempColorChannel.AddKey(2200, 44 / 255f);
            redTempColorChannel.AddKey(2300, 255 / 255f); greenTempColorChannel.AddKey(2300, 152 / 255f); blueTempColorChannel.AddKey(2300, 54 / 255f);
            redTempColorChannel.AddKey(2400, 255 / 255f); greenTempColorChannel.AddKey(2400, 157 / 255f); blueTempColorChannel.AddKey(2400, 63 / 255f);
            redTempColorChannel.AddKey(2500, 255 / 255f); greenTempColorChannel.AddKey(2500, 161 / 255f); blueTempColorChannel.AddKey(2500, 72 / 255f);
            redTempColorChannel.AddKey(2600, 255 / 255f); greenTempColorChannel.AddKey(2600, 165 / 255f); blueTempColorChannel.AddKey(2600, 79 / 255f);
            redTempColorChannel.AddKey(2700, 255 / 255f); greenTempColorChannel.AddKey(2700, 169 / 255f); blueTempColorChannel.AddKey(2700, 87 / 255f);
            redTempColorChannel.AddKey(2800, 255 / 255f); greenTempColorChannel.AddKey(2800, 173 / 255f); blueTempColorChannel.AddKey(2800, 94 / 255f);
            redTempColorChannel.AddKey(2900, 255 / 255f); greenTempColorChannel.AddKey(2900, 177 / 255f); blueTempColorChannel.AddKey(2900, 101 / 255f);
            redTempColorChannel.AddKey(3000, 255 / 255f); greenTempColorChannel.AddKey(3000, 180 / 255f); blueTempColorChannel.AddKey(3000, 107 / 255f);
            redTempColorChannel.AddKey(3100, 255 / 255f); greenTempColorChannel.AddKey(3100, 184 / 255f); blueTempColorChannel.AddKey(3100, 114 / 255f);
            redTempColorChannel.AddKey(3200, 255 / 255f); greenTempColorChannel.AddKey(3200, 187 / 255f); blueTempColorChannel.AddKey(3200, 120 / 255f);
            redTempColorChannel.AddKey(3300, 255 / 255f); greenTempColorChannel.AddKey(3300, 190 / 255f); blueTempColorChannel.AddKey(3300, 126 / 255f);
            redTempColorChannel.AddKey(3400, 255 / 255f); greenTempColorChannel.AddKey(3400, 193 / 255f); blueTempColorChannel.AddKey(3400, 132 / 255f);
            redTempColorChannel.AddKey(3500, 255 / 255f); greenTempColorChannel.AddKey(3500, 196 / 255f); blueTempColorChannel.AddKey(3500, 137 / 255f);
            redTempColorChannel.AddKey(3600, 255 / 255f); greenTempColorChannel.AddKey(3600, 199 / 255f); blueTempColorChannel.AddKey(3600, 143 / 255f);
            redTempColorChannel.AddKey(3700, 255 / 255f); greenTempColorChannel.AddKey(3700, 201 / 255f); blueTempColorChannel.AddKey(3700, 148 / 255f);
            redTempColorChannel.AddKey(3800, 255 / 255f); greenTempColorChannel.AddKey(3800, 204 / 255f); blueTempColorChannel.AddKey(3800, 153 / 255f);
            redTempColorChannel.AddKey(3900, 255 / 255f); greenTempColorChannel.AddKey(3900, 206 / 255f); blueTempColorChannel.AddKey(3900, 159 / 255f);
            redTempColorChannel.AddKey(4000, 255 / 255f); greenTempColorChannel.AddKey(4000, 209 / 255f); blueTempColorChannel.AddKey(4000, 163 / 255f);
            redTempColorChannel.AddKey(4100, 255 / 255f); greenTempColorChannel.AddKey(4100, 211 / 255f); blueTempColorChannel.AddKey(4100, 168 / 255f);
            redTempColorChannel.AddKey(4200, 255 / 255f); greenTempColorChannel.AddKey(4200, 213 / 255f); blueTempColorChannel.AddKey(4200, 173 / 255f);
            redTempColorChannel.AddKey(4300, 255 / 255f); greenTempColorChannel.AddKey(4300, 215 / 255f); blueTempColorChannel.AddKey(4300, 177 / 255f);
            redTempColorChannel.AddKey(4400, 255 / 255f); greenTempColorChannel.AddKey(4400, 217 / 255f); blueTempColorChannel.AddKey(4400, 182 / 255f);
            redTempColorChannel.AddKey(4500, 255 / 255f); greenTempColorChannel.AddKey(4500, 219 / 255f); blueTempColorChannel.AddKey(4500, 186 / 255f);
            redTempColorChannel.AddKey(4600, 255 / 255f); greenTempColorChannel.AddKey(4600, 221 / 255f); blueTempColorChannel.AddKey(4600, 190 / 255f);
            redTempColorChannel.AddKey(4700, 255 / 255f); greenTempColorChannel.AddKey(4700, 223 / 255f); blueTempColorChannel.AddKey(4700, 194 / 255f);
            redTempColorChannel.AddKey(4800, 255 / 255f); greenTempColorChannel.AddKey(4800, 225 / 255f); blueTempColorChannel.AddKey(4800, 198 / 255f);
            redTempColorChannel.AddKey(4900, 255 / 255f); greenTempColorChannel.AddKey(4900, 227 / 255f); blueTempColorChannel.AddKey(4900, 202 / 255f);
            redTempColorChannel.AddKey(5000, 255 / 255f); greenTempColorChannel.AddKey(5000, 228 / 255f); blueTempColorChannel.AddKey(5000, 206 / 255f);
            redTempColorChannel.AddKey(5100, 255 / 255f); greenTempColorChannel.AddKey(5100, 230 / 255f); blueTempColorChannel.AddKey(5100, 210 / 255f);
            redTempColorChannel.AddKey(5200, 255 / 255f); greenTempColorChannel.AddKey(5200, 232 / 255f); blueTempColorChannel.AddKey(5200, 213 / 255f);
            redTempColorChannel.AddKey(5300, 255 / 255f); greenTempColorChannel.AddKey(5300, 233 / 255f); blueTempColorChannel.AddKey(5300, 217 / 255f);
            redTempColorChannel.AddKey(5400, 255 / 255f); greenTempColorChannel.AddKey(5400, 235 / 255f); blueTempColorChannel.AddKey(5400, 220 / 255f);
            redTempColorChannel.AddKey(5500, 255 / 255f); greenTempColorChannel.AddKey(5500, 236 / 255f); blueTempColorChannel.AddKey(5500, 224 / 255f);
            redTempColorChannel.AddKey(5600, 255 / 255f); greenTempColorChannel.AddKey(5600, 238 / 255f); blueTempColorChannel.AddKey(5600, 227 / 255f);
            redTempColorChannel.AddKey(5700, 255 / 255f); greenTempColorChannel.AddKey(5700, 239 / 255f); blueTempColorChannel.AddKey(5700, 230 / 255f);
            redTempColorChannel.AddKey(5800, 255 / 255f); greenTempColorChannel.AddKey(5800, 240 / 255f); blueTempColorChannel.AddKey(5800, 233 / 255f);
            redTempColorChannel.AddKey(5900, 255 / 255f); greenTempColorChannel.AddKey(5900, 242 / 255f); blueTempColorChannel.AddKey(5900, 236 / 255f);
            redTempColorChannel.AddKey(6000, 255 / 255f); greenTempColorChannel.AddKey(6000, 243 / 255f); blueTempColorChannel.AddKey(6000, 239 / 255f);
            redTempColorChannel.AddKey(6100, 255 / 255f); greenTempColorChannel.AddKey(6100, 244 / 255f); blueTempColorChannel.AddKey(6100, 242 / 255f);
            redTempColorChannel.AddKey(6200, 255 / 255f); greenTempColorChannel.AddKey(6200, 245 / 255f); blueTempColorChannel.AddKey(6200, 245 / 255f);
            redTempColorChannel.AddKey(6300, 255 / 255f); greenTempColorChannel.AddKey(6300, 246 / 255f); blueTempColorChannel.AddKey(6300, 248 / 255f);
            redTempColorChannel.AddKey(6400, 255 / 255f); greenTempColorChannel.AddKey(6400, 248 / 255f); blueTempColorChannel.AddKey(6400, 251 / 255f);
            redTempColorChannel.AddKey(6500, 255 / 255f); greenTempColorChannel.AddKey(6500, 249 / 255f); blueTempColorChannel.AddKey(6500, 253 / 255f);
            redTempColorChannel.AddKey(6600, 254 / 255f); greenTempColorChannel.AddKey(6600, 249 / 255f); blueTempColorChannel.AddKey(6600, 255 / 255f);
            redTempColorChannel.AddKey(6700, 252 / 255f); greenTempColorChannel.AddKey(6700, 247 / 255f); blueTempColorChannel.AddKey(6700, 255 / 255f);
            redTempColorChannel.AddKey(6800, 249 / 255f); greenTempColorChannel.AddKey(6800, 246 / 255f); blueTempColorChannel.AddKey(6800, 255 / 255f);
            redTempColorChannel.AddKey(6900, 247 / 255f); greenTempColorChannel.AddKey(6900, 245 / 255f); blueTempColorChannel.AddKey(6900, 255 / 255f);
            redTempColorChannel.AddKey(7000, 245 / 255f); greenTempColorChannel.AddKey(7000, 243 / 255f); blueTempColorChannel.AddKey(7000, 255 / 255f);
            redTempColorChannel.AddKey(7100, 243 / 255f); greenTempColorChannel.AddKey(7100, 242 / 255f); blueTempColorChannel.AddKey(7100, 255 / 255f);
            redTempColorChannel.AddKey(7200, 240 / 255f); greenTempColorChannel.AddKey(7200, 241 / 255f); blueTempColorChannel.AddKey(7200, 255 / 255f);
            redTempColorChannel.AddKey(7300, 239 / 255f); greenTempColorChannel.AddKey(7300, 240 / 255f); blueTempColorChannel.AddKey(7300, 255 / 255f);
            redTempColorChannel.AddKey(7400, 237 / 255f); greenTempColorChannel.AddKey(7400, 239 / 255f); blueTempColorChannel.AddKey(7400, 255 / 255f);
            redTempColorChannel.AddKey(7500, 235 / 255f); greenTempColorChannel.AddKey(7500, 238 / 255f); blueTempColorChannel.AddKey(7500, 255 / 255f);
            redTempColorChannel.AddKey(7600, 233 / 255f); greenTempColorChannel.AddKey(7600, 237 / 255f); blueTempColorChannel.AddKey(7600, 255 / 255f);
            redTempColorChannel.AddKey(7700, 231 / 255f); greenTempColorChannel.AddKey(7700, 236 / 255f); blueTempColorChannel.AddKey(7700, 255 / 255f);
            redTempColorChannel.AddKey(7800, 230 / 255f); greenTempColorChannel.AddKey(7800, 235 / 255f); blueTempColorChannel.AddKey(7800, 255 / 255f);
            redTempColorChannel.AddKey(7900, 228 / 255f); greenTempColorChannel.AddKey(7900, 234 / 255f); blueTempColorChannel.AddKey(7900, 255 / 255f);
            redTempColorChannel.AddKey(8000, 227 / 255f); greenTempColorChannel.AddKey(8000, 233 / 255f); blueTempColorChannel.AddKey(8000, 255 / 255f);
            redTempColorChannel.AddKey(8100, 225 / 255f); greenTempColorChannel.AddKey(8100, 232 / 255f); blueTempColorChannel.AddKey(8100, 255 / 255f);
            redTempColorChannel.AddKey(8200, 224 / 255f); greenTempColorChannel.AddKey(8200, 231 / 255f); blueTempColorChannel.AddKey(8200, 255 / 255f);
            redTempColorChannel.AddKey(8300, 222 / 255f); greenTempColorChannel.AddKey(8300, 230 / 255f); blueTempColorChannel.AddKey(8300, 255 / 255f);
            redTempColorChannel.AddKey(8400, 221 / 255f); greenTempColorChannel.AddKey(8400, 230 / 255f); blueTempColorChannel.AddKey(8400, 255 / 255f);
            redTempColorChannel.AddKey(8500, 220 / 255f); greenTempColorChannel.AddKey(8500, 229 / 255f); blueTempColorChannel.AddKey(8500, 255 / 255f);
            redTempColorChannel.AddKey(8600, 218 / 255f); greenTempColorChannel.AddKey(8600, 228 / 255f); blueTempColorChannel.AddKey(8600, 255 / 255f);
            redTempColorChannel.AddKey(8700, 217 / 255f); greenTempColorChannel.AddKey(8700, 227 / 255f); blueTempColorChannel.AddKey(8700, 255 / 255f);
            redTempColorChannel.AddKey(8800, 216 / 255f); greenTempColorChannel.AddKey(8800, 227 / 255f); blueTempColorChannel.AddKey(8800, 255 / 255f);
            redTempColorChannel.AddKey(8900, 215 / 255f); greenTempColorChannel.AddKey(8900, 226 / 255f); blueTempColorChannel.AddKey(8900, 255 / 255f);
            redTempColorChannel.AddKey(9000, 214 / 255f); greenTempColorChannel.AddKey(9000, 225 / 255f); blueTempColorChannel.AddKey(9000, 255 / 255f);
            redTempColorChannel.AddKey(9100, 212 / 255f); greenTempColorChannel.AddKey(9100, 225 / 255f); blueTempColorChannel.AddKey(9100, 255 / 255f);
            redTempColorChannel.AddKey(9200, 211 / 255f); greenTempColorChannel.AddKey(9200, 224 / 255f); blueTempColorChannel.AddKey(9200, 255 / 255f);
            redTempColorChannel.AddKey(9300, 210 / 255f); greenTempColorChannel.AddKey(9300, 223 / 255f); blueTempColorChannel.AddKey(9300, 255 / 255f);
            redTempColorChannel.AddKey(9400, 209 / 255f); greenTempColorChannel.AddKey(9400, 223 / 255f); blueTempColorChannel.AddKey(9400, 255 / 255f);
            redTempColorChannel.AddKey(9500, 208 / 255f); greenTempColorChannel.AddKey(9500, 222 / 255f); blueTempColorChannel.AddKey(9500, 255 / 255f);
            redTempColorChannel.AddKey(9600, 207 / 255f); greenTempColorChannel.AddKey(9600, 221 / 255f); blueTempColorChannel.AddKey(9600, 255 / 255f);
            redTempColorChannel.AddKey(9700, 207 / 255f); greenTempColorChannel.AddKey(9700, 221 / 255f); blueTempColorChannel.AddKey(9700, 255 / 255f);
            redTempColorChannel.AddKey(9800, 206 / 255f); greenTempColorChannel.AddKey(9800, 220 / 255f); blueTempColorChannel.AddKey(9800, 255 / 255f);
            redTempColorChannel.AddKey(9900, 205 / 255f); greenTempColorChannel.AddKey(9900, 220 / 255f); blueTempColorChannel.AddKey(9900, 255 / 255f);
            redTempColorChannel.AddKey(10000, 204 / 255f); greenTempColorChannel.AddKey(10000, 219 / 255f); blueTempColorChannel.AddKey(10000, 255 / 255f);
            redTempColorChannel.AddKey(10100, 203 / 255f); greenTempColorChannel.AddKey(10100, 219 / 255f); blueTempColorChannel.AddKey(10100, 255 / 255f);
            redTempColorChannel.AddKey(10200, 202 / 255f); greenTempColorChannel.AddKey(10200, 218 / 255f); blueTempColorChannel.AddKey(10200, 255 / 255f);
            redTempColorChannel.AddKey(10300, 201 / 255f); greenTempColorChannel.AddKey(10300, 218 / 255f); blueTempColorChannel.AddKey(10300, 255 / 255f);
            redTempColorChannel.AddKey(10400, 201 / 255f); greenTempColorChannel.AddKey(10400, 217 / 255f); blueTempColorChannel.AddKey(10400, 255 / 255f);
            redTempColorChannel.AddKey(10500, 200 / 255f); greenTempColorChannel.AddKey(10500, 217 / 255f); blueTempColorChannel.AddKey(10500, 255 / 255f);
            redTempColorChannel.AddKey(10600, 199 / 255f); greenTempColorChannel.AddKey(10600, 216 / 255f); blueTempColorChannel.AddKey(10600, 255 / 255f);
            redTempColorChannel.AddKey(10700, 199 / 255f); greenTempColorChannel.AddKey(10700, 216 / 255f); blueTempColorChannel.AddKey(10700, 255 / 255f);
            redTempColorChannel.AddKey(10800, 198 / 255f); greenTempColorChannel.AddKey(10800, 216 / 255f); blueTempColorChannel.AddKey(10800, 255 / 255f);
            redTempColorChannel.AddKey(10900, 197 / 255f); greenTempColorChannel.AddKey(10900, 215 / 255f); blueTempColorChannel.AddKey(10900, 255 / 255f);
            redTempColorChannel.AddKey(11000, 196 / 255f); greenTempColorChannel.AddKey(11000, 215 / 255f); blueTempColorChannel.AddKey(11000, 255 / 255f);
            redTempColorChannel.AddKey(11100, 196 / 255f); greenTempColorChannel.AddKey(11100, 214 / 255f); blueTempColorChannel.AddKey(11100, 255 / 255f);
            redTempColorChannel.AddKey(11200, 195 / 255f); greenTempColorChannel.AddKey(11200, 214 / 255f); blueTempColorChannel.AddKey(11200, 255 / 255f);
            redTempColorChannel.AddKey(11300, 195 / 255f); greenTempColorChannel.AddKey(11300, 214 / 255f); blueTempColorChannel.AddKey(11300, 255 / 255f);
            redTempColorChannel.AddKey(11400, 194 / 255f); greenTempColorChannel.AddKey(11400, 213 / 255f); blueTempColorChannel.AddKey(11400, 255 / 255f);
            redTempColorChannel.AddKey(11500, 193 / 255f); greenTempColorChannel.AddKey(11500, 213 / 255f); blueTempColorChannel.AddKey(11500, 255 / 255f);
            redTempColorChannel.AddKey(11600, 193 / 255f); greenTempColorChannel.AddKey(11600, 212 / 255f); blueTempColorChannel.AddKey(11600, 255 / 255f);
            redTempColorChannel.AddKey(11700, 192 / 255f); greenTempColorChannel.AddKey(11700, 212 / 255f); blueTempColorChannel.AddKey(11700, 255 / 255f);
            redTempColorChannel.AddKey(11800, 192 / 255f); greenTempColorChannel.AddKey(11800, 212 / 255f); blueTempColorChannel.AddKey(11800, 255 / 255f);
            redTempColorChannel.AddKey(11900, 191 / 255f); greenTempColorChannel.AddKey(11900, 211 / 255f); blueTempColorChannel.AddKey(11900, 255 / 255f);
            redTempColorChannel.AddKey(12000, 191 / 255f); greenTempColorChannel.AddKey(12000, 211 / 255f); blueTempColorChannel.AddKey(12000, 255 / 255f);
            redTempColorChannel.AddKey(12100, 190 / 255f); greenTempColorChannel.AddKey(12100, 211 / 255f); blueTempColorChannel.AddKey(12100, 255 / 255f);
            redTempColorChannel.AddKey(12200, 190 / 255f); greenTempColorChannel.AddKey(12200, 210 / 255f); blueTempColorChannel.AddKey(12200, 255 / 255f);
            redTempColorChannel.AddKey(12300, 189 / 255f); greenTempColorChannel.AddKey(12300, 210 / 255f); blueTempColorChannel.AddKey(12300, 255 / 255f);
            redTempColorChannel.AddKey(12400, 189 / 255f); greenTempColorChannel.AddKey(12400, 210 / 255f); blueTempColorChannel.AddKey(12400, 255 / 255f);
            redTempColorChannel.AddKey(12500, 188 / 255f); greenTempColorChannel.AddKey(12500, 210 / 255f); blueTempColorChannel.AddKey(12500, 255 / 255f);
            redTempColorChannel.AddKey(12600, 188 / 255f); greenTempColorChannel.AddKey(12600, 209 / 255f); blueTempColorChannel.AddKey(12600, 255 / 255f);
            redTempColorChannel.AddKey(12700, 187 / 255f); greenTempColorChannel.AddKey(12700, 209 / 255f); blueTempColorChannel.AddKey(12700, 255 / 255f);
            redTempColorChannel.AddKey(12800, 187 / 255f); greenTempColorChannel.AddKey(12800, 209 / 255f); blueTempColorChannel.AddKey(12800, 255 / 255f);
            redTempColorChannel.AddKey(12900, 186 / 255f); greenTempColorChannel.AddKey(12900, 208 / 255f); blueTempColorChannel.AddKey(12900, 255 / 255f);
            redTempColorChannel.AddKey(13000, 186 / 255f); greenTempColorChannel.AddKey(13000, 208 / 255f); blueTempColorChannel.AddKey(13000, 255 / 255f);
            redTempColorChannel.AddKey(13100, 185 / 255f); greenTempColorChannel.AddKey(13100, 208 / 255f); blueTempColorChannel.AddKey(13100, 255 / 255f);
            redTempColorChannel.AddKey(13200, 185 / 255f); greenTempColorChannel.AddKey(13200, 208 / 255f); blueTempColorChannel.AddKey(13200, 255 / 255f);
            redTempColorChannel.AddKey(13300, 185 / 255f); greenTempColorChannel.AddKey(13300, 207 / 255f); blueTempColorChannel.AddKey(13300, 255 / 255f);
            redTempColorChannel.AddKey(13400, 184 / 255f); greenTempColorChannel.AddKey(13400, 207 / 255f); blueTempColorChannel.AddKey(13400, 255 / 255f);
            redTempColorChannel.AddKey(13500, 184 / 255f); greenTempColorChannel.AddKey(13500, 207 / 255f); blueTempColorChannel.AddKey(13500, 255 / 255f);
            redTempColorChannel.AddKey(13600, 183 / 255f); greenTempColorChannel.AddKey(13600, 207 / 255f); blueTempColorChannel.AddKey(13600, 255 / 255f);
            redTempColorChannel.AddKey(13700, 183 / 255f); greenTempColorChannel.AddKey(13700, 206 / 255f); blueTempColorChannel.AddKey(13700, 255 / 255f);
            redTempColorChannel.AddKey(13800, 183 / 255f); greenTempColorChannel.AddKey(13800, 206 / 255f); blueTempColorChannel.AddKey(13800, 255 / 255f);
            redTempColorChannel.AddKey(13900, 182 / 255f); greenTempColorChannel.AddKey(13900, 206 / 255f); blueTempColorChannel.AddKey(13900, 255 / 255f);
            redTempColorChannel.AddKey(14000, 182 / 255f); greenTempColorChannel.AddKey(14000, 206 / 255f); blueTempColorChannel.AddKey(14000, 255 / 255f);
            redTempColorChannel.AddKey(14100, 182 / 255f); greenTempColorChannel.AddKey(14100, 205 / 255f); blueTempColorChannel.AddKey(14100, 255 / 255f);
            redTempColorChannel.AddKey(14200, 181 / 255f); greenTempColorChannel.AddKey(14200, 205 / 255f); blueTempColorChannel.AddKey(14200, 255 / 255f);
            redTempColorChannel.AddKey(14300, 181 / 255f); greenTempColorChannel.AddKey(14300, 205 / 255f); blueTempColorChannel.AddKey(14300, 255 / 255f);
            redTempColorChannel.AddKey(14400, 181 / 255f); greenTempColorChannel.AddKey(14400, 205 / 255f); blueTempColorChannel.AddKey(14400, 255 / 255f);
            redTempColorChannel.AddKey(14500, 180 / 255f); greenTempColorChannel.AddKey(14500, 205 / 255f); blueTempColorChannel.AddKey(14500, 255 / 255f);
            redTempColorChannel.AddKey(14600, 180 / 255f); greenTempColorChannel.AddKey(14600, 204 / 255f); blueTempColorChannel.AddKey(14600, 255 / 255f);
            redTempColorChannel.AddKey(14700, 180 / 255f); greenTempColorChannel.AddKey(14700, 204 / 255f); blueTempColorChannel.AddKey(14700, 255 / 255f);
            redTempColorChannel.AddKey(14800, 179 / 255f); greenTempColorChannel.AddKey(14800, 204 / 255f); blueTempColorChannel.AddKey(14800, 255 / 255f);
            redTempColorChannel.AddKey(14900, 179 / 255f); greenTempColorChannel.AddKey(14900, 204 / 255f); blueTempColorChannel.AddKey(14900, 255 / 255f);
            redTempColorChannel.AddKey(15000, 179 / 255f); greenTempColorChannel.AddKey(15000, 204 / 255f); blueTempColorChannel.AddKey(15000, 255 / 255f);
            redTempColorChannel.AddKey(15100, 178 / 255f); greenTempColorChannel.AddKey(15100, 203 / 255f); blueTempColorChannel.AddKey(15100, 255 / 255f);
            redTempColorChannel.AddKey(15200, 178 / 255f); greenTempColorChannel.AddKey(15200, 203 / 255f); blueTempColorChannel.AddKey(15200, 255 / 255f);
            redTempColorChannel.AddKey(15300, 178 / 255f); greenTempColorChannel.AddKey(15300, 203 / 255f); blueTempColorChannel.AddKey(15300, 255 / 255f);
            redTempColorChannel.AddKey(15400, 178 / 255f); greenTempColorChannel.AddKey(15400, 203 / 255f); blueTempColorChannel.AddKey(15400, 255 / 255f);
            redTempColorChannel.AddKey(15500, 177 / 255f); greenTempColorChannel.AddKey(15500, 203 / 255f); blueTempColorChannel.AddKey(15500, 255 / 255f);
            redTempColorChannel.AddKey(15600, 177 / 255f); greenTempColorChannel.AddKey(15600, 202 / 255f); blueTempColorChannel.AddKey(15600, 255 / 255f);
            redTempColorChannel.AddKey(15700, 177 / 255f); greenTempColorChannel.AddKey(15700, 202 / 255f); blueTempColorChannel.AddKey(15700, 255 / 255f);
            redTempColorChannel.AddKey(15800, 177 / 255f); greenTempColorChannel.AddKey(15800, 202 / 255f); blueTempColorChannel.AddKey(15800, 255 / 255f);
            redTempColorChannel.AddKey(15900, 176 / 255f); greenTempColorChannel.AddKey(15900, 202 / 255f); blueTempColorChannel.AddKey(15900, 255 / 255f);
            redTempColorChannel.AddKey(16000, 176 / 255f); greenTempColorChannel.AddKey(16000, 202 / 255f); blueTempColorChannel.AddKey(16000, 255 / 255f);
            redTempColorChannel.AddKey(16100, 176 / 255f); greenTempColorChannel.AddKey(16100, 202 / 255f); blueTempColorChannel.AddKey(16100, 255 / 255f);
            redTempColorChannel.AddKey(16200, 175 / 255f); greenTempColorChannel.AddKey(16200, 201 / 255f); blueTempColorChannel.AddKey(16200, 255 / 255f);
            redTempColorChannel.AddKey(16300, 175 / 255f); greenTempColorChannel.AddKey(16300, 201 / 255f); blueTempColorChannel.AddKey(16300, 255 / 255f);
            redTempColorChannel.AddKey(16400, 175 / 255f); greenTempColorChannel.AddKey(16400, 201 / 255f); blueTempColorChannel.AddKey(16400, 255 / 255f);
            redTempColorChannel.AddKey(16500, 175 / 255f); greenTempColorChannel.AddKey(16500, 201 / 255f); blueTempColorChannel.AddKey(16500, 255 / 255f);
            redTempColorChannel.AddKey(16600, 175 / 255f); greenTempColorChannel.AddKey(16600, 201 / 255f); blueTempColorChannel.AddKey(16600, 255 / 255f);
            redTempColorChannel.AddKey(16700, 174 / 255f); greenTempColorChannel.AddKey(16700, 201 / 255f); blueTempColorChannel.AddKey(16700, 255 / 255f);
            redTempColorChannel.AddKey(16800, 174 / 255f); greenTempColorChannel.AddKey(16800, 201 / 255f); blueTempColorChannel.AddKey(16800, 255 / 255f);
            redTempColorChannel.AddKey(16900, 174 / 255f); greenTempColorChannel.AddKey(16900, 200 / 255f); blueTempColorChannel.AddKey(16900, 255 / 255f);
            redTempColorChannel.AddKey(17000, 174 / 255f); greenTempColorChannel.AddKey(17000, 200 / 255f); blueTempColorChannel.AddKey(17000, 255 / 255f);
            redTempColorChannel.AddKey(17100, 173 / 255f); greenTempColorChannel.AddKey(17100, 200 / 255f); blueTempColorChannel.AddKey(17100, 255 / 255f);
            redTempColorChannel.AddKey(17200, 173 / 255f); greenTempColorChannel.AddKey(17200, 200 / 255f); blueTempColorChannel.AddKey(17200, 255 / 255f);
            redTempColorChannel.AddKey(17300, 173 / 255f); greenTempColorChannel.AddKey(17300, 200 / 255f); blueTempColorChannel.AddKey(17300, 255 / 255f);
            redTempColorChannel.AddKey(17400, 173 / 255f); greenTempColorChannel.AddKey(17400, 200 / 255f); blueTempColorChannel.AddKey(17400, 255 / 255f);
            redTempColorChannel.AddKey(17500, 173 / 255f); greenTempColorChannel.AddKey(17500, 200 / 255f); blueTempColorChannel.AddKey(17500, 255 / 255f);
            redTempColorChannel.AddKey(17600, 172 / 255f); greenTempColorChannel.AddKey(17600, 199 / 255f); blueTempColorChannel.AddKey(17600, 255 / 255f);
            redTempColorChannel.AddKey(17700, 172 / 255f); greenTempColorChannel.AddKey(17700, 199 / 255f); blueTempColorChannel.AddKey(17700, 255 / 255f);
            redTempColorChannel.AddKey(17800, 172 / 255f); greenTempColorChannel.AddKey(17800, 199 / 255f); blueTempColorChannel.AddKey(17800, 255 / 255f);
            redTempColorChannel.AddKey(17900, 172 / 255f); greenTempColorChannel.AddKey(17900, 199 / 255f); blueTempColorChannel.AddKey(17900, 255 / 255f);
            redTempColorChannel.AddKey(18000, 172 / 255f); greenTempColorChannel.AddKey(18000, 199 / 255f); blueTempColorChannel.AddKey(18000, 255 / 255f);
            redTempColorChannel.AddKey(18100, 171 / 255f); greenTempColorChannel.AddKey(18100, 199 / 255f); blueTempColorChannel.AddKey(18100, 255 / 255f);
            redTempColorChannel.AddKey(18200, 171 / 255f); greenTempColorChannel.AddKey(18200, 199 / 255f); blueTempColorChannel.AddKey(18200, 255 / 255f);
            redTempColorChannel.AddKey(18300, 171 / 255f); greenTempColorChannel.AddKey(18300, 199 / 255f); blueTempColorChannel.AddKey(18300, 255 / 255f);
            redTempColorChannel.AddKey(18400, 171 / 255f); greenTempColorChannel.AddKey(18400, 198 / 255f); blueTempColorChannel.AddKey(18400, 255 / 255f);
            redTempColorChannel.AddKey(18500, 171 / 255f); greenTempColorChannel.AddKey(18500, 198 / 255f); blueTempColorChannel.AddKey(18500, 255 / 255f);
            redTempColorChannel.AddKey(18600, 170 / 255f); greenTempColorChannel.AddKey(18600, 198 / 255f); blueTempColorChannel.AddKey(18600, 255 / 255f);
            redTempColorChannel.AddKey(18700, 170 / 255f); greenTempColorChannel.AddKey(18700, 198 / 255f); blueTempColorChannel.AddKey(18700, 255 / 255f);
            redTempColorChannel.AddKey(18800, 170 / 255f); greenTempColorChannel.AddKey(18800, 198 / 255f); blueTempColorChannel.AddKey(18800, 255 / 255f);
            redTempColorChannel.AddKey(18900, 170 / 255f); greenTempColorChannel.AddKey(18900, 198 / 255f); blueTempColorChannel.AddKey(18900, 255 / 255f);
            redTempColorChannel.AddKey(19000, 170 / 255f); greenTempColorChannel.AddKey(19000, 198 / 255f); blueTempColorChannel.AddKey(19000, 255 / 255f);
            redTempColorChannel.AddKey(19100, 170 / 255f); greenTempColorChannel.AddKey(19100, 198 / 255f); blueTempColorChannel.AddKey(19100, 255 / 255f);
            redTempColorChannel.AddKey(19200, 169 / 255f); greenTempColorChannel.AddKey(19200, 198 / 255f); blueTempColorChannel.AddKey(19200, 255 / 255f);
            redTempColorChannel.AddKey(19300, 169 / 255f); greenTempColorChannel.AddKey(19300, 197 / 255f); blueTempColorChannel.AddKey(19300, 255 / 255f);
            redTempColorChannel.AddKey(19400, 169 / 255f); greenTempColorChannel.AddKey(19400, 197 / 255f); blueTempColorChannel.AddKey(19400, 255 / 255f);
            redTempColorChannel.AddKey(19500, 169 / 255f); greenTempColorChannel.AddKey(19500, 197 / 255f); blueTempColorChannel.AddKey(19500, 255 / 255f);
            redTempColorChannel.AddKey(19600, 169 / 255f); greenTempColorChannel.AddKey(19600, 197 / 255f); blueTempColorChannel.AddKey(19600, 255 / 255f);
            redTempColorChannel.AddKey(19700, 169 / 255f); greenTempColorChannel.AddKey(19700, 197 / 255f); blueTempColorChannel.AddKey(19700, 255 / 255f);
            redTempColorChannel.AddKey(19800, 169 / 255f); greenTempColorChannel.AddKey(19800, 197 / 255f); blueTempColorChannel.AddKey(19800, 255 / 255f);
            redTempColorChannel.AddKey(19900, 168 / 255f); greenTempColorChannel.AddKey(19900, 197 / 255f); blueTempColorChannel.AddKey(19900, 255 / 255f);
            redTempColorChannel.AddKey(20000, 168 / 255f); greenTempColorChannel.AddKey(20000, 197 / 255f); blueTempColorChannel.AddKey(20000, 255 / 255f);
            redTempColorChannel.AddKey(20100, 168 / 255f); greenTempColorChannel.AddKey(20100, 197 / 255f); blueTempColorChannel.AddKey(20100, 255 / 255f);
            redTempColorChannel.AddKey(20200, 168 / 255f); greenTempColorChannel.AddKey(20200, 197 / 255f); blueTempColorChannel.AddKey(20200, 255 / 255f);
            redTempColorChannel.AddKey(20300, 168 / 255f); greenTempColorChannel.AddKey(20300, 196 / 255f); blueTempColorChannel.AddKey(20300, 255 / 255f);
            redTempColorChannel.AddKey(20400, 168 / 255f); greenTempColorChannel.AddKey(20400, 196 / 255f); blueTempColorChannel.AddKey(20400, 255 / 255f);
            redTempColorChannel.AddKey(20500, 168 / 255f); greenTempColorChannel.AddKey(20500, 196 / 255f); blueTempColorChannel.AddKey(20500, 255 / 255f);
            redTempColorChannel.AddKey(20600, 167 / 255f); greenTempColorChannel.AddKey(20600, 196 / 255f); blueTempColorChannel.AddKey(20600, 255 / 255f);
            redTempColorChannel.AddKey(20700, 167 / 255f); greenTempColorChannel.AddKey(20700, 196 / 255f); blueTempColorChannel.AddKey(20700, 255 / 255f);
            redTempColorChannel.AddKey(20800, 167 / 255f); greenTempColorChannel.AddKey(20800, 196 / 255f); blueTempColorChannel.AddKey(20800, 255 / 255f);
            redTempColorChannel.AddKey(20900, 167 / 255f); greenTempColorChannel.AddKey(20900, 196 / 255f); blueTempColorChannel.AddKey(20900, 255 / 255f);
            redTempColorChannel.AddKey(21000, 167 / 255f); greenTempColorChannel.AddKey(21000, 196 / 255f); blueTempColorChannel.AddKey(21000, 255 / 255f);
            redTempColorChannel.AddKey(21100, 167 / 255f); greenTempColorChannel.AddKey(21100, 196 / 255f); blueTempColorChannel.AddKey(21100, 255 / 255f);
            redTempColorChannel.AddKey(21200, 167 / 255f); greenTempColorChannel.AddKey(21200, 196 / 255f); blueTempColorChannel.AddKey(21200, 255 / 255f);
            redTempColorChannel.AddKey(21300, 166 / 255f); greenTempColorChannel.AddKey(21300, 196 / 255f); blueTempColorChannel.AddKey(21300, 255 / 255f);
            redTempColorChannel.AddKey(21400, 166 / 255f); greenTempColorChannel.AddKey(21400, 195 / 255f); blueTempColorChannel.AddKey(21400, 255 / 255f);
            redTempColorChannel.AddKey(21500, 166 / 255f); greenTempColorChannel.AddKey(21500, 195 / 255f); blueTempColorChannel.AddKey(21500, 255 / 255f);
            redTempColorChannel.AddKey(21600, 166 / 255f); greenTempColorChannel.AddKey(21600, 195 / 255f); blueTempColorChannel.AddKey(21600, 255 / 255f);
            redTempColorChannel.AddKey(21700, 166 / 255f); greenTempColorChannel.AddKey(21700, 195 / 255f); blueTempColorChannel.AddKey(21700, 255 / 255f);
            redTempColorChannel.AddKey(21800, 166 / 255f); greenTempColorChannel.AddKey(21800, 195 / 255f); blueTempColorChannel.AddKey(21800, 255 / 255f);
            redTempColorChannel.AddKey(21900, 166 / 255f); greenTempColorChannel.AddKey(21900, 195 / 255f); blueTempColorChannel.AddKey(21900, 255 / 255f);
            redTempColorChannel.AddKey(22000, 166 / 255f); greenTempColorChannel.AddKey(22000, 195 / 255f); blueTempColorChannel.AddKey(22000, 255 / 255f);
            redTempColorChannel.AddKey(22100, 165 / 255f); greenTempColorChannel.AddKey(22100, 195 / 255f); blueTempColorChannel.AddKey(22100, 255 / 255f);
            redTempColorChannel.AddKey(22200, 165 / 255f); greenTempColorChannel.AddKey(22200, 195 / 255f); blueTempColorChannel.AddKey(22200, 255 / 255f);
            redTempColorChannel.AddKey(22300, 165 / 255f); greenTempColorChannel.AddKey(22300, 195 / 255f); blueTempColorChannel.AddKey(22300, 255 / 255f);
            redTempColorChannel.AddKey(22400, 165 / 255f); greenTempColorChannel.AddKey(22400, 195 / 255f); blueTempColorChannel.AddKey(22400, 255 / 255f);
            redTempColorChannel.AddKey(22500, 165 / 255f); greenTempColorChannel.AddKey(22500, 195 / 255f); blueTempColorChannel.AddKey(22500, 255 / 255f);
            redTempColorChannel.AddKey(22600, 165 / 255f); greenTempColorChannel.AddKey(22600, 195 / 255f); blueTempColorChannel.AddKey(22600, 255 / 255f);
            redTempColorChannel.AddKey(22700, 165 / 255f); greenTempColorChannel.AddKey(22700, 194 / 255f); blueTempColorChannel.AddKey(22700, 255 / 255f);
            redTempColorChannel.AddKey(22800, 165 / 255f); greenTempColorChannel.AddKey(22800, 194 / 255f); blueTempColorChannel.AddKey(22800, 255 / 255f);
            redTempColorChannel.AddKey(22900, 165 / 255f); greenTempColorChannel.AddKey(22900, 194 / 255f); blueTempColorChannel.AddKey(22900, 255 / 255f);
            redTempColorChannel.AddKey(23000, 164 / 255f); greenTempColorChannel.AddKey(23000, 194 / 255f); blueTempColorChannel.AddKey(23000, 255 / 255f);
            redTempColorChannel.AddKey(23100, 164 / 255f); greenTempColorChannel.AddKey(23100, 194 / 255f); blueTempColorChannel.AddKey(23100, 255 / 255f);
            redTempColorChannel.AddKey(23200, 164 / 255f); greenTempColorChannel.AddKey(23200, 194 / 255f); blueTempColorChannel.AddKey(23200, 255 / 255f);
            redTempColorChannel.AddKey(23300, 164 / 255f); greenTempColorChannel.AddKey(23300, 194 / 255f); blueTempColorChannel.AddKey(23300, 255 / 255f);
            redTempColorChannel.AddKey(23400, 164 / 255f); greenTempColorChannel.AddKey(23400, 194 / 255f); blueTempColorChannel.AddKey(23400, 255 / 255f);
            redTempColorChannel.AddKey(23500, 164 / 255f); greenTempColorChannel.AddKey(23500, 194 / 255f); blueTempColorChannel.AddKey(23500, 255 / 255f);
            redTempColorChannel.AddKey(23600, 164 / 255f); greenTempColorChannel.AddKey(23600, 194 / 255f); blueTempColorChannel.AddKey(23600, 255 / 255f);
            redTempColorChannel.AddKey(23700, 164 / 255f); greenTempColorChannel.AddKey(23700, 194 / 255f); blueTempColorChannel.AddKey(23700, 255 / 255f);
            redTempColorChannel.AddKey(23800, 164 / 255f); greenTempColorChannel.AddKey(23800, 194 / 255f); blueTempColorChannel.AddKey(23800, 255 / 255f);
            redTempColorChannel.AddKey(23900, 164 / 255f); greenTempColorChannel.AddKey(23900, 194 / 255f); blueTempColorChannel.AddKey(23900, 255 / 255f);
            redTempColorChannel.AddKey(24000, 163 / 255f); greenTempColorChannel.AddKey(24000, 194 / 255f); blueTempColorChannel.AddKey(24000, 255 / 255f);
            redTempColorChannel.AddKey(24100, 163 / 255f); greenTempColorChannel.AddKey(24100, 194 / 255f); blueTempColorChannel.AddKey(24100, 255 / 255f);
            redTempColorChannel.AddKey(24200, 163 / 255f); greenTempColorChannel.AddKey(24200, 193 / 255f); blueTempColorChannel.AddKey(24200, 255 / 255f);
            redTempColorChannel.AddKey(24300, 163 / 255f); greenTempColorChannel.AddKey(24300, 193 / 255f); blueTempColorChannel.AddKey(24300, 255 / 255f);
            redTempColorChannel.AddKey(24400, 163 / 255f); greenTempColorChannel.AddKey(24400, 193 / 255f); blueTempColorChannel.AddKey(24400, 255 / 255f);
            redTempColorChannel.AddKey(24500, 163 / 255f); greenTempColorChannel.AddKey(24500, 193 / 255f); blueTempColorChannel.AddKey(24500, 255 / 255f);
            redTempColorChannel.AddKey(24600, 163 / 255f); greenTempColorChannel.AddKey(24600, 193 / 255f); blueTempColorChannel.AddKey(24600, 255 / 255f);
            redTempColorChannel.AddKey(24700, 163 / 255f); greenTempColorChannel.AddKey(24700, 193 / 255f); blueTempColorChannel.AddKey(24700, 255 / 255f);
            redTempColorChannel.AddKey(24800, 163 / 255f); greenTempColorChannel.AddKey(24800, 193 / 255f); blueTempColorChannel.AddKey(24800, 255 / 255f);
            redTempColorChannel.AddKey(24900, 163 / 255f); greenTempColorChannel.AddKey(24900, 193 / 255f); blueTempColorChannel.AddKey(24900, 255 / 255f);
            redTempColorChannel.AddKey(25000, 163 / 255f); greenTempColorChannel.AddKey(25000, 193 / 255f); blueTempColorChannel.AddKey(25000, 255 / 255f);
            redTempColorChannel.AddKey(25100, 162 / 255f); greenTempColorChannel.AddKey(25100, 193 / 255f); blueTempColorChannel.AddKey(25100, 255 / 255f);
            redTempColorChannel.AddKey(25200, 162 / 255f); greenTempColorChannel.AddKey(25200, 193 / 255f); blueTempColorChannel.AddKey(25200, 255 / 255f);
            redTempColorChannel.AddKey(25300, 162 / 255f); greenTempColorChannel.AddKey(25300, 193 / 255f); blueTempColorChannel.AddKey(25300, 255 / 255f);
            redTempColorChannel.AddKey(25400, 162 / 255f); greenTempColorChannel.AddKey(25400, 193 / 255f); blueTempColorChannel.AddKey(25400, 255 / 255f);
            redTempColorChannel.AddKey(25500, 162 / 255f); greenTempColorChannel.AddKey(25500, 193 / 255f); blueTempColorChannel.AddKey(25500, 255 / 255f);
            redTempColorChannel.AddKey(25600, 162 / 255f); greenTempColorChannel.AddKey(25600, 193 / 255f); blueTempColorChannel.AddKey(25600, 255 / 255f);
            redTempColorChannel.AddKey(25700, 162 / 255f); greenTempColorChannel.AddKey(25700, 193 / 255f); blueTempColorChannel.AddKey(25700, 255 / 255f);
            redTempColorChannel.AddKey(25800, 162 / 255f); greenTempColorChannel.AddKey(25800, 193 / 255f); blueTempColorChannel.AddKey(25800, 255 / 255f);
            redTempColorChannel.AddKey(25900, 162 / 255f); greenTempColorChannel.AddKey(25900, 192 / 255f); blueTempColorChannel.AddKey(25900, 255 / 255f);
            redTempColorChannel.AddKey(26000, 162 / 255f); greenTempColorChannel.AddKey(26000, 192 / 255f); blueTempColorChannel.AddKey(26000, 255 / 255f);
            redTempColorChannel.AddKey(26100, 162 / 255f); greenTempColorChannel.AddKey(26100, 192 / 255f); blueTempColorChannel.AddKey(26100, 255 / 255f);
            redTempColorChannel.AddKey(26200, 162 / 255f); greenTempColorChannel.AddKey(26200, 192 / 255f); blueTempColorChannel.AddKey(26200, 255 / 255f);
            redTempColorChannel.AddKey(26300, 162 / 255f); greenTempColorChannel.AddKey(26300, 192 / 255f); blueTempColorChannel.AddKey(26300, 255 / 255f);
            redTempColorChannel.AddKey(26400, 161 / 255f); greenTempColorChannel.AddKey(26400, 192 / 255f); blueTempColorChannel.AddKey(26400, 255 / 255f);
            redTempColorChannel.AddKey(26500, 161 / 255f); greenTempColorChannel.AddKey(26500, 192 / 255f); blueTempColorChannel.AddKey(26500, 255 / 255f);
            redTempColorChannel.AddKey(26600, 161 / 255f); greenTempColorChannel.AddKey(26600, 192 / 255f); blueTempColorChannel.AddKey(26600, 255 / 255f);
            redTempColorChannel.AddKey(26700, 161 / 255f); greenTempColorChannel.AddKey(26700, 192 / 255f); blueTempColorChannel.AddKey(26700, 255 / 255f);
            redTempColorChannel.AddKey(26800, 161 / 255f); greenTempColorChannel.AddKey(26800, 192 / 255f); blueTempColorChannel.AddKey(26800, 255 / 255f);
            redTempColorChannel.AddKey(26900, 161 / 255f); greenTempColorChannel.AddKey(26900, 192 / 255f); blueTempColorChannel.AddKey(26900, 255 / 255f);
            redTempColorChannel.AddKey(27000, 161 / 255f); greenTempColorChannel.AddKey(27000, 192 / 255f); blueTempColorChannel.AddKey(27000, 255 / 255f);
            redTempColorChannel.AddKey(27100, 161 / 255f); greenTempColorChannel.AddKey(27100, 192 / 255f); blueTempColorChannel.AddKey(27100, 255 / 255f);
            redTempColorChannel.AddKey(27200, 161 / 255f); greenTempColorChannel.AddKey(27200, 192 / 255f); blueTempColorChannel.AddKey(27200, 255 / 255f);
            redTempColorChannel.AddKey(27300, 161 / 255f); greenTempColorChannel.AddKey(27300, 192 / 255f); blueTempColorChannel.AddKey(27300, 255 / 255f);
            redTempColorChannel.AddKey(27400, 161 / 255f); greenTempColorChannel.AddKey(27400, 192 / 255f); blueTempColorChannel.AddKey(27400, 255 / 255f);
            redTempColorChannel.AddKey(27500, 161 / 255f); greenTempColorChannel.AddKey(27500, 192 / 255f); blueTempColorChannel.AddKey(27500, 255 / 255f);
            redTempColorChannel.AddKey(27600, 161 / 255f); greenTempColorChannel.AddKey(27600, 192 / 255f); blueTempColorChannel.AddKey(27600, 255 / 255f);
            redTempColorChannel.AddKey(27700, 161 / 255f); greenTempColorChannel.AddKey(27700, 192 / 255f); blueTempColorChannel.AddKey(27700, 255 / 255f);
            redTempColorChannel.AddKey(27800, 160 / 255f); greenTempColorChannel.AddKey(27800, 192 / 255f); blueTempColorChannel.AddKey(27800, 255 / 255f);
            redTempColorChannel.AddKey(27900, 160 / 255f); greenTempColorChannel.AddKey(27900, 192 / 255f); blueTempColorChannel.AddKey(27900, 255 / 255f);
            redTempColorChannel.AddKey(28000, 160 / 255f); greenTempColorChannel.AddKey(28000, 191 / 255f); blueTempColorChannel.AddKey(28000, 255 / 255f);
            redTempColorChannel.AddKey(28100, 160 / 255f); greenTempColorChannel.AddKey(28100, 191 / 255f); blueTempColorChannel.AddKey(28100, 255 / 255f);
            redTempColorChannel.AddKey(28200, 160 / 255f); greenTempColorChannel.AddKey(28200, 191 / 255f); blueTempColorChannel.AddKey(28200, 255 / 255f);
            redTempColorChannel.AddKey(28300, 160 / 255f); greenTempColorChannel.AddKey(28300, 191 / 255f); blueTempColorChannel.AddKey(28300, 255 / 255f);
            redTempColorChannel.AddKey(28400, 160 / 255f); greenTempColorChannel.AddKey(28400, 191 / 255f); blueTempColorChannel.AddKey(28400, 255 / 255f);
            redTempColorChannel.AddKey(28500, 160 / 255f); greenTempColorChannel.AddKey(28500, 191 / 255f); blueTempColorChannel.AddKey(28500, 255 / 255f);
            redTempColorChannel.AddKey(28600, 160 / 255f); greenTempColorChannel.AddKey(28600, 191 / 255f); blueTempColorChannel.AddKey(28600, 255 / 255f);
            redTempColorChannel.AddKey(28700, 160 / 255f); greenTempColorChannel.AddKey(28700, 191 / 255f); blueTempColorChannel.AddKey(28700, 255 / 255f);
            redTempColorChannel.AddKey(28800, 160 / 255f); greenTempColorChannel.AddKey(28800, 191 / 255f); blueTempColorChannel.AddKey(28800, 255 / 255f);
            redTempColorChannel.AddKey(28900, 160 / 255f); greenTempColorChannel.AddKey(28900, 191 / 255f); blueTempColorChannel.AddKey(28900, 255 / 255f);
            redTempColorChannel.AddKey(29000, 160 / 255f); greenTempColorChannel.AddKey(29000, 191 / 255f); blueTempColorChannel.AddKey(29000, 255 / 255f);
            redTempColorChannel.AddKey(29100, 160 / 255f); greenTempColorChannel.AddKey(29100, 191 / 255f); blueTempColorChannel.AddKey(29100, 255 / 255f);
            redTempColorChannel.AddKey(29200, 160 / 255f); greenTempColorChannel.AddKey(29200, 191 / 255f); blueTempColorChannel.AddKey(29200, 255 / 255f);
            redTempColorChannel.AddKey(29300, 159 / 255f); greenTempColorChannel.AddKey(29300, 191 / 255f); blueTempColorChannel.AddKey(29300, 255 / 255f);
            redTempColorChannel.AddKey(29400, 159 / 255f); greenTempColorChannel.AddKey(29400, 191 / 255f); blueTempColorChannel.AddKey(29400, 255 / 255f);
            redTempColorChannel.AddKey(29500, 159 / 255f); greenTempColorChannel.AddKey(29500, 191 / 255f); blueTempColorChannel.AddKey(29500, 255 / 255f);
            redTempColorChannel.AddKey(29600, 159 / 255f); greenTempColorChannel.AddKey(29600, 191 / 255f); blueTempColorChannel.AddKey(29600, 255 / 255f);
            redTempColorChannel.AddKey(29700, 159 / 255f); greenTempColorChannel.AddKey(29700, 191 / 255f); blueTempColorChannel.AddKey(29700, 255 / 255f);
            redTempColorChannel.AddKey(29800, 159 / 255f); greenTempColorChannel.AddKey(29800, 191 / 255f); blueTempColorChannel.AddKey(29800, 255 / 255f);
            redTempColorChannel.AddKey(29900, 159 / 255f); greenTempColorChannel.AddKey(29900, 191 / 255f); blueTempColorChannel.AddKey(29900, 255 / 255f);
            redTempColorChannel.AddKey(30000, 159 / 255f); greenTempColorChannel.AddKey(30000, 191 / 255f); blueTempColorChannel.AddKey(30000, 255 / 255f);
            redTempColorChannel.AddKey(30100, 159 / 255f); greenTempColorChannel.AddKey(30100, 191 / 255f); blueTempColorChannel.AddKey(30100, 255 / 255f);
            redTempColorChannel.AddKey(30200, 159 / 255f); greenTempColorChannel.AddKey(30200, 191 / 255f); blueTempColorChannel.AddKey(30200, 255 / 255f);
            redTempColorChannel.AddKey(30300, 159 / 255f); greenTempColorChannel.AddKey(30300, 191 / 255f); blueTempColorChannel.AddKey(30300, 255 / 255f);
            redTempColorChannel.AddKey(30400, 159 / 255f); greenTempColorChannel.AddKey(30400, 190 / 255f); blueTempColorChannel.AddKey(30400, 255 / 255f);
            redTempColorChannel.AddKey(30500, 159 / 255f); greenTempColorChannel.AddKey(30500, 190 / 255f); blueTempColorChannel.AddKey(30500, 255 / 255f);
            redTempColorChannel.AddKey(30600, 159 / 255f); greenTempColorChannel.AddKey(30600, 190 / 255f); blueTempColorChannel.AddKey(30600, 255 / 255f);
            redTempColorChannel.AddKey(30700, 159 / 255f); greenTempColorChannel.AddKey(30700, 190 / 255f); blueTempColorChannel.AddKey(30700, 255 / 255f);
            redTempColorChannel.AddKey(30800, 159 / 255f); greenTempColorChannel.AddKey(30800, 190 / 255f); blueTempColorChannel.AddKey(30800, 255 / 255f);
            redTempColorChannel.AddKey(30900, 159 / 255f); greenTempColorChannel.AddKey(30900, 190 / 255f); blueTempColorChannel.AddKey(30900, 255 / 255f);
            redTempColorChannel.AddKey(31000, 159 / 255f); greenTempColorChannel.AddKey(31000, 190 / 255f); blueTempColorChannel.AddKey(31000, 255 / 255f);
            redTempColorChannel.AddKey(31100, 158 / 255f); greenTempColorChannel.AddKey(31100, 190 / 255f); blueTempColorChannel.AddKey(31100, 255 / 255f);
            redTempColorChannel.AddKey(31200, 158 / 255f); greenTempColorChannel.AddKey(31200, 190 / 255f); blueTempColorChannel.AddKey(31200, 255 / 255f);
            redTempColorChannel.AddKey(31300, 158 / 255f); greenTempColorChannel.AddKey(31300, 190 / 255f); blueTempColorChannel.AddKey(31300, 255 / 255f);
            redTempColorChannel.AddKey(31400, 158 / 255f); greenTempColorChannel.AddKey(31400, 190 / 255f); blueTempColorChannel.AddKey(31400, 255 / 255f);
            redTempColorChannel.AddKey(31500, 158 / 255f); greenTempColorChannel.AddKey(31500, 190 / 255f); blueTempColorChannel.AddKey(31500, 255 / 255f);
            redTempColorChannel.AddKey(31600, 158 / 255f); greenTempColorChannel.AddKey(31600, 190 / 255f); blueTempColorChannel.AddKey(31600, 255 / 255f);
            redTempColorChannel.AddKey(31700, 158 / 255f); greenTempColorChannel.AddKey(31700, 190 / 255f); blueTempColorChannel.AddKey(31700, 255 / 255f);
            redTempColorChannel.AddKey(31800, 158 / 255f); greenTempColorChannel.AddKey(31800, 190 / 255f); blueTempColorChannel.AddKey(31800, 255 / 255f);
            redTempColorChannel.AddKey(31900, 158 / 255f); greenTempColorChannel.AddKey(31900, 190 / 255f); blueTempColorChannel.AddKey(31900, 255 / 255f);
            redTempColorChannel.AddKey(32000, 158 / 255f); greenTempColorChannel.AddKey(32000, 190 / 255f); blueTempColorChannel.AddKey(32000, 255 / 255f);
            redTempColorChannel.AddKey(32100, 158 / 255f); greenTempColorChannel.AddKey(32100, 190 / 255f); blueTempColorChannel.AddKey(32100, 255 / 255f);
            redTempColorChannel.AddKey(32200, 158 / 255f); greenTempColorChannel.AddKey(32200, 190 / 255f); blueTempColorChannel.AddKey(32200, 255 / 255f);
            redTempColorChannel.AddKey(32300, 158 / 255f); greenTempColorChannel.AddKey(32300, 190 / 255f); blueTempColorChannel.AddKey(32300, 255 / 255f);
            redTempColorChannel.AddKey(32400, 158 / 255f); greenTempColorChannel.AddKey(32400, 190 / 255f); blueTempColorChannel.AddKey(32400, 255 / 255f);
            redTempColorChannel.AddKey(32500, 158 / 255f); greenTempColorChannel.AddKey(32500, 190 / 255f); blueTempColorChannel.AddKey(32500, 255 / 255f);
            redTempColorChannel.AddKey(32600, 158 / 255f); greenTempColorChannel.AddKey(32600, 190 / 255f); blueTempColorChannel.AddKey(32600, 255 / 255f);
            redTempColorChannel.AddKey(32700, 158 / 255f); greenTempColorChannel.AddKey(32700, 190 / 255f); blueTempColorChannel.AddKey(32700, 255 / 255f);
            redTempColorChannel.AddKey(32800, 158 / 255f); greenTempColorChannel.AddKey(32800, 190 / 255f); blueTempColorChannel.AddKey(32800, 255 / 255f);
            redTempColorChannel.AddKey(32900, 158 / 255f); greenTempColorChannel.AddKey(32900, 190 / 255f); blueTempColorChannel.AddKey(32900, 255 / 255f);
            redTempColorChannel.AddKey(33000, 158 / 255f); greenTempColorChannel.AddKey(33000, 190 / 255f); blueTempColorChannel.AddKey(33000, 255 / 255f);
            redTempColorChannel.AddKey(33100, 158 / 255f); greenTempColorChannel.AddKey(33100, 190 / 255f); blueTempColorChannel.AddKey(33100, 255 / 255f);
            redTempColorChannel.AddKey(33200, 157 / 255f); greenTempColorChannel.AddKey(33200, 190 / 255f); blueTempColorChannel.AddKey(33200, 255 / 255f);
            redTempColorChannel.AddKey(33300, 157 / 255f); greenTempColorChannel.AddKey(33300, 190 / 255f); blueTempColorChannel.AddKey(33300, 255 / 255f);
            redTempColorChannel.AddKey(33400, 157 / 255f); greenTempColorChannel.AddKey(33400, 189 / 255f); blueTempColorChannel.AddKey(33400, 255 / 255f);
            redTempColorChannel.AddKey(33500, 157 / 255f); greenTempColorChannel.AddKey(33500, 189 / 255f); blueTempColorChannel.AddKey(33500, 255 / 255f);
            redTempColorChannel.AddKey(33600, 157 / 255f); greenTempColorChannel.AddKey(33600, 189 / 255f); blueTempColorChannel.AddKey(33600, 255 / 255f);
            redTempColorChannel.AddKey(33700, 157 / 255f); greenTempColorChannel.AddKey(33700, 189 / 255f); blueTempColorChannel.AddKey(33700, 255 / 255f);
            redTempColorChannel.AddKey(33800, 157 / 255f); greenTempColorChannel.AddKey(33800, 189 / 255f); blueTempColorChannel.AddKey(33800, 255 / 255f);
            redTempColorChannel.AddKey(33900, 157 / 255f); greenTempColorChannel.AddKey(33900, 189 / 255f); blueTempColorChannel.AddKey(33900, 255 / 255f);
            redTempColorChannel.AddKey(34000, 157 / 255f); greenTempColorChannel.AddKey(34000, 189 / 255f); blueTempColorChannel.AddKey(34000, 255 / 255f);
            redTempColorChannel.AddKey(34100, 157 / 255f); greenTempColorChannel.AddKey(34100, 189 / 255f); blueTempColorChannel.AddKey(34100, 255 / 255f);
            redTempColorChannel.AddKey(34200, 157 / 255f); greenTempColorChannel.AddKey(34200, 189 / 255f); blueTempColorChannel.AddKey(34200, 255 / 255f);
            redTempColorChannel.AddKey(34300, 157 / 255f); greenTempColorChannel.AddKey(34300, 189 / 255f); blueTempColorChannel.AddKey(34300, 255 / 255f);
            redTempColorChannel.AddKey(34400, 157 / 255f); greenTempColorChannel.AddKey(34400, 189 / 255f); blueTempColorChannel.AddKey(34400, 255 / 255f);
            redTempColorChannel.AddKey(34500, 157 / 255f); greenTempColorChannel.AddKey(34500, 189 / 255f); blueTempColorChannel.AddKey(34500, 255 / 255f);
            redTempColorChannel.AddKey(34600, 157 / 255f); greenTempColorChannel.AddKey(34600, 189 / 255f); blueTempColorChannel.AddKey(34600, 255 / 255f);
            redTempColorChannel.AddKey(34700, 157 / 255f); greenTempColorChannel.AddKey(34700, 189 / 255f); blueTempColorChannel.AddKey(34700, 255 / 255f);
            redTempColorChannel.AddKey(34800, 157 / 255f); greenTempColorChannel.AddKey(34800, 189 / 255f); blueTempColorChannel.AddKey(34800, 255 / 255f);
            redTempColorChannel.AddKey(34900, 157 / 255f); greenTempColorChannel.AddKey(34900, 189 / 255f); blueTempColorChannel.AddKey(34900, 255 / 255f);
            redTempColorChannel.AddKey(35000, 157 / 255f); greenTempColorChannel.AddKey(35000, 189 / 255f); blueTempColorChannel.AddKey(35000, 255 / 255f);
            redTempColorChannel.AddKey(35100, 157 / 255f); greenTempColorChannel.AddKey(35100, 189 / 255f); blueTempColorChannel.AddKey(35100, 255 / 255f);
            redTempColorChannel.AddKey(35200, 157 / 255f); greenTempColorChannel.AddKey(35200, 189 / 255f); blueTempColorChannel.AddKey(35200, 255 / 255f);
            redTempColorChannel.AddKey(35300, 157 / 255f); greenTempColorChannel.AddKey(35300, 189 / 255f); blueTempColorChannel.AddKey(35300, 255 / 255f);
            redTempColorChannel.AddKey(35400, 157 / 255f); greenTempColorChannel.AddKey(35400, 189 / 255f); blueTempColorChannel.AddKey(35400, 255 / 255f);
            redTempColorChannel.AddKey(35500, 157 / 255f); greenTempColorChannel.AddKey(35500, 189 / 255f); blueTempColorChannel.AddKey(35500, 255 / 255f);
            redTempColorChannel.AddKey(35600, 156 / 255f); greenTempColorChannel.AddKey(35600, 189 / 255f); blueTempColorChannel.AddKey(35600, 255 / 255f);
            redTempColorChannel.AddKey(35700, 156 / 255f); greenTempColorChannel.AddKey(35700, 189 / 255f); blueTempColorChannel.AddKey(35700, 255 / 255f);
            redTempColorChannel.AddKey(35800, 156 / 255f); greenTempColorChannel.AddKey(35800, 189 / 255f); blueTempColorChannel.AddKey(35800, 255 / 255f);
            redTempColorChannel.AddKey(35900, 156 / 255f); greenTempColorChannel.AddKey(35900, 189 / 255f); blueTempColorChannel.AddKey(35900, 255 / 255f);
            redTempColorChannel.AddKey(36000, 156 / 255f); greenTempColorChannel.AddKey(36000, 189 / 255f); blueTempColorChannel.AddKey(36000, 255 / 255f);
            redTempColorChannel.AddKey(36100, 156 / 255f); greenTempColorChannel.AddKey(36100, 189 / 255f); blueTempColorChannel.AddKey(36100, 255 / 255f);
            redTempColorChannel.AddKey(36200, 156 / 255f); greenTempColorChannel.AddKey(36200, 189 / 255f); blueTempColorChannel.AddKey(36200, 255 / 255f);
            redTempColorChannel.AddKey(36300, 156 / 255f); greenTempColorChannel.AddKey(36300, 189 / 255f); blueTempColorChannel.AddKey(36300, 255 / 255f);
            redTempColorChannel.AddKey(36400, 156 / 255f); greenTempColorChannel.AddKey(36400, 189 / 255f); blueTempColorChannel.AddKey(36400, 255 / 255f);
            redTempColorChannel.AddKey(36500, 156 / 255f); greenTempColorChannel.AddKey(36500, 189 / 255f); blueTempColorChannel.AddKey(36500, 255 / 255f);
            redTempColorChannel.AddKey(36600, 156 / 255f); greenTempColorChannel.AddKey(36600, 189 / 255f); blueTempColorChannel.AddKey(36600, 255 / 255f);
            redTempColorChannel.AddKey(36700, 156 / 255f); greenTempColorChannel.AddKey(36700, 189 / 255f); blueTempColorChannel.AddKey(36700, 255 / 255f);
            redTempColorChannel.AddKey(36800, 156 / 255f); greenTempColorChannel.AddKey(36800, 189 / 255f); blueTempColorChannel.AddKey(36800, 255 / 255f);
            redTempColorChannel.AddKey(36900, 156 / 255f); greenTempColorChannel.AddKey(36900, 189 / 255f); blueTempColorChannel.AddKey(36900, 255 / 255f);
            redTempColorChannel.AddKey(37000, 156 / 255f); greenTempColorChannel.AddKey(37000, 189 / 255f); blueTempColorChannel.AddKey(37000, 255 / 255f);
            redTempColorChannel.AddKey(37100, 156 / 255f); greenTempColorChannel.AddKey(37100, 189 / 255f); blueTempColorChannel.AddKey(37100, 255 / 255f);
            redTempColorChannel.AddKey(37200, 156 / 255f); greenTempColorChannel.AddKey(37200, 188 / 255f); blueTempColorChannel.AddKey(37200, 255 / 255f);
            redTempColorChannel.AddKey(37300, 156 / 255f); greenTempColorChannel.AddKey(37300, 188 / 255f); blueTempColorChannel.AddKey(37300, 255 / 255f);
            redTempColorChannel.AddKey(37400, 156 / 255f); greenTempColorChannel.AddKey(37400, 188 / 255f); blueTempColorChannel.AddKey(37400, 255 / 255f);
            redTempColorChannel.AddKey(37500, 156 / 255f); greenTempColorChannel.AddKey(37500, 188 / 255f); blueTempColorChannel.AddKey(37500, 255 / 255f);
            redTempColorChannel.AddKey(37600, 156 / 255f); greenTempColorChannel.AddKey(37600, 188 / 255f); blueTempColorChannel.AddKey(37600, 255 / 255f);
            redTempColorChannel.AddKey(37700, 156 / 255f); greenTempColorChannel.AddKey(37700, 188 / 255f); blueTempColorChannel.AddKey(37700, 255 / 255f);
            redTempColorChannel.AddKey(37800, 156 / 255f); greenTempColorChannel.AddKey(37800, 188 / 255f); blueTempColorChannel.AddKey(37800, 255 / 255f);
            redTempColorChannel.AddKey(37900, 156 / 255f); greenTempColorChannel.AddKey(37900, 188 / 255f); blueTempColorChannel.AddKey(37900, 255 / 255f);
            redTempColorChannel.AddKey(38000, 156 / 255f); greenTempColorChannel.AddKey(38000, 188 / 255f); blueTempColorChannel.AddKey(38000, 255 / 255f);
            redTempColorChannel.AddKey(38100, 156 / 255f); greenTempColorChannel.AddKey(38100, 188 / 255f); blueTempColorChannel.AddKey(38100, 255 / 255f);
            redTempColorChannel.AddKey(38200, 156 / 255f); greenTempColorChannel.AddKey(38200, 188 / 255f); blueTempColorChannel.AddKey(38200, 255 / 255f);
            redTempColorChannel.AddKey(38300, 156 / 255f); greenTempColorChannel.AddKey(38300, 188 / 255f); blueTempColorChannel.AddKey(38300, 255 / 255f);

            for (var i = 0; i < redTempColorChannel.keys.Length; i++)
            {
                redTempColorChannel.SmoothTangents(i, 0);
            }
            for (var i = 0; i < greenTempColorChannel.keys.Length; i++)
            {
                greenTempColorChannel.SmoothTangents(i, 0);
            }
            for (var i = 0; i < blueTempColorChannel.keys.Length; i++)
            {
                blueTempColorChannel.SmoothTangents(i, 0);
            }
        }

        private static List<FNRadiator> GetRadiatorsForVessel(Vessel vessel)
        {
            if (RadiatorsByVessel.TryGetValue(vessel, out var vesselRadiator))
                return vesselRadiator;

            vesselRadiator = vessel.FindPartModulesImplementing<FNRadiator>().ToList();
            RadiatorsByVessel.Add(vessel, vesselRadiator);

            return vesselRadiator;
        }

        private double GetMaximumTemperatureForGen(GenerationType generationType)
        {
            var generation = (int)generationType;

            if (generation >= (int)GenerationType.Mk6 && IsGraphene)
                return RadiatorProperties.RadiatorTemperatureMk6;
            if (generation >= (int)GenerationType.Mk5 && IsGraphene)
                return RadiatorProperties.RadiatorTemperatureMk5;
            if (generation >= (int)GenerationType.Mk4)
                return RadiatorProperties.RadiatorTemperatureMk4;
            if (generation >= (int)GenerationType.Mk3)
                return RadiatorProperties.RadiatorTemperatureMk3;
            if (generation >= (int)GenerationType.Mk2)
                return RadiatorProperties.RadiatorTemperatureMk2;
            else
                return RadiatorProperties.RadiatorTemperatureMk1;
        }

        public double BaseRadiatorArea => radiatorArea * _upgradeModifier * _attachedPartsModifier;

        public double EffectiveRadiatorArea => BaseRadiatorArea * areaMultiplier * PluginSettings.Config.RadiatorAreaMultiplier;

        private void DetermineGenerationType()
        {
            // check if we have SurfaceAreaUpgradeTechReq
            hasSurfaceAreaUpgradeTechReq = PluginHelper.UpgradeAvailable(surfaceAreaUpgradeTechReq);

            _upgradeModifier = hasSurfaceAreaUpgradeTechReq ? surfaceAreaUpgradeMult : 1;

            // determine number of upgrade techs
            nrAvailableUpgradeTechs = 1;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech5))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech4))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech2))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(RadiatorProperties.RadiatorUpgradeTech1))
                nrAvailableUpgradeTechs++;

            // determine tech levels
            if (nrAvailableUpgradeTechs == 6)
                CurrentGenerationType = GenerationType.Mk6;
            else if (nrAvailableUpgradeTechs == 5)
                CurrentGenerationType = GenerationType.Mk5;
            else if (nrAvailableUpgradeTechs == 4)
                CurrentGenerationType = GenerationType.Mk4;
            else if (nrAvailableUpgradeTechs == 3)
                CurrentGenerationType = GenerationType.Mk3;
            else if (nrAvailableUpgradeTechs == 2)
                CurrentGenerationType = GenerationType.Mk2;
            else
                CurrentGenerationType = GenerationType.Mk1;

            maxRadiatorTemperature = GetMaximumTemperatureForGen(CurrentGenerationType);
        }

        private string RadiatorType
        {
            get
            {
                if (CurrentGenerationType == GenerationType.Mk6)
                    return radiatorTypeMk6;
                if (CurrentGenerationType == GenerationType.Mk5)
                    return radiatorTypeMk5;
                if (CurrentGenerationType == GenerationType.Mk4)
                    return radiatorTypeMk4;
                if (CurrentGenerationType == GenerationType.Mk3)
                    return radiatorTypeMk3;
                if (CurrentGenerationType == GenerationType.Mk2)
                    return radiatorTypeMk2;
                return radiatorTypeMk1;
            }
        }

        public static void Reset()
        {
            RadiatorsByVessel.Clear();
        }

        public static bool HasRadiatorsForVessel(Vessel vess)
        {
            return GetRadiatorsForVessel(vess).Any();
        }

        public static double GetCurrentRadiatorTemperatureForVessel(Vessel vess)
        {
            var vesselRadiators = GetRadiatorsForVessel(vess);

            return vesselRadiators.Average(m => m.CurrentRadiatorTemperature);
        }

        public static double GetAverageRadiatorTemperatureForVessel(Vessel vess)
        {
            var radiatorVessel = GetRadiatorsForVessel(vess);

            if (!radiatorVessel.Any())
                return _maximumRadiatorTempInSpace;

            if (radiatorVessel.Any())
            {
                var maxRadiatorTemperature = radiatorVessel.Max(r => r.MaxRadiatorTemperature);
                var totalRadiatorsMass = radiatorVessel.Sum(r => (double)(decimal)r.part.mass);

                return radiatorVessel.Sum(r => Math.Min(1, r.GetAverageRadiatorTemperature() / r.MaxRadiatorTemperature) * maxRadiatorTemperature * (r.part.mass / totalRadiatorsMass));
            }
            else
                return _maximumRadiatorTempInSpace;
        }

        public static double GetAverageMaximumRadiatorTemperatureForVessel(Vessel vess)
        {
            var radiatorVessel = GetRadiatorsForVessel(vess);

            double averageTemp = 0;
            float nRadiators = 0;

            foreach (FNRadiator radiator in radiatorVessel)
            {
                if (radiator == null) continue;

                averageTemp += radiator.maxRadiatorTemperature;
                nRadiators += 1;
            }

            return nRadiators > 0 ? averageTemp / nRadiators : 0.0f;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_DeployRadiator", active = true)]//Deploy Radiator
        public void DeployRadiator()
        {
            isAutomated = false;

            Deploy();
        }

        private void Deploy()
        {
            if (preventShieldedDeploy && (part.ShieldedFromAirstream || _radiatorDeployDelay < RADIATOR_DELAY))
            {
                return;
            }

            if (_moduleDeployableRadiator != null)
                _moduleDeployableRadiator.Extend();

            ActivateRadiator();

            if (_deployAnimation == null) return;

            _deployAnimation[animName].enabled = true;
            _deployAnimation[animName].speed = 0.5f;
            _deployAnimation[animName].normalizedTime = 0f;
            _deployAnimation.Blend(animName, 2);
        }

        private void ActivateRadiator()
        {
            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Activate();

            radiatorIsEnabled = true;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_RetractRadiator", active = true)]//Retract Radiator
        public void RetractRadiator()
        {
            if (!isDeployable) return;

            isAutomated = false;

            Retract();
        }

        private void Retract()
        {
            Debug.Log("[KSPI]: Retract Called ");

            if (_moduleDeployableRadiator != null)
            {
                _moduleDeployableRadiator.hasPivot = true;
                _moduleDeployableRadiator.Retract();
            }

            DeactivateRadiator();

            if (_deployAnimation == null) return;

            _deployAnimation[animName].enabled = true;
            _deployAnimation[animName].speed = -0.5f;
            _deployAnimation[animName].normalizedTime = 1;
            _deployAnimation.Blend(animName, 2);
        }

        private void DeactivateRadiator()
        {
            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Shutdown();

            radiatorIsEnabled = false;
        }

        [KSPAction("Deploy Radiator")]
        public void DeployRadiatorAction(KSPActionParam param)
        {
            Debug.Log("[KSPI]: DeployRadiatorAction Called ");
            DeployRadiator();
        }

        [KSPAction("Retract Radiator")]
        public void RetractRadiatorAction(KSPActionParam param)
        {
            RetractRadiator();
        }

        [KSPAction("Toggle Radiator")]
        public void ToggleRadiatorAction(KSPActionParam param)
        {
            if (radiatorIsEnabled)
                RetractRadiator();
            else
            {
                Debug.Log("[KSPI]: ToggleRadiatorAction Called ");
                DeployRadiator();
            }
        }

        public bool MeshRadiatorSize(out double size)
        {
            size = 0;

            var mf = part.FindModelComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.Log("MeshRadiatorSize: Cannot find a MeshFilter in GetComponent");
                return false;
            }
            if (mf.mesh == null)
            {
                Debug.Log("MeshRadiatorSize: Cannot find a Mesh");
                return false;
            }

            var triangles = mf.mesh.triangles;
            var vertices = mf.mesh.vertices;

            double totalArea = 0.0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 corner = vertices[triangles[i]];
                Vector3 a = vertices[triangles[i + 1]] - corner;
                Vector3 b = vertices[triangles[i + 2]] - corner;

                totalArea += Vector3.Cross(a, b).magnitude;
            }

            if (totalArea.IsInfinityOrNaNorZero() == true)
            {
                Debug.Log("MeshRadiatorSize: total_area is IsInfinityOrNaNorZero :(");
                return false;
            }

            // convert from Mesh size to in game size. rescaleFactor changes when TweakScale modifies the size of the part.
            size = (totalArea / 2.0) * part.rescaleFactor;

            Debug.Log($"MeshRadiatorSize: surface_area is {size}, rescale_factor is {part.rescaleFactor}, and scale_factor is {part.scaleFactor}. Total radiator size is {size}");

            return true;
        }

        protected virtual double ExternalTemp()
        {
            // subclass may override, if needed
            return vessel == null ? PhysicsGlobals.SpaceTemperature : vessel.externalTemperature;
        }

        protected void UpdateRadiatorArea()
        {
            effectiveRadiatorArea = EffectiveRadiatorArea;
            _stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;
        }

        public override void OnStart(StartState state)
        {
            string[] resourcesToSupply = { ResourceSettings.Config.WasteHeatInMegawatt };
            this.resources_to_supply = resourcesToSupply;

            base.OnStart(state);

            _radiatedThermalPower = 0;
            _convectedThermalPower = 0;
            CurrentRadiatorTemperature = 0;
            _radiatorDeployDelay = 0;

            DetermineGenerationType();
            InitializeRadiatorAreaWhenMissing();
            UpdateAttachedPartsModifier();
            UpdateRadiatorArea();

            IsGraphene = !string.IsNullOrEmpty(surfaceAreaUpgradeTechReq);
            _maximumRadiatorTempInSpace = RadiatorProperties.RadiatorTemperatureMk6;
            _maxSpaceTempBonus = _maximumRadiatorTempInSpace - maximumRadiatorTempAtOneAtmosphere;
            _temperatureRange = _maximumRadiatorTempInSpace - drapperPoint;

            _kspShader = Shader.Find(kspShaderLocation);

            part.heatConvectiveConstant = convectiveBonus;
            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            _deployRadiatorEvent = Events[nameof(DeployRadiator)];
            _retractRadiatorEvent = Events[nameof(RetractRadiator)];

            _thermalPowerConvStrField = Fields[nameof(thermalPowerConvStr)];
            _radiatorIsEnabledField = Fields[nameof(radiatorIsEnabled)];
            _isAutomatedField = Fields[nameof(isAutomated)];
            _pivotEnabledField = Fields[nameof(pivotEnabled)];

            var preventDeployField = Fields[nameof(preventShieldedDeploy)];
            preventDeployField.guiActive = isDeployable;
            preventDeployField.guiActiveEditor = isDeployable;

            Actions[nameof(DeployRadiatorAction)].guiName = Events[nameof(DeployRadiator)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_DeployRadiator");//"Deploy Radiator"
            Actions[nameof(ToggleRadiatorAction)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_ToggleRadiator");//String.Format("Toggle Radiator")
            Actions[nameof(RetractRadiatorAction)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_RetractRadiator");//"Retract Radiator"

            Events[nameof(RetractRadiator)].guiName = Localizer.Format("#LOC_KSPIE_Radiator_RetractRadiator");//"Retract Radiator"

            var myAttachedEngine = part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                partMass = part.mass;
                Fields[nameof(partMass)].guiActiveEditor = true;
                Fields[nameof(partMass)].guiActive = true;
            }

            if (!string.IsNullOrEmpty(thermalAnim))
            {
                _heatStates = PluginHelper.SetUpAnimation(thermalAnim, part);

                if (_heatStates != null)
                    SetHeatAnimationRatio(0);
            }

            _deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            if (_deployAnimation != null)
            {
                _deployAnimation[animName].layer = 1;
                _deployAnimation[animName].speed = 0;

                _deployAnimation[animName].normalizedTime = radiatorIsEnabled ? 1 : 0;
            }

            _moduleActiveRadiator = part.FindModuleImplementing<ModuleActiveRadiator>();

            if (_moduleActiveRadiator != null)
            {
                _moduleActiveRadiator.Events[nameof(_moduleActiveRadiator.Activate)].guiActive = false;
                _moduleActiveRadiator.Events[nameof(_moduleActiveRadiator.Shutdown)].guiActive = false;
            }
            _moduleDeployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            if (_moduleDeployableRadiator != null)
                _radiatorState = _moduleDeployableRadiator.deployState;

            var radiatorField = Fields[nameof(radiatorIsEnabled)];
            radiatorField.guiActive = showControls;
            radiatorField.guiActiveEditor = showControls;
            radiatorField.OnValueModified += radiatorIsEnabled_OnValueModified;

            var automatedField = Fields[nameof(isAutomated)];
            automatedField.guiActive = showControls;
            automatedField.guiActiveEditor = showControls;

            var pivotField = Fields[nameof(pivotEnabled)];
            pivotField.guiActive = showControls;
            pivotField.guiActiveEditor = showControls;

            if (_moduleActiveRadiator != null)
            {
                _maxEnergyTransfer = radiatorArea * PhysicsGlobals.StefanBoltzmanConstant * Math.Pow(MaxRadiatorTemperature, 4) * 0.001;

                _moduleActiveRadiator.maxEnergyTransfer = _maxEnergyTransfer;
                _moduleActiveRadiator.overcoolFactor = 0.20 + ((int)CurrentGenerationType * 0.025);

                if (radiatorIsEnabled)
                    _moduleActiveRadiator.Activate();
                else
                    _moduleActiveRadiator.Shutdown();
            }

            var intakeLqdDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.IntakeLiquid);
            var intakeAirDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.IntakeOxygenAir);
            var intakeAtmDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.IntakeAtmosphere);

            if (intakeLqdDefinition != null && intakeAirDefinition != null && intakeAtmDefinition != null)
            {
                _intakeLqdSpecificHeatCapacity = intakeLqdDefinition.specificHeatCapacity;
                _intakeAtmSpecificHeatCapacity = intakeAtmDefinition.specificHeatCapacity == 0 ? intakeAirDefinition.specificHeatCapacity : intakeAtmDefinition.specificHeatCapacity;
                _intakeAtmDensity = intakeAtmDefinition.density;
                _intakeLqdDensity = intakeLqdDefinition.density;
            }
            else
            {
                Debug.Log("[radiator initialization] Missing definitions for Lqd/Air/Atm :(");
                return;
            }

            if (state == StartState.Editor)
                return;

            if (isAutomated && !isDeployable)
            {
                ActivateRadiator();
            }

            for (var i = 0; i < 20; i++)
            {
                _radTempQueue.Enqueue(currentRadTemp);
            }

            InitializeTemperatureColorChannels();

            ApplyColorHeat();

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            _renderArray = part.FindModelComponents<Renderer>().ToArray();

            if (radiatorInit == false)
                radiatorInit = true;

            radiatorTempStr = maxRadiatorTemperature + "K";

            maxVacuumTemperature = IsGraphene ? Math.Min(maxVacuumTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk4, maxRadiatorTemperature);
            maxAtmosphereTemperature = IsGraphene ? Math.Min(maxAtmosphereTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk3, maxRadiatorTemperature);

            UpdateMaxCurrentTemperature();

            if (keepMaxPartTempEqualToMaxRadiatorTemp)
            {
                var partSkinTemperature = Math.Min(part.skinTemperature, maxCurrentRadiatorTemperature * 0.99);
                if (double.IsNaN(partSkinTemperature) == false)
                    part.skinTemperature = partSkinTemperature;

                var partTemperature = Math.Min(part.temperature, maxCurrentRadiatorTemperature * 0.99);
                if (double.IsNaN(partTemperature) == false)
                    part.temperature = partTemperature;

                if (double.IsNaN(maxCurrentRadiatorTemperature) == false)
                {
                    part.skinMaxTemp = maxCurrentRadiatorTemperature;
                    part.maxTemp = maxCurrentRadiatorTemperature;
                }
            }

            if (maintainResourceBuffers)
            {
                _resourceBuffers = new ResourceBuffers();
                _resourceBuffers.AddConfiguration(new WasteHeatBufferConfig(wasteHeatMultiplier, 2.0e+6));
                _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, this.part.mass);
                _resourceBuffers.Init(this.part);
            }

            Fields[nameof(dynamicPressureStress)].guiActive = isDeployable;
        }

        private void UpdateAttachedPartsModifier()
        {
            var stackAttachedNodesCount = part.attachNodes.Count(m => m.attachedPart != null && m.nodeType == AttachNode.NodeType.Stack);
            var surfaceAttachedNodesCount = part.attachNodes.Count(m => m.attachedPart != null && m.nodeType == AttachNode.NodeType.Surface);
            _attachedPartsModifier = Math.Max(0, 1 - (0.2 * stackAttachedNodesCount + 0.1 * surfaceAttachedNodesCount));
        }

        private void InitializeRadiatorAreaWhenMissing()
        {
            if (radiatorArea != 0) return;

            clarifyFunction = true;

            radiatorArea = Math.PI * part.partInfo.partSize;

            if (MeshRadiatorSize(out var size))
                convectiveBonus = Math.Max(1, size / radiatorArea);
        }

        void radiatorIsEnabled_OnValueModified(object arg1)
        {
            isAutomated = false;

            if (radiatorIsEnabled)
                Deploy();
            else
                Retract();
        }

        public void Update()
        {
            partMass = part.mass;

            UpdateAttachedPartsModifier();

            baseRadiatorArea = BaseRadiatorArea;

            var isDeployStateUndefined = _moduleDeployableRadiator == null
                                         || _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDING
                                         || _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTING;

            var canBeActive = showControls && isDeployable && isDeployStateUndefined;

            _deployRadiatorEvent.active = canBeActive && !radiatorIsEnabled;
            _retractRadiatorEvent.active = canBeActive && radiatorIsEnabled;
        }

        public override void OnUpdate() // is called while in flight
        {
            _radiatorDeployDelay++;

            if (_moduleDeployableRadiator != null && (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED ||
                                                       _moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED))
            {
                if (_radiatorState != _moduleDeployableRadiator.deployState)
                {
                    part.SendMessage("GeometryPartModuleRebuildMeshData");
                }
                _radiatorState = _moduleDeployableRadiator.deployState;
            }

            _stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            oxidationModifier = 0;

            UpdateMaxCurrentTemperature();

            if (keepMaxPartTempEqualToMaxRadiatorTemp && double.IsNaN(maxCurrentRadiatorTemperature) == false)
            {
                part.skinMaxTemp = maxCurrentRadiatorTemperature;
                part.maxTemp = maxCurrentRadiatorTemperature;
            }

            _thermalPowerConvStrField.guiActive = _convectedThermalPower > 0;

            // synchronize states
            if (_moduleDeployableRadiator != null && pivotEnabled && showControls)
            {
                if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)
                    ActivateRadiator();
                else if (_moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.RETRACTED)
                    DeactivateRadiator();
            }

            _radiatorIsEnabledField.guiActive = !isPassive && showControls;
            _radiatorIsEnabledField.guiActiveEditor = !isPassive && showControls;

            _isAutomatedField.guiActive = showControls && isDeployable;
            _isAutomatedField.guiActiveEditor = showControls && isDeployable;

            _pivotEnabledField.guiActive = showControls && isDeployable;
            _pivotEnabledField.guiActiveEditor = showControls && isDeployable;

            if (radiatorIsEnabled && canRadiateHeat)
            {
                thermalPowerDissipStr = PluginHelper.getFormattedPowerString(_radiatedThermalPower);
                thermalPowerConvStr = PluginHelper.getFormattedPowerString(_convectedThermalPower);
            }
            else
            {
                thermalPowerDissipStr = Localizer.Format("#LOC_KSPIE_Radiator_disabled");//"disabled"
                thermalPowerConvStr = Localizer.Format("#LOC_KSPIE_Radiator_disabled");//"disabled"
            }

            radiatorTempStr = CurrentRadiatorTemperature.ToString("0.0") + "K / " + maxCurrentRadiatorTemperature.ToString("0.0") + "K";

            partTempStr = Math.Max(part.skinTemperature, part.temperature).ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";

            if (showColorHeat)
                ApplyColorHeat();
        }

        private void UpdateMaxCurrentTemperature()
        {
            if (vessel.mainBody.atmosphereContainsOxygen && vessel.staticPressurekPa > 0)
            {
                var combinedPressure = vessel.staticPressurekPa + vessel.dynamicPressurekPa * 0.2;

                if (combinedPressure > GameConstants.EarthAtmospherePressureAtSeaLevel)
                {
                    var extraPressure = combinedPressure - GameConstants.EarthAtmospherePressureAtSeaLevel;
                    var ratio = extraPressure / GameConstants.EarthAtmospherePressureAtSeaLevel;
                    if (ratio <= 1)
                        ratio *= ratio;
                    else
                        ratio = Math.Sqrt(ratio);
                    oxidationModifier = 1 + ratio * 0.1;
                }
                else
                    oxidationModifier = Math.Pow(combinedPressure / GameConstants.EarthAtmospherePressureAtSeaLevel, 0.25);

                spaceRadiatorModifier = Math.Max(0.25, Math.Min(0.95, 0.95 + vessel.verticalSpeed * 0.002));

                spaceRadiatorBonus = (1 / spaceRadiatorModifier) * _maxSpaceTempBonus * (1 - oxidationModifier);

                maxCurrentRadiatorTemperature = Math.Min(maxVacuumTemperature, Math.Max(PhysicsGlobals.SpaceTemperature, maxAtmosphereTemperature + spaceRadiatorBonus));
            }
            else
            {
                spaceRadiatorModifier = 0.95;
                spaceRadiatorBonus = _maxSpaceTempBonus;
                maxCurrentRadiatorTemperature = maxVacuumTemperature;
            }
        }

        public override void OnFixedUpdate()
        {
            _active = true;
            base.OnFixedUpdate();
        }

        private double PartRotationDistance()
        {
            // how much did we rotate
            var rb = part.GetComponent<Rigidbody>();
            if (rb == null)
            {
                // should not happen.
                return 0;
            }

            // rb.angularVelocity.magnitude in radians/second
            double tmp = 180 * Math.Abs(rb.angularVelocity.magnitude);
            // calculate the linear velocity
            double tmpVelocity = tmp / (Mathf.PI * (radiatorArea / Mathf.PI).Sqrt());
            // and then distance traveled.
            double distanceTraveled = effectiveRadiatorArea * tmpVelocity;

            partRotationDistance = Math.Round(distanceTraveled, 2) * Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);

            return partRotationDistance;
        }

        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;

                if (!_active)
                    base.OnFixedUpdate();

                if (_resourceBuffers != null)
                {
                    _resourceBuffers.UpdateVariable(ResourceSettings.Config.WasteHeatInMegawatt, radiatorIsEnabled ? this.part.mass : this.part.mass * 1e-3);
                    _resourceBuffers.UpdateBuffers();
                }

                // get resource bar ratio at start of frame
                if (!(getManagerForVessel(ResourceSettings.Config.WasteHeatInMegawatt) is WasteHeatResourceManager wasteheatManager))
                {
                    Debug.LogError("[KSPI]: FNRadiator: Failed to find WasteHeatResourceManager");
                    return;
                }

                if (double.IsNaN(wasteheatManager.TemperatureRatio))
                {
                    Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Double.IsNaN detected in TemperatureRatio");
                    return;
                }

                // ToDo replace wasteheatManager.SqrtResourceBarRatioBegin by ResourceBarRatioBegin after generators hot bath takes into account expected temperature
                radiator_temperature_temp_val = Math.Min(maxRadiatorTemperature * wasteheatManager.TemperatureRatio, maxCurrentRadiatorTemperature);

                deltaTemp = Math.Max(radiator_temperature_temp_val - Math.Max(ExternalTemp() * Math.Min(1, wasteheatManager.AtmosphericMultiplier), PhysicsGlobals.SpaceTemperature), 0);
                var deltaTempToPowerFour = deltaTemp * deltaTemp * deltaTemp * deltaTemp;

                if (radiatorIsEnabled)
                {
                    if (!CheatOptions.IgnoreMaxTemperature && wasteheatManager.ResourceFillFraction >= 1 && CurrentRadiatorTemperature >= maxRadiatorTemperature)
                    {
                        _explodeCounter++;
                        if (_explodeCounter > 25)
                            part.explode();
                    }
                    else
                        _explodeCounter = 0;

                    _thermalPowerDissipationPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * _stefanArea;

                    if (double.IsNaN(_thermalPowerDissipationPerSecond))
                        Debug.LogWarning("[KSPI]: FNRadiator: FixedUpdate Double.IsNaN detected in _thermalPowerDissipationPerSecond");

                    _radiatedThermalPower = canRadiateHeat ? ConsumeWasteHeatPerSecond(_thermalPowerDissipationPerSecond, wasteheatManager) : 0;

                    if (double.IsNaN(_radiatedThermalPower))
                        Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Double.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + _thermalPowerDissipationPerSecond + ")");

                    _instantaneousRadTemp = CalculateInstantaneousRadTemp();

                    CurrentRadiatorTemperature = _instantaneousRadTemp;

                    if (_moduleDeployableRadiator)
                        _moduleDeployableRadiator.hasPivot = pivotEnabled;
                }
                else
                {
                    _thermalPowerDissipationPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * _stefanArea * 0.5;

                    _radiatedThermalPower = canRadiateHeat ? ConsumeWasteHeatPerSecond(_thermalPowerDissipationPerSecond, wasteheatManager) : 0;

                    _instantaneousRadTemp = CalculateInstantaneousRadTemp();

                    CurrentRadiatorTemperature = _instantaneousRadTemp;
                }

                if (CanConvect())
                {
                    atmDensity = AtmDensity();

                    var convPowerDissipation = CalculateConvPowerDissipation(
                        radiatorSurfaceArea : radiatorArea,
                        radiatorConvectiveBonus: convectiveBonus,
                        radiatorTemperature: CurrentRadiatorTemperature,
                        externalTemperature: ExternalTemp(),
                        atmosphericDensity: atmDensity,
                        grapheneRadiatorRatio: IsGraphene ? 1 : 0,
                        submergedPortion: part.submergedPortion,
                        effectiveVesselSpeed:  vessel.speed.Sqrt(),
                        rotationModifier: PartRotationDistance().Sqrt()
                        );

                    if (!radiatorIsEnabled)
                        convPowerDissipation *= 0.2;

                    //_convectedThermalPower = canRadiateHeat && convPowerDissipation > 0 ? ConsumeWasteHeatPerSecond(convPowerDissipation, wasteheatManager) : 0;

                    _convectedThermalPower = canRadiateHeat
                        ? convPowerDissipation > 0
                            ? ConsumeWasteHeatPerSecond(convPowerDissipation, wasteheatManager)
                            : supplyManagedFNResourcePerSecond(-convPowerDissipation, ResourceSettings.Config.WasteHeatInMegawatt)
                        : 0;

                    if (_radiatorDeployDelay >= DEPLOYMENT_DELAY)
                        DeploymentControl();
                }
                else
                {
                    _convectedThermalPower = 0;
                    if (radiatorIsEnabled || !isAutomated || !canRadiateHeat || !showControls || _radiatorDeployDelay < DEPLOYMENT_DELAY) return;
                    Deploy();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception on " + part.name + " during FNRadiator.FixedUpdate with message " + e.Message);
                throw;
            }
        }

         public static double CalculateConvPowerDissipation(
            double radiatorSurfaceArea,
            double radiatorConvectiveBonus,
            double radiatorTemperature,
            double externalTemperature,
            double atmosphericDensity = 0,
            double grapheneRadiatorRatio = 0,
            double submergedPortion = 0,
            double effectiveVesselSpeed = 0,
            double rotationModifier = 0
            )
         {
             if (radiatorTemperature.IsInfinityOrNaN())
                 return 0;

            var airHeatTransferModifier = PluginSettings.Config.AirHeatTransferCoefficient * (1 - submergedPortion) * atmosphericDensity;
            var lqdHeatTransferModifier = PluginSettings.Config.LqdHeatTransferCoefficient * submergedPortion;
            var grapheneModifier = 1 - grapheneRadiatorRatio + grapheneRadiatorRatio * 0.10;
            var totalHeatTransferModifier = airHeatTransferModifier + lqdHeatTransferModifier;
            var heatTransferModifier = radiatorConvectiveBonus + Math.Max(1, effectiveVesselSpeed + rotationModifier);

            var temperatureDifference = radiatorTemperature - externalTemperature;

            // q = h * A * deltaT
            return heatTransferModifier * radiatorSurfaceArea *  temperatureDifference * PluginSettings.Config.ConvectionMultiplier * grapheneModifier * totalHeatTransferModifier;
        }

        protected virtual bool CanConvect()
        {
            return vessel.mainBody != null && vessel.mainBody.atmosphere && vessel.altitude < vessel.mainBody.atmosphereDepth;
        }

        protected virtual double AtmDensity()
        {
            // Another buff for titanium radiators - minimum of 50% effectiveness at the edge of space
            return (IsGraphene ? 1 : 1.5) - (vessel.altitude / vessel.mainBody.atmosphereDepth);
        }

        private double CalculateInstantaneousRadTemp()
        {
            var result = Math.Min(maxCurrentRadiatorTemperature, radiator_temperature_temp_val);

            if (result.IsInfinityOrNaN())
                Debug.LogError("[KSPI]: FNRadiator: FixedUpdate IsNaN or Infinity detected in CalculateInstantaneousRadTemp");

            return result;
        }

        private void DeploymentControl()
        {
            dynamicPressureStress = 4 * vessel.dynamicPressurekPa;

            if (dynamicPressureStress > 1)
            {
                if (!isDeployable || !radiatorIsEnabled) return;

                if (isAutomated)
                {
                    Debug.Log("[KSPI]: DeploymentControl Auto Retracted, stress at " + dynamicPressureStress.ToString("P2") + "%");
                    Retract();
                }
                else
                {
                    if (CheatOptions.UnbreakableJoints) return;

                    Debug.Log("[KSPI]: DeploymentControl Decoupled!");
                    part.deactivate();
                    part.decouple(1);
                }
            }
            else if (!radiatorIsEnabled && isAutomated && canRadiateHeat && showControls && (!preventShieldedDeploy || !part.ShieldedFromAirstream))
            {
                // Suppress message spam on repeated deploy attempts due to radiator delay
                //if (radiator_deploy_delay > DEPLOYMENT_DELAY)
                Debug.Log("[KSPI]: DeploymentControl Auto Deploy");
                Deploy();
            }
        }

        private double ConsumeWasteHeatPerSecond(double wasteheatToConsume, ResourceManager wasteheatManager)
        {
            if (!radiatorIsEnabled) return 0;

            var consumedWasteheat = CheatOptions.IgnoreMaxTemperature || wasteheatToConsume == 0
                ? wasteheatToConsume
                : consumeFNResourcePerSecond(wasteheatToConsume, ResourceSettings.Config.WasteHeatInMegawatt, wasteheatManager);

            return Double.IsNaN(consumedWasteheat) ? 0 : consumedWasteheat;
        }

        public double CurrentRadiatorTemperature
        {
            get => currentRadTemp;
            set
            {
                if (!value.IsInfinityOrNaN())
                {
                    currentRadTemp = value;
                    _radTempQueue.Enqueue(currentRadTemp);
                    if (_radTempQueue.Count > 20)
                        _radTempQueue.Dequeue();
                }

                var currentExternalTemp = PhysicsGlobals.SpaceTemperature;

                if (vessel != null && vessel.atmDensity > 0)
                    currentExternalTemp = ExternalTemp() * Math.Min(1, vessel.atmDensity);

                _externalTempQueue.Enqueue(Math.Max(PhysicsGlobals.SpaceTemperature, currentExternalTemp));
                if (_externalTempQueue.Count > 20)
                    _externalTempQueue.Dequeue();
            }
        }

        private double GetStableRadiatorTemperature()
        {
            return _radTempQueue.Count > 0 ? _radTempQueue.Average() : currentRadTemp;
        }

        private double GetAverageRadiatorTemperature()
        {
            return Math.Max(_externalTempQueue.Count > 0 ? _externalTempQueue.Average() : Math.Max(PhysicsGlobals.SpaceTemperature, vessel.externalTemperature), GetStableRadiatorTemperature());
        }

        public override string GetInfo()
        {
            DetermineGenerationType();
            RadiatorProperties.Initialize();

            effectiveRadiatorArea = EffectiveRadiatorArea;
            _stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;

            var sb = StringBuilderCache.Acquire();
            sb.Append("<size=11>");
            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_radiatorArea")).Append(" ");//Base surface area:
            sb.Append(radiatorArea.ToString("F2")).AppendLine(" m\xB2 ");
            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_Area_Mass")).Append(" ");//Surface area / Mass
            sb.AppendLine((radiatorArea / part.mass).ToString("F2"));

            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_Area_Bonus")).Append(" ");//Surface Area Bonus:
            sb.AppendLine((string.IsNullOrEmpty(surfaceAreaUpgradeTechReq) ? 0.0 : surfaceAreaUpgradeMult - 1).ToString("P0"));

            sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_MaximumWasteHeatRadiatedMk1")).Append(" ");//\nMaximum Waste Heat Radiated\nMk1:
            sb.Append(RadiatorProperties.RadiatorTemperatureMk1.ToString("F0")).Append(" K, ");
            sb.AppendLine(PluginHelper.getFormattedPowerString(_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk1, 4)));

            sb.Append("Mk2: ").Append(RadiatorProperties.RadiatorTemperatureMk2.ToString("F0")).Append(" K, ");
            sb.AppendLine(PluginHelper.getFormattedPowerString(_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk2, 4)));

            sb.Append("Mk3: ").Append(RadiatorProperties.RadiatorTemperatureMk3.ToString("F0")).Append(" K, ");
            sb.AppendLine(PluginHelper.getFormattedPowerString(_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk3, 4)));

            sb.Append("Mk4: ").Append(RadiatorProperties.RadiatorTemperatureMk4.ToString("F0")).Append(" K, ");
            sb.AppendLine(PluginHelper.getFormattedPowerString(_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk4, 4)));

            if (!string.IsNullOrEmpty(surfaceAreaUpgradeTechReq))
            {
                sb.Append("Mk5: ").Append(RadiatorProperties.RadiatorTemperatureMk5.ToString("F0")).Append(" K, ");
                sb.AppendLine(PluginHelper.getFormattedPowerString(_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk5, 4)));

                sb.Append("Mk6: ").Append(RadiatorProperties.RadiatorTemperatureMk6.ToString("F0")).Append(" K, ");
                sb.AppendLine(PluginHelper.getFormattedPowerString(_stefanArea * Math.Pow(RadiatorProperties.RadiatorTemperatureMk6, 4)));

                var convection = effectiveRadiatorArea * convectiveBonus;
                var dissipation = _stefanArea * Math.Pow(900, 4);

                sb.Append(Localizer.Format("#LOC_KSPIE_Radiator_Maximumat1atmosphere", dissipation.ToString("F3"), convection.ToString("F3")));
            }

            sb.Append("</size>");

            return sb.ToStringAndRelease();
        }

        public override int getPowerPriority()
        {
            return 3;
        }

        private void SetHeatAnimationRatio(float color)
        {
            var heatStatesCount = _heatStates.Count();
            for (var i = 0; i < heatStatesCount; i++)
            {
                _anim = _heatStates[i];
                if (_anim == null)
                    continue;
                _anim.normalizedTime = color;
            }
        }

        private void ApplyColorHeat()
        {
            displayTemperature = (float)GetAverageRadiatorTemperature();

            colorRatio = displayTemperature < drapperPoint ? 0 : (float)Math.Min(1, (Math.Max(0, displayTemperature - drapperPoint) / _temperatureRange) * 1.05f);

            if (_heatStates != null && _heatStates.Any())
            {
                SetHeatAnimationRatio(colorRatio.Sqrt());
            }
            else if (!string.IsNullOrEmpty(colorHeat) && colorRatioExponent != 0)
            {
                if (_renderArray == null)
                    return;

                float colorRatioRed = 0;
                float colorRatioGreen = 0;
                float colorRatioBlue = 0;

                if (displayTemperature >= drapperPoint)
                {
                    colorRatioRed = redTempColorChannel.Evaluate(displayTemperature);
                    colorRatioGreen = greenTempColorChannel.Evaluate(displayTemperature);
                    colorRatioBlue = blueTempColorChannel.Evaluate(displayTemperature);
                }

                var effectiveColorRatio = Mathf.Pow(colorRatio, colorRatioExponent);

                var emissiveColor = new Color(colorRatioRed, colorRatioGreen, colorRatioBlue, effectiveColorRatio);

                for (var i = 0; i < _renderArray.Count(); i++)
                {
                    _renderer = _renderArray[i];

                    if (_renderer == null || _renderer.material == null)
                        continue;

                    if (_renderer.material.shader != null && _renderer.material.shader.name != kspShaderLocation)
                        _renderer.material.shader = _kspShader;

                    if (!string.IsNullOrEmpty(emissiveTextureLocation))
                    {
                        if (_renderer.material.GetTexture("_Emissive") == null)
                            _renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture(emissiveTextureLocation, false));
                    }

                    if (!string.IsNullOrEmpty(bumpMapTextureLocation))
                    {
                        if (_renderer.material.GetTexture("_BumpMap") == null)
                            _renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture(bumpMapTextureLocation, false));
                    }

                    _renderer.material.SetColor(colorHeat, emissiveColor);
                }
            }
        }

        public override string getResourceManagerDisplayName()
        {
            // use identical names so it will be grouped together
            return part.partInfo.title + (clarifyFunction ? " (radiator)" : "");
        }
    }
}
