using System;
using System.Collections.Generic;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    public static partial class FurnitureRegistry
    {
        public static bool AddFlatpackLoot(string furnitureId, string category, int weight)
        {
            string error = FurnitureIds.CheckLoot(category, weight);
            if (error != null) throw new ArgumentException(error);
            if (furnitureId == null || !IsAvailable || !FurnitureLoot.Installed || !LootPools.IsAvailable) return false;
            try
            {
                FurnitureEntry entry = Find(furnitureId);
                return entry != null && FurnitureLoot.Add(entry.Id, category, weight);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AddFlatpackLoot", e);
                return false;
            }
        }
    }

    internal static class FurnitureLoot
    {
        internal static bool Installed;
        private static readonly Dictionary<string, List<LootRecord>> categories = new Dictionary<string, List<LootRecord>>(StringComparer.Ordinal);

        internal static void Install()
        {
            bool ok;
            try
            {
                ok = Hook(typeof(Storage), "SetNextItemsToSpawn", Type.EmptyTypes, null, "SetNextItemsToSpawnPostfix");
                ok &= Hook(typeof(CollectibleItemSpawner), "SpawnItem", new[] { typeof(Item), typeof(Vector3), typeof(Quaternion), typeof(StorageSpawnCategory), typeof(bool), typeof(object[]) }, "SpawnItemPrefix", null);
                ok &= Hook(typeof(StorageSpawnCategory), "GetRandomItemWithMeta", new[] { typeof(float), typeof(int), typeof(int), typeof(int) }, null, "GetRandomItemWithMetaPostfix");
                ok &= Hook(typeof(ItemMetaUtilities), "GetMetaForItem", new[] { typeof(ItemReference), typeof(StorageMetaValuesBase), typeof(int), typeof(int), typeof(int) }, null, "GetMetaForItemPostfix");
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "loot install", e);
                ok = false;
            }
            Installed = ok;
            if (!ok) CoreLog.For(FurnitureRegistry.Owner).Warn("loot not installed, AddFlatpackLoot returns false");
        }

        internal static bool Add(string furnitureId, string category, int weight)
        {
            List<LootRecord> records;
            if (!categories.TryGetValue(category, out records)) records = new List<LootRecord>();
            List<LootRecord> next = new List<LootRecord>(records);
            LootRecord record = new LootRecord { FurnitureId = furnitureId, Weight = weight };
            int index = next.FindIndex(r => r.FurnitureId == furnitureId);
            if (index < 0) next.Add(record);
            else next[index] = record;
            List<int> weights = new List<int>();
            foreach (LootRecord r in next) weights.Add(r.Weight);
            if (!LootPools.Add(FurnitureRegistry.Owner, category, FurnitureFlatpacks.FlatpackItemId, FurnitureIds.LootWeight(weights), LootTier.Common)) return false;
            categories[category] = next;
            return true;
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(FurnitureLoot), prefix), CorePatcher.Handler(typeof(FurnitureLoot), postfix), null, false, FurnitureRegistry.Owner);
        }

        private static void SetNextItemsToSpawnPostfix(Storage __instance, List<ItemStackSpawn> ___nextItemsToSpawn, bool ___spawnItemsFromCategory)
        {
            if (!Installed) return;
            try
            {
                FillRolls(__instance.itemSpawnCategory, ___nextItemsToSpawn, ___spawnItemsFromCategory);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "loot roll postfix", e);
            }
        }

        private static bool SpawnItemPrefix(CollectibleItemSpawner __instance, Item item, StorageSpawnCategory category, ref object[] savedMeta)
        {
            if (!Installed || savedMeta != null || category == null || item == null || item.ID != FurnitureFlatpacks.FlatpackItemId) return true;
            List<LootRecord> records = Records(category.Name);
            if (records == null) return true;
            try
            {
                Furniture furniture = PickFurniture(records);
                if (furniture == null) return false;
                object[] meta = ItemMetaUtilities.GetMetaForItem(new ItemReference(item), __instance.useSpawnInfo ? __instance.itemSpawnInfo : category.MetaValues, __instance.owner == null ? 0 : __instance.owner.ID);
                savedMeta = FurnitureFlatpacks.Fill(meta, furniture);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "ground loot prefix", e);
                return true;
            }
        }

        private static void GetRandomItemWithMetaPostfix(StorageSpawnCategory __instance, ref ValueTuple<Item, object[]> __result)
        {
            if (!Installed) return;
            try
            {
                if (__result.Item1 == null || __result.Item1.ID != FurnitureFlatpacks.FlatpackItemId || !IsEmptyFlatpack(__result.Item2)) return;
                List<LootRecord> records = Records(__instance.Name);
                if (records == null) return;
                Furniture furniture = PickFurniture(records);
                if (furniture != null) FurnitureFlatpacks.Fill(__result.Item2, furniture);
                else ApiLog.Once(FurnitureRegistry.Owner, "package loot", "no furniture built for a flatpack of category " + __instance.Name);
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "package loot postfix", e);
            }
        }

        private static void GetMetaForItemPostfix(StorageMetaValuesBase metaValues, object[] __result)
        {
            if (!Installed || __result == null) return;
            try
            {
                StorageMetaValues storageMetaValues = metaValues as StorageMetaValues;
                if (storageMetaValues == null || storageMetaValues.package == null || !storageMetaValues.package.spawnFromCategory) return;
                List<LootRecord> records = Records(storageMetaValues.package.spawnCategoroyName);
                if (records == null) return;
                ItemPackageData packageData = Item.GetMetaOfType<ItemPackageData>(__result);
                if (packageData == null || packageData.containedItems == null) return;
                for (int i = packageData.containedItems.Count - 1; i >= 0; i--)
                {
                    ChildItem child = packageData.containedItems[i];
                    if (child == null || child.item == null || child.item.ID != FurnitureFlatpacks.FlatpackItemId || !IsEmptyFlatpack(child.meta)) continue;
                    Furniture furniture = PickFurniture(records);
                    if (furniture != null) FurnitureFlatpacks.Fill(child.meta, furniture);
                    else packageData.containedItems.RemoveAt(i);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "package meta postfix", e);
            }
        }

        private static void FillRolls(string category, List<ItemStackSpawn> spawns, bool fromCategory)
        {
            if (!fromCategory || spawns == null) return;
            List<LootRecord> records = Records(category);
            if (records == null) return;
            for (int i = spawns.Count - 1; i >= 0; i--)
            {
                ItemStackSpawn spawn = spawns[i];
                int itemId = spawn.itemId == 0 && spawn.itemReference != null ? spawn.itemReference.ID : spawn.itemId;
                if (itemId != FurnitureFlatpacks.FlatpackItemId) continue;
                object[] meta = spawn.Meta;
                if (!IsEmptyFlatpack(meta)) continue;
                Furniture furniture = PickFurniture(records);
                if (furniture != null) FurnitureFlatpacks.Fill(meta, furniture);
                else spawns.RemoveAt(i);
            }
        }

        private static List<LootRecord> Records(string category)
        {
            List<LootRecord> records;
            return category != null && categories.TryGetValue(category, out records) ? records : null;
        }

        private static Furniture PickFurniture(List<LootRecord> records)
        {
            List<int> weights = new List<int>();
            int total = 0;
            foreach (LootRecord record in records)
            {
                weights.Add(record.Weight);
                total += record.Weight;
            }
            int index = FurnitureIds.PickWeighted(weights, UnityEngine.Random.Range(0, total));
            if (index < 0) return null;
            FurnitureEntry entry = FurnitureRegistry.Find(records[index].FurnitureId);
            return entry == null ? null : FurnitureBuild.Registered(entry);
        }

        private static bool IsEmptyFlatpack(object[] meta)
        {
            ItemFurnitureData data = Item.GetMetaOfType<ItemFurnitureData>(meta);
            return data != null && data.furnitureInfo == null;
        }

        private sealed class LootRecord
        {
            internal string FurnitureId;
            internal int Weight;
        }
    }
}
