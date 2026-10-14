using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    public sealed class StackSpec
    {
        public float? Quality { get; set; }

        public int OwnerId { get; set; }

        public StackSpec()
        {
            OwnerId = -1;
        }

        public int LiquidItemId { get; set; }

        public float LiquidAmount { get; set; }
    }

    public static class StackActions
    {
        internal const string Owner = "obenseuer.moddingframework.stacks";
        private const float MaxQuality = 2f;

        public static bool Give(string modId, int itemId, int count, StackSpec spec)
        {
            return Place(modId, itemId, count, spec, false, "StackActions.Give");
        }

        public static bool GiveOrDrop(string modId, int itemId, int count, StackSpec spec)
        {
            return Place(modId, itemId, count, spec, true, "StackActions.GiveOrDrop");
        }

        public static bool Drop(string modId, int itemId, int count, StackSpec spec)
        {
            ModIds.Require(modId, "modId");
            if (PlayerLocator.instance == null) return false;
            try
            {
                Item item;
                object[] meta;
                if (!Prepare(itemId, count, spec, out item, out meta)) return false;
                ItemOperations.DropItem(item, count, spec.OwnerId, meta);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "StackActions.Drop", e);
                return false;
            }
        }

        public static float AddLiquid(string modId, int liquidItemId, float amount)
        {
            ModIds.Require(modId, "modId");
            if (!(amount > 0f) || float.IsInfinity(amount) || SaveGate.Busy) return 0f;
            try
            {
                Item liquid = ItemRegistry.FindInDatabase(liquidItemId);
                if (liquid == null) return 0f;
                float remaining;
                ItemOperations.AddLiquid(liquid, amount, out remaining);
                return amount - remaining;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "StackActions.AddLiquid", e);
                return 0f;
            }
        }

        public static List<ItemStackInfo> StolenStacks()
        {
            List<ItemStackInfo> list = new List<ItemStackInfo>();
            try
            {
                global::Inventory inventory = global::Inventory.instance;
                if (inventory == null) return list;
                foreach (global::ItemStack stack in inventory.GetStolenItems()) list.Add(ItemStackInfo.From(stack));
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "StackActions.StolenStacks", e);
                return new List<ItemStackInfo>();
            }
        }

        private static bool Place(string modId, int itemId, int count, StackSpec spec, bool dropRest, string what)
        {
            ModIds.Require(modId, "modId");
            try
            {
                Item item;
                object[] meta;
                global::Inventory inventory = global::Inventory.instance;
                if (inventory == null || !Prepare(itemId, count, spec, out item, out meta)) return false;
                if (!dropRest && !inventory.InventoryIsNotFull(item, spec.OwnerId, count, 1, meta, inventory.Slots)) return false;
                ItemOperations.AddItemsAndDropRemaining(item, spec.OwnerId, null, meta, count);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, what, e);
                return false;
            }
        }

        private static bool Prepare(int itemId, int count, StackSpec spec, out Item item, out object[] meta)
        {
            item = null;
            meta = null;
            if (count < 1 || spec == null || itemId == 0 || itemId == 1 || SaveGate.Busy) return false;
            item = ItemRegistry.FindInDatabase(itemId);
            if (item == null) return false;
            meta = MetaFor(item, count, spec);
            return meta != null;
        }

        private static object[] MetaFor(Item item, int count, StackSpec spec)
        {
            object[] meta = item.CloneMeta();
            if (spec.Quality.HasValue)
            {
                ItemQualityData quality = Item.GetMetaOfType<ItemQualityData>(meta);
                float value = spec.Quality.Value;
                if (quality == null || float.IsNaN(value) || value <= 0f || value > MaxQuality) return null;
                quality.quality = value;
            }
            if (spec.LiquidItemId == 0) return meta;
            Item liquid = ItemRegistry.FindInDatabase(spec.LiquidItemId);
            if (liquid == null || liquid.Categories == null || Array.IndexOf(liquid.Categories, "Liquid") < 0) return null;
            if (!(spec.LiquidAmount > 0f) || Item.GetMetaOfType<ItemLiquidData>(meta) == null) return null;
            float added;
            return item.CloneMetaAndSetLiquid(liquid, meta, spec.LiquidAmount, out added, count);
        }
    }
}
