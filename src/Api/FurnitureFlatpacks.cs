using System;
using OS.Items;

namespace ObenseuerCore
{
    internal static class FurnitureFlatpacks
    {
        internal const int FlatpackItemId = 19410;
        internal const int MaxAmount = 100;

        internal static bool Give(Furniture furniture, int amount)
        {
            global::Inventory inventory = global::Inventory.instance;
            Item item = ItemRegistry.FindInDatabase(FlatpackItemId);
            if (inventory == null || item == null || Item.GetMetaOfType<ItemFurnitureData>(item.CloneMeta()) == null) return false;
            if (!inventory.InventoryIsNotFull(item, -1, amount, 1, item.CloneMeta(), inventory.Slots)) return false;
            for (int i = 0; i < amount; i++)
            {
                ItemOperations.AddItem(item, -1, null, Fill(item.CloneMeta(), furniture), true);
            }
            return true;
        }

        internal static object[] Fill(object[] meta, Furniture furniture)
        {
            for (int i = 0; i < meta.Length; i++)
            {
                if (meta[i] is ItemFurnitureData)
                {
                    BuildingSystem.FurnitureInfo.Meta furnitureMeta = new BuildingSystem.FurnitureInfo.Meta();
                    ItemFurnitureData data = new ItemFurnitureData();
                    data.furnitureInfo = new BuildingSystem.FurnitureInfo(furniture, furnitureMeta, Furniture.CreateTaskItem(furniture, furnitureMeta), null);
                    data.ocValue = furniture.PriceOC + furniture.PriceRM * 10;
                    meta[i] = data;
                }
            }
            return meta;
        }
    }
}
