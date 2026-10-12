using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class WardrobePatches
    {

        internal static bool Install()
        {
            try
            {
                bool equipped = Hook(typeof(Inventory), "Equip", new[] { typeof(string), typeof(SlotController) }, "UiPrefix", "EquipPostfix", "UiFinalizer");
                bool unequipped = Hook(typeof(Inventory), "Unequip", new[] { typeof(SlotController) }, "UiPrefix", "UnequipPostfix", "UiFinalizer");
                bool taken = Hook(typeof(SlotController), "TakeItem", new[] { typeof(Item), typeof(int), typeof(int), typeof(object[]) }, "SlotPrefix", "SlotPostfix", null);
                bool dropped = Hook(typeof(SlotController), "DropItem", new[] { typeof(ItemData), typeof(int).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool) }, "SlotPrefix", "SlotPostfix", null);
                bool returned = Hook(typeof(ItemData), "Return", new[] { typeof(int) }, "ReturnPrefix", "ReturnPostfix", null);
                bool returnedOriginal = Hook(typeof(ItemData), "ReturnOriginal", Type.EmptyTypes, "ReturnPrefix", "ReturnPostfix", null);
                return equipped && unequipped && taken && dropped && returned && returnedOriginal;
            }
            catch (Exception e)
            {
                CoreLog.For(Wardrobe.Owner).Error("wardrobe patch install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(WardrobePatches), prefix), CorePatcher.Handler(typeof(WardrobePatches), postfix), CorePatcher.Handler(typeof(WardrobePatches), finalizer), true, Wardrobe.Owner);
        }

        private static void UiPrefix()
        {
            PlayerInventory.EnterUi();
        }

        private static void UiFinalizer()
        {
            PlayerInventory.ExitUi();
        }

        private static void EquipPostfix(string target)
        {
            if (!Wardrobe.HasHandlers) return;
            try
            {
                if (Wardrobe.IsSlot(target)) Wardrobe.RaiseChanged(target);
            }
            catch (Exception e)
            {
                ApiLog.Once(Wardrobe.Owner, "Changed postfix", e);
            }
        }

        private static void UnequipPostfix(SlotController item)
        {
            if (!Wardrobe.HasHandlers) return;
            try
            {
                string slot = Wardrobe.SlotOf(item);
                if (slot != null) Wardrobe.RaiseChanged(slot);
            }
            catch (Exception e)
            {
                ApiLog.Once(Wardrobe.Owner, "Changed postfix", e);
            }
        }

        private static void SlotPrefix(SlotController __instance, out int __state)
        {
            __state = -2;
            if (!Wardrobe.HasHandlers) return;
            try
            {
                __state = Wardrobe.StateOf(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Wardrobe.Owner, "Changed postfix", e);
            }
        }

        private static void SlotPostfix(SlotController __instance, int __state)
        {
            if (__state == -2) return;
            try
            {
                string slot = Wardrobe.SlotOf(__instance);
                if (slot != null && Wardrobe.StateOf(__instance) != __state) Wardrobe.RaiseChanged(slot);
            }
            catch (Exception e)
            {
                ApiLog.Once(Wardrobe.Owner, "Changed postfix", e);
            }
        }

        private static void ReturnPrefix(ItemData __instance, out int __state)
        {
            __state = -2;
            if (!Wardrobe.HasHandlers || __instance.slotController == null) return;
            SlotPrefix(__instance.slotController, out __state);
        }

        private static void ReturnPostfix(ItemData __instance, int __state)
        {
            SlotPostfix(__instance.slotController, __state);
        }
    }
}
