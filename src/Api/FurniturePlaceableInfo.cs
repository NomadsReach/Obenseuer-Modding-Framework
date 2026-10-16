using System;

namespace ObenseuerCore
{
    internal static class FurniturePlaceableInfo
    {
        internal static bool Installed;

        internal static void Install()
        {
            bool ok;
            try
            {
                ok = CorePatcher.PatchCore(typeof(FurniturePlaceable), "SetFurnitureInfo", new[] { typeof(BuildingSystem.FurnitureInfo), typeof(FurnitureManager) }, null, CorePatcher.Handler(typeof(FurniturePlaceableInfo), "SetFurnitureInfoPostfix"), null, false, FurnitureRegistry.Owner);
            }
            catch (Exception e)
            {
                ok = false;
                ApiLog.Once(FurnitureRegistry.Owner, "placement install", e);
            }
            Installed = ok;
            if (!ok) CoreLog.For(FurnitureRegistry.Owner).Warn("placement not installed, a re-placed placeholder takes back as its template");
        }

        private static void SetFurnitureInfoPostfix(FurniturePlaceable __instance, BuildingSystem.FurnitureInfo info)
        {
            if (!Installed) return;
            try
            {
                if (__instance == null || info == null || info.furniture == null) return;
                if (FurnitureIds.Classify(info.furniture.id) != FurnitureIdKind.Furniture) return;
                if (__instance.furniture != info.furniture) __instance.furniture = info.furniture;
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "SetFurnitureInfo postfix", e);
            }
        }
    }
}
