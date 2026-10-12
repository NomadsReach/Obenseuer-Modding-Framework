using System;
using System.Reflection;
using NPC;

namespace ObenseuerCore
{
    internal static class NpcSchedulesPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(NPCDataState), "set_CurrentActivity", new[] { typeof(Activity) }, CorePatcher.Handler(typeof(NpcSchedulesPatches), "ActivityPrefix"), CorePatcher.Handler(typeof(NpcSchedulesPatches), "ActivityPostfix"), null, true, NpcSchedules.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(NpcSchedules.Owner, "install", e);
                return false;
            }
        }

        private static void ActivityPrefix(NPCDataState __instance, out Activity __state)
        {
            __state = null;
            if (!NpcSchedules.HasHandlers) return;
            __state = __instance.CurrentActivity;
        }

        private static void ActivityPostfix(NPCDataState __instance, Activity __state)
        {
            try
            {
                NpcSchedules.RaiseActivityChanged(__instance, __state);
            }
            catch (Exception e)
            {
                ApiLog.Once(NpcSchedules.Owner, "CurrentActivity postfix", e);
            }
        }
    }
}
