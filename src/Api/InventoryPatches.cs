using System;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class InventoryPatches
    {
        private static int droppedAmount;

        internal static bool Install()
        {
            try
            {
                bool added = Hook(typeof(SlotController), "AddItemAndReturnRemaining", new[] { typeof(Item), typeof(int), typeof(int), typeof(object[]), typeof(bool), typeof(GameObject), typeof(bool), typeof(int), typeof(ItemOperations.DropItemValues), typeof(ItemOperations.ItemOperationSettings) }, "AddedPrefix", "AddedPostfix", null);
                bool taken = Hook(typeof(SlotController), "TakeItem", new[] { typeof(Item), typeof(int), typeof(int), typeof(object[]) }, null, "TakenPostfix", null);
                bool dragged = Hook(typeof(SlotController), "TakeItem", new[] { typeof(ItemData) }, "UiPrefix", null, "UiFinalizer");
                bool dropped = Hook(typeof(SlotController), "DropItem", new[] { typeof(ItemData), typeof(int).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool) }, "UiPrefix", null, "UiFinalizer");
                bool floorDrop = Hook(typeof(ItemOperations), "DropItem", new[] { typeof(Item), typeof(int), typeof(int), typeof(object[]), typeof(GameObject), typeof(ItemOperations.DropItemValues) }, "DropPrefix", null, null);
                return added && taken && dragged && dropped && floorDrop;
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("inventory patch install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(InventoryPatches), prefix), CorePatcher.Handler(typeof(InventoryPatches), postfix), CorePatcher.Handler(typeof(InventoryPatches), finalizer), true, PlayerInventory.Owner);
        }

        private static void UiPrefix()
        {
            PlayerInventory.EnterUi();
        }

        private static void UiFinalizer()
        {
            PlayerInventory.ExitUi();
        }

        private static void AddedPrefix(out int __state)
        {
            __state = droppedAmount;
        }

        private static void DropPrefix(int amount)
        {
            droppedAmount += amount;
        }

        private static void AddedPostfix(SlotController __instance, Item item, int amount, int __result, int __state)
        {
            if (!PlayerInventory.HasHandlers) return;
            try
            {
                int added = amount - __result - (droppedAmount - __state);
                if (added > 0 && !PlayerInventory.UiActive && PlayerInventory.IsPlayerSlot(__instance)) PlayerInventory.RaiseItemAdded(item.ID, added);
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "ItemAdded postfix", e);
            }
        }

        private static void TakenPostfix(SlotController __instance, Item item, int amount)
        {
            if (!PlayerInventory.HasHandlers) return;
            try
            {
                if (amount > 0 && !PlayerInventory.UiActive && PlayerInventory.IsPlayerSlot(__instance)) PlayerInventory.RaiseItemRemoved(item.ID, amount);
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "ItemRemoved postfix", e);
            }
        }
    }
}
