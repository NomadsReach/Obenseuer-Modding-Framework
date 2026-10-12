using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public static class CorePatcher
    {
        private const BindingFlags AnyMethod = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const string ReservedPrefix = ModIds.CoreId;
        private static readonly Dictionary<string, Harmony> harmonies = new Dictionary<string, Harmony>();
        private static readonly HashSet<string> disabled = new HashSet<string>();
        private const int Inherit = -1;

        public static bool IsDisabled(string owner)
        {
            return disabled.Contains(owner);
        }

        public static bool Patch(MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner)
        {
            return Patch(target, prefix, postfix, finalizer, core, owner, Inherit);
        }

        public static bool Patch(MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner, int priority)
        {
            ValidatePublicOwner(owner);
            return PatchCore(target, prefix, postfix, finalizer, core, owner, priority, null);
        }

        public static bool Patch(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner)
        {
            return Patch(type, name, args, prefix, postfix, finalizer, core, owner, Inherit);
        }

        public static bool Patch(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner, int priority)
        {
            ValidatePublicOwner(owner);
            return PatchCore(type, name, args, prefix, postfix, finalizer, core, owner, priority);
        }

        internal static bool PatchCore(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner, int priority = Inherit, MethodInfo transpiler = null)
        {
            ValidateOwner(owner);
            try
            {
                MethodBase target = type.GetMethod(name, AnyMethod, null, args, null);
                if (target == null && !IsDisabled(owner) && Core.PatchingAllowed) return Fail(owner, core, type + "::" + name, new MissingMethodException(type.Name, name));
                return PatchCore(target, prefix, postfix, finalizer, core, owner, priority, transpiler);
            }
            catch (Exception e)
            {
                return Fail(owner, core, type + "::" + name, e);
            }
        }

        private static bool PatchCore(MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner, int priority, MethodInfo transpiler)
        {
            ValidateOwner(owner);
            if (IsDisabled(owner)) return false;
            if (!Core.PatchingAllowed)
            {
                CoreLog.For(owner).Warn("patch skipped, unsupported game build " + Core.GameBuild);
                return false;
            }
            try
            {
                if (target == null) throw new ArgumentNullException("target");
                HarmonyFor(owner).Patch(target, Wrap(prefix, priority), Wrap(postfix, priority), Wrap(transpiler, priority), Wrap(finalizer, priority), null);
                return true;
            }
            catch (Exception e)
            {
                return Fail(owner, core, target == null ? "null target" : target.DeclaringType + "::" + target.Name, e);
            }
        }

        private static void ValidatePublicOwner(string owner)
        {
            ValidateOwner(owner);
            if (owner.StartsWith(ReservedPrefix, StringComparison.OrdinalIgnoreCase) || owner.StartsWith(ModIds.LegacyCoreId, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("owner " + owner + " is reserved for the Obenseuer Modding Framework", "owner");
        }

        private static void ValidateOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentException("owner must be a non-empty mod id", "owner");
        }

        private static Harmony HarmonyFor(string owner)
        {
            Harmony harmony;
            if (!harmonies.TryGetValue(owner, out harmony))
            {
                harmony = new Harmony(owner);
                harmonies[owner] = harmony;
            }
            return harmony;
        }

        private static HarmonyMethod Wrap(MethodInfo method, int priority)
        {
            if (method == null) return null;
            HarmonyMethod harmony = new HarmonyMethod(method);
            if (priority != Inherit) harmony.priority = priority;
            return harmony;
        }

        private static bool Fail(string owner, bool core, string what, Exception e)
        {
            CoreLog.For(owner).Error("patch failed on " + what + ": " + e.GetType().Name + ": " + e.Message);
            if (core) DisableOwner(owner);
            return false;
        }

        internal static MethodInfo Handler(Type hooks, string name)
        {
            if (name == null) return null;
            MethodInfo method = hooks.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(hooks.Name, name);
            return method;
        }

        internal static void DisableOwner(string owner)
        {
            if (IsDisabled(owner)) return;
            disabled.Add(owner);
            Harmony harmony;
            if (harmonies.TryGetValue(owner, out harmony)) harmony.UnpatchSelf();
            CoreLog.For(owner).Error("core patch failed, all patches from " + owner + " removed and the owner disabled");
        }
    }
}
