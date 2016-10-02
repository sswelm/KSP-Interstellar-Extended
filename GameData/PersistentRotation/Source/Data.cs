using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.IO;

namespace PersistentRotation
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class Data : MonoBehaviour
    {
        public static Data instance { get; private set; }

        public class PRVessel
        {
            //General
            public Vessel vessel;
            public Vector3 momentum;
            public Vector3 planetariumRight;

            //Other Mods
            public MechJebWrapper.SATarget mjMode;
            public RemoteTechWrapper.ACFlightMode rtMode;

            //Rotation Mode
            public bool rotationModeActive;
            public bool dynamicReference;
            public Quaternion rotation;
            public Vector3 direction;
            public ITargetable reference;

            //Momentum Mode
            public bool momentumModeActive;
            public float desiredRPM;

            //Legacy Data
            public Vector3 lastPosition;
            public Transform lastTransform;
            public bool lastActive;
            public ITargetable lastReference;

            //Activity check
            public bool processed;

            public PRVessel(Vessel _vessel, Vector3 _momentum, MechJebWrapper.SATarget _mjMode, RemoteTechWrapper.ACFlightMode _rtMode, Vector3 _planetariumRight, bool _rotationModeActive, bool _dynamicReference, Quaternion _rotation, Vector3 _direction, ITargetable _reference, bool _momentumModeActive, float _desiredRPM)
            {
                vessel = _vessel;
                momentum = _momentum;
                planetariumRight = _planetariumRight;
                mjMode = _mjMode;
                rtMode = _rtMode;
                rotationModeActive = _rotationModeActive;
                dynamicReference = _dynamicReference;
                rotation = _rotation;
                direction = _direction;
                reference = _reference;
                momentumModeActive = _momentumModeActive;
                desiredRPM = _desiredRPM;

                lastPosition = Vector3.zero;
                lastTransform = vessel.ReferenceTransform;
                lastReference = reference;
                lastActive = false;

                processed = false;
            }
        }

        public enum DefaultReferenceMode
        {
            NONE,
            DYNAMIC
        }
        public DefaultReferenceMode defaultReferenceMode = DefaultReferenceMode.NONE;

        /* MONOBEHAVIOUR METHODS */
        private void Awake()
        {
            instance = this;
        }
        private void Start()
        {
            PRVessels = new List<PRVessel>();
            Load();

            if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH)
            {
                FindPRVessel(FlightGlobals.ActiveVessel).momentum = Vector3.zero; //Disables Flipping Vessels on Launchpad after Revert
            }
        }
        private void OnDestroy()
        {
            instance = null;
        }

        /* PRVESSEL STORAGE METHODS */
        public void Save()
        {
            Debug.Log("[PR] Saving Data.");
            try
            {
                ConfigNode save = new ConfigNode();
                save.AddValue("TIME", Planetarium.GetUniversalTime().ToString());
                save.AddValue("DEFAULT_REFERENCE_MODE", ((int)defaultReferenceMode).ToString());
                ConfigNode.CreateConfigFromObject(this, 0, save);

                //Save values per vessel
                foreach(PRVessel v in PRVessels)
                {
                    ConfigNode cn_vessel = save.AddNode(v.vessel.id.ToString());
                    cn_vessel.AddValue("MOMENTUM", KSPUtil.WriteVector(v.momentum));
                    cn_vessel.AddValue("PLANETARIUM_RIGHT", KSPUtil.WriteVector(v.planetariumRight));

                    cn_vessel.AddValue("MJMODE", ((int)(v.mjMode)).ToString());
                    cn_vessel.AddValue("RTMODE", ((int)(v.rtMode)).ToString());

                    cn_vessel.AddValue("ROTATION_MODE_ACTIVE", v.rotationModeActive.ToString());
                    cn_vessel.AddValue("DYNAMIC_REFERENCE", v.dynamicReference.ToString());
                    cn_vessel.AddValue("ROTATION", KSPUtil.WriteQuaternion(v.rotation));
                    cn_vessel.AddValue("DIRECTION", KSPUtil.WriteVector(v.direction));

                    //Get Reference Type and save accordingly
                    if(v.reference != null)
                    {
                        if(v.reference.GetType() == typeof(CelestialBody))
                            cn_vessel.AddValue("REFERENCE", v.reference.GetName());
                        else if(v.reference.GetType() == typeof(Vessel))
                            cn_vessel.AddValue("REFERENCE", v.reference.GetVessel().id.ToString());
                    }
                    else
                        cn_vessel.AddValue("REFERENCE", "NONE");

                    cn_vessel.AddValue("MOMENTUM_MODE_ACTIVE", v.momentumModeActive.ToString());
                    cn_vessel.AddValue("DESIRED_RPM", v.desiredRPM.ToString());
                }

                save.Save(GetUnusedPath());
            }
            catch (Exception e) { Debug.Log("[PR] Saving not sucessfull: " + e.Message); }
        }
        public void Load()
        {
            Debug.Log("[PR] Loading Data.");

            //This is called when all persistent rotation data is being loaded from the cfg file.

            #region ### Quicksave selection ###
            ConfigNode temp = null;
            float temp_delta = 0f;
            ConfigNode load = null;
            float load_delta = 0f;
            float oldest_time = 0f;

            List<String> allPaths = GetAllPaths();

            if(allPaths.Count() == 0)
            {
                Debug.Log("[PR] No save files found.");
                return;
            }

            foreach (String path in allPaths)
            {
                temp = ConfigNode.Load(path);
                if (temp == null)
                {
                    Debug.Log("[PR] Couldn't load data: File not found.");
                    continue;
                }


                float time = float.Parse(temp.GetValue("TIME"));
                temp_delta = Mathf.Abs(time - (float)Planetarium.GetUniversalTime());

                if(time > oldest_time)
                {
                    oldest_time = time;
                }

                if(load == null)
                {
                    load = temp;
                    load_delta = temp_delta;
                }
                else
                {
                    if(temp_delta < load_delta)
                    {
                        load = temp;
                        load_delta = temp_delta;
                    }
                }
            }
            #endregion

            //Load global variables

            defaultReferenceMode = (DefaultReferenceMode)(int.Parse(load.GetValue("DEFAULT_REFERENCE_MODE")));

            //Pregenerate data for all vessels that currently exist

            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                FindPRVessel(vessel);
            }

            //All vessels should now have data.

            //Load PRVessel data

            foreach (PRVessel v in PRVessels)
            {
                ConfigNode cn_vessel = load.GetNode(v.vessel.id.ToString());

                if(cn_vessel != null) //If node exists at all
                {
                    Debug.Log("[PR] Found node for vessel " + v.vessel.vesselName);
                    v.momentum = KSPUtil.ParseVector3(cn_vessel.GetValue("MOMENTUM"));
                    v.planetariumRight = KSPUtil.ParseVector3(cn_vessel.GetValue("PLANETARIUM_RIGHT"));
                    v.mjMode = MechJebWrapper.saTargetMap[int.Parse(cn_vessel.GetValue("MJMODE"))];
                    v.rtMode = RemoteTechWrapper.acFlightModeMap[int.Parse(cn_vessel.GetValue("RTMODE"))];
                    v.rotationModeActive = Boolean.Parse(cn_vessel.GetValue("ROTATION_MODE_ACTIVE"));
                    v.dynamicReference = Boolean.Parse(cn_vessel.GetValue("DYNAMIC_REFERENCE"));
                    v.rotation = KSPUtil.ParseQuaternion(cn_vessel.GetValue("ROTATION"));
                    v.direction = KSPUtil.ParseVector3(cn_vessel.GetValue("DIRECTION"));

                    string reference = cn_vessel.GetValue("REFERENCE");

                    v.reference = null;

                    if (reference != "NONE")
                    {
                        foreach (CelestialBody body in FlightGlobals.Bodies)
                        {
                            if(body.name == reference)
                            {
                                v.reference = body;
                            }
                        }

                        foreach (Vessel vessel in FlightGlobals.Vessels)
                        {
                            if (vessel.id.ToString() == reference)
                            {
                                v.reference = vessel;
                            }
                        }
                    }

                    v.momentumModeActive = Boolean.Parse(cn_vessel.GetValue("MOMENTUM_MODE_ACTIVE"));
                    v.desiredRPM = float.Parse(cn_vessel.GetValue("DESIRED_RPM"));
                }
            }

            //If old save state is loaded, delete all save files!
            if (float.Parse(load.GetValue("TIME")) < oldest_time)
            {
                Debug.Log("[PR] Reloading old save, flushing data.");

                foreach (String path in GetAllPaths())
                {
                    File.Delete(path);
                }

                //Save current loaded one.
                Save();
            }

            //TODO: Delete all files on Launch / Keep all files that correspond to a quicksave

            Debug.Log("[PR] Oldest time: " + oldest_time.ToString());
            Debug.Log("[PR] Loaded time: " + load.GetValue("TIME"));

            Interface.instance.desiredRPMstr = FindPRVessel(FlightGlobals.ActiveVessel).desiredRPM.ToString(); //Set desired rpm of active vessel
        }

        /* PRVESSEL METHODS */
        public List<PRVessel> PRVessels;
        public PRVessel FindPRVessel(Vessel vessel)
        {
            foreach (PRVessel v in PRVessels)
            {
                if (v.vessel == vessel)
                    return v;
            }
            return SubGenerate(vessel);
        }
        private PRVessel SubGenerate(Vessel vessel)
        {
            //v is null, I have to generate a new PRVessel

            Debug.Log("[PR] Generating data for " + vessel.vesselName);

            //Generate Momentum (Add slight spin to unidentified and identified asteroids)
            Vector3 momentum;
            if (vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Unknown)
            {
                float ran_x = (float)UnityEngine.Random.Range(0, 10) / (float)100;
                float ran_y = (float)UnityEngine.Random.Range(0, 10) / (float)100;
                float ran_z = (float)UnityEngine.Random.Range(0, 10) / (float)100;

                momentum = new Vector3(ran_x, ran_y, ran_z);
            }
            else
            {
                momentum = Vector3.zero;
            }

            PRVessel v;

            if(defaultReferenceMode == DefaultReferenceMode.DYNAMIC)
                v = new PRVessel(vessel, momentum, MechJebWrapper.SATarget.OFF, RemoteTechWrapper.ACFlightMode.Off, Planetarium.right, true, true, vessel.transform.rotation, (vessel.mainBody.position - vessel.transform.position).normalized, vessel.mainBody, false, 0f);
            else
                v = new PRVessel(vessel, momentum, MechJebWrapper.SATarget.OFF, RemoteTechWrapper.ACFlightMode.Off, Planetarium.right, true, false, vessel.transform.rotation, (vessel.mainBody.position - vessel.transform.position).normalized, null, false, 0f);

            PRVessels.Add(v);
            return v;
        }

        /* PATH METHODS */
        private String GetPath(int counter)
        {
            if (!Directory.Exists(KSPUtil.ApplicationRootPath + "/GameData/PersistentRotation/PluginData"))
                Directory.CreateDirectory(KSPUtil.ApplicationRootPath + "/GameData/PersistentRotation/PluginData");

            return KSPUtil.ApplicationRootPath + "/GameData/PersistentRotation/PluginData/PersistentRotation_" + HighLogic.CurrentGame.Title.TrimEnd("_()SANDBOXCAREERSCIENCE".ToCharArray()).TrimEnd(' ') + "_" + counter.ToString() + ".cfg";
        }
        private String GetUnusedPath()
        {
            int i = 0;
            while (true)
            {
                if (!File.Exists(GetPath(i)))
                {
                    return (GetPath(i));
                }
                else
                {
                    i++;
                }
            }
        }
        private List<String> GetAllPaths()
        {
            int i = 0;
            List<String> paths = new List<String>();
            while (true)
            {
                if (!File.Exists(GetPath(i)))
                {
                    return (paths);
                }
                else
                {
                    paths.Add(GetPath(i));
                    i++;
                }
            }
        }
    }
}