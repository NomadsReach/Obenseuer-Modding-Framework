using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ObenseuerCore
{
    public sealed class FurnitureItem
    {
        public string Guid { get; private set; }

        public string Id { get; private set; }

        public string Title { get; private set; }

        public string AreaId { get; private set; }

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; }

        public Quaternion ChildRotation { get; private set; }

        public int Skin { get; private set; }

        public int AltPrefab { get; private set; }

        public string ManagerGuid { get; private set; }

        public string ParentGuid { get; private set; }

        public string AttachedGuid { get; private set; }

        public GameObject ParentObject { get; private set; }

        public GameObject AttachedObject { get; private set; }

        public bool FromApi { get; private set; }

        public GameObject Object { get; private set; }

        internal FurnitureItem(string guid, string id, string title, string areaId, Vector3 position, Quaternion rotation, Quaternion childRotation, int skin, int altPrefab, string managerGuid, string parentGuid, string attachedGuid, GameObject parentObject, GameObject attachedObject, bool fromApi, GameObject placed)
        {
            FromApi = fromApi;
            Object = placed;
            Guid = guid;
            Id = id;
            Title = title;
            AreaId = areaId;
            Position = position;
            Rotation = rotation;
            ChildRotation = childRotation;
            Skin = skin;
            AltPrefab = altPrefab;
            ManagerGuid = managerGuid;
            ParentGuid = parentGuid;
            AttachedGuid = attachedGuid;
            ParentObject = parentObject;
            AttachedObject = attachedObject;
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

    public sealed class FurnitureManagerInfo
    {
        public string Guid { get; private set; }

        public string SceneName { get; private set; }

        internal FurnitureManagerInfo(string guid, string sceneName)
        {
            Guid = guid;
            SceneName = sceneName;
        }
    }

    public static partial class Building
    {
        public static event Action<FurnitureItem> FurniturePlaced;

        public static event Action<FurnitureItem> FurnitureRemoved;

        public static event Action<FurnitureItem> FurnitureLoaded;

        private static int applyDepth;

        public static bool Applying { get { return applyDepth > 0; } }

        internal static bool Placing;

        internal static FurniturePlaceable Placed;

        internal static bool ApiCall;

        private static readonly FieldInfo trashField = AccessTools.Field(typeof(FurniturePlaceable), "trash");

        public static bool IsAvailable
        {
            get
            {
                return Housing.FurnitureAvailable;
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

        internal static List<FurnitureItem> AllPlacedFurniture()
        {
            List<FurnitureItem> items = new List<FurnitureItem>();
            try
            {
                if (BuildingSystem.instance == null) return items;
                foreach (FurnitureManager manager in BuildingSystem.instance.furnitureManagers)
                {
                    if (manager == null || string.IsNullOrEmpty(manager.GUID)) continue;
                    foreach (BuildingSystem.FurnitureInfo info in manager.placedFurniture)
                    {
                        if (info != null && info.gameObject != null) items.Add(Describe(manager.GUID, info.gameObject));
                    }
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "AllPlacedFurniture", e);
            }
            return items;
        }

        public static FurnitureItem Find(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            try
            {
                FurnitureManager manager;
                FurniturePlaceable placeable = FindPlaced(p => p.GUID == guid, out manager);
                return placeable == null ? null : Describe(manager.GUID, placeable.gameObject);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Find", e);
                return null;
            }
        }

        public static List<FurnitureManagerInfo> Managers(string sceneName)
        {
            try
            {
                List<FurnitureManagerInfo> managers = new List<FurnitureManagerInfo>();
                if (BuildingSystem.instance == null) return managers;
                foreach (FurnitureManager manager in BuildingSystem.instance.furnitureManagers)
                {
                    if (manager == null) continue;
                    string scene = manager.gameObject.scene.name;
                    if (string.IsNullOrEmpty(sceneName) || scene == sceneName) managers.Add(new FurnitureManagerInfo(manager.GUID, scene));
                }
                return managers;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Managers", e);
                return new List<FurnitureManagerInfo>();
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
            return UnlockBlueprint(name, false);
        }

        public static bool UnlockBlueprint(string name, bool silent)
        {
            if (string.IsNullOrEmpty(name))
            {
                ApiLog.Once(Housing.Owner, "UnlockBlueprint empty name", "name must be a non-empty blueprint name");
                return false;
            }
            try
            {
                TaskItemBlueprint blueprint = FindBlueprint(name);
                if (SaveGate.Busy || blueprint == null || IsKnown(blueprint)) return false;
                return silent ? TaskItemsManager.instance.AddTaskItem(blueprint) : blueprint.GiveItemWithResults();
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "UnlockBlueprint", e);
                return false;
            }
        }

        public static FurnitureItem Place(string managerGuid, string furnitureId, Vector3 position, Quaternion rotation, Quaternion childRotation, int skin, int altPrefab)
        {
            return Place(managerGuid, furnitureId, position, rotation, childRotation, skin, altPrefab, false);
        }

        public static FurnitureItem Place(string managerGuid, string furnitureId, Vector3 position, Quaternion rotation, Quaternion childRotation, int skin, int altPrefab, bool requireKnown)
        {
            if (string.IsNullOrEmpty(managerGuid))
            {
                ApiLog.Once(Housing.Owner, "Place empty area id", "managerGuid must be a non-empty area id");
                return null;
            }
            if (string.IsNullOrEmpty(furnitureId))
            {
                ApiLog.Once(Housing.Owner, "Place empty furniture id", "furnitureId must be a non-empty furniture id");
                return null;
            }
            if (requireKnown && !IsKnownFurniture(furnitureId))
            {
                ApiLog.Once(Housing.Owner, "Place unknown furniture id", "furnitureId is not a known furniture, so nothing was placed");
                return null;
            }
            FurnitureManager manager = null;
            GameObject go = null;
            try
            {
                manager = FindArea(managerGuid);
                Furniture furniture = manager == null ? null : FindFurniture(furnitureId);
                if (SaveGate.Busy || furniture == null || skin < 0 || skin > furniture.skins.Length || altPrefab < 0 || altPrefab > furniture.AlternativePrefabs.Length) return null;
                if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(childRotation))
                {
                    ApiLog.Once(Housing.Owner, "Place", "position or rotation is NaN or Infinity, nothing was placed");
                    return null;
                }
                GameObject prefab = furniture.GetPrefab(altPrefab);
                if (prefab == null) return null;
                go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, manager.transform);
                FurniturePlaceable placeable = go.GetComponent<FurniturePlaceable>();
                if (placeable == null)
                {
                    UnityEngine.Object.Destroy(go);
                    return null;
                }
                go.transform.localRotation = rotation;
                if (go.transform.childCount > 0) go.transform.GetChild(0).localRotation = childRotation;
                BuildingSystem.FurnitureInfo.Meta meta = new BuildingSystem.FurnitureInfo.Meta();
                meta.currentSkin = skin;
                meta.currentAlternativePrefab = altPrefab;
                BuildingSystem.FurnitureInfo info = new BuildingSystem.FurnitureInfo(furniture, meta, Furniture.CreateTaskItem(furniture, meta), null);
                Placing = true;
                ApiCall = true;
                applyDepth++;
                try
                {
                    placeable.SetFurnitureInfo(info, manager);
                    placeable.UpdateMeta();
                    FurnitureItem item = Describe(manager.GUID, go);
                    RaiseFurniturePlaced(item);
                    return item;
                }
                finally
                {
                    Placing = false;
                    Placed = null;
                    ApiCall = false;
                    applyDepth--;
                }
            }
            catch (Exception e)
            {
                if (go != null) Discard(manager, go);
                ApiLog.Once(Housing.Owner, "Place", e);
                return null;
            }
        }

        private static void Discard(FurnitureManager manager, GameObject go)
        {
            manager.placedFurniture.RemoveAll(info => info != null && info.gameObject == go);
            UnityEngine.Object.Destroy(go);
        }

        public static bool Remove(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                ApiLog.Once(Housing.Owner, "Remove empty guid", "guid must be a non-empty furniture GUID");
                return false;
            }
            try
            {
                FurnitureManager manager;
                FurniturePlaceable placeable = FindPlaced(p => p.GUID == guid, out manager);
                return RemovePlaced(manager, placeable);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Remove", e);
                return false;
            }
        }

        public static bool Remove(FurnitureItem item)
        {
            if (item == null) throw new ArgumentNullException("item");
            try
            {
                GameObject go = item.Object;
                if (go == null) return false;
                FurnitureManager manager;
                FurniturePlaceable placeable = FindPlaced(p => p.gameObject == go, out manager);
                return RemovePlaced(manager, placeable);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Remove", e);
                return false;
            }
        }

        private static bool RemovePlaced(FurnitureManager manager, FurniturePlaceable placeable)
        {
            if (placeable == null || SaveGate.Busy || placeable.cantTake || HasPieces(manager, placeable) || HoldsThings(placeable)) return false;
            GameObject go = placeable.gameObject;
            ApiCall = true;
            applyDepth++;
            try
            {
                if (placeable.parent != null) placeable.parent.RemoveChildFurniture(placeable);
                if (placeable.attachedto != null) placeable.attachedto.RemoveAttachedFurniture(placeable);
                manager.TookFurniture(go);
                placeable.Destroyed();
                UnityEngine.Object.Destroy(go);
            }
            finally
            {
                ApiCall = false;
                applyDepth--;
            }
            return true;
        }

        public static bool Move(string guid, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrEmpty(guid))
            {
                ApiLog.Once(Housing.Owner, "Move empty guid", "guid must be a non-empty furniture GUID");
                return false;
            }
            try
            {
                FurnitureManager manager;
                FurniturePlaceable placeable = FindPlaced(p => p.GUID == guid, out manager);
                return MovePlaced(manager, placeable, position, rotation);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Move", e);
                return false;
            }
        }

        public static bool Move(FurnitureItem item, Vector3 position, Quaternion rotation)
        {
            if (item == null) throw new ArgumentNullException("item");
            try
            {
                GameObject go = item.Object;
                if (go == null) return false;
                FurnitureManager manager;
                FurniturePlaceable placeable = FindPlaced(p => p.gameObject == go, out manager);
                return MovePlaced(manager, placeable, position, rotation);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Move", e);
                return false;
            }
        }

        private static bool MovePlaced(FurnitureManager manager, FurniturePlaceable placeable, Vector3 position, Quaternion rotation)
        {
            if (!IsFinite(position) || !IsFinite(rotation))
            {
                ApiLog.Once(Housing.Owner, "Move", "position or rotation is NaN or Infinity, nothing was moved");
                return false;
            }
            if (placeable == null || SaveGate.Busy || placeable.cantTake || placeable.parent != null || placeable.attachedto != null || HasPieces(manager, placeable)) return false;
            GameObject go = placeable.gameObject;
            applyDepth++;
            try
            {
                go.transform.position = position;
                go.transform.localRotation = rotation;
                NaturalLightSourceChecker light = go.GetComponentInChildren<NaturalLightSourceChecker>();
                if (light != null) light.RefreshModifiers();
            }
            finally
            {
                applyDepth--;
            }
            return true;
        }

        internal static FurnitureItem Describe(string areaId, GameObject go)
        {
            FurniturePlaceable placeable = go.GetComponent<FurniturePlaceable>();
            Furniture furniture = placeable == null ? null : placeable.furniture;
            BuildingSystem.FurnitureInfo.Meta meta = placeable == null ? null : placeable.meta;
            Quaternion childRotation = go.transform.childCount > 0 ? go.transform.GetChild(0).localRotation : Quaternion.identity;
            GameObject parentObject = placeable == null || placeable.parent == null ? null : placeable.parent.gameObject;
            GameObject attachedObject = placeable == null || placeable.attachedto == null ? null : placeable.attachedto.gameObject;
            return new FurnitureItem(placeable == null ? null : NonEmpty(placeable.GUID), furniture == null ? null : furniture.Id, furniture == null ? go.name : furniture.title, areaId, go.transform.position, go.transform.localRotation, childRotation, meta == null ? 0 : meta.currentSkin, meta == null ? 0 : meta.currentAlternativePrefab, placeable == null || placeable.manager == null ? null : NonEmpty(placeable.manager.GUID), parentObject == null ? null : NonEmpty(placeable.parent.GUID), attachedObject == null ? null : NonEmpty(placeable.attachedto.GUID), parentObject, attachedObject, ApiCall, go);
        }

        private static string NonEmpty(string guid)
        {
            return string.IsNullOrEmpty(guid) ? null : guid;
        }

        internal static void RaiseFurniturePlaced(FurnitureItem item)
        {
            Events.Raise(FurniturePlaced, "Building.FurniturePlaced", h => h(item));
        }

        internal static void RaiseFurnitureRemoved(FurnitureItem item)
        {
            Events.Raise(FurnitureRemoved, "Building.FurnitureRemoved", h => h(item));
        }

        internal static void RaiseFurnitureLoaded(FurnitureItem item)
        {
            Events.Raise(FurnitureLoaded, "Building.FurnitureLoaded", h => h(item));
        }

        internal static bool IsFinite(Vector3 vector)
        {
            return IsFinite(vector.x) && IsFinite(vector.y) && IsFinite(vector.z);
        }

        internal static bool IsFinite(Quaternion rotation)
        {
            return IsFinite(rotation.x) && IsFinite(rotation.y) && IsFinite(rotation.z) && IsFinite(rotation.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static FurnitureManager FindArea(string areaId)
        {
            if (BuildingSystem.instance == null || string.IsNullOrEmpty(areaId)) return null;
            foreach (FurnitureManager manager in BuildingSystem.instance.furnitureManagers)
            {
                if (manager != null && manager.GUID == areaId) return manager;
            }
            return null;
        }

        private static FurniturePlaceable FindPlaced(Func<FurniturePlaceable, bool> match, out FurnitureManager owner)
        {
            owner = null;
            if (BuildingSystem.instance == null) return null;
            foreach (FurnitureManager manager in BuildingSystem.instance.furnitureManagers)
            {
                if (manager == null) continue;
                foreach (BuildingSystem.FurnitureInfo info in manager.placedFurniture)
                {
                    if (info == null || info.gameObject == null) continue;
                    FurniturePlaceable placeable = info.gameObject.GetComponent<FurniturePlaceable>();
                    if (placeable != null && match(placeable))
                    {
                        owner = manager;
                        return placeable;
                    }
                }
            }
            return null;
        }

        private static bool HasPieces(FurnitureManager manager, FurniturePlaceable placeable)
        {
            foreach (BuildingSystem.FurnitureInfo info in manager.AllFurnitures)
            {
                if (info == null || info.gameObject == null) continue;
                FurniturePlaceable other = info.gameObject.GetComponent<FurniturePlaceable>();
                if (other != null && other != placeable && (other.parent == placeable || other.attachedto == placeable)) return true;
            }
            return false;
        }

        private static bool HoldsThings(FurniturePlaceable placeable)
        {
            if (placeable.item != null || (placeable.itemRequired != null && placeable.itemRequired.ID > 0)) return true;
            FurnitureTrash trash = (FurnitureTrash)trashField.GetValue(placeable);
            if (trash != null && trash.items != null && trash.items.Count > 0) return true;
            return BuildingSystem.instance.HasSaveableContent(placeable.gameObject);
        }

        private static Furniture FindFurniture(string id)
        {
            if (!FurnitureRegistry.IsModId(id)) return LoadVanilla(id);
            FurnitureEntry entry = FurnitureRegistry.Find(id);
            return FurnitureManifestStore.Ready && entry != null ? FurnitureBuild.Registered(entry) : null;
        }

        internal static Furniture LoadVanilla(string path)
        {
            ScriptableObject asset = SavableScriptableObject.LoadFromPath(path);
            Furniture furniture = asset as Furniture;
            if (asset != null && furniture == null) Addressables.Release(asset);
            return furniture;
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
