using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class MarketShopInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public bool Locked { get; private set; }

        public int Price { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        internal MarketShopInfo(string id, string name, bool locked, int price, MoneyCurrency currency)
        {
            Id = id;
            Name = name;
            Locked = locked;
            Price = price;
            Currency = currency;
        }
    }

    public sealed class MarketPayArgs
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public int Price { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        internal MarketPayArgs(string id, string name, int price, MoneyCurrency currency)
        {
            Id = id;
            Name = name;
            Price = price;
            Currency = currency;
        }
    }

    public static class MarketShops
    {
        internal const string Owner = "obenseuer.moddingframework.marketshops";
        private static bool installed;

        public static event Action<MarketPayArgs> Paid;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<MarketShopInfo> All()
        {
            List<MarketShopInfo> list = new List<MarketShopInfo>();
            try
            {
                foreach (MarketShop shop in SceneIds.Active<MarketShop>())
                {
                    list.Add(new MarketShopInfo(SceneIds.Of(shop), shop.marketName, shop.marketLock != null && shop.marketLock.Locked, shop.GetCurrentPrice(), Cash.FromGame(shop.currency)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "MarketShops.All", e);
            }
            return list;
        }

        public static bool SetLocked(string id, bool locked)
        {
            try
            {
                MarketShop shop = SceneIds.Find<MarketShop>(id);
                if (shop == null || shop.marketLock == null || SaveGate.Busy) return false;
                if (locked) shop.Lock();
                else shop.Unlock();
                return shop.marketLock.Locked == locked;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "MarketShops.SetLocked", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = MarketShopsPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static void RaisePaid(MarketShop shop, int price)
        {
            MarketPayArgs args = new MarketPayArgs(SceneIds.Of(shop), shop.marketName, price, Cash.FromGame(shop.currency));
            Events.Raise(Paid, "MarketShops.Paid", h => h(args));
        }
    }
}
