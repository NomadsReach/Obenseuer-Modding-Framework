using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class TelevisionsPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo prefix = CorePatcher.Handler(typeof(TelevisionsPatches), "StatePrefix");
                MethodInfo postfix = CorePatcher.Handler(typeof(TelevisionsPatches), "StatePostfix");
                return CorePatcher.PatchCore(typeof(TelevisionNoise), "TurnOn", new[] { typeof(bool), typeof(bool) }, prefix, postfix, null, true, Televisions.Owner)
                    && CorePatcher.PatchCore(typeof(TelevisionNoise), "TurnOff", new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) }, prefix, postfix, null, true, Televisions.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Televisions.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void StatePrefix(TelevisionNoise __instance, out bool __state)
        {
            __state = false;
            if (!Televisions.HasHandlers) return;
            try
            {
                __state = Televisions.IsOn(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Televisions.Owner, "Televisions.Changed", e);
            }
        }

        private static void StatePostfix(TelevisionNoise __instance, bool __state)
        {
            if (!Televisions.HasHandlers) return;
            try
            {
                if (__state != Televisions.IsOn(__instance)) Televisions.RaiseChanged(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Televisions.Owner, "Televisions.Changed", e);
            }
        }
    }
}
