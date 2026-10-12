using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class RenovationsPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(TenementUpgradeProgress), "FinishUpgrade", new[] { typeof(bool) }, null, CorePatcher.Handler(typeof(RenovationsPatches), "FinishPostfix"), null, false, Renovations.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "install", e);
                return false;
            }
        }

        private static void FinishPostfix(TenementUpgradeProgress __instance, bool silentUpgrade)
        {
            try
            {
                if (!silentUpgrade && __instance != null && __instance.upgrade != null) Renovations.RaiseFinished(Renovations.Describe(__instance));
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "FinishUpgrade postfix", e);
            }
        }
    }
}
