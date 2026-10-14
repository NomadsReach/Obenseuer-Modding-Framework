using System;
using System.Reflection;
using OS.Items;

namespace ObenseuerCore
{
    internal static class SurfacesPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo setPrefix = CorePatcher.Handler(typeof(SurfacesPatches), "SetPrefix");
                MethodInfo setPostfix = CorePatcher.Handler(typeof(SurfacesPatches), "SetPostfix");
                MethodInfo removePrefix = CorePatcher.Handler(typeof(SurfacesPatches), "RemovePrefix");
                MethodInfo removePostfix = CorePatcher.Handler(typeof(SurfacesPatches), "RemovePostfix");
                return CorePatcher.PatchCore(typeof(FurnitureSurfaceArea), "SetWallpaper", new[] { typeof(ItemWallpaperData), typeof(int), typeof(int) }, setPrefix, setPostfix, null, true, Surfaces.Owner)
                    && CorePatcher.PatchCore(typeof(FurnitureSurfaceArea), "RemoveWallpaper", Type.EmptyTypes, removePrefix, removePostfix, null, true, Surfaces.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Surfaces.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static void SetPrefix(FurnitureSurfaceArea __instance, int skinIndex, int itemId, out bool __state)
        {
            __state = __instance.HasSameWallpaper(itemId, skinIndex);
        }

        private static void SetPostfix(FurnitureSurfaceArea __instance, bool __state)
        {
            if (__state) return;
            try
            {
                Surfaces.RaiseChanged(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Surfaces.Owner, "Surfaces.Changed", e);
            }
        }

        private static void RemovePrefix(FurnitureSurfaceArea __instance, out bool __state)
        {
            __state = Surfaces.WallpaperItemId(__instance) != 0;
        }

        private static void RemovePostfix(FurnitureSurfaceArea __instance, bool __state)
        {
            if (!__state) return;
            try
            {
                Surfaces.RaiseChanged(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Surfaces.Owner, "Surfaces.Changed", e);
            }
        }
    }
}
