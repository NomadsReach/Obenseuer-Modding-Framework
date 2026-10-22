using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using OS.Items;

namespace ObenseuerCore
{
    public sealed class InventorySourceContext
    {
        public string Kind { get; private set; }
        public string Id { get; private set; }

        internal InventorySourceContext(string kind, string id)
        {
            Kind = kind;
            Id = id;
        }
    }

    public static class InventorySources
    {
        internal const string Owner = "obenseuer.moddingframework.inventorysources";
        internal const string FurnitureBlueprint = "FurnitureBlueprint";
        internal const string Renovation = "Renovation";
        private static readonly List<Entry> providers = new List<Entry>();
        private static readonly FieldInfo resourceStorages = AccessTools.Field(typeof(TenementController), "tenementResourceStorages");
        private static Scope current;
        private static bool installed;

        public static bool Register(string modId, Func<InventorySourceContext, IEnumerable<Storage>> provider)
        {
            ModIds.Require(modId, "modId");
            if (provider == null) throw new ArgumentNullException("provider");
            if (!Ensure())
            {
                CoreLog.For(modId).Warn("InventorySources.Register ignored, the inventory hooks are not installed");
                return false;
            }
            Entry entry = new Entry(modId, provider);
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                providers[i] = entry;
                return true;
            }
            providers.Add(entry);
            return true;
        }

        public static bool Unregister(string modId)
        {
            ModIds.Require(modId, "modId");
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i].ModId != modId) continue;
                providers.RemoveAt(i);
                return true;
            }
            return false;
        }

        internal static bool IsOpen
        {
            get
            {
                return current != null;
            }
        }

        internal static bool Consuming
        {
            get
            {
                return current != null && current.Consume;
            }
        }

        internal static Scope Open(string kind, string id, bool consume)
        {
            if (providers.Count == 0 || current != null || SaveGate.Busy) return null;
            Scope opened = new Scope(kind, id, consume);
            current = opened;
            try
            {
                opened.Storages = Collect(opened);
                if (opened.Storages.Count == 0)
                {
                    current = null;
                    return null;
                }
                return opened;
            }
            catch (Exception)
            {
                current = null;
                throw;
            }
        }

        internal static void Close(Scope scope)
        {
            if (scope == null || scope.Closed) return;
            scope.Closed = true;
            if (ReferenceEquals(current, scope)) current = null;
        }

        internal static int Count(int itemId, bool checkAmmo, bool ignoreOwner, int owner, bool liquid)
        {
            return (int)CountAmount(itemId, checkAmmo, ignoreOwner, owner, liquid);
        }

        internal static float CountAmount(int itemId, bool checkAmmo, bool ignoreOwner, int owner, bool liquid)
        {
            Scope open = current;
            if (open == null) return 0f;
            float total = 0f;
            foreach (Storage storage in open.Storages)
            {
                SlotController[] slots = storage.Slots;
                if (slots == null) continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    SlotController slot = slots[i];
                    if (slot == null) continue;
                    total += ItemOperations.GetItemAmountFromSlot(slot, itemId, checkAmmo, ignoreOwner, owner, liquid);
                }
            }
            return total;
        }

        internal static int CountAmmo(int itemId)
        {
            Scope open = current;
            if (open == null) return 0;
            float total = 0f;
            foreach (Storage storage in open.Storages)
            {
                SlotController[] slots = storage.Slots;
                if (slots == null) continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    SlotController slot = slots[i];
                    if (slot == null || slot.itemStack.itemId != itemId || slot.itemStack.itemAmount <= 0) continue;
                    ItemAmmodata ammo = slot.itemStack.GetAmmodata();
                    if (ammo != null) total += ammo.ammo;
                }
            }
            return (int)total;
        }

        internal static bool TakeAmmo(int itemId, int ammoAmount, bool ignoreOwner, bool ignoreOwned)
        {
            Scope open = current;
            if (open == null || !open.Consume) return false;
            foreach (Storage storage in open.Storages)
            {
                SlotController[] slots = storage.Slots;
                if (slots == null) continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    SlotController slot = slots[i];
                    if (slot == null || slot.itemStack.itemId != itemId || slot.itemStack.itemAmount <= 0) continue;
                    if (slot.itemStack.ownerId > 0 && (!ignoreOwner || (!ignoreOwned && slot.itemStack.GetMetaOfType<ItemOwnerPermission>() != null))) continue;
                    ItemAmmodata ammo = slot.itemStack.GetAmmodata();
                    if (ammo == null) continue;
                    if (ammoAmount < -1 && ammo.ammo < (float)(-ammoAmount)) return false;
                    ammo.ChangeAmmoDirect(slot, ammoAmount);
                    storage.UpdateSlots();
                    return true;
                }
            }
            return false;
        }

        internal static float Take(int itemId, float amount, bool ignoreOwner, int owner)
        {
            Scope open = current;
            if (open == null || !open.Consume || amount <= 0f) return 0f;
            int need = (int)Math.Ceiling(amount);
            if (need < 1) return 0f;
            int wanted = need;
            foreach (Storage storage in open.Storages)
            {
                if (wanted <= 0) break;
                int left;
                storage.RemoveItem(itemId, out left, wanted, false, ignoreOwner, owner);
                if (left >= wanted) continue;
                wanted = left;
                storage.UpdateSlots();
            }
            int taken = need - wanted;
            if (taken >= need || !IsLiquidItem(itemId)) return taken;
            return taken + TakeLiquid(open, itemId, need - taken, ignoreOwner, owner);
        }

        private static bool IsLiquidItem(int itemId)
        {
            ItemDatabase database = ItemDatabase.instance;
            if (database == null) return false;
            Item item = database.FetchItemByID(itemId);
            return item != null && item.Categories != null && Array.IndexOf(item.Categories, "Liquid") >= 0;
        }

        private static int TakeLiquid(Scope scope, int itemId, int units, bool ignoreOwner, int owner)
        {
            int wanted = units;
            foreach (Storage storage in scope.Storages)
            {
                if (wanted <= 0) break;
                SlotController[] slots = storage.Slots;
                if (slots == null) continue;
                bool changed = false;
                for (int i = 0; i < slots.Length && wanted > 0; i++)
                {
                    SlotController slot = slots[i];
                    if (slot == null) continue;
                    if (!ignoreOwner && slot.itemStack.ownerId != owner) continue;
                    ItemLiquidData liquid = slot.itemStack.GetMetaOfType<ItemLiquidData>();
                    if (liquid == null || liquid.liquidItem == null || liquid.liquidItem.ID != itemId) continue;
                    slot.TakeItem(1);
                    wanted--;
                    changed = true;
                }
                if (changed) storage.UpdateSlots();
            }
            return units - wanted;
        }

        private static bool Ensure()
        {
            if (installed) return true;
            if (CorePatcher.IsDisabled(Owner)) return false;
            installed = InventorySourcesPatches.Install();
            if (installed) return true;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("inventory source hooks incomplete, extra sources are disabled");
            return false;
        }

        private static List<Storage> Collect(Scope scope)
        {
            InventorySourceContext context = new InventorySourceContext(scope.Kind, scope.Id);
            Storage backpack = Backpack();
            List<Storage> tenement = scope.Kind == Renovation ? ResourceStorages() : null;
            List<Storage> found = new List<Storage>();
            HashSet<Storage> seen = new HashSet<Storage>();
            for (int i = 0; i < providers.Count; i++)
            {
                Entry entry = providers[i];
                List<Storage> given = Ask(entry, context);
                if (given == null) continue;
                foreach (Storage storage in given)
                {
                    if (storage == null || ReferenceEquals(storage, backpack) || storage.storageSlots == null || storage.storageSlots.Length == 0 || !seen.Add(storage)) continue;
                    if (tenement != null && tenement.Contains(storage)) continue;
                    found.Add(storage);
                }
            }
            return found;
        }

        private static Storage Backpack()
        {
            BackpackStorage backpack = BackpackStorage.instance;
            return backpack == null ? null : backpack.GetStorage();
        }

        private static List<Storage> ResourceStorages()
        {
            TenementController tenement = TenementController.instance;
            if (tenement == null || resourceStorages == null) return null;
            return resourceStorages.GetValue(tenement) as List<Storage>;
        }

        private static List<Storage> Ask(Entry entry, InventorySourceContext context)
        {
            try
            {
                IEnumerable<Storage> given = entry.Provider(context);
                if (given == null)
                {
                    ApiLog.Once(entry.ModId, "InventorySources provider", "returned null, its containers are ignored");
                    return null;
                }
                return new List<Storage>(given);
            }
            catch (Exception e)
            {
                ApiLog.Once(entry.ModId, "InventorySources provider", e);
                return null;
            }
        }

        internal sealed class Entry
        {
            internal readonly string ModId;
            internal readonly Func<InventorySourceContext, IEnumerable<Storage>> Provider;

            internal Entry(string modId, Func<InventorySourceContext, IEnumerable<Storage>> provider)
            {
                ModId = modId;
                Provider = provider;
            }
        }

        internal sealed class Scope
        {
            internal readonly string Kind;
            internal readonly string Id;
            internal readonly bool Consume;
            internal List<Storage> Storages;
            internal bool Closed;

            internal Scope(string kind, string id, bool consume)
            {
                Kind = kind;
                Id = id;
                Consume = consume;
            }
        }
    }
}
