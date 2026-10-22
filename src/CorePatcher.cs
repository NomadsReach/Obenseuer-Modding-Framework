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
        private const int InnerMaxChars = 512;
        private const int InnerMaxDepth = 16;
        private const int InnerMaxParts = 6;
        private static readonly List<MethodInfo> installedHandlers = new List<MethodInfo>();

        internal static event Action<string, string, MethodInfo[], bool> Installed;

        internal static IList<MethodInfo> InstalledHandlers
        {
            get { return installedHandlers.AsReadOnly(); }
        }

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

        public static bool Patch(MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo transpiler, MethodInfo finalizer, bool core, string owner)
        {
            return Patch(target, prefix, postfix, transpiler, finalizer, core, owner, Inherit);
        }

        public static bool Patch(MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo transpiler, MethodInfo finalizer, bool core, string owner, int priority)
        {
            if (!AcceptOwner(owner)) return false;
            return PatchCore(target, prefix, postfix, finalizer, core, owner, priority, transpiler);
        }

        public static bool Patch(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo transpiler, MethodInfo finalizer, bool core, string owner)
        {
            return Patch(type, name, args, prefix, postfix, transpiler, finalizer, core, owner, Inherit);
        }

        public static bool Patch(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo transpiler, MethodInfo finalizer, bool core, string owner, int priority)
        {
            if (!AcceptOwner(owner)) return false;
            return PatchCore(type, name, args, prefix, postfix, finalizer, core, owner, priority, transpiler);
        }

        /// <summary>
        /// Adds a static prefix handler to the method found by name, returning false on failure.
        /// </summary>
        public static bool Prefix(Type target, string method, Type handlerType, string handlerName, string owner)
        {
            return PatchNamed(target, method, null, Handler(handlerType, handlerName), null, null, owner);
        }

        /// <summary>
        /// Adds a static prefix handler to the method with the given parameter types, returning false on failure.
        /// </summary>
        public static bool Prefix(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner)
        {
            return PatchNamed(target, method, args, Handler(handlerType, handlerName), null, null, owner);
        }

        /// <summary>
        /// Adds a static postfix handler to the method found by name, returning false on failure.
        /// </summary>
        public static bool Postfix(Type target, string method, Type handlerType, string handlerName, string owner)
        {
            return PatchNamed(target, method, null, null, Handler(handlerType, handlerName), null, owner);
        }

        /// <summary>
        /// Adds a static postfix handler to the method with the given parameter types, returning false on failure.
        /// </summary>
        public static bool Postfix(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner)
        {
            return PatchNamed(target, method, args, null, Handler(handlerType, handlerName), null, owner);
        }

        /// <summary>
        /// Adds a static finalizer handler to the method found by name, returning false on failure.
        /// </summary>
        public static bool Finalizer(Type target, string method, Type handlerType, string handlerName, string owner)
        {
            return PatchNamed(target, method, null, null, null, Handler(handlerType, handlerName), owner);
        }

        /// <summary>
        /// Adds a static finalizer handler to the method with the given parameter types, returning false on failure.
        /// </summary>
        public static bool Finalizer(Type target, string method, Type[] args, Type handlerType, string handlerName, string owner)
        {
            return PatchNamed(target, method, args, null, null, Handler(handlerType, handlerName), owner);
        }

        private static bool PatchNamed(Type target, string method, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, string owner)
        {
            ValidatePublicOwner(owner);
            if (args != null) return Patch(target, method, args, prefix, postfix, finalizer, false, owner);
            MethodBase found = UniqueNamed(target, method);
            if (found == null)
            {
                ApiLog.Once(owner, "patch " + target + "::" + method, "the name is missing or overloaded, pass the parameter types with the Type[] args overload");
                return false;
            }
            return Patch(found, prefix, postfix, finalizer, false, owner);
        }

        internal static MethodBase UniqueNamed(Type target, string method)
        {
            MethodBase found = null;
            foreach (MethodInfo candidate in target.GetMethods(AnyMethod))
            {
                if (candidate.Name != method) continue;
                if (found != null) return null;
                found = candidate;
            }
            return found;
        }

        internal static bool PatchCore(Type type, string name, Type[] args, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer, bool core, string owner, int priority = Inherit, MethodInfo transpiler = null)
        {
            ValidateOwner(owner);
            try
            {
                MethodBase target = name == ".ctor" ? (MethodBase)type.GetConstructor(AnyMethod, null, args, null) : type.GetMethod(name, AnyMethod, null, args, null);
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
            MethodInfo[] handlers = new[] { prefix, postfix, transpiler, finalizer };
            string what = target == null ? "null target" : target.DeclaringType + "::" + target.Name;
            try
            {
                if (target == null) throw new ArgumentNullException("target");
                HarmonyFor(owner).Patch(target, Wrap(prefix, priority), Wrap(postfix, priority), Wrap(transpiler, priority), Wrap(finalizer, priority), null);
            }
            catch (Exception e)
            {
                Report(owner, what, handlers, false);
                return Fail(owner, core, what, e);
            }
            Report(owner, what, handlers, true);
            return true;
        }

        private static bool AcceptOwner(string owner)
        {
            string logOwner = string.IsNullOrEmpty(owner) ? "ObenseuerCore" : owner;
            if (string.IsNullOrEmpty(owner))
            {
                ApiLog.Once(logOwner, "transpiler patch owner", "owner must be a non-empty mod id, no change was made");
                return false;
            }
            if (owner.StartsWith(ReservedPrefix, StringComparison.OrdinalIgnoreCase) || owner.StartsWith(ModIds.LegacyCoreId, StringComparison.OrdinalIgnoreCase))
            {
                ApiLog.Once(logOwner, "transpiler patch owner", "owner " + owner + " is reserved for the Obenseuer Modding Framework, no change was made");
                return false;
            }
            return true;
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
                harmony = new Harmony("core:" + owner);
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

        private static void Report(string owner, string what, MethodInfo[] handlers, bool ok)
        {
            if (ok)
            {
                foreach (MethodInfo handler in handlers)
                {
                    if (handler != null) installedHandlers.Add(handler);
                }
            }
            if (Installed != null) Installed(owner, what, handlers, ok);
        }

        private static bool Fail(string owner, bool core, string what, Exception e)
        {
            string reason = what + ": " + e.GetType().Name + ": " + e.Message + Inner(e);
            CoreLog.For(owner).Error("patch failed on " + reason);
            if (core) DisableOwner(owner, reason);
            return false;
        }

        private static string Inner(Exception e)
        {
            List<string> parts = new List<string>();
            int length = 0;
            AggregateException group = e as AggregateException;
            if (group != null)
            {
                foreach (Exception item in group.InnerExceptions) AddInner(parts, item, ref length);
            }
            else
            {
                AddInner(parts, e.InnerException, ref length);
            }
            if (parts.Count == 0) return string.Empty;
            return " | inner: " + string.Join("; ", parts.ToArray());
        }

        private static void AddInner(List<string> parts, Exception e, ref int length)
        {
            int depth = 0;
            while (e != null && depth < InnerMaxDepth && parts.Count < InnerMaxParts && length < InnerMaxChars)
            {
                string text = Flatten(e.GetType().Name + ": " + e.Message);
                if (text.Length > InnerMaxChars - length) text = text.Substring(0, Math.Max(0, InnerMaxChars - length));
                if (!parts.Contains(text))
                {
                    parts.Add(text);
                    length += text.Length;
                }
                e = e.InnerException;
                depth++;
            }
        }

        private static string Flatten(string text)
        {
            return text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        }

        internal static MethodInfo Handler(Type hooks, string name)
        {
            if (name == null) return null;
            MethodInfo method = hooks.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(hooks.Name, name);
            return method;
        }

        internal static void DisableOwner(string owner, string reason = null)
        {
            if (IsDisabled(owner)) return;
            disabled.Add(owner);
            Harmony harmony;
            if (harmonies.TryGetValue(owner, out harmony)) harmony.UnpatchSelf();
            CoreLog.For(owner).Error("core patch failed, all patches from " + owner + " removed and the owner disabled" + (reason == null ? "" : ": " + reason));
        }
    }
}
