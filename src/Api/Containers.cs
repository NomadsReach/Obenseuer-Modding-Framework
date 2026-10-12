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
        public static List<ContainerInfo> All()
        {
            var list = new List<ContainerInfo>();
            try
            {
                foreach (Storage storage in UnityEngine.Object.FindObjectsByType<Storage>(FindObjectsSortMode.None))
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

        public static List<ItemStackInfo> GetItems(ContainerInfo container)
        {
            var list = new List<ItemStackInfo>();
            if (container == null || container.Source == null || container.Source.storageSlots == null) return list;
            try
            {
                foreach (global::ItemStack stack in container.Source.storageSlots)
                {
                    if (stack != null && stack.itemId != -1 && stack.itemAmount > 0) list.Add(ItemStackInfo.From(stack));
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

        private static ContainerInfo Describe(Storage storage)
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
            Item item = ItemDatabase.instance == null ? null : ItemDatabase.instance.FetchItemByID(itemId);
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
