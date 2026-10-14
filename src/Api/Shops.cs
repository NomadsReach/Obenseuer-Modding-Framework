using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum ShopKind
    {
        Trader = 0,
        Vending = 1
    }

    public sealed class ShopInfo
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public ShopKind Kind { get; private set; }

        internal ShopInfo(string id, string name, ShopKind kind)
        {
            Id = id;
            Name = name;
            Kind = kind;
        }
    }

    public sealed class StockItem
    {
        public int ItemId { get; private set; }
        public string Name { get; private set; }
        public int Count { get; private set; }
        public float UnitPrice { get; private set; }
        public MoneyCurrency Currency { get; private set; }

        internal StockItem(int itemId, string name, int count, float unitPrice, MoneyCurrency currency)
        {
            ItemId = itemId;
            Name = name;
            Count = count;
            UnitPrice = unitPrice;
            Currency = currency;
        }

        internal void AddCount(int count)
        {
            Count += count;
        }
    }

    public sealed class TradeArgs
    {
        public int ItemId { get; private set; }
        public int Count { get; private set; }
        public float Price { get; private set; }
        public MoneyCurrency Currency { get; private set; }
        public ShopInfo Shop { get; private set; }

        internal TradeArgs(int itemId, int count, float price, MoneyCurrency currency, ShopInfo shop)
        {
            ItemId = itemId;
            Count = count;
            Price = price;
            Currency = currency;
            Shop = shop;
        }
    }

    public sealed class PriceQueryArgs
    {
        public ShopInfo Shop { get; private set; }
        public int ItemId { get; private set; }
        public int Count { get; private set; }
        public bool PlayerBuys { get; private set; }
        public MoneyCurrency Currency { get; private set; }
        public float Price { get; set; }

        internal PriceQueryArgs(ShopInfo shop, int itemId, int count, bool playerBuys, MoneyCurrency currency, float price)
        {
            Shop = shop;
            ItemId = itemId;
            Count = count;
            PlayerBuys = playerBuys;
            Currency = currency;
            Price = price;
        }
    }

    [Flags]
    internal enum ShopGroup
    {
        None = 0,
        Trade = 1,
        Vending = 2,
        Price = 4,
        All = Trade | Vending | Price
    }

    public static class Shops
    {
        internal const string Owner = "obenseuer.moddingframework.shops";
        private static ShopGroup installed;

        public static event Action<TradeArgs> Bought;
        public static event Action<TradeArgs> Sold;
        public static event Action<PriceQueryArgs> PriceQuery;

        internal static bool HasPriceQuery
        {
            get
            {
                return PriceQuery != null;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return Installed(ShopGroup.All);
            }
        }

        internal static bool Installed(ShopGroup group)
        {
            return (installed & group) == group && !CorePatcher.IsDisabled(Owner);
        }

        public static List<ShopInfo> All()
        {
            List<ShopInfo> shops = new List<ShopInfo>();
            try
            {
                foreach (Trade trade in SceneIds.Active<Trade>())
                {
                    ShopInfo info = InfoOf(trade);
                    if (info.Id.Length > 0) shops.Add(info);
                }
                foreach (VendingMachine machine in SceneIds.Active<VendingMachine>())
                {
                    ShopInfo info = InfoOf(machine);
                    if (info.Id.Length > 0) shops.Add(info);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Shops.All", e);
            }
            return shops;
        }

        public static ShopInfo Open()
        {
            try
            {
                Trade trade = TradePanel.instance == null ? null : TradePanel.instance.activeTrade;
                if (trade == null || string.IsNullOrEmpty(trade.GUID)) return null;
                return InfoOf(trade);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Open", e);
                return null;
            }
        }

        public static List<StockItem> Stock(string shopId)
        {
            List<StockItem> stock = new List<StockItem>();
            if (string.IsNullOrEmpty(shopId) || !ShopsPatches.Resolved) return stock;
            try
            {
                Trade trade = FindTrader(shopId);
                if (trade != null)
                {
                    TraderStock(trade, stock);
                    return stock;
                }
                VendingMachine machine = FindVending(shopId);
                if (machine != null) VendingStock(machine, stock);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "Shops.Stock", e);
            }
            return stock;
        }

        public static float PriceOf(string shopId, int itemId, int count, bool playerBuys, out MoneyCurrency currency)
        {
            currency = MoneyCurrency.OC;
            if (string.IsNullOrEmpty(shopId) || itemId <= 0 || count <= 0 || !ShopsPatches.Resolved) return 0f;
            try
            {
                Trade trade = FindTrader(shopId);
                if (trade != null) return TraderPrice(trade, itemId, count, playerBuys, out currency);
                VendingMachine machine = FindVending(shopId);
                if (machine != null) return VendingPrice(machine, itemId, count, playerBuys, out currency);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "PriceOf", e);
            }
            return 0f;
        }

        internal static void Init()
        {
            installed = ShopsPatches.Install();
            CoreLog.For(Owner).Info(IsAvailable ? "shops installed" : "shops not installed, missing " + (ShopGroup.All & ~installed));
        }

        internal static ShopInfo InfoOf(Trade trade)
        {
            return new ShopInfo(trade.GUID ?? "", string.IsNullOrEmpty(trade.title) ? trade.name : trade.title, ShopKind.Trader);
        }

        internal static ShopInfo InfoOf(VendingMachine machine)
        {
            return new ShopInfo(machine.GUID ?? "", string.IsNullOrEmpty(machine.title) ? machine.name : machine.title, ShopKind.Vending);
        }

        internal static void RaiseBought(TradeArgs trade)
        {
            Events.Raise(Bought, "Shops.Bought", h => h(trade));
        }

        internal static void RaiseSold(TradeArgs trade)
        {
            Events.Raise(Sold, "Shops.Sold", h => h(trade));
        }

        internal static void RaisePriceQuery(PriceQueryArgs args)
        {
            Events.Raise(PriceQuery, "Shops.PriceQuery", h => h(args));
        }

        internal static Trade FindTrader(string shopId)
        {
            foreach (Trade trade in SceneIds.Active<Trade>())
            {
                if (trade.GUID == shopId) return trade;
            }
            return null;
        }

        private static VendingMachine FindVending(string shopId)
        {
            foreach (VendingMachine machine in SceneIds.Active<VendingMachine>())
            {
                if (machine.GUID == shopId) return machine;
            }
            return null;
        }

        private static void TraderStock(Trade trade, List<StockItem> stock)
        {
            Storage storage = ShopsPatches.TradeStorage.GetValue(trade) as Storage;
            if (storage == null || storage.storageSlots == null) return;
            foreach (StorageSlot slot in storage.storageSlots)
            {
                if (slot == null || slot.itemReference == null || slot.itemReference.ID <= 0 || slot.itemAmount <= 0) continue;
                MoneyCurrency currency;
                float unitPrice = TraderPrice(trade, slot.itemReference.ID, 1, true, out currency);
                AddStock(stock, slot.itemReference.ID, slot.itemAmount, unitPrice, currency);
            }
        }

        private static void VendingStock(VendingMachine machine, List<StockItem> stock)
        {
            if (machine.sellableItems == null) return;
            foreach (VendingMachine.VendingMachineItem entry in machine.sellableItems)
            {
                if (entry == null || entry.item == null || entry.item.ID <= 0) continue;
                string name = string.IsNullOrEmpty(entry.customTitle) ? NameOf(entry.item.ID) : entry.customTitle;
                stock.Add(new StockItem(entry.item.ID, name, entry.currentStock, entry.GetPrice(), Cash.FromGame(entry.currency)));
            }
        }

        internal static void AddStock(List<StockItem> stock, int itemId, int count, float unitPrice, MoneyCurrency currency)
        {
            foreach (StockItem existing in stock)
            {
                if (existing.ItemId == itemId && existing.Currency == currency && existing.UnitPrice == unitPrice)
                {
                    existing.AddCount(count);
                    return;
                }
            }
            stock.Add(new StockItem(itemId, NameOf(itemId), count, unitPrice, currency));
        }

        private static float TraderPrice(Trade trade, int itemId, int count, bool playerBuys, out MoneyCurrency currency)
        {
            Money.Currency game = ShopsPatches.CurrencyOf(trade, playerBuys ? trade.purchasePriceMode : trade.sellPriceMode);
            currency = Cash.FromGame(game);
            Item item = ItemOf(itemId);
            if (item == null) return 0f;
            float basePrice = playerBuys ? trade.basePurchasePrice : trade.baseSellPrice;
            float price = Trade.GetItemPrice(item, 0, null, game, basePrice, trade.stolenPriceFactor) * (float)count;
            return ShopsPatches.Adjust(trade, itemId, count, playerBuys, game, price);
        }

        private static float VendingPrice(VendingMachine machine, int itemId, int count, bool playerBuys, out MoneyCurrency currency)
        {
            currency = MoneyCurrency.OC;
            if (!playerBuys || machine.sellableItems == null) return 0f;
            foreach (VendingMachine.VendingMachineItem entry in machine.sellableItems)
            {
                if (entry == null || entry.item == null || entry.item.ID != itemId) continue;
                currency = Cash.FromGame(entry.currency);
                return entry.GetPrice() * (float)count;
            }
            return 0f;
        }

        internal static Item ItemOf(int itemId)
        {
            if (itemId <= 0 || ItemDatabase.instance == null) return null;
            return ItemRegistry.FindInDatabase(itemId);
        }

        private static string NameOf(int itemId)
        {
            Item item = ItemOf(itemId);
            return item == null || item.Title == null ? "" : item.Title;
        }
    }
}
