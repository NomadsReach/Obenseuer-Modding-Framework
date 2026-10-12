using System;
using System.Reflection;
using PlayerStatsConditions;

namespace ObenseuerCore
{
    internal static class ConditionsPatches
    {
        internal static bool Install()
        {
            try
            {
                return Hook("ApplyCondition", new[] { typeof(bool), typeof(float) })
                    & Hook("RemoveCondition", Type.EmptyTypes);
            }
            catch (Exception e)
            {
                ApiLog.Once(Conditions.Owner, "install", e);
                CorePatcher.DisableOwner(Conditions.Owner);
                return false;
            }
        }

        private static bool Hook(string name, Type[] args)
        {
            return CorePatcher.PatchCore(typeof(BaseHealthCondition), name, args, CorePatcher.Handler(typeof(ConditionsPatches), "Before"), CorePatcher.Handler(typeof(ConditionsPatches), "After"), null, true, Conditions.Owner);
        }

        private static void Before(BaseHealthCondition __instance, out float __state)
        {
            __state = float.NaN;
            if (!Conditions.Watched) return;
            try
            {
                __state = Conditions.Level(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Conditions.Owner, "Before", e);
            }
        }

        private static void After(BaseHealthCondition __instance, float __state)
        {
            if (!Conditions.Watched) return;
            try
            {
                float after = Conditions.Level(__instance);
                if (float.IsNaN(after) && float.IsNaN(__state)) return;
                if (after == __state) return;
                bool active = !float.IsNaN(after);
                Conditions.RaiseChanged(new ConditionChangedArgs(__instance.name, active, active ? after : 0f));
            }
            catch (Exception e)
            {
                ApiLog.Once(Conditions.Owner, "After", e);
            }
        }
    }
}
