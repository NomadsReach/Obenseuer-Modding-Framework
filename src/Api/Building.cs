using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class FurnitureItem
    {
        public string Guid { get; private set; }

        public string Id { get; private set; }

        public string Title { get; private set; }

        public string AreaId { get; private set; }

        public Vector3 Position { get; private set; }

        internal FurnitureItem(string guid, string id, string title, string areaId, Vector3 position)
        {
            Guid = guid;
            Id = id;
            Title = title;
            AreaId = areaId;
            Position = position;
        }
    }

    public sealed class BlueprintInfo
    {
        public string Name { get; private set; }

        public string Title { get; private set; }

        public bool Known { get; private set; }

        internal BlueprintInfo(string name, string title, bool known)
        {
            Name = name;
            Title = title;
            Known = known;
        }
    }

    public static class Building
    {
        public static event Action<FurnitureItem> FurniturePlaced;

        public static event Action<FurnitureItem> FurnitureRemoved;

        public static bool IsAvailable
        {
            get
            {
                return Housing.IsAvailable;
            }
        }

        public static List<string> AllAreaIds()
        {
            try
            {
                List<string> ids = new List<string>();
                if (BuildingSystem.instance == null) return ids;
                foreach (FurnitureManager manager in BuildingSystem.instance.furnitureManagers)
                {
                    if (manager != null && !string.IsNullOrEmpty(manager.GUID)) ids.Add(manager.GUID);
                }
                return ids;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "AllAreaIds", e);
                return new List<string>();
            }
        }

        public static List<FurnitureItem> GetPlacedFurniture(string areaId)
        {
            try
            {
                List<FurnitureItem> items = new List<FurnitureItem>();
                FurnitureManager manager = FindArea(areaId);
                if (manager == null) return items;
                foreach (BuildingSystem.FurnitureInfo info in manager.placedFurniture)
                {
                    if (info != null && info.gameObject != null) items.Add(Describe(areaId, info.gameObject));
                }
                return items;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "GetPlacedFurniture", e);
                return new List<FurnitureItem>();
            }
        }

        public static List<BlueprintInfo> AllBlueprints()
        {
            try
            {
                List<BlueprintInfo> list = new List<BlueprintInfo>();
                if (FurnitureBlueprintController.instance == null) return list;
                foreach (TaskItemBlueprint blueprint in FurnitureBlueprintController.instance.GetAllBluePrints())
                {
                    if (blueprint != null) list.Add(new BlueprintInfo(blueprint.name, blueprint.itemName, IsKnown(blueprint)));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "AllBlueprints", e);
                return new List<BlueprintInfo>();
            }
        }

        public static bool IsBlueprintKnown(string name)
        {
            try
            {
                TaskItemBlueprint blueprint = FindBlueprint(name);
                return blueprint != null && IsKnown(blueprint);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "IsBlueprintKnown", e);
                return false;
            }
        }

        public static bool UnlockBlueprint(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name must be a non-empty blueprint name", "name");
            try
            {
                TaskItemBlueprint blueprint = FindBlueprint(name);
                if (blueprint == null || IsKnown(blueprint)) return false;
                return blueprint.GiveItemWithResults();
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "UnlockBlueprint", e);
                return false;
            }
        }

        internal static FurnitureItem Describe(string areaId, GameObject go)
        {
            FurniturePlaceable placeable = go.GetComponent<FurniturePlaceable>();
            Furniture furniture = placeable == null ? null : placeable.furniture;
            return new FurnitureItem(placeable == null ? null : placeable.GUID, furniture == null ? null : furniture.id, furniture == null ? go.name : furniture.title, areaId, go.transform.position);
        }

        internal static void RaiseFurniturePlaced(FurnitureItem item)
        {
            Events.Raise(FurniturePlaced, "Building.FurniturePlaced", h => h(item));
        }

        internal static void RaiseFurnitureRemoved(FurnitureItem item)
        {
            Events.Raise(FurnitureRemoved, "Building.FurnitureRemoved", h => h(item));
        }

        private static FurnitureManager FindArea(string areaId)
        {
            if (BuildingSystem.instance == null || string.IsNullOrEmpty(areaId)) return null;
            foreach (FurnitureManager manager in BuildingSystem.instance.furnitureManagers)
            {
                if (manager != null && manager.GUID == areaId) return manager;
            }
            return null;
        }

        private static TaskItemBlueprint FindBlueprint(string name)
        {
            if (string.IsNullOrEmpty(name) || FurnitureBlueprintController.instance == null) return null;
            foreach (TaskItemBlueprint blueprint in FurnitureBlueprintController.instance.GetAllBluePrints())
            {
                if (blueprint != null && blueprint.name == name) return blueprint;
            }
            return null;
        }

        private static bool IsKnown(TaskItemBlueprint blueprint)
        {
            return FurnitureBlueprintController.instance.BlueprintIsCollected(blueprint);
        }
    }
}
