using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class RegistryManifestEntry
    {
        public int Id;
        public string Mod;
        public string Key;
        public string Title;
    }

    [Serializable]
    internal sealed class RegistryManifest
    {
        public List<RegistryManifestEntry> Items = new List<RegistryManifestEntry>();
        public List<RegistryManifestEntry> Recipes = new List<RegistryManifestEntry>();
    }

    internal static class RegistryManifestStore
    {
        private static List<RegistryManifestEntry> carriedItems = new List<RegistryManifestEntry>();
        private static List<RegistryManifestEntry> carriedRecipes = new List<RegistryManifestEntry>();
        private static string carriedFolder = "";
        private const int MaxEntries = 4096;

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
            if (manifest != null)
            {
                Collect(manifest.Items, carriedItems, ItemRegistry.IsApplied, "item", "until then its stacks show as Missing item and keep their id.");
                Collect(manifest.Recipes, carriedRecipes, RecipeRegistry.IsApplied, "recipe", "until then this recipe cannot be crafted from this save.");
            }
            RegistryPlaceholders.Retitle();
        }

        internal static RegistryManifestEntry CarriedItem(int id)
        {
            foreach (RegistryManifestEntry entry in carriedItems)
            {
                if (entry.Id == id) return entry;
            }
            return null;
        }

        internal static void Forget(string modId, Func<int, bool> isUsed)
        {
            carriedItems.RemoveAll(entry => (modId == null || entry.Mod == modId) && !isUsed(entry.Id));
            carriedRecipes.RemoveAll(entry => modId == null || entry.Mod == modId);
            RegistryPlaceholders.Retitle();
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
                if (carried.Count == MaxEntries)
                {
                    CoreLog.For(RegistryIds.Owner).Warn("save data lists more than " + MaxEntries + " unloaded " + kind + "s, the rest are not kept");
                    break;
                }
                carried.Add(entry);
                ApiLog.Once(RegistryIds.Owner, "save data uses " + kind + " id " + entry.Id + ", mod " + entry.Mod + "/" + entry.Key, "'" + Net.Plain(entry.Title, Net.MaxTextLength) + "' is not loaded now. Reinstall that mod; " + consequence + " To drop this warning, call ItemRegistry.RemoveMissingItems(\"" + Net.Plain(entry.Mod, Net.MaxTextLength) + "\"), which removes its missing stacks in the player's inventory and loaded containers, and this entry.");
            }
        }
    }
}
