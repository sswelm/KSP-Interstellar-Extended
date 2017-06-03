using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PersistentRotation
{
    public static class RemoteTechWrapper
    {
        //typeof(T).GetProperty(propName).GetValue(obj, null);

        private static Type rtISigProc_t;       //ModuleSPU : ISignalProcessor
        private static PropertyInfo spFliCom_t; //SignalProcessor.FlightComputer

        private static Type rtUtil_t;           //RTUtil
        private static Type rtFliCom_t;         //FlightComputer
        private static Type rtAttCom_t;         //AttitudeCommand
        private static PropertyInfo fcAttCom_t;    //FlightComputer._activeCommands
        private static FieldInfo acMode_t;      //AttitudeCommand.Mode
        private static PropertyInfo fcControllable_t;
        private static Reflection.DynamicMethod<Vessel> GetSignalProcessor;
        static internal bool rtAvailable;

        public enum ACFlightMode
        {
            Off,
            KillRot,
            AttitudeHold,
            AltitudeHold,
            Rover
        }

        static public readonly Dictionary<int, ACFlightMode> acFlightModeMap = new Dictionary<int, ACFlightMode>
        {
            {0, ACFlightMode.Off },
            {1, ACFlightMode.KillRot },
            {2, ACFlightMode.AttitudeHold },
            {3, ACFlightMode.AltitudeHold },
            {4, ACFlightMode.Rover }
        };

        private static Dictionary<String, object> signalProcessors = new Dictionary<String, object>();

        public static void Initialize()
        {
            rtAvailable = false;
            try
            {
                Debug.Log("[PR] Initializing RemoteTech wrapper...");
                rtUtil_t = Reflection.GetExportedType("RemoteTech", "RemoteTech.RTUtil");
                if (rtUtil_t == null)
                {
                    return;
                }
                MethodInfo GetSignalProcessor_t = rtUtil_t.GetMethod("GetSignalProcessor", BindingFlags.Static | BindingFlags.Public);
                if (GetSignalProcessor_t == null)
                {
                    return;
                }
                GetSignalProcessor = Reflection.CreateFunc<Vessel>(GetSignalProcessor_t);
                if (GetSignalProcessor == null)
                {
                    return;
                }
                rtISigProc_t = Reflection.GetExportedType("RemoteTech", "RemoteTech.Modules.ModuleSPU");
                if (rtISigProc_t == null)
                {
                    return;
                }
                spFliCom_t = rtISigProc_t.GetProperty("FlightComputer", BindingFlags.Instance | BindingFlags.Public);
                if (spFliCom_t == null)
                {
                    return;
                }
                rtFliCom_t = Reflection.GetExportedType("RemoteTech", "RemoteTech.FlightComputer.FlightComputer");
                if (rtFliCom_t == null)
                {
                    return;
                }

                /* 
                From FlightComputer.cs in RemoteTech:
                
                public AttitudeCommand CurrentFlightMode => _activeCommands[0] as AttitudeCommand;
                 */

                fcAttCom_t = rtFliCom_t.GetProperty("CurrentFlightMode", BindingFlags.Instance | BindingFlags.Public); //changed currentFlightMode to CurrentFlightMode 
                if (fcAttCom_t == null)
                {
                    return;
                }
                rtAttCom_t = Reflection.GetExportedType("RemoteTech", "RemoteTech.FlightComputer.Commands.AttitudeCommand");
                if (rtAttCom_t == null)
                {
                    return;
                }
                acMode_t = rtAttCom_t.GetField("Mode", BindingFlags.Instance | BindingFlags.Public);
                if (acMode_t == null)
                {
                    return;
                }
                fcControllable_t = rtFliCom_t.GetProperty("InputAllowed", BindingFlags.Instance | BindingFlags.Public);
                if (fcControllable_t == null)
                {
                    return;
                }

                rtAvailable = true;
                Debug.Log("[PR] RemoteTech reflection successfull!");
            }
            catch
            {
                Debug.LogWarning("[PR] RemoteTech exception.");
            }
        }

        public static ACFlightMode GetMode(Vessel vessel)
        {
            object signalProcessor;
            object flightComputer;
            object currentFlightMode;
            object mode;

            if (rtAvailable)
            {
                signalProcessor = null;

                if (vessel.loaded && vessel.parts.Count > 0)
                {
                    var partModuleList = vessel.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Where(pm => pm.Fields.GetValue<bool>("IsRTSignalProcessor")).ToList();
                    if (partModuleList.Count > 0)
                    {
                        signalProcessor = partModuleList.FirstOrDefault(pm => pm.moduleName == "ModuleSPU");
                    }
                    if (signalProcessor == null)
                    {
                        return ACFlightMode.Off;
                    }

                    flightComputer = spFliCom_t.GetValue(signalProcessor, null);

                    if (flightComputer == null)
                    {
                        return ACFlightMode.Off;
                    }

                    currentFlightMode = fcAttCom_t.GetValue(flightComputer, null);

                    if (currentFlightMode == null)
                    {
                        return ACFlightMode.Off;
                    }

                    mode = acMode_t.GetValue(currentFlightMode);

                    if (mode == null)
                    {
                        return ACFlightMode.Off;
                    }

                    return acFlightModeMap[(int)mode];
                }
                else
                {
                    return ACFlightMode.Off;
                }
            }
            else
            {
                return ACFlightMode.Off;
            }
        }

        /* Returns false if FlightComputer is not controllable, in any other case true */
        public static bool Controllable(Vessel vessel)
        {
            object signalProcessor;
            object flightComputer;
            object controllable;

            if (rtAvailable)
            {
                signalProcessor = null;

                if (vessel.loaded && vessel.parts.Count > 0)
                {
                    var partModuleList = vessel.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Where(pm => pm.Fields.GetValue<bool>("IsRTSignalProcessor")).ToList();
                    if (partModuleList.Count > 0) //necessary?
                    {
                        signalProcessor = partModuleList.FirstOrDefault(pm => pm.moduleName == "ModuleSPU");
                    }

                    if (signalProcessor == null)
                    {
                        return true;
                    }

                    flightComputer = spFliCom_t.GetValue(signalProcessor, null);

                    if (flightComputer == null)
                    {
                        return true;
                    }

                    controllable = fcControllable_t.GetValue(flightComputer, null);

                    if (controllable == null)
                    {
                        return true;
                    }

                    return (bool)controllable;

                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }
}