using System;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class ShopStockPatches
    {
        private static MethodInfo addItemToSlot;

        internal static bool Install()
        {
            try
            {
                addItemToSlot = AccessTools.Method(typeof(Storage), "AddItemToStorageSlot");
                if (addItemToSlot == null)
                {
                    CorePatcher.DisableOwner(ShopStock.Owner);
                    return false;
                }
                bool ok = Hook(typeof(VendingMachine), "Start", Type.EmptyTypes, "VendingPostfix");
                ok &= Hook(typeof(VendingMachine), "OnLoadingGame", Type.EmptyTypes, "VendingPostfix");
                ok &= Hook(typeof(Storage), "OnLoadingGame", Type.EmptyTypes, "TraderPostfix");
                ok &= Hook(typeof(VendingMachine), "Buy", new[] { typeof(VendingMachine.VendingMachineItem) }, "VendingBoughtPostfix");
                ok &= Hook(typeof(VendingMachine), "RefillStock", Type.EmptyTypes, "VendingRefilledPostfix");
                return ok;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(ShopStock.Owner);
                ApiLog.Once(ShopStock.Owner, "shop stock install", e);
                return false;
            }
        }

        internal static bool AddToSlot(Storage storage, int slot, ItemReference reference, int amount)
        {
            return (bool)addItemToSlot.Invoke(storage, new object[] { reference, slot, amount, null, null });
        }

        private static bool Hook(Type type, string name, Type[] args, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, null, CorePatcher.Handler(typeof(ShopStockPatches), postfix), null, true, ShopStock.Owner);
        }

        private static void VendingPostfix(VendingMachine __instance)
        {
            try
            {
                ShopStock.VendingReady(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(ShopStock.Owner, "vending postfix", e);
            }
        }

        private static void VendingBoughtPostfix(VendingMachine __instance, VendingMachine.VendingMachineItem item)
        {
            try
            {
                ShopStock.VendingBought(__instance, item);
            }
            catch (Exception e)
            {
                ApiLog.Once(ShopStock.Owner, "vending buy postfix", e);
            }
        }

        private static void VendingRefilledPostfix(VendingMachine __instance)
        {
            try
            {
                ShopStock.VendingRefilled(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(ShopStock.Owner, "vending refill postfix", e);
            }
        }

        private static void TraderPostfix(Storage __instance)
        {
            try
            {
                ShopStock.TopUp(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(ShopStock.Owner, "trader load postfix", e);
            }
        }
    }
}
