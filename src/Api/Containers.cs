using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class ContainerInfo
    {
        public string Guid { get; private set; }
        public int InstanceId { get; private set; }
        public string Name { get; private set; }
        public Vector3 Position { get; private set; }
        public int Apartment { get; private set; }
        public int OwnerId { get; private set; }
        public bool NpcOwned { get; private set; }
        public bool Locked { get; private set; }
        internal Storage Source { get; private set; }

        internal ContainerInfo(Storage source)
        {
            Source = source;
            Guid = source.GUID ?? string.Empty;
            InstanceId = source.GetInstanceID();
            Name = source.GetTitle();
            Position = source.transform.position;
            Apartment = Containers.ApartmentOf(source);
            NpcOwned = Containers.IsNpcOwned(source);
            OwnerId = NpcOwned ? source.StorageOwner.ID : -1;
            Locked = Containers.IsLocked(source);
        }
    }

    public static class Containers
    {
        internal const string Owner = "obenseuer.moddingframework.containers";
        private static Storage open;

        public static event Action<ContainerInfo> Opened;

        public static event Action<ContainerInfo> Closed;

        public static List<ContainerInfo> All()
        {
            var list = new List<ContainerInfo>();
            try
            {
                foreach (Storage storage in SceneIds.Active<Storage>())
                {
                    ContainerInfo info = Describe(storage);
                    if (info != null) list.Add(info);
                }
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Debug("Containers.All", "All failed: " + e.ToString());
            }
            return list;
        }

        public static ContainerInfo FindByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            Storage storage = SaveController.GetObjectByGUID(guid) as Storage;
            if (storage == null) storage = SaveController.GetObjectByGUID(guid, typeof(Storage)) as Storage;
            return Describe(storage);
        }

        public static string GuidOf(ContainerInfo container)
        {
            return container == null ? null : container.Guid;
        }

        public static List<ItemStackInfo> GetItems(ContainerInfo container)
        {
            var list = new List<ItemStackInfo>();
            if (container == null || container.Source == null || container.Source.storageSlots == null) return list;
            try
            {
                for (int i = 0; i < container.Source.storageSlots.Length; i++)
                {
                    global::ItemStack stack = container.Source.storageSlots[i];
                    if (stack != null && stack.itemId != -1 && stack.itemAmount > 0) list.Add(ItemStackInfo.From(stack, i));
                }
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Debug("Containers.GetItems", "GetItems failed: " + e.ToString());
            }
            return list;
        }

        public static bool Give(ContainerInfo container, int itemId, int count)
        {
            PlayerInventory.RequirePositive(count);
            if (container == null || itemId == 0 || itemId == 1 || SaveGate.Busy) return false;
            return PlayerInventory.Guard(() => GiveTo(container.Source, itemId, count), false, "Containers.Give");
        }

        public static bool Take(ContainerInfo container, int itemId, int count)
        {
            PlayerInventory.RequirePositive(count);
            if (container == null || SaveGate.Busy) return false;
            return PlayerInventory.Guard(() => TakeFrom(container.Source, itemId, count), false, "Containers.Take");
        }

        public static ItemStackInfo TakeStack(ContainerInfo container, int slot, int count)
        {
            PlayerInventory.RequirePositive(count);
            if (container == null || SaveGate.Busy) return null;
            global::ItemStack taken = PlayerInventory.Guard(() => TakeAt(container, slot, count), null, "Containers.TakeStack");
            return taken == null ? null : ItemStackInfo.From(taken, slot);
        }

        internal static void Init()
        {
            if (ContainersPatches.Install()) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("container hooks incomplete, Opened and Closed events are disabled");
        }

        internal static void RaiseOpened(Storage storage)
        {
            if (storage == null || open == storage) return;
            open = storage;
            RaiseChanged(storage, Opened, "Containers.Opened");
        }

        internal static void RaiseClosed(Storage storage)
        {
            if (storage == null || open != storage) return;
            open = null;
            RaiseChanged(storage, Closed, "Containers.Closed");
        }

        private static void RaiseChanged(Storage storage, Action<ContainerInfo> handlers, string name)
        {
            if (handlers == null) return;
            ContainerInfo info = Describe(storage);
            if (info != null) Events.Raise(handlers, name, h => h(info));
        }

        internal static ContainerInfo Describe(Storage storage)
        {
            return PlayerInventory.Guard(() => storage != null && storage.Slots != null ? new ContainerInfo(storage) : null, null, "Containers.Describe");
        }

        internal static int ApartmentOf(Component component)
        {
            TenementApartmentPrefabs prefabs = component.GetComponentInParent<TenementApartmentPrefabs>();
            if (prefabs == null)
            {
                FurnitureManager manager = component.GetComponentInParent<FurnitureManager>();
                if (manager != null) prefabs = manager.tenementApartmentPrefabs;
            }
            return prefabs == null ? -1 : prefabs.apartmentNumber;
        }

        internal static bool IsNpcOwned(Storage storage)
        {
            return storage.StorageOwner != null && storage.StorageOwner.ID > 1;
        }

        internal static bool IsLocked(Storage storage)
        {
            return storage._Lock != null && storage._Lock.Locked;
        }

        private static bool GiveTo(Storage storage, int itemId, int count)
        {
            if (storage == null || storage.Slots == null || storage.Slots.Length == 0 || IsLocked(storage)) return false;
            Item item = ItemRegistry.FindInDatabase(itemId);
            if (item == null) return false;
            object[] meta = item.CloneMeta(false, count);
            return Fits(storage.Slots, item, count, meta) && storage.AddItem(item, count, -1, meta);
        }

        private static bool TakeFrom(Storage storage, int itemId, int count)
        {
            if (storage == null || IsLocked(storage) || (IsNpcOwned(storage) && !storage.PermissionToTake)) return false;
            if (storage.GetItemAmount(itemId) < count) return false;
            int remaining;
            storage.RemoveItem(itemId, out remaining, count, false, true, -1);
            PlayerInventory.Guard(() => { storage.UpdateSlots(); return true; }, false, "Containers.Refresh");
            return remaining == 0;
        }

        internal static global::ItemStack StackAt(ContainerInfo container, int slot, int count)
        {
            Storage storage = container == null ? null : container.Source;
            if (storage == null || storage.Slots == null || storage.storageSlots == null || IsLocked(storage) || (IsNpcOwned(storage) && !storage.PermissionToTake)) return null;
            if (slot < 0 || slot >= storage.Slots.Length || slot >= storage.storageSlots.Length || storage.Slots[slot] == null) return null;
            global::ItemStack stack = storage.Slots[slot].itemStack;
            return stack != null && stack == storage.storageSlots[slot] && stack.itemReference != null && stack.itemId != -1 && ShopsPlayerMath.CanTake(storage.Slots.Length, slot, stack.itemAmount, count) ? stack : null;
        }

        internal static global::ItemStack TakeAt(ContainerInfo container, int slot, int count)
        {
            global::ItemStack source = StackAt(container, slot, count);
            if (source == null) return null;
            global::ItemStack taken = new global::ItemStack(source);
            taken.itemAmount = count;
            container.Source.Slots[slot].TakeItem(taken);
            return taken;
        }

        internal static void RestoreAt(ContainerInfo container, int slot, global::ItemStack stack)
        {
            int remaining = container.Source.Slots[slot].AddItemAndReturnRemaining(stack.itemReference.Item, stack.itemAmount, stack.ownerId, stack.Meta);
            if (remaining != 0) CoreLog.For(Owner).Error("Containers.RestoreAt: " + remaining + " of " + stack.itemAmount + " items did not return to slot " + slot);
        }

        private static bool Fits(SlotController[] slots, Item item, int count, object[] meta)
        {
            int left = count;
            foreach (SlotController slot in slots)
            {
                if (slot == null) continue;
                int rest;
                slot.SlotHasSpaceForItem(item, out rest, -1, left, 0, meta);
                left = rest;
                if (left < 1) return true;
            }
            return false;
        }
    }
}
