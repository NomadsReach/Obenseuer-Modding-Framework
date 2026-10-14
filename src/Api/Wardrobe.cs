using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    public sealed class WardrobeChangedArgs
    {
        public string Slot { get; private set; }
        public int ItemId { get; private set; }

        internal WardrobeChangedArgs(string slot, int itemId)
        {
            Slot = slot;
            ItemId = itemId;
        }
    }

    public static class Wardrobe
    {
        internal const string Owner = "obenseuer.moddingframework.wardrobe";
        internal const int MaxNestedHandlers = 8;
        private static readonly string[] slotNames = { "Head", "Face", "Watch", "Backpack" };
        private static readonly int[] slotIndexes = { 2, 3, 4, 5 };
        private static bool installed;
        private static int nestedHandlers;
        private static bool refusalLogged;

        public static event Action<WardrobeChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<string> Slots()
        {
            return new List<string>(slotNames);
        }

        public static int WornOn(string slot)
        {
            int index = IndexOf(slot);
            return Guard(() => ItemIdOf(index), -1, "WornOn");
        }

        public static bool Equip(int itemId)
        {
            if (itemId < 0) throw new ArgumentException("itemId must not be negative", "itemId");
            if (Refused("Wardrobe.Equip")) return false;
            return Guard(() => EquipFromBag(itemId), false, "Equip");
        }

        public static bool Unequip(string slot)
        {
            int index = IndexOf(slot);
            if (Refused("Wardrobe.Unequip")) return false;
            return Guard(() => UnequipSlot(index), false, "Unequip");
        }

        internal static void Init()
        {
            installed = WardrobePatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("wardrobe events not installed, Changed is disabled");
        }

        internal static bool IsSlot(string slot)
        {
            return slot != null && Array.IndexOf(slotNames, slot) >= 0;
        }

        internal static string SlotOf(SlotController controller)
        {
            Inventory inventory = Inventory.instance;
            if (controller == null || inventory == null || inventory.CharacterSlots == null) return null;
            for (int i = 0; i < slotIndexes.Length; i++)
            {
                if (slotIndexes[i] < inventory.CharacterSlots.Length && inventory.CharacterSlots[slotIndexes[i]] == controller) return slotNames[i];
            }
            return null;
        }

        internal static bool HasHandlers
        {
            get
            {
                return Changed != null;
            }
        }

        internal static int StateOf(SlotController controller)
        {
            return SlotOf(controller) == null ? -2 : ItemIdOf(controller);
        }

        internal static void RaiseChanged(string slot)
        {
            WardrobeChangedArgs args = new WardrobeChangedArgs(slot, WornOn(slot));
            nestedHandlers++;
            try
            {
                Events.Raise(Changed, "Wardrobe.Changed", h => h(args));
            }
            finally
            {
                nestedHandlers--;
            }
        }

        internal static bool Refused(string member)
        {
            if (SaveGate.Busy) return true;
            if (nestedHandlers <= MaxNestedHandlers) return false;
            if (!refusalLogged)
            {
                refusalLogged = true;
                CoreLog.For(Owner).Warn(member + " refused: Wardrobe.Changed handlers nested deeper than " + MaxNestedHandlers + ", further refusals are not logged");
            }
            return true;
        }

        private static int IndexOf(string slot)
        {
            if (slot == null) throw new ArgumentException("slot must not be null", "slot");
            int position = Array.IndexOf(slotNames, slot);
            if (position < 0) throw new ArgumentException("unknown wardrobe slot: " + slot, "slot");
            return slotIndexes[position];
        }

        private static int ItemIdOf(int index)
        {
            Inventory inventory = Inventory.instance;
            if (inventory == null || inventory.CharacterSlots == null || index >= inventory.CharacterSlots.Length) return -1;
            return ItemIdOf(inventory.CharacterSlots[index]);
        }

        private static int ItemIdOf(SlotController controller)
        {
            if (controller == null || controller.itemStack == null || controller.itemStack.itemAmount <= 0) return -1;
            return controller.itemStack.itemId;
        }

        private static bool EquipFromBag(int itemId)
        {
            Inventory inventory = Inventory.instance;
            if (inventory == null || inventory.Slots == null) return false;
            SlotController source = null;
            foreach (SlotController slot in inventory.Slots)
            {
                if (slot != null && slot.itemStack != null && slot.itemStack.itemId == itemId && slot.itemStack.itemAmount > 0)
                {
                    source = slot;
                    break;
                }
            }
            if (source == null) return false;
            if (!OwnerAllows(source))
            {
                ApiLog.Once(Owner, "Equip", "the item is owned and its owner does not allow use, it was not equipped");
                return false;
            }
            Item item = ItemRegistry.FindInDatabase(itemId);
            string target = SlotFor(item);
            if (target == null) return false;
            int index = IndexOf(target);
            if (inventory.CharacterSlots == null || index >= inventory.CharacterSlots.Length) return false;
            SlotController worn = inventory.CharacterSlots[index];
            if (worn == null || !worn.CheckIfAllowed(item)) return false;
            if (Owned(worn))
            {
                ApiLog.Once(Owner, "Equip", "the worn item is owned, it was not swapped out");
                return false;
            }
            inventory.EquipItem(target, source);
            return WornOn(target) == itemId;
        }

        private static bool Owned(SlotController slot)
        {
            return ItemIdOf(slot) >= 0 && slot.itemStack.ownerId > 0;
        }

        private static bool OwnerAllows(SlotController slot)
        {
            object[] meta = slot.itemStack.Meta;
            if (meta == null) return true;
            foreach (object obj in meta)
            {
                if (obj is ItemOwnerPermission) return ((ItemOwnerPermission)obj).allowUse;
            }
            return true;
        }

        private static string SlotFor(Item item)
        {
            if (item == null || item.Categories == null) return null;
            foreach (string category in item.Categories)
            {
                if (IsSlot(category)) return category;
            }
            return null;
        }

        private static bool UnequipSlot(int index)
        {
            Inventory inventory = Inventory.instance;
            if (inventory == null || inventory.CharacterSlots == null || index >= inventory.CharacterSlots.Length) return false;
            SlotController worn = inventory.CharacterSlots[index];
            if (ItemIdOf(worn) < 0) return false;
            if (Owned(worn))
            {
                ApiLog.Once(Owner, "Unequip", "the worn item is owned, it was not taken off");
                return false;
            }
            inventory.Unequip(worn);
            return ItemIdOf(worn) < 0;
        }

        private static T Guard<T>(Func<T> read, T fallback, string name)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Debug(name, name + " failed: " + e.ToString());
                return fallback;
            }
        }
    }
}
