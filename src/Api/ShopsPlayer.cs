using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class PlayerShopInfo
    {
        public string Id { get; private set; }
        public bool Open { get; private set; }
        public bool HasCashRegister { get; private set; }
        public bool ResidentsOnly { get; private set; }
        public bool InvalidArea { get; private set; }
        public MoneyCurrency Currency { get; private set; }
        public float PriceFactor { get; private set; }
        public int StockCount { get; private set; }
        public int UncollectedOc { get; private set; }
        public float UncollectedRm { get; private set; }

        internal PlayerShopInfo(string id, bool open, bool hasCashRegister, bool residentsOnly, bool invalidArea, MoneyCurrency currency, float priceFactor, int stockCount, int uncollectedOc, float uncollectedRm)
        {
            Id = id;
            Open = open;
            HasCashRegister = hasCashRegister;
            ResidentsOnly = residentsOnly;
            InvalidArea = invalidArea;
            Currency = currency;
            PriceFactor = priceFactor;
            StockCount = stockCount;
            UncollectedOc = uncollectedOc;
            UncollectedRm = uncollectedRm;
        }
    }

    public static class PlayerShops
    {
        public static List<PlayerShopInfo> All()
        {
            List<PlayerShopInfo> shops = new List<PlayerShopInfo>();
            try
            {
                foreach (PlayerShopManager manager in PlayerShopController.shopmanagers)
                {
                    if (manager != null && manager.data != null && !string.IsNullOrEmpty(manager.GUID)) shops.Add(Describe(manager));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "PlayerShops.All", e);
            }
            return shops;
        }

        public static PlayerShopInfo Get(string shopId)
        {
            try
            {
                PlayerShopManager manager = Find(shopId);
                return manager == null ? null : Describe(manager);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "PlayerShops.Get", e);
                return null;
            }
        }

        public static List<StockItem> Stock(string shopId)
        {
            List<StockItem> stock = new List<StockItem>();
            try
            {
                PlayerShopManager manager = Find(shopId);
                if (manager == null) return stock;
                MoneyCurrency currency = Cash.FromGame(manager.data.Currency);
                foreach (ItemStack stack in manager.GetItemsOnSale())
                {
                    if (stack == null || stack.itemReference == null || stack.itemReference.ID <= 0 || stack.itemAmount <= 0) continue;
                    Money.money price = manager.data.GetItemPrice(stack.itemReference);
                    float unitPrice = currency == MoneyCurrency.RM ? price.RM : price.OC;
                    Shops.AddStock(stock, stack.itemReference.ID, stack.itemAmount, unitPrice, currency);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "PlayerShops.Stock", e);
            }
            return stock;
        }

        public static bool CollectEarnings(string shopId)
        {
            PlayerShopManager manager = Find(shopId);
            if (manager == null || !Cash.Ready()) return false;
            try
            {
                if (!Uncollected(manager.data).IsMoreThanZero()) return false;
                manager.data.CollectEarnings();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "CollectEarnings", e);
                return false;
            }
        }

        private static PlayerShopManager Find(string shopId)
        {
            if (string.IsNullOrEmpty(shopId)) return null;
            foreach (PlayerShopManager manager in PlayerShopController.shopmanagers)
            {
                if (manager != null && manager.data != null && manager.GUID == shopId) return manager;
            }
            return null;
        }

        private static PlayerShopInfo Describe(PlayerShopManager manager)
        {
            PlayerShopManager.PlayerShopData data = manager.data;
            int units = 0;
            foreach (ItemStack stack in manager.GetItemsOnSale())
            {
                if (stack != null) units += stack.itemAmount;
            }
            Money.money owed = Uncollected(data);
            return new PlayerShopInfo(manager.GUID, data.isOpenAndValid, data.hasCashRegister, data.residentsOnly, data.invalidArea, Cash.FromGame(data.Currency), data.GeneralPriceFactor, units, owed.OC, owed.RM);
        }

        private static Money.money Uncollected(PlayerShopManager.PlayerShopData data)
        {
            return ShopsPatches.Uncollected == null ? default(Money.money) : (Money.money)ShopsPatches.Uncollected.GetValue(data);
        }
    }
}
