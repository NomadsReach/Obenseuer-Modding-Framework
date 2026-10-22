using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using OS.Items;

namespace ObenseuerCore
{
    internal static class CraftingSourcesSelfTest
    {
        internal static string Run()
        {
            try
            {
                string combined = CheckCombineSlots();
                if (combined != null) return combined;
                string order = CheckOrder();
                if (order != null) return order;
                string keep = CheckKeep();
                if (keep != null) return keep;
                string gone = CheckGone();
                if (gone != null) return gone;
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "crafting sources self-check ok";
        }

        private static string CheckCombineSlots()
        {
            StorageSlot a1 = Slot(10, 2);
            StorageSlot a2 = Slot(20, 1);
            StorageSlot b1 = Slot(30, 5);
            StorageSlot b2 = Slot(40, 3);
            StorageSlot c1 = Slot(50, 1);
            StorageSlot c2 = Slot(60, 4);
            StorageSlot[] own = { a1, a2 };
            StorageSlot[] extra = { b1, b2 };
            StorageSlot[] refused = { c1, c2 };
            CraftingSources.Entry plain = new CraftingSources.Entry("selfcheck.a", null, 0, null, 0);
            CraftingSources.Entry filtering = new CraftingSources.Entry("selfcheck.b", null, 0, RefuseSecond, 1);
            List<CraftingSources.Source> sources = new List<CraftingSources.Source>();
            sources.Add(new CraftingSources.Source(plain, Storage(extra)));
            sources.Add(new CraftingSources.Source(plain, Storage(extra)));
            sources.Add(new CraftingSources.Source(plain, Storage(own)));
            sources.Add(new CraftingSources.Source(plain, Storage(null)));
            sources.Add(new CraftingSources.Source(plain, Storage(new StorageSlot[0])));
            StorageSlot[] combined = CraftingSources.CombineSlots(own, sources);
            if (combined.Length != 4) return "a repeated or empty container changed the slot count to " + combined.Length;
            if (!ReferenceEquals(combined[0], a1) || !ReferenceEquals(combined[1], a2)) return "the station's own slots do not come first";
            if (!ReferenceEquals(combined[2], b1) || !ReferenceEquals(combined[3], b2)) return "an extra container was not appended in one piece";
            sources.Clear();
            sources.Add(new CraftingSources.Source(filtering, Storage(refused)));
            StorageSlot[] filtered = CraftingSources.CombineSlots(own, sources);
            if (filtered.Length != 4) return "a refused slot changed the slot count to " + filtered.Length;
            if (!ReferenceEquals(filtered[2], c1)) return "filtering dropped a kept slot";
            if (ReferenceEquals(filtered[3], c2)) return "filtering kept a refused slot";
            if (filtered[3].itemId != -1 || filtered[3].itemAmount != 0) return "a refused slot is not blank";
            return null;
        }

        private static bool RefuseSecond(Storage storage, int index, StorageSlot slot)
        {
            return index != 1;
        }

        private static string CheckOrder()
        {
            List<CraftingSources.Entry> list = new List<CraftingSources.Entry>();
            list.Add(new CraftingSources.Entry("low", null, 0, null, 0));
            list.Add(new CraftingSources.Entry("high", null, 5, null, 1));
            list.Add(new CraftingSources.Entry("high2", null, 5, null, 2));
            list.Add(new CraftingSources.Entry("under", null, -1, null, 3));
            CraftingSources.Sort(list);
            if (list[0].ModId != "high" || list[1].ModId != "high2" || list[2].ModId != "low" || list[3].ModId != "under") return "priority order is " + Names(list);
            return null;
        }

        private static string CheckKeep()
        {
            StorageSlot a = Slot(10, 2);
            StorageSlot b = Slot(20, 1);
            StorageSlot c = Slot(30, 5);
            StorageSlot[] slots = { a, b, c };
            StorageSlot[] kept = CraftingSources.Keep(slots, RefuseMiddle);
            if (kept.Length != 3) return "filtering changed the slot count to " + kept.Length;
            if (!ReferenceEquals(kept[0], a) || !ReferenceEquals(kept[2], c)) return "filtering moved or dropped a kept slot";
            if (ReferenceEquals(kept[1], b)) return "filtering kept a refused slot";
            if (kept[1] == null || kept[1].itemId != -1 || kept[1].itemAmount != 0) return "a refused slot is not blank";
            StorageSlot[] all = CraftingSources.Keep(slots, null);
            if (all.Length != 3 || !ReferenceEquals(all[1], b)) return "a missing filter blanked a slot";
            return null;
        }

        private static string CheckGone()
        {
            StorageSlot emptied = Slot(10, 3);
            StorageSlot shrunk = Slot(20, 2);
            StorageSlot untouched = Slot(30, 1);
            List<CraftingSources.Snapshot> before = new List<CraftingSources.Snapshot>();
            before.Add(new CraftingSources.Snapshot("mod.a", null, emptied, 0, emptied.itemId, emptied.itemAmount, -1, 0f));
            before.Add(new CraftingSources.Snapshot("mod.a", null, shrunk, 1, shrunk.itemId, shrunk.itemAmount, -1, 0f));
            before.Add(new CraftingSources.Snapshot("mod.b", null, untouched, 5, untouched.itemId, untouched.itemAmount, -1, 0f));
            emptied.itemId = -1;
            emptied.itemAmount = 0;
            shrunk.RemoveItemAmount(1);
            List<CraftingSources.CraftingTaken> taken = CraftingSources.Gone(null, before);
            if (taken.Count != 2) return "the taken list has " + taken.Count + " entries, expected 2";
            if (taken[0].ModId != "mod.a" || taken[0].SlotIndex != 0 || taken[0].ItemId != 10 || taken[0].Amount != 3) return "the emptied slot is reported wrong";
            if (taken[1].ModId != "mod.a" || taken[1].SlotIndex != 1 || taken[1].ItemId != 20 || taken[1].Amount != 1) return "the shrunk slot is reported wrong";
            if (taken[0].LiquidItemId != -1 || taken[0].LiquidAmount != 0f) return "a slot without liquid reports a pour";
            if (CraftingSources.Gone(null, null).Count != 0) return "a missing snapshot list reports items";
            return CheckGoneLiquid();
        }

        private static string CheckGoneLiquid()
        {
            ItemLiquidData pouredMeta = LiquidMeta(42, 1f);
            ItemLiquidData drainedMeta = LiquidMeta(43, 0.5f);
            ItemLiquidData steadyMeta = LiquidMeta(44, 0.5f);
            StorageSlot poured = LiquidSlot(100, 1, pouredMeta);
            StorageSlot drained = LiquidSlot(100, 1, drainedMeta);
            StorageSlot steady = LiquidSlot(100, 1, steadyMeta);
            List<CraftingSources.Snapshot> before = new List<CraftingSources.Snapshot>();
            before.Add(new CraftingSources.Snapshot("mod.c", null, poured, 0, poured.itemId, poured.itemAmount, 42, 1f));
            before.Add(new CraftingSources.Snapshot("mod.c", null, drained, 1, drained.itemId, drained.itemAmount, 43, 0.5f));
            before.Add(new CraftingSources.Snapshot("mod.c", null, steady, 2, steady.itemId, steady.itemAmount, 44, 0.5f));
            pouredMeta.liquidAmount = 0.5f;
            drainedMeta.liquidAmount = 0f;
            drainedMeta.liquidItem = null;
            steadyMeta.liquidAmount = 0.50005f;
            List<CraftingSources.CraftingTaken> taken = CraftingSources.Gone(null, before);
            if (taken.Count != 2) return "a pour reported " + taken.Count + " entries, expected 2";
            if (taken[0].ItemId != 100 || taken[0].Amount != 0) return "a partial pour reported the container as taken";
            if (taken[0].LiquidItemId != 42 || taken[0].LiquidAmount != 0.5f) return "the partial pour is reported wrong";
            if (taken[1].SlotIndex != 1 || taken[1].Amount != 0 || taken[1].LiquidItemId != 43 || taken[1].LiquidAmount != 0.5f) return "the emptied container is reported wrong";
            return null;
        }

        private static ItemLiquidData LiquidMeta(int itemId, float amount)
        {
            ItemLiquidData liquid = new ItemLiquidData();
            liquid.liquidItem = LiquidReference(itemId);
            liquid.liquidAmount = amount;
            return liquid;
        }

        private static ItemReference LiquidReference(int itemId)
        {
            ItemReference reference = (ItemReference)FormatterServices.GetUninitializedObject(typeof(ItemReference));
            reference.ID = itemId;
            return reference;
        }

        private static StorageSlot LiquidSlot(int itemId, int amount, ItemLiquidData liquid)
        {
            StorageSlot slot = Slot(itemId, amount);
            slot.itemReference = LiquidReference(itemId);
            slot.Meta = new object[] { liquid };
            return slot;
        }

        private static bool RefuseMiddle(int index, StorageSlot slot)
        {
            return index != 1;
        }

        private static Storage Storage(StorageSlot[] slots)
        {
            Storage storage = (Storage)FormatterServices.GetUninitializedObject(typeof(Storage));
            storage.storageSlots = slots;
            return storage;
        }

        private static StorageSlot Slot(int itemId, int amount)
        {
            StorageSlot slot = new StorageSlot();
            slot.itemId = itemId;
            slot.itemAmount = amount;
            return slot;
        }

        private static string Names(List<CraftingSources.Entry> list)
        {
            List<string> names = new List<string>();
            foreach (CraftingSources.Entry entry in list) names.Add(entry.ModId);
            return Join(names.ToArray());
        }

        private static string Join(string[] items)
        {
            return string.Join(",", items);
        }
    }
}
