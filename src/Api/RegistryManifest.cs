using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    [Serializable]
    public sealed class RegistryManifestEntry
    {
        public int Id;
        public string Mod;
        public string Key;
        public string Title;
    }

    [Serializable]
    public sealed class RegistryManifest
    {
        public List<RegistryManifestEntry> Items = new List<RegistryManifestEntry>();
        public List<RegistryManifestEntry> Recipes = new List<RegistryManifestEntry>();
    }

    internal static class RegistryManifestStore
    {
        private static List<RegistryManifestEntry> carriedItems = new List<RegistryManifestEntry>();
        private static List<RegistryManifestEntry> carriedRecipes = new List<RegistryManifestEntry>();
        private static string carriedFolder = "";

        internal static RegistryManifest Build()
        {
            RegistryManifest manifest = new RegistryManifest();
            ItemRegistry.AddManifestEntries(manifest.Items);
            RecipeRegistry.AddManifestEntries(manifest.Recipes);
            if (carriedFolder.Length > 0 && carriedFolder == (SaveController.LastSaveFolderPath ?? ""))
            {
                Merge(manifest.Items, carriedItems);
                Merge(manifest.Recipes, carriedRecipes);
            }
            return manifest;
        }

        internal static void Warn(RegistryManifest manifest)
        {
            carriedFolder = SaveController.LastSaveFolderPath ?? "";
            carriedItems = new List<RegistryManifestEntry>();
            carriedRecipes = new List<RegistryManifestEntry>();
            if (manifest == null) return;
            Collect(manifest.Items, carriedItems, ItemRegistry.IsApplied, "item", "until then that id shows as coin.");
            Collect(manifest.Recipes, carriedRecipes, RecipeRegistry.IsApplied, "recipe", "before crafting from this save.");
        }

        internal static void Merge(List<RegistryManifestEntry> target, List<RegistryManifestEntry> carried)
        {
            HashSet<int> seen = new HashSet<int>();
            foreach (RegistryManifestEntry entry in target)
            {
                seen.Add(entry.Id);
            }
            foreach (RegistryManifestEntry entry in carried)
            {
                if (seen.Add(entry.Id)) target.Add(entry);
            }
        }

        private static void Collect(List<RegistryManifestEntry> saved, List<RegistryManifestEntry> carried, Func<int, bool> isApplied, string kind, string consequence)
        {
            if (saved == null) return;
            foreach (RegistryManifestEntry entry in saved)
            {
                if (entry == null || isApplied(entry.Id)) continue;
                carried.Add(entry);
                CoreLog.For(RegistryIds.Owner).Warn("save data uses " + kind + " '" + entry.Title + "' (id " + entry.Id + ", mod " + entry.Mod + "/" + entry.Key + "), which is not loaded now. Reinstall that mod; " + consequence);
            }
        }
    }
}
