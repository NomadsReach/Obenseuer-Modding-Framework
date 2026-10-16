using System;

namespace ObenseuerCore
{
    internal static class BuildingLoaded
    {
        internal static void OnLoaded()
        {
            if (!FurnitureRegistry.IsAvailable) return;
            if (SaveData.Folder != null) Scan();
        }

        private static void Scan()
        {
            try
            {
                foreach (FurnitureItem item in Building.AllPlacedFurniture())
                {
                    if (item.Object != null && FurnitureRegistry.IsModId(item.Id)) Building.RaiseFurnitureLoaded(item);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(FurnitureRegistry.Owner, "Building.FurnitureLoaded scan", e);
            }
        }
    }
}
