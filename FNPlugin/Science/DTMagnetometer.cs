using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin 
{
    class DTMagnetometer : PartModule 
    {
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = false)]
        public string animName = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "|B|")]
        public string Bmag;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_r")]
        public string Brad;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_T")]
        public string Bthe;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Flux")]
        public string ParticleFlux;

        protected Animation anim;
        protected CelestialBody homeworld;

        [KSPEvent(guiActive = true, guiName = "Activate Magnetometer", active = true)]
        public void ActivateMagnetometer() 
        {
            anim [animName].speed = 1;
            anim [animName].normalizedTime = 0;
            anim.Blend (animName, 2);
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Magnetometer", active = false)]
        public void DeactivateMagnetometer() 
        {
            anim [animName].speed = -1;
            anim [animName].normalizedTime = 1;
            anim.Blend (animName, 2);
            IsEnabled = false;
        }

        [KSPAction("Activate Magnetometer")]
        public void ActivateMagnetometerAction(KSPActionParam param) 
        {
            ActivateMagnetometer();
        }

        [KSPAction("Deactivate Magnetometer")]
        public void DeactivateMagnetometerAction(KSPActionParam param) 
        {
            DeactivateMagnetometer();
        }

        [KSPAction("Toggle Magnetometer")]
        public void ToggleMagnetometerAction(KSPActionParam param)
        {
            if (IsEnabled)
                DeactivateMagnetometer();
            else
                ActivateMagnetometer();
        }

        public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor) return;

            homeworld = FlightGlobals.fetch.bodies.First(m => m.isHomeWorld == true);

            UnityEngine.Debug.Log("[KSPI]: DTMagnetometer on " + part.name + " was Force Activated");
            this.part.force_activate();

            anim = part.FindModelAnimators (animName).FirstOrDefault ();

            if (anim == null) return;

            anim [animName].layer = 1;
            if (!IsEnabled) 
            {
                anim [animName].normalizedTime = 1;
                anim [animName].speed = -1;
            } 
            else 
            {
                anim [animName].normalizedTime = 0;
                anim [animName].speed = 1;
            }
            anim.Play ();
        }

        public override void OnUpdate() 
        {
            Events["ActivateMagnetometer"].active = !IsEnabled;
            Events["DeactivateMagnetometer"].active = IsEnabled;
            Fields["Bmag"].guiActive = IsEnabled;
            Fields["Brad"].guiActive = IsEnabled;
            Fields["Bthe"].guiActive = IsEnabled;
            Fields["ParticleFlux"].guiActive = IsEnabled;

            var lat = vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            var Bmag = vessel.mainBody.GetBeltMagneticFieldMagnitude(homeworld, vessel.altitude, lat);
            var Brad = vessel.mainBody.GetBeltMagneticFieldRadial(homeworld, vessel.altitude, lat);
            var Bthe = vessel.mainBody.getBeltMagneticFieldAzimuthal(homeworld, vessel.altitude, lat);
            var flux = vessel.mainBody.GetBeltAntiparticles(homeworld, vessel.altitude, lat);
            this.Bmag = Bmag.ToString("E") + "T";
            this.Brad = Brad.ToString("E") + "T";
            this.Bthe = Bthe.ToString("E") + "T";
            ParticleFlux = flux.ToString("E");
        }

        public override void OnFixedUpdate() {}
    }
}
