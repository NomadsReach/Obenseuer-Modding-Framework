using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    [Serializable]
    public sealed class ShopStockEntry
    {
        public string Key;
        public int Count;
    }

    [Serializable]
    public sealed class ShopStockSave
    {
        public List<string> Placed = new List<string>();
        public List<ShopStockEntry> Stocks = new List<ShopStockEntry>();
    }

    public static class ShopStock
    {
        internal const string Owner = "obenseuer.moddingframework.shopstock";
        private static readonly List<TraderRecord> traders = new List<TraderRecord>();
        private static readonly List<VendingRecord> vendings = new List<VendingRecord>();
        private static readonly HashSet<string> placed = new HashSet<string>();
        private static readonly Dictionary<string, int> savedStock = new Dictionary<string, int>();
        private static bool installed;
        private static bool fullLogged;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool AddTraderStock(string modId, string shopKey, int itemId, int amount)
        {
            ModIds.Require(modId, "modId");
            RequireKey(shopKey);
            if (amount < 1) throw new ArgumentException("amount must be 1 or more", "amount");
            if (!IsAvailable || itemId == 0 || itemId == 1 || SaveGate.Busy) return false;
            try
            {
                TraderRecord record = traders.Find(r => r.ModId == modId && r.ShopKey == shopKey && r.ItemId == itemId);
                if (record == null)
                {
                    record = new TraderRecord { ModId = modId, ShopKey = shopKey, ItemId = itemId };
                    traders.Add(record);
                }
                record.Amount = amount;
                Trade trade = FindTrade(shopKey);
                if (trade != null) TopUp(trade.GetComponent<Storage>());
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ShopStock.AddTraderStock", e);
                return false;
            }
        }

        public static bool AddVendingEntry(string modId, string shopKey, int itemId, int stock, float price, MoneyCurrency currency)
        {
            ModIds.Require(modId, "modId");
            RequireKey(shopKey);
            if (stock < 1) throw new ArgumentException("stock must be 1 or more", "stock");
            if (float.IsNaN(price) || float.IsInfinity(price)) throw new ArgumentException("price must be a finite number", "price");
            if (!Enum.IsDefined(typeof(MoneyCurrency), currency)) throw new ArgumentException("unknown currency", "currency");
            if (!IsAvailable || itemId == 0 || itemId == 1 || SaveGate.Busy) return false;
            try
            {
                VendingRecord record = vendings.Find(r => r.ModId == modId && r.ShopKey == shopKey && r.ItemId == itemId);
                if (record == null)
                {
                    record = new VendingRecord { ModId = modId, ShopKey = shopKey, ItemId = itemId };
                    vendings.Add(record);
                }
                else
                {
                    Undo(record);
                }
                record.Stock = stock;
                record.Price = price;
                record.Currency = currency;
                foreach (VendingMachine machine in FindVendingMachines())
                {
                    if (Matches(shopKey, Shops.InfoOf(machine))) Append(machine, record);
                }
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ShopStock.AddVendingEntry", e);
                return false;
            }
        }

        public static bool Remove(string modId, string shopKey, int itemId)
        {
            ModIds.Require(modId, "modId");
            RequireKey(shopKey);
            if (!IsAvailable || SaveGate.Busy) return false;
            try
            {
                bool found = traders.RemoveAll(r => r.ModId == modId && r.ShopKey == shopKey && r.ItemId == itemId) > 0;
                VendingRecord record = vendings.Find(r => r.ModId == modId && r.ShopKey == shopKey && r.ItemId == itemId);
                if (record != null)
                {
                    Undo(record);
                    vendings.Remove(record);
                    found = true;
                }
                return found;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ShopStock.Remove", e);
                return false;
            }
        }

        public static bool Place(string modId, string shopKey, int itemId, int amount)
        {
            ModIds.Require(modId, "modId");
            RequireKey(shopKey);
            if (amount < 1) throw new ArgumentException("amount must be 1 or more", "amount");
            if (!IsAvailable || itemId == 0 || itemId == 1 || SaveGate.Busy) return false;
            try
            {
                Trade trade = FindTrade(shopKey);
                if (trade == null) return false;
                string guid = Shops.InfoOf(trade).Id;
                if (guid.Length == 0) return false;
                string key = modId + "|" + guid + "|" + itemId;
                if (placed.Contains(key)) return false;
                if (Fill(trade.GetComponent<Storage>(), itemId, amount) == 0) return false;
                placed.Add(key);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ShopStock.Place", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = ShopStockPatches.Install();
            try
            {
                SaveData.Register<ShopStockSave>(Owner, Build, Load);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("save data not registered: " + e.ToString());
            }
            CoreLog.For(Owner).Info(installed ? "shop stock installed" : "shop stock not installed");
        }

        internal static void TopUp(Storage storage)
        {
            if (storage == null || storage.storageSlots == null || traders.Count == 0 || !IsAvailable) return;
            Trade trade = storage.GetComponent<Trade>();
            if (trade == null) return;
            ShopInfo info = Shops.InfoOf(trade);
            foreach (TraderRecord record in traders)
            {
                if (!Matches(record.ShopKey, info)) continue;
                int missing = record.Amount - Count(storage, record.ItemId);
                if (missing > 0) Fill(storage, record.ItemId, missing);
            }
        }

        internal static void VendingReady(VendingMachine machine)
        {
            if (vendings.Count == 0 || !IsAvailable) return;
            ShopInfo info = Shops.InfoOf(machine);
            foreach (VendingRecord record in vendings)
            {
                if (Matches(record.ShopKey, info)) Append(machine, record);
            }
        }

        internal static void VendingBought(VendingMachine machine, VendingMachine.VendingMachineItem entry)
        {
            if (vendings.Count == 0 || !IsAvailable || entry == null || entry.item == null || OwnerOf(entry) == null) return;
            string guid = Shops.InfoOf(machine).Id;
            if (guid.Length == 0) return;
            savedStock[guid + "|" + entry.item.ID] = entry.currentStock;
        }

        internal static void VendingRefilled(VendingMachine machine)
        {
            if (savedStock.Count == 0) return;
            string guid = Shops.InfoOf(machine).Id;
            if (guid.Length == 0) return;
            string prefix = guid + "|";
            List<string> stale = new List<string>();
            foreach (string key in savedStock.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal)) stale.Add(key);
            }
            foreach (string key in stale) savedStock.Remove(key);
        }

        private static ShopStockSave Build()
        {
            CaptureVendingStock();
            ShopStockSave save = new ShopStockSave();
            save.Placed.AddRange(placed);
            foreach (KeyValuePair<string, int> pair in savedStock)
            {
                save.Stocks.Add(new ShopStockEntry { Key = pair.Key, Count = pair.Value });
            }
            return save;
        }

        private static void Load(ShopStockSave save)
        {
            placed.Clear();
            savedStock.Clear();
            if (save != null && save.Placed != null) placed.UnionWith(save.Placed);
            if (save != null && save.Stocks != null)
            {
                foreach (ShopStockEntry entry in save.Stocks)
                {
                    if (entry != null && entry.Key != null) savedStock[entry.Key] = entry.Count;
                }
            }
            RestoreVendingStock();
        }

        private static void CaptureVendingStock()
        {
            foreach (VendingMachine machine in FindVendingMachines())
            {
                string guid = Shops.InfoOf(machine).Id;
                if (guid.Length == 0 || machine.sellableItems == null) continue;
                foreach (VendingMachine.VendingMachineItem entry in machine.sellableItems)
                {
                    if (entry == null || entry.item == null || OwnerOf(entry) == null) continue;
                    savedStock[guid + "|" + entry.item.ID] = entry.currentStock;
                }
            }
        }

        private static void RestoreVendingStock()
        {
            if (savedStock.Count == 0 || SaveGate.Busy) return;
            foreach (VendingMachine machine in FindVendingMachines())
            {
                string guid = Shops.InfoOf(machine).Id;
                if (guid.Length == 0 || machine.sellableItems == null) continue;
                foreach (VendingMachine.VendingMachineItem entry in machine.sellableItems)
                {
                    VendingRecord record = entry == null || entry.item == null ? null : OwnerOf(entry);
                    int stock;
                    if (record != null && TrySavedStock(guid, entry.item.ID, record.Stock, out stock)) entry.currentStock = stock;
                }
            }
        }

        private static bool TrySavedStock(string guid, int itemId, int maxStock, out int stock)
        {
            stock = maxStock;
            int saved;
            if (guid.Length == 0 || !savedStock.TryGetValue(guid + "|" + itemId, out saved)) return false;
            stock = Math.Max(0, Math.Min(saved, maxStock));
            return true;
        }

        private static VendingRecord OwnerOf(VendingMachine.VendingMachineItem entry)
        {
            foreach (VendingRecord record in vendings)
            {
                if (record.Entries.Contains(entry)) return record;
            }
            return null;
        }

        private static void RequireKey(string shopKey)
        {
            if (string.IsNullOrEmpty(shopKey)) throw new ArgumentException("shopKey must be a non-empty shop id or name", "shopKey");
        }

        private static bool Matches(string key, ShopInfo info)
        {
            return (info.Id.Length > 0 && key == info.Id) || key == info.Name;
        }

        private static Trade FindTrade(string shopKey)
        {
            foreach (Trade trade in UnityEngine.Object.FindObjectsByType<Trade>(FindObjectsSortMode.None))
            {
                if (Matches(shopKey, Shops.InfoOf(trade))) return trade;
            }
            return null;
        }

        private static List<VendingMachine> FindVendingMachines()
        {
            return new List<VendingMachine>(UnityEngine.Object.FindObjectsByType<VendingMachine>(FindObjectsSortMode.None));
        }

        private static int Count(Storage storage, int itemId)
        {
            int total = 0;
            foreach (StorageSlot slot in storage.storageSlots)
            {
                if (slot != null && slot.itemId == itemId) total += slot.itemAmount;
            }
            return total;
        }

        private static int Fill(Storage storage, int itemId, int wanted)
        {
            Item target = Shops.ItemOf(itemId);
            if (storage == null || storage.storageSlots == null || target == null) return 0;
            ItemReference reference = new ItemReference((Item)null) { ID = itemId, name = target.Title };
            int done = 0;
            for (int i = 0; i < storage.storageSlots.Length && done < wanted; i++)
            {
                StorageSlot slot = storage.storageSlots[i];
                if (slot == null || slot.itemId != -1) continue;
                if (ShopStockPatches.AddToSlot(storage, i, reference, wanted - done)) done += slot.itemAmount;
            }
            if (done < wanted && !fullLogged)
            {
                fullLogged = true;
                CoreLog.For(Owner).Warn("no empty slot in " + storage.name + " for item " + itemId + ", the rest of its stock is skipped");
            }
            return done;
        }

        private static void Append(VendingMachine machine, VendingRecord record)
        {
            if (machine.sellableItems != null)
            {
                foreach (VendingMachine.VendingMachineItem entry in machine.sellableItems)
                {
                    if (entry == null || entry.item == null || entry.item.ID != record.ItemId) continue;
                    if (!record.Entries.Contains(entry) && !record.Duplicate)
                    {
                        record.Duplicate = true;
                        CoreLog.For(Owner).Warn("vending entry from " + record.ModId + " skipped: " + machine.name + " already sells item " + record.ItemId);
                    }
                    return;
                }
            }
            Item target = Shops.ItemOf(record.ItemId);
            if (target == null)
            {
                if (!record.Warned)
                {
                    record.Warned = true;
                    CoreLog.For(Owner).Warn("vending entry from " + record.ModId + " skipped: item " + record.ItemId + " is not loaded");
                }
                return;
            }
            int current;
            TrySavedStock(Shops.InfoOf(machine).Id, record.ItemId, record.Stock, out current);
            VendingMachine.VendingMachineItem added = new VendingMachine.VendingMachineItem
            {
                item = new ItemReference((Item)null) { ID = record.ItemId, name = target.Title },
                stock = record.Stock,
                currentStock = current,
                currency = Cash.ToGame(record.Currency)
            };
            if (record.Price > 0f) added.absolutePrice = record.Price;
            int length = machine.sellableItems == null ? 0 : machine.sellableItems.Length;
            Array.Resize(ref machine.sellableItems, length + 1);
            machine.sellableItems[length] = added;
            record.Entries.Add(added);
        }

        private static void Undo(VendingRecord record)
        {
            if (record.Entries.Count == 0) return;
            foreach (VendingMachine machine in FindVendingMachines())
            {
                if (machine.sellableItems == null) continue;
                List<VendingMachine.VendingMachineItem> kept = new List<VendingMachine.VendingMachineItem>(machine.sellableItems);
                if (kept.RemoveAll(entry => record.Entries.Contains(entry)) > 0) machine.sellableItems = kept.ToArray();
            }
            record.Entries.Clear();
        }

        private sealed class TraderRecord
        {
            internal string ModId;
            internal string ShopKey;
            internal int ItemId;
            internal int Amount;
        }

        private sealed class VendingRecord
        {
            internal string ModId;
            internal string ShopKey;
            internal int ItemId;
            internal int Stock;
            internal float Price;
            internal MoneyCurrency Currency;
            internal readonly List<VendingMachine.VendingMachineItem> Entries = new List<VendingMachine.VendingMachineItem>();
            internal bool Warned;
            internal bool Duplicate;
        }
    }
}
