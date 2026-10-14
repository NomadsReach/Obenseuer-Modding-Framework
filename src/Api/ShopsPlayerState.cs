using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class PlayerShopState
    {
        public string Shop = "";
        public string Manager = "";
        public bool Open;
        public float Markup;
        public int TillOc;
        public float TillRm;
        public Dictionary<int, int> Prices = new Dictionary<int, int>();
    }

    [Serializable]
    internal sealed class PlayerShopSaveFile
    {
        public List<PlayerShopState> Shops = new List<PlayerShopState>();
    }

    internal static class ShopsPlayerState
    {
        private static readonly Dictionary<string, PlayerShopState> states = new Dictionary<string, PlayerShopState>();

        internal static PlayerShopState StateOf(string shopId)
        {
            PlayerShopState state;
            return string.IsNullOrEmpty(shopId) || !states.TryGetValue(shopId, out state) ? null : state;
        }

        internal static PlayerShopState Of(PlayerShopManager.PlayerShopData data)
        {
            return data == null || data.parent == null ? null : StateOf(data.parent.GUID);
        }

        internal static PlayerShopState Track(string shopId)
        {
            PlayerShopState state = StateOf(shopId);
            if (state != null) return state;
            PlayerShopManager shop = PlayerShops.Find(shopId);
            if (shop == null) return null;
            Money.money owed = PlayerShops.Uncollected(shop.data);
            state = new PlayerShopState { Shop = shopId, Open = shop.data.isOpen, Markup = ShopsPlayerMath.Factor(shop.data.GeneralPriceFactor), TillOc = owed.OC, TillRm = owed.RM };
            states[shopId] = state;
            return state;
        }

        internal static PlayerShopState ConvertedBy(string managerGuid)
        {
            if (string.IsNullOrEmpty(managerGuid)) return null;
            foreach (PlayerShopState state in states.Values)
            {
                if (state.Manager == managerGuid) return state;
            }
            return null;
        }

        internal static void Put(PlayerShopState state)
        {
            states[state.Shop] = state;
        }

        internal static void Remove(string shopId)
        {
            states.Remove(shopId);
        }

        internal static void Collected(string shopId)
        {
            PlayerShopState state = StateOf(shopId);
            if (state == null) return;
            state.TillOc = 0;
            state.TillRm = 0f;
        }

        internal static void Credit(PlayerShopState state, MoneyCurrency currency, float amount)
        {
            if (currency == MoneyCurrency.RM)
            {
                state.TillRm = Money.RoundRM(Math.Min(state.TillRm + amount, Cash.MaxAmount));
                return;
            }
            state.TillOc = (int)Math.Min(state.TillOc + amount, Cash.MaxAmount);
        }

        internal static void Debit(PlayerShopState state, MoneyCurrency currency, float amount)
        {
            if (currency == MoneyCurrency.RM)
            {
                state.TillRm = Money.RoundRM(state.TillRm - amount);
                return;
            }
            state.TillOc -= (int)amount;
        }

        internal static void Write(PlayerShopManager shop, PlayerShopState state)
        {
            bool converted = !string.IsNullOrEmpty(state.Manager);
            shop.data.parent = shop;
            shop.data.isOpen = state.Open;
            shop.data.GeneralPriceFactor = state.Markup;
            shop.data.residentsOnly = ShopsPlayerMath.AreaFlag(converted, shop.data.residentsOnly);
            shop.data.invalidArea = ShopsPlayerMath.AreaFlag(converted, shop.data.invalidArea);
            ShopsPatches.Uncollected.SetValue(shop.data, new Money.money(state.TillOc, state.TillRm));
        }

        internal static FurnitureManager FindManager(string managerGuid)
        {
            foreach (FurnitureManager manager in SceneIds.Active<FurnitureManager>(FindObjectsInactive.Include))
            {
                if (manager.GUID == managerGuid) return manager;
            }
            return null;
        }

        internal static PlayerShopManager ShopOf(FurnitureManager manager)
        {
            if (manager.Shop != null) return manager.Shop;
            foreach (PlayerShopManager shop in SceneIds.Active<PlayerShopManager>(FindObjectsInactive.Include))
            {
                if (ManagerOf(shop) == manager) return shop;
            }
            return null;
        }

        internal static FurnitureManager ManagerOf(PlayerShopManager shop)
        {
            return shop.manager != null ? shop.manager : ParentManager(shop.transform);
        }

        private static FurnitureManager ParentManager(Transform at)
        {
            for (; at != null; at = at.parent)
            {
                FurnitureManager manager = at.GetComponent<FurnitureManager>();
                if (manager != null) return manager;
            }
            return null;
        }

        internal static PlayerShopManager AddShop(FurnitureManager manager, string shopId)
        {
            PlayerShopManager shop = manager.gameObject.AddComponent<PlayerShopManager>();
            shop.manager = manager;
            shop.GUID = shopId;
            shop.data.parent = shop;
            manager.Shop = shop;
            foreach (InteractableCashRegister register in ComponentsOf<InteractableCashRegister>(manager))
            {
                register.manager = shop;
                shop.AddCashRegister(register);
            }
            return shop;
        }

        internal static void Unlink(PlayerShopManager shop)
        {
            FurnitureManager manager = shop.manager;
            if (manager == null) return;
            if (manager.Shop == shop) manager.Shop = null;
            foreach (InteractableCashRegister register in ComponentsOf<InteractableCashRegister>(manager))
            {
                if (register.manager == shop) register.manager = null;
            }
        }

        internal static void ApplyAll()
        {
            if (!PlayerShops.IsAvailable) return;
            foreach (PlayerShopState state in new List<PlayerShopState>(states.Values))
            {
                try
                {
                    ApplyOne(state);
                }
                catch (Exception e)
                {
                    ApiLog.Once(PlayerShops.Owner, "player shop apply " + state.Shop, e);
                }
            }
        }

        internal static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && !SaveGate.Loading) ApplyAll();
        }

        internal static void OnActiveSceneChanged(Scene previous, Scene next)
        {
            try
            {
                if (!SaveGate.Loading) ApplyAll();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerShops.Owner, "player shop active scene", e);
            }
        }

        internal static PlayerShopSaveFile Save()
        {
            PlayerShopSaveFile file = new PlayerShopSaveFile();
            file.Shops.AddRange(states.Values);
            return file;
        }

        internal static void Load(PlayerShopSaveFile file)
        {
            states.Clear();
            if (file != null && file.Shops != null)
            {
                foreach (PlayerShopState state in file.Shops)
                {
                    if (state == null || string.IsNullOrEmpty(state.Shop)) continue;
                    if (state.Prices == null) state.Prices = new Dictionary<int, int>();
                    states[state.Shop] = state;
                }
            }
            ApplyAll();
        }

        private static void ApplyOne(PlayerShopState state)
        {
            PlayerShopManager shop = PlayerShops.Find(state.Shop);
            if (shop == null && !string.IsNullOrEmpty(state.Manager))
            {
                FurnitureManager manager = FindManager(state.Manager);
                if (manager != null && ShopOf(manager) == null) shop = AddShop(manager, state.Shop);
            }
            if (shop != null) Write(shop, state);
        }

        internal static List<T> ComponentsOf<T>(FurnitureManager manager) where T : Component
        {
            List<T> found = new List<T>();
            foreach (BuildingSystem.FurnitureInfo info in manager.AllFurnitures)
            {
                if (info != null && info.gameObject != null) found.AddRange(info.gameObject.GetComponentsInChildren<T>(true));
            }
            return found;
        }
    }
}
