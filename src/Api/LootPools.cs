using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public enum LootTier
    {
        Common = 0,
        Rare = 1
    }

    public sealed class LootCategoryInfo
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int CommonWeight { get; private set; }
        public int RareWeight { get; private set; }

        internal LootCategoryInfo(int id, string name, int commonWeight, int rareWeight)
        {
            Id = id;
            Name = name;
            CommonWeight = commonWeight;
            RareWeight = rareWeight;
        }
    }

    public sealed class LootEntryInfo
    {
        public int ItemId { get; private set; }
        public LootTier Tier { get; private set; }
        public int Weight { get; private set; }

        internal LootEntryInfo(int itemId, LootTier tier, int weight)
        {
            ItemId = itemId;
            Tier = tier;
            Weight = weight;
        }
    }

    public sealed class ContainerFilledArgs
    {
        public ContainerInfo Container { get; private set; }
        public List<int> ItemIds { get; private set; }

        internal ContainerFilledArgs(ContainerInfo container, List<int> itemIds)
        {
            Container = container;
            ItemIds = itemIds;
        }
    }

    public static class LootPools
    {
        internal const string Owner = "obenseuer.moddingframework.loot";
        private const int MaxWeight = 100;
        private static readonly List<Record> records = new List<Record>();
        private static readonly List<CategoryRecord> categories = new List<CategoryRecord>();
        private static readonly FieldInfo databaseField = AccessTools.Field(typeof(StorageSpawnCategoryDatabase), "database");
        private static bool installed;

        public static event Action<ContainerFilledArgs> ContainerFilled;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<LootCategoryInfo> Categories()
        {
            try
            {
                return CategoryList();
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "LootPools.Categories", e);
                return new List<LootCategoryInfo>();
            }
        }

        public static List<LootEntryInfo> PoolFor(string category)
        {
            RequireCategory(category);
            try
            {
                StorageSpawnCategory live = Live(category);
                return live == null ? new List<LootEntryInfo>() : Entries(live);
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "LootPools.PoolFor", e);
                return new List<LootEntryInfo>();
            }
        }

        public static bool Add(string modId, string category, int itemId, int weight, LootTier tier = LootTier.Common)
        {
            ModIds.Require(modId, "modId");
            RequireCategory(category);
            if (!Enum.IsDefined(typeof(LootTier), tier)) throw new ArgumentException("unknown loot tier", "tier");
            if (weight < 1 || weight > MaxWeight) throw new ArgumentException("weight must be 1 to " + MaxWeight, "weight");
            if (!IsAvailable || SaveGate.Busy || itemId == 0 || itemId == 1) return false;
            try
            {
                Record existing = Find(modId, category, itemId);
                if (existing != null)
                {
                    Undo(existing);
                    records.Remove(existing);
                }
                else
                {
                    Record other = records.Find(r => r.ModId != modId && r.Category == category && r.ItemId == itemId);
                    if (other != null) CoreLog.For(Owner).Warn("loot entry for item " + itemId + " in '" + category + "' is already added by " + other.ModId + ", the entry from " + modId + " is added too and the weights add up");
                }
                Record record = new Record { ModId = modId, Category = category, ItemId = itemId, Weight = weight, Tier = tier };
                records.Add(record);
                Apply(record);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "LootPools.Add", e);
                return false;
            }
        }

        public static bool DefineCategory(string modId, string name)
        {
            ModIds.Require(modId, "modId");
            if (string.IsNullOrEmpty(name) || !IsAvailable) return false;
            try
            {
                CategoryRecord existing = categories.Find(c => c.Name == name);
                if (existing != null)
                {
                    if (existing.ModId != modId) CoreLog.For(Owner).Warn("loot category '" + name + "' is already defined by " + existing.ModId + ", the call from " + modId + " is ignored");
                    return false;
                }
                if (Live(name) != null)
                {
                    CoreLog.For(Owner).Warn("loot category '" + name + "' is a game category, the call from " + modId + " is ignored");
                    return false;
                }
                CategoryRecord record = new CategoryRecord { ModId = modId, Name = name, Id = 1000000 + categories.Count + 1 };
                categories.Add(record);
                ApplyCategory(record);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "LootPools.DefineCategory", e);
                return false;
            }
        }

        public static bool Remove(string modId, string category, int itemId)
        {
            ModIds.Require(modId, "modId");
            RequireCategory(category);
            if (!IsAvailable || SaveGate.Busy) return false;
            try
            {
                Record existing = Find(modId, category, itemId);
                if (existing == null) return false;
                Undo(existing);
                records.Remove(existing);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "LootPools.Remove", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = LootPoolsPatches.Install();
            ItemRegistry.Loaded += ApplyAll;
            CoreLog.For(Owner).Info(installed ? "loot pools installed" : "loot pools not installed");
        }

        internal static void ApplyAll()
        {
            foreach (CategoryRecord category in categories) ApplyCategory(category);
            foreach (Record record in records) Apply(record);
        }

        internal static int[] BeforeFill(Storage storage, bool removeOldItems)
        {
            StorageSlot[] slots = storage.storageSlots;
            if (slots == null || ContainerFilled == null) return null;
            int[] ids = new int[slots.Length];
            for (int i = 0; i < slots.Length; i++) ids[i] = (slots[i] == null || removeOldItems) ? -1 : slots[i].itemId;
            return ids;
        }

        internal static void OnFilled(Storage storage, int[] before)
        {
            StorageSlot[] slots = storage.storageSlots;
            if (slots == null || ContainerFilled == null) return;
            List<int> added = new List<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                int previous = i < before.Length ? before[i] : -1;
                if (slots[i] != null && slots[i].itemId != -1 && slots[i].itemId != previous) added.Add(slots[i].itemId);
            }
            if (added.Count == 0) return;
            ContainerFilledArgs args = new ContainerFilledArgs(new ContainerInfo(storage), added);
            Events.Raise(ContainerFilled, "LootPools.ContainerFilled", h => h(args));
        }


        private static List<LootCategoryInfo> CategoryList()
        {
            List<LootCategoryInfo> list = new List<LootCategoryInfo>();
            StorageSpawnCategoryDatabase database = StorageSpawnCategoryDatabase.instance;
            if (database == null) return list;
            foreach (int id in database.ListStorageSpawnCategoryByID())
            {
                StorageSpawnCategory category = database.FetchStorageSpawnCategoryByID(id);
                if (category != null) list.Add(new LootCategoryInfo(category.ID, category.Name, category.ItemCommon.Count, category.ItemRare.Count));
            }
            return list;
        }

        private static List<LootEntryInfo> Entries(StorageSpawnCategory category)
        {
            List<LootEntryInfo> list = new List<LootEntryInfo>();
            Collect(list, category.ItemCommon, LootTier.Common);
            Collect(list, category.ItemRare, LootTier.Rare);
            return list;
        }

        private static void Collect(List<LootEntryInfo> list, List<int> ids, LootTier tier)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                if (ids.IndexOf(id) == i) list.Add(new LootEntryInfo(id, tier, ids.FindAll(x => x == id).Count));
            }
        }

        private static StorageSpawnCategory Live(string category)
        {
            StorageSpawnCategoryDatabase database = StorageSpawnCategoryDatabase.instance;
            return database == null ? null : database.FetchStorageSpawnCategory(category);
        }

        private static List<int> Pool(StorageSpawnCategory category, LootTier tier)
        {
            return tier == LootTier.Rare ? category.ItemRare : category.ItemCommon;
        }

        private static bool ItemExists(int itemId)
        {
            List<Item> database = ItemDatabase.database;
            if (database == null) return false;
            for (int i = 0; i < database.Count; i++)
            {
                if (database[i] != null && database[i].ID == itemId) return true;
            }
            return false;
        }

        private static Record Find(string modId, string category, int itemId)
        {
            return records.Find(r => r.ModId == modId && r.Category == category && r.ItemId == itemId);
        }

        private static List<StorageSpawnCategory> Database()
        {
            StorageSpawnCategoryDatabase database = StorageSpawnCategoryDatabase.instance;
            return database == null ? null : databaseField.GetValue(database) as List<StorageSpawnCategory>;
        }

        private static void ApplyCategory(CategoryRecord record)
        {
            try
            {
                List<StorageSpawnCategory> database = Database();
                if (database == null) return;
                StorageSpawnCategory existing = Live(record.Name);
                if (existing != null)
                {
                    if (existing.ID != record.Id && !record.Warned)
                    {
                        record.Warned = true;
                        CoreLog.For(Owner).Warn("mod category '" + record.Name + "' is skipped: a vanilla category has the same name");
                    }
                    return;
                }
                StorageSpawnCategory category = new StorageSpawnCategory();
                category.ID = record.Id;
                category.Name = record.Name;
                category.ItemCommon = new List<int>();
                category.ItemRare = new List<int>();
                category.MetaValues = null;
                database.Add(category);
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "LootPools.ApplyCategory", e);
            }
        }

        private static bool Apply(Record record)
        {
            try
            {
                StorageSpawnCategory category = Live(record.Category);
                if (category != null && record.Target == category) return true;
                if (category == null || !ItemExists(record.ItemId))
                {
                    Waiting(record);
                    return false;
                }
                List<int> pool = Pool(category, record.Tier);
                for (int i = 0; i < record.Weight; i++) pool.Add(record.ItemId);
                record.Target = category;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(LootPools.Owner, "LootPools.Apply", e);
                return false;
            }
        }

        private static void Waiting(Record record)
        {
            if (record.Warned || StorageSpawnCategoryDatabase.instance == null || ItemDatabase.database == null) return;
            record.Warned = true;
            CoreLog.For(Owner).Warn("loot entry from " + record.ModId + " waits: category '" + record.Category + "' or item " + record.ItemId + " is not loaded");
        }

        private static void Undo(Record record)
        {
            StorageSpawnCategory category = Live(record.Category);
            if (category != null && record.Target == category)
            {
                List<int> pool = Pool(category, record.Tier);
                for (int i = 0; i < record.Weight; i++)
                {
                    int index = pool.LastIndexOf(record.ItemId);
                    if (index < 0) break;
                    pool.RemoveAt(index);
                }
            }
            record.Target = null;
        }

        private static void RequireCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) throw new ArgumentException("category must be a non-empty category name", "category");
        }

        private sealed class Record
        {
            internal string ModId;
            internal string Category;
            internal int ItemId;
            internal int Weight;
            internal LootTier Tier;
            internal StorageSpawnCategory Target;
            internal bool Warned;
        }

        private sealed class CategoryRecord
        {
            internal string ModId;
            internal string Name;
            internal int Id;
            internal bool Warned;
        }
    }
}
