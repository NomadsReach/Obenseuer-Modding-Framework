using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class HazardsPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(TriggerRadiation), "RadiationEnteredPrefix", "RadiationEnteredPostfix");
                ok &= Hook(typeof(TriggerHurt), "HurtEnteredPrefix", "HurtEnteredPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Hazards.Owner).Error("hazard hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(
                type,
                "OnTriggerEnter",
                new[] { typeof(Collider) },
                CorePatcher.Handler(typeof(HazardsPatches), prefix),
                CorePatcher.Handler(typeof(HazardsPatches), postfix),
                null,
                true,
                Hazards.Owner);
        }

        private static void RadiationEnteredPrefix(bool ___playerIn, out bool __state)
        {
            __state = ___playerIn || !Hazards.HasHandlers;
        }

        private static void RadiationEnteredPostfix(TriggerRadiation __instance, bool __state, bool ___playerIn)
        {
            if (__state || !___playerIn) return;
            try
            {
                Hazards.RaiseRadiationEntered(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Hazards.Owner, "Hazards.Entered", e);
            }
        }

        private static void HurtEnteredPrefix(List<Stats> ___currentStats, out int __state)
        {
            __state = Hazards.HasHandlers ? ___currentStats.Count : int.MaxValue;
        }

        private static void HurtEnteredPostfix(TriggerHurt __instance, int __state, List<Stats> ___currentStats)
        {
            if (___currentStats.Count <= __state || ___currentStats[___currentStats.Count - 1].type != Stats.Type.Player) return;
            try
            {
                Hazards.RaiseHurtEntered(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Hazards.Owner, "Hazards.Entered", e);
            }
        }
    }
}
