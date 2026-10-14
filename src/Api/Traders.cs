using System;
using System.Collections.Generic;
using OS.Items;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public sealed class TraderInfo
    {
        public string Id { get; private set; }
        public bool Open { get; private set; }
        public float Money { get; private set; }
        public MoneyCurrency Currency { get; private set; }
        public float BuyFactor { get; private set; }
        public float SellFactor { get; private set; }
        public bool BuysStolen { get; private set; }
        public string[] AllowedCategories { get; private set; }
        public string[] ForbiddenCategories { get; private set; }

        internal TraderInfo(string id, bool open, float money, MoneyCurrency currency, float buyFactor, float sellFactor, bool buysStolen, string[] allowed, string[] forbidden)
        {
            Id = id;
            Open = open;
            Money = money;
            Currency = currency;
            BuyFactor = buyFactor;
            SellFactor = sellFactor;
            BuysStolen = buysStolen;
            AllowedCategories = allowed;
            ForbiddenCategories = forbidden;
        }
    }

    public enum TraderTradeRefusal
    {
        None,
        NotReady,
        BadInput,
        SaveBusy,
        UnknownTrader,
        Closed,
        OutOfStock,
        NoRoom,
        PlayerCannotAfford,
        TraderCannotAfford,
        NotEnoughItems,
        NotAccepted,
        Failed,
        SpecialTrader,
        LiquidItem,
        TradeBusy
    }

    public static class Traders
    {
        internal const string Owner = "obenseuer.moddingframework.traders";
        private const float MaxFactor = 4f;
        private static readonly List<PriceFactor> factors = new List<PriceFactor>();

        public static bool IsAvailable
        {
            get
            {
                return ShopsPatches.Resolved;
            }
        }

        public static bool PriceFactorsAvailable
        {
            get
            {
                return Shops.Installed(ShopGroup.Price);
            }
        }

        public static TraderInfo Get(string shopId)
        {
            if (string.IsNullOrEmpty(shopId) || !IsAvailable) return null;
            try
            {
                Trade trade = Shops.FindTrader(shopId);
                return trade == null ? null : InfoOf(trade);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Get", e);
                return null;
            }
        }

        public static bool SetMoney(string modId, string shopId, float amount)
        {
            if (!ValidMod(modId) || string.IsNullOrEmpty(shopId) || !IsAvailable || SaveGate.Busy || PlayerInventory.InTransaction) return false;
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f || amount > Cash.MaxAmount)
            {
                ApiLog.Once(Owner, "amount", "amount must be 0 to " + Cash.MaxAmount);
                return false;
            }
            try
            {
                Trade trade = Shops.FindTrader(shopId);
                if (trade == null) return false;
                trade.CurrentMoney = Whole(trade, amount);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetMoney", e);
                return false;
            }
        }

        public static bool Restock(string modId, string shopId)
        {
            if (!ValidMod(modId) || string.IsNullOrEmpty(shopId) || !IsAvailable || SaveGate.Busy || PlayerInventory.InTransaction) return false;
            try
            {
                Trade trade = Shops.FindTrader(shopId);
                if (trade == null) return false;
                float wealth = DifficultyController.instance.currentDifficultyInfo.GetTraderWealth(UnityEngine.Random.Range(trade.traderMoneyAmountMin, trade.traderMoneyAmountMax));
                trade.CurrentMoney = Whole(trade, wealth);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Restock", e);
                return false;
            }
        }

        public static bool SetPriceFactors(string modId, string shopId, float buy, float sell)
        {
            if (!ValidMod(modId) || string.IsNullOrEmpty(shopId) || !PriceFactorsAvailable) return false;
            if (!ValidFactor(buy) || !ValidFactor(sell))
            {
                ApiLog.Once(Owner, "factor", "price factors must be above 0 and at most " + MaxFactor);
                return false;
            }
            try
            {
                if (Shops.FindTrader(shopId) == null) return false;
                PriceFactor factor = factors.Find(f => f.ModId == modId && f.ShopId == shopId);
                if (factor == null)
                {
                    factors.Add(new PriceFactor { ModId = modId, ShopId = shopId, Buy = buy, Sell = sell });
                }
                else
                {
                    factor.Buy = buy;
                    factor.Sell = sell;
                }
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetPriceFactors", e);
                return false;
            }
        }

        public static bool ClearPriceFactors(string modId, string shopId)
        {
            if (!ValidMod(modId) || string.IsNullOrEmpty(shopId)) return false;
            return factors.RemoveAll(f => f.ModId == modId && f.ShopId == shopId) > 0;
        }

        public static bool Buy(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal)
        {
            return Report(Deal(modId, traderId, itemId, count, true), "Buy", out refusal);
        }

        public static bool Sell(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal)
        {
            return Report(Deal(modId, traderId, itemId, count, false), "Sell", out refusal);
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
        }

        internal static float Scale(Trade trade, bool playerBuys, float price)
        {
            if (factors.Count == 0) return price;
            return price * Product(Shops.InfoOf(trade).Id, playerBuys);
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            factors.Clear();
        }

        private static TraderInfo InfoOf(Trade trade)
        {
            string id = Shops.InfoOf(trade).Id;
            MoneyCurrency currency = Cash.FromGame(ShopsPatches.CurrencyOf(trade, trade.purchasePriceMode));
            return new TraderInfo(id, trade.open, trade.CurrentMoney, currency, Product(id, true), Product(id, false), trade.buysStolen, (string[])trade.allowedCategories.Clone(), (string[])trade.forbiddenCategories.Clone());
        }

        private static float Product(string shopId, bool playerBuys)
        {
            float product = 1f;
            foreach (PriceFactor factor in factors)
            {
                if (factor.ShopId == shopId) product *= playerBuys ? factor.Buy : factor.Sell;
            }
            return product;
        }

        private static float Whole(Trade trade, float value)
        {
            MoneyCurrency currency = Cash.FromGame(ShopsPatches.CurrencyOf(trade, trade.purchasePriceMode));
            float clamped = Math.Min(Math.Max(value, 0f), Cash.MaxAmount);
            return currency == MoneyCurrency.RM ? Money.RoundRM(clamped) : (float)Math.Round(clamped);
        }

        private static bool ValidFactor(float factor)
        {
            return !float.IsNaN(factor) && factor > 0f && factor <= MaxFactor;
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }

        private static bool Report(TraderTradeRefusal reason, string what, out TraderTradeRefusal refusal)
        {
            refusal = reason;
            if (reason == TraderTradeRefusal.None) return true;
            ApiLog.Once(Owner, what + " " + reason, "refused: " + reason);
            return false;
        }

        private static TraderTradeRefusal Deal(string modId, string traderId, int itemId, int count, bool buy)
        {
            if (!ValidMod(modId) || string.IsNullOrEmpty(traderId) || count < 1) return TraderTradeRefusal.BadInput;
            if (PlayerInventory.InTransaction || ShopsPatches.InPriceQuery) return TraderTradeRefusal.TradeBusy;
            if (SaveGate.Busy) return TraderTradeRefusal.SaveBusy;
            if (!IsAvailable || !Cash.Ready() || global::Inventory.instance == null) return TraderTradeRefusal.NotReady;
            Item item = Shops.ItemOf(itemId);
            if (item == null) return TraderTradeRefusal.BadInput;
            if (IsLiquid(item)) return TraderTradeRefusal.LiquidItem;
            Trade trade = Shops.FindTrader(traderId);
            if (trade == null) return TraderTradeRefusal.UnknownTrader;
            if (!trade.open) return TraderTradeRefusal.Closed;
            if (trade is TradeSpecial) return TraderTradeRefusal.SpecialTrader;
            Storage stock = ShopsPatches.TradeStorage.GetValue(trade) as Storage;
            if (stock == null || stock.Slots == null) return TraderTradeRefusal.NotReady;
            UnityEngine.GameObject holder = new UnityEngine.GameObject("ObenseuerCore.TraderDeal");
            holder.SetActive(false);
            TraderTradeRefusal reason = TraderTradeRefusal.Failed;
            try
            {
                Slot slot = holder.AddComponent<Slot>();
                slot.playerSlot = !buy;
                ItemData data = holder.AddComponent<ItemData>();
                data.slotController = new SlotController { slot = slot };
                data.item = item;
                PlayerInventory.Transaction(() =>
                {
                    reason = buy ? BuyFrom(trade, stock, data, count) : SellTo(trade, stock, data, count);
                    return reason == TraderTradeRefusal.None;
                });
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, buy ? "Buy" : "Sell", e);
                reason = TraderTradeRefusal.Failed;
            }
            finally
            {
                UnityEngine.Object.Destroy(holder);
            }
            return reason;
        }

        private static TraderTradeRefusal BuyFrom(Trade trade, Storage stock, ItemData data, int count)
        {
            int itemId = data.item.ID;
            float newPrice = Trade.GetItemPrice(data.item, -1, data.item.CloneMeta(), Money.Currency.OC, trade.basePurchasePrice, trade.stolenPriceFactor);
            int missing;
            List<Chunk> chunks = Chunks(stock.Slots, itemId, count, s => SameAsNew(trade, data.item, s, newPrice), out missing);
            if (missing > 0) return TraderTradeRefusal.OutOfStock;
            Load(data, chunks[0].Controller.itemStack, count);
            if (!trade.CanBuy(data, count, false)) return TraderTradeRefusal.PlayerCannotAfford;
            Wallet wallet = new Wallet();
            StackState[] shelf = Capture(stock.Slots);
            StackState[] pack = Capture(PlayerSlots());
            int packBefore = Units(pack, itemId);
            float stockBefore = stock.GetItemAmount(itemId);
            TraderTradeRefusal reason = TraderTradeRefusal.Failed;
            try
            {
                if (!GiveChunks(data.item, chunks) || Units(pack, itemId) - packBefore != count) reason = TraderTradeRefusal.NoRoom;
                else
                {
                    foreach (Chunk chunk in chunks) chunk.Controller.TakeItem(chunk.Amount);
                    if (stockBefore - stock.GetItemAmount(itemId) == count && wallet.Step(trade, data, count, true)) reason = TraderTradeRefusal.None;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Buy", e);
            }
            if (reason == TraderTradeRefusal.None)
            {
                PlayerInventory.AfterCommit(() => PlayerInventory.NewItemNotice(data.item, count, data.meta));
                return reason;
            }
            wallet.Undo(trade);
            RollBack(shelf, pack);
            return reason;
        }

        private static bool GiveChunks(Item item, List<Chunk> chunks)
        {
            foreach (Chunk chunk in chunks)
            {
                ItemStack stack = chunk.Controller.itemStack;
                if (!PlayerInventory.GiveWithMeta(item, chunk.Amount, stack.ownerId, stack.Meta)) return false;
            }
            return true;
        }

        private static TraderTradeRefusal SellTo(Trade trade, Storage stock, ItemData data, int count)
        {
            int itemId = data.item.ID;
            SlotController[] held = PlayerSlots();
            int missing;
            List<Chunk> chunks = Chunks(held, itemId, count, s => Sellable(trade, data, s), out missing);
            if (chunks.Count == 0 || missing > 0) return Declined(trade, data, held) ? TraderTradeRefusal.NotAccepted : TraderTradeRefusal.NotEnoughItems;
            Wallet wallet = new Wallet();
            StackState[] shelf = Capture(stock.Slots);
            StackState[] pack = Capture(held);
            TraderTradeRefusal reason = TraderTradeRefusal.Failed;
            try
            {
                foreach (Chunk chunk in chunks)
                {
                    reason = SellChunk(trade, stock, data, chunk, wallet);
                    if (reason != TraderTradeRefusal.None) break;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Sell", e);
                reason = TraderTradeRefusal.Failed;
            }
            if (reason == TraderTradeRefusal.None) return reason;
            wallet.Undo(trade);
            RollBack(shelf, pack);
            return reason;
        }

        private static TraderTradeRefusal SellChunk(Trade trade, Storage stock, ItemData data, Chunk chunk, Wallet wallet)
        {
            ItemStack stack = chunk.Controller.itemStack;
            int count = chunk.Amount;
            Load(data, stack, count);
            if (!trade.CanSell(data, count, false)) return SellRefusal(trade, data, count);
            int before = stack.itemAmount;
            float stockBefore = stock.GetItemAmount(data.item.ID);
            chunk.Controller.TakeItem(count);
            if (before - stack.itemAmount != count) return TraderTradeRefusal.NotEnoughItems;
            if (!trade.destroyPurchasedItems && !Stored(stock, data, count, stockBefore)) return TraderTradeRefusal.NoRoom;
            return wallet.Step(trade, data, count, false) ? TraderTradeRefusal.None : TraderTradeRefusal.NotAccepted;
        }

        private static List<Chunk> Chunks(SlotController[] slots, int itemId, int count, Func<ItemStack, bool> accept, out int missing)
        {
            List<Chunk> chunks = new List<Chunk>();
            missing = count;
            foreach (SlotController slot in slots)
            {
                ItemStack stack = slot == null ? null : slot.itemStack;
                if (missing < 1 || stack == null || stack.itemId != itemId || stack.itemAmount < 1 || !accept(stack)) continue;
                int amount = Math.Min(missing, stack.itemAmount);
                chunks.Add(new Chunk { Controller = slot, Amount = amount });
                missing -= amount;
            }
            return chunks;
        }

        private static void Load(ItemData data, ItemStack stack, int amount)
        {
            data.owner = stack.ownerId;
            data.meta = stack.Meta;
            data.amount = amount;
        }

        private static bool SameAsNew(Trade trade, Item item, ItemStack stack, float newPrice)
        {
            return stack.ownerId <= 0 && Item.GetMetaOfType<ItemStolenData>(stack.Meta) == null && Trade.GetItemPrice(item, stack.ownerId, stack.Meta, Money.Currency.OC, trade.basePurchasePrice, trade.stolenPriceFactor) == newPrice;
        }

        private static bool Stored(Storage stock, ItemData data, int count, float before)
        {
            return stock.Slots[0].AddItemAndReturnRemaining(data.item, count, data.owner, data.meta, dropItems: false) == 0 && stock.GetItemAmount(data.item.ID) - before == count;
        }

        private static TraderTradeRefusal SellRefusal(Trade trade, ItemData data, int count)
        {
            if (!trade.CheckIfTraderHasMoney(data, count, false)) return TraderTradeRefusal.TraderCannotAfford;
            if (trade.IsStolen(data) || !trade.CheckIfAllowed(data)) return TraderTradeRefusal.NotAccepted;
            return TraderTradeRefusal.NoRoom;
        }

        private static bool Sellable(Trade trade, ItemData data, ItemStack stack)
        {
            Load(data, stack, 1);
            return !trade.IsStolen(data) && trade.CheckIfAllowed(data);
        }

        private static bool Declined(Trade trade, ItemData data, SlotController[] slots)
        {
            foreach (SlotController slot in slots)
            {
                ItemStack stack = slot == null ? null : slot.itemStack;
                if (stack == null || stack.itemId != data.item.ID || stack.itemAmount < 1) continue;
                Load(data, stack, 1);
                if (!trade.CheckIfAllowed(data)) return true;
            }
            return false;
        }

        private sealed class Wallet
        {
            private Money.Currency currency;
            private float player;
            private float shop;
            private int tradeBalance;

            internal bool Step(Trade trade, ItemData data, int count, bool buy)
            {
                currency = trade.GetItemCurrency(data);
                float playerBefore = Cash.Read(currency);
                float shopBefore = trade.CurrentMoney;
                int balanceBefore = trade.cumulativeTradeBalance;
                try
                {
                    return buy ? trade.Buy(data, count) : trade.Sell(data, count);
                }
                finally
                {
                    player += Cash.Read(currency) - playerBefore;
                    shop += trade.CurrentMoney - shopBefore;
                    tradeBalance += trade.cumulativeTradeBalance - balanceBefore;
                }
            }

            internal void Undo(Trade trade)
            {
                MoneyCurrency mine = Cash.FromGame(currency);
                bool restored = player == 0f || (player > 0f ? Cash.Withdraw(mine, player) : Cash.Restore(mine, -player));
                if (!restored) CoreLog.For(Owner).Error("trade money undo failed: " + mine + " moved " + player);
                trade.CurrentMoney -= shop;
                trade.cumulativeTradeBalance -= tradeBalance;
            }
        }

        private static bool IsLiquid(Item item)
        {
            return (item.Categories != null && Array.IndexOf(item.Categories, "Liquid") >= 0) || Item.GetMetaOfType<ItemLiquidData>(item.Meta) != null;
        }

        private static SlotController[] PlayerSlots()
        {
            return global::Inventory.instance.AllSlots(false, false);
        }

        private static StackState[] Capture(SlotController[] slots)
        {
            List<StackState> states = new List<StackState>();
            foreach (SlotController slot in slots)
            {
                if (slot == null || slot.itemStack == null) continue;
                ItemStack stack = slot.itemStack;
                states.Add(new StackState { Controller = slot, Stack = stack, ItemId = stack.itemId, Amount = stack.itemAmount, OwnerId = stack.ownerId, Reference = stack.itemReference, Refigerated = stack.refigerated, SpoilDisabled = stack.spoilDisabled, Meta = stack.Meta });
            }
            return states.ToArray();
        }

        private static int Units(StackState[] states, int itemId)
        {
            int total = 0;
            foreach (StackState state in states)
            {
                if (state.Stack.itemId == itemId) total += state.Stack.itemAmount;
            }
            return total;
        }

        private static void Write(StackState[] states)
        {
            foreach (StackState state in states)
            {
                ItemStack stack = state.Stack;
                stack.itemId = state.ItemId;
                stack.itemAmount = state.Amount;
                stack.ownerId = state.OwnerId;
                stack.itemReference = state.Reference;
                stack.refigerated = state.Refigerated;
                stack.spoilDisabled = state.SpoilDisabled;
                stack.Meta = state.Meta;
            }
        }

        private static void Show(StackState[] states)
        {
            foreach (StackState state in states)
            {
                state.Controller.UpdateNow();
            }
        }

        private static void RollBack(StackState[] shelf, StackState[] pack)
        {
            Write(shelf);
            Write(pack);
            Show(shelf);
            Show(pack);
        }

        private sealed class StackState
        {
            internal SlotController Controller;
            internal ItemStack Stack;
            internal int ItemId;
            internal int Amount;
            internal int OwnerId;
            internal ItemReference Reference;
            internal float Refigerated;
            internal bool SpoilDisabled;
            internal object[] Meta;
        }

        private sealed class Chunk
        {
            internal SlotController Controller;
            internal int Amount;
        }

        private sealed class PriceFactor
        {
            internal string ModId;
            internal string ShopId;
            internal float Buy;
            internal float Sell;
        }
    }
}
