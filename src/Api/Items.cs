using System;
using System.Collections.Generic;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    public enum ItemQuality
    {
        Normal = 0,
        Crap = 1,
        Poor = 2,
        Bad = 3,
        Okay = 4,
        Good = 5,
        Fine = 6,
        Excellent = 7
    }

    public sealed class ItemDef
    {
        public int Id { get; private set; }
        public string InternalName { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public string Category { get; private set; }
        public string[] Categories { get; private set; }
        public int BaseValue { get; private set; }
        public int Price { get; private set; }
        public int BaseMaxStack { get; private set; }
        public int MaxStack { get; private set; }
        public bool Perishable { get; private set; }
        public float IntegrityLossPerHour { get; private set; }
        public int SpoilsIntoId { get; private set; }
        public bool LiquidContainer { get; private set; }
        public float LiquidCapacity { get; private set; }
        public int EmptiedIntoId { get; private set; }
        public int FilledIntoId { get; private set; }
        public bool Consumable { get; private set; }
        public string[] ActionNames { get; private set; }
        public string[] MetaTypes { get; private set; }
        public float Quality { get; private set; }
        public string VariantCategoryName { get; private set; }
        public string VariantName { get; private set; }
        public string EquipSlot { get; private set; }
        public string EquipType { get; private set; }
        public int[] AmmoItemIds { get; private set; }
        public string[] AmmoCategories { get; private set; }
        public int FuelBurnTime { get; private set; }
        public bool Electronic { get; private set; }
        public string ElectronicType { get; private set; }

        private static readonly string[] NoStrings = new string[0];
        private static readonly int[] NoInts = new int[0];

        internal ItemDef(Item item)
        {
            Id = item.ID;
            InternalName = Text(item.Title);
            DisplayName = InternalName;
            Description = Text(item.Description);
            Categories = item.Categories == null ? new string[0] : (string[])item.Categories.Clone();
            Category = Categories.Length > 0 ? Categories[0] : "";
            BaseValue = item.BaseValue;
            Price = item.Value;
            BaseMaxStack = item.BaseStackable;
            MaxStack = item.Stackable;

            ItemPerishabledata perishable = Item.GetMetaOfType<ItemPerishabledata>(item.Meta);
            Perishable = perishable != null;
            IntegrityLossPerHour = perishable == null ? 0f : perishable.integrityLossPerHour;
            SpoilsIntoId = perishable == null ? -1 : RefId(perishable.spoiledItem);

            ItemLiquidData liquid = Item.GetMetaOfType<ItemLiquidData>(item.Meta);
            ItemEmptyLiquidContainerData emptyContainer = Item.GetMetaOfType<ItemEmptyLiquidContainerData>(item.Meta);
            LiquidContainer = liquid != null || emptyContainer != null;
            LiquidCapacity = liquid == null ? 0f : liquid.liquidMaxAmount;
            EmptiedIntoId = liquid == null ? -1 : RefId(liquid.emptyContainerItem);
            FilledIntoId = emptyContainer == null ? -1 : RefId(emptyContainer.filledItem);

            int actionCount = item.Actions == null ? 0 : item.Actions.Length;
            ActionNames = new string[actionCount];
            bool consumable = false;
            for (int i = 0; i < actionCount; i++)
            {
                ActionNames[i] = Text(item.Actions[i].Name);
                if (item.Actions[i] is ItemConsumable) consumable = true;
            }
            Consumable = consumable;

            List<string> metaTypes = new List<string>();
            if (item.Meta != null)
            {
                foreach (object meta in item.Meta)
                {
                    if (meta != null) metaTypes.Add(meta.GetType().Name);
                }
            }
            MetaTypes = metaTypes.ToArray();

            ItemQualityData quality = Item.GetMetaOfType<ItemQualityData>(item.Meta);
            Quality = quality == null ? 1f : quality.quality;

            EquipSlot = "";
            foreach (string category in Categories)
            {
                if (IsEquipSlot(category))
                {
                    EquipSlot = category;
                    break;
                }
            }

            EquipType = "";
            Electronic = false;
            ElectronicType = "";
            for (int i = 0; i < actionCount; i++)
            {
                ItemEquipable equipable = item.Actions[i] as ItemEquipable;
                ItemElectronic electronic = item.Actions[i] as ItemElectronic;
                if (equipable != null) EquipType = equipable.type.ToString();
                if (electronic != null)
                {
                    Electronic = true;
                    ElectronicType = electronic.type.ToString();
                }
            }

            ItemAmmodata ammo = Item.GetMetaOfType<ItemAmmodata>(item.Meta);
            AmmoCategories = ammo == null || ammo.ammoItemcategories == null || ammo.ammoItemcategories.Length == 0 ? NoStrings : (string[])ammo.ammoItemcategories.Clone();
            AmmoItemIds = RefIds(ammo == null ? null : ammo.ammoItems);
            ItemFueldata fuel = Item.GetMetaOfType<ItemFueldata>(item.Meta);
            FuelBurnTime = fuel == null ? 0 : fuel.burntime;

            VariantCategoryName = "";
            VariantName = "";
            if (ItemVariantManager.instance != null)
            {
                try
                {
                    ItemReference reference = new ItemReference(item);
                    foreach (object value in Enum.GetValues(typeof(VariantCategory)))
                    {
                        VariantCategory category = (VariantCategory)value;
                        string name = ItemVariantManager.instance.GetVariantName(reference, category);
                        if (!string.IsNullOrEmpty(name))
                        {
                            VariantCategoryName = category.ToString();
                            VariantName = name;
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    VariantCategoryName = "";
                    VariantName = "";
                }
            }
        }

        private static string Text(string value)
        {
            return value == null ? "" : value;
        }

        private static int RefId(ItemReference reference)
        {
            return reference == null ? -1 : reference.ID;
        }

        private static int[] RefIds(List<ItemReference> references)
        {
            if (references == null || references.Count == 0) return NoInts;
            List<int> ids = new List<int>();
            foreach (ItemReference reference in references)
            {
                if (reference != null) ids.Add(reference.ID);
            }
            return ids.ToArray();
        }

        private static bool IsEquipSlot(string category)
        {
            switch (category)
            {
                case "Wieldable Primary":
                case "Wieldable Secondary":
                case "Backpack":
                case "Face":
                case "Head":
                case "Watch":
                    return true;
            }
            return false;
        }
    }

    /// <seealso cref="ItemRegistry"/>
    public static class Items
    {

        public static List<ItemDef> All()
        {
            return Collect(item => true);
        }

        public static ItemDef Get(int id)
        {
            return Build(FindItem(id));
        }

        public static ItemDef Find(string name)
        {
            if (name == null) throw new ArgumentException("name must not be null", "name");
            List<Item> database = Database();
            for (int i = 0; i < database.Count; i++)
            {
                if (database[i] != null && string.Equals(database[i].Title, name, StringComparison.OrdinalIgnoreCase)) return Build(database[i]);
            }
            return null;
        }

        public static List<ItemDef> Search(string text)
        {
            if (text == null) throw new ArgumentException("text must not be null", "text");
            string needle = text.ToLowerInvariant();
            if (needle.Length == 0) return new List<ItemDef>();
            return Collect(item => item.Title != null && item.Title.ToLowerInvariant().Contains(needle));
        }

        public static List<ItemDef> InCategory(string category)
        {
            if (category == null) throw new ArgumentException("category must not be null", "category");
            return Collect(item => item.Categories != null && item.ifInCategory(category));
        }

        public static Sprite Icon(int id)
        {
            Item item = FindItem(id);
            return item == null || item.Appearance == null ? null : item.Appearance.Sprite;
        }

        public static ItemQuality QualityOf(float quality)
        {
            if (float.IsNaN(quality)) return ItemQuality.Normal;
            return (ItemQuality)(int)ItemQualityData.GetQuality(quality);
        }

        private static List<Item> Database()
        {
            List<Item> database = ItemDatabase.database;
            return database == null ? new List<Item>() : database;
        }

        private static Item FindItem(int id)
        {
            List<Item> database = Database();
            for (int i = 0; i < database.Count; i++)
            {
                if (database[i] != null && database[i].ID == id) return database[i];
            }
            return null;
        }

        private static List<ItemDef> Collect(Predicate<Item> match)
        {
            List<ItemDef> result = new List<ItemDef>();
            List<Item> database = Database();
            for (int i = 0; i < database.Count; i++)
            {
                if (database[i] == null || !match(database[i])) continue;
                ItemDef def = Build(database[i]);
                if (def != null) result.Add(def);
            }
            return result;
        }

        private static ItemDef Build(Item item)
        {
            if (item == null) return null;
            try
            {
                return new ItemDef(item);
            }
            catch (Exception e)
            {
                ApiLog.Once("obenseuer.moddingframework.items", "Items.Build", e);
                return null;
            }
        }
    }
}
