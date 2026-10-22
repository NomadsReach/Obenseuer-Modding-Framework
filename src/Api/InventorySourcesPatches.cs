using System;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class InventorySourcesPatches
    {
        private const string Blueprint = "FurnitureBlueprint";
        private const string Renovation = "Renovation";

        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(global::Inventory), "CheckForItem", new[] { typeof(int), typeof(float), typeof(bool), typeof(bool), typeof(int), typeof(bool), typeof(bool) }, null, "CheckPostfix", null);
                ok &= Hook(typeof(global::Inventory), "GetItemAmountInInventory", new[] { typeof(int), typeof(bool) }, null, "AmountPostfix", null);
                ok &= Hook(typeof(global::Inventory), "FindAndGetTotalAmmo", new[] { typeof(int), typeof(bool) }, null, "AmmoPostfix", null);
                ok &= Hook(typeof(global::Inventory), "FindAndChangeItemAmmo", new[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool) }, "ChangeAmmoPrefix", null, null);
                ok &= Hook(typeof(global::Inventory), "RemoveItems", new[] { typeof(int), typeof(float), typeof(bool), typeof(int), typeof(bool), typeof(bool) }, "RemoveItemsPrefix", null, null);
                ok &= Hook(typeof(TenementController), "RemoveRequiredItems", new[] { typeof(int), typeof(int) }, "RemoveRequiredPrefix", null, null, Priority.First);
                ok &= Hook(typeof(TaskItemBlueprint), "PlayerHasResources", Type.EmptyTypes, "BlueprintCheckPrefix", null, "ScopeFinalizer");
                ok &= Hook(typeof(ShopBaseUI), "Craft", Type.EmptyTypes, "BlueprintTakePrefix", null, "ScopeFinalizer");
                ok &= Hook(typeof(FurnitureResource), "AddResourceInfo", new[] { typeof(Furniture.ReseourceItem), typeof(bool), typeof(bool) }, "FurnitureResourcePrefix", null, "ScopeFinalizer");
                ok &= Hook(typeof(TenementController), "CheckForItem", new[] { typeof(int), typeof(float), typeof(TenementContractor) }, "RenovationCheckPrefix", null, "ScopeFinalizer");
                ok &= Hook(typeof(TenementController), "GetItemAmount", new[] { typeof(int), typeof(TenementContractor) }, "RenovationAmountPrefix", null, "ScopeFinalizer");
                ok &= Hook(typeof(TenementUpgrade), "TakeRequiredItemsAndMoney", Type.EmptyTypes, "RenovationTakePrefix", null, "ScopeFinalizer");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(InventorySourcesPatches), prefix), CorePatcher.Handler(typeof(InventorySourcesPatches), postfix), CorePatcher.Handler(typeof(InventorySourcesPatches), finalizer), true, InventorySources.Owner);
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer, int priority)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(InventorySourcesPatches), prefix), CorePatcher.Handler(typeof(InventorySourcesPatches), postfix), CorePatcher.Handler(typeof(InventorySourcesPatches), finalizer), true, InventorySources.Owner, priority);
        }

        private static void CheckPostfix(global::Inventory __instance, int id, float amount, bool checkAmmo, bool ignoreOwner, int owner, bool liquid, bool ignoreBackpack, ref bool __result)
        {
            if (__result) return;
            try
            {
                if (!InventorySources.IsOpen) return;
                float num = amount > 0f ? amount : 1f;
                float total = 0f;
                SlotController[] slots = __instance.AllSlots(ignoreBackpack);
                for (int i = 0; i < slots.Length; i++)
                {
                    total += ItemOperations.GetItemAmountFromSlot(slots[i], id, checkAmmo, ignoreOwner, owner, liquid);
                }
                if (total + InventorySources.CountAmount(id, checkAmmo, ignoreOwner, owner, liquid) >= num) __result = true;
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "count sourced items", e);
            }
        }

        private static void AmountPostfix(int id, ref int __result)
        {
            try
            {
                if (!InventorySources.IsOpen) return;
                __result += InventorySources.Count(id, false, true, -1, false);
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "count sourced items", e);
            }
        }

        private static void AmmoPostfix(int id, ref int __result)
        {
            try
            {
                if (!InventorySources.IsOpen) return;
                __result += InventorySources.CountAmmo(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "count sourced ammo", e);
            }
        }

        private static bool ChangeAmmoPrefix(int id, int ammoAmount, bool ignoreOwner, bool ignoreOwned, ref bool __result)
        {
            try
            {
                if (!InventorySources.Consuming) return true;
                if (!InventorySources.TakeAmmo(id, ammoAmount, ignoreOwner, ignoreOwned)) return true;
                __result = true;
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "take sourced ammo", e);
                return true;
            }
        }

        private static bool RemoveItemsPrefix(int id, ref float amount, bool ignoreOwner, int owner)
        {
            try
            {
                if (!InventorySources.Consuming) return true;
                amount -= InventorySources.Take(id, amount, ignoreOwner, owner);
                if (amount <= 0f) return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "take sourced items", e);
            }
            return true;
        }

        private static bool RemoveRequiredPrefix(int id, ref int amount)
        {
            try
            {
                if (!InventorySources.Consuming) return true;
                TenementController controller = TenementController.instance;
                if (controller == null || controller.ignoreUpgradePricesAndItems) return true;
                if (TweaksRenovations.MoneyOnly) return true;
                if (amount < 1) return true;
                amount -= (int)InventorySources.Take(id, amount, true, -1);
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "take sourced renovation items", e);
            }
            return true;
        }

        private static void BlueprintCheckPrefix(out InventorySources.Scope __state)
        {
            __state = OpenScope(Blueprint, false, "blueprint check scope");
        }

        private static void BlueprintTakePrefix(out InventorySources.Scope __state)
        {
            __state = OpenScope(Blueprint, true, "blueprint take scope");
        }

        private static void RenovationCheckPrefix(out InventorySources.Scope __state)
        {
            __state = OpenScope(Renovation, false, "renovation check scope");
        }

        private static void RenovationAmountPrefix(out InventorySources.Scope __state)
        {
            __state = OpenScope(Renovation, false, "renovation amount scope");
        }

        private static void FurnitureResourcePrefix(bool crafting, out InventorySources.Scope __state)
        {
            __state = crafting ? OpenScope(Blueprint, false, "furniture resource scope") : null;
        }

        private static void RenovationTakePrefix(out InventorySources.Scope __state)
        {
            __state = OpenScope(Renovation, true, "renovation take scope");
        }

        private static void ScopeFinalizer(InventorySources.Scope __state)
        {
            try
            {
                InventorySources.Close(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, "close scope", e);
            }
        }

        private static InventorySources.Scope OpenScope(string kind, bool consume, string what)
        {
            try
            {
                return InventorySources.Open(kind, null, consume);
            }
            catch (Exception e)
            {
                ApiLog.Once(InventorySources.Owner, what, e);
                return null;
            }
        }
    }
}
