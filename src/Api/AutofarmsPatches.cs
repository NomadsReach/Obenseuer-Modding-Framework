using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class AutofarmsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Autofarm), "Plant", Type.EmptyTypes, CorePatcher.Handler(typeof(AutofarmsPatches), "PlantPrefix"), CorePatcher.Handler(typeof(AutofarmsPatches), "PlantPostfix"), null, true, Autofarms.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Autofarms.Owner, "install", e);
                return false;
            }
        }

        private static void PlantPrefix(Autofarm __instance, out int __state)
        {
            __state = Autofarms.HasHandlers ? Autofarms.CountPlanted(__instance) : -1;
        }

        private static void PlantPostfix(Autofarm __instance, int __state, bool ___initDone)
        {
            try
            {
                if (__state < 0 || !Autofarms.HasHandlers) return;
                int planted = Autofarms.CountPlanted(__instance) - __state;
                if (planted > 0) Autofarms.RaiseReplanted(new ReplantedArgs(SceneIds.Of(__instance), planted, !___initDone));
            }
            catch (Exception e)
            {
                ApiLog.Once(Autofarms.Owner, "Plant postfix", e);
            }
        }
    }
}
