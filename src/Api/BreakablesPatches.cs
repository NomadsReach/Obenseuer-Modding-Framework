using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class BreakablesPatches
    {
        private const string BreakOwner = Breakables.Owner + ".break";
        private const string RepairOwner = Breakables.Owner + ".repair";
        private const string LockOwner = Breakables.Owner + ".lock";
        private const string SmashOwner = Breakables.Owner + ".smash";
        private static readonly HashSet<Breakable> restoredRepairs = new HashSet<Breakable>();

        internal static BreakableGroup Install()
        {
            Events.SceneLoaded += OnSceneLoaded;
            try
            {
                bool broke = Hook(BreakOwner, typeof(Breakable), "Break", new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(DamageReceiver.DamageType), typeof(bool), typeof(bool) }, "BreakPrefix", "BreakPostfix");
                bool repaired = Hook(RepairOwner, typeof(Breakable), "Repair", Type.EmptyTypes, "RepairPrefix", "RepairPostfix");
                repaired &= Hook(RepairOwner, typeof(Breakable), "OnLoadingGame", Type.EmptyTypes, null, "LoadPostfix");
                bool lockBroke = Hook(LockOwner, typeof(BreakableLock), "Break", new[] { typeof(int), typeof(DamageReceiver.DamageType), typeof(bool) }, "LockBreakPrefix", "LockBreakPostfix");
                bool smashed = Hook(SmashOwner, typeof(SpawnItemOnBreak), "Damage", new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(DamageReceiver.DamageType) }, "SmashPrefix", "SmashPostfix");
                return (broke ? BreakableGroup.Break : BreakableGroup.None)
                    | (repaired ? BreakableGroup.Repair : BreakableGroup.None)
                    | (lockBroke ? BreakableGroup.LockBreak : BreakableGroup.None)
                    | (smashed ? BreakableGroup.Smash : BreakableGroup.None);
            }
            catch (Exception e)
            {
                CoreLog.For(Breakables.Owner).Error("breakable hook install failed: " + e.ToString());
                return BreakableGroup.None;
            }
        }

        private static void OnSceneLoaded(string scene)
        {
            restoredRepairs.RemoveWhere(breakable => breakable == null);
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(BreakablesPatches), prefix), CorePatcher.Handler(typeof(BreakablesPatches), postfix), null, true, owner);
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
            if (!Breakables.Installed(BreakableGroup.Repair)) return;
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
            if (!Breakables.Installed(BreakableGroup.Repair)) return;
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

        private static void LockBreakPrefix(BreakableLock __instance, out bool __state)
        {
            __state = Breakables.HasLockBrokenHandlers && __instance.strength > 0;
        }

        private static void LockBreakPostfix(BreakableLock __instance, bool __state)
        {
            try
            {
                if (!__state || __instance.strength > 0 || SaveGate.Loading || !Breakables.HasLockBrokenHandlers) return;
                Breakables.RaiseLockBroken(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Breakables.Owner, "Breakables.LockBroken", e);
            }
        }

        private static void SmashPrefix(SpawnItemOnBreak __instance, int damage, DamageReceiver.DamageType type, out SmashedArgs __state)
        {
            __state = null;
            try
            {
                if (!Breakables.HasSmashedHandlers || SaveGate.Loading || !__instance.CheckDamageType(type) || __instance.health - damage > 0) return;
                __state = new SmashedArgs(SceneIds.Of(__instance), __instance.transform.position, __instance.objectType == SpawnItemOnBreak.Type.Animal, SpawnItemId(__instance));
            }
            catch (Exception e)
            {
                ApiLog.Once(Breakables.Owner, "Breakables.Smashed", e);
            }
        }

        private static void SmashPostfix(SpawnItemOnBreak __instance, SmashedArgs __state)
        {
            try
            {
                if (__state == null || __instance.health > 0 || SaveGate.Loading || !Breakables.HasSmashedHandlers) return;
                Breakables.RaiseSmashed(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Breakables.Owner, "Breakables.Smashed", e);
            }
        }

        private static int SpawnItemId(SpawnItemOnBreak breakable)
        {
            if (breakable.itemToSpawn == null || breakable.itemToSpawn.Item == null || breakable.itemToSpawn.Item.ID == 0) return -1;
            return breakable.itemToSpawn.Item.ID;
        }
    }
}
