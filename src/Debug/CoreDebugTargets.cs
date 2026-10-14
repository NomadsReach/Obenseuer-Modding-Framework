using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class CoreDebugTargets
    {
        private static readonly string[] InternalTypes = { "Events", "SaveGate", "SaveData" };

        internal static List<MethodBase> Handlers(Assembly assembly, string filter, IEnumerable<MethodInfo> handlers)
        {
            List<MethodBase> found = new List<MethodBase>();
            foreach (MethodInfo handler in handlers)
            {
                if (handler != null && handler.DeclaringType.Assembly == assembly && Traceable(handler) && Eligible(handler.DeclaringType, filter)) found.Add(handler);
            }
            return found;
        }

        internal static List<MethodBase> Find(Assembly assembly, string filter, IEnumerable<MethodInfo> handlers)
        {
            List<MethodBase> found = new List<MethodBase>();
            foreach (Type type in Types(assembly))
            {
                if (!Eligible(type, filter)) continue;
                if (CoreDebugTrace.PublicType(type) && !type.Name.EndsWith("Info", StringComparison.Ordinal) && !type.Name.EndsWith("Args", StringComparison.Ordinal))
                {
                    AddMembers(found, type, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                }
                if (Array.IndexOf(InternalTypes, type.Name) >= 0)
                {
                    AddMembers(found, type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                }
                if (type.Name.EndsWith("Patches", StringComparison.Ordinal))
                {
                    AddMembers(found, type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
            }
            found.AddRange(Handlers(assembly, filter, handlers));
            return found;
        }

        private static Type[] Types(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                List<Type> loaded = new List<Type>();
                foreach (Type type in e.Types)
                {
                    if (type != null) loaded.Add(type);
                }
                return loaded.ToArray();
            }
        }

        private static bool Eligible(Type type, string filter)
        {
            string name = type.Name;
            return !type.IsGenericType && name.IndexOf('<') < 0 && name != "CoreLog" && name != "ApiLog" && !name.StartsWith("CoreDebug", StringComparison.Ordinal) && CoreDebugTrace.Matches(name, filter);
        }

        private static void AddMembers(List<MethodBase> found, Type type, BindingFlags flags)
        {
            bool staticClass = type.IsAbstract && type.IsSealed;
            foreach (MethodInfo method in type.GetMethods(flags | BindingFlags.DeclaredOnly))
            {
                if (!Traceable(method)) continue;
                bool accessor = method.IsSpecialName && !method.Name.StartsWith("set_", StringComparison.Ordinal) && !(staticClass && method.Name.StartsWith("get_", StringComparison.Ordinal));
                if (!accessor) found.Add(method);
            }
        }

        private static bool Traceable(MethodInfo method)
        {
            string name = method.Name;
            return !method.ContainsGenericParameters && !method.DeclaringType.IsGenericType && !method.IsAbstract && (method.Attributes & MethodAttributes.PinvokeImpl) == 0 &&(method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0 && name.IndexOf('<') < 0 && name != "ToString" && name != "Equals" && name != "GetHashCode";
        }
    }
}
