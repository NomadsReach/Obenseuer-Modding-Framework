using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    public sealed class ItemChangedArgs
    {
        public int ItemId { get; private set; }
        public int Count { get; private set; }

        internal ItemChangedArgs(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    public static partial class PlayerInventory
    {
        internal const string Owner = "obenseuer.moddingframework.inventory";
        private static bool installed;

        private static readonly Dictionary<int, int> pendingAdded = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> pendingRemoved = new Dictionary<int, int>();
        private static int batchDepth;
        private static List<Action> deferred;
        private static int uiDepth;
        private static int raiseDepth;
        private const int MaxNestedHandlers = 8;

        public static event Action<ItemChangedArgs> ItemAdded;
        public static event Action<ItemChangedArgs> ItemRemoved;

        public static int FreeSlots
        {
            get
            {
                return Read(inventory => inventory.GetEmptySlotsAmount(false, false), 0, "FreeSlots");
            }
        }

        public static int Capacity
        {
            get
            {
                return Read(inventory => inventory.AllSlots(false, false).Length, 0, "Capacity");
            }
        }

        public static int Count(int itemId)
        {
            return Read(inventory => inventory.GetItemAmountInInventory(itemId), 0, "Count");
        }

        public static bool Has(int itemId, int count)
        {
            RequirePositive(count);
            return Read(inventory => inventory.CheckForItem(itemId, count), false, "Has");
        }

        public static bool Give(int itemId, int count)
        {
            RequirePositive(count);
            if (itemId == 0 || itemId == 1 || SaveGate.Busy || InTransaction || Nested("Give")) return false;
            return Read(inventory => Batched(() => ItemFits(inventory, itemId, count) && inventory.AddItemDialogueAmount(itemId, count)), false, "Give");
        }

        internal static bool GiveWithMeta(Item item, int count, int owner, object[] meta)
        {
            if (item == null || item.ID == 0 || item.ID == 1) return false;
            object[] copy = Item.CloneMeta(meta, true, count, item);
            return Read(inventory => Batched(() => inventory.InventoryIsNotFull(item, owner, count, 1, copy, inventory.Slots) && ItemOperations.AddItems(item, count, owner, copy, null, false)), false, "Give");
        }

        internal static void NewItemNotice(Item item, int count, object[] meta)
        {
            Notifications notifications = Notifications.instance;
            if (notifications == null) return;
            string text = item.GetItemTitle(meta);
            if (count > 1) text += " (" + count + ")";
            notifications.CreateNotification("New item", text);
        }

        public static bool Take(int itemId, int count)
        {
            RequirePositive(count);
            if (SaveGate.Busy || InTransaction || Nested("Take")) return false;
            return Read(inventory => Batched(() => TakeUnits(inventory, itemId, count)), false, "Take");
        }

        public static List<ItemStackInfo> Items()
        {
            return Read(inventory => ItemStacks(inventory), new List<ItemStackInfo>(), "Items");
        }

        public static int GetHeldItemId(bool leftHand)
        {
            return Guard(() => HeldItemId(leftHand), -1, "GetHeldItemId");
        }

        public static bool EquipHeld(string modId, int itemId)
        {
            if (!ValidMod(modId) || SaveGate.Busy || Nested("EquipHeld")) return false;
            try
            {
                global::Inventory inventory = global::Inventory.instance;
                Item item = ItemRegistry.FindInDatabase(itemId);
                if (inventory == null || inventory.Slots == null || inventory.CharacterSlots == null || item == null) return false;
                int index = global::Inventory.GetEquipSlotIndex(item);
                if (index != 6 && index != 7) return false;
                SlotController source = WholeStack(inventory.Slots, itemId);
                SlotController hand = inventory.CharacterSlots[index];
                if (source == null || !hand.CheckIfAllowed(item) || Owned(hand) || TwoHandClash(inventory, index, item)) return false;
                inventory.EquipItem(index == 6 ? "Wieldable Primary" : "Wieldable Secondary", source);
                return hand.itemStack.itemId == itemId;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "EquipHeld", e);
                return false;
            }
        }

        public static bool UnequipHeld(string modId, bool leftHand)
        {
            if (!ValidMod(modId) || SaveGate.Busy || Nested("UnequipHeld")) return false;
            try
            {
                global::Inventory inventory = global::Inventory.instance;
                if (inventory == null || inventory.CharacterSlots == null) return false;
                SlotController hand = inventory.CharacterSlots[leftHand ? 7 : 6];
                if (Empty(hand) || Owned(hand)) return false;
                Item item = ItemRegistry.FindInDatabase(hand.itemStack.itemId);
                if (item == null || Array.IndexOf(item.Categories, "Unstorable") >= 0) return false;
                inventory.Unequip(hand);
                return Empty(hand);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "UnequipHeld", e);
                return false;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        internal static void Init()
        {
            installed = InventoryPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For("ObenseuerCore").Warn("inventory events not installed, ItemAdded and ItemRemoved are disabled");
        }

        internal static bool UiActive
        {
            get { return uiDepth > 0; }
        }

        internal static bool InTransaction
        {
            get { return deferred != null; }
        }

        internal static bool HasHandlers
        {
            get { return ItemAdded != null || ItemRemoved != null || batchDepth > 0; }
        }

        internal static void EnterUi()
        {
            uiDepth++;
        }

        internal static void ExitUi()
        {
            if (uiDepth > 0) uiDepth--;
        }

        internal static void RaiseItemAdded(int itemId, int count)
        {
            if (batchDepth > 0) Accumulate(pendingAdded, itemId, count);
            else RaiseChanged(ItemAdded, "PlayerInventory.ItemAdded", itemId, count);
        }

        internal static void RaiseItemRemoved(int itemId, int count)
        {
            if (batchDepth > 0) Accumulate(pendingRemoved, itemId, count);
            else RaiseChanged(ItemRemoved, "PlayerInventory.ItemRemoved", itemId, count);
        }

        private static void RaiseChanged(Action<ItemChangedArgs> handlers, string name, int itemId, int count)
        {
            ItemChangedArgs args = new ItemChangedArgs(itemId, count);
            raiseDepth++;
            try
            {
                Events.Raise(handlers, name, h => h(args));
            }
            finally
            {
                raiseDepth--;
            }
        }

        private static bool Nested(string member)
        {
            if (raiseDepth <= MaxNestedHandlers) return false;
            ApiLog.Once(Owner, member, "refused: ItemAdded or ItemRemoved handlers nested deeper than " + MaxNestedHandlers);
            return true;
        }

        internal static bool IsPlayerSlot(SlotController slot)
        {
            global::Inventory inventory = global::Inventory.instance;
            if (inventory == null) return false;
            if (Contains(inventory.Slots, slot) || Contains(inventory.CharacterSlots, slot)) return true;
            BackpackStorage backpack = BackpackStorage.instance;
            Storage storage = backpack == null ? null : backpack.GetStorage();
            return storage != null && Contains(storage.Slots, slot);
        }

        private static bool Contains(SlotController[] slots, SlotController slot)
        {
            if (slots == null) return false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == slot) return true;
            }
            return false;
        }

        internal static void RequirePositive(int count)
        {
            if (count <= 0) throw new ArgumentException("count must be greater than zero", "count");
        }

        internal static T Guard<T>(Func<T> read, T fallback, string name)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Debug(name, name + " failed: " + e.ToString());
                return fallback;
            }
        }

        private static T Read<T>(Func<global::Inventory, T> read, T fallback, string name)
        {
            global::Inventory inventory = global::Inventory.instance;
            if (inventory == null) return fallback;
            return Guard(() => read(inventory), fallback, name);
        }

        private static bool Batched(Func<bool> run)
        {
            batchDepth++;
            try
            {
                return run();
            }
            finally
            {
                batchDepth--;
                if (batchDepth == 0) Flush();
            }
        }

        internal static void AfterCommit(Action action)
        {
            if (deferred == null) RunAfterCommit(action);
            else deferred.Add(action);
        }

        private static void RunAfterCommit(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AfterCommit", e);
            }
        }

        internal static bool Transaction(Func<bool> run)
        {
            Dictionary<int, int> added = new Dictionary<int, int>(pendingAdded);
            Dictionary<int, int> removed = new Dictionary<int, int>(pendingRemoved);
            List<Action> outer = deferred;
            List<Action> queued = new List<Action>();
            bool kept = false;
            batchDepth++;
            deferred = queued;
            try
            {
                kept = run();
                return kept;
            }
            finally
            {
                deferred = outer;
                batchDepth--;
                if (!kept)
                {
                    Restore(pendingAdded, added);
                    Restore(pendingRemoved, removed);
                }
                if (batchDepth == 0) Flush();
                if (kept && outer != null) outer.AddRange(queued);
                else if (kept) foreach (Action action in queued) RunAfterCommit(action);
            }
        }

        private static void Restore(Dictionary<int, int> pending, Dictionary<int, int> saved)
        {
            pending.Clear();
            foreach (KeyValuePair<int, int> pair in saved) pending[pair.Key] = pair.Value;
        }

        private static void Accumulate(Dictionary<int, int> pending, int itemId, int count)
        {
            int sum;
            pending.TryGetValue(itemId, out sum);
            pending[itemId] = sum + count;
        }

        private static void Flush()
        {
            var added = new List<KeyValuePair<int, int>>(pendingAdded);
            var removed = new List<KeyValuePair<int, int>>(pendingRemoved);
            pendingAdded.Clear();
            pendingRemoved.Clear();
            foreach (KeyValuePair<int, int> pair in removed) RaiseItemRemoved(pair.Key, pair.Value);
            foreach (KeyValuePair<int, int> pair in added) RaiseItemAdded(pair.Key, pair.Value);
        }

        private static bool ItemFits(global::Inventory inventory, int itemId, int count)
        {
            Item item = ItemRegistry.FindInDatabase(itemId);
            return item != null && inventory.InventoryIsNotFull(item, -1, count, 1, item.CloneMeta(), inventory.Slots);
        }

        private static bool TakeUnits(global::Inventory inventory, int itemId, int count)
        {
            if (LiquidId(inventory, itemId))
            {
                ApiLog.Once(Owner, "Take " + itemId, "refused: the id is a liquid or a slot holds it as a liquid, and the game's remove can match that slot before a real stack, so a take could remove a whole container while the counted amount does not move. Take liquid units with Containers or StackActions instead");
                return false;
            }
            if (!inventory.CheckForItem(itemId, count)) return false;
            int before = inventory.GetItemAmountInInventory(itemId);
            inventory.RemoveItemDialogueAmount(itemId, count);
            int removed;
            TakeResult result = Account(count, before, inventory.GetItemAmountInInventory(itemId), out removed);
            if (result == TakeResult.Exact) return true;
            if (removed > 0) GiveBack(inventory, itemId, before, removed);
            return false;
        }

        private static bool LiquidId(global::Inventory inventory, int itemId)
        {
            Item item = ItemRegistry.FindInDatabase(itemId);
            bool isLiquid = item != null && IsLiquidCategory(item.Categories);
            return LiquidRisk(isLiquid, CarriesLiquidId(inventory, itemId));
        }

        private static bool CarriesLiquidId(global::Inventory inventory, int itemId)
        {
            foreach (SlotController slot in inventory.AllSlots(false, false))
            {
                if (slot == null) continue;
                ItemLiquidData data = slot.itemStack.GetMetaOfType<ItemLiquidData>();
                if (data != null && data.liquidItem != null && data.liquidItem.ID == itemId) return true;
            }
            return false;
        }

        private static void GiveBack(global::Inventory inventory, int itemId, int before, int removed)
        {
            try
            {
                bool added = inventory.AddItemDialogueAmount(itemId, removed);
                int now = inventory.GetItemAmountInInventory(itemId);
                if (added && now == before) return;
                CoreLog.For(Owner).Error("Take could not give back " + removed + " of item " + itemId + ", the player holds " + now + " instead of " + before + ", those units are lost or duplicated");
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Take could not give back " + removed + " of item " + itemId + ", the player items were lost: " + e.ToString());
            }
        }

        private static List<ItemStackInfo> ItemStacks(global::Inventory inventory)
        {
            var list = new List<ItemStackInfo>();
            foreach (SlotController slot in inventory.AllSlots(false, false))
            {
                if (slot.itemStack.itemId != -1 && slot.itemStack.itemAmount > 0) list.Add(ItemStackInfo.From(slot.itemStack));
            }
            return list;
        }

        private static int HeldItemId(bool leftHand)
        {
            PlayerHandItems hands = PlayerHandItems.instance;
            if (hands == null) return -1;
            PlayerHandItems.IPlayerHandItem hand = leftHand ? hands.lefthandItem : hands.righthandItem;
            return hand == null || hand.item == null ? -1 : hand.item.ID;
        }

        private static SlotController WholeStack(SlotController[] slots, int itemId)
        {
            foreach (SlotController slot in slots)
            {
                if (slot != null && slot.itemStack.itemId == itemId && slot.itemStack.itemAmount == 1 && slot.itemStack.ownerId <= 0) return slot;
            }
            return null;
        }

        private static bool Empty(SlotController slot)
        {
            return slot.itemStack.itemId == -1 || slot.itemStack.itemAmount <= 0;
        }

        private static bool Owned(SlotController slot)
        {
            return !Empty(slot) && slot.itemStack.ownerId > 0;
        }

        private static bool TwoHandClash(global::Inventory inventory, int index, Item item)
        {
            SlotController other = inventory.CharacterSlots[index == 6 ? 7 : 6];
            return !Empty(other) && (TwoHanded(item) || TwoHanded(ItemRegistry.FindInDatabase(other.itemStack.itemId)));
        }

        private static bool TwoHanded(Item item)
        {
            ItemEquipable equipable = item == null ? null : item.GetActionOfType<ItemEquipable>() as ItemEquipable;
            return equipable != null && equipable.bothHands;
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }
    }
}
