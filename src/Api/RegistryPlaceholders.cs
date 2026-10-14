using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class RegistryPlaceholders
    {
        internal const int StackLimit = 100000000;
        private const string IconPath = "Sprites/Items/Paper Bag_spr";
        private const string PrefabPath = "Prefabs/Items/ERROR";
        private static readonly Dictionary<int, Item> cache = new Dictionary<int, Item>();

        internal static bool IsPlaceholder(int id)
        {
            return id >= RegistryIds.FirstId && ItemRegistry.IsAnnounced && ItemDatabase.instance != null && ItemRegistry.FindInDatabase(id) == null;
        }

        internal static bool IsMissing(int id)
        {
            return IsPlaceholder(id) && !ItemRegistry.IsDefined(id);
        }

        internal static Item For(int id)
        {
            if (!ItemRegistry.IsAnnounced) return null;
            Item item;
            if (!cache.TryGetValue(id, out item))
            {
                item = Create(id);
                cache.Add(id, item);
            }
            return item;
        }

        internal static void Forget(int id)
        {
            if (cache.Count > 0) cache.Remove(id);
        }

        internal static void Retitle()
        {
            foreach (KeyValuePair<int, Item> pair in cache)
            {
                Label(pair.Value, pair.Key);
            }
        }

        private static Item Create(int id)
        {
            Item item = new Item
            {
                ID = id,
                BaseValue = 0,
                BaseStackable = StackLimit,
                Categories = new string[0],
                Appearance = new Item.ItemAppearance { SpritePath = IconPath, PrefabPath = PrefabPath },
                Actions = new Item.ItemAction[0],
                Meta = new object[0]
            };
            item.Appearance.LoadSprite();
            Label(item, id);
            return item;
        }

        private static void Label(Item item, int id)
        {
            RegistryManifestEntry entry = RegistryManifestStore.CarriedItem(id);
            string mod = entry == null || string.IsNullOrEmpty(entry.Mod) ? "" : entry.Mod;
            string title = entry == null || string.IsNullOrEmpty(entry.Title) ? "" : entry.Title;
            string detail = mod.Length == 0 ? title : mod + (title.Length == 0 ? "" : ": " + title);
            item.Title = detail.Length == 0 ? "Missing item" : "Missing item (" + detail + ")";
            string who = mod.Length == 0 ? "A mod" : "The mod " + mod;
            item.Description = ItemRegistry.IsDefined(id)
                ? who + " is installed but could not add this item. Check the Core log. The item comes back when it loads."
                : who + " that adds this item is not installed. The item comes back when that mod is reinstalled.";
        }
    }
}
