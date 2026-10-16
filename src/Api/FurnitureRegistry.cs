using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class FurnitureDefinition
    {
        public string TemplatePath;
        public string TemplateName;
        public string Title;
        public string Details;
        public int PriceOC = -1;
        public int PriceRM = -1;
        public Sprite Icon;
        public string[] ShopIds;
        public int ShopStock = 3;
        public ModelHandle Model;
        public FurniturePlacement Placement = FurniturePlacement.Template;
        public Bounds[] Colliders;
        public string CategoryName;
    }

    public enum FurniturePlacement
    {
        Template,
        Floor,
        Wall,
        Ceiling,
        FloorAndWall,
        Any
    }

    public sealed class BlueprintDefinition
    {
        public string Title;
        public string Details;
        public Sprite Icon;
        public Dictionary<int, int> Inputs = new Dictionary<int, int>();
        public List<string> FurnitureInputs = new List<string>();
        public float ProcessTime = 3600f;
        public Skill Skill;
        public int MinimumSkill;
        public int Amount = 1;
        public List<string> Pools = new List<string>();
    }

    public static partial class FurnitureRegistry
    {
        internal const string Owner = "obenseuer.moddingframework.furniture";
        private const int MaxAmount = 1000;
        private const int MaxColliders = 64;
        private static readonly Dictionary<string, FurnitureEntry> entries = new Dictionary<string, FurnitureEntry>();

        public static event Action Loaded;

        public static bool IsAvailable
        {
            get { return FurniturePatches.Installed && !CorePatcher.IsDisabled(Owner); }
        }

        public static string IdFor(string modGuid, string key)
        {
            ModIds.Require(modGuid, "modGuid");
            if (!ModIds.IsValid(key)) throw new ArgumentException("key must be 1 to 64 characters of letters, digits, '.', '_' or '-'", "key");
            return FurnitureIds.Prefix + modGuid + "/" + key;
        }

        public static string Define(string modGuid, string key, FurnitureDefinition definition)
        {
            string id = IdFor(modGuid, key);
            if (definition == null) throw new ArgumentNullException("definition");
            FurnitureDefinition snapshot = Snapshot(definition);
            FurnitureEntry existing;
            if (entries.TryGetValue(id, out existing)) return existing.Id;
            if (definition.Colliders != null && definition.Colliders.Length > MaxColliders)
            {
                ApiLog.Once(Owner, "Define colliders", "Colliders must hold at most " + MaxColliders + " Bounds, each one becomes a BoxCollider on the piece and on its clones");
                return null;
            }
            if (!IsAvailable || FurnitureBuild.Started)
            {
                CoreLog.For(Owner).Error("furniture " + id + " refused: the furniture patches are not installed, or a furniture was already built");
                return null;
            }
            entries[id] = new FurnitureEntry(id, snapshot);
            return id;
        }

        public static bool Give(string furnitureId, int amount)
        {
            if (amount < 1 || amount > MaxAmount) throw new ArgumentException("amount must be 1 to " + MaxAmount, "amount");
            if (furnitureId == null || !FurnitureManifestStore.Ready || SaveGate.Busy || BuildingSystem.instance == null) return false;
            try
            {
                FurnitureEntry entry = Find(furnitureId);
                Furniture furniture = entry == null ? null : FurnitureBuild.Registered(entry);
                return furniture != null && BuildingSystem.instance.AddFurniture(furniture, amount);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Give", e);
                return false;
            }
        }

        public static Furniture Get(string id)
        {
            try
            {
                FurnitureEntry entry = Find(id);
                return FurnitureManifestStore.Ready && entry != null ? FurnitureBuild.Registered(entry) : null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Get", e);
                return null;
            }
        }

        public static string DefineBlueprint(string furnitureId, BlueprintDefinition definition)
        {
            if (furnitureId == null)
            {
                ApiLog.Once(Owner, "DefineBlueprint null furnitureId", "furnitureId is null, no blueprint defined");
                return null;
            }
            if (definition == null) throw new ArgumentNullException("definition");
            BlueprintDefinition snapshot = SnapshotBlueprint(definition);
            FurnitureEntry target = Find(furnitureId);
            if (target == null) throw new ArgumentException("furnitureId " + furnitureId + " is not a furniture defined with Define", "furnitureId");
            string blueprintId = FurnitureIds.BlueprintIdFor(target.Id);
            if (FurnitureBlueprints.Find(blueprintId) != null) return blueprintId;
            if (!IsAvailable || FurnitureBuild.Started)
            {
                CoreLog.For(Owner).Error("blueprint " + blueprintId + " refused: the furniture patches are not installed, or a furniture was already built");
                return null;
            }
            FurnitureBlueprints.Add(target, snapshot);
            return blueprintId;
        }

        public static bool GiveBlueprint(string furnitureId)
        {
            return GiveBlueprint(furnitureId, false);
        }

        public static bool GiveBlueprint(string furnitureId, bool silent)
        {
            if (furnitureId == null || !IsAvailable || !FurnitureManifestStore.Ready || SaveGate.Busy || !FurnitureBlueprints.ManagersLoaded()) return false;
            try
            {
                BlueprintEntry entry = FurnitureBlueprints.Find(FurnitureIds.BlueprintIdFor(furnitureId));
                if (entry == null || FurnitureBlueprints.IsKnown(entry.Id)) return false;
                TaskItemBlueprint blueprint = FurnitureBlueprints.Registered(entry);
                if (blueprint == null) return false;
                return silent ? TaskItemsManager.instance.AddTaskItem(blueprint) : blueprint.GiveItemWithResults();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GiveBlueprint", e);
                return false;
            }
        }

        public static bool IsBlueprintKnown(string furnitureId)
        {
            if (furnitureId == null || SaveGate.Loading) return false;
            try
            {
                BlueprintEntry entry = FurnitureBlueprints.Find(FurnitureIds.BlueprintIdFor(furnitureId));
                return entry != null && FurnitureBlueprints.IsKnown(entry.Id);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "IsBlueprintKnown", e);
                return false;
            }
        }

        public static bool GiveFlatpack(string furnitureId, int amount)
        {
            if (amount < 1 || amount > FurnitureFlatpacks.MaxAmount) throw new ArgumentException("amount must be 1 to " + FurnitureFlatpacks.MaxAmount, "amount");
            if (furnitureId == null || !FurnitureManifestStore.Ready || SaveGate.Busy) return false;
            try
            {
                FurnitureEntry entry = Find(furnitureId);
                Furniture furniture = entry == null ? null : FurnitureBuild.Registered(entry);
                return furniture != null && FurnitureFlatpacks.Give(furniture, amount);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GiveFlatpack", e);
                return false;
            }
        }

        public static List<string> ShopGuids()
        {
            List<string> guids = new List<string>();
            foreach (FurnitureShop shop in SceneIds.Active<FurnitureShop>())
            {
                guids.Add(shop.GUID);
            }
            return guids;
        }

        internal static void RaiseLoaded()
        {
            Events.Raise(Loaded, "FurnitureRegistry.Loaded", h => h());
        }

        internal static FurnitureEntry Find(string id)
        {
            FurnitureEntry entry;
            return id != null && entries.TryGetValue(id, out entry) ? entry : null;
        }

        internal static ICollection<FurnitureEntry> All()
        {
            return entries.Values;
        }

        internal static bool IsModId(string id)
        {
            return FurnitureIds.IsModId(id);
        }

        internal static bool TryParse(string id, out string modGuid, out string key)
        {
            return FurnitureIds.TryParse(id, out modGuid, out key);
        }

        private static FurnitureDefinition Snapshot(FurnitureDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.TemplatePath) == string.IsNullOrEmpty(definition.TemplateName)) throw new ArgumentException("set exactly one of TemplatePath or TemplateName", "definition");
            RegistryIds.CheckText(definition.Title, 1, 200, "Title");
            RegistryIds.CheckText(definition.Details, 0, 2000, "Details");
            if (definition.PriceOC < -1 || definition.PriceRM < -1) throw new ArgumentException("PriceOC and PriceRM must be 0 or more, or -1 to keep the template's", "definition");
            if (definition.ShopStock < 1) throw new ArgumentException("ShopStock must be 1 or more", "definition");
            string[] shops = definition.ShopIds ?? new string[0];
            foreach (string shop in shops)
            {
                if (string.IsNullOrEmpty(shop)) throw new ArgumentException("ShopIds must not contain empty entries", "definition");
            }
            if (definition.Model != null && !ModelRegistry.Owns(definition.Model)) throw new ArgumentException("Model must come from ModelRegistry.LoadObj, or be null to keep the template's mesh", "definition");
            if (!Enum.IsDefined(typeof(FurniturePlacement), definition.Placement)) throw new ArgumentException("Placement is not a defined value", "definition");
            RegistryIds.CheckText(definition.CategoryName, 0, 40, "CategoryName");
            if (FurnitureCategories.IsReserved(definition.CategoryName)) throw new ArgumentException("CategoryName " + definition.CategoryName + " is a game group with no build menu group of its own, use another name", "definition");
            if (definition.Colliders != null && definition.Model == null) throw new ArgumentException("Colliders needs Model, the boxes are fitted to the model", "definition");
            if (definition.Colliders != null)
            {
                if (definition.Colliders.Length == 0) throw new ArgumentException("Colliders must hold at least one Bounds, or be null", "definition");
                foreach (Bounds bounds in definition.Colliders)
                {
                    if (!Building.IsFinite(bounds.center) || !Building.IsFinite(bounds.size) || !(bounds.size.x > 0f && bounds.size.y > 0f && bounds.size.z > 0f)) throw new ArgumentException("each Colliders box needs a finite center and a size above 0 on every axis", "definition");
                }
            }
            return new FurnitureDefinition
            {
                TemplatePath = definition.TemplatePath,
                TemplateName = definition.TemplateName,
                Title = definition.Title,
                Details = definition.Details,
                PriceOC = definition.PriceOC,
                PriceRM = definition.PriceRM,
                Icon = definition.Icon,
                ShopIds = (string[])shops.Clone(),
                ShopStock = definition.ShopStock,
                Model = definition.Model,
                Placement = definition.Placement,
                Colliders = definition.Colliders == null ? null : (Bounds[])definition.Colliders.Clone(),
                CategoryName = definition.CategoryName
            };
        }

        private static BlueprintDefinition SnapshotBlueprint(BlueprintDefinition definition)
        {
            RegistryIds.CheckText(definition.Title, 1, 200, "Title");
            RegistryIds.CheckText(definition.Details, 0, 2000, "Details");
            if (!Enum.IsDefined(typeof(Skill), definition.Skill)) throw new ArgumentException("Skill is not a defined skill", "definition");
            string error = FurnitureIds.CheckBlueprint(definition.ProcessTime, definition.Amount, definition.Skill != Skill.None, definition.MinimumSkill, Skills.MaxLevel) ?? FurnitureIds.CheckInputs(definition.Inputs, definition.FurnitureInputs) ?? FurnitureIds.CheckPools(definition.Pools);
            if (error != null) throw new ArgumentException(error, "definition");
            return new BlueprintDefinition
            {
                Title = definition.Title,
                Details = definition.Details,
                Icon = definition.Icon,
                Inputs = new Dictionary<int, int>(definition.Inputs),
                FurnitureInputs = new List<string>(definition.FurnitureInputs),
                ProcessTime = definition.ProcessTime,
                Skill = definition.Skill,
                MinimumSkill = definition.MinimumSkill,
                Amount = definition.Amount,
                Pools = new List<string>(definition.Pools)
            };
        }
    }

    internal sealed class FurnitureEntry
    {
        internal readonly string Id;
        internal readonly FurnitureDefinition Definition;
        internal Furniture Built;
        internal bool BuildFailed;

        internal FurnitureEntry(string id, FurnitureDefinition definition)
        {
            Id = id;
            Definition = definition;
        }

        internal bool ListedAt(string shopGuid)
        {
            return shopGuid != null && Array.IndexOf(Definition.ShopIds, shopGuid) >= 0;
        }
    }
}
