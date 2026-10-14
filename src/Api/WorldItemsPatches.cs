using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class WorldItemsPatches
    {
        private static int dropDepth;
        private static int actionsDepth;
        private static bool raisingDropped;
        internal static HashSet<InstantiatedPrefab> SavedEntries = new HashSet<InstantiatedPrefab>();

        private struct PickupStart
        {
            public int ItemId;
            public int Amount;
            public Vector3 Position;
        }

        private struct ActionsState
        {
            public bool Raise;
            public bool Counted;
        }

        private const string PickupOwner = WorldItems.Owner + ".pickup";
        private const string DropOwner = WorldItems.Owner + ".drop";
        private const string UsedOwner = WorldItems.Owner + ".used";
        private const string SavedOwner = WorldItems.Owner + ".saved";

        internal static WorldItemGroup Install()
        {
            try
            {
                bool picked = Hook(PickupOwner, typeof(CollectibleItem), "PerformPickup", Type.EmptyTypes, "PickupPrefix", "PickupPostfix", null);
                bool dropped = Hook(DropOwner, typeof(ItemData), "DropItem", new[] { typeof(int), typeof(bool) }, "DropPrefix", null, "DropFinalizer");
                dropped &= Hook(DropOwner, typeof(ItemOperations), "SpawnItemToPosition", new[] { typeof(ItemStack), typeof(Vector3), typeof(RaycastHit), typeof(bool) }, null, "SpawnPostfix", null);
                bool used = Hook(UsedOwner, typeof(Item), "InvokeAction", new[] { typeof(ItemData), typeof(Item.ItemAction), typeof(bool) }, "UsePrefix", "UsePostfix", null);
                used &= Hook(UsedOwner, typeof(Item), "InvokeActions", new[] { typeof(ItemData), typeof(bool) }, "ActionsPrefix", "ActionsPostfix", "ActionsFinalizer");
                bool loaded = Hook(SavedOwner, typeof(DestructibleList), "OnLoadingGameDestructibleList", Type.EmptyTypes, null, "LoadPostfix", null);
                return (picked ? WorldItemGroup.PickedUp : WorldItemGroup.None)
                    | (dropped ? WorldItemGroup.Dropped : WorldItemGroup.None)
                    | (used ? WorldItemGroup.Used : WorldItemGroup.None)
                    | (loaded ? WorldItemGroup.Saved : WorldItemGroup.None);
            }
            catch (Exception e)
            {
                CoreLog.For(WorldItems.Owner).Error("world item patch install failed: " + e.ToString());
                return WorldItemGroup.None;
            }
        }

        internal static bool InstallExpiry()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Collectible), "StartExpirationTimer", new[] { typeof(int?) }, CorePatcher.Handler(typeof(WorldItemsPatches), "ExpiryPrefix"), null, null, true, WorldItems.ExpiryOwner);
            }
            catch (Exception e)
            {
                CoreLog.For(WorldItems.ExpiryOwner).Error("expiry patch install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WorldItemsPatches), prefix), CorePatcher.Handler(typeof(WorldItemsPatches), postfix), CorePatcher.Handler(typeof(WorldItemsPatches), finalizer), true, owner);
        }

        private static void ExpiryPrefix(ref int? overrideTime)
        {
            try
            {
                if (overrideTime == null && WorldItems.Installed(WorldItemGroup.Expiry)) overrideTime = WorldItems.ExpiryOverride;
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldItems.ExpiryOwner, "StartExpirationTimer prefix", e);
            }
        }

        private static void PickupPrefix(CollectibleItem __instance, out PickupStart __state)
        {
            __state = new PickupStart();
            if (!WorldItems.HasPickedUpHandlers) return;
            try
            {
                __state.ItemId = __instance.Item.ID;
                __state.Position = __instance.transform.position;
                __state.Amount = __instance.amount;
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldItems.Owner, "PickedUp postfix", e);
            }
        }

        private static void PickupPostfix(bool __result, PickupStart __state)
        {
            if (!__result || __state.Amount <= 0 || !WorldItems.HasPickedUpHandlers) return;
            try
            {
                WorldItems.RaisePickedUp(__state.ItemId, __state.Amount, __state.Position);
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldItems.Owner, "PickedUp postfix", e);
            }
        }

        private static void LoadPostfix(DestructibleList __instance, bool __result)
        {
            if (!WorldItems.Installed(WorldItemGroup.Saved)) return;
            try
            {
                SavedEntries = __result && __instance.prefabs != null ? new HashSet<InstantiatedPrefab>(__instance.prefabs) : new HashSet<InstantiatedPrefab>();
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldItems.Owner, "DestructibleList load postfix", e);
            }
        }

        private static void DropPrefix()
        {
            if (!WorldItems.Installed(WorldItemGroup.Dropped)) return;
            dropDepth++;
        }

        private static void DropFinalizer()
        {
            if (dropDepth > 0) dropDepth--;
        }

        private static void SpawnPostfix(ItemStack item, Vector3 spawnPosition)
        {
            if (dropDepth == 0 || raisingDropped || !WorldItems.HasDroppedHandlers) return;
            raisingDropped = true;
            try
            {
                WorldItems.RaiseDropped(item.itemId, item.itemAmount, spawnPosition);
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldItems.Owner, "Dropped postfix", e);
            }
            finally
            {
                raisingDropped = false;
            }
        }

        private static void UsePrefix(bool worlduse, out bool __state)
        {
            __state = !worlduse && WorldItems.HasUsedHandlers && actionsDepth == 0 && !Trading();
        }

        private static void ActionsPrefix(Item __instance, bool worlduse, out ActionsState __state)
        {
            if (!WorldItems.Installed(WorldItemGroup.Used))
            {
                __state = new ActionsState();
                return;
            }
            bool raise;
            UsePrefix(worlduse, out raise);
            __state = new ActionsState { Raise = raise && __instance.Actions != null && __instance.Actions.Length > 0, Counted = true };
            actionsDepth++;
        }

        private static void ActionsPostfix(Item __instance, ActionsState __state)
        {
            if (!__state.Raise || !WorldItems.HasUsedHandlers) return;
            try
            {
                WorldItems.RaiseUsed(__instance.ID);
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldItems.Owner, "Used postfix", e);
            }
        }

        private static void ActionsFinalizer(ActionsState __state)
        {
            if (__state.Counted && actionsDepth > 0) actionsDepth--;
        }

        private static void UsePostfix(Item __instance, bool __state)
        {
            if (!__state || !WorldItems.HasUsedHandlers) return;
            try
            {
                WorldItems.RaiseUsed(__instance.ID);
            }
            catch (Exception e)
            {
                ApiLog.Once(WorldItems.Owner, "Used postfix", e);
            }
        }

        private static bool Trading()
        {
            return TradePanel.instance != null && TradePanel.instance.activeTrade != null;
        }
    }
}
