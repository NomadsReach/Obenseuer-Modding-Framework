using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class SaunasPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo prefix = CorePatcher.Handler(typeof(SaunasPatches), "TogglePrefix");
                return CorePatcher.PatchCore(typeof(Sauna), "TurnOn", Type.EmptyTypes, prefix, CorePatcher.Handler(typeof(SaunasPatches), "TurnedOnPostfix"), null, true, Saunas.Owner)
                    && CorePatcher.PatchCore(typeof(Sauna), "TurnOff", Type.EmptyTypes, prefix, CorePatcher.Handler(typeof(SaunasPatches), "TurnedOffPostfix"), null, true, Saunas.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Saunas.Owner, "install", e);
                return false;
            }
        }

        private static void TogglePrefix(Sauna __instance, out bool __state)
        {
            __state = __instance.On;
        }

        private static void TurnedOnPostfix(Sauna __instance, bool __state)
        {
            if (__state || !__instance.On) return;
            try
            {
                Saunas.RaiseTurnedOn(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Saunas.Owner, "TurnOn postfix", e);
            }
        }

        private static void TurnedOffPostfix(Sauna __instance, bool __state)
        {
            if (!__state || __instance.On) return;
            try
            {
                Saunas.RaiseTurnedOff(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Saunas.Owner, "TurnOff postfix", e);
            }
        }
    }
}
