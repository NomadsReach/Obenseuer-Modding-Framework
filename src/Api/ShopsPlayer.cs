using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public string ManagerGuid { get; private set; }

        internal PlayerShopInfo(string id, bool open, bool hasCashRegister, bool residentsOnly, bool invalidArea, MoneyCurrency currency, float priceFactor, int stockCount, int uncollectedOc, float uncollectedRm, string managerGuid)
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
            ManagerGuid = managerGuid;
        }
    }

    public sealed class SoldArgs
    {
        public string Shop { get; private set; }
        public int ItemId { get; private set; }
        public int Count { get; private set; }
        public float Price { get; private set; }
        public MoneyCurrency Currency { get; private set; }
        public bool Stolen { get; private set; }
        public bool ByWorker { get; private set; }

        internal SoldArgs(string shop, int itemId, int count, float price, MoneyCurrency currency, bool stolen, bool byWorker)
        {
            Shop = shop;
            ItemId = itemId;
            Count = count;
            Price = price;
            Currency = currency;
            Stolen = stolen;
            ByWorker = byWorker;
        }
    }

    public static class PlayerShops
    {
        internal const string Owner = Shops.Owner + ".player";
        private static bool installed;

        public static event Action<SoldArgs> Sold;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<PlayerShopInfo> All()
        {
            List<PlayerShopInfo> shops = new List<PlayerShopInfo>();
            try
            {
                foreach (PlayerShopManager manager in SceneIds.Active<PlayerShopManager>(FindObjectsInactive.Include))
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
            if (Cash.Refused("PlayerShops.CollectEarnings")) return false;
            PlayerShopManager manager = Find(shopId);
            if (manager == null || !Cash.Ready()) return false;
            try
            {
                if (!Uncollected(manager.data).IsMoreThanZero()) return false;
                manager.data.CollectEarnings();
                ShopsPlayerState.Collected(shopId);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "CollectEarnings", e);
                return false;
            }
        }

        public static string Ensure(string managerGuid)
        {
            if (!IsAvailable || string.IsNullOrEmpty(managerGuid) || SaveGate.Busy) return null;
            try
            {
                FurnitureManager manager = ShopsPlayerState.FindManager(managerGuid);
                if (manager == null) return null;
                PlayerShopManager shop = ShopsPlayerState.ShopOf(manager);
                if (shop != null)
                {
                    if (string.IsNullOrEmpty(shop.GUID) || ShopsPlayerState.Track(shop.GUID) == null) return null;
                    return shop.GUID;
                }
                string shopId = managerGuid + "-shop";
                PlayerShopState state = ShopsPlayerState.StateOf(shopId) ?? new PlayerShopState { Shop = shopId, Markup = ShopsPlayerMath.DefaultMarkup };
                state.Manager = managerGuid;
                ShopsPlayerState.Put(state);
                shop = ShopsPlayerState.AddShop(manager, shopId);
                ShopsPlayerState.Write(shop, state);
                return shopId;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerShops.Ensure", e);
                return null;
            }
        }

        public static bool Release(string managerGuid)
        {
            if (!IsAvailable || SaveGate.Busy) return false;
            try
            {
                PlayerShopState state = ShopsPlayerState.ConvertedBy(managerGuid);
                if (state == null || state.TillOc > 0 || state.TillRm > 0f) return false;
                PlayerShopManager shop = Find(state.Shop);
                if (shop != null)
                {
                    ShopsPlayerState.Unlink(shop);
                    UnityEngine.Object.Destroy(shop);
                }
                ShopsPlayerState.Remove(state.Shop);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerShops.Release", e);
                return false;
            }
        }

        public static bool SetOpen(string shopId, bool open)
        {
            return IsAvailable && Edit(shopId, state => state.Open = open);
        }

        public static bool SetMarkup(string shopId, float factor)
        {
            return IsAvailable && factor > 0f && !float.IsInfinity(factor) && Edit(shopId, state => state.Markup = factor);
        }

        public static bool SetPrice(string shopId, int itemId, int price)
        {
            return IsAvailable && itemId > 0 && price >= 0 && Shops.ItemOf(itemId) != null && Edit(shopId, state => state.Prices[itemId] = price);
        }

        public static bool ClearPrice(string shopId, int itemId)
        {
            return IsAvailable && itemId > 0 && Edit(shopId, state => state.Prices.Remove(itemId));
        }

        public static float GetPrice(string shopId, int itemId, object[] meta, out MoneyCurrency currency)
        {
            currency = MoneyCurrency.OC;
            try
            {
                Item item = Shops.ItemOf(itemId);
                PlayerShopManager shop = IsAvailable ? Find(shopId) : null;
                if (item == null || shop == null) return 0f;
                currency = Cash.FromGame(shop.data.Currency);
                Money.money price = shop.data.GetItemPrice(new ItemReference(item), meta);
                return currency == MoneyCurrency.RM ? price.RM : price.OC;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerShops.GetPrice", e);
                return 0f;
            }
        }

        public static bool AddEarnings(string shopId, MoneyCurrency currency, float amount)
        {
            float value;
            return IsAvailable && !Cash.Refused("PlayerShops.AddEarnings") && Cash.TryQuantize(currency, amount, out value) && Edit(shopId, state => ShopsPlayerState.Credit(state, currency, value));
        }

        public static bool TakeEarnings(string shopId, MoneyCurrency currency, float amount)
        {
            float value;
            return IsAvailable && !Cash.Refused("PlayerShops.TakeEarnings") && Cash.TryQuantize(currency, amount, out value) && ShopsPlayerMath.CanDebit(Earnings(shopId, currency), value) && Edit(shopId, state => ShopsPlayerState.Debit(state, currency, value));
        }

        public static float Earnings(string shopId, MoneyCurrency currency)
        {
            PlayerShopState state = ShopsPlayerState.StateOf(shopId);
            if (state != null) return currency == MoneyCurrency.RM ? state.TillRm : state.TillOc;
            PlayerShopManager shop = Find(shopId);
            if (shop == null) return 0f;
            Money.money owed = Uncollected(shop.data);
            return currency == MoneyCurrency.RM ? owed.RM : owed.OC;
        }

        public static bool Sell(string shopId, ContainerInfo container, int slot, int expectedItemId, int count, bool byWorker)
        {
            if (!IsAvailable || container == null || count <= 0 || Cash.Refused("PlayerShops.Sell")) return false;
            try
            {
                PlayerShopManager shop = Find(shopId);
                PlayerShopState state = shop == null ? null : ShopsPlayerState.Track(shopId);
                if (state == null || !Storages(shopId).Exists(storage => storage.InstanceId == container.InstanceId)) return false;
                ItemStack source = Containers.StackAt(container, slot, count);
                if (source == null) return false;
                MoneyCurrency currency = Cash.FromGame(shop.data.Currency);
                Money.money unit = shop.data.GetItemPrice(source.itemReference, source.Meta);
                float unitPrice = currency == MoneyCurrency.RM ? unit.RM : unit.OC;
                float total = currency == MoneyCurrency.RM ? Money.RoundRM(unitPrice * count) : unitPrice * count;
                float value;
                if (!ShopsPlayerMath.CanSell(source.itemId, expectedItemId, unitPrice) || !Cash.TryQuantize(currency, total, out value)) return false;
                ItemStack taken = Containers.TakeAt(container, slot, count);
                if (taken == null) return false;
                int oc = state.TillOc;
                float rm = state.TillRm;
                try
                {
                    ShopsPlayerState.Credit(state, currency, value);
                    ShopsPlayerState.Write(shop, state);
                }
                catch (Exception e)
                {
                    state.TillOc = oc;
                    state.TillRm = rm;
                    Containers.RestoreAt(container, slot, taken);
                    ApiLog.Once(Owner, "PlayerShops.Sell", e);
                    return false;
                }
                bool stolen = taken.ownerId > 0;
                Cash.RaiseNested(Sold, "PlayerShops.Sold", h => h(new SoldArgs(shopId, taken.itemId, count, value, currency, stolen, byWorker)));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerShops.Sell", e);
                return false;
            }
        }

        public static List<ContainerInfo> Storages(string shopId)
        {
            List<ContainerInfo> storages = new List<ContainerInfo>();
            try
            {
                PlayerShopManager shop = Find(shopId);
                if (shop == null || shop.manager == null) return storages;
                foreach (Storage storage in ShopsPlayerState.ComponentsOf<Storage>(shop.manager))
                {
                    ContainerInfo container = Containers.Describe(storage);
                    if (container != null) storages.Add(container);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerShops.Storages", e);
            }
            return storages;
        }

        public static List<Vector3> Registers(string shopId)
        {
            List<Vector3> positions = new List<Vector3>();
            try
            {
                PlayerShopManager shop = Find(shopId);
                if (shop == null) return positions;
                foreach (InteractableCashRegister register in SceneIds.Active<InteractableCashRegister>(FindObjectsInactive.Include))
                {
                    if (register != null && register.manager == shop) positions.Add(register.transform.position);
                }
                if (positions.Count == 0 && shop.manager != null)
                {
                    foreach (InteractableCashRegister register in ShopsPlayerState.ComponentsOf<InteractableCashRegister>(shop.manager)) positions.Add(register.transform.position);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerShops.Registers", e);
            }
            return positions;
        }

        internal static void Init()
        {
            installed = ShopsPlayerPatches.Install();
            try
            {
                SaveData.Register<PlayerShopSaveFile>(Owner, ShopsPlayerState.Save, ShopsPlayerState.Load);
            }
            catch (Exception e)
            {
                installed = false;
                CoreLog.For(Owner).Error("player shop save data not registered, the shop writes are off: " + e);
                return;
            }
            Events.SceneLoadedWithMode += ShopsPlayerState.OnSceneLoaded;
            SceneManager.activeSceneChanged += ShopsPlayerState.OnActiveSceneChanged;
            CoreLog.For(Owner).Info(installed ? "player shops installed" : "player shops not installed, the price hook is missing");
        }

        internal static PlayerShopManager Find(string shopId)
        {
            if (string.IsNullOrEmpty(shopId)) return null;
            foreach (PlayerShopManager manager in SceneIds.Active<PlayerShopManager>(FindObjectsInactive.Include))
            {
                if (manager != null && manager.data != null && manager.GUID == shopId) return manager;
            }
            return null;
        }

        internal static Money.money Uncollected(PlayerShopManager.PlayerShopData data)
        {
            return ShopsPatches.Uncollected == null ? default(Money.money) : (Money.money)ShopsPatches.Uncollected.GetValue(data);
        }

        private static bool Edit(string shopId, Action<PlayerShopState> change)
        {
            if (SaveGate.Busy) return false;
            try
            {
                PlayerShopState state = ShopsPlayerState.Track(shopId);
                if (state == null) return false;
                change(state);
                PlayerShopManager shop = Find(shopId);
                if (shop != null) ShopsPlayerState.Write(shop, state);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlayerShops.Edit", e);
                return false;
            }
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
            FurnitureManager furniture = ShopsPlayerState.ManagerOf(manager);
            string managerGuid = furniture == null || string.IsNullOrEmpty(furniture.GUID) ? null : furniture.GUID;
            return new PlayerShopInfo(manager.GUID, data.isOpenAndValid, data.hasCashRegister, data.residentsOnly, data.invalidArea, Cash.FromGame(data.Currency), data.GeneralPriceFactor, units, owed.OC, owed.RM, managerGuid);
        }
    }
}
