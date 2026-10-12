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
    }

    public static class FurnitureRegistry
    {
        internal const string Owner = "obenseuer.moddingframework.furniture";
        internal const string ModIdPrefix = "obx:";
        private const int MaxAmount = 1000;
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
            return ModIdPrefix + modGuid + "/" + key;
        }

        public static string Define(string modGuid, string key, FurnitureDefinition definition)
        {
            string id = IdFor(modGuid, key);
            if (definition == null) throw new ArgumentNullException("definition");
            FurnitureDefinition snapshot = Snapshot(definition);
            if (entries.ContainsKey(id)) return id;
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

        public static List<string> ShopGuids()
        {
            List<string> guids = new List<string>();
            foreach (FurnitureShop shop in UnityEngine.Object.FindObjectsByType<FurnitureShop>(FindObjectsSortMode.None))
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
            return id != null && id.StartsWith(ModIdPrefix, StringComparison.Ordinal);
        }

        internal static bool TryParse(string id, out string modGuid, out string key)
        {
            modGuid = null;
            key = null;
            if (!IsModId(id)) return false;
            int slash = id.IndexOf('/', ModIdPrefix.Length);
            if (slash < 0) return false;
            string mod = id.Substring(ModIdPrefix.Length, slash - ModIdPrefix.Length);
            string name = id.Substring(slash + 1);
            if (!ModIds.IsValid(mod) || !ModIds.IsValid(name)) return false;
            modGuid = mod;
            key = name;
            return true;
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
                Model = definition.Model
            };
        }
    }

    internal sealed class FurnitureEntry
    {
        internal readonly string Id;
        internal readonly FurnitureDefinition Definition;
        internal Furniture Built;

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
