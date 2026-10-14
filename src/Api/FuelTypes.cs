using System;
using System.Collections.Generic;
using OS.Items;

namespace ObenseuerCore
{
    public sealed class FuelTypeInfo
    {
        public int ItemId { get; private set; }

        public int BurnSeconds { get; private set; }

        public int BurntItemId { get; private set; }

        public float BurntItemAmount { get; private set; }

        internal FuelTypeInfo(int itemId, int burnSeconds, int burntItemId, float burntItemAmount)
        {
            ItemId = itemId;
            BurnSeconds = burnSeconds;
            BurntItemId = burntItemId;
            BurntItemAmount = burntItemAmount;
        }
    }

    public static class FuelTypes
    {
        public static List<FuelTypeInfo> All()
        {
            List<FuelTypeInfo> list = new List<FuelTypeInfo>();
            List<Item> database = ItemDatabase.database;
            if (database == null) return list;
            for (int i = 0; i < database.Count; i++)
            {
                FuelTypeInfo info = ToInfo(database[i]);
                if (info != null) list.Add(info);
            }
            return list;
        }

        public static FuelTypeInfo Get(int itemId)
        {
            foreach (FuelTypeInfo info in All())
            {
                if (info.ItemId == itemId) return info;
            }
            return null;
        }

        private static FuelTypeInfo ToInfo(Item item)
        {
            if (item == null) return null;
            try
            {
                ItemFueldata fuel = Item.GetMetaOfType<ItemFueldata>(item.Meta);
                if (fuel == null) return null;
                return new FuelTypeInfo(item.ID, fuel.burntime, fuel.burntItem == null ? -1 : fuel.burntItem.ID, fuel.burntItemAmount);
            }
            catch (Exception e)
            {
                ApiLog.Once(FuelStations.Owner, "FuelTypes.All", e);
                return null;
            }
        }
    }
}
