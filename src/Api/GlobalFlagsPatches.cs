using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class GlobalFlagsPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo prefix = CorePatcher.Handler(typeof(GlobalFlagsPatches), "SetStatePrefix");
                MethodInfo postfix = CorePatcher.Handler(typeof(GlobalFlagsPatches), "SetStatePostfix");
                bool ok = Hook(new Type[] { typeof(string) }, prefix, postfix);
                ok &= Hook(new Type[] { typeof(string), typeof(bool) }, prefix, postfix);
                ok &= Hook(new Type[] { typeof(string), typeof(bool), typeof(object) }, prefix, postfix);
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(GlobalFlags.Owner).Error("global state hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type[] args, MethodInfo prefix, MethodInfo postfix)
        {
            return CorePatcher.PatchCore(typeof(GlobalState), "SetState", args, prefix, postfix, null, true, GlobalFlags.Owner);
        }

        private static void SetStatePrefix(string key, out int __state)
        {
            try
            {
                __state = GlobalFlags.HasHandlers ? (GlobalFlags.Read(key) ? 1 : 0) : -1;
            }
            catch (Exception e)
            {
                __state = -1;
                ApiLog.Once(GlobalFlags.Owner, "GlobalFlags.Changed prefix", e);
            }
        }

        private static void SetStatePostfix(string key, int __state)
        {
            try
            {
                if (__state < 0 || !GlobalFlags.HasHandlers) return;
                bool now = GlobalFlags.Read(key);
                if (now != (__state == 1)) GlobalFlags.RaiseChanged(key, now);
            }
            catch (Exception e)
            {
                ApiLog.Once(GlobalFlags.Owner, "GlobalFlags.Changed", e);
            }
        }
    }
}
