using FNPlugin.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin.Collectors
{
    class CrustalResourceAbundance
    {
        public CrustalResource Resource { get; set; }
        public double Local { get; set; }
    }

    class UniversalCrustExtractor : ResourceSuppliableModule
    {
        List<CrustalResource> localResources; // list of resources

        // state of the extractor
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillEnabled")]//Drill Enabled
        public bool bIsEnabled = false;
        [KSPField(isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillDeployed")]//Deployed
        public bool isDeployed;

        // previous data
        [KSPField(isPersistant = true)]
        double dLastActiveTime;
        [KSPField(isPersistant = true)]
        double dLastPseudoMinedAmount;

        [KSPField(isPersistant = true)]
        public float windowPositionX = 20;
        [KSPField(isPersistant = true)]
        public float windowPositionY = 20;

        // drill properties, need to be adressed in the cfg file of the part
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_Drillsize", guiUnits = " m\xB3")]//Drill size
        public double drillSize = 5; // Volume of the collector's drill. Raise in part config (for larger drills) to make collecting faster.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillEffectiveness", guiFormat = "P1")]//Drill effectiveness
        public double effectiveness = 1; // Effectiveness of the drill. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_MWRequirements", guiUnits = " MW")]//MW Requirements
        public double mwRequirements = 1; // MW requirements of the drill. Affects heat produced.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_WasteHeatModifier", guiFormat = "P1")]//Waste Heat Modifier
        public double wasteHeatModifier = 0.25; // How much of the power requirements ends up as heat. Change in part cfg, treat as a percentage (1 = 100%). Higher modifier means more energy ends up as waste heat.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillReach", guiUnits = " m\xB3")]//Drill reach
        public float drillReach = 5; // How far can the drill actually reach? Used in calculating raycasts to hit ground down below the part. The 5 is just about the reach of the generic drill. Change in part cfg for different models.
        [KSPField(isPersistant = false, guiActive = false)]
        public string loopingAnimationName = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public string deployAnimationName = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public float animationState;
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_ReasonNotCollecting")]//Reason Not Collecting
        public string reasonNotCollecting;
        [KSPField(isPersistant = false, guiActive = true, guiName = "HeatSink Status")] // Debugging information for the heatsink
        public string heatsinkDebug;
        [KSPField(isPersistant = true, guiActive = false)] // Debugging information for the heatsink
        private bool pumpingHeat;


        // GUI elements declaration
        private Rect _window_position = new Rect(50, 50, labelWidth + valueWidth * 5, 150);
        private int _window_ID;
        private bool _render_window;
        

        private ModuleScienceExperiment _moduleScienceExperiment;

        private Animation deployAnimation;
        private Animation loopAnimation;

        private const int labelWidth = 200;
        private const int valueWidth = 100;

        private GUIStyle _bold_label;
        private GUIStyle _normal_label;

        private KSPParticleEmitter[] particleEmitters;

        Dictionary<string, CrustalResourceAbundance> CrustalResourceAbundanceDict = new Dictionary<string, CrustalResourceAbundance>();

        private AbundanceRequest resourceRequest = new AbundanceRequest // create a new request object that we'll reuse to get the current stock-system resource concentration
        {
            ResourceType = HarvestTypes.Planetary,
            ResourceName = "", // this will need to be updated before 'sending the request'
            BodyId = 1, // this will need to be updated before 'sending the request'
            Latitude = 0, // this will need to be updated before 'sending the request'
            Longitude = 0, // this will need to be updated before 'sending the request'
            Altitude = 0, // this will need to be updated before 'sending the request'
            CheckForLock = false,
            ExcludeVariance = false,
        };

        // *** KSP Events ***



        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DeployDrill", active = true)]//Deploy Drill
        public void DeployDrill()
        {
            isDeployed = true;
            if (deployAnimation != null)
            {
                deployAnimation[deployAnimationName].speed = 1;
                deployAnimation[deployAnimationName].normalizedTime = 0;
                deployAnimation.Blend(deployAnimationName);
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_RetractDrill", active = true)]//Retract Drill
        public void RetractDrill()
        {
            bIsEnabled = false;
            isDeployed = false;

            animationState = 0;
            if (loopAnimation != null)
            {
                loopAnimation[loopingAnimationName].speed = -1;
                loopAnimation[loopingAnimationName].normalizedTime = 0;
                loopAnimation.Blend(loopingAnimationName);
            }

            if (deployAnimation != null)
            {
                deployAnimation[deployAnimationName].speed = -1;
                deployAnimation[deployAnimationName].normalizedTime = 1;
                deployAnimation.Blend(deployAnimationName);
            }
        }


        // *** KSP Events ***
        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_ActivateDrill", active = true)]//Activate Drill
        public void ActivateCollector()
        {
            isDeployed = true;
            bIsEnabled = true;
            OnFixedUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DisableDrill", active = true)]//Disable Drill
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnFixedUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_ToggleMiningInterface", active = false)]//Toggle Mining Interface
        public void ToggleWindow()
        {
            _render_window = !_render_window;
        }

        [KSPEvent(guiActive = true, guiName = "Toggle HeatPump", active = true)]//Toggle HeatPump
        public void ToggleHeatPump()
        {
            pumpingHeat = !pumpingHeat;
        }

        // *** END of KSP Events

        // *** KSP Actions ***

        [KSPAction("Toggle Deployment")]
        public void ToggleDeployAction(KSPActionParam param)
        {
            if (isDeployed)
                RetractDrill();
            else
                DeployDrill();
        }

        [KSPAction("Toggle Drill")]
        public void ToggleDrillAction(KSPActionParam param)
        {
            if (bIsEnabled)
                DisableCollector();
            else
                ActivateCollector();
        }

        [KSPAction("Activate Drill")]
        public void ActivateScoopAction(KSPActionParam param)
        {
            ActivateCollector();
        }

        [KSPAction("Disable Drill")]
        public void DisableScoopAction(KSPActionParam param)
        {
            DisableCollector();
        }
        // *** END of KSP Actions

        public override void OnStart(PartModule.StartState state)
        {
            // initialise resources
            this.resources_to_supply = new[] { ResourceManager.FNRESOURCE_WASTEHEAT };
            base.OnStart(state);

            _moduleScienceExperiment = this.part.FindModuleImplementing<ModuleScienceExperiment>();

            deployAnimation = part.FindModelAnimators(deployAnimationName).FirstOrDefault();
            loopAnimation = part.FindModelAnimators(loopingAnimationName).FirstOrDefault();

            particleEmitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            if (isDeployed)
            {
                deployAnimation[deployAnimationName].speed = 1;
                deployAnimation[deployAnimationName].normalizedTime = 1;
                deployAnimation.Blend(deployAnimationName);
            }
            else
            {
                deployAnimation[deployAnimationName].speed = -1;
                deployAnimation[deployAnimationName].normalizedTime = 0;
                deployAnimation.Blend(deployAnimationName);
            }

            ToggleEmmitters(false);

            // if the setup went well, do the offline collecting dance
            if (StartupSetup(state))
            {
                // force activate this part if not in editor; otherwise the OnFixedUpdate etc. would not work

                Debug.Log("[KSPI]: UniversalCrustExtractor on " + part.name + " was Force Activated");
                this.part.force_activate();

                // create the id for the GUI window
                _window_ID = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);

                if (bIsEnabled)
                {
                    if (CheckForPreviousData())
                    {
                        double timeDifference = (Planetarium.GetUniversalTime() - dLastActiveTime) * 55;
                        MineResources(true, timeDifference);
                    }
                }
            }

        }

        public override void OnUpdate()
        {
            reasonNotCollecting = CheckIfCollectingPossible();

            Events["DeployDrill"].active = !isDeployed && !deployAnimation.IsPlaying(deployAnimationName);
            Events["RetractDrill"].active = isDeployed && !deployAnimation.IsPlaying(deployAnimationName);

            if (String.IsNullOrEmpty(reasonNotCollecting))
            {
                if (_moduleScienceExperiment != null)
                {
                    _moduleScienceExperiment.Events["DeployExperiment"].active = true;
                    _moduleScienceExperiment.Events["DeployExperimentExternal"].active = true;
                    _moduleScienceExperiment.Actions["DeployAction"].active = true;
                }

                if (effectiveness > 0)
                {
                    Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
                    Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled
                }

                Events["ToggleWindow"].active = true;

                UpdateResourceAbundances();
            }
            else
            {
                if (_moduleScienceExperiment != null)
                {
                    _moduleScienceExperiment.Events["DeployExperiment"].active = false;
                    _moduleScienceExperiment.Events["DeployExperimentExternal"].active = false;
                    _moduleScienceExperiment.Actions["DeployAction"].active = false;
                }

                Events["ActivateCollector"].active = false;
                Events["DisableCollector"].active = false;
                Events["ToggleWindow"].active = false;

                _render_window = false;
            }

            //if (bIsEnabled && loopingAnimation != "")
            //    PlayAnimation(loopingAnimation, false, false, true); //plays independently of other anims

            base.OnUpdate();
        }

        private void UpdateResourceAbundances()
        {
            if (localResources == null)
                return;

            foreach (CrustalResource resource in localResources)
            {
                var currentAbundance = GetResourceAbundance(resource);

                CrustalResourceAbundance existingAbundance;
                if (CrustalResourceAbundanceDict.TryGetValue(resource.ResourceName, out existingAbundance))
                {
                    //existingAbundance.GlobalWithVariance = currentAbundance.GlobalWithVariance;
                    existingAbundance.Local = currentAbundance.Local;
                    //existingAbundance.Biome = currentAbundance.Biome;
                }
                else
                    CrustalResourceAbundanceDict.Add(resource.ResourceName, currentAbundance);
            }
        }

        //public override void OnFixedUpdate()
        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            if (bIsEnabled)
            {
                heatsinkDebug = string.Format("drilling, not pumping heat");
                ToggleEmmitters(true);
                UpdateLoopingAnimation();

                //double fixedDeltaTime = (double)(decimal)Math.Round(TimeWarp.fixedDeltaTime, 7);                
                MineResources(false, fixedDeltaTime);
                // Save time data for offline mining
                dLastActiveTime = Planetarium.GetUniversalTime();
            }
            else
            {
                foreach (CrustalResource resource in localResources)
                {
                    CalculateSpareRoom(resource);
                }

                if (pumpingHeat == true)
                {
                    ToggleEmmitters(true);

                    var state = CheckIfCollectingPossible();
                    if (String.IsNullOrEmpty(state) == false)
                    {
                        heatsinkDebug = state;
                        pumpingHeat = false;
                        return;
                    }

                    
                }
                else
                {
                    heatsinkDebug = string.Format("heatpump disabled");
                    ToggleEmmitters(false);
                }
            }
        }

        public override void OnPostResourceSuppliable(double fixedDeltaTime)
        {
            if (pumpingHeat)
            {
                WasteHeat();
            }
        }

        private void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && _render_window)
                _window_position = GUILayout.Window(_window_ID, _window_position, DrawGui, Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_windowtitle"));//"Universal Mining Interface"

            //scrollPosition[1] = GUI.VerticalScrollbar(_window_position, scrollPosition[1], 1, 0, 150, "Scroll");
        }

        // *** STARTUP FUNCTIONS ***
        private bool StartupSetup(StartState state)
        {
            // this bit goes through parts that contain animations and disables the "Status" field in GUI part window so that it's less crowded
            List<ModuleAnimateGeneric> MAGlist = part.FindModulesImplementing<ModuleAnimateGeneric>();
            foreach (ModuleAnimateGeneric MAG in MAGlist)
            {
                MAG.Fields["status"].guiActive = false;
                MAG.Fields["status"].guiActiveEditor = false;
            }
            if (state == StartState.Editor)
            {
                return false;
            }
            else
            {
                localResources = new List<CrustalResource>();
                return true;
            }
        }

        private bool CheckForPreviousData()
        {
            // verify a timestamp is available
            if (dLastActiveTime == 0)
            {
                return false;
            }

            // verify any power was available in previous state
            //if (dLastPowerPercentage < 0.01)
            //{
            //    return false;
            //}

            return true;
        }

        // *** END OF STARTUP FUNCTIONS ***

        // *** MINING FACILITATION FUNCTIONS ***

        /// <summary>
        /// The main "check-if-we-can-mine-here" function.
        /// </summary>
        /// <returns>Bool signifying whether yes, we can mine here, or not.</returns>
        private string CheckIfCollectingPossible()
        {
            if (vessel.checkLanded() == false || vessel.checkSplashed())
            {
                return Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_msg1");//"Vessel is not landed properly."
            }

            if (!IsDrillExtended())
            {
                return Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_msg2");//"needs to be extended before it can be used."
            }

            if (!CanReachTerrain())
            {
                return " " + Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_msg3");//trouble reaching the terrain.
            }

            // cleared all the prerequisites
            return String.Empty;
        }

        /// <summary>
        /// Helper function to see if the drill part is extended.
        /// </summary>
        /// <returns>Bool signifying whether the part is extended or not (if it's animation is played out).</returns>
        private bool IsDrillExtended()
        {
            return isDeployed && !deployAnimation.IsPlaying(deployAnimationName);
            //return deployAnimation.GetScalar == 1; 

            //if (_moduleAnimationGroup != null)
            //    return _moduleAnimationGroup.isDeployed;

            //return false;
        }

        /// <summary>
        /// Helper function to raycast what the drill could hit.
        /// </summary>
        /// <returns>The RaycastHit, which allows us to determine what is underneath us</returns>
        private RaycastHit WhatsUnderneath()
        {
            Vector3d partPosition = this.part.transform.position; // find the position of the transform in 3d space
            var scaleFactor = this.part.rescaleFactor; // what is the rescale factor of the drill?
            var drillDistance = drillReach * scaleFactor; // adjust the distance for the ray with the rescale factor, needs to be a float for raycast.

            RaycastHit hit = new RaycastHit(); // create a variable that stores info about hit colliders etc.
            LayerMask terrainMask = 32768; // layermask in unity, number 1 bitshifted to the left 15 times (1 << 15), (terrain = 15, the bitshift is there so that the mask bits are raised; this is a good reading about that: http://answers.unity3d.com/questions/8715/how-do-i-use-layermasks.html)
            Ray drillPartRay = new Ray(partPosition, -part.transform.up); // this ray will start at the part's center and go down in local space coordinates (Vector3d.down is in world space)

            /* This little bit will fire a ray from the part, straight down, in the distance that the part should be able to reach.
             * It returns true if there is solid terrain in the reach AND the drill is extended. Otherwise false.
             * This is actually needed because stock KSP terrain detection is not really dependable. This module was formerly using just part.GroundContact 
             * to check for contact, but that seems to be bugged somehow, at least when paired with this drill - it works enough times to pass tests, but when testing 
             * this module in a difficult terrain, it just doesn't work properly. (I blame KSP planet meshes + Unity problems with accuracy further away from origin). 
            */
            Physics.Raycast(drillPartRay, out hit, drillDistance, terrainMask); // use the defined ray, pass info about a hit, go the proper distance and choose the proper layermask 

            return hit;
        }

        /// <summary>
        /// Helper function to calculate (and raycast) if the drill could potentially hit the terrain.
        /// </summary>
        /// <returns>True if the raycast hits the terrain layermask and it's close enough for the drill to reach (affected by the drillReach part property).</returns>
        private bool CanReachTerrain()
        {
            RaycastHit hit = WhatsUnderneath();
            return hit.collider != null;
        }

        private bool DistanceToGround(out double groundDistance)
        {
            RaycastHit hit = WhatsUnderneath();

            if (hit.collider == null)
            {
                groundDistance = double.NaN;
                return false;
            }

            groundDistance = hit.distance;
            heatsinkDebug = String.Format("distance to ground is {0}", groundDistance);

            if (groundDistance <= 0)
            {
                return false;
            }

            return true;
        }

        private bool CalculateWasteHeatConsumable(double amountUnderground, double externalTemp, out double consume, out double newTemp)
        {
            // https://www.researchgate.net/publication/329044575_Calculation_of_Underground_Soil_Temperature_for_the_Installation_of_Ground_Heat_Exchange_Systems_in_Baghdad
            // Depending on ground thermal properties, the temperature becomes
            // constant all year. 15 metres is where it becomes stable in the
            // listed cities.

            // To quote, "Average soil temperature is expected to be equal to the annual mean air temperature.".

            // Moons are another beast. https://www.space.com/18175-moon-temperature.html

            // Per AntaresMC, in #balance, 
            // "In icy planets your heat storage will be effectively infinite and in salty ones it would grow quickly enough."
            // "For rocky/metalic ones it will be slow enough to need a few just to dump enpugh shit"
            // "We could just use an overall depth multiplier depending on the planet and ignore the specifics."
            // "Also if its above a certain tem itwould start melting the rock, at this point we can assume that the stored heat can infinetly increase"
            // "Or just cap the temp to 400K"

            // TL;DR - https://discord.com/channels/586489099632902178/615531179503910942/753192660126269480 :)

            double maxSize = 30; // 30 metres underground for 1TW of Cooling. Requires Tweakscale'd drills.
            consume = newTemp = 0;

            if (Double.IsNaN(amountUnderground) || Double.IsNaN(externalTemp))
            {
                return false;
            }

            double percent = amountUnderground >= maxSize ? 1 : amountUnderground / maxSize;
            // Perhaps if they have tweakscale'd their drill to reach maxSize metres underground,
            // we should let them have more of a bonus?

            // one large amount of of WasteHeat consumption.
            consume = 1e6 * percent;

            // Please improve me. :)
            newTemp = Math.Max(4, Math.Min(externalTemp, part.temperature) / 2);

            return true;
        }

        private bool WasteHeat()
        {
            double distance;
            double underground;

            // TODO: What should be the Megajoule cost of the drill dumping heat?
            // Should WasteHeat consumption be based on Megajoules available?

            if (DistanceToGround(out distance) == false)
            {
                pumpingHeat = false;
                return false;
            }

            // On a clamped to the ground base, the distance to the ground varies and oscillates.
            // Round it off to keep things relatively consistent.
            underground = Math.Round(this.drillSize - distance, 5);
            heatsinkDebug = String.Format("T: {0}, U: {1}", vessel.externalTemperature, underground);

            double avail = getResourceAvailability(ResourceManager.FNRESOURCE_WASTEHEAT);
            if (double.IsNaN(avail))
            {
                heatsinkDebug += String.Format(" - avail NaN");
                return false;
            }

            if(avail <= 0)
            {
                heatsinkDebug += String.Format(" - no available heat");
                return false;
            }

            double newTemp;
            double consume;

            if (CalculateWasteHeatConsumable(underground, vessel.externalTemperature, out consume, out newTemp) == false)
            {
                heatsinkDebug += String.Format(" - no draw");
                return false;
            }

            if (consume > avail)
            {
                heatsinkDebug += String.Format(" - reducing consumption");
                consume = avail;
            }

            consumeFNResourcePerSecond(consume, ResourceManager.FNRESOURCE_WASTEHEAT);

            // do we need to do something here about thermal flux / how easy it is for the drill to cool other parts?
            part.temperature = newTemp;
            part.skinTemperature = newTemp;

            return true;
        }

        /// <summary>
        /// Helper function to calculate whether the extractor is getting enough power.
        /// It also takes care of the power consumption.
        /// Returns true if there's enough power (or the Cheat Option Inifinite Electricity is ON).
        /// </summary>
        /// <returns>Bool signifying if there is enough power for the extractor to operate.</returns>
        private bool HasEnoughPower(double deltaTime)
        {
            if (CheatOptions.InfiniteElectricity || mwRequirements == 0) // is the cheat option of infinite electricity ON? Then skip all these checks.
                return true;

            double dPowerRequirementsMW = PluginHelper.PowerConsumptionMultiplier * mwRequirements;

            // calculate the provided power and consume it
            double dNormalisedRecievedPowerMW = consumeFNResourcePerSecond(dPowerRequirementsMW, ResourceManager.FNRESOURCE_MEGAJOULES);

            // when the requirements are low enough, we can get the power needed from stock energy charge
            if (dPowerRequirementsMW < 5 && dNormalisedRecievedPowerMW <= dPowerRequirementsMW)
            {
                double dRequiredKW = (dPowerRequirementsMW - dNormalisedRecievedPowerMW) * 1000;
                double dReceivedKW = part.RequestResource(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, dRequiredKW * deltaTime) / deltaTime;
                dNormalisedRecievedPowerMW += dReceivedKW / 1000;
            }

            // Workaround for some weird glitches where dNormalisedRecievedPowerMW gets slightly smaller than it should be during timewarping
            double powerPercentage = dNormalisedRecievedPowerMW / dPowerRequirementsMW;
            if (powerPercentage >= 0.99)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Function for accessing the resource data for the current planet.
        /// Returns true if getting the data went okay.
        /// </summary>
        /// <returns>Bool signifying whether the data arrived okay.</returns>
        private bool GetResourceData()
        {
            try
            {
                localResources = CrustalResourceHandler.GetCrustalCompositionForBody(FlightGlobals.currentMainBody).OrderBy(m => m.ResourceName).ToList();

                UpdateResourceAbundances();
            }
            catch (Exception e)
            {
                Console.WriteLine("[KSPI] UniversalCrustExtractor - Error while getting the crustal composition for the current body. Msg: " + e.Message + ". StackTrace: " + e.StackTrace);
                return false;
            }
            if (localResources == null)
            {
                Console.WriteLine("[KSPI] UniversalCrustExtractor - Error while getting the crustal composition. The composition arrived, but it was null.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the resource content percentage on the current planet.
        /// Takes a CrustalResource as a parameter.
        /// Returns boolean true if the data was gotten without trouble and also returns a double with the percentage.
        /// </summary>
        /// <param name="currentResource">A CrustalResource we want to get the percentage for.</param>
        /// <param name="globalPercentage">An output parameter, returns the resource content percentage on the current planet.</param>
        /// <returns></returns>
        private CrustalResourceAbundance GetResourceAbundance(CrustalResource currentResource)
        {
            var abundance = new CrustalResourceAbundance() { Resource = currentResource };

            if (currentResource != null)
            {
                try
                {
                    abundance.Local = GetAbundance(new AbundanceRequest()
                    {
                        ResourceType = HarvestTypes.Planetary,
                        ResourceName = currentResource.ResourceName,
                        BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                        Latitude = FlightGlobals.ship_latitude,
                        Longitude = FlightGlobals.ship_longitude,
                        CheckForLock = false
                    });

                }
                catch (Exception)
                {
                    Console.WriteLine("[KSPI]: UniversalCrustExtractor - Error while retrieving crustal resource percentage for " + currentResource.ResourceName + " from CrustalResourceHandler. Setting to zero.");
                    return null; // if the percentage was not gotten correctly, we want to know, so return false
                }

                return abundance; // if we got here, the percentage-getting went well, so return true
            }
            else
            {
                Console.WriteLine("[KSPI]: UniversalCrustExtractor - Error while calculating percentage, resource null. Setting to zero.");
                return null; // resource was null, we want to know that we should disregard it, so return false
            }
        }

        private double GetAbundance(AbundanceRequest request)
        {
            // retreive and convert to double
            double abundance = (double)(decimal)ResourceMap.Instance.GetAbundance(request) * 100;

            if (abundance < 1)
                abundance = Math.Pow(abundance, 3);

            return abundance;
        }

        /// <summary>
        /// Gets the 'thickness' of the planet's crust. Returns true if the calculation went without a hitch.
        /// </summary>
        /// <param name="altitude">Current altitude of the vessel doing the mining.</param>
        /// <param name="planet">Current planetary body.</param>
        /// <param name="thickness">The output parameter that gets returned, the thickness of the crust (i.e. how much resources can be mined here).</param>
        /// <returns>True if data was acquired okay. Also returns an output parameter, the thickness of the crust.</returns>
        private bool CalculateCrustThickness(double altitude, CelestialBody planet, out double thickness)
        {
            thickness = 0;
            CelestialBody homeworld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);
            if (homeworld == null)
            {
                Console.WriteLine("[KSPI]: UniversalCrustExtractor. Homeworld not found, setting crust thickness to 0.");
                return false;
            }
            double homeplanetMass = homeworld.Mass; // This will usually be Kerbin, but players can always use custom planet packs with a custom homeplanet or resized systems
            double planetMass = planet.Mass;

            /* I decided to incorporate an altitude modifier (similarly to regolith collector before).
             * According to various source, crust thickness is higher in higher altitudes (duh).
             * This is great from a gameplay perspective, because it creates an incentive for players to mine resources in more difficult circumstances 
             * (i.e. landing on highlands instead of flats etc.) and breaks the flatter-is-better base building strategy at least a bit.
             * This check will divide current altitude by 2500. At that arbitrarily-chosen altitude, we should be getting the basic concentration for the planet. 
             * Go to a higher terrain and you will find **more** resources. The + 500 shift is there so that even at altitude of 0 (i.e. Minmus flats etc.) there will
             * still be at least SOME resources to be mined, but not all that much.
             * This is pretty much the same as the regolith collector (which might get phased out eventually).
             */
            double dAltModifier = (altitude + 500.0) / 2500.0;

            // if the dAltModifier is negative (if we're somehow trying to mine in a crack under sea level, perhaps), assign 0, otherwise keep it as it is
            dAltModifier = dAltModifier < 0 ? 0 : dAltModifier;

            /* The actual concentration calculation is pretty simple. The more mass the current planet has in comparison to the homeworld, the more resources can be mined here.
             * While this might seem unfair to smaller moons and planets, this is actually somewhat realistic - bodies with smaller mass would be more porous,
             * so there might be lesser amount of heavier elements and less useful stuff to go around altogether.
             * This is then adjusted for the altitude modifier - there is simply more material to mine at high hills and mountains.
            */
            thickness = dAltModifier * (planetMass / homeplanetMass); // get a basic concentration. The more mass the current planet has, the more crustal resources to be found here
            return true;
        }

        /// <summary>
        /// Calculates the spare room for the current resource on the vessel.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns>Double, signifying the amount of spare room for the resource on the vessel.</returns>
        private double CalculateSpareRoom(CrustalResource resource)
        {
            double currentAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(resource.Definition.id, out currentAmount, out maxAmount);

            resource.Amount = currentAmount;
            resource.MaxAmount = maxAmount;
            resource.SpareRoom = maxAmount - currentAmount;

            return resource.SpareRoom;
        }

        /// <summary>
        /// Does the actual addition (collection) of the current resource.
        /// </summary>
        /// <param name="amount">The amount of resource to collect/add.</param>
        /// <param name="resourceName">The name of the current resource.</param>
        /// <param name="deltaTime">The time since last Fixed Update (Unity).</param>
        private double AddResource(double amount, string resourceName)
        {
            return part.RequestResource(resourceName, -amount, ResourceFlowMode.ALL_VESSEL);
        }

        private void StoreDataForOfflineMining(double amount)
        {
            // then add to the end of the list
            dLastPseudoMinedAmount = amount;
        }


        // *** The important function controlling the mining ***
        /// <summary>
        /// The controlling function of the mining. Calls individual/granular functions and decides whether to continue
        /// collecting resources or not.
        /// </summary>
        /// <param name="offlineCollecting">Bool parameter, signifies if this collection is done in catch-up mode (i.e. after the focus has been on another vessel).</param>
        /// <param name="deltaTime">Double, signifies the amount of time since last Fixed Update (Unity).</param>
        private void MineResources(bool offlineCollecting, double deltaTime)
        {
            if (!offlineCollecting)
            {
                if (!HasEnoughPower(deltaTime)) // if there was not enough power, no mining
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_PostMsg1"), 3.0f, ScreenMessageStyle.LOWER_CENTER);//"Not enough power to run the universal drill."
                    DisableCollector();
                    return;
                }

                reasonNotCollecting = CheckIfCollectingPossible();

                if (!String.IsNullOrEmpty(reasonNotCollecting)) // collecting not possible due to some reasons.
                {
                    ScreenMessages.PostScreenMessage(reasonNotCollecting, 3.0f, ScreenMessageStyle.LOWER_CENTER);

                    DisableCollector();
                    return; // let's get out of here, no mining for now
                }

                if (!GetResourceData()) // if the resource data was not okay, no mining
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_PostMsg2"), 3.0f, ScreenMessageStyle.LOWER_CENTER);//"The universal drill is not sure where you are trying to mine. Please contact the mod author, tell him the details of this situation and provide the output log."
                    DisableCollector();
                    return;
                }

                double crustThickness;
                if (!CalculateCrustThickness(vessel.altitude, FlightGlobals.currentMainBody, out crustThickness)) // crust thickness calculation off, no mining
                {
                    DisableCollector();
                    return;
                }

                double minedAmount = crustThickness * drillSize * effectiveness;
                double minedAmountStock = 0.0005 * drillSize * effectiveness;

                StoreDataForOfflineMining(minedAmount);

                foreach (CrustalResource resource in localResources)
                {
                    CrustalResourceAbundance abundance;
                    CrustalResourceAbundanceDict.TryGetValue(resource.ResourceName, out abundance);

                    if (abundance == null)
                        continue;

                    if (resource.ResourceName == "Ore")
                        resource.Production = minedAmountStock * abundance.Local;
                    else
                        resource.Production = minedAmount * abundance.Local;

                    CalculateSpareRoom(resource);

                    if (resource.SpareRoom > 0) // if there's space, add the resource
                        AddResource(resource.Production * deltaTime, resource.ResourceName);
                }
            }
            else // this is offline collecting, so use the simplified version
            {
                // these are helper variables for the message
                double totalAmount = 0;
                int numberOfResources = 0;

                // get the resource data
                if (!GetResourceData()) // if getting the resource data went wrong, no offline mining
                {
                    Debug.Log("[KSPI]: Universal Drill - Error while getting resource data for offline mining calculations.");
                    return;
                }

                // go through each resource, calculate the percentage, abundance, amount collected and spare room in tanks. If possible, add the resource
                foreach (CrustalResource resource in localResources)
                {
                    CrustalResourceAbundance abundance;
                    CrustalResourceAbundanceDict.TryGetValue(resource.ResourceName, out abundance);
                    if (abundance == null)
                        continue;

                    //amount = CalculateResourceAmountCollected(dLastPseudoMinedAmount, abundance.GlobalWithVariance, abundance.Local, deltaTime);
                    resource.Production = dLastPseudoMinedAmount * abundance.Local;
                    CalculateSpareRoom(resource);
                    if ((resource.SpareRoom > 0) && (resource.Production > 0))
                    {
                        var additionFixed = resource.Production * deltaTime;
                        AddResource(additionFixed, resource.ResourceName);
                        totalAmount += (additionFixed > resource.SpareRoom) ? resource.SpareRoom : additionFixed; // add the mined amount to the total for the message, but only the amount that actually got into the tanks
                        numberOfResources++;
                    }

                }
                // inform the player about the offline processing
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_PostMsg3", deltaTime.ToString("0"), totalAmount.ToString("0.000"), numberOfResources), 10.0f, ScreenMessageStyle.LOWER_CENTER);//"Universal drill mined offline for <<1>> seconds, drilling out <<2>> units of <<3>> resources."
            }
        }


        private void DrawGui(int window)
        {
            if (_bold_label == null)
                _bold_label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };

            if (_normal_label == null)
                _normal_label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Normal, font = PluginHelper.MainFont };

            if (GUI.Button(new Rect(_window_position.width - 20, 2, 18, 18), "x"))
                _render_window = false;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Drillparameters"), _bold_label, GUILayout.Width(labelWidth));//"Drill parameters:"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Size") + ": " + drillSize.ToString("#.#") + " m\xB3", _normal_label);//Size
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_MWRequirements") + ": " + mwRequirements.ToString("0.000") + " MW", _normal_label);//MW Requirements
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_DrillEffectiveness") + ": " + effectiveness.ToString("P1"), _normal_label);//Drill effectiveness
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Resourcesabundances") + ":", _bold_label, GUILayout.Width(labelWidth));//Resources abundances
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Name"), _bold_label, GUILayout.Width(valueWidth));//"Name"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Abundance"), _bold_label, GUILayout.Width(valueWidth));//"Abundance"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Productionpersecond"), _bold_label, GUILayout.Width(valueWidth));//"Production per second"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Productionperhour"), _bold_label, GUILayout.Width(valueWidth));//"Production per hour"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_SpareRoom"), _bold_label, GUILayout.Width(valueWidth));//"Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Stored"), _bold_label, GUILayout.Width(valueWidth));//"Stored"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_MaxCapacity"), _bold_label, GUILayout.Width(valueWidth));//"Max Capacity"
            GUILayout.EndHorizontal();

            GetResourceData();

            if (localResources != null)
            {
                foreach (CrustalResource resource in localResources)
                {
                    CrustalResourceAbundance abundance;
                    CrustalResourceAbundanceDict.TryGetValue(resource.ResourceName, out abundance);
                    if (abundance == null)
                        continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.DisplayName, _normal_label, GUILayout.Width(valueWidth));
                    GUILayout.Label(abundance.Local.ToString("##.######") + "%", _normal_label, GUILayout.Width(valueWidth));

                    if (resource.Definition != null)
                    {
                        if (resource.MaxAmount > 0)
                        {
                            var spareRoomMass = resource.SpareRoom * resource.Definition.density;

                            if (Math.Round(spareRoomMass, 6) > 0.000001)
                            {
                                GUILayout.Label(resource.Production.ToString("##.######") + " U/s", _normal_label, GUILayout.Width(valueWidth));
                                GUILayout.Label((resource.Production * resource.Definition.density * 3600).ToString("##.######") + " t/h", _normal_label, GUILayout.Width(valueWidth));
                                GUILayout.Label(spareRoomMass.ToString("##.######") + " t", _normal_label, GUILayout.Width(valueWidth));
                            }
                            else
                            {
                                GUILayout.Label("", _normal_label, GUILayout.Width(valueWidth));
                                GUILayout.Label("", _normal_label, GUILayout.Width(valueWidth));
                                GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_full"), _normal_label, GUILayout.Width(valueWidth));//"full"
                            }

                            GUILayout.Label((resource.Amount * resource.Definition.density).ToString("##.######") + " t", _normal_label, GUILayout.Width(valueWidth));
                            GUILayout.Label((resource.MaxAmount * resource.Definition.density).ToString("##.######") + " t", _normal_label, GUILayout.Width(valueWidth));
                        }
                        else
                        {
                            GUILayout.Label("", _normal_label, GUILayout.Width(valueWidth));
                            GUILayout.Label("", _normal_label, GUILayout.Width(valueWidth));
                            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_missing"), _normal_label, GUILayout.Width(valueWidth));//"missing"
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void ToggleEmmitters(bool state)
        {
            for (int i = 0; i < particleEmitters.Length; ++i)
            {
                var e = particleEmitters[i];
                e.emit = state;
                e.enabled = state;
            }
        }

        public void UpdateLoopingAnimation()
        {
            if (loopAnimation == null)
                return;

            if (animationState > 1)
                animationState = 0;

            animationState += 0.05f;

            loopAnimation[loopingAnimationName].speed = 0;
            loopAnimation[loopingAnimationName].normalizedTime = animationState;
            loopAnimation.Blend(loopingAnimationName, 1);
        }
    }
}