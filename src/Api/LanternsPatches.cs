using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class LanternsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Lantern), "CandleExtinguished", Type.EmptyTypes, CorePatcher.Handler(typeof(LanternsPatches), "CandleExtinguishedPrefix"), CorePatcher.Handler(typeof(LanternsPatches), "CandleExtinguishedPostfix"), null, true, Lanterns.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Lanterns.Owner, "install", e);
                return false;
            }
        }

        private static void CandleExtinguishedPrefix(Lantern __instance, out bool __state)
        {
            __state = __instance.candleTimer != null && __instance.candleTimer.remainingInSeconds < 0;
        }

        private static void CandleExtinguishedPostfix(Lantern __instance, bool __state)
        {
            if (!__state) return;
            try
            {
                Lanterns.RaiseCandleBurntOut(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Lanterns.Owner, "CandleExtinguished postfix", e);
            }
        }
    }
}
