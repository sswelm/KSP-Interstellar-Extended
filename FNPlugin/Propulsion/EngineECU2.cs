using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    enum GenerationType { Mk1, Mk2, Mk3, Mk4, Mk5 }
    abstract class EngineECU2 : FNResourceSuppliableModule
    {

        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk3 = 0.05f;

        // None Persistant

        [KSPField(isPersistant = false)]
        public float maxThrust = 75;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 300;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded2 = 1200;


        [KSPField(isPersistant = false)]
        public float efficiency = 0.19f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded = 0.38f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded2 = 0.76f;


        // Use for SETI Mode

        [KSPField(isPersistant = false)]
        public float maxTemp = 2500;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;


        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust", guiUnits = " kN")]
        public float maximumThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Throtle", guiFormat = "F2")]
        public float throttle;
        
   

        // abstracts
 


        // protected
    /*    protected bool hasrequiredupgrade = false;
        protected bool radhazard = false;
        protected double standard_megajoule_rate = 0;
        protected double standard_deuterium_rate = 0;
        protected double standard_tritium_rate = 0;
    */

  
    }
}
