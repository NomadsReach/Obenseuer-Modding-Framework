using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using OS.Items;

namespace ObenseuerCore
{
    public sealed class SurfaceInfo
    {
        public string Id { get; private set; }

        public int WallpaperItemId { get; private set; }

        public int SkinIndex { get; private set; }

        internal SurfaceInfo(string id, int wallpaperItemId, int skinIndex)
        {
            Id = id;
            WallpaperItemId = wallpaperItemId;
            SkinIndex = skinIndex;
        }
    }

    public sealed class SurfaceArgs
    {
        public string Id { get; private set; }

        public int WallpaperItemId { get; private set; }

        public int SkinIndex { get; private set; }

        internal SurfaceArgs(string id, int wallpaperItemId, int skinIndex)
        {
            Id = id;
            WallpaperItemId = wallpaperItemId;
            SkinIndex = skinIndex;
        }
    }

    public static class Surfaces
    {
        internal const string Owner = "obenseuer.moddingframework.surfaces";
        private static bool installed;
        private static readonly FieldInfo savedItemIdField = AccessTools.Field(typeof(FurnitureSurfaceArea), "savedItemId");
        private static readonly FieldInfo savedSkinIndexField = AccessTools.Field(typeof(FurnitureSurfaceArea), "savedSkinIndex");

        public static event Action<SurfaceArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<SurfaceInfo> All()
        {
            List<SurfaceInfo> list = new List<SurfaceInfo>();
            try
            {
                foreach (FurnitureSurfaceArea surface in SceneIds.Active<FurnitureSurfaceArea>())
                {
                    list.Add(ToInfo(surface));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Surfaces.All", e);
            }
            return list;
        }

        public static SurfaceInfo Get(string id)
        {
            try
            {
                FurnitureSurfaceArea surface = SceneIds.Find<FurnitureSurfaceArea>(id);
                return surface == null ? null : ToInfo(surface);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Surfaces.Get", e);
                return null;
            }
        }

        public static bool SetWallpaper(string id, int itemId, int skinIndex)
        {
            try
            {
                FurnitureSurfaceArea surface = SceneIds.Find<FurnitureSurfaceArea>(id);
                if (surface == null || SaveGate.Busy) return false;
                if (!surface.HasSameWallpaper(itemId, skinIndex))
                {
                    Item item = ItemRegistry.FindInDatabase(itemId);
                    ItemWallpaperData wallpaper = item == null ? null : Item.GetMetaOfType<ItemWallpaperData>(item.Meta);
                    if (wallpaper == null) return false;
                    wallpaper.LoadVariants();
                    if (wallpaper.GetMaterial(skinIndex) == null || !surface.AllowedToPlace(item, skinIndex)) return false;
                    surface.SetWallpaper(wallpaper, skinIndex, itemId);
                }
                return surface.HasSameWallpaper(itemId, skinIndex);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Surfaces.SetWallpaper", e);
                return false;
            }
        }

        public static bool Remove(string id)
        {
            try
            {
                FurnitureSurfaceArea surface = SceneIds.Find<FurnitureSurfaceArea>(id);
                if (surface == null || SaveGate.Busy) return false;
                surface.RemoveWallpaper();
                return WallpaperItemId(surface) == 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Surfaces.Remove", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = savedItemIdField != null && savedSkinIndexField != null && SurfacesPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static void RaiseChanged(FurnitureSurfaceArea surface)
        {
            SurfaceArgs args = new SurfaceArgs(SceneIds.Of(surface), WallpaperItemId(surface), SkinIndex(surface));
            Events.Raise(Changed, "Surfaces.Changed", h => h(args));
        }

        internal static int WallpaperItemId(FurnitureSurfaceArea surface)
        {
            return (int)savedItemIdField.GetValue(surface);
        }

        private static int SkinIndex(FurnitureSurfaceArea surface)
        {
            return (int)savedSkinIndexField.GetValue(surface);
        }

        private static SurfaceInfo ToInfo(FurnitureSurfaceArea surface)
        {
            return new SurfaceInfo(SceneIds.Of(surface), WallpaperItemId(surface), SkinIndex(surface));
        }
    }
}
