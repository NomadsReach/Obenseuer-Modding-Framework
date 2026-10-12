using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class LootPoolsPatches
    {
        internal static bool Install()
        {
            try
            {
                return Hook(typeof(StorageSpawnCategoryDatabase), "ConstructSpawnCategoryDatabase", Type.EmptyTypes, null, "ConstructPostfix")
                    && Hook(typeof(Storage), "SpawnItems", Type.EmptyTypes, "SpawnItemsPrefix", "SpawnItemsPostfix");
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(LootPools.Owner);
                CoreLog.For(LootPools.Owner).Warn("loot patches not installed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(LootPoolsPatches), prefix), CorePatcher.Handler(typeof(LootPoolsPatches), postfix), null, true, LootPools.Owner);
        }

        private static void ConstructPostfix()
        {
            try
            {
                LootPools.ApplyAll();
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "ConstructSpawnCategoryDatabase postfix", e);
            }
        }

        private static void SpawnItemsPrefix(Storage __instance, bool ___removeOldItems, out int[] __state)
        {
            __state = null;
            try
            {
                __state = LootPools.BeforeFill(__instance, ___removeOldItems);
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "SpawnItems prefix", e);
            }
        }

        private static void SpawnItemsPostfix(Storage __instance, int[] __state)
        {
            if (__state != null)
            {
                try
                {
                    LootPools.OnFilled(__instance, __state);
                }
                catch (Exception e)
                {
                    ApiLog.Once(LootPools.Owner, "SpawnItems postfix", e);
                }
            }
            try
            {
                ShopStock.TopUp(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(ShopStock.Owner, "trader top-up", e);
            }
        }
    }
}
