using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class TarpDoorsPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo prefix = CorePatcher.Handler(typeof(TarpDoorsPatches), "TogglePrefix");
                MethodInfo postfix = CorePatcher.Handler(typeof(TarpDoorsPatches), "TogglePostfix");
                return CorePatcher.PatchCore(typeof(TarpDoor), "Toggle", new[] { typeof(bool) }, prefix, postfix, null, true, TarpDoors.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(TarpDoors.Owner).Error("tarp door hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void TogglePrefix(bool ___opened, out bool __state)
        {
            __state = ___opened;
        }

        private static void TogglePostfix(TarpDoor __instance, bool ___opened, bool __state)
        {
            if (__state == ___opened || !TarpDoors.HasHandlers) return;
            try
            {
                TarpDoors.RaiseChanged(__instance, ___opened);
            }
            catch (Exception e)
            {
                ApiLog.Once(TarpDoors.Owner, "TarpDoors.Changed", e);
            }
        }
    }
}
