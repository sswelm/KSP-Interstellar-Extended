using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PersistentRotation
{
    public static class MechJebWrapper
    {
        private static Type mjCore_t;
        private static FieldInfo saTarget_t;
        private static Type mjVesselExtensions_t;
        private static Reflection.DynamicMethod<Vessel> GetMasterMechJeb;
        private static Reflection.DynamicMethod<object, string> GetComputerModule;
        private static Reflection.DynamicMethodBool<object> ModuleEnabled;
        static internal bool mjAvailable;

        #region ### MechJeb Enum Imports ###
        public enum SATarget
        {
            OFF = 0,
            KILLROT = 1,
            NODE = 2,
            SURFACE = 3,
            PROGRADE = 4,
            RETROGRADE = 5,
            NORMAL_PLUS = 6,
            NORMAL_MINUS = 7,
            RADIAL_PLUS = 8,
            RADIAL_MINUS = 9,
            RELATIVE_PLUS = 10,
            RELATIVE_MINUS = 11,
            TARGET_PLUS = 12,
            TARGET_MINUS = 13,
            PARALLEL_PLUS = 14,
            PARALLEL_MINUS = 15,
            ADVANCED = 16,
            AUTO = 17,
            SURFACE_PROGRADE = 18,
            SURFACE_RETROGRADE = 19,
            HORIZONTAL_PLUS = 20,
            HORIZONTAL_MINUS = 21,
            VERTICAL_PLUS = 22,
        }
        static public Dictionary<int, SATarget> saTargetMap = new Dictionary<int, SATarget>
        {
            { 0, SATarget.OFF },
            { 1, SATarget.KILLROT },
            { 2, SATarget.NODE },
            { 3, SATarget.SURFACE },
            { 4, SATarget.PROGRADE },
            { 5, SATarget.RETROGRADE },
            { 6, SATarget.NORMAL_PLUS },
            { 7, SATarget.NORMAL_MINUS },
            { 8, SATarget.RADIAL_PLUS },
            { 9, SATarget.RADIAL_MINUS },
            {10, SATarget.RELATIVE_PLUS },
            {11, SATarget.RELATIVE_MINUS },
            {12, SATarget.TARGET_PLUS },
            {13, SATarget.TARGET_MINUS },
            {14, SATarget.PARALLEL_PLUS },
            {15, SATarget.PARALLEL_MINUS },
            {16, SATarget.ADVANCED },
            {17, SATarget.AUTO },
            {18, SATarget.SURFACE_PROGRADE },
            {19, SATarget.SURFACE_RETROGRADE },
            {20, SATarget.HORIZONTAL_PLUS },
            {21, SATarget.HORIZONTAL_MINUS },
            {22, SATarget.VERTICAL_PLUS }
        };
        #endregion

        /* MONOBEHAVIOUR METHODS */
        public static void Initialize()
        {
            Debug.Log("[PR] Initializing MechJeb wrapper...");
            mjAvailable = false;
            try
            {
                mjCore_t = Reflection.GetExportedType("MechJeb2", "MuMech.MechJebCore");
                if (mjCore_t == null)
                {
                    return;
                }

                mjVesselExtensions_t = Reflection.GetExportedType("MechJeb2", "MuMech.VesselExtensions");
                if (mjVesselExtensions_t == null)
                {
                    return;
                }

                Type mjModuleSmartass_t = Reflection.GetExportedType("MechJeb2", "MuMech.MechJebModuleSmartASS");
                if (mjModuleSmartass_t == null)
                {
                    return;
                }

                saTarget_t = mjModuleSmartass_t.GetField("target", BindingFlags.Instance | BindingFlags.Public);
                if (saTarget_t == null)
                {
                    return;
                }

                MethodInfo GetMasterMechJeb_t = mjVesselExtensions_t.GetMethod("GetMasterMechJeb", BindingFlags.Static | BindingFlags.Public);
                if (GetMasterMechJeb_t == null)
                {
                    return;
                }
                GetMasterMechJeb = Reflection.CreateFunc<Vessel>(GetMasterMechJeb_t);
                if (GetMasterMechJeb == null)
                {
                    return;
                }

                MethodInfo GetComputerModule_t = mjCore_t.GetMethod("GetComputerModule", new Type[] { typeof(string) });
                if (GetComputerModule_t == null)
                {
                    return;
                }
                GetComputerModule = Reflection.CreateFunc<object, string>(GetComputerModule_t);
                if (GetComputerModule == null)
                {
                    return;
                }

                Type mjComputerModule_t = Reflection.GetExportedType("MechJeb2", "MuMech.ComputerModule");
                if (mjComputerModule_t == null)
                {
                    return;
                }
                PropertyInfo mjModuleEnabledProperty = mjComputerModule_t.GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo mjModuleEnabled = null;
                if (mjModuleEnabledProperty != null)
                {
                    mjModuleEnabled = mjModuleEnabledProperty.GetGetMethod();
                }
                if (mjModuleEnabled == null)
                {
                    return;
                }
                ModuleEnabled = Reflection.CreateFuncBool<object>(mjModuleEnabled);


                mjAvailable = true;
                Debug.Log("[PR] MechJeb reflection successfull!");
            }
            catch
            {
                Debug.LogWarning("[PR] MechJeb exception.");
            }
        }

        /* PUBLIC METHODS */
        public static SATarget GetMode(Vessel vessel)
        {
            object masterMechJeb;
            object smartAss;
            SATarget saTarget;

            if (mjAvailable)
            {
                masterMechJeb = GetMasterMechJeb(vessel);
                if(masterMechJeb == null)
                {
                    return SATarget.OFF;
                }
                smartAss = GetComputerModule(masterMechJeb, "MechJebModuleSmartASS");

                object activeSATarget = saTarget_t.GetValue(smartAss);
                saTarget = saTargetMap[(int)activeSATarget];
                return saTarget;
            }
            else
            {
                return SATarget.OFF;
            }
        }
        public static bool Active(Vessel vessel)
        {
            object masterMechJeb;
            object ascentAutopilot;
            object landingAutopilot;
            object nodeExecutor;
            object rendezvousAutopilot;

            if (mjAvailable)
            {
                masterMechJeb = GetMasterMechJeb(vessel);
                if (masterMechJeb == null)
                {
                    return false;
                }
                ascentAutopilot = GetComputerModule(masterMechJeb, "MechJebModuleAscentAutopilot");
                landingAutopilot = GetComputerModule(masterMechJeb, "MechJebModuleLandingAutopilot");
                nodeExecutor = GetComputerModule(masterMechJeb, "MechJebModuleNodeExecutor");
                rendezvousAutopilot = GetComputerModule(masterMechJeb, "MechJebModuleRendezvousAutopilot");

                if (ModuleEnabled(ascentAutopilot) || ModuleEnabled(landingAutopilot) || ModuleEnabled(nodeExecutor) || ModuleEnabled(rendezvousAutopilot))
                    return true;

                return false;
            }
            else
            {
                return false;
            }
        }
    }
}