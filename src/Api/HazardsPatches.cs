using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class HazardsPatches
    {
        private const string RadiationOwner = Hazards.Owner + ".radiation";
        private const string HurtOwner = Hazards.Owner + ".hurt";

        internal static HazardGroup Install()
        {
            try
            {
                bool radiation = Hook(RadiationOwner, typeof(TriggerRadiation), "RadiationEnteredPrefix", "RadiationEnteredPostfix");
                bool hurt = Hook(HurtOwner, typeof(TriggerHurt), "HurtEnteredPrefix", "HurtEnteredPostfix");
                return (radiation ? HazardGroup.Radiation : HazardGroup.None) | (hurt ? HazardGroup.Hurt : HazardGroup.None);
            }
            catch (Exception e)
            {
                CoreLog.For(Hazards.Owner).Error("hazard hook install failed: " + e.ToString());
                return HazardGroup.None;
            }
        }

        private static bool Hook(string owner, Type type, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(
                type,
                "OnTriggerEnter",
                new[] { typeof(Collider) },
                CorePatcher.Handler(typeof(HazardsPatches), prefix),
                CorePatcher.Handler(typeof(HazardsPatches), postfix),
                null,
                true,
                owner);
        }

        private static void RadiationEnteredPrefix(bool ___playerIn, out bool __state)
        {
            __state = ___playerIn || !Hazards.Installed(HazardGroup.Radiation) || !Hazards.HasHandlers;
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
            __state = Hazards.Installed(HazardGroup.Hurt) && Hazards.HasHandlers ? ___currentStats.Count : int.MaxValue;
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
