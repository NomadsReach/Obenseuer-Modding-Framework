using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class StampedEntry<T>
    {
        internal T Value;
        internal long Stamp;
    }

    internal static class ModelIcons
    {
        internal const string Owner = "obenseuer.moddingframework.icons";
        private const int MaxSide = 1024;
        private const int WarnSide = 256;
        private static readonly Dictionary<string, StampedEntry<Sprite>> sprites = new Dictionary<string, StampedEntry<Sprite>>(StringComparer.OrdinalIgnoreCase);

        internal static Sprite Load(string absPath)
        {
            if (string.IsNullOrEmpty(absPath)) return null;
            string key = FullPath(absPath);
            long stamp = WriteStamp(key);
            StampedEntry<Sprite> cached;
            if (sprites.TryGetValue(key, out cached) && cached.Stamp == stamp) return cached.Value;
            cached = new StampedEntry<Sprite> { Value = Build(key), Stamp = stamp };
            sprites[key] = cached;
            return cached.Value;
        }

        internal static long WriteStamp(string path)
        {
            try
            {
                return File.GetLastWriteTimeUtc(path).Ticks;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static string FullPath(string absPath)
        {
            try
            {
                return Path.GetFullPath(absPath);
            }
            catch (Exception)
            {
                return absPath;
            }
        }

        private static Sprite Build(string absPath)
        {
            try
            {
                byte[] bytes = new FileInfo(absPath).Length > ModelBuild.MaxImageBytes ? null : File.ReadAllBytes(absPath);
                if (bytes == null || ModelBuild.ImageRefused(bytes, MaxSide))
                {
                    CoreLog.For(Owner).Error("icon " + absPath + " is not a readable PNG or JPEG, or is over " + MaxSide + " px a side or " + (ModelBuild.MaxImageBytes >> 20) + " MB, it is not decoded, the template icon is kept");
                    return null;
                }
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, bytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    CoreLog.For(Owner).Error("icon " + absPath + " is not a readable image, the template icon is kept");
                    return null;
                }
                if (texture.width > MaxSide || texture.height > MaxSide)
                {
                    CoreLog.For(Owner).Error("icon " + absPath + " is " + texture.width + "x" + texture.height + ", the limit is " + MaxSide + " px per side, the template icon is kept");
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }
                if (texture.width > WarnSide || texture.height > WarnSide)
                {
                    CoreLog.For(Owner).Warn("icon " + absPath + " is " + texture.width + "x" + texture.height + ", 128 or 256 px is recommended");
                }
                if (texture.width != texture.height) texture = Square(texture);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("icon " + absPath + " failed to load, the template icon is kept: " + e.ToString());
                return null;
            }
        }

        private static Texture2D Square(Texture2D source)
        {
            int size = Math.Max(source.width, source.height);
            int offsetX = (size - source.width) / 2;
            int offsetY = (size - source.height) / 2;
            Color32[] from = source.GetPixels32();
            Color32[] to = new Color32[size * size];
            for (int y = 0; y < source.height; y++)
            {
                Array.Copy(from, y * source.width, to, (y + offsetY) * size + offsetX, source.width);
            }
            Texture2D square = new Texture2D(size, size, TextureFormat.RGBA32, false);
            square.SetPixels32(to);
            square.Apply();
            UnityEngine.Object.Destroy(source);
            return square;
        }
    }

    public static class ItemIcons
    {
        private static readonly Dictionary<int, string> overrides = new Dictionary<int, string>();

        public static bool IsAvailable
        {
            get { return ModelIconsPatches.Installed && !CorePatcher.IsDisabled(ModelIcons.Owner); }
        }

        public static bool OverrideVanilla(int itemId, string pngPath)
        {
            if (string.IsNullOrEmpty(pngPath) || !Path.IsPathRooted(pngPath)) throw new ArgumentException("pngPath must be an absolute path", "pngPath");
            if (!IsAvailable || ModelIcons.Load(pngPath) == null) return false;
            List<Item> list = ItemDatabase.database;
            bool built = list != null && list.Count > 0;
            if (built && !HasAppearance(list, itemId)) return false;
            string previous;
            if (overrides.TryGetValue(itemId, out previous) && previous != pngPath) CoreLog.For(ModelIcons.Owner).Warn("item " + itemId + " icon replaced: " + previous + " by " + pngPath);
            overrides[itemId] = pngPath;
            if (built) ApplyToDatabase(list, itemId);
            return true;
        }

        internal static void Apply(Item item, string pngPath)
        {
            if (string.IsNullOrEmpty(pngPath) || item.Appearance == null) return;
            Sprite sprite = ModelIcons.Load(pngPath);
            if (sprite == null) return;
            item.Appearance.Sprite = sprite;
            item.Appearance.SpritePath = "";
            List<Sprite> skins = item.Appearance.SkinSprites;
            for (int i = 0; i < skins.Count; i++)
            {
                skins[i] = sprite;
            }
        }

        internal static void Reapply(Item item)
        {
            string pngPath;
            if (!overrides.TryGetValue(item.ID, out pngPath)) pngPath = ItemRegistry.IconPathFor(item.ID);
            Apply(item, pngPath);
        }

        internal static void ReapplyAll(List<Item> list)
        {
            foreach (Item item in list)
            {
                Reapply(item);
            }
        }

        private static bool HasAppearance(List<Item> list, int itemId)
        {
            foreach (Item item in list)
            {
                if (item.ID == itemId) return item.Appearance != null;
            }
            return false;
        }

        private static void ApplyToDatabase(List<Item> list, int itemId)
        {
            foreach (Item item in list)
            {
                if (item.ID == itemId) Apply(item, overrides[itemId]);
            }
        }
    }
}
