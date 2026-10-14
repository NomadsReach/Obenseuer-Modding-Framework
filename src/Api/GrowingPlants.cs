using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class PlantInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public int Age { get; private set; }

        public int FullGrownAge { get; private set; }

        public float Progress { get; private set; }

        public bool FullyGrown { get; private set; }

        public bool Paused { get; private set; }

        public bool Rotting { get; private set; }

        public int ReadyFruit { get; private set; }

        internal PlantInfo(string id, Vector3 position, int age, int fullGrownAge, float progress, bool paused, bool rotting, int readyFruit)
        {
            Id = id;
            Position = position;
            Age = age;
            FullGrownAge = fullGrownAge;
            Progress = progress;
            FullyGrown = age >= fullGrownAge;
            Paused = paused;
            Rotting = rotting;
            ReadyFruit = readyFruit;
        }
    }

    public sealed class PlantFruitInfo
    {
        public int ItemId { get; private set; }

        public int Amount { get; private set; }

        public bool Visible { get; private set; }

        public bool Ready { get; private set; }

        public bool Spoiled { get; private set; }

        public float GrowProgress { get; private set; }

        public float SpoilProgress { get; private set; }

        internal PlantFruitInfo(int itemId, int amount, bool visible, bool ready, bool spoiled, float growProgress, float spoilProgress)
        {
            ItemId = itemId;
            Amount = amount;
            Visible = visible;
            Ready = ready;
            Spoiled = spoiled;
            GrowProgress = growProgress;
            SpoilProgress = spoilProgress;
        }
    }

    public sealed class PlantHarvestArgs
    {
        public string PlantId { get; private set; }

        public List<ItemCount> Items { get; private set; }

        internal PlantHarvestArgs(string plantId, List<ItemCount> items)
        {
            PlantId = plantId;
            Items = items;
        }
    }

    public sealed class PlantPickArgs
    {
        public string PlantId { get; private set; }

        public int ItemId { get; private set; }

        public int Amount { get; private set; }

        internal PlantPickArgs(string plantId, int itemId, int amount)
        {
            PlantId = plantId;
            ItemId = itemId;
            Amount = amount;
        }
    }

    public static class Plants
    {
        internal const string Owner = "obenseuer.moddingframework.growing";
        private static bool installed;
        private static FieldInfo fruitItemField;
        private static FieldInfo fruitItemAmountField;
        private static FieldInfo fruitFullGrownAgeField;

        public static event Action<PlantHarvestArgs> Harvested;

        public static event Action<PlantPickArgs> Picked;

        public static List<PlantInfo> All()
        {
            List<PlantInfo> list = new List<PlantInfo>();
            try
            {
                foreach (grow plant in SceneIds.Active<grow>())
                {
                    if (plant != null) list.Add(new PlantInfo(plant.GUID, plant.transform.position, plant.Age, plant.FullGrownAge, plant.GetCurrentStageProcess(), plant.Paused, plant.Rotting, ReadyFruitCount(plant)));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Plants.Owner, "All", e);
                return new List<PlantInfo>();
            }
        }

        public static List<PlantFruitInfo> Fruits(string plantId)
        {
            List<PlantFruitInfo> list = new List<PlantFruitInfo>();
            if (string.IsNullOrEmpty(plantId)) return list;
            try
            {
                if (!ResolveFruitFields()) return list;
                foreach (grow plant in SceneIds.Active<grow>())
                {
                    if (plant == null || plant.GUID != plantId) continue;
                    foreach (GrowItem fruit in plant.visibleGrownItems ?? new GrowItem[0])
                    {
                        try
                        {
                            if (fruit != null) list.Add(FruitInfo(fruit));
                        }
                        catch (Exception e)
                        {
                            ApiLog.Once(Plants.Owner, "Fruits item", e);
                        }
                    }
                    return list;
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Plants.Owner, "Fruits", e);
                return new List<PlantFruitInfo>();
            }
        }

        private static int ReadyFruitCount(grow plant)
        {
            int count = 0;
            foreach (GrowItem fruit in plant.visibleGrownItems ?? new GrowItem[0])
            {
                if (fruit != null && fruit.gameObject.activeSelf && fruit.readyToPick) count++;
            }
            return count;
        }

        private static bool ResolveFruitFields()
        {
            if (fruitItemField == null) fruitItemField = AccessTools.Field(typeof(GrowItem), "item");
            if (fruitItemAmountField == null) fruitItemAmountField = AccessTools.Field(typeof(GrowItem), "itemAmount");
            if (fruitFullGrownAgeField == null) fruitFullGrownAgeField = AccessTools.Field(typeof(GrowItem), "fullGrownAgeFinal");
            return fruitItemField != null && fruitItemAmountField != null && fruitFullGrownAgeField != null;
        }

        private static PlantFruitInfo FruitInfo(GrowItem fruit)
        {
            grow.ItemAmount fruitItem = (grow.ItemAmount)fruitItemField.GetValue(fruit);
            int fullGrownAge = (int)fruitFullGrownAgeField.GetValue(fruit);
            float spoilProgress = fruit.GetCurrentSpoilTime();
            if (float.IsNaN(spoilProgress)) spoilProgress = 0f;
            float growProgress = fullGrownAge > 0 ? Mathf.Clamp01((float)fruit.age / fullGrownAge) : 0f;
            return new PlantFruitInfo(fruitItem != null && fruitItem.item != null ? fruitItem.item.ID : 0, (int)fruitItemAmountField.GetValue(fruit), fruit.gameObject.activeSelf, fruit.readyToPick, fruit.Spoiled(), growProgress, spoilProgress);
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
            installed = GrowingPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("growing hooks incomplete, harvest events are disabled");
        }

        internal static bool HasHarvestHandlers
        {
            get { return Harvested != null; }
        }

        internal static bool HasPickHandlers
        {
            get { return Picked != null; }
        }

        internal static void RaiseHarvested(PlantHarvestArgs info)
        {
            Events.Raise(Harvested, "Plants.Harvested", h => h(info));
        }

        internal static void RaisePicked(PlantPickArgs info)
        {
            Events.Raise(Picked, "Plants.Picked", h => h(info));
        }
    }
}
