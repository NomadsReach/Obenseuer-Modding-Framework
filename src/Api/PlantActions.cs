using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public static class PlantActions
    {
        internal const string Owner = "obenseuer.moddingframework.plantactions";

        public static bool Harvest(string modId, string plantId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                grow plant = SceneIds.Find<grow>(plantId);
                if (plant == null) return false;
                bool picked = HasReadyItem(plant);
                bool destroy;
                List<global::ItemStack> stacks = plant.HarvestExternal(out destroy);
                if (destroy && plant.growingSpot != null) plant.growingSpot.HarvestPlant();
                foreach (global::ItemStack stack in stacks)
                {
                    ItemOperations.AddItemsAndDropRemaining(stack.itemReference.Item, stack.ownerId, null, stack.Meta, stack.itemAmount);
                }
                return picked || stacks.Count > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.Harvest", e);
                return false;
            }
        }

        public static bool Reset(string modId, string plantId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                grow plant = SceneIds.Find<grow>(plantId);
                if (plant == null) return false;
                plant.ResetGrow();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.Reset", e);
                return false;
            }
        }

        public static bool SetAgePercent(string modId, string plantId, int percent)
        {
            ModIds.Require(modId, "modId");
            if (percent < 0 || percent > 100)
            {
                ApiLog.Once(Owner, "percent", "percent must be from 0 to 100");
                return false;
            }
            if (SaveGate.Busy) return false;
            try
            {
                grow plant = SceneIds.Find<grow>(plantId);
                if (plant == null) return false;
                plant.SetAgeFromPercentage(percent);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.SetAgePercent", e);
                return false;
            }
        }

        public static bool SetFullGrownAge(string modId, string plantId, int age)
        {
            ModIds.Require(modId, "modId");
            if (age < 1)
            {
                ApiLog.Once(Owner, "age", "full grown age must be at least 1");
                return false;
            }
            if (SaveGate.Busy) return false;
            try
            {
                grow plant = SceneIds.Find<grow>(plantId);
                if (plant == null)
                {
                    ApiLog.Once(Owner, "plantId", "plant id matches no plant");
                    return false;
                }
                plant.FullGrownAge = age;
                plant.Age = Math.Min(plant.Age, age);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.SetFullGrownAge", e);
                return false;
            }
        }

        public static bool Pause(string modId, string plantId, bool paused)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                grow plant = SceneIds.Find<grow>(plantId);
                if (plant == null) return false;
                plant.Paused = paused;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.Pause", e);
                return false;
            }
        }

        public static bool SetRotting(string modId, string plantId, bool rotting)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                grow plant = SceneIds.Find<grow>(plantId);
                if (plant == null) return false;
                plant.Rotting = rotting;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.SetRotting", e);
                return false;
            }
        }

        public static bool Plant(string modId, string spotId, int itemId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                GrowingSpot spot = SceneIds.Find<GrowingSpot>(spotId);
                Item item = ItemRegistry.FindInDatabase(itemId);
                if (spot == null || spot.HasPlant() || item == null || item.Prefab == null || item.Prefab.GetComponent<grow>() == null) return false;
                spot.AddPlant(item);
                return spot.HasPlant();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.Plant", e);
                return false;
            }
        }

        public static bool HarvestSpot(string modId, string spotId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                GrowingSpot spot = SceneIds.Find<GrowingSpot>(spotId);
                if (spot == null || !spot.HasPlant() || spot.Plant.GetComponent<grow>() != null) return false;
                CollectibleItem collectible = spot.Plant.GetComponentInChildren<CollectibleItem>();
                if (collectible == null || !collectible.PerformPickup()) return false;
                spot.HarvestPlant();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "PlantActions.HarvestSpot", e);
                return false;
            }
        }

        private static bool HasReadyItem(grow plant)
        {
            foreach (GrowItem item in plant.visibleGrownItems)
            {
                if (item != null && item.gameObject.activeInHierarchy && item.readyToPick) return true;
            }
            return false;
        }
    }
}
