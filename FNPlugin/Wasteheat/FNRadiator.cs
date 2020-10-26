using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Power;
using FNPlugin.Powermanagement;
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
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Power Priority", guiFormat = "F0", guiUnits = ""), UI_FloatRange(stepIncrement = 1.0F, maxValue = 5F, minValue = 0F)]
        public float powerPriority = 5;

        [KSPField(isPersistant = true, guiActive = false, guiName = "Surface Area", guiFormat = "F0"), UI_FloatRange(stepIncrement = 1.0F, maxValue = 1000F, minValue = 1F)]
        public float surfaceArea = 1;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Surface Area Upgrade", guiFormat = "F0", guiUnits = " m2"), UI_FloatRange(stepIncrement = 1F, maxValue = 128F, minValue = 0F)]
        public float surfaceAreaUpgrade;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Pump Speed Upgrade", guiFormat = "F0", guiUnits = " m/s"), UI_FloatRange(stepIncrement = 1F, maxValue = 1024F, minValue = 0F)]
        public float pumpSpeedUpgrade;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Pump Speed"), UI_FloatRange(stepIncrement = 1.0F, maxValue = 1000F, minValue = 0F)]
        public float pumpSpeed = 1;

        [KSPField(isPersistant = false, guiActive = false, guiName = "intakeAtmSpecificHeatCapacity", guiFormat = "F0", guiUnits = "")]
        public double intakeAtmSpecificHeatCapacity;

        [KSPField(isPersistant = false, guiActive = false, guiName = "intakeLqdSpecificHeatCapacity", guiFormat = "F0", guiUnits = "")]
        public double intakeLqdSpecificHeatCapacity;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Air Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double airHeatTransferrable;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Water Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double waterHeatTransferrable;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Steam Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double steamHeatTransferrable;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Heat Transferrable", guiFormat = "F2", guiUnits = " K")]
        public double heatTransferrable;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Heat Supply", guiFormat = "F2", guiUnits = " K")]
        public double maxSupplyOfHeat;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Coolant Supply Used", guiFormat = "F2", guiUnits = "%")]
        public double intakeReduction;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Intake ATM Amount", guiFormat = "F2", guiUnits = "")]
        public double intakeAtmAmount;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Intake Lqd Amount", guiFormat = "F2", guiUnits = "")]
        public double intakeLqdAmount;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Air Coolant Total", guiFormat = "F2", guiUnits = "")]
        public double airCoolantTotal;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Water Coolant Total", guiFormat = "F2", guiUnits = "")]
        public double waterCoolantTotal;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Steam Coolant Total", guiFormat = "F2", guiUnits = "")]
        public double steamCoolantTotal;


        // https://www.engineersedge.com/heat_transfer/convective_heat_transfer_coefficients__13378.htm
        // forced convection case
        [KSPField]
        public double airHeatTransferCoefficient = 0.002;
        [KSPField]
        public double lqdHeatTransferCoefficient = 0.03;
        [KSPField]
        public double powerDrawInJoules = 1; // How much power needed to run fans / etc. in joules.

        private int intakeLqdID;
        private int intakeAtmID;
        private double intakeLqdDensity;
        private double intakeAtmDensity;

        private double waterBoilPointInKelvin = 400; // at some stage, calculate it properly

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Upgrades applied")]
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

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk1"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk2"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk2;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk3"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk3;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasSurfaceUpgradeMk4"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasSurfaceUpgradeMk4;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk1"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk2"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk2;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk3"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk3;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasFanUpgradeMk4"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasPumpUpgradeMk4;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk1"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk1;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk2"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk2;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk3"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk3;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "hasStorageUpgradeMk4"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)] public bool hasStorageUpgradeMk4;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Max External Temp")] public double maxExternalTemp;

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

            var intakeLqdDefinition = PartResourceLibrary.Instance.GetDefinition("IntakeLqd");
            var intakeAirDefinition = PartResourceLibrary.Instance.GetDefinition("IntakeAir");
            var intakeAtmDefinition = PartResourceLibrary.Instance.GetDefinition("IntakeAtm");

            if (intakeLqdDefinition == null || intakeAirDefinition == null || intakeAtmDefinition == null)
            {
                Debug.Log("[ActiveCoolingSystemv3] Missing definitions :(");
                return;
            }

            intakeLqdSpecificHeatCapacity = intakeLqdDefinition.specificHeatCapacity;
            intakeAtmSpecificHeatCapacity = (intakeAtmDefinition.specificHeatCapacity == 0) ? intakeAirDefinition.specificHeatCapacity : intakeAtmDefinition.specificHeatCapacity;

            intakeLqdID = intakeLqdDefinition.id;
            intakeAtmID = intakeAtmDefinition.id;
            intakeAtmDensity = intakeAtmDefinition.density;
            intakeLqdDensity = intakeLqdDefinition.density;
        }

        private double DrawPower()
        {
            // what does electricity look like, anyways?

            var powerNeeded = powerDrawInJoules;
            var powerAvail = consumeFNResourcePerSecond(powerNeeded, ResourceManager.FNRESOURCE_MEGAJOULES);

            return Math.Round(powerAvail / powerNeeded, 2);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            intakeAtmAmount = intakeLqdAmount = 0;

            if (null == vessel || null == part) return;

            part.GetConnectedResourceTotals(intakeAtmID, out intakeAtmAmount, out _);
            part.GetConnectedResourceTotals(intakeLqdID, out intakeLqdAmount, out _);

            if (intakeAtmAmount == 0 && intakeLqdAmount == 0) return;

            /* reduce the efficiency of the transfer if there is not enough power to run at 100% */
            var efficiency = DrawPower();
            if (efficiency == 0) return;

            var wasteheatManager = getManagerForVessel(ResourceManager.FNRESOURCE_WASTEHEAT);

            maxSupplyOfHeat = wasteheatManager.CurrentSurplus + wasteheatManager.GetResourceAvailability();
            if (maxSupplyOfHeat == 0) return;

            var fixedDeltaTime = (double)(decimal)TimeWarp.fixedDeltaTime;

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
            heatTransferrable = airHeatTransferrable + waterHeatTransferrable + steamHeatTransferrable;
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

            var heatTransferred = consumeFNResourcePerSecond(actuallyReduced, ResourceManager.FNRESOURCE_WASTEHEAT);

            if (heatTransferred == 0) return;

            if (intakeAtmAmount > 0) part.RequestResource(intakeAtmID, intakeAtmAmount * intakeReduction * fixedDeltaTime);
            if (intakeLqdAmount > 0) part.RequestResource(intakeLqdID, intakeLqdAmount * intakeReduction * fixedDeltaTime);
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

        // persitent
        [KSPField(isPersistant = true)] public bool radiatorInit;
        [KSPField(isPersistant = true)] public bool showRetractButton = false;
        [KSPField(isPersistant = true)] public bool showControls = true;
        [KSPField(isPersistant = true)] public double currentRadTemp;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_Cooling"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Radiator Cooling--Off--On
        public bool radiatorIsEnabled;
        [KSPField(groupName = GROUP, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_Automated"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Automated-Off-On
        public bool isAutomated = true;
        [KSPField(groupName = GROUP, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PivotOff"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Pivot--Off--On
        public bool pivotEnabled = true;
        [KSPField(groupName = GROUP, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PreventShieldedDeploy"), UI_Toggle(disabledText = "#LOC_KSPIE_Radiator_Off", enabledText = "#LOC_KSPIE_Radiator_On", affectSymCounterparts = UI_Scene.All)]//Prevent Shielded Deploy-Off-On
        public bool preventShieldedDeploy = true;

        // non persistent
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_Type")]//Type
        public string radiatorType;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_Radiator_MaxVacuumTemp", guiFormat = "F0", guiUnits = "K")]//Max Vacuum Temp
        public double maxVacuumTemperature = _maximumRadiatorTempInSpace;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_MaxAtmosphereTemp", guiFormat = "F0", guiUnits = "K")]//Max Atmosphere Temp
        public double maxAtmosphereTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_MaxCurrentTemp", guiFormat = "F0", guiUnits = "K")]//Max Current Temp
        public double maxCurrentRadiatorTemperature = maximumRadiatorTempAtOneAtmosphere;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_MaxRadiatorTemperature", guiFormat = "F0", guiUnits = "K")]//Max Radiator Temperature
        public float maxRadiatorTemperature = _maximumRadiatorTempInSpace;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_SpaceRadiatorBonus", guiFormat = "F0", guiUnits = "K")]//Space Radiator Bonus
        public double spaceRadiatorBonus;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_Mass", guiUnits = " t", guiFormat = "F3")]//Mass
        public float partMass;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_ConverctionBonus", guiUnits = "x")]//Converction Bonus
        public double convectiveBonus = 1;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_EffectiveArea", guiFormat = "F2", guiUnits = " m\xB2")]//Effective Area
        public double effectiveRadiatorArea;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "#LOC_KSPIE_Radiator_AtmosphereModifier")]//Atmosphere Modifier
        public double atmosphere_modifier;
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_Radiator_SurfaceArea", guiFormat = "F2", guiUnits = " m\xB2")]//Surface Area
        public double radiatorArea = 1;

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
        [KSPField] public double airHeatTransferCoefficient = 0.001; // 100W/m2/K, range: 10 - 100, "Air"
        [KSPField] public double lqdHeatTransferCoefficient = 0.01; // 1000/m2/K, range: 100-1200, "Water in Free Convection"

        [KSPField] public string kspShaderLocation = "KSP/Emissive/Bumped Specular";
        [KSPField] public int RADIATOR_DELAY = 20;
        [KSPField] public int DEPLOYMENT_DELAY = 6;
        [KSPField] public float drapperPoint = 500; // 798

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_Radiator_RadiatorTemp")]//Rad Temp
        public string radiatorTempStr;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PartTemp")]//Part Temp
        public string partTempStr;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PowerRadiated")]//Power Radiated
        public string thermalPowerDissipStr;
        [KSPField(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Radiator_PowerConvected")]//Power Convected
        public string thermalPowerConvStr;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_RadUpgradeCost")]//Rad Upgrade Cost
        public string upgradeCostStr;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_RadiatorStartTemp")]//Radiator Start Temp
        public double radiator_temperature_temp_val;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_DynamicPressureStress", guiActive = true, guiFormat = "P2")]//Dynamic Pressure Stress
        public double dynamicPressureStress;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Radiator_MaxEnergyTransfer", guiFormat = "F2")]//Max Energy Transfer
        private double _maxEnergyTransfer;

        [KSPField(groupName = GROUP, guiActive = false, guiName = "Part Rotation Distance", guiFormat = "F2", guiUnits = "m/s")] public double partRotationDistance;
        [KSPField(groupName = GROUP, guiActive = false, guiName = "vessel.atmDensity", guiFormat = "F4", guiUnits = "")] public double atmDensity;

        private double instantaneous_rad_temp;
        private int nrAvailableUpgradeTechs;
        private bool hasSurfaceAreaUpgradeTechReq;
        private double atmosphericMultiplier;
        private float displayTemperature;
        private float colorRatio;
        private double deltaTemp;
        private double verticalSpeed;
        private double spaceRadiatorModifier;
        private double oxidationModifier;
        private double temperatureDifference;
        private double submergedModifier;
        [KSPField(isPersistant = true)] public bool clarifyFunction;
        private double sphericalCowInAVaccum;

        static float _maximumRadiatorTempInSpace = 4500;
        static float maximumRadiatorTempAtOneAtmosphere = 1200;
        static float _maxSpaceTempBonus;
        static float _temperatureRange;

        // minimize garbage by recycling variables
        private double _stefanArea;
        private double _thermalPowerDissipationPerSecond;
        private double _radiatedThermalPower;
        private double _convectedThermalPower;

        private bool _active;
        private bool _isGraphene;

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

        private static AnimationCurve redTempColorChannel;
        private static AnimationCurve greenTempColorChannel;
        private static AnimationCurve blueTempColorChannel;

        private double intakeLqdDensity;
        private double intakeAtmDensity;
        private double intakeAtmSpecificHeatCapacity;
        private double intakeLqdSpecificHeatCapacity;

        public static void InitializeTemperatureColorChannels()
        {
            if (redTempColorChannel != null)
                return;

            redTempColorChannel = new AnimationCurve();
            redTempColorChannel.AddKey(500, 0 / 255f);
            redTempColorChannel.AddKey(800, 100 / 255f);
            redTempColorChannel.AddKey(1000, 200 / 255f);
            redTempColorChannel.AddKey(1250, 255 / 255f);
            redTempColorChannel.AddKey(1500, 255 / 255f);
            redTempColorChannel.AddKey(2000, 255 / 255f);
            redTempColorChannel.AddKey(2680, 255 / 255f);
            redTempColorChannel.AddKey(3000, 255 / 255f);
            redTempColorChannel.AddKey(3200, 255 / 255f);
            redTempColorChannel.AddKey(3500, 255 / 255f);
            redTempColorChannel.AddKey(4000, 255 / 255f);
            redTempColorChannel.AddKey(4200, 255 / 255f);
            redTempColorChannel.AddKey(4500, 255 / 255f);
            redTempColorChannel.AddKey(5000, 255 / 255f);

            greenTempColorChannel = new AnimationCurve();
            greenTempColorChannel.AddKey(500, 0 / 255f);
            greenTempColorChannel.AddKey(800, 0 / 255f);
            greenTempColorChannel.AddKey(1000, 0 / 255f);
            greenTempColorChannel.AddKey(1250, 0 / 255f);
            greenTempColorChannel.AddKey(1500, 30 / 255f);
            greenTempColorChannel.AddKey(2000, 100 / 255f);
            greenTempColorChannel.AddKey(2680, 180 / 255f);
            greenTempColorChannel.AddKey(3000, 230 / 255f);
            greenTempColorChannel.AddKey(3200, 255 / 255f);
            greenTempColorChannel.AddKey(3500, 255 / 255f);
            greenTempColorChannel.AddKey(4000, 255 / 255f);
            greenTempColorChannel.AddKey(4200, 255 / 255f);
            greenTempColorChannel.AddKey(4500, 255 / 255f);
            greenTempColorChannel.AddKey(5000, 255 / 255f);

            blueTempColorChannel = new AnimationCurve();
            blueTempColorChannel.AddKey(500, 0 / 255f);
            blueTempColorChannel.AddKey(800, 0 / 255f);
            blueTempColorChannel.AddKey(1000, 0 / 255f);
            blueTempColorChannel.AddKey(1500, 0 / 255f);
            blueTempColorChannel.AddKey(2000, 0 / 255f);
            blueTempColorChannel.AddKey(2680, 0 / 255f);
            blueTempColorChannel.AddKey(3000, 0 / 255f);
            blueTempColorChannel.AddKey(3200, 0 / 255f);
            blueTempColorChannel.AddKey(3500, 76 / 255f);
            blueTempColorChannel.AddKey(4000, 140 / 255f);
            blueTempColorChannel.AddKey(4200, 169 / 255f);
            blueTempColorChannel.AddKey(4500, 200 / 255f);
            blueTempColorChannel.AddKey(5000, 255 / 255f);

            for (int i = 0; i < redTempColorChannel.keys.Length; i++)
            {
                redTempColorChannel.SmoothTangents(i, 0);
            }
            for (int i = 0; i < greenTempColorChannel.keys.Length; i++)
            {
                greenTempColorChannel.SmoothTangents(i, 0);
            }
            for (int i = 0; i < blueTempColorChannel.keys.Length; i++)
            {
                blueTempColorChannel.SmoothTangents(i, 0);
            }
        }

        private static Dictionary<Vessel, List<FNRadiator>> radiators_by_vessel = new Dictionary<Vessel, List<FNRadiator>>();

        private static List<FNRadiator> GetRadiatorsForVessel(Vessel vessel)
        {
            if (radiators_by_vessel.TryGetValue(vessel, out var vesselRadiator))
                return vesselRadiator;

            vesselRadiator = vessel.FindPartModulesImplementing<FNRadiator>().ToList();
            radiators_by_vessel.Add(vessel, vesselRadiator);

            return vesselRadiator;
        }

        public GenerationType CurrentGenerationType { get; private set; }

        public ModuleActiveRadiator ModuleActiveRadiator => _moduleActiveRadiator;

        public double MaxRadiatorTemperature => GetMaximumTemperatureForGen(CurrentGenerationType);

        private double GetMaximumTemperatureForGen(GenerationType generationType)
        {
            var generation = (int)generationType;

            if (generation >= (int)GenerationType.Mk6 && _isGraphene)
                return RadiatorProperties.RadiatorTemperatureMk6;
            if (generation >= (int)GenerationType.Mk5 && _isGraphene)
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

        public double EffectiveRadiatorArea
        {
            get
            {
                if (radiatorArea == 0)
                {
                    clarifyFunction = true;

                    if (MeshRadiatorSize(out var size) == true)
                    {
                        radiatorArea = Math.Round(size);
                    }

                    if (radiatorArea == 0)
                    {
                        // The Liquid Metal Cooled Reactor shows a tiny surface space, so this should not be an else statement
                        radiatorArea = 1;
                    }
                }

                double effectiveRadiativeArea = PluginHelper.RadiatorAreaMultiplier * areaMultiplier * radiatorArea;

                // Because I have absolutely no idea what I'm doing, I'm taking some short cuts and major simplifications.
                // This is the radius of a circular radiator, (operating in a vacuum)
                sphericalCowInAVaccum = (effectiveRadiativeArea / Mathf.PI).Sqrt();

                return hasSurfaceAreaUpgradeTechReq
                    ? effectiveRadiativeArea * surfaceAreaUpgradeMult
                    : effectiveRadiativeArea;
            }
        }

        private void DetermineGenerationType()
        {
            // check if we have SurfaceAreaUpgradeTechReq
            hasSurfaceAreaUpgradeTechReq = PluginHelper.UpgradeAvailable(surfaceAreaUpgradeTechReq);

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
            radiators_by_vessel.Clear();
        }

        public static bool hasRadiatorsForVessel(Vessel vess)
        {
            return GetRadiatorsForVessel(vess).Any();
        }

        public static double getAverageRadiatorTemperatureForVessel(Vessel vess)
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

        public static float getAverageMaximumRadiatorTemperatureForVessel(Vessel vess)
        {
            var radiatorVessel = GetRadiatorsForVessel(vess);

            float averageTemp = 0;
            float nRadiators = 0;

            foreach (FNRadiator radiator in radiatorVessel)
            {
                if (radiator == null) continue;

                averageTemp += radiator.maxRadiatorTemperature;
                nRadiators += 1;
            }

            if (nRadiators > 0)
                averageTemp = averageTemp / nRadiators;
            else
                averageTemp = 0;

            return averageTemp;
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
            return (vessel == null) ? PhysicsGlobals.SpaceTemperature : vessel.externalTemperature;
        }

        protected void UpdateRadiatorArea()
        {
            effectiveRadiatorArea = EffectiveRadiatorArea;
            _stefanArea = PhysicsGlobals.StefanBoltzmanConstant * effectiveRadiatorArea * 1e-6;
        }

        public override void OnStart(StartState state)
        {
            string[] resourcesToSupply = { ResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resourcesToSupply;

            base.OnStart(state);

            _radiatedThermalPower = 0;
            _convectedThermalPower = 0;
            CurrentRadiatorTemperature = 0;
            _radiatorDeployDelay = 0;

            DetermineGenerationType();

            _isGraphene = !string.IsNullOrEmpty(surfaceAreaUpgradeTechReq);
            _maximumRadiatorTempInSpace = (float)RadiatorProperties.RadiatorTemperatureMk6;
            _maxSpaceTempBonus = _maximumRadiatorTempInSpace - maximumRadiatorTempAtOneAtmosphere;
            _temperatureRange = _maximumRadiatorTempInSpace - drapperPoint;

            _kspShader = Shader.Find(kspShaderLocation);
            maxRadiatorTemperature = (float)MaxRadiatorTemperature;

            part.heatConvectiveConstant = convectiveBonus;
            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            UpdateRadiatorArea();

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

            var intakeLqdDefinition = PartResourceLibrary.Instance.GetDefinition("IntakeLqd");
            var intakeAirDefinition = PartResourceLibrary.Instance.GetDefinition("IntakeAir");
            var intakeAtmDefinition = PartResourceLibrary.Instance.GetDefinition("IntakeAtm");

            if (intakeLqdDefinition != null && intakeAirDefinition != null && intakeAtmDefinition != null)
            {
                intakeLqdSpecificHeatCapacity = intakeLqdDefinition.specificHeatCapacity;
                intakeAtmSpecificHeatCapacity = (intakeAtmDefinition.specificHeatCapacity == 0) ? intakeAirDefinition.specificHeatCapacity : intakeAtmDefinition.specificHeatCapacity;
                intakeAtmDensity = intakeAtmDefinition.density;
                intakeLqdDensity = intakeLqdDefinition.density;
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

            maxVacuumTemperature = _isGraphene ? Math.Min(maxVacuumTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk4, maxRadiatorTemperature);
            maxAtmosphereTemperature = _isGraphene ? Math.Min(maxAtmosphereTemperature, maxRadiatorTemperature) : Math.Min(RadiatorProperties.RadiatorTemperatureMk3, maxRadiatorTemperature);

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
                _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
                _resourceBuffers.Init(this.part);
            }

            Fields[nameof(dynamicPressureStress)].guiActive = isDeployable;
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
                thermalPowerDissipStr = PluginHelper.getFormattedPowerString(_radiatedThermalPower, "0.0", "0.000");
                thermalPowerConvStr = PluginHelper.getFormattedPowerString(_convectedThermalPower, "0.0", "0.000");
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
            verticalSpeed = vessel.verticalSpeed;
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
            double tmpVelocity = tmp / (Mathf.PI * sphericalCowInAVaccum);
            // and then distance traveled.
            double distanceTraveled = effectiveRadiatorArea * tmpVelocity;

            return Math.Round(distanceTraveled, 2) * TimeWarp.fixedDeltaTime;
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
                    _resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, radiatorIsEnabled ? this.part.mass : this.part.mass * 1e-3);
                    _resourceBuffers.UpdateBuffers();
                }

                // get resource bar ratio at start of frame
                var wasteheatManager = getManagerForVessel(ResourceManager.FNRESOURCE_WASTEHEAT) as WasteHeatResourceManager;

                if (double.IsNaN(wasteheatManager.TemperatureRatio))
                {
                    Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Double.IsNaN detected in TemperatureRatio");
                    return;
                }

                // ToDo replace wasteheatManager.SqrtResourceBarRatioBegin by ResourceBarRatioBegin after generators hotbath takes into account expected temperature
                radiator_temperature_temp_val = Math.Min(maxRadiatorTemperature * wasteheatManager.TemperatureRatio, maxCurrentRadiatorTemperature);

                atmosphericMultiplier = Math.Sqrt(vessel.atmDensity);

                deltaTemp = Math.Max(radiator_temperature_temp_val - Math.Max(ExternalTemp() * Math.Min(1, atmosphericMultiplier), PhysicsGlobals.SpaceTemperature), 0);
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

                    _radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(_thermalPowerDissipationPerSecond, wasteheatManager) : 0;

                    if (double.IsNaN(_radiatedThermalPower))
                        Debug.LogError("[KSPI]: FNRadiator: FixedUpdate Double.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + _thermalPowerDissipationPerSecond + ")");

                    instantaneous_rad_temp = CalculateInstantaneousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;

                    if (_moduleDeployableRadiator)
                        _moduleDeployableRadiator.hasPivot = pivotEnabled;
                }
                else
                {
                    _thermalPowerDissipationPerSecond = wasteheatManager.RadiatorEfficiency * deltaTempToPowerFour * _stefanArea * 0.5;

                    _radiatedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(_thermalPowerDissipationPerSecond, wasteheatManager) : 0;

                    instantaneous_rad_temp = CalculateInstantaneousRadTemp();

                    CurrentRadiatorTemperature = instantaneous_rad_temp;
                }

                if (CanConvect())
                {
                    double bonusCalculation;

                    // Titanium radiators get a minimum surface area, and a slight buff here.
                    double era = _isGraphene ? effectiveRadiatorArea : Math.Max(15, effectiveRadiatorArea) * 1.05;

                    atmDensity = AtmDensity();

                    // density * exposed surface area * specific heat capacity
                    bonusCalculation = (intakeLqdDensity * effectiveRadiatorArea * intakeLqdSpecificHeatCapacity) * part.submergedPortion;
                    bonusCalculation += atmDensity * (intakeAtmDensity * era * intakeAtmSpecificHeatCapacity) * (1 - part.submergedPortion);

                    partRotationDistance = PartRotationDistance();
                    atmosphere_modifier = bonusCalculation * Math.Max(1, vessel.speed.Sqrt() + partRotationDistance.Sqrt());

                    temperatureDifference = Math.Max(0, CurrentRadiatorTemperature - ExternalTemp());

                    double heatTransferCoefficient = (part.submergedPortion > 0) ? lqdHeatTransferCoefficient : airHeatTransferCoefficient;

                    var convPowerDissipation = wasteheatManager.RadiatorEfficiency * atmosphere_modifier * temperatureDifference * effectiveRadiatorArea * heatTransferCoefficient;

                    if (!radiatorIsEnabled)
                        convPowerDissipation *= 0.25;

                    if(_isGraphene)
                    {
                        // Per AntaresMC and others in #development, graphene is chemically unstable in an atmosphere,
                        // prone to oxidation etc which reduces its effectiveness. 
                        convPowerDissipation *= 0.10;
                    }

                    _convectedThermalPower = canRadiateHeat ? consumeWasteHeatPerSecond(convPowerDissipation, wasteheatManager) : 0;

                    if (_radiatorDeployDelay >= DEPLOYMENT_DELAY)
                        DeploymentControl();
                }
                else
                {
                    submergedModifier = 0;
                    temperatureDifference = 0;
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

        protected virtual bool CanConvect()
        {
            return vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold;
        }

        protected virtual double AtmDensity()
        {
            // Another buff for titanium radiators - minimum of 50% effectiveness at the edge of space
            return (_isGraphene ? 1 : 1.5) - (vessel.altitude / vessel.mainBody.scienceValues.spaceAltitudeThreshold);
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

        private double consumeWasteHeatPerSecond(double wasteheatToConsume, ResourceManager wasteheatManager)
        {
            if (!radiatorIsEnabled) return 0;

            var consumedWasteheat = CheatOptions.IgnoreMaxTemperature || wasteheatToConsume == 0
                ? wasteheatToConsume
                : consumeFNResourcePerSecond(wasteheatToConsume, ResourceManager.FNRESOURCE_WASTEHEAT, wasteheatManager);

            return Double.IsNaN(consumedWasteheat) ? 0 : consumedWasteheat;
        }

        public double CurrentRadiatorTemperature
        {
            get => currentRadTemp;
            set
            {
                if (!value.IsInfinityOrNaNorZero())
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

        private double GetAverageRadiatorTemperature()
        {
            return Math.Max(_externalTempQueue.Count > 0 ? _externalTempQueue.Average() : Math.Max(PhysicsGlobals.SpaceTemperature, vessel.externalTemperature), _radTempQueue.Count > 0 ? _radTempQueue.Average() : currentRadTemp);
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

                var convection = 0.9 * (Math.Min(15, effectiveRadiatorArea) * 3);
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

            colorRatio = displayTemperature < drapperPoint ? 0 : Mathf.Min(1, (Mathf.Max(0, displayTemperature - drapperPoint) / _temperatureRange) * 1.05f);

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
