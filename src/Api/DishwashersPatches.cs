using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class DishwashersPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(manufacturing), "CraftDelayed", new[] { typeof(TimeOfDayAzure.Timer) }, null, CorePatcher.Handler(typeof(DishwashersPatches), "CraftDelayedPostfix"), null, false, Dishwashers.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "install", e);
                return false;
            }
        }

        private static void CraftDelayedPostfix(manufacturing __instance)
        {
            if (!Dishwashers.HasHandlers) return;
            try
            {
                Dishwasher dishwasher = __instance as Dishwasher;
                if (dishwasher != null && !string.IsNullOrEmpty(dishwasher.GUID)) Dishwashers.RaiseFinished(Dishwashers.ToInfo(dishwasher));
            }
            catch (Exception e)
            {
                ApiLog.Once(Dishwashers.Owner, "CraftDelayed postfix", e);
            }
        }
    }
}
