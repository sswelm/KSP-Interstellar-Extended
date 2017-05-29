using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;
using FNPlugin.Extensions;

namespace FNPlugin.Collectors
{
    class UniversalCrustExtractor : FNResourceSuppliableModule
    {
        List<CrustalResource> localResources; // list of resources

        // state of the extractor
        [KSPField(isPersistant = true)]
        public bool bIsEnabled = false;

        // previous data
        [KSPField(isPersistant = true)]
        double dLastActiveTime;
        [KSPField(isPersistant = true)]
        double dLastPseudoMinedAmount;

        // drill properties, need to be adressed in the cfg file of the part
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Drill size", guiUnits = " m\xB3")]
        public double drillSize = 5; // Volume of the collector's drill. Raise in part config (for larger drills) to make collecting faster.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Drill effectiveness", guiFormat = "P1")]
        public double effectiveness = 1.0; // Effectiveness of the drill. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "MW Requirements", guiUnits = " MW")]
        public double mwRequirements = 1.0; // MW requirements of the drill. Affects heat produced.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Waste Heat Modifier", guiFormat = "P1")]
        public double wasteHeatModifier = 0.25; // How much of the power requirements ends up as heat. Change in part cfg, treat as a percentage (1 = 100%). Higher modifier means more energy ends up as waste heat.
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Drill reach", guiUnits = " m\xB3")]
        public double drillReach = 5.0; // How far can the drill actually reach? Used in calculating raycasts to hit ground down below the part. The 5 is just about the reach of the generic drill. Change in part cfg for different models.

        // GUI elements declaration
        private Rect _window_position = new Rect(50, 50, labelWidth + valueWidth, 150);
        private int _window_ID;
        private bool _render_window;
        private Vector2 scrollPosition;

        private const int labelWidth = 200;
        private const int valueWidth = 200;

        private GUIStyle _bold_label;
        private GUIStyle _enabled_button;
        private GUIStyle _disabled_button;
        // end of GUI elements declaration

        private AbundanceRequest resourceRequest = new AbundanceRequest // create a new request object that we'll reuse to get the current stock-system resource concentration
        {
            ResourceType = HarvestTypes.Planetary,
            ResourceName = "", // this will need to be updated before 'sending the request'
            BodyId = 1, // this will need to be updated before 'sending the request'
            Latitude = 0, // this will need to be updated before 'sending the request'
            Longitude = 0, // this will need to be updated before 'sending the request'
            Altitude = 0, // this will need to be updated before 'sending the request'
            CheckForLock = false
        };

        // *** KSP Events ***
        [KSPEvent(guiActive = true, guiName = "Activate Drill", active = true)]
        public void ActivateCollector()
        {
            bIsEnabled = true;
            OnFixedUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "Disable Drill", active = true)]
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnFixedUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Mining Interface", active = true)]
        public void ToggleWindow()
        {
            _render_window = !_render_window;
        }
        // *** END of KSP Events

        // *** KSP Actions ***
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
            // if the setup went well, do the offline collecting dance
            if (StartupSetup(state))
            {
                // force activate this part if not in editor; otherwise the OnFixedUpdate etc. would not work
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
            Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled
        }

        public override void OnFixedUpdate()
        {
            if (bIsEnabled)
            {
                MineResources(false, TimeWarp.fixedDeltaTime);

                // Save time data for offline mining
                dLastActiveTime = Planetarium.GetUniversalTime();
            }
        }

        private void OnGUI()
        {
            if (this.vessel != FlightGlobals.ActiveVessel || !_render_window) return;

            _window_position = GUILayout.Window(_window_ID, _window_position, DrawGUI, "Universal Mining Interface");

            scrollPosition[1] = GUI.VerticalScrollbar(_window_position, scrollPosition[1], 1, 0, 150, "Scroll");
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
        private bool CheckIfCollectingPossible()
        {
            if (vessel.checkLanded() == false || vessel.checkSplashed() == true)
            {
                ScreenMessages.PostScreenMessage("Vessel is not landed properly.", 3.0f, ScreenMessageStyle.LOWER_CENTER);
                return false;
            }

            if (!IsDrillExtended())
            {
                ScreenMessages.PostScreenMessage("The universal drill needs to be extended before it can be used.", 3.0f, ScreenMessageStyle.LOWER_CENTER);
                return false;
            }

            if (!CanReachTerrain())
            {
                ScreenMessages.PostScreenMessage("The universal drill has trouble reaching the terrain, check the vessel situation and setup.", 3.0f, ScreenMessageStyle.LOWER_CENTER);
                return false;
            }

            // cleared all the prerequisites
            return true;
        }

        /// <summary>
        /// Helper function to see if the drill part is extended.
        /// </summary>
        /// <returns>Bool signifying whether the part is extended or not (if it's animation is played out).</returns>
        private bool IsDrillExtended()
        {
            ModuleAnimationGroup thisPartsAnimGroup = this.part.FindModuleImplementing<ModuleAnimationGroup>();
            return thisPartsAnimGroup.isDeployed;
        }

        /// <summary>
        /// Helper function to calculate (and raycast) if the drill could potentially hit the terrain.
        /// </summary>
        /// <returns>True if the raycast hits the terrain layermask and it's close enough for the drill to reach (affected by the drillReach part property).</returns>
        private bool CanReachTerrain()
        {
            Vector3d partPosition = this.part.transform.position; // find the position of the transform in 3d space
            double scaleFactor = this.part.rescaleFactor; // what is the rescale factor of the drill?
            float drillDistance = (float)(drillReach * scaleFactor); // adjust the distance for the ray with the rescale factor, needs to be a float for raycast.

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
            if (hit.collider != null) // we definitely hit the terrain
            {
                return true;
            }
            else // no hit, dammit
            {
                return false;
            }
        }

        /// <summary>
        /// Helper function to calculate whether the extractor is getting enough power.
        /// It also takes care of the power consumption.
        /// Returns true if there's enough power (or the Cheat Option Inifinite Electricity is ON).
        /// </summary>
        /// <returns>Bool signifying if there is enough power for the extractor to operate.</returns>
        private bool HasEnoughPower(double deltaTime)
        {
            if (CheatOptions.InfiniteElectricity) // is the cheat option of infinite electricity ON? Then skip all these checks.
            {
                return true;
            }
            else
            {
                double dPowerRequirementsMW = PluginHelper.PowerConsumptionMultiplier * mwRequirements;

                // calculate the provided power and consume it
                double dPowerReceivedMW = Math.Max(consumeFNResource(dPowerRequirementsMW * deltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES), 0);
                double dNormalisedRecievedPowerMW = dPowerReceivedMW / deltaTime;

                // when the requirements are low enough, we can get the power needed from stock energy charge
                if (dPowerRequirementsMW < 5 && dNormalisedRecievedPowerMW <= dPowerRequirementsMW)
                {
                    double dRequiredKW = (dPowerRequirementsMW - dNormalisedRecievedPowerMW) * 1000;
                    double dReceivedKW = ORSHelper.fixedRequestResource(part, FNResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, dRequiredKW * deltaTime);
                    dPowerReceivedMW += (dReceivedKW / 1000);
                    dNormalisedRecievedPowerMW = dPowerReceivedMW / deltaTime;
                }

                //dLastPowerPercentage = (dPowerReceivedMW / dPowerRequirementsMW / deltaTime);

                if (dNormalisedRecievedPowerMW >= dPowerRequirementsMW)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
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
                localResources = CrustalResourceHandler.GetCrustalCompositionForBody(FlightGlobals.currentMainBody);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[KSPI] UniversalCrustExtractor - Error while getting the crustal composition for the current body. Msg: {e.Message}. StackTrace: {e.StackTrace}.");
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
        /// <param name="percentage">An output parameter, returns the resource content percentage on the current planet.</param>
        /// <returns></returns>
        private bool CalculateResourcePercentage(CrustalResource currentResource, out double percentage)
        {
            percentage = 0;

            if (currentResource != null)
            {
                string strName = currentResource.ResourceName;
                var definition = PartResourceLibrary.Instance.GetDefinition(strName);
                try
                {
                    percentage = CrustalResourceHandler.getCrustalResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, strName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"[KSPI] - UniversalCrustExtractor - Error while retrieving crustal resource percentage for {strName} from CrustalResourceHandler. Setting to zero.");
                    return false; // if the percentage was not gotten correctly, we want to know, so return false
                }
                return true; // if we got here, the percentage-getting went well, so return true
            }
            else
            {
                Console.WriteLine($"[KSPI] - UniversalCrustExtractor - Error while calculating percentage, resource null. Setting to zero.");
                return false; // resource was null, we want to know that we should disregard it, so return false
            }
        }

        /// <summary>
        /// Gets the local abundance of the resource. Uses stock KSP behaviour. Returns true if data arrived okay.
        /// </summary>
        /// <param name="currentResource">The Crustal Resource we want the data for.</param>
        /// <param name="localAbundance">The local abundance of the currentResource.</param>
        /// <returns>True if the data was accessed without raising an exception. Also returns an output parameter.</returns>
        private bool GetLocalAbundance(CrustalResource currentResource, out double localAbundance)
        {
            localAbundance = 0;
            resourceRequest.ResourceName = currentResource.ResourceName;
            resourceRequest.Latitude = vessel.latitude;
            resourceRequest.Longitude = vessel.longitude;
            resourceRequest.Altitude = vessel.altitude;
            try
            {
                localAbundance = ResourceMap.Instance.GetAbundance(resourceRequest);
            }
            catch (Exception)
            {
                Console.WriteLine("[KSPI] UniversalCrustExtractor - Abundance request failed.");
                return false; // if we got here, something went wrong, return false
            }
            return true; // if we got this far, everything went well, presumably
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
                Console.WriteLine("[KSPI] UniversalCrustExtractor. Homeworld not found, setting crust thickness to 0.");
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
        /// Calculates the amount of the crust that has been "mined". The crust is just a pseudo-resource, that is not actually collected,
        /// nor stored in the vessel. It is immediately ("on-pickup") converted to other resources that are defined for the current body.
        /// Returns true if calculations went well.
        /// </summary>
        /// <param name="crustThickness">The thickness of the planet's crust.</param>
        /// <param name="minedAmount">The amount of the general crust pseudo-resource that has been "mined". Is used for further calculations.</param>
        /// <returns>Bool, signifying if the calculation went well.</returns>
        private bool CalculatePseudoMinedAmount(double crustThickness, out double minedAmount)
        {
            minedAmount = 0;

            try
            {
                minedAmount = crustThickness * drillSize * effectiveness;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KSPI] UniversalCrustExtractor. Mined amount calculation failed. Setting to 0. { ex.Message }.");
            }
            return true;

        }

        /// <summary>
        /// Calculates the amount of actual resource that will be collected.
        /// </summary>
        /// <param name="minedAmount">The amount of the crust pseudo-resource that has been "mined".</param>
        /// <param name="resourcePercentage">The percentage of the current resource in this planet's crust.</param>
        /// <param name="localAbundance">The local abundance of the resource.</param>
        /// <param name="deltaTime">The time since last Fixed Update (Unity).</param>
        /// <returns>Double, signifying the amount of the current resource to collect.</returns>
        private double CalculateResourceAmountCollected(double minedAmount, double resourcePercentage, double localAbundance, double deltaTime)
        {
            double resourceAmount = minedAmount * resourcePercentage * localAbundance * deltaTime;
            return resourceAmount;
        }

        /// <summary>
        /// Calculates the spare room for the current resource on the vessel.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns>Double, signifying the amount of spare room for the resource on the vessel.</returns>
        private double CalculateSpareRoom(string resourceName)
        {
            double spareRoom = 0;
            try
            {
                spareRoom = part.GetConnectedResources(resourceName).Sum(r => r.maxAmount - r.amount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KSPI] - UniversalCrustExtractor - error while calculating spare room for the current resource. {ex.Message}.");
                return 0;
            }
            return spareRoom;
        }

        /// <summary>
        /// Does the actual addition (collection) of the current resource.
        /// </summary>
        /// <param name="amount">The amount of resource to collect/add.</param>
        /// <param name="resourceName">The name of the current resource.</param>
        /// <param name="deltaTime">The time since last Fixed Update (Unity).</param>
        private void AddResource(double amount, string resourceName)
        {
            //amount = part.RequestResource(resourceName, -amount * deltaTime, ResourceFlowMode.ALL_VESSEL) / deltaTime;
            // dResourceFlow = (float)ORSHelper.fixedRequestResource(part, strRegolithResourceName, -dResourceChange);
            amount = ORSHelper.fixedRequestResource(part, resourceName, -amount, ResourceFlowMode.ALL_VESSEL);
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
                    ScreenMessages.PostScreenMessage("Not enough power to run the universal drill.", 3.0f, ScreenMessageStyle.LOWER_CENTER);
                    DisableCollector();
                    return;
                }

                if (!CheckIfCollectingPossible()) // collecting not possible due to some reasons.
                {
                    DisableCollector();
                    return; // let's get out of here, no mining for now
                }

                if (!GetResourceData()) // if the resource data was not okay, no mining
                {
                    ScreenMessages.PostScreenMessage("The universal drill is not sure where you are trying to mine. Please contact the mod author, tell him the details of this situation and provide the output log.", 3.0f, ScreenMessageStyle.LOWER_CENTER);
                    DisableCollector();
                    return;
                }


                double crustThickness = 0;
                double minedAmount = 0;

                if (!CalculateCrustThickness(vessel.altitude, FlightGlobals.currentMainBody, out crustThickness)) // crust thickness calculation off, no mining
                {
                    DisableCollector();
                    return;
                }

                if (!CalculatePseudoMinedAmount(crustThickness, out minedAmount)) // mined amount calculation off, no mining
                {
                    DisableCollector();
                    return;
                }

                StoreDataForOfflineMining(minedAmount);

                foreach (CrustalResource resource in localResources)
                {
                    double percentage = 0;
                    double localAbundance = 0;
                    double amount = 0;
                    double spareRoom = 0;
                    string resourceName = resource.ResourceName;
                    int iterator = 0;

                    if (CalculateResourcePercentage(resource, out percentage))
                    {
                        if (GetLocalAbundance(resource, out localAbundance))
                        {
                            deltaTime = (deltaTime >= 1.0 ? deltaTime : 1.0 ); 
                            amount = CalculateResourceAmountCollected(minedAmount, percentage, localAbundance, deltaTime);
                            spareRoom = CalculateSpareRoom(resourceName);

                            if (spareRoom > 0) // if there's space, add the resource
                            {
                                AddResource(amount, resourceName);
                            }

                            iterator++;
                        }
                    }
                }
            }
            else // this is offline collecting, so use the simplified version
            {
                // calculate the elapsed time since we last checked on the vessel
                //double elapsedTime = (Planetarium.GetUniversalTime() - dLastActiveTime) * 55;

                // these are helper variables for the message
                double amount = 0;
                double totalAmount = 0;
                int numberOfResources = 0;
                double percentage;
                double localAbundance;
                double spareRoom;
                string resourceName;

                // get the resource data
                if (!GetResourceData()) // if getting the resource data went wrong, no offline mining
                {
                    Debug.Log("[KSPI] - Universal Drill - Error while getting resource data for offline mining calculations.");
                    return;
                }

                // go through each resource, calculate the percentage, abundance, amount collected and spare room in tanks. If possible, add the resource
                foreach (CrustalResource resource in localResources)
                {
                    resourceName = resource.ResourceName;
                    if (CalculateResourcePercentage(resource, out percentage))
                    {
                        if (GetLocalAbundance(resource, out localAbundance))
                        {
                            // dLastPseudoMinedAmount is persistently stored, saving us some processing power
                            amount = CalculateResourceAmountCollected(dLastPseudoMinedAmount, percentage, localAbundance, deltaTime);
                            spareRoom = CalculateSpareRoom(resourceName);
                            if ((spareRoom > 0) && (amount > 0))
                            {
                                AddResource(amount, resourceName);
                                totalAmount += (amount > spareRoom) ? spareRoom : amount; // add the mined amount to the total for the message, but only the amount that actually got into the tanks
                                numberOfResources++;
                            }
                        }
                    }    
                }
                // inform the player about the offline processing
                ScreenMessages.PostScreenMessage($"Universal drill mined offline for {deltaTime:#} seconds, drilling out {totalAmount} units of {numberOfResources} resources.", 5.0f, ScreenMessageStyle.LOWER_CENTER);
            }



        }


        private void DrawGUI(int window)
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
            }

            //if (_enabled_button == null)
            //{
            //    _enabled_button = new GUIStyle(GUI.skin.button);
            //    _enabled_button.fontStyle = FontStyle.Bold;
            //}

            //if (_disabled_button == null)
            //{
            //    _disabled_button = new GUIStyle(GUI.skin.button);
            //    _disabled_button.fontStyle = FontStyle.Normal;
            //}

            if (GUI.Button(new Rect(_window_position.width - 20, 2, 18, 18), "x"))
            {
                _render_window = false;
            }
                
            
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Drill parameters:",_bold_label, GUILayout.Width(labelWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Size: " + drillSize.ToString("#.#") + " m\xB3");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("MW Requirements: " + mwRequirements.ToString("#.#") + " MW");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Drill effectiveness: " + effectiveness.ToString("P1"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Resources for the current location:", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label("Avg. crustal abundance", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.EndHorizontal();

            GetResourceData();

            
            //GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(labelWidth + valueWidth), GUILayout.Height(150));
            
            if (localResources != null)
            {
                foreach (CrustalResource resource in localResources)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.DisplayName, GUILayout.Width(labelWidth));
                    GUILayout.Label(resource.ResourceAbundance.ToString("##.######"));
                    GUILayout.EndHorizontal();
                }
            }
            //GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUI.DragWindow();

        }

    }
}