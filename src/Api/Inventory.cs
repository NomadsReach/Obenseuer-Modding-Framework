using System;
using System.Collections.Generic;

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

    public static class PlayerInventory
    {
        internal const string Owner = "obenseuer.moddingframework.inventory";
        private static bool installed;

        private static readonly Dictionary<int, int> pendingAdded = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> pendingRemoved = new Dictionary<int, int>();
        private static int batchDepth;
        private static int uiDepth;

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
            if (itemId == 0 || itemId == 1 || SaveGate.Busy) return false;
            return Read(inventory => Batched(() => ItemFits(inventory, itemId, count) && inventory.AddItemDialogueAmount(itemId, count)), false, "Give");
        }

        public static bool Take(int itemId, int count)
        {
            RequirePositive(count);
            if (SaveGate.Busy) return false;
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
            Events.Raise(handlers, name, h => h(args));
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
            Item item = ItemDatabase.instance == null ? null : ItemDatabase.instance.FetchItemByID(itemId);
            return item != null && inventory.InventoryIsNotFull(item, -1, count, 1, item.CloneMeta(), inventory.Slots);
        }

        private static bool TakeUnits(global::Inventory inventory, int itemId, int count)
        {
            if (!inventory.CheckForItem(itemId, count)) return false;
            inventory.RemoveItemDialogueAmount(itemId, count);
            return true;
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
    }
}
