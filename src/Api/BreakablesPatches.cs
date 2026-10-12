using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class BreakablesPatches
    {
        private static readonly HashSet<Breakable> restoredRepairs = new HashSet<Breakable>();

        internal static bool Install()
        {
            try
            {
                bool broke = Hook("Break", new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(DamageReceiver.DamageType), typeof(bool), typeof(bool) }, "BreakPrefix", "BreakPostfix");
                bool repaired = Hook("Repair", Type.EmptyTypes, "RepairPrefix", "RepairPostfix");
                bool loaded = Hook("OnLoadingGame", Type.EmptyTypes, null, "LoadPostfix");
                return broke && repaired && loaded;
            }
            catch (Exception e)
            {
                CoreLog.For(Breakables.Owner).Error("breakable hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(typeof(Breakable), name, args, CorePatcher.Handler(typeof(BreakablesPatches), prefix), CorePatcher.Handler(typeof(BreakablesPatches), postfix), null, true, Breakables.Owner);
        }

        private static void BreakPrefix(Breakable __instance, out bool __state)
        {
            __state = Breakables.HasBrokenHandlers && __instance.strength <= 0;
        }

        private static void BreakPostfix(Breakable __instance, bool __state, bool onLoadingGame)
        {
            try
            {
                if (__state || onLoadingGame || __instance.strength > 0 || SaveGate.Loading || !Breakables.HasBrokenHandlers) return;
                Breakables.RaiseBroken(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Breakables.Owner, "Breakables.Broken", e);
            }
        }

        private static void RepairPrefix(Breakable __instance, out bool __state)
        {
            __state = Breakables.HasRepairedHandlers && __instance.strength <= 0;
        }

        private static void RepairPostfix(Breakable __instance, bool __state)
        {
            try
            {
                bool restored = restoredRepairs.Remove(__instance);
                if (!__state || restored || __instance.strength <= 0 || SaveGate.Loading || !Breakables.HasRepairedHandlers) return;
                Breakables.RaiseRepaired(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Breakables.Owner, "Breakables.Repaired", e);
            }
        }

        private static void LoadPostfix(Breakable __instance, bool __result)
        {
            try
            {
                restoredRepairs.Remove(__instance);
                if (__result && __instance.repairAfterTime > 0 && __instance.timer != null && __instance.timer.timeInSeconds > 0 && __instance.timer.remainingInSeconds <= 0 && !__instance.timer.stopped) restoredRepairs.Add(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Breakables.Owner, "Breakables.Load", e);
            }
        }
    }
}
