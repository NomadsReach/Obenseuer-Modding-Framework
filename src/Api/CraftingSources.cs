using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using OS.Items;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class CraftingSources
    {
        internal const string Owner = "obenseuer.moddingframework.craftingsources";
        private static readonly List<Entry> providers = new List<Entry>();
        private static readonly List<Scope> open = new List<Scope>();
        private static readonly List<CraftingBase> awaitingConsume = new List<CraftingBase>();
        private static readonly MethodInfo compareRecipe = AccessTools.Method(typeof(CraftingBase), "CompareRecipeToItems", new[] { typeof(Recipe), typeof(ItemStack[]), typeof(bool), typeof(bool) });
        private static bool installed;
        private static int registrationOrder;

        public static event Action<CraftingTaken> Consumed;

        public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider)
        {
            return Register(modId, provider, 0, null);
        }

        public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider, int priority)
        {
            return Register(modId, provider, priority, null);
        }

        public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider, int priority, Func<Storage, int, StorageSlot, bool> slotFilter)
        {
            ModIds.Require(modId, "modId");
            if (provider == null) throw new ArgumentNullException("provider");
            if (!installed || CorePatcher.IsDisabled(Owner))
            {
                CoreLog.For(modId).Warn("CraftingSources.Register ignored, the crafting hooks are not installed");
                return false;
            }
            Entry entry = new Entry(modId, provider, priority, slotFilter, registrationOrder++);
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                entry.Order = providers[i].Order;
                providers[i] = entry;
                return true;
            }
            providers.Add(entry);
            return true;
        }

        public static bool SetPriority(string modId, int priority)
        {
            ModIds.Require(modId, "modId");
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                providers[i].Priority = priority;
                return true;
            }
            return false;
        }

        public static bool SetSlotFilter(string modId, Func<Storage, int, StorageSlot, bool> slotFilter)
        {
            ModIds.Require(modId, "modId");
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                providers[i].SlotFilter = slotFilter;
                return true;
            }
            return false;
        }

        public static bool Unregister(string modId)
        {
            ModIds.Require(modId, "modId");
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                providers.RemoveAt(i);
                return true;
            }
            return false;
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
            installed = CraftingSourcesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("crafting sources hooks incomplete, extra sources are disabled");
        }

        internal static Scope Open(CraftingBase station, bool consume)
        {
            if (providers.Count == 0 || station == null || station.InputOutputStorage == null || SaveGate.Busy || IsOpen(station) || station is Dishwasher) return null;
            bool paired = consume && awaitingConsume.Remove(station);
            Scope scope = new Scope(station, consume);
            open.Add(scope);
            try
            {
                scope.Extras = Collect(station);
                if (scope.Extras.Count == 0)
                {
                    if (paired) return scope;
                    open.Remove(scope);
                    return null;
                }
                scope.Own = station.InputOutputStorage.storageSlots;
                if (consume) scope.Snapshots = Capture(scope.Extras);
                scope.Combined = CombineSlots(scope.Own, scope.Extras);
                station.InputOutputStorage.storageSlots = scope.Combined;
                return scope;
            }
            catch (Exception)
            {
                open.Remove(scope);
                throw;
            }
        }

        internal static void Close(Scope scope)
        {
            if (scope == null || scope.Closed) return;
            scope.Closed = true;
            open.Remove(scope);
            if (scope.Combined == null) return;
            Storage storage = scope.Station.InputOutputStorage;
            if (storage != null && storage.storageSlots == scope.Combined) storage.storageSlots = scope.Own;
            else ApiLog.Once(Owner, "restore station slots", "the station slots changed during a call that used extra sources, the new slots are kept");
            if (!scope.Consume) return;
            Report(scope);
            Refresh(scope);
        }

        private static void Refresh(Scope scope)
        {
            foreach (Source source in scope.Extras)
            {
                try
                {
                    if (source.Storage.Slots != null) source.Storage.UpdateSlots();
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "refresh extra source", e);
                }
            }
        }

        private static void Report(Scope scope)
        {
            Action<CraftingTaken> handlers = Consumed;
            if (handlers == null) return;
            foreach (CraftingTaken item in Gone(scope.Station, scope.Snapshots))
            {
                Events.Raise(handlers, "CraftingSources.Consumed", h => h(item));
            }
        }

        internal static void CloseConsume(CraftingBase station)
        {
            foreach (Scope scope in open.ToArray())
            {
                if (scope.Consume && ReferenceEquals(scope.Station, station)) Close(scope);
            }
        }

        internal static bool Allows(CraftingBase station, Recipe recipe, int multiples)
        {
            if (recipe == null) return true;
            awaitingConsume.Remove(station);
            Scope scope = Open(station, false);
            if (scope == null) return true;
            try
            {
                bool fits = Fits(station, recipe, multiples);
                if (fits) awaitingConsume.Add(station);
                return fits;
            }
            finally
            {
                Close(scope);
            }
        }

        internal static void Expire(CraftingBase station)
        {
            awaitingConsume.RemoveAll(s => s == null || ReferenceEquals(s, station));
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            awaitingConsume.Clear();
        }

        internal static bool Fits(CraftingBase station, Recipe recipe, int multiples)
        {
            if (!(bool)compareRecipe.Invoke(station, new object[] { recipe, null, false, true })) return false;
            return multiples <= 1 || station.GetRecipeMultiplier(recipe) >= multiples;
        }

        internal static StorageSlot[] Keep(StorageSlot[] slots, Func<int, StorageSlot, bool> allowed)
        {
            if (slots == null || slots.Length == 0) return new StorageSlot[0];
            StorageSlot[] result = new StorageSlot[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                StorageSlot slot = slots[i];
                if (slot == null || (allowed != null && !allowed(i, slot))) result[i] = new StorageSlot();
                else result[i] = slot;
            }
            return result;
        }

        internal static List<CraftingTaken> Gone(CraftingBase station, List<Snapshot> snapshots)
        {
            List<CraftingTaken> taken = new List<CraftingTaken>();
            if (snapshots == null) return taken;
            foreach (Snapshot before in snapshots)
            {
                if (before.ItemId == -1 || before.Amount <= 0) continue;
                int left = before.Slot != null && before.Slot.itemId == before.ItemId ? before.Slot.itemAmount : 0;
                int gone = before.Amount - left;
                float poured = LiquidGone(before);
                if (gone <= 0 && poured <= 0f) continue;
                taken.Add(new CraftingTaken(before.ModId, before.Storage, before.SlotIndex, before.ItemId, gone > 0 ? gone : 0, poured > 0f ? before.LiquidItemId : -1, poured > 0f ? poured : 0f, station));
            }
            return taken;
        }

        private static float LiquidGone(Snapshot before)
        {
            if (before.LiquidItemId == -1 || before.LiquidAmount <= 0f || before.Slot == null) return 0f;
            ItemLiquidData liquid = before.Slot.GetMetaOfType<ItemLiquidData>();
            float left = liquid == null ? 0f : liquid.GetLiquidAmount(before.Slot.itemAmount);
            if (!ItemLiquidData.IsGreaterThan(before.LiquidAmount, left)) return 0f;
            return before.LiquidAmount - left;
        }

        internal static List<Entry> Sort(List<Entry> list)
        {
            for (int i = 1; i < list.Count; i++)
            {
                Entry entry = list[i];
                int j = i - 1;
                while (j >= 0 && list[j].After(entry))
                {
                    list[j + 1] = list[j];
                    j--;
                }
                list[j + 1] = entry;
            }
            return list;
        }

        private static List<Entry> Ordered()
        {
            return Sort(new List<Entry>(providers));
        }

        private static bool IsOpen(CraftingBase station)
        {
            foreach (Scope scope in open)
            {
                if (ReferenceEquals(scope.Station, station)) return true;
            }
            return false;
        }

        private static List<Source> Collect(CraftingBase station)
        {
            List<Source> found = new List<Source>();
            List<Storage> seen = new List<Storage>();
            CraftingStationInfo info = Crafting.ToStation(station);
            foreach (Entry entry in Ordered())
            {
                List<Storage> given = Ask(entry, info);
                if (given == null) continue;
                foreach (Storage storage in given)
                {
                    if (storage == null || ReferenceEquals(storage, station.InputOutputStorage) || ReferenceEquals(storage, station.OutputStorage) || storage.storageSlots == null || storage.storageSlots.Length == 0 || seen.Contains(storage)) continue;
                    seen.Add(storage);
                    found.Add(new Source(entry, storage));
                }
            }
            return found;
        }

        private static List<Storage> Ask(Entry entry, CraftingStationInfo info)
        {
            try
            {
                IEnumerable<Storage> given = entry.Provider(info);
                if (given == null)
                {
                    ApiLog.Once(entry.ModId, "CraftingSources provider", "returned null, its containers are ignored");
                    return null;
                }
                return new List<Storage>(given);
            }
            catch (Exception e)
            {
                ApiLog.Once(entry.ModId, "CraftingSources provider", e);
                return null;
            }
        }

        private static List<Snapshot> Capture(List<Source> extras)
        {
            List<Snapshot> snapshots = new List<Snapshot>();
            foreach (Source source in extras)
            {
                StorageSlot[] slots = source.Storage.storageSlots;
                if (slots == null) continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    StorageSlot slot = slots[i];
                    if (slot == null) continue;
                    ItemLiquidData liquid = slot.GetMetaOfType<ItemLiquidData>();
                    int liquidItemId = liquid == null || liquid.liquidItem == null ? -1 : liquid.liquidItem.ID;
                    float liquidAmount = liquid == null || liquid.liquidItem == null ? 0f : liquid.GetLiquidAmount(slot.itemAmount);
                    snapshots.Add(new Snapshot(source.Entry.ModId, source.Storage, slot, i, slot.itemId, slot.itemAmount, liquidItemId, liquidAmount));
                }
            }
            return snapshots;
        }

        internal static StorageSlot[] CombineSlots(StorageSlot[] own, List<Source> extras)
        {
            List<StorageSlot> result = new List<StorageSlot>(own);
            List<StorageSlot[]> seen = new List<StorageSlot[]> { own };
            foreach (Source source in extras)
            {
                StorageSlot[] extra = source.Storage.storageSlots;
                if (extra == null || extra.Length == 0 || seen.Exists(s => ReferenceEquals(s, extra))) continue;
                seen.Add(extra);
                result.AddRange(Keep(extra, FilterOf(source)));
            }
            return result.ToArray();
        }

        private static Func<int, StorageSlot, bool> FilterOf(Source source)
        {
            if (source.Entry.SlotFilter == null) return null;
            return (index, slot) => Allowed(source, index, slot);
        }

        private static bool Allowed(Source source, int index, StorageSlot slot)
        {
            try
            {
                return source.Entry.SlotFilter(source.Storage, index, slot);
            }
            catch (Exception e)
            {
                ApiLog.Once(source.Entry.ModId, "CraftingSources slot filter", e);
                return false;
            }
        }

        internal sealed class Entry
        {
            internal readonly string ModId;
            internal readonly Func<CraftingStationInfo, IEnumerable<Storage>> Provider;
            internal int Priority;
            internal Func<Storage, int, StorageSlot, bool> SlotFilter;
            internal int Order;

            internal Entry(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider, int priority, Func<Storage, int, StorageSlot, bool> slotFilter, int order)
            {
                ModId = modId;
                Provider = provider;
                Priority = priority;
                SlotFilter = slotFilter;
                Order = order;
            }

            internal bool After(Entry other)
            {
                if (Priority != other.Priority) return Priority < other.Priority;
                return Order > other.Order;
            }
        }

        internal sealed class Source
        {
            internal readonly Entry Entry;
            internal readonly Storage Storage;

            internal Source(Entry entry, Storage storage)
            {
                Entry = entry;
                Storage = storage;
            }
        }

        internal sealed class Snapshot
        {
            internal readonly string ModId;
            internal readonly Storage Storage;
            internal readonly StorageSlot Slot;
            internal readonly int SlotIndex;
            internal readonly int ItemId;
            internal readonly int Amount;
            internal readonly int LiquidItemId;
            internal readonly float LiquidAmount;

            internal Snapshot(string modId, Storage storage, StorageSlot slot, int slotIndex, int itemId, int amount, int liquidItemId, float liquidAmount)
            {
                ModId = modId;
                Storage = storage;
                Slot = slot;
                SlotIndex = slotIndex;
                ItemId = itemId;
                Amount = amount;
                LiquidItemId = liquidItemId;
                LiquidAmount = liquidAmount;
            }
        }

        internal sealed class Scope
        {
            internal readonly CraftingBase Station;
            internal readonly bool Consume;
            internal StorageSlot[] Own;
            internal StorageSlot[] Combined;
            internal List<Source> Extras;
            internal List<Snapshot> Snapshots;
            internal bool Closed;

            internal Scope(CraftingBase station, bool consume)
            {
                Station = station;
                Consume = consume;
            }
        }

        public sealed class CraftingTaken
        {
            public string ModId { get; private set; }
            public Storage Storage { get; private set; }
            public int SlotIndex { get; private set; }
            public int ItemId { get; private set; }
            public int Amount { get; private set; }
            public int LiquidItemId { get; private set; }
            public float LiquidAmount { get; private set; }
            public CraftingBase Station { get; private set; }

            internal CraftingTaken(string modId, Storage storage, int slotIndex, int itemId, int amount, int liquidItemId, float liquidAmount, CraftingBase station)
            {
                ModId = modId;
                Storage = storage;
                SlotIndex = slotIndex;
                ItemId = itemId;
                Amount = amount;
                LiquidItemId = liquidItemId;
                LiquidAmount = liquidAmount;
                Station = station;
            }
        }
    }
}
