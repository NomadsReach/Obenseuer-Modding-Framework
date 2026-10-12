using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class WorldItemsPatches
    {
        private static int dropDepth;
        private static bool raisingDropped;
        internal static int CoreSpawnDepth;

        private struct PickupStart
        {
            public int ItemId;
            public int Amount;
            public Vector3 Position;
        }

        internal static bool Install()
        {
            try
            {
                bool picked = Hook(typeof(CollectibleItem), "PerformPickup", Type.EmptyTypes, "PickupPrefix", "PickupPostfix", null);
                bool dropped = Hook(typeof(ItemData), "DropItem", new[] { typeof(int), typeof(bool) }, "DropPrefix", null, "DropFinalizer");
                bool spawned = Hook(typeof(ItemOperations), "SpawnItemToPosition", new[] { typeof(ItemStack), typeof(Vector3), typeof(RaycastHit), typeof(bool) }, null, "SpawnPostfix", null);
                bool used = Hook(typeof(Item), "InvokeAction", new[] { typeof(ItemData), typeof(Item.ItemAction), typeof(bool) }, "UsePrefix", "UsePostfix", null);
                return picked && dropped && spawned && used;
            }
            catch (Exception e)
            {
                CoreLog.For(WorldItems.Owner).Error("world item patch install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WorldItemsPatches), prefix), CorePatcher.Handler(typeof(WorldItemsPatches), postfix), CorePatcher.Handler(typeof(WorldItemsPatches), finalizer), true, WorldItems.Owner);
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

        private static void DropPrefix()
        {
            dropDepth++;
        }

        private static void DropFinalizer()
        {
            if (dropDepth > 0) dropDepth--;
        }

        private static void SpawnPostfix(ItemStack item, Vector3 spawnPosition)
        {
            if (dropDepth == 0 || raisingDropped || CoreSpawnDepth > 0 || !WorldItems.HasDroppedHandlers) return;
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
            __state = !worlduse && WorldItems.HasUsedHandlers && !Trading();
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
