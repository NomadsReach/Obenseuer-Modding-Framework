using System;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    internal static class ShopsPatches
    {
        private static readonly Type[] TradeTypes = { typeof(ItemData), typeof(int) };
        private static bool reentered;
        internal static FieldInfo TradeStorage;
        internal static MethodInfo TradeGetCurrency;
        internal static FieldInfo Uncollected;

        private const string TradeOwner = Shops.Owner + ".trade";
        private const string VendingOwner = Shops.Owner + ".vending";
        private const string PriceOwner = Shops.Owner + ".price";

        internal static ShopGroup Install()
        {
            try
            {
                TradeStorage = AccessTools.Field(typeof(Trade), "storage");
                TradeGetCurrency = AccessTools.Method(typeof(Trade), "GetCurrency", new[] { typeof(Trade.PriceMode) });
                Uncollected = AccessTools.Field(typeof(PlayerShopManager.PlayerShopData), "uncollectedEarnings");
                if (TradeStorage == null || TradeGetCurrency == null || Uncollected == null)
                {
                    CorePatcher.DisableOwner(Shops.Owner);
                    return ShopGroup.None;
                }
                bool trade = Hook(TradeOwner, typeof(Trade), "Buy", TradeTypes, "TradeBefore", "BoughtAfter");
                trade &= Hook(TradeOwner, typeof(Trade), "Sell", TradeTypes, "TradeBefore", "SoldAfter");
                trade &= Hook(TradeOwner, typeof(TradeSpecial), "Buy", TradeTypes, "TradeBefore", "BoughtAfter");
                trade &= Hook(TradeOwner, typeof(TradeSpecial), "Sell", TradeTypes, "TradeBefore", "SoldAfter");
                bool vending = Hook(VendingOwner, typeof(VendingMachine), "Buy", new[] { typeof(VendingMachine.VendingMachineItem) }, "VendingBefore", "VendingAfter");
                bool price = Hook(PriceOwner, typeof(Trade), "GetItemStackPrice", new[] { typeof(ItemData), typeof(int), typeof(Money.Currency) }, null, "StackPricePostfix");
                return (trade ? ShopGroup.Trade : ShopGroup.None) | (vending ? ShopGroup.Vending : ShopGroup.None) | (price ? ShopGroup.Price : ShopGroup.None);
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Shops.Owner);
                ApiLog.Once(Cash.Owner, "shops install", e);
                return ShopGroup.None;
            }
        }

        internal static bool Resolved
        {
            get
            {
                return TradeStorage != null && TradeGetCurrency != null && Uncollected != null;
            }
        }

        internal static bool InPriceQuery
        {
            get
            {
                return reentered;
            }
        }

        internal static Money.Currency CurrencyOf(Trade trade, Trade.PriceMode mode)
        {
            return (Money.Currency)TradeGetCurrency.Invoke(trade, new object[] { mode });
        }

        internal static float Adjust(Trade trade, int itemId, int count, bool playerBuys, Money.Currency currency, float price)
        {
            if (!Shops.Installed(ShopGroup.Price)) return price;
            float scaled = Settle(Traders.Scale(trade, playerBuys, price), currency, price, count);
            if (reentered || !Shops.HasPriceQuery) return scaled;
            PriceQueryArgs args = new PriceQueryArgs(Shops.InfoOf(trade), itemId, count, playerBuys, Cash.FromGame(currency), scaled);
            reentered = true;
            try
            {
                Shops.RaisePriceQuery(args);
            }
            finally
            {
                reentered = false;
            }
            return Settle(args.Price, currency, scaled, count);
        }

        private static bool Hook(string owner, Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, prefix == null ? null : CorePatcher.Handler(typeof(ShopsPatches), prefix), CorePatcher.Handler(typeof(ShopsPatches), postfix), null, true, owner);
        }

        private static float Settle(float value, Money.Currency currency, float original, int count)
        {
            if (value == original) return original;
            if (float.IsNaN(value) || float.IsInfinity(value)) return original;
            float floor = (currency == Money.Currency.RM ? Cash.OcToRm(1) : 1f) * count;
            if (value < floor) value = floor;
            if (value > Cash.MaxAmount) value = Cash.MaxAmount;
            return currency == Money.Currency.RM ? Money.RoundRM(value) : (float)Math.Round(value);
        }

        private static float Moved(Money.Currency currency, float before)
        {
            float moved = Math.Abs(Cash.Read(currency) - before);
            return currency == Money.Currency.RM ? Money.RoundRM(moved) : moved;
        }

        private static void TradeBefore(Trade __instance, ItemData itemData, int amount, out TradeState __state)
        {
            __state = null;
            if (!Shops.Installed(ShopGroup.Trade)) return;
            try
            {
                __state = new TradeState(itemData.item.ID, amount, __instance.GetItemCurrency(itemData));
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "trade prefix", e);
            }
        }

        private static void BoughtAfter(Trade __instance, bool __result, TradeState __state)
        {
            Report(__instance, __result, __state, true);
        }

        private static void SoldAfter(Trade __instance, bool __result, TradeState __state)
        {
            Report(__instance, __result, __state, false);
        }

        private static void Report(Trade trade, bool success, TradeState state, bool bought)
        {
            if (!success || state == null || !Shops.Installed(ShopGroup.Trade)) return;
            try
            {
                TradeArgs evt = new TradeArgs(state.ItemId, state.Count, Moved(state.Currency, state.Before), Cash.FromGame(state.Currency), Shops.InfoOf(trade));
                PlayerInventory.AfterCommit(() =>
                {
                    if (bought) Shops.RaiseBought(evt);
                    else Shops.RaiseSold(evt);
                });
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "trade postfix", e);
            }
        }

        private static void VendingBefore(VendingMachine __instance, VendingMachine.VendingMachineItem item, out VendingState __state)
        {
            __state = null;
            if (!Shops.Installed(ShopGroup.Vending)) return;
            try
            {
                __state = new VendingState(item.item.ID, item.currentStock, item.currency);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "vending prefix", e);
            }
        }

        private static void VendingAfter(VendingMachine __instance, VendingMachine.VendingMachineItem item, VendingState __state)
        {
            if (__state == null || item.currentStock >= __state.Stock) return;
            try
            {
                TradeArgs evt = new TradeArgs(__state.ItemId, 1, Moved(__state.Currency, __state.Before), Cash.FromGame(__state.Currency), Shops.InfoOf(__instance));
                Shops.RaiseBought(evt);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "vending postfix", e);
            }
        }

        private static void StackPricePostfix(Trade __instance, ItemData itemData, int amount, Money.Currency currency, ref float __result)
        {
            if (!Shops.Installed(ShopGroup.Price)) return;
            try
            {
                __result = Adjust(__instance, itemData.item.ID, amount, !itemData.slotController.slot.playerSlot, currency, __result);
            }
            catch (Exception e)
            {
                ApiLog.Once(Cash.Owner, "price postfix", e);
            }
        }

        internal sealed class TradeState
        {
            internal readonly int ItemId;
            internal readonly int Count;
            internal readonly Money.Currency Currency;
            internal readonly float Before;

            internal TradeState(int itemId, int count, Money.Currency currency)
            {
                ItemId = itemId;
                Count = count;
                Currency = currency;
                Before = Cash.Read(currency);
            }
        }

        internal sealed class VendingState
        {
            internal readonly int ItemId;
            internal readonly int Stock;
            internal readonly Money.Currency Currency;
            internal readonly float Before;

            internal VendingState(int itemId, int stock, Money.Currency currency)
            {
                ItemId = itemId;
                Stock = stock;
                Currency = currency;
                Before = Cash.Read(currency);
            }
        }
    }
}
