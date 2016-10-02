using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace PersistentRotation
{
    public static class Reflection
    {
        /* UTILITY METHODS */
        internal static Type GetExportedType(string assemblyName, string fullTypeName)
        {
            int assyCount = AssemblyLoader.loadedAssemblies.Count;
            for (int assyIndex = 0; assyIndex < assyCount; ++assyIndex)
            {
                AssemblyLoader.LoadedAssembly assy = AssemblyLoader.loadedAssemblies[assyIndex];
                if (assy.name == assemblyName)
                {
                    Type[] exportedTypes = assy.assembly.GetExportedTypes();
                    int typeCount = exportedTypes.Length;
                    for (int typeIndex = 0; typeIndex < typeCount; ++typeIndex)
                    {
                        if (exportedTypes[typeIndex].FullName == fullTypeName)
                        {
                            return exportedTypes[typeIndex];
                        }
                    }
                }
            }

            return null;
        }

        public delegate object DynamicMethod<T>(T param0);
        public delegate object DynamicMethod<T, U>(T param0, U parmam1);
        public delegate bool DynamicMethodBool<T>(T param0);

        static internal DynamicMethod<T> CreateFunc<T>(MethodInfo methodInfo)
        {
            // Up front validation:
            ParameterInfo[] parms = methodInfo.GetParameters();
            if (methodInfo.IsStatic)
            {
                if (parms.Length != 1)
                {
                    throw new ArgumentException("CreateFunc<T> called with static method that takes " + parms.Length + " parameters");
                }

                if (typeof(T) != parms[0].ParameterType)
                {
                    // What to do?
                }
            }
            else
            {
                if (parms.Length != 0)
                {
                    throw new ArgumentException("CreateFunc<T> called with non-static method that takes " + parms.Length + " parameters");
                }
            }

            Type[] _argTypes = { typeof(T) };

            // Create dynamic method and obtain its IL generator to
            // inject code.
            DynamicMethod dynam =
                new DynamicMethod(
                "", // name - don't care
                typeof(object), // return type
                _argTypes, // argument types
                typeof(Reflection));
            ILGenerator il = dynam.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            // Perform actual call.
            // If method is not final a callvirt is required
            // otherwise a normal call will be emitted.
            if (methodInfo.IsFinal)
            {
                il.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                // If result is of value type it needs to be boxed
                if (methodInfo.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, methodInfo.ReturnType);
                }
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            // Emit return opcode.
            il.Emit(OpCodes.Ret);


            return (DynamicMethod<T>)dynam.CreateDelegate(typeof(DynamicMethod<T>));
        }
        static internal DynamicMethod<T, U> CreateFunc<T, U>(MethodInfo methodInfo)
        {
            // Up front validation:
            ParameterInfo[] parms = methodInfo.GetParameters();
            if (methodInfo.IsStatic)
            {
                if (parms.Length != 2)
                {
                    throw new ArgumentException("CreateFunc<T, U> called with static method that takes " + parms.Length + " parameters");
                }

                if (typeof(T) != parms[0].ParameterType)
                {
                    // What to do?
                }
                if (typeof(U) != parms[1].ParameterType)
                {
                    // What to do?
                }
            }
            else
            {
                if (parms.Length != 1)
                {
                    throw new ArgumentException("CreateFunc<T, U> called with non-static method that takes " + parms.Length + " parameters");
                }
                if (typeof(U) != parms[0].ParameterType)
                {
                    // What to do?
                }
            }

            Type[] _argTypes = { typeof(T), typeof(U) };

            // Create dynamic method and obtain its IL generator to
            // inject code.
            DynamicMethod dynam =
                new DynamicMethod(
                "", // name - don't care
                typeof(object), // return type
                _argTypes, // argument types
                typeof(Reflection));
            ILGenerator il = dynam.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);

            // Perform actual call.
            // If method is not final a callvirt is required
            // otherwise a normal call will be emitted.
            if (methodInfo.IsFinal)
            {
                il.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                // If result is of value type it needs to be boxed
                if (methodInfo.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, methodInfo.ReturnType);
                }
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            // Emit return opcode.
            il.Emit(OpCodes.Ret);


            return (DynamicMethod<T, U>)dynam.CreateDelegate(typeof(DynamicMethod<T, U>));
        }
        static internal DynamicMethodBool<T> CreateFuncBool<T>(MethodInfo methodInfo)
        {
            // Up front validation:
            ParameterInfo[] parms = methodInfo.GetParameters();
            if (methodInfo.IsStatic)
            {
                if (parms.Length != 1)
                {
                    throw new ArgumentException("CreateFunc<T> called with static method that takes " + parms.Length + " parameters");
                }

                if (typeof(T) != parms[0].ParameterType)
                {
                    // What to do?
                }
            }
            else
            {
                if (parms.Length != 0)
                {
                    throw new ArgumentException("CreateFunc<T, U> called with non-static method that takes " + parms.Length + " parameters");
                }
                // How do I validate T?
                //if (typeof(T) != parms[0].ParameterType)
                //{
                //    // What to do?
                //}
            }
            if (methodInfo.ReturnType != typeof(bool))
            {
                throw new ArgumentException("CreateFunc<T> called with method that returns void");
            }

            Type[] _argTypes = { typeof(T) };

            // Create dynamic method and obtain its IL generator to
            // inject code.
            DynamicMethod dynam =
                new DynamicMethod(
                "", // name - don't care
                methodInfo.ReturnType, // return type
                _argTypes, // argument types
                typeof(Reflection));
            ILGenerator il = dynam.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            // Perform actual call.
            // If method is not final a callvirt is required
            // otherwise a normal call will be emitted.
            if (methodInfo.IsFinal)
            {
                il.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }

            // Emit return opcode.
            il.Emit(OpCodes.Ret);


            return (DynamicMethodBool<T>)dynam.CreateDelegate(typeof(DynamicMethodBool<T>));
        }
    }
}