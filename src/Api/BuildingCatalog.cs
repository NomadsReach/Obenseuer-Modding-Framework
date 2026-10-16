using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class FurnitureCatalogEntry
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Title { get; private set; }

        public string Category { get; private set; }

        public int SkinCount { get; private set; }

        public int AltCount { get; private set; }

        internal FurnitureCatalogEntry(string id, string name, string title, string category, int skinCount, int altCount)
        {
            Id = id;
            Name = name;
            Title = title;
            Category = category;
            SkinCount = skinCount;
            AltCount = altCount;
        }
    }

    public static partial class Building
    {
        public static List<FurnitureCatalogEntry> Catalog()
        {
            List<FurnitureCatalogEntry> list = new List<FurnitureCatalogEntry>();
            try
            {
                DeveloperConsole console = DeveloperConsole.Instance;
                if (console != null && console.furniture != null && console.furniture.furnitures != null)
                {
                    foreach (Furniture furniture in console.furniture.furnitures)
                    {
                        if (furniture != null && !FurnitureIds.IsModId(furniture.id)) list.Add(CatalogEntry(furniture));
                    }
                }
                if (FurnitureManifestStore.Ready)
                {
                    foreach (FurnitureEntry entry in FurnitureRegistry.All())
                    {
                        FurnitureCatalogEntry described = CoreEntry(entry);
                        if (described != null) list.Add(described);
                    }
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Catalog", e);
            }
            return list;
        }

        public static GameObject PrefabOf(string furnitureId, int alt)
        {
            if (string.IsNullOrEmpty(furnitureId))
            {
                ApiLog.Once(Housing.Owner, "PrefabOf empty id", "furnitureId must be a non-empty furniture id");
                return null;
            }
            try
            {
                Furniture furniture = FindFurniture(furnitureId);
                if (furniture == null || alt < 0 || alt > furniture.AlternativePrefabs.Length) return null;
                return furniture.GetPrefab(alt);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "PrefabOf", e);
                return null;
            }
        }

        public static bool IsKnownFurniture(string furnitureId)
        {
            if (string.IsNullOrEmpty(furnitureId)) return false;
            try
            {
                if (FurnitureRegistry.IsModId(furnitureId)) return FurnitureManifestStore.Ready && FurnitureRegistry.Find(furnitureId) != null;
                DeveloperConsole console = DeveloperConsole.Instance;
                if (console == null || console.furniture == null || console.furniture.furnitures == null) return false;
                foreach (Furniture furniture in console.furniture.furnitures)
                {
                    if (furniture != null && furniture.Id == furnitureId) return true;
                }
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "IsKnownFurniture", e);
                return false;
            }
        }

        private static FurnitureCatalogEntry CoreEntry(FurnitureEntry entry)
        {
            try
            {
                if (entry.Built != null) return CatalogEntry(entry.Built);
                Furniture template = FurnitureBuild.Template(entry.Definition);
                try
                {
                    string title = entry.Definition.Title ?? template.title;
                    return new FurnitureCatalogEntry(entry.Id, title, title, FurnitureCategories.DisplayName(template.category), 0, 0);
                }
                finally
                {
                    FurnitureBuild.ReleaseTemplate(entry.Definition, template);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Catalog " + entry.Id, e);
                return null;
            }
        }

        private static FurnitureCatalogEntry CatalogEntry(Furniture furniture)
        {
            return new FurnitureCatalogEntry(furniture.Id, furniture.name, furniture.title, FurnitureCategories.DisplayName(furniture.category), furniture.skins == null ? 0 : furniture.skins.Length, furniture.AlternativePrefabs.Length);
        }
    }
}
