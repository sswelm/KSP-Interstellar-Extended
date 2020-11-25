using System;
using System.Collections.Generic;
using FNPlugin.Propulsion;
using UnityEngine;

namespace FNPlugin
{
    class FnRcsSounds : PartModule
    {
        [KSPField]
        public string rcsSoundFile = "WarpPlugin/Sounds/RcsHeavy";
        [KSPField]
        public string rcsShutoffSoundFile = "WarpPlugin/Sounds/RcsHeavyShutoff";
        [KSPField]
        public string rcsColdGasSoundFile = "Squad/Sounds/sound_rocket_mini";
        [KSPField]
        public float rcsVolume = 0.5f;
        [KSPField]
        public bool loopRcsSound = true;
        [KSPField]
        public bool internalRcsSoundsOnly = false;
        //[KSPField]
        //public bool useLightingEffects = true;

        public FXGroup RcsSound = null;
        public FXGroup RcsShutoffSound = null;
        public FXGroup RcsColdGasSound = null;

        private List<GameObject> RcsLights = new List<GameObject>();
        private bool Paused = false;

        private ElectricRCSController electricRCSController;

        private ModuleRCS _rcsModule = null;
        public ModuleRCS rcsModule
        {
            get
            {
                if (this._rcsModule == null)
                this._rcsModule = this.part.FindModuleImplementing<ModuleRCS>();
                return this._rcsModule;
            }
        }

        //Create FX group for sounds
        public bool CreateGroup(FXGroup group, string filename, bool loop)
        {
            if (name != string.Empty)
            {
                if (!GameDatabase.Instance.ExistsAudioClip(filename))
                {
                    Debug.LogError("[KSPI]: ERROR - file " + filename + ".* not found!");
                    return false;
                }
                group.audio = gameObject.AddComponent<AudioSource>();
                group.audio.volume = GameSettings.SHIP_VOLUME;
                group.audio.rolloffMode = AudioRolloffMode.Logarithmic;
                group.audio.dopplerLevel = 0;
                //group.audio.panLevel = 1f; Depreciated so we add 'spatialBlend' below
                group.audio.spatialBlend = 1;
                group.audio.clip = GameDatabase.Instance.GetAudioClip(filename);
                group.audio.loop = loop;
                group.audio.playOnAwake = false;
                return true;
            }
            return false;
        }

        public override void OnStart(PartModule.StartState state)
        {
            try
            {
                if (state == StartState.Editor || state == StartState.None) return;

                electricRCSController = this.part.FindModuleImplementing<ElectricRCSController>();

                // Works with squad sounds, not with rcsSoundFile.
                if (!GameDatabase.Instance.ExistsAudioClip(rcsSoundFile))
                    Debug.LogError("[KSPI]:RcsSounds: Audio file not found: " + rcsSoundFile);

                if (RcsSound == null)
                    Debug.LogError("[KSPI]:RcsSounds: Sound FXGroup not found.");

                if (RcsShutoffSound == null)
                    Debug.LogError("[KSPI]: RcsSounds: Sound shuttof FXGroup not found.");

                CreateGroup(RcsSound, rcsSoundFile, false);
                CreateGroup(RcsShutoffSound, rcsShutoffSoundFile, false);
                CreateGroup(RcsColdGasSound, rcsColdGasSoundFile, false);
                
                GameEvents.onGamePause.Add(new EventVoid.OnEvent(OnPause));
                GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(OnUnPause));
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]: RcsSounds OnStart: " + ex.Message);
            }
        }

        public void OnDestroy()
        {
            GameEvents.onGamePause.Remove(new EventVoid.OnEvent(OnPause));
            GameEvents.onGameUnpause.Remove(new EventVoid.OnEvent(OnUnPause));
        }

        public void OnPause()
        {
            Paused = true;
            RcsSound.audio.Stop();
            RcsShutoffSound.audio.Stop();
        }

        public void OnUnPause()
        {
            Paused = false;
        }

        private float soundPitch = 1;
        private float soundVolume = 0;
        private bool previouslyActive = false;
        public override void OnUpdate()
        {
            try
            {
                if (!Paused && RcsSound != null && RcsSound.audio != null && RcsShutoffSound != null && RcsShutoffSound.audio != null)
                {
                    bool rcsActive = false;
                    float rcsHighestPower = 0f;

                    if (!internalRcsSoundsOnly || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
                    {
                        // Check for the resource as the effects still fire slightly without fuel.
                        var resourceList = new List<PartResource>();
                        ResourceFlowMode m;
                        try
                        {
                            m = (ResourceFlowMode)Enum.Parse(typeof(ResourceFlowMode), rcsModule.resourceFlowMode);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[KSPI]: RcsSounds OnUpdate: " + ex.Message);
                            m = ResourceFlowMode.ALL_VESSEL;
                        }

                            for (int i = 0; i < rcsModule.thrusterFX.Count; i++)
                            {
                                rcsHighestPower = Mathf.Max(rcsHighestPower, rcsModule.thrusterFX[i].Power);
                            }
                            if (rcsHighestPower > 0.1f)
                                // Don't respond to SAS idling.
                                rcsActive = true;
                    }

                    if (rcsActive)
                    {
                        soundVolume = GameSettings.SHIP_VOLUME * rcsVolume * rcsHighestPower;
                        soundPitch = Mathf.Lerp(0.5f, 1f, rcsHighestPower);

                        if (electricRCSController != null && !electricRCSController.hasSufficientPower)
                        {
                            RcsSound.audio.Stop();

                            RcsColdGasSound.audio.pitch = soundPitch; // / 2;
                            RcsColdGasSound.audio.volume = soundVolume;
                            if (!RcsColdGasSound.audio.isPlaying)
                                RcsColdGasSound.audio.Play();
                        }
                        else
                        {
                            if (electricRCSController != null)
                                RcsColdGasSound.audio.Stop();

                            RcsSound.audio.pitch = soundPitch;
                            RcsSound.audio.volume = soundVolume;
                            if (!RcsSound.audio.isPlaying)
                                RcsSound.audio.Play();
                        }
                        previouslyActive = true;
                    }
                    else
                    {
                        if (electricRCSController != null)
                            RcsColdGasSound.audio.Stop();
                        RcsSound.audio.Stop();
                        //if (useLightingEffects)
                        //{
                        //    for (int i = 0; i < rcsModule.thrusterFX.Count; i++)
                        //        RcsLights[i].GetComponent<Light>().enabled = false;
                        //}
                        if (previouslyActive)
                        {
                            if (!internalRcsSoundsOnly ||
                                CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
                            {
                                RcsShutoffSound.audio.volume = soundVolume / 2;
                                RcsShutoffSound.audio.Play();
                            }
                            previouslyActive = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[KSPI]: RcsSounds Error OnUpdate: " + ex.Message);
            }
        }

        private void AddLights()
        {
            foreach (Transform t in rcsModule.thrusterTransforms)
            {
                // Only one Light is allowed per GameObject, so create a new GameObject each time.
                GameObject rcsLight = new GameObject();
                Light light = rcsLight.AddComponent<Light>();
                 //light.color = Color.white;
                //light.type = LightType.Spot;
                light.intensity = 1;
                light.range = 5f;
                //light.spotAngle = 45f;
                //light.transform.parent = t;
                //light.transform.position = t.transform.position;
                //light.transform.forward = t.transform.up;
                light.enabled = false;
                rcsLight.AddComponent<MeshRenderer>();
                RcsLights.Add(rcsLight);
            }
        }
    }
}
