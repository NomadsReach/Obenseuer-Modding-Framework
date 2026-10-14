using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum BaseShopBuyResult
    {
        Ok,
        Busy,
        NotFound,
        Unsupported,
        Refused,
        OutOfStock,
        NoMoney,
        Unavailable,
        Failed
    }

    public sealed class BaseShopInfo
    {
        public string Id { get; private set; }

        public string Kind { get; private set; }

        public int Oc { get; private set; }

        public int Rm { get; private set; }

        public bool RestockDaily { get; private set; }

        internal BaseShopInfo(string id, string kind, int oc, int rm, bool restockDaily)
        {
            Id = id;
            Kind = kind;
            Oc = oc;
            Rm = rm;
            RestockDaily = restockDaily;
        }
    }

    public sealed class BaseShopBoughtArgs
    {
        public string Id { get; private set; }

        public string Kind { get; private set; }

        public string FurnitureId { get; private set; }

        public int Count { get; private set; }

        public int Oc { get; private set; }

        public int Rm { get; private set; }

        internal BaseShopBoughtArgs(string id, string kind, string furnitureId, int count, int oc, int rm)
        {
            Id = id;
            Kind = kind;
            FurnitureId = furnitureId;
            Count = count;
            Oc = oc;
            Rm = rm;
        }
    }

    public enum BaseShopSellResult
    {
        Ok,
        Busy,
        NotFound,
        Unsupported,
        Refused,
        OutOfStock,
        NoMoney,
        Unavailable,
        Failed
    }

    public sealed class BaseShopSoldArgs
    {
        public string Id { get; private set; }

        public string Kind { get; private set; }

        public string FurnitureId { get; private set; }

        public int Count { get; private set; }

        public int Oc { get; private set; }

        public int Rm { get; private set; }

        internal BaseShopSoldArgs(string id, string kind, string furnitureId, int count, int oc, int rm)
        {
            Id = id;
            Kind = kind;
            FurnitureId = furnitureId;
            Count = count;
            Oc = oc;
            Rm = rm;
        }
    }

    public static class BaseShops
    {
        internal const string Owner = "obenseuer.moddingframework.baseshops";

        private static bool selling;

        public static event Action<BaseShopBoughtArgs> Bought;

        public static event Action<BaseShopSoldArgs> Sold;

        public static List<BaseShopInfo> All()
        {
            List<BaseShopInfo> list = new List<BaseShopInfo>();
            try
            {
                foreach (BaseShop shop in SceneIds.Active<BaseShop>())
                {
                    list.Add(new BaseShopInfo(SceneIds.Of(shop), shop.GetType().Name, shop.currentOcMoney, shop.currentRmMoney, shop.restockEveryDay));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.All", e);
            }
            return list;
        }

        public static bool SetMoney(string id, int oc, int rm)
        {
            try
            {
                BaseShop shop = SceneIds.Find<BaseShop>(id);
                if (shop == null || SaveGate.Busy || oc < 0 || rm < 0) return false;
                shop.currentOcMoney = oc;
                shop.currentRmMoney = rm;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.SetMoney", e);
                return false;
            }
        }

        public static bool Restock(string id)
        {
            try
            {
                BaseShop shop = SceneIds.Find<BaseShop>(id);
                if (shop == null || SaveGate.Busy) return false;
                shop.Restock();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Restock", e);
                return false;
            }
        }

        public static BaseShopBuyResult Buy(string id, string furnitureId, int count)
        {
            try
            {
                if (Cash.Refused("BaseShops.Buy") || (ShopController.instance != null && ShopController.instance.menuIsOpen)) return BaseShopBuyResult.Busy;
                if (string.IsNullOrEmpty(furnitureId) || count < 1) return BaseShopBuyResult.Refused;
                BaseShop shop = SceneIds.Find<BaseShop>(id);
                if (shop == null) return BaseShopBuyResult.NotFound;
                if (shop is TrainShop) return BaseShopBuyResult.Unsupported;
                BuildingSystem.FurnitureInfo info = StockOf(shop, furnitureId);
                if (info == null) return BaseShopBuyResult.NotFound;
                if (info.amount < count) return BaseShopBuyResult.OutOfStock;
                if (info.furniture.canPlaceCustomImage && (info.meta == null || !info.meta.HasCustomImages)) return BaseShopBuyResult.Refused;
                if (!Cash.Ready() || BuildingSystem.instance == null) return BaseShopBuyResult.Unavailable;
                float oc = (float)shop.GetOcBuyPrice(info) * count;
                float rm = (float)shop.GetRmBuyPrice(info) * count;
                if (oc > Cash.MaxAmount || rm > Cash.MaxAmount) return BaseShopBuyResult.Refused;
                if (Cash.Balance(MoneyCurrency.OC) < oc || Cash.Balance(MoneyCurrency.RM) < rm) return BaseShopBuyResult.NoMoney;
                if (!Spend(MoneyCurrency.OC, oc)) return BaseShopBuyResult.Failed;
                if (!Spend(MoneyCurrency.RM, rm))
                {
                    Refund(MoneyCurrency.OC, oc);
                    return BaseShopBuyResult.Failed;
                }
                if (!AddToPlayer(info, count))
                {
                    Refund(MoneyCurrency.OC, oc);
                    Refund(MoneyCurrency.RM, rm);
                    return BaseShopBuyResult.Failed;
                }
                if (!RemoveFromBuyShop(shop, info, count))
                {
                    if (RemoveFromPlayer(info, count))
                    {
                        Refund(MoneyCurrency.OC, oc);
                        Refund(MoneyCurrency.RM, rm);
                    }
                    return BaseShopBuyResult.Failed;
                }
                shop.currentOcMoney += (int)oc;
                shop.currentRmMoney += (int)rm;
                RaiseBought(new BaseShopBoughtArgs(SceneIds.Of(shop), shop.GetType().Name, furnitureId, count, (int)oc, (int)rm));
                return BaseShopBuyResult.Ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Buy", e);
                return BaseShopBuyResult.Failed;
            }
        }

        public static BaseShopSellResult Sell(string id, string furnitureId, int count)
        {
            if (selling) return BaseShopSellResult.Busy;
            selling = true;
            BaseShopSoldArgs args = null;
            BaseShopSellResult result;
            try
            {
                result = SellOnce(id, furnitureId, count, out args);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Sell", e);
                result = BaseShopSellResult.Failed;
            }
            finally
            {
                selling = false;
            }
            if (args != null) Events.Raise(Sold, "BaseShops.Sold", h => h(args));
            return result;
        }

        private static BaseShopSellResult SellOnce(string id, string furnitureId, int count, out BaseShopSoldArgs args)
        {
            args = null;
            if (Cash.Refused("BaseShops.Sell") || (ShopController.instance != null && ShopController.instance.menuIsOpen)) return BaseShopSellResult.Busy;
            if (string.IsNullOrEmpty(furnitureId) || count < 1) return BaseShopSellResult.Refused;
            BaseShop shop = SceneIds.Find<BaseShop>(id);
            if (shop == null) return BaseShopSellResult.NotFound;
            if (shop is TrainShop) return BaseShopSellResult.Unsupported;
            if (!Cash.Ready() || BuildingSystem.instance == null) return BaseShopSellResult.Unavailable;
            BuildingSystem.FurnitureInfo info = PlayerStockOf(furnitureId);
            if (info == null) return BaseShopSellResult.NotFound;
            if (info.amount < count) return BaseShopSellResult.OutOfStock;
            if (info.furniture.IsFree() || info.gameObject != null || !shop.CanSell(info)) return BaseShopSellResult.Refused;
            long oc = (long)shop.GetOcSellPrice(info) * count;
            long rm = (long)shop.GetRmSellPrice(info) * count;
            if (oc < 0 || rm < 0 || oc + rm <= 0 || oc > Cash.MaxAmount || rm > Cash.MaxAmount) return BaseShopSellResult.Refused;
            if (shop.currentOcMoney < oc || shop.currentRmMoney < rm) return BaseShopSellResult.NoMoney;
            if (!Cash.Fits(MoneyCurrency.OC, oc) || !Cash.Fits(MoneyCurrency.RM, rm)) return BaseShopSellResult.Refused;
            List<BuildingSystem.FurnitureInfo> stock = BuildingSystem.instance.availableFurnitures;
            int index = stock.IndexOf(info);
            int amount = info.amount;
            BaseShopSoldArgs sold = new BaseShopSoldArgs(SceneIds.Of(shop), shop.GetType().Name, furnitureId, count, (int)oc, (int)rm);
            int done = 0;
            try
            {
                shop.currentOcMoney -= (int)oc;
                shop.currentRmMoney -= (int)rm;
                done = 1;
                if (BuildingSystem.instance.RemoveFurniture(info, count)) done = 2;
                if (done == 2 && shop.AddItem(info.furniture, info.meta, null, count)) done = 3;
                if (done == 3 && Pay(MoneyCurrency.OC, oc)) done = 4;
                if (done == 4 && Pay(MoneyCurrency.RM, rm))
                {
                    args = sold;
                    return BaseShopSellResult.Ok;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Sell step " + done, e);
            }
            if (done >= 4) Take(MoneyCurrency.OC, oc);
            if (done >= 3) RemoveFromShop(shop, info, count);
            if (done >= 1)
            {
                ReturnToPlayer(stock, info, amount, index, done >= 2 ? count : 0);
                shop.currentOcMoney += (int)oc;
                shop.currentRmMoney += (int)rm;
            }
            return BaseShopSellResult.Failed;
        }

        internal static void RaiseBought(BaseShopBoughtArgs args)
        {
            Events.Raise(Bought, "BaseShops.Bought", h => h(args));
        }

        private static BuildingSystem.FurnitureInfo PlayerStockOf(string furnitureId)
        {
            foreach (BuildingSystem.FurnitureInfo info in BuildingSystem.instance.availableFurnitures)
            {
                if (info.furniture != null && info.furniture.Id == furnitureId && info.gameObject == null) return info;
            }
            return null;
        }

        private static bool Pay(MoneyCurrency currency, long amount)
        {
            return amount <= 0 || Cash.Give(currency, amount);
        }

        private static void Take(MoneyCurrency currency, long amount)
        {
            if (amount > 0 && !Cash.Withdraw(currency, amount)) CoreLog.For(Owner).Error("BaseShops.Sell payment undo failed: " + amount + " " + currency + " was not taken back from the wallet");
        }

        private static void RemoveFromShop(BaseShop shop, BuildingSystem.FurnitureInfo info, int count)
        {
            try
            {
                BuildingSystem.FurnitureInfo entry = BuildingSystem.FurnitureInfo.FindFurnitureFromList(shop.GetAvailableItems(), info.furniture, info.meta);
                if (entry != null)
                {
                    shop.RemoveItem(entry, count);
                    return;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Sell stock undo", e);
            }
            CoreLog.For(Owner).Error("BaseShops.Sell stock undo failed: " + count + " of " + info.furniture.Id + " may still be in the shop's stock");
        }

        private static void ReturnToPlayer(List<BuildingSystem.FurnitureInfo> stock, BuildingSystem.FurnitureInfo info, int amount, int index, int taskItems)
        {
            try
            {
                info.amount = amount;
                if (!stock.Contains(info)) stock.Insert(Math.Min(index, stock.Count), info);
                if (taskItems > 0) TaskItemsManager.instance.AddTaskItem(info.taskItem, taskItems);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Sell removal undo", e);
                CoreLog.For(Owner).Error("BaseShops.Sell removal undo failed: " + info.furniture.Id + " may not be back in the building stock");
            }
        }

        private static BuildingSystem.FurnitureInfo StockOf(BaseShop shop, string furnitureId)
        {
            foreach (BuildingSystem.FurnitureInfo info in shop.GetAvailableItems())
            {
                if (info.furniture != null && info.furniture.Id == furnitureId) return info;
            }
            return null;
        }

        private static bool Spend(MoneyCurrency currency, float amount)
        {
            return amount <= 0f || Cash.TrySpend(currency, amount);
        }

        private static void Refund(MoneyCurrency currency, float amount)
        {
            if (amount > 0f && !Cash.Restore(currency, amount)) CoreLog.For(Owner).Error("BaseShops.Buy refund failed: " + amount + " " + currency + " was not returned to the wallet");
        }

        private static bool RemoveFromBuyShop(BaseShop shop, BuildingSystem.FurnitureInfo info, int count)
        {
            try
            {
                return shop.RemoveItem(info, count);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Buy stock", e);
                return false;
            }
        }

        private static bool RemoveFromPlayer(BuildingSystem.FurnitureInfo info, int count)
        {
            try
            {
                BuildingSystem.FurnitureInfo entry = BuildingSystem.FurnitureInfo.FindFurnitureFromList(BuildingSystem.instance.availableFurnitures, info.furniture, info.meta);
                if (entry != null && BuildingSystem.instance.RemoveFurniture(entry, count)) return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Buy delivery undo", e);
            }
            CoreLog.For(Owner).Error("BaseShops.Buy delivery undo failed: " + count + " of " + info.furniture.Id + " stays in the player's building stock, so the payment is not refunded");
            return false;
        }

        private static bool AddToPlayer(BuildingSystem.FurnitureInfo info, int count)
        {
            try
            {
                return BuildingSystem.instance.AddFurniture(info.furniture, count, info.meta);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "BaseShops.Buy delivery", e);
                return false;
            }
        }
    }
}
